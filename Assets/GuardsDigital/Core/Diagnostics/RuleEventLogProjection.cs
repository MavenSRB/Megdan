using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Diagnostics
{
    public sealed class RuleEventLogProjection
    {
        private readonly AppendOnlyEventLog<RuleEventLogEntry> entries = new AppendOnlyEventLog<RuleEventLogEntry>();

        /// <summary>
        /// The log's contents as an immutable value, in constant time. Callers hold these across
        /// commands - the rollback pre-image is one - so this must not copy the accumulated log.
        /// </summary>
        public IReadOnlyList<RuleEventLogEntry> Entries => entries.Entries;

        public void ResetToSetup(MatchParticipants participants, TieBreakerState tieBreakerState)
        {
            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (!tieBreakerState.IsDefined)
            {
                throw new ArgumentException("The setup log requires a defined Tie Breaker.", nameof(tieBreakerState));
            }

            entries.Reset();
            var heroReferences = string.Join(",", participants.Heroes
                .Select(hero => hero.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal));
            Add(
                "Setup.MatchInitialized",
                $"heroes={heroReferences}",
                $"tieBreaker={tieBreakerState.RevealedSide};players={participants.Players.Count};teams={participants.Teams.Count}");
        }

        public void Append(IEnumerable<RoundResolutionEvent> ruleEvents)
        {
            if (ruleEvents == null)
            {
                throw new ArgumentNullException(nameof(ruleEvents));
            }

            foreach (var ruleEvent in ruleEvents)
            {
                Append(ruleEvent);
            }
        }

        public void Restore(IEnumerable<RuleEventLogEntry> restoredEntries)
        {
            if (restoredEntries == null)
            {
                throw new ArgumentNullException(nameof(restoredEntries));
            }

            var restored = restoredEntries.ToArray();
            if (restored.Any(entry => entry == null))
            {
                throw new ArgumentException("Restored rule-event entries must not contain null.", nameof(restoredEntries));
            }

            for (var index = 0; index < restored.Length; index++)
            {
                if (restored[index].Sequence != index + 1)
                {
                    throw new ArgumentException(
                        "Restored rule-event entries must have contiguous one-based sequence numbers.",
                        nameof(restoredEntries));
                }
            }

            entries.Reset(restored);
        }

        private void Append(RoundResolutionEvent ruleEvent)
        {
            var roundReference = $"round={ruleEvent.RoundIndex};turn={ruleEvent.TurnIndex}";
            switch (ruleEvent.Kind)
            {
                case RoundResolutionEventKind.CommitmentProgressed:
                    AppendCommitment(roundReference, ruleEvent.CommitmentEvent);
                    return;
                case RoundResolutionEventKind.TurnResolutionProgressed:
                    AppendTurn(roundReference, ruleEvent.TurnEvent);
                    return;
                case RoundResolutionEventKind.TurnSlotRecorded:
                    var assignments = string.Join(",", ruleEvent.TurnSlot.Assignments.Select(assignment =>
                        assignment.CardInstanceId.HasValue
                            ? $"{assignment.HeroId.Value}->{assignment.CardInstanceId.Value.Value}"
                            : $"{assignment.HeroId.Value}->PASS"));
                    Add("Round.TurnSlotRecorded", roundReference, $"assignments={assignments}");
                    return;
                case RoundResolutionEventKind.EndOfRoundStageResolved:
                    Add(
                        "Round.EndOfRoundStageResolved",
                        roundReference,
                        $"stage={ruleEvent.EndOfRoundStage};outcome={ruleEvent.EndOfRoundOutcomeKind}");
                    return;
                case RoundResolutionEventKind.CardRetrieved:
                    Add(
                        "Round.CardRetrieved",
                        JoinReferences(
                            roundReference,
                            $"hero={ruleEvent.CardOwnerId.Value.Value}",
                            $"card={ruleEvent.CardInstanceId.Value.Value}"),
                        $"stage={ruleEvent.EndOfRoundStage}");
                    return;
                case RoundResolutionEventKind.RoundCompleted:
                case RoundResolutionEventKind.NextRoundStarted:
                    Add(
                        $"Round.{ruleEvent.Kind}",
                        roundReference,
                        $"completedRound={ruleEvent.CompletedRound.RoundIndex}");
                    return;
                case RoundResolutionEventKind.ActiveEffectCancellationCheckpoint:
                    Add(
                        $"Round.{ruleEvent.Kind}",
                        roundReference,
                        $"stage={ruleEvent.EndOfRoundStage}");
                    return;
                case RoundResolutionEventKind.GameEnded:
                    // Milestone 7.7. The stage is still reported, and is now the part that can be
                    // absent: p.13's ending happens inside a turn. What is never absent is who won and
                    // why, which is what an ended match is read for.
                    Add(
                        "Round.GameEnded",
                        JoinReferences(
                            roundReference,
                            $"winner={ruleEvent.MatchOutcome.WinningTeamId.Value}",
                            $"loser={ruleEvent.MatchOutcome.LosingTeamId.Value}"),
                        ruleEvent.EndOfRoundStage.HasValue
                            ? $"cause={ruleEvent.MatchOutcome.Cause};stage={ruleEvent.EndOfRoundStage}"
                            : $"cause={ruleEvent.MatchOutcome.Cause}");
                    return;
                case RoundResolutionEventKind.TimingProgressed:
                    AppendTiming(roundReference, ruleEvent.ActiveEffectEvent);
                    return;
                case RoundResolutionEventKind.MinionBattleChoiceRequested:
                    Add(
                        "Round.MinionBattleChoiceRequested",
                        JoinReferences(
                            roundReference,
                            $"resolution={ruleEvent.EndOfRoundResolutionId.Value}",
                            $"choice={ruleEvent.EndOfRoundChoiceId.Value}",
                            $"team={ruleEvent.TeamId.Value}"),
                        $"side={ruleEvent.TeamSide}");
                    return;
                case RoundResolutionEventKind.MinionBattleSelectionChanged:
                    Add(
                        "Round.MinionBattleSelectionChanged",
                        JoinReferences(
                            roundReference,
                            $"resolution={ruleEvent.EndOfRoundResolutionId.Value}",
                            $"choice={ruleEvent.EndOfRoundChoiceId.Value}",
                            $"team={ruleEvent.TeamId.Value}",
                            ruleEvent.HeroId.IsDefined
                                ? $"hero={ruleEvent.HeroId.Value}"
                                : $"minion={ruleEvent.MinionId.Value}"),
                        $"side={ruleEvent.TeamSide};operation={ruleEvent.MinionBattleChoiceCommandKind}");
                    return;
                case RoundResolutionEventKind.MinionBattleMinionRemoved:
                    Add(
                        "Round.MinionBattleMinionRemoved",
                        JoinReferences(
                            roundReference,
                            $"resolution={ruleEvent.EndOfRoundResolutionId.Value}",
                            $"team={ruleEvent.TeamId.Value}",
                            ruleEvent.HeroId.IsDefined
                                ? $"hero={ruleEvent.HeroId.Value}"
                                : $"minion={ruleEvent.MinionId.Value}"),
                        $"side={ruleEvent.TeamSide};stage={ruleEvent.EndOfRoundStage}");
                    return;
                case RoundResolutionEventKind.LanePushRequested:
                    Add(
                        "Round.LanePushRequested",
                        JoinReferences(
                            roundReference,
                            $"request={ruleEvent.LanePushRequestId.Value}",
                            $"resolution={ruleEvent.EndOfRoundResolutionId.Value}",
                            $"losingTeam={ruleEvent.TeamId.Value}"),
                        $"losingSide={ruleEvent.TeamSide};checkpoint={EndOfRoundStage.ResolveRequestedLanePush}");
                    return;
                case RoundResolutionEventKind.SystemTransitionProgressed:
                {
                    // Milestone 6.10a. What a reader needs is where the transition is now, so a paused
                    // one logs its step and chooser and a finished one logs that it finished. The
                    // completed steps are deliberately absent: they are the continuation's own record,
                    // and repeating them here would be a second history that could disagree with it.
                    var step = ruleEvent.SystemTransitionStep;
                    Add(
                        "Round.SystemTransitionProgressed",
                        step == null
                            ? roundReference
                            : JoinReferences(
                                roundReference,
                                $"transition={step.InstanceId.Value}",
                                $"definition={step.DefinitionId.Value}",
                                $"step={step.StepId.Value}"),
                        step == null
                            ? $"completed={ruleEvent.SystemTransitionCompleted}"
                            : $"chooser={step.ChoosingSide};options={step.Options.Count}");
                    return;
                }

                case RoundResolutionEventKind.HeroLevelUpProgressed:
                {
                    // Milestone 7.5. One line per thing p.15's and p.16's leveling did, naming the hero
                    // and the Level it belongs to, because a hero that buys several levels in one stage
                    // produces several of these and only the Level tells them apart.
                    var outcome = ruleEvent.HeroLevelUpOutcome;
                    Add(
                        $"Round.HeroLevelUp{outcome.Kind}",
                        JoinReferences(
                            roundReference,
                            $"hero={outcome.HeroId.Value}",
                            $"level={outcome.Level}"),
                        Detail(outcome));
                    return;
                }

                case RoundResolutionEventKind.PityCoinCollected:
                {
                    // Milestone 7.6. The balance after is the part a reader wants: p.8's own closing
                    // sentence is about what this coin cannot then buy, and a line that named only the
                    // hero would not show whether it took them to a level threshold.
                    var grant = ruleEvent.PityCoinGrant;
                    Add(
                        "Round.PityCoinCollected",
                        JoinReferences(roundReference, $"hero={grant.RecipientHeroId.Value}"),
                        $"coins={grant.Amount};balance={grant.BalanceAfter};stage={ruleEvent.EndOfRoundStage}");
                    return;
                }

                default:
                    Add($"Round.{ruleEvent.Kind}", roundReference, "accepted boundary");
                    return;
            }
        }

        /// <summary>Milestone 7.5: what one leveling outcome moved, in that outcome's own vocabulary.</summary>
        private static string Detail(GuardsDigital.Core.Rules.Progression.HeroLevelUpOutcome outcome)
        {
            switch (outcome.Kind)
            {
                case GuardsDigital.Core.Rules.Progression.HeroLevelUpOutcomeKind.LevelPurchased:
                    return $"cost={outcome.Spend.Amount};coins={outcome.Spend.BalanceAfter}";
                case GuardsDigital.Core.Rules.Progression.HeroLevelUpOutcomeKind.CardUpgraded:
                    return $"returned={outcome.UpgradedCardInstanceId.Value};" +
                        $"taken={outcome.TakenCardInstanceId.Value};" +
                        $"setAside={(outcome.SetAsideCardInstanceId.IsDefined ? outcome.SetAsideCardInstanceId.Value : "none")}";
                case GuardsDigital.Core.Rules.Progression.HeroLevelUpOutcomeKind.ItemGained:
                    return $"card={outcome.Item.SourceCardInstanceId.Value};" +
                        $"slot={outcome.Item.Slot};icon={outcome.Item.Icon}";
                default:
                    return $"card={outcome.UltimateCardInstanceId.Value}";
            }
        }

        private void AppendCommitment(string roundReference, TurnCardCommitmentEvent ruleEvent)
        {
            var references = ruleEvent.HasHeroId
                ? JoinReferences(roundReference, $"hero={ruleEvent.HeroId.Value}")
                : roundReference;
            var detail = ruleEvent.Kind == TurnCardCommitmentEventKind.CardCommittedFacedown
                ? "facedown commitment accepted;card identity hidden"
                : ruleEvent.Kind == TurnCardCommitmentEventKind.HeroPassed
                    ? "empty-hand Pass accepted"
                    : ruleEvent.Kind == TurnCardCommitmentEventKind.CardRevealed
                        ? $"card={ruleEvent.CardInstanceId.Value}"
                        : "joint reveal completed";

            if (ruleEvent.HasCardInstanceId)
            {
                references = JoinReferences(references, $"card={ruleEvent.CardInstanceId.Value}");
            }

            Add($"Commitment.{ruleEvent.Kind}", references, detail);
        }

        private void AppendTurn(string roundReference, TurnResolutionEvent ruleEvent)
        {
            if (ruleEvent.Kind == TurnResolutionEventKind.InitiativeProgressed)
            {
                var initiative = ruleEvent.InitiativeEvent;
                var references = JoinReferences(
                    roundReference,
                    initiative.ActorId.IsDefined ? $"hero={initiative.ActorId.Value}" : null,
                    initiative.CardInstanceId.IsDefined ? $"card={initiative.CardInstanceId.Value}" : null);
                Add(
                    $"Initiative.{initiative.Kind}",
                    references,
                    $"side={initiative.TeamSide};initiative={initiative.InitiativeTotal}");
                return;
            }

            if (ruleEvent.Kind == TurnResolutionEventKind.MovementActionProgressed ||
                ruleEvent.Kind == TurnResolutionEventKind.AttackActionProgressed ||
                ruleEvent.Kind == TurnResolutionEventKind.FastTravelActionProgressed ||
                ruleEvent.Kind == TurnResolutionEventKind.ClearActionProgressed ||
                ruleEvent.Kind == TurnResolutionEventKind.SkillActionProgressed)
            {
                AppendCardAction(roundReference, ruleEvent.CardActionEvent);
                return;
            }

            if (ruleEvent.Kind == TurnResolutionEventKind.TimingProgressed)
            {
                AppendTiming(roundReference, ruleEvent.ActiveEffectEvent);
                return;
            }

            var turnReferences = JoinReferences(
                roundReference,
                ruleEvent.ActorId.IsDefined ? $"hero={ruleEvent.ActorId.Value}" : null,
                ruleEvent.CardInstanceId.IsDefined ? $"card={ruleEvent.CardInstanceId.Value}" : null,
                ruleEvent.ActionDefinitionId.IsDefined ? $"action={ruleEvent.ActionDefinitionId.Value}" : null);
            var detail = ruleEvent.Kind == TurnResolutionEventKind.HoldActionPerformed
                ? $"invocation={ruleEvent.InvocationContext}"
                // Milestone 8.9. The kind is the half of rulebook p.9's fact that a trigger narrows by,
                // so the log says which action was basic rather than only that one was.
                : ruleEvent.Kind == TurnResolutionEventKind.BasicActionPerformed
                    ? $"basicActionKind={ruleEvent.BasicAction.Kind}"
                    : "accepted turn checkpoint";
            Add($"Turn.{ruleEvent.Kind}", turnReferences, detail);
        }

        private void AppendCardAction(string roundReference, CardActionResolutionEvent ruleEvent)
        {
            var references = JoinReferences(
                roundReference,
                $"resolution={ruleEvent.ResolutionId.Value}",
                $"hero={ruleEvent.ActorId.Value}",
                $"card={ruleEvent.SourceCardInstanceId.Value}",
                $"action={ruleEvent.ActionDefinitionId.Value}",
                ruleEvent.ReplacedActionDefinitionId.IsDefined
                    ? $"replaces={ruleEvent.ReplacedActionDefinitionId.Value}"
                    : null,
                ruleEvent.TargetHeroId.IsDefined ? $"target={ruleEvent.TargetHeroId.Value}" : null,
                ruleEvent.TargetMinionId.IsDefined ? $"minion={ruleEvent.TargetMinionId.Value}" : null,
                ruleEvent.TargetTokenInstanceId.IsDefined ? $"token={ruleEvent.TargetTokenInstanceId.Value}" : null,
                ruleEvent.DefeatOutcomeId.IsDefined ? $"defeat={ruleEvent.DefeatOutcomeId.Value}" : null,
                ruleEvent.DefenseCardInstanceId.IsDefined ? $"defenseCard={ruleEvent.DefenseCardInstanceId.Value}" : null);

            var detailParts = new List<string> { $"step={ruleEvent.StepId.Value}" };
            if (ruleEvent.Kind == CardActionResolutionEventKind.MovementActionStarted ||
                ruleEvent.Kind == CardActionResolutionEventKind.MovementChoiceRequested ||
                ruleEvent.Kind == CardActionResolutionEventKind.MovementStepCommitted ||
                ruleEvent.Kind == CardActionResolutionEventKind.MovementActionCompleted)
            {
                detailParts.Add($"remaining={ruleEvent.RemainingAllowance}");
                if (ruleEvent.Kind == CardActionResolutionEventKind.MovementStepCommitted)
                {
                    detailParts.Add($"move={ruleEvent.MovementEvent.Origin}->{ruleEvent.MovementEvent.Destination}");
                }
            }

            if (ruleEvent.Kind == CardActionResolutionEventKind.FastTravelActionStarted ||
                ruleEvent.Kind == CardActionResolutionEventKind.FastTravelCommitted ||
                ruleEvent.Kind == CardActionResolutionEventKind.FastTravelActionCompleted)
            {
                detailParts.Add(
                    $"fastTravel={ruleEvent.FastTravelEvent.Origin}->{ruleEvent.FastTravelEvent.Destination}");
                detailParts.Add(
                    $"zones={ruleEvent.FastTravelEvent.OriginZoneId.Value}->{ruleEvent.FastTravelEvent.DestinationZoneId.Value}");
            }

            if (ruleEvent.AttackTotal > 0)
            {
                detailParts.Add($"attack={ruleEvent.AttackTotal}");
            }

            if (ruleEvent.DefenseTotal > 0)
            {
                detailParts.Add($"defense={ruleEvent.DefenseTotal}");
            }

            if (ruleEvent.AttackOutcome != AttackResolutionOutcome.None)
            {
                detailParts.Add($"outcome={ruleEvent.AttackOutcome}");
            }

            if (ruleEvent.MinionDefeatFactKind != MinionDefeatFactKind.None)
            {
                detailParts.Add($"minionFact={ruleEvent.MinionDefeatFactKind}");
            }

            if (ruleEvent.Kind == CardActionResolutionEventKind.TokenCleared)
            {
                detailParts.Add($"tokenDefinition={ruleEvent.TargetTokenDefinitionReference}");
                detailParts.Add($"space={ruleEvent.TargetTokenCoordinate}");
            }

            if (ruleEvent.Kind == CardActionResolutionEventKind.DisplacedMinionCorrectionMoved ||
                ruleEvent.Kind == CardActionResolutionEventKind.DisplacedMinionCorrectionPlaced ||
                ruleEvent.Kind == CardActionResolutionEventKind.DisplacedMinionCorrectionLeftInPlace)
            {
                detailParts.Add(
                    $"correction={ruleEvent.CorrectionEvent.Origin}->{ruleEvent.CorrectionEvent.Destination}");
                detailParts.Add($"correctionKind={ruleEvent.CorrectionEvent.CorrectionKind}");
            }

            Add($"Action.{ruleEvent.Kind}", references, string.Join(";", detailParts));
        }

        private void Add(string eventType, string stableReferences, string detail)
        {
            entries.Add(new RuleEventLogEntry(entries.Count + 1, eventType, stableReferences, detail));
        }

        private void AppendTiming(string roundReference, GuardsDigital.Core.Timing.ActiveEffectLifecycleEvent ruleEvent)
        {
            Add(
                $"Timing.{ruleEvent.Kind}",
                JoinReferences(roundReference, $"effect={ruleEvent.EffectId.Value}"),
                $"checkpoint={ruleEvent.Checkpoint}");
        }

        private static string JoinReferences(params string[] references)
        {
            return string.Join(";", references.Where(reference => !string.IsNullOrWhiteSpace(reference)));
        }
    }
}
