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
            : this(
                sourceCardInstanceId,
                sourceDefinitionReference,
                actionDefinitionId,
                actorId,
                invocationContext,
                isAvailable,
                AttackRangeKind.None,
                default)
        {
        }

        public ActionOffer(
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionInvocationContext invocationContext,
            bool isAvailable,
            AttackRangeKind attackRange,
            ActionCardStateTransition sourceCardStateTransition,
            ActionUnavailability unavailability = default)
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

            if (!Enum.IsDefined(typeof(AttackRangeKind), attackRange))
            {
                throw new ArgumentException("Action offer requires a valid Attack range classification.", nameof(attackRange));
            }

            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionReference = sourceDefinitionReference;
            ActionDefinitionId = actionDefinitionId;
            ActorId = actorId;
            InvocationContext = invocationContext;
            IsAvailable = isAvailable;
            AttackRange = attackRange;
            SourceCardStateTransition = sourceCardStateTransition;
            Unavailability = isAvailable ? ActionUnavailability.None : unavailability;
        }

        public CardInstanceId SourceCardInstanceId { get; }

        public CardDefinitionReference SourceDefinitionReference { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        public HeroId ActorId { get; }

        public ActionInvocationContext InvocationContext { get; }

        public bool IsAvailable { get; }

        /// <summary>
        /// The same offer with availability decided. Milestone 6.5af gave this field its first
        /// producer of <c>false</c>: the card query that builds an offer sees card state alone, so it
        /// cannot know whether the action could do anything, and the authority that can stamps the
        /// answer here rather than rebuilding the offer and risking a differing copy.
        /// </summary>
        public ActionOffer WithAvailability(bool isAvailable) =>
            WithAvailability(isAvailable, ActionUnavailability.None);

        /// <summary>
        /// Milestone 8.35l: the same stamp, saying why where the authority knows why.
        ///
        /// <para>The reason is dropped whenever the offer is available, in the constructor rather than
        /// here, so an offer can never carry both "you may take this" and an explanation of why you may
        /// not. A stamp with <see cref="ActionUnavailability.None"/> is the ordinary greyed control
        /// that has always existed and is not a regression from one that speaks.</para>
        /// </summary>
        public ActionOffer WithAvailability(bool isAvailable, ActionUnavailability unavailability) =>
            new ActionOffer(
                SourceCardInstanceId,
                SourceDefinitionReference,
                ActionDefinitionId,
                ActorId,
                InvocationContext,
                isAvailable,
                AttackRange,
                SourceCardStateTransition,
                unavailability);

        public AttackRangeKind AttackRange { get; }

        public ActionCardStateTransition SourceCardStateTransition { get; }

        /// <summary>
        /// Milestone 8.35l: why this offer cannot be taken, where the authority can say. Always
        /// <see cref="ActionUnavailability.None"/> on an available offer.
        /// </summary>
        public ActionUnavailability Unavailability { get; }

        public bool Equals(ActionOffer other)
        {
            return SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
                SourceDefinitionReference.Equals(other.SourceDefinitionReference) &&
                ActionDefinitionId.Equals(other.ActionDefinitionId) &&
                ActorId.Equals(other.ActorId) &&
                InvocationContext == other.InvocationContext &&
                IsAvailable == other.IsAvailable &&
                AttackRange == other.AttackRange &&
                SourceCardStateTransition.Equals(other.SourceCardStateTransition) &&
                Unavailability.Equals(other.Unavailability);
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
                hashCode = (hashCode * 397) ^ (int)AttackRange;
                hashCode = (hashCode * 397) ^ SourceCardStateTransition.GetHashCode();
                hashCode = (hashCode * 397) ^ Unavailability.GetHashCode();
                return hashCode;
            }
        }
    }
}
