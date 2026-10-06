using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Milestone 9.53: the complete, selectable NebKher printed set.</summary>
    public sealed class NebKherDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedNebKher_DealsExactlyEighteenFronts_AndBoxesMindProbe(int perTeam)
        {
            var nebkher = RegisteredHeroRoster.ForThisBuild.Single(hero =>
                hero.HeroIdValue == QuickGameSetupConfiguration.NebKherHeroIdValue);
            Assert.That(nebkher.DisplayName, Is.EqualTo("NebKher the Harbinger"));
            Assert.That(nebkher.Complexity, Is.EqualTo(HeroComplexity.VeryComplex));
            Assert.That(nebkher.Roles, Is.EqualTo(
                HeroRole.Disabler | HeroRole.Tokens | HeroRole.Sniper));

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { nebkher }.Concat(printed.Red.Skip(1)),
                printed.Blue);
            var format = perTeam == 2
                ? MatchFormat.TwoVersusTwoQuick : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(
                format, lineup, TeamSide.Red);
            var cards = configuration.CardInstances.Where(card =>
                card.OwnerId.Value == nebkher.HeroIdValue).ToArray();

            Assert.That(cards.Length, Is.EqualTo(18));
            Assert.That(cards.Select(card => card.DefinitionReference),
                Is.EquivalentTo(NebKherCardContent.DealtDefinitions
                    .Select(card => card.Reference)));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Has.No.Member(NebKherCardContent.MindProbe));
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(NebKherCardContent.StartingHand));
            Assert.That(cards.Count(card => card.State.Location == CardLocation.Deck),
                Is.EqualTo(13));
        }

        [Test]
        public void EveryFrontAndTriggeredBodyIsLoadedFromTheProductionCatalog()
        {
            Assert.That(NebKherCardContent.InventoryOrder.Count, Is.EqualTo(19));
            Assert.That(NebKherCardContent.NotYetComposed, Is.Empty);
            Assert.That(NebKherCardContent.AllDefinitions.Select(card => card.Id),
                Is.EqualTo(NebKherCardContent.InventoryOrder));
            Assert.That(NebKherCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(NebKherCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { NebKherCardContent.MindProbe }));

            var catalog = MatchContentCatalogs.ForThisBuild();
            foreach (var card in NebKherCardContent.DealtDefinitions)
                foreach (var action in card.ActionDefinitions
                    .Where(action => action.EffectRoot != null))
                    Assert.That(EffectDefinitionValidator.Validate(
                        action.EffectRoot, catalog.Capabilities), Is.Empty,
                        card.Id.Value);
            foreach (var effect in NebKherDelayedDoubtContent.TriggeredEffects()
                .Concat(new[] { NebKherUltimateContent.TriggeredEffect }))
            {
                Assert.That(catalog.TriggeredEffects.TryGet(effect.Reference, out _),
                    Is.True, effect.Id.Value);
                Assert.That(EffectDefinitionValidator.Validate(effect,
                    catalog.Capabilities), Is.Empty, effect.Id.Value);
            }
            Assert.That(catalog.Components.RegisteredTokens,
                Has.Member(NebKherComponentContent.Illusion.Reference));
        }

        [Test]
        public void DelayedColorEffect_ActivatesAndTriggersExactlyAtNextReveal()
        {
            var actor = new HeroId("hero-nebkher");
            var lifecycle = new ActiveEffectLifecycle();
            var effect = NebKherDelayedDoubtContent.DelayedEffect(
                NebKherCardContent.ImbueDoubt, CardColor.Red);
            lifecycle.Create(new ActiveEffectId("doubt-red"), effect,
                actor, TeamSide.Red, default,
                new CardInstanceId("imbue-source"),
                NebKherCardContent.PrimarySkill, 1, 1);
            var tie = new TieBreakerState(TeamSide.Red);
            Assert.That(lifecycle.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(
                    TimingCheckpoint.CardsPlayedAndRevealed, 1, 1), tie), Is.Empty);
            Assert.That(lifecycle.ApplyCheckpoint(
                    new ActiveEffectCheckpointContext(
                        TimingCheckpoint.CardsPlayedAndRevealed, 1, 2), tie)
                    .Select(value => value.Kind),
                Is.EqualTo(new[]
                {
                    ActiveEffectLifecycleEventKind.Activated,
                    ActiveEffectLifecycleEventKind.Triggered
                }));
            Assert.That(lifecycle.ApplyCheckpoint(
                    new ActiveEffectCheckpointContext(
                        TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded, 1, 2), tie)
                    .Single().Kind,
                Is.EqualTo(ActiveEffectLifecycleEventKind.Expired));
            Assert.That(lifecycle.ApplyCheckpoint(
                    new ActiveEffectCheckpointContext(
                        TimingCheckpoint.CardsPlayedAndRevealed, 1, 3), tie), Is.Empty);
        }

        [Test]
        public void AnIllusionOfChoice_AffectsUpToTwoPresentEnemiesWithoutADecline()
        {
            var body = NebKherDelayedDoubtContent.TriggeredBody(
                NebKherCardContent.AnIllusionOfChoice, CardColor.Red);
            var root = body.Nodes.Single(node => node.Id.Equals(body.RootNodeId));
            Assert.That(root.Kind, Is.EqualTo(EffectNodeKind.Optional));
            var optional = body.Nodes.Where(node =>
                node.Id.Equals(root.Id) ||
                node.Id.Value.EndsWith("-optional-2")).ToArray();
            Assert.That(optional, Has.Length.EqualTo(2));
            Assert.That(optional.Select(node => node.Kind),
                Is.All.EqualTo(EffectNodeKind.Optional));
            Assert.That(optional.Select(node => node.FailurePolicy),
                Is.All.EqualTo(EffectFailurePolicy.IfAble));
            Assert.That(optional.Select(node => node.Chooser),
                Is.All.EqualTo(EffectChooserRole.None));
            var secondTarget = body.Nodes.Single(node =>
                node.Id.Value.EndsWith("-target-2"));
            Assert.That(secondTarget.Inputs.Any(input =>
                input.SourceKind == EffectBindingSourceKind.NodeOutput &&
                input.SourceNodeId.Value.EndsWith("-target-1")), Is.True,
                "the second automatic pass may not hit the first enemy again");
        }
    }
}
