using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Retrieval confined to cards that actually entered discard during the current turn.</summary>
    public static class EmmittTimeCapsuleContent
    {
        public const string OwnerParameter = "owner";
        public const string RetrievedCountOutput = "retrievedCount";

        public static EffectCapabilityReference RetrieveCurrentTurnDiscardsV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("emmitt-retrieve-turn-discards"), 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new RetrieveHandler();
        }

        private sealed class RetrieveHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => RetrieveCurrentTurnDiscardsV1;
            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(OwnerParameter, EffectValueKind.Hero) },
                new[] { new EffectOutputBinding(RetrievedCountOutput, EffectValueKind.Integer) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Length > 0
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var cards = Candidates(context);
                if (cards.Length == 0)
                    return EffectExecutionResult.ImpossibleMandatory();
                foreach (var card in cards)
                    if (!CardLifecycleAuthority.TryRetrieveEligibleCard(card))
                        throw new InvalidOperationException(
                            "An eligible current-turn discard changed state during retrieval.");
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(RetrievedCountOutput, EffectValue.Integer(cards.Length))
                });
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context,
                IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("Retrieving all eligible cards asks no card choice.");

            private static CardInstance[] Candidates(EffectCapabilityContext context)
            {
                var owner = context.Input(OwnerParameter);
                var history = context.RosterState.CurrentTurnCardHistory;
                if (!context.CardState.IsAvailable || history == null ||
                    owner.Kind != EffectValueKind.Hero || !owner.HeroId.IsDefined)
                    return Array.Empty<CardInstance>();
                return context.CardState.Instances
                    .Where(card => card.OwnerId.Equals(owner.HeroId) &&
                        CardLifecycleAuthority.IsDiscardedRestingCard(card) &&
                        history.WasDiscardedThisTurn(card.Id))
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal).ToArray();
            }
        }
    }
}
