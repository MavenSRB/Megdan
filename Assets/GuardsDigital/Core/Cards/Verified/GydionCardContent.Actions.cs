using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.30i. Gydion's action fronts that cast - Cantrip, the fifteen coloured cards and the
    /// boxed Cheap Trick - read from the shard's inventory table and checked against each front
    /// (<c>active-hero-cards/Gydion/gydion*__front.png</c>): Initiative, the ribbon's Defense and
    /// Movement, the item icon, Tier and colour. No action card prints an Attack, Range or Radius value
    /// or icon: every value is on the spell. Prepare Spells and The Archwizard are 9.30g's
    /// (<c>GydionCardContent.Archwizard.cs</c>).
    ///
    /// <para><b>Every primary is a Skill that casts</b> (GY-Q3: the played card is a Skill; the spell
    /// is then performed as its own printed type). "Choose and cast one spell in the spellbook - X,
    /// Y[, Z]" is a <c>Choose</c> over one 9.30c cast arm per listed spell
    /// (<see cref="SyntheticSpellbookCapabilities.CastArm"/>, named by <see cref="InBookV1"/>), in
    /// printed order: a bullet whose spell is removed is greyed, an in-book spell is always castable
    /// (GY-Q12), so a card is greyed only when every spell it lists is removed. "Cast the X spell" is
    /// one cast arm as the root. The reminder text in parentheses composes to nothing.</para>
    /// </summary>
    public static partial class GydionCardContent
    {
        public static CardDefinitionId Cantrip { get; } = Id("cantrip");
        public static CardDefinitionId ElementaryEvocation { get; } = Id("elementary-evocation");
        public static CardDefinitionId LesserEvocation { get; } = Id("lesser-evocation");
        public static CardDefinitionId LesserNecromancy { get; } = Id("lesser-necromancy");
        public static CardDefinitionId GreaterEvocation { get; } = Id("greater-evocation");
        public static CardDefinitionId GreaterNecromancy { get; } = Id("greater-necromancy");
        public static CardDefinitionId ElementaryEnchantment { get; } = Id("elementary-enchantment");
        public static CardDefinitionId LesserEnchantment { get; } = Id("lesser-enchantment");
        public static CardDefinitionId LesserTransmutation { get; } = Id("lesser-transmutation");
        public static CardDefinitionId GreaterEnchantment { get; } = Id("greater-enchantment");
        public static CardDefinitionId GreaterTransmutation { get; } = Id("greater-transmutation");
        public static CardDefinitionId ElementaryAbjuration { get; } = Id("elementary-abjuration");
        public static CardDefinitionId LesserAbjuration { get; } = Id("lesser-abjuration");
        public static CardDefinitionId LesserConjuration { get; } = Id("lesser-conjuration");
        public static CardDefinitionId GreaterAbjuration { get; } = Id("greater-abjuration");
        public static CardDefinitionId GreaterConjuration { get; } = Id("greater-conjuration");
        public static CardDefinitionId CheapTrick { get; } = Id("cheap-trick");

        public static ActionDefinitionId SecondaryMovement { get; } = new ActionDefinitionId("secondary-movement");

        private const string ChooseAndCast = "Choose and cast one spell in the spellbook — ";

        public const string CantripText =
            ChooseAndCast + "• \"Magic Missile\" (To blast at short range!) • \"Expeditious Retreat\" " +
            "(To dart a great distance!) • \"Shocking Grasp\" (To repel an adjacent assailant!)";

        public const string ElementaryEvocationText =
            "Cast the \"Burning Hands\" spell in the spellbook. (Burn multiple opponents in near vicinity.)";

        public const string LesserEvocationText =
            ChooseAndCast + "• \"Burning Hands\" • \"Fireball\" (Wait for your allies to clear the " +
            "area before hurling a fireball!)";

        public const string LesserNecromancyText =
            ChooseAndCast + "• \"Vampiric Touch\" • \"Create Undead\" (Harm your foe to heal yourself. " +
            "Raise an undead servant.)";

        public const string GreaterEvocationText =
            ChooseAndCast + "• \"Burning Hands\" • \"Fireball\" • \"Sunburst\" (Sunburst's reach and " +
            "power grows with every cast spell.)";

        public const string GreaterNecromancyText =
            ChooseAndCast + "• \"Vampiric Touch\" • \"Create Undead\" • \"Energy Drain\" (Drain energy " +
            "from a foe in sight to restore your vitality.)";

        public const string ElementaryEnchantmentText =
            "Cast \"Suggestion\" spell in the spellbook. (Compel the opposing champion to take a few extra steps.)";

        public const string LesserEnchantmentText =
            ChooseAndCast + "• \"Suggestion\" • \"Dominate Person\" (Turn the dominated person against " +
            "their own troops.)";

        public const string LesserTransmutationText =
            ChooseAndCast + "• \"Midas Touch\" • \"Disintegrate\" (Create gold. Turn objects and lesser " +
            "creatures into dust.)";

        public const string GreaterEnchantmentText =
            ChooseAndCast + "• \"Suggestion\" • \"Dominate Person\" • \"Power Word Kill\" (Command a " +
            "weakened foe within earshot to perish instantly.)";

        public const string GreaterTransmutationText =
            ChooseAndCast + "• \"Midas Touch\" • \"Disintegrate\" • \"Polymorph\" (Polymorph enemies " +
            "into creatures or objects and vice versa.)";

        public const string ElementaryAbjurationText =
            "Cast the \"Shield\" spell in the spellbook. (The magic shield protects against mundane threats.)";

        public const string LesserAbjurationText =
            ChooseAndCast + "• \"Shield\" • \"Banishment\" (Touch anything to have it banished where " +
            "you please.)";

        public const string LesserConjurationText =
            ChooseAndCast + "• \"Find Familiar\" • \"Dimension Door\" (Summon a familiar to assist you. " +
            "Travel to a faraway place.)";

        public const string GreaterAbjurationText =
            ChooseAndCast + "• \"Shield\" • \"Banishment\" • \"Invulnerability\" (Invulnerability makes " +
            "you impervious to deadly perils.)";

        public const string GreaterConjurationText =
            ChooseAndCast + "• \"Find Familiar\" • \"Dimension Door\" • \"Cloud Kill\" (Create a deadly " +
            "cloud within distance to weaken your foe.)";

        public const string CheapTrickText =
            "Cast \"Expeditious Retreat\" spell in the spellbook. (To dart a great distance!)";

        /// <summary>
        /// The node a card's bullet that casts <paramref name="spell"/> is: the card's name and
        /// <c>-cast-</c> and the spell's, so a decision label and the decision window name it per card.
        /// </summary>
        public static EffectNodeId CastBullet(CardDefinitionId card, CardDefinitionId spell) =>
            new EffectNodeId(Suffix(card) + "-cast-" + Suffix(spell));

        /// <summary>The <c>Choose</c> a "Choose and cast one" card asks at.</summary>
        public static EffectNodeId CastChoice(CardDefinitionId card) =>
            new EffectNodeId(Suffix(card) + "-choose-and-cast-one");

        /// <summary>
        /// The spells each casting front lists, in printed order. Prepare Spells and The Archwizard
        /// cast through their own composition (9.30g).
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> SpellsListedOn(CardDefinitionId card) =>
            LazyListedSpells.Value.TryGetValue(card, out var spells)
                ? spells
                : throw new ArgumentException($"Gydion's '{card}' lists no spell to cast.", nameof(card));

        /// <summary>
        /// The words each "Choose and cast one" bullet says, DERIVED from the content: "Cast" and the
        /// name the spell prints (so Lesser Transmutation's "Midas Touch" reads "Cast Midas' Touch", the
        /// spell's own face). A "Cast the X spell" card asks nothing and declares nothing.
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> CastingCardDecisionLabels() =>
            LazyListedSpells.Value
                .Where(pair => pair.Value.Count > 1)
                .SelectMany(pair => pair.Value.Select(spell => DecisionLabelDeclaration.DecisionArm(
                    pair.Key, CastBullet(pair.Key, spell).Value, "Cast " + Definition(spell).DisplayName)));

        /// <summary>The seventeen casting fronts this file composes, in the shard's inventory order.</summary>
        public static IReadOnlyList<CardDefinition> CastingCardDefinitions => LazyCastingCards.Value;

        private static string Suffix(CardDefinitionId id) => id.Value.Substring(ContentSetId.Length + 1);

        private static readonly Lazy<IReadOnlyDictionary<CardDefinitionId, IReadOnlyList<CardDefinitionId>>>
            LazyListedSpells =
                new Lazy<IReadOnlyDictionary<CardDefinitionId, IReadOnlyList<CardDefinitionId>>>(() =>
                    new Dictionary<CardDefinitionId, IReadOnlyList<CardDefinitionId>>
                    {
                        [Cantrip] = new[] { MagicMissile, ExpeditiousRetreat, ShockingGrasp },
                        [ElementaryEvocation] = new[] { BurningHands },
                        [LesserEvocation] = new[] { BurningHands, Fireball },
                        [LesserNecromancy] = new[] { VampiricTouch, CreateUndead },
                        [GreaterEvocation] = new[] { BurningHands, Fireball, Sunburst },
                        [GreaterNecromancy] = new[] { VampiricTouch, CreateUndead, EnergyDrain },
                        [ElementaryEnchantment] = new[] { Suggestion },
                        [LesserEnchantment] = new[] { Suggestion, DominatePerson },
                        [LesserTransmutation] = new[] { MidasTouch, Disintegrate },
                        [GreaterEnchantment] = new[] { Suggestion, DominatePerson, PowerWordKill },
                        [GreaterTransmutation] = new[] { MidasTouch, Disintegrate, Polymorph },
                        [ElementaryAbjuration] = new[] { Shield },
                        [LesserAbjuration] = new[] { Shield, Banishment },
                        [LesserConjuration] = new[] { FindFamiliar, DimensionDoor },
                        [GreaterAbjuration] = new[] { Shield, Banishment, Invulnerability },
                        [GreaterConjuration] = new[] { FindFamiliar, DimensionDoor, CloudKill },
                        [CheapTrick] = new[] { ExpeditiousRetreat }
                    });

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyCastingCards =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                // Gold, basic: Initiative 11, Defense 1.
                Basic(Cantrip, "Cantrip", 11, 1, CantripText),

                // Red: Evocation and Necromancy.
                Coloured(ElementaryEvocation, "Elementary Evocation", CardColor.Red, CardTier.I, 7, 6, 3,
                    PrintedValueChannel.None, new[] { LesserEvocation, LesserNecromancy }, ElementaryEvocationText),
                Coloured(LesserEvocation, "Lesser Evocation", CardColor.Red, CardTier.II, 8, 6, 3,
                    PrintedValueChannel.Defense, new[] { GreaterEvocation, GreaterNecromancy }, LesserEvocationText),
                Coloured(LesserNecromancy, "Lesser Necromancy", CardColor.Red, CardTier.II, 8, 6, 3,
                    PrintedValueChannel.Initiative, new[] { GreaterEvocation, GreaterNecromancy }, LesserNecromancyText),
                Coloured(GreaterEvocation, "Greater Evocation", CardColor.Red, CardTier.III, 8, 7, 3,
                    PrintedValueChannel.Defense, Array.Empty<CardDefinitionId>(), GreaterEvocationText),
                Coloured(GreaterNecromancy, "Greater Necromancy", CardColor.Red, CardTier.III, 8, 7, 3,
                    PrintedValueChannel.Radius, Array.Empty<CardDefinitionId>(), GreaterNecromancyText),

                // Blue: Enchantment and Transmutation.
                Coloured(ElementaryEnchantment, "Elementary Enchantment", CardColor.Blue, CardTier.I, 9, 3, 2,
                    PrintedValueChannel.None, new[] { LesserEnchantment, LesserTransmutation },
                    ElementaryEnchantmentText),
                Coloured(LesserEnchantment, "Lesser Enchantment", CardColor.Blue, CardTier.II, 10, 4, 2,
                    PrintedValueChannel.Defense, new[] { GreaterEnchantment, GreaterTransmutation },
                    LesserEnchantmentText),
                Coloured(LesserTransmutation, "Lesser Transmutation", CardColor.Blue, CardTier.II, 10, 4, 2,
                    PrintedValueChannel.Attack, new[] { GreaterEnchantment, GreaterTransmutation },
                    LesserTransmutationText),
                Coloured(GreaterEnchantment, "Greater Enchantment", CardColor.Blue, CardTier.III, 10, 4, 2,
                    PrintedValueChannel.Range, Array.Empty<CardDefinitionId>(), GreaterEnchantmentText),
                Coloured(GreaterTransmutation, "Greater Transmutation", CardColor.Blue, CardTier.III, 10, 4, 2,
                    PrintedValueChannel.Attack, Array.Empty<CardDefinitionId>(), GreaterTransmutationText),

                // Green: Abjuration and Conjuration.
                Coloured(ElementaryAbjuration, "Elementary Abjuration", CardColor.Green, CardTier.I, 5, 2, 2,
                    PrintedValueChannel.None, new[] { LesserAbjuration, LesserConjuration }, ElementaryAbjurationText),
                Coloured(LesserAbjuration, "Lesser Abjuration", CardColor.Green, CardTier.II, 4, 3, 2,
                    PrintedValueChannel.Initiative, new[] { GreaterAbjuration, GreaterConjuration },
                    LesserAbjurationText),
                Coloured(LesserConjuration, "Lesser Conjuration", CardColor.Green, CardTier.II, 4, 3, 2,
                    PrintedValueChannel.Attack, new[] { GreaterAbjuration, GreaterConjuration },
                    LesserConjurationText),
                Coloured(GreaterAbjuration, "Greater Abjuration", CardColor.Green, CardTier.III, 4, 3, 2,
                    PrintedValueChannel.Initiative, Array.Empty<CardDefinitionId>(), GreaterAbjurationText),
                Coloured(GreaterConjuration, "Greater Conjuration", CardColor.Green, CardTier.III, 4, 3, 2,
                    PrintedValueChannel.Movement, Array.Empty<CardDefinitionId>(), GreaterConjurationText),

                // Gold, p.20 Handicap (boxed at setup): Initiative 10, Defense 1.
                Basic(CheapTrick, "Cheap Trick", 10, 1, CheapTrickText)
            }));

        /// <summary>A Gold basic: "Basic Skill" and the ribbon's Defense; no Movement.</summary>
        private static CardDefinition Basic(
            CardDefinitionId card, string name, int initiative, int defense, string text) =>
            new CardDefinition(
                card, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                new[] { CastingSkill(card), SecondaryDefenseOf(defense) },
                CardSourceContent.Transcribed(text));

        /// <summary>A coloured Tier card: the casting Skill, the ribbon's Movement and Defense, its item.</summary>
        private static CardDefinition Coloured(
            CardDefinitionId card, string name, CardColor color, CardTier tier, int initiative,
            int defense, int movement, PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next,
            string text) =>
            new CardDefinition(
                card, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(color, tier, item, next),
                new[]
                {
                    CastingSkill(card),
                    new ActionDefinition(
                        SecondaryMovement, ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                        new[] { Value(PrintedValueChannel.Movement, movement) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),
                    SecondaryDefenseOf(defense)
                },
                CardSourceContent.Transcribed(text));

        /// <summary>
        /// The printed Skill: one cast arm as the root when the card names one spell, otherwise a
        /// mandatory <c>Choose</c> (one of them, the actor choosing) over a cast arm per listed spell.
        /// </summary>
        private static ActionDefinition CastingSkill(CardDefinitionId card)
        {
            var spells = SpellsListedOn(card);
            EffectNodeId root;
            var nodes = new List<EffectNodeDefinition>();
            if (spells.Count == 1)
            {
                root = CastBullet(card, spells[0]);
            }
            else
            {
                root = CastChoice(card);
                nodes.Add(new EffectNodeDefinition(root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: spells.Select(spell => CastBullet(card, spell)),
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 1,
                    maximumSelections: 1));
            }

            nodes.AddRange(spells.SelectMany(spell =>
                SyntheticSpellbookCapabilities.CastArm(CastBullet(card, spell), InBookV1(spell))));
            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default,
                new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"), ContentRevision, root, nodes));
        }

        private static ActionDefinition SecondaryDefenseOf(int defense) =>
            new ActionDefinition(
                SecondaryDefense, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
                new[] { Value(PrintedValueChannel.Defense, defense) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));
    }
}
