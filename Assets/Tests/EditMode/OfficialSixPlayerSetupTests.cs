using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.25: rulebook p.5-6's official six-player Quick Game setup, which is what a player
    /// now loads.
    ///
    /// <para>Every case here drives the real configuration and the real setup authority against the
    /// official map resource, because the subject is the match a player is dealt rather than what a
    /// factory returns when asked. The staged one-hero-per-team boards are unchanged and keep their
    /// own fixtures; nothing here reads them.</para>
    /// </summary>
    public sealed class OfficialSixPlayerSetupTests
    {
        /// <summary>
        /// p.6 setup step 7's six dashboards, the seat each is dealt to, and the hero spawn point each
        /// hero stands on. Written out rather than read from the configuration under test, so a case
        /// that agreed with a changed configuration would still have to disagree with this.
        /// </summary>
        private static readonly (string HeroId, string ContentId, string DisplayName,
            TeamSide Side, string SpawnPoint)[] ExpectedSeats =
        {
            (OfficialSixPlayerParticipants.WaspHeroIdValue, "wasp", "Wasp the Warmaiden",
                TeamSide.Red, "red-hero-01"),
            (OfficialSixPlayerParticipants.XargathaHeroIdValue, "xargatha", "Xargatha the Changed",
                TeamSide.Red, "red-hero-02"),
            (OfficialSixPlayerParticipants.TigerclawHeroIdValue, "tigerclaw", "Tigerclaw the Cutpurse",
                TeamSide.Red, "red-hero-03"),
            (OfficialSixPlayerParticipants.ArienHeroIdValue, "arien", "Arien the Tidemaster",
                TeamSide.Blue, "blue-hero-01"),
            (OfficialSixPlayerParticipants.BroganHeroIdValue, "brogan", "Brogan the Destroyer",
                TeamSide.Blue, "blue-hero-02"),
            (OfficialSixPlayerParticipants.SabinaHeroIdValue, "sabina", "Sabina the Gunslinger",
                TeamSide.Blue, "blue-hero-03")
        };

        [Test]
        public void TheOfficialSetup_PlacesThreeDifferentHeroesOnEachSideAtTheirOwnThroneSpawnPoints()
        {
            var setup = CreateSetup(out var configuration, out var map);

            Assert.That(setup.Heroes, Has.Count.EqualTo(6), "p.5 splits six players into two teams of three.");
            foreach (var expected in ExpectedSeats)
            {
                var hero = setup.Heroes.Single(candidate =>
                    candidate.HeroId.Value == expected.HeroId);
                Assert.That(hero.ContentId.Value, Is.EqualTo(expected.ContentId));
                Assert.That(hero.DisplayName, Is.EqualTo(expected.DisplayName),
                    "The display label is p.6 setup step 7's own name for that dashboard.");
                Assert.That(hero.TeamSide, Is.EqualTo(expected.Side));
                Assert.That(hero.SpawnPoint.SpawnPointId.Value, Is.EqualTo(expected.SpawnPoint));

                // p.6 setup step 9 places every hero miniature on a marked spawn point in its own
                // Throne Zone. The authority already refuses anything else; this states which one.
                var space = map.Spaces.Single(candidate => candidate.Coordinate.Equals(hero.Coordinate));
                var zone = map.GetZoneForSpace(space.Id);
                Assert.That(zone.IsThrone, Is.True);
                Assert.That(zone.TeamId, Is.EqualTo(ThroneZoneQuery.GetMapTeamId(hero.TeamSide)));

                var player = configuration.Participants.GetPlayerForHero(hero.HeroId);
                var team = configuration.Participants.GetTeamForHero(hero.HeroId);
                Assert.That(team.Side, Is.EqualTo(expected.Side),
                    "The team a hero belongs to resolves through the participant graph, not through " +
                    "where the setup happened to place it.");
                Assert.That(configuration.Participants.GetPlayer(player.Id).TeamId,
                    Is.EqualTo(team.Id),
                    "and the controlling player belongs to that same team.");
            }

            Assert.That(setup.Heroes.Select(hero => hero.ContentId.Value).Distinct().Count(),
                Is.EqualTo(6), "All six heroes are different: `DEV-002`'s mirror is retired.");
            Assert.That(setup.Heroes.Count(hero => hero.ContentId.Value == "wasp"), Is.EqualTo(1),
                "Wasp appears once.");
            Assert.That(setup.Heroes.Count(hero => hero.TeamSide == TeamSide.Red), Is.EqualTo(3));
            Assert.That(setup.Heroes.Count(hero => hero.TeamSide == TeamSide.Blue), Is.EqualTo(3),
                "`DEV-001`'s one hero per team is retired: both teams field three.");
            Assert.That(
                setup.Heroes.Select(hero => hero.SpawnPoint.SpawnPointId).Distinct().Count(),
                Is.EqualTo(6), "and no two heroes share a spawn point.");
        }

        /// <summary>
        /// p.6's economy at six players: three Wave counters, five Life counters per team, and setup
        /// step 10's minions - which the rulebook states per team and not per player count, so they are
        /// the same twelve the staged board has always placed.
        /// </summary>
        [Test]
        public void TheOfficialSetup_UsesThreeWavesFiveLivesPerTeamAndSetupStepTensMinions()
        {
            var setup = CreateSetup(out _, out var map);

            Assert.That(setup.WaveCount, Is.EqualTo(3), "p.6 step 4: a Quick game uses three Waves.");
            Assert.That(setup.Teams, Has.Count.EqualTo(2));
            foreach (var team in setup.Teams)
            {
                Assert.That(team.LifeCount, Is.EqualTo(5),
                    "p.6 step 5's table: a Quick game at six players is five Life counters per team, " +
                    "where four players is four.");
            }

            Assert.That(setup.TieBreakerState.IsDefined, Is.True, "p.6 step 3 places the coin.");
            Assert.That(setup.Minions, Has.Count.EqualTo(12));
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var owned = setup.Minions.Where(minion => minion.TeamSide == side).ToArray();
                Assert.That(owned.Count(minion => minion.MinionType == StandardMinionType.Ranged),
                    Is.EqualTo(1), "p.6 step 10 places one Ranged minion per team.");
                Assert.That(owned.Count(minion => minion.MinionType == StandardMinionType.Melee),
                    Is.EqualTo(4), "four Melee,");
                Assert.That(owned.Count(minion => minion.MinionType == StandardMinionType.Heavy),
                    Is.EqualTo(1), "and one Heavy.");
                foreach (var minion in owned)
                {
                    var space = map.Spaces.Single(candidate =>
                        candidate.Coordinate.Equals(minion.Coordinate));
                    Assert.That(BattleZoneQuery.IsInStartingBattleZone(map, space.Id), Is.True,
                        "and every one of them stands in the starting Battle Zone.");
                }
            }
        }

        /// <summary>
        /// p.6's normal Tie Breaker flip, which the configuration a player loads makes rather than
        /// selecting a side under `DEV-005`. The seed it drew is recorded, so the same match can be
        /// carried out again from its own record - milestone 8.28b.
        /// </summary>
        [Test]
        public void TheConfigurationAPlayerLoads_FlipsTheTieBreakerAndRecordsTheDrawItMade()
        {
            var configuration = MatchConfiguration.Create();

            Assert.That(configuration.TieBreakerState.IsDefined, Is.True);
            Assert.That(configuration.SetupSeed.IsRecorded, Is.True,
                "A flipped coin records the draw it was derived from.");
            Assert.That(
                TieBreakerSetupAuthority.CreateNormalSetupState(configuration.SetupSeed).RevealedSide,
                Is.EqualTo(configuration.TieBreakerState.RevealedSide),
                "and the record lands on the same side when it is carried out again.");
        }

        /// <summary>
        /// Six hands, six dashboards, six owners: every card in the match resolves to one of p.5's six
        /// heroes, and every hero's four p.16 item slots are its own.
        /// </summary>
        [Test]
        public void EverySeat_ResolvesItsOwnHandAndDashboardWithoutAssumingOneHeroPerTeam()
        {
            var setup = CreateSetup(out var configuration, out _);
            var session = Compose(configuration, setup);
            var projection = session.Projection;

            Assert.That(projection.Heroes, Has.Count.EqualTo(6));
            Assert.That(projection.Cards, Has.Count.EqualTo(108));
            foreach (var expected in ExpectedSeats)
            {
                var heroId = new HeroId(expected.HeroId);
                var hero = configuration.Participants.GetHero(heroId);
                Assert.That(hero.Dashboard.Slots, Has.Count.EqualTo(4),
                    "p.16 gives every hero four item slots.");
                Assert.That(
                    projection.Cards.Count(card => card.OwnerId.Equals(heroId)),
                    Is.EqualTo(18),
                    "p.6 step 11 gives each player their hero's deck of eighteen Action cards.");
                Assert.That(
                    projection.Cards.Count(card =>
                        card.OwnerId.Equals(heroId) && card.State.Location == CardLocation.Hand),
                    Is.EqualTo(5),
                    "five of which are the starting hand.");
                Assert.That(projection.HeroProgression.Any(value => value.HeroId.Equals(heroId)),
                    Is.True,
                    "and every seat has its own p.15 progression, not one shared by a side.");
            }

            Assert.That(
                projection.Cards.Select(card => card.OwnerId).Distinct().Count(), Is.EqualTo(6),
                "Nothing in the dealt match belongs to a team rather than to a hero.");

            // The hero-owned components of the six sets. Tigerclaw's two Poison markers are the only
            // ones a printed face in this roster defines, and rulebook p.6 setup step 11 has the
            // player who holds that hero take them, so a configuration that dealt him without them
            // would compose a hero whose cards name a component the match never registered. Loaded
            // from this configuration's own declared catalog rather than from the build's.
            foreach (var marker in new[]
            {
                GuardsDigital.Core.Cards.Verified.TigerclawComponentContent.PoisonOneDefinition,
                GuardsDigital.Core.Cards.Verified.TigerclawComponentContent.PoisonTwoDefinition
            })
            {
                Assert.That(
                    configuration.ContentCatalogs.Components.IsRegistered(marker.Reference), Is.True,
                    $"The dealt roster's '{marker.Reference}' marker is content this match resolves " +
                    "against.");
            }
        }

        /// <summary>
        /// p.7's turn slot at six players: one ownership-validated assignment for every configured
        /// hero, each naming a committed card or an empty-hand Pass, in one deterministic order that
        /// survives a snapshot round trip.
        /// </summary>
        [Test]
        public void EveryTurnSlot_HoldsSixOwnershipValidatedAssignmentsThatSurviveASnapshot()
        {
            var setup = CreateSetup(out var configuration, out _);
            var session = Compose(configuration, setup);
            CommitEveryHand(session);
            ResolveTheOpenTurn(session);

            var slot = session.Projection.TurnSlots.Single();
            Assert.That(slot.Assignments, Has.Count.EqualTo(6),
                "Every configured hero responds to every turn.");
            Assert.That(
                slot.Assignments.Select(assignment => assignment.HeroId).Distinct().Count(),
                Is.EqualTo(6));
            foreach (var assignment in slot.Assignments)
            {
                Assert.That(
                    assignment.CardInstanceId.HasValue ^ assignment.PassedWithEmptyHand, Is.True,
                    "p.7 leaves exactly one of a committed card and an empty-hand Pass.");
                var player = assignment.GetControllingPlayerId(configuration.Participants);
                var side = assignment.GetTeamSide(configuration.Participants);
                Assert.That(
                    configuration.Participants.GetPlayerForHero(assignment.HeroId).Id, Is.EqualTo(player),
                    "The controlling player resolves through the participant graph.");
                Assert.That(
                    configuration.Participants.GetTeamForHero(assignment.HeroId).Side, Is.EqualTo(side));
                if (!assignment.CardInstanceId.HasValue)
                {
                    continue;
                }

                var card = session.Projection.Cards.Single(candidate =>
                    candidate.Id.Equals(assignment.CardInstanceId.Value));
                Assert.That(card.OwnerId, Is.EqualTo(assignment.HeroId),
                    "and a committed card is owned by the hero it is assigned to.");
            }

            var before = SlotFacts(session.Projection.TurnSlots);
            var payload = session.CaptureSnapshot();
            var restored = Compose(configuration, setup);
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);
            Assert.That(SlotFacts(restored.Projection.TurnSlots), Is.EqualTo(before),
                "and the whole slot - order, owners, committed cards and Passes - comes back as it went.");
        }

        /// <summary>
        /// The complete six-player setup round-trips deterministically: the same payload out, the same
        /// payload back, and the same board, participants and economy inside it.
        /// </summary>
        [Test]
        public void TheCompleteSixPlayerSetup_RoundTripsThroughASnapshotByteForByte()
        {
            var setup = CreateSetup(out var configuration, out _);
            var session = Compose(configuration, setup);
            var payload = session.CaptureSnapshot();

            var restored = Compose(configuration, setup);
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload),
                "A restored six-player setup captures the payload it was restored from.");

            var restoredSetup = restored.Projection.Setup;
            Assert.That(restoredSetup.WaveCount, Is.EqualTo(3));
            Assert.That(restoredSetup.Teams.Select(team => team.LifeCount), Is.EqualTo(new[] { 5, 5 }));
            Assert.That(
                restoredSetup.Heroes.Select(hero => hero.HeroId.Value),
                Is.EqualTo(setup.Heroes.Select(hero => hero.HeroId.Value)),
                "in the same deterministic order.");
            Assert.That(
                restoredSetup.Heroes.Select(hero => hero.ContentId.Value),
                Is.EqualTo(setup.Heroes.Select(hero => hero.ContentId.Value)));
            // Keyed by hero rather than by index: the projection's occupancy list is ordered by hero ID
            // after a restore and by setup order before one, which is a presentation-facing ordering
            // difference rather than a state one - the payload above is byte-identical either way.
            Assert.That(
                restored.Projection.Heroes
                    .OrderBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                    .Select(hero => $"{hero.HeroId.Value}@{hero.Coordinate}"),
                Is.EqualTo(session.Projection.Heroes
                    .OrderBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                    .Select(hero => $"{hero.HeroId.Value}@{hero.Coordinate}")),
                "Every hero comes back on the space it was placed on.");
        }

        // -----------------------------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------------------------

        private static QuickGameSetupState CreateSetup(
            out MatchConfiguration configuration, out BoardMapDefinition map)
        {
            configuration = MatchConfiguration
                .CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            return QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateOfficialSixPlayer(),
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

        /// <summary>
        /// Holds through the open turn, answering the tied-Initiative decisions six revealed cards make
        /// ordinary. The budget is stated so a turn that stops making progress fails here instead of
        /// looping.
        /// </summary>
        private static void ResolveTheOpenTurn(MatchSession session)
        {
            var budget = 64;
            while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn)
            {
                Assert.That(budget--, Is.GreaterThan(0), "The turn did not resolve inside its budget.");
                var tie = session.Projection.PendingInitiativeChoice;
                // Milestone 8.35g. Six seats mean six releases a turn, and the release has to be
                // chosen ABOVE the Hold's selection read: the pause is exactly the state in which
                // that selection is empty.
                var command =
                    session.Projection.PendingTurnRelease != null
                        ? DirectorCommand.ReleaseTurn()
                    : tie != null
                        ? DirectorCommand.ChooseTiedActor(tie.OwningSide, tie.CandidateCardInstanceIds[0])
                    : DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId);
                Assert.That(session.Execute(command).Accepted, Is.True);
            }
        }

        private static IEnumerable<string> SlotFacts(IEnumerable<RoundTurnSlot> slots) =>
            slots.SelectMany(slot => slot.Assignments.Select(assignment =>
                $"{slot.RoundIndex}/{slot.TurnIndex}/{assignment.HeroId.Value}/" +
                $"{assignment.CardInstanceId?.Value ?? "pass"}/{assignment.PassedWithEmptyHand}"))
                .ToArray();
    }
}
