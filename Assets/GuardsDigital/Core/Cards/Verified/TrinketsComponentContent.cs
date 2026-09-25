using System.Collections.Generic;
using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Registered physical components of Trinkets.</summary>
    public static class TrinketsComponentContent
    {
        public const string TurretKind = "trinkets-turret";

        public static EffectCapabilityReference TurretSpaceV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-turret-space"), 1);

        public static EffectCapabilityReference PlaceTurretAdjacentV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-place-turret-adjacent"), 1);

        public static EffectCapabilityReference RemoveTurretV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-remove-turret"), 1);

        public static EffectCapabilityReference ActorInTurretRadiusV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-actor-in-turret-radius"), 1);

        public static EffectCapabilityReference TurretRadiusObjectV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-turret-radius-object"), 1);

        public static EffectCapabilityReference TurretRadiusEnemyHeroV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-turret-radius-hero"), 1);

        public static EffectCapabilityReference PlaceSelfInTurretRadiusV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-place-self-turret-radius"), 1);

        public static EffectCapabilityReference DualRangeTurretUnitV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-dual-range-turret-unit"), 1);

        public static EffectCapabilityReference AdjacentTurretUnitV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-adjacent-turret-unit"), 1);

        public static IEnumerable<IEffectCapabilityHandler> ObstacleHandlers()
        {
            yield return SyntheticObstacleCapabilities.SpaceHandler(TurretSpaceV1, TurretKind);
            yield return SyntheticObstacleCapabilities.PlaceAdjacentHandler(
                PlaceTurretAdjacentV1, TurretKind);
            yield return SyntheticObstacleCapabilities.RemoveHandler(RemoveTurretV1, TurretKind);
            yield return new ActorInTurretRadiusHandler();
            yield return SyntheticTargetingCapabilities.OwnedObstacleRadiusHandler(
                TurretRadiusObjectV1, TurretKind, false);
            yield return SyntheticTargetingCapabilities.OwnedObstacleRadiusHandler(
                TurretRadiusEnemyHeroV1, TurretKind, true);
            yield return SyntheticPlacementCapabilities.OwnedObstacleRadiusPlaceHandler(
                PlaceSelfInTurretRadiusV1, TurretKind);
            yield return SyntheticTargetingCapabilities.OwnedObstacleRangeUnitHandler(
                DualRangeTurretUnitV1, TurretKind, true);
            yield return SyntheticTargetingCapabilities.OwnedObstacleRangeUnitHandler(
                AdjacentTurretUnitV1, TurretKind, false);
        }

        public static TokenDefinitionId BarrierId { get; } =
            new TokenDefinitionId("trinkets-barrier");

        public static EffectCapabilityReference PlaceFirstBarrierV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-place-first-barrier"), 1);

        public static EffectCapabilityReference PlaceBarrierV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-place-barrier"), 1);

        public static EffectCapabilityReference ReclaimBarrierV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-reclaim-barrier"), 1);

        public static TokenDefinitionReference BarrierV1 =>
            new TokenDefinitionReference(BarrierId, 1);

        public static TokenDefinition Barrier { get; } = new TokenDefinition(
            BarrierId,
            1,
            "Barrier",
            3,
            new[]
            {
                new TokenPropertyDefinition(
                    "adjacent-friendly-defense",
                    "printed-defense-bonus",
                    "while-barrier-remains-placed",
                    RuleModification.ForValue(PrintedValueChannel.Defense, 1),
                    TokenPropertyAffectedObject.FriendlyHeroesAdjacentToTheToken)
            },
            TokenRemovalCondition.EndOfRoundCleanup);

        public static IEnumerable<TokenDefinition> Tokens()
        {
            yield return Barrier;
        }

        public static IEnumerable<IEffectCapabilityHandler> BarrierHandlers()
        {
            yield return SyntheticTokenCapabilities.PlaceHandler(
                PlaceFirstBarrierV1, Barrier, PrintedValueChannel.Radius,
                adjacentToObstacleKind: TurretKind);
            yield return SyntheticTokenCapabilities.PlaceHandler(
                PlaceBarrierV1, Barrier, PrintedValueChannel.Radius);
            yield return SyntheticTokenCapabilities.ReclaimHandler(ReclaimBarrierV1, Barrier);
        }

        private sealed class ActorInTurretRadiusHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ActorInTurretRadiusV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter("radius", EffectValueKind.Integer) },
                    new[] { new EffectOutputBinding("inside", EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static bool? Read(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var obstacles = context.Components?.Obstacles;
                if (board == null || obstacles == null ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(
                            context.ActorId), out var actorAt) ||
                    !obstacles.TryGetObstacleCoordinate(
                        OwnedObstacleIdentity.For(TurretKind, context.ActorId), out var turretAt))
                    return null;
                return HexDistanceQuery.Calculate(actorAt, turretAt) <=
                    context.Input("radius").IntegerValue;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var inside = Read(context);
                return inside.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("inside", EffectValue.Boolean(inside.Value))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Read(context).HasValue ? EffectExecutionResultKind.Applied :
                    EffectExecutionResultKind.ImpossibleMandatory;

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("The Turret-radius fact requests no choice.");
        }
    }
}
