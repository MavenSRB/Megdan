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
    /// A deliberately featureless board for milestone 8.38b's two placement qualifiers: one zone, a
    /// radius-4 disk of playable spaces, no Spawn points and nobody on it but the hero the case names.
    ///
    /// <para><b>Featurelessness is the point.</b> The question these qualifiers answer is rulebook
    /// p.19's - whether a destination stands on one of the six files through the acting hero - and it
    /// is a question about the map and nothing else. A fixture on the official map would answer it
    /// beside terrain, Spawn-point exclusions, zone boundaries and other pieces, any of which could
    /// remove a space the qualifier admitted and leave a case passing for the wrong reason. Here the
    /// only thing that can remove a space is the clause under test.</para>
    ///
    /// <para>The disk is radius 4 because the widest Radius Swift prints on a jump face is 4 (Assault
    /// Jump and Drop Trooper), so a Radius-4 Place on the origin can reach the whole board and nothing
    /// is cut off by the map's edge unless a case asks for it.</para>
    /// </summary>
    internal static class SwiftJumpBoard
    {
        internal static readonly MapId Map = new MapId("swift-jump-board");
        internal static readonly ZoneId Battle = new ZoneId("swift-jump-zone");

        internal static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        internal static readonly HeroId Swift = new HeroId("swift-probe");
        internal static readonly HeroId Enemy = new HeroId("swift-probe-enemy");

        private static readonly TeamId RedTeam = new TeamId("swift-probe-red");
        private static readonly TeamId BlueTeam = new TeamId("swift-probe-blue");

        /// <summary>Every playable coordinate: the radius-4 disk around the origin.</summary>
        internal static IReadOnlyList<HexCoordinate> Playable { get; } = Disk(Origin, 4).ToArray();

        internal static BoardMapDefinition CreateMap()
        {
            var zone = new BoardMapZone(Battle, "Swift Jump Zone", "lane", null, false, true);
            var spaces = Playable
                .Select(coordinate => new BoardMapSpace(
                    SpaceIdFor(coordinate), coordinate, BoardMapSpaceKind.Playable, Battle))
                .ToArray();

            // No Spawn points at all, which is what lets these cases measure the line clause alone:
            // the printed arm of the Place carries a Spawn-point exclusion parameter, and a board with
            // no points to exclude cannot have its answer changed by whichever exclusion is declared.
            return new BoardMapDefinition(
                Map, 1, "Swift Jump Board", spaces, new[] { zone },
                Array.Empty<BoardMapSpawnPoint>(), Array.Empty<ZoneId>());
        }

        /// <summary>
        /// The board with the hero at the origin and a minion standing on each named coordinate, which
        /// is how a case makes a space stop being empty.
        /// </summary>
        internal static AdjacentMoveAuthority CreateBoard(params HexCoordinate[] occupied) =>
            new AdjacentMoveAuthority(
                CreateMap(),
                new[] { new HeroOccupancy(Swift, Origin) },
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                (occupied ?? Array.Empty<HexCoordinate>())
                    .Select((at, index) => new QuickGameMinionSetupState(
                        new MinionId($"swift-probe-minion-{index:00}"),
                        BlueTeam,
                        TeamSide.Blue,
                        StandardMinionType.Melee,
                        default,
                        new ConfiguredSpaceReference(Map, SpaceIdFor(at)),
                        at))
                    .ToArray());

        internal static MatchParticipants Participants()
        {
            var swiftPlayer = new PlayerId("swift-probe-player");
            var enemyPlayer = new PlayerId("swift-probe-enemy-player");
            return new MatchParticipants(
                new[] { new Team(RedTeam, TeamSide.Red), new Team(BlueTeam, TeamSide.Blue) },
                new[] { new Player(swiftPlayer, RedTeam), new Player(enemyPlayer, BlueTeam) },
                new[]
                {
                    new Hero(Swift, swiftPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Enemy, enemyPlayer, HeroDashboard.RulebookLayout)
                });
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
