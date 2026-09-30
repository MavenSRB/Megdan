using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.47. The phase banner's detector and view, on plain values: which transition is
    /// announced to which viewer, that priming announces nothing, that a key never shows twice, and
    /// that the surface draws what it is handed without ever taking the pointer.
    /// </summary>
    [TestFixture]
    public sealed class PhaseBannerTests
    {
        private static readonly HeroId Sabina = new HeroId("hero-sabina");
        private static readonly HeroId Tali = new HeroId("hero-tali");

        private static string Name(HeroId hero) => hero.Equals(Sabina) ? "Sabina" : hero.Equals(Tali) ? "Tali" : null;

        private static PhaseSnapshot Committing(int round, int turn) =>
            new PhaseSnapshot(round, turn, isCommitting: true, actingHero: default, isMinionBattle: false);

        private static PhaseSnapshot Resolving(int round, int turn, HeroId actor) =>
            new PhaseSnapshot(round, turn, isCommitting: false, actingHero: actor, isMinionBattle: false);

        private static PhaseSnapshot Between(int round, int turn) =>
            new PhaseSnapshot(round, turn, isCommitting: false, actingHero: default, isMinionBattle: false);

        private static PhaseSnapshot Battle(int round) =>
            new PhaseSnapshot(round, 4, isCommitting: false, actingHero: default, isMinionBattle: true);

        // Milestone 9.28: 8.35g's release pause. The acknowledgement that raised it empties the
        // selection, so nobody is acting while it stands.
        private static PhaseSnapshot Releasing(int round, int turn, HeroId actor) =>
            new PhaseSnapshot(round, turn, isCommitting: false, actingHero: default, isMinionBattle: false,
                releasingHero: actor);

        [Test]
        public void TheFirstObservation_PrimesAndAnnouncesNothing()
        {
            var detector = new PhaseBannerCueDetector();
            Assert.That(detector.Observe(Committing(1, 1), Sabina, Name).HasBanner, Is.False,
                "A client that has only just composed has watched nothing change.");
            Assert.That(detector.Observe(Committing(1, 1), Sabina, Name).HasBanner, Is.False,
                "and the state it was primed on is already announced.");
        }

        [Test]
        public void ACommitmentPhase_IsAnnouncedOncePerTurn_WithTheRoundInItsEyebrow()
        {
            var detector = new PhaseBannerCueDetector();
            detector.Observe(Between(1, 1), Sabina, Name);
            var banner = detector.Observe(Committing(2, 1), Sabina, Name);
            Assert.That(banner.Line, Is.EqualTo(PhaseBannerLabels.CommitLine));
            Assert.That(banner.Eyebrow, Is.EqualTo("ROUND 2 · TURN 1"),
                "Round start is bundled into the round's first commit banner rather than announced alone.");
            Assert.That(detector.Observe(Committing(2, 1), Sabina, Name).HasBanner, Is.False,
                "The same turn's commitment is never announced twice.");
            Assert.That(detector.Observe(Committing(2, 2), Sabina, Name).Eyebrow, Is.EqualTo("ROUND 2 · TURN 2"));
        }

        [Test]
        public void TheViewersOwnTurn_ReadsYourTurn_AndAnotherHerosTurn_IsNotAnnounced()
        {
            var detector = new PhaseBannerCueDetector();
            detector.Observe(Committing(1, 1), Sabina, Name);
            var own = detector.Observe(Resolving(1, 1, Sabina), Sabina, Name);
            Assert.That(own.Eyebrow, Is.EqualTo(PhaseBannerLabels.ResolveOwnEyebrow));
            Assert.That(own.Line, Is.EqualTo(PhaseBannerLabels.ResolveOwnLine));
            Assert.That(detector.Observe(Resolving(1, 1, Sabina), Sabina, Name).HasBanner, Is.False,
                "A turn is announced when it begins, not on every refresh while it resolves.");

            // Milestone 8.50, the user after the 2026-09-18 playtest: "Just when a player is up."
            // Another hero's turn is not this player's moment, so nothing is said about it.
            Assert.That(detector.Observe(Resolving(1, 2, Tali), Sabina, Name).HasBanner, Is.False,
                "Another seat's turn is not announced to this seat.");
            Assert.That(detector.Observe(Resolving(1, 3, Sabina), Sabina, Name).Eyebrow,
                Is.EqualTo(PhaseBannerLabels.ResolveOwnEyebrow),
                "and the viewer's next own turn is still announced after the silent one.");
        }

        [Test]
        public void TheDirector_IsUpForNobody_AndHearsNoTurnBanner()
        {
            var detector = new PhaseBannerCueDetector();
            detector.Observe(Committing(1, 1), default, Name);
            Assert.That(detector.Observe(Resolving(1, 1, Sabina), default, Name).HasBanner, Is.False,
                "Milestone 8.50: with no other-seat line left, the Director is told of no turn at all.");
            Assert.That(detector.Observe(Committing(1, 2), default, Name).Line,
                Is.EqualTo(PhaseBannerLabels.CommitLine),
                "but still sees each commitment and the Minion Battle.");
        }

        /// <summary>
        /// Milestone 9.28. The viewer's own finished turn is announced once, when it starts waiting
        /// on its release - not on every refresh while it waits, not again when a rewind inside the
        /// same turn reaches the pause a second time, and never for another seat's release.
        /// </summary>
        [Test]
        public void TheViewersOwnRelease_IsAnnouncedOncePerTurn_ToThatSeatAlone()
        {
            var detector = new PhaseBannerCueDetector();
            detector.Observe(Resolving(1, 1, Sabina), Sabina, Name);
            var banner = detector.Observe(Releasing(1, 1, Sabina), Sabina, Name);
            Assert.That(banner.Eyebrow, Is.EqualTo(PhaseBannerLabels.EndTurnEyebrow));
            Assert.That(banner.Line, Is.EqualTo(PhaseBannerLabels.EndTurnLine));
            Assert.That(PhaseBannerLabels.IsEndTurn(banner), Is.True);
            Assert.That(detector.Observe(Releasing(1, 1, Sabina), Sabina, Name).HasBanner, Is.False,
                "Not on every refresh while the release waits.");

            // Undo steps back behind the pause and the turn reaches it again: the same turn, so the
            // banner is not repeated - the flashing control still says it.
            Assert.That(detector.Observe(Resolving(1, 1, Sabina), Sabina, Name).HasBanner, Is.False);
            Assert.That(detector.Observe(Releasing(1, 1, Sabina), Sabina, Name).HasBanner, Is.False);

            Assert.That(detector.Observe(Releasing(1, 2, Tali), Sabina, Name).HasBanner, Is.False,
                "Another seat's release is not this seat's moment.");
            Assert.That(PhaseBannerLabels.IsEndTurn(detector.Observe(Releasing(1, 3, Sabina), Sabina, Name)),
                Is.True, "and the viewer's next own release is announced after the silent one.");
            Assert.That(PhaseBannerLabels.IsEndTurn(PhaseBannerLabels.Commit(1, 1)), Is.False);
        }

        [Test]
        public void TheDirector_IsNotToldToEndAnybodysTurn()
        {
            var detector = new PhaseBannerCueDetector();
            detector.Observe(Resolving(1, 1, Sabina), default, Name);
            Assert.That(detector.Observe(Releasing(1, 1, Sabina), default, Name).HasBanner, Is.False,
                "The Director answers for everybody and is up for nobody, as 8.50 reads the turn banner.");
        }

        [Test]
        public void ATurnsEnd_IsNotAnnounced()
        {
            var detector = new PhaseBannerCueDetector();
            detector.Observe(Resolving(1, 1, Sabina), Sabina, Name);
            Assert.That(detector.Observe(Between(1, 1), Sabina, Name).HasBanner, Is.False,
                "Nothing stands between turns; the next banner says the turn ended.");
        }

        [Test]
        public void TheMinionBattle_IsAnnouncedOncePerRound_AndOutranksTheOthers()
        {
            var detector = new PhaseBannerCueDetector();
            detector.Observe(Resolving(1, 4, Tali), Sabina, Name);
            var banner = detector.Observe(Battle(1), Sabina, Name);
            Assert.That(banner.Eyebrow, Is.EqualTo("END OF ROUND 1"));
            Assert.That(banner.Line, Is.EqualTo(PhaseBannerLabels.MinionBattleLine));
            Assert.That(detector.Observe(Battle(1), Sabina, Name).HasBanner, Is.False);

            // A refresh that carries a whole turn's worth of change tells the player where the
            // game IS: the battle standing now, not the resolve it passed through.
            var jumped = new PhaseBannerCueDetector();
            jumped.Observe(Committing(1, 4), Sabina, Name);
            var both = jumped.Observe(Battle(1), Sabina, Name);
            Assert.That(both.Line, Is.EqualTo(PhaseBannerLabels.MinionBattleLine));
        }

        [Test]
        public void ATransitionPassedThroughInOneRefresh_IsNotOwedLater()
        {
            var detector = new PhaseBannerCueDetector();
            detector.Observe(Committing(1, 1), Sabina, Name);
            // The bots resolved the whole turn between two frames: the viewer sees the battle.
            Assert.That(detector.Observe(Battle(1), Sabina, Name).Line, Is.EqualTo(PhaseBannerLabels.MinionBattleLine));
            // Then the next commitment, and only that.
            var next = detector.Observe(Committing(2, 1), Sabina, Name);
            Assert.That(next.Eyebrow, Is.EqualTo("ROUND 2 · TURN 1"));
        }

        [Test]
        public void Repriming_SilencesTheNextObservation()
        {
            var detector = new PhaseBannerCueDetector();
            detector.Observe(Committing(1, 1), Sabina, Name);
            detector.Reprime();
            Assert.That(detector.Observe(Resolving(1, 1, Sabina), Sabina, Name).HasBanner, Is.False,
                "A rebound seat is joining a state, not watching it change.");
            Assert.That(detector.Observe(Committing(1, 2), Sabina, Name).HasBanner, Is.True);
        }

        [Test]
        public void Labels_NameTheConsequence()
        {
            Assert.That(PhaseBannerLabels.Commit(3, 2).Key, Is.EqualTo("commit:3:2"));
            Assert.That(() => new PhaseBannerViewModel("k", "eyebrow", " "), Throws.ArgumentException);
            Assert.That(() => new PhaseBannerViewModel(" ", "eyebrow", "line"), Throws.ArgumentException);
        }

        [Test]
        public void TheView_DrawsTheWordsOnTheRoot_InBloodRedWithAnOutline_AndTakesNoPointer()
        {
            var root = new VisualElement();
            var view = new PhaseBannerUnityView(() => root);
            Assert.That(view.Root.parent, Is.Null, "Kept detached until a banner stands.");

            view.Show(PhaseBannerLabels.Commit(2, 3));

            Assert.That(view.Root.parent, Is.SameAs(root));
            Assert.That(view.IsShowing, Is.True);
            Assert.That(view.EyebrowText, Is.EqualTo("ROUND 2 · TURN 3"));
            Assert.That(view.LineText, Is.EqualTo(PhaseBannerLabels.CommitLine));
            Assert.That(view.Root.pickingMode, Is.EqualTo(PickingMode.Ignore));
            var line = view.Root.Q<Label>(PhaseBannerUnityView.LineName);
            Assert.That(line, Is.Not.Null);
            Assert.That(line.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(line.style.color.value, Is.EqualTo(PhaseBannerUnityView.BloodRed));
            Assert.That(line.style.unityTextOutlineColor.value, Is.EqualTo(Color.black));
            Assert.That(line.style.unityTextOutlineWidth.value, Is.EqualTo(PhaseBannerUnityView.OutlineWidth));
            Assert.That(line.style.fontSize.value.value, Is.EqualTo(PhaseBannerUnityView.LineFontSize));
            Assert.That(view.Root.Q<VisualElement>(PhaseBannerUnityView.PlateName).style.backgroundColor.value,
                Is.EqualTo(PhaseBannerUnityView.Plate));

            view.Hide();
            Assert.That(view.IsShowing, Is.False);
            Assert.That(view.Root.parent, Is.Null, "Taken down means detached, so no sweep sees a hidden element.");
        }

        [Test]
        public void TheView_WithNoRootYet_ShowsNothingAndThrowsNothing()
        {
            var view = new PhaseBannerUnityView(() => null);
            view.Show(PhaseBannerLabels.MinionBattle(1));
            Assert.That(view.IsShowing, Is.False);
        }

        /// <summary>
        /// Milestone 8.46e, owed by 8.47: the banner yields to the decision window opening expanded on
        /// the same refresh, and the transition it yielded is not owed later.
        /// </summary>
        [Test]
        public void TheBanner_YieldsToAnExpandedWindowOpeningOnTheSameRefresh_AndIsNotOwedLater()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var red = session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Red &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.LiftUp));
            var blue = session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Blue &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.StopProjectiles));
            // Milestone 8.50: only the player who is up hears a turn banner, so the viewer is the
            // Red seat whose Lift Up acts first - the Director would hear nothing to yield.
            var view = new PhaseBannerUnityView(() => new VisualElement());
            var presenter = new PhaseBannerPresenter(new DirectorMatchClient(session), view, red.OwnerId);
            presenter.Refresh();
            Assert.That(view.IsShowing, Is.False, "The first observation primes.");

            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(red.OwnerId),
                "Both commitments in, the viewer's own hero is acting: the transition a banner " +
                "would announce as YOUR TURN.");

            presenter.YieldsWhen = () => true;
            presenter.Refresh();
            Assert.That(view.IsShowing, Is.False, "An expanded window opening here is the thing nobody misses.");
            Assert.That(presenter.LastYielded.HasBanner, Is.True);
            Assert.That(presenter.LastAnnounced.HasBanner, Is.False);

            presenter.YieldsWhen = () => false;
            presenter.Refresh();
            Assert.That(view.IsShowing, Is.False, "The yielded transition is not owed later.");
        }

        /// <summary>
        /// Milestone 9.28, the user on 8.35g's release pause: "when the player seat reach that state
        /// ... big blood red lights should say 'end turn', same way they say 'commit a card'". The
        /// seat whose own finished turn waits on END TURN is told so in the banner's words, no other
        /// seat is, and the banner comes down the moment the press lands.
        /// </summary>
        [Test]
        public void TheViewersOwnFinishedTurn_SaysEndTurn_UntilItIsReleased()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var (red, blue) = LiftUpAndStopProjectiles(session);
            var redRoot = new VisualElement();
            var redView = new PhaseBannerUnityView(() => redRoot);
            var redBanner = new PhaseBannerPresenter(new DirectorMatchClient(session), redView, red.OwnerId);
            var blueRoot = new VisualElement();
            var blueView = new PhaseBannerUnityView(() => blueRoot);
            var blueBanner = new PhaseBannerPresenter(new DirectorMatchClient(session), blueView, blue.OwnerId);

            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(red.OwnerId),
                "Arrange: Red's Lift Up acts first.");
            redBanner.Refresh();
            blueBanner.Refresh();

            var hold = RoundActionPresenter.CreateViewModel(session.Projection, red.OwnerId).EndTurnInvocation;
            Assert.That(hold?.Command.Kind, Is.EqualTo(DirectorCommandKind.PerformHold),
                "Arrange: an untouched card's first END TURN is p.12's Hold.");
            ConfiguredMatchFixtures.Accept(session, hold.Command);
            Assert.That(session.Projection.PendingTurnRelease?.ActorId, Is.EqualTo(red.OwnerId),
                "Arrange: the finished turn now waits on Red's own release (8.35g).");

            redBanner.Refresh();
            blueBanner.Refresh();
            Assert.That(redView.IsShowing, Is.True,
                "The seat whose finished turn waits on its own END TURN is told so.");
            Assert.That(redView.LineText, Is.EqualTo("END TURN"));
            Assert.That(redView.EyebrowText, Is.EqualTo("TURN COMPLETE"));
            Assert.That(blueView.IsShowing, Is.False,
                "Another seat is not up: 8.50's rule, nothing is said to it.");

            var release = RoundActionPresenter.CreateViewModel(session.Projection, red.OwnerId).EndTurnInvocation;
            Assert.That(release?.Command.Kind, Is.EqualTo(DirectorCommandKind.ReleaseTurn));
            ConfiguredMatchFixtures.Accept(session, release.Command);
            redBanner.Refresh();
            Assert.That(redView.IsShowing, Is.False,
                "Pressed, the banner comes down at once instead of asking for what was just done.");
        }

        /// <summary>
        /// Milestone 9.28. Over the wire one END TURN press is a Hold, a round trip, then the release
        /// (8.35z4's continuation), so the match passes THROUGH the seat's own release pause after
        /// the player has already pressed. Announcing END TURN there would tell them to do what they
        /// just did, and it is never announced: the round panel refreshes first and sends the release
        /// on the push that brings the pause, and a seat waiting on the host is not published a
        /// release it could answer, so the banner never sees one standing - then or later.
        /// </summary>
        [Test]
        public void AReleaseTheSeatHasAlreadyPressedFor_IsNeverAnnounced()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            var (red, blue) = LiftUpAndStopProjectiles(session);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));

            var client = new DeferredSeatClient(session, red.OwnerId);
            var round = new RoundActionPresenter(client, new RoundActionUnityView(), red.OwnerId);
            var root = new VisualElement();
            var view = new PhaseBannerUnityView(() => root);
            var banner = new PhaseBannerPresenter(client, view, red.OwnerId);
            // The coordinator's own order (MatchFeatureCoordinator.RefreshAll): the round panel,
            // then the banner.
            void Push()
            {
                round.Refresh();
                banner.Refresh();
            }

            Push();
            Assert.That(round.SubmitEndTurn(), Is.True, "Arrange: the press sends p.12's Hold.");

            client.DeliverPending();
            Assert.That(session.Projection.PendingTurnRelease?.ActorId, Is.EqualTo(red.OwnerId),
                "Arrange: the Hold landed and the match stands at Red's own release.");
            Push();
            Assert.That(client.InFlight, Is.True,
                "Arrange: the round panel resumed the press and sent the release on this push.");
            Assert.That(client.Projection.PendingTurnRelease, Is.Null,
                "Arrange: a seat waiting on the host is not published a release to answer.");
            Assert.That(view.IsShowing, Is.False, "The player already pressed END TURN.");

            client.DeliverPending();
            Push();
            Assert.That(session.Projection.PendingTurnRelease, Is.Null,
                "Arrange: the same press's release landed on the next round trip.");
            Assert.That(view.IsShowing, Is.False, "and the release is not announced later.");
            Assert.That(banner.LastAnnounced.HasBanner, Is.False);
        }

        private static (CardInstance Red, CardInstance Blue) LiftUpAndStopProjectiles(MatchSession session)
        {
            var red = session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Red &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.LiftUp));
            var blue = session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Blue &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.StopProjectiles));
            return (red, blue);
        }

        /// <summary>
        /// A seat's client whose submissions are SENT rather than decided, as over the wire: while a
        /// command is in flight the seat renders its own stripped projection with input disabled, and
        /// <see cref="DeliverPending"/> is the host's answer arriving. The same shape as
        /// <c>MatchFeaturePresenterTests.DeferredMatchClient</c>, bound to a named hero's seat.
        /// </summary>
        private sealed class DeferredSeatClient : IMatchClient
        {
            private readonly IMatchSession session;
            private readonly MatchHost host;
            private readonly HeroId heroId;
            private DirectorCommand pending;

            public DeferredSeatClient(IMatchSession session, HeroId heroId)
            {
                this.session = session;
                this.heroId = heroId;
                host = new MatchHost(session);
            }

            public bool IsDirector => false;

            public bool InFlight => pending != null;

            public IMatchReadModel Projection
            {
                get
                {
                    if (pending == null)
                    {
                        return session.Projection;
                    }

                    var seat = host.Seats.FindByHero(heroId);
                    Assert.That(NetworkSeatProjectionCodec.TryDecode(
                        NetworkSeatProjectionCodec.Encode(host.ProjectionForSeat(seat.Id)),
                        out var record), Is.True);
                    var waiting = new NetworkMatchReadModel(
                        host.Projection, MatchContentCatalogs.ForThisBuild());
                    waiting.Replace(record, false);
                    return waiting;
                }
            }

            public MatchClientResult Submit(DirectorCommand command)
            {
                if (pending != null)
                {
                    return new MatchClientResult(false, "Waiting for the host.", Projection, true);
                }

                pending = command;
                return new MatchClientResult(true, "Sent to the host.", Projection, false);
            }

            public MatchClientResult SubmitAgreement(TeamAgreementCommand command) =>
                new MatchClientResult(false, "Not used by this fixture.", Projection, true);

            public MatchClientResult SubmitUndo() =>
                new MatchClientResult(false, "Not used by this fixture.", Projection, true);

            public void DeliverPending()
            {
                if (pending == null)
                {
                    return;
                }

                var command = pending;
                pending = null;
                session.Execute(command);
            }
        }

        /// <summary>
        /// Milestone 9.28, the user: "all those warnings should linger little bit longer". Every
        /// banner holds a second longer than 8.47's 1.3 s.
        /// </summary>
        [Test]
        public void EveryBanner_HoldsASecondLongerThanItDidAt847()
        {
            Assert.That(PhaseBannerUnityView.HoldMs, Is.EqualTo(2300L));
            Assert.That(PhaseBannerUnityView.TotalMs, Is.EqualTo(2900L),
                "Fade in, hold and fade out: 2.9 s in all, up from 1.9 s.");
        }
    }
}
