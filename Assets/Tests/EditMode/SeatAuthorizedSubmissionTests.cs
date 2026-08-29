using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.30a. What a seat may submit, and what happens to everything else.
    /// </summary>
    /// <remarks>
    /// Each case here plays the part of a modified client: it builds commands out of state a real
    /// seat could not see and sends them anyway. The host is not asked to be clever about that - it
    /// resolves who is asking from the seat the submission arrived on and compares the request with
    /// the set that seat's own projection published.
    /// </remarks>
    public sealed class SeatAuthorizedSubmissionTests
    {
        [Test]
        public void AnAuthorizedSubmissionIsAcceptedAndAdvancesTheAuthorityExactlyOnce()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var client = FirstCommittingClient(host);
            var before = client.Projection;
            var command = before.AuthorizedCommands.First();

            var result = client.Submit(new SeatSubmission(new SubmissionId("s1"), before.Version, command));

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(result.RejectionReason, Is.EqualTo(SeatSubmissionRejectionReason.None));
            Assert.That(result.Version, Is.EqualTo(before.Version + 1));
            Assert.That(result.Sequence, Is.EqualTo(1));
            Assert.That(result.Projection.Version, Is.EqualTo(result.Version),
                "The result carries this seat's view of the state its own submission produced.");
            Assert.That(host.Version, Is.EqualTo(before.Version + 1));
        }

        [Test]
        public void AStaleOrFutureExpectedVersionIsRefusedWithoutTouchingTheAuthority()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var first = FirstCommittingClient(host);
            Assert.That(first.Submit(new SeatSubmission(new SubmissionId("advance"),
                first.Projection.Version, first.Projection.AuthorizedCommands.First())).Accepted,
                Is.True, "A stale version needs an earlier version to exist.");
            var client = FirstCommittingClient(host);
            var command = client.Projection.AuthorizedCommands.First();
            var version = client.Projection.Version;

            var stale = client.Submit(new SeatSubmission(new SubmissionId("stale"), version - 1, command));
            var future = client.Submit(new SeatSubmission(new SubmissionId("future"), version + 1, command));

            foreach (var refused in new[] { stale, future })
            {
                Assert.That(refused.Accepted, Is.False);
                Assert.That(refused.RejectionReason,
                    Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));
                Assert.That(refused.SessionRejectionReason,
                    Is.EqualTo(MatchSessionRejectionReason.None),
                    "The authority was never consulted, so it has no opinion to report.");
                Assert.That(refused.Sequence, Is.EqualTo(0));
                Assert.That(refused.Version, Is.EqualTo(version));
            }

            Assert.That(host.Version, Is.EqualTo(version));
            Assert.That(host.Sequence, Is.EqualTo(1), "Only the setup command ever reached the authority.");
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(1));
        }

        [Test]
        public void ARepeatedSubmissionIdentityIsRefusedAndNeverExecutedTwice()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var client = FirstCommittingClient(host);
            var identity = new SubmissionId("once");
            var first = client.Submit(new SeatSubmission(
                identity, client.Projection.Version, client.Projection.AuthorizedCommands.First()));
            Assert.That(first.Accepted, Is.True, first.Detail);

            // The retry a lost acknowledgement would produce: same identity, and this time carrying
            // the version the client would read after re-synchronizing.
            var second = FirstCommittingClient(host);
            var repeated = second.Submit(new SeatSubmission(
                identity, second.Projection.Version, second.Projection.AuthorizedCommands.First()));

            Assert.That(repeated.Accepted, Is.False);
            Assert.That(repeated.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.DuplicateSubmission));
            Assert.That(host.Sequence, Is.EqualTo(1), "The authority ran that identity once.");
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(1));
        }

        [Test]
        public void AnEnvelopeRefusalLeavesTheSubmissionIdentityUnused()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var client = FirstCommittingClient(host);
            var identity = new SubmissionId("retried");
            var command = client.Projection.AuthorizedCommands.First();

            var refused = client.Submit(new SeatSubmission(
                identity, client.Projection.Version + 1, command));
            Assert.That(refused.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));

            var retried = client.Submit(new SeatSubmission(
                identity, client.Projection.Version, command));

            Assert.That(retried.Accepted, Is.True, retried.Detail);
            Assert.That(retried.Sequence, Is.EqualTo(1),
                "A submission the authority never saw does not spend its own name; the client " +
                "re-reads its version and retries the same attempt.");
        }

        [Test]
        public void AForgedPayloadIdentityIsRefusedAsAnOwnershipMismatch()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var seats = host.Seats.Seats;
            var attacker = host.ClientForSeat(seats[0].Id);
            var victim = seats.First(seat => !seat.HeroId.Equals(attacker.Seat.HeroId));
            var opponent = seats.First(seat => seat.TeamSide != attacker.Seat.TeamSide);
            var victimsCard = host.Projection.Cards.First(card =>
                card.OwnerId.Equals(victim.HeroId) && card.State.Location == CardLocation.Hand);
            var version = attacker.Projection.Version;

            var forged = new[]
            {
                DirectorCommand.CommitCard(victim.HeroId, victimsCard.Id),
                DirectorCommand.Pass(victim.HeroId),
                DirectorCommand.ChooseTiedActor(opponent.TeamSide, victimsCard.Id),
                DirectorCommand.RespawnHero(new HeroRespawnCommand(
                    HeroRespawnCommandKind.Respawn, victim.HeroId, victimsCard.Id, default))
            };

            for (var index = 0; index < forged.Length; index++)
            {
                var result = attacker.Submit(new SeatSubmission(
                    new SubmissionId("forged-" + index), version, forged[index]));

                Assert.That(result.Accepted, Is.False);
                Assert.That(result.RejectionReason,
                    Is.EqualTo(SeatSubmissionRejectionReason.OwnershipMismatch),
                    "A payload naming another seat's hero or side as the chooser is refused as a " +
                    "claim, before anything asks whether it would have been legal: " + forged[index]);
                Assert.That(result.Sequence, Is.EqualTo(0));
            }

            Assert.That(host.Version, Is.EqualTo(version));
            Assert.That(host.Projection.AcceptedCommands, Is.Empty);
        }

        [Test]
        public void ACommandOutsideTheSeatsOwnSurfaceIsRefusedEvenWhenItIsRulesLegal()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            MatchSeat acting = null;
            DirectorCommand theirCommand = null;
            for (var driven = 0; driven < 200 && theirCommand == null; driven += 10)
            {
                OwnerScopedProjectionTests.DriveThroughSeats(host, 10);
                foreach (var seat in host.Seats.Seats)
                {
                    // A command whose payload claims nothing, so that the refusal below can only
                    // have come from the offered surface and never from an ownership claim.
                    var candidate = host.ProjectionForSeat(seat.Id).AuthorizedCommands
                        .FirstOrDefault(command => !SeatCommandOwnership.Claim(command).IsDefined);
                    if (candidate != null)
                    {
                        acting = seat;
                        theirCommand = candidate;
                        break;
                    }
                }
            }

            Assert.That(theirCommand, Is.Not.Null,
                "The drive must reach a turn action or a pending answer that somebody owns.");

            var bystander = host.Seats.Seats.First(seat => !seat.Id.Equals(acting.Id));
            var version = host.Version;

            var result = host.ClientForSeat(bystander.Id).Submit(
                new SeatSubmission(new SubmissionId("bystander"), version, theirCommand));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.UnauthorizedCommand));
            Assert.That(result.SessionRejectionReason, Is.EqualTo(MatchSessionRejectionReason.None),
                "The rules were never asked: a command this seat was not offered is refused first.");
            Assert.That(host.Version, Is.EqualTo(version));

            // And the same command from the seat that was offered it is accepted, so the refusal was
            // about the seat and not about the command.
            var owner = host.ClientForSeat(acting.Id);
            Assert.That(owner.Submit(new SeatSubmission(
                new SubmissionId("owner"), version, theirCommand)).Accepted, Is.True);
        }

        [Test]
        public void AnUnknownSeatIsRefusedAndCannotBeProjected()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var real = FirstCommittingClient(host);
            var command = real.Projection.AuthorizedCommands.First();

            var result = host.SubmitForSeat(
                new SeatId("not-a-seat"),
                new SeatSubmission(new SubmissionId("ghost"), real.Projection.Version, command));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(SeatSubmissionRejectionReason.UnknownSeat));
            Assert.That(result.Projection, Is.Null,
                "There is no seat to answer with a view of.");
            Assert.That(() => host.ProjectionForSeat(new SeatId("not-a-seat")),
                Throws.ArgumentException);
            Assert.That(host.Version, Is.EqualTo(real.Projection.Version));
        }

        [Test]
        public void EveryCommandKindIsClassifiedAndOnlyChoosersClaimAnIdentity()
        {
            var heroClaiming = SeatCommandOwnership.HeroClaimingKinds;
            var teamClaiming = SeatCommandOwnership.TeamClaimingKinds;

            Assert.That(heroClaiming.Intersect(teamClaiming), Is.Empty,
                "A command claims one kind of identity or none, never both.");
            Assert.That(heroClaiming, Is.EquivalentTo(new[]
            {
                DirectorCommandKind.CommitCard,
                DirectorCommandKind.Pass,
                DirectorCommandKind.RespawnHero,
                DirectorCommandKind.ResolveCardWithoutRespawn
            }), "These four are the answers a hero gives about itself.");
            Assert.That(teamClaiming,
                Is.EquivalentTo(new[] { DirectorCommandKind.ChooseTiedActor }));

            var everyKind = Enum.GetValues(typeof(DirectorCommandKind))
                .Cast<DirectorCommandKind>()
                .ToArray();
            foreach (var kind in everyKind.Except(heroClaiming).Except(teamClaiming))
            {
                Assert.That(kind, Is.Not.EqualTo(DirectorCommandKind.CommitCard));
            }

            Assert.That(heroClaiming.Concat(teamClaiming).All(kind => everyKind.Contains(kind)),
                Is.True, "A rule cannot name a command kind that does not exist.");
            Assert.That(SeatCommandOwnership.Claim(
                    DirectorCommand.ChooseAttackTarget(default, default, new HeroId("anyone"))).IsDefined,
                Is.False,
                "An Attack target's HeroId names who is being attacked. Reading it as the caller " +
                "would let a seat act on another's turn by naming itself, and would refuse every " +
                "ordinary attack on anybody else.");
            Assert.That(SeatCommandOwnership.Claim(DirectorCommand.AdvanceEndOfRound()).IsDefined,
                Is.False);
        }

        [Test]
        public void NoRefusalMovesTheVersionTheStreamOrTheSnapshot()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var client = FirstCommittingClient(host);
            var command = client.Projection.AuthorizedCommands.First();
            var version = host.Version;
            var sequence = host.Sequence;
            var snapshot = host.CaptureSnapshot();

            var refusals = new List<SeatSubmissionResult>
            {
                host.SubmitForSeat(new SeatId("ghost"),
                    new SeatSubmission(new SubmissionId("a"), version, command)),
                client.Submit(new SeatSubmission(new SubmissionId("b"), version + 5, command)),
                client.Submit(new SeatSubmission(new SubmissionId("c"), version,
                    DirectorCommand.AdvanceEndOfRound()))
            };

            Assert.That(refusals.Select(value => value.RejectionReason), Is.EqualTo(new[]
            {
                SeatSubmissionRejectionReason.UnknownSeat,
                SeatSubmissionRejectionReason.StaleProjection,
                SeatSubmissionRejectionReason.UnauthorizedCommand
            }));
            Assert.That(host.Version, Is.EqualTo(version));
            Assert.That(host.Sequence, Is.EqualTo(sequence));
            Assert.That(host.CaptureSnapshot(), Is.EqualTo(snapshot));
            Assert.That(host.ReadResults(0).Records, Is.Empty);
        }

        [Test]
        public void AnAnswerToAVersionThatHasMovedIsStaleRatherThanARulesRefusal()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var client = FirstCommittingClient(host);
            var command = client.Projection.AuthorizedCommands.First();
            var version = client.Projection.Version;

            // Accept it once, then submit the same authorized command again with a fresh identity
            // against the version it was published at - the surface has moved on, so this is the
            // ordinary stale case, and the seat is told so rather than being told a rule said no.
            Assert.That(client.Submit(new SeatSubmission(
                new SubmissionId("first"), version, command)).Accepted, Is.True);
            var again = client.Submit(new SeatSubmission(new SubmissionId("second"), version, command));

            Assert.That(again.Accepted, Is.False);
            Assert.That(again.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(1));
        }

        private static ISeatClient FirstCommittingClient(IMatchHost host)
        {
            var seat = host.Seats.Seats.First(value =>
                host.ProjectionForSeat(value.Id).AuthorizedCommands
                    .Any(command => command.Kind == DirectorCommandKind.CommitCard));
            return host.ClientForSeat(seat.Id);
        }
    }
}
