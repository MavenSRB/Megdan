using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Content;
using GuardsDigital.Pve.Play;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>
    /// Plays a PvE encounter headlessly: the encounter session takes every automatic step, and a
    /// deterministic, deliberately passive "player" answers Blue's decisions - first legal placement,
    /// first card committed, Hold, no Defense, first answer to every tie prompt.
    /// </summary>
    internal sealed class PveTestDriver
    {
        internal static readonly string[] TwoHeroes = { "hero-arien", "hero-brogan" };

        private PveTestDriver(PveComposedEncounter composed)
        {
            Composed = composed;
            Host = MatchHost.Compose(composed.InitialState);
            Director = new LocalDirectorMatchClient(Host);
            Session = new PveEncounterSession(composed, Host, Director);
        }

        internal PveComposedEncounter Composed { get; }
        internal IMatchHost Host { get; }
        internal LocalDirectorMatchClient Director { get; }
        internal PveEncounterSession Session { get; }
        internal IMatchReadModel Match => Session.Projection;
        internal List<string> Log { get; } = new List<string>();
        internal int PlayerCommands { get; private set; }

        internal static PveTestDriver Start(
            ulong seed = 1234,
            string[] heroes = null,
            PveEncounterDefinition encounter = null,
            PveEnemyCatalog catalog = null) =>
            new PveTestDriver(PveEncounterComposer.Compose(
                new PveEncounterSetup(heroes ?? TwoHeroes, seed),
                encounter ?? PveTestContent.Encounter(),
                catalog ?? PveTestContent.Catalog(),
                PveTestContent.OfficialMap()));

        /// <summary>Automatic steps, then one passive player answer; false when nothing can move.</summary>
        internal bool Advance()
        {
            var automatic = Session.RunAutomatic();
            if (Session.Status == PveEncounterStatus.Lost) return false;
            var command = PlayerAnswer();
            if (command == null) return automatic > 0;
            var result = Session.Execute(command);
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    "The passive player's " + command.Kind + " was refused: " + result.Detail);
            }

            PlayerCommands++;
            return true;
        }

        /// <summary>Plays until <paramref name="stop"/> holds, the encounter is lost or the budget is spent.</summary>
        internal void PlayUntil(Func<IMatchReadModel, bool> stop, int budget = 2000)
        {
            for (var step = 0; step < budget; step++)
            {
                Session.RunAutomatic();
                if (stop(Match) || Session.Status == PveEncounterStatus.Lost) return;
                if (!Advance()) return;
            }

            throw new InvalidOperationException("The encounter did not reach its stop within the budget.");
        }

        internal DirectorCommand PlayerAnswer()
        {
            var prompt = Session.CurrentPrompt;
            if (prompt != null) return prompt.Answers[0];

            var match = Match;
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
                foreach (var hero in Composed.Heroes)
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

            var commands = Composed.Heroes
                .SelectMany(hero => Host.ClientForSeat(Host.Seats.FindByHero(hero).Id).Projection.AuthorizedCommands)
                .ToArray();
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
