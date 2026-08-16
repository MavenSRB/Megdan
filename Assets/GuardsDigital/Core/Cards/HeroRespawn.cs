using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    public enum HeroRespawnCommandKind
    {
        None = 0,
        Respawn = 1,
        ResolveWithoutAction = 2
    }

    public sealed class HeroRespawnCommand : IEquatable<HeroRespawnCommand>
    {
        public HeroRespawnCommand(
            HeroRespawnCommandKind kind,
            HeroId heroId,
            CardInstanceId cardInstanceId,
            HexCoordinate destination = default)
        {
            if (kind != HeroRespawnCommandKind.Respawn &&
                kind != HeroRespawnCommandKind.ResolveWithoutAction)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (!heroId.IsDefined || !cardInstanceId.IsDefined)
            {
                throw new ArgumentException("A hero respawn command requires the selected hero and card.");
            }

            Kind = kind;
            HeroId = heroId;
            CardInstanceId = cardInstanceId;
            Destination = destination;
        }

        public HeroRespawnCommandKind Kind { get; }
        public HeroId HeroId { get; }
        public CardInstanceId CardInstanceId { get; }
        public HexCoordinate Destination { get; }

        public bool Equals(HeroRespawnCommand other) =>
            other != null && Kind == other.Kind && HeroId.Equals(other.HeroId) &&
            CardInstanceId.Equals(other.CardInstanceId) && Destination.Equals(other.Destination);

        public override bool Equals(object obj) => Equals(obj as HeroRespawnCommand);

        public override int GetHashCode() =>
            (((int)Kind * 397) ^ HeroId.GetHashCode()) * 397 ^
            CardInstanceId.GetHashCode() ^ Destination.GetHashCode();
    }

    public readonly struct HeroRespawnDestination : IEquatable<HeroRespawnDestination>
    {
        public HeroRespawnDestination(
            HexCoordinate coordinate,
            SpawnPointId adjacentSpawnPointId,
            bool isFallback)
        {
            Coordinate = coordinate;
            AdjacentSpawnPointId = adjacentSpawnPointId;
            IsFallback = isFallback;
        }

        public HexCoordinate Coordinate { get; }
        public SpawnPointId AdjacentSpawnPointId { get; }
        public bool IsFallback { get; }

        public bool Equals(HeroRespawnDestination other) =>
            Coordinate.Equals(other.Coordinate) &&
            AdjacentSpawnPointId.Equals(other.AdjacentSpawnPointId) &&
            IsFallback == other.IsFallback;

        public override bool Equals(object obj) =>
            obj is HeroRespawnDestination other && Equals(other);

        public override int GetHashCode() =>
            ((Coordinate.GetHashCode() * 397) ^ AdjacentSpawnPointId.GetHashCode()) * 397 ^
            (IsFallback ? 1 : 0);
    }

    public sealed class PendingHeroRespawnChoice
    {
        public PendingHeroRespawnChoice(
            HeroId heroId,
            CardInstanceId cardInstanceId,
            IEnumerable<HeroRespawnDestination> destinations)
        {
            HeroId = heroId;
            CardInstanceId = cardInstanceId;
            Destinations = Array.AsReadOnly((destinations ??
                throw new ArgumentNullException(nameof(destinations)))
                .OrderBy(value => value.Coordinate.Q)
                .ThenBy(value => value.Coordinate.R)
                .ToArray());
            RespawnInvocations = Array.AsReadOnly(Destinations.Select(value =>
                new HeroRespawnCommand(
                    HeroRespawnCommandKind.Respawn,
                    heroId,
                    cardInstanceId,
                    value.Coordinate)).ToArray());
            DeclineInvocation = new HeroRespawnCommand(
                HeroRespawnCommandKind.ResolveWithoutAction,
                heroId,
                cardInstanceId);
        }

        public HeroId HeroId { get; }
        public CardInstanceId CardInstanceId { get; }
        public IReadOnlyList<HeroRespawnDestination> Destinations { get; }
        public IReadOnlyList<HeroRespawnCommand> RespawnInvocations { get; }
        public HeroRespawnCommand DeclineInvocation { get; }
    }

    public static class HeroRespawnRules
    {
        public static PendingHeroRespawnChoice CreateChoice(
            HeroId heroId,
            CardInstanceId cardInstanceId,
            MatchParticipants participants,
            AdjacentMoveAuthority board)
        {
            if (board.TryGetHeroCoordinate(heroId, out _))
            {
                return null;
            }

            var side = participants.GetTeamForHero(heroId).Side;
            var mapTeam = side == TeamSide.Red ? "red" : "blue";
            var heroSpawns = board.MapDefinition.SpawnPoints
                .Where(spawn => string.Equals(spawn.TeamId, mapTeam, StringComparison.Ordinal) &&
                    string.Equals(spawn.UnitType, "hero", StringComparison.Ordinal) &&
                    board.MapDefinition.GetZoneForSpace(spawn.SpaceId).IsThrone &&
                    string.Equals(
                        board.MapDefinition.GetZoneForSpace(spawn.SpaceId).TeamId,
                        mapTeam,
                        StringComparison.Ordinal))
                .Select(spawn => new
                {
                    Spawn = spawn,
                    Coordinate = board.MapDefinition.Spaces.Single(space =>
                        space.Id.Equals(spawn.SpaceId)).Coordinate
                })
                .ToArray();

            var emptySpawns = heroSpawns
                .Where(value => board.IsEmptyPlayableSpace(value.Coordinate))
                .Select(value => new HeroRespawnDestination(
                    value.Coordinate, value.Spawn.Id, false))
                .ToArray();
            if (emptySpawns.Length > 0)
            {
                return new PendingHeroRespawnChoice(heroId, cardInstanceId, emptySpawns);
            }

            var fallback = heroSpawns
                .SelectMany(value => value.Coordinate.GetNeighbors()
                    .Where(board.IsEmptyPlayableSpace)
                    .Select(coordinate => new HeroRespawnDestination(
                        coordinate, value.Spawn.Id, true)))
                .GroupBy(value => value.Coordinate)
                .Select(group => group.OrderBy(value => value.AdjacentSpawnPointId.Value,
                    StringComparer.Ordinal).First())
                .ToArray();
            return new PendingHeroRespawnChoice(heroId, cardInstanceId, fallback);
        }
    }
}
