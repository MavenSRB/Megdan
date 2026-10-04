using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class EmmittCardContent
    {
        public const string FlashbackText =
            "Target a unit adjacent to you. After the attack: You may place 3 Glitch tokens in " +
            "radius, with at least two spaces between each token; if you do, up to 1 enemy hero " +
            "in radius swaps with a Glitch token of their choice. End of turn: Remove all Glitch tokens.";

        public const string DejaVuText =
            "Target a unit adjacent to you. After the attack: You may place 2 Glitch tokens in " +
            "radius, with at least two spaces between each token; if you do, up to 1 enemy hero " +
            "in radius swaps with a Glitch token of their choice. End of turn: Remove all Glitch tokens.";

        public static CardDefinition FlashbackDefinition { get; } = GlitchAttack(
            Flashback, "Flashback", CardTier.II, 7, 6, 5, 3,
            new[] { TemporalJudgement, DejaVu }, FlashbackText);

        public static CardDefinition DejaVuDefinition { get; } = GlitchAttack(
            DejaVu, "Déjà Vu", CardTier.III, 8, 7, 6, 2,
            Array.Empty<CardDefinitionId>(), DejaVuText);

        private static CardDefinition GlitchAttack(CardDefinitionId id, string title, CardTier tier,
            int initiative, int defense, int attack, int count,
            IReadOnlyList<CardDefinitionId> upgrades, string text) => new CardDefinition(
            id, ContentRevision, title, new InitiativeValue(initiative),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.NonBasicCard(
                CardColor.Red, tier, PrintedValueChannel.Defense, upgrades),
            new[]
            {
                new ActionDefinition(
                    PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Attack, attack),
                        new PrintedValueDefinition(PrintedValueChannel.Radius, 3)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged, default,
                    GlitchAttackEffect(id, count),
                    PrintedTextComposition.None, PrintedDefenseResolution.None,
                    CardTextTiming.AfterTheAttack,
                    PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged)),
                Movement(3), Defense(defense)
            },
            null, CardSourceContent.Transcribed(text));

        private static EffectDefinition GlitchAttackEffect(CardDefinitionId card, int count)
        {
            var prefix = card.Value;
            var mayPlace = new EffectNodeId(prefix + "-may-place-glitches");
            var sequence = new EffectNodeId(prefix + "-if-placed");
            var upToOne = new EffectNodeId(prefix + "-up-to-one-enemy");
            var enemySwap = new EffectNodeId(prefix + "-enemy-swaps");
            var target = new EffectNodeId(prefix + "-enemy-in-radius");
            var token = new EffectNodeId(prefix + "-enemy-chooses-glitch");
            var swap = new EffectNodeId(prefix + "-swap");
            var steps = new List<EffectNodeId>();
            var nodes = new List<EffectNodeDefinition>();
            for (var index = 1; index <= count; index++)
            {
                var reclaim = new EffectNodeId(prefix + "-reclaim-" + index);
                var place = new EffectNodeId(prefix + "-place-" + index);
                steps.Add(reclaim);
                steps.Add(place);
                nodes.Add(GlitchReclaim(reclaim));
                nodes.Add(GlitchPlace(place));
            }

            steps.Add(upToOne);
            nodes.Insert(0, new EffectNodeDefinition(mayPlace, EffectNodeKind.Optional,
                EffectFailurePolicy.May, childNodeIds: new[] { sequence },
                chooser: EffectChooserRole.Actor));
            nodes.Insert(1, new EffectNodeDefinition(sequence, EffectNodeKind.Sequence,
                EffectFailurePolicy.Mandatory, childNodeIds: steps));
            nodes.Add(new EffectNodeDefinition(upToOne, EffectNodeKind.Optional,
                EffectFailurePolicy.UpTo, childNodeIds: new[] { enemySwap },
                chooser: EffectChooserRole.Actor));
            nodes.Add(new EffectNodeDefinition(enemySwap, EffectNodeKind.Sequence,
                EffectFailurePolicy.Mandatory, childNodeIds: new[] { target, token, swap }));
            nodes.Add(new EffectNodeDefinition(target, EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Attack),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticTargetingCapabilities.TargetRelationParameterName,
                        SourceTeamRelation.OpposingSource),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
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
                }));
            nodes.Add(new EffectNodeDefinition(token, EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                EmmittGlitchChoiceContent.ChooseGlitchByHeroV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        EmmittGlitchChoiceContent.ChooserParameter, target,
                        SyntheticTargetingCapabilities.ChosenHeroOutputName, EffectValueKind.Hero)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(
                        EmmittGlitchChoiceContent.ChosenTokenSpaceOutput,
                        EffectValueKind.Coordinate)
                }));
            nodes.Add(new EffectNodeDefinition(swap, EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                SyntheticExchangeCapabilities.SwapV2,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput("first", target,
                        SyntheticTargetingCapabilities.ChosenHeroSpaceOutputName,
                        EffectValueKind.Coordinate),
                    EffectInputBinding.FromNodeOutput("second", token,
                        EmmittGlitchChoiceContent.ChosenTokenSpaceOutput,
                        EffectValueKind.Coordinate),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticExchangeCapabilities.EligiblePieceParameterName,
                        SwapEligiblePieceKind.GamePiece)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("firstAt", EffectValueKind.Coordinate),
                    new EffectOutputBinding("secondAt", EffectValueKind.Coordinate)
                }));
            return new EffectDefinition(new EffectDefinitionId(prefix + "-attack-text"),
                ContentRevision, mayPlace, nodes);
        }
    }
}
