using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.AdjacentMove;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Presence, which rulebook p.19 separates from targeting: "Whenever your action has any effect on
    /// another unit in any way, you target that unit. Effects that check for unit presence (i.e. if you
    /// are adjacent to a minion) do not target."
    ///
    /// The consequence p.18 spells out is that presence sees what targeting cannot: "Effects that check
    /// for unit presence ... will count immune units." This query therefore consults no immunity at all -
    /// its result is the same whether or not the units it finds are immune - while still applying
    /// <c>Counts As</c>, because the question "is a minion here" is type-based and p.17 replaces an
    /// object's type for exactly such rules.
    /// </summary>
    public static class UnitPresenceQuery
    {
        /// <summary>
        /// Every unit within <paramref name="distance"/> of <paramref name="origin"/> whose effective type
        /// is <paramref name="effectiveUnitKind"/>, in canonical identity order. Distance counts spaces
        /// including spaces with obstacles (p.18, Near/Far).
        /// </summary>
        public static IReadOnlyList<RuleObject> FindUnitsWithin(
            HexCoordinate origin,
            int distance,
            EffectiveUnitKind effectiveUnitKind,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (distance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(distance), distance, "A presence check cannot use a negative distance.");
            }

            if (effectiveUnitKind == EffectiveUnitKind.None)
            {
                throw new ArgumentException(
                    "A presence check requires the unit type it looks for.", nameof(effectiveUnitKind));
            }

            var heroes = board.Occupancies
                .Where(occupancy => HexDistanceQuery.Calculate(origin, occupancy.Coordinate) <= distance)
                .Select(occupancy => RuleObject.ForHero(occupancy.HeroId));
            var minions = board.Minions
                .Where(minion => HexDistanceQuery.Calculate(origin, minion.Coordinate) <= distance)
                .Select(minion => RuleObject.ForMinion(minion.MinionId));

            return Array.AsReadOnly(heroes.Concat(minions)
                .Where(unit => EffectiveUnitKindQuery.Is(unit, effectiveUnitKind, modifications))
                .OrderBy(unit => unit.Kind)
                .ThenBy(
                    unit => unit.Kind == RuleObjectKind.Hero ? unit.HeroId.Value : unit.MinionId.Value,
                    StringComparer.Ordinal)
                .ToArray());
        }

        /// <summary>
        /// Every unit standing in the named zone whose effective type is
        /// <paramref name="effectiveUnitKind"/>, in canonical identity order. Milestone 6.8g added this
        /// zone-scoped presence question for the p.13 Heavy-immunity accompaniment clause - "any
        /// friendly non-heavy minions in the Battle Zone" - which is a membership question about a
        /// region rather than a distance. A hero standing on a playable space of the zone whose
        /// declarations make it count as a minion is a minion for this count (p.17).
        /// </summary>
        public static IReadOnlyList<RuleObject> FindUnitsInZone(
            ZoneId zoneId,
            EffectiveUnitKind effectiveUnitKind,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications)
        {
            if (string.IsNullOrWhiteSpace(zoneId.Value))
            {
                throw new ArgumentException(
                    "A zone presence check requires the zone it looks in.", nameof(zoneId));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (effectiveUnitKind == EffectiveUnitKind.None)
            {
                throw new ArgumentException(
                    "A presence check requires the unit type it looks for.", nameof(effectiveUnitKind));
            }

            var playableByCoordinate = board.MapDefinition.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .ToDictionary(space => space.Coordinate, space => space.Id);
            var heroes = board.Occupancies
                .Where(occupancy =>
                    playableByCoordinate.TryGetValue(occupancy.Coordinate, out var spaceId) &&
                    board.MapDefinition.GetZoneForSpace(spaceId).Id.Equals(zoneId))
                .Select(occupancy => RuleObject.ForHero(occupancy.HeroId));
            var minions = board.Minions
                .Where(minion =>
                    board.MapDefinition.GetZoneForSpace(minion.OccupiedSpace.SpaceId).Id.Equals(zoneId))
                .Select(minion => RuleObject.ForMinion(minion.MinionId));

            return Array.AsReadOnly(heroes.Concat(minions)
                .Where(unit => EffectiveUnitKindQuery.Is(unit, effectiveUnitKind, modifications))
                .OrderBy(unit => unit.Kind)
                .ThenBy(
                    unit => unit.Kind == RuleObjectKind.Hero ? unit.HeroId.Value : unit.MinionId.Value,
                    StringComparer.Ordinal)
                .ToArray());
        }

        /// <summary>
        /// Whether at least one such unit other than <paramref name="self"/> is present, which is the
        /// rulebook's own example shape ("if you are adjacent to a minion"). The required set sits
        /// before the optional exclusion since milestone 6.8f: supplying no modifications is a named
        /// decision, while omitting <paramref name="self"/> stays an ordinary default.
        /// </summary>
        public static bool IsPresentWithin(
            HexCoordinate origin,
            int distance,
            EffectiveUnitKind effectiveUnitKind,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications,
            RuleObject self = default) =>
            FindUnitsWithin(origin, distance, effectiveUnitKind, board, modifications)
                .Any(unit => !(self.IsDefined && unit.Equals(self)));
    }
}
