using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// Derives the permanent triggered sources applicable to one completed basic action. The source
    /// remains card-defined; this query owns p.16's shared on-board lifetime and the trigger fact's
    /// actor/kind match.
    /// </summary>
    public static class UltimateTriggeredEffectSourceQuery
    {
        public static IReadOnlyList<ContinuousTriggeredEffectSource> Derive(
            UnlockedUltimateSet ultimates,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            PerformedBasicAction basicAction)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (!basicAction.IsDefined)
                return Array.Empty<ContinuousTriggeredEffectSource>();

            return ultimates.Ultimates
                .Where(ultimate =>
                    Matches(ultimate.Rule.TriggerCondition, basicAction) &&
                    ultimate.OwnerId.Equals(basicAction.ActorId) &&
                    board.TryGetHeroCoordinate(ultimate.OwnerId, out _))
                .Select((ultimate, index) => ultimate.TriggeredSource(participants, index, basicAction))
                .Where(source => source != null)
                .ToArray();
        }

        /// <summary>
        /// Whether one declared condition is met by the kind of basic action that just completed.
        ///
        /// An explicit switch rather than a comparison, for the reason
        /// <see cref="GuardsDigital.Core.Rules.Modifications.RuleActivityQuery.ForAction"/> is a switch:
        /// milestone 8.19c added a second condition beside the first, and the two are narrower and
        /// wider readings of the same page. A card that says "basic skill" must keep meaning that after
        /// a card that says "basic action" arrives, and the only way to guarantee it is to state both
        /// answers rather than to test one and let the other fall through.
        /// </summary>
        private static bool Matches(
            UltimateTriggerCondition condition, PerformedBasicAction performed)
        {
            switch (condition)
            {
                case UltimateTriggerCondition.AfterControllerPerformsBasicSkill:
                    return performed.Kind == ActionKind.Skill;
                case UltimateTriggerCondition.AfterControllerPerformsBasicAction:
                    return true;

                // Milestone 8.35z29. The one condition here that reads what the action did rather
                // than which kind it was: a basic Skill of Misa's that moved her five spaces is not a
                // basic Skill of Misa's that placed her, and only the second is what her Ultimate's
                // second paragraph watches.
                case UltimateTriggerCondition.AfterControllerPlacesSelf:
                    return performed.PlacedSelf;

                // Milestone 8.37. Tali's Reign of Winter: a second condition reading a
                // CONSEQUENCE of the completed action, exactly as AfterControllerPlacesSelf does - here
                // an Attack that actually removed a minion, not merely one that was performed.
                case UltimateTriggerCondition.AfterControllerDefeatsMinionWithBasicAttack:
                    return performed.Kind == ActionKind.Attack &&
                        performed.DefeatedMinionWithBasicAttack;
                default:
                    return false;
            }
        }
    }
}
