using System;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class NebKherCardContent
    {
        public const string TwistFateText =
            "Target a unit in range. After the attack: You may swap an enemy unit in range " +
            "with an Illusion token adjacent to you.";
        public const string DeviousSchemeText =
            "Target a unit in range. After the attack: You may swap an enemy unit in range " +
            "with an Illusion token in range.";

        public static CardDefinition TwistFateDefinition { get; } = FateFace(
            TwistFate, "Twist Fate", CardTier.II, 7,
            new[] { PhantasmalChampion, DeviousScheme }, false, TwistFateText);

        public static CardDefinition DeviousSchemeDefinition { get; } = FateFace(
            DeviousScheme, "Devious Scheme", CardTier.III, 8,
            Array.Empty<CardDefinitionId>(), true, DeviousSchemeText);

        private static CardDefinition FateFace(CardDefinitionId card, string title,
            CardTier tier, int initiative, CardDefinitionId[] upgrades,
            bool tokenInRange, string text) =>
            new CardDefinition(card, ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Red, tier,
                    PrintedValueChannel.Defense, upgrades),
                new[]
                {
                    new ActionDefinition(
                        PrimaryAttack, ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Attack, 4),
                            new PrintedValueDefinition(PrintedValueChannel.Range, 2)
                        },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.Ranged, default,
                        FateSwap(card, tokenInRange),
                        PrintedTextComposition.None, PrintedDefenseResolution.None,
                        CardTextTiming.AfterTheAttack,
                        PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged)),
                    Movement(3), Defense(6)
                },
                null, CardSourceContent.Transcribed(text));

        private static EffectDefinition FateSwap(CardDefinitionId card, bool tokenInRange)
        {
            EffectNodeId N(string suffix) => new EffectNodeId(card.Value + "-" + suffix);
            var root = N("may-swap");
            var body = N("swap-body");
            var unit = N("enemy-unit");
            var image = N("illusion");
            var swap = N("swap");
            return new EffectDefinition(new EffectDefinitionId(card.Value + "-attack-text"),
                ContentRevision, root, new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Optional,
                        EffectFailurePolicy.May, childNodeIds: new[] { body },
                        chooser: EffectChooserRole.Actor),
                    new EffectNodeDefinition(body, EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { unit, image, swap }),
                    new EffectNodeDefinition(unit, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RangeUnitTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Attack),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.OpposingSource)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionOutputName,
                                EffectValueKind.Minion),
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                                EffectValueKind.Coordinate)
                        }),
                    new EffectNodeDefinition(image, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        tokenInRange
                            ? SyntheticTargetingCapabilities.RangeTargetV1
                            : SyntheticTargetingCapabilities.ChooseTargetV4,
                        inputs: tokenInRange
                            ? RangeIllusionInputs(swap)
                            : AdjacentIllusionInputs(swap),
                        outputs: new[] { new EffectOutputBinding("chosen",
                            EffectValueKind.Coordinate) }),
                    new EffectNodeDefinition(swap, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticExchangeCapabilities.SwapV2,
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput("first", unit,
                                SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName,
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromNodeOutput("second", image, "chosen",
                                EffectValueKind.Coordinate),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                                SwapEligiblePieceKind.GamePiece)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("firstAt", EffectValueKind.Coordinate),
                            new EffectOutputBinding("secondAt", EffectValueKind.Coordinate)
                        })
                });
        }

        private static EffectInputBinding[] RangeIllusionInputs(EffectNodeId consumer) =>
            new[]
            {
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
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
                    EffectValue.Node(consumer))
            };

        private static EffectInputBinding[] AdjacentIllusionInputs(EffectNodeId consumer) =>
            new[]
            {
                EffectInputBinding.FromLiteral("range", EffectValue.Integer(1)),
                EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                EffectInputBinding.FromLiteral(
                    SyntheticTargetingCapabilities.TokenKindParameterName,
                    EffectValue.TokenKind(NebKherComponentContent.Illusion.Id)),
                EffectInputBinding.FromLiteral(
                    EffectSelectionDeclaration.ConsumerParameterName,
                    EffectValue.Node(consumer))
            };
    }
}
