using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's three Blue mine faces, as printed. Milestone 8.61d.
    ///
    /// <para>The rows are the shard's, unchanged: Trip Mine (Blue I) Initiative 10, Defense 4, Movement
    /// 2, Skill, Radius 3, no item; Cluster Mine (Blue II) 11 / 4 / 2, Radius 3, Attack item; Mine
    /// Field (Blue III) 11 / 5 / 2, Radius 3, Attack item. A missing icon is absent, never zero - none
    /// of the three prints a Range, and only Trip Mine prints no Item.</para>
    ///
    /// <para><b>Each face's primary carries its placements and nothing else.</b> The trap half of the
    /// printed sentence is milestone 8.61e's, so all three declare
    /// <c>PrintedTextComposition.DeferredUntil</c> from <see cref="MinEffectContent.MineCompositionOwed"/>
    /// - the one list, read here and written there, so a face cannot be marked composed on the
    /// definition while the effect content returns half of the text.</para>
    ///
    /// <para><b>The upgrade pairs are deliberately empty.</b> p.15 upgrades Trip Mine to Cluster Mine
    /// or Death Grenade and Cluster Mine to Mine Field or Holy Death Grenade, and both Grenades are
    /// milestone 8.61f's definitions. A half-written pair would be a printed claim this block cannot
    /// make; milestone 8.61i composes her set and owns the progression for all nineteen faces at once.
    /// The colour, the tier and the Item icon are printed on these cards and are stated here.</para>
    /// </summary>
    public static partial class MinCardContent
    {
        /// <summary>Her content-set prefix, block-scoped until milestone 8.61i hoists one.</summary>
        private const string MineContentSetId = "min";

        public static CardDefinitionId TripMine { get; } = MineId("trip-mine");

        public static CardDefinitionId ClusterMine { get; } = MineId("cluster-mine");

        public static CardDefinitionId MineField { get; } = MineId("mine-field");

        /// <summary>The three faces this block composes, in printed tier order.</summary>
        public static IReadOnlyList<CardDefinitionId> MineFaces { get; } =
            System.Array.AsReadOnly(new[] { TripMine, ClusterMine, MineField });

        /// <summary>
        /// The three definitions. Not registered and not dealt: milestone 8.61i composes her set and
        /// 8.61j deals her, so nothing here reaches <c>MatchContentCatalogs.ForThisBuild</c>.
        /// </summary>
        public static IReadOnlyList<CardDefinition> MineDefinitions() => LazyMineDefinitions.Value;

        /// <summary>One mine face by ID; it throws for a card this block does not own.</summary>
        public static CardDefinition MineDefinition(CardDefinitionId cardId) =>
            MineDefinitions().Single(definition => definition.Id.Equals(cardId));

        // MERGE NOTE (8.61d -> main): the class aggregate lives in MinCardContent.SmokeBomb.cs
        // (LazyAllDefinitions concatenates MineDefinitions()) until 8.61i mints the real set; the
        // AllDefinitions/BlockDefinitions this partial carried on its branch are gone.

        private static readonly System.Lazy<IReadOnlyList<CardDefinition>> LazyMineDefinitions =
            new System.Lazy<IReadOnlyList<CardDefinition>>(() =>
                System.Array.AsReadOnly(MineProfiles().Select(Definition).ToArray()));

        private static CardDefinitionId MineId(string cardId) =>
            new CardDefinitionId($"{MineContentSetId}-{cardId}");

        private static IEnumerable<MineCardProfile> MineProfiles() => new[]
        {
            new MineCardProfile(
                TripMine, "Trip Mine", 10, 4, 2, CardTier.I, PrintedValueChannel.None,
                "Place 2 mine tokens, 1 blast and 1 dud, facedown in radius; units may move through " +
                "them. After an enemy hero moves through a mine, remove it; for every blast removed, " +
                "that hero discards a card, if able."),

            new MineCardProfile(
                ClusterMine, "Cluster Mine", 11, 4, 2, CardTier.II, PrintedValueChannel.Attack,
                "Place 3 Mine tokens, 1 blast and 2 duds, facedown in radius; units may move through " +
                "them; remove mine tokens after an enemy hero moves through them; for every blast " +
                "removed, that hero discards a card, if able."),

            new MineCardProfile(
                MineField, "Mine Field", 11, 5, 2, CardTier.III, PrintedValueChannel.Attack,
                "Place 3 Mine tokens, 2 blasts and 1 dud, facedown in radius; units may move through " +
                "them; remove mine tokens after an enemy hero moves through them; for every blast " +
                "removed, that hero discards a card, if able.")
        };

        private static CardDefinition Definition(MineCardProfile profile) =>
            new CardDefinition(
                profile.Id,
                MinEffectContent.MineRevision,
                profile.DisplayName,
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, profile.Tier, profile.ItemIcon),
                MineActions(profile),
                CardSourceContent.Transcribed(profile.PrintedText));

        private static IEnumerable<ActionDefinition> MineActions(MineCardProfile profile) => new[]
        {
            // The primary. p.9's Radius icon is what "in radius" on all three faces reads, and it is a
            // Skill rather than a Ranged one: AttackRangeKind classifies an Attack and says nothing
            // about a Skill, whose reach is the printed icon alone.
            new ActionDefinition(
                MinePrimarySkill,
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, MineRadius) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                // PrintedTextComposition.None, and the reason is a fact about the printed sentence
                // rather than a claim that nothing is owed. The trap - "After an enemy hero moves
                // through a mine, remove it; ..." - can never be a STEP of this Skill: it fires on a
                // route somebody else walks on a later turn, so it is a standing rule the placed piece
                // carries and milestone 8.61e hangs it off the board authority's own displacement
                // commits. This action's own steps ARE fully composed, and the engine refuses an
                // action that carries both an effect root and an outstanding composition precisely
                // because that pair would be two answers to one question. The debt is written down as
                // what it is - a token-anchored rule - in MinEffectContent.MineTrapOwed.
                MinEffectContent.MineSkillEffectFor(profile.Id, MinEffectContent.MineRevision)),

            new ActionDefinition(
                MineSecondaryMovement,
                ActionKind.Movement,
                ActionDefinitionOrigin.PrintedSecondary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, profile.SecondaryMovement) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),

            new ActionDefinition(
                MineSecondaryDefense,
                ActionKind.Defense,
                ActionDefinitionOrigin.Reaction,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, profile.SecondaryDefense) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
        };

        /// <summary>The one Radius all three mine faces print.</summary>
        private const int MineRadius = 3;

        /// <summary>
        /// The primary action every mine face declares. It is named block-scoped rather than
        /// <c>PrimarySkill</c> so that the four sibling 8.61x blocks can each declare their own faces
        /// without two partial files claiming one member; the VALUE is the same "primary-skill" every
        /// hero in the roster prints, and milestone 8.61i hoists one declaration for her whole set.
        /// </summary>
        public static ActionDefinitionId MinePrimarySkill { get; } =
            new ActionDefinitionId("primary-skill");

        private static ActionDefinitionId MineSecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");

        private static ActionDefinitionId MineSecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        private sealed class MineCardProfile
        {
            public MineCardProfile(
                CardDefinitionId id,
                string displayName,
                int initiative,
                int secondaryDefense,
                int secondaryMovement,
                CardTier tier,
                PrintedValueChannel itemIcon,
                string printedText)
            {
                Id = id;
                DisplayName = displayName;
                Initiative = initiative;
                SecondaryDefense = secondaryDefense;
                SecondaryMovement = secondaryMovement;
                Tier = tier;
                ItemIcon = itemIcon;
                PrintedText = printedText;
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public CardTier Tier { get; }
            public PrintedValueChannel ItemIcon { get; }
            public string PrintedText { get; }
        }
    }
}
