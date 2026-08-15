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

        public static EffectCapabilityReference DirectMoveV1 =>
            new EffectCapabilityReference(DirectMove, 1);

        public static EffectCapabilityReference ObjectPushV1 =>
            new EffectCapabilityReference(ObjectPush, 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new DirectMoveHandler(),
                new ObjectPushHandler()
            };

        /// <summary>
        /// Rulebook p.11's movement written inside card text: "This movement follows the same rules and
        /// restrictions as a Movement action, except: it cannot be used to Fast Travel; effects which
        /// stop you from performing a Movement action do not prevent movement in the card text."
        ///
        /// It moves a hero. p.11 says card text may move "another unit or a token", and Object Push below
        /// carries that whole breadth because p.18 names both; a direct Move of a minion or a token would
        /// additionally need reachability for a non-hero mover, which the Movement action's own query does
        /// not express, and inventing one here would be a second traversal for the two to disagree
        /// through. Milestone 6.4's acceptance names units and tokens for Push and does not for Move.
        ///
        /// The mover is a bound parameter rather than always the actor, so the source-relative policy has
        /// a real subject: `move a unit towards you` is this capability with `towardActor` set, not a
        /// separate Pull keyword, exactly as `BOARD_CAPABILITIES.md` requires.
        /// </summary>
        private sealed class DirectMoveHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => DirectMoveV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter("mover", EffectValueKind.Hero),
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
            /// The destinations this move may use, from the Movement action's own reachability plus the
            /// node's distance policy and the source-relative policy. Nothing here walks the board.
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

                var reachable = components.Board.FindDirectMoveDestinations(
                    mover, distance, context.Modifications);

                // `up to` makes the distance itself optional, so every shorter destination is legal.
                // Every other policy means the exact number of spaces (p.11).
                var admitted = context.FailurePolicy == EffectFailurePolicy.UpTo
                    ? reachable
                    : reachable.Where(destination => destination.Cost == distance).ToArray();

                var spaces = admitted
                    .Select(destination => destination.Destination)
                    .OrderBy(coordinate => coordinate.Q)
                    .ThenBy(coordinate => coordinate.R)
                    .ToArray();

                if (!context.Input("towardActor").BooleanValue)
                {
                    return spaces;
                }

                // p.18's `Closer (Space)`, measured from the acting hero. A move `towards you` admits
                // only spaces closer to the actor than the mover's own starting space.
                if (!components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return Array.Empty<HexCoordinate>();
                }

                return CloserQuery.FilterCloserTo(actorAt, moverAt, spaces);
            }

            /// <summary>
            /// The object this node moves, and where it stands.
            ///
            /// Still a typed hero. Milestone 6.4a widened the layers beneath this one - the board port
            /// and the reachability query both take any displaceable object now - but the capability's
            /// own subject cannot widen until a node can name a non-hero one. The frame resolves a role
            /// binding from the frame alone and has no board access, so there is no `actor's space`
            /// role to bind, and naming a minion or token needs the coordinate identity Object Push
            /// uses for its targets, which in turn needs a target-selection node this fixture has not
            /// got. Widening the parameter without that would give the definition no legal way to fill
            /// it, so it waits for the selection node rather than being half-done here.
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

                var heroId = context.Input("mover").HeroId;
                if (!heroId.IsDefined)
                {
                    return false;
                }

                mover = BoardObjectReference.ForHero(heroId);
                return components.Board.TryGetObjectCoordinate(mover, out moverAt);
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
            public EffectCapabilityReference Reference => ObjectPushV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter("distance", EffectValueKind.Integer),
                    new EffectCapabilityParameter("ignoresObstacles", EffectValueKind.Boolean)
                },
                new[]
                {
                    new EffectOutputBinding("distance", EffectValueKind.Integer),
                    new EffectOutputBinding("moved", EffectValueKind.Integer)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var distance = context.Input("distance").IntegerValue;
                if (distance < 0 || !TryFindTargets(context, out _, out _))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                if (context.FailurePolicy != EffectFailurePolicy.UpTo)
                {
                    return Push(context, distance);
                }

                // "If a card allows you to push multiple targets up to X spaces, you choose the
                // distance": zero is a legal choice, so the candidate range starts there.
                var candidates = Enumerable.Range(0, distance + 1)
                    .Select(EffectValue.Integer)
                    .ToArray();
                return EffectExecutionResult.NeedsChoice(
                    new EffectChoiceRequest(EffectChooserRole.Actor, candidates, 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
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

                return Push(context, chosen);
            }

            /// <summary>
            /// One chosen distance applied to every target at once: each is resolved against the state as
            /// it stands before anything moves, and only then is anything committed.
            /// </summary>
            private static EffectExecutionResult Push(EffectCapabilityContext context, int distance)
            {
                if (!TryFindTargets(context, out var pusherAt, out var targets))
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
            private static bool TryFindTargets(
                EffectCapabilityContext context,
                out HexCoordinate pusherAt,
                out IReadOnlyList<BoardObjectReference> targets)
            {
                pusherAt = default;
                targets = Array.Empty<BoardObjectReference>();

                var components = context.Components;
                if (components == null || !components.CanDisplace)
                {
                    return false;
                }

                var board = components.Board;
                var pusher = BoardObjectReference.ForHero(context.ActorId);
                if (!board.TryGetObjectCoordinate(pusher, out pusherAt))
                {
                    return false;
                }

                var pushingUnit = pusher.AsRuleObject();
                var found = new List<BoardObjectReference>();
                foreach (var neighbor in pusherAt.GetNeighbors()
                    .OrderBy(coordinate => coordinate.Q)
                    .ThenBy(coordinate => coordinate.R))
                {
                    if (!board.TryGetObjectAt(neighbor, out var occupant))
                    {
                        continue;
                    }

                    var asRuleObject = occupant.AsRuleObject();
                    if (asRuleObject.IsDefined &&
                        context.Modifications.IsImmuneTo(
                            asRuleObject, RuleActivity.ObjectPush, pushingUnit))
                    {
                        continue;
                    }

                    found.Add(occupant);
                }

                targets = found;
                return found.Count > 0;
            }
        }
    }
}
