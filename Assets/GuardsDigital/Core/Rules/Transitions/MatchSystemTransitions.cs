using System.Collections.Generic;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Progression;
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
    /// that contract's first production consumer, and milestone 7.5 adds p.15's Leveling Up, which is
    /// the second one 6.10a named.
    ///
    /// Both production rules need a running match to be about, so they are supplied per match rather
    /// than registered statically - which is also the honest difference between them and the synthetic
    /// entry: that one has no rules of its own, and the declared fallback is exactly right for it.
    /// </summary>
    public static class MatchSystemTransitions
    {
        public static SystemTransitionCatalog CreateCatalog() =>
            new SystemTransitionCatalog(new[]
            {
                SyntheticSystemTransitions.BoundedFourStep,
                LanePushSystemTransition.Definition,
                HeroLevelUpSystemTransition.Definition
            });

        /// <summary>
        /// The rules of the transitions this build can actually run. A <c>null</c> collaborator is a
        /// build that has no such transition to run - a board-free fixture has no Lane to push and no
        /// coin ledger to spend from - and its definition then falls through to the declared rules,
        /// which refuse a derived step rather than guessing at one.
        /// </summary>
        public static ISystemTransitionStepRules CreateRules(
            LanePushTransitionAuthority lanePush,
            HeroLevelUpStepRules levelUp)
        {
            var rules = new List<KeyValuePair<SystemTransitionDefinitionId, ISystemTransitionStepRules>>();
            if (lanePush != null)
            {
                rules.Add(new KeyValuePair<SystemTransitionDefinitionId, ISystemTransitionStepRules>(
                    LanePushSystemTransition.DefinitionId, new LanePushStepRules(lanePush)));
            }

            if (levelUp != null)
            {
                rules.Add(new KeyValuePair<SystemTransitionDefinitionId, ISystemTransitionStepRules>(
                    HeroLevelUpSystemTransition.DefinitionId, levelUp));
            }

            return new SystemTransitionStepRuleSet(rules);
        }
    }
}
