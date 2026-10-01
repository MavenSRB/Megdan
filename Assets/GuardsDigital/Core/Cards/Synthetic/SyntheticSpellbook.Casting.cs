using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Synthetic
{
    /// <summary>
    /// Milestone 9.30c. Test-only spells that ACT, and the cards that cast them and refill the book.
    /// Fixture content, not a verified card or a rule-source interpretation; no Gydion face is
    /// registered.
    ///
    /// <para><b>The three spells</b> are one of each printed primary shape a spell takes: a basic Gold
    /// Attack (melee, Attack <see cref="AttackSpellValue"/>), a Tier I Red Movement (Movement
    /// <see cref="MovementSpellValue"/>), and a Tier II Blue Skill whose text is a "This round" Active
    /// effect (+1 Defense) that ends when the spell changes state - the shape of Shield and
    /// Invulnerability (GY-05).</para>
    ///
    /// <para><b>The three casting cards</b> are Skills: a "Choose and cast one" over the three spells
    /// (<see cref="CantripId"/>), a single "Cast the Attack spell" (<see cref="EvocationId"/>), and
    /// Prepare Spells' "Add all spell cards to the spellbook" (<see cref="PrepareSpellsId"/>).</para>
    ///
    /// <para><b>Unregistered.</b> Each spell's naming step and the Ward's Active effect are declared
    /// here with the spells they belong to, and handed to a fixture's own catalog through
    /// <see cref="CastingHandlers"/>; the build registers them only with a dealt hero.</para>
    /// </summary>
    public static partial class SyntheticSpellbook
    {
        public const string CastingContentRevision = "9.30c-rev-1";

        public const int AttackSpellValue = 3;
        public const int MovementSpellValue = 3;

        public static readonly CardDefinitionId AttackSpellId = new CardDefinitionId("synthetic-spell-attack");
        public static readonly CardDefinitionId MovementSpellId = new CardDefinitionId("synthetic-spell-movement");
        public static readonly CardDefinitionId WardSpellId = new CardDefinitionId("synthetic-spell-ward");

        public static readonly CardDefinitionId CantripId = new CardDefinitionId("synthetic-cantrip");
        public static readonly CardDefinitionId EvocationId = new CardDefinitionId("synthetic-evocation");
        public static readonly CardDefinitionId PrepareSpellsId = new CardDefinitionId("synthetic-prepare-spells");

        /// <summary>Every casting card's one printed primary, a Skill.</summary>
        public static readonly ActionDefinitionId CastingPrimaryId = new ActionDefinitionId("primary-skill");

        /// <summary>The spells' own printed primaries.</summary>
        public static readonly ActionDefinitionId SpellPrimaryId = new ActionDefinitionId("spell-primary");

        /// <summary>The Cantrip's three bullets, in printed order.</summary>
        public static readonly EffectNodeId CastAttackArm = new EffectNodeId("cast-attack-spell");
        public static readonly EffectNodeId CastMovementArm = new EffectNodeId("cast-movement-spell");
        public static readonly EffectNodeId CastWardArm = new EffectNodeId("cast-ward-spell");

        public static EffectCapabilityReference AttackSpellInBookV1 => Reference("synthetic-attack-spell-in-book");
        public static EffectCapabilityReference MovementSpellInBookV1 => Reference("synthetic-movement-spell-in-book");
        public static EffectCapabilityReference WardSpellInBookV1 => Reference("synthetic-ward-spell-in-book");
        public static EffectCapabilityReference GrantWardV1 => Reference("synthetic-grant-ward");

        /// <summary>
        /// "This round: +1 Defense", cancelled when its source card changes state (p.17) - so a refill
        /// that returns the Ward spell to the book ends it (GY-05 designer) - and when its controller
        /// is defeated.
        /// </summary>
        public static ActiveEffectDefinition WardDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-ward-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.ThisRound,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.RoundEnd,
            new[]
            {
                ActiveEffectCancellationCondition.SourceCardStateChanged,
                ActiveEffectCancellationCondition.ControllerHeroDefeated
            },
            false,
            "SyntheticWard",
            "synthetic-ward",
            Array.Empty<TimingCheckpoint>(),
            new[] { RuleModification.ForValue(PrintedValueChannel.Defense, 1) });

        /// <summary>The three spells that act, in book order.</summary>
        public static IReadOnlyList<CardDefinition> CastingSpellDefinitions => LazyCastingSpells.Value;

        /// <summary>The Cantrip, the Evocation and Prepare Spells.</summary>
        public static IReadOnlyList<CardDefinition> CastingCardDefinitions => LazyCastingCards.Value;

        /// <summary>
        /// The handlers this content declares and a fixture's catalog must carry: each spell's naming
        /// step, the Ward's Active effect, and the cast and the refill themselves.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> CastingHandlers() =>
            new[]
            {
                SyntheticSpellbookCapabilities.SpellInBookHandler(AttackSpellInBookV1, AttackSpellId),
                SyntheticSpellbookCapabilities.SpellInBookHandler(MovementSpellInBookV1, MovementSpellId),
                SyntheticSpellbookCapabilities.SpellInBookHandler(WardSpellInBookV1, WardSpellId),
                new ActiveEffectCreationHandler(GrantWardV1, WardDefinition)
            }.Concat(SyntheticSpellbookCapabilities.DeferredHandlers());

        /// <summary>One instance of each acting spell, owned by <paramref name="ownerId"/>, in the book.</summary>
        public static IReadOnlyList<CardInstance> CreateCastingSpellbookInstances(HeroId ownerId, string instanceIdPrefix)
        {
            if (string.IsNullOrWhiteSpace(instanceIdPrefix))
            {
                throw new ArgumentException("Spell instance identities are per-seat.", nameof(instanceIdPrefix));
            }

            return Array.AsReadOnly(CastingSpellDefinitions
                .Select(definition => CardInstance.CreateInSpellbook(
                    new CardInstanceId($"{instanceIdPrefix}-{definition.Id.Value}"),
                    definition.Reference,
                    ownerId))
                .ToArray());
        }

        /// <summary>
        /// The words each cast bullet is drawn with, DERIVED from the content: a bullet's label is
        /// "Cast" and the name its spell prints, read off the spell definition its naming step names.
        /// Nothing here is a second list of spells to keep in step with the cards.
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels() =>
            CastArms.Select(arm => DecisionLabelDeclaration.DecisionArm(
                CantripId, arm.Arm.Value, CastLabel(arm.Spell)));

        /// <summary>The label a bullet that casts <paramref name="spell"/> carries.</summary>
        public static string CastLabel(CardDefinitionId spell) =>
            "Cast " + CastingSpellDefinitions.Single(definition => definition.Id.Equals(spell)).DisplayName;

        /// <summary>The Cantrip's bullets: each arm, the spell it casts and that spell's naming step.</summary>
        private static IEnumerable<(EffectNodeId Arm, CardDefinitionId Spell, EffectCapabilityReference InBook)> CastArms =>
            new[]
            {
                (CastAttackArm, AttackSpellId, AttackSpellInBookV1),
                (CastMovementArm, MovementSpellId, MovementSpellInBookV1),
                (CastWardArm, WardSpellId, WardSpellInBookV1)
            };

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyCastingSpells =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                new CardDefinition(
                    AttackSpellId, CastingContentRevision, "TEST - Attack Spell", default,
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.SpellCard(CardColor.Gold, CardTier.None),
                    new[]
                    {
                        new ActionDefinition(SpellPrimaryId, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, AttackSpellValue) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                            AttackRangeKind.NonRanged, default)
                    }),
                new CardDefinition(
                    MovementSpellId, CastingContentRevision, "TEST - Movement Spell", default,
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.SpellCard(CardColor.Red, CardTier.I),
                    new[]
                    {
                        new ActionDefinition(SpellPrimaryId, ActionKind.Movement, ActionDefinitionOrigin.PrintedPrimary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, MovementSpellValue) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true))
                    }),
                new CardDefinition(
                    WardSpellId, CastingContentRevision, "TEST - Ward Spell", default,
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.SpellCard(CardColor.Blue, CardTier.II),
                    new[]
                    {
                        new ActionDefinition(SpellPrimaryId, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                            Array.Empty<PrintedValueDefinition>(),
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                            AttackRangeKind.None, default,
                            new EffectDefinition(new EffectDefinitionId("synthetic-spell-ward-skill"),
                                CastingContentRevision, new EffectNodeId("ward"), new[]
                                {
                                    new EffectNodeDefinition(new EffectNodeId("ward"), EffectNodeKind.Capability,
                                        EffectFailurePolicy.Mandatory, GrantWardV1,
                                        outputs: new[]
                                        {
                                            new EffectOutputBinding(
                                                ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                                        })
                                }))
                    })
            }));

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyCastingCards =
            new Lazy<IReadOnlyList<CardDefinition>>(() =>
            {
                var cantripRoot = new EffectNodeId("choose-and-cast-one");
                var cantrip = new EffectDefinition(
                    new EffectDefinitionId("synthetic-cantrip-skill"), CastingContentRevision, cantripRoot,
                    new[]
                    {
                        new EffectNodeDefinition(cantripRoot, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                            childNodeIds: CastArms.Select(arm => arm.Arm),
                            chooser: EffectChooserRole.Actor,
                            minimumSelections: 1,
                            maximumSelections: 1)
                    }.Concat(CastArms.SelectMany(arm =>
                        SyntheticSpellbookCapabilities.CastArm(arm.Arm, arm.InBook))));

                var evocation = new EffectDefinition(
                    new EffectDefinitionId("synthetic-evocation-skill"), CastingContentRevision, CastAttackArm,
                    SyntheticSpellbookCapabilities.CastArm(CastAttackArm, AttackSpellInBookV1));

                var refill = new EffectNodeId("add-all-spell-cards");
                var prepare = new EffectDefinition(
                    new EffectDefinitionId("synthetic-prepare-spells-skill"), CastingContentRevision, refill,
                    new[]
                    {
                        new EffectNodeDefinition(refill, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticSpellbookCapabilities.RefillSpellbookV1,
                            outputs: new[]
                            {
                                new EffectOutputBinding(
                                    SyntheticSpellbookCapabilities.ReturnedOutput, EffectValueKind.Integer)
                            })
                    });

                return Array.AsReadOnly(new[]
                {
                    CastingCard(CantripId, "TEST - Cantrip", 9, cantrip),
                    CastingCard(EvocationId, "TEST - Evocation", 9, evocation),
                    CastingCard(PrepareSpellsId, "TEST - Prepare Spells", 9, prepare)
                });
            });

        private static CardDefinition CastingCard(CardDefinitionId id, string name, int initiative, EffectDefinition text) =>
            new CardDefinition(
                id, CastingContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(CastingPrimaryId, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, text)
                });

        private static EffectCapabilityReference Reference(string id) =>
            new EffectCapabilityReference(new EffectCapabilityId(id), 1);
    }
}
