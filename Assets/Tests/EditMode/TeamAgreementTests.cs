using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Replay;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TeamAgreementTests
    {
        [Test]
        public void TiedInitiative_PlayerSeatsWaitForUnanimousPublicCandidatePreferenceBeforeChoosingTheActor()
        {
            var host = ReachAgreement(TeamAgreementKind.Initiative);
            var publicProposal = host.Seats.Seats[0].Id;
            var agreement = host.ProjectionForSeat(publicProposal).TeamAgreement;
            var chosen = agreement.Candidates[0].CardInstanceId;
            var startingVersion = host.Version;
            var startingSequence = host.Sequence;

            for (var index = 0; index < agreement.Participants.Count; index++)
            {
                var participant = agreement.Participants[index];
                var seat = host.Seats.Seats.Single(value => value.HeroId.Equals(participant.HeroId));
                var view = host.ProjectionForSeat(seat.Id);
                var command = view.TeamAgreement.AuthorizedCommands.Single(value =>
                    value.PreferredCardInstanceId.Equals(chosen));
                var result = host.ClientForSeat(seat.Id).SubmitAgreement(new TeamAgreementSubmission(
                    new SubmissionId("initiative-unanimity-" + index), view.Version, command));
                Assert.That(result.Accepted, Is.True, result.Detail);

                if (index + 1 < agreement.Participants.Count)
                {
                    Assert.That(host.Version, Is.EqualTo(startingVersion));
                    Assert.That(host.Sequence, Is.EqualTo(startingSequence));
                    Assert.That(host.Projection.PendingInitiativeChoice, Is.Not.Null);
                    foreach (var observer in host.Seats.Seats)
                        Assert.That(host.ProjectionForSeat(observer.Id).TeamAgreement.Participants[index]
                            .PreferredCardInstanceId, Is.EqualTo(chosen));
                }
            }

            Assert.That(host.Projection.PendingInitiativeChoice, Is.Null);
            Assert.That(host.Version, Is.EqualTo(startingVersion + 1));
            Assert.That(host.Sequence, Is.EqualTo(startingSequence + 1),
                "Unanimity submits exactly one existing rules command.");
        }

        [Test]
        public void MinionBattle_PlayerSeatsWaitForUnanimousPublicConfirmationOfTheCurrentRemovalSet()
        {
            var host = ReachAgreement(TeamAgreementKind.MinionBattle);
            CompleteMinionSelection(host, "minion-select");
            var agreement = host.Seats.Seats.Select(seat => host.ProjectionForSeat(seat.Id).TeamAgreement)
                .First(value => value != null);
            Assert.That(agreement.SelectionIsConfirmable, Is.True);
            var startingVersion = host.Version;
            var startingSequence = host.Sequence;
            var acceptedBefore = host.Projection.AcceptedCommands.Count;

            for (var index = 0; index < agreement.Participants.Count; index++)
            {
                var seat = host.Seats.Seats.Single(value =>
                    value.HeroId.Equals(agreement.Participants[index].HeroId));
                var view = host.ProjectionForSeat(seat.Id);
                var command = view.TeamAgreement.AuthorizedCommands.Single(value =>
                    value.Kind == TeamAgreementCommandKind.AgreeToMinionBattleSelection);
                var result = host.ClientForSeat(seat.Id).SubmitAgreement(new TeamAgreementSubmission(
                    new SubmissionId("minion-unanimity-" + index), view.Version, command));
                Assert.That(result.Accepted, Is.True, result.Detail);
                if (index + 1 < agreement.Participants.Count)
                {
                    Assert.That(host.Version, Is.EqualTo(startingVersion));
                    Assert.That(host.Sequence, Is.EqualTo(startingSequence));
                    Assert.That(host.Projection.PendingMinionBattleChoice, Is.Not.Null);
                }
            }

            // Milestone 8.35d. Unanimity still submits exactly one final rules choice; confirming
            // the Minion Battle completes rulebook p.8's first stage, so the seat-serving host
            // then runs the rest of the sequence itself and those advances are in the stream too.
            var appended = host.Projection.AcceptedCommands.Skip(acceptedBefore).ToArray();
            Assert.That(host.Projection.PendingMinionBattleChoice, Is.Null);
            Assert.That(appended.First().Kind,
                Is.EqualTo(DirectorCommandKind.ConfirmMinionBattleRemovals));
            Assert.That(appended.Count(command =>
                    command.Kind == DirectorCommandKind.ConfirmMinionBattleRemovals),
                Is.EqualTo(1), "The final rules choice reaches the authority exactly once.");
            Assert.That(appended.Skip(1).All(command =>
                    command.Kind == DirectorCommandKind.AdvanceEndOfRound), Is.True);
            Assert.That(host.Version, Is.EqualTo(startingVersion + appended.Length));
            Assert.That(host.Sequence, Is.EqualTo(startingSequence + appended.Length));
        }

        [Test]
        public void TeamAgreement_RefusesIneligibleOrStaleStatementsAndSubmitsOneFamilyRulesChoice()
        {
            var host = ReachAgreement(TeamAgreementKind.Initiative);
            var eligible = host.Seats.Seats.First(seat =>
                host.ProjectionForSeat(seat.Id).TeamAgreement.AuthorizedCommands.Count > 0);
            var outsider = host.Seats.Seats.First(seat =>
                host.ProjectionForSeat(seat.Id).TeamAgreement.AuthorizedCommands.Count == 0);
            var offered = host.ProjectionForSeat(eligible.Id).TeamAgreement.AuthorizedCommands[0];
            var version = host.Version;
            var sequence = host.Sequence;

            var ineligible = host.ClientForSeat(outsider.Id).SubmitAgreement(new TeamAgreementSubmission(
                new SubmissionId("agreement-ineligible"), version, offered));
            Assert.That(ineligible.Accepted, Is.False);
            Assert.That(ineligible.RejectionReason,
                Is.EqualTo(TeamAgreementSubmissionRejectionReason.IneligibleSeat));
            Assert.That(host.Version, Is.EqualTo(version));
            Assert.That(host.Sequence, Is.EqualTo(sequence));

            var stale = host.ClientForSeat(eligible.Id).SubmitAgreement(new TeamAgreementSubmission(
                new SubmissionId("agreement-stale"), version + 1, offered));
            Assert.That(stale.Accepted, Is.False);
            Assert.That(stale.RejectionReason,
                Is.EqualTo(TeamAgreementSubmissionRejectionReason.StaleProjection));
            Assert.That(host.Version, Is.EqualTo(version));
            Assert.That(host.Sequence, Is.EqualTo(sequence));

            var first = host.ClientForSeat(eligible.Id).SubmitAgreement(new TeamAgreementSubmission(
                new SubmissionId("agreement-duplicate"), version, offered));
            Assert.That(first.Accepted, Is.True);

            // Milestone 8.34. An identical repeat is answered from what the statement already did
            // - the failure policy's retained-result clause, which the agreement path obeys
            // exactly as the command path does - and records nothing a second time.
            var repeated = host.ClientForSeat(eligible.Id).SubmitAgreement(new TeamAgreementSubmission(
                new SubmissionId("agreement-duplicate"), version, offered));
            Assert.That(repeated.Accepted, Is.EqualTo(first.Accepted));
            Assert.That(repeated.RejectionReason, Is.EqualTo(first.RejectionReason));
            Assert.That(repeated.Version, Is.EqualTo(first.Version));
            Assert.That(host.Version, Is.EqualTo(version));
            Assert.That(host.Sequence, Is.EqualTo(sequence));

            // The same identity carrying a different statement is a used name, and is refused.
            var withdrawal = host.ProjectionForSeat(eligible.Id).TeamAgreement.AuthorizedCommands
                .FirstOrDefault(command => !command.Equals(offered));
            if (withdrawal != null)
            {
                var duplicate = host.ClientForSeat(eligible.Id).SubmitAgreement(
                    new TeamAgreementSubmission(
                        new SubmissionId("agreement-duplicate"), version, withdrawal));
                Assert.That(duplicate.Accepted, Is.False);
                Assert.That(duplicate.RejectionReason,
                    Is.EqualTo(TeamAgreementSubmissionRejectionReason.DuplicateSubmission));
                Assert.That(host.Version, Is.EqualTo(version));
                Assert.That(host.Sequence, Is.EqualTo(sequence));
            }
        }

        /// <summary>
        /// Milestone 8.35e shares this walk rather than re-deriving a second seed: it is the one
        /// place that knows how far a seeded seat-served match has to go to reach each agreement
        /// family, and a second copy would drift from it the next time the walk moves.
        /// </summary>
        internal static IMatchHost ReachAgreement(TeamAgreementKind kind)
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());

            // Milestone 8.35d re-derived the seed. This walk no longer draws the round's own
            // advance - the seat-serving host runs p.8's stages itself - and that moved seed 2's
            // Minion Battle beyond this budget; seed 3 reaches it at step 268.
            var random = new MatchLoopRandom(3);
            for (var step = 0; step < 2400; step++)
            {
                var agreement = host.Seats.Seats.Select(seat => host.ProjectionForSeat(seat.Id).TeamAgreement)
                    .FirstOrDefault(value => value != null);
                if (agreement?.Kind == kind)
                    return host;

                var legal = DeterministicMatchLoop.EnumerateLegalCommands(host.Projection);
                Assert.That(legal, Is.Not.Empty, "The seeded match ended before reaching " + kind + ".");
                var command = legal[random.Next(legal.Count)];
                if (TeamAgreementTestDriver.SubmitFinalAgreementCommand(
                    host, command, "reach-" + kind + "-" + step))
                    continue;
                var seat = host.Seats.Seats.First(value =>
                    host.ProjectionForSeat(value.Id).AuthorizedCommands.Contains(command));
                var client = host.ClientForSeat(seat.Id);
                var result = client.Submit(new SeatSubmission(
                    new SubmissionId("reach-direct-" + kind + "-" + step),
                    client.Projection.Version,
                    command));
                Assert.That(result.Accepted, Is.True, result.Detail);
            }

            throw new AssertionException("The seeded match did not reach " + kind + ".");
        }

        internal static void CompleteMinionSelection(IMatchHost host, string idPrefix)
        {
            var index = 0;
            while (!host.Projection.PendingMinionBattleChoice.CanConfirm)
            {
                var command = host.Projection.DecisionContract.Commands.First(value =>
                    value.Kind == DirectorCommandKind.SelectMinionBattleRemoval);
                var seat = host.Seats.Seats.First(value =>
                    host.ProjectionForSeat(value.Id).AuthorizedCommands.Contains(command));
                var client = host.ClientForSeat(seat.Id);
                var result = client.Submit(new SeatSubmission(
                    new SubmissionId(idPrefix + "-" + index++), client.Projection.Version, command));
                Assert.That(result.Accepted, Is.True, result.Detail);
            }
        }
    }
}
