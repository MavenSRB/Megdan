using System;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Synthetic
{
    /// <summary>Fixture-only sentence: move your Hazard and an adjacent enemy together two spaces.</summary>
    public static class SyntheticCoupledEscortHand
    {
        public static readonly CardDefinitionId CardId = new CardDefinitionId("synthetic-coupled-escort");
        public static ConfiguredHand Create(HeroId owner)
        {
            var basis = SyntheticShowcaseHand.CreateRed(owner);
            var definition = Definition;
            return new ConfiguredHand(basis.Definitions.Concat(new[] { definition }),
                basis.Instances.Concat(new[] { CardInstance.CreateInHand(new CardInstanceId("red-escort"), definition.Reference, owner) }),
                basis.DeckInstances);
        }

        public static CardDefinition Definition { get; } = new CardDefinition(CardId, "rev-1", "TEST - Coupled Escort",
            new InitiativeValue(40), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.BasicCard(CardColor.Silver), new[]
            {
                new ActionDefinition(new ActionDefinitionId("primary-escort"), ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary, Array.Empty<PrintedValueDefinition>(),
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true), AttackRangeKind.None, default,
                    new EffectDefinition(new EffectDefinitionId("synthetic-coupled-escort"), "rev-1", Id("root"), new[]
                    {
                        new EffectNodeDefinition(Id("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                            childNodeIds: new[] { Id("anchor"), Id("target"), Id("move") }),
                        new EffectNodeDefinition(Id("anchor"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticOwnedTokenCapabilities.OwnedHazardSpaceV1,
                            outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }),
                        new EffectNodeDefinition(Id("target"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            new EffectCapabilityReference(new EffectCapabilityId("enemy-unit-beside-anchor"), 1),
                            inputs: new[] { From("anchor", "anchor"),
                                EffectInputBinding.FromLiteral(EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(Id("move"))) },
                            outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) }),
                        new EffectNodeDefinition(Id("move"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticCoupledMoveCapabilities.MoveV1, inputs: new[]
                            {
                                From("first", "anchor"), From("second", "target"),
                                EffectInputBinding.FromLiteral("leastDistance", EffectValue.Integer(2)),
                                EffectInputBinding.FromLiteral("distance", EffectValue.Integer(2)),
                                EffectInputBinding.FromLiteral("ignoresObstacles", EffectValue.Boolean(false))
                            }, outputs: new[] { new EffectOutputBinding("firstAt", EffectValueKind.Coordinate),
                                new EffectOutputBinding("secondAt", EffectValueKind.Coordinate) })
                    }))
            });

        private static EffectNodeId Id(string name) => new EffectNodeId(name);
        private static EffectInputBinding From(string parameter, string node) =>
            EffectInputBinding.FromNodeOutput(parameter, Id(node), "space", EffectValueKind.Coordinate);
    }
}
