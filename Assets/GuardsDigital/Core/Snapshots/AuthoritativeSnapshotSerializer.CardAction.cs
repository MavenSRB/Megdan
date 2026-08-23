using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Snapshots
{
    public static partial class AuthoritativeSnapshotSerializer
    {
        private static void WriteCardAction(BinaryWriter writer, CardActionResolutionRestoreState state)
        {
            if (state == null)
            {
                writer.Write(NoCardActionAuthorityKind);
                WriteCoinLedger(writer, HeroCoinLedgerRestoreStateHolder.Absent());
                return;
            }

            var (continuationKind, pendingMovement, pendingAttack, pendingSkill, coinLedger) = state;
            writer.Write((int)continuationKind);
            WriteCoinLedger(writer, coinLedger);
            WriteImmediateLanePush(writer, state.PendingImmediateLanePush);
            WritePreAction(writer, state.PendingPreAction);
            if (continuationKind == CardActionContinuationKind.Movement)
            {
                WriteMovementResolution(writer, pendingMovement);
            }

            if (continuationKind == CardActionContinuationKind.Attack)
            {
                WriteAttackResolution(writer, pendingAttack);
            }

            if (continuationKind == CardActionContinuationKind.Skill)
            {
                WriteSkillResolution(writer, pendingSkill);
            }

            if (continuationKind == CardActionContinuationKind.Correction)
            {
                WriteCorrectionResolution(writer, state.PendingCorrection);
                WriteRepeatedAction(writer, state.PendingRepeat);
            }

            // Milestone 8.10, schema v23. An Attack stopped inside rulebook p.14's card text writes
            // three things and no fourth: the frame that asked - the same section a paused Skill writes
            // - the attack the rest of the page belongs to, and which of the page's moments it stopped
            // at. The steps still owed are deliberately absent, because they are facts about an attack
            // rather than identity; the backend regenerates them from exactly this on the way back, and
            // regenerates them the same way when nothing paused at all.
            if (continuationKind == CardActionContinuationKind.AttackCardText)
            {
                var cardText = state.PendingCardText;
                WriteSkillResolution(writer, cardText.Frame);
                WriteAttackResolution(writer, cardText.Attack);
                writer.Write((int)cardText.Moment);
                writer.Write((int)cardText.Outcome);
            }
        }

        /// <summary>
        /// Milestone 6.9. Rulebook p.8's mid-turn Lane Push, requested and not yet resolved.
        ///
        /// It is written outside the continuation discriminator because it accompanies whichever
        /// continuation the action left behind, including none: a Skill can record the cause at one node
        /// and still owe a choice, and an Attack that caused one leaves no continuation at all.
        ///
        /// Everything it carries is identity - the cause, the zone, and the two teams - and nothing is
        /// re-derived on the way back, because nothing about the restored board could say whether a side
        /// was emptied by a Defeat, by a Remove, or by a push p.8 excludes. What restore does check is
        /// that the identities describe this match: an unknown team or an enum outside its declared
        /// range is refused atomically rather than reconstructed.
        /// </summary>
        private static void WriteImmediateLanePush(
            BinaryWriter writer,
            PendingImmediateLanePushRequest request)
        {
            writer.Write(request != null);
            if (request == null)
            {
                return;
            }

            writer.Write(request.RequestId.Value);
            writer.Write((int)request.Cause);
            writer.Write(request.BattleZoneId.Value);
            writer.Write(request.WinningTeamId.Value);
            writer.Write((int)request.WinningSide);
            writer.Write(request.LosingTeamId.Value);
            writer.Write((int)request.LosingSide);
            writer.Write(request.CauseResolutionId.Value);
        }

        /// <summary>
        /// Milestone 8.19a, schema v25. The outer pre-action continuation: an effect frame standing
        /// outside an action that has not started, or the side of that action the actor placed its work
        /// on while the work is still owed.
        ///
        /// <para>It is written outside the continuation discriminator, beside milestone 6.9's Lane-Push
        /// request and for a sharper version of the same reason: it does not merely accompany another
        /// continuation, it coexists with one. Work the actor placed after a Movement is parked while
        /// that Movement is mid-route, so a payload holding both is the ordinary case.</para>
        ///
        /// <para>What crosses the wire is identity and the held offer, never semantics. The frame names
        /// an effect definition and a restore rebinds it to the trusted catalog; the offer is the exact
        /// one the turn validated, which is what the acceptance check means by resuming the offer
        /// rather than re-deriving one against a board the trigger has changed.</para>
        /// </summary>
        private static void WritePreAction(
            BinaryWriter writer,
            GuardsDigital.Core.Rules.CardResolution.PendingPreActionResolution pending)
        {
            writer.Write(pending != null);
            if (pending == null)
            {
                return;
            }

            writer.Write((int)pending.Kind);
            writer.Write((int)pending.Placement);
            writer.Write(pending.HeldResolutionId.Value);
            WriteActionOffer(writer, pending.HeldOffer);
            writer.Write(pending.HeldActionHasStarted);
            writer.Write(pending.Frame != null);
            if (pending.Frame != null)
            {
                WriteSkillResolution(writer, pending.Frame);
            }
        }

        private static GuardsDigital.Core.Rules.CardResolution.PendingPreActionResolution ReadPreAction(
            BinaryReader reader,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> cards,
            MatchParticipants participants,
            MatchContentCatalogs catalogs,
            MatchComponents components,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems)
        {
            if (!reader.ReadBoolean())
            {
                return null;
            }

            var kind = SnapshotEnumReader.Read<GuardsDigital.Core.Rules.CardResolution.PreActionContinuationKind>(
                reader, "pre-action continuation kind");
            var placement = SnapshotEnumReader.Read<GuardsDigital.Core.Rules.CardResolution.PreActionPlacement>(
                reader, "pre-action continuation placement");
            var heldResolutionId = new ActionResolutionId(reader.ReadString());
            var heldOffer = ReadActionOffer(reader);
            var heldActionHasStarted = reader.ReadBoolean();
            var frame = reader.ReadBoolean()
                ? ReadSkillResolution(
                    reader, definitions, cards, catalogs.Capabilities, participants, components,
                    modifications, heroItems, catalogs.TriggeredEffects)
                : null;

            // Every disagreement the shape admits is refused here rather than thrown by the type's own
            // guard, so a tampered payload is a rejected snapshot instead of an exception.
            if (kind == GuardsDigital.Core.Rules.CardResolution.PreActionContinuationKind.None ||
                (frame == null &&
                    placement == GuardsDigital.Core.Rules.CardResolution.PreActionPlacement.Undecided) ||
                (kind == GuardsDigital.Core.Rules.CardResolution.PreActionContinuationKind.BeforeActionTrigger &&
                    placement != GuardsDigital.Core.Rules.CardResolution.PreActionPlacement.BeforeTheAction) ||
                (heldActionHasStarted &&
                    placement != GuardsDigital.Core.Rules.CardResolution.PreActionPlacement.AfterTheAction) ||
                !heldOffer.IsAvailable)
            {
                throw Malformed(
                    "Snapshot pre-action continuation describes a shape this engine cannot be in.");
            }

            return new GuardsDigital.Core.Rules.CardResolution.PendingPreActionResolution(
                frame, kind, placement, heldResolutionId, heldOffer, heldActionHasStarted);
        }

        private static PendingImmediateLanePushRequest ReadImmediateLanePush(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
            {
                return null;
            }

            return new PendingImmediateLanePushRequest(
                new LanePushRequestId(reader.ReadString()),
                SnapshotEnumReader.Read<LanePushCauseKind>(reader, "mid-turn Lane-Push cause"),
                new ZoneId(reader.ReadString()),
                new TeamId(reader.ReadString()),
                SnapshotEnumReader.Read<TeamSide>(reader, "mid-turn Lane Push winning side"),
                new TeamId(reader.ReadString()),
                SnapshotEnumReader.Read<TeamSide>(reader, "mid-turn Lane Push losing side"),
                new ActionResolutionId(reader.ReadString()));
        }

        /// <summary>
        /// Milestone 6.7. A repeat that has been asked for and cannot start until the correction
        /// holding the boundary open finishes. It is written here rather than as a continuation kind of
        /// its own because that is the only state it survives a command in, and it carries identity
        /// alone - the effect it repeats is rebound from the trusted card definitions on the way back,
        /// so no payload can state semantics.
        /// </summary>
        private static void WriteRepeatedAction(BinaryWriter writer, PendingRepeatedAction pending)
        {
            writer.Write(pending != null);
            if (pending == null)
            {
                return;
            }

            writer.Write(pending.ResolutionId.Value);
            writer.Write(pending.SourceCardInstanceId.Value);
            writer.Write(pending.SourceDefinitionReference.DefinitionId.Value);
            writer.Write(pending.SourceDefinitionReference.ContentRevision);
            writer.Write(pending.ActionDefinitionId.Value);
            writer.Write(pending.EffectDefinitionReference.DefinitionId.Value);
            writer.Write(pending.EffectDefinitionReference.ContentRevision);
            writer.Write(pending.ActorId.Value);
            writer.Write(pending.ControllerId.Value);
            WriteRepeatContinuation(writer, pending.Continuation);
        }

        /// <summary>
        /// Milestone 8.6's schema move, in one place because two continuations carry it: the effect
        /// frame of an instance that is running, and the owed instance that has not started yet.
        ///
        /// Three fields, and deliberately no node identity: which selection a bound target belongs to
        /// is read from the trusted definition when the repeated instance reaches it, so a tampered
        /// payload can move a coordinate but cannot move a rule. The coordinate is written only when
        /// there is one, which keeps every action that never repeats at one extra byte.
        /// </summary>
        private static void WriteRepeatContinuation(BinaryWriter writer, RepeatContinuation repeat)
        {
            writer.Write(repeat.CompletedRepeats);
            writer.Write((int)repeat.BoundTarget.Kind);
            if (repeat.HasBoundTarget)
            {
                writer.Write(BoundTargetIdentity(repeat.BoundTarget));
            }
        }

        /// <summary>
        /// The one identity a board object of each kind has. Written as a kind and a string rather than
        /// as three optional ids, because exactly one of them is ever defined and a payload that named
        /// two would be describing a piece that does not exist.
        /// </summary>
        private static string BoundTargetIdentity(BoardObjectReference target)
        {
            switch (target.Kind)
            {
                case BoardObjectKind.Hero:
                    return target.HeroId.Value;
                case BoardObjectKind.Minion:
                    return target.MinionId.Value;
                case BoardObjectKind.Token:
                    return target.TokenInstanceId.Value;
                default:
                    throw new InvalidOperationException(
                        "A bound repeat target is a hero, a minion or a token.");
            }
        }

        /// <summary>
        /// Reads that continuation back and refuses one that disagrees with the invocation context
        /// beside it. The two say the same thing from two directions - a repeated instance runs under
        /// the repeated context and has a positive count - and a payload that made them disagree would
        /// otherwise be caught by <see cref="PendingResolution"/>'s own guard as an exception rather
        /// than as a rejected snapshot.
        /// </summary>
        private static RepeatContinuation ReadRepeatContinuation(
            BinaryReader reader,
            ActionInvocationContext invocationContext)
        {
            var completedRepeats = reader.ReadInt32();
            var boundTargetKind = SnapshotEnumReader.Read<BoardObjectKind>(
                reader, "repeat continuation bound target kind");
            var boundTarget = default(BoardObjectReference);
            if (boundTargetKind != BoardObjectKind.None)
            {
                var identity = reader.ReadString();
                switch (boundTargetKind)
                {
                    case BoardObjectKind.Hero:
                        boundTarget = BoardObjectReference.ForHero(new HeroId(identity));
                        break;
                    case BoardObjectKind.Minion:
                        boundTarget = BoardObjectReference.ForMinion(new MinionId(identity));
                        break;
                    default:
                        boundTarget = BoardObjectReference.ForToken(new TokenInstanceId(identity));
                        break;
                }
            }

            if (completedRepeats < 0)
            {
                throw Malformed("Snapshot repeat continuation records a negative repeat count.");
            }

            if ((completedRepeats > 0) !=
                (invocationContext == ActionInvocationContext.Repeated))
            {
                throw Malformed(
                    "Snapshot repeat continuation and invocation context disagree about whether this " +
                    "instance is a repeat.");
            }

            if (completedRepeats == 0)
            {
                // A printed instance may already have named the piece its own repeat will bind, so the
                // target is legal here; what is not is a count that says this instance is a repeat when
                // the invocation context says it is not, which the check above refuses.
                return boundTarget.IsDefined
                    ? RepeatContinuation.FirstInstance.Binding(boundTarget)
                    : RepeatContinuation.FirstInstance;
            }

            return boundTarget.IsDefined
                ? RepeatContinuation.NextBoundTo(completedRepeats, boundTarget)
                : RepeatContinuation.Next(completedRepeats);
        }

        /// <summary>
        /// Reads that repeat back and refuses one this build cannot honour. The action it names must be
        /// a Skill this build's trusted definitions supply at the exact effect-definition revision
        /// recorded, so a tampered or superseded repeat is rejected at load rather than throwing when
        /// the correction is answered.
        /// </summary>
        private static PendingRepeatedAction ReadRepeatedAction(
            BinaryReader reader,
            IReadOnlyList<CardDefinition> definitions)
        {
            if (!reader.ReadBoolean())
            {
                return null;
            }

            var repeat = new PendingRepeatedAction(
                new ActionResolutionId(reader.ReadString()),
                new CardInstanceId(reader.ReadString()),
                new CardDefinitionReference(
                    new CardDefinitionId(reader.ReadString()), reader.ReadString()),
                new ActionDefinitionId(reader.ReadString()),
                new EffectDefinitionReference(
                    new EffectDefinitionId(reader.ReadString()), reader.ReadString()),
                new HeroId(reader.ReadString()),
                new HeroId(reader.ReadString()),
                ReadRepeatContinuation(reader, ActionInvocationContext.Repeated));
            if (SkillCardActionStrategyBackend.FindRepeatableAction(definitions, repeat) == null)
            {
                throw Malformed(
                    "Snapshot pending repeat names an action this build's trusted definitions do not supply.");
            }

            return repeat;
        }

        private static CardActionResolutionAuthority ReadCardAction(
            BinaryReader reader,
            AdjacentMoveAuthority board,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> cards,
            MatchParticipants participants,
            MatchContentCatalogs catalogs,
            MarkerAttachmentRegister markers,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            TieBreakerState tieBreaker)
        {
            var kind = reader.ReadInt32();
            var coinLedger = ReadCoinLedger(reader);
            if (kind == NoCardActionAuthorityKind) return null;
            var lanePush = ReadImmediateLanePush(reader);
            var preAction = ReadPreAction(
                reader, definitions, cards, participants, catalogs,
                CardActionResolutionAuthority.CreateRestoredComponents(catalogs, board, markers),
                modifications, heroItems);

            CardActionResolutionRestoreState state;
            switch ((CardActionContinuationKind)kind)
            {
                case CardActionContinuationKind.None:
                    state = CardActionResolutionRestoreState.WithoutPendingAction(coinLedger, lanePush);
                    break;
                case CardActionContinuationKind.Movement:
                    state = CardActionResolutionRestoreState.WithPendingMovement(
                        ReadMovementResolution(reader), coinLedger, lanePush);
                    break;
                case CardActionContinuationKind.Attack:
                    state = CardActionResolutionRestoreState.WithPendingAttack(
                        ReadAttackResolution(reader), coinLedger, lanePush);
                    break;
                case CardActionContinuationKind.Skill:
                    state = CardActionResolutionRestoreState.WithPendingSkill(
                        ReadSkillResolution(
                            reader, definitions, cards, catalogs.Capabilities, participants,
                            CardActionResolutionAuthority.CreateRestoredComponents(catalogs, board, markers),
                            modifications, heroItems, catalogs.TriggeredEffects),
                        coinLedger,
                        lanePush);
                    break;
                case CardActionContinuationKind.Correction:
                    var correction = ReadCorrectionResolution(reader, board, participants, tieBreaker);
                    state = CardActionResolutionRestoreState.WithPendingCorrection(
                        correction, coinLedger, ReadRepeatedAction(reader, definitions), lanePush);
                    break;
                case CardActionContinuationKind.AttackCardText:
                    var frame = ReadSkillResolution(
                        reader, definitions, cards, catalogs.Capabilities, participants,
                        CardActionResolutionAuthority.CreateRestoredComponents(catalogs, board, markers),
                        modifications, heroItems, null);
                    var pausedAttack = ReadAttackResolution(reader);
                    var moment = SnapshotEnumReader.Read<CardTextTiming>(
                        reader, "paused card-text moment");
                    var pausedOutcome = SnapshotEnumReader.Read<AttackResolutionOutcome>(
                        reader, "paused card-text attack outcome");
                    state = CardActionResolutionRestoreState.WithPendingCardText(
                        new PendingAttackCardTextResolution(frame, moment, pausedAttack, pausedOutcome),
                        coinLedger,
                        lanePush);
                    break;
                default:
                    throw Malformed("Unknown pending card-action kind.");
            }

            // The snapshot never stores the pending choices; they are derived during
            // restore, so no verified choice is supplied here.
            return CardActionResolutionAuthority.Restore(
                catalogs, board, state.WithPreAction(preAction), null, null, null, definitions, cards,
                participants, markers, modifications, heroItems);
        }

        /// <summary>
        /// Milestone 6.4a. A paused correction writes the owning action's identity, the stage, the
        /// deciding team, and the candidate identities - never a route, a cost, or anything else the
        /// board can answer. Restore re-derives the whole correction from the restored board and the
        /// Tie Breaker and refuses any payload the derivation does not reproduce, so a tampered
        /// candidate is unrepresentable and a tampered identity is refused.
        /// </summary>
        private static void WriteCorrectionResolution(
            BinaryWriter writer, GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection pending)
        {
            writer.Write(pending.ResolutionId.Value);
            writer.Write(pending.SourceCardInstanceId.Value);
            writer.Write(pending.ActionDefinitionId.Value);
            writer.Write(pending.ActorId.Value);
            writer.Write((int)pending.Stage);
            writer.Write(pending.ChoosingTeamId.Value);
            writer.Write((int)pending.ChoosingSide);
            writer.Write(pending.BattleZoneId.Value);
            if (pending.Stage == GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionStage.SelectSubject)
            {
                WriteStrings(writer, pending.SubjectCandidates.Select(subject => subject.MinionId.Value));
            }
            else
            {
                writer.Write(pending.SubjectMinionId.Value);
                writer.Write((int)pending.Kind);
                writer.Write(pending.DestinationCandidates.Count);
                foreach (var candidate in pending.DestinationCandidates)
                {
                    writer.Write(candidate.Destination.Q);
                    writer.Write(candidate.Destination.R);
                }
            }
        }

        private static GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            ReadCorrectionResolution(
                BinaryReader reader,
                AdjacentMoveAuthority board,
                MatchParticipants participants,
                TieBreakerState tieBreaker)
        {
            var resolutionId = new ActionResolutionId(reader.ReadString());
            var cardId = new CardInstanceId(reader.ReadString());
            var actionId = new ActionDefinitionId(reader.ReadString());
            var actorId = new HeroId(reader.ReadString());
            var stage = SnapshotEnumReader.Read<GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionStage>(
                reader, "correction stage");
            var teamId = new TeamId(reader.ReadString());
            var side = SnapshotEnumReader.Read<TeamSide>(reader, "correction choosing side");
            var zoneId = new ZoneId(reader.ReadString());

            if (stage == GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionStage.SelectSubject)
            {
                var subjectIds = ReadStrings(reader, "correction subject candidates");
                var derived = GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionPlanner.DeriveNext(
                    board, participants, tieBreaker, resolutionId, cardId, actionId, actorId);
                if (derived.Kind != GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionStepKind.Choice ||
                    derived.Pending.Stage != GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionStage.SelectSubject ||
                    !derived.Pending.ChoosingTeamId.Equals(teamId) ||
                    derived.Pending.ChoosingSide != side ||
                    !derived.Pending.BattleZoneId.Equals(zoneId) ||
                    !derived.Pending.SubjectCandidates.Select(subject => subject.MinionId.Value)
                        .SequenceEqual(subjectIds))
                {
                    throw Malformed(
                        "Snapshot pending correction subjects do not match the authoritative derivation.");
                }

                return derived.Pending;
            }

            var subjectMinionId = new MinionId(reader.ReadString());
            var kind = SnapshotEnumReader.Read<GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionKind>(
                reader, "correction kind");
            var destinationCount = reader.ReadInt32();
            var destinations = new List<HexCoordinate>(destinationCount);
            for (var index = 0; index < destinationCount; index++)
            {
                var q = reader.ReadInt32();
                var r = reader.ReadInt32();
                destinations.Add(new HexCoordinate(q, r));
            }

            var subject = board.Minions.SingleOrDefault(minion => minion.MinionId.Equals(subjectMinionId));
            if (subject == null ||
                GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInCurrentBattleZone(
                    board, subject.OccupiedSpace.SpaceId))
            {
                throw Malformed("Snapshot pending correction names a subject the board does not show displaced.");
            }

            var zoneCoordinates = GuardsDigital.Core.Rules.Modifications.BattleZoneQuery
                .FindCurrentBattleZoneCoordinates(board);
            var destinationStep = GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionPlanner
                .DeriveDestinationStep(
                    board, participants, zoneId, zoneCoordinates, subject,
                    resolutionId, cardId, actionId, actorId);
            if (destinationStep.Kind != GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionStepKind.Choice ||
                destinationStep.Pending.Kind != kind ||
                !destinationStep.Pending.ChoosingTeamId.Equals(teamId) ||
                destinationStep.Pending.ChoosingSide != side ||
                !destinationStep.Pending.BattleZoneId.Equals(zoneId) ||
                !destinationStep.Pending.DestinationCandidates.Select(candidate => candidate.Destination)
                    .SequenceEqual(destinations))
            {
                throw Malformed(
                    "Snapshot pending correction destinations do not match the authoritative derivation.");
            }

            return destinationStep.Pending;
        }

        /// <summary>
        /// Milestone 6.2a. A pending Skill writes identity, position, and results - never the semantics
        /// of what it is doing.
        ///
        /// The effect definition appears only as an ID plus content revision, and the pending choice's
        /// candidate list is deliberately absent: restore re-derives candidates by asking the trusted
        /// handler and then checks the payload's selection against them. A tampered candidate is
        /// therefore not merely rejected, it is unrepresentable. This is the pattern
        /// <see cref="ReadCardAction"/> already used for movement and attack choices.
        /// </summary>
        private static void WriteSkillResolution(BinaryWriter writer, PendingResolution pending)
        {
            WriteCommonResolution(writer, new ActionResolutionId(pending.ResolutionId.Value),
                pending.SourceCardInstanceId, pending.SourceDefinitionReference,
                pending.ActionDefinitionId, pending.ActorId, pending.InvocationContext,
                new ActionResolutionStepId("skill"));
            writer.Write(pending.ControllerId.Value);
            writer.Write(pending.EffectDefinitionReference.DefinitionId.Value);
            writer.Write(pending.EffectDefinitionReference.ContentRevision);
            WriteRepeatContinuation(writer, pending.Repeat);

            writer.Write(pending.Continuation.Count);
            foreach (var frame in pending.Continuation)
            {
                writer.Write(frame.NodeId.Value);
                writer.Write(frame.ChildIndex);
            }

            writer.Write(pending.CompletedOutcomes.Count);
            foreach (var outcome in pending.CompletedOutcomes)
            {
                writer.Write(outcome.NodeId.Value);
                writer.Write((int)outcome.ResultKind);
                writer.Write(outcome.Outputs.Count);
                foreach (var output in outcome.Outputs)
                {
                    writer.Write(output.OutputName);
                    WriteEffectValue(writer, output.Value);
                }
            }

            writer.Write(pending.AcceptedChoices.Count);
            foreach (var record in pending.AcceptedChoices)
            {
                writer.Write(record.NodeId.Value);
                writer.Write(record.ChoiceId.Value);
                writer.Write((int)record.ResolvedBy);
                writer.Write(record.Selected.Count);
                foreach (var candidate in record.Selected) WriteEffectCandidate(writer, candidate);
            }

            writer.Write(pending.PendingChoice != null);
            if (pending.PendingChoice == null) return;

            var choice = pending.PendingChoice;
            writer.Write(choice.ChoiceId.Value);
            writer.Write(choice.NodeId.Value);
            writer.Write(choice.ChooserId.Value);
            writer.Write((int)choice.Kind);
            writer.Write(choice.MinimumSelections);
            writer.Write(choice.MaximumSelections);
            writer.Write(choice.SelectedCandidates.Count);
            foreach (var candidate in choice.SelectedCandidates) WriteEffectCandidate(writer, candidate);
        }

        /// <summary>
        /// Rebinds a pending Skill to the trusted match definitions before any of its state is
        /// accepted. The payload names an effect definition; the semantics come from the configured
        /// card, and a payload that names a definition or node this build does not have is refused
        /// rather than reconstructed.
        /// </summary>
        private static PendingResolution ReadSkillResolution(
            BinaryReader reader,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> cards,
            EffectCapabilityCatalog capabilities,
            MatchParticipants participants,
            MatchComponents components,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            TriggeredEffectCatalog triggeredEffects)
        {
            var common = ReadCommonResolution(reader);
            var controllerId = new HeroId(reader.ReadString());
            var effectDefinitionId = new EffectDefinitionId(reader.ReadString());
            var effectContentRevision = reader.ReadString();
            var effectReference = new EffectDefinitionReference(effectDefinitionId, effectContentRevision);

            var trusted = FindTrustedEffectRoot(definitions, common, effectReference) ??
                FindTrustedTriggeredEffectRoot(
                    definitions, common, effectReference, triggeredEffects);
            if (trusted == null)
            {
                throw new SnapshotLoadException(
                    SnapshotLoadRejectionReason.EffectDefinitionMissing,
                    $"Snapshot names effect definition '{effectReference}', which the configured match definitions do not provide for action '{common.ActionId}'.");
            }

            var repeat = ReadRepeatContinuation(reader, common.InvocationContext);

            var continuation = ReadMany(reader, "effect continuation frames", () =>
            {
                var nodeId = new EffectNodeId(reader.ReadString());
                var childIndex = reader.ReadInt32();
                if (!trusted.TryGetNode(nodeId, out _))
                {
                    throw Malformed($"Snapshot effect continuation names unknown node '{nodeId}'.");
                }

                return new EffectContinuationFrame(nodeId, childIndex);
            });

            var outcomes = ReadMany(reader, "effect node outcomes", () =>
            {
                var nodeId = new EffectNodeId(reader.ReadString());
                var resultKind = SnapshotEnumReader.Read<EffectExecutionResultKind>(
                    reader, "effect node result kind");
                if (!trusted.TryGetNode(nodeId, out var node))
                {
                    throw Malformed($"Snapshot effect outcome names unknown node '{nodeId}'.");
                }

                var outputs = ReadMany(reader, "effect node outputs", () =>
                {
                    var outputName = reader.ReadString();
                    var value = ReadEffectValue(reader);
                    var declared = node.Outputs.FirstOrDefault(candidate =>
                        string.Equals(candidate.OutputName, outputName, StringComparison.Ordinal));
                    if (string.IsNullOrEmpty(declared.OutputName) || declared.ValueKind != value.Kind)
                    {
                        throw Malformed(
                            $"Snapshot effect outcome declares output '{outputName}' that node '{nodeId}' does not publish at that type.");
                    }

                    return new EffectOutcomeFact(outputName, value);
                });

                return new EffectNodeOutcome(nodeId, resultKind, outputs);
            });

            var accepted = ReadMany(reader, "accepted effect choices", () =>
            {
                var nodeId = new EffectNodeId(reader.ReadString());
                var choiceId = new EffectChoiceId(reader.ReadString());
                var resolvedBy = SnapshotEnumReader.Read<EffectChoiceCommandKind>(
                    reader, "accepted effect choice resolution");
                var selected = ReadMany(reader, "accepted effect choice selection", () => ReadEffectCandidate(reader));
                return new EffectChoiceRecord(nodeId, choiceId, resolvedBy, selected);
            });

            var restored = new PendingResolution(
                new EffectResolutionId(common.ResolutionId.Value),
                common.CardId,
                common.DefinitionReference,
                common.ActionId,
                trusted.Reference,
                common.ActorId,
                controllerId,
                common.InvocationContext,
                continuation,
                outcomes,
                accepted,
                repeat: repeat);
            if (!reader.ReadBoolean())
            {
                return restored;
            }

            var choiceId = new EffectChoiceId(reader.ReadString());
            var nodeId = new EffectNodeId(reader.ReadString());
            var chooserId = new HeroId(reader.ReadString());
            var choiceKind = SnapshotEnumReader.Read<EffectChoiceKind>(reader, "pending effect choice kind");
            var minimum = reader.ReadInt32();
            var maximum = reader.ReadInt32();
            var selected = ReadMany(reader, "pending effect choice selection", () => ReadEffectCandidate(reader));
            if (!trusted.TryGetNode(nodeId, out var node))
            {
                throw Malformed($"Snapshot pending effect choice names unknown node '{nodeId}'.");
            }

            // Milestone 7.1a: the restored instances, not an empty view. A capability whose candidates
            // come from a hand must be re-asked against the cards this same payload just restored, for
            // the same reason milestone 6.2 re-asks a board capability against the restored board. The
            // cards are read before any pending action is decoded, which is what makes it possible.
            // Milestone 8.5. The enclosing action's kind is read off the trusted definition the frame
            // names rather than persisted beside it: the payload already records which card and which
            // action the resolution began from, so re-deriving the kind from the content is exact and
            // adds no wire field, while a written one could disagree with the definition it names.
            var resolution = new EffectResolutionContext(
                trusted, capabilities, participants, null, 1, 1, modifications,
                EffectCardState.Of(definitions, cards),
                EnclosingActionKindOf(definitions, common.DefinitionReference, common.ActionId),
                components: components, heroItems: heroItems);

            // Candidates are never written. A control node's options come from the trusted
            // definition; a capability's come from asking the trusted handler again. Either way a
            // payload can only ever narrow an authoritative candidate set, never invent one.
            // A confirm-or-decline choice offers nothing to pick between, so there is no candidate
            // set to re-derive and no capability to ask - the control node itself opened it.
            var candidates =
                choiceKind == EffectChoiceKind.ConfirmOrDecline
                    ? Array.Empty<EffectChoiceCandidate>()
                    : choiceKind == EffectChoiceKind.SelectOption
                        ? node.ChildNodeIds.Select(EffectChoiceCandidate.Option).ToArray()
                        : DeriveCapabilityCandidates(node, restored, resolution);
            if (selected.Any(candidate => !candidates.Contains(candidate)))
            {
                throw Malformed(
                    $"Snapshot pending effect choice selects a candidate node '{nodeId}' does not offer.");
            }

            if (maximum > candidates.Length)
            {
                throw Malformed(
                    $"Snapshot pending effect choice at node '{nodeId}' allows {maximum} selections " +
                    $"from the {candidates.Length} the restored state offers.");
            }

            // Milestone 6.5ac: which options are unperformable is never written either. It is asked
            // again here, against the state this same payload has just restored, for the reason the
            // candidate set is: a refusal is a reading of the board, the supply, and the timing as they
            // now stand, so carrying one in the payload could only carry a stale one.
            var unavailable = choiceKind == EffectChoiceKind.SelectOption
                ? EffectOptionFeasibilityQuery.FindUnperformableOptions(
                    node, restored, restored.CompletedOutcomes, resolution)
                : Array.Empty<EffectChoiceCandidate>();
            if (selected.Any(candidate => unavailable.Contains(candidate)))
            {
                throw Malformed(
                    $"Snapshot pending effect choice selects an option node '{nodeId}' cannot perform.");
            }

            // Milestone 6.5ae: nor is the stop written. It follows from the refusals just re-derived
            // and the trusted node's own failure policy, so a payload restored after the last refusal
            // lifts offers the option again instead of a stop, and one restored into the position the
            // pause was written in offers the stop again without having carried it.
            var canStop = choiceKind == EffectChoiceKind.SelectOption &&
                candidates.Length > 0 &&
                unavailable.Count == candidates.Length &&
                node.FailurePolicy == EffectFailurePolicy.Mandatory;

            return restored.With(
                restored.Continuation,
                restored.CompletedOutcomes,
                restored.AcceptedChoices,
                new PendingEffectChoice(
                    new EffectResolutionId(common.ResolutionId.Value), choiceId, nodeId, chooserId,
                    choiceKind, candidates, minimum, maximum, selected, unavailable, canStop));
        }

        /// <summary>
        /// Re-asks the registered handler what it offered. Handlers are pure and stateless, so replaying
        /// the request reproduces the authoritative candidate set without the payload describing it.
        /// </summary>
        /// <remarks>
        /// Milestone 6.2 made the state this request runs against load-bearing. A capability whose
        /// candidates come from the board - which empty spaces may take a token, which same-type tokens
        /// may be taken back - must be re-asked against the state this same payload just restored, not
        /// against an empty context. The board and the marker register are both already read by the time
        /// a pending Skill is decoded, which is what makes that possible; before this milestone every
        /// capability derived its candidates from identities alone and the difference could not show.
        /// Milestone 6.2c extended the same discipline to input bindings: the request resolves the
        /// node's inputs from the choice-less restored frame through the resolver's own binding path,
        /// so a capability whose candidates depend on a bound input re-derives what live execution
        /// offered instead of being re-asked with none. Milestone 6.8l extended it once more: a
        /// selection that declares what it selects for offered only the objects that consumer accepts,
        /// so the same narrowing is applied to the re-asked set. It is asked here rather than carried
        /// for the reason the set itself is - eligibility is a reading of the board as it now stands.
        /// </remarks>
        private static EffectChoiceCandidate[] DeriveCapabilityCandidates(
            EffectNodeDefinition node,
            PendingResolution restored,
            EffectResolutionContext resolution)
        {
            var handler = resolution.Catalog.Load(node.Capability);
            var offered = handler.Execute(new EffectCapabilityContext(
                node.Id,
                node.FailurePolicy,
                restored.ActorId,
                restored.ControllerId,
                restored.SourceCardInstanceId,
                restored.SourceDefinitionReference,
                restored.ActionDefinitionId,
                EffectResolutionSession.ResolveInputBindings(node, restored, restored.CompletedOutcomes),
                1,
                1,
                resolution.Participants,
                null,
                resolution.Modifications,
                resolution.CardState,
                resolution.EnclosingActionKind,
                resolution.Components,
                resolution.HeroItems,
                // Milestone 8.8 extended the same discipline once more, to a capability that asks more
                // than one question: what it is offering depends on the answers it already holds, and
                // those are the resolution's own accepted choices, which this payload has just
                // restored. Without them the re-ask reproduces the first question rather than the one
                // the frame paused on.
                EffectResolutionSession.AnswersAlreadyGiven(restored, node)));
            if (offered.Choice == null)
            {
                throw Malformed(
                    $"Snapshot pending effect choice names node '{node.Id}', whose capability no longer requests a choice.");
            }

            return EffectSelectionEligibilityQuery
                .EligibleCandidates(
                    node, offered.Choice.Candidates, restored, restored.CompletedOutcomes, resolution)
                .Select(EffectChoiceCandidate.Value_)
                .ToArray();
        }


        /// <summary>
        /// The kind of action a restored effect frame is running inside, read from the trusted card
        /// definition the frame names. Milestone 8.5: p.18's immunity is declared per action type, so a
        /// capability that filters by activity has to be told the same activity after a restore that it
        /// was told live.
        /// </summary>
        private static ActionKind EnclosingActionKindOf(
            IReadOnlyList<CardDefinition> definitions,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId)
        {
            var definition = definitions.FirstOrDefault(candidate =>
                candidate.Reference.Equals(sourceDefinitionReference));
            var action = definition?.ActionDefinitions
                .FirstOrDefault(candidate => candidate.Id.Equals(actionDefinitionId));
            if (action == null)
            {
                if (definition?.UltimateRule != null &&
                    definition.UltimateRule.EffectSourceId.Equals(actionDefinitionId))
                {
                    return ActionKind.Skill;
                }

                throw Malformed(
                    $"Snapshot pending effect resolution names action '{actionDefinitionId}' on card " +
                    $"definition '{sourceDefinitionReference}', which this build does not supply.");
            }

            return action.Kind;
        }
        private static EffectDefinition FindTrustedEffectRoot(
            IReadOnlyList<CardDefinition> definitions,
            CommonResolution common,
            EffectDefinitionReference reference)
        {
            foreach (var definition in definitions)
            {
                if (!definition.Reference.Equals(common.DefinitionReference)) continue;
                foreach (var action in definition.ActionDefinitions)
                {
                    if (action.Id.Equals(common.ActionId) &&
                        action.EffectRoot != null &&
                        action.EffectRoot.Reference.Equals(reference))
                    {
                        return action.EffectRoot;
                    }
                }
            }

            return null;
        }

        private static EffectDefinition FindTrustedTriggeredEffectRoot(
            IReadOnlyList<CardDefinition> definitions,
            CommonResolution common,
            EffectDefinitionReference reference,
            TriggeredEffectCatalog triggeredEffects)
        {
            if (triggeredEffects == null || !triggeredEffects.TryGet(reference, out var effect))
                return null;

            var definition = definitions.SingleOrDefault(value =>
                value.Reference.Equals(common.DefinitionReference));
            var rule = definition?.UltimateRule;
            return rule != null && rule.EffectSourceId.Equals(common.ActionId) &&
                rule.TriggeredEffect.HasValue && rule.TriggeredEffect.Value.Equals(reference)
                    ? effect
                    : null;
        }

        private static EffectChoiceCandidate ReadEffectCandidate(BinaryReader reader)
        {
            var kind = SnapshotEnumReader.Read<EffectChoiceCandidateKind>(reader, "effect choice candidate kind");
            return kind == EffectChoiceCandidateKind.Option
                ? EffectChoiceCandidate.Option(new EffectNodeId(reader.ReadString()))
                : EffectChoiceCandidate.Value_(ReadEffectValue(reader));
        }

        private static EffectValue ReadEffectValue(BinaryReader reader)
        {
            var kind = SnapshotEnumReader.Read<EffectValueKind>(reader, "effect value kind");
            switch (kind)
            {
                case EffectValueKind.Boolean:
                    return EffectValue.Boolean(reader.ReadBoolean());
                case EffectValueKind.Integer:
                    return EffectValue.Integer(reader.ReadInt32());
                case EffectValueKind.Hero:
                    return EffectValue.Hero(new HeroId(reader.ReadString()));
                case EffectValueKind.Minion:
                    return EffectValue.Minion(new MinionId(reader.ReadString()));
                case EffectValueKind.CardInstance:
                    return EffectValue.Card(new CardInstanceId(reader.ReadString()));
                case EffectValueKind.Coordinate:
                    return EffectValue.Space(ReadCoordinate(reader));
                default:
                    throw Malformed("Snapshot effect value has no defined kind.");
            }
        }

        private static void WriteEffectCandidate(BinaryWriter writer, EffectChoiceCandidate candidate)
        {
            writer.Write((int)candidate.Kind);
            if (candidate.Kind == EffectChoiceCandidateKind.Option)
            {
                writer.Write(candidate.OptionNodeId.Value);
                return;
            }

            WriteEffectValue(writer, candidate.Value);
        }

        private static void WriteEffectValue(BinaryWriter writer, EffectValue value)
        {
            if (value.Kind == EffectValueKind.Node)
            {
                // Milestone 6.8l. A node reference addresses the trusted definition a restore already
                // loads, so writing one would put a second, weaker statement of the same thing in the
                // payload. The typed value union carries it for a literal binding and nowhere else,
                // and this is the boundary that says so.
                throw new InvalidOperationException(
                    "A node reference is a definition-time address and is never written to a snapshot.");
            }

            writer.Write((int)value.Kind);
            switch (value.Kind)
            {
                case EffectValueKind.Boolean:
                    writer.Write(value.BooleanValue);
                    break;
                case EffectValueKind.Integer:
                    writer.Write(value.IntegerValue);
                    break;
                case EffectValueKind.Hero:
                    writer.Write(value.HeroId.Value);
                    break;
                case EffectValueKind.Minion:
                    writer.Write(value.MinionId.Value);
                    break;
                case EffectValueKind.CardInstance:
                    writer.Write(value.CardInstanceId.Value);
                    break;
                case EffectValueKind.Coordinate:
                    WriteCoordinate(writer, value.Coordinate);
                    break;
            }
        }

        private static void WriteMovementResolution(BinaryWriter writer, PendingCardActionResolution pending)
        {
            WriteCommonResolution(writer, pending.ResolutionId, pending.SourceCardInstanceId,
                pending.SourceDefinitionReference, pending.ActionDefinitionId, pending.ActorId,
                pending.InvocationContext, pending.CurrentStepId);
            var state = pending.MovementSessionState;
            writer.Write(state.MovementAllowance);
            writer.Write(state.RemainingAllowance);
            writer.Write(state.IsComplete);
            WriteCoordinates(writer, state.Route);
        }

        private static PendingCardActionResolution ReadMovementResolution(BinaryReader reader)
        {
            var common = ReadCommonResolution(reader);
            var allowance = reader.ReadInt32();
            var remaining = reader.ReadInt32();
            var complete = reader.ReadBoolean();
            var route = ReadCoordinates(reader, "movement route");
            var events = new List<AdjacentMoveEvent>();
            for (var index = 0; index < route.Length - 1; index++)
                events.Add(new AdjacentMoveEvent(common.ActorId, route[index], route[index + 1]));
            return new PendingCardActionResolution(
                common.ResolutionId, common.CardId, common.DefinitionReference, common.ActionId,
                common.ActorId, common.InvocationContext, common.StepId,
                new MovementSessionState(common.ActorId, allowance, remaining, complete, route, events));
        }

        private static void WriteAttackResolution(BinaryWriter writer, PendingAttackResolution pending)
        {
            WriteCommonResolution(writer, pending.ResolutionId, pending.SourceCardInstanceId,
                pending.SourceDefinitionReference, pending.ActionDefinitionId, pending.ActorId,
                pending.InvocationContext, pending.CurrentStepId);
            writer.Write((int)pending.Phase);
            writer.Write((int)pending.Target.Kind);
            if (pending.Target.Kind == AttackTargetKind.Hero) writer.Write(pending.TargetHeroId.Value);
            if (pending.Target.Kind == AttackTargetKind.Minion) writer.Write(pending.TargetMinionId.Value);
            WritePrintedValue(writer, pending.AttackValue);
            writer.Write(pending.DefenseOffer.HasValue);
            if (pending.DefenseOffer.HasValue) WriteActionOffer(writer, pending.DefenseOffer.Value);
            WritePrintedValue(writer, pending.DefenseValue);

            // Milestone 8.10, schema v23. The unit a repeated instance may not take - Thunder
            // Boomerang's "on a different target". It has to cross the wire because the legal target
            // set is re-derived on restore rather than stored, so a restored repeat with no exclusion
            // would offer the target the card just forbade.
            writer.Write((int)pending.ExcludedTarget.Kind);
            if (pending.ExcludedTarget.Kind == AttackTargetKind.Hero)
                writer.Write(pending.ExcludedTarget.HeroId.Value);
            if (pending.ExcludedTarget.Kind == AttackTargetKind.Minion)
                writer.Write(pending.ExcludedTarget.MinionId.Value);

            // Milestone 8.11, schema v24. What rulebook p.14.s Block did, decided at the page.s step 3
            // where the defender.s card is revealed and read at step 6. It crosses the wire because
            // those two are separated by every pause the page allows in between - the defender.s own
            // card text among them - and because a Block is the one Defense that reaches the
            // comparison with no Defense Total to re-derive it from.
            writer.Write((int)pending.BlockResult);

            // Milestone 8.19a, schema v25. Where the target stood before rulebook p.14's consequences.
            // It crosses the wire because it is the one target fact the restored board cannot answer:
            // step 6 removes a defeated hero and step 7 applies text that may name the space it stood
            // on, and a rider that pauses at step 7 has a whole save and reload between the two.
            writer.Write(pending.TargetSpaceBeforeConsequences.HasValue);
            if (pending.TargetSpaceBeforeConsequences.HasValue)
            {
                WriteCoordinate(writer, pending.TargetSpaceBeforeConsequences.Value);
            }
        }

        private static PendingAttackResolution ReadAttackResolution(BinaryReader reader)
        {
            var common = ReadCommonResolution(reader);
            var phase = SnapshotEnumReader.Read<AttackResolutionPhase>(reader, "attack resolution phase");
            var targetKind = SnapshotEnumReader.Read<AttackTargetKind>(reader, "attack target kind");
            // A switch mirroring WriteAttackResolution rather than a ternary chain. The chain's final
            // `default` silently produced an empty target and, because it also skipped the ReadString
            // that a Hero or Minion target consumes, left every field after it misaligned - surfacing,
            // if at all, as the unrelated trailing-data error. Read<T> already rejects an undefined
            // value; this makes a future member added to AttackTargetKind fail loudly here too,
            // matching the pending card-action kind switch above.
            AttackTarget target;
            switch (targetKind)
            {
                case AttackTargetKind.Hero:
                    target = AttackTarget.Hero(new HeroId(reader.ReadString()));
                    break;
                case AttackTargetKind.Minion:
                    target = AttackTarget.Minion(new MinionId(reader.ReadString()));
                    break;
                case AttackTargetKind.None:
                    target = default;
                    break;
                default:
                    throw Malformed($"Unhandled attack target kind '{targetKind}'.");
            }
            var attackValue = ReadPrintedValue(reader);
            var defenseOffer = reader.ReadBoolean() ? (ActionOffer?)ReadActionOffer(reader) : null;
            var defenseValue = ReadPrintedValue(reader);
            var excludedKind = SnapshotEnumReader.Read<AttackTargetKind>(
                reader, "repeated attack excluded target kind");
            AttackTarget excludedTarget;
            switch (excludedKind)
            {
                case AttackTargetKind.Hero:
                    excludedTarget = AttackTarget.Hero(new HeroId(reader.ReadString()));
                    break;
                case AttackTargetKind.Minion:
                    excludedTarget = AttackTarget.Minion(new MinionId(reader.ReadString()));
                    break;
                case AttackTargetKind.None:
                    excludedTarget = default;
                    break;
                default:
                    throw Malformed($"Unhandled excluded attack target kind '{excludedKind}'.");
            }

            var blockResult = SnapshotEnumReader.Read<DefenseBlockResult>(
                reader, "pending attack Block result");
            var targetSpace = reader.ReadBoolean() ? (HexCoordinate?)ReadCoordinate(reader) : null;
            if (targetSpace.HasValue && !target.IsDefined)
            {
                throw Malformed(
                    "Snapshot pending attack records where a target stood while naming no target.");
            }

            return new PendingAttackResolution(
                common.ResolutionId, common.CardId, common.DefinitionReference, common.ActionId,
                common.ActorId, common.InvocationContext, common.StepId, phase, target,
                attackValue, defenseOffer, defenseValue, excludedTarget, blockResult, targetSpace);
        }

        private static void WriteCoinLedger(BinaryWriter writer, HeroCoinLedgerRestoreStateHolder holder)
        {
            var (isPresent, ledger) = holder;
            writer.Write(isPresent);
            if (!isPresent) return;
            var (coinBalances, awardedOutcomeIds) = ledger;
            writer.Write(coinBalances.Count);
            foreach (var balance in coinBalances)
            {
                writer.Write(balance.Key.Value);
                writer.Write(balance.Value);
            }
            writer.Write(awardedOutcomeIds.Count);
            foreach (var outcomeId in awardedOutcomeIds) writer.Write(outcomeId.Value);
        }

        private static HeroCoinLedgerRestoreStateHolder ReadCoinLedger(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return HeroCoinLedgerRestoreStateHolder.Absent();
            var balances = ReadMany(reader, "hero coin balances", () =>
                new KeyValuePair<HeroId, int>(new HeroId(reader.ReadString()), reader.ReadInt32()));
            var outcomes = ReadMany(reader, "awarded Defeat outcomes", () =>
                new DefeatOutcomeId(reader.ReadString()));
            return HeroCoinLedgerRestoreStateHolder.Present(
                new HeroCoinLedgerRestoreState(balances, outcomes));
        }

    }
}
