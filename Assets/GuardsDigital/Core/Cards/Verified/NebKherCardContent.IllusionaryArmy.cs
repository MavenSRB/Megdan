using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class NebKherCardContent
    {
        public const string IllusionaryForceText =
            "Place up to 2 Illusion tokens in radius. This round: While you are performing actions, " +
            "all Illusion tokens count as both tokens and friendly melee minions.";

        public const string IllusionaryArmyText =
            "Place up to 3 Illusion tokens in radius. This round: While you are performing actions, " +
            "all Illusion tokens count as both tokens and friendly melee minions.";

        public static CardDefinition IllusionaryForceDefinition { get; } = ArmyFace(
            IllusionaryForce, "Illusionary Force", CardTier.II, 3, 2,
            new[] { MasterOfIllusions, IllusionaryArmy }, IllusionaryForceText);

        public static CardDefinition IllusionaryArmyDefinition { get; } = ArmyFace(
            IllusionaryArmy, "Illusionary Army", CardTier.III, 4, 3,
            Array.Empty<CardDefinitionId>(), IllusionaryArmyText);

        private static CardDefinition ArmyFace(
            CardDefinitionId card, string title, CardTier tier, int defense, int places,
            CardDefinitionId[] upgrades, string text) =>
            new CardDefinition(card, ContentRevision, title, new InitiativeValue(5),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Green, tier,
                    PrintedValueChannel.Attack, upgrades),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 4) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, ArmyEffect(card, places)),
                    Movement(2), Defense(defense)
                },
                null, CardSourceContent.Transcribed(text));

        private static EffectDefinition ArmyEffect(CardDefinitionId card, int places)
        {
            var root = new EffectNodeId(card.Value + "-root");
            var placement = new EffectNodeId(card.Value + "-up-to-placements");
            var grant = new EffectNodeId(card.Value + "-count-as-minions");
            var steps = new List<EffectNodeId>();
            var nodes = new List<EffectNodeDefinition>();
            for (var index = 1; index <= places; index++)
            {
                var reclaim = new EffectNodeId(card.Value + "-reclaim-" + index);
                var place = new EffectNodeId(card.Value + "-place-" + index);
                steps.Add(reclaim);
                steps.Add(place);
                nodes.Add(new EffectNodeDefinition(reclaim, EffectNodeKind.Capability,
                    EffectFailurePolicy.May, NebKherComponentContent.ReclaimIllusionV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticTokenCapabilities.ReclaimedOutput,
                            EffectValueKind.Coordinate)
                    }));
                nodes.Add(new EffectNodeDefinition(place, EffectNodeKind.Capability,
                    EffectFailurePolicy.May, NebKherComponentContent.PlaceIllusionV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(SyntheticTokenCapabilities.PlacedOutput,
                            EffectValueKind.Coordinate)
                    }));
            }
            nodes.Insert(0, new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                EffectFailurePolicy.Mandatory, childNodeIds: new[] { placement, grant }));
            nodes.Insert(1, new EffectNodeDefinition(placement, EffectNodeKind.Sequence,
                EffectFailurePolicy.UpTo, childNodeIds: steps));
            nodes.Add(new EffectNodeDefinition(grant, EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory,
                NebKherComponentContent.IllusionsCountAsMinionsV1,
                outputs: new[]
                {
                    new EffectOutputBinding(ActiveEffectCreationHandler.AffectedOutputName,
                        EffectValueKind.Hero)
                }));
            return new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"),
                ContentRevision, root, nodes);
        }
    }
}
