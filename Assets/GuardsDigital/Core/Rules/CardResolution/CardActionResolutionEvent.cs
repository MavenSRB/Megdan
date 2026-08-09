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
            ActionResolutionStepId stepId,
            int remainingAllowance,
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
            StepId = stepId;
            RemainingAllowance = remainingAllowance;
            MovementEvent = movementEvent;
        }

        public CardActionResolutionEventKind Kind { get; }

        public ActionResolutionId ResolutionId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        public HeroId ActorId { get; }

        public ActionResolutionStepId StepId { get; }

        public int RemainingAllowance { get; }

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

        public bool Equals(CardActionResolutionEvent other)
        {
            return Kind == other.Kind &&
                ResolutionId.Equals(other.ResolutionId) &&
                SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
                ActionDefinitionId.Equals(other.ActionDefinitionId) &&
                ActorId.Equals(other.ActorId) &&
                StepId.Equals(other.StepId) &&
                RemainingAllowance == other.RemainingAllowance &&
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
                hashCode = (hashCode * 397) ^ StepId.GetHashCode();
                hashCode = (hashCode * 397) ^ RemainingAllowance;
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
                resolution.CurrentStepId,
                resolution.RemainingAllowance,
                movementEvent);
        }
    }
}
