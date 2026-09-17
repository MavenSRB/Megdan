using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>A text-named own discarded card grants its primary without selecting another card.
    /// Optionality belongs to the enclosing Optional node. This never changes the source's state.</summary>
    public static class SyntheticNamedBorrowCapabilities
    {
        public static EffectCapabilityReference BorrowNamedDiscardedPrimaryV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("borrow-named-discarded-primary"), 1);
        public const string CardParameter = "card";
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            new IEffectCapabilityHandler[] { new Handler() };

        private sealed class Handler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => BorrowNamedDiscardedPrimaryV1;
            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(CardParameter, EffectValueKind.CardInstance) },
                new[] { new EffectOutputBinding(SyntheticBorrowedActionCapabilities.BorrowedCardOutput,
                    EffectValueKind.CardInstance) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            private static CardInstance Candidate(EffectCapabilityContext context)
            {
                var named = context.Input(CardParameter);
                if (!context.CardState.IsAvailable || named.Kind != EffectValueKind.CardInstance)
                    return null;
                return BorrowedPrimaryActionQuery.EligibleCards(
                    BorrowedPrimaryActionDeclaration.FromDiscardedCards(CardColor.None),
                    context.ActorId, context.CardState.Definitions, context.CardState.Instances,
                    context.Timing?.Effects, context.Capabilities).SingleOrDefault(card =>
                        card.Id.Equals(named.CardInstanceId));
            }
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var card = Candidate(context);
                return card == null ? EffectExecutionResult.ImpossibleMandatory() :
                    EffectExecutionResult.Applied(new[] { new EffectOutcomeFact(
                        SyntheticBorrowedActionCapabilities.BorrowedCardOutput, EffectValue.Card(card.Id)) });
            }
            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidate(context) == null ? EffectExecutionResultKind.ImpossibleMandatory : EffectExecutionResultKind.Applied;
        }
    }
}
