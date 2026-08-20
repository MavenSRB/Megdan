using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>Which part of one purchased level a paused or completed item is about.</summary>
    public enum HeroLevelUpItemKind
    {
        None = 0,

        /// <summary>p.15's level up itself: the coins are spent and the Level indicator moves.</summary>
        Purchase = 1,

        /// <summary>p.15's Upgrade a Card step, which is the one thing a level asks a player.</summary>
        Upgrade = 2,

        /// <summary>
        /// p.16's alternative: "instead of upgrading a card and gaining an item, you unlock the
        /// Ultimate card". It is a separate kind rather than a flag on <see cref="Upgrade"/> because it
        /// is the page's own either/or, and because the two must be separately recorded for a resumed
        /// transition to be unable to do both for one level.
        /// </summary>
        Ultimate = 3
    }

    /// <summary>
    /// One item of the level-up step: which hero it is about, which of that hero's levels, and which
    /// half of that level.
    ///
    /// All three are in the identity because all three are what an answer has to match. Milestone
    /// 6.11's respawn put the Spawn point and the miniature in its item id for the same reason - the
    /// fifth acceptance check calls it "stable hero/level/choice identity" here, and an answer that
    /// named the right hero and the wrong level would otherwise be indistinguishable from the right one
    /// when a hero buys several levels in one end of round.
    ///
    /// The Level recorded is the one the purchase <em>reached</em>, so it is unique per hero within one
    /// transition: p.15's purchases are consecutive and never repeat a row of its table.
    /// </summary>
    public readonly struct HeroLevelUpItem : IEquatable<HeroLevelUpItem>
    {
        private const char Separator = ':';

        public HeroLevelUpItem(HeroId heroId, HeroLevel level, HeroLevelUpItemKind kind)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("A level-up item names the hero it is about.", nameof(heroId));
            }

            if (!level.IsDefined)
            {
                throw new ArgumentException("A level-up item names a legal Level.", nameof(level));
            }

            if (kind == HeroLevelUpItemKind.None)
            {
                throw new ArgumentException("A level-up item names which half of the level it is.", nameof(kind));
            }

            HeroId = heroId;
            Level = level;
            Kind = kind;
        }

        public HeroId HeroId { get; }

        public HeroLevel Level { get; }

        public HeroLevelUpItemKind Kind { get; }

        public SystemTransitionItemId ToItemId() =>
            new SystemTransitionItemId(string.Concat(
                HeroId.Value,
                Separator,
                Level.Value.ToString(CultureInfo.InvariantCulture),
                Separator,
                Token(Kind)));

        /// <summary>
        /// Reads an item back. The hero id is taken as everything before the last two fields rather
        /// than as the first, so a hero id is not quietly constrained by this encoding.
        /// </summary>
        public static HeroLevelUpItem Read(SystemTransitionItemId itemId)
        {
            var value = itemId.Value ?? string.Empty;
            var lastSeparator = value.LastIndexOf(Separator);
            var firstSeparator = lastSeparator <= 0
                ? -1
                : value.LastIndexOf(Separator, lastSeparator - 1);
            if (firstSeparator <= 0 ||
                !int.TryParse(
                    value.Substring(firstSeparator + 1, lastSeparator - firstSeparator - 1),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var level) ||
                level < HeroLevel.Minimum || level > HeroLevel.Maximum ||
                !TryReadKind(value.Substring(lastSeparator + 1), out var kind))
            {
                throw new InvalidOperationException(
                    $"Level-up item '{itemId}' does not name a hero, a Level and a part of that level.");
            }

            return new HeroLevelUpItem(
                new HeroId(value.Substring(0, firstSeparator)), new HeroLevel(level), kind);
        }

        private static string Token(HeroLevelUpItemKind kind)
        {
            switch (kind)
            {
                case HeroLevelUpItemKind.Purchase:
                    return "purchase";
                case HeroLevelUpItemKind.Upgrade:
                    return "upgrade";
                case HeroLevelUpItemKind.Ultimate:
                    return "ultimate";
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown level-up item kind.");
            }
        }

        private static bool TryReadKind(string token, out HeroLevelUpItemKind kind)
        {
            switch (token)
            {
                case "purchase":
                    kind = HeroLevelUpItemKind.Purchase;
                    return true;
                case "upgrade":
                    kind = HeroLevelUpItemKind.Upgrade;
                    return true;
                case "ultimate":
                    kind = HeroLevelUpItemKind.Ultimate;
                    return true;
                default:
                    kind = HeroLevelUpItemKind.None;
                    return false;
            }
        }

        public bool Equals(HeroLevelUpItem other) =>
            HeroId.Equals(other.HeroId) && Level.Equals(other.Level) && Kind == other.Kind;

        public override bool Equals(object obj) => obj is HeroLevelUpItem other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = HeroId.GetHashCode();
                hashCode = (hashCode * 397) ^ Level.GetHashCode();
                return (hashCode * 397) ^ (int)Kind;
            }
        }

        public override string ToString() => ToItemId().Value;
    }

    /// <summary>
    /// One legal answer to p.15's Upgrade a Card step: the card in hand that is upgraded, and the
    /// card taken from the deck in its place.
    ///
    /// The page asks two questions - "Choose a non-basic Action card in your hand with the lowest card
    /// Tier or tied for the lowest" and then which of the two same-colour next-Tier cards to take - and
    /// this is one option that answers both. That is a decision about where an answer can be recorded
    /// rather than a reading of the page. Milestone 6.10a's continuation records where a transition is
    /// and what it may be answered with, and nothing else; the second question's options are a function
    /// of the first question's answer, so two sequential pauses would need the first answer kept
    /// somewhere between them, and the only somewhere is a second record of the resume location - the
    /// exact defect that contract exists to make unrepresentable. Naming both cards in one option keeps
    /// one record, and loses nothing the page states: both decisions are still the player's, still
    /// authoritative, and still refused when they name anything the step did not offer.
    ///
    /// The unchosen sibling is deliberately not part of the option. p.15 does not offer it - "set the
    /// other one aside" is a consequence rather than a choice - so it is derived when the answer is
    /// applied.
    /// </summary>
    public readonly struct HeroLevelUpOption : IEquatable<HeroLevelUpOption>
    {
        private const string Separator = "->";

        public HeroLevelUpOption(CardInstanceId upgradedCardInstanceId, CardInstanceId takenCardInstanceId)
        {
            if (!upgradedCardInstanceId.IsDefined || !takenCardInstanceId.IsDefined)
            {
                throw new ArgumentException(
                    "An upgrade option names the card in hand and the card taken in its place.");
            }

            UpgradedCardInstanceId = upgradedCardInstanceId;
            TakenCardInstanceId = takenCardInstanceId;
        }

        /// <summary>p.15's "Choose a non-basic Action card in your hand with the lowest card Tier".</summary>
        public CardInstanceId UpgradedCardInstanceId { get; }

        /// <summary>p.15's "Take one of those cards into your hand".</summary>
        public CardInstanceId TakenCardInstanceId { get; }

        public SystemTransitionOptionId ToOptionId() =>
            new SystemTransitionOptionId(
                string.Concat(UpgradedCardInstanceId.Value, Separator, TakenCardInstanceId.Value));

        public static HeroLevelUpOption Read(SystemTransitionOptionId optionId)
        {
            var value = optionId.Value ?? string.Empty;
            var separator = value.IndexOf(Separator, StringComparison.Ordinal);
            if (separator <= 0 || separator + Separator.Length >= value.Length)
            {
                throw new InvalidOperationException(
                    $"Upgrade option '{optionId}' does not name an upgraded card and a taken card.");
            }

            return new HeroLevelUpOption(
                new CardInstanceId(value.Substring(0, separator)),
                new CardInstanceId(value.Substring(separator + Separator.Length)));
        }

        public bool Equals(HeroLevelUpOption other) =>
            UpgradedCardInstanceId.Equals(other.UpgradedCardInstanceId) &&
            TakenCardInstanceId.Equals(other.TakenCardInstanceId);

        public override bool Equals(object obj) => obj is HeroLevelUpOption other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (UpgradedCardInstanceId.GetHashCode() * 397) ^ TakenCardInstanceId.GetHashCode();
            }
        }

        public override string ToString() => ToOptionId().Value;
    }

    /// <summary>What one part of one hero's leveling actually did.</summary>
    public enum HeroLevelUpOutcomeKind
    {
        None = 0,

        /// <summary>p.15: coins spent, Level raised by one.</summary>
        LevelPurchased = 1,

        /// <summary>p.15's Upgrade a Card: one card returned to the deck, one taken, one set aside.</summary>
        CardUpgraded = 2,

        /// <summary>p.16's Gain an Item: one set-aside card tucked under a matching slot.</summary>
        ItemGained = 3,

        /// <summary>p.16's Ultimate, unlocked instead of an upgrade and an item.</summary>
        UltimateUnlocked = 4
    }

    /// <summary>
    /// One thing p.15 and p.16's leveling did, reported so the round can turn it into an event.
    ///
    /// The rules report rather than raise, in the shape milestone 6.11's Lane Push already uses: a step
    /// rules implementation returns a resolution to the transition authority and has nowhere to put an
    /// event, so what happened is accumulated here and drained by the round that owns the event stream.
    /// </summary>
    public sealed class HeroLevelUpOutcome
    {
        private HeroLevelUpOutcome(
            HeroLevelUpOutcomeKind kind,
            HeroId heroId,
            HeroLevel level,
            CoinSpendOperation spend,
            CardInstanceId upgradedCardInstanceId,
            CardInstanceId takenCardInstanceId,
            CardInstanceId setAsideCardInstanceId,
            CardInstanceId ultimateCardInstanceId,
            HeroItem item)
        {
            Kind = kind;
            HeroId = heroId;
            Level = level;
            Spend = spend;
            UpgradedCardInstanceId = upgradedCardInstanceId;
            TakenCardInstanceId = takenCardInstanceId;
            SetAsideCardInstanceId = setAsideCardInstanceId;
            UltimateCardInstanceId = ultimateCardInstanceId;
            Item = item;
        }

        public HeroLevelUpOutcomeKind Kind { get; }

        public HeroId HeroId { get; }

        /// <summary>The Level this outcome belongs to - the one the purchase reached.</summary>
        public HeroLevel Level { get; }

        public CoinSpendOperation Spend { get; }

        public CardInstanceId UpgradedCardInstanceId { get; }

        public CardInstanceId TakenCardInstanceId { get; }

        /// <summary>
        /// The sibling candidate p.15 sets aside, or the named absence when the deck held only the card
        /// that was taken.
        /// </summary>
        public CardInstanceId SetAsideCardInstanceId { get; }

        public CardInstanceId UltimateCardInstanceId { get; }

        public HeroItem Item { get; }

        public static HeroLevelUpOutcome LevelPurchased(
            HeroId heroId, HeroLevel level, CoinSpendOperation spend) =>
            new HeroLevelUpOutcome(
                HeroLevelUpOutcomeKind.LevelPurchased, heroId, level, spend,
                default, default, default, default, null);

        public static HeroLevelUpOutcome CardUpgraded(
            HeroId heroId,
            HeroLevel level,
            CardInstanceId upgradedCardInstanceId,
            CardInstanceId takenCardInstanceId,
            CardInstanceId setAsideCardInstanceId) =>
            new HeroLevelUpOutcome(
                HeroLevelUpOutcomeKind.CardUpgraded, heroId, level, default,
                upgradedCardInstanceId, takenCardInstanceId, setAsideCardInstanceId, default, null);

        public static HeroLevelUpOutcome ItemGained(HeroItem item) =>
            new HeroLevelUpOutcome(
                HeroLevelUpOutcomeKind.ItemGained,
                (item ?? throw new ArgumentNullException(nameof(item))).OwnerId,
                default, default, default, default, default, default, item);

        public static HeroLevelUpOutcome UltimateUnlocked(
            HeroId heroId, HeroLevel level, CardInstanceId ultimateCardInstanceId) =>
            new HeroLevelUpOutcome(
                HeroLevelUpOutcomeKind.UltimateUnlocked, heroId, level, default,
                default, default, default, ultimateCardInstanceId, null);

        public override string ToString() => $"{HeroId} {Kind} (Level {Level})";
    }

    /// <summary>
    /// Rulebook p.15's Leveling Up and p.16's Gain an Item, as a catalog-owned multi-step system
    /// transition (milestone 7.5).
    ///
    /// This is the second production consumer of milestone 6.10a's continuation contract, which named
    /// it in the same breath as the Lane Push and built neither. The two steps are the two headings the
    /// rulebook prints: p.15's "Upgrade a Card", which every purchased level runs and which is the only
    /// thing leveling asks a player, and p.16's "Gain an Item", which turns each card set aside during
    /// those upgrades into an item. They are separate steps for the reason p.8's removal and respawn
    /// are: the acceptance check asks that an answered upgrade cannot repeat a purchase, a cost, an
    /// item or an Ultimate unlock, and only separately recorded steps are separately unrepeatable.
    ///
    /// Only the first step can pause, and it pauses once per purchased level. p.16's Gain an Item asks
    /// nobody anything - the slot is the one the card's own icon matches - so it resolves itself.
    /// </summary>
    public static class HeroLevelUpSystemTransition
    {
        public static SystemTransitionDefinitionId DefinitionId { get; } =
            new SystemTransitionDefinitionId("end-of-round-level-up");

        /// <summary>p.15: "you must purchase a level up", and its Upgrade a Card step for each one.</summary>
        public static SystemTransitionStepId PurchaseAndUpgradeStepId { get; } =
            new SystemTransitionStepId("level-up-purchase-and-upgrade");

        /// <summary>p.16: "Take each card set aside during the Upgrade card step and tuck it Faceup".</summary>
        public static SystemTransitionStepId GainItemsStepId { get; } =
            new SystemTransitionStepId("level-up-gain-items");

        public static SystemTransitionDefinition Definition { get; } =
            new SystemTransitionDefinition(
                DefinitionId,
                "Leveling Up",
                new List<SystemTransitionStepDefinition>
                {
                    SystemTransitionStepDefinition.Derived(PurchaseAndUpgradeStepId),
                    new SystemTransitionStepDefinition(GainItemsStepId)
                });
    }

    /// <summary>
    /// What p.15's and p.16's leveling steps do, consulted by the continuation authority and holding no
    /// record of where the transition is.
    ///
    /// The division is milestone 6.10a's conformance rule: this holds the match's participants, content
    /// and the three states a purchase moves, asks them questions, and is told by its caller which step
    /// and which item it is being asked about. Where the transition is lives in exactly one place, and
    /// it is the place the snapshot carries.
    ///
    /// One call of <see cref="ResolveStep"/> either changes something and reports an item finished, or
    /// changes nothing and stops. That is not a style choice: a restored continuation is validated by
    /// re-running this method over the restored state and requiring it to reproduce the same pause, so a
    /// call that mutated on its way to stopping would apply a purchased level twice for every restore.
    /// It is the same invariant the Lane-Push respawn keeps.
    /// </summary>
    public sealed class HeroLevelUpStepRules
        : ISystemTransitionStepRules, IDescribingSystemTransitionStepRules
    {
        private readonly MatchParticipants participants;
        private readonly IReadOnlyList<CardDefinition> definitions;
        private readonly IReadOnlyList<CardInstance> instances;
        private readonly List<HeroLevelUpOutcome> outcomes = new List<HeroLevelUpOutcome>();
        private HeroProgressionState levels;
        private HeroItemState items;
        private HeroCoinLedger coins;

        public HeroLevelUpStepRules(
            MatchParticipants participants,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            this.participants = participants ?? throw new ArgumentNullException(nameof(participants));
            this.definitions = Array.AsReadOnly(
                (definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray());
            this.instances = Array.AsReadOnly(
                (instances ?? throw new ArgumentNullException(nameof(instances))).ToArray());
        }

        /// <summary>
        /// Hands these rules the three states a level purchase moves: the Levels it raises, the items it
        /// creates, and the coin ledger it spends from.
        ///
        /// It is a binding rather than three constructor arguments for the reason
        /// <c>CardActionResolutionAuthority.BindHeroItems</c> is one: the round authority owns all three
        /// and is built after the transition authority these rules are composed into. Public rather than
        /// internal so a focused fixture can compose the rules directly against states it owns, which is
        /// the only way to prove one step without a whole match around it.
        /// </summary>
        public void Bind(HeroProgressionState levels, HeroItemState items, HeroCoinLedger coins)
        {
            this.levels = levels ?? throw new ArgumentNullException(nameof(levels));
            this.items = items ?? throw new ArgumentNullException(nameof(items));
            this.coins = coins ?? throw new ArgumentNullException(nameof(coins));
        }

        /// <summary>What the leveling has done since <see cref="DrainOutcomes"/> was last called.</summary>
        public IReadOnlyList<HeroLevelUpOutcome> DrainOutcomes()
        {
            var drained = outcomes.ToArray();
            outcomes.Clear();
            return Array.AsReadOnly(drained);
        }

        public SystemTransitionStepResolution ResolveStep(SystemTransitionStepContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (context.Step.Id.Equals(HeroLevelUpSystemTransition.PurchaseAndUpgradeStepId))
            {
                return ResolvePurchaseAndUpgrade(context);
            }

            if (context.Step.Id.Equals(HeroLevelUpSystemTransition.GainItemsStepId))
            {
                return ResolveItemGain();
            }

            throw new InvalidOperationException(
                $"Level-up step '{context.Step.Id}' has no rules; every step of a registered transition does.");
        }

        /// <summary>
        /// p.15's Upgrade a Card step, applied to the level the paused item names.
        ///
        /// Nothing is re-derived from the answer beyond the sibling the page sets aside, and the three
        /// moves are the page's three sentences in its order: return, take, set aside.
        /// </summary>
        public void ApplyAnswer(SystemTransitionAnswerContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!context.Step.Id.Equals(HeroLevelUpSystemTransition.PurchaseAndUpgradeStepId))
            {
                throw new InvalidOperationException(
                    $"Level-up step '{context.Step.Id}' asks nobody anything, so nothing answers it.");
            }

            var item = HeroLevelUpItem.Read(context.ItemId);
            if (item.Kind != HeroLevelUpItemKind.Upgrade)
            {
                throw new InvalidOperationException(
                    $"Level-up item '{context.ItemId}' is not the part of a level a player answers.");
            }

            var option = HeroLevelUpOption.Read(context.OptionId);
            var upgraded = Card(option.UpgradedCardInstanceId);
            var taken = Card(option.TakenCardInstanceId);
            var setAside = FindSibling(item.HeroId, upgraded, taken);

            // Nothing else in the match runs while a transition is paused, so the cards the options were
            // derived from are the cards they are applied to. A legal option that cannot be applied is an
            // engine defect rather than a rules outcome.
            if (!CardLifecycleAuthority.TryReturnToDeck(upgraded) ||
                !CardLifecycleAuthority.TryTakeFromDeckIntoHand(taken) ||
                (setAside != null && !CardLifecycleAuthority.TrySetAsideFromDeck(setAside)))
            {
                throw new InvalidOperationException(
                    $"A validated upgrade of '{option.UpgradedCardInstanceId}' into " +
                    $"'{option.TakenCardInstanceId}' could not be committed.");
            }

            outcomes.Add(HeroLevelUpOutcome.CardUpgraded(
                item.HeroId,
                item.Level,
                upgraded.Id,
                taken.Id,
                setAside?.Id ?? default));
        }

        /// <summary>
        /// p.15's leveling, one indivisible piece at a time: finish the purchase somebody has already
        /// made, or make the next one.
        ///
        /// That order is what lets a paused transition be restored. A pause can only exist while a
        /// purchase is outstanding, so re-running this over restored state reaches the first branch and
        /// reproduces the pause from the cards alone - before anything has to ask what a hero can
        /// afford, which is state the payload has not been read far enough to supply.
        ///
        /// Heroes are taken in hero-id order, and a hero finishes every level it can afford before the
        /// next hero starts. p.15 has them level "simultaneously" and then wait for each other, and
        /// nothing one hero's purchase does can change what another's offers - the cards, the coins and
        /// the Level are all that hero's own - so the order is a serialization of the page's
        /// simultaneity rather than a rule about who goes first.
        /// </summary>
        private SystemTransitionStepResolution ResolvePurchaseAndUpgrade(SystemTransitionStepContext context)
        {
            var completed = context.CompletedItemIds.Select(HeroLevelUpItem.Read).ToArray();
            foreach (var hero in OrderedHeroes())
            {
                if (TryFindOutstandingPurchase(completed, hero.Id, out var level))
                {
                    return ResolveOutstandingPurchase(hero.Id, level);
                }
            }

            foreach (var hero in OrderedHeroes())
            {
                if (TryPurchaseLevel(hero.Id, out var purchased))
                {
                    return purchased;
                }
            }

            return SystemTransitionStepResolution.Completed;
        }

        /// <summary>
        /// The level somebody has bought and not yet spent, or none. More than one at a time is an
        /// engine defect rather than a state: this step never begins a purchase while one is
        /// outstanding, so two would mean a purchase was completed without its consequence.
        /// </summary>
        private static bool TryFindOutstandingPurchase(
            IReadOnlyList<HeroLevelUpItem> completed,
            HeroId heroId,
            out HeroLevel level)
        {
            var spent = new HashSet<int>(completed
                .Where(item => item.HeroId.Equals(heroId) && item.Kind != HeroLevelUpItemKind.Purchase)
                .Select(item => item.Level.Value));
            var outstanding = completed
                .Where(item => item.HeroId.Equals(heroId) &&
                    item.Kind == HeroLevelUpItemKind.Purchase &&
                    !spent.Contains(item.Level.Value))
                .Select(item => item.Level)
                .ToArray();
            if (outstanding.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Hero '{heroId}' has {outstanding.Length} purchased levels waiting to be spent; " +
                    "p.15 finishes each one before the next is bought.");
            }

            level = outstanding.FirstOrDefault();
            return outstanding.Length == 1;
        }

        private SystemTransitionStepResolution ResolveOutstandingPurchase(HeroId heroId, HeroLevel level)
        {
            if (TryFindUnlockableUltimate(heroId, out var ultimateCard))
            {
                var unlock = UltimateUnlockQuery.TryUnlock(
                    ultimateCard,
                    UltimateUnlockQuery.UpgradeCompletionReading(heroId),
                    definitions,
                    instances);
                if (!unlock.IsUnlocked)
                {
                    throw new InvalidOperationException(
                        $"Hero '{heroId}' reached p.16's Ultimate unlock and it was refused as " +
                        $"{unlock.RejectionReason}.");
                }

                outcomes.Add(HeroLevelUpOutcome.UltimateUnlocked(heroId, level, ultimateCard.Id));
                return SystemTransitionStepResolution.ItemCompleted(
                    new HeroLevelUpItem(heroId, level, HeroLevelUpItemKind.Ultimate).ToItemId());
            }

            var options = BuildUpgradeOptions(heroId);
            if (options.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Hero '{heroId}' bought a level and p.15's Upgrade a Card step offers nothing: no " +
                    "non-Basic card in hand leads to a card of the next Tier in that hero's deck.");
            }

            return SystemTransitionStepResolution.Paused(
                new HeroLevelUpItem(heroId, level, HeroLevelUpItemKind.Upgrade).ToItemId(),
                participants.GetTeamForHero(heroId).Side,
                options.Select(option => option.ToOptionId()));
        }

        /// <summary>
        /// p.15's mandatory purchase for one hero: "you must purchase a level up if you have enough
        /// coins", and "you must purchase as many levels as you can afford".
        ///
        /// Four things stop it, and each is a printed sentence. p.15's table ends at Level 8. p.16 says
        /// an unlocked Ultimate ends leveling outright: "you cannot gain any additional levels, but can
        /// still gain coins". The coins have to be there. And the level has to have something to spend
        /// itself on - p.15 presents the level and its upgrade as one transaction, so a hero with no
        /// card to upgrade and no Ultimate to unlock has no purchase to make; in this build's content
        /// that case is unreachable until the Ultimate is unlocked, and refusing it here is what keeps a
        /// fixture with unusual content from spending a coin on nothing.
        /// </summary>
        private bool TryPurchaseLevel(HeroId heroId, out SystemTransitionStepResolution resolution)
        {
            resolution = null;
            RequireBinding();
            var current = levels.GetLevel(heroId);
            if (!HeroLevelSchedule.HasNextLevel(current) ||
                UltimateUnlockQuery.Derive(definitions, instances).IsUnlockedFor(heroId) ||
                !CanCompletePurchase(heroId))
            {
                return false;
            }

            if (!coins.TrySpend(
                    heroId, HeroLevelSchedule.LevelUpCost(current), CoinSpendKind.LevelUp, out var spend))
            {
                return false;
            }

            if (!levels.TryRaiseLevel(heroId, out var raised))
            {
                throw new InvalidOperationException(
                    $"Hero '{heroId}' paid for a level p.15's table does not have.");
            }

            outcomes.Add(HeroLevelUpOutcome.LevelPurchased(heroId, raised, spend));
            resolution = SystemTransitionStepResolution.ItemCompleted(
                new HeroLevelUpItem(heroId, raised, HeroLevelUpItemKind.Purchase).ToItemId());
            return true;
        }

        /// <summary>
        /// Milestone 7.8a. What p.15's Upgrade a Card is asking, described from the state that produced
        /// the pause: whose level it is, which level was reached, what it cost, what is left, and for
        /// each option the three cards p.15 moves plus the icon p.16 files the third one under.
        ///
        /// Every card is named by its instance and its definition reference rather than by a display
        /// name, so the words belong to whichever surface draws them and none of them is produced by
        /// decoding an option identity. The sibling is found by <see cref="FindSibling"/> - the same
        /// method <see cref="ApplyAnswer"/> sets aside - so a described option and an applied one cannot
        /// disagree about which card the player is about to lose.
        ///
        /// The paid cost is p.15's table read one row back. A purchase raises the Level and then pauses,
        /// so the level the pause names is the one already bought, and its price is the cost printed for
        /// the level the hero was on before it.
        /// </summary>
        public SystemTransitionStepDescription Describe(PendingSystemTransitionStep pendingStep)
        {
            if (pendingStep == null ||
                !pendingStep.DefinitionId.Equals(HeroLevelUpSystemTransition.DefinitionId) ||
                !pendingStep.StepId.Equals(HeroLevelUpSystemTransition.PurchaseAndUpgradeStepId) ||
                !pendingStep.ItemId.IsDefined ||
                levels == null || items == null || coins == null)
            {
                return null;
            }

            var item = HeroLevelUpItem.Read(pendingStep.ItemId);
            if (item.Kind != HeroLevelUpItemKind.Upgrade || !item.Level.IsDefined ||
                item.Level.Value <= HeroLevel.Minimum)
            {
                return null;
            }

            var dashboard = participants.Heroes
                .FirstOrDefault(hero => hero.Id.Equals(item.HeroId))?.Dashboard;
            if (dashboard == null)
            {
                return null;
            }

            var described = new List<SystemTransitionOptionDescription>();
            foreach (var optionId in pendingStep.Options)
            {
                var option = HeroLevelUpOption.Read(optionId);
                var upgraded = Card(option.UpgradedCardInstanceId);
                var taken = Card(option.TakenCardInstanceId);
                var setAside = FindSibling(item.HeroId, upgraded, taken);
                var icon = PrintedValueChannel.None;
                var slot = default(HeroDashboardSlotPosition);
                var setAsideDefinition = setAside == null ? null : DefinitionOf(setAside);
                if (setAsideDefinition != null && setAsideDefinition.Progression.HasItemIcon &&
                    dashboard.TryFindSlotAccepting(setAsideDefinition.Progression.ItemIcon, out var accepting))
                {
                    icon = setAsideDefinition.Progression.ItemIcon;
                    slot = accepting.Position;
                }

                described.Add(SystemTransitionOptionDescription.Upgrade(
                    optionId,
                    upgraded.Id,
                    upgraded.DefinitionReference,
                    taken.Id,
                    taken.DefinitionReference,
                    setAside?.Id ?? default,
                    setAside?.DefinitionReference ?? default,
                    icon,
                    slot));
            }

            return new SystemTransitionStepDescription(
                SystemTransitionPromptKind.LevelUpUpgrade,
                pendingStep,
                item.HeroId,
                described,
                item.Level.Value,
                HeroLevelSchedule.LevelUpCost(new HeroLevel(item.Level.Value - 1)),
                coins.GetCoins(item.HeroId));
        }

        private bool CanCompletePurchase(HeroId heroId) =>
            TryFindUnlockableUltimate(heroId, out _) || BuildUpgradeOptions(heroId).Count > 0;

        /// <summary>
        /// p.16's condition and its card together: this hero has upgraded all their cards to Tier III,
        /// and the Tier IV card is still where p.6 left it.
        /// </summary>
        private bool TryFindUnlockableUltimate(HeroId heroId, out CardInstance ultimateCard)
        {
            ultimateCard = null;
            if (!UltimateUnlockQuery.IsUpgradeComplete(
                    UltimateUnlockQuery.UpgradeCompletionReading(heroId), definitions, instances))
            {
                return false;
            }

            ultimateCard = instances
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Deck &&
                    DefinitionOf(card)?.Progression.IsUltimate == true)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .FirstOrDefault();
            return ultimateCard != null;
        }

        /// <summary>
        /// Every legal answer to p.15's Upgrade a Card step for one hero, in a stable order.
        ///
        /// The page's two sentences are read in order. "Choose a non-basic Action card in your hand with
        /// the lowest card Tier or tied for the lowest" gives the sources - Basic cards drop out because
        /// they are "not considered when checking your lowest card Tier", and so does every card that is
        /// not in the hand. "Find two Action cards in your deck that have the same color as the chosen
        /// card but are one Tier higher" gives the cards that may be taken, and those are the candidates
        /// the source's own definition names: milestone 7.1 made the relationship content the card
        /// states rather than a colour comparison, and <c>CardProgressionValidator</c> is what checks
        /// that statement against the colour and Tier rules.
        /// </summary>
        private IReadOnlyList<HeroLevelUpOption> BuildUpgradeOptions(HeroId heroId)
        {
            var handCards = instances
                .Where(card => card.OwnerId.Equals(heroId) && card.State.Location == CardLocation.Hand)
                .Select(card => new { Card = card, Definition = DefinitionOf(card) })
                .Where(entry => entry.Definition != null && entry.Definition.Progression.IsNonBasic)
                .ToArray();
            if (handCards.Length == 0)
            {
                return Array.Empty<HeroLevelUpOption>();
            }

            var lowestTier = handCards.Min(entry => (int)entry.Definition.Progression.Tier);
            return handCards
                .Where(entry => (int)entry.Definition.Progression.Tier == lowestTier)
                .OrderBy(entry => entry.Card.Id.Value, StringComparer.Ordinal)
                .SelectMany(entry => entry.Definition.Progression.NextTierCandidateIds
                    .SelectMany(candidateId => DeckCardsOf(heroId, candidateId))
                    .Select(candidate => new HeroLevelUpOption(entry.Card.Id, candidate.Id)))
                .ToArray();
        }

        /// <summary>
        /// p.15's "set the other one aside": the candidate of the upgraded card that the hero did not
        /// take, still in the deck.
        ///
        /// Absent is an answer rather than a failure. The page assumes both cards are there, and in this
        /// build's content they always are; a deck holding only the card that was taken leaves nothing
        /// to set aside and therefore no item, which is what the page's own instruction produces.
        /// </summary>
        private CardInstance FindSibling(HeroId heroId, CardInstance upgraded, CardInstance taken) =>
            (DefinitionOf(upgraded)?.Progression.NextTierCandidateIds ?? Array.Empty<CardDefinitionId>())
                .SelectMany(candidateId => DeckCardsOf(heroId, candidateId))
                .FirstOrDefault(candidate => !candidate.Id.Equals(taken.Id));

        private IEnumerable<CardInstance> DeckCardsOf(HeroId heroId, CardDefinitionId definitionId) =>
            instances
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.Deck &&
                    card.DefinitionReference.DefinitionId.Equals(definitionId))
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal);

        /// <summary>
        /// p.16's Gain an Item: "Take each card set aside during the Upgrade card step and tuck it
        /// Faceup under your hero dashboard, under the slot with a matching icon."
        ///
        /// Every set-aside card in the match, because that is what the page says and because the step
        /// runs once after every hero has finished upgrading. It asks nobody anything - which slot is
        /// the card's own icon's - so it resolves itself and can never be the step a snapshot is paused
        /// inside.
        ///
        /// The order is hero id then card instance id. p.16 attaches no consequence to which item a hero
        /// gained first, and <c>HeroItem.Order</c> exists so that an ordering the state does not hold is
        /// not one a later build can silently change; stating it by identity is what makes it
        /// reproducible by a replay and a restore rather than an accident of iteration.
        /// </summary>
        private SystemTransitionStepResolution ResolveItemGain()
        {
            RequireBinding();
            foreach (var card in instances
                .Where(card => card.State.Location == CardLocation.SetAside)
                .OrderBy(card => card.OwnerId.Value, StringComparer.Ordinal)
                .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray())
            {
                var placement = items.TryPlaceSetAsideCard(card, definitions);
                if (!placement.IsPlaced)
                {
                    throw new InvalidOperationException(
                        $"p.16's item for set-aside card '{card.Id}' was refused as " +
                        $"{placement.RejectionReason}.");
                }

                outcomes.Add(HeroLevelUpOutcome.ItemGained(placement.Item));
            }

            return SystemTransitionStepResolution.Completed;
        }

        private IEnumerable<Hero> OrderedHeroes() =>
            participants.Heroes.OrderBy(hero => hero.Id.Value, StringComparer.Ordinal);

        private CardDefinition DefinitionOf(CardInstance card) => definitions
            .FirstOrDefault(definition => definition.Reference.Equals(card.DefinitionReference));

        private CardInstance Card(CardInstanceId cardInstanceId) =>
            instances.FirstOrDefault(card => card.Id.Equals(cardInstanceId)) ??
            throw new InvalidOperationException($"Card '{cardInstanceId}' is not in this match.");

        private void RequireBinding()
        {
            if (levels == null || items == null || coins == null)
            {
                throw new InvalidOperationException(
                    "p.15's leveling moves a Level, a coin balance and a set of items, and this build " +
                    "composed the rules without them.");
            }
        }
    }
}
