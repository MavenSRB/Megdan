using System;
using GuardsDigital.Core.Components;

namespace GuardsDigital.Core.Rules.BoardObjects
{
    /// <summary>
    /// What kind of piece a Remove may take off the board.
    ///
    /// p.19's own general answer is any game piece - "Remove a game piece (such as a miniature, or a
    /// token) from the board" - and a narrower rule states its own narrower kind. This is an intrinsic
    /// representation question, like <c>PlaceEligiblePieceKind</c>: it asks which miniature is being
    /// picked up, so no <c>Counts As</c> is consulted anywhere it is used.
    /// </summary>
    public enum RemoveEligiblePieceKind
    {
        None = 0,

        /// <summary>p.19's unrestricted "such as a miniature, or a token".</summary>
        GamePiece = 1,

        /// <summary>A minion miniature only.</summary>
        MinionMiniature = 2,

        /// <summary>A token only, which is the kind p.13's Clear replacement already removes.</summary>
        Token = 3
    }

    /// <summary>
    /// Where a removed piece goes. p.19 says a Remove takes the piece "from the board" without saying
    /// where it lands, because that depends on what the piece is, and
    /// <c>CARD_EFFECT_CAPABILITIES.md</c> requires the destination to be recorded rather than implied:
    /// "It records the destination (supply, pool, or other defined location)".
    /// </summary>
    public enum RemoveDestination
    {
        None = 0,

        /// <summary>
        /// The component supply the piece was taken from. A token rejoins its finite token supply
        /// (p.19: "tokens are intentionally limited") and a minion miniature rejoins the reserve p.19's
        /// Respawn ordering depends on - "a minion must first be removed, before it can be respawned".
        /// </summary>
        Supply = 1
    }

    /// <summary>
    /// The complete declaration of one Remove operation: the kind of piece it may take and where that
    /// piece goes.
    ///
    /// It exists as one object rather than two arguments for the reason <c>PlacePolicy</c> does: those
    /// two are what <c>CARD_EFFECT_CAPABILITIES.md</c> requires a removal to state, and a Remove that
    /// fails to state one cannot be constructed at all.
    ///
    /// It deliberately declares no reward, no Defeat stage, and no prevention source. p.19 states the
    /// first outright - "Do not collect any coins" - and the second and third belong to p.13's Defeat,
    /// which this operation is kept distinct from. A Remove is also not p.13's Clear: Clear is an
    /// optional replacement for an entire Attack action that removes a chosen subset of adjacent
    /// tokens through this same kind of removal, so Clear is a caller and never a mode.
    /// </summary>
    public sealed class RemovePolicy
    {
        public RemovePolicy(RemoveEligiblePieceKind eligiblePiece, RemoveDestination destination)
        {
            if (eligiblePiece == RemoveEligiblePieceKind.None)
            {
                throw new ArgumentException(
                    "A Remove declares which kind of game piece it may take off the board.",
                    nameof(eligiblePiece));
            }

            if (destination == RemoveDestination.None)
            {
                throw new ArgumentException(
                    "A Remove declares where the piece it takes off the board goes.", nameof(destination));
            }

            EligiblePiece = eligiblePiece;
            Destination = destination;
        }

        public RemoveEligiblePieceKind EligiblePiece { get; }
        public RemoveDestination Destination { get; }

        /// <summary>
        /// Whether the declared eligible kind admits this piece. Normally intrinsic representation: the
        /// question is which miniature is being picked up, so no <c>Counts As</c> is consulted. A hero
        /// is admitted by no declared kind, which is why <see cref="RemoveEligiblePieceKind.GamePiece"/>
        /// stops short of p.19's literal breadth - a hero leaving the board is p.13's Defeat with its
        /// consequences, and the exchange port refuses one as well.
        ///
        /// It lives on the declaration rather than inside the operation because the operation is not
        /// its only reader: the calling capability's probe has to reach the same answer without
        /// removing anything, and two readings of one declaration is how a probe comes to report a
        /// piece removable that the operation then refuses.
        /// </summary>
        public bool Admits(BoardObjectKind kind, bool hasModeMinionCategory = false)
        {
            // PVE-D30 explicitly adds minion effects to selected Hero identities; this is not Counts As.
            if (kind == BoardObjectKind.Hero && hasModeMinionCategory)
                kind = BoardObjectKind.Minion;
            switch (EligiblePiece)
            {
                case RemoveEligiblePieceKind.GamePiece:
                    return kind == BoardObjectKind.Minion || kind == BoardObjectKind.Token;
                case RemoveEligiblePieceKind.MinionMiniature:
                    return kind == BoardObjectKind.Minion;
                case RemoveEligiblePieceKind.Token:
                    return kind == BoardObjectKind.Token;
                default:
                    return false;
            }
        }

        public override string ToString() => $"Remove {EligiblePiece} to {Destination}";
    }
}
