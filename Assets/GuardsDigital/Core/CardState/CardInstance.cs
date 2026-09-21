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
            CardStateFacts state,
            PrintedInitiativeOverride initiativeOverride = default)
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
            InitiativeOverride = initiativeOverride;
        }

        public CardInstanceId Id { get; }

        public CardDefinitionReference DefinitionReference { get; }

        public HeroId OwnerId { get; }

        public CardStateFacts State { get; private set; }

        /// <summary>
        /// Milestone 8.71a. The printed Initiative some card text has rewritten on THIS card, or
        /// <see cref="PrintedInitiativeOverride.None"/> - which is every card until a sentence writes
        /// one.
        ///
        /// <para><b>It is beside <see cref="State"/> and deliberately not inside it.</b> Rulebook p.10
        /// names four things about where a card is - its location, whether it has been played, whether
        /// it is resolved and which face is up - and <see cref="CardStateFacts"/> is exactly those
        /// four, compared by value at half a dozen seams that ask "did this card's state change".
        /// A rewritten icon is not one of p.10's four, and folding it in would have made Hurry up!
        /// itself read as a state change of the card it rewrote - which is precisely the event that
        /// ENDS the rewrite.</para>
        ///
        /// <para><b>It is cleared by this card's own state changes and by nothing else</b>, which is
        /// the whole of "until it is resolved or otherwise changes state" (HA-Q2). Both mutators
        /// below are the only ways a card's state moves in this build, so the clearing is written
        /// once and every exit - resolved, discarded, retrieved to hand, turned face down, returned
        /// to a deck, set aside, made an item, unlocked as an Ultimate - reaches it without naming
        /// any of them.</para>
        /// </summary>
        public PrintedInitiativeOverride InitiativeOverride { get; private set; }

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

            ApplyState(new CardStateFacts(
                transition.DestinationLocation,
                State.HasBeenPlayed,
                State.ResolutionStatus,
                State.FaceOrientation));
            return true;
        }

        /// <summary>
        /// The one mutation of this card's p.10 state, and therefore - since milestone 8.71a - the one
        /// place a rewritten printed Initiative lapses.
        ///
        /// <para>The rewrite ends at the FIRST state change of the card it was written on, whatever
        /// that change is: <c>CardLifecycleAuthority</c> reaches every transition p.10 knows through
        /// here, so resolution, either discard, either retrieval and a turn to face down all clear it
        /// without this method or that authority listing them. A call that changes nothing - a
        /// transition re-applied to the state it already describes - is not a state change and leaves
        /// the rewrite standing, because p.10's word is "changes".</para>
        /// </summary>
        internal void ApplyState(CardStateFacts state)
        {
            if (InitiativeOverride.IsDefined && !state.Equals(State))
            {
                InitiativeOverride = PrintedInitiativeOverride.None;
            }

            State = state;
        }

        /// <summary>
        /// Milestone 8.71a. Rewrite this card's printed Initiative, or clear a rewrite.
        ///
        /// <para>It is <c>internal</c> and unconditional: <b>whether</b> this card may be rewritten is
        /// <see cref="CardLifecycleAuthority.TrySetPrintedInitiativeOverride"/>'s question, exactly as
        /// whether a card may be discarded is that authority's and never this type's. The division is
        /// milestone 7.1's, restated for a new fact about a card.</para>
        /// </summary>
        internal void ApplyInitiativeOverride(PrintedInitiativeOverride value)
        {
            InitiativeOverride = value;
        }
    }
}
