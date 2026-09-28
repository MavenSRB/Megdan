using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Movement
{
    /// <summary>
    /// Milestone 8.114b1. "If you move in a straight line: You may ignore obstacles" - Wuk's Trample
    /// and Angry Stampede, read as a property of the Movement action's own walk (WU-Q5 (A), user,
    /// 2026-09-28).
    ///
    /// <para><b>What it permits.</b> A hero holding
    /// <see cref="RulePermissionKind.MoveThroughObstaclesInAStraightLine"/> for Movement may pass
    /// through obstacles (p.18) only while the whole walk of the action - every space from where it
    /// started - is one straight line (p.19). The walk still never ends on an obstacle. A walk that
    /// bends is an ordinary walk: nothing is crossed and nothing is ignored.</para>
    ///
    /// <para><b>Several legs.</b> A Movement action may commit its route in more than one leg. While
    /// the walk so far is straight, the next leg may continue it along its file (or, from the start,
    /// along any file) through obstacles. Once it HAS crossed an obstacle, bending later would make
    /// the crossing one the card never allowed, so only the continuation is offered from then on.</para>
    ///
    /// <para><b>Which route a destination takes.</b> A space on the walk's file is reached by the
    /// straight route, which is also a shortest one (p.19's distance along a file is the hex
    /// distance), so it costs the same allowance whichever way it is walked.</para>
    /// </summary>
    public static class StraightLineCrossingQuery
    {
        private static readonly HexDirection[] Directions =
            (HexDirection[])Enum.GetValues(typeof(HexDirection));

        public static bool Permits(HeroId hero, RuleModificationSet modifications) =>
            hero.IsDefined && modifications != null && modifications.HasPermission(
                RuleObject.ForHero(hero), RulePermissionKind.MoveThroughObstaclesInAStraightLine,
                RuleActivity.Movement);

        /// <summary>Whether the walk so far followed by <paramref name="route"/> is one straight line.</summary>
        public static bool IsStraightWalk(IReadOnlyList<HexCoordinate> walked, IReadOnlyList<HexCoordinate> route)
        {
            if (walked == null) throw new ArgumentNullException(nameof(walked));
            if (route == null) throw new ArgumentNullException(nameof(route));
            return StraightLineQuery.IsStraightLine(walked.Concat(route.Skip(1)).ToArray());
        }

        /// <summary>
        /// The destinations a Movement action offers from where its walk now stands, given the
        /// ordinary ones: unchanged without the permission or once the walk has bent; otherwise every
        /// endpoint along the walk's file (or any file, from the start) up to the allowance, through
        /// obstacles, in place of or beside the ordinary offers - and ONLY those once an obstacle has
        /// been crossed.
        /// </summary>
        public static IReadOnlyList<ReachableMovementDestination> Widen(
            IReadOnlyList<ReachableMovementDestination> ordinary,
            IReadOnlyList<HexCoordinate> walked,
            HeroId hero,
            AdjacentMoveAuthority board,
            int allowance,
            RuleModificationSet modifications)
        {
            if (ordinary == null) throw new ArgumentNullException(nameof(ordinary));
            if (walked == null || walked.Count == 0) throw new ArgumentException("A walk starts somewhere.", nameof(walked));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (allowance <= 0 || !Permits(hero, modifications) || !StraightLineQuery.IsStraightLine(walked))
            {
                return ordinary;
            }

            var current = walked[walked.Count - 1];
            var files = walked.Count == 1
                ? Directions
                : StraightLineQuery.TryGetDirection(walked[0], current, out var file, out _)
                    ? new[] { file }
                    : Array.Empty<HexDirection>();
            var onMap = new HashSet<HexCoordinate>(board.PlayableCoordinateSet);
            onMap.UnionWith(board.TerrainCoordinateSet);

            var lines = new List<ReachableMovementDestination>();
            foreach (var direction in files)
            {
                var route = new List<HexCoordinate> { current };
                for (var step = 1; step <= allowance; step++)
                {
                    var space = StraightLineQuery.Step(current, direction, step);
                    if (!onMap.Contains(space))
                    {
                        break;
                    }

                    route.Add(space);
                    if (!ObstacleQuery.BlocksEndOfMovement(board, space, modifications))
                    {
                        lines.Add(new ReachableMovementDestination(space, route.ToArray(), step));
                    }
                }
            }

            var crossedSoFar = walked.Skip(1).Take(walked.Count - 2)
                .Any(space => ObstacleQuery.BlocksEndOfMovement(board, space, modifications));
            if (crossedSoFar)
            {
                return lines.AsReadOnly();
            }

            var byDestination = lines.ToDictionary(line => line.Destination);
            var widened = ordinary
                .Select(entry => byDestination.TryGetValue(entry.Destination, out var line) ? line : entry)
                .ToList();
            widened.AddRange(lines.Where(line => ordinary.All(entry => !entry.Destination.Equals(line.Destination))));
            return widened.AsReadOnly();
        }
    }
}
