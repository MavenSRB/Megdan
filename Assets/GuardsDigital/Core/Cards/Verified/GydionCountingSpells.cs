using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Placement;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.30e. Gydion's counting spells and the Familiar - the four spells whose text reads
    /// "other spell cards removed from the spellbook". The count is
    /// <see cref="SpellbookQuery.OtherRemoved"/>'s, one derivation for the value pipeline
    /// (<see cref="RosterValueQueryKind.OtherSpellsRemovedFromSpellbook"/>) and the counting step
    /// (<see cref="SyntheticSpellbookCapabilities.CountOtherRemovedSpellsV1"/>): the cast spell is
    /// excluded and the count is read when the value is asked, so an offer and its resolution count the
    /// same spells (lesson 27).
    ///
    /// <para>Each is performed by the hero casting it through 9.30c's borrow path, with the spell's
    /// own values and the caster's items. Nothing here is registered (9.30i).</para>
    /// </summary>
    public static partial class GydionCardContent
    {
        /// <summary>"+1" per other spell removed, on Sunburst's Attack and Range and Dimension Door's Radius.</summary>
        public const int PerOtherRemovedSpell = 1;

        /// <summary>Midas' Touch: "Gain 1 coin for every two other spell cards removed".</summary>
        public const int MidasTouchSpellsPerCoin = 2;

        /// <summary>Find Familiar: "You may remove up to three other spell cards".</summary>
        public const int FindFamiliarRemovesUpTo = 3;

        /// <summary>
        /// Sunburst (Red III, Attack - Ranged, Attack 0+, Range 0+): "Target a unit at maximum range.
        /// +1 Attack and +1 Range for each other spell card removed from the spellbook."
        ///
        /// <para><b>"At maximum range" is the Attack's own reach</b>,
        /// <see cref="AttackTargetReach.ExactlyPrintedRange"/> - RULING 1's equality, which Silverarrow's
        /// Attacks carry on the action and the printed-icon family's <c>atMaximumReach</c> carries for a
        /// card-text selection (Ignatia). Sunburst's target is the action's own, so the band rides on
        /// the action and the cast Attack stays on p.13/p.14's ordinary path. The Range Total it is
        /// measured against is printed 0, the count, and p.16's Range item; the Attack Total is printed
        /// 0, the count, and the Attack item.</para>
        ///
        /// <para>With nothing else removed and no Range item the Range Total is 0 and reaches no unit.
        /// The cast still happens (GY-Q12, user: "Yes, castable"): the spell is removed faceup and its
        /// Attack reaches nobody.</para>
        /// </summary>
        private static ActionDefinition SunburstPrimary() =>
            new ActionDefinition(
                PrimaryRangedAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Attack, 0), Value(PrintedValueChannel.Range, 0) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.Ranged, default,
                attackTargetPolicy: PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged)
                    .AtExactlyThePrintedRange(),
                rosterValueContributions: new[]
                {
                    new RosterValueContributionDefinition(
                        PrintedValueChannel.Attack, RosterValueQueryKind.OtherSpellsRemovedFromSpellbook,
                        PerOtherRemovedSpell),
                    new RosterValueContributionDefinition(
                        PrintedValueChannel.Range, RosterValueQueryKind.OtherSpellsRemovedFromSpellbook,
                        PerOtherRemovedSpell)
                });

        /// <summary>
        /// Dimension Door (Green II, Skill, Radius 0+): "Place yourself into a space at maximum radius.
        /// +1 Radius for each other spell card removed from the spellbook."
        ///
        /// <para>The acting hero's own space, then
        /// <see cref="SyntheticPlacementCapabilities.PlaceAtMaximumRadiusV1"/>: the empty spaces at
        /// EXACTLY the Radius Total, which is printed 0, the count and p.16's Radius item (R1). The Place
        /// is mandatory - the face prints no "may" - so a Radius of 0, or a ring with no empty space, is
        /// a Skill that cannot apply; a cast of it still removes the spell and he stays put (GY-Q12).</para>
        /// </summary>
        private static ActionDefinition DimensionDoorPrimary()
        {
            var root = new EffectNodeId("dimension-door-root");
            var self = new EffectNodeId("dimension-door-yourself");
            var place = new EffectNodeId("dimension-door-place");
            var text = new EffectDefinition(
                new EffectDefinitionId(DimensionDoor.Value + "-skill"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { self, place }),
                    new EffectNodeDefinition(self, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.ActorSpaceV1,
                        outputs: new[] { new EffectOutputBinding(SpaceOutput, EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(place, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticPlacementCapabilities.PlaceAtMaximumRadiusV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                PlaceSubjectParameter, self, SpaceOutput, EffectValueKind.Coordinate),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.EligiblePieceParameterName,
                                PlaceEligiblePieceKind.GamePiece),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                                PlaceDestinationAnchor.ActingHero),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                                PlaceSpawnPointExclusion.None)
                        },
                        outputs: new[] { new EffectOutputBinding(PlacedOutput, EffectValueKind.Coordinate) })
                });

            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Radius, 0) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default, text,
                rosterValueContributions: new[]
                {
                    new RosterValueContributionDefinition(
                        PrintedValueChannel.Radius, RosterValueQueryKind.OtherSpellsRemovedFromSpellbook,
                        PerOtherRemovedSpell)
                });
        }

        /// <summary>
        /// Midas' Touch (Blue II, Skill, no icon): "Gain 1 coin for every two other spell cards removed
        /// from the spellbook."
        ///
        /// <para>The count, divided by two and rounded down
        /// (<see cref="SyntheticSpellbookCapabilities.CountOtherRemovedSpellsV1"/>), then
        /// <c>gain-resource@1</c> for the caster. The gain is <c>IfAble</c>: with fewer than two other
        /// spells removed it gains 0, which is no gain at all - nothing to perform rather than a step
        /// that failed - so the Skill applies nothing. The cast is still playable (GY-Q12, user: "Yes,
        /// castable"): the spell is removed faceup, which raises the next count, and nothing is gained.
        /// </para>
        /// </summary>
        private static ActionDefinition MidasTouchPrimary()
        {
            var root = new EffectNodeId("midas-touch-root");
            var count = new EffectNodeId("midas-touch-count");
            var gain = new EffectNodeId("midas-touch-gain");
            var text = new EffectDefinition(
                new EffectDefinitionId(MidasTouch.Value + "-skill"), ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { count, gain }),
                    new EffectNodeDefinition(count, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticSpellbookCapabilities.CountOtherRemovedSpellsV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(
                                SyntheticSpellbookCapabilities.EveryParameter,
                                EffectValue.Integer(MidasTouchSpellsPerCoin))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(SyntheticSpellbookCapabilities.CountOutput, EffectValueKind.Integer)
                        }),
                    new EffectNodeDefinition(gain, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                        SyntheticRosterCapabilities.GainResourceV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole("recipient", EffectRoleKind.Actor, EffectValueKind.Hero),
                            EffectInputBinding.FromNodeOutput(
                                "amount", count, SyntheticSpellbookCapabilities.CountOutput, EffectValueKind.Integer)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("recipient", EffectValueKind.Hero),
                            new EffectOutputBinding("amount", EffectValueKind.Integer)
                        })
                });

            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                System.Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default, text);
        }

        /// <summary>The arm Find Familiar's removal offers for <paramref name="spell"/>.</summary>
        public static EffectNodeId FindFamiliarRemovalArm(CardDefinitionId spell) =>
            new EffectNodeId("remove-" + spell.Value.Substring(ContentSetId.Length + 1));

        /// <summary>The node Find Familiar's "remove up to three other spell cards" is asked at.</summary>
        public static EffectNodeId FindFamiliarRemovalChoice { get; } = new EffectNodeId("find-familiar-remove");

        /// <summary>
        /// The spells Find Familiar may remove: every spell but itself ("other"), in book order. It is
        /// already removed as it is cast, so naming it would offer a bullet that could never be taken.
        /// </summary>
        public static IReadOnlyList<CardDefinitionId> FindFamiliarRemovableSpells =>
            SpellIds.Where(id => !id.Equals(FindFamiliar)).ToArray();

        /// <summary>
        /// Find Familiar (Green II, Skill, Radius 3): "Place a [Familiar] token in radius. You may remove
        /// up to three other spell cards from the spell book faceup." GY-Q5 as the user settled it.
        ///
        /// <para><b>The Place</b> is Widget's Pyro shape (Dragon Bond): one Familiar exists, so with it on
        /// the board this Place takes it from there (<c>synthetic-place-in-radius@1</c>, measured from
        /// Gydion) - placing it again MOVES it - and otherwise from the supply
        /// (<see cref="GydionComponentContent.PlaceFamiliarInRadiusV1"/>). Both are <c>IfAble</c>, so a
        /// radius with no empty space places nothing and the removal still happens: the two sentences are
        /// separate steps (the shard's reading).</para>
        ///
        /// <para><b>The removal</b> is a <c>Choose</c> of up to three (W2/W5: "up to", zero a real
        /// choice) over one option per OTHER spell - <see cref="SyntheticSpellbookCapabilities.RemoveArm"/>:
        /// the naming step, then book -> removed faceup by 9.30b's transition. A spell is named, never a
        /// book card (GY-05), and an option whose spell is already removed is greyed by its naming step.
        /// It is not a cast: no action is performed off a removed spell.</para>
        /// </summary>
        private static ActionDefinition FindFamiliarPrimary()
        {
            var root = new EffectNodeId("find-familiar-root");
            var placeFamiliar = new EffectNodeId("find-familiar-place");
            var relocate = new EffectNodeId("find-familiar-relocate");
            var familiarAt = new EffectNodeId("find-familiar-space");
            var moveFamiliar = new EffectNodeId("find-familiar-move");
            var fromSupply = new EffectNodeId("find-familiar-from-supply");
            var arms = FindFamiliarRemovableSpells
                .Select(spell => (Arm: FindFamiliarRemovalArm(spell), Spell: spell))
                .ToArray();

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { placeFamiliar, FindFamiliarRemovalChoice }),
                new EffectNodeDefinition(placeFamiliar, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    GydionComponentContent.FamiliarPresentV1,
                    childNodeIds: new[] { relocate, fromSupply },
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticOwnedTokenCapabilities.PresentOutput, EffectValueKind.Boolean)
                    }),
                new EffectNodeDefinition(relocate, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { familiarAt, moveFamiliar }),
                new EffectNodeDefinition(familiarAt, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    GydionComponentContent.FamiliarSpaceV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticOwnedTokenCapabilities.SpaceOutput, EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(moveFamiliar, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    SyntheticPlacementCapabilities.PlaceInRadiusV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            PlaceSubjectParameter, familiarAt, SyntheticOwnedTokenCapabilities.SpaceOutput,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.EligiblePieceParameterName, PlaceEligiblePieceKind.Token),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.DestinationAnchorParameterName,
                            PlaceDestinationAnchor.ActingHero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticPlacementCapabilities.SpawnPointExclusionParameterName,
                            PlaceSpawnPointExclusion.None)
                    },
                    outputs: new[] { new EffectOutputBinding(PlacedOutput, EffectValueKind.Coordinate) }),
                new EffectNodeDefinition(fromSupply, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                    GydionComponentContent.PlaceFamiliarInRadiusV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticTokenCapabilities.PlacedOutput, EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(FindFamiliarRemovalChoice, EffectNodeKind.Choose, EffectFailurePolicy.UpTo,
                    childNodeIds: arms.Select(arm => arm.Arm),
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 0,
                    maximumSelections: FindFamiliarRemovesUpTo)
            };
            nodes.AddRange(arms.SelectMany(arm =>
                SyntheticSpellbookCapabilities.RemoveArm(arm.Arm, InBookV1(arm.Spell))));

            return new ActionDefinition(
                PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                new[] { Value(PrintedValueChannel.Radius, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None, default,
                new EffectDefinition(
                    new EffectDefinitionId(FindFamiliar.Value + "-skill"), ContentRevision, root, nodes));
        }

        /// <summary>
        /// What Find Familiar's removal options say, DERIVED from the content: "Remove" and the name the
        /// spell prints. 9.30i aggregates it into <see cref="DecisionLabelCatalog"/> with the dealing.
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> CountingSpellDecisionLabels() =>
            FindFamiliarRemovableSpells.Select(spell => DecisionLabelDeclaration.DecisionArm(
                FindFamiliar, FindFamiliarRemovalArm(spell).Value, "Remove " + Definition(spell).DisplayName));

        private const string PlaceSubjectParameter = "subject";
        private const string SpaceOutput = "space";
        private const string PlacedOutput = "placed";
    }
}
