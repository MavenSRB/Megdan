using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z61. THE SILENT SKIP: rulebook p.13's second line has a match-log row.
    ///
    /// <para><b>What this fixture is about.</b> The 2026-09-06 six-player playtest reported that a
    /// human playing Brogan played a Green card and "the game just passed him over" - he was never
    /// asked for anything, and the in-game match log showed nothing at all for his resolution. The
    /// engine was right. p.13 says "If the defeated hero had an unresolved card, it is resolved,
    /// without performing an action", and
    /// <see cref="AttackDefenseCardActionStrategyBackend"/> has applied that line since milestone
    /// 7.4, publishing
    /// <see cref="CardActionResolutionEventKind.HeroUnresolvedCardResolved"/> when it does.</para>
    ///
    /// <para><b>What was wrong is that nothing read it.</b> That event kind appeared exactly twice in
    /// the whole build - its own enum member and the switch that produced it - so
    /// <see cref="MatchLogProjection"/> carried no row for it. The card changed to Resolved,
    /// Initiative stopped seeing it, its owner was never selected and never prompted, and the log
    /// said nothing. Every other line of p.13's list already had a row; this was the one hole, and
    /// it is the only path in the build that can resolve a played card without either an action or a
    /// prompt.</para>
    ///
    /// <para><b>Why a Green made it visible.</b> Brogan's five Greens print Initiative 5, 5, 5, 5 and
    /// 6 - the lowest in his 21-card deck, where nothing else is under 7. Turn order runs from the
    /// highest Initiative down, so a Green is always among the last cards in the round to come up,
    /// which is the longest possible window in which its owner can be defeated first. The card most
    /// likely to be eaten this way is exactly the card the playtest reported.</para>
    /// </summary>
    public sealed class HeroSkippedCardLogTests
    {
        private const string DefeatedHero = "hero-brogan";
        private const string Attacker = "hero-tigerclaw";

        /// <summary>
        /// The pinning case. Before this milestone the derived log was EMPTY for this event, which is
        /// the whole of what the playtest could not explain afterwards.
        /// </summary>
        [Test]
        public void Page13sUnresolvedCardLine_WritesARowNamingTheHeroItPassedOver()
        {
            var derived = MatchLogProjection.Derive(new[]
            {
                Entry(1, CardActionResolutionEventKind.HeroUnresolvedCardResolved)
            });

            var row = derived.SingleOrDefault(entry =>
                entry.Kind == MatchLogEntryKind.HeroCardResolvedWithoutAction);

            Assert.That(row, Is.Not.Null,
                "p.13's second line produced no match-log row. A played card that resolves with no " +
                "action and no prompt is the one thing in this engine that can pass a seat over, so " +
                "a log that stays silent about it leaves the player with no account of their turn - " +
                "which is precisely the 2026-09-06 playtest report.");
            Assert.That(row.SubjectHeroId, Is.EqualTo(new HeroId(DefeatedHero)),
                "The row names the hero whose card was eaten, not the hero who did the defeating. " +
                "On a card-action event 'hero' is the ACTOR and 'target' is the hero the action was " +
                "about, so a row that read 'hero' here would print the attacker's name beside the " +
                "one sentence in this log that has to name the right player.");
        }

        /// <summary>
        /// The guard that stops this coming back. p.13's list is published one typed fact per printed
        /// line, and a line the log has no word for is a line the table cannot read afterwards - so
        /// every fact kind the page can publish is required to reach a row here.
        /// </summary>
        /// <remarks>
        /// <see cref="HeroDefeatFactKind.DefeatRecorded"/> is carried by
        /// <see cref="CardActionResolutionEventKind.HeroDefeatRecorded"/> rather than by the shared
        /// fact route - milestone 7.4 says so at the factory - and
        /// <see cref="HeroDefeatFactKind.RemovalAttempted"/> is the attempt whose OUTCOME the next
        /// two lines state, which is why the log carries the outcome and not the attempt. Both are
        /// named here rather than skipped silently, so the exclusions stay arguments instead of
        /// omissions.
        /// </remarks>
        [Test]
        public void EveryLineOfPage13sList_ReachesTheMatchLogOrIsANamedException()
        {
            var carriedByAnotherRow = new Dictionary<HeroDefeatFactKind, string>
            {
                {
                    HeroDefeatFactKind.DefeatRecorded,
                    "carried by Action.HeroDefeatRecorded, which also holds the Attack totals"
                },
                {
                    HeroDefeatFactKind.RemovalAttempted,
                    "the attempt; its outcome is RemovalCompleted or RemovalPrevented, both carried"
                },

                // KNOWN GAP, found by this fixture on the day it was written and left named rather
                // than quietly excluded. p.13's fourth line - "collect coins equal to that hero's
                // level", plus the assist coins - runs and is published, and no row carries it. It
                // is not fixed here because the coin AMOUNTS are not on the event: the fact travels
                // with the defeat's identity only, so a row written today could say that a bounty
                // was collected but not how much, and coins are what buy p.15's levels. Closing it
                // properly means publishing the award on the event first. Recorded as a follow-up
                // rather than folded into the silent-skip fix, which is about a different line.
                {
                    HeroDefeatFactKind.RewardAwarded,
                    "KNOWN GAP - follow-up: the award amount is not published on the event, so the " +
                    "row cannot yet state the coins it is about"
                }
            };

            var missing = new List<string>();
            foreach (HeroDefeatFactKind fact in Enum.GetValues(typeof(HeroDefeatFactKind)))
            {
                if (fact == HeroDefeatFactKind.None || carriedByAnotherRow.ContainsKey(fact))
                {
                    continue;
                }

                var eventKind = EventKindFor(fact);
                var derived = MatchLogProjection.Derive(new[] { Entry(1, eventKind) });
                if (derived.Count == 0)
                {
                    missing.Add(fact + " (" + eventKind + ")");
                }
            }

            Assert.That(missing, Is.Empty,
                "These lines of rulebook p.13 run and are published, but write nothing a player can " +
                "read: " + string.Join(", ", missing) + ". A p.13 line with no row is invisible to " +
                "everyone at the table after it happens.");
        }

        /// <summary>
        /// The event kind each p.13 fact travels as, restating
        /// <see cref="CardActionResolutionEvent.HeroDefeatFact"/>'s own switch. It is restated rather
        /// than reached through the factory because the factory needs a completed Defeat to build
        /// one, and what this fixture is asking about is the LOG's vocabulary rather than the
        /// Defeat's.
        /// </summary>
        private static CardActionResolutionEventKind EventKindFor(HeroDefeatFactKind fact)
        {
            switch (fact)
            {
                case HeroDefeatFactKind.RemovalAttempted:
                    return CardActionResolutionEventKind.HeroRemovalAttempted;
                case HeroDefeatFactKind.RemovalCompleted:
                    return CardActionResolutionEventKind.HeroRemovalCompleted;
                case HeroDefeatFactKind.RemovalPrevented:
                    return CardActionResolutionEventKind.HeroRemovalPrevented;
                case HeroDefeatFactKind.UnresolvedCardResolved:
                    return CardActionResolutionEventKind.HeroUnresolvedCardResolved;
                case HeroDefeatFactKind.RewardAwarded:
                    return CardActionResolutionEventKind.HeroDefeatRewardAwarded;
                case HeroDefeatFactKind.LifeSpent:
                    return CardActionResolutionEventKind.TeamLifeSpent;
                case HeroDefeatFactKind.TeamEliminated:
                    return CardActionResolutionEventKind.TeamEliminated;
                case HeroDefeatFactKind.DefeatRecorded:
                    return CardActionResolutionEventKind.HeroDefeatRecorded;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(fact), fact,
                        "A new line of p.13 has no event kind here. Name the one it travels as, or " +
                        "the log cannot be asked whether it carries it.");
            }
        }

        /// <summary>
        /// One p.13 fact as the rule-event log records it: the ACTOR is the attacker and the TARGET
        /// is the hero the page is being run about, which is the pairing
        /// <c>RuleEventLogProjection.AppendCardAction</c> writes for every card-action event. The two
        /// are deliberately different heroes here so a row that reads the wrong one is caught.
        /// </summary>
        private static RuleEventLogEntry Entry(int sequence, CardActionResolutionEventKind kind) =>
            new RuleEventLogEntry(
                sequence,
                "Action." + kind,
                "hero=" + Attacker + ";target=" + DefeatedHero,
                "fact=" + kind);
    }
}
