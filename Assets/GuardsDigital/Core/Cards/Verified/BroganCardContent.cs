using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brogan the Destroyer's nineteen printed faces as definition content - milestone 8.21, and the
    /// third <see cref="CardContentAuthority.VerifiedOfficial"/> hero this project has built.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at milestone 8.18b and recorded in
    /// <c>Docs/AI/Rules/HERO_BROGAN.md</c>. The tables there and the rows here are meant to be read
    /// side by side: the order, the names and the column meanings match, so a reviewer can audit one
    /// against the other without holding anything in their head. Nothing is derived, for the reason
    /// <see cref="WaspCardContent"/> records.</para>
    ///
    /// <para><b>What is different from the two sets already built, and therefore what this file is the
    /// first to state.</b> Brogan prints no Defense primary, no Block and no combined Defense/Skill
    /// card, so no text of his ever applies while defending - his eighteen non-Ultimate faces all
    /// carry a secondary Defense value and none of them carries a word beside it. Two of his Attacks
    /// print rulebook p.10's modal <c>Choose one —</c>, which no implemented face had used. Three more
    /// name their own target from their "Before the attack:" text rather than choosing one when the
    /// action starts, which is <see cref="AttackTargetBinding.ByCardText"/> and what milestone 8.19a
    /// built it for. And One Man Army is the first Ultimate whose whole printed content is a rule
    /// about rulebook p.8's Minion Battle rather than an effect that runs.</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file, and no member of the content model it builds, names an image, an
    /// art asset or a file path. A definition is its values, its words and its relationships.</para>
    /// </summary>
    public static class BroganCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, with the same coupling
        /// <see cref="ArienCardContent.ContentRevision"/> has: it moves when Brogan's content changes.
        /// </summary>
        public const string ContentRevision = "brogan-8-21-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself, and the identity his prepared token is addressed by.
        /// </summary>
        public const string ContentSetId = "brogan";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        public static CardDefinitionId Onslaught { get; } = Id("onslaught");
        public static CardDefinitionId LeapForward { get; } = Id("leap-forward");
        public static CardDefinitionId Bulwark { get; } = Id("bulwark");
        public static CardDefinitionId MadDash { get; } = Id("mad-dash");
        public static CardDefinitionId Bullrush { get; } = Id("bullrush");
        public static CardDefinitionId ThrowingAxe { get; } = Id("throwing-axe");
        public static CardDefinitionId FuriousCharge { get; } = Id("furious-charge");
        public static CardDefinitionId ThrowingSpear { get; } = Id("throwing-spear");
        public static CardDefinitionId BrutalJab { get; } = Id("brutal-jab");
        public static CardDefinitionId MightyPunch { get; } = Id("mighty-punch");
        public static CardDefinitionId ShieldBash { get; } = Id("shield-bash");
        public static CardDefinitionId SavageKick { get; } = Id("savage-kick");
        public static CardDefinitionId Counterattack { get; } = Id("counterattack");
        public static CardDefinitionId Shield { get; } = Id("shield");
        public static CardDefinitionId Bolster { get; } = Id("bolster");
        public static CardDefinitionId WarDrummer { get; } = Id("war-drummer");
        public static CardDefinitionId Fortify { get; } = Id("fortify");
        public static CardDefinitionId MasterSkald { get; } = Id("master-skald");
        public static CardDefinitionId OneManArmy { get; } = Id("one-man-army");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as Wasp's and Arien's are. Brogan needs
        // four of the seven the two earlier sets between them declare: he prints no Defense primary
        // and no Movement primary, so those two slots never carry one of his icons.
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
        /// The Handicap card is already out of the box by then, so the Gold card here is Onslaught.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            Onslaught, Bulwark, MadDash, BrutalJab, Shield
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and
        /// p.16's unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            Bullrush, ThrowingAxe, FuriousCharge, ThrowingSpear,
            MightyPunch, ShieldBash, SavageKick, Counterattack,
            Bolster, WarDrummer, Fortify, MasterSkald,
            OneManArmy
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content, which is why it is listed apart
        /// from both.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[]
        {
            LeapForward
        });

        /// <summary>
        /// All nineteen faces, in the order <c>Rules/HERO_BROGAN.md</c>'s printed-value table lists
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
            throw new ArgumentException($"Brogan prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Brogan's cards require a defined owner.", nameof(ownerId));
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

        private static IEnumerable<BroganCardProfile> Profiles() => new[]
        {
            // "After the attack:" is p.14's step 7, which runs after step 6 has already removed the
            // defeated unit - and that is the whole reason "the space it occupied" is a space anyone
            // can move into. The card names no unit type, so the target is p.13's default pair.
            new BroganCardProfile(
                Onslaught, "Onslaught", 11, 3, 1,
                BroganPrimaryKind.Attack, 4, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a unit adjacent to you. After the attack: Move into the space it occupied.",
                PrintedTextComposition.None),

            // p.20's Handicap card, deferred exactly as Wasp's Crackling Dagger and Arien's Ceremonial
            // Blade are, by the same 2026-08-21 user decision: p.6 returns it to the box and the
            // six-player pitch never brings it back. Its printed values and words ship here as
            // inventory all the same, which is what makes the deferral a decision about composition
            // rather than a gap in the inventory. "Target a hero adjacent to you" is the one thing it
            // says that Onslaught does not, and it is stated on the row rather than in the deferral.
            new BroganCardProfile(
                LeapForward, "Leap Forward", 10, 3, 1,
                BroganPrimaryKind.Attack, 3, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a hero adjacent to you. After the attack: Move into the space it occupied.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration"),
                targetableUnits: AttackTargetableUnits.HeroesOnly),

            // Two unrelated clauses under one primary: p.10's retrieve, then p.17's keyword. Radius 4
            // is the largest disk in the verified roster so far.
            new BroganCardProfile(
                Bulwark, "Bulwark", 12, 4, 0,
                BroganPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "You may retrieve a discarded card. This turn: You and friendly units in radius " +
                "cannot be moved, pushed, swapped or placed by enemy heroes.",
                PrintedTextComposition.None),

            // The three charges name their own target: the unit the move ends beside, which nothing
            // could have chosen before the move. The parenthesised reminder is p.9's item I and only
            // this one of the three prints it; p.10's mandatory-step rule reaches the same result for
            // all three, which is why the sentence changes nothing about the composition.
            new BroganCardProfile(
                MadDash, "Mad Dash", 7, 7, 3,
                BroganPrimaryKind.Attack, 6, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    Bullrush, ThrowingAxe),
                "Before the attack: Move 2 spaces in a straight line to a space adjacent to an enemy " +
                "unit, then target that unit. ( If you cannot make this move, you cannot attack. )",
                PrintedTextComposition.None,
                targetBoundByCardText: true),

            new BroganCardProfile(
                Bullrush, "Bullrush", 7, 8, 3,
                BroganPrimaryKind.Attack, 6, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    FuriousCharge, ThrowingSpear),
                "Before the attack: Move 2 or 3 spaces in a straight line to a space adjacent to an " +
                "enemy unit, then target that unit.",
                PrintedTextComposition.None,
                targetBoundByCardText: true),

            // p.10's modal form, which no implemented face had printed. The bullets are the targeting
            // sentence itself, so the target is bound by the card text on this card too - and the
            // second bullet's discard is what buys the printed Range.
            new BroganCardProfile(
                ThrowingAxe, "Throwing Axe", 7, 7, 4,
                BroganPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    FuriousCharge, ThrowingSpear),
                "Choose one — • Target a unit adjacent to you. • You may discard a card; if you do, " +
                "target a unit in range.",
                PrintedTextComposition.None,
                targetBoundByCardText: true),

            new BroganCardProfile(
                FuriousCharge, "Furious Charge", 8, 8, 3,
                BroganPrimaryKind.Attack, 7, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Movement),
                "Before the attack: Move 2, 3, or 4 spaces in a straight line to a space adjacent to " +
                "an enemy unit, then target that unit.",
                PrintedTextComposition.None,
                targetBoundByCardText: true),

            // The Tier III modal differs from the Tier II one in who pays: a card already sitting in
            // the discard enables the range bullet, so the discard this card offers is itself
            // optional. The transcription keeps each face's own punctuation.
            new BroganCardProfile(
                ThrowingSpear, "Throwing Spear", 8, 7, 4,
                BroganPrimaryKind.RangedAttack, 4, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Radius),
                "Choose one — • Target a unit adjacent to you. • You may discard a card. If you have " +
                "a card in the discard, target a unit in range.",
                PrintedTextComposition.None,
                targetBoundByCardText: true),

            // p.18's Push preceded by an optional step of Brogan's own. The three cards differ in
            // whether the move is offered or measured and in how far each half reaches, and in
            // nothing else.
            new BroganCardProfile(
                BrutalJab, "Brutal Jab", 8, 5, 2,
                BroganPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    MightyPunch, ShieldBash),
                "You may move 1 space. Push an enemy unit or a token adjacent to you up to 1 space.",
                PrintedTextComposition.None),

            new BroganCardProfile(
                MightyPunch, "Mighty Punch", 9, 6, 2,
                BroganPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Initiative,
                    SavageKick, Counterattack),
                "Move up to 1 space. Push an enemy unit, or a token, adjacent to you up to 2 spaces.",
                PrintedTextComposition.None),

            new BroganCardProfile(
                ShieldBash, "Shield Bash", 9, 6, 2,
                BroganPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    SavageKick, Counterattack),
                "An enemy hero adjacent to you, who has played an attack card this turn discards a " +
                "card, if able.",
                PrintedTextComposition.None),

            new BroganCardProfile(
                SavageKick, "Savage Kick", 9, 6, 2,
                BroganPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Initiative),
                "Move up to 2 spaces. Push an enemy unit or a token adjacent to you up to 2 spaces.",
                PrintedTextComposition.None),

            // The third verified printing of ledger question 13's "discards a card, or is defeated",
            // after Wasp's Electroblast and Arien's three Red Attacks - one mandatory step with two
            // ways to complete, and the only difference from Shield Bash is which of the two the
            // face prints as the second.
            new BroganCardProfile(
                Counterattack, "Counterattack", 9, 6, 2,
                BroganPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Range),
                "An enemy hero adjacent to you who has played an attack card this turn discards a " +
                "card, or is defeated.",
                PrintedTextComposition.None),

            // The Green trio prints the colour-versus-supertype distinction on the cards themselves:
            // two say "a silver card" and the Tier III one says "a basic card", which under the
            // 2026-08-23 ruling recorded as Arien's A-03 is a genuine upgrade. The reminder text on
            // Shield is p.9's item I and the rulebook's own p.12 example says the same thing.
            new BroganCardProfile(
                Shield, "Shield", 6, 4, 2,
                BroganPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 1,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    Bolster, WarDrummer),
                "This round: When any friendly minion in radius is defeated you may discard a silver " +
                "card. If you do, the minion is not removed. ( The enemy hero still gains the coins " +
                "for defeating the minion. )",
                PrintedTextComposition.None),

            new BroganCardProfile(
                Bolster, "Bolster", 5, 5, 2,
                BroganPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    Fortify, MasterSkald),
                "This round: When any friendly minion in radius is defeated, you may discard a silver " +
                "card; if you do, the minion is not removed.",
                PrintedTextComposition.None),

            // The first verified cards that grant coins, and the first that condition on a
            // round-scoped history rather than on board state.
            new BroganCardProfile(
                WarDrummer, "War Drummer", 5, 5, 2,
                BroganPrimaryKind.Skill, 0, PrintedValueChannel.Range, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Defense,
                    Fortify, MasterSkald),
                "A friendly hero in range gains 1 coin; if any hero was defeated this round, that " +
                "friendly hero gains 3 coins, instead.",
                PrintedTextComposition.None),

            new BroganCardProfile(
                Fortify, "Fortify", 5, 5, 2,
                BroganPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Attack),
                "This round: When any friendly minion in radius is defeated you may discard a basic " +
                "card. If you do, the minion is not removed.",
                PrintedTextComposition.None),

            new BroganCardProfile(
                MasterSkald, "Master Skald", 5, 5, 2,
                BroganPrimaryKind.Skill, 0, PrintedValueChannel.Range, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Defense),
                "A friendly hero in range gains 2 coins; if any hero was defeated this round, that " +
                "friendly hero gains 4 coins instead.",
                PrintedTextComposition.None),

            // p.16's Ultimate. It prints no Initiative and no action, and - unlike Wasp's High
            // Voltage and like Arien's Living Tsunami - neither Range nor Radius, so its whole
            // printed content is the permanent ability. Both of its sentences are about p.8's Minion
            // Battle, which is what makes it a declared rule rather than an effect that runs.
            new BroganCardProfile(
                OneManArmy, "One Man Army", 0, 0, 0,
                BroganPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "During minion battle you count as a heavy minion; if you would be removed, lose the " +
                "push instead.",
                PrintedTextComposition.None)
        };

        private static CardDefinition Build(BroganCardProfile profile)
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
                    ? UltimateRuleDefinition.MinionBattleAbility(
                        "one-man-army-minion-battle",
                        "BroganOneManArmy",
                        // Both printed sentences, as the one declaration the battle reads. "you count
                        // as a heavy minion" is the strength his side counts him at, and "if you
                        // would be removed, lose the push instead" is the substitution - the user's
                        // 2026-08-23 decision on B-02, which reads the substituted push as p.8's
                        // ordinary Lane Push run in full. Neither sentence is a hero-name branch: the
                        // handler reads this declaration and nothing else.
                        new MinionBattleRuleDeclaration(
                            OneManArmyMinionBattleHandler.RuleReference,
                            MinionBattleStrength.HeavyMinion,
                            MinionBattleRemovalSubstitution.PushLost),
                        PrintedIcon(profile))
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));
        }

        private static IEnumerable<ActionDefinition> BuildActions(BroganCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the shard's Sec Mov or Sec Def column means the face prints no such icon, so
            // that secondary action is unavailable from this card - expressed by the action not
            // existing rather than by a zero value, exactly as the two earlier sets express it.
            // Brogan's dashes fall in one place only: Bulwark prints no secondary Movement, and the
            // Ultimate prints no secondary anything.
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

        private static ActionDefinition BuildPrimary(BroganCardProfile profile)
        {
            switch (profile.Primary)
            {
                case BroganPrimaryKind.None:
                    return null;

                case BroganPrimaryKind.Attack:
                    return new ActionDefinition(
                        PrimaryAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        BroganEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        BroganEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.NonRanged));

                case BroganPrimaryKind.RangedAttack:
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        BroganEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        BroganEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, AttackRangeKind.Ranged));

                case BroganPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        BroganEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Brogan primary action kind.");
            }
        }

        /// <summary>
        /// What one printed Attack of Brogan's may target, or <c>null</c> where the face narrows
        /// nothing and p.13's own reading for the subtype stands.
        ///
        /// <para>Two of his faces state something p.13 does not. Leap Forward prints "Target a hero
        /// adjacent to you", which is the narrowing <see cref="AttackTargetableUnits.HeroesOnly"/>
        /// names. And five - the three charges and the two modal Ranged Attacks - name their target in
        /// their own text rather than choosing one when the action starts, which is
        /// <see cref="AttackTargetBinding.ByCardText"/>. Everything else about the reach is p.13's:
        /// adjacent for a non-Ranged Attack and the printed Range for a Ranged one, taken from
        /// <see cref="PrintedAttackTargetPolicy.ForClassification"/> rather than restated here.</para>
        /// </summary>
        private static PrintedAttackTargetPolicy? TargetPolicy(
            BroganCardProfile profile, AttackRangeKind attackRange)
        {
            if (profile.TargetableUnits == AttackTargetableUnits.HeroesAndMinions &&
                !profile.TargetBoundByCardText)
            {
                return null;
            }

            var classification = PrintedAttackTargetPolicy.ForClassification(attackRange);
            var policy = new PrintedAttackTargetPolicy(
                profile.TargetableUnits, classification.Reach, classification.Line);
            return profile.TargetBoundByCardText ? policy.BoundByCardText() : policy;
        }

        /// <summary>
        /// p.9's one Range or Radius icon, or nothing when the face prints neither. Nine of Brogan's
        /// faces print neither, and the absence is content.
        ///
        /// <para>The shard's Range/Radius column and its item-icon column are read separately here
        /// because they are printed separately there: Counterattack's item icon is Range while its
        /// Range/Radius column is a dash, and a reader who conflated the two would give that card a
        /// printed Range it does not have.</para>
        /// </summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(BroganCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum BroganPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,
            Attack = 1,
            RangedAttack = 2,
            Skill = 3
        }

        private sealed class BroganCardProfile
        {
            public BroganCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                BroganPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                PrintedTextComposition textComposition,
                AttackTargetableUnits targetableUnits = AttackTargetableUnits.HeroesAndMinions,
                bool targetBoundByCardText = false)
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
                TargetBoundByCardText = targetBoundByCardText;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public BroganPrimaryKind Primary { get; }

            /// <summary>
            /// The number printed on the primary action's own icon, which for Brogan is always an
            /// Attack value or nothing: he prints no Defense primary and no Movement primary.
            /// </summary>
            public int PrimaryValue { get; }

            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }

            /// <summary>Which units the face's own words let this Attack target.</summary>
            public AttackTargetableUnits TargetableUnits { get; }

            /// <summary>Whether this Attack's own text names its target rather than the action's start.</summary>
            public bool TargetBoundByCardText { get; }
        }
    }
}
