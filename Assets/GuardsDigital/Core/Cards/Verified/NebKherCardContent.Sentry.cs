using System;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class NebKherCardContent
    {
        public const string PhantasmalSentryText =
            "Choose one — Target a hero in range adjacent to an Illusion token in range. " +
            "Or: Target a unit adjacent to you.";

        public static CardDefinition PhantasmalSentryDefinition { get; } =
            new CardDefinition(
                PhantasmalSentry, ContentRevision, "Phantasmal Sentry",
                new InitiativeValue(7), CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Red, CardTier.I,
                    PrintedValueChannel.None, new[] { PhantasmalWarrior, TwistFate }),
                new[]
                {
                    new ActionDefinition(
                        PrimaryAttack, ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, 2),
                            new PrintedValueDefinition(PrintedValueChannel.Range, 4)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged, default,
                        SentryTarget(),
                        PrintedTextComposition.None, PrintedDefenseResolution.None,
                        CardTextTiming.BeforeTheAttack,
                        PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged)
                            .BoundByCardText()),
                    Movement(3), Defense(6)
                },
                null, CardSourceContent.Transcribed(PhantasmalSentryText));

        private static EffectDefinition SentryTarget()
        {
            var root = new EffectNodeId("phantasmal-sentry-choice");
            var imageArm = new EffectNodeId("phantasmal-sentry-image-arm");
            var image = new EffectNodeId("phantasmal-sentry-image");
            var hero = new EffectNodeId("phantasmal-sentry-hero");
            var heroBind = new EffectNodeId("phantasmal-sentry-hero-bind");
            var adjacentArm = new EffectNodeId("phantasmal-sentry-adjacent-arm");
            var adjacent = new EffectNodeId("phantasmal-sentry-adjacent");
            var adjacentBind = new EffectNodeId("phantasmal-sentry-adjacent-bind");
            return new EffectDefinition(new EffectDefinitionId(
                PhantasmalSentry.Value + "-attack-text"), ContentRevision, root, new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Choose,
                        EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { imageArm, adjacentArm },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1, maximumSelections: 1),
                    new EffectNodeDefinition(imageArm, EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { image, hero, heroBind }),
                    new EffectNodeDefinition(image, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Attack),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.IncludeSelfParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeStraightLineParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.AdmittedObjectKindParameterName,
                                PrintedIconObjectKind.TokensOnly),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.TokenKindParameterName,
                                EffectValue.TokenKind(NebKherComponentContent.Illusion.Id))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen",
                            EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(hero, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Attack),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticTargetingCapabilities.AdjacentToSpaceParameterName,
                                image, "chosen", EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(heroBind))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(heroBind, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticContinuationCapabilities.SetAttackTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticContinuationCapabilities.HeroParameter,
                                hero, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticContinuationCapabilities.BoundHeroTargetOutput,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(adjacentArm, EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { adjacent, adjacentBind }),
                    new EffectNodeDefinition(adjacent, EffectNodeKind.Capability,
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
                                RosterUnitKind.Unit),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticRosterCapabilities.RelationParameter,
                                SourceTeamRelation.OpposingSource),
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Attack)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenHeroOutput,
                                EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion)
                        }),
                    new EffectNodeDefinition(adjacentBind, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticContinuationCapabilities.SetAttackTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                SyntheticContinuationCapabilities.HeroParameter,
                                adjacent, SyntheticRosterCapabilities.ChosenHeroOutput,
                                EffectValueKind.Hero),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticContinuationCapabilities.MinionParameter,
                                adjacent, SyntheticRosterCapabilities.ChosenMinionOutput,
                                EffectValueKind.Minion)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticContinuationCapabilities.BoundHeroTargetOutput,
                                EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticContinuationCapabilities.BoundMinionTargetOutput,
                                EffectValueKind.Minion)
                        })
                });
        }
    }
}
