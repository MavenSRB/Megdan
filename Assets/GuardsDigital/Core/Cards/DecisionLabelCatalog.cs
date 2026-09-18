using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.46. The one place presentation asks what a decision control should say, keyed by
    /// the card whose effect raised the pause and the node the control answers: a <c>Choose</c> node's
    /// child arm, or the optional (<c>may</c>) node that opened confirm-or-decline.
    ///
    /// <para><b>Staged inert by the series head.</b> Until milestone 8.46a fills it from each hero's
    /// own content, every lookup answers <c>false</c> and presentation keeps the label it draws today,
    /// so the window (8.46c) and the labels (8.46a) can be built in parallel against one contract.
    /// 8.46a aggregates per-hero tables here exactly as <see cref="MatchContentCatalogs"/> aggregates
    /// capability handlers, and its content check derives the set of nodes that need a label from the
    /// resolver's own rule rather than from a second list.</para>
    ///
    /// <para>Nothing here is read by any rule. The catalog sits beside the definitions rather than
    /// inside them: no card definition, content digest, snapshot schema or wire revision moves when a
    /// label is added or reworded.</para>
    /// </summary>
    public static class DecisionLabelCatalog
    {
        private static readonly IReadOnlyDictionary<CardDefinitionId, IReadOnlyDictionary<EffectNodeId, DecisionLabel>>
            Tables = BuildTables();

        /// <summary>
        /// The declared label for <paramref name="nodeId"/> under <paramref name="cardId"/>, or
        /// <c>false</c> when the content declares none.
        /// </summary>
        public static bool TryGetLabel(CardDefinitionId cardId, EffectNodeId nodeId, out DecisionLabel label)
        {
            label = default;
            if (!cardId.IsDefined || !nodeId.IsDefined)
            {
                return false;
            }

            return Tables.TryGetValue(cardId, out var table) && table.TryGetValue(nodeId, out label);
        }

        /// <summary>Every declared table, for the content check that proves the roster complete.</summary>
        public static IReadOnlyDictionary<CardDefinitionId, IReadOnlyDictionary<EffectNodeId, DecisionLabel>> Registered =>
            Tables;

        private static IReadOnlyDictionary<CardDefinitionId, IReadOnlyDictionary<EffectNodeId, DecisionLabel>> BuildTables()
        {
            // 8.46a replaces this with the aggregation of every hero's declared table.
            return new Dictionary<CardDefinitionId, IReadOnlyDictionary<EffectNodeId, DecisionLabel>>();
        }
    }
}
