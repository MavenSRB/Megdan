using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>p.10's explicitly resolved-card discard, required by Garrus's Chilling Howl.</summary>
    public static class SyntheticResolvedDiscardCapabilities
    {
        public static EffectCapabilityReference DiscardResolvedCardV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("discard-resolved-card"), 1);

        public const string DiscardedOutput = "discarded";

        // Register only when a configured card can start the contract (milestone 8.41e).
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            new IEffectCapabilityHandler[] { new DiscardResolvedCardHandler() };

        private sealed class DiscardResolvedCardHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => DiscardResolvedCardV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(outputs: new[]
                {
                    new EffectOutputBinding(DiscardedOutput, EffectValueKind.CardInstance)
                });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<CardInstance> Candidates(EffectCapabilityContext context) =>
                context.CardState.Instances.Where(card => card.OwnerId.Equals(context.ActorId) &&
                    CardLifecycleAuthority.IsResolvedCardInTurnArea(card))
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal).ToArray();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(card => EffectValue.Card(card.Id)),
                        1, 1, context.ActorId));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var card = selected?.Count == 1 && selected[0].Kind == EffectValueKind.CardInstance
                    ? Candidates(context).SingleOrDefault(value => value.Id.Equals(selected[0].CardInstanceId))
                    : null;
                if (card == null || !CardLifecycleAuthority.TryDiscardResolvedCard(card))
                    return EffectExecutionResult.ImpossibleMandatory();
                context.Timing?.CancelForSourceCardStateChange(card.Id, context.RoundIndex, context.TurnIndex);
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(DiscardedOutput, EffectValue.Card(card.Id))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;
        }
    }
}
