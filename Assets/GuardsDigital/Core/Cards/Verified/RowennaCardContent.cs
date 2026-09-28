using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Rowenna the Vanguard's nineteen printed faces as definition content - milestones 8.57a through
    /// 8.57f, and the eighteenth hero this project builds.
    ///
    /// <para><b>The set is whole at this milestone, and that is what changed.</b> Her shard records
    /// nineteen printed faces. 8.57a composed the SIX whose clauses that block owned - Code of
    /// Chivalry and the five Reds that reward a team-mate after the attack - 8.57b added FOUR more -
    /// her Silver basic Throw the Gauntlet and the three Blue faces that swap with a friendly unit -
    /// 8.57c added the TWO Blue faces that defeat a surrendering enemy, 8.57d added the FIVE Green
    /// ones, and 8.57e her Purple Ultimate. 8.57f adds the nineteenth - p.20's boxed Handicap, Earn
    /// the Spurs - and with it the members p.5's shape is a statement about: a
    /// <see cref="StartingHand"/> of five, a <see cref="Deck"/> of thirteen, the one card
    /// <see cref="ReturnedToBoxAtSetup"/>, and <see cref="DealtDefinitions"/> - the eighteen a match
    /// actually deals. 8.57g deals her.</para>
    ///
    /// <para><b>All three coloured ladders are composed, so every upgrade pair she prints is stated
    /// whole.</b> Stand Guard upgrades to Protect the Weak or Accept Surrender, and each of those to
    /// Defend the Innocent or Glorious Triumph; Close Quarters upgrades to Melee or Opening Shots, and
    /// each of those to Grand Melee or Opening Volley; Token of Gratitude upgrades to Fair Share or
    /// Feat of Bravery, and each of those to Paragon of Grace or Paragon of Valor. Two faces sit on no
    /// ladder and are named by none: p.20 keeps Earn the Spurs in the box, and p.16 UNLOCKS Fabled
    /// Lance rather than upgrading into it.</para>
    ///
    /// <para><b>The revision is real from this milestone on.</b> <see cref="ContentRevision"/> reads
    /// <c>rowenna-9-1-rev-1</c> - every dealt hero's
    /// <c>&lt;hero&gt;-&lt;milestone&gt;-rev-&lt;n&gt;</c> spelling - and replaces the deliberately
    /// malformed placeholder milestone 8.57a minted and the five composition blocks carried. The
    /// project's placeholder rule is that a placeholder must not be a value that could be mistaken for
    /// a real one, which is why that string is gone from the tree rather than left in a comment; the
    /// real value is minted here, where the whole set moves together, so every one of the nineteen
    /// definitions and every effect body they reach carries one string.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <see cref="MatchContentCatalogs"/> names no Rowenna
    /// content, so no configured match can deal these eighteen cards and nothing reachable can start them.
    /// That is milestone 7.9's gate rather than an oversight - the registered content list is
    /// serialized, and registering a card before it can be dealt moves the pinned list, every
    /// snapshot's bytes and every corpus digest while the content never moved. 8.57g is the block that
    /// flips it, and until it does <c>RowennaPrintedSetTests</c> asserts the gate from this side: the
    /// four faces naming a deferred Place are refused by what the build registers, and by nothing
    /// else.</para>
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at milestone 8.57's inventory pass and
    /// recorded in the <c>Docs/AI/Rules/HERO_ROWENNA.md</c> shard. The table there and the rows here
    /// are meant to be read side by side. Nothing is derived.</para>
    ///
    /// <para><b>RO-03 is open and FOUR of these eighteen faces are touched by it.</b> The printed colour
    /// conflict her shard holds for the user is on Accept Surrender, Glorious Triumph, Melee and Grand
    /// Melee - each carries a ribbon, band and frame of one colour and a corner letter of another. The
    /// shard's working assumption is the RIBBON, which makes the first two Blue and the second two
    /// Green, and the rows below state that. What the open question can move is which colour's p.15 ladder they sit
    /// on and nothing else: their text, values, Initiative, item and Tier are printed and read, and
    /// the two Defeat bodies do not consult a colour at all. Milestone 8.57f states the whole set and
    /// is where a user answer would land.</para>
    /// </summary>
    public static class RowennaCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, and the one string every effect body of hers is
        /// composed under. Minted at 8.57f, where the set became whole - see the type's own remarks.
        /// </summary>
        public const string ContentRevision = "rowenna-9-1-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself.
        /// </summary>
        public const string ContentSetId = "rowenna";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities for the faces these four blocks compose. Named rather than
        // generated, because the upgrade pairs below reference them.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Her Gold basic, and the first face in the corpus whose rider hands the hero its own Attack
        /// just targeted something to do.
        /// </summary>
        public static CardDefinitionId CodeOfChivalry { get; } = Id("code-of-chivalry");

        /// <summary>Her printed Red Tier I.</summary>
        public static CardDefinitionId TokenOfGratitude { get; } = Id("token-of-gratitude");

        /// <summary>The first Red Tier II sibling: Token of Gratitude's sentence at Radius 3.</summary>
        public static CardDefinitionId FairShare { get; } = Id("fair-share");

        /// <summary>The second Red Tier II sibling: the coin replaced by an optional retrieval.</summary>
        public static CardDefinitionId FeatOfBravery { get; } = Id("feat-of-bravery");

        /// <summary>The Red Tier III that adds the conditional repeat to the coin.</summary>
        public static CardDefinitionId ParagonOfGrace { get; } = Id("paragon-of-grace");

        /// <summary>The Red Tier III that adds the conditional repeat to the retrieval.</summary>
        public static CardDefinitionId ParagonOfValor { get; } = Id("paragon-of-valor");

        /// <summary>
        /// Her Silver basic, and the only face in the corpus that places its own hero beside an enemy
        /// and then hands that enemy a decision.
        /// </summary>
        public static CardDefinitionId ThrowTheGauntlet { get; } = Id("throw-the-gauntlet");

        /// <summary>Her printed Blue Tier I.</summary>
        public static CardDefinitionId StandGuard { get; } = Id("stand-guard");

        /// <summary>The Blue Tier II sibling this block owns: Stand Guard's sentence at Range 4.</summary>
        public static CardDefinitionId ProtectTheWeak { get; } = Id("protect-the-weak");

        /// <summary>The Blue Tier III that adds her own retrieval to the swap.</summary>
        public static CardDefinitionId DefendTheInnocent { get; } = Id("defend-the-innocent");

        /// <summary>
        /// Milestone 8.57c. The second Blue Tier II sibling, and the only face in the corpus that
        /// defeats a hero for holding nothing.
        /// </summary>
        public static CardDefinitionId AcceptSurrender { get; } = Id("accept-surrender");

        /// <summary>
        /// Milestone 8.57c. The Blue Tier III that adds the tripled assist coins to that Defeat.
        /// </summary>
        public static CardDefinitionId GloriousTriumph { get; } = Id("glorious-triumph");

        /// <summary>
        /// Milestone 8.57d. Her printed Green Tier I, and the first face in the corpus whose text
        /// places somebody ELSE beside a hero the same sentence names.
        /// </summary>
        public static CardDefinitionId CloseQuarters { get; } = Id("close-quarters");

        /// <summary>
        /// Milestone 8.57d. The first Green Tier II sibling: Close Quarters' sentence at Radius 3.
        /// </summary>
        public static CardDefinitionId Melee { get; } = Id("melee");

        /// <summary>
        /// Milestone 8.57d. The second Green Tier II sibling, and the only face in the corpus whose
        /// condition is about two heroes' discard piles being EMPTY.
        /// </summary>
        public static CardDefinitionId OpeningShots { get; } = Id("opening-shots");

        /// <summary>
        /// Milestone 8.57d. The Green Tier III that takes the modal up to twice.
        /// </summary>
        public static CardDefinitionId GrandMelee { get; } = Id("grand-melee");

        /// <summary>
        /// Milestone 8.57d. The Green Tier III that adds p.10's repeat to the forced discard.
        /// </summary>
        public static CardDefinitionId OpeningVolley { get; } = Id("opening-volley");

        /// <summary>
        /// Milestone 8.57e. Her Purple Tier IV - rulebook p.16's permanent ability, and the only face
        /// in the corpus that changes what rulebook p.13 makes of every Attack its hero performs.
        /// </summary>
        public static CardDefinitionId FabledLance { get; } = Id("fabled-lance");

        /// <summary>
        /// Milestone 8.57f. Her Gold Handicap - rulebook p.20's card, and the nineteenth face that
        /// makes the set whole.
        ///
        /// <para>It is inventory rather than deck content: p.6's setup returns it to the box and p.20
        /// brings it back only for an odd player count, which is why it is in
        /// <see cref="ReturnedToBoxAtSetup"/> and in neither <see cref="StartingHand"/> nor
        /// <see cref="Deck"/>. Its printed values, words, colour and icons ship here all the same -
        /// what the established deferral defers is the COMPOSITION of its text, not the card.</para>
        /// </summary>
        public static CardDefinitionId EarnTheSpurs { get; } = Id("earn-the-spurs");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as every earlier set's are.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryAttack { get; } =
            new ActionDefinitionId("primary-attack");

        /// <summary>
        /// Milestone 8.57b. p.9's Silver band with a Skill primary - "BASIC SKILL - RANGED" on the
        /// card. It is a separate local identity from <see cref="PrimarySkill"/> for the reason every
        /// earlier set keeps its basic and non-basic primaries apart: p.15's level-up replaces coloured
        /// cards and never a basic, so the two are different slots on the dashboard rather than two
        /// printings of one.
        /// </summary>
        public static ActionDefinitionId PrimaryBasicSkill { get; } =
            new ActionDefinitionId("primary-basic-skill");

        /// <summary>Milestone 8.57b. The Skill primary her three composed Blue faces print.</summary>
        public static ActionDefinitionId PrimarySkill { get; } =
            new ActionDefinitionId("primary-skill");

        /// <summary>
        /// Milestone 8.57d. The Movement primary her three composed Green Movement faces print - p.9's
        /// green band, whose value is the walk and whose Radius icon her card text reads.
        ///
        /// <para>It is a separate local identity from <see cref="SecondaryMovement"/> for the reason
        /// every earlier set keeps the two apart: p.11 applies a Movement card's text when the PRIMARY
        /// is performed, and a secondary Movement ribbon on some other card performs none - which is
        /// exactly what <see cref="CardTextApplicationPolicy.Suppressed"/> says on the secondary
        /// below.</para>
        /// </summary>
        public static ActionDefinitionId PrimaryMovement { get; } =
            new ActionDefinitionId("primary-movement");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// Milestone 8.57f. p.6's five starting cards - one of each colour a hero holds at setup: her
        /// Gold basic, her Silver basic, and the Tier I of each of Red, Blue and Green.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            CodeOfChivalry, ThrowTheGauntlet, TokenOfGratitude, StandGuard, CloseQuarters
        });

        /// <summary>
        /// Milestone 8.57f. The thirteen cards p.6 leaves in the deck, reachable only through p.15's
        /// Level Up and p.16's unlock: the six Tier II faces, the six Tier III faces, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            FairShare, FeatOfBravery, ParagonOfGrace, ParagonOfValor,
            ProtectTheWeak, AcceptSurrender, DefendTheInnocent, GloriousTriumph,
            Melee, OpeningShots, GrandMelee, OpeningVolley,
            FabledLance
        });

        /// <summary>
        /// Milestone 8.57f. p.6's setup returns the Handicap card to the box, and p.20 brings it back
        /// only for an odd player count. It is inventory rather than deck content, which is exactly why
        /// <see cref="DealtDefinitions"/> is eighteen where <see cref="AllDefinitions"/> is nineteen.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } =
            Array.AsReadOnly(new[] { EarnTheSpurs });

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAllDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() =>
                Array.AsReadOnly(Profiles().Select(Build).ToArray()));

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDealtDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(
                LazyAllDefinitions.Value
                    .Where(definition => !ReturnedToBoxAtSetup.Contains(definition.Id))
                    .ToArray()));

        /// <summary>
        /// All nineteen faces, in the order the five composition blocks added them - her Gold basic and
        /// the five Reds, then her Silver basic and the three swapping Blues, the two Defeat Blues, the
        /// five Greens, the Ultimate, and p.20's Handicap last.
        ///
        /// <para>That is deliberately NOT her shard's table order, and the difference is stated rather
        /// than papered over: the shard lists both basics first, and these rows grew one block at a
        /// time. <c>RowennaPrintedSetTests</c> asserts the two against each other as a BIJECTION over
        /// nineteen named faces, which is the claim that matters - the order a list happens to carry is
        /// not one.</para>
        ///
        /// <para>Built lazily, which is the convention milestone 8.38d adopted after a
        /// static-initialization cycle cost milestone 8.37 1,423 failing cases on one
        /// <c>NullReferenceException</c> and then cost a later session <c>NUnit failed to load</c> with
        /// zero tests discovered while <c>dotnet build</c> reported no errors at all.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;

        /// <summary>
        /// Milestone 8.57f. The eighteen definitions a match deals from - everything but the boxed
        /// Handicap card.
        ///
        /// <para>Its absence was a RED on main from 8.57a: two Edit Mode cases reflect over every
        /// <c>*CardContent</c> class in the assembly for a public static <c>DealtDefinitions</c> - the
        /// artwork catalog's clean-checkout walk asserts the property is there at all, and
        /// <c>DealtCorpusCompositionValidationTests</c> walks whatever it finds - and hers was the one
        /// class without one while her composition blocks ran.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealtDefinitions.Value;

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException($"Rowenna prints no card '{id}'.", nameof(id));

        /// <summary>
        /// Milestone 8.57f. One hero's eighteen card instances at p.6's setup: the five starting-hand
        /// cards in hand and the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Rowenna's cards require a defined owner.", nameof(ownerId));
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
        /// Milestone 8.57f. One seat's whole setup as a hand the configured match can be composed from:
        /// p.6's five in hand, the thirteen its deck keeps, and the eighteen definitions both draw on.
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
        // The printed faces. One row per card, in the order the composition blocks added them, with
        // the same columns as the shard's table: Initiative, the two secondary icons, the primary
        // Attack value, the p.9 Radius icon (0 where the face prints none), the progression content
        // and the transcribed text.
        // -----------------------------------------------------------------------------------------

        private static IEnumerable<RowennaCardProfile> Profiles() => new[]
        {
            new RowennaCardProfile(
                CodeOfChivalry, "Code of Chivalry", 11, 2, 1, 4, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a unit adjacent to you. Before the attack: If you target a hero, both you " +
                "and the target may retrieve a discarded card."),

            new RowennaCardProfile(
                TokenOfGratitude, "Token of Gratitude", 8, 6, 4, 5, 1,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    FairShare, FeatOfBravery),
                "Target a unit adjacent to you. After the attack: A friendly hero in radius gains 1 " +
                "coin."),

            new RowennaCardProfile(
                FairShare, "Fair Share", 8, 6, 4, 6, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    ParagonOfGrace, ParagonOfValor),
                "Target a unit adjacent to you. After the attack: A friendly hero in radius gains 1 " +
                "coin."),

            new RowennaCardProfile(
                FeatOfBravery, "Feat of Bravery", 8, 7, 4, 7, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    ParagonOfGrace, ParagonOfValor),
                "Target a unit adjacent to you. After the attack, a friendly hero in radius may " +
                "retrieve a discarded card."),

            new RowennaCardProfile(
                ParagonOfGrace, "Paragon of Grace", 9, 7, 4, 6, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Range),
                "Target a unit adjacent to you. After the attack: A friendly hero in radius gains 1 " +
                "coin. If they do, repeat once on a different target."),

            new RowennaCardProfile(
                ParagonOfValor, "Paragon of Valor", 9, 8, 4, 7, 4,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Defense),
                "Target a unit adjacent to you. After the attack, a friendly hero in radius may " +
                "retrieve a discarded card; If they do, repeat once on a different target."),

            // Milestone 8.57b. Her Silver basic and three of her six Blue faces. The Movement column
            // is 0 on the Silver one because RO-05 records that Throw the Gauntlet prints NO Movement
            // ribbon icon at all - a missing icon is absent, never zero - and the builder below
            // expresses that by not defining the action rather than by defining it with a zero.
            new RowennaCardProfile(
                ThrowTheGauntlet, "Throw the Gauntlet", 5, 3, 0, 0, 0,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "Place yourself into a space in range adjacent to an enemy hero in range; that hero " +
                "may move 1 space; if they do, gain 2 coins.",
                RowennaPrimaryKind.BasicSkill, range: 4),

            // Milestone 8.57c completes both Blue upgrade pairs: Stand Guard upgrades to Protect the
            // Weak or Accept Surrender, and Protect the Weak to Defend the Innocent or Glorious
            // Triumph. 8.57b could name only the sibling that existed; both pairs are whole here.
            new RowennaCardProfile(
                StandGuard, "Stand Guard", 9, 6, 2, 0, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    ProtectTheWeak, AcceptSurrender),
                "Swap with a friendly unit in range which is adjacent to an enemy hero, or has a " +
                "card in the discard.",
                RowennaPrimaryKind.Skill, range: 3),

            new RowennaCardProfile(
                ProtectTheWeak, "Protect the Weak", 10, 6, 2, 0, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    DefendTheInnocent, GloriousTriumph),
                "Swap with a friendly unit in range which is adjacent to an enemy hero, or has a " +
                "card in the discard.",
                RowennaPrimaryKind.Skill, range: 4),

            // Milestone 8.57c. Her two Defeat faces print NO Range and NO Radius icon at all - RO-05's
            // rule again, and the reason their reach is the word "adjacent" rather than a number p.16
            // could raise. The Range column is therefore 0 and the builder expresses that by defining
            // the Skill without a printed icon rather than with a zero one.
            //
            // RO-03 is OPEN on both of these faces and neither reading changes what is composed here:
            // the shard's working assumption is the RIBBON, which is Blue, and the corner letter reads
            // G. What the open question decides is which colour's upgrade ladder they sit on - which
            // is why the pairs above name them - and nothing about their text, values or item.
            new RowennaCardProfile(
                AcceptSurrender, "Accept Surrender", 10, 6, 2, 0, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Defense,
                    DefendTheInnocent, GloriousTriumph),
                "Defeat an enemy hero adjacent to you with no cards in hand.",
                RowennaPrimaryKind.Skill),

            new RowennaCardProfile(
                DefendTheInnocent, "Defend the Innocent", 10, 7, 2, 0, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Radius),
                "Swap with a friendly unit in range which is adjacent to an enemy hero, or has a " +
                "card in the discard; you may retrieve a discarded card.",
                RowennaPrimaryKind.Skill, range: 4),

            new RowennaCardProfile(
                GloriousTriumph, "Glorious Triumph", 10, 7, 2, 0, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Movement),
                "Defeat an enemy hero adjacent to you with no cards in hand; your friendly heroes " +
                "gain triple assist coins.",
                RowennaPrimaryKind.Skill),

            // Milestone 8.57d. Her whole Green ladder, which is the last coloured one she owes: a Tier
            // I that upgrades to Melee or Opening Shots, and each of those to Grand Melee or Opening
            // Volley. Three of the five print a MOVEMENT primary and two print a Skill, which is why
            // the ladder's two rungs are not the two halves of one shape - p.15 upgrades by colour and
            // says nothing about the band.
            //
            // RO-05 is why the Movement COLUMN below is 0 on three of them: Close Quarters, Melee and
            // Grand Melee print no Movement ribbon icon at all - only Defense - and their Movement
            // value is the PRIMARY's, which the builder reads from its own field. A missing icon is
            // absent, never zero, and the builder expresses that by not defining the secondary action.
            //
            // RO-03 is OPEN on Melee and Grand Melee and neither reading changes what is composed here:
            // both carry a green ribbon, a green Movement band and a corner B, the shard's working
            // assumption is the RIBBON, and what the open question can move is which colour's p.15
            // ladder they sit on - which is why the pairs below name them - and nothing about their
            // text, values, Initiative, item or Tier.
            new RowennaCardProfile(
                CloseQuarters, "Close Quarters", 4, 3, 0, 0, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    Melee, OpeningShots),
                "After movement, if you are adjacent to an enemy hero, you may Choose one - (1) " +
                "Place a friendly minion in radius into a space adjacent to that enemy hero. (2) " +
                "Place an enemy minion in radius into a space adjacent to you.",
                RowennaPrimaryKind.Movement, movement: 2),

            new RowennaCardProfile(
                Melee, "Melee", 4, 4, 0, 0, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    GrandMelee, OpeningVolley),
                "After movement, if you are adjacent to an enemy hero, you may Choose one - (1) " +
                "Place a friendly minion in radius into a space adjacent to that enemy hero. (2) " +
                "Place an enemy minion in radius into a space adjacent to you.",
                RowennaPrimaryKind.Movement, movement: 2),

            new RowennaCardProfile(
                OpeningShots, "Opening Shots", 4, 4, 2, 0, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Initiative,
                    GrandMelee, OpeningVolley),
                "If both you and an enemy hero in radius have no cards in the discard, that hero " +
                "discards a card, if able.",
                RowennaPrimaryKind.Skill, radiusSkill: true),

            new RowennaCardProfile(
                GrandMelee, "Grand Melee", 3, 4, 0, 0, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Attack),
                "After movement, if you are adjacent to an enemy hero, Choose up to two times - (1) " +
                "Place a friendly minion in radius into a space adjacent to that enemy hero. (2) " +
                "Place an enemy minion in radius into a space adjacent to you.",
                RowennaPrimaryKind.Movement, movement: 2),

            new RowennaCardProfile(
                OpeningVolley, "Opening Volley", 3, 4, 2, 0, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Initiative),
                "If both you and an enemy hero in radius have no cards in the discard, that hero " +
                "discards a card, if able. May repeat once.",
                RowennaPrimaryKind.Skill, radiusSkill: true),

            // Milestone 8.57e. p.16's permanent ability. It prints a Range 2 icon and no Initiative, no
            // secondary icons and no action at all, which is what makes the row's other zeros a
            // statement rather than an omission - the same shape Tali's Reign of Winter and Ursafar's
            // Unbound Fury take. The Range icon does NOT arrive through the Range column below, which
            // feeds a printed ACTION's icon: this card has no action, so the icon is carried on the
            // ability itself, where milestone 8.3 put p.9's one Ultimate icon.
            new RowennaCardProfile(
                FabledLance, "Fabled Lance", 0, 0, 0, 0, 0,
                CardProgressionProfile.UltimateCard(),
                "All your attack actions gain the \"Ranged\" subtype, target a unit in range, and " +
                "count as having a printed Range value of 2.",
                RowennaPrimaryKind.None),

            // Milestone 8.57f. p.20's Handicap card, and the nineteenth face. Its printed values,
            // words, colour and both secondary icons ship here as inventory exactly as every other
            // face's do - what is deferred is the COMPOSITION of its text, not the card.
            //
            // The deferral is the established one and it is the ONLY one on her set: p.6 returns this
            // card to the box, p.20 brings it back only for an odd player count, and the 2026-08-21
            // user decision named that trigger for Wasp's Crackling Dagger. Arien's Ceremonial Blade,
            // Widget's Follow Along, Swift's Backup and Bain's Side Quest each carry the same string.
            //
            // What the deferral covers is the WHOLE printed sentence, its "Target a HERO adjacent to
            // you" included, which is why this row declares no hero-only target narrowing: every
            // roster Handicap printing that same clause leaves it to the deferral rather than
            // composing half a card, and composing one half here would make her the one hero whose
            // boxed face is partly live.
            //
            // It sits on NO upgrade ladder and is named by none - p.15 upgrades a coloured card into
            // its own colour's next Tier, and this is a Gold basic that never enters a deck.
            new RowennaCardProfile(
                EarnTheSpurs, "Earn the Spurs", 10, 1, 1, 4, 0,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Target a hero adjacent to you. Before the attack: Both you and the target may " +
                "retrieve a discarded card.",
                boxed: true)
        };

        private static CardDefinition Build(RowennaCardProfile profile) =>
            new CardDefinition(
                profile.Id,
                ContentRevision,
                profile.DisplayName,
                // Milestone 8.57e. p.16's card prints no Initiative at all, so the absence is the
                // undefined value rather than a zero - the same statement Tali's and Ursafar's
                // Ultimates make, and the reason this is a branch rather than a number.
                profile.Primary == RowennaPrimaryKind.None
                    ? default
                    : new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                profile.Progression,
                BuildActions(profile),
                // Milestone 8.57e. p.16's permanent ability as a typed rule rather than a standing
                // declaration: Fabled Lance changes what rulebook p.13 makes of every Attack she
                // performs and what p.9's Range icon on it reads, which is a modification family
                // rather than a name some query matches on. See RowennaEffectContent.FabledLanceRule.
                profile.Id.Equals(FabledLance)
                    ? RowennaEffectContent.FabledLanceRule()
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));

        private static IEnumerable<ActionDefinition> BuildActions(RowennaCardProfile profile)
        {
            // Milestone 8.57e. The Ultimate prints no action at all - not a primary, not a secondary
            // Defense - so she cannot discard it to defend and it is never played. Expressed by
            // yielding nothing rather than by a zero-valued action, which is the same rule RO-05
            // applies to every missing icon in this set.
            if (profile.Primary == RowennaPrimaryKind.None)
            {
                yield break;
            }

            yield return BuildPrimary(profile);

            // Milestone 8.57b. A dash in the shard's Movement column means the face prints no such
            // icon at all - RO-05, and Throw the Gauntlet is the one composed face it applies to - so
            // that secondary action is expressed by not existing rather than by a zero value.
            if (profile.SecondaryMovement > 0)
            {
                yield return new ActionDefinition(
                    SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[]
                    {
                        new PrintedValueDefinition(
                            PrintedValueChannel.Movement, profile.SecondaryMovement)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));
            }

            yield return new ActionDefinition(
                SecondaryDefense,
                ActionKind.Defense,
                ActionDefinitionOrigin.Reaction,
                new[]
                {
                    new PrintedValueDefinition(
                        PrintedValueChannel.Defense, profile.SecondaryDefense)
                },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));
        }

        /// <summary>
        /// Milestone 8.57b. The primary action this face prints, by the band p.9 draws it in.
        ///
        /// <para>Her six Attack faces print "Target a unit adjacent to you", which is p.13's own reach
        /// for a non-Ranged Attack - "even if the card has a radius icon" - so the target policy is the
        /// page's default, supplied by <see cref="ActionDefinition"/> rather than restated here, and
        /// the Radius icon is read by the card TEXT's own selection and by nothing else. Her four
        /// composed Skill faces print a Range icon instead, which is what "Skill - Ranged" is in
        /// engine terms: p.9's printed value with p.16's items composed onto it, read by the Skill's
        /// own text through <c>RangeValueQuery</c>.</para>
        /// </summary>
        private static ActionDefinition BuildPrimary(RowennaCardProfile profile)
        {
            switch (profile.Primary)
            {
                case RowennaPrimaryKind.BasicSkill:
                case RowennaPrimaryKind.Skill:
                    return new ActionDefinition(
                        profile.Primary == RowennaPrimaryKind.BasicSkill
                            ? PrimaryBasicSkill
                            : PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        profile.RadiusSkill ? PrintedRadius(profile) : PrintedRange(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        RowennaEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                // Milestone 8.57d. p.11's Movement as Primary Action: "apply the card text in addition
                // to moving up to the number of spaces on the icon". Which side of the walk that text
                // lands on is the card's own word rather than the page's default - all three of her
                // Green Movement faces print "After movement" in so many words, which is the one thing
                // p.11's "unless it says otherwise" is about - so the timing arrives beside the root.
                case RowennaPrimaryKind.Movement:
                    return new ActionDefinition(
                        PrimaryMovement,
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                            {
                                new PrintedValueDefinition(
                                    PrintedValueChannel.Movement, profile.Movement)
                            }
                            .Concat(PrintedRadius(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        RowennaEffectContent.MovementTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        RowennaEffectContent.MovementTextTimingFor(profile.Id));

                default:
                    return new ActionDefinition(
                        PrimaryAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                            {
                                new PrintedValueDefinition(
                                    PrintedValueChannel.Attack, profile.Attack)
                            }
                            .Concat(PrintedRadius(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        RowennaEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        RowennaEffectContent.AttackTextTimingFor(profile.Id));
            }
        }

        /// <summary>
        /// p.9's Radius icon, or nothing when the face prints none. Code of Chivalry is the one face
        /// here that prints neither a Range nor a Radius, and that absence is content: its rider names
        /// the Attack's own target rather than reaching for anybody at a distance.
        /// </summary>
        private static IEnumerable<PrintedValueDefinition> PrintedRadius(RowennaCardProfile profile) =>
            profile.Radius > 0
                ? new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, profile.Radius) }
                : Enumerable.Empty<PrintedValueDefinition>();

        /// <summary>
        /// Milestone 8.57b. p.9's Range icon, or nothing when the face prints none. Every Skill this
        /// block composes prints one; a Skill that printed none would have no such reach at all and
        /// p.16's items would not invent one.
        /// </summary>
        private static IEnumerable<PrintedValueDefinition> PrintedRange(RowennaCardProfile profile) =>
            profile.Range > 0
                ? new[] { new PrintedValueDefinition(PrintedValueChannel.Range, profile.Range) }
                : Enumerable.Empty<PrintedValueDefinition>();

        /// <summary>
        /// Milestone 8.57b. Which band a composed face prints its primary action in. Her Purple
        /// Ultimate joins at 8.57e.
        /// </summary>
        private enum RowennaPrimaryKind
        {
            /// <summary>p.9's Gold band with an Attack primary, and her five coloured Reds.</summary>
            Attack = 0,

            /// <summary>p.9's Silver band with a Skill primary - "BASIC SKILL - RANGED".</summary>
            BasicSkill = 1,

            /// <summary>A coloured Skill primary - her five composed Blues and two of her Greens.</summary>
            Skill = 2,

            /// <summary>
            /// Milestone 8.57d. p.9's green band with a Movement primary - her three Green faces whose
            /// icon is a walk and whose text is applied around it.
            /// </summary>
            Movement = 3,

            /// <summary>
            /// Milestone 8.57e. No band and no action: p.16's Ultimate, whose whole printed content is
            /// a permanent ability. It is a member here rather than an absence some other field
            /// implies, because the two things that follow from it - no Initiative and no action
            /// definitions at all - are read off exactly this.
            /// </summary>
            None = 4
        }

        private sealed class RowennaCardProfile
        {
            public RowennaCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                int attack,
                int radius,
                CardProgressionProfile progression,
                string printedText,
                RowennaPrimaryKind primary = RowennaPrimaryKind.Attack,
                int range = 0,
                int movement = 0,
                bool radiusSkill = false,
                bool boxed = false)
            {
                Movement = movement;
                RadiusSkill = radiusSkill;
                Boxed = boxed;
                Id = id;
                DisplayName = displayName;
                Initiative = initiative;
                SecondaryDefense = secondaryDefense;
                SecondaryMovement = secondaryMovement;
                Attack = attack;
                Radius = radius;
                Progression = progression;
                PrintedText = printedText;
                Primary = primary;
                Range = range;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public int Attack { get; }
            public int Radius { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public RowennaPrimaryKind Primary { get; }
            public int Range { get; }

            /// <summary>Milestone 8.57d. The PRIMARY Movement icon's value, where the face prints one.</summary>
            public int Movement { get; }

            /// <summary>
            /// Milestone 8.57d. Whether this face's Skill primary prints a RADIUS icon rather than a
            /// Range one. Opening Shots and Opening Volley are the first Skills in her set that do, and
            /// the distinction is p.9's own: a Range reaches out to a target and a Radius draws a disk,
            /// which p.16's two different items then raise separately.
            /// </summary>
            public bool RadiusSkill { get; }

            /// <summary>
            /// Milestone 8.57f. Whether p.6's setup returns this face to the box - true for Earn the
            /// Spurs and for nothing else she prints. It decides two things at once and they are the
            /// same statement: the card is outside <see cref="DealtDefinitions"/>, and its primary
            /// declares the established odd-player deferral instead of a composed body.
            /// </summary>
            public bool Boxed { get; }

            /// <summary>
            /// What this face's primary says about its own card text: the established p.20 deferral on
            /// the one boxed face, and nothing at all on the eighteen dealt ones.
            /// </summary>
            public PrintedTextComposition TextComposition =>
                Boxed
                    ? PrintedTextComposition.DeferredUntil("first odd-player-count configuration")
                    : PrintedTextComposition.None;
        }
    }
}
