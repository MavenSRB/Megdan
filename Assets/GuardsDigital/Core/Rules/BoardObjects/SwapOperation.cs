using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Rules.BoardObjects
{
    /// <summary>
    /// What kind of piece a Swap may exchange.
    ///
    /// p.19's own answer is any object - "Two objects on the board swap places with each other" - and
    /// a narrower rule states its own narrower kind, which is the same division
    /// <see cref="RemoveEligiblePieceKind"/> draws for a Remove. GamePiece asks for intrinsic
    /// representation. Unit also admits a token temporarily counting as a minion during the
    /// applicable action; outside that scope it is still only a token.
    ///
    /// Both objects are judged by it rather than only the one a definition happens to name second,
    /// for the reason the calling capability asks immunity about both: a Swap affects each object as
    /// much as the other.
    /// </summary>
    public enum SwapEligiblePieceKind
    {
        None = 0,

        /// <summary>p.19's unrestricted "two objects on the board".</summary>
        GamePiece = 1,

        /// <summary>
        /// A unit only - a hero or a minion, including a token made to count as a minion by an
        /// applicable effect. An ordinary token is not a unit.
        /// </summary>
        Unit = 2
    }

    /// <summary>Why a Swap was refused. A refusal is a rules answer, never an engine fault.</summary>
    public enum SwapRejectionReason
    {
        None = 0,

        /// <summary>One of the two named objects is not on the board.</summary>
        SubjectNotOnBoard = 1,

        /// <summary>Both names resolve to the same object; p.19 requires two.</summary>
        SameObject = 2,

        /// <summary>The board refused the exchange between derivation and commit.</summary>
        ExchangeUnavailable = 3,

        /// <summary>One of the two objects is not the kind of piece this Swap declared it may exchange.</summary>
        IneligiblePiece = 4
    }

    /// <summary>
    /// The typed outcome of one Swap: both objects and both spaces, and nothing else.
    ///
    /// It carries no movement, placement, route, or distance vocabulary, because p.19 says outright
    /// that a Swap "does not count as movement or placement". It also carries no Lane-Push
    /// qualification, and that absence is the milestone's fourth acceptance check stated structurally
    /// rather than as a case list: p.8 rules that "a Lane Push can never be triggered by moving,
    /// pushing, or swapping a minion", so there is no member here for one to be recorded in.
    /// </summary>
    public readonly struct SwapOutcome
    {
        private SwapOutcome(
            bool accepted,
            SwapRejectionReason rejectionReason,
            BoardObjectReference first,
            BoardObjectReference second,
            HexCoordinate firstOrigin,
            HexCoordinate secondOrigin)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            First = first;
            Second = second;
            FirstOrigin = firstOrigin;
            SecondOrigin = secondOrigin;
        }

        public bool Accepted { get; }
        public SwapRejectionReason RejectionReason { get; }
        public BoardObjectReference First { get; }
        public BoardObjectReference Second { get; }

        /// <summary>Where the first object stood before the exchange; the second stands there after it.</summary>
        public HexCoordinate FirstOrigin { get; }

        /// <summary>Where the second object stood before the exchange; the first stands there after it.</summary>
        public HexCoordinate SecondOrigin { get; }

        internal static SwapOutcome Accept(
            BoardObjectReference first,
            BoardObjectReference second,
            HexCoordinate firstOrigin,
            HexCoordinate secondOrigin) =>
            new SwapOutcome(true, SwapRejectionReason.None, first, second, firstOrigin, secondOrigin);

        internal static SwapOutcome Reject(SwapRejectionReason reason)
        {
            if (reason == SwapRejectionReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason), reason, "A refused Swap requires a reason.");
            }

            return new SwapOutcome(false, reason, default, default, default, default);
        }
    }

    /// <summary>
    /// Rulebook p.19's Swap, as one operation every caller shares.
    ///
    /// "Two objects on the board swap places with each other. This does not count as movement or
    /// placement."
    ///
    /// Both positions are read from one pre-effect state and the exchange is one commit. That ordering
    /// is the milestone's first acceptance check and it is not merely stylistic: resolving the second
    /// object's space after relocating the first would resolve it to the space the first just vacated,
    /// which is how a Swap written as two moves silently becomes a move onto itself.
    ///
    /// The resulting occupancies need no legality question of their own, and that is a property of the
    /// operation rather than an omission: each object is entering a space the other is standing in, so
    /// both are playable and each is vacated by exactly the piece arriving in the other. There is no
    /// empty destination to find, which is precisely why this cannot reuse the displacement commit.
    ///
    /// Immunity is asked by the calling capability rather than here, exactly as milestone 6.5's Place
    /// asks it while declaring its subject. p.18 makes immunity a targeting rule addressed to the
    /// activity doing the targeting, and this operation knows neither the activity nor the acting unit.
    /// </summary>
    public static class SwapOperation
    {
        public static SwapOutcome Swap(
            SwapEligiblePieceKind eligiblePiece,
            IBoardObjectExchangeAuthority exchange,
            IBoardDisplacementAuthority board,
            BoardObjectReference first,
            BoardObjectReference second,
            RuleModificationSet modifications)
        {
            if (exchange == null) throw new ArgumentNullException(nameof(exchange));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (eligiblePiece == SwapEligiblePieceKind.None)
            {
                throw new ArgumentException(
                    "A Swap declares which kind of piece it may exchange.", nameof(eligiblePiece));
            }

            if (!Admits(eligiblePiece, first, modifications) ||
                !Admits(eligiblePiece, second, modifications))
            {
                return SwapOutcome.Reject(SwapRejectionReason.IneligiblePiece);
            }

            if (first.Equals(second))
            {
                return SwapOutcome.Reject(SwapRejectionReason.SameObject);
            }

            // One pre-effect reading of both positions, before anything is committed.
            if (!board.TryGetObjectCoordinate(first, out var firstOrigin) ||
                !board.TryGetObjectCoordinate(second, out var secondOrigin))
            {
                return SwapOutcome.Reject(SwapRejectionReason.SubjectNotOnBoard);
            }

            var committed = exchange.CommitValidatedSwap(first, second);
            return committed.Accepted
                ? SwapOutcome.Accept(first, second, firstOrigin, secondOrigin)
                : SwapOutcome.Reject(SwapRejectionReason.ExchangeUnavailable);
        }

        /// <summary>
        /// Whether the declared eligible kind admits this piece. Public because the calling capability
        /// has to reach the same answer without exchanging anything: a probe that read eligibility
        /// differently from the operation would report a piece exchangeable and then refuse it, which
        /// is exactly the disagreement milestone 6.8l's narrowing reads as an offer.
        /// </summary>
        public static bool Admits(SwapEligiblePieceKind eligiblePiece, BoardObjectKind kind)
        {
            switch (eligiblePiece)
            {
                case SwapEligiblePieceKind.GamePiece:
                    return kind == BoardObjectKind.Hero || kind == BoardObjectKind.Minion ||
                        kind == BoardObjectKind.Token;
                case SwapEligiblePieceKind.Unit:
                    return kind == BoardObjectKind.Hero || kind == BoardObjectKind.Minion;
                default:
                    return false;
            }
        }

        /// <summary>
        /// A token that temporarily counts as a minion is also a unit for an action saying
        /// "unit". The intrinsic overload remains for representation-only callers.
        /// </summary>
        public static bool Admits(
            SwapEligiblePieceKind eligiblePiece,
            BoardObjectReference piece,
            RuleModificationSet modifications) =>
            Admits(eligiblePiece, piece.Kind) ||
            (eligiblePiece == SwapEligiblePieceKind.Unit &&
             piece.Kind == BoardObjectKind.Token &&
             EffectiveUnitKindQuery.Is(
                 RuleObject.ForToken(piece.TokenInstanceId),
                 EffectiveUnitKind.Minion, modifications));
    }
}
