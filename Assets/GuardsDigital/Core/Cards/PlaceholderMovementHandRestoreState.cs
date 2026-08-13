using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// The complete restore inputs of a <see cref="PlaceholderMovementHand"/>.
    /// </summary>
    /// <remarks>
    /// This entry point had no defaulted parameter to remove. It carries a typed state
    /// anyway so that a later persisted hand field cannot be added without every
    /// construction site failing to compile - the enforcement this milestone establishes
    /// is against future additions, not only against the defaults present today.
    /// </remarks>
    public sealed class PlaceholderMovementHandRestoreState
    {
        public PlaceholderMovementHandRestoreState(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            Definitions = Array.AsReadOnly(
                (definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray());
            Instances = Array.AsReadOnly(
                (instances ?? throw new ArgumentNullException(nameof(instances))).ToArray());
        }

        public IReadOnlyList<CardDefinition> Definitions { get; }
        public IReadOnlyList<CardInstance> Instances { get; }

        public void Deconstruct(
            out IReadOnlyList<CardDefinition> definitions,
            out IReadOnlyList<CardInstance> instances)
        {
            definitions = Definitions;
            instances = Instances;
        }
    }
}
