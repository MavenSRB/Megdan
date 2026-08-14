using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Test infrastructure only. Pairs an <see cref="ActiveEffectDefinition"/> with the runtime bindings
    /// milestone 6.2a moved onto <see cref="ActiveEffectInstance"/> - controller hero, controller side,
    /// and affected hero.
    ///
    /// Fixtures written before that split described an effect as one object, and that description is
    /// still the natural thing for a test to state in one place. Keeping the pair here rather than
    /// widening the production API means the split is not softened for testing: production callers must
    /// still supply the bindings explicitly at every creation site.
    /// </summary>
    internal readonly struct EffectSpec
    {
        public EffectSpec(
            ActiveEffectDefinition definition,
            HeroId controllerHeroId,
            TeamSide controllerTeamSide,
            HeroId affectedHeroId)
        {
            Definition = definition;
            ControllerHeroId = controllerHeroId;
            ControllerTeamSide = controllerTeamSide;
            AffectedHeroId = affectedHeroId;
        }

        public ActiveEffectDefinition Definition { get; }
        public HeroId ControllerHeroId { get; }
        public TeamSide ControllerTeamSide { get; }
        public HeroId AffectedHeroId { get; }
    }

    internal static class EffectSpecLifecycleExtensions
    {
        public static ActiveEffectLifecycleEvent Create(
            this ActiveEffectLifecycle lifecycle,
            ActiveEffectId id,
            EffectSpec spec,
            CardInstanceId sourceCardId,
            ActionDefinitionId sourceActionId,
            int roundIndex,
            int turnIndex) =>
            lifecycle.Create(
                id,
                spec.Definition,
                spec.ControllerHeroId,
                spec.ControllerTeamSide,
                spec.AffectedHeroId,
                sourceCardId,
                sourceActionId,
                roundIndex,
                turnIndex);
    }
}
