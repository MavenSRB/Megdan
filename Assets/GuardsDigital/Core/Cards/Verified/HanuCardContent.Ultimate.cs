using System;
using System.Collections.Generic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's Purple Tier IV - The Ultimate Trick - whose clause milestone 8.71b
    /// owns. The other eighteen faces belong to the blocks that own their clauses, and 8.71g states
    /// the whole set under one real content revision.
    ///
    /// <para><b>Where these values come from.</b> <c>Docs/AI/Rules/HERO_HANU.md</c>'s printed
    /// inventory, read off the extracted front at milestone 8.71: The Ultimate Trick
    /// (<c>hanu18__7635514</c>) prints NO Initiative, NO Defense, NO Movement, no Range and no
    /// Radius - HA-06's "a missing icon is absent, never zero" at its sharpest, since this is the
    /// one face of his that prints no value at all.</para>
    ///
    /// <para><b>It is on no ladder and states no progression row</b>, because p.16 UNLOCKS an
    /// Ultimate rather than upgrading into one - the same statement Brynn's Over the Top and every
    /// other dealt Ultimate makes.</para>
    ///
    /// <para><b>Nothing here is registered and nothing can deal it</b>, which is milestone 7.9's
    /// gate: <see cref="MatchContentCatalogs"/> names no Hanu content until 8.71h.</para>
    /// </summary>
    public static partial class HanuCardContent
    {
        // MILESTONE 8.71g DELETED this block's placeholder revision. It read "hanu-8-71b-prep" and was
        // deliberately not a revision any dealt content could carry; the face carries the set's own
        // `ContentRevision` now.

        /// <summary>His Purple Tier IV: the hero he hurried acts, and he decides how.</summary>
        public static CardDefinitionId TheUltimateTrick { get; } =
            new CardDefinitionId("hanu-the-ultimate-trick");

        /// <summary>
        /// The Ultimate Trick, word for word - including the card's own spelling of the Silver's
        /// title, which the shard records as the same card.
        /// </summary>
        public const string TheUltimateTrickText =
            "You choose the next action, and how it is performed for a hero you target with the " +
            "\"Hurry Up!\".";

        /// <summary>The one face this block composes.</summary>
        public static IEnumerable<CardDefinition> UltimateDefinitions() => LazyUltimate.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyUltimate =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                BuildTheUltimateTrick()
            }));

        /// <summary>
        /// The Ultimate Trick. p.16's permanent ability: it prints no Initiative, no icons and no
        /// action at all, so it is never played, never discarded to defend, and the whole of its
        /// content is the standing declaration below.
        ///
        /// <para>The declaration carries no <c>RuleModification</c>, and that is a statement rather
        /// than an omission - <c>UltimateRuleDefinition.PermanentDeclaration</c>'s own remarks give
        /// the reason. This ability changes no value, grants no permission and replaces no type: it
        /// changes WHO CHOOSES, which none of the five modification families can say, and which
        /// <c>DelegatedActionQuery</c> reads off the rule kind while the card sits beside the
        /// dashboard and its hero is on the board.</para>
        /// </summary>
        private static CardDefinition BuildTheUltimateTrick() => new CardDefinition(
            TheUltimateTrick,
            ContentRevision,
            "The Ultimate Trick",
            default,
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.UltimateCard(),
            Array.Empty<ActionDefinition>(),
            UltimateRuleDefinition.PermanentDeclaration(
                HanuEffectContent.TheUltimateTrickRuleId,
                HanuEffectContent.TheUltimateTrickRuleKind),
            CardSourceContent.Transcribed(TheUltimateTrickText));

        // ===== MERGE BLOCK =====
        // ABSENT, deliberately, and this is the one wave partial of 8.71 that must not carry it.
        // The common brief has every block declare the aggregate and the six shared action
        // identities so that each compiles alone; 8.71b was cut from main AFTER 8.71a and 8.71e
        // merged, so the surviving copy is already in this partial class - it is in
        // HanuCardContent.CoMoves.cs, where the coordinator kept it - and a second declaration of
        // PrimaryBasicAttack, AllDefinitions or DealtDefinitions in the same partial class is a
        // duplicate-member compile error rather than a merge conflict somebody resolves.
        //
        // This block's face is therefore concatenated into that surviving ComposedFaces() instead,
        // which is exactly what the coordinator does at every other merge. Nothing else in
        // CoMoves.cs is touched.
        // ===== END MERGE BLOCK =====
    }
}
