using System.Collections.Generic;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Rules.Transitions.Synthetic
{
    /// <summary>
    /// The bounded synthetic multi-step transition that is this contract's first consumer (milestone
    /// 6.10a's fifth acceptance check), in the shape the synthetic effect capabilities and the synthetic
    /// card-retrieval handler already take: production content that exists so a fixture can exercise a
    /// contract before the rule that will use it is written.
    ///
    /// It is bounded in the literal sense - four declared steps, no recursion, no runtime-derived
    /// length - because an unbounded one would prove nothing extra about a continuation and would need a
    /// termination argument of its own.
    ///
    /// The shape is chosen to make the second acceptance check answerable rather than to look like
    /// anything. Step one resolves itself, so a transition can be begun and already be past a step
    /// nobody answered; steps two and three pause on different sides, so a chooser is genuinely
    /// recorded rather than assumed; and step four resolves itself, so the transition finishes on its
    /// own after the last answer rather than needing one more command to notice it is done.
    /// </summary>
    public static class SyntheticSystemTransitions
    {
        public static SystemTransitionDefinitionId BoundedFourStepId { get; } =
            new SystemTransitionDefinitionId("synthetic-bounded-transition");

        public static SystemTransitionStepId OpeningStepId { get; } =
            new SystemTransitionStepId("synthetic-step-opening");

        public static SystemTransitionStepId RedChoiceStepId { get; } =
            new SystemTransitionStepId("synthetic-step-red-choice");

        public static SystemTransitionStepId BlueChoiceStepId { get; } =
            new SystemTransitionStepId("synthetic-step-blue-choice");

        public static SystemTransitionStepId ClosingStepId { get; } =
            new SystemTransitionStepId("synthetic-step-closing");

        public static SystemTransitionOptionId FirstOptionId { get; } =
            new SystemTransitionOptionId("first");

        public static SystemTransitionOptionId SecondOptionId { get; } =
            new SystemTransitionOptionId("second");

        public static SystemTransitionDefinition BoundedFourStep { get; } =
            new SystemTransitionDefinition(
                BoundedFourStepId,
                "Synthetic Bounded Transition",
                new List<SystemTransitionStepDefinition>
                {
                    new SystemTransitionStepDefinition(OpeningStepId),
                    new SystemTransitionStepDefinition(
                        RedChoiceStepId,
                        TeamSide.Red,
                        new[] { FirstOptionId, SecondOptionId }),
                    new SystemTransitionStepDefinition(
                        BlueChoiceStepId,
                        TeamSide.Blue,
                        new[] { FirstOptionId, SecondOptionId }),
                    new SystemTransitionStepDefinition(ClosingStepId)
                });

        public static SystemTransitionCatalog CreateCatalog() =>
            new SystemTransitionCatalog(new[] { BoundedFourStep });
    }
}
