using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.30g. Wish, The Archwizard, and the one action card the Archwizard reaches.
    ///
    /// <para><b>Wish</b> (Purple, Tier IV, Skill): "Cast any spell in the Spellbook. After you cast the
    /// "Wish" spell three times your team wins the game." Wish is itself cast, so its primary runs
    /// exactly when it is cast and is performed by the hero casting it (9.30c's borrow path). In order:
    /// the caster's team's count rises (GY-Q10: "the Wish count rises when Wish is cast"); one choice
    /// over every OTHER spell, each named (GY-05) and cast through 9.30c's cast arm, so any spell still
    /// in the book of any tier may be cast, including one none of his cards lists (GY-Q9), a removed
    /// one is greyed, and an in-book one is always castable (GY-Q12); then the claim. The cast arm's
    /// spell action runs inside Wish's text and Wish resumes after it, so the claim is asked once the
    /// whole action, its inner spell included, has resolved; an ending the inner spell caused stands
    /// (GY-Q10). Wish itself is not an option: it is removed faceup as it is cast (GY-Q2), so it is
    /// never in the book while it resolves. The choice is <c>IfAble</c>: with every other spell
    /// removed there is nothing to cast, and the count and the claim still happen.</para>
    ///
    /// <para><b>The Archwizard</b> (Purple, Tier IV, Ultimate): "Whenever you would add cards to the
    /// spellbook, you may cast the "Wish" spell in the spellbook instead." Mortimer's Master of Puppets
    /// prints the same construction and is composed the same way: a p.16 standing declaration, read by
    /// the face that "would" do the replaced thing through p.16's own question
    /// (<see cref="SyntheticRosterCapabilities.UltimateUnlockedV1"/>). Prepare Spells is the only face
    /// that adds cards to the book, so its refill is the replacement's one site: with the Ultimate
    /// unlocked and Wish in the book he chooses "Add all spell cards to the spellbook" or "Cast Wish
    /// instead"; otherwise the refill happens with nothing asked. GY-Q7 (user): a full book still
    /// "would add cards", so nothing need be removed for the replacement to be offered. Casting Wish
    /// instead returns no spell, so an Active effect a removed spell is the source of keeps running
    /// (lesson 26).</para>
    ///
    /// <para><b>Only what 9.30g needs</b>: Prepare Spells and The Archwizard are the two action
    /// fronts this block composes; the other seventeen, the set's aggregate and the registration are
    /// 9.30i's, which folds <see cref="ArchwizardCardDefinitions"/> into <see cref="AllDefinitions"/>.
    /// Nothing here is registered (<see cref="ArchwizardDeferredHandlers"/>).</para>
    /// </summary>
    public static partial class GydionCardContent
    {
        /// <summary>Wish: "After you cast the "Wish" spell three times your team wins the game."</summary>
        public const int WishesToWin = 3;

        public static CardDefinitionId PrepareSpells { get; } = Id("prepare-spells");

        public static CardDefinitionId TheArchwizard { get; } = Id("the-archwizard");

        public const string PrepareSpellsText =
            "Add all spell cards to the spellbook. Only Gydion can look at the cards in the spellbook. " +
            "As the spell is cast, remove it from the spellbook faceup. The action on the spell card is " +
            "performed by the hero casting the spell.";

        public const string TheArchwizardText =
            "Whenever you would add cards to the spellbook, you may cast the \"Wish\" spell in the " +
            "spellbook instead.";

        /// <summary>The standing declaration The Archwizard supplies while it is unlocked.</summary>
        public const string TheArchwizardRuleId = "gydion-the-archwizard";

        public const string TheArchwizardRuleKind = "gydion-cast-wish-instead-of-adding-spells";

        /// <summary>Prepare Spells' printed Initiative and Defense (the shard's inventory row).</summary>
        public const int PrepareSpellsInitiative = 13;

        public const int PrepareSpellsDefense = 2;

        public static ActionDefinitionId SecondaryDefense { get; } = new ActionDefinitionId("secondary-defense");

        /// <summary>
        /// "... cast the "Wish" spell in the spellbook instead" first asks whether Wish lies in the
        /// book: <see cref="SyntheticSpellbookCapabilities.SpellInBookPresentHandler"/> bound to Wish.
        /// </summary>
        public static EffectCapabilityReference WishInBookPresentV1 =>
            new EffectCapabilityReference(new EffectCapabilityId(Wish.Value + "-in-book-present"), 1);

        /// <summary>The node Wish's "Cast any spell in the Spellbook" is asked at.</summary>
        public static EffectNodeId WishChoice { get; } = new EffectNodeId("wish-cast-any");

        /// <summary>The arm Wish casts <paramref name="spell"/> with.</summary>
        public static EffectNodeId WishCastArm(CardDefinitionId spell) =>
            new EffectNodeId("wish-cast-" + spell.Value.Substring(ContentSetId.Length + 1));

        /// <summary>
        /// The spells Wish may cast: every spell but Wish, in book order (GY-Q9: any tier, listed on
        /// one of his cards or not). Wish is removed as it is cast, so it is never one of them.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> WishCastableSpells =>
            SpellIds.Where(id => !id.Equals(Wish)).ToArray();

        /// <summary>The node Prepare Spells asks "add, or cast Wish instead" at.</summary>
        public static EffectNodeId PrepareSpellsReplacementChoice { get; } =
            new EffectNodeId("prepare-spells-add-or-wish");

        /// <summary>Prepare Spells' "Add all spell cards to the spellbook" when it is chosen over Wish.</summary>
        public static EffectNodeId PrepareSpellsAddAllArm { get; } = new EffectNodeId("prepare-spells-add-all");

        /// <summary>The Archwizard's "cast the Wish spell in the spellbook instead".</summary>
        public static EffectNodeId PrepareSpellsWishInsteadArm { get; } =
            new EffectNodeId("prepare-spells-wish-instead");

        /// <summary>Prepare Spells and The Archwizard - the two action fronts 9.30g composes.</summary>
        public static IReadOnlyList<CardDefinition> ArchwizardCardDefinitions => LazyArchwizardCards.Value;

        public static CardDefinition PrepareSpellsDefinition => LazyArchwizardCards.Value[0];

        public static CardDefinition TheArchwizardDefinition => LazyArchwizardCards.Value[1];

        /// <summary>
        /// The handlers this block declares and a fixture's catalog must carry until 9.30i registers
        /// them: Wish's presence predicate. The count and the claim are
        /// <see cref="SyntheticTeamVictoryCapabilities.DeferredHandlers"/>.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> ArchwizardDeferredHandlers() =>
            new[] { SyntheticSpellbookCapabilities.SpellInBookPresentHandler(WishInBookPresentV1, Wish) };

        /// <summary>
        /// What Wish's options and Prepare Spells' replacement choice say, DERIVED from the content:
        /// "Cast" and the name each spell prints; the refill's own printed sentence; and "Cast", Wish's
        /// printed name and "instead". 9.30i aggregates them into <see cref="DecisionLabelCatalog"/>.
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> ArchwizardDecisionLabels() =>
            WishCastableSpells
                .Select(spell => DecisionLabelDeclaration.DecisionArm(
                    Wish, WishCastArm(spell).Value, "Cast " + Definition(spell).DisplayName))
                .Concat(new[]
                {
                    DecisionLabelDeclaration.DecisionArm(
                        PrepareSpells, PrepareSpellsAddAllArm.Value,
                        PrepareSpellsText.Substring(0, PrepareSpellsText.IndexOf('.'))),
                    DecisionLabelDeclaration.DecisionArm(
                        PrepareSpells, PrepareSpellsWishInsteadArm.Value,
                        "Cast " + Definition(Wish).DisplayName + " instead")
                });

        /// <summary>
        /// Wish's printed primary (Skill, no icon). See the class remarks for the order and why.
        /// </summary>
        private static ActionDefinition WishPrimary()
        {
            var root = new EffectNodeId("wish-root");
            var count = new EffectNodeId("wish-count");
            var claim = new EffectNodeId("wish-claim");
            var arms = WishCastableSpells.Select(spell => (Arm: WishCastArm(spell), Spell: spell)).ToArray();
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { count, WishChoice, claim }),
                new EffectNodeDefinition(count, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTeamVictoryCapabilities.AdvanceTeamVictoryTallyV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral(
                            SyntheticTeamVictoryCapabilities.ThresholdParameter, EffectValue.Integer(WishesToWin))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticTeamVictoryCapabilities.CountOutput, EffectValueKind.Integer)
                    }),
                new EffectNodeDefinition(WishChoice, EffectNodeKind.Choose, EffectFailurePolicy.IfAble,
                    childNodeIds: arms.Select(arm => arm.Arm),
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 1,
                    maximumSelections: 1),
                new EffectNodeDefinition(claim, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTeamVictoryCapabilities.ClaimTeamVictoryV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticTeamVictoryCapabilities.ClaimedOutput, EffectValueKind.Boolean)
                    })
            };
            nodes.AddRange(arms.SelectMany(arm =>
                SyntheticSpellbookCapabilities.CastArm(arm.Arm, InBookV1(arm.Spell))));

            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                System.Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default,
                new EffectDefinition(new EffectDefinitionId(Wish.Value + "-skill"), ContentRevision, root, nodes));
        }

        /// <summary>
        /// Prepare Spells (Silver, basic, Initiative 13, Defense 2): "Add all spell cards to the
        /// spellbook." The refill is 9.30c's <c>refill-spellbook@1</c>; The Archwizard's replacement is
        /// composed in front of it (see the class remarks). The other three sentences are the casting
        /// rules every cast already follows.
        /// </summary>
        private static CardDefinition PrepareSpellsCard()
        {
            var root = new EffectNodeId("prepare-spells-root");
            var wishGate = new EffectNodeId("prepare-spells-wish-gate");
            var addWhileLocked = new EffectNodeId("prepare-spells-add-all-locked");
            var addWithoutWish = new EffectNodeId("prepare-spells-add-all-no-wish");
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(root, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.UltimateUnlockedV1,
                    childNodeIds: new[] { wishGate, addWhileLocked },
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticRosterCapabilities.PresentOutput, EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(wishGate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    WishInBookPresentV1,
                    childNodeIds: new[] { PrepareSpellsReplacementChoice, addWithoutWish },
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(
                            SyntheticSpellbookCapabilities.OwnerParameter, EffectRoleKind.Actor, EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticSpellbookCapabilities.PresentOutput, EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(PrepareSpellsReplacementChoice, EffectNodeKind.Choose,
                    EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { PrepareSpellsAddAllArm, PrepareSpellsWishInsteadArm },
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 1,
                    maximumSelections: 1),
                Refill(PrepareSpellsAddAllArm),
                Refill(addWhileLocked),
                Refill(addWithoutWish)
            };
            nodes.AddRange(SyntheticSpellbookCapabilities.CastArm(PrepareSpellsWishInsteadArm, InBookV1(Wish)));

            return new CardDefinition(
                PrepareSpells, ContentRevision, "Prepare Spells", new InitiativeValue(PrepareSpellsInitiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        System.Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        new EffectDefinition(
                            new EffectDefinitionId(PrepareSpells.Value + "-skill"), ContentRevision, root, nodes)),
                    new ActionDefinition(
                        SecondaryDefense, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
                        new[] { Value(PrintedValueChannel.Defense, PrepareSpellsDefense) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                },
                CardSourceContent.Transcribed(PrepareSpellsText));
        }

        private static EffectNodeDefinition Refill(EffectNodeId id) =>
            new EffectNodeDefinition(id, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticSpellbookCapabilities.RefillSpellbookV1,
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticSpellbookCapabilities.ReturnedOutput, EffectValueKind.Integer)
                });

        private static readonly System.Lazy<IReadOnlyList<CardDefinition>> LazyArchwizardCards =
            new System.Lazy<IReadOnlyList<CardDefinition>>(() => System.Array.AsReadOnly(new[]
            {
                PrepareSpellsCard(),
                new CardDefinition(
                    TheArchwizard, ContentRevision, "The Archwizard", default,
                    CardContentAuthority.VerifiedOfficial,
                    CardProgressionProfile.UltimateCard(),
                    System.Array.Empty<ActionDefinition>(),
                    UltimateRuleDefinition.PermanentDeclaration(TheArchwizardRuleId, TheArchwizardRuleKind),
                    CardSourceContent.Transcribed(TheArchwizardText))
            }));
    }
}
