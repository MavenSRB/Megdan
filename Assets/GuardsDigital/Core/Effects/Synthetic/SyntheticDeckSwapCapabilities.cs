using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 9.32a. Rulebook p.10's Swap a Card with the owner's deck, as Takahide the Warlord's
    /// basic cards print it.
    ///
    /// <list type="bullet">
    /// <item><see cref="SwapThisCardWithDeckCardV1"/> - Sting like a Bee and Strike like a Tiger, "After
    /// the attack: Swap this card with a different gold card in your deck", and Float like a
    /// Butterfly's "Swap this card with a different gold card in your deck" after its Movement (TK-Q1).
    /// The subject is the card resolving the action.</item>
    /// <item><see cref="SwapOwnedColorCardWithDeckCardV1"/> - Bushido, "Swap your gold card with a
    /// different gold card in your deck; if you swap a resolved or discarded gold card this way, the
    /// new card is facedown." The subject is the actor's one card of the named colour outside the
    /// deck.</item>
    /// </list>
    ///
    /// <para><b>Who chooses.</b> The deck card is the actor's choice (p.10 Making Choices), asked of
    /// his seat alone: a pending choice is projected to its owner only, so no other seat learns which
    /// cards the deck holds (p.4).</para>
    ///
    /// <para><b>What moves.</b> The round's <see cref="IDeckCardSwap"/> exchanges both cards' states,
    /// the turn slot and - for the resolving card - the Initiative's selected card in one operation
    /// (lesson 28). The outgoing card changed state, so the Active effects it was the source of end
    /// (p.17), and a card turned facedown has none (p.10: "When a card becomes facedown, cancel its
    /// Active effect"; the incoming card was in the deck).</para>
    ///
    /// <para>Unregistered until Takahide is dealt (9.32h); a fixture's catalog carries
    /// <see cref="Handlers"/>.</para>
    /// </summary>
    public static class SyntheticDeckSwapCapabilities
    {
        public const string ColorParameterName = "color";
        public const string FacedownWhenRestingParameterName = "facedownWhenResting";
        public const string SwappedOutOutputName = "swappedOut";
        public const string SwappedInOutputName = "swappedIn";

        public static EffectCapabilityReference SwapThisCardWithDeckCardV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("swap-this-card-with-deck-card"), 1);

        public static EffectCapabilityReference SwapOwnedColorCardWithDeckCardV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("swap-owned-color-card-with-deck-card"), 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new SwapWithDeckCardHandler(SwapThisCardWithDeckCardV1, ownedColorSubject: false);
            yield return new SwapWithDeckCardHandler(SwapOwnedColorCardWithDeckCardV1, ownedColorSubject: true);
        }

        private sealed class SwapWithDeckCardHandler : IEffectCapabilityHandler
        {
            private readonly bool ownedColorSubject;

            public SwapWithDeckCardHandler(EffectCapabilityReference reference, bool ownedColorSubject)
            {
                Reference = reference;
                this.ownedColorSubject = ownedColorSubject;
                var parameters = new List<EffectCapabilityParameter>
                {
                    new EffectCapabilityParameter(
                        ColorParameterName,
                        EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.AllExcept(CardColor.None))
                };
                if (ownedColorSubject)
                {
                    parameters.Add(new EffectCapabilityParameter(
                        FacedownWhenRestingParameterName, EffectValueKind.Boolean));
                }

                Schema = new EffectCapabilityParameterSchema(
                    parameters,
                    new[]
                    {
                        new EffectOutputBinding(SwappedOutOutputName, EffectValueKind.CardInstance),
                        new EffectOutputBinding(SwappedInOutputName, EffectValueKind.CardInstance)
                    });
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; }

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var subject = Subject(context);
                var candidates = subject == null
                    ? Array.Empty<CardInstance>()
                    : DeckCandidates(context, subject);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.None,
                        candidates.Select(card => EffectValue.Card(card.Id)),
                        1, 1, subject.OwnerId));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var subject = Subject(context);
                var chosen = subject != null && selected?.Count == 1 &&
                    selected[0].Kind == EffectValueKind.CardInstance
                    ? DeckCandidates(context, subject).SingleOrDefault(card =>
                        card.Id.Equals(selected[0].CardInstanceId))
                    : null;
                var swap = context.RosterState.DeckCardSwap;
                if (chosen == null || swap == null ||
                    !swap.TrySwapWithDeckCard(subject.OwnerId, subject.Id, chosen.Id, FacedownWhenResting(context)))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // p.17: the outgoing card changed state, so an Active effect it was the source of ends
                // now, before the next node, as a spell returned to the book ends its own (9.30c).
                context.Timing?.CancelForSourceCardStateChange(subject.Id, context.RoundIndex, context.TurnIndex);
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(SwappedOutOutputName, EffectValue.Card(subject.Id)),
                    new EffectOutcomeFact(SwappedInOutputName, EffectValue.Card(chosen.Id))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var subject = Subject(context);
                return subject != null && DeckCandidates(context, subject).Count > 0
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }

            private bool FacedownWhenResting(EffectCapabilityContext context) =>
                ownedColorSubject && context.Input(FacedownWhenRestingParameterName).BooleanValue;

            /// <summary>
            /// "This card" is the card resolving the action; "your gold card" is the actor's one card
            /// of that colour that is outside the deck and that p.10 lets a swap name (in hand, or
            /// resolved or discarded faceup). A facedown card has no colour, so it is never "your gold
            /// card" (p.10).
            /// </summary>
            private CardInstance Subject(EffectCapabilityContext context)
            {
                if (!context.CardState.IsAvailable || !context.RosterState.IsAvailable)
                {
                    return null;
                }

                if (!ownedColorSubject)
                {
                    var source = context.CardState.Instances.SingleOrDefault(card =>
                        card.Id.Equals(context.SourceCardInstanceId));
                    return source != null && source.OwnerId.Equals(context.ActorId) &&
                        CardLifecycleAuthority.IsResolvingCardInTurnArea(source)
                        ? source
                        : null;
                }

                var color = Color(context);
                var owned = context.CardState.Instances
                    .Where(card => card.OwnerId.Equals(context.ActorId) &&
                        !card.Id.Equals(context.SourceCardInstanceId) &&
                        CardLifecycleAuthority.IsDeckSwapSubject(card) &&
                        !CardLifecycleAuthority.IsResolvingCardInTurnArea(card) &&
                        ColorOf(context, card) == color)
                    .ToArray();
                return owned.Length == 1 ? owned[0] : null;
            }

            private static IReadOnlyList<CardInstance> DeckCandidates(
                EffectCapabilityContext context, CardInstance subject)
            {
                var color = Color(context);
                return context.CardState.Instances
                    .Where(card => card.OwnerId.Equals(subject.OwnerId) &&
                        !card.Id.Equals(subject.Id) &&
                        card.State.IsInitialInDeck &&
                        ColorOf(context, card) == color)
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                    .ToArray();
            }

            private static CardColor Color(EffectCapabilityContext context) =>
                (CardColor)context.Input(ColorParameterName).IntegerValue;

            private static CardColor ColorOf(EffectCapabilityContext context, CardInstance card) =>
                context.CardState.Definitions
                    .FirstOrDefault(definition => definition.Reference.Equals(card.DefinitionReference))
                    ?.Progression.Color ?? CardColor.None;
        }
    }
}
