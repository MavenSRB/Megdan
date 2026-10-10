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
        /// <summary>
        /// Milestone 9.32c. p.7 respawns into an empty space, and an empty space a unit overlay bars to
        /// this hero is not one for it (Takahide's Spinning Blade, TK-Q3: "cannot be ... respawned into
        /// them"). Asked with the turn's live set; <see cref="GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty"/> is the board's own question.
        /// </summary>
        private static bool IsEmptyFor(
            AdjacentMoveAuthority board, GuardsDigital.Core.Board.HexCoordinate space, HeroId heroId,
            GuardsDigital.Core.Rules.Modifications.RuleModificationSet modifications) =>
            board.IsEmptyPlayableSpace(space) &&
            !modifications.AnyOverlayMakesAnObstacleFor(
                space, GuardsDigital.Core.Rules.Modifications.RuleObject.ForHero(heroId));

        public static PendingHeroRespawnChoice CreateChoice(
            HeroId heroId,
            CardInstanceId cardInstanceId,
            MatchParticipants participants,
            AdjacentMoveAuthority board,
            GuardsDigital.Core.Rules.Modifications.RuleModificationSet modifications,
            IReadOnlyList<HexCoordinate> referenceSpaces = null)
        {
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            if (board.IsHeroOnBoard(heroId))
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

            // W-022, PVE-D18. A mode may name the spaces a side respawns onto instead of its Throne's
            // spawn points. They take the spawn points' place in everything below - empty ones are
            // offered, otherwise the nearest empty spaces to them - and each keeps the side's first
            // Throne spawn as its informational anchor. No existing match names any.
            if (referenceSpaces != null && referenceSpaces.Count > 0 && heroSpawns.Length > 0)
            {
                var anchor = heroSpawns
                    .OrderBy(value => value.Spawn.Id.Value, StringComparer.Ordinal)
                    .First()
                    .Spawn;
                heroSpawns = referenceSpaces
                    .Select(coordinate => new { Spawn = anchor, Coordinate = coordinate })
                    .ToArray();
            }

            var emptySpawns = heroSpawns
                .Where(value => IsEmptyFor(board, value.Coordinate, heroId, modifications))
                .Select(value => new HeroRespawnDestination(
                    value.Coordinate, value.Spawn.Id, false))
                .ToArray();
            if (emptySpawns.Length > 0)
            {
                return new PendingHeroRespawnChoice(heroId, cardInstanceId, emptySpawns);
            }

            // Milestone 7.4. The nearest empty space, read over the whole map rather than over the
            // spawn point's own six neighbours.
            //
            // p.7 sends a respawning hero to an empty spawn point and, when none is empty, to the
            // nearest empty space to one - and "nearest" is a distance rather than a ring. Until this
            // milestone the fallback searched the adjacent ring alone, which answered the same thing
            // whenever any neighbour was free and answered *nothing* when none was: a hero with all
            // its spawn points and every neighbouring space occupied was offered no destination at
            // all, on a Phase 4 map with dozens of empty spaces on it. The search now widens ring by
            // ring and stops at the first distance that has an empty playable space, so it offers
            // every space tied at that distance and never more than one ring's worth.
            //
            // Distance is measured to the spawn point rather than to the hero's last position, which
            // is what the page says and is also the only reading available: a hero being offered a
            // respawn is off the board and has no position to measure from.
            var candidates = board.MapDefinition.Spaces
                .Select(space => space.Coordinate)
                .Where(coordinate => IsEmptyFor(board, coordinate, heroId, modifications))
                .Select(coordinate => new
                {
                    Coordinate = coordinate,
                    Nearest = heroSpawns
                        .Select(spawn => new
                        {
                            spawn.Spawn.Id,
                            Distance = GuardsDigital.Core.Board.HexDistanceQuery.Calculate(
                                coordinate, spawn.Coordinate)
                        })
                        .OrderBy(value => value.Distance)
                        .ThenBy(value => value.Id.Value, StringComparer.Ordinal)
                        .FirstOrDefault()
                })
                .Where(value => value.Nearest != null && value.Nearest.Distance > 0)
                .ToArray();
            if (candidates.Length == 0)
            {
                return new PendingHeroRespawnChoice(
                    heroId, cardInstanceId, Array.Empty<HeroRespawnDestination>());
            }

            var nearestDistance = candidates.Min(value => value.Nearest.Distance);
            var fallback = candidates
                .Where(value => value.Nearest.Distance == nearestDistance)
                .Select(value => new HeroRespawnDestination(
                    value.Coordinate, value.Nearest.Id, true))
                .ToArray();
            return new PendingHeroRespawnChoice(heroId, cardInstanceId, fallback);
        }
    }
}
