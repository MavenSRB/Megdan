using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// What one card definition states about colour, Tier, Basic classification, and which cards it
    /// upgrades into. Immutable definition content, like the printed values beside it.
    ///
    /// <see cref="CardDefinition"/> takes one as a required argument, and that is what makes acceptance
    /// check two's "explicitly represents its applicable typed <c>CardColor</c> or verified absence"
    /// true by construction rather than by convention: there is no overload that omits it, so a
    /// definition that states nothing has to say so, through <see cref="NotStated"/>.
    ///
    /// The three factories are the three shapes the rulebook admits, and each refuses the fields that
    /// do not belong to it:
    ///
    /// <list type="bullet">
    /// <item><see cref="BasicCard"/> - p.15's Gold and Silver pair. They "are never upgraded, have no
    /// Tier, and are not considered when checking your lowest card Tier", so this factory refuses a
    /// Tier and refuses candidates outright rather than leaving either merely empty.</item>
    /// <item><see cref="NonBasicCard"/> - a Tier I, II, or III card. It requires a Tier and p.16's Item
    /// icon or that page's explicit absence of one, and its
    /// candidates are the explicit next-Tier cards p.15's Upgrade a Card step looks for: "two Action
    /// cards in your deck that have the same color as the chosen card but are one Tier higher". Empty
    /// is legal and means this card has no next Tier in the content set - a Tier III card, since Tier IV
    /// is the Ultimate, which is unlocked rather than upgraded into.</item>
    /// <item><see cref="UltimateCard"/> - milestone 7.3's, and p.16's "special Tier IV card". It states
    /// a Tier and no colour, prints no Item icon, and names no candidates, and each of those three is
    /// the page's own: the Tier is stated outright, the printed swatch is none of p.15's five hand
    /// colours and no rule reads it, the Item icon belongs to "every Tier II and Tier III Action card"
    /// and to no other, and the level that unlocks it happens "instead of upgrading a card and gaining
    /// an item", so there is nothing above it to name.</item>
    /// <item><see cref="NotStated"/> - the definition carries no progression content. Every query in
    /// this milestone excludes such a definition by classification rather than by a colour comparison,
    /// so a fixture card that models no progression can never satisfy a colour predicate by accident.
    /// </item>
    /// </list>
    ///
    /// Candidates are stable <see cref="CardDefinitionId"/> values rather than derived from colour.
    /// Colour constrains progression and does not determine it: which two cards a Tier I card upgrades
    /// into is content the card states, and <see cref="CardProgressionValidator"/> is what checks the
    /// stated relationship against the colour and Tier rules over a whole definition set. A revision is
    /// deliberately not part of a candidate reference: p.15 sends the player to their own deck, and the
    /// card they find there is whichever revision of it this build supplies.
    /// </summary>
    public sealed class CardProgressionProfile
    {
        private static readonly CardDefinitionId[] NoCandidates = Array.Empty<CardDefinitionId>();

        private CardProgressionProfile(
            CardColor color,
            CardClassification classification,
            CardTier tier,
            PrintedValueChannel itemIcon,
            IReadOnlyList<CardDefinitionId> nextTierCandidateIds)
        {
            Color = color;
            Classification = classification;
            Tier = tier;
            ItemIcon = itemIcon;
            NextTierCandidateIds = nextTierCandidateIds;
        }

        /// <summary>
        /// The explicit statement that a definition carries no colour, no Tier, and no classification.
        ///
        /// It is a statement rather than a default because it cannot be reached by omission: the only
        /// way a definition acquires this profile is by naming it at construction.
        /// </summary>
        public static CardProgressionProfile NotStated { get; } = new CardProgressionProfile(
            CardColor.None, CardClassification.None, CardTier.None, PrintedValueChannel.None,
            Array.AsReadOnly(NoCandidates));

        public CardColor Color { get; }

        public CardClassification Classification { get; }

        public CardTier Tier { get; }

        /// <summary>
        /// The Item icon printed on the bottom of this card, or <see cref="PrintedValueChannel.None"/>
        /// when the card prints none.
        ///
        /// p.16 states which cards carry one, and states it exhaustively: "On the bottom of every Tier
        /// II and Tier III Action card, there is an Item icon." The icon is therefore not an optional
        /// embellishment on a non-Basic card - it is present on exactly the two Tiers an upgrade can set
        /// aside, and absent everywhere else, which is what <see cref="NonBasicCard"/> enforces in both
        /// directions rather than merely permitting.
        ///
        /// The channel vocabulary is shared with <see cref="PrintedValueDefinition"/> deliberately. p.16
        /// closes its Item Bonus box by naming the affected icons - "only the value of the Initiative,
        /// Attack, Defense, Movement, Range, or Radius icons on a card are affected" - which is
        /// <see cref="PrintedValueChannel"/>'s exact membership. A second parallel enum for item icons
        /// would have to be kept in agreement with this one by hand, and the first disagreement would be
        /// an item matching an icon that no printed value can carry.
        /// </summary>
        public PrintedValueChannel ItemIcon { get; }

        /// <summary>Whether this card prints an Item icon at all - p.16's Tier II and Tier III cards.</summary>
        public bool HasItemIcon => ItemIcon != PrintedValueChannel.None;

        public IReadOnlyList<CardDefinitionId> NextTierCandidateIds { get; }

        /// <summary>Whether this definition states progression content at all.</summary>
        public bool StatesProgression => Classification != CardClassification.None;

        public bool IsBasic => Classification == CardClassification.Basic;

        public bool IsNonBasic => Classification == CardClassification.NonBasic;

        /// <summary>
        /// Whether this is p.16's Ultimate. Deliberately not implied by <see cref="Tier"/>: the Tier is
        /// printed content and the classification is what the rules read, and a query that asked
        /// "is this Tier IV" instead would be answering the upgrade and lowest-Tier questions off a
        /// numeral rather than off the page's own exclusion.
        /// </summary>
        public bool IsUltimate => Classification == CardClassification.Ultimate;

        /// <summary>
        /// Whether this definition has a Tier at all. False for a Basic card and for a definition that
        /// states nothing, which are exactly the two kinds p.15 excludes from the lowest-Tier reading.
        /// </summary>
        public bool HasTier => Tier != CardTier.None;

        /// <summary>
        /// p.15's Basic card. The colour is required because the pair is named by colour - "Your Basic
        /// cards (Gold and Silver)" - and only those two are accepted, which is the page's own claim
        /// rather than an inference from the five-colour hand.
        /// </summary>
        public static CardProgressionProfile BasicCard(CardColor color)
        {
            if (color != CardColor.Gold && color != CardColor.Silver)
            {
                throw new ArgumentException(
                    "A Basic card is one of the Gold and Silver pair (rulebook p.15).", nameof(color));
            }

            return new CardProgressionProfile(
                color, CardClassification.Basic, CardTier.None, PrintedValueChannel.None,
                Array.AsReadOnly(NoCandidates));
        }

        /// <summary>
        /// p.16's Ultimate. It takes no arguments because the page leaves nothing to choose: the Tier
        /// is IV, there is no Item icon, there are no next-Tier candidates, and the printed swatch is
        /// none of p.15's five colours. A factory with parameters would invite a caller to state one of
        /// those differently, and every such statement would be an invention.
        ///
        /// What varies between two heroes' Ultimates is the ability itself, and that is not progression
        /// content: <see cref="CardDefinition"/> carries it as an <see cref="UltimateRuleDefinition"/>
        /// and refuses an Ultimate that states none.
        /// </summary>
        public static CardProgressionProfile UltimateCard() => Ultimate;

        private static readonly CardProgressionProfile Ultimate = new CardProgressionProfile(
            CardColor.None, CardClassification.Ultimate, CardTier.IV, PrintedValueChannel.None,
            Array.AsReadOnly(NoCandidates));

        public static CardProgressionProfile NonBasicCard(
            CardColor color,
            CardTier tier,
            PrintedValueChannel itemIcon,
            params CardDefinitionId[] nextTierCandidateIds) =>
            NonBasicCard(color, tier, itemIcon, (IEnumerable<CardDefinitionId>)nextTierCandidateIds);

        /// <param name="itemIcon">
        /// p.16's Item icon, or <see cref="PrintedValueChannel.None"/> for a Tier I card that prints
        /// none. It is a required argument with no overload that omits it, for the reason the whole
        /// profile is a required argument of <see cref="CardDefinition"/>: a Tier II card whose icon was
        /// simply left out would be a card the upgrade step can set aside and then never place.
        /// </param>
        public static CardProgressionProfile NonBasicCard(
            CardColor color,
            CardTier tier,
            PrintedValueChannel itemIcon,
            IEnumerable<CardDefinitionId> nextTierCandidateIds)
        {
            if (color == CardColor.None)
            {
                throw new ArgumentException(
                    "A non-Basic card states a card colour.", nameof(color));
            }

            // Milestone 8.37. CardColor.Purple is p.15's HAND colours widened by one for
            // a naming step that reaches beyond them (RULING 21) - it is never a printed swatch on a
            // card in a hand, so no definition may claim it as its own colour, structurally rather than
            // by convention.
            if (color == CardColor.Purple)
            {
                throw new ArgumentException(
                    "Purple is the Ultimate's naming-only colour and prints on no hand card " +
                    "(rulebook p.15).",
                    nameof(color));
            }

            if (tier == CardTier.None)
            {
                throw new ArgumentException(
                    "A non-Basic card states a Tier; only a Basic card has none (rulebook p.15).",
                    nameof(tier));
            }

            if (!Enum.IsDefined(typeof(CardTier), tier))
            {
                throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown card Tier.");
            }

            // Milestone 7.3. Tier IV belongs to the Ultimate alone. p.16 gives it a classification of
            // its own, so a non-Basic card claiming the numeral would be an ordinary upgradeable card
            // that the lowest-Tier reading and the upgrade choice would both have to consider.
            if (tier == CardTier.IV)
            {
                throw new ArgumentException(
                    "Tier IV is the Ultimate's; state it through UltimateCard (rulebook p.16).",
                    nameof(tier));
            }

            if (!Enum.IsDefined(typeof(PrintedValueChannel), itemIcon))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(itemIcon), itemIcon, "Unknown Item icon channel.");
            }

            // p.16 states the presence exhaustively rather than permissively, so both directions are
            // refused. A Tier II or Tier III card without an icon is one the upgrade step can set aside
            // and then never place under a matching slot; a Tier I card with one claims an icon the page
            // does not print on it. The absence a Tier I card carries is therefore stated at the call
            // site rather than reached by leaving an argument out.
            var printsItemIcon = tier == CardTier.II || tier == CardTier.III;
            if (printsItemIcon && itemIcon == PrintedValueChannel.None)
            {
                throw new ArgumentException(
                    $"A Tier {tier} card prints an Item icon (rulebook p.16).", nameof(itemIcon));
            }

            if (!printsItemIcon && itemIcon != PrintedValueChannel.None)
            {
                throw new ArgumentException(
                    $"Only a Tier II or Tier III card prints an Item icon; Tier {tier} prints none " +
                    "(rulebook p.16).",
                    nameof(itemIcon));
            }

            var candidates = (nextTierCandidateIds ?? throw new ArgumentNullException(nameof(nextTierCandidateIds)))
                .ToArray();
            if (candidates.Any(candidate => !candidate.IsDefined))
            {
                throw new ArgumentException(
                    "Next-Tier candidates must be defined definition IDs.", nameof(nextTierCandidateIds));
            }

            if (candidates.Distinct().Count() != candidates.Length)
            {
                throw new ArgumentException(
                    "Next-Tier candidates must be distinct.", nameof(nextTierCandidateIds));
            }

            return new CardProgressionProfile(
                color, CardClassification.NonBasic, tier, itemIcon, Array.AsReadOnly(candidates));
        }
    }
}
