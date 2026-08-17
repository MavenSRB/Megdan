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
        /// Revision 2, minted at milestone 6.8k: the subject parameter widened from a typed hero to a
        /// typed coordinate. A revision rather than a second capability because the rule did not change
        /// - the same direct Move now merely names any displaceable subject - and the registration list
        /// records contracts, so the widened schema must be visible there for a restore to compare.
        /// </summary>
        public static EffectCapabilityReference DirectMoveV2 =>
            new EffectCapabilityReference(DirectMove, 2);

        /// <summary>
        /// Revision 2, minted at milestone 6.4c: revision 1's declaration plus the eligible piece kind.
        ///
        /// Both Push declarations move together because both resolve through one routine and one pair
        /// of target finders, so a parameter added to only the named one would leave the batch reading
        /// a declaration it does not have.
        /// </summary>
        public static EffectCapabilityReference ObjectPushV2 =>
            new EffectCapabilityReference(ObjectPush, 2);

        /// <summary>Revision 2, minted at milestone 6.4c, for the same reason the batch's is.</summary>
        public static EffectCapabilityReference PushTargetV2 =>
            new EffectCapabilityReference(PushTarget, 2);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new DirectMoveHandler(),
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
        /// a real subject: `move a unit towards you` is this capability with `towardActor` set, not a
        /// separate Pull keyword, exactly as `BOARD_CAPABILITIES.md` requires. A definition fills the
        /// parameter from a prior target-selection node's published space, never from a role - the frame
        /// resolves roles without board access, so no role can supply a coordinate, and the definition
        /// validator refuses one that tries at content load.
        /// </summary>
        private sealed class DirectMoveHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => DirectMoveV2;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter("mover", EffectValueKind.Coordinate),
                    new EffectCapabilityParameter("distance", EffectValueKind.Integer),
                    new EffectCapabilityParameter("towardActor", EffectValueKind.Boolean)
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
            private static IReadOnlyList<HexCoordinate> Candidates(EffectCapabilityContext context)
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

                var admitted = AdmittedDistances.ForDeclared(context.FailurePolicy, distance);
                IEnumerable<HexCoordinate> travelled = components.Board
                    .FindDirectMoveDestinations(mover, distance, context.Modifications)
                    .Where(destination => admitted.Admits(destination.Cost))
                    .Select(destination => destination.Destination);

                if (context.Input("towardActor").BooleanValue)
                {
                    // p.18's `Closer (Space)`, measured from the acting hero. A move `towards you`
                    // admits only spaces closer to the actor than the mover's own starting space.
                    if (!components.Board.TryGetObjectCoordinate(
                            BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                    {
                        return Array.Empty<HexCoordinate>();
                    }

                    travelled = CloserQuery.FilterCloserTo(actorAt, moverAt, travelled);
                }

                // Standing still is admitted by the wording, not by the geometry, so it is added to
                // whatever the geometry admits rather than filtered through it. The reachability walk
                // beneath never emits the mover's own space - a Movement action's destination costs at
                // least one point - and `Closer` would refuse it as well, since no space is closer to
                // the actor than itself. Both are right about travel and neither is being asked about
                // travel: this is p.10's decision not to move.
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
            private static bool TryResolveMover(
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
                return !unit.IsDefined || !ImmunityQuery.IsImmuneTo(
                    unit,
                    RuleActivity.Skill,
                    RuleObject.ForHero(context.ActorId),
                    components.Board,
                    context.Participants,
                    context.Modifications);
            }
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
            public EffectCapabilityReference Reference => ObjectPushV2;

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
            public EffectCapabilityReference Reference => PushTargetV2;

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
            parameters.Add(new EffectCapabilityParameter("ignoresObstacles", EffectValueKind.Boolean));

            // Milestone 6.4c. Both declarations carry it, because both produce their target set
            // through the finders below and the reading lives there rather than per handler. The
            // spelling is milestone 6.6a's own constant rather than a second one with the same
            // literal: a card author declaring an eligible kind for a Push and for a Remove must not
            // be able to get one of them wrong.
            parameters.Add(new EffectCapabilityParameter(
                SyntheticExchangeCapabilities.EligiblePieceParameterName, EffectValueKind.Integer));
            return new EffectCapabilityParameterSchema(
                parameters,
                new[]
                {
                    new EffectOutputBinding("distance", EffectValueKind.Integer),
                    new EffectOutputBinding("moved", EffectValueKind.Integer)
                });
        }

        private static EffectExecutionResult ExecutePush(
            EffectCapabilityContext context, PushTargetFinder finder)
        {
            var distance = context.Input("distance").IntegerValue;
            if (distance < 0 || !finder(context, out _, out _))
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            if (context.FailurePolicy != EffectFailurePolicy.UpTo)
            {
                return Push(context, distance, finder);
            }

            // "If a card allows you to push multiple targets up to X spaces, you choose the
            // distance", and which distances that offers is the one optional-distance reading rather
            // than arithmetic written here - the same statement direct Move consults, which is what
            // stops the two from answering differently again.
            var candidates = AdmittedDistances.ForDeclared(context.FailurePolicy, distance)
                .Enumerate()
                .Select(EffectValue.Integer)
                .ToArray();
            return EffectExecutionResult.NeedsChoice(
                new EffectChoiceRequest(EffectChooserRole.Actor, candidates, 1, 1));
        }

        /// <summary>
        /// Whether a push would do anything, asked without pushing. Both declarations need it and both
        /// need it for the same reason: under any policy but `up to` the execution above resolves and
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
            if (context.Input("distance").IntegerValue < 0 || !finder(context, out _, out _))
            {
                return EffectExecutionResultKind.ImpossibleMandatory;
            }

            return context.FailurePolicy == EffectFailurePolicy.UpTo
                ? EffectExecutionResultKind.NeedsChoice
                : EffectExecutionResultKind.Applied;
        }

        private static EffectExecutionResult ResumePush(
            EffectCapabilityContext context,
            IReadOnlyList<EffectValue> selected,
            PushTargetFinder finder)
        {
            if (selected == null || selected.Count != 1 ||
                selected[0].Kind != EffectValueKind.Integer)
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            var chosen = selected[0].IntegerValue;
            if (chosen < 0 || chosen > context.Input("distance").IntegerValue)
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            return Push(context, chosen, finder);
        }

        /// <summary>
        /// One chosen distance applied to every target at once: each is resolved against the state as
        /// it stands before anything moves, and only then is anything committed. p.18's geometry lives
        /// here alone, whichever declaration produced the target set.
        /// </summary>
        private static EffectExecutionResult Push(
            EffectCapabilityContext context, int distance, PushTargetFinder finder)
        {
            if (!finder(context, out var pusherAt, out var targets))
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

            // Phase two: commit farthest-from-the-pusher first. Every target travels directly away
            // along its own file, so working outwards means a space is always vacated before
            // anything is asked to enter it, and the batch never refuses itself.
            var moved = 0;
            foreach (var entry in resolved
                .OrderByDescending(entry => entry.Value.ActualDistance)
                .ThenBy(entry => entry.Value.Origin.Q)
                .ThenBy(entry => entry.Value.Origin.R))
            {
                if (!entry.Value.Moves)
                {
                    // p.18's zero-space push: a legal result that relocates nothing.
                    continue;
                }

                var committed = board.CommitValidatedDisplacement(entry.Key, entry.Value.Endpoint);
                if (committed.Accepted)
                {
                    moved++;
                }
            }

            return EffectExecutionResult.Applied(new[]
            {
                new EffectOutcomeFact("distance", EffectValue.Integer(distance)),
                new EffectOutcomeFact("moved", EffectValue.Integer(moved))
            });
        }

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
                    !IsImmuneToThisPush(context, occupant, pushingUnit))
                {
                    found.Add(occupant);
                }
            }

            targets = found;
            return found.Count > 0;
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
