using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// The unlocked source watching an actual laugh leaf, while its controller remains on the board.
    /// The effect resolver reports the leaf; this query decides whether its Ultimate applies.
    /// </summary>
    public static class AfterLaughTriggerQuery
    {
        public static bool AnyWatching(UnlockedUltimateSet ultimates) =>
            ultimates != null && ultimates.Ultimates.Any(Watches);

        public static IReadOnlyList<UnlockedUltimate> Derive(
            UnlockedUltimateSet ultimates, AdjacentMoveAuthority board, HeroId actorId)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (!actorId.IsDefined) return Array.Empty<UnlockedUltimate>();
            var watching = ultimates.Ultimates.Where(ultimate =>
                Watches(ultimate) && ultimate.OwnerId.Equals(actorId) &&
                board.IsHeroOnBoard(actorId)).ToArray();
            if (watching.Length > 1)
                throw new InvalidOperationException(
                    "One hero cannot have two unlocked Ultimates watching one laugh.");
            return watching;
        }

        private static bool Watches(UnlockedUltimate ultimate) =>
            ultimate.Rule.TriggerCondition ==
                UltimateTriggerCondition.AfterControllerLaughsDiabolically &&
            ultimate.Rule.TriggerCheckpoints.Contains(TimingCheckpoint.LaughedDiabolically) &&
            ultimate.Rule.TriggeredEffect.HasValue;
    }
}
