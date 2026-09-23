using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Xargatha the Changed's nineteen printed faces as definition content - milestone 8.24, and the
    /// sixth <see cref="CardContentAuthority.VerifiedOfficial"/> hero this project has built.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at milestone 8.18e and recorded in
    /// <c>Docs/AI/Rules/HERO_XARGATHA.md</c>. The tables there and the rows here are meant to be read
    /// side by side: the order, the names and the column meanings match, so a reviewer can audit one
    /// against the other without holding anything in their head. Nothing is derived, for the reason
    /// <see cref="WaspCardContent"/> records.</para>
    ///
    /// <para><b>What is different from the five sets already built, and therefore what this file is the
    /// first to state.</b> Three of her faces print a Movement primary whose text she places either
    /// side of it - "before or after movement" - the first verified content to say otherwise to p.11's
    /// default that "the card text comes into effect before movement";
    /// <see cref="ArienCardContent"/>'s two Movement primaries take that default and say so. Five faces
    /// carry the printed <c>+</c> as a value that scales with a live count rather than as a fixed
    /// conditional bonus, and two of those scale Range rather than Attack. And she is the first hero
    /// whose Ultimate is a standing declaration rather than a trigger: Metamorphosis states three rules
    /// that are simply true while she is on the board.</para>
    ///
    /// <para><b>What her set does not print, which is content too.</b> She prints no Defense primary
    /// and no p.14 Block: every Red card is an Attack, every Blue card is a Skill, and the Green branch
    /// splits three Movement primaries from two Skills. She owns no rules token and no marker - none of
    /// her faces names, places, gives or targets one - so this milestone adds no component content at
    /// all, and <c>Rules/HERO_XARGATHA.md</c> records that absence as an answer rather than a gap.</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file, and no member of the content model it builds, names an image, an
    /// art asset or a file path. A definition is its values, its words and its relationships.</para>
    /// </summary>
    public static class XargathaCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, with the same coupling
        /// <see cref="TigerclawCardContent.ContentRevision"/> has: it moves when Xargatha's content
        /// does.
        /// </summary>
        public const string ContentRevision = "xargatha-8-83-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself.
        /// </summary>
        public const string ContentSetId = "xargatha";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        public static CardDefinitionId Cleave { get; } = Id("cleave");
        public static CardDefinitionId DesperateCleave { get; } = Id("desperate-cleave");
        public static CardDefinitionId SirensCall { get; } = Id("sirens-call");
        public static CardDefinitionId DangerousSlash { get; } = Id("dangerous-slash");
        public static CardDefinitionId DeadlySwipe { get; } = Id("deadly-swipe");
        public static CardDefinitionId LongThrust { get; } = Id("long-thrust");
        public static CardDefinitionId LethalSpin { get; } = Id("lethal-spin");
        public static CardDefinitionId RapidThrusts { get; } = Id("rapid-thrusts");
        public static CardDefinitionId StoneGaze { get; } = Id("stone-gaze");
        public static CardDefinitionId PetrifyingStare { get; } = Id("petrifying-stare");
        public static CardDefinitionId FreshConverts { get; } = Id("fresh-converts");
        public static CardDefinitionId TurnIntoStatues { get; } = Id("turn-into-statues");
        public static CardDefinitionId DevotedFollowers { get; } = Id("devoted-followers");
        public static CardDefinitionId Charm { get; } = Id("charm");
        public static CardDefinitionId Control { get; } = Id("control");
        public static CardDefinitionId Constrict { get; } = Id("constrict");
        public static CardDefinitionId Dominate { get; } = Id("dominate");
        public static CardDefinitionId FinalEmbrace { get; } = Id("final-embrace");
        public static CardDefinitionId Metamorphosis { get; } = Id("metamorphosis");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as the five earlier sets' are. Xargatha
        // needs five: she prints no Defense primary, so there is no Block identity here, and her three
        // Green Movement cards are the reason a primary Movement identity is.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        /// <summary>
        /// p.11's Movement as a primary action, which Charm, Control and Dominate print. Its identity
        /// is separate from the ordinary secondary Movement below because the two are different actions
        /// on different cards: no face of hers carries both, and only this one applies card text.
        /// </summary>
        public static ActionDefinitionId PrimaryMovement { get; } =
            new ActionDefinitionId("primary-movement");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// p.6 step 11's starting hand: "the three Tier I cards and the two basic cards with no Tier".
        /// The Handicap card is already out of the box by then, so the Gold card here is Cleave.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            Cleave, SirensCall, DangerousSlash, StoneGaze, Charm
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and
        /// p.16's unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            DeadlySwipe, LongThrust, LethalSpin, RapidThrusts,
            PetrifyingStare, FreshConverts, TurnIntoStatues, DevotedFollowers,
            Control, Constrict, Dominate, FinalEmbrace,
            Metamorphosis
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content, which is why it is listed apart
        /// from both.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[]
        {
            DesperateCleave
        });

        /// <summary>
        /// All nineteen faces, in the order <c>Rules/HERO_XARGATHA.md</c>'s printed-value table lists
        /// them.
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
            throw new ArgumentException($"Xargatha prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Xargatha's cards require a defined owner.", nameof(ownerId));
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

        private static IEnumerable<XargathaCardProfile> Profiles() => new[]
        {
            // Her Gold basic, and the roster's first Attack whose repeat narrows the class of unit it
            // may take: the printed instance targets "a unit", the repeated one "a different enemy
            // hero". The parenthesis is p.9 reminder text and states the consequence rather than a
            // rule - it is why the first target being a minion does not spend the repeat.
            new XargathaCardProfile(
                Cleave, "Cleave", 11, 2, 1,
                XargathaPrimaryKind.Attack, 4, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a unit adjacent to you. After the attack: May repeat once on a different " +
                "enemy hero. ( You may repeat even if the original target was a minion. )",
                PrintedTextComposition.None),

            // p.20's Handicap card, deferred exactly as Wasp's Crackling Dagger, Arien's Ceremonial
            // Blade, Brogan's Leap Forward, Sabina's Bang and Tigerclaw's Nimble Strike are, by the
            // same 2026-08-21 user decision: p.6 returns it to the box and the six-player pitch never
            // brings it back. Its printed values and words ship here as inventory all the same. What
            // it says that Cleave does not is on the row rather than in the deferral: it targets a
            // hero where the ordinary Gold card targets any adjacent unit.
            new XargathaCardProfile(
                DesperateCleave, "Desperate Cleave", 10, 1, 1,
                XargathaPrimaryKind.Attack, 3, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a hero adjacent to you. After the attack: May repeat once on a different " +
                "enemy hero.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration"),
                targetableUnits: AttackTargetableUnits.HeroesOnly),

            // Her Silver basic and her only Ranged Skill. The lower-right 3 belongs to the primary's
            // printed Range; the left-ribbon shield is Defense 3, and the face prints no Movement.
            // Its text moves the target TO her rather than away, which is why the movement clause is
            // bounded by a destination rather than by a direction.
            new XargathaCardProfile(
                SirensCall, "Siren's Call", 3, 3, 0,
                XargathaPrimaryKind.Skill, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "Target an enemy unit not adjacent to you and in range; if able, move the target up " +
                "to 3 spaces to a space adjacent to you.",
                PrintedTextComposition.None),

            // The Red line. Three melee Attacks scaling with the enemies around her and two Ranged
            // Attacks scaling their Range the same way; what separates the two families is which
            // channel the count feeds and whether the target is excluded from it.
            new XargathaCardProfile(
                DangerousSlash, "Dangerous Slash", 7, 6, 5,
                XargathaPrimaryKind.Attack, 5, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    DeadlySwipe, LongThrust),
                "Target a unit adjacent to you. +1 [Attack] Attack for each other enemy unit " +
                "adjacent to you. ( Do not count the target when calculating the attack bonus. )",
                PrintedTextComposition.None,
                scaledChannel: PrintedValueChannel.Attack,
                scaledAmount: 1),

            new XargathaCardProfile(
                DeadlySwipe, "Deadly Swipe", 8, 7, 5,
                XargathaPrimaryKind.Attack, 5, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    LethalSpin, RapidThrusts),
                "Target a unit adjacent to you. +2 [Attack] Attack for each other enemy unit " +
                "adjacent to you.",
                PrintedTextComposition.None,
                scaledChannel: PrintedValueChannel.Attack,
                scaledAmount: 2),

            new XargathaCardProfile(
                LongThrust, "Long Thrust", 8, 4, 4,
                XargathaPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 1,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    LethalSpin, RapidThrusts),
                "Target a unit in range; +1 [Range] Range for each enemy unit adjacent to you.",
                PrintedTextComposition.None,
                scaledChannel: PrintedValueChannel.Range,
                scaledAmount: 1),

            new XargathaCardProfile(
                LethalSpin, "Lethal Spin", 8, 7, 5,
                XargathaPrimaryKind.Attack, 5, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Radius),
                "Target a unit adjacent to you. +3 [Attack] Attack for each other enemy unit " +
                "adjacent to you.",
                PrintedTextComposition.None,
                scaledChannel: PrintedValueChannel.Attack,
                scaledAmount: 3),

            // The one face carrying both halves at once: a Range that scales with the enemies beside
            // her and a repeat that recalculates it. p.10 reapplies the same Range rule to the
            // separate repeated instance, so an adjacent minion the first attack removed is a space of
            // Range the second one no longer has.
            new XargathaCardProfile(
                RapidThrusts, "Rapid Thrusts", 8, 4, 4,
                XargathaPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 1,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Movement),
                "Target a unit in range; +1 [Range] Range for each enemy unit adjacent to you. May " +
                "repeat once on a different enemy hero.",
                PrintedTextComposition.None,
                scaledChannel: PrintedValueChannel.Range,
                scaledAmount: 1),

            // The Blue line's three petrification cards. One printed sentence, two declarations: the
            // affected heroes keep every hero rule and gain terrain as a second effective type, and
            // they cannot perform a Movement action at all. p.9 makes the disk the printed icon's, so
            // p.16's Radius item widens it, and p.17 moves it with her.
            new XargathaCardProfile(
                StoneGaze, "Stone Gaze", 9, 5, 3,
                XargathaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    PetrifyingStare, FreshConverts),
                "Next turn: Enemy heroes in radius count as both heroes and terrain, and cannot " +
                "perform movement actions. ( If you move, the radius “moves” with you. )",
                PrintedTextComposition.None),

            new XargathaCardProfile(
                PetrifyingStare, "Petrifying Stare", 10, 6, 3,
                XargathaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Defense,
                    TurnIntoStatues, DevotedFollowers),
                "Next turn: Enemy heroes in radius count as both heroes and terrain, and cannot " +
                "perform movement actions.",
                PrintedTextComposition.None),

            // The two retrieval cards. Each is one presence question and one optional retrieve, and
            // the difference between them is only which enemy pieces answer the question.
            new XargathaCardProfile(
                FreshConverts, "Fresh Converts", 10, 6, 3,
                XargathaPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    TurnIntoStatues, DevotedFollowers),
                "If you are adjacent to an enemy minion, you may retrieve a discarded card.",
                PrintedTextComposition.None),

            new XargathaCardProfile(
                TurnIntoStatues, "Turn Into Statues", 10, 6, 3,
                XargathaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Defense),
                "Next turn: Enemy heroes in radius count as both heroes and terrain, and cannot " +
                "perform movement actions.",
                PrintedTextComposition.None),

            new XargathaCardProfile(
                DevotedFollowers, "Devoted Followers", 10, 6, 3,
                XargathaPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Range),
                "If you are adjacent to an enemy unit, you may retrieve a discarded card.",
                PrintedTextComposition.None),

            // The Green Movement primaries - the first verified faces to place their own text either
            // side of the action rather than taking p.11's before-movement default. What separates the
            // three is which enemy minions the radius offers and, for Dominate, that a Heavy minion's
            // immunity does not stop the move.
            new XargathaCardProfile(
                Charm, "Charm", 5, 3, 0,
                XargathaPrimaryKind.Movement, 2, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    Control, Constrict),
                "You may move an enemy ranged minion in radius up to 2 spaces before or after " +
                "movement.",
                PrintedTextComposition.None),

            new XargathaCardProfile(
                Control, "Control", 4, 3, 0,
                XargathaPrimaryKind.Movement, 2, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    Dominate, FinalEmbrace),
                "You may move an enemy ranged or melee minion in radius up to 2 spaces before or " +
                "after movement.",
                PrintedTextComposition.None),

            // The two end-of-round Defeats. Constrict's parenthesis states their timing plainly -
            // before p.8's Minion Battle - and p.9 makes reminder text clarification rather than a new
            // rule, so Final Embrace's identical keyword has the same timing with the reminder absent.
            new XargathaCardProfile(
                Constrict, "Constrict", 4, 3, 2,
                XargathaPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Initiative,
                    Dominate, FinalEmbrace),
                "End of round: Defeat an enemy melee minion adjacent to you. ( Before the end of " +
                "round minion battle. )",
                PrintedTextComposition.None),

            new XargathaCardProfile(
                Dominate, "Dominate", 4, 4, 0,
                XargathaPrimaryKind.Movement, 2, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Attack),
                "You may move an enemy minion in radius up to 2 spaces before or after movement; " +
                "ignore heavy minion's immunity.",
                PrintedTextComposition.None),

            new XargathaCardProfile(
                FinalEmbrace, "Final Embrace", 4, 4, 2,
                XargathaPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Initiative),
                "End of round: Defeat an enemy melee or ranged minion adjacent to you.",
                PrintedTextComposition.None),

            // p.16's permanent ability, and the first verified Ultimate that is a standing declaration
            // rather than a trigger. It prints no Initiative, no icons and no actions at all, which is
            // what makes the row's zeros a statement rather than an omission.
            new XargathaCardProfile(
                Metamorphosis, "Metamorphosis", 0, 0, 0,
                XargathaPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "Gain +1 [Movement] Movement and +1 [Initiative] Initiative for each enemy unit " +
                "adjacent to you. You may move through obstacles.",
                PrintedTextComposition.None)
        };

        private static CardDefinition Build(XargathaCardProfile profile) =>
            new CardDefinition(
                profile.Id,
                ContentRevision,
                profile.DisplayName,
                profile.Primary == XargathaPrimaryKind.None
                    ? default
                    : new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                profile.Progression,
                BuildActions(profile),
                profile.Id.Equals(Metamorphosis)
                    ? UltimateRuleDefinition.PermanentAbility(
                        XargathaEffectContent.MetamorphosisRuleId,
                        "XargathaMetamorphosis",
                        XargathaEffectContent.MetamorphosisModifications)
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));

        private static IEnumerable<ActionDefinition> BuildActions(XargathaCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the shard's Sec Mov or Sec Def column means the face prints no such icon, so
            // that secondary action is unavailable from this card - expressed by the action not
            // existing rather than by a zero value, exactly as the five earlier sets express it. Her
            // dashes are Siren's Call's Movement column, the three Green Movement primaries' Movement
            // column - where the icon is the primary's - and the Ultimate's whole row.
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

        private static ActionDefinition BuildPrimary(XargathaCardProfile profile)
        {
            switch (profile.Primary)
            {
                case XargathaPrimaryKind.None:
                    return null;

                case XargathaPrimaryKind.Attack:
                    return new ActionDefinition(
                        PrimaryAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        XargathaEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        XargathaEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.NonRanged),
                        ScaledValues(profile),
                        MinionDefenseContributionPolicy.IncludeAll);

                case XargathaPrimaryKind.RangedAttack:
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        XargathaEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        XargathaEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.Ranged),
                        ScaledValues(profile),
                        MinionDefenseContributionPolicy.IncludeAll);

                case XargathaPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        XargathaEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                case XargathaPrimaryKind.Movement:
                    // p.11's Movement as Primary Action: "apply the card text in addition to moving up
                    // to the number of spaces on the icon". The page's default is that the text comes
                    // into effect before movement "unless it says otherwise", and all three of these
                    // faces say otherwise in as many words - "before or after movement" - so the
                    // timing is the actor's rather than the page's.
                    return new ActionDefinition(
                        PrimaryMovement,
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        XargathaEffectContent.MovementTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        CardTextTiming.AroundTheAction);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Xargatha primary action kind.");
            }
        }

        /// <summary>
        /// The printed <c>+</c> on five of her faces, as the typed declaration that makes it true - or
        /// nothing on the fourteen that print a plain number or none at all.
        ///
        /// <para><b>Why it is a value contribution rather than an effect node.</b> It is not a step:
        /// nobody chooses anything, nothing happens on the board, and the clause is read when the value
        /// is computed. That is the same reading <see cref="TigerclawCardContent"/> records for his
        /// conditional <c>+2</c>; what is different here is that the amount is per matching unit rather
        /// than per satisfied condition, so the count is the live board's and only the amount is the
        /// card's.</para>
        ///
        /// <para><b>Two queries, and the word that separates them is "other".</b> Dangerous Slash,
        /// Deadly Swipe and Lethal Spin say "each <em>other</em> enemy unit adjacent to you", and
        /// Dangerous Slash's parenthesis spells out the consequence - the target is not counted. Long
        /// Thrust and Rapid Thrusts say "each enemy unit adjacent to you" with no such word, so an
        /// adjacent target counts itself. p.18 makes immunity irrelevant to a presence question, so an
        /// immune Heavy minion contributes to both.</para>
        /// </summary>
        private static IEnumerable<RosterValueContributionDefinition> ScaledValues(
            XargathaCardProfile profile) =>
            profile.ScaledChannel == PrintedValueChannel.None
                ? Enumerable.Empty<RosterValueContributionDefinition>()
                : new[]
                {
                    new RosterValueContributionDefinition(
                        profile.ScaledChannel,
                        profile.ScaledChannel == PrintedValueChannel.Attack
                            ? RosterValueQueryKind.EnemyUnitsAdjacentToActorExcludingTarget
                            : RosterValueQueryKind.EnemyUnitsAdjacentToActor,
                        profile.ScaledAmount)
                };

        /// <summary>
        /// What one printed Attack of Xargatha's may target, or <c>null</c> where the face narrows
        /// nothing and p.13's own reading for the subtype stands.
        ///
        /// <para>One of her seven Attacks states something p.13 does not: Desperate Cleave prints "a
        /// hero", which is the narrowing <see cref="AttackTargetableUnits.HeroesOnly"/> names. The rest
        /// print "a unit", which is p.13's own word - including Cleave, whose repeated instance is
        /// narrowed by its own repeat declaration rather than by the printed target.</para>
        /// </summary>
        private static PrintedAttackTargetPolicy? TargetPolicy(
            XargathaCardProfile profile, AttackRangeKind attackRange)
        {
            if (profile.TargetableUnits == AttackTargetableUnits.HeroesAndMinions)
            {
                return null;
            }

            var classification = PrintedAttackTargetPolicy.ForClassification(attackRange);
            return new PrintedAttackTargetPolicy(
                profile.TargetableUnits, classification.Reach, classification.Line);
        }

        /// <summary>The p.9 Range or Radius icon a face prints, or nothing where it prints neither.</summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(XargathaCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum XargathaPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,
            Attack = 1,
            RangedAttack = 2,
            Skill = 3,

            /// <summary>p.11's Movement printed as the card's primary action.</summary>
            Movement = 4
        }

        private sealed class XargathaCardProfile
        {
            public XargathaCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                XargathaPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition,
                AttackTargetableUnits targetableUnits = AttackTargetableUnits.HeroesAndMinions,
                PrintedValueChannel scaledChannel = PrintedValueChannel.None,
                int scaledAmount = 0)
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
                TargetableUnits = targetableUnits;
                ScaledChannel = scaledChannel;
                ScaledAmount = scaledAmount;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public XargathaPrimaryKind Primary { get; }
            public int PrimaryValue { get; }
            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }
            public AttackTargetableUnits TargetableUnits { get; }

            /// <summary>The channel this face's printed `+` raises, or None on the fourteen without one.</summary>
            public PrintedValueChannel ScaledChannel { get; }

            /// <summary>What each matching enemy unit adds to that channel.</summary>
            public int ScaledAmount { get; }
        }
    }
}
