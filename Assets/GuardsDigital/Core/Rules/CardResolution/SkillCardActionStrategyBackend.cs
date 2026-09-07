using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// The fourth concrete card-action backend. It owns Skill's pending state and its translation to
    /// and from the layers around it; the rules of what a Skill actually does live in the effect
    /// definition and its registered capabilities, not here.
    ///
    /// That division is what acceptance check 4 asks for. A later verified capability adds a catalog
    /// entry and a handler; it does not touch this file, the dispatcher, or the turn authority. The
    /// only switch on effect-node kind in the whole engine is inside
    /// <see cref="EffectResolutionSession"/>, which is the component whose job that is.
    /// </summary>
    internal sealed class SkillCardActionStrategyBackend
    {
        private readonly EffectCapabilityCatalog catalog;
        private PendingResolution pendingResolution;

        /// <summary>
        /// Milestone 7.2b: the same item state the round authority owns, bound by the dispatcher for
        /// the reason the Attack, Defense and Movement backends' is. A capability that reads its card's
        /// printed Radius composes p.16's Item Bonus onto it, and every one of the frame's five ask
        /// points takes the state from here, so a paused disk cannot come back derived from a different
        /// set of items than the one the player was offered. Absent in a composition with no items.
        /// </summary>
        private GuardsDigital.Core.Rules.Progression.HeroItemState heroItems;
        private EffectRosterState rosterState = EffectRosterState.Absent;

        internal SkillCardActionStrategyBackend(EffectCapabilityCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        internal void BindHeroItems(GuardsDigital.Core.Rules.Progression.HeroItemState items)
        {
            heroItems = items;
        }

        internal void BindRosterState(EffectRosterState state)
        {
            rosterState = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>
        /// The match items an effect this backend resolves composes p.16's bonus from. Milestone 8.5
        /// reads it from here rather than binding a second copy, because card text applied inside an
        /// Attack has to compose the identical bonus a Skill running the same capability would.
        /// </summary>
        internal GuardsDigital.Core.Rules.Progression.HeroItemState HeroItems => heroItems;

        private SkillCardActionStrategyBackend(
            EffectCapabilityCatalog catalog,
            PendingResolution pendingResolution)
            : this(catalog)
        {
            this.pendingResolution = pendingResolution;
        }

        public PendingResolution PendingResolution => pendingResolution;

        public PendingEffectChoice PendingChoice => pendingResolution?.PendingChoice;

        public bool HasPendingResolution => pendingResolution != null;

        /// <summary>
        /// Milestone 8.19c1. Whether any unlocked Ultimate in these cards declares an
        /// `after-push-trigger@1`, which is what makes the resolver stop at a Push that applied.
        ///
        /// <para>It is deliberately the coarse question. Whether a particular source concerns a
        /// particular displaced object is <c>AfterPushTriggerQuery</c>'s, asked by the authority that
        /// holds the board and the participants; all this decides is whether the walk pauses so that
        /// question can be asked at all. A match whose sources all turn out not to apply resumes
        /// immediately and reaches exactly the same nodes in exactly the same order.</para>
        /// </summary>
        private static bool WatchesAppliedPushes(
            IReadOnlyList<CardDefinition> definitions,
            IEnumerable<CardInstance> instances) =>
            GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(definitions, instances)
                .Ultimates.Any(ultimate => ultimate.Rule.AfterPush.IsDeclared);

        internal SkillBackendRestoreState CaptureRestoreState() =>
            pendingResolution == null ? null : new SkillBackendRestoreState(pendingResolution);

        internal static SkillCardActionStrategyBackend Restore(
            EffectCapabilityCatalog catalog,
            SkillBackendRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            state.Deconstruct(out var pending);
            return new SkillCardActionStrategyBackend(catalog, pending);
        }

        public CardActionResolutionResult StartTurnSkill(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            MatchComponents components,
            int roundIndex,
            int turnIndex,
            out PendingRepeatedAction repeat,
            out LanePushCauseKind lanePushCause,
            out IReadOnlyList<RuleObject> appliedPushes,
            out IReadOnlyList<HeroId> requestedHeroDefeats,
            // Milestone 8.35z50. A borrow this Skill's own text asked for, surfaced beside the repeat
            // and never inside it: a borrowed primary is not a p.10 repeat (RULING 22).
            out GuardsDigital.Core.Rules.EffectResolution.BorrowRequest borrowRequested)
        {
            repeat = null;
            borrowRequested = default;
            lanePushCause = LanePushCauseKind.None;
            appliedPushes = Array.Empty<RuleObject>();
            requestedHeroDefeats = Array.Empty<HeroId>();
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("A Skill requires a resolution ID.", nameof(resolutionId));
            }

            if (pendingResolution != null)
            {
                return Reject(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var action = ActionOfferQuery.FindActionDefinition(offer, materializedDefinitions);
            if (action == null || action.Kind != ActionKind.Skill || action.EffectRoot == null)
            {
                return Reject(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var context = new EffectResolutionContext(
                action.EffectRoot, catalog, participants, timing, roundIndex, turnIndex, modifications,
                EffectCardState.Of(materializedDefinitions, materializedInstances), ActionKind.Skill,
                components: components, heroItems: heroItems, rosterState: rosterState,
                watchesAppliedPushes: WatchesAppliedPushes(
                    materializedDefinitions, materializedInstances));
            var step = EffectResolutionSession.Start(
                new EffectResolutionId(resolutionId.Value),
                offer.SourceCardInstanceId,
                offer.SourceDefinitionReference,
                offer.ActionDefinitionId,
                offer.ActorId,
                offer.ActorId,
                offer.InvocationContext,
                context);

            if (step.RepeatRequested.IsRequested)
            {
                repeat = PendingRepeatedAction.Mint(
                    resolutionId, offer.SourceCardInstanceId, offer.SourceDefinitionReference,
                    offer.ActionDefinitionId, action.EffectRoot.Reference, offer.ActorId, offer.ActorId,
                    step.RepeatRequested.Continuation);
            }

            borrowRequested = step.BorrowRequested;
            lanePushCause = step.LanePushCause;
            appliedPushes = step.AppliedPushes;
            requestedHeroDefeats = step.RequestedHeroDefeats;
            return Complete(
                step, resolutionId, offer.SourceCardInstanceId, offer.ActionDefinitionId,
                offer.ActorId, true);
        }

        /// <summary>
        /// Milestone 6.7: starts the second instance a completed action asked for.
        ///
        /// It takes the identities the finished instance recorded rather than an
        /// <see cref="ActionOffer"/>, because a repeated action is created by a rule and is never
        /// offered: the dispatcher's own start gate admits the two printed invocation contexts only, so
        /// nothing that reads offers can reach this. The action definition is resolved from the trusted
        /// card definitions, and the repeated instance runs under
        /// <see cref="ActionInvocationContext.Repeated"/>, which is what makes its own Repeat node
        /// ineligible and the whole thing bounded at two instances.
        ///
        /// Every collaborator - participants, timing, modifications, components, round and turn - is the
        /// caller's current state rather than anything the first instance carried, so an Active effect
        /// the first instance created restricts the second.
        /// </summary>
        public CardActionResolutionResult StartRepeatedSkill(
            ActionResolutionId resolutionId,
            PendingRepeatedAction repeat,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            MatchComponents components,
            int roundIndex,
            int turnIndex,
            out PendingRepeatedAction furtherRepeat,
            out LanePushCauseKind lanePushCause,
            out IReadOnlyList<RuleObject> appliedPushes,
            out IReadOnlyList<HeroId> requestedHeroDefeats,
            // Milestone 8.35z50. A borrow this Skill's own text asked for, surfaced beside the repeat
            // and never inside it: a borrowed primary is not a p.10 repeat (RULING 22).
            out GuardsDigital.Core.Rules.EffectResolution.BorrowRequest borrowRequested)
        {
            furtherRepeat = null;
            borrowRequested = default;
            lanePushCause = LanePushCauseKind.None;
            appliedPushes = Array.Empty<RuleObject>();
            requestedHeroDefeats = Array.Empty<HeroId>();
            if (repeat == null) throw new ArgumentNullException(nameof(repeat));
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("A repeated Skill requires a resolution ID.", nameof(resolutionId));
            }

            if (pendingResolution != null)
            {
                return Reject(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var action = FindRepeatableAction(materializedDefinitions, repeat);
            if (action == null)
            {
                return Reject(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var context = new EffectResolutionContext(
                action.EffectRoot, catalog, participants, timing, roundIndex, turnIndex, modifications,
                EffectCardState.Of(materializedDefinitions, materializedInstances), ActionKind.Skill,
                components: components, heroItems: heroItems, rosterState: rosterState,
                watchesAppliedPushes: WatchesAppliedPushes(
                    materializedDefinitions, materializedInstances));
            var step = EffectResolutionSession.Start(
                new EffectResolutionId(resolutionId.Value),
                repeat.SourceCardInstanceId,
                repeat.SourceDefinitionReference,
                repeat.ActionDefinitionId,
                repeat.ActorId,
                repeat.ControllerId,
                ActionInvocationContext.Repeated,
                context,
                repeat.Continuation);

            // Milestone 8.6. A repeated instance can now ask for another, up to the maximum its own
            // node declares - which milestone 6.7's structural bound made impossible and 6.7's code
            // therefore never read. The chain still ends without a guard here, because the node's
            // eligibility is what stops it and the frame this instance ran under carries the count.
            if (step.RepeatRequested.IsRequested)
            {
                furtherRepeat = PendingRepeatedAction.Mint(
                    resolutionId, repeat.SourceCardInstanceId, repeat.SourceDefinitionReference,
                    repeat.ActionDefinitionId, action.EffectRoot.Reference, repeat.ActorId,
                    repeat.ControllerId, step.RepeatRequested.Continuation);
            }

            borrowRequested = step.BorrowRequested;
            lanePushCause = step.LanePushCause;
            appliedPushes = step.AppliedPushes;
            requestedHeroDefeats = step.RequestedHeroDefeats;
            return Complete(
                step, resolutionId, repeat.SourceCardInstanceId, repeat.ActionDefinitionId,
                repeat.ActorId, true, ActionInvocationContext.Repeated);
        }

        /// <summary>
        /// The Skill action a repeat names, resolved from the trusted definitions. Returns <c>null</c>
        /// wherever the payload does not describe one this build supplies, so a tampered or stale
        /// repeat is refused rather than reconstructed.
        /// </summary>
        internal static ActionDefinition FindRepeatableAction(
            IReadOnlyList<CardDefinition> definitions,
            PendingRepeatedAction repeat)
        {
            foreach (var definition in definitions)
            {
                if (!definition.Reference.Equals(repeat.SourceDefinitionReference))
                {
                    continue;
                }

                foreach (var action in definition.ActionDefinitions)
                {
                    if (action.Id.Equals(repeat.ActionDefinitionId) &&
                        action.Kind == ActionKind.Skill &&
                        action.EffectRoot != null &&
                        action.EffectRoot.Reference.Equals(repeat.EffectDefinitionReference))
                    {
                        return action;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Milestone 6.5af: whether starting this Skill could apply anything.
        ///
        /// Asked before the action starts, so nothing is created and no frame is kept - the probe
        /// builds the frame a start would begin from, hands it to the same feasibility walk milestone
        /// 6.5ac built for a mode, and drops it. The resolution identity it composes never leaves this
        /// method and never could matter: a capability is given the node, the actor, the controller,
        /// the source card, the action, its inputs, and the match state, and no resolution ID at all.
        ///
        /// Anything that is not a Skill with an effect root answers <c>true</c>. This question is about
        /// what an effect can do, and an action that has no effect is judged by the rules that already
        /// judge it.
        /// </summary>
        public bool CanApplyAnything(
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            MatchComponents components,
            int roundIndex,
            int turnIndex,
            out AppliedRuleModification blocking)
        {
            blocking = null;
            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            if (!ActionOfferQuery.TryFindActionDefinition(offer, materializedDefinitions, out var action) ||
                action.Kind != ActionKind.Skill || action.EffectRoot == null)
            {
                return true;
            }

            var context = new EffectResolutionContext(
                action.EffectRoot, catalog, participants, timing, roundIndex, turnIndex, modifications,
                EffectCardState.Of(materializedDefinitions, instances), ActionKind.Skill,
                components: components, heroItems: heroItems, rosterState: rosterState);
            return EffectOptionFeasibilityQuery.CanApplyAnything(
                EffectResolutionSession.BuildStartFrame(
                    new EffectResolutionId($"probe-{offer.SourceCardInstanceId.Value}-{offer.ActionDefinitionId.Value}"),
                    offer.SourceCardInstanceId,
                    offer.SourceDefinitionReference,
                    offer.ActionDefinitionId,
                    offer.ActorId,
                    offer.ActorId,
                    offer.InvocationContext,
                    context),
                context,
                out blocking);
        }

        public CardActionResolutionResult AnswerEffectChoice(
            EffectChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            MatchComponents components,
            int roundIndex,
            int turnIndex,
            out PendingRepeatedAction repeat,
            out LanePushCauseKind lanePushCause,
            out IReadOnlyList<RuleObject> appliedPushes,
            out IReadOnlyList<HeroId> requestedHeroDefeats,
            // Milestone 8.35z50. A borrow this Skill's own text asked for, surfaced beside the repeat
            // and never inside it: a borrowed primary is not a p.10 repeat (RULING 22).
            out GuardsDigital.Core.Rules.EffectResolution.BorrowRequest borrowRequested)
        {
            repeat = null;
            borrowRequested = default;
            lanePushCause = LanePushCauseKind.None;
            appliedPushes = Array.Empty<RuleObject>();
            requestedHeroDefeats = Array.Empty<HeroId>();
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (pendingResolution == null)
            {
                return Reject(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var effectRoot = FindEffectRoot(materializedDefinitions, pendingResolution);
            if (effectRoot == null)
            {
                return Reject(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var context = new EffectResolutionContext(
                effectRoot, catalog, participants, timing, roundIndex, turnIndex, modifications,
                EffectCardState.Of(materializedDefinitions, materializedInstances), ActionKind.Skill,
                components: components, heroItems: heroItems, rosterState: rosterState,
                watchesAppliedPushes: WatchesAppliedPushes(
                    materializedDefinitions, materializedInstances));
            var step = EffectResolutionSession.Answer(pendingResolution, command, context);
            if (!step.Accepted)
            {
                switch (step.RejectionReason)
                {
                    case EffectResolutionRejectionReason.StaleChoice:
                        return Reject(CardActionResolutionRejectionReason.StaleChoice);
                    case EffectResolutionRejectionReason.WrongChooser:
                        return Reject(CardActionResolutionRejectionReason.WrongChooser);
                    default:
                        return Reject(CardActionResolutionRejectionReason.InvalidChoice);
                }
            }

            if (step.RepeatRequested.IsRequested)
            {
                repeat = PendingRepeatedAction.Mint(
                    new ActionResolutionId(pendingResolution.ResolutionId.Value),
                    pendingResolution.SourceCardInstanceId,
                    pendingResolution.SourceDefinitionReference,
                    pendingResolution.ActionDefinitionId,
                    pendingResolution.EffectDefinitionReference,
                    pendingResolution.ActorId,
                    pendingResolution.ControllerId,
                    step.RepeatRequested.Continuation);
            }

            borrowRequested = step.BorrowRequested;
            lanePushCause = step.LanePushCause;
            appliedPushes = step.AppliedPushes;
            requestedHeroDefeats = step.RequestedHeroDefeats;
            return Complete(step, default, default, default, default, false);
        }

        /// <summary>
        /// Milestone 8.19c1. Carries a Skill on from the node it stopped at when a Push it performed
        /// was watched, once whatever watched it has finished.
        ///
        /// <para>It is not <see cref="AnswerEffectChoice"/> without a command: that method resumes a
        /// frame holding a question and records the answer, and there is no question here. This resumes
        /// the walk from exactly the position the frame already holds, which is what makes "returns the
        /// enclosing effect to the exact node it left" a property of the frame rather than of a
        /// reconstruction.</para>
        /// </summary>
        public CardActionResolutionResult ContinueAfterInterrupt(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            MatchComponents components,
            int roundIndex,
            int turnIndex,
            out PendingRepeatedAction repeat,
            out LanePushCauseKind lanePushCause,
            out IReadOnlyList<RuleObject> appliedPushes,
            out IReadOnlyList<HeroId> requestedHeroDefeats,
            // Milestone 8.35z50. A borrow this Skill's own text asked for, surfaced beside the repeat
            // and never inside it: a borrowed primary is not a p.10 repeat (RULING 22).
            out GuardsDigital.Core.Rules.EffectResolution.BorrowRequest borrowRequested)
        {
            repeat = null;
            borrowRequested = default;
            lanePushCause = LanePushCauseKind.None;
            appliedPushes = Array.Empty<RuleObject>();
            requestedHeroDefeats = Array.Empty<HeroId>();
            if (pendingResolution == null || pendingResolution.PendingChoice != null)
            {
                return Reject(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var materializedInstances = instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances));
            var effectRoot = FindEffectRoot(materializedDefinitions, pendingResolution);
            if (effectRoot == null)
            {
                return Reject(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var context = new EffectResolutionContext(
                effectRoot, catalog, participants, timing, roundIndex, turnIndex, modifications,
                EffectCardState.Of(materializedDefinitions, materializedInstances), ActionKind.Skill,
                components: components, heroItems: heroItems, rosterState: rosterState,
                watchesAppliedPushes: WatchesAppliedPushes(
                    materializedDefinitions, materializedInstances));
            var step = EffectResolutionSession.Continue(pendingResolution, context);
            if (!step.Accepted)
            {
                return Reject(CardActionResolutionRejectionReason.InvalidChoice);
            }

            if (step.RepeatRequested.IsRequested)
            {
                repeat = PendingRepeatedAction.Mint(
                    new ActionResolutionId(pendingResolution.ResolutionId.Value),
                    pendingResolution.SourceCardInstanceId,
                    pendingResolution.SourceDefinitionReference,
                    pendingResolution.ActionDefinitionId,
                    pendingResolution.EffectDefinitionReference,
                    pendingResolution.ActorId,
                    pendingResolution.ControllerId,
                    step.RepeatRequested.Continuation);
            }

            borrowRequested = step.BorrowRequested;
            lanePushCause = step.LanePushCause;
            appliedPushes = step.AppliedPushes;
            requestedHeroDefeats = step.RequestedHeroDefeats;
            return Complete(step, default, default, default, default, false);
        }

        /// <summary>
        /// Resolves the effect definition a pending Skill is executing from the trusted card
        /// definitions rather than from anything the frame carries. The frame holds a reference; the
        /// semantics always come from the definition the match was configured with.
        /// </summary>
        internal static EffectDefinition FindEffectRoot(
            IReadOnlyList<CardDefinition> definitions,
            PendingResolution pending)
        {
            foreach (var definition in definitions)
            {
                if (!definition.Reference.Equals(pending.SourceDefinitionReference))
                {
                    continue;
                }

                foreach (var action in definition.ActionDefinitions)
                {
                    if (!action.Id.Equals(pending.ActionDefinitionId))
                    {
                        continue;
                    }

                    // Milestone 8.23. The action is asked which of its bodies the frame names, rather
                    // than being assumed to have one: an Attack printing both of p.14's attacker
                    // moments declares two, and a step-7 frame names the second.
                    var root = action.EffectRootFor(pending.EffectDefinitionReference);
                    if (root != null)
                    {
                        return root;
                    }
                }
            }

            return null;
        }

        private CardActionResolutionResult Complete(
            EffectResolutionStepResult step,
            ActionResolutionId startedResolutionId,
            CardInstanceId startedCard,
            ActionDefinitionId startedAction,
            HeroId startedActor,
            bool isStart,
            ActionInvocationContext invocationContext = ActionInvocationContext.PrintedPrimary)
        {
            // Read the identity before overwriting the field: on the final step the frame is gone, so
            // the completing facts would otherwise have nothing to name.
            //
            // At a start the identity is the offer's, not the frame's. The frame is built from that
            // offer, so the two agree whenever a frame exists - but a Skill whose root resolves to
            // completion during the start itself leaves no frame, and there is no earlier pending
            // resolution either, because a start is refused while one is live. Reading the frame there
            // read null and faulted, which made choice-free Skills unwritable.
            ActionResolutionId resolutionId;
            CardInstanceId card;
            ActionDefinitionId action;
            HeroId actor;
            if (isStart)
            {
                resolutionId = startedResolutionId;
                card = startedCard;
                action = startedAction;
                actor = startedActor;
            }
            else
            {
                var source = step.Frame ?? pendingResolution;
                resolutionId = new ActionResolutionId(source.ResolutionId.Value);
                card = source.SourceCardInstanceId;
                action = source.ActionDefinitionId;
                actor = source.ActorId;

                // Milestone 8.23. A repeated Skill that paused finishes here rather than at its start,
                // so the completion the caller named is not the one that reports the invocation. The
                // frame carries it and is the only thing that still does, which is why it is read
                // rather than passed down a second time.
                invocationContext = source.InvocationContext;
            }

            pendingResolution = step.Frame;

            var steps = new List<ResolutionStep>();
            if (isStart)
            {
                steps.Add(ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionStarted));
                steps.Add(ResolutionStep.FromFact(
                    CardActionResolutionEvent.SkillActionStarted(resolutionId, card, action, actor)));
            }

            foreach (var effectEvent in step.Events)
            {
                foreach (var checkpoint in effectEvent.CheckpointsBefore)
                {
                    steps.Add(ResolutionStep.AtCheckpoint(checkpoint));
                }

                steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.SkillEffectNode(
                    FactKindOf(effectEvent.ResultKind),
                    resolutionId, card, action, actor, effectEvent.NodeId.Value,
                    // Milestone 8.35z14 (the blocker-identity channel). Read off the same transient fact
                    // `EffectResolutionSession` already attached it to, never re-derived here.
                    effectEvent.Blocking?.ControllerHeroId ?? default,
                    effectEvent.Blocking?.SourceCardInstanceId ?? default)));

                foreach (var checkpoint in effectEvent.CheckpointsAfter)
                {
                    steps.Add(ResolutionStep.AtCheckpoint(checkpoint));
                }
            }

            if (step.IsComplete)
            {
                steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.SkillActionCompleted(
                    resolutionId, card, action, actor)));
                steps.Add(ResolutionStep.AtActionEnded(
                    card, action, actor, ActionKind.Skill, invocationContext,
                    // Milestone 8.35z29. Whether this Skill's own effect placed the hero
                    // performing it, which is what Misa's Power Overwhelming watches. The resolution
                    // answered it from its trusted definition; nothing is inferred here.
                    placedSelf: step.PlacedActingHero));
            }
            else if (step.Frame.PendingChoice != null)
            {
                steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.SkillChoiceRequested(
                    resolutionId, card, action, actor, step.Frame.PendingChoice.ChoiceId.Value)));
            }

            // Milestone 8.19c1. A frame that stopped with no question outstanding is a Push waiting for
            // whatever watches it. Nothing is reported here: the moment belongs to the trigger, and the
            // authority that runs it publishes the trigger's own facts. If nothing applies, the same
            // authority carries this frame straight on and the stream reads as though it never stopped.
            return CardActionResolutionResult.Accept(step.IsComplete, null, null, steps);
        }

        /// <summary>
        /// The one place a node outcome becomes a fact kind. It is a translation between two closed
        /// vocabularies, not a rules decision, and it lives here rather than in any dispatcher.
        /// </summary>
        private static CardActionResolutionEventKind FactKindOf(EffectExecutionResultKind resultKind)
        {
            switch (resultKind)
            {
                case EffectExecutionResultKind.DeclinedOptional:
                    return CardActionResolutionEventKind.SkillEffectNodeDeclined;
                case EffectExecutionResultKind.ImpossibleMandatory:
                    return CardActionResolutionEventKind.SkillEffectNodeStopped;
                default:
                    return CardActionResolutionEventKind.SkillEffectNodeApplied;
            }
        }

        private static CardActionResolutionResult Reject(CardActionResolutionRejectionReason reason) =>
            CardActionResolutionResult.Reject(reason, null, null);
    }
}
