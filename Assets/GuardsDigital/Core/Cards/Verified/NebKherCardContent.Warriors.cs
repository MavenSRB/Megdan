using System;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class NebKherCardContent
    {
        public const string PhantasmalWarriorText =
            "Choose one — Before the attack: Move an Illusion token in range up to 1 space " +
            "to a space adjacent to an enemy hero in range; target that hero. " +
            "Or: Target a unit adjacent to you.";

        public const string PhantasmalChampionText =
            "Choose one — Before the attack: Move an Illusion token in range up to 2 spaces " +
            "to a space adjacent to an enemy hero in range; target that hero. " +
            "Or: Target a unit adjacent to you.";

        public static CardDefinition PhantasmalWarriorDefinition { get; } = WarriorFace(
            PhantasmalWarrior, "Phantasmal Warrior", CardTier.II, 7, 7, 1,
            PrintedValueChannel.Initiative,
            new[] { PhantasmalChampion, DeviousScheme }, PhantasmalWarriorText);

        public static CardDefinition PhantasmalChampionDefinition { get; } = WarriorFace(
            PhantasmalChampion, "Phantasmal Champion", CardTier.III, 8, 7, 2,
            PrintedValueChannel.Radius, Array.Empty<CardDefinitionId>(),
            PhantasmalChampionText);

        private static CardDefinition WarriorFace(CardDefinitionId card, string title,
            CardTier tier, int initiative, int defense, int imageMove,
            PrintedValueChannel item, CardDefinitionId[] upgrades, string text) =>
            new CardDefinition(card, ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Red, tier, item, upgrades),
                new[]
                {
                    new ActionDefinition(
                        PrimaryAttack, ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, 3),
                            new PrintedValueDefinition(PrintedValueChannel.Range, 5)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged, default,
                        WarriorTarget(card, imageMove),
                        PrintedTextComposition.None, PrintedDefenseResolution.None,
                        CardTextTiming.BeforeTheAttack,
                        PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged)
                            .BoundByCardText()),
                    Movement(3), Defense(defense)
                },
                null, CardSourceContent.Transcribed(text));

        private static EffectDefinition WarriorTarget(CardDefinitionId card, int distance)
        {
            EffectNodeId N(string suffix) => new EffectNodeId(card.Value + "-" + suffix);
            var root = N("choice");
            var imageArm = N("image-arm");
            var hero = N("enemy-hero");
            var image = N("image-in-range");
            var move = N("move-image");
            var bindHero = N("bind-hero");
            var adjacentArm = N("adjacent-arm");
            var adjacent = N("adjacent-unit");
            var bindAdjacent = N("bind-adjacent");
            return new EffectDefinition(new EffectDefinitionId(card.Value + "-attack-text"),
                ContentRevision, root, new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Choose,
                        EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { imageArm, adjacentArm },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1, maximumSelections: 1),
                    new EffectNodeDefinition(imageArm, EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { hero, image, move, bindHero }),
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
                                EffectValue.Boolean(false))
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
                                EffectValue.TokenKind(NebKherComponentContent.Illusion.Id)),
                            EffectInputBinding.FromLiteral(
                                EffectSelectionDeclaration.ConsumerParameterName,
                                EffectValue.Node(move))
                        },
                        outputs: new[] { new EffectOutputBinding("chosen",
                            EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(move, EffectNodeKind.Capability,
                        EffectFailurePolicy.UpTo,
                        SyntheticDisplacementCapabilities.DirectMoveV3,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput("mover", image, "chosen",
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromLiteral("distance",
                                EffectValue.Integer(distance)),
                            EffectInputBinding.FromLiteral(
                                SyntheticDisplacementCapabilities.LeastDistanceParameterName,
                                EffectValue.Integer(0)),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                                DirectMoveDestinationPolicy.AdjacentToTheNamedSpace),
                            EffectInputBinding.FromNodeOutput(
                                SyntheticDisplacementCapabilities.BesideSpaceParameterName,
                                hero,
                                SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                                EffectValueKind.Coordinate)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(bindHero, EffectNodeKind.Capability,
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
                        childNodeIds: new[] { adjacent, bindAdjacent }),
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
                    new EffectNodeDefinition(bindAdjacent, EffectNodeKind.Capability,
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
