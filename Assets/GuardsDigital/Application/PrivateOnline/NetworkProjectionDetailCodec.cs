using System;
using System.Collections.Generic;
using System.IO;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.33. Writes the presentation-fidelity half of one seat's projection and reads it
    /// back field by field, refusing anything out of bounds - the same refuse-on-read idiom as the
    /// acting-surface codec it extends.
    /// </summary>
    internal static class NetworkProjectionDetailCodec
    {
        public static void Write(BinaryWriter writer, SeatProjection projection)
        {
            WriteTeamAgreement(writer, projection.TeamAgreement);
            WriteCompletedRounds(writer, projection.CompletedRounds);
            WriteInitiativeChoice(writer, projection.PendingInitiativeChoice);
            WriteMovementResolution(writer, projection.PendingMovementResolution);
            WriteMovementChoice(writer, projection.PendingMovementChoice);
            WriteAttackResolution(writer, projection.PendingAttackResolution);
            WriteAttackTargetChoice(writer, projection.PendingAttackTargetChoice);
            writer.Write(projection.PendingAttackTargetDefensePreviews.Count);
            foreach (var preview in projection.PendingAttackTargetDefensePreviews)
            {
                writer.Write(preview.HeroId.Value);
                writer.Write(preview.HeroBody);
                WriteContributions(writer, preview.MinionContributions);
            }

            WriteDefenseChoice(writer, projection.PendingDefenseChoice);
            WriteDefensePreview(writer, projection.PendingDefensePreview);
            WriteSkillResolution(writer, projection.PendingSkillResolution);
            WriteEffectChoice(writer, projection.PendingEffectChoice);
            writer.Write(projection.PendingEffectChoicePreview.Count);
            foreach (var preview in projection.PendingEffectChoicePreview)
            {
                WriteEffectValue(writer, preview.CandidateValue);
                writer.Write(preview.Coordinate.Q);
                writer.Write(preview.Coordinate.R);
                writer.Write((int)preview.Occupant.Kind);
                WriteOptional(writer, preview.Occupant.HeroId.IsDefined, preview.Occupant.HeroId.Value);
                WriteOptional(writer, preview.Occupant.MinionId.IsDefined, preview.Occupant.MinionId.Value);
                WriteOptional(writer, preview.Occupant.TokenInstanceId.IsDefined,
                    preview.Occupant.TokenInstanceId.Value);
                writer.Write(preview.ResultingCoordinate.HasValue);
                if (preview.ResultingCoordinate.HasValue)
                {
                    writer.Write(preview.ResultingCoordinate.Value.Q);
                    writer.Write(preview.ResultingCoordinate.Value.R);
                }

                writer.Write((int)preview.ConsequenceIntent);
            }

            WriteCorrection(writer, projection.PendingDisplacedMinionCorrection);
            WriteMinionBattleChoice(writer, projection.PendingMinionBattleChoice);
            WriteHeroRespawnChoice(writer, projection.PendingHeroRespawnChoice);
            WriteSystemTransitionStep(writer, projection.PendingSystemTransitionStep);
            WriteSystemTransitionDescription(writer, projection.PendingSystemTransitionDescription);
            WriteRemovalPrevention(writer, projection.PendingRemovalPrevention);
            WriteTurnRelease(writer, projection.PendingTurnRelease);

            writer.Write(projection.AvailableTurnActions.Count);
            foreach (var offer in projection.AvailableTurnActions)
            {
                WriteOffer(writer, offer);
            }

            writer.Write(projection.AvailableFastTravelOptions.Count);
            foreach (var option in projection.AvailableFastTravelOptions)
            {
                WriteOffer(writer, option.ReplacementOffer);
                writer.Write(option.ReplacedMovementActionDefinitionId.Value);
                WriteRoute(writer, option.LegalDestinations);
            }

            writer.Write(projection.AvailableClearOptions.Count);
            foreach (var option in projection.AvailableClearOptions)
            {
                WriteOffer(writer, option.ReplacementOffer);
                writer.Write(option.ReplacedAttackActionDefinitionId.Value);
                writer.Write(option.AdjacentTokens.Count);
                foreach (var token in option.AdjacentTokens)
                {
                    writer.Write(token.Id.Value);
                }

                writer.Write(option.SubsetInvocations.Count);
                foreach (var invocation in option.SubsetInvocations)
                {
                    NetworkCommandCodec.Write(writer, invocation);
                }
            }

            writer.Write(projection.AvailableHoldOffer.HasValue);
            if (projection.AvailableHoldOffer.HasValue)
            {
                WriteOffer(writer, projection.AvailableHoldOffer.Value);
            }

            // Milestone 8.35r. The standing rewind, last in the record so a version 5 peer's
            // mis-read of it stops at the end of the payload rather than sliding every field.
            writer.Write(projection.UndoMarker.IsStated);
            if (projection.UndoMarker.IsStated)
            {
                writer.Write(projection.UndoMarker.HeroId.Value);
                writer.Write(projection.UndoMarker.Count);
            }
        }

        public static bool TryRead(BinaryReader reader, out NetworkProjectionDetailRecord record)
        {
            record = null;
            if (!TryReadTeamAgreement(reader, out var teamAgreement) ||
                !TryReadCompletedRounds(reader, out var completedRounds) ||
                !TryReadInitiativeChoice(reader, out var initiative) ||
                !TryReadMovementResolution(reader, out var movementResolution) ||
                !TryReadMovementChoice(reader, out var movementChoice) ||
                !TryReadAttackResolution(reader, out var attackResolution) ||
                !TryReadAttackTargetChoice(reader, out var attackTargetChoice))
            {
                return false;
            }

            if (!TryReadCount(reader, out var attackPreviewCount))
            {
                return false;
            }

            var attackPreviews = new List<NetworkAttackTargetDefensePreviewRow>(attackPreviewCount);
            for (var index = 0; index < attackPreviewCount; index++)
            {
                var heroId = reader.ReadString();
                var heroBody = reader.ReadInt32();
                if (string.IsNullOrWhiteSpace(heroId) || heroBody < 1 ||
                    !TryReadContributions(reader, out var contributions))
                {
                    return false;
                }

                attackPreviews.Add(new NetworkAttackTargetDefensePreviewRow(heroId, heroBody, contributions));
            }

            if (!TryReadDefenseChoice(reader, out var defenseChoice) ||
                !TryReadDefensePreview(reader, out var defensePreview) ||
                !TryReadSkillResolution(reader, out var skillResolution) ||
                !TryReadEffectChoice(reader, out var effectChoice))
            {
                return false;
            }

            if (!TryReadCount(reader, out var effectPreviewCount))
            {
                return false;
            }

            var effectPreviews = new List<NetworkEffectChoicePreviewRow>(effectPreviewCount);
            for (var index = 0; index < effectPreviewCount; index++)
            {
                if (!TryReadEffectValue(reader, out var candidateValue))
                {
                    return false;
                }

                var q = reader.ReadInt32();
                var r = reader.ReadInt32();
                var occupantKind = reader.ReadInt32();
                if (!Enum.IsDefined(typeof(BoardObjectKind), occupantKind) ||
                    !TryReadOptional(reader, out var occupantHero) ||
                    !TryReadOptional(reader, out var occupantMinion) ||
                    !TryReadOptional(reader, out var occupantToken))
                {
                    return false;
                }

                NetworkCoordinateRow? resulting = null;
                if (reader.ReadBoolean())
                {
                    resulting = new NetworkCoordinateRow(reader.ReadInt32(), reader.ReadInt32());
                }

                var intent = reader.ReadInt32();
                if (!Enum.IsDefined(typeof(EffectChoiceConsequenceIntent), intent))
                {
                    return false;
                }

                effectPreviews.Add(new NetworkEffectChoicePreviewRow(
                    candidateValue, q, r, occupantKind, occupantHero, occupantMinion, occupantToken,
                    resulting, intent));
            }

            if (!TryReadCorrection(reader, out var correction) ||
                !TryReadMinionBattleChoice(reader, out var minionBattle) ||
                !TryReadHeroRespawnChoice(reader, out var respawn) ||
                !TryReadSystemTransitionStep(reader, out var transitionStep) ||
                !TryReadSystemTransitionDescription(reader, out var transitionDescription) ||
                !TryReadRemovalPrevention(reader, out var removalPrevention) ||
                !TryReadTurnRelease(reader, out var turnRelease))
            {
                return false;
            }

            if (!TryReadCount(reader, out var turnActionCount))
            {
                return false;
            }

            var turnActions = new List<NetworkActionOfferRow>(turnActionCount);
            for (var index = 0; index < turnActionCount; index++)
            {
                if (!TryReadOffer(reader, out var offer))
                {
                    return false;
                }

                turnActions.Add(offer);
            }

            if (!TryReadCount(reader, out var fastTravelCount))
            {
                return false;
            }

            var fastTravel = new List<NetworkFastTravelOptionRow>(fastTravelCount);
            for (var index = 0; index < fastTravelCount; index++)
            {
                if (!TryReadOffer(reader, out var offer))
                {
                    return false;
                }

                var replacedId = reader.ReadString();
                if (string.IsNullOrWhiteSpace(replacedId) ||
                    !TryReadRoute(reader, out var destinations))
                {
                    return false;
                }

                fastTravel.Add(new NetworkFastTravelOptionRow(offer, replacedId, destinations));
            }

            if (!TryReadCount(reader, out var clearCount))
            {
                return false;
            }

            var clears = new List<NetworkClearOptionRow>(clearCount);
            for (var index = 0; index < clearCount; index++)
            {
                if (!TryReadOffer(reader, out var offer))
                {
                    return false;
                }

                var replacedId = reader.ReadString();
                if (string.IsNullOrWhiteSpace(replacedId) || !TryReadCount(reader, out var tokenCount))
                {
                    return false;
                }

                var tokens = new List<string>(tokenCount);
                for (var tokenIndex = 0; tokenIndex < tokenCount; tokenIndex++)
                {
                    var tokenId = reader.ReadString();
                    if (string.IsNullOrWhiteSpace(tokenId))
                    {
                        return false;
                    }

                    tokens.Add(tokenId);
                }

                if (!TryReadCount(reader, out var invocationCount))
                {
                    return false;
                }

                var invocations = new List<DirectorCommand>(invocationCount);
                for (var invocationIndex = 0; invocationIndex < invocationCount; invocationIndex++)
                {
                    if (!NetworkCommandCodec.TryRead(reader, out var invocation))
                    {
                        return false;
                    }

                    invocations.Add(invocation);
                }

                clears.Add(new NetworkClearOptionRow(
                    offer, replacedId,
                    Array.AsReadOnly(tokens.ToArray()),
                    Array.AsReadOnly(invocations.ToArray())));
            }

            NetworkActionOfferRow holdOffer = null;
            if (reader.ReadBoolean() && !TryReadOffer(reader, out holdOffer))
            {
                return false;
            }

            NetworkUndoMarkerRow undoMarker = null;
            if (reader.ReadBoolean())
            {
                var undoHeroId = reader.ReadString();
                var undoCount = reader.ReadInt32();
                if (string.IsNullOrWhiteSpace(undoHeroId) || undoCount < 1)
                {
                    return false;
                }

                undoMarker = new NetworkUndoMarkerRow(undoHeroId, undoCount);
            }

            record = new NetworkProjectionDetailRecord(
                teamAgreement,
                completedRounds,
                initiative,
                movementResolution,
                movementChoice,
                attackResolution,
                attackTargetChoice,
                Array.AsReadOnly(attackPreviews.ToArray()),
                defenseChoice,
                defensePreview,
                skillResolution,
                effectChoice,
                Array.AsReadOnly(effectPreviews.ToArray()),
                correction,
                minionBattle,
                respawn,
                transitionStep,
                transitionDescription,
                removalPrevention,
                turnRelease,
                Array.AsReadOnly(turnActions.ToArray()),
                Array.AsReadOnly(fastTravel.ToArray()),
                Array.AsReadOnly(clears.ToArray()),
                holdOffer,
                undoMarker);
            return true;
        }

        private static void WriteTeamAgreement(BinaryWriter writer, TeamAgreementProjection agreement)
        {
            writer.Write(agreement != null);
            if (agreement == null)
            {
                return;
            }

            writer.Write((int)agreement.Kind);
            writer.Write(agreement.ProposalId.Value);
            writer.Write((int)agreement.ChoosingSide);
            writer.Write(agreement.Candidates.Count);
            foreach (var candidate in agreement.Candidates)
            {
                writer.Write(candidate.CardInstanceId.Value);
                writer.Write(candidate.HeroId.Value);
            }

            writer.Write(agreement.Participants.Count);
            foreach (var participant in agreement.Participants)
            {
                writer.Write(participant.HeroId.Value);
                writer.Write(participant.HasStatement);
                WriteOptional(writer, participant.PreferredCardInstanceId.IsDefined,
                    participant.PreferredCardInstanceId.Value);

                // Milestone 9.29. Each participant's own picks - already empty in the projection a
                // seat on the other side is served, so the wire carries only what that seat may read.
                writer.Write(participant.Picks.Count);
                foreach (var unit in participant.Picks)
                {
                    WriteUnit(writer, unit);
                }

                writer.Write(participant.PicksComplete);
            }

            writer.Write(agreement.RequiredSelectionCount);
            writer.Write(agreement.AuthorizedCommands.Count);
            foreach (var statement in agreement.AuthorizedCommands)
            {
                WriteAgreementStatement(writer, statement);
            }
        }

        private static bool TryReadTeamAgreement(BinaryReader reader, out NetworkTeamAgreementRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var kind = reader.ReadInt32();
            var proposalId = reader.ReadString();
            var side = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(TeamAgreementKind), kind) ||
                kind == (int)TeamAgreementKind.None ||
                string.IsNullOrWhiteSpace(proposalId) ||
                !Enum.IsDefined(typeof(TeamSide), side) ||
                !TryReadCount(reader, out var candidateCount))
            {
                return false;
            }

            var candidates = new List<NetworkAgreementCandidateRow>(candidateCount);
            for (var index = 0; index < candidateCount; index++)
            {
                var cardId = reader.ReadString();
                var heroId = reader.ReadString();
                if (string.IsNullOrWhiteSpace(cardId) || string.IsNullOrWhiteSpace(heroId))
                {
                    return false;
                }

                candidates.Add(new NetworkAgreementCandidateRow(cardId, heroId));
            }

            if (!TryReadCount(reader, out var participantCount))
            {
                return false;
            }

            var participants = new List<NetworkAgreementParticipantRow>(participantCount);
            for (var index = 0; index < participantCount; index++)
            {
                var heroId = reader.ReadString();
                var hasStatement = reader.ReadBoolean();
                if (string.IsNullOrWhiteSpace(heroId) || !TryReadOptional(reader, out var preferred) ||
                    !TryReadUnits(reader, out var picks))
                {
                    return false;
                }

                participants.Add(new NetworkAgreementParticipantRow(
                    heroId, hasStatement, preferred, picks, reader.ReadBoolean()));
            }

            var requiredCount = reader.ReadInt32();
            if (requiredCount < 0 || !TryReadCount(reader, out var statementCount))
            {
                return false;
            }

            var statements = new List<TeamAgreementCommand>(statementCount);
            for (var index = 0; index < statementCount; index++)
            {
                if (!TryReadAgreementStatement(reader, out var statement))
                {
                    return false;
                }

                statements.Add(statement);
            }

            row = new NetworkTeamAgreementRow(
                kind, proposalId, side,
                Array.AsReadOnly(candidates.ToArray()),
                Array.AsReadOnly(participants.ToArray()),
                requiredCount,
                Array.AsReadOnly(statements.ToArray()));
            return true;
        }

        /// <summary>
        /// One agreement statement, rebuilt through the coordinator's own factories so the client
        /// resubmits the host's exact instance shape - the command-codec rule, applied here.
        /// </summary>
        internal static void WriteAgreementStatement(BinaryWriter writer, TeamAgreementCommand statement)
        {
            writer.Write(statement.ProposalId.Value);
            writer.Write((int)statement.Kind);
            WriteOptional(writer, statement.PreferredCardInstanceId.IsDefined,
                statement.PreferredCardInstanceId.Value);
            writer.Write(statement.Unit.IsDefined);
            if (statement.Unit.IsDefined)
            {
                WriteUnit(writer, statement.Unit);
            }
        }

        internal static bool TryReadAgreementStatement(
            BinaryReader reader, out TeamAgreementCommand statement)
        {
            statement = null;
            var proposalId = reader.ReadString();
            var kind = reader.ReadInt32();
            if (string.IsNullOrWhiteSpace(proposalId) || !TryReadOptional(reader, out var preferred))
            {
                return false;
            }

            NetworkUnitRow unit = null;
            if (reader.ReadBoolean() && !TryReadUnit(reader, out unit))
            {
                return false;
            }

            var proposal = new TeamAgreementProposalId(proposalId);
            switch (kind)
            {
                case (int)TeamAgreementCommandKind.PreferInitiativeCandidate:
                    if (string.IsNullOrWhiteSpace(preferred) || unit != null)
                    {
                        return false;
                    }

                    statement = TeamAgreementCommand.Prefer(proposal, new CardInstanceId(preferred));
                    return true;
                case (int)TeamAgreementCommandKind.PickMinionBattleRemoval:
                case (int)TeamAgreementCommandKind.UnpickMinionBattleRemoval:
                    if (preferred != null || unit == null)
                    {
                        return false;
                    }

                    var picked = unit.Kind == (int)RuleObjectKind.Hero
                        ? RuleObject.ForHero(new GuardsDigital.Core.Units.Heroes.HeroId(unit.HeroId))
                        : RuleObject.ForMinion(new MinionId(unit.MinionId));
                    statement = kind == (int)TeamAgreementCommandKind.PickMinionBattleRemoval
                        ? TeamAgreementCommand.Pick(proposal, picked)
                        : TeamAgreementCommand.Unpick(proposal, picked);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Milestone 8.35w. This writes what the read model holds and does not bound it
        /// itself, on purpose: the bound lives at
        /// <see cref="MatchSessionProjection.RetainedCompletedRounds"/>, where the host's own view
        /// and every client's are the same window. A cap applied HERE would make the encoded frame
        /// disagree with the projection it was encoded from, which is the one thing convergence is
        /// asserted on. The layout is unchanged - count, then that many records - so a shorter list
        /// is the same wire format carrying fewer rows, and no protocol version moves for it.
        /// </summary>
        private static void WriteCompletedRounds(
            BinaryWriter writer, IReadOnlyList<CompletedRoundRecord> rounds)
        {
            writer.Write(rounds.Count);
            foreach (var round in rounds)
            {
                writer.Write(round.RoundIndex);
                writer.Write(round.TurnSlots.Count);
                foreach (var slot in round.TurnSlots)
                {
                    writer.Write(slot.RoundIndex);
                    writer.Write(slot.TurnIndex);
                    writer.Write(slot.Assignments.Count);
                    foreach (var assignment in slot.Assignments)
                    {
                        writer.Write(assignment.HeroId.Value);
                        WriteOptional(writer, assignment.CardInstanceId.HasValue,
                            assignment.CardInstanceId.HasValue
                                ? assignment.CardInstanceId.Value.Value
                                : null);
                        writer.Write(assignment.PassedWithEmptyHand);
                    }
                }

                writer.Write(round.EndOfRoundOutcomes.Count);
                foreach (var outcome in round.EndOfRoundOutcomes)
                {
                    writer.Write((int)outcome.Stage);
                    writer.Write((int)outcome.Kind);
                }
            }
        }

        private static bool TryReadCompletedRounds(
            BinaryReader reader, out IReadOnlyList<NetworkCompletedRoundRow> rounds)
        {
            rounds = null;
            if (!TryReadCount(reader, out var roundCount))
            {
                return false;
            }

            var rows = new List<NetworkCompletedRoundRow>(roundCount);
            for (var index = 0; index < roundCount; index++)
            {
                var roundIndex = reader.ReadInt32();
                if (roundIndex < 1 || !TryReadCount(reader, out var slotCount))
                {
                    return false;
                }

                var slots = new List<NetworkTurnSlotRow>(slotCount);
                for (var slotIndex = 0; slotIndex < slotCount; slotIndex++)
                {
                    var slotRound = reader.ReadInt32();
                    var slotTurn = reader.ReadInt32();
                    if (!TryReadCount(reader, out var assignmentCount))
                    {
                        return false;
                    }

                    var assignments = new List<NetworkTurnAssignmentRow>(assignmentCount);
                    for (var assignmentIndex = 0; assignmentIndex < assignmentCount; assignmentIndex++)
                    {
                        var heroId = reader.ReadString();
                        if (string.IsNullOrWhiteSpace(heroId) ||
                            !TryReadOptional(reader, out var cardId))
                        {
                            return false;
                        }

                        assignments.Add(new NetworkTurnAssignmentRow(
                            heroId, cardId, reader.ReadBoolean()));
                    }

                    slots.Add(new NetworkTurnSlotRow(
                        slotRound, slotTurn, Array.AsReadOnly(assignments.ToArray())));
                }

                if (!TryReadCount(reader, out var outcomeCount))
                {
                    return false;
                }

                var outcomes = new List<NetworkEndOfRoundOutcomeRow>(outcomeCount);
                for (var outcomeIndex = 0; outcomeIndex < outcomeCount; outcomeIndex++)
                {
                    var stage = reader.ReadInt32();
                    var kind = reader.ReadInt32();
                    if (!Enum.IsDefined(typeof(EndOfRoundStage), stage) ||
                        !Enum.IsDefined(typeof(EndOfRoundStageOutcomeKind), kind))
                    {
                        return false;
                    }

                    outcomes.Add(new NetworkEndOfRoundOutcomeRow(stage, kind));
                }

                rows.Add(new NetworkCompletedRoundRow(
                    roundIndex,
                    Array.AsReadOnly(slots.ToArray()),
                    Array.AsReadOnly(outcomes.ToArray())));
            }

            rounds = Array.AsReadOnly(rows.ToArray());
            return true;
        }

        private static void WriteInitiativeChoice(BinaryWriter writer, PendingInitiativeChoice choice)
        {
            writer.Write(choice != null);
            if (choice == null)
            {
                return;
            }

            writer.Write((int)choice.OwningSide);
            writer.Write(choice.InitiativeTotal);
            writer.Write(choice.CandidateCardInstanceIds.Count);
            foreach (var candidate in choice.CandidateCardInstanceIds)
            {
                writer.Write(candidate.Value);
            }

            writer.Write(choice.FlipTieBreakerAfterResolution);
        }

        private static bool TryReadInitiativeChoice(
            BinaryReader reader, out NetworkInitiativeChoiceRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var side = reader.ReadInt32();
            var total = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(TeamSide), side) || side == (int)TeamSide.None ||
                !TryReadCount(reader, out var candidateCount) || candidateCount < 2)
            {
                return false;
            }

            var candidates = new List<string>(candidateCount);
            for (var index = 0; index < candidateCount; index++)
            {
                var candidate = reader.ReadString();
                if (string.IsNullOrWhiteSpace(candidate))
                {
                    return false;
                }

                candidates.Add(candidate);
            }

            row = new NetworkInitiativeChoiceRow(
                side, total, Array.AsReadOnly(candidates.ToArray()), reader.ReadBoolean());
            return true;
        }

        private static void WriteMovementResolution(
            BinaryWriter writer, PendingCardActionResolution resolution)
        {
            writer.Write(resolution != null);
            if (resolution == null)
            {
                return;
            }

            writer.Write(resolution.ResolutionId.Value);
            writer.Write(resolution.SourceCardInstanceId.Value);
            writer.Write(resolution.SourceDefinitionReference.DefinitionId.Value);
            writer.Write(resolution.SourceDefinitionReference.ContentRevision);
            writer.Write(resolution.ActionDefinitionId.Value);
            writer.Write(resolution.ActorId.Value);
            writer.Write((int)resolution.InvocationContext);
            writer.Write(resolution.CurrentStepId.Value);
            writer.Write(resolution.MovementSessionState.MovementAllowance);
            writer.Write(resolution.MovementSessionState.RemainingAllowance);
            writer.Write(resolution.MovementSessionState.IsComplete);
            WriteRoute(writer, resolution.MovementSessionState.Route);
        }

        private static bool TryReadMovementResolution(
            BinaryReader reader, out NetworkMovementResolutionRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var resolutionId = reader.ReadString();
            var sourceCard = reader.ReadString();
            var definitionId = reader.ReadString();
            var revision = reader.ReadString();
            var actionId = reader.ReadString();
            var actorId = reader.ReadString();
            var context = reader.ReadInt32();
            var stepId = reader.ReadString();
            var allowance = reader.ReadInt32();
            var remaining = reader.ReadInt32();
            var complete = reader.ReadBoolean();
            if (string.IsNullOrWhiteSpace(resolutionId) || string.IsNullOrWhiteSpace(sourceCard) ||
                string.IsNullOrWhiteSpace(definitionId) || string.IsNullOrWhiteSpace(revision) ||
                string.IsNullOrWhiteSpace(actionId) || string.IsNullOrWhiteSpace(actorId) ||
                !Enum.IsDefined(typeof(ActionInvocationContext), context) ||
                context == (int)ActionInvocationContext.None ||
                string.IsNullOrWhiteSpace(stepId) ||
                allowance < 0 || remaining < 0 || remaining > allowance ||
                !TryReadRoute(reader, out var route) || route.Count == 0)
            {
                return false;
            }

            row = new NetworkMovementResolutionRow(
                resolutionId, sourceCard, definitionId, revision, actionId, actorId, context,
                stepId, allowance, remaining, complete, route);
            return true;
        }

        private static void WriteMovementChoice(BinaryWriter writer, PendingMovementChoice choice)
        {
            writer.Write(choice != null);
            if (choice == null)
            {
                return;
            }

            writer.Write(choice.ResolutionId.Value);
            writer.Write(choice.ChoiceId.Value);
            writer.Write(choice.ChooserId.Value);
            writer.Write(choice.MovingHeroId.Value);
            writer.Write(choice.Origin.Q);
            writer.Write(choice.Origin.R);
            writer.Write(choice.RemainingAllowance);
            writer.Write(choice.CanStop);
            writer.Write(choice.LegalDestinations.Count);
            foreach (var destination in choice.LegalDestinations)
            {
                writer.Write(destination.Destination.Q);
                writer.Write(destination.Destination.R);
                writer.Write(destination.Cost);
                WriteRoute(writer, destination.Route);
            }
        }

        private static bool TryReadMovementChoice(BinaryReader reader, out NetworkMovementChoiceRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var resolutionId = reader.ReadString();
            var choiceId = reader.ReadString();
            var chooserId = reader.ReadString();
            var movingHeroId = reader.ReadString();
            var originQ = reader.ReadInt32();
            var originR = reader.ReadInt32();
            var remaining = reader.ReadInt32();
            var canStop = reader.ReadBoolean();
            if (string.IsNullOrWhiteSpace(resolutionId) || string.IsNullOrWhiteSpace(choiceId) ||
                string.IsNullOrWhiteSpace(chooserId) || string.IsNullOrWhiteSpace(movingHeroId) ||
                remaining < 0 || !TryReadCount(reader, out var destinationCount))
            {
                return false;
            }

            var destinations = new List<NetworkMovementDestinationRow>(destinationCount);
            for (var index = 0; index < destinationCount; index++)
            {
                if (!TryReadDestination(reader, out var destination))
                {
                    return false;
                }

                destinations.Add(destination);
            }

            row = new NetworkMovementChoiceRow(
                resolutionId, choiceId, chooserId, movingHeroId, originQ, originR, remaining,
                canStop, Array.AsReadOnly(destinations.ToArray()));
            return true;
        }

        private static bool TryReadDestination(
            BinaryReader reader, out NetworkMovementDestinationRow destination)
        {
            destination = null;
            var q = reader.ReadInt32();
            var r = reader.ReadInt32();
            var cost = reader.ReadInt32();
            if (cost < 0 || !TryReadRoute(reader, out var route))
            {
                return false;
            }

            destination = new NetworkMovementDestinationRow(q, r, cost, route);
            return true;
        }

        private static void WriteAttackResolution(BinaryWriter writer, PendingAttackResolution resolution)
        {
            writer.Write(resolution != null);
            if (resolution == null)
            {
                return;
            }

            writer.Write(resolution.ResolutionId.Value);
            writer.Write(resolution.SourceCardInstanceId.Value);
            writer.Write(resolution.SourceDefinitionReference.DefinitionId.Value);
            writer.Write(resolution.SourceDefinitionReference.ContentRevision);
            writer.Write(resolution.ActionDefinitionId.Value);
            writer.Write(resolution.ActorId.Value);
            writer.Write((int)resolution.InvocationContext);
            writer.Write(resolution.CurrentStepId.Value);
            writer.Write((int)resolution.Phase);
            WriteAttackTarget(writer, resolution.Target);
            writer.Write(resolution.AttackValue != null);
            if (resolution.AttackValue != null)
            {
                writer.Write(resolution.AttackValue.Total);
            }

            writer.Write(resolution.DefenseOffer.HasValue);
            if (resolution.DefenseOffer.HasValue)
            {
                WriteOffer(writer, resolution.DefenseOffer.Value);
            }

            writer.Write(resolution.HasDefenseTotal);
            if (resolution.HasDefenseTotal)
            {
                writer.Write(resolution.DefenseTotal.Value);
            }

            WriteAttackTarget(writer, resolution.ExcludedTarget);
            writer.Write((int)resolution.BlockResult);
            writer.Write(resolution.TargetSpaceBeforeConsequences.HasValue);
            if (resolution.TargetSpaceBeforeConsequences.HasValue)
            {
                writer.Write(resolution.TargetSpaceBeforeConsequences.Value.Q);
                writer.Write(resolution.TargetSpaceBeforeConsequences.Value.R);
            }

            writer.Write(resolution.ActorSpaceAtActionStart.HasValue);
            if (resolution.ActorSpaceAtActionStart.HasValue)
            {
                writer.Write(resolution.ActorSpaceAtActionStart.Value.Q);
                writer.Write(resolution.ActorSpaceAtActionStart.Value.R);
            }
        }

        private static bool TryReadAttackResolution(
            BinaryReader reader, out NetworkAttackResolutionRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var resolutionId = reader.ReadString();
            var sourceCard = reader.ReadString();
            var definitionId = reader.ReadString();
            var revision = reader.ReadString();
            var actionId = reader.ReadString();
            var actorId = reader.ReadString();
            var context = reader.ReadInt32();
            var stepId = reader.ReadString();
            var phase = reader.ReadInt32();
            if (string.IsNullOrWhiteSpace(resolutionId) || string.IsNullOrWhiteSpace(sourceCard) ||
                string.IsNullOrWhiteSpace(definitionId) || string.IsNullOrWhiteSpace(revision) ||
                string.IsNullOrWhiteSpace(actionId) || string.IsNullOrWhiteSpace(actorId) ||
                !Enum.IsDefined(typeof(ActionInvocationContext), context) ||
                context == (int)ActionInvocationContext.None ||
                string.IsNullOrWhiteSpace(stepId) ||
                !Enum.IsDefined(typeof(AttackResolutionPhase), phase) ||
                phase == (int)AttackResolutionPhase.None ||
                !TryReadAttackTarget(reader, out var target))
            {
                return false;
            }

            var hasAttackTotal = reader.ReadBoolean();
            var attackTotal = hasAttackTotal ? reader.ReadInt32() : 0;
            NetworkActionOfferRow defenseOffer = null;
            if (reader.ReadBoolean() && !TryReadOffer(reader, out defenseOffer))
            {
                return false;
            }

            var hasDefenseTotal = reader.ReadBoolean();
            var defenseTotal = hasDefenseTotal ? reader.ReadInt32() : 0;
            if (!TryReadAttackTarget(reader, out var excluded))
            {
                return false;
            }

            var blockResult = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(DefenseBlockResult), blockResult))
            {
                return false;
            }

            NetworkCoordinateRow? targetSpace = null;
            if (reader.ReadBoolean())
            {
                targetSpace = new NetworkCoordinateRow(reader.ReadInt32(), reader.ReadInt32());
            }

            NetworkCoordinateRow? actorSpace = null;
            if (reader.ReadBoolean())
            {
                actorSpace = new NetworkCoordinateRow(reader.ReadInt32(), reader.ReadInt32());
            }

            row = new NetworkAttackResolutionRow(
                resolutionId, sourceCard, definitionId, revision, actionId, actorId, context,
                stepId, phase, target, hasAttackTotal, attackTotal, defenseOffer, hasDefenseTotal,
                defenseTotal, excluded, blockResult, targetSpace, actorSpace);
            return true;
        }

        private static void WriteAttackTargetChoice(
            BinaryWriter writer, PendingAttackTargetChoice choice)
        {
            writer.Write(choice != null);
            if (choice == null)
            {
                return;
            }

            writer.Write(choice.ResolutionId.Value);
            writer.Write(choice.ChoiceId.Value);
            writer.Write(choice.ChooserId.Value);
            writer.Write(choice.LegalTargets.Count);
            foreach (var target in choice.LegalTargets)
            {
                WriteAttackTarget(writer, target);
            }
        }

        private static bool TryReadAttackTargetChoice(
            BinaryReader reader, out NetworkAttackTargetChoiceRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var resolutionId = reader.ReadString();
            var choiceId = reader.ReadString();
            var chooserId = reader.ReadString();
            if (string.IsNullOrWhiteSpace(resolutionId) || string.IsNullOrWhiteSpace(choiceId) ||
                string.IsNullOrWhiteSpace(chooserId) || !TryReadCount(reader, out var targetCount))
            {
                return false;
            }

            var targets = new List<NetworkAttackTargetRow>(targetCount);
            for (var index = 0; index < targetCount; index++)
            {
                if (!TryReadAttackTarget(reader, out var target) || target == null)
                {
                    return false;
                }

                targets.Add(target);
            }

            row = new NetworkAttackTargetChoiceRow(
                resolutionId, choiceId, chooserId, Array.AsReadOnly(targets.ToArray()));
            return true;
        }

        private static void WriteDefenseChoice(BinaryWriter writer, PendingDefenseResponseChoice choice)
        {
            writer.Write(choice != null);
            if (choice == null)
            {
                return;
            }

            writer.Write(choice.ResolutionId.Value);
            writer.Write(choice.ChoiceId.Value);
            writer.Write(choice.ChooserId.Value);
            writer.Write(choice.AttackerId.Value);
            // Milestone 8.80, wire `guards-private-online/21`. The attacking card, its definition
            // reference as id + revision, and the action off it - the same three fields, in the same
            // order, `WriteAttackResolution` writes for the frame the defender never receives.
            writer.Write(choice.SourceCardInstanceId.Value);
            writer.Write(choice.SourceDefinitionReference.DefinitionId.Value);
            writer.Write(choice.SourceDefinitionReference.ContentRevision);
            writer.Write(choice.ActionDefinitionId.Value);
            writer.Write(choice.AttackTotal);
            writer.Write(choice.CanDecline);
            writer.Write(choice.LegalDefenseOffers.Count);
            foreach (var offer in choice.LegalDefenseOffers)
            {
                WriteOffer(writer, offer);
            }
        }

        private static bool TryReadDefenseChoice(BinaryReader reader, out NetworkDefenseChoiceRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var resolutionId = reader.ReadString();
            var choiceId = reader.ReadString();
            var chooserId = reader.ReadString();
            var attackerId = reader.ReadString();
            var sourceCardInstanceId = reader.ReadString();
            var sourceDefinitionId = reader.ReadString();
            var sourceContentRevision = reader.ReadString();
            var actionDefinitionId = reader.ReadString();
            var attackTotal = reader.ReadInt32();
            var canDecline = reader.ReadBoolean();
            if (string.IsNullOrWhiteSpace(resolutionId) || string.IsNullOrWhiteSpace(choiceId) ||
                string.IsNullOrWhiteSpace(chooserId) || string.IsNullOrWhiteSpace(attackerId) ||
                string.IsNullOrWhiteSpace(sourceCardInstanceId) ||
                string.IsNullOrWhiteSpace(sourceDefinitionId) ||
                string.IsNullOrWhiteSpace(sourceContentRevision) ||
                string.IsNullOrWhiteSpace(actionDefinitionId) ||
                attackTotal < 0 || !TryReadCount(reader, out var offerCount))
            {
                return false;
            }

            var offers = new List<NetworkActionOfferRow>(offerCount);
            for (var index = 0; index < offerCount; index++)
            {
                if (!TryReadOffer(reader, out var offer))
                {
                    return false;
                }

                offers.Add(offer);
            }

            row = new NetworkDefenseChoiceRow(
                resolutionId, choiceId, chooserId, attackerId,
                sourceCardInstanceId, sourceDefinitionId, sourceContentRevision, actionDefinitionId,
                attackTotal, canDecline,
                Array.AsReadOnly(offers.ToArray()));
            return true;
        }

        private static void WriteDefensePreview(
            BinaryWriter writer, PendingDefensePreviewProjection preview)
        {
            writer.Write(preview != null);
            if (preview == null)
            {
                return;
            }

            writer.Write(preview.ResolutionId.Value);
            writer.Write(preview.ChoiceId.Value);
            writer.Write(preview.AttackTotal);
            WriteContributions(writer, preview.AttackContributions);
            writer.Write(preview.Choices.Count);
            foreach (var choice in preview.Choices)
            {
                WriteOptional(writer, choice.CardInstanceId.HasValue,
                    choice.CardInstanceId.HasValue ? choice.CardInstanceId.Value.Value : null);
                writer.Write(choice.ChoiceLabel);
                WriteContributions(writer, choice.DefenseContributions);
                writer.Write(choice.DefenseTotal.HasValue);
                if (choice.DefenseTotal.HasValue)
                {
                    writer.Write(choice.DefenseTotal.Value);
                }

                writer.Write((int)choice.BlockResult);
                writer.Write((int)choice.Outcome);
                writer.Write(choice.SuppressesMinionContributions);
            }

            WriteContributions(writer, preview.MinionContributions);
        }

        private static bool TryReadDefensePreview(BinaryReader reader, out NetworkDefensePreviewRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var resolutionId = reader.ReadString();
            var choiceId = reader.ReadString();
            var attackTotal = reader.ReadInt32();
            if (string.IsNullOrWhiteSpace(resolutionId) || string.IsNullOrWhiteSpace(choiceId) ||
                !TryReadContributions(reader, out var attackContributions) ||
                !TryReadCount(reader, out var choiceCount) || choiceCount == 0)
            {
                return false;
            }

            var choices = new List<NetworkDefenseChoicePreviewRow>(choiceCount);
            for (var index = 0; index < choiceCount; index++)
            {
                if (!TryReadOptional(reader, out var cardId))
                {
                    return false;
                }

                var label = reader.ReadString();
                if (string.IsNullOrWhiteSpace(label) ||
                    !TryReadContributions(reader, out var contributions))
                {
                    return false;
                }

                var hasTotal = reader.ReadBoolean();
                var total = hasTotal ? reader.ReadInt32() : 0;
                var blockResult = reader.ReadInt32();
                var outcome = reader.ReadInt32();
                if (!Enum.IsDefined(typeof(DefenseBlockResult), blockResult) ||
                    !Enum.IsDefined(typeof(AttackResolutionOutcome), outcome))
                {
                    return false;
                }

                choices.Add(new NetworkDefenseChoicePreviewRow(
                    cardId, label, contributions, hasTotal, total, blockResult, outcome,
                    reader.ReadBoolean()));
            }

            if (!TryReadContributions(reader, out var minionContributions))
            {
                return false;
            }

            row = new NetworkDefensePreviewRow(
                resolutionId, choiceId, attackTotal, attackContributions,
                Array.AsReadOnly(choices.ToArray()), minionContributions);
            return true;
        }

        private static void WriteSkillResolution(BinaryWriter writer, PendingResolution resolution)
        {
            writer.Write(resolution != null);
            if (resolution == null)
            {
                return;
            }

            writer.Write(resolution.ResolutionId.Value);
            writer.Write(resolution.SourceCardInstanceId.Value);
            WriteOptional(writer, resolution.SourceDefinitionReference.IsDefined,
                resolution.SourceDefinitionReference.IsDefined
                    ? resolution.SourceDefinitionReference.DefinitionId.Value
                    : null);
            if (resolution.SourceDefinitionReference.IsDefined)
            {
                writer.Write(resolution.SourceDefinitionReference.ContentRevision);
            }

            writer.Write(resolution.ActionDefinitionId.Value);
            writer.Write(resolution.EffectDefinitionReference.DefinitionId.Value);
            writer.Write(resolution.EffectDefinitionReference.ContentRevision);
            writer.Write(resolution.ActorId.Value);
            writer.Write(resolution.ControllerId.Value);
            writer.Write((int)resolution.InvocationContext);
            writer.Write(resolution.PendingChoice != null);
        }

        private static bool TryReadSkillResolution(BinaryReader reader, out NetworkSkillResolutionRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var resolutionId = reader.ReadString();
            var sourceCard = reader.ReadString();
            if (string.IsNullOrWhiteSpace(resolutionId) || string.IsNullOrWhiteSpace(sourceCard) ||
                !TryReadOptional(reader, out var definitionId))
            {
                return false;
            }

            var revision = definitionId != null ? reader.ReadString() : null;
            if (definitionId != null && string.IsNullOrWhiteSpace(revision))
            {
                return false;
            }

            var actionId = reader.ReadString();
            var effectId = reader.ReadString();
            var effectRevision = reader.ReadString();
            var actorId = reader.ReadString();
            var controllerId = reader.ReadString();
            var context = reader.ReadInt32();
            if (string.IsNullOrWhiteSpace(actionId) || string.IsNullOrWhiteSpace(effectId) ||
                string.IsNullOrWhiteSpace(effectRevision) || string.IsNullOrWhiteSpace(actorId) ||
                string.IsNullOrWhiteSpace(controllerId) ||
                !Enum.IsDefined(typeof(ActionInvocationContext), context) ||
                context == (int)ActionInvocationContext.None)
            {
                return false;
            }

            row = new NetworkSkillResolutionRow(
                resolutionId, sourceCard, definitionId, revision, actionId, effectId,
                effectRevision, actorId, controllerId, context, reader.ReadBoolean());
            return true;
        }

        private static void WriteEffectChoice(BinaryWriter writer, PendingEffectChoice choice)
        {
            writer.Write(choice != null);
            if (choice == null)
            {
                return;
            }

            writer.Write(choice.ResolutionId.Value);
            writer.Write(choice.ChoiceId.Value);
            writer.Write(choice.NodeId.Value);
            writer.Write(choice.ChooserId.Value);
            writer.Write((int)choice.Kind);
            writer.Write(choice.MinimumSelections);
            writer.Write(choice.MaximumSelections);
            WriteCandidates(writer, choice.Candidates);
            WriteCandidates(writer, choice.SelectedCandidates);
            WriteCandidates(writer, choice.UnavailableCandidates);
            writer.Write(choice.ConfirmInvocation != null);
            writer.Write(choice.DeclineInvocation != null);
            writer.Write(choice.StopInvocation != null);
            writer.Write((int)choice.ValueVocabulary);
            // Milestone 9.11 (wire /25): the node's own flag, not the Decline offer it helps derive.
            writer.Write(choice.DeclinableRegardlessOfMinimum);
        }

        private static bool TryReadEffectChoice(BinaryReader reader, out NetworkEffectChoiceRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var resolutionId = reader.ReadString();
            var choiceId = reader.ReadString();
            var nodeId = reader.ReadString();
            var chooserId = reader.ReadString();
            var kind = reader.ReadInt32();
            var minimum = reader.ReadInt32();
            var maximum = reader.ReadInt32();
            if (string.IsNullOrWhiteSpace(resolutionId) || string.IsNullOrWhiteSpace(choiceId) ||
                string.IsNullOrWhiteSpace(nodeId) || string.IsNullOrWhiteSpace(chooserId) ||
                !Enum.IsDefined(typeof(EffectChoiceKind), kind) ||
                kind == (int)EffectChoiceKind.None ||
                minimum < 0 || maximum < minimum ||
                !TryReadCandidates(reader, out var candidates) ||
                !TryReadCandidates(reader, out var selected) ||
                !TryReadCandidates(reader, out var unavailable))
            {
                return false;
            }

            var hasConfirm = reader.ReadBoolean();
            var hasDecline = reader.ReadBoolean();
            var hasStop = reader.ReadBoolean();
            var vocabulary = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(EffectChoiceValueVocabulary), vocabulary))
            {
                return false;
            }

            var declinableRegardlessOfMinimum = reader.ReadBoolean();
            row = new NetworkEffectChoiceRow(
                resolutionId, choiceId, nodeId, chooserId, kind, minimum, maximum,
                candidates, selected, unavailable,
                hasConfirm, hasDecline, hasStop, vocabulary, declinableRegardlessOfMinimum);
            return true;
        }

        private static void WriteCandidates(
            BinaryWriter writer, IReadOnlyList<EffectChoiceCandidate> candidates)
        {
            writer.Write(candidates.Count);
            foreach (var candidate in candidates)
            {
                writer.Write((int)candidate.Kind);
                if (candidate.Kind == EffectChoiceCandidateKind.Option)
                {
                    writer.Write(candidate.OptionNodeId.Value);
                }
                else
                {
                    WriteEffectValue(writer, candidate.Value);
                }
            }
        }

        private static bool TryReadCandidates(
            BinaryReader reader, out IReadOnlyList<NetworkEffectCandidateRow> candidates)
        {
            candidates = null;
            if (!TryReadCount(reader, out var count))
            {
                return false;
            }

            var rows = new List<NetworkEffectCandidateRow>(count);
            for (var index = 0; index < count; index++)
            {
                var kind = reader.ReadInt32();
                if (kind == (int)EffectChoiceCandidateKind.Option)
                {
                    var optionId = reader.ReadString();
                    if (string.IsNullOrWhiteSpace(optionId))
                    {
                        return false;
                    }

                    rows.Add(new NetworkEffectCandidateRow(kind, optionId, null));
                }
                else if (kind == (int)EffectChoiceCandidateKind.Value)
                {
                    if (!TryReadEffectValue(reader, out var value))
                    {
                        return false;
                    }

                    rows.Add(new NetworkEffectCandidateRow(kind, null, value));
                }
                else
                {
                    return false;
                }
            }

            candidates = Array.AsReadOnly(rows.ToArray());
            return true;
        }

        private static void WriteEffectValue(BinaryWriter writer, Core.Effects.EffectValue value)
        {
            writer.Write((int)value.Kind);
            writer.Write(value.BooleanValue);
            writer.Write(value.IntegerValue);
            WriteOptional(writer, value.HeroId.IsDefined, value.HeroId.Value);
            WriteOptional(writer, value.MinionId.IsDefined, value.MinionId.Value);
            WriteOptional(writer, value.CardInstanceId.IsDefined, value.CardInstanceId.Value);
            writer.Write(value.Coordinate.Q);
            writer.Write(value.Coordinate.R);
            WriteOptional(writer, value.NodeId.IsDefined, value.NodeId.Value);
        }

        private static bool TryReadEffectValue(BinaryReader reader, out NetworkEffectValueRow row)
        {
            row = null;
            var kind = reader.ReadInt32();
            var booleanValue = reader.ReadBoolean();
            var integerValue = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(Core.Effects.EffectValueKind), kind) ||
                !TryReadOptional(reader, out var heroId) ||
                !TryReadOptional(reader, out var minionId) ||
                !TryReadOptional(reader, out var cardId))
            {
                return false;
            }

            var q = reader.ReadInt32();
            var r = reader.ReadInt32();
            if (!TryReadOptional(reader, out var nodeId))
            {
                return false;
            }

            row = new NetworkEffectValueRow(
                kind, booleanValue, integerValue, heroId, minionId, cardId, q, r, nodeId);
            return true;
        }

        private static void WriteCorrection(
            BinaryWriter writer, PendingDisplacedMinionCorrection correction)
        {
            writer.Write(correction != null);
            if (correction == null)
            {
                return;
            }

            writer.Write(correction.ResolutionId.Value);
            writer.Write(correction.SourceCardInstanceId.Value);
            writer.Write(correction.ActionDefinitionId.Value);
            writer.Write(correction.ActorId.Value);
            writer.Write((int)correction.Stage);
            writer.Write(correction.ChoosingTeamId.Value);
            writer.Write((int)correction.ChoosingSide);
            WriteOptional(writer, !string.IsNullOrWhiteSpace(correction.BattleZoneId.Value),
                correction.BattleZoneId.Value);
            writer.Write(correction.ChoiceId.Value);
            WriteOptional(writer, correction.SubjectMinionId.IsDefined, correction.SubjectMinionId.Value);
            writer.Write((int)correction.Kind);
            writer.Write(correction.SubjectCandidates.Count);
            foreach (var subject in correction.SubjectCandidates)
            {
                writer.Write(subject.MinionId.Value);
                writer.Write(subject.Coordinate.Q);
                writer.Write(subject.Coordinate.R);
            }

            writer.Write(correction.DestinationCandidates.Count);
            foreach (var destination in correction.DestinationCandidates)
            {
                writer.Write(destination.Destination.Q);
                writer.Write(destination.Destination.R);
                writer.Write(destination.Cost);
                WriteRoute(writer, destination.Route);
            }
        }

        private static bool TryReadCorrection(BinaryReader reader, out NetworkCorrectionRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var resolutionId = reader.ReadString();
            var sourceCard = reader.ReadString();
            var actionId = reader.ReadString();
            var actorId = reader.ReadString();
            var stage = reader.ReadInt32();
            var teamId = reader.ReadString();
            var side = reader.ReadInt32();
            if (string.IsNullOrWhiteSpace(resolutionId) || string.IsNullOrWhiteSpace(sourceCard) ||
                string.IsNullOrWhiteSpace(actionId) || string.IsNullOrWhiteSpace(actorId) ||
                !Enum.IsDefined(typeof(DisplacedMinionCorrectionStage), stage) ||
                stage == (int)DisplacedMinionCorrectionStage.None ||
                string.IsNullOrWhiteSpace(teamId) ||
                !Enum.IsDefined(typeof(TeamSide), side) || side == (int)TeamSide.None ||
                !TryReadOptional(reader, out var battleZoneId))
            {
                return false;
            }

            var choiceId = reader.ReadString();
            if (string.IsNullOrWhiteSpace(choiceId) ||
                !TryReadOptional(reader, out var subjectMinionId))
            {
                return false;
            }

            var kind = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(DisplacedMinionCorrectionKind), kind) ||
                !TryReadCount(reader, out var subjectCount))
            {
                return false;
            }

            var subjects = new List<NetworkCorrectionSubjectRow>(subjectCount);
            for (var index = 0; index < subjectCount; index++)
            {
                var minionId = reader.ReadString();
                if (string.IsNullOrWhiteSpace(minionId))
                {
                    return false;
                }

                subjects.Add(new NetworkCorrectionSubjectRow(
                    minionId, reader.ReadInt32(), reader.ReadInt32()));
            }

            if (!TryReadCount(reader, out var destinationCount))
            {
                return false;
            }

            var destinations = new List<NetworkMovementDestinationRow>(destinationCount);
            for (var index = 0; index < destinationCount; index++)
            {
                if (!TryReadDestination(reader, out var destination))
                {
                    return false;
                }

                destinations.Add(destination);
            }

            row = new NetworkCorrectionRow(
                resolutionId, sourceCard, actionId, actorId, stage, teamId, side, battleZoneId,
                choiceId, subjectMinionId, kind,
                Array.AsReadOnly(subjects.ToArray()),
                Array.AsReadOnly(destinations.ToArray()));
            return true;
        }

        private static void WriteMinionBattleChoice(
            BinaryWriter writer, PendingMinionBattleChoice choice)
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
                WriteUnit(writer, candidate.Unit);
                writer.Write((int)candidate.MinionType);
            }

            writer.Write(choice.SelectedUnits.Count);
            foreach (var unit in choice.SelectedUnits)
            {
                WriteUnit(writer, unit);
            }

            writer.Write(choice.CanConfirm);
        }

        private static bool TryReadMinionBattleChoice(
            BinaryReader reader, out NetworkMinionBattleChoiceRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var resolutionId = reader.ReadString();
            var choiceId = reader.ReadString();
            var losingTeamId = reader.ReadString();
            var side = reader.ReadInt32();
            var battleZoneId = reader.ReadString();
            var required = reader.ReadInt32();
            if (string.IsNullOrWhiteSpace(resolutionId) || string.IsNullOrWhiteSpace(choiceId) ||
                string.IsNullOrWhiteSpace(losingTeamId) ||
                !Enum.IsDefined(typeof(TeamSide), side) || side == (int)TeamSide.None ||
                string.IsNullOrWhiteSpace(battleZoneId) || required < 1 ||
                !TryReadCount(reader, out var candidateCount))
            {
                return false;
            }

            var candidates = new List<NetworkMinionBattleCandidateRow>(candidateCount);
            for (var index = 0; index < candidateCount; index++)
            {
                if (!TryReadUnit(reader, out var unit))
                {
                    return false;
                }

                var minionType = reader.ReadInt32();
                if (!Enum.IsDefined(typeof(StandardMinionType), minionType))
                {
                    return false;
                }

                candidates.Add(new NetworkMinionBattleCandidateRow(unit, minionType));
            }

            if (!TryReadUnits(reader, out var selected))
            {
                return false;
            }

            row = new NetworkMinionBattleChoiceRow(
                resolutionId, choiceId, losingTeamId, side, battleZoneId, required,
                Array.AsReadOnly(candidates.ToArray()), selected, reader.ReadBoolean());
            return true;
        }

        private static void WriteHeroRespawnChoice(BinaryWriter writer, PendingHeroRespawnChoice choice)
        {
            writer.Write(choice != null);
            if (choice == null)
            {
                return;
            }

            writer.Write(choice.HeroId.Value);
            writer.Write(choice.CardInstanceId.Value);
            writer.Write(choice.Destinations.Count);
            foreach (var destination in choice.Destinations)
            {
                writer.Write(destination.Coordinate.Q);
                writer.Write(destination.Coordinate.R);
                WriteOptional(writer, !string.IsNullOrWhiteSpace(destination.AdjacentSpawnPointId.Value),
                    destination.AdjacentSpawnPointId.Value);
                writer.Write(destination.IsFallback);
            }
        }

        private static bool TryReadHeroRespawnChoice(
            BinaryReader reader, out NetworkHeroRespawnChoiceRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var heroId = reader.ReadString();
            var cardId = reader.ReadString();
            if (string.IsNullOrWhiteSpace(heroId) || string.IsNullOrWhiteSpace(cardId) ||
                !TryReadCount(reader, out var destinationCount))
            {
                return false;
            }

            var destinations = new List<NetworkRespawnDestinationRow>(destinationCount);
            for (var index = 0; index < destinationCount; index++)
            {
                var q = reader.ReadInt32();
                var r = reader.ReadInt32();
                if (!TryReadOptional(reader, out var spawnId))
                {
                    return false;
                }

                destinations.Add(new NetworkRespawnDestinationRow(q, r, spawnId, reader.ReadBoolean()));
            }

            row = new NetworkHeroRespawnChoiceRow(
                heroId, cardId, Array.AsReadOnly(destinations.ToArray()));
            return true;
        }

        private static void WriteSystemTransitionStep(
            BinaryWriter writer, PendingSystemTransitionStep step)
        {
            writer.Write(step != null);
            if (step == null)
            {
                return;
            }

            writer.Write(step.InstanceId.Value);
            writer.Write(step.DefinitionId.Value);
            writer.Write(step.StepId.Value);
            WriteOptional(writer, step.ItemId.IsDefined, step.ItemId.Value);
            writer.Write((int)step.ChoosingSide);
            WriteStringList(writer, step.Options.Count, index => step.Options[index].Value);
            WriteStringList(writer, step.CompletedStepIds.Count,
                index => step.CompletedStepIds[index].Value);
            WriteStringList(writer, step.CompletedItemIds.Count,
                index => step.CompletedItemIds[index].Value);
        }

        private static bool TryReadSystemTransitionStep(
            BinaryReader reader, out NetworkSystemTransitionStepRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var instanceId = reader.ReadString();
            var definitionId = reader.ReadString();
            var stepId = reader.ReadString();
            if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(definitionId) ||
                string.IsNullOrWhiteSpace(stepId) || !TryReadOptional(reader, out var itemId))
            {
                return false;
            }

            var side = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(TeamSide), side) || side == (int)TeamSide.None ||
                !TryReadStringList(reader, out var options) || options.Count == 0 ||
                !TryReadStringList(reader, out var completedSteps) ||
                !TryReadStringList(reader, out var completedItems))
            {
                return false;
            }

            row = new NetworkSystemTransitionStepRow(
                instanceId, definitionId, stepId, itemId, side, options, completedSteps,
                completedItems);
            return true;
        }

        private static void WriteSystemTransitionDescription(
            BinaryWriter writer, SystemTransitionStepDescription description)
        {
            writer.Write(description != null);
            if (description == null)
            {
                return;
            }

            writer.Write((int)description.Kind);
            WriteOptional(writer, description.HeroId.IsDefined, description.HeroId.Value);
            writer.Write(description.Level);
            writer.Write(description.PaidCoins);
            writer.Write(description.RemainingCoins);
            writer.Write(description.Options.Count);
            foreach (var option in description.Options)
            {
                writer.Write(option.OptionId.Value);
                writer.Write((int)option.Kind);
                WriteOptional(writer, option.UpgradedCardInstanceId.IsDefined,
                    option.UpgradedCardInstanceId.Value);
                WriteCardReference(writer, option.UpgradedCard);
                WriteOptional(writer, option.TakenCardInstanceId.IsDefined,
                    option.TakenCardInstanceId.Value);
                WriteCardReference(writer, option.TakenCard);
                WriteOptional(writer, option.SetAsideCardInstanceId.IsDefined,
                    option.SetAsideCardInstanceId.Value);
                WriteCardReference(writer, option.SetAsideCard);
                writer.Write((int)option.ItemIcon);
                writer.Write(option.ItemSlot.Index);
                writer.Write(option.Destination.Q);
                writer.Write(option.Destination.R);
                WriteOptional(writer, !string.IsNullOrWhiteSpace(option.DestinationSpaceId.Value),
                    option.DestinationSpaceId.Value);
            }

            // Milestone 9.11 (wire /25): the item this description names, or none.
            WriteOptional(writer, description.ItemId.IsDefined, description.ItemId.Value);
        }

        private static bool TryReadSystemTransitionDescription(
            BinaryReader reader, out NetworkSystemTransitionDescriptionRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var kind = reader.ReadInt32();
            if (!Enum.IsDefined(typeof(SystemTransitionPromptKind), kind) ||
                kind == (int)SystemTransitionPromptKind.None ||
                !TryReadOptional(reader, out var heroId))
            {
                return false;
            }

            var level = reader.ReadInt32();
            var paidCoins = reader.ReadInt32();
            var remainingCoins = reader.ReadInt32();
            if (level < 0 || paidCoins < 0 || remainingCoins < 0 ||
                !TryReadCount(reader, out var optionCount))
            {
                return false;
            }

            var options = new List<NetworkSystemTransitionOptionRow>(optionCount);
            for (var index = 0; index < optionCount; index++)
            {
                var optionId = reader.ReadString();
                var optionKind = reader.ReadInt32();
                if (string.IsNullOrWhiteSpace(optionId) ||
                    !Enum.IsDefined(typeof(SystemTransitionPromptKind), optionKind) ||
                    optionKind == (int)SystemTransitionPromptKind.None ||
                    !TryReadOptional(reader, out var upgradedCard) ||
                    !TryReadCardReference(reader, out var upgradedDefinition) ||
                    !TryReadOptional(reader, out var takenCard) ||
                    !TryReadCardReference(reader, out var takenDefinition) ||
                    !TryReadOptional(reader, out var setAsideCard) ||
                    !TryReadCardReference(reader, out var setAsideDefinition))
                {
                    return false;
                }

                var itemIcon = reader.ReadInt32();
                var itemSlot = reader.ReadInt32();
                if (!Enum.IsDefined(typeof(PrintedValueChannel), itemIcon) || itemSlot < 0)
                {
                    return false;
                }

                var destinationQ = reader.ReadInt32();
                var destinationR = reader.ReadInt32();
                if (!TryReadOptional(reader, out var destinationSpaceId))
                {
                    return false;
                }

                options.Add(new NetworkSystemTransitionOptionRow(
                    optionId, optionKind,
                    upgradedCard, upgradedDefinition.definitionId, upgradedDefinition.revision,
                    takenCard, takenDefinition.definitionId, takenDefinition.revision,
                    setAsideCard, setAsideDefinition.definitionId, setAsideDefinition.revision,
                    itemIcon, itemSlot, destinationQ, destinationR, destinationSpaceId));
            }

            if (!TryReadOptional(reader, out var itemId))
            {
                return false;
            }

            row = new NetworkSystemTransitionDescriptionRow(
                kind, heroId, level, paidCoins, remainingCoins,
                Array.AsReadOnly(options.ToArray()), itemId);
            return true;
        }

        private static void WriteRemovalPrevention(BinaryWriter writer, RemovalPreventionPrompt prompt)
        {
            writer.Write(prompt != null);
            if (prompt == null)
            {
                return;
            }

            writer.Write(prompt.RequestId);
            writer.Write(prompt.DecidingHeroId.Value);
            writer.Write(prompt.SubjectMinionId.Value);
            writer.Write((int)prompt.Origin);
            writer.Write(prompt.Payments.Count);
            foreach (var payment in prompt.Payments)
            {
                writer.Write(payment.SourceId);
                writer.Write((int)payment.Cost.Kind);
                writer.Write((int)payment.Cost.FromLocation);
                writer.Write(payment.Cost.AdmittedColors.Count);
                foreach (var color in payment.Cost.AdmittedColors)
                {
                    writer.Write((int)color);
                }

                writer.Write((int)payment.Cost.AdmittedClassification);
                writer.Write(payment.PaymentCardInstanceId.Value);
            }
        }

        /// <summary>
        /// Milestone 8.35g. The finished turn awaiting its own player. Written for every seat,
        /// because every seat is waiting on it.
        /// </summary>
        private static void WriteTurnRelease(
            BinaryWriter writer, GuardsDigital.Core.Cards.PendingTurnRelease release)
        {
            writer.Write(release != null);
            if (release == null)
            {
                return;
            }

            writer.Write(release.ActorId.Value);
            writer.Write(release.CardInstanceId.Value);
            writer.Write((int)release.TeamSide);
        }

        private static bool TryReadTurnRelease(
            BinaryReader reader, out NetworkTurnReleaseRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var actorId = reader.ReadString();
            var cardInstanceId = reader.ReadString();
            var teamSide = reader.ReadInt32();
            if (string.IsNullOrWhiteSpace(actorId) || string.IsNullOrWhiteSpace(cardInstanceId))
            {
                return false;
            }

            row = new NetworkTurnReleaseRow(actorId, cardInstanceId, teamSide);
            return true;
        }

        private static bool TryReadRemovalPrevention(
            BinaryReader reader, out NetworkRemovalPreventionRow row)
        {
            row = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var requestId = reader.ReadString();
            var decidingHeroId = reader.ReadString();
            var subjectMinionId = reader.ReadString();
            var origin = reader.ReadInt32();
            if (string.IsNullOrWhiteSpace(requestId) || string.IsNullOrWhiteSpace(decidingHeroId) ||
                string.IsNullOrWhiteSpace(subjectMinionId) ||
                !Enum.IsDefined(typeof(RemovalPreventionOrigin), origin) ||
                !TryReadCount(reader, out var paymentCount))
            {
                return false;
            }

            var payments = new List<NetworkRemovalPaymentRow>(paymentCount);
            for (var index = 0; index < paymentCount; index++)
            {
                var sourceId = reader.ReadString();
                var costKind = reader.ReadInt32();
                var fromLocation = reader.ReadInt32();
                if (string.IsNullOrWhiteSpace(sourceId) ||
                    !Enum.IsDefined(typeof(RemovalPreventionCostKind), costKind) ||
                    !Enum.IsDefined(typeof(CardLocation), fromLocation) ||
                    !TryReadCount(reader, out var colorCount))
                {
                    return false;
                }

                var colors = new List<int>(colorCount);
                for (var colorIndex = 0; colorIndex < colorCount; colorIndex++)
                {
                    var color = reader.ReadInt32();
                    if (!Enum.IsDefined(typeof(CardColor), color))
                    {
                        return false;
                    }

                    colors.Add(color);
                }

                var classification = reader.ReadInt32();
                var paymentCard = reader.ReadString();
                if (!Enum.IsDefined(typeof(CardClassification), classification) ||
                    string.IsNullOrWhiteSpace(paymentCard))
                {
                    return false;
                }

                payments.Add(new NetworkRemovalPaymentRow(
                    sourceId, costKind, fromLocation,
                    Array.AsReadOnly(colors.ToArray()), classification, paymentCard));
            }

            row = new NetworkRemovalPreventionRow(
                requestId, decidingHeroId, subjectMinionId, origin,
                Array.AsReadOnly(payments.ToArray()));
            return true;
        }

        private static void WriteOffer(BinaryWriter writer, ActionOffer offer)
        {
            writer.Write(offer.SourceCardInstanceId.Value);
            writer.Write(offer.SourceDefinitionReference.DefinitionId.Value);
            writer.Write(offer.SourceDefinitionReference.ContentRevision);
            writer.Write(offer.ActionDefinitionId.Value);
            writer.Write(offer.ActorId.Value);
            writer.Write((int)offer.InvocationContext);
            writer.Write(offer.IsAvailable);
            writer.Write((int)offer.AttackRange);
            writer.Write(offer.SourceCardStateTransition.IsDefined);
            if (offer.SourceCardStateTransition.IsDefined)
            {
                writer.Write((int)offer.SourceCardStateTransition.RequiredLocation);
                writer.Write((int)offer.SourceCardStateTransition.DestinationLocation);
            }

            // Milestone 8.35l. Written only where there is a reason, so an ordinary greyed control
            // costs one bool as it always did - and an available offer can never carry one, because
            // the offer itself drops the reason when availability is stamped true.
            writer.Write(offer.Unavailability.IsStated);
            if (offer.Unavailability.IsStated)
            {
                writer.Write((int)offer.Unavailability.Kind);
                writer.Write((int)offer.Unavailability.BlockedActivity);
                writer.Write(offer.Unavailability.BlockingHeroId.Value ?? string.Empty);
                writer.Write(offer.Unavailability.BlockingCardInstanceId.Value ?? string.Empty);
                writer.Write(offer.Unavailability.BlockingActiveEffectId.Value ?? string.Empty);
            }
        }

        private static bool TryReadOffer(BinaryReader reader, out NetworkActionOfferRow row)
        {
            row = null;
            var sourceCard = reader.ReadString();
            var definitionId = reader.ReadString();
            var revision = reader.ReadString();
            var actionId = reader.ReadString();
            var actorId = reader.ReadString();
            var context = reader.ReadInt32();
            var isAvailable = reader.ReadBoolean();
            var attackRange = reader.ReadInt32();
            if (string.IsNullOrWhiteSpace(sourceCard) || string.IsNullOrWhiteSpace(definitionId) ||
                string.IsNullOrWhiteSpace(revision) || string.IsNullOrWhiteSpace(actionId) ||
                string.IsNullOrWhiteSpace(actorId) ||
                !Enum.IsDefined(typeof(ActionInvocationContext), context) ||
                context == (int)ActionInvocationContext.None ||
                !Enum.IsDefined(typeof(AttackRangeKind), attackRange))
            {
                return false;
            }

            var hasTransition = reader.ReadBoolean();
            var required = 0;
            var destination = 0;
            if (hasTransition)
            {
                required = reader.ReadInt32();
                destination = reader.ReadInt32();
                if (!Enum.IsDefined(typeof(CardLocation), required) ||
                    required == (int)CardLocation.None ||
                    !Enum.IsDefined(typeof(CardLocation), destination) ||
                    destination == (int)CardLocation.None)
                {
                    return false;
                }
            }

            var unavailabilityKind = 0;
            var blockedActivity = 0;
            var blockingHero = string.Empty;
            var blockingCard = string.Empty;
            var blockingEffect = string.Empty;
            if (reader.ReadBoolean())
            {
                unavailabilityKind = reader.ReadInt32();
                blockedActivity = reader.ReadInt32();
                blockingHero = reader.ReadString();
                blockingCard = reader.ReadString();
                blockingEffect = reader.ReadString();

                // A stated reason has to be one this build's vocabulary names and has to belong to an
                // offer that is actually unavailable. A payload claiming otherwise is refused whole
                // rather than decoded into an offer that says "you may take this, and here is why you
                // may not" - the envelope discipline milestone 8.32 set for every other field.
                if (isAvailable ||
                    !Enum.IsDefined(typeof(ActionUnavailabilityKind), unavailabilityKind) ||
                    unavailabilityKind == (int)ActionUnavailabilityKind.None ||
                    !Enum.IsDefined(typeof(RuleActivity), blockedActivity))
                {
                    return false;
                }
            }

            row = new NetworkActionOfferRow(
                sourceCard, definitionId, revision, actionId, actorId, context, isAvailable,
                attackRange, hasTransition, required, destination,
                unavailabilityKind, blockedActivity, blockingHero, blockingCard, blockingEffect);
            return true;
        }

        private static void WriteContributions(
            BinaryWriter writer, IReadOnlyList<ValueContributionProjection> contributions)
        {
            writer.Write(contributions.Count);
            foreach (var projection in contributions)
            {
                var contribution = projection.Contribution;
                writer.Write((int)contribution.Kind);
                writer.Write((int)contribution.Channel);
                writer.Write(contribution.Amount);
                writer.Write(projection.SourceLabel);
                WriteOptional(writer, contribution.SourceCardInstanceId.IsDefined,
                    contribution.SourceCardInstanceId.Value);
                WriteOptional(writer, contribution.SourceActionDefinitionId.IsDefined,
                    contribution.SourceActionDefinitionId.Value);
                WriteOptional(writer, contribution.SourceMinionId.IsDefined,
                    contribution.SourceMinionId.Value);
                WriteOptional(writer, contribution.SourceActiveEffectId.IsDefined,
                    contribution.SourceActiveEffectId.Value);
                WriteOptional(writer, contribution.HasSourceContinuousSourceId,
                    contribution.SourceContinuousSourceId);
            }
        }

        private static bool TryReadContributions(
            BinaryReader reader, out IReadOnlyList<NetworkValueContributionRow> contributions)
        {
            contributions = null;
            if (!TryReadCount(reader, out var count))
            {
                return false;
            }

            var rows = new List<NetworkValueContributionRow>(count);
            for (var index = 0; index < count; index++)
            {
                var kind = reader.ReadInt32();
                var channel = reader.ReadInt32();
                var amount = reader.ReadInt32();
                var label = reader.ReadString();
                if (!Enum.IsDefined(typeof(ValueContributionKind), kind) ||
                    kind == (int)ValueContributionKind.None ||
                    !Enum.IsDefined(typeof(PrintedValueChannel), channel) ||
                    channel == (int)PrintedValueChannel.None ||
                    string.IsNullOrWhiteSpace(label) ||
                    !TryReadOptional(reader, out var sourceCard) ||
                    !TryReadOptional(reader, out var sourceAction) ||
                    !TryReadOptional(reader, out var sourceMinion) ||
                    !TryReadOptional(reader, out var sourceActiveEffect) ||
                    !TryReadOptional(reader, out var sourceContinuous))
                {
                    return false;
                }

                rows.Add(new NetworkValueContributionRow(
                    kind, channel, amount, label, sourceCard, sourceAction, sourceMinion,
                    sourceActiveEffect, sourceContinuous));
            }

            contributions = Array.AsReadOnly(rows.ToArray());
            return true;
        }

        private static void WriteAttackTarget(BinaryWriter writer, AttackTarget target)
        {
            writer.Write((int)target.Kind);
            if (target.Kind == AttackTargetKind.Hero)
            {
                // Milestone 9.21a, protocol /26: the attacked body follows the hero.
                writer.Write(target.HeroId.Value);
                writer.Write(target.HeroBody);
            }
            else if (target.Kind == AttackTargetKind.Minion)
            {
                writer.Write(target.MinionId.Value);
            }
        }

        /// <summary>An undefined target reads back as null - the named absence, not a failure.</summary>
        private static bool TryReadAttackTarget(BinaryReader reader, out NetworkAttackTargetRow target)
        {
            target = null;
            var kind = reader.ReadInt32();
            if (kind == (int)AttackTargetKind.None)
            {
                return true;
            }

            if (kind == (int)AttackTargetKind.Hero)
            {
                var heroId = reader.ReadString();
                var heroBody = reader.ReadInt32();
                if (string.IsNullOrWhiteSpace(heroId) || heroBody < 1)
                {
                    return false;
                }

                target = new NetworkAttackTargetRow(kind, heroId, heroBody, null);
                return true;
            }

            if (kind == (int)AttackTargetKind.Minion)
            {
                var minionId = reader.ReadString();
                if (string.IsNullOrWhiteSpace(minionId))
                {
                    return false;
                }

                target = new NetworkAttackTargetRow(kind, null, 0, minionId);
                return true;
            }

            return false;
        }

        private static void WriteUnit(BinaryWriter writer, RuleObject unit)
        {
            writer.Write((int)unit.Kind);
            if (unit.Kind == RuleObjectKind.Hero)
            {
                writer.Write(unit.HeroId.Value);
            }
            else if (unit.Kind == RuleObjectKind.Minion)
            {
                writer.Write(unit.MinionId.Value);
            }
        }

        private static bool TryReadUnit(BinaryReader reader, out NetworkUnitRow unit)
        {
            unit = null;
            var kind = reader.ReadInt32();
            if (kind == (int)RuleObjectKind.Hero)
            {
                var heroId = reader.ReadString();
                if (string.IsNullOrWhiteSpace(heroId))
                {
                    return false;
                }

                unit = new NetworkUnitRow(kind, heroId, null);
                return true;
            }

            if (kind == (int)RuleObjectKind.Minion)
            {
                var minionId = reader.ReadString();
                if (string.IsNullOrWhiteSpace(minionId))
                {
                    return false;
                }

                unit = new NetworkUnitRow(kind, null, minionId);
                return true;
            }

            return false;
        }

        private static bool TryReadUnits(BinaryReader reader, out IReadOnlyList<NetworkUnitRow> units)
        {
            units = null;
            if (!TryReadCount(reader, out var count))
            {
                return false;
            }

            var rows = new List<NetworkUnitRow>(count);
            for (var index = 0; index < count; index++)
            {
                if (!TryReadUnit(reader, out var unit))
                {
                    return false;
                }

                rows.Add(unit);
            }

            units = Array.AsReadOnly(rows.ToArray());
            return true;
        }

        private static void WriteCardReference(BinaryWriter writer, CardDefinitionReference reference)
        {
            writer.Write(reference.IsDefined);
            if (reference.IsDefined)
            {
                writer.Write(reference.DefinitionId.Value);
                writer.Write(reference.ContentRevision);
            }
        }

        private static bool TryReadCardReference(
            BinaryReader reader, out (string definitionId, string revision) reference)
        {
            reference = (null, null);
            if (!reader.ReadBoolean())
            {
                return true;
            }

            var definitionId = reader.ReadString();
            var revision = reader.ReadString();
            if (string.IsNullOrWhiteSpace(definitionId) || string.IsNullOrWhiteSpace(revision))
            {
                return false;
            }

            reference = (definitionId, revision);
            return true;
        }

        private static void WriteRoute(BinaryWriter writer, IReadOnlyList<HexCoordinate> route)
        {
            writer.Write(route.Count);
            foreach (var space in route)
            {
                writer.Write(space.Q);
                writer.Write(space.R);
            }
        }

        private static bool TryReadRoute(
            BinaryReader reader, out IReadOnlyList<NetworkCoordinateRow> route)
        {
            route = null;
            if (!TryReadCount(reader, out var count))
            {
                return false;
            }

            var spaces = new List<NetworkCoordinateRow>(count);
            for (var index = 0; index < count; index++)
            {
                spaces.Add(new NetworkCoordinateRow(reader.ReadInt32(), reader.ReadInt32()));
            }

            route = Array.AsReadOnly(spaces.ToArray());
            return true;
        }

        private static void WriteStringList(BinaryWriter writer, int count, Func<int, string> value)
        {
            writer.Write(count);
            for (var index = 0; index < count; index++)
            {
                writer.Write(value(index));
            }
        }

        private static bool TryReadStringList(BinaryReader reader, out IReadOnlyList<string> values)
        {
            values = null;
            if (!TryReadCount(reader, out var count))
            {
                return false;
            }

            var list = new List<string>(count);
            for (var index = 0; index < count; index++)
            {
                var value = reader.ReadString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    return false;
                }

                list.Add(value);
            }

            values = Array.AsReadOnly(list.ToArray());
            return true;
        }

        private static bool TryReadCount(BinaryReader reader, out int count)
        {
            count = reader.ReadInt32();
            return count >= 0 && count <= NetworkSeatProjectionCodec.MaxListEntries;
        }

        private static void WriteOptional(BinaryWriter writer, bool isDefined, string value)
        {
            writer.Write(isDefined);
            if (isDefined)
            {
                writer.Write(value);
            }
        }

        private static bool TryReadOptional(BinaryReader reader, out string value)
        {
            value = null;
            if (!reader.ReadBoolean())
            {
                return true;
            }

            value = reader.ReadString();
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
