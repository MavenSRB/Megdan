using System;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.UIElements;

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

        /// <summary>
        /// Milestone 8.30c's presentation-lifecycle finding. Before the home hero was supplied at
        /// composition, every seat opened whichever hero sorted first in the Director's global wait;
        /// for five seats that was an empty tray rather than their playable private hand.
        /// </summary>
        [Test]
        public void ASeatScopedHandsPresenterOpensOnThatSeatsOwnPlayableHand()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var firstAwaited = host.Projection.Setup.Heroes
                .OrderBy(hero => hero.TeamSide)
                .ThenBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                .First().HeroId;
            var seat = host.Seats.Seats.First(value => !value.HeroId.Equals(firstAwaited));
            var client = new SeatMatchClient(host.ClientForSeat(seat.Id));
            var view = new CapturingHandsView();

            new DirectorHandsPresenter(client, view, seat.HeroId).Refresh();

            Assert.That(view.Model.ViewedHeroId, Is.EqualTo(seat.HeroId));
            Assert.That(view.Model.ViewedHand.HeroId, Is.EqualTo(seat.HeroId));
            Assert.That(view.Model.ViewedHand.Cards, Is.Not.Empty,
                "The seat can see and choose its own opening commitment.");
            Assert.That(view.Model.ViewedHand.Cards,
                Has.All.Matches<CardInstance>(card => card.OwnerId.Equals(seat.HeroId)));
            Assert.That(
                () => new DirectorHandsPresenter(
                    client, new CapturingHandsView(), new HeroId("not-in-this-match")),
                Throws.ArgumentException,
                "A presentation default cannot invent or transfer ownership.");
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
            Assert.That(waitingProjection.PendingAttackTargetDefensePreviews, Is.Empty);
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


        /// <summary>
        /// Milestone 8.30b, the amendment's own surfaces. Both are moved rather than rebuilt, which is
        /// what keeps one derivation behind each of them.
        /// </summary>
        [Test]
        public void TheAmendedSurfacesCarryTheProjectedNarrationAndEndTurnAndAreMovedNotRebuilt()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var client = new SeatMatchClient(host.ClientForSeat(host.Seats.Seats[0].Id));
            var model = RoundActionPresenter.CreateViewModel(client.Projection);
            var view = new RoundActionUnityView();

            view.Render(model);

            Assert.That(view.NarrationRoot.name, Is.EqualTo("gd-narration"));
            Assert.That(Labels(view.NarrationRoot), Does.Contain(model.Status),
                "The narration element carries exactly the sentence the presenter derived.");
            Assert.That(model.Status, Is.Not.Empty,
                "There is no step at which the derivation has nothing to report.");

            // The bar hosts that element; it does not build a second one of its own.
            var bar = new MatchStatusBarUnityView();
            bar.HostNarration(view.NarrationRoot);

            Assert.That(view.NarrationRoot.parent.name, Is.EqualTo("gd-narration-lane"));
            Assert.That(Descendants(bar.Root).Contains(view.NarrationRoot), Is.True);
            Assert.That(Descendants(view.Root).Contains(view.NarrationRoot), Is.False,
                "It left the contextual region rather than being copied out of it.");
            Assert.That(Labels(bar.Root), Does.Contain(model.Status));

            // And it keeps following the projection from its new home.
            var moved = RoundActionPresenter.CreateViewModel(client.Projection);
            view.Render(moved);
            Assert.That(Labels(bar.Root), Does.Contain(moved.Status));
        }

        /// <summary>
        /// Milestone 8.30b. End Turn is the seat's own control in its own surface, offered exactly when
        /// the seat's projection offers it and forwarding exactly what it was given.
        /// </summary>
        [Test]
        public void EndTurnStandsAloneForTheSeatThatMayEndTheTurnAndIsDrawnForNobodyElse()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            MatchSeat acting = null;
            for (var driven = 0; driven < 400 && acting == null; driven += 5)
            {
                OwnerScopedProjectionTests.DriveThroughSeats(host, 5);
                acting = host.Seats.Seats.FirstOrDefault(value =>
                    RoundActionPresenter.CreateViewModel(host.ProjectionForSeat(value.Id))
                        .EndTurnInvocation != null);
            }

            Assert.That(acting, Is.Not.Null,
                "The drive must reach a seat that may end its turn; a skipped case would be a hole " +
                "in this milestone's evidence rather than a pass.");

            var actingModel = RoundActionPresenter.CreateViewModel(host.ProjectionForSeat(acting.Id));
            var actingView = new RoundActionUnityView();
            actingView.Render(actingModel);

            var control = actingView.EndTurnRoot.Q<Button>("gd-end-turn-control");
            Assert.That(control, Is.Not.Null);
            Assert.That(control.text, Is.EqualTo(actingModel.EndTurnInvocation.Label));
            Assert.That(actingView.EndTurnRoot.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(Descendants(actingView.Root)
                    .OfType<Button>()
                    .Count(button => button.text == actingModel.EndTurnInvocation.Label),
                Is.EqualTo(1),
                "It is drawn once. The action row it used to sit in must not still be drawing one.");

            // The control's own action, taken the way every other Edit Mode case in this project takes
            // one: an element with no live panel dispatches no pointer event, so the forwarding path
            // is exercised directly and the Play Mode fixture presses the button for real.
            TypedInvocationViewModel forwarded = null;
            actingView.InvocationSubmitted += invocation => forwarded = invocation;
            actingView.ForwardInvocation(actingModel.EndTurnInvocation);

            Assert.That(forwarded, Is.SameAs(actingModel.EndTurnInvocation),
                "It forwards the projected invocation without composing a command of its own.");

            foreach (var other in host.Seats.Seats.Where(value => !value.Id.Equals(acting.Id)))
            {
                var otherModel = RoundActionPresenter.CreateViewModel(host.ProjectionForSeat(other.Id));
                if (otherModel.EndTurnInvocation != null)
                {
                    continue;
                }

                var otherView = new RoundActionUnityView();
                otherView.Render(otherModel);
                Assert.That(otherView.EndTurnRoot.style.display.value,
                    Is.EqualTo(DisplayStyle.None),
                    "A seat the projection offers no End Turn draws no control in the corner - it is " +
                    "absent rather than standing and refusing.");
                Assert.That(otherView.EndTurnRoot.childCount, Is.Zero);
            }
        }

        /// <summary>
        /// Milestone 8.30b, and this is the behaviour behind the closing check.
        ///
        /// <para>The Minion Battle is one of the two declared pending families the status derivation
        /// had never named. It stands at the end of a round, so the sentence a player read was the
        /// round's own phase line while the match was in fact waiting for them to choose which
        /// minions leave the board. Now that the sentence is the top status bar's standing report,
        /// that is the difference between a bar that says what is happening and one that does not.
        /// </para>
        /// </summary>
        [Test]
        public void TheNarrationNamesAStandingMinionBattleRatherThanTheRoundItStandsIn()
        {
            var session = MinionChoicePresentationTests.CreatePendingRemovalSession();
            var battle = session.Projection.PendingMinionBattleChoice;

            Assert.That(battle, Is.Not.Null, "The fixture stands on the family this case is about.");

            var status = RoundActionPresenter.CreateViewModel(session.Projection).Status;

            Assert.That(status, Does.StartWith("Minion Battle:"));
            Assert.That(status, Does.Contain(MinionChoiceLabels.TeamLabel(battle.ChoosingSide)),
                "The side that decides is named, which is what a team-owned pause has to say.");
            Assert.That(status, Does.Not.Contain("complete"),
                "and never the round's own phase line, which is what this pause used to arrive under.");
        }

        /// <summary>
        /// Milestone 8.30c's demonstrated HUD/prompt defect. The terminal projection already offered
        /// no controls, but its narration fell through to "Waiting for the match to continue". An
        /// ended match now reads as ended, identically and safely from all six seat perspectives.
        /// </summary>
        [Test]
        public void ACompletedMatchNamesTheWinnerAndVictoryPathAndOffersNoSeatAControl()
        {
            var session = ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession();
            var record = DeterministicMatchLoop.Play(session, seed: 1, maximumCommands: 1200);
            Assert.That(record.Outcome.HasEnded, Is.True,
                "The recorded local scenario must reach its terminal projection.");
            var host = new MatchHost(session);
            var statuses = new System.Collections.Generic.List<string>();

            foreach (var seat in host.Seats.Seats)
            {
                var client = new SeatMatchClient(host.ClientForSeat(seat.Id));
                var model = RoundActionPresenter.CreateViewModel(client.Projection);
                statuses.Add(model.Status);

                Assert.That(model.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded));
                Assert.That(model.Status,
                    Is.EqualTo("[R] RED wins: the enemy Throne was captured."));
                Assert.That(model.Status, Does.Not.Contain("Waiting"));
                Assert.That(model.Actions, Is.Empty);
                Assert.That(model.EndTurnInvocation, Is.Null);
                Assert.That(model.ContinueEndOfRoundInvocation, Is.Null);
                Assert.That(model.InitiativeChoices, Is.Empty);
                Assert.That(model.DefenseChoices, Is.Empty);
                Assert.That(model.FastTravelOffers, Is.Empty);
                Assert.That(model.EffectChoices, Is.Empty);
                Assert.That(model.EffectChoiceTargets, Is.Empty);
                Assert.That(model.EffectChoiceDestinations, Is.Empty);
                Assert.That(model.HeroRespawnOptions, Is.Empty);
                Assert.That(model.ResolveWithoutRespawn, Is.Null);
                Assert.That(model.ClearOffers, Is.Empty);
                Assert.That(model.ClearSelection, Is.Null);
                Assert.That(model.SystemTransitionPrompt, Is.Null);
                Assert.That(model.EffectChoicePrompt, Is.Null);
                Assert.That(model.RemovalPreventionPrompt, Is.Null);

                var hands = new CapturingHandsView();
                new DirectorHandsPresenter(client, hands).Refresh();
                Assert.That(hands.Model.Hands
                        .Where(hand => !hand.HeroId.Equals(seat.HeroId))
                        .SelectMany(hand => hand.Cards),
                    Is.Empty,
                    "The completed-state tray still cannot name another seat's private hand.");
            }

            Assert.That(statuses.Distinct().Count(), Is.EqualTo(1),
                "The public completed-match result is identical from all six seats.");
        }

        private static System.Collections.Generic.IEnumerable<VisualElement> Descendants(
            VisualElement element)
        {
            foreach (var child in element.Children())
            {
                yield return child;
                foreach (var nested in Descendants(child))
                {
                    yield return nested;
                }
            }
        }

        private static string[] Labels(VisualElement element) =>
            Descendants(element).OfType<Label>().Select(label => label.text).ToArray();

        private sealed class CapturingHandsView : IDirectorHandsView
        {
            public DirectorHandsViewModel Model { get; private set; }

            public void Render(DirectorHandsViewModel model) => Model = model;
        }
    }
}
