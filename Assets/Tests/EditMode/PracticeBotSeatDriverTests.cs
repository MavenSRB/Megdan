using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// MILESTONE 8.35z43. The standing regression <see cref="PracticeBotSeatDriver"/> never
    /// had.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this fixture exists.</b> Milestone 8.35z26 shipped this component opt-in and
    /// compile-probed only, with no case anywhere driving it. Its FIRST EVER launch - by hand, under
    /// Unity, at integration - found an uncaught <c>ArgumentException</c> thrown out of
    /// <c>Update()</c> once per frame: the conductor accepts any non-empty seat id, so a seat this
    /// match does not bind survived composition, logged "seated", and then threw on the first tick
    /// that asked the host for its projection. That was fixed at the compose site and left with no
    /// coverage, so the next equivalent defect would cost another hand launch to find. These are the
    /// three properties that launch established, made durable.</para>
    /// <para><b>How a per-frame component is driven here.</b> <c>Update</c> is now a five-line
    /// adapter that reads what it needs off the scene runtime and calls <c>Tick</c>; everything the
    /// component decides is in <c>Tick</c>, which takes those values. So the whole of the behaviour
    /// runs against a real <c>MatchHost</c> built the way every other offline fixture builds one, and
    /// none of it needs a composed scene. The component itself is still a real
    /// <c>MonoBehaviour</c> on a real <c>GameObject</c>, configured through its own internal seam, on
    /// the pattern <c>PrivateOnlineBackgroundExecutionTests</c> uses.</para>
    /// <para><b>This fixture is Unity-dependent</b> - it constructs a <c>MonoBehaviour</c> and the
    /// component lives in Presentation, which the dotnet harness does not compile - so it is on that
    /// harness's exclude list and runs at the Edit Mode tier.</para>
    /// </remarks>
    public sealed class PracticeBotSeatDriverTests
    {
        /// <summary>
        /// THE DEFECT THE FIRST LAUNCH FOUND. A seat the match does not bind is refused at the
        /// compose site, by name, and the refusal names what IS bound.
        /// </summary>
        [Test]
        public void ASeatTheMatchDoesNotBind_IsRefusedByNameBeforeAnythingIsComposed()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var carrier = new GameObject(nameof(ASeatTheMatchDoesNotBind_IsRefusedByNameBeforeAnythingIsComposed));
            try
            {
                var driver = Configure(carrier, "no-such-seat");
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                    "GUARDS_PRACTICEBOT: FAIL no seat named no-such-seat is bound"));

                driver.Tick(null, host, null, null, false);

                Assert.That(driver.HasSeated, Is.False,
                    "Nothing may be composed for a seat the match does not bind. Before the fix the " +
                    "conductor accepted the id, logged \"seated\", and threw on the next tick.");
                Assert.That(driver.HasStopped, Is.True,
                    "A malformed request stops this driver for good rather than retrying every frame.");
            }
            finally
            {
                Object.DestroyImmediate(carrier);
            }
        }

        /// <summary>
        /// The refusal names the seats that ARE bound, which is what makes a mistyped command line
        /// self-correcting rather than a puzzle.
        /// </summary>
        [Test]
        public void TheUnknownSeatRefusal_ListsTheSeatsThisMatchDoesBind()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var bound = host.Seats.Seats.Select(seat => seat.Id.Value).ToArray();
            var carrier = new GameObject(nameof(TheUnknownSeatRefusal_ListsTheSeatsThisMatchDoesBind));
            try
            {
                var driver = Configure(carrier, "typo-seat");
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                    "this match binds .*" + System.Text.RegularExpressions.Regex.Escape(bound[0])));

                driver.Tick(null, host, null, null, false);

                Assert.That(driver.HasStopped, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(carrier);
            }
        }

        /// <summary>
        /// THE HAPPY PATH: a bound seat seats, and the driver then submits through the host it was
        /// given.
        /// </summary>
        [Test]
        public void ABoundSeat_IsSeatedAndThenPlaysThroughTheHostItWasGiven()
        {
            var initialState = OwnerScopedProjectionTests.CreateOfficialInitialState();
            var host = MatchHost.Compose(initialState);
            var session = new LocalDirectorMatchClient(host);
            // Every seat, which is the shipped shape: the flag takes a comma-separated list and a
            // solo player names every seat but their own. It also keeps this case independent of
            // WHOSE turn the opening position is on, which naming one seat would not be.
            var seats = host.Seats.Seats.Select(seat => seat.Id.Value).ToArray();
            var carrier = new GameObject(nameof(ABoundSeat_IsSeatedAndThenPlaysThroughTheHostItWasGiven));
            try
            {
                var driver = Configure(carrier, string.Join(",", seats));
                var before = session.Projection.Version;

                driver.Tick(null, host, session, initialState, false);

                Assert.That(driver.HasSeated, Is.True, "Bound seats compose a conductor.");
                Assert.That(driver.HasStopped, Is.False);
                Assert.That(session.Projection.Version, Is.GreaterThan(before),
                    "and the first tick that seats also plays, through the host the driver was " +
                    "handed - which is the whole of what this component does.");
            }
            finally
            {
                Object.DestroyImmediate(carrier);
            }
        }

        /// <summary>
        /// A seat-scoped composition is a NETWORK client, and this driver owns nothing there. It
        /// refuses rather than pretending, which is the privacy line the amendment kept out of scope.
        /// </summary>
        [Test]
        public void ASeatScopedComposition_IsRefusedBecauseNothingInItIsHostOwned()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var carrier = new GameObject(nameof(ASeatScopedComposition_IsRefusedBecauseNothingInItIsHostOwned));
            try
            {
                var driver = Configure(carrier, host.Seats.Seats[0].Id.Value);
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                    "practice bots require the local, seat-less composition"));

                driver.Tick(host.Seats.Seats[0], host, null, null, false);

                Assert.That(driver.HasSeated, Is.False);
                Assert.That(driver.HasStopped, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(carrier);
            }
        }

        /// <summary>
        /// THE PER-FRAME PROPERTY, and the one the first launch actually failed: many ticks in a row
        /// throw nothing, on every state this component can be in.
        /// </summary>
        /// <remarks>
        /// The original defect was not that the driver refused - it was that it threw out of
        /// <c>Update</c> once per frame, uncaught, with the honest cause buried under the exception.
        /// So the claim worth pinning is about REPETITION rather than about one call: a component
        /// whose failure mode is per-frame must be asked for many frames. Each of the three
        /// reachable states is driven the same way - refused, seated, and finished - because a stop
        /// that only holds on the first frame is not a stop.
        /// </remarks>
        [Test]
        public void ManyFramesInEveryState_ThrowNothing()
        {
            var initialState = OwnerScopedProjectionTests.CreateOfficialInitialState();
            var carrier = new GameObject(nameof(ManyFramesInEveryState_ThrowNothing));
            try
            {
                // 1. Refused, then asked again and again. The uncaught throw was on the tick AFTER
                //    the one that composed, so a single call could never have found it.
                var refusedHost = MatchHost.Compose(initialState);
                var refused = Configure(carrier, "no-such-seat");
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                    "GUARDS_PRACTICEBOT: FAIL"));
                Assert.That(
                    () =>
                    {
                        for (var frame = 0; frame < 120; frame++)
                        {
                            refused.Tick(null, refusedHost, null, null, false);
                        }
                    },
                    Throws.Nothing,
                    "A refused driver is silent and harmless for as long as the scene runs.");
                Assert.That(refused.HasStopped, Is.True);
                Object.DestroyImmediate(refused);

                // 2. Seated, playing at no pace at all, for many frames of a real match.
                var host = MatchHost.Compose(initialState);
                var session = new LocalDirectorMatchClient(host);
                var seated = Configure(
                    carrier, string.Join(",", host.Seats.Seats.Select(seat => seat.Id.Value)));
                Assert.That(
                    () =>
                    {
                        // Ninety is chosen for cost, not for caution: each seated frame runs a real
                        // tier-2 decision with the shadow probe (about 20 ms), and the defect this
                        // pins showed on the SECOND frame. Repetition is the claim; the count only
                        // has to be far past one.
                        for (var frame = 0; frame < 90; frame++)
                        {
                            seated.Tick(
                                null, host, session, initialState,
                                session.Projection.Outcome.HasEnded);
                        }
                    },
                    Throws.Nothing,
                    "Driving every seat for many frames is the shape a solo practice match has, and " +
                    "it is the shape the first launch threw in.");
                Assert.That(seated.HasSeated, Is.True);

                // 3. And with the match reported over, which is the state a finished practice match
                //    leaves the component in for as long as the player looks at the result.
                Assert.That(
                    () =>
                    {
                        for (var frame = 0; frame < 120; frame++)
                        {
                            seated.Tick(null, host, session, initialState, true);
                        }
                    },
                    Throws.Nothing);
            }
            finally
            {
                Object.DestroyImmediate(carrier);
            }
        }

        /// <summary>
        /// A driver nobody configured does nothing at all, however long the scene runs. This is the
        /// "absent by default" guarantee that keeps every Play Mode test unchanged by this
        /// component's existence.
        /// </summary>
        [Test]
        public void AnUnconfiguredDriver_DoesNothingForeverAndTouchesNoMatch()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var session = new LocalDirectorMatchClient(host);
            var carrier = new GameObject(nameof(AnUnconfiguredDriver_DoesNothingForeverAndTouchesNoMatch));
            try
            {
                var driver = carrier.AddComponent<PracticeBotSeatDriver>();
                var before = session.Projection.Version;

                for (var frame = 0; frame < 120; frame++)
                {
                    driver.Tick(null, host, session, null, false);
                }

                Assert.That(driver.HasSeated, Is.False);
                Assert.That(session.Projection.Version, Is.EqualTo(before),
                    "A build launched normally attaches no options, so the Director's own " +
                    "composition must behave exactly as it did before this component existed.");
            }
            finally
            {
                Object.DestroyImmediate(carrier);
            }
        }

        /// <summary>
        /// Attaches the component and configures it for the named seats, through the same options
        /// parser the command line goes through - so a test can never ask for a shape the command
        /// line cannot produce.
        /// </summary>
        private static PracticeBotSeatDriver Configure(GameObject carrier, string seats)
        {
            Assert.That(
                PracticeBotSeatDriverOptions.TryParse(
                    new[]
                    {
                        PracticeBotSeatDriverOptions.RoleFlag,
                        PracticeBotSeatDriverOptions.SeatsFlag, seats,
                        // No pause between moves: a fixture measures what the component decides, not
                        // the pace a person watches it at.
                        PracticeBotSeatDriverOptions.ThinkSecondsFlag, "0"
                    },
                    out var options,
                    out var error),
                Is.True,
                error);
            Assert.That(options, Is.Not.Null, error);

            var driver = carrier.AddComponent<PracticeBotSeatDriver>();
            driver.Configure(null, options);
            return driver;
        }
    }
}
