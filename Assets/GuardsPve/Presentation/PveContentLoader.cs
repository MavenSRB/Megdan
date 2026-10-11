using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Setup;
using GuardsDigital.Pve.Content;
using UnityEngine;

namespace GuardsDigital.Pve.Presentation
{
    /// <summary>
    /// PVE-D15. Where the tunable content lives: beside the executable in a player, and in the
    /// repository's non-imported data folder in the Editor.
    /// </summary>
    public static class PveDataPaths
    {
        public const string EnemiesFileName = "enemies.json";
        public const string EncounterFileName = "encounter.json";
        public const string PlayerFolderName = "PveData";

        public static string Directory =>
            UnityEngine.Application.isEditor
                ? Path.Combine(UnityEngine.Application.dataPath, "GuardsPve", "Data~")
                : Path.Combine(Path.GetDirectoryName(UnityEngine.Application.dataPath) ?? ".", PlayerFolderName);
    }

    /// <summary>
    /// Reads the content JSON into the engine-free model. JsonUtility has no nullable or nested-array
    /// support, so absent values are written as 0 and a plan as one space-separated string.
    /// </summary>
    public static class PveContentLoader
    {
        public static PveEnemyCatalog LoadCatalog(string json)
        {
            var source = JsonUtility.FromJson<CatalogDto>(json ?? string.Empty)
                ?? throw new FormatException("Enemy content is not valid JSON.");
            return new PveEnemyCatalog(
                source.contentStatus,
                (source.enemyTypes ?? new EnemyTypeDto[0]).Select(type => new PveEnemyType(
                    type.id,
                    type.displayName,
                    type.level,
                    (type.orangeCards ?? new OrangeCardDto[0]).Select(card => new PveOrangeCard(
                        card.key,
                        card.name,
                        card.initiative,
                        card.copies <= 0 ? 1 : card.copies,
                        card.attack > 0 ? card.attack : (int?)null,
                        card.range,
                        card.move > 0 ? card.move : (int?)null,
                        (card.options ?? new OptionDto[0]).Select(option => new PveCardOption(
                            ParseKind(option.kind),
                            ParseTarget(option.target),
                            option.stopDistance <= 0 ? 1 : option.stopDistance)),
                        card.idle)),
                    (type.purpleCards ?? new PurpleCardDto[0]).Select(card =>
                        new PvePurpleCard(card.key, card.name, card.initiative, card.defense)),
                    (type.plans ?? new string[0]).Select(plan =>
                        (plan ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)),
                    string.IsNullOrWhiteSpace(type.tokenType) ? (StandardMinionType?)null : ParseMinion(type.tokenType))));
        }

        /// <summary>The enemy content as the JSON <see cref="LoadCatalog"/> reads back unchanged.</summary>
        public static string SaveCatalog(PveEnemyCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var dto = new CatalogDto
            {
                contentStatus = catalog.ContentStatus,
                enemyTypes = catalog.Types.Select(type => new EnemyTypeDto
                {
                    id = type.Id,
                    displayName = type.DisplayName,
                    level = type.Level,
                    tokenType = type.TokenType?.ToString().ToLowerInvariant(),
                    orangeCards = type.OrangeCards.Select(card => new OrangeCardDto
                    {
                        key = card.Key,
                        name = card.Name,
                        initiative = card.Initiative,
                        copies = card.Copies,
                        attack = card.AttackValue ?? 0,
                        range = card.AttackRange,
                        move = card.MoveValue ?? 0,
                        idle = card.IsIdle,
                        options = card.Options.Select(option => new OptionDto
                        {
                            kind = option.Kind.ToString().ToLowerInvariant(),
                            target = option.Target.ToString().ToLowerInvariant(),
                            stopDistance = option.StopDistance
                        }).ToArray()
                    }).ToArray(),
                    purpleCards = type.PurpleCards.Select(card => new PurpleCardDto
                    {
                        key = card.Key,
                        name = card.Name,
                        initiative = card.Initiative,
                        defense = card.Defense
                    }).ToArray(),
                    plans = type.Plans.Select(plan => string.Join(" ", plan)).ToArray()
                }).ToArray()
            };
            return JsonUtility.ToJson(dto, true);
        }

