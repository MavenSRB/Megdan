using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Pve.Composition
{
    /// <summary>PVE-D37: nearest Blue unit first, then straight-line blocked movement.</summary>
    public static class PveHookContent
    {
        public const string Text = "Move the closest Blue unit toward this unit in a straight line until adjacent. Stop at obstacles. If the closest unit is not in a straight line, do nothing.";
        public static readonly EffectNodeId SelectionNode = new EffectNodeId("pve-hook-closest-unit");
        public static readonly EffectCapabilityReference Selection = new EffectCapabilityReference(new EffectCapabilityId("pve-closest-blue-pull"), 1);
        public static IEffectCapabilityHandler Handler() => new ClosestUnitHandler();

        public static EffectDefinition Effect()
        {
            var root = new EffectNodeId("hook");
            var move = new EffectNodeId("hook-move");
            return new EffectDefinition(new EffectDefinitionId("pve-hook"), PveCardFactory.ContentRevision, root, new[]
            {
                new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: new[] { SelectionNode, move }),
                new EffectNodeDefinition(SelectionNode, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory, Selection,
                    outputs: new[] { new EffectOutputBinding("mover", EffectValueKind.Coordinate),
                        new EffectOutputBinding("destination", EffectValueKind.Coordinate), new EffectOutputBinding("distance", EffectValueKind.Integer) }),
                new EffectNodeDefinition(move, EffectNodeKind.Capability, EffectFailurePolicy.IfAble, SyntheticDisplacementCapabilities.DirectMoveV3,
                    inputs: new[] {
                        EffectInputBinding.FromNodeOutput("mover", SelectionNode, "mover", EffectValueKind.Coordinate),
                        EffectInputBinding.FromNodeOutput("distance", SelectionNode, "distance", EffectValueKind.Integer),
                        EffectInputBinding.FromNodeOutput("destination", SelectionNode, "destination", EffectValueKind.Coordinate),
                        EffectInputBinding.FromClosedVocabulary("destinationPolicy", DirectMoveDestinationPolicy.NamedSpace) })
            });
        }

        private sealed class ClosestUnitHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => Selection;
            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(outputs: new[] {
                new EffectOutputBinding("mover", EffectValueKind.Coordinate), new EffectOutputBinding("destination", EffectValueKind.Coordinate),
                new EffectOutputBinding("distance", EffectValueKind.Integer) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions => Array.Empty<ActiveEffectDefinition>();
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Closest(context);
                return candidates.Count == 0 ? EffectExecutionResult.ImpossibleMandatory() : candidates.Count == 1
                    ? Resolve(context, candidates[0])
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(EffectChooserRole.Actor, candidates.Select(EffectValue.Space), 1, 1));
            }
            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                selected != null && selected.Count == 1 && selected[0].Kind == EffectValueKind.Coordinate && Closest(context).Contains(selected[0].Coordinate)
                    ? Resolve(context, selected[0].Coordinate) : EffectExecutionResult.ImpossibleMandatory();
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) => Closest(context).Count == 0
                ? EffectExecutionResultKind.ImpossibleMandatory : Closest(context).Count == 1
                    ? EffectExecutionResultKind.Applied : EffectExecutionResultKind.NeedsChoice;

            private static IReadOnlyList<HexCoordinate> Closest(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                if (board == null || !board.TryGetObjectCoordinate(BoardObjectReference.ForHero(context.ActorId), out var origin))
                    return Array.Empty<HexCoordinate>();
                var units = board.Occupancies.Where(hero => context.Participants.GetTeamForHero(hero.HeroId).Side == TeamSide.Blue)
                    .Select(hero => hero.Coordinate).Concat(board.Minions.Where(minion => minion.TeamSide == TeamSide.Blue).Select(minion => minion.Coordinate))
                    .Distinct().OrderBy(at => at.Q).ThenBy(at => at.R).ToArray();
                if (units.Length == 0) return units;
                var distance = units.Min(at => HexDistanceQuery.Calculate(origin, at));
                return units.Where(at => HexDistanceQuery.Calculate(origin, at) == distance).ToArray();
            }

            private static EffectExecutionResult Resolve(EffectCapabilityContext context, HexCoordinate from)
            {
                var board = context.Components.Board;
                if (!board.TryGetObjectAt(from, out var mover) || !board.TryGetObjectCoordinate(BoardObjectReference.ForHero(context.ActorId), out var origin))
                    return EffectExecutionResult.ImpossibleMandatory();
                var destination = from;
                var distance = 0;
                if (StraightLineQuery.TryGetDirection(from, origin, out var direction, out var separation))
                {
                    var reachable = board.FindDirectMoveDestinations(mover, Math.Max(0, separation - 1), context.Modifications, false);
                    for (var step = 1; step < separation; step++)
                    {
                        var next = StraightLineQuery.Step(from, direction, step);
                        if (board.TryGetObjectAt(next, out _) ||
                            !board.MapDefinition.Spaces.Any(space => space.Coordinate.Equals(next) && space.Kind != BoardMapSpaceKind.Terrain) ||
                            !reachable.Any(route => route.Destination.Equals(next) && route.Cost == step)) break;
                        destination = next;
                        distance = step;
                    }
                }
                // The shared direct-Move consumer rechecks immunity, restrictions, route and occupancy.
                return EffectExecutionResult.Applied(new[] {
                    new EffectOutcomeFact("mover", EffectValue.Space(from)), new EffectOutcomeFact("destination", EffectValue.Space(destination)),
                    new EffectOutcomeFact("distance", EffectValue.Integer(distance)) });
            }
        }
    }
}
