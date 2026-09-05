using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The ELEVENTH amendment's 2v2: rulebook p.6 setup step 7's four-player line-up, composed and
    /// played.
    ///
    /// <para>Every case drives the real configuration and the real setup authority against the
    /// official map resource, for the same reason the six-player fixture does - the subject is a
    /// match a player can be dealt, not what a factory returns. The six-player board is unchanged
    /// and keeps its own fixture; the one case here that reads it does so to prove exactly
    /// that.</para>
    /// </summary>
    public sealed class OfficialFourPlayerSetupTests
    {
        /// <summary>
        /// p.6 setup step 7's four dashboards - "Wasp the Warmaiden" and "Xargatha the Changed" for
        /// the Red team, "Arien the Tidemaster" and "Brogan the Destroyer" for the Blue - with the
        /// seat each is dealt to and the hero spawn point each stands on. Written out rather than
        /// read from the configuration under test.
        /// </summary>
        private static readonly (string HeroId, string ContentId, string DisplayName,
            TeamSide Side, string SpawnPoint, string InstancePrefix)[] ExpectedSeats =
        {
            (OfficialSixPlayerParticipants.WaspHeroIdValue, "wasp", "Wasp the Warmaiden",
                TeamSide.Red, "red-hero-01", "red-01"),
            (OfficialSixPlayerParticipants.XargathaHeroIdValue, "xargatha", "Xargatha the Changed",
                TeamSide.Red, "red-hero-02", "red-02"),
            (OfficialSixPlayerParticipants.ArienHeroIdValue, "arien", "Arien the Tidemaster",
                TeamSide.Blue, "blue-hero-01", "blue-01"),
            (OfficialSixPlayerParticipants.BroganHeroIdValue, "brogan", "Brogan the Destroyer",
                TeamSide.Blue, "blue-hero-02", "blue-02")
        };

        [Test]
        public void TheFourPlayerSetup_PlacesStepSevensFourDashboardsOnTheirOwnThroneSpawnPoints()
        {
            var setup = CreateSetup(MatchFormat.TwoVersusTwoQuick, out _, out _);

            Assert.That(setup.Heroes, Has.Count.EqualTo(4),
                "Setup step 2 splits four players into two equal teams of two.");
            foreach (var expected in ExpectedSeats)
            {
                var hero = setup.Heroes.Single(candidate => candidate.HeroId.Value == expected.HeroId);
                Assert.That(hero.ContentId.Value, Is.EqualTo(expected.ContentId));
                Assert.That(hero.DisplayName, Is.EqualTo(expected.DisplayName),
                    "The label is p.6 setup step 7's own name for that dashboard.");
                Assert.That(hero.TeamSide, Is.EqualTo(expected.Side),
                    "Step 7 names Wasp and Xargatha for Red and Arien and Brogan for Blue.");
                Assert.That(hero.SpawnPoint.SpawnPointId.Value, Is.EqualTo(expected.SpawnPoint),
                    "A hero keeps the spawn point it stands on in the six-player board.");
            }

            Assert.That(setup.Heroes.Count(hero => hero.TeamSide == TeamSide.Red), Is.EqualTo(2));
            Assert.That(setup.Heroes.Count(hero => hero.TeamSide == TeamSide.Blue), Is.EqualTo(2));
        }

        [Test]
        public void NeitherTigerclawNorSabina_IsDealtAtFourPlayers()
        {
            var setup = CreateSetup(MatchFormat.TwoVersusTwoQuick, out var configuration, out _);
            var dealt = setup.Heroes.Select(hero => hero.HeroId.Value).ToArray();

            Assert.That(dealt, Does.Not.Contain(OfficialSixPlayerParticipants.TigerclawHeroIdValue),
                "Step 7 brings Tigerclaw in for 'the third players' - and a 2v2 has none.");
            Assert.That(dealt, Does.Not.Contain(OfficialSixPlayerParticipants.SabinaHeroIdValue));
            Assert.That(
                configuration.CardDefinitions.Any(definition =>
                    definition.Id.Value.Contains("tigerclaw") || definition.Id.Value.Contains("sabina")),
                Is.False,
                "and a hero nobody is dealt brings no cards to the table either.");
        }

        [Test]
        public void TheFourPlayerRoster_IsTheSixPlayerRostersFirstTwoASideAsStepSevenDescribesIt()
        {
            // Step 7 builds the six-player line-up out of the four-player one: the same four
            // dashboards, plus Tigerclaw and Sabina for the third players. This asserts that
            // relationship rather than relying on it - the two rosters are stated independently.
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var four = OfficialFourPlayerParticipants.Seats
                    .Where(seat => seat.Side == side)
                    .Select(seat => (seat.HeroId, seat.PlayerIdValue))
                    .ToArray();
                var firstTwoOfSix = OfficialSixPlayerParticipants.Seats
                    .Where(seat => seat.Side == side)
                    .Take(2)
                    .Select(seat => (seat.HeroId, seat.PlayerIdValue))
                    .ToArray();

                Assert.That(four, Is.EqualTo(firstTwoOfSix),
                    "The " + side + " team's 2v2 seats are its 3v3 seats without the third.");
            }
        }

        [Test]
        public void TheQuickAndLongFourPlayerBoards_DifferOnlyInTheirWaveAndLifeCounts()
        {
            var quick = CreateSetup(MatchFormat.TwoVersusTwoQuick, out _, out _);
            var length = CreateSetup(MatchFormat.TwoVersusTwoLong, out _, out _);

            Assert.That(quick.WaveCount, Is.EqualTo(3), "p.6 step 4: a Quick game uses three Waves.");
            Assert.That(length.WaveCount, Is.EqualTo(5), "and a Long game uses five.");
            foreach (var team in quick.Teams)
            {
                Assert.That(team.LifeCount, Is.EqualTo(4),
                    "p.6 step 5's table: four players in a Quick game take four Life counters.");
            }

            foreach (var team in length.Teams)
            {
                Assert.That(team.LifeCount, Is.EqualTo(6),
                    "and six in a Long game.");
            }

            Assert.That(
                length.Heroes.Select(hero => (hero.HeroId.Value, hero.SpawnPoint.SpawnPointId.Value)),
                Is.EqualTo(
                    quick.Heroes.Select(hero => (hero.HeroId.Value, hero.SpawnPoint.SpawnPointId.Value))),
                "Nothing else on the page varies with the game length.");
        }

        [Test]
        public void TheMinions_AreSetupStepTensTwelveAtEveryPlayerCount()
        {
            var fourPlayer = QuickGameSetupConfiguration.CreateOfficial(MatchFormat.TwoVersusTwoQuick);
            var sixPlayer = QuickGameSetupConfiguration.CreateOfficial(MatchFormat.ThreeVersusThreeQuick);

            Assert.That(fourPlayer.Minions, Has.Count.EqualTo(12),
                "Setup step 10 places one Ranged, four Melee and one Heavy per team.");
            Assert.That(
                fourPlayer.Minions.Select(minion =>
                    (minion.MinionId.Value, minion.TeamSide, minion.MinionType,
                        minion.SpawnPoint.SpawnPointId.Value)),
                Is.EqualTo(
                    sixPlayer.Minions.Select(minion =>
                        (minion.MinionId.Value, minion.TeamSide, minion.MinionType,
                            minion.SpawnPoint.SpawnPointId.Value))),
                "Step 10 says nothing about the player count, so the twelve placements are shared.");
        }

        [Test]
        public void TheSixPlayerBoard_IsUnchangedByTheFormatMenu()
        {
            var setup = CreateSetup(MatchFormat.ThreeVersusThreeQuick, out var configuration, out _);

            Assert.That(setup.Heroes, Has.Count.EqualTo(6));
            Assert.That(setup.WaveCount, Is.EqualTo(3));
            foreach (var team in setup.Teams)
            {
                Assert.That(team.LifeCount, Is.EqualTo(5));
            }

            Assert.That(configuration.CardDefinitions, Has.Count.EqualTo(108));
            Assert.That(
                setup.Heroes.Select(hero =>
                    (hero.HeroId.Value, hero.SpawnPoint.SpawnPointId.Value, hero.DisplayName)).ToArray(),
                Is.EqualTo(
                    QuickGameSetupConfiguration.CreateOfficialSixPlayer().Heroes
                        .Select(hero =>
                            (hero.HeroId.Value, hero.SpawnPoint.SpawnPointId.Value, hero.DisplayName))
                        .ToArray()),
                "The named six-player factory and the format-selected one compose the same board.");
        }

        [Test]
        public void EachOfTheFourSeats_HoldsItsOwnPrintedSetUnderItsOwnSeatPrefix()
        {
            var configuration = MatchConfiguration.CreateOfficialForInternalTest(
                MatchFormat.TwoVersusTwoQuick, TeamSide.Red);

            Assert.That(configuration.Participants.Players, Has.Count.EqualTo(4));
            Assert.That(configuration.Participants.Heroes, Has.Count.EqualTo(4));
            Assert.That(configuration.CardDefinitions, Has.Count.EqualTo(72),
                "Four printed sets of eighteen faces, where six seats deal 108.");
            Assert.That(configuration.CardInstances, Has.Count.EqualTo(72));

            foreach (var expected in ExpectedSeats)
            {
                var heroId = new HeroId(expected.HeroId);
                var owned = configuration.CardInstances
                    .Where(instance => instance.OwnerId.Equals(heroId))
                    .ToArray();
                Assert.That(owned, Has.Length.EqualTo(18),
                    expected.ContentId + " deals its own eighteen faces.");
                foreach (var instance in owned)
                {
                    Assert.That(instance.Id.Value, Does.StartWith(expected.InstancePrefix + "-"),
                        "An instance id names the seat holding the card, and a 2v2 seat is named " +
                        "exactly as the same chair is in a 3v3.");
                }
            }
        }

        [Test]
        public void AFourPlayerMatch_ComposesAndPlaysATurnWithFourAssignments()
        {
            var setup = CreateSetup(MatchFormat.TwoVersusTwoQuick, out var configuration, out _);
            var session = Compose(configuration, setup);

            Assert.That(session.Projection.Heroes, Has.Count.EqualTo(4));
            Assert.That(session.Projection.Cards, Has.Count.EqualTo(72));

            CommitEveryHand(session);
            var slot = session.Projection.TurnSlots.Single();
            Assert.That(slot.Assignments, Has.Count.EqualTo(4),
                "Every configured hero responds to every turn, and a 2v2 configures four.");
            Assert.That(slot.Assignments.Select(assignment => assignment.HeroId).Distinct().Count(),
                Is.EqualTo(4));
        }

        [Test]
        public void AFourPlayerMatch_SeatsFourAndBindsOneHeroToEach()
        {
            var configuration = MatchConfiguration.CreateOfficialForInternalTest(
                MatchFormat.TwoVersusTwoQuick, TeamSide.Red);
            var seats = MatchSeatBinding.ForParticipants(configuration.Participants);

            Assert.That(seats.Seats, Has.Count.EqualTo(4));
            Assert.That(seats.Seats.Select(seat => seat.Id.Value).ToArray(),
                Is.EquivalentTo(ExpectedSeats.Select(expected =>
                    OfficialFourPlayerParticipants.Seats
                        .Single(row => row.HeroId == expected.HeroId).PlayerIdValue).ToArray()));
            Assert.That(seats.Seats.Select(seat => seat.HeroId.Value).Distinct().Count(),
                Is.EqualTo(4), "No two seats hold the same hero.");
        }

        [Test]
        public void AFourPlayerMatch_KeepsRulebookPageSixteensDashboardLayoutForEveryHero()
        {
            var participants = OfficialFourPlayerParticipants.Create();

            Assert.That(participants.Teams, Has.Count.EqualTo(2));
            foreach (var hero in participants.Heroes)
            {
                Assert.That(hero.Dashboard.Slots, Has.Count.EqualTo(HeroDashboard.SlotCount),
                    "All six printed upgrade trees carry four item icons; dealing four of them " +
                    "changes nothing any of the four prints.");
            }
        }

        private static QuickGameSetupState CreateSetup(
            MatchFormat format, out MatchConfiguration configuration, out BoardMapDefinition map)
        {
            configuration = MatchConfiguration.CreateOfficialForInternalTest(format, TeamSide.Red);
            map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            return QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateOfficial(format),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
        }

        private static MatchSession Compose(
            MatchConfiguration configuration, QuickGameSetupState setup)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>Every hero's first held card, or a Pass when p.7 leaves it nothing to commit.</summary>
        private static void CommitEveryHand(MatchSession session)
        {
            foreach (var hero in session.Projection.Setup.Heroes.Select(value => value.HeroId).ToArray())
            {
                var card = session.Projection.Cards.FirstOrDefault(candidate =>
                    candidate.OwnerId.Equals(hero) && candidate.State.Location == CardLocation.Hand);
                var command = card == null
                    ? DirectorCommand.Pass(hero)
                    : DirectorCommand.CommitCard(hero, card.Id);
                Assert.That(session.Execute(command).Accepted, Is.True);
            }
        }
    }
}
