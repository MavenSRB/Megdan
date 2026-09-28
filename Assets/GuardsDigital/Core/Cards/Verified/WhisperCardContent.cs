using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Whisper the Outcast's nineteen printed faces as definition content - milestone
    /// <c>8.35z33</c>, and the first hero of the <b>Devoted Hero pack</b> this project has
    /// built.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade
    /// pair below is transcribed from the extracted card faces by the read-only inventory worker of
    /// 2026-09-05/06. Nothing is derived, for the reason <see cref="WaspCardContent"/> records. Under
    /// the pack-wide source ruling (PLAN Ruling 27) the Tabletopia extraction is the <em>sole</em>
    /// authority for a pack hero's faces: the second-printing rulebook names neither Whisper nor any
    /// of her nineteen cards, which is expected of an expansion hero and is recorded as an absence
    /// rather than a gap.</para>
    ///
    /// <para><b>What her set is the first verified content to state.</b> Two things.
    /// <list type="number">
    /// <item><description><b>A p.14 timing keyword printed inside a modal bullet.</b> Swift Justice
    /// and Vile Accusation print "After the attack:" inside their <em>first</em> bullet only, so
    /// whether the step-7 seam opens at all is decided at targeting time. Brogan's two modal Ranged
    /// Attacks are the only other modal card texts in the verified roster and neither carries a timing
    /// keyword.</description></item>
    /// <item><description><b>An Ultimate that changes how another card's text is read.</b> Grim Reaper
    /// adds no trigger, no value and no rule source: it raises the number of options a modal on one of
    /// her <em>basic</em> cards admits. Every earlier verified Ultimate states something true about the
    /// board.</description></item>
    /// </list></para>
    ///
    /// <para><b>What her set does not print, which is content too.</b> No Defense primary, no Movement
    /// primary, no p.14 Block, no <c>!</c> icon, and no token or marker on any face - so this milestone
    /// adds no component content at all. Every Red card is an Attack and every Blue and Green card is a
    /// Skill, without exception; that is the same shape Sabina's set has. Grim Reaper prints neither
    /// Range nor Radius, so retired ledger question 12 has no instance here.</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file names an image, an art asset or a file path.</para>
    /// </summary>
    public static class WhisperCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, with the same coupling
        /// <see cref="TigerclawCardContent.ContentRevision"/> has: it moves when Whisper's content
        /// does.
        /// </summary>
        public const string ContentRevision = "whisper-9-1-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself.
        /// </summary>
        public const string ContentSetId = "whisper";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        public static CardDefinitionId SwiftJustice { get; } = Id("swift-justice");
        public static CardDefinitionId VileAccusation { get; } = Id("vile-accusation");
        public static CardDefinitionId DeathSeeker { get; } = Id("death-seeker");
        public static CardDefinitionId CrimsonTrail { get; } = Id("crimson-trail");
        public static CardDefinitionId SanguinePath { get; } = Id("sanguine-path");
        public static CardDefinitionId BloodFury { get; } = Id("blood-fury");
        public static CardDefinitionId BloodPilgrimage { get; } = Id("blood-pilgrimage");
        public static CardDefinitionId BloodFrenzy { get; } = Id("blood-frenzy");
        public static CardDefinitionId SeedsOfFear { get; } = Id("seeds-of-fear");
        public static CardDefinitionId SproutsOfPanic { get; } = Id("sprouts-of-panic");
        public static CardDefinitionId LesserEvil { get; } = Id("lesser-evil");
        public static CardDefinitionId BloomingNightmare { get; } = Id("blooming-nightmare");
        public static CardDefinitionId GreaterGood { get; } = Id("greater-good");
        public static CardDefinitionId ShadowStep { get; } = Id("shadow-step");
        public static CardDefinitionId ShadowWalk { get; } = Id("shadow-walk");
        public static CardDefinitionId CruelTwist { get; } = Id("cruel-twist");
        public static CardDefinitionId CreepingShadow { get; } = Id("creeping-shadow");
        public static CardDefinitionId SealedFate { get; } = Id("sealed-fate");
        public static CardDefinitionId GrimReaper { get; } = Id("grim-reaper");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as the six earlier sets' are. Whisper
        // needs four: she prints no Defense primary and no Movement primary, so there is neither a
        // Block identity nor a primary Movement one here.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// p.6 step 11's starting hand: "the three Tier I cards and the two basic cards with no Tier".
        /// The Handicap card is already out of the box by then, so the Gold card here is Swift Justice.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            SwiftJustice, DeathSeeker, CrimsonTrail, SeedsOfFear, ShadowStep
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and
        /// p.16's unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            SanguinePath, BloodFury, BloodPilgrimage, BloodFrenzy,
            SproutsOfPanic, LesserEvil, BloomingNightmare, GreaterGood,
            ShadowWalk, CruelTwist, CreepingShadow, SealedFate,
            GrimReaper
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content, which is why it is listed apart
        /// from both.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[]
        {
            VileAccusation
        });

        /// <summary>
        /// All nineteen faces, in the order the inventory's printed-value table lists them.
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions { get; } = Array.AsReadOnly(
            Profiles().Select(Build).ToArray());

        /// <summary>
        /// The eighteen definitions a match deals from - everything but the boxed Handicap card.
        /// </summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions { get; } = Array.AsReadOnly(
            AllDefinitions
                .Where(definition => !ReturnedToBoxAtSetup.Contains(definition.Id))
                .ToArray());

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Whisper prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Whisper's cards require a defined owner.", nameof(ownerId));
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
        // The printed faces. One row per card, in the inventory's table order, with the same columns:
        // Initiative, the two secondary icons (0 where the face prints none), the primary action and
        // its value, the p.9 Range or Radius icon, the progression content, and the transcribed text.
        // -----------------------------------------------------------------------------------------

        private static IEnumerable<WhisperCardProfile> Profiles() => new[]
        {
            // Her Gold basic, and the roster's first modal Attack whose bullets are two different
            // TARGETING sentences - which is why its target is bound by its own card text rather than
            // by p.14's opening question. Bullet 1 additionally carries a step-7 rider, so the
            // "After the attack:" seam exists on one branch of the modal and not the other.
            new WhisperCardProfile(
                SwiftJustice, "Swift Justice", 12, 2, 1,
                WhisperPrimaryKind.RangedAttack, 2, PrintedValueChannel.Range, 3,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Choose one — • Target a hero in range with an empty discard; After the attack: If " +
                "able, that hero performs a movement action on the card they defended with, moving " +
                "full distance in a straight line. • Target a unit adjacent to you.",
                PrintedTextComposition.None,
                bindsTargetInCardText: true),

            // p.20's Handicap card, deferred exactly as Wasp's Crackling Dagger, Arien's Ceremonial
            // Blade, Brogan's Leap Forward, Sabina's Bang, Tigerclaw's Nimble Strike and Xargatha's
            // Desperate Cleave are, by the same 2026-08-21 user decision: p.6 returns it to the box
            // and the six-player pitch never brings it back. Its printed values and words ship here
            // as inventory all the same. It is Swift Justice narrowed in exactly one place - its
            // second bullet says "a hero" where the Gold card says "a unit" - and every value is
            // lower, which is the whole of what p.20's handicap does.
            new WhisperCardProfile(
                VileAccusation, "Vile Accusation", 11, 1, 1,
                WhisperPrimaryKind.RangedAttack, 1, PrintedValueChannel.Range, 2,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Choose one — • Target a hero in range with an empty discard; After the attack: If " +
                "able, that hero performs a movement action on the card they defended with, moving " +
                "full distance in a straight line. • Target a hero adjacent to you.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration"),
                bindsTargetInCardText: true),

            // Her Silver basic. One printed secondary icon and it is the Defense shield, sitting in
            // the slot a two-icon card gives the Movement boot - the trap the inventory records at
            // length and re-read at 7x against a known boot and a known shield. The face prints no
            // Movement secondary at all, which is why the row's Sec Mov is zero.
            new WhisperCardProfile(
                DeathSeeker, "Death Seeker", 13, 2, 0,
                WhisperPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "If an enemy hero in radius has a card in the discard, choose one — • This turn: You " +
                "are immune to enemy actions. • You may retrieve a discarded card.",
                PrintedTextComposition.None),

            // The Red line, and it splits the way Arien's already does: three faces print "Before the
            // attack:" (step 1) and two print "After the attack:" (step 7). What separates the two
            // families here is that the step-1 three move BEFORE they target, so their target is
            // bound by their own text, and the step-7 two target first and repeat afterwards.
            new WhisperCardProfile(
                CrimsonTrail, "Crimson Trail", 9, 4, 4,
                WhisperPrimaryKind.Attack, 3, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    SanguinePath, BloodFury),
                "Before the attack: You may move up to 1 space if there is an empty minion spawn " +
                "point in radius in the battle zone. Target a unit adjacent to you.",
                PrintedTextComposition.None,
                bindsTargetInCardText: true),

            new WhisperCardProfile(
                SanguinePath, "Sanguine Path", 9, 4, 4,
                WhisperPrimaryKind.Attack, 3, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    BloodPilgrimage, BloodFrenzy),
                "Before the attack: You may move up to 1 space for every empty minion spawn point in " +
                "radius in the battle zone up to a maximum of 2 spaces. Target a unit adjacent to you.",
                PrintedTextComposition.None,
                bindsTargetInCardText: true),

            new WhisperCardProfile(
                BloodFury, "Blood Fury", 9, 6, 4,
                WhisperPrimaryKind.Attack, 6, PrintedValueChannel.Radius, 5,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    BloodPilgrimage, BloodFrenzy),
                "Target a unit adjacent to you. After the attack: If an enemy hero in radius has a " +
                "card in the discard, repeat once on a different target.",
                PrintedTextComposition.None),

            new WhisperCardProfile(
                BloodPilgrimage, "Blood Pilgrimage", 10, 5, 4,
                WhisperPrimaryKind.Attack, 4, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Movement),
                "Before the attack: You may move up to 1 space for every empty minion spawn point in " +
                "radius in the battle zone up to a maximum of 3 spaces. Target a unit adjacent to you.",
                PrintedTextComposition.None,
                bindsTargetInCardText: true),

            new WhisperCardProfile(
                BloodFrenzy, "Blood Frenzy", 10, 7, 4,
                WhisperPrimaryKind.Attack, 7, PrintedValueChannel.Radius, 5,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Range),
                "Target a unit adjacent to you. After the attack: If an enemy hero in radius has a " +
                "card in the discard, repeat up to five times on different targets.",
                PrintedTextComposition.None),

            // The Blue line's two families. Three faces move an enemy unit off a spawn point, and the
            // only thing separating them is how far; two hand the decision to an enemy hero.
            new WhisperCardProfile(
                SeedsOfFear, "Seeds of Fear", 10, 5, 2,
                WhisperPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    SproutsOfPanic, LesserEvil),
                "Target an enemy unit in radius occupying a spawn point. Move that unit 1 space.",
                PrintedTextComposition.None),

            new WhisperCardProfile(
                SproutsOfPanic, "Sprouts of Panic", 10, 5, 2,
                WhisperPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Initiative,
                    BloomingNightmare, GreaterGood),
                "Target an enemy unit in radius occupying a spawn point. Move that unit up to 2 spaces.",
                PrintedTextComposition.None),

            // The parenthesis is p.9 reminder text and composes to nothing - the designer's own
            // 2026-08-19 answer, in as many words: "It holds no rules text. The card would function in
            // exactly the same way if you remove the text in italics. Reminder text is only printed on
            // the lowest-tier card of the branch." So Greater Good, which omits it, behaves the same.
            new WhisperCardProfile(
                LesserEvil, "Lesser Evil", 10, 5, 2,
                WhisperPrimaryKind.Skill, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    BloomingNightmare, GreaterGood),
                "An enemy hero in range chooses one — • That hero discards a card, if able. • You may " +
                "defeat a minion adjacent to you. ( Any option can be chosen, even if it would have " +
                "no effect ).",
                PrintedTextComposition.None),

            new WhisperCardProfile(
                BloomingNightmare, "Blooming Nightmare", 11, 6, 2,
                WhisperPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Initiative),
                "Target an enemy unit in radius occupying a spawn point. Move that unit up to 3 spaces.",
                PrintedTextComposition.None),

            new WhisperCardProfile(
                GreaterGood, "Greater Good", 11, 6, 2,
                WhisperPrimaryKind.Skill, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Attack),
                "An enemy hero in range chooses one — • That hero discards a card, or is defeated. " +
                "• You defeat up to 3 minions adjacent to you.",
                PrintedTextComposition.None),

            // The Green line. Three placements and two swaps, and Shadow Step and Shadow Walk print
            // byte-for-byte identical text - the Tier II printing buys one point of Range, one of
            // Initiative and an Attack item icon and nothing else. That is the same "identical
            // sentence, different icon" pair Wasp's Shock and Electrocute have.
            new WhisperCardProfile(
                ShadowStep, "Shadow Step", 3, 3, 2,
                WhisperPrimaryKind.Skill, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    ShadowWalk, CruelTwist),
                "Place yourself into an empty minion spawn point in range in the battle zone.",
                PrintedTextComposition.None),

            new WhisperCardProfile(
                ShadowWalk, "Shadow Walk", 2, 3, 2,
                WhisperPrimaryKind.Skill, 0, PrintedValueChannel.Range, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    CreepingShadow, SealedFate),
                "Place yourself into an empty minion spawn point in range in the battle zone.",
                PrintedTextComposition.None),

            new WhisperCardProfile(
                CruelTwist, "Cruel Twist", 2, 3, 2,
                WhisperPrimaryKind.Skill, 0, PrintedValueChannel.Range, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Defense,
                    CreepingShadow, SealedFate),
                "Swap with a unit adjacent to you, or with an enemy hero in range with a card in the " +
                "discard. You may then move up to 2 spaces.",
                PrintedTextComposition.None),

            new WhisperCardProfile(
                CreepingShadow, "Creeping Shadow", 2, 4, 2,
                WhisperPrimaryKind.Skill, 0, PrintedValueChannel.Range, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Radius),
                "Place yourself into an empty minion spawn point in range in the battle zone, or into " +
                "a space in range adjacent to such spawn point.",
                PrintedTextComposition.None),

            new WhisperCardProfile(
                SealedFate, "Sealed Fate", 2, 4, 2,
                WhisperPrimaryKind.Skill, 0, PrintedValueChannel.Range, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Defense),
                "Swap with a unit adjacent to you, or with an enemy hero in range with a card in the " +
                "discard. You may then move up to 3 spaces.",
                PrintedTextComposition.None),

            // p.16's permanent ability. It prints no Initiative, no icons and no action at all, which
            // is what makes the row's zeros a statement rather than an omission - the same shape
            // Xargatha's Metamorphosis has. What it says is unlike anything else in the roster: it
            // does not state a fact about the board, it changes how a modal on one of her own basic
            // cards is read.
            new WhisperCardProfile(
                GrimReaper, "Grim Reaper", 0, 0, 0,
                WhisperPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "When performing basic actions you may choose one, or both.",
                PrintedTextComposition.None)
        };

        private static CardDefinition Build(WhisperCardProfile profile) =>
            new CardDefinition(
                profile.Id,
                ContentRevision,
                profile.DisplayName,
                profile.Primary == WhisperPrimaryKind.None
                    ? default
                    : new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                profile.Progression,
                BuildActions(profile),
                profile.Id.Equals(GrimReaper)
                    ? UltimateRuleDefinition.PermanentAbility(
                        WhisperEffectContent.GrimReaperRuleId,
                        "WhisperGrimReaper",
                        WhisperEffectContent.GrimReaperModifications)
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));

        private static IEnumerable<ActionDefinition> BuildActions(WhisperCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the inventory's Sec Mov or Sec Def column means the face prints no such icon,
            // so that secondary action is unavailable from this card - expressed by the action not
            // existing rather than by a zero value, exactly as the six earlier sets express it. Her
            // dashes are Death Seeker's Movement column and the Ultimate's whole row.
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

        private static ActionDefinition BuildPrimary(WhisperCardProfile profile)
        {
            switch (profile.Primary)
            {
                case WhisperPrimaryKind.None:
                    return null;

                case WhisperPrimaryKind.Attack:
                    return new ActionDefinition(
                        PrimaryAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        WhisperEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        WhisperEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.NonRanged));

                case WhisperPrimaryKind.RangedAttack:
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        WhisperEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        WhisperEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.Ranged),
                        null,
                        MinionDefenseContributionPolicy.IncludeAll,
                        DefenseOfferOriginRestriction.None,
                        PrintedBlockCondition.None,
                        WhisperEffectContent.SecondAttackTextFor(profile.Id, ContentRevision),
                        WhisperEffectContent.SecondAttackTextTimingFor(profile.Id));

                case WhisperPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        WhisperEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Whisper primary action kind.");
            }
        }

        /// <summary>
        /// What one printed Attack of Whisper's may target, or <c>null</c> where the face narrows
        /// nothing and p.13's own reading for the subtype stands.
        ///
        /// <para><b>Five of her seven Attacks bind their target in their own card text</b>, and the
        /// printed reason differs between the two families. The two Gold cards are modal Attacks whose
        /// bullets <em>are</em> the targeting sentences, so there is no single sentence for p.14's
        /// opening question to ask - the same shape milestone 8.19a minted for Brogan's charges. The
        /// three step-1 Red movers print their move BEFORE "Target a unit adjacent to you.", so under
        /// the 2026-09-06 printed-reading-order ruling the target is named from the space the move
        /// left her in, which p.14's opening question - asked before any text runs - cannot do.</para>
        ///
        /// <para>Blood Fury and Blood Frenzy print the targeting sentence FIRST and their rider at
        /// step 7, so they take p.14's ordinary question and narrow nothing: "a unit" is the page's
        /// own word.</para>
        ///
        /// <para>A deferred face composes no step-1 text for a binding to be about, which is why Vile
        /// Accusation's binding ships with its composition exactly as Nimble Strike's does.</para>
        /// </summary>
        private static PrintedAttackTargetPolicy? TargetPolicy(
            WhisperCardProfile profile, AttackRangeKind attackRange)
        {
            var classification = PrintedAttackTargetPolicy.ForClassification(attackRange);
            if (profile.BindsTargetInCardText && !profile.TextComposition.IsAwaited)
            {
                return classification.BoundByCardText();
            }

            return null;
        }

        /// <summary>The p.9 Range or Radius icon a face prints, or nothing where it prints neither.</summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(WhisperCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum WhisperPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,
            Attack = 1,
            RangedAttack = 2,
            Skill = 3
        }

        private sealed class WhisperCardProfile
        {
            public WhisperCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                WhisperPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition,
                bool bindsTargetInCardText = false)
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
                BindsTargetInCardText = bindsTargetInCardText;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public WhisperPrimaryKind Primary { get; }
            public int PrimaryValue { get; }
            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }

            /// <summary>
            /// Whether this Attack's own text names its target, which p.14's opening question then
            /// does not ask. True on the two modal Gold cards and the three step-1 Red movers.
            /// </summary>
            public bool BindsTargetInCardText { get; }
        }
    }
}
