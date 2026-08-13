using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    internal enum BoardOccupantKind
    {
        Hero = 1,
        GenericObstacle = 2,
        StandardMinion = 3
    }

    internal readonly struct BoardOccupant
    {
        private BoardOccupant(BoardOccupantKind kind, HeroId heroId, ObstacleId obstacleId, MinionId minionId)
        {
            Kind = kind;
            HeroId = heroId;
            ObstacleId = obstacleId;
            MinionId = minionId;
        }

        internal BoardOccupantKind Kind { get; }
        internal HeroId HeroId { get; }
        internal ObstacleId ObstacleId { get; }
        internal MinionId MinionId { get; }

        internal static BoardOccupant ForHero(HeroId heroId) =>
            new BoardOccupant(BoardOccupantKind.Hero, heroId, default, default);

        internal static BoardOccupant ForObstacle(ObstacleId obstacleId) =>
            new BoardOccupant(BoardOccupantKind.GenericObstacle, default, obstacleId, default);

        internal static BoardOccupant ForMinion(MinionId minionId) =>
            new BoardOccupant(BoardOccupantKind.StandardMinion, default, default, minionId);
    }

    // One authoritative per-space occupancy record for every occupant kind. The single Add owns
    // the one-obstacle-per-space rule, so a new occupant kind joins the board by declaring a
    // BoardOccupant instead of adding another hand-written per-kind cross-collision block and
    // per-kind commit scan.
    internal sealed class BoardOccupancyIndex
    {
        private readonly Dictionary<HexCoordinate, BoardOccupant> occupants;

        internal BoardOccupancyIndex()
        {
            occupants = new Dictionary<HexCoordinate, BoardOccupant>();
        }

        private BoardOccupancyIndex(Dictionary<HexCoordinate, BoardOccupant> occupants)
        {
            this.occupants = occupants;
        }

        internal IReadOnlyCollection<HexCoordinate> OccupiedCoordinates => occupants.Keys;

        internal bool IsOccupied(HexCoordinate coordinate) => occupants.ContainsKey(coordinate);

        internal void Add(HexCoordinate coordinate, BoardOccupant occupant, string parameterName)
        {
            if (occupants.ContainsKey(coordinate))
            {
                throw new ArgumentException(
                    "Board occupants must not share a space: one obstacle or unit per space.",
                    parameterName);
            }

            occupants.Add(coordinate, occupant);
        }

        internal void Move(HexCoordinate origin, HexCoordinate destination)
        {
            if (!occupants.TryGetValue(origin, out var occupant))
            {
                throw new InvalidOperationException("No board occupant exists at the move origin.");
            }

            if (occupants.ContainsKey(destination))
            {
                throw new InvalidOperationException("The move destination is already occupied.");
            }

            occupants.Remove(origin);
            occupants.Add(destination, occupant);
        }

        internal void Remove(HexCoordinate coordinate)
        {
            if (!occupants.Remove(coordinate))
            {
                throw new InvalidOperationException("No board occupant exists at the removal coordinate.");
            }
        }

        internal BoardOccupancyIndex Clone() =>
            new BoardOccupancyIndex(new Dictionary<HexCoordinate, BoardOccupant>(occupants));
    }
}
