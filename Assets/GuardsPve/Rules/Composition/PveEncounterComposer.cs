using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Pve.Content;

namespace GuardsDigital.Pve.Composition
{
    /// <summary>PVE-D02/D13. What the player chose at setup.</summary>
    public sealed class PveEncounterSetup
    {
        public PveEncounterSetup(IEnumerable<string> heroIdValues, ulong seed)
        {
            HeroIdValues = Array.AsReadOnly((heroIdValues ?? Enumerable.Empty<string>()).ToArray());
            Seed = PveSeed.Normalize(seed);
        }

        public IReadOnlyList<string> HeroIdValues { get; }
        public ulong Seed { get; }
    }

    /// <summary>One enemy in the encounter: its hero identity, type and where and when it enters.</summary>
    public sealed class PveEnemyRecord
    {
        public PveEnemyRecord(HeroId heroId, PveEnemyType type, int ordinal, PveSpawn spawn)
        {
            HeroId = heroId;
            Type = type;
            Ordinal = ordinal;
            Spawn = spawn;
        }

        public HeroId HeroId { get; }
        public PveEnemyType Type { get; }

        /// <summary>1-based, in spawn order; the number on the enemy's placeholder token.</summary>
        public int Ordinal { get; }

        public PveSpawn Spawn { get; }

        public string DisplayName => Type.DisplayName + " " + Ordinal.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>A composed encounter: the match's initial state and what PvE needs to drive it.</summary>
    public sealed class PveComposedEncounter
    {
        public PveComposedEncounter(
            PveEncounterSetup setup,
            PveEncounterDefinition encounter,
            PveEnemyCatalog catalog,
            MatchSessionInitialState initialState,
            IEnumerable<HeroId> heroes,
            IEnumerable<PveEnemyRecord> enemies)
        {
            Setup = setup;
            Encounter = encounter;
            Catalog = catalog;
            InitialState = initialState;
            Heroes = Array.AsReadOnly(heroes.ToArray());
            Enemies = Array.AsReadOnly(enemies.ToArray());
        }

        public PveEncounterSetup Setup { get; }
        public PveEncounterDefinition Encounter { get; }
        public PveEnemyCatalog Catalog { get; }
        public MatchSessionInitialState InitialState { get; }
        public IReadOnlyList<HeroId> Heroes { get; }
        public IReadOnlyList<PveEnemyRecord> Enemies { get; }

        public PveEnemyRecord FindEnemy(HeroId heroId) =>
            Enemies.FirstOrDefault(enemy => enemy.HeroId.Equals(heroId));
    }

    public sealed class PveSetupException : Exception
    {
        public PveSetupException(IEnumerable<string> problems)
            : base(string.Join(Environment.NewLine, problems))
        {
            Problems = Array.AsReadOnly(problems.ToArray());
        }

        public IReadOnlyList<string> Problems { get; }
    }

    /// <summary>
    /// Composes a PvE encounter as an ordinary match initial state through public APIs only: two
    /// teams, one player per hero, the chosen heroes' printed sets on Blue (PVE-D02), every enemy as
    /// a Red hero holding its enemy cards (PVE-D06), the creeps as Blue standard minions (PVE-D04)
    /// and the Tie Breaker from the seed (PVE-D13).
    /// </summary>
    /// <remarks>
    /// <para>W-021 composes the static encounter: heroes stand on the placement spaces in the order
    /// chosen and round-1 enemies on their spawns. Round-2 enemies are participants that start off
    /// the board. Life counters exist only because the engine counts them; PvE never shows or
    /// spends them toward an ending (PVE-D04).</para>
    /// </remarks>
    public static class PveEncounterComposer
    {
        public const int UnusedLifeCount = 99;
        public const int UnusedWaveCount = 3;
        public const string RedTeamIdValue = OfficialSixPlayerParticipants.RedTeamIdValue;
        public const string BlueTeamIdValue = OfficialSixPlayerParticipants.BlueTeamIdValue;

