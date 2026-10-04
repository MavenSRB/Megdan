using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    public interface IUnitTurnHistory
    {
        bool TryGetTurnStartPosition(BoardObjectReference unit, out HexCoordinate coordinate);
        bool RemainedSincePreviousTurn(BoardObjectReference unit);
    }

    /// <summary>A unit's space and movement revision at a turn-start checkpoint.</summary>
    public readonly struct UnitTurnPosition
    {
        public UnitTurnPosition(BoardObjectReference unit, HexCoordinate coordinate, int mutation)
        {
            if (unit.Kind != BoardObjectKind.Hero && unit.Kind != BoardObjectKind.Minion)
                throw new ArgumentException("A turn position requires a unit.", nameof(unit));
            if (mutation < 0) throw new ArgumentOutOfRangeException(nameof(mutation));
            Unit = unit;
            Coordinate = coordinate;
            Mutation = mutation;
        }

        public BoardObjectReference Unit { get; }
        public HexCoordinate Coordinate { get; }
        public int Mutation { get; }
    }
}
