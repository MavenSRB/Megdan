using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;

namespace GuardsDigital.Core.Rules.Displacement
{
    /// <summary>Why a validated displacement could not be committed after all.</summary>
    public enum DisplacementRejectionReason
    {
        None = 0,

        /// <summary>The subject is no longer on the board.</summary>
        UnknownObject = 1,

        /// <summary>The destination is not a space of the selected map.</summary>
        DestinationOffBoard = 2,

        /// <summary>Another obstacle took the destination between validation and commit.</summary>
        DestinationOccupied = 3,

        /// <summary>
        /// The destination is a declared terrain cell: on the board and permanently an obstacle
        /// (p.18), so nothing may end up there. Distinct from off-board, which no permission answers.
        /// </summary>
        DestinationTerrain = 4
    }

    /// <summary>The outcome of one committed or refused displacement.</summary>
    public readonly struct DisplacementCommitResult
    {
        private DisplacementCommitResult(
            bool accepted,
            DisplacementRejectionReason rejectionReason,
            BoardObjectReference subject,
            HexCoordinate origin,
            HexCoordinate destination)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Subject = subject;
            Origin = origin;
            Destination = destination;
        }

        public bool Accepted { get; }
        public DisplacementRejectionReason RejectionReason { get; }
        public BoardObjectReference Subject { get; }
        public HexCoordinate Origin { get; }
        public HexCoordinate Destination { get; }

        public static DisplacementCommitResult Accept(
            BoardObjectReference subject,
            HexCoordinate origin,
            HexCoordinate destination) =>
            new DisplacementCommitResult(
                true, DisplacementRejectionReason.None, subject, origin, destination);

        public static DisplacementCommitResult Reject(DisplacementRejectionReason reason)
        {
            if (reason == DisplacementRejectionReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason), reason, "A refused displacement requires a reason.");
            }

            return new DisplacementCommitResult(false, reason, default, default, default);
        }
    }

    /// <summary>
    /// What one displacement did, as the plain typed fact a later checkpoint reads.
    ///
    /// Milestone 6.4's fifth acceptance check requires the effect to record its displacement and expose
    /// the after-action checkpoint milestone 6.4a attaches its displaced-minion correction to. This is
    /// that record. It deliberately carries no correction, no Battle-Zone membership, and no path: p.18
    /// runs the correction "after you perform an action", so what an individual displacement owes the
    /// checkpoint is only which object moved, from where, to where, and how far short of its request it
    /// fell.
    ///
    /// It also deliberately does not request a Lane Push. p.18 is explicit that "displacing a minion
    /// miniature (by moving, pushing, or swapping it) has no impact on the Minion Battle", so a Lane
    /// Push follows a recorded Defeat or a successful Remove and never an ordinary relocation.
    /// </summary>
    public sealed class DisplacementRecord
    {
        public DisplacementRecord(
            BoardObjectReference subject,
            DisplacementKind kind,
            HexCoordinate origin,
            HexCoordinate destination,
            int requestedDistance,
            int actualDistance)
        {
            if (!subject.IsDefined)
            {
                throw new ArgumentException("A displacement record requires a subject.", nameof(subject));
            }

            if (kind == DisplacementKind.None)
            {
                throw new ArgumentException("A displacement record requires a kind.", nameof(kind));
            }

            if (requestedDistance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedDistance), requestedDistance, "A requested distance must not be negative.");
            }

            if (actualDistance < 0 || actualDistance > requestedDistance)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(actualDistance),
                    actualDistance,
                    "An actual distance must lie between zero and the requested distance.");
            }

            Subject = subject;
            Kind = kind;
            Origin = origin;
            Destination = destination;
            RequestedDistance = requestedDistance;
            ActualDistance = actualDistance;
        }

        public BoardObjectReference Subject { get; }

        /// <summary>
        /// Which operation displaced the subject. The two stay distinct rather than becoming one
        /// "moved" fact because p.11 and p.18 give them different rules, and
        /// <c>CARD_EFFECT_CAPABILITIES.md</c> forbids treating direct Move and Object Push as
        /// interchangeable.
        /// </summary>
        public DisplacementKind Kind { get; }

        public HexCoordinate Origin { get; }
        public HexCoordinate Destination { get; }
        public int RequestedDistance { get; }
        public int ActualDistance { get; }

        /// <summary>Whether the subject actually changed space. A zero-space push is recorded, not dropped.</summary>
        public bool Moved => ActualDistance > 0;
    }

    public enum DisplacementKind
    {
        None = 0,

        /// <summary>Rulebook p.11 movement written inside card text. Not a Movement action.</summary>
        DirectMove = 1,

        /// <summary>Rulebook p.18 Object Push. Not Lane Push, not Movement, not Place.</summary>
        ObjectPush = 2
    }
}
