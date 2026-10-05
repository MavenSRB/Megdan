using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Selects one printed action on the chosen enemy hero's previous-slot card. The output is
    /// an index into trusted definition order, not a seat-supplied action ID; the borrowed-action
    /// authority resolves and revalidates it before starting the instance.
    /// </summary>
    public static class SyntheticPreviousSlotBorrowCapabilities
    {
        public static EffectCapabilityReference BorrowPrintedActionV1 { get; } =
            new EffectCapabilityReference(
                new EffectCapabilityId("borrow-previous-slot-printed-action"), 1);

        public const string EnemyParameter = "enemy";
        public const string SubstituteTokenKindParameter = "substituteTokenKind";
        public const string BorrowedActionIndexOutput = "borrowedActionIndex";

        public static IEffectCapabilityHandler Handler() => new HandlerImpl();

        public static IReadOnlyList<PreviousSlotBorrowedAction> Candidates(
            EffectCapabilityContext context)
        {
            if (context == null || !context.RosterState.IsAvailable ||
                !context.CardState.IsAvailable ||
                context.Input(EnemyParameter).Kind != EffectValueKind.Hero)
                return Array.Empty<PreviousSlotBorrowedAction>();

            return PreviousSlotBorrowedActionQuery.Eligible(
                context.ActorId, context.Input(EnemyParameter).HeroId,
                context.RoundIndex, context.TurnIndex,
                context.Participants, context.RosterState.TurnSlots,
                context.CardState.Definitions, context.CardState.Instances);
        }

        private sealed class HandlerImpl : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => BorrowPrintedActionV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(EnemyParameter, EffectValueKind.Hero),
                        new EffectCapabilityParameter(
                            SubstituteTokenKindParameter, EffectValueKind.TokenKind)
                    },
                    new[]
                    {
                        new EffectOutputBinding(
                            SyntheticBorrowedActionCapabilities.BorrowedCardOutput,
                            EffectValueKind.CardInstance),
                        new EffectOutputBinding(BorrowedActionIndexOutput, EffectValueKind.Integer)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor,
                        candidates.Select(candidate => EffectValue.Integer(candidate.PrintedIndex)),
                        1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected?.Count != 1 || selected[0].Kind != EffectValueKind.Integer)
                    return EffectExecutionResult.ImpossibleMandatory();
                var candidate = Candidates(context).SingleOrDefault(action =>
                    action.PrintedIndex == selected[0].IntegerValue);
                if (!candidate.CardId.IsDefined)
                    return EffectExecutionResult.ImpossibleMandatory();

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        SyntheticBorrowedActionCapabilities.BorrowedCardOutput,
                        EffectValue.Card(candidate.CardId)),
                    new EffectOutcomeFact(
                        BorrowedActionIndexOutput,
                        EffectValue.Integer(candidate.PrintedIndex))
                });
            }
        }
    }
}
