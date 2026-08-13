using System;
using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Timing
{
    /// <summary>
    /// The complete persisted state of an <see cref="ActiveEffectLifecycle"/>.
    /// </summary>
    /// <remarks>
    /// Every member is a required constructor argument, so a persisted field cannot be
    /// added without every capture site failing to compile. <see cref="Deconstruct"/>
    /// extends that to the write direction: a serializer that destructures this state
    /// fails on arity when a member is added, rather than silently omitting it. Adding a
    /// field here is intended to break the build in all four places at once.
    /// </remarks>
    public sealed class ActiveEffectLifecycleRestoreState
    {
        public ActiveEffectLifecycleRestoreState(
            IEnumerable<ActiveEffectInstance> effects,
            IEnumerable<ContinuousRuleSource> continuousSources)
        {
            Effects = Array.AsReadOnly(
                (effects ?? throw new ArgumentNullException(nameof(effects))).ToArray());
            ContinuousSources = Array.AsReadOnly(
                (continuousSources ?? throw new ArgumentNullException(nameof(continuousSources))).ToArray());
            if (Effects.Select(effect => effect.Id).Distinct().Count() != Effects.Count ||
                ContinuousSources.Select(source => source.SourceId)
                    .Distinct(StringComparer.Ordinal).Count() != ContinuousSources.Count)
            {
                throw new ArgumentException("Restored timing identities must be unique.");
            }
        }

        public IReadOnlyList<ActiveEffectInstance> Effects { get; }
        public IReadOnlyList<ContinuousRuleSource> ContinuousSources { get; }

        public void Deconstruct(
            out IReadOnlyList<ActiveEffectInstance> effects,
            out IReadOnlyList<ContinuousRuleSource> continuousSources)
        {
            effects = Effects;
            continuousSources = ContinuousSources;
        }
    }
}
