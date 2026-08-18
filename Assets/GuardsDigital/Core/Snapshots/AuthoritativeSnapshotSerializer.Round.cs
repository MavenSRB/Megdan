using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Snapshots
{
    public static partial class AuthoritativeSnapshotSerializer
    {
        private static void WriteRound(BinaryWriter writer, RoundResolutionAuthority round)
        {
            var (cursor, history, endOfRound, roundCardActions, systemTransitions) =
                round.CaptureRestoreState();
            var (roundIndex, turnIndex, phase, tieBreakerState) = cursor;
            writer.Write(roundIndex);
            writer.Write(turnIndex);
            writer.Write((int)phase);
            writer.Write((int)tieBreakerState.RevealedSide);

            var (turnSlots, completedRounds) = history;
            WriteSlots(writer, turnSlots);
            writer.Write(completedRounds.Count);
            foreach (var completed in completedRounds)
            {
                writer.Write(completed.RoundIndex);
                WriteSlots(writer, completed.TurnSlots);
                WriteOutcomes(writer, completed.EndOfRoundOutcomes);
            }

            var (currentStage, outcomes, pendingMinionBattleChoice, pendingLanePushRequest) = endOfRound;
            writer.Write(currentStage.HasValue);
            if (currentStage.HasValue) writer.Write((int)currentStage.Value);
            WriteOutcomes(writer, outcomes);
            WritePendingMinionBattleChoice(writer, pendingMinionBattleChoice);
            WritePendingLanePushRequest(writer, pendingLanePushRequest);
            WriteTiming(writer, round.Timing.CaptureRestoreState());
            WriteMarkers(writer, round.Markers.CaptureRestoreState());
            WriteLanePush(writer, round.LanePush?.LanePush);
            WritePendingSystemTransitionStep(writer, systemTransitions);

            var (hasCardActions, roundDefeatRewards) = roundCardActions;
            writer.Write(hasCardActions);
            WriteDefeatRewards(writer, roundDefeatRewards);

            writer.Write(round.CurrentCommitmentAuthority != null);
            if (round.CurrentCommitmentAuthority != null)
            {
                WriteCommitment(writer, round.CurrentCommitmentAuthority.CaptureRestoreState());
            }

            writer.Write(round.CurrentTurnAuthority != null);
            if (round.CurrentTurnAuthority != null) WriteTurn(writer, round.CurrentTurnAuthority);
        }

        private static RoundResolutionAuthority ReadRound(
            BinaryReader reader,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> cards,
            MatchParticipants participants,
            AdjacentMoveAuthority board,
            EffectCapabilityCatalog capabilities,
            ComponentDefinitionCatalog components,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionCatalog transitions)
        {
            var roundIndex = reader.ReadInt32();
            var turnIndex = reader.ReadInt32();
            var phase = SnapshotEnumReader.Read<RoundResolutionPhase>(reader, "round resolution phase");
            var tieBreaker = new TieBreakerState(
                SnapshotEnumReader.Read<TeamSide>(reader, "round tie-breaker revealed side"));
            var slots = ReadSlots(reader, cards, participants);
            var completed = ReadMany(reader, "completed rounds", () =>
            {
                var completedIndex = reader.ReadInt32();
                return new CompletedRoundRecord(
                    completedIndex,
                    ReadSlots(reader, cards, participants),
                    ReadOutcomes(reader));
            });
            var stage = SnapshotEnumReader.ReadNullable<EndOfRoundStage>(reader, "current end-of-round stage");
            var outcomes = ReadOutcomes(reader);
            var pendingMinionBattleChoice = ReadPendingMinionBattleChoice(reader);
            var pendingLanePushRequest = ReadPendingLanePushRequest(reader);
            var timing = ActiveEffectLifecycle.Restore(ReadTiming(reader, capabilities));
            var markers = MarkerAttachmentRegister.Restore(ReadMarkers(reader, components, participants));

            // The Lane Push is read before the continuation that may be paused inside it, because a
            // paused respawn is re-derived against this board and these Wave counters rather than
            // trusted from the payload.
            var lanePushAuthority = ReadLanePush(reader, board);
            var transitionRules = lanePushAuthority == null
                ? GuardsDigital.Core.Rules.Transitions.DeclaredSystemTransitionStepRules.Instance
                : GuardsDigital.Core.Rules.Transitions.MatchSystemTransitions.CreateRules(lanePushAuthority);
            var systemTransitions = ReadSystemTransitions(reader, transitions, transitionRules);
            var lanePush = lanePushAuthority == null
                ? null
                : new GuardsDigital.Core.Rules.LanePush.LanePushConsumption(
                    lanePushAuthority, systemTransitions);
            var hasRoundCardActions = reader.ReadBoolean();
            var roundDefeatRewards = ReadDefeatRewards(reader);

            TurnCardCommitmentAuthority commitment = null;
            if (reader.ReadBoolean())
            {
                commitment = TurnCardCommitmentAuthority.Restore(cards, participants, ReadCommitment(reader));
            }

            CardActionResolutionAuthority cardActions = null;
            TurnResolutionAuthority turn = null;
            if (reader.ReadBoolean())
            {
                turn = ReadTurn(reader, definitions, cards, participants, board, timing, markers,
                    roundIndex, turnIndex, capabilities, lanePush, out cardActions);
            }
            else if (hasRoundCardActions)
            {
                // No pending action means nothing here consumes the set today, but the site states
                // the honest live value rather than an implicit none, exactly as the pending-turn
                // path does; if this arm ever gains a consumer it is already correct.
                cardActions = CardActionResolutionAuthority.Restore(
                    board,
                    CardActionResolutionRestoreState.WithoutPendingAction(roundDefeatRewards),
                    null,
                    null,
                    null,
                    definitions,
                    cards,
                    participants,
                    markers,
                    RuleModificationQuery.Build(timing, board, participants, tieBreaker));
            }

            return RoundResolutionAuthority.Restore(
                definitions,
                cards,
                participants,
                new RoundResolutionRestoreState(
                    new RoundCursorRestoreState(roundIndex, turnIndex, phase, tieBreaker),
                    new RoundHistoryRestoreState(slots, completed),
                    new EndOfRoundRestoreState(
                        stage,
                        outcomes,
                        pendingMinionBattleChoice,
                        pendingLanePushRequest),
                    new RoundCardActionsRestoreState(hasRoundCardActions, roundDefeatRewards),
                    systemTransitions.CaptureRestoreState()),
                commitment,
                turn,
                cardActions,
                timing,
                markers,
                board,
                systemTransitions,
                lanePush);
        }

        /// <summary>
        /// Milestone 6.11. Rulebook p.8's shared Wave counters, the victory sentence that ended the game
        /// if either has, and the push now running.
        ///
        /// Milestone 6.10 wrote none of this and said why: nothing a live match could reach read a Wave
        /// counter, so a persisted one would have been a wire field no restore could disagree about.
        /// The transition is reachable now, so a restore that defaulted the pool would hand a match back
        /// counters it had already spent, and one that forgot the running push's sides would resume a
        /// transition that no longer knew who had won it.
        /// </summary>
        private static void WriteLanePush(
            BinaryWriter writer, GuardsDigital.Core.Rules.LanePush.LanePushTransitionAuthority lanePush)
        {
            writer.Write(lanePush != null);
            if (lanePush == null) return;

            var state = lanePush.CaptureRestoreState();
            state.Deconstruct(
                out var remainingWaveCounters, out var gameEndingCause, out var winningSide,
                out var running);
            writer.Write(remainingWaveCounters);
            writer.Write((int)gameEndingCause);
            writer.Write((int)winningSide);
            writer.Write(running != null);
            if (running == null) return;

            writer.Write((int)running.WinningSide);
            writer.Write((int)running.LosingSide);
            writer.Write((int)running.TieBreakerSide);
            writer.Write(running.PreviousBattleZoneId.HasValue);
            if (running.PreviousBattleZoneId.HasValue)
            {
                writer.Write(running.PreviousBattleZoneId.Value.Value);
            }

            writer.Write(running.Outcomes.Count);
            foreach (var outcome in running.Outcomes)
            {
                writer.Write((int)outcome.Stage);
                writer.Write((int)outcome.Kind);
                writer.Write((int)outcome.GameEndingCause);
            }
        }

        private static GuardsDigital.Core.Rules.LanePush.LanePushTransitionAuthority ReadLanePush(
            BinaryReader reader, AdjacentMoveAuthority board)
        {
            if (!reader.ReadBoolean())
            {
                return null;
            }

            var remainingWaveCounters = reader.ReadInt32();
            var gameEndingCause = SnapshotEnumReader.Read<
                GuardsDigital.Core.Rules.LanePush.LanePushGameEndingCause>(
                reader, "Lane-Push game-ending cause");
            var winningSide = SnapshotEnumReader.Read<TeamSide>(reader, "Lane-Push winning side");
            GuardsDigital.Core.Rules.LanePush.RunningLanePush running = null;
            if (reader.ReadBoolean())
            {
                var runningWinner = SnapshotEnumReader.Read<TeamSide>(reader, "running push winner");
                var runningLoser = SnapshotEnumReader.Read<TeamSide>(reader, "running push loser");
                var tieBreakerSide = SnapshotEnumReader.Read<TeamSide>(reader, "running push tie-breaker");
                ZoneId? previousBattleZoneId = reader.ReadBoolean()
                    ? new ZoneId(reader.ReadString())
                    : (ZoneId?)null;
                var outcomes = ReadMany(reader, "Lane-Push stage outcomes", () =>
                    new GuardsDigital.Core.Rules.LanePush.LanePushStageOutcome(
                        SnapshotEnumReader.Read<
                            GuardsDigital.Core.Rules.LanePush.LanePushTransitionStage>(
                            reader, "Lane-Push stage"),
                        SnapshotEnumReader.Read<
                            GuardsDigital.Core.Rules.LanePush.LanePushStageOutcomeKind>(
                            reader, "Lane-Push stage outcome"),
                        SnapshotEnumReader.Read<
                            GuardsDigital.Core.Rules.LanePush.LanePushGameEndingCause>(
                            reader, "Lane-Push stage victory cause")));
                running = new GuardsDigital.Core.Rules.LanePush.RunningLanePush(
                    runningWinner, runningLoser, tieBreakerSide, previousBattleZoneId, outcomes);
            }

            return GuardsDigital.Core.Rules.LanePush.LanePushTransitionAuthority.Restore(
                new GuardsDigital.Core.Rules.LanePush.LanePushRestoreState(
                    remainingWaveCounters, gameEndingCause, winningSide, running),
                board);
        }

        /// <summary>
        /// Milestone 6.10a. A paused transition writes identity and recorded facts only - which
        /// transition, which step, which item of it, who chooses, what they may choose, and which steps
        /// and items have already happened. What those steps *are* stays in the catalog, for the reason
        /// the Active-effect section states at v8 and the marker section repeats: a payload may say which
        /// content this is and what has happened to it, never what that content means.
        ///
        /// The single leading boolean is the whole cost for a match with no transition running.
        /// </summary>
        private static void WritePendingSystemTransitionStep(
            BinaryWriter writer,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionRestoreState state)
        {
            state.Deconstruct(out var pending);
            writer.Write(pending != null);
            if (pending == null) return;

            writer.Write(pending.InstanceId.Value);
            writer.Write(pending.DefinitionId.Value);
            writer.Write(pending.StepId.Value);

            // Milestone 6.11's item, written as a present/absent pair rather than a blank string,
            // because "this pause is about no item" and "this pause is about an item whose name is
            // empty" are different claims and only the first is legal.
            writer.Write(pending.ItemId.IsDefined);
            if (pending.ItemId.IsDefined)
            {
                writer.Write(pending.ItemId.Value);
            }

            writer.Write((int)pending.ChoosingSide);
            WriteStrings(writer, pending.Options.Select(option => option.Value));
            WriteStrings(writer, pending.CompletedStepIds.Select(step => step.Value));
            WriteStrings(writer, pending.CompletedItemIds.Select(item => item.Value));
        }

        private static GuardsDigital.Core.Rules.Transitions.SystemTransitionAuthority ReadSystemTransitions(
            BinaryReader reader,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionCatalog transitions,
            GuardsDigital.Core.Rules.Transitions.ISystemTransitionStepRules transitionRules)
        {
            GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep pending = null;
            if (reader.ReadBoolean())
            {
                var instanceId =
                    new GuardsDigital.Core.Rules.Transitions.SystemTransitionInstanceId(reader.ReadString());
                var definitionId =
                    new GuardsDigital.Core.Rules.Transitions.SystemTransitionDefinitionId(reader.ReadString());
                var stepId =
                    new GuardsDigital.Core.Rules.Transitions.SystemTransitionStepId(reader.ReadString());
                var itemId = reader.ReadBoolean()
                    ? new GuardsDigital.Core.Rules.Transitions.SystemTransitionItemId(reader.ReadString())
                    : GuardsDigital.Core.Rules.Transitions.SystemTransitionItemId.None;
                pending = new GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep(
                    instanceId,
                    definitionId,
                    stepId,
                    itemId,
                    SnapshotEnumReader.Read<TeamSide>(reader, "transition choosing side"),
                    ReadStrings(reader, "transition options")
                        .Select(value => new GuardsDigital.Core.Rules.Transitions.SystemTransitionOptionId(value)),
                    ReadStrings(reader, "transition completed steps")
                        .Select(value => new GuardsDigital.Core.Rules.Transitions.SystemTransitionStepId(value)),
                    ReadStrings(reader, "transition completed items")
                        .Select(value => new GuardsDigital.Core.Rules.Transitions.SystemTransitionItemId(value)));
            }

            // The authority's own restore is what refuses a payload the trusted definitions do not
            // reproduce, so the reader states no second opinion about what a legal continuation is.
            // The rules travel with it because milestone 6.11's derived steps have no catalog options
            // to be compared with, and re-deriving them over the restored board is what takes their
            // place.
            return GuardsDigital.Core.Rules.Transitions.SystemTransitionAuthority.Restore(
                new GuardsDigital.Core.Rules.Transitions.SystemTransitionRestoreState(pending),
                transitions,
                transitionRules);
        }

        /// <summary>
        /// Milestone 6.2. An attached marker writes identity, provenance, and who holds it. Its supply
        /// size and return condition are absent from the wire and rebound from the component catalog,
        /// for the reason the Active-effect section states at v8: a payload may not say what a piece of
        /// content means. That is what makes the marker's return state trusted rather than asserted.
        /// </summary>
        private static void WriteMarkers(BinaryWriter writer, MarkerAttachmentRestoreState state)
        {
            // Explicit Deconstruct rather than tuple syntax: C# has no one-element deconstruction, and
            // the point of destructuring here is the arity break when a persisted field is added.
            state.Deconstruct(out var attachments);
            writer.Write(attachments.Count);
            foreach (var attachment in attachments.OrderBy(value => value.Id.Value, StringComparer.Ordinal))
            {
                writer.Write(attachment.Id.Value);
                writer.Write(attachment.Definition.Id.Value);
                writer.Write(attachment.Definition.Revision);
                writer.Write(attachment.OwnerHeroId.Value ?? string.Empty);
                writer.Write(attachment.HolderHeroId.Value);
                writer.Write(attachment.SourceCardInstanceId.Value ?? string.Empty);
                writer.Write(attachment.SourceActionDefinitionId.Value ?? string.Empty);
            }
        }

        private static MarkerAttachmentRestoreState ReadMarkers(
            BinaryReader reader,
            ComponentDefinitionCatalog components,
            MatchParticipants participants)
        {
            return new MarkerAttachmentRestoreState(ReadMany(reader, "marker attachments", () =>
            {
                var id = new MarkerInstanceId(reader.ReadString());
                var reference = new MarkerDefinitionReference(
                    new MarkerDefinitionId(reader.ReadString()), reader.ReadInt32());
                var owner = ReadOptionalHeroId(reader);
                var holder = new HeroId(reader.ReadString());
                // A marker is held by a hero, so a payload naming one this match does not have is
                // refused here rather than producing an attachment nothing can ever return.
                participants.GetHero(holder);
                var sourceCard = ReadOptionalCardInstanceId(reader);
                var sourceActionValue = reader.ReadString();
                return new MarkerAttachment(
                    id,
                    LoadTrustedMarkerDefinition(components, reference),
                    owner,
                    holder,
                    sourceCard,
                    string.IsNullOrEmpty(sourceActionValue) ? default : new ActionDefinitionId(sourceActionValue));
            }));
        }

        private static MarkerDefinition LoadTrustedMarkerDefinition(
            ComponentDefinitionCatalog components,
            MarkerDefinitionReference reference)
        {
            try
            {
                return components.LoadMarker(reference);
            }
            catch (ComponentDefinitionLoadException exception)
            {
                throw new SnapshotLoadException(
                    exception.Reason == ComponentDefinitionLoadRejectionReason.MissingDefinition
                        ? SnapshotLoadRejectionReason.ComponentDefinitionMissing
                        : SnapshotLoadRejectionReason.ComponentDefinitionRevisionMismatch,
                    exception.Message,
                    exception);
            }
        }

        private static void WritePendingMinionBattleChoice(
            BinaryWriter writer,
            PendingMinionBattleChoice choice)
        {
            writer.Write(choice != null);
            if (choice == null)
            {
                return;
            }

            writer.Write(choice.ResolutionId.Value);
            writer.Write(choice.ChoiceId.Value);
            writer.Write(choice.LosingTeamId.Value);
            writer.Write((int)choice.ChoosingSide);
            writer.Write(choice.BattleZoneId.Value);
            writer.Write(choice.RequiredRemovalCount);
            writer.Write(choice.Candidates.Count);
            foreach (var candidate in choice.Candidates)
            {
                writer.Write((int)candidate.Unit.Kind);
                writer.Write(candidate.Unit.Kind == RuleObjectKind.Hero
                    ? candidate.HeroId.Value
                    : candidate.MinionId.Value);
                writer.Write((int)candidate.MinionType);
            }

            writer.Write(choice.SelectedUnits.Count);
            foreach (var unit in choice.SelectedUnits)
            {
                writer.Write((int)unit.Kind);
                writer.Write(unit.Kind == RuleObjectKind.Hero ? unit.HeroId.Value : unit.MinionId.Value);
            }
        }

        private static PendingMinionBattleChoice ReadPendingMinionBattleChoice(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
            {
                return null;
            }

            var resolutionId = new EndOfRoundResolutionId(reader.ReadString());
            var choiceId = new EndOfRoundChoiceId(reader.ReadString());
            var losingTeamId = new TeamId(reader.ReadString());
            var choosingSide = SnapshotEnumReader.Read<TeamSide>(reader, "Minion Battle choosing side");
            var battleZoneId = new ZoneId(reader.ReadString());
            var requiredRemovalCount = reader.ReadInt32();
            var candidates = ReadMany(reader, "Minion Battle candidates", () =>
            {
                var kind = SnapshotEnumReader.Read<RuleObjectKind>(reader, "Minion Battle candidate kind");
                var identity = reader.ReadString();
                var type = SnapshotEnumReader.Read<StandardMinionType>(
                    reader, "Minion Battle candidate type");
                return kind == RuleObjectKind.Hero
                    ? new MinionBattleCandidate(new HeroId(identity))
                    : new MinionBattleCandidate(new MinionId(identity), type);
            });
            var selected = ReadMany(reader, "selected Minion Battle units", () =>
            {
                var kind = SnapshotEnumReader.Read<RuleObjectKind>(reader, "selected Minion Battle unit kind");
                var identity = reader.ReadString();
                return kind == RuleObjectKind.Hero
                    ? RuleObject.ForHero(new HeroId(identity))
                    : RuleObject.ForMinion(new MinionId(identity));
            });
            return new PendingMinionBattleChoice(
                resolutionId,
                choiceId,
                losingTeamId,
                choosingSide,
                battleZoneId,
                requiredRemovalCount,
                candidates,
                selected);
        }

        private static void WritePendingLanePushRequest(
            BinaryWriter writer,
            PendingLanePushRequest request)
        {
            writer.Write(request != null);
            if (request == null)
            {
                return;
            }

            writer.Write(request.RequestId.Value);
            writer.Write(request.RoundIndex);
            writer.Write(request.BattleZoneId.Value);
            writer.Write(request.WinningTeamId.Value);
            writer.Write((int)request.WinningSide);
            writer.Write(request.LosingTeamId.Value);
            writer.Write((int)request.LosingSide);
            writer.Write(request.CauseResolutionId.Value);
        }

        private static PendingLanePushRequest ReadPendingLanePushRequest(BinaryReader reader)
        {
            if (!reader.ReadBoolean())
            {
                return null;
            }

            return new PendingLanePushRequest(
                new LanePushRequestId(reader.ReadString()),
                reader.ReadInt32(),
                new ZoneId(reader.ReadString()),
                new TeamId(reader.ReadString()),
                SnapshotEnumReader.Read<TeamSide>(reader, "Lane Push winning side"),
                new TeamId(reader.ReadString()),
                SnapshotEnumReader.Read<TeamSide>(reader, "Lane Push losing side"),
                new EndOfRoundResolutionId(reader.ReadString()));
        }

        private static void WriteCommitment(BinaryWriter writer, TurnCardCommitmentRestoreState state)
        {
            var (commitments, passes, isRevealed) = state;
            writer.Write(isRevealed);
            writer.Write(commitments.Count);
            foreach (var pair in commitments)
            {
                writer.Write(pair.Key.Value);
                writer.Write(pair.Value.Value);
            }
            writer.Write(passes.Count);
            foreach (var hero in passes)
            {
                writer.Write(hero.Value);
            }
        }

        private static TurnCardCommitmentRestoreState ReadCommitment(BinaryReader reader)
        {
            var revealed = reader.ReadBoolean();
            var commitments = ReadMany(reader, "commitments", () => new KeyValuePair<HeroId, CardInstanceId>(
                new HeroId(reader.ReadString()),
                new CardInstanceId(reader.ReadString())));
            var passes = ReadMany(reader, "passes", () => new HeroId(reader.ReadString()));
            return new TurnCardCommitmentRestoreState(commitments, passes, revealed);
        }

    }
}
