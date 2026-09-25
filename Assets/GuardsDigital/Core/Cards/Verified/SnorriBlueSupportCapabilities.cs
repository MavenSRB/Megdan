using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Card-zone operations printed on Snorri's Ancestral Blue Skills.</summary>
    public static class SnorriBlueSupportCapabilities
    {
        public static EffectCapabilityReference RetrieveAllDiscardedV1 { get; } =
            new EffectCapabilityReference(
                new EffectCapabilityId("retrieve-all-owned-discarded-cards"), 1);
        public static EffectCapabilityReference ChooseResolvedCardV1 { get; } =
            new EffectCapabilityReference(
                new EffectCapabilityId("choose-owned-resolved-turn-card"), 1);
        public static EffectCapabilityReference SwapResolvedWithHandV1 { get; } =
            new EffectCapabilityReference(
                new EffectCapabilityId("swap-owned-resolved-card-with-hand"), 1);
        public static EffectCapabilityReference ChooseOwnedItemV1 { get; } =
            new EffectCapabilityReference(
                new EffectCapabilityId("choose-owned-swappable-item"), 1);
        public static EffectCapabilityReference SwapItemWithCardV1 { get; } =
            new EffectCapabilityReference(
                new EffectCapabilityId("swap-owned-item-with-matching-card"), 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new RetrieveAllDiscardedHandler();
            yield return new ChooseResolvedCardHandler();
            yield return new SwapResolvedWithHandHandler();
            yield return new ChooseOwnedItemHandler();
            yield return new SwapItemWithCardHandler();
        }

        private static bool ValidOwner(EffectCapabilityContext context, out HeroId owner)
        {
            var value = context.Input("owner");
            owner = value.Kind == EffectValueKind.Hero ? value.HeroId : default;
            if (!context.CardState.IsAvailable || !context.RosterState.IsAvailable ||
                !owner.IsDefined)
                return false;
            var selectedOwner = owner;
            return context.Participants.Heroes.Any(hero => hero.Id.Equals(selectedOwner));
        }

        private static IReadOnlyList<CardInstance> HandCards(
            EffectCapabilityContext context, HeroId owner) =>
            context.CardState.Instances
                .Where(card => card.OwnerId.Equals(owner) &&
                    card.State.Location == CardLocation.Hand &&
                    !card.State.HasBeenPlayed &&
                    card.State.ResolutionStatus == CardResolutionStatus.None)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();

        private static IReadOnlyList<CardInstance> ResolvedCards(
            EffectCapabilityContext context, HeroId owner)
        {
            if (HandCards(context, owner).Count == 0 ||
                context.RosterState.ResolvedTurnSlotSwap == null)
                return Array.Empty<CardInstance>();
            var recorded = context.RosterState.TurnSlots
                .Where(slot => slot.RoundIndex == context.RoundIndex)
                .SelectMany(slot => slot.Assignments)
                .Where(assignment => assignment.HeroId.Equals(owner) &&
                    assignment.CardInstanceId.HasValue)
                .Select(assignment => assignment.CardInstanceId.Value)
                .ToHashSet();
            return context.CardState.Instances
                .Where(card => card.OwnerId.Equals(owner) && recorded.Contains(card.Id) &&
                    CardLifecycleAuthority.IsResolvedCardInTurnArea(card))
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private sealed class ChooseResolvedCardHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ChooseResolvedCardV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter("owner", EffectValueKind.Hero) },
                    new[] { new EffectOutputBinding("resolved", EffectValueKind.CardInstance) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<CardInstance> Candidates(EffectCapabilityContext context) =>
                ValidOwner(context, out var owner)
                    ? ResolvedCards(context, owner)
                    : Array.Empty<CardInstance>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.None,
                        candidates.Select(card => EffectValue.Card(card.Id)),
                        1, 1, context.Input("owner").HeroId));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var chosen = selected?.Count == 1 &&
                    selected[0].Kind == EffectValueKind.CardInstance
                    ? Candidates(context).SingleOrDefault(card =>
                        card.Id.Equals(selected[0].CardInstanceId))
                    : null;
                return chosen == null
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("resolved", EffectValue.Card(chosen.Id))
                    });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count > 0
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class SwapResolvedWithHandHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => SwapResolvedWithHandV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("owner", EffectValueKind.Hero),
                        new EffectCapabilityParameter("resolved", EffectValueKind.CardInstance)
                    },
                    new[] { new EffectOutputBinding("replacement", EffectValueKind.CardInstance) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<CardInstance> Candidates(EffectCapabilityContext context)
            {
                if (!ValidOwner(context, out var owner) ||
                    context.Input("resolved").Kind != EffectValueKind.CardInstance ||
                    !ResolvedCards(context, owner).Any(card =>
                        card.Id.Equals(context.Input("resolved").CardInstanceId)))
                    return Array.Empty<CardInstance>();
                return HandCards(context, owner);
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.None,
                        candidates.Select(card => EffectValue.Card(card.Id)),
                        1, 1, context.Input("owner").HeroId));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var chosen = selected?.Count == 1 &&
                    selected[0].Kind == EffectValueKind.CardInstance
                    ? Candidates(context).SingleOrDefault(card =>
                        card.Id.Equals(selected[0].CardInstanceId))
                    : null;
                return chosen == null ||
                    !context.RosterState.ResolvedTurnSlotSwap.TrySwapResolvedWithHand(
                        context.Input("owner").HeroId,
                        context.Input("resolved").CardInstanceId, chosen.Id)
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("replacement", EffectValue.Card(chosen.Id))
                    });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count > 0
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private static IReadOnlyList<CardInstance> ItemCandidates(
            EffectCapabilityContext context, HeroId owner)
        {
            if (context.HeroItems == null || context.RosterState.ItemCardSwap == null)
                return Array.Empty<CardInstance>();
            var items = context.HeroItems.GetItems(owner)
                .Select(item => item.SourceCardInstanceId).ToHashSet();
            return context.CardState.Instances
                .Where(card => items.Contains(card.Id) &&
                    context.CardState.Instances.Any(replacement =>
                        context.HeroItems.CanSwapItemWithCard(
                            card, replacement, context.CardState.Definitions)))
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private sealed class ChooseOwnedItemHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ChooseOwnedItemV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter("owner", EffectValueKind.Hero) },
                    new[] { new EffectOutputBinding("item", EffectValueKind.CardInstance) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<CardInstance> Candidates(EffectCapabilityContext context) =>
                ValidOwner(context, out var owner)
                    ? ItemCandidates(context, owner)
                    : Array.Empty<CardInstance>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.None,
                        candidates.Select(card => EffectValue.Card(card.Id)),
                        1, 1, context.Input("owner").HeroId));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var chosen = selected?.Count == 1 &&
                    selected[0].Kind == EffectValueKind.CardInstance
                    ? Candidates(context).SingleOrDefault(card =>
                        card.Id.Equals(selected[0].CardInstanceId))
                    : null;
                return chosen == null
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("item", EffectValue.Card(chosen.Id))
                    });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count > 0
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class SwapItemWithCardHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => SwapItemWithCardV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("owner", EffectValueKind.Hero),
                        new EffectCapabilityParameter("item", EffectValueKind.CardInstance)
                    },
                    new[] { new EffectOutputBinding("replacement", EffectValueKind.CardInstance) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<CardInstance> Candidates(EffectCapabilityContext context)
            {
                if (!ValidOwner(context, out var owner) || context.HeroItems == null ||
                    context.RosterState.ItemCardSwap == null ||
                    context.Input("item").Kind != EffectValueKind.CardInstance)
                    return Array.Empty<CardInstance>();
                var item = ItemCandidates(context, owner).SingleOrDefault(card =>
                    card.Id.Equals(context.Input("item").CardInstanceId));
                return item == null
                    ? Array.Empty<CardInstance>()
                    : context.CardState.Instances
                        .Where(card => context.HeroItems.CanSwapItemWithCard(
                            item, card, context.CardState.Definitions))
                        .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                        .ToArray();
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.None,
                        candidates.Select(card => EffectValue.Card(card.Id)),
                        1, 1, context.Input("owner").HeroId));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var chosen = selected?.Count == 1 &&
                    selected[0].Kind == EffectValueKind.CardInstance
                    ? Candidates(context).SingleOrDefault(card =>
                        card.Id.Equals(selected[0].CardInstanceId))
                    : null;
                return chosen == null ||
                    !context.RosterState.ItemCardSwap.TrySwapItemWithCard(
                        context.Input("owner").HeroId,
                        context.Input("item").CardInstanceId, chosen.Id)
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("replacement", EffectValue.Card(chosen.Id))
                    });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count > 0
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class RetrieveAllDiscardedHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => RetrieveAllDiscardedV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter("owner", EffectValueKind.Hero) },
                    new[] { new EffectOutputBinding("retrievedCount", EffectValueKind.Integer) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<CardInstance> Candidates(EffectCapabilityContext context)
            {
                var owner = context.Input("owner");
                if (!context.CardState.IsAvailable || owner.Kind != EffectValueKind.Hero ||
                    !owner.HeroId.IsDefined)
                    return Array.Empty<CardInstance>();
                return context.CardState.Instances
                    .Where(card => card.OwnerId.Equals(owner.HeroId) &&
                        CardLifecycleAuthority.IsDiscardedRestingCard(card))
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                    .ToArray();
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var cards = Candidates(context);
                if (cards.Count == 0)
                    return EffectExecutionResult.ImpossibleMandatory();
                foreach (var card in cards)
                {
                    if (!CardLifecycleAuthority.TryRetrieveEligibleCard(card))
                        throw new InvalidOperationException(
                            "A prevalidated discarded-card retrieval changed state mid-resolution.");
                }
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("retrievedCount", EffectValue.Integer(cards.Count))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("Retrieve all does not request a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count > 0
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }
    }
}
