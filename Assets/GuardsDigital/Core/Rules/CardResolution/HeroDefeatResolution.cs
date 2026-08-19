using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// The ordered facts rulebook p.13's "When a Hero is Defeated" list produces, one member per line
    /// of the page, in the page's own order.
    ///
    /// It is a separate vocabulary from <see cref="MinionDefeatFactKind"/> rather than a widening of
    /// it, because the two lists are different: a minion Defeat is priced, removed and counted for
    /// p.8, while a hero Defeat additionally resolves a card, cancels effects, returns markers and
    /// spends Life. Sharing one enum would have given every minion Defeat four members it can never
    /// report and every hero Defeat a "reward awarded" that means something else.
    /// </summary>
    public enum HeroDefeatFactKind
    {
        None = 0,

        /// <summary>The Defeat itself, which milestone 5.8 recorded and every later line follows.</summary>
        DefeatRecorded = 1,

        /// <summary>p.13: "Remove the defeated hero from the board." - attempted.</summary>
        RemovalAttempted = 2,

        /// <summary>The hero left the board.</summary>
        RemovalCompleted = 3,

        /// <summary>
        /// The removal was stopped and the hero is still standing. It is a named outcome for the reason
        /// milestone 6.6b gave the minion path one: two removal paths that disagree about whether a
        /// removal can be stopped are worse than a missing rule, and a Defeat whose removal is
        /// prevented is a complete Defeat rather than a failed one.
        /// </summary>
        RemovalPrevented = 4,

        /// <summary>
        /// p.13: "If the defeated hero had an unresolved card, it is resolved, without performing an
        /// action."
        /// </summary>
        UnresolvedCardResolved = 5,

        /// <summary>
        /// p.13: "When you defeat an enemy hero, collect coins equal to that hero's level. Your friendly
        /// heroes collect assist coins based on the defeated hero's lowest card Tier."
        /// </summary>
        RewardAwarded = 6,

        /// <summary>
        /// p.13: "Defeated hero's team spends the number of Life counters, equal to that hero's lowest
        /// card Tier."
        /// </summary>
        LifeSpent = 7,

        /// <summary>
        /// p.13: "If the last counter is spent, the game ends and the team with no Life counters left,
        /// loses the game."
        /// </summary>
        TeamEliminated = 8
    }

    /// <summary>
    /// Rulebook p.13's complete hero Defeat, as the staged outcome the page describes.
    ///
    /// Milestone 5.8 recorded the Defeat identity alone and deferred every consequence; 6.12 added the
    /// marker return; this is the milestone that resolves the rest. The staging follows p.14's own
    /// numbered <em>Attack/Defense Detailed Order</em>, whose step 6 is "Check if the attack was
    /// successful; if it was, remove the defending hero" and whose step 7 is the Attack card's "After
    /// the attack" text - so every line of p.13's list happens inside the Attack action and before the
    /// after-attack checkpoint, which is where the resolver raises it.
    ///
    /// The marker return is deliberately not one of these facts. It has carried its own event since
    /// milestone 6.12, one per attachment, and folding it into a single "markers returned" fact would
    /// have replaced a per-marker record with a less precise one.
    /// </summary>
    public sealed class HeroDefeatResolution
    {
        private HeroDefeatResolution(
            DefeatOutcome defeat,
            DefeatRewardResult reward,
            DefeatRemovalAttempt removalAttempt,
            DefeatRemovalResult removalResult,
            HeroOccupancy? removedHero,
            CardInstanceId resolvedUnresolvedCardId,
            TeamLifeSpend lifeSpend,
            bool hasLifeSpend,
            LanePushQualification lanePush)
        {
            Defeat = defeat ?? throw new ArgumentNullException(nameof(defeat));
            Reward = reward ?? throw new ArgumentNullException(nameof(reward));
            if (!defeat.HasRewardAmounts)
            {
                throw new ArgumentException(
                    "p.13 prices a hero Defeat by the defeated hero's Level and lowest card Tier.",
                    nameof(defeat));
            }

            if (!reward.Accepted ||
                !removalAttempt.DefeatOutcomeId.Equals(defeat.Id) ||
                !removalAttempt.TargetId.Equals(defeat.TargetId) ||
                !removalResult.DefeatOutcomeId.Equals(defeat.Id) ||
                !removalResult.TargetId.Equals(defeat.TargetId))
            {
                throw new ArgumentException(
                    "Hero Defeat stages must reference one accepted outcome and one target.");
            }

            if (hasLifeSpend && !lifeSpend.TeamId.Equals(defeat.DefeatedTeamId))
            {
                throw new ArgumentException(
                    "p.13 spends the Life counters of the defeated hero's own team.", nameof(lifeSpend));
            }

            RemovalAttempt = removalAttempt;
            RemovalResult = removalResult;
            RemovedHero = removedHero;
            ResolvedUnresolvedCardId = resolvedUnresolvedCardId;
            LifeSpend = lifeSpend;
            HasLifeSpend = hasLifeSpend;
            LanePush = lanePush;

            var facts = new List<HeroDefeatFactKind>
            {
                HeroDefeatFactKind.DefeatRecorded,
                HeroDefeatFactKind.RemovalAttempted,
                removalResult.Kind == DefeatRemovalResultKind.Removed
                    ? HeroDefeatFactKind.RemovalCompleted
                    : HeroDefeatFactKind.RemovalPrevented
            };
            if (resolvedUnresolvedCardId.IsDefined)
            {
                facts.Add(HeroDefeatFactKind.UnresolvedCardResolved);
            }

            facts.Add(HeroDefeatFactKind.RewardAwarded);
            if (hasLifeSpend)
            {
                facts.Add(HeroDefeatFactKind.LifeSpent);
                if (lifeSpend.SpentLastCounter)
                {
                    facts.Add(HeroDefeatFactKind.TeamEliminated);
                }
            }

            Facts = Array.AsReadOnly(facts.ToArray());
        }

        public DefeatOutcome Defeat { get; }

        public DefeatRewardResult Reward { get; }

        public DefeatRemovalAttempt RemovalAttempt { get; }

        public DefeatRemovalResult RemovalResult { get; }

        /// <summary>
        /// The hero occupancy that left the board, or <c>null</c> when the removal was prevented - the
        /// same distinction <c>MinionDefeatResolution.RemovedMinion</c> draws, and for the same reason:
        /// naming an occupancy for a piece that never moved would name a fact that did not happen.
        /// </summary>
        public HeroOccupancy? RemovedHero { get; }

        /// <summary>
        /// The card p.13's second line resolved without an action, or an undefined id when the defeated
        /// hero had none - which is the ordinary case for a hero that has already acted this turn, and
        /// for every hero on a turn it passed.
        /// </summary>
        public CardInstanceId ResolvedUnresolvedCardId { get; }

        /// <summary>
        /// The Life counters the defeated hero's team gave up. Absent only when the defeated hero has
        /// no non-Basic card at all: p.15's Basic pair "have no Tier, and are not considered when
        /// checking your lowest card Tier", so such a hero has no Tier for p.13 to read and the page
        /// states no number to spend. No configured hero is in that position - every hand holds three
        /// non-Basic cards - but a rule that reads a card is not permitted to invent one.
        /// </summary>
        public TeamLifeSpend LifeSpend { get; }

        public bool HasLifeSpend { get; }

        /// <summary>
        /// p.8's recorded cause, or its named absence, asked through the same shared removal stage the
        /// minion path asks. p.8 counts minions, so a hero leaving the board qualifies nothing on its
        /// own; the question is still routed through the shared decision rather than answered here,
        /// because a hero that <c>Counts As</c> a minion is exactly the case where guessing would make
        /// the two removal paths disagree again.
        /// </summary>
        public LanePushQualification LanePush { get; }

        public IReadOnlyList<HeroDefeatFactKind> Facts { get; }

        public static HeroDefeatResolution Removed(
            DefeatOutcome defeat,
            DefeatRewardResult reward,
            DefeatRemovalAttempt removalAttempt,
            DefeatRemovalResult removalResult,
            HeroOccupancy removedHero,
            CardInstanceId resolvedUnresolvedCardId,
            TeamLifeSpend lifeSpend,
            bool hasLifeSpend,
            LanePushQualification lanePush)
        {
            if (defeat == null) throw new ArgumentNullException(nameof(defeat));
            if (removalResult.Kind != DefeatRemovalResultKind.Removed ||
                !string.Equals(defeat.TargetId.Value, removedHero.HeroId.Value, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A removed hero Defeat stage names the miniature that left the board.");
            }

            RequireDefeatCause(lanePush);
            return new HeroDefeatResolution(
                defeat, reward, removalAttempt, removalResult, removedHero,
                resolvedUnresolvedCardId, lifeSpend, hasLifeSpend, lanePush);
        }

        public static HeroDefeatResolution Prevented(
            DefeatOutcome defeat,
            DefeatRewardResult reward,
            DefeatRemovalAttempt removalAttempt,
            DefeatRemovalResult removalResult,
            CardInstanceId resolvedUnresolvedCardId,
            TeamLifeSpend lifeSpend,
            bool hasLifeSpend,
            LanePushQualification lanePush)
        {
            if (removalResult.Kind != DefeatRemovalResultKind.RemovalPrevented)
            {
                throw new ArgumentException(
                    "A prevented hero Defeat stage records the prevented result.", nameof(removalResult));
            }

            RequireDefeatCause(lanePush);
            return new HeroDefeatResolution(
                defeat, reward, removalAttempt, removalResult, null,
                resolvedUnresolvedCardId, lifeSpend, hasLifeSpend, lanePush);
        }

        private static void RequireDefeatCause(LanePushQualification lanePush)
        {
            if (lanePush.Qualifies && lanePush.Cause != LanePushCauseKind.Defeat)
            {
                throw new ArgumentException(
                    "p.8 recognizes a Defeat and a removal separately; a Defeat records its own cause.",
                    nameof(lanePush));
            }
        }
    }
}
