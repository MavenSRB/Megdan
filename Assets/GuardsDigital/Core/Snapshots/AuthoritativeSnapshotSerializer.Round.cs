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
            var (cursor, history, endOfRound, roundCardActions) = round.CaptureRestoreState();
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
            ComponentDefinitionCatalog components)
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
                    roundIndex, turnIndex, capabilities, out cardActions);
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
                    new RoundCardActionsRestoreState(hasRoundCardActions, roundDefeatRewards)),
                commitment,
                turn,
                cardActions,
                timing,
                markers,
                board);
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
                writer.Write(candidate.MinionId.Value);
                writer.Write((int)candidate.MinionType);
            }

            WriteStrings(writer, choice.SelectedMinionIds.Select(value => value.Value));
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
                new MinionBattleCandidate(
                    new MinionId(reader.ReadString()),
                    SnapshotEnumReader.Read<StandardMinionType>(reader, "Minion Battle candidate type")));
            var selected = ReadStrings(reader, "selected Minion Battle minions")
                .Select(value => new MinionId(value));
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
