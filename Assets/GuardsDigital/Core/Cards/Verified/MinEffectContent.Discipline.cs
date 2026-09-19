using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's two Green Skill faces as typed effect definitions - Inner Strength and
    /// Perfect Self, milestone 8.61h.
    ///
    /// <para>It sits beside <c>MinCardContent</c> for the reason every hero's pair does: that file is
    /// the transcription and this one is the composition. A reader auditing what the cards say reads
    /// one; a reader auditing what the engine does with it reads the other.</para>
    ///
    /// <para><b>Both faces compose against declarations rather than against new machinery.</b> "This
    /// round: Double your item bonuses" is one Active-effect creation whose whole content is the
    /// <see cref="GuardsDigital.Core.Rules.Modifications.RuleModificationKind.ItemBonusMultiplier"/>
    /// declaration <c>MinComponentContent</c> carries; "Take a Tier II card from your deck and add it
    /// to your dashboard as a permanent item" is one performance of
    /// <c>take-deck-card-as-item@1</c>, whose Tier is the one number the sentence prints. Neither node
    /// reads a card name at runtime.</para>
    /// </summary>
    public static partial class MinEffectContent
    {
        /// <summary>
        /// Milestone 8.46a. What Perfect Self's own decision control says - the consequence in the
        /// printed text's own words, never the mechanism.
        ///
        /// <para>Inner Strength declares none: its Skill is one mandatory step with nothing to choose,
        /// so it opens no decision window at all.</para>
        ///
        /// <para>The member is named for this block rather than <c>DecisionLabels</c> because five
        /// parallel partials of <see cref="MinEffectContent"/> are being written at once; milestone
        /// 8.61i folds them into the set's own single declaration.</para>
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> DisciplineDecisionLabels()
        {
            yield return DecisionArm(
                MinCardContent.PerfectSelf, PerfectSelfDoubleArmNode,
                "This round: Double your item bonuses");
            yield return DecisionArm(
                MinCardContent.PerfectSelf, PerfectSelfTakeArmNode,
                "Take a Tier II card from your deck as a permanent item");
        }

        /// <summary>Perfect Self's first bullet, named so its decision label and its node agree.</summary>
        public const string PerfectSelfDoubleArmNode = "perfect-self-double";

        /// <summary>Perfect Self's second bullet.</summary>
        public const string PerfectSelfTakeArmNode = "perfect-self-take";

        /// <summary>
        /// "<b>This round:</b> Double your item bonuses." - Inner Strength, whole.
        ///
        /// <para>One mandatory node, because the printed sentence has no choice, no subject and no
        /// number the effect tree could carry: the scope, the duration, the expiry and the multiplier
        /// are all definition content on the Active effect itself. An effect tree with a branch in it
        /// would be a shape the card does not have.</para>
        /// </summary>
        public static EffectDefinition InnerStrengthSkillEffect(string contentRevision)
        {
            var root = new EffectNodeId("inner-strength-double");
            return new EffectDefinition(
                new EffectDefinitionId("min-inner-strength-skill-effect"),
                contentRevision,
                root,
                new[] { DoubleItemBonusesNode(root, MinComponentContent.InnerStrengthDoubleItemBonusesV1) });
        }

        /// <summary>
        /// "Choose one or both - (1) <b>This round:</b> Double your item bonuses. (2) Take a Tier II
        /// card from your deck and add it to your dashboard as a permanent item." - Perfect Self.
        ///
        /// <para><b>"Choose one or both" is p.10's modal cardinality written out</b>: minimum one,
        /// maximum two, chooser the actor, because p.10 hands an unassigned decision to the hero
        /// performing the card. Dodger's Middlefinger of Death is the roster's precedent for the exact
        /// phrase, and p.10 adds that chosen bullets are applied in any order the chooser likes - which
        /// the <see cref="EffectNodeKind.Choose"/> node already is.</para>
        ///
        /// <para><b>Neither bullet is a repeat of the other and neither gates the other.</b> Taking
        /// both is two independent steps: the doubling is an Active effect on Min, and the deck card
        /// becomes an item whose own icon the doubling then raises for the rest of the round, which is
        /// the interaction the card buys and not a rule either bullet states.</para>
        ///
        /// <para><b>The second bullet is mandatory INSIDE its own arm.</b> The choice is whether to
        /// take the bullet at all; once taken, "Take a Tier II card" has no "you may" and no "if able"
        /// in it. A deck holding no Tier II card makes the arm an impossible step, which p.10 skips -
        /// and the capability reports it as such before the modal is offered, so the bullet is not
        /// presented rather than presented and then refused.</para>
        /// </summary>
        public static EffectDefinition PerfectSelfSkillEffect(string contentRevision)
        {
            var modal = new EffectNodeId("perfect-self-mode");
            var doubleArm = new EffectNodeId(PerfectSelfDoubleArmNode);
            var takeArm = new EffectNodeId(PerfectSelfTakeArmNode);

            return new EffectDefinition(
                new EffectDefinitionId("min-perfect-self-skill-effect"),
                contentRevision,
                modal,
                new[]
                {
                    new EffectNodeDefinition(
                        modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { doubleArm, takeArm },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 2),

                    DoubleItemBonusesNode(
                        doubleArm, MinComponentContent.PerfectSelfDoubleItemBonusesV1),

                    new EffectNodeDefinition(
                        takeArm, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.TakeDeckCardAsItemV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole(
                                "hero", EffectRoleKind.Actor, EffectValueKind.Hero),

                            // "a Tier II card" - the one number this bullet prints, bound through the
                            // closed vocabulary p.15's Tiers already are.
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.ItemCardTierParameter, CardTier.II)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("hero", EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.TakenItemCardOutput,
                                EffectValueKind.CardInstance),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.GrantedItemOrderOutput,
                                EffectValueKind.Integer)
                        })
                });
        }

        /// <summary>
        /// The doubling as one node. Written once because both faces print the same sentence and each
        /// names its own capability - see <c>MinComponentContent</c> for why there are two.
        /// </summary>
        private static EffectNodeDefinition DoubleItemBonusesNode(
            EffectNodeId nodeId, EffectCapabilityReference capability) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                capability,
                outputs: new[]
                {
                    new EffectOutputBinding(
                        ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                });
    }
}
