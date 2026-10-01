using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Silverarrow the Pathfinder's nineteen printed faces as definition content - milestone
    /// 8.35z30, and the first Devoted Hero pack hero this project has composed.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces and recorded in the inventory this milestone
    /// drafts as <c>Docs/AI/Rules/HERO_SILVERARROW.md</c>. Nothing is derived - not the colour from the
    /// printed icon, not the Tier from the Initiative, not the item icon from the colour - for the
    /// reason <see cref="WaspCardContent"/> records.</para>
    ///
    /// <para><b>The source position is weaker than any hero before her, and RULING 27 is why that is
    /// recorded rather than worked around.</b> The local rulebook is the base game's; it never names
    /// her and illustrates none of her cards, so no face of hers can be reproduced against a second
    /// source. The user's pack-wide ruling makes the Tabletopia extraction the sole authority for pack
    /// heroes and leaves ledger question 1 open for the pack. Her set is consistent with the second
    /// printing's conventions - p.9's colour letters and Tiers, the p.20 <c>H</c> card, the p.16 Tier IV
    /// Ultimate, p.21's two-star complexity and five role icons - but consistency with a convention is
    /// not a reproduced face, and it is not claimed as one.</para>
    ///
    /// <para><b>Three structural facts separate her from the six shipped heroes.</b> She prints no
    /// Defense primary and no Block anywhere, so the whole block-condition family milestone 8.11 built
    /// is simply not exercised by her set. Every one of her seven Attacks is Ranged - she has no melee
    /// Attack primary at all, and the two that reach an adjacent unit do so through a modal bullet on a
    /// card whose band still reads Attack - Ranged (RULING 39). And her Ultimate prints neither Range
    /// nor Radius, which makes her the first hero for whom ledger question 12's
    /// item-reaches-the-Ultimate interpretation has nothing at all to apply to.</para>
    /// </summary>
    public static class SilverarrowCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry. It moves when Silverarrow's content changes,
        /// which is the same coupling <see cref="WaspCardContent.ContentRevision"/> has.
        /// </summary>
        public const string ContentRevision = "silverarrow-9-35-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity.
        ///
        /// <para>SL-10: both printed faces spell the name one word with a single capital - the profile
        /// card prints <c>SILVERARROW</c> and the dashboard <c>Silverarrow the Pathfinder</c> - so the
        /// two printed sources agree and there is nothing to reconcile. The extraction directory's
        /// camel case is that extraction's identity, not this project's.</para>
        /// </summary>
        public const string ContentSetId = "silverarrow";

        /// <summary>The display name p.21's selection card and the dashboard print.</summary>
        public const string DisplayName = "Silverarrow the Pathfinder";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        public static CardDefinitionId ShootAndScoot { get; } = Id("shoot-and-scoot");
        public static CardDefinitionId KeepTheDistance { get; } = Id("keep-the-distance");
        public static CardDefinitionId Trailblazer { get; } = Id("trailblazer");
        public static CardDefinitionId SnapShot { get; } = Id("snap-shot");
        public static CardDefinitionId OpportunityShot { get; } = Id("opportunity-shot");
        public static CardDefinitionId LongShot { get; } = Id("long-shot");
        public static CardDefinitionId ClearShot { get; } = Id("clear-shot");
        public static CardDefinitionId RainOfArrows { get; } = Id("rain-of-arrows");
        public static CardDefinitionId GraspingRoots { get; } = Id("grasping-roots");
        public static CardDefinitionId EntanglingVines { get; } = Id("entangling-vines");
        public static CardDefinitionId WarningShot { get; } = Id("warning-shot");
        public static CardDefinitionId GrapplingBranches { get; } = Id("grappling-branches");
        public static CardDefinitionId TreetopSentinel { get; } = Id("treetop-sentinel");
        public static CardDefinitionId Disorient { get; } = Id("disorient");
        public static CardDefinitionId DivertAttention { get; } = Id("divert-attention");
        public static CardDefinitionId FaeHealing { get; } = Id("fae-healing");
        public static CardDefinitionId LeadAstray { get; } = Id("lead-astray");
        public static CardDefinitionId NaturesBlessing { get; } = Id("natures-blessing");
        public static CardDefinitionId WildHunt { get; } = Id("wild-hunt");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as every other hero's are.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// p.6 step 11's starting hand: "the three Tier I cards and the two basic cards with no Tier".
        /// The Handicap card is already out of the box by then, so the Gold card here is Shoot and
        /// Scoot.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            ShootAndScoot, Trailblazer, SnapShot, GraspingRoots, Disorient
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and p.16's
        /// unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            OpportunityShot, LongShot, ClearShot, RainOfArrows,
            EntanglingVines, WarningShot, GrapplingBranches, TreetopSentinel,
            DivertAttention, FaeHealing, LeadAstray, NaturesBlessing,
            WildHunt
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[]
        {
            KeepTheDistance
        });

        /// <summary>All nineteen faces, in the inventory's printed-value table order.</summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions { get; } = Array.AsReadOnly(
            Profiles().Select(Build).ToArray());

        /// <summary>The eighteen definitions a match deals from - everything but the boxed Handicap card.</summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions { get; } = Array.AsReadOnly(
            AllDefinitions
                .Where(definition => !ReturnedToBoxAtSetup.Contains(definition.Id))
                .ToArray());

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Silverarrow prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Silverarrow's cards require a defined owner.", nameof(ownerId));
            }

            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException(
                    "Card instance identities are stable and per-seat, so the caller names the prefix " +
                    "that distinguishes one seat's cards from another's.",
                    nameof(instanceIdPrefix));
            }

            return Array.AsReadOnly(DealtDefinitions
                .Select(definition =>
                {
                    var instanceId = new CardInstanceId($"{instanceIdPrefix}-{definition.Id.Value}");
                    return StartingHand.Contains(definition.Id)
                        ? CardInstance.CreateInHand(instanceId, definition.Reference, ownerId)
                        : CardInstance.CreateInDeck(instanceId, definition.Reference, ownerId);
                })
                .ToArray());
        }

        /// <summary>
        /// One seat's whole setup as a hand the configured match can be composed from: p.6's five in
        /// hand, the thirteen its deck keeps, and the eighteen definitions both draw on.
        /// </summary>
        public static ConfiguredHand CreateHand(HeroId ownerId, string instanceIdPrefix)
        {
            var cards = CreateDealtCards(ownerId, instanceIdPrefix);
            return new ConfiguredHand(
                DealtDefinitions,
                cards.Where(card => StartingHand.Contains(card.DefinitionReference.DefinitionId)),
                cards.Where(card => !StartingHand.Contains(card.DefinitionReference.DefinitionId)));
        }

        private static CardDefinitionId Id(string cardId) =>
            new CardDefinitionId($"{ContentSetId}-{cardId}");

        // -----------------------------------------------------------------------------------------
        // The printed faces. One row per card, in the shard's table order, with the same columns:
        // Initiative, the two secondary icons (0 where the face prints none), the primary action and
        // its value, the p.9 Range or Radius icon, the progression content, the transcribed text, and
        // whether that text is composed.
        // -----------------------------------------------------------------------------------------

        private static IEnumerable<SilverarrowCardProfile> Profiles() => new[]
        {
            // "at maximum range" is RULING 1 and it lands on the target policy rather than on any
            // effect: the target stands at exactly the Range Total, and a unit any nearer is not a
            // legal target of this Attack at all. An Attack 1 that cannot answer an enemy who has
            // closed with her is what the card's own name describes.
            new SilverarrowCardProfile(
                ShootAndScoot, "Shoot and Scoot", 12, 1, 1,
                SilverarrowPrimaryKind.RangedAttack, 1, PrintedValueChannel.Range, 2,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a unit at maximum range. After the attack: If able, you may fast travel to an " +
                "adjacent zone.",
                PrintedTextComposition.None,
                SilverarrowTargetNarrowing.AtMaximumRange),

            // p.20's Handicap card, deferred exactly as Wasp's Crackling Dagger and Arien's Ceremonial
            // Blade are, and by the same 2026-08-21 user decision: p.6 returns it to the box and the
            // six-player pitch never brings it back. Its printed values and words ship here as
            // inventory all the same. It narrows to heroes where Shoot and Scoot admits any unit -
            // the same hero-only narrowing that separates Crackling Dagger from Magnetic Dagger, and
            // the Handicap card is again the more restrictive of the pair.
            new SilverarrowCardProfile(
                KeepTheDistance, "Keep the Distance", 11, 1, 2,
                SilverarrowPrimaryKind.RangedAttack, 1, PrintedValueChannel.Range, 2,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a hero at maximum range. After the attack: If able, you may fast travel to an " +
                "adjacent zone.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration"),
                SilverarrowTargetNarrowing.HeroesAtMaximumRange),

            // The only card in her set printing one secondary icon, and the one the slot-position trap
            // would have got backwards: its single glyph is the riveted shield of a Defense, not the
            // heeled boot of a Movement. The inventory records the glyph-shape verification that
            // settled it against governed reference faces.
            new SilverarrowCardProfile(
                Trailblazer, "Trailblazer", 13, 1, 0,
                SilverarrowPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "You may fast travel, if able. This round: You and friendly heroes in radius may " +
                "ignore obstacles while performing movement actions.",
                PrintedTextComposition.None),

            // The three Red modals print one sentence and differ only in their Attack and Range. The
            // modal is the Attack's own candidate set - see AttackTargetableUnits
            // .IsolatedUnitsOrUnitsAdjacentToYou for why one set states both printed bullets and why
            // the union is invariant to what "any other unit" counts.
            new SilverarrowCardProfile(
                SnapShot, "Snap Shot", 9, 5, 5,
                SilverarrowPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    OpportunityShot, LongShot),
                "Choose one — • Target a unit in range, which is not adjacent to any other unit. " +
                "• Target a unit adjacent to you.",
                PrintedTextComposition.None,
                SilverarrowTargetNarrowing.IsolatedOrAdjacent),

            new SilverarrowCardProfile(
                OpportunityShot, "Opportunity Shot", 9, 5, 5,
                SilverarrowPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    ClearShot, RainOfArrows),
                "Choose one — • Target a unit in range, which is not adjacent to any other unit. " +
                "• Target a unit adjacent to you.",
                PrintedTextComposition.None,
                SilverarrowTargetNarrowing.IsolatedOrAdjacent),

            new SilverarrowCardProfile(
                LongShot, "Long Shot", 9, 4, 5,
                SilverarrowPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    ClearShot, RainOfArrows),
                "Target a unit at maximum range.",
                PrintedTextComposition.None,
                SilverarrowTargetNarrowing.AtMaximumRange),

            // Range 4 is the longest printed reach in the verified roster; Wasp's maximum is 3.
            new SilverarrowCardProfile(
                ClearShot, "Clear Shot", 10, 6, 5,
                SilverarrowPrimaryKind.RangedAttack, 5, PrintedValueChannel.Range, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Radius),
                "Choose one — • Target a unit in range, which is not adjacent to any other unit. " +
                "• Target a unit adjacent to you.",
                PrintedTextComposition.None,
                SilverarrowTargetNarrowing.IsolatedOrAdjacent),

            new SilverarrowCardProfile(
                RainOfArrows, "Rain of Arrows", 10, 5, 5,
                SilverarrowPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Initiative),
                "Target a unit at maximum range. If you target a hero, repeat once on a different " +
                "hero; if you do, repeat once on a minion.",
                PrintedTextComposition.None,
                SilverarrowTargetNarrowing.AtMaximumRange),

            // Arien's Deluge, word for word. Three cards, one sentence, three radii.
            new SilverarrowCardProfile(
                GraspingRoots, "Grasping Roots", 10, 3, 3,
                SilverarrowPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    EntanglingVines, WarningShot),
                "This turn: Enemy heroes in radius cannot fast travel, or move more than 1 space with " +
                "a movement action.",
                PrintedTextComposition.None),

            new SilverarrowCardProfile(
                EntanglingVines, "Entangling Vines", 10, 3, 3,
                SilverarrowPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Defense,
                    GrapplingBranches, TreetopSentinel),
                "This turn: Enemy heroes in radius cannot fast travel, or move more than 1 space with " +
                "a movement action.",
                PrintedTextComposition.None),

            // The roster's first "End of turn:" face.
            new SilverarrowCardProfile(
                WarningShot, "Warning Shot", 10, 3, 3,
                SilverarrowPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    GrapplingBranches, TreetopSentinel),
                "End of turn: An enemy hero in radius discards a card, if able.",
                PrintedTextComposition.None),

            new SilverarrowCardProfile(
                GrapplingBranches, "Grappling Branches", 11, 4, 3,
                SilverarrowPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Defense),
                "This turn: Enemy heroes in radius cannot fast travel, or move more than 1 space with " +
                "a movement action.",
                PrintedTextComposition.None),

            // Warning Shot's sentence with ledger question 13's escalation in place of its "if able" -
            // the same relationship Wasp's Shock and Electroblast have.
            new SilverarrowCardProfile(
                TreetopSentinel, "Treetop Sentinel", 11, 4, 3,
                SilverarrowPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Range),
                "End of turn: An enemy hero in radius discards a card, or is defeated.",
                PrintedTextComposition.None),

            // Its self-move differs from its two siblings in two printed ways at once: it is optional
            // and it carries no straight-line constraint. Recorded as printed.
            new SilverarrowCardProfile(
                Disorient, "Disorient", 3, 1, 2,
                SilverarrowPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    DivertAttention, FaeHealing),
                "Move an enemy unit adjacent to you 1 space; if you do, you may move 1 space.",
                PrintedTextComposition.None),

            new SilverarrowCardProfile(
                DivertAttention, "Divert Attention", 2, 2, 2,
                SilverarrowPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Initiative,
                    LeadAstray, NaturesBlessing),
                "Move an enemy unit adjacent to you up to 2 spaces; if you do, move up to that number " +
                "of spaces in a straight line.",
                PrintedTextComposition.None),

            new SilverarrowCardProfile(
                FaeHealing, "Fae Healing", 2, 2, 2,
                SilverarrowPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    LeadAstray, NaturesBlessing),
                "A hero in radius may retrieve a discarded card; if they do, that hero gains 1 coin.",
                PrintedTextComposition.None),

            new SilverarrowCardProfile(
                LeadAstray, "Lead Astray", 2, 2, 2,
                SilverarrowPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Movement),
                "Move an enemy unit adjacent to you up to 3 spaces; if you do, move up to that number " +
                "of spaces in a straight line.",
                PrintedTextComposition.None),

            new SilverarrowCardProfile(
                NaturesBlessing, "Nature's Blessing", 2, 2, 2,
                SilverarrowPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Attack),
                "A hero in radius may retrieve a discarded card; if they do, that hero gains 2 coins.",
                PrintedTextComposition.None),

            // p.16's Ultimate. It prints no Initiative, no action, and - unlike Wasp's High Voltage and
            // like Arien's Living Tsunami - neither Range nor Radius, which is why ledger question 12's
            // item-reaches-the-Ultimate reading has nothing to reach here. Its whole printed content is
            // the permanent ability.
            new SilverarrowCardProfile(
                WildHunt, "Wild Hunt", 0, 0, 0,
                SilverarrowPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "Each time before you perform an action you may move 2 spaces in a straight line.",
                PrintedTextComposition.None)
        };

        private static CardDefinition Build(SilverarrowCardProfile profile)
        {
            var isUltimate = profile.Progression.IsUltimate;
            return new CardDefinition(
                profile.Id,
                ContentRevision,
                profile.DisplayName,
                isUltimate ? default : new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                profile.Progression,
                BuildActions(profile),
                isUltimate
                    ? UltimateRuleDefinition.BeforeActionAbility(
                        "wild-hunt-before-action",
                        "SilverarrowWildHunt",
                        SilverarrowEffectContent.WildHuntTriggeredEffect(ContentRevision).Reference,
                        // "Each time before you perform an action": no limit and no action kind, which
                        // is the one pairing BeforeActionTriggerFrequency.EveryAction admits.
                        new BeforeActionTrigger(
                            ActionKind.None, BeforeActionTriggerFrequency.EveryAction),
                        PrintedIcon(profile))
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));
        }

        private static IEnumerable<ActionDefinition> BuildActions(SilverarrowCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the inventory's Sec Mov or Sec Def column means the face prints no such icon,
            // so that secondary action is unavailable from this card - expressed by the action not
            // existing rather than by a zero value. Trailblazer is the only card in her set where that
            // falls, and it falls on Movement.
            if (profile.SecondaryMovement > 0)
            {
                actions.Add(new ActionDefinition(
                    SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Movement, profile.SecondaryMovement)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)));
            }

            if (profile.SecondaryDefense > 0)
            {
                actions.Add(new ActionDefinition(
                    SecondaryDefense,
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Defense, profile.SecondaryDefense)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile)));
            }

            return actions;
        }

        private static ActionDefinition BuildPrimary(SilverarrowCardProfile profile)
        {
            switch (profile.Primary)
            {
                case SilverarrowPrimaryKind.None:
                    return null;

                case SilverarrowPrimaryKind.RangedAttack:
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        SilverarrowEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        SilverarrowEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile));

                case SilverarrowPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        SilverarrowEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Silverarrow primary action kind.");
            }
        }

        /// <summary>
        /// What one of her Attacks may target, where its own words say something p.13's default does
        /// not - which, uniquely in the verified roster, is every Attack she prints.
        ///
        /// <para>Six of the seven narrow the reach or the candidate set and one narrows both: Keep the
        /// Distance is hero-only <em>and</em> at maximum range. The declaration is what makes RULING 1
        /// and the modal one answer for the offer, the choice and the resolution; nothing about either
        /// is written as an effect.</para>
        ///
        /// <para>Note what is <b>not</b> declared: no face of hers is
        /// <see cref="AttackTargetBinding.ByCardText"/>, because none moves before it targets, and none
        /// excludes p.19's straight line, because none prints the Boomerangs' clause.</para>
        /// </summary>
        private static PrintedAttackTargetPolicy? TargetPolicy(SilverarrowCardProfile profile)
        {
            if (profile.TargetNarrowing == SilverarrowTargetNarrowing.None)
            {
                return null;
            }

            var classification = PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged);
            switch (profile.TargetNarrowing)
            {
                case SilverarrowTargetNarrowing.AtMaximumRange:
                    return classification.AtExactlyThePrintedRange();
                case SilverarrowTargetNarrowing.HeroesAtMaximumRange:
                    return classification.NarrowedToHeroes().AtExactlyThePrintedRange();
                case SilverarrowTargetNarrowing.IsolatedOrAdjacent:
                    return classification.NarrowedToIsolatedUnitsAndThoseAdjacentToYou();
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.TargetNarrowing,
                        "Unknown Silverarrow target narrowing.");
            }
        }

        /// <summary>
        /// p.9's one Range or Radius icon, or nothing when the face prints neither. Four of her faces
        /// print neither, and the absence is content.
        /// </summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(SilverarrowCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum SilverarrowPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,

            /// <summary>
            /// Every Attack she prints. She has no melee Attack primary anywhere in her set, which is
            /// the first time that is true of a verified hero.
            /// </summary>
            RangedAttack = 1,

            Skill = 2
        }

        /// <summary>
        /// Which of the three sentences one of her Attacks prints in place of p.13's default targeting.
        /// It is a row of the printed table rather than a rule: <see cref="TargetPolicy"/> is where each
        /// one becomes a declaration.
        /// </summary>
        private enum SilverarrowTargetNarrowing
        {
            /// <summary>p.13's own default. No Attack of hers takes it, and no Skill has one at all.</summary>
            None = 0,

            /// <summary>"Target a unit at maximum range." - RULING 1.</summary>
            AtMaximumRange = 1,

            /// <summary>"Target a hero at maximum range." - the Handicap card, narrowing both halves.</summary>
            HeroesAtMaximumRange = 2,

            /// <summary>The three Red modals' two bullets, as one candidate set.</summary>
            IsolatedOrAdjacent = 3
        }

        private sealed class SilverarrowCardProfile
        {
            public SilverarrowCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                SilverarrowPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition,
                SilverarrowTargetNarrowing targetNarrowing = SilverarrowTargetNarrowing.None)
            {
                Id = id;
                DisplayName = displayName;
                Initiative = initiative;
                SecondaryDefense = secondaryDefense;
                SecondaryMovement = secondaryMovement;
                Primary = primary;
                PrimaryValue = primaryValue;
                IconChannel = iconChannel;
                IconValue = iconValue;
                Progression = progression;
                PrintedText = printedText;
                TextComposition = textComposition;
                TargetNarrowing = targetNarrowing;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public SilverarrowPrimaryKind Primary { get; }

            /// <summary>
            /// The number printed on the primary action's own icon. One field rather than three,
            /// because p.9 prints one primary icon per card.
            /// </summary>
            public int PrimaryValue { get; }

            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }
            public SilverarrowTargetNarrowing TargetNarrowing { get; }
        }
    }
}
