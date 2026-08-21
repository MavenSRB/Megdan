using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// The complete restore inputs of a <see cref="ConfiguredHand"/>.
    /// </summary>
    /// <remarks>
    /// This entry point had no defaulted parameter to remove. It carries a typed state
    /// anyway so that a later persisted hand field cannot be added without every
    /// construction site failing to compile - the enforcement this milestone establishes
    /// is against future additions, not only against the defaults present today.
    /// </remarks>
    public sealed class ConfiguredHandRestoreState
    {
        public ConfiguredHandRestoreState(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            IEnumerable<CardInstance> deckInstances)
        {
            Definitions = Array.AsReadOnly(
                (definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray());
            Instances = Array.AsReadOnly(
                (instances ?? throw new ArgumentNullException(nameof(instances))).ToArray());
            DeckInstances = Array.AsReadOnly(
                (deckInstances ?? throw new ArgumentNullException(nameof(deckInstances))).ToArray());
        }

        public IReadOnlyList<CardDefinition> Definitions { get; }
        public IReadOnlyList<CardInstance> Instances { get; }

        /// <summary>
        /// Milestone 7.5. The cards p.6's setup left in the deck, kept apart from the dealt ones for
        /// the reason <c>ConfiguredHand.DeckInstances</c> gives: the split is a setup fact, and
        /// where each card is right now lives on the card.
        /// </summary>
        public IReadOnlyList<CardInstance> DeckInstances { get; }

        public void Deconstruct(
            out IReadOnlyList<CardDefinition> definitions,
            out IReadOnlyList<CardInstance> instances,
            out IReadOnlyList<CardInstance> deckInstances)
        {
            definitions = Definitions;
            instances = Instances;
            deckInstances = DeckInstances;
        }
    }
}
