using GuardsDigital.Core.Components;

namespace GuardsDigital.Core.Rules.Displacement
{
    /// <summary>
    /// What kind of piece a Push may move.
    ///
    /// Rulebook p.18 states the rule's own breadth in its own words - "when a unit or a token is
    /// pushed, move it in a straight line directly away from the pushing unit" - and its glossary
    /// heading, `Push (Unit / Token)`, says the same thing twice. A narrower card states its own
    /// narrower kind, which is the division
    /// <see cref="GuardsDigital.Core.Rules.BoardObjects.SwapEligiblePieceKind"/> already draws for a
    /// Swap and <see cref="GuardsDigital.Core.Rules.BoardObjects.RemoveEligiblePieceKind"/> for a
    /// Remove.
    ///
    /// The unrestricted value is deliberately not called `GamePiece`. That name quotes p.19's "two
    /// objects on the board", which is the Swap's sentence and not this one; naming this kind after
    /// it would attribute to p.18 a breadth the page does not state.
    ///
    /// It is an intrinsic representation question, exactly as the other two are: it asks what is
    /// being pushed, so no <c>Counts As</c> is consulted anywhere it is read, and a hero counting as
    /// a minion is still a hero here. Immunity stays a separate question asked separately, because
    /// p.18 makes that one a rule about what may be affected rather than about what a card names.
    /// </summary>
    public enum PushEligiblePieceKind
    {
        None = 0,

        /// <summary>p.18's own breadth: a unit or a token.</summary>
        UnitOrToken = 1,

        /// <summary>
        /// A unit only - a hero or a minion miniature. p.19 states that tokens are not units, which
        /// is the kind card text means when it says unit rather than object.
        /// </summary>
        Unit = 2
    }

    /// <summary>
    /// The one statement of which pieces a declared Push kind admits.
    ///
    /// One statement rather than one per caller, and it is the whole of milestone 6.4c: the target
    /// finders are the only readers, and every entry point reaches a finder, so a probe that reported
    /// a piece pushable and an operation that then refused it is a disagreement this shape cannot
    /// express. Milestone 6.6a found exactly that disagreement in <c>RemoveHandler</c>, where the
    /// eligibility reading had been written twice, and milestone 6.8l is why it matters: a consumer's
    /// own answer is the offer a player is shown.
    /// </summary>
    public static class PushEligibility
    {
        public static bool Admits(PushEligiblePieceKind eligiblePiece, BoardObjectKind kind)
        {
            switch (eligiblePiece)
            {
                case PushEligiblePieceKind.UnitOrToken:
                    return kind == BoardObjectKind.Hero || kind == BoardObjectKind.Minion ||
                        kind == BoardObjectKind.Token;
                case PushEligiblePieceKind.Unit:
                    return kind == BoardObjectKind.Hero || kind == BoardObjectKind.Minion;
                default:
                    return false;
            }
        }
    }
}
