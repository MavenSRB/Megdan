using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Exchange two already-resolved positions in one enemy hero's current-round turn history.
    /// This does not change either card state or cancel source-owned Active effects.
    /// </summary>
    public static class SyntheticResolvedCardOrderSwapCapabilities
    {
        public const string OwnerParameterName = "owner";
        public const string FirstOutputName = "first";
        public const string SecondOutputName = "second";

        public static EffectCapabilityReference SwapResolvedPairV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("swap-resolved-card-pair"), 1);

        public static IEffectCapabilityHandler Handler() => new SwapResolvedPairHandler();

        private sealed class SwapResolvedPairHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => SwapResolvedPairV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter(OwnerParameterName, EffectValueKind.Hero) },
                    new[]
                    {
                        new EffectOutputBinding(FirstOutputName, EffectValueKind.CardInstance),
                        new EffectOutputBinding(SecondOutputName, EffectValueKind.CardInstance)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<CardInstance> Candidates(EffectCapabilityContext context)
            {
                var owner = context.Input(OwnerParameterName);
                if (owner.Kind != EffectValueKind.Hero || !owner.HeroId.IsDefined ||
                    owner.HeroId.Equals(context.ActorId) || !context.RosterState.IsAvailable ||
                    context.RosterState.ResolvedCardOrderSwap == null ||
                    !context.CardState.IsAvailable)
                    return Array.Empty<CardInstance>();

                var recorded = context.RosterState.TurnSlots
                    .Where(slot => slot.RoundIndex == context.RoundIndex)
                    .SelectMany(slot => slot.Assignments)
                    .Where(assignment => assignment.HeroId.Equals(owner.HeroId) &&
                        assignment.CardInstanceId.HasValue)
                    .Select(assignment => assignment.CardInstanceId.Value)
                    .ToArray();
                return context.CardState.Instances
                    .Where(card => card.OwnerId.Equals(owner.HeroId) &&
                        CardLifecycleAuthority.IsResolvedCardInTurnArea(card) &&
                        recorded.Count(id => id.Equals(card.Id)) == 1)
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                    .ToArray();
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count < 2
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor,
                        candidates.Select(card => EffectValue.Card(card.Id)), 2, 2));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var candidates = Candidates(context);
                if (selected == null || selected.Count != 2 ||
                    selected.Any(value => value.Kind != EffectValueKind.CardInstance) ||
                    selected[0].CardInstanceId.Equals(selected[1].CardInstanceId) ||
                    selected.Any(value => !candidates.Any(card => card.Id.Equals(value.CardInstanceId))))
                    return EffectExecutionResult.ImpossibleMandatory();

                HeroId owner = context.Input(OwnerParameterName).HeroId;
                var first = selected[0].CardInstanceId;
                var second = selected[1].CardInstanceId;
                if (!context.RosterState.ResolvedCardOrderSwap.TrySwapResolvedCardPositions(
                        owner, first, second))
                    return EffectExecutionResult.ImpossibleMandatory();

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(FirstOutputName, EffectValue.Card(first)),
                    new EffectOutcomeFact(SecondOutputName, EffectValue.Card(second))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count >= 2
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }
    }
}
