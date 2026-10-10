using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Pve.Composition;

namespace GuardsDigital.Pve.Play
{
    public enum PveEncounterStatus
    {
        Ongoing = 0,
        Lost = 1
    }

    /// <summary>
    /// The PvE encounter over the ordinary Director session (PVE-D02/D04/D10/D14). The player's
    /// commands pass through a guard - Blue heroes only, the round-start windows, the standing tie
    /// prompt, nothing after the loss - and the enemies' commands are taken one step at a time by
    /// <see cref="Step"/>, so a presentation can pace them. Undo walks back over the enemies' and the
    /// flow's own commands to the last command the player gave.
    /// </summary>
    /// <remarks>
    /// Every command, the player's and the enemies' alike, is an ordinary Director command recorded
    /// by the inner session, so restart, Undo and replay rebuild the encounter exactly; this session
    /// adds only which recorded commands were the player's. A snapshot restore is not offered.
    /// </remarks>
    public sealed class PveEncounterSession : IMatchSession
    {
        private readonly IMatchSession inner;
        private readonly IMatchHost host;
        private readonly List<bool> playerGiven = new List<bool>();

        public PveEncounterSession(PveComposedEncounter encounter, IMatchHost host, IMatchSession inner)
        {
            Encounter = encounter ?? throw new ArgumentNullException(nameof(encounter));
            this.host = host;
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            Controller = new PveEnemyController(encounter);
        }

        public PveComposedEncounter Encounter { get; }
        public PveEnemyController Controller { get; }
        public MatchSessionProjection Projection => inner.Projection;

        public PveEncounterStatus Status =>
            PveEnemyController.IsLost(inner.Projection) ? PveEncounterStatus.Lost : PveEncounterStatus.Ongoing;

        /// <summary>What the encounter does next on its own, without changing anything.</summary>
        public PveStep Peek() => Controller.NextStep(inner.Projection, AuthorizedFor);

        /// <summary>The tie the player is asked to settle for an enemy, or null.</summary>
        public PvePrompt CurrentPrompt
        {
            get
            {
                var next = Peek();
                return next.Kind == PveStepKind.Prompt ? next.Prompt : null;
            }
        }

        /// <summary>
        /// Takes one automatic command - an enemy's or the encounter's own - if one is owed. Returns
        /// the step considered; its command was executed when its kind is <see cref="PveStepKind.Command"/>
        /// and the returned result is accepted.
        /// </summary>
        public MatchCommandResult Step(out PveStep step)
        {
            step = Peek();
            if (step.Kind != PveStepKind.Command) return null;
            var result = inner.Execute(step.Command);
            if (result.Accepted) playerGiven.Add(false);
            return result;
        }

        /// <summary>Runs automatic steps until the player is awaited, a prompt stands or a step is refused.</summary>
        public int RunAutomatic(int limit = 500)
        {
            var taken = 0;
            while (taken < limit)
            {
                var result = Step(out var step);
                if (step.Kind != PveStepKind.Command || result == null || !result.Accepted) break;
                taken++;
            }

            return taken;
        }

        public MatchCommandResult Execute(DirectorCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var refusal = Refusal(command);
            if (refusal != null)
            {
                return MatchCommandResult.Refused(command, refusal, inner.Projection);
            }

            var result = inner.Execute(command);
            if (result.Accepted) playerGiven.Add(true);
            return result;
        }

        /// <summary>Why the player may not give this command now, or null when they may.</summary>
        public string Refusal(DirectorCommand command)
        {
            var match = inner.Projection;
            if (Status == PveEncounterStatus.Lost) return "The encounter is lost; undo or restart.";

            var next = Controller.NextStep(match, AuthorizedFor);
            if (next.Kind == PveStepKind.Prompt)
            {
                return next.Prompt.Answers.Contains(command) ? null : next.Prompt.Text;
            }

            if (next.Kind == PveStepKind.Command) return "The enemies are still acting.";

            // PVE-D05/D14. A paused placement or granted level is finished before anything else; the
            // engine itself does not hold commitments back while a transition is paused.
            if (match.PendingSystemTransitionStep != null &&
                command.Kind != DirectorCommandKind.AnswerSystemTransitionStep)
            {
                return "Finish the placement or the level-up first.";
            }

            switch (command.Kind)
            {
                case DirectorCommandKind.CommitCard:
                case DirectorCommandKind.CommitTwoCards:
                case DirectorCommandKind.Pass:
                case DirectorCommandKind.RetractCommitment:
                case DirectorCommandKind.RetrieveOneAfterReveal:
                    if (Controller.IsEnemy(command.HeroId)) return "Enemies commit their own planned cards.";
                    return null;
                case DirectorCommandKind.BeginSystemTransition:
                    return MatchModeRules.TryReadLevelGrantHero(command.SystemTransitionDefinitionId, out var hero) &&
                        !Controller.IsEnemy(hero)
                        ? null
                        : "Only a hero's granted level may be started here.";
                case DirectorCommandKind.ChooseTiedActor:
                    return command.TeamSide == TeamSide.Blue ? null : "Enemy ties are settled through their prompt.";
                case DirectorCommandKind.AnswerSystemTransitionStep:
                    return command.TeamSide == TeamSide.Red ? "Enemy placements are settled through their prompt." : null;
                case DirectorCommandKind.AdvanceEndOfRound:
                    return null;
            }

            var owner = match.DecisionOwner;
            if (owner.IsDefined && owner.HeroIds.Any(Controller.IsEnemy))
            {
                return "That decision belongs to an enemy.";
            }

            var actor = match.SelectedActor;
            if (!owner.IsDefined && actor.ActorId.IsDefined && Controller.IsEnemy(actor.ActorId))
            {
                return "It is an enemy's turn.";
            }

            return null;
        }

        public MatchSessionOperationResult Undo()
        {
            MatchSessionOperationResult result = null;
            while (playerGiven.Count > 0 && !playerGiven[playerGiven.Count - 1])
            {
                result = inner.Undo();
                playerGiven.RemoveAt(playerGiven.Count - 1);
                if (!result.Accepted) return result;
            }

            if (playerGiven.Count > 0)
            {
                result = inner.Undo();
                if (result.Accepted) playerGiven.RemoveAt(playerGiven.Count - 1);
                return result;
            }

            return result ?? inner.Undo();
        }

        public MatchSessionOperationResult Restart()
        {
            var result = inner.Restart();
            if (result.Accepted) playerGiven.Clear();
            return result;
        }

        public MatchSessionOperationResult RestoreSnapshot(string payload) =>
            MatchSessionOperationResult.Refused("A PvE encounter does not restore snapshots.", inner.Projection);

        public string CaptureSnapshot() => inner.CaptureSnapshot();

        /// <summary>The commands a hero's seat may submit now, read from the host's seat projection.</summary>
        public IReadOnlyList<DirectorCommand> AuthorizedFor(HeroId hero)
        {
            if (host == null) return Array.Empty<DirectorCommand>();
            var seat = host.Seats.FindByHero(hero);
            return seat == null ? Array.Empty<DirectorCommand>() : host.ClientForSeat(seat.Id).Projection.AuthorizedCommands;
        }
    }
}
