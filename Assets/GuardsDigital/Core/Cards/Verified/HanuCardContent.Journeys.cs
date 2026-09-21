using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's three Blue journeys - Unexpected Journey, There and Back Again and Safe
    /// Travels - whose clause milestone 8.71d owns. The other sixteen faces belong to the blocks that
    /// own their clauses, and milestone 8.71g states the whole set under one real content revision.
    ///
    /// <para><b>Where these numbers come from.</b> <c>Docs/AI/Rules/HERO_HANU.md</c>'s printed
    /// inventory, read off the extracted fronts at milestone 8.71: Unexpected Journey
    /// (<c>hanu8__7635528</c>) Blue Tier I, Initiative <b>10</b>, Defense <b>4</b>, Movement <b>3</b>,
    /// primary SKILL with Radius <b>2</b>, no item; There and Back Again (<c>hanu9__7635513</c>) Blue
    /// Tier II, 11/4/3, Radius <b>3</b>, <b>Attack</b> item; Safe Travels (<c>hanu11__7635522</c>)
    /// Blue Tier III, 11/5/3, Radius <b>3</b>, <b>Attack</b> item.</para>
    ///
    /// <para><b>Both Blue forks are stated here since 8.71g.</b> p.15 forks Blue twice -
    /// Unexpected Journey climbs to There and Back Again or This Way!, and either of those to Safe
    /// Travels or That Way! - and This Way! and That Way! are milestone 8.71e's faces, being written
    /// on another branch as this one is. <see cref="CardProgressionProfile.NonBasicCard"/> takes the
    /// candidate list as given and requires no particular count, so naming an id another branch owns
    /// would be naming something that does not compile here. 8.71g completes both pairs, exactly as
    /// Brynn's 8.69f folded four such halves together.</para>
    ///
    /// <para><b>The revision is real since 8.71g.</b> This block carried the placeholder
    /// <c>hanu-8-71d-prep</c> - deliberately not the <c>hanu-8-71g-rev-1</c> spelling a real revision
    /// takes - and 8.71g deleted it. The three faces and the three end-of-turn bodies in
    /// <see cref="HanuEffectContent.JourneyTriggeredEffects"/> carry
    /// <see cref="ContentRevision"/>, which they must share: a triggered body and the card that arms it
    /// have to name one revision or the catalog cannot resolve the reference.</para>
    ///
    /// <para><b>Nothing here is registered and nothing can deal it</b>, which is milestone 7.9's gate:
    /// <see cref="MatchContentCatalogs"/> names no Hanu content until 8.71h.</para>
    /// </summary>
    public static partial class HanuCardContent
    {
        /// <summary>His Blue Tier I: Radius 2, no item.</summary>
        public static CardDefinitionId UnexpectedJourney { get; } =
            new CardDefinitionId("hanu-unexpected-journey");

        /// <summary>His Blue Tier II: the same clause at Radius 3, with an Attack item.</summary>
        public static CardDefinitionId ThereAndBackAgain { get; } =
            new CardDefinitionId("hanu-there-and-back-again");

        /// <summary>His Blue Tier III: the same clause, then "Move up to 1 space."</summary>
        public static CardDefinitionId SafeTravels { get; } =
            new CardDefinitionId("hanu-safe-travels");

        // -----------------------------------------------------------------------------------------
        // The printed text, transcribed.
        // -----------------------------------------------------------------------------------------

        /// <summary>The clause all three print, word for word.</summary>
        public const string JourneySentence =
            "Swap with an enemy hero in radius. This turn: That hero is immune. End of turn: Swap " +
            "with that hero regardless of radius and immunity.";

        public const string UnexpectedJourneyText = JourneySentence;

        public const string ThereAndBackAgainText = JourneySentence;

        public const string SafeTravelsText = JourneySentence + " Move up to 1 space.";

        /// <summary>The three faces this block composes, in his shard's inventory order.</summary>
        public static IEnumerable<CardDefinition> JourneyDefinitions() => LazyJourneys.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyJourneys =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                // MILESTONE 8.71g COMPLETED BOTH OF THESE ROWS. 8.71d could name only the successors
                // it composed itself, because This Way! and That Way! are 8.71e's faces and were being
                // written on another branch; p.15 forks Blue twice and each row reaches BOTH.
                BuildJourney(
                    UnexpectedJourney, "Unexpected Journey", CardTier.I, 10, 4, 3, 2,
                    PrintedValueChannel.None, UnexpectedJourneyText,
                    HanuActiveEffectContent.UnexpectedJourneyEffectV1,
                    new[] { ThereAndBackAgain, ThisWay }),
                BuildJourney(
                    ThereAndBackAgain, "There and Back Again", CardTier.II, 11, 4, 3, 3,
                    PrintedValueChannel.Attack, ThereAndBackAgainText,
                    HanuActiveEffectContent.ThereAndBackAgainEffectV1,
                    new[] { SafeTravels, ThatWay }),
                BuildJourney(
                    SafeTravels, "Safe Travels", CardTier.III, 11, 5, 3, 3,
                    PrintedValueChannel.Attack, SafeTravelsText,
                    HanuActiveEffectContent.SafeTravelsEffectV1,
                    Array.Empty<CardDefinitionId>())
            }));

        /// <summary>
        /// One face of the three. They differ in Initiative, Defense, the printed Radius, the item
        /// icon, the trailing move and which Active effect they create - and in nothing else at all.
        ///
        /// <para>The primary is p.9's Skill band, so its card text IS the action (p.12) and the Radius
        /// icon is read by that text rather than by any targeting the page performs. The Movement
        /// ribbon's own card text is suppressed by p.11 and the Defense ribbon is p.14's discard from
        /// hand - his shard's HA-06 records that every dealt face but the Ultimate prints Defense and
        /// every face but Hurry up! and the Ultimate prints Movement, and these three print
        /// both.</para>
        /// </summary>
        private static CardDefinition BuildJourney(
            CardDefinitionId id,
            string displayName,
            CardTier tier,
            int initiative,
            int defense,
            int movement,
            int radius,
            PrintedValueChannel itemIcon,
            string printedText,
            EffectCapabilityReference journeyEffect,
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
                    HanuEffectContent.Journey(id, ContentRevision, journeyEffect),
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

        // MERGE NOTE: this partial's MERGE BLOCK was dropped at the squash-merge; the surviving
        // aggregate and the shared action identities live in HanuCardContent.CoMoves.cs.
    }
}
