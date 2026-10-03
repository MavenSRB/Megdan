using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Snapshots;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    internal static partial class ExpectedOutcomeExecutor
    {
        private sealed partial class CaseRun
        {
            private bool PushZoneFits(Piece piece, HexCoordinate at,
                IReadOnlyDictionary<string, HexCoordinate> positions,
                IReadOnlyDictionary<HexCoordinate, BoardMapSpace> spaces)
            {
                if (!(piece.Spec.Get("push-zone") is string wanted)) return true;
                var actorAt = positions["actor"];
                var beyond = new HexCoordinate(at.Q + at.Q - actorAt.Q, at.R + at.R - actorAt.R);
                if (!spaces.TryGetValue(at, out var start) || !spaces.TryGetValue(beyond, out var end) ||
                    start.Kind != BoardMapSpaceKind.Playable || end.Kind != BoardMapSpaceKind.Playable ||
                    !start.ZoneId.HasValue || !end.ZoneId.HasValue)
                    return false;
                var crosses = !start.ZoneId.Value.Equals(end.ZoneId.Value);
                return crosses == (wanted == "cross");
            }

            /// <summary>
            /// A source-relative check of a placement question. It requires a real, free witness in
            /// each named class within printed range before judging the question's offered spaces.
            /// This keeps an exclusion from passing vacuously on a map with no such destination.
            /// </summary>
            private bool TryEvaluateSupplemental(JsonObject expectation)
            {
                if (expectation.Get("solo-outcome") is JsonObject solo)
                    return EvaluateSoloOutcome(expectation, solo);
                if (expectation.Get("match-ongoing") is bool ongoing)
                {
                    if (expectation.Count != 1 || !ongoing)
                        throw Unsupported("match-ongoing expectation", Describe(expectation));
                    if (after.Outcome.HasEnded)
                        Mismatch(expectation, "the match ended with " + after.Outcome.Cause);
                    return true;
                }
                if (expectation.Get("hero-terrain-lifetime") is JsonObject lifetime)
                    return EvaluateHeroTerrainLifetime(expectation, lifetime);
                if (expectation.Get("push-zone") is JsonObject push)
                {
                    if (expectation.Count != 1 || push.Count != 2 ||
                        !(push.Get("ref") is string subject) ||
                        !(push.Get("relation") is string relation) ||
                        relation != "same" && relation != "cross")
                        throw Unsupported("push-zone expectation", Describe(expectation));
                    var start = PositionOf(before, subject) ?? throw Refuse(subject + " did not start on board");
                    var end = PositionOf(after, subject) ?? throw Refuse(subject + " did not finish on board");
                    var startZone = Map.Value.Spaces.Single(space => space.Coordinate.Equals(start)).ZoneId;
                    var endZone = Map.Value.Spaces.Single(space => space.Coordinate.Equals(end)).ZoneId;
                    if (!startZone.HasValue || !endZone.HasValue ||
                        (!startZone.Value.Equals(endZone.Value)) != (relation == "cross"))
                        Mismatch(expectation, subject + " finished in the wrong Zone relation");
                    return true;
                }
                if (expectation.Get("same-zone") is JsonObject pair)
                {
                    if (expectation.Count != 1 || pair.Count != 2 ||
                        !(pair.Get("a") is string first) || !(pair.Get("b") is string second))
                        throw Unsupported("same-zone expectation", Describe(expectation));
                    var firstAt = PositionOf(after, first) ?? throw Refuse(first + " is off board");
                    var secondAt = PositionOf(after, second) ?? throw Refuse(second + " is off board");
                    var firstZone = Map.Value.Spaces.Single(space => space.Coordinate.Equals(firstAt)).ZoneId;
                    var secondZone = Map.Value.Spaces.Single(space => space.Coordinate.Equals(secondAt)).ZoneId;
                    if (!firstZone.HasValue || !secondZone.HasValue ||
                        !firstZone.Value.Equals(secondZone.Value))
                        Mismatch(expectation, first + " and " + second + " do not stand in the same Zone");
                    return true;
                }
                if (expectation.Get("card-text-fast-travel") is bool traveled)
                {
                    if (expectation.Count != 1)
                        throw Unsupported("card-text-fast-travel expectation", Describe(expectation));
                    if (cardTextFastTravelOffered != traveled ||
                        cardTextFastTravelCommitted != traveled)
                        Mismatch(expectation, "end-turn card-text Fast Travel offered=" +
                            cardTextFastTravelOffered + ", relocated=" + cardTextFastTravelCommitted);
                    return true;
                }
                if (!(expectation.Get("landing-offers") is JsonObject spec)) return false;
                if (expectation.Count != 1 || spec.Count != 4 ||
                    !(spec.Get("range") is double rangeValue) || rangeValue < 1 ||
                    rangeValue != Math.Truncate(rangeValue) ||
                    !(spec.Get("spawn-point") is bool spawnWanted) ||
                    !(spec.Get("beside-empty-spawn") is bool besideWanted) ||
                    !(spec.Get("other-open-space") is bool legalWanted) ||
                    spec.Any(pair => pair.Key != "range" && pair.Key != "spawn-point" &&
                        pair.Key != "beside-empty-spawn" && pair.Key != "other-open-space"))
                    throw Unsupported("landing-offers expectation", Describe(expectation));

                var origin = PositionOf(before, "actor") ??
                    throw Refuse("landing-offers: actor was not on the board before the action");
                var range = (int)rangeValue;
                var emptySpawns = SpawnSpaces.Value.Where(space => !Occupied(before, space)).ToArray();
                var spaces = Map.Value.Spaces.Select(space => space.Coordinate)
                    .Where(space => !space.Equals(origin) && !Occupied(before, space) &&
                        Dist(origin, space) <= range).ToArray();
                var spawn = spaces.Where(space => SpawnSpaces.Value.Contains(space)).ToArray();
                var beside = spaces.Where(space => !SpawnSpaces.Value.Contains(space) &&
                    emptySpawns.Any(point => Dist(space, point) == 1)).ToArray();
                var legal = spaces.Where(space => !SpawnSpaces.Value.Contains(space) &&
                    !emptySpawns.Any(point => Dist(space, point) == 1)).ToArray();
                if (spawn.Length == 0 || beside.Length == 0 || legal.Length == 0)
                    throw Refuse("landing-offers: the staged board lacks a free spawn, a free non-spawn " +
                        "beside an empty spawn, or a free legal control within range " + range);
                if (offeredEffectDestinations.Count == 0)
                {
                    Mismatch(expectation, "no coordinate choice was asked");
                    return true;
                }

                var offered = offeredEffectDestinations.SelectMany(question => question)
                    .ToHashSet();
                Check("spawn-point", spawn, spawnWanted);
                Check("beside-empty-spawn", beside, besideWanted);
                Check("other-open-space", legal, legalWanted);
                return true;

                void Check(string label, IEnumerable<HexCoordinate> witnesses, bool wanted)
                {
                    var actual = witnesses.Any(offered.Contains);
                    if (actual != wanted)
                        Mismatch(expectation, label + " offered: " + actual);
                }
            }

            private bool EvaluateSoloOutcome(JsonObject expectation, JsonObject spec)
            {
                if (expectation.Count != 1 || spec.Count != 2 ||
                    !(spec.Get("winner") is string winner) ||
                    !(spec.Get("teammate") is string teammate) ||
                    spec.Any(pair => pair.Key != "winner" && pair.Key != "teammate"))
                    throw Unsupported("solo-outcome expectation", Describe(expectation));
                var winnerId = HeroOfRef(winner);
                var teammateId = HeroOfRef(teammate);
                if (winnerId.Equals(teammateId) ||
                    !setup.Heroes.Single(hero => hero.HeroId.Equals(winnerId)).TeamId.Equals(
                        setup.Heroes.Single(hero => hero.HeroId.Equals(teammateId)).TeamId))
                    throw Refuse("solo-outcome requires a distinct teammate of the named winner");
                var outcome = after.Outcome;
                if (!outcome.HasEnded || !outcome.HasSoloWinner ||
                    !outcome.WinningHeroId.Equals(winnerId) ||
                    outcome.WinningHeroId.Equals(teammateId) ||
                    outcome.WinningTeamId.IsDefined || outcome.LosingTeamId.IsDefined)
                    Mismatch(expectation, "outcome is " + outcome.Cause + " with hero " +
                        outcome.WinningHeroId + ", winning team " + outcome.WinningTeamId +
                        ", losing team " + outcome.LosingTeamId);
                return true;
            }

            private bool EvaluateHeroTerrainLifetime(JsonObject expectation, JsonObject spec)
            {
                if (expectation.Count != 1 || spec.Count != 1 ||
                    !(spec.Get("ref") is string reference) ||
                    !byRef.TryGetValue(reference, out var piece) || !piece.IsHero)
                    throw Unsupported("hero-terrain-lifetime expectation", Describe(expectation));
                if (afterOriginalAction == null || atFollowup == null || afterRound == null)
                    throw Refuse("hero-terrain-lifetime needs a next-turn follow-up and a later timing checkpoint");
                Check(before, false, "before Stone Gaze");
                Check(afterOriginalAction, false, "on Stone Gaze's own turn");
                Check(atFollowup, true, "during the next turn");
                Check(afterRound, false, "after the next turn");
                return true;

                void Check(GuardsDigital.Application.MatchSessionProjection projection, bool terrainExpected,
                    string phase)
                {
                    var restored = AuthoritativeSnapshotSerializer.Deserialize(
                        MatchContentCatalogs.ForThisBuild(), projection.AuthoritativeSnapshot,
                        projection.Definitions, new[] { projection.SelectedMap }, projection.Setup.Minions);
                    var modifications = RuleModificationQuery.Build(
                        restored.Round.Timing, restored.Board, restored.Participants,
                        restored.Round.TieBreakerState,
                        UltimateUnlockQuery.Derive(projection.Definitions, restored.Cards),
                        RuleObject.None, restored.Round.HeroItems, restored.Round.Markers);
                    var subject = RuleObject.ForHero(piece.Hero);
                    var isHero = EffectiveUnitKindQuery.Is(subject, EffectiveUnitKind.Hero, modifications);
                    var isTerrain = EffectiveUnitKindQuery.Is(subject, EffectiveUnitKind.Terrain, modifications);
                    if (!isHero || isTerrain != terrainExpected)
                        Mismatch(expectation, phase + ": hero=" + isHero + ", terrain=" + isTerrain);
                }
            }
        }
    }
}
