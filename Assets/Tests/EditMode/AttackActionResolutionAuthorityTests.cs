using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class AttackActionResolutionAuthorityTests
    {
        [Test]
        public void AdjacentEnemyTarget_OpensDefenderOwnedChoiceWithPrintedAttackTotalAndOrderedEvents()
        {
            var fixture = CreateFixture();
            var originalStates = fixture.Configuration.CardInstances
                .ToDictionary(instance => instance.Id, instance => instance.State);

            var start = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("attack-resolution"),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            var answer = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    fixture.DefenderId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(start.Accepted, Is.True);
            Assert.That(start.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.AttackTargetChoiceRequested
            }));
            Assert.That(start.PendingTargetChoice.ChooserId, Is.EqualTo(fixture.AttackerId));
            Assert.That(start.PendingTargetChoice.LegalTargetHeroIds, Is.EqualTo(new[] { fixture.DefenderId }));
            Assert.That(answer.Accepted, Is.True);
            Assert.That(answer.PendingResolution.Phase, Is.EqualTo(AttackResolutionPhase.DefenseResponse));
            Assert.That(answer.PendingResolution.TargetHeroId, Is.EqualTo(fixture.DefenderId));
            Assert.That(answer.PendingResolution.AttackValue.Channel, Is.EqualTo(PrintedValueChannel.Attack));
            Assert.That(answer.PendingResolution.AttackValue.Contributions, Has.Count.EqualTo(1));
            Assert.That(
                answer.PendingResolution.AttackValue.Contributions.Single().Kind,
                Is.EqualTo(PrintedValueContributionKind.PrintedIcon));
            Assert.That(answer.PendingResolution.AttackTotal, Is.EqualTo(3));
            Assert.That(answer.PendingDefenseChoice.ChooserId, Is.EqualTo(fixture.DefenderId));
            Assert.That(answer.PendingDefenseChoice.AttackerId, Is.EqualTo(fixture.AttackerId));
            Assert.That(answer.PendingDefenseChoice.AttackTotal, Is.EqualTo(3));
            Assert.That(answer.PendingDefenseChoice.CanDecline, Is.True);
            Assert.That(answer.PendingDefenseChoice.LegalDefenseOffers, Has.Count.EqualTo(2));
            Assert.That(
                answer.PendingDefenseChoice.LegalDefenseOffers.All(offer =>
                    offer.ActorId.Equals(fixture.DefenderId) &&
                    offer.InvocationContext == ActionInvocationContext.Reaction),
                Is.True);
            Assert.That(answer.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.AttackStarted,
                CardActionResolutionEventKind.AttackTotalCalculated,
                CardActionResolutionEventKind.DefenseResponseChoiceRequested
            }));
            Assert.That(answer.Events.All(ruleEvent => ruleEvent.TargetHeroId.Equals(fixture.DefenderId)), Is.True);
            Assert.That(answer.Events.All(ruleEvent => ruleEvent.AttackTotal == 3), Is.True);
            Assert.That(fixture.Authority.PendingAttackTargetChoice, Is.Null);
            Assert.That(fixture.Authority.PendingDefenseChoice, Is.SameAs(answer.PendingDefenseChoice));
            Assert.That(
                fixture.Configuration.CardInstances.All(instance => instance.State.Equals(originalStates[instance.Id])),
                Is.True,
                "Initiating an Attack and opening Defense must not consume either hero's card.");
        }

        [Test]
        public void TargetProjection_IncludesOnlyAdjacentEnemyHeroesAndNeverOccupancyObstacles()
        {
            var attackerId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var adjacentEnemyId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var friendlyId = new HeroId("red-friendly-hero");
            var nonAdjacentEnemyId = new HeroId("blue-far-hero");
            var participants = CreateFourHeroParticipants(attackerId, adjacentEnemyId, friendlyId, nonAdjacentEnemyId);
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var authority = new CardActionResolutionAuthority(new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(adjacentEnemyId, new HexCoordinate(1, 0)),
                    new HeroOccupancy(friendlyId, new HexCoordinate(0, 1)),
                    new HeroOccupancy(nonAdjacentEnemyId, new HexCoordinate(2, 0))
                },
                new[] { new ObstacleOccupancy(new ObstacleId("terrain-token-obstacle"), new HexCoordinate(-1, 0)) }));
            var attackOffer = GetAttackOffer(configuration, attackerId);

            var start = authority.StartAttackResolution(
                new ActionResolutionId("attack-resolution"),
                attackOffer,
                configuration.CardDefinitions,
                configuration.CardInstances,
                participants);

            Assert.That(start.Accepted, Is.True);
            Assert.That(start.PendingTargetChoice.LegalTargetHeroIds, Is.EqualTo(new[] { adjacentEnemyId }));

            var invalidTargets = new[]
            {
                attackerId,
                friendlyId,
                nonAdjacentEnemyId,
                new HeroId("empty-space"),
                new HeroId("terrain"),
                new HeroId("token"),
                new HeroId("terrain-token-obstacle")
            };
            foreach (var invalidTarget in invalidTargets)
            {
                var rejection = authority.AnswerAttackTarget(
                    new AttackTargetChoiceCommand(
                        start.PendingTargetChoice.ResolutionId,
                        start.PendingTargetChoice.ChoiceId,
                        invalidTarget),
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    participants);

                Assert.That(rejection.Accepted, Is.False, $"Target '{invalidTarget}' should be rejected.");
                Assert.That(rejection.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.TargetUnavailable));
                Assert.That(rejection.Events, Is.Empty);
                Assert.That(rejection.PendingResolution, Is.SameAs(start.PendingResolution));
                Assert.That(rejection.PendingTargetChoice, Is.SameAs(start.PendingTargetChoice));
            }
        }

        [Test]
        public void RangedAttack_RangeOneIncludesAdjacentEnemyThroughTypedOfferRange()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var attackerId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var defenderId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var rangedDefinition = new CardDefinition(
                new CardDefinitionId("range-one-test-card"),
                "rev-1",
                "TEST - Range One Attack",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("range-one-attack"),
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, 4),
                            new PrintedValueDefinition(PrintedValueChannel.Range, 1)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default)
                });
            var rangedCard = CardInstance.CreateInHand(
                new CardInstanceId("range-one-test-instance"),
                rangedDefinition.Reference,
                attackerId);
            var definitions = configuration.CardDefinitions.Concat(new[] { rangedDefinition }).ToArray();
            var instances = configuration.CardInstances.Concat(new[] { rangedCard }).ToArray();
            var offer = ActionOfferQuery.GetAvailableOffers(
                    definitions,
                    instances,
                    attackerId,
                    ActionOfferWindow.NormalPlay)
                .Single(candidate => candidate.ActionDefinitionId.Equals(new ActionDefinitionId("range-one-attack")));
            var authority = new CardActionResolutionAuthority(CreateBoardAuthority(attackerId, defenderId));

            var start = authority.StartAttackResolution(
                new ActionResolutionId("range-one-resolution"),
                offer,
                definitions,
                instances,
                configuration.Participants);

            Assert.That(start.Accepted, Is.True);
            Assert.That(start.PendingTargetChoice.LegalTargetHeroIds, Is.EqualTo(new[] { defenderId }));
            Assert.That(
                PrintedValueQuery.CalculatePrintedValue(offer, definitions, PrintedValueChannel.Range).Total,
                Is.EqualTo(1));
        }

        [Test]
        public void RangedAttack_UsesHexDistanceThroughOccupiedSpaceAndRejectsBeyondMaximumRange()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var attackerId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var maximumRangeEnemyId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var friendlyBlockerId = new HeroId("red-friendly-hero");
            var beyondRangeEnemyId = new HeroId("blue-far-hero");
            var participants = CreateFourHeroParticipants(
                attackerId,
                maximumRangeEnemyId,
                friendlyBlockerId,
                beyondRangeEnemyId);
            var board = new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(friendlyBlockerId, new HexCoordinate(1, 0)),
                    new HeroOccupancy(maximumRangeEnemyId, new HexCoordinate(2, 0)),
                    new HeroOccupancy(beyondRangeEnemyId, new HexCoordinate(3, 0))
                });
            var authority = new CardActionResolutionAuthority(board);
            var rangedOffer = GetRangedAttackOffer(configuration, attackerId);
            var originalStates = configuration.CardInstances.Select(instance => instance.State).ToArray();

            var start = authority.StartAttackResolution(
                new ActionResolutionId("ranged-resolution"),
                rangedOffer,
                configuration.CardDefinitions,
                configuration.CardInstances,
                participants);
            var beyondRange = authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    beyondRangeEnemyId),
                configuration.CardDefinitions,
                configuration.CardInstances,
                participants);
            var accepted = authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    maximumRangeEnemyId),
                configuration.CardDefinitions,
                configuration.CardInstances,
                participants);

            Assert.That(HexDistanceQuery.Calculate(new HexCoordinate(0, 0), new HexCoordinate(2, 0)), Is.EqualTo(2));
            Assert.That(start.PendingTargetChoice.LegalTargetHeroIds, Is.EqualTo(new[] { maximumRangeEnemyId }));
            Assert.That(beyondRange.Accepted, Is.False);
            Assert.That(beyondRange.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.TargetUnavailable));
            Assert.That(beyondRange.Events, Is.Empty);
            Assert.That(configuration.CardInstances.Select(instance => instance.State), Is.EqualTo(originalStates));
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.PendingResolution.AttackTotal, Is.EqualTo(2));
            Assert.That(accepted.PendingDefenseChoice.ChooserId, Is.EqualTo(maximumRangeEnemyId));
            Assert.That(accepted.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.AttackStarted,
                CardActionResolutionEventKind.AttackTotalCalculated,
                CardActionResolutionEventKind.DefenseResponseChoiceRequested
            }));
        }

        [Test]
        public void RangedAttack_TargetsThroughObstacleWithoutLineOfSightRule()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var attackerId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var defenderId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var authority = new CardActionResolutionAuthority(new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(defenderId, new HexCoordinate(2, 0))
                },
                new[] { new ObstacleOccupancy(new ObstacleId("between-heroes"), new HexCoordinate(1, 0)) }));

            var start = authority.StartAttackResolution(
                new ActionResolutionId("ranged-through-obstacle"),
                GetRangedAttackOffer(configuration, attackerId),
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants);

            Assert.That(start.Accepted, Is.True);
            Assert.That(start.PendingTargetChoice.LegalTargetHeroIds, Is.EqualTo(new[] { defenderId }));
        }

        [Test]
        public void RangedAttack_RestoresTargetAndDefenseChoicesThroughSharedContinuationBoundary()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var attackerId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var defenderId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var board = new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(defenderId, new HexCoordinate(2, 0))
                });
            var authority = new CardActionResolutionAuthority(board);
            var rangedOffer = GetRangedAttackOffer(configuration, attackerId);
            var start = authority.StartAttackResolution(
                new ActionResolutionId("restorable-ranged-resolution"),
                rangedOffer,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants);

            var restoredTarget = CardActionResolutionAuthority.RestoreAttack(
                new AdjacentMoveAuthority(
                    CreateRadiusThreeCoordinates(),
                    new[]
                    {
                        new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                        new HeroOccupancy(defenderId, new HexCoordinate(2, 0))
                    }),
                start.PendingResolution,
                start.PendingTargetChoice,
                null,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants);
            var restoredTargetChoice = restoredTarget.PendingAttackTargetChoice;
            var answer = restoredTarget.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    defenderId),
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants);
            var restoredDefense = CardActionResolutionAuthority.RestoreAttack(
                new AdjacentMoveAuthority(
                    CreateRadiusThreeCoordinates(),
                    new[]
                    {
                        new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                        new HeroOccupancy(defenderId, new HexCoordinate(2, 0))
                    }),
                answer.PendingResolution,
                null,
                answer.PendingDefenseChoice,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants);

            Assert.That(restoredTargetChoice.LegalTargetHeroIds,
                Is.EqualTo(new[] { defenderId }));
            Assert.That(answer.Accepted, Is.True);
            Assert.That(answer.PendingResolution.AttackTotal, Is.EqualTo(2));
            Assert.That(restoredDefense.PendingDefenseChoice.ChooserId, Is.EqualTo(defenderId));
            Assert.That(restoredDefense.PendingDefenseChoice.AttackTotal, Is.EqualTo(2));
        }

        [Test]
        public void StartAttack_WhenNoAdjacentEnemyExists_RejectsWithoutPendingStateOrCardMutation()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var attackerId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var defenderId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var authority = new CardActionResolutionAuthority(new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(defenderId, new HexCoordinate(2, 0))
                }));
            var originalStates = configuration.CardInstances.Select(instance => instance.State).ToArray();

            var result = authority.StartAttackResolution(
                new ActionResolutionId("attack-resolution"),
                GetAttackOffer(configuration, attackerId),
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.TargetUnavailable));
            Assert.That(result.Events, Is.Empty);
            Assert.That(authority.PendingAttackResolution, Is.Null);
            Assert.That(authority.PendingAttackTargetChoice, Is.Null);
            Assert.That(
                configuration.CardInstances.Select(instance => instance.State),
                Is.EqualTo(originalStates));
        }

        [Test]
        public void TargetChoice_WhenBoardChanges_RejectsWithoutChangingPendingAttackOrCards()
        {
            var fixture = CreateFixture();
            var start = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("attack-resolution"),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            var originalStates = fixture.Configuration.CardInstances.Select(instance => instance.State).ToArray();
            var moved = fixture.BoardAuthority.TryCommit(new AdjacentMoveCommand(
                fixture.DefenderId,
                new HexCoordinate(1, 0),
                new HexCoordinate(2, 0)));

            var result = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    fixture.DefenderId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(moved.Accepted, Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.InvalidChoice));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.PendingResolution, Is.SameAs(start.PendingResolution));
            Assert.That(result.PendingTargetChoice, Is.SameAs(start.PendingTargetChoice));
            Assert.That(
                fixture.Configuration.CardInstances.Select(instance => instance.State),
                Is.EqualTo(originalStates));
        }

        [Test]
        public void PendingDefenseResponse_RestoresOnlyWhenChoiceAndAttackTotalMatchAuthority()
        {
            var fixture = CreateFixture();
            var start = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("attack-resolution"),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            var answer = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    fixture.DefenderId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            var restoredBoard = CreateBoardAuthority(fixture.AttackerId, fixture.DefenderId);

            var restored = CardActionResolutionAuthority.RestoreAttack(
                restoredBoard,
                answer.PendingResolution,
                null,
                answer.PendingDefenseChoice,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(restored.PendingAttackResolution, Is.SameAs(answer.PendingResolution));
            Assert.That(restored.PendingDefenseChoice, Is.SameAs(answer.PendingDefenseChoice));

            var tamperedChoice = new PendingDefenseResponseChoice(
                answer.PendingDefenseChoice.ResolutionId,
                answer.PendingDefenseChoice.ChoiceId,
                answer.PendingDefenseChoice.ChooserId,
                answer.PendingDefenseChoice.AttackerId,
                answer.PendingDefenseChoice.AttackTotal + 1,
                answer.PendingDefenseChoice.LegalDefenseOffers,
                answer.PendingDefenseChoice.CanDecline);
            Assert.Throws<ArgumentException>(() => CardActionResolutionAuthority.RestoreAttack(
                CreateBoardAuthority(fixture.AttackerId, fixture.DefenderId),
                answer.PendingResolution,
                null,
                tamperedChoice,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants));
        }

        [Test]
        public void DefenseResponse_DiscardsExactlyOneOwnedCardAndRecordsOnlyItsPrintedDefense()
        {
            var fixture = CreateFixture();
            var opened = OpenDefenseResponse(fixture);
            var selectedOffer = opened.PendingDefenseChoice.LegalDefenseOffers
                .Single(offer => PrintedValueQuery.CalculatePrintedValue(
                    offer,
                    fixture.Configuration.CardDefinitions,
                    PrintedValueChannel.Defense).Total == 3);
            var selectedCard = fixture.Configuration.CardInstances.Single(
                instance => instance.Id.Equals(selectedOffer.SourceCardInstanceId));
            var otherStates = fixture.Configuration.CardInstances
                .Where(instance => !instance.Id.Equals(selectedCard.Id))
                .ToDictionary(instance => instance.Id, instance => instance.State);

            var result = fixture.Authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId,
                    selectedCard.Id),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.PendingResolution.Phase, Is.EqualTo(AttackResolutionPhase.CompareOutcome));
            Assert.That(result.PendingDefenseChoice, Is.Null);
            Assert.That(fixture.Authority.PendingDefenseChoice, Is.Null);
            Assert.That(result.PendingResolution.DefenseOffer, Is.EqualTo(selectedOffer));
            Assert.That(result.PendingResolution.DefenseOffer.Value.InvocationContext,
                Is.EqualTo(ActionInvocationContext.Reaction));
            Assert.That(result.PendingResolution.InvocationContext,
                Is.EqualTo(ActionInvocationContext.PrintedPrimary));
            Assert.That(result.PendingResolution.DefenseValue.Channel, Is.EqualTo(PrintedValueChannel.Defense));
            Assert.That(result.PendingResolution.DefenseValue.Contributions, Has.Count.EqualTo(1));
            Assert.That(result.PendingResolution.DefenseValue.Contributions.Single().Kind,
                Is.EqualTo(PrintedValueContributionKind.PrintedIcon));
            Assert.That(result.PendingResolution.DefenseValue.Contributions.Single().SourceCardInstanceId,
                Is.EqualTo(selectedCard.Id));
            Assert.That(result.PendingResolution.DefenseTotal, Is.EqualTo(3));
            Assert.That(selectedCard.State, Is.EqualTo(CardStateFacts.DiscardedUnplayed()));
            Assert.That(
                fixture.Configuration.CardInstances
                    .Where(instance => !instance.Id.Equals(selectedCard.Id))
                    .All(instance => instance.State.Equals(otherStates[instance.Id])),
                Is.True);
            Assert.That(result.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.DefenseCardDiscarded,
                CardActionResolutionEventKind.DefenseTotalCalculated,
                CardActionResolutionEventKind.DefenseResponseCompleted
            }));
            Assert.That(result.Events.All(ruleEvent =>
                ruleEvent.DefenseCardInstanceId.Equals(selectedCard.Id) &&
                ruleEvent.DefenseTotal == 3), Is.True);
            Assert.That(
                ActionOfferQuery.GetAvailableOffers(
                    fixture.Configuration.CardDefinitions,
                    fixture.Configuration.CardInstances,
                    fixture.DefenderId,
                    ActionOfferWindow.NormalPlay)
                    .Any(offer => GetActionKind(offer, fixture.Configuration.CardDefinitions) == ActionKind.Defense),
                Is.False,
                "A Defense reaction must not become the defender's normal action.");
        }

        [Test]
        public void DefenseResponse_DeclineAndNoLegalCardProduceNoDefenseTotalOrCardMutation()
        {
            var fixture = CreateFixture();
            var attackerOnlyInstances = fixture.Configuration.CardInstances
                .Where(instance => instance.OwnerId.Equals(fixture.AttackerId))
                .ToArray();
            var start = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("no-defense-card-resolution"),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                attackerOnlyInstances,
                fixture.Configuration.Participants);
            var opened = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    fixture.DefenderId),
                fixture.Configuration.CardDefinitions,
                attackerOnlyInstances,
                fixture.Configuration.Participants);
            var originalStates = attackerOnlyInstances.Select(instance => instance.State).ToArray();

            var result = fixture.Authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.Decline(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId),
                fixture.Configuration.CardDefinitions,
                attackerOnlyInstances,
                fixture.Configuration.Participants);

            Assert.That(opened.PendingDefenseChoice.LegalDefenseOffers, Is.Empty);
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.PendingResolution.Phase, Is.EqualTo(AttackResolutionPhase.CompareOutcome));
            Assert.That(result.PendingResolution.HasDefenseTotal, Is.False);
            Assert.That(result.PendingResolution.DefenseTotal, Is.Null);
            Assert.That(result.PendingResolution.DefenseOffer, Is.Null);
            Assert.That(attackerOnlyInstances.Select(instance => instance.State), Is.EqualTo(originalStates));
            Assert.That(result.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.DefenseDeclined,
                CardActionResolutionEventKind.DefenseResponseCompleted
            }));
            Assert.That(result.Events.All(ruleEvent =>
                !ruleEvent.DefenseCardInstanceId.IsDefined && ruleEvent.DefenseTotal == 0), Is.True);
        }

        [Test]
        public void DefenseResponse_InvalidMissingMultipleCrossOwnerAndDiscardedAttemptsPreservePendingAttack()
        {
            var fixture = CreateFixture();
            var opened = OpenDefenseResponse(fixture);
            var choice = opened.PendingDefenseChoice;
            var legalCardIds = choice.LegalDefenseOffers
                .Select(offer => offer.SourceCardInstanceId)
                .ToArray();
            var originalStates = fixture.Configuration.CardInstances.Select(instance => instance.State).ToArray();
            var otherAttackerCardId = fixture.Configuration.CardInstances
                .Single(instance =>
                    instance.OwnerId.Equals(fixture.AttackerId) &&
                    !instance.Id.Equals(fixture.AttackOffer.SourceCardInstanceId))
                .Id;
            var invalidCommands = new[]
            {
                DefenseResponseChoiceCommand.SelectCards(choice.ResolutionId, choice.ChoiceId),
                DefenseResponseChoiceCommand.SelectCards(
                    choice.ResolutionId,
                    choice.ChoiceId,
                    legalCardIds[0],
                    legalCardIds[1]),
                DefenseResponseChoiceCommand.SelectCards(
                    choice.ResolutionId,
                    choice.ChoiceId,
                    fixture.AttackOffer.SourceCardInstanceId),
                DefenseResponseChoiceCommand.SelectCards(
                    choice.ResolutionId,
                    choice.ChoiceId,
                    otherAttackerCardId),
                DefenseResponseChoiceCommand.SelectCards(
                    choice.ResolutionId,
                    choice.ChoiceId,
                    new CardInstanceId("missing-defense-card"))
            };

            foreach (var command in invalidCommands)
            {
                var rejection = fixture.Authority.AnswerDefenseResponse(
                    command,
                    fixture.Configuration.CardDefinitions,
                    fixture.Configuration.CardInstances,
                    fixture.Configuration.Participants);
                Assert.That(rejection.Accepted, Is.False);
                Assert.That(rejection.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.InvalidChoice));
                Assert.That(rejection.PendingResolution, Is.SameAs(opened.PendingResolution));
                Assert.That(rejection.PendingDefenseChoice, Is.SameAs(choice));
                Assert.That(rejection.Events, Is.Empty);
                Assert.That(fixture.Configuration.CardInstances.Select(instance => instance.State), Is.EqualTo(originalStates));
            }

            var selectedCard = fixture.Configuration.CardInstances.Single(instance => instance.Id.Equals(legalCardIds[0]));
            var staleInstances = fixture.Configuration.CardInstances
                .Select(instance => instance.Id.Equals(selectedCard.Id)
                    ? new CardInstance(instance.Id, instance.DefinitionReference, instance.OwnerId, CardStateFacts.DiscardedUnplayed())
                    : instance)
                .ToArray();
            var discardedRejection = fixture.Authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(choice.ResolutionId, choice.ChoiceId, selectedCard.Id),
                fixture.Configuration.CardDefinitions,
                staleInstances,
                fixture.Configuration.Participants);

            Assert.That(discardedRejection.Accepted, Is.False);
            Assert.That(discardedRejection.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.InvalidChoice));
            Assert.That(discardedRejection.PendingResolution, Is.SameAs(opened.PendingResolution));
            Assert.That(discardedRejection.PendingDefenseChoice, Is.SameAs(choice));
            Assert.That(fixture.Configuration.CardInstances.Select(instance => instance.State), Is.EqualTo(originalStates));
        }

        [Test]
        public void DefenseResponse_StaleCommandDoesNotReuseDiscardedCardOrChangeResumedAttack()
        {
            var fixture = CreateFixture();
            var opened = OpenDefenseResponse(fixture);
            var selectedCardId = opened.PendingDefenseChoice.LegalDefenseOffers[0].SourceCardInstanceId;
            var stale = fixture.Authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    new ActionResolutionStepId("stale-defense-choice"),
                    selectedCardId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(stale.Accepted, Is.False);
            Assert.That(stale.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.StaleChoice));
            Assert.That(fixture.Configuration.CardInstances.Single(instance => instance.Id.Equals(selectedCardId)).State,
                Is.EqualTo(CardStateFacts.InitialInHand()));

            var command = DefenseResponseChoiceCommand.SelectCards(
                opened.PendingDefenseChoice.ResolutionId,
                opened.PendingDefenseChoice.ChoiceId,
                selectedCardId);
            var accepted = fixture.Authority.AnswerDefenseResponse(
                command,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            var replay = fixture.Authority.AnswerDefenseResponse(
                command,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(accepted.Accepted, Is.True);
            Assert.That(replay.Accepted, Is.False);
            Assert.That(replay.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.NoPendingResolution));
            Assert.That(replay.PendingResolution, Is.SameAs(accepted.PendingResolution));
            Assert.That(replay.Events, Is.Empty);
            Assert.That(fixture.Configuration.CardInstances.Single(instance => instance.Id.Equals(selectedCardId)).State,
                Is.EqualTo(CardStateFacts.DiscardedUnplayed()));
        }

        [Test]
        public void CompletedDefenseResponse_RestoresAtOutcomeComparisonCheckpoint()
        {
            var fixture = CreateFixture();
            var opened = OpenDefenseResponse(fixture);
            var selectedCardId = opened.PendingDefenseChoice.LegalDefenseOffers[0].SourceCardInstanceId;
            var instancesWithFacedownDefense = fixture.Configuration.CardInstances
                .Select(instance => instance.Id.Equals(selectedCardId)
                    ? new CardInstance(
                        instance.Id,
                        instance.DefinitionReference,
                        instance.OwnerId,
                        new CardStateFacts(
                            CardLocation.Hand,
                            false,
                            CardResolutionStatus.None,
                            CardFaceOrientation.FaceDown))
                    : instance)
                .ToArray();
            var answered = fixture.Authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId,
                    selectedCardId),
                fixture.Configuration.CardDefinitions,
                instancesWithFacedownDefense,
                fixture.Configuration.Participants);

            var restored = CardActionResolutionAuthority.RestoreAttack(
                CreateBoardAuthority(fixture.AttackerId, fixture.DefenderId),
                answered.PendingResolution,
                null,
                null,
                fixture.Configuration.CardDefinitions,
                instancesWithFacedownDefense,
                fixture.Configuration.Participants);

            Assert.That(restored.PendingAttackResolution, Is.SameAs(answered.PendingResolution));
            Assert.That(restored.PendingAttackResolution.Phase, Is.EqualTo(AttackResolutionPhase.CompareOutcome));
            Assert.That(restored.PendingAttackResolution.DefenseTotal, Is.Not.Null);
            Assert.That(restored.PendingDefenseChoice, Is.Null);
            Assert.That(
                instancesWithFacedownDefense.Single(instance => instance.Id.Equals(selectedCardId)).State,
                Is.EqualTo(new CardStateFacts(
                    CardLocation.DiscardPile,
                    false,
                    CardResolutionStatus.None,
                    CardFaceOrientation.FaceDown)));
        }

        [Test]
        public void OutcomeComparison_EqualDefenseSuccessfullyDefendsAndCompletesSameAttack()
        {
            var fixture = CreateFixture();
            var opened = OpenDefenseResponse(fixture);
            var defenseOffer = opened.PendingDefenseChoice.LegalDefenseOffers.Single(offer =>
                PrintedValueQuery.CalculatePrintedValue(
                    offer,
                    fixture.Configuration.CardDefinitions,
                    PrintedValueChannel.Defense).Total == opened.PendingResolution.AttackTotal);
            var response = fixture.Authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId,
                    defenseOffer.SourceCardInstanceId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            var outcome = fixture.Authority.ResolveAttackOutcome(
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(response.PendingResolution.ResolutionId, Is.EqualTo(opened.PendingResolution.ResolutionId));
            Assert.That(response.PendingResolution.AttackTotal, Is.EqualTo(3));
            Assert.That(response.PendingResolution.DefenseTotal, Is.EqualTo(3));
            Assert.That(outcome.Accepted, Is.True);
            Assert.That(outcome.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended));
            Assert.That(outcome.PendingResolution, Is.Null);
            Assert.That(fixture.Authority.PendingAttackResolution, Is.Null);
            Assert.That(fixture.Authority.PendingDefenseChoice, Is.Null);
            Assert.That(outcome.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.AttackSuccessfullyDefended,
                CardActionResolutionEventKind.AttackResolutionCompleted
            }));
            Assert.That(outcome.Events.All(ruleEvent =>
                ruleEvent.ResolutionId.Equals(opened.PendingResolution.ResolutionId) &&
                ruleEvent.TargetHeroId.Equals(fixture.DefenderId) &&
                ruleEvent.AttackTotal == 3 &&
                ruleEvent.DefenseTotal == 3 &&
                ruleEvent.AttackOutcome == AttackResolutionOutcome.SuccessfullyDefended), Is.True);
        }

        [Test]
        public void OutcomeComparison_GreaterDefenseSuccessfullyDefends()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var attackerId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var defenderId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var authority = new CardActionResolutionAuthority(new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(defenderId, new HexCoordinate(2, 0))
                }));
            var start = authority.StartAttackResolution(
                new ActionResolutionId("greater-defense-outcome"),
                GetRangedAttackOffer(configuration, attackerId),
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants);
            var opened = authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    defenderId),
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants);
            var defenseOffer = opened.PendingDefenseChoice.LegalDefenseOffers.Single(offer =>
                PrintedValueQuery.CalculatePrintedValue(
                    offer,
                    configuration.CardDefinitions,
                    PrintedValueChannel.Defense).Total == 3);
            authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId,
                    defenseOffer.SourceCardInstanceId),
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants);

            var outcome = authority.ResolveAttackOutcome(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants);

            Assert.That(opened.PendingResolution.AttackTotal, Is.EqualTo(2));
            Assert.That(outcome.Accepted, Is.True);
            Assert.That(outcome.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended));
            Assert.That(outcome.Events, Has.All.Matches<CardActionResolutionEvent>(ruleEvent =>
                ruleEvent.AttackTotal == 2 &&
                ruleEvent.DefenseTotal == 3 &&
                ruleEvent.AttackOutcome == AttackResolutionOutcome.SuccessfullyDefended));
        }

        [Test]
        public void OutcomeComparison_LowerDefenseMakesAttackSuccessful()
        {
            var fixture = CreateFixture();
            var opened = OpenDefenseResponse(fixture);
            var defenseOffer = opened.PendingDefenseChoice.LegalDefenseOffers.Single(offer =>
                PrintedValueQuery.CalculatePrintedValue(
                    offer,
                    fixture.Configuration.CardDefinitions,
                    PrintedValueChannel.Defense).Total < opened.PendingResolution.AttackTotal);
            fixture.Authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId,
                    defenseOffer.SourceCardInstanceId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            var outcome = fixture.Authority.ResolveAttackOutcome(
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(outcome.Accepted, Is.True);
            Assert.That(outcome.Outcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful));
            Assert.That(outcome.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.AttackSuccessful,
                CardActionResolutionEventKind.AttackResolutionCompleted
            }));
            Assert.That(outcome.Events.All(ruleEvent =>
                ruleEvent.AttackTotal == 3 &&
                ruleEvent.DefenseTotal == 2 &&
                ruleEvent.AttackOutcome == AttackResolutionOutcome.AttackSuccessful), Is.True);
        }

        [Test]
        public void OutcomeComparison_DeclineOrNoLegalDefenseMakesAttackSuccessful()
        {
            foreach (var removeDefenderCards in new[] { false, true })
            {
                var fixture = CreateFixture();
                var instances = removeDefenderCards
                    ? fixture.Configuration.CardInstances
                        .Where(instance => instance.OwnerId.Equals(fixture.AttackerId))
                        .ToArray()
                    : fixture.Configuration.CardInstances;
                var start = fixture.Authority.StartAttackResolution(
                    new ActionResolutionId(removeDefenderCards
                        ? "unable-to-defend-outcome"
                        : "declined-defense-outcome"),
                    fixture.AttackOffer,
                    fixture.Configuration.CardDefinitions,
                    instances,
                    fixture.Configuration.Participants);
                var opened = fixture.Authority.AnswerAttackTarget(
                    new AttackTargetChoiceCommand(
                        start.PendingTargetChoice.ResolutionId,
                        start.PendingTargetChoice.ChoiceId,
                        fixture.DefenderId),
                    fixture.Configuration.CardDefinitions,
                    instances,
                    fixture.Configuration.Participants);
                var response = fixture.Authority.AnswerDefenseResponse(
                    DefenseResponseChoiceCommand.Decline(
                        opened.PendingDefenseChoice.ResolutionId,
                        opened.PendingDefenseChoice.ChoiceId),
                    fixture.Configuration.CardDefinitions,
                    instances,
                    fixture.Configuration.Participants);

                var outcome = fixture.Authority.ResolveAttackOutcome(
                    fixture.Configuration.CardDefinitions,
                    instances,
                    fixture.Configuration.Participants);

                Assert.That(opened.PendingDefenseChoice.LegalDefenseOffers.Count == 0,
                    Is.EqualTo(removeDefenderCards));
                Assert.That(response.PendingResolution.HasDefenseTotal, Is.False);
                Assert.That(outcome.Accepted, Is.True);
                Assert.That(outcome.Outcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful));
                Assert.That(outcome.Events, Has.All.Matches<CardActionResolutionEvent>(ruleEvent =>
                    ruleEvent.DefenseTotal == 0 &&
                    !ruleEvent.DefenseCardInstanceId.IsDefined &&
                    ruleEvent.AttackOutcome == AttackResolutionOutcome.AttackSuccessful));
            }
        }

        [Test]
        public void OutcomeComparison_StaleDefenseStateRejectsWithoutClosingPendingAttack()
        {
            var fixture = CreateFixture();
            var opened = OpenDefenseResponse(fixture);
            var defenseOffer = opened.PendingDefenseChoice.LegalDefenseOffers[0];
            var response = fixture.Authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId,
                    defenseOffer.SourceCardInstanceId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            var staleInstances = fixture.Configuration.CardInstances.Select(instance =>
                instance.Id.Equals(defenseOffer.SourceCardInstanceId)
                    ? new CardInstance(
                        instance.Id,
                        instance.DefinitionReference,
                        instance.OwnerId,
                        CardStateFacts.InitialInHand())
                    : instance).ToArray();

            var rejected = fixture.Authority.ResolveAttackOutcome(
                fixture.Configuration.CardDefinitions,
                staleInstances,
                fixture.Configuration.Participants);

            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.InvalidChoice));
            Assert.That(rejected.PendingResolution, Is.SameAs(response.PendingResolution));
            Assert.That(rejected.Outcome, Is.EqualTo(AttackResolutionOutcome.None));
            Assert.That(rejected.Events, Is.Empty);
            Assert.That(fixture.Authority.PendingAttackResolution, Is.SameAs(response.PendingResolution));
        }

        [Test]
        public void SameDefenseResponseCommands_ProduceSameResumedStateAndOrderedEvents()
        {
            var first = RunDefenseReplay();
            var second = RunDefenseReplay();

            Assert.That(first.Events, Is.EqualTo(second.Events));
            Assert.That(first.DefenseCardInstanceId, Is.EqualTo(second.DefenseCardInstanceId));
            Assert.That(first.DefenseTotal, Is.EqualTo(second.DefenseTotal));
            Assert.That(first.DefenseCardState, Is.EqualTo(second.DefenseCardState));
            Assert.That(first.Phase, Is.EqualTo(AttackResolutionPhase.CompareOutcome));
            Assert.That(second.Phase, Is.EqualTo(AttackResolutionPhase.CompareOutcome));
        }

        [Test]
        public void SameOutcomeComparisons_ProduceSameResultAndOrderedEvents()
        {
            var first = RunOutcomeReplay();
            var second = RunOutcomeReplay();

            Assert.That(first.Outcome, Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended));
            Assert.That(second.Outcome, Is.EqualTo(first.Outcome));
            Assert.That(second.Events, Is.EqualTo(first.Events));
            Assert.That(first.PendingResolution, Is.Null);
            Assert.That(second.PendingResolution, Is.Null);
        }

        [Test]
        public void SameAttackCommands_ProduceSamePendingStateAndOrderedEvents()
        {
            var first = RunAttackReplay();
            var second = RunAttackReplay();

            Assert.That(first.Events, Is.EqualTo(second.Events));
            Assert.That(first.TargetHeroId, Is.EqualTo(second.TargetHeroId));
            Assert.That(first.AttackTotal, Is.EqualTo(second.AttackTotal));
            Assert.That(first.DefenseChooserId, Is.EqualTo(second.DefenseChooserId));
        }

        [Test]
        public void StartAttack_RejectsForgedOfferAndCannotOverlapMovementResolution()
        {
            var fixture = CreateFixture();
            var forgedOffer = new ActionOffer(
                new CardInstanceId("forged-card"),
                fixture.AttackOffer.SourceDefinitionReference,
                fixture.AttackOffer.ActionDefinitionId,
                fixture.AttackerId,
                fixture.AttackOffer.InvocationContext,
                true,
                AttackRangeKind.NonRanged,
                default);
            var forged = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("forged-attack"),
                forgedOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(forged.Accepted, Is.False);
            Assert.That(forged.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.UnavailableOffer));
            Assert.That(fixture.Authority.PendingAttackResolution, Is.Null);

            var movementOffer = ActionOfferQuery.GetAvailableOffers(
                    fixture.Configuration.CardDefinitions,
                    fixture.Configuration.CardInstances,
                    fixture.AttackerId,
                    ActionOfferWindow.NormalPlay)
                .First(offer => offer.ActionDefinitionId.Equals(new ActionDefinitionId("primary-movement")));
            var movement = fixture.Authority.StartMovementResolution(
                new ActionResolutionId("movement-resolution"),
                movementOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances);
            var overlap = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("overlap-attack"),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(movement.Accepted, Is.True);
            Assert.That(overlap.Accepted, Is.False);
            Assert.That(overlap.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.ResolutionAlreadyPending));
            Assert.That(fixture.Authority.PendingResolution, Is.SameAs(movement.PendingResolution));
        }

        private static ReplayResult RunAttackReplay()
        {
            var fixture = CreateFixture();
            var events = new List<CardActionResolutionEvent>();
            var start = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("attack-resolution"),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            events.AddRange(start.Events);
            var answer = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    fixture.DefenderId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            events.AddRange(answer.Events);

            return new ReplayResult(
                answer.PendingResolution.TargetHeroId,
                answer.PendingResolution.AttackTotal,
                answer.PendingDefenseChoice.ChooserId,
                events);
        }

        private static AttackActionResolutionResult OpenDefenseResponse(AttackFixture fixture)
        {
            var start = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("defense-response-resolution"),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            return fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    fixture.DefenderId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
        }

        private static DefenseReplayResult RunDefenseReplay()
        {
            var fixture = CreateFixture();
            var opened = OpenDefenseResponse(fixture);
            var selectedCardId = opened.PendingDefenseChoice.LegalDefenseOffers[0].SourceCardInstanceId;
            var result = fixture.Authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId,
                    selectedCardId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            var cardState = fixture.Configuration.CardInstances.Single(
                instance => instance.Id.Equals(selectedCardId)).State;
            return new DefenseReplayResult(
                selectedCardId,
                result.PendingResolution.DefenseTotal.Value,
                cardState,
                result.PendingResolution.Phase,
                result.Events);
        }

        private static AttackActionResolutionResult RunOutcomeReplay()
        {
            var fixture = CreateFixture();
            var opened = OpenDefenseResponse(fixture);
            var defenseOffer = opened.PendingDefenseChoice.LegalDefenseOffers.Single(offer =>
                PrintedValueQuery.CalculatePrintedValue(
                    offer,
                    fixture.Configuration.CardDefinitions,
                    PrintedValueChannel.Defense).Total == opened.PendingResolution.AttackTotal);
            fixture.Authority.AnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    opened.PendingDefenseChoice.ResolutionId,
                    opened.PendingDefenseChoice.ChoiceId,
                    defenseOffer.SourceCardInstanceId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            return fixture.Authority.ResolveAttackOutcome(
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
        }

        private static ActionKind GetActionKind(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions)
        {
            var definition = definitions.Single(candidate =>
                candidate.Reference.Equals(offer.SourceDefinitionReference));
            return definition.ActionDefinitions.Single(candidate =>
                candidate.Id.Equals(offer.ActionDefinitionId)).Kind;
        }

        private static AttackFixture CreateFixture()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var attackerId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var defenderId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var boardAuthority = CreateBoardAuthority(attackerId, defenderId);
            return new AttackFixture(
                configuration,
                attackerId,
                defenderId,
                boardAuthority,
                new CardActionResolutionAuthority(boardAuthority),
                GetAttackOffer(configuration, attackerId));
        }

        private static AdjacentMoveAuthority CreateBoardAuthority(HeroId attackerId, HeroId defenderId)
        {
            return new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(defenderId, new HexCoordinate(1, 0))
                });
        }

        private static ActionOffer GetAttackOffer(
            PlaceholderOneVsOneTestConfiguration configuration,
            HeroId attackerId)
        {
            return ActionOfferQuery.GetAvailableOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    attackerId,
                    ActionOfferWindow.NormalPlay)
                .Single(offer => offer.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
        }

        private static ActionOffer GetRangedAttackOffer(
            PlaceholderOneVsOneTestConfiguration configuration,
            HeroId attackerId)
        {
            return ActionOfferQuery.GetAvailableOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    attackerId,
                    ActionOfferWindow.NormalPlay)
                .Single(offer => offer.ActionDefinitionId.Equals(new ActionDefinitionId("primary-ranged-attack")));
        }

        private static MatchParticipants CreateFourHeroParticipants(
            HeroId attackerId,
            HeroId adjacentEnemyId,
            HeroId friendlyId,
            HeroId nonAdjacentEnemyId)
        {
            var redTeamId = new TeamId(PlaceholderOneVsOneParticipants.RedTeamIdValue);
            var blueTeamId = new TeamId(PlaceholderOneVsOneParticipants.BlueTeamIdValue);
            var redPlayerId = new PlayerId(PlaceholderOneVsOneParticipants.RedPlayerIdValue);
            var bluePlayerId = new PlayerId(PlaceholderOneVsOneParticipants.BluePlayerIdValue);
            var friendlyPlayerId = new PlayerId("red-friendly-player");
            var farPlayerId = new PlayerId("blue-far-player");
            return new MatchParticipants(
                new[]
                {
                    new Team(redTeamId, TeamSide.Red),
                    new Team(blueTeamId, TeamSide.Blue)
                },
                new[]
                {
                    new Player(redPlayerId, redTeamId),
                    new Player(bluePlayerId, blueTeamId),
                    new Player(friendlyPlayerId, redTeamId),
                    new Player(farPlayerId, blueTeamId)
                },
                new[]
                {
                    new Hero(attackerId, redPlayerId),
                    new Hero(adjacentEnemyId, bluePlayerId),
                    new Hero(friendlyId, friendlyPlayerId),
                    new Hero(nonAdjacentEnemyId, farPlayerId)
                });
        }

        private static IEnumerable<HexCoordinate> CreateRadiusThreeCoordinates()
        {
            const int radius = 3;
            for (var q = -radius; q <= radius; q++)
            {
                var minimumR = Math.Max(-radius, -q - radius);
                var maximumR = Math.Min(radius, -q + radius);
                for (var r = minimumR; r <= maximumR; r++)
                {
                    yield return new HexCoordinate(q, r);
                }
            }
        }

        private sealed class AttackFixture
        {
            public AttackFixture(
                PlaceholderOneVsOneTestConfiguration configuration,
                HeroId attackerId,
                HeroId defenderId,
                AdjacentMoveAuthority boardAuthority,
                CardActionResolutionAuthority authority,
                ActionOffer attackOffer)
            {
                Configuration = configuration;
                AttackerId = attackerId;
                DefenderId = defenderId;
                BoardAuthority = boardAuthority;
                Authority = authority;
                AttackOffer = attackOffer;
            }

            public PlaceholderOneVsOneTestConfiguration Configuration { get; }
            public HeroId AttackerId { get; }
            public HeroId DefenderId { get; }
            public AdjacentMoveAuthority BoardAuthority { get; }
            public CardActionResolutionAuthority Authority { get; }
            public ActionOffer AttackOffer { get; }
        }

        private readonly struct ReplayResult
        {
            public ReplayResult(
                HeroId targetHeroId,
                int attackTotal,
                HeroId defenseChooserId,
                IEnumerable<CardActionResolutionEvent> events)
            {
                TargetHeroId = targetHeroId;
                AttackTotal = attackTotal;
                DefenseChooserId = defenseChooserId;
                Events = Array.AsReadOnly(events.ToArray());
            }

            public HeroId TargetHeroId { get; }
            public int AttackTotal { get; }
            public HeroId DefenseChooserId { get; }
            public IReadOnlyList<CardActionResolutionEvent> Events { get; }
        }

        private readonly struct DefenseReplayResult
        {
            public DefenseReplayResult(
                CardInstanceId defenseCardInstanceId,
                int defenseTotal,
                CardStateFacts defenseCardState,
                AttackResolutionPhase phase,
                IEnumerable<CardActionResolutionEvent> events)
            {
                DefenseCardInstanceId = defenseCardInstanceId;
                DefenseTotal = defenseTotal;
                DefenseCardState = defenseCardState;
                Phase = phase;
                Events = Array.AsReadOnly(events.ToArray());
            }

            public CardInstanceId DefenseCardInstanceId { get; }
            public int DefenseTotal { get; }
            public CardStateFacts DefenseCardState { get; }
            public AttackResolutionPhase Phase { get; }
            public IReadOnlyList<CardActionResolutionEvent> Events { get; }
        }
    }
}
