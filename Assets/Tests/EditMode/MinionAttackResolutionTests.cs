using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MinionAttackResolutionTests
    {
        [TestCase(StandardMinionType.Melee, 2)]
        [TestCase(StandardMinionType.Ranged, 2)]
        [TestCase(StandardMinionType.Heavy, 4)]
        public void EnemyMinionAttack_SkipsDefenseAndRecordsOrderedStagedDefeat(
            StandardMinionType type,
            int expectedCoins)
        {
            var fixture = CreateFixture(CreateMinion("blue-target", TeamSide.Blue, type, new HexCoordinate(1, 0)));

            var result = AttackMinion(fixture, "blue-target", $"attack-{type}");

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.PendingResolution, Is.Null);
            Assert.That(result.PendingDefenseChoice, Is.Null, "Minions never open a Defense reaction.");
            Assert.That(result.Outcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful));
            Assert.That(result.MinionDefeat, Is.Not.Null);
            Assert.That(result.MinionDefeat.Facts, Is.EqualTo(new[]
            {
                MinionDefeatFactKind.DefeatRecorded,
                MinionDefeatFactKind.RewardAwarded,
                MinionDefeatFactKind.RemovalAttempted,
                MinionDefeatFactKind.RemovalCompleted
            }));
            Assert.That(result.MinionDefeat.Reward.Operations, Has.Count.EqualTo(1));
            Assert.That(result.MinionDefeat.Reward.Operations.Single().AwardKind, Is.EqualTo(CoinAwardKind.Defeat));
            Assert.That(result.MinionDefeat.Reward.Operations.Single().Amount, Is.EqualTo(expectedCoins));
            Assert.That(fixture.Authority.CoinLedger.GetCoins(fixture.AttackerId), Is.EqualTo(expectedCoins));
            Assert.That(fixture.Board.Minions, Is.Empty);
            Assert.That(result.Events.Select(value => value.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.AttackTargetValidated,
                CardActionResolutionEventKind.AttackStarted,
                CardActionResolutionEventKind.MinionDefeatRecorded,
                CardActionResolutionEventKind.MinionRewardAwarded,
                CardActionResolutionEventKind.MinionRemovalAttempted,
                CardActionResolutionEventKind.MinionRemovalCompleted,
                CardActionResolutionEventKind.AttackResolutionCompleted
            }));
            Assert.That(result.Events, Has.None.Matches<CardActionResolutionEvent>(value =>
                value.Kind == CardActionResolutionEventKind.DefenseResponseChoiceRequested));
        }

        [Test]
        public void Heavy_RemainsUntargetableUntilFriendlyNonHeavyLeavesBattleZone()
        {
            var fixture = CreateFixture(
                CreateMinion("blue-heavy", TeamSide.Blue, StandardMinionType.Heavy, new HexCoordinate(1, 0)),
                CreateMinion("blue-melee", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(0, 1)));

            var opened = StartAttack(fixture, "first-attack");
            Assert.That(opened.PendingTargetChoice.LegalTargetMinionIds,
                Is.EqualTo(new[] { new MinionId("blue-melee") }));
            var heavyRejected = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(opened.PendingTargetChoice.ResolutionId,
                    opened.PendingTargetChoice.ChoiceId, new MinionId("blue-heavy")),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants, RuleModificationSet.Empty);
            Assert.That(heavyRejected.Accepted, Is.False);
            Assert.That(fixture.Board.Minions.Count, Is.EqualTo(2));

            var meleeDefeat = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(opened.PendingTargetChoice.ResolutionId,
                    opened.PendingTargetChoice.ChoiceId, new MinionId("blue-melee")),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants, RuleModificationSet.Empty);
            Assert.That(meleeDefeat.Accepted, Is.True);

            var heavyDefeat = AttackMinion(fixture, "blue-heavy", "second-attack");
            Assert.That(heavyDefeat.Accepted, Is.True);
            Assert.That(heavyDefeat.MinionDefeat.Reward.Operations.Single().Amount, Is.EqualTo(4));
            Assert.That(fixture.Authority.CoinLedger.GetCoins(fixture.AttackerId), Is.EqualTo(6));
        }

        [Test]
        public void FriendlyAndOutOfRangeMinions_AreNotLegalTargets()
        {
            var fixture = CreateFixture(
                CreateMinion("red-friendly", TeamSide.Red, StandardMinionType.Melee, new HexCoordinate(1, 0)),
                CreateMinion("blue-far", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(3, 0)),
                CreateMinion("blue-adjacent", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(0, 1)));

            var opened = StartAttack(fixture, "target-boundaries");

            Assert.That(opened.PendingTargetChoice.LegalTargetMinionIds,
                Is.EqualTo(new[] { new MinionId("blue-adjacent") }));
        }

        [Test]
        public void RemovedMinionAndCoinAward_RoundTripThroughSchemaFourSnapshot()
        {
            var minion = CreateMinion("blue-snapshot", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0));
            var fixture = CreateFixture(minion);
            Assert.That(AttackMinion(fixture, minion.MinionId.Value, "snapshot-attack").Accepted, Is.True);
            var round = new RoundResolutionAuthority(
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                new TieBreakerState(TeamSide.Red),
                1,
                RoundResolutionComposition.ForCardActionsOnly(fixture.Authority));

            var payload = AuthoritativeSnapshotSerializer.Serialize(round, fixture.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                payload,
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Map },
                new[] { minion });

            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(21));
            Assert.That(restored.Board.Minions, Is.Empty);
            Assert.That(restored.CardActions.CoinLedger.GetCoins(fixture.AttackerId), Is.EqualTo(2));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board), Is.EqualTo(payload));
        }

        [Test]
        public void MovedMinionCoordinate_RoundTripsThroughSchemaSevenSnapshot()
        {
            // ER2 closing check for F-ER2-02: a restored match preserves a mutated minion
            // coordinate instead of reconstructing the position from the setup placement.
            // Pre-fix this fixture could not be expressed at all: no minion mutation path existed.
            var minion = CreateMinion("blue-mover", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0));
            var fixture = CreateFixture(minion);
            var destination = new HexCoordinate(1, 1);

            var move = fixture.Board.TryCommitMinionMove(minion.MinionId, destination);

            Assert.That(move.Accepted, Is.True);
            Assert.That(move.MovedMinion.Coordinate, Is.EqualTo(destination));
            Assert.That(move.Origin, Is.EqualTo(new HexCoordinate(1, 0)));
            Assert.That(fixture.Board.Minions.Single().Coordinate, Is.EqualTo(destination));

            var round = new RoundResolutionAuthority(
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants,
                new TieBreakerState(TeamSide.Red),
                1,
                RoundResolutionComposition.ForCardActionsOnly(fixture.Authority));
            var payload = AuthoritativeSnapshotSerializer.Serialize(round, fixture.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                payload,
                fixture.Configuration.CardDefinitions,
                new[] { fixture.Map },
                new[] { minion });

            var restoredMinion = restored.Board.Minions.Single();
            Assert.That(restoredMinion.MinionId, Is.EqualTo(minion.MinionId));
            Assert.That(restoredMinion.Coordinate, Is.EqualTo(destination),
                "Restore must take the minion position from the stream, not from the setup placement.");
            Assert.That(restoredMinion.Coordinate, Is.Not.EqualTo(minion.Coordinate));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board), Is.EqualTo(payload));
        }

        [Test]
        public void TryCommitMinionMove_ValidatesIdentityBoardAndOccupancy()
        {
            var minion = CreateMinion("blue-guard", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(1, 0));
            var fixture = CreateFixture(minion);

            var unknown = fixture.Board.TryCommitMinionMove(new MinionId("missing"), new HexCoordinate(1, 1));
            Assert.That(unknown.Accepted, Is.False);
            Assert.That(unknown.RejectionReason, Is.EqualTo(MinionMoveRejectionReason.UnknownMinion));

            var offBoard = fixture.Board.TryCommitMinionMove(minion.MinionId, new HexCoordinate(9, 9));
            Assert.That(offBoard.Accepted, Is.False);
            Assert.That(offBoard.RejectionReason, Is.EqualTo(MinionMoveRejectionReason.DestinationOffBoard));

            var occupied = fixture.Board.TryCommitMinionMove(minion.MinionId, new HexCoordinate(0, 0));
            Assert.That(occupied.Accepted, Is.False);
            Assert.That(occupied.RejectionReason, Is.EqualTo(MinionMoveRejectionReason.DestinationOccupied));
            Assert.That(fixture.Board.Minions.Single().Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));

            var accepted = fixture.Board.TryCommitMinionMove(minion.MinionId, new HexCoordinate(1, 1));
            Assert.That(accepted.Accepted, Is.True);

            // The vacated space is genuinely free again: the hero can step onto it.
            var heroMove = fixture.Board.TryCommit(new AdjacentMoveCommand(
                fixture.AttackerId, new HexCoordinate(0, 0), new HexCoordinate(1, 0)));
            Assert.That(heroMove.Accepted, Is.True);
        }

        private static AttackActionResolutionResult AttackMinion(
            Fixture fixture,
            string minionId,
            string resolutionId)
        {
            var opened = StartAttack(fixture, resolutionId);
            return fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(opened.PendingTargetChoice.ResolutionId,
                    opened.PendingTargetChoice.ChoiceId, new MinionId(minionId)),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants, RuleModificationSet.Empty);
        }

        private static AttackActionResolutionResult StartAttack(Fixture fixture, string resolutionId) =>
            fixture.Authority.StartAttackResolution(
                new ActionResolutionId(resolutionId),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants, RuleModificationSet.Empty);

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
                new CardActionResolutionAuthority(MatchContentCatalogs.ForThisBuild(), board), offer);
        }

        private static BoardMapDefinition CreateBattleZoneMap()
        {
            var mapId = new MapId("minion-attack-map");
            var zone = new BoardMapZone(new ZoneId("battle-zone"), "Battle Zone", "lane", null, false, true);
            var spaces = CreateRadiusThreeCoordinates().Select(coordinate =>
                new BoardMapSpace(new SpaceId($"q{coordinate.Q}_r{coordinate.R}"), coordinate,
                    BoardMapSpaceKind.Playable, zone.Id));
            return new BoardMapDefinition(mapId, 1, "Minion Attack Map", spaces, new[] { zone },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        private static QuickGameMinionSetupState CreateMinion(
            string id,
            TeamSide side,
            StandardMinionType type,
            HexCoordinate coordinate)
        {
            var mapId = new MapId("minion-attack-map");
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
