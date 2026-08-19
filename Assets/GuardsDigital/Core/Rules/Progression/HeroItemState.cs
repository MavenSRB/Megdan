using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// One item tucked under one hero dashboard: whose it is, which card became it, which slot it sits
    /// under, and where it falls in that hero's sequence of gained items.
    ///
    /// All four are recorded rather than derived, and each for its own reason. The owner and the source
    /// card are what p.16 physically places - "Take each card set aside during the Upgrade card step and
    /// tuck it Faceup under your hero dashboard". The slot is a placement decision the page attaches a
    /// consequence to: "If your item was not clearly visible or if it was not placed in the appropriate
    /// item slot, the item has no effect!" A slot re-derived from the card's icon on every read could
    /// never be wrong and so could never carry that consequence, which is the difference between
    /// recording where an item sits and assuming it. The order is the sequence the items were gained in;
    /// no rule in the source reads it, and it is recorded anyway because milestone 7.5 gains several
    /// items in one end-of-round step and an ordering the state does not hold is one a later build can
    /// silently change.
    ///
    /// The slot is identified by its icon rather than by a position index, because that is what p.16
    /// matches on: the icon on the bottom of the card "matches the icon on one of four slots on the top
    /// of your hero dashboard". Which four icons a given dashboard prints is hero content that milestone
    /// 8.1 supplies with the rest of a verified hero; nothing here invents it, and nothing here needs
    /// it, because the bonus reads the slot's icon and not the dashboard's layout.
    /// </summary>
    public sealed class HeroItem : IEquatable<HeroItem>
    {
        public HeroItem(
            HeroId ownerId,
            CardInstanceId sourceCardInstanceId,
            PrintedValueChannel slot,
            int order)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("An item requires the hero who owns it.", nameof(ownerId));
            }

            if (!sourceCardInstanceId.IsDefined)
            {
                throw new ArgumentException(
                    "An item requires the card instance that became it.", nameof(sourceCardInstanceId));
            }

            if (slot == PrintedValueChannel.None || !Enum.IsDefined(typeof(PrintedValueChannel), slot))
            {
                throw new ArgumentException(
                    "An item occupies a slot named by one of p.16's six icons.", nameof(slot));
            }

            if (order < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(order), order, "An item's order within its hero's items starts at 1.");
            }

            OwnerId = ownerId;
            SourceCardInstanceId = sourceCardInstanceId;
            Slot = slot;
            Order = order;
        }

        public HeroId OwnerId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        /// <summary>The dashboard slot this item is tucked under, named by that slot's icon.</summary>
        public PrintedValueChannel Slot { get; }

        /// <summary>This item's 1-based position in its owner's sequence of gained items.</summary>
        public int Order { get; }

        public bool Equals(HeroItem other) =>
            other != null &&
            OwnerId.Equals(other.OwnerId) &&
            SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
            Slot == other.Slot &&
            Order == other.Order;

        public override bool Equals(object obj) => Equals(obj as HeroItem);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OwnerId.GetHashCode();
                hashCode = (hashCode * 397) ^ SourceCardInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Slot;
                hashCode = (hashCode * 397) ^ Order;
                return hashCode;
            }
        }

        public override string ToString() =>
            $"{OwnerId} item {Order} ({Slot}) from {SourceCardInstanceId}";
    }

    /// <summary>Why an attempt to place a set-aside card as an item was refused.</summary>
    public enum HeroItemPlacementRejectionReason
    {
        None = 0,

        /// <summary>The card is not the owner's, or the owner is not a participant.</summary>
        NotOwnedByHero = 1,

        /// <summary>
        /// p.16 places "each card set aside during the Upgrade card step". A card in hand, in the deck,
        /// in the discard pile, in the turn area, or already an item is not that card.
        /// </summary>
        NotSetAside = 2,

        /// <summary>
        /// The card's definition is not in the supplied content set, so nothing can say which icon it
        /// prints. A placement that guessed the slot here would be the misplacement p.16 penalizes.
        /// </summary>
        UnresolvedDefinition = 3,

        /// <summary>
        /// The card prints no Item icon. p.16 gives one to every Tier II and Tier III card and to no
        /// other, so this is a Tier I card, a Basic card, or a card stating no progression at all.
        /// </summary>
        NoItemIcon = 4,

        /// <summary>This card instance is already recorded as an item.</summary>
        AlreadyAnItem = 5
    }

    /// <summary>The outcome of one placement attempt: the item that was placed, or why it was refused.</summary>
    public readonly struct HeroItemPlacementResult
    {
        private HeroItemPlacementResult(HeroItem item, HeroItemPlacementRejectionReason rejectionReason)
        {
            Item = item;
            RejectionReason = rejectionReason;
        }

        public HeroItem Item { get; }

        public HeroItemPlacementRejectionReason RejectionReason { get; }

        public bool IsPlaced => RejectionReason == HeroItemPlacementRejectionReason.None;

        public static HeroItemPlacementResult Placed(HeroItem item) =>
            new HeroItemPlacementResult(
                item ?? throw new ArgumentNullException(nameof(item)),
                HeroItemPlacementRejectionReason.None);

        public static HeroItemPlacementResult Rejected(HeroItemPlacementRejectionReason reason) =>
            reason == HeroItemPlacementRejectionReason.None
                ? throw new ArgumentException("A refusal names its reason.", nameof(reason))
                : new HeroItemPlacementResult(null, reason);
    }

    /// <summary>
    /// Every hero's items, as authoritative match state.
    ///
    /// Milestone 7.2 adds the state, the one transition that creates an item, and the bonus it supplies;
    /// it deliberately adds no way for a match to reach that transition on its own. p.16 gains an item
    /// during p.15's Upgrade a Card step, and the purchase that runs the step - its mandatory ordering,
    /// its coin cost, and the two candidates it makes a hero choose between - is milestone 7.5's, which
    /// this milestone's non-goals exclude. So in a live match every hero has no items and keeps none,
    /// and this type is what 7.5 will move rather than introduce, exactly as
    /// <see cref="HeroProgressionState"/> is for Level.
    ///
    /// It is persisted now rather than later because Phase 7's durability rule requires the milestone
    /// that adds authoritative state to close its persistence boundary in the same milestone and says a
    /// later integration gate is not substitute evidence. What that buys is concrete: <see
    /// cref="Restore"/> refuses a payload whose items name a hero the match does not have, name one card
    /// twice, or number a hero's items as anything other than 1..n, so an item cannot come back owned by
    /// nobody, duplicated, or in an order the payload never stated.
    ///
    /// Multiple items may occupy one slot - p.16 says so outright ("Multiple items can be placed under
    /// the same item slot") - so nothing here treats a slot as a key. The uniqueness that does hold is
    /// the card's: one card instance becomes at most one item.
    /// </summary>
    public sealed class HeroItemState
    {
        private readonly Dictionary<HeroId, List<HeroItem>> itemsByHero;

        public HeroItemState(MatchParticipants participants)
            : this(participants, null)
        {
        }

        private HeroItemState(MatchParticipants participants, IEnumerable<HeroItem> restoredItems)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            itemsByHero = participants.Heroes.ToDictionary(hero => hero.Id, _ => new List<HeroItem>());

            if (restoredItems == null)
            {
                return;
            }

            var restored = restoredItems.ToArray();
            if (restored.Any(item => item == null))
            {
                throw new ArgumentException("Restored items must not contain null entries.", nameof(restoredItems));
            }

            if (restored.Any(item => !itemsByHero.ContainsKey(item.OwnerId)))
            {
                throw new ArgumentException(
                    "Every restored item belongs to a participant hero.", nameof(restoredItems));
            }

            if (restored.Select(item => item.SourceCardInstanceId).Distinct().Count() != restored.Length)
            {
                throw new ArgumentException(
                    "One card instance becomes at most one item.", nameof(restoredItems));
            }

            foreach (var group in restored.GroupBy(item => item.OwnerId))
            {
                var ordered = group.OrderBy(item => item.Order).ToArray();
                if (!ordered.Select(item => item.Order).SequenceEqual(Enumerable.Range(1, ordered.Length)))
                {
                    throw new ArgumentException(
                        $"Hero '{group.Key}' restores items numbered other than 1..{ordered.Length}.",
                        nameof(restoredItems));
                }

                itemsByHero[group.Key].AddRange(ordered);
            }
        }

        /// <summary>
        /// Every item in the match, ordered by hero id and then by that hero's own order, so the sequence
        /// is a fact rather than an accident of the order placements happened in.
        /// </summary>
        public IReadOnlyList<HeroItem> Items => Array.AsReadOnly(itemsByHero
            .OrderBy(entry => entry.Key.Value, StringComparer.Ordinal)
            .SelectMany(entry => entry.Value.OrderBy(item => item.Order))
            .ToArray());

        /// <summary>One hero's items in gain order. Empty is an ordinary answer and the usual one.</summary>
        public IReadOnlyList<HeroItem> GetItems(HeroId heroId)
        {
            if (!heroId.IsDefined || !itemsByHero.TryGetValue(heroId, out var items))
            {
                throw new KeyNotFoundException($"Unknown hero ID '{heroId}'.");
            }

            return Array.AsReadOnly(items.OrderBy(item => item.Order).ToArray());
        }

        /// <summary>
        /// p.16's Gain an Item, performed on one set-aside card: turn it Faceup under the slot matching
        /// its printed Item icon and record the item.
        ///
        /// The slot is derived from the card's own icon at this one moment and then stored, which is how
        /// the page's "appropriate item slot" is honoured without letting a caller name a slot the card
        /// does not match. Every refusal is decided before anything moves, so a rejected placement leaves
        /// both the card and this state untouched - the card's transition is the last thing that happens.
        ///
        /// The definition set is required rather than optional because the icon is definition content: a
        /// card whose definition this build cannot resolve is refused rather than placed under a guessed
        /// slot, which is the misplacement p.16 gives no effect.
        /// </summary>
        public HeroItemPlacementResult TryPlaceSetAsideCard(
            CardInstance card,
            IEnumerable<CardDefinition> definitions)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            if (!itemsByHero.TryGetValue(card.OwnerId, out var owned))
            {
                return HeroItemPlacementResult.Rejected(HeroItemPlacementRejectionReason.NotOwnedByHero);
            }

            if (card.State.Location != CardLocation.SetAside)
            {
                return HeroItemPlacementResult.Rejected(HeroItemPlacementRejectionReason.NotSetAside);
            }

            if (itemsByHero.Values.Any(items =>
                    items.Any(item => item.SourceCardInstanceId.Equals(card.Id))))
            {
                return HeroItemPlacementResult.Rejected(HeroItemPlacementRejectionReason.AlreadyAnItem);
            }

            var definition = definitions.FirstOrDefault(candidate =>
                candidate != null && candidate.Reference.Equals(card.DefinitionReference));
            if (definition == null)
            {
                return HeroItemPlacementResult.Rejected(
                    HeroItemPlacementRejectionReason.UnresolvedDefinition);
            }

            if (!definition.Progression.HasItemIcon)
            {
                return HeroItemPlacementResult.Rejected(HeroItemPlacementRejectionReason.NoItemIcon);
            }

            if (!CardLifecycleAuthority.TryPlaceAsItemFromSetAside(card))
            {
                return HeroItemPlacementResult.Rejected(HeroItemPlacementRejectionReason.NotSetAside);
            }

            var item = new HeroItem(
                card.OwnerId,
                card.Id,
                definition.Progression.ItemIcon,
                owned.Count + 1);
            owned.Add(item);
            return HeroItemPlacementResult.Placed(item);
        }

        public HeroItemRestoreState CaptureRestoreState() => new HeroItemRestoreState(Items);

        public static HeroItemState Restore(MatchParticipants participants, HeroItemRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            return new HeroItemState(participants, state.Items);
        }
    }

    /// <summary>The persisted form of <see cref="HeroItemState"/>: the items themselves, and nothing else.</summary>
    public sealed class HeroItemRestoreState
    {
        public HeroItemRestoreState(IEnumerable<HeroItem> items)
        {
            Items = Array.AsReadOnly(
                (items ?? throw new ArgumentNullException(nameof(items))).ToArray());
        }

        public IReadOnlyList<HeroItem> Items { get; }

        public void Deconstruct(out IReadOnlyList<HeroItem> items)
        {
            items = Items;
        }
    }
}
