using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// Milestone 8.38d. The one place that knows how
    /// <see cref="GrantedTargetExclusion.SameEnemyHeroOncePerTurn"/> is written down and read back.
    ///
    /// <para><b>Where the accumulation lives, and why there.</b> The heroes a grant has already
    /// reached this turn ride as keys in the turn-scoped string set
    /// <see cref="TurnResolutionRestoreState.ConsumedTurnScopedKeys"/>, beside milestone 8.19a's
    /// once-per-turn trigger ids. That set is already serialized as a length-prefixed run of strings,
    /// so a second kind of key in it is format-identical and moves no snapshot schema; and its lifetime
    /// is already exactly the window this clause names, because the turn that clears one clears the
    /// other.</para>
    ///
    /// <para><b>What is deliberately NOT the mechanism.</b>
    /// <c>PendingAttackResolution.ExcludedTargets</c> accumulates inside one repeat chain and dies with
    /// the resolution that owns it, so it cannot carry a fact that spans two separate granted actions
    /// in one turn. It is still what one started instance READS - seeding it is how a narrowing reaches
    /// the target offer and the bind check together, which is the discipline milestone 8.10 set - but
    /// the store is here.</para>
    ///
    /// <para><b>The keys are namespaced so the two readers cannot collide.</b> A before-action trigger
    /// id is an ability's own rule id, and both prefixes below carry a character such an id never does;
    /// <see cref="RefuseAmbiguousSourceId"/> is what keeps that true rather than hoped for.</para>
    /// </summary>
    public static class GrantedTargetExclusionQuery
    {
        /// <summary>Prefix of an accumulated exclusion: one grant, one enemy hero, this turn.</summary>
        public const string ExcludedTargetPrefix = "granted-target:";

        /// <summary>
        /// Prefix of the key naming which grant the action currently in flight came from.
        ///
        /// <para>It exists because the exclusion is RECORDED when a target is answered, which is a
        /// later command than the one that started the action - and a restore in between must still
        /// know whose grant is running. Holding it in the same turn-scoped set is what lets that
        /// survive without a new field, which is to say without a schema revision.</para>
        /// </summary>
        public const string RunningGrantPrefix = "granted-run:";

        /// <summary>
        /// Refuses an ability id that could be mistaken for one of this type's keys. Called wherever a
        /// key is minted, so a later ability cannot silently start excluding targets by being named
        /// after one.
        /// </summary>
        public static void RefuseAmbiguousSourceId(string grantSourceId)
        {
            if (string.IsNullOrWhiteSpace(grantSourceId))
            {
                throw new ArgumentException(
                    "A granted-target exclusion is keyed by the ability that grants the action.",
                    nameof(grantSourceId));
            }

            if (grantSourceId.IndexOf(':') >= 0)
            {
                throw new ArgumentException(
                    "An Ultimate's rule id shares the turn-scoped key set with milestone 8.38d's " +
                    "granted-target exclusions, which namespace themselves with ':'.",
                    nameof(grantSourceId));
            }
        }

        /// <summary>The key recording that this grant has reached <paramref name="heroId"/>.</summary>
        public static string ExcludedTargetKey(string grantSourceId, HeroId heroId)
        {
            RefuseAmbiguousSourceId(grantSourceId);
            if (!heroId.IsDefined)
            {
                throw new ArgumentException(
                    "Only a named enemy hero is excluded; the clause says nothing about minions.",
                    nameof(heroId));
            }

            return ExcludedTargetPrefix + grantSourceId + ":" + heroId.Value;
        }

        /// <summary>The key saying an action granted by this ability is in flight.</summary>
        public static string RunningGrantKey(string grantSourceId)
        {
            RefuseAmbiguousSourceId(grantSourceId);
            return RunningGrantPrefix + grantSourceId;
        }

        /// <summary>
        /// The grant whose action is in flight, or <c>null</c> when none is. At most one is ever
        /// present: an action does not start inside another, and the key is cleared when it ends.
        /// </summary>
        public static string RunningGrant(IEnumerable<string> turnScopedKeys) =>
            (turnScopedKeys ?? Enumerable.Empty<string>())
                .Where(key => key != null &&
                    key.StartsWith(RunningGrantPrefix, StringComparison.Ordinal))
                .Select(key => key.Substring(RunningGrantPrefix.Length))
                .FirstOrDefault();

        /// <summary>
        /// Every enemy hero this grant may no longer reach this turn, shaped as the targets a started
        /// instance excludes. No minion target is ever produced: the printed clause names the enemy
        /// hero, so a granted Attack may take the same minion as often as the board allows.
        /// </summary>
        public static IReadOnlyList<CardResolution.AttackTarget> ExcludedTargets(
            string grantSourceId, IEnumerable<string> turnScopedKeys)
        {
            if (string.IsNullOrWhiteSpace(grantSourceId))
            {
                return Array.Empty<CardResolution.AttackTarget>();
            }

            var prefix = ExcludedTargetPrefix + grantSourceId + ":";
            return (turnScopedKeys ?? Enumerable.Empty<string>())
                .Where(key => key != null && key.StartsWith(prefix, StringComparison.Ordinal))
                .Select(key => key.Substring(prefix.Length))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .Select(value => CardResolution.AttackTarget.Hero(new HeroId(value)))
                .ToArray();
        }

        /// <summary>
        /// The rule the hero's unlocked Ultimate is keyed under - the name a grant of theirs runs
        /// under - or <c>null</c> when they have unlocked none.
        ///
        /// <para>It reads the same unlocked set every other permanent ability is read from, so an
        /// Ultimate still in the deck names nothing, exactly as rulebook p.16 requires. It answers
        /// regardless of whether that ability narrows anything, because the name is what says WHICH
        /// grant is running and every ability-granted instance needs that said; whether the narrowing
        /// applies is <see cref="Excludes"/>'s question, asked of the same rule.</para>
        /// </summary>
        public static string GrantSourceIdFor(
            UnlockedUltimateSet unlockedUltimates, HeroId controllerId)
        {
            var rule = RuleFor(unlockedUltimates, controllerId);
            if (rule == null)
            {
                return null;
            }

            RefuseAmbiguousSourceId(rule.RuleId);
            return rule.RuleId;
        }

        /// <summary>
        /// Whether this hero's unlocked Ultimate narrows what the actions it grants may target.
        /// <c>false</c> for every hero in this build but Swift, and for Swift until p.16 unlocks
        /// Bullet Time.
        /// </summary>
        public static bool Excludes(UnlockedUltimateSet unlockedUltimates, HeroId controllerId) =>
            RuleFor(unlockedUltimates, controllerId)?.GrantedTargetExclusion ==
                GrantedTargetExclusion.SameEnemyHeroOncePerTurn;

        private static UltimateRuleDefinition RuleFor(
            UnlockedUltimateSet unlockedUltimates, HeroId controllerId)
        {
            if (!controllerId.IsDefined || unlockedUltimates == null)
            {
                return null;
            }

            foreach (var unlocked in unlockedUltimates.Ultimates)
            {
                if (unlocked != null && unlocked.OwnerId.Equals(controllerId) && unlocked.Rule != null)
                {
                    return unlocked.Rule;
                }
            }

            return null;
        }
    }
}
