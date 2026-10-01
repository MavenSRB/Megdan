using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 9.32b. Rulebook p.10's Swap a Card on a teammate's unresolved card, as Takahide the
    /// Warlord's Red faces print it: Set an Example and Lead from the Front, "A friendly hero in radius
    /// may swap their unresolved card with a card in their hand", and Hold my Saké, "... with a card in
    /// their hand, or in their discard".
    ///
    /// <para><b>Who chooses</b> (p.10 Making Choices, RO-Q2's reading): Takahide chooses the friendly
    /// hero; that hero decides whether to swap - the composition's <c>May</c>, answered on their seat -
    /// and with which card, which this capability asks of the owner's seat alone.</para>
    ///
    /// <para><b>What changes</b>: the round's <see cref="IUnresolvedCardSwap"/> exchanges both cards'
    /// states, this turn's slot and the revealed record, so the turn order reads the swapped-in card's
    /// Initiative (p.7) - the hero may now act before or after a hero they were behind. The owner must
    /// have an unresolved card in this turn's slot and must not be the hero acting now (whose card is
    /// resolving, not waiting).</para>
    ///
    /// <para>Unregistered until Takahide is dealt (9.32h); a fixture's catalog carries
    /// <see cref="Handlers"/>.</para>
    /// </summary>
    public static class SyntheticUnresolvedSwapCapabilities
    {
        public const string OwnerParameterName = "owner";
        public const string FromDiscardParameterName = "fromDiscard";
        public const string SwappedOutOutputName = "swappedOut";
        public const string SwappedInOutputName = "swappedIn";

        public static EffectCapabilityReference SwapUnresolvedCardV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("swap-unresolved-card"), 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new SwapUnresolvedCardHandler();
        }

        private sealed class SwapUnresolvedCardHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => SwapUnresolvedCardV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(OwnerParameterName, EffectValueKind.Hero),
                        new EffectCapabilityParameter(FromDiscardParameterName, EffectValueKind.Boolean)
                    },
                    new[]
                    {
                        new EffectOutputBinding(SwappedOutOutputName, EffectValueKind.CardInstance),
                        new EffectOutputBinding(SwappedInOutputName, EffectValueKind.CardInstance)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var unresolved = Unresolved(context);
                var partners = unresolved == null ? Array.Empty<CardInstance>() : Partners(context, unresolved);
                return partners.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.None,
                        partners.Select(card => EffectValue.Card(card.Id)),
                        1, 1, unresolved.OwnerId));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var unresolved = Unresolved(context);
                var chosen = unresolved != null && selected?.Count == 1 &&
                    selected[0].Kind == EffectValueKind.CardInstance
                    ? Partners(context, unresolved).SingleOrDefault(card =>
                        card.Id.Equals(selected[0].CardInstanceId))
                    : null;
                var swap = context.RosterState.UnresolvedCardSwap;
                if (chosen == null || swap == null ||
                    !swap.TrySwapUnresolvedCard(unresolved.OwnerId, unresolved.Id, chosen.Id))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(SwappedOutOutputName, EffectValue.Card(unresolved.Id)),
                    new EffectOutcomeFact(SwappedInOutputName, EffectValue.Card(chosen.Id))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var unresolved = Unresolved(context);
                return unresolved != null && Partners(context, unresolved).Count > 0
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }

            /// <summary>
            /// "Their unresolved card": the owner's card in this turn's slot, still waiting - played,
            /// faceup and unresolved - and never the acting hero's own, which is resolving.
            /// </summary>
            private static CardInstance Unresolved(EffectCapabilityContext context)
            {
                var owner = context.Input(OwnerParameterName);
                if (owner.Kind != EffectValueKind.Hero || !owner.HeroId.IsDefined ||
                    owner.HeroId.Equals(context.ActorId) ||
                    !context.CardState.IsAvailable || !context.RosterState.IsAvailable)
                {
                    return null;
                }

                var slotCard = context.RosterState.TurnSlots
                    .Where(slot => slot.RoundIndex == context.RoundIndex && slot.TurnIndex == context.TurnIndex)
                    .SelectMany(slot => slot.Assignments)
                    .Where(assignment => assignment.HeroId.Equals(owner.HeroId) && assignment.CardInstanceId.HasValue)
                    .Select(assignment => assignment.CardInstanceId.Value)
                    .FirstOrDefault();
                if (!slotCard.IsDefined)
                {
                    return null;
                }

                var card = context.CardState.Instances.SingleOrDefault(value => value.Id.Equals(slotCard));
                return card != null && card.OwnerId.Equals(owner.HeroId) &&
                    CardLifecycleAuthority.IsResolvingCardInTurnArea(card)
                    ? card
                    : null;
            }

            private static IReadOnlyList<CardInstance> Partners(
                EffectCapabilityContext context, CardInstance unresolved)
            {
                var admitsDiscarded = context.Input(FromDiscardParameterName).BooleanValue;
                return context.CardState.Instances
                    .Where(card => card.OwnerId.Equals(unresolved.OwnerId) &&
                        !card.Id.Equals(unresolved.Id) &&
                        CardLifecycleAuthority.IsUnresolvedSwapPartner(card, admitsDiscarded))
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                    .ToArray();
            }
        }
    }
}
