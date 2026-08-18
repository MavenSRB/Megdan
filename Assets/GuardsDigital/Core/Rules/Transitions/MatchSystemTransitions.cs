using System.Collections.Generic;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Transitions.Synthetic;

namespace GuardsDigital.Core.Rules.Transitions
{
    /// <summary>
    /// The multi-step system transitions this build supplies, and the rules that run them.
    ///
    /// One place says both, because they are one composition decision: a catalog entry with no rules
    /// is a transition that would refuse itself the moment a derived step was reached, and rules with
    /// no catalog entry could never be asked anything. Milestone 6.10a registered the bounded synthetic
    /// transition its contract was proved on; milestone 6.11 adds rulebook p.8's Lane Push, which is
    /// that contract's first production consumer.
    ///
    /// The Lane-Push rules need a running match to be about, so they are supplied per match rather than
    /// registered statically - which is also the honest difference between the two entries: the
    /// synthetic one has no rules of its own, and the declared fallback is exactly right for it.
    /// </summary>
    public static class MatchSystemTransitions
    {
        public static SystemTransitionCatalog CreateCatalog() =>
            new SystemTransitionCatalog(new[]
            {
                SyntheticSystemTransitions.BoundedFourStep,
                LanePushSystemTransition.Definition
            });

        public static ISystemTransitionStepRules CreateRules(LanePushTransitionAuthority lanePush) =>
            new SystemTransitionStepRuleSet(
                new[]
                {
                    new KeyValuePair<SystemTransitionDefinitionId, ISystemTransitionStepRules>(
                        LanePushSystemTransition.DefinitionId, new LanePushStepRules(lanePush))
                });
    }
}
