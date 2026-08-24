using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// The bounded synthetic capability set milestone 6.2a executes its frame through.
    ///
    /// Deliberately none of these mutates the board. Milestone 6.2a's non-goals bar Repeat, Clear,
    /// direct Move, Object Push, Place, Spawn, Swap, Replace, and Remove, which is every board mutation
    /// the roadmap has defined so far, so a board-mutating fixture here would either violate a non-goal
    /// or pre-empt milestone 6.2. These exercise the execution frame - the four handler results, typed
    /// output binding, chooser ownership, and declared checkpoints - not new game rules.
    ///
    /// It lives in production Core rather than a test assembly for the same reason
    /// <c>SyntheticShowcaseHand</c> and <c>CardRetrievalHandler</c> do: a configured session composes
    /// it, so it must be reachable from the composition root.
    /// </summary>
    public static class SyntheticEffectCapabilities
    {
        public static EffectCapabilityId NoteActor { get; } = new EffectCapabilityId("synthetic-note-actor");
        public static EffectCapabilityId ChooseHero { get; } = new EffectCapabilityId("synthetic-choose-hero");
        public static EffectCapabilityId IsFlagged { get; } = new EffectCapabilityId("synthetic-is-flagged");
        public static EffectCapabilityId Impossible { get; } = new EffectCapabilityId("synthetic-impossible");
        public static EffectCapabilityId Declined { get; } = new EffectCapabilityId("synthetic-declined");
        public static EffectCapabilityId AcceptHero { get; } = new EffectCapabilityId("synthetic-accept-hero");
        public static EffectCapabilityId MarkActor { get; } = new EffectCapabilityId("synthetic-mark-actor");

        /// <summary>
        /// The Active effect <see cref="MarkActor"/> creates. Catalog-owned semantics: an instance
        /// records only which hero controls it and when, and a restore rebinds to this object rather
        /// than rebuilding an equivalent one from the payload.
        /// </summary>
        public static ActiveEffectDefinition MarkDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-mark-actor-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.ThisTurn,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            new[] { ActiveEffectCancellationCondition.SourceCardStateChanged },
            false,
            "SyntheticMark",
            "synthetic-mark-actor",
            new[] { TimingCheckpoint.ActionEnded });

        /// <summary>
        /// A durable Next Turn effect, declared so fixtures that need an effect surviving a turn or round
        /// boundary have one the catalog actually owns. Registered on the note-actor capability because
        /// every Active effect crossing a snapshot must be declared by some registered capability.
        /// </summary>
        public static ActiveEffectDefinition DurableDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-durable-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.NextTurn,
            TimingCheckpoint.ActionEnded,
            TimingCheckpoint.TurnStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            new[] { ActiveEffectCancellationCondition.Explicit },
            true,
            "SyntheticDurable",
            "synthetic-durable",
            Array.Empty<TimingCheckpoint>());

        public static EffectCapabilityReference NoteActorV1 => new EffectCapabilityReference(NoteActor, 1);
        public static EffectCapabilityReference ChooseHeroV1 => new EffectCapabilityReference(ChooseHero, 1);
        public static EffectCapabilityReference IsFlaggedV1 => new EffectCapabilityReference(IsFlagged, 1);
        public static EffectCapabilityReference ImpossibleV1 => new EffectCapabilityReference(Impossible, 1);
        public static EffectCapabilityReference DeclinedV1 => new EffectCapabilityReference(Declined, 1);
        public static EffectCapabilityReference AcceptHeroV1 => new EffectCapabilityReference(AcceptHero, 1);
        public static EffectCapabilityReference MarkActorV1 => new EffectCapabilityReference(MarkActor, 1);

        /// <summary>
        /// The build's one capability contract set. Milestone 6.2's component handlers join here rather
        /// than forming a second catalog: a match has one registered set, and the snapshot records it as
        /// one list to compare a restore against.
        ///
        /// Milestone 8.9's verified Active effects join it for that reason and not by exception. They
        /// are the first entries here that carry an official card's numbers, and a second catalog for
        /// them would mean two registration lists where the snapshot compares one.
        /// </summary>
        public static EffectCapabilityCatalog CreateCatalog() =>
            new EffectCapabilityCatalog(new IEffectCapabilityHandler[]
                {
                    new NoteActorHandler(),
                    new ChooseHeroHandler(),
                    new IsFlaggedHandler(),
                    new ImpossibleHandler(),
                    new DeclinedHandler(),
                    new AcceptHeroHandler(),
                    new MarkActorHandler()
                }
                .Concat(SyntheticComponentCapabilities.Handlers())
                .Concat(SyntheticModificationCapabilities.Handlers())
                .Concat(SyntheticDisplacementCapabilities.Handlers())
                .Concat(SyntheticTargetingCapabilities.Handlers())
                .Concat(SyntheticPlacementCapabilities.Handlers())
                .Concat(SyntheticExchangeCapabilities.Handlers())
                .Concat(SyntheticCardStateCapabilities.Handlers())
                .Concat(SyntheticConsequenceCapabilities.Handlers())
                .Concat(SyntheticContinuationCapabilities.Handlers())
                .Concat(SyntheticValueCapabilities.Handlers())
                .Concat(SyntheticPersistentSourceCapabilities.Handlers())
                .Concat(SyntheticPausingSourceCapabilities.Handlers())
                .Concat(GuardsDigital.Core.Cards.Verified.WaspActiveEffectContent.Handlers()));

        /// <summary>
        /// Creates one Active effect on the actor. This is the capability that gives acceptance check 5
        /// something to prove: the instance crosses the snapshot boundary carrying only runtime state,
        /// and restore rebinds its semantics to <see cref="MarkDefinition"/> from this catalog.
        /// </summary>
        private sealed class MarkActorHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => MarkActorV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("marked", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                new[] { MarkDefinition };

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (context.Timing == null || context.Participants == null)
                {
                    // Outside a live match there is no lifecycle to create into; the node is a legal
                    // no-op rather than a failure, which is what Applied means.
                    return EffectExecutionResult.Applied(
                        new[] { new EffectOutcomeFact("marked", EffectValue.Hero(context.ActorId)) });
                }

                var effectId = ActiveEffectInstanceIdentity.Mint(context, MarkDefinition);
                if (context.Timing.Effects.All(effect => !effect.Id.Equals(effectId)))
                {
                    context.Timing.Create(
                        effectId,
                        MarkDefinition,
                        context.ActorId,
                        context.Participants.GetTeamForHero(context.ActorId).Side,
                        default,
                        context.SourceCardInstanceId,
                        context.ActionDefinitionId,
                        Math.Max(context.RoundIndex, 1),
                        Math.Max(context.TurnIndex, 1));
                }

                return EffectExecutionResult.Applied(
                    new[] { new EffectOutcomeFact("marked", EffectValue.Hero(context.ActorId)) });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// Always applies: creating a mark that already exists is the lifecycle's own no-op, and a
            /// context without a lifecycle is a legal no-op too, so there is no state this could read
            /// that would change the answer.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        /// <summary>Applies unconditionally and publishes the actor, so a later node can bind it.</summary>
        private sealed class NoteActorHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => NoteActorV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("hero", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                new[] { DurableDefinition };

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied(
                    new[] { new EffectOutcomeFact("hero", EffectValue.Hero(context.ActorId)) },
                    checkpointsBefore: new[] { TimingCheckpoint.ActionStarted },
                    checkpointsAfter: new[] { TimingCheckpoint.ActionEnded });

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        /// <summary>Requests a chooser-owned selection between the actor and the controller.</summary>
        private sealed class ChooseHeroHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ChooseHeroV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);

                return candidates.Length == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(
                        new EffectChoiceRequest(EffectChooserRole.Actor, candidates, 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(
                    new[] { new EffectOutcomeFact("chosen", selected[0]) });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Length == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// The heroes this selection offers. Extracted so the probe reads the same derivation the
            /// execution does rather than restating the condition beside it.
            /// </summary>
            private static EffectValue[] Candidates(EffectCapabilityContext context) =>
                new[] { context.ActorId, context.ControllerId }
                    .Where(hero => hero.IsDefined)
                    .Distinct()
                    .OrderBy(hero => hero.Value, StringComparer.Ordinal)
                    .Select(EffectValue.Hero)
                    .ToArray();
        }

        /// <summary>A pure predicate over one Boolean input, for Conditional branching.</summary>
        private sealed class IsFlaggedHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => IsFlaggedV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter("flag", EffectValueKind.Boolean) },
                new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        "value", EffectValue.Boolean(context.Input("flag").BooleanValue))
                });

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>A pure predicate always applies; what it publishes is the branch's business.</summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        /// <summary>
        /// Consumes a Hero the frame supplies, so typed result binding has something to bind into: an
        /// earlier node's published output flows here without a closure or an expression language.
        /// </summary>
        private sealed class AcceptHeroHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => AcceptHeroV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter("hero", EffectValueKind.Hero) },
                new[] { new EffectOutputBinding("received", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var hero = context.Input("hero");
                return hero.IsDefined
                    ? EffectExecutionResult.Applied(
                        new[] { new EffectOutcomeFact("received", hero) })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context.Input("hero").IsDefined
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>Never possible, so the four failure policies can each be observed distinctly.</summary>
        private sealed class ImpossibleHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ImpossibleV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema();

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>
        /// Declines of its own accord. Distinct from <see cref="ImpossibleHandler"/>: this one could
        /// have applied and chose not to, which is not the same rules fact as being unable to.
        /// </summary>
        private sealed class DeclinedHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => DeclinedV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema();

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.DeclinedOptional();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// Declining applies nothing, and unlike an impossible step it stops nothing either, so an
            /// enclosing option is carried by whatever stands after this node rather than by it.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.DeclinedOptional;
        }
    }
}
