using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.10. Rulebook p.8, "Push the Lane": "1. Flip one Wave counter face-down. If the last
    /// remaining Wave counter is flipped, the game ends immediately and the team who won this final push
    /// wins the game. 2. The next Zone towards the losing team's Throne becomes the new Battle Zone. If
    /// the enemy Throne Zone itself becomes the new Battle Zone, the team who won the push wins the
    /// game. 3. Remove all minions in the previous Battle Zone. Respawn one minion of the matching type
    /// in each Spawn point in the new Battle Zone".
    ///
    /// This milestone owns the first two steps and stops. Everything here therefore checks a prefix
    /// rather than a transition: the third step is reported as unimplemented rather than skipped, and
    /// the board still holds every minion it held before, which is what makes "no partial transition"
    /// an observation rather than a promise.
    ///
    /// The official map is used throughout, because the Lane is map content: its ordered Lane runs
    /// red-throne, red-approach, starting-battle-zone, blue-approach, blue-throne, so a Battle Zone in
    /// the middle is two pushes from either Throne and the two directions are genuinely different
    /// answers rather than the same one mirrored.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class LanePushOrderedPrefixTests
    {
        private static readonly ZoneId RedThrone = new ZoneId("red-throne");
        private static readonly ZoneId RedApproach = new ZoneId("red-approach");
        private static readonly ZoneId StartingBattleZone = new ZoneId("starting-battle-zone");
        private static readonly ZoneId BlueApproach = new ZoneId("blue-approach");
        private static readonly ZoneId BlueThrone = new ZoneId("blue-throne");

        // -------------------------------------------------------------------------------------------
        // The Wave stage
        // -------------------------------------------------------------------------------------------

        [Test]
        public void OnePush_FlipsExactlyOneWaveCounter()
        {
            var board = CreateOfficialBoard();
            var transition = new LanePushTransitionAuthority(board, 3);

            var result = Push(transition, TeamSide.Red, TeamSide.Blue);

            Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
            Assert.That(result.RemainingWaveCounters, Is.EqualTo(2),
                "p.8 flips one counter per push, not one per Zone crossed and not one per minion.");
            Assert.That(transition.RemainingWaveCounters, Is.EqualTo(2));
            Assert.That(transition.HasEnded, Is.False);
        }

        [Test]
        public void FlippingTheLastWaveCounter_EndsTheGameForTheTeamThatWonThatPush()
        {
            var board = CreateOfficialBoard();
            var transition = new LanePushTransitionAuthority(board, 1);

            var result = Push(transition, TeamSide.Blue, TeamSide.Red);

            Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
            Assert.That(result.EndedTheGame, Is.True);
            Assert.That(result.GameEndingCause, Is.EqualTo(LanePushGameEndingCause.FinalWaveFlipped));
            Assert.That(result.WinningSide, Is.EqualTo(TeamSide.Blue),
                "p.8: 'the team who won this final push wins the game', which is the pushing team and " +
                "not the team whose Throne the Lane happens to be nearest.");
            Assert.That(transition.RemainingWaveCounters, Is.EqualTo(0));
            Assert.That(transition.HasEnded, Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // The Zone stage
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheZoneStage_AdvancesOneZoneTowardsTheLosingTeamsThrone()
        {
            var board = CreateOfficialBoard();
            var transition = new LanePushTransitionAuthority(board, 3);

            var result = Push(transition, TeamSide.Red, TeamSide.Blue);

            Assert.That(result.BattleZoneId, Is.EqualTo(BlueApproach),
                "Blue lost, so the Lane moves one Zone towards the Blue Throne.");
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(BlueApproach),
                "The advance is a commit against the board, not a value the result carries alone.");
        }

        [Test]
        public void TheDirection_IsTheLosingTeamsThroneRatherThanAFixedOne()
        {
            var board = CreateOfficialBoard();
            var transition = new LanePushTransitionAuthority(board, 3);

            Push(transition, TeamSide.Blue, TeamSide.Red);

            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(RedApproach),
                "Red lost this time, so the same starting Zone advances the other way. A Lane that " +
                "only ever moved one direction would pass every single-sided fixture.");
        }

        [Test]
        public void TheZoneAdvances_ExactlyOncePerPush()
        {
            var board = CreateOfficialBoard();
            var transition = new LanePushTransitionAuthority(board, 5);

            Push(transition, TeamSide.Red, TeamSide.Blue);
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(BlueApproach));

            Push(transition, TeamSide.Blue, TeamSide.Red);

            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(StartingBattleZone),
                "A push the other way steps back one Zone rather than jumping to the far end, so the " +
                "Lane is a sequence the Battle Zone walks rather than a pair of destinations.");
        }

        [Test]
        public void ArrivingAtTheLosingTeamsThrone_EndsTheGameByCapture()
        {
            var board = CreateOfficialBoard();
            var transition = new LanePushTransitionAuthority(board, 5);

            Push(transition, TeamSide.Red, TeamSide.Blue);
            var capture = Push(transition, TeamSide.Red, TeamSide.Blue);

            Assert.That(capture.Accepted, Is.True, capture.RejectionReason.ToString());
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(BlueThrone));
            Assert.That(capture.GameEndingCause, Is.EqualTo(LanePushGameEndingCause.ThroneCaptured),
                "p.8: 'If the enemy Throne Zone itself becomes the new Battle Zone, the team who won " +
                "the push wins the game.'");
            Assert.That(capture.WinningSide, Is.EqualTo(TeamSide.Red));
            Assert.That(capture.RemainingWaveCounters, Is.EqualTo(3),
                "The capture still cost a Wave counter, because p.8 flips one before the Zone moves.");
        }

        // -------------------------------------------------------------------------------------------
        // Ordering: an early game-ending result prevents later stages
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheFinalWaveEndsTheGame_BeforeTheZoneStageRuns()
        {
            var board = CreateOfficialBoard();
            var transition = new LanePushTransitionAuthority(board, 1);

            var result = Push(transition, TeamSide.Red, TeamSide.Blue);

            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(StartingBattleZone),
                "p.8 ends the game 'immediately' on the last Wave counter, so the Battle Zone never " +
                "moves. This is the observation rather than a reading of the control flow: had the " +
                "Zone stage run anyway, the board would say so.");
            Assert.That(
                result.Outcomes.Select(outcome => outcome.Stage),
                Is.EqualTo(new[] { LanePushTransitionStage.FlipWaveCounter }),
                "The later steps are absent because they did not run, not reported as skipped.");
            Assert.That(result.BattleZoneId, Is.EqualTo(StartingBattleZone));
        }

        [Test]
        public void AThroneCapture_LeavesNoStepAfterIt()
        {
            var board = CreateOfficialBoard();
            var transition = new LanePushTransitionAuthority(board, 5);
            Push(transition, TeamSide.Blue, TeamSide.Red);

            var capture = Push(transition, TeamSide.Blue, TeamSide.Red);

            Assert.That(capture.Outcomes, Is.EqualTo(new[]
            {
                LanePushStageOutcome.Completed(LanePushTransitionStage.FlipWaveCounter),
                LanePushStageOutcome.GameEnded(
                    LanePushTransitionStage.AdvanceBattleZone, LanePushGameEndingCause.ThroneCaptured)
            }), "The removal and respawn step is not reported at all, because a captured Throne ends " +
                "the game before p.8 reaches it.");
        }

        [Test]
        public void APushAfterTheGameHasEnded_IsRefusedAndChangesNothing()
        {
            var board = CreateOfficialBoard();
            var transition = new LanePushTransitionAuthority(board, 1);
            Push(transition, TeamSide.Red, TeamSide.Blue);

            var second = Push(transition, TeamSide.Red, TeamSide.Blue);

            Assert.That(second.Accepted, Is.False);
            Assert.That(second.RejectionReason, Is.EqualTo(LanePushPrefixRejectionReason.GameAlreadyEnded));
            Assert.That(second.Outcomes, Is.Empty);
            Assert.That(transition.RemainingWaveCounters, Is.EqualTo(0));
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(StartingBattleZone));
        }

        // -------------------------------------------------------------------------------------------
        // Deterministic typed outcomes
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AnOrdinaryPush_ReportsThePagesThreeStepsCompleted()
        {
            var board = CreateOfficialBoard();
            var transition = new LanePushTransitionAuthority(board, 3);

            var result = Push(transition, TeamSide.Red, TeamSide.Blue);

            Assert.That(result.Outcomes, Is.EqualTo(new[]
            {
                LanePushStageOutcome.Completed(LanePushTransitionStage.FlipWaveCounter),
                LanePushStageOutcome.Completed(LanePushTransitionStage.AdvanceBattleZone),
                LanePushStageOutcome.Completed(LanePushTransitionStage.RemoveAndRespawnMinions)
            }), "p.8's three steps, in the page's order and all of them run. Until milestone 6.11 the " +
                "third reported itself unimplemented, which was the difference between an ordered " +
                "prefix and a transition.");
            Assert.That(
                BattleZoneQuery.FindMinionsInside(board), Is.Not.Empty,
                "The same statement measured on the board rather than read off the outcome list: the " +
                "new Battle Zone is filled.");
        }

        [Test]
        public void TheSameStateAndSides_ProduceTheSameOutcomesEveryTime()
        {
            var first = Resolve(CreateOfficialBoard(), 3, TeamSide.Red, TeamSide.Blue);
            var second = Resolve(CreateOfficialBoard(), 3, TeamSide.Red, TeamSide.Blue);

            Assert.That(second.Outcomes, Is.EqualTo(first.Outcomes));
            Assert.That(second.RemainingWaveCounters, Is.EqualTo(first.RemainingWaveCounters));
            Assert.That(second.BattleZoneId, Is.EqualTo(first.BattleZoneId));
            Assert.That(second.GameEndingCause, Is.EqualTo(first.GameEndingCause));
        }

        // -------------------------------------------------------------------------------------------
        // Refusals, each leaving the match exactly as it was
        // -------------------------------------------------------------------------------------------

        [Test]
        public void OneSideCannotWinAndLoseTheSamePush()
        {
            var board = CreateOfficialBoard();
            var transition = new LanePushTransitionAuthority(board, 3);

            var same = Push(transition, TeamSide.Red, TeamSide.Red);
            var undefined = Push(transition, TeamSide.Red, TeamSide.None);

            Assert.That(same.RejectionReason, Is.EqualTo(LanePushPrefixRejectionReason.UndeclaredSides));
            Assert.That(undefined.RejectionReason, Is.EqualTo(LanePushPrefixRejectionReason.UndeclaredSides));
            Assert.That(transition.RemainingWaveCounters, Is.EqualTo(3),
                "A refused push flips nothing, so the pool is untouched.");
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(StartingBattleZone));
        }

        [Test]
        public void ABoardWithNoBattleZone_HasNoLaneToPush()
        {
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(
                    new MapId("lane-push-no-zone"), 1, new[] { new HexCoordinate(0, 0) }),
                Array.Empty<HeroOccupancy>(),
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());
            var transition = new LanePushTransitionAuthority(board, 3);

            var result = Push(transition, TeamSide.Red, TeamSide.Blue);

            Assert.That(result.RejectionReason,
                Is.EqualTo(LanePushPrefixRejectionReason.NoCurrentBattleZone));
            Assert.That(transition.RemainingWaveCounters, Is.EqualTo(3));
        }

        [Test]
        public void ABattleZoneOffTheOrderedLane_IsRefusedRatherThanGuessedAt()
        {
            // The two-zone synthetic map flags a Battle Zone and declares no ordered Lane at all, so
            // "the next Zone towards the losing team's Throne" names nothing this map has.
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSyntheticWithStartingBattleZone(
                    new MapId("lane-push-no-lane"),
                    1,
                    new[] { new HexCoordinate(0, 0) },
                    new[] { new HexCoordinate(1, 0) },
                    Array.Empty<HexCoordinate>()),
                Array.Empty<HeroOccupancy>(),
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());
            var transition = new LanePushTransitionAuthority(board, 3);

            var result = Push(transition, TeamSide.Red, TeamSide.Blue);

            Assert.That(result.RejectionReason,
                Is.EqualTo(LanePushPrefixRejectionReason.BattleZoneOffTheLane));
            Assert.That(board.CurrentBattleZoneId, Is.Not.Null,
                "The board still has its Battle Zone; what it lacks is a Lane to move it along.");
        }

        [Test]
        public void ALaneWhoseThronesBelongToNeitherSide_GivesTheAdvanceNoDirection()
        {
            var board = new AdjacentMoveAuthority(
                CreateUnownedThroneLaneMap(),
                Array.Empty<HeroOccupancy>(),
                Array.Empty<ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());
            var transition = new LanePushTransitionAuthority(board, 3);

            var result = Push(transition, TeamSide.Red, TeamSide.Blue);

            Assert.That(result.RejectionReason,
                Is.EqualTo(LanePushPrefixRejectionReason.UnavailableLosingThrone),
                "p.8 advances towards the losing team's Throne, so a map that gives that side no " +
                "Throne is refused rather than pushed in an arbitrary direction.");
            Assert.That(transition.RemainingWaveCounters, Is.EqualTo(3));
        }

        [Test]
        public void APushIntoAThroneTheLaneHasAlreadyReached_IsRefused()
        {
            var board = CreateOfficialBoard();
            Push(new LanePushTransitionAuthority(board, 5), TeamSide.Red, TeamSide.Blue);
            Push(new LanePushTransitionAuthority(board, 5), TeamSide.Red, TeamSide.Blue);
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(BlueThrone));

            var afterwards = Push(
                new LanePushTransitionAuthority(board, 5), TeamSide.Red, TeamSide.Blue);

            Assert.That(afterwards.RejectionReason,
                Is.EqualTo(LanePushPrefixRejectionReason.BattleZoneAlreadyCaptured),
                "The Lane is already in Blue's Throne, so there is no next Zone towards it. A fresh " +
                "authority is used deliberately: the one that captured it refuses on its own ended " +
                "game, which would prove nothing about the board.");
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(BlueThrone));
        }

        // -------------------------------------------------------------------------------------------
        // The board's own verification of an advance
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheBoard_RefusesAnAdvanceThatIsNotOneStepAlongTheLane()
        {
            var board = CreateOfficialBoard();

            Assert.That(board.CommitValidatedBattleZoneAdvance(BlueThrone), Is.False,
                "Two Zones at once is not a step p.8 can take.");
            Assert.That(board.CommitValidatedBattleZoneAdvance(new ZoneId("top-jungle")), Is.False,
                "A jungle is a real zone of this map and is not on the Lane.");
            Assert.That(board.CommitValidatedBattleZoneAdvance(new ZoneId("no-such-zone")), Is.False);
            Assert.That(board.CommitValidatedBattleZoneAdvance(StartingBattleZone), Is.False,
                "Standing still is not an advance.");
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(StartingBattleZone));

            Assert.That(board.CommitValidatedBattleZoneAdvance(RedApproach), Is.True);
            Assert.That(board.CurrentBattleZoneId, Is.EqualTo(RedApproach));
        }

        // -------------------------------------------------------------------------------------------
        // Fixtures
        // -------------------------------------------------------------------------------------------

        private static LanePushPrefixResult Resolve(
            AdjacentMoveAuthority board, int waveCounters, TeamSide winning, TeamSide losing) =>
            Push(new LanePushTransitionAuthority(board, waveCounters), winning, losing);

        /// <summary>
        /// One complete p.8 push, driven the way a live match drives it: through the continuation
        /// authority, over the composed step rules.
        ///
        /// Milestone 6.10 drove the first two steps through a seam nothing else could reach, because
        /// the third did not exist. Milestone 6.11 implements the third, so there is no prefix left to
        /// run on its own and these cases exercise the whole transition. The push is asserted not to
        /// stop, because none of these boards has a Spawn point an obstacle is standing on; the cases
        /// that do are <c>LanePushTransitionTests</c>.
        /// </summary>
        private static LanePushPrefixResult Push(
            LanePushTransitionAuthority transition, TeamSide winning, TeamSide losing)
        {
            var transitions = new SystemTransitionAuthority(
                MatchSystemTransitions.CreateCatalog(),
                MatchSystemTransitions.CreateRules(transition, null));
            var result = new LanePushConsumption(transition, transitions).TryConsume(
                new LanePushRequestId("ordered-prefix-fixture"),
                winning,
                losing,
                new TieBreakerState(TeamSide.Red));

            Assert.That(result.IsPaused, Is.False,
                "None of these boards blocks a Spawn point with a non-token obstacle, so p.8's sidebar " +
                "asks nobody anything and the push runs to its end.");
            return result.Finished;
        }

        private static AdjacentMoveAuthority CreateOfficialBoard()
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                StagedBoardParticipants.Create(),
                map,
                new TieBreakerState(TeamSide.Red));
            return new AdjacentMoveAuthority(
                map,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>(),
                setup.Minions);
        }

        /// <summary>
        /// A three-zone Lane whose Throne endpoints belong to no team the match has. The map format
        /// names a zone's owner as a map-local string, so this is data a map really could carry.
        /// </summary>
        private static BoardMapDefinition CreateUnownedThroneLaneMap()
        {
            var mapId = new MapId("lane-push-unowned-thrones");
            var first = new ZoneId("first-throne");
            var middle = new ZoneId("middle");
            var last = new ZoneId("last-throne");
            var zones = new[]
            {
                new BoardMapZone(first, "First Throne", "throne", "green", true, false),
                new BoardMapZone(middle, "Middle", "lane", null, false, true),
                new BoardMapZone(last, "Last Throne", "throne", "purple", true, false)
            };
            var spaces = new List<BoardMapSpace>
            {
                Space(new HexCoordinate(0, 0), first),
                Space(new HexCoordinate(1, 0), middle),
                Space(new HexCoordinate(2, 0), last)
            };
            return new BoardMapDefinition(
                mapId, 1, "Unowned Throne Lane", spaces, zones,
                Array.Empty<BoardMapSpawnPoint>(), new[] { first, middle, last });
        }

        private static BoardMapSpace Space(HexCoordinate at, ZoneId zoneId) =>
            new BoardMapSpace(
                new SpaceId($"q{at.Q}_r{at.R}"), at, BoardMapSpaceKind.Playable, zoneId);
    }
}
