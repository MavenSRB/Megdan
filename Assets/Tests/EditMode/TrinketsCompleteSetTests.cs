using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TrinketsCompleteSetTests
    {
        [Test]
        public void NineteenFaces_MatchInventoryAndOnlyHandicapIsBoxed()
        {
            var all = TrinketsCardContent.AllDefinitions;
            Assert.That(all.Count, Is.EqualTo(19));
            Assert.That(all.Select(card => card.Id),
                Is.EqualTo(TrinketsCardContent.InventoryOrder));
            Assert.That(all.Select(card => card.Id).Distinct().Count(), Is.EqualTo(19));
            Assert.That(TrinketsCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(TrinketsCardContent.StartingHand.Count, Is.EqualTo(5));
            Assert.That(TrinketsCardContent.Deck.Count, Is.EqualTo(13));
            Assert.That(TrinketsCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { TrinketsCardContent.RamshackleTurret }));
            Assert.That(TrinketsCardContent.DealtDefinitions.Select(card => card.Id),
                Is.EquivalentTo(TrinketsCardContent.StartingHand.Concat(TrinketsCardContent.Deck)));
            Assert.That(all.All(card => card.SourceContent != null), Is.True);
            Assert.That(all.All(card => card.ContentAuthority == CardContentAuthority.VerifiedOfficial),
                Is.True);
        }

        [Test]
        public void EveryDealtPrimaryEffect_ValidatesAgainstPlannedCatalog()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                TrinketsComponentContent.ObstacleHandlers()
                    .Concat(TrinketsComponentContent.BarrierHandlers())
                    .Concat(TrinketsActiveEffectContent.Handlers()));
            foreach (var card in TrinketsCardContent.DealtDefinitions)
                foreach (var action in card.ActionDefinitions)
                    if (action.EffectRoot != null)
                        Assert.That(EffectDefinitionValidator.Validate(action.EffectRoot, catalog),
                            Is.Empty, card.DisplayName + " / " + action.Id.Value);
        }

        [Test]
        public void Ultimate_ProvidesExactlyOneRangeAndRadiusWhileUnlocked()
        {
            var ultimate = TrinketsCardContent.Definition(
                TrinketsCardContent.UnlimitedFirepower);
            Assert.That(ultimate.Progression.IsUltimate, Is.True);
            Assert.That(ultimate.Initiative.IsDefined, Is.False);
            Assert.That(ultimate.ActionDefinitions, Is.Empty);
            Assert.That(ultimate.UltimateRule.Modifications.Select(rule => rule.Channel),
                Is.EquivalentTo(new[]
                {
                    PrintedValueChannel.Range, PrintedValueChannel.Radius
                }));
            Assert.That(ultimate.UltimateRule.Modifications.Select(rule => rule.Amount),
                Is.EqualTo(new[] { 1, 1 }));
        }

        [Test]
        public void UltimateStandingSource_AddsOnceToRedRangeAndTurretCentredGreenRadius()
        {
            var owner = new HeroId("trinkets-complete");
            var board = new AdjacentMoveAuthority(BoardMapDefinition.CreateSynthetic(
                new MapId("trinkets-ultimate"), 1,
                Enumerable.Range(-4, 9).SelectMany(q => Enumerable.Range(-4, 9)
                    .Select(r => new HexCoordinate(q, r))).ToArray()),
                new[] { new HeroOccupancy(owner, new HexCoordinate(0, 0)) },
                Array.Empty<ObstacleOccupancy>());
            var participants = new MatchParticipants(
                new[] { new Team(new TeamId("red"), TeamSide.Red) },
                new[] { new Player(new PlayerId("owner"), new TeamId("red")) },
                new[] { new Hero(owner, new PlayerId("owner"),
                    HeroDashboard.RulebookLayout) });
            var ultimate = TrinketsCardContent.Definition(
                TrinketsCardContent.UnlimitedFirepower);
            var instance = new CardInstance(new CardInstanceId("ultimate-standing"),
                ultimate.Reference, owner, new CardStateFacts(CardLocation.UltimateArea,
                    false, CardResolutionStatus.None, CardFaceOrientation.FaceUp));
            var unlocked = UltimateUnlockQuery.Derive(
                TrinketsCardContent.AllDefinitions, new[] { instance });
            Assert.That(unlocked.Ultimates.Count, Is.EqualTo(1));
            var rules = RuleModificationQuery.Build(null, board, participants,
                TieBreakerState.Unrevealed, unlocked, RuleObject.None, null, null);
            var red = TrinketsCardContent.Definition(TrinketsCardContent.MakeshiftMinigun);
            var green = TrinketsCardContent.Definition(TrinketsCardContent.EarlyPrototype);
            var redOffer = new ActionOffer(new CardInstanceId("red"), red.Reference,
                TrinketsCardContent.PrimaryRangedAttack, owner,
                ActionInvocationContext.PrintedPrimary, true);
            var greenOffer = new ActionOffer(new CardInstanceId("green"), green.Reference,
                TrinketsCardContent.PrimarySkill, owner,
                ActionInvocationContext.PrintedPrimary, true);
            Assert.That(RangeValueQuery.Calculate(redOffer,
                TrinketsCardContent.AllDefinitions, null, rules).Total, Is.EqualTo(3));
            Assert.That(RadiusValueQuery.Calculate(greenOffer,
                TrinketsCardContent.AllDefinitions, null, rules).Total, Is.EqualTo(3));
            var baseline = RuleModificationQuery.Build(null, board, participants,
                TieBreakerState.Unrevealed, UnlockedUltimateSet.None,
                RuleObject.None, null, null);
            Assert.That(RangeValueQuery.Calculate(redOffer,
                TrinketsCardContent.AllDefinitions, null, baseline).Total, Is.EqualTo(2));
            Assert.That(RadiusValueQuery.Calculate(greenOffer,
                TrinketsCardContent.AllDefinitions, null, baseline).Total, Is.EqualTo(2));
        }

        [Test]
        public void Basics_KeepPrintedActionsAndHandicapIsExplicitlyDeferred()
        {
            var rapid = TrinketsCardContent.Definition(TrinketsCardContent.RapidRedeployment);
            var salvage = TrinketsCardContent.Definition(TrinketsCardContent.SalvageParts);
            var handicap = TrinketsCardContent.Definition(TrinketsCardContent.RamshackleTurret);
            Assert.That(rapid.ActionDefinitions.Select(action => action.Kind),
                Is.EqualTo(new[] { ActionKind.Skill, ActionKind.Movement,
                    ActionKind.Defense }));
            Assert.That(salvage.ActionDefinitions.Select(action => action.Kind),
                Is.EqualTo(new[] { ActionKind.Skill, ActionKind.Defense }));
            Assert.That(handicap.ActionDefinitions.Single(action =>
                action.Kind == ActionKind.Skill).EffectRoot, Is.Null);
            Assert.That(handicap.SourceContent, Is.Not.Null);
        }
    }
}
