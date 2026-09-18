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
            var view = new PhaseBannerUnityView(() => new VisualElement());
            var presenter = new PhaseBannerPresenter(new DirectorMatchClient(session), view);
            presenter.Refresh();
            Assert.That(view.IsShowing, Is.False, "The first observation primes.");

            var red = session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Red &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.LiftUp));
            var blue = session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Blue &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.StopProjectiles));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.IsDefined, Is.True,
                "Both commitments in, a hero is acting: the transition a banner would announce.");

            presenter.YieldsWhen = () => true;
            presenter.Refresh();
            Assert.That(view.IsShowing, Is.False, "An expanded window opening here is the thing nobody misses.");
            Assert.That(presenter.LastYielded.HasBanner, Is.True);
            Assert.That(presenter.LastAnnounced.HasBanner, Is.False);

            presenter.YieldsWhen = () => false;
            presenter.Refresh();
            Assert.That(view.IsShowing, Is.False, "The yielded transition is not owed later.");
        }
    }
}
