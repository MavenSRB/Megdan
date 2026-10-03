using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    internal static partial class ExpectedOutcomeExecutor
    {
        private sealed partial class CaseRun
        {
            private sealed class SpawnConstraint
            {
                internal int Count;
                internal int Within;
                internal string Origin;
                internal TeamSide? Side;
                internal StandardMinionType? Type;
            }

            private readonly List<SpawnConstraint> spawnConstraints = new List<SpawnConstraint>();
            private readonly List<Piece> reservePieces = new List<Piece>();

            private StandardMinionType SpawnType(object value)
            {
                switch (Normalize(value as string))
                {
                    case "melee": return StandardMinionType.Melee;
                    case "ranged": return StandardMinionType.Ranged;
                    case "heavy": return StandardMinionType.Heavy;
                    default: throw Unsupported("minion type", Describe(value));
                }
            }

            private void ReadSpawnStaging(JsonObject spec)
            {
                if (spec.Has("empty-minion-spawns"))
                {
                    var constraints = spec.Get("empty-minion-spawns") as List<object>
                        ?? throw Unsupported("empty-minion-spawns", Describe(spec.Get("empty-minion-spawns")));
                    foreach (var raw in constraints)
                    {
                        var value = raw as JsonObject ?? throw Unsupported("spawn constraint", Describe(raw));
                        foreach (var pair in value)
                            if (!new[] { "count", "within", "of", "side", "type" }.Contains(pair.Key))
                                throw Unsupported("spawn constraint key", pair.Key);
                        if (value.Has("of") && !(value.Get("of") is string))
                            throw Unsupported("spawn origin", Describe(value.Get("of")));
                        var constraint = new SpawnConstraint
                        {
                            Count = IntOf(value.Get("count"), "spawn count"),
                            Within = IntOf(value.Get("within"), "spawn within"),
                            Origin = value.Get("of") as string ?? "actor"
                        };
                        if (constraint.Count < 0 || constraint.Within < 0 || !byRef.ContainsKey(constraint.Origin))
                            throw Refuse("invalid empty-minion-spawns count, distance or reference");
                        if (value.Has("side"))
                        {
                            var side = value.Get("side") as string;
                            if (side != "friendly" && side != "enemy") throw Unsupported("spawn side", Describe(side));
                            constraint.Side = side == "friendly" ? actorSide : Opposite(actorSide);
                        }
                        if (value.Has("type")) constraint.Type = SpawnType(value.Get("type"));
                        spawnConstraints.Add(constraint);
                    }
                }
                if (!spec.Has("reserve-minions")) return;
                var reserves = spec.Get("reserve-minions") as List<object>
                    ?? throw Unsupported("reserve-minions", Describe(spec.Get("reserve-minions")));
                foreach (var raw in reserves)
                {
                    var value = raw as JsonObject ?? throw Unsupported("reserve minion", Describe(raw));
                    if (value.Any(pair => pair.Key != "ref" && pair.Key != "type"))
                        throw Unsupported("reserve minion", Describe(value));
                    var reference = value.Get("ref") as string ?? string.Empty;
                    var shape = RefShape.Match(reference);
                    if (!shape.Success || (shape.Groups[1].Value != "friendly-minion" &&
                        shape.Groups[1].Value != "enemy-minion") || byRef.ContainsKey(reference))
                        throw Unsupported("reserve minion reference", reference);
                    var piece = new Piece
                    {
                        Ref = reference,
                        Kind = reference.StartsWith("friendly-", StringComparison.Ordinal) ? RefKind.FriendlyMinion : RefKind.EnemyMinion,
                        MinionType = SpawnType(value.Get("type") ?? "melee"),
                        Spec = value,
                        Where = ParsePlacement("distance 4 from actor"),
                        BattleZone = true
                    };
                    pieces.Add(piece);
                    byRef.Add(reference, piece);
                    reservePieces.Add(piece);
                }
            }

            private bool FitsSpawnStaging(IReadOnlyDictionary<string, HexCoordinate> positions,
                HashSet<HexCoordinate> occupied, out string reason)
            {
                reason = null;
                if (spawnConstraints.Count == 0) return true;
                var map = Map.Value;
                var zone = map.Zones.Single(value => value.IsStartingBattleZone).Id;
                var visibleOccupied = new HashSet<HexCoordinate>(occupied);
                foreach (var reserve in reservePieces) visibleOccupied.Remove(reserve.At);
                foreach (var constraint in spawnConstraints)
                {
                    var actual = map.SpawnPoints.Count(point =>
                    {
                        var definition = setup.Minions.FirstOrDefault(value => value.SpawnPoint.SpawnPointId.Equals(point.Id));
                        if (definition == null) return false;
                        var type = definition.MinionType;
                        if (constraint.Type.HasValue && constraint.Type.Value != type) return false;
                        if (constraint.Side.HasValue && constraint.Side.Value != definition.TeamSide) return false;
                        var space = map.Spaces.Single(value => value.Id.Equals(point.SpaceId));
                        return space.ZoneId.HasValue && space.ZoneId.Value.Equals(zone) &&
                            !visibleOccupied.Contains(space.Coordinate) &&
                            Dist(space.Coordinate, positions[constraint.Origin]) <= constraint.Within;
                    });
                    if (actual != constraint.Count)
                    {
                        reason = "unresolvable exact empty-minion-spawns: wanted " + constraint.Count +
                            " within " + constraint.Within + " of " + constraint.Origin + ", found " + actual;
                        return false;
                    }
                }
                return true;
            }

            private void VerifySpawnStaging()
            {
                if (spawnConstraints.Count == 0 && reservePieces.Count == 0) return;
                var board = session.aggregate.Board;
                var facts = EmptyMinionSpawnPointQuery.FindFactsInCurrentBattleZone(board, board, SpawnPointOverlay.None);
                foreach (var constraint in spawnConstraints)
                {
                    var origin = PositionOf(session.Projection, constraint.Origin);
                    if (!origin.HasValue) throw Refuse("spawn constraint origin is not on the board");
                    var actual = facts.Count(point =>
                        (!constraint.Side.HasValue || point.TeamSide == constraint.Side.Value) &&
                        (!constraint.Type.HasValue || point.MinionType == constraint.Type.Value) &&
                        Dist(point.At, origin.Value) <= constraint.Within);
                    if (actual != constraint.Count) throw Refuse("restored empty-minion-spawns count differs: " + actual);
                    Log("verified empty minion spawns: " + actual + " within " + constraint.Within + " of " + constraint.Origin);
                }
                foreach (var reserve in reservePieces)
                {
                    if (!board.FindUnitSupply().Any(value => value.MinionId.Equals(reserve.Minion)))
                        throw Refuse("staged reserve miniature missing after restore: " + reserve.Ref);
                    Log("reserve " + reserve.Ref + ": " + reserve.Minion.Value + " " + reserve.MinionType);
                }
            }

            private void EvaluateRespawn(JsonObject expectation)
            {
                var spec = expectation.Get("respawned") as JsonObject;
                if (expectation.Count != 1 || spec == null ||
                    spec.Any(pair => pair.Key != "ref" && pair.Key != "type" && pair.Key != "at"))
                    throw Unsupported("respawned expectation", Describe(expectation));
                var reference = spec.Get("ref") as string;
                var piece = reservePieces.SingleOrDefault(value => value.Ref == reference);
                if (piece == null) throw Refuse("respawned reference must name a staged reserve miniature");
                var type = SpawnType(spec.Get("type"));
                var side = piece.Kind == RefKind.FriendlyMinion ? actorSide : Opposite(actorSide);
                var current = after.Minions.SingleOrDefault(value => value.MinionId.Equals(piece.Minion));
                if (before.Minions.Any(value => value.MinionId.Equals(piece.Minion)) || current == null)
                {
                    Mismatch(expectation, "the named reserve miniature did not enter the board");
                    return;
                }
                if (current.MinionType != type || current.TeamSide != side)
                    Mismatch(expectation, "respawned miniature has the wrong type or side");
                var countBefore = before.Minions.Count(value => value.TeamSide == side && value.MinionType == type);
                var countAfter = after.Minions.Count(value => value.TeamSide == side && value.MinionType == type);
                if (countAfter != countBefore + 1) Mismatch(expectation, "minion count did not increase by exactly one");
                var placement = ParsePlacement(spec.Get("at") as string)
                    ?? throw Unsupported("respawn placement", Describe(spec.Get("at")));
                var positions = Positions(before, byRef.Keys);
                // The destination was empty before the action; afterwards the respawned minion occupies it.
                var restored = GuardsDigital.Core.Snapshots.AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(), before.AuthoritativeSnapshot, before.Definitions,
                    new[] { before.SelectedMap }, before.Setup.Minions);
                if (!Satisfies(placement, current.Coordinate, positions, restored.Board.IsEmptyPlayableSpace))
                    Mismatch(expectation, "respawn destination does not satisfy the pre-action placement");
                var destination = EmptyMinionSpawnPointQuery.FindFactsInCurrentBattleZone(
                    restored.Board, restored.Board, SpawnPointOverlay.None);
                if (!destination.Any(value => value.At.Equals(current.Coordinate) && value.TeamSide == side && value.MinionType == type))
                    Mismatch(expectation, "destination was not an empty friendly spawn point of the requested type");
            }
        }
    }
}
