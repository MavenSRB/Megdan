using System.Collections.Generic;
using System.Linq;
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
        /// <summary>
        /// W-022. The catalog above plus the transitions a separate game mode declares; with no mode
        /// rules it is exactly the catalog above.
        /// </summary>
        public static SystemTransitionCatalog CreateCatalog(GuardsDigital.Core.Setup.MatchModeRules modeRules) =>
            modeRules == null
                ? CreateCatalog()
                : new SystemTransitionCatalog(new[]
                    {
                        SyntheticSystemTransitions.BoundedFourStep,
                        LanePushSystemTransition.Definition,
                        HeroLevelUpSystemTransition.Definition
                    }.Concat(modeRules.TransitionDefinitions));

        /// <summary>
        /// W-022. The rules above plus a mode's: each round-start placement runs on
        /// <see cref="ModeRoundStartPlacementRules"/> over the match's board, and each granted level on
        /// the same <paramref name="levelUp"/> rules a purchased level uses. With no mode rules it is
        /// exactly the rule set above.
        /// </summary>
        public static ISystemTransitionStepRules CreateRules(
            LanePushTransitionAuthority lanePush,
            HeroLevelUpStepRules levelUp,
            GuardsDigital.Core.Setup.MatchModeRules modeRules,
            GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority board,
            GuardsDigital.Core.Participants.MatchParticipants participants)
        {
            if (modeRules == null) return CreateRules(lanePush, levelUp);
            var rules = new List<KeyValuePair<SystemTransitionDefinitionId, ISystemTransitionStepRules>>(
                Named(lanePush, levelUp));
            if (board != null && participants != null)
            {
                var placement = new ModeRoundStartPlacementRules(modeRules, board, participants);
                rules.AddRange(modeRules.PlacementRounds.Select(round =>
                    new KeyValuePair<SystemTransitionDefinitionId, ISystemTransitionStepRules>(
                        GuardsDigital.Core.Setup.MatchModeRules.PlacementTransitionId(round), placement)));
            }

            if (levelUp != null)
            {
                rules.AddRange(modeRules.LevelGrantHeroes.Select(hero =>
                    new KeyValuePair<SystemTransitionDefinitionId, ISystemTransitionStepRules>(
                        GuardsDigital.Core.Setup.MatchModeRules.LevelGrantTransitionId(hero), levelUp)));
            }

            return new SystemTransitionStepRuleSet(rules);
        }

        public static ISystemTransitionStepRules CreateRules(
            LanePushTransitionAuthority lanePush,
            HeroLevelUpStepRules levelUp) =>
            new SystemTransitionStepRuleSet(Named(lanePush, levelUp));

        private static IEnumerable<KeyValuePair<SystemTransitionDefinitionId, ISystemTransitionStepRules>> Named(
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

            return rules;
        }
    }
}
