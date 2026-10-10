using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;

namespace GuardsDigital.Pve.Content
{
    /// <summary>
    /// PVE-D15. Refuses invalid enemy or encounter content with readable messages before anything is
    /// composed, so a tuning mistake in the JSON beside the executable is reported at setup rather
    /// than thrown mid-match.
    /// </summary>
    public static class PveContentValidator
    {
        public const int PlanLength = 4;
        public const int MinimumActingOrangeCards = 3;
        public const int MaximumPurpleCards = 2;
        public const int MaximumValue = 20;

        public static IReadOnlyList<string> Validate(
            PveEnemyCatalog catalog, PveEncounterDefinition encounter, BoardMapDefinition map)
        {
            var errors = new List<string>();
            if (catalog == null) errors.Add("Enemy content is missing.");
            if (encounter == null) errors.Add("Encounter content is missing.");
            if (map == null) errors.Add("The encounter map is missing.");
            if (errors.Count > 0) return errors;

            ValidateCatalog(catalog, errors);
            ValidateEncounter(catalog, encounter, map, errors);
            return errors;
        }

        private static void ValidateCatalog(PveEnemyCatalog catalog, List<string> errors)
        {
            if (catalog.Types.Count == 0) errors.Add("Enemy content declares no enemy type.");
            foreach (var duplicate in catalog.Types.GroupBy(type => type.Id ?? string.Empty)
                         .Where(group => group.Count() > 1))
            {
                errors.Add($"Enemy type '{duplicate.Key}' is declared more than once.");
            }

            foreach (var type in catalog.Types)
            {
                ValidateType(type, errors);
            }
        }

        private static void ValidateType(PveEnemyType type, List<string> errors)
        {
            var name = string.IsNullOrWhiteSpace(type.Id) ? "(unnamed)" : type.Id;
            if (string.IsNullOrWhiteSpace(type.Id) || !IsIdentifier(type.Id))
                errors.Add($"Enemy type '{name}' needs an id of lowercase letters, digits and dashes.");
            if (string.IsNullOrWhiteSpace(type.DisplayName))
                errors.Add($"Enemy type '{name}' needs a display name.");
            if (type.Level != 1)
                errors.Add($"Enemy type '{name}' states level {type.Level}; this slice supports level 1 only.");

            var keys = type.OrangeCards.Select(card => card.Key ?? string.Empty)
                .Concat(type.PurpleCards.Select(card => card.Key ?? string.Empty)).ToArray();
            foreach (var duplicate in keys.GroupBy(key => key).Where(group => group.Count() > 1))
                errors.Add($"Enemy type '{name}' declares card key '{duplicate.Key}' more than once.");

            foreach (var card in type.OrangeCards) ValidateOrange(name, card, errors);
            foreach (var card in type.PurpleCards) ValidatePurple(name, card, errors);

            var hand = type.OrangeHand;
            var acting = type.OrangeCards.Where(card => !card.IsIdle).Sum(card => Math.Max(0, card.Copies));
            if (hand.Count != PlanLength)
                errors.Add($"Enemy type '{name}' holds {hand.Count} orange cards; it must hold {PlanLength} (idle cards included).");
            if (acting < MinimumActingOrangeCards)
                errors.Add($"Enemy type '{name}' has {acting} acting orange cards; it needs {MinimumActingOrangeCards} or {PlanLength}.");
            if (type.PurpleCards.Count > MaximumPurpleCards)
                errors.Add($"Enemy type '{name}' has {type.PurpleCards.Count} purple cards; at most {MaximumPurpleCards}.");
            if (type.Plans.Count == 0)
                errors.Add($"Enemy type '{name}' lists no plan.");

            var sortedHand = hand.OrderBy(key => key, StringComparer.Ordinal).ToArray();
            for (var index = 0; index < type.Plans.Count; index++)
            {
                var plan = type.Plans[index];
                if (!plan.OrderBy(key => key, StringComparer.Ordinal).SequenceEqual(sortedHand, StringComparer.Ordinal))
                {
                    errors.Add($"Enemy type '{name}' plan {index + 1} ({string.Join(" ", plan)}) must use each of " +
                        $"its orange cards exactly once ({string.Join(" ", hand)}).");
                }
            }

            foreach (var duplicate in type.Plans.GroupBy(plan => string.Join(" ", plan)).Where(group => group.Count() > 1))
                errors.Add($"Enemy type '{name}' lists plan {duplicate.Key} more than once.");
        }

