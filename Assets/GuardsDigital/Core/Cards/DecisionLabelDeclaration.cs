using System;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.46a. One hero's declaration that a named node of a named card says a named thing
    /// when its control is pressed - the row a per-hero table yields and
    /// <see cref="DecisionLabelCatalog"/> aggregates.
    ///
    /// <para><b>Why a flat row rather than a nested table per hero.</b> Every hero declares the same
    /// shape, so the catalog's aggregation is one <c>Concat</c> per hero exactly as
    /// <see cref="MatchContentCatalogs"/>'s handler list is, and a hero whose cards raise nothing
    /// declares nothing rather than an empty dictionary. Grouping into the per-card tables the lookup
    /// reads is the catalog's job, done once, where a duplicate node under one card is a loud failure
    /// instead of a silent overwrite.</para>
    ///
    /// <para><b>Two factories rather than one, because the shape is the declaration.</b> A
    /// <c>Choose</c> arm has no decline of its own - whether the modal may be declined is the
    /// <c>Choose</c> node's own policy, and one arm cannot answer for it - while an optional step has
    /// exactly two answers and owes words for both. Naming that at the call site is what lets the
    /// content check compare the shape an author declared against the shape the resolver's own rule
    /// says that node opens. A node that is BOTH - an arm that is itself a <c>may</c>, which Bain's
    /// We're Not Done Yet!, Ursafar's two Green modals and Whisper's Death Seeker and Lesser Evil all
    /// print - is declared with <see cref="Step"/>: its chosen words serve both controls and its
    /// declined words serve the pause that follows.</para>
    /// </summary>
    public readonly struct DecisionLabelDeclaration
    {
        public DecisionLabelDeclaration(CardDefinitionId cardId, EffectNodeId nodeId, DecisionLabel label)
        {
            if (!cardId.IsDefined)
            {
                throw new ArgumentException("A decision label names the card that raises it.", nameof(cardId));
            }

            if (!nodeId.IsDefined)
            {
                throw new ArgumentException("A decision label names the node it answers.", nameof(nodeId));
            }

            if (!label.IsDefined)
            {
                throw new ArgumentException("A decision label carries words.", nameof(label));
            }

            CardId = cardId;
            NodeId = nodeId;
            Label = label;
        }

        public CardDefinitionId CardId { get; }

        public EffectNodeId NodeId { get; }

        public DecisionLabel Label { get; }

        /// <summary>
        /// One printed bullet of a <c>Choose</c>, named by the child node id the chooser's Select
        /// carries.
        /// </summary>
        public static DecisionLabelDeclaration DecisionArm(
            CardDefinitionId cardId, string nodeId, string chosen) =>
            new DecisionLabelDeclaration(cardId, new EffectNodeId(nodeId), new DecisionLabel(chosen));

        /// <summary>
        /// A node that opens rulebook p.10's confirm-or-decline, named by both of its consequences -
        /// what accepting does and what declining does.
        /// </summary>
        public static DecisionLabelDeclaration DecisionStep(
            CardDefinitionId cardId, string nodeId, string chosen, string declined) =>
            new DecisionLabelDeclaration(
                cardId, new EffectNodeId(nodeId), new DecisionLabel(chosen, declined));

        public override string ToString() => $"{CardId.Value} / {NodeId.Value}: {Label}";
    }
}
