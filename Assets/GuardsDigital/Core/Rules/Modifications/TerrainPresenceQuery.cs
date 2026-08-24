using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Whether a space counts as terrain right now: the map's printed terrain, plus milestone 8.19c's
    /// objects that an effective-type overlay has made terrain as well.
    ///
    /// <para>p.18 lists terrain among the obstacles and the map prints most of it, so before this the
    /// question had one answer and one asker. Xargatha's Stone Gaze gives it a second source - "Enemy
    /// heroes in radius count as both heroes and terrain" - and the two have to reach one answer, or a
    /// card asking "are you adjacent to terrain" would see the rock and not the petrified hero standing
    /// next to it.</para>
    ///
    /// <para>It reads the effective type rather than the intrinsic one, which is the whole of what the
    /// overlay is for, and it reads nothing about occupancy: a hero counting as terrain still occupies
    /// its space as a hero, is still targetable as a hero, and still belongs to its team. p.17's
    /// replacement would have taken all three away, which is why the overlay is a separate family.</para>
    /// </summary>
    public static class TerrainPresenceQuery
    {
        /// <summary>Whether <paramref name="coordinate"/> counts as terrain at this moment.</summary>
        public static bool IsTerrain(
            IBoardDisplacementAuthority board,
            HexCoordinate coordinate,
            RuleModificationSet modifications)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));

            if (board.MapDefinition.Spaces.Any(space =>
                    space.Kind == BoardMapSpaceKind.Terrain && space.Coordinate.Equals(coordinate)))
            {
                return true;
            }

            return StandsAsTerrain(board, coordinate, modifications);
        }

        /// <summary>
        /// Whether any of <paramref name="coordinate"/>'s six neighbours counts as terrain. The shape
        /// every "adjacent to terrain" card text asks in, stated once so the two sources cannot be
        /// consulted separately.
        /// </summary>
        public static bool IsAdjacentToTerrain(
            IBoardDisplacementAuthority board,
            HexCoordinate coordinate,
            RuleModificationSet modifications)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            return coordinate.GetNeighbors().Any(neighbor => IsTerrain(board, neighbor, modifications));
        }

        private static bool StandsAsTerrain(
            IBoardDisplacementAuthority board,
            HexCoordinate coordinate,
            RuleModificationSet modifications)
        {
            foreach (var occupancy in board.Occupancies)
            {
                if (occupancy.Coordinate.Equals(coordinate) &&
                    EffectiveUnitKindQuery.Is(
                        RuleObject.ForHero(occupancy.HeroId), EffectiveUnitKind.Terrain, modifications))
                {
                    return true;
                }
            }

            foreach (var minion in board.Minions)
            {
                if (minion.Coordinate.Equals(coordinate) &&
                    EffectiveUnitKindQuery.Is(
                        RuleObject.ForMinion(minion.MinionId), EffectiveUnitKind.Terrain, modifications))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
