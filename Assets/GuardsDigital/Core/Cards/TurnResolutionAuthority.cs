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
        SkillCannotApplyAnything = 26,

        /// <summary>
        /// Rulebook p.8's mid-turn Lane Push is requested and unfinished, so "the turn continues in the
        /// Initiative order as normal" has not been reached yet.
        ///
        /// Milestone 6.9 introduced this as the checkpoint nothing consumed; milestone 6.11 consumes it,
        /// so what stands here now is a push stopped on the page's sidebar Place - answered by its own
        /// command - or one this composition has no Lane to run. Either way the turn is stopped at the
        /// checkpoint, and saying so is better than a rejection that blames a missing actor or a
        /// resolved card.
        /// </summary>
        IncompleteLanePushTransition = 27,

        /// <summary>
        /// Milestone 8.4: the printed primary's card text has no typed composition yet, so performing
        /// the action would perform part of the card and silently drop the rest.
        ///
        /// Rulebook p.12 never forces a hero to act - they may Hold - so refusing costs the player
        /// nothing they were owed, while running an Attack without its After-the-attack rider or a
        /// Skill without its effect would be a rules violation the match could not see. The action
        /// definition itself states the debt through <see cref="PrintedTextComposition"/>, naming the
        /// milestone that owes the composition or the trigger that would schedule it, so this refusal
        /// can always say whose work is missing.
        /// </summary>
        PrintedCardTextNotComposed = 28,

        /// <summary>
        /// Milestone 8.8b: no token stands adjacent to the actor, so rulebook p.13's Clear could remove
        /// nothing whatever the hero chose.
        ///
        /// It is the same judgement <see cref="SkillCannotApplyAnything"/> makes and it draws the same
        /// distinction: p.13's "any number" includes zero, so a hero standing beside a token may still
        /// clear none, and what is refused is a Clear that *cannot* remove anything rather than one
        /// whose chooser declines to. Refusing costs the player only an action p.12 never forces them
        /// to take. `DEV-007` records it, because the page does permit performing one that removes
        /// nothing.
        /// </summary>
        ClearCannotRemoveAnything = 29
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
        CardResolvedWithoutAction = 13,

        /// <summary>
        /// Milestone 6.9: rulebook p.8's mid-turn Lane Push has been requested at the checkpoint that
        /// precedes the next Initiative action. It is a turn checkpoint rather than a card-action fact,
        /// because the request outlives the action that caused it and is what the turn stops on.
        /// </summary>
        ImmediateLanePushRequested = 14,

        /// <summary>
        /// Milestone 6.11: p.8's mid-turn push has finished, so the turn continues "in the Initiative
        /// order as normal". It is reported separately from the request because the two are separated by
        /// everything the transition did, and by any Place the page's sidebar asked a team for.
        /// </summary>
        ImmediateLanePushResolved = 15,

        /// <summary>
        /// Milestone 8.9: rulebook p.9's supertype, recorded at the checkpoint that completed the
        /// action - "only Gold and Silver cards are basic", and every action performed on one is a
        /// basic action. The event carries <see cref="TurnResolutionEvent.BasicAction"/>, which names
        /// the kind as well as the card, because the trigger that reads it says "a basic skill".
        /// </summary>
        BasicActionPerformed = 16
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
            HexCoordinate coordinate = default,
            PerformedBasicAction basicAction = default)
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
            BasicAction = basicAction;
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

        /// <summary>
        /// Milestone 8.9. The basic action p.9's supertype recorded, on a
        /// <see cref="TurnResolutionEventKind.BasicActionPerformed"/> event and nowhere else.
        /// </summary>
        public PerformedBasicAction BasicAction { get; }

        /// <summary>
        /// Milestone 8.9. p.9's supertype, recorded at the checkpoint that completed the action.
        ///
        /// The identities are re-published on the event's own fields as well as inside the fact, so a
        /// reader that filters this stream by hero or by card sees it without unpacking anything - the
        /// same shape <see cref="HoldPerformed"/> already has.
        /// </summary>
        public static TurnResolutionEvent BasicActionPerformed(PerformedBasicAction basicAction)
        {
            if (!basicAction.IsDefined)
            {
                throw new ArgumentException(
                    "A basic-action event requires the action rulebook p.9 counted as basic.",
                    nameof(basicAction));
            }

            return new TurnResolutionEvent(
                TurnResolutionEventKind.BasicActionPerformed,
                default,
                basicAction.SourceCardInstanceId,
                basicAction.ActorId,
                basicAction.ActionDefinitionId,
                ActionInvocationContext.None,
                default,
                default,
                default,
                basicAction);
        }

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
                kind != TurnResolutionEventKind.TurnEnded &&
                kind != TurnResolutionEventKind.ImmediateLanePushRequested &&
                kind != TurnResolutionEventKind.ImmediateLanePushResolved)
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

        /// <summary>
        /// Milestone 6.11: rulebook p.8's mid-turn push, or <c>null</c> in a composition with no Lane.
        /// It is the round's collaborator, handed down because the checkpoint p.8 puts the push at is the
        /// Initiative advance, which is this authority's.
        /// </summary>
        private readonly GuardsDigital.Core.Rules.LanePush.LanePushConsumption lanePush;

        /// <summary>
        /// Milestone 7.2a: the same item state the round authority owns, because p.16's Item Bonus
        /// raises the Initiative icon and Initiative is what orders this turn. It is bound after
        /// construction, exactly as the card-action dispatcher's is and for the same reason - the round
        /// creates this authority, so a state created here would be a second copy of what the match
        /// records. Absent in a composition with no items, where the printed Initiative is the total.
        /// </summary>
        private GuardsDigital.Core.Rules.Progression.HeroItemState heroItems;

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
            : this(definitions, instances, participants, initialTieBreakerState, cardActions, timing,
                roundIndex, turnIndex, null)
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
            int turnIndex,
            GuardsDigital.Core.Rules.LanePush.LanePushConsumption lanePush)
        {
            this.lanePush = lanePush;
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

        /// <summary>
        /// Milestone 7.2a. Hands this authority the match's item state, which the round owns, so that
        /// Initiative composes the same items every other total does.
        /// </summary>
        internal void BindHeroItems(GuardsDigital.Core.Rules.Progression.HeroItemState items)
        {
            heroItems = items;
        }

        public bool IsStarted { get; private set; }

        public bool IsEnded { get; private set; }

        /// <summary>
        /// Milestone 7.7. Whether the match ended in the middle of this turn, so the decisions it was
        /// waiting on are no longer offered.
        ///
        /// It is distinct from <see cref="IsEnded"/> on purpose. An ended turn is a turn that finished
        /// and hands the round on to the next one; this turn did not finish and never will, and the
        /// round does not advance past it. What the two have in common is the only thing this flag is
        /// about: nothing further is asked of anyone.
        ///
        /// Nothing is discarded. The continuation the turn stopped in the middle of is still held, still
        /// captured by <see cref="CaptureRestoreState"/>, and still written to the payload, so a
        /// restored ended match is the match as it ended and a retained-prefix Undo truncated before the
        /// ending replays back into it.
        /// </summary>
        public bool IsResolutionStoppedByGameEnd { get; private set; }

        internal void StopResolutionForGameEnd()
        {
            IsResolutionStoppedByGameEnd = true;
        }

        public TieBreakerState TieBreakerState => initiative.TieBreakerState;

        public PendingInitiativeChoice PendingChoice =>
            IsResolutionStoppedByGameEnd ? null : initiative.PendingChoice;

        public SelectedInitiativeActor SelectedActor => initiative.SelectedActor;

        public PendingHeroRespawnChoice PendingHeroRespawnChoice =>
            !IsStarted || IsEnded || IsResolutionStoppedByGameEnd ||
            !initiative.SelectedActor.IsDefined || cardActions?.Board == null
                ? null
                : HeroRespawnRules.CreateChoice(
                    initiative.SelectedActor.ActorId,
                    initiative.SelectedActor.CardInstanceId,
                    participants,
                    cardActions.Board);

        public PendingCardActionResolution PendingMovementResolution =>
            IsResolutionStoppedByGameEnd ? null : cardActions?.PendingResolution;

        public PendingMovementChoice PendingMovementChoice =>
            IsResolutionStoppedByGameEnd ? null : cardActions?.PendingChoice;

        public PendingAttackResolution PendingAttackResolution =>
            IsResolutionStoppedByGameEnd ? null : cardActions?.PendingAttackResolution;

        public PendingAttackTargetChoice PendingAttackTargetChoice =>
            IsResolutionStoppedByGameEnd ? null : cardActions?.PendingAttackTargetChoice;

        public PendingDefenseResponseChoice PendingDefenseChoice =>
            IsResolutionStoppedByGameEnd ? null : cardActions?.PendingDefenseChoice;

        public GuardsDigital.Core.Rules.EffectResolution.PendingResolution PendingSkillResolution =>
            IsResolutionStoppedByGameEnd ? null : cardActions?.PendingSkillResolution;

        public PendingEffectChoice PendingEffectChoice =>
            IsResolutionStoppedByGameEnd ? null : cardActions?.PendingEffectChoice;

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
        ///
        /// Milestone 8.7's conditional obstacle overlays make the acting hero part of that state:
        /// p.17's "while an enemy hero is performing an action" is a condition about who is acting, so
        /// the turn's own selected actor is what the set is derived for. Before an actor is selected
        /// there is nobody acting, and <see cref="RuleObject.None"/> says so.
        /// </summary>
        public RuleModificationSet CurrentModifications() =>
            cardActions == null
                ? RuleModificationSet.Empty
                : cardActions.CurrentModifications(
                    timing,
                    participants,
                    TieBreakerState,
                    CurrentUltimates(),
                    initiative.SelectedActor.IsDefined
                        ? GuardsDigital.Core.Rules.Modifications.RuleObject.ForHero(
                            initiative.SelectedActor.ActorId)
                        : GuardsDigital.Core.Rules.Modifications.RuleObject.None);

        /// <summary>
        /// Milestone 7.3's unlocked Ultimates, read off this turn's own card instances at the moment
        /// they are asked for. Derived rather than held for the reason the modification set itself is:
        /// a hero can unlock one, and the state that says so is where the card is.
        /// </summary>
        private GuardsDigital.Core.Rules.Progression.UnlockedUltimateSet CurrentUltimates() =>
            GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(definitions, instances);

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
            TurnResolutionRestoreState state,
            GuardsDigital.Core.Rules.LanePush.LanePushConsumption lanePush)
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
                state.TurnIndex,
                lanePush)
            {
                IsStarted = state.IsStarted,
                IsEnded = state.IsEnded,
                initiative = InitiativeResolutionAuthority.Restore(
                    state.TieBreakerState,
                    state.Initiative)
            };
            // A started turn that has not ended normally has either an actor or a choice deciding who
            // acts next. Milestone 6.9 adds the third state it can legitimately be in: rulebook p.8's
            // mid-turn Lane Push has been requested, the acting hero's card has resolved, and Initiative
            // is deliberately not handed on until the push resolves. A snapshot taken there has to
            // restore a turn that is stopped too, so this is a state to admit rather than a payload to
            // refuse.
            if (!state.IsStarted ||
                (!state.IsEnded && state.Initiative.PendingChoice == null &&
                    !state.Initiative.SelectedActor.IsDefined &&
                    cardActions?.PendingImmediateLanePush == null))
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
                participants,
                heroItems);
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
                instances,
                CurrentModifications());
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

            // Milestone 8.9. p.9 counts *every* action on a basic card as basic, and p.7's Hold is an
            // action a hero performs on one, so a Hold spent from Magnetic Dagger or Static Barrier
            // records the fact exactly as their printed primaries do. It is the one completion the turn
            // authority raises itself rather than materializing from a resolver's stream.
            AddCompletedAction(
                events,
                offer.Value.SourceCardInstanceId,
                offer.Value.ActionDefinitionId,
                offer.Value.ActorId,
                ActionKind.Hold);
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

            // Milestone 8.4. The same statement as on the Attack and Skill starts, so the rule holds
            // for every action a start command can name rather than for the two kinds that happen to
            // carry text today. No content in this build reaches it: a printed secondary Movement
            // suppresses card text and so cannot declare a composition at all, and Wasp prints no
            // Movement primary. It is stated because the next hero who prints one would otherwise
            // perform it without its sentence and nothing would say so.
            if (!CardTextIsComposed(offer))
            {
                return TurnResolutionResult.Reject(
                    TurnResolutionRejectionReason.PrintedCardTextNotComposed);
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

            // Milestone 8.8b, and refused here for the reason milestone 6.5af's Skill clause is refused
            // here: the offer set stamps this answer onto `ActionOffer.IsAvailable`, but the dispatcher
            // re-derives the replacement offer from the card query on its way in and that query cannot
            // see the board. One statement of the judgement, asked in both places.
            if (!cardActions.CanClearRemoveAnything(
                    replacementOffer,
                    replacedAttackActionDefinitionId,
                    definitions,
                    instances,
                    CurrentModifications()))
            {
                return TurnResolutionResult.Reject(
                    TurnResolutionRejectionReason.ClearCannotRemoveAnything);
            }

            var result = cardActions.PerformTurnClear(
                resolutionId,
                replacementOffer,
                replacedAttackActionDefinitionId,
                selectedTokenIds,
                definitions,
                instances,
                CurrentModifications());
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

            // Milestone 8.4: asked before the board question below, so a primary that is both owed its
            // composition and empty is refused for the reason that will still be true after the board
            // moves. Refused here for the same reason 6.5af's rule is: the dispatcher revalidates a
            // start against the card-state offer query, which does not consult availability at all.
            if (!CardTextIsComposed(offer))
            {
                return TurnResolutionResult.Reject(
                    TurnResolutionRejectionReason.PrintedCardTextNotComposed);
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
            if (!initiative.SelectedActor.IsDefined || IsResolutionStoppedByGameEnd)
            {
                return Array.Empty<ActionOffer>();
            }

            var offers = ActionOfferQuery.GetAvailableTurnActionOffers(
                definitions,
                instances,
                initiative.SelectedActor.ActorId,
                initiative.SelectedActor.CardInstanceId);
            return Array.AsReadOnly(offers
                .Select(offer => offer.IsAvailable && !CanStart(offer)
                    ? offer.WithAvailability(false)
                    : offer)
                .ToArray());
        }

        /// <summary>
        /// Whether this offer can be started at all, as the offer set reports it.
        ///
        /// Three rules answer it, each for its own case and each returning true for the others':
        /// milestone 6.5af's Skill whose effect can apply nothing, milestone 7.8a1's Attack with no
        /// legal target, and milestone 8.4's printed primary whose card text has no composition. They
        /// are asked together here so that adding a fourth is one more clause rather than one more
        /// place the offer set is post-processed - which is exactly what the third one was.
        /// </summary>
        private bool CanStart(ActionOffer offer) =>
            CanStartSkill(offer) && CanStartAttack(offer) && CardTextIsComposed(offer) &&
            ActorMayPerform(offer);

        /// <summary>
        /// Milestone 8.7: whether a rule currently in effect bars this hero from an action of this
        /// kind at all.
        ///
        /// It is the fourth clause rather than a fourth place the offer set is post-processed, which
        /// is what the comment above this method asks for. What it adds over the other three is a
        /// different kind of reason: they refuse an action the board leaves nothing to do, while this
        /// refuses one a declared rule forbids doing. Milestone 6.8c gave Fast Travel this treatment
        /// alone; the shared predicate gives every action kind the same one, so a card that first
        /// prints "cannot Attack" is obeyed without another consumer being written.
        ///
        /// Answered <c>true</c> wherever the offer names no readable action, for the reason
        /// <see cref="CardTextIsComposed"/> is: this refuses an action a rule forbids and never one
        /// that could not be judged.
        /// </summary>
        private bool ActorMayPerform(ActionOffer offer) =>
            !ActionOfferQuery.TryFindActionDefinition(offer, definitions, out var action) ||
            !GuardsDigital.Core.Rules.Modifications.ActionRestrictionQuery.IsRestrictedFrom(
                offer.ActorId, action.Kind, CurrentModifications());

        /// <summary>
        /// Milestone 8.4: whether this offer's action can perform the whole of what its card prints.
        ///
        /// A verified definition whose printed text has no typed composition yet says so out loud
        /// through <see cref="ActionDefinition.TextComposition"/>, naming the milestone that owes the
        /// work or the trigger that would schedule it. Performing such an action would carry out the
        /// printed values and silently drop the sentence beside them, and a silently dropped rule is a
        /// rules violation the match cannot detect afterwards - so the action is drawn as one that
        /// exists and cannot be taken, exactly as milestones 6.5af and 7.8a1 draw theirs. Rulebook p.12
        /// is what makes that costless: a hero may Hold rather than act, so the card is still spent and
        /// the turn still passes.
        ///
        /// It is asked of the action rather than of the card, because a card whose primary is owed
        /// still prints a secondary Movement or Defense that is complete; and it is answered true
        /// wherever the action cannot be resolved at all, for the reason the other two are.
        /// </summary>
        private bool CardTextIsComposed(ActionOffer offer) =>
            !ActionOfferQuery.TryFindActionDefinition(offer, definitions, out var action) ||
            !action.TextComposition.IsAwaited;

        /// <summary>
        /// One statement of the judgement, so the offer set and the start command cannot disagree.
        /// Answered <c>true</c> wherever it cannot be asked at all - no Skill authority yet - because
        /// this refuses an action proven empty and never one that merely could not be judged.
        /// </summary>
        private bool CanStartSkill(ActionOffer offer) =>
            cardActions == null ||
            cardActions.CanSkillApplyAnything(
                offer, definitions, instances, participants, timing, CurrentModifications(),
                roundIndex, turnIndex);

        /// <summary>
        /// Milestone 7.8a1: the same statement for an Attack. Rulebook p.12 lets a player Hold rather
        /// than act, so an Attack the rules can prove has nothing to target is drawn as an action that
        /// exists and cannot be taken - milestone 6.5af's treatment, on the other kind of offer it
        /// always applied to.
        ///
        /// It asks the backend that builds the candidates when the Attack starts, so the greyed control
        /// and the refusal that would follow a start are the same answer read at two moments rather than
        /// two readings that could drift. Answered <c>true</c> wherever it cannot be asked, for the
        /// reason the Skill one is.
        /// </summary>
        private bool CanStartAttack(ActionOffer offer) =>
            cardActions == null ||
            cardActions.CanAttackFindTarget(
                offer, definitions, participants, CurrentModifications());

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
                command, definitions, instances, participants, timing, CurrentModifications(),
                TieBreakerState, roundIndex, turnIndex);
            if (!answered.Accepted)
                return TurnResolutionResult.Reject(TurnResolutionRejectionReason.SkillCommandRejected);
            return CompleteSkillStep(answered);
        }

        /// <summary>Milestone 6.4a's paused after-action correction, or <c>null</c>.</summary>
        public GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            PendingDisplacedMinionCorrection =>
            IsResolutionStoppedByGameEnd ? null : cardActions?.PendingDisplacedMinionCorrection;

        /// <summary>
        /// Milestone 6.9's mid-turn Lane-Push request, or <c>null</c>. The turn reads it rather than
        /// owning it, the way it reads the pending correction: the dispatcher records it, and what the
        /// turn adds is the checkpoint no later Initiative action gets past while it is set.
        /// </summary>
        public GuardsDigital.Core.Rules.CardResolution.PendingImmediateLanePushRequest
            PendingImmediateLanePush =>
            IsResolutionStoppedByGameEnd ? null : cardActions?.PendingImmediateLanePush;

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
                command,
                new GuardsDigital.Core.Rules.CardResolution.ActionBoundaryContext(
                    definitions, instances, participants, timing, TieBreakerState,
                    roundIndex, turnIndex));
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

            // Milestone 8.4. Milestone 7.8a1 let the backend refuse an Attack with no target, because
            // the offer flag and the refusal were then the same question asked twice. This one is not a
            // question the backend can be asked - it is about the card's words, not its targets - so it
            // is stated here, before the target set exists to be built and rolled back.
            if (!CardTextIsComposed(offer))
            {
                return TurnResolutionResult.Reject(
                    TurnResolutionRejectionReason.PrintedCardTextNotComposed);
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

        /// <summary>
        /// Hands the turn back to Initiative order once a paused mid-turn push has been answered to its
        /// end (milestone 6.11).
        ///
        /// The round calls this, because the answer that finished the push arrived as a round command.
        /// There is no "an Initiative advance is owed" flag anywhere: a held request with no push
        /// running is exactly that fact, and a second record of it could only ever disagree.
        /// </summary>
        internal TurnResolutionResult ContinueAfterImmediateLanePush()
        {
            var events = new List<TurnResolutionEvent>
            {
                TurnResolutionEvent.TurnCheckpoint(TurnResolutionEventKind.ImmediateLanePushResolved)
            };
            cardActions?.ConsumeImmediateLanePush();
            return AdvanceInitiative(events);
        }

        private TurnResolutionResult AdvanceInitiative(IEnumerable<TurnResolutionEvent> priorEvents)
        {
            var events = new List<TurnResolutionEvent>(priorEvents);

            // Milestone 6.9's immediate checkpoint, which milestone 6.11 turned from a stop into p.8's
            // transition. The page pushes the Lane in the middle of the turn and only then does "the
            // turn continue in the Initiative order as normal", so the push runs here rather than after
            // the next actor is selected.
            //
            // It also has to be here rather than in the round dispatcher: this same advance ends the
            // turn when no unresolved cards remain, so a push placed after it would let a mid-turn
            // request fall through into end-of-round resolution.
            var request = cardActions?.PendingImmediateLanePush;
            if (request != null)
            {
                events.Add(TurnResolutionEvent.TurnCheckpoint(
                    TurnResolutionEventKind.ImmediateLanePushRequested));
                if (lanePush == null)
                {
                    // No Lane to push: a board with no Battle Zone cannot have produced this request,
                    // so this is a composition without the transition rather than a rules outcome.
                    return TurnResolutionResult.Accept(events);
                }

                var consumption = lanePush.TryConsume(
                    request.RequestId, request.WinningSide, request.LosingSide,
                    initiative.TieBreakerState);
                if (!consumption.Accepted || consumption.IsPaused)
                {
                    // A refused push leaves the request standing at its checkpoint, exactly as it stood
                    // before milestone 6.11; a paused one withholds the Initiative advance until the
                    // sidebar's Place has been answered. Both are "the turn does not continue yet",
                    // which is the one thing p.8 requires of an unfinished push.
                    return TurnResolutionResult.Accept(events);
                }

                cardActions.ConsumeImmediateLanePush();
                events.Add(TurnResolutionEvent.TurnCheckpoint(
                    TurnResolutionEventKind.ImmediateLanePushResolved));
                if (consumption.Finished.EndedTheGame)
                {
                    // p.8: "the game ends immediately". The round authority reads the ended push and
                    // stops the match; nothing later in this turn runs.
                    return TurnResolutionResult.Accept(events);
                }
            }

            var result = initiative.TrySelectNextActor(definitions, instances, participants, heroItems);
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
                    if (step.Checkpoint == TimingCheckpoint.ActionEnded)
                    {
                        AddCompletedAction(
                            events,
                            step.CheckpointSourceCardInstanceId,
                            step.CompletedActionDefinitionId,
                            step.CompletedActorId,
                            step.CompletedActionKind);
                        continue;
                    }

                    AddTiming(
                        events,
                        step.Checkpoint,
                        step.CheckpointAffectedHeroId,
                        step.CheckpointSourceCardInstanceId);
                    continue;
                }

                // Milestone 8.5. Rulebook p.14 applies card text at three numbered points inside an
                // Attack, and the resolver says where each one falls. It is applied here, in stream
                // order, for the same reason a checkpoint is: this is the one place that turns the
                // ordered stream a card action produced into the ordered events a turn emits, so the
                // text lands exactly where the page puts it and nothing downstream has to infer it.
                if (step.Kind == ResolutionStepKind.CardText)
                {
                    foreach (var fact in cardActions.ApplyCardText(
                        step, definitions, instances, participants, timing, CurrentModifications(),
                        roundIndex, turnIndex))
                    {
                        events.Add(toTurnEvent(fact));
                    }

                    continue;
                }

                events.Add(toTurnEvent(step.Fact));
            }

            return events;
        }

        /// <summary>
        /// Milestone 8.9. One action has finished: record rulebook p.9's supertype if it applies, then
        /// raise the checkpoint that finishes it.
        ///
        /// <para>The order is the sentence's. High Voltage triggers "each time <em>after</em> you
        /// perform a basic skill" (milestone 8.13), so the fact that the basic action happened is
        /// already recorded when the checkpoint a trigger hangs on is raised, rather than arriving
        /// after the moment it describes.</para>
        ///
        /// <para>This is the only place the fact is produced, and it is unavoidable rather than
        /// remembered: <see cref="ResolutionStep.AtCheckpoint"/> refuses this checkpoint, so every
        /// resolver reaches it through <see cref="ResolutionStep.AtActionEnded"/> and hands over what
        /// the classification needs. A future action family gets the same treatment by construction.</para>
        /// </summary>
        private void AddCompletedAction(
            ICollection<TurnResolutionEvent> events,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionKind kind)
        {
            var basicAction = BasicActionQuery.ForCompletedAction(
                actorId, sourceCardInstanceId, actionDefinitionId, kind, definitions, instances);
            if (basicAction.IsDefined)
            {
                events.Add(TurnResolutionEvent.BasicActionPerformed(basicAction));
            }

            AddTiming(events, TimingCheckpoint.ActionEnded, default, sourceCardInstanceId);
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

            // Milestone 6.9. The gate is "a request is held and the action that caused it has closed",
            // not simply "a request is held": a Skill can record p.8's cause at one node and still owe
            // a choice or an after-action correction, and refusing the command that finishes it would
            // stop the match one step short of the checkpoint rather than at it.
            if (cardActions != null && cardActions.PendingImmediateLanePush != null &&
                !cardActions.HasPendingResolution)
            {
                return TurnResolutionRejectionReason.IncompleteLanePushTransition;
            }

            return IsEnded
                ? TurnResolutionRejectionReason.TurnAlreadyEnded
                : TurnResolutionRejectionReason.None;
        }
    }
}
