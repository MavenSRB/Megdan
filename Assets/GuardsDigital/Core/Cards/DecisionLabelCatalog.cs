using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards.Verified;
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
            var tables = new Dictionary<CardDefinitionId, Dictionary<EffectNodeId, DecisionLabel>>();
            foreach (var declaration in Declared())
            {
                if (!tables.TryGetValue(declaration.CardId, out var table))
                {
                    table = new Dictionary<EffectNodeId, DecisionLabel>();
                    tables.Add(declaration.CardId, table);
                }

                // Milestone 8.46a. One node under one card has one label: the window asks by that pair
                // and gets one answer, so a second declaration for the same pair is an authoring
                // mistake rather than an override. Saying so here is what keeps a dual-role node - an
                // arm that is itself a `may` - an obvious single declaration rather than two halves
                // that silently replace each other.
                if (table.ContainsKey(declaration.NodeId))
                {
                    throw new InvalidOperationException(
                        $"Card '{declaration.CardId.Value}' declares two decision labels for node " +
                        $"'{declaration.NodeId.Value}'.");
                }

                table.Add(declaration.NodeId, declaration.Label);
            }

            return tables.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyDictionary<EffectNodeId, DecisionLabel>)pair.Value);
        }

        /// <summary>
        /// Milestone 8.46a. Every hero's own table, aggregated exactly as
        /// <see cref="MatchContentCatalogs"/> aggregates capability handlers: one contribution per
        /// hero, declared beside that hero's effect content, named here in the roster's own order.
        ///
        /// <para>Silverarrow contributes nothing and is named all the same, for the reason Sabina's
        /// empty handler contribution is named in <see cref="MatchContentCatalogs"/>: no face of hers
        /// prints a "Choose one" and none of her five <c>may</c> declarations sits on a node the
        /// resolution session stops on - every one is a capability that may simply fail. That is an
        /// explicit empty contribution rather than a missing table, and
        /// <c>DecisionLabelCoverageTests</c> is what would say so if it stopped being true.</para>
        /// </summary>
        private static IEnumerable<DecisionLabelDeclaration> Declared() =>
            WaspEffectContent.DecisionLabels()
                .Concat(ArienEffectContent.DecisionLabels())
                .Concat(BroganEffectContent.DecisionLabels())
                .Concat(SabinaEffectContent.DecisionLabels())
                .Concat(TigerclawEffectContent.DecisionLabels())
                .Concat(XargathaEffectContent.DecisionLabels())
                .Concat(DodgerEffectContent.DecisionLabels())
                .Concat(MisaEffectContent.DecisionLabels())
                // Silverarrow: zero declarations, and deliberately so - see above.
                .Concat(Enumerable.Empty<DecisionLabelDeclaration>())
                .Concat(UrsafarEffectContent.DecisionLabels())
                .Concat(WhisperEffectContent.DecisionLabels())
                .Concat(TaliEffectContent.DecisionLabels())
                .Concat(SwiftEffectContent.DecisionLabels())
                .Concat(CutterEffectContent.DecisionLabels())
                .Concat(BainEffectContent.DecisionLabels())
                .Concat(GarrusEffectContent.DecisionLabels())
                .Concat(WidgetEffectContent.DecisionLabels());
    }
}
