using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z11 (the TWENTIETH AMENDMENT's principle 3: ONE THING AT A TIME).
    ///
    /// <para>These are the cases about PACING, and they are deliberately separate from
    /// <c>KineticCueTests</c> (what a row means) and <c>KineticPlaybackTests</c> (what a curve does).
    /// The conductor is the piece that decides a player watches six reveals as six moments rather than
    /// as one flicker, and every rule that could get that wrong - the order, the beat, what happens to
    /// a viewer who fell behind, and what an undo does to a queue - is asserted here.</para>
    /// </summary>
    [TestFixture]
    public sealed class KineticConductorTests
    {
        private static readonly HeroId Hero = new HeroId("hero-a");
        private static readonly HeroId Other = new HeroId("hero-b");

        [Test]
        public void QueuedTravelKeepsOnlyItsCurrentSubjectAtTheRouteOrigin_AndCancelReleasesIt()
        {
            var conductor = new KineticConductor();
            var subject = KineticSubject.ForHero(Hero);
            var from = new HexCoordinate(0, 0);
            var to = new HexCoordinate(1, 0);
            conductor.Enqueue(new[] { new KineticScene(1, KineticSceneKind.Board,
                new[] { new KineticCue(1, KineticCueKind.Slide, subject, route: new[] { from, to }) }) });
            Assert.That(conductor.WaitingTravelOriginFor(subject, to), Is.EqualTo(from));
            Assert.That(conductor.WaitingTravelOriginFor(subject, new HexCoordinate(2, 0)), Is.Null,
                "A stale route must not displace a piece that has since moved elsewhere.");
            Assert.That(conductor.WaitingTravelOriginFor(KineticSubject.ForHero(Other), to), Is.Null);
            conductor.CancelAll();
            Assert.That(conductor.WaitingTravelOriginFor(subject, to), Is.Null);
        }

        private static KineticScene Scene(
            int order,
            KineticSceneKind kind = KineticSceneKind.Board,
            bool holds = false,
            string label = null) =>
            new KineticScene(
                order,
                kind,
                new[] { new KineticCue(order, KineticCueKind.Respawn, KineticSubject.ForHero(Hero)) },
                holdsForAcknowledgement: holds,
                label: label ?? order.ToString());

        /// <summary>Runs the conductor in frame-sized steps and records the order scenes started in.</summary>
        private static List<string> PlayOut(KineticConductor conductor, int frames = 4000)
        {
            var started = new List<string>();
            for (var frame = 0; frame < frames && !conductor.IsIdle; frame++)
            {
                var scene = conductor.Advance(1f / 60f);
                if (scene != null)
                {
                    started.Add(scene.Label);
                }
            }

            return started;
        }

        // ---- The principle itself.

        [Test]
        public void TheSecondSceneWaitsForTheFirstToFinish_AndForItsBeat()
        {
            var conductor = new KineticConductor();
            conductor.Enqueue(new[] { Scene(1, label: "first"), Scene(2, label: "second") });

            var first = conductor.Advance(1f / 60f);
            Assert.That(first?.Label, Is.EqualTo("first"), "The head of the queue starts at once.");

            var duration = first.DurationSeconds;

            // Just short of the first scene's own length: nothing else may have begun.
            var elapsed = 1f / 60f;
            while (elapsed < duration - 0.02f)
            {
                Assert.That(conductor.Advance(1f / 60f), Is.Null,
                    "A second moment must not begin while the first is still playing.");
                elapsed += 1f / 60f;
            }

            // Through the scene's end but inside its trailing beat: still nothing.
            var startedDuringBeat = false;
            var beatEnd = duration + KineticStaging.SceneStaggerSeconds - 0.02f;
            while (elapsed < beatEnd)
            {
                startedDuringBeat |= conductor.Advance(1f / 60f) != null;
                elapsed += 1f / 60f;
            }

            Assert.That(startedDuringBeat, Is.False,
                "The deliberate stagger is the silence that makes two motions read as two events.");

            var rest = PlayOut(conductor);
            Assert.That(rest, Is.EqualTo(new[] { "second" }));
        }

        [Test]
        public void EveryQueuedScene_EventuallyPlays_InOrder()
        {
            var conductor = new KineticConductor();
            conductor.Enqueue(Enumerable.Range(1, 6).Select(i => Scene(i, label: "s" + i)).ToArray());

            var played = PlayOut(conductor);

            Assert.That(played, Is.EqualTo(new[] { "s1", "s2", "s3", "s4", "s5", "s6" }));
            Assert.That(conductor.IsIdle, Is.True);
            Assert.That(conductor.DroppedSceneCount, Is.EqualTo(0),
                "Six moments is an ordinary end-of-round, not a queue to start dropping from.");
        }

        // ---- Ordering: principle 4's staging.

        [Test]
        public void ABatchIsStagedByItsOrder_NotByTheOrderItArrivedIn()
        {
            var conductor = new KineticConductor();

            // The engine emits reveals in ordinal hero-id order; the ranks say what a player should
            // watch. Arrival order here is deliberately the reverse of the ranks.
            conductor.Enqueue(new[]
            {
                Scene(30, KineticSceneKind.Reveal, label: "third"),
                Scene(10, KineticSceneKind.Reveal, label: "first"),
                Scene(20, KineticSceneKind.Reveal, label: "second")
            });

            Assert.That(PlayOut(conductor), Is.EqualTo(new[] { "first", "second", "third" }));
        }

        /// <summary>
        /// Two heroes whose cards print the same Initiative are ranked equal, and the tie has to break
        /// the same way on every client or two players watch a different story. An unstable sort would
        /// break it differently per run.
        /// </summary>
        [Test]
        public void ScenesThatRankEqual_KeepTheirArrivalOrder()
        {
            var conductor = new KineticConductor();
            conductor.Enqueue(new[]
            {
                Scene(7, KineticSceneKind.Reveal, label: "a"),
                Scene(7, KineticSceneKind.Reveal, label: "b"),
                Scene(7, KineticSceneKind.Reveal, label: "c"),
                Scene(7, KineticSceneKind.Reveal, label: "d"),
                Scene(7, KineticSceneKind.Reveal, label: "e")
            });

            Assert.That(PlayOut(conductor), Is.EqualTo(new[] { "a", "b", "c", "d", "e" }));
        }

        /// <summary>
        /// The sort is WITHIN a batch, never across the queue: a projection that arrives later happened
        /// later, whatever numbers its scenes carry. Re-sorting globally would let round four's first
        /// reveal jump ahead of round three's last attack.
        /// </summary>
        [Test]
        public void ALaterBatch_NeverJumpsAheadOfAnEarlierOne()
        {
            var conductor = new KineticConductor();
            conductor.Enqueue(new[] { Scene(90, label: "early-batch-high-order") });
            conductor.Enqueue(new[] { Scene(1, label: "later-batch-low-order") });

            Assert.That(
                PlayOut(conductor),
                Is.EqualTo(new[] { "early-batch-high-order", "later-batch-low-order" }));
        }

        // ---- Falling behind: the board never waits.

        [Test]
        public void AViewerFarBehind_PlaysFasterAndDropsTheBeats()
        {
            var paced = new KineticConductor();
            paced.Enqueue(Enumerable.Range(1, 3).Select(i => Scene(i)).ToArray());
            var pacedFrames = FramesToDrain(paced);

            var behind = new KineticConductor();
            behind.Enqueue(
                Enumerable.Range(1, KineticStaging.CatchUpQueueBudget + 6)
                    .Select(i => Scene(i))
                    .ToArray());
            Assert.That(behind.IsCatchingUp, Is.True,
                "A queue past the catch-up budget is a viewer who fell behind their own board.");

            var perScenePaced = pacedFrames / 3f;
            var perSceneBehind =
                FramesToDrain(behind) / (float)(KineticStaging.CatchUpQueueBudget + 6);

            Assert.That(perSceneBehind, Is.LessThan(perScenePaced),
                "The queue compresses under load; the alternative is a story minutes behind the match.");
        }

        [Test]
        public void AQueueBeyondItsBudget_DropsTheOldest_AndCountsWhatItDropped()
        {
            var conductor = new KineticConductor();
            var overflow = KineticStaging.MaxQueuedScenes + 5;
            conductor.Enqueue(Enumerable.Range(1, overflow).Select(i => Scene(i, label: "s" + i)).ToArray());

            Assert.That(conductor.PendingSceneCount, Is.EqualTo(KineticStaging.MaxQueuedScenes));
            Assert.That(conductor.DroppedSceneCount, Is.EqualTo(5));

            var played = PlayOut(conductor);
            Assert.That(played.Contains("s" + overflow), Is.True,
                "The newest moments are the ones a player is still trying to understand.");
            Assert.That(played.Contains("s1"), Is.False,
                "The oldest are what a far-behind viewer can afford to lose; the board is true either way.");
        }

        // ---- Interruption.

        [Test]
        public void ASnap_DropsEverythingQueuedAndPlaying()
        {
            var conductor = new KineticConductor();
            conductor.Enqueue(Enumerable.Range(1, 10).Select(i => Scene(i)).ToArray());
            conductor.Advance(1f / 60f);

            Assert.That(conductor.IsIdle, Is.False);

            conductor.CancelAll();

            Assert.That(conductor.IsIdle, Is.True,
                "An undo must never leave a queue describing events that no longer happened.");
            Assert.That(conductor.ActiveScene, Is.Null);
            Assert.That(conductor.PendingSceneCount, Is.EqualTo(0));
            Assert.That(conductor.Advance(1f / 60f), Is.Null);
        }

        // ---- The spotlight's seam.

        [Test]
        public void AHoldingScene_WaitsForTheViewer_AndReleasesWhenAcknowledged()
        {
            var conductor = new KineticConductor();
            conductor.Enqueue(new[]
            {
                Scene(1, KineticSceneKind.Spotlight, holds: true, label: "spotlight"),
                Scene(2, label: "after")
            });

            var started = conductor.Advance(1f / 60f);
            Assert.That(started?.Label, Is.EqualTo("spotlight"));

            for (var frame = 0; frame < 240; frame++)
            {
                Assert.That(conductor.Advance(1f / 60f), Is.Null,
                    "A spotlight holds until the viewer continues, however long that takes.");
            }

            Assert.That(conductor.IsHoldingForAcknowledgement, Is.True);
            Assert.That(conductor.Acknowledge(), Is.True);
            Assert.That(conductor.ActiveScene?.Label, Is.EqualTo("after"),
                "Continuing releases the hold and starts the next moment immediately.");
        }

        [Test]
        public void AcknowledgeDoesNothing_ToASceneStillPlaying()
        {
            var conductor = new KineticConductor();
            conductor.Enqueue(new[] { Scene(1, KineticSceneKind.Spotlight, holds: true, label: "one") });
            conductor.Advance(1f / 240f);

            Assert.That(conductor.IsHoldingForAcknowledgement, Is.False);
            Assert.That(conductor.Acknowledge(), Is.False,
                "A stray click must not skip a beat the player has not seen yet.");
            Assert.That(conductor.ActiveScene?.Label, Is.EqualTo("one"));
        }

        [Test]
        public void AHoldNobodyAnswers_ReleasesItself()
        {
            var conductor = new KineticConductor();
            conductor.Enqueue(new[]
            {
                Scene(1, KineticSceneKind.Spotlight, holds: true, label: "spotlight"),
                Scene(2, label: "after")
            });
            conductor.Advance(1f / 60f);

            // A player who walked away. Frame-sized steps, because Advance clamps a single large delta.
            var frames = 0;
            while (conductor.ActiveScene?.Label == "spotlight" && frames < 20000)
            {
                conductor.Advance(1f / 60f);
                frames++;
            }

            Assert.That(conductor.ActiveScene?.Label, Is.EqualTo("after"),
                "A viewer who never comes back must come back to a caught-up board, not a frozen one.");
        }

        // ---- The architectural claim, asserted rather than reviewed.

        /// <summary>
        /// The conductor's whole safety argument is that it is downstream of the projection and
        /// upstream of nothing: it cannot delay a command, a prompt or a frame of the board, which is
        /// what lets six clients watch one match at six different points in the same story. That claim
        /// is only worth anything if it stays true, so it is checked structurally.
        /// </summary>
        [Test]
        public void TheConductor_HoldsNothingAuthoritative()
        {
            var forbidden = new[]
            {
                typeof(IMatchReadModel), typeof(IMatchClient)
            };

            var fields = typeof(KineticConductor)
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var field in fields)
            {
                foreach (var type in forbidden)
                {
                    Assert.That(
                        type.IsAssignableFrom(field.FieldType), Is.False,
                        "The conductor paces a viewer's presentation and must never be able to reach " +
                        "the match: " + field.Name + " is a " + field.FieldType.Name + ".");
                }
            }

            foreach (var constructor in typeof(KineticConductor).GetConstructors())
            {
                Assert.That(constructor.GetParameters().Length, Is.EqualTo(0),
                    "Nothing may be handed to the conductor at all.");
            }
        }

        // ---- The guard that makes a queued slide safe.

        [Test]
        public void ASlideWhosePieceMovedOnAgain_TravelsNothing()
        {
            var route = new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) };

            Assert.That(
                KineticRouteFreshness.IsRouteStillCurrent(route, new HexCoordinate(1, 0)), Is.True,
                "A slide whose route still ends where the piece stands is the ordinary case.");

            Assert.That(
                KineticRouteFreshness.IsRouteStillCurrent(route, new HexCoordinate(2, 0)), Is.False,
                "A queued slide can reach the screen after the piece moved again. Drawing it as an " +
                "offset from the piece's NEW position would put it on a path it never took.");

            Assert.That(KineticRouteFreshness.IsRouteStillCurrent(null, new HexCoordinate(0, 0)), Is.False);
            Assert.That(
                KineticRouteFreshness.IsRouteStillCurrent(
                    new[] { new HexCoordinate(0, 0) }, new HexCoordinate(0, 0)),
                Is.False,
                "One waypoint is not a journey.");
        }

        // ---- Scene shape.

        [Test]
        public void ASceneRefusesAShapeThatIsNotAMoment()
        {
            Assert.Throws<ArgumentException>(
                () => new KineticScene(1, KineticSceneKind.None, new[] { AnyCue() }),
                "A scene has to say what it is about.");

            Assert.Throws<ArgumentException>(
                () => new KineticScene(1, KineticSceneKind.Board, Array.Empty<KineticCue>()),
                "A scene is something the player sees.");

            Assert.Throws<ArgumentException>(
                () => new KineticScene(1, KineticSceneKind.Board, new KineticCue[] { null }),
                "A scene of nothing but nulls is a scene of nothing.");
        }

        [Test]
        public void ASceneLastsAsLongAsItsLongestCue()
        {
            var scene = new KineticScene(
                1,
                KineticSceneKind.Board,
                new[]
                {
                    new KineticCue(1, KineticCueKind.Flash, KineticSubject.ForHero(Hero)),
                    new KineticCue(1, KineticCueKind.Defeat, KineticSubject.ForHero(Other))
                });

            Assert.That(scene.DurationSeconds, Is.EqualTo(KineticTiming.DefeatSeconds).Within(0.0001f),
                "Two cues in one moment end together, so the moment is as long as the slower of them.");
        }

        [Test]
        public void FlatteningScenes_GivesEveryCueInPlayOrder()
        {
            var scenes = new[]
            {
                new KineticScene(
                    1, KineticSceneKind.Board,
                    new[]
                    {
                        new KineticCue(
                            1, KineticCueKind.Lunge, KineticSubject.ForHero(Hero),
                            KineticSubject.ForHero(Other)),
                        new KineticCue(1, KineticCueKind.Flash, KineticSubject.ForHero(Other))
                    }),
                new KineticScene(
                    2, KineticSceneKind.Board,
                    new[] { new KineticCue(2, KineticCueKind.Respawn, KineticSubject.ForHero(Other)) })
            };

            var flat = KineticScene.Flatten(scenes);

            Assert.That(flat.Count, Is.EqualTo(3));
            Assert.That(flat[0].Kind, Is.EqualTo(KineticCueKind.Lunge));
            Assert.That(flat[1].Kind, Is.EqualTo(KineticCueKind.Flash));
            Assert.That(flat[2].Kind, Is.EqualTo(KineticCueKind.Respawn));
            Assert.That(KineticScene.Flatten(null).Count, Is.EqualTo(0));
        }

        // ---- Milestone 8.35z12: the reconciliation of the two conductors that were designed in
        // ---- parallel. These four cases are the whole of what integration had to prove.

        /// <summary>
        /// THE ONE FLAGGED CONFLICT BETWEEN THE TWO DESIGNS, PINNED SO IT CANNOT COME BACK.
        ///
        /// <para>8.35z11's conductor self-releases a held scene after 20 seconds; 8.35z12's spotlight
        /// self-releases after 6. Neither number was wrong - they answer different questions, and the
        /// rule that reconciles them is <b>the valve is short when the chrome is hidden</b>. A held
        /// scene that leaves the HUD up is merely slow; one that has hidden the HUD leaves the viewer
        /// holding a client they cannot play. The spotlight is the first scene of the second kind, so
        /// its own constant and the conductor's chrome-hidden constant must be the same number, and
        /// this asserts they are rather than trusting two files to stay in step.</para>
        /// </summary>
        [Test]
        public void TheChromeHiddenHoldValve_IsTheSameNumberOnBothSidesOfTheReconciliation()
        {
            Assert.That(
                KineticStaging.ChromeHiddenHoldSeconds,
                Is.EqualTo(AttackSpotlightTiming.MaximumHeldSeconds).Within(0.0001f),
                "The conductor's chrome-hidden valve and the spotlight's own hold must be one number.");

            Assert.That(
                KineticStaging.ChromeHiddenHoldSeconds,
                Is.LessThan(KineticStaging.MaxHoldSeconds),
                "Hiding the chrome must SHORTEN the valve, never lengthen it.");
        }

        /// <summary>
        /// The valve is chosen by the scene, not by a single global constant - and the two cases are
        /// asserted against each other at the same instant so the difference cannot be a coincidence.
        /// </summary>
        [Test]
        public void AHeldSceneThatHidTheChrome_ReleasesOnTheShortValve_WhileOneThatDidNot_IsStillWaiting()
        {
            var hiding = new KineticConductor();
            var showing = new KineticConductor();
            hiding.Enqueue(new[] { ChromeHidingHeldScene() });
            showing.Enqueue(new[] { Scene(1, holds: true) });

            hiding.Advance(0f);
            showing.Advance(0f);

            // Past the short valve, comfortably short of the long one.
            var elapsed = 0f;
            var target = KineticStaging.ChromeHiddenHoldSeconds + 1f;
            while (elapsed < target)
            {
                hiding.Advance(1f / 60f);
                showing.Advance(1f / 60f);
                elapsed += 1f / 60f;
            }

            Assert.That(target, Is.LessThan(KineticStaging.MaxHoldSeconds),
                "The window this case measures in must sit between the two valves to mean anything.");
            Assert.That(hiding.IsIdle, Is.True,
                "A held scene that hid the HUD must let go on the short valve.");
            Assert.That(showing.IsHoldingForAcknowledgement, Is.True,
                "A held scene that left the HUD up is merely slow and keeps its long valve.");
        }

        /// <summary>
        /// ONE CONDUCTOR. The spotlight's stage is not made of cues, so it is registered as the
        /// conductor's <c>ConductedScene</c> rather than queued - and principle 3 then covers it too:
        /// a scene that is not made of cues still gets the stage to itself.
        /// </summary>
        [Test]
        public void AConductedSceneIsGrantedTheStage_AndNoCueSceneStartsWhileItHoldsIt()
        {
            var conductor = new KineticConductor();
            var conducted = new FakeConductedScene();
            conductor.ConductedScene = conducted;
            conductor.Enqueue(new[] { Scene(1, label: "waiting") });

            conducted.Requesting = true;
            var started = conductor.Advance(1f / 60f);

            Assert.That(conducted.Grants, Is.EqualTo(1), "A requesting scene is granted the stage.");
            Assert.That(started, Is.Null, "Granting the stage starts no cue scene on the same frame.");

            conducted.Requesting = false;
            conducted.Holding = true;
            conducted.Deferring = true;
            for (var frame = 0; frame < 120; frame++)
            {
                Assert.That(conductor.Advance(1f / 60f), Is.Null,
                    "Nothing may start while the scene is travelling onto or off the stage.");
            }

            Assert.That(conductor.PendingSceneCount, Is.EqualTo(1),
                "The waiting scene is HELD, not dropped - it plays when the stage is free.");

            conducted.Deferring = false;
            Assert.That(conductor.Advance(1f / 60f), Is.Not.Null,
                "The moment the scene stops travelling the queue resumes.");
        }

        /// <summary>
        /// THE DEFECT THE FIRST COMPLETE PLAY MODE RUN OF THE WAVE FOUND, pinned so it cannot come
        /// back. The conductor's first gate asked <c>HoldsStage</c>, which is true through the
        /// spotlight's HELD phase - and that is precisely the phase the attack's own released cues
        /// have to play in, framed, with both faces up. Gating on it kept the smack queued until the
        /// spotlight had CLOSED, moving it out of the shot it exists for.
        ///
        /// <para>The gate is <c>DefersOtherScenes</c> now, which is the same (phase, elapsed) answer
        /// the cue layer's own deferral uses - so the conductor and the deferral answer ONE question
        /// rather than two that happen to agree. This asserts they are still the same predicate,
        /// including milestone 8.35z56's addition: HELD defers for its own opening
        /// <see cref="AttackSpotlightTiming.PostZoomBeatSeconds"/> and stops deferring after.</para>
        /// </summary>
        [Test]
        public void TheConductorsGateAndTheCueDeferral_AreTheSamePhaseAndElapsedAnswer()
        {
            var stage = new AttackSpotlightStage { AutoGrantsStage = false };
            var phases = (AttackSpotlightPhase[])Enum.GetValues(typeof(AttackSpotlightPhase));
            var beat = AttackSpotlightTiming.PostZoomBeatSeconds;
            var samples = new[] { 0f, beat * 0.5f, beat - 0.001f, beat, beat + 0.001f, beat * 4f };

            Assert.That(stage.DefersOtherScenes, Is.False,
                "A stage doing nothing defers nothing.");

            foreach (var phase in phases)
            {
                foreach (var elapsed in samples)
                {
                    var expected =
                        phase == AttackSpotlightPhase.Requested ||
                        phase == AttackSpotlightPhase.Entering ||
                        phase == AttackSpotlightPhase.Exiting ||
                        (phase == AttackSpotlightPhase.Held && elapsed < beat);
                    Assert.That(
                        AttackSpotlightPresenter.DefersCuesInPhase(phase, elapsed), Is.EqualTo(expected),
                        "phase=" + phase + " elapsed=" + elapsed);
                }
            }

            Assert.That(
                AttackSpotlightPresenter.DefersCuesInPhase(AttackSpotlightPhase.Held, 0f), Is.True,
                "The instant HELD begins, the beat has not passed yet, so it still defers.");
            Assert.That(
                AttackSpotlightPresenter.DefersCuesInPhase(AttackSpotlightPhase.Held, beat), Is.False,
                "Once the beat has fully elapsed HELD stops deferring - that is where the framed " +
                "smack plays, and the whole reason this pair of predicates is not HoldsStage.");
        }

        /// <summary>
        /// THE 2026-09-08 CAMERA-TIMING DIRECTIVE, reproduced as numbers before being fixed: "wait
        /// for the camera to zoom in on the attack, before doing the animation", refined the same day
        /// to "Zoom in, a small pause, then smack."
        ///
        /// <para>This drives the REAL <see cref="AttackSpotlightStage"/> (not a fake) through its own
        /// <see cref="AttackSpotlightStage.Advance"/> clock beside the conductor's, in PRODUCTION
        /// ORDER: <c>MatchSceneCompositionRoot</c> attaches <c>KineticCueDriver</c> (which ticks the
        /// conductor) to the composition root's GameObject BEFORE <c>AttackSpotlightDriver</c> (which
        /// ticks the stage), so on any one frame the conductor's own <see cref="KineticConductor.Advance"/>
        /// always runs first - reproduced here rather than assumed, because the order is exactly what
        /// makes the gate's staleness benign (see the method's own remarks).</para>
        ///
        /// <para>Before milestone 8.35z56 this measured a near-zero gap - the attack cue
        /// started on the very frame the zoom finished, which read as the animation stepping on the
        /// camera's own landing rather than waiting for it. The fix inserts
        /// <see cref="AttackSpotlightTiming.PostZoomBeatSeconds"/> of held-but-not-yet-releasing time
        /// between the two, asserted here as the ordinal the user asked for: attack start
        /// &gt;= zoom-complete + beat.</para>
        /// </summary>
        [Test]
        public void TheAttackCueWaitsForTheZoomToCompleteAndForThePostZoomBeat()
        {
            var conductor = new KineticConductor();
            var stage = new AttackSpotlightStage { AutoGrantsStage = false };
            conductor.ConductedScene = stage;

            stage.Offer(new AttackSpotlightScene(
                1,
                KineticSubject.ForHero(Hero),
                KineticSubject.ForHero(Other),
                GuardsDigital.Core.Diagnostics.MatchLogOutcome.AttackSuccessful));

            // The attack's own Lunge/Flash cue. Enqueued up front rather than the instant the beat
            // lifts, deliberately: AConductedSceneIsGrantedTheStage_AndNoCueSceneStartsWhileItHoldsIt
            // already established that the conductor's own DefersOtherScenes gate controls START
            // regardless of when a scene entered the queue, so enqueuing early asks the harder
            // question and the production KineticCuePresenter timing cannot make this test pass by
            // accident.
            conductor.Enqueue(new[] { Scene(1, label: "attack-cue") });

            const float frame = 1f / 120f;
            var clock = 0f;
            var zoomCompleteAt = -1f;
            var attackStartAt = -1f;

            while (attackStartAt < 0f && clock < 10f)
            {
                // Production order: the conductor's driver ticks before the spotlight's own.
                var result = conductor.Advance(frame);
                var wasHeld = stage.Phase == AttackSpotlightPhase.Held;
                stage.Advance(frame);
                clock += frame;

                if (zoomCompleteAt < 0f && !wasHeld && stage.Phase == AttackSpotlightPhase.Held)
                {
                    zoomCompleteAt = clock;
                }

                if (attackStartAt < 0f && result != null)
                {
                    attackStartAt = clock;
                }
            }

            Assert.That(zoomCompleteAt, Is.GreaterThan(0f).And.LessThan(10f),
                "The zoom must actually complete during this run for the measurement to mean anything.");
            Assert.That(attackStartAt, Is.GreaterThan(0f).And.LessThan(10f),
                "The attack cue must eventually start.");

            var gap = attackStartAt - zoomCompleteAt;
            TestContext.WriteLine(string.Format(
                "zoom-complete={0:0.0000}s attack-start={1:0.0000}s measured-gap={2:0.0000}s " +
                "required-beat={3:0.0000}s",
                zoomCompleteAt, attackStartAt, gap, AttackSpotlightTiming.PostZoomBeatSeconds));
            Assert.That(gap, Is.GreaterThanOrEqualTo(AttackSpotlightTiming.PostZoomBeatSeconds - frame),
                string.Format(
                    "The attack animation must not start until the post-zoom beat has passed. " +
                    "zoom-complete={0:0.0000}s attack-start={1:0.0000}s measured-gap={2:0.0000}s " +
                    "required-beat={3:0.0000}s",
                    zoomCompleteAt, attackStartAt, gap, AttackSpotlightTiming.PostZoomBeatSeconds));
        }

        /// <summary>
        /// The null registration is a supported composition, and it is the one every existing case in
        /// this fixture runs under - stated explicitly so the reconciliation cannot have changed the
        /// behaviour of a composition that has no spotlight.
        /// </summary>
        [Test]
        public void WithNoConductedSceneRegistered_TheConductorPacesExactlyAsItAlwaysDid()
        {
            var conductor = new KineticConductor();
            Assert.That(conductor.ConductedScene, Is.Null, "Nothing is registered by default.");

            conductor.Enqueue(new[] { Scene(1, label: "a"), Scene(2, label: "b") });
            var order = PlayOut(conductor);

            Assert.That(order, Is.EqualTo(new[] { "a", "b" }));
        }

        private static KineticScene ChromeHidingHeldScene() =>
            new KineticScene(
                1,
                KineticSceneKind.Spotlight,
                new[] { new KineticCue(1, KineticCueKind.Respawn, KineticSubject.ForHero(Hero)) },
                holdsForAcknowledgement: true,
                label: "chrome-hidden",
                hidesChrome: true);

        /// <summary>A conducted scene with no Unity, no camera and no cues - just the six members.</summary>
        private sealed class FakeConductedScene : IConductedScene
        {
            public bool Requesting;
            public bool Holding;
            public bool Deferring;
            public int Grants;
            public ConductedSceneInterruption LastInterruption = ConductedSceneInterruption.None;

            public string Name => "fake";

            public bool IsRequestingStage => Requesting;

            public bool HoldsStage => Holding;

            public bool DefersOtherScenes => Deferring;

            public void GrantStage() => Grants++;

            public void Advance(float deltaSeconds)
            {
            }

            public void Interrupt(ConductedSceneInterruption reason) => LastInterruption = reason;
        }

        private static KineticCue AnyCue() =>
            new KineticCue(1, KineticCueKind.Respawn, KineticSubject.ForHero(Hero));

        private static int FramesToDrain(KineticConductor conductor)
        {
            var frames = 0;
            while (!conductor.IsIdle && frames < 100000)
            {
                conductor.Advance(1f / 60f);
                frames++;
            }

            return frames;
        }
    }
}
