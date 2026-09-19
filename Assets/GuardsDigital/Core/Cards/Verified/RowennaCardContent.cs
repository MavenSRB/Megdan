using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Rowenna the Vanguard's printed faces as definition content - milestones 8.57a, 8.57b and
    /// 8.57c, and the eighteenth hero this project builds.
    ///
    /// <para><b>This file is deliberately partial, and it says so rather than pretending otherwise.</b>
    /// Her shard records nineteen printed faces. 8.57a composed the SIX whose clauses that block owned
    /// - Code of Chivalry and the five Reds that reward a team-mate after the attack - 8.57b added
    /// FOUR more - her Silver basic Throw the Gauntlet and the three Blue faces that swap with a
    /// friendly unit - and 8.57c adds the TWO Blue faces that defeat a surrendering enemy.
    /// <see cref="AllDefinitions"/> is therefore twelve cards and not nineteen, there is no starting
    /// hand, no deck, no boxed Handicap and no dealt set: p.5's shape is a statement about a complete
    /// set, and stating it over two thirds of one would be false. Milestone 8.57f composes the
    /// remaining seven and adds those members; 8.57g deals her.</para>
    ///
    /// <para><b>Her whole Blue ladder is composed, so both of its upgrade pairs are now stated
    /// whole.</b> Stand Guard upgrades to Protect the Weak or Accept Surrender, and each of those to
    /// Defend the Innocent or Glorious Triumph. 8.57b could name only the sibling that existed at the
    /// time; nothing on the ladder is deferred any more. Her Red ladder is whole too; what 8.57f still
    /// owes is the Green one, the Ultimate and the boxed Handicap.</para>
    ///
    /// <para><b>The revision is a placeholder and is not a valid content identity.</b>
    /// <see cref="ContentRevision"/> reads <c>rowenna-8-57a-prep</c>, which follows no dealt hero's
    /// <c>&lt;hero&gt;-&lt;milestone&gt;-rev-&lt;n&gt;</c> spelling on purpose: the project's
    /// placeholder rule is that a placeholder must not be a value that could be mistaken for a real
    /// one, and the real revision is minted at 8.57f/8.57g where the whole set moves together.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <see cref="MatchContentCatalogs"/> names no Rowenna
    /// content, so no configured match can deal these ten cards and nothing reachable can start them.
    /// That is milestone 7.9's gate rather than an oversight - the registered content list is
    /// serialized, and registering a card before it can be dealt moves the pinned list, every
    /// snapshot's bytes and every corpus digest while the content never moved.</para>
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at milestone 8.57's inventory pass and
    /// recorded in the <c>Docs/AI/Rules/HERO_ROWENNA.md</c> shard. The table there and the rows here
    /// are meant to be read side by side. Nothing is derived.</para>
    ///
    /// <para><b>RO-03 is open and TWO of these twelve faces are touched by it.</b> The printed colour
    /// conflict her shard holds for the user is on Accept Surrender, Glorious Triumph, Melee and Grand
    /// Melee - each carries a ribbon, band and frame of one colour and a corner letter of another. The
    /// shard's working assumption is the RIBBON, which makes both of this block's faces Blue, and the
    /// rows below state that. What the open question can move is which colour's p.15 ladder they sit
    /// on and nothing else: their text, values, Initiative, item and Tier are printed and read, and
    /// the two Defeat bodies do not consult a colour at all. Milestone 8.57f states the whole set and
    /// is where a user answer would land.</para>
    /// </summary>
    public static class RowennaCardContent
    {
        /// <summary>
        /// The revision these ten definitions carry. A PLACEHOLDER, deliberately not shaped like a
        /// dealt hero's revision - see the type's own remarks.
        /// </summary>
        public const string ContentRevision = "rowenna-8-57a-prep";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself.
        /// </summary>
        public const string ContentSetId = "rowenna";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities for the faces these two blocks compose. Named rather than
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

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAllDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() =>
                Array.AsReadOnly(Profiles().Select(Build).ToArray()));

        /// <summary>
        /// The twelve faces these three blocks compose, in her shard's printed-value table order.
        ///
        /// <para>Built lazily, which is the convention milestone 8.38d adopted after a
        /// static-initialization cycle cost milestone 8.37 1,423 failing cases on one
        /// <c>NullReferenceException</c> and then cost a later session <c>NUnit failed to load</c> with
        /// zero tests discovered while <c>dotnet build</c> reported no errors at all.</para>
        /// </summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAllDefinitions.Value;

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.FirstOrDefault(definition => definition.Id.Equals(id)) ??
            throw new ArgumentException(
                $"Milestones 8.57a, 8.57b and 8.57c compose no Rowenna card '{id}'.", nameof(id));

        private static CardDefinitionId Id(string cardId) =>
            new CardDefinitionId($"{ContentSetId}-{cardId}");

        // -----------------------------------------------------------------------------------------
        // The printed faces. One row per card, in the shard's table order, with the same columns:
        // Initiative, the two secondary icons, the primary Attack value, the p.9 Radius icon (0 where
        // the face prints none), the progression content and the transcribed text.
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
                RowennaPrimaryKind.Skill)
        };

        private static CardDefinition Build(RowennaCardProfile profile) =>
            new CardDefinition(
                profile.Id,
                ContentRevision,
                profile.DisplayName,
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                profile.Progression,
                BuildActions(profile),
                CardSourceContent.Transcribed(profile.PrintedText));

        private static IEnumerable<ActionDefinition> BuildActions(RowennaCardProfile profile)
        {
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
                        PrintedRange(profile),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        RowennaEffectContent.SkillEffectFor(profile.Id, ContentRevision),
                        PrintedTextComposition.None);

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
                        PrintedTextComposition.None,
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
        /// Milestone 8.57b. Which band a composed face prints its primary action in. Her Green
        /// Movement primaries and her Purple Ultimate join at 8.57d and 8.57e.
        /// </summary>
        private enum RowennaPrimaryKind
        {
            /// <summary>p.9's Gold band with an Attack primary, and her five coloured Reds.</summary>
            Attack = 0,

            /// <summary>p.9's Silver band with a Skill primary - "BASIC SKILL - RANGED".</summary>
            BasicSkill = 1,

            /// <summary>A coloured Skill primary - her three composed Blues.</summary>
            Skill = 2
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
                int range = 0)
            {
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
        }
    }
}
