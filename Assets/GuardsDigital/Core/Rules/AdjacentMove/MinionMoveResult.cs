using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    public enum MinionMoveRejectionReason
    {
        None = 0,
        UnknownMinion = 1,
        DestinationOffBoard = 2,
        DestinationOccupied = 3
    }

    public readonly struct MinionMoveResult
    {
        private MinionMoveResult(bool accepted, MinionMoveRejectionReason rejectionReason, MinionOccupancy movedMinion, HexCoordinate origin)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            MovedMinion = movedMinion;
            Origin = origin;
        }

        public bool Accepted { get; }
        public MinionMoveRejectionReason RejectionReason { get; }
        public MinionOccupancy MovedMinion { get; }
        public HexCoordinate Origin { get; }

        public static MinionMoveResult Accept(MinionOccupancy movedMinion, HexCoordinate origin)
        {
            if (movedMinion == null) throw new ArgumentNullException(nameof(movedMinion));
            return new MinionMoveResult(true, MinionMoveRejectionReason.None, movedMinion, origin);
        }

        public static MinionMoveResult Reject(MinionMoveRejectionReason reason)
        {
            if (reason == MinionMoveRejectionReason.None)
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new MinionMoveResult(false, reason, null, default);
        }
    }
}
