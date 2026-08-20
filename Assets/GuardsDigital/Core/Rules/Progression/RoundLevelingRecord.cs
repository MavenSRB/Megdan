using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// Which heroes have purchased at least one level in the current round.
    ///
    /// This is a fact rather than a rule, and the distinction is the reason the type is named for what
    /// it holds. Rulebook p.8 pays its Pity Coin to "All players who have NOT leveled up this round",
    /// so the rule is the negation of this record and is asked where that page is quoted; nothing here
    /// knows what a Pity Coin is. That keeps the same division milestone 7.4 drew when it derived
    /// p.13's losing team from <c>TeamLifeState</c> rather than storing a second field beside it.
    ///
    /// It is authoritative persisted state rather than something re-derived, and that is forced rather
    /// than chosen. The leveling transition knows exactly who bought a level while it is running - its
    /// own completed-item record names each purchase - but that record is cleared when its step
    /// finishes, and p.8 puts Collect a Pity Coin in a later stage. A match can be saved sitting at that
    /// stage with leveling already done, so the answer has to survive a snapshot taken between two
    /// adjacent stages, which is exactly what Phase 7's durability rule requires of state a milestone
    /// adds.
    ///
    /// The record is sparse rather than one flag per hero, in the shape milestone 7.2 gave items and for
    /// the same reason: for most of a match nobody has levelled, so a per-hero section would write an
    /// entry for every hero in the match to say nothing at all. What the reader enforces instead is that
    /// every name is a participant and that no hero appears twice.
    /// </summary>
    public sealed class RoundLevelingRecord
    {
        private readonly HashSet<HeroId> participantIds;
        private readonly HashSet<HeroId> leveled = new HashSet<HeroId>();

        public RoundLevelingRecord(MatchParticipants participants)
            : this(participants, null)
        {
        }

        private RoundLevelingRecord(MatchParticipants participants, IEnumerable<HeroId> restoredHeroes)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            participantIds = new HashSet<HeroId>(participants.Heroes.Select(hero => hero.Id));

            if (restoredHeroes == null)
            {
                return;
            }

            foreach (var heroId in restoredHeroes)
            {
                if (!participantIds.Contains(heroId) || !leveled.Add(heroId))
                {
                    throw new ArgumentException(
                        "Every hero recorded as having levelled is a participant, named at most once.",
                        nameof(restoredHeroes));
                }
            }
        }

        /// <summary>
        /// The heroes that have bought a level this round, ordered by hero id so the order is a fact
        /// rather than an accident of the order the purchases happened in.
        /// </summary>
        public IReadOnlyList<HeroId> LeveledThisRound => Array.AsReadOnly(
            leveled.OrderBy(heroId => heroId.Value, StringComparer.Ordinal).ToArray());

        /// <summary>Whether this hero has bought at least one level in the current round.</summary>
        public bool HasLeveled(HeroId heroId)
        {
            if (!heroId.IsDefined || !participantIds.Contains(heroId))
            {
                throw new KeyNotFoundException($"Unknown hero ID '{heroId}'.");
            }

            return leveled.Contains(heroId);
        }

        /// <summary>
        /// Records that this hero bought a level. Deliberately idempotent: p.8 asks whether a hero
        /// levelled at all, not how often, so a hero buying a second level in the same round is the
        /// same fact rather than a repeated one.
        /// </summary>
        public void RecordLevelPurchased(HeroId heroId)
        {
            if (!heroId.IsDefined || !participantIds.Contains(heroId))
            {
                throw new KeyNotFoundException($"Unknown hero ID '{heroId}'.");
            }

            leveled.Add(heroId);
        }

        /// <summary>
        /// Forgets the round that just ended. p.8's question is asked of "this round", so the record has
        /// to start empty in the next one; a replay reaches the same point by replaying the same
        /// commands, and a restore reads whatever the payload recorded rather than this.
        /// </summary>
        public void BeginRound()
        {
            leveled.Clear();
        }

        public RoundLevelingRestoreState CaptureRestoreState() =>
            new RoundLevelingRestoreState(LeveledThisRound);

        public static RoundLevelingRecord Restore(
            MatchParticipants participants,
            RoundLevelingRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new RoundLevelingRecord(participants, state.LeveledThisRound);
        }
    }

    /// <summary>The persisted form of <see cref="RoundLevelingRecord"/>: the hero names, and nothing else.</summary>
    public sealed class RoundLevelingRestoreState
    {
        public RoundLevelingRestoreState(IEnumerable<HeroId> leveledThisRound)
        {
            LeveledThisRound = Array.AsReadOnly(
                (leveledThisRound ?? throw new ArgumentNullException(nameof(leveledThisRound))).ToArray());
        }

        public IReadOnlyList<HeroId> LeveledThisRound { get; }

        public void Deconstruct(out IReadOnlyList<HeroId> leveledThisRound)
        {
            leveledThisRound = LeveledThisRound;
        }
    }
}
