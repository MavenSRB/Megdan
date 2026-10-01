using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Rules.Placement
{
    /// <summary>Why a Place was refused. A refusal is a rules answer, never an engine fault.</summary>
    public enum PlaceRejectionReason
    {
        None = 0,

        /// <summary>The subject is not the kind of piece this Place declared it may take.</summary>
        IneligiblePiece = 1,

        /// <summary>The named board piece is not on the board.</summary>
        SubjectNotOnBoard = 2,

        /// <summary>No copy of the supply piece is left to place (p.19: "tokens are intentionally limited").</summary>
        SupplyEmpty = 3,

        /// <summary>The destination is not one this Place's declared query currently admits.</summary>
        DestinationNotAdmitted = 4,

        /// <summary>The board refused the relocation between derivation and commit.</summary>
        DestinationUnavailable = 5,

        /// <summary>The operation asked for a source container this call does not serve.</summary>
        SourceContainerMismatch = 6,

        /// <summary>
        /// The space the caller measured destinations from is not the one this Place declared its
        /// anchor to be. Milestone 8.6: the anchor is content, so a caller and a declaration that
        /// disagree about it are refused rather than quietly resolved in the caller's favour.
        /// </summary>
        DestinationAnchorMismatch = 7
    }

    /// <summary>
    /// The typed outcome of one Place. It names the piece and both spaces, and carries no movement
    /// vocabulary at all: p.18 says placing "does not count as movement", so a caller that publishes a
    /// movement fact from this would be stating a rule the operation never performed.
    /// </summary>
    public readonly struct PlaceOutcome
    {
        private PlaceOutcome(
            bool accepted,
            PlaceRejectionReason rejectionReason,
            PlaceSourceContainer sourceContainer,
            BoardObjectReference subject,
            bool hasOrigin,
            HexCoordinate origin,
            HexCoordinate destination)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            SourceContainer = sourceContainer;
            Subject = subject;
            HasOrigin = hasOrigin;
            Origin = origin;
            Destination = destination;
        }

        public bool Accepted { get; }
        public PlaceRejectionReason RejectionReason { get; }
        public PlaceSourceContainer SourceContainer { get; }

        /// <summary>The piece that was placed, including the instance identity a supply Place minted.</summary>
        public BoardObjectReference Subject { get; }

        /// <summary>Whether the piece came off a space. False for a supply Place, which had none.</summary>
        public bool HasOrigin { get; }

        public HexCoordinate Origin { get; }
        public HexCoordinate Destination { get; }

        internal static PlaceOutcome AcceptFromBoard(
            BoardObjectReference subject, HexCoordinate origin, HexCoordinate destination) =>
            new PlaceOutcome(
                true, PlaceRejectionReason.None, PlaceSourceContainer.Board,
                subject, true, origin, destination);

        internal static PlaceOutcome AcceptFromSupply(
            BoardObjectReference subject, HexCoordinate destination) =>
            new PlaceOutcome(
                true, PlaceRejectionReason.None, PlaceSourceContainer.Supply,
                subject, false, default, destination);

        internal static PlaceOutcome Reject(
            PlaceSourceContainer sourceContainer, PlaceRejectionReason reason)
        {
            if (reason == PlaceRejectionReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason), reason, "A refused Place requires a reason.");
            }

            return new PlaceOutcome(false, reason, sourceContainer, default, false, default, default);
        }
    }

    /// <summary>
    /// Rulebook p.18's Place, as one operation every caller shares.
    ///
    /// "Take a game piece (such as a miniature, or a token) from the board or from the supply, and put
    /// it into a different space. A game piece can only be placed into an empty space (this does not
    /// count as movement)."
    ///
    /// Three callers state three different <see cref="PlacePolicy"/> instances against it and none
    /// implements the rule twice: the general Place capability places any game piece from the board,
    /// the token capability places a token from the supply, and milestone 6.4a's after-action
    /// correction places a displaced minion miniature into a nearest empty Battle-Zone space. That
    /// consolidation is milestone 6.5's fifth acceptance check: the correction's minimum Place path was
    /// a direct board commit, and is now this operation.
    ///
    /// The board arm relocates through <see cref="IBoardDisplacementAuthority.CommitValidatedDisplacement"/>,
    /// which is the neutral validated-relocation commit rather than a displacement rule: it publishes
    /// no fact of its own precisely so the calling operation's rule owns what happened. What keeps
    /// Place distinct from a displacement is everything around that commit - the declared source
    /// container, the declared eligible piece, the geometric destination query, p.18's different-space
    /// requirement, and the typed outcome above, which no displacement produces.
    /// </summary>
    public static class PlaceOperation
    {
        /// <summary>
        /// Takes a piece already on the board and puts it into a different empty space.
        ///
        /// The destination is re-derived here rather than trusted from the caller, because a space
        /// legal when a choice opened may have been taken before the answer arrived - the same
        /// discipline every capability resumption follows.
        /// </summary>
        /// <param name="measuredFrom">
        /// The space the destination question is measured from, which must be the one the policy
        /// declares its <see cref="PlacePolicy.DestinationAnchor"/> to be. Milestone 8.6 made it an
        /// argument rather than always the subject's own space, because Wasp's Telekinesis places "into
        /// a space adjacent to you" - a question about where the actor stands, which no radius around
        /// the piece can express - and made it checked rather than trusted, so a caller that passed the
        /// wrong space is refused instead of silently measuring from it.
        /// </param>
        public static PlaceOutcome PlaceFromBoard(
            PlacePolicy policy,
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority spaces,
            BoardObjectReference subject,
            HexCoordinate measuredFrom,
            HexCoordinate destination,
            IReadOnlyList<HexCoordinate> scopedCoordinates,
            RuleModificationSet modifications,
            IUnitSpawnAuthority spawnPoints = null)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (spaces == null) throw new ArgumentNullException(nameof(spaces));

            if (policy.SourceContainer != PlaceSourceContainer.Board)
            {
                return PlaceOutcome.Reject(
                    policy.SourceContainer, PlaceRejectionReason.SourceContainerMismatch);
            }

            if (!IsEligible(policy.EligiblePiece, subject.Kind))
            {
                return PlaceOutcome.Reject(PlaceSourceContainer.Board, PlaceRejectionReason.IneligiblePiece);
            }

            if (!board.TryGetObjectCoordinate(subject, out var origin))
            {
                return PlaceOutcome.Reject(PlaceSourceContainer.Board, PlaceRejectionReason.SubjectNotOnBoard);
            }

            if (policy.DestinationAnchor == PlaceDestinationAnchor.PlacedPiece &&
                !measuredFrom.Equals(origin))
            {
                return PlaceOutcome.Reject(
                    PlaceSourceContainer.Board, PlaceRejectionReason.DestinationAnchorMismatch);
            }

            var admitted = PlaceDestinationQuery.FindDestinations(
                policy, spaces, measuredFrom, scopedCoordinates, modifications, spawnPoints, board,
                subject.AsRuleObject());
            if (!admitted.Contains(destination))
            {
                return PlaceOutcome.Reject(
                    PlaceSourceContainer.Board, PlaceRejectionReason.DestinationNotAdmitted);
            }

            var committed = board.CommitValidatedDisplacement(subject, destination);
            return committed.Accepted
                ? PlaceOutcome.AcceptFromBoard(subject, origin, destination)
                : PlaceOutcome.Reject(
                    PlaceSourceContainer.Board, PlaceRejectionReason.DestinationUnavailable);
        }

        /// <summary>
        /// Takes a token from its finite supply and puts it into an empty space.
        ///
        /// This is the supply arm of p.18's Place, and it serves tokens alone. A miniature taken from
        /// the supply is the same page's Spawn - "similar to place, but unlike 'Place', the spawned
        /// object must be taken from the supply and cannot be taken from the board" - and that is its
        /// own named operation rather than a mode of this one, exactly as
        /// <c>CARD_EFFECT_CAPABILITIES.md</c> keeps Place and Spawn separate identities.
        ///
        /// The origin is genuinely absent rather than defaulted: a supply piece came off no space, and
        /// the different-space requirement therefore has nothing to compare against, which is why the
        /// destination query runs from the space the invoking rule measures from.
        /// </summary>
        public static PlaceOutcome PlaceFromSupply(
            PlacePolicy policy,
            ITokenPlacementAuthority spaces,
            ComponentDefinitionCatalog catalog,
            PlacedToken token,
            HexCoordinate measuredFrom,
            IReadOnlyList<HexCoordinate> scopedCoordinates,
            RuleModificationSet modifications)
        {
            if (policy == null) throw new ArgumentNullException(nameof(policy));
            if (spaces == null) throw new ArgumentNullException(nameof(spaces));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (token == null) throw new ArgumentNullException(nameof(token));

            if (policy.SourceContainer != PlaceSourceContainer.Supply)
            {
                return PlaceOutcome.Reject(
                    policy.SourceContainer, PlaceRejectionReason.SourceContainerMismatch);
            }

            // The supply arm serves tokens alone, so the declaration has to admit one. Asked through
            // the same eligibility statement the board arm uses rather than by listing the kinds that
            // happen to include tokens, which is what let milestone 8.6 add a fourth kind without
            // touching this line.
            if (!IsEligible(policy.EligiblePiece, BoardObjectKind.Token))
            {
                return PlaceOutcome.Reject(
                    PlaceSourceContainer.Supply, PlaceRejectionReason.IneligiblePiece);
            }

            // A piece taken from the supply came off no space, so it cannot be the space its own
            // destinations are measured from.
            if (policy.DestinationAnchor == PlaceDestinationAnchor.PlacedPiece)
            {
                return PlaceOutcome.Reject(
                    PlaceSourceContainer.Supply, PlaceRejectionReason.DestinationAnchorMismatch);
            }

            var admitted = PlaceDestinationQuery.FindDestinations(
                policy, spaces, measuredFrom, scopedCoordinates, modifications);
            if (!admitted.Contains(token.Coordinate))
            {
                return PlaceOutcome.Reject(
                    PlaceSourceContainer.Supply, PlaceRejectionReason.DestinationNotAdmitted);
            }

            if (catalog.CountAvailableTokens(token.Definition.Reference, spaces.PlacedTokens) < 1)
            {
                return PlaceOutcome.Reject(PlaceSourceContainer.Supply, PlaceRejectionReason.SupplyEmpty);
            }

            spaces.CommitValidatedTokenPlacement(token);
            return PlaceOutcome.AcceptFromSupply(
                BoardObjectReference.ForToken(token.Id), token.Coordinate);
        }

        /// <summary>
        /// Whether the declared eligible kind admits this piece. Intrinsic representation only: the
        /// question is which miniature is being picked up, which p.18 states outright for the one rule
        /// that narrows it - "objects that count as minions are unaffected".
        /// </summary>
        private static bool IsEligible(PlaceEligiblePieceKind eligible, BoardObjectKind kind)
        {
            switch (eligible)
            {
                // p.18's "a game piece (such as a miniature, or a token)" and Wasp's "a unit or a
                // token" are two cards' sentences that coincide on this board, where a hero, a minion
                // miniature and a token are the only pieces there are. They share an arm rather than
                // a member, so the definitions keep saying what their own faces say.
                case PlaceEligiblePieceKind.GamePiece:
                case PlaceEligiblePieceKind.UnitOrToken:
                    return kind == BoardObjectKind.Hero ||
                        kind == BoardObjectKind.Minion ||
                        kind == BoardObjectKind.Token;
                case PlaceEligiblePieceKind.MinionMiniature:
                    return kind == BoardObjectKind.Minion;
                case PlaceEligiblePieceKind.Token:
                    return kind == BoardObjectKind.Token;
                default:
                    return false;
            }
        }
    }
}
