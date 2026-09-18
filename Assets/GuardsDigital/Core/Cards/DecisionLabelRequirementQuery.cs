using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.EffectResolution;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.46a. Which kind of decision a required label answers.
    ///
    /// <para>Flags, because one node can be both. An arm of a <c>Choose</c> that is itself a
    /// <c>may</c> - Bain's "otherwise may repeat once or gain 2 coins", Ursafar's "• You may retrieve
    /// a discarded card", Whisper's Death Seeker and Lesser Evil - draws a control in the modal AND
    /// opens p.10's confirm-or-decline after it is taken. The catalog is keyed by card and node, so
    /// those two controls share one label: its chosen words serve both, and its declined words serve
    /// the pause. Two separate rows would make that one declaration look like two.</para>
    /// </summary>
    [Flags]
    public enum DecisionLabelKind
    {
        None = 0,

        /// <summary>
        /// One arm of a <c>Choose</c> node - the child node id an <c>EffectChoiceCandidate.Option</c>
        /// carries at runtime. Its label says what that printed bullet does, and it has no decline of
        /// its own: the node's own policy decides whether declining the whole modal is offered.
        /// </summary>
        ChooseArm = 1,

        /// <summary>
        /// A node that opens rulebook p.10's confirm-or-decline pause. Its label says what accepting
        /// does and, separately, what declining does.
        /// </summary>
        OptionalStep = 2
    }

    /// <summary>
    /// Milestone 8.46a. One decision control that a dealt card raises and therefore owes words.
    /// </summary>
    public readonly struct DecisionLabelRequirement : IEquatable<DecisionLabelRequirement>
    {
        public DecisionLabelRequirement(
            CardDefinitionId cardId,
            EffectDefinitionId effectDefinitionId,
            EffectNodeId nodeId,
            DecisionLabelKind kind)
        {
            CardId = cardId;
            EffectDefinitionId = effectDefinitionId;
            NodeId = nodeId;
            Kind = kind;
        }

        /// <summary>The card the pause is raised under, which is how the catalog is keyed.</summary>
        public CardDefinitionId CardId { get; }

        /// <summary>
        /// The effect body the node lives in. Carried for the failure message rather than for the key:
        /// a card's two printed bodies share one node-id namespace in the catalog, and the content
        /// check proves that namespace unambiguous.
        /// </summary>
        public EffectDefinitionId EffectDefinitionId { get; }

        public EffectNodeId NodeId { get; }

        public DecisionLabelKind Kind { get; }

        /// <summary>
        /// The same control, also answering <paramref name="kind"/> - how a node that is both a
        /// <c>Choose</c> arm and an optional step becomes one row rather than two.
        /// </summary>
        public DecisionLabelRequirement Also(DecisionLabelKind kind) =>
            new DecisionLabelRequirement(CardId, EffectDefinitionId, NodeId, Kind | kind);

        /// <summary>True when this control opens p.10's confirm-or-decline, alone or as an arm.</summary>
        public bool OpensConfirmOrDecline =>
            (Kind & DecisionLabelKind.OptionalStep) == DecisionLabelKind.OptionalStep;

        public bool Equals(DecisionLabelRequirement other) =>
            CardId.Equals(other.CardId) && NodeId.Equals(other.NodeId) && Kind == other.Kind;

        public override bool Equals(object obj) => obj is DecisionLabelRequirement other && Equals(other);

        public override int GetHashCode() =>
            ((CardId.GetHashCode() * 397) ^ NodeId.GetHashCode()) * 397 ^ (int)Kind;

        public override string ToString() => $"{CardId.Value} / {NodeId.Value} ({Kind})";
    }

    /// <summary>
    /// Milestone 8.46a. Every decision control a card's own effects can raise, derived from the
    /// definitions themselves.
    ///
    /// <para><b>Why a query rather than a list.</b> The decision window says what pressing a control
    /// does, and a control with nothing declared falls back to the node id a player cannot read. The
    /// set of controls that need words is therefore a fact about content, and it has exactly two
    /// sources: the child arms of a <c>Choose</c> node - the ids <c>EffectChoiceCandidate.Option</c>
    /// carries - and the nodes <see cref="OptionalStepQuery.OpensConfirmOrDecline"/> names. That
    /// second half is the resolver's OWN rule, asked here rather than restated, so a node kind that
    /// starts asking cannot quietly arrive without words.</para>
    ///
    /// <para><b>What it walks.</b> Every effect body a card definition can reach. Its own first: each
    /// action's printed root and, for the faces that print a second body at another of p.14's moments,
    /// that second root. Then the bodies it names BY REFERENCE, which a card definition alone cannot
    /// resolve - a p.16 permanent ability names its triggered effect on
    /// <c>UltimateRuleDefinition.TriggeredEffect</c>, and an "End of turn:" clause names its own on
    /// the <c>ActiveEffectDefinition</c> the creating capability's handler holds. Both are resolved
    /// against the build's <see cref="MatchContentCatalogs"/>, which is where the authority resolves
    /// them at runtime, and both matter here: Wasp's High Voltage, Tigerclaw's Cloak and Daggers,
    /// Tali's Blizzard, Bain's Another One! and Garrus's Battle Fury each open p.10's
    /// confirm-or-decline from a body that is not on any action of the card that raises it. The
    /// resolution carries the SOURCE card's reference on the pause, so those labels are keyed by that
    /// card exactly as an action's own are.</para>
    /// </summary>
    public static class DecisionLabelRequirementQuery
    {
        /// <summary>Every effect body <paramref name="definition"/>'s own actions declare.</summary>
        public static IEnumerable<EffectDefinition> PrintedRootsOf(CardDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            foreach (var action in definition.ActionDefinitions)
            {
                if (action.EffectRoot != null)
                {
                    yield return action.EffectRoot;
                }

                if (action.SecondCardTextRoot != null)
                {
                    yield return action.SecondCardTextRoot;
                }
            }
        }

        /// <summary>
        /// Every effect body a pause under <paramref name="definition"/> can be resolving: its own
        /// printed roots, plus every triggered body it names by reference, transitively.
        ///
        /// <para><paramref name="catalogs"/> is what turns a reference into a body. Passing none
        /// answers the printed roots alone, which is the right answer for a caller that has no build
        /// in hand and the wrong one for a coverage check - so the check passes
        /// <see cref="MatchContentCatalogs.ForThisBuild"/>.</para>
        /// </summary>
        public static IReadOnlyList<EffectDefinition> EffectRootsOf(
            CardDefinition definition, MatchContentCatalogs catalogs = null)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var roots = new List<EffectDefinition>();
            var seen = new HashSet<EffectDefinitionReference>();
            var pending = new Queue<EffectDefinition>();
            foreach (var root in PrintedRootsOf(definition))
            {
                if (seen.Add(root.Reference))
                {
                    roots.Add(root);
                    pending.Enqueue(root);
                }
            }

            if (catalogs == null)
            {
                return roots;
            }

            foreach (var referenced in NamedByUltimate(definition, catalogs))
            {
                if (seen.Add(referenced.Reference))
                {
                    roots.Add(referenced);
                    pending.Enqueue(referenced);
                }
            }

            // Transitive, because a triggered body may itself arm an Active effect that names a third.
            // Nothing in the build does that today; walking it costs one queue and removes the need to
            // notice the day something does.
            while (pending.Count > 0)
            {
                foreach (var referenced in NamedByActiveEffects(pending.Dequeue(), catalogs))
                {
                    if (seen.Add(referenced.Reference))
                    {
                        roots.Add(referenced);
                        pending.Enqueue(referenced);
                    }
                }
            }

            return roots;
        }

        /// <summary>
        /// Every node id a pause under <paramref name="definition"/> can name, which is what a declared
        /// label has to name to be about this card at all.
        /// </summary>
        public static IReadOnlyCollection<EffectNodeId> DeclaredNodeIds(
            CardDefinition definition, MatchContentCatalogs catalogs = null) =>
            new HashSet<EffectNodeId>(EffectRootsOf(definition, catalogs)
                .SelectMany(root => root.Nodes)
                .Select(node => node.Id));

        /// <summary>
        /// Every control <paramref name="definition"/> raises that owes words, one row per label the
        /// catalog has to hold.
        ///
        /// <para>Folded by node id, because the catalog is keyed by card and node. A node that is both
        /// a <c>Choose</c> arm and an optional step is one declaration answering both, and a card
        /// whose two printed bodies happen to share a node id declares one label for it - so folding
        /// is what keeps the coverage count equal to the number of labels an author writes.</para>
        /// </summary>
        public static IReadOnlyList<DecisionLabelRequirement> For(
            CardDefinition definition, MatchContentCatalogs catalogs = null) =>
            Folded(EffectRootsOf(definition, catalogs)
                .SelectMany(root => In(root, definition.Id)));

        /// <summary>Every control the whole of <paramref name="definitions"/> raises.</summary>
        public static IReadOnlyList<DecisionLabelRequirement> For(
            IEnumerable<CardDefinition> definitions, MatchContentCatalogs catalogs = null)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            return definitions.SelectMany(definition => For(definition, catalogs)).ToArray();
        }

        /// <summary>
        /// Every control the given effect bodies raise, under no card. Used by the coverage check to
        /// ask the same question of the build's registered triggered effects, which carry no card id
        /// of their own - so an asking node nobody's card reaches is visible rather than silent.
        /// </summary>
        public static IReadOnlyList<DecisionLabelRequirement> For(IEnumerable<EffectDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            // Folded per body rather than across them: with no card to key by, two bodies sharing a
            // node id - "root" is the commonest id in the whole corpus - are different controls.
            return definitions
                .SelectMany(definition => Folded(In(definition, default)))
                .ToArray();
        }

        private static IReadOnlyList<DecisionLabelRequirement> Folded(
            IEnumerable<DecisionLabelRequirement> rows)
        {
            var order = new List<DecisionLabelRequirement>();
            var positions = new Dictionary<EffectNodeId, int>();
            foreach (var row in rows)
            {
                if (positions.TryGetValue(row.NodeId, out var at))
                {
                    order[at] = order[at].Also(row.Kind);
                    continue;
                }

                positions.Add(row.NodeId, order.Count);
                order.Add(row);
            }

            return order;
        }

        private static IEnumerable<EffectDefinition> NamedByUltimate(
            CardDefinition definition, MatchContentCatalogs catalogs)
        {
            var reference = definition.UltimateRule?.TriggeredEffect;
            if (reference.HasValue &&
                catalogs.TriggeredEffects.TryGet(reference.Value, out var triggered))
            {
                yield return triggered;
            }
        }

        private static IEnumerable<EffectDefinition> NamedByActiveEffects(
            EffectDefinition root, MatchContentCatalogs catalogs)
        {
            foreach (var node in root.Nodes)
            {
                if (!node.Capability.IsDefined ||
                    !catalogs.Capabilities.TryLoad(node.Capability, out var handler))
                {
                    continue;
                }

                foreach (var active in handler.ActiveEffectDefinitions)
                {
                    if (active.TriggeredEffect.HasValue &&
                        catalogs.TriggeredEffects.TryGet(active.TriggeredEffect.Value, out var triggered))
                    {
                        yield return triggered;
                    }
                }
            }
        }

        private static IEnumerable<DecisionLabelRequirement> In(
            EffectDefinition definition, CardDefinitionId cardId)
        {
            foreach (var node in definition.Nodes)
            {
                if (node.Kind == EffectNodeKind.Choose)
                {
                    // The ARM, not the Choose node: the id a player's Select carries is the child's,
                    // and one control is drawn per printed bullet.
                    foreach (var arm in node.ChildNodeIds)
                    {
                        yield return new DecisionLabelRequirement(
                            cardId, definition.Id, arm, DecisionLabelKind.ChooseArm);
                    }

                    continue;
                }

                if (OptionalStepQuery.OpensConfirmOrDecline(node))
                {
                    yield return new DecisionLabelRequirement(
                        cardId, definition.Id, node.Id, DecisionLabelKind.OptionalStep);
                }
            }
        }
    }
}
