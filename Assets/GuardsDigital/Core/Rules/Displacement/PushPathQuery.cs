using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Rules.Displacement
{
    /// <summary>Why a push stopped where it did. Every arm is a legal outcome except the two refusals.</summary>
    public enum PushStoppingReason
    {
        None = 0,

        /// <summary>The target reached the full requested distance.</summary>
        ReachedRequestedDistance = 1,

        /// <summary>
        /// An obstacle the target may not pass stopped it short, or every space beyond the one it could
        /// pass was itself an illegal endpoint. p.18: "move it as far as possible; this can result in
        /// pushing it 0 spaces", so a zero-space result carries this reason and is still applied.
        /// </summary>
        BlockedByObstacle = 2,

        /// <summary>The straight line leaves the map before the requested distance is reached.</summary>
        LeftBoard = 3,

        /// <summary>
        /// Refusal, not a short push: the pusher and the target share no file, so p.18's "directly away
        /// from the pushing unit" names no direction. Distinct from a zero-space push, which has a
        /// direction and simply cannot use it.
        /// </summary>
        NotInStraightLine = 4,

        /// <summary>Refusal: the pusher and the target are the same space, so there is no direction.</summary>
        NoDirection = 5
    }

    public readonly struct PushPathResult
    {
        private PushPathResult(
            bool hasDirection,
            HexCoordinate origin,
            HexCoordinate endpoint,
            int requestedDistance,
            int actualDistance,
            PushStoppingReason stoppingReason)
        {
            HasDirection = hasDirection;
            Origin = origin;
            Endpoint = endpoint;
            RequestedDistance = requestedDistance;
            ActualDistance = actualDistance;
            StoppingReason = stoppingReason;
        }

        /// <summary>Whether p.18 could name a direction at all. A false value is the only refusal.</summary>
        public bool HasDirection { get; }

        public HexCoordinate Origin { get; }

        /// <summary>Where the target ends up. Equal to <see cref="Origin"/> for a zero-space push.</summary>
        public HexCoordinate Endpoint { get; }

        public int RequestedDistance { get; }

        public int ActualDistance { get; }

        public PushStoppingReason StoppingReason { get; }

        /// <summary>Whether the push actually relocates anything. A zero-space push is applied, not failed.</summary>
        public bool Moves => HasDirection && ActualDistance > 0;

        /// <summary>
        /// Whether card text that says "stopped by an obstacle" is talking about this outcome -
        /// milestone 8.8, and the resolution of ledger question W-04.
        ///
        /// <para>Both short arms count, and the second one is the question. p.19's Terrain keyword makes
        /// the water surrounding the play area terrain, and p.18 lists terrain among the obstacles, so a
        /// unit shoved off the edge of the board is stopped by the water and the card's sentence applies.
        /// The map model does not represent that water: <see cref="AdjacentMoveAuthority.IsPlayable"/>
        /// and <see cref="AdjacentMoveAuthority.IsTerrain"/> both answer no for an off-map coordinate,
        /// and milestone 6.8e made that a deliberate third answer - off the map is nothing, and no
        /// Move-Through permission crosses it, which is exactly what a modelled terrain space would
        /// wrongly allow. The two readings are reconciled here rather than in either of them: the
        /// classification keeps its three answers, because the permission needs them, and the card's
        /// sentence reads the two that mean "it ran into something" as one.</para>
        ///
        /// <para><see cref="PushStoppingReason.ReachedRequestedDistance"/> is not one of them, and neither
        /// refusal is: nothing stopped a push that travelled its whole distance, and a push with no
        /// direction never happened. A zero-space push blocked at the first space is stopped by an
        /// obstacle, because p.18 calls that a push.</para>
        /// </summary>
        public bool StoppedByAnObstacle =>
            HasDirection &&
            (StoppingReason == PushStoppingReason.BlockedByObstacle ||
                StoppingReason == PushStoppingReason.LeftBoard);

        internal static PushPathResult Refuse(HexCoordinate origin, int requestedDistance, PushStoppingReason reason) =>
            new PushPathResult(false, origin, origin, requestedDistance, 0, reason);

        internal static PushPathResult Resolve(
            HexCoordinate origin,
            HexCoordinate endpoint,
            int requestedDistance,
            int actualDistance,
            PushStoppingReason reason) =>
            new PushPathResult(true, origin, endpoint, requestedDistance, actualDistance, reason);
    }

    /// <summary>
    /// Rulebook p.18's Push geometry: "When a unit or a token is pushed, move it in a straight line
    /// directly away from the pushing unit the number of spaces equal to the push distance."
    ///
    /// Two further sentences of that entry are the whole of the walk below. "If the push is 'ignoring
    /// obstacles', or the obstacle can be 'moved through', the pushed target is moved through obstacles
    /// as long as it can reach an empty space" - so transit and endpoint stay the two separate p.18
    /// questions milestone 6.8c already split, and a permitted target walks past obstacles but still has
    /// to land on an empty space. "If the target cannot be moved the full distance, move it as far as
    /// possible; this can result in pushing it 0 spaces" - so a blocked push is a short push, never a
    /// failure, and this query returns the farthest legal endpoint at or before the requested distance.
    ///
    /// It composes <see cref="ObstacleQuery"/> rather than reading occupancy itself, which is milestone
    /// 6.4's fourth acceptance check: ordinary obstacles, Ignore Obstacles, Move Through, terrain, and
    /// legal endpoints are one shared contract, not per-effect geometry.
    /// </summary>
    public static class PushPathQuery
    {
        /// <summary>
        /// Resolves where one pushed object ends up.
        /// </summary>
        /// <param name="target">
        /// The pushed object as a rules object, or <see cref="RuleObject.None"/> for a token. p.19 states
        /// that tokens are not units, and <see cref="RuleObjectKind"/> deliberately has no token member,
        /// so a pushed token holds no Move-Through permission of its own and reaches the walk with
        /// <paramref name="pushIgnoresObstacles"/> as its only route through an obstacle - which is
        /// exactly p.18's "if the push is 'ignoring obstacles'" arm.
        /// </param>
        /// <param name="pushIgnoresObstacles">
        /// Whether the pushing effect itself ignores obstacles. Required rather than defaulted, per the
        /// milestone 6.8f/6.8i regime: a caller states which kind of push this is instead of inheriting
        /// one. It is OR-ed with the target's own permission because p.18 lists the two as alternatives
        /// in one sentence.
        /// </param>
        public static PushPathResult Resolve(
            AdjacentMoveAuthority board,
            HexCoordinate pusher,
            HexCoordinate targetCoordinate,
            int requestedDistance,
            RuleObject target,
            bool pushIgnoresObstacles,
            RuleModificationSet modifications)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            if (requestedDistance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requestedDistance), requestedDistance, "A push distance must not be negative.");
            }

            if (pusher.Equals(targetCoordinate))
            {
                return PushPathResult.Refuse(targetCoordinate, requestedDistance, PushStoppingReason.NoDirection);
            }

            if (!StraightLineQuery.TryGetDirection(pusher, targetCoordinate, out var direction, out _))
            {
                return PushPathResult.Refuse(
                    targetCoordinate, requestedDistance, PushStoppingReason.NotInStraightLine);
            }

            var endpoint = targetCoordinate;
            var actualDistance = 0;
            var stoppingReason = PushStoppingReason.ReachedRequestedDistance;
            var current = targetCoordinate;
            for (var step = 1; step <= requestedDistance; step++)
            {
                current = current.GetNeighbor(direction);

                // Off the map is not an obstacle to ignore - it is nothing, and no permission answers
                // it - so it ends the walk on its own reason rather than looking like a blocked space.
                if (!board.IsPlayable(current) && !board.IsTerrain(current))
                {
                    stoppingReason = PushStoppingReason.LeftBoard;
                    break;
                }

                // Milestone 8.61d. The transit question is asked of the one query that owns it rather
                // than of the classification plus a locally composed permission, which is what this
                // line used to do. The two were equal for every push in the roster - the walk has
                // already answered off-map above, and BlocksTransit composes exactly the same
                // "this push ignores obstacles OR the target was granted Move Through" - and they stop
                // being equal for p.18's third source, an obstacle whose own text says units may move
                // through it. Composing it here would have given that sentence a second reader that
                // could disagree with the movement walk's.
                if (ObstacleQuery.BlocksTransit(
                    board, current, target, modifications, pushIgnoresObstacles))
                {
                    stoppingReason = PushStoppingReason.BlockedByObstacle;
                    break;
                }

                // Passed through, but only an empty space may be kept. A permitted target that ends the
                // walk standing on an obstacle keeps the last empty space it crossed, which is what
                // "as long as it can reach an empty space" means when the full distance lands badly.
                if (!ObstacleQuery.BlocksEndOfMovement(board, current, modifications))
                {
                    endpoint = current;
                    actualDistance = step;
                }
            }

            if (actualDistance < requestedDistance && stoppingReason == PushStoppingReason.ReachedRequestedDistance)
            {
                // The walk ran its full length without leaving the map or being refused entry, yet the
                // last legal endpoint is short of it: every remaining space was an obstacle crossed
                // under the permission.
                stoppingReason = PushStoppingReason.BlockedByObstacle;
            }

            return PushPathResult.Resolve(
                targetCoordinate, endpoint, requestedDistance, actualDistance, stoppingReason);
        }
    }
}
