using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Victory
{
    /// <summary>
    /// Milestone 8.53. One hero's line of the match scoreboard the victory screen prints: what that
    /// hero did over the whole match, not over the current round.
    /// </summary>
    /// <remarks>
    /// <para><b>Level is deliberately absent.</b> p.15's Level is already authoritative on
    /// <c>HeroProgressionState</c> and already published as <c>HeroProgressionProjection.Level</c>,
    /// and a second record of it here could only agree with that one or be wrong about it - the
    /// argument milestone 7.3 made for deriving the unlocked Ultimate from the card's own position
    /// and milestone 7.4 made for reading the losing team off its Life counters.</para>
    /// <para><b>Coins EARNED is not coins HELD.</b> The balance on the progression projection goes
    /// down at every p.15 purchase; this number only ever goes up, because the column the mockup
    /// asks for is what a hero collected over the match rather than what they have left.</para>
    /// </remarks>
    public sealed class HeroScoreboardEntry : IEquatable<HeroScoreboardEntry>
    {
        public HeroScoreboardEntry(
            HeroId heroId,
            int kills,
            int deaths,
            int attacksAtHeroes,
            int timesDefended,
            int minionsKilled,
            int coinsEarned)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("A scoreboard line names the hero it is about.", nameof(heroId));
            }

            if (kills < 0 || deaths < 0 || attacksAtHeroes < 0 || timesDefended < 0 ||
                minionsKilled < 0 || coinsEarned < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(kills), "Every scoreboard column counts occurrences and cannot be negative.");
            }

            HeroId = heroId;
            Kills = kills;
            Deaths = deaths;
            AttacksAtHeroes = attacksAtHeroes;
            TimesDefended = timesDefended;
            MinionsKilled = minionsKilled;
            CoinsEarned = coinsEarned;
        }

        public HeroId HeroId { get; }

        /// <summary>
        /// Enemy heroes this hero defeated: the acting hero of every recorded p.13 hero Defeat.
        /// An effect-invoked Defeat - Sabina's Covering Fire, Electroblast's rider - credits the
        /// hero whose text asked for it, because that is the actor the Defeat itself records.
        /// </summary>
        public int Kills { get; }

        /// <summary>Times this hero was the subject of a recorded p.13 hero Defeat.</summary>
        public int Deaths { get; }

        /// <summary>
        /// Attacks this hero declared against an enemy HERO, counted once per complete p.14 order
        /// and counted whether the order ended in a Defeat or in a successful defense. A repeated
        /// Attack and each sequence of a modal Attack are their own orders, because each stages its
        /// own p.14 comparison.
        /// </summary>
        public int AttacksAtHeroes { get; }

        /// <summary>
        /// Attacks against this hero that ended <c>AttackSuccessfullyDefended</c> - p.14's step 6
        /// answered and the hero left standing.
        /// </summary>
        public int TimesDefended { get; }

        /// <summary>
        /// Minions this hero defeated, by an Attack or by card text that asked for the Defeat.
        /// </summary>
        public int MinionsKilled { get; }

        /// <summary>
        /// Every coin this hero COLLECTED: p.13's Defeat and assist coins, a minion's bounty, p.8's
        /// Pity Coin and any coin card text granted. Coins spent at p.15's Level Up are not
        /// subtracted, and a coin one hero hands another is not counted twice - a transfer moves
        /// coins that were already collected once.
        /// </summary>
        public int CoinsEarned { get; }

        public bool Equals(HeroScoreboardEntry other) =>
            other != null && HeroId.Equals(other.HeroId) && Kills == other.Kills &&
            Deaths == other.Deaths && AttacksAtHeroes == other.AttacksAtHeroes &&
            TimesDefended == other.TimesDefended && MinionsKilled == other.MinionsKilled &&
            CoinsEarned == other.CoinsEarned;

        public override bool Equals(object obj) => Equals(obj as HeroScoreboardEntry);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = HeroId.GetHashCode();
                hash = (hash * 397) ^ Kills;
                hash = (hash * 397) ^ Deaths;
                hash = (hash * 397) ^ AttacksAtHeroes;
                hash = (hash * 397) ^ TimesDefended;
                hash = (hash * 397) ^ MinionsKilled;
                hash = (hash * 397) ^ CoinsEarned;
                return hash;
            }
        }

        public override string ToString() =>
            $"{HeroId.Value}: kills={Kills};deaths={Deaths};attacks={AttacksAtHeroes};" +
            $"defended={TimesDefended};minions={MinionsKilled};coins={CoinsEarned}";
    }

    /// <summary>
    /// Milestone 8.53. The match's own per-hero record of what each hero did, accumulated from the
    /// typed facts the seat-public match log is written from and carried in the authoritative
    /// snapshot so every seat and every restored match reads the same board.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this is accumulated state rather than a derivation.</b> Everything on it could
    /// be counted out of the rule-event log - it is counted out of exactly the facts that log is
    /// written from - but that log is not a match-long source a reader can trust for a TOTAL: the
    /// seat-public projection over it is bounded to
    /// <c>MatchLogProjection.RetainedEntries</c> recent lines by construction, because a bounded
    /// window is what lets one wire frame carry the whole thing to a seat that just arrived. A
    /// scoreboard derived from a window would show a player fewer kills the longer they played,
    /// which is the one thing a victory screen must not do. So the totals are accumulated once, by
    /// the authority, and persisted.</para>
    /// <para><b>Two seams, each the one place its fact already exists.</b> The five combat columns
    /// are folded from the committed rule events in <c>MatchAggregate</c>'s single commit boundary -
    /// the same call that appends the rule-event log the seat-public rows are derived from - so a
    /// fact counted here is a fact that log carries, by construction rather than by agreement.
    /// <see cref="CoinsEarned"/> cannot be folded there because no event carries the AMOUNT: p.13's
    /// reward facts name the Defeat and not its payout, and a coin granted by card text reaches the
    /// stream only as an applied node. It is therefore recorded by
    /// <see cref="Consequences.HeroCoinLedger"/> itself, which is the one place in the build where a
    /// coin is credited to a hero at all - so a coin source added later is counted without touching
    /// this type.</para>
    /// <para><b>Deterministic from commands.</b> Nothing here reads a clock, a seat or a random
    /// draw: every column is a count of committed facts. A retained-prefix Undo replays its prefix
    /// into a fresh aggregate and re-folds the same events in the same order, and a snapshot restore
    /// puts back the totals the payload recorded rather than re-deriving them.</para>
    /// </remarks>
    public sealed class MatchScoreboard
    {
        private readonly Dictionary<HeroId, Line> lines;

        public MatchScoreboard(MatchParticipants participants)
            : this(participants, null)
        {
        }

        private MatchScoreboard(
            MatchParticipants participants,
            IEnumerable<HeroScoreboardEntry> restoredEntries)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            lines = participants.Heroes.ToDictionary(hero => hero.Id, _ => new Line());

            if (restoredEntries == null)
            {
                return;
            }

            var restored = restoredEntries.ToArray();
            if (restored.Any(entry => entry == null) ||
                restored.Length != lines.Count ||
                restored.Select(entry => entry.HeroId).Distinct().Count() != restored.Length ||
                restored.Any(entry => !lines.ContainsKey(entry.HeroId)))
            {
                throw new ArgumentException(
                    "A restored scoreboard must cover every participant hero exactly once.",
                    nameof(restoredEntries));
            }

            foreach (var entry in restored)
            {
                lines[entry.HeroId] = new Line
                {
                    Kills = entry.Kills,
                    Deaths = entry.Deaths,
                    AttacksAtHeroes = entry.AttacksAtHeroes,
                    TimesDefended = entry.TimesDefended,
                    MinionsKilled = entry.MinionsKilled,
                    CoinsEarned = entry.CoinsEarned
                };
            }
        }

        /// <summary>
        /// Every participant hero's line, ordered by hero id so that the host's board and a
        /// restored or remote reading of it are the same list in the same order.
        /// </summary>
        public IReadOnlyList<HeroScoreboardEntry> Entries => Array.AsReadOnly(
            lines
                .OrderBy(pair => pair.Key.Value, StringComparer.Ordinal)
                .Select(pair => new HeroScoreboardEntry(
                    pair.Key,
                    pair.Value.Kills,
                    pair.Value.Deaths,
                    pair.Value.AttacksAtHeroes,
                    pair.Value.TimesDefended,
                    pair.Value.MinionsKilled,
                    pair.Value.CoinsEarned))
                .ToArray());

        public MatchScoreboardRestoreState CaptureRestoreState() =>
            new MatchScoreboardRestoreState(Entries);

        public static MatchScoreboard Restore(
            MatchParticipants participants,
            MatchScoreboardRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new MatchScoreboard(participants, state.Entries);
        }

        /// <summary>One hero's line, or the all-zero line for a hero nothing has happened to yet.</summary>
        public HeroScoreboardEntry For(HeroId heroId)
        {
            if (!TryGet(heroId, out var entry))
            {
                throw new KeyNotFoundException($"Unknown hero ID '{heroId}'.");
            }

            return entry;
        }

        public bool TryGet(HeroId heroId, out HeroScoreboardEntry entry)
        {
            entry = null;
            if (!heroId.IsDefined || !lines.TryGetValue(heroId, out var line))
            {
                return false;
            }

            entry = new HeroScoreboardEntry(
                heroId, line.Kills, line.Deaths, line.AttacksAtHeroes,
                line.TimesDefended, line.MinionsKilled, line.CoinsEarned);
            return true;
        }

        /// <summary>
        /// Counts one committed rule event. Called once per event at the aggregate's single commit
        /// boundary, beside the append that writes the rule-event log, so that what this counts and
        /// what that log carries are the same facts rather than two readings of the match.
        /// </summary>
        /// <remarks>
        /// <para>Every arm reads a typed <see cref="CardActionResolutionEvent"/> field. Nothing here
        /// parses a narration string, and nothing here re-derives a rule: whether an attack was
        /// defended, who defeated whom and which minion died are decisions the card-action authority
        /// has already made and published.</para>
        /// <para>A fact about a hero this scoreboard does not know is ignored rather than thrown on.
        /// It cannot happen in a composed match - every actor and every target is a participant -
        /// and the commit boundary is the wrong place to discover otherwise: an exception here would
        /// turn a counted column into a lost command.</para>
        /// </remarks>
        public void Observe(RoundResolutionEvent ruleEvent)
        {
            if (ruleEvent.Kind != RoundResolutionEventKind.TurnResolutionProgressed)
            {
                return;
            }

            var action = ruleEvent.TurnEvent.CardActionEvent;
            switch (action.Kind)
            {
                // p.14's order has been staged against a HERO: the target is locked, the defense
                // response is being asked for, and the attack is going to happen whatever the
                // comparison says. Counted here rather than at the outcome so that an order is
                // counted once and counted whichever way it ends. A minion attack rides the same
                // fact with TargetMinionId set instead, and is not an attack at a hero.
                case CardActionResolutionEventKind.AttackStarted:
                    if (action.TargetHeroId.IsDefined)
                    {
                        Add(action.ActorId, line => line.AttacksAtHeroes++);
                    }

                    return;

                // p.14 step 6 answered and the defender still standing.
                case CardActionResolutionEventKind.AttackSuccessfullyDefended:
                    Add(action.TargetHeroId, line => line.TimesDefended++);
                    return;

                // p.13's Defeat, however it was caused. The event names the defeated hero and the
                // hero whose action asked for the Defeat, which for an effect-invoked one is the
                // acting hero of the text rather than an attacker.
                case CardActionResolutionEventKind.HeroDefeatRecorded:
                    Add(action.TargetHeroId, line => line.Deaths++);
                    if (!action.ActorId.Equals(action.TargetHeroId))
                    {
                        Add(action.ActorId, line => line.Kills++);
                    }

                    return;

                case CardActionResolutionEventKind.MinionDefeatRecorded:
                    Add(action.ActorId, line => line.MinionsKilled++);
                    return;
            }
        }

        /// <summary>
        /// Counts coins a hero collected. Called by <see cref="Consequences.HeroCoinLedger"/> from
        /// the two operations that can raise a balance - a p.13 Defeat's award and a grant - so that
        /// every coin source in the build, and every one added after this, is counted at the single
        /// place the coin is actually credited.
        /// </summary>
        public void RecordCoinsEarned(HeroId heroId, int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount), amount, "A collected coin amount is a positive number of coins.");
            }

            Add(heroId, line => line.CoinsEarned += amount);
        }

        private void Add(HeroId heroId, Action<Line> mutate)
        {
            if (heroId.IsDefined && lines.TryGetValue(heroId, out var line))
            {
                mutate(line);
            }
        }

        private sealed class Line
        {
            public int Kills;
            public int Deaths;
            public int AttacksAtHeroes;
            public int TimesDefended;
            public int MinionsKilled;
            public int CoinsEarned;
        }
    }
}
