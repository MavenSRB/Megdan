using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 9.32e. "Choose a card in the discard of a friendly hero in radius" - Takahide the
    /// Warlord's Proven Warrior, Chosen Champion and The Right Hand, whose next sentence discards "a card
    /// of the same color". The acting hero chooses (p.10 Making Choices) one card in the named hero's
    /// discard pile, and the card's colour is published for the colour-restricted discard to read, as
    /// Tali's Ice Blast reads the defended card's (8.37).
    ///
    /// <para><b>What can be chosen.</b> A card at rest in the named hero's discard pile. Discarded cards
    /// are open information (p.10), so the choice reveals nothing. A facedown discarded card (Bushido,
    /// 9.32a) has no colour (p.10 Facedown), so it is not a choice; the pile still holds it for every
    /// reader that only asks whether a card is there.</para>
    ///
    /// <para><b>Nothing to choose is impossible, not declined.</b> The sentence is mandatory, so a face
    /// whose friendly hero has no choosable card cannot start (TK-Q4, Rule 14), and the friendly-hero
    /// selection that names this node as its consumer offers only heroes it can act on (6.8l).</para>
    ///
    /// <para>Unregistered until Takahide is dealt (9.32h); a fixture's catalog carries
    /// <see cref="Handlers"/>.</para>
    /// </summary>
    public static class SyntheticDiscardedCardChoiceCapabilities
    {
        public const string OwnerParameterName = "owner";
        public const string ChosenOutputName = "chosen";
        public const string ColorOutputName = "color";

        public static EffectCapabilityReference ChooseDiscardedCardV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("choose-discarded-card"), 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new ChooseDiscardedCardHandler();
        }

        private sealed class ChooseDiscardedCardHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler
        {
            public EffectCapabilityReference Reference => ChooseDiscardedCardV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter(OwnerParameterName, EffectValueKind.Hero) },
                    new[]
                    {
                        new EffectOutputBinding(ChosenOutputName, EffectValueKind.CardInstance),
                        new EffectOutputBinding(ColorOutputName, EffectValueKind.Integer)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor,
                        candidates.Select(card => EffectValue.Card(card.Id)),
                        1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                // Re-derived, not trusted: a card that has left the pile since the offer is refused.
                var chosen = selected?.Count == 1 && selected[0].Kind == EffectValueKind.CardInstance
                    ? Candidates(context).SingleOrDefault(card => card.Id.Equals(selected[0].CardInstanceId))
                    : null;
                if (chosen == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(ChosenOutputName, EffectValue.Card(chosen.Id)),
                    new EffectOutcomeFact(
                        ColorOutputName, EffectValue.Integer((int)ColorOf(context, chosen)))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(card => EffectValue.Card(card.Id)).ToArray();

            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            /// <summary>
            /// The named hero's faceup cards at rest in their discard pile that print a colour, in a
            /// stable order. An unnamed hero (an earlier selection that named nobody) has none.
            /// </summary>
            private static IReadOnlyList<CardInstance> Candidates(EffectCapabilityContext context)
            {
                var owner = context.Input(OwnerParameterName);
                if (owner.Kind != EffectValueKind.Hero || !owner.HeroId.IsDefined ||
                    !context.CardState.IsAvailable)
                {
                    return Array.Empty<CardInstance>();
                }

                return context.CardState.Instances
                    .Where(card => card.OwnerId.Equals(owner.HeroId) &&
                        CardLifecycleAuthority.IsDiscardedRestingCard(card) &&
                        !CardLifecycleAuthority.IsFacedownRestingCard(card) &&
                        ColorOf(context, card) != CardColor.None)
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                    .ToArray();
            }

            private static CardColor ColorOf(EffectCapabilityContext context, CardInstance card) =>
                context.CardState.Definitions
                    .SingleOrDefault(definition => definition.Reference.Equals(card.DefinitionReference))
                    ?.Progression.Color ?? CardColor.None;
        }
    }
}
