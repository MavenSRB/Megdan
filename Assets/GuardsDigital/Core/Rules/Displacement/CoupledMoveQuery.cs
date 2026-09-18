using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Rules.Displacement
{
    /// <summary>One same-direction, same-distance pair. WI-Q1 permits following a vacated partner.</summary>
    public static class CoupledMoveQuery
    {
        public static bool CanMove(AdjacentMoveAuthority board, BoardObjectReference first,
            BoardObjectReference second, HexDirection direction, int distance,
            RuleModificationSet modifications, bool ignoresObstacles)
        {
            if (board == null || modifications == null || distance < 1 || first.Equals(second) ||
                !Enum.IsDefined(typeof(HexDirection), direction) ||
                !board.TryGetObjectCoordinate(first, out var firstAt) ||
                !board.TryGetObjectCoordinate(second, out var secondAt)) return false;
            var vacating = new[] { first, second };
            for (var step = 1; step <= distance; step++)
            {
                var a = StraightLineQuery.Step(firstAt, direction, step);
                var b = StraightLineQuery.Step(secondAt, direction, step);
                if (ObstacleQuery.BlocksTransitAfterVacating(board, a, first.AsRuleObject(), modifications,
                        ignoresObstacles, RuleActivity.DirectMove, vacating) ||
                    ObstacleQuery.BlocksTransitAfterVacating(board, b, second.AsRuleObject(), modifications,
                        ignoresObstacles, RuleActivity.DirectMove, vacating)) return false;
                if (step == distance && (ObstacleQuery.BlocksEndAfterVacating(board, a, modifications, vacating) ||
                    ObstacleQuery.BlocksEndAfterVacating(board, b, modifications, vacating))) return false;
            }
            return true;
        }
    }
}
