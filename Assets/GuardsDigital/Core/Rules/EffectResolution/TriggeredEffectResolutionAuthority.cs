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

        /// <summary>
        /// Milestone 8.67. What this checkpoint still owes, held from the moment one candidate pauses
        /// until the list is exhausted. Null whenever a checkpoint is finished, which is every moment
        /// before this milestone and every checkpoint whose candidates all complete in one pass.
        /// </summary>
        private CheckpointContinuation continuation;

        /// <summary>
        /// The identity of a pause that arrived from a snapshot rather than from a live checkpoint, or
        /// <c>null</c>. It is the one input the regeneration needs: the id carries the source that
        /// paused, the checkpoint it paused at, and that checkpoint's round and turn.
        /// </summary>
        private EffectResolutionId? restoredPauseId;

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

        /// <summary>
        /// Milestone 9.21h. The restored pause with its choice narrowed by the dispatcher that holds
        /// the repeat probe - a "different you" repeat offers only the bodies it can be performed from.
        /// </summary>
        internal void NarrowPendingChoice(PendingEffectChoice narrowed)
        {
            if (PendingResolution?.PendingChoice == null || narrowed == null) return;
            PendingResolution = PendingResolution.With(
                PendingResolution.Continuation, PendingResolution.CompletedOutcomes,
                PendingResolution.AcceptedChoices, narrowed);
        }

        /// <summary>The trusted definition a paused triggered frame runs, or null. Milestone 9.21h.</summary>
        internal EffectDefinition PendingDefinition =>
            PendingResolution != null && effects.TryGet(PendingResolution.EffectDefinitionReference, out var effect)
                ? effect
                : null;

        internal void RestorePending(PendingResolution pending)
        {
            if (PendingResolution != null) throw new InvalidOperationException("A triggered continuation is already present.");
            PendingResolution = pending ?? throw new ArgumentNullException(nameof(pending));

            // Milestone 8.67. A restored pause has lost the in-memory remainder of its checkpoint, so
            // it is marked as owing one and the remainder is REGENERATED from the lifecycle when the
            // answer arrives - milestone 8.10's discipline for a restored card-text page, applied to a
            // checkpoint: what is left to do is produced from the continuation's own identity rather
            // than carried in the payload, which is what keeps snapshot schema v43 where it is.
            restoredPauseId = pending.ResolutionId;
        }

        /// <summary>
        /// Milestone 8.67. Whether this checkpoint has candidates left to run once the frame standing
        /// on it finishes.
        ///
        /// <para>True while a remainder is held, and true for a RESTORED pause until the remainder has
        /// been regenerated once - because a restored authority cannot know whether there is one
        /// without asking the lifecycle, and it is handed the lifecycle only at resume time.</para>
        /// </summary>
        public bool HasCheckpointContinuation =>
            continuation != null || restoredPauseId.HasValue;

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
            RepeatFeasibility repeatCanBePerformed = null,
            IMatchTieBreakerCoin coin = null)
        {
            if (PendingResolution != null)
                throw new InvalidOperationException("A paused triggered effect must be answered before another checkpoint is visited.");

            // Milestone 8.67. The same sentence one step further out: a checkpoint that still owes
            // candidates is not finished either, and visiting the next one would drop them silently.
            // Every path that can finish a paused trigger resumes the remainder inside the command
            // that finished it, so reaching here with one owed is a wiring fault rather than a state a
            // match can be in.
            if (continuation != null || restoredPauseId.HasValue)
                throw new InvalidOperationException(
                    "A triggered checkpoint still owes candidates and must be resumed before another checkpoint is visited.");

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

            RunCandidates(
                candidates, 0, checkpoint, tieBreaker, timing, definitions, instances, participants,
                modifications ?? throw new ArgumentNullException(nameof(modifications)), heroItems,
                repeatCanBePerformed, coin);

            return events;
        }

        /// <summary>
        /// Milestone 8.67. Runs the checkpoint's ordered candidates from <paramref name="startIndex"/>
        /// on, and holds whatever is left when one of them pauses.
        ///
        /// <para><b>The defect this closes, in one sentence: a checkpoint stopped at its first pause
        /// and never started again.</b> The loop below is the loop that was always here; what is new is
        /// that the break records its position instead of discarding it, and that
        /// <see cref="ResumeCheckpoint"/> re-enters at exactly that position with the same ordered
        /// list, the same checkpoint context and the same per-candidate context. Milestone 8.61f
        /// measured the gap on shipped content: Min's Death Grenade and Silverarrow's Warning Shot at
        /// one end-of-turn checkpoint, where only the trigger the pipeline reached first ever ran.</para>
        ///
        /// <para>What a candidate that finishes here publishes is unchanged - a repeat request stands
        /// for the whole checkpoint and a borrow is published the moment it is made - because one
        /// checkpoint visiting several abilities was always this loop's business.</para>
        /// </summary>
        private void RunCandidates(
            IReadOnlyList<TriggerCandidate> candidates,
            int startIndex,
            ActiveEffectCheckpointContext checkpoint,
            TieBreakerState tieBreaker,
            ActiveEffectLifecycle timing,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            RepeatFeasibility repeatCanBePerformed,
            IMatchTieBreakerCoin coin = null)
        {
            var materializedDefinitions = definitions as CardDefinition[] ?? definitions.ToArray();
            var materializedInstances = instances as CardInstance[] ?? instances.ToArray();
            for (var index = startIndex; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (!effects.TryGet(candidate.TriggeredEffect, out var effect))
                    throw new InvalidOperationException(
                        $"Triggered effect '{candidate.TriggeredEffect}' is not configured.");

                Func<EffectResolutionContext> build = () => new EffectResolutionContext(
                    effect,
                    capabilities,
                    participants,
                    timing,
                    checkpoint.RoundIndex,
                    checkpoint.TurnIndex,
                    modifications ?? throw new ArgumentNullException(nameof(modifications)),
                    EffectCardState.Of(materializedDefinitions, materializedInstances),
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
                        : null,
                    // Milestone 8.75a. The coin is read HERE rather than from the checkpoint's own
                    // `tieBreaker`, and the difference is one flip: Chaos Incarnate turns the coin
                    // over in the middle of this frame, and every context built after it must carry
                    // the new face.
                    tieBreakerRevealedSide: coin?.TieBreakerState.RevealedSide ?? TeamSide.None,
                    watchesTieBreakerFlips: coin != null);
                var context = build();
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
                step = SettleTieBreakerFlips(step, coin, build);
                PendingResolution = step.Frame;
                if (step.IsComplete && step.RepeatRequested.IsRequested)
                {
                    // One checkpoint can visit several abilities. A request already made stands:
                    // a later ability finishing quietly is not a decline of an earlier one.
                    LastRepeatRequested = step.RepeatRequested;
                }
                if (step.IsComplete && step.BorrowRequested.IsRequested)
                {
                    PublishBorrow(step.BorrowRequested, candidate.ControllerHeroId,
                        materializedDefinitions, materializedInstances);
                }
                if (PendingResolution != null)
                {
                    // Milestone 8.67. The break that dropped the rest of the checkpoint now writes
                    // down where it stopped. The remainder is the SAME ordered list, so nothing is
                    // re-sorted and no candidate is re-derived when it starts again.
                    continuation = new CheckpointContinuation(
                        checkpoint,
                        tieBreaker,
                        Skip(candidates, index + 1));
                    return;
                }
            }

            continuation = null;
        }

        /// <summary>
        /// Milestone 8.75a. Turns the Tie Breaker coin over for a frame that asked, and carries that
        /// frame on with the new face in hand.
        ///
        /// <para><b>Why the frame stops and is continued rather than reading the coin later.</b>
        /// Ignatia the Mad's Chaos Incarnate prints "you may flip the Tie Breaker coin; if you do,
        /// you may perform that action again, choosing different targets", and the node AFTER the
        /// flip is that repeat. Whether the repeat has a legal subject is a question about her card
        /// text under the face the coin is NOW showing - every one of her actions branches on it - so
        /// the flip has to be applied before the node that asks. The resolver stops on the published
        /// fact (milestone 8.19c1's shape for an applied Push), this turns the coin over, and the
        /// continuation is built from a fresh context carrying the new face.</para>
        ///
        /// <para>It is a loop rather than a single step because nothing in the shape forbids a body
        /// that flips twice; no content does, and a loop costs one comparison for every frame that
        /// never flips at all.</para>
        /// </summary>
        private static EffectResolutionStepResult SettleTieBreakerFlips(
            EffectResolutionStepResult step,
            IMatchTieBreakerCoin coin,
            Func<EffectResolutionContext> buildContext)
        {
            List<EffectResolutionEvent> earlier = null;
            while (step.Accepted && step.TieBreakerFlipRequested && step.Frame != null)
            {
                if (coin == null)
                {
                    throw new InvalidOperationException(
                        "A frame asked for the Tie Breaker coin to be flipped with no coin bound; " +
                        "the resolver only reports the request where one is.");
                }

                coin.FlipTieBreaker();
                (earlier = earlier ?? new List<EffectResolutionEvent>()).AddRange(step.Events);
                var continued = EffectResolutionSession.Continue(step.Frame, buildContext());
                if (!continued.Accepted)
                {
                    throw new InvalidOperationException(
                        "A triggered frame standing on a Tie Breaker flip was refused by its own " +
                        "continuation.");
                }

                step = continued;
            }

            return step.PrependingEvents(earlier);
        }

        private static IReadOnlyList<TriggerCandidate> Skip(
            IReadOnlyList<TriggerCandidate> candidates, int from)
        {
            var remaining = new TriggerCandidate[Math.Max(0, candidates.Count - from)];
            for (var index = 0; index < remaining.Length; index++)
            {
                remaining[index] = candidates[from + index];
            }

            return remaining;
        }

        /// <summary>
        /// Milestone 8.67. Carries the checkpoint on from the candidate after the one that paused, and
        /// reports whether anything of it is still standing when this returns.
        ///
        /// <para><b>It is called by every path that can finish a paused triggered frame</b> - the
        /// effect-choice answer and milestone 8.21's removal-prevention answer - and is a no-op when a
        /// frame is still standing or when nothing is owed, which is what makes it safe to call at any
        /// such boundary. The lifecycle is NOT visited again: the checkpoint's own events were raised
        /// by the first call and belong to it, and re-raising them would trigger every effect a second
        /// time.</para>
        ///
        /// <para><b>A restored pause regenerates its remainder from the lifecycle</b> rather than from
        /// the payload, because carrying it would have moved snapshot schema v43. The regeneration is
        /// exact for every Active-effect trigger, and states its two limits in
        /// <see cref="RegenerateRemainder"/>.</para>
        /// </summary>
        public bool ResumeCheckpoint(
            ActiveEffectLifecycle timing,
            TieBreakerState tieBreaker,
            IEnumerable<CardDefinition> cardDefinitions,
            IEnumerable<CardInstance> cardInstances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null,
            RepeatFeasibility repeatCanBePerformed = null,
            IMatchTieBreakerCoin coin = null)
        {
            if (PendingResolution != null) return true;

            var definitions = (cardDefinitions ?? throw new ArgumentNullException(nameof(cardDefinitions))).ToArray();
            var instances = (cardInstances ?? throw new ArgumentNullException(nameof(cardInstances))).ToArray();

            var owed = continuation;
            if (owed == null && restoredPauseId.HasValue)
            {
                owed = RegenerateRemainder(
                    restoredPauseId.Value, timing, tieBreaker, definitions, instances);
            }

            restoredPauseId = null;
            continuation = null;
            if (owed == null || owed.Remaining.Count == 0) return false;

            RunCandidates(
                owed.Remaining, 0, owed.Checkpoint, owed.TieBreaker, timing, definitions, instances,
                participants, modifications ?? throw new ArgumentNullException(nameof(modifications)),
                heroItems, repeatCanBePerformed, coin);
            return PendingResolution != null;
        }

        /// <summary>
        /// Milestone 8.67. The candidates a RESTORED pause still owes, derived from the lifecycle and
        /// the paused frame's own identity.
        ///
        /// <para>The identity is the whole input: <c>trigger-{source}-{checkpoint}-{round}-{turn}</c> is
        /// minted by <see cref="RunCandidates"/> and persisted with the frame, so a restored authority
        /// knows which checkpoint it is standing on and which candidate it stopped at without a byte
        /// being added to the payload. The remainder is every Active effect that declares that
        /// checkpoint as a trigger and either still stands or ended AT that checkpoint - the
        /// trigger-and-expire shape every "End of turn:" face has - ordered by the same three keys and
        /// cut after the paused source.</para>
        ///
        /// <para><b>Two limits, stated rather than left to be found.</b> A CONTINUOUS candidate - an
        /// Ultimate's permanent trigger, derived per checkpoint from the action that had just been
        /// performed - is not in the lifecycle and cannot be regenerated, so a restore taken while one
        /// of those is paused resumes only the Active-effect candidates behind it. And the checkpoint's
        /// affected hero is not part of the identity, so the hero narrowing
        /// <c>ActiveEffectLifecycle.IsRelevantHero</c> applies is not re-applied here. Both are limits
        /// of a RESTORED remainder only: a live one is the exact list the checkpoint built.</para>
        /// </summary>
        private CheckpointContinuation RegenerateRemainder(
            EffectResolutionId pausedId,
            ActiveEffectLifecycle timing,
            TieBreakerState tieBreaker,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances)
        {
            if (timing == null || !tieBreaker.IsDefined) return null;
            if (!TryReadCheckpointIdentity(
                    pausedId, out var pausedSourceId, out var checkpoint, out var roundIndex,
                    out var turnIndex))
            {
                return null;
            }

            var context = new ActiveEffectCheckpointContext(checkpoint, roundIndex, turnIndex);
            var ordered = timing.Effects
                .Where(effect => effect.Definition.TriggeredEffect.HasValue &&
                    effect.Definition.TriggerCheckpoints.Contains(checkpoint) &&
                    (effect.Phase == ActiveEffectPhase.Ended
                        ? effect.EndedAt == checkpoint
                        : effect.Phase == ActiveEffectPhase.Active))
                .OrderBy(effect => effect.ControllerTeamSide == tieBreaker.RevealedSide ? 0 : 1)
                .ThenBy(effect => effect.CreationSequence)
                .ThenBy(effect => effect.Id.Value, StringComparer.Ordinal)
                .ToArray();

            var paused = Array.FindIndex(
                ordered, effect => string.Equals(effect.Id.Value, pausedSourceId, StringComparison.Ordinal));
            if (paused < 0) return null;

            var remaining = new List<TriggerCandidate>();
            for (var index = paused + 1; index < ordered.Length; index++)
            {
                var source = ordered[index];
                var sourceCard = instances.FirstOrDefault(card => card.Id.Equals(source.SourceCardInstanceId));
                if (sourceCard == null) continue;
                var sourceDefinition = definitions.FirstOrDefault(definition =>
                    definition.Reference.Equals(sourceCard.DefinitionReference));
                var sourceAction = sourceDefinition?.ActionDefinitions.FirstOrDefault(action =>
                    action.Id.Equals(source.SourceActionDefinitionId));
                if (sourceAction == null) continue;
                remaining.Add(TriggerCandidate.FromActive(
                    source, sourceDefinition.Reference, sourceAction.Kind));
            }

            return remaining.Count == 0
                ? null
                : new CheckpointContinuation(context, tieBreaker, remaining);
        }

        /// <summary>
        /// Reads back the four facts <see cref="RunCandidates"/> mints a triggered frame's identity
        /// from. It is read from the END, because a source id may itself contain the separator while
        /// the three trailing fields never can.
        /// </summary>
        private static bool TryReadCheckpointIdentity(
            EffectResolutionId id,
            out string sourceId,
            out TimingCheckpoint checkpoint,
            out int roundIndex,
            out int turnIndex)
        {
            sourceId = null;
            checkpoint = TimingCheckpoint.None;
            roundIndex = 0;
            turnIndex = 0;
            var value = id.Value;
            if (string.IsNullOrEmpty(value) || !value.StartsWith("trigger-", StringComparison.Ordinal))
                return false;

            var parts = value.Split('-');
            if (parts.Length < 5) return false;
            if (!int.TryParse(parts[parts.Length - 1], out turnIndex)) return false;
            if (!int.TryParse(parts[parts.Length - 2], out roundIndex)) return false;
            if (!Enum.TryParse(parts[parts.Length - 3], false, out checkpoint) ||
                checkpoint == TimingCheckpoint.None)
            {
                return false;
            }

            var prefix = "trigger-".Length;
            var suffix = parts[parts.Length - 1].Length + parts[parts.Length - 2].Length +
                parts[parts.Length - 3].Length + 3;
            if (value.Length - suffix <= prefix) return false;
            sourceId = value.Substring(prefix, value.Length - suffix - prefix);
            return roundIndex >= 1 && turnIndex >= 1;
        }

        /// <summary>
        /// Milestone 8.67. One checkpoint's unfinished business: where it was standing and what it has
        /// left to run, in the order it built.
        /// </summary>
        private sealed class CheckpointContinuation
        {
            internal CheckpointContinuation(
                ActiveEffectCheckpointContext checkpoint,
                TieBreakerState tieBreaker,
                IReadOnlyList<TriggerCandidate> remaining)
            {
                Checkpoint = checkpoint;
                TieBreaker = tieBreaker;
                Remaining = remaining;
            }

            internal ActiveEffectCheckpointContext Checkpoint { get; }
            internal TieBreakerState TieBreaker { get; }
            internal IReadOnlyList<TriggerCandidate> Remaining { get; }
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
                PerformedAttackAction triggeringAttackAction,
                PerformedPrimaryAction triggeringPrimaryAction)
            {
                TriggeringPrimaryAction = triggeringPrimaryAction;
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
            /// The completed PRIMARY action this occurrence fired for, when it was derived from one.
            /// Milestone 8.75a; <see cref="PerformedPrimaryAction.None"/> for every Active-effect
            /// trigger and for every continuous source whose condition reads a different fact.
            /// </summary>
            public PerformedPrimaryAction TriggeringPrimaryAction { get; }

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
            ///
            /// <para>Milestone 8.75a adds the third fact, read on the same terms: Ignatia the Mad's
            /// Chaos Incarnate fires off <see cref="PerformedPrimaryAction"/>, whose own
            /// <c>CanBeRepeated</c> answers the same sentences for the coloured Skills and Movements
            /// that neither of the two facts above records.</para>
            /// </summary>
            public bool RepeatIsUnavailable =>
                !TriggeringBasicAction.CanBeRepeated && !TriggeringAttackAction.CanBeRepeated &&
                !TriggeringPrimaryAction.CanBeRepeated;

            /// <summary>
            /// Whether this occurrence came from a completed action at all, which is what decides
            /// whether the authority's live feasibility probe travels with it. Milestone 8.61h reads
            /// both facts for the reason <see cref="RepeatIsUnavailable"/> does.
            /// </summary>
            public bool HasCompletedActionSubject =>
                TriggeringBasicAction.IsDefined || TriggeringAttackAction.IsDefined ||
                TriggeringPrimaryAction.IsDefined;

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
                    PerformedAttackAction.None,
                    PerformedPrimaryAction.None);

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
                    source.TriggeringAttackAction,
                    source.TriggeringPrimaryAction);
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
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null,
            RepeatFeasibility repeatCanBePerformed = null,
            IMatchTieBreakerCoin coin = null)
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
            Func<EffectResolutionContext> build = () => new EffectResolutionContext(
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
                rosterState: rosterState,
                // Milestone 8.75a. The answer that finishes a `may` can itself walk into a
                // RepeatAction node - Chaos Incarnate's flip is answered and the repeat is the node
                // after it - so the live feasibility probe milestone 8.25e built for the checkpoint's
                // first pass travels this path too. Before this milestone no shipped body could reach
                // a repeat from an answer (Tigerclaw's and Min's are root-level lone repeats), so
                // nothing in the build changes shape for it.
                repeatCanBePerformed: repeatCanBePerformed,
                tieBreakerRevealedSide: coin?.TieBreakerState.RevealedSide ?? TeamSide.None,
                watchesTieBreakerFlips: coin != null);
            var step = SettleTieBreakerFlips(
                EffectResolutionSession.Answer(PendingResolution, command, build()), coin, build);
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
