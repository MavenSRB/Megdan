using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35e. The two presentation crashes the 2026-09-03 invited playtest recorded, and
    /// the states they happen in.
    ///
    /// <para>The first is the Minion Battle's public stage: a reader that carries milestone 8.30n's
    /// team agreement and no owned <c>PendingMinionBattleChoice</c>. Two readers reach it - a seat
    /// on the side that is not choosing, which never holds the detail, and a seat on the choosing
    /// side while its own accepted press is in flight, because a network view that is not actionable
    /// is stripped of every pending answer and keeps the public agreement. The branch that exists to
    /// draw that stage built the removal prompt with default end-of-round identities, and that
    /// constructor's own guard refuses exactly those, so it threw on every render and took the rest
    /// of <see cref="MatchFeatureCoordinator.RefreshAll"/> with it.</para>
    ///
    /// <para>The second is the tray: rulebook p.8's Retrieve stage puts each hero's revealed card
    /// back in that hero's hand, and a seat's scoped card list withholds another hero's hand, so
    /// from that stage until the next round every seat holds five commitments naming a card its own
    /// projection may not carry. <c>Single</c> refused that absence and failed the whole refresh.</para>
    ///
    /// <para>Unity-free on purpose - every view here is a capturing double - so the fixture states
    /// presenter behaviour rather than element layout.</para>
    /// </summary>
    public sealed class MinionBattleAgreementRenderingTests
    {
        /// <summary>
        /// The non-choosing side, at every version of the battle from the first selection through
        /// the unanimous agreement to the applied removal. It owns none of it, so every one of those
        /// versions is a public-stage render, and before this milestone every one of them threw.
        /// </summary>
        [Test]
        public void ANonChoosingSeatRendersEveryVersionOfTheBattleReadOnlyWithoutAnException()
        {
            var host = TeamAgreementTests.ReachAgreement(TeamAgreementKind.MinionBattle);
            var choosingSide = host.Projection.PendingMinionBattleChoice.ChoosingSide;
            var observer = host.Seats.Seats.First(seat => !seat.PlaysFor(choosingSide));
            var surface = SeatSurface.ForLocalSeat(host, observer);
            var renders = 0;

            void Observe()
            {
                surface.RefreshAll();
                Assert.That(surface.Exceptions, Is.Empty, Describe(surface.Exceptions));
                var model = surface.PromptModel;
                Assert.That(model, Is.Not.Null,
                    "The battle is public, so the seat that is not choosing still reads it.");
                Assert.That(model.IsPublicAgreementStage, Is.True);
                Assert.That(model.ResolutionId.IsDefined, Is.False,
                    "A reader with no pending detail names no end-of-round identity.");
                Assert.That(model.ChoiceId.IsDefined, Is.False);
                Assert.That(model.Candidates, Is.Empty);
                Assert.That(model.ConfirmInvocation, Is.Null);
                Assert.That(model.Agreement, Is.Not.Null);
                Assert.That(model.Agreement.Kind, Is.EqualTo(TeamAgreementKind.MinionBattle));
                Assert.That(model.Agreement.Choices, Is.Empty,
                    "The other side states nothing about this team's removal.");
                Assert.That(model.ChoosingSide, Is.EqualTo(choosingSide));
                Assert.That(model.StatusLabel, Is.EqualTo(
                    "SELECTED " + model.SelectedCount + " OF " + model.RequiredRemovalCount));
                renders++;
            }

            Observe();
            TeamAgreementTests.CompleteMinionSelection(host, "observer-select");
            Observe();
            AgreeUnanimously(host, "observer-agree", Observe);

            Assert.That(host.Projection.PendingMinionBattleChoice, Is.Null,
                "The unanimous agreement submitted the confirm and the removal was applied.");
            Assert.That(renders, Is.GreaterThan(2),
                "Only " + renders + " public-stage renders were observed, which is too few to " +
                "cover selection, agreement and confirmation.");

            // And afterwards the prompt is gone rather than stuck on the battle that ended.
            surface.RefreshAll();
            Assert.That(surface.Exceptions, Is.Empty, Describe(surface.Exceptions));
            Assert.That(surface.PromptModel, Is.Null);
        }

        /// <summary>
        /// The choosing side's own public stage, which is the state its accepted press lands in: the
        /// counts, the selected units the board cues, its own agreement statements, and no
        /// fabricated identities. Then the statement it holds there is accepted by the host.
        /// </summary>
        [Test]
        public void AChoosingSeatAtTheAgreementStageDrawsItsOwnStatementsAndIsAccepted()
        {
            var host = TeamAgreementTests.ReachAgreement(TeamAgreementKind.MinionBattle);
            TeamAgreementTests.CompleteMinionSelection(host, "chooser-select");
            var choice = host.Projection.PendingMinionBattleChoice;
            Assert.That(choice.CanConfirm, Is.True);
            var selected = choice.SelectedUnits.ToArray();
            Assert.That(selected, Is.Not.Empty);

            var harness = PrivateOnlineWireHarness.Compose(host);
            harness.SeatAll();
            var chooser = host.Seats.Seats.First(seat => seat.PlaysFor(choice.ChoosingSide));
            var surface = SeatSurface.ForWireSeat(harness, host, chooser);

            // While it still owns the detail it holds both halves: the board candidates it may
            // still revise, and the agreement it may state.
            surface.RefreshAll();
            Assert.That(surface.Exceptions, Is.Empty, Describe(surface.Exceptions));
            Assert.That(surface.PromptModel.IsPublicAgreementStage, Is.False);
            Assert.That(surface.PromptModel.ResolutionId.IsDefined, Is.True);
            var statement = surface.PromptModel.Agreement.Choices.Single();

            // The press. Its accepted result is the projection with the detail stripped and the
            // public agreement standing, and refreshing from it is where the crash was entered.
            var accepted = surface.Press(() => surface.Prompt.SubmitAgreement(statement));

            Assert.That(surface.Exceptions, Is.Empty,
                "The accepted press's own result refresh is one of the three entry paths the " +
                "playtest logs recorded: " + Describe(surface.Exceptions));
            Assert.That(accepted, Is.True);
            var inFlight = surface.PromptModel;
            Assert.That(surface.Client.Projection.IsInputEnabled, Is.False,
                "The in-flight view is the one that carries the agreement and no pending detail.");
            Assert.That(surface.Client.Projection.PendingMinionBattleChoice, Is.Null);
            Assert.That(surface.Client.Projection.TeamAgreement, Is.Not.Null);
            Assert.That(inFlight.IsPublicAgreementStage, Is.True);
            Assert.That(inFlight.ResolutionId.IsDefined, Is.False);
            Assert.That(inFlight.ChoiceId.IsDefined, Is.False);
            Assert.That(inFlight.Candidates, Is.Empty);
            Assert.That(inFlight.ConfirmInvocation, Is.Null);
            Assert.That(inFlight.ChoosingTeamId,
                Is.EqualTo(host.Projection.Participants.Teams
                    .Single(team => team.Side == choice.ChoosingSide).Id));
            Assert.That(inFlight.RequiredRemovalCount, Is.EqualTo(choice.RequiredRemovalCount));
            Assert.That(inFlight.SelectedCount, Is.EqualTo(selected.Length));
            Assert.That(inFlight.StatusLabel, Is.EqualTo(
                "SELECTED " + selected.Length + " OF " + choice.RequiredRemovalCount));
            Assert.That(inFlight.Agreement.Participants, Has.Count.EqualTo(3),
                "The whole choosing side's public standing is what this stage reports.");

            // The selected units are drawn where they stand, from the public agreement, so the
            // stage shows what is about to be removed without naming a candidate control.
            var cued = surface.Board.Model.PieceTargetCues
                .Where(cue => cue.State == BoardTargetCueState.Selected)
                .Select(cue => cue.Target)
                .ToArray();
            foreach (var unit in selected)
            {
                Assert.That(
                    cued.Any(target => target.Equals(unit.Kind == RuleObjectKind.Hero
                        ? BoardObjectReference.ForHero(unit.HeroId)
                        : BoardObjectReference.ForMinion(unit.MinionId))),
                    Is.True,
                    "Every selected unit of the public agreement is cued where it stands.");
            }

            // And the statement really reached the authority.
            harness.Pump();
            var client = harness.Client(harness.OrdinalOf(chooser.Id.Value));
            Assert.That(client.LastAgreementResult, Is.Not.Null);
            Assert.That(client.LastAgreementResult.Accepted, Is.True,
                client.LastAgreementResult.Detail);
            Assert.That(host.ProjectionForSeat(chooser.Id).TeamAgreement.Participants
                    .Single(participant => participant.HeroId.Equals(chooser.HeroId)).Agreed,
                Is.True);
            surface.RefreshAll();
            Assert.That(surface.Exceptions, Is.Empty, Describe(surface.Exceptions));
        }

        /// <summary>
        /// The guard the crash came out of is not what was wrong, and it stands unchanged: the
        /// removal-prompt form still refuses identities nothing authoritative supplied, and the new
        /// public form draws the Minion Battle's own agreement or nothing.
        /// </summary>
        [Test]
        public void TheRemovalPromptFormStillRefusesFabricatedIdentitiesAndThePublicFormIsAgreementOnly()
        {
            var host = TeamAgreementTests.ReachAgreement(TeamAgreementKind.MinionBattle);
            var choice = host.Projection.PendingMinionBattleChoice;
            var teamId = host.Projection.Participants.Teams
                .Single(team => team.Side == choice.ChoosingSide).Id;
            var agreement = new TeamAgreementHudViewModel(
                TeamAgreementKind.MinionBattle, "TEAM AGREEMENT",
                Array.Empty<TeamAgreementParticipantViewModel>(),
                Array.Empty<TeamAgreementChoiceViewModel>());

            Assert.That(
                () => new MinionChoiceViewModel(
                    host.Version, default, default, teamId, choice.ChoosingSide,
                    choice.RequiredRemovalCount, 0,
                    Array.Empty<MinionChoiceCandidateViewModel>(), null, agreement),
                Throws.ArgumentException,
                "The removal prompt is built from an owned pending choice or not at all.");

            Assert.That(
                () => MinionChoiceViewModel.ForPublicAgreement(
                    host.Version, teamId, choice.ChoosingSide,
                    choice.RequiredRemovalCount, 0, null),
                Throws.ArgumentNullException,
                "The public stage exists because there is an agreement to draw.");

            var initiative = new TeamAgreementHudViewModel(
                TeamAgreementKind.Initiative, "WHO ACTS FIRST?",
                Array.Empty<TeamAgreementParticipantViewModel>(),
                Array.Empty<TeamAgreementChoiceViewModel>());
            Assert.That(
                () => MinionChoiceViewModel.ForPublicAgreement(
                    host.Version, teamId, choice.ChoosingSide,
                    choice.RequiredRemovalCount, 0, initiative),
                Throws.InstanceOf<ArgumentOutOfRangeException>(),
                "The tied-Initiative agreement is not this prompt's stage.");

            var publicStage = MinionChoiceViewModel.ForPublicAgreement(
                host.Version, teamId, choice.ChoosingSide, choice.RequiredRemovalCount, 0,
                agreement);

            Assert.That(publicStage.IsPublicAgreementStage, Is.True);
            Assert.That(publicStage.Kind, Is.EqualTo(MinionChoicePromptKind.MinionBattleRemoval));
            Assert.That(publicStage.Candidates, Is.Empty);
            Assert.That(publicStage.ConfirmInvocation, Is.Null);
            Assert.That(publicStage.ActionResolutionId.IsDefined, Is.False);
            Assert.That(publicStage.ActionChoiceId.IsDefined, Is.False);
            Assert.That(publicStage.SectionLabel, Is.EqualTo("Minion Battle"));
        }

        /// <summary>
        /// Milestone 8.35z2 (the empty-button gap a false-alarm invited-playtest finding recorded).
        /// The choosing side's own AGREE/WITHDRAW press is in flight -
        /// <c>TeamAgreement.AuthorizedCommands</c> is deliberately empty until the host's correlated
        /// projection returns - and before this milestone <see cref="MinionChoiceUnityView"/> drew the
        /// public status line over nothing at all, which read as a vanished control.
        /// </summary>
        [Test]
        public void MinionChoiceView_WhenAStatementIsInFlightDrawsWaitingForHostInsteadOfNoControls()
        {
            var agreement = new TeamAgreementHudViewModel(
                TeamAgreementKind.MinionBattle,
                "TEAM AGREEMENT",
                new[]
                {
                    new TeamAgreementParticipantViewModel("HERO 1", "agreed"),
                    new TeamAgreementParticipantViewModel("HERO 2", "is deciding")
                },
                Array.Empty<TeamAgreementChoiceViewModel>(),
                isInputEnabled: false);
            var publicStage = MinionChoiceViewModel.ForPublicAgreement(
                1, new TeamId("choosing-team"), TeamSide.Red, 1, 1, agreement);
            var view = new MinionChoiceUnityView();

            view.Render(publicStage);

            Assert.That(view.Root.Query<Button>().ToList().Any(button =>
                    button.text == "AGREE WITH THIS SELECTION" || button.text == "WITHDRAW MY AGREEMENT"),
                Is.False, "No agreement control is offered while the seat has nothing authorized.");
            Assert.That(view.Root.Query<Label>().ToList().Any(label =>
                    label.text == ActionUnavailabilityLabels.WaitingForHost),
                Is.True, "The seat's own in-flight statement reads a normal wait, not a silent gap.");
        }

        /// <summary>
        /// The other half: the non-choosing side keeps an empty command set for the whole battle, not
        /// for one round trip - <c>IsInputEnabled</c> stays true - so drawing "WAITING FOR HOST…" there
        /// would be misleading. The pre-existing, still-correct silent reading must not change.
        /// </summary>
        [Test]
        public void MinionChoiceView_TheNonChoosingSideDrawsNeitherControlsNorWaitingText()
        {
            var agreement = new TeamAgreementHudViewModel(
                TeamAgreementKind.MinionBattle,
                "TEAM AGREEMENT",
                new[]
                {
                    new TeamAgreementParticipantViewModel("HERO 1", "agreed"),
                    new TeamAgreementParticipantViewModel("HERO 2", "is deciding")
                },
                Array.Empty<TeamAgreementChoiceViewModel>());
            var publicStage = MinionChoiceViewModel.ForPublicAgreement(
                1, new TeamId("choosing-team"), TeamSide.Red, 1, 1, agreement);
            var view = new MinionChoiceUnityView();

            view.Render(publicStage);

            Assert.That(view.Root.Query<Label>().ToList().Any(label =>
                    label.text == ActionUnavailabilityLabels.WaitingForHost),
                Is.False, "The other side is never waiting on a host round trip of its own.");
        }

        /// <summary>
        /// The six-seat wire regression. Six real client machines each carry the whole presenter set
        /// a served seat composes, and every one of them is refreshed at every version it observes
        /// while the battle runs - first selection, each in-flight press, each public statement, and
        /// the applied removal. No seat may record an exception at any of them.
        /// </summary>
        [Test]
        public void SixWireSeatsRefreshTheirWholePresenterSetAtEveryVersionOfAMinionBattle()
        {
            var host = TeamAgreementTests.ReachAgreement(TeamAgreementKind.MinionBattle);
            var choosingSide = host.Projection.PendingMinionBattleChoice.ChoosingSide;
            var harness = PrivateOnlineWireHarness.Compose(host);
            harness.SeatAll();
            var surfaces = host.Seats.Seats
                .Select(seat => SeatSurface.ForWireSeat(harness, host, seat))
                .ToArray();
            var versions = surfaces.ToDictionary(
                surface => surface.Seat.Id.Value, _ => new HashSet<long>());
            var presses = 0;
            var statements = 0;

            void RefreshEverySeat()
            {
                foreach (var surface in surfaces)
                {
                    surface.RefreshAll();
                    versions[surface.Seat.Id.Value].Add(surface.Client.Projection.Version);
                }
            }

            RefreshEverySeat();
            while (host.Projection.PendingMinionBattleChoice != null &&
                !host.Projection.PendingMinionBattleChoice.CanConfirm &&
                presses < 12)
            {
                var acting = surfaces.First(surface =>
                    surface.Seat.PlaysFor(choosingSide) &&
                    surface.PromptModel != null &&
                    surface.PromptModel.Candidates.Any(value => !value.IsSelected));
                var candidate = acting.PromptModel.Candidates.First(value => !value.IsSelected);

                Assert.That(acting.Press(() => acting.Prompt.Submit(candidate)), Is.True,
                    "Seat " + acting.Seat.Id + " forwarded a control its own wire record " +
                    "published: " + Describe(acting.Exceptions));
                presses++;
                RefreshEverySeat();
                harness.Pump();
                RefreshEverySeat();
            }

            Assert.That(presses, Is.GreaterThan(0),
                "The battle must be selected through the wire for this regression to mean anything.");
            Assert.That(host.Projection.PendingMinionBattleChoice, Is.Not.Null);
            Assert.That(host.Projection.PendingMinionBattleChoice.CanConfirm, Is.True);

            foreach (var surface in surfaces.Where(value => value.Seat.PlaysFor(choosingSide)))
            {
                var statement = surface.PromptModel.Agreement.Choices.Single();
                Assert.That(surface.Press(() => surface.Prompt.SubmitAgreement(statement)), Is.True,
                    "Seat " + surface.Seat.Id + " could not state its agreement: " +
                    Describe(surface.Exceptions));
                statements++;
                RefreshEverySeat();
                harness.Pump();
                RefreshEverySeat();
            }

            Assert.That(statements, Is.EqualTo(3),
                "The whole choosing side states, which is what makes the agreement unanimous.");
            Assert.That(host.Projection.PendingMinionBattleChoice, Is.Null,
                "Unanimity submitted the confirm and the removal was applied.");
            harness.AssertConverged();
            foreach (var surface in surfaces)
            {
                Assert.That(surface.Exceptions, Is.Empty,
                    "Seat " + surface.Seat.Id + " recorded " + surface.Exceptions.Count +
                    " presentation exceptions: " + Describe(surface.Exceptions));
                Assert.That(versions[surface.Seat.Id.Value].Count, Is.GreaterThan(1),
                    "Seat " + surface.Seat.Id + " observed only one version, so it was never " +
                    "refreshed across the battle at all.");
                Assert.That(versions[surface.Seat.Id.Value].Contains(host.Version), Is.True,
                    "Seat " + surface.Seat.Id + " never rendered the settled version.");
            }
        }

        /// <summary>
        /// The tray half, over a real seat-served walk. From rulebook p.8's Retrieve stage until the
        /// next round begins, commitments still name the cards the table saw. The bounded public log
        /// preserves those exact identities when they return to hand. The 8.42b player tray remains
        /// owner-only even though that public identity can still be read by public surfaces.
        /// </summary>
        [Test]
        public void ASeatTrayStaysPrivateWhenARevealedCardReturnsToAnotherHerosHand()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var seat = host.Seats.Seats[0];
            var client = new SeatMatchClient(host.ClientForSeat(seat.Id));
            var view = new CapturingHandsView();
            var presenter = new DirectorHandsPresenter(client, view, seat.HeroId);
            var retrievedPublicRenders = 0;
            var hiddenOpponentCards = 0;

            var walked = OwnerScopedProjectionTests.DriveThroughSeats(host, 1500, _ =>
            {
                // Every version this seat observes, including every pause the host's end-of-round
                // continuation stops at - which is where the playtest's failures were entered.
                presenter.Refresh();
                Assert.That(view.Model, Is.Not.Null);
                Assert.That(view.Model.Hands.Select(hand => hand.HeroId), Is.EqualTo(new[] { seat.HeroId }),
                    "8.42b removed all foreign trays, including their publicly known cards.");
                var scoped = client.Projection;
                foreach (var commitment in scoped.Commitments)
                {
                    if (!commitment.RevealedCardInstanceId.HasValue)
                    {
                        continue;
                    }

                    var authoritative = host.Projection.Cards.Single(card =>
                        card.Id.Equals(commitment.RevealedCardInstanceId.Value));
                    var known = scoped.Cards.SingleOrDefault(card => card.Id.Equals(authoritative.Id));
                    Assert.That(known, Is.Not.Null,
                        "A revealed identity retained by the bounded public log must still resolve.");
                    if (!commitment.HeroId.Equals(seat.HeroId) &&
                        authoritative.State.Location == CardLocation.Hand)
                    {
                        presenter.SelectViewedHero(commitment.HeroId);
                        presenter.Refresh();
                        Assert.That(view.Model.ViewedHeroId, Is.EqualTo(seat.HeroId),
                            "Public knowledge of a retrieved face does not authorize a foreign tray.");
                        retrievedPublicRenders++;
                    }
                }

                hiddenOpponentCards += host.Projection.Cards.Count(card =>
                    !card.OwnerId.Equals(seat.HeroId) &&
                    card.State.Location == CardLocation.Hand &&
                    !scoped.Cards.Any(visible => visible.Id.Equals(card.Id)));

                return false;
            }, seed: 29);

            Assert.That(retrievedPublicRenders, Is.GreaterThan(0),
                "The seeded seat walk never observed a publicly revealed opponent card after " +
                "Retrieve in " + walked + " commands, so this case proves nothing.");
            Assert.That(hiddenOpponentCards, Is.GreaterThan(0),
                "Retaining an exact revealed identity must not expose the rest of an opponent's hand.");
        }

        /// <summary>
        /// The other half of the same reading: the absence is a property of a scoped list, not of
        /// the authority. The Director reads every card there is, so its tray resolves every
        /// revealed commitment and a null there would still be a real defect.
        /// </summary>
        [Test]
        public void TheDirectorsOwnTrayStillResolvesEveryRevealedCommitment()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var view = new CapturingHandsView();
            var presenter = new DirectorHandsPresenter(
                new DirectorMatchClient(new LocalDirectorMatchClient(host)), view);
            var resolved = 0;

            OwnerScopedProjectionTests.DriveThroughSeats(host, 800, _ =>
            {
                presenter.Refresh();
                foreach (var hand in view.Model.Hands.Where(value =>
                    value.RevealedCardInstanceId.HasValue))
                {
                    Assert.That(hand.RevealedCard, Is.Not.Null,
                        "The full-trust read model carries every card, so a missing one here " +
                        "would mean the commitment named a card that does not exist.");
                    resolved++;
                }

                return false;
            });

            Assert.That(resolved, Is.GreaterThan(0));
        }

        private static void AgreeUnanimously(IMatchHost host, string idPrefix, Action afterEach)
        {
            var side = host.Projection.PendingMinionBattleChoice.ChoosingSide;
            var index = 0;
            foreach (var seat in host.Seats.Seats.Where(value => value.PlaysFor(side)).ToArray())
            {
                var view = host.ProjectionForSeat(seat.Id);
                var statement = view.TeamAgreement.AuthorizedCommands.Single(value =>
                    value.Kind == TeamAgreementCommandKind.AgreeToMinionBattleSelection);
                var result = host.ClientForSeat(seat.Id).SubmitAgreement(new TeamAgreementSubmission(
                    new SubmissionId(idPrefix + "-" + index++), view.Version, statement));
                Assert.That(result.Accepted, Is.True, result.Detail);
                if (host.Projection.PendingMinionBattleChoice != null)
                {
                    afterEach();
                }
            }
        }

        private static string Describe(IReadOnlyList<Exception> exceptions) =>
            exceptions.Count == 0
                ? "none"
                : string.Join(" | ", exceptions.Select(value =>
                    value.GetType().Name + ": " + value.Message).ToArray());

        /// <summary>
        /// One seat's whole HUD: the presenter set a served seat composes, wired through the same
        /// coordinator the runtime uses, so an accepted press refreshes every one of them exactly as
        /// <c>PrivateLobbyDriver.Update</c> does. Exceptions are collected rather than left to
        /// escape, so a pre-fix run reports every failing render instead of only the first.
        /// </summary>
        private sealed class SeatSurface
        {
            private readonly MatchFeatureCoordinator coordinator;
            private readonly CapturingMinionChoiceView prompt;
            private readonly List<Exception> exceptions = new List<Exception>();

            private SeatSurface(MatchSeat seat, IMatchClient client)
            {
                Seat = seat;
                Client = client;
                Board = new CapturingBoardView();
                Hands = new CapturingHandsView();
                Round = new CapturingRoundView();
                HeroOverviews = new CapturingHeroOverviewsView();
                prompt = new CapturingMinionChoiceView();
                Prompt = new MinionChoicePresenter(client, prompt);
                coordinator = new MatchFeatureCoordinator(
                    new BoardInteractionPresenter(client, Board),
                    new DirectorHandsPresenter(client, Hands, seat.HeroId),
                    new RoundActionPresenter(client, Round),
                    null,
                    null,
                    Prompt,
                    null,
                    new HeroOverviewsPresenter(client, HeroOverviews, () => seat.HeroId));
            }

            public MatchSeat Seat { get; }
            public IMatchClient Client { get; }
            public CapturingBoardView Board { get; }
            public CapturingHandsView Hands { get; }
            public CapturingRoundView Round { get; }
            public CapturingHeroOverviewsView HeroOverviews { get; }
            public MinionChoicePresenter Prompt { get; }
            public MinionChoiceViewModel PromptModel => prompt.Model;
            public IReadOnlyList<Exception> Exceptions => exceptions;

            public static SeatSurface ForLocalSeat(IMatchHost host, MatchSeat seat) =>
                new SeatSurface(seat, new SeatMatchClient(host.ClientForSeat(seat.Id)));

            public static SeatSurface ForWireSeat(
                PrivateOnlineWireHarness harness, IMatchHost host, MatchSeat seat) =>
                new SeatSurface(seat, new NetworkMatchClient(
                    harness.Client(harness.OrdinalOf(seat.Id.Value)),
                    host.Projection,
                    MatchContentCatalogs.ForThisBuild()));

            public void RefreshAll()
            {
                try
                {
                    coordinator.RefreshAll();
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            }

            /// <summary>
            /// A press, and the refresh the coordinator runs from inside its accepted result. That
            /// refresh is the presenter's own, not this type's, so it is caught here rather than
            /// left to abort the fixture at the first of many.
            /// </summary>
            public bool Press(Func<bool> press)
            {
                try
                {
                    return press();
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                    return false;
                }
            }
        }

        private sealed class CapturingBoardView : IBoardInteractionView
        {
            public BoardInteractionViewModel Model { get; private set; }
            public void Render(BoardInteractionViewModel model) => Model = model;
        }

        private sealed class CapturingHandsView : IDirectorHandsView
        {
            public DirectorHandsViewModel Model { get; private set; }
            public void Render(DirectorHandsViewModel model) => Model = model;
        }

        private sealed class CapturingRoundView : IRoundActionView
        {
            public RoundActionViewModel Model { get; private set; }
            public void Render(RoundActionViewModel model) => Model = model;
        }

        private sealed class CapturingMinionChoiceView : IMinionChoiceView
        {
            public MinionChoiceViewModel Model { get; private set; }
            public void Render(MinionChoiceViewModel model) => Model = model;
        }

        private sealed class CapturingHeroOverviewsView : IHeroOverviewsView
        {
            public HeroOverviewsViewModel Model { get; private set; }
            public void Render(HeroOverviewsViewModel model) => Model = model;
        }
    }
}
