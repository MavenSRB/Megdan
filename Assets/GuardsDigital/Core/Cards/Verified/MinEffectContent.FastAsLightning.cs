using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.61g. Min the Dragonmonk's borrowed rider - the composed body behind Fast as
    /// Lightning's printed "After the attack:" clause, and the block's only effect content.
    ///
    /// <para>Her other faces' bodies belong to the blocks that own their clauses; this partial declares
    /// exactly one, and 8.61i composes the set.</para>
    /// </summary>
    public static partial class MinEffectContent
    {
        /// <summary>
        /// "Apply the \"After the attack\" text of your resolved or discarded red card. (If it has
        /// radius, use that card's value)."
        ///
        /// <para><b>One node, and that is the whole sentence.</b> The clause names a card and applies
        /// its text; it chooses nothing else, it is not conditional, and it prints no "may", no "up to"
        /// and no "if able" - so the node is mandatory, and a round in which her Red card is still in
        /// hand or committed-but-unresolved leaves the step impossible in rulebook p.10's sense. An
        /// impossible step inside p.14's step 7 stops that text and nothing else, so the Attack that
        /// carried it keeps its value, its target and its outcome: MI-Q7 reads the clause as a rider
        /// that sometimes has nothing to apply, never as a condition on the Attack.</para>
        ///
        /// <para><b>The parenthesis is not composed as a second node</b>, because it is not a second
        /// instruction. "(If it has radius, use that card's value)" says which card's printed Radius
        /// icon the borrowed body's own "in radius" reads, and the borrowed body is applied with the
        /// borrowed card as its source - so <c>synthetic-card-radius@1</c> reads that card's icon with
        /// rulebook p.16's items already, and a node here would be restating what the application
        /// decides. See <see cref="BorrowedCardTextDeclaration"/> for the whole argument.</para>
        ///
        /// <para><b>The node is the last thing the body does</b>, which
        /// <see cref="GuardsDigital.Core.Rules.EffectResolution.BorrowedCardTextQuery"/> records as a
        /// requirement rather than an accident: the borrowed text is applied after the borrowing text
        /// finishes, so a clause with work of its own after the borrow would be printing a different
        /// sentence than this one.</para>
        /// </summary>
        public static EffectDefinition BorrowedAfterAttackRider(
            CardDefinitionId card, string revision)
        {
            var node = new EffectNodeId("min-borrow-after-attack");
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-after-attack"),
                revision,
                node,
                new[]
                {
                    new EffectNodeDefinition(
                        node,
                        EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticBorrowedCardTextCapabilities.BorrowAfterAttackTextV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticBorrowedCardTextCapabilities.PoolParameter,
                                BorrowedCardTextPool.OwnedResolvedOrDiscardedCards),
                            // "your ... RED card" - p.9's colour, named rather than left open. Both of
                            // her Gold faces print the same narrowing, and it is what makes a borrow of
                            // a Gold face off a Gold face structurally unreachable.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticBorrowedCardTextCapabilities.ColorParameter, CardColor.Red)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticBorrowedCardTextCapabilities.BorrowedCardOutput,
                                EffectValueKind.CardInstance)
                        })
                });
        }
    }
}
