using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// A deliberately featureless board for milestone 8.39a's two directional clauses: one zone, a
    /// radius-4 disk of playable spaces, no Spawn points, and nobody on it but the pieces a case
    /// names.
    ///
    /// <para><b>Featurelessness is the point</b>, on <see cref="SwiftJumpBoard"/>'s terms. What these
    /// clauses answer is which of p.19's six files an earlier step travelled, and that is a question
    /// about the map and nothing else. On the official map a candidate removed by terrain, by a zone
    /// boundary or by another piece would leave a case passing for a reason the clause had nothing to
    /// do with. Here the only things that can remove a candidate are the clause under test and the
    /// pieces the case puts down itself.</para>
    ///
    /// <para><b>It is its own board rather than Swift's</b> because these cases need enemy HEROES
    /// standing on it - "target a hero adjacent to you in the direction of the move" is a clause about
    /// heroes - and Swift's board carries one hero, the actor, with the enemy declared as a participant
    /// and never placed. Widening hers would change what her own fixture measures.</para>
    ///
    /// <para>The disk is radius 4 because the longest continuation any of the five faces reads is
    /// Tumble Shot's "up to 3 spaces", so a retreat can run its full printed distance and still have a
    /// space beyond it, and nothing is cut off by the map's edge unless a case asks for it.</para>
    /// </summary>
    internal static class ContinuedLineBoard
    {
        internal static readonly MapId Map = new MapId("continued-line-board");
        internal static readonly ZoneId Battle = new ZoneId("continued-line-zone");

        /// <summary>Where the acting hero stands. Every line these cases read runs through it.</summary>
        internal static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        internal static readonly HeroId Actor = new HeroId("continued-line-actor");

        /// <summary>
        /// Six enemy heroes, one per p.19 direction, so a case can place as many of them adjacent to
        /// the actor as it needs and still tell them apart by name.
        /// </summary>
        internal static readonly IReadOnlyList<HeroId> Enemies = Enumerable.Range(0, 6)
            .Select(index => new HeroId($"continued-line-enemy-{index}"))
            .ToArray();

        private static readonly TeamId RedTeam = new TeamId("continued-line-red");
        private static readonly TeamId BlueTeam = new TeamId("continued-line-blue");

        /// <summary>Every playable coordinate: the radius-4 disk around the origin.</summary>
        internal static IReadOnlyList<HexCoordinate> Playable { get; } = Disk(Origin, 4).ToArray();

        /// <summary>
        /// The space <paramref name="steps"/> spaces from the origin along <paramref name="direction"/>
        /// - the shorthand every case below reads its board in.
        /// </summary>
        internal static HexCoordinate At(HexDirection direction, int steps) =>
            StraightLineQuery.Step(Origin, direction, steps);

        internal static BoardMapDefinition CreateMap()
        {
            var zone = new BoardMapZone(Battle, "Continued Line Zone", "lane", null, false, true);
            var spaces = Playable
                .Select(coordinate => new BoardMapSpace(
                    SpaceIdFor(coordinate), coordinate, BoardMapSpaceKind.Playable, Battle))
                .ToArray();

            return new BoardMapDefinition(
                Map, 1, "Continued Line Board", spaces, new[] { zone },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        /// <summary>
        /// The board with the actor at the origin, one enemy hero on each named space, and nothing
        /// else standing anywhere.
        /// </summary>
        internal static AdjacentMoveAuthority CreateBoard(params HexCoordinate[] enemyHeroesAt) =>
            CreateBoard(enemyHeroesAt, Array.Empty<HexCoordinate>(), Array.Empty<HexCoordinate>());

        /// <summary>
        /// The same board with enemy minions and obstacles too - the two things that stop a p.19 move
        /// short of where the printed distance would have reached.
        /// </summary>
        internal static AdjacentMoveAuthority CreateBoard(
            IReadOnlyList<HexCoordinate> enemyHeroesAt,
            IReadOnlyList<HexCoordinate> enemyMinionsAt,
            IReadOnlyList<HexCoordinate> obstaclesAt)
        {
            if (enemyHeroesAt.Count > Enemies.Count)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(enemyHeroesAt), enemyHeroesAt.Count,
                    "This board declares six enemy heroes, one per p.19 direction.");
            }

            var heroes = new List<HeroOccupancy> { new HeroOccupancy(Actor, Origin) };
            heroes.AddRange(enemyHeroesAt.Select((at, index) => new HeroOccupancy(Enemies[index], at)));

            return new AdjacentMoveAuthority(
                CreateMap(),
                heroes,
                obstaclesAt
                    .Select((at, index) => new ObstacleOccupancy(
                        new ObstacleId($"continued-line-obstacle-{index:00}"), at))
                    .ToArray(),
                enemyMinionsAt
                    .Select((at, index) => new QuickGameMinionSetupState(
                        new MinionId($"continued-line-minion-{index:00}"),
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
            // One player per hero, because MatchParticipants requires exactly that - "Each player
            // must be represented by exactly one hero" - and these cases need six enemy heroes so a
            // board can stand one in each of p.19's six directions.
            var actorPlayer = new PlayerId("continued-line-actor-player");
            var enemyPlayers = Enemies
                .Select((_, index) => new PlayerId($"continued-line-enemy-player-{index}"))
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

        internal static SpaceId SpaceIdFor(HexCoordinate coordinate) =>
            new SpaceId($"q{coordinate.Q}_r{coordinate.R}");

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
