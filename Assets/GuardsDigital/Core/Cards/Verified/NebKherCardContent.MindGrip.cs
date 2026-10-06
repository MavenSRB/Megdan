using System;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class NebKherCardContent
    {
        public const string MindGripText =
            "Choose one — Perform an action on the card in the previous turn slot of an enemy hero " +
            "in range; if you would place any tokens this way, place Illusion tokens instead; do not " +
            "use markers. Or: Defeat a minion adjacent to you.";

        public const string MindProbeText =
            "Perform an action on the card in the previous turn slot of an enemy hero in range; " +
            "if you would place any tokens this way, place Illusion tokens instead; do not use markers.";

        public static CardDefinition MindGripDefinition { get; } = MindFace(
            MindGrip, "Mind Grip", 9, 2, 5, true, MindGripText);

        // This p.20 Handicap is transcribed and composed, but never enters a configured hand.
        public static CardDefinition MindProbeDefinition { get; } = MindFace(
            MindProbe, "Mind Probe", 8, 1, 3, false, MindProbeText);

        private static CardDefinition MindFace(
            CardDefinitionId card, string title, int initiative, int defense, int range,
            bool mayDefeatMinion, string text) =>
            new CardDefinition(card, ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Range, range) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, MindEffect(card, mayDefeatMinion)),
                    Movement(1), Defense(defense)
                },
                null, CardSourceContent.Transcribed(text));

        private static EffectDefinition MindEffect(CardDefinitionId card, bool mayDefeatMinion)
        {
            var prefix = card.Value + "-";
            var root = new EffectNodeId(prefix + "root");
            var borrowArm = new EffectNodeId(prefix + "borrow-arm");
            var enemy = new EffectNodeId(prefix + "enemy");
            var borrow = new EffectNodeId(prefix + "borrow");
            var minionArm = new EffectNodeId(prefix + "minion-arm");
            var minion = new EffectNodeId(prefix + "minion");
            var defeat = new EffectNodeId(prefix + "defeat");
            var borrowNodes = new[]
            {
                new EffectNodeDefinition(borrowArm, EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory, childNodeIds: new[] { enemy, borrow }),
                new EffectNodeDefinition(enemy, EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RangeHeroTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.OpposingSource),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                            EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValue.Node(borrow))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                            EffectValueKind.Coordinate)
                    }),
                new EffectNodeDefinition(borrow, EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticPreviousSlotBorrowCapabilities.BorrowPrintedActionV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticPreviousSlotBorrowCapabilities.EnemyParameter,
                            enemy, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        EffectInputBinding.FromLiteral(
                            SyntheticPreviousSlotBorrowCapabilities.SubstituteTokenKindParameter,
                            EffectValue.TokenKind(NebKherComponentContent.Illusion.Id))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticBorrowedActionCapabilities.BorrowedCardOutput,
                            EffectValueKind.CardInstance),
                        new EffectOutputBinding(
                            SyntheticPreviousSlotBorrowCapabilities.BorrowedActionIndexOutput,
                            EffectValueKind.Integer)
                    })
            };
            if (!mayDefeatMinion)
                return new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"),
                    ContentRevision, borrowArm, borrowNodes);

            return new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"),
                ContentRevision, root, new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Choose,
                        EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { borrowArm, minionArm },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1, maximumSelections: 1)
                }.Concat(borrowNodes).Concat(new[]
                {
                    new EffectNodeDefinition(minionArm, EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory, childNodeIds: new[] { minion, defeat }),
                    new EffectNodeDefinition(minion, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseTargetV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.OriginParameter,
                                RosterQueryOrigin.Actor),
                            EffectInputBinding.FromLiteral(
                                SyntheticRosterCapabilities.DistanceParameter,
                                EffectValue.Integer(1)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.UnitKindParameter,
                                RosterUnitKind.Minion),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Skill)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion)
                        }),
                    new EffectNodeDefinition(defeat, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticConsequenceCapabilities.DefeatMinionV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticConsequenceCapabilities.MinionParameter,
                                minion, SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticConsequenceCapabilities.DefeatedMinionOutput,
                                EffectValueKind.Minion)
                        })
                }).ToArray());
        }
    }
}
