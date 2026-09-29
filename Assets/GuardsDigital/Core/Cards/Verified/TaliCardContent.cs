using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Tali the Spiritcaller's nineteen printed faces as definition content - milestone 8.35z32,
    /// and the ninth <see cref="CardContentAuthority.VerifiedOfficial"/> hero this project composed and the twelfth it deals.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at the Tali inventory pass and recorded in
    /// the <c>Docs/AI/Rules/HERO_TALI.md</c> shard this milestone's ceremony writes. The tables there
    /// and the rows here are meant to be read side by side: the order, the names and the column
    /// meanings match, so a reviewer can audit one against the other without holding anything in their
    /// head. Nothing is derived, for the reason <see cref="WaspCardContent"/> records.</para>
    ///
    /// <para><b>She is a Devoted Hero pack hero, and the pack has no second source.</b> The
    /// second-printing rulebook in this repository illustrates no Tali card and never names her:
    /// searched for the hero name and for all nineteen card names, zero hits for every one. RULING 27
    /// makes the Tabletopia extraction sole authority for these faces, so every string below is what
    /// our scan prints and nothing is reconciled against a printing we do not hold.</para>
    ///
    /// <para><b>What is different from the eight sets already built, and therefore what this file is
    /// the first to state.</b> She is the first verified hero who <b>uses tokens</b> - five faces place
    /// them, and <see cref="TaliComponentContent"/> is the content that says what they are. She is the
    /// first hero to print the rulebook's <c>+</c> value cue on an <b>Attack</b> value (p.13 and p.14
    /// illustrate it only on Range and Defense), and the first to print a condition a card asks about
    /// <em>itself</em> - eight of nineteen faces read their own discard state, which is what makes
    /// Commune with Spirits' borrow the hero rather than a fringe option. She prints
    /// <b>no reminder text on any face</b> and <b>no <c>This turn:</c> or <c>This round:</c> Active
    /// effect</b>; her one Active-effect keyword is Blizzard's <c>End of turn:</c>. And she is the
    /// first hero above one star on p.21's complexity scale - three stars, "Very Complex".</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file, and no member of the content model it builds, names an image, an
    /// art asset or a file path. A definition is its values, its words and its relationships.</para>
    /// </summary>
    public static class TaliCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, with the same coupling
        /// <see cref="UrsafarCardContent.ContentRevision"/> has: it moves when Tali's content does.
        /// </summary>
        public const string ContentRevision = "tali-9-6-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself.
        /// </summary>
        public const string ContentSetId = "tali";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        /// <summary>Her only basic Attack, and therefore the one action Reign of Winter reads.</summary>
        public static CardDefinitionId IceBlast { get; } = Id("ice-blast");

        /// <summary>p.20's Handicap card, returned to the box at setup.</summary>
        public static CardDefinitionId IceShard { get; } = Id("ice-shard");

        /// <summary>
        /// Her only basic Skill, and the hero's signature: an ally names a colour and she performs the
        /// primary action on a discarded card of it. The borrow contract minted for Ursafar's three
        /// faces is consumed here unchanged - RULING 22 was given about <em>this</em> card first.
        /// </summary>
        public static CardDefinitionId CommuneWithSpirits { get; } = Id("commune-with-spirits");

        public static CardDefinitionId WinterDagger { get; } = Id("winter-dagger");
        public static CardDefinitionId WinterSpear { get; } = Id("winter-spear");
        public static CardDefinitionId SpiritWolf { get; } = Id("spirit-wolf");
        public static CardDefinitionId WinterScepter { get; } = Id("winter-scepter");
        public static CardDefinitionId SpiritBear { get; } = Id("spirit-bear");
        public static CardDefinitionId ColdSnap { get; } = Id("cold-snap");
        public static CardDefinitionId Snowstorm { get; } = Id("snowstorm");
        public static CardDefinitionId GuardianSpirit { get; } = Id("guardian-spirit");
        public static CardDefinitionId Blizzard { get; } = Id("blizzard");
        public static CardDefinitionId WarriorSpirit { get; } = Id("warrior-spirit");
        public static CardDefinitionId GlacialBarrier { get; } = Id("glacial-barrier");
        public static CardDefinitionId WallOfFrost { get; } = Id("wall-of-frost");
        public static CardDefinitionId AncestralTotem { get; } = Id("ancestral-totem");
        public static CardDefinitionId PackIce { get; } = Id("pack-ice");
        public static CardDefinitionId VeneratedTotem { get; } = Id("venerated-totem");
        public static CardDefinitionId ReignOfWinter { get; } = Id("reign-of-winter");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as the eight earlier sets' are.
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
        /// The Handicap card is already out of the box by then, so the Gold card here is Ice Blast.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            IceBlast, CommuneWithSpirits, WinterDagger, ColdSnap, GlacialBarrier
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and
        /// p.16's unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            WinterSpear, SpiritWolf, Snowstorm, GuardianSpirit, WallOfFrost, AncestralTotem,
            WinterScepter, SpiritBear, Blizzard, WarriorSpirit, PackIce, VeneratedTotem,
            ReignOfWinter
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content, which is why it is listed apart
        /// from both.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } =
            Array.AsReadOnly(new[] { IceShard });

        /// <summary>
        /// The eight faces written to be read from the discard, which is the set's organising fact and
        /// the internal evidence that Commune with Spirits' borrow is the hero rather than a corner
        /// case: three Winter Attacks print "+3 Attack <b>if this card is not in the discard</b>", the
        /// two Spirit Attacks gate their first bullet on the same condition, and Guardian Spirit,
        /// Warrior Spirit and Blizzard each print a clause that only exists when the card <b>is</b> in
        /// the discard.
        ///
        /// <para>It is a list on this class rather than a flag on the row because three separate
        /// readers ask about it: the shard's table is audited against it, this milestone's tests use it
        /// to prove that a borrowed primary reads the <em>borrowed</em> card's lifecycle state, and
        /// <see cref="TaliEffectContent"/> reads it when it composes the self-referential
        /// conditions.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReadsItsOwnDiscardState { get; } =
            Array.AsReadOnly(new[]
            {
                WinterDagger, WinterSpear, WinterScepter,
                SpiritWolf, SpiritBear,
                GuardianSpirit, WarriorSpirit, Blizzard
            });

        /// <summary>
        /// The five faces that place a token, and therefore the five whose text
        /// <see cref="TaliComponentContent"/> supplies the placed piece for.
        ///
        /// <para>Three place Ice and two place a Totem, and under RULING 44 all five draw on
        /// <b>one physical supply of three</b>: they are the same token, and the placing card decides
        /// what each copy currently is. Under RULING 24 the two Totem faces never coexist in a deck,
        /// because p.15's upgrade replaces Tier II with Tier III.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> PlacesTokens { get; } = Array.AsReadOnly(new[]
        {
            GlacialBarrier, WallOfFrost, PackIce, AncestralTotem, VeneratedTotem
        });

        /// <summary>
        /// All nineteen faces, in the order <c>Rules/HERO_TALI.md</c>'s printed-value table lists them.
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
            throw new ArgumentException($"Tali prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Tali's cards require a defined owner.", nameof(ownerId));
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

        private static IEnumerable<TaliCardProfile> Profiles() => new[]
        {
            // The Gold pair. Both print an "After the attack:" rider conditioned on something that
            // happened at p.14 step 3 - the defender discarding to defend - which is the same
            // step-3-to-step-7 span Ice Blast's own inventory notes read off the page's numbered order.
            new TaliCardProfile(
                IceBlast, "Ice Blast", 11, 1, 1,
                TaliPrimaryKind.Attack, 3, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a unit adjacent to you. If the target discards a card to defend, After the " +
                "attack: Another enemy hero in radius discards a card of the same color, if able."),

            // p.20's Handicap card, deferred exactly as Wasp's Crackling Dagger, Arien's Ceremonial
            // Blade, Brogan's Leap Forward, Sabina's Bang, Tigerclaw's Nimble Strike, Ursafar's Jaws
            // That Bite and Silverarrow's Keep the Distance are, by the same 2026-08-21 user decision:
            // p.6 returns it to the box and the six-player pitch never brings it back. Its printed
            // values and words ship here as inventory all the same. It differs from Ice Blast in
            // exactly one word - the target class narrows from a unit to a hero - alongside the
            // ordinary Handicap value reduction on Initiative, Attack and Radius.
            new TaliCardProfile(
                IceShard, "Ice Shard", 10, 1, 1,
                TaliPrimaryKind.Attack, 2, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a hero adjacent to you. If the target discards a card to defend, After the " +
                "attack: Another enemy hero in radius discards a card of the same color, if able.",
                targetableUnits: AttackTargetableUnits.HeroesOnly),

            // The Silver, and the hero. It prints no Range and no Radius icon and no secondary
            // Movement, so its complete offer set is the primary, the Defense 2 secondary, and Hold.
            // The lone icon on its ribbon is a SHIELD sitting in the lower slot: the one-icon
            // slot-position trap, read by glyph shape rather than by position.
            new TaliCardProfile(
                CommuneWithSpirits, "Commune with Spirits", 13, 2, 0,
                TaliPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "A friendly hero in play names a color; you may discard a card of that color. " +
                "Perform the primary action on a card of that color in your discard."),

            // The Red line: two tracks the Level Up choice picks between. "Winter" is raw melee Attack
            // with the +3 discard-state cue; "Spirit" is a Ranged Attack with a modal.
            new TaliCardProfile(
                WinterDagger, "Winter Dagger", 8, 6, 4,
                TaliPrimaryKind.Attack, 2, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    WinterSpear, SpiritWolf),
                "Target a unit adjacent to you. +3 Attack if this card is not in the discard."),

            new TaliCardProfile(
                WinterSpear, "Winter Spear", 9, 6, 4,
                TaliPrimaryKind.Attack, 3, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    WinterScepter, SpiritBear),
                "Target a unit adjacent to you. +3 Attack if this card is not in the discard."),

            new TaliCardProfile(
                SpiritWolf, "Spirit Wolf", 9, 3, 4,
                TaliPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    WinterScepter, SpiritBear),
                "Choose one - • If this card is not in the discard, target a unit in range. " +
                "• Target a hero adjacent to you."),

            new TaliCardProfile(
                WinterScepter, "Winter Scepter", 9, 7, 4,
                TaliPrimaryKind.Attack, 4, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Radius),
                "Target a unit adjacent to you. +3 Attack if this card is not in the discard."),

            // The roster's second face to print "choose one, or both, on different targets", and the
            // constrained arm of the multi-sequence contract minted for Dodger's Middlefinger.
            new TaliCardProfile(
                SpiritBear, "Spirit Bear", 9, 4, 4,
                TaliPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Defense),
                "Choose one, or both, on different targets - • If this card is not in the discard, " +
                "target a unit in range. • Target a hero adjacent to you."),

            // The Blue line: enemy displacement against ally card-retrieval. The three displacement
            // faces print the SAME sentence and differ only in the printed Radius the selection reads
            // off the card - so Blizzard buys the repeat, not the radius.
            new TaliCardProfile(
                ColdSnap, "Cold Snap", 9, 5, 3,
                TaliPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    Snowstorm, GuardianSpirit),
                "Choose a direction. Move each enemy unit in radius 1 space in that direction, if able."),

            new TaliCardProfile(
                Snowstorm, "Snowstorm", 10, 5, 3,
                TaliPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Defense,
                    Blizzard, WarriorSpirit),
                "Choose a direction. Move each enemy unit in radius 1 space in that direction, if able."),

            new TaliCardProfile(
                GuardianSpirit, "Guardian Spirit", 10, 5, 3,
                TaliPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    Blizzard, WarriorSpirit),
                "Up to 1 friendly hero in radius may retrieve a discarded card. If Guardian Spirit is " +
                "in your discard, retrieve it."),

            new TaliCardProfile(
                Blizzard, "Blizzard", 10, 6, 3,
                TaliPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Movement),
                "Choose a direction. Move each enemy unit in radius 1 space in that direction, if " +
                "able. If Blizzard is in your discard, End of turn: May repeat once."),

            new TaliCardProfile(
                WarriorSpirit, "Warrior Spirit", 10, 6, 3,
                TaliPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Range),
                "Up to 1 friendly hero in radius may retrieve a discarded card. If Warrior Spirit is " +
                "in your discard, you may retrieve a discarded card."),

            // The Green line: Ice tokens against Totem tokens, and the only place in the verified
            // roster where a hero's own components are the card's whole content. All five draw on the
            // one supply of three (RULING 44), and the two Totem faces never coexist (RULING 24).
            new TaliCardProfile(
                GlacialBarrier, "Glacial Barrier", 4, 3, 2,
                TaliPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    WallOfFrost, AncestralTotem),
                "Place an Ice token in radius. Enemy heroes have -1 Initiative for every Ice token " +
                "they are adjacent to."),

            new TaliCardProfile(
                WallOfFrost, "Wall of Frost", 3, 3, 2,
                TaliPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Initiative,
                    PackIce, VeneratedTotem),
                "Place up to 2 Ice tokens in radius. Enemy heroes have -1 Initiative for every Ice " +
                "token they are adjacent to."),

            new TaliCardProfile(
                AncestralTotem, "Ancestral Totem", 3, 3, 2,
                TaliPrimaryKind.Skill, 0, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    PackIce, VeneratedTotem),
                "Place a Totem token in range. If a friendly melee minion adjacent to a totem would " +
                "be defeated, remove the totem instead."),

            new TaliCardProfile(
                PackIce, "Pack Ice", 3, 4, 2,
                TaliPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Initiative),
                "Place up to 3 Ice tokens in radius. Enemy heroes have -1 Initiative for every Ice " +
                "token they are adjacent to."),

            new TaliCardProfile(
                VeneratedTotem, "Venerated Totem", 3, 4, 2,
                TaliPrimaryKind.Skill, 0, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Attack),
                "Place a Totem token in range. If a friendly minion adjacent to a totem would be " +
                "defeated, remove the totem instead. Totem tokens are immune."),

            // p.16's permanent ability. It prints a Radius 4 icon and no Initiative, no secondary
            // icons and no action at all, which is what makes the row's other zeros a statement rather
            // than an omission. Ledger question 12's approved interpretation has an instance here: a
            // +1 Radius item makes this Radius 5.
            new TaliCardProfile(
                ReignOfWinter, "Reign of Winter", 0, 0, 0,
                TaliPrimaryKind.None, 0, PrintedValueChannel.Radius, 4,
                CardProgressionProfile.UltimateCard(),
                "Each time after you defeat a minion with your basic attack, an enemy hero in radius " +
                "discards a card of the color of your choice, if able.")
        };

        private static CardDefinition Build(TaliCardProfile profile) =>
            new CardDefinition(
                profile.Id,
                ContentRevision,
                profile.DisplayName,
                profile.Primary == TaliPrimaryKind.None
                    ? default
                    : new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                profile.Progression,
                BuildActions(profile),
                profile.Id.Equals(ReignOfWinter)
                    ? TaliEffectContent.ReignOfWinterRule(ContentRevision)
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));

        private static IEnumerable<ActionDefinition> BuildActions(TaliCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the shard's Sec Mov or Sec Def column means the face prints no such icon, so
            // that secondary action is unavailable from this card - expressed by the action not
            // existing rather than by a zero value, exactly as the eight earlier sets express it. Her
            // set has exactly two dashes outside the Ultimate's empty row: Commune with Spirits'
            // Movement column, which is the one-icon ribbon the inventory verified hardest, and
            // nothing else. No face of hers omits the Defense secondary except the Ultimate, so she
            // can always discard to defend.
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

        private static ActionDefinition BuildPrimary(TaliCardProfile profile)
        {
            switch (profile.Primary)
            {
                case TaliPrimaryKind.None:
                    return null;

                case TaliPrimaryKind.Attack:
                case TaliPrimaryKind.RangedAttack:
                {
                    var ranged = profile.Primary == TaliPrimaryKind.RangedAttack;
                    return new ActionDefinition(
                        ranged ? PrimaryRangedAttack : PrimaryAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        ranged ? AttackRangeKind.Ranged : AttackRangeKind.NonRanged,
                        default,
                        TaliEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        Composition(profile.Id),
                        PrintedDefenseResolution.None,
                        TaliEffectContent.AttackTextTimingFor(profile.Id),
                        TargetPolicy(profile, ranged),
                        TaliEffectContent.RaisedAttackFor(profile.Id),
                        MinionDefenseContributionPolicy.IncludeAll,
                        DefenseOfferOriginRestriction.None,
                        PrintedBlockCondition.None,
                        TaliEffectContent.SecondAttackTextFor(profile.Id, ContentRevision),
                        TaliEffectContent.SecondAttackTextTimingFor(profile.Id));
                }

                case TaliPrimaryKind.Skill:
                    // p.9's Ranged qualifier is a statement about the primary's reach, and on a Skill
                    // it is carried by the printed Range icon alone - AttackRangeKind is an Attack's
                    // classification and says nothing about a Skill. Her two Totem faces are the
                    // roster's second and third Ranged Skills after Xargatha's Siren's Call, and they
                    // are told apart from the Radius faces exactly as p.9 says: by the icon, which
                    // each card's own wording ("in range" against "in radius") independently
                    // corroborates.
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        TaliEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        Composition(profile.Id));

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Tali primary action kind.");
            }
        }

        /// <summary>
        /// What one printed Attack of Tali's may target, or <c>null</c> where the face narrows nothing
        /// and p.13's own reading for the subtype stands.
        ///
        /// <para>Two of her five Attacks state something p.13 does not. Ice Shard prints "a hero",
        /// which is the narrowing <see cref="AttackTargetableUnits.HeroesOnly"/> names. Spirit Wolf and
        /// Spirit Bear name their target inside a modal - one bullet reaches a unit in range and the
        /// other a hero adjacent - so neither card can declare one target class for the whole action,
        /// and the binding belongs to the bullet that was chosen rather than to the card. That is the
        /// card-text binding milestone 8.19a minted for Brogan's charges, reused here for the reason
        /// it exists: the printed sentence, not p.14's default question, decides the subject.</para>
        /// </summary>
        private static PrintedAttackTargetPolicy? TargetPolicy(TaliCardProfile profile, bool ranged)
        {
            var classification = PrintedAttackTargetPolicy.ForClassification(
                ranged ? AttackRangeKind.Ranged : AttackRangeKind.NonRanged);
            if (profile.TargetableUnits != AttackTargetableUnits.HeroesAndMinions)
            {
                classification = new PrintedAttackTargetPolicy(
                    profile.TargetableUnits, classification.Reach, classification.Line);
            }

            if (profile.BindsTargetInCardText && !Composition(profile.Id).IsAwaited)
            {
                return classification.BoundByCardText();
            }

            return profile.TargetableUnits == AttackTargetableUnits.HeroesAndMinions
                ? (PrintedAttackTargetPolicy?)null
                : classification;
        }

        /// <summary>
        /// Whether this milestone composes a face's printed text, or which piece it waits on.
        ///
        /// <para><b>One source, read here and nowhere else.</b>
        /// <see cref="TaliEffectContent.CompositionOwed"/> is where the debts are written, beside the
        /// compositions themselves, so a face cannot be marked composed on the definition while the
        /// effect content quietly returns nothing for it - which is the exact drift a second list would
        /// allow. This milestone's tests assert the two agree in both directions.</para>
        /// </summary>
        private static PrintedTextComposition Composition(CardDefinitionId cardId) =>
            TaliEffectContent.CompositionOwed.TryGetValue(cardId, out var owed)
                ? PrintedTextComposition.DeferredUntil(owed)
                : PrintedTextComposition.None;

        /// <summary>The p.9 Range or Radius icon a face prints, or nothing where it prints neither.</summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(TaliCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        private enum TaliPrimaryKind
        {
            /// <summary>p.16's Ultimate, which is not an action at all.</summary>
            None = 0,
            Attack = 1,
            Skill = 2,

            /// <summary>
            /// p.9's Ranged Attack: "Ranged primary actions have the word Ranged next to their type,
            /// and a range icon on the right side of the card." Her two Spirit Attacks.
            /// </summary>
            RangedAttack = 3
        }

        private sealed class TaliCardProfile
        {
            public TaliCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                TaliPrimaryKind primary,
                int primaryValue,
                PrintedValueChannel iconChannel,
                int iconValue,
                CardProgressionProfile progression,
                string printedText,
                AttackTargetableUnits targetableUnits = AttackTargetableUnits.HeroesAndMinions,
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
                TargetableUnits = targetableUnits;
                BindsTargetInCardText = bindsTargetInCardText;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public TaliPrimaryKind Primary { get; }
            public int PrimaryValue { get; }
            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public AttackTargetableUnits TargetableUnits { get; }

            /// <summary>Whether this Attack's own text names the unit it attacks.</summary>
            public bool BindsTargetInCardText { get; }
        }
    }
}
