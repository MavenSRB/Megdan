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
    /// A board for milestone 8.40e's pull, built on <see cref="GrappleLineBoard"/>'s shape with the one
    /// thing that board cannot express.
    ///
    /// <para><b>It can put terrain in the MIDDLE of a file, which is the whole reason it is not
    /// <see cref="GrappleLineBoard"/>.</b> That board's terrain is a ring at radius 5, one step past
    /// the last playable space, so every file it offers reaches its terrain last and nothing can ever
    /// stand behind it. Bain's Silver basic prints "no obstacles", which is p.18's full list, and the
    /// case that matters most for it is a file whose FIRST obstacle is terrain and whose second is a
    /// unit: the narrowing must offer nothing on that file rather than offering the unit behind the
    /// wall. Declaring one inner terrain space is what makes that board reachable.</para>
    ///
    /// <para>Otherwise featureless on the same terms: one zone, no Spawn points, playable spaces out to
    /// radius 4, the terrain ring at exactly 5, nothing at all past that, and nobody on it but the
    /// pieces a case names - so the only things that can remove a candidate are the clause under test
    /// and what the case puts down.</para>
    /// </summary>
    internal static class PullLineBoard
    {
        internal static readonly MapId Map = new MapId("pull-line-board");
        internal static readonly ZoneId Battle = new ZoneId("pull-line-zone");

        /// <summary>Where the pulling hero stands. Every file these cases walk runs out of it.</summary>
        internal static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        /// <summary>The last playable ring; the terrain ring is one step further out.</summary>
        internal const int PlayableRadius = 4;

        internal const int TerrainRadius = 5;

        internal static readonly HeroId Actor = new HeroId("pull-line-actor");

        internal static readonly IReadOnlyList<HeroId> Enemies = Enumerable.Range(0, 6)
            .Select(index => new HeroId($"pull-line-enemy-{index}"))
            .ToArray();

        private static readonly TeamId RedTeam = new TeamId("pull-line-red");
        private static readonly TeamId BlueTeam = new TeamId("pull-line-blue");

        internal static IReadOnlyList<HexCoordinate> Ring { get; } =
            Disk(TerrainRadius).Where(at => Distance(at) == TerrainRadius).ToArray();

        internal static HexCoordinate At(HexDirection direction, int steps) =>
            StraightLineQuery.Step(Origin, direction, steps);

        /// <summary>
        /// The map. <paramref name="innerTerrainAt"/> declares playable-ring coordinates as terrain
        /// instead, which is how a case reaches a wall with something standing behind it.
        /// </summary>
        internal static BoardMapDefinition CreateMap(
            IReadOnlyList<HexCoordinate> innerTerrainAt = null)
        {
            var inner = innerTerrainAt ?? Array.Empty<HexCoordinate>();
            var zone = new BoardMapZone(Battle, "Pull Line Zone", "lane", null, false, true);
            var spaces = Disk(PlayableRadius)
                .Select(at => inner.Contains(at)
                    ? new BoardMapSpace(SpaceIdFor(at), at, BoardMapSpaceKind.Terrain, null)
                    : new BoardMapSpace(SpaceIdFor(at), at, BoardMapSpaceKind.Playable, Battle))
                .ToList();
            spaces.AddRange(Ring.Select(at => new BoardMapSpace(
                SpaceIdFor(at), at, BoardMapSpaceKind.Terrain, null)));

            return new BoardMapDefinition(
                Map, 1, "Pull Line Board", spaces, new[] { zone },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        internal static AdjacentMoveAuthority CreateBoard(
            IReadOnlyList<HexCoordinate> enemyHeroesAt = null,
            IReadOnlyList<HexCoordinate> enemyMinionsAt = null,
            IReadOnlyList<HexCoordinate> innerTerrainAt = null)
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
                CreateMap(innerTerrainAt),
                heroes,
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                minionsAt
                    .Select((at, index) => new QuickGameMinionSetupState(
                        new MinionId($"pull-line-minion-{index:00}"),
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
            var actorPlayer = new PlayerId("pull-line-actor-player");
            var enemyPlayers = Enemies
                .Select((_, index) => new PlayerId($"pull-line-enemy-player-{index}"))
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
            new SpaceId($"pq{at.Q}_r{at.R}");

        private static int Distance(HexCoordinate at) => HexDistanceQuery.Calculate(Origin, at);

        private static IEnumerable<HexCoordinate> Disk(int radius)
        {
            for (var dq = -radius; dq <= radius; dq++)
            {
                for (var dr = Math.Max(-radius, -dq - radius); dr <= Math.Min(radius, -dq + radius); dr++)
                {
                    yield return new HexCoordinate(Origin.Q + dq, Origin.R + dr);
                }
            }
        }
    }
}
