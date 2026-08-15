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
}
