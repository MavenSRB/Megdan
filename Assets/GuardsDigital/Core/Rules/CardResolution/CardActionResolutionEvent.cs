using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public readonly struct CardActionResolutionEvent : IEquatable<CardActionResolutionEvent>
    {
        private CardActionResolutionEvent(
            CardActionResolutionEventKind kind,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            HeroId targetHeroId,
            ActionResolutionStepId stepId,
            int remainingAllowance,
            int attackTotal,
            CardInstanceId defenseCardInstanceId,
            int defenseTotal,
            AttackResolutionOutcome attackOutcome,
            AdjacentMoveEvent movementEvent)
        {
            if (kind == CardActionResolutionEventKind.None)
            {
                throw new ArgumentException("Card action resolution event requires a kind.", nameof(kind));
            }

            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires a resolution ID.", nameof(resolutionId));
            }

            if (!sourceCardInstanceId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires a source card instance ID.", nameof(sourceCardInstanceId));
            }

            if (!actionDefinitionId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires an action definition ID.", nameof(actionDefinitionId));
            }

            if (!actorId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires an actor ID.", nameof(actorId));
            }

            if (!stepId.IsDefined)
            {
                throw new ArgumentException("Card action resolution event requires a step ID.", nameof(stepId));
            }

            Kind = kind;
            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActionDefinitionId = actionDefinitionId;
            ActorId = actorId;
            TargetHeroId = targetHeroId;
            StepId = stepId;
            RemainingAllowance = remainingAllowance;
            AttackTotal = attackTotal;
            DefenseCardInstanceId = defenseCardInstanceId;
            DefenseTotal = defenseTotal;
            AttackOutcome = attackOutcome;
            MovementEvent = movementEvent;
        }

        public CardActionResolutionEventKind Kind { get; }

        public ActionResolutionId ResolutionId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        public HeroId ActorId { get; }

        public HeroId TargetHeroId { get; }

        public ActionResolutionStepId StepId { get; }

        public int RemainingAllowance { get; }

        public int AttackTotal { get; }

        public CardInstanceId DefenseCardInstanceId { get; }

        public int DefenseTotal { get; }

        public AttackResolutionOutcome AttackOutcome { get; }

        public AdjacentMoveEvent MovementEvent { get; }

        public static CardActionResolutionEvent MovementActionStarted(
            PendingCardActionResolution resolution)
        {
            return FromResolution(CardActionResolutionEventKind.MovementActionStarted, resolution, default);
        }

        public static CardActionResolutionEvent MovementChoiceRequested(
            PendingCardActionResolution resolution)
        {
            return FromResolution(CardActionResolutionEventKind.MovementChoiceRequested, resolution, default);
        }

        public static CardActionResolutionEvent MovementStepCommitted(
            PendingCardActionResolution resolution,
            AdjacentMoveEvent movementEvent)
        {
            return FromResolution(CardActionResolutionEventKind.MovementStepCommitted, resolution, movementEvent);
        }

        public static CardActionResolutionEvent MovementActionCompleted(
            PendingCardActionResolution resolution)
        {
            return FromResolution(CardActionResolutionEventKind.MovementActionCompleted, resolution, default);
        }

        public static CardActionResolutionEvent AttackTargetChoiceRequested(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.AttackTargetChoiceRequested, resolution);
        }

        public static CardActionResolutionEvent AttackStarted(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.AttackStarted, resolution);
        }

        public static CardActionResolutionEvent AttackTotalCalculated(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.AttackTotalCalculated, resolution);
        }

        public static CardActionResolutionEvent DefenseResponseChoiceRequested(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseResponseChoiceRequested, resolution);
        }

        public static CardActionResolutionEvent DefenseCardDiscarded(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseCardDiscarded, resolution);
        }

        public static CardActionResolutionEvent DefenseTotalCalculated(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseTotalCalculated, resolution);
        }

        public static CardActionResolutionEvent DefenseDeclined(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseDeclined, resolution);
        }

        public static CardActionResolutionEvent DefenseResponseCompleted(PendingAttackResolution resolution)
        {
            return FromAttackResolution(CardActionResolutionEventKind.DefenseResponseCompleted, resolution);
        }

        public static CardActionResolutionEvent AttackOutcomeDetermined(
            PendingAttackResolution resolution,
            AttackResolutionOutcome outcome)
        {
            if (outcome != AttackResolutionOutcome.SuccessfullyDefended &&
                outcome != AttackResolutionOutcome.AttackSuccessful)
            {
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Attack outcome must be final.");
            }

            var kind = outcome == AttackResolutionOutcome.SuccessfullyDefended
                ? CardActionResolutionEventKind.AttackSuccessfullyDefended
                : CardActionResolutionEventKind.AttackSuccessful;
            return FromAttackResolution(kind, resolution, outcome);
        }

        public static CardActionResolutionEvent AttackResolutionCompleted(
            PendingAttackResolution resolution,
            AttackResolutionOutcome outcome)
        {
            return FromAttackResolution(
                CardActionResolutionEventKind.AttackResolutionCompleted,
                resolution,
                outcome);
        }

        public bool Equals(CardActionResolutionEvent other)
        {
            return Kind == other.Kind &&
                ResolutionId.Equals(other.ResolutionId) &&
                SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
                ActionDefinitionId.Equals(other.ActionDefinitionId) &&
                ActorId.Equals(other.ActorId) &&
                TargetHeroId.Equals(other.TargetHeroId) &&
                StepId.Equals(other.StepId) &&
                RemainingAllowance == other.RemainingAllowance &&
                AttackTotal == other.AttackTotal &&
                DefenseCardInstanceId.Equals(other.DefenseCardInstanceId) &&
                DefenseTotal == other.DefenseTotal &&
                AttackOutcome == other.AttackOutcome &&
                MovementEvent.Equals(other.MovementEvent);
        }

        public override bool Equals(object obj)
        {
            return obj is CardActionResolutionEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ ResolutionId.GetHashCode();
                hashCode = (hashCode * 397) ^ SourceCardInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ ActionDefinitionId.GetHashCode();
                hashCode = (hashCode * 397) ^ ActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ TargetHeroId.GetHashCode();
                hashCode = (hashCode * 397) ^ StepId.GetHashCode();
                hashCode = (hashCode * 397) ^ RemainingAllowance;
                hashCode = (hashCode * 397) ^ AttackTotal;
                hashCode = (hashCode * 397) ^ DefenseCardInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ DefenseTotal;
                hashCode = (hashCode * 397) ^ (int)AttackOutcome;
                hashCode = (hashCode * 397) ^ MovementEvent.GetHashCode();
                return hashCode;
            }
        }

        private static CardActionResolutionEvent FromResolution(
            CardActionResolutionEventKind kind,
            PendingCardActionResolution resolution,
            AdjacentMoveEvent movementEvent)
        {
            if (resolution == null)
            {
                throw new ArgumentNullException(nameof(resolution));
            }

            return new CardActionResolutionEvent(
                kind,
                resolution.ResolutionId,
                resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId,
                resolution.ActorId,
                default,
                resolution.CurrentStepId,
                resolution.RemainingAllowance,
                0,
                default,
                0,
                AttackResolutionOutcome.None,
                movementEvent);
        }

        private static CardActionResolutionEvent FromAttackResolution(
            CardActionResolutionEventKind kind,
            PendingAttackResolution resolution,
            AttackResolutionOutcome outcome = AttackResolutionOutcome.None)
        {
            if (resolution == null)
            {
                throw new ArgumentNullException(nameof(resolution));
            }

            return new CardActionResolutionEvent(
                kind,
                resolution.ResolutionId,
                resolution.SourceCardInstanceId,
                resolution.ActionDefinitionId,
                resolution.ActorId,
                resolution.TargetHeroId,
                resolution.CurrentStepId,
                0,
                resolution.AttackTotal,
                resolution.DefenseOffer.HasValue
                    ? resolution.DefenseOffer.Value.SourceCardInstanceId
                    : default,
                resolution.DefenseTotal ?? 0,
                outcome,
                default);
        }
    }
}
