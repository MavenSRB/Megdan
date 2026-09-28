using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.114b. Wuk's five Red faces.
    ///
    /// <para><b>The Nature's Attacks</b> - Nature's Protector, Nature's Guardian: "Choose one - Target a
    /// hero adjacent to you. / Target a unit in range adjacent to a Tree token." Nature's Champion
    /// prints "Choose one, or both, on different targets". Each is p.10's modal over two
    /// <c>attack-sequence@1</c> bullets, composed as Brynn's Familiar Ground and Bain's Hunter-Seeker
    /// are: the card-action authority runs a complete p.14 for every bullet taken, each bullet's
    /// target set derived from its own printed sentence when its sequence starts. The second
    /// bullet's clause is <see cref="AttackSequenceSubjectNarrowing.AdjacentToATokenTheActorOwns"/>,
    /// milestone 8.88's ownership reading of "a Tree token" (Wuk owns no other kind), measured
    /// around the candidate within the printed Range with p.16's items. Champion's two sequences
    /// are two full p.14 Attacks on different targets - Dodger's D-06 and Bain's composition.</para>
    ///
    /// <para><b>The trampling moves</b> - Trample and Angry Stampede - are composed by this block's
    /// second half.</para>
    ///
    /// <para>Nothing here is registered until 8.114f deals him.</para>
    /// </summary>
    public static class WukRedFaces
    {
        public static CardDefinitionId NaturesProtector { get; } = WukCardContent.Id("natures-protector");
        public static CardDefinitionId NaturesGuardian { get; } = WukCardContent.Id("natures-guardian");
        public static CardDefinitionId Trample { get; } = WukCardContent.Id("trample");
        public static CardDefinitionId NaturesChampion { get; } = WukCardContent.Id("natures-champion");
        public static CardDefinitionId AngryStampede { get; } = WukCardContent.Id("angry-stampede");

        public const string NatureText =
            "Choose one - • Target a hero adjacent to you. • Target a unit in range adjacent to a " +
            "Tree token.";

        public const string ChampionText =
            "Choose one, or both, on different targets - • Target a hero adjacent to you. • Target a " +
            "unit in range adjacent to a Tree token.";

        public const string NatureModalNode = "nature-modal";
        public const string NatureAdjacentBulletNode = "nature-adjacent-bullet";
        public const string NatureTreeBulletNode = "nature-tree-bullet";

        /// <summary>The three Nature's Attacks this half of the block composes, in inventory order.</summary>
        public static IReadOnlyList<CardDefinition> AttackDefinitions => LazyAttacks.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAttacks =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Nature(NaturesProtector, "Nature's Protector", CardTier.I, 7, 7, 5, 2,
                    PrintedValueChannel.None, new[] { NaturesGuardian, Trample }, false),
                Nature(NaturesGuardian, "Nature's Guardian", CardTier.II, 7, 8, 5, 3,
                    PrintedValueChannel.Initiative, new[] { NaturesChampion, AngryStampede }, false),
                Nature(NaturesChampion, "Nature's Champion", CardTier.III, 8, 8, 6, 3,
                    PrintedValueChannel.Initiative, Array.Empty<CardDefinitionId>(), true)
            }));

        private static CardDefinition Nature(
            CardDefinitionId card, string title, CardTier tier, int initiative, int defense,
            int attack, int range, PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next,
            bool mayTakeBoth) =>
            new CardDefinition(
                card, WukCardContent.ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Red, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        WukCardContent.PrimaryRangedAttack, ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, attack),
                            new PrintedValueDefinition(PrintedValueChannel.Range, range)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged, default,
                        NatureModal(card, mayTakeBoth),
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        CardTextTiming.BeforeTheAttack,
                        PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged)
                            .BoundByCardText()),
                    WukCardContent.Movement(4),
                    WukCardContent.Defense(defense)
                },
                null, CardSourceContent.Transcribed(mayTakeBoth ? ChampionText : NatureText));

        /// <summary>
        /// The two bullets as p.10's modal. "Choose one" takes one; Champion's "one, or both, on
        /// different targets" takes one or two with the targets kept apart.
        /// </summary>
        private static EffectDefinition NatureModal(CardDefinitionId card, bool mayTakeBoth)
        {
            var modal = new EffectNodeId(NatureModalNode);
            var adjacent = new EffectNodeId(NatureAdjacentBulletNode);
            var tree = new EffectNodeId(NatureTreeBulletNode);
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-attack-text"), WukCardContent.ContentRevision,
                modal,
                new[]
                {
                    new EffectNodeDefinition(
                        modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { adjacent, tree },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: mayTakeBoth ? 2 : 1),

                    // "Target a hero adjacent to you."
                    Bullet(adjacent, AttackTargetableUnits.HeroesOnly, AttackTargetReach.AdjacentOnly,
                        AttackSequenceSubjectNarrowing.None, mayTakeBoth),

                    // "Target a unit in range adjacent to a Tree token."
                    Bullet(tree, AttackTargetableUnits.HeroesAndMinions, AttackTargetReach.PrintedRange,
                        AttackSequenceSubjectNarrowing.AdjacentToATokenTheActorOwns, mayTakeBoth)
                });
        }

        /// <summary>
        /// One bullet as an <c>attack-sequence@1</c> leaf publishing its printed targeting sentence -
        /// Brynn's and Bain's bullet shape.
        /// </summary>
        private static EffectNodeDefinition Bullet(
            EffectNodeId nodeId,
            AttackTargetableUnits units,
            AttackTargetReach reach,
            AttackSequenceSubjectNarrowing narrowing,
            bool distinctTargets)
        {
            var inputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.TargetableUnitsParameter, units),
                EffectInputBinding.FromClosedVocabulary(SyntheticSequenceCapabilities.ReachParameter, reach),
                EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.LineParameter, AttackTargetLine.AnyLine),
                EffectInputBinding.FromLiteral(
                    SyntheticSequenceCapabilities.DistinctTargetsParameter,
                    EffectValue.Boolean(distinctTargets))
            };
            var outputs = new List<EffectOutputBinding>
            {
                new EffectOutputBinding(
                    SyntheticSequenceCapabilities.TargetableUnitsOutput, EffectValueKind.Integer),
                new EffectOutputBinding(SyntheticSequenceCapabilities.ReachOutput, EffectValueKind.Integer),
                new EffectOutputBinding(SyntheticSequenceCapabilities.LineOutput, EffectValueKind.Integer),
                new EffectOutputBinding(
                    SyntheticSequenceCapabilities.DistinctTargetsOutput, EffectValueKind.Boolean)
            };
            if (narrowing != AttackSequenceSubjectNarrowing.None)
            {
                inputs.Add(EffectInputBinding.FromClosedVocabulary(
                    SyntheticSequenceCapabilities.SubjectNarrowingParameter, narrowing));
                outputs.Add(new EffectOutputBinding(
                    SyntheticSequenceCapabilities.SubjectNarrowingOutput, EffectValueKind.Integer));
            }

            return new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticSequenceCapabilities.AttackSequenceV1,
                inputs: inputs,
                outputs: outputs);
        }
    }
}
