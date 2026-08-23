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
            if (!basicAction.IsDefined || basicAction.Kind != ActionKind.Skill)
                return Array.Empty<ContinuousTriggeredEffectSource>();

            return ultimates.Ultimates
                .Where(ultimate =>
                    ultimate.Rule.TriggerCondition ==
                        UltimateTriggerCondition.AfterControllerPerformsBasicSkill &&
                    ultimate.OwnerId.Equals(basicAction.ActorId) &&
                    board.TryGetHeroCoordinate(ultimate.OwnerId, out _))
                .Select((ultimate, index) => ultimate.TriggeredSource(participants, index))
                .Where(source => source != null)
                .ToArray();
        }
    }
}
