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
    /// A board for milestone 8.39b's obstacle line, built to tell rulebook p.18's three kinds of
    /// obstacle apart from each other and from the void past the map's edge.
    ///
    /// <para><b>It has a terrain ring, which is the whole reason it is not
    /// <see cref="ContinuedLineBoard"/>.</b> p.19 says "Any board space without a full grid outline
    /// counts as terrain and is an obstacle, including the water spaces surrounding the board", so a
    /// fixture that measures what "target an obstacle" admits has to carry terrain as well as units -
    /// and has to carry an edge beyond it, because off the map is the absence of a space rather than
    /// an obstacle. Playable spaces run to radius 4, terrain sits at exactly radius 5, and nothing at
    /// all lies past that.</para>
    ///
    /// <para>Otherwise featureless on <see cref="SwiftJumpBoard"/>'s terms: one zone, no Spawn points,
    /// nobody on it but the pieces a case names, so the only things that can remove a candidate are
    /// the clause under test and what the case puts down.</para>
    /// </summary>
    internal static class GrappleLineBoard
    {
        internal static readonly MapId Map = new MapId("grapple-line-board");
        internal static readonly ZoneId Battle = new ZoneId("grapple-line-zone");

        /// <summary>Where the grappling hero stands. Every file these cases walk runs out of it.</summary>
        internal static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        /// <summary>The last playable ring; terrain is one step further out.</summary>
        internal const int PlayableRadius = 4;

        internal const int TerrainRadius = 5;

        internal static readonly HeroId Actor = new HeroId("grapple-line-actor");

        internal static readonly IReadOnlyList<HeroId> Enemies = Enumerable.Range(0, 6)
            .Select(index => new HeroId($"grapple-line-enemy-{index}"))
            .ToArray();

        private static readonly TeamId RedTeam = new TeamId("grapple-line-red");
        private static readonly TeamId BlueTeam = new TeamId("grapple-line-blue");

        internal static IReadOnlyList<HexCoordinate> Playable { get; } =
            Disk(Origin, PlayableRadius).ToArray();

        internal static IReadOnlyList<HexCoordinate> Terrain { get; } =
            Disk(Origin, TerrainRadius).Where(at => Distance(at) == TerrainRadius).ToArray();

        internal static HexCoordinate At(HexDirection direction, int steps) =>
            StraightLineQuery.Step(Origin, direction, steps);

        /// <summary>
        /// The map, optionally without its terrain ring - which is how a case reaches the map's edge
        /// and proves that the void past it offers nothing rather than offering an obstacle.
        /// </summary>
        internal static BoardMapDefinition CreateMap(bool withTerrainRing = true)
        {
            var zone = new BoardMapZone(Battle, "Grapple Line Zone", "lane", null, false, true);
            var spaces = Playable
                .Select(at => new BoardMapSpace(
                    SpaceIdFor(at), at, BoardMapSpaceKind.Playable, Battle))
                .ToList();
            if (withTerrainRing)
            {
                spaces.AddRange(Terrain.Select(at => new BoardMapSpace(
                    SpaceIdFor(at), at, BoardMapSpaceKind.Terrain, null)));
            }

            return new BoardMapDefinition(
                Map, 1, "Grapple Line Board", spaces, new[] { zone },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        internal static AdjacentMoveAuthority CreateBoard(
            IReadOnlyList<HexCoordinate> enemyHeroesAt = null,
            IReadOnlyList<HexCoordinate> enemyMinionsAt = null,
            bool withTerrainRing = true)
        {
            var heroesAt = enemyHeroesAt ?? Array.Empty<HexCoordinate>();
            var minionsAt = enemyMinionsAt ?? Array.Empty<HexCoordinate>();
            if (heroesAt.Count > Enemies.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(enemyHeroesAt), heroesAt.Count,
                    "This board declares six enemy heroes, one per p.19 direction.");
            }

            var heroes = new List<HeroOccupancy> { new HeroOccupancy(Actor, Origin) };
            heroes.AddRange(heroesAt.Select((at, index) => new HeroOccupancy(Enemies[index], at)));

            return new AdjacentMoveAuthority(
                CreateMap(withTerrainRing),
                heroes,
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                minionsAt
                    .Select((at, index) => new QuickGameMinionSetupState(
                        new MinionId($"grapple-line-minion-{index:00}"),
                        BlueTeam,
                        TeamSide.Blue,
                        StandardMinionType.Melee,
                        default,
                        new ConfiguredSpaceReference(Map, SpaceIdFor(at)),
                        at))
                    .ToArray());
        }

        internal static MatchParticipants Participants()
        {
            // One player per hero: MatchParticipants requires "Each player must be represented by
            // exactly one hero", and these cases need enemy heroes on several files at once.
            var actorPlayer = new PlayerId("grapple-line-actor-player");
            var enemyPlayers = Enemies
                .Select((_, index) => new PlayerId($"grapple-line-enemy-player-{index}"))
                .ToArray();

            var heroes = new List<Hero>
            {
                new Hero(Actor, actorPlayer, HeroDashboard.RulebookLayout)
            };
            heroes.AddRange(Enemies.Select(
                (enemy, index) => new Hero(enemy, enemyPlayers[index], HeroDashboard.RulebookLayout)));

            var players = new List<Player> { new Player(actorPlayer, RedTeam) };
            players.AddRange(enemyPlayers.Select(player => new Player(player, BlueTeam)));

            return new MatchParticipants(
                new[] { new Team(RedTeam, TeamSide.Red), new Team(BlueTeam, TeamSide.Blue) },
                players,
                heroes);
        }

        internal static SpaceId SpaceIdFor(HexCoordinate at) =>
            new SpaceId($"q{at.Q}_r{at.R}");

        private static int Distance(HexCoordinate at) => HexDistanceQuery.Calculate(Origin, at);

        private static IEnumerable<HexCoordinate> Disk(HexCoordinate centre, int radius)
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
