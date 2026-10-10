using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Pve.Content
{
    /// <summary>PVE-D07. What an orange option aims at.</summary>
    public enum PveTargetKind
    {
        None = 0,
        Hero = 1,
        Minion = 2
    }

    /// <summary>PVE-D07. The two simple options an orange card may list.</summary>
    public enum PveOptionKind
    {
        None = 0,
        Move = 1,
        Attack = 2
    }

    /// <summary>
    /// PVE-D07/D08. One entry of an orange card's ordered priority list: attack a target of a kind,
    /// or move toward the nearest target of a kind and stop at <see cref="StopDistance"/>.
    /// </summary>
    public sealed class PveCardOption
    {
        public PveCardOption(PveOptionKind kind, PveTargetKind target, int stopDistance = 1)
        {
            Kind = kind;
            Target = target;
            StopDistance = stopDistance;
        }

        public PveOptionKind Kind { get; }
        public PveTargetKind Target { get; }

        /// <summary>Move only: the distance to the target at which the move stops (adjacent is 1).</summary>
        public int StopDistance { get; }
    }

    /// <summary>
    /// PVE-D07. An orange card: an Initiative, at most one Attack (value, range) and one Movement
    /// value, and the ordered options the enemy tries. An idle card lists no option and no value.
    /// </summary>
    /// <remarks>
    /// All Attack options of one card share its single Attack, and all Move options its single
    /// Movement: the engine plays a card as one printed Attack and one printed Movement, and the
    /// options only decide which target the enemy chooses. Content stating two different Attack
    /// values on one card is refused rather than silently merged.
    /// </remarks>
    public sealed class PveOrangeCard
    {
        public PveOrangeCard(
            string key,
            string name,
            int initiative,
            int copies,
            int? attackValue,
            int attackRange,
            int? moveValue,
            IEnumerable<PveCardOption> options,
            bool isIdle = false)
        {
            Key = key;
            Name = name;
            Initiative = initiative;
            Copies = copies;
            AttackValue = attackValue;
            AttackRange = attackRange;
            MoveValue = moveValue;
            Options = Array.AsReadOnly((options ?? Enumerable.Empty<PveCardOption>()).ToArray());
            IsIdle = isIdle;
        }

        public string Key { get; }
        public string Name { get; }
        public int Initiative { get; }

        /// <summary>How many copies the enemy holds (Vorty's "2xB").</summary>
        public int Copies { get; }

        public int? AttackValue { get; }

        /// <summary>0 for an adjacent (non-Ranged) Attack; otherwise the printed Range.</summary>
        public int AttackRange { get; }

        public int? MoveValue { get; }
        public IReadOnlyList<PveCardOption> Options { get; }
        public bool IsIdle { get; }
    }

    /// <summary>PVE-D09. A purple Defense card.</summary>
    public sealed class PvePurpleCard
    {
        public PvePurpleCard(string key, string name, int initiative, int defense)
        {
            Key = key;
            Name = name;
            Initiative = initiative;
            Defense = defense;
        }

        public string Key { get; }
        public string Name { get; }
        public int Initiative { get; }
        public int Defense { get; }
    }

    /// <summary>PVE-D06/D07/D09. One enemy type: its cards, its listed four-turn plans and level.</summary>
    public sealed class PveEnemyType
    {
        public PveEnemyType(
            string id,
            string displayName,
            int level,
            IEnumerable<PveOrangeCard> orangeCards,
            IEnumerable<PvePurpleCard> purpleCards,
            IEnumerable<IEnumerable<string>> plans)
        {
            Id = id;
            DisplayName = displayName;
            Level = level;
            OrangeCards = Array.AsReadOnly((orangeCards ?? Enumerable.Empty<PveOrangeCard>()).ToArray());
            PurpleCards = Array.AsReadOnly((purpleCards ?? Enumerable.Empty<PvePurpleCard>()).ToArray());
            Plans = Array.AsReadOnly((plans ?? Enumerable.Empty<IEnumerable<string>>())
                .Select(plan => (IReadOnlyList<string>)Array.AsReadOnly(
                    (plan ?? Enumerable.Empty<string>()).ToArray()))
                .ToArray());
        }

        public string Id { get; }
        public string DisplayName { get; }
        public int Level { get; }
        public IReadOnlyList<PveOrangeCard> OrangeCards { get; }
        public IReadOnlyList<PvePurpleCard> PurpleCards { get; }

        /// <summary>Each plan lists, per turn, the key of the orange card played that turn.</summary>
        public IReadOnlyList<IReadOnlyList<string>> Plans { get; }

        /// <summary>The orange cards this type holds, one key per copy, in declared order.</summary>
        public IReadOnlyList<string> OrangeHand =>
            OrangeCards.SelectMany(card => Enumerable.Repeat(card.Key, Math.Max(0, card.Copies)))
                .ToArray();

        public PveOrangeCard FindOrange(string key) =>
            OrangeCards.FirstOrDefault(card => string.Equals(card.Key, key, StringComparison.Ordinal));
    }

    /// <summary>PVE-D15. Every enemy type the encounter may spawn, loaded from content data.</summary>
    public sealed class PveEnemyCatalog
    {
        public PveEnemyCatalog(string contentStatus, IEnumerable<PveEnemyType> types)
        {
            ContentStatus = contentStatus;
            Types = Array.AsReadOnly((types ?? Enumerable.Empty<PveEnemyType>()).ToArray());
        }

        /// <summary>"placeholder-test" for the first slice's data: not approved balance.</summary>
        public string ContentStatus { get; }

        public IReadOnlyList<PveEnemyType> Types { get; }

        public PveEnemyType Find(string id) =>
            Types.FirstOrDefault(type => string.Equals(type.Id, id, StringComparison.Ordinal));
    }
}
