using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    public enum RoundResolutionPhase
    {
        AwaitingCommitments = 0,
        ResolvingTurn = 1,
        EndOfRound = 2,
        GameEnded = 3
    }

    public enum RoundResolutionRejectionReason
    {
        None = 0,
        RoundAlreadyEnded = 1,
        NotAwaitingCommitments = 2,
        NotResolvingTurn = 3,
        CommitmentCommandRejected = 4,
        TurnCommandRejected = 5,
        NotResolvingEndOfRound = 6,
        MissingRequiredEndOfRoundHandler = 7,
        GameAlreadyEnded = 8
    }

    public enum RoundResolutionEventKind
    {
        None = 0,
        CommitmentProgressed = 1,
        TurnSlotRecorded = 2,
        TurnResolutionProgressed = 3,
        TurnAdvanced = 4,
        EndOfRoundEntered = 5,
        EndOfRoundStageResolved = 6,
        CardRetrieved = 7,
        ActiveEffectCancellationCheckpoint = 8,
        RoundCompleted = 9,
        NextRoundStarted = 10,
        GameEnded = 11
    }

    public sealed class TurnCardAssignment
    {
        public TurnCardAssignment(
            HeroId heroId,
            CardInstanceId? cardInstanceId,
            bool passedWithEmptyHand)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("A turn-card assignment requires a canonical hero ID.", nameof(heroId));
            }

            if (cardInstanceId.HasValue && !cardInstanceId.Value.IsDefined)
            {
                throw new ArgumentException(
                    "A turn-card assignment requires a defined card instance ID.",
                    nameof(cardInstanceId));
            }

            var hasCard = cardInstanceId.HasValue && cardInstanceId.Value.IsDefined;
            if (hasCard == passedWithEmptyHand)
            {
                throw new ArgumentException(
                    "A turn-card assignment requires exactly one committed card or explicit empty-hand Pass.",
                    nameof(cardInstanceId));
            }

            HeroId = heroId;
            CardInstanceId = cardInstanceId;
            PassedWithEmptyHand = passedWithEmptyHand;
        }

        public HeroId HeroId { get; }

        public CardInstanceId? CardInstanceId { get; }

        public bool PassedWithEmptyHand { get; }

        public PlayerId GetControllingPlayerId(MatchParticipants participants)
        {
            return (participants ?? throw new ArgumentNullException(nameof(participants)))
                .GetPlayerForHero(HeroId).Id;
        }

        public TeamSide GetTeamSide(MatchParticipants participants)
        {
            return (participants ?? throw new ArgumentNullException(nameof(participants)))
                .GetTeamForHero(HeroId).Side;
        }
    }

    public sealed class RoundTurnSlot
    {
        public RoundTurnSlot(
            int roundIndex,
            int turnIndex,
            IEnumerable<TurnCardAssignment> assignments,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants)
        {
            if (roundIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundIndex));
            }

            if (turnIndex < 1 || turnIndex > RoundResolutionAuthority.TurnsPerRound)
            {
                throw new ArgumentOutOfRangeException(nameof(turnIndex));
            }

            if (assignments == null)
            {
                throw new ArgumentNullException(nameof(assignments));
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            var assignmentArray = assignments.ToArray();
            if (assignmentArray.Any(assignment => assignment == null))
            {
                throw new ArgumentException("Turn-slot assignments must not contain null entries.", nameof(assignments));
            }

            var expectedHeroIds = participants.Heroes
                .Select(hero => hero.Id)
                .OrderBy(heroId => heroId.Value, StringComparer.Ordinal)
                .ToArray();
            if (assignmentArray.Length != expectedHeroIds.Length ||
                assignmentArray.Select(assignment => assignment.HeroId).Distinct().Count() != assignmentArray.Length ||
                !assignmentArray.Select(assignment => assignment.HeroId).OrderBy(
                        heroId => heroId.Value,
                        StringComparer.Ordinal)
                    .SequenceEqual(expectedHeroIds))
            {
                throw new ArgumentException(
                    "A turn slot requires exactly one assignment for every configured hero.",
                    nameof(assignments));
            }

            var cardArray = instances.ToArray();
            if (cardArray.Any(card => card == null) ||
                cardArray.Select(card => card.Id).Distinct().Count() != cardArray.Length)
            {
                throw new ArgumentException(
                    "Turn-slot card instances must be non-null and have unique IDs.",
                    nameof(instances));
            }

            var assignedCardIds = assignmentArray
                .Where(assignment => assignment.CardInstanceId.HasValue)
                .Select(assignment => assignment.CardInstanceId.Value)
                .ToArray();
            if (assignedCardIds.Distinct().Count() != assignedCardIds.Length)
            {
                throw new ArgumentException(
                    "A committed card cannot be assigned to more than one hero.",
                    nameof(assignments));
            }

            foreach (var assignment in assignmentArray)
            {
                participants.GetPlayerForHero(assignment.HeroId);
                participants.GetTeamForHero(assignment.HeroId);
                if (!assignment.CardInstanceId.HasValue)
                {
                    continue;
                }

                var card = cardArray.SingleOrDefault(candidate =>
                    candidate.Id.Equals(assignment.CardInstanceId.Value));
                if (card == null || !card.OwnerId.Equals(assignment.HeroId))
                {
                    throw new ArgumentException(
                        "Every committed turn-slot card must exist and be owned by its assigned hero.",
                        nameof(assignments));
                }
            }

            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Assignments = Array.AsReadOnly(assignmentArray
                .OrderBy(assignment => assignment.HeroId.Value, StringComparer.Ordinal)
                .ToArray());
        }

        public int RoundIndex { get; }

        public int TurnIndex { get; }

        public IReadOnlyList<TurnCardAssignment> Assignments { get; }
    }

    public readonly struct RoundResolutionEvent
    {
        private RoundResolutionEvent(
            RoundResolutionEventKind kind,
            int roundIndex,
            int turnIndex,
            TurnCardCommitmentEvent commitmentEvent = default,
            TurnResolutionEvent turnEvent = default,
            RoundTurnSlot turnSlot = null,
            EndOfRoundStage? endOfRoundStage = null,
            EndOfRoundStageOutcomeKind? endOfRoundOutcomeKind = null,
            CardInstanceId? cardInstanceId = null,
            HeroId? cardOwnerId = null,
            CompletedRoundRecord completedRound = null)
        {
            Kind = kind;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            CommitmentEvent = commitmentEvent;
            TurnEvent = turnEvent;
            TurnSlot = turnSlot;
            EndOfRoundStage = endOfRoundStage;
            EndOfRoundOutcomeKind = endOfRoundOutcomeKind;
            CardInstanceId = cardInstanceId;
            CardOwnerId = cardOwnerId;
            CompletedRound = completedRound;
        }

        public RoundResolutionEventKind Kind { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public TurnCardCommitmentEvent CommitmentEvent { get; }
        public TurnResolutionEvent TurnEvent { get; }
        public RoundTurnSlot TurnSlot { get; }
        public EndOfRoundStage? EndOfRoundStage { get; }
        public EndOfRoundStageOutcomeKind? EndOfRoundOutcomeKind { get; }
        public CardInstanceId? CardInstanceId { get; }
        public HeroId? CardOwnerId { get; }
        public CompletedRoundRecord CompletedRound { get; }

        public static RoundResolutionEvent FromCommitment(
            int roundIndex,
            int turnIndex,
            TurnCardCommitmentEvent commitmentEvent)
        {
            if (commitmentEvent.Kind == TurnCardCommitmentEventKind.None)
            {
                throw new ArgumentException("A round commitment event requires a defined nested event.");
            }

            return new RoundResolutionEvent(
                RoundResolutionEventKind.CommitmentProgressed,
                roundIndex,
                turnIndex,
                commitmentEvent: commitmentEvent);
        }

        public static RoundResolutionEvent SlotRecorded(RoundTurnSlot turnSlot)
        {
            if (turnSlot == null)
            {
                throw new ArgumentNullException(nameof(turnSlot));
            }

            return new RoundResolutionEvent(
                RoundResolutionEventKind.TurnSlotRecorded,
                turnSlot.RoundIndex,
                turnSlot.TurnIndex,
                turnSlot: turnSlot);
        }

        public static RoundResolutionEvent FromTurn(
            int roundIndex,
            int turnIndex,
            TurnResolutionEvent turnEvent)
        {
            if (turnEvent.Kind == TurnResolutionEventKind.None)
            {
                throw new ArgumentException("A round turn event requires a defined nested event.");
            }

            return new RoundResolutionEvent(
                RoundResolutionEventKind.TurnResolutionProgressed,
                roundIndex,
                turnIndex,
                turnEvent: turnEvent);
        }

        public static RoundResolutionEvent Boundary(
            RoundResolutionEventKind kind,
            int roundIndex,
            int turnIndex)
        {
            if (kind != RoundResolutionEventKind.TurnAdvanced &&
                kind != RoundResolutionEventKind.EndOfRoundEntered)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return new RoundResolutionEvent(kind, roundIndex, turnIndex);
        }

        public static RoundResolutionEvent EndOfRoundStageOutcome(
            int roundIndex,
            EndOfRoundStage stage,
            EndOfRoundStageOutcomeKind outcomeKind)
        {
            return new RoundResolutionEvent(
                RoundResolutionEventKind.EndOfRoundStageResolved,
                roundIndex,
                RoundResolutionAuthority.TurnsPerRound,
                endOfRoundStage: stage,
                endOfRoundOutcomeKind: outcomeKind);
        }

        public static RoundResolutionEvent RetrievedCard(
            int roundIndex,
            EndOfRoundStage stage,
            CardRetrievalFact retrieval)
        {
            return new RoundResolutionEvent(
                RoundResolutionEventKind.CardRetrieved,
                roundIndex,
                RoundResolutionAuthority.TurnsPerRound,
                endOfRoundStage: stage,
                endOfRoundOutcomeKind: EndOfRoundStageOutcomeKind.Completed,
                cardInstanceId: retrieval.CardInstanceId,
                cardOwnerId: retrieval.OwnerId);
        }

        public static RoundResolutionEvent EndOfRoundBoundary(
            RoundResolutionEventKind kind,
            int roundIndex,
            EndOfRoundStage stage)
        {
            if (kind != RoundResolutionEventKind.ActiveEffectCancellationCheckpoint &&
                kind != RoundResolutionEventKind.GameEnded)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return new RoundResolutionEvent(
                kind,
                roundIndex,
                RoundResolutionAuthority.TurnsPerRound,
                endOfRoundStage: stage);
        }

        public static RoundResolutionEvent CompletedRoundBoundary(
            RoundResolutionEventKind kind,
            CompletedRoundRecord completedRound,
            int currentRoundIndex)
        {
            if (completedRound == null)
            {
                throw new ArgumentNullException(nameof(completedRound));
            }

            if (kind != RoundResolutionEventKind.RoundCompleted &&
                kind != RoundResolutionEventKind.NextRoundStarted)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return new RoundResolutionEvent(
                kind,
                currentRoundIndex,
                kind == RoundResolutionEventKind.NextRoundStarted
                    ? 1
                    : RoundResolutionAuthority.TurnsPerRound,
                completedRound: completedRound);
        }
    }

    public readonly struct RoundResolutionResult
    {
        private RoundResolutionResult(
            bool accepted,
            RoundResolutionRejectionReason rejectionReason,
            TurnCardCommitmentRejectionReason commitmentRejectionReason,
            TurnResolutionRejectionReason turnRejectionReason,
            IEnumerable<RoundResolutionEvent> events)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            CommitmentRejectionReason = commitmentRejectionReason;
            TurnRejectionReason = turnRejectionReason;
            Events = Array.AsReadOnly((events ?? throw new ArgumentNullException(nameof(events))).ToArray());
        }

        public bool Accepted { get; }

        public RoundResolutionRejectionReason RejectionReason { get; }

        public TurnCardCommitmentRejectionReason CommitmentRejectionReason { get; }

        public TurnResolutionRejectionReason TurnRejectionReason { get; }

        public IReadOnlyList<RoundResolutionEvent> Events { get; }

        public static RoundResolutionResult Accept(IEnumerable<RoundResolutionEvent> events)
        {
            return new RoundResolutionResult(
                true,
                RoundResolutionRejectionReason.None,
                TurnCardCommitmentRejectionReason.None,
                TurnResolutionRejectionReason.None,
                events);
        }

        public static RoundResolutionResult Reject(
            RoundResolutionRejectionReason reason,
            TurnCardCommitmentRejectionReason commitmentReason = TurnCardCommitmentRejectionReason.None,
            TurnResolutionRejectionReason turnReason = TurnResolutionRejectionReason.None)
        {
            if (reason == RoundResolutionRejectionReason.None)
            {
                throw new ArgumentException("A rejected round command requires a reason.", nameof(reason));
            }

            return new RoundResolutionResult(
                false,
                reason,
                commitmentReason,
                turnReason,
                Array.Empty<RoundResolutionEvent>());
        }
    }

    public sealed class RoundResolutionAuthority
    {
        public const int TurnsPerRound = 4;

        private readonly IReadOnlyList<CardDefinition> definitions;
        private readonly IReadOnlyList<CardInstance> instances;
        private readonly MatchParticipants participants;
        private readonly CardActionResolutionAuthority cardActions;
        private readonly List<RoundTurnSlot> turnSlots = new List<RoundTurnSlot>();
        private readonly List<CompletedRoundRecord> completedRounds = new List<CompletedRoundRecord>();
        private readonly List<EndOfRoundStageOutcome> endOfRoundOutcomes =
            new List<EndOfRoundStageOutcome>();
        private readonly EndOfRoundFixtureCapabilities endOfRoundCapabilities;
        private readonly IReadOnlyDictionary<EndOfRoundStage, IEndOfRoundStageHandler> endOfRoundHandlers;
        private TieBreakerState tieBreakerState;

        public RoundResolutionAuthority(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            TieBreakerState initialTieBreakerState,
            int roundIndex = 1,
            CardActionResolutionAuthority cardActions = null,
            EndOfRoundFixtureCapabilities endOfRoundCapabilities = null,
            IReadOnlyDictionary<EndOfRoundStage, IEndOfRoundStageHandler> endOfRoundHandlers = null)
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
                    "Round card instances must be non-null and have unique IDs.",
                    nameof(instances));
            }

            this.instances = Array.AsReadOnly(cards);
            this.participants = participants ?? throw new ArgumentNullException(nameof(participants));
            foreach (var card in cards)
            {
                if (!this.definitions.Any(definition => definition.Reference.Equals(card.DefinitionReference)))
                {
                    throw new ArgumentException(
                        "Every round card must reference a current definition.",
                        nameof(instances));
                }

                participants.GetHero(card.OwnerId);
            }

            if (!initialTieBreakerState.IsDefined)
            {
                throw new ArgumentException("Round authority requires Tie Breaker state.", nameof(initialTieBreakerState));
            }

            if (roundIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundIndex));
            }

            tieBreakerState = initialTieBreakerState;
            this.cardActions = cardActions;
            this.endOfRoundCapabilities = endOfRoundCapabilities ??
                EndOfRoundFixtureCapabilities.SyntheticRepeatedRound();
            var handlers = endOfRoundHandlers != null
                ? new Dictionary<EndOfRoundStage, IEndOfRoundStageHandler>(endOfRoundHandlers)
                : new Dictionary<EndOfRoundStage, IEndOfRoundStageHandler>
                {
                    {
                        EndOfRoundStage.RetrieveCardsAndCancelActiveEffects,
                        new SyntheticCardRetrievalHandler()
                    }
                };
            if (handlers.Any(pair =>
                    pair.Key == EndOfRoundStage.Complete ||
                    pair.Value == null))
            {
                throw new ArgumentException(
                    "End-of-round handlers require executable stages and non-null handlers.",
                    nameof(endOfRoundHandlers));
            }

            this.endOfRoundHandlers = new System.Collections.ObjectModel.ReadOnlyDictionary<
                EndOfRoundStage,
                IEndOfRoundStageHandler>(handlers);
            RoundIndex = roundIndex;
            TurnIndex = 1;
            Phase = RoundResolutionPhase.AwaitingCommitments;
            CurrentCommitmentAuthority = new TurnCardCommitmentAuthority(this.instances, this.participants);
        }

        public int RoundIndex { get; private set; }

        public int TurnIndex { get; private set; }

        public RoundResolutionPhase Phase { get; private set; }

        public TieBreakerState TieBreakerState =>
            CurrentTurnAuthority?.TieBreakerState ?? tieBreakerState;

        public IReadOnlyList<RoundTurnSlot> TurnSlots => turnSlots.AsReadOnly();

        public IReadOnlyList<CompletedRoundRecord> CompletedRounds => completedRounds.AsReadOnly();

        public EndOfRoundStage? CurrentEndOfRoundStage { get; private set; }

        public IReadOnlyList<EndOfRoundStageOutcome> EndOfRoundOutcomes =>
            endOfRoundOutcomes.AsReadOnly();

        public TurnCardCommitmentAuthority CurrentCommitmentAuthority { get; private set; }

        public TurnResolutionAuthority CurrentTurnAuthority { get; private set; }

        internal IReadOnlyList<CardDefinition> Definitions => definitions;

        internal IReadOnlyList<CardInstance> Instances => instances;

        internal MatchParticipants Participants => participants;

        internal CardActionResolutionAuthority CardActions => cardActions;

        internal static RoundResolutionAuthority Restore(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            TieBreakerState tieBreakerState,
            int roundIndex,
            int turnIndex,
            RoundResolutionPhase phase,
            IEnumerable<RoundTurnSlot> turnSlots,
            IEnumerable<CompletedRoundRecord> completedRounds,
            EndOfRoundStage? currentEndOfRoundStage,
            IEnumerable<EndOfRoundStageOutcome> endOfRoundOutcomes,
            TurnCardCommitmentAuthority commitmentAuthority,
            TurnResolutionAuthority turnAuthority,
            CardActionResolutionAuthority cardActions)
        {
            var authority = new RoundResolutionAuthority(
                definitions,
                instances,
                participants,
                tieBreakerState,
                roundIndex,
                cardActions);
            if (turnIndex < 1 || turnIndex > TurnsPerRound || !Enum.IsDefined(typeof(RoundResolutionPhase), phase))
            {
                throw new ArgumentException("Restored round cursor is invalid.");
            }

            authority.RoundIndex = roundIndex;
            authority.TurnIndex = turnIndex;
            authority.Phase = phase;
            authority.tieBreakerState = tieBreakerState;
            authority.turnSlots.Clear();
            authority.turnSlots.AddRange(turnSlots ?? throw new ArgumentNullException(nameof(turnSlots)));
            authority.completedRounds.Clear();
            authority.completedRounds.AddRange(completedRounds ?? throw new ArgumentNullException(nameof(completedRounds)));
            authority.endOfRoundOutcomes.Clear();
            authority.endOfRoundOutcomes.AddRange(endOfRoundOutcomes ?? throw new ArgumentNullException(nameof(endOfRoundOutcomes)));
            authority.CurrentEndOfRoundStage = currentEndOfRoundStage;
            authority.CurrentCommitmentAuthority = commitmentAuthority;
            authority.CurrentTurnAuthority = turnAuthority;

            var awaitingShape = phase == RoundResolutionPhase.AwaitingCommitments &&
                commitmentAuthority != null && turnAuthority == null && !currentEndOfRoundStage.HasValue;
            var resolvingShape = phase == RoundResolutionPhase.ResolvingTurn &&
                commitmentAuthority != null && turnAuthority != null && !turnAuthority.IsEnded &&
                !currentEndOfRoundStage.HasValue;
            var endShape = (phase == RoundResolutionPhase.EndOfRound || phase == RoundResolutionPhase.GameEnded) &&
                commitmentAuthority != null && turnAuthority != null && turnAuthority.IsEnded &&
                currentEndOfRoundStage.HasValue;
            if (!awaitingShape && !resolvingShape && !endShape)
            {
                throw new ArgumentException("Restored round phase does not match its active authority or cursor.");
            }

            return authority;
        }

        public RoundResolutionResult TryCommitCard(HeroId heroId, CardInstanceId cardInstanceId)
        {
            return ExecuteCommitment(() => CurrentCommitmentAuthority.TryCommitCard(
                heroId,
                cardInstanceId));
        }

        public RoundResolutionResult TryPass(HeroId heroId)
        {
            return ExecuteCommitment(() => CurrentCommitmentAuthority.TryPass(heroId));
        }

        public RoundResolutionResult TryChooseTiedActor(
            TeamSide choosingSide,
            CardInstanceId cardInstanceId)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryChooseTiedActor(
                choosingSide,
                cardInstanceId));
        }

        public RoundResolutionResult TryPerformHold(CardInstanceId cardInstanceId)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryPerformHold(cardInstanceId));
        }

        public RoundResolutionResult TryStartMovement(
            ActionResolutionId resolutionId,
            ActionOffer offer)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryStartMovement(resolutionId, offer));
        }

        public RoundResolutionResult TryAnswerMovementChoice(MovementChoiceCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerMovementChoice(command));
        }

        public RoundResolutionResult TryPerformFastTravel(
            ActionResolutionId resolutionId,
            ActionOffer replacementOffer,
            ActionDefinitionId replacedMovementActionDefinitionId,
            GuardsDigital.Core.Board.HexCoordinate destination,
            IEnumerable<QuickGameMinionSetupState> minions)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryPerformFastTravel(
                resolutionId,
                replacementOffer,
                replacedMovementActionDefinitionId,
                destination,
                minions));
        }

        public RoundResolutionResult TryStartAttack(
            ActionResolutionId resolutionId,
            ActionOffer offer)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryStartAttack(resolutionId, offer));
        }

        public RoundResolutionResult TryAnswerAttackTarget(AttackTargetChoiceCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerAttackTarget(command));
        }

        public RoundResolutionResult TryAnswerDefenseResponse(DefenseResponseChoiceCommand command)
        {
            return ExecuteTurnCommand(() => CurrentTurnAuthority.TryAnswerDefenseResponse(command));
        }

        public RoundResolutionResult TryAdvanceEndOfRound()
        {
            if (Phase == RoundResolutionPhase.GameEnded)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.GameAlreadyEnded);
            }

            if (Phase != RoundResolutionPhase.EndOfRound || !CurrentEndOfRoundStage.HasValue)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.NotResolvingEndOfRound);
            }

            var stage = CurrentEndOfRoundStage.Value;
            EndOfRoundStageResolution resolution;
            if (!endOfRoundCapabilities.IsAvailable(stage))
            {
                resolution = EndOfRoundStageResolution.NotApplicable();
            }
            else
            {
                if (!endOfRoundHandlers.TryGetValue(stage, out var handler))
                {
                    return RoundResolutionResult.Reject(
                        RoundResolutionRejectionReason.MissingRequiredEndOfRoundHandler);
                }

                resolution = handler.TryResolve(new EndOfRoundStageContext(
                    RoundIndex,
                    instances,
                    participants));
            }

            var events = new List<RoundResolutionEvent>();
            foreach (var retrieval in resolution.RetrievedCards ?? Array.Empty<CardRetrievalFact>())
            {
                events.Add(RoundResolutionEvent.RetrievedCard(RoundIndex, stage, retrieval));
            }

            if (resolution.ActiveEffectCancellationBoundaryReached)
            {
                events.Add(RoundResolutionEvent.EndOfRoundBoundary(
                    RoundResolutionEventKind.ActiveEffectCancellationCheckpoint,
                    RoundIndex,
                    stage));
            }

            events.Add(RoundResolutionEvent.EndOfRoundStageOutcome(
                RoundIndex,
                stage,
                resolution.OutcomeKind));
            if (resolution.OutcomeKind == EndOfRoundStageOutcomeKind.Paused)
            {
                return RoundResolutionResult.Accept(events);
            }

            if (resolution.OutcomeKind == EndOfRoundStageOutcomeKind.GameEnded)
            {
                endOfRoundOutcomes.Add(new EndOfRoundStageOutcome(stage, resolution.OutcomeKind));
                Phase = RoundResolutionPhase.GameEnded;
                events.Add(RoundResolutionEvent.EndOfRoundBoundary(
                    RoundResolutionEventKind.GameEnded,
                    RoundIndex,
                    stage));
                return RoundResolutionResult.Accept(events);
            }

            endOfRoundOutcomes.Add(new EndOfRoundStageOutcome(stage, resolution.OutcomeKind));
            var nextStage = (EndOfRoundStage)((int)stage + 1);
            if (nextStage != EndOfRoundStage.Complete)
            {
                CurrentEndOfRoundStage = nextStage;
                return RoundResolutionResult.Accept(events);
            }

            var completedRound = new CompletedRoundRecord(
                RoundIndex,
                turnSlots,
                endOfRoundOutcomes);
            completedRounds.Add(completedRound);
            events.Add(RoundResolutionEvent.CompletedRoundBoundary(
                RoundResolutionEventKind.RoundCompleted,
                completedRound,
                RoundIndex));

            RoundIndex++;
            TurnIndex = 1;
            turnSlots.Clear();
            endOfRoundOutcomes.Clear();
            CurrentEndOfRoundStage = null;
            CurrentTurnAuthority = null;
            CurrentCommitmentAuthority = new TurnCardCommitmentAuthority(instances, participants);
            Phase = RoundResolutionPhase.AwaitingCommitments;
            events.Add(RoundResolutionEvent.CompletedRoundBoundary(
                RoundResolutionEventKind.NextRoundStarted,
                completedRound,
                RoundIndex));
            return RoundResolutionResult.Accept(events);
        }

        private RoundResolutionResult ExecuteCommitment(Func<TurnCardCommitmentResult> command)
        {
            if (Phase == RoundResolutionPhase.EndOfRound)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.RoundAlreadyEnded);
            }

            if (Phase != RoundResolutionPhase.AwaitingCommitments)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.NotAwaitingCommitments);
            }

            var result = command();
            if (!result.Accepted)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.CommitmentCommandRejected,
                    result.RejectionReason);
            }

            var events = result.Events
                .Select(ruleEvent => RoundResolutionEvent.FromCommitment(
                    RoundIndex,
                    TurnIndex,
                    ruleEvent))
                .ToList();
            if (CurrentCommitmentAuthority.IsRevealed)
            {
                StartCurrentTurn(events);
            }

            return RoundResolutionResult.Accept(events);
        }

        private RoundResolutionResult ExecuteTurnCommand(Func<TurnResolutionResult> command)
        {
            if (Phase == RoundResolutionPhase.EndOfRound)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.RoundAlreadyEnded);
            }

            if (Phase != RoundResolutionPhase.ResolvingTurn || CurrentTurnAuthority == null)
            {
                return RoundResolutionResult.Reject(RoundResolutionRejectionReason.NotResolvingTurn);
            }

            var result = command();
            if (!result.Accepted)
            {
                return RoundResolutionResult.Reject(
                    RoundResolutionRejectionReason.TurnCommandRejected,
                    turnReason: result.RejectionReason);
            }

            var events = result.Events
                .Select(ruleEvent => RoundResolutionEvent.FromTurn(
                    RoundIndex,
                    TurnIndex,
                    ruleEvent))
                .ToList();
            if (CurrentTurnAuthority.IsEnded)
            {
                CompleteCurrentTurn(events);
            }

            return RoundResolutionResult.Accept(events);
        }

        private void StartCurrentTurn(ICollection<RoundResolutionEvent> events)
        {
            var slot = new RoundTurnSlot(
                RoundIndex,
                TurnIndex,
                CurrentCommitmentAuthority.ExpectedHeroIds.Select(heroId =>
                    new TurnCardAssignment(
                        heroId,
                        CurrentCommitmentAuthority.GetRevealedCardInstanceId(heroId),
                        CurrentCommitmentAuthority.GetStatus(heroId) == TurnCardCommitmentStatus.Passed)),
                instances,
                participants);
            turnSlots.Add(slot);
            events.Add(RoundResolutionEvent.SlotRecorded(slot));

            CurrentTurnAuthority = new TurnResolutionAuthority(
                definitions,
                instances,
                participants,
                tieBreakerState,
                cardActions);
            Phase = RoundResolutionPhase.ResolvingTurn;
            var start = CurrentTurnAuthority.TryStart();
            if (!start.Accepted)
            {
                throw new InvalidOperationException("A validated round turn could not start.");
            }

            foreach (var turnEvent in start.Events)
            {
                events.Add(RoundResolutionEvent.FromTurn(RoundIndex, TurnIndex, turnEvent));
            }

            if (CurrentTurnAuthority.IsEnded)
            {
                CompleteCurrentTurn(events);
            }
        }

        private void CompleteCurrentTurn(ICollection<RoundResolutionEvent> events)
        {
            tieBreakerState = CurrentTurnAuthority.TieBreakerState;
            if (TurnIndex == TurnsPerRound)
            {
                Phase = RoundResolutionPhase.EndOfRound;
                CurrentEndOfRoundStage = EndOfRoundStage.MinionBattle;
                events.Add(RoundResolutionEvent.Boundary(
                    RoundResolutionEventKind.EndOfRoundEntered,
                    RoundIndex,
                    TurnIndex));
                return;
            }

            TurnIndex++;
            CurrentTurnAuthority = null;
            CurrentCommitmentAuthority = new TurnCardCommitmentAuthority(instances, participants);
            Phase = RoundResolutionPhase.AwaitingCommitments;
            events.Add(RoundResolutionEvent.Boundary(
                RoundResolutionEventKind.TurnAdvanced,
                RoundIndex,
                TurnIndex));
        }
    }
}
