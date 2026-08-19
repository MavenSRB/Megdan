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
        private static void WriteTurn(BinaryWriter writer, TurnResolutionAuthority turn)
        {
            var state = turn.CaptureRestoreState();
            var (isStarted, isEnded, tieBreakerState, initiative) = state;
            writer.Write(isStarted);
            writer.Write(isEnded);
            writer.Write((int)tieBreakerState.RevealedSide);
            var (pendingChoice, selectedActor) = initiative;
            WritePendingInitiativeChoice(writer, pendingChoice);
            WriteSelectedActor(writer, selectedActor);
            WriteCardAction(writer, turn.CardActions?.CaptureRestoreState());
        }

        private static TurnResolutionAuthority ReadTurn(
            BinaryReader reader,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> cards,
            MatchParticipants participants,
            AdjacentMoveAuthority board,
            ActiveEffectLifecycle timing,
            MarkerAttachmentRegister markers,
            int roundIndex,
            int turnIndex,
            EffectCapabilityCatalog capabilities,
            GuardsDigital.Core.Rules.LanePush.LanePushConsumption lanePush,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            out CardActionResolutionAuthority cardActions)
        {
            var started = reader.ReadBoolean();
            var ended = reader.ReadBoolean();
            var tieBreaker = new TieBreakerState(
                SnapshotEnumReader.Read<TeamSide>(reader, "turn tie-breaker revealed side"));
            var pendingChoice = ReadPendingInitiativeChoice(reader);
            var selectedActor = ReadSelectedActor(reader);
            // Milestone 6.8: the restored continuation is cross-checked against the modifications the
            // restored effects and tokens supply, so a Defense total or a target list that a modifier
            // produced live is reproduced here rather than looking like tampering.
            cardActions = ReadCardAction(
                reader, board, definitions, cards, participants, capabilities, markers,
                RuleModificationQuery.Build(timing, board, participants, tieBreaker),
                heroItems,
                tieBreaker);
            return TurnResolutionAuthority.Restore(
                definitions,
                cards,
                participants,
                cardActions,
                timing,
                new TurnResolutionRestoreState(
                    started,
                    ended,
                    tieBreaker,
                    new InitiativeResolutionRestoreState(pendingChoice, selectedActor),
                    roundIndex,
                    turnIndex),
                lanePush);
        }

        private static void WriteTiming(BinaryWriter writer, ActiveEffectLifecycleRestoreState state)
        {
            var (effects, continuousSources) = state;
            writer.Write(effects.Count);
            foreach (var effect in effects.OrderBy(value => value.Id.Value, StringComparer.Ordinal))
            {
                writer.Write(effect.Id.Value);
                var definition = effect.Definition;
                writer.Write(definition.DefinitionId);
                // Milestone 6.2a. Identity and runtime state only. Scope, duration, the three lifecycle
                // checkpoints, cancellation conditions, the cross-round flag, effect kind, uniqueness
                // key, and trigger checkpoints all left this payload: they are definition semantics, and
                // a restore takes them from the trusted catalog by definition ID. A payload that could
                // still state them could redefine what a saved effect means.
                writer.Write(effect.ControllerHeroId.Value);
                writer.Write((int)effect.ControllerTeamSide);
                writer.Write(effect.AffectedHeroId.IsDefined);
                if (effect.AffectedHeroId.IsDefined) writer.Write(effect.AffectedHeroId.Value);
                writer.Write(effect.SourceCardInstanceId.Value);
                writer.Write(effect.SourceActionDefinitionId.Value);
                writer.Write(effect.CreatedRoundIndex);
                writer.Write(effect.CreatedTurnIndex);
                writer.Write(effect.CreationSequence);
                writer.Write((int)effect.Phase);
                writer.Write(effect.ActivatedRoundIndex.HasValue);
                if (effect.ActivatedRoundIndex.HasValue) writer.Write(effect.ActivatedRoundIndex.Value);
                writer.Write(effect.ActivatedTurnIndex.HasValue);
                if (effect.ActivatedTurnIndex.HasValue) writer.Write(effect.ActivatedTurnIndex.Value);
                writer.Write(effect.EndedAt.HasValue);
                if (effect.EndedAt.HasValue) writer.Write((int)effect.EndedAt.Value);
            }

            writer.Write(continuousSources.Count);
            foreach (var source in continuousSources.OrderBy(value => value.SourceId, StringComparer.Ordinal))
            {
                writer.Write(source.SourceId);
                writer.Write((int)source.Kind);
                writer.Write(source.ControllerHeroId.Value);
                writer.Write(source.RuleKind);
                writer.Write(source.ApplicabilityKind);
                writer.Write(source.SourceCardInstanceId.IsDefined);
                if (source.SourceCardInstanceId.IsDefined) writer.Write(source.SourceCardInstanceId.Value);
            }
        }

        private static ActiveEffectLifecycleRestoreState ReadTiming(
            BinaryReader reader,
            EffectCapabilityCatalog capabilities)
        {
            var effects = ReadMany(reader, "Active effects", () =>
            {
                var id = new ActiveEffectId(reader.ReadString());
                var definitionId = reader.ReadString();
                var controller = new HeroId(reader.ReadString());
                var side = SnapshotEnumReader.Read<TeamSide>(reader, "Active-effect controlling side");
                var affectedHero = reader.ReadBoolean() ? new HeroId(reader.ReadString()) : default;

                // The one line acceptance check 5 turns on: semantics are looked up, never rebuilt.
                if (!capabilities.TryGetActiveEffectDefinition(definitionId, out var definition))
                {
                    throw new SnapshotLoadException(
                        SnapshotLoadRejectionReason.EffectDefinitionMissing,
                        $"Snapshot names Active-effect definition '{definitionId}', which no registered capability in this build declares.");
                }

                var sourceCard = new CardInstanceId(reader.ReadString());
                var sourceAction = new ActionDefinitionId(reader.ReadString());
                var createdRound = reader.ReadInt32();
                var createdTurn = reader.ReadInt32();
                var sequence = reader.ReadInt64();
                var phase = SnapshotEnumReader.Read<ActiveEffectPhase>(reader, "Active-effect phase");
                var activatedRound = reader.ReadBoolean() ? (int?)reader.ReadInt32() : null;
                var activatedTurn = reader.ReadBoolean() ? (int?)reader.ReadInt32() : null;
                var endedAt = SnapshotEnumReader.ReadNullable<TimingCheckpoint>(reader, "Active-effect ended-at checkpoint");
                return new ActiveEffectInstance(id, definition, controller, side, affectedHero,
                    sourceCard, sourceAction, createdRound,
                    createdTurn, sequence, phase, activatedRound, activatedTurn, endedAt);
            });
            var continuous = ReadMany(reader, "continuous rule sources", () =>
            {
                var sourceId = reader.ReadString();
                var kind = SnapshotEnumReader.Read<ContinuousRuleSourceKind>(reader, "continuous rule source kind");
                var controller = new HeroId(reader.ReadString());
                var ruleKind = reader.ReadString();
                var applicability = reader.ReadString();
                var sourceCard = reader.ReadBoolean() ? new CardInstanceId(reader.ReadString()) : default;
                return new ContinuousRuleSource(sourceId, kind, controller, ruleKind, applicability, sourceCard);
            });
            return new ActiveEffectLifecycleRestoreState(effects, continuous);
        }

        private static void WritePendingInitiativeChoice(BinaryWriter writer, PendingInitiativeChoice choice)
        {
            writer.Write(choice != null);
            if (choice == null) return;
            writer.Write((int)choice.OwningSide);
            writer.Write(choice.InitiativeTotal);
            writer.Write(choice.FlipTieBreakerAfterResolution);
            WriteStrings(writer, choice.CandidateCardInstanceIds.Select(value => value.Value));
        }

        private static PendingInitiativeChoice ReadPendingInitiativeChoice(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return null;
            var side = SnapshotEnumReader.Read<TeamSide>(reader, "Initiative choice owning side");
            var total = reader.ReadInt32();
            var flip = reader.ReadBoolean();
            var cards = ReadStrings(reader, "Initiative candidates").Select(value => new CardInstanceId(value));
            return new PendingInitiativeChoice(side, total, cards, flip);
        }

        private static void WriteSelectedActor(BinaryWriter writer, SelectedInitiativeActor actor)
        {
            writer.Write(actor.IsDefined);
            if (!actor.IsDefined) return;
            writer.Write(actor.CardInstanceId.Value);
            writer.Write(actor.ActorId.Value);
            writer.Write((int)actor.TeamSide);
            writer.Write(actor.InitiativeTotal);
            writer.Write(actor.FlipTieBreakerAfterResolution);
        }

        private static SelectedInitiativeActor ReadSelectedActor(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return default;
            return new SelectedInitiativeActor(
                new CardInstanceId(reader.ReadString()),
                new HeroId(reader.ReadString()),
                SnapshotEnumReader.Read<TeamSide>(reader, "selected Initiative actor side"),
                reader.ReadInt32(),
                reader.ReadBoolean());
        }

        private const int NoCardActionAuthorityKind = -1;

    }
}
