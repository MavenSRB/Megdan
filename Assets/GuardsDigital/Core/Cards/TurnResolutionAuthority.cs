using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    public enum TurnResolutionRejectionReason
    {
        None = 0,
        TurnAlreadyStarted = 1,
        TurnNotStarted = 2,
        TurnAlreadyEnded = 3,
        NoActorSelected = 4,
        CardNotSelectedForAction = 5,
        HoldNotAvailable = 6,
        CardResolutionFailed = 7,
        InitiativeCommandRejected = 8,
        ActionAlreadyPending = 9,
        MovementAuthorityUnavailable = 10,
        MovementActionRejected = 11,
        MovementCommandRejected = 12,
        AttackAuthorityUnavailable = 13,
        AttackActionRejected = 14,
        AttackCommandRejected = 15
    }

    public enum TurnResolutionEventKind
    {
        None = 0,
        InitiativeProgressed = 1,
        HoldActionPerformed = 2,
        CardResolved = 3,
        TurnOnlyEffectsCancellationCheckpoint = 4,
        TurnEnded = 5,
        MovementActionProgressed = 6,
        AttackActionProgressed = 7
    }

    public readonly struct TurnResolutionEvent
    {
        private TurnResolutionEvent(
            TurnResolutionEventKind kind,
            InitiativeResolutionEvent initiativeEvent,
            CardInstanceId cardInstanceId,
            HeroId actorId,
            ActionDefinitionId actionDefinitionId,
            ActionInvocationContext invocationContext,
            CardActionResolutionEvent cardActionEvent)
        {
            Kind = kind;
            InitiativeEvent = initiativeEvent;
            CardInstanceId = cardInstanceId;
            ActorId = actorId;
            ActionDefinitionId = actionDefinitionId;
            InvocationContext = invocationContext;
            CardActionEvent = cardActionEvent;
        }

        public TurnResolutionEventKind Kind { get; }

        public InitiativeResolutionEvent InitiativeEvent { get; }

        public CardInstanceId CardInstanceId { get; }

        public HeroId ActorId { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        public ActionInvocationContext InvocationContext { get; }

        public CardActionResolutionEvent CardActionEvent { get; }

        public static TurnResolutionEvent FromInitiative(InitiativeResolutionEvent initiativeEvent)
        {
            if (initiativeEvent.Kind == InitiativeResolutionEventKind.None)
            {
                throw new ArgumentException("A turn Initiative event requires a defined Initiative event.", nameof(initiativeEvent));
            }

            return new TurnResolutionEvent(
                TurnResolutionEventKind.InitiativeProgressed,
                initiativeEvent,
                default,
                default,
                default,
                ActionInvocationContext.None,
                default);
        }

        public static TurnResolutionEvent HoldPerformed(ActionOffer offer)
        {
            if (!offer.SourceCardInstanceId.IsDefined ||
                !offer.ActorId.IsDefined ||
                !offer.ActionDefinitionId.IsDefined ||
                offer.InvocationContext == ActionInvocationContext.None ||
                !offer.IsAvailable)
            {
                throw new ArgumentException("A performed Hold event requires a complete available offer.", nameof(offer));
            }

            return new TurnResolutionEvent(
                TurnResolutionEventKind.HoldActionPerformed,
                default,
                offer.SourceCardInstanceId,
                offer.ActorId,
                offer.ActionDefinitionId,
                offer.InvocationContext,
                default);
        }

        public static TurnResolutionEvent CardWasResolved(
            CardInstanceId cardInstanceId,
            HeroId actorId)
        {
            if (!cardInstanceId.IsDefined || !actorId.IsDefined)
            {
                throw new ArgumentException("A resolved-card event requires defined card and actor IDs.");
            }

            return new TurnResolutionEvent(
                TurnResolutionEventKind.CardResolved,
                default,
                cardInstanceId,
                actorId,
                default,
                ActionInvocationContext.None,
                default);
        }

        public static TurnResolutionEvent FromCardAction(CardActionResolutionEvent cardActionEvent)
        {
            return FromCardAction(cardActionEvent, TurnResolutionEventKind.MovementActionProgressed);
        }

        public static TurnResolutionEvent FromAttackAction(CardActionResolutionEvent cardActionEvent)
        {
            return FromCardAction(cardActionEvent, TurnResolutionEventKind.AttackActionProgressed);
        }

        private static TurnResolutionEvent FromCardAction(
            CardActionResolutionEvent cardActionEvent,
            TurnResolutionEventKind kind)
        {
            if (cardActionEvent.Kind == CardActionResolutionEventKind.None)
            {
                throw new ArgumentException(
                    "A turn card-action event requires a defined card-action event.",
                    nameof(cardActionEvent));
            }

            return new TurnResolutionEvent(
                kind,
                default,
                cardActionEvent.SourceCardInstanceId,
                cardActionEvent.ActorId,
                cardActionEvent.ActionDefinitionId,
                ActionInvocationContext.None,
                cardActionEvent);
        }

        public static TurnResolutionEvent TurnCheckpoint(TurnResolutionEventKind kind)
        {
            if (kind != TurnResolutionEventKind.TurnOnlyEffectsCancellationCheckpoint &&
                kind != TurnResolutionEventKind.TurnEnded)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return new TurnResolutionEvent(
                kind,
                default,
                default,
                default,
                default,
                ActionInvocationContext.None,
                default);
        }
    }

    public readonly struct TurnResolutionResult
    {
        private TurnResolutionResult(
            bool accepted,
            TurnResolutionRejectionReason rejectionReason,
            IEnumerable<TurnResolutionEvent> events)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Events = Array.AsReadOnly((events ?? throw new ArgumentNullException(nameof(events))).ToArray());
        }

        public bool Accepted { get; }

        public TurnResolutionRejectionReason RejectionReason { get; }

        public IReadOnlyList<TurnResolutionEvent> Events { get; }

        public static TurnResolutionResult Accept(IEnumerable<TurnResolutionEvent> events)
        {
            return new TurnResolutionResult(true, TurnResolutionRejectionReason.None, events);
        }

        public static TurnResolutionResult Reject(TurnResolutionRejectionReason reason)
        {
            if (reason == TurnResolutionRejectionReason.None)
            {
                throw new ArgumentException("A rejected turn command requires a reason.", nameof(reason));
            }

            return new TurnResolutionResult(false, reason, Array.Empty<TurnResolutionEvent>());
        }
    }

    public sealed class TurnResolutionAuthority
    {
        private readonly IReadOnlyList<CardDefinition> definitions;
        private readonly IReadOnlyList<CardInstance> instances;
        private readonly MatchParticipants participants;
        private readonly InitiativeResolutionAuthority initiative;
        private readonly CardActionResolutionAuthority cardActions;

        public TurnResolutionAuthority(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            TieBreakerState initialTieBreakerState)
            : this(definitions, instances, participants, initialTieBreakerState, null)
        {
        }

        public TurnResolutionAuthority(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            TieBreakerState initialTieBreakerState,
            CardActionResolutionAuthority cardActions)
        {
            this.definitions = Array.AsReadOnly(
                ActionOfferQuery.BuildDefinitionLookup(definitions).Values.ToArray());
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            var cards = instances.ToArray();
            if (cards.Any(card => card == null) ||
                cards.Select(card => card.Id).Distinct().Count() != cards.Length)
            {
                throw new ArgumentException(
                    "Turn card instances must be non-null and have unique IDs.",
                    nameof(instances));
            }

            this.instances = Array.AsReadOnly(cards);
            this.participants = participants ?? throw new ArgumentNullException(nameof(participants));
            foreach (var card in cards)
            {
                if (!this.definitions.Any(definition => definition.Reference.Equals(card.DefinitionReference)))
                {
                    throw new ArgumentException(
                        "Every turn card must reference a current definition.",
                        nameof(instances));
                }

                participants.GetHero(card.OwnerId);
            }

            initiative = new InitiativeResolutionAuthority(initialTieBreakerState);
            this.cardActions = cardActions;
        }

        public bool IsStarted { get; private set; }

        public bool IsEnded { get; private set; }

        public TieBreakerState TieBreakerState => initiative.TieBreakerState;

        public PendingInitiativeChoice PendingChoice => initiative.PendingChoice;

        public SelectedInitiativeActor SelectedActor => initiative.SelectedActor;

        public PendingCardActionResolution PendingMovementResolution => cardActions?.PendingResolution;

        public PendingMovementChoice PendingMovementChoice => cardActions?.PendingChoice;

        public PendingAttackResolution PendingAttackResolution => cardActions?.PendingAttackResolution;

        public PendingAttackTargetChoice PendingAttackTargetChoice => cardActions?.PendingAttackTargetChoice;

        public PendingDefenseResponseChoice PendingDefenseChoice => cardActions?.PendingDefenseChoice;

        public TurnResolutionResult TryStart()
        {
            if (IsStarted)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.TurnAlreadyStarted);
            }

            IsStarted = true;
            return AdvanceInitiative(Array.Empty<TurnResolutionEvent>());
        }

        public TurnResolutionResult TryChooseTiedActor(
            TeamSide choosingSide,
            CardInstanceId cardInstanceId)
        {
            var commonRejection = GetCommonRejection();
            if (commonRejection != TurnResolutionRejectionReason.None)
            {
                return TurnResolutionResult.Reject(commonRejection);
            }

            var result = initiative.TryChooseTiedActor(
                choosingSide,
                cardInstanceId,
                definitions,
                instances,
                participants);
            return result.Accepted
                ? TurnResolutionResult.Accept(result.Events.Select(TurnResolutionEvent.FromInitiative))
                : TurnResolutionResult.Reject(TurnResolutionRejectionReason.InitiativeCommandRejected);
        }

        public TurnResolutionResult TryPerformHold(CardInstanceId cardInstanceId)
        {
            var commonRejection = GetCommonRejection();
            if (commonRejection != TurnResolutionRejectionReason.None)
            {
                return TurnResolutionResult.Reject(commonRejection);
            }

            if (!initiative.SelectedActor.IsDefined)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.NoActorSelected);
            }

            if (!initiative.SelectedActor.CardInstanceId.Equals(cardInstanceId))
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.CardNotSelectedForAction);
            }

            if (cardActions?.HasPendingResolution == true)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.ActionAlreadyPending);
            }

            var offer = GeneralRuleActionOfferQuery.GetAvailableHoldOffer(
                cardInstanceId,
                initiative.SelectedActor.ActorId,
                definitions,
                instances);
            if (!offer.HasValue)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.HoldNotAvailable);
            }

            var card = instances.Single(candidate => candidate.Id.Equals(cardInstanceId));
            if (!CardLifecycleAuthority.TryResolveAfterAction(card))
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.CardResolutionFailed);
            }

            var events = new List<TurnResolutionEvent>
            {
                TurnResolutionEvent.HoldPerformed(offer.Value),
                TurnResolutionEvent.CardWasResolved(card.Id, card.OwnerId)
            };
            var acknowledgement = initiative.TryAcknowledgeSelectedCardResolved(card.Id, instances);
            if (!acknowledgement.Accepted)
            {
                throw new InvalidOperationException(
                    "A card resolved by the turn authority could not be acknowledged by Initiative authority.");
            }

            events.AddRange(acknowledgement.Events.Select(TurnResolutionEvent.FromInitiative));
            return AdvanceInitiative(events);
        }

        public TurnResolutionResult TryStartMovement(
            ActionResolutionId resolutionId,
            ActionOffer offer)
        {
            var commonRejection = GetCommonRejection();
            if (commonRejection != TurnResolutionRejectionReason.None)
            {
                return TurnResolutionResult.Reject(commonRejection);
            }

            if (!initiative.SelectedActor.IsDefined)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.NoActorSelected);
            }

            if (!initiative.SelectedActor.CardInstanceId.Equals(offer.SourceCardInstanceId) ||
                !initiative.SelectedActor.ActorId.Equals(offer.ActorId))
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.CardNotSelectedForAction);
            }

            if (cardActions == null)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.MovementAuthorityUnavailable);
            }

            if (cardActions.HasPendingResolution)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.ActionAlreadyPending);
            }

            var result = cardActions.StartTurnMovementResolution(
                resolutionId,
                offer,
                definitions,
                instances);
            return result.Accepted
                ? TurnResolutionResult.Accept(result.Events.Select(TurnResolutionEvent.FromCardAction))
                : TurnResolutionResult.Reject(TurnResolutionRejectionReason.MovementActionRejected);
        }

        public TurnResolutionResult TryAnswerMovementChoice(MovementChoiceCommand command)
        {
            var commonRejection = GetCommonRejection();
            if (commonRejection != TurnResolutionRejectionReason.None)
            {
                return TurnResolutionResult.Reject(commonRejection);
            }

            if (cardActions?.PendingResolution == null || !initiative.SelectedActor.IsDefined)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.MovementCommandRejected);
            }

            var pending = cardActions.PendingResolution;
            if (!pending.SourceCardInstanceId.Equals(initiative.SelectedActor.CardInstanceId) ||
                !pending.ActorId.Equals(initiative.SelectedActor.ActorId))
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.MovementCommandRejected);
            }

            var card = instances.Single(candidate => candidate.Id.Equals(pending.SourceCardInstanceId));
            if (card.State.Location != CardLocation.TurnArea ||
                !card.State.HasBeenPlayed ||
                card.State.ResolutionStatus != CardResolutionStatus.Unresolved ||
                card.State.FaceOrientation != CardFaceOrientation.FaceUp)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.CardResolutionFailed);
            }

            var result = cardActions.AnswerMovementChoice(command);
            if (!result.Accepted)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.MovementCommandRejected);
            }

            var events = result.Events.Select(TurnResolutionEvent.FromCardAction).ToList();
            if (!result.IsComplete)
            {
                return TurnResolutionResult.Accept(events);
            }

            if (!CardLifecycleAuthority.TryResolveAfterAction(card))
            {
                throw new InvalidOperationException(
                    "A completed turn Movement could not resolve its prevalidated source card.");
            }

            events.Add(TurnResolutionEvent.CardWasResolved(card.Id, card.OwnerId));
            var acknowledgement = initiative.TryAcknowledgeSelectedCardResolved(card.Id, instances);
            if (!acknowledgement.Accepted)
            {
                throw new InvalidOperationException(
                    "A card resolved after turn Movement could not be acknowledged by Initiative authority.");
            }

            events.AddRange(acknowledgement.Events.Select(TurnResolutionEvent.FromInitiative));
            return AdvanceInitiative(events);
        }

        public TurnResolutionResult TryStartAttack(
            ActionResolutionId resolutionId,
            ActionOffer offer)
        {
            var commonRejection = GetCommonRejection();
            if (commonRejection != TurnResolutionRejectionReason.None)
            {
                return TurnResolutionResult.Reject(commonRejection);
            }

            if (!initiative.SelectedActor.IsDefined)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.NoActorSelected);
            }

            if (!initiative.SelectedActor.CardInstanceId.Equals(offer.SourceCardInstanceId) ||
                !initiative.SelectedActor.ActorId.Equals(offer.ActorId))
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.CardNotSelectedForAction);
            }

            if (cardActions == null)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.AttackAuthorityUnavailable);
            }

            if (cardActions.HasPendingResolution)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.ActionAlreadyPending);
            }

            var result = cardActions.StartTurnAttackResolution(
                resolutionId,
                offer,
                definitions,
                instances,
                participants);
            return result.Accepted
                ? TurnResolutionResult.Accept(result.Events.Select(TurnResolutionEvent.FromAttackAction))
                : TurnResolutionResult.Reject(TurnResolutionRejectionReason.AttackActionRejected);
        }

        public TurnResolutionResult TryAnswerAttackTarget(AttackTargetChoiceCommand command)
        {
            var commonRejection = GetCommonRejection();
            if (commonRejection != TurnResolutionRejectionReason.None)
            {
                return TurnResolutionResult.Reject(commonRejection);
            }

            if (!IsPendingAttackForSelectedActor())
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.AttackCommandRejected);
            }

            var result = cardActions.AnswerAttackTarget(
                command,
                definitions,
                instances,
                participants);
            return result.Accepted
                ? TurnResolutionResult.Accept(result.Events.Select(TurnResolutionEvent.FromAttackAction))
                : TurnResolutionResult.Reject(TurnResolutionRejectionReason.AttackCommandRejected);
        }

        public TurnResolutionResult TryAnswerDefenseResponse(DefenseResponseChoiceCommand command)
        {
            var commonRejection = GetCommonRejection();
            if (commonRejection != TurnResolutionRejectionReason.None)
            {
                return TurnResolutionResult.Reject(commonRejection);
            }

            if (!IsPendingAttackForSelectedActor() || cardActions.PendingDefenseChoice == null)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.AttackCommandRejected);
            }

            var response = cardActions.AnswerDefenseResponse(
                command,
                definitions,
                instances,
                participants);
            if (!response.Accepted)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.AttackCommandRejected);
            }

            var events = response.Events.Select(TurnResolutionEvent.FromAttackAction).ToList();
            var outcome = cardActions.ResolveAttackOutcome(definitions, instances, participants);
            if (!outcome.Accepted)
            {
                throw new InvalidOperationException(
                    "An accepted turn Defense response could not resume its prevalidated Attack outcome.");
            }

            events.AddRange(outcome.Events.Select(TurnResolutionEvent.FromAttackAction));
            var card = instances.Single(candidate =>
                candidate.Id.Equals(initiative.SelectedActor.CardInstanceId));
            if (!CardLifecycleAuthority.TryResolveAfterAction(card))
            {
                throw new InvalidOperationException(
                    "A completed turn Attack could not resolve its prevalidated source card.");
            }

            events.Add(TurnResolutionEvent.CardWasResolved(card.Id, card.OwnerId));
            var acknowledgement = initiative.TryAcknowledgeSelectedCardResolved(card.Id, instances);
            if (!acknowledgement.Accepted)
            {
                throw new InvalidOperationException(
                    "A card resolved after turn Attack could not be acknowledged by Initiative authority.");
            }

            events.AddRange(acknowledgement.Events.Select(TurnResolutionEvent.FromInitiative));
            return AdvanceInitiative(events);
        }

        private bool IsPendingAttackForSelectedActor()
        {
            var pending = cardActions?.PendingAttackResolution;
            return pending != null &&
                initiative.SelectedActor.IsDefined &&
                pending.SourceCardInstanceId.Equals(initiative.SelectedActor.CardInstanceId) &&
                pending.ActorId.Equals(initiative.SelectedActor.ActorId);
        }

        private TurnResolutionResult AdvanceInitiative(IEnumerable<TurnResolutionEvent> priorEvents)
        {
            var events = new List<TurnResolutionEvent>(priorEvents);
            var result = initiative.TrySelectNextActor(definitions, instances, participants);
            if (!result.Accepted)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.InitiativeCommandRejected);
            }

            events.AddRange(result.Events.Select(TurnResolutionEvent.FromInitiative));
            if (result.Events.Any(ruleEvent =>
                    ruleEvent.Kind == InitiativeResolutionEventKind.NoUnresolvedCardsRemaining))
            {
                events.Add(TurnResolutionEvent.TurnCheckpoint(
                    TurnResolutionEventKind.TurnOnlyEffectsCancellationCheckpoint));
                events.Add(TurnResolutionEvent.TurnCheckpoint(TurnResolutionEventKind.TurnEnded));
                IsEnded = true;
            }

            return TurnResolutionResult.Accept(events);
        }

        private TurnResolutionRejectionReason GetCommonRejection()
        {
            if (!IsStarted)
            {
                return TurnResolutionRejectionReason.TurnNotStarted;
            }

            return IsEnded
                ? TurnResolutionRejectionReason.TurnAlreadyEnded
                : TurnResolutionRejectionReason.None;
        }
    }
}
