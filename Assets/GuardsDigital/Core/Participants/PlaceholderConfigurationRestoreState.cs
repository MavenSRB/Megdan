using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Participants
{
    /// <summary>
    /// The complete restore inputs of a <see cref="PlaceholderOneVsOneTestConfiguration"/>.
    /// </summary>
    /// <remarks>
    /// This entry point had no defaulted parameter to remove. It carries a typed state
    /// anyway so that a later persisted configuration field cannot be added without every
    /// construction site failing to compile.
    /// </remarks>
    public sealed class PlaceholderConfigurationRestoreState
    {
        public PlaceholderConfigurationRestoreState(
            MatchParticipants participants,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            TieBreakerState tieBreakerState)
        {
            Participants = participants ?? throw new ArgumentNullException(nameof(participants));
            Definitions = Array.AsReadOnly(
                (definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray());
            Instances = Array.AsReadOnly(
                (instances ?? throw new ArgumentNullException(nameof(instances))).ToArray());
            TieBreakerState = tieBreakerState;
        }

        public MatchParticipants Participants { get; }
        public IReadOnlyList<CardDefinition> Definitions { get; }
        public IReadOnlyList<CardInstance> Instances { get; }
        public TieBreakerState TieBreakerState { get; }

        public void Deconstruct(
            out MatchParticipants participants,
            out IReadOnlyList<CardDefinition> definitions,
            out IReadOnlyList<CardInstance> instances,
            out TieBreakerState tieBreakerState)
        {
            participants = Participants;
            definitions = Definitions;
            instances = Instances;
            tieBreakerState = TieBreakerState;
        }
    }
}
