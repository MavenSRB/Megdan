using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 9.21e. Which of the acting hero's bodies a removal by his own text may take, one
    /// member per printed quantity (RZ-Q6: "one of you" and "all of you" include the acting body;
    /// "other you" excludes it).
    /// </summary>
    public enum ActorBodyRemoval
    {
        None = 0,

        /// <summary>"Remove one of you": exactly one standing body, the acting body included.</summary>
        OneBody = 1,

        /// <summary>"Remove one or more of you in play, except the last one of you": one to all but one.</summary>
        OneOrMoreButNotTheLast = 2,

        /// <summary>"Remove one or more of you in play, including all of you": one to all of them.</summary>
        OneOrMoreIncludingAll = 3,

        /// <summary>"Remove all other you in play": every body but the acting one, nothing chosen.</summary>
        EveryOtherBody = 4
    }

    /// <summary>
    /// Milestone 9.21d. p.19's Spawn of one more body of the acting hero - the first card text that
    /// adds a body to a hero standing on several spaces (the many-bodies model, 9.21a).
    ///
    /// <para><b>What it does.</b> It takes the lowest body left in the actor's declared supply
    /// (<see cref="IUnitSpawnAuthority.FindHeroBodySupply"/>; RZ-Q1, no reclaim - p.19's Spawn "must be
    /// taken from the supply and cannot be taken from the board") and puts it into an empty space the
    /// actor chooses within the action's printed Radius, raised by Radius items (R1), measured from
    /// the acting body (RZ-Q4: <see cref="ITokenPlacementAuthority.TryGetHeroCoordinate"/> answers the
    /// body settled for the action). The body is a <see cref="Units.Heroes.HeroOccupancy"/> of the same
    /// hero, so presence, targeting and a Defeat of any body (RZ-Q3) already see it.</para>
    ///
    /// <para><b>One body per node,</b> for the reason the token placement is one token per node: "up
    /// to 3" is the composition's three optional nodes, each asking again once the previous body
    /// stands. A node that finds the supply empty or no space in reach is impossible, which an
    /// optional node records as a legal empty step (W5: "up to N" admits 0).</para>
    ///
    /// <para><b>Milestone 9.21e adds the inverse,</b> p.19's Remove of the actor's own bodies by his
    /// text (<see cref="RemoveActorBodiesHandler"/>, bounded by <see cref="ActorBodyRemoval"/>), and the
    /// read-only "more than one of you in play" gate. A removal is never a Defeat (RZ-Q6): it commits
    /// through <see cref="IUnitSpawnAuthority.CommitValidatedHeroBodyRemoval"/>, never p.13's stages.</para>
    ///
    /// <para>Nothing here names a card or a hero; nothing registers it until the hero whose content
    /// composes it is dealt.</para>
    /// </summary>
    public static class SyntheticHeroBodyCapabilities
    {
        /// <summary>The space the spawned body stands on, for a following node to read.</summary>
        public const string SpawnedOutput = "spawned";

        /// <summary>One handler under the reference the composing content names.</summary>
        public static IEffectCapabilityHandler SpawnActorBodyHandler(EffectCapabilityReference reference) =>
            new SpawnActorBodyInRadiusHandler(reference);

        /// <summary>
        /// Milestone 9.21e. The closed-vocabulary input naming which of the actor's bodies a removal
        /// may take (<see cref="ActorBodyRemoval"/>).
        /// </summary>
        public const string RemovalParameterName = "removal";

        /// <summary>Milestone 9.21e. How many bodies the removal took off the board.</summary>
        public const string RemovedOutput = "removed";

        /// <summary>Milestone 9.21e. Whether more than one body of the actor is in play.</summary>
        public const string SeveralBodiesOutput = "several";

        /// <summary>
        /// Milestone 9.21e. p.19's Remove of bodies of the acting hero by his own text: the bodies the
        /// <see cref="RemovalParameterName"/> input admits leave the board and return to the supply
        /// (<see cref="IUnitSpawnAuthority.CommitValidatedHeroBodyRemoval"/>). Never a Defeat.
        /// </summary>
        public static IEffectCapabilityHandler RemoveActorBodiesHandler(EffectCapabilityReference reference) =>
            new RemoveActorBodiesFromTheBoard(reference);

        /// <summary>
        /// Milestone 9.21e. The read-only predicate "there is more than one of you in play": more than
        /// one body of the acting hero stands on the board.
        /// </summary>
        public static IEffectCapabilityHandler ActorHasSeveralBodiesHandler(EffectCapabilityReference reference) =>
            new ActorHasSeveralBodiesPredicate(reference);

        private sealed class RemoveActorBodiesFromTheBoard : IEffectCapabilityHandler
        {
            public RemoveActorBodiesFromTheBoard(EffectCapabilityReference reference)
            {
                Reference = reference;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(
                        RemovalParameterName, EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.AllExcept(ActorBodyRemoval.None))
                },
                new[] { new EffectOutputBinding(RemovedOutput, EffectValueKind.Integer) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var plan = Plan(context);
                if (plan == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // "Remove all other you in play" names its bodies; nothing is asked.
                return plan.Fixed
                    ? Commit(context, plan.Candidates)
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor,
                        plan.Candidates.Select(body => EffectValue.Space(body.Coordinate)),
                        plan.Minimum, plan.Maximum));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                // Re-derived rather than trusted, as the spawn is: the bodies standing when the answer
                // arrives decide, and each named space must hold one of them.
                var plan = Plan(context);
                if (plan == null || plan.Fixed || selected == null ||
                    selected.Count < plan.Minimum || selected.Count > plan.Maximum ||
                    selected.Any(value => value.Kind != EffectValueKind.Coordinate))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var chosen = selected
                    .Select(value => plan.Candidates.FirstOrDefault(body =>
                        body.Coordinate.Equals(value.Coordinate)))
                    .ToArray();
                if (chosen.Any(body => !body.HeroId.IsDefined) ||
                    chosen.Select(body => body.Body).Distinct().Count() != chosen.Length)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return Commit(context, chosen);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var plan = Plan(context);
                return plan == null
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : plan.Fixed
                        ? EffectExecutionResultKind.Applied
                        : EffectExecutionResultKind.NeedsChoice;
            }

            private static EffectExecutionResult Commit(
                EffectCapabilityContext context, IReadOnlyList<HeroOccupancy> bodies) =>
                context.Components.Units.CommitValidatedHeroBodyRemoval(
                    context.ActorId, bodies.Select(body => body.Body).ToArray(), out var removed)
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(RemovedOutput, EffectValue.Integer(removed.Count))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();

            /// <summary>
            /// The bodies the removal may take and how many, from the printed words the input names -
            /// the one derivation Execute, Resume and Anticipate ask. Null when nothing can be removed:
            /// no body of a many-bodied actor stands, "except the last one" with one body standing, or
            /// "all other you" with no other body (RZ-Q7: a mandatory step with nothing to act on).
            /// </summary>
            private static RemovalPlan Plan(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive || !components.CanSpawnUnits ||
                    !context.Inputs.TryGetValue(RemovalParameterName, out var input) ||
                    input.Kind != EffectValueKind.Integer)
                {
                    return null;
                }

                var bodies = components.Units.GetHeroBodies(context.ActorId);
                if (bodies.Count + components.Units.FindHeroBodySupply(context.ActorId).Count < 2)
                {
                    // A hero declaring one body has no bodies to remove: he leaves only by p.13.
                    return null;
                }

                switch ((ActorBodyRemoval)input.IntegerValue)
                {
                    case ActorBodyRemoval.OneBody:
                        return bodies.Count == 0 ? null : new RemovalPlan(bodies, 1, 1, false);
                    case ActorBodyRemoval.OneOrMoreButNotTheLast:
                        return bodies.Count < 2 ? null : new RemovalPlan(bodies, 1, bodies.Count - 1, false);
                    case ActorBodyRemoval.OneOrMoreIncludingAll:
                        return bodies.Count == 0 ? null : new RemovalPlan(bodies, 1, bodies.Count, false);
                    case ActorBodyRemoval.EveryOtherBody:
                    {
                        // "Other" is every body but the one performing the action (RZ-Q4).
                        if (!components.Tokens.TryGetHeroCoordinate(context.ActorId, out var acting))
                        {
                            return null;
                        }

                        var others = bodies.Where(body => !body.Coordinate.Equals(acting)).ToArray();
                        return others.Length == 0
                            ? null
                            : new RemovalPlan(others, others.Length, others.Length, true);
                    }
                    default:
                        return null;
                }
            }

            private sealed class RemovalPlan
            {
                public RemovalPlan(IReadOnlyList<HeroOccupancy> candidates, int minimum, int maximum, bool isFixed)
                {
                    Candidates = candidates;
                    Minimum = minimum;
                    Maximum = maximum;
                    Fixed = isFixed;
                }

                public IReadOnlyList<HeroOccupancy> Candidates { get; }
                public int Minimum { get; }
                public int Maximum { get; }
                public bool Fixed { get; }
            }
        }

        private sealed class ActorHasSeveralBodiesPredicate : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            public ActorHasSeveralBodiesPredicate(EffectCapabilityReference reference)
            {
                Reference = reference;
            }

            public EffectCapabilityReference Reference { get; }

            /// <summary>A predicate reads the board and changes none of it (milestone 9.6).</summary>
            public bool PublishesWithoutApplying => true;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding(SeveralBodiesOutput, EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                Execute(context).Outcomes;

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(SeveralBodiesOutput, EffectValue.Boolean(HasSeveral(context)))
                });

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            // A false gate is an answer, not an impossibility (RULING 53: a false printed "If" is a
            // legal do-nothing).
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;

            private static bool HasSeveral(EffectCapabilityContext context)
            {
                var components = context.Components;
                return components != null && components.IsLive && components.CanSpawnUnits &&
                    components.Units.GetHeroBodies(context.ActorId).Count > 1;
            }
        }

        private sealed class SpawnActorBodyInRadiusHandler : IEffectCapabilityHandler
        {
            public SpawnActorBodyInRadiusHandler(EffectCapabilityReference reference)
            {
                Reference = reference;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                null, new[] { new EffectOutputBinding(SpawnedOutput, EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = LegalSpaces(context).Select(EffectValue.Space).ToArray();
                return candidates.Length == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(
                        new EffectChoiceRequest(EffectChooserRole.Actor, candidates, 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived rather than trusted: a space legal when the question opened may have been
                // taken before the answer arrived.
                var destination = selected[0].Coordinate;
                if (!LegalSpaces(context).Contains(destination) ||
                    !context.Components.Units.CommitValidatedHeroBodySpawn(
                        context.ActorId, destination, out var spawned))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(SpawnedOutput, EffectValue.Space(spawned.Coordinate))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                LegalSpaces(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// The empty spaces within the printed Radius (with items) of the acting body, or none when
            /// no body is left in the supply. The one derivation Execute, Resume and Anticipate ask.
            /// </summary>
            private static IReadOnlyList<HexCoordinate> LegalSpaces(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive || !components.CanSpawnUnits ||
                    components.Units.FindHeroBodySupply(context.ActorId).Count == 0 ||
                    !components.Tokens.TryGetHeroCoordinate(context.ActorId, out var actingBody))
                {
                    return Array.Empty<HexCoordinate>();
                }

                var radius = RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications).Total;
                if (radius < 1)
                {
                    return Array.Empty<HexCoordinate>();
                }

                return PlaceDestinationQuery.FindDestinations(
                    new PlacePolicy(
                        PlaceSourceContainer.Supply,
                        PlaceEligiblePieceKind.GamePiece,
                        PlaceDestinationScope.EmptySpacesWithinRadius,
                        radius,
                        PlaceChooserKind.ActingHero,
                        PlaceNoDestinationPolicy.Impossible,
                        PlaceDestinationAnchor.ActingHero),
                    components.Tokens,
                    actingBody,
                    Array.Empty<HexCoordinate>(),
                    context.Modifications);
            }
        }
    }
}
