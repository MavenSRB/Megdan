using System;

namespace GuardsDigital.Core.Board
{
    public static class HexDistanceQuery
    {
        public static int Calculate(HexCoordinate first, HexCoordinate second)
        {
            var deltaQ = (long)first.Q - second.Q;
            var deltaR = (long)first.R - second.R;
            var deltaS = -deltaQ - deltaR;
            var distance = (Math.Abs(deltaQ) + Math.Abs(deltaR) + Math.Abs(deltaS)) / 2L;
            return checked((int)distance);
        }
    }
}
