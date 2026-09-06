using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// A board built to make Silverarrow's two targeting rulings measurable rather than assumed.
    /// Milestone 8.35z30.
    ///
    /// <para><b>Why a purpose-built map.</b> RULING 1 turns "at maximum range" into a ring, and a ring
    /// can only be told from a disk on a board with room for both - the official single-lane map's
    /// playable area is not laid out to give a clean set of spaces at every distance from one hero. And
    /// the Red modals' union is a statement about who is standing <em>beside a candidate</em>, which
    /// needs pieces placed to order at named distances. This map is an open disk of radius six with no
    /// terrain and no spawn points: every question it is asked is about distance and occupancy, and
    /// nothing else can interfere with the answer.</para>
    ///
    /// <para>Both teams exist on it so that "enemy" is a real relation rather than an assumption, and
    /// minions can be placed because p.19 makes a minion as much a unit as a hero - which is exactly
    /// the half of the isolation predicate a hero-only fixture would never exercise.</para>
    /// </summary>
    internal static class SilverarrowRangeBoard
    {
        internal static readonly MapId Map = new MapId("silverarrow-range-board");
        internal static readonly ZoneId Lane = new ZoneId("silverarrow-lane-zone");

        internal static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        internal static readonly HeroId Silverarrow = new HeroId("silverarrow-hero");
        internal static readonly HeroId Ally = new HeroId("silverarrow-ally");
        internal static readonly HeroId Enemy = new HeroId("silverarrow-enemy");
        internal static readonly HeroId SecondEnemy = new HeroId("silverarrow-enemy-2");

        internal static readonly TeamId RedTeam = new TeamId("silverarrow-red");
        internal static readonly TeamId BlueTeam = new TeamId("silverarrow-blue");

        /// <summary>The outermost radius the map has any space at all.</summary>
        internal const int MapRadius = 6;

        internal static IReadOnlyList<HexCoordinate> PlayableCoordinates { get; } =
            Disk(Origin, MapRadius).ToArray();

        internal static BoardMapDefinition CreateMap()
        {
            var lane = new BoardMapZone(Lane, "Silverarrow Lane", "lane", null, false, true);
            var spaces = PlayableCoordinates
                .Select(coordinate => new BoardMapSpace(
                    SpaceIdFor(coordinate), coordinate, BoardMapSpaceKind.Playable, Lane))
                .ToArray();
            return new BoardMapDefinition(
                Map, 1, "Silverarrow Range Board", spaces, new[] { lane },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        /// <summary>
        /// The board with the named heroes and minions standing where the caller says. Silverarrow is
        /// always on the origin unless the caller places her somewhere else.
        /// </summary>
        internal static AdjacentMoveAuthority CreateBoard(
            IEnumerable<(HeroId Hero, HexCoordinate At)> heroes,
            IEnumerable<(string Minion, TeamId Team, HexCoordinate At)> minions = null) =>
            new AdjacentMoveAuthority(
                CreateMap(),
                (heroes ?? Array.Empty<(HeroId, HexCoordinate)>())
                    .Select(value => new HeroOccupancy(value.Hero, value.At))
                    .ToArray(),
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                (minions ?? Array.Empty<(string, TeamId, HexCoordinate)>())
                    .Select(value => new QuickGameMinionSetupState(
                        new MinionId(value.Minion),
                        value.Team,
                        value.Team.Equals(RedTeam) ? TeamSide.Red : TeamSide.Blue,
                        StandardMinionType.Melee,
                        new ConfiguredSpawnPointReference(
                            Map, new SpawnPointId("silverarrow-spawn")),
                        new ConfiguredSpaceReference(Map, SpaceIdFor(value.At)),
                        value.At))
                    .ToArray());

        /// <summary>
        /// Silverarrow and one ally on Red, two enemies on Blue. Every hero the fixture can place is
        /// declared here whether or not a case puts it on the board, because the relation query reads
        /// the roster rather than the occupancies.
        /// </summary>
        internal static MatchParticipants CreateParticipants() => new MatchParticipants(
            new[]
            {
                new Team(RedTeam, TeamSide.Red),
                new Team(BlueTeam, TeamSide.Blue)
            },
            new[]
            {
                new Player(new PlayerId("silverarrow-player"), RedTeam),
                new Player(new PlayerId("silverarrow-ally-player"), RedTeam),
                new Player(new PlayerId("silverarrow-enemy-player"), BlueTeam),
                new Player(new PlayerId("silverarrow-enemy-2-player"), BlueTeam)
            },
            new[]
            {
                new Hero(Silverarrow, new PlayerId("silverarrow-player"), HeroDashboard.RulebookLayout),
                new Hero(Ally, new PlayerId("silverarrow-ally-player"), HeroDashboard.RulebookLayout),
                new Hero(Enemy, new PlayerId("silverarrow-enemy-player"), HeroDashboard.RulebookLayout),
                new Hero(
                    SecondEnemy, new PlayerId("silverarrow-enemy-2-player"),
                    HeroDashboard.RulebookLayout)
            });

        /// <summary>One space at exactly the named distance from the origin, along a fixed file.</summary>
        internal static HexCoordinate At(int distance) => new HexCoordinate(distance, 0);

        /// <summary>A space at the named distance along a different file, for a second piece.</summary>
        internal static HexCoordinate AtOtherFile(int distance) => new HexCoordinate(0, distance);

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
