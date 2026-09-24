using System.Collections.Generic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Physical components of Trinkets; registration waits for his dealing milestone.</summary>
    public static class TrinketsComponentContent
    {
        public const string TurretKind = "trinkets-turret";

        public static EffectCapabilityReference TurretSpaceV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-turret-space"), 1);

        public static EffectCapabilityReference PlaceTurretAdjacentV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-place-turret-adjacent"), 1);

        public static EffectCapabilityReference RemoveTurretV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("trinkets-remove-turret"), 1);

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
    }
}
