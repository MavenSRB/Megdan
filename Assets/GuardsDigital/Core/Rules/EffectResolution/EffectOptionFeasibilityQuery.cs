using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Milestone 6.5ac: which of a control node's options the acting player cannot perform.
    ///
    /// A `Choose` used to offer every child unconditionally, so a mode that could do nothing was
    /// selectable, and choosing it ran the branch to a mandatory stop that spent the card, ended the
    /// turn, changed nothing on the board, and explained nothing. This is the question that refuses it,
    /// and it is asked of the branch's own declared contract - the nodes it orders and the capabilities
    /// they name - so every definition of the same shape is judged by this one walk rather than by
    /// anything about a card, a hero, or a node's name.
    ///
    /// Nothing here executes. A capability handler may apply rather than pause - marking an actor,
    /// creating an Active effect, returning a marker, pushing every adjacent object at a fixed distance
    /// - so a probe that called <see cref="IEffectCapabilityHandler.Execute"/> would perform the very
    /// effect it was asked about. Each handler answers
    /// <see cref="IEffectCapabilityHandler.Anticipate"/> instead, from the candidate derivation it
    /// already owns.
    /// </summary>
    public static class EffectOptionFeasibilityQuery
    {
        /// <summary>
        /// Bounds one walk. A definition that cycles is refused at content load, so exceeding this
        /// means the walker itself is wrong; failing loudly beats hanging a match.
        /// </summary>
        private const int MaximumNodesPerOption = 1024;

        /// <summary>
        /// What a walk proves about one node or branch. The three are distinct because the enclosing
        /// sequence treats them differently: only <see cref="Blocked"/> means nothing after this can
        /// run, and only <see cref="Performable"/> means the option is worth <em>taking</em>.
        ///
        /// <para>Worth taking and worth offering stopped being the same question at milestone
        /// 8.35z35. <see cref="FindUnperformableOptions"/> refuses <see cref="Blocked"/> alone,
        /// because a mode that can be taken and would change nothing is a legal answer - p.12's Hold on
        /// the actor's own modal, and the escape hatch on one the card hands to an opponent. The other
        /// two callers still ask "worth taking": <see cref="CanApplyAnything"/> will not offer an action
        /// that can do nothing at all, and <see cref="CanPerformOptionalBranch"/> will not open a
        /// confirm-or-decline whose two answers leave the same board. Those are questions about whether
        /// a prompt should exist; this one is about which answers a prompt that exists may carry.</para>
        /// </summary>
        private enum Verdict
        {
            /// <summary>Applies nothing here, and stops nothing either: execution carries on.</summary>
            Empty = 0,

            /// <summary>Something here can apply, pause, or publish.</summary>
            Performable = 1,

            /// <summary>A mandatory step cannot be taken, so the resolution stops at it.</summary>
            Blocked = 2
        }

        private enum BindingVerdict
        {
            Continue = 0,
            Bound = 1,
            Blocked = 2
        }

        /// <summary>
        /// The options of <paramref name="chooseNode"/> the chooser cannot perform, in the definition's
        /// own order. Empty when every option can be taken, and never larger than the option set.
        ///
        /// Feasibility is a reading of the state as it stands, never a stored fact: this is asked again
        /// at every pause, on snapshot restore, on replay, and on retained-prefix Undo, so a mode that
        /// becomes performable inside one frame becomes selectable and one that stops being performable
        /// stops being offered.
        ///
        /// <para><b>Milestone 8.35z35 - the two-tier rule, and it is a rules decision rather than
        /// a presentation one.</b> Only <see cref="Verdict.Blocked"/> is refused here. Since milestone
        /// 6.5ac this asked for <see cref="Verdict.Performable"/> and greyed everything else, which put
        /// two different rules facts behind one grey: a mode that <em>cannot be taken</em> - a mandatory
        /// step of it stops the resolution - and a mode that <em>can be taken and would apply
        /// nothing</em>. The designer's 2026-08-19 ruling separates them, and the separation matters
        /// because the second one is a move. "An enemy hero in range chooses one" hands the choice to
        /// the opponent, and the bullet that does nothing to them is the escape hatch that decision
        /// exists to offer; on the actor's own modal it is p.12's Hold, which the page says a player can
        /// always take. Greying it deleted a legal answer from the board.</para>
        ///
        /// <para>Nothing about the refused tier moves. A mandatory step that cannot be taken still ends
        /// its branch, so the option still carries no way to select it, still says which standing rule
        /// refused it (milestone 8.35l), and a node whose every option is blocked still reaches
        /// milestone 6.5ae's stop. What changes is that a branch the walk proves merely <em>empty</em>
        /// is now offered and choosing it completes the node applying nothing - which is what the
        /// resolver already did with every empty step inside such a branch, so the answer and the offer
        /// finally read the state the same way.</para>
        /// </summary>
        public static IReadOnlyList<EffectChoiceCandidate> FindUnperformableOptions(
            EffectNodeDefinition chooseNode,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            if (chooseNode == null) throw new ArgumentNullException(nameof(chooseNode));
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var completed = outcomes ?? Array.Empty<EffectNodeOutcome>();
            var refused = new List<EffectChoiceCandidate>();
            foreach (var option in chooseNode.ChildNodeIds)
            {
                // Milestone 8.40f. A repeat offered as one BULLET of a modal is the one shape where
                // the walk's own rule leaves a real question unanswered. Milestone 6.7's
                // <see cref="Verdict.Empty"/> for a repeat is exactly right when the walk is judging
                // the ACTION the repeat would re-run - "calling it performable would let an action
                // that can do nothing be offered on the strength of being able to do nothing twice" -
                // and says nothing about a bullet. We're Not Done Yet! prints "may repeat once OR
                // gain 2 coins", so choosing the repeat SPENDS the answer: a bullet whose second
                // instance cannot run has to be greyed rather than taken and then skipped in
                // silence, which is 8.25e's "an offer is an offer" read one node higher.
                //
                // It is asked here and nowhere else, of a Choose node's own child, so the
                // whole-action walk below is untouched and every repeat written before this one
                // keeps its exact verdict.
                if (context.Definition.TryGetNode(option, out var node) &&
                    node.Kind == EffectNodeKind.RepeatAction &&
                    (!RepeatedActionQuery.CanRepeat(frame, node) ||
                        context.RepeatIsUnavailableFor(node.RepeatTargetBinding)))
                {
                    refused.Add(EffectChoiceCandidate.Option(option));
                    continue;
                }

                var budget = MaximumNodesPerOption;
                if (Walk(option, frame, completed, context, ref budget) == Verdict.Blocked)
                {
                    refused.Add(EffectChoiceCandidate.Option(option));
                }
            }

            return refused;
        }

        /// <summary>
        /// Milestone 6.5af: whether a whole effect definition could apply anything at all, asked of
        /// the frame a start would begin from rather than of one it began.
        ///
        /// It is the same walk, the same verdicts, and the same <c>Anticipate</c> contract the option
        /// question uses - one root instead of a control node's children - so an action is judged by
        /// exactly the rule its modes are, and a later definition of any shape is covered without
        /// naming it. <c>false</c> is returned only where the walk can prove nothing applies; a branch
        /// it cannot read stays performable, so this refuses an action it knows is empty and never one
        /// it merely cannot judge.
        /// </summary>
        public static bool CanApplyAnything(
            PendingResolution prospectiveFrame,
            EffectResolutionContext context) =>
            CanApplyAnything(prospectiveFrame, context, out _);

        /// <summary>
        /// Milestone 8.35l: the same answer, plus the standing modification that produced it where the
        /// walk met one.
        ///
        /// <para>The reason travels out with the verdict rather than being asked for afterwards,
        /// because asking afterwards means asking a second time and a second reading can differ from
        /// the first. What comes back is the applied modification the refusing capability itself named,
        /// and it is null wherever the action is empty for any other reason - a card whose text can
        /// bind no target is greyed exactly as before and says nothing new.</para>
        /// </summary>
        public static bool CanApplyAnything(
            PendingResolution prospectiveFrame,
            EffectResolutionContext context,
            out AppliedRuleModification blocking)
        {
            if (prospectiveFrame == null) throw new ArgumentNullException(nameof(prospectiveFrame));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var budget = MaximumNodesPerOption;
            var explanation = new RefusalExplanation();
            var verdict = Walk(
                context.Definition.RootNodeId,
                prospectiveFrame,
                Array.Empty<EffectNodeOutcome>(),
                context,
                ref budget,
                explanation);
            blocking = verdict == Verdict.Performable ? null : explanation.Blocking;
            return verdict == Verdict.Performable;
        }

        /// <summary>
        /// The first standing modification a walk met, kept so the offer can name it.
        ///
        /// <para>First rather than every one, and first in the walk's own order, which is execution
        /// order: what a player is shown is why the action stops where it stops, and a list of every
        /// rule that would also have refused it further along is not that. It is written once and never
        /// overwritten, so a later branch cannot rename the blocker the walk actually stopped at.</para>
        /// </summary>
        private sealed class RefusalExplanation
        {
            public AppliedRuleModification Blocking { get; private set; }

            public void RecordFirst(AppliedRuleModification blocking)
            {
                if (Blocking == null && blocking != null)
                {
                    Blocking = blocking;
                }
            }
        }

        /// <summary>
        /// Milestone 8.29e: whether the one branch an <see cref="EffectNodeKind.Optional"/> node
        /// governs can perform anything from the state as it stands.
        ///
        /// A `Choose` has asked this of each of its options since milestone 6.5ac, and an `Optional`
        /// asked it of nothing: it opened p.10's confirm-or-decline whatever the branch behind it
        /// could do, so "You may move another unit adjacent to the target" was a live prompt with no
        /// unit to move, and the only answer that changed anything was the one the player was going to
        /// give anyway. This is the same walk the option question uses, over the one child the node
        /// governs - <see cref="EffectNodeKind.Optional"/> in <see cref="Walk"/> already reads a
        /// declined branch as applying nothing, so the offer and the walk now read the node the same
        /// way rather than one of them deciding twice.
        ///
        /// It answers about the branch alone. Whether declining is free, whether the enclosing action
        /// continues, and what a skipped node records are the resolver's, and none of them change.
        /// </summary>
        public static bool CanPerformOptionalBranch(
            EffectNodeDefinition optionalNode,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            if (optionalNode == null) throw new ArgumentNullException(nameof(optionalNode));
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var budget = MaximumNodesPerOption;
            return Walk(
                optionalNode.ChildNodeIds[0],
                frame,
                outcomes ?? Array.Empty<EffectNodeOutcome>(),
                context,
                ref budget) == Verdict.Performable;
        }

        /// <summary>
        /// Whether a mandatory card-text path can reach the typed operation that binds an enclosing
        /// Attack's target. Unlike <see cref="CanApplyAnything"/>, an earlier optional or otherwise
        /// performable step is not sufficient: the walk continues until a mandatory target binding is
        /// reachable, or a mandatory prerequisite proves the path blocked. A modal choice succeeds
        /// when any legal branch can bind, matching the player's ability to choose that branch.
        /// </summary>
        public static bool CanCompleteMandatoryAttackTargetBinding(
            PendingResolution prospectiveFrame,
            EffectResolutionContext context) =>
            CanCompleteMandatoryAttackTargetBinding(prospectiveFrame, context, out _);

        /// <summary>
        /// Milestone 8.35z42: the same answer, plus the standing modification behind a refusal
        /// where the walk met one - the channel milestone 8.35l already opened for a Skill, on the other
        /// kind of offer it always applied to.
        ///
        /// <para><c>null</c> wherever the text is empty for any other reason, which is most of them: a
        /// board with nobody in reach is greyed exactly as before and says nothing new.</para>
        /// </summary>
        public static bool CanCompleteMandatoryAttackTargetBinding(
            PendingResolution prospectiveFrame,
            EffectResolutionContext context,
            out AppliedRuleModification blocking)
        {
            if (prospectiveFrame == null) throw new ArgumentNullException(nameof(prospectiveFrame));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var budget = MaximumNodesPerOption;
            var explanation = new RefusalExplanation();
            var verdict = WalkToAttackTargetBinding(
                context.Definition.RootNodeId,
                prospectiveFrame,
                Array.Empty<EffectNodeOutcome>(),
                context,
                ref budget,
                prospectiveActorSpaces: null,
                explanation);
            blocking = verdict == BindingVerdict.Bound ? null : explanation.Blocking;
            return verdict == BindingVerdict.Bound;
        }

        /// <param name="prospectiveActorSpaces">
        /// Milestone 8.35p, extended at milestone 8.35z42. Where the acting hero could be
        /// standing by the time this node is reached, or <c>null</c> while he is still standing where
        /// the board says he is.
        ///
        /// <para><b>Why the question exists at all.</b> Brogan's charges are the original case. "Move N
        /// spaces in a straight line to a space adjacent to an enemy unit, then target that unit" asks
        /// its halves in printed order - where he lands, the Move, then whom he takes - and the second
        /// question is an ordinary adjacency selection measured from where the hero is standing. This
        /// walk stands before all of it, so judging that selection from his starting square answers
        /// about the wrong space. It answered "nothing is adjacent" and the whole Attack was greyed in
        /// exactly the position the card exists for.</para>
        ///
        /// <para><b>Milestone 8.35p suppressed the reading; this milestone proves it instead.</b>
        /// Suppression is right whenever the destinations cannot be known, and it left a hole the
        /// reading-order corrective measured: Combat Reflexes and Leaping Strike became offerable with
        /// nothing at all within move-and-reach, because "a unit adjacent to you" was asked from his
        /// current space alone and then ignored. Rule 14 and milestone 8.35l say an action PROVEN empty
        /// is refused before it is offered, and RULING 45 says every destination of an unconstrained
        /// move stays offered while ANY completion exists - the player's rope covers choosing badly,
        /// not being offered the impossible. Both are satisfied by asking the selection's own sentence
        /// once per space he could reach: offered where some space keeps a candidate, greyed only where
        /// every one of them is empty.</para>
        ///
        /// <para><b>Three states, and the empty list is not the null.</b> <c>null</c> means no untaken
        /// Move stands behind this node, so it is judged from the board exactly as it always was. A
        /// non-empty list is the spaces to judge it from, one at a time. An EMPTY list is milestone
        /// 8.35p's suppression, kept for every case this walk still cannot enumerate - a Move whose
        /// subject it cannot resolve, and a second Move standing after a first, whose start square is
        /// itself a hypothesis. There, nothing after it is judged and nothing after it is refused,
        /// which is this query's own standing direction: it refuses what it can prove does nothing and
        /// never disables what it merely cannot read.</para>
        /// </param>
        private static BindingVerdict WalkToAttackTargetBinding(
            EffectNodeId nodeId,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context,
            ref int budget,
            IReadOnlyList<HexCoordinate> prospectiveActorSpaces,
            RefusalExplanation explanation)
        {
            if (--budget < 0)
            {
                throw new InvalidOperationException(
                    $"Effect definition '{context.Definition.Id}' did not settle within " +
                    $"{MaximumNodesPerOption} nodes while judging an Attack target binding.");
            }

            if (!context.Definition.TryGetNode(nodeId, out var node))
            {
                return BindingVerdict.Continue;
            }

            switch (node.Kind)
            {
                case EffectNodeKind.Capability:
                    if (node.Capability.Equals(
                            GuardsDigital.Core.Effects.Synthetic.SyntheticContinuationCapabilities
                                .SetAttackTargetV1) ||
                        // Milestone 8.35z27. The second way a card text binds this
                        // Attack's target, and it is a binding for the same reason the first is: a
                        // mandatory bullet of "choose one, or both" contributes a complete rulebook
                        // p.14 sequence, and that sequence's first act is to bind a target. What it
                        // does not do is name a unit here - it names its own printed targeting
                        // SENTENCE, and each sequence derives candidates from it against the board
                        // that sequence starts on. Whether those candidates exist is asked separately
                        // by CanAttackFindTarget's modal arm, which can read the sentences off this
                        // same definition; this walk answers only "is a binding reachable".
                        node.Capability.Equals(
                            GuardsDigital.Core.Effects.Synthetic.SyntheticSequenceCapabilities
                                .AttackSequenceV1))
                    {
                        return node.FailurePolicy == EffectFailurePolicy.Mandatory
                            ? BindingVerdict.Bound
                            : BindingVerdict.Continue;
                    }

                    if (prospectiveActorSpaces == null)
                    {
                        return Judge(node, frame, outcomes, context, explanation) == Verdict.Blocked
                            ? BindingVerdict.Blocked
                            : BindingVerdict.Continue;
                    }

                    return JudgeFromEverySpaceHeCouldReach(
                        node, frame, outcomes, context, prospectiveActorSpaces, explanation);

                case EffectNodeKind.Sequence:
                    // The outcomes handed to each child are the ones this walk was given, unchanged:
                    // what a step would publish is carried alongside rather than into the judging, so
                    // no node's verdict moves because a neighbour became readable. The carried copy
                    // exists for one question - which spaces a Move could leave the actor on - and a
                    // Move's subject is exactly the kind of input that arrives from an earlier node
                    // (`actor-space@1` publishes the square he starts from).
                    var spaces = prospectiveActorSpaces;
                    var carried = outcomes;
                    foreach (var child in node.ChildNodeIds)
                    {
                        var verdict = WalkToAttackTargetBinding(
                            child, frame, outcomes, context, ref budget, spaces, explanation);
                        if (verdict != BindingVerdict.Continue)
                        {
                            return verdict;
                        }

                        if (MovesSomebodyThisWalkCannotFollow(child, context))
                        {
                            spaces = WhereHeCouldStandAfter(child, frame, carried, context, spaces);
                        }

                        carried = WithAnticipatedOutputs(child, frame, carried, context);
                    }

                    return BindingVerdict.Continue;

                case EffectNodeKind.Optional:
                    // A player may decline this whole subtree, so it cannot be the mandatory binding
                    // that makes the enclosing Attack legal. Continue to the next ordered step.
                    return BindingVerdict.Continue;

                case EffectNodeKind.Choose:
                case EffectNodeKind.Conditional:
                    // A declared read-only predicate can prove which branch exists now. Do not
                    // invoke Execute, and keep the conservative walk for undecidable predicates
                    // or a board already made hypothetical by earlier movement.
                    if (node.Kind == EffectNodeKind.Conditional && prospectiveActorSpaces == null &&
                        context.Catalog.TryLoad(node.Capability, out var predicateHandler) &&
                        predicateHandler is IEffectAnticipatedOutcomeCapabilityHandler predicatePublisher &&
                        predicatePublisher.PublishesWithoutApplying)
                    {
                        var predicateContext = EffectResolutionSession.BuildCapabilityContext(frame, node, outcomes, context);
                        if (IsDecidable(predicateHandler, predicateContext) &&
                            predicateHandler.Anticipate(predicateContext) == EffectExecutionResultKind.Applied)
                        {
                            var facts = predicatePublisher.AnticipateOutcomes(predicateContext, Array.Empty<EffectValue>());
                            if (facts != null && facts.Any(fact => fact.Value.Kind == EffectValueKind.Boolean))
                            {
                                var branch = facts.Any(fact => fact.Value.Kind == EffectValueKind.Boolean && fact.Value.BooleanValue) ? 0 : 1;
                                return branch < node.ChildNodeIds.Count
                                    ? WalkToAttackTargetBinding(node.ChildNodeIds[branch], frame, outcomes, context,
                                        ref budget, prospectiveActorSpaces, explanation)
                                    : BindingVerdict.Continue;
                            }
                        }
                    }
                    foreach (var child in node.ChildNodeIds)
                    {
                        if (WalkToAttackTargetBinding(
                                child, frame, outcomes, context, ref budget,
                                prospectiveActorSpaces, explanation) ==
                            BindingVerdict.Bound)
                        {
                            return BindingVerdict.Bound;
                        }
                    }

                    // Milestone 8.35z49. A CONDITIONAL IS NOT A CHOOSE, and reading them as one greyed
                    // two cards on every board.
                    //
                    // <para>The two kinds arrive here together because both branch, and they were given
                    // one answer because for a Choose that answer is right: a Choose IS the decision, so
                    // a mandatory one whose every arm fails to reach the binding has no path to a
                    // binding at all, and `Blocked` is the honest verdict. Milestone 8.35z27's whole
                    // modal-Attack family depends on that reading and it is deliberately unchanged.</para>
                    //
                    // <para>A Conditional is the other thing entirely. An unsatisfied "if" is a step
                    // that simply does not happen, and the enclosing Sequence carries on to whatever
                    // stands after the gate - so a binding may legitimately stand AFTER it rather than
                    // inside it, and reporting `Blocked` made the Sequence short-circuit before ever
                    // reaching that binding. Whisper's Crimson Trail and Ursafar's Claws That Catch both
                    // compose exactly that shape (a mandatory enraged/discard gate wrapping an unrelated
                    // optional Move, with the mandatory `set-attack-target@1` standing after it), and
                    // both had their Attack greyed on EVERY board, whatever the board looked like.</para>
                    //
                    // <para><b>Why this cannot over-offer.</b> `Continue` is not an assertion that a
                    // binding exists - it is the absence of a verdict, which hands the question back to
                    // the enclosing Sequence. Where a binding stands after the gate the walk now reaches
                    // it and answers `Bound`; where none does, the Sequence runs out of children and
                    // returns `Continue`, and the caller reads anything that is not `Bound` as "not
                    // offerable" exactly as it read `Blocked`. So the ONLY definitions whose answer can
                    // move are those with a binding standing after such a gate, which is the set the
                    // offer-delta sweep enumerates structurally - and it is two.</para>
                    if (node.Kind == EffectNodeKind.Conditional)
                    {
                        return BindingVerdict.Continue;
                    }

                    return node.FailurePolicy == EffectFailurePolicy.Mandatory
                        ? BindingVerdict.Blocked
                        : BindingVerdict.Continue;

                default:
                    return BindingVerdict.Continue;
            }
        }

        /// <summary>
        /// Milestone 8.35z42: one node's verdict when the acting hero has not moved yet, asked
        /// once from each space he could be standing on.
        ///
        /// <para><b>Refused only where every space refuses.</b> That is RULING 45 stated as a walk: an
        /// unconstrained move offers every destination while ANY of them completes the sentence
        /// standing after it, so one space with a candidate is the whole of what makes the Attack
        /// offerable, and the player choosing a different one is the rope RULING 3 already gives him -
        /// he moves, the mandatory step stops the action, and the card is spent. What this refuses is
        /// the other case, and rule 14 has always refused it: text that can bind nothing from anywhere
        /// he can reach is proven empty before it is offered.</para>
        ///
        /// <para><b>Only a node that says it reads his space is asked.</b> A selection declaring
        /// <c>origin = Actor</c> resolves that origin in one place, and that place reads the
        /// hypothetical square. Every other node after an untaken Move keeps milestone 8.35p's
        /// suppression, because a node whose reading of the board this walk cannot redirect would be
        /// judged from the square he is about to leave - the exact defect 8.35p was written for.</para>
        ///
        /// <para><b>The explanation is local until the refusal is real.</b> A blocker met while
        /// probing a space that turns out to have an answer explains nothing, so it is discarded; only
        /// where every space refuses does the first named one reach the caller's explanation.</para>
        /// </summary>
        private static BindingVerdict JudgeFromEverySpaceHeCouldReach(
            EffectNodeDefinition node,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context,
            IReadOnlyList<HexCoordinate> spaces,
            RefusalExplanation explanation)
        {
            if (spaces.Count == 0 || !ReadsTheActorSpace(node))
            {
                return BindingVerdict.Continue;
            }

            var whileProbing = new RefusalExplanation();
            foreach (var space in spaces)
            {
                if (Judge(node, frame, outcomes, context, whileProbing, space) != Verdict.Blocked)
                {
                    return BindingVerdict.Continue;
                }
            }

            explanation?.RecordFirst(whileProbing.Blocking);
            return BindingVerdict.Blocked;
        }

        /// <summary>
        /// Milestone 8.35z42: whether this node's answer is measured from the acting hero's own
        /// space, read off the node's declaration rather than guessed from its capability.
        ///
        /// <para>A roster selection states where it measures from as a declared origin, and
        /// <c>RosterQueryOrigin.Actor</c> is resolved to a square in exactly one place - which is the
        /// place that reads a hypothetical one. So the declaration is the honest test: a node that says
        /// it measures from the actor can be asked about a space he does not stand on, and a node that
        /// says anything else cannot, whatever else it may read.</para>
        /// </summary>
        private static bool ReadsTheActorSpace(EffectNodeDefinition node)
        {
            // Milestone 8.93e regression closure. This selector's contract is a radius measured from
            // the acting hero; unlike the generic roster selector it has no explicit origin input
            // because Actor is its only origin. The hypothetical-space walk must therefore name the
            // typed reader itself or an impossible move-then-target continuation is offered merely
            // because its selection expresses that origin through its capability rather than an input.
            if (node.Capability.Equals(
                    GuardsDigital.Core.Effects.Synthetic.SyntheticTargetingCapabilities
                        .RadiusHeroTargetV1))
            {
                return true;
            }

            foreach (var input in node.Inputs)
            {
                if (string.Equals(
                        input.ParameterName,
                        GuardsDigital.Core.Effects.Synthetic.SyntheticRosterCapabilities
                            .OriginParameter,
                        StringComparison.Ordinal) &&
                    input.Literal.Kind == EffectValueKind.Integer &&
                    input.Literal.IntegerValue ==
                        (int)GuardsDigital.Core.Rules.CardResolution.RosterQueryOrigin.Actor)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Milestone 8.35z42: where the acting hero could be standing once this Move has been
        /// answered, or the suppressing empty set where this walk cannot say.
        ///
        /// <para>The handler answers, because the destinations are its own - the same list the prompt
        /// would carry - and because standing still is a reading of two rulebook sentences it already
        /// owns. Nothing here re-derives reachability, so the spaces this walk judges from and the
        /// spaces the player would be offered cannot part company.</para>
        ///
        /// <para>A Move standing after another Move suppresses rather than compounds: its own start
        /// square is already a hypothesis, and enumerating a move from each of several starts would be
        /// answering a question no printed card in this build asks. It is the conservative arm, and it
        /// is the arm every case outside the declared shape lands in.</para>
        /// </summary>
        private static IReadOnlyList<HexCoordinate> WhereHeCouldStandAfter(
            EffectNodeId nodeId,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> carried,
            EffectResolutionContext context,
            IReadOnlyList<HexCoordinate> alreadyHypothetical)
        {
            if (alreadyHypothetical == null &&
                TryAnticipateActorRelocationClause(
                    nodeId, frame, carried, context, out var anticipatedSpaces))
            {
                return anticipatedSpaces;
            }

            if (alreadyHypothetical != null ||
                !context.Definition.TryGetNode(nodeId, out var node) ||
                !context.Catalog.TryLoad(node.Capability, out var handler) ||
                !(handler is IEffectActorRelocationCapabilityHandler relocating))
            {
                return Array.Empty<HexCoordinate>();
            }

            var capabilityContext =
                EffectResolutionSession.BuildCapabilityContext(frame, node, carried, context);
            return relocating.TryAnticipateActorSpaces(capabilityContext, out var spaces) &&
                spaces != null
                ? spaces
                : Array.Empty<HexCoordinate>();
        }

        /// <summary>
        /// Milestone 8.35p: whether this step will put a piece somewhere this walk cannot know, so
        /// that the board every later step reads is not the board in front of it.
        ///
        /// <para>It names the typed Move contract rather than any card, exactly as the binding walk
        /// above names <c>set-attack-target@1</c>: a declared Move is the one operation a card text
        /// takes whose result is a position, and where that position ends up is either a destination
        /// the player has not chosen yet or an output no earlier step has published. Either way it is
        /// unknown here, and there is nothing conservative about judging a later adjacency question
        /// against the square the piece is about to leave.</para>
        /// </summary>
        private static bool MovesSomebodyThisWalkCannotFollow(EffectNodeId nodeId, EffectResolutionContext context) =>
            ContainsActorRelocation(nodeId, context);

        private static bool ContainsActorRelocation(EffectNodeId id, EffectResolutionContext context)
        {
            if (!context.Definition.TryGetNode(id, out var node)) return false;
            if (node.Kind == EffectNodeKind.Capability)
            {
                return node.Capability.Equals(
                           GuardsDigital.Core.Effects.Synthetic.SyntheticDisplacementCapabilities
                               .DirectMoveV3) ||
                       node.Capability.Equals(
                           GuardsDigital.Core.Effects.Synthetic.SyntheticExchangeCapabilities
                               .SwapWithActorV1);
            }

            return (node.Kind == EffectNodeKind.Optional ||
                    node.Kind == EffectNodeKind.Sequence ||
                    node.Kind == EffectNodeKind.Conditional) &&
                node.ChildNodeIds.Any(child => ContainsActorRelocation(child, context));
        }

        /// <summary>
        /// Milestone 8.104: the actor spaces produced by one declared relocation clause, including a
        /// relocation selected by a read-only Conditional and an optional wrapper that preserves the
        /// unchanged space. This is the same non-mutating handler contract used for a direct Move;
        /// containers only carry that answer to the enclosing Sequence.
        /// </summary>
        private static bool TryAnticipateActorRelocationClause(
            EffectNodeId id,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context,
            out IReadOnlyList<HexCoordinate> spaces)
        {
            spaces = Array.Empty<HexCoordinate>();
            if (!context.Definition.TryGetNode(id, out var node) ||
                !ContainsActorRelocation(id, context))
            {
                return false;
            }

            if (node.Kind == EffectNodeKind.Capability)
            {
                if (!context.Catalog.TryLoad(node.Capability, out var capability) ||
                    !(capability is IEffectActorRelocationCapabilityHandler relocating))
                {
                    return false;
                }

                var capabilityContext =
                    EffectResolutionSession.BuildCapabilityContext(
                        frame, node, outcomes, context);
                return relocating.TryAnticipateActorSpaces(capabilityContext, out spaces) &&
                    spaces != null;
            }

            if (node.Kind == EffectNodeKind.Conditional)
            {
                if (!context.Catalog.TryLoad(node.Capability, out var predicate) ||
                    !(predicate is IEffectAnticipatedOutcomeCapabilityHandler publisher) ||
                    !publisher.PublishesWithoutApplying)
                {
                    return false;
                }

                var predicateContext =
                    EffectResolutionSession.BuildCapabilityContext(frame, node, outcomes, context);
                if (!IsDecidable(predicate, predicateContext) ||
                    predicate.Anticipate(predicateContext) != EffectExecutionResultKind.Applied)
                {
                    return false;
                }

                var facts = publisher.AnticipateOutcomes(
                    predicateContext, Array.Empty<EffectValue>());
                if (facts == null ||
                    !facts.Any(fact => fact.Value.Kind == EffectValueKind.Boolean))
                {
                    return false;
                }

                var branch = facts.Any(fact =>
                    fact.Value.Kind == EffectValueKind.Boolean && fact.Value.BooleanValue) ? 0 : 1;
                if (branch >= node.ChildNodeIds.Count)
                {
                    return TryCurrentActorSpace(frame, context, out spaces);
                }

                return TryAnticipateActorRelocationClause(
                    node.ChildNodeIds[branch], frame,
                    WithAnticipatedOutputs(id, frame, outcomes, context), context, out spaces);
            }

            if (node.Kind == EffectNodeKind.Optional)
            {
                if (node.ChildNodeIds.Count != 1)
                {
                    return false;
                }

                if (!TryAnticipateActorRelocationClause(
                        node.ChildNodeIds[0], frame, outcomes, context, out spaces))
                {
                    // Declining an optional relocation always preserves the actor's current space.
                    // Keep that proven answer when the branch itself is impossible; where the branch
                    // is performable but its destinations cannot be anticipated, retain the existing
                    // conservative unknown instead of falsely ruling out a continuation it may create.
                    return !CanPerformOptionalBranch(node, frame, outcomes, context) &&
                        TryCurrentActorSpace(frame, context, out spaces);
                }

                if (TryCurrentActorSpace(frame, context, out var unchanged))
                {
                    spaces = spaces.Concat(unchanged).Distinct().ToArray();
                }

                return true;
            }

            if (node.Kind == EffectNodeKind.Sequence && node.ChildNodeIds.Count == 2 &&
                TryAnticipateSelectedRelocation(
                    node.ChildNodeIds[0], node.ChildNodeIds[1], frame, outcomes, context,
                    out spaces))
            {
                return true;
            }

            var nodes = node.Kind == EffectNodeKind.Sequence
                ? node.ChildNodeIds
                : new[] { node.Id };
            var carried = outcomes;
            for (var index = 0; index < nodes.Count; index++)
            {
                var child = nodes[index];
                if (ContainsActorRelocation(child, context))
                {
                    return index == nodes.Count - 1 &&
                        TryAnticipateActorRelocationClause(
                            child, frame, carried, context, out spaces);
                }

                if (!context.Definition.TryGetNode(child, out var leaf) || leaf.Kind != EffectNodeKind.Capability ||
                    !context.Catalog.TryLoad(leaf.Capability, out var handler)) return false;
                var probe = EffectResolutionSession.BuildCapabilityContext(frame, leaf, carried, context);
                if (!IsDecidable(handler, probe)) return false;
                if (handler.Anticipate(probe) == EffectExecutionResultKind.ImpossibleMandatory)
                {
                    return false;
                }
                if (!(handler is IEffectAnticipatedOutcomeCapabilityHandler publisher) || !publisher.PublishesWithoutApplying) return false;
                carried = WithAnticipatedOutputs(child, frame, carried, context);
            }

            return false;
        }

        /// <summary>
        /// Milestone 8.93e regression closure: the actor spaces behind the typed
        /// selection-then-relocation shape used by an optional Move. The selection and relocation
        /// handlers already own every candidate and destination; this method merely joins their
        /// read-only contracts once per eligible answer. Moving another piece preserves the actor's
        /// current space, while moving the actor contributes the relocation handler's own spaces.
        /// Any unreadable answer leaves the whole clause unknown rather than narrowing it by guess.
        /// </summary>
        private static bool TryAnticipateSelectedRelocation(
            EffectNodeId selectionId,
            EffectNodeId relocationId,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context,
            out IReadOnlyList<HexCoordinate> spaces)
        {
            spaces = Array.Empty<HexCoordinate>();
            if (!context.Definition.TryGetNode(selectionId, out var selectionNode) ||
                selectionNode.Kind != EffectNodeKind.Capability ||
                !context.Definition.TryGetNode(relocationId, out var relocationNode) ||
                relocationNode.Kind != EffectNodeKind.Capability ||
                !ContainsActorRelocation(relocationId, context) ||
                !context.Catalog.TryLoad(selectionNode.Capability, out var selectionHandler) ||
                !(selectionHandler is IEffectSelectionCapabilityHandler selection) ||
                !(selectionHandler is IEffectAnticipatedOutcomeCapabilityHandler publisher) ||
                !publisher.PublishesWithoutApplying ||
                !context.Catalog.TryLoad(relocationNode.Capability, out var relocationHandler) ||
                !(relocationHandler is IEffectActorRelocationCapabilityHandler relocating) ||
                !TryCurrentActorSpace(frame, context, out var current))
            {
                return false;
            }

            var selectionContext =
                EffectResolutionSession.BuildCapabilityContext(
                    frame, selectionNode, outcomes, context);
            if (!IsDecidable(selectionHandler, selectionContext) ||
                selectionHandler.Anticipate(selectionContext) !=
                    EffectExecutionResultKind.NeedsChoice ||
                selection.AnticipateMinimumSelections(selectionContext) != 1)
            {
                return false;
            }

            var candidates = EffectSelectionEligibilityQuery.EligibleCandidates(
                selectionNode, selection.AnticipateCandidates(selectionContext),
                frame, outcomes, context);
            if (candidates.Count == 0)
            {
                return false;
            }

            var anticipated = new List<HexCoordinate>();
            foreach (var candidate in candidates)
            {
                var facts = publisher.AnticipateOutcomes(
                    selectionContext, new[] { candidate });
                if (facts == null || facts.Count == 0)
                {
                    return false;
                }

                var carried = outcomes.Concat(new[]
                {
                    new EffectNodeOutcome(
                        selectionNode.Id, EffectExecutionResultKind.Applied, facts)
                }).ToArray();
                var relocationContext =
                    EffectResolutionSession.BuildCapabilityContext(
                        frame, relocationNode, carried, context);
                if (relocating.TryAnticipateActorSpaces(
                        relocationContext, out var candidateSpaces) &&
                    candidateSpaces != null)
                {
                    anticipated.AddRange(candidateSpaces);
                    continue;
                }

                // A relocation of somebody other than the actor changes none of the spaces from
                // which later actor-anchored text is measured. This is read from the relocation's
                // declared subject after the selection outcome has been supplied.
                var mover = relocationContext.Input("mover");
                if (mover.Kind == EffectValueKind.Coordinate &&
                    !mover.Coordinate.Equals(current[0]))
                {
                    anticipated.Add(current[0]);
                    continue;
                }

                return false;
            }

            spaces = anticipated.Distinct().ToArray();
            return spaces.Count > 0;
        }

        private static bool TryCurrentActorSpace(
            PendingResolution frame,
            EffectResolutionContext context,
            out IReadOnlyList<HexCoordinate> spaces)
        {
            spaces = Array.Empty<HexCoordinate>();
            if (!context.Components.Board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(frame.ActorId), out var unchanged))
            {
                return false;
            }

            spaces = new[] { unchanged };
            return true;
        }

        /// <summary>
        /// One node's verdict, and its subtree's, in the order execution would reach them. The walk
        /// short-circuits at the first thing that can apply, because an option only has to do one
        /// thing to be worth offering.
        /// </summary>
        private static Verdict Walk(
            EffectNodeId nodeId,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context,
            ref int budget,
RefusalExplanation explanation = null)
        {
            if (--budget < 0)
            {
                throw new InvalidOperationException(
                    $"Effect definition '{context.Definition.Id}' did not settle within " +
                    $"{MaximumNodesPerOption} nodes while judging an option.");
            }

            if (!context.Definition.TryGetNode(nodeId, out var node))
            {
                // An unresolvable child is a definition fault the resolver reports when it walks
                // there. Judging it unperformable would hide that behind a greyed control.
                return Verdict.Performable;
            }

            switch (node.Kind)
            {
                case EffectNodeKind.Capability:
                    return Judge(node, frame, outcomes, context, explanation);

                case EffectNodeKind.Sequence:
                    // Milestone 8.35l. The sequence carries forward what an earlier step would publish,
                    // so a later step bound to it can be judged at all. Nothing is executed to get it:
                    // only a node whose handler declares
                    // `IEffectAnticipatedOutcomeCapabilityHandler` - a claim that deriving its outputs
                    // mutates nothing - contributes, and only when the walk already found it decidable
                    // and performable. `actor-space@1` is the whole reason: a Place reading the actor's
                    // own space was undecidable to this walk, so Blend Into Shadows was offered against
                    // a standing restriction that would refuse it, drew no prompt, and spent the card.
                    var carried = outcomes;
                    foreach (var child in node.ChildNodeIds)
                    {
                        var verdict = Walk(child, frame, carried, context, ref budget, explanation);
                        if (verdict != Verdict.Empty)
                        {
                            // Performable ends the walk; blocked ends the sequence, because the
                            // resolution stops at a mandatory step it cannot take.
                            return verdict;
                        }

                        carried = WithAnticipatedOutputs(child, frame, carried, context);
                    }

                    return Verdict.Empty;

                case EffectNodeKind.Optional:
                    // Declining is always available and always does nothing, so an optional step is
                    // worth offering exactly when the step it governs is.
                    return Walk(node.ChildNodeIds[0], frame, outcomes, context, ref budget, explanation) ==
                        Verdict.Performable
                        ? Verdict.Performable
                        : Verdict.Empty;

                case EffectNodeKind.Choose:
                    foreach (var child in node.ChildNodeIds)
                    {
                        if (Walk(child, frame, outcomes, context, ref budget, explanation) == Verdict.Performable)
                        {
                            return Verdict.Performable;
                        }
                    }

                    // Every option of a nested Choose is refused, so the node itself cannot be taken
                    // and its own policy decides what that means - the same reading the resolver makes
                    // when it reaches such a node directly.
                    return node.FailurePolicy == EffectFailurePolicy.Mandatory
                        ? Verdict.Blocked
                        : Verdict.Empty;

                case EffectNodeKind.Conditional:
                    // Which branch runs could only be learned by executing the predicate, which this
                    // walk may not do. Both branches are therefore judged, and the node counts as
                    // performable when either is; when neither is, execution still continues, because
                    // an unevaluated predicate could have taken the harmless path.
                    foreach (var child in node.ChildNodeIds)
                    {
                        if (Walk(child, frame, outcomes, context, ref budget, explanation) == Verdict.Performable)
                        {
                            return Verdict.Performable;
                        }
                    }

                    return Verdict.Empty;

                case EffectNodeKind.RepeatAction:
                    // Milestone 6.7. A Repeat applies nothing of its own: what it re-runs is the very
                    // action this walk is judging, so calling it performable would let an action that
                    // can do nothing be offered on the strength of being able to do nothing twice. It
                    // never blocks either, because the node kind refuses to be mandatory.
                    return Verdict.Empty;

                default:
                    return Verdict.Performable;
            }
        }

        /// <summary>
        /// One capability node's verdict: what its handler says it would report, read through the
        /// node's own failure policy, which is where p.10's mandatory-versus-optional distinction
        /// already lives.
        /// </summary>
        /// <param name="prospectiveActorSpace">
        /// Milestone 8.35z42: the square to answer as if the acting hero were standing on,
        /// and nothing on every path but the per-space proof above. It reaches the handler through the
        /// context alone, so a node that never asks where the actor is cannot tell it was asked twice.
        /// </param>
        private static Verdict Judge(
            EffectNodeDefinition node,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context,
            RefusalExplanation explanation = null,
            HexCoordinate? prospectiveActorSpace = null)
        {
            if (!context.Catalog.TryLoad(node.Capability, out var handler))
            {
                // An unregistered capability is a load fault the resolver raises when it gets there.
                return Verdict.Performable;
            }

            var capabilityContext =
                EffectResolutionSession.BuildCapabilityContext(frame, node, outcomes, context);
            if (prospectiveActorSpace.HasValue)
            {
                capabilityContext =
                    capabilityContext.AsIfTheActorStoodAt(prospectiveActorSpace.Value);
            }

            if (!IsDecidable(handler, capabilityContext))
            {
                return Verdict.Performable;
            }

            // Milestone 6.8l: a selection that says what it selects for is judged by that consumer
            // rather than by its own ability to pause. Without this a branch behind a selection was
            // performable whenever anything was targetable, however little the step after it could do
            // with what was chosen - and since milestone 6.8k every named subject arrives this way.
            var anticipated = EffectSelectionEligibilityQuery.JudgeSelection(
                handler.Anticipate(capabilityContext),
                handler,
                node,
                capabilityContext,
                frame,
                outcomes,
                context);

            switch (anticipated)
            {
                case EffectExecutionResultKind.Applied:
                    // Milestone 8.35l. A node that only publishes a fact has not made the action worth
                    // taking. Reading the acting hero's own coordinate is the case that mattered:
                    // `actor-space@1` applied, the walk short-circuited there, and the Place standing
                    // behind it - which a restriction had already barred - was never judged, so
                    // Tigerclaw's Blend Into Shadows was offered, drew no prompt, and spent the card
                    // for nothing. The sequence carries what it published to the next step instead, so
                    // the step that does the visible thing is the one this answer is about.
                    return handler is IEffectAnticipatedOutcomeCapabilityHandler publisher &&
                        publisher.PublishesWithoutApplying
                        ? Verdict.Empty
                        : Verdict.Performable;

                case EffectExecutionResultKind.NeedsChoice:
                    return Verdict.Performable;

                case EffectExecutionResultKind.ImpossibleMandatory:
                    // Milestone 8.35l. Ask what refused, before reading the failure policy: the node
                    // itself where it can explain its own refusal, and otherwise the consumer this
                    // selection was narrowed by, which is where a selection's refusal actually lives.
                    RecordExplanation(
                        explanation, handler, node, capabilityContext, frame, outcomes, context);

                    // `if able`, `may`, and `up to` all treat an impossible step as simply not taken,
                    // so only a mandatory one stops what follows it.
                    return node.FailurePolicy == EffectFailurePolicy.Mandatory
                        ? Verdict.Blocked
                        : Verdict.Empty;

                default:
                    return Verdict.Empty;
            }
        }

        /// <summary>
        /// Milestone 8.35l: which standing modification refused this node, recorded once.
        ///
        /// <para>Two shapes reach it and both are the refusing party answering for itself. A node that
        /// refused on a restriction of its own - the Place behind Tigerclaw's actor-space bridge - says
        /// so directly. A selection narrowed to nothing was not refused by itself at all: its consumer
        /// refused every candidate, so the consumer is asked. Neither case re-derives anything, and a
        /// refusal with no restriction behind it records nothing rather than inventing a cause.</para>
        /// </summary>
        private static void RecordExplanation(
            RefusalExplanation explanation,
            IEffectCapabilityHandler handler,
            EffectNodeDefinition node,
            EffectCapabilityContext capabilityContext,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            if (explanation == null)
            {
                return;
            }

            if (TryExplainNodeRefusal(
                    handler, node, capabilityContext, frame, outcomes, context, out var blocking))
            {
                explanation.RecordFirst(blocking);
            }
        }

        /// <summary>
        /// Milestone 8.35z14 (the blocker-identity channel): which standing modification explains why a
        /// node refuses, asked the same way whether the question is "would this be worth offering" (a
        /// probe, before <see cref="IEffectCapabilityHandler.Execute"/> or
        /// <see cref="IEffectCapabilityHandler.Resume"/> has run) or "why did this just stop" (the live
        /// resolution, after one of them already returned
        /// <see cref="EffectExecutionResultKind.ImpossibleMandatory"/>). Two shapes answer for
        /// themselves - the refusing node's own <see cref="IEffectRefusalExplainingCapabilityHandler"/>
        /// where it has one, and otherwise the consumer that narrowed a selection to nothing, via
        /// <see cref="EffectSelectionEligibilityQuery.TryExplainSelectionRefusal"/> - and this is the
        /// one answer both moments ask, so a control's greyed reason and a stop's recorded reason can
        /// never read as two readings of the same rule.
        /// </summary>
        internal static bool TryExplainNodeRefusal(
            IEffectCapabilityHandler handler,
            EffectNodeDefinition node,
            EffectCapabilityContext capabilityContext,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context,
            out AppliedRuleModification blocking)
        {
            blocking = null;
            if (handler == null || node == null || capabilityContext == null ||
                frame == null || context == null)
            {
                return false;
            }

            if (handler is IEffectRefusalExplainingCapabilityHandler explaining &&
                explaining.TryExplainRefusal(capabilityContext, out blocking))
            {
                return true;
            }

            return EffectSelectionEligibilityQuery.TryExplainSelectionRefusal(
                handler, node, capabilityContext, frame, outcomes, context, out blocking);
        }

        /// <summary>
        /// Milestone 8.35l: the outcomes so far, plus what this node would publish if it publishes
        /// without applying anything and can be read now.
        ///
        /// <para>Nothing is executed. A handler contributes only by declaring
        /// <see cref="IEffectAnticipatedOutcomeCapabilityHandler.PublishesWithoutApplying"/>, which is
        /// a claim that running it changes no rules state, and only when its required inputs already
        /// resolve - so this widens what the walk can read without widening what it may do. A node it
        /// cannot read contributes nothing and the step after it stays undecidable, which is milestone
        /// 6.5ac's conservative position exactly where it still applies.</para>
        /// </summary>
        private static IReadOnlyList<EffectNodeOutcome> WithAnticipatedOutputs(
            EffectNodeId nodeId,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context)
        {
            if (!context.Definition.TryGetNode(nodeId, out var node) ||
                node.Kind != EffectNodeKind.Capability ||
                !context.Catalog.TryLoad(node.Capability, out var handler) ||
                !(handler is IEffectAnticipatedOutcomeCapabilityHandler publisher) ||
                !publisher.PublishesWithoutApplying)
            {
                return outcomes;
            }

            var capabilityContext =
                EffectResolutionSession.BuildCapabilityContext(frame, node, outcomes, context);
            if (!IsDecidable(handler, capabilityContext) ||
                handler.Anticipate(capabilityContext) != EffectExecutionResultKind.Applied)
            {
                return outcomes;
            }

            var published = publisher.AnticipateOutcomes(
                capabilityContext, Array.Empty<EffectValue>());
            if (published == null || published.Count == 0)
            {
                return outcomes;
            }

            var carried = new List<EffectNodeOutcome>(outcomes);
            carried.Add(new EffectNodeOutcome(
                node.Id, EffectExecutionResultKind.Applied, published));
            return carried;
        }

        /// <summary>
        /// Whether this node can be judged at all yet.
        ///
        /// A required parameter bound to a later node's output resolves to nothing while the walk runs
        /// - the usual shape being a subject a selection has not made yet - and a handler asked with a
        /// missing subject would answer about a question nobody posed. Such a node is treated as
        /// performable, which is the direction to err in: this milestone refuses an option it can prove
        /// does nothing and never disables one it merely cannot read.
        /// </summary>
        private static bool IsDecidable(
            IEffectCapabilityHandler handler,
            EffectCapabilityContext capabilityContext) =>
            handler.Schema.Parameters
                .Where(parameter => parameter.IsRequired)
                .All(parameter => capabilityContext.Input(parameter.Name).IsDefined);
    }
}
