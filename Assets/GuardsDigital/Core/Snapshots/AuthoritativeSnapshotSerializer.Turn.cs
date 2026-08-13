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
            int roundIndex,
            int turnIndex,
            out CardActionResolutionAuthority cardActions)
        {
            var started = reader.ReadBoolean();
            var ended = reader.ReadBoolean();
            var tieBreaker = new TieBreakerState((TeamSide)reader.ReadInt32());
            var pendingChoice = ReadPendingInitiativeChoice(reader);
            var selectedActor = ReadSelectedActor(reader);
            cardActions = ReadCardAction(reader, board, definitions, cards, participants);
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
                    turnIndex));
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
                writer.Write(definition.ControllerHeroId.Value);
                writer.Write((int)definition.ControllerTeamSide);
                writer.Write((int)definition.AffectedScope);
                writer.Write(definition.AffectedHeroId.IsDefined);
                if (definition.AffectedHeroId.IsDefined) writer.Write(definition.AffectedHeroId.Value);
                writer.Write((int)definition.DurationKind);
                writer.Write((int)definition.CreationCheckpoint);
                writer.Write((int)definition.ActivationCheckpoint);
                writer.Write((int)definition.ExpirationCheckpoint);
                writer.Write(definition.CancellationConditions.Count);
                foreach (var condition in definition.CancellationConditions) writer.Write((int)condition);
                writer.Write(definition.CarriesAcrossRoundBoundary);
                writer.Write(definition.EffectKind);
                writer.Write(definition.UniquenessKey);
                writer.Write(definition.TriggerCheckpoints.Count);
                foreach (var trigger in definition.TriggerCheckpoints) writer.Write((int)trigger);
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

        private static ActiveEffectLifecycleRestoreState ReadTiming(BinaryReader reader)
        {
            var effects = ReadMany(reader, "Active effects", () =>
            {
                var id = new ActiveEffectId(reader.ReadString());
                var definitionId = reader.ReadString();
                var controller = new HeroId(reader.ReadString());
                var side = (TeamSide)reader.ReadInt32();
                var scope = (ActiveEffectAffectedScopeKind)reader.ReadInt32();
                var affectedHero = reader.ReadBoolean() ? new HeroId(reader.ReadString()) : default;
                var duration = (ActiveEffectDurationKind)reader.ReadInt32();
                var creation = (TimingCheckpoint)reader.ReadInt32();
                var activation = (TimingCheckpoint)reader.ReadInt32();
                var expiration = (TimingCheckpoint)reader.ReadInt32();
                var conditions = ReadMany(reader, "Active-effect cancellation conditions",
                    () => (ActiveEffectCancellationCondition)reader.ReadInt32());
                var carries = reader.ReadBoolean();
                var effectKind = reader.ReadString();
                var uniquenessKey = reader.ReadString();
                var triggers = ReadMany(reader, "Active-effect trigger checkpoints",
                    () => (TimingCheckpoint)reader.ReadInt32());
                var definition = new ActiveEffectDefinition(definitionId, controller, side, scope, affectedHero,
                    duration, creation, activation, expiration, conditions, carries, effectKind,
                    uniquenessKey, triggers);
                var sourceCard = new CardInstanceId(reader.ReadString());
                var sourceAction = new ActionDefinitionId(reader.ReadString());
                var createdRound = reader.ReadInt32();
                var createdTurn = reader.ReadInt32();
                var sequence = reader.ReadInt64();
                var phase = (ActiveEffectPhase)reader.ReadInt32();
                var activatedRound = reader.ReadBoolean() ? (int?)reader.ReadInt32() : null;
                var activatedTurn = reader.ReadBoolean() ? (int?)reader.ReadInt32() : null;
                var endedAt = reader.ReadBoolean() ? (TimingCheckpoint?)reader.ReadInt32() : null;
                return new ActiveEffectInstance(id, definition, sourceCard, sourceAction, createdRound,
                    createdTurn, sequence, phase, activatedRound, activatedTurn, endedAt);
            });
            var continuous = ReadMany(reader, "continuous rule sources", () =>
            {
                var sourceId = reader.ReadString();
                var kind = (ContinuousRuleSourceKind)reader.ReadInt32();
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
            var side = (TeamSide)reader.ReadInt32();
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
                (TeamSide)reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadBoolean());
        }

        private const int NoCardActionAuthorityKind = -1;

    }
}
