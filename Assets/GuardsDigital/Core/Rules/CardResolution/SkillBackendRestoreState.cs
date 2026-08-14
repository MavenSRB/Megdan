using System;
using GuardsDigital.Core.Rules.EffectResolution;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// The complete persisted state of the Skill backend.
    ///
    /// Deliberately one member with a <c>Deconstruct</c>, following the same compile-enforced discipline
    /// as the movement and attack backends: adding a persisted field changes this type's arity, so
    /// capture, write, read, and restore stop compiling together rather than one of them silently
    /// omitting the new field.
    /// </summary>
    public sealed class SkillBackendRestoreState
    {
        public SkillBackendRestoreState(PendingResolution pendingResolution)
        {
            PendingResolution = pendingResolution ?? throw new ArgumentNullException(nameof(pendingResolution));
        }

        public PendingResolution PendingResolution { get; }

        public void Deconstruct(out PendingResolution pendingResolution)
        {
            pendingResolution = PendingResolution;
        }
    }
}
