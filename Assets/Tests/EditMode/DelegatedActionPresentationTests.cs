using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.72, the user's correction to the twenty-first hero: "Q15i means that currently
    /// that card wont work."
    ///
    /// <para><b>What these cases measure, and what they deliberately do not.</b> Milestone 8.71b
    /// already proved WHO MAY ACT - the host authorizes the deciding seat and refuses the targeted
    /// one - and <c>HanuUltimateTrickTests</c> owns every one of those assertions. These cases
    /// measure the other half, which had no coverage because it had no behaviour: WHAT EACH SEAT
    /// DRAWS. They run on that fixture's own staged six-seat match rather than a second one, so
    /// "when a delegation stands" is one answer here and there.</para>
    ///
    /// <para><b>The claim, stated as an equality rather than as a screenshot.</b> The deciding seat's
    /// drawn controls are exactly the commands the host authorizes it for the delegated actor, and
    /// the targeted seat's are none. Both halves are read off a real
    /// <see cref="DirectorHandsUnityView"/> - the HUD's own view, built through the HUD's own
    /// presenters - because the defect this milestone fixes was invisible at every tier above the
    /// drawn element: the view models were right and the tray had no card to draw them on.</para>
    /// </summary>
    public sealed class DelegatedActionPresentationTests
    {
        // -------------------------------------------------------------------------------------------
        // (a) The owner's one sentence
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A seat draws the selected actor's printed controls exactly when its hero is the hero
        /// DECIDING that action. Asked of every seat at the table, so "exactly" is measured rather
        /// than asserted twice.
        /// </summary>
        [Test]
        public void OnlyTheDecidingSeatDrawsTheActionControls()
        {
            var match = HanuUltimateTrickTests.DelegatedMatch.Stage(againstAnEnemy: true);

            foreach (var seat in match.Host.Seats.Seats)
            {
                var projection = match.Host.ProjectionForSeat(seat.Id);
                Assert.That(
                    DelegatedActionPresentation.DrawsTheActionControls(projection, seat.HeroId),
                    Is.EqualTo(seat.Owns(match.Decider)),
                    "Seat " + seat.Id + ": the controls belong where the host accepts them.");
                Assert.That(
                    DelegatedActionPresentation.DecidedBy(projection, seat.HeroId).IsDefined,
                    Is.EqualTo(seat.Owns(match.Decider)),
                    "Seat " + seat.Id + ": and exactly one seat is handed another hero's card.");
                Assert.That(
                    DelegatedActionPresentation.IsDecidedElsewhere(projection, seat.HeroId),
                    Is.EqualTo(seat.Owns(match.Delegated)),
                    "Seat " + seat.Id + ": exactly one seat's own piece is acting without it.");
            }
        }

        /// <summary>
        /// The Director's seatless sandbox names no hero and answers for everybody, so it keeps
        /// drawing whatever tray it is pointed at - the property every pre-8.72 fixture that composes
        /// a viewless model depends on.
        /// </summary>
        [Test]
        public void TheSeatlessViewerKeepsDrawingEveryTurnItAlreadyDrew()
        {
            var match = HanuUltimateTrickTests.DelegatedMatch.Stage(againstAnEnemy: true);

            Assert.That(
                DelegatedActionPresentation.DrawsTheActionControls(match.Host.Projection, default),
                Is.True);
            Assert.That(
                DelegatedActionPresentation.DecidedBy(match.Host.Projection, default).IsDefined,
                Is.False,
                "and a viewer naming no hero is handed no slot of its own, because a slot says " +
                "WHOSE card it is and a seatless view is nobody's.");
        }

        // -------------------------------------------------------------------------------------------
        // (b) The deciding seat: every control the host authorizes, drawn
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheDecidingSeatDrawsTheDelegatedHerosCardWithEveryControlTheHostAuthorizes()
        {
            AssertTheDecidingSeatDrawsWhatTheHostAuthorizes(againstAnEnemy: true);
        }

        /// <summary>
        /// HA-Q3: "It applies to any hero Hanu targets with Hurry up!, friend or enemy." A reading
        /// that only drew the controls against an enemy would be a different card, so the friendly
        /// board is measured on its own rather than assumed.
        /// </summary>
        [Test]
        public void ADelegatedFriendlyAction_IsDrawnOnTheSameTerms()
        {
            AssertTheDecidingSeatDrawsWhatTheHostAuthorizes(againstAnEnemy: false);
        }

        private static void AssertTheDecidingSeatDrawsWhatTheHostAuthorizes(bool againstAnEnemy)
        {
            var match = HanuUltimateTrickTests.DelegatedMatch.Stage(againstAnEnemy);
            var deciding = Compose(match, match.Decider);

            Assert.That(deciding.Hands.Model.DecidedForHand, Is.Not.Null,
                "The deciding seat is given the delegated hero's card to draw the controls on - " +
                "HA-Q15i, and the whole of this milestone.");
            Assert.That(deciding.Hands.Model.DecidedForHand.HeroId, Is.EqualTo(match.Delegated));
            Assert.That(
                deciding.Hands.Model.DecidedForHand.TrayCards.Select(card => card.CardInstanceId),
                Is.EqualTo(new[] { match.DelegatedCardId }),
                "and exactly that hero's one revealed resolving card, never their hand.");

            var authorized = deciding.Projection.AuthorizedCommands;
            Assert.That(
                DrawnStartCommands(deciding),
                Is.EquivalentTo(authorized
                    .Where(command => command.Kind == DirectorCommandKind.StartAction)
                    .Select(Describe))
                    .And.Not.Empty,
                "THE EQUALITY THIS MILESTONE IS ABOUT: the controls this seat can press are the " +
                "commands the host accepts from it, neither more nor fewer.");
            Assert.That(
                DrawnStartCommands(deciding).All(drawn =>
                    drawn.StartsWith(match.DelegatedCardId.Value + "/", System.StringComparison.Ordinal)),
                Is.True,
                "and every one of them is started off the DELEGATED hero's own card.");

            Assert.That(
                Chips(deciding.Hands, "FAST TRAVEL").Count,
                Is.EqualTo(deciding.Projection.AvailableFastTravelOptions.Count),
                "p.11's Fast Travel replacement is drawn where the host offers it and nowhere else.");
            Assert.That(
                Chips(deciding.Hands, "CLEAR").Count,
                Is.EqualTo(deciding.Projection.AvailableClearOptions.Count),
                "and so is p.13's Clear.");

            // 8.35l's rule: an offer the authority judged unstartable keeps its place and its label
            // and names its blocker, which must hold on a borrowed card exactly as on an owned one.
            foreach (var region in deciding.Round.Model.PrintedActionRegions
                .Where(candidate => candidate.Invocation == null))
            {
                var control = RegionControls(deciding.Hands)
                    .Single(candidate => ReferenceEquals(candidate.userData, region));
                Assert.That(control.enabledSelf, Is.False);
                Assert.That(control.tooltip, Is.EqualTo(region.AssistiveLabel));
                Assert.That(control.tooltip, Does.Contain(region.ActionLabel),
                    "A greyed delegated control still names what it is and why it is greyed.");
            }
        }

        /// <summary>
        /// p.12's Hold is one of the offers the targeted hero's own seat would have had, so it is
        /// drawn on the deciding seat - as END TURN, whose command is <c>PerformHold</c> - and on no
        /// other.
        /// </summary>
        [Test]
        public void ThePrintedHold_IsOfferedToTheDecidingSeatAndWithheldFromTheTargetedOne()
        {
            var match = HanuUltimateTrickTests.DelegatedMatch.Stage(againstAnEnemy: true);
            var deciding = Compose(match, match.Decider);
            var targeted = Compose(match, match.Delegated);

            var hold = deciding.Round.Model.EndTurnInvocation;
            Assert.That(hold, Is.Not.Null,
                "The deciding seat is drawn the turn-ending control of a turn that is not its own.");
            Assert.That(hold.Command.Kind, Is.EqualTo(DirectorCommandKind.PerformHold));
            Assert.That(
                deciding.Projection.AuthorizedCommands.Any(command =>
                    command.Kind == DirectorCommandKind.PerformHold),
                Is.True,
                "and the host accepts exactly that command from exactly that seat.");

            Assert.That(targeted.Round.Model.EndTurnInvocation, Is.Null,
                "while the targeted player is shown no way to end the turn their piece is taking: " +
                "the offer is absent from their projection rather than present and refused.");
            Assert.That(targeted.Round.Model.EndTurnCancelsOpenAttack, Is.False);
        }

        // -------------------------------------------------------------------------------------------
        // (c) The targeted seat: a dark card and nothing to press
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheTargetedSeatDrawsItsOwnCardDarkAndPressesNothing()
        {
            var match = HanuUltimateTrickTests.DelegatedMatch.Stage(againstAnEnemy: true);
            var targeted = Compose(match, match.Delegated);

            Assert.That(targeted.Hands.Model.DecidedForHand, Is.Null,
                "It decides nothing for anybody.");
            Assert.That(targeted.Hands.Model.ViewedHandDrawsActionControls, Is.False,
                "and its own resolving card is not the card it draws the controls on.");
            Assert.That(RegionControls(targeted.Hands), Is.Empty,
                "so there is no region on its tray at all - not a greyed one, not an inert one.");
            Assert.That(targeted.Round.Model.PrintedActionRegions, Is.Empty);
            Assert.That(targeted.Round.Model.Actions, Is.Empty);

            var own = targeted.Hands.Model.ViewedHand.TrayCards
                .Single(card => card.CardInstanceId.Equals(match.DelegatedCardId));
            Assert.That(own.IsResolving, Is.True,
                "The card is still in play and still drawn at full strength - it is the card the " +
                "turn is being taken on.");
            Assert.That(own.IsAwaitedAnswer, Is.False,
                "THE DARK HALF. The gold's grammar is \"this is yours to answer\", and while the " +
                "delegation stands this player may answer nothing.");
            Assert.That(own.AssistiveLabel, Does.Not.Contain("AWAITING YOUR ANSWER"),
                "and the assistive reading says the same thing the colour does.");
        }

        /// <summary>
        /// The same card, on the same frame, read from the two seats: lit and pressable on one, dark
        /// and inert on the other. One assertion rather than two, because the defect was precisely
        /// that both seats read the same answer.
        /// </summary>
        [Test]
        public void TheSameCardIsLitOnTheDecidingSeatAndDarkOnTheTargetedOne_OnOneFrame()
        {
            var match = HanuUltimateTrickTests.DelegatedMatch.Stage(againstAnEnemy: true);
            var deciding = Compose(match, match.Decider);
            var targeted = Compose(match, match.Delegated);

            Assert.That(deciding.Projection.Version, Is.EqualTo(targeted.Projection.Version),
                "Both readings are of one position, or they measure nothing.");

            var lit = deciding.Hands.Model.DecidedForHand.TrayCards.Single();
            var dark = targeted.Hands.Model.ViewedHand.TrayCards
                .Single(card => card.CardInstanceId.Equals(match.DelegatedCardId));

            Assert.That(lit.CardInstanceId, Is.EqualTo(dark.CardInstanceId));
            Assert.That(lit.IsAwaitedAnswer, Is.True);
            Assert.That(dark.IsAwaitedAnswer, Is.False);
            Assert.That(RegionControls(deciding.Hands), Is.Not.Empty);
            Assert.That(RegionControls(targeted.Hands), Is.Empty);
        }

        // -------------------------------------------------------------------------------------------
        // (d) The slot itself, and the card it is not
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The card is drawn beside the hand and never inside it: it names whose it is, carries no
        /// commitment command, and is not one of the cards the viewer could commit.
        /// </summary>
        [Test]
        public void TheDecidingSlot_NamesWhoseCardItIsAndIsNeverAHandCard()
        {
            var match = HanuUltimateTrickTests.DelegatedMatch.Stage(againstAnEnemy: true);
            var deciding = Compose(match, match.Decider);
            var hero = deciding.Projection.Setup.Heroes
                .Single(candidate => candidate.HeroId.Equals(match.Delegated));

            var slot = Descendants(deciding.Hands.Root)
                .Single(element => element.name == "gd-deciding-for-slot");
            var ribbon = slot.Q<Label>("gd-deciding-for-label");
            Assert.That(ribbon, Is.Not.Null);
            Assert.That(ribbon.text,
                Is.EqualTo(DelegatedActionPresentation.DecidingForLabel(hero.DisplayName)));
            Assert.That(ribbon.text, Does.Contain(hero.DisplayName.ToUpperInvariant()));

            Assert.That(deciding.Hands.Model.DecidedForHand.CanPass, Is.False);
            Assert.That(deciding.Hands.Model.DecidedForHand.Cards, Is.Empty,
                "It is a card to act off, never a hand to commit from.");
            Assert.That(deciding.Hands.Model.DecidedForHand.TrayCards.Single().IsSelectable, Is.False);
            Assert.That(
                deciding.Hands.Model.ViewedHand.TrayCards.Any(card =>
                    card.CardInstanceId.Equals(match.DelegatedCardId)),
                Is.False,
                "and it is not smuggled into the viewer's own tray, where it would read as theirs.");
        }

        /// <summary>
        /// The rewind across the drawn state. Milestone 8.71b put the undo watermark on the DECIDING
        /// hero, so the step back belongs to the player who made the choice; the targeted player has
        /// no press of their own inside the action to take back, and their HUD is offered none.
        /// </summary>
        [Test]
        public void TheRewindAcrossTheDrawnState_BelongsToTheDecidingSeat()
        {
            var match = HanuUltimateTrickTests.DelegatedMatch.Stage(againstAnEnemy: true);
            Assert.That(match.StartTheDelegatedAction(), Is.True,
                "Arrange: the delegated action has to open a pause for a rewind to cross one.");

            var deciding = Compose(match, match.Decider);
            var targeted = Compose(match, match.Delegated);

            Assert.That(deciding.Round.Model.SeatUndoAvailable, Is.True,
                "His seat may step back out of the choice his own press opened.");
            Assert.That(targeted.Projection.AuthorizedCommands, Is.Empty,
                "while the targeted seat may submit nothing inside the action at all.");
            Assert.That(RegionControls(targeted.Hands), Is.Empty);
        }

        // -------------------------------------------------------------------------------------------
        // (e) The narration
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheNarrationNamesBothHeroesOnEverySeatAtTheTable()
        {
            var match = HanuUltimateTrickTests.DelegatedMatch.Stage(againstAnEnemy: true);
            var deciding = HeroName(match, match.Decider);
            var delegated = HeroName(match, match.Delegated);

            foreach (var seat in match.Host.Seats.Seats)
            {
                var composed = Compose(match, seat.HeroId);
                var status = composed.Round.Model.Status;
                Assert.That(status, Does.Contain(deciding),
                    "Seat " + seat.Id + " must be able to read who is choosing.");
                Assert.That(status, Does.Contain(delegated),
                    "Seat " + seat.Id + " must be able to read whose action it is.");
                Assert.That(status, Does.Contain("CHOOSES FOR"));
                Assert.That(
                    status,
                    Does.Contain(seat.Owns(match.Decider)
                        ? DelegatedActionPresentation.DecidingActivity
                        : DelegatedActionPresentation.WaitingActivity),
                    "Seat " + seat.Id + ": one sentence, and only its last clause is the seat's own.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // (f) The end of it
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// HA-Q4: the delegation is spent when the delegated action completes, because the turn
        /// authority resolves the rewritten card after the whole action. The screen follows in one
        /// step - the card leaves the deciding seat's tray, and the targeted seat is a seat with its
        /// own controls again.
        /// </summary>
        [Test]
        public void WhenTheDelegatedActionCompletes_TheCardLeavesTheDecidingSeatAndControlReturns()
        {
            var match = HanuUltimateTrickTests.DelegatedMatch.Stage(againstAnEnemy: true);
            Assert.That(Compose(match, match.Decider).Hands.Model.DecidedForHand, Is.Not.Null,
                "Arrange: the slot has to be standing for its disappearance to mean anything.");

            match.DriveTheDelegatedActionToItsEnd();

            var afterwards = Compose(match, match.Decider);
            Assert.That(afterwards.Hands.Model.DecidedForHand, Is.Null,
                "The card leaves his tray the moment the action that borrowed it is over.");
            Assert.That(
                RegionControls(afterwards.Hands).Any(control =>
                    ((PrintedActionRegionViewModel)control.userData).SourceCardInstanceId
                        .Equals(match.DelegatedCardId)),
                Is.False,
                "and so does every control it carried.");
            Assert.That(
                DelegatedActionPresentation.Standing(afterwards.Projection).IsDefined, Is.False);

            var targeted = Compose(match, match.Delegated);
            Assert.That(
                DelegatedActionPresentation.IsDecidedElsewhere(
                    targeted.Projection, match.Delegated),
                Is.False,
                "The targeted player's own seat is its own again - nobody is deciding for it.");
            Assert.That(targeted.Hands.Model.DecidedForHand, Is.Null);
            Assert.That(
                Descendants(targeted.Hands.Root).Any(element =>
                    element.name == "gd-deciding-for-slot"),
                Is.False,
                "and no slot is left behind on either side of the table.");
        }

        // -------------------------------------------------------------------------------------------
        // (g) Every ordinary turn, unchanged
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The whole change is gated on a delegation standing, so an ordinary turn draws exactly what
        /// it always drew: no slot, the acting seat's own controls on its own card, and the gold on
        /// the card whose turn it is. Measured on the same match one step before the rewrite matters
        /// - the seat whose turn it is, with nothing delegated.
        /// </summary>
        [Test]
        public void AnUndelegatedTurnDrawsExactlyWhatItAlwaysDrew()
        {
            var match = HanuUltimateTrickTests.DelegatedMatch.Stage(againstAnEnemy: true);
            var actor = match.DriveToAnUndelegatedTurn();

            Assert.That(actor.IsDefined, Is.True,
                "The walk never reached an ordinary turn, so this case measures nothing. A walk " +
                "that stops reaching one is a coverage regression, not a reason to skip.");

            var acting = Compose(match, actor);
            Assert.That(
                DelegatedActionPresentation.Standing(acting.Projection).IsDefined, Is.False,
                "Arrange: an ordinary turn, with nothing delegated over it.");
            Assert.That(acting.Hands.Model.DecidedForHand, Is.Null);
            Assert.That(acting.Hands.Model.ViewedHandDrawsActionControls, Is.True);
            Assert.That(
                Descendants(acting.Hands.Root).Any(element =>
                    element.name == "gd-deciding-for-slot"),
                Is.False,
                "No slot is reserved, drawn empty, or left behind on an ordinary turn.");
            Assert.That(acting.Round.Model.Status, Does.Not.Contain("CHOOSES FOR"));
        }

        // -------------------------------------------------------------------------------------------
        // Composition
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A hero's name as the narration lane prints it. <c>RoundActionPresenter.HeroDisplayName</c>
        /// upper-cases every name it says out loud, which is the panel's own house voice rather than
        /// anything this milestone decided - so the comparison is made in that voice.
        /// </summary>
        private static string HeroName(HanuUltimateTrickTests.DelegatedMatch match, HeroId heroId) =>
            match.Host.Projection.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(heroId)).DisplayName.ToUpperInvariant();

        /// <summary>
        /// One seat's whole HUD reading: its own published projection, the round view model built
        /// from it, and the HUD's real hand view rendered from the same frame and fed the same
        /// regions the configured renderer feeds it.
        /// </summary>
        private static SeatSurface Compose(
            HanuUltimateTrickTests.DelegatedMatch match, HeroId heroId)
        {
            var seat = match.Host.Seats.FindByHero(heroId);
            var client = new SeatMatchClient(match.Host.ClientForSeat(seat.Id));
            var round = new RoundActionUnityView();
            new RoundActionPresenter(client, round, heroId).Refresh();
            var hands = new DirectorHandsUnityView();
            new DirectorHandsPresenter(client, hands, heroId).Refresh();
            hands.SetPrintedActionRegions(round.Model.PrintedActionRegions);
            return new SeatSurface(match.Host.ProjectionForSeat(seat.Id), round, hands);
        }

        private sealed class SeatSurface
        {
            internal SeatSurface(
                SeatProjection projection,
                RoundActionUnityView round,
                DirectorHandsUnityView hands)
            {
                Projection = projection;
                Round = round;
                Hands = hands;
            }

            internal SeatProjection Projection { get; }
            internal RoundActionUnityView Round { get; }
            internal DirectorHandsUnityView Hands { get; }
        }

        /// <summary>Every printed-action control actually built into this seat's tray.</summary>
        private static IReadOnlyList<Button> RegionControls(DirectorHandsUnityView hands) =>
            Descendants(hands.Root).OfType<Button>()
                .Where(button => button.name == "gd-printed-action-region")
                .ToArray();

        /// <summary>The start commands a player could actually press, as comparable strings.</summary>
        private static IReadOnlyList<string> DrawnStartCommands(SeatSurface surface) =>
            RegionControls(surface.Hands)
                .Select(button => (PrintedActionRegionViewModel)button.userData)
                .Where(region => region.Invocation != null && IsForwardable(surface, region))
                .Select(region => Describe(region.Invocation.Command))
                .ToArray();

        /// <summary>
        /// A drawn control only submits if the view would forward it, which is the same guard the
        /// pointer path runs - so a control this method counts is one a player can really press.
        /// </summary>
        private static bool IsForwardable(
            SeatSurface surface, PrintedActionRegionViewModel region) =>
            surface.Hands.PrintedActionRegions.Any(candidate => ReferenceEquals(candidate, region));

        private static IReadOnlyList<Button> Chips(DirectorHandsUnityView hands, string label) =>
            Descendants(hands.Root).OfType<Button>()
                .Where(button => button.name == "gd-action-replacement-chip" && button.text == label)
                .ToArray();

        private static string Describe(DirectorCommand command) =>
            command.CardInstanceId.Value + "/" + command.ActionDefinitionId.Value;

        private static IEnumerable<VisualElement> Descendants(VisualElement root)
        {
            if (root == null)
            {
                yield break;
            }

            yield return root;
            foreach (var child in root.Children())
            {
                foreach (var descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
