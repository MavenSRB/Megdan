using System;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Synthetic
{
    /// <summary>Test-only showcase consumer for resolved discard followed by that exact primary.
    /// This is fixture content, not a verified card or a rule-source interpretation.</summary>
    public static class SyntheticResolvedRepriseHand
    {
        public static readonly CardDefinitionId CardId = new CardDefinitionId("synthetic-red-reprise");
        public static ConfiguredHand Create(HeroId owner)
        {
            var basis = SyntheticShowcaseHand.CreateRed(owner);
            var root = new EffectNodeId("reprise");
            var discard = new EffectNodeId("discard");
            var primary = new EffectNodeId("primary");
            var effect = new EffectDefinition(new EffectDefinitionId("synthetic-resolved-reprise"), "rev-1", root, new[]
            {
                new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { discard, primary }),
                new EffectNodeDefinition(discard, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticResolvedDiscardCapabilities.DiscardResolvedCardV1,
                    outputs: new[] { new EffectOutputBinding("discarded", EffectValueKind.CardInstance) }),
                new EffectNodeDefinition(primary, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticNamedBorrowCapabilities.BorrowNamedDiscardedPrimaryV1,
                    inputs: new[] { EffectInputBinding.FromNodeOutput("card", discard, "discarded", EffectValueKind.CardInstance) },
                    outputs: new[] { new EffectOutputBinding(SyntheticBorrowedActionCapabilities.BorrowedCardOutput, EffectValueKind.CardInstance) })
            });
            var definition = new CardDefinition(CardId, "rev-1", "Red Reprise", new InitiativeValue(40),
                CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.BasicCard(CardColor.Silver), new[]
                {
                    new ActionDefinition(new ActionDefinitionId("primary-reprise"), ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary, Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, effect)
                });
            return new ConfiguredHand(basis.Definitions.Concat(new[] { definition }),
                basis.Instances.Concat(new[] { CardInstance.CreateInHand(new CardInstanceId("red-reprise"), definition.Reference, owner) }),
                basis.DeckInstances);
        }
    }
}
