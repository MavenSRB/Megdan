using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's four faces whose clauses milestone 8.69a owns: Familiar Ground, High Ground,
    /// Elevated Ambush and Over the Top. The other fifteen belong to the blocks that own their clauses,
    /// and milestone 8.69f states the whole set under one real content revision in
    /// <c>BrynnCardContent.cs</c>.
    ///
    /// <para><b>Where these numbers come from.</b> <c>Docs/AI/Rules/HERO_BRYNN.md</c>'s printed
    /// inventory, read off the extracted fronts at milestone 8.69: Familiar Ground
    /// (<c>brynn__7690719</c>) Gold, Basic, Initiative <b>12</b>, Defense <b>2</b>, Movement <b>1</b>,
    /// primary BASIC ATTACK - RANGED with Attack <b>2</b> and Range <b>3</b>, no item; High Ground
    /// (<c>brynn3__7696235</c>) Red Tier I, 9/6/4, Attack <b>4+</b>, no item; Elevated Ambush
    /// (<c>brynn4__7696236</c>) Red Tier II, 9/6/4, Attack <b>5+</b>, Initiative item; Over the Top
    /// (<c>brynn18__7550498</c>) Purple Tier IV, no values at all. BR-06 is the rule the absent icons
    /// follow - "A missing icon is absent, never zero" - so the two Reds declare no Range or Radius and
    /// the Ultimate declares no action.</para>
    ///
    /// <para><b>The printed "+" is the contribution and not a second number.</b> p.14 states the
    /// notation outright ("The '+' sign signifies that the value can be increased by the card text"),
    /// so High Ground prints Attack 4 and Elevated Ambush Attack 5, each with one
    /// <see cref="RosterValueContributionDefinition"/> of +2 on the Attack channel - Tigerclaw's
    /// Backstab shape, and BR-Q6's reading of when it is read.</para>
    ///
    /// <para><b>Both Red progression pairs are stated whole from milestone 8.69f on.</b> p.15 forks
    /// Red twice - High Ground climbs to Elevated Ambush or Split Attack, and either of those to Peak
    /// Precision or Split Throw - and Split Attack, Split Throw and Peak Precision are 8.69b's faces.
    /// While that block was being written on another branch this one named only the half it composed,
    /// because naming an id a sibling branch owns is what Min's series refused four times over; the
    /// set exists now, so each row names both of its successors.</para>
    ///
    /// <para><b>Nothing here is registered and nothing can deal it</b>, which is milestone 7.9's gate
    /// rather than an oversight - see <see cref="BrynnComponentContent"/>'s own remarks. The revision
    /// is <see cref="ContentRevision"/>, the one real string milestone 8.69f states his whole set
    /// under and which replaced this block's placeholder.</para>
    /// </summary>
    public static partial class BrynnCardContent
    {
        // -----------------------------------------------------------------------------------------
        // Identities.
        //
        // The conventional action identities his whole set shares - PrimaryRangedAttack,
        // PrimaryAttack, PrimaryBasicSkill, PrimarySkill, SecondaryMovement and SecondaryDefense - are
        // declared ONCE, in BrynnCardContent.cs, which is the fold milestone 8.69f performed over the
        // five wave blocks that each declared the ones its own faces needed.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// His Gold basic: "Choose one - • Target a unit adjacent to you. • Target a hero in range who
        /// is adjacent to 3 or more obstacles. ( You, other heroes, minions, tokens and terrain are
        /// obstacles )."
        /// </summary>
        public static CardDefinitionId FamiliarGround { get; } =
            new CardDefinitionId("brynn-familiar-ground");

        /// <summary>His Red Tier I: "Target a unit adjacent to you. If you target a hero adjacent to 3
        /// or more obstacles, +2 [Attack] attack."</summary>
        public static CardDefinitionId HighGround { get; } =
            new CardDefinitionId("brynn-high-ground");

        /// <summary>His Red Tier II, printing High Ground's text over a higher Attack.</summary>
        public static CardDefinitionId ElevatedAmbush { get; } =
            new CardDefinitionId("brynn-elevated-ambush");

        /// <summary>His Purple Tier IV: "While you are performing actions all enemy heroes in play
        /// count as adjacent to 3 or more obstacles."</summary>
        public static CardDefinitionId OverTheTop { get; } =
            new CardDefinitionId("brynn-over-the-top");

        // -----------------------------------------------------------------------------------------
        // The printed text, transcribed.
        // -----------------------------------------------------------------------------------------

        public const string FamiliarGroundText =
            "Choose one - • Target a unit adjacent to you. • Target a hero in range who is adjacent " +
            "to 3 or more obstacles. ( You, other heroes, minions, tokens and terrain are obstacles ).";

        public const string HighGroundText =
            "Target a unit adjacent to you. If you target a hero adjacent to 3 or more obstacles, " +
            "+2 [Attack] attack.";

        /// <summary>Elevated Ambush prints High Ground's sentence exactly; BR-06 records the values.</summary>
        public const string ElevatedAmbushText = HighGroundText;

        public const string OverTheTopText =
            "While you are performing actions all enemy heroes in play count as adjacent to 3 or " +
            "more obstacles.";

        /// <summary>The four faces this block composes, in his shard's inventory order.</summary>
        public static IReadOnlyList<CardDefinition> ObstacleDefinitions => LazyObstacle.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyObstacle =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                BuildFamiliarGround(),
                BuildRaisedAttack(
                    HighGround, "High Ground", CardTier.I, 4, PrintedValueChannel.None,
                    HighGroundText, new[] { ElevatedAmbush, SplitAttack }),
                BuildRaisedAttack(
                    ElevatedAmbush, "Elevated Ambush", CardTier.II, 5,
                    PrintedValueChannel.Initiative, ElevatedAmbushText,
                    new[] { PeakPrecision, SplitThrow }),
                BuildOverTheTop()
            }));

        /// <summary>
        /// Familiar Ground. p.9's Gold band with a RANGED Attack primary, so p.13 reads its printed
        /// Range icon; its own text names the targets instead, which is
        /// <see cref="AttackTargetBinding.ByCardText"/> - the action reaches p.14's step 1 with no
        /// target and the modal answers with bullets.
        ///
        /// <para>The text applies BEFORE the attack, because choosing which bullet to take is how the
        /// target is bound at all - Hand Crossbow's timing exactly.</para>
        /// </summary>
        private static CardDefinition BuildFamiliarGround() => new CardDefinition(
            FamiliarGround,
            ContentRevision,
            "Familiar Ground",
            new InitiativeValue(12),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimaryRangedAttack,
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Attack, 2),
                        new PrintedValueDefinition(PrintedValueChannel.Range, 3)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.Ranged,
                    default,
                    BrynnEffectContent.FamiliarGround(
                        FamiliarGround, ContentRevision),
                    PrintedTextComposition.None,
                    PrintedDefenseResolution.None,
                    CardTextTiming.BeforeTheAttack,
                    PrintedAttackTargetPolicy
                        .ForClassification(AttackRangeKind.Ranged)
                        .BoundByCardText()),

                new ActionDefinition(
                    SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),

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
            CardSourceContent.Transcribed(FamiliarGroundText));

        /// <summary>
        /// High Ground and Elevated Ambush, which differ in Tier, Attack value, item icon and nothing
        /// else - they print the identical sentence.
        ///
        /// <para><b>They compose no card text at all.</b> "Target a unit adjacent to you" is p.13's own
        /// reading of a non-Ranged Attack, which every Attack definition already carries, and the "+2
        /// attack" is a value on the Attack total rather than a step the page performs - see
        /// <see cref="BrynnEffectContent"/>'s own remarks and BR-Q6.</para>
        /// </summary>
        private static CardDefinition BuildRaisedAttack(
            CardDefinitionId id,
            string displayName,
            CardTier tier,
            int printedAttack,
            PrintedValueChannel itemIcon,
            string printedText,
            IReadOnlyList<CardDefinitionId> nextTierCandidates) => new CardDefinition(
            id,
            ContentRevision,
            displayName,
            new InitiativeValue(9),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.NonBasicCard(
                CardColor.Red, tier, itemIcon, nextTierCandidates),
            new List<ActionDefinition>
            {
                new ActionDefinition(
                    PrimaryAttack,
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, printedAttack) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged,
                    default,
                    null,
                    PrintedTextComposition.None,
                    PrintedDefenseResolution.None,
                    CardTextTiming.None,
                    null,
                    RaisedAttack()),

                new ActionDefinition(
                    SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 4) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),

                new ActionDefinition(
                    SecondaryDefense,
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 6) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            },
            null,
            CardSourceContent.Transcribed(printedText));

        /// <summary>
        /// "If you target a hero adjacent to 3 or more obstacles, +2 [Attack] attack" - the printed
        /// plus beside a conditional bonus, as the one shape this build has for one: a contribution
        /// declared on the ACTION and scaled by a reusable board predicate (BR-Q6).
        /// </summary>
        public static IEnumerable<RosterValueContributionDefinition> RaisedAttack() =>
            new[]
            {
                new RosterValueContributionDefinition(
                    PrintedValueChannel.Attack,
                    RosterValueQueryKind.AttackTargetHeroAdjacentToThreeOrMoreObstacles,
                    2)
            };

        /// <summary>
        /// Over the Top. p.16's permanent ability: it prints no Initiative, no icons and no action at
        /// all, so it is never played, never discarded to defend, and the whole of its content is the
        /// standing declaration below. It is on no ladder, because p.16 UNLOCKS an Ultimate rather than
        /// upgrading into one.
        /// </summary>
        private static CardDefinition BuildOverTheTop() => new CardDefinition(
            OverTheTop,
            ContentRevision,
            "Over the Top",
            default,
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.UltimateCard(),
            Array.Empty<ActionDefinition>(),
            UltimateRuleDefinition.PermanentAbility(
                BrynnEffectContent.OverTheTopRuleId,
                BrynnEffectContent.OverTheTopRuleKind,
                BrynnEffectContent.OverTheTopModifications),
            CardSourceContent.Transcribed(OverTheTopText));
    }
}
