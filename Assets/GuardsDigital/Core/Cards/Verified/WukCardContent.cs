using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Wuk the Grove Keeper's printed content. Milestone 8.114a starts it with his two basic faces.
    ///
    /// <para><b>Tree Slam</b> (Gold): "Choose one - Target a minion adjacent to you. / Remove a Tree
    /// token adjacent to you. Target a unit in range." It is p.10's modal composed as Brogan's
    /// Throwing Axe is: the card names its target by its own text
    /// (<see cref="AttackTargetBinding.ByCardText"/>), before the attack, and each bullet binds one
    /// through <c>set-attack-target@1</c>. The second bullet's removal is a MANDATORY step (p.10:
    /// only "you may", "up to" and "if able" are optional), so with no Tree beside Wuk it cannot be
    /// performed, the bullet binds nothing and p.10 stops the action - and the selection that finds
    /// no Tree is what greys the bullet before it is taken.</para>
    ///
    /// <para><b>Mystic Saplings</b> (Silver): "Place up to 3 Tree tokens in radius; Tree tokens are not
    /// removed at the end of round." Three optional supply Places reaching the printed Radius, each
    /// preceded by p.18's short-supply step - Ignatia's moon placement exactly. The second sentence is
    /// the Tree token's own declared lifetime (WU-Q2, <see cref="WukComponentContent.Tree"/>), so it
    /// composes to no node here.</para>
    ///
    /// <para>Nothing here is registered: Wuk sits behind milestone 7.9's gate until 8.114f deals
    /// him.</para>
    /// </summary>
    public static class WukCardContent
    {
        /// <summary>The revision every Wuk definition carries.</summary>
        public const string ContentRevision = "wuk-8-114-rev-1";

        public const string ContentSetId = "wuk";

        public static CardDefinitionId TreeSlam { get; } = Id("tree-slam");
        public static CardDefinitionId MysticSaplings { get; } = Id("mystic-saplings");

        public static ActionDefinitionId PrimaryRangedAttack { get; } =
            new ActionDefinitionId("primary-ranged-attack");
        public static ActionDefinitionId PrimarySkill { get; } = new ActionDefinitionId("primary-skill");
        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        public const string TreeSlamText =
            "Choose one - • Target a minion adjacent to you. • Remove a Tree token adjacent to you. " +
            "Target a unit in range.";

        public const string MysticSaplingsText =
            "Place up to 3 Tree tokens in radius; Tree tokens are not removed at the end of round.";

        /// <summary>The modal's node, so a paused chain names what it is owed.</summary>
        public const string TreeSlamModalNode = "tree-slam-modal";

        /// <summary>The Tree the second bullet removes.</summary>
        public const string TreeSlamTreeNode = "tree-slam-tree";

        /// <summary>The most Trees Mystic Saplings prints.</summary>
        public const int SaplingsPrintedTreeCount = 3;

        /// <summary>Gold, Basic: Initiative 11, Defense 3, Movement 1, Basic Attack - Ranged 4, Range 2.</summary>
        public static CardDefinition TreeSlamDefinition => LazyTreeSlam.Value;

        private static readonly Lazy<CardDefinition> LazyTreeSlam = new Lazy<CardDefinition>(() =>
            new CardDefinition(
                TreeSlam, ContentRevision, "Tree Slam", new InitiativeValue(11),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                new[]
                {
                    new ActionDefinition(
                        PrimaryRangedAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, 4),
                            new PrintedValueDefinition(PrintedValueChannel.Range, 2)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged, default,
                        TreeSlamAttackText(TreeSlam),
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        CardTextTiming.BeforeTheAttack,
                        BoundByCardText(AttackRangeKind.Ranged)),
                    Movement(1),
                    Defense(3)
                },
                null, CardSourceContent.Transcribed(TreeSlamText)));

        /// <summary>Silver, Basic: Initiative 4, Defense 4, Basic Skill, Radius 3.</summary>
        public static CardDefinition MysticSaplingsDefinition => LazyMysticSaplings.Value;

        private static readonly Lazy<CardDefinition> LazyMysticSaplings = new Lazy<CardDefinition>(() =>
            new CardDefinition(
                MysticSaplings, ContentRevision, "Mystic Saplings", new InitiativeValue(4),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 3) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        MysticSaplingsSkill(MysticSaplings)),
                    Defense(4)
                },
                null, CardSourceContent.Transcribed(MysticSaplingsText)));

        /// <summary>Every face composed so far, in his shard's inventory order.</summary>
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAll.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAll =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(
                new[] { TreeSlamDefinition, MysticSaplingsDefinition }
                    .Concat(WukRedFaces.AttackDefinitions)
                    .ToArray()));

        /// <summary>
        /// Tree Slam's two bullets. The first is Throwing Axe's adjacent bullet narrowed to minions;
        /// the second removes a Tree beside Wuk and then names a unit in the printed Range.
        /// </summary>
        private static EffectDefinition TreeSlamAttackText(CardDefinitionId card)
        {
            var modal = new EffectNodeId(TreeSlamModalNode);
            var adjacentArm = new EffectNodeId("tree-slam-adjacent");
            var adjacentTarget = new EffectNodeId("tree-slam-adjacent-target");
            var adjacentBind = new EffectNodeId("tree-slam-adjacent-bind");
            var treeArm = new EffectNodeId("tree-slam-tree-arm");
            var tree = new EffectNodeId(TreeSlamTreeNode);
            var remove = new EffectNodeId("tree-slam-remove");
            var rangeTarget = new EffectNodeId("tree-slam-range-target");
            var rangeBind = new EffectNodeId("tree-slam-range-bind");

            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-attack-text"), ContentRevision, modal,
                new[]
                {
                    new EffectNodeDefinition(
                        modal, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { adjacentArm, treeArm },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),

                    // "Target a minion adjacent to you."
                    new EffectNodeDefinition(
                        adjacentArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { adjacentTarget, adjacentBind }),
                    new EffectNodeDefinition(
                        adjacentTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Minion),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion)
                        }),
                    BindAttackTarget(adjacentBind, adjacentTarget),

                    // "Remove a Tree token adjacent to you. Target a unit in range."
                    new EffectNodeDefinition(
                        treeArm, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { tree, remove, rangeTarget, rangeBind }),
                    new EffectNodeDefinition(
                        tree, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.ChooseTargetV4,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                            EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.TokenKindParameterName,
                                EffectValue.TokenKind(WukComponentContent.TreeId)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(remove))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(
                        remove, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.RemoveV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "subject", tree, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                RemoveEligiblePieceKind.Token)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("removed", EffectValueKind.Coordinate),
                            new EffectOutputBinding(
                                RecordedLanePushQuery.OutcomeFactName, EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        rangeTarget, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeUnitTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionOutputName,
                                EffectValueKind.Minion)
                        }),
                    BindAttackTarget(rangeBind, rangeTarget)
                });
        }

        /// <summary>
        /// "Place up to 3 Tree tokens in radius": three optional supply Places, each after p.18's
        /// short-supply step - the composition Ignatia's moon face carries since milestone 8.113.
        /// </summary>
        private static EffectDefinition MysticSaplingsSkill(CardDefinitionId card)
        {
            var root = new EffectNodeId("mystic-saplings-root");
            var steps = new List<EffectNodeId>();
            var nodes = new List<EffectNodeDefinition>();
            for (var index = 1; index <= SaplingsPrintedTreeCount; index++)
            {
                var reclaim = new EffectNodeId($"mystic-saplings-reclaim-{index}");
                var place = new EffectNodeId($"mystic-saplings-place-{index}");
                steps.Add(reclaim);
                steps.Add(place);
                nodes.Add(new EffectNodeDefinition(
                    reclaim, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    WukComponentContent.ReclaimTreeV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTokenCapabilities.ReclaimedOutput, EffectValueKind.Coordinate)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    place, EffectNodeKind.Capability, EffectFailurePolicy.May,
                    WukComponentContent.PlaceTreeInRadiusV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTokenCapabilities.PlacedOutput, EffectValueKind.Coordinate)
                    }));
            }

            nodes.Insert(0, new EffectNodeDefinition(
                root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: steps));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), ContentRevision, root, nodes);
        }

        /// <summary>
        /// The node that names the enclosing Attack's target from the unit a selection chose - Brogan's
        /// transport. Both selections publish under the same two output names.
        /// </summary>
        private static EffectNodeDefinition BindAttackTarget(EffectNodeId nodeId, EffectNodeId source) =>
            new EffectNodeDefinition(
                nodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticContinuationCapabilities.SetAttackTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.HeroParameter, source,
                        SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                    EffectInputBinding.FromNodeOutput(
                        SyntheticContinuationCapabilities.MinionParameter, source,
                        SyntheticRosterCapabilities.ChosenMinionOutput, EffectValueKind.Minion)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        SyntheticContinuationCapabilities.BoundHeroTargetOutput, EffectValueKind.Hero),
                    new EffectOutputBinding(
                        SyntheticContinuationCapabilities.BoundMinionTargetOutput, EffectValueKind.Minion)
                });

        /// <summary>p.13's reach for the subtype, with the target named by the card's own text.</summary>
        private static PrintedAttackTargetPolicy BoundByCardText(AttackRangeKind range)
        {
            var classification = PrintedAttackTargetPolicy.ForClassification(range);
            return new PrintedAttackTargetPolicy(
                AttackTargetableUnits.HeroesAndMinions, classification.Reach, classification.Line)
                .BoundByCardText();
        }

        internal static ActionDefinition Movement(int value) => new ActionDefinition(
            SecondaryMovement, ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));

        internal static ActionDefinition Defense(int value) => new ActionDefinition(
            SecondaryDefense, ActionKind.Defense, ActionDefinitionOrigin.Reaction,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
            AttackRangeKind.None,
            new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));

        internal static CardDefinitionId Id(string card) =>
            new CardDefinitionId(ContentSetId + "-" + card);
    }
}
