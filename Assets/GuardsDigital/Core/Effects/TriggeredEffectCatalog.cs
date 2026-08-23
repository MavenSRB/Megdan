using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GuardsDigital.Core.Effects
{
    /// <summary>Trusted effect definitions entered from timing sources rather than action roots.</summary>
    public sealed class TriggeredEffectCatalog
    {
        private readonly IReadOnlyDictionary<EffectDefinitionReference, EffectDefinition> definitions;

        public TriggeredEffectCatalog(IEnumerable<EffectDefinition> definitions)
        {
            var materialized = (definitions ?? throw new ArgumentNullException(nameof(definitions))).ToArray();
            if (materialized.Any(value => value == null) ||
                materialized.Select(value => value.Reference).Distinct().Count() != materialized.Length)
                throw new ArgumentException("Triggered-effect definitions must be non-null and uniquely referenced.", nameof(definitions));
            if (materialized.Any(value => value.Nodes.Any(node => node.Kind == EffectNodeKind.RepeatAction)))
                throw new ArgumentException(
                    "A triggered effect cannot request repetition of the action that originally created its source.",
                    nameof(definitions));
            this.definitions = new ReadOnlyDictionary<EffectDefinitionReference, EffectDefinition>(
                materialized.ToDictionary(value => value.Reference));
        }

        public IReadOnlyList<EffectDefinition> Definitions => definitions.Values.ToArray();

        public bool TryGet(EffectDefinitionReference reference, out EffectDefinition definition) =>
            definitions.TryGetValue(reference, out definition);

        public static TriggeredEffectCatalog Empty { get; } =
            new TriggeredEffectCatalog(Array.Empty<EffectDefinition>());
    }
}
