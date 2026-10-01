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
    /// <para>Milestone 9.29 replaced the shared selection and its public stage with each choosing
    /// seat's own picks, so the first half now pins what replaced it: the other side draws nothing,
    /// a choosing seat's press picks for that seat alone, and every reader of every version of the
    /// battle still renders without an exception.</para>
    ///
    /// <para>Unity-free on purpose - every view here is a capturing double - so the fixture states
    /// presenter behaviour rather than element layout.</para>
    /// </summary>
    public sealed class MinionBattleAgreementRenderingTests
    {
        /// <summary>
        /// Milestone 9.29. The side that is not choosing, at every version of the battle from the
        /// first pick to the applied removal. It is told the other side is choosing and nothing of
        /// what: no prompt, no cue, no pick - and, as 8.35e required of every reader, no exception.
        /// </summary>
        [Test]
        public void ANonChoosingSeatDrawsNoPromptCueOrPickAtAnyVersionOfTheBattle()
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
                Assert.That(surface.PromptModel, Is.Null,
                    "The other side has nothing to answer and nothing of the picks to draw.");
                Assert.That(surface.Board.Model.PieceTargetCues.Where(cue =>
                        cue.ConsequenceIntent == BoardTargetConsequenceIntent.Removal), Is.Empty,
                    "No minion of the choosing side is cued for the other side.");
                Assert.That(surface.Board.Model.MinionBattlePicks, Is.Empty);
                renders++;
            }

            Observe();
            var choosers = TeamAgreementTests.ChoosingSeats(host);
            var required = host.Projection.PendingMinionBattleChoice.RequiredRemovalCount;
            var leaderPicks = TeamAgreementTests.PickFirstOffered(host, choosers[0], required, "observer-lead");
            Observe();
            for (var index = 1; index < choosers.Length; index++)
            {
                TeamAgreementTests.PickSet(host, choosers[index], leaderPicks, "observer-" + index);
                if (host.Projection.PendingMinionBattleChoice != null)
                    Observe();
            }

            Assert.That(host.Projection.PendingMinionBattleChoice, Is.Null,
                "Matching picks removed the minions.");
            Assert.That(renders, Is.GreaterThan(2));
            surface.RefreshAll();
            Assert.That(surface.Exceptions, Is.Empty, Describe(surface.Exceptions));
            Assert.That(surface.PromptModel, Is.Null);
        }

        /// <summary>
        /// Milestone 9.29, over two real wire seats of the choosing side. A press picks for the seat
        /// that pressed and for nobody else; the teammate's board draws that pick as SELECTED - the
        /// full outline - with the picker's token on it, and still offers the teammate its own pick
        /// of the same minion.
        /// </summary>
        [Test]
        public void AChoosingSeatsPressPicksForItAlone_AndItsTeammateSeesThePickWithItsToken()
        {
            var host = TeamAgreementTests.ReachAgreement(TeamAgreementKind.MinionBattle);
            var choice = host.Projection.PendingMinionBattleChoice;
            var harness = PrivateOnlineWireHarness.Compose(host);
            harness.SeatAll();
            var choosers = TeamAgreementTests.ChoosingSeats(host);
            var leader = SeatSurface.ForWireSeat(harness, host, choosers[0]);
            var teammate = SeatSurface.ForWireSeat(harness, host, choosers[1]);
            leader.RefreshAll();
            teammate.RefreshAll();
            Assert.That(leader.Exceptions, Is.Empty, Describe(leader.Exceptions));
            Assert.That(leader.PromptModel, Is.Not.Null);
            Assert.That(leader.PromptModel.ConfirmInvocation, Is.Null, "Matching picks remove; no button.");
            var candidate = leader.PromptModel.Candidates.First(value => !value.IsSelected);
            Assert.That(candidate.AgreementCommand, Is.Not.Null,
                "A seat's control is its own pick statement, never a rules command.");
            Assert.That(candidate.Command, Is.Null);
            var eligible = leader.Board.Model.PieceTargetCues.Where(cue =>
                cue.ConsequenceIntent == BoardTargetConsequenceIntent.Removal).ToArray();
            Assert.That(eligible.All(cue => cue.State == BoardTargetCueState.Eligible), Is.True,
                "Before anyone picks, every pickable minion wears the corner marks and none the outline.");

            Assert.That(leader.Press(() => leader.Prompt.Submit(candidate)), Is.True,
                Describe(leader.Exceptions));
            Assert.That(leader.Exceptions, Is.Empty,
                "The in-flight refresh is one of 8.35e's entry paths: " + Describe(leader.Exceptions));
            harness.Pump();
            leader.RefreshAll();
            teammate.RefreshAll();
            Assert.That(leader.Exceptions, Is.Empty, Describe(leader.Exceptions));
            Assert.That(teammate.Exceptions, Is.Empty, Describe(teammate.Exceptions));

            var picked = candidate.Unit.Kind == RuleObjectKind.Hero
                ? BoardObjectReference.ForHero(candidate.Unit.HeroId)
                : BoardObjectReference.ForMinion(candidate.Unit.MinionId);
            var leaderPick = leader.Board.Model.MinionBattlePicks.Single(pick => pick.Target.Equals(picked));
            Assert.That(leaderPick.PickedByViewer, Is.True);
            Assert.That(leaderPick.TeammatePickers, Is.Empty, "A player's own pick carries no token.");
            Assert.That(leader.PromptModel.Candidates.Single(value => value.Unit.Equals(candidate.Unit))
                .IsSelected, Is.True, "Pressing it again takes the pick back.");

            var teammatePick = teammate.Board.Model.MinionBattlePicks.Single(pick => pick.Target.Equals(picked));
            Assert.That(teammatePick.PickedByViewer, Is.False);
            Assert.That(teammatePick.TeammatePickers, Is.EqualTo(new[] { choosers[0].HeroId }),
                "The teammate's board names who picked it.");
            Assert.That(teammate.Board.Model.PieceTargetCues.Single(cue => cue.Target.Equals(picked)).State,
                Is.EqualTo(BoardTargetCueState.Selected), "A teammate's pick wears the full outline.");
            var teammatesOwn = teammate.PromptModel.Candidates.Single(value => value.Unit.Equals(candidate.Unit));
            Assert.That(teammatesOwn.IsSelected, Is.False,
                "The leader's pick is not the teammate's: its own pick of that minion is still offered.");
            Assert.That(teammatesOwn.AgreementCommand.Kind,
                Is.EqualTo(TeamAgreementCommandKind.PickMinionBattleRemoval));
            Assert.That(teammate.PromptModel.SelectedCount, Is.Zero);
            Assert.That(host.Projection.PendingMinionBattleChoice.SelectedUnits, Is.Empty);
            Assert.That(choice.RequiredRemovalCount, Is.GreaterThan(0));
        }

        /// <summary>
        /// The guard 8.35e's crash came out of was not what was wrong, and it stands unchanged: the
        /// removal-prompt form still refuses identities nothing authoritative supplied.
        /// </summary>
        [Test]
        public void TheRemovalPromptFormStillRefusesFabricatedIdentities()
        {
            var host = TeamAgreementTests.ReachAgreement(TeamAgreementKind.MinionBattle);
            var choice = host.Projection.PendingMinionBattleChoice;
            var teamId = host.Projection.Participants.Teams
                .Single(team => team.Side == choice.ChoosingSide).Id;

            Assert.That(
                () => new MinionChoiceViewModel(
                    host.Version, default, default, teamId, choice.ChoosingSide,
                    choice.RequiredRemovalCount, 0,
                    Array.Empty<MinionChoiceCandidateViewModel>(), null, null),
                Throws.ArgumentException,
                "The removal prompt is built from an owned pending choice or not at all.");
        }

        /// <summary>
        /// Milestone 9.29 retired 8.30n's AGREE / WITHDRAW controls: picks are made on the board, so
        /// the old prompt view draws no agreement control for the battle at all.
        /// </summary>
        [Test]
        public void MinionChoiceView_DrawsNoAgreementControlForTheBattle()
        {
            var host = TeamAgreementTests.ReachAgreement(TeamAgreementKind.MinionBattle);
            var chooser = TeamAgreementTests.ChoosingSeats(host)[0];
            var surface = SeatSurface.ForLocalSeat(host, chooser);
            surface.RefreshAll();
            Assert.That(surface.PromptModel.Agreement, Is.Not.Null);
            Assert.That(surface.PromptModel.Agreement.Choices, Is.Empty);
            var view = new MinionChoiceUnityView();

            view.Render(surface.PromptModel);

            Assert.That(view.Root.Query<Button>().ToList().Any(button =>
                    button.text == "AGREE WITH THIS SELECTION" || button.text == "WITHDRAW MY AGREEMENT"),
                Is.False);

            // And while the decision window draws the battle, this older prompt draws nothing at all:
            // its "SELECTED 0 OF 1" header sat under the docked bar in the 9.29 capture.
            var yielding = new MinionChoiceUnityView
            {
                YieldedDecisionFamily = () => DecisionWindowFamily.MinionBattleRemoval
            };
            yielding.Render(surface.PromptModel);
            Assert.That(yielding.Root.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        /// <summary>
        /// The six-seat wire regression. Six real client machines each carry the whole presenter set
        /// a served seat composes, and every one of them is refreshed at every version it observes
        /// while the battle runs - each press, each in-flight statement and the applied removal. No
        /// seat may record an exception at any of them.
        /// </summary>
        [Test]
        public void SixWireSeatsRefreshTheirWholePresenterSetAtEveryVersionOfAMinionBattle()
        {
            var host = TeamAgreementTests.ReachAgreement(TeamAgreementKind.MinionBattle);
            var choosingSide = host.Projection.PendingMinionBattleChoice.ChoosingSide;
            var required = host.Projection.PendingMinionBattleChoice.RequiredRemovalCount;
            var harness = PrivateOnlineWireHarness.Compose(host);
            harness.SeatAll();
            var surfaces = host.Seats.Seats
                .Select(seat => SeatSurface.ForWireSeat(harness, host, seat))
                .ToArray();
            var versions = surfaces.ToDictionary(
                surface => surface.Seat.Id.Value, _ => new HashSet<long>());
            var presses = 0;

            void RefreshEverySeat()
            {
                foreach (var surface in surfaces)
                {
                    surface.RefreshAll();
                    versions[surface.Seat.Id.Value].Add(surface.Client.Projection.Version);
                }
            }

            RefreshEverySeat();
            IReadOnlyList<RuleObject> agreed = null;
            foreach (var surface in surfaces.Where(value => value.Seat.PlaysFor(choosingSide)))
            {
                while (host.Projection.PendingMinionBattleChoice != null && presses < 24)
                {
                    var own = surface.PromptModel.Candidates.Where(value => value.IsSelected)
                        .Select(value => value.Unit).ToArray();
                    if (agreed == null && own.Length == required)
                    {
                        agreed = own;
                        break;
                    }

                    if (agreed != null && own.Length == agreed.Count && own.All(agreed.Contains))
                        break;

                    var candidate = agreed == null
                        ? surface.PromptModel.Candidates.First(value => !value.IsSelected)
                        : surface.PromptModel.Candidates.First(value =>
                            value.IsSelected != agreed.Contains(value.Unit));
                    Assert.That(surface.Press(() => surface.Prompt.Submit(candidate)), Is.True,
                        "Seat " + surface.Seat.Id + " forwarded a control its own wire record " +
                        "published: " + Describe(surface.Exceptions));
                    presses++;
                    RefreshEverySeat();
                    harness.Pump();
                    RefreshEverySeat();
                }
            }

            Assert.That(presses, Is.GreaterThanOrEqualTo(required * 3),
                "The whole choosing side picks, which is what makes the picks match.");
            Assert.That(host.Projection.PendingMinionBattleChoice, Is.Null,
                "Matching picks removed the minions.");
            harness.AssertConverged();
            foreach (var surface in surfaces)
            {
                Assert.That(surface.Exceptions, Is.Empty,
                    "Seat " + surface.Seat.Id + " recorded " + surface.Exceptions.Count +
                    " presentation exceptions: " + Describe(surface.Exceptions));
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
            var returnedToDeckWithheld = 0;

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
                    // Milestone 8.46e. The one-press rule re-derived this walk, and seed 29 now reaches
                    // a state the old walk never did: an opponent plays a card, retrieves it at the end
                    // of the round, and then RETURNS IT TO THE DECK through p.15's level-up while the
                    // round's commitment still names it. The seat scope's retention rule is written for
                    // the Hand (a retrieved card the public log already revealed) and is fail-closed
                    // for every location it has not considered, the Deck among them - which is the
                    // shipped contract the tray presenter is deliberately tolerant of. So the identity
                    // must resolve wherever the scope retains it, and a returned-to-deck card is the
                    // one documented absence.
                    if (authoritative.State.Location == CardLocation.Deck)
                    {
                        returnedToDeckWithheld++;
                        continue;
                    }

                    Assert.That(known, Is.Not.Null,
                        "A revealed identity retained by the bounded public log must still resolve " +
                        "(location " + authoritative.State.Location + ").");
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
            TestContext.Out.WriteLine(
                "returned-to-deck commitments the scope withheld: " + returnedToDeckWithheld);
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
