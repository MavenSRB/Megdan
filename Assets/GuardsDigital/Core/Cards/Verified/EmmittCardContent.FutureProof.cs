using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class EmmittCardContent
    {
        public const string FutureProofText =
            "Choose one — You, and friendly heroes in radius, may retrieve all cards " +
            "discarded this turn. Or: This turn: Friendly heroes in radius are immune to enemy actions.";

        public static CardDefinition FutureProofDefinition { get; } = GreenSkill(
            FutureProof, "Future Proof", CardTier.III, 5, 3, 4,
            PrintedValueChannel.Initiative, Array.Empty<CardDefinitionId>(),
            FutureProofEffect(), FutureProofText, PrintedValueChannel.Radius);

        private static EffectDefinition FutureProofEffect()
        {
            var mode = new EffectNodeId("future-proof-mode");
            var retrieval = TimeCapsuleEffect();
            var immunity = GrantEffect(FutureProof,
                EmmittActiveEffectContent.FutureProofImmunityV1);
            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(mode, EffectNodeKind.Choose,
                    EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { retrieval.RootNodeId, immunity.RootNodeId },
                    chooser: EffectChooserRole.Actor, minimumSelections: 1, maximumSelections: 1)
            };
            nodes.AddRange(retrieval.Nodes);
            nodes.AddRange(immunity.Nodes);
            return new EffectDefinition(new EffectDefinitionId(FutureProof.Value + "-skill"),
                ContentRevision, mode, nodes);
        }
    }
}
