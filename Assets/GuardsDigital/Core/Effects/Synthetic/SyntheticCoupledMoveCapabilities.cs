using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>WI-Q1: two named objects move one common distance and direction, or neither moves.</summary>
    public static class SyntheticCoupledMoveCapabilities
    {
        public static EffectCapabilityReference MoveV1 => new EffectCapabilityReference(new EffectCapabilityId("coupled-direct-move"), 1);
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() => new[] { new CoupledMoveHandler() };

        private sealed class CoupledMoveHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => MoveV1;
            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(new[]
            {
                new EffectCapabilityParameter("first", EffectValueKind.Coordinate),
                new EffectCapabilityParameter("second", EffectValueKind.Coordinate),
                new EffectCapabilityParameter("leastDistance", EffectValueKind.Integer),
                new EffectCapabilityParameter("distance", EffectValueKind.Integer),
                new EffectCapabilityParameter("ignoresObstacles", EffectValueKind.Boolean)
            }, new[]
            {
                new EffectOutputBinding("firstAt", EffectValueKind.Coordinate),
                new EffectOutputBinding("secondAt", EffectValueKind.Coordinate),
                new EffectOutputBinding("distance", EffectValueKind.Integer)
            });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } = Array.Empty<ActiveEffectDefinition>();
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0 ? EffectExecutionResult.ImpossibleMandatory() :
                    EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(EffectChooserRole.Actor,
                        candidates.Select(EffectValue.Space).ToArray(), 1, 1));
            }
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0 ? EffectExecutionResultKind.ImpossibleMandatory : EffectExecutionResultKind.NeedsChoice;
            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 || selected[0].Kind != EffectValueKind.Coordinate ||
                    !Candidates(context).Contains(selected[0].Coordinate) || !TryPair(context, out var first, out var second))
                    return EffectExecutionResult.ImpossibleMandatory();
                var firstAt = context.Input("first").Coordinate;
                var secondAt = context.Input("second").Coordinate;
                var destination = selected[0].Coordinate;
                if (!StraightLineQuery.TryGetDirection(firstAt, destination, out var direction, out var distance))
                    return EffectExecutionResult.ImpossibleMandatory();
                if (!context.Components.Board.TryMoveCoupled(first, second, direction, distance,
                    context.Modifications, context.Input("ignoresObstacles").BooleanValue))
                    return EffectExecutionResult.ImpossibleMandatory();
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("firstAt", EffectValue.Space(destination)),
                    new EffectOutcomeFact("secondAt", EffectValue.Space(StraightLineQuery.Step(secondAt, direction, distance))),
                    new EffectOutcomeFact("distance", EffectValue.Integer(distance))
                }.Where(f => context.Publishes(f.OutputName)));
            }
            private static IReadOnlyList<HexCoordinate> Candidates(EffectCapabilityContext context)
            {
                var least = context.Input("leastDistance").IntegerValue;
                var maximum = context.Input("distance").IntegerValue;
                if (least < 1 || maximum < least || !TryPair(context, out var first, out var second))
                    return Array.Empty<HexCoordinate>();
                var firstAt = context.Input("first").Coordinate;
                var choices = new List<HexCoordinate>();
                foreach (HexDirection direction in Enum.GetValues(typeof(HexDirection)))
                    for (var distance = least; distance <= maximum; distance++)
                        if (context.Components.Board.CanMoveCoupled(first, second, direction, distance,
                            context.Modifications, context.Input("ignoresObstacles").BooleanValue))
                            choices.Add(StraightLineQuery.Step(firstAt, direction, distance));
                return choices.OrderBy(p => p.Q).ThenBy(p => p.R).ToArray();
            }
            private static bool TryPair(EffectCapabilityContext context, out BoardObjectReference first, out BoardObjectReference second)
            {
                first = default; second = default;
                var board = context.Components?.Board;
                if (board == null || context.Input("first").Kind != EffectValueKind.Coordinate ||
                    context.Input("second").Kind != EffectValueKind.Coordinate ||
                    !board.TryGetObjectAt(context.Input("first").Coordinate, out first) ||
                    !board.TryGetObjectAt(context.Input("second").Coordinate, out second) || first.Equals(second)) return false;
                foreach (var target in new[] { first, second })
                    if (target.AsRuleObject().IsDefined &&
                        (ImmunityQuery.IsImmuneTo(target.AsRuleObject(), context.EnclosingActivity,
                            RuleObject.ForHero(context.ActorId), board, context.Participants, context.Modifications, context.ImmunityOverrides) ||
                         ImmunityQuery.IsImmuneTo(target.AsRuleObject(), RuleActivity.DirectMove,
                            RuleObject.ForHero(context.ActorId), board, context.Participants, context.Modifications, context.ImmunityOverrides)))
                        return false;
                return true;
            }
        }
    }
}
