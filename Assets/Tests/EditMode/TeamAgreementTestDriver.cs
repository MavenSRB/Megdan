using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    internal static class TeamAgreementTestDriver
    {
        public static bool IsFinalAgreementCommand(DirectorCommand command) =>
            command != null &&
            (command.Kind == DirectorCommandKind.ChooseTiedActor ||
             command.Kind == DirectorCommandKind.ConfirmMinionBattleRemovals);

        public static bool SubmitFinalAgreementCommand(
            IMatchHost host,
            DirectorCommand finalCommand,
            string idPrefix,
            Action<MatchSeat> onStatementAccepted = null)
        {
            if (!IsFinalAgreementCommand(finalCommand))
                return false;

            var initial = host.Seats.Seats
                .Select(seat => new { Seat = seat, Projection = host.ProjectionForSeat(seat.Id) })
                .FirstOrDefault(value => value.Projection.TeamAgreement != null);
            Assert.That(initial, Is.Not.Null, "A final team choice must publish an agreement proposal.");

            var participants = initial.Projection.TeamAgreement.Participants
                .Select(value => value.HeroId)
                .ToArray();
            foreach (var heroId in participants)
            {
                var seat = host.Seats.Seats.Single(value => value.HeroId.Equals(heroId));
                var projection = host.ProjectionForSeat(seat.Id);
                var agreement = projection.TeamAgreement;
                Assert.That(agreement, Is.Not.Null);
                var statement = agreement.AuthorizedCommands.Single(value =>
                    finalCommand.Kind == DirectorCommandKind.ChooseTiedActor
                        ? value.Kind == TeamAgreementCommandKind.PreferInitiativeCandidate &&
                          value.PreferredCardInstanceId.Equals(finalCommand.CardInstanceId)
                        : value.Kind == TeamAgreementCommandKind.AgreeToMinionBattleSelection);
                var result = host.ClientForSeat(seat.Id).SubmitAgreement(
                    new TeamAgreementSubmission(
                        new SubmissionId(idPrefix + "-" + seat.Id.Value),
                        projection.Version,
                        statement));
                Assert.That(result.Accepted, Is.True, result.Detail);
                onStatementAccepted?.Invoke(seat);
            }

            return true;
        }
    }
}