        public static PveComposedEncounter Compose(
            PveEncounterSetup setup,
            PveEncounterDefinition encounter,
            PveEnemyCatalog catalog,
            BoardMapDefinition map)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            var problems = PveContentValidator.Validate(catalog, encounter, map).ToList();
            problems.AddRange(ValidateSetup(setup, encounter));
            if (problems.Count > 0) throw new PveSetupException(problems);

            var redTeam = new TeamId(RedTeamIdValue);
            var blueTeam = new TeamId(BlueTeamIdValue);
            var teams = new[] { new Team(redTeam, TeamSide.Red), new Team(blueTeam, TeamSide.Blue) };
            var players = new List<Player>();
            var heroes = new List<Hero>();
            var hands = new List<ConfiguredHand>();
            var heroSetups = new List<QuickGameHeroSetupState>();
            var standing = new List<HeroOccupancy>();

            var redHeroSpawn = map.SpawnPoints.First(point => point.TeamId == "red" && point.UnitType == "hero");
            var blueHeroSpawn = map.SpawnPoints.First(point => point.TeamId == "blue" && point.UnitType == "hero");

            var enemies = new List<PveEnemyRecord>();
            var ordinal = 0;
            foreach (var spawn in encounter.Spawns.OrderBy(value => value.Round))
            {
                ordinal++;
                var number = ordinal.ToString("00", CultureInfo.InvariantCulture);
                var type = catalog.Find(spawn.EnemyTypeId);
                var heroId = new HeroId("pve-enemy-" + number);
                var playerId = new PlayerId("red-player-" + number);
                players.Add(new Player(playerId, redTeam));
                heroes.Add(new Hero(heroId, playerId, HeroDashboard.RulebookLayout));
                hands.Add(PveCardFactory.CreateHand(type, heroId, "red-" + number));
                var record = new PveEnemyRecord(heroId, type, ordinal, spawn);
                enemies.Add(record);
                heroSetups.Add(HeroSetup(map, heroId, new HeroContentId("pve-" + type.Id), record.DisplayName,
                    redTeam, TeamSide.Red, redHeroSpawn, spawn.Coordinate));
                if (spawn.Round == 1) standing.Add(new HeroOccupancy(heroId, spawn.Coordinate));
            }

            var blueHeroes = new List<HeroId>();
            for (var index = 0; index < setup.HeroIdValues.Count; index++)
            {
                var number = (index + 1).ToString("00", CultureInfo.InvariantCulture);
                RegisteredHeroRoster.TryFind(setup.HeroIdValues[index], out var registered);
                var heroId = new HeroId(registered.HeroIdValue);
                var playerId = new PlayerId("blue-player-" + number);
                players.Add(new Player(playerId, blueTeam));
                heroes.Add(new Hero(heroId, playerId, HeroDashboard.RulebookLayout));
                hands.Add(MatchConfiguration.CreateVerifiedHandForInternalTest(registered.HeroIdValue, "blue-" + number));
                blueHeroes.Add(heroId);
                var space = encounter.HeroPlacement[index];
                heroSetups.Add(HeroSetup(map, heroId, registered.ContentId, registered.DisplayName,
                    blueTeam, TeamSide.Blue, blueHeroSpawn, space));
                standing.Add(new HeroOccupancy(heroId, space));
            }

            var participants = new MatchParticipants(teams, players, heroes);
            var setupSeed = new SetupRandomnessSeed(setup.Seed);
            var tieBreaker = TieBreakerSetupAuthority.CreateNormalSetupState(setupSeed);
            var configuration = MatchConfiguration.CreateValidatedFromHands(participants, hands, tieBreaker, setupSeed);

