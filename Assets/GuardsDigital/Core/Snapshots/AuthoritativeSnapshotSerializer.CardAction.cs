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
        private static void WriteCardAction(BinaryWriter writer, CardActionResolutionRestoreState state)
        {
            if (state == null)
            {
                writer.Write(NoCardActionAuthorityKind);
                WriteDefeatRewards(writer, DefeatRewardRestoreStateHolder.Absent());
                return;
            }

            var (continuationKind, pendingMovement, pendingAttack, defeatRewards) = state;
            writer.Write((int)continuationKind);
            WriteDefeatRewards(writer, defeatRewards);
            if (continuationKind == CardActionContinuationKind.Movement)
            {
                WriteMovementResolution(writer, pendingMovement);
            }

            if (continuationKind == CardActionContinuationKind.Attack)
            {
                WriteAttackResolution(writer, pendingAttack);
            }
        }

        private static CardActionResolutionAuthority ReadCardAction(
            BinaryReader reader,
            AdjacentMoveAuthority board,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> cards,
            MatchParticipants participants)
        {
            var kind = reader.ReadInt32();
            var defeatRewards = ReadDefeatRewards(reader);
            if (kind == NoCardActionAuthorityKind) return null;

            CardActionResolutionRestoreState state;
            switch ((CardActionContinuationKind)kind)
            {
                case CardActionContinuationKind.None:
                    state = CardActionResolutionRestoreState.WithoutPendingAction(defeatRewards);
                    break;
                case CardActionContinuationKind.Movement:
                    state = CardActionResolutionRestoreState.WithPendingMovement(
                        ReadMovementResolution(reader), defeatRewards);
                    break;
                case CardActionContinuationKind.Attack:
                    state = CardActionResolutionRestoreState.WithPendingAttack(
                        ReadAttackResolution(reader), defeatRewards);
                    break;
                default:
                    throw Malformed("Unknown pending card-action kind.");
            }

            // The snapshot never stores the pending choices; they are derived during
            // restore, so no verified choice is supplied here.
            return CardActionResolutionAuthority.Restore(
                board, state, null, null, null, definitions, cards, participants);
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
        }

        private static PendingAttackResolution ReadAttackResolution(BinaryReader reader)
        {
            var common = ReadCommonResolution(reader);
            var phase = (AttackResolutionPhase)reader.ReadInt32();
            var targetKind = (AttackTargetKind)reader.ReadInt32();
            var target = targetKind == AttackTargetKind.Hero
                ? AttackTarget.Hero(new HeroId(reader.ReadString()))
                : targetKind == AttackTargetKind.Minion
                    ? AttackTarget.Minion(new MinionId(reader.ReadString()))
                    : default;
            var attackValue = ReadPrintedValue(reader);
            var defenseOffer = reader.ReadBoolean() ? (ActionOffer?)ReadActionOffer(reader) : null;
            var defenseValue = ReadPrintedValue(reader);
            return new PendingAttackResolution(
                common.ResolutionId, common.CardId, common.DefinitionReference, common.ActionId,
                common.ActorId, common.InvocationContext, common.StepId, phase, target,
                attackValue, defenseOffer, defenseValue);
        }

        private static void WriteDefeatRewards(BinaryWriter writer, DefeatRewardRestoreStateHolder holder)
        {
            var (isPresent, rewards) = holder;
            writer.Write(isPresent);
            if (!isPresent) return;
            var (coinBalances, awardedOutcomeIds) = rewards;
            writer.Write(coinBalances.Count);
            foreach (var balance in coinBalances)
            {
                writer.Write(balance.Key.Value);
                writer.Write(balance.Value);
            }
            writer.Write(awardedOutcomeIds.Count);
            foreach (var outcomeId in awardedOutcomeIds) writer.Write(outcomeId.Value);
        }

        private static DefeatRewardRestoreStateHolder ReadDefeatRewards(BinaryReader reader)
        {
            if (!reader.ReadBoolean()) return DefeatRewardRestoreStateHolder.Absent();
            var balances = ReadMany(reader, "hero coin balances", () =>
                new KeyValuePair<HeroId, int>(new HeroId(reader.ReadString()), reader.ReadInt32()));
            var outcomes = ReadMany(reader, "awarded Defeat outcomes", () =>
                new DefeatOutcomeId(reader.ReadString()));
            return DefeatRewardRestoreStateHolder.Present(
                new DefeatRewardRestoreState(balances, outcomes));
        }

    }
}
