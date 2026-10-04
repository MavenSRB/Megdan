using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class EmmittCardContent
    {
        public const string UnstableTimelineText =
            "Place 2 Glitch tokens in radius, with at least two spaces between each token; " +
            "if used as a defense, place 3 tokens instead. An enemy hero in play chooses one " +
            "of the Glitch tokens; you swap with that token. End of turn: Remove all Glitch tokens.";

        public static CardDefinition UnstableTimelineDefinition { get; } = new CardDefinition(
            UnstableTimeline, ContentRevision, "Unstable Timeline", new InitiativeValue(1),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Silver),
            new[]
            {
                new ActionDefinition(
                    PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 4) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None, default,
                    UnstableTimelineEffect("skill", 2)),
                new ActionDefinition(
                    PrimaryDefense, ActionKind.Defense, ActionDefinitionOrigin.PrintedPrimary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Defense, 6),
                        new PrintedValueDefinition(PrintedValueChannel.Radius, 4)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                    UnstableTimelineEffect("defense", 3),
                    PrintedTextComposition.None, PrintedDefenseResolution.Value,
                    CardTextTiming.DefenseAction)
            },
            null, CardSourceContent.Transcribed(UnstableTimelineText));

        private static EffectDefinition UnstableTimelineEffect(string suffix, int count)
        {
            var prefix = "unstable-timeline-" + suffix;
            var root = new EffectNodeId(prefix + "-root");
            var enemy = new EffectNodeId(prefix + "-enemy-in-play");
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

            steps.Add(enemy);
            steps.Add(token);
            steps.Add(swap);
            nodes.Insert(0, new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                EffectFailurePolicy.Mandatory, childNodeIds: steps));
            nodes.Add(new EffectNodeDefinition(enemy, EffectNodeKind.Capability,
                EffectFailurePolicy.IfAble,
                EmmittGlitchChoiceContent.EnemyHeroInPlayV1,
                outputs: new[]
                {
                    new EffectOutputBinding(EmmittGlitchChoiceContent.ChosenHeroOutput,
                        EffectValueKind.Hero)
                }));
            nodes.Add(new EffectNodeDefinition(token, EffectNodeKind.Capability,
                EffectFailurePolicy.IfAble,
                EmmittGlitchChoiceContent.ChooseGlitchByHeroV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput(
                        EmmittGlitchChoiceContent.ChooserParameter, enemy,
                        EmmittGlitchChoiceContent.ChosenHeroOutput, EffectValueKind.Hero)
                },
                outputs: new[]
                {
                    new EffectOutputBinding(EmmittGlitchChoiceContent.ChosenTokenSpaceOutput,
                        EffectValueKind.Coordinate)
                }));
            nodes.Add(new EffectNodeDefinition(swap, EffectNodeKind.Capability,
                EffectFailurePolicy.IfAble,
                SyntheticExchangeCapabilities.SwapWithActorV1,
                inputs: new[]
                {
                    EffectInputBinding.FromNodeOutput("subject", token,
                        EmmittGlitchChoiceContent.ChosenTokenSpaceOutput,
                        EffectValueKind.Coordinate),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticExchangeCapabilities.EligiblePieceParameterName,
                        SwapEligiblePieceKind.GamePiece)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("actorAt", EffectValueKind.Coordinate),
                    new EffectOutputBinding("subjectAt", EffectValueKind.Coordinate)
                }));
            return new EffectDefinition(new EffectDefinitionId(prefix + "-text"),
                ContentRevision, root, nodes);
        }

        private static EffectNodeDefinition GlitchReclaim(EffectNodeId id) =>
            new EffectNodeDefinition(id, EffectNodeKind.Capability, EffectFailurePolicy.IfAble,
                EmmittComponentContent.ReclaimGlitchV1,
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticTokenCapabilities.ReclaimedOutput,
                        EffectValueKind.Coordinate)
                });

        private static EffectNodeDefinition GlitchPlace(EffectNodeId id) =>
            new EffectNodeDefinition(id, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                EmmittComponentContent.PlaceGlitchV1,
                outputs: new[]
                {
                    new EffectOutputBinding(SyntheticTokenCapabilities.PlacedOutput,
                        EffectValueKind.Coordinate)
                });
    }
}
