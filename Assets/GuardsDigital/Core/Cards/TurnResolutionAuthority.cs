using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Minions;

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
        AttackCommandRejected = 15,
        FastTravelAuthorityUnavailable = 16,
        FastTravelActionRejected = 17,
        SkillAuthorityUnavailable = 18,
        SkillActionRejected = 19,
        SkillCommandRejected = 20,
        ClearAuthorityUnavailable = 21,
        ClearActionRejected = 22,
        CorrectionCommandRejected = 23,
        HeroRespawnChoicePending = 24,
        HeroRespawnCommandRejected = 25,

        /// <summary>
        /// Milestone 6.5af: the Skill can apply nothing, so starting it would spend the turn on an
        /// action p.12 says the player is never forced to perform.
        /// </summary>
        SkillCannotApplyAnything = 26
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
        AttackActionProgressed = 7,
        FastTravelActionProgressed = 8,
        TimingProgressed = 9,
        SkillActionProgressed = 10,
        ClearActionProgressed = 11,
        HeroRespawned = 12,
        CardResolvedWithoutAction = 13
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
            CardActionResolutionEvent cardActionEvent,
            ActiveEffectLifecycleEvent activeEffectEvent = default,
            HexCoordinate coordinate = default)
        {
            Kind = kind;
            InitiativeEvent = initiativeEvent;
            CardInstanceId = cardInstanceId;
            ActorId = actorId;
            ActionDefinitionId = actionDefinitionId;
            InvocationContext = invocationContext;
            CardActionEvent = cardActionEvent;
            ActiveEffectEvent = activeEffectEvent;
            Coordinate = coordinate;
        }

        public TurnResolutionEventKind Kind { get; }

        public InitiativeResolutionEvent InitiativeEvent { get; }

        public CardInstanceId CardInstanceId { get; }

        public HeroId ActorId { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        public ActionInvocationContext InvocationContext { get; }

        public CardActionResolutionEvent CardActionEvent { get; }

        public ActiveEffectLifecycleEvent ActiveEffectEvent { get; }

        public HexCoordinate Coordinate { get; }

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

        public static TurnResolutionEvent HeroWasRespawned(
            HeroId heroId,
            CardInstanceId cardInstanceId,
            HexCoordinate coordinate) =>
            new TurnResolutionEvent(
                TurnResolutionEventKind.HeroRespawned,
                default,
                cardInstanceId,
                heroId,
                default,
                ActionInvocationContext.None,
                default,
                coordinate: coordinate);

        public static TurnResolutionEvent CardWasResolvedWithoutAction(
            CardInstanceId cardInstanceId,
            HeroId heroId) =>
            new TurnResolutionEvent(
                TurnResolutionEventKind.CardResolvedWithoutAction,
                default,
                cardInstanceId,
                heroId,
                default,
                ActionInvocationContext.None,
                default);

        public static TurnResolutionEvent FromCardAction(CardActionResolutionEvent cardActionEvent)
        {
            return FromCardAction(cardActionEvent, TurnResolutionEventKind.MovementActionProgressed);
        }

        public static TurnResolutionEvent FromAttackAction(CardActionResolutionEvent cardActionEvent)
        {
            return FromCardAction(cardActionEvent, TurnResolutionEventKind.AttackActionProgressed);
        }

        public static TurnResolutionEvent FromFastTravelAction(CardActionResolutionEvent cardActionEvent)
        {
            return FromCardAction(cardActionEvent, TurnResolutionEventKind.FastTravelActionProgressed);
        }

        public static TurnResolutionEvent FromClearAction(CardActionResolutionEvent cardActionEvent)
        {
            return FromCardAction(cardActionEvent, TurnResolutionEventKind.ClearActionProgressed);
        }

        public static TurnResolutionEvent FromSkillAction(CardActionResolutionEvent cardActionEvent)
        {
            return FromCardAction(cardActionEvent, TurnResolutionEventKind.SkillActionProgressed);
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

        public static TurnResolutionEvent FromTiming(ActiveEffectLifecycleEvent activeEffectEvent)
        {
            if (!activeEffectEvent.EffectId.IsDefined || activeEffectEvent.Checkpoint == TimingCheckpoint.None)
                throw new ArgumentException("A turn timing event requires a defined effect and checkpoint.", nameof(activeEffectEvent));
            return new TurnResolutionEvent(
                TurnResolutionEventKind.TimingProgressed,
                default,
                default,
                default,
                default,
                ActionInvocationContext.None,
                default,
                activeEffectEvent);
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
        private InitiativeResolutionAuthority initiative;
        private readonly CardActionResolutionAuthority cardActions;
        private readonly ActiveEffectLifecycle timing;
        private int roundIndex;
        private int turnIndex;

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
            : this(definitions, instances, participants, initialTieBreakerState, cardActions, null, 1, 1)
        {
        }

        public TurnResolutionAuthority(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            TieBreakerState initialTieBreakerState,
            CardActionResolutionAuthority cardActions,
            ActiveEffectLifecycle timing,
            int roundIndex,
            int turnIndex)
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
            this.timing = timing ?? new ActiveEffectLifecycle();
            this.roundIndex = roundIndex;
            this.turnIndex = turnIndex;
        }

        public bool IsStarted { get; private set; }

        public bool IsEnded { get; private set; }

        public TieBreakerState TieBreakerState => initiative.TieBreakerState;

        public PendingInitiativeChoice PendingChoice => initiative.PendingChoice;

        public SelectedInitiativeActor SelectedActor => initiative.SelectedActor;

        public PendingHeroRespawnChoice PendingHeroRespawnChoice =>
            !IsStarted || IsEnded || !initiative.SelectedActor.IsDefined || cardActions?.Board == null
                ? null
                : HeroRespawnRules.CreateChoice(
                    initiative.SelectedActor.ActorId,
                    initiative.SelectedActor.CardInstanceId,
                    participants,
                    cardActions.Board);

        public PendingCardActionResolution PendingMovementResolution => cardActions?.PendingResolution;

        public PendingMovementChoice PendingMovementChoice => cardActions?.PendingChoice;

        public PendingAttackResolution PendingAttackResolution => cardActions?.PendingAttackResolution;

        public PendingAttackTargetChoice PendingAttackTargetChoice => cardActions?.PendingAttackTargetChoice;

        public PendingDefenseResponseChoice PendingDefenseChoice => cardActions?.PendingDefenseChoice;

        public GuardsDigital.Core.Rules.EffectResolution.PendingResolution PendingSkillResolution =>
            cardActions?.PendingSkillResolution;

        public PendingEffectChoice PendingEffectChoice => cardActions?.PendingEffectChoice;

        /// <summary>
        /// Milestone 6.5c: the board meaning of the pending effect choice's space-valued candidates,
        /// forwarded from the dispatcher that owns both the frame and the components.
        /// </summary>
        public IReadOnlyList<EffectChoiceCandidatePreview> DescribePendingEffectChoice(
            IEnumerable<CardDefinition> definitions,
            RuleModificationSet modifications) =>
            cardActions?.DescribePendingEffectChoice(definitions, modifications) ??
                Array.Empty<EffectChoiceCandidatePreview>();

        internal CardActionResolutionAuthority CardActions => cardActions;

        public ActiveEffectLifecycle Timing => timing;

        /// <summary>
        /// The modifications in effect for this turn's authoritative state, or an empty set when no
        /// card-action authority is composed. Rebuilt at every command so an effect that expired or a
        /// token that was removed since the last one cannot still supply a rule.
        /// </summary>
        public RuleModificationSet CurrentModifications() =>
            cardActions == null
                ? RuleModificationSet.Empty
                : cardActions.CurrentModifications(timing, participants, TieBreakerState);

        internal TurnResolutionRestoreState CaptureRestoreState() =>
            new TurnResolutionRestoreState(
                IsStarted,
                IsEnded,
                TieBreakerState,
                initiative.CaptureRestoreState(),
                roundIndex,
                turnIndex);

        internal static TurnResolutionAuthority Restore(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            CardActionResolutionAuthority cardActions,
            ActiveEffectLifecycle timing,
            TurnResolutionRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var authority = new TurnResolutionAuthority(
                definitions,
                instances,
                participants,
                state.TieBreakerState,
                cardActions,
                timing,
                state.RoundIndex,
                state.TurnIndex)
            {
                IsStarted = state.IsStarted,
                IsEnded = state.IsEnded,
                initiative = InitiativeResolutionAuthority.Restore(
                    state.TieBreakerState,
                    state.Initiative)
            };
            if (!state.IsStarted ||
                (!state.IsEnded && state.Initiative.PendingChoice == null &&
                    !state.Initiative.SelectedActor.IsDefined))
            {
                throw new ArgumentException("Restored active turn state is incomplete.", nameof(state));
            }

            return authority;
        }

        public TurnResolutionResult TryStart()
        {
            if (IsStarted)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.TurnAlreadyStarted);
            }

            IsStarted = true;
            var timingEvents = timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                TimingCheckpoint.TurnStarted,
                roundIndex,
                turnIndex),
                initiative.TieBreakerState);
            return AdvanceInitiative(timingEvents.Select(TurnResolutionEvent.FromTiming));
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

        public TurnResolutionResult TryAnswerHeroRespawn(HeroRespawnCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (!IsStarted)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.TurnNotStarted);
            }

            if (IsEnded)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.TurnAlreadyEnded);
            }

            var choice = PendingHeroRespawnChoice;
            if (choice == null || !command.HeroId.Equals(choice.HeroId) ||
                !command.CardInstanceId.Equals(choice.CardInstanceId))
            {
                return TurnResolutionResult.Reject(
                    TurnResolutionRejectionReason.HeroRespawnCommandRejected);
            }

            if (command.Kind == HeroRespawnCommandKind.Respawn)
            {
                if (!choice.RespawnInvocations.Contains(command) ||
                    !cardActions.Board.CommitValidatedHeroRespawn(
                        command.HeroId, command.Destination))
                {
                    return TurnResolutionResult.Reject(
                        TurnResolutionRejectionReason.HeroRespawnCommandRejected);
                }

                return TurnResolutionResult.Accept(new[]
                {
                    TurnResolutionEvent.HeroWasRespawned(
                        command.HeroId, command.CardInstanceId, command.Destination)
                });
            }

            if (!choice.DeclineInvocation.Equals(command))
            {
                return TurnResolutionResult.Reject(
                    TurnResolutionRejectionReason.HeroRespawnCommandRejected);
            }

            var card = instances.Single(candidate => candidate.Id.Equals(command.CardInstanceId));
            if (!CardLifecycleAuthority.TryResolveAfterAction(card))
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.CardResolutionFailed);
            }

            var events = new List<TurnResolutionEvent>
            {
                TurnResolutionEvent.CardWasResolvedWithoutAction(card.Id, command.HeroId),
                TurnResolutionEvent.CardWasResolved(card.Id, command.HeroId)
            };
            foreach (var timingEvent in NotifySourceCardStateChanged(card.Id))
            {
                events.Add(TurnResolutionEvent.FromTiming(timingEvent));
            }

            var acknowledgement = initiative.TryAcknowledgeSelectedCardResolved(card.Id, instances);
            if (!acknowledgement.Accepted)
            {
                throw new InvalidOperationException(
                    "A card resolved without action could not be acknowledged by Initiative authority.");
            }

            events.AddRange(acknowledgement.Events.Select(TurnResolutionEvent.FromInitiative));
            return AdvanceInitiative(events);
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

            var events = new List<TurnResolutionEvent>();
            AddTiming(events, TimingCheckpoint.ActionStarted);
            events.Add(TurnResolutionEvent.HoldPerformed(offer.Value));
            events.Add(TurnResolutionEvent.CardWasResolved(card.Id, card.OwnerId));
            foreach (var timingEvent in NotifySourceCardStateChanged(card.Id))
                events.Add(TurnResolutionEvent.FromTiming(timingEvent));
            AddTiming(events, TimingCheckpoint.ActionEnded);
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
                instances,
                CurrentModifications());
            if (!result.Accepted)
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.MovementActionRejected);
            return TurnResolutionResult.Accept(
                MaterializeSteps(result.Steps, TurnResolutionEvent.FromCardAction));
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

            var result = cardActions.AnswerMovementChoice(command, CurrentModifications());
            if (!result.Accepted)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.MovementCommandRejected);
            }

            var events = MaterializeSteps(result.Steps, TurnResolutionEvent.FromCardAction);
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
            foreach (var timingEvent in NotifySourceCardStateChanged(card.Id))
                events.Add(TurnResolutionEvent.FromTiming(timingEvent));
            var acknowledgement = initiative.TryAcknowledgeSelectedCardResolved(card.Id, instances);
            if (!acknowledgement.Accepted)
            {
                throw new InvalidOperationException(
                    "A card resolved after turn Movement could not be acknowledged by Initiative authority.");
            }

            events.AddRange(acknowledgement.Events.Select(TurnResolutionEvent.FromInitiative));
            return AdvanceInitiative(events);
        }

        public TurnResolutionResult TryPerformFastTravel(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            GuardsDigital.Core.Board.HexCoordinate destination,
            IEnumerable<MinionOccupancy> minions)
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

            if (!initiative.SelectedActor.CardInstanceId.Equals(replacementOffer.SourceCardInstanceId) ||
                !initiative.SelectedActor.ActorId.Equals(replacementOffer.ActorId))
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.CardNotSelectedForAction);
            }

            if (cardActions == null)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.FastTravelAuthorityUnavailable);
            }

            if (cardActions.HasPendingResolution)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.ActionAlreadyPending);
            }

            var result = cardActions.PerformTurnFastTravel(
                resolutionId,
                replacementOffer,
                replacedMovementActionDefinitionId,
                destination,
                definitions,
                instances,
                participants,
                minions,
                CurrentModifications());
            if (!result.Accepted || !result.IsComplete)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.FastTravelActionRejected);
            }

            var card = instances.Single(candidate =>
                candidate.Id.Equals(initiative.SelectedActor.CardInstanceId));
            if (!CardLifecycleAuthority.TryResolveAfterAction(card))
            {
                throw new InvalidOperationException(
                    "A completed Fast Travel could not resolve its prevalidated source card.");
            }

            var events = MaterializeSteps(result.Steps, TurnResolutionEvent.FromFastTravelAction);
            events.Add(TurnResolutionEvent.CardWasResolved(card.Id, card.OwnerId));
            foreach (var timingEvent in NotifySourceCardStateChanged(card.Id))
                events.Add(TurnResolutionEvent.FromTiming(timingEvent));
            var acknowledgement = initiative.TryAcknowledgeSelectedCardResolved(card.Id, instances);
            if (!acknowledgement.Accepted)
            {
                throw new InvalidOperationException(
                    "A card resolved after Fast Travel could not be acknowledged by Initiative authority.");
            }

            events.AddRange(acknowledgement.Events.Select(TurnResolutionEvent.FromInitiative));
            return AdvanceInitiative(events);
        }

        public TurnResolutionResult TryPerformClear(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<TokenInstanceId> selectedTokenIds)
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

            if (!initiative.SelectedActor.CardInstanceId.Equals(replacementOffer.SourceCardInstanceId) ||
                !initiative.SelectedActor.ActorId.Equals(replacementOffer.ActorId))
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.CardNotSelectedForAction);
            }

            if (cardActions == null)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.ClearAuthorityUnavailable);
            }

            if (cardActions.HasPendingResolution)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.ActionAlreadyPending);
            }

            var result = cardActions.PerformTurnClear(
                resolutionId,
                replacementOffer,
                replacedAttackActionDefinitionId,
                selectedTokenIds,
                definitions,
                instances);
            if (!result.Accepted || !result.IsComplete)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.ClearActionRejected);
            }

            var card = instances.Single(candidate =>
                candidate.Id.Equals(initiative.SelectedActor.CardInstanceId));
            if (!CardLifecycleAuthority.TryResolveAfterAction(card))
            {
                throw new InvalidOperationException(
                    "A completed Clear could not resolve its prevalidated source card.");
            }

            var events = MaterializeSteps(result.Steps, TurnResolutionEvent.FromClearAction);
            events.Add(TurnResolutionEvent.CardWasResolved(card.Id, card.OwnerId));
            foreach (var timingEvent in NotifySourceCardStateChanged(card.Id))
                events.Add(TurnResolutionEvent.FromTiming(timingEvent));
            var acknowledgement = initiative.TryAcknowledgeSelectedCardResolved(card.Id, instances);
            if (!acknowledgement.Accepted)
            {
                throw new InvalidOperationException(
                    "A card resolved after Clear could not be acknowledged by Initiative authority.");
            }

            events.AddRange(acknowledgement.Events.Select(TurnResolutionEvent.FromInitiative));
            return AdvanceInitiative(events);
        }

        public TurnResolutionResult TryStartSkill(
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
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.SkillAuthorityUnavailable);
            }

            if (cardActions.HasPendingResolution)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.ActionAlreadyPending);
            }

            // Milestone 6.5af: refused here rather than only left out of the projection, because the
            // dispatcher revalidates a start against the card-state offer query, which cannot see the
            // board and so cannot know this. A start naming an inert Skill is rejected before any
            // frame exists, so nothing to roll back was created.
            if (!CanStartSkill(offer))
            {
                return TurnResolutionResult.Reject(
                    TurnResolutionRejectionReason.SkillCannotApplyAnything);
            }

            var skillResult = cardActions.StartTurnSkill(
                resolutionId, offer, definitions, instances, participants, timing,
                CurrentModifications(), TieBreakerState, roundIndex, turnIndex);
            if (!skillResult.Accepted)
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.SkillActionRejected);
            return CompleteSkillStep(skillResult);
        }

        /// <summary>
        /// Milestone 6.5af: the current turn's action offers with each one's availability decided
        /// rather than assumed.
        ///
        /// `ActionOfferQuery` derives what the card prints from card state alone and cannot see the
        /// board, so every offer it builds says it is available. This is where that becomes an answer:
        /// a Skill whose effect can apply nothing is stamped unavailable, and every other offer is
        /// returned exactly as the card query built it. `ActionOffer.IsAvailable` has carried this
        /// meaning since it was introduced; until now nothing produced a false.
        /// </summary>
        public IReadOnlyList<ActionOffer> GetTurnActionOffers()
        {
            if (!initiative.SelectedActor.IsDefined)
            {
                return Array.Empty<ActionOffer>();
            }

            var offers = ActionOfferQuery.GetAvailableTurnActionOffers(
                definitions,
                instances,
                initiative.SelectedActor.ActorId,
                initiative.SelectedActor.CardInstanceId);
            return Array.AsReadOnly(offers
                .Select(offer => offer.IsAvailable && !CanStartSkill(offer)
                    ? offer.WithAvailability(false)
                    : offer)
                .ToArray());
        }

        /// <summary>
        /// One statement of the judgement, so the offer set and the start command cannot disagree.
        /// Answered <c>true</c> wherever it cannot be asked at all - no Skill authority yet - because
        /// this refuses an action proven empty and never one that merely could not be judged.
        /// </summary>
        private bool CanStartSkill(ActionOffer offer) =>
            cardActions == null ||
            cardActions.CanSkillApplyAnything(
                offer, definitions, participants, timing, CurrentModifications(),
                roundIndex, turnIndex);

        public TurnResolutionResult TryAnswerEffectChoice(EffectChoiceCommand command)
        {
            var commonRejection = GetCommonRejection();
            if (commonRejection != TurnResolutionRejectionReason.None)
            {
                return TurnResolutionResult.Reject(commonRejection);
            }

            if (cardActions?.PendingSkillResolution == null || !initiative.SelectedActor.IsDefined)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.SkillCommandRejected);
            }

            var pending = cardActions.PendingSkillResolution;
            if (!pending.SourceCardInstanceId.Equals(initiative.SelectedActor.CardInstanceId) ||
                !pending.ActorId.Equals(initiative.SelectedActor.ActorId))
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.SkillCommandRejected);
            }

            var card = instances.Single(candidate => candidate.Id.Equals(pending.SourceCardInstanceId));
            if (card.State.Location != CardLocation.TurnArea ||
                !card.State.HasBeenPlayed ||
                card.State.ResolutionStatus != CardResolutionStatus.Unresolved ||
                card.State.FaceOrientation != CardFaceOrientation.FaceUp)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.CardResolutionFailed);
            }

            var answered = cardActions.AnswerEffectChoice(
                command, definitions, participants, timing, CurrentModifications(), TieBreakerState,
                roundIndex, turnIndex);
            if (!answered.Accepted)
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.SkillCommandRejected);
            return CompleteSkillStep(answered);
        }

        /// <summary>Milestone 6.4a's paused after-action correction, or <c>null</c>.</summary>
        public GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            PendingDisplacedMinionCorrection => cardActions?.PendingDisplacedMinionCorrection;

        /// <summary>
        /// Answers milestone 6.4a's paused after-action correction. The gate is the pending choice's
        /// own choosing side rather than the selected actor, because p.18 hands the decision to the
        /// displaced minion's team - which can be the acting hero's opponent - while the correction
        /// still belongs to the selected actor's action, whose card resolves only when it completes.
        /// </summary>
        public TurnResolutionResult TryAnswerDisplacedMinionCorrection(
            GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionCommand command)
        {
            var commonRejection = GetCommonRejection();
            if (commonRejection != TurnResolutionRejectionReason.None)
            {
                return TurnResolutionResult.Reject(commonRejection);
            }

            var pending = cardActions?.PendingDisplacedMinionCorrection;
            if (command == null || pending == null || !initiative.SelectedActor.IsDefined)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.CorrectionCommandRejected);
            }

            if (!pending.SourceCardInstanceId.Equals(initiative.SelectedActor.CardInstanceId) ||
                command.ChoosingSide != pending.ChoosingSide)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.CorrectionCommandRejected);
            }

            var answered = cardActions.AnswerDisplacedMinionCorrection(
                command, participants, TieBreakerState);
            if (!answered.Accepted)
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.CorrectionCommandRejected);
            return CompleteSkillStep(answered);
        }

        /// <summary>
        /// A Skill that has finished resolves its source card and advances Initiative exactly as every
        /// other completed action does; one that is still waiting simply publishes its facts.
        /// </summary>
        private TurnResolutionResult CompleteSkillStep(CardActionResolutionResult result)
        {
            var events = MaterializeSteps(result.Steps, TurnResolutionEvent.FromSkillAction);
            if (!result.IsComplete)
            {
                return TurnResolutionResult.Accept(events);
            }

            var card = instances.Single(candidate =>
                candidate.Id.Equals(initiative.SelectedActor.CardInstanceId));
            if (!CardLifecycleAuthority.TryResolveAfterAction(card))
            {
                throw new InvalidOperationException(
                    "A completed Skill could not resolve its prevalidated source card.");
            }

            events.Add(TurnResolutionEvent.CardWasResolved(card.Id, card.OwnerId));
            foreach (var timingEvent in NotifySourceCardStateChanged(card.Id))
                events.Add(TurnResolutionEvent.FromTiming(timingEvent));
            var acknowledgement = initiative.TryAcknowledgeSelectedCardResolved(card.Id, instances);
            if (!acknowledgement.Accepted)
            {
                throw new InvalidOperationException(
                    "A card resolved after a Skill could not be acknowledged by Initiative authority.");
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
                participants,
                CurrentModifications());
            if (!result.Accepted)
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.AttackActionRejected);
            return TurnResolutionResult.Accept(
                MaterializeSteps(result.Steps, TurnResolutionEvent.FromAttackAction));
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
                participants,
                CurrentModifications());
            if (!result.Accepted)
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.AttackCommandRejected);
            var targetEvents = MaterializeSteps(result.Steps, TurnResolutionEvent.FromAttackAction);
            return result.MinionDefeat != null
                ? CompleteAttackAndAdvance(targetEvents)
                : TurnResolutionResult.Accept(targetEvents);
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
                participants,
                CurrentModifications());
            if (!response.Accepted)
            {
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.AttackCommandRejected);
            }

            // The result-comparison checkpoint is emitted by the Defense resolver itself, as the last
            // step of its own stream, rather than raised here between two backend calls.
            var events = MaterializeSteps(response.Steps, TurnResolutionEvent.FromAttackAction);
            var outcome = cardActions.ResolveAttackOutcome(
                definitions, instances, participants, CurrentModifications());
            if (!outcome.Accepted)
            {
                throw new InvalidOperationException(
                    "An accepted turn Defense response could not resume its prevalidated Attack outcome.");
            }

            events.AddRange(MaterializeSteps(outcome.Steps, TurnResolutionEvent.FromAttackAction));
            return CompleteAttackAndAdvance(events);
        }

        private TurnResolutionResult CompleteAttackAndAdvance(List<TurnResolutionEvent> events)
        {
            var card = instances.Single(candidate =>
                candidate.Id.Equals(initiative.SelectedActor.CardInstanceId));
            if (!CardLifecycleAuthority.TryResolveAfterAction(card))
            {
                throw new InvalidOperationException(
                    "A completed turn Attack could not resolve its prevalidated source card.");
            }

            events.Add(TurnResolutionEvent.CardWasResolved(card.Id, card.OwnerId));
            foreach (var timingEvent in NotifySourceCardStateChanged(card.Id))
                events.Add(TurnResolutionEvent.FromTiming(timingEvent));
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
                events.AddRange(timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                    TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                    roundIndex,
                    turnIndex),
                    initiative.TieBreakerState).Select(TurnResolutionEvent.FromTiming));
                events.Add(TurnResolutionEvent.TurnCheckpoint(
                    TurnResolutionEventKind.TurnOnlyEffectsCancellationCheckpoint));
                AddTiming(events, TimingCheckpoint.TurnEnded);
                events.Add(TurnResolutionEvent.TurnCheckpoint(TurnResolutionEventKind.TurnEnded));
                IsEnded = true;
            }

            return TurnResolutionResult.Accept(events);
        }

        /// <summary>
        /// Milestone 6.1ap, finding <c>F-ER2-05</c>. Turns the ordered stream a resolver emitted into
        /// turn events, raising each checkpoint where the resolver placed it.
        ///
        /// This replaced <c>ApplyActionTiming</c>, which rescanned the produced event list and inferred
        /// checkpoints from each fact's <see cref="CardActionResolutionEventKind"/>. Nothing here reads
        /// a fact - the only branch is on whether a step is a fact or a checkpoint - so a new action or
        /// effect family cannot require an arm in this method.
        ///
        /// The caller supplies <paramref name="toTurnEvent"/> because it already knows statically which
        /// action family it is resolving; carrying that discriminator on the step would only move the
        /// switch here.
        /// </summary>
        private List<TurnResolutionEvent> MaterializeSteps(
            IEnumerable<ResolutionStep> steps,
            Func<CardActionResolutionEvent, TurnResolutionEvent> toTurnEvent)
        {
            var events = new List<TurnResolutionEvent>();
            foreach (var step in steps)
            {
                if (step.Kind == ResolutionStepKind.Checkpoint)
                {
                    AddTiming(
                        events,
                        step.Checkpoint,
                        step.CheckpointAffectedHeroId,
                        step.CheckpointSourceCardInstanceId);
                    continue;
                }

                events.Add(toTurnEvent(step.Fact));
            }

            return events;
        }

        private void AddTiming(
            ICollection<TurnResolutionEvent> events,
            TimingCheckpoint checkpoint,
            HeroId affectedHeroId = default,
            CardInstanceId sourceCardInstanceId = default)
        {
            if (!affectedHeroId.IsDefined && initiative.SelectedActor.IsDefined)
                affectedHeroId = initiative.SelectedActor.ActorId;
            var timingEvents = timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                checkpoint,
                roundIndex,
                turnIndex,
                affectedHeroId,
                sourceCardInstanceId),
                initiative.TieBreakerState);
            foreach (var timingEvent in timingEvents) events.Add(TurnResolutionEvent.FromTiming(timingEvent));
        }

        public IReadOnlyList<ActiveEffectLifecycleEvent> NotifySourceCardStateChanged(
            CardInstanceId sourceCardInstanceId)
        {
            return timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                TimingCheckpoint.SourceCardStateChanged,
                roundIndex,
                turnIndex,
                sourceCardInstanceId: sourceCardInstanceId),
                initiative.TieBreakerState);
        }

        private TurnResolutionRejectionReason GetCommonRejection()
        {
            if (!IsStarted)
            {
                return TurnResolutionRejectionReason.TurnNotStarted;
            }

            if (PendingHeroRespawnChoice != null)
            {
                return TurnResolutionRejectionReason.HeroRespawnChoicePending;
            }

            return IsEnded
                ? TurnResolutionRejectionReason.TurnAlreadyEnded
                : TurnResolutionRejectionReason.None;
        }
    }
}
