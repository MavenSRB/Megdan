using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Executes the effect reference carried by an Active-effect definition when the lifecycle emits
    /// its ordered Triggered visit. It deliberately delegates node execution to
    /// <see cref="EffectResolutionSession"/>: a trigger is another entry into the ordinary typed frame,
    /// not a second effect language.
    /// </summary>
    public sealed class TriggeredEffectResolutionAuthority
    {
        private readonly TriggeredEffectCatalog effects;
        private readonly EffectCapabilityCatalog capabilities;
        private readonly MatchComponents components;
        private EffectRosterState rosterState = EffectRosterState.Absent;

        public TriggeredEffectResolutionAuthority(
            TriggeredEffectCatalog effects,
            EffectCapabilityCatalog capabilities,
            MatchComponents components = null,
            PendingResolution pendingResolution = null)
        {
            this.effects = effects ?? throw new ArgumentNullException(nameof(effects));
            this.capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
            this.components = components;
            PendingResolution = pendingResolution;
            var rejections = effects.Definitions
                .SelectMany(effect => EffectDefinitionValidator.Validate(effect, capabilities))
                .ToArray();
            if (rejections.Length > 0)
                throw new ArgumentException(
                    "A triggered-effect catalog contains an invalid definition: " + rejections[0],
                    nameof(effects));
        }

        public PendingResolution PendingResolution { get; private set; }

        public PendingResolution LastCompletedResolution { get; private set; }

        /// <summary>
        /// What the last triggered frame to finish asked for, or <c>null</c> when none has finished
        /// since the question was asked. Milestone 8.23: a permanent ability may ask to repeat the
        /// action it fired after, and the request is an ordinary node outcome that the authority
        /// holding the subject reads once the frame is done.
        /// </summary>
        public RepeatRequest? LastRepeatRequested { get; private set; }

        /// <summary>
        /// Milestone 8.38d. What the last triggered frame to finish asked to BORROW, or <c>null</c>
        /// when none has. The exact twin of <see cref="LastRepeatRequested"/>, published at the same
        /// two moments and read by the same holder, because a permanent ability that grants an action
        /// off a card is asking the dispatcher for the same kind of thing a repeat asks for.
        ///
        /// <para>Until this milestone a triggered frame could carry a borrow node and the request went
        /// nowhere: only the Skill and card-text altitudes read one back, so Bullet Time would have
        /// chosen a card and granted nothing.</para>
        /// </summary>
        public BorrowRequest? LastBorrowRequested { get; private set; }

        /// <summary>
        /// The rule id of the granting ability behind <see cref="LastBorrowRequested"/> when that
        /// ability narrows what its grant may target, and <c>null</c> otherwise - milestone 8.38d,
        /// Bullet Time's "you cannot target the same enemy hero twice in the same turn this way".
        ///
        /// <para><b>It is derived from the unlocked set rather than held from the trigger that
        /// started the frame</b>, and that is what makes it survive a snapshot taken while the frame
        /// is paused: p.16 allows one Ultimate per hero, and a borrow published from inside a
        /// TRIGGERED frame came from that hero's Ultimate by construction. A borrow published from
        /// card text never reaches here at all, which is exactly the distinction "this way"
        /// draws.</para>
        /// </summary>
        public string LastBorrowGrantSourceId { get; private set; }

        /// <summary>
        /// The hero who will perform <see cref="LastBorrowRequested"/>. It travels with the request
        /// rather than being read off a frame afterwards, because the frame that asked is gone by the
        /// time the holder settles it - and on the path where the trigger completes without pausing it
        /// was never standing at all.
        /// </summary>
        public HeroId LastBorrowActorId { get; private set; }

        /// <summary>
        /// Takes the published borrow, so the holder that turns it into an owed action cannot be
        /// handed the same one twice. Milestone 8.38d.
        ///
        /// <para>It is consumed where <see cref="LastRepeatRequested"/> is not, and the difference is
        /// what each becomes: a settled repeat SUBJECT is the same subject however often it is settled,
        /// while a settled borrow becomes an owed instance that the turn then starts - so a second
        /// settle between two checkpoints would grant the action twice.</para>
        /// </summary>
        internal BorrowRequest? TakeLastBorrowRequested()
        {
            var requested = LastBorrowRequested;
            LastBorrowRequested = null;
            return requested;
        }

        internal void BindRosterState(EffectRosterState state)
        {
            rosterState = state ?? throw new ArgumentNullException(nameof(state));
        }

        internal void RestorePending(PendingResolution pending)
        {
            if (PendingResolution != null) throw new InvalidOperationException("A triggered continuation is already present.");
            PendingResolution = pending ?? throw new ArgumentNullException(nameof(pending));
        }

        public IReadOnlyList<ActiveEffectLifecycleEvent> ApplyCheckpoint(
            ActiveEffectLifecycle timing,
            ActiveEffectCheckpointContext checkpoint,
            TieBreakerState tieBreaker,
            IEnumerable<CardDefinition> cardDefinitions,
            IEnumerable<CardInstance> cardInstances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null,
            IEnumerable<ContinuousTriggeredEffectSource> continuousSources = null,
            Func<GuardsDigital.Core.Effects.RepeatTargetBinding, bool> repeatCanBePerformed = null)
        {
            if (PendingResolution != null)
                throw new InvalidOperationException("A paused triggered effect must be answered before another checkpoint is visited.");

            LastRepeatRequested = null;
            LastBorrowRequested = null;
            LastBorrowGrantSourceId = null;
            LastBorrowActorId = default;

            var definitions = (cardDefinitions ?? throw new ArgumentNullException(nameof(cardDefinitions))).ToArray();
            var instances = (cardInstances ?? throw new ArgumentNullException(nameof(cardInstances))).ToArray();
            var events = timing.ApplyCheckpoint(checkpoint, tieBreaker);
            var active = events.Where(value => value.Kind == ActiveEffectLifecycleEventKind.Triggered)
                .Select(triggered =>
                {
                    var source = timing.Effects.Single(effect => effect.Id.Equals(triggered.EffectId));
                    if (!source.Definition.TriggeredEffect.HasValue)
                        return null;

                    var sourceCard = instances.Single(card => card.Id.Equals(source.SourceCardInstanceId));
                    var sourceDefinition = definitions.Single(definition =>
                        definition.Reference.Equals(sourceCard.DefinitionReference));
                    var sourceAction = sourceDefinition.ActionDefinitions.Single(action =>
                        action.Id.Equals(source.SourceActionDefinitionId));
                    return TriggerCandidate.FromActive(source, sourceDefinition.Reference, sourceAction.Kind);
                })
                .Where(value => value != null);
            var continuous = (continuousSources ?? Enumerable.Empty<ContinuousTriggeredEffectSource>())
                .Where(source => source.TriggerCheckpoints.Contains(checkpoint.Checkpoint))
                .Select(TriggerCandidate.FromContinuous);
            var candidates = active.Concat(continuous)
                .OrderBy(source => source.ControllerTeamSide == tieBreaker.RevealedSide ? 0 : 1)
                .ThenBy(source => source.OrderingSequence)
                .ThenBy(source => source.SourceId, StringComparer.Ordinal)
                .ToArray();

            foreach (var candidate in candidates)
            {
                if (!effects.TryGet(candidate.TriggeredEffect, out var effect))
                    throw new InvalidOperationException(
                        $"Triggered effect '{candidate.TriggeredEffect}' is not configured.");

                var context = new EffectResolutionContext(
                    effect,
                    capabilities,
                    participants,
                    timing,
                    checkpoint.RoundIndex,
                    checkpoint.TurnIndex,
                    modifications ?? throw new ArgumentNullException(nameof(modifications)),
                    EffectCardState.Of(definitions, instances),
                    candidate.EnclosingActionKind,
                    components: components,
                    heroItems: heroItems,
                    rosterState: rosterState,
                    repeatIsUnavailable: candidate.RepeatIsUnavailable,
                    // Milestone 8.25e. Only an occurrence derived from a completed action can ask
                    // whether that action could be performed again, so the probe travels with the
                    // candidate that has one rather than with the checkpoint.
                    repeatCanBePerformed: candidate.HasCompletedActionSubject
                        ? repeatCanBePerformed
                        : null);
                var step = EffectResolutionSession.Start(
                    new EffectResolutionId(
                        $"trigger-{candidate.SourceId}-{checkpoint.Checkpoint}-{checkpoint.RoundIndex}-{checkpoint.TurnIndex}"),
                    candidate.SourceCardInstanceId,
                    candidate.SourceDefinitionReference,
                    candidate.SourceActionDefinitionId,
                    candidate.ControllerHeroId,
                    candidate.ControllerHeroId,
                    ActionInvocationContext.PrintedPrimary,
                    context);
                if (!step.Accepted)
                    throw new InvalidOperationException("A configured triggered effect was refused by its effect frame.");
                PendingResolution = step.Frame;
                if (step.IsComplete && step.RepeatRequested.IsRequested)
                {
                    // One checkpoint can visit several abilities. A request already made stands:
                    // a later ability finishing quietly is not a decline of an earlier one.
                    LastRepeatRequested = step.RepeatRequested;
                }
                if (step.IsComplete && step.BorrowRequested.IsRequested)
                {
                    PublishBorrow(step.BorrowRequested, candidate.ControllerHeroId, definitions,
                        instances);
                }
                if (PendingResolution != null)
                    break;
            }

            return events;
        }

        private sealed class TriggerCandidate
        {
            private TriggerCandidate(
                string sourceId,
                HeroId controllerHeroId,
                TeamSide controllerTeamSide,
                CardInstanceId sourceCardInstanceId,
                CardDefinitionReference sourceDefinitionReference,
                ActionDefinitionId sourceActionDefinitionId,
                ActionKind enclosingActionKind,
                EffectDefinitionReference triggeredEffect,
                long orderingSequence,
                PerformedBasicAction triggeringBasicAction,
                PerformedAttackAction triggeringAttackAction)
            {
                SourceId = sourceId;
                ControllerHeroId = controllerHeroId;
                ControllerTeamSide = controllerTeamSide;
                SourceCardInstanceId = sourceCardInstanceId;
                SourceDefinitionReference = sourceDefinitionReference;
                SourceActionDefinitionId = sourceActionDefinitionId;
                EnclosingActionKind = enclosingActionKind;
                TriggeredEffect = triggeredEffect;
                OrderingSequence = orderingSequence;
                TriggeringBasicAction = triggeringBasicAction;
                TriggeringAttackAction = triggeringAttackAction;
            }

            public string SourceId { get; }
            public HeroId ControllerHeroId { get; }
            public TeamSide ControllerTeamSide { get; }
            public CardInstanceId SourceCardInstanceId { get; }
            public CardDefinitionReference SourceDefinitionReference { get; }
            public ActionDefinitionId SourceActionDefinitionId { get; }
            public ActionKind EnclosingActionKind { get; }
            public EffectDefinitionReference TriggeredEffect { get; }
            public long OrderingSequence { get; }

            /// <summary>
            /// The completed basic action this occurrence fired for, when it was derived from one.
            /// Milestone 8.23; <see cref="PerformedBasicAction.None"/> for every Active-effect
            /// trigger, which fires inside a lifecycle rather than after an action.
            /// </summary>
            public PerformedBasicAction TriggeringBasicAction { get; }

            /// <summary>
            /// The completed ATTACK action this occurrence fired for, when it was derived from one.
            /// Milestone 8.61h; <see cref="PerformedAttackAction.None"/> for every Active-effect
            /// trigger and for every continuous source whose condition reads a different fact.
            /// </summary>
            public PerformedAttackAction TriggeringAttackAction { get; }

            /// <summary>
            /// Whether a repeat node in this occurrence has something to repeat. Rulebook p.10 forbids
            /// repeating a repeated action, an occurrence derived from no completed action has no
            /// subject at all, and milestone 8.25e added the third case the page states: two of the
            /// kinds p.9 counts as basic have no second instance the page will make. All three live on
            /// <see cref="PerformedBasicAction.CanBeRepeated"/>, which is the one place the question is
            /// answered.
            ///
            /// <para>What this cannot say is whether the second instance could be <em>performed</em>
            /// from the board as it now stands, because that is not a fact about the action that
            /// ended. The authority holding the subject answers that one, through
            /// <see cref="EffectResolutionContext.RepeatIsUnavailableFor"/>.</para>
            ///
            /// <para>Milestone 8.61h adds the second fact, read on the same terms: Min's Flurry of
            /// Blows fires off <see cref="PerformedAttackAction"/>, whose own
            /// <c>CanBeRepeated</c> answers the same two of p.10's three sentences. Either fact
            /// offering a repeat is enough, because an occurrence carries at most one of them and a
            /// basic Attack that carries both names one action through either.</para>
            /// </summary>
            public bool RepeatIsUnavailable =>
                !TriggeringBasicAction.CanBeRepeated && !TriggeringAttackAction.CanBeRepeated;

            /// <summary>
            /// Whether this occurrence came from a completed action at all, which is what decides
            /// whether the authority's live feasibility probe travels with it. Milestone 8.61h reads
            /// both facts for the reason <see cref="RepeatIsUnavailable"/> does.
            /// </summary>
            public bool HasCompletedActionSubject =>
                TriggeringBasicAction.IsDefined || TriggeringAttackAction.IsDefined;

            public static TriggerCandidate FromActive(
                ActiveEffectInstance source,
                CardDefinitionReference sourceDefinitionReference,
                ActionKind actionKind) =>
                new TriggerCandidate(
                    source.Id.Value,
                    source.ControllerHeroId,
                    source.ControllerTeamSide,
                    source.SourceCardInstanceId,
                    sourceDefinitionReference,
                    source.SourceActionDefinitionId,
                    actionKind,
                    source.Definition.TriggeredEffect.Value,
                    source.CreationSequence,
                    PerformedBasicAction.None,
                    PerformedAttackAction.None);

            public static TriggerCandidate FromContinuous(ContinuousTriggeredEffectSource source) =>
                new TriggerCandidate(
                    source.SourceId,
                    source.ControllerHeroId,
                    source.ControllerTeamSide,
                    source.SourceCardInstanceId,
                    source.SourceDefinitionReference,
                    source.SourceActionDefinitionId,
                    source.EnclosingActionKind,
                    source.TriggeredEffect,
                    source.OrderingSequence,
                    source.TriggeringBasicAction,
                    source.TriggeringAttackAction);
        }

        public EffectResolutionStepResult Answer(
            EffectChoiceCommand command,
            ActiveEffectLifecycle timing,
            IEnumerable<CardDefinition> cardDefinitions,
            IEnumerable<CardInstance> cardInstances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (PendingResolution == null)
                throw new InvalidOperationException("No triggered effect is waiting for an answer.");
            var resolving = PendingResolution;
            if (!effects.TryGet(PendingResolution.EffectDefinitionReference, out var effect))
                throw new InvalidOperationException($"Triggered effect '{PendingResolution.EffectDefinitionReference}' is not configured.");

            var definitions = (cardDefinitions ?? throw new ArgumentNullException(nameof(cardDefinitions))).ToArray();
            var instances = (cardInstances ?? throw new ArgumentNullException(nameof(cardInstances))).ToArray();
            var sourceDefinition = definitions.Single(definition =>
                definition.Reference.Equals(PendingResolution.SourceDefinitionReference));
            var sourceAction = sourceDefinition.ActionDefinitions.SingleOrDefault(action =>
                action.Id.Equals(PendingResolution.ActionDefinitionId));
            var enclosingActionKind = sourceAction?.Kind ??
                (sourceDefinition.UltimateRule != null &&
                 sourceDefinition.UltimateRule.EffectSourceId.Equals(PendingResolution.ActionDefinitionId)
                    ? ActionKind.Skill
                    : throw new InvalidOperationException(
                        "A triggered continuation references neither an action nor its Ultimate ability."));
            var context = new EffectResolutionContext(
                effect,
                capabilities,
                participants,
                timing,
                roundIndex,
                turnIndex,
                modifications ?? throw new ArgumentNullException(nameof(modifications)),
                EffectCardState.Of(definitions, instances),
                enclosingActionKind,
                components: components,
                heroItems: heroItems,
                rosterState: rosterState);
            var step = EffectResolutionSession.Answer(PendingResolution, command, context);
            if (step.Accepted)
            {
                PendingResolution = step.Frame;
                LastCompletedResolution = step.IsComplete ? resolving : null;

                // Milestone 8.23. The answer that finished the frame is where a `may` repeat is
                // actually decided, so it is where the request is published. A decline finishes the
                // frame with no request at all, which is the same null a trigger that never mentioned
                // a repeat leaves behind - and the two are the same outcome for the holder.
                if (step.IsComplete && step.RepeatRequested.IsRequested)
                {
                    LastRepeatRequested = step.RepeatRequested;
                }

                // Milestone 8.38d. The same moment, for the same reason: Bullet Time's borrow node
                // pauses to ask which card, so the answer that finishes the frame is where the grant
                // is actually decided. A decline finishes it with no request, which is the same null
                // a trigger that never mentioned a borrow leaves behind.
                if (step.IsComplete && step.BorrowRequested.IsRequested)
                {
                    PublishBorrow(
                        step.BorrowRequested, resolving.ActorId, definitions, instances);
                }
            }
            return step;
        }

        /// <summary>
        /// Records a borrow a triggered frame asked for, together with the granting ability's id when
        /// that ability narrows what its grant may target. Milestone 8.38d.
        /// </summary>
        private void PublishBorrow(
            BorrowRequest requested,
            HeroId controllerId,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances)
        {
            LastBorrowRequested = requested;
            LastBorrowActorId = controllerId;
            LastBorrowGrantSourceId =
                GuardsDigital.Core.Rules.Progression.GrantedTargetExclusionQuery.GrantSourceIdFor(
                    GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery.Derive(
                        definitions, instances),
                    controllerId);
        }
    }
}
