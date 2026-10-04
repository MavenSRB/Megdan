using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>The targeted hero chooses which of their resolved cards to exchange for their waiting card.</summary>
    public static class EmmittTimeWarpContent
    {
        public const string OwnerParameter = "owner";
        public const string SwappedOutOutput = "swappedOut";
        public const string SwappedInOutput = "swappedIn";

        public static EffectCapabilityReference SwapWithResolvedV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("emmitt-swap-unresolved-with-resolved"), 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new SwapHandler();
        }

        private sealed class SwapHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => SwapWithResolvedV1;
            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(OwnerParameter, EffectValueKind.Hero) },
                new[]
                {
                    new EffectOutputBinding(SwappedOutOutput, EffectValueKind.CardInstance),
                    new EffectOutputBinding(SwappedInOutput, EffectValueKind.CardInstance)
                });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Length > 0
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Length == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.None, candidates.Select(card => EffectValue.Card(card.Id)),
                        1, 1, Owner(context)));
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context,
                IReadOnlyList<EffectValue> selected)
            {
                var candidates = Candidates(context);
                var chosen = selected?.Count == 1 && selected[0].Kind == EffectValueKind.CardInstance
                    ? candidates.SingleOrDefault(card => card.Id.Equals(selected[0].CardInstanceId))
                    : null;
                var owner = Owner(context);
                var waiting = Waiting(context, owner);
                if (chosen == null || waiting == null ||
                    context.RosterState.UnresolvedCardSwap == null ||
                    !context.RosterState.UnresolvedCardSwap.TrySwapUnresolvedWithResolvedCard(
                        owner, waiting.Id, chosen.Id))
                    return EffectExecutionResult.ImpossibleMandatory();

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(SwappedOutOutput, EffectValue.Card(waiting.Id)),
                    new EffectOutcomeFact(SwappedInOutput, EffectValue.Card(chosen.Id))
                });
            }

            private static HeroId Owner(EffectCapabilityContext context)
            {
                var value = context.Input(OwnerParameter);
                return value.Kind == EffectValueKind.Hero && value.HeroId.IsDefined &&
                    !value.HeroId.Equals(context.ActorId)
                    ? value.HeroId : default;
            }

            private static CardInstance Waiting(EffectCapabilityContext context, HeroId owner)
            {
                if (!owner.IsDefined || !context.CardState.IsAvailable || !context.RosterState.IsAvailable)
                    return null;
                var cardId = context.RosterState.TurnSlots
                    .Where(slot => slot.RoundIndex == context.RoundIndex &&
                        slot.TurnIndex == context.TurnIndex)
                    .SelectMany(slot => slot.Assignments)
                    .Where(assignment => assignment.HeroId.Equals(owner) &&
                        assignment.CardInstanceId.HasValue)
                    .Select(assignment => assignment.CardInstanceId.Value)
                    .FirstOrDefault();
                var card = context.CardState.Instances.SingleOrDefault(value => value.Id.Equals(cardId));
                return card != null && card.OwnerId.Equals(owner) &&
                    CardLifecycleAuthority.IsResolvingCardInTurnArea(card) ? card : null;
            }

            private static CardInstance[] Candidates(EffectCapabilityContext context)
            {
                var owner = Owner(context);
                if (Waiting(context, owner) == null)
                    return Array.Empty<CardInstance>();
                var earlier = context.RosterState.TurnSlots
                    .Where(slot => slot.RoundIndex == context.RoundIndex &&
                        slot.TurnIndex < context.TurnIndex)
                    .SelectMany(slot => slot.Assignments)
                    .Where(assignment => assignment.HeroId.Equals(owner) &&
                        assignment.CardInstanceId.HasValue)
                    .Select(assignment => assignment.CardInstanceId.Value)
                    .ToHashSet();
                return context.CardState.Instances.Where(card =>
                        card.OwnerId.Equals(owner) && earlier.Contains(card.Id) &&
                        CardLifecycleAuthority.IsResolvedCardInTurnArea(card))
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal).ToArray();
            }
        }
    }
}
