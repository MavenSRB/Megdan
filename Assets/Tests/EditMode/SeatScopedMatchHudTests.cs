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
                    RoundActionPresenter.CreateViewModel(
                            host.ProjectionForSeat(value.Id), value.HeroId)
                        .EndTurnInvocation != null);
            }

            Assert.That(acting, Is.Not.Null,
                "The drive must reach a seat that may end its turn; a skipped case would be a hole " +
                "in this milestone's evidence rather than a pass.");

            var actingModel = RoundActionPresenter.CreateViewModel(
                host.ProjectionForSeat(acting.Id), acting.HeroId);
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
                var otherModel = RoundActionPresenter.CreateViewModel(
                    host.ProjectionForSeat(other.Id), other.HeroId);
                if (otherModel.EndTurnInvocation != null)
                {
                    continue;
                }

                // Milestone 8.35v: END TURN is now drawn (disabled, never hidden) for a seat that
                // IS the acting hero but has nothing left it can complete or cancel. A seat that is
                // not the acting hero at all - every "other" seat this drive reaches - still draws
                // nothing, because `IsViewerTheActingHero` gates both new facts on the exact viewer,
                // not merely on some turn being active.
                Assert.That(otherModel.EndTurnCancelsOpenAttack, Is.False,
                    "A seat that is not the acting hero has no open Attack selection of its own " +
                    "to cancel.");
                Assert.That(otherModel.EndTurnUnavailableReason, Is.Null,
                    "A seat that is not the acting hero draws no disabled End Turn either - it is " +
                    "not that seat's control to refuse.");

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
        /// Milestone 8.35f. The seat's own UNDO square: drawn in its own surface for the one seat
        /// the host published a rewind to, drawn for nobody else, and carrying no invocation at all.
        /// </summary>
        /// <remarks>
        /// The absence of an invocation is the structural half. End Turn forwards a projected
        /// command; a rewind is not a command, so this control raises a bare event and the presenter
        /// asks the client. A view that could compose an undo could compose one for another seat.
        /// </remarks>
        [Test]
        public void TheSeatUndoSquareIsDrawnForTheSeatThatMayRewindAndForNobodyElse()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            MatchSeat acting = null;
            for (var driven = 0; driven < 400 && acting == null; driven += 5)
            {
                OwnerScopedProjectionTests.DriveThroughSeats(host, 5);
                acting = host.Seats.Seats.FirstOrDefault(value =>
                    host.ProjectionForSeat(value.Id).IsSeatUndoAvailable);
            }

            Assert.That(acting, Is.Not.Null,
                "The drive must reach a seat the host published a rewind to; a skipped case would " +
                "be a hole in this milestone's evidence rather than a pass.");

            var actingModel = RoundActionPresenter.CreateViewModel(host.ProjectionForSeat(acting.Id));
            Assert.That(actingModel.SeatUndoAvailable, Is.True);
            var actingView = new RoundActionUnityView();
            actingView.Render(actingModel);

            var control = actingView.SeatUndoRoot.Q<Button>("gd-seat-undo-control");
            Assert.That(control, Is.Not.Null);
            Assert.That(actingView.SeatUndoRoot.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(control.text, Is.EqualTo(RoundActionUnityView.SeatUndoGlyph),
                "It says the glyph and nothing else - the square has no room for lettering.");
            Assert.That(control.text, Does.Not.Contain("UNDO"),
                "No words, by the user's own placement decision.");
            Assert.That(control.style.minHeight.value.value,
                Is.EqualTo(ConfiguredMatchHudRenderer.SeatUndoControlSide));

            // Taken the way every other Edit Mode case here takes a control's action: an element
            // with no live panel dispatches no pointer event, so the raising path is exercised
            // directly and the seat-level fixtures drive the rest of it.
            var pressed = 0;
            actingView.SeatUndoRequested += () => pressed++;
            actingView.RequestSeatUndo();

            Assert.That(pressed, Is.EqualTo(1),
                "The press raises a bare request; it composes no command of its own.");
            Assert.That(Descendants(actingView.Root).OfType<Button>()
                    .Any(button => button.text != null &&
                        button.text.IndexOf("UNDO", StringComparison.OrdinalIgnoreCase) >= 0),
                Is.False,
                "Nothing in a seat's surface spells the word: the Director's drawer owns that one.");

            foreach (var other in host.Seats.Seats.Where(value => !value.Id.Equals(acting.Id)))
            {
                var otherProjection = host.ProjectionForSeat(other.Id);
                Assert.That(otherProjection.IsSeatUndoAvailable, Is.False,
                    "Only the seat resolving its own turn is published a rewind.");
                var otherView = new RoundActionUnityView();
                otherView.Render(RoundActionPresenter.CreateViewModel(otherProjection));
                Assert.That(otherView.SeatUndoRoot.style.display.value,
                    Is.EqualTo(DisplayStyle.None),
                    "A seat with no rewind draws no square - absent rather than standing and inert.");
                Assert.That(otherView.SeatUndoRoot.childCount, Is.Zero);
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

            var model = RoundActionPresenter.CreateViewModel(session.Projection);
            var status = model.Status;

            // Milestone 8.35n: the own-decision family header leads ("YOUR MINION BATTLE — "),
            // and the deciding side moved to the demoted detail beside it.
            Assert.That(status, Does.StartWith("YOUR MINION BATTLE —"));
            Assert.That(model.StatusDetail, Does.Contain(MinionChoiceLabels.TeamLabel(battle.ChoosingSide)),
                "The side that decides is named, which is what a team-owned pause has to say.");
            Assert.That(status, Does.Not.Contain("complete"),
                "and never the round's own phase line, which is what this pause used to arrive under.");
        }

        /// <summary>
        /// Milestone 8.39j. EVERY CAUSE HAS A SENTENCE THE HUD CAN SAY, and this is a derived guard
        /// rather than one member's case because the failure it closes was exactly a member added
        /// without one.
        ///
        /// <para><b>What happened.</b> Milestone 8.39g added <c>CoinHoardClaimed</c> and taught
        /// <c>RoundActionPresenter.FormatStatus</c> to name a solo winner, and left the switch that
        /// turns a cause into words at p.8's three team sentences. So the instant A Fistful of Coins
        /// ended a match the round panel threw <c>An ended match requires one rulebook victory
        /// cause</c>, the HUD stopped refreshing, and the win was never announced to anybody - found
        /// at milestone 8.39j by a Play Mode journey that drove the card to its own ending, which no
        /// offline tier could have done because no offline tier draws a panel.</para>
        ///
        /// <para>The formatter is private, deliberately: what it returns is presentation and only the
        /// status line consumes it. Reflecting on it here is what lets the guard walk the ENUM rather
        /// than a list somebody has to remember to extend.</para>
        /// </summary>
        [Test]
        public void EveryVictoryCause_HasAWordedSentenceTheStatusLineCanSay()
        {
            var formatter = typeof(RoundActionPresenter)
                .GetMethod(
                    "FormatVictoryCause",
                    BindingFlags.NonPublic |
                    BindingFlags.Static);
            Assert.That(formatter, Is.Not.Null,
                "The status line's own cause formatter is what this guard walks; a rename belongs " +
                "here rather than in a panel that throws at the moment somebody wins.");

            var unworded = Enum.GetValues(typeof(GuardsDigital.Core.Rules.Victory.MatchVictoryCause))
                .Cast<GuardsDigital.Core.Rules.Victory.MatchVictoryCause>()
                .Where(cause => cause != GuardsDigital.Core.Rules.Victory.MatchVictoryCause.None)
                .Where(cause =>
                {
                    try
                    {
                        return string.IsNullOrWhiteSpace(
                            (string)formatter.Invoke(null, new object[] { cause }));
                    }
                    catch (Exception)
                    {
                        return true;
                    }
                })
                .Select(cause => cause.ToString())
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(unworded, Is.Empty,
                "A cause the HUD cannot word is a match that ends with a thrown panel instead of a " +
                "winner: " + string.Join(", ", unworded));
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
                new DirectorHandsPresenter(client, hands, seat.HeroId).Refresh();
                var projectedOwnHand = client.Projection.Cards
                    .Where(card => card.OwnerId.Equals(seat.HeroId) &&
                        card.State.Location == CardLocation.Hand)
                    .Select(card => card.Id)
                    .OrderBy(id => id.Value)
                    .ToArray();
                var renderedOwnHand = hands.Model.Hands
                    .Where(hand => hand.HeroId.Equals(seat.HeroId))
                    .SelectMany(hand => hand.Cards)
                    .Select(card => card.Id)
                    .OrderBy(id => id.Value)
                    .ToArray();
                CollectionAssert.AreEqual(projectedOwnHand, renderedOwnHand);
                Assert.That(hands.Model.Hands.All(hand => hand.HeroId.Equals(seat.HeroId)), Is.True,
                    "Even after the match, a player tray shows only its owner, not public foreign cards.");
            }

            Assert.That(statuses.Distinct().Count(), Is.EqualTo(1),
                "The public completed-match result is identical from all six seats.");
        }

        /// <summary>
        /// Milestone 8.35e, at the element level. The Minion Battle's public stage is drawn for a
        /// seat that owns none of it: the prompt, the counts and the choosing side's public standing,
        /// and not one control. The view needed no change for this - milestone 8.11a already moved
        /// every candidate onto the board - so what this pins is that the stage reaches the element
        /// tree at all, which is what the ArgumentException prevented.
        /// </summary>
        [Test]
        public void TheMinionBattlesPublicStageIsDrawnForAnObservingSeatWithNoControls()
        {
            var host = TeamAgreementTests.ReachAgreement(TeamAgreementKind.MinionBattle);
            var choosingSide = host.Projection.PendingMinionBattleChoice.ChoosingSide;
            var observer = host.Seats.Seats.First(seat => !seat.PlaysFor(choosingSide));
            var client = new SeatMatchClient(host.ClientForSeat(observer.Id));
            var view = new MinionChoiceUnityView();

            new MinionChoicePresenter(client, view).Refresh();

            Assert.That(view.Model, Is.Not.Null,
                "The battle is public; the side that is not choosing reads it and answers nothing.");
            Assert.That(view.Model.IsPublicAgreementStage, Is.True);
            Assert.That(view.Root.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(Labels(view.Root), Does.Contain(view.Model.PromptLabel));
            Assert.That(Labels(view.Root), Does.Contain(view.Model.StatusLabel));
            Assert.That(Labels(view.Root), Does.Contain(view.Model.SectionLabel));
            Assert.That(Descendants(view.Root).OfType<Button>().Any(), Is.False,
                "An observing seat is drawn the stage and no control of any kind - no confirm, no " +
                "agreement statement, and no candidate.");
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

        /// <summary>
        /// Milestone 8.35d, and this is the control the live playtest read as a stuck game.
        ///
        /// <para>The continuation control was drawn from the phase and the two pauses the projection
        /// reported - and a seat's scoped surface reports the standing decision it does <em>not</em>
        /// own as absent, by design. So while one player answered an end-of-round question, the
        /// other five were each shown CONTINUE END OF ROUND, and every press of it was refused as
        /// an unauthorized command. A seat-served round is now continued by its host, and no seat
        /// is drawn a control it cannot submit.</para>
        /// </summary>
        [Test]
        public void ASeatWaitingOnAnothersEndOfRoundDecisionIsDrawnNoContinueControl()
        {
            // Milestone 8.35u. The stop condition gained its third clause, and the reason is this
            // milestone's own behaviour: rulebook p.15 now asks every hero that bought a level at
            // once, and p.8's pity coin makes "everybody levels" the ordinary round rather than a
            // rare one - so an end-of-round decision can be owned by ALL SIX seats and leave no
            // waiting seat to draw. This case is about a seat waiting on somebody else's answer, so
            // it must stop where one exists; without the clause it stopped on a decision nobody was
            // waiting for and threw "Sequence contains no matching element" from the line below.
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var walked = OwnerScopedProjectionTests.DriveThroughSeats(host, 2000,
                projection => projection.Phase == RoundResolutionPhase.EndOfRound &&
                    projection.DecisionContract != null &&
                    host.Seats.Seats.Any(seat => host.ProjectionForSeat(seat.Id)
                        .IsWaitingForAnotherSeat));

            Assert.That(host.Projection.DecisionContract, Is.Not.Null,
                "The seeded seat walk reached no end-of-round decision in " + walked + " commands.");
            var owner = SeatProjectionFactory.OwnerOfStandingDecision(host.Projection);
            var waiting = host.Seats.Seats.FirstOrDefault(seat => !owner.IsOwnedBy(seat));
            Assert.That(waiting, Is.Not.Null,
                "The walk stopped on an end-of-round decision every seat owns, so no seat is " +
                "waiting on another's and this case would prove nothing.");
            var client = new SeatMatchClient(host.ClientForSeat(waiting.Id));

            Assert.That(client.SeatProjection.AuthorizedCommands, Is.Empty,
                "Seat " + waiting.Id + " holds no answer, which is what makes any control it is " +
                "drawn here a control it cannot press.");

            var view = new RoundActionUnityView();
            new RoundActionPresenter(client, view).Refresh();

            Assert.That(view.Model.ContinueEndOfRoundInvocation, Is.Null,
                "A seat is drawn no end-of-round continuation, in this state or any other.");
        }

        /// <summary>
        /// Milestone 8.35m (SEVENTH amendment, card scope) - the privacy case. Rulebook p.7 asks
        /// every seat for a commitment at once, so another hero's OWN screen shows their hand
        /// shining gold right now; this seat's projection never carries that hand at all, so there
        /// is nothing here for a card-scope shine to leak onto.
        /// </summary>
        [Test]
        public void AnotherSeatsAwaitedHand_NeverShinesOnThisViewersScreen()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var seat = host.Seats.Seats[0];
            var other = host.Seats.Seats.First(value => !value.Id.Equals(seat.Id));
            var client = new SeatMatchClient(host.ClientForSeat(seat.Id));

            Assert.That(client.Projection.Cards.Any(card =>
                card.OwnerId.Equals(other.HeroId) && card.State.Location == CardLocation.Hand),
                Is.False, "This seat's own projection withholds another hero's hand entirely.");

            var view = new CapturingHandsView();
            var presenter = new DirectorHandsPresenter(client, view, seat.HeroId);
            presenter.Refresh();
            Assert.That(presenter.SelectViewedHero(other.HeroId), Is.False);
            Assert.That(view.Model.ViewedHand.HeroId, Is.EqualTo(seat.HeroId));
            Assert.That(view.Model.Hands.SelectMany(hand => hand.TrayCards)
                .All(card => client.Projection.Cards.Single(value => value.Id.Equals(card.CardInstanceId))
                    .OwnerId.Equals(seat.HeroId)), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PlayerTrayRejectsEveryForeignHero_EvenWhenTheirCardsArePublic(bool revealed)
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            if (revealed)
                OwnerScopedProjectionTests.DriveThroughSeats(host, 100,
                    projection => projection.Phase != RoundResolutionPhase.AwaitingCommitments);

            foreach (var seat in host.Seats.Seats)
            {
                var client = new SeatMatchClient(host.ClientForSeat(seat.Id));
                if (revealed)
                    Assert.That(client.Projection.Cards.Any(card =>
                        !card.OwnerId.Equals(seat.HeroId) && card.State.Location == CardLocation.TurnArea),
                        Is.True, "Exercise visible foreign cards, not only hidden hands.");
                var view = new CapturingHandsView();
                var presenter = new DirectorHandsPresenter(client, view, seat.HeroId);
                presenter.Refresh();
                var version = host.Version;
                foreach (var other in host.Seats.Seats.Where(value => !value.Id.Equals(seat.Id)))
                {
                    Assert.That(presenter.SelectViewedHero(other.HeroId), Is.False);
                    presenter.Refresh();
                    Assert.That(presenter.ViewedHeroId, Is.EqualTo(seat.HeroId));
                    Assert.That(view.Model.ViewedHand.HeroId, Is.EqualTo(seat.HeroId));
                    Assert.That(view.Model.ViewedHand.TrayCards, Is.Not.Empty);
                    Assert.That(view.Model.Hands.Select(hand => hand.HeroId),
                        Is.EqualTo(new[] { seat.HeroId }));
                }
                Assert.That(presenter.SelectViewedHero(seat.HeroId), Is.True);
                Assert.That(host.Version, Is.EqualTo(version), "Navigation submits no command.");
            }
        }

        [Test]
        public void AnUnboundPlayerTrayShowsNoFallbackHero_UntilItsSeatIsBound()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var seat = host.Seats.Seats[0];
            var view = new CapturingHandsView();
            var presenter = new DirectorHandsPresenter(new SeatMatchClient(host.ClientForSeat(seat.Id)), view);
            presenter.Refresh();
            Assert.That(view.Model.ViewedHand, Is.Null);
            Assert.That(presenter.SelectViewedHero(seat.HeroId), Is.False);
            Assert.That(presenter.SetHomeHero(seat.HeroId), Is.True);
            Assert.That(view.Model.ViewedHand.HeroId, Is.EqualTo(seat.HeroId));
        }

        /// <summary>
        /// The Director's own direct surface is untouched by that removal: it still draws the
        /// manual control, because it is still the surface that steps the round by hand.
        /// </summary>
        [Test]
        public void TheDirectorsSurfaceStillDrawsItsOwnContinueControl()
        {
            var session = ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(out _);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerSystemTransitionStep(
                session.Projection.PendingSystemTransitionStep.Invocations[0]));

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
            Assert.That(session.Projection.DecisionContract, Is.Null);

            var view = new RoundActionUnityView();
            new RoundActionPresenter(session, view).Refresh();

            Assert.That(view.Model.ContinueEndOfRoundInvocation, Is.Not.Null);
            Assert.That(view.Model.ContinueEndOfRoundInvocation.Label,
                Is.EqualTo("CONTINUE END OF ROUND"));
        }

        private sealed class CapturingHandsView : IDirectorHandsView
        {
            public DirectorHandsViewModel Model { get; private set; }

            public void Render(DirectorHandsViewModel model) => Model = model;
        }
    }
}
