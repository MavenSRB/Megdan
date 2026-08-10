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
        private readonly List<RuleEventLogEntry> entries = new List<RuleEventLogEntry>();

        public IReadOnlyList<RuleEventLogEntry> Entries => entries.AsReadOnly();

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

            entries.Clear();
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

            entries.Clear();
            entries.AddRange(restored);
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
                case RoundResolutionEventKind.GameEnded:
                    Add(
                        $"Round.{ruleEvent.Kind}",
                        roundReference,
                        $"stage={ruleEvent.EndOfRoundStage}");
                    return;
                default:
                    Add($"Round.{ruleEvent.Kind}", roundReference, "accepted boundary");
                    return;
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
                ruleEvent.Kind == TurnResolutionEventKind.AttackActionProgressed)
            {
                AppendCardAction(roundReference, ruleEvent.CardActionEvent);
                return;
            }

            var turnReferences = JoinReferences(
                roundReference,
                ruleEvent.ActorId.IsDefined ? $"hero={ruleEvent.ActorId.Value}" : null,
                ruleEvent.CardInstanceId.IsDefined ? $"card={ruleEvent.CardInstanceId.Value}" : null,
                ruleEvent.ActionDefinitionId.IsDefined ? $"action={ruleEvent.ActionDefinitionId.Value}" : null);
            var detail = ruleEvent.Kind == TurnResolutionEventKind.HoldActionPerformed
                ? $"invocation={ruleEvent.InvocationContext}"
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
                ruleEvent.TargetHeroId.IsDefined ? $"target={ruleEvent.TargetHeroId.Value}" : null,
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

            Add($"Action.{ruleEvent.Kind}", references, string.Join(";", detailParts));
        }

        private void Add(string eventType, string stableReferences, string detail)
        {
            entries.Add(new RuleEventLogEntry(entries.Count + 1, eventType, stableReferences, detail));
        }

        private static string JoinReferences(params string[] references)
        {
            return string.Join(";", references.Where(reference => !string.IsNullOrWhiteSpace(reference)));
        }
    }
}
