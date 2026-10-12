using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Play;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    public sealed class PveCoopMatchTests
    {
        internal static PveCoopMatch Create() => new PveCoopMatch(PveEncounterComposer.Compose(
            new PveEncounterSetup(new[] { "hero-arien", "hero-brogan", "hero-wasp" }, 42),
            PveTestContent.Encounter(), PveTestContent.Catalog(), PveTestContent.OfficialMap()));

        [Test]
        public void OnlyBlueHasSeatsAndEnemyCardsArePublicButOtherHeroesHandsAreNot()
        {
            var match = Create();
            Assert.That(match.Host.Seats.Seats.Count, Is.EqualTo(3));
            Assert.That(match.Host.Seats.Seats.All(seat => seat.TeamSide == TeamSide.Blue), Is.True);
            foreach (var seat in match.Host.Seats.Seats)
            {
                var projection = match.Host.ProjectionForSeat(seat.Id);
                Assert.That(projection.Cards.Where(card => !seat.Owns(card.OwnerId)),
                    Has.All.Matches<GuardsDigital.Core.Cards.CardInstance>(card =>
                        match.Encounter.FindEnemy(card.OwnerId) != null));
                Assert.That(projection.Cards.Count(card => match.Encounter.FindEnemy(card.OwnerId) != null),
                    Is.EqualTo(match.Session.Projection.Cards.Count(card => match.Encounter.FindEnemy(card.OwnerId) != null)));
                Assert.That(projection.TeamAgreement, Is.Null);
                Assert.That(projection.IsSeatUndoAvailable, Is.False);
            }
        }

        [Test]
        public void PlacementIsOwnedAndForeignCommitmentsCannotMutateTheMatch()
        {
            var match = Create();
            match.Session.RunAutomatic();
            var seats = match.Host.Seats.Seats;
            var count = 0;
            for (var step = 0; step < 20 && match.Session.Projection.PendingSystemTransitionStep != null; step++)
            {
                var choice = seats.Select(seat => new { seat, command = match.Host.ProjectionForSeat(seat.Id).AuthorizedCommands
                    .FirstOrDefault(command => command.Kind == DirectorCommandKind.AnswerSystemTransitionStep) })
                    .FirstOrDefault(value => value.command != null);
                Assert.That(choice, Is.Not.Null, "A placement must reach its owner's seat.");
                var other = seats.First(seat => !seat.Id.Equals(choice.seat.Id));
                var version = match.Host.Version;
                var refused = match.Host.SubmitForSeat(other.Id, new SeatSubmission(new SubmissionId("foreign-" + step), version, choice.command));
                Assert.That(refused.Accepted, Is.False);
                Assert.That(match.Host.Version, Is.EqualTo(version));
                var accepted = match.Host.SubmitForSeat(choice.seat.Id, new SeatSubmission(new SubmissionId("place-" + step), version, choice.command));
                Assert.That(accepted.Accepted, Is.True, accepted.Detail);
                count++;
                match.Session.RunAutomatic();
            }
            Assert.That(count, Is.EqualTo(3));
            var owned = match.Host.ProjectionForSeat(seats[0].Id).AuthorizedCommands.First(command => command.Kind == DirectorCommandKind.CommitCard);
            Assert.That(match.Host.SubmitForSeat(seats[1].Id,
                new SeatSubmission(new SubmissionId("foreign-card"), match.Host.Version, owned)).Accepted, Is.False);
            var grant = match.Host.ProjectionForSeat(seats[0].Id).AuthorizedCommands
                .First(command => command.Kind == DirectorCommandKind.BeginSystemTransition);
            var granted = match.Host.SubmitForSeat(seats[0].Id,
                new SeatSubmission(new SubmissionId("own-level"), match.Host.Version, grant));
            Assert.That(granted.Accepted, Is.True, granted.Detail);
            Assert.That(PveCoopMatch.IsSharedChoice(match.Session.Projection), Is.False);
            Assert.That(match.Host.ProjectionForSeat(seats[0].Id).AuthorizedCommands, Is.Not.Empty);
            Assert.That(match.Host.ProjectionForSeat(seats[1].Id).AuthorizedCommands, Is.Empty);
            Assert.That(match.Host.ProjectionForSeat(seats[2].Id).AuthorizedCommands, Is.Empty);
        }

        [Test]
        public void CoopIdentitySeparatesPvpSeedsAndEditableContent()
        {
            var match = Create();
            var initial = match.Encounter.InitialState;
            var normal = PrivateSessionIdentity.ForComposition(PveTestContent.OfficialMap(), initial.InitialConfiguration, initial.Setup, "test");
            var coop = PveCoopMatch.Identity(normal, 42, "catalog", "layout");
            Assert.That(coop.MatchFormatId, Is.EqualTo("pve-coop-2-42"));
            var legacy = new PrivateSessionIdentity(coop.ProtocolFamily, coop.ProtocolVersion,
                coop.BuildIdentity, coop.SnapshotSchemaVersion, coop.MapId, coop.MapContentRevision,
                "pve-coop-1-42", coop.ContentDigest);
            Assert.That(coop.Equals(legacy), Is.False, "Preset-plan peers must not join shuffle-plan peers.");
            Assert.That(coop.Equals(normal), Is.False);
            Assert.That(coop.Equals(PveCoopMatch.Identity(normal, 43, "catalog", "layout")), Is.False);
            Assert.That(coop.Equals(PveCoopMatch.Identity(normal, 42, "changed", "layout")), Is.False);
            Assert.That(coop.Equals(PveCoopMatch.Identity(normal, 42, "catalog", "changed")), Is.False);
            Assert.That(coop.Equals(PveCoopMatch.Identity(normal, 42, "catalog", "layout")), Is.True);
        }
    }
}
