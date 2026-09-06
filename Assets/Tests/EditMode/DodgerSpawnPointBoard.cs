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
    /// A board built to ask Dodger's one board question the four ways her cards ask it, and to make the
    /// two user rulings of 2026-09-06 measurable rather than assumed. Milestone 8.35z28.
    ///
    /// <para><b>Why a purpose-built map and not the official one.</b> The two rulings turn on cases the
    /// official map cannot pose today: RULING 4 excludes <em>hero</em> spawn points, and on the official
    /// map every hero spawn point sits in a Throne Zone that the Battle Zone only reaches in the
    /// endgame - so a fixture on that map would measure a difference of zero and pass whichever reading
    /// were implemented. This map puts a hero spawn point inside the Battle Zone and a minion spawn
    /// point outside it, both within one radius of the same space, so the two rulings are the only
    /// things that can produce the measured number.</para>
    ///
    /// <para><b>The geometry, stated once so a reading can be checked by hand.</b> The Battle Zone is
    /// every coordinate within distance 2 of the origin except three, which form the Throne Zone at
    /// exactly distance 2 - so a hero standing in the Throne Zone still has Battle-Zone spawn points
    /// inside her printed Radius, which is what the designer's zone-anchoring ruling is about.</para>
    ///
    /// <list type="table">
    /// <item><term>(1, 0)</term><description>melee-minion spawn point, Battle Zone, distance 1</description></item>
    /// <item><term>(-1, 0)</term><description>ranged-minion spawn point, Battle Zone, distance 1</description></item>
    /// <item><term>(0, 2)</term><description>melee-minion spawn point, Battle Zone, distance 2</description></item>
    /// <item><term>(-2, 2)</term><description>heavy-minion spawn point, Battle Zone, distance 2</description></item>
    /// <item><term>(0, 1)</term><description><b>hero</b> spawn point, Battle Zone, distance 1 - RULING 4's discriminator</description></item>
    /// <item><term>(2, -1)</term><description>melee-minion spawn point, <b>Throne Zone</b>, distance 2 - the zone discriminator</description></item>
    /// </list>
    ///
    /// <para>So a hero at the origin with a Radius of 2 counts exactly <b>four</b>: five points lie in
    /// the Battle Zone within reach and one of them is a hero's, and a sixth minion point lies within
    /// reach but outside the Zone.</para>
    /// </summary>
    internal static class DodgerSpawnPointBoard
    {
        internal static readonly MapId Map = new MapId("dodger-spawn-point-board");
        internal static readonly ZoneId BattleZone = new ZoneId("dodger-battle-zone");
        internal static readonly ZoneId ThroneZone = new ZoneId("dodger-throne-zone");

        internal static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        /// <summary>The four qualifying points a Radius-2 disk on the origin reaches.</summary>
        internal static readonly HexCoordinate MeleeSpawnNear = new HexCoordinate(1, 0);
        internal static readonly HexCoordinate RangedSpawnNear = new HexCoordinate(-1, 0);
        internal static readonly HexCoordinate MeleeSpawnFar = new HexCoordinate(0, 2);
        internal static readonly HexCoordinate HeavySpawnFar = new HexCoordinate(-2, 2);

        /// <summary>The hero spawn point inside the Battle Zone, which RULING 4 excludes.</summary>
        internal static readonly HexCoordinate HeroSpawn = new HexCoordinate(0, 1);

        /// <summary>The minion spawn point outside the Battle Zone, which the printed clause excludes.</summary>
        internal static readonly HexCoordinate ThroneMinionSpawn = new HexCoordinate(2, -1);

        /// <summary>A Throne-Zone space with no spawn point, for the zone-anchoring regression.</summary>
        internal static readonly HexCoordinate ThroneStandingSpace = new HexCoordinate(2, 0);

        /// <summary>
        /// The three Throne-Zone spaces, all at exactly distance 2 from the origin. Declared before the
        /// Battle Zone because that one is derived by subtracting these, and a static field initialiser
        /// that read a field declared below it would read null.
        /// </summary>
        internal static IReadOnlyList<HexCoordinate> ThroneCoordinates { get; } = new[]
        {
            new HexCoordinate(2, -2), new HexCoordinate(2, -1), new HexCoordinate(2, 0)
        };

        /// <summary>Every playable coordinate the Battle Zone owns: p.9's radius-2 disk, less the Throne.</summary>
        internal static IReadOnlyList<HexCoordinate> BattleZoneCoordinates { get; } =
            Disk(Origin, 2).Where(value => !ThroneCoordinates.Contains(value)).ToArray();

        internal static BoardMapDefinition CreateMap()
        {
            var battle = new BoardMapZone(BattleZone, "Dodger Battle Zone", "lane", null, false, true);
            var throne = new BoardMapZone(ThroneZone, "Dodger Throne Zone", "throne", "red", true, false);
            var spaces = BattleZoneCoordinates
                .Select(coordinate => new BoardMapSpace(
                    SpaceIdFor(coordinate), coordinate, BoardMapSpaceKind.Playable, BattleZone))
                .Concat(ThroneCoordinates.Select(coordinate => new BoardMapSpace(
                    SpaceIdFor(coordinate), coordinate, BoardMapSpaceKind.Playable, ThroneZone)))
                .ToArray();

            var spawnPoints = new[]
            {
                Spawn("bz-melee-near", MeleeSpawnNear, "red", "melee-minion"),
                Spawn("bz-ranged-near", RangedSpawnNear, "red", "ranged-minion"),
                Spawn("bz-melee-far", MeleeSpawnFar, "blue", "melee-minion"),
                Spawn("bz-heavy-far", HeavySpawnFar, "blue", "heavy-minion"),
                Spawn("bz-hero", HeroSpawn, "red", "hero"),
                Spawn("throne-melee", ThroneMinionSpawn, "red", "melee-minion")
            };

            // No ordered Lane is declared: this map poses a static Battle Zone question and p.8's push
            // is not part of it, so the lane validation is deliberately left with nothing to check.
            return new BoardMapDefinition(
                Map, 1, "Dodger Spawn Point Board", spaces, new[] { battle, throne },
                spawnPoints, Array.Empty<ZoneId>());
        }

        /// <summary>
        /// The board with <paramref name="hero"/> standing where the caller says, and a minion on each
        /// coordinate of <paramref name="occupiedSpawnPoints"/> - which is how a fixture makes a spawn
        /// point stop being empty.
        /// </summary>
        internal static AdjacentMoveAuthority CreateBoard(
            HeroId hero,
            HexCoordinate heroAt,
            params HexCoordinate[] occupiedSpawnPoints) =>
            CreateBoardWithTypedOccupiers(
                hero,
                heroAt,
                (occupiedSpawnPoints ?? Array.Empty<HexCoordinate>())
                    .Select(coordinate => (coordinate, TeamSide.Red, StandardMinionType.Melee))
                    .ToArray());

        /// <summary>
        /// The same board with each occupier's side and type named. Milestone 8.35z28 needed it
        /// for rulebook p.19's Respawn count, which compares Spawn points and miniatures <em>of that
        /// type and colour</em> - so a fixture that occupied a Heavy point with the default Melee
        /// miniature would leave the Heavy count reading zero miniatures and answer the opposite of
        /// what it meant to ask.
        ///
        /// <para>That is the same trap phase one recorded from the other side: when a fixture perturbs
        /// the board to set up a condition, the perturbation is often itself an input to the number
        /// being measured. Here the type of the occupier <em>is</em> the question.</para>
        /// </summary>
        internal static AdjacentMoveAuthority CreateBoardWithTypedOccupiers(
            HeroId hero,
            HexCoordinate heroAt,
            params (HexCoordinate At, TeamSide Side, StandardMinionType Type)[] occupiers)
        {
            var minions = (occupiers ??
                    Array.Empty<(HexCoordinate, TeamSide, StandardMinionType)>())
                .Select((occupier, index) => new QuickGameMinionSetupState(
                    new MinionId($"occupying-minion-{index:00}"),
                    new TeamId(occupier.Side == TeamSide.Red ? "red" : "blue"),
                    occupier.Side,
                    occupier.Type,
                    new ConfiguredSpawnPointReference(Map, new SpawnPointId($"bz-melee-near")),
                    new ConfiguredSpaceReference(Map, SpaceIdFor(occupier.At)),
                    occupier.At))
                .ToArray();

            return new AdjacentMoveAuthority(
                CreateMap(),
                new[] { new HeroOccupancy(hero, heroAt) },
                Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                minions);
        }

        internal static SpaceId SpaceIdFor(HexCoordinate coordinate) =>
            new SpaceId($"q{coordinate.Q}_r{coordinate.R}");

        private static BoardMapSpawnPoint Spawn(
            string id, HexCoordinate at, string team, string unitType) =>
            new BoardMapSpawnPoint(new SpawnPointId(id), SpaceIdFor(at), team, unitType);

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
