using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MinionObstacleAndDefenseTests
    {
        [Test]
        public void ExistingMinionState_IsAUnitObstacleForConstructionMovementAndReachability()
        {
            var heroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var origin = new HexCoordinate(0, 0);
            var minionCoordinate = new HexCoordinate(1, 0);
            var minion = CreateMinion("blue-melee", TeamSide.Blue, StandardMinionType.Melee, minionCoordinate);
            var board = new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[] { new HeroOccupancy(heroId, origin) },
                Array.Empty<ObstacleOccupancy>(),
                new[] { minion });

            var move = board.TryCommit(new AdjacentMoveCommand(heroId, origin, minionCoordinate));
            var reachable = MovementReachabilityQuery.FindReachableDestinations(
                new HeroOccupancy(heroId, origin),
                board.BoardCoordinates,
                board.Occupancies,
                board.ObstacleOccupancies,
                board.Minions,
                1);

            // The board holds a runtime MinionOccupancy converted from the setup placement; its
            // stable identity and configured position are preserved exactly.
            var boardMinion = board.Minions.Single();
            Assert.That(boardMinion.MinionId, Is.EqualTo(minion.MinionId));
            Assert.That(boardMinion.Coordinate, Is.EqualTo(minion.Coordinate));
            Assert.That(boardMinion.OccupiedSpace, Is.EqualTo(minion.OccupiedSpace));
            Assert.That(move.Accepted, Is.False);
            Assert.That(move.RejectionReason, Is.EqualTo(AdjacentMoveRejectionReason.DestinationOccupied));
            Assert.That(reachable.Any(value => value.Destination.Equals(minionCoordinate)), Is.False);
            Assert.That(board.Occupancies.Single().Coordinate, Is.EqualTo(origin));

            Assert.That(() => new AdjacentMoveAuthority(
                    CreateRadiusThreeCoordinates(),
                    new[] { new HeroOccupancy(heroId, origin) },
                    Array.Empty<ObstacleOccupancy>(),
                    new[] { CreateMinion("same-space", TeamSide.Blue, StandardMinionType.Heavy, origin) }),
                Throws.ArgumentException);
            Assert.That(() => new AdjacentMoveAuthority(
                    CreateRadiusThreeCoordinates(),
                    new[] { new HeroOccupancy(heroId, origin) },
                    new[] { new ObstacleOccupancy(new ObstacleId("rock"), minionCoordinate) },
                    new[] { minion }),
                Throws.ArgumentException);
            Assert.That(() => new AdjacentMoveAuthority(
                    CreateRadiusThreeCoordinates(),
                    new[] { new HeroOccupancy(heroId, origin) },
                    Array.Empty<ObstacleOccupancy>(),
                    new[]
                    {
                        new QuickGameMinionSetupState(
                            new MinionId("wrong-space"),
                            new TeamId(PlaceholderOneVsOneParticipants.BlueTeamIdValue),
                            TeamSide.Blue,
                            StandardMinionType.Melee,
                            new ConfiguredSpawnPointReference(
                                new MapId("synthetic-coordinate-board"),
                                new SpawnPointId("wrong-space-spawn")),
                            new ConfiguredSpaceReference(
                                new MapId("synthetic-coordinate-board"),
                                new SpaceId("q2_r0")),
                            minionCoordinate)
                    }),
                Throws.ArgumentException);
            Assert.That(() => new AdjacentMoveAuthority(
                    CreateRadiusThreeCoordinates(),
                    new[] { new HeroOccupancy(heroId, origin) },
                    Array.Empty<ObstacleOccupancy>(),
                    new[] { CreateMinion("invalid-type", TeamSide.Blue, StandardMinionType.None, minionCoordinate) }),
                Throws.ArgumentException);
        }

        [Test]
        public void DefenseResponse_AddsOnlyRulebookMinionModifiersWithStableSourceProvenance()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var attackerId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var defenderId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var minions = new[]
            {
                CreateMinion("blue-friendly-heavy", TeamSide.Blue, StandardMinionType.Heavy, new HexCoordinate(1, 1)),
                CreateMinion("blue-friendly-melee", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(2, 0)),
                CreateMinion("blue-friendly-ranged", TeamSide.Blue, StandardMinionType.Ranged, new HexCoordinate(3, 0)),
                CreateMinion("red-enemy-heavy", TeamSide.Red, StandardMinionType.Heavy, new HexCoordinate(2, -1)),
                CreateMinion("red-enemy-melee", TeamSide.Red, StandardMinionType.Melee, new HexCoordinate(0, 1)),
                CreateMinion("red-enemy-ranged-adjacent", TeamSide.Red, StandardMinionType.Ranged, new HexCoordinate(1, -1)),
                CreateMinion("red-enemy-ranged-near", TeamSide.Red, StandardMinionType.Ranged, new HexCoordinate(3, -1)),
                CreateMinion("red-enemy-ranged-far", TeamSide.Red, StandardMinionType.Ranged, new HexCoordinate(-2, 0))
            };
            var board = new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(defenderId, new HexCoordinate(1, 0))
                },
                Array.Empty<ObstacleOccupancy>(),
                minions);
            var authority = DefeatConsequenceComposition.Dispatcher(board, configuration.Participants);
            var attackOffer = ActionOfferQuery.GetAvailableOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    attackerId,
                    ActionOfferWindow.NormalPlay)
                .Single(offer =>
                    offer.SourceCardInstanceId.Value == "red-pursuit-card" &&
                    offer.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
            var started = authority.StartAttackResolution(
                new ActionResolutionId("minion-defense"),
                attackOffer,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants, RuleModificationSet.Empty);
            var opened = authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    started.PendingTargetChoice.ResolutionId,
                    started.PendingTargetChoice.ChoiceId,
                    defenderId),
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants, RuleModificationSet.Empty);
            var defenseOffer = opened.PendingDefenseChoice.LegalDefenseOffers[0];
            var printed = PrintedValueQuery.CalculatePrintedValue(
                defenseOffer,
                configuration.CardDefinitions,
                PrintedValueChannel.Defense);
            var answered = authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId,
                    defenseOffer.SourceCardInstanceId),
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants, RuleModificationSet.Empty);

            Assert.That(answered.Accepted, Is.True);
            Assert.That(answered.PendingResolution.DefenseTotal, Is.EqualTo(printed.Total - 2));
            Assert.That(answered.PendingResolution.DefenseValue.Contributions[0], Is.EqualTo(printed.Contributions[0]));
            var minionContributions = answered.PendingResolution.DefenseValue.Contributions
                .Where(value => value.Kind == ValueContributionKind.MinionDefenseModifier)
                .ToArray();
            Assert.That(minionContributions.Select(value => value.SourceMinionId.Value), Is.EqualTo(new[]
            {
                "blue-friendly-heavy",
                "blue-friendly-melee",
                "red-enemy-heavy",
                "red-enemy-melee",
                "red-enemy-ranged-adjacent",
                "red-enemy-ranged-near"
            }));
            Assert.That(minionContributions.Select(value => value.Amount), Is.EqualTo(new[] { 1, 1, -1, -1, -1, -1 }));
            Assert.That(minionContributions.All(value =>
                value.HasSourceMinionId &&
                !value.SourceCardInstanceId.IsDefined &&
                !value.HasSourceActionDefinitionId &&
                value.Channel == PrintedValueChannel.Defense), Is.True);

            var restored = CardActionResolutionAuthority.Restore(
                board,
                CardActionResolutionRestoreState.WithPendingAttack(
                    answered.PendingResolution,
                    HeroCoinLedgerRestoreStateHolder.Absent()),
                null,
                null,
                null,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                new MarkerAttachmentRegister(),
                RuleModificationSet.Empty,
                null,
                // Milestone 7.4: the restored dispatcher resumes an Attack whose outcome defeats a hero,
                // so it is handed the two states p.13 prices that Defeat against - the same pair a round
                // rebinds when it restores a whole match.
                new GuardsDigital.Core.Rules.Progression.HeroProgressionState(configuration.Participants),
                new GuardsDigital.Core.Rules.Consequences.TeamLifeState(
                    configuration.Participants, RoundResolutionAuthority.BaselineLifeCountersPerTeam));
            var outcome = restored.ResolveAttackOutcome(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants, RuleModificationSet.Empty);
            Assert.That(outcome.Accepted, Is.True);
        }

        [Test]
        public void SnapshotRoundTrip_RebindsExistingMinionsAndPreservesDefenseContributionSources()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            // Red Pursuit at Initiative 7 against Blue Hammer at 6: the Red hero acts first with a
            // printed non-Ranged Attack, which the showcase conversions took off both hands' first card.
            var red = configuration.RedHand.Instances[3];
            var blue = configuration.BlueHand.Instances[3];
            var minions = new[]
            {
                CreateMinion("blue-snapshot-melee", TeamSide.Blue, StandardMinionType.Melee, new HexCoordinate(2, 0))
            };
            var board = new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(red.OwnerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(blue.OwnerId, new HexCoordinate(1, 0))
                },
                Array.Empty<ObstacleOccupancy>(),
                minions);
            var cardActions = new CardActionResolutionAuthority(board);
            var round = new RoundResolutionAuthority(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                configuration.TieBreakerState,
                1,
                RoundResolutionComposition.ForCardActionsOnly(cardActions));
            Assert.That(round.TryCommitCard(red.OwnerId, red.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(blue.OwnerId, blue.Id).Accepted, Is.True);
            var selected = round.CurrentTurnAuthority.SelectedActor;
            var attackOffer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    selected.ActorId,
                    selected.CardInstanceId)
                .Single(offer => offer.AttackRange != AttackRangeKind.None);
            Assert.That(round.TryStartAttack(new ActionResolutionId("snapshot-minion-defense"), attackOffer).Accepted, Is.True);
            var targetChoice = round.CurrentTurnAuthority.PendingAttackTargetChoice;
            Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                targetChoice.ResolutionId,
                targetChoice.ChoiceId,
                targetChoice.LegalTargetHeroIds.Single())).Accepted, Is.True);
            var defenseChoice = round.CurrentTurnAuthority.PendingDefenseChoice;
            var defenseOffer = defenseChoice.LegalDefenseOffers[0];
            Assert.That(cardActions.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    defenseChoice.ResolutionId,
                    defenseChoice.ChoiceId,
                    defenseOffer.SourceCardInstanceId),
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants, RuleModificationSet.Empty).Accepted, Is.True);

            var payload = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                payload,
                configuration.CardDefinitions,
                new[] { board.MapDefinition },
                minions);

            Assert.That(restored.Board.Minions.Select(value => value.MinionId.Value),
                Is.EqualTo(new[] { "blue-snapshot-melee" }));
            Assert.That(restored.CardActions.PendingAttackResolution.DefenseValue.Contributions.Any(value =>
                value.Kind == ValueContributionKind.MinionDefenseModifier &&
                value.SourceMinionId.Value == "blue-snapshot-melee" &&
                value.Amount == 1), Is.True);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board), Is.EqualTo(payload));
        }

        private static QuickGameMinionSetupState CreateMinion(
            string id,
            TeamSide side,
            StandardMinionType type,
            HexCoordinate coordinate)
        {
            var mapId = new MapId("synthetic-coordinate-board");
            var teamId = new TeamId(side == TeamSide.Red
                ? PlaceholderOneVsOneParticipants.RedTeamIdValue
                : PlaceholderOneVsOneParticipants.BlueTeamIdValue);
            return new QuickGameMinionSetupState(
                new MinionId(id),
                teamId,
                side,
                type,
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
                for (var r = minimumR; r <= maximumR; r++)
                {
                    yield return new HexCoordinate(q, r);
                }
            }
        }
    }
}
