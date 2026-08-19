using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Rules.Consequences
{
    /// <summary>
    /// Whether a minion stands on the defender's own side or the other one. p.14's modifier table is
    /// written entirely in these terms - "adjacent friendly melee or heavy minion", "adjacent enemy
    /// melee or heavy minion", "enemy ranged minion in radius of 2 spaces around you" - so the
    /// declaration says the same thing the page does rather than restating it as two team ids.
    /// </summary>
    public enum MinionDefenseRelationship
    {
        None = 0,
        Friendly = 1,
        Enemy = 2
    }

    /// <summary>
    /// One printed row of rulebook p.14's <em>Minion Defense Modifiers</em> box: who the minion belongs
    /// to relative to the defender, how far away it still counts, and what it adds.
    ///
    /// The distance is a maximum rather than an exact ring because that is how the page writes both
    /// forms - "adjacent" is a maximum of one, and "in radius of 2 spaces around you" is a maximum of
    /// two - and reading them as exact rings would silently drop the adjacent enemy ranged minion the
    /// second row is written to include.
    /// </summary>
    public readonly struct StandardMinionDefenseModifier
    {
        public StandardMinionDefenseModifier(
            MinionDefenseRelationship relationship,
            int maximumDistance,
            int amount)
        {
            if (relationship != MinionDefenseRelationship.Friendly &&
                relationship != MinionDefenseRelationship.Enemy)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(relationship), relationship,
                    "A p.14 Defense modifier is declared for a friendly or an enemy minion.");
            }

            if (maximumDistance < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumDistance), maximumDistance,
                    "A p.14 Defense modifier reaches at least the adjacent ring.");
            }

            if (amount == 0)
            {
                throw new ArgumentException(
                    "A p.14 Defense modifier that changes nothing is an absent row, not a zero one.",
                    nameof(amount));
            }

            Relationship = relationship;
            MaximumDistance = maximumDistance;
            Amount = amount;
        }

        public MinionDefenseRelationship Relationship { get; }

        public int MaximumDistance { get; }

        public int Amount { get; }
    }

    /// <summary>
    /// Everything the rules say about one standard minion subtype that the combat rules read as a
    /// number: p.13's coin value when it is defeated, and p.14's Defense modifiers it contributes.
    ///
    /// This exists because finding `F-ER2-09` recorded the two as verified rule content living in code
    /// - "the Heavy-versus-other coin literal and the standard-minion Defense modifier table live in
    /// code, one step from the banned name-branching category" - and scheduled the relocation for the
    /// milestone that supplies verified reward derivation. The relocation is what it says it is: the
    /// numbers move into a declared table that carries the page they came from, and the two consumers
    /// read the table rather than re-stating the rule as an `if`. No number changes; the pre-relocation
    /// and post-relocation answers are asserted equal for every subtype, distance and side.
    /// </summary>
    public sealed class StandardMinionProfile
    {
        public StandardMinionProfile(
            StandardMinionType minionType,
            int defeatCoins,
            IEnumerable<StandardMinionDefenseModifier> defenseModifiers)
        {
            if (minionType == StandardMinionType.None)
            {
                throw new ArgumentException(
                    "A standard minion profile is declared for a named subtype.", nameof(minionType));
            }

            if (defeatCoins <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(defeatCoins), defeatCoins,
                    "p.13 prices every standard minion at a positive number of coins.");
            }

            var modifiers = (defenseModifiers ?? throw new ArgumentNullException(nameof(defenseModifiers)))
                .ToArray();
            if (modifiers.Select(modifier => modifier.Relationship).Distinct().Count() != modifiers.Length)
            {
                throw new ArgumentException(
                    "p.14 states at most one modifier per subtype and relationship.",
                    nameof(defenseModifiers));
            }

            MinionType = minionType;
            DefeatCoins = defeatCoins;
            DefenseModifiers = Array.AsReadOnly(modifiers);
        }

        public StandardMinionType MinionType { get; }

        /// <summary>
        /// p.13: "Ranged and melee minions are worth 2 coins when defeated, while heavy minions are
        /// worth 4, but are protected by the heavy minion immunity."
        /// </summary>
        public int DefeatCoins { get; }

        /// <summary>p.14's <em>Minion Defense Modifiers</em> rows for this subtype.</summary>
        public IReadOnlyList<StandardMinionDefenseModifier> DefenseModifiers { get; }

        /// <summary>
        /// What this minion adds to a defender's Defense at the given distance, or zero when no row
        /// reaches that far. Summed over rows rather than returned from the first match, so a subtype
        /// that later declares two rows for one relationship is not silently half-read.
        /// </summary>
        public int DefenseModifierAt(MinionDefenseRelationship relationship, int distance)
        {
            if (distance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distance), distance, "A distance is not negative.");
            }

            var total = 0;
            foreach (var modifier in DefenseModifiers)
            {
                if (modifier.Relationship == relationship && distance <= modifier.MaximumDistance)
                {
                    total += modifier.Amount;
                }
            }

            return total;
        }
    }

    /// <summary>
    /// The declared standard-minion content the combat rules read, one profile per subtype.
    ///
    /// It is a catalog with one named instance rather than a static method per number, for the reason
    /// the component and capability catalogs are: what the rules say is content, and content is looked
    /// up. <see cref="Rulebook"/> is the second printing's table verbatim, and a consumer that meets a
    /// subtype the catalog does not declare gets an exception naming it rather than a default of zero -
    /// a minion worth no coins would be a silently wrong reward, not a missing feature.
    /// </summary>
    public sealed class StandardMinionProfileCatalog
    {
        private readonly Dictionary<StandardMinionType, StandardMinionProfile> profiles;

        public StandardMinionProfileCatalog(IEnumerable<StandardMinionProfile> declaredProfiles)
        {
            var declared = (declaredProfiles ?? throw new ArgumentNullException(nameof(declaredProfiles)))
                .ToArray();
            if (declared.Any(profile => profile == null))
            {
                throw new ArgumentException(
                    "A standard minion catalog declares no null profile.", nameof(declaredProfiles));
            }

            if (declared.Select(profile => profile.MinionType).Distinct().Count() != declared.Length)
            {
                throw new ArgumentException(
                    "A standard minion catalog declares each subtype once.", nameof(declaredProfiles));
            }

            profiles = declared.ToDictionary(profile => profile.MinionType);
        }

        /// <summary>
        /// Rulebook p.13's coin values and p.14's <em>Minion Defense Modifiers</em> box, for the three
        /// standard subtypes the official setup supplies.
        /// </summary>
        public static StandardMinionProfileCatalog Rulebook { get; } = new StandardMinionProfileCatalog(
            new[]
            {
                // p.13: melee and ranged are worth 2. p.14: "+1 for every adjacent friendly melee or
                // heavy minion", "-1 for every adjacent enemy melee or heavy minion".
                new StandardMinionProfile(
                    StandardMinionType.Melee,
                    2,
                    new[]
                    {
                        new StandardMinionDefenseModifier(MinionDefenseRelationship.Friendly, 1, 1),
                        new StandardMinionDefenseModifier(MinionDefenseRelationship.Enemy, 1, -1)
                    }),

                // p.14 gives a ranged minion only the enemy row - "-1 for every enemy ranged minion in
                // radius of 2 spaces around you" - so a friendly ranged minion adds nothing at any
                // distance, and the absence of the row is what says so.
                new StandardMinionProfile(
                    StandardMinionType.Ranged,
                    2,
                    new[]
                    {
                        new StandardMinionDefenseModifier(MinionDefenseRelationship.Enemy, 2, -1)
                    }),

                // p.13: "heavy minions are worth 4". p.14 names heavy alongside melee in both adjacent
                // rows, so its Defense rows are the melee ones and only the coin value differs.
                new StandardMinionProfile(
                    StandardMinionType.Heavy,
                    4,
                    new[]
                    {
                        new StandardMinionDefenseModifier(MinionDefenseRelationship.Friendly, 1, 1),
                        new StandardMinionDefenseModifier(MinionDefenseRelationship.Enemy, 1, -1)
                    })
            });

        public IReadOnlyList<StandardMinionProfile> Profiles => Array.AsReadOnly(
            profiles.Values.OrderBy(profile => (int)profile.MinionType).ToArray());

        public StandardMinionProfile Get(StandardMinionType minionType)
        {
            if (!profiles.TryGetValue(minionType, out var profile))
            {
                throw new KeyNotFoundException(
                    $"No standard minion profile is declared for '{minionType}'.");
            }

            return profile;
        }
    }
}
