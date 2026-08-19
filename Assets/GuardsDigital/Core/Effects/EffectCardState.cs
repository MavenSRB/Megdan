using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// The card definitions and instances a capability may read, or the named absence of them.
    ///
    /// Milestone 7.1 built <see cref="ColorRestrictedDiscard"/> out of the three pieces the card-system
    /// contract names and found the composition unreachable from a card: the effect frame carried card
    /// definitions but not card instances once a Skill had started, so a paused colour choice could not
    /// re-derive its candidates when the answer arrived, and neither could snapshot restore. This is
    /// what the frame carries so that it can.
    ///
    /// <see cref="Absent"/> is a named absence rather than an empty view, and the difference is the
    /// whole point of the type. A capability that reads cards and is handed nothing has been asked a
    /// question it cannot answer; answering "no candidates" would be indistinguishable from the
    /// ordinary, legal answer that the chooser holds no eligible card, and the two mean opposite things
    /// to the enclosing node - one is a bug in the caller, the other is <c>if able</c> declining.
    /// Reading <see cref="Instances"/> on the absence therefore throws rather than yielding an empty
    /// set, which is what makes "no path silently substitutes an empty set for cards it was not given"
    /// structural instead of asserted. <see cref="RuleModificationSet.Empty"/> makes the same
    /// distinction for rules, for the same reason.
    ///
    /// A genuinely card-free caller states <see cref="Absent"/> rather than inheriting it, and the
    /// capabilities that never read cards - every board, component, and value capability in the build -
    /// are unaffected either way, because they never ask.
    /// </summary>
    public sealed class EffectCardState
    {
        private static readonly EffectCardState AbsentState = new EffectCardState();

        private readonly IReadOnlyList<CardDefinition> definitions;
        private readonly IReadOnlyList<CardInstance> instances;

        private EffectCardState()
        {
        }

        private EffectCardState(
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances)
        {
            this.definitions = definitions;
            this.instances = instances;
        }

        /// <summary>
        /// The stated absence of card state. Reading <see cref="Definitions"/> or
        /// <see cref="Instances"/> on it throws.
        /// </summary>
        public static EffectCardState Absent => AbsentState;

        /// <summary>
        /// The view a live resolution supplies. Both collections are copied and null entries dropped,
        /// so the frame cannot be handed a sequence that changes under it mid-resolution; an empty
        /// collection is a legitimate view and is not the same thing as <see cref="Absent"/>.
        /// </summary>
        public static EffectCardState Of(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (instances == null) throw new ArgumentNullException(nameof(instances));

            return new EffectCardState(
                new ReadOnlyCollection<CardDefinition>(
                    definitions.Where(definition => definition != null).ToArray()),
                new ReadOnlyCollection<CardInstance>(
                    instances.Where(instance => instance != null).ToArray()));
        }

        /// <summary>Whether card state was supplied at all. False only for <see cref="Absent"/>.</summary>
        public bool IsAvailable => instances != null;

        public IReadOnlyList<CardDefinition> Definitions => Available(definitions);

        public IReadOnlyList<CardInstance> Instances => Available(instances);

        private T Available<T>(T value)
            where T : class
        {
            if (!IsAvailable)
            {
                throw new InvalidOperationException(
                    "This effect resolution was given no card state, so a capability that reads cards " +
                    "cannot be answered here. Supply EffectCardState.Of(definitions, instances) at the " +
                    "resolution context rather than treating the absence as an empty hand.");
            }

            return value;
        }
    }
}
