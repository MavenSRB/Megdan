using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.AdjacentMove
{
    internal enum BoardOccupantKind
    {
        Hero = 1,
        GenericObstacle = 2,
        StandardMinion = 3,

        /// <summary>
        /// Milestone 6.2. Rulebook p.19 classes tokens as obstacles and p.18 admits no second obstacle
        /// in a space "ever, no exceptions", so a token is an occupant like any other. It joined by
        /// declaring this member and a factory below; no per-kind collision block or commit scan was
        /// added anywhere, which is the prediction milestone 6.1r's index was built to satisfy.
        /// </summary>
        Token = 4
    }

    internal readonly struct BoardOccupant
    {
        private BoardOccupant(
            BoardOccupantKind kind,
            HeroId heroId,
            ObstacleId obstacleId,
            MinionId minionId,
            TokenInstanceId tokenInstanceId)
        {
            Kind = kind;
            HeroId = heroId;
            ObstacleId = obstacleId;
            MinionId = minionId;
            TokenInstanceId = tokenInstanceId;
        }

        internal BoardOccupantKind Kind { get; }
        internal HeroId HeroId { get; }
        internal ObstacleId ObstacleId { get; }
        internal MinionId MinionId { get; }
        internal TokenInstanceId TokenInstanceId { get; }

        internal static BoardOccupant ForHero(HeroId heroId) =>
            new BoardOccupant(BoardOccupantKind.Hero, heroId, default, default, default);

        internal static BoardOccupant ForObstacle(ObstacleId obstacleId) =>
            new BoardOccupant(BoardOccupantKind.GenericObstacle, default, obstacleId, default, default);

        internal static BoardOccupant ForMinion(MinionId minionId) =>
            new BoardOccupant(BoardOccupantKind.StandardMinion, default, default, minionId, default);

        internal static BoardOccupant ForToken(TokenInstanceId tokenInstanceId) =>
            new BoardOccupant(BoardOccupantKind.Token, default, default, default, tokenInstanceId);
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

        // Which occupant, not merely whether there is one. Milestone 6.4's displacement port names a
        // push target by the space it stands on, which the one-obstacle-per-space rule above makes an
        // unambiguous identity for every kind at once; this is where that space becomes the identity.
        internal bool TryGetOccupant(HexCoordinate coordinate, out BoardOccupant occupant) =>
            occupants.TryGetValue(coordinate, out occupant);

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

        // Milestone 6.6's Swap (p.19): "Two objects on the board swap places with each other." Written
        // as two assignments rather than a remove-and-add pair, so the one-obstacle-per-space rule this
        // index owns holds at every instant - a pair would leave one space momentarily empty and could
        // lose an occupant entirely if the second half threw.
        internal void Exchange(HexCoordinate first, HexCoordinate second)
        {
            if (!occupants.TryGetValue(first, out var firstOccupant) ||
                !occupants.TryGetValue(second, out var secondOccupant))
            {
                throw new InvalidOperationException("A board exchange requires an occupant in both spaces.");
            }

            occupants[first] = secondOccupant;
            occupants[second] = firstOccupant;
        }

        // Milestone 6.6's Replace (p.19): one object stands where another stood. The space stays
        // occupied throughout, which is what separates a substitution from a removal and a placement.
        internal void Substitute(HexCoordinate coordinate, BoardOccupant occupant)
        {
            if (!occupants.ContainsKey(coordinate))
            {
                throw new InvalidOperationException("No board occupant exists at the substitution coordinate.");
            }

            occupants[coordinate] = occupant;
        }

        internal BoardOccupancyIndex Clone() =>
            new BoardOccupancyIndex(new Dictionary<HexCoordinate, BoardOccupant>(occupants));
    }
}
