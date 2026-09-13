using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Bain the Bounty Hunter's nineteen printed faces as definition content - milestone 8.40i, and the
    /// fifteenth <see cref="CardContentAuthority.VerifiedOfficial"/> hero this project has built.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at milestone 8.40's inventory pass and
    /// recorded in the <c>Docs/AI/Rules/HERO_BAIN.md</c> shard. The tables there and the rows here are
    /// meant to be read side by side: the order, the names and the column meanings match, so a reviewer
    /// can audit one against the other without holding anything in their head. Nothing is derived, for
    /// the reason <see cref="WaspCardContent"/> records.</para>
    ///
    /// <para><b>The second-printing rulebook names him once and says nothing.</b> BA-01: a scan returns
    /// exactly one hit for <c>Bain</c> and one for <c>Bounty Hunter</c> - the same line, printed page
    /// 2's opening fiction - and zero for all nineteen card names. So the extraction is sole authority
    /// for these faces, exactly as it is for Swift's and Cutter's, by a different route.</para>
    ///
    /// <para><b>What is different from the fourteen sets already built.</b> His Green line is shaped
    /// like nobody else's: three of its five are Blocks and two are Movement primaries, so his Green
    /// colour prints no Skill at all, and every one of the five reads the same board fact - "If a hero
    /// in play has a bounty marker". Three of his Blue faces have an ENEMY hero choose one of their own
    /// hand cards and keep it hidden while he guesses its colour, which is the first hidden information
    /// and the first public reveal this build has ever carried. And his marker is the second rules
    /// component in the project after Tigerclaw's Poison - one copy, whose holder pays rulebook p.13 an
    /// additional Life counter when defeated.</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file, and no member of the content model it builds, names an image, an
    /// art asset or a file path. A definition is its values, its words and its relationships.</para>
    /// </summary>
    public static class BainCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, with the same coupling every verified set has:
        /// it moves when Bain's content does.
        /// </summary>
        public const string ContentRevision = "bain-8-40i-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself.
        /// </summary>
        public const string ContentSetId = "bain";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// His Gold basic, and the face his Ultimate watches: a Basic Attack on an adjacent unit whose
        /// "After the attack:" clause may give an enemy hero in radius the bounty marker.
        ///
        /// <para>BA-07 settles the reach icon at crop resolution: the lower-right glyph is the circling
        /// arrows of a RADIUS and not the arrow of a Range, which is what the text's own "an enemy hero
        /// in radius" has to match.</para>
        /// </summary>
        public static CardDefinitionId DeadOrAlive { get; } = Id("dead-or-alive");

        /// <summary>
        /// p.20's Handicap card, deferred exactly as every other hero's is and by the same 2026-08-21
        /// user decision: p.6 returns it to the box and the six-player pitch never brings it back. Its
        /// printed values and words ship here as inventory all the same.
        ///
        /// <para>BA-14 records how its give differs from Dead or Alive's: this one is mandatory and
        /// gated on "If you are adjacent to an enemy unit" - a fact about Bain - where Dead or Alive's
        /// is p.10's "may" and unconditional, and neither condition is about the hero receiving the
        /// marker.</para>
        /// </summary>
        public static CardDefinitionId SideQuest { get; } = Id("side-quest");

        /// <summary>
        /// His Silver basic: the pull. It is the one face of his that prints p.18's full obstacle list -
        /// "with no obstacles between you and the target" - where his three plain crossbows print the
        /// strictly narrower "no units, or terrain", which is BA-03 and the most consequential source
        /// fact in his inventory.
        /// </summary>
        public static CardDefinitionId GetOverHere { get; } = Id("get-over-here");

        public static CardDefinitionId LightCrossbow { get; } = Id("light-crossbow");
        public static CardDefinitionId HeavyCrossbow { get; } = Id("heavy-crossbow");
        public static CardDefinitionId HandCrossbow { get; } = Id("hand-crossbow");
        public static CardDefinitionId Arbalest { get; } = Id("arbalest");
        public static CardDefinitionId HunterSeeker { get; } = Id("hunter-seeker");

        public static CardDefinitionId AGameOfChance { get; } = Id("a-game-of-chance");
        public static CardDefinitionId DeadMansHand { get; } = Id("dead-mans-hand");
        public static CardDefinitionId DrinkingBuddies { get; } = Id("drinking-buddies");
        public static CardDefinitionId WereNotDoneYet { get; } = Id("were-not-done-yet");
        public static CardDefinitionId AnotherOne { get; } = Id("another-one");

        public static CardDefinitionId CloseCall { get; } = Id("close-call");
        public static CardDefinitionId NarrowEscape { get; } = Id("narrow-escape");
        public static CardDefinitionId VantagePoint { get; } = Id("vantage-point");
        public static CardDefinitionId PerfectGetaway { get; } = Id("perfect-getaway");
        public static CardDefinitionId HighGround { get; } = Id("high-ground");

        public static CardDefinitionId AComplicatedProfession { get; } =
            Id("a-complicated-profession");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as the fourteen earlier sets' are. Bain
        // needs seven, and he is the first hero in the roster to print a Block primary AND a Movement
        // primary in one colour.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryBasicAttack { get; } =
            new ActionDefinitionId("primary-basic-attack");

        public static ActionDefinitionId PrimaryBasicSkill { get; } =
            new ActionDefinitionId("primary-basic-skill");

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        public static ActionDefinitionId PrimaryBlock { get; } = new ActionDefinitionId("primary-block");

        public static ActionDefinitionId PrimaryMovement { get; } =
            new ActionDefinitionId("primary-movement");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// p.6's five starting cards: one of each colour a hero holds at setup.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            DeadOrAlive, GetOverHere, LightCrossbow, AGameOfChance, CloseCall
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and
        /// p.16's unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            HeavyCrossbow, HandCrossbow, Arbalest, HunterSeeker,
            DeadMansHand, DrinkingBuddies, WereNotDoneYet, AnotherOne,
            NarrowEscape, VantagePoint, PerfectGetaway, HighGround,
            AComplicatedProfession
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } =
            Array.AsReadOnly(new[] { SideQuest });

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
        /// <para><b>Built lazily from the first line</b>, which is the convention milestone 8.38d
        /// adopted and Swift's and Cutter's sets follow. The static-initialization cycle it avoids does
        /// not present as a failure: it presents as <c>NUnit failed to load</c> with ZERO tests
        /// discovered and a green <c>dotnet build</c>, which reads like tooling breakage rather than
        /// like content breakage.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;

        /// <summary>The eighteen definitions a match deals from - everything but the boxed Handicap card.</summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealtDefinitions.Value;

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Bain prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Bain's cards require a defined owner.", nameof(ownerId));
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

        private static IEnumerable<BainCardProfile> Profiles() => new[]
        {
            new BainCardProfile(
                DeadOrAlive, "Dead or Alive", 11, 2, 1,
                BainPrimaryKind.BasicAttack, 4, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a unit adjacent to you. After the attack: You may give an enemy hero in " +
                "radius a [bounty] bounty marker; a hero with a bounty marker spends 1 additional " +
                "[life] life counter when defeated.",
                PrintedTextComposition.None),

            new BainCardProfile(
                SideQuest, "Side Quest", 10, 1, 1,
                BainPrimaryKind.BasicSkill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "If you are adjacent to an enemy unit, give a hero in radius a [bounty] bounty " +
                "marker. The hero with a bounty marker spends 1 additional [life] life counter when " +
                "defeated.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration")),

            // BA-05's cleanest instance: its one secondary glyph is drawn in the LOWER band where
            // twelve of his faces put a boot, and the glyph is a riveted shield. Defense 2, no
            // secondary Movement - read by SHAPE at the shared crop and never by slot position.
            new BainCardProfile(
                GetOverHere, "Get over here!", 13, 2, 0,
                BainPrimaryKind.BasicSkill, 0, PrintedValueChannel.Range, 4,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "Target a unit or a token in a straight line and in range with no obstacles between " +
                "you and the target; move the target towards you in a straight line, until you are " +
                "adjacent.",
                PrintedTextComposition.None),

            new BainCardProfile(
                LightCrossbow, "Light Crossbow", 8, 5, 4,
                BainPrimaryKind.RangedAttack, 5, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    HeavyCrossbow, HandCrossbow),
                "Target a unit in a straight line and in range with no units, or terrain, between you.",
                PrintedTextComposition.None,
                BainAttackShape.ClearLineShot),

            new BainCardProfile(
                HeavyCrossbow, "Heavy Crossbow", 8, 5, 4,
                BainPrimaryKind.RangedAttack, 5, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    Arbalest, HunterSeeker),
                "Target a unit in a straight line and in range with no units, or terrain, between you.",
                PrintedTextComposition.None,
                BainAttackShape.ClearLineShot),

            new BainCardProfile(
                HandCrossbow, "Hand Crossbow", 8, 6, 4,
                BainPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    Arbalest, HunterSeeker),
                "Choose one — • Target a hero in range with a [bounty] bounty marker. • Target a unit " +
                "adjacent to you.",
                PrintedTextComposition.None,
                BainAttackShape.MarkerModal),

            new BainCardProfile(
                Arbalest, "Arbalest", 9, 6, 4,
                BainPrimaryKind.RangedAttack, 6, PrintedValueChannel.Range, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Defense),
                "Target a unit in a straight line and in range with no units, or terrain, between you.",
                PrintedTextComposition.None,
                BainAttackShape.ClearLineShot),

            new BainCardProfile(
                HunterSeeker, "Hunter-Seeker", 9, 7, 4,
                BainPrimaryKind.RangedAttack, 5, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Movement),
                "Choose one, or both, on different targets — • Target a hero in range with a [bounty] " +
                "bounty marker. • Target a unit adjacent to you.",
                PrintedTextComposition.None,
                BainAttackShape.MarkerModalOnBothTargets),

            new BainCardProfile(
                AGameOfChance, "A Game of Chance", 9, 4, 3,
                BainPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    DeadMansHand, DrinkingBuddies),
                "An enemy hero in radius with two or more cards in hand chooses one of those cards; " +
                "Guess the card's color, then reveal the card; If you guessed correctly, discard that " +
                "card; otherwise you gain 1 coin. (You can only guess colors that could be in that " +
                "player's hand).",
                PrintedTextComposition.None),

            new BainCardProfile(
                DeadMansHand, "Dead Man's Hand", 10, 4, 3,
                BainPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Defense,
                    WereNotDoneYet, AnotherOne),
                "An enemy hero in radius with two or more cards in hand chooses one of those cards; " +
                "Guess that card's color, then reveal the card; If you guessed correctly, discard " +
                "that card; otherwise you gain 2 coins.",
                PrintedTextComposition.None),

            // BA-12. Drinking Buddies and Another One! print BYTE-IDENTICAL first sentences at the
            // SAME Radius, so - unlike Cutter's Bombardment and Barrage - the reach does not separate
            // them. What separates them is Another One!'s extra keyword sentence, and the composition
            // must not collapse the two into one definition.
            new BainCardProfile(
                DrinkingBuddies, "Drinking Buddies", 10, 4, 3,
                BainPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    WereNotDoneYet, AnotherOne),
                "You may have a hero in radius retrieve a discarded card. If they do, you may also " +
                "retrieve a discarded card.",
                PrintedTextComposition.None),

            new BainCardProfile(
                WereNotDoneYet, "We're Not Done Yet!", 10, 5, 3,
                BainPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Range),
                "An enemy hero in radius with two or more cards in hand chooses one of those cards; " +
                "Guess that card's color, then reveal the card; If you guessed correctly, discard " +
                "that card; otherwise may repeat once or gain 2 coins.",
                PrintedTextComposition.None),

            new BainCardProfile(
                AnotherOne, "Another One!", 10, 5, 3,
                BainPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Attack),
                "You may have a hero in radius retrieve a discarded card. If they do, you may also " +
                "retrieve a discarded card. End of turn: May repeat once.",
                PrintedTextComposition.None),

            // BA-08. All five Green faces print a bare DEFENSE or MOVEMENT band with no reach glyph at
            // all, because none of their clauses measures a distance - each asks only whether SOME
            // hero in play holds the marker.
            new BainCardProfile(
                CloseCall, "Close Call", 4, 0, 2,
                BainPrimaryKind.Block, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    NarrowEscape, VantagePoint),
                "If a hero in play has a [bounty] bounty marker, block the attack and that hero gives " +
                "the marker to you. (The marker's effect is applied to you).",
                PrintedTextComposition.None),

            new BainCardProfile(
                NarrowEscape, "Narrow Escape", 4, 0, 2,
                BainPrimaryKind.Block, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Initiative,
                    PerfectGetaway, HighGround),
                "If a hero in play has a [bounty] bounty marker, block the attack and retrieve the " +
                "marker.",
                PrintedTextComposition.None),

            // BA-09. The printed `2+` is p.14's own notation for a value the card text raises, and the
            // text is what says where the increment comes from.
            new BainCardProfile(
                VantagePoint, "Vantage Point", 4, 3, 0,
                BainPrimaryKind.Movement, 2, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    PerfectGetaway, HighGround),
                "Ignore obstacles. If a hero in play has a [bounty] bounty marker, +1 [movement] " +
                "movement.",
                PrintedTextComposition.None),

            new BainCardProfile(
                PerfectGetaway, "Perfect Getaway", 3, 0, 2,
                BainPrimaryKind.Block, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Initiative),
                "If a hero in play has a [bounty] bounty marker, block the attack.",
                PrintedTextComposition.None),

            new BainCardProfile(
                HighGround, "High Ground", 3, 4, 0,
                BainPrimaryKind.Movement, 2, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Radius),
                "Ignore obstacles. If a hero in play has a [bounty] bounty marker, +2 [movement] " +
                "movement.",
                PrintedTextComposition.None),

            // p.16's Ultimate. It prints no Initiative, no action, and neither Range nor Radius, so
            // ledger question 12's item-reaches-the-Ultimate reading has nothing to reach here. Its
            // whole printed content is the permanent ability.
            new BainCardProfile(
                AComplicatedProfession, "A Complicated Profession", 0, 0, 0,
                BainPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "After you give a hero the [bounty] bounty marker that hero discards a card, if able.",
                PrintedTextComposition.None)
        };

        private static CardDefinition Build(BainCardProfile profile)
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
                    ? UltimateRuleDefinition.AfterMarkerGivenAbility(
                        "a-complicated-profession-after-marker-given",
                        "BainAComplicatedProfession",
                        BainEffectContent
                            .AComplicatedProfessionTriggeredEffect(ContentRevision).Reference,
                        PrintedIcon(profile))
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));
        }

        private static IEnumerable<ActionDefinition> BuildActions(BainCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the inventory's Sec Mov or Sec Def column means the face prints no such icon,
            // so that secondary action is unavailable from this card - expressed by the action not
            // existing rather than by a zero value. Six faces of his print exactly one icon (BA-05),
            // and all six draw it in the same band: three shields and three boots.
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

        private static ActionDefinition BuildPrimary(BainCardProfile profile)
        {
            switch (profile.Primary)
            {
                case BainPrimaryKind.None:
                    return null;

                case BainPrimaryKind.BasicSkill:
                    return new ActionDefinition(
                        PrimaryBasicSkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        BainEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                case BainPrimaryKind.BasicAttack:
                    // p.9's Gold band with an Attack primary: "BASIC ATTACK" on the card, a non-Ranged
                    // Attack in p.13's terms, whose printed target clause - "Target a unit adjacent to
                    // you" - is that page's own sentence and therefore narrows nothing.
                    return new ActionDefinition(
                        PrimaryBasicAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        BainEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        BainEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.NonRanged));

                case BainPrimaryKind.RangedAttack:
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        BainEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        BainEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.Ranged));

                case BainPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        BainEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                case BainPrimaryKind.Block:
                    // p.14's Block card, spent from hand exactly as a numeric Defense is and offered
                    // only in response to an attack. What is new here is the CONDITION: milestone
                    // 8.40g's third `PrintedBlockCondition` member, which asks the board rather than
                    // the attacking card, and which all three of his Green Blocks declare.
                    return new ActionDefinition(
                        PrimaryBlock,
                        ActionKind.Defense,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                        BainEffectContent.DefenseTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.Block,
                        BainEffectContent.DefenseTextTimingFor(profile.Id),
                        blockCondition: PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker);

                case BainPrimaryKind.Movement:
                    // p.11's Movement as Primary Action: "apply the card text in addition to moving up
                    // to the number of spaces on the icon". Both of his print a raise, so both are read
                    // BEFORE the walk - a raise applied afterwards would raise an allowance already
                    // spent, which is Ursafar's Cold Ire reading exactly.
                    return new ActionDefinition(
                        PrimaryMovement,
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        BainEffectContent.MovementTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        BainEffectContent.MovementTextTimingFor(profile.Id));

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Bain primary action kind.");
            }
        }

        /// <summary>
        /// What one of his printed Attacks may target where its own words say something p.13's default
        /// does not. Four of his six Attack faces do, in two different ways; Dead or Alive prints p.13's
        /// own sentence for a non-Ranged Attack and therefore narrows nothing.
        ///
        /// <para>The three plain crossbows print "in a straight line and in range with no units, or
        /// terrain, between you", which is milestone 8.40d's fourth line member reached through its own
        /// builder. BA-03 is why that is a different member from the pull's obstacle list: p.18 makes
        /// the wider list strictly wider, so a TOKEN blocks Get over here! and does not block a
        /// crossbow, and one deck printing both halves is as strong a signal as this corpus gives that
        /// the narrower wording is deliberate.</para>
        ///
        /// <para>The two modal Attacks print no line clause at all and name their targets in their own
        /// bullets, which is <see cref="AttackTargetBinding.ByCardText"/>: the action reaches p.14's
        /// step 1 with no target and the modal answers with bullets.</para>
        /// </summary>
        private static PrintedAttackTargetPolicy? TargetPolicy(
            BainCardProfile profile, AttackRangeKind attackRange)
        {
            var classification = PrintedAttackTargetPolicy.ForClassification(attackRange);
            switch (profile.AttackShape)
            {
                case BainAttackShape.None:
                    return null;
                case BainAttackShape.ClearLineShot:
                    return classification.RestrictedToTheStraightLineClearOfUnitsAndTerrain();
                case BainAttackShape.MarkerModal:
                case BainAttackShape.MarkerModalOnBothTargets:
                    return classification.BoundByCardText();
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.AttackShape, "Unknown Bain attack shape.");
            }
        }

        /// <summary>
        /// p.9's one Range or Radius icon, or nothing when the face prints neither. Six of his faces
        /// print neither - all five Greens (BA-08) and the Ultimate - and the absence is content.
        /// </summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(BainCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum BainPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,

            /// <summary>p.9's Gold and Silver band - a Skill printed on a basic card.</summary>
            BasicSkill = 1,

            /// <summary>p.9's Gold band with an Attack primary - his Dead or Alive.</summary>
            BasicAttack = 2,

            RangedAttack = 3,
            Skill = 4,

            /// <summary>p.14's Block, which three of his five Greens print.</summary>
            Block = 5,

            /// <summary>p.11's Movement as a primary action, which the other two Greens print.</summary>
            Movement = 6
        }

        /// <summary>
        /// Which printed targeting sentence one of his Attack faces states, or <see cref="None"/> for a
        /// face that states p.13's own.
        /// </summary>
        private enum BainAttackShape
        {
            /// <summary>Dead or Alive: "Target a unit adjacent to you", which is p.13's own sentence.</summary>
            None = 0,

            /// <summary>
            /// Light Crossbow, Heavy Crossbow and Arbalest: "in a straight line and in range with no
            /// units, or terrain, between you".
            /// </summary>
            ClearLineShot = 1,

            /// <summary>Hand Crossbow: "Choose one —" over two bullets that each name a target.</summary>
            MarkerModal = 2,

            /// <summary>
            /// Hunter-Seeker: "Choose one, or both, on different targets —", which BA-D4 reads as two
            /// complete p.14 orders, each defending separately.
            /// </summary>
            MarkerModalOnBothTargets = 3
        }

        private sealed class BainCardProfile
        {
            public BainCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                BainPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition,
                BainAttackShape attackShape = BainAttackShape.None)
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
            public BainPrimaryKind Primary { get; }
            public int PrimaryValue { get; }
            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }
            public BainAttackShape AttackShape { get; }
        }
    }
}