        private static void ValidateOrange(string type, PveOrangeCard card, List<string> errors)
        {
            var label = $"Enemy type '{type}' card '{card.Key}'";
            if (string.IsNullOrWhiteSpace(card.Key) || !IsIdentifier(card.Key.ToLowerInvariant()))
                errors.Add($"Enemy type '{type}' has an orange card without a valid key.");
            if (string.IsNullOrWhiteSpace(card.Name)) errors.Add($"{label} needs a name.");
            if (card.Initiative < 1 || card.Initiative > MaximumValue)
                errors.Add($"{label} Initiative {card.Initiative} must be 1 to {MaximumValue}.");
            if (card.Copies < 1 || card.Copies > PlanLength)
                errors.Add($"{label} copies {card.Copies} must be 1 to {PlanLength}.");

            if (card.IsIdle)
            {
                if (card.Options.Count > 0 || card.AttackValue.HasValue || card.MoveValue.HasValue)
                    errors.Add($"{label} is idle and must state no option, Attack or Movement.");
                return;
            }

            if (card.Options.Count == 0) errors.Add($"{label} lists no option; mark it idle instead.");
            var attacks = card.Options.Where(option => option.Kind == PveOptionKind.Attack).ToArray();
            var moves = card.Options.Where(option => option.Kind == PveOptionKind.Move).ToArray();
            if (card.Options.Any(option => option.Kind != PveOptionKind.Attack && option.Kind != PveOptionKind.Move))
                errors.Add($"{label} lists an option that is neither move nor attack.");
            if (card.Options.Any(option => option.Target != PveTargetKind.Hero && option.Target != PveTargetKind.Minion))
                errors.Add($"{label} lists an option whose target is neither hero nor minion.");

            if (attacks.Length > 0)
            {
                if (!card.AttackValue.HasValue || card.AttackValue < 1 || card.AttackValue > MaximumValue)
                    errors.Add($"{label} attacks and needs an Attack value of 1 to {MaximumValue}.");
                if (card.AttackRange < 0 || card.AttackRange > MaximumValue)
                    errors.Add($"{label} Attack range {card.AttackRange} must be 0 (adjacent) to {MaximumValue}.");
            }
            else if (card.AttackValue.HasValue)
            {
                errors.Add($"{label} states an Attack value but lists no attack option.");
            }

            if (moves.Length > 0)
            {
                if (!card.MoveValue.HasValue || card.MoveValue < 1 || card.MoveValue > MaximumValue)
                    errors.Add($"{label} moves and needs a Movement value of 1 to {MaximumValue}.");
                if (moves.Any(option => option.StopDistance < 1 || option.StopDistance > MaximumValue))
                    errors.Add($"{label} lists a move whose stop distance is not 1 to {MaximumValue}.");
            }
            else if (card.MoveValue.HasValue)
            {
                errors.Add($"{label} states a Movement value but lists no move option.");
            }
        }

        private static void ValidatePurple(string type, PvePurpleCard card, List<string> errors)
        {
            var label = $"Enemy type '{type}' purple card '{card.Key}'";
            if (string.IsNullOrWhiteSpace(card.Key) || !IsIdentifier(card.Key.ToLowerInvariant()))
                errors.Add($"Enemy type '{type}' has a purple card without a valid key.");
            if (string.IsNullOrWhiteSpace(card.Name)) errors.Add($"{label} needs a name.");
            if (card.Initiative < 1 || card.Initiative > MaximumValue)
                errors.Add($"{label} Initiative {card.Initiative} must be 1 to {MaximumValue}.");
            if (card.Defense < 1 || card.Defense > MaximumValue)
                errors.Add($"{label} Defense {card.Defense} must be 1 to {MaximumValue}.");
        }

        private static void ValidateEncounter(
            PveEnemyCatalog catalog, PveEncounterDefinition encounter, BoardMapDefinition map, List<string> errors)
        {
            if (!encounter.MapReference.Equals(map.Reference))
            {
                errors.Add($"The encounter is laid out on '{encounter.MapId}@{encounter.MapRevision}' but the map " +
                    $"loaded is '{map.Reference}'.");
                return;
            }

            var playable = new HashSet<HexCoordinate>(map.PlayableCoordinates);
            var used = new Dictionary<HexCoordinate, string>();

            void Place(HexCoordinate coordinate, string what)
            {
                if (!playable.Contains(coordinate))
                    errors.Add($"{what} at {Describe(coordinate)} is not a playable space.");
                if (used.TryGetValue(coordinate, out var other))
                    errors.Add($"{what} at {Describe(coordinate)} shares its space with {other}.");
                else
                    used[coordinate] = what;
            }

            if (encounter.Creeps.Count == 0) errors.Add("The encounter places no creep to defend.");
            foreach (var creep in encounter.Creeps) Place(creep.Coordinate, "A creep");
            if (encounter.HeroPlacement.Count == 0) errors.Add("The encounter has no hero placement space.");
            foreach (var space in encounter.HeroPlacement) Place(space, "A hero placement space");
            if (!encounter.Spawns.Any(spawn => spawn.Round == 1))
                errors.Add("The encounter spawns no enemy in round 1.");
            foreach (var spawn in encounter.Spawns)
            {
                Place(spawn.Coordinate, $"A round-{spawn.Round} spawn");
                if (spawn.Round < 1) errors.Add($"A spawn at {Describe(spawn.Coordinate)} states round {spawn.Round}.");
                if (catalog.Find(spawn.EnemyTypeId) == null)
                    errors.Add($"A spawn at {Describe(spawn.Coordinate)} names unknown enemy type '{spawn.EnemyTypeId}'.");
            }
        }

        public static string Describe(HexCoordinate coordinate) => coordinate.Q + "," + coordinate.R;

        private static bool IsIdentifier(string value) =>
            value.Length > 0 && value.All(character =>
                (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '-');
    }
}
