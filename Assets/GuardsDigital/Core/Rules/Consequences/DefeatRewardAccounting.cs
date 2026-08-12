using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Consequences
{
    public readonly struct DefeatOutcomeId : IEquatable<DefeatOutcomeId>
    {
        public DefeatOutcomeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Defeat outcome ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(DefeatOutcomeId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is DefeatOutcomeId other && Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct DefeatTargetId : IEquatable<DefeatTargetId>
    {
        public DefeatTargetId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Defeat target ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(DefeatTargetId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is DefeatTargetId other && Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }

    public sealed class DefeatOutcome
    {
        public DefeatOutcome(
            DefeatOutcomeId id,
            DefeatTargetId targetId,
            TeamId defeatedTeamId,
            HeroId defeatingHeroId,
            int defeatCoins,
            int assistCoins)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("Defeat outcome requires a defined ID.", nameof(id));
            }

            if (!targetId.IsDefined)
            {
                throw new ArgumentException("Defeat outcome requires a defined target ID.", nameof(targetId));
            }

            if (!defeatedTeamId.IsDefined)
            {
                throw new ArgumentException("Defeat outcome requires a defined defeated team.", nameof(defeatedTeamId));
            }

            if (!defeatingHeroId.IsDefined)
            {
                throw new ArgumentException("Defeat outcome requires a defined defeating hero.", nameof(defeatingHeroId));
            }

            if (defeatCoins <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(defeatCoins), defeatCoins, "Defeat coins must be positive.");
            }

            if (assistCoins < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(assistCoins), assistCoins, "Assist coins must not be negative.");
            }

            Id = id;
            TargetId = targetId;
            DefeatedTeamId = defeatedTeamId;
            DefeatingHeroId = defeatingHeroId;
            DefeatCoins = defeatCoins;
            AssistCoins = assistCoins;
        }

        public DefeatOutcomeId Id { get; }

        public DefeatTargetId TargetId { get; }

        public TeamId DefeatedTeamId { get; }

        public HeroId DefeatingHeroId { get; }

        public int DefeatCoins { get; }

        public int AssistCoins { get; }
    }

    public enum DefeatRemovalResultKind
    {
        None = 0,
        Removed = 1,
        RemovalPrevented = 2
    }

    public readonly struct DefeatRemovalResult
    {
        public DefeatRemovalResult(
            DefeatOutcomeId defeatOutcomeId,
            DefeatTargetId targetId,
            DefeatRemovalResultKind kind)
        {
            if (!defeatOutcomeId.IsDefined)
            {
                throw new ArgumentException("Removal result requires a defined Defeat outcome ID.", nameof(defeatOutcomeId));
            }

            if (!targetId.IsDefined)
            {
                throw new ArgumentException("Removal result requires a defined target ID.", nameof(targetId));
            }

            if (kind != DefeatRemovalResultKind.Removed &&
                kind != DefeatRemovalResultKind.RemovalPrevented)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Removal result must be final.");
            }

            DefeatOutcomeId = defeatOutcomeId;
            TargetId = targetId;
            Kind = kind;
        }

        public DefeatOutcomeId DefeatOutcomeId { get; }

        public DefeatTargetId TargetId { get; }

        public DefeatRemovalResultKind Kind { get; }
    }

    public enum CoinAwardKind
    {
        None = 0,
        Defeat = 1,
        Assist = 2
    }

    public enum ResourceOperationFailureBehavior
    {
        None = 0,
        RejectWithoutMutation = 1
    }

    public readonly struct CoinResourceOperation
    {
        public CoinResourceOperation(
            DefeatOutcomeId sourceOutcomeId,
            DefeatTargetId sourceTargetId,
            HeroId recipientHeroId,
            TeamId recipientTeamId,
            CoinAwardKind awardKind,
            int amount,
            int balanceBefore,
            int balanceAfter)
        {
            if (!sourceOutcomeId.IsDefined || !sourceTargetId.IsDefined)
            {
                throw new ArgumentException("Coin operation requires a typed Defeat source.");
            }

            if (!recipientHeroId.IsDefined || !recipientTeamId.IsDefined)
            {
                throw new ArgumentException("Coin operation requires a typed recipient and team.");
            }

            if (awardKind != CoinAwardKind.Defeat && awardKind != CoinAwardKind.Assist)
            {
                throw new ArgumentOutOfRangeException(nameof(awardKind), awardKind, "Coin award kind must be Defeat or Assist.");
            }

            if (amount <= 0 || balanceBefore < MinimumBalance || balanceAfter != balanceBefore + amount)
            {
                throw new ArgumentException("Coin gain must be positive and preserve its declared balance transition.");
            }

            SourceOutcomeId = sourceOutcomeId;
            SourceTargetId = sourceTargetId;
            RecipientHeroId = recipientHeroId;
            RecipientTeamId = recipientTeamId;
            AwardKind = awardKind;
            Amount = amount;
            BalanceBefore = balanceBefore;
            BalanceAfter = balanceAfter;
        }

        public const int MinimumBalance = 0;

        public DefeatOutcomeId SourceOutcomeId { get; }

        public DefeatTargetId SourceTargetId { get; }

        public HeroId RecipientHeroId { get; }

        public TeamId RecipientTeamId { get; }

        public CoinAwardKind AwardKind { get; }

        public int Amount { get; }

        public int BalanceBefore { get; }

        public int BalanceAfter { get; }

        public ResourceOperationFailureBehavior FailureBehavior =>
            ResourceOperationFailureBehavior.RejectWithoutMutation;
    }

    public enum DefeatRewardRejectionReason
    {
        None = 0,
        DefeatingHeroNotInMatch = 1,
        DefeatedTeamNotInMatch = 2,
        FriendlyTarget = 3,
        OutcomeAlreadyAwarded = 4,
        CoinBalanceOverflow = 5
    }

    public sealed class DefeatRewardResult
    {
        private DefeatRewardResult(
            bool accepted,
            DefeatRewardRejectionReason rejectionReason,
            IEnumerable<CoinResourceOperation> operations)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Operations = Array.AsReadOnly(
                (operations ?? throw new ArgumentNullException(nameof(operations))).ToArray());
        }

        public bool Accepted { get; }

        public DefeatRewardRejectionReason RejectionReason { get; }

        public IReadOnlyList<CoinResourceOperation> Operations { get; }

        public static DefeatRewardResult Accept(IEnumerable<CoinResourceOperation> operations) =>
            new DefeatRewardResult(true, DefeatRewardRejectionReason.None, operations);

        public static DefeatRewardResult Reject(DefeatRewardRejectionReason rejectionReason)
        {
            if (rejectionReason == DefeatRewardRejectionReason.None)
            {
                throw new ArgumentException("A rejected reward requires a rejection reason.", nameof(rejectionReason));
            }

            return new DefeatRewardResult(false, rejectionReason, Array.Empty<CoinResourceOperation>());
        }
    }

    public sealed class DefeatRewardAccounting
    {
        private readonly MatchParticipants participants;
        private readonly Dictionary<HeroId, int> coinBalances;
        private readonly HashSet<DefeatOutcomeId> awardedOutcomeIds;

        public DefeatRewardAccounting(MatchParticipants participants)
        {
            this.participants = participants ?? throw new ArgumentNullException(nameof(participants));
            coinBalances = participants.Heroes.ToDictionary(hero => hero.Id, hero => 0);
            awardedOutcomeIds = new HashSet<DefeatOutcomeId>();
        }

        public int GetCoins(HeroId heroId)
        {
            if (!heroId.IsDefined || !coinBalances.TryGetValue(heroId, out var coins))
            {
                throw new KeyNotFoundException($"Unknown hero ID '{heroId}'.");
            }

            return coins;
        }

        public DefeatRewardResult Award(DefeatOutcome outcome)
        {
            if (outcome == null)
            {
                throw new ArgumentNullException(nameof(outcome));
            }

            if (!coinBalances.ContainsKey(outcome.DefeatingHeroId))
            {
                return DefeatRewardResult.Reject(DefeatRewardRejectionReason.DefeatingHeroNotInMatch);
            }

            if (!participants.Teams.Any(team => team.Id.Equals(outcome.DefeatedTeamId)))
            {
                return DefeatRewardResult.Reject(DefeatRewardRejectionReason.DefeatedTeamNotInMatch);
            }

            var awardingTeam = participants.GetTeamForHero(outcome.DefeatingHeroId);
            if (awardingTeam.Id.Equals(outcome.DefeatedTeamId))
            {
                return DefeatRewardResult.Reject(DefeatRewardRejectionReason.FriendlyTarget);
            }

            if (awardedOutcomeIds.Contains(outcome.Id))
            {
                return DefeatRewardResult.Reject(DefeatRewardRejectionReason.OutcomeAlreadyAwarded);
            }

            var plannedBalances = new Dictionary<HeroId, int>(coinBalances);
            var operations = new List<CoinResourceOperation>();
            if (!TryPlanGain(
                outcome,
                outcome.DefeatingHeroId,
                awardingTeam.Id,
                CoinAwardKind.Defeat,
                outcome.DefeatCoins,
                plannedBalances,
                operations))
            {
                return DefeatRewardResult.Reject(DefeatRewardRejectionReason.CoinBalanceOverflow);
            }

            if (outcome.AssistCoins > 0)
            {
                foreach (var friendlyHero in participants.GetHeroesForTeam(awardingTeam.Id))
                {
                    if (friendlyHero.Id.Equals(outcome.DefeatingHeroId))
                    {
                        continue;
                    }

                    if (!TryPlanGain(
                        outcome,
                        friendlyHero.Id,
                        awardingTeam.Id,
                        CoinAwardKind.Assist,
                        outcome.AssistCoins,
                        plannedBalances,
                        operations))
                    {
                        return DefeatRewardResult.Reject(DefeatRewardRejectionReason.CoinBalanceOverflow);
                    }
                }
            }

            foreach (var operation in operations)
            {
                coinBalances[operation.RecipientHeroId] = operation.BalanceAfter;
            }

            awardedOutcomeIds.Add(outcome.Id);
            return DefeatRewardResult.Accept(operations);
        }

        private static bool TryPlanGain(
            DefeatOutcome outcome,
            HeroId recipientHeroId,
            TeamId recipientTeamId,
            CoinAwardKind awardKind,
            int amount,
            IDictionary<HeroId, int> plannedBalances,
            ICollection<CoinResourceOperation> operations)
        {
            var balanceBefore = plannedBalances[recipientHeroId];
            var candidateBalance = (long)balanceBefore + amount;
            if (candidateBalance > int.MaxValue)
            {
                return false;
            }

            var balanceAfter = (int)candidateBalance;
            operations.Add(new CoinResourceOperation(
                outcome.Id,
                outcome.TargetId,
                recipientHeroId,
                recipientTeamId,
                awardKind,
                amount,
                balanceBefore,
                balanceAfter));
            plannedBalances[recipientHeroId] = balanceAfter;
            return true;
        }
    }
}
