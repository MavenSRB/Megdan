using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Presentation-only state for one possible card drag. It knows pointer distance and identity, but
    /// neither map geometry nor card-choice legality; those are rechecked by their existing owners.
    /// </summary>
    internal sealed class CardCommitmentDragState
    {
        private int pointerId = -1;
        private CardInstanceId cardInstanceId;
        private Vector2 start;

        public bool IsTracking => pointerId >= 0;
        public int PointerId => pointerId;
        public bool IsDragging { get; private set; }
        public CardInstanceId CardInstanceId => cardInstanceId;

        public bool Begin(int nextPointerId, CardInstanceId nextCardInstanceId, Vector2 position)
        {
            if (IsTracking || nextPointerId < 0 || !nextCardInstanceId.IsDefined)
            {
                return false;
            }

            pointerId = nextPointerId;
            cardInstanceId = nextCardInstanceId;
            start = position;
            IsDragging = false;
            return true;
        }

        public bool Move(int movedPointerId, Vector2 position, float threshold)
        {
            if (!IsTracking || movedPointerId != pointerId)
            {
                return false;
            }

            if (!IsDragging && (position - start).sqrMagnitude >= threshold * threshold)
            {
                IsDragging = true;
            }

            return IsDragging;
        }

        public bool Release(int releasedPointerId, out CardInstanceId releasedCard, out bool wasDrag)
        {
            releasedCard = default;
            wasDrag = false;
            if (!IsTracking || releasedPointerId != pointerId)
            {
                return false;
            }

            releasedCard = cardInstanceId;
            wasDrag = IsDragging;
            Reset();
            return true;
        }

        public void Cancel() => Reset();

        private void Reset()
        {
            pointerId = -1;
            cardInstanceId = default;
            start = default;
            IsDragging = false;
        }
    }
}
