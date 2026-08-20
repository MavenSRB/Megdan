using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.6b. p.13's Defeat and p.19's standalone Remove ask one shared decision about whether
    /// a removal happens, so a minion whose removal is prevented stays on the board with its Defeat
    /// recorded and its reward already collected, whichever operation attempted it.
    ///
    /// The prevented arm is exercised on state built directly rather than through a played Attack, and
    /// that is a property of the engine rather than a shortcut. The only prevention the rules can
    /// currently express is p.18's immunity, and an Attack filters an immune minion out of its legal
    /// targets and revalidates that filter immediately before the removal runs, so no legal Attack
    /// target can be immune by the time its removal stage is reached. What this milestone guarantees is
    /// that both removal paths will honour a prevention source on the day one exists; the general
    /// prevent-removal family named on p.13's Bolster example is still unimplemented for want of
    /// verified card text.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class DefeatRemovalStageTests
    {
        [Test]
        public void PreventedRemoval_RecordsTheDefeatTheRewardAndTheAttemptWithoutARemovedOccupancy()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));
            var accounting = new DefeatRewardAccounting(fixture.Configuration.Participants);
            var outcome = MinionDefeatOutcome(fixture, "blue-target", 2);
            var reward = accounting.Award(outcome);

            var defeat = MinionDefeatResolution.Prevented(
                outcome,
                reward,
                new DefeatRemovalAttempt(outcome.Id, outcome.TargetId),
                new DefeatRemovalResult(
                    outcome.Id, outcome.TargetId, DefeatRemovalResultKind.RemovalPrevented),
                LanePushQualification.None);

            Assert.That(defeat.Reward.Accepted, Is.True,
                "p.13's Bolster example prevents the removal and still collects the coins, so the " +
                "reward stage stays accepted rather than being unwound by the stage after it.");
            Assert.That(defeat.RemovalResult.Kind, Is.EqualTo(DefeatRemovalResultKind.RemovalPrevented));
            Assert.That(defeat.RemovalAttempt.TargetId, Is.EqualTo(outcome.TargetId),
                "The attempt is recorded whether or not it succeeded; that is why 5.5 made it its own fact.");
            Assert.That(defeat.RemovedMinion, Is.Null,
                "Nothing left the board, so naming a removed occupancy would name a piece that never moved.");
            Assert.That(defeat.Facts, Is.EqualTo(new[]
            {
                MinionDefeatFactKind.DefeatRecorded,
                MinionDefeatFactKind.RewardAwarded,
                MinionDefeatFactKind.RemovalAttempted,
                MinionDefeatFactKind.RemovalPrevented
            }), "The outcome is stated distinctly rather than by a fact going missing, which would read " +
                "as a resolution cut short instead of a removal that was stopped.");
        }

        [Test]
        public void PreventedRemoval_ProjectsItsOwnResolutionEventRatherThanTheCompletedOne()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));
            var defeat = PreventedDefeat(fixture, "blue-target");
            var resolution = MinionResolution(fixture, "blue-target");

            var events = defeat.Facts
                .Select(fact => CardActionResolutionEvent.MinionDefeatFact(resolution, defeat, fact))
                .ToArray();

            Assert.That(events.Select(value => value.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.MinionDefeatRecorded,
                CardActionResolutionEventKind.MinionRewardAwarded,
                CardActionResolutionEventKind.MinionRemovalAttempted,
                CardActionResolutionEventKind.MinionRemovalPrevented
            }));
        }

        [Test]
        public void PreventedRemoval_KeepsTheRewardAndNeitherWithdrawsNorReawardsItOnASecondAttempt()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));
            var accounting = new DefeatRewardAccounting(fixture.Configuration.Participants);
            var outcome = MinionDefeatOutcome(fixture, "blue-target", 2);
            var reward = accounting.Award(outcome);
            var attempt = new DefeatRemovalAttempt(outcome.Id, outcome.TargetId);
            var prevented = new DefeatRemovalResult(
                outcome.Id, outcome.TargetId, DefeatRemovalResultKind.RemovalPrevented);

            var first = MinionDefeatResolution.Prevented(
                outcome, reward, attempt, prevented, LanePushQualification.None);
            Assert.That(accounting.GetCoins(fixture.AttackerId), Is.EqualTo(2),
                "p.13 awards the coins at the reward stage, which runs before the removal stage decides " +
                "anything, so a prevented removal cannot withdraw them.");

            var second = accounting.Award(outcome);

            Assert.That(second.Accepted, Is.False);
            Assert.That(second.RejectionReason,
                Is.EqualTo(DefeatRewardRejectionReason.OutcomeAlreadyAwarded));
            Assert.That(accounting.GetCoins(fixture.AttackerId), Is.EqualTo(2),
                "A later attempt on the same outcome re-awards nothing; 5.5's outcome identity is what " +
                "makes the reward idempotent across a removal that has to be tried more than once.");
            Assert.That(FactIndex(first, MinionDefeatFactKind.RewardAwarded),
                Is.LessThan(FactIndex(first, MinionDefeatFactKind.RemovalPrevented)),
                "p.13's ordering: the coins are collected, then the removal is attempted.");
        }

        [Test]
        public void CompletedRemoval_OrdersTheRewardBeforeItsOwnStage()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));

            var defeat = AttackMinion(fixture, "blue-target", "ordering-attack").MinionDefeat;

            Assert.That(FactIndex(defeat, MinionDefeatFactKind.RewardAwarded),
                Is.LessThan(FactIndex(defeat, MinionDefeatFactKind.RemovalAttempted)));
            Assert.That(FactIndex(defeat, MinionDefeatFactKind.RemovalAttempted),
                Is.LessThan(FactIndex(defeat, MinionDefeatFactKind.RemovalCompleted)),
                "The same order in both directions: p.13 states the reward and the removal as separate " +
                "stages resolved in that sequence, so neither outcome may reorder them.");
        }

        [Test]
        public void PreventedRemoval_LeavesTheBoardTheReserveAndTheAttacksOwnCompletionAlone()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)),
                CreateMinion("blue-second", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(0, 1)));

            var defeat = PreventedDefeat(fixture, "blue-target");

            Assert.That(
                fixture.Board.Minions.Select(minion => minion.MinionId.Value),
                Is.EquivalentTo(new[] { "blue-target", "blue-second" }),
                "A prevented removal changed nothing about occupancy: the piece is still in its space.");
            Assert.That(fixture.Board.FindUnitSupply(), Is.Empty,
                "The reserve is fed by miniatures that actually left the board, so a prevented removal " +
                "must not put one there and make it spawnable.");
            Assert.That(defeat.Facts, Has.Count.EqualTo(4),
                "The Defeat itself completed; only its removal stage answered that the piece stays.");
        }

        // -------------------------------------------------------------------------------------------
        // The shared decision
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheSharedDecision_StopsARemovalTheRulesProtect_WhicheverOperationDeclaresIt()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-heavy", TeamSide.Blue, StandardMinionType.Heavy, new HexCoordinate(1, 0)),
                CreateMinion("blue-melee", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(0, 1)));
            var protectedHeavy = BoardObjectReference.ForMinion(new MinionId("blue-heavy"));

            var defeatStage = RemovalStage(fixture, RuleActivity.Attack, LanePushCauseKind.Defeat);
            var removeStage = RemovalStage(fixture, RuleActivity.Skill, LanePushCauseKind.Removal);

            Assert.That(defeatStage.IsPrevented(protectedHeavy), Is.True);
            Assert.That(removeStage.IsPrevented(protectedHeavy), Is.True,
                "One decision, so a protected minion that survives a Skill's Remove cannot die to an " +
                "Attack's Defeat - the half-true rule this milestone exists to close.");
            Assert.That(
                defeatStage.IsPrevented(BoardObjectReference.ForMinion(new MinionId("blue-melee"))),
                Is.False,
                "A rule that stopped every removal would pass the two assertions above and protect nothing.");
        }

        [Test]
        public void ARemovalStage_RefusesToExistWithoutDeclaringWhichCauseItWouldRecord()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RemovalStage(fixture, RuleActivity.Attack, LanePushCauseKind.None));
        }

        // -------------------------------------------------------------------------------------------
        // p.8's second cause
        // -------------------------------------------------------------------------------------------

        [Test]
        public void DefeatingTheLastBattleZoneMinionOfASide_RecordsPageEightsDefeatCause()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));

            var defeat = AttackMinion(fixture, "blue-target", "lane-attack").MinionDefeat;

            Assert.That(defeat.LanePush.Qualifies, Is.True);
            Assert.That(defeat.LanePush.Cause, Is.EqualTo(LanePushCauseKind.Defeat),
                "p.8 names its two causes separately - 'It must be either defeated, or removed' - so a " +
                "later milestone reading the record is entitled to know which sentence it answers.");
            Assert.That(defeat.LanePush.LosingSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(defeat.LanePush.BattleZoneId, Is.EqualTo(new ZoneId("battle-zone")));
        }

        [Test]
        public void DefeatingOneOfTwoBattleZoneMinions_RecordsNoCause()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)),
                CreateMinion("blue-second", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(0, 1)));

            var defeat = AttackMinion(fixture, "blue-target", "survivor-attack").MinionDefeat;

            Assert.That(defeat.LanePush.Qualifies, Is.False,
                "p.8 asks about the last remaining enemy minion; one is still standing.");
        }

        [Test]
        public void APreventedDefeatRemoval_RecordsNoCauseEvenWhenTheZoneWouldHaveBeenEmptied()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0)));

            var defeat = PreventedDefeat(fixture, "blue-target");

            Assert.That(defeat.LanePush.Qualifies, Is.False,
                "The question is asked only after a piece has left the board, exactly as it is for a " +
                "prevented Remove; the zone is no emptier than it was.");
            Assert.That(defeat.LanePush.Cause, Is.EqualTo(LanePushCauseKind.None));
        }

        [Test]
        public void TheDerivedCause_ReachesNoPayload()
        {
            var minion = CreateMinion(
                "blue-target", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0));
            var fixture = CreateFixture(minion);
            var defeat = AttackMinion(fixture, "blue-target", "snapshot-attack").MinionDefeat;
            Assert.That(defeat.LanePush.Qualifies, Is.True, "The case is only evidence if a cause exists.");

            var round = new RoundResolutionAuthority(
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                new TieBreakerState(TeamSide.Red),
                1,
                RoundResolutionComposition.ForCardActionsOnly(fixture.Authority));
            var payload = AuthoritativeSnapshotSerializer.Serialize(round, fixture.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                payload,
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Map },
                new[] { minion });

            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(21),
                "Nothing about the removal decision is stored, so it carried no schema move of its " +
                "own. The version has since moved nine times, none of them for it: milestone 6.7 for " +
                "a repeated action instance owed across a paused correction, milestone 6.9 for the " +
                "mid-turn Lane-Push request a completed action can leave behind, milestone 6.10 for " +
                "p.8's Battle Zone itself, milestone 6.10a for a paused system transition, " +
                "milestone 6.11 for p.8's Wave counters and the push that spends them, " +
                "milestone 7.1 for every hero's current Level, milestone 7.2 for p.16's items, " +
                "milestone 7.2a for the four dashboard item slots every hero declares, and " +
                "milestone 7.4 for every team's remaining Life counters. " +
                "The decision is still re-derived from the state as it then stands.");
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(payload),
                "A restored match re-derives the decision from the state as it then stands, for the " +
                "reason every other derived judgement is re-derived rather than carried.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private static int FactIndex(MinionDefeatResolution defeat, MinionDefeatFactKind fact)
        {
            for (var index = 0; index < defeat.Facts.Count; index++)
            {
                if (defeat.Facts[index] == fact) return index;
            }

            return -1;
        }

        private static MinionDefeatResolution PreventedDefeat(Fixture fixture, string minionId)
        {
            var outcome = MinionDefeatOutcome(fixture, minionId, 2);
            var accounting = new DefeatRewardAccounting(fixture.Configuration.Participants);
            return MinionDefeatResolution.Prevented(
                outcome,
                accounting.Award(outcome),
                new DefeatRemovalAttempt(outcome.Id, outcome.TargetId),
                new DefeatRemovalResult(
                    outcome.Id, outcome.TargetId, DefeatRemovalResultKind.RemovalPrevented),
                LanePushQualification.None);
        }

        private static DefeatOutcome MinionDefeatOutcome(Fixture fixture, string minionId, int coins) =>
            new DefeatOutcome(
                new DefeatOutcomeId($"prevented-{minionId}:minion-defeat"),
                new DefeatTargetId(minionId),
                new TeamId(PlaceholderOneVsOneParticipants.BlueTeamIdValue),
                fixture.AttackerId,
                coins,
                0);

        private static PendingAttackResolution MinionResolution(Fixture fixture, string minionId) =>
            new PendingAttackResolution(
                new ActionResolutionId($"prevented-{minionId}"),
                fixture.AttackOffer.SourceCardInstanceId,
                fixture.AttackOffer.SourceDefinitionReference,
                fixture.AttackOffer.ActionDefinitionId,
                fixture.AttackerId,
                fixture.AttackOffer.InvocationContext,
                new ActionResolutionStepId("minion-defeat"),
                AttackResolutionPhase.ResolveMinionDefeat,
                AttackTarget.Minion(new MinionId(minionId)),
                null);

        private static RemovalStageDecision RemovalStage(
            Fixture fixture, RuleActivity activity, LanePushCauseKind cause) =>
            new RemovalStageDecision(
                activity,
                RuleObject.ForHero(fixture.AttackerId),
                fixture.Board,
                fixture.Board,
                fixture.Configuration.Participants,
                RuleModificationSet.Empty,
                cause);

        private static AttackActionResolutionResult AttackMinion(
            Fixture fixture,
            string minionId,
            string resolutionId)
        {
            var opened = fixture.Authority.StartAttackResolution(
                new ActionResolutionId(resolutionId),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants, RuleModificationSet.Empty);
            var result = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(opened.PendingTargetChoice.ResolutionId,
                    opened.PendingTargetChoice.ChoiceId, new MinionId(minionId)),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants, RuleModificationSet.Empty);
            Assert.That(result.Accepted, Is.True);
            return result;
        }

        private static Fixture CreateFixture(params QuickGameMinionSetupState[] minions)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var attacker = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var defender = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var map = CreateBattleZoneMap();
            var board = new AdjacentMoveAuthority(
                map,
                new[]
                {
                    new HeroOccupancy(attacker, new HexCoordinate(0, 0)),
                    new HeroOccupancy(defender, new HexCoordinate(-3, 0))
                },
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                minions);
            var offer = ActionOfferQuery.GetAvailableOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    attacker,
                    ActionOfferWindow.NormalPlay)
                .Single(value => value.SourceCardInstanceId.Value == "red-pursuit-card" &&
                    value.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
            return new Fixture(configuration, attacker, map, board,
                new CardActionResolutionAuthority(board), offer);
        }

        private static BoardMapDefinition CreateBattleZoneMap()
        {
            var mapId = new MapId("defeat-removal-map");
            var zone = new BoardMapZone(new ZoneId("battle-zone"), "Battle Zone", "lane", null, false, true);
            var spaces = CreateRadiusThreeCoordinates().Select(coordinate =>
                new BoardMapSpace(new SpaceId($"q{coordinate.Q}_r{coordinate.R}"), coordinate,
                    BoardMapSpaceKind.Playable, zone.Id));
            return new BoardMapDefinition(mapId, 1, "Defeat Removal Map", spaces, new[] { zone },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        private static QuickGameMinionSetupState CreateMinion(
            string id,
            TeamSide side,
            StandardMinionType type,
            HexCoordinate coordinate)
        {
            var mapId = new MapId("defeat-removal-map");
            var teamId = new TeamId(side == TeamSide.Red
                ? PlaceholderOneVsOneParticipants.RedTeamIdValue
                : PlaceholderOneVsOneParticipants.BlueTeamIdValue);
            return new QuickGameMinionSetupState(
                new MinionId(id), teamId, side, type,
                new ConfiguredSpawnPointReference(mapId, new SpawnPointId($"spawn-{id}")),
                new ConfiguredSpaceReference(mapId, new SpaceId($"q{coordinate.Q}_r{coordinate.R}")),
                coordinate);
        }

        private static IEnumerable<HexCoordinate> CreateRadiusThreeCoordinates()
        {
            for (var q = -3; q <= 3; q++)
            {
                var minimumR = Math.Max(-3, -q - 3);
                var maximumR = Math.Min(3, -q + 3);
                for (var r = minimumR; r <= maximumR; r++) yield return new HexCoordinate(q, r);
            }
        }

        private sealed class Fixture
        {
            public Fixture(PlaceholderOneVsOneTestConfiguration configuration, HeroId attackerId,
                BoardMapDefinition map, AdjacentMoveAuthority board,
                CardActionResolutionAuthority authority, ActionOffer attackOffer)
            {
                Configuration = configuration; AttackerId = attackerId; Map = map;
                Board = board; Authority = authority; AttackOffer = attackOffer;
            }

            public PlaceholderOneVsOneTestConfiguration Configuration { get; }
            public HeroId AttackerId { get; }
            public BoardMapDefinition Map { get; }
            public AdjacentMoveAuthority Board { get; }
            public CardActionResolutionAuthority Authority { get; }
            public ActionOffer AttackOffer { get; }
        }
    }
}
