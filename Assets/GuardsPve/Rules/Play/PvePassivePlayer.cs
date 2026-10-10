using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Transitions;

namespace GuardsDigital.Pve.Play
{
    /// <summary>
    /// A deterministic, deliberately passive stand-in for the player, used by the headless tests and
    /// the executable's launch smoke: first placement, first card, Hold, no Defense, the first answer
    /// to every prompt. It proves the encounter runs; it plays no strategy.
    /// </summary>
    public static class PvePassivePlayer
    {
        public static DirectorCommand NextAnswer(PveEncounterSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));
            var prompt = session.CurrentPrompt;
            if (prompt != null) return prompt.Answers[0];

            var match = session.Projection;
            var pending = match.PendingSystemTransitionStep;
            if (pending != null)
            {
                var item = pending.OpenItems.FirstOrDefault(value => value.ChoosingSide == TeamSide.Blue);
                return item == null
                    ? null
                    : DirectorCommand.AnswerSystemTransitionStep(new SystemTransitionChoiceCommand(
                        pending.InstanceId, pending.StepId, item.ItemId, TeamSide.Blue, item.Options[0]));
            }

            var tie = match.PendingInitiativeChoice;
            if (tie != null && tie.OwningSide == TeamSide.Blue)
            {
                return DirectorCommand.ChooseTiedActor(TeamSide.Blue, tie.CandidateCardInstanceIds[0]);
            }

            if (match.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                foreach (var hero in session.Encounter.Heroes)
                {
                    var commitment = match.Commitments.FirstOrDefault(value => value.HeroId.Equals(hero));
                    if (!commitment.HeroId.IsDefined || commitment.Status != TurnCardCommitmentStatus.AwaitingResponse)
                        continue;
                    var card = match.Cards
                        .Where(value => value.OwnerId.Equals(hero) && value.State.Location == CardLocation.Hand)
                        .OrderBy(value => value.Id.Value, StringComparer.Ordinal)
                        .FirstOrDefault();
                    return card == null ? DirectorCommand.Pass(hero) : DirectorCommand.CommitCard(hero, card.Id);
                }

                return null;
            }

            var commands = session.Encounter.Heroes.SelectMany(session.AuthorizedFor).ToArray();
            return Prefer(commands, DirectorCommandKind.DeclineDefense)
                ?? Prefer(commands, DirectorCommandKind.ReleaseTurn)
                ?? Prefer(commands, DirectorCommandKind.PerformHold)
                ?? Prefer(commands, DirectorCommandKind.ResolveCardWithoutRespawn)
                ?? Prefer(commands, DirectorCommandKind.StopMovement)
                ?? commands.FirstOrDefault();
        }

        private static DirectorCommand Prefer(IEnumerable<DirectorCommand> commands, DirectorCommandKind kind) =>
            commands.FirstOrDefault(command => command.Kind == kind);
    }
}
