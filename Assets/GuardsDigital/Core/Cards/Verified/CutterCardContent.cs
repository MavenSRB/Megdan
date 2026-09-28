using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Cutter the Sky Pirate's nineteen printed faces as definition content - milestone 8.39h, and the
    /// fourteenth <see cref="CardContentAuthority.VerifiedOfficial"/> hero this project has built.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at milestone 8.39's inventory pass and
    /// recorded in the <c>Docs/AI/Rules/HERO_CUTTER.md</c> shard. The tables there and the rows here
    /// are meant to be read side by side: the order, the names and the column meanings match, so a
    /// reviewer can audit one against the other without holding anything in their head. Nothing is
    /// derived, for the reason <see cref="WaspCardContent"/> records.</para>
    ///
    /// <para><b>The second-printing rulebook is silent about her, so the extraction is sole
    /// authority.</b> It returns zero hits for <c>Cutter</c>, zero for <c>Sky Pirate</c> and zero for
    /// all nineteen card names. That is Swift's reason rather than Ursafar's: she is a base-box hero
    /// the one second source this project holds simply does not mention, not a Devoted Hero pack hero
    /// with no second source at all.</para>
    ///
    /// <para><b>What is different from the thirteen sets already built.</b> Her Silver basic is the
    /// only face in the corpus that targets an OBSTACLE rather than one of p.19's units, and the only
    /// one that overrides p.18's immunity without qualification. Her Gold basic is the first push whose
    /// consequence reads the zone the pushed hero ended in. Her three melee Reds are the first Attacks
    /// whose printed <c>+</c> is scaled by whether the attacker moved during the action, and the first
    /// to narrow a target to the heading a move inside the same action travelled. Her Blue Tier III
    /// prints the first repeat in the roster that excludes a card-text SELECTION's subject rather than
    /// an Attack's target - and the other Blue Tier III prints the only sentence in the whole corpus
    /// that ends the match for a single hero.</para>
    ///
    /// <para><b>She owns no token and no marker</b>, and prints no Attack primary that is not one of
    /// her five Reds: every other dealt face of hers is a Basic Skill, a Skill or the Ultimate. She
    /// prints no Movement primary and no Defense primary at all.</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file, and no member of the content model it builds, names an image, an
    /// art asset or a file path. A definition is its values, its words and its relationships.</para>
    /// </summary>
    public static class CutterCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, with the same coupling every verified set has:
        /// it moves when Cutter's content does.
        /// </summary>
        public const string ContentRevision = "cutter-9-2-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself.
        /// </summary>
        public const string ContentSetId = "cutter";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Her Gold basic, and the roster's first face whose consequence reads rulebook p.19's zones.
        ///
        /// <para>"If that hero is pushed into another zone" is judged on where the push started against
        /// where it ended, never on the spaces between - so a push that leaves a zone and comes back
        /// has carried nobody anywhere. Milestone 8.39c's pair on the push's own outputs is that
        /// reading.</para>
        /// </summary>
        public static CardDefinitionId WalkThePlank { get; } = Id("walk-the-plank");

        /// <summary>
        /// p.20's Handicap card, deferred exactly as every other hero's is and by the same 2026-08-21
        /// user decision: p.6 returns it to the box and the six-player pitch never brings it back. Its
        /// printed values and words ship here as inventory all the same.
        ///
        /// <para>It is the more restrictive half of a pair with <see cref="WalkThePlank"/>, which is the
        /// shape Swift's Backup has to Reload! and Tigerclaw's Crackling Dagger to Magnetic Dagger: it
        /// pushes three where Walk the Plank pushes four, prints Defense 1 where that prints 2, and
        /// drops the "Choose one" and its minion bullet entirely.</para>
        /// </summary>
        public static CardDefinitionId FewMoreSteps { get; } = Id("few-more-steps");

        /// <summary>
        /// Her Silver basic, and the only face in the corpus that targets an obstacle.
        ///
        /// <para>It is also her one single-secondary face and milestone 8.35z20's slot-position trap in
        /// its purest form: its only icon is drawn in the LOWER band where sixteen of her faces put a
        /// boot, and its glyph is a riveted shield. Defense 2, no secondary Movement.</para>
        /// </summary>
        public static CardDefinitionId GrapplingBolt { get; } = Id("grappling-bolt");

        public static CardDefinitionId DaringStrike { get; } = Id("daring-strike");
        public static CardDefinitionId BoldThrust { get; } = Id("bold-thrust");
        public static CardDefinitionId EvasiveShot { get; } = Id("evasive-shot");
        public static CardDefinitionId FearlessLunge { get; } = Id("fearless-lunge");
        public static CardDefinitionId TumbleShot { get; } = Id("tumble-shot");

        public static CardDefinitionId Bombardment { get; } = Id("bombardment");
        public static CardDefinitionId Barrage { get; } = Id("barrage");
        public static CardDefinitionId XMarksTheSpot { get; } = Id("x-marks-the-spot");
        public static CardDefinitionId Broadside { get; } = Id("broadside");
        public static CardDefinitionId AFistfulOfCoins { get; } = Id("a-fistful-of-coins");

        public static CardDefinitionId BraceForImpact { get; } = Id("brace-for-impact");
        public static CardDefinitionId RammingSpeed { get; } = Id("ramming-speed");
        public static CardDefinitionId Outmaneuver { get; } = Id("outmaneuver");
        public static CardDefinitionId Crashland { get; } = Id("crashland");
        public static CardDefinitionId Outsmart { get; } = Id("outsmart");

        public static CardDefinitionId LegendOfTheSkies { get; } = Id("legend-of-the-skies");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as the thirteen earlier sets' are. Cutter
        // needs five, and prints neither a Movement primary nor a Defense primary.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryBasicSkill { get; } =
            new ActionDefinitionId("primary-basic-skill");

        public static ActionDefinitionId PrimaryAttack { get; } =
            new ActionDefinitionId("primary-attack");

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// p.6's five starting cards: one of each colour a hero holds at setup.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            WalkThePlank, GrapplingBolt, DaringStrike, Bombardment, BraceForImpact
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and
        /// p.16's unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            BoldThrust, EvasiveShot, FearlessLunge, TumbleShot,
            Barrage, XMarksTheSpot, Broadside, AFistfulOfCoins,
            RammingSpeed, Outmaneuver, Crashland, Outsmart,
            LegendOfTheSkies
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } =
            Array.AsReadOnly(new[] { FewMoreSteps });

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAllDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() =>
                Array.AsReadOnly(Profiles().Select(Build).ToArray()));

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDealtDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(
                LazyAllDefinitions.Value
                    .Where(definition => !ReturnedToBoxAtSetup.Contains(definition.Id))
                    .ToArray()));

        /// <summary>
        /// All nineteen faces, in the inventory's printed-value table order.
        ///
        /// <para><b>Built lazily, and that is a deliberate divergence from the twelve sets before
        /// Swift's.</b> Those twelve compose eagerly with <c>= Array.AsReadOnly(...)</c> and are not
        /// wrong; this is the convention milestone 8.38d adopted after a static-initialization cycle
        /// cost milestone 8.37 1,423 failing cases on one <c>NullReferenceException</c> and then cost
        /// the thirteenth-hero session something far worse - <c>NUnit failed to load</c>, with ZERO
        /// tests discovered and the whole suite absent rather than red, while <c>dotnet build</c>
        /// reported no errors at all. Deferring the composition to first use removes the cycle edge in
        /// both directions AND converts a hidden constructor throw into an ordinary named failure, so
        /// it is adopted here at the start rather than as a repair.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;

        /// <summary>The eighteen definitions a match deals from - everything but the boxed Handicap card.</summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealtDefinitions.Value;

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Cutter prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Cutter's cards require a defined owner.", nameof(ownerId));
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

        private static IEnumerable<CutterCardProfile> Profiles() => new[]
        {
            new CutterCardProfile(
                WalkThePlank, "Walk the Plank", 12, 2, 1,
                CutterPrimaryKind.BasicSkill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Choose one — • Push an enemy hero adjacent to you up to 4 spaces; if that hero is " +
                "pushed into another zone, that hero discards a card, or is defeated. • Defeat a " +
                "minion adjacent to you.",
                PrintedTextComposition.None),

            new CutterCardProfile(
                FewMoreSteps, "Few More Steps", 12, 1, 1,
                CutterPrimaryKind.BasicSkill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Push an enemy hero adjacent to you up to 3 spaces; if that hero is pushed into " +
                "another zone, that hero discards a card, or is defeated.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration")),

            // The one single-secondary face in her set, and milestone 8.35z20's slot-position trap in
            // its purest form: its only icon is drawn in the LOWER band where sixteen of her faces put
            // a boot, and its glyph is a riveted shield. Defense 2, no secondary Movement.
            new CutterCardProfile(
                GrapplingBolt, "Grappling Bolt", 5, 2, 0,
                CutterPrimaryKind.BasicSkill, 0, PrintedValueChannel.Range, 5,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "Target an obstacle in range and in a straight line, with no obstacles between you; " +
                "ignore immunity. Move in a straight line towards that obstacle until you are " +
                "adjacent.",
                PrintedTextComposition.None),

            // Her three melee Reds print `Attack 4+` and `5+`, which is p.14's own convention for a
            // number the card text raises - the same notation Tigerclaw's Backstab and Xargatha's Long
            // Thrust use. The printed text says where the increment comes from, and the +2 belongs to
            // the branch that moves first and to nothing else.
            new CutterCardProfile(
                DaringStrike, "Daring Strike", 8, 6, 3,
                CutterPrimaryKind.Attack, 4, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    BoldThrust, EvasiveShot),
                "Choose one — • Before the attack: Move 1 space in a straight line. Target a hero " +
                "adjacent to you in the direction of the move; +2 Attack. • Target a unit adjacent " +
                "to you.",
                PrintedTextComposition.None,
                CutterAttackShape.MovedBranchLunge),

            new CutterCardProfile(
                BoldThrust, "Bold Thrust", 9, 6, 3,
                CutterPrimaryKind.Attack, 5, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    FearlessLunge, TumbleShot),
                "Choose one — • Before the attack: Move 1 or 2 spaces in a straight line. Target a " +
                "hero adjacent to you in the direction of the move; +2 Attack. • Target a unit " +
                "adjacent to you.",
                PrintedTextComposition.None,
                CutterAttackShape.MovedBranchLunge),

            new CutterCardProfile(
                EvasiveShot, "Evasive Shot", 9, 4, 3,
                CutterPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    FearlessLunge, TumbleShot),
                "Target a unit in range and in a straight line. After the attack: Move up to 2 spaces " +
                "in the opposite direction.",
                PrintedTextComposition.None,
                CutterAttackShape.StraightLineShot),

            new CutterCardProfile(
                FearlessLunge, "Fearless Lunge", 9, 7, 3,
                CutterPrimaryKind.Attack, 5, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Movement),
                "Choose one — • Before the attack: Move 1, 2 or 3 spaces in a straight line. Target a " +
                "hero adjacent to you in the direction of the move; +2 Attack. • Target a unit " +
                "adjacent to you.",
                PrintedTextComposition.None,
                CutterAttackShape.MovedBranchLunge),

            new CutterCardProfile(
                TumbleShot, "Tumble Shot", 9, 5, 3,
                CutterPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Radius),
                "Target a unit in range and in a straight line. After the attack: Move up to 3 spaces " +
                "in the opposite direction.",
                PrintedTextComposition.None,
                CutterAttackShape.StraightLineShot),

            // Bombardment and Barrage print BYTE-IDENTICAL text and differ only in the Radius their
            // icon carries. They compose as two definitions all the same, on Whisper's Shadow Step and
            // Shadow Walk precedent: the Radius is an icon rather than a word, and a shared definition
            // would let a p.16 Radius item raise the wrong face.
            new CutterCardProfile(
                Bombardment, "Bombardment", 10, 5, 3,
                CutterPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    Barrage, XMarksTheSpot),
                "An enemy hero adjacent to another enemy unit and not adjacent to you discards a " +
                "card, if able.",
                PrintedTextComposition.None),

            new CutterCardProfile(
                Barrage, "Barrage", 10, 5, 3,
                CutterPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Initiative,
                    Broadside, AFistfulOfCoins),
                "An enemy hero adjacent to another enemy unit and not adjacent to you discards a " +
                "card, if able.",
                PrintedTextComposition.None),

            new CutterCardProfile(
                XMarksTheSpot, "X Marks the Spot", 10, 5, 3,
                CutterPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    Broadside, AFistfulOfCoins),
                "An enemy hero in radius chooses one — • You place that hero in a space in radius. " +
                "• You gain 2 coins.",
                PrintedTextComposition.None),

            new CutterCardProfile(
                Broadside, "Broadside", 11, 6, 3,
                CutterPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Initiative),
                "An enemy hero adjacent to another enemy unit and not adjacent to you discards a " +
                "card, if able. May repeat once on a different target.",
                PrintedTextComposition.None),

            new CutterCardProfile(
                AFistfulOfCoins, "A Fistful of Coins", 11, 6, 3,
                CutterPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Attack),
                "An enemy hero in radius chooses one — • You place that hero in a space in radius. " +
                "• You gain 3 coins. If you have 13 or more coins, you alone win the game.",
                PrintedTextComposition.None),

            // Three of her five Greens print no reach glyph at all, because their own text carries
            // their distance and names their destination by adjacency rather than by a measured disk.
            new CutterCardProfile(
                BraceForImpact, "Brace for Impact", 3, 3, 2,
                CutterPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    RammingSpeed, Outmaneuver),
                "Move 3 spaces in a straight line, ignoring obstacles, to a space adjacent to an " +
                "enemy hero; that hero discards a card, if able.",
                PrintedTextComposition.None),

            new CutterCardProfile(
                RammingSpeed, "Ramming Speed", 3, 3, 2,
                CutterPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    Crashland, Outsmart),
                "Move 3 or 4 spaces in a straight line, ignoring obstacles, to a space adjacent to an " +
                "enemy hero; that hero discards a card, if able.",
                PrintedTextComposition.None),

            new CutterCardProfile(
                Outmaneuver, "Outmaneuver", 3, 3, 2,
                CutterPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Defense,
                    Crashland, Outsmart),
                "Swap with an enemy minion in radius; you may move that minion up to 2 spaces.",
                PrintedTextComposition.None),

            new CutterCardProfile(
                Crashland, "Crashland", 2, 4, 2,
                CutterPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Range),
                "Move 3, 4 or 5 spaces in a straight line, ignoring obstacles, to a space adjacent to " +
                "an enemy hero; that hero discards a card, if able.",
                PrintedTextComposition.None),

            new CutterCardProfile(
                Outsmart, "Outsmart", 2, 4, 2,
                CutterPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Defense),
                "Swap with an enemy minion in radius; you may move that minion up to 3 spaces.",
                PrintedTextComposition.None),

            // p.16's Ultimate. It prints no Initiative, no action, and neither Range nor Radius, so
            // ledger question 12's item-reaches-the-Ultimate reading has nothing to reach here. Its
            // whole printed content is the permanent ability.
            new CutterCardProfile(
                LegendOfTheSkies, "Legend of the Skies", 0, 0, 0,
                CutterPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "The first time each turn after you perform a primary action, you may perform the " +
                "primary action of a card in the previous turn slot.",
                PrintedTextComposition.None)
        };

        private static CardDefinition Build(CutterCardProfile profile)
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
                    ? UltimateRuleDefinition.TriggeredAbility(
                        "legend-of-the-skies-after-primary-action",
                        "CutterLegendOfTheSkies",
                        CutterEffectContent
                            .LegendOfTheSkiesTriggeredEffect(ContentRevision).Reference,

                        // Milestone 8.39f1's sixth condition, and the reason it had to exist: the five
                        // before it are all matched against a fact that is undefined unless the source
                        // card is p.9-Basic, and every card prints a primary action.
                        UltimateTriggerCondition.AfterControllerPerformsPrimaryAction,
                        new[] { TimingCheckpoint.ActionEnded },
                        PrintedIcon(profile))
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));
        }

        private static IEnumerable<ActionDefinition> BuildActions(CutterCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the inventory's Sec Mov or Sec Def column means the face prints no such icon,
            // so that secondary action is unavailable from this card - expressed by the action not
            // existing rather than by a zero value. Grappling Bolt is the only card in her set where
            // that falls, and it falls on Movement.
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

        private static ActionDefinition BuildPrimary(CutterCardProfile profile)
        {
            switch (profile.Primary)
            {
                case CutterPrimaryKind.None:
                    return null;

                case CutterPrimaryKind.BasicSkill:
                    return new ActionDefinition(
                        PrimaryBasicSkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        CutterEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                case CutterPrimaryKind.Attack:
                    return new ActionDefinition(
                        PrimaryAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        CutterEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        CutterEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.NonRanged),
                        RaisedAttack(profile));

                case CutterPrimaryKind.RangedAttack:
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        CutterEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        CutterEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.Ranged),
                        RaisedAttack(profile));

                case CutterPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        CutterEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Cutter primary action kind.");
            }
        }

        /// <summary>
        /// The printed <c>+2 Attack</c> her three melee Reds carry, as the action-level value
        /// declaration every printed <c>+N</c> in this build is.
        ///
        /// <para><b>It is declared on the ACTION and still belongs to one bullet, and milestone 8.39g2
        /// is the whole of why that works.</b> A <see cref="RosterValueContributionDefinition"/> cannot
        /// be arm-conditional - there is no node-level value grant in this build at all - so the arm is
        /// identified by its own consequence instead: <see cref="RosterValueQueryKind.ActorMovedThisAction"/>
        /// asks whether the attacker is standing somewhere other than where the Attack started, which
        /// is the question <c>actor-moved-this-action@1</c> asks from the effect side. The two are
        /// equivalent to "the first bullet was taken" because the Move inside that bullet is mandatory,
        /// which is CU-Q4's answer settled at that milestone.</para>
        ///
        /// <para>The alternative - an Active effect the first arm creates - was measured and refused:
        /// <c>RuleValueApplicability</c> has nothing scoped to ONE action, so a turn-scoped raise would
        /// also lift a repeat of this Attack or an Attack granted later in the same turn, which is a
        /// rule the card does not print.</para>
        /// </summary>
        private static IEnumerable<RosterValueContributionDefinition> RaisedAttack(
            CutterCardProfile profile) =>
            profile.AttackShape == CutterAttackShape.MovedBranchLunge
                ? new[]
                {
                    new RosterValueContributionDefinition(
                        PrintedValueChannel.Attack,
                        RosterValueQueryKind.ActorMovedThisAction,
                        2)
                }
                : Enumerable.Empty<RosterValueContributionDefinition>();

        /// <summary>
        /// What one of her printed Attacks may target where its own words say something p.13's default
        /// does not. All five of her Red faces do, in two different ways.
        ///
        /// <para>The three melee lunges name their target in their own text rather than choosing one
        /// when the action starts, which is <see cref="AttackTargetBinding.ByCardText"/>: the action
        /// reaches p.14's step 1 with no target and each bullet binds one through
        /// <c>set-attack-target@1</c>.</para>
        ///
        /// <para>The two Ranged shots print "in range and in a straight line", which is milestone
        /// 8.38a's line member reached through Swift's own builder rather than constructed by hand.
        /// Their reach is p.13's own for a Ranged Attack and is not restated.</para>
        ///
        /// <para><b>A Ranged Attack always carries a policy</b> - <c>ForClassification</c> is p.13's
        /// own default and replaces a null - so "this face narrows nothing" is read off
        /// <c>NarrowsTheDefault</c> and never off a null check. No face of hers reads that way.</para>
        /// </summary>
        private static PrintedAttackTargetPolicy? TargetPolicy(
            CutterCardProfile profile, AttackRangeKind attackRange)
        {
            var classification = PrintedAttackTargetPolicy.ForClassification(attackRange);
            switch (profile.AttackShape)
            {
                case CutterAttackShape.None:
                    return null;
                case CutterAttackShape.MovedBranchLunge:
                    return classification.BoundByCardText();
                case CutterAttackShape.StraightLineShot:
                    return classification.RestrictedToTheStraightLine();
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.AttackShape, "Unknown Cutter attack shape.");
            }
        }

        /// <summary>
        /// p.9's one Range or Radius icon, or nothing when the face prints neither. Eight of her faces
        /// print neither - both Gold basics, the three melee Reds, three of the five Greens and the
        /// Ultimate - and the absence is content.
        /// </summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(CutterCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum CutterPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,

            /// <summary>p.9's Gold and Silver band - a Skill printed on a basic card.</summary>
            BasicSkill = 1,

            Attack = 2,
            RangedAttack = 3,
            Skill = 4
        }

        /// <summary>
        /// Which printed targeting sentence one of her Red faces states, or <see cref="None"/> for a
        /// face that is not an Attack at all. Every Attack of hers states one.
        /// </summary>
        private enum CutterAttackShape
        {
            None = 0,

            /// <summary>
            /// Daring Strike, Bold Thrust and Fearless Lunge: a modal whose bullets each name the
            /// target, and whose first bullet raises the Attack by moving first.
            /// </summary>
            MovedBranchLunge = 1,

            /// <summary>Evasive Shot and Tumble Shot: "Target a unit in range and in a straight line".</summary>
            StraightLineShot = 2
        }

        private sealed class CutterCardProfile
        {
            public CutterCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                CutterPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition,
                CutterAttackShape attackShape = CutterAttackShape.None)
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
                AttackShape = attackShape;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public CutterPrimaryKind Primary { get; }
            public int PrimaryValue { get; }
            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }
            public CutterAttackShape AttackShape { get; }
        }
    }
}
