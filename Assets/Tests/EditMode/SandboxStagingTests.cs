using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The Director-only sandbox's staging surface. Milestone 8.35z17.
    ///
    /// <para>The subject of every case here is the same claim: a staged situation is composed the
    /// way a dealt match is composed, so everything downstream of composition cannot tell the two
    /// apart. That is why the load-bearing cases are not about staging at all - they are about Undo,
    /// restart and replay continuing to work over a staged board, which is what the user-approved
    /// amendment requires and what the snapshot-restore alternative would have lost.</para>
    /// </summary>
    [TestFixture]
    public sealed class SandboxStagingTests
    {
        private static BoardMapDefinition Map() =>
            OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);

        private static MatchConfiguration Configuration() =>
            MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);

        private static QuickGameSetupState Setup(MatchConfiguration configuration, BoardMapDefinition map) =>
            QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateOfficialSixPlayer(),
                configuration.Participants, map, configuration.TieBreakerState);

        /// <summary>A free playable space no dealt piece stands on, for a staged relocation.</summary>
        private static HexCoordinate FreeSpace(BoardMapDefinition map, QuickGameSetupState setup, int skip = 0)
        {
            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Select(hero => hero.Coordinate)
                    .Concat(setup.Minions.Select(minion => minion.Coordinate)));
            return map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable && !occupied.Contains(space.Coordinate))
                .OrderBy(space => space.Coordinate.Q).ThenBy(space => space.Coordinate.R)
                .Skip(skip)
                .First().Coordinate;
        }

        // ---------------------------------------------------------------------------------------
        // The seam is inert for every existing caller
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The whole composition seam this milestone adds is optional, and an empty staging must
        /// compose exactly what the dealt match composes. This is the case that says the change is
        /// additive rather than a new default: it compares the two snapshots byte for byte.
        /// </summary>
        [Test]
        public void AnEmptyStaging_ComposesTheDealtMatchByteForByte()
        {
            var map = Map();
            var dealtConfiguration = Configuration();
            var dealt = new MatchSession(ConfiguredInitialState(dealtConfiguration, map));

            var stagedConfiguration = Configuration();
            var staged = new MatchSession(SandboxMatchComposition.Compose(
                SandboxStagingDescription.Empty, map,
                Setup(stagedConfiguration, map), stagedConfiguration));

            Assert.That(staged.CaptureSnapshot(), Is.EqualTo(dealt.CaptureSnapshot()),
                "An empty staging must be indistinguishable from the dealt match, or the sandbox " +
                "seam has changed what every ordinary composition produces.");
        }

        [Test]
        public void AnUnstagedMatch_ComposesTheStartingLevelsAndAnEmptyPurse()
        {
            var map = Map();
            var configuration = Configuration();
            var session = new MatchSession(SandboxMatchComposition.Compose(
                SandboxStagingDescription.Empty, map, Setup(configuration, map), configuration));

            Assert.That(
                session.Projection.HeroProgression.Select(hero => hero.Level).Distinct(),
                Is.EqualTo(new[] { HeroLevel.Minimum }),
                "p.15 opens with 'Your hero starts the game at level 1'.");
            Assert.That(
                session.Projection.HeroProgression.Select(hero => hero.Coins).Distinct(),
                Is.EqualTo(new[] { 0 }));
        }

        // ---------------------------------------------------------------------------------------
        // Staging the board
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AStagedHero_StandsWhereTheDescriptionPutsThem()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var hero = setup.Heroes.First().HeroId;
            var destination = FreeSpace(map, setup);

            var session = new MatchSession(SandboxMatchComposition.Compose(
                new SandboxStagingDescription(
                    heroes: new[] { new SandboxHeroStaging(hero, destination) }),
                map, setup, configuration));

            Assert.That(
                session.Projection.Heroes.Single(value => value.HeroId.Equals(hero)).Coordinate,
                Is.EqualTo(destination));
        }

        [Test]
        public void AStagedMinion_IsRemovedSpawnedAndMovedAsTheDescriptionSays()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var removed = setup.Minions.First(minion => minion.TeamSide == TeamSide.Blue).MinionId;
            var moved = setup.Minions.First(minion => minion.TeamSide == TeamSide.Red);
            var movedTo = FreeSpace(map, setup);
            var spawnedAt = FreeSpace(map, setup, 1);
            var spawned = new MinionId("sandbox-melee-99");

            var session = new MatchSession(SandboxMatchComposition.Compose(
                new SandboxStagingDescription(
                    spawnedMinions: new[]
                    {
                        new SandboxMinionStaging(
                            spawned, TeamSide.Blue, StandardMinionType.Melee, spawnedAt)
                    },
                    removedMinions: new[] { removed },
                    movedMinions: new[]
                    {
                        new KeyValuePair<MinionId, HexCoordinate>(moved.MinionId, movedTo)
                    }),
                map, setup, configuration));

            var minions = session.Projection.Minions;
            Assert.That(minions.Any(minion => minion.MinionId.Equals(removed)), Is.False,
                "A removed minion is not on the staged board.");
            Assert.That(
                minions.Single(minion => minion.MinionId.Equals(spawned)).Coordinate,
                Is.EqualTo(spawnedAt));
            Assert.That(
                minions.Single(minion => minion.MinionId.Equals(moved.MinionId)).Coordinate,
                Is.EqualTo(movedTo));
        }

        [Test]
        public void AStagedSpawn_TakesASpawnPointNoOtherMinionHolds()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var staged = SandboxStagingAuthority.Stage(
                new SandboxStagingDescription(spawnedMinions: new[]
                {
                    new SandboxMinionStaging(
                        new MinionId("sandbox-heavy-99"), TeamSide.Red, StandardMinionType.Heavy,
                        FreeSpace(map, setup))
                }),
                map, setup, configuration);

            var spawnPoints = staged.Setup.Minions.Select(minion => minion.SpawnPoint.SpawnPointId).ToArray();
            Assert.That(spawnPoints.Distinct().Count(), Is.EqualTo(spawnPoints.Length),
                "p.8's Lane Push returns a removed minion to its own spawn point, so two minions " +
                "sharing one would be two pieces owed one space.");
        }

        [Test]
        public void StagedWaveAndLifeCounters_AreWhatTheMatchBeginsWith()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var session = new MatchSession(SandboxMatchComposition.Compose(
                new SandboxStagingDescription(
                    waveCount: 1,
                    teamLifeCounts: new[]
                    {
                        new KeyValuePair<TeamSide, int>(TeamSide.Red, 2),
                        new KeyValuePair<TeamSide, int>(TeamSide.Blue, 4)
                    }),
                map, setup, configuration));

            var counters = session.Projection.TeamLifeCounters;
            Assert.That(counters.Count, Is.EqualTo(2));
            Assert.That(counters.Select(counter => counter.Value).OrderBy(value => value),
                Is.EqualTo(new[] { 2, 4 }));
        }

        // ---------------------------------------------------------------------------------------
        // Staging progression - the composition seam this milestone adds
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AStagedLevelAndPurse_AreWhatTheHeroBeginsWith()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var hero = setup.Heroes.First().HeroId;

            var session = new MatchSession(SandboxMatchComposition.Compose(
                new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(hero, level: 4, coins: 11)
                }),
                map, setup, configuration));

            var progression = session.Projection.HeroProgression.Single(value => value.HeroId.Equals(hero));
            Assert.That(progression.Level, Is.EqualTo(4));
            Assert.That(progression.Coins, Is.EqualTo(11));
            Assert.That(
                session.Projection.HeroProgression
                    .Where(value => !value.HeroId.Equals(hero))
                    .Select(value => value.Level)
                    .Distinct(),
                Is.EqualTo(new[] { HeroLevel.Minimum }),
                "Staging one hero states nothing about the others, who begin where p.15 puts them.");
        }

        [Test]
        public void AStagedPurse_CarriesNoAwardedDefeat()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var staged = new SandboxStagingDescription(heroes: new[]
            {
                new SandboxHeroStaging(setup.Heroes.First().HeroId, coins: 7)
            });

            var progression = SandboxStagingAuthority.Stage(staged, map, setup, configuration).Progression;
            var ledger = progression.CreateCoinLedgerState();

            Assert.That(ledger.AwardedOutcomeIds, Is.Empty,
                "Staged coins are simply there; no p.13 Defeat paid them out, so none may be " +
                "recorded as awarded or a later Defeat could be refused as a repeat.");
        }

        // ---------------------------------------------------------------------------------------
        // Staging a hand
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AStagedHand_HoldsExactlyTheCardsNamed()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var hero = setup.Heroes.First().HeroId;
            var chosen = configuration.CardInstances
                .Where(card => card.OwnerId.Equals(hero))
                .Select(card => card.DefinitionReference.DefinitionId)
                .Distinct()
                .Take(2)
                .ToArray();

            var session = new MatchSession(SandboxMatchComposition.Compose(
                new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(hero, hand: chosen)
                }),
                map, setup, configuration));

            var held = session.Projection.Cards
                .Where(card => card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand)
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            Assert.That(held, Is.EqualTo(chosen.Select(card => card.Value)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray()));
        }

        [Test]
        public void AnEmptyStagedHand_LeavesTheHeroHoldingNothing()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var hero = setup.Heroes.First().HeroId;

            var session = new MatchSession(SandboxMatchComposition.Compose(
                new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(hero, hand: Array.Empty<CardDefinitionId>())
                }),
                map, setup, configuration));

            Assert.That(
                session.Projection.Cards.Any(card =>
                    card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand),
                Is.False,
                "An empty staged hand is a stated position, not an omission: it is how a fixture " +
                "reaches a hero who cannot pay for a Defense.");
        }

        // ---------------------------------------------------------------------------------------
        // THE LOAD-BEARING CASES: a staged match is an ordinary match
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The amendment requires Undo, restart and the event log to work over a staged situation
        /// "as they already do in Director mode". Restart is the sharpest of the three, because it
        /// is the one that could silently return to the DEALT board - which would throw away the
        /// staging a tester had just built and would look like the sandbox forgetting itself.
        /// </summary>
        [Test]
        public void ARestartReturnsToTheStagedBoard_AndNotToTheDealtOne()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var hero = setup.Heroes.First().HeroId;
            var destination = FreeSpace(map, setup);

            var session = new MatchSession(SandboxMatchComposition.Compose(
                new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(hero, destination, level: 3, coins: 9)
                }),
                map, setup, configuration));
            var staged = session.CaptureSnapshot();

            var command = session.Projection.AcceptedCommands;
            var commitment = FirstAcceptedCommand(session);
            Assert.That(commitment, Is.Not.Null, "The staged board must be playable at all.");

            var restarted = session.Restart();
            Assert.That(restarted.Accepted, Is.True, restarted.Detail);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(staged),
                "A restart returns to the situation the sandbox staged, byte for byte.");
            Assert.That(
                session.Projection.Heroes.Single(value => value.HeroId.Equals(hero)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(
                session.Projection.HeroProgression.Single(value => value.HeroId.Equals(hero)).Level,
                Is.EqualTo(3),
                "A staged Level is part of the staged situation, so a restart restores it too.");
        }

        [Test]
        public void AnUndoOverAStagedBoard_RewindsToTheStagedSituation()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var session = new MatchSession(SandboxMatchComposition.Compose(
                new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(setup.Heroes.First().HeroId, coins: 6)
                }),
                map, setup, configuration));
            var staged = session.CaptureSnapshot();

            Assert.That(FirstAcceptedCommand(session), Is.Not.Null);
            var undone = session.Undo();

            Assert.That(undone.Accepted, Is.True, undone.Detail);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(staged),
                "Undo over a staged board rewinds to the staged situation. A snapshot restore " +
                "would have made this impossible: it clears accepted history and stops recording " +
                "it, which is exactly why the staging is composed rather than restored.");
        }

        [Test]
        public void AStagedSession_RecordsAcceptedCommandsForReplay()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var session = new MatchSession(SandboxMatchComposition.Compose(
                new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(setup.Heroes.First().HeroId, level: 2)
                }),
                map, setup, configuration));

            Assert.That(session.Projection.IsHistoryAvailable, Is.True,
                "A staged session records history like any other. This is the property a snapshot " +
                "restore gives up, and the reason the sandbox does not use one.");
            Assert.That(FirstAcceptedCommand(session), Is.Not.Null);
            Assert.That(session.Projection.AcceptedCommands, Is.Not.Empty);
        }

        /// <summary>
        /// The amendment's central promise: a staged situation is played out "with the ordinary
        /// authoritative rules". Every case above proves the staging composed; this one proves the
        /// rules can actually resolve the board it composed, by driving a real seeded walk through
        /// the seats over a board no dealt setup would ever produce - two heroes relocated, a minion
        /// removed, a minion spawned, one hero levelled with coins to spend and one holding a hand
        /// of two.
        ///
        /// <para>It matters because staging bypasses p.6's setup restrictions deliberately, so the
        /// question "is a staged board still a board the rules can run" is a real one and not a
        /// formality. A wedge would show up here as a walk that stops far short of its budget.</para>
        /// </summary>
        [Test]
        public void AThoroughlyStagedBoard_IsPlayedOutByTheOrdinaryRules()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var first = setup.Heroes.First();
            var second = setup.Heroes.Last();
            var hand = configuration.CardInstances
                .Where(card => card.OwnerId.Equals(first.HeroId))
                .Select(card => card.DefinitionReference.DefinitionId)
                .Distinct().Take(2).ToArray();

            var host = MatchHost.Compose(SandboxMatchComposition.Compose(
                new SandboxStagingDescription(
                    heroes: new[]
                    {
                        new SandboxHeroStaging(first.HeroId, FreeSpace(map, setup), 4, 15, hand),
                        new SandboxHeroStaging(second.HeroId, FreeSpace(map, setup, 1))
                    },
                    spawnedMinions: new[]
                    {
                        new SandboxMinionStaging(
                            new MinionId("sandbox-ranged-99"), TeamSide.Red,
                            StandardMinionType.Ranged, FreeSpace(map, setup, 2))
                    },
                    removedMinions: new[]
                    {
                        setup.Minions.First(minion => minion.TeamSide == TeamSide.Blue).MinionId
                    }),
                map, setup, configuration));

            var submitted = OwnerScopedProjectionTests.DriveThroughSeats(host, 300, seed: 4);

            Assert.That(submitted, Is.GreaterThan(50),
                "A staged board must be one the rules can resolve. A walk that stops this early " +
                "means the staging composed a position the authority cannot play out, which would " +
                "make the whole test environment worthless.");
            // Not compared to `submitted`: an agreement is driven through its own submission path
            // and does not contribute one accepted command per submission, so equality here would
            // be asserting a fact about the driver rather than about the staged board.
            Assert.That(host.Projection.AcceptedCommands, Is.Not.Empty);
            Assert.That(host.Projection.IsHistoryAvailable, Is.True,
                "A staged match records its history like any other, so the walk it just played " +
                "back is replayable and undoable.");
        }

        // ---------------------------------------------------------------------------------------
        // Refusals, each by its own named reason
        // ---------------------------------------------------------------------------------------

        [Test]
        public void EveryIllFormedStaging_IsRefusedByItsOwnName()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var hero = setup.Heroes.First().HeroId;
            var minion = setup.Minions.First();
            var free = FreeSpace(map, setup);

            AssertRefused(SandboxStagingRejectionReason.UnknownHero, map, setup, configuration,
                new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(new HeroId("nobody"), free)
                }));

            AssertRefused(SandboxStagingRejectionReason.UnknownMinion, map, setup, configuration,
                new SandboxStagingDescription(removedMinions: new[] { new MinionId("nobody") }));

            AssertRefused(SandboxStagingRejectionReason.DuplicateMinionId, map, setup, configuration,
                new SandboxStagingDescription(spawnedMinions: new[]
                {
                    new SandboxMinionStaging(minion.MinionId, TeamSide.Red, StandardMinionType.Melee, free)
                }));

            AssertRefused(SandboxStagingRejectionReason.UnplayableSpace, map, setup, configuration,
                new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(hero, new HexCoordinate(999, 999))
                }));

            AssertRefused(SandboxStagingRejectionReason.OccupancyConflict, map, setup, configuration,
                new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(hero, minion.Coordinate)
                }));

            AssertRefused(SandboxStagingRejectionReason.CardNotOwnedByHero, map, setup, configuration,
                new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(hero, hand: new[] { new CardDefinitionId("not-a-card") })
                }));

            AssertRefused(SandboxStagingRejectionReason.IllegalLevel, map, setup, configuration,
                new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(hero, level: HeroLevel.Maximum + 1)
                }));

            AssertRefused(SandboxStagingRejectionReason.IllegalCoinBalance, map, setup, configuration,
                new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(hero, coins: -1)
                }));
        }

        [Test]
        public void AHeroStagedTwice_IsRefusedWhenTheDescriptionIsBuilt()
        {
            var exception = Assert.Throws<SandboxStagingException>(() => new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(new HeroId("a"), level: 2),
                    new SandboxHeroStaging(new HeroId("a"), level: 3)
                }));
            Assert.That(exception.Reason, Is.EqualTo(SandboxStagingRejectionReason.DuplicateHeroStaging));
        }

        // ---------------------------------------------------------------------------------------
        // The description is a file
        // ---------------------------------------------------------------------------------------

        [Test]
        public void AStagingDescription_RoundTripsThroughItsOwnText()
        {
            var original = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(
                        new HeroId("red-hero-01"), new HexCoordinate(-3, 1), 5, 12,
                        new[] { new CardDefinitionId("card-a"), new CardDefinitionId("card-b") }),
                    new SandboxHeroStaging(new HeroId("blue-hero-01"), coins: 3)
                },
                spawnedMinions: new[]
                {
                    new SandboxMinionStaging(
                        new MinionId("m99"), TeamSide.Blue, StandardMinionType.Ranged,
                        new HexCoordinate(0, 2))
                },
                removedMinions: new[] { new MinionId("red-heavy-01") },
                movedMinions: new[]
                {
                    new KeyValuePair<MinionId, HexCoordinate>(
                        new MinionId("blue-melee-01"), new HexCoordinate(1, -1))
                },
                waveCount: 2,
                teamLifeCounts: new[] { new KeyValuePair<TeamSide, int>(TeamSide.Red, 4) });

            Assert.That(SandboxStagingDescription.TryParse(original.Serialize(), out var parsed, out var error),
                Is.True, error);
            Assert.That(parsed.Serialize(), Is.EqualTo(original.Serialize()),
                "A staged situation is meant to be saved and re-loaded, so the text must be a " +
                "fixed point.");
            Assert.That(original.Serialize(), Is.EqualTo(original.Serialize()));
        }

        [Test]
        public void AStagingDescriptionText_IsRefusedByLineWhenItCannotBeRead()
        {
            Assert.That(
                SandboxStagingDescription.TryParse("not-the-format\n", out _, out var wrongTag), Is.False);
            Assert.That(wrongTag, Does.Contain(SandboxStagingDescription.FormatTag));

            Assert.That(SandboxStagingDescription.TryParse(
                SandboxStagingDescription.FormatTag + "\nteleport red-hero-01\n", out _, out var badLine),
                Is.False);
            Assert.That(badLine, Does.Contain("Line 2"),
                "A hand-edited staging file must be answerable by line rather than fatal.");
        }

        [Test]
        public void AParsedStagingDescription_StagesTheSameBoardAsTheBuiltOne()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var hero = setup.Heroes.First().HeroId;
            var destination = FreeSpace(map, setup);
            var built = new SandboxStagingDescription(heroes: new[]
            {
                new SandboxHeroStaging(hero, destination, level: 3, coins: 8)
            });

            Assert.That(
                SandboxStagingDescription.TryParse(built.Serialize(), out var parsed, out var error),
                Is.True, error);

            var fromText = new MatchSession(SandboxMatchComposition.Compose(
                parsed, map, Setup(Configuration(), map), Configuration()));
            var fromObject = new MatchSession(SandboxMatchComposition.Compose(
                built, map, Setup(Configuration(), map), Configuration()));

            Assert.That(fromText.CaptureSnapshot(), Is.EqualTo(fromObject.CaptureSnapshot()),
                "The text is the description, so composing either must produce one match.");
        }

        // ---------------------------------------------------------------------------------------
        // The amendment's architecture constraint, as a check
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The amendment's load-bearing constraint: the staging operations are a Director-only
        /// surface, "never offered to seats, never in the networked protocol". This states it as a
        /// check rather than as a comment.
        ///
        /// <para>It is also the reason the staging is a description rather than recorded commands.
        /// A recorded staging operation would have to be a <see cref="DirectorCommandKind"/>, and
        /// this build's conformance treats that enum as the network command vocabulary - every kind
        /// owes a codec round-trip specimen and a seat-authorization classification. So a staging
        /// command kind could not exist without reaching the wire.</para>
        /// </summary>
        [Test]
        public void NoStagingOperation_IsACommandKindOrReachesTheSeatVocabulary()
        {
            var stagingWords = new[] { "stage", "staging", "sandbox", "spawnminion", "setcoins", "setlevel" };
            foreach (var kind in Enum.GetNames(typeof(DirectorCommandKind)))
            {
                Assert.That(
                    stagingWords.Any(word => kind.ToLowerInvariant().Contains(word)), Is.False,
                    $"'{kind}' names a staging operation as a director command. Staging must not be " +
                    "a command kind: every kind is carried by NetworkCommandCodec and classified by " +
                    "SeatCommandOwnership, so a staging kind would be offered on the wire - which " +
                    "the amendment forbids.");
            }
        }

        /// <summary>
        /// Staging happens strictly before an authority exists, so there is no submission path for
        /// it at all. This asserts the shape rather than the absence of a word: a composed sandbox
        /// match binds the Director's own client, and the staged facts are already in the initial
        /// state by then.
        /// </summary>
        [Test]
        public void TheSandboxComposes_TheOrdinaryDirectorClientOverAStagedInitialState()
        {
            var map = Map();
            var configuration = Configuration();
            var setup = Setup(configuration, map);
            var description = new SandboxStagingDescription(heroes: new[]
            {
                new SandboxHeroStaging(setup.Heroes.First().HeroId, level: 2, coins: 5)
            });

            var initialState = SandboxMatchComposition.Compose(description, map, setup, Configuration());
            Assert.That(initialState.StagedProgression.IsEmpty, Is.False,
                "The staged position lives in the initial state, which is what a rebuild reads.");

            var client = SandboxMatchComposition.ComposeDirectorClient(
                description, map, Setup(Configuration(), map), Configuration());
            Assert.That(client, Is.InstanceOf<IMatchSession>());
            Assert.That(client.Projection.HeroProgression
                    .Single(hero => hero.HeroId.Equals(setup.Heroes.First().HeroId)).Coins,
                Is.EqualTo(5));
        }

        // ---------------------------------------------------------------------------------------

        private static MatchSessionInitialState ConfiguredInitialState(
            MatchConfiguration configuration, BoardMapDefinition map)
        {
            var setup = Setup(configuration, map);
            var scenario = new InitialScenarioDescription(
                configuration.Participants, configuration.CardDefinitions, configuration.CardInstances,
                configuration.TieBreakerState, configuration.SetupSeed, map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>());
            return new MatchSessionInitialState(scenario, setup, new[] { map }, configuration);
        }

        /// <summary>
        /// Plays the first command the staged board offers, so a case about Undo or restart has
        /// something to rewind. It is deliberately whatever the projection offers rather than a
        /// named command: the point is that a staged board is playable, not which card is played.
        /// </summary>
        private static DirectorCommand FirstAcceptedCommand(MatchSession session)
        {
            foreach (var hero in session.Projection.Commitments)
            {
                if (!hero.RevealedCardInstanceId.HasValue &&
                    hero.Status == TurnCardCommitmentStatus.AwaitingResponse)
                {
                    var card = session.Projection.Cards.FirstOrDefault(value =>
                        value.OwnerId.Equals(hero.HeroId) && value.State.Location == CardLocation.Hand);
                    if (card == null) continue;
                    var result = session.Execute(DirectorCommand.CommitCard(hero.HeroId, card.Id));
                    if (result.Accepted) return result.Command;
                }
            }

            return null;
        }

        private static void AssertRefused(
            SandboxStagingRejectionReason expected,
            BoardMapDefinition map,
            QuickGameSetupState setup,
            MatchConfiguration configuration,
            SandboxStagingDescription description)
        {
            var exception = Assert.Throws<SandboxStagingException>(
                () => SandboxStagingAuthority.Stage(
                    description, map, setup, MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red)),
                $"A staging that should have been refused as {expected} was accepted.");
            Assert.That(exception.Reason, Is.EqualTo(expected), exception.Message);
        }
    }
}
