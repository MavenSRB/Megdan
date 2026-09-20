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
    /// <para><b>Each face's primary carries its placements and nothing else, and that is the whole
    /// text.</b> The trap half of the printed sentence - "After an enemy hero moves through a mine,
    /// remove it; for every blast removed, that hero discards a card, if able" - can never be a STEP of
    /// this Skill, because it fires on a route another hero walks on a later turn. Milestone 8.61e
    /// built it where it belongs, on the placed piece: the two token properties and the blast's
    /// consequence list in <see cref="MinComponentContent"/>, applied by
    /// <c>MoveThroughTrapOperation</c> off the board's own displacements.
    /// <see cref="MinEffectContent.MineTrapLivesOn"/> is that statement, per face, so a reader of these
    /// definitions is told where the rest of the sentence went.</para>
    ///
    /// <para><b>The Blue ladder is whole here since milestone 8.61i.</b> p.15 upgrades Trip Mine to
    /// Cluster Mine or Death Grenade and Cluster Mine to Mine Field or Holy Death Grenade, and both
    /// Grenades are milestone 8.61f's definitions - so while the two branches ran in parallel this
    /// block's pairs were deliberately EMPTY rather than half-written, and the block that composed her
    /// set completed them. The colour, the tier and the Item icon are printed on these cards and were
    /// always stated here.</para>
    /// </summary>
    public static partial class MinCardContent
    {
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

        private static readonly System.Lazy<IReadOnlyList<CardDefinition>> LazyMineDefinitions =
            new System.Lazy<IReadOnlyList<CardDefinition>>(() =>
                System.Array.AsReadOnly(MineProfiles().Select(Definition).ToArray()));

        private static CardDefinitionId MineId(string cardId) => Id(cardId);

        // The transcriptions carry the printed [Mine] glyph exactly as her shard's "Exact printed
        // rules" write it, and as her Smoke bomb and Grenade faces already wrote theirs. Milestone
        // 8.61d left the two glyphs out of all three sentences; milestone 8.61i put them back, because
        // the shard is the source and a transcription is the claim CardSourceContent.Transcribed makes.
        private static IEnumerable<MineCardProfile> MineProfiles() => new[]
        {
            new MineCardProfile(
                TripMine, "Trip Mine", 10, 4, 2, CardTier.I, PrintedValueChannel.None,
                "Place 2 mine tokens, 1 [Mine] blast and 1 [Mine] dud, facedown in radius; units may " +
                "move through them. After an enemy hero moves through a mine, remove it; for every " +
                "blast removed, that hero discards a card, if able.",
                // p.15's fork, whole since 8.61i: Cluster Mine is this block's and Death Grenade is
                // 8.61f's, and a Tier I names both of its colour's Tier II faces.
                ClusterMine, DeathGrenade),

            new MineCardProfile(
                ClusterMine, "Cluster Mine", 11, 4, 2, CardTier.II, PrintedValueChannel.Attack,
                "Place 3 Mine tokens, 1 [Mine] blast and 2 [Mine] duds, facedown in radius; units may " +
                "move through them; remove mine tokens after an enemy hero moves through them; for " +
                "every blast removed, that hero discards a card, if able.",
                MineField, HolyDeathGrenade),

            new MineCardProfile(
                MineField, "Mine Field", 11, 5, 2, CardTier.III, PrintedValueChannel.Attack,
                "Place 3 Mine tokens, 2 [Mine] blasts and 1 [Mine] dud, facedown in radius; units may " +
                "move through them; remove mine tokens after an enemy hero moves through them; for " +
                "every blast removed, that hero discards a card, if able.")
        };

        private static CardDefinition Definition(MineCardProfile profile) =>
            new CardDefinition(
                profile.Id,
                ContentRevision,
                profile.DisplayName,
                new InitiativeValue(profile.Initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, profile.Tier, profile.ItemIcon,
                    profile.NextTierCandidates.ToArray()),
                MineActions(profile),
                CardSourceContent.Transcribed(profile.PrintedText));

        private static IEnumerable<ActionDefinition> MineActions(MineCardProfile profile) => new[]
        {
            // The primary. p.9's Radius icon is what "in radius" on all three faces reads, and it is a
            // Skill rather than a Ranged one: AttackRangeKind classifies an Attack and says nothing
            // about a Skill, whose reach is the printed icon alone.
            new ActionDefinition(
                PrimarySkill,
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, MineRadius) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                // PrintedTextComposition.None, and the reason is a fact about the printed sentence
                // rather than a claim that something is owed. The trap - "After an enemy hero moves
                // through a mine, remove it; ..." - can never be a STEP of this Skill: it fires on a
                // route somebody else walks on a later turn, so it is a standing rule the placed piece
                // carries and milestone 8.61e hung it off the board authority's own displacement
                // commits. This action's own steps ARE fully composed, and the engine refuses an
                // action that carries both an effect root and an outstanding composition precisely
                // because that pair would be two answers to one question. Where the other half went is
                // written down, per face, in MinEffectContent.MineTrapLivesOn.
                MinEffectContent.MineSkillEffectFor(profile.Id, ContentRevision)),

            new ActionDefinition(
                SecondaryMovement,
                ActionKind.Movement,
                ActionDefinitionOrigin.PrintedSecondary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, profile.SecondaryMovement) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),

            new ActionDefinition(
                SecondaryDefense,
                ActionKind.Defense,
                ActionDefinitionOrigin.Reaction,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, profile.SecondaryDefense) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
        };

        /// <summary>The one Radius all three mine faces print.</summary>
        private const int MineRadius = 3;

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
                string printedText,
                params CardDefinitionId[] nextTierCandidates)
            {
                Id = id;
                DisplayName = displayName;
                Initiative = initiative;
                SecondaryDefense = secondaryDefense;
                SecondaryMovement = secondaryMovement;
                Tier = tier;
                ItemIcon = itemIcon;
                PrintedText = printedText;
                NextTierCandidates = System.Array.AsReadOnly(nextTierCandidates);
            }

            public CardDefinitionId Id { get; }
            public string DisplayName { get; }
            public int Initiative { get; }
            public int SecondaryDefense { get; }
            public int SecondaryMovement { get; }
            public CardTier Tier { get; }
            public PrintedValueChannel ItemIcon { get; }
            public string PrintedText { get; }

            /// <summary>
            /// p.15's two candidates of the next Tier, whole since milestone 8.61i: each Blue pair
            /// names one face of this block and one of 8.61f's two Grenades.
            /// </summary>
            public IReadOnlyList<CardDefinitionId> NextTierCandidates { get; }
        }
    }
}
