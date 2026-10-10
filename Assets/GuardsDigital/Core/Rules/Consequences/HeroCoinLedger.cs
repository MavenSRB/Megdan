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
    /// Where a coin came from when no Defeat put it there. Milestone 7.6 adds the first: rulebook p.8's
    /// Pity Coin, paid at the end of a round to every hero that did not level up in it.
    ///
    /// It is a separate vocabulary from <see cref="CoinAwardKind"/> rather than a third member of it,
    /// because that enum's two members answer "which half of p.13's Defeat reward is this" and a Pity
    /// Coin is not an answer to that question. Widening it would have made every consumer that switches
    /// on a Defeat award handle a value with no Defeat behind it.
    /// </summary>
    public enum CoinGrantKind
    {
        None = 0,
        PityCoin = 1,

        /// <summary>A card effect gains coins from the supply without a Defeat source.</summary>
        CardEffect = 2
    }

    /// <summary>
    /// One addition to a hero's coin balance with no Defeat source, in the shape
    /// <see cref="CoinSpendOperation"/> gives a deduction: the hero, what put the coin there, the
    /// amount, and the exact balance transition.
    /// </summary>
    public readonly struct CoinGrantOperation
    {
        public CoinGrantOperation(
            HeroId recipientHeroId,
            TeamId recipientTeamId,
            CoinGrantKind grantKind,
            int amount,
            int balanceBefore,
            int balanceAfter)
        {
            if (!recipientHeroId.IsDefined || !recipientTeamId.IsDefined)
            {
                throw new ArgumentException("Coin grant requires a typed recipient and team.");
            }

            if (grantKind != CoinGrantKind.PityCoin &&
                grantKind != CoinGrantKind.CardEffect)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(grantKind), grantKind, "Coin grant kind must name what put the coin there.");
            }

            if (amount <= 0 ||
                balanceBefore < CoinResourceOperation.MinimumBalance ||
                balanceAfter != balanceBefore + amount)
            {
                throw new ArgumentException(
                    "Coin grant must be positive and preserve its declared balance transition.");
            }

            RecipientHeroId = recipientHeroId;
            RecipientTeamId = recipientTeamId;
            GrantKind = grantKind;
            Amount = amount;
            BalanceBefore = balanceBefore;
            BalanceAfter = balanceAfter;
            AddsNothing = false;
        }

        private CoinGrantOperation(
            HeroId recipientHeroId,
            TeamId recipientTeamId,
            CoinGrantKind grantKind,
            int amount,
            int balance)
            : this(recipientHeroId, recipientTeamId, grantKind, amount, balance, balance + amount)
        {
            BalanceAfter = balance;
            AddsNothing = true;
        }

        /// <summary>
        /// W-022, PVE-D22. A grant a mode accepts as done while adding nothing: it names the recipient
        /// and the printed amount, and the balance stays where it was.
        /// </summary>
        internal static CoinGrantOperation AddingNothing(
            HeroId recipientHeroId, TeamId recipientTeamId, CoinGrantKind grantKind, int amount, int balance) =>
            new CoinGrantOperation(recipientHeroId, recipientTeamId, grantKind, amount, balance);

        /// <summary>True only for a grant a mode accepted while adding nothing (PVE-D22).</summary>
        public bool AddsNothing { get; }

        public HeroId RecipientHeroId { get; }

        public TeamId RecipientTeamId { get; }

        public CoinGrantKind GrantKind { get; }

        public int Amount { get; }

        public int BalanceBefore { get; }

        public int BalanceAfter { get; }

        public ResourceOperationFailureBehavior FailureBehavior =>
            ResourceOperationFailureBehavior.RejectWithoutMutation;
    }

    /// <summary>
    /// One atomic transfer between two existing hero balances. It neither creates nor destroys coins:
    /// the amount removed from the source is exactly the amount added to the recipient.
    /// </summary>
    public readonly struct CoinTransferOperation
    {
        public CoinTransferOperation(
            HeroId sourceHeroId,
            HeroId recipientHeroId,
            int amount,
            int sourceBalanceBefore,
            int sourceBalanceAfter,
            int recipientBalanceBefore,
            int recipientBalanceAfter)
        {
            if (!sourceHeroId.IsDefined || !recipientHeroId.IsDefined ||
                sourceHeroId.Equals(recipientHeroId))
            {
                throw new ArgumentException(
                    "A coin transfer requires two distinct, defined heroes.");
            }

            if (amount <= 0 || sourceBalanceAfter < CoinResourceOperation.MinimumBalance ||
                sourceBalanceAfter != sourceBalanceBefore - amount ||
                recipientBalanceAfter != recipientBalanceBefore + amount)
            {
                throw new ArgumentException(
                    "A coin transfer must be positive, affordable, and preserve both balance transitions.");
            }

            SourceHeroId = sourceHeroId;
            RecipientHeroId = recipientHeroId;
            Amount = amount;
            SourceBalanceBefore = sourceBalanceBefore;
            SourceBalanceAfter = sourceBalanceAfter;
            RecipientBalanceBefore = recipientBalanceBefore;
            RecipientBalanceAfter = recipientBalanceAfter;
        }

        public HeroId SourceHeroId { get; }
        public HeroId RecipientHeroId { get; }
        public int Amount { get; }
        public int SourceBalanceBefore { get; }
        public int SourceBalanceAfter { get; }
        public int RecipientBalanceBefore { get; }
        public int RecipientBalanceAfter { get; }

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
    /// Three operations move a balance and only the first is about a Defeat: <see cref="Award"/> is
    /// rulebook p.13's Defeat and assist coins, <see cref="TrySpend"/> is p.15's mandatory level up, and
    /// <see cref="TryGrant"/> is p.8's Pity Coin. All three live here because a second writer to a
    /// balance is a second answer to what a hero can afford, which is the one thing that must not
    /// happen. What stays genuinely Defeat-specific is the awarded-outcome set, which exists so a
    /// Defeat cannot pay out twice.
    ///
    /// Milestone 7.8d renamed this from <c>DefeatRewardAccounting</c> and changed nothing else. That
    /// name was accurate while p.13's Defeat was the only rule that touched a coin; milestone 7.5's
    /// spend and milestone 7.6's grant made it name one of three operations instead of the balances
    /// they all move, and both of those milestones deferred the correction rather than bury a
    /// mechanical rename inside a progression milestone. Milestone 6.13's naming audit is the
    /// precedent - a qualifier that has stopped describing its type is corrected in a pass of its own.
    /// </summary>
    public sealed class HeroCoinLedger
    {
        private readonly MatchParticipants participants;
        private readonly Dictionary<HeroId, int> coinBalances;
        private readonly HashSet<DefeatOutcomeId> awardedOutcomeIds;

        /// <summary>
        /// Milestone 8.53. The match scoreboard's "Coins earned" column, or <c>null</c> outside a
        /// composed match. See <see cref="BindScoreboard"/>.
        /// </summary>
        private GuardsDigital.Core.Rules.Victory.MatchScoreboard scoreboard;

        public HeroCoinLedger(MatchParticipants participants)
            : this(participants, null, null)
        {
        }

        private HeroCoinLedger(
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

        public HeroCoinLedgerRestoreState CaptureRestoreState() =>
            new HeroCoinLedgerRestoreState(CoinBalances, AwardedOutcomeIds);

        /// <summary>
        /// Milestone 8.53. Hands this ledger the match scoreboard whose "Coins earned" column counts
        /// what it credits, on exactly the terms the round authority hands the dispatcher its items
        /// and its Active-effect lifecycle: the round owns the state and this collaborator is built
        /// before it.
        /// </summary>
        /// <remarks>
        /// <para><b>Why the count lives here rather than on the event stream.</b> Every coin a hero
        /// collects in this build passes through <see cref="Award"/> or <see cref="TryGrant"/> -
        /// p.13's Defeat and assist coins, a minion's bounty, p.8's Pity Coin and card text's own
        /// grants - and this is the only place in the build where a balance goes up at all. The
        /// alternative was to count from the rule events, which cannot be done: p.13's reward fact
        /// names the Defeat and not the payout, and a card-text grant reaches the stream only as an
        /// applied node. Counting here also means a coin source added later is counted without
        /// anything being told about it.</para>
        /// <para><b>A transfer is not a collection</b> and deliberately does not call this: the
        /// coins it moves were counted once when the hero who holds them earned them, and counting
        /// them again on arrival would make the table's total grow by handing coins around. A spend
        /// does not call it either, because the column is what a hero collected over the match
        /// rather than what they still hold.</para>
        /// </remarks>
        public void BindScoreboard(GuardsDigital.Core.Rules.Victory.MatchScoreboard matchScoreboard)
        {
            scoreboard = matchScoreboard;
        }

        /// <summary>
        /// W-022, PVE-D11/D14/D22. A mode without coins: every Defeat award and every grant is
        /// accepted and adds nothing, so a card's "if they do" still proceeds and no balance, reward
        /// or scoreboard count moves. False - every existing match - changes nothing.
        /// </summary>
        public bool GainsAddNothing { get; private set; }

        public void SetGainsAddNothing(bool value)
        {
            GainsAddNothing = value;
        }

        public static HeroCoinLedger Restore(
            MatchParticipants participants,
            HeroCoinLedgerRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new HeroCoinLedger(
                participants,
                state.CoinBalances,
                state.AwardedOutcomeIds);
        }

        public int GetCoins(HeroId heroId)
        {
            if (!TryGetCoins(heroId, out var coins))
            {
                throw new KeyNotFoundException($"Unknown hero ID '{heroId}'.");
            }

            return coins;
        }

        /// <summary>Purely reads one known participant's balance without throwing for stale input.</summary>
        public bool TryGetCoins(HeroId heroId, out int coins)
        {
            coins = 0;
            return heroId.IsDefined && coinBalances.TryGetValue(heroId, out coins);
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

        /// <summary>
        /// Adds coins to a hero's balance from a source that is not a Defeat - rulebook p.8's Pity Coin
        /// being the only one this build has.
        ///
        /// It is a third operation rather than a reuse of <see cref="Award"/> because that method takes
        /// a <see cref="DefeatOutcome"/> and enforces what one means: an enemy team, a non-friendly
        /// target, and one payout per outcome. A Pity Coin has none of those - nobody was defeated - so
        /// expressing it there would have meant inventing an outcome to satisfy the checks, which is the
        /// shape milestone 7.2c's record warns about when a literal is dressed up as a binding.
        ///
        /// Like the spend beside it, this decides nothing about whether the coin is owed. Who is
        /// eligible and when is the calling rule's, which is where p.8 is quoted.
        /// </summary>
        public bool TryGrant(
            HeroId heroId,
            int amount,
            CoinGrantKind grantKind,
            out CoinGrantOperation operation)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount), amount, "A coin grant is a positive number of coins.");
            }

            var balanceBefore = GetCoins(heroId);
            if (GainsAddNothing)
            {
                operation = CoinGrantOperation.AddingNothing(
                    heroId, participants.GetTeamForHero(heroId).Id, grantKind, amount, balanceBefore);
                return true;
            }

            var candidateBalance = (long)balanceBefore + amount;
            if (candidateBalance > int.MaxValue)
            {
                operation = default;
                return false;
            }

            operation = new CoinGrantOperation(
                heroId,
                participants.GetTeamForHero(heroId).Id,
                grantKind,
                amount,
                balanceBefore,
                (int)candidateBalance);
            coinBalances[heroId] = operation.BalanceAfter;
            scoreboard?.RecordCoinsEarned(heroId, amount);
            return true;
        }

        /// <summary>Whether a positive grant can be committed without overflowing the balance.</summary>
        public bool CanGrant(HeroId heroId, int amount)
        {
            return amount > 0 && TryGetCoins(heroId, out var balance) &&
                (long)balance + amount <= int.MaxValue;
        }

        /// <summary>
        /// Transfers existing coins in one commit. Every refusal leaves both balances untouched, so a
        /// stale choice cannot spend from one hero and fail while crediting the other.
        /// </summary>
        public bool TryTransfer(
            HeroId sourceHeroId,
            HeroId recipientHeroId,
            int amount,
            out CoinTransferOperation operation)
        {
            if (!sourceHeroId.IsDefined || !recipientHeroId.IsDefined ||
                sourceHeroId.Equals(recipientHeroId) || amount <= 0)
            {
                operation = default;
                return false;
            }

            if (!TryGetCoins(sourceHeroId, out var sourceBefore) ||
                !TryGetCoins(recipientHeroId, out var recipientBefore))
            {
                operation = default;
                return false;
            }
            var recipientAfter = (long)recipientBefore + amount;
            if (sourceBefore < amount || recipientAfter > int.MaxValue)
            {
                operation = default;
                return false;
            }

            operation = new CoinTransferOperation(
                sourceHeroId,
                recipientHeroId,
                amount,
                sourceBefore,
                sourceBefore - amount,
                recipientBefore,
                (int)recipientAfter);
            coinBalances[sourceHeroId] = operation.SourceBalanceAfter;
            coinBalances[recipientHeroId] = operation.RecipientBalanceAfter;
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

            if (GainsAddNothing)
            {
                // W-022, PVE-D11. The Defeat is still awarded exactly once; it pays nobody.
                awardedOutcomeIds.Add(outcome.Id);
                return DefeatRewardResult.Accept(Array.Empty<CoinResourceOperation>());
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
                // A zero-coin line is a real planned operation - p.13's schedule can owe a Defeat
                // no coins at all - and it is not a collection, so it is not counted.
                if (operation.Amount > 0)
                {
                    scoreboard?.RecordCoinsEarned(operation.RecipientHeroId, operation.Amount);
                }
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
