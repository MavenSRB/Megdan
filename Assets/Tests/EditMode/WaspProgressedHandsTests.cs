using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.15: the staged p.15 positions the player-pipeline journeys start from.
    ///
    /// <para>The progressed hands and the engaged staging are `DEV-004` fixture arms, but what they
    /// state is rules content: which locations p.15's own procedure leaves Wasp's cards in at each
    /// stage, and a board position p.19's geometry claims hold on. These cases pin those shapes at
    /// the content level, so a journey that starts failing is debugged against a staging that is
    /// known to be right.</para>
    /// </summary>
    public sealed class WaspProgressedHandsTests
    {
        [Test]
        public void TheTierTwoHands_DealTheBasicsPlusOneTierTwoOfEachColourAndKeepTheRestInTheDeck()
        {
            var configuration =
                PlaceholderOneVsOneTestConfiguration.CreateVerifiedTierTwoMirrorForInternalTest(
                    TeamSide.Red);

            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var hand = HandDefinitions(configuration, side);
                Assert.That(hand, Has.Count.EqualTo(5),
                    "p.15's closing count: exactly 5 cards, 1 of each color.");
                Assert.That(hand, Does.Contain(WaspCardContent.MagneticDagger));
                Assert.That(hand, Does.Contain(WaspCardContent.StaticBarrier));
                Assert.That(hand.Select(id => Definition(configuration, id).Progression.Tier)
                        .Where(tier => tier != CardTier.None).Distinct().Single(),
                    Is.EqualTo(CardTier.II),
                    "Every non-basic card in a Tier II hand is Tier II.");
            }

            var bothHands = HandDefinitions(configuration, TeamSide.Red)
                .Concat(HandDefinitions(configuration, TeamSide.Blue))
                .ToArray();
            Assert.That(bothHands, Is.SupersetOf(new[]
                {
                    WaspCardContent.Electrocute, WaspCardContent.ChargedBoomerang,
                    WaspCardContent.ControlGravity, WaspCardContent.KineticRepulse,
                    WaspCardContent.Telekinesis, WaspCardContent.DeflectProjectiles
                }),
                "The two seats hold complementary siblings, so every Tier II card is in some hand.");
            Assert.That(
                configuration.CardInstances.Where(card =>
                    card.State.Location != CardLocation.Hand),
                Has.All.Matches<CardInstance>(card => card.State.Location == CardLocation.Deck),
                "Everything not dealt to a hand waits in the deck, exactly as p.15's returns leave it.");
        }

        [Test]
        public void TheTierThreeMirror_UnlocksBothHighVoltagesThroughThePageSixteenTransition()
        {
            var configuration =
                PlaceholderOneVsOneTestConfiguration.CreateVerifiedTierThreeMirrorForInternalTest(
                    TeamSide.Red);

            var ultimates = configuration.CardInstances
                .Where(card => card.DefinitionReference.DefinitionId
                    .Equals(WaspCardContent.HighVoltage))
                .ToArray();
            Assert.That(ultimates, Has.Length.EqualTo(2), "One per mirrored seat.");
            foreach (var ultimate in ultimates)
            {
                Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea),
                    "The factory unlocks through CardLifecycleAuthority.TryUnlockUltimateFromDeck, " +
                    "the one p.16 transition allowed to put a card here.");
                Assert.That(ultimate.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp),
                    "p.16: \"Place it Faceup to the left side of your hero dashboard\".");
            }

            var bothHands = HandDefinitions(configuration, TeamSide.Red)
                .Concat(HandDefinitions(configuration, TeamSide.Blue))
                .ToArray();
            Assert.That(bothHands, Is.SupersetOf(new[]
                {
                    WaspCardContent.Electroblast, WaspCardContent.ThunderBoomerang,
                    WaspCardContent.CenterOfMass, WaspCardContent.KineticBlast,
                    WaspCardContent.MassTelekinesis, WaspCardContent.ReflectProjectiles
                }),
                "The two seats hold complementary siblings, so every Tier III card is in some hand.");
        }

        [Test]
        public void TheEngagedStaging_PutsBothWaspsBesideOneBlueMeleeMinionTwoSpacesApart()
        {
            var configuration =
                PlaceholderOneVsOneTestConfiguration.CreateVerifiedMirrorForInternalTest(
                    TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var staged = EngagedMirrorStaging.Restage(map, setup, out var engagedMinionId);
            var minion = staged.Minions.Single(value => value.MinionId.Equals(engagedMinionId));
            Assert.That(minion.TeamSide, Is.EqualTo(TeamSide.Blue));
            var red = staged.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var blue = staged.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue);
            Assert.That(HexDistanceQuery.Calculate(red.Coordinate, minion.Coordinate), Is.EqualTo(1),
                "The Red Wasp stands adjacent to the engaged minion, so adjacency Attacks are legal.");
            Assert.That(HexDistanceQuery.Calculate(blue.Coordinate, minion.Coordinate), Is.EqualTo(1),
                "The Blue Wasp stands beside the same minion, so it is was-adjacent for High Voltage.");
            Assert.That(HexDistanceQuery.Calculate(red.Coordinate, blue.Coordinate), Is.EqualTo(2),
                "Two spaces via a shared neighbor is inside Radius 2 without adjacency, and a bent " +
                "line under p.19 - the one gap every journey's geometry leans on.");
            Assert.That(
                staged.Minions.Select(value => value.Coordinate),
                Has.None.EqualTo(red.Coordinate).And.None.EqualTo(blue.Coordinate),
                "Both staged spaces are unoccupied.");
            Assert.That(staged.WaveCount, Is.EqualTo(setup.WaveCount));
            Assert.That(staged.TieBreakerState, Is.EqualTo(setup.TieBreakerState));
            foreach (var hero in staged.Heroes)
            {
                var space = map.Spaces.Single(value =>
                    value.Id.Equals(hero.OccupiedSpace.SpaceId));
                Assert.That(space.Coordinate, Is.EqualTo(hero.Coordinate),
                    "The occupied space reference and the coordinate agree, which is what the " +
                    "hero view validates against the rendered map.");
                Assert.That(space.Kind, Is.EqualTo(BoardMapSpaceKind.Playable));
            }
        }

        private static System.Collections.Generic.List<CardDefinitionId> HandDefinitions(
            PlaceholderOneVsOneTestConfiguration configuration, TeamSide side)
        {
            var heroId = configuration.Participants.Heroes.Single(hero =>
                configuration.Participants.GetTeamForHero(hero.Id).Side == side).Id;
            return configuration.CardInstances
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => card.DefinitionReference.DefinitionId)
                .ToList();
        }

        private static CardDefinition Definition(
            PlaceholderOneVsOneTestConfiguration configuration, CardDefinitionId id) =>
            configuration.CardDefinitions.Single(definition => definition.Id.Equals(id));
    }
}
