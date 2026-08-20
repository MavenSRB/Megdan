using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// One hero's current Level, as a value that cannot be an illegal one.
    ///
    /// The bounds are the rulebook's. p.15 opens with "Your hero starts the game at level 1", and its
    /// Hero Level table runs 1 to 8 with a Level Up cost for each step and no row above eight - p.16
    /// says the same thing from the other end, that once the Ultimate is unlocked "you cannot gain any
    /// additional levels, but can still gain coins".
    /// </summary>
    public readonly struct HeroLevel : IEquatable<HeroLevel>
    {
        /// <summary>p.15: "Your hero starts the game at level 1."</summary>
        public const int Minimum = 1;

        /// <summary>p.15's Hero Level table ends at 8, and p.16 confirms nothing is gained above it.</summary>
        public const int Maximum = 8;

        public HeroLevel(int value)
        {
            if (value < Minimum || value > Maximum)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value), value, $"A hero Level is between {Minimum} and {Maximum} (rulebook p.15).");
            }

            Value = value;
        }

        public int Value { get; }

        /// <summary>
        /// False only for the default-constructed value, whose <see cref="Value"/> is zero. A struct
        /// can always be default-constructed, so the type says which values it did not vouch for
        /// rather than pretending the constructor is the only way in.
        /// </summary>
        public bool IsDefined => Value >= Minimum && Value <= Maximum;

        public static HeroLevel Starting => new HeroLevel(Minimum);

        public bool Equals(HeroLevel other) => Value == other.Value;

        public override bool Equals(object obj) => obj is HeroLevel other && Equals(other);

        public override int GetHashCode() => Value;

        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Every participating hero's current Level, as authoritative match state.
    ///
    /// Milestone 7.1 added the state and its persistence and deliberately added no way to change it,
    /// because p.15 changes a Level by purchasing one at the end of a round and the purchase - its
    /// mandatory ordering, its coin cost, the upgrade it forces and the pending choices it opens - was
    /// milestone 7.5's. Milestone 7.5 is that milestone, and <see cref="TryRaiseLevel"/> is the one
    /// transition it adds here: the Level moves by one, upward, and only within p.15's table. Every
    /// other half of the purchase - whether it is owed, what it costs, what it upgrades - belongs to
    /// the rules that run p.15's step, exactly as the card lifecycle's three upgrade transitions decide
    /// only the shape of a move and never whether it is due.
    ///
    /// It is nonetheless persisted now rather than later, because Phase 7's durability rule requires
    /// the milestone that adds authoritative state to close its persistence boundary in the same
    /// milestone and says a later integration gate is not substitute evidence. What that buys is
    /// concrete: <see cref="Restore"/> refuses a payload whose Levels do not cover exactly the
    /// participating heroes, so a saved match cannot come back with a hero whose Level nobody recorded,
    /// and the round-trip carries whatever Level each hero has rather than re-deriving Level 1 - which
    /// is the difference between persisting state and persisting a default.
    ///
    /// Shaped after <c>HeroCoinLedger</c>'s coin balances, which is the closest existing thing:
    /// per-hero authoritative numbers, covered exactly once, ordered by hero id so the wire is stable.
    /// </summary>
    public sealed class HeroProgressionState
    {
        private readonly Dictionary<HeroId, HeroLevel> levels;

        public HeroProgressionState(MatchParticipants participants)
            : this(participants, null)
        {
        }

        private HeroProgressionState(
            MatchParticipants participants,
            IEnumerable<KeyValuePair<HeroId, HeroLevel>> restoredLevels)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            levels = participants.Heroes.ToDictionary(hero => hero.Id, hero => HeroLevel.Starting);

            if (restoredLevels == null)
            {
                return;
            }

            var restored = restoredLevels.ToArray();
            if (restored.Length != levels.Count ||
                restored.Select(entry => entry.Key).Distinct().Count() != restored.Length ||
                restored.Any(entry => !levels.ContainsKey(entry.Key)) ||
                restored.Any(entry => !entry.Value.IsDefined))
            {
                throw new ArgumentException(
                    "Restored hero Levels must cover every participant hero exactly once with a legal Level.",
                    nameof(restoredLevels));
            }

            foreach (var entry in restored)
            {
                levels[entry.Key] = entry.Value;
            }
        }

        /// <summary>Every hero's current Level, ordered by hero id so the order is a fact, not an accident.</summary>
        public IReadOnlyList<KeyValuePair<HeroId, HeroLevel>> Levels => Array.AsReadOnly(
            levels.OrderBy(entry => entry.Key.Value, StringComparer.Ordinal).ToArray());

        public HeroLevel GetLevel(HeroId heroId)
        {
            if (!heroId.IsDefined || !levels.TryGetValue(heroId, out var level))
            {
                throw new KeyNotFoundException($"Unknown hero ID '{heroId}'.");
            }

            return level;
        }

        /// <summary>
        /// p.15's "slide the Level indicator card up to the corresponding number": one hero's Level
        /// moves to the next row of the table, and nothing else about the purchase happens here.
        ///
        /// It refuses rather than throws in exactly one case - a hero already at p.15's last row - so
        /// that "there is no level above this one" is an answer the caller can act on, which is what
        /// p.16's "you cannot gain any additional levels" needs it to be. An unknown hero still throws,
        /// because that is a caller defect rather than a rules outcome.
        ///
        /// One step at a time, by construction. p.15 states the consequence of the alternative outright
        /// - "You cannot skip a level. A level 1 hero needs 3 coins to reach level 3" - so a method that
        /// took a destination Level could express the purchase the page forbids, and the caller that
        /// buys several does so by asking for several.
        /// </summary>
        public bool TryRaiseLevel(HeroId heroId, out HeroLevel raisedLevel)
        {
            var current = GetLevel(heroId);
            if (!HeroLevelSchedule.HasNextLevel(current))
            {
                raisedLevel = current;
                return false;
            }

            raisedLevel = HeroLevelSchedule.NextLevel(current);
            levels[heroId] = raisedLevel;
            return true;
        }

        public HeroProgressionRestoreState CaptureRestoreState() =>
            new HeroProgressionRestoreState(Levels);

        public static HeroProgressionState Restore(
            MatchParticipants participants,
            HeroProgressionRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new HeroProgressionState(participants, state.Levels);
        }
    }

    /// <summary>The persisted form of <see cref="HeroProgressionState"/>: one Level per hero, and nothing else.</summary>
    public sealed class HeroProgressionRestoreState
    {
        public HeroProgressionRestoreState(IEnumerable<KeyValuePair<HeroId, HeroLevel>> levels)
        {
            Levels = Array.AsReadOnly(
                (levels ?? throw new ArgumentNullException(nameof(levels))).ToArray());
        }

        public IReadOnlyList<KeyValuePair<HeroId, HeroLevel>> Levels { get; }

        public void Deconstruct(out IReadOnlyList<KeyValuePair<HeroId, HeroLevel>> levels)
        {
            levels = Levels;
        }
    }
}
