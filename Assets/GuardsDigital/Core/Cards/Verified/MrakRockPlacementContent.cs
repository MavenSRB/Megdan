using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.112b. The two rock placements Mrak's basic cards print, each split into p.18's
    /// short-supply step and the placement itself.
    ///
    /// <para><b>Fissure's line</b> (MR-Q2, user-settled "Skip it, keep going"): "Place a rock token
    /// in each of the first three empty spaces in the straight line from you in the direction of the
    /// attack." The line starts at the attacked space - read through
    /// <c>attacked-target-space@1</c>, because p.14's step 6 may already have removed the target - and
    /// runs outward; occupied spaces and terrain are skipped, and the walk ends only where the board
    /// does.</para>
    ///
    /// <para><b>Stone Grip's farthest spaces</b> (MR-Q3, "Fill; place what fits"): "place exactly 3
    /// rock tokens into empty spaces adjacent to the target and farthest away from you." The three
    /// empty adjacent spaces at the greatest distance from Mrak, the next-farthest filling in when
    /// fewer than three share it, Mrak choosing among a tie that does not fit whole; fewer than three
    /// empty spaces place as many as fit. Distance is p.18's "count the spaces between them".</para>
    ///
    /// <para><b>Why each is two nodes.</b> p.18: "If you need to place more tokens than are available
    /// in the supply, first remove up to that number of tokens of the same type from the board, if
    /// able." That removal can be a choice, and so can Stone Grip's tie. A node that asked twice would
    /// ask its second question from <c>Resume</c>, which a restored snapshot cannot rebuild - restore
    /// re-runs <c>Execute</c> and would ask the first question again. So the reclamation is its own
    /// node with at most one question, and the placement after it has at most one of its own.</para>
    ///
    /// <para>Nothing here is registered until the dealing (8.112g).</para>
    /// </summary>
    public static class MrakRockPlacementContent
    {
        public const string OriginParameterName = "origin";
        public const string ThroughParameterName = "through";
        public const string TargetParameterName = "target";
        public const string PlacedCountOutput = "placedCount";

        /// <summary>The most rocks Fissure's line and Stone Grip print.</summary>
        public const int PrintedRockCount = 3;

        /// <summary>
        /// How far a line may be walked: beyond any board this build ships, so the walk ends where the
        /// board does rather than at a printed number.
        /// </summary>
        private const int BoardReach = 64;

        public static EffectCapabilityReference ReclaimForLineV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("mrak-reclaim-rocks-for-line"), 1);

        public static EffectCapabilityReference PlaceAlongLineV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("mrak-place-rocks-along-line"), 1);

        public static EffectCapabilityReference ReclaimForFarthestAdjacentV1 =>
            new EffectCapabilityReference(
                new EffectCapabilityId("mrak-reclaim-rocks-for-farthest-adjacent"), 1);

        public static EffectCapabilityReference PlaceFarthestAdjacentV1 =>
            new EffectCapabilityReference(
                new EffectCapabilityId("mrak-place-rocks-farthest-adjacent"), 1);

        /// <summary>
        /// Milestone 8.112d. Stomping Step and Ground Shaker, "Place a rock token in the space it
        /// occupied": one rock into one named space - the space the moved unit left.
        /// </summary>
        public const string SpaceParameterName = "space";

        public static EffectCapabilityReference ReclaimForSpaceV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("mrak-reclaim-rock-for-space"), 1);

        public static EffectCapabilityReference PlaceInSpaceV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("mrak-place-rock-in-space"), 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new ReclaimHandler(ReclaimForLineV1, LineDemand.Instance),
                new PlaceAlongLineHandler(),
                new ReclaimHandler(ReclaimForFarthestAdjacentV1, FarthestAdjacentDemand.Instance),
                new PlaceFarthestAdjacentHandler(),
                new ReclaimHandler(ReclaimForSpaceV1, SpaceDemand.Instance),
                new PlaceInSpaceHandler()
            };

        // -------------------------------------------------------------------------------------------
        // Geometry, read live from the board every time
        // -------------------------------------------------------------------------------------------

        /// <summary>Every space a rock from the supply could be placed on right now.</summary>
        private static HashSet<HexCoordinate> EmptySpaces(
            EffectCapabilityContext context, HexCoordinate anchor)
        {
            var policy = new PlacePolicy(
                PlaceSourceContainer.Supply,
                PlaceEligiblePieceKind.Token,
                PlaceDestinationScope.EmptySpacesWithinRadius,
                BoardReach,
                PlaceChooserKind.ActingHero,
                PlaceNoDestinationPolicy.Impossible,
                PlaceDestinationAnchor.ActingHero);
            return new HashSet<HexCoordinate>(PlaceDestinationQuery.FindDestinations(
                policy, context.Components.Tokens, anchor, Array.Empty<HexCoordinate>(),
                context.Modifications));
        }

        /// <summary>
        /// Fissure's first three empty spaces: the line from <paramref name="origin"/> through
        /// <paramref name="through"/>, starting at <paramref name="through"/>, skipping every space
        /// that is not empty (MR-Q2).
        /// </summary>
        internal static IReadOnlyList<HexCoordinate> FirstEmptyAlongLine(
            EffectCapabilityContext context, HexCoordinate origin, HexCoordinate through)
        {
            if (!StraightLineQuery.TryGetDirection(origin, through, out var direction, out var distance) ||
                distance < 1)
            {
                return Array.Empty<HexCoordinate>();
            }

            var empty = EmptySpaces(context, origin);
            var found = new List<HexCoordinate>(PrintedRockCount);
            for (var step = distance; step <= BoardReach && found.Count < PrintedRockCount; step++)
            {
                var space = StraightLineQuery.Step(origin, direction, step);
                if (empty.Contains(space))
                {
                    found.Add(space);
                }
            }

            return found;
        }

        /// <summary>
        /// Stone Grip's empty spaces adjacent to the target, grouped by distance from Mrak, farthest
        /// group first.
        /// </summary>
        internal static IReadOnlyList<HexCoordinate[]> FarthestAdjacentGroups(
            EffectCapabilityContext context, HexCoordinate actor, HexCoordinate target)
        {
            var empty = EmptySpaces(context, actor);
            return empty
                .Where(space => HexDistanceQuery.Calculate(space, target) == 1)
                .GroupBy(space => HexDistanceQuery.Calculate(space, actor))
                .OrderByDescending(group => group.Key)
                .Select(group => group
                    .OrderBy(space => space.Q).ThenBy(space => space.R).ToArray())
                .ToArray();
        }

        private static int Available(EffectCapabilityContext context) =>
            context.Components.Catalog.CountAvailableTokens(
                MrakComponentContent.RockReference, context.Components.Tokens.PlacedTokens);

        private static PlacedToken[] StandingRocks(EffectCapabilityContext context) =>
            context.Components.Catalog
                .ReclaimableForPlacement(
                    MrakComponentContent.RockReference, context.Components.Tokens.PlacedTokens)
                .OrderBy(token => token.Coordinate.Q)
                .ThenBy(token => token.Coordinate.R)
                .ToArray();

        private static bool TryCoordinate(EffectCapabilityContext context, string name, out HexCoordinate at)
        {
            at = default;
            var value = context.Input(name);
            if (value.Kind != EffectValueKind.Coordinate) return false;
            at = value.Coordinate;
            return true;
        }

        private static bool TryHeroSpace(EffectCapabilityContext context, string name, out HexCoordinate at)
        {
            at = default;
            var value = context.Input(name);
            return value.Kind == EffectValueKind.Hero &&
                context.Components.Board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(value.HeroId), out at);
        }

        private static bool TryActorSpace(EffectCapabilityContext context, out HexCoordinate at) =>
            context.Components.Board.TryGetObjectCoordinate(
                BoardObjectReference.ForHero(context.ActorId), out at);

        private static bool PlaceRock(EffectCapabilityContext context, HexCoordinate space, int index)
        {
            var token = new PlacedToken(
                new TokenInstanceId(
                    $"{context.SourceCardInstanceId.Value}:{context.NodeId.Value}:mrak-rock" +
                    $":r{context.RoundIndex}t{context.TurnIndex}:{space.Q}:{space.R}:{index}"),
                MrakComponentContent.Rock,
                context.ActorId,
                context.ControllerId,
                context.SourceCardInstanceId,
                space);
            if (Available(context) < 1) return false;
            context.Components.Tokens.CommitValidatedTokenPlacement(token);
            return true;
        }

        private static EffectExecutionResult Placed(int count) =>
            EffectExecutionResult.Applied(new[]
            {
                new EffectOutcomeFact(PlacedCountOutput, EffectValue.Integer(count))
            });

        // -------------------------------------------------------------------------------------------
        // p.18's short-supply step
        // -------------------------------------------------------------------------------------------

        /// <summary>How many rocks the placement that follows will ask for, read from the live board.</summary>
        private interface IRockDemand
        {
            EffectCapabilityParameterSchema Schema { get; }

            bool TryMeasure(EffectCapabilityContext context, out int demand);
        }

        private sealed class LineDemand : IRockDemand
        {
            internal static readonly LineDemand Instance = new LineDemand();

            public EffectCapabilityParameterSchema Schema { get; } = LineSchema(false);

            public bool TryMeasure(EffectCapabilityContext context, out int demand)
            {
                demand = 0;
                if (!TryCoordinate(context, OriginParameterName, out var origin) ||
                    !TryCoordinate(context, ThroughParameterName, out var through))
                {
                    return false;
                }

                demand = FirstEmptyAlongLine(context, origin, through).Count;
                return true;
            }
        }

        private sealed class FarthestAdjacentDemand : IRockDemand
        {
            internal static readonly FarthestAdjacentDemand Instance = new FarthestAdjacentDemand();

            public EffectCapabilityParameterSchema Schema { get; } = TargetSchema(false);

            public bool TryMeasure(EffectCapabilityContext context, out int demand)
            {
                demand = 0;
                if (!TryActorSpace(context, out var actor) ||
                    !TryHeroSpace(context, TargetParameterName, out var target))
                {
                    return false;
                }

                demand = Math.Min(PrintedRockCount,
                    FarthestAdjacentGroups(context, actor, target).Sum(group => group.Length));
                return true;
            }
        }

        /// <summary>One rock, when the named space is empty; none when something stands there.</summary>
        private sealed class SpaceDemand : IRockDemand
        {
            internal static readonly SpaceDemand Instance = new SpaceDemand();

            public EffectCapabilityParameterSchema Schema { get; } = SpaceSchema(false);

            public bool TryMeasure(EffectCapabilityContext context, out int demand)
            {
                demand = 0;
                if (!TryActorSpace(context, out var actor) ||
                    !TryCoordinate(context, SpaceParameterName, out var space))
                {
                    return false;
                }

                demand = EmptySpaces(context, actor).Contains(space) ? 1 : 0;
                return true;
            }
        }

        /// <summary>
        /// p.18's short-supply step for one placement: declines when the pile covers the demand, takes
        /// back every standing rock when all of them are needed, and otherwise asks which. It measures
        /// the demand before anything is placed, so the rocks the placement is about to lay are never
        /// candidates.
        /// </summary>
        private sealed class ReclaimHandler : IEffectCapabilityHandler
        {
            private readonly IRockDemand demand;

            internal ReclaimHandler(EffectCapabilityReference reference, IRockDemand demand)
            {
                Reference = reference;
                this.demand = demand;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema => demand.Schema;

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!TryTake(context, out var take, out var standing))
                {
                    return EffectExecutionResult.DeclinedOptional();
                }

                if (take < standing.Length)
                {
                    return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor,
                        standing.Select(token => EffectValue.Space(token.Coordinate)).ToArray(),
                        take,
                        take));
                }

                foreach (var token in standing)
                {
                    context.Components.Tokens.CommitValidatedTokenReclamation(token.Id);
                }

                return EffectExecutionResult.Applied();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || !TryTake(context, out var take, out var standing))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var chosen = selected
                    .Where(value => value.Kind == EffectValueKind.Coordinate)
                    .Select(value => value.Coordinate)
                    .Distinct()
                    .Select(space => standing.FirstOrDefault(token => token.Coordinate.Equals(space)))
                    .ToArray();
                if (chosen.Length != selected.Count || chosen.Length != take ||
                    chosen.Any(token => token == null))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                foreach (var token in chosen)
                {
                    context.Components.Tokens.CommitValidatedTokenReclamation(token.Id);
                }

                return EffectExecutionResult.Applied();
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context?.Components != null && context.Components.IsLive
                    ? EffectExecutionResultKind.DeclinedOptional
                    : EffectExecutionResultKind.ImpossibleMandatory;

            private bool TryTake(
                EffectCapabilityContext context, out int take, out PlacedToken[] standing)
            {
                take = 0;
                standing = Array.Empty<PlacedToken>();
                if (context?.Components == null || !context.Components.IsLive ||
                    !demand.TryMeasure(context, out var needed))
                {
                    return false;
                }

                standing = StandingRocks(context);
                take = Math.Min(needed - Available(context), standing.Length);
                return take > 0;
            }
        }

        // -------------------------------------------------------------------------------------------
        // The placements
        // -------------------------------------------------------------------------------------------

        /// <summary>Fissure's line: automatic, "in EACH of the first three empty spaces".</summary>
        private sealed class PlaceAlongLineHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => PlaceAlongLineV1;

            public EffectCapabilityParameterSchema Schema { get; } = LineSchema(true);

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (context?.Components == null || !context.Components.IsLive)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                if (!TryCoordinate(context, OriginParameterName, out var origin) ||
                    !TryCoordinate(context, ThroughParameterName, out var through))
                {
                    return Placed(0);
                }

                var placed = 0;
                foreach (var space in FirstEmptyAlongLine(context, origin, through))
                {
                    if (PlaceRock(context, space, placed)) placed++;
                }

                return Placed(placed);
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException(
                    "mrak-place-rocks-along-line@1 places automatically and asks nothing.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context?.Components != null && context.Components.IsLive
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>
        /// Stone Grip's farthest spaces: as many as the demand and the pile allow, farthest first, and
        /// a question only for a tie that does not fit whole.
        /// </summary>
        private sealed class PlaceFarthestAdjacentHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => PlaceFarthestAdjacentV1;

            public EffectCapabilityParameterSchema Schema { get; } = TargetSchema(true);

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!TryPlan(context, out var certain, out var tie, out var fromTie))
                {
                    return context?.Components != null && context.Components.IsLive
                        ? Placed(0)
                        : EffectExecutionResult.ImpossibleMandatory();
                }

                if (fromTie > 0 && fromTie < tie.Length)
                {
                    return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor,
                        tie.Select(EffectValue.Space).ToArray(),
                        fromTie,
                        fromTie));
                }

                return PlaceAll(context, certain.Concat(tie.Take(fromTie)));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || !TryPlan(context, out var certain, out var tie, out var fromTie))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var chosen = selected
                    .Where(value => value.Kind == EffectValueKind.Coordinate)
                    .Select(value => value.Coordinate)
                    .Distinct()
                    .ToArray();
                if (chosen.Length != selected.Count || chosen.Length != fromTie ||
                    chosen.Any(space => !tie.Contains(space)))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return PlaceAll(context, certain.Concat(chosen));
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context?.Components != null && context.Components.IsLive
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            private static EffectExecutionResult PlaceAll(
                EffectCapabilityContext context, IEnumerable<HexCoordinate> spaces)
            {
                var placed = 0;
                foreach (var space in spaces)
                {
                    if (PlaceRock(context, space, placed)) placed++;
                }

                return Placed(placed);
            }

            /// <summary>
            /// The spaces taken without asking, the group a tie is drawn from, and how many of that
            /// group are taken - bounded by the printed three, the empty spaces and the pile.
            /// </summary>
            private static bool TryPlan(
                EffectCapabilityContext context,
                out HexCoordinate[] certain,
                out HexCoordinate[] tie,
                out int fromTie)
            {
                certain = Array.Empty<HexCoordinate>();
                tie = Array.Empty<HexCoordinate>();
                fromTie = 0;
                if (context?.Components == null || !context.Components.IsLive ||
                    !TryActorSpace(context, out var actor) ||
                    !TryHeroSpace(context, TargetParameterName, out var target))
                {
                    return false;
                }

                var wanted = Math.Min(PrintedRockCount, Available(context));
                var taken = new List<HexCoordinate>();
                foreach (var group in FarthestAdjacentGroups(context, actor, target))
                {
                    var room = wanted - taken.Count;
                    if (room <= 0) break;
                    if (group.Length <= room)
                    {
                        taken.AddRange(group);
                        continue;
                    }

                    tie = group;
                    fromTie = room;
                    break;
                }

                certain = taken.ToArray();
                return true;
            }
        }

        /// <summary>
        /// Milestone 8.112d. The stomp's one rock into the space the moved unit left: automatic, and
        /// nothing placed when that space is no longer empty or the pile (after p.18's step) is dry.
        /// </summary>
        private sealed class PlaceInSpaceHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => PlaceInSpaceV1;

            public EffectCapabilityParameterSchema Schema { get; } = SpaceSchema(true);

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (context?.Components == null || !context.Components.IsLive)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                if (!TryActorSpace(context, out var actor) ||
                    !TryCoordinate(context, SpaceParameterName, out var space) ||
                    !EmptySpaces(context, actor).Contains(space))
                {
                    return Placed(0);
                }

                return Placed(PlaceRock(context, space, 0) ? 1 : 0);
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException(
                    "mrak-place-rock-in-space@1 places automatically and asks nothing.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context?.Components != null && context.Components.IsLive
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        // -------------------------------------------------------------------------------------------

        private static EffectCapabilityParameterSchema SpaceSchema(bool publishesCount) =>
            new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(SpaceParameterName, EffectValueKind.Coordinate) },
                publishesCount
                    ? new[] { new EffectOutputBinding(PlacedCountOutput, EffectValueKind.Integer) }
                    : Array.Empty<EffectOutputBinding>());

        private static EffectCapabilityParameterSchema LineSchema(bool publishesCount) =>
            new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(OriginParameterName, EffectValueKind.Coordinate),
                    new EffectCapabilityParameter(ThroughParameterName, EffectValueKind.Coordinate)
                },
                publishesCount
                    ? new[] { new EffectOutputBinding(PlacedCountOutput, EffectValueKind.Integer) }
                    : Array.Empty<EffectOutputBinding>());

        private static EffectCapabilityParameterSchema TargetSchema(bool publishesCount) =>
            new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(TargetParameterName, EffectValueKind.Hero) },
                publishesCount
                    ? new[] { new EffectOutputBinding(PlacedCountOutput, EffectValueKind.Integer) }
                    : Array.Empty<EffectOutputBinding>());
    }
}
