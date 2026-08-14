using System;
using System.Collections.Generic;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    /// <summary>
    /// The outcome of committing a whole adjacent route, added by milestone 6.8c beside
    /// <see cref="AdjacentMoveResult"/> rather than replacing it.
    ///
    /// It carries one extra fact a single-space result has no use for: which space of the route was
    /// refused. A caller that walked the route itself learned that from how far it got; a caller that
    /// hands the whole route over cannot, and a refusal that does not say where is unactionable.
    /// </summary>
    public readonly struct AdjacentMoveRouteResult
    {
        private AdjacentMoveRouteResult(
            bool accepted,
            AdjacentMoveRejectionReason rejectionReason,
            int rejectedRouteIndex,
            IReadOnlyList<AdjacentMoveEvent> events)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            RejectedRouteIndex = rejectedRouteIndex;
            Events = events;
        }

        public bool Accepted { get; }

        public AdjacentMoveRejectionReason RejectionReason { get; }

        /// <summary>
        /// The zero-based position in the route that was refused, or zero when the route was accepted.
        /// </summary>
        public int RejectedRouteIndex { get; }

        /// <summary>One event per space entered, in order, or empty when the route was refused.</summary>
        public IReadOnlyList<AdjacentMoveEvent> Events { get; }

        public static AdjacentMoveRouteResult Accept(IReadOnlyList<AdjacentMoveEvent> events)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            if (events.Count == 0)
            {
                throw new ArgumentException(
                    "An accepted route entered at least one space.", nameof(events));
            }

            return new AdjacentMoveRouteResult(true, AdjacentMoveRejectionReason.None, 0, events);
        }

        public static AdjacentMoveRouteResult Reject(
            AdjacentMoveRejectionReason reason,
            int rejectedRouteIndex)
        {
            if (reason == AdjacentMoveRejectionReason.None)
            {
                throw new ArgumentException(
                    "A rejected route requires a rejection reason.", nameof(reason));
            }

            if (rejectedRouteIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rejectedRouteIndex));
            }

            return new AdjacentMoveRouteResult(
                false, reason, rejectedRouteIndex, Array.Empty<AdjacentMoveEvent>());
        }
    }
}
