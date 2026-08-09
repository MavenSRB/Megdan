using System;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    public readonly struct ActionOffer : IEquatable<ActionOffer>
    {
        public ActionOffer(
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionInvocationContext invocationContext,
            bool isAvailable)
        {
            if (!sourceCardInstanceId.IsDefined)
            {
                throw new ArgumentException("Action offer requires a defined source card instance ID.", nameof(sourceCardInstanceId));
            }

            if (!sourceDefinitionReference.IsDefined)
            {
                throw new ArgumentException("Action offer requires a defined source definition reference.", nameof(sourceDefinitionReference));
            }

            if (!actionDefinitionId.IsDefined)
            {
                throw new ArgumentException("Action offer requires a defined action definition ID.", nameof(actionDefinitionId));
            }

            if (!actorId.IsDefined)
            {
                throw new ArgumentException("Action offer requires a defined actor ID.", nameof(actorId));
            }

            if (invocationContext == ActionInvocationContext.None)
            {
                throw new ArgumentException("Action offer requires an invocation context.", nameof(invocationContext));
            }

            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionReference = sourceDefinitionReference;
            ActionDefinitionId = actionDefinitionId;
            ActorId = actorId;
            InvocationContext = invocationContext;
            IsAvailable = isAvailable;
        }

        public CardInstanceId SourceCardInstanceId { get; }

        public CardDefinitionReference SourceDefinitionReference { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        public HeroId ActorId { get; }

        public ActionInvocationContext InvocationContext { get; }

        public bool IsAvailable { get; }

        public bool Equals(ActionOffer other)
        {
            return SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
                SourceDefinitionReference.Equals(other.SourceDefinitionReference) &&
                ActionDefinitionId.Equals(other.ActionDefinitionId) &&
                ActorId.Equals(other.ActorId) &&
                InvocationContext == other.InvocationContext &&
                IsAvailable == other.IsAvailable;
        }

        public override bool Equals(object obj)
        {
            return obj is ActionOffer other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = SourceCardInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ SourceDefinitionReference.GetHashCode();
                hashCode = (hashCode * 397) ^ ActionDefinitionId.GetHashCode();
                hashCode = (hashCode * 397) ^ ActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)InvocationContext;
                hashCode = (hashCode * 397) ^ IsAvailable.GetHashCode();
                return hashCode;
            }
        }
    }
}
