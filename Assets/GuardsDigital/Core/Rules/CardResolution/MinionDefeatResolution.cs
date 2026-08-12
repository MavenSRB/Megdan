using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public enum MinionDefeatFactKind
    {
        None = 0,
        DefeatRecorded = 1,
        RewardAwarded = 2,
        RemovalAttempted = 3,
        RemovalCompleted = 4
    }

    public sealed class MinionDefeatResolution
    {
        public MinionDefeatResolution(
            DefeatOutcome defeat,
            DefeatRewardResult reward,
            DefeatRemovalAttempt removalAttempt,
            DefeatRemovalResult removalResult,
            QuickGameMinionSetupState removedMinion)
        {
            Defeat = defeat ?? throw new ArgumentNullException(nameof(defeat));
            Reward = reward ?? throw new ArgumentNullException(nameof(reward));
            RemovedMinion = removedMinion ?? throw new ArgumentNullException(nameof(removedMinion));
            if (!reward.Accepted || removalResult.Kind != DefeatRemovalResultKind.Removed ||
                !removalAttempt.DefeatOutcomeId.Equals(defeat.Id) ||
                !removalAttempt.TargetId.Equals(defeat.TargetId) ||
                !removalResult.DefeatOutcomeId.Equals(defeat.Id) ||
                !removalResult.TargetId.Equals(defeat.TargetId) ||
                !string.Equals(defeat.TargetId.Value, removedMinion.MinionId.Value, StringComparison.Ordinal))
            {
                throw new ArgumentException("Minion Defeat stages must reference one accepted outcome and removed target.");
            }

            RemovalAttempt = removalAttempt;
            RemovalResult = removalResult;
            Facts = Array.AsReadOnly(new[]
            {
                MinionDefeatFactKind.DefeatRecorded,
                MinionDefeatFactKind.RewardAwarded,
                MinionDefeatFactKind.RemovalAttempted,
                MinionDefeatFactKind.RemovalCompleted
            });
        }

        public DefeatOutcome Defeat { get; }
        public DefeatRewardResult Reward { get; }
        public DefeatRemovalAttempt RemovalAttempt { get; }
        public DefeatRemovalResult RemovalResult { get; }
        public QuickGameMinionSetupState RemovedMinion { get; }
        public IReadOnlyList<MinionDefeatFactKind> Facts { get; }
    }
}
