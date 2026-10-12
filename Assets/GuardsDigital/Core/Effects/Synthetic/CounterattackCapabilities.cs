using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>A typed request for the source card's declared adjacent counterattack.</summary>
    public static class CounterattackCapabilities
    {
        public static readonly EffectCapabilityReference RequestV1 = new EffectCapabilityReference(new EffectCapabilityId("request-adjacent-counterattack"), 1);
        public const string ActionIndexOutput = "counterattackActionIndex";
        public const string GrantId = "counterattack-reaction";
        public static IEffectCapabilityHandler Handler() => new RequestHandler();

        private sealed class RequestHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => RequestV1;
            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(outputs: new[] {
                new EffectOutputBinding(SyntheticBorrowedActionCapabilities.BorrowedCardOutput, EffectValueKind.CardInstance),
                new EffectOutputBinding(ActionIndexOutput, EffectValueKind.Integer) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions => Array.Empty<ActiveEffectDefinition>();
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var attacker = context.EnclosingAttack.ActorId;
                if (context.EnclosingActionKind != ActionKind.Defense ||
                    !context.ActorId.Equals(context.EnclosingAttack.HeroTargetId) ||
                    !context.SourceCardInstanceId.Equals(context.EnclosingAttack.DefenseCardInstanceId))
                    return EffectExecutionResult.ImpossibleMandatory();
                if (board == null || !attacker.IsDefined || !board.TryGetObjectCoordinate(BoardObjectReference.ForHero(attacker), out var from) ||
                    !board.TryGetObjectCoordinate(BoardObjectReference.ForHero(context.ActorId), out var to) || HexDistanceQuery.Calculate(from, to) != 1)
                    return EffectExecutionResult.Applied();
                var definition = context.CardState.Definitions.Single(value => value.Reference.Equals(context.SourceDefinitionReference));
                var actions = definition.ActionDefinitions.ToArray();
                var index = Array.FindIndex(actions, action => action.Origin == ActionDefinitionOrigin.TextGranted &&
                    action.Kind == ActionKind.Attack && action.AttackRange == AttackRangeKind.NonRanged);
                if (index < 0) return EffectExecutionResult.ImpossibleMandatory();
                return EffectExecutionResult.Applied(new[] {
                    new EffectOutcomeFact(SyntheticBorrowedActionCapabilities.BorrowedCardOutput, EffectValue.Card(context.SourceCardInstanceId)),
                    new EffectOutcomeFact(ActionIndexOutput, EffectValue.Integer(index)) });
            }
            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) => EffectExecutionResult.ImpossibleMandatory();
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) => EffectExecutionResultKind.Applied;
        }
    }
}
