using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// The immutable, catalog-owned description of what one Skill or other effect does: a root node and
    /// a flat table of stable nodes reachable from it.
    ///
    /// This is definition data, never runtime state. A pending resolution references it by
    /// <see cref="Reference"/> and stores its own position and outcomes; it never carries a copy of
    /// these semantics, which is what lets a restored snapshot rebind to the trusted catalog rather
    /// than reconstruct meaning from a payload.
    /// </summary>
    public sealed class EffectDefinition
    {
        public EffectDefinition(
            EffectDefinitionId id,
            string contentRevision,
            EffectNodeId rootNodeId,
            IEnumerable<EffectNodeDefinition> nodes)
        {
            Reference = new EffectDefinitionReference(id, contentRevision);

            if (!rootNodeId.IsDefined)
            {
                throw new ArgumentException("An effect definition requires a root node.", nameof(rootNodeId));
            }

            var table = (nodes ?? throw new ArgumentNullException(nameof(nodes))).ToArray();
            if (table.Length == 0)
            {
                throw new ArgumentException("An effect definition requires at least one node.", nameof(nodes));
            }

            if (table.Any(node => node == null))
            {
                throw new ArgumentException("Effect definition nodes must be non-null.", nameof(nodes));
            }

            if (table.Select(node => node.Id).Distinct().Count() != table.Length)
            {
                throw new ArgumentException("Effect node IDs must be unique within a definition.", nameof(nodes));
            }

            if (table.All(node => !node.Id.Equals(rootNodeId)))
            {
                throw new ArgumentException(
                    "An effect definition's root node must appear in its node table.", nameof(rootNodeId));
            }

            Id = id;
            ContentRevision = contentRevision;
            RootNodeId = rootNodeId;
            Nodes = new ReadOnlyCollection<EffectNodeDefinition>(table);
            nodesById = table.ToDictionary(node => node.Id);
        }

        private readonly Dictionary<EffectNodeId, EffectNodeDefinition> nodesById;

        public EffectDefinitionId Id { get; }
        public string ContentRevision { get; }
        public EffectDefinitionReference Reference { get; }
        public EffectNodeId RootNodeId { get; }
        public IReadOnlyList<EffectNodeDefinition> Nodes { get; }

        public EffectNodeDefinition Root => nodesById[RootNodeId];

        /// <summary>
        /// Resolves a node this definition declares. A snapshot's continuation frame is checked through
        /// <see cref="TryGetNode"/> instead, because an unknown node there is a rejected payload rather
        /// than a programming error.
        /// </summary>
        public EffectNodeDefinition GetNode(EffectNodeId nodeId)
        {
            if (!nodesById.TryGetValue(nodeId, out var node))
            {
                throw new ArgumentException(
                    $"Effect definition '{Id}' declares no node '{nodeId}'.", nameof(nodeId));
            }

            return node;
        }

        public bool TryGetNode(EffectNodeId nodeId, out EffectNodeDefinition node) =>
            nodesById.TryGetValue(nodeId, out node);
    }
}
