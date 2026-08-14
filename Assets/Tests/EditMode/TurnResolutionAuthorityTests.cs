using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TurnResolutionAuthorityTests
    {
        [Test]
        public void TwoUnresolvedCards_PerformHoldInInitiativeOrderAndEndTurn()
        {
            var fixture = CreateFixture(7, 4, TeamSide.Red);

            var start = fixture.Authority.TryStart();

            Assert.That(start.Accepted, Is.True);
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.Red.Id));

            var firstHold = fixture.Authority.TryPerformHold(fixture.Red.Id);

            Assert.That(firstHold.Accepted, Is.True);
            Assert.That(fixture.Red.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(fixture.Blue.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.Blue.Id));
            Assert.That(firstHold.Events.Any(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.TurnEnded), Is.False);

            var secondHold = fixture.Authority.TryPerformHold(fixture.Blue.Id);

            Assert.That(secondHold.Accepted, Is.True);
            Assert.That(fixture.Blue.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(fixture.Authority.SelectedActor.IsDefined, Is.False);
            Assert.That(fixture.Authority.IsEnded, Is.True);
            Assert.That(secondHold.Events.Select(ruleEvent => ruleEvent.Kind).TakeLast(2), Is.EqualTo(new[]
            {
                TurnResolutionEventKind.TurnOnlyEffectsCancellationCheckpoint,
                TurnResolutionEventKind.TurnEnded
            }));

            var holdEvent = firstHold.Events.Single(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.HoldActionPerformed);
            Assert.That(holdEvent.ActionDefinitionId,
                Is.EqualTo(GeneralRuleActionOfferQuery.HoldDefinition.Id));
            Assert.That(holdEvent.InvocationContext,
                Is.EqualTo(ActionInvocationContext.GeneralRuleSecondary));

            AssertRejected(
                fixture.Authority.TryPerformHold(fixture.Blue.Id),
                TurnResolutionRejectionReason.TurnAlreadyEnded);
        }

        [Test]
        public void CrossTeamTie_FlipsAfterResolvedFactAndBeforeNextActorSelection()
        {
            var fixture = CreateFixture(7, 7, TeamSide.Red);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.Red.Id));
            Assert.That(fixture.Authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Red));

            var result = fixture.Authority.TryPerformHold(fixture.Red.Id);

            Assert.That(result.Accepted, Is.True);
            Assert.That(fixture.Authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.Blue.Id));
            var kinds = EventKinds(result).ToArray();
            Assert.That(System.Array.IndexOf(kinds, InitiativeResolutionEventKind.TieBreakerFlipped),
                Is.GreaterThan(System.Array.IndexOf(kinds, InitiativeResolutionEventKind.SelectedActorFinished)));
            Assert.That(System.Array.IndexOf(kinds, InitiativeResolutionEventKind.NextActorSelected),
                Is.GreaterThan(System.Array.IndexOf(kinds, InitiativeResolutionEventKind.TieBreakerFlipped)));
        }

        [Test]
        public void AfterHold_NextActorIsRequeriedFromCurrentUnresolvedCards()
        {
            var participants = CreateParticipants(2, 1);
            var firstDefinition = CreateDefinition("first", 10);
            var lowerDefinition = CreateDefinition("lower", 5);
            var nextDefinition = CreateDefinition("next", 8);
            var first = CreateRevealedCard("first-card", firstDefinition, "red-hero-1");
            var lower = CreateRevealedCard("lower-card", lowerDefinition, "red-hero-2");
            var next = CreateRevealedCard("next-card", nextDefinition, "blue-hero-1");
            var authority = new TurnResolutionAuthority(
                new[] { lowerDefinition, nextDefinition, firstDefinition },
                new[] { lower, next, first },
                participants,
                new TieBreakerState(TeamSide.Red));
            Assert.That(authority.TryStart().Accepted, Is.True);
            Assert.That(authority.SelectedActor.CardInstanceId, Is.EqualTo(first.Id));

            Assert.That(authority.TryPerformHold(first.Id).Accepted, Is.True);

            Assert.That(authority.SelectedActor.CardInstanceId, Is.EqualTo(next.Id));
            Assert.That(authority.SelectedActor.InitiativeTotal, Is.EqualTo(8));
        }

        [Test]
        public void SameTeamTie_UsesTeamChoiceWithoutFlippingTieBreaker()
        {
            var participants = CreateParticipants(2, 0);
            var firstDefinition = CreateDefinition("first", 6);
            var secondDefinition = CreateDefinition("second", 6);
            var first = CreateRevealedCard("first-card", firstDefinition, "red-hero-1");
            var second = CreateRevealedCard("second-card", secondDefinition, "red-hero-2");
            var authority = new TurnResolutionAuthority(
                new[] { firstDefinition, secondDefinition },
                new[] { second, first },
                participants,
                new TieBreakerState(TeamSide.Blue));

            Assert.That(authority.TryStart().Accepted, Is.True);
            Assert.That(authority.PendingChoice.OwningSide, Is.EqualTo(TeamSide.Red));
            Assert.That(authority.SelectedActor.IsDefined, Is.False);
            AssertRejected(
                authority.TryChooseTiedActor(TeamSide.Blue, first.Id),
                TurnResolutionRejectionReason.InitiativeCommandRejected);
            Assert.That(authority.PendingChoice, Is.Not.Null);

            Assert.That(authority.TryChooseTiedActor(TeamSide.Red, second.Id).Accepted, Is.True);
            Assert.That(authority.SelectedActor.CardInstanceId, Is.EqualTo(second.Id));
            Assert.That(authority.TryPerformHold(second.Id).Accepted, Is.True);

            Assert.That(authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(authority.SelectedActor.CardInstanceId, Is.EqualTo(first.Id));
        }

        [Test]
        public void RepeatedOrWrongHeroHold_IsRejectedWithoutAnotherActionOrMutation()
        {
            var fixture = CreateFixture(7, 4, TeamSide.Red);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);
            var blueState = fixture.Blue.State;

            AssertRejected(
                fixture.Authority.TryPerformHold(fixture.Blue.Id),
                TurnResolutionRejectionReason.CardNotSelectedForAction);
            Assert.That(fixture.Blue.State, Is.EqualTo(blueState));
            Assert.That(fixture.Authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Red));

            Assert.That(fixture.Authority.TryPerformHold(fixture.Red.Id).Accepted, Is.True);
            var redResolvedState = fixture.Red.State;
            AssertRejected(
                fixture.Authority.TryPerformHold(fixture.Red.Id),
                TurnResolutionRejectionReason.CardNotSelectedForAction);
            Assert.That(fixture.Red.State, Is.EqualTo(redResolvedState));
            Assert.That(fixture.Blue.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
        }

        [Test]
        public void TurnOnlyEffectsCancellationCheckpoint_OccursOnlyWhenLastCardResolves()
        {
            var fixture = CreateFixture(7, 4, TeamSide.Blue);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);

            var firstHold = fixture.Authority.TryPerformHold(fixture.Red.Id);
            var secondHold = fixture.Authority.TryPerformHold(fixture.Blue.Id);

            Assert.That(firstHold.Events.Any(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.TurnOnlyEffectsCancellationCheckpoint), Is.False);
            Assert.That(secondHold.Events.Count(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.TurnOnlyEffectsCancellationCheckpoint), Is.EqualTo(1));
            Assert.That(secondHold.Events.Last().Kind, Is.EqualTo(TurnResolutionEventKind.TurnEnded));
        }

        [Test]
        public void HoldOffer_IsGeneralRuleSecondaryForAnyCurrentPlayedCardOnly()
        {
            var definition = CreateDefinition("offer", 5);
            var actorId = new HeroId("red-hero-1");
            var card = CreateRevealedCard("offer-card", definition, actorId.Value);

            var offer = GeneralRuleActionOfferQuery.GetAvailableHoldOffer(
                card.Id,
                actorId,
                new[] { definition },
                new[] { card });

            Assert.That(offer.HasValue, Is.True);
            Assert.That(offer.Value.SourceCardInstanceId, Is.EqualTo(card.Id));
            Assert.That(offer.Value.SourceDefinitionReference, Is.EqualTo(definition.Reference));
            Assert.That(offer.Value.ActionDefinitionId,
                Is.EqualTo(GeneralRuleActionOfferQuery.HoldDefinition.Id));
            Assert.That(GeneralRuleActionOfferQuery.HoldDefinition.Kind, Is.EqualTo(ActionKind.Hold));
            Assert.That(GeneralRuleActionOfferQuery.HoldDefinition.Origin,
                Is.EqualTo(ActionDefinitionOrigin.GeneralRuleSecondary));
            Assert.That(GeneralRuleActionOfferQuery.HoldDefinition.DefaultPolicy.CardTextApplication,
                Is.EqualTo(CardTextApplicationPolicy.Suppressed));

            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(card), Is.True);
            Assert.That(GeneralRuleActionOfferQuery.GetAvailableHoldOffer(
                card.Id,
                actorId,
                new[] { definition },
                new[] { card }), Is.Null);
        }

        [Test]
        public void TurnEvents_RejectUndefinedSemanticPayloads()
        {
            Assert.Throws<ArgumentException>(() =>
                TurnResolutionEvent.FromInitiative(default));
            Assert.Throws<ArgumentException>(() =>
                TurnResolutionEvent.HoldPerformed(default));
            Assert.Throws<ArgumentException>(() =>
                TurnResolutionEvent.FromCardAction(default));
            Assert.Throws<ArgumentException>(() =>
                TurnResolutionEvent.FromAttackAction(default));
            Assert.Throws<ArgumentException>(() =>
                TurnResolutionEvent.CardWasResolved(default, default));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TurnResolutionEvent.TurnCheckpoint(TurnResolutionEventKind.CardResolved));
        }

        [Test]
        public void SelectedActor_CompletesSegmentedMovementThenResolvesAndYields()
        {
            var fixture = CreateMovementFixture(7, 4, 2);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);

            var offers = ActionOfferQuery.GetAvailableTurnActionOffers(
                fixture.Definitions,
                fixture.Instances,
                fixture.Red.OwnerId,
                fixture.Red.Id);
            Assert.That(offers, Has.Count.EqualTo(1));
            Assert.That(offers[0].SourceCardInstanceId, Is.EqualTo(fixture.Red.Id));
            Assert.That(ActionOfferQuery.GetAvailableOffers(
                fixture.Definitions,
                fixture.Instances,
                fixture.Red.OwnerId), Is.Empty,
                "The legacy in-hand offer path must remain distinct from played turn offers.");

            var start = fixture.Authority.TryStartMovement(
                new ActionResolutionId("turn-red-move"),
                offers[0]);

            Assert.That(start.Accepted, Is.True);
            Assert.That(fixture.Red.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            Assert.That(fixture.Authority.PendingMovementChoice, Is.Not.Null);
            var firstChoice = fixture.Authority.PendingMovementChoice;
            var destination = new HexCoordinate(1, 0);
            Assert.That(firstChoice.LegalDestinations.Select(candidate => candidate.Destination),
                Does.Contain(destination));

            var partial = fixture.Authority.TryAnswerMovementChoice(
                MovementChoiceCommand.SelectDestination(
                    firstChoice.ResolutionId,
                    firstChoice.ChoiceId,
                    destination));

            Assert.That(partial.Accepted, Is.True);
            Assert.That(fixture.Red.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.Red.Id));
            Assert.That(fixture.Board.Occupancies.Single(occupancy =>
                occupancy.HeroId.Equals(fixture.Red.OwnerId)).Coordinate, Is.EqualTo(destination));
            AssertRejected(
                fixture.Authority.TryPerformHold(fixture.Red.Id),
                TurnResolutionRejectionReason.ActionAlreadyPending);

            var stopChoice = fixture.Authority.PendingMovementChoice;
            var completed = fixture.Authority.TryAnswerMovementChoice(
                MovementChoiceCommand.Stop(stopChoice.ResolutionId, stopChoice.ChoiceId));

            Assert.That(completed.Accepted, Is.True);
            Assert.That(fixture.Red.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(fixture.Blue.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.Blue.Id));
            Assert.That(fixture.Authority.PendingMovementResolution, Is.Null);
            var kinds = completed.Events.Select(ruleEvent => ruleEvent.Kind).ToArray();
            Assert.That(System.Array.IndexOf(kinds, TurnResolutionEventKind.CardResolved),
                Is.GreaterThan(System.Array.IndexOf(kinds, TurnResolutionEventKind.MovementActionProgressed)));
            Assert.That(completed.Events.Last(ruleEvent =>
                    ruleEvent.Kind == TurnResolutionEventKind.InitiativeProgressed)
                .InitiativeEvent.Kind, Is.EqualTo(InitiativeResolutionEventKind.NextActorSelected));
        }

        [Test]
        public void MovementWithinTurn_ReusesObstacleRouteAndDeferredTieBreakerOrdering()
        {
            var fixture = CreateMovementFixture(7, 7, 3, true);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);
            var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                fixture.Definitions,
                fixture.Instances,
                fixture.Red.OwnerId,
                fixture.Red.Id).Single();
            Assert.That(fixture.Authority.TryStartMovement(
                new ActionResolutionId("turn-obstacle-move"),
                offer).Accepted, Is.True);
            var choice = fixture.Authority.PendingMovementChoice;
            var destination = new HexCoordinate(2, 0);
            var candidate = choice.LegalDestinations.Single(item => item.Destination.Equals(destination));
            Assert.That(candidate.Cost, Is.EqualTo(3));
            Assert.That(candidate.Route.Any(coordinate =>
                coordinate.Equals(new HexCoordinate(1, 0))), Is.False);

            var completed = fixture.Authority.TryAnswerMovementChoice(
                MovementChoiceCommand.SelectDestination(
                    choice.ResolutionId,
                    choice.ChoiceId,
                    destination));

            Assert.That(completed.Accepted, Is.True);
            Assert.That(fixture.Board.Occupancies.Single(occupancy =>
                occupancy.HeroId.Equals(fixture.Red.OwnerId)).Coordinate, Is.EqualTo(destination));
            Assert.That(completed.Events.Count(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.MovementActionProgressed &&
                ruleEvent.CardActionEvent.Kind == CardActionResolutionEventKind.MovementStepCommitted),
                Is.EqualTo(3));
            Assert.That(fixture.Red.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(fixture.Authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.Blue.Id));

            var cardResolvedIndex = completed.Events.ToList().FindIndex(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.CardResolved);
            var flipIndex = completed.Events.ToList().FindIndex(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.InitiativeProgressed &&
                ruleEvent.InitiativeEvent.Kind == InitiativeResolutionEventKind.TieBreakerFlipped);
            var nextActorIndex = completed.Events.ToList().FindIndex(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.InitiativeProgressed &&
                ruleEvent.InitiativeEvent.Kind == InitiativeResolutionEventKind.NextActorSelected);
            Assert.That(flipIndex, Is.GreaterThan(cardResolvedIndex));
            Assert.That(nextActorIndex, Is.GreaterThan(flipIndex));
        }

        [Test]
        public void InvalidMovementCommands_DoNotMutateOccupancyCardOrPendingChoice()
        {
            var fixture = CreateMovementFixture(7, 4, 2);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);
            var redOffer = ActionOfferQuery.GetAvailableTurnActionOffers(
                fixture.Definitions,
                fixture.Instances,
                fixture.Red.OwnerId,
                fixture.Red.Id).Single();
            var blueOffer = ActionOfferQuery.GetAvailableTurnActionOffers(
                fixture.Definitions,
                fixture.Instances,
                fixture.Blue.OwnerId,
                fixture.Blue.Id).Single();
            AssertRejected(
                fixture.Authority.TryStartMovement(new ActionResolutionId("wrong-actor"), blueOffer),
                TurnResolutionRejectionReason.CardNotSelectedForAction);
            Assert.That(fixture.Authority.TryStartMovement(
                new ActionResolutionId("valid-actor"), redOffer).Accepted, Is.True);

            var originalChoice = fixture.Authority.PendingMovementChoice;
            var originalOccupancy = fixture.Board.Occupancies.Single(occupancy =>
                occupancy.HeroId.Equals(fixture.Red.OwnerId));
            AssertRejected(
                fixture.Authority.TryAnswerMovementChoice(MovementChoiceCommand.Stop(
                    originalChoice.ResolutionId,
                    new ActionResolutionStepId("stale-choice"))),
                TurnResolutionRejectionReason.MovementCommandRejected);

            Assert.That(fixture.Board.Occupancies.Single(occupancy =>
                occupancy.HeroId.Equals(fixture.Red.OwnerId)), Is.EqualTo(originalOccupancy));
            Assert.That(fixture.Red.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            Assert.That(fixture.Authority.PendingMovementChoice, Is.SameAs(originalChoice));
            Assert.That(fixture.Authority.TryAnswerMovementChoice(MovementChoiceCommand.Stop(
                originalChoice.ResolutionId,
                originalChoice.ChoiceId)).Accepted, Is.True);
            Assert.That(fixture.Red.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void SelectedActor_AttackPausesForDefenseThenResolvesOnlyItsCardAndYields()
        {
            var fixture = CreateAttackFixture(false);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);
            var attackOffer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    fixture.Configuration.CardDefinitions,
                    fixture.Configuration.CardInstances,
                    fixture.RedPlayed.OwnerId,
                    fixture.RedPlayed.Id)
                .Single(offer => offer.ActionDefinitionId.Equals(
                    new ActionDefinitionId("primary-attack")));

            var started = fixture.Authority.TryStartAttack(
                new ActionResolutionId("turn-red-attack"),
                attackOffer);

            Assert.That(started.Accepted, Is.True);
            Assert.That(fixture.Authority.PendingAttackTargetChoice, Is.Not.Null);
            Assert.That(fixture.RedPlayed.State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Unresolved));
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.RedPlayed.Id));

            var targetChoice = fixture.Authority.PendingAttackTargetChoice;
            var targeted = fixture.Authority.TryAnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    targetChoice.ResolutionId,
                    targetChoice.ChoiceId,
                    fixture.BluePlayed.OwnerId));

            Assert.That(targeted.Accepted, Is.True);
            Assert.That(fixture.Authority.PendingDefenseChoice.ChooserId,
                Is.EqualTo(fixture.BluePlayed.OwnerId));
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.RedPlayed.Id),
                "The nested Defense frame must not replace the outer turn actor.");
            AssertRejected(
                fixture.Authority.TryPerformHold(fixture.RedPlayed.Id),
                TurnResolutionRejectionReason.ActionAlreadyPending);

            var defenseChoice = fixture.Authority.PendingDefenseChoice;
            var defenseCardId = defenseChoice.LegalDefenseOffers
                .Single(offer => offer.SourceCardInstanceId.Equals(fixture.BlueDefense.Id))
                .SourceCardInstanceId;
            var completed = fixture.Authority.TryAnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    defenseChoice.ResolutionId,
                    defenseChoice.ChoiceId,
                    defenseCardId));

            Assert.That(completed.Accepted, Is.True);
            Assert.That(fixture.BlueDefense.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(fixture.BlueDefense.State.HasBeenPlayed, Is.False);
            Assert.That(fixture.RedPlayed.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(fixture.BluePlayed.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            Assert.That(fixture.Authority.PendingAttackResolution, Is.Null);
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.BluePlayed.Id));

            var kinds = completed.Events.Select(ruleEvent => ruleEvent.Kind).ToArray();
            Assert.That(System.Array.IndexOf(kinds, TurnResolutionEventKind.CardResolved),
                Is.GreaterThan(System.Array.LastIndexOf(kinds, TurnResolutionEventKind.AttackActionProgressed)));
            Assert.That(completed.Events.Last(ruleEvent =>
                    ruleEvent.Kind == TurnResolutionEventKind.InitiativeProgressed)
                .InitiativeEvent.Kind, Is.EqualTo(InitiativeResolutionEventKind.NextActorSelected));

            Assert.That(fixture.Authority.TryPerformHold(fixture.BluePlayed.Id).Accepted, Is.True,
                "The defender's reaction must not consume its selected card's normal action.");
        }

        [Test]
        public void AttackDefenseReaction_RejectsStaleCommandsAndRequeriesAfterDeferredFlip()
        {
            var fixture = CreateAttackFixture(true);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.BluePlayed.Id));
            var attackOffer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    fixture.Configuration.CardDefinitions,
                    fixture.Configuration.CardInstances,
                    fixture.BluePlayed.OwnerId,
                    fixture.BluePlayed.Id)
                .Single(offer => offer.ActionDefinitionId.Equals(
                    new ActionDefinitionId("primary-attack")));
            Assert.That(fixture.Authority.TryStartAttack(
                new ActionResolutionId("turn-tied-attack"),
                attackOffer).Accepted, Is.True);

            var targetChoice = fixture.Authority.PendingAttackTargetChoice;
            AssertRejected(
                fixture.Authority.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                    targetChoice.ResolutionId,
                    new ActionResolutionStepId("stale-target"),
                    fixture.RedPlayed.OwnerId)),
                TurnResolutionRejectionReason.AttackCommandRejected);
            Assert.That(fixture.Authority.PendingAttackTargetChoice, Is.SameAs(targetChoice));
            Assert.That(fixture.BluePlayed.State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Unresolved));

            Assert.That(fixture.Authority.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                targetChoice.ResolutionId,
                targetChoice.ChoiceId,
                fixture.RedPlayed.OwnerId)).Accepted, Is.True);
            var defenseChoice = fixture.Authority.PendingDefenseChoice;
            AssertRejected(
                fixture.Authority.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.Decline(
                    defenseChoice.ResolutionId,
                    new ActionResolutionStepId("stale-defense"))),
                TurnResolutionRejectionReason.AttackCommandRejected);
            Assert.That(fixture.Authority.PendingDefenseChoice, Is.SameAs(defenseChoice));
            Assert.That(fixture.Authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));

            var completed = fixture.Authority.TryAnswerDefenseResponse(
                DefenseResponseChoiceCommand.Decline(
                    defenseChoice.ResolutionId,
                    defenseChoice.ChoiceId));

            Assert.That(completed.Accepted, Is.True);
            Assert.That(fixture.Authority.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Red));
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.RedPlayed.Id));
            var cardResolvedIndex = completed.Events.ToList().FindIndex(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.CardResolved);
            var flipIndex = completed.Events.ToList().FindIndex(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.InitiativeProgressed &&
                ruleEvent.InitiativeEvent.Kind == InitiativeResolutionEventKind.TieBreakerFlipped);
            var nextActorIndex = completed.Events.ToList().FindIndex(ruleEvent =>
                ruleEvent.Kind == TurnResolutionEventKind.InitiativeProgressed &&
                ruleEvent.InitiativeEvent.Kind == InitiativeResolutionEventKind.NextActorSelected);
            Assert.That(flipIndex, Is.GreaterThan(cardResolvedIndex));
            Assert.That(nextActorIndex, Is.GreaterThan(flipIndex));
        }

        [Test]
        public void SelectedActor_RangedAttackUsesItsSupportedPrimaryAndAdvances()
        {
            var fixture = CreateAttackFixture(false, true);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);
            var rangedOffer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    fixture.Configuration.CardDefinitions,
                    fixture.Configuration.CardInstances,
                    fixture.RedPlayed.OwnerId,
                    fixture.RedPlayed.Id)
                .Single(offer => offer.ActionDefinitionId.Equals(
                    new ActionDefinitionId("primary-ranged-attack")));

            Assert.That(fixture.Authority.TryStartAttack(
                new ActionResolutionId("turn-red-ranged-attack"),
                rangedOffer).Accepted, Is.True);
            var target = fixture.Authority.PendingAttackTargetChoice;
            Assert.That(fixture.Authority.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                target.ResolutionId,
                target.ChoiceId,
                fixture.BluePlayed.OwnerId)).Accepted, Is.True);
            var defense = fixture.Authority.PendingDefenseChoice;
            Assert.That(fixture.Authority.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.Decline(
                defense.ResolutionId,
                defense.ChoiceId)).Accepted, Is.True);

            Assert.That(fixture.RedPlayed.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(fixture.BluePlayed.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.BluePlayed.Id));
        }

        [Test]
        public void HeroAttackDefense_VisitsVerifiedGeneralCheckpointsInExactOrder()
        {
            var timing = new ActiveEffectLifecycle();
            var checkpoints = new[]
            {
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.BeforeAttack,
                TimingCheckpoint.DefenseResponse,
                TimingCheckpoint.AttackResultComparison,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.ActionEnded
            };
            for (var index = 0; index < checkpoints.Length; index++)
            {
                var checkpoint = checkpoints[index];
                var definition = new ActiveEffectDefinition(
                    $"attack-order-{index}",
                    ActiveEffectAffectedScopeKind.Global,
                    ActiveEffectDurationKind.ThisTurn,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                    new[]
                    {
                        ActiveEffectCancellationCondition.SourceCardStateChanged,
                        ActiveEffectCancellationCondition.AffectedHeroDefeated,
                        ActiveEffectCancellationCondition.CardsRetrieved,
                        ActiveEffectCancellationCondition.Explicit
                    },
                    false,
                    "SyntheticAttackOrderProbe",
                    $"attack-order-{index}",
                    new[] { checkpoint });
                timing.Create(
                    new ActiveEffectId($"attack-order-effect-{index}"),
                    definition,
                    new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue),
                    TeamSide.Red,
                    default,
                    new CardInstanceId($"attack-order-source-{index}"),
                    new ActionDefinitionId($"attack-order-action-{index}"),
                    1,
                    1);
            }

            var fixture = CreateAttackFixture(false, timing: timing);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);
            var attackOffer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    fixture.Configuration.CardDefinitions,
                    fixture.Configuration.CardInstances,
                    fixture.RedPlayed.OwnerId,
                    fixture.RedPlayed.Id)
                .Single(offer => offer.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
            Assert.That(fixture.Authority.TryStartAttack(
                new ActionResolutionId("complete-hero-attack-order"), attackOffer).Accepted, Is.True);
            var targetChoice = fixture.Authority.PendingAttackTargetChoice;
            var targeted = fixture.Authority.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                targetChoice.ResolutionId,
                targetChoice.ChoiceId,
                fixture.BluePlayed.OwnerId));
            var defenseChoice = fixture.Authority.PendingDefenseChoice;
            var completed = fixture.Authority.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.Decline(
                defenseChoice.ResolutionId,
                defenseChoice.ChoiceId));

            var events = targeted.Events.Concat(completed.Events).ToArray();
            var observedCheckpoints = events
                .Where(value => value.Kind == TurnResolutionEventKind.TimingProgressed &&
                    value.ActiveEffectEvent.Kind == ActiveEffectLifecycleEventKind.Triggered)
                .Select(value => value.ActiveEffectEvent.Checkpoint)
                .ToArray();
            Assert.That(observedCheckpoints, Is.EqualTo(checkpoints));

            var targetValidated = Array.FindIndex(events, value =>
                value.Kind == TurnResolutionEventKind.AttackActionProgressed &&
                value.CardActionEvent.Kind == CardActionResolutionEventKind.AttackTargetValidated);
            var beforeAttack = Array.FindIndex(events, value =>
                value.Kind == TurnResolutionEventKind.TimingProgressed &&
                value.ActiveEffectEvent.Checkpoint == TimingCheckpoint.BeforeAttack);
            var attackTotal = Array.FindIndex(events, value =>
                value.Kind == TurnResolutionEventKind.AttackActionProgressed &&
                value.CardActionEvent.Kind == CardActionResolutionEventKind.AttackTotalCalculated);
            var defenseResponse = Array.FindIndex(events, value =>
                value.Kind == TurnResolutionEventKind.TimingProgressed &&
                value.ActiveEffectEvent.Checkpoint == TimingCheckpoint.DefenseResponse);
            var comparison = Array.FindIndex(events, value =>
                value.Kind == TurnResolutionEventKind.TimingProgressed &&
                value.ActiveEffectEvent.Checkpoint == TimingCheckpoint.AttackResultComparison);
            var afterAttack = Array.FindIndex(events, value =>
                value.Kind == TurnResolutionEventKind.TimingProgressed &&
                value.ActiveEffectEvent.Checkpoint == TimingCheckpoint.AfterAttack);
            var actionEnded = Array.FindIndex(events, value =>
                value.Kind == TurnResolutionEventKind.TimingProgressed &&
                value.ActiveEffectEvent.Checkpoint == TimingCheckpoint.ActionEnded);
            var order = new[]
            {
                targetValidated, beforeAttack, attackTotal, defenseResponse, comparison, afterAttack, actionEnded
            };
            Assert.That(order, Is.All.GreaterThanOrEqualTo(0));
            Assert.That(order, Is.Ordered);
        }

        [Test]
        public void SelectedActor_MinionAttackResolvesCardWithoutDefenseAndAdvances()
        {
            var fixture = CreateAttackFixture(false, minionTarget: true);
            Assert.That(fixture.Authority.TryStart().Accepted, Is.True);
            var attackOffer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    fixture.Configuration.CardDefinitions,
                    fixture.Configuration.CardInstances,
                    fixture.RedPlayed.OwnerId,
                    fixture.RedPlayed.Id)
                .Single(offer => offer.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
            Assert.That(fixture.Authority.TryStartAttack(
                new ActionResolutionId("turn-minion-attack"), attackOffer).Accepted, Is.True);
            var target = fixture.Authority.PendingAttackTargetChoice;

            var completed = fixture.Authority.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                target.ResolutionId, target.ChoiceId, new MinionId("blue-turn-minion")));

            Assert.That(completed.Accepted, Is.True);
            Assert.That(fixture.Authority.PendingDefenseChoice, Is.Null);
            Assert.That(fixture.RedPlayed.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(fixture.BluePlayed.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            Assert.That(fixture.Authority.SelectedActor.CardInstanceId, Is.EqualTo(fixture.BluePlayed.Id));
            Assert.That(completed.Events.Where(value => value.Kind == TurnResolutionEventKind.AttackActionProgressed)
                .Select(value => value.CardActionEvent.Kind), Does.Contain(CardActionResolutionEventKind.MinionDefeatRecorded));
        }

        /// <summary>
        /// Milestone 6.2c closing check for finding F62B-02, start side: a refused Skill start
        /// reports Skill-specific typed reasons instead of borrowing ActionAlreadyPending for
        /// every failure shape.
        /// </summary>
        [Test]
        public void TryStartSkill_RefusalsReportSkillSpecificTypedReasons()
        {
            var withoutDispatcher = CreateSkillTurnFixture(false);
            AssertRejected(
                withoutDispatcher.Authority.TryStartSkill(
                    new ActionResolutionId("skill-no-dispatcher"),
                    SkillOffer(withoutDispatcher)),
                TurnResolutionRejectionReason.SkillAuthorityUnavailable);

            var fixture = CreateSkillTurnFixture();
            var forged = new ActionOffer(
                fixture.RedFocus.Id,
                fixture.RedFocus.DefinitionReference,
                new ActionDefinitionId("no-such-skill"),
                fixture.RedFocus.OwnerId,
                ActionInvocationContext.PrintedPrimary,
                true);
            AssertRejected(
                fixture.Authority.TryStartSkill(new ActionResolutionId("skill-forged"), forged),
                TurnResolutionRejectionReason.SkillActionRejected);
            Assert.That(fixture.Authority.PendingSkillResolution, Is.Null);
        }

        /// <summary>
        /// Milestone 6.2c closing check for finding F62B-02, answer side: a refused effect-choice
        /// answer reports a Skill-specific typed reason rather than AttackCommandRejected.
        /// </summary>
        [Test]
        public void TryAnswerEffectChoice_RefusalsReportSkillSpecificTypedReasons()
        {
            var fixture = CreateSkillTurnFixture();
            AssertRejected(
                fixture.Authority.TryAnswerEffectChoice(new EffectChoiceCommand(
                    new EffectResolutionId("no-pending-skill"),
                    new EffectChoiceId("no-pending-choice"),
                    EffectChoiceCommandKind.Confirm)),
                TurnResolutionRejectionReason.SkillCommandRejected);

            Assert.That(fixture.Authority.TryStartSkill(
                new ActionResolutionId("skill-stale-answer"),
                SkillOffer(fixture)).Accepted, Is.True);
            var choice = fixture.Authority.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            AssertRejected(
                fixture.Authority.TryAnswerEffectChoice(new EffectChoiceCommand(
                    choice.ResolutionId,
                    new EffectChoiceId("stale-effect-choice"),
                    EffectChoiceCommandKind.Confirm)),
                TurnResolutionRejectionReason.SkillCommandRejected);
            Assert.That(fixture.Authority.PendingEffectChoice, Is.SameAs(choice));
        }

        /// <summary>
        /// Milestone 6.2c closing check for finding F62B-02, event side: accepted Skill facts
        /// materialize under the Skill-specific turn event kind rather than
        /// MovementActionProgressed.
        /// </summary>
        [Test]
        public void AcceptedSkillFacts_MaterializeUnderTheSkillTurnEventKind()
        {
            var fixture = CreateSkillTurnFixture();

            var started = fixture.Authority.TryStartSkill(
                new ActionResolutionId("skill-event-kind"), SkillOffer(fixture));

            Assert.That(started.Accepted, Is.True);
            var cardActionKinds = started.Events
                .Where(ruleEvent => ruleEvent.CardActionEvent.Kind != CardActionResolutionEventKind.None)
                .Select(ruleEvent => ruleEvent.Kind)
                .ToArray();
            Assert.That(cardActionKinds, Is.Not.Empty);
            Assert.That(cardActionKinds, Is.All.EqualTo(TurnResolutionEventKind.SkillActionProgressed),
                "Skill facts must not materialize under another action family's event kind.");
        }

        /// <summary>
        /// Milestone 6.2c closing check for the answer half of finding F62B-03: the effect-answer
        /// path prechecks source-card state the way Movement's answer path does, so a bad state is
        /// a typed rejection rather than a completion-time fault.
        /// </summary>
        [Test]
        public void TryAnswerEffectChoice_BadSourceCardState_RejectsInsteadOfFaulting()
        {
            var fixture = CreateSkillTurnFixture();
            Assert.That(fixture.Authority.TryStartSkill(
                new ActionResolutionId("skill-bad-card-state"),
                SkillOffer(fixture)).Accepted, Is.True);
            var choice = fixture.Authority.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(fixture.RedFocus), Is.True,
                "The arrangement resolves the source card out from under the paused Skill.");

            AssertRejected(
                fixture.Authority.TryAnswerEffectChoice(choice.DeclineInvocation),
                TurnResolutionRejectionReason.CardResolutionFailed);
            Assert.That(fixture.Authority.PendingEffectChoice, Is.SameAs(choice));
        }

        private static IEnumerable<InitiativeResolutionEventKind> EventKinds(
            TurnResolutionResult result)
        {
            return result.Events
                .Where(ruleEvent => ruleEvent.Kind == TurnResolutionEventKind.InitiativeProgressed)
                .Select(ruleEvent => ruleEvent.InitiativeEvent.Kind);
        }

        private static TestFixture CreateFixture(
            int redInitiative,
            int blueInitiative,
            TeamSide initialTieBreakerSide)
        {
            var participants = CreateParticipants(1, 1);
            var redDefinition = CreateDefinition("red", redInitiative);
            var blueDefinition = CreateDefinition("blue", blueInitiative);
            var red = CreateRevealedCard("red-card", redDefinition, "red-hero-1");
            var blue = CreateRevealedCard("blue-card", blueDefinition, "blue-hero-1");
            return new TestFixture(
                red,
                blue,
                new TurnResolutionAuthority(
                    new[] { redDefinition, blueDefinition },
                    new[] { blue, red },
                    participants,
                    new TieBreakerState(initialTieBreakerSide)));
        }

        private static MovementTestFixture CreateMovementFixture(
            int redInitiative,
            int blueInitiative,
            int movementAllowance,
            bool withObstacleDetour = false)
        {
            var participants = CreateParticipants(1, 1);
            var redDefinition = CreateMovementDefinition("red-move", redInitiative, movementAllowance);
            var blueDefinition = CreateMovementDefinition("blue-move", blueInitiative, 1);
            var red = CreateRevealedCard("red-move-card", redDefinition, "red-hero-1");
            var blue = CreateRevealedCard("blue-move-card", blueDefinition, "blue-hero-1");
            var coordinates = withObstacleDetour
                ? new[]
                {
                    new HexCoordinate(-1, 0),
                    new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0),
                    new HexCoordinate(0, 1),
                    new HexCoordinate(1, 1),
                    new HexCoordinate(2, 0)
                }
                : new[]
                {
                    new HexCoordinate(-1, 0),
                    new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0),
                    new HexCoordinate(2, 0)
                };
            var board = new AdjacentMoveAuthority(
                coordinates,
                new[]
                {
                    new HeroOccupancy(red.OwnerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(blue.OwnerId, new HexCoordinate(-1, 0))
                },
                withObstacleDetour
                    ? new[] { new ObstacleOccupancy(new ObstacleId("turn-blocker"), new HexCoordinate(1, 0)) }
                    : System.Array.Empty<ObstacleOccupancy>());
            var definitions = new[] { redDefinition, blueDefinition };
            var instances = new[] { blue, red };
            return new MovementTestFixture(
                red,
                blue,
                definitions,
                instances,
                board,
                new TurnResolutionAuthority(
                    definitions,
                    instances,
                    participants,
                    new TieBreakerState(TeamSide.Red),
                    new CardActionResolutionAuthority(board)));
        }

        private static AttackTestFixture CreateAttackFixture(
            bool tiedInitiative,
            bool rangedAttack = false,
            bool minionTarget = false,
            ActiveEffectLifecycle timing = null)
        {
            // Red Bulwark and Blue Sentinel are the hands' one cross-hero Initiative tie, at 11. Since
            // milestone 6.8a replaced Red Bulwark's printed Attack with its showcase Skill, only the
            // Blue card of that pair still supplies a printed Attack, so the tied fixture reveals Blue
            // and the Blue hero acts first. The tie, the deferred flip, and the Attack all survive;
            // only which hero performs the Attack moved.
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(
                tiedInitiative ? TeamSide.Blue : TeamSide.Red);
            var redPlayed = configuration.RedHand.Instances[rangedAttack ? 2 : tiedInitiative ? 1 : 0];
            var bluePlayed = configuration.BlueHand.Instances[rangedAttack ? 4 : 1];
            var blueDefense = configuration.BlueHand.Instances[rangedAttack ? 0 : 2];
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(redPlayed), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(redPlayed), Is.True);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(bluePlayed), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(bluePlayed), Is.True);
            var minions = minionTarget
                ? new[]
                {
                    new QuickGameMinionSetupState(
                        new MinionId("blue-turn-minion"),
                        configuration.Participants.GetTeamForHero(bluePlayed.OwnerId).Id,
                        TeamSide.Blue,
                        StandardMinionType.Melee,
                        new ConfiguredSpawnPointReference(new MapId("synthetic-coordinate-board"), new SpawnPointId("blue-turn-spawn")),
                        new ConfiguredSpaceReference(new MapId("synthetic-coordinate-board"), new SpaceId("q1_r0")),
                        new HexCoordinate(1, 0))
                }
                : Array.Empty<QuickGameMinionSetupState>();
            var board = new AdjacentMoveAuthority(
                new[]
                {
                    new HexCoordinate(-1, 0),
                    new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0),
                    new HexCoordinate(2, 0)
                },
                new[]
                {
                    new HeroOccupancy(redPlayed.OwnerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(bluePlayed.OwnerId, new HexCoordinate(minionTarget ? 2 : 1, 0))
                },
                Array.Empty<ObstacleOccupancy>(),
                minions);
            return new AttackTestFixture(
                configuration,
                redPlayed,
                bluePlayed,
                blueDefense,
                new TurnResolutionAuthority(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    configuration.TieBreakerState,
                    new CardActionResolutionAuthority(board),
                    timing,
                    1,
                    1));
        }

        /// <summary>
        /// The 6.2a Skill cards played and revealed under a real turn authority. Red's Focus card
        /// (Initiative 14) wins the turn against Blue's (3), so the Skill under test is the
        /// selected actor's without steering Initiative. The cache cards stay in hand and never
        /// compete.
        /// </summary>
        private static SkillTurnTestFixture CreateSkillTurnFixture(bool withCardActions = true)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Red);
            var redFocus = configuration.CardInstances.Single(card => card.Id.Value.Contains("red-focus"));
            var blueFocus = configuration.CardInstances.Single(card => card.Id.Value.Contains("blue-focus"));
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(redFocus), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(redFocus), Is.True);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(blueFocus), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(blueFocus), Is.True);
            var board = new AdjacentMoveAuthority(
                new[]
                {
                    new HexCoordinate(-1, 0),
                    new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0),
                    new HexCoordinate(2, 0)
                },
                new[]
                {
                    new HeroOccupancy(redFocus.OwnerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(blueFocus.OwnerId, new HexCoordinate(1, 0))
                });
            var authority = withCardActions
                ? new TurnResolutionAuthority(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    configuration.TieBreakerState,
                    new CardActionResolutionAuthority(board),
                    new ActiveEffectLifecycle(),
                    1,
                    1)
                : new TurnResolutionAuthority(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    configuration.TieBreakerState);
            Assert.That(authority.TryStart().Accepted, Is.True);
            Assert.That(authority.SelectedActor.CardInstanceId, Is.EqualTo(redFocus.Id));
            return new SkillTurnTestFixture(configuration, redFocus, authority);
        }

        private static ActionOffer SkillOffer(SkillTurnTestFixture fixture)
        {
            return ActionOfferQuery.GetAvailableTurnActionOffers(
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.RedFocus.OwnerId,
                fixture.RedFocus.Id).Single();
        }

        private static CardDefinition CreateMovementDefinition(
            string id,
            int initiative,
            int movementAllowance)
        {
            return new CardDefinition(
                new CardDefinitionId(id),
                "turn-movement-test-rev-1",
                $"TEST - {id}",
                new InitiativeValue(initiative),
                CardContentAuthority.SyntheticTestFixture,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-movement"),
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, movementAllowance) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true))
                });
        }

        private static CardDefinition CreateDefinition(string id, int initiative)
        {
            return new CardDefinition(
                new CardDefinitionId(id),
                "turn-test-rev-1",
                $"TEST - {id}",
                new InitiativeValue(initiative),
                CardContentAuthority.SyntheticTestFixture);
        }

        private static CardInstance CreateRevealedCard(
            string cardId,
            CardDefinition definition,
            string ownerId)
        {
            var card = CardInstance.CreateInHand(
                new CardInstanceId(cardId),
                definition.Reference,
                new HeroId(ownerId));
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
            return card;
        }

        private static MatchParticipants CreateParticipants(int redCount, int blueCount)
        {
            var redTeamId = new TeamId("red-team");
            var blueTeamId = new TeamId("blue-team");
            var teams = new List<Team>();
            var players = new List<Player>();
            var heroes = new List<Hero>();
            if (redCount > 0)
            {
                teams.Add(new Team(redTeamId, TeamSide.Red));
            }

            if (blueCount > 0)
            {
                teams.Add(new Team(blueTeamId, TeamSide.Blue));
            }

            AddSide(redCount, "red", redTeamId, players, heroes);
            AddSide(blueCount, "blue", blueTeamId, players, heroes);
            return new MatchParticipants(teams, players, heroes);
        }

        private static void AddSide(
            int count,
            string prefix,
            TeamId teamId,
            ICollection<Player> players,
            ICollection<Hero> heroes)
        {
            for (var index = 1; index <= count; index++)
            {
                var playerId = new PlayerId($"{prefix}-player-{index}");
                players.Add(new Player(playerId, teamId));
                heroes.Add(new Hero(new HeroId($"{prefix}-hero-{index}"), playerId));
            }
        }

        private static void AssertRejected(
            TurnResolutionResult result,
            TurnResolutionRejectionReason reason)
        {
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(reason));
            Assert.That(result.Events, Is.Empty);
        }

        private sealed class SkillTurnTestFixture
        {
            public SkillTurnTestFixture(
                PlaceholderOneVsOneTestConfiguration configuration,
                CardInstance redFocus,
                TurnResolutionAuthority authority)
            {
                Configuration = configuration;
                RedFocus = redFocus;
                Authority = authority;
            }

            public PlaceholderOneVsOneTestConfiguration Configuration { get; }

            public CardInstance RedFocus { get; }

            public TurnResolutionAuthority Authority { get; }
        }

        private sealed class TestFixture
        {
            public TestFixture(
                CardInstance red,
                CardInstance blue,
                TurnResolutionAuthority authority)
            {
                Red = red;
                Blue = blue;
                Authority = authority;
            }

            public CardInstance Red { get; }

            public CardInstance Blue { get; }

            public TurnResolutionAuthority Authority { get; }
        }

        private sealed class MovementTestFixture
        {
            public MovementTestFixture(
                CardInstance red,
                CardInstance blue,
                IReadOnlyList<CardDefinition> definitions,
                IReadOnlyList<CardInstance> instances,
                AdjacentMoveAuthority board,
                TurnResolutionAuthority authority)
            {
                Red = red;
                Blue = blue;
                Definitions = definitions;
                Instances = instances;
                Board = board;
                Authority = authority;
            }

            public CardInstance Red { get; }

            public CardInstance Blue { get; }

            public IReadOnlyList<CardDefinition> Definitions { get; }

            public IReadOnlyList<CardInstance> Instances { get; }

            public AdjacentMoveAuthority Board { get; }

            public TurnResolutionAuthority Authority { get; }
        }

        private sealed class AttackTestFixture
        {
            public AttackTestFixture(
                PlaceholderOneVsOneTestConfiguration configuration,
                CardInstance redPlayed,
                CardInstance bluePlayed,
                CardInstance blueDefense,
                TurnResolutionAuthority authority)
            {
                Configuration = configuration;
                RedPlayed = redPlayed;
                BluePlayed = bluePlayed;
                BlueDefense = blueDefense;
                Authority = authority;
            }

            public PlaceholderOneVsOneTestConfiguration Configuration { get; }

            public CardInstance RedPlayed { get; }

            public CardInstance BluePlayed { get; }

            public CardInstance BlueDefense { get; }

            public TurnResolutionAuthority Authority { get; }
        }
    }
}
