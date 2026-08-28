using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 6.4's two displacement capabilities: rulebook p.11's direct Move and p.18's Object Push.
    ///
    /// They are two capability IDs rather than one relocation handler because the rulebook gives them
    /// different rules and <c>CARD_EFFECT_CAPABILITIES.md</c> forbids treating Movement, direct Move, and
    /// Object Push as interchangeable. Neither emits a movement fact, neither can become Fast Travel, and
    /// neither requests a Lane Push: p.18 states outright that "displacing a minion miniature (by moving,
    /// pushing, or swapping it) has no impact on the Minion Battle".
    ///
    /// Both take their optionality from the node's own <see cref="EffectFailurePolicy"/> rather than a
    /// parameter of their own. That is p.11's rule expressed in milestone 6.2a's existing vocabulary:
    /// "unless the distance is optional ('up to 2 spaces'), or the move itself is optional ('you may
    /// move'), you must move the exact number of spaces, or stop performing the action at that step".
    /// A mandatory node therefore means exactly that distance, and an `up to` node makes the distance the
    /// decision - which is the same distinction the four failure policies already carry.
    ///
    /// Milestone 8.8 adds the one thing that distinction could not carry. Wasp's Kinetic Repulse prints
    /// "Push up to 2 enemy units adjacent to you 3 spaces": the `up to` is over the units and the spaces
    /// are exact, and a node has one policy. So the Push declarations now say the count and the distance
    /// separately - a declared maximum number of targets beside the declared distance, and a printed
    /// least distance beside it that <see cref="AdmittedDistances"/> reads - and the node's policy keeps
    /// its p.10 meaning over the step as a whole. Kinetic Blast's "3 or 4 spaces" is the first distance
    /// a card writes as a choice rather than as a number.
    ///
    /// Registered into the one shared catalog for the reason milestone 6.2 records: a match has one
    /// capability contract set, and the snapshot records it as one list.
    /// </summary>
    public static class SyntheticDisplacementCapabilities
    {
        public static EffectCapabilityId DirectMove { get; } =
            new EffectCapabilityId("synthetic-direct-move");

        public static EffectCapabilityId ObjectPush { get; } =
            new EffectCapabilityId("synthetic-object-push");

        /// <summary>
        /// Milestone 6.5a. p.18's Push with the target the definition names rather than the adjacency
        /// set <see cref="ObjectPush"/> derives.
        ///
        /// A second capability ID rather than a second revision, because the two are not versions of
        /// one declaration - both are live, and a card that pushes "multiple targets up to X spaces"
        /// and a card that pushes one named unit are different declarations of the same rule. It is
        /// not a second implementation of that rule either: the geometry, the immunity filter, the
        /// one-pre-effect-state resolution, the farthest-first commit, and p.18's zero-space result all
        /// live in the shared routine below, and these two handlers differ only in how their target set
        /// is produced. <c>CARD_EFFECT_CAPABILITIES.md</c> names this move for exactly this case: when
        /// existing parameters cannot express an effect, add a capability rather than overload a term.
        /// They could not - a coordinate cannot say "every adjacent object", and a boolean beside it
        /// would leave one parameter meaningless, which the definition validator refuses as ambiguous.
        /// </summary>
        public static EffectCapabilityId PushTarget { get; } =
            new EffectCapabilityId("synthetic-push-target");

        /// <summary>
        /// Revision 3, minted at milestone 8.6: revision 2's declaration with the `towardActor` boolean
        /// replaced by a declared <see cref="DirectMoveDestinationPolicy"/>.
        ///
        /// Revision 2 - milestone 6.8k's, which widened the subject from a typed hero to a typed
        /// coordinate - is retired rather than kept beside it, exactly as 6.8k retired revision 1 when
        /// it widened that subject. The registration list records contracts, and two live revisions of
        /// one declaration would let a definition ask the same capability a question it can no longer
        /// answer the same way: a boolean cannot express Wasp's orbit, and a policy that also carried
        /// the boolean could state two narrowings at once.
        /// </summary>
        public static EffectCapabilityReference DirectMoveV3 =>
            new EffectCapabilityReference(DirectMove, 3);

        /// <summary>
        /// The destination-policy parameter's name, spelled once so a definition cannot mistype it and
        /// so the enum it carries has exactly one reading site.
        /// </summary>
        public const string DestinationPolicyParameterName = "destinationPolicy";

        /// <summary>
        /// Milestone 8.21. The one space
        /// <see cref="DirectMoveDestinationPolicy.NamedSpace"/> moves into, bound from the node that
        /// published it.
        ///
        /// <para>Optional, and defaulting to nothing, so every declaration written before this
        /// milestone keeps its exact candidate set - the same shape milestone 8.20 gave
        /// <c>excludeTarget</c>. A policy that names a space and binds none admits nothing, which is
        /// what an unresolved binding means everywhere else in this catalog.</para>
        /// </summary>
        public const string NamedDestinationParameterName = "destination";

        /// <summary>
        /// Milestone 8.22. The disk
        /// <see cref="DirectMoveDestinationPolicy.WithinActorRadius"/> keeps the destination inside,
        /// written down by the definition rather than read off the card's icon.
        ///
        /// <para>Optional and defaulting to nothing, exactly as
        /// <see cref="NamedDestinationParameterName"/> is, so every declaration written before this
        /// milestone keeps its exact candidate set. It is a separate parameter from
        /// <c>distance</c> because the two bound different things and a card can print them
        /// differently: Sabina's Path to Victory moves a minion <em>one</em> space and keeps it within
        /// <em>three</em>, so one number would have to be both the step and the leash.</para>
        /// </summary>
        public const string ActorRadiusParameterName = "actorRadius";

        /// <summary>
        /// Milestone 8.23. Whether this Move crosses obstacles on its way - Blink Strike's "Move 2
        /// spaces in a straight line <em>through</em> an enemy unit", and Nimble Strike's same
        /// sentence about a hero.
        ///
        /// <para>Optional and defaulting to absent, exactly as
        /// <see cref="NamedDestinationParameterName"/> and <see cref="ActorRadiusParameterName"/>
        /// are, so every declaration written before this milestone keeps its exact candidate set: a
        /// card that says nothing about crossing is a card whose mover is stopped by p.18's
        /// obstacles.</para>
        ///
        /// <para>It is declared on the Move rather than granted to the hero, which is the whole
        /// distinction p.18's <c>Move Through</c> entry draws. A granted
        /// <see cref="RulePermissionKind.MoveThroughObstacles"/> is a property of a unit and lasts
        /// while its source does; this is a property of one printed movement, so nothing about
        /// Tigerclaw changes and the next space he moves into is closed to him again. p.18's Push
        /// has read the same pair since milestone 6.4 - see the <c>ignoresObstacles</c> parameter
        /// beside it - and the two are answered by one transit rule rather than by two.</para>
        ///
        /// <para>It reaches transit alone. Where a Move may END is p.18's other sentence, which no
        /// declaration is offered: a crossing Move still may not stop on an obstacle, which is why
        /// Blink Strike's line needs the space beyond the crossed unit empty before it exists at
        /// all.</para>
        /// </summary>
        public const string MovesThroughObstaclesParameterName = "movesThroughObstacles";

        /// <summary>
        /// Revision 3, minted at milestone 8.8: revision 2's declaration plus the two things Wasp's
        /// kinetic Skills print that a node's failure policy could not say - the fewest spaces the card
        /// names, and how many units it may push.
        ///
        /// Revision 2 - milestone 6.4c's, which added the eligible piece kind - is retired rather than
        /// kept beside it, for the reason milestone 6.8k and milestone 8.6 retired the revisions they
        /// replaced: the registration list records contracts, and a definition bound to a declaration
        /// that cannot state its count bound would be pushing everything standing there while its
        /// printed text says two.
        ///
        /// Both Push declarations move to revision 3 together because both resolve through one routine
        /// and one pair of target finders, so a parameter added to only the named one would leave the
        /// batch reading a declaration it does not have. Only the shared reading moves to both: the
        /// count bound and the target relation are the batch's alone, because the named push is handed
        /// one object by a selection node that has already answered both questions.
        /// </summary>
        public static EffectCapabilityReference ObjectPushV3 =>
            new EffectCapabilityReference(ObjectPush, 3);

        /// <summary>Revision 3, minted at milestone 8.8, for the same reason the batch's is.</summary>
        public static EffectCapabilityReference PushTargetV3 =>
            new EffectCapabilityReference(PushTarget, 3);

        /// <summary>
        /// The printed-least-distance parameter's name, spelled once so a definition cannot mistype it.
        /// A card naming one distance declares the same number here; Kinetic Blast's "3 or 4 spaces"
        /// declares 3 beside a distance of 4. Direct Move also reads the optional floor when a card's
        /// destination clause makes standing still incapable of satisfying its printed instruction.
        /// </summary>
        public const string LeastDistanceParameterName = "leastDistance";

        /// <summary>
        /// The count bound's parameter name: the most units the card lets one push take.
        ///
        /// Zero is the named absence, and it is the reading every Push declaration had before milestone
        /// 8.8 - p.18's batch takes every object adjacent to the pusher, and a card that says so names
        /// no number for this to carry. A card that does name one, as both of Wasp's kinetic Skills do,
        /// makes which units are pushed a decision, because two of the three standing there is not a
        /// set the rule can pick on the player's behalf.
        /// </summary>
        public const string MaximumTargetsParameterName = "maximumTargets";

        /// <summary>
        /// The declared side of the board the batch may push, carrying a
        /// <see cref="SourceTeamRelation"/>. Wasp's kinetic Skills print "enemy units"; a card that
        /// names no side declares <see cref="SourceTeamRelation.AnyTeam"/>, which is p.18's own breadth.
        /// </summary>
        public const string TargetRelationParameterName = "targetRelation";

        /// <summary>
        /// Milestone 8.8's stop-cause facts, in the order the batch commits. Each pushed hero gets a
        /// pair - whether an obstacle stopped it, and which hero it was - so a rider hanging off the
        /// push can ask a <see cref="EffectNodeKind.Conditional"/> about one hero at a time and hand the
        /// same hero to whatever the card makes them do.
        ///
        /// <para>Two pairs, because the verified text that reads them pushes up to two units. The batch
        /// itself can stop more, and publishes stop causes only for as many heroes as it has names for;
        /// a card that pushes more and prints a rider for all of them needs a third name declared here,
        /// which is visible at content load - a definition can only bind an output the schema declares -
        /// rather than silently dropped. The pair is heroes only because the fact is a
        /// <see cref="EffectValueKind.Hero"/> and because a minion has no hand to answer a rider with;
        /// p.19's "stopped by an obstacle" is the same event for one, and stays unpublished until a card
        /// prints something a minion can be told to do.</para>
        /// </summary>
        public const string FirstStoppedHeroOutputName = "firstStoppedHero";

        /// <summary>Whether an obstacle stopped the first hero this push moved. See <see cref="FirstStoppedHeroOutputName"/>.</summary>
        public const string FirstHeroWasStoppedOutputName = "firstHeroWasStopped";

        /// <summary>The second stopped hero, in commit order. See <see cref="FirstStoppedHeroOutputName"/>.</summary>
        public const string SecondStoppedHeroOutputName = "secondStoppedHero";

        /// <summary>Whether an obstacle stopped the second hero this push moved.</summary>
        public const string SecondHeroWasStoppedOutputName = "secondHeroWasStopped";

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new DirectMoveHandler(DirectMoveV3, supportsImmunityOverrides: true),
                new ObjectPushHandler(),
                new PushTargetHandler()
            };

        /// <summary>
        /// Rulebook p.11's movement written inside card text: "This movement follows the same rules and
        /// restrictions as a Movement action, except: it cannot be used to Fast Travel; effects which
        /// stop you from performing a Movement action do not prevent movement in the card text."
        ///
        /// It moves any displaceable subject. p.11 says card text may move "another unit or a token",
        /// and since milestone 6.8k the subject parameter is a typed coordinate naming whatever stands
        /// there - a hero, a minion, or a token - the same space-as-identity Object Push below already
        /// uses for its targets, which one-obstacle-per-space makes unambiguous and which keeps a token
        /// expressible without widening the typed value union. The reachability beneath is unchanged:
        /// the port has served every kind since milestone 6.4a widened it.
        ///
        /// The mover is a bound parameter rather than always the actor, so the source-relative policy has
        /// a real subject: `move a unit towards you` is this capability under
        /// <see cref="DirectMoveDestinationPolicy.TowardActor"/>, not a separate Pull keyword, exactly
        /// as `BOARD_CAPABILITIES.md` requires. A definition fills the
        /// parameter from a prior target-selection node's published space, never from a role - the frame
        /// resolves roles without board access, so no role can supply a coordinate, and the definition
        /// validator refuses one that tries at content load.
        /// </summary>
        private sealed class DirectMoveHandler :
            IEffectCapabilityHandler, IEffectImmunityOverrideCapabilityHandler
        {
            private readonly bool supportsImmunityOverrides;

            public DirectMoveHandler(
                EffectCapabilityReference reference,
                bool supportsImmunityOverrides)
            {
                Reference = reference;
                this.supportsImmunityOverrides = supportsImmunityOverrides;
            }

            public EffectCapabilityReference Reference { get; }

            public IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                supportsImmunityOverrides
                    ? DirectMoveActivity
                    : Array.Empty<RuleActivity>();

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter("mover", EffectValueKind.Coordinate),
                    new EffectCapabilityParameter("distance", EffectValueKind.Integer),
                    new EffectCapabilityParameter(
                        DestinationPolicyParameterName,
                        EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.AllExcept(
                            DirectMoveDestinationPolicy.None)),
                    new EffectCapabilityParameter(
                        NamedDestinationParameterName, EffectValueKind.Coordinate, isRequired: false),
                    new EffectCapabilityParameter(
                        ActorRadiusParameterName, EffectValueKind.Integer, isRequired: false),
                    new EffectCapabilityParameter(
                        MovesThroughObstaclesParameterName,
                        EffectValueKind.Boolean,
                        isRequired: false),
                    new EffectCapabilityParameter(
                        LeastDistanceParameterName, EffectValueKind.Integer, isRequired: false)
                },
                new[] { new EffectOutputBinding("moved", EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                if (candidates.Count == 0)
                {
                    // p.11: a mandatory exact distance that cannot be walked stops the action at that
                    // step. Under `up to` or `if able` the enclosing node's policy turns this into a
                    // skip rather than a stop, which is why the handler reports the same thing either
                    // way and the node decides what it means.
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Milestone 8.21. A declaration that names its destination has nothing to ask, so it
                // moves rather than opening a prompt with one answer in it - milestone 6.5a's reading
                // of "yourself", applied to where rather than to who. The commit below is the same one
                // the answered path reaches; only the question is missing, because the card asks none.
                if ((DirectMoveDestinationPolicy)context.Input(DestinationPolicyParameterName)
                        .IntegerValue == DirectMoveDestinationPolicy.NamedSpace)
                {
                    return Resume(context, new[] { EffectValue.Space(candidates[0]) });
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    candidates.Select(candidate => EffectValue.Space(candidate)).ToArray(),
                    1,
                    1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived against current state rather than trusted from the paused choice: a space
                // legal when the choice opened may have been taken before the answer arrived.
                var destination = selected[0].Coordinate;
                if (!Candidates(context).Contains(destination))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                if (!TryResolveMover(context, out var mover, out _))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var committed = context.Components.Board.CommitValidatedDisplacement(mover, destination);
                if (!committed.Accepted)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("moved", EffectValue.Space(committed.Destination))
                });
            }

            /// <summary>
            /// The same reachability question <see cref="Execute"/> asks, without opening the choice.
            /// An unbound subject leaves the mover unresolvable and the node impossible, which is what
            /// the enclosing walk reads as "not decidable here" when the space comes from a later node.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : (DirectMoveDestinationPolicy)context.Input(DestinationPolicyParameterName)
                        .IntegerValue == DirectMoveDestinationPolicy.NamedSpace
                        ? EffectExecutionResultKind.Applied
                        : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// The destinations this move may use, from the Movement action's own reachability plus the
            /// node's distance policy and the source-relative policy. Nothing here walks the board.
            ///
            /// How much of the declared distance is optional is <see cref="AdmittedDistances"/>'s
            /// answer rather than this handler's, which is milestone 6.5ad's correction: reading p.10
            /// and p.11 here as well let this capability and Object Push disagree about whether `up to
            /// X spaces` admits no spaces at all, and they did.
            /// </summary>
            private IReadOnlyList<HexCoordinate> Candidates(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.CanDisplace)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var distance = context.Input("distance").IntegerValue;
                if (!TryResolveMover(context, out var mover, out var moverAt) || distance < 0)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var destinationPolicy =
                    (DirectMoveDestinationPolicy)context.Input(DestinationPolicyParameterName).IntegerValue;
                if (!Enum.IsDefined(typeof(DirectMoveDestinationPolicy), destinationPolicy) ||
                    destinationPolicy == DirectMoveDestinationPolicy.None)
                {
                    return Array.Empty<HexCoordinate>();
                }

                // p.18's `Closer (Space)`, measured from the acting hero, is what both narrowing
                // policies compare against, so an actor who is not on the board can satisfy neither.
                // The unrestricted reading needs no such position and is therefore not refused for
                // lacking one.
                var actorAt = default(HexCoordinate);
                if (DirectMoveDestinations.NeedsActorPosition(destinationPolicy) &&
                    !components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out actorAt))
                {
                    return Array.Empty<HexCoordinate>();
                }

                // Milestone 8.21. A named destination is what the sentence pins, so the declared
                // number stops being the exact count the mover walks and becomes the budget it may
                // spend reaching that one space. Brogan's charges are why: the charge line has already
                // chosen a destination two, three or four spaces away under one declaration, and a
                // reading that admitted only the largest of those would refuse the shorter charges the
                // same card offers.
                var namesASpace = destinationPolicy == DirectMoveDestinationPolicy.NamedSpace;
                var leastDistanceInput = context.Input(LeastDistanceParameterName);
                var leastDistance = leastDistanceInput.Kind == EffectValueKind.Integer
                    ? leastDistanceInput.IntegerValue
                    : 0;
                if (leastDistance < 0 || leastDistance > distance)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var admitted = AdmittedDistances.ForDeclared(
                    namesASpace ? EffectFailurePolicy.UpTo : context.FailurePolicy,
                    distance,
                    leastDistance);
                var namedDestination = context.Input(NamedDestinationParameterName);
                var actorRadius = context.Input(ActorRadiusParameterName);

                // Milestone 8.23. An unbound declaration is the absence of the sentence, and the
                // absence of "through" is p.18's ordinary movement - so a card that never mentions
                // crossing reaches the same walk it always did.
                var crossing = context.Input(MovesThroughObstaclesParameterName);
                var movesThroughObstacles =
                    crossing.Kind == EffectValueKind.Boolean && crossing.BooleanValue;
                IEnumerable<HexCoordinate> travelled = DirectMoveDestinations.Admit(
                    destinationPolicy,
                    actorAt,
                    moverAt,
                    components.Board
                        .FindDirectMoveDestinations(
                            mover, distance, context.Modifications, movesThroughObstacles)
                        .Where(destination => admitted.Admits(destination.Cost))
                        .Select(destination => destination.Destination),
                    namedDestination.Kind == EffectValueKind.Coordinate
                        ? namedDestination.Coordinate
                        : (HexCoordinate?)null,
                    actorRadius.Kind == EffectValueKind.Integer
                        ? actorRadius.IntegerValue
                        : (int?)null);

                // Standing still is p.10's decision not to move, and a sentence that names where to
                // move offers no such decision: the named space is the only thing that satisfies it.
                if (namesASpace)
                {
                    return travelled
                        .OrderBy(coordinate => coordinate.Q)
                        .ThenBy(coordinate => coordinate.R)
                        .ToArray();
                }


                // Standing still is admitted by the wording, not by the geometry, so it is added to
                // whatever the geometry admits rather than filtered through it. The reachability walk
                // beneath never emits the mover's own space - a Movement action's destination costs at
                // least one point - and the destination policy is not asked about it either. Both are
                // right about travel and neither is being asked about travel: this is p.10's decision
                // not to move.
                var spaces = admitted.AdmitsStandingStill
                    ? travelled.Concat(new[] { moverAt })
                    : travelled;

                return spaces
                    .OrderBy(coordinate => coordinate.Q)
                    .ThenBy(coordinate => coordinate.R)
                    .ToArray();
            }

            /// <summary>
            /// The object this node moves, and where it stands.
            ///
            /// The subject is the object standing at the bound coordinate, resolved through the board
            /// port the way Object Push resolves its targets. An empty space names nothing and the node
            /// reports itself impossible, which is what a stale selection becomes by the time it is
            /// consumed here: the space is re-read, not remembered.
            /// </summary>
            private bool TryResolveMover(
                EffectCapabilityContext context,
                out BoardObjectReference mover,
                out HexCoordinate moverAt)
            {
                mover = default;
                moverAt = default;

                var components = context.Components;
                if (components == null || !components.CanDisplace)
                {
                    return false;
                }

                var subject = context.Input("mover");
                if (subject.Kind != EffectValueKind.Coordinate)
                {
                    return false;
                }

                moverAt = subject.Coordinate;
                if (!components.Board.TryGetObjectAt(moverAt, out mover))
                {
                    return false;
                }

                var unit = mover.AsRuleObject();
                if (!unit.IsDefined)
                {
                    return true;
                }

                var overrides = supportsImmunityOverrides
                    ? context.ImmunityOverrides
                    : Array.Empty<ImmunityOverrideDeclaration>();
                var source = RuleObject.ForHero(context.ActorId);
                return !ImmunityQuery.IsImmuneTo(
                        unit,
                        context.EnclosingActivity,
                        source,
                        components.Board,
                        context.Participants,
                        context.Modifications,
                        overrides,
                        RuleActivity.DirectMove) &&
                    !ImmunityQuery.IsImmuneTo(
                        unit,
                        RuleActivity.DirectMove,
                        source,
                        components.Board,
                        context.Participants,
                        context.Modifications,
                        overrides,
                        RuleActivity.DirectMove);
            }

            private static IReadOnlyList<RuleActivity> DirectMoveActivity { get; } =
                Array.AsReadOnly(new[] { RuleActivity.DirectMove });
        }

        /// <summary>
        /// Rulebook p.18's Push: "When a unit or a token is pushed, move it in a straight line directly
        /// away from the pushing unit the number of spaces equal to the push distance."
        ///
        /// Its targets are every object adjacent to the acting hero. That bound is synthetic, not a rule:
        /// which objects a real effect may push comes from its card text through the reusable targeting
        /// contracts milestone 6.8 owns, and this milestone has no verified text to read. It is chosen
        /// because it makes both shapes one code path - one adjacent object is the single-target push, and
        /// several is p.18's multi-target batch - and because adjacency guarantees the straight line the
        /// rule needs, since every neighbour lies on one of the six files by definition.
        ///
        /// The one decision is the distance, which is why this is one node. Under `up to` the chooser
        /// picks it ("you choose the distance and then push all those units that many spaces at the same
        /// time"); under any other policy the bound distance is fixed and the node opens nothing. Every
        /// target is then resolved against one pre-effect state and committed in a single batch, which is
        /// the one-batch commit milestone 6.4 owns. It is discharged inside this capability rather than as
        /// a general `SimultaneousBatch` control node, following milestone 6.3's precedent of implementing
        /// Clear through the existing seam instead of generalising `ReplaceAction` before a verified card
        /// needs it.
        /// </summary>
        private sealed class ObjectPushHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ObjectPushV3;

            public EffectCapabilityParameterSchema Schema { get; } = PushSchema(named: false);

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                ExecutePush(context, TryFindAdjacentTargets);

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                ResumePush(context, selected, TryFindAdjacentTargets);

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                AnticipatePush(context, TryFindAdjacentTargets);
        }

        /// <summary>
        /// Milestone 6.5a's Push over the one object a definition names, for card text of the shape
        /// `choose another adjacent unit and Push it 3 spaces`.
        ///
        /// The subject arrives as a coordinate from an earlier target-selection node, the same
        /// space-as-identity direct Move and Place use. Which objects are offerable - adjacency,
        /// self-exclusion, immunity - is that node's declaration through the reusable targeting
        /// contract, so this capability states no target bound of its own; what it does state, and
        /// re-derives on every call, is that the named space still holds an object this actor may push.
        /// </summary>
        private sealed class PushTargetHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => PushTargetV3;

            public EffectCapabilityParameterSchema Schema { get; } = PushSchema(named: true);

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                ExecutePush(context, TryFindNamedTarget);

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                ResumePush(context, selected, TryFindNamedTarget);

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                AnticipatePush(context, TryFindNamedTarget);
        }

        /// <summary>How a Push handler produces its target set. The only thing the two differ in.</summary>
        private delegate bool PushTargetFinder(
            EffectCapabilityContext context,
            out HexCoordinate pusherAt,
            out IReadOnlyList<BoardObjectReference> targets);

        private static EffectCapabilityParameterSchema PushSchema(bool named)
        {
            var parameters = new List<EffectCapabilityParameter>();
            if (named)
            {
                parameters.Add(new EffectCapabilityParameter("target", EffectValueKind.Coordinate));
            }

            parameters.Add(new EffectCapabilityParameter("distance", EffectValueKind.Integer));

            // Milestone 8.8. The distance a card names when it names more than one - "3 or 4 spaces" -
            // stated as the floor of the same declaration rather than as a second distance parameter,
            // because there is one distance and the card writes its ends.
            parameters.Add(new EffectCapabilityParameter(
                LeastDistanceParameterName, EffectValueKind.Integer));
            parameters.Add(new EffectCapabilityParameter("ignoresObstacles", EffectValueKind.Boolean));

            // Milestone 6.4c. Both declarations carry it, because both produce their target set
            // through the finders below and the reading lives there rather than per handler. The
            // spelling is milestone 6.6a's own constant rather than a second one with the same
            // literal: a card author declaring an eligible kind for a Push and for a Remove must not
            // be able to get one of them wrong.
            parameters.Add(new EffectCapabilityParameter(
                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                EffectValueKind.Integer,
                integerDomain: EffectIntegerDomain.AllExcept(
                    PushEligiblePieceKind.None)));

            var outputs = new List<EffectOutputBinding>
            {
                new EffectOutputBinding("distance", EffectValueKind.Integer),
                new EffectOutputBinding("moved", EffectValueKind.Integer),
                new EffectOutputBinding(FirstStoppedHeroOutputName, EffectValueKind.Hero),
                new EffectOutputBinding(FirstHeroWasStoppedOutputName, EffectValueKind.Boolean)
            };

            if (!named)
            {
                // Milestone 8.8, and the batch's alone. Which side of the board a push may take and
                // how many of it are the two things a card says about a target set it does not name
                // one by one; the named push is handed its object by a selection node that has
                // already answered both, so a count bound there would be a parameter with one legal
                // value and a relation there would ask the same question twice.
                parameters.Add(new EffectCapabilityParameter(
                    TargetRelationParameterName,
                    EffectValueKind.Integer,
                    integerDomain: EffectIntegerDomain.AllExcept(
                        SourceTeamRelation.None)));
                parameters.Add(new EffectCapabilityParameter(
                    MaximumTargetsParameterName, EffectValueKind.Integer));

                // The second stop-cause pair, for the same reason: one named target can stop one hero.
                outputs.Add(new EffectOutputBinding(SecondStoppedHeroOutputName, EffectValueKind.Hero));
                outputs.Add(new EffectOutputBinding(
                    SecondHeroWasStoppedOutputName, EffectValueKind.Boolean));
            }

            return new EffectCapabilityParameterSchema(parameters, outputs);
        }

        /// <summary>
        /// The spaces this declaration admits, read through the one statement of pages 10 and 11 that
        /// every capability moving something a stated number of spaces consults.
        /// </summary>
        private static AdmittedDistances Distances(EffectCapabilityContext context) =>
            AdmittedDistances.ForDeclared(
                context.FailurePolicy,
                context.Input("distance").IntegerValue,
                context.Input(LeastDistanceParameterName).IntegerValue);

        /// <summary>
        /// The most units this declaration lets one push take, or 0 where it names no number - a batch
        /// whose card says every adjacent object, and the named push, whose definition already named
        /// its one object and declares no such parameter at all.
        /// </summary>
        private static int MaximumTargets(EffectCapabilityContext context) =>
            context.Input(MaximumTargetsParameterName).IntegerValue;

        /// <summary>
        /// Whether the declared distance is one this capability can read at all. Asked in both entry
        /// points and stated once, because a probe that admitted a declaration the execution refuses
        /// is the disagreement milestone 6.6a found in another handler.
        /// </summary>
        private static bool DeclaresALegalDistance(EffectCapabilityContext context)
        {
            var declared = context.Input("distance").IntegerValue;
            var least = context.Input(LeastDistanceParameterName).IntegerValue;
            return declared >= 0 && least >= 0 && least <= declared;
        }

        /// <summary>
        /// Whether which of the admitted objects to push is the chooser's decision, which is exactly
        /// whether the card named a maximum at all.
        ///
        /// A card that names one has said something the rule cannot answer for the player: "up to 2
        /// enemy units adjacent to you" leaves which two, and - under p.10's non-mandatory wording -
        /// whether to push two, one or none. A card that names none is p.18's plain batch, where every
        /// object standing beside the pusher is pushed and there is nothing to ask.
        /// </summary>
        private static bool CountIsADecision(EffectCapabilityContext context) =>
            MaximumTargets(context) > 0;

        private static EffectExecutionResult ExecutePush(
            EffectCapabilityContext context, PushTargetFinder finder)
        {
            if (!DeclaresALegalDistance(context) || !finder(context, out _, out var admitted))
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            // A push that has already been told which units - a resolution restored or replayed
            // between its two questions - is past question one, and asking it again would reopen a
            // decision the frame records as made. The answers decide which question this is, exactly
            // as they do in Resume, which is what lets snapshot restore re-ask this handler what it
            // is offering and get the question the pause was written on.
            if (TryFindTargetsNamedEarlier(context, admitted, out var namedEarlier))
            {
                return PushOrAskDistance(context, namedEarlier);
            }

            // Question one: which units. p.18 settles the distance over "all those units", so the set
            // is named before the distance is asked, and the card's own sentence reads in that order
            // too - "Push up to 2 enemy units adjacent to you 3 or 4 spaces".
            if (CountIsADecision(context))
            {
                return OfferTargets(context, admitted);
            }

            return PushOrAskDistance(context, admitted);
        }

        /// <summary>
        /// Whether a push would do anything, asked without pushing. Both declarations need it and both
        /// need it for the same reason: with nothing left to decide the execution above resolves and
        /// commits the batch in the same call, so this is the one shape where probing by executing
        /// would move every adjacent object across the board.
        ///
        /// It stops at the target question, which is the whole of what "can this be done" means here.
        /// p.18 admits a zero-space push as a legal result, so a target that cannot travel is still a
        /// target and this reports the push performable - the rule that a blocked push resolves to
        /// zero is the same rule whether or not anyone asked first.
        /// </summary>
        private static EffectExecutionResultKind AnticipatePush(
            EffectCapabilityContext context, PushTargetFinder finder)
        {
            if (!DeclaresALegalDistance(context) || !finder(context, out _, out _))
            {
                return EffectExecutionResultKind.ImpossibleMandatory;
            }

            return CountIsADecision(context) || !Distances(context).NamesOneDistance
                ? EffectExecutionResultKind.NeedsChoice
                : EffectExecutionResultKind.Applied;
        }

        /// <summary>
        /// One answer, routed by what it answers.
        ///
        /// A push asks at most two questions and they carry different kinds, so nothing has to be
        /// remembered about which one is open: coordinates name the units, an integer names the
        /// distance. What the earlier answer said is read off the frame through
        /// <see cref="EffectCapabilityContext.AnswersAlreadyGiven"/>, and re-derived against live state
        /// before it is used, because an object named when the choice opened may have moved, been
        /// removed, or become immune since.
        /// </summary>
        private static EffectExecutionResult ResumePush(
            EffectCapabilityContext context,
            IReadOnlyList<EffectValue> selected,
            PushTargetFinder finder)
        {
            if (selected == null)
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            if (selected.Count == 0)
            {
                // Confirming an `up to` selection with nothing in it is the count the card allows -
                // "up to 2" includes none - so the step was offered and not taken rather than failed.
                return EffectExecutionResult.DeclinedOptional();
            }

            if (selected.All(value => value.Kind == EffectValueKind.Coordinate))
            {
                if (!finder(context, out _, out var admitted))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var chosen = NamedTargets(context, selected, admitted);
                if (chosen.Count != selected.Count)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return PushOrAskDistance(context, chosen);
            }

            if (selected.Count != 1 || selected[0].Kind != EffectValueKind.Integer)
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            var distance = selected[0].IntegerValue;
            if (!Distances(context).Admits(distance) || !finder(context, out _, out var stillAdmitted))
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            TryFindTargetsNamedEarlier(context, stillAdmitted, out var targets);
            return Push(context, distance, targets);
        }

        /// <summary>
        /// Whether an earlier answer to this node already named the units, and which of the admitted
        /// ones they are. False where no such answer exists, which is the unbounded batch p.18
        /// describes and every declaration before milestone 8.8.
        /// </summary>
        private static bool TryFindTargetsNamedEarlier(
            EffectCapabilityContext context,
            IReadOnlyList<BoardObjectReference> admitted,
            out IReadOnlyList<BoardObjectReference> named)
        {
            for (var index = context.AnswersAlreadyGiven.Count - 1; index >= 0; index--)
            {
                var answer = context.AnswersAlreadyGiven[index];
                if (answer.Count > 0 && answer.All(value => value.Kind == EffectValueKind.Coordinate))
                {
                    named = NamedTargets(context, answer, admitted);
                    return true;
                }
            }

            named = admitted;
            return false;
        }

        /// <summary>
        /// The admitted objects standing where these coordinates point, in the admitted set's own
        /// order. An answer naming a space that no longer holds an admitted object simply drops out,
        /// which is what makes the count check at the call site meaningful.
        /// </summary>
        private static IReadOnlyList<BoardObjectReference> NamedTargets(
            EffectCapabilityContext context,
            IReadOnlyList<EffectValue> named,
            IReadOnlyList<BoardObjectReference> admitted)
        {
            var board = context.Components.Board;
            var chosen = new List<BoardObjectReference>();
            foreach (var target in admitted)
            {
                if (board.TryGetObjectCoordinate(target, out var standing) &&
                    named.Any(value =>
                        value.Kind == EffectValueKind.Coordinate &&
                        value.Coordinate.Equals(standing)))
                {
                    chosen.Add(target);
                }
            }

            return chosen;
        }

        /// <summary>
        /// Where the admitted objects stand right now. A push names its targets by space, the identity
        /// milestone 6.4 chose for displacement, so this is what the chooser is offered.
        /// </summary>
        private static IReadOnlyList<HexCoordinate> AdmittedCoordinates(
            EffectCapabilityContext context, IReadOnlyList<BoardObjectReference> admitted)
        {
            var board = context.Components.Board;
            var coordinates = new List<HexCoordinate>();
            foreach (var target in admitted)
            {
                if (board.TryGetObjectCoordinate(target, out var standing))
                {
                    coordinates.Add(standing);
                }
            }

            return coordinates;
        }

        private static EffectExecutionResult OfferTargets(
            EffectCapabilityContext context, IReadOnlyList<BoardObjectReference> admitted)
        {
            var candidates = AdmittedCoordinates(context, admitted)
                .Select(EffectValue.Space)
                .ToArray();
            var maximum = Math.Min(MaximumTargets(context), candidates.Length);
            var minimum = context.FailurePolicy == EffectFailurePolicy.UpTo ? 0 : maximum;
            return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                EffectChooserRole.Actor, candidates, minimum, maximum));
        }

        /// <summary>
        /// Either the distance question or the push itself. p.18: "If a card allows you to push
        /// multiple targets up to X spaces, you choose the distance and then push all those units that
        /// many spaces at the same time" - so a declaration admitting more than one number asks, and
        /// one admitting a single number simply uses it.
        /// </summary>
        private static EffectExecutionResult PushOrAskDistance(
            EffectCapabilityContext context,
            IReadOnlyList<BoardObjectReference> targets)
        {
            var admitted = Distances(context);
            if (!admitted.NamesOneDistance)
            {
                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    admitted.Enumerate().Select(EffectValue.Integer).ToArray(),
                    1,
                    1));
            }

            return Push(context, admitted.Maximum, targets);
        }

        /// <summary>
        /// One chosen distance applied to every target at once: each is resolved against the state as
        /// it stands before anything moves, and only then is anything committed. p.18's geometry lives
        /// here alone, whichever declaration produced the target set.
        /// </summary>
        private static EffectExecutionResult Push(
            EffectCapabilityContext context,
            int distance,
            IReadOnlyList<BoardObjectReference> targets)
        {
            if (!TryFindPusher(context, out var pusherAt, out _))
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            var ignoresObstacles = context.Input("ignoresObstacles").BooleanValue;
            var board = context.Components.Board;

            // Phase one: resolve every path against one pre-effect state. Nothing is committed
            // here, so no target's outcome can depend on another target having already moved -
            // which is what p.18's "at the same time" forbids.
            var resolved = new List<KeyValuePair<BoardObjectReference, PushPathResult>>();
            foreach (var target in targets)
            {
                if (!board.TryGetObjectCoordinate(target, out var targetAt))
                {
                    continue;
                }

                var path = board.ResolvePush(
                    pusherAt, targetAt, distance, target.AsRuleObject(), ignoresObstacles,
                    context.Modifications);
                if (path.HasDirection)
                {
                    resolved.Add(new KeyValuePair<BoardObjectReference, PushPathResult>(target, path));
                }
            }

            // Phase two: read the stop causes, in the order the targets themselves are in.
            //
            // Deliberately not the commit order below. That order is p.18's geometry protecting itself
            // from its own batch; this one is what a card's rider reads, and "the first hero this push
            // stopped" should mean the first of the units the card named rather than whichever of them
            // happened to travel furthest. A hero pushed zero spaces by an obstacle standing right
            // behind it is counted here, because p.18 calls that a push.
            var stopped = resolved
                .Where(entry =>
                    entry.Key.Kind == BoardObjectKind.Hero && entry.Value.StoppedByAnObstacle)
                .Select(entry => entry.Key.HeroId)
                .ToList();

            // Phase three: commit farthest-from-the-pusher first. Every target travels directly away
            // along its own file, so working outwards means a space is always vacated before
            // anything is asked to enter it, and the batch never refuses itself.
            var moved = 0;

            // Milestone 8.19c1. What a Push applied to, in the order it was committed. p.18's "if you
            // push" is the displacement rather than the attempt, so this list is built from accepted
            // commits alone: a target the geometry left standing where it was is not in it, and a push
            // that moved nothing produces an empty list and reaches no after-push trigger.
            var displaced = new List<RuleObject>();
            foreach (var entry in resolved
                .Where(entry => entry.Value.Moves)
                .OrderByDescending(entry => entry.Value.ActualDistance)
                .ThenBy(entry => entry.Value.Origin.Q)
                .ThenBy(entry => entry.Value.Origin.R))
            {
                var committed = board.CommitValidatedDisplacement(entry.Key, entry.Value.Endpoint);
                if (committed.Accepted)
                {
                    moved++;
                    var subject = entry.Key.AsRuleObject();
                    if (subject.IsDefined)
                    {
                        displaced.Add(subject);
                    }
                }
            }

            return EffectExecutionResult.Applied(
                StopCauseFacts(context, distance, moved, stopped), appliedPushes: displaced);
        }

        /// <summary>
        /// What one push publishes: how far it pushed, how many it moved, and milestone 8.8's stop
        /// causes, one pair per hero this declaration has a name for.
        ///
        /// The boolean of a pair is always published and the hero only when there is one, because
        /// <see cref="EffectValue.Hero"/> refuses an undefined hero and a fact naming nobody would be a
        /// worse answer than no fact: a <see cref="EffectNodeKind.Conditional"/> reading the boolean
        /// gets false either way, and the branch behind it is what would have consumed the hero.
        /// </summary>
        private static IReadOnlyList<EffectOutcomeFact> StopCauseFacts(
            EffectCapabilityContext context, int distance, int moved, IReadOnlyList<HeroId> stopped)
        {
            var facts = new List<EffectOutcomeFact>
            {
                new EffectOutcomeFact("distance", EffectValue.Integer(distance)),
                new EffectOutcomeFact("moved", EffectValue.Integer(moved)),
                new EffectOutcomeFact(
                    FirstHeroWasStoppedOutputName, EffectValue.Boolean(stopped.Count > 0))
            };

            if (stopped.Count > 0)
            {
                facts.Add(new EffectOutcomeFact(
                    FirstStoppedHeroOutputName, EffectValue.Hero(stopped[0])));
            }

            if (!PublishesASecondStopCause(context))
            {
                return facts;
            }

            facts.Add(new EffectOutcomeFact(
                SecondHeroWasStoppedOutputName, EffectValue.Boolean(stopped.Count > 1)));
            if (stopped.Count > 1)
            {
                facts.Add(new EffectOutcomeFact(
                    SecondStoppedHeroOutputName, EffectValue.Hero(stopped[1])));
            }

            return facts;
        }

        /// <summary>
        /// Whether this declaration is the batch. Read off the parameters it was handed rather than
        /// passed down through the routine, because the count bound is exactly what makes a push able
        /// to stop a second hero, and the two would otherwise be two statements of one fact.
        /// </summary>
        private static bool PublishesASecondStopCause(EffectCapabilityContext context) =>
            context.Inputs.ContainsKey(MaximumTargetsParameterName);

        /// <summary>
        /// The acting hero's space and every object it may push out of the six adjacent spaces.
        ///
        /// Immunity filters here rather than later, because p.18 makes it a targeting rule: "If a
        /// unit is immune, you cannot target that unit". The same page keeps presence separate -
        /// "effects that check for unit presence will count immune units" - so a presence query over
        /// these spaces would still see what this list drops. A token is never filtered: p.19 states
        /// tokens are not units, so no immunity is addressed to one.
        /// </summary>
        private static bool TryFindAdjacentTargets(
            EffectCapabilityContext context,
            out HexCoordinate pusherAt,
            out IReadOnlyList<BoardObjectReference> targets)
        {
            targets = Array.Empty<BoardObjectReference>();
            if (!TryFindPusher(context, out pusherAt, out var pushingUnit))
            {
                return false;
            }

            var board = context.Components.Board;
            var found = new List<BoardObjectReference>();
            foreach (var neighbor in pusherAt.GetNeighbors()
                .OrderBy(coordinate => coordinate.Q)
                .ThenBy(coordinate => coordinate.R))
            {
                if (board.TryGetObjectAt(neighbor, out var occupant) &&
                    AdmitsThisPiece(context, occupant) &&
                    StandsOnTheDeclaredSide(context, occupant) &&
                    !IsImmuneToThisPush(context, occupant, pushingUnit))
                {
                    found.Add(occupant);
                }
            }

            targets = found;
            return found.Count > 0;
        }

        /// <summary>
        /// Milestone 8.8: whether the declared side of the board admits this object - "enemy units" for
        /// Wasp's two kinetic Skills, and everything for a card that names no side.
        ///
        /// It filters here, beside the piece kind and the immunity question, because p.19 makes it the
        /// same kind of question: what the card's own words let this action target. The reading itself
        /// is <see cref="TargetQuery.StandsInRelationTo"/>'s, so the selection a player is offered and
        /// the operation that follows cannot part company over what "enemy" means.
        /// </summary>
        private static bool StandsOnTheDeclaredSide(
            EffectCapabilityContext context, BoardObjectReference occupant)
        {
            var declared = context.Input(TargetRelationParameterName);
            if (declared.Kind != EffectValueKind.Integer)
            {
                // The named push declares no relation at all: its one object came from a selection
                // node that already asked whatever its card says about whose unit it is.
                return true;
            }

            var relation = (SourceTeamRelation)declared.IntegerValue;
            if (!Enum.IsDefined(typeof(SourceTeamRelation), relation) ||
                relation == SourceTeamRelation.None)
            {
                return false;
            }

            return TargetQuery.StandsInRelationTo(
                relation, occupant, context.ActorId, context.Participants, context.Components.Board);
        }

        /// <summary>
        /// The acting hero's space and the one object standing where the definition points.
        ///
        /// The bound space is re-read rather than remembered: a target chosen when the selection opened
        /// may have moved, been removed, or become immune before this node runs, and an empty space
        /// names nothing at all. Immunity is asked here for the same p.18 reason as above, so a target
        /// selected under one activity's rules cannot slip past this one's.
        /// </summary>
        private static bool TryFindNamedTarget(
            EffectCapabilityContext context,
            out HexCoordinate pusherAt,
            out IReadOnlyList<BoardObjectReference> targets)
        {
            targets = Array.Empty<BoardObjectReference>();
            if (!TryFindPusher(context, out pusherAt, out var pushingUnit))
            {
                return false;
            }

            var named = context.Input("target");
            if (named.Kind != EffectValueKind.Coordinate || named.Coordinate.Equals(pusherAt) ||
                !context.Components.Board.TryGetObjectAt(named.Coordinate, out var occupant) ||
                !AdmitsThisPiece(context, occupant) ||
                IsImmuneToThisPush(context, occupant, pushingUnit))
            {
                return false;
            }

            targets = new[] { occupant };
            return true;
        }

        /// <summary>The pushing unit and where it stands - p.18 measures every direction from it.</summary>
        private static bool TryFindPusher(
            EffectCapabilityContext context, out HexCoordinate pusherAt, out RuleObject pushingUnit)
        {
            pusherAt = default;
            pushingUnit = RuleObject.None;

            var components = context.Components;
            if (components == null || !components.CanDisplace)
            {
                return false;
            }

            var pusher = BoardObjectReference.ForHero(context.ActorId);
            if (!components.Board.TryGetObjectCoordinate(pusher, out pusherAt))
            {
                return false;
            }

            pushingUnit = pusher.AsRuleObject();
            return true;
        }

        /// <summary>
        /// Milestone 6.4c: whether the declared eligible kind admits this piece, asked in the one place
        /// both finders share.
        ///
        /// p.18 pushes "a unit or a token", so the breadth is the rule's and the narrowing is the
        /// card's. Reading it here rather than in each handler is what makes the probe and the
        /// operation the same answer: <c>Anticipate</c>, <c>Execute</c>, and <c>Resume</c> all reach a
        /// finder, so neither can admit a piece the other refuses, and milestone 6.8l turns that one
        /// answer into the offer a player is shown.
        /// </summary>
        private static bool AdmitsThisPiece(
            EffectCapabilityContext context, BoardObjectReference occupant) =>
            PushEligibility.Admits(
                (PushEligiblePieceKind)context
                    .Input(SyntheticExchangeCapabilities.EligiblePieceParameterName).IntegerValue,
                occupant.Kind);

        private static bool IsImmuneToThisPush(
            EffectCapabilityContext context, BoardObjectReference occupant, RuleObject pushingUnit)
        {
            var asRuleObject = occupant.AsRuleObject();
            return asRuleObject.IsDefined && ImmunityQuery.IsImmuneTo(
                asRuleObject,
                RuleActivity.ObjectPush,
                pushingUnit,
                context.Components.Board,
                context.Participants,
                context.Modifications);
        }
    }
}
