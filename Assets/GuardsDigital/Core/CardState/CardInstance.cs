using System;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    public sealed class CardInstance
    {
        public CardInstance(
            CardInstanceId id,
            CardDefinitionReference definitionReference,
            HeroId ownerId,
            CardStateFacts state)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("Card instance requires a defined ID.", nameof(id));
            }

            if (!definitionReference.IsDefined)
            {
                throw new ArgumentException("Card instance requires a defined card definition reference.", nameof(definitionReference));
            }

            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Card instance requires a defined owner ID.", nameof(ownerId));
            }

            Id = id;
            DefinitionReference = definitionReference;
            OwnerId = ownerId;
            State = state;
        }

        public CardInstanceId Id { get; }

        public CardDefinitionReference DefinitionReference { get; }

        public HeroId OwnerId { get; }

        public CardStateFacts State { get; private set; }

        public static CardInstance CreateInHand(
            CardInstanceId id,
            CardDefinitionReference definitionReference,
            HeroId ownerId)
        {
            return new CardInstance(id, definitionReference, ownerId, CardStateFacts.InitialInHand());
        }

        /// <summary>
        /// Milestone 7.5. A card rulebook p.6's setup leaves in its owner's deck rather than dealing.
        /// </summary>
        public static CardInstance CreateInDeck(
            CardInstanceId id,
            CardDefinitionReference definitionReference,
            HeroId ownerId)
        {
            return new CardInstance(id, definitionReference, ownerId, CardStateFacts.InitialInDeck());
        }

        internal bool TryApplyLocationTransition(ActionCardStateTransition transition)
        {
            if (!transition.IsDefined || State.Location != transition.RequiredLocation)
            {
                return false;
            }

            State = new CardStateFacts(
                transition.DestinationLocation,
                State.HasBeenPlayed,
                State.ResolutionStatus,
                State.FaceOrientation);
            return true;
        }

        internal void ApplyState(CardStateFacts state)
        {
            State = state;
        }
    }
}
