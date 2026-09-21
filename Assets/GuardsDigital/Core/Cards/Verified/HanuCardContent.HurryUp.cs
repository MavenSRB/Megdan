using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's Hurry up! - one of his nineteen printed faces, composed at milestone
    /// 8.71a because it is the face whose clause this block owns. The other eighteen belong to the
    /// blocks that own their clauses, and 8.71g states the whole set under one real content revision.
    ///
    /// <para><b>Where these numbers come from.</b> <c>Docs/AI/Rules/HERO_HANU.md</c>'s printed
    /// inventory, read off the extracted front <c>hanu2__7635511</c> at milestone 8.71: Silver band,
    /// Basic, Initiative <b>12</b>, Defense <b>1</b>, <b>no Movement icon at all</b>, primary BASIC
    /// SKILL - RANGED with a Range <b>4</b> icon, no item. HA-06 is the rule the absent Movement
    /// follows - "A missing icon is absent, never zero" - so the definition below simply does not
    /// declare a Movement action, exactly as Min's Smoke Bomb and Rowenna's Throw the Gauntlet do
    /// not.</para>
    ///
    /// <para><b>The Range icon sits on a SKILL, which is content rather than an oddity.</b> The
    /// printed band reads "Basic Skill - Ranged": p.12 makes the card text the action, and the Range
    /// icon is read by that text - "a hero in range" - rather than by any targeting p.13 performs.
    /// Bain's Get over here! is the shipped precedent for exactly that band with exactly that
    /// icon.</para>
    ///
    /// <para><b>Nothing here is registered and nothing can deal it</b>, which is milestone 7.9's
    /// gate - see <see cref="HanuComponentContent"/>'s own remarks for why that is the point rather
    /// than an oversight. The revision is the block's placeholder
    /// <see cref="HurryUpPrepRevision"/>, which 8.71g replaces with the one real string his whole set
    /// is stated under.</para>
    /// </summary>
    public static partial class HanuCardContent
    {
        /// <summary>
        /// His Silver basic: "Set the printed [Initiative] Initiative value of an unresolved card of a
        /// hero in range to 11, until it is resolved or otherwise changes state."
        /// </summary>
        public static CardDefinitionId HurryUp { get; } = new CardDefinitionId("hanu-hurry-up");

        /// <summary>
        /// The printed text, transcribed. The bracketed glyph is the printed [Initiative] icon,
        /// written the way this project writes every icon in a transcription, and the parenthesised
        /// reminder is kept because the card prints it.
        /// </summary>
        public const string HurryUpText =
            "Set the printed [Initiative] Initiative value of an unresolved card of a hero in range " +
            "to 11, until it is resolved or otherwise changes state. ( This may change the " +
            "initiative order and tie breakers ).";

        /// <summary>
        /// This block's placeholder revision, and deliberately not a valid content revision: the
        /// wave-one id-collision rule (8.35z11-8.35z14) is that a placeholder must not be a string a
        /// real definition could ever carry. 8.71g mints the real one.
        /// </summary>
        private const string HurryUpPrepRevision = "hanu-8-71a-prep";

        private static readonly Lazy<CardDefinition> LazyHurryUp = new Lazy<CardDefinition>(
            () => new CardDefinition(
                HurryUp,
                HurryUpPrepRevision,
                "Hurry up!",
                new InitiativeValue(12),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new List<ActionDefinition>
                {
                    new ActionDefinition(
                        PrimaryBasicSkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Range, 4) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        HanuEffectContent.HurryUp(HurryUp, HurryUpPrepRevision)),

                    // p.14's Defense action: the ribbon icon, suppressed card text, and the discard
                    // every reaction performs. His Movement ribbon is ABSENT on this face (HA-06), so
                    // there is no Movement action here to declare with a zero.
                    new ActionDefinition(
                        SecondaryDefense,
                        ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 1) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                },
                null,
                CardSourceContent.Transcribed(HurryUpText)));

        /// <summary>The one face this block composes.</summary>
        public static IEnumerable<CardDefinition> HurryUpDefinitions()
        {
            yield return LazyHurryUp.Value;
        }

        // MERGE NOTE: this partial's MERGE BLOCK was dropped at the squash-merge; the surviving
        // aggregate and the shared action identities live in HanuCardContent.CoMoves.cs.
    }
}