        public static PveEncounterDefinition LoadEncounter(string json)
        {
            var source = JsonUtility.FromJson<EncounterDto>(json ?? string.Empty)
                ?? throw new FormatException("Encounter content is not valid JSON.");
            return new PveEncounterDefinition(
                source.map,
                source.mapRevision,
                (source.creeps ?? new CreepDto[0]).Select(creep =>
                    new PveCreepPlacement(ParseHex(creep.hex), ParseMinion(creep.type))),
                (source.heroPlacement ?? new string[0]).Select(ParseHex),
                (source.spawns ?? new SpawnDto[0]).Select(spawn =>
                    new PveSpawn(spawn.round, ParseHex(spawn.hex), spawn.enemyType)));
        }

        public static void LoadFromDirectory(
            string directory, out PveEnemyCatalog catalog, out PveEncounterDefinition encounter)
        {
            catalog = LoadCatalog(ReadRequired(directory, PveDataPaths.EnemiesFileName));
            encounter = LoadEncounter(ReadRequired(directory, PveDataPaths.EncounterFileName));
        }

        public static HexCoordinate ParseHex(string value)
        {
            var parts = (value ?? string.Empty).Split(',');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var q) ||
                !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var r))
            {
                throw new FormatException($"'{value}' is not a hex coordinate written as \"q,r\".");
            }

            return new HexCoordinate(q, r);
        }

        private static string ReadRequired(string directory, string fileName)
        {
            var path = Path.Combine(directory, fileName);
            if (!File.Exists(path)) throw new FileNotFoundException($"PvE content file is missing: {path}", path);
            return File.ReadAllText(path);
        }

        private static PveOptionKind ParseKind(string value)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "move": return PveOptionKind.Move;
                case "attack": return PveOptionKind.Attack;
                default: return PveOptionKind.None;
            }
        }

        private static PveTargetKind ParseTarget(string value)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "hero": return PveTargetKind.Hero;
                case "minion": return PveTargetKind.Minion;
                default: return PveTargetKind.None;
            }
        }

        private static StandardMinionType ParseMinion(string value)
        {
            switch ((value ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "melee": return StandardMinionType.Melee;
                case "ranged": return StandardMinionType.Ranged;
                case "heavy": return StandardMinionType.Heavy;
                default: throw new FormatException($"'{value}' is not a creep type (melee, ranged or heavy).");
            }
        }

#pragma warning disable 0649
        [Serializable] private sealed class CatalogDto { public string contentStatus; public EnemyTypeDto[] enemyTypes; }

        [Serializable]
        private sealed class EnemyTypeDto
        {
            public string id;
            public string displayName;
            public int level;
            public string tokenType;
            public OrangeCardDto[] orangeCards;
            public PurpleCardDto[] purpleCards;
            public string[] plans;
        }

        [Serializable]
        private sealed class OrangeCardDto
        {
            public string key;
            public string name;
            public int initiative;
            public int copies;
            public int attack;
            public int range;
            public int move;
            public bool idle;
            public OptionDto[] options;
        }

        [Serializable] private sealed class OptionDto { public string kind; public string target; public int stopDistance; }

        [Serializable] private sealed class PurpleCardDto { public string key; public string name; public int initiative; public int defense; }

        [Serializable]
        private sealed class EncounterDto
        {
            public string map;
            public int mapRevision;
            public CreepDto[] creeps;
            public string[] heroPlacement;
            public SpawnDto[] spawns;
        }

        [Serializable] private sealed class CreepDto { public string hex; public string type; }

        [Serializable] private sealed class SpawnDto { public int round; public string hex; public string enemyType; }
#pragma warning restore 0649
    }
}
