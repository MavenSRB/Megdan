using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Rules.Progression
{
    public static class ResolvedCardDiscardTriggerQuery
    {
        public static bool AnyWatching(UnlockedUltimateSet ultimates) =>
            ultimates != null && ultimates.Ultimates.Any(Watches);

        public static UnlockedUltimate Derive(UnlockedUltimateSet ultimates,
            AdjacentMoveAuthority board, CardInstanceId discardedCardId,
            IEnumerable<CardInstance> instances)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            if (board == null) throw new ArgumentNullException(nameof(board));
            var card = instances.SingleOrDefault(value => value.Id.Equals(discardedCardId));
            if (card == null || !CardLifecycleAuthority.IsDiscardedRestingCard(card) ||
                !card.State.HasBeenPlayed || !board.TryGetHeroCoordinate(card.OwnerId, out _))
                return null;
            // Rulebook p.16 gives each hero exactly one Ultimate; do not silently choose one.
            return ultimates.Ultimates.SingleOrDefault(value =>
                Watches(value) && value.OwnerId.Equals(card.OwnerId));
        }

        private static bool Watches(UnlockedUltimate value) =>
            value.Rule.TriggerCondition == UltimateTriggerCondition.AfterOwnedResolvedCardDiscarded &&
            value.Rule.TriggerCheckpoints.Contains(TimingCheckpoint.ResolvedCardDiscarded) &&
            value.Rule.TriggeredEffect.HasValue;
    }
}
