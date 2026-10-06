using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class NebKherCardContent
    {
        public const string DiabolicalLaughterText =
            "Laugh diabolically; if you do, choose up to three times — Swap with an Illusion " +
            "token in radius. Or: Place an Illusion token in an adjacent space. Or: Swap two " +
            "resolved cards of an enemy hero in radius, without canceling active effects.";

        public static CardDefinition DiabolicalLaughterDefinition { get; } =
            new CardDefinition(DiabolicalLaughter, ContentRevision, "Diabolical Laughter",
                new InitiativeValue(3), CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(CardColor.Silver),
                new[]
                {
                    new ActionDefinition(PrimarySkill, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 4) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, LaughterEffect()),
                    Defense(3)
                }, null, CardSourceContent.Transcribed(DiabolicalLaughterText));

        private static EffectDefinition LaughterEffect()
        {
            EffectNodeId N(string name) => new EffectNodeId(DiabolicalLaughter.Value + "-" + name);
            var root = N("laugh-optional");
            var body = N("laugh-body");
            var laugh = N("laugh");
            var choices = N("choices");
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(root, EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { body }, chooser: EffectChooserRole.Actor),
                new EffectNodeDefinition(body, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { laugh, choices }),
                new EffectNodeDefinition(laugh, EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory, LaughterCapabilities.LaughV1)
            };
            var passes = new List<EffectNodeId>();
            for (var number = 1; number <= 3; number++)
            {
                var prefix = "choice-" + number;
                var optional = N(prefix + "-optional");
                var modal = N(prefix + "-modal");
                var swapBody = N(prefix + "-swap-body");
                var token = N(prefix + "-token");
                var swap = N(prefix + "-swap");
                var placeBody = N(prefix + "-place-body");
                var reclaim = N(prefix + "-reclaim");
                var place = N(prefix + "-place");
                var cardBody = N(prefix + "-cards-body");
                var enemy = N(prefix + "-enemy");
                var cardSwap = N(prefix + "-card-swap");
                passes.Add(optional);
                nodes.Add(new EffectNodeDefinition(optional, EffectNodeKind.Optional,
                    EffectFailurePolicy.UpTo, childNodeIds: new[] { modal },
                    chooser: EffectChooserRole.Actor));
                nodes.Add(new EffectNodeDefinition(modal, EffectNodeKind.Choose,
                    EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { swapBody, placeBody, cardBody },
                    chooser: EffectChooserRole.Actor, minimumSelections: 1,
                    maximumSelections: 1));

                nodes.Add(new EffectNodeDefinition(swapBody, EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory, childNodeIds: new[] { token, swap }));
                nodes.Add(new EffectNodeDefinition(token, EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.IncludeSelfParameterName,
                            EffectValue.Boolean(false)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.AdmittedObjectKindParameterName,
                            PrintedIconObjectKind.TokensOnly),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.TokenKindParameterName,
                            EffectValue.TokenKind(NebKherComponentContent.Illusion.Id)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValue.Node(swap))
                    },
                    outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) }));
                nodes.Add(new EffectNodeDefinition(swap, EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.SwapWithActorV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("subject", token, "chosen",
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

                nodes.Add(new EffectNodeDefinition(placeBody, EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory, childNodeIds: new[] { reclaim, place }));
                nodes.Add(new EffectNodeDefinition(reclaim, EffectNodeKind.Capability,
                    EffectFailurePolicy.IfAble, NebKherComponentContent.ReclaimIllusionV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticTokenCapabilities.ReclaimedOutput,
                            EffectValueKind.Coordinate)
                    }));
                nodes.Add(new EffectNodeDefinition(place, EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    NebKherComponentContent.PlaceAdjacentIllusionV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticTokenCapabilities.PlacedOutput,
                            EffectValueKind.Coordinate)
                    }));

                nodes.Add(new EffectNodeDefinition(cardBody, EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory, childNodeIds: new[] { enemy, cardSwap }));
                nodes.Add(new EffectNodeDefinition(enemy, EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            SourceTeamRelation.OpposingSource),
                        EffectInputBinding.FromLiteral(
                            SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                            EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValue.Node(cardSwap))
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    }));
                nodes.Add(new EffectNodeDefinition(cardSwap, EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticResolvedCardOrderSwapCapabilities.SwapResolvedPairV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticResolvedCardOrderSwapCapabilities.OwnerParameterName,
                            enemy, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticResolvedCardOrderSwapCapabilities.FirstOutputName,
                            EffectValueKind.CardInstance),
                        new EffectOutputBinding(
                            SyntheticResolvedCardOrderSwapCapabilities.SecondOutputName,
                            EffectValueKind.CardInstance)
                    }));
            }
            nodes.Add(new EffectNodeDefinition(choices, EffectNodeKind.Sequence,
                EffectFailurePolicy.UpTo, childNodeIds: passes));
            return new EffectDefinition(
                new EffectDefinitionId(DiabolicalLaughter.Value + "-skill"),
                ContentRevision, root, nodes);
        }
    }
}
