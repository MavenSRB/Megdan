using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
#if UNITY_EDITOR
using GuardsDigital.Presentation.Match;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using UnityEngine;
#endif
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TrinketsDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedTrinkets_DealsTheEighteenPlayableFacesInEitherFormat(int perTeam)
        {
            var trinkets = RegisteredHeroRoster.ForThisBuild.Single(hero =>
                hero.ContentId.Value == "trinkets");
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(25));
            Assert.That(trinkets.HeroIdValue,
                Is.EqualTo(QuickGameSetupConfiguration.TrinketsHeroIdValue));
            Assert.That(trinkets.DisplayName, Is.EqualTo("Trinkets the Scavenger"));
            Assert.That(trinkets.Complexity, Is.EqualTo(HeroComplexity.VeryComplex));
            Assert.That(trinkets.Roles,
                Is.EqualTo(HeroRole.Sniper | HeroRole.Damager |
                    HeroRole.Pusher | HeroRole.Tokens));

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { trinkets }.Concat(printed.Red.Skip(1)),
                printed.Blue);
            var format = perTeam == 2 ? MatchFormat.TwoVersusTwoQuick :
                MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(
                format, lineup, TeamSide.Red);
            var cards = configuration.CardInstances.Where(card =>
                card.OwnerId.Value == trinkets.HeroIdValue).ToArray();
            Assert.That(cards.Length, Is.EqualTo(18));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(TrinketsCardContent.DealtDefinitions.Select(card => card.Id)));
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(TrinketsCardContent.StartingHand));
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Deck)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(TrinketsCardContent.Deck));
            Assert.That(HeroLineupComposition.Compose(format, lineup).Heroes.Single(hero =>
                    hero.ContentId.Value == "trinkets").SpawnPoint.SpawnPointId.Value,
                Is.EqualTo("red-hero-01"));
        }

        [Test]
        public void StagedBoard_AndTrustedCatalog_RegisterCardsBarrierAndTriggers()
        {
            var staged = MatchConfiguration.CreateStagedTrinketsForInternalTest(TeamSide.Red);
            Assert.That(staged.CardDefinitions.Where(card =>
                card.Id.Value.StartsWith("trinkets-", StringComparison.Ordinal)).Count(),
                Is.EqualTo(18));
            var catalogs = MatchContentCatalogs.ForThisBuild();
            Assert.That(catalogs.Components.TryLoadToken(
                TrinketsComponentContent.BarrierV1, out _), Is.True);
            foreach (var effect in new[]
            {
                TrinketsCardContent.DisruptorJolt,
                TrinketsCardContent.DisruptorPulse,
                TrinketsCardContent.DisruptorGrid
            })
            {
                var trigger = TrinketsEffectContent.DisruptorTrigger(effect,
                    TrinketsCardContent.ContentRevision,
                    effect.Equals(TrinketsCardContent.DisruptorGrid));
                Assert.That(catalogs.TriggeredEffects.Definitions.Any(definition =>
                    definition.Id.Equals(trigger.Id)), Is.True);
            }
        }

        #if UNITY_EDITOR
        [Test]
        public void BoardModel_CarriesTheSingleTurretObstacleWithoutMakingItAToken()
        {
            var owner = new GuardsDigital.Core.Units.Heroes.HeroId("hero-trinkets");
            var turret = new ObstacleOccupancy(
                OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, owner),
                new HexCoordinate(1, 1));
            var model = new BoardInteractionViewModel(1,
                Array.Empty<HeroOccupancy>(), Array.Empty<MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(), Array.Empty<MinionId>(), default,
                obstacles: new[] { turret });
            Assert.That(model.Obstacles, Is.EqualTo(new[] { turret }));
            Assert.That(model.PlacedTokens, Is.Empty);
        }

        [Test]
        public void BoardView_ProjectsRepositionsAndRemovesTheTurretObstacle()
        {
            var root = new GameObject("Trinkets projected board test");
            try
            {
                var view = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(), Array.Empty<HexTileView>(), null);
                var id = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind,
                    new HeroId("hero-trinkets"));
                BoardInteractionViewModel Model(params ObstacleOccupancy[] obstacles) =>
                    new BoardInteractionViewModel(1, Array.Empty<HeroOccupancy>(),
                        Array.Empty<MinionOccupancy>(), Array.Empty<HexCoordinate>(),
                        Array.Empty<HeroId>(), Array.Empty<MinionId>(), default,
                        obstacles: obstacles);

                var first = new ObstacleOccupancy(id, new HexCoordinate(1, 1));
                view.Render(Model(first));
                var piece = UnityEngine.Object.FindObjectsByType<PlaceholderObstacleView>(FindObjectsSortMode.None)
                    .Single(obstacle => obstacle.Occupancy.ObstacleId.Equals(id));
                piece.transform.SetParent(root.transform, true);
                Assert.That(piece.Occupancy, Is.EqualTo(first));
                Assert.That(piece.gameObject.activeSelf, Is.True);

                var moved = new ObstacleOccupancy(id, new HexCoordinate(2, 1));
                view.Render(Model(moved));
                Assert.That(piece.Occupancy, Is.EqualTo(moved));
                Assert.That(UnityEngine.Object.FindObjectsByType<PlaceholderObstacleView>(FindObjectsSortMode.None)
                    .Count(obstacle => obstacle.Occupancy.ObstacleId.Equals(id)), Is.EqualTo(1));

                view.Render(Model());
                Assert.That(piece.gameObject.activeSelf, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
        #endif
    }
}
