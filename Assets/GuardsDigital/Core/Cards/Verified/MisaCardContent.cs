using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Misa the Samurai's nineteen printed faces as definition content - milestone 8.35z29.
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces by the read-only inventory pass of
    /// 2026-09-06, whose record is <c>worker-hero-misa/INVENTORY.md</c> and whose eventual governed
    /// home is <c>Docs/AI/Rules/HERO_MISA.md</c>. The tables there and the rows here are meant to be
    /// read side by side: the order, the names and the column meanings match, so a reviewer can audit
    /// one against the other without holding anything in their head. Nothing is derived, for the reason
    /// <see cref="WaspCardContent"/> records.</para>
    ///
    /// <para><b>What is different from the six sets already built, and therefore what this file is the
    /// first to state.</b> Misa prints <em>no basic Attack anywhere</em> - her Gold basic, her Silver
    /// basic and her Gold Handicap are all Basic Skills, where every prior verified hero put an Attack
    /// on the Gold card. Her Blue line is the first verified content to print p.17's <b>ignoring
    /// obstacles</b> on a movement of her own. And four of her faces print <b>"at maximum
    /// range/radius"</b>, which the user's 2026-09-06 ruling settles as <em>exactly</em> the printed
    /// number rather than "up to" it - the first reach in this build whose candidate set is a ring
    /// rather than a disk, and therefore the first that can be legally empty.</para>
    ///
    /// <para><b>What her set does not print, which is content too.</b> No Defense primary, no p.14
    /// Block, no Defense/Skill combined card - every Red card is an Attack and every Blue and Green
    /// card is a Skill. She owns no rules token and no marker: no face of hers names, places, gives or
    /// targets one, so this milestone adds no component content at all, and the blank Tabletopia
    /// dashboard back is missing art rather than missing rules (retired ledger question 14).</para>
    ///
    /// <para><b>No image, anywhere.</b> The extracted faces are audit and presentation assets and stay
    /// that way: nothing in this file, and no member of the content model it builds, names an image, an
    /// art asset or a file path. A definition is its values, its words and its relationships.</para>
    /// </summary>
    public static class MisaCardContent
    {
        /// <summary>
        /// The revision all nineteen definitions carry, with the same coupling
        /// <see cref="XargathaCardContent.ContentRevision"/> has: it moves when Misa's content does.
        /// </summary>
        public const string ContentRevision = "misa-8-35x-rev-1";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself.
        /// </summary>
        public const string ContentSetId = "misa";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities. Named rather than generated, because the upgrade pairs below
        // reference them and a generated id would let a renamed card silently break a pair.
        // -----------------------------------------------------------------------------------------

        public static CardDefinitionId WatchHowISoar { get; } = Id("watch-how-i-soar");
        public static CardDefinitionId LeafOnTheWind { get; } = Id("leaf-on-the-wind");
        public static CardDefinitionId SwoopIn { get; } = Id("swoop-in");
        public static CardDefinitionId ChallengeAccepted { get; } = Id("challenge-accepted");
        public static CardDefinitionId MatterOfHonor { get; } = Id("matter-of-honor");
        public static CardDefinitionId PowerShot { get; } = Id("power-shot");
        public static CardDefinitionId WorthyOpponent { get; } = Id("worthy-opponent");
        public static CardDefinitionId ThunderShot { get; } = Id("thunder-shot");
        public static CardDefinitionId SuddenBreeze { get; } = Id("sudden-breeze");
        public static CardDefinitionId GustOfWind { get; } = Id("gust-of-wind");
        public static CardDefinitionId DashAndSlash { get; } = Id("dash-and-slash");
        public static CardDefinitionId CrushingSquall { get; } = Id("crushing-squall");
        public static CardDefinitionId DeathFromAbove { get; } = Id("death-from-above");
        public static CardDefinitionId Focus { get; } = Id("focus");
        public static CardDefinitionId Discipline { get; } = Id("discipline");
        public static CardDefinitionId LivingTornado { get; } = Id("living-tornado");
        public static CardDefinitionId Mastery { get; } = Id("mastery");
        public static CardDefinitionId StormSpirit { get; } = Id("storm-spirit");
        public static CardDefinitionId PowerOverwhelming { get; } = Id("power-overwhelming");

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as the six earlier sets' are. Misa needs
        // four: she prints no Defense primary and no Movement primary, so there is no Block identity
        // and no primary Movement identity here - and that absence is the same statement her
        // never-a-basic-Attack shape makes, said in the other direction.
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
        /// The Handicap card is already out of the box by then, so the Gold card here is Watch How I
        /// Soar.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            WatchHowISoar, SwoopIn, ChallengeAccepted, SuddenBreeze, Focus
        });

        /// <summary>
        /// The thirteen cards p.6 leaves in the deck, reachable only through p.15's Level Up and
        /// p.16's unlock: the six Tier II cards, the six Tier III cards, and the Ultimate.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> Deck { get; } = Array.AsReadOnly(new[]
        {
            MatterOfHonor, PowerShot, WorthyOpponent, ThunderShot,
            GustOfWind, DashAndSlash, CrushingSquall, DeathFromAbove,
            Discipline, LivingTornado, Mastery, StormSpirit,
            PowerOverwhelming
        });

        /// <summary>
        /// p.6's setup returns the Handicap card to the box, and p.20 brings it back only for an odd
        /// player count. It is inventory rather than deck content, which is why it is listed apart
        /// from both.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } = Array.AsReadOnly(new[]
        {
            LeafOnTheWind
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
            throw new ArgumentException($"Misa prints no card '{id}'.", nameof(id));

        /// <summary>
        /// One hero's eighteen card instances at p.6's setup: the five starting-hand cards in hand and
        /// the remaining thirteen in the deck, each unplayed, unresolved and face-up.
        /// </summary>
        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string instanceIdPrefix)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("Misa's cards require a defined owner.", nameof(ownerId));
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
        // its value, the p.9 Range or Radius icon, the progression content, the transcribed text, and
        // the milestone that owes that text its composition.
        // -----------------------------------------------------------------------------------------

        private static IEnumerable<MisaCardProfile> Profiles() => new[]
        {
            // Her Gold basic, and the roster's first basic card that is not an Attack. It is also the
            // only modal in the whole set - two bullets and no more - which is exactly the node the
            // Ultimate's first clause reaches, and the reason that clause has one trigger rather than
            // none or many.
            new MisaCardProfile(
                WatchHowISoar, "Watch How I Soar", 12, 2, 1,
                MisaPrimaryKind.Skill, 0, PrintedValueChannel.Range, 4,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Choose one — • Place yourself into a space at maximum range. • Defeat a minion " +
                "adjacent to you.",
                PrintedTextComposition.None),

            // p.20's Handicap card, deferred exactly as Wasp's Crackling Dagger, Arien's Ceremonial
            // Blade, Brogan's Leap Forward, Sabina's Bang, Tigerclaw's Nimble Strike and Xargatha's
            // Desperate Cleave are, by the same 2026-08-21 user decision: p.6 returns it to the box
            // and the six-player pitch never brings it back. Its printed values and words ship here as
            // inventory all the same. What it says that Watch How I Soar does not is on the row rather
            // than in the deferral: it prints no "Choose one" at all, only the first bullet, which is
            // what makes it p.20's "weaker version" and what leaves the Ultimate's first clause with
            // no trigger anywhere in a handicapped game (RULING 28 records that as printed).
            new MisaCardProfile(
                LeafOnTheWind, "Leaf on the Wind", 11, 1, 0,
                MisaPrimaryKind.Skill, 0, PrintedValueChannel.Range, 4,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                "Place yourself into a space at maximum range.",
                PrintedTextComposition.DeferredUntil("first odd-player-count configuration")),

            // Her Silver basic. p.9 makes it non-Ranged despite reaching three spaces, because the
            // icon it prints is a Radius: "All other actions are non-ranged, including those with a
            // Radius icon." Its destination test is the first in this build that qualifies the SPACE
            // being considered rather than the space the actor stands in.
            new MisaCardProfile(
                SwoopIn, "Swoop In", 6, 3, 0,
                MisaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                "Place yourself into a space in radius adjacent to two or more enemy units. If you " +
                "do, you may retrieve a discarded card.",
                PrintedTextComposition.None),

            // The Red melee family - the whole of Misa's Durable role. One sentence with one number,
            // scaling 2/3/5 across Tier I, II and III, and the only thing that changes between them.
            new MisaCardProfile(
                ChallengeAccepted, "Challenge Accepted", 9, 6, 3,
                MisaPrimaryKind.Attack, 5, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None,
                    MatterOfHonor, PowerShot),
                "Target a unit adjacent to you. After the attack: This turn: Gain +2 [Defense] " +
                "Defense.",
                PrintedTextComposition.None),

            new MisaCardProfile(
                MatterOfHonor, "Matter of Honor", 9, 7, 3,
                MisaPrimaryKind.Attack, 6, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Initiative,
                    WorthyOpponent, ThunderShot),
                "Target a unit adjacent to you. After the attack: This turn: Gain +3 [Defense] " +
                "Defense.",
                PrintedTextComposition.None),

            // The Red ranged pair. Identical in every printed value - Attack 3, Range 2, Sec Def 4,
            // Sec Mov 3 - and separated only by Initiative and by the TENSE of their rider's
            // condition, which RULING 32 settles as two different moments rather than one phrasing.
            new MisaCardProfile(
                PowerShot, "Power Shot", 9, 4, 3,
                MisaPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.II, PrintedValueChannel.Defense,
                    WorthyOpponent, ThunderShot),
                "Target a unit in range. After the attack: If the target was at maximum range, you " +
                "may move it 1 space, to a space farther away from you.",
                PrintedTextComposition.None),

            new MisaCardProfile(
                WorthyOpponent, "Worthy Opponent", 10, 7, 3,
                MisaPrimaryKind.Attack, 6, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Movement),
                "Target a unit adjacent to you. After the attack: This turn: Gain +5 [Defense] " +
                "Defense.",
                PrintedTextComposition.None),

            new MisaCardProfile(
                ThunderShot, "Thunder Shot", 10, 4, 3,
                MisaPrimaryKind.RangedAttack, 3, PrintedValueChannel.Range, 2,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Defense),
                "Target a unit in range. After the attack: If the target is not adjacent to you, you " +
                "may move it 1 space, to a space farther away from you.",
                PrintedTextComposition.None),

            // The Blue line: one sentence with two parameters, and the first verified content in this
            // build to print p.17's "ignoring obstacles". None prints a Range or Radius icon, because
            // the reach IS the movement - which is why the row's icon column is empty on all five and
            // why two of them mint items (Radius, Range) whose reach is entirely outside the Blue set.
            new MisaCardProfile(
                SuddenBreeze, "Sudden Breeze", 10, 5, 1,
                MisaPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.I, PrintedValueChannel.None,
                    GustOfWind, DashAndSlash),
                "Move up to 3 spaces in a straight line, ignoring obstacles; you may place an enemy " +
                "unit you moved through into a space adjacent to you.",
                PrintedTextComposition.None),

            new MisaCardProfile(
                GustOfWind, "Gust of Wind", 10, 6, 1,
                MisaPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Attack,
                    CrushingSquall, DeathFromAbove),
                "Move up to 4 spaces in a straight line, ignoring obstacles; you may place an enemy " +
                "unit you moved through into a space adjacent to you.",
                PrintedTextComposition.None),

            new MisaCardProfile(
                DashAndSlash, "Dash and Slash", 10, 6, 1,
                MisaPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Defense,
                    CrushingSquall, DeathFromAbove),
                "Move up to 4 spaces in a straight line, ignoring obstacles; an enemy hero you moved " +
                "through discards a card, if able.",
                PrintedTextComposition.None),

            new MisaCardProfile(
                CrushingSquall, "Crushing Squall", 11, 6, 1,
                MisaPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Radius),
                "Move up to 5 spaces in a straight line, ignoring obstacles; you may place an enemy " +
                "unit you moved through into a space adjacent to you.",
                PrintedTextComposition.None),

            new MisaCardProfile(
                DeathFromAbove, "Death from Above", 11, 6, 1,
                MisaPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Range),
                "Move up to 5 spaces in a straight line, ignoring obstacles; an enemy hero you moved " +
                "through discards a card, if able.",
                PrintedTextComposition.None),

            // The Green line's three "Next turn:" cards. p.17's keyword, already built for Tigerclaw's
            // Blend Into Shadows, and the p.7 End of Round Retrieve cancels it outright - so one played
            // on the round's fourth turn never takes effect at all. RULING 31 makes the grant fire
            // ONCE: "It says action, not actions."
            new MisaCardProfile(
                Focus, "Focus", 3, 3, 2,
                MisaPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.I, PrintedValueChannel.None,
                    Discipline, LivingTornado),
                "Next turn: Before you perform a primary action, you may move 1 space.",
                PrintedTextComposition.None),

            new MisaCardProfile(
                Discipline, "Discipline", 2, 3, 2,
                MisaPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Initiative,
                    Mastery, StormSpirit),
                "Next turn: Before you perform a primary action, you may move up to 2 spaces.",
                PrintedTextComposition.None),

            // The two Swap cards. p.10's Picking Targets excludes Misa from both - they say "two
            // units", not "you" - and p.18 makes a Swap neither movement nor placement, which is also
            // why neither can ever trigger the Ultimate's second clause.
            new MisaCardProfile(
                LivingTornado, "Living Tornado", 2, 3, 2,
                MisaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.II, PrintedValueChannel.Attack,
                    Mastery, StormSpirit),
                "Swap two units at maximum radius.",
                PrintedTextComposition.None),

            new MisaCardProfile(
                Mastery, "Mastery", 2, 4, 2,
                MisaPrimaryKind.Skill, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Initiative),
                "Next turn: Before you perform a primary action, you may move up to 3 spaces.",
                PrintedTextComposition.None),

            new MisaCardProfile(
                StormSpirit, "Storm Spirit", 2, 4, 2,
                MisaPrimaryKind.Skill, 0, PrintedValueChannel.Radius, 3,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Green, CardTier.III, PrintedValueChannel.Attack),
                "Swap two units in radius and at equal distance from you.",
                PrintedTextComposition.None),

            // p.16's permanent ability, printed as two separate paragraphs in one text box. It prints
            // no Initiative, no icons and no actions at all, which is what makes the row's zeros a
            // statement rather than an omission.
            new MisaCardProfile(
                PowerOverwhelming, "Power Overwhelming", 0, 0, 0,
                MisaPrimaryKind.None, 0, PrintedValueChannel.None, 0,
                CardProgressionProfile.UltimateCard(),
                "Whenever you choose one, you may choose two different options instead, in any " +
                "order. Each time after you place yourself, an enemy hero adjacent to you discards a " +
                "card, if able.",
                PrintedTextComposition.None)
        };

        private static CardDefinition Build(MisaCardProfile profile) =>
            new CardDefinition(
                profile.Id,
                ContentRevision,
                profile.DisplayName,
                profile.Primary == MisaPrimaryKind.None
                    ? default
                    : new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                profile.Progression,
                BuildActions(profile),
                profile.Id.Equals(PowerOverwhelming)
                    ? MisaEffectContent.PowerOverwhelmingRule(ContentRevision)
                    : null,
                CardSourceContent.Transcribed(profile.PrintedText));

        private static IEnumerable<ActionDefinition> BuildActions(MisaCardProfile profile)
        {
            var actions = new List<ActionDefinition>();
            var primary = BuildPrimary(profile);
            if (primary != null)
            {
                actions.Add(primary);
            }

            // A dash in the inventory's Sec Mov or Sec Def column means the face prints no such icon,
            // so that secondary action is unavailable from this card - expressed by the action not
            // existing rather than by a zero value, exactly as the six earlier sets express it. Misa's
            // dashes are Swoop In's and Leaf on the Wind's Movement column and the Ultimate's whole
            // row; every other face prints both.
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

        private static ActionDefinition BuildPrimary(MisaCardProfile profile)
        {
            switch (profile.Primary)
            {
                case MisaPrimaryKind.None:
                    return null;

                case MisaPrimaryKind.Attack:
                    return new ActionDefinition(
                        PrimaryAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        MisaEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        MisaEffectContent.AttackTextTimingFor(profile.Id),
                        null,
                        null,
                        MinionDefenseContributionPolicy.IncludeAll);

                case MisaPrimaryKind.RangedAttack:
                    return new ActionDefinition(
                        PrimaryRangedAttack,
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.PrimaryValue) }
                            .Concat(PrintedIcon(profile)),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged,
                        default,
                        MisaEffectContent.AttackTextFor(profile.Id, ContentRevision),
                        profile.TextComposition,
                        PrintedDefenseResolution.None,
                        MisaEffectContent.AttackTextTimingFor(profile.Id),
                        null,
                        null,
                        MinionDefenseContributionPolicy.IncludeAll);

                case MisaPrimaryKind.Skill:
                    return new ActionDefinition(
                        PrimarySkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        PrintedIcon(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        MisaEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        profile.TextComposition);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(profile), profile.Primary, "Unknown Misa primary action kind.");
            }
        }

        /// <summary>
        /// p.9's Range or Radius icon where the face prints one, and nothing at all where it does not.
        /// Only seven of Misa's nineteen faces carry one; the inventory verified the other twelve's
        /// absence as a group rather than assuming it.
        /// </summary>
        private static IEnumerable<PrintedValueDefinition> PrintedIcon(MisaCardProfile profile) =>
            profile.IconChannel == PrintedValueChannel.None
                ? Enumerable.Empty<PrintedValueDefinition>()
                : new[] { new PrintedValueDefinition(profile.IconChannel, profile.IconValue) };

        /// <summary>Which primary action one of Misa's faces prints, as the inventory's band column.</summary>
        private enum MisaPrimaryKind
        {
            None = 0,
            Attack = 1,
            RangedAttack = 2,
            Skill = 3
        }

        /// <summary>
        /// One printed face, as the inventory's row. It is a private carrier rather than public
        /// content: what ships is <see cref="CardDefinition"/>, and this exists so the rows above read
        /// like the table they were transcribed from.
        /// </summary>
        private sealed class MisaCardProfile
        {
            public MisaCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                MisaPrimaryKind primary,
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
            public MisaPrimaryKind Primary { get; }
            public int PrimaryValue { get; }
            public PrintedValueChannel IconChannel { get; }
            public int IconValue { get; }
            public CardProgressionProfile Progression { get; }
            public string PrintedText { get; }
            public PrintedTextComposition TextComposition { get; }
        }
    }
}
