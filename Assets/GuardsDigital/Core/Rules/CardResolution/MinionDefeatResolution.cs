using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public enum MinionDefeatFactKind
    {
        None = 0,
        DefeatRecorded = 1,
        RewardAwarded = 2,
        RemovalAttempted = 3,
        RemovalCompleted = 4,

        /// <summary>
        /// The removal was attempted and the piece stayed on the board. It is a fourth fact rather than
        /// the absence of one: milestones 5.5 and 5.6 made "the removal was attempted" and "the removal
        /// happened" separate facts precisely so this outcome could be stated, and a stream that simply
        /// stopped after the attempt would read as a resolution cut short instead of a removal stopped.
        /// </summary>
        RemovalPrevented = 5
    }

    /// <summary>
    /// p.13's Defeat as the staged outcome it is: the Defeat itself, its reward, an attempted removal,
    /// and what the removal stage decided.
    ///
    /// The two outcomes are named rather than distinguished by a constructor argument, in the shape
    /// <see cref="RemoveOutcome"/> already uses for p.19's standalone Remove. p.13's own worked example
    /// is why both have to exist: Bolster prevents the removal of a minion and the attacking hero still
    /// collects the coins for defeating it, so reward and removal are separate stages with separate
    /// results, and a prevented removal is a complete Defeat rather than a failed one.
    ///
    /// Which outcome applies is never decided here. <see cref="RemovalStageDecision"/> answers that for
    /// both removal paths, so a prevention source added later cannot be honoured by one and missed by
    /// the other - the disagreement milestone 6.6b closed.
    /// </summary>
    public sealed class MinionDefeatResolution
    {
        private MinionDefeatResolution(
            DefeatOutcome defeat,
            DefeatRewardResult reward,
            DefeatRemovalAttempt removalAttempt,
            DefeatRemovalResult removalResult,
            MinionOccupancy removedMinion,
            LanePushQualification lanePush,
            MinionDefeatFactKind removalFact)
        {
            Defeat = defeat ?? throw new ArgumentNullException(nameof(defeat));
            Reward = reward ?? throw new ArgumentNullException(nameof(reward));
            if (!reward.Accepted ||
                !removalAttempt.DefeatOutcomeId.Equals(defeat.Id) ||
                !removalAttempt.TargetId.Equals(defeat.TargetId) ||
                !removalResult.DefeatOutcomeId.Equals(defeat.Id) ||
                !removalResult.TargetId.Equals(defeat.TargetId))
            {
                throw new ArgumentException(
                    "Minion Defeat stages must reference one accepted outcome and one target.");
            }

            RemovalAttempt = removalAttempt;
            RemovalResult = removalResult;
            RemovedMinion = removedMinion;
            LanePush = lanePush;
            Facts = Array.AsReadOnly(new[]
            {
                MinionDefeatFactKind.DefeatRecorded,
                MinionDefeatFactKind.RewardAwarded,
                MinionDefeatFactKind.RemovalAttempted,
                removalFact
            });
        }

        public DefeatOutcome Defeat { get; }

        public DefeatRewardResult Reward { get; }

        public DefeatRemovalAttempt RemovalAttempt { get; }

        public DefeatRemovalResult RemovalResult { get; }

        /// <summary>
        /// The miniature that left the board, or <c>null</c> when the removal was prevented. A prevented
        /// removal requires no removed occupancy because nothing was removed: the minion is still where
        /// the board says it is, and naming it here would name a piece that never moved.
        /// </summary>
        public MinionOccupancy RemovedMinion { get; }

        /// <summary>
        /// p.8's recorded cause, or its named absence.
        ///
        /// A prevented removal carries it too, since milestone 6.9. p.8 names two independent triggers
        /// - "If you defeat, or remove, the last remaining enemy minion in the Battle Zone ... It must
        /// be either defeated, or removed" - and p.13's Bolster example makes a Defeat whose removal is
        /// prevented a complete Defeat that still collects its coins, so the first sentence is answered
        /// by the Defeat rather than by the miniature leaving. That is the asymmetry against p.19's
        /// standalone Remove, which never reaches the question when it is prevented because nothing was
        /// removed; it is also why milestone 6.6 gave <see cref="LanePushCauseKind"/> two members rather
        /// than one.
        ///
        /// It is a cause and never a request: <see cref="ImmediateLanePushQuery"/> turns it into one.
        /// </summary>
        public LanePushQualification LanePush { get; }

        public IReadOnlyList<MinionDefeatFactKind> Facts { get; }

        public static MinionDefeatResolution Removed(
            DefeatOutcome defeat,
            DefeatRewardResult reward,
            DefeatRemovalAttempt removalAttempt,
            DefeatRemovalResult removalResult,
            MinionOccupancy removedMinion,
            LanePushQualification lanePush)
        {
            if (defeat == null) throw new ArgumentNullException(nameof(defeat));
            if (removedMinion == null) throw new ArgumentNullException(nameof(removedMinion));
            if (removalResult.Kind != DefeatRemovalResultKind.Removed ||
                !string.Equals(
                    defeat.TargetId.Value, removedMinion.MinionId.Value, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    "A removed Defeat stage names the miniature that left the board.");
            }

            if (lanePush.Qualifies && lanePush.Cause != LanePushCauseKind.Defeat)
            {
                throw new ArgumentException(
                    "p.8 recognizes a Defeat and a removal separately; a Defeat records its own cause.",
                    nameof(lanePush));
            }

            return new MinionDefeatResolution(
                defeat, reward, removalAttempt, removalResult, removedMinion, lanePush,
                MinionDefeatFactKind.RemovalCompleted);
        }

        public static MinionDefeatResolution Prevented(
            DefeatOutcome defeat,
            DefeatRewardResult reward,
            DefeatRemovalAttempt removalAttempt,
            DefeatRemovalResult removalResult,
            LanePushQualification lanePush)
        {
            if (removalResult.Kind != DefeatRemovalResultKind.RemovalPrevented)
            {
                throw new ArgumentException(
                    "A prevented Defeat stage records the prevented result.", nameof(removalResult));
            }

            if (lanePush.Qualifies && lanePush.Cause != LanePushCauseKind.Defeat)
            {
                throw new ArgumentException(
                    "p.8 recognizes a Defeat and a removal separately; a Defeat records its own cause.",
                    nameof(lanePush));
            }

            return new MinionDefeatResolution(
                defeat, reward, removalAttempt, removalResult, null, lanePush,
                MinionDefeatFactKind.RemovalPrevented);
        }
    }
}