            var minions = CreateCreeps(map, encounter, blueTeam);
            var quickSetup = new QuickGameSetupState(
                map.Reference,
                UnusedWaveCount,
                tieBreaker,
                new[]
                {
                    new QuickGameTeamSetupState(redTeam, TeamSide.Red, UnusedLifeCount, ThroneOf(map, "red")),
                    new QuickGameTeamSetupState(blueTeam, TeamSide.Blue, UnusedLifeCount, ThroneOf(map, "blue"))
                },
                heroSetups,
                minions);
            var scenario = new InitialScenarioDescription(
                participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                tieBreaker,
                setupSeed,
                map.Reference,
                standing,
                Array.Empty<ObstacleOccupancy>());
            var initialState = new MatchSessionInitialState(scenario, quickSetup, new[] { map }, configuration);
            return new PveComposedEncounter(setup, encounter, catalog, initialState, blueHeroes, enemies);
        }

        public static IReadOnlyList<string> ValidateSetup(PveEncounterSetup setup, PveEncounterDefinition encounter)
        {
            var problems = new List<string>();
            var limit = encounter?.HeroPlacement.Count ?? 0;
            if (setup.HeroIdValues.Count == 0) problems.Add("Choose at least one hero.");
            if (setup.HeroIdValues.Count > limit)
                problems.Add($"Choose at most {limit} heroes; the encounter has {limit} placement spaces.");
            foreach (var duplicate in setup.HeroIdValues.GroupBy(value => value).Where(group => group.Count() > 1))
                problems.Add($"Hero '{duplicate.Key}' is chosen more than once.");
            foreach (var value in setup.HeroIdValues.Where(value => !RegisteredHeroRoster.TryFind(value, out _)))
                problems.Add($"Hero '{value}' is not a configured hero.");
            return problems;
        }

        private static QuickGameHeroSetupState HeroSetup(
            BoardMapDefinition map,
            HeroId heroId,
            HeroContentId contentId,
            string displayName,
            TeamId team,
            TeamSide side,
            BoardMapSpawnPoint referenceSpawn,
            HexCoordinate coordinate)
        {
            // The spawn point is a reference the board view requires to exist; where the hero
            // stands is the space, which need not be that spawn's (as in staged boards).
            var space = map.Spaces.Single(value => value.Coordinate.Equals(coordinate));
            return new QuickGameHeroSetupState(
                heroId,
                contentId,
                displayName,
                team,
                side,
                new ConfiguredSpawnPointReference(map.Id, referenceSpawn.Id),
                new ConfiguredSpaceReference(map.Id, space.Id),
                coordinate);
        }

        private static IReadOnlyList<QuickGameMinionSetupState> CreateCreeps(
            BoardMapDefinition map, PveEncounterDefinition encounter, TeamId blueTeam)
        {
            var creeps = new List<QuickGameMinionSetupState>();
            var counts = new Dictionary<StandardMinionType, int>();
            foreach (var creep in encounter.Creeps)
            {
                counts.TryGetValue(creep.MinionType, out var count);
                counts[creep.MinionType] = ++count;
                var name = creep.MinionType.ToString().ToLowerInvariant();
                // A real Blue spawn point of the creep's type: the standard-minion record names one.
                // Lane Push, the only reader that returns a minion to it, never runs (PVE-D03).
                var reference = map.SpawnPoints
                    .Where(point => point.TeamId == "blue" && point.UnitType == name + "-minion")
                    .OrderBy(point => point.Id.Value, StringComparer.Ordinal)
                    .ElementAt((count - 1) % map.SpawnPoints.Count(point =>
                        point.TeamId == "blue" && point.UnitType == name + "-minion"));
                var space = map.Spaces.Single(value => value.Coordinate.Equals(creep.Coordinate));
                creeps.Add(new QuickGameMinionSetupState(
                    new MinionId("blue-" + name + "-" + count.ToString("00", CultureInfo.InvariantCulture)),
                    blueTeam,
                    TeamSide.Blue,
                    creep.MinionType,
                    new ConfiguredSpawnPointReference(map.Id, reference.Id),
                    new ConfiguredSpaceReference(map.Id, space.Id),
                    creep.Coordinate));
            }

            return creeps;
        }

        private static ZoneId ThroneOf(BoardMapDefinition map, string team) =>
            map.Zones.Single(zone => zone.IsThrone && zone.TeamId == team).Id;
    }
}
