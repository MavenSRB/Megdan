using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// One hero's unlocked Ultimate, derived from authoritative state rather than stored.
    ///
    /// Nothing here is a record the match keeps. The unlock is the card's own position - rulebook p.16
    /// places it "Faceup to the left side of your hero dashboard" - and everything else on this type is
    /// read from that card and its trusted definition. That is the same shape milestone 6.3 gave the
    /// token half of this seam, where a placed token exposes its definition's properties as continuous
    /// sources for as long as the instance is on the board and stores none of them, and it is why a
    /// restored match supplies the same rule again without the payload carrying it.
    ///
    /// It is deliberately not the shape milestone 7.2 gave items. An item records which slot it was
    /// tucked under because p.16 attaches a consequence to that choice ("if it was not placed in the
    /// appropriate item slot, the item has no effect!"), so a re-derived slot could never describe the
    /// case the page penalizes. The Ultimate has no such choice: there is one place it goes, and a
    /// second record of it could only ever agree or be wrong.
    /// </summary>
    public sealed class UnlockedUltimate
    {
        internal UnlockedUltimate(
            HeroId ownerId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionId sourceDefinitionId,
            UltimateRuleDefinition rule)
        {
            OwnerId = ownerId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionId = sourceDefinitionId;
            Rule = rule;
            Source = new ContinuousRuleSource(
                SourceId(sourceCardInstanceId, rule),
                ContinuousRuleSourceKind.Ultimate,
                ownerId,
                rule.RuleKind,
                UltimateRuleDefinition.ApplicabilityKind,
                sourceCardInstanceId);
        }

        public HeroId OwnerId { get; }

        /// <summary>The card instance that sits beside this hero's dashboard.</summary>
        public CardInstanceId SourceCardInstanceId { get; }

        public CardDefinitionId SourceDefinitionId { get; }

        /// <summary>The permanent ability that card prints.</summary>
        public UltimateRuleDefinition Rule { get; }

        /// <summary>
        /// The continuous source this Ultimate is, in the vocabulary the timing contract already uses
        /// for a rule that is not an Active effect: its own stable identity, its controller, the rule it
        /// supplies, and p.16's applicability. It is built here rather than kept anywhere, so it can
        /// never outlive the card it describes.
        /// </summary>
        public ContinuousRuleSource Source { get; }

        /// <summary>
        /// The source's stable identity: the card instance beside the dashboard plus the ability's own
        /// id. The card instance is already unique within a match, so this is unique without consulting
        /// anything, and it is reproduced identically by a restore that reads the same card.
        /// </summary>
        private static string SourceId(CardInstanceId cardInstanceId, UltimateRuleDefinition rule) =>
            $"ultimate:{cardInstanceId.Value}:{rule.RuleId}";

        public override string ToString() => $"{OwnerId} Ultimate {SourceDefinitionId} ({Rule})";
    }

    /// <summary>
    /// Every unlocked Ultimate in a match at one moment, ordered by hero id so the order is a fact
    /// rather than an accident of instance order.
    ///
    /// <see cref="None"/> is the named absence a caller writes when it genuinely has no card content to
    /// read, in the same spirit as <see cref="RuleModificationSet.Empty"/>: milestone 6.8f's rule is
    /// that an omitted rules input must be a statement rather than a default.
    /// </summary>
    public sealed class UnlockedUltimateSet
    {
        public static UnlockedUltimateSet None { get; } =
            new UnlockedUltimateSet(Array.Empty<UnlockedUltimate>());

        private UnlockedUltimateSet(IReadOnlyList<UnlockedUltimate> ultimates)
        {
            Ultimates = ultimates;
        }

        public IReadOnlyList<UnlockedUltimate> Ultimates { get; }

        public bool IsEmpty => Ultimates.Count == 0;

        public bool IsUnlockedFor(HeroId heroId) =>
            Ultimates.Any(ultimate => ultimate.OwnerId.Equals(heroId));

        internal static UnlockedUltimateSet Of(IEnumerable<UnlockedUltimate> ultimates)
        {
            var ordered = ultimates
                .OrderBy(ultimate => ultimate.OwnerId.Value, StringComparer.Ordinal)
                .ThenBy(ultimate => ultimate.SourceCardInstanceId.Value, StringComparer.Ordinal)
                .ToArray();
            return ordered.Length == 0 ? None : new UnlockedUltimateSet(Array.AsReadOnly(ordered));
        }
    }

    /// <summary>Why an attempt to unlock an Ultimate was refused.</summary>
    public enum UltimateUnlockRejectionReason
    {
        None = 0,

        /// <summary>The reading covers a different hero than the card belongs to.</summary>
        NotOwnedByHero = 1,

        /// <summary>
        /// The card's definition is not in the supplied content set, so nothing can say whether it is an
        /// Ultimate or what ability it prints.
        /// </summary>
        UnresolvedDefinition = 2,

        /// <summary>The card is not an Ultimate. p.16 unlocks "the Ultimate card" and no other.</summary>
        NotAnUltimate = 3,

        /// <summary>
        /// p.6's setup leaves the Tier IV card in the player's deck, and p.16 takes it from there. A
        /// card already beside the dashboard, in hand, or anywhere else is not that card.
        /// </summary>
        NotInDeck = 4,

        /// <summary>This hero already has an Ultimate beside their dashboard.</summary>
        AlreadyUnlocked = 5,

        /// <summary>
        /// p.16's condition is not met: this hero has not upgraded all their cards to Tier III, so the
        /// level they are buying still upgrades a card and gains an item.
        /// </summary>
        UpgradeIncomplete = 6
    }

    /// <summary>The outcome of one unlock attempt: what was unlocked, or why it was refused.</summary>
    public readonly struct UltimateUnlockResult
    {
        private UltimateUnlockResult(UnlockedUltimate ultimate, UltimateUnlockRejectionReason rejectionReason)
        {
            Ultimate = ultimate;
            RejectionReason = rejectionReason;
        }

        public UnlockedUltimate Ultimate { get; }

        public UltimateUnlockRejectionReason RejectionReason { get; }

        public bool IsUnlocked => RejectionReason == UltimateUnlockRejectionReason.None;

        internal static UltimateUnlockResult Unlocked(UnlockedUltimate ultimate) =>
            new UltimateUnlockResult(
                ultimate ?? throw new ArgumentNullException(nameof(ultimate)),
                UltimateUnlockRejectionReason.None);

        internal static UltimateUnlockResult Rejected(UltimateUnlockRejectionReason reason) =>
            reason == UltimateUnlockRejectionReason.None
                ? throw new ArgumentException("A refusal names its reason.", nameof(reason))
                : new UltimateUnlockResult(null, reason);
    }

    /// <summary>
    /// Rulebook p.16's Ultimate, as the two questions the page asks about one: which heroes currently
    /// have one, and whether a hero may unlock theirs now.
    ///
    /// Pure, like every other query in this build: it holds nothing, and both answers are functions of
    /// the definitions and card instances handed to them.
    /// </summary>
    public static class UltimateUnlockQuery
    {
        /// <summary>
        /// Every unlocked Ultimate the supplied cards describe.
        ///
        /// The whole question is "which cards are beside a dashboard", because that is where p.16 puts
        /// the card and nowhere else. A card there whose definition this content set cannot supply, or
        /// whose definition is not an Ultimate, is skipped rather than throwing - the same rule
        /// <c>CardCharacteristicQuery</c> follows, and for the same reason: a query is not the place to
        /// fail a match over content, and the setup validator is where such a card is refused.
        /// </summary>
        public static UnlockedUltimateSet Derive(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (instances == null) throw new ArgumentNullException(nameof(instances));

            var definitionsByReference = definitions
                .Where(definition => definition != null)
                .GroupBy(definition => definition.Reference)
                .ToDictionary(group => group.Key, group => group.First());

            return UnlockedUltimateSet.Of(instances
                .Where(instance => instance != null &&
                    instance.State.Location == CardLocation.UltimateArea)
                .Select(instance => definitionsByReference.TryGetValue(
                        instance.DefinitionReference, out var definition) &&
                    definition.Progression.IsUltimate &&
                    definition.UltimateRule != null
                        ? new UnlockedUltimate(
                            instance.OwnerId, instance.Id, definition.Id, definition.UltimateRule)
                        : null)
                .Where(ultimate => ultimate != null));
        }

        /// <summary>
        /// p.15's scope for "all your cards", as one named reading rather than a scope every caller
        /// re-invents.
        ///
        /// The page asks the Tier question of the hand and says so twice: its Upgrade a Card step opens
        /// with "Choose a non-basic Action card in your hand with the lowest card Tier", and it closes
        /// the level-up with "You should now have exactly 5 cards, 1 of each color, in your hand." The
        /// deck is deliberately not included, and that is not a simplification: p.15 returns every
        /// upgraded card to the deck, so a hero who has upgraded everything to Tier III still has the
        /// Tier I and Tier II cards they returned sitting in it, and a reading that covered the deck
        /// could never become complete. The item slots are excluded for the same reason from the other
        /// side - p.16's own Level 8 example shows Tier II cards tucked under the dashboard.
        ///
        /// Scope is still the caller's to state, exactly as it is for
        /// <c>CardCharacteristicQuery.TryGetLowestNonBasicTier</c>: this is the reading p.16's condition
        /// uses, offered by name so that the page's own scope has one home.
        /// </summary>
        public static CardCharacteristicCriteria UpgradeCompletionReading(HeroId heroId) =>
            new CardCharacteristicCriteria(
                ownerId: heroId,
                locations: new[] { CardLocation.Hand });

        /// <summary>
        /// p.16's condition: "Once you upgrade all your cards to Tier III".
        ///
        /// It is expressed as the lowest non-Basic Tier being III, which is the same thing said in the
        /// vocabulary p.15 already gives this build - and it composes rather than duplicates, because
        /// milestone 7.1's query is the one place that reads a Tier. Basic cards drop out of the reading
        /// on their own, since that page excludes them from it by name.
        ///
        /// False when the reading selects no non-Basic card at all. A hero holding nothing to upgrade
        /// has not completed the upgrades; answering true there would let an emptied hand unlock an
        /// Ultimate, which is the one way this condition could be satisfied by having fewer cards rather
        /// than better ones. No hero identity is read anywhere: the reading names an owner and the
        /// condition is the same for every hero there is.
        /// </summary>
        public static bool IsUpgradeComplete(
            CardCharacteristicCriteria reading,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances) =>
            CardCharacteristicQuery.TryGetLowestNonBasicTier(
                reading ?? throw new ArgumentNullException(nameof(reading)),
                definitions,
                instances,
                out var lowestTier) &&
            lowestTier == CardTier.III;

        /// <summary>
        /// p.16's unlock, performed on one Ultimate card: take it from the deck and place it Faceup
        /// beside its owner's dashboard.
        ///
        /// Every refusal is decided before anything moves, so a rejected attempt leaves the card exactly
        /// where it was; the card's transition is the last thing that happens, as it is for an item.
        /// The definition set is required rather than optional because whether this is an Ultimate at
        /// all, and what ability it prints, is definition content.
        ///
        /// What this does not do is decide when the unlock happens. p.16 attaches it to a level -
        /// "the next time you level up, instead of upgrading a card and gaining an item" - and the
        /// purchase that runs that step, its coin cost and its mandatory ordering are milestone 7.5's.
        /// So in a live match no hero reaches this, exactly as no hero changes a Level or gains an item
        /// today, and this is the transition 7.5 will call rather than introduce.
        /// </summary>
        public static UltimateUnlockResult TryUnlock(
            CardInstance ultimateCard,
            CardCharacteristicCriteria reading,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            if (ultimateCard == null) throw new ArgumentNullException(nameof(ultimateCard));
            if (reading == null) throw new ArgumentNullException(nameof(reading));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (instances == null) throw new ArgumentNullException(nameof(instances));

            var materializedDefinitions = definitions.ToArray();
            var materializedInstances = instances.ToArray();

            if (!reading.OwnerId.HasValue || !reading.OwnerId.Value.Equals(ultimateCard.OwnerId))
            {
                return UltimateUnlockResult.Rejected(UltimateUnlockRejectionReason.NotOwnedByHero);
            }

            var definition = materializedDefinitions.FirstOrDefault(candidate =>
                candidate != null && candidate.Reference.Equals(ultimateCard.DefinitionReference));
            if (definition == null)
            {
                return UltimateUnlockResult.Rejected(UltimateUnlockRejectionReason.UnresolvedDefinition);
            }

            if (!definition.Progression.IsUltimate || definition.UltimateRule == null)
            {
                return UltimateUnlockResult.Rejected(UltimateUnlockRejectionReason.NotAnUltimate);
            }

            // Asked before the deck question, and the order is the difference between a useful refusal
            // and a misleading one: a card already beside the dashboard is also not in the deck, so the
            // deck test would answer every repeated unlock with the least specific reason it has.
            if (Derive(materializedDefinitions, materializedInstances)
                .IsUnlockedFor(ultimateCard.OwnerId))
            {
                return UltimateUnlockResult.Rejected(UltimateUnlockRejectionReason.AlreadyUnlocked);
            }

            if (ultimateCard.State.Location != CardLocation.Deck)
            {
                return UltimateUnlockResult.Rejected(UltimateUnlockRejectionReason.NotInDeck);
            }

            if (!IsUpgradeComplete(reading, materializedDefinitions, materializedInstances))
            {
                return UltimateUnlockResult.Rejected(UltimateUnlockRejectionReason.UpgradeIncomplete);
            }

            if (!CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimateCard))
            {
                return UltimateUnlockResult.Rejected(UltimateUnlockRejectionReason.NotInDeck);
            }

            return UltimateUnlockResult.Unlocked(new UnlockedUltimate(
                ultimateCard.OwnerId, ultimateCard.Id, definition.Id, definition.UltimateRule));
        }
    }
}
