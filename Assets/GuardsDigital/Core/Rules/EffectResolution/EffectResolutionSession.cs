using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    public enum EffectResolutionRejectionReason
    {
        None = 0,
        NoPendingChoice = 1,
        StaleChoice = 2,
        WrongChooser = 3,
        InvalidChoice = 4,
        IncompleteSelection = 5,
        UnregisteredCapability = 6,
        UnknownNode = 7
    }

    /// <summary>
    /// One node's contribution to the ordered fact stream, with the timing checkpoints its capability
    /// declared around itself. The caller turns these into whatever event shape its layer speaks;
    /// keeping that translation outside the resolver is what lets the frame stay Unity-free and
    /// action-family-agnostic.
    /// </summary>
    public sealed class EffectResolutionEvent
    {
        public EffectResolutionEvent(
            EffectNodeId nodeId,
            EffectExecutionResultKind resultKind,
            IEnumerable<EffectOutcomeFact> outputs = null,
            IEnumerable<TimingCheckpoint> checkpointsBefore = null,
            IEnumerable<TimingCheckpoint> checkpointsAfter = null)
        {
            NodeId = nodeId;
            ResultKind = resultKind;
            Outputs = new ReadOnlyCollection<EffectOutcomeFact>(
                (outputs ?? Enumerable.Empty<EffectOutcomeFact>()).ToArray());
            CheckpointsBefore = new ReadOnlyCollection<TimingCheckpoint>(
                (checkpointsBefore ?? Enumerable.Empty<TimingCheckpoint>()).ToArray());
            CheckpointsAfter = new ReadOnlyCollection<TimingCheckpoint>(
                (checkpointsAfter ?? Enumerable.Empty<TimingCheckpoint>()).ToArray());
        }

        public EffectNodeId NodeId { get; }
        public EffectExecutionResultKind ResultKind { get; }
        public IReadOnlyList<EffectOutcomeFact> Outputs { get; }
        public IReadOnlyList<TimingCheckpoint> CheckpointsBefore { get; }
        public IReadOnlyList<TimingCheckpoint> CheckpointsAfter { get; }
    }

    public sealed class EffectResolutionStepResult
    {
        private EffectResolutionStepResult(
            bool accepted,
            EffectResolutionRejectionReason rejectionReason,
            PendingResolution frame,
            bool isComplete,
            IEnumerable<EffectResolutionEvent> events,
            RepeatRequest repeatRequested,
            GuardsDigital.Core.Rules.BoardObjects.LanePushCauseKind lanePushCause,
            IReadOnlyList<GuardsDigital.Core.Units.Heroes.HeroId> requestedHeroDefeats,
            IReadOnlyList<GuardsDigital.Core.Setup.MinionId> requestedMinionDefeats,
            GuardsDigital.Core.Rules.CardResolution.AttackTarget boundAttackTarget = default,
            GuardsDigital.Core.Rules.CardResolution.PreActionPlacement aroundActionPlacement =
                GuardsDigital.Core.Rules.CardResolution.PreActionPlacement.Undecided,
            IReadOnlyList<GuardsDigital.Core.Rules.Modifications.RuleObject> appliedPushes = null)
        {
            AppliedPushes =
                new ReadOnlyCollection<GuardsDigital.Core.Rules.Modifications.RuleObject>(
                    (appliedPushes ??
                        Array.Empty<GuardsDigital.Core.Rules.Modifications.RuleObject>()).ToArray());
            BoundAttackTarget = boundAttackTarget;
            AroundActionPlacement = aroundActionPlacement;
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Frame = frame;
            IsComplete = isComplete;
            Events = new ReadOnlyCollection<EffectResolutionEvent>(
                (events ?? Enumerable.Empty<EffectResolutionEvent>()).ToArray());
            RepeatRequested = repeatRequested;
            LanePushCause = lanePushCause;
            RequestedHeroDefeats = new ReadOnlyCollection<GuardsDigital.Core.Units.Heroes.HeroId>(
                (requestedHeroDefeats ?? Array.Empty<GuardsDigital.Core.Units.Heroes.HeroId>())
                    .ToArray());
            RequestedMinionDefeats = new ReadOnlyCollection<GuardsDigital.Core.Setup.MinionId>(
                (requestedMinionDefeats ?? Array.Empty<GuardsDigital.Core.Setup.MinionId>())
                    .ToArray());
        }

        public bool Accepted { get; }
        public EffectResolutionRejectionReason RejectionReason { get; }

        /// <summary>The frame to keep, or null once the resolution has completed.</summary>
        public PendingResolution Frame { get; }

        public bool IsComplete { get; }
        public IReadOnlyList<EffectResolutionEvent> Events { get; }

        /// <summary>
        /// Milestone 6.7: whether this completed instance asked to be repeated. Meaningful only
        /// alongside <see cref="IsComplete"/>, because the completing frame is the last thing that
        /// carries the outcome the request lives on; while a resolution is still paused the same
        /// question is asked of <see cref="Frame"/> through <see cref="RepeatedActionQuery"/>.
        ///
        /// It says nothing about when the next instance starts. That is the card action's business,
        /// and its answer is "once this instance's after-action correction boundary is clean".
        ///
        /// Milestone 8.6 widened it from a boolean to the request itself, because a bounded repeat's
        /// next instance has to be told which repeat it is and, when the card said `on the same
        /// target`, which target it inherits.
        /// </summary>
        public RepeatRequest RepeatRequested { get; }

        /// <summary>
        /// Milestone 6.9: which of rulebook p.8's two sentences this completed instance answered, or
        /// the named absence. Meaningful only alongside <see cref="IsComplete"/>, for the reason
        /// <see cref="RepeatRequested"/> is: the completing frame is the last thing that carries the
        /// outcome the recorded cause lives on.
        ///
        /// It says nothing about whether a push is owed. That question needs the board, and the card
        /// action asks it.
        /// </summary>
        public GuardsDigital.Core.Rules.BoardObjects.LanePushCauseKind LanePushCause { get; }

        /// <summary>
        /// Milestone 8.10. The heroes this step's completed outcomes asked rulebook p.13's Defeat list
        /// to be run about, in the order the frame recorded them. Empty on every step that asked for
        /// none, which is every step in this build before Electroblast.
        ///
        /// It says nothing about whether the list can be run: that needs the coin ledger, the Levels
        /// and the Life counters, and the card action asks it - the same division
        /// <see cref="LanePushCause"/> draws.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Units.Heroes.HeroId> RequestedHeroDefeats { get; }

        public IReadOnlyList<GuardsDigital.Core.Setup.MinionId> RequestedMinionDefeats { get; }

        /// <summary>
        /// Milestone 8.19a. The unit this step's completed outcomes named as the enclosing Attack's
        /// target, or the undefined target on every step that named none - which is every step in this
        /// build outside an Attack whose card declares `attack-target@2`.
        ///
        /// <para>It is reported here rather than read off the frame for the reason
        /// <see cref="RequestedHeroDefeats"/> is: the completing frame is the last thing that carries
        /// the outcome the binding lives on, and by the time the caller sees the result that frame is
        /// gone. What it says nothing about is whether the Attack may take that unit - the target set
        /// is the Attack resolver's, re-derived against the board this text has just changed.</para>
        /// </summary>
        public GuardsDigital.Core.Rules.CardResolution.AttackTarget BoundAttackTarget { get; }

        /// <summary>
        /// Milestone 8.19a. Which side of its primary action this step's completed outcomes placed the
        /// rest of the text on, or <c>Undecided</c> wherever no ordering question was answered.
        /// </summary>
        public GuardsDigital.Core.Rules.CardResolution.PreActionPlacement AroundActionPlacement { get; }

        /// <summary>
        /// Milestone 8.19c1. The objects a Push applied to at the node this step stopped on, in commit
        /// order; empty on every step where no Push applied.
        ///
        /// <para>It is nonempty only on a frame that stopped <em>without</em> a pending choice, which
        /// is the one shape this milestone adds: the resolver holds the enclosing effect exactly where
        /// it is so that whatever declared an <c>after-push-trigger@1</c> can run before the next node
        /// does. The resolver knows nothing about triggers - it is told, through
        /// <see cref="EffectResolutionContext.WatchesAppliedPushes"/>, that somebody is watching, and
        /// stopping is all it does about it. Who runs, in what order, and what happens if nobody
        /// applies is the caller's, exactly as <see cref="LanePushCause"/> and
        /// <see cref="RequestedHeroDefeats"/> are.</para>
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Rules.Modifications.RuleObject> AppliedPushes { get; }

        public static EffectResolutionStepResult Accept(
            PendingResolution frame,
            bool isComplete,
            IEnumerable<EffectResolutionEvent> events,
            RepeatRequest repeatRequested = default,
            GuardsDigital.Core.Rules.BoardObjects.LanePushCauseKind lanePushCause =
                GuardsDigital.Core.Rules.BoardObjects.LanePushCauseKind.None,
            IReadOnlyList<GuardsDigital.Core.Units.Heroes.HeroId> requestedHeroDefeats = null,
            IReadOnlyList<GuardsDigital.Core.Setup.MinionId> requestedMinionDefeats = null,
            GuardsDigital.Core.Rules.CardResolution.AttackTarget boundAttackTarget = default,
            GuardsDigital.Core.Rules.CardResolution.PreActionPlacement aroundActionPlacement =
                GuardsDigital.Core.Rules.CardResolution.PreActionPlacement.Undecided,
            IReadOnlyList<GuardsDigital.Core.Rules.Modifications.RuleObject> appliedPushes = null) =>
            new EffectResolutionStepResult(
                true, EffectResolutionRejectionReason.None, isComplete ? null : frame, isComplete, events,
                repeatRequested, lanePushCause, requestedHeroDefeats, requestedMinionDefeats,
                boundAttackTarget, aroundActionPlacement, appliedPushes);

        public static EffectResolutionStepResult Reject(
            EffectResolutionRejectionReason reason,
            PendingResolution frame)
        {
            if (reason == EffectResolutionRejectionReason.None)
            {
                throw new ArgumentException("A rejected effect step requires a reason.", nameof(reason));
            }

            return new EffectResolutionStepResult(
                false, reason, frame, false, null, RepeatRequest.None,
                GuardsDigital.Core.Rules.BoardObjects.LanePushCauseKind.None, null, null);
        }
    }

    /// <summary>Everything the resolver needs that is not part of the frame's own state.</summary>
    public sealed class EffectResolutionContext
    {
        public EffectResolutionContext(
            EffectDefinition definition,
            EffectCapabilityCatalog catalog,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            int roundIndex,
            int turnIndex,
            RuleModificationSet modifications,
            EffectCardState cardState,
            ActionKind enclosingActionKind,
            HeroId opposingHeroId = default,
            MatchComponents components = null,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null,
            EnclosingAttackFacts enclosingAttack = default,
            EffectRosterState rosterState = null,
            bool watchesAppliedPushes = false)
        {
            WatchesAppliedPushes = watchesAppliedPushes;
            EnclosingAttack = enclosingAttack;
            if (!Enum.IsDefined(typeof(ActionKind), enclosingActionKind) ||
                enclosingActionKind == ActionKind.None)
            {
                throw new ArgumentException(
                    "An effect resolution names the action it is running inside. Rulebook p.18 makes " +
                    "an immunity declared against one action type unaffected by the others, so a " +
                    "capability that filters by activity has to be told which one it is in rather " +
                    "than assuming the Skill it used to be.",
                    nameof(enclosingActionKind));
            }

            EnclosingActionKind = enclosingActionKind;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Participants = participants;
            Timing = timing;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Modifications = modifications ?? throw new ArgumentNullException(nameof(modifications));
            CardState = cardState ?? throw new ArgumentNullException(nameof(cardState));
            OpposingHeroId = opposingHeroId;
            Components = components;
            HeroItems = heroItems;
            RosterState = rosterState ?? EffectRosterState.Absent;
        }

        /// <summary>
        /// Milestone 8.19c1. Whether any live rule source has declared that it runs after one of
        /// rulebook p.18's Pushes applies.
        ///
        /// <para>A boolean rather than the sources themselves, and false by default, because the
        /// resolver's whole part in the moment is to stop at it. Which sources apply, which pushed
        /// object each of them concerns, and what their effects do are the caller's questions - it
        /// holds the unlocked Ultimates and the triggered-effect catalog, and this walker deliberately
        /// holds neither. False costs nothing: the walk is exactly what it was before this milestone,
        /// so a match with no such source cannot pay for one.</para>
        /// </summary>
        public bool WatchesAppliedPushes { get; }

        public EffectDefinition Definition { get; }
        public EffectCapabilityCatalog Catalog { get; }
        public MatchParticipants Participants { get; }
        public ActiveEffectLifecycle Timing { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public HeroId OpposingHeroId { get; }

        /// <summary>
        /// What card text applied inside rulebook p.14 may ask about the Attack it is running inside,
        /// or the named absence outside one. Milestone 8.10 carried the hero the Attack took; 8.11
        /// widened it, because Wasp.s Block cards are conditioned on the attack being ranged and two
        /// of them exclude the attacker from a selection the defender makes.
        /// </summary>
        public EnclosingAttackFacts EnclosingAttack { get; }

        /// <summary>
        /// The kind of action this effect is running inside. Milestone 8.5, closing the 7.9a review's
        /// F-BOARD-03: four capability handlers asked p.18's immunity about
        /// <c>RuleActivity.Skill</c> because a Skill was the only action an effect could run inside,
        /// and the answer would have quietly stayed Skill the moment an Attack's card text ran the same
        /// handler. Card text has no activity of its own - p.11 says so for movement written on a card,
        /// and nothing on p.19 gives Swap, Replace or Remove one either - so it borrows the enclosing
        /// action's, which is what this carries.
        /// </summary>
        public ActionKind EnclosingActionKind { get; }

        /// <summary>
        /// The modifications in effect for this resolution. Required since milestone 6.4, which added
        /// the first capabilities that read rules rather than only supplying them: a Push consults
        /// p.18's immunity and the Move-Through permission, and a direct Move consults the same
        /// obstacle contract Movement does. <see cref="RuleModificationSet.Empty"/> is the named
        /// no-modification value, so a genuinely rule-free caller states it rather than inheriting it.
        /// </summary>
        public RuleModificationSet Modifications { get; }

        /// <summary>
        /// The card definitions and instances this resolution may read. Required since milestone 7.1a,
        /// which added the first capability that reads card state rather than only card identities: a
        /// discard derives its candidates from the chooser's actual hand, and must derive them again
        /// when the answer arrives, when the option walk asks what the node would do, and when a
        /// snapshot restores the paused choice. <see cref="EffectCardState.Absent"/> is the named
        /// no-card-state value, so a caller with none states it rather than inheriting an empty hand.
        /// </summary>
        public EffectCardState CardState { get; }

        /// <summary>Milestone 6.2's token and marker collaborators, or <c>null</c> outside a match.</summary>
        public MatchComponents Components { get; }

        /// <summary>
        /// Milestone 7.2b's items, or <c>null</c> in a composition that holds none. A capability that
        /// reads a printed value composes p.16's Item Bonus onto it, and must compose the identical one
        /// at every point it can be asked - Skill start, repeated-Skill start, the option-feasibility
        /// walk, the choice-answer path, and snapshot restore's candidate re-derivation - or a paused
        /// Radius would come back a ring smaller than the one the player was offered.
        /// </summary>
        public GuardsDigital.Core.Rules.Progression.HeroItemState HeroItems { get; }

        /// <summary>Milestone 8.19b's live round-owned query and resource state.</summary>
        public EffectRosterState RosterState { get; }
    }

    /// <summary>
    /// Walks an effect definition's node table, pausing on authoritative pending choices and resuming
    /// from the frame alone.
    ///
    /// Static and frame-in / frame-out by construction. That is not a style preference: it is how
    /// <c>CORE_MODEL.md</c>'s sixth invariant - any resolution awaiting a choice is serializable data,
    /// never a delegate, iterator, coroutine, closure, or UI callback - is made structurally true
    /// rather than merely observed. There is no instance to hold a continuation in.
    /// </summary>
    public static class EffectResolutionSession
    {
        /// <summary>
        /// Bounds a single Advance pass. A definition that cycles is already refused at content load,
        /// so exceeding this means the walker itself is wrong; failing loudly beats hanging a match.
        /// </summary>
        private const int MaximumStepsPerPass = 1024;

        public static EffectResolutionStepResult Start(
            EffectResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            HeroId controllerId,
            ActionInvocationContext invocationContext,
            EffectResolutionContext context,
            RepeatContinuation repeat = default,
            EffectNodeId entryNodeId = default)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            return Advance(
                BuildStartFrame(
                    resolutionId,
                    sourceCardInstanceId,
                    sourceDefinitionReference,
                    actionDefinitionId,
                    actorId,
                    controllerId,
                    invocationContext,
                    context,
                    repeat,
                    entryNodeId),
                context,
                new List<EffectResolutionEvent>());
        }

        /// <summary>
        /// The exact frame a start begins from, before a single node has run.
        ///
        /// Extracted at milestone 6.5af so that asking whether an effect could do anything asks it of
        /// the frame a start <em>would</em> create rather than of a hand-built approximation. The two
        /// cannot drift, because <see cref="Start"/> begins from this same method.
        ///
        /// Public because <see cref="EffectOptionFeasibilityQuery.CanApplyAnything"/> is: a caller that
        /// may ask the question must be able to build the frame the question is asked about, and this
        /// is the only sanctioned way to build one.
        /// </summary>
        public static PendingResolution BuildStartFrame(
            EffectResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            HeroId controllerId,
            ActionInvocationContext invocationContext,
            EffectResolutionContext context,
            RepeatContinuation repeat = default,
            EffectNodeId entryNodeId = default) =>
            new PendingResolution(
                resolutionId,
                sourceCardInstanceId,
                sourceDefinitionReference,
                actionDefinitionId,
                context.Definition.Reference,
                actorId,
                controllerId,
                invocationContext,
                new[] { new EffectContinuationFrame(EntryNode(context, entryNodeId), 0) },
                repeat: repeat);

        /// <summary>
        /// Where a start begins walking. The definition's own root unless a caller names a node inside
        /// it, which milestone 8.19a added for exactly one shape: text applied around a primary action
        /// is one definition entered twice - once for the question that places it, and once for the
        /// work that question placed - because what stands between those two entries is a whole
        /// Movement action with a continuation of its own.
        ///
        /// <para>The named node is checked against the definition, so an entry point is an address in
        /// trusted content rather than a claim a caller makes. Nothing else about the walk changes: the
        /// frame is the same frame, and its continuation stack is rooted at whichever node it entered.</para>
        /// </summary>
        private static EffectNodeId EntryNode(EffectResolutionContext context, EffectNodeId entryNodeId)
        {
            if (!entryNodeId.IsDefined)
            {
                return context.Definition.RootNodeId;
            }

            if (!context.Definition.TryGetNode(entryNodeId, out _))
            {
                throw new ArgumentException(
                    $"Effect definition '{context.Definition.Id}' does not declare entry node " +
                    $"'{entryNodeId}'.",
                    nameof(entryNodeId));
            }

            return entryNodeId;
        }

        /// <summary>
        /// Milestone 8.19c1. Carries on a frame that stopped for something other than a question.
        ///
        /// <para><see cref="Answer"/> is the entry for a frame holding a
        /// <see cref="PendingEffectChoice"/>, and every pause before this milestone held one. An
        /// after-push trigger stops the enclosing effect with no question outstanding - the question,
        /// if the trigger asks one, belongs to the trigger's own frame - so resuming it is not
        /// answering anything and must not record a choice. This is that entry: the same walk, from the
        /// position the frame already holds.</para>
        /// </summary>
        public static EffectResolutionStepResult Continue(
            PendingResolution frame,
            EffectResolutionContext context)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (frame.PendingChoice != null)
            {
                throw new InvalidOperationException(
                    "A frame holding a question is resumed by answering it, not by carrying on past it.");
            }

            return Advance(frame, context, new List<EffectResolutionEvent>());
        }

        public static EffectResolutionStepResult Answer(
            PendingResolution frame,
            EffectChoiceCommand command,
            EffectResolutionContext context)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var choice = frame.PendingChoice;
            if (choice == null)
            {
                return EffectResolutionStepResult.Reject(
                    EffectResolutionRejectionReason.NoPendingChoice, frame);
            }

            if (!command.ResolutionId.Equals(choice.ResolutionId) ||
                !command.ChoiceId.Equals(choice.ChoiceId))
            {
                return EffectResolutionStepResult.Reject(
                    EffectResolutionRejectionReason.StaleChoice, frame);
            }

            // Milestone 7.1a: whose decision this is, asked before what the decision was.
            //
            // Until a chooser could be someone other than the actor there was nobody to mistake, and
            // this reason was declared but unreachable. Now that a Skill can require another hero to
            // discard, the actor submitting that hero's answer is a real command to refuse, and it is
            // refused as the wrong chooser rather than as an invalid choice, because the difference is
            // exactly what a player needs told. The frame is returned untouched, so the choice still
            // stands and the hero whose decision it is can still make it.
            if (!command.AnsweringHeroId.Equals(choice.ChooserId))
            {
                return EffectResolutionStepResult.Reject(
                    EffectResolutionRejectionReason.WrongChooser, frame);
            }

            switch (command.Kind)
            {
                case EffectChoiceCommandKind.Select:
                    if (!choice.SelectInvocations.Any(invocation => invocation.Equals(command)))
                    {
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.InvalidChoice, frame);
                    }

                    return Pause(frame, choice.WithSelected(
                        choice.SelectedCandidates.Concat(new[] { command.Candidate })));

                case EffectChoiceCommandKind.Deselect:
                    if (!choice.DeselectInvocations.Any(invocation => invocation.Equals(command)))
                    {
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.InvalidChoice, frame);
                    }

                    return Pause(frame, choice.WithSelected(
                        choice.SelectedCandidates.Where(candidate => !candidate.Equals(command.Candidate))));

                case EffectChoiceCommandKind.Confirm:
                    if (choice.ConfirmInvocation == null || !choice.ConfirmInvocation.Equals(command))
                    {
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.IncompleteSelection, frame);
                    }

                    return Resume(frame, choice, EffectChoiceCommandKind.Confirm, context);

                case EffectChoiceCommandKind.Decline:
                    if (choice.DeclineInvocation == null || !choice.DeclineInvocation.Equals(command))
                    {
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.InvalidChoice, frame);
                    }

                    return Resume(frame, choice, EffectChoiceCommandKind.Decline, context);

                case EffectChoiceCommandKind.Stop:
                    if (choice.StopInvocation == null || !choice.StopInvocation.Equals(command))
                    {
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.InvalidChoice, frame);
                    }

                    return Stop(frame, choice, context);

                default:
                    return EffectResolutionStepResult.Reject(
                        EffectResolutionRejectionReason.InvalidChoice, frame);
            }
        }

        private static EffectResolutionStepResult Pause(
            PendingResolution frame,
            PendingEffectChoice choice) =>
            EffectResolutionStepResult.Accept(
                frame.With(frame.Continuation, frame.CompletedOutcomes, frame.AcceptedChoices, choice),
                false,
                null);

        /// <summary>
        /// Milestone 6.5ae: closes a <c>Choose</c> whose every option was refused, on the player's
        /// own command.
        ///
        /// This is the stop milestone 6.5ac performed unasked at the node itself, moved rather than
        /// rewritten, so accepting the invocation produces exactly what the unprompted stop produced:
        /// the same node result, the same ordered facts, and therefore the same card resolution and
        /// turn hand-off downstream. What changed is when the action ends, not what ending it means.
        ///
        /// It records no <see cref="EffectChoiceRecord"/>. That type closes on Confirm or Decline,
        /// and the path this replaces recorded nothing either - a stop answers no question, so there
        /// is no answer for a replay to reproduce.
        /// </summary>
        private static EffectResolutionStepResult Stop(
            PendingResolution frame,
            PendingEffectChoice choice,
            EffectResolutionContext context)
        {
            var continuation = frame.Continuation.ToList();
            var outcomes = frame.CompletedOutcomes.ToList();
            var events = new List<EffectResolutionEvent>();

            Record(outcomes, events, choice.NodeId, EffectExecutionResultKind.ImpossibleMandatory);
            continuation.Clear();
            return EffectResolutionStepResult.Accept(
                Rebuild(frame, continuation, outcomes, frame.AcceptedChoices), true, events,
                RepeatedActionQuery.Requested(context.Definition, frame, outcomes),
                RecordedLanePushQuery.Recorded(context.Definition, outcomes),
                RequestedHeroDefeatQuery.Requested(context.Definition, outcomes),
                RequestedMinionDefeatQuery.Requested(context.Definition, outcomes),
                        RequestedAttackTargetQuery.Bound(context.Definition, outcomes),
                        AroundActionPlacementQuery.Placed(context.Definition, outcomes));
        }

        private static EffectResolutionStepResult Resume(
            PendingResolution frame,
            PendingEffectChoice choice,
            EffectChoiceCommandKind resolvedBy,
            EffectResolutionContext context)
        {
            var node = context.Definition.GetNode(choice.NodeId);
            var continuation = frame.Continuation.ToList();
            var outcomes = frame.CompletedOutcomes.ToList();
            var choices = frame.AcceptedChoices.ToList();
            var events = new List<EffectResolutionEvent>();

            choices.Add(new EffectChoiceRecord(
                choice.NodeId, choice.ChoiceId, resolvedBy, choice.SelectedCandidates));

            if (resolvedBy == EffectChoiceCommandKind.Decline)
            {
                Pop(continuation);
                Record(outcomes, events, node.Id, EffectExecutionResultKind.DeclinedOptional);
                return Advance(Rebuild(frame, continuation, outcomes, choices), context, events);
            }

            switch (node.Kind)
            {
                case EffectNodeKind.Optional:
                    AdvanceTop(continuation);
                    continuation.Add(new EffectContinuationFrame(node.ChildNodeIds[0], 0));
                    break;

                case EffectNodeKind.Choose:
                    AdvanceTop(continuation);
                    // Pushed in reverse so the selected options execute in the definition's own order.
                    foreach (var candidate in node.ChildNodeIds
                        .Where(child => choice.SelectedCandidates.Any(selected =>
                            selected.Kind == EffectChoiceCandidateKind.Option &&
                            selected.OptionNodeId.Equals(child)))
                        .Reverse())
                    {
                        continuation.Add(new EffectContinuationFrame(candidate, 0));
                    }

                    break;

                case EffectNodeKind.RepeatAction:
                    // Milestone 6.7. The request is the whole of what confirming does; the second
                    // instance is created by the card action once this one's boundary is clean. The
                    // node is popped here rather than re-entered, because re-entering would reach the
                    // same `may` and ask again.
                    Pop(continuation);
                    Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied);
                    break;

                // Milestone 8.5. A predicate that asked a question can be answered, for the same reason
                // its non-Applied results are routed at all: the alternative is a frame that paused on
                // a node this switch refuses, which no command could ever close. Nothing in this build
                // pauses a predicate - a boolean query has nobody to ask - so this is the path that
                // keeps the routing above honest rather than one current content reaches.
                case EffectNodeKind.Conditional:
                    var predicateSelection = choice.SelectedCandidates
                        .Select(candidate => candidate.Value)
                        .ToArray();
                    var resumedPredicate = context.Catalog.Load(node.Capability).Resume(
                        BuildCapabilityContext(frame, node, outcomes, context), predicateSelection);
                    if (resumedPredicate.Kind != EffectExecutionResultKind.Applied)
                    {
                        var routedPredicate = ApplyCapabilityResult(
                            frame, node, resumedPredicate, continuation, outcomes, choices, events,
                            context);
                        if (routedPredicate != null)
                        {
                            return routedPredicate;
                        }

                        break;
                    }

                    Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied, resumedPredicate);
                    var resumedBranch = ReadsTrue(resumedPredicate)
                        ? node.ChildNodeIds[0]
                        : node.ChildNodeIds.Count > 1 ? node.ChildNodeIds[1] : default;
                    AdvanceTop(continuation);
                    if (resumedBranch.IsDefined)
                    {
                        continuation.Add(new EffectContinuationFrame(resumedBranch, 0));
                    }

                    break;

                case EffectNodeKind.Capability:
                    var selected = choice.SelectedCandidates
                        .Select(candidate => candidate.Value)
                        .ToArray();

                    // Milestone 6.8l: eligibility is read again here rather than trusted from the
                    // offer, for the reason the selection handler re-derives its own targets in
                    // Resume - what was true when the choice opened is not what this answer applies
                    // to. Rejecting leaves the frame and its pending choice exactly as they were, so
                    // the decision is still there to be made.
                    if (!EffectSelectionEligibilityQuery.AreStillEligible(
                            node, selected, frame, outcomes, context))
                    {
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.InvalidChoice, frame);
                    }

                    var handler = context.Catalog.Load(node.Capability);
                    var resumed = handler.Resume(
                        BuildCapabilityContext(frame, node, outcomes, context), selected);
                    frame = CaptureBoundTarget(frame, node, resumed, context);
                    var terminal = ApplyCapabilityResult(
                        frame, node, resumed, continuation, outcomes, choices, events, context);
                    if (terminal != null)
                    {
                        return terminal;
                    }

                    // A resumed capability that simply applied leaves the rest of the effect to run.
                    break;

                default:
                    return EffectResolutionStepResult.Reject(
                        EffectResolutionRejectionReason.InvalidChoice, frame);
            }

            return Advance(Rebuild(frame, continuation, outcomes, choices), context, events);
        }

        private static EffectResolutionStepResult Advance(
            PendingResolution frame,
            EffectResolutionContext context,
            List<EffectResolutionEvent> events)
        {
            var continuation = frame.Continuation.ToList();
            var outcomes = frame.CompletedOutcomes.ToList();
            var choices = frame.AcceptedChoices.ToList();

            for (var step = 0; step < MaximumStepsPerPass; step++)
            {
                if (continuation.Count == 0)
                {
                    return EffectResolutionStepResult.Accept(
                        Rebuild(frame, continuation, outcomes, choices), true, events,
                        RepeatedActionQuery.Requested(context.Definition, frame, outcomes),
                        RecordedLanePushQuery.Recorded(context.Definition, outcomes),
                        RequestedHeroDefeatQuery.Requested(context.Definition, outcomes),
                        RequestedMinionDefeatQuery.Requested(context.Definition, outcomes),
                        RequestedAttackTargetQuery.Bound(context.Definition, outcomes),
                        AroundActionPlacementQuery.Placed(context.Definition, outcomes));
                }

                var top = continuation[continuation.Count - 1];
                if (!context.Definition.TryGetNode(top.NodeId, out var node))
                {
                    return EffectResolutionStepResult.Reject(
                        EffectResolutionRejectionReason.UnknownNode, frame);
                }

                switch (node.Kind)
                {
                    case EffectNodeKind.Sequence:
                        if (top.ChildIndex < node.ChildNodeIds.Count)
                        {
                            AdvanceTop(continuation);
                            continuation.Add(new EffectContinuationFrame(
                                node.ChildNodeIds[top.ChildIndex], 0));
                            continue;
                        }

                        Pop(continuation);
                        Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied);
                        continue;

                    case EffectNodeKind.Optional:
                        if (top.ChildIndex == 0)
                        {
                            if (node.FailurePolicy == EffectFailurePolicy.IfAble)
                            {
                                // `if able` never asks. It attempts, and a refusal is simply skipped.
                                AdvanceTop(continuation);
                                continuation.Add(new EffectContinuationFrame(node.ChildNodeIds[0], 0));
                                continue;
                            }

                            return PauseOn(frame, continuation, outcomes, choices, events, BuildChoice(
                                frame, node, context, EffectChoiceKind.ConfirmOrDecline,
                                Array.Empty<EffectChoiceCandidate>(), 0, 0));
                        }

                        Pop(continuation);
                        Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied);
                        continue;

                    case EffectNodeKind.Choose:
                        if (top.ChildIndex == 0)
                        {
                            // Milestone 6.5ac: an option the chooser cannot perform is refused rather
                            // than offered, so no card is spent on a mode that was never going to do
                            // anything.
                            var unavailable = EffectOptionFeasibilityQuery.FindUnperformableOptions(
                                node, frame, outcomes, context);
                            var everyOptionRefused = unavailable.Count == node.ChildNodeIds.Count;
                            if (everyOptionRefused &&
                                node.FailurePolicy != EffectFailurePolicy.Mandatory)
                            {
                                // `if able`, `may`, and `up to` treat a node they cannot take as
                                // simply not taken, so there is nothing here for a player to close.
                                Pop(continuation);
                                Record(outcomes, events, node.Id,
                                    EffectExecutionResultKind.DeclinedOptional);
                                continue;
                            }

                            // Milestone 6.5ae: a mandatory node whose every option is refused pauses
                            // with all of them greyed and offers p.10's stop, which the player takes.
                            // Milestone 6.5ac stopped here without asking, because a mandatory Choose
                            // has no decline and every other control is withheld while a choice
                            // stands; the stop is the command that position was missing.
                            return PauseOn(frame, continuation, outcomes, choices, events, BuildChoice(
                                frame, node, context, EffectChoiceKind.SelectOption,
                                node.ChildNodeIds.Select(EffectChoiceCandidate.Option).ToArray(),
                                node.MinimumSelections, node.MaximumSelections,
                                unavailableCandidates: unavailable,
                                canStop: everyOptionRefused));
                        }

                        Pop(continuation);
                        Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied);
                        continue;

                    case EffectNodeKind.Conditional:
                        if (top.ChildIndex == 0)
                        {
                            // Milestone 8.5, closing the 7.9a review's F-CARD-07. A predicate used to be
                            // executed for one boolean and everything else it returned was discarded:
                            // its typed outcomes never reached the outcome list, so no later node could
                            // bind to what it had established, and a handler that paused, refused, or
                            // declined arrived here as "no true outcome" and was read as false. Those
                            // are three different answers being spelled the same way, and the false one
                            // silently takes the else branch of a rule the card actually printed.
                            //
                            // So the result is routed through the one place that knows what each kind
                            // means, and only an Applied one decides a branch. The facts are recorded
                            // here rather than when the node finishes, because the branch below may
                            // pause and this pass may not come back: the outcome list is what survives,
                            // and a second factless record at the end would shadow this one, since an
                            // input binding reads the last outcome a node published.
                            var predicate = context.Catalog.Load(node.Capability)
                                .Execute(BuildCapabilityContext(frame, node, outcomes, context));
                            if (predicate.Kind != EffectExecutionResultKind.Applied)
                            {
                                var routed = ApplyCapabilityResult(
                                    frame, node, predicate, continuation, outcomes, choices, events,
                                    context);
                                if (routed != null)
                                {
                                    return routed;
                                }

                                continue;
                            }

                            Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied, predicate);
                            var branch = ReadsTrue(predicate)
                                ? node.ChildNodeIds[0]
                                : node.ChildNodeIds.Count > 1 ? node.ChildNodeIds[1] : default;
                            AdvanceTop(continuation);
                            if (branch.IsDefined)
                            {
                                continuation.Add(new EffectContinuationFrame(branch, 0));
                            }

                            continue;
                        }

                        Pop(continuation);
                        continue;

                    case EffectNodeKind.RepeatAction:
                        // Milestone 6.7. Nothing is performed here. An eligible `may` opens p.10's
                        // confirm-or-decline; an eligible `if able` requests without asking; and an
                        // instance whose chain has run the node's declared maximum skips in silence,
                        // which is the whole bound and is why this node kind refuses to be mandatory.
                        if (!RepeatedActionQuery.CanRepeat(frame, node))
                        {
                            Pop(continuation);
                            Record(outcomes, events, node.Id,
                                EffectExecutionResultKind.DeclinedOptional);
                            continue;
                        }

                        if (node.FailurePolicy == EffectFailurePolicy.May)
                        {
                            return PauseOn(frame, continuation, outcomes, choices, events, BuildChoice(
                                frame, node, context, EffectChoiceKind.ConfirmOrDecline,
                                Array.Empty<EffectChoiceCandidate>(), 0, 0));
                        }

                        Pop(continuation);
                        Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied);
                        continue;

                    case EffectNodeKind.Capability:
                        var handler = context.Catalog.Load(node.Capability);
                        var capabilityContext = BuildCapabilityContext(frame, node, outcomes, context);

                        // Milestone 8.6. `on the same target` means the repeated instance does not
                        // choose here: the space the inherited piece is standing on now is handed to
                        // this node's own Resume, which re-derives its candidates and refuses a target
                        // that is no longer eligible exactly as it would refuse a stale answer to a
                        // real choice. Routing it through Resume rather than through a narrowed Execute
                        // is what keeps the binding capability-agnostic - nothing here knows what a
                        // target is - and what makes a bound target that has moved out of reach an
                        // impossible step rather than a silently free one.
                        var result = RepeatedActionQuery.TryReadBoundTarget(
                            context.Definition, node, frame, context.Components, out var boundTarget)
                            ? handler.Resume(capabilityContext, new[] { boundTarget })
                            : handler.Execute(capabilityContext);
                        frame = CaptureBoundTarget(frame, node, result, context);
                        var applied = ApplyCapabilityResult(
                            frame, node, result, continuation, outcomes, choices, events, context);
                        if (applied != null)
                        {
                            return applied;
                        }

                        continue;

                    default:
                        return EffectResolutionStepResult.Reject(
                            EffectResolutionRejectionReason.UnknownNode, frame);
                }
            }

            throw new InvalidOperationException(
                $"Effect definition '{context.Definition.Id}' did not settle within {MaximumStepsPerPass} steps.");
        }

        /// <summary>
        /// Returns a terminal result when the capability paused or stopped the action, and null when
        /// execution should simply carry on to the next node.
        /// </summary>
        private static EffectResolutionStepResult ApplyCapabilityResult(
            PendingResolution frame,
            EffectNodeDefinition node,
            EffectExecutionResult result,
            List<EffectContinuationFrame> continuation,
            List<EffectNodeOutcome> outcomes,
            List<EffectChoiceRecord> choices,
            List<EffectResolutionEvent> events,
            EffectResolutionContext context)
        {
            switch (result.Kind)
            {
                case EffectExecutionResultKind.NeedsChoice:
                    // Milestone 6.8l: a selection that declares what it selects for offers only the
                    // objects that consumer can act on, so the prompt stops naming targets the effect
                    // would refuse. Every other capability's candidates come back exactly as offered.
                    var offered = EffectSelectionEligibilityQuery.EligibleCandidates(
                        node, result.Choice.Candidates, frame, outcomes, context);
                    if (offered.Count == 0 || offered.Count < result.Choice.MinimumSelections)
                    {
                        // Nothing left to name is the same position as nothing to name in the first
                        // place, which is what the handler itself reports as impossible. The refusal
                        // carries no result, because the one this node produced was a request for a
                        // choice that is not going to be opened.
                        return RefuseCapability(
                            frame, node, null, continuation, outcomes, choices, events, context);
                    }

                    return PauseOn(frame, continuation, outcomes, choices, events, BuildChoice(
                        frame, node, context, EffectChoiceKind.SelectTarget,
                        offered.Select(EffectChoiceCandidate.Value_).ToArray(),
                        result.Choice.MinimumSelections,
                        Math.Min(result.Choice.MaximumSelections, offered.Count),
                        result.Choice.Chooser,
                        chooserHeroOverride: result.Choice.ChooserHeroId));

                case EffectExecutionResultKind.ImpossibleMandatory:
                    return RefuseCapability(
                        frame, node, result, continuation, outcomes, choices, events, context);

                case EffectExecutionResultKind.DeclinedOptional:
                    Pop(continuation);
                    Record(outcomes, events, node.Id, EffectExecutionResultKind.DeclinedOptional, result);
                    return null;

                default:
                    Pop(continuation);
                    Record(outcomes, events, node.Id, EffectExecutionResultKind.Applied, result);

                    // Milestone 8.19c1. The node is finished and its facts are recorded before the walk
                    // stops, so what the caller resumes is the effect standing exactly after the Push -
                    // "the exact node it left" is this frame, and a later node binding to a fact this
                    // push published still finds it. Stopping happens only when a source is watching
                    // and only when a displacement actually happened, so an ordinary push is the walk
                    // it always was.
                    if (context.WatchesAppliedPushes && result.AppliedPushes.Count > 0)
                    {
                        return EffectResolutionStepResult.Accept(
                            Rebuild(frame, continuation, outcomes, choices), false, events,
                            appliedPushes: result.AppliedPushes);
                    }

                    return null;
            }
        }

        /// <summary>
        /// What a capability node that cannot be taken does to the resolution: stops it where the node
        /// is mandatory, and is skipped where p.10's `if able`, `may`, and `up to` say a step nobody
        /// can take is simply not taken. Returns a terminal result only in the first case.
        /// </summary>
        private static EffectResolutionStepResult RefuseCapability(
            PendingResolution frame,
            EffectNodeDefinition node,
            EffectExecutionResult result,
            List<EffectContinuationFrame> continuation,
            List<EffectNodeOutcome> outcomes,
            List<EffectChoiceRecord> choices,
            List<EffectResolutionEvent> events,
            EffectResolutionContext context)
        {
            Pop(continuation);
            if (node.FailurePolicy != EffectFailurePolicy.Mandatory)
            {
                Record(outcomes, events, node.Id, EffectExecutionResultKind.DeclinedOptional, result);
                return null;
            }

            // The action stops here. Everything already applied stays applied - a repeat this
            // instance already asked for included, because asking is applied work like any other.
            Record(outcomes, events, node.Id, EffectExecutionResultKind.ImpossibleMandatory, result);
            continuation.Clear();
            return EffectResolutionStepResult.Accept(
                Rebuild(frame, continuation, outcomes, choices), true, events,
                RepeatedActionQuery.Requested(context.Definition, frame, outcomes),
                RecordedLanePushQuery.Recorded(context.Definition, outcomes),
                RequestedHeroDefeatQuery.Requested(context.Definition, outcomes),
                RequestedMinionDefeatQuery.Requested(context.Definition, outcomes),
                        RequestedAttackTargetQuery.Bound(context.Definition, outcomes),
                        AroundActionPlacementQuery.Placed(context.Definition, outcomes));
        }

        private static EffectResolutionStepResult PauseOn(
            PendingResolution frame,
            List<EffectContinuationFrame> continuation,
            List<EffectNodeOutcome> outcomes,
            List<EffectChoiceRecord> choices,
            List<EffectResolutionEvent> events,
            PendingEffectChoice choice) =>
            EffectResolutionStepResult.Accept(
                frame.With(continuation, outcomes, choices, choice), false, events);

        private static PendingEffectChoice BuildChoice(
            PendingResolution frame,
            EffectNodeDefinition node,
            EffectResolutionContext context,
            EffectChoiceKind kind,
            IReadOnlyList<EffectChoiceCandidate> candidates,
            int minimumSelections,
            int maximumSelections,
            EffectChooserRole chooserOverride = EffectChooserRole.None,
            IReadOnlyList<EffectChoiceCandidate> unavailableCandidates = null,
            bool canStop = false,
            HeroId chooserHeroOverride = default) =>
            new PendingEffectChoice(
                frame.ResolutionId,
                new EffectChoiceId($"node-{node.Id.Value}"),
                node.Id,
                // Milestone 7.1a: a handler that names the exact hero has already answered this, and
                // a role would only re-derive a worse version of the same answer.
                chooserHeroOverride.IsDefined
                    ? chooserHeroOverride
                    : ResolveChooser(
                        chooserOverride != EffectChooserRole.None ? chooserOverride : node.Chooser,
                        frame,
                        context),
                kind,
                candidates,
                minimumSelections,
                maximumSelections,
                null,
                unavailableCandidates,
                canStop);

        private static HeroId ResolveChooser(
            EffectChooserRole role,
            PendingResolution frame,
            EffectResolutionContext context)
        {
            switch (role)
            {
                case EffectChooserRole.Controller:
                    return frame.ControllerId;
                case EffectChooserRole.OpposingHero:
                    if (!context.OpposingHeroId.IsDefined)
                    {
                        throw new InvalidOperationException(
                            "An effect node names the opposing hero as chooser, but none was supplied.");
                    }

                    return context.OpposingHeroId;
                default:
                    return frame.ActorId;
            }
        }

        /// <summary>
        /// Whether an applied predicate reported true. Only the boolean facts it published are read,
        /// and only from a result that applied: milestone 8.5 made "the predicate could not be asked"
        /// a separate answer from "the predicate said no", because a `Conditional` reading the second
        /// where the first is true takes the else branch of a rule the card printed.
        /// </summary>
        private static bool ReadsTrue(EffectExecutionResult predicate) =>
            predicate.Outcomes.Any(outcome =>
                outcome.Value.Kind == EffectValueKind.Boolean && outcome.Value.BooleanValue);

        internal static EffectCapabilityContext BuildCapabilityContext(
            PendingResolution frame,
            EffectNodeDefinition node,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context) =>
            BuildCapabilityContext(
                frame, node, outcomes, context, ResolveInputBindings(node, frame, outcomes), null);

        /// <summary>
        /// The same context, with some inputs answered rather than resolved.
        ///
        /// Milestone 6.8l asks a node what it would do with a subject a prior selection has not chosen
        /// yet, so it supplies that subject instead of the nothing the binding currently resolves to.
        /// It builds the context here rather than assembling one of its own, because everything else a
        /// handler is allowed to see - the actor, the source card, the round, the modifications, the
        /// components - has to be the same in a probe as in the execution it is asking about, and one
        /// construction site is what keeps it so.
        /// </summary>
        internal static EffectCapabilityContext BuildCapabilityContext(
            PendingResolution frame,
            EffectNodeDefinition node,
            IReadOnlyList<EffectNodeOutcome> outcomes,
            EffectResolutionContext context,
            IReadOnlyDictionary<string, EffectValue> resolvedInputs,
            IReadOnlyDictionary<string, EffectValue> substitutedInputs)
        {
            var inputs = resolvedInputs;
            if (substitutedInputs != null && substitutedInputs.Count > 0)
            {
                var merged = new Dictionary<string, EffectValue>(StringComparer.Ordinal);
                foreach (var entry in resolvedInputs)
                {
                    merged[entry.Key] = entry.Value;
                }

                foreach (var entry in substitutedInputs)
                {
                    merged[entry.Key] = entry.Value;
                }

                inputs = merged;
            }

            return new EffectCapabilityContext(
                node.Id,
                node.FailurePolicy,
                frame.ActorId,
                frame.ControllerId,
                frame.SourceCardInstanceId,
                frame.SourceDefinitionReference,
                frame.ActionDefinitionId,
                inputs,
                context.RoundIndex,
                context.TurnIndex,
                context.Participants,
                context.Timing,
                context.Modifications,
                context.CardState,
                context.EnclosingActionKind,
                context.Components,
                context.HeroItems,
                AnswersAlreadyGiven(frame, node),
                context.EnclosingAttack,
                context.RosterState,
                node.ImmunityOverrides,
                node.Outputs);
        }

        /// <summary>
        /// Milestone 8.8. What this node has already been told, oldest first, so a capability that asks
        /// two questions can complete the work both answers describe.
        ///
        /// It is read off the frame's own accepted choices rather than remembered anywhere: those are
        /// what a snapshot persists and a replay reproduces, so a resolution restored between one
        /// question and the next arrives with the same answers it paused holding. A declined choice
        /// records no selection and never reaches a handler, because declining pops the node.
        ///
        /// Internal for the reason <see cref="ResolveInputBindings"/> is: snapshot restore re-asks the
        /// handler what it is offering, and a handler on its second question has to be asked through
        /// the same path live execution asks it through, or the restored pause is the first question
        /// again.
        /// </summary>
        internal static IReadOnlyList<IReadOnlyList<EffectValue>> AnswersAlreadyGiven(
            PendingResolution frame, EffectNodeDefinition node) =>
            frame.AcceptedChoices
                .Where(record => record.NodeId.Equals(node.Id))
                .Select(record => (IReadOnlyList<EffectValue>)record.Selected
                    .Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value)
                    .Select(candidate => candidate.Value)
                    .ToArray())
                .ToArray();

        /// <summary>
        /// The exact input dictionary live execution hands a capability. Internal so that snapshot
        /// restore re-derives a paused choice's candidates through this same path, rebuilding the
        /// bindings from the persisted completed outcomes rather than re-asking with none.
        /// </summary>
        internal static IReadOnlyDictionary<string, EffectValue> ResolveInputBindings(
            EffectNodeDefinition node,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            var inputs = new Dictionary<string, EffectValue>(StringComparer.Ordinal);
            foreach (var binding in node.Inputs)
            {
                inputs[binding.ParameterName] = ResolveInput(binding, frame, outcomes);
            }

            return inputs;
        }

        private static EffectValue ResolveInput(
            EffectInputBinding binding,
            PendingResolution frame,
            IReadOnlyList<EffectNodeOutcome> outcomes)
        {
            switch (binding.SourceKind)
            {
                case EffectBindingSourceKind.Literal:
                    return binding.Literal;

                case EffectBindingSourceKind.Role:
                    switch (binding.Role)
                    {
                        case EffectRoleKind.Actor:
                        case EffectRoleKind.Chooser:
                            return EffectValue.Hero(frame.ActorId);
                        case EffectRoleKind.Controller:
                            return EffectValue.Hero(frame.ControllerId);
                        case EffectRoleKind.SourceCard:
                            return EffectValue.Card(frame.SourceCardInstanceId);
                        default:
                            return default;
                    }

                case EffectBindingSourceKind.NodeOutput:
                    var source = outcomes.LastOrDefault(outcome =>
                        outcome.NodeId.Equals(binding.SourceNodeId));
                    if (source == null)
                    {
                        return default;
                    }

                    foreach (var output in source.Outputs)
                    {
                        if (string.Equals(output.OutputName, binding.SourceOutputName, StringComparison.Ordinal))
                        {
                            return output.Value;
                        }
                    }

                    return default;

                default:
                    return default;
            }
        }

        private static void Record(
            ICollection<EffectNodeOutcome> outcomes,
            ICollection<EffectResolutionEvent> events,
            EffectNodeId nodeId,
            EffectExecutionResultKind kind,
            EffectExecutionResult result = null)
        {
            outcomes.Add(new EffectNodeOutcome(nodeId, kind, result?.Outcomes));
            events.Add(new EffectResolutionEvent(
                nodeId, kind, result?.Outcomes, result?.CheckpointsBefore, result?.CheckpointsAfter));
        }

        private static void AdvanceTop(IList<EffectContinuationFrame> continuation) =>
            continuation[continuation.Count - 1] = continuation[continuation.Count - 1].Advanced();

        private static void Pop(IList<EffectContinuationFrame> continuation) =>
            continuation.RemoveAt(continuation.Count - 1);

        private static PendingResolution Rebuild(
            PendingResolution frame,
            IEnumerable<EffectContinuationFrame> continuation,
            IEnumerable<EffectNodeOutcome> outcomes,
            IEnumerable<EffectChoiceRecord> choices) =>
            frame.With(continuation, outcomes, choices, null);

        /// <summary>
        /// Milestone 8.6. Records onto the frame which piece a selection just named, for the one
        /// definition shape that will need it: a card whose repeat says `on the same target`.
        ///
        /// It happens here, at the moment the selection applies, and not at the moment the repeat asks,
        /// because those two moments are separated by everything the instance does - and for Wasp's
        /// orbit skills what it does is move the piece. A coordinate remembered now would name whatever
        /// is standing there later; the piece does not change when it moves.
        ///
        /// Every other definition passes through unchanged, because <c>BindsSelection</c> is false for
        /// every node no repeat binds.
        /// </summary>
        private static PendingResolution CaptureBoundTarget(
            PendingResolution frame,
            EffectNodeDefinition node,
            EffectExecutionResult result,
            EffectResolutionContext context) =>
            RepeatedActionQuery.TryCaptureBoundTarget(
                context.Definition, node, result, context.Components, out var boundTarget)
                ? frame.WithRepeat(frame.Repeat.Binding(boundTarget))
                : frame;
    }
}
