using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Arien the Tidemaster's nineteen printed faces as definition content - milestone 8.20, and the
    /// second <see cref="CardContentAuthority.VerifiedOfficial"/> hero this project has built.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at milestone 8.18a and recorded in
    /// <c>Docs/AI/Rules/HERO_ARIEN.md</c>. The tables there and the rows here are meant to be read side
    /// by side: the order, the names and the column meanings match, so a reviewer can audit one against
    /// the other without holding anything in their head. Nothing is derived - not the colour from the
    /// printed icon, not the Tier from the Initiative, not the item icon from the colour - for the
    /// reason <see cref="WaspCardContent"/> records, and Arien's own set would break any such
    /// derivation as thoroughly as hers.</para>
    ///
    /// <para><b>What is different from Wasp's set, and therefore what this file is the first to
    /// state.</b> Arien prints no Block card and no combined Defense/Skill card. His three Blue Duelist
    /// cards print a Defense value in the <em>primary</em> slot - rulebook p.14 illustrates exactly that
    /// shape and names it "Primary Defense Value" - so their text reaches the table only when the card
    /// is discarded to defend. Two Blue cards print a Movement primary, whose text p.11 applies before
    /// the move. Five cards print "Before the attack:", which no implemented card had used. And Master
    /// Duelist prints "This round:", the first Active effect in this build that outlives a turn.</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file, and no member of the content model it builds, names an image, an
    /// art asset or a file path. A definition is its values, its words and its relationships.</para>
    /// </summary>
    public static class ArienCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry. It moves when Arien's content changes, which is
        /// the same coupling <see cref="WaspCardContent.ContentRevision"/> has and the reason a later
        /// slice's compatibility decision is a real decision rather than a formality.
        /// </summary>
        public const string ContentRevision = "arien-8-20-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity. It is not a
        /// <see cref="HeroId"/>: a HeroId names a seat in a match, while this names the printed set that
        /// seat deals from. It is also the identity the prepared hero token is addressed by, which is
        /// why it is a plain lowercase word and not a display name.
        /// </summary>
        public const string ContentSetId = "arien";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        public static CardDefinitionId NobleBlade { get; } = Id("noble-blade");
        public static CardDefinitionId CeremonialBlade { get; } = Id("ceremonial-blade");
        public static CardDefinitionId SpellBreak { get; } = Id("spell-break");
        public static CardDefinitionId DangerousCurrent { get; } = Id("dangerous-current");
        public static CardDefinitionId RagingStream { get; } = Id("raging-stream");
        public static CardDefinitionId RogueWave { get; } = Id("rogue-wave");
        public static CardDefinitionId ViolentTorrent { get; } = Id("violent-torrent");
        public static CardDefinitionId TidalBlast { get; } = Id("tidal-blast");
        public static CardDefinitionId AspiringDuelist { get; } = Id("aspiring-duelist");
        public static CardDefinitionId ExpertDuelist { get; } = Id("expert-duelist");
        public static CardDefinitionId SlipperyGround { get; } = Id("slippery-ground");
        public static CardDefinitionId MasterDuelist { get; } = Id("master-duelist");
        public static CardDefinitionId Deluge { get; } = Id("deluge");
        public static CardDefinitionId LiquidLeap { get; } = Id("liquid-leap");
        public static CardDefinitionId MagicalCurrent { get; } = Id("magical-current");
        public static CardDefinitionId ArcaneWhirlpool { get; } = Id("arcane-whirlpool");
        public static CardDefinitionId StrangerTide { get; } = Id("stranger-tide");
        public static CardDefinitionId EbbAndFlow { get; } = Id("ebb-and-flow");
        public static CardDefinitionId LivingTsunami { get; } = Id("living-tsunami");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as Wasp's are. Two of the six are this
        // file's own: a Defense printed as a card's primary, and a Movement printed as one.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        /// <summary>
        /// p.14's "Primary Defense Value" - a Defense action printed in a card's primary slot with a
        /// number on it. It is a distinct identity from <see cref="SecondaryDefense"/> because the two
        /// are different actions on different cards, and Arien's Duelist cards print only this one.
        /// </summary>
        public static ActionDefinitionId PrimaryDefense { get; } =
            new ActionDefinitionId("primary-defense");

        /// <summary>p.11's Movement as Primary Action, whose card text applies before the move.</summary>
        public static ActionDefinitionId PrimaryMovement { get; } =
            new ActionDefinitionId("primary-movement");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// p.6 step 11's starting hand: "the three Tier I cards and the two basic cards with no Tier".
        /// The Handicap card is already out of the box by then, so the Gold card here is Noble Blade -
        /// and printed p.6 illustrates these exact five together, so the list is read rather than
        /// derived.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            NobleBlade, SpellBreak, DangerousCurrent, AspiringDuelist, LiquidLeap
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and p.16's
        /// unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            RagingStream, RogueWave, ViolentTorrent, TidalBlast,
            ExpertDuelist, SlipperyGround, MasterDuelist, Deluge,
            MagicalCurrent, ArcaneWhirlpool, StrangerTide, EbbAndFlow,
            LivingTsunami
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content, which is why it is listed apart from
        /// both: a match that instantiated it would be dealing a card the page removed.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[]
        {
            CeremonialBlade
        });

        /// <summary>
        /// All nineteen faces, in the order <c>Rules/HERO_ARIEN.md</c>'s printed-value table lists them.
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions { get; } = Array.AsReadOnly(
            Profiles().Select(Build).ToArray());

        /// <summary>The eighteen definitions a match deals from - everything but the boxed Handicap card.</summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions { get; } = Array.AsReadOnly(
            AllDefinitions
                .Where(definition => !ReturnedToBoxAtSetup.Contains(definition.Id))
                .ToArray());

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Arien prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Arien's cards require a defined owner.", nameof(ownerId));
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
        // the milestone that owes that text its composition.
        // -----------------------------------------------------------------------------------------

        private static IEnumerable<ArienCardProfile> Profiles() => new[]
        {
            // The reminder text in parentheses is p.9's item I, printed on the face and transcribed
            // with it. "Another unit" never includes you - which is what the choose-target node's own
            // exclusion states, so the sentence and the declaration say one thing twice on purpose.
            new ArienCardProfile(
                NobleBlade, "Noble Blade", 11, 2, 1,
                ArienPrimaryKind.Attack, 4, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a unit adjacent to you. Before the attack: You may move another unit that is " +
                "adjacent to the target 1 space. ( \"Another unit\" never includes you. )",
                PrintedTextComposition.None),

            // p.20's Handicap card, deferred exactly as Wasp's Crackling Dagger is and by the same
            // 2026-08-21 user decision: p.6 returns it to the box and the six-player pitch never brings
            // it back. Its printed values and words ship here as inventory all the same, which is what
            // makes the deferral a decision about composition rather than a gap in the inventory.
            new ArienCardProfile(
                CeremonialBlade, "Ceremonial Blade", 10, 1, 1,
                ArienPrimaryKind.Attack, 3, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a hero adjacent to you. Before the attack: You may move a minion that is " +
                "adjacent to the target 1 space.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration")),

            // The face reads "except on the gold cards" where the rulebook's two illustrations of this
            // card drop the article and the capital. The face wins under the ledger's authority
            // hierarchy, and ledger question A-03 - answered by the user on 2026-08-23 - reads "the
            // gold cards" as the Gold colour, not p.15's Basic supertype. Its live consequence is that
            // this card shuts down Wasp's Static Barrier, whose Basic Skill is on Silver.
            new ArienCardProfile(
                SpellBreak, "Spell Break", 13, 3, 0,
                ArienPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "This turn: Enemy heroes in radius cannot perform Skill actions, except on the gold " +
                "cards. ( Skill is an action type. Other action types are unaffected. )",
                PrintedTextComposition.None),

            new ArienCardProfile(
                DangerousCurrent, "Dangerous Current", 8, 6, 4,
                ArienPrimaryKind.Attack, 6, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    RagingStream, RogueWave),
                "Target a unit adjacent to you. Before the attack: Up to 1 enemy hero in any of the 2 " +
                "spaces in a straight line directly behind the target discards a card, or is defeated.",
                PrintedTextComposition.None),

            new ArienCardProfile(
                RagingStream, "Raging Stream", 8, 7, 4,
                ArienPrimaryKind.Attack, 7, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    ViolentTorrent, TidalBlast),
                "Target a unit adjacent to you. Before the attack: Up to 1 enemy hero in any of the 3 " +
                "spaces in a straight line directly behind the target discards a card, or is defeated.",
                PrintedTextComposition.None),

            new ArienCardProfile(
                RogueWave, "Rogue Wave", 8, 4, 4,
                ArienPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    ViolentTorrent, TidalBlast),
                "Target a unit in range. After the attack: You may push an enemy unit adjacent to you " +
                "up to 2 spaces.",
                PrintedTextComposition.None),

            // "May repeat once on a different unit" where Wasp's Thunder Boomerang prints "different
            // target". Each face's own noun is transcribed; what the repeat excludes is declared once,
            // in the effect content, and is the same exclusion either noun names.
            new ArienCardProfile(
                ViolentTorrent, "Violent Torrent", 9, 7, 4,
                ArienPrimaryKind.Attack, 7, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Initiative),
                "Target a unit adjacent to you. Before the attack: Up to 1 enemy hero in any of the 5 " +
                "spaces in a straight line directly behind the target discards a card, or is defeated. " +
                "May repeat once on a different unit.",
                PrintedTextComposition.None),

            new ArienCardProfile(
                TidalBlast, "Tidal Blast", 9, 4, 4,
                ArienPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Movement),
                "Target a unit in range. After the attack: You may push an enemy unit adjacent to you " +
                "up to 3 spaces.",
                PrintedTextComposition.None),

            // The face's reminder text is p.9's item I: the suppression reaches both sides, which is
            // what MinionDefenseContributionPolicy.SuppressAll states and why this card carries no
            // effect root at all - its whole sentence is that declaration.
            new ArienCardProfile(
                AspiringDuelist, "Aspiring Duelist", 9, 0, 3,
                ArienPrimaryKind.PrimaryDefense, 5, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    ExpertDuelist, SlipperyGround),
                "Ignore all minion defense modifiers. ( This includes your minions, as well as the " +
                "enemy minions. )",
                PrintedTextComposition.None),

            new ArienCardProfile(
                ExpertDuelist, "Expert Duelist", 10, 0, 3,
                ArienPrimaryKind.PrimaryDefense, 6, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Initiative,
                    MasterDuelist, Deluge),
                "Ignore all minion defense modifiers. This turn: You are immune to attack actions of " +
                "all enemy heroes, except this attacker.",
                PrintedTextComposition.None),

            new ArienCardProfile(
                SlipperyGround, "Slippery Ground", 10, 6, 0,
                ArienPrimaryKind.Movement, 3, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    MasterDuelist, Deluge),
                "This turn: Enemy heroes adjacent to you cannot fast travel, or move more than 1 space " +
                "with a movement action.",
                PrintedTextComposition.None),

            // The one card in the pitch inventory whose Active effect outlives its turn. p.19 defines
            // both keywords, and the only difference between this card and Expert Duelist is which of
            // the two its sentence prints.
            new ArienCardProfile(
                MasterDuelist, "Master Duelist", 10, 0, 3,
                ArienPrimaryKind.PrimaryDefense, 6, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Range),
                "Ignore all minion defense modifiers. This round: You are immune to attack actions of " +
                "all enemy heroes, except this attacker.",
                PrintedTextComposition.None),

            // Radius 1 is p.9's smallest disk and reaches exactly the spaces Slippery Ground's
            // "adjacent to you" reaches. The Tier III card buys scaling rather than reach: p.16 raises
            // a printed icon and never a word in card text, so an item can widen this disk and can
            // never widen Slippery Ground's.
            new ArienCardProfile(
                Deluge, "Deluge", 10, 6, 0,
                ArienPrimaryKind.Movement, 3, PrintedValueChannel.Radius, 1,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Attack),
                "This turn: Enemy heroes in radius cannot fast travel, or move more than 1 space with " +
                "a movement action.",
                PrintedTextComposition.None),

            new ArienCardProfile(
                LiquidLeap, "Liquid Leap", 4, 3, 2,
                ArienPrimaryKind.Skill, 0, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    MagicalCurrent, ArcaneWhirlpool),
                "Place yourself into a space in range without a spawn point and not adjacent to an " +
                "empty spawn point.",
                PrintedTextComposition.None),

            new ArienCardProfile(
                MagicalCurrent, "Magical Current", 4, 3, 2,
                ArienPrimaryKind.Skill, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    StrangerTide, EbbAndFlow),
                "Place yourself into a space in range without a spawn point and not adjacent to an " +
                "empty spawn point.",
                PrintedTextComposition.None),

            new ArienCardProfile(
                ArcaneWhirlpool, "Arcane Whirlpool", 4, 3, 2,
                ArienPrimaryKind.Skill, 0, PrintedValueChannel.Range, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Defense,
                    StrangerTide, EbbAndFlow),
                "Swap with an enemy minion in range. ( Swap places with the target. This is not " +
                "movement. )",
                PrintedTextComposition.None),

            // A strict superset of Liquid Leap and Magical Current: the same Place with one clause
            // dropped. The rulebook's printed p.12 worked example illustrates exactly what the longer
            // clause bars, which is what makes the shorter one the upgrade.
            new ArienCardProfile(
                StrangerTide, "Stranger Tide", 3, 4, 2,
                ArienPrimaryKind.Skill, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Radius),
                "Place yourself into a space in range without a spawn point.",
                PrintedTextComposition.None),

            // The repeat condition is printed in the past tense - "if it was adjacent to you" - which
            // is what makes it readable at all: the Swap has already moved the minion into Arien's
            // former space by the time the condition is asked.
            new ArienCardProfile(
                EbbAndFlow, "Ebb and Flow", 3, 4, 2,
                ArienPrimaryKind.Skill, 0, PrintedValueChannel.Range, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Defense),
                "Swap with an enemy minion in range; if it was adjacent to you, may repeat once.",
                PrintedTextComposition.None),

            // p.16's Ultimate. It prints no Initiative and no action at all - the card is placed beside
            // the dashboard and its ability "is always in effect" - and, unlike Wasp's High Voltage, it
            // prints neither Range nor Radius, so its whole printed content is the permanent ability.
            new ArienCardProfile(
                LivingTsunami, "Living Tsunami", 0, 0, 0,
                ArienPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "Once per turn, before performing an Attack action, you may move 1 space.",
                PrintedTextComposition.None)
        };

        private static CardDefinition Build(ArienCardProfile profile)
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
                        "living-tsunami-before-attack",
                        "ArienLivingTsunami",
                        ArienEffectContent.LivingTsunamiTriggeredEffect(ContentRevision).Reference,
                        new BeforeActionTrigger(
                            ActionKind.Attack, BeforeActionTriggerFrequency.OncePerTurn),
                        PrintedIcon(profile))
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));
        }

        private static IEnumerable<ActionDefinition> BuildActions(ArienCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the shard's Sec Mov or Sec Def column means the face prints no such icon, so
            // that secondary action is unavailable from this card. It is expressed by the action not
            // existing rather than by a zero value, exactly as Wasp's set expresses it. Arien is the
            // first hero where the dash falls on both columns across the set: his three Duelist cards
            // print no secondary Defense and his two Movement primaries print no secondary Movement,
            // because in each case the icon has been promoted into the primary slot.
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

        private static ActionDefinition BuildPrimary(ArienCardProfile profile)
        {
            switch (profile.Primary)
            {
                case ArienPrimaryKind.None:
                    return null;

                case ArienPrimaryKind.Attack:
                    return new ActionDefinition(
                        PrimaryAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        ArienEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        ArienEffectContent.AttackTextTimingFor(profile.Id));

                case ArienPrimaryKind.RangedAttack:
                    // Both Ranged Attacks print "Target a unit in range", which is p.13's own sentence
                    // for a Ranged Attack word for word, so neither declares a target policy of its
                    // own and neither narrows anything.
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        ArienEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        ArienEffectContent.AttackTextTimingFor(profile.Id));

                case ArienPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        ArienEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                case ArienPrimaryKind.PrimaryDefense:
                    // p.14's "Primary Defense Value", which that page illustrates beside the secondary
                    // one. It costs and resolves exactly as every other Defense does - discarded from
                    // hand, in response to an attack, compared against the Attack Total - and what is
                    // new is where the number is printed and that the card's text therefore applies.
                    //
                    // "Ignore all minion defense modifiers" is the whole of one card's text and half of
                    // the other two's, and it is declared rather than composed: it is not a step that
                    // happens, it is a statement about which contributions the Defense Total takes, so
                    // an effect node describing it would be describing the same rule in a second
                    // vocabulary. All three Duelist cards print it, which is why the policy is on the
                    // action rather than on the row.
                    return new ActionDefinition(
                        PrimaryDefense,
                        ActionKind.Defense,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, profile.PrimaryValue) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                        ArienEffectContent.DefenseTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.Value,
                        ArienEffectContent.DefenseTextTimingFor(profile.Id),
                        null,
                        null,
                        MinionDefenseContributionPolicy.SuppressAll);

                case ArienPrimaryKind.Movement:
                    // p.11's Movement as Primary Action: "apply the card text in addition to moving up
                    // to the number of spaces on the icon", and "Unless it says otherwise, the card
                    // text comes into effect before movement". Neither card says otherwise, so both
                    // take the page's default rather than asking the actor to place the text.
                    return new ActionDefinition(
                        PrimaryMovement,
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        ArienEffectContent.MovementTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        CardTextTiming.BeforeTheMovement);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Arien primary action kind.");
            }
        }

        /// <summary>
        /// p.9's one Range or Radius icon, or nothing when the face prints neither. Nine of Arien's
        /// faces print neither, and the absence is content.
        ///
        /// <para>The shard's Range/Radius column and its item-icon column are read separately here
        /// because they are printed separately there, and Master Duelist is the card that shows why:
        /// its item icon is Range and its Range/Radius column is a dash. p.16's item icon is what the
        /// upgrade pair leaves behind, and a reader who conflated the two would give this card a
        /// printed Range it does not have.</para>
        ///
        /// <para>The absence is keyed on the channel alone. A row that named a channel and a value of
        /// zero would be a typing mistake, and it fails loudly inside
        /// <see cref="ActionDefinition"/> - which refuses a non-positive icon - rather than quietly
        /// dropping an icon the face prints.</para>
        /// </summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(ArienCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum ArienPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,
            Attack = 1,
            RangedAttack = 2,
            Skill = 3,

            /// <summary>p.14's Primary Defense Value: a Defense printed in the card's primary slot.</summary>
            PrimaryDefense = 4,

            /// <summary>p.11's Movement as Primary Action.</summary>
            Movement = 5
        }

        private sealed class ArienCardProfile
        {
            public ArienCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                ArienPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition)
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
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public ArienPrimaryKind Primary { get; }

            /// <summary>
            /// The number printed on the primary action's own icon - Attack, Defense or Movement,
            /// whichever the primary is. One field rather than three, because p.9 prints one primary
            /// icon per card and a definition with two of them would be a card that does not exist.
            /// </summary>
            public int PrimaryValue { get; }

            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }
        }
    }
}
