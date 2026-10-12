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
using UnityEngine.UIElements;

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

            // Milestone 8.35z11. THE SEMANTIC CHANGE THIS FIXTURE HAD TO ABSORB, and the wave's first
            // complete Play Mode run is what got it right. Before the conductor a projection change
            // STARTED its cues; now it queues them and the conductor plays ONE MOMENT AT A TIME. This
            // fixture's own two commitments are moments now - a CardCommit scene each, plus their
            // reveals - so they sit in the queue AHEAD of the slide, and a single `Tick(0f)` starts
            // the head of that queue rather than the move. The run reported it exactly:
            // `active=commit:primitive-hero pending=3`, with the spotlight at None and nothing
            // deferred, so this is the conductor doing its job and not a defect in it.
            //
            // So drain to the moment under test instead of assuming it is first. The slide then
            // starts on the tick that reaches it, which is where every assertion below reads from.
            const float Frame = 1f / 60f;
            var drainedFrames = 0;
            while (view.Playback.RouteFor(mover) == null && drainedFrames < 600)
            {
                view.Tick(Frame);
                drainedFrames++;
            }

            Assert.That(drainedFrames, Is.LessThan(600),
                "The slide must reach the head of the conductor's queue within ten seconds of " +
                "presentation time; if it never does, the queue is stuck rather than merely staged.");

            var route = view.Playback.RouteFor(mover);
            Assert.That(route, Is.Not.Null,
                "A move the log recorded puts the piece on a route rather than teleporting it.");
            Assert.That(route, Is.EqualTo(new[] { origin, step.Destination }),
                "Two waypoints, and exactly the two the log carries: the log keeps the first origin " +
                "and the last destination, so a slide travels the recorded ends and invents nothing " +
                "in between.");
            Assert.That(view.Playback.MotionFor(mover).HasRouteTravel, Is.True);

            // MID-SLIDE IS READ WHERE THE DRAIN LEFT IT, not at a fixed offset past a real frame,
            // and the wave's first complete Play Mode run is why. One real frame in batch mode can
            // carry a large `Time.deltaTime`; adding a fixed half-duration on top of it pushed this
            // assertion past the END of a 0.32 s slide, and the fixture then reported a finished
            // slide as a missing one. The drain above stops on the tick that starts the slide, so
            // exactly one frame of it has played here - genuinely between the two spaces, by
            // construction rather than by arithmetic that depends on the frame rate.
            var midSlide = view.Playback.MotionFor(mover);
            Assert.That(midSlide.HasRouteTravel, Is.True);
            Assert.That(midSlide.RouteProgress, Is.GreaterThan(0f).And.LessThan(1f),
                "Mid-slide the piece is genuinely between its two spaces.");

            // The scene's driver is live: one ordinary frame, nothing ticked by this fixture.
            var progressBefore = midSlide.RouteProgress;
            yield return null;
            Assert.That(driver.FailureCount, Is.EqualTo(0),
                "The driver isolates its tick and disables itself after three failures; a failure " +
                "here would mean the board had already fallen back to the pre-milestone redraw.");
            Assert.That(view.Playback.MotionFor(mover).RouteProgress,
                Is.Not.EqualTo(progressBefore),
                "The slide advanced across one real frame without this case ticking it, which is " +
                "the whole of what 'plays on the real HUD' means.");

            // AND THE THIRD CLAMP LESSON THIS FIXTURE PAID FOR: every clock in this stack caps a
            // SINGLE delta at 0.25 s, so "tick the whole duration" cannot finish anything longer -
            // a 0.32 s slide included. Advance in frame-sized steps until it is genuinely over
            // rather than handing the clock one number and trusting it.
            var settleFrames = 0;
            while (view.Playback.RouteFor(mover) != null && settleFrames < 600)
            {
                view.Tick(Frame);
                settleFrames++;
            }

            Assert.That(settleFrames, Is.LessThan(600),
                "The slide must end inside its own declared duration rather than running on.");
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

            // As above, and with the wave's second lesson folded in: BOTH clocks have to be driven.
            // A resolved attack now also asks milestone 8.35z12's spotlight for the stage, and the
            // real scene advances two drivers per frame - the kinetic view's and the spotlight's.
            // While the spotlight is TRAVELLING (Requested, Entering, Exiting) the cue layer holds
            // its cues and the conductor starts nothing; the moment it HOLDS, the cues are released
            // and play framed, which is the whole point of the sequence. A fixture that ticked only
            // the kinetic view would sit at Requested for ever and read as a dead conductor.
            var attackFrames = 0;
            while (!view.Playback.LungeTargetFor(attacker).IsDefined && attackFrames < 600)
            {
                runtime.SpotlightPresenter.Tick(Frame);
                view.Tick(Frame);
                attackFrames++;
            }

            Assert.That(attackFrames, Is.LessThan(600),
                "The lunge must reach the board once the spotlight stops travelling.");
            Assert.That(runtime.SpotlightPresenter.Stage.Phase,
                Is.EqualTo(AttackSpotlightPhase.Held),
                "and it reaches it DURING the hold, not after the spotlight has closed - the smack " +
                "belongs in the shot, framed, with both faces up.");

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

        /// <summary>
        /// Milestone 8.35z47. THE CASE THAT DID NOT EXIST WHEN THE USER REPORTED "Saw no
        /// breathing, just solid colors": the awaited breath sampled on CONSECUTIVE REAL FRAMES,
        /// with nothing in this method touching the clock.
        ///
        /// <para><b>Why every proof the breath already had was the wrong shape.</b> Milestone
        /// 8.35z39 shipped eight cases and the integration batch shipped a screenshot instrument, and
        /// between them they established, correctly, that brightness is a function of time:
        /// <c>KineticPlaybackTests</c> advances a <c>KineticPlayback</c> by hand and watches the
        /// number move, and <c>MatchHudPresentationTests</c> pins the colours the function produces.
        /// Not one of them asserted that TIME ITSELF ADVANCES on the shipped surface while a player
        /// sits looking at an awaited element - which is the only condition the treatment exists
        /// for. A frozen clock and a correct curve are indistinguishable to every one of those
        /// cases, and would have been indistinguishable here too.</para>
        ///
        /// <para><b>What this asserts, and deliberately does not.</b> It reads the host's own
        /// <c>resolvedStyle</c> - what the panel actually resolved for painting, not what the view
        /// intended - once per real frame, with only <c>yield return null</c> between reads. The
        /// scene's own <see cref="KineticCueDriver"/> is the only thing advancing anything. It then
        /// asserts the sampled colour actually MOVED and stayed on the breath's own segment (the hue
        /// is held constant by construction, so a colour that leaves the segment is a different
        /// defect than a colour that does not move).</para>
        ///
        /// <para><b>It passes on shipped 8.35z39, and that is the finding.</b> The prime hypothesis
        /// behind this corrective was a cue-gated clock; it is false, and this case is what says so
        /// on the real surface. The user's report was accurate and its cause was the CARRIER, not the
        /// clock - see <c>MatchHudPresentationTests</c>'s band-width guard, which is the case that
        /// does fail on shipped. This one is here so that a future change CAN break the clock and be
        /// caught, which until now nothing could.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheAwaitedBreath_ActuallyMovesAcrossConsecutiveRealFrames()
        {
            yield return LoadEngagedScene();
            var root = Root();
            var driver = Object.FindAnyObjectByType<KineticCueDriver>();
            Assert.That(driver, Is.Not.Null,
                "Without the scene's own driver nothing below measures the shipped path.");

            // Settle, so the HUD has composed and the awaited treatments are on screen.
            for (var frame = 0; frame < 8; frame++)
            {
                yield return null;
            }

            var hosts = KineticCueUnityView.FindAwaitedShineHosts(root.Hud.RootElement);
            Assert.That(hosts, Is.Not.Empty,
                "The engaged staging must show an awaited element, or there is no breath to sample " +
                "and this case would pass by measuring nothing.");

            // Rails now breathe in their stable player accent, not the retired gold card ring.
            var host = hosts.First(value => value.name == "gd-rail-awaited-glow" && value.userData is Color);
            var accent = (Color)host.userData;
            System.Func<VisualElement, Color> read = value => value.resolvedStyle.borderTopColor;

            // One full loop's worth of REAL frames. Nothing here ticks anything.
            var lowest = float.MaxValue;
            var highest = float.MinValue;
            var elapsed = 0f;
            while (elapsed < KineticTiming.ShineBreatheLoopSeconds * 1.2f)
            {
                var sampled = read(host);
                var strength = Mathf.Max(sampled.r, sampled.g, sampled.b) /
                    Mathf.Max(accent.r, accent.g, accent.b);
                Assert.That(strength, Is.InRange(0.33f, 1.02f));
                Assert.That(Vector3.Distance(new Vector3(sampled.r, sampled.g, sampled.b),
                    new Vector3(accent.r, accent.g, accent.b) * strength), Is.LessThan(0.025f),
                    "Breathing changes brightness without changing the player accent hue.");
                var luminance = (0.2126f * sampled.r) + (0.7152f * sampled.g) + (0.0722f * sampled.b);
                lowest = Mathf.Min(lowest, luminance);
                highest = Mathf.Max(highest, luminance);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Debug.Log($"[breathfix] sampled luminance over real frames: {lowest:F4} to {highest:F4}, " +
                $"ratio {highest / Mathf.Max(lowest, 0.0001f):F2}x on {Describe(host)}");

            Assert.That(highest - lowest, Is.GreaterThan(0.05f),
                "THE USER'S REPORT, AS AN ASSERTION: an awaited element whose luminance never moves " +
                "across a whole loop of real frames is 'no breathing, just solid colors'. A frozen " +
                "clock, a driver that disabled itself, or a treatment nothing ticks all land here.");
            Assert.That(highest / Mathf.Max(lowest, 0.0001f), Is.GreaterThan(1.8f),
                "and the recorded swing is a kindle, not a shimmer - 8.35z39 measured 2.22x and the " +
                "first version of it measured 3.44%, which read as the static gold it replaced.");
            Assert.That(driver.FailureCount, Is.EqualTo(0),
                "A driver that threw its way to disabled would have frozen the light silently, which " +
                "is one of the two ways the reported symptom can be produced.");
        }

        /// <summary>
        /// All six awaited hero rails breathe within the shared budget. Hand-card gold outlines
        /// were deliberately retired; cards remain available without joining the shine hosts.
        /// Samples real frames rather than ticking the conductor directly.
        /// </summary>
        [UnityTest]
        public IEnumerator OnlyHeroRailsBreatheWhileCommittableCardsStayUnoutlined()
        {
            yield return LoadConfiguredMatchSceneForFirstSeat();
            var root = Root();
            var driver = Object.FindAnyObjectByType<KineticCueDriver>();
            Assert.That(driver, Is.Not.Null,
                "Without the scene's own driver nothing below measures the shipped path.");

            // Settle, so the HUD has composed and every awaited treatment is on screen.
            for (var frame = 0; frame < 8; frame++)
            {
                yield return null;
            }

            var hudRoot = root.Hud.RootElement;

            // The TRUE set the marker class names, uncapped - what the breath SHOULD touch.
            var trueHosts = hudRoot.Query<VisualElement>(className: MatchHudTheme.AwaitedShineClass)
                .ToList();
            var railHosts = trueHosts.Where(host => host.name == "gd-rail-awaited-glow").ToArray();
            var cardHosts = trueHosts.Where(host => host.name == "gd-card-awaited-shine").ToArray();

            Assert.That(railHosts, Has.Length.EqualTo(6),
                "Rulebook p.7's commitment is public and simultaneous: every hero of the official " +
                "six-player match owes it at once at round start, so six rails shine together.");
            Assert.That(cardHosts, Is.Empty, "The breathing gold card outline was explicitly retired.");
            Assert.That(root.Runtime.HandsView.Model.ViewedHand.Cards.Count, Is.GreaterThanOrEqualTo(5),
                "Retiring the outline must not remove the hand.");

            // What the breath's own capped query actually returns - the fidelity number.
            var litHosts = KineticCueUnityView.FindAwaitedShineHosts(hudRoot);
            TestContext.WriteLine(
                $"[allcardsbreath] true awaited hosts={trueHosts.Count} " +
                $"(rails={railHosts.Length}, cards={cardHosts.Length}), " +
                $"capped FindAwaitedShineHosts={litHosts.Count}, " +
                $"MaxBreathingShineElements={KineticCueUnityView.MaxBreathingShineElements}.");

            Assert.That(litHosts.Count, Is.EqualTo(trueHosts.Count),
                "Every awaited element must be within the breath's own shared budget - " +
                $"true={trueHosts.Count} vs capped={litHosts.Count} means at least " +
                $"{trueHosts.Count - litHosts.Count} legally-committable card(s) or rail(s) are " +
                "silently excluded from Light() and never move, which is the user's report as a " +
                "number: 'the rest static'.");

            // Sample every hero rail's own border across consecutive real frames, all six at once,
            // exactly as the sibling case above samples one - nothing here ticks anything.
            var lowest = new float[railHosts.Length];
            var highest = new float[railHosts.Length];
            for (var index = 0; index < railHosts.Length; index++)
            {
                lowest[index] = float.MaxValue;
                highest[index] = float.MinValue;
            }

            var elapsed = 0f;
            while (elapsed < KineticTiming.ShineBreatheLoopSeconds * 1.2f)
            {
                for (var index = 0; index < railHosts.Length; index++)
                {
                    var sampled = railHosts[index].resolvedStyle.borderTopColor;
                    var luminance = (0.2126f * sampled.r) + (0.7152f * sampled.g) + (0.0722f * sampled.b);
                    lowest[index] = Mathf.Min(lowest[index], luminance);
                    highest[index] = Mathf.Max(highest[index], luminance);
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            for (var index = 0; index < railHosts.Length; index++)
            {
                var swing = highest[index] - lowest[index];
                TestContext.WriteLine(
                    $"[railbreath] hero rail {index}: luminance {lowest[index]:F4} to " +
                    $"{highest[index]:F4}, swing {swing:F4}.");
                Assert.That(swing, Is.GreaterThan(0.05f),
                    $"Hero rail {index} of {railHosts.Length} never moved across a whole real-frame " +
                    "loop - exactly 'the rest static' from the LIVE ACCEPTANCE VERDICT, reproduced " +
                    "as a number for this specific rail rather than trusted from a screenshot.");
            }

            Assert.That(driver.FailureCount, Is.EqualTo(0));
        }

        private static string Describe(VisualElement element) =>
            $"{element.GetType().Name}#{element.name}";

        private static IEnumerator LoadConfiguredMatchSceneForFirstSeat()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            var director = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(director, Is.Not.Null);
            while (!director.isDone)
            {
                yield return null;
            }

            yield return null;

            var seatId = Root().Runtime.Host.Seats.Seats[0].Id;
            MatchSceneCompositionRoot.NextComposedSeat = seatId;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            var seated = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(seated, Is.Not.Null);
            while (!seated.isDone)
            {
                yield return null;
            }

            yield return null;
            Assert.That(Root().Runtime.Seat, Is.Not.Null,
                "The scene composed the Director runtime rather than the requested seat.");
            Assert.That(Root().Runtime.Seat.Id, Is.EqualTo(seatId));
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
