using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.35y in the real configured scene: the one case proving the kinetic pass actually
    /// plays on the shipped HUD rather than only in the Edit Mode fixtures that drive its presenter
    /// and its clock directly.
    ///
    /// <para><b>Why this fixture has to exist at all.</b> Every other 8.35y case composes a
    /// <c>KineticCuePresenter</c> and a fake view by hand, which proves the derivation and the
    /// interpolation and proves nothing about the scene. Three things only the real scene can answer:
    /// that <see cref="ConfiguredMatchSceneRuntime"/> composes a kinetic view at all, that the
    /// coordinator's appended "kinetics" panel is reached by an ordinary projection change from a
    /// real press, and that <see cref="KineticCueDriver"/> - the scene's first and only per-frame
    /// work - is attached and advancing the clock on its own. A cue that is derived correctly and
    /// never ticked is invisible, and no Edit Mode case can tell the difference.</para>
    ///
    /// <para><b>It earned its place immediately, and the finding is why the lunge is asserted
    /// hero-against-hero.</b> Written first against an attack on a MINION - the commonest attack in
    /// a match, and the one the SIXTEENTH amendment named ("tokens smacking each other") - this case
    /// failed, and the cause is upstream of the whole kinetic pass: an attack whose target is a
    /// minion carries no minion identity in the seat-public log at all. Every attack-resolution
    /// event is built by <c>CardActionResolutionEvent.FromAttackResolution</c>, which passes the
    /// actor and <c>TargetHeroId</c> but leaves <c>targetMinionId</c> at its default, so the row
    /// reaches <c>MatchLogEntry</c> with an attacker and no defender and the presenter correctly
    /// declines to invent one - no lunge, no flash. It is a one-line omission
    /// (<c>targetMinionId: resolution.TargetMinionId</c>; <c>RuleEventLogProjection</c> already
    /// writes <c>minion=</c> for a defined value, and <c>MatchLogEntry.SubjectMinionId</c> is
    /// documented as exactly "a target, a defeat, a Minion Battle removal"), but it is a Core change
    /// that moves the snapshot compatibility corpus, so it was recorded as a named gap with its
    /// measured cost rather than made here. The hero-against-hero attack is the arm the log fully
    /// described, so it is the arm this case pins.</para>
    ///
    /// <para><b>Milestone 8.35z made that one-line repair</b>, with the migrate-or-reject decision and
    /// the digest re-pin the corpus move owes - see <c>SnapshotCompatibilityCorpusTests</c>'s own
    /// record of it. A minion attack now reaches the log naming its defender and lunges like any
    /// other. This case is deliberately left on the HERO arm: it is the arm that was always fully
    /// described, so it stays the stable pin for "a lunge plays on the real HUD", and the minion arm
    /// is guarded where the defect actually was - on the event the authority builds
    /// (<c>MinionAttackResolutionTests.EveryAttackResolutionEvent_NamesTheMinionItTargeted</c>) and on
    /// the cue derived from it (<c>KineticCueTests</c>).</para>
    ///
    /// <para><b>The clock is advanced two ways on purpose.</b> One real frame proves the scene's
    /// driver is live - nothing in this fixture ticks anything for that assertion. The rest is
    /// advanced through <see cref="KineticCueUnityView.Tick"/> explicitly, because a case that
    /// waited on wall-clock frames to cross a 0.32 s slide would be asserting the frame rate of
    /// whatever machine ran it.</para>
    /// </summary>
    public sealed class KineticCuePlayModeTests
    {
        [UnityTest]
        public IEnumerator TheRealHud_SlidesThePieceItMovedAndLungesTheAttackItResolved()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var redHeroId = HeroOf(runtime, TeamSide.Red);
            var blueHeroId = HeroOf(runtime, TeamSide.Blue);
            var mover = KineticSubject.ForHero(blueHeroId);

            var view = runtime.KineticView;
            Assert.That(view, Is.Not.Null,
                "The configured scene composes a kinetic surface like any other panel.");
            var driver = Object.FindAnyObjectByType<KineticCueDriver>();
            Assert.That(driver, Is.Not.Null,
                "The scene's per-frame driver is attached by the composition root; without it every " +
                "cue would be derived correctly and never advance.");
            Assert.That(driver.FailureCount, Is.EqualTo(0));

            // Rule 2 of the kinetic contract, on the real surface: the first projection a client
            // sees primes the watermark and plays nothing.
            Assert.That(view.Playback.IsIdle, Is.True,
                "The opening projection primes and plays nothing.");

            // ---- SLIDE: round 1, the Blue Wasp closes the two-space gap to adjacency ----
            Commit(runtime, TeamSide.Red, WaspCardContent.Shock);
            Commit(runtime, TeamSide.Blue, WaspCardContent.MagneticDagger);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blueHeroId),
                "Blue's Magnetic Dagger at 12 acts before Red's Shock at 8.");
            Assert.That(view.Playback.IsIdle, Is.True,
                "Two real commitments leave the BOARD still. Milestone 8.35z11 did give a " +
                "commitment and its reveal journeys of their own, but they are HUD journeys - a card " +
                "arcing to a slot, a card turning over - and `Playback` is the piece clock. The " +
                "partition is KineticCueFamilies', and this assertion is what it buys: the board " +
                "reports motion when pieces move, and at no other time.");

            var movement = runtime.RoundView.Model.Actions.Single(value =>
                ActionKindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            var origin = runtime.Heroes.Single(hero => hero.HeroId.Equals(blueHeroId)).Coordinate;
            var redAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(redHeroId)).Coordinate;
            var step = runtime.PendingMovementChoice.LegalDestinations
                .First(value => HexDistanceQuery.Calculate(value.Destination, redAt) == 1);

            // No frame between the submission and these assertions, so the scene's own driver cannot
            // be advancing anything while they run.
            Assert.That(runtime.BoardPresenter.SubmitDestination(step.Destination), Is.True);

            // Milestone 8.35z11. THE ONE SEMANTIC CHANGE THIS FIXTURE HAD TO ABSORB. Before the
            // conductor, a projection change STARTED its cues; now it QUEUES them, and the head of the
            // queue begins on the next tick. `Tick(0f)` is that tick with no time passing - the
            // conductor starts a waiting scene whenever it is idle, and a zero delta advances no clock
            // - so every assertion below still runs at progress zero with no frame elapsed.
            view.Tick(0f);

            var route = view.Playback.RouteFor(mover);
            Assert.That(route, Is.Not.Null,
                "A move the log recorded puts the piece on a route rather than teleporting it.");
            Assert.That(route, Is.EqualTo(new[] { origin, step.Destination }),
                "Two waypoints, and exactly the two the log carries: the log keeps the first origin " +
                "and the last destination, so a slide travels the recorded ends and invents nothing " +
                "in between.");
            Assert.That(view.Playback.MotionFor(mover).HasRouteTravel, Is.True);

            // The scene's driver is live: one ordinary frame, nothing ticked by this fixture.
            var progressBefore = view.Playback.MotionFor(mover).RouteProgress;
            yield return null;
            Assert.That(driver.FailureCount, Is.EqualTo(0),
                "The driver isolates its tick and disables itself after three failures; a failure " +
                "here would mean the board had already fallen back to the pre-milestone redraw.");
            Assert.That(view.Playback.MotionFor(mover).RouteProgress,
                Is.Not.EqualTo(progressBefore),
                "The slide advanced across one real frame without this case ticking it, which is " +
                "the whole of what 'plays on the real HUD' means.");

            view.Tick(KineticTiming.SlideSeconds * 0.5f);
            var midSlide = view.Playback.MotionFor(mover);
            Assert.That(midSlide.HasRouteTravel, Is.True);
            Assert.That(midSlide.RouteProgress, Is.GreaterThan(0f).And.LessThan(1f),
                "Mid-slide the piece is genuinely between its two spaces.");

            view.Tick(KineticTiming.SlideSeconds);
            Assert.That(view.Playback.RouteFor(mover), Is.Null);
            Assert.That(view.Playback.MotionFor(mover).IsStill, Is.True,
                "A finished cue leaves the piece at the placement the projection alone decides - " +
                "there is no unwind step, because the view never integrated in the first place.");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(blueHeroId)).Coordinate,
                Is.EqualTo(step.Destination),
                "and the piece is where the projection says it stands, which it was the whole time.");

            // Round 1 is finished as filler; the journey resumes at round 2's attack.
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            Assert.That(runtime.SubmitEndTurn(), Is.True, "Red holds its turn.");
            for (var budget = 0; runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                Assert.That(budget, Is.LessThan(40),
                    "The end of round must complete within a bounded number of answers.");
                AnswerAnyEndOfRoundPause(runtime);
            }

            // ---- LUNGE: round 2, Blue attacks the now-adjacent Red hero ----
            Commit(runtime, TeamSide.Blue, WaspCardContent.Shock);
            Commit(runtime, TeamSide.Red, WaspCardContent.LiftUp);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId),
                "Red's Lift Up at 10 acts before Blue's Shock at 8.");
            Assert.That(runtime.SubmitEndTurn(), Is.True, "Red holds its turn.");
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(blueHeroId));

            var attacker = KineticSubject.ForHero(blueHeroId);
            var defender = KineticSubject.ForHero(redHeroId);
            var attack = runtime.RoundView.Model.Actions.Single(value =>
                ActionKindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack);
            Assert.That(runtime.RoundPresenter.Submit(attack), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds,
                Does.Contain(redHeroId), "The adjacent Red hero is a legal Shock target.");
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var redHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(value => value.Occupancy.HeroId.Equals(redHeroId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, redHeroView.transform.position), Is.True);
            yield return null;

            // p.11's Defense response stands; the attack has not resolved and so has written no
            // Attack row yet. Declining it completes the attack, which is the row the lunge reads.
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null);
            Assert.That(runtime.DeclineDefense(), Is.True);

            // As above: the conductor starts the queued moment on the next tick, and a zero delta is
            // a tick that advances nothing.
            view.Tick(0f);

            Assert.That(view.Playback.ActiveMotionCount, Is.GreaterThan(0),
                "The resolved attack left something playing on the board.");
            Assert.That(view.Conductor.ActiveScene?.Kind, Is.EqualTo(KineticSceneKind.Board),
                "and it is playing as ONE moment - the lunge and the flash it caused together.");
            Assert.That(view.Playback.LungeTargetFor(attacker), Is.EqualTo(defender),
                "The attacker leans into the piece the log says it struck, and at no other.");

            view.Tick(KineticTiming.LungeSeconds * 0.5f);
            Assert.That(view.Playback.MotionFor(attacker).LungeLean, Is.GreaterThan(0f),
                "Mid-lunge the attacker is displaced toward the defender.");

            view.Tick(KineticTiming.LongestPieceKineticSeconds + 0.1f);
            Assert.That(view.Playback.LungeTargetFor(attacker).IsDefined, Is.False,
                "and the lunge ends inside the vocabulary's own longest bound.");
            Assert.That(view.Playback.MotionFor(attacker).IsStill, Is.True);

            Assert.That(driver.FailureCount, Is.EqualTo(0),
                "Nothing in the pass threw across the whole journey.");
        }

        private static HeroId HeroOf(ConfiguredMatchSceneRuntime runtime, TeamSide side) =>
            runtime.Setup.Heroes.Single(hero => hero.TeamSide == side).HeroId;

        /// <summary>Headless filler commitment for turns that are not this case's subject.</summary>
        private static void Commit(
            ConfiguredMatchSceneRuntime runtime, TeamSide side, CardDefinitionId definitionId)
        {
            UiPlayer.EndAnyTurnInProgress(runtime);

            var heroId = runtime.Setup.Heroes.Single(hero => hero.TeamSide == side).HeroId;
            var card = runtime.Cards.Single(value =>
                value.OwnerId.Equals(heroId) &&
                value.State.Location == CardLocation.Hand &&
                value.DefinitionReference.DefinitionId.Equals(definitionId));
            Assert.That(runtime.SelectCommitmentCard(card.Id), Is.True,
                $"{definitionId.Value} must be selectable from the {side} hand.");
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
        }

        /// <summary>
        /// Answers whichever end-of-round pause stands with its first projected option, then
        /// advances. Filler for a journey whose subject is elsewhere.
        /// </summary>
        private static void AnswerAnyEndOfRoundPause(ConfiguredMatchSceneRuntime runtime)
        {
            if (runtime.PendingMinionBattleChoice != null)
            {
                var battle = runtime.MinionChoiceView.Model;
                if (battle != null && battle.Candidates.Count > 0)
                {
                    Assert.That(runtime.SubmitMinionChoiceCandidate(
                        battle.Candidates.First(value => value.Destination == null).MinionId),
                        Is.True);
                }

                Assert.That(runtime.ConfirmMinionChoice(), Is.True);
                return;
            }

            if (runtime.PausedSystemTransitionOptions.Count > 0)
            {
                Assert.That(
                    runtime.AnswerPausedSystemTransition(runtime.PausedSystemTransitionOptions[0]),
                    Is.True);
                return;
            }

            Assert.That(runtime.ContinueEndOfRound(), Is.True);
        }

        private static ActionKind ActionKindOf(
            ConfiguredMatchSceneRuntime runtime, ActionDefinitionId actionId)
        {
            var offer = runtime.AvailableTurnActions.Single(value =>
                value.ActionDefinitionId.Equals(actionId));
            return runtime.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(actionId)).Kind;
        }

        private static IEnumerator LoadEngagedScene()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedCardContent =
                SceneCardContent.VerifiedEngagedMirror;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }
    }
}
