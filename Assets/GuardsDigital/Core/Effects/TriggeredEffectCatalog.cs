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
            // Milestone 8.23 moved this guarantee to where it can actually be decided, and the
            // guarantee itself is unchanged: a triggered effect must never repeat the action that
            // created its source.
            //
            // Until now the catalog refused every `RepeatAction` node outright, which was exact
            // because every triggered source was an Active effect - created by an action, running
            // inside that action's lifetime, and therefore able to name only it. Tigerclaw's Cloak and
            // Daggers is a *continuous* source: p.16's permanent ability, created by nothing, which
            // fires after an action ends and offers to repeat that action. The blanket refusal cannot
            // tell the two apart, because a catalog holds definitions and not the sources that run
            // them.
            //
            // What decides it is the frame. `TriggeredEffectResolutionAuthority` starts every
            // Active-effect occurrence with no repeatable completed action - `TriggerCandidate
            // .FromActive` passes `PerformedBasicAction.None` - so `EffectResolutionContext
            // .RepeatIsUnavailable` is true for one and its repeat node is skipped in silence. The
            // rule is therefore enforced structurally rather than by a check that has to guess, and
            // `RosterPersistentSourceTests` holds it there.

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
