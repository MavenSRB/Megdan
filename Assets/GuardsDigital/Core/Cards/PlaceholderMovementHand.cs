using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    public sealed class PlaceholderMovementHand
    {
        private static readonly SyntheticCardProfile[] RedProfiles =
        {
            new SyntheticCardProfile("red-needle", "Red Needle", 13, AttackRangeKind.NonRanged, 2, 0, 2, 1),
            new SyntheticCardProfile("red-bulwark", "Red Bulwark", 11, AttackRangeKind.NonRanged, 3, 0, 2, 3),
            new SyntheticCardProfile("red-lancer", "Red Lancer", 9, AttackRangeKind.Ranged, 2, 3, 3, 1),
            new SyntheticCardProfile("red-pursuit", "Red Pursuit", 7, AttackRangeKind.NonRanged, 4, 0, 3, 2),
            new SyntheticCardProfile("red-bombard", "Red Bombard", 5, AttackRangeKind.Ranged, 3, 2, 2, 2)
        };

        private static readonly SyntheticCardProfile[] BlueProfiles =
        {
            new SyntheticCardProfile("blue-feint", "Blue Feint", 12, AttackRangeKind.NonRanged, 2, 0, 3, 2),
            new SyntheticCardProfile("blue-sentinel", "Blue Sentinel", 11, AttackRangeKind.NonRanged, 3, 0, 2, 4),
            new SyntheticCardProfile("blue-scout-shot", "Blue Scout Shot", 8, AttackRangeKind.Ranged, 2, 2, 4, 1),
            new SyntheticCardProfile("blue-hammer", "Blue Hammer", 6, AttackRangeKind.NonRanged, 5, 0, 2, 1),
            new SyntheticCardProfile("blue-longbow", "Blue Longbow", 4, AttackRangeKind.Ranged, 3, 3, 3, 2)
        };

        private PlaceholderMovementHand(
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances)
        {
            Definitions = definitions;
            Instances = instances;
        }

        public IReadOnlyList<CardDefinition> Definitions { get; }

        public IReadOnlyList<CardInstance> Instances { get; }

        public static PlaceholderMovementHand Create(HeroId ownerId)
        {
            return Create(ownerId, RedProfiles, ownerId.Value);
        }

        public static PlaceholderMovementHand CreateRed(HeroId ownerId)
        {
            return Create(ownerId, RedProfiles, null);
        }

        public static PlaceholderMovementHand CreateBlue(HeroId ownerId)
        {
            return Create(ownerId, BlueProfiles, null);
        }

        internal static PlaceholderMovementHand Restore(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (instances == null) throw new ArgumentNullException(nameof(instances));
            var definitionArray = definitions.ToArray();
            var instanceArray = instances.ToArray();
            if (definitionArray.Length == 0 || instanceArray.Length == 0 ||
                instanceArray.Select(instance => instance.OwnerId).Distinct().Count() != 1 ||
                instanceArray.Any(instance => !definitionArray.Any(definition =>
                    definition.Reference.Equals(instance.DefinitionReference))))
            {
                throw new ArgumentException("A restored placeholder hand requires one owner's definition-backed cards.");
            }

            return new PlaceholderMovementHand(
                Array.AsReadOnly(definitionArray),
                Array.AsReadOnly(instanceArray));
        }

        private static PlaceholderMovementHand Create(
            HeroId ownerId,
            IReadOnlyList<SyntheticCardProfile> profiles,
            string instancePrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("A placeholder hand requires a defined owner.", nameof(ownerId));
            }

            var definitions = new CardDefinition[profiles.Count];
            var instances = new CardInstance[profiles.Count];
            for (var index = 0; index < profiles.Count; index++)
            {
                var profile = profiles[index];
                var definition = CreateDefinition(profile);
                definitions[index] = definition;
                instances[index] = CardInstance.CreateInHand(
                    new CardInstanceId(
                        string.IsNullOrWhiteSpace(instancePrefix)
                            ? $"{profile.Id}-card"
                            : $"{instancePrefix}-{profile.Id}-card"),
                    definition.Reference,
                    ownerId);
            }

            return new PlaceholderMovementHand(
                Array.AsReadOnly(definitions),
                Array.AsReadOnly(instances));
        }

        private static CardDefinition CreateDefinition(SyntheticCardProfile profile)
        {
            var actions = new[]
            {
                CreateAttackAction(profile.AttackRange, profile.AttackValue, profile.RangeValue),
                CreateMovementAction(profile.MovementValue),
                CreateDefenseAction(profile.DefenseValue)
            };

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{profile.Id}"),
                "phase-5-1a-rev-3",
                $"TEST - {profile.DisplayName}",
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                actions);
        }

        private static ActionDefinition CreateMovementAction(int value)
        {
            return new ActionDefinition(
                new ActionDefinitionId("secondary-movement"),
                ActionKind.Movement,
                ActionDefinitionOrigin.PrintedSecondary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));
        }

        private static ActionDefinition CreateAttackAction(
            AttackRangeKind attackRange,
            int attackValue,
            int rangeValue)
        {
            var values = attackRange == AttackRangeKind.Ranged
                ? new[]
                {
                    new PrintedValueDefinition(PrintedValueChannel.Attack, attackValue),
                    new PrintedValueDefinition(PrintedValueChannel.Range, rangeValue)
                }
                : new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, attackValue) };
            return new ActionDefinition(
                new ActionDefinitionId(
                    attackRange == AttackRangeKind.Ranged
                        ? "primary-ranged-attack"
                        : "primary-attack"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                values,
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                attackRange,
                default);
        }

        private static ActionDefinition CreateDefenseAction(int value)
        {
            return new ActionDefinition(
                new ActionDefinitionId("defense-reaction"),
                ActionKind.Defense,
                ActionDefinitionOrigin.Reaction,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, value) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));
        }

        private sealed class SyntheticCardProfile
        {
            public SyntheticCardProfile(
                string id,
                string displayName,
                int initiative,
                AttackRangeKind attackRange,
                int attackValue,
                int rangeValue,
                int movementValue,
                int defenseValue)
            {
                Id = id;
                DisplayName = displayName;
                Initiative = initiative;
                AttackRange = attackRange;
                AttackValue = attackValue;
                RangeValue = rangeValue;
                MovementValue = movementValue;
                DefenseValue = defenseValue;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public AttackRangeKind AttackRange { get; }
            public int AttackValue { get; }
            public int RangeValue { get; }
            public int MovementValue { get; }
            public int DefenseValue { get; }
        }
    }
}
