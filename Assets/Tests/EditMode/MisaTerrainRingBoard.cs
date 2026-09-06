using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// A board built to make Misa's two 2026-09-06 geometry rulings measurable rather than assumed.
    /// Milestone 8.35z29.
    ///
    /// <para><b>Why a purpose-built map and not the official one.</b> RULING 29 says her straight line
    /// "passes THROUGH ANY obstacle including the water terrain ring ... but as long as hero ends up on
    /// a playable space". A map with no terrain in the middle of a line cannot tell that reading from
    /// its opposite, and the official single-lane map rings its whole playable area in terrain - so a
    /// line there either stays inside or leaves the board, and never crosses and comes back. This map
    /// puts a ring of terrain <em>between</em> two bands of playable space, which is the only shape
    /// where crossing and landing are separate questions.</para>
    ///
    /// <para><b>The geometry, stated once so a reading can be checked by hand.</b> Every coordinate
    /// within distance 5 of the origin exists. Those at exactly distance 3 are terrain; every other is
    /// playable. So from the origin:</para>
    ///
    /// <list type="bullet">
    /// <item>distances 1 and 2 are the inner playable band,</item>
    /// <item>distance 3 is the terrain ring - crossable while moving, never an endpoint,</item>
    /// <item>distances 4 and 5 are the outer playable band, reachable only by crossing,</item>
    /// <item>distance 6 and beyond is off the map - not an obstacle, simply not a space.</item>
    /// </list>
    ///
    /// <para>That makes the three arms of RULING 29 separately observable from one board: a line of 4
    /// crosses the ring and lands, a line of 3 has nowhere to land at all, and a line of 6 runs off the
    /// map and produces nothing.</para>
    ///
    /// <para>It also poses RULING 1's ring directly, because "at maximum range 4" over this map is a
    /// set of spaces that exists, and "at maximum range 3" is a set that is entirely terrain and is
    /// therefore legitimately empty - which is the case the exact-distance Place had to be built to
    /// express.</para>
    /// </summary>
    internal static class MisaTerrainRingBoard
    {
        internal static readonly MapId Map = new MapId("misa-terrain-ring-board");
        internal static readonly ZoneId Lane = new ZoneId("misa-lane-zone");

        internal static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        /// <summary>The radius at which every space is declared terrain.</summary>
        internal const int TerrainRingRadius = 3;

        /// <summary>The outermost radius the map has any space at all.</summary>
        internal const int MapRadius = 5;

        internal static IReadOnlyList<HexCoordinate> AllCoordinates { get; } =
            Disk(Origin, MapRadius).ToArray();

        internal static IReadOnlyList<HexCoordinate> TerrainCoordinates { get; } =
            AllCoordinates.Where(value => Distance(Origin, value) == TerrainRingRadius).ToArray();

        internal static IReadOnlyList<HexCoordinate> PlayableCoordinates { get; } =
            AllCoordinates.Where(value => Distance(Origin, value) != TerrainRingRadius).ToArray();

        internal static BoardMapDefinition CreateMap()
        {
            var lane = new BoardMapZone(Lane, "Misa Lane", "lane", null, false, true);
            // Terrain belongs to no zone - the map definition refuses one - which is itself the
            // rulebook's reading: p.19 makes terrain a feature of the board rather than a space a
            // Battle Zone can push through.
            var spaces = AllCoordinates
                .Select(coordinate => Distance(Origin, coordinate) == TerrainRingRadius
                    ? new BoardMapSpace(
                        SpaceIdFor(coordinate), coordinate, BoardMapSpaceKind.Terrain, null)
                    : new BoardMapSpace(
                        SpaceIdFor(coordinate), coordinate, BoardMapSpaceKind.Playable, Lane))
                .ToArray();

            // No ordered Lane and no spawn points: this map poses a movement and placement question and
            // p.8's push is not part of it, so the lane validation is left with nothing to check.
            return new BoardMapDefinition(
                Map, 1, "Misa Terrain Ring Board", spaces, new[] { lane },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        /// <summary>
        /// The board with the named heroes standing where the caller says. Heroes are the only
        /// occupants this fixture needs: what it measures is geometry, and a hero is as much an
        /// obstacle as a minion under p.18.
        /// </summary>
        internal static AdjacentMoveAuthority CreateBoard(
            params (HeroId Hero, HexCoordinate At)[] heroes) =>
            new AdjacentMoveAuthority(
                CreateMap(),
                (heroes ?? Array.Empty<(HeroId, HexCoordinate)>())
                    .Select(value => new HeroOccupancy(value.Hero, value.At))
                    .ToArray(),
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                Array.Empty<QuickGameMinionSetupState>());

        internal static SpaceId SpaceIdFor(HexCoordinate coordinate) =>
            new SpaceId($"q{coordinate.Q}_r{coordinate.R}");

        /// <summary>
        /// p.18's distance, restated here rather than reached for, so the fixture's own description of
        /// its geometry cannot drift from the production query it is used to test.
        /// </summary>
        internal static int Distance(HexCoordinate first, HexCoordinate second) =>
            (Math.Abs(first.Q - second.Q) +
                Math.Abs(first.Q + first.R - second.Q - second.R) +
                Math.Abs(first.R - second.R)) / 2;

        internal static IEnumerable<HexCoordinate> Disk(HexCoordinate centre, int radius)
        {
            for (var dq = -radius; dq <= radius; dq++)
            {
                for (var dr = Math.Max(-radius, -dq - radius); dr <= Math.Min(radius, -dq + radius); dr++)
                {
                    yield return new HexCoordinate(centre.Q + dq, centre.R + dr);
                }
            }
        }
    }
}
