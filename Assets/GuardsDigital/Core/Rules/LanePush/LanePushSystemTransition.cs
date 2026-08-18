using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.LanePush
{
    /// <summary>
    /// Rulebook p.8's Lane Push, as a catalog-owned multi-step system transition (milestone 6.11).
    ///
    /// This is the first production consumer of milestone 6.10a's continuation contract, which named it
    /// and did not build it. The page numbers three steps and this declares four, because the fifth
    /// acceptance check asks that an answered Place cannot "skip, repeat, or restart Wave, Zone,
    /// removal, token removal, or prior respawns" - and the removal and the respawn are only separately
    /// answerable if they are separately recorded. The page's third step therefore spans the last two.
    ///
    /// Only the last step derives a chooser, and only it can pause: p.8's sidebar is the one place the
    /// push asks anybody anything.
    /// </summary>
    public static class LanePushSystemTransition
    {
        public static SystemTransitionDefinitionId DefinitionId { get; } =
            new SystemTransitionDefinitionId("lane-push");

        /// <summary>p.8 step 1: "Flip one Wave counter face-down."</summary>
        public static SystemTransitionStepId FlipWaveStepId { get; } =
            new SystemTransitionStepId("lane-push-flip-wave");

        /// <summary>p.8 step 2: "The next Zone towards the losing team's Throne becomes the new Battle Zone."</summary>
        public static SystemTransitionStepId AdvanceZoneStepId { get; } =
            new SystemTransitionStepId("lane-push-advance-zone");

        /// <summary>p.8 step 3, first half: "Remove all minions in the previous Battle Zone."</summary>
        public static SystemTransitionStepId RemovePreviousZoneStepId { get; } =
            new SystemTransitionStepId("lane-push-remove-previous-zone");

        /// <summary>
        /// p.8 step 3, second half: "Respawn one minion of the matching type in each Spawn point in the
        /// new Battle Zone."
        /// </summary>
        public static SystemTransitionStepId RespawnStepId { get; } =
            new SystemTransitionStepId("lane-push-respawn");

        public static SystemTransitionDefinition Definition { get; } =
            new SystemTransitionDefinition(
                DefinitionId,
                "Lane Push",
                new List<SystemTransitionStepDefinition>
                {
                    new SystemTransitionStepDefinition(FlipWaveStepId),
                    new SystemTransitionStepDefinition(AdvanceZoneStepId),
                    new SystemTransitionStepDefinition(RemovePreviousZoneStepId),
                    SystemTransitionStepDefinition.Derived(RespawnStepId)
                });

        /// <summary>The map unit types that are minions, in the map specification's own vocabulary.</summary>
        internal static bool TryReadMinionType(string unitType, out StandardMinionType minionType)
        {
            switch (unitType)
            {
                case "melee-minion":
                    minionType = StandardMinionType.Melee;
                    return true;
                case "ranged-minion":
                    minionType = StandardMinionType.Ranged;
                    return true;
                case "heavy-minion":
                    minionType = StandardMinionType.Heavy;
                    return true;
                default:
                    minionType = StandardMinionType.None;
                    return false;
            }
        }

        internal static TeamSide ReadTeamSide(string teamId) =>
            string.Equals(teamId, "red", StringComparison.Ordinal) ? TeamSide.Red
                : string.Equals(teamId, "blue", StringComparison.Ordinal) ? TeamSide.Blue
                : TeamSide.None;
    }

    /// <summary>
    /// One item of the respawn step: which Spawn point it is about, and which miniature that Spawn point
    /// drew from the supply.
    ///
    /// Both halves are recorded because both are asked for. The Spawn point is what says whether this
    /// item has been dealt with - the respawn walks the new Battle Zone's Spawn points once and each is
    /// dealt with once - and the miniature is the "stable current step/minion identity" the fifth
    /// acceptance check names, which a re-derivation could not reproduce once that miniature has left
    /// the supply.
    ///
    /// A Spawn point with nothing left to draw records <see cref="NoMinion"/> rather than being skipped
    /// silently, because p.8's own example has one: "One Red minion will not be respawned."
    /// </summary>
    internal readonly struct LanePushRespawnItem
    {
        private const char Separator = ':';

        /// <summary>The named absence of a miniature, for a Spawn point whose team has none to draw.</summary>
        public const string NoMinion = "none";

        public LanePushRespawnItem(SpawnPointId spawnPointId, MinionId minionId)
        {
            SpawnPointId = spawnPointId;
            MinionId = minionId;
        }

        public SpawnPointId SpawnPointId { get; }
        public MinionId MinionId { get; }

        public SystemTransitionItemId ToItemId() =>
            new SystemTransitionItemId(
                $"{SpawnPointId.Value}{Separator}{(MinionId.IsDefined ? MinionId.Value : NoMinion)}");

        /// <summary>The Spawn-point half of a recorded item, which is what "already dealt with" means.</summary>
        public static string SpawnPointOf(SystemTransitionItemId itemId)
        {
            var value = itemId.Value ?? string.Empty;
            var separator = value.IndexOf(Separator);
            if (separator <= 0)
            {
                throw new InvalidOperationException(
                    $"Lane-Push respawn item '{itemId}' does not name a Spawn point and a miniature.");
            }

            return value.Substring(0, separator);
        }

        public static MinionId MinionOf(SystemTransitionItemId itemId)
        {
            var value = itemId.Value ?? string.Empty;
            var separator = value.IndexOf(Separator);
            if (separator <= 0 || separator == value.Length - 1)
            {
                throw new InvalidOperationException(
                    $"Lane-Push respawn item '{itemId}' does not name a Spawn point and a miniature.");
            }

            var minion = value.Substring(separator + 1);
            return string.Equals(minion, NoMinion, StringComparison.Ordinal)
                ? default
                : new MinionId(minion);
        }
    }

    /// <summary>
    /// What p.8's Lane-Push steps do (milestone 6.11), consulted by the continuation authority and
    /// holding no record of where the transition is.
    ///
    /// That division is the whole of why this type exists rather than a handler that drives the push
    /// itself. Milestone 6.10a's conformance rule reports any type that drives a transition and keeps
    /// step state of its own, because two records of one position is a defect a snapshot makes visible
    /// and nothing else does. This one keeps a board reference and a Wave-counter authority, asks them
    /// questions, and is told by its caller which step it is being asked about.
    /// </summary>
    public sealed class LanePushStepRules : ISystemTransitionStepRules
    {
        private readonly LanePushTransitionAuthority lanePush;

        public LanePushStepRules(LanePushTransitionAuthority lanePush)
        {
            this.lanePush = lanePush ?? throw new ArgumentNullException(nameof(lanePush));
        }

        private ILaneAdvanceAuthority Board => lanePush.Board;

        public SystemTransitionStepResolution ResolveStep(SystemTransitionStepContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var stepId = context.Step.Id;
            if (stepId.Equals(LanePushSystemTransition.FlipWaveStepId))
            {
                return FromStageOutcome(lanePush.ResolveWaveStage());
            }

            if (stepId.Equals(LanePushSystemTransition.AdvanceZoneStepId))
            {
                return FromStageOutcome(lanePush.ResolveZoneStage());
            }

            if (stepId.Equals(LanePushSystemTransition.RemovePreviousZoneStepId))
            {
                lanePush.ResolvePreviousZoneRemoval();
                return SystemTransitionStepResolution.Completed;
            }

            if (stepId.Equals(LanePushSystemTransition.RespawnStepId))
            {
                return ResolveRespawn(context);
            }

            throw new InvalidOperationException(
                $"Lane-Push step '{stepId}' has no rules; every step of a registered transition does.");
        }

        public void ApplyAnswer(SystemTransitionAnswerContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!context.Step.Id.Equals(LanePushSystemTransition.RespawnStepId))
            {
                throw new InvalidOperationException(
                    $"Lane-Push step '{context.Step.Id}' asks nobody anything, so nothing answers it.");
            }

            var minionId = LanePushRespawnItem.MinionOf(context.ItemId);
            var space = Board.MapDefinition.Spaces.SingleOrDefault(candidate =>
                candidate.Kind == BoardMapSpaceKind.Playable &&
                candidate.Id.Value == context.OptionId.Value);
            if (space == null ||
                !Board.CommitValidatedLanePushPlacement(minionId, space.Coordinate))
            {
                // Nothing else in the match runs while a transition is paused, so the board the options
                // were derived from is the board they are applied to. A legal option that cannot be
                // applied is an engine defect rather than a rules outcome.
                throw new InvalidOperationException(
                    $"A validated Lane-Push placement of '{minionId}' on '{context.OptionId}' could not " +
                    "be committed.");
            }
        }

        private static SystemTransitionStepResolution FromStageOutcome(LanePushStageOutcome outcome) =>
            outcome.Kind == LanePushStageOutcomeKind.GameEnded
                ? SystemTransitionStepResolution.EndedTransition
                : SystemTransitionStepResolution.Completed;

        /// <summary>
        /// p.8 step 3's second half, one Spawn point at a time.
        ///
        /// The order is: every Spawn point an obstacle is not standing on, in canonical identity order,
        /// and only then the ones it is - Tie-Breaker side first, and without flipping the coin. That
        /// ordering is not decoration. "The nearest empty space of their choice in the Battle Zone" is a
        /// question about the board at the moment of choosing, and answering it before the uncontested
        /// respawns had happened would let a chosen space be one a minion was about to respawn into,
        /// which would block that Spawn point in turn and cascade.
        /// </summary>
        private SystemTransitionStepResolution ResolveRespawn(SystemTransitionStepContext context)
        {
            if (!Board.CurrentBattleZoneId.HasValue)
            {
                throw new InvalidOperationException(
                    "p.8 respawns into the new Battle Zone, and the board reports none.");
            }

            var running = lanePush.Running;
            var handled = new HashSet<string>(
                context.CompletedItemIds.Select(LanePushRespawnItem.SpawnPointOf), StringComparer.Ordinal);
            var supply = Board.FindMinionSupply();
            var candidates = MinionSpawnPointsInBattleZone()
                .Where(candidate => !handled.Contains(candidate.SpawnPoint.Id.Value))
                .Select(candidate => new
                {
                    candidate.SpawnPoint,
                    candidate.Side,
                    candidate.MinionType,
                    candidate.Coordinate,
                    Blocked = IsBlockedByNonToken(candidate.Coordinate)
                })
                .OrderBy(candidate => candidate.Blocked ? 1 : 0)
                .ThenBy(candidate =>
                    candidate.Blocked && candidate.Side == running.TieBreakerSide ? 0 : 1)
                .ThenBy(candidate => candidate.SpawnPoint.Id.Value, StringComparer.Ordinal)
                .ToArray();

            var next = candidates.FirstOrDefault();
            if (next == null)
            {
                lanePush.CompleteRespawnStage();
                return SystemTransitionStepResolution.Completed;
            }

            var miniature = supply.FirstOrDefault(minion =>
                minion.TeamSide == next.Side && minion.MinionType == next.MinionType);
            if (miniature == null)
            {
                // p.8's own example: "One Red minion will not be respawned." The Spawn point is dealt
                // with and stays empty, which is recorded rather than skipped so a resumed respawn does
                // not look at it again.
                return SystemTransitionStepResolution.ItemCompleted(
                    new LanePushRespawnItem(next.SpawnPoint.Id, default).ToItemId());
            }

            var item = new LanePushRespawnItem(next.SpawnPoint.Id, miniature.MinionId).ToItemId();
            if (!next.Blocked)
            {
                // p.19's token clearance happens inside the commit, which is p.8's parenthesis: "If a
                // Spawn point is occupied by a token, first remove that token, then respawn the minion."
                var spawned = Board.CommitValidatedRespawnAtSpawnPoint(
                    miniature.MinionId, next.SpawnPoint.Id);
                if (!spawned.Accepted)
                {
                    throw new InvalidOperationException(
                        $"A Lane-Push respawn of '{miniature.MinionId}' at '{next.SpawnPoint.Id}' was " +
                        $"refused as {spawned.RejectionReason}, on a Spawn point nothing was standing on.");
                }

                return SystemTransitionStepResolution.ItemCompleted(item);
            }

            // p.8's sidebar: "If a Spawn point is occupied by a non-token obstacle, the owning team must
            // place that minion in the nearest empty space of their choice in the Battle Zone."
            var destinations = NearestQuery.FindNearest(next.Coordinate, EmptyBattleZoneCoordinates());
            if (destinations.Count == 0)
            {
                // A Battle Zone with no empty space at all leaves the sidebar with nothing to choose
                // between, so the miniature stays in the supply and the Spawn point is dealt with.
                return SystemTransitionStepResolution.ItemCompleted(item);
            }

            return SystemTransitionStepResolution.Paused(
                item,
                next.Side,
                destinations.Select(coordinate => new SystemTransitionOptionId(
                    SpaceIdAt(coordinate).Value)));
        }

        private IEnumerable<SpawnPointCandidate> MinionSpawnPointsInBattleZone()
        {
            var zoneId = Board.CurrentBattleZoneId.Value;
            foreach (var spawnPoint in Board.MapDefinition.SpawnPoints)
            {
                if (!LanePushSystemTransition.TryReadMinionType(spawnPoint.UnitType, out var minionType))
                {
                    continue;
                }

                var side = LanePushSystemTransition.ReadTeamSide(spawnPoint.TeamId);
                if (side == TeamSide.None) continue;

                var space = Board.MapDefinition.Spaces.Single(candidate =>
                    candidate.Id.Equals(spawnPoint.SpaceId));
                if (!space.ZoneId.HasValue || !space.ZoneId.Value.Equals(zoneId)) continue;

                yield return new SpawnPointCandidate(spawnPoint, side, minionType, space.Coordinate);
            }
        }

        private IEnumerable<HexCoordinate> EmptyBattleZoneCoordinates()
        {
            var zoneId = Board.CurrentBattleZoneId.Value;
            return Board.MapDefinition.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable &&
                    space.ZoneId.HasValue && space.ZoneId.Value.Equals(zoneId) &&
                    Board.IsEmptyPlayableSpace(space.Coordinate))
                .Select(space => space.Coordinate);
        }

        /// <summary>
        /// Whether something other than a token is standing on a Spawn point. A token is the one
        /// occupant p.8 clears out of the way; every other one sends the miniature elsewhere.
        /// </summary>
        private bool IsBlockedByNonToken(HexCoordinate coordinate) =>
            !Board.IsEmptyPlayableSpace(coordinate) && !Board.HasTokenAt(coordinate);

        private SpaceId SpaceIdAt(HexCoordinate coordinate) =>
            Board.MapDefinition.Spaces
                .Single(space =>
                    space.Kind == BoardMapSpaceKind.Playable && space.Coordinate.Equals(coordinate))
                .Id;

        private sealed class SpawnPointCandidate
        {
            public SpawnPointCandidate(
                BoardMapSpawnPoint spawnPoint,
                TeamSide side,
                StandardMinionType minionType,
                HexCoordinate coordinate)
            {
                SpawnPoint = spawnPoint;
                Side = side;
                MinionType = minionType;
                Coordinate = coordinate;
            }

            public BoardMapSpawnPoint SpawnPoint { get; }
            public TeamSide Side { get; }
            public StandardMinionType MinionType { get; }
            public HexCoordinate Coordinate { get; }
        }
    }
}
