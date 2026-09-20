using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's vanishing Block - the Defense-action text Poof!, Vanish and Ruse print, and
    /// nothing else. Milestone 8.61b.
    ///
    /// <para><b>The printed sentences.</b> Poof! and Vanish: "Swap with a [Smoke bomb] Smoke bomb token
    /// in range; if you do, block the attack." Ruse: the same, and then "You may place the Smoke bomb
    /// into a space in range."</para>
    ///
    /// <para><b>Only one clause of those sentences is composed as card text, and that is the whole
    /// design.</b> "If you do, block the attack" is not a step - it is rulebook p.14's Block condition,
    /// which the card declares as
    /// <see cref="PrintedBlockCondition.TheActorsTokenNamedByTheDefenseTextIsWithinTheCardsRange"/> and
    /// the page resolves at its step 6. What runs here is the swap, at p.14's step 4 ("Defender applies
    /// Defense action text, if any"), and Ruse's re-place after it.</para>
    ///
    /// <para><b>The condition and the swap are one fact, not two that agree.</b> The swap is mandatory -
    /// neither of the two words that would make it a choice is printed on any of the three faces - so it
    /// happens exactly when her bomb is inside the card's printed Range, which is exactly when the
    /// condition holds. <c>DefenseBlockQuery</c> reads that very node's own declaration to answer the
    /// condition (see <c>OwnedTokenReachQuery</c>), so the defence preview, the resolution and the swap
    /// are one reading reached from three places. Nothing here gates anything on the verdict, because
    /// p.14's step 4 applies the defender's text whatever step 6 will decide - the behaviour this build
    /// has had since milestone 8.23 and which <c>BoardConditionedBlockTests</c> measures.</para>
    ///
    /// <para><b>Nothing here is registered.</b> The sub-graphs are 8.61a's, whose handlers are deferred
    /// until 8.61j; no configured match can start them.</para>
    /// </summary>
    public static partial class MinEffectContent
    {
        /// <summary>
        /// The node prefix her Block's swap is composed under. It is card-local - every one of the three
        /// faces composes its own definition - so the same three node ids appear on all three, which is
        /// what a restored pause compares against the definition it was taken from.
        /// </summary>
        private const string VanishingSwapPrefix = "vanishing-swap";

        /// <summary>The node prefix Ruse's optional re-place is composed under.</summary>
        private const string VanishingRePlacePrefix = "vanishing-replace";

        /// <summary>The root of Ruse's two-clause body; Poof! and Vanish root on the swap itself.</summary>
        private const string VanishingRootId = "vanishing-root";

        /// <summary>The `may` Ruse's second sentence prints, wrapped around the re-place.</summary>
        private const string VanishingRePlaceMayId = "vanishing-replace-may";

        /// <summary>
        /// The Defense-action text of one of her three Block faces.
        ///
        /// <para><b>Poof! and Vanish are the swap and nothing else</b>, so their body is 8.61a's
        /// swap-in-range pair rooted directly: a bounded owned-token fact publishing where her one bomb
        /// stands when it stands inside this card's printed Range, and p.19's Swap consuming it. A bomb
        /// out of range, reclaimed earlier or never placed publishes nothing, and the mandatory pair is
        /// then p.10's impossible step - which is the same board on which the Block condition is
        /// unfulfilled and p.14 defeats her.</para>
        ///
        /// <para><b>Ruse adds a sentence and the ordering is printed.</b> "You may place the Smoke bomb
        /// into a space in range" follows the swap in a Sequence, so p.10's stop does the gating that
        /// "the Smoke bomb" needs: a swap that could not happen ends the sequence, and the re-place -
        /// whose subject is "the Smoke bomb", the piece just swapped with - never runs on a board where
        /// there was no such piece. Its destination is measured from where Min is standing AFTER the
        /// swap, because that is where she is when the sentence is read; 8.61a's re-place reads the
        /// bomb's own position unbounded and bounds the DESTINATION, which is what the printed clause
        /// bounds.</para>
        /// </summary>
        public static EffectDefinition VanishingBlock(
            CardDefinitionId card, string revision, bool mayRePlaceTheBomb)
        {
            var swap = SwapWithSmokeBombInRange(VanishingSwapPrefix);
            if (!mayRePlaceTheBomb)
            {
                return new EffectDefinition(
                    new EffectDefinitionId(card.Value + "-defense"),
                    revision,
                    SubGraphRootNodeId(VanishingSwapPrefix),
                    swap);
            }

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    VanishingNode(VanishingRootId),
                    EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory,
                    childNodeIds: new[]
                    {
                        SubGraphRootNodeId(VanishingSwapPrefix),
                        VanishingNode(VanishingRePlaceMayId)
                    }),

                // p.10's `may`, answered by Min herself: she is the Defense action's actor, so the
                // choice inside her own card's text is hers. Declining continues the action, which on a
                // Block that has already held means the attack stays blocked and the bomb stays where
                // the swap left it.
                new EffectNodeDefinition(
                    VanishingNode(VanishingRePlaceMayId),
                    EffectNodeKind.Optional,
                    EffectFailurePolicy.May,
                    childNodeIds: new[] { SubGraphRootNodeId(VanishingRePlacePrefix) },
                    chooser: EffectChooserRole.Actor)
            };
            nodes.AddRange(swap);
            nodes.AddRange(PlaceSmokeBombInRange(VanishingRePlacePrefix));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-defense"),
                revision,
                VanishingNode(VanishingRootId),
                nodes);
        }

        /// <summary>
        /// Which of p.14's moments a Block's composed text is applied at. The page numbers the
        /// defender's own text step 4 - "Defender applies Defense action text, if any" - and that is the
        /// only moment a Defense action has, which milestone 8.40g settled for every Block before these
        /// three.
        /// </summary>
        public static CardTextTiming VanishingDefenseTextTimingFor(CardDefinitionId cardId) =>
            MinCardContent.VanishingCards().Any(candidate => candidate.Equals(cardId))
                ? CardTextTiming.DefenseAction
                : CardTextTiming.None;

        /// <summary>
        /// The one decision window her three Block faces open - Ruse's "You may place the Smoke bomb
        /// into a space in range", and nothing else.
        ///
        /// <para>Poof! and Vanish declare NOTHING, and that is the printed truth rather than an
        /// omission: their whole body is one mandatory swap, which p.10 either performs or stops at.
        /// Ruse's own swap is the same mandatory node, so the only control any of the three raises is
        /// the <c>may</c> around the re-place. The Place underneath it asks for a SPACE rather than a
        /// step, which is a board selection and not a confirm-or-decline.</para>
        ///
        /// <para>Milestone 8.61b composed these three faces and recorded this table as a debt for the
        /// dealing, for the reason every contribution to <c>DecisionLabelCatalog</c> joins at a
        /// dealing milestone: what a table is FOR is a window over a card a match deals.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> VanishingDecisionLabels()
        {
            yield return DecisionLabelDeclaration.DecisionStep(
                MinCardContent.Ruse, VanishingRePlaceMayId,
                "Throw the smoke bomb again", "Leave the smoke bomb");
        }

        private static EffectNodeId VanishingNode(string id) => new EffectNodeId(id);
    }
}
