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
        EndOfRound = 2
    }

    public enum RoundResolutionRejectionReason
    {
        None = 0,
        RoundAlreadyEnded = 1,
        NotAwaitingCommitments = 2,
        NotResolvingTurn = 3,
        CommitmentCommandRejected = 4,
        TurnCommandRejected = 5
    }

    public enum RoundResolutionEventKind
    {
        None = 0,
        CommitmentProgressed = 1,
        TurnSlotRecorded = 2,
        TurnResolutionProgressed = 3,
        TurnAdvanced = 4,
        EndOfRoundEntered = 5
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
            TurnCardCommitmentEvent commitmentEvent,
            TurnResolutionEvent turnEvent,
            RoundTurnSlot turnSlot)
        {
            Kind = kind;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            CommitmentEvent = commitmentEvent;
            TurnEvent = turnEvent;
            TurnSlot = turnSlot;
        }

        public RoundResolutionEventKind Kind { get; }

        public int RoundIndex { get; }

        public int TurnIndex { get; }

        public TurnCardCommitmentEvent CommitmentEvent { get; }

        public TurnResolutionEvent TurnEvent { get; }

        public RoundTurnSlot TurnSlot { get; }

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
                commitmentEvent,
                default,
                null);
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
                default,
                default,
                turnSlot);
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
                default,
                turnEvent,
                null);
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

            return new RoundResolutionEvent(
                kind,
                roundIndex,
                turnIndex,
                default,
                default,
                null);
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
        private TieBreakerState tieBreakerState;

        public RoundResolutionAuthority(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            TieBreakerState initialTieBreakerState,
            int roundIndex = 1,
            CardActionResolutionAuthority cardActions = null)
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
            RoundIndex = roundIndex;
            TurnIndex = 1;
            Phase = RoundResolutionPhase.AwaitingCommitments;
            CurrentCommitmentAuthority = new TurnCardCommitmentAuthority(this.instances, this.participants);
        }

        public int RoundIndex { get; }

        public int TurnIndex { get; private set; }

        public RoundResolutionPhase Phase { get; private set; }

        public TieBreakerState TieBreakerState =>
            CurrentTurnAuthority?.TieBreakerState ?? tieBreakerState;

        public IReadOnlyList<RoundTurnSlot> TurnSlots => turnSlots.AsReadOnly();

        public TurnCardCommitmentAuthority CurrentCommitmentAuthority { get; private set; }

        public TurnResolutionAuthority CurrentTurnAuthority { get; private set; }

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
