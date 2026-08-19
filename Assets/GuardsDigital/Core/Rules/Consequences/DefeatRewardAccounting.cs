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
            HeroId defeatingHeroId)
            : this(id, targetId, defeatedTeamId, defeatingHeroId, 0, 0, false)
        {
        }

        public DefeatOutcome(
            DefeatOutcomeId id,
            DefeatTargetId targetId,
            TeamId defeatedTeamId,
            HeroId defeatingHeroId,
            int defeatCoins,
            int assistCoins)
            : this(id, targetId, defeatedTeamId, defeatingHeroId, defeatCoins, assistCoins, true)
        {
        }

        private DefeatOutcome(
            DefeatOutcomeId id,
            DefeatTargetId targetId,
            TeamId defeatedTeamId,
            HeroId defeatingHeroId,
            int defeatCoins,
            int assistCoins,
            bool hasRewardAmounts)
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

            if (hasRewardAmounts && defeatCoins <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(defeatCoins), defeatCoins, "Defeat coins must be positive.");
            }

            if (hasRewardAmounts && assistCoins < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(assistCoins), assistCoins, "Assist coins must not be negative.");
            }

            Id = id;
            TargetId = targetId;
            DefeatedTeamId = defeatedTeamId;
            DefeatingHeroId = defeatingHeroId;
            DefeatCoins = defeatCoins;
            AssistCoins = assistCoins;
            HasRewardAmounts = hasRewardAmounts;
        }

        public DefeatOutcomeId Id { get; }

        public DefeatTargetId TargetId { get; }

        public TeamId DefeatedTeamId { get; }

        public HeroId DefeatingHeroId { get; }

        public int DefeatCoins { get; }

        public int AssistCoins { get; }

        public bool HasRewardAmounts { get; }

        public DefeatOutcome WithRewardAmounts(int defeatCoins, int assistCoins) =>
            new DefeatOutcome(Id, TargetId, DefeatedTeamId, DefeatingHeroId, defeatCoins, assistCoins);
    }

    public enum DefeatRemovalResultKind
    {
        None = 0,
        Removed = 1,
        RemovalPrevented = 2
    }

    public readonly struct DefeatRemovalAttempt
    {
        public DefeatRemovalAttempt(DefeatOutcomeId defeatOutcomeId, DefeatTargetId targetId)
        {
            if (!defeatOutcomeId.IsDefined || !targetId.IsDefined)
            {
                throw new ArgumentException("Removal attempt requires a typed Defeat outcome and target.");
            }

            DefeatOutcomeId = defeatOutcomeId;
            TargetId = targetId;
        }

        public DefeatOutcomeId DefeatOutcomeId { get; }
        public DefeatTargetId TargetId { get; }
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

    /// <summary>
    /// What a hero spent coins on. Milestone 7.5 adds the first and so far only sink: rulebook p.15's
    /// mandatory level up, whose price is <c>HeroLevelSchedule.LevelUpCost</c>.
    ///
    /// It is named rather than left implicit for the reason <see cref="CoinAwardKind"/> is: the ledger
    /// records where each coin came from, and a ledger that recorded where they went as an unlabelled
    /// subtraction would be able to say less about a spend than about a gain.
    /// </summary>
    public enum CoinSpendKind
    {
        None = 0,
        LevelUp = 1
    }

    /// <summary>
    /// One deduction from a hero's coin balance, in the shape <see cref="CoinResourceOperation"/> gives
    /// a gain: the hero, what it bought, the amount, and the exact balance transition.
    ///
    /// A spend has no Defeat source, which is the whole reason this is a second type rather than a
    /// negative amount on the first: <see cref="CoinResourceOperation"/> requires a typed Defeat outcome
    /// and target because every gain in this build has one, and a level purchase would have had to
    /// invent both to be expressed there.
    /// </summary>
    public readonly struct CoinSpendOperation
    {
        public CoinSpendOperation(
            HeroId spendingHeroId,
            TeamId spendingTeamId,
            CoinSpendKind spendKind,
            int amount,
            int balanceBefore,
            int balanceAfter)
        {
            if (!spendingHeroId.IsDefined || !spendingTeamId.IsDefined)
            {
                throw new ArgumentException("Coin spend requires a typed spender and team.");
            }

            if (spendKind != CoinSpendKind.LevelUp)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(spendKind), spendKind, "Coin spend kind must name what the coins bought.");
            }

            if (amount <= 0 ||
                balanceAfter < CoinResourceOperation.MinimumBalance ||
                balanceAfter != balanceBefore - amount)
            {
                throw new ArgumentException(
                    "Coin spend must be positive, affordable, and preserve its declared balance transition.");
            }

            SpendingHeroId = spendingHeroId;
            SpendingTeamId = spendingTeamId;
            SpendKind = spendKind;
            Amount = amount;
            BalanceBefore = balanceBefore;
            BalanceAfter = balanceAfter;
        }

        public HeroId SpendingHeroId { get; }

        public TeamId SpendingTeamId { get; }

        public CoinSpendKind SpendKind { get; }

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
        CoinBalanceOverflow = 5,
        RewardAmountsUnavailable = 6
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

    /// <summary>
    /// The match's per-hero coin ledger, plus the record of which Defeat outcomes have already paid out.
    ///
    /// Milestone 7.5 makes the name narrower than the type. Every coin in this build still arrives
    /// through <see cref="Award"/> - rulebook p.13's Defeat and assist coins are the only source
    /// implemented - but p.15's mandatory level up is now a sink, so the balances are no longer read
    /// only by the rule that filled them. <see cref="TrySpend"/> lives here rather than beside the level
    /// purchase because a second writer to a balance is a second answer to what a hero can afford; the
    /// awarded-outcome set is what stays Defeat-specific, and it is the reason a rename was left to a
    /// naming pass of its own rather than folded into a progression milestone.
    /// </summary>
    public sealed class DefeatRewardAccounting
    {
        private readonly MatchParticipants participants;
        private readonly Dictionary<HeroId, int> coinBalances;
        private readonly HashSet<DefeatOutcomeId> awardedOutcomeIds;

        public DefeatRewardAccounting(MatchParticipants participants)
            : this(participants, null, null)
        {
        }

        private DefeatRewardAccounting(
            MatchParticipants participants,
            IEnumerable<KeyValuePair<HeroId, int>> restoredBalances,
            IEnumerable<DefeatOutcomeId> restoredOutcomeIds)
        {
            this.participants = participants ?? throw new ArgumentNullException(nameof(participants));
            coinBalances = participants.Heroes.ToDictionary(hero => hero.Id, hero => 0);
            awardedOutcomeIds = new HashSet<DefeatOutcomeId>();

            if (restoredBalances != null)
            {
                var balances = restoredBalances.ToArray();
                if (balances.Length != coinBalances.Count || balances.Any(value => value.Value < 0) ||
                    balances.Select(value => value.Key).Distinct().Count() != balances.Length ||
                    balances.Any(value => !coinBalances.ContainsKey(value.Key)))
                {
                    throw new ArgumentException("Restored coin balances must cover every participant hero exactly once.", nameof(restoredBalances));
                }

                foreach (var balance in balances) coinBalances[balance.Key] = balance.Value;
            }

            if (restoredOutcomeIds != null)
            {
                foreach (var outcomeId in restoredOutcomeIds)
                {
                    if (!outcomeId.IsDefined || !awardedOutcomeIds.Add(outcomeId))
                        throw new ArgumentException("Restored awarded outcomes require unique defined IDs.", nameof(restoredOutcomeIds));
                }
            }
        }

        public IReadOnlyList<KeyValuePair<HeroId, int>> CoinBalances => Array.AsReadOnly(
            coinBalances.OrderBy(value => value.Key.Value, StringComparer.Ordinal).ToArray());

        public IReadOnlyList<DefeatOutcomeId> AwardedOutcomeIds => Array.AsReadOnly(
            awardedOutcomeIds.OrderBy(value => value.Value, StringComparer.Ordinal).ToArray());

        public DefeatRewardRestoreState CaptureRestoreState() =>
            new DefeatRewardRestoreState(CoinBalances, AwardedOutcomeIds);

        public static DefeatRewardAccounting Restore(
            MatchParticipants participants,
            DefeatRewardRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new DefeatRewardAccounting(
                participants,
                state.CoinBalances,
                state.AwardedOutcomeIds);
        }

        public int GetCoins(HeroId heroId)
        {
            if (!heroId.IsDefined || !coinBalances.TryGetValue(heroId, out var coins))
            {
                throw new KeyNotFoundException($"Unknown hero ID '{heroId}'.");
            }

            return coins;
        }

        /// <summary>
        /// Spends coins a hero has, refusing without mutation when they do not have them.
        ///
        /// It answers one question and states what it refuses to decide: whether this hero has
        /// <paramref name="amount"/> coins right now. Whether the purchase is owed, what it costs, and
        /// what it buys belong to the rule that asked - p.15's level up is the only one today - for the
        /// reason milestone 7.1 gave the card lifecycle's transitions: a ledger that re-derived
        /// eligibility would give the purchase two answers that nothing keeps in agreement.
        ///
        /// The refusal is the affordable/unaffordable one alone. p.15 makes the purchase mandatory when
        /// the coins are there, so "cannot afford it" is the ordinary end of a hero's leveling rather
        /// than an error, and it is reported as <c>false</c> with an untouched balance.
        /// </summary>
        public bool TrySpend(
            HeroId heroId,
            int amount,
            CoinSpendKind spendKind,
            out CoinSpendOperation operation)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount), amount, "A coin spend is a positive number of coins.");
            }

            var balanceBefore = GetCoins(heroId);
            if (balanceBefore < amount)
            {
                operation = default;
                return false;
            }

            operation = new CoinSpendOperation(
                heroId,
                participants.GetTeamForHero(heroId).Id,
                spendKind,
                amount,
                balanceBefore,
                balanceBefore - amount);
            coinBalances[heroId] = operation.BalanceAfter;
            return true;
        }

        public DefeatRewardResult Award(DefeatOutcome outcome)
        {
            if (outcome == null)
            {
                throw new ArgumentNullException(nameof(outcome));
            }

            if (!outcome.HasRewardAmounts)
            {
                return DefeatRewardResult.Reject(DefeatRewardRejectionReason.RewardAmountsUnavailable);
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
