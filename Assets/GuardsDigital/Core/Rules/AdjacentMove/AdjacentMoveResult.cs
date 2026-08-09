using System;
using System.Collections.Generic;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    public readonly struct AdjacentMoveResult
    {
        private AdjacentMoveResult(
            bool accepted,
            AdjacentMoveRejectionReason rejectionReason,
            IReadOnlyList<AdjacentMoveEvent> events)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Events = events;
        }

        public bool Accepted { get; }

        public AdjacentMoveRejectionReason RejectionReason { get; }

        public IReadOnlyList<AdjacentMoveEvent> Events { get; }

        public static AdjacentMoveResult Accept(AdjacentMoveEvent movementEvent)
        {
            return new AdjacentMoveResult(
                true,
                AdjacentMoveRejectionReason.None,
                new[] { movementEvent });
        }

        public static AdjacentMoveResult Reject(AdjacentMoveRejectionReason reason)
        {
            if (reason == AdjacentMoveRejectionReason.None)
            {
                throw new ArgumentException("A rejected adjacent move requires a rejection reason.", nameof(reason));
            }

            return new AdjacentMoveResult(false, reason, Array.Empty<AdjacentMoveEvent>());
        }
    }
}
