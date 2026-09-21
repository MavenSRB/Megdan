using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.69c. Brynn the Seeker's three faces that move somebody else: Decoy, his Silver
    /// basic, and the two mountain guides that move a friendly unit.
    ///
    /// <para><b>Where these numbers come from.</b> <c>Docs/AI/Rules/HERO_BRYNN.md</c>'s printed
    /// inventory, read off the extracted fronts at milestone 8.69: Decoy (<c>brynn2__7711616</c>)
    /// Silver, Basic, Initiative <b>1</b>, Defense <b>2</b>, <b>no Movement icon</b>, primary BASIC
    /// SKILL with Radius <b>4</b>, no item; Mountain Guide (<c>brynn10__7724914</c>) Blue Tier II,
    /// 10/5/3, Skill with Radius <b>4</b>, Attack item; Expedition Leader (<c>brynn12__7724915</c>)
    /// Blue Tier III, 11/6/3, Skill with Radius <b>4</b>, Attack item. BR-06 is the rule the absent
    /// icon follows - "A missing icon is absent, never zero" - so Decoy declares no Movement action at
    /// all rather than one printing zero.</para>
    ///
    /// <para><b>The two guides print one sentence at two distances.</b> "up to 2 spaces" twice on
    /// Mountain Guide and "up to 3 spaces" twice on Expedition Leader is the whole of what separates
    /// them; the Radius is the same 4 on both, which is why it is read off the card rather than
    /// written into the body.</para>
    ///
    /// <para><b>The Blue ladder is stated whole from milestone 8.69f on.</b> p.15 forks it - Tread
    /// Lightly climbs to Cover Tracks or Mountain Guide, and either of those to Hide Traces or
    /// Expedition Leader - and Tread Lightly, Cover Tracks and Hide Traces are 8.69d's faces. While
    /// that block was being written on another branch this one named only the half it composed, on the
    /// rule Min's series kept four times over; the set exists now, so Mountain Guide names both Tier
    /// III faces and Expedition Leader, a Tier III itself, names none.</para>
    ///
    /// <para><b>Nothing here is registered and nothing can deal it.</b> The revision is
    /// <see cref="ContentRevision"/>, the one real string milestone 8.69f states his whole set under
    /// and which replaced this block's placeholder.</para>
    /// </summary>
    public static partial class BrynnCardContent
    {
        // -----------------------------------------------------------------------------------------
        // Identities.
        //
        // The conventional action identities his whole set shares are declared ONCE, in
        // BrynnCardContent.cs - milestone 8.69f's fold over the five wave blocks, each of which had
        // declared the ones its own faces needed because a partial class cannot declare one member
        // twice.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// His Silver basic: "Choose up to two times, on different targets - • Move an enemy minion in
        /// radius 1 space. • Move an enemy hero in radius who is adjacent to 3 or more obstacles 1
        /// space."
        /// </summary>
        public static CardDefinitionId Decoy { get; } = new CardDefinitionId("brynn-decoy");

        /// <summary>His Blue Tier II: the guide at two spaces.</summary>
        public static CardDefinitionId MountainGuide { get; } =
            new CardDefinitionId("brynn-mountain-guide");

        /// <summary>His Blue Tier III: the same sentence at three spaces.</summary>
        public static CardDefinitionId ExpeditionLeader { get; } =
            new CardDefinitionId("brynn-expedition-leader");

        // -----------------------------------------------------------------------------------------
        // The printed text, transcribed.
        // -----------------------------------------------------------------------------------------

        public const string DecoyText =
            "Choose up to two times, on different targets - • Move an enemy minion in radius 1 " +
            "space. • Move an enemy hero in radius who is adjacent to 3 or more obstacles 1 space.";

        public const string MountainGuideText =
            "You may move a friendly unit adjacent to you up to 2 spaces. If an enemy hero in radius " +
            "is adjacent to 3 or more obstacles, move a different friendly unit in radius up to 2 " +
            "spaces.";

        public const string ExpeditionLeaderText =
            "You may move a friendly unit adjacent to you up to 3 spaces. If an enemy hero in radius " +
            "is adjacent to 3 or more obstacles, move a different friendly unit in radius up to 3 " +
            "spaces.";

        /// <summary>The three faces this block composes, in his shard's inventory order.</summary>
        public static IReadOnlyList<CardDefinition> GuidesDefinitions => LazyGuides.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyGuides =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                BuildDecoy(),
                BuildGuide(
                    MountainGuide, "Mountain Guide", CardTier.II, 10, 5, 2, MountainGuideText,
                    new[] { HideTraces, ExpeditionLeader }),
                BuildGuide(
                    ExpeditionLeader, "Expedition Leader", CardTier.III, 11, 6, 3,
                    ExpeditionLeaderText, Array.Empty<CardDefinitionId>())
            }));

        /// <summary>
        /// Decoy. p.9's Silver band with a Basic Skill primary, whose Radius 4 icon is what both of
        /// its bullets mean by "in radius" - so his Radius item widens the face that prints one.
        ///
        /// <para>It prints NO Movement icon (BR-06), so there is no Movement action here at all; its
        /// Defense ribbon is p.14's reaction, spent by discarding the card from hand.</para>
        /// </summary>
        private static CardDefinition BuildDecoy() => new CardDefinition(
            Decoy,
            ContentRevision,
            "Decoy",
            new InitiativeValue(1),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Silver),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimaryBasicSkill,
                    ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 4) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None,
                    default,
                    BrynnEffectContent.Decoy(Decoy, ContentRevision)),

                new ActionDefinition(
                    SecondaryDefense,
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 2) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            },
            null,
            CardSourceContent.Transcribed(DecoyText));

        /// <summary>
        /// Mountain Guide and Expedition Leader, which differ in Tier, Initiative, Defense and the one
        /// number their sentence prints twice - and in nothing else. Both carry the Attack item their
        /// unchosen twin becomes under p.15.
        /// </summary>
        private static CardDefinition BuildGuide(
            CardDefinitionId id,
            string displayName,
            CardTier tier,
            int initiative,
            int defense,
            int spaces,
            string printedText,
            IReadOnlyList<CardDefinitionId> nextTierCandidates) => new CardDefinition(
            id,
            ContentRevision,
            displayName,
            new InitiativeValue(initiative),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.NonBasicCard(
                CardColor.Blue, tier, PrintedValueChannel.Attack, nextTierCandidates),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimarySkill,
                    ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 4) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None,
                    default,
                    BrynnEffectContent.Guide(
                        id, ContentRevision, spaces)),

                new ActionDefinition(
                    SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 3) },
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
