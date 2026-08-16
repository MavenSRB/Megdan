using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;

namespace GuardsDigital.Core.Rules.BoardObjects
{
    /// <summary>Why a Replace was refused. A refusal is a rules answer, never an engine fault.</summary>
    public enum ReplaceRejectionReason
    {
        None = 0,

        /// <summary>The object to replace is not on the board.</summary>
        OutgoingNotOnBoard = 1,

        /// <summary>
        /// The incoming object is the same kind of object as the outgoing one. p.19 requires "a
        /// different object", so a substitution that changes nothing is not a Replace.
        /// </summary>
        IncomingNotDifferent = 2,

        /// <summary>No copy of the incoming object is left in its supply (p.19: "tokens are intentionally limited").</summary>
        SupplyEmpty = 3,

        /// <summary>The declared observation policy is one this operation does not implement.</summary>
        UnsupportedObservation = 4,

        /// <summary>The board refused the substitution between derivation and commit.</summary>
        ReplacementUnavailable = 5
    }

    /// <summary>
    /// The typed outcome of one Replace: both objects and the one space they share.
    ///
    /// One space, not two, is the whole point. p.19's Replace substitutes an object where it stands, so
    /// there is no origin and destination pair to record and nothing here can be read as a relocation.
    /// It also carries no Lane-Push qualification: a Replace takes no piece off the board on its own
    /// account - the space it occupied is occupied still - so p.8 has nothing to count.
    /// </summary>
    public readonly struct ReplaceOutcome
    {
        private ReplaceOutcome(
            bool accepted,
            ReplaceRejectionReason rejectionReason,
            PlacedToken outgoing,
            PlacedToken incoming,
            HexCoordinate coordinate)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Outgoing = outgoing;
            Incoming = incoming;
            Coordinate = coordinate;
        }

        public bool Accepted { get; }
        public ReplaceRejectionReason RejectionReason { get; }

        /// <summary>The instance that went back to its supply, or <c>null</c> when the Replace was refused.</summary>
        public PlacedToken Outgoing { get; }

        /// <summary>The instance now standing in the space, or <c>null</c> when the Replace was refused.</summary>
        public PlacedToken Incoming { get; }

        /// <summary>The one space both objects occupy, before and after.</summary>
        public HexCoordinate Coordinate { get; }

        internal static ReplaceOutcome Accept(
            PlacedToken outgoing, PlacedToken incoming, HexCoordinate coordinate) =>
            new ReplaceOutcome(
                true, ReplaceRejectionReason.None, outgoing, incoming, coordinate);

        internal static ReplaceOutcome Reject(ReplaceRejectionReason reason)
        {
            if (reason == ReplaceRejectionReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason), reason, "A refused Replace requires a reason.");
            }

            return new ReplaceOutcome(false, reason, null, null, default);
        }
    }

    /// <summary>
    /// Rulebook p.19's Replace, as one operation every caller shares.
    ///
    /// "Swap an object (such as a miniature or a token on the board) with a different object."
    ///
    /// The declared <see cref="ReplacePolicy"/> is what makes this a Replace rather than a Remove and a
    /// Place written next to each other, and the commit is what makes the declaration true: the
    /// substitution is one board transaction, so the space is never empty between the two objects and
    /// nothing - a Spawn looking for an empty Spawn point, another placement, a displacement - can take
    /// it in between. That is why an observation policy promising two observable stages is refused here
    /// instead of silently satisfied by the single-stage commit.
    ///
    /// This milestone's arm is token-for-token, which is what the supply arm can express: a miniature
    /// substitute would need the unit reserve and p.18's Respawn eligibility, and that specialized rule
    /// is deliberately unimplemented.
    /// </summary>
    public static class ReplaceOperation
    {
        public static ReplaceOutcome ReplaceToken(
            ReplacePolicy policy,
            IBoardObjectExchangeAuthority exchange,
            ITokenPlacementAuthority spaces,
            ComponentDefinitionCatalog catalog,
            TokenInstanceId outgoing,
            PlacedToken incoming)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (exchange == null) throw new ArgumentNullException(nameof(exchange));
            if (spaces == null) throw new ArgumentNullException(nameof(spaces));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));

            if (policy.Observation != ReplaceObservationPolicy.ReplacementOnly)
            {
                return ReplaceOutcome.Reject(ReplaceRejectionReason.UnsupportedObservation);
            }

            PlacedToken replaced = null;
            foreach (var token in spaces.PlacedTokens)
            {
                if (token.Id.Equals(outgoing))
                {
                    replaced = token;
                    break;
                }
            }

            if (replaced == null)
            {
                return ReplaceOutcome.Reject(ReplaceRejectionReason.OutgoingNotOnBoard);
            }

            if (replaced.Definition.Reference.Equals(incoming.Definition.Reference))
            {
                return ReplaceOutcome.Reject(ReplaceRejectionReason.IncomingNotDifferent);
            }

            if (!incoming.Coordinate.Equals(replaced.Coordinate))
            {
                // The substitute stands where the replaced object stood. A different space would make
                // this a removal and a placement, which the declared observation policy denies.
                return ReplaceOutcome.Reject(ReplaceRejectionReason.ReplacementUnavailable);
            }

            if (catalog.CountAvailableTokens(incoming.Definition.Reference, spaces.PlacedTokens) < 1)
            {
                return ReplaceOutcome.Reject(ReplaceRejectionReason.SupplyEmpty);
            }

            var committed = exchange.CommitValidatedTokenReplacement(outgoing, incoming);
            return committed.Accepted
                ? ReplaceOutcome.Accept(replaced, incoming, replaced.Coordinate)
                : ReplaceOutcome.Reject(ReplaceRejectionReason.ReplacementUnavailable);
        }
    }
}
