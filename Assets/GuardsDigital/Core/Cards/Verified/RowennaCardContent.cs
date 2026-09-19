using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Rowenna the Vanguard's printed faces as definition content - milestone 8.57a, and the eighteenth
    /// hero this project builds.
    ///
    /// <para><b>This file is deliberately partial, and it says so rather than pretending otherwise.</b>
    /// Her shard records nineteen printed faces; 8.57a composes the FIVE whose clauses this block owns
    /// - Code of Chivalry and the four Reds that reward a team-mate after the attack - and defines no
    /// others. <see cref="AllDefinitions"/> is therefore five cards and not nineteen, there is no
    /// starting hand, no deck, no boxed Handicap and no dealt set: p.5's shape is a statement about a
    /// complete set, and stating it over a fifth of one would be false. Milestone 8.57f composes the
    /// remaining fourteen and adds those members; 8.57g deals her.</para>
    ///
    /// <para><b>The revision is a placeholder and is not a valid content identity.</b>
    /// <see cref="ContentRevision"/> reads <c>rowenna-8-57a-prep</c>, which follows no dealt hero's
    /// <c>&lt;hero&gt;-&lt;milestone&gt;-rev-&lt;n&gt;</c> spelling on purpose: the project's
    /// placeholder rule is that a placeholder must not be a value that could be mistaken for a real
    /// one, and the real revision is minted at 8.57f/8.57g where the whole set moves together.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <see cref="MatchContentCatalogs"/> names no Rowenna
    /// content, so no configured match can deal these five cards and nothing reachable can start them.
    /// That is milestone 7.9's gate rather than an oversight - the registered content list is
    /// serialized, and registering a card before it can be dealt moves the pinned list, every
    /// snapshot's bytes and every corpus digest while the content never moved.</para>
    ///
    /// <para><b>Where the numbers come from.</b> Every value, word, colour, Tier, icon and upgrade pair
    /// below is transcribed from the extracted card faces at milestone 8.57's inventory pass and
    /// recorded in the <c>Docs/AI/Rules/HERO_ROWENNA.md</c> shard. The table there and the rows here
    /// are meant to be read side by side. Nothing is derived.</para>
    ///
    /// <para><b>RO-03 is open and these five faces are not touched by it.</b> The printed colour
    /// conflict her shard holds for the user is on Accept Surrender, Glorious Triumph, Melee and Grand
    /// Melee; every face composed here prints a corner letter that agrees with its own ribbon.</para>
    /// </summary>
    public static class RowennaCardContent
    {
        /// <summary>
        /// The revision these five definitions carry. A PLACEHOLDER, deliberately not shaped like a
        /// dealt hero's revision - see the type's own remarks.
        /// </summary>
        public const string ContentRevision = "rowenna-8-57a-prep";

        /// <summary>
        /// The hero this content belongs to, as p.5's content-set identity - the printed set a seat
        /// deals from rather than the seat itself.
        /// </summary>
        public const string ContentSetId = "rowenna";

        // -----------------------------------------------------------------------------------------
        // Stable definition identities for the five faces this block composes. Named rather than
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

        // -----------------------------------------------------------------------------------------
        // Local action identities, local to a card exactly as every earlier set's are.
        // -----------------------------------------------------------------------------------------

        public static ActionDefinitionId PrimaryAttack { get; } =
            new ActionDefinitionId("primary-attack");

        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAllDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() =>
                Array.AsReadOnly(Profiles().Select(Build).ToArray()));

        /// <summary>
        /// The five faces this block composes, in her shard's printed-value table order.
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
                $"Milestone 8.57a composes no Rowenna card '{id}'.", nameof(id));

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
                "retrieve a discarded card; If they do, repeat once on a different target.")
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
            // Every one of these five faces prints an Attack primary, and every one of them prints
            // "Target a unit adjacent to you" - which is p.13's own reach for a non-Ranged Attack,
            // "even if the card has a radius icon". So the target policy is the page's default,
            // supplied by ActionDefinition rather than restated here, and the Radius icon below is
            // read by the card TEXT's own selection and by nothing else.
            yield return new ActionDefinition(
                PrimaryAttack,
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, profile.Attack) }
                    .Concat(PrintedRadius(profile)),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.NonRanged,
                default,
                RowennaEffectContent.AttackTextFor(profile.Id, ContentRevision),
                PrintedTextComposition.None,
                PrintedDefenseResolution.None,
                RowennaEffectContent.AttackTextTimingFor(profile.Id));

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
        /// p.9's Radius icon, or nothing when the face prints none. Code of Chivalry is the one face
        /// here that prints neither a Range nor a Radius, and that absence is content: its rider names
        /// the Attack's own target rather than reaching for anybody at a distance.
        /// </summary>
        private static IEnumerable<PrintedValueDefinition> PrintedRadius(RowennaCardProfile profile) =>
            profile.Radius > 0
                ? new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, profile.Radius) }
                : Enumerable.Empty<PrintedValueDefinition>();

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
                string printedText)
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
        }
    }
}
