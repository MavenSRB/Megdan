using System;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.30b. The proven HUD, driven by one seat's authorized projection.
    /// </summary>
    /// <remarks>
    /// The presenters are the surface under test rather than a composed scene: they are what turns a
    /// projection into what a player sees, so scoping them is what scopes the HUD. What a scene then
    /// does with the result is this milestone's Play Mode fixture and 8.30c's gate.
    /// </remarks>
    public sealed class SeatScopedMatchHudTests
    {
        [Test]
        public void ASeatsSurfaceDrawsItsOwnCardsAndCannotNameAnothersHandOrDeck()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var seat = host.Seats.Seats[0];
            var other = host.Seats.Seats.First(value => !value.Id.Equals(seat.Id));
            var client = new SeatMatchClient(host.ClientForSeat(seat.Id));

            var visible = client.Projection.Cards.Select(card => card.Id.Value).ToArray();

            Assert.That(client.Projection.Cards.Any(card => card.OwnerId.Equals(seat.HeroId)), Is.True,
                "The seat's own cards are what its hand surface draws.");
            foreach (var hidden in host.Projection.Cards.Where(card =>
                card.OwnerId.Equals(other.HeroId) &&
                (card.State.Location == CardLocation.Hand || card.State.Location == CardLocation.Deck)))
            {
                Assert.That(visible, Does.Not.Contain(hidden.Id.Value),
                    "No surface can draw a card the read model does not carry.");
            }
        }

        [Test]
        public void TheHandsSurfaceOffersACommitmentOnlyToTheSeatBeingAsked()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var seat = host.Seats.Seats[0];
            var client = new SeatMatchClient(host.ClientForSeat(seat.Id));
            var view = new CapturingHandsView();
            var presenter = new DirectorHandsPresenter(client, view);

            presenter.Refresh();

            Assert.That(view.Model, Is.Not.Null);
            var offered = client.SeatProjection.AuthorizedCommands
                .Where(command => command.Kind == DirectorCommandKind.CommitCard)
                .ToArray();
            Assert.That(offered, Is.Not.Empty,
                "Rulebook p.7 asks every seat at once, so this one has its own commitment to make.");
            Assert.That(offered.All(command => command.HeroId.Equals(seat.HeroId)), Is.True,
                "Every commitment this surface can forward is this seat's own.");

            var accepted = client.Submit(offered[0]);

            Assert.That(accepted.Accepted, Is.True, accepted.Detail);
            Assert.That(host.Version, Is.EqualTo(1),
                "One surface submission is one authorized state advance.");
        }

        [Test]
        public void AWaitingSeatIsOfferedNothingAndIsToldWhoIsBeingWaitedOn()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            MatchSeat deciding = null;
            for (var driven = 0; driven < 300 && deciding == null; driven += 5)
            {
                OwnerScopedProjectionTests.DriveThroughSeats(host, 5);
                deciding = host.Seats.Seats.FirstOrDefault(value =>
                    host.ProjectionForSeat(value.Id).DecisionContract != null);
            }

            Assert.That(deciding, Is.Not.Null,
                "The drive must reach a standing decision for the waiting state to be observable; a " +
                "skipped case would be a hole in this milestone's evidence rather than a pass.");

            var waiting = host.Seats.Seats.First(value =>
                host.ProjectionForSeat(value.Id).DecisionContract == null);
            var waitingProjection = host.ProjectionForSeat(waiting.Id);

            Assert.That(waitingProjection.AuthorizedCommands, Is.Empty,
                "A seat with nothing to answer is offered nothing to press.");
            Assert.That(waitingProjection.IsWaitingForAnotherSeat, Is.True);
            Assert.That(waitingProjection.DecisionOwner.IsDefined, Is.True,
                "Who is being waited on is public, which is what a waiting surface has to show.");
            Assert.That(waitingProjection.PendingMovementChoice, Is.Null);
            Assert.That(waitingProjection.PendingAttackTargetChoice, Is.Null);
            Assert.That(waitingProjection.PendingEffectChoice, Is.Null);
            Assert.That(waitingProjection.PendingDefenseChoice, Is.Null);
            Assert.That(waitingProjection.AvailableTurnActions, Is.Empty);
            Assert.That(waitingProjection.AvailableHoldOffer, Is.Null);
        }

        /// <summary>
        /// Milestone 8.30b, check three's stale-view and rejected-input half. A surface never decides
        /// its own legality: it forwards the invocation it was given, and the client always states the
        /// version it last read, so the host and not the HUD decides whether the answer still applies.
        /// </summary>
        [Test]
        public void AStaleControlIsRefusedBeforeTheRulesAndAnOrdinaryAnswerStillApplies()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var first = host.Seats.Seats[0];
            var second = host.Seats.Seats[1];
            var firstClient = new SeatMatchClient(host.ClientForSeat(first.Id));
            var secondClient = new SeatMatchClient(host.ClientForSeat(second.Id));
            var held = secondClient.SeatProjection.AuthorizedCommands.First();

            // The board moves under the second seat's held view.
            Assert.That(firstClient.Submit(firstClient.SeatProjection.AuthorizedCommands.First()).Accepted,
                Is.True);

            var version = host.Version;
            var applied = secondClient.Submit(held);

            Assert.That(applied.Accepted, Is.True, applied.Detail);
            Assert.That(applied.WasRefusedBeforeTheRules, Is.False,
                "An accepted submission was never refused at all.");
            Assert.That(host.Version, Is.EqualTo(version + 1),
                "The client re-read its own version, so an ordinary answer still applies.");

            // The same control pressed twice: the offer it came from is gone, so the second press is
            // refused as an authorization failure rather than reported to the player as a rule.
            var expired = secondClient.Submit(held);

            Assert.That(expired.Accepted, Is.False);
            Assert.That(expired.WasRefusedBeforeTheRules, Is.True,
                "A control left over from a projection that has moved on is a rejected input, which " +
                "a surface must be able to say without claiming a rule refused it.");
            Assert.That(host.Version, Is.EqualTo(version + 1),
                "And it moved nothing.");
        }

        [Test]
        public void TheDirectorsOwnControlsAreAbsentFromASeatScopedComposition()
        {
            var runtime = typeof(ConfiguredMatchSceneRuntime);

            Assert.That(runtime.GetProperty(nameof(ConfiguredMatchSceneRuntime.Seat)), Is.Not.Null,
                "A composed runtime says which seat it is for, or that it is the Director's.");
            Assert.That(runtime.GetConstructors().Single().GetParameters()
                    .Any(parameter => parameter.ParameterType == typeof(SeatId?)),
                Is.True, "The seat is chosen at composition and never afterwards.");

            var coordinator = typeof(MatchFeatureCoordinator).GetConstructors().Single();
            Assert.That(coordinator.GetParameters()
                    .Count(parameter => parameter.ParameterType == typeof(UndoRestartPresenter) ||
                        parameter.ParameterType == typeof(DiagnosticLogPresenter)),
                Is.EqualTo(2),
                "The Director-only pair is still named by the coordinator, and is now optional so a " +
                "seat's HUD composes neither rather than composing and hiding them.");

            Assert.That(typeof(UndoRestartPresenter).GetConstructors()
                    .All(value => value.GetParameters()[0].ParameterType == typeof(IMatchSession)),
                Is.True,
                "Undo takes the session, not the read model, so it cannot be handed a seat client.");
            Assert.That(typeof(DiagnosticLogPresenter).GetConstructors()
                    .All(value => value.GetParameters()[0].ParameterType == typeof(IMatchSession)),
                Is.True);
        }

        [Test]
        public void TheDevelopmentSeatSwitcherSelectsAndCannotMutateOwnership()
        {
            var property = typeof(MatchSceneCompositionRoot).GetProperty(
                nameof(MatchSceneCompositionRoot.NextComposedSeat),
                BindingFlags.Public | BindingFlags.Static);

            Assert.That(property, Is.Not.Null,
                "The switcher is a development affordance on the composition root, like the one-shot " +
                "scene content beside it.");
            Assert.That(property.PropertyType, Is.EqualTo(typeof(SeatId?)),
                "It names an existing seat and carries nothing else - no hero, no team, no player.");
            Assert.That(property.GetSetMethod().IsStatic, Is.True);

            var previous = MatchSceneCompositionRoot.NextComposedSeat;
            try
            {
                MatchSceneCompositionRoot.NextComposedSeat = new SeatId("red-player-01");
                Assert.That(MatchSceneCompositionRoot.NextComposedSeat.HasValue, Is.True);
            }
            finally
            {
                MatchSceneCompositionRoot.NextComposedSeat = previous;
            }

            // Selecting a seat that is not bound is a refusal, not a composition with a made-up owner.
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            Assert.That(() => host.ClientForSeat(new SeatId("not-a-seat")), Throws.ArgumentException);
        }

        [Test]
        public void TheSameSurfaceRendersTheDirectorsViewAndASeatsWithoutKnowingWhich()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var directorView = new CapturingHandsView();
            var seatView = new CapturingHandsView();

            new DirectorHandsPresenter(
                new DirectorMatchClient(new LocalDirectorMatchClient(host)), directorView).Refresh();
            new DirectorHandsPresenter(
                new SeatMatchClient(host.ClientForSeat(host.Seats.Seats[0].Id)), seatView).Refresh();

            Assert.That(directorView.Model, Is.Not.Null);
            Assert.That(seatView.Model, Is.Not.Null,
                "One presenter, two read models, and it never asks which it was given.");
            Assert.That(typeof(DirectorHandsPresenter).GetConstructors()
                    .Any(value => value.GetParameters()[0].ParameterType == typeof(IMatchClient)),
                Is.True);
        }

        private sealed class CapturingHandsView : IDirectorHandsView
        {
            public DirectorHandsViewModel Model { get; private set; }

            public void Render(DirectorHandsViewModel model) => Model = model;
        }
    }
}
