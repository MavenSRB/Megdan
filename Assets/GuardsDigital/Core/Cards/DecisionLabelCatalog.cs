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
        /// prints a "Choose one", and every <c>may</c> she declares sits on a CAPABILITY node - a step
        /// that may simply fail - rather than on one the resolution session stops on. That is an
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
                .Concat(WidgetEffectContent.DecisionLabels())
                // Milestone 8.57g, the milestone that deals Rowenna the Vanguard. Her fifteen
                // declarations join here rather than at milestone 8.57f, which composed them, for the
                // reason every contribution in this file joins at a dealing milestone: what a table
                // is FOR is a window over a card a match deals.
                .Concat(RowennaEffectContent.DecisionLabels())
                // Milestone 8.61j, the milestone that deals Min the Dragonmonk. Her ELEVEN
                // declarations join here rather than at the eight blocks that composed them, on the
                // same rule: her two swapping Stances and Dragon's step, Ruse's and Cobra's
                // re-places, both Grenades' "up to N enemy heroes", Perfect Self's one-or-both modal
                // and Flurry of Blows' repeat.
                .Concat(MinEffectContent.DecisionLabels())
                // Milestone 8.69g, the milestone that deals Brynn the Seeker as the TWENTIETH hero.
                // His NINETEEN declarations join here rather than at the five blocks that composed
                // them, on the same rule: what a table is FOR is a window over a card a match deals.
                // Decoy's three performances, Familiar Ground's two p.10 bullets, the two bullets each
                // of his three traps prints, Split Attack's printed "may repeat" and Peak Precision's
                // "you may retrieve" - derived at 8.69f from `DecisionLabelRequirementQuery` over his
                // dealt faces rather than counted by hand, which is what found ten of them.
                .Concat(BrynnEffectContent.DecisionLabels())
                // Milestone 8.71h, the milestone that deals Hanu the Trickster as the TWENTY-FIRST
                // hero. His FIFTEEN declarations join here rather than at the six blocks that composed
                // them, on the same rule: what a table is FOR is a window over a card a match deals.
                // Ten of them are the two bullets each of his five Reds prints, and five are the
                // printed distances This Way! and That Way! let a FRIENDLY hero choose on their own
                // seat - the first rows in this catalog that name a control another seat answers.
                // Derived at 8.71g from `DecisionLabelRequirementQuery` over his dealt faces and the
                // three end-of-turn bodies, rather than counted by hand.
                .Concat(HanuEffectContent.DecisionLabels())
                // Milestone 8.75b, the milestone that deals Ignatia the Mad as the TWENTY-SECOND
                // hero. FOUR declarations, which is the narrowest contribution any dealing has made
                // since Garrus, and the reason is the mechanism her whole set is built on: every one
                // of her eighteen dealt faces is a `Conditional` on the Tie Breaker coin rather than
                // a p.10 `Choose`, so the CARD picks which of its two printed texts applies and the
                // player is never handed a modal arm to pick it with.
                //
                // What is left is four confirm-or-declines. Two are the printed "may repeat once"
                // clauses - Loosely-Aimed Firebolts' on its MOON face, Imminent Eruption's on its SUN
                // face - derived at 8.75 from `DecisionLabelRequirementQuery` over her dealt faces
                // rather than counted by hand. The other two are Chaos Incarnate's, and they live on
                // the triggered body a p.16 permanent ability names by reference rather than on any
                // dealt face, which is why they arrive as a second member: "Flip the Tie Breaker
                // coin" and then "Perform that action again".
                //
                // Equilibrium's own question is deliberately NOT here. Its round-long free choice
                // makes `ignatia-coin-sun@1` stop reading the coin and ask the actor which face to
                // apply, and that pause is raised by a CAPABILITY whose candidates carry their own
                // words (milestone 8.75b's `EffectChoiceValueVocabulary.PrintedTextFace`, drawn as
                // "APPLY THE SUN TEXT" / "APPLY THE MOON TEXT") - so `OptionalStepQuery` names no row
                // for it and none is declared.
                .Concat(IgnatiaEffectContent.DecisionLabels())
                .Concat(IgnatiaEffectContent.UltimateDecisionLabels())
                // Milestone 9.30i, the milestone that deals Gydion the Archwizard as the TWENTY-NINTH
                // hero, joined here as the twenty-third table (Mrak, Wuk, Mortimer and Razzle declare
                // theirs beside their content). Every label is DERIVED from his content: each
                // "Choose and cast one" bullet and each of Wish's arms reads "Cast" and the spell's
                // printed name, Find Familiar's options "Remove" and the name, Prepare Spells' two
                // arms its own sentence and "Cast Wish instead", and Vampiric Touch's retrieval the
                // roster's words. GydionDealingTests asserts the set equals what
                // DecisionLabelRequirementQuery derives over his dealt fronts and spells.
                .Concat(GydionCardContent.DecisionLabels());
    }
}
