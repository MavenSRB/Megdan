using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's three Blue tracking swaps - Tread Lightly, Cover Tracks and Hide Traces -
    /// whose clause milestone 8.69d owns. The other sixteen faces belong to the blocks that own their
    /// clauses, and milestone 8.69f states the whole set under one real content revision in
    /// <c>BrynnCardContent.cs</c>.
    ///
    /// <para><b>Where these numbers come from.</b> <c>Docs/AI/Rules/HERO_BRYNN.md</c>'s printed
    /// inventory, read off the extracted fronts at milestone 8.69: Tread Lightly
    /// (<c>brynn8__7690726</c>) Blue Tier I, Initiative <b>10</b>, Defense <b>5</b>, Movement <b>3</b>,
    /// primary SKILL with Radius <b>2</b>, no item; Cover Tracks (<c>brynn9__7690727</c>) Blue Tier II,
    /// 10/5/3, Radius <b>3</b>, <b>Defense</b> item; Hide Traces (<c>brynn11__7690728</c>) Blue Tier
    /// III, 11/6/3, Radius <b>3</b>, <b>Range</b> item.</para>
    ///
    /// <para><b>Hide Traces' Range item is beside a Radius icon, and that is content rather than an
    /// oddity.</b> p.16's items raise the channel they name; the face prints no Range at all, so the
    /// item it hands out on the unchosen branch is a Range item his OTHER faces spend. It is also the
    /// measurement that settles which contract the swap composes on - see
    /// <see cref="BrynnComponentContent.TrackingSwapSubject"/> and BR-Q17d.</para>
    ///
    /// <para><b>Both Blue progression pairs are stated whole from milestone 8.69f on.</b> p.15 forks
    /// Blue twice - Tread Lightly climbs to Cover Tracks or Mountain Guide, and either of those to Hide
    /// Traces or Expedition Leader - and Mountain Guide and Expedition Leader are 8.69c's faces. While
    /// that block was being written on another branch each face here named only the half this one
    /// composed; the set exists now, so every row names both of its successors.</para>
    ///
    /// <para><b>Nothing here is registered and nothing can deal it</b>, which is milestone 7.9's gate:
    /// the revision is <see cref="ContentRevision"/>, the one real string milestone 8.69f states his
    /// whole set under and which replaced this block's placeholder.</para>
    /// </summary>
    public static partial class BrynnCardContent
    {
        // The conventional action identities his whole set shares - p.9's Skill band among them - are
        // declared ONCE, in BrynnCardContent.cs: milestone 8.69f's fold over the five wave blocks,
        // each of which had declared the ones its own faces needed because a partial class cannot
        // declare one member twice.

        /// <summary>His Blue Tier I: the swap, and nothing after it.</summary>
        public static CardDefinitionId TreadLightly { get; } =
            new CardDefinitionId("brynn-tread-lightly");

        /// <summary>His Blue Tier II: the swap, then "You may move 1 space".</summary>
        public static CardDefinitionId CoverTracks { get; } =
            new CardDefinitionId("brynn-cover-tracks");

        /// <summary>His Blue Tier III: the swap, then "Move up to 2 spaces".</summary>
        public static CardDefinitionId HideTraces { get; } =
            new CardDefinitionId("brynn-hide-traces");

        // -----------------------------------------------------------------------------------------
        // The printed text, transcribed.
        // -----------------------------------------------------------------------------------------

        /// <summary>The sentence all three print, word for word.</summary>
        public const string TrackingSwapSentence =
            "Swap either with a unit adjacent to you, or with an enemy hero in radius who is " +
            "adjacent to 3 or more obstacles.";

        public const string TreadLightlyText = TrackingSwapSentence;

        public const string CoverTracksText = TrackingSwapSentence + " You may move 1 space.";

        public const string HideTracesText = TrackingSwapSentence + " Move up to 2 spaces.";

        /// <summary>The three faces this block composes, in his shard's inventory order.</summary>
        public static IReadOnlyList<CardDefinition> SwapDefinitions => LazySwap.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazySwap =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                BuildTrackingSwap(
                    TreadLightly, "Tread Lightly", CardTier.I, 10, 5, 3, 2,
                    PrintedValueChannel.None, TreadLightlyText, 0, false,
                    new[] { CoverTracks, MountainGuide }),
                BuildTrackingSwap(
                    CoverTracks, "Cover Tracks", CardTier.II, 10, 5, 3, 3,
                    PrintedValueChannel.Defense, CoverTracksText, 1, false,
                    new[] { HideTraces, ExpeditionLeader }),
                BuildTrackingSwap(
                    HideTraces, "Hide Traces", CardTier.III, 11, 6, 3, 3,
                    PrintedValueChannel.Range, HideTracesText, 2, true,
                    Array.Empty<CardDefinitionId>())
            }));

        /// <summary>
        /// One face of the three. They differ in Initiative, Defense, the printed Radius, the item icon
        /// and the trailing move - and in nothing else at all, which is what makes one factory the
        /// honest statement of three cards rather than a saving.
        ///
        /// <para>The primary is p.9's Skill band, so its card text IS the action (p.12) and the Radius
        /// icon is read by that text rather than by any targeting the page performs. The Movement
        /// ribbon's own card text is suppressed by p.11 and the Defense ribbon is p.14's discard from
        /// hand, exactly as 8.69a's four faces declare them.</para>
        /// </summary>
        private static CardDefinition BuildTrackingSwap(
            CardDefinitionId id,
            string displayName,
            CardTier tier,
            int initiative,
            int defense,
            int movement,
            int radius,
            PrintedValueChannel itemIcon,
            string printedText,
            int moveDistance,
            bool moveIsUpTo,
            IReadOnlyList<CardDefinitionId> nextTierCandidates) => new CardDefinition(
            id,
            ContentRevision,
            displayName,
            new InitiativeValue(initiative),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.NonBasicCard(
                CardColor.Blue, tier, itemIcon, nextTierCandidates),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimarySkill,
                    ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, radius) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None,
                    default,
                    BrynnEffectContent.TrackingSwap(
                        id, ContentRevision, moveDistance, moveIsUpTo),
                    PrintedTextComposition.None),

                new ActionDefinition(
                    SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, movement) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),

                new ActionDefinition(
                    SecondaryDefense,
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, defense) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            },
            null,
            CardSourceContent.Transcribed(printedText));
    }
}
