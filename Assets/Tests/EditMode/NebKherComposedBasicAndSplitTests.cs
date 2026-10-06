using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Source-linked composition checks for the first 9.53 card slice.</summary>
    public sealed class NebKherComposedBasicAndSplitTests
    {
        [Test]
        public void NebKherDecisionRequirements_AreEnumeratedFromTheDealtGraphs()
        {
            var required = DecisionLabelRequirementQuery.For(
                NebKherCardContent.DealtDefinitions, MatchContentCatalogs.ForThisBuild());
            var declared = NebKherCardContent.DecisionLabels().ToArray();
            string Key(CardDefinitionId card, GuardsDigital.Core.Effects.EffectNodeId node) =>
                card.Value + " / " + node.Value;
            Assert.That(declared.Select(row => Key(row.CardId, row.NodeId))
                    .OrderBy(value => value),
                Is.EqualTo(required.Select(row => Key(row.CardId, row.NodeId))
                    .OrderBy(value => value)));
            foreach (var requirement in required)
            {
                Assert.That(DecisionLabelCatalog.TryGetLabel(
                    requirement.CardId, requirement.NodeId, out var label),
                    Is.True, requirement.ToString());
                Assert.That(label.HasDeclined, Is.EqualTo(requirement.OpensConfirmOrDecline),
                    requirement.ToString());
            }
        }

        [Test]
        public void Ultimate_LaughsOnceAndVisitsEveryEnemyInPrintedRadius()
        {
            var card = NebKherUltimateContent.Definition;
            Assert.That(card.Progression.Classification,
                Is.EqualTo(CardClassification.Ultimate));
            Assert.That(card.Progression.Tier, Is.EqualTo(CardTier.IV));
            Assert.That(card.ActionDefinitions, Is.Empty);
            Assert.That(card.UltimateRule.TriggerCondition,
                Is.EqualTo(UltimateTriggerCondition.AfterControllerLaughsDiabolically));
            Assert.That(card.UltimateRule.PrintedValues.Single().Channel,
                Is.EqualTo(PrintedValueChannel.Radius));
            Assert.That(card.UltimateRule.PrintedValues.Single().Amount, Is.EqualTo(5));
            var effect = NebKherUltimateContent.TriggeredEffect;
            Assert.That(effect.Nodes.Count(node => node.Capability.Equals(
                SyntheticCardStateCapabilities.DiscardChosenCardV2)), Is.EqualTo(6));
            Assert.That(effect.Nodes.Count(node => node.Capability.Equals(
                SyntheticConsequenceCapabilities.DefeatHeroV1)), Is.EqualTo(6));
            Assert.That(EffectDefinitionValidator.Validate(effect, Catalog()), Is.Empty);
        }

        [Test]
        public void DelayedBlueLine_NamesSixColorsAndCarriesChoiceToNextReveal()
        {
            AssertDoubt(NebKherCardContent.ImbueDoubtDefinition, 10, 4, 3,
                CardTier.I, PrintedValueChannel.None);
            AssertDoubt(NebKherCardContent.TimeToReconsiderDefinition, 11, 5, 4,
                CardTier.II, PrintedValueChannel.Attack);
            AssertDoubt(NebKherCardContent.AnIllusionOfChoiceDefinition, 11, 5, 4,
                CardTier.III, PrintedValueChannel.Movement);
            Assert.That(NebKherDelayedDoubtContent.NameableColors,
                Is.EqualTo(new[]
                {
                    CardColor.Gold, CardColor.Silver, CardColor.Red,
                    CardColor.Green, CardColor.Blue, CardColor.Purple
                }));
            Assert.That(NebKherDelayedDoubtContent.TriggeredEffects().Count(),
                Is.EqualTo(18));
            foreach (var triggered in NebKherDelayedDoubtContent.TriggeredEffects())
                Assert.That(EffectDefinitionValidator.Validate(triggered, Catalog()),
                    Is.Empty, triggered.Id.Value);
            foreach (var handler in NebKherDelayedDoubtContent.Handlers())
            {
                var active = handler.ActiveEffectDefinitions.Single();
                Assert.That(active.DurationKind,
                    Is.EqualTo(GuardsDigital.Core.Timing.ActiveEffectDurationKind
                        .NextTurnAfterPlayingCards));
                Assert.That(active.TriggerCheckpoints,
                    Is.EqualTo(new[]
                    {
                        GuardsDigital.Core.Timing.TimingCheckpoint.CardsPlayedAndRevealed
                    }));
                Assert.That(active.TriggeredEffect.HasValue, Is.True);
            }
        }

        private static void AssertDoubt(CardDefinition card, int initiative,
            int defense, int radius, CardTier tier, PrintedValueChannel item)
        {
            AssertSkill(card, initiative, defense, 2, PrintedValueChannel.Radius,
                radius, tier, item);
            var effect = Skill(card).EffectRoot;
            Assert.That(effect.Nodes.Single(node => node.Id.Equals(effect.RootNodeId)).Kind,
                Is.EqualTo(EffectNodeKind.Choose));
            Assert.That(effect.Nodes.Count(node => node.Kind == EffectNodeKind.Capability),
                Is.EqualTo(6));
            Assert.That(EffectDefinitionValidator.Validate(effect, Catalog()), Is.Empty);
        }

        [Test]
        public void DiabolicalLaughter_OffersRealLaughThenThreeOptionalThreeWayChoices()
        {
            var card = NebKherCardContent.DiabolicalLaughterDefinition;
            Assert.That(card.Initiative.Value, Is.EqualTo(3));
            Assert.That(card.Progression.Color, Is.EqualTo(CardColor.Silver));
            Assert.That(card.Progression.Tier, Is.EqualTo(CardTier.None));
            Assert.That(card.ActionDefinitions.Count, Is.EqualTo(2));
            Assert.That(card.ActionDefinitions.Single(action =>
                action.Kind == ActionKind.Defense).BaseValues.Single().Amount,
                Is.EqualTo(3));
            var skill = Skill(card);
            Assert.That(skill.BaseValues.Single().Channel,
                Is.EqualTo(PrintedValueChannel.Radius));
            Assert.That(skill.BaseValues.Single().Amount, Is.EqualTo(4));
            var effect = skill.EffectRoot;
            var root = effect.Nodes.Single(node => node.Id.Equals(effect.RootNodeId));
            Assert.That(root.Kind, Is.EqualTo(EffectNodeKind.Optional));
            Assert.That(effect.Nodes.Count(node => node.Capability.Equals(
                LaughterCapabilities.LaughV1)), Is.EqualTo(1));
            Assert.That(effect.Nodes.Count(node => node.Capability.Equals(
                NebKherComponentContent.PlaceAdjacentIllusionV1)), Is.EqualTo(3));
            Assert.That(effect.Nodes.Count(node => node.Capability.Equals(
                SyntheticResolvedCardOrderSwapCapabilities.SwapResolvedPairV1)),
                Is.EqualTo(3));
            Assert.That(effect.Nodes.Count(node => node.Kind == EffectNodeKind.Choose),
                Is.EqualTo(3));
            Assert.That(EffectDefinitionValidator.Validate(effect, Catalog()), Is.Empty);
        }

        [Test]
        public void MindGrip_OffersExactlyTheBorrowAndAdjacentDefeatArms()
        {
            var card = NebKherCardContent.MindGripDefinition;
            AssertSkill(card, 9, 2, 1, PrintedValueChannel.Range, 5,
                CardTier.None, PrintedValueChannel.None);
            var root = Skill(card).EffectRoot;
            Assert.That(root.Nodes.Single(node => node.Id.Equals(root.RootNodeId)).Kind,
                Is.EqualTo(EffectNodeKind.Choose));
            Assert.That(root.Nodes.Count(node => node.Capability.Equals(
                SyntheticPreviousSlotBorrowCapabilities.BorrowPrintedActionV1)), Is.EqualTo(1));
            Assert.That(root.Nodes.Count(node => node.Capability.Equals(
                SyntheticConsequenceCapabilities.DefeatMinionV1)), Is.EqualTo(1));
            Assert.That(EffectDefinitionValidator.Validate(root, Catalog()), Is.Empty);
        }

        [Test]
        public void MindProbe_HasOnlyThePreviousSlotBorrow_AndIsTheBoxedHandicap()
        {
            var card = NebKherCardContent.MindProbeDefinition;
            AssertSkill(card, 8, 1, 1, PrintedValueChannel.Range, 3,
                CardTier.None, PrintedValueChannel.None);
            var root = Skill(card).EffectRoot;
            Assert.That(root.Nodes.Single(node => node.Id.Equals(root.RootNodeId)).Kind,
                Is.EqualTo(EffectNodeKind.Sequence));
            Assert.That(root.Nodes.Any(node => node.Capability.Equals(
                SyntheticConsequenceCapabilities.DefeatMinionV1)), Is.False);
            Assert.That(EffectDefinitionValidator.Validate(root, Catalog()), Is.Empty);
        }

        [Test]
        public void CrackAndShift_PrintDistinctTypedBoardPartitions()
        {
            AssertSplit(NebKherCardContent.CrackInRealityDefinition,
                NebKherComponentContent.CrackRealityV1, CardTier.II,
                PrintedValueChannel.Defense,
                new[] { NebKherCardContent.AnIllusionOfChoice,
                    NebKherCardContent.ShiftReality });
            AssertSplit(NebKherCardContent.ShiftRealityDefinition,
                NebKherComponentContent.ShiftRealityV1, CardTier.III,
                PrintedValueChannel.Range,
                System.Array.Empty<CardDefinitionId>());
        }

        [Test]
        public void ForceAndArmy_PlaceUpToThePrintedSupply_ThenGrantTheActionOnlyOverlay()
        {
            AssertArmy(NebKherCardContent.IllusionaryForceDefinition,
                CardTier.II, 3, 2,
                new[] { NebKherCardContent.MasterOfIllusions,
                    NebKherCardContent.IllusionaryArmy });
            AssertArmy(NebKherCardContent.IllusionaryArmyDefinition,
                CardTier.III, 4, 3, System.Array.Empty<CardDefinitionId>());
        }

        [Test]
        public void ImageLine_PlacesItsPrintedCount_ThenOffersARealOptionalIllusionSwap()
        {
            AssertImage(NebKherCardContent.FleetingImageDefinition,
                6, 3, 2, 1, CardTier.I, PrintedValueChannel.None,
                new[] { NebKherCardContent.MultipleProjections,
                    NebKherCardContent.IllusionaryForce });
            AssertImage(NebKherCardContent.MultipleProjectionsDefinition,
                5, 3, 3, 2, CardTier.II, PrintedValueChannel.Initiative,
                new[] { NebKherCardContent.MasterOfIllusions,
                    NebKherCardContent.IllusionaryArmy });
            AssertImage(NebKherCardContent.MasterOfIllusionsDefinition,
                5, 4, 3, 3, CardTier.III, PrintedValueChannel.Initiative,
                System.Array.Empty<CardDefinitionId>());
        }

        [Test]
        public void Sentry_BindsEitherARangedHeroBesideAnInRangeIllusion_OrAnAdjacentUnit()
        {
            var card = NebKherCardContent.PhantasmalSentryDefinition;
            Assert.That(card.Initiative.Value, Is.EqualTo(7));
            Assert.That(card.Progression.Tier, Is.EqualTo(CardTier.I));
            Assert.That(card.Progression.NextTierCandidateIds,
                Is.EqualTo(new[] { NebKherCardContent.PhantasmalWarrior,
                    NebKherCardContent.TwistFate }));
            var attack = card.ActionDefinitions.Single(action =>
                action.Kind == ActionKind.Attack);
            Assert.That(attack.BaseValues.Single(value =>
                value.Channel == PrintedValueChannel.Attack).Amount, Is.EqualTo(2));
            Assert.That(attack.BaseValues.Single(value =>
                value.Channel == PrintedValueChannel.Range).Amount, Is.EqualTo(4));
            Assert.That(attack.CardTextTiming, Is.EqualTo(CardTextTiming.BeforeTheAttack));
            Assert.That(attack.EffectRoot.Nodes.Count(node => node.Capability.Equals(
                SyntheticContinuationCapabilities.SetAttackTargetV1)), Is.EqualTo(2));
            Assert.That(EffectDefinitionValidator.Validate(attack.EffectRoot, Catalog()),
                Is.Empty);
        }

        [Test]
        public void WarriorAndChampion_MoveAnInRangeIllusionAtMostTheirPrintedDistance()
        {
            AssertWarrior(NebKherCardContent.PhantasmalWarriorDefinition,
                7, 1, CardTier.II, PrintedValueChannel.Initiative,
                new[] { NebKherCardContent.PhantasmalChampion,
                    NebKherCardContent.DeviousScheme });
            AssertWarrior(NebKherCardContent.PhantasmalChampionDefinition,
                8, 2, CardTier.III, PrintedValueChannel.Radius,
                System.Array.Empty<CardDefinitionId>());
        }

        [Test]
        public void FateAndScheme_DistinguishAdjacentFromInRangeIllusionSwapsAfterAttack()
        {
            AssertFate(NebKherCardContent.TwistFateDefinition, 7, CardTier.II,
                SyntheticTargetingCapabilities.ChooseTargetV4,
                new[] { NebKherCardContent.PhantasmalChampion,
                    NebKherCardContent.DeviousScheme });
            AssertFate(NebKherCardContent.DeviousSchemeDefinition, 8, CardTier.III,
                SyntheticTargetingCapabilities.RangeTargetV1,
                System.Array.Empty<CardDefinitionId>());
        }

        private static void AssertFate(CardDefinition card, int initiative, CardTier tier,
            EffectCapabilityReference tokenSelector, CardDefinitionId[] upgrades)
        {
            Assert.That(card.Initiative.Value, Is.EqualTo(initiative));
            Assert.That(card.Progression.Tier, Is.EqualTo(tier));
            Assert.That(card.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Defense));
            Assert.That(card.Progression.NextTierCandidateIds, Is.EqualTo(upgrades));
            var attack = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);
            Assert.That(attack.BaseValues.Single(value =>
                value.Channel == PrintedValueChannel.Attack).Amount, Is.EqualTo(4));
            Assert.That(attack.BaseValues.Single(value =>
                value.Channel == PrintedValueChannel.Range).Amount, Is.EqualTo(2));
            Assert.That(attack.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack));
            Assert.That(attack.EffectRoot.Nodes.Any(node =>
                node.Capability.Equals(tokenSelector)), Is.True);
            Assert.That(attack.EffectRoot.Nodes.Any(node =>
                node.Capability.Equals(SyntheticExchangeCapabilities.SwapV2)), Is.True);
            Assert.That(EffectDefinitionValidator.Validate(attack.EffectRoot, Catalog()), Is.Empty);
        }

        private static void AssertWarrior(CardDefinition card, int initiative, int imageMove,
            CardTier tier, PrintedValueChannel item, CardDefinitionId[] upgrades)
        {
            Assert.That(card.Initiative.Value, Is.EqualTo(initiative));
            Assert.That(card.Progression.Tier, Is.EqualTo(tier));
            Assert.That(card.Progression.ItemIcon, Is.EqualTo(item));
            Assert.That(card.Progression.NextTierCandidateIds, Is.EqualTo(upgrades));
            var attack = card.ActionDefinitions.Single(action =>
                action.Kind == ActionKind.Attack);
            Assert.That(attack.BaseValues.Single(value =>
                value.Channel == PrintedValueChannel.Attack).Amount, Is.EqualTo(3));
            Assert.That(attack.BaseValues.Single(value =>
                value.Channel == PrintedValueChannel.Range).Amount, Is.EqualTo(5));
            Assert.That(attack.CardTextTiming, Is.EqualTo(CardTextTiming.BeforeTheAttack));
            var move = attack.EffectRoot.Nodes.Single(node => node.Capability.Equals(
                SyntheticDisplacementCapabilities.DirectMoveV3));
            Assert.That(move.Inputs.Single(input => input.ParameterName == "distance")
                .Literal.IntegerValue, Is.EqualTo(imageMove));
            Assert.That(attack.EffectRoot.Nodes.Count(node => node.Capability.Equals(
                SyntheticContinuationCapabilities.SetAttackTargetV1)), Is.EqualTo(2));
            Assert.That(EffectDefinitionValidator.Validate(attack.EffectRoot, Catalog()),
                Is.Empty);
        }

        private static void AssertImage(CardDefinition card, int initiative, int defense,
            int radius, int places, CardTier tier, PrintedValueChannel item,
            CardDefinitionId[] upgrades)
        {
            AssertSkill(card, initiative, defense, 2, PrintedValueChannel.Radius,
                radius, tier, item);
            Assert.That(card.Progression.NextTierCandidateIds, Is.EqualTo(upgrades));
            var effect = Skill(card).EffectRoot;
            Assert.That(effect.Nodes.Count(node => node.Capability.Equals(
                NebKherComponentContent.PlaceIllusionV1)), Is.EqualTo(places));
            Assert.That(effect.Nodes.Count(node => node.Capability.Equals(
                SyntheticTargetingCapabilities.ChooseTargetV4)), Is.EqualTo(1));
            Assert.That(effect.Nodes.Count(node => node.Capability.Equals(
                SyntheticExchangeCapabilities.SwapWithActorV1)), Is.EqualTo(1));
            Assert.That(effect.Nodes.Any(node => node.Kind == EffectNodeKind.Optional),
                Is.True);
            Assert.That(EffectDefinitionValidator.Validate(effect, Catalog()), Is.Empty);
        }

        private static void AssertArmy(CardDefinition card, CardTier tier, int defense,
            int places, CardDefinitionId[] upgrades)
        {
            AssertSkill(card, 5, defense, 2, PrintedValueChannel.Radius, 4,
                tier, PrintedValueChannel.Attack);
            Assert.That(card.Progression.NextTierCandidateIds, Is.EqualTo(upgrades));
            var effect = Skill(card).EffectRoot;
            Assert.That(effect.Nodes.Count(node => node.Capability.Equals(
                NebKherComponentContent.PlaceIllusionV1)), Is.EqualTo(places));
            Assert.That(effect.Nodes.Count(node => node.Capability.Equals(
                NebKherComponentContent.ReclaimIllusionV1)), Is.EqualTo(places));
            Assert.That(effect.Nodes.Count(node => node.Capability.Equals(
                NebKherComponentContent.IllusionsCountAsMinionsV1)), Is.EqualTo(1));
            Assert.That(EffectDefinitionValidator.Validate(effect, Catalog()), Is.Empty);
        }

        private static void AssertSplit(CardDefinition card, EffectCapabilityReference capability,
            CardTier tier, PrintedValueChannel item, CardDefinitionId[] upgrades)
        {
            AssertSkill(card, 11, 5, 2, PrintedValueChannel.None, 0, tier, item);
            Assert.That(card.Progression.NextTierCandidateIds, Is.EqualTo(upgrades));
            var root = Skill(card).EffectRoot;
            Assert.That(root.Nodes.Single().Capability, Is.EqualTo(capability));
            Assert.That(EffectDefinitionValidator.Validate(root, Catalog()), Is.Empty);
        }

        private static ActionDefinition Skill(CardDefinition card) =>
            card.ActionDefinitions.Single(action =>
                action.Origin == ActionDefinitionOrigin.PrintedPrimary);

        private static void AssertSkill(CardDefinition card, int initiative, int defense,
            int movement, PrintedValueChannel channel, int value, CardTier tier,
            PrintedValueChannel item)
        {
            Assert.That(card.Initiative.Value, Is.EqualTo(initiative));
            Assert.That(card.Progression.Tier, Is.EqualTo(tier));
            Assert.That(card.Progression.ItemIcon, Is.EqualTo(item));
            var skill = Skill(card);
            Assert.That(skill.Kind, Is.EqualTo(ActionKind.Skill));
            if (channel == PrintedValueChannel.None)
                Assert.That(skill.BaseValues, Is.Empty);
            else
            {
                Assert.That(skill.BaseValues.Single().Channel, Is.EqualTo(channel));
                Assert.That(skill.BaseValues.Single().Amount, Is.EqualTo(value));
            }
            Assert.That(card.ActionDefinitions.Single(action =>
                action.Kind == ActionKind.Movement).BaseValues.Single().Amount,
                Is.EqualTo(movement));
            Assert.That(card.ActionDefinitions.Single(action =>
                action.Kind == ActionKind.Defense).BaseValues.Single().Amount,
                Is.EqualTo(defense));
        }

        private static EffectCapabilityCatalog Catalog() =>
            SyntheticEffectCapabilities.CreateCatalog(
                NebKherComponentContent.Handlers()
                .Concat(NebKherDelayedDoubtContent.Handlers())
                .Concat(NebKherUltimateContent.Handlers())
                .Concat(new[]
                {
                    SyntheticPreviousSlotBorrowCapabilities.Handler(),
                    SyntheticResolvedCardOrderSwapCapabilities.Handler()
                }));
    }
}
