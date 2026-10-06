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
        public const string FleetingImageText =
            "Place an Illusion token in radius. You may swap with an Illusion token in play.";
        public const string MultipleProjectionsText =
            "Place up to 2 Illusion tokens in radius. You may swap with an Illusion token in play.";
        public const string MasterOfIllusionsText =
            "Place up to 3 Illusion tokens in radius. You may swap with an Illusion token in play.";

        public static CardDefinition FleetingImageDefinition { get; } = ImageFace(
            FleetingImage, "Fleeting Image", CardTier.I, 6, 3, 2, 1,
            PrintedValueChannel.None,
            new[] { MultipleProjections, IllusionaryForce }, FleetingImageText);

        public static CardDefinition MultipleProjectionsDefinition { get; } = ImageFace(
            MultipleProjections, "Multiple Projections", CardTier.II, 5, 3, 3, 2,
            PrintedValueChannel.Initiative,
            new[] { MasterOfIllusions, IllusionaryArmy }, MultipleProjectionsText);

        public static CardDefinition MasterOfIllusionsDefinition { get; } = ImageFace(
            MasterOfIllusions, "Master of Illusions", CardTier.III, 5, 4, 3, 3,
            PrintedValueChannel.Initiative, Array.Empty<CardDefinitionId>(),
            MasterOfIllusionsText);

        private static CardDefinition ImageFace(
            CardDefinitionId card, string title, CardTier tier, int initiative, int defense,
            int radius, int places, PrintedValueChannel item,
            CardDefinitionId[] upgrades, string text) =>
            new CardDefinition(card, ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Green, tier, item, upgrades),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, radius) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, ImageEffect(card, places)),
                    Movement(2), Defense(defense)
                },
                null, CardSourceContent.Transcribed(text));

        private static EffectDefinition ImageEffect(CardDefinitionId card, int places)
        {
            var root = new EffectNodeId(card.Value + "-root");
            var placements = new EffectNodeId(card.Value + "-placements");
            var optionalSwap = new EffectNodeId(card.Value + "-may-swap");
            var swapBody = new EffectNodeId(card.Value + "-swap-body");
            var token = new EffectNodeId(card.Value + "-illusion-in-play");
            var swap = new EffectNodeId(card.Value + "-swap");
            var placementSteps = new List<EffectNodeId>();
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { placements, optionalSwap })
            };
            for (var index = 1; index <= places; index++)
            {
                var reclaim = new EffectNodeId(card.Value + "-reclaim-" + index);
                var place = new EffectNodeId(card.Value + "-place-" + index);
                placementSteps.Add(reclaim);
                placementSteps.Add(place);
                nodes.Add(new EffectNodeDefinition(reclaim, EffectNodeKind.Capability,
                    places == 1 ? EffectFailurePolicy.IfAble : EffectFailurePolicy.May,
                    NebKherComponentContent.ReclaimIllusionV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticTokenCapabilities.ReclaimedOutput,
                            EffectValueKind.Coordinate)
                    }));
                nodes.Add(new EffectNodeDefinition(place, EffectNodeKind.Capability,
                    places == 1 ? EffectFailurePolicy.Mandatory : EffectFailurePolicy.May,
                    NebKherComponentContent.PlaceIllusionV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticTokenCapabilities.PlacedOutput,
                            EffectValueKind.Coordinate)
                    }));
            }
            nodes.Add(new EffectNodeDefinition(placements, EffectNodeKind.Sequence,
                places == 1 ? EffectFailurePolicy.Mandatory : EffectFailurePolicy.UpTo,
                childNodeIds: placementSteps));
            nodes.Add(new EffectNodeDefinition(optionalSwap, EffectNodeKind.Optional,
                EffectFailurePolicy.May, childNodeIds: new[] { swapBody },
                chooser: EffectChooserRole.Actor));
            nodes.Add(new EffectNodeDefinition(swapBody, EffectNodeKind.Sequence,
                EffectFailurePolicy.Mandatory, childNodeIds: new[] { token, swap }));
            // "In play" has no printed reach. The map is finite; a maximal integer bound
            // includes every standing token while the shared target query still enforces
            // immunity and the current board-partition overlay.
            nodes.Add(new EffectNodeDefinition(token, EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                SyntheticTargetingCapabilities.ChooseTargetV4,
                inputs: new[]
                {
                    EffectInputBinding.FromLiteral("range", EffectValue.Integer(int.MaxValue)),
                    EffectInputBinding.FromLiteral("includeSelf", EffectValue.Boolean(false)),
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.TokenKindParameterName,
                        EffectValue.TokenKind(NebKherComponentContent.Illusion.Id)),
                    EffectInputBinding.FromLiteral(
                        EffectSelectionDeclaration.ConsumerParameterName,
                        EffectValue.Node(swap))
                },
                outputs: new[]
                {
                    new EffectOutputBinding("chosen", EffectValueKind.Coordinate)
                }));
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
            return new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"),
                ContentRevision, root, nodes);
        }
    }
}
