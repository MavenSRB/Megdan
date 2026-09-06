using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ursafar the Savage's nineteen printed faces as definition content - milestone 8.35z31,
    /// and the seventh <see cref="CardContentAuthority.VerifiedOfficial"/> hero this project has built.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at the Ursafar inventory pass and recorded in
    /// the <c>Docs/AI/Rules/HERO_URSAFAR.md</c> shard this milestone's ceremony writes. The tables there
    /// and the rows here are meant to be read side by side: the order, the names and the column meanings
    /// match, so a reviewer can audit one against the other without holding anything in their head.
    /// Nothing is derived, for the reason <see cref="WaspCardContent"/> records.</para>
    ///
    /// <para><b>He is a Devoted Hero pack hero, and the pack has no second source.</b> The
    /// second-printing rulebook in this repository names Wasp, Xargatha, Arien, Brogan and Dodger and
    /// returns zero hits for <c>Ursafar</c>, for all nineteen of his card names, and for <c>enrage</c> in
    /// any form. The pack-wide source ruling makes the Tabletopia extraction sole authority for these
    /// faces, so every string below is what our scan prints and nothing is reconciled against a printing
    /// we do not hold. Two live printing questions are recorded in the ceremony's ask-the-designer queue
    /// rather than answered here - see <see cref="AngryRoar"/>.</para>
    ///
    /// <para><b>What is different from the six sets already built, and therefore what this file is the
    /// first to state.</b> All five of his Blue cards print a <b>Movement</b> primary, which p.9's
    /// <c>L Primary Action Type</c> makes them Movement cards - the first primary Movement icons in the
    /// verified roster, and the first faces to print card text on the far side of the walk. Fourteen of
    /// his nineteen faces end with the p.17/p.19 Active-effect keyword <c>This round: You are enraged.</c>
    /// and all nineteen read that state, which is the whole hero; <c>enraged</c> is defined by these
    /// faces alone. Three faces print the borrow clause - <c>perform the primary action on one of
    /// your ... cards</c> - which starts a whole action instance off a card that is not the one
    /// committed this turn. And Tear is the roster's first face to change what rulebook p.13's Defeat
    /// costs in Life counters.</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file, and no member of the content model it builds, names an image, an
    /// art asset or a file path. A definition is its values, its words and its relationships.</para>
    /// </summary>
    public static class UrsafarCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, with the same coupling
        /// <see cref="TigerclawCardContent.ContentRevision"/> has: it moves when Ursafar's content does.
        /// </summary>
        public const string ContentRevision = "ursafar-8-35x-ursafar1-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself.
        /// </summary>
        public const string ContentSetId = "ursafar";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        public static CardDefinitionId ClawsThatCatch { get; } = Id("claws-that-catch");
        public static CardDefinitionId JawsThatBite { get; } = Id("jaws-that-bite");

        /// <summary>
        /// His only basic Skill, and the face two recorded printing questions attach to.
        ///
        /// <para>The designer channel's Ursafar thread records (a) that a newer printing of this card
        /// adds the help text <c>(You may choose to perform a replacement action instead)</c>, and (b) a
        /// reply describing the card as starting with <c>you may</c>. Our scan prints neither: it reads
        /// <c>If enraged, perform the primary action on one of your active cards with an active
        /// effect.</c> and nothing else before the keyword line. The pack-wide source ruling makes the
        /// extraction sole authority, so this file composes what the scan prints; both deltas are
        /// consolidated printing-currency questions for the designer and neither is settled in code.
        /// The outcome is unchanged either way for the case that matters - the general conditional
        /// ruling already lets a false <c>If enraged</c> skip its clause and the grant still land.</para>
        /// </summary>
        public static CardDefinitionId AngryRoar { get; } = Id("angry-roar");

        public static CardDefinitionId PreyDrive { get; } = Id("prey-drive");
        public static CardDefinitionId PreyAbundance { get; } = Id("prey-abundance");
        public static CardDefinitionId Rip { get; } = Id("rip");
        public static CardDefinitionId FeedingFrenzy { get; } = Id("feeding-frenzy");
        public static CardDefinitionId Tear { get; } = Id("tear");
        public static CardDefinitionId ProwlingBrute { get; } = Id("prowling-brute");
        public static CardDefinitionId RampagingBeast { get; } = Id("rampaging-beast");
        public static CardDefinitionId ColdIre { get; } = Id("cold-ire");
        public static CardDefinitionId UnstoppableForce { get; } = Id("unstoppable-force");
        public static CardDefinitionId EyesOfFlame { get; } = Id("eyes-of-flame");
        public static CardDefinitionId SniffOut { get; } = Id("sniff-out");
        public static CardDefinitionId EyesOnThePrey { get; } = Id("eyes-on-the-prey");
        public static CardDefinitionId InstinctiveReaction { get; } = Id("instinctive-reaction");
        public static CardDefinitionId ApexPredator { get; } = Id("apex-predator");
        public static CardDefinitionId EvolutionaryResponse { get; } = Id("evolutionary-response");
        public static CardDefinitionId UnboundFury { get; } = Id("unbound-fury");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as the six earlier sets' are. Ursafar needs
        // five, and one of them is new to the roster: he is the first hero printing a Movement primary.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryAttack { get; } = new ActionDefinitionId("primary-attack");

        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");

        /// <summary>
        /// p.9's <c>L Primary Action Type</c> applied to his Blue line: five faces print a MOVEMENT band
        /// with a boot icon and a value, so the card's type is Movement and the icon is the primary's
        /// rather than a secondary. Consistently with that, no Blue face prints a secondary Movement
        /// icon - the same promotion Arien's two Movement primaries and Wasp's Green Blocks show.
        /// </summary>
        public static ActionDefinitionId PrimaryMovement { get; } =
            new ActionDefinitionId("primary-movement");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// p.6 step 11's starting hand: "the three Tier I cards and the two basic cards with no Tier".
        /// The Handicap card is already out of the box by then, so the Gold card here is Claws That
        /// Catch.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            ClawsThatCatch, AngryRoar, PreyDrive, ProwlingBrute, SniffOut
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and
        /// p.16's unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            PreyAbundance, Rip, FeedingFrenzy, Tear,
            RampagingBeast, ColdIre, UnstoppableForce, EyesOfFlame,
            EyesOnThePrey, InstinctiveReaction, ApexPredator, EvolutionaryResponse,
            UnboundFury
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content, which is why it is listed apart
        /// from both.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[]
        {
            JawsThatBite
        });

        /// <summary>
        /// The fourteen faces that print <c>This round: You are enraged.</c> as their last line - both
        /// Gold cards, the Silver, all five Red and all five Blue. The five Green faces read the state
        /// and never grant it, and the Ultimate replaces the mechanism with a permanent one.
        ///
        /// <para>It is a list on this class rather than a flag on the row because it is the set's
        /// organising fact and three separate readers ask about it: the Active-effect content builds one
        /// definition per member (p.10 makes an Active effect unique per <em>card</em>, so fourteen
        /// coexist), the shard's table is audited against it, and the borrow contract's own tests use it
        /// to prove that a borrowed primary carries the borrowed card's grant rather than the borrowing
        /// card's.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> GrantsEnraged { get; } = Array.AsReadOnly(new[]
        {
            ClawsThatCatch, JawsThatBite, AngryRoar,
            PreyDrive, PreyAbundance, Rip, FeedingFrenzy, Tear,
            ProwlingBrute, RampagingBeast, ColdIre, UnstoppableForce, EyesOfFlame
        });

        /// <summary>
        /// All nineteen faces, in the order <c>Rules/HERO_URSAFAR.md</c>'s printed-value table lists
        /// them.
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
            throw new ArgumentException($"Ursafar prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Ursafar's cards require a defined owner.", nameof(ownerId));
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

        private static IEnumerable<UrsafarCardProfile> Profiles() => new[]
        {
            // The Gold pair. Both print a conditional step-1 move that ends by naming the unit the
            // Attack takes, so p.14 does not ask the ordinary target question first - the same
            // move-then-target shape Brogan's charges print, read in the order the face prints it.
            new UrsafarCardProfile(
                ClawsThatCatch, "Claws That Catch", 12, 2, 1,
                UrsafarPrimaryKind.Attack, 4, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Before the attack: If enraged, you may move 1 space to a space adjacent to an enemy " +
                "hero; target a unit adjacent to you. This round: You are enraged.",
                PrintedTextComposition.None,
                bindsTargetInCardText: true),

            // p.20's Handicap card, deferred exactly as Wasp's Crackling Dagger, Arien's Ceremonial
            // Blade, Brogan's Leap Forward, Sabina's Bang and Tigerclaw's Nimble Strike are, by the same
            // 2026-08-21 user decision: p.6 returns it to the box and the six-player pitch never brings
            // it back. Its printed values and words ship here as inventory all the same. It differs from
            // Claws That Catch in exactly one word - the target class narrows from a unit to a hero -
            // alongside the ordinary Handicap value reduction.
            new UrsafarCardProfile(
                JawsThatBite, "Jaws That Bite", 11, 1, 1,
                UrsafarPrimaryKind.Attack, 3, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Before the attack: If enraged, you may move 1 space to a space adjacent to an enemy " +
                "hero; target a hero adjacent to you. This round: You are enraged.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration"),
                targetableUnits: AttackTargetableUnits.HeroesOnly,
                bindsTargetInCardText: true),

            // The Silver, and the hero's signature. It is the roster's first Skill whose whole printed
            // effect is to perform some OTHER card's primary action. It prints no Range or Radius icon
            // and no secondary Movement, which makes it the first verified Silver basic with neither -
            // both of the other two print a Radius - so its complete offer set is the primary, the
            // Defense 4 secondary, and Hold.
            new UrsafarCardProfile(
                AngryRoar, "Angry Roar", 7, 4, 0,
                UrsafarPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "If enraged, perform the primary action on one of your active cards with an active " +
                "effect. This round: You are enraged.",
                PrintedTextComposition.None),

            // The Red line. Every one of them targets "a unit adjacent to you" - he has no ranged Attack
            // anywhere in the set, which is what his printed Melee role rests on - and every one carries
            // an After-the-attack rider under the same condition.
            new UrsafarCardProfile(
                PreyDrive, "Prey Drive", 8, 7, 3,
                UrsafarPrimaryKind.Attack, 5, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    PreyAbundance, Rip),
                "Target a unit adjacent to you. After the attack: If enraged, and the target was not " +
                "removed, remove up to 1 enemy minion in radius. This round: You are enraged.",
                PrintedTextComposition.None),

            // Prey Abundance prints Prey Drive's text word for word and differs only in Initiative,
            // Attack, printed Radius and item icon - the one-definition-two-faces shape Wasp's
            // Shock/Electrocute pair has.
            new UrsafarCardProfile(
                PreyAbundance, "Prey Abundance", 9, 7, 3,
                UrsafarPrimaryKind.Attack, 6, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    FeedingFrenzy, Tear),
                "Target a unit adjacent to you. After the attack: If enraged, and the target was not " +
                "removed, remove up to 1 enemy minion in radius. This round: You are enraged.",
                PrintedTextComposition.None),

            new UrsafarCardProfile(
                Rip, "Rip", 9, 6, 3,
                UrsafarPrimaryKind.Attack, 5, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    FeedingFrenzy, Tear),
                "Target a unit adjacent to you. After the attack: If enraged, gain 1 coin. This round: " +
                "You are enraged.",
                PrintedTextComposition.None),

            new UrsafarCardProfile(
                FeedingFrenzy, "Feeding Frenzy", 9, 8, 3,
                UrsafarPrimaryKind.Attack, 6, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Defense),
                "Target a unit adjacent to you. After the attack: If enraged, and the target was not " +
                "removed, remove up to 2 enemy minions in radius. This round: You are enraged.",
                PrintedTextComposition.None),

            // The roster's first face to change what p.13's Defeat costs. The extra counter is not a
            // second payment: the cost is computed once as the defeated hero's lowest card Tier plus
            // one and spent once, at the defeat, with the After-the-attack placement deciding when the
            // condition is CHECKED rather than when the counter is paid.
            new UrsafarCardProfile(
                Tear, "Tear", 9, 7, 3,
                UrsafarPrimaryKind.Attack, 5, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Initiative),
                "Target a unit adjacent to you. After the attack: If enraged, gain 2 coins; if you " +
                "defeated a hero, that hero spends 1 additional [Life] Life counter. This round: You " +
                "are enraged.",
                PrintedTextComposition.None),

            // The Blue line - the verified roster's first primary Movement actions. Three of them print
            // a swap on the far side of the walk; two of them raise the walk's own allowance instead,
            // and those two are exactly the faces printing p.13/p.14's `+` cue on a Movement icon.
            new UrsafarCardProfile(
                ProwlingBrute, "Prowling Brute", 9, 5, 0,
                UrsafarPrimaryKind.Movement, 1, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    RampagingBeast, ColdIre),
                "If enraged, after movement, you may swap with a unit or a token adjacent to you. This " +
                "round: You are enraged.",
                PrintedTextComposition.None),

            new UrsafarCardProfile(
                RampagingBeast, "Rampaging Beast", 10, 6, 0,
                UrsafarPrimaryKind.Movement, 1, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Defense,
                    UnstoppableForce, EyesOfFlame),
                "If enraged, after movement, you may swap with a unit or a token adjacent to you; if " +
                "you do, move up to 1 additional space. This round: You are enraged.",
                PrintedTextComposition.None),

            // The printed `1+` is p.13/p.14's plus cue extended to a Movement icon: the printed value IS
            // 1, and it is the card's own text that raises it. The semicolon after the first clause is
            // what this face prints where Eyes of Flame prints a full stop; the transcription keeps
            // both, as Wasp's shard keeps Thunder Boomerang's missing comma.
            new UrsafarCardProfile(
                ColdIre, "Cold Ire", 10, 6, 0,
                UrsafarPrimaryKind.Movement, 1, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    UnstoppableForce, EyesOfFlame),
                "If enraged, gain +1 [Movement] Movement; This round: You are enraged.",
                PrintedTextComposition.None,
                raisedMovement: 1),

            new UrsafarCardProfile(
                UnstoppableForce, "Unstoppable Force", 10, 6, 0,
                UrsafarPrimaryKind.Movement, 1, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Range),
                "If enraged, after movement, you may swap with a unit or a token adjacent to you; if " +
                "you do, move up to 2 additional spaces. This round: You are enraged.",
                PrintedTextComposition.None),

            new UrsafarCardProfile(
                EyesOfFlame, "Eyes of Flame", 10, 6, 0,
                UrsafarPrimaryKind.Movement, 1, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Radius),
                "If enraged, gain +2 [Movement] Movement. This round: You are enraged.",
                PrintedTextComposition.None,
                raisedMovement: 2),

            // The Green line - the five faces that READ the enraged state and never grant it. Three of
            // them force a discard at Range; the Tier-escalation contrast between `if able` and `or is
            // defeated` reproduces exactly what Wasp's Red line shows.
            new UrsafarCardProfile(
                SniffOut, "Sniff Out", 4, 2, 2,
                UrsafarPrimaryKind.Skill, 0, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    EyesOnThePrey, InstinctiveReaction),
                "If enraged, an enemy hero in range discards a card, if able.",
                PrintedTextComposition.None),

            new UrsafarCardProfile(
                EyesOnThePrey, "Eyes on the Prey", 3, 3, 2,
                UrsafarPrimaryKind.Skill, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    ApexPredator, EvolutionaryResponse),
                "If enraged, an enemy hero in range discards a card, if able.",
                PrintedTextComposition.None),

            // The second and third borrow faces, over the discard pile rather than the dashboard.
            // Instinctive Reaction prints p.10's plain modal; Evolutionary Response prints the roster's
            // first `choose one, or both`, which p.10's Modal Actions already licenses.
            new UrsafarCardProfile(
                InstinctiveReaction, "Instinctive Reaction", 3, 3, 2,
                UrsafarPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Initiative,
                    ApexPredator, EvolutionaryResponse),
                "If enraged, choose one - • Perform the primary action on one of your discarded cards. " +
                "• You may retrieve a discarded card.",
                PrintedTextComposition.None),

            new UrsafarCardProfile(
                ApexPredator, "Apex Predator", 3, 3, 2,
                UrsafarPrimaryKind.Skill, 0, PrintedValueChannel.Range, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Attack),
                "If enraged, an enemy hero in range discards a card, or is defeated.",
                PrintedTextComposition.None),

            new UrsafarCardProfile(
                EvolutionaryResponse, "Evolutionary Response", 3, 3, 2,
                UrsafarPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Movement),
                "If enraged, choose one, or both - • Perform the primary action on one of your " +
                "discarded cards. • You may retrieve a discarded card.",
                PrintedTextComposition.None),

            // p.16's permanent ability. It prints no Initiative, no icons and no actions at all, which
            // is what makes the row's zeros a statement rather than an omission. Its two clauses do two
            // different things: the first makes the enraged predicate always true for him, and the
            // second widens what Angry Roar can reach - and both qualifiers on that card's pool keep
            // doing work afterwards, which is why the sentence is worth its Tier.
            new UrsafarCardProfile(
                UnboundFury, "Unbound Fury", 0, 0, 0,
                UrsafarPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "You are always enraged and all your resolved cards count as active.",
                PrintedTextComposition.None)
        };

        private static CardDefinition Build(UrsafarCardProfile profile) =>
            new CardDefinition(
                profile.Id,
                ContentRevision,
                profile.DisplayName,
                profile.Primary == UrsafarPrimaryKind.None
                    ? default
                    : new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                profile.Progression,
                BuildActions(profile),
                // p.16's permanent ability as a standing declaration rather than a modification. Neither
                // of Unbound Fury's clauses changes a value, a permission or an object type: the first
                // asserts the named condition his own faces test, and the second widens which of his
                // cards a borrow may reach. Both are read off the rule kind this names, which is what
                // the unlocked card supplies as a continuous source for exactly as long as p.16 says.
                profile.Id.Equals(UnboundFury)
                    ? UltimateRuleDefinition.PermanentDeclaration(
                        UrsafarEffectContent.UnboundFuryRuleId,
                        UrsafarEffectContent.UnboundFuryRuleKind)
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));

        private static IEnumerable<ActionDefinition> BuildActions(UrsafarCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the shard's Sec Mov or Sec Def column means the face prints no such icon, so
            // that secondary action is unavailable from this card - expressed by the action not
            // existing rather than by a zero value, exactly as the six earlier sets express it. His
            // dashes are the five Blue faces' Movement column - where the icon is the primary's - Angry
            // Roar's Movement column, and the Ultimate's whole row. No face of his omits the Defense
            // secondary except the Ultimate, so he can always discard to defend.
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

        private static ActionDefinition BuildPrimary(UrsafarCardProfile profile)
        {
            switch (profile.Primary)
            {
                case UrsafarPrimaryKind.None:
                    return null;

                case UrsafarPrimaryKind.Attack:
                    return new ActionDefinition(
                        PrimaryAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        UrsafarEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        UrsafarEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile),
                        Enumerable.Empty<RosterValueContributionDefinition>(),
                        MinionDefenseContributionPolicy.IncludeAll,
                        DefenseOfferOriginRestriction.None,
                        PrintedBlockCondition.None,
                        UrsafarEffectContent.SecondAttackTextFor(profile.Id, ContentRevision),
                        UrsafarEffectContent.SecondAttackTextTimingFor(profile.Id));

                case UrsafarPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        UrsafarEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                case UrsafarPrimaryKind.Movement:
                    // p.11's Movement as Primary Action: "apply the card text in addition to moving up
                    // to the number of spaces on the icon". Which side of the walk the text lands on is
                    // the card's own word here rather than the page's default: Cold Ire and Eyes of
                    // Flame raise the allowance and so must be read before it is spent, while the three
                    // swap faces print "after movement" in so many words, which is the one thing p.11's
                    // "unless it says otherwise" is about.
                    return new ActionDefinition(
                        PrimaryMovement,
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        UrsafarEffectContent.MovementTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        UrsafarEffectContent.MovementTextTimingFor(profile.Id));

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Ursafar primary action kind.");
            }
        }

        /// <summary>
        /// What one printed Attack of Ursafar's may target, or <c>null</c> where the face narrows
        /// nothing and p.13's own reading for the subtype stands.
        ///
        /// <para>One of his seven Attacks states something p.13 does not: Jaws That Bite prints "a
        /// hero", which is the narrowing <see cref="AttackTargetableUnits.HeroesOnly"/> names. The rest
        /// print "a unit", which is p.13's own word.</para>
        ///
        /// <para>Both Gold faces name their own target - "target a unit/hero adjacent to you", reached
        /// after the step-1 move their text may make - so p.14 does not ask the ordinary target question
        /// first. That is the card-text binding milestone 8.19a minted for Brogan's charges. Jaws That
        /// Bite is excluded from it because p.6 returns it to the box and a deferred face composes no
        /// step-1 text for the binding to be about; its target binding ships with its composition,
        /// exactly as its nodes do.</para>
        /// </summary>
        private static PrintedAttackTargetPolicy? TargetPolicy(UrsafarCardProfile profile)
        {
            var classification = PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged);
            if (profile.TargetableUnits != AttackTargetableUnits.HeroesAndMinions)
            {
                classification = new PrintedAttackTargetPolicy(
                    profile.TargetableUnits, classification.Reach, classification.Line);
            }

            if (profile.BindsTargetInCardText && !profile.TextComposition.IsAwaited)
            {
                return classification.BoundByCardText();
            }

            return profile.TargetableUnits == AttackTargetableUnits.HeroesAndMinions
                ? (PrintedAttackTargetPolicy?)null
                : classification;
        }

        /// <summary>The p.9 Range or Radius icon a face prints, or nothing where it prints neither.</summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(UrsafarCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum UrsafarPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,
            Attack = 1,
            Skill = 2,

            /// <summary>
            /// p.9's <c>L Primary Action Type</c> on a Blue face: the Movement icon is the primary's,
            /// and the card is a Movement card. The verified roster's first.
            /// </summary>
            Movement = 3
        }

        private sealed class UrsafarCardProfile
        {
            public UrsafarCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                UrsafarPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition,
                AttackTargetableUnits targetableUnits = AttackTargetableUnits.HeroesAndMinions,
                bool bindsTargetInCardText = false,
                int raisedMovement = 0)
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
                BindsTargetInCardText = bindsTargetInCardText;
                RaisedMovement = raisedMovement;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public UrsafarPrimaryKind Primary { get; }
            public int PrimaryValue { get; }
            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }
            public AttackTargetableUnits TargetableUnits { get; }

            /// <summary>Whether this Attack's own step-1 text names the unit it attacks.</summary>
            public bool BindsTargetInCardText { get; }

            /// <summary>
            /// The amount this face's own text adds to its printed Movement value when the condition
            /// holds - the two faces printing the `1+` cue, and zero everywhere else.
            ///
            /// <para>It is carried on the row because the shard's value table carries it, and it is read
            /// by <see cref="UrsafarEffectContent"/> when it composes the raise. It is deliberately NOT
            /// a printed-value shape of its own: the printed value is 1, and the `+` is the p.13/p.14
            /// cue saying the card's text can raise it.</para>
            /// </summary>
            public int RaisedMovement { get; }
        }
    }
}
