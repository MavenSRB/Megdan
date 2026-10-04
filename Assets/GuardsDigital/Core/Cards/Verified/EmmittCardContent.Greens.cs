using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class EmmittCardContent
    {
        public const string TimeWalkText =
            "Move an enemy hero in range, who remained in the same space since the last turn, " +
            "2 spaces in a straight line.";

        public const string FastForwardText = TimeWalkText;

        public static CardDefinition TimeWalkDefinition { get; } = GreenSkill(
            TimeWalk, "Time Walk", CardTier.I, 6, 2, 3, PrintedValueChannel.None,
            new[] { FastForward, TimeCapsule }, MoveStationaryEnemy(TimeWalk), TimeWalkText);

        public static CardDefinition FastForwardDefinition { get; } = GreenSkill(
            FastForward, "Fast Forward", CardTier.II, 5, 2, 4, PrintedValueChannel.Attack,
            new[] { BackToTheFuture, FutureProof },
            MoveStationaryEnemy(FastForward), FastForwardText);

        private static CardDefinition GreenSkill(CardDefinitionId id, string name, CardTier tier,
            int initiative, int defense, int range, PrintedValueChannel item,
            IReadOnlyList<CardDefinitionId> upgrades, EffectDefinition effect, string text,
            PrintedValueChannel reachChannel = PrintedValueChannel.Range) =>
            new CardDefinition(id, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Green, tier, item, upgrades),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(reachChannel, range) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, effect),
                    Movement(2), Defense(defense)
                },
                null, CardSourceContent.Transcribed(text));

        private static EffectDefinition MoveStationaryEnemy(CardDefinitionId card)
        {
            var root = new EffectNodeId(card.Value + "-root");
            var target = new EffectNodeId(card.Value + "-stationary-enemy-in-range");
            var line = new EffectNodeId(card.Value + "-two-space-line");
            var move = new EffectNodeId(card.Value + "-move");
            return new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"),
                ContentRevision, root, new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory, childNodeIds: new[] { target, line, move }),
                    new EffectNodeDefinition(target, EffectNodeKind.Capability,
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
                                SyntheticTargetingCapabilities.RemainedSincePreviousTurnParameterName,
                                EffectValue.Boolean(true)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(line))
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
                    new EffectNodeDefinition(line, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticRosterCapabilities.ChooseLineDestinationV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("maximumDistance", EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral("minimumDistance", EffectValue.Integer(2)),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticRosterCapabilities.LineSubjectSpaceParameter, target,
                                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                EffectValueKind.Coordinate)
                        },
                        outputs: new[] { new EffectOutputBinding("destination", EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(move, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput("mover", target,
                                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(2)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.NamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.NamedDestinationParameterName,
                                line, "destination", EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate),
                            new EffectOutputBinding(
                                SyntheticDisplacementCapabilities.DidMoveOutputName,
                                EffectValueKind.Boolean)
                        })
                });
        }
    }
}
