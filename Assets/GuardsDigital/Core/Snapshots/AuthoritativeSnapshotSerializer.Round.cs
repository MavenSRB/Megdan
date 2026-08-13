using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
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
            AdjacentMoveAuthority board)
        {
            var roundIndex = reader.ReadInt32();
            var turnIndex = reader.ReadInt32();
            var phase = (RoundResolutionPhase)reader.ReadInt32();
            var tieBreaker = new TieBreakerState((TeamSide)reader.ReadInt32());
            var slots = ReadSlots(reader, cards, participants);
            var completed = ReadMany(reader, "completed rounds", () =>
            {
                var completedIndex = reader.ReadInt32();
                return new CompletedRoundRecord(
                    completedIndex,
                    ReadSlots(reader, cards, participants),
                    ReadOutcomes(reader));
            });
            var hasStage = reader.ReadBoolean();
            var stage = hasStage ? (EndOfRoundStage?)(EndOfRoundStage)reader.ReadInt32() : null;
            var outcomes = ReadOutcomes(reader);
            var pendingMinionBattleChoice = ReadPendingMinionBattleChoice(reader);
            var pendingLanePushRequest = ReadPendingLanePushRequest(reader);
            var timing = ActiveEffectLifecycle.Restore(ReadTiming(reader));
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
                turn = ReadTurn(reader, definitions, cards, participants, board, timing,
                    roundIndex, turnIndex, out cardActions);
            }
            else if (hasRoundCardActions)
            {
                cardActions = CardActionResolutionAuthority.Restore(
                    board,
                    CardActionResolutionRestoreState.WithoutPendingAction(roundDefeatRewards),
                    null,
                    null,
                    null,
                    definitions,
                    cards,
                    participants);
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
                board);
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
            var choosingSide = (TeamSide)reader.ReadInt32();
            var battleZoneId = new ZoneId(reader.ReadString());
            var requiredRemovalCount = reader.ReadInt32();
            var candidates = ReadMany(reader, "Minion Battle candidates", () =>
                new MinionBattleCandidate(
                    new MinionId(reader.ReadString()),
                    (StandardMinionType)reader.ReadInt32()));
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
                (TeamSide)reader.ReadInt32(),
                new TeamId(reader.ReadString()),
                (TeamSide)reader.ReadInt32(),
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
