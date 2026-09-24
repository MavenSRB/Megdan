using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// A singular, persistent obstacle miniature. Each handler binds its kind in trusted content;
    /// actor identity selects that kind's one physical piece without reading card names or text.
    /// Placement is not movement, and none of these handlers creates a token or terrain source.
    /// </summary>
    public static class SyntheticObstacleCapabilities
    {
        public const string SpaceOutput = "space";
        public const string PlacedOutput = "placed";
        public const string RemovedOutput = "removed";

        public static IEffectCapabilityHandler SpaceHandler(
            EffectCapabilityReference reference, string obstacleKind) =>
            new ObstacleSpaceHandler(reference, obstacleKind);

        public static IEffectCapabilityHandler PlaceAdjacentHandler(
            EffectCapabilityReference reference, string obstacleKind) =>
            new PlaceAdjacentObstacleHandler(reference, obstacleKind);

        public static IEffectCapabilityHandler RemoveHandler(
            EffectCapabilityReference reference, string obstacleKind) =>
            new RemoveObstacleHandler(reference, obstacleKind);

        private static ObstacleId IdFor(string obstacleKind, EffectCapabilityContext context) =>
            OwnedObstacleIdentity.For(obstacleKind, context.ActorId);

        private abstract class ObstacleHandler : IEffectCapabilityHandler
        {
            protected ObstacleHandler(EffectCapabilityReference reference, string obstacleKind)
            {
                if (string.IsNullOrWhiteSpace(obstacleKind))
                    throw new ArgumentException("Obstacle kind is required.", nameof(obstacleKind));
                Reference = reference;
                ObstacleKind = obstacleKind;
            }

            public EffectCapabilityReference Reference { get; }
            protected string ObstacleKind { get; }
            public abstract EffectCapabilityParameterSchema Schema { get; }
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            public abstract EffectExecutionResult Execute(EffectCapabilityContext context);
            public abstract EffectExecutionResultKind Anticipate(EffectCapabilityContext context);
            public abstract EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected);
        }

        private sealed class ObstacleSpaceHandler : ObstacleHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            public ObstacleSpaceHandler(EffectCapabilityReference reference, string kind)
                : base(reference, kind) { }

            public override EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(outputs: new[]
                {
                    new EffectOutputBinding(SpaceOutput, EffectValueKind.Coordinate)
                });

            public bool PublishesWithoutApplying => true;

            public override EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var facts = Observe(context);
                return facts.Count == 0 ? EffectExecutionResult.ImpossibleMandatory() :
                    EffectExecutionResult.Applied(facts);
            }

            public override EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("An obstacle-position fact requests no choice.");

            public override EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Observe(context).Count == 0 ? EffectExecutionResultKind.ImpossibleMandatory :
                    EffectExecutionResultKind.Applied;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) => Observe(context);

            private IReadOnlyList<EffectOutcomeFact> Observe(EffectCapabilityContext context)
            {
                if (context.Components?.Obstacles == null ||
                    !context.Components.Obstacles.TryGetObstacleCoordinate(
                        IdFor(ObstacleKind, context), out var at))
                    return Array.Empty<EffectOutcomeFact>();
                return new[] { new EffectOutcomeFact(SpaceOutput, EffectValue.Space(at)) };
            }
        }

        private sealed class PlaceAdjacentObstacleHandler : ObstacleHandler
        {
            public PlaceAdjacentObstacleHandler(EffectCapabilityReference reference, string kind)
                : base(reference, kind) { }

            public override EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(outputs: new[]
                {
                    new EffectOutputBinding(PlacedOutput, EffectValueKind.Coordinate)
                });

            public override EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var spaces = LegalSpaces(context).Select(EffectValue.Space).ToArray();
                return spaces.Length == 0 ? EffectExecutionResult.ImpossibleMandatory() :
                    EffectExecutionResult.NeedsChoice(
                        new EffectChoiceRequest(EffectChooserRole.Actor, spaces, 1, 1));
            }

            public override EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                LegalSpaces(context).Count == 0 ? EffectExecutionResultKind.ImpossibleMandatory :
                    EffectExecutionResultKind.NeedsChoice;

            public override EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate)
                    return EffectExecutionResult.ImpossibleMandatory();
                var at = selected[0].Coordinate;
                if (!LegalSpaces(context).Contains(at) ||
                    !context.Components.Obstacles.TryPlaceOrRepositionObstacle(
                        IdFor(ObstacleKind, context), at))
                    return EffectExecutionResult.ImpossibleMandatory();
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(PlacedOutput, EffectValue.Space(at))
                });
            }

            private IReadOnlyList<HexCoordinate> LegalSpaces(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components?.Obstacles == null || components.Tokens == null ||
                    !components.Tokens.TryGetHeroCoordinate(context.ActorId, out var actorAt))
                    return Array.Empty<HexCoordinate>();
                return actorAt.GetNeighbors()
                    .Where(space => !ObstacleQuery.BlocksPlacement(
                        components.Tokens, space, context.Modifications))
                    .ToArray();
            }
        }

        private sealed class RemoveObstacleHandler : ObstacleHandler
        {
            public RemoveObstacleHandler(EffectCapabilityReference reference, string kind)
                : base(reference, kind) { }

            public override EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(outputs: new[]
                {
                    new EffectOutputBinding(RemovedOutput, EffectValueKind.Coordinate)
                });

            public override EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var obstacles = context.Components?.Obstacles;
                var id = IdFor(ObstacleKind, context);
                if (obstacles == null || !obstacles.TryGetObstacleCoordinate(id, out var at) ||
                    !obstacles.TryRemoveObstacle(id))
                    return EffectExecutionResult.ImpossibleMandatory();
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(RemovedOutput, EffectValue.Space(at))
                });
            }

            public override EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context.Components?.Obstacles != null &&
                context.Components.Obstacles.TryGetObstacleCoordinate(
                    IdFor(ObstacleKind, context), out _)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            public override EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("Obstacle removal requests no choice.");
        }
    }
}
