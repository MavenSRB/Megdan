using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Consequences
{
    /// <summary>
    /// One typed hero-Defeat fact retained for the current round.
    /// </summary>
    /// <remarks>
    /// The outcome identity is kept beside the hero identity so this record never has to parse a
    /// resolution id or infer a Defeat from cumulative coins or Life counters. Those other records are
    /// match-scoped and answer different questions; this one is deliberately round-scoped.
    /// </remarks>
    public sealed class HeroDefeatHistoryEntry
    {
        public HeroDefeatHistoryEntry(DefeatOutcomeId outcomeId, HeroId defeatedHeroId)
        {
            if (!outcomeId.IsDefined)
            {
                throw new ArgumentException(
                    "A hero-Defeat history entry requires a defined outcome ID.", nameof(outcomeId));
            }

            if (!defeatedHeroId.IsDefined)
            {
                throw new ArgumentException(
                    "A hero-Defeat history entry requires a defined defeated hero.",
                    nameof(defeatedHeroId));
            }

            OutcomeId = outcomeId;
            DefeatedHeroId = defeatedHeroId;
        }

        public DefeatOutcomeId OutcomeId { get; }

        public HeroId DefeatedHeroId { get; }
    }

    /// <summary>
    /// Authoritative hero-Defeat outcomes in the current round, in resolution order.
    /// </summary>
    /// <remarks>
    /// Brogan's War Drummer and Master Skald ask whether any hero was defeated "this round". A
    /// diagnostic event log cannot answer a rule, while the coin ledger and Life counters are
    /// cumulative. This sparse record is therefore persisted with the round and cleared only at the
    /// round boundary. It records completed Defeats, including one whose removal was prevented: p.13's
    /// Defeat list is complete in that case and <c>HeroDefeatFactKind.RemovalPrevented</c> says so.
    /// </remarks>
    public sealed class RoundHeroDefeatHistory
    {
        private readonly HashSet<HeroId> participantIds;
        private readonly List<HeroDefeatHistoryEntry> entries =
            new List<HeroDefeatHistoryEntry>();

        public RoundHeroDefeatHistory(MatchParticipants participants)
            : this(participants, null)
        {
        }

        private RoundHeroDefeatHistory(
            MatchParticipants participants,
            IEnumerable<HeroDefeatHistoryEntry> restoredEntries)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            participantIds = new HashSet<HeroId>(participants.Heroes.Select(hero => hero.Id));

            if (restoredEntries == null)
            {
                return;
            }

            var outcomeIds = new HashSet<DefeatOutcomeId>();
            foreach (var entry in restoredEntries)
            {
                if (entry == null ||
                    !participantIds.Contains(entry.DefeatedHeroId) ||
                    !outcomeIds.Add(entry.OutcomeId))
                {
                    throw new ArgumentException(
                        "Current-round hero-Defeat history requires non-null entries, participating " +
                        "heroes, and unique outcome IDs.",
                        nameof(restoredEntries));
                }

                entries.Add(entry);
            }
        }

        public IReadOnlyList<HeroDefeatHistoryEntry> Entries => entries.AsReadOnly();

        /// <summary>
        /// Records a completed p.13 hero Defeat without interpreting its opaque outcome identity.
        /// </summary>
        public void Record(DefeatOutcome outcome)
        {
            if (outcome == null) throw new ArgumentNullException(nameof(outcome));
            var defeatedHeroId = new HeroId(outcome.TargetId.Value);
            if (!participantIds.Contains(defeatedHeroId))
            {
                throw new ArgumentException(
                    "A current-round hero-Defeat must target a participating hero.", nameof(outcome));
            }

            if (entries.Any(entry => entry.OutcomeId.Equals(outcome.Id)))
            {
                throw new InvalidOperationException(
                    $"Hero-Defeat outcome '{outcome.Id}' was already recorded this round.");
            }

            entries.Add(new HeroDefeatHistoryEntry(outcome.Id, defeatedHeroId));
        }

        /// <summary>Starts the next round with no Defeat inherited from the one that ended.</summary>
        public void BeginRound()
        {
            entries.Clear();
        }

        public RoundHeroDefeatHistoryRestoreState CaptureRestoreState() =>
            new RoundHeroDefeatHistoryRestoreState(Entries);

        public static RoundHeroDefeatHistory Restore(
            MatchParticipants participants,
            RoundHeroDefeatHistoryRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new RoundHeroDefeatHistory(participants, state.Entries);
        }
    }

    /// <summary>The persisted ordered entries of <see cref="RoundHeroDefeatHistory"/>.</summary>
    public sealed class RoundHeroDefeatHistoryRestoreState
    {
        public RoundHeroDefeatHistoryRestoreState(IEnumerable<HeroDefeatHistoryEntry> entries)
        {
            Entries = Array.AsReadOnly(
                (entries ?? throw new ArgumentNullException(nameof(entries))).ToArray());
        }

        public IReadOnlyList<HeroDefeatHistoryEntry> Entries { get; }

        public void Deconstruct(out IReadOnlyList<HeroDefeatHistoryEntry> entries)
        {
            entries = Entries;
        }
    }

    /// <summary>The typed round-history predicate printed by Brogan's coin-granting cards.</summary>
    public static class HeroDefeatedThisRoundQuery
    {
        public static bool IsSatisfied(RoundHeroDefeatHistory history) =>
            (history ?? throw new ArgumentNullException(nameof(history))).Entries.Count > 0;
    }
}
