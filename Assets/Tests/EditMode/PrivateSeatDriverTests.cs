using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Network;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// THIRTEENTH amendment. <see cref="PrivateSeatDriverOptions"/>'s command-line/environment
    /// parsing, and the lobby presenter/model surface the spawned driver and the host's own FILL
    /// WITH TEST DRIVERS control share.
    /// </summary>
    /// <remarks>
    /// The endpoint and key ride only the process environment block (the same privacy rule
    /// <c>PrivateOnlineRehearsalOptions</c> already follows), so every case here restores whatever
    /// the three variables held before it ran - a process-global side effect a parallel or later
    /// test must never inherit.
    /// </remarks>
    public sealed class PrivateSeatDriverTests
    {
        private string savedAddress;
        private string savedPort;
        private string savedKey;
        private string savedParentProcessId;

        [SetUp]
        public void SaveEnvironment()
        {
            savedAddress = Environment.GetEnvironmentVariable(
                PrivateSeatDriverOptions.AddressEnvironmentVariable);
            savedPort = Environment.GetEnvironmentVariable(
                PrivateSeatDriverOptions.PortEnvironmentVariable);
            savedKey = Environment.GetEnvironmentVariable(
                PrivateSeatDriverOptions.KeyEnvironmentVariable);
            savedParentProcessId = Environment.GetEnvironmentVariable(
                PrivateSeatDriverOptions.ParentProcessIdEnvironmentVariable);
        }

        [TearDown]
        public void RestoreEnvironment()
        {
            Environment.SetEnvironmentVariable(
                PrivateSeatDriverOptions.AddressEnvironmentVariable, savedAddress);
            Environment.SetEnvironmentVariable(
                PrivateSeatDriverOptions.PortEnvironmentVariable, savedPort);
            Environment.SetEnvironmentVariable(
                PrivateSeatDriverOptions.KeyEnvironmentVariable, savedKey);
            Environment.SetEnvironmentVariable(
                PrivateSeatDriverOptions.ParentProcessIdEnvironmentVariable, savedParentProcessId);
        }

        private static void SetEnvironment(string address, string port, string key)
        {
            Environment.SetEnvironmentVariable(PrivateSeatDriverOptions.AddressEnvironmentVariable, address);
            Environment.SetEnvironmentVariable(PrivateSeatDriverOptions.PortEnvironmentVariable, port);
            Environment.SetEnvironmentVariable(PrivateSeatDriverOptions.KeyEnvironmentVariable, key);
            Environment.SetEnvironmentVariable(PrivateSeatDriverOptions.ParentProcessIdEnvironmentVariable, null);
        }

        private static string[] Args(params string[] extra) =>
            new[] { PrivateSeatDriverOptions.RoleFlag, PrivateSessionLaunchOptions.LobbyFlag }
                .Concat(extra).ToArray();

        [Test]
        public void ALine_WithNoRoleFlag_IsNotRequested()
        {
            // Unlike PrivateOnlineRehearsalOptions.TryParse (which always "succeeds" and reports
            // presence separately through its own TryParseRequested wrapper), this TryParse
            // answers the presence question itself: false, with nothing parsed and nothing to
            // say, is a normal launch's own command line - the common case, not a refusal.
            var parsed = PrivateSeatDriverOptions.TryParse(
                new[] { PrivateSessionLaunchOptions.LobbyFlag }, out var result, out var error);

            Assert.That(parsed, Is.False);
            Assert.That(result, Is.Null);
            Assert.That(error, Is.Empty);
        }

        [Test]
        public void AWellFormedLine_ParsesTheSeatAndTheEnvironmentCarriedEndpoint()
        {
            SetEnvironment("127.0.0.1", "45677", "the-session-key");

            var parsed = PrivateSeatDriverOptions.TryParse(
                Args(PrivateSeatDriverOptions.SeatFlag, "red-player-02"), out var result, out var error);

            Assert.That(parsed, Is.True);
            Assert.That(error, Is.Empty);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Seat, Is.EqualTo(new SeatId("red-player-02")));
            Assert.That(result.Address, Is.EqualTo("127.0.0.1"));
            Assert.That(result.Port, Is.EqualTo(45677));
            Assert.That(result.SessionKey, Is.EqualTo("the-session-key"));
            Assert.That(result.ThinkSeconds, Is.EqualTo(PrivateSeatDriverOptions.DefaultThinkSeconds),
                "A human-followable default, unasked - the amendment's own rule that full rehearsal " +
                "speed is unwatchable.");
        }

        [Test]
        public void MissingTheSeatFlag_IsRefused()
        {
            SetEnvironment("127.0.0.1", "45677", "key");

            PrivateSeatDriverOptions.TryParse(Args(), out var result, out var error);

            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain(PrivateSeatDriverOptions.SeatFlag));
        }

        [Test]
        public void MissingTheLobbyFlag_IsRefused()
        {
            SetEnvironment("127.0.0.1", "45677", "key");

            PrivateSeatDriverOptions.TryParse(
                new[] { PrivateSeatDriverOptions.RoleFlag, PrivateSeatDriverOptions.SeatFlag, "red-player-01" },
                out var result, out var error);

            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain("lobby"));
        }

        /// <summary>
        /// 8.35t corrective, reversing 8.35q's own original rule. The live defect: a host bound to
        /// this machine's own real network address (typed for a remote friend's port-forward, or to
        /// rehearse that exact setup even solo, per the tester instructions' own paragraph) hands
        /// that same address to a spawned test driver, which this parser used to refuse outright -
        /// confining every driver to a loopback listener nothing was bound to. Dropping the
        /// restriction grants no new capability: <c>-guardsPrivateClient</c> has always been free to
        /// dial any address with any key, and the session key is what actually gates entry here too.
        /// </summary>
        [Test]
        public void ANonLoopbackAddress_IsNowAccepted()
        {
            SetEnvironment("198.51.100.7", "45677", "key");

            var parsed = PrivateSeatDriverOptions.TryParse(
                Args(PrivateSeatDriverOptions.SeatFlag, "red-player-01"), out var result, out var error);

            Assert.That(parsed, Is.True);
            Assert.That(error, Is.Empty);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Address, Is.EqualTo("198.51.100.7"));
        }

        [Test]
        public void AMalformedAddress_IsStillRefused()
        {
            SetEnvironment("not-a-numeric-address", "45677", "key");

            PrivateSeatDriverOptions.TryParse(
                Args(PrivateSeatDriverOptions.SeatFlag, "red-player-01"), out var result, out var error);

            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain(PrivateSeatDriverOptions.AddressEnvironmentVariable));
        }

        [Test]
        public void AParentProcessId_WhenPresentAndValid_IsParsed()
        {
            SetEnvironment("127.0.0.1", "45677", "key");
            Environment.SetEnvironmentVariable(
                PrivateSeatDriverOptions.ParentProcessIdEnvironmentVariable, "4242");

            var parsed = PrivateSeatDriverOptions.TryParse(
                Args(PrivateSeatDriverOptions.SeatFlag, "red-player-01"), out var result, out var error);

            Assert.That(parsed, Is.True);
            Assert.That(error, Is.Empty);
            Assert.That(result.ParentProcessId, Is.EqualTo(4242));
        }

        [Test]
        public void AParentProcessId_WhenAbsentOrUnparsable_LeavesTheWatchdogOff()
        {
            SetEnvironment("127.0.0.1", "45677", "key");
            // SetEnvironment already clears the variable; this asserts that absence is a non-fatal
            // "no parent to watch" rather than a reason to refuse the whole launch.
            var parsed = PrivateSeatDriverOptions.TryParse(
                Args(PrivateSeatDriverOptions.SeatFlag, "red-player-01"), out var result, out var error);

            Assert.That(parsed, Is.True);
            Assert.That(error, Is.Empty);
            Assert.That(result.ParentProcessId, Is.EqualTo(0));
        }

        [Test]
        public void AnEmptySessionKey_IsRefused()
        {
            SetEnvironment("127.0.0.1", "45677", string.Empty);

            PrivateSeatDriverOptions.TryParse(
                Args(PrivateSeatDriverOptions.SeatFlag, "red-player-01"), out var result, out var error);

            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain(PrivateSeatDriverOptions.KeyEnvironmentVariable));
        }

        [Test]
        public void AnExplicitThinkSecondsAndSeed_AreBothHonored()
        {
            SetEnvironment("127.0.0.1", "45677", "key");

            PrivateSeatDriverOptions.TryParse(
                Args(
                    PrivateSeatDriverOptions.SeatFlag, "blue-player-03",
                    PrivateSeatDriverOptions.ThinkSecondsFlag, "0.1",
                    PrivateSeatDriverOptions.SeedFlag, "42"),
                out var result, out var error);

            Assert.That(error, Is.Empty);
            Assert.That(result.ThinkSeconds, Is.EqualTo(0.1).Within(0.0001));
            Assert.That(result.Seed, Is.EqualTo(42));
        }

        [Test]
        public void ANegativeThinkSeconds_IsRefused()
        {
            SetEnvironment("127.0.0.1", "45677", "key");

            PrivateSeatDriverOptions.TryParse(
                Args(
                    PrivateSeatDriverOptions.SeatFlag, "red-player-01",
                    PrivateSeatDriverOptions.ThinkSecondsFlag, "-1"),
                out var result, out var error);

            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain(PrivateSeatDriverOptions.ThinkSecondsFlag));
        }

        // --- The lobby presenter's side of the shipped control ------------------------------------

        [Test]
        public void PressingFillWithTestDrivers_RaisesTheEventRatherThanTouchingASession()
        {
            var view = new RecordingView();
            var presenter = new PrivateLobbyPresenter(view);
            var harness = PrivateOnlineWireHarness.Compose();
            presenter.AttachHost(harness.Session, string.Empty);
            var raised = 0;
            presenter.FillWithTestDriversRequested += () => raised++;

            var offered = presenter.Press(PrivateLobbyControl.FillWithTestDrivers, null);

            Assert.That(offered, Is.True);
            Assert.That(raised, Is.EqualTo(1),
                "The presenter names no process type - spawning is the one thing only the driver " +
                "may know how to do, exactly as HOST/JOIN/RECONNECT already work.");
        }

        [Test]
        public void SetTestDriverSeats_UpdatesTheModelsRowsAndOptionallyTheFeedback()
        {
            var view = new RecordingView();
            var presenter = new PrivateLobbyPresenter(view);
            var harness = PrivateOnlineWireHarness.Compose();
            presenter.AttachHost(harness.Session, string.Empty);

            presenter.SetTestDriverSeats(new[] { new SeatId("red-player-01") }, "1/6 test drivers spawned.");

            Assert.That(presenter.Model.Seats.Single(row => row.Seat.Value == "red-player-01")
                .IsTestDriverSeat, Is.True);
            Assert.That(presenter.Model.FeedbackLabel, Is.EqualTo("1/6 test drivers spawned."));
        }

        [Test]
        public void AttachingAFreshHost_ClearsAnyPreviousTestDriverTracking()
        {
            var view = new RecordingView();
            var presenter = new PrivateLobbyPresenter(view);
            var firstHarness = PrivateOnlineWireHarness.Compose();
            presenter.AttachHost(firstHarness.Session, string.Empty);
            presenter.SetTestDriverSeats(new[] { new SeatId("red-player-01") });

            var secondHarness = PrivateOnlineWireHarness.Compose();
            presenter.AttachHost(secondHarness.Session, string.Empty);

            Assert.That(presenter.Model.Seats.All(row => !row.IsTestDriverSeat), Is.True,
                "A brand new hosted session must not read as though last session's bots are still " +
                "sitting in it.");
        }

        // --- 8.35t corrective: the dial-address resolution the live defect found missing ---------

        private static PrivateSessionEndpoint Endpoint(string address, int port = 45677)
        {
            Assert.That(
                PrivateSessionEndpoint.TryCreate(address, port, out var endpoint, out var error), Is.True, error);
            return endpoint;
        }

        [Test]
        public void AutomaticLobbyHostBind_UnkeyedIsLoopbackAndKeyedIsIPv4Wildcard()
        {
            Assert.That(PrivateSessionEndpoint.TryCreateAutomaticHostBind(
                45677, false, out var localOnly, out var localError), Is.True, localError);
            Assert.That(localOnly.Address, Is.EqualTo(System.Net.IPAddress.Loopback),
                "Plaintext owner-private traffic remains local to this machine.");
            Assert.That(localOnly.ResolveLocalDialAddress(), Is.EqualTo("127.0.0.1"));

            Assert.That(PrivateSessionEndpoint.TryCreateAutomaticHostBind(
                45677, true, out var invited, out var invitedError), Is.True, invitedError);
            Assert.That(invited.Address, Is.EqualTo(System.Net.IPAddress.Any),
                "A keyed invited lobby accepts traffic through any local IPv4 interface.");
            Assert.That(invited.ResolveLocalDialAddress(), Is.EqualTo("127.0.0.1"),
                "The hosting player's own ordinary client still dials locally.");
        }

        [TestCase(0)]
        [TestCase(65536)]
        public void AutomaticLobbyHostBind_RejectsAnInvalidPort(int port)
        {
            Assert.That(PrivateSessionEndpoint.TryCreateAutomaticHostBind(
                port, true, out var endpoint, out var error), Is.False);
            Assert.That(endpoint, Is.Null);
            Assert.That(error, Is.Not.Empty);
        }

        /// <summary>
        /// The exact fix, pinned directly: before 8.35t, every spawned test driver dialed a
        /// hardcoded "127.0.0.1" regardless of what the host actually bound. A host bound to this
        /// machine's own real address (typed for a remote friend's port-forward, or to rehearse
        /// that same setup solo) then spawned drivers that dialed an address nothing was listening
        /// on - reproduced directly against a real socket pair before this fix was written (a
        /// <c>TcpListener</c> bound to a concrete address refuses a <c>TcpClient</c> dialing a
        /// different one: "the target machine actively refused it"). Lives on
        /// <see cref="PrivateSessionEndpoint"/> itself, in the adapter, so the one caller in
        /// Presentation - <c>PrivateLobbyDriver.BeginHosting</c> - names no socket type of its own.
        /// </summary>
        [Test]
        public void ResolveLocalDialAddress_ForAConcreteNonLoopbackBind_ReturnsThatSameAddressVerbatim()
        {
            Assert.That(Endpoint("192.168.0.22").ResolveLocalDialAddress(), Is.EqualTo("192.168.0.22"));
        }

        [Test]
        public void ResolveLocalDialAddress_ForTheOrdinaryLoopbackBind_ReturnsLoopbackVerbatim()
        {
            Assert.That(Endpoint("127.0.0.1").ResolveLocalDialAddress(), Is.EqualTo("127.0.0.1"));
        }

        /// <summary>
        /// A wildcard bind is not itself a valid destination to dial - every spawned child runs on
        /// this same machine, so loopback is the one address guaranteed to reach a wildcard-bound
        /// listener, and is substituted rather than handed back unusable.
        /// </summary>
        [Test]
        public void ResolveLocalDialAddress_ForTheIPv4WildcardBind_FallsBackToLoopback()
        {
            Assert.That(Endpoint("0.0.0.0").ResolveLocalDialAddress(), Is.EqualTo("127.0.0.1"));
        }

        [Test]
        public void ResolveLocalDialAddress_ForTheIPv6WildcardBind_FallsBackToLoopback()
        {
            Assert.That(Endpoint("::").ResolveLocalDialAddress(), Is.EqualTo("127.0.0.1"));
        }

        /// <summary>
        /// The general socket principle the fix relies on, proven with real sockets rather than
        /// assumed - confined to the loopback range (127.0.0.1 vs. 127.0.0.2) so the case runs
        /// identically on any machine's network stack, CI included, with no dependency on a real
        /// external NIC address being present.
        /// </summary>
        [Test]
        public void ARealListener_BoundToOneConcreteAddress_RefusesADialToADifferentOne()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Parse("127.0.0.1"), 0);
            listener.Start();
            try
            {
                var boundPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    Assert.Throws<System.Net.Sockets.SocketException>(() =>
                        client.Connect(System.Net.IPAddress.Parse("127.0.0.2"), boundPort));
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        /// <summary>
        /// The other half of the same principle: a wildcard bind DOES accept a loopback dial, which
        /// is exactly why <see cref="PrivateLobbyDriver.ResolveDialAddress"/> substitutes loopback
        /// for a wildcard bind rather than any other address.
        /// </summary>
        [Test]
        public void ARealListener_BoundToTheWildcardAddress_AcceptsALoopbackDial()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, 0);
            listener.Start();
            try
            {
                var boundPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    Assert.DoesNotThrow(() =>
                        client.Connect(System.Net.IPAddress.Parse("127.0.0.1"), boundPort));
                }
            }
            finally
            {
                listener.Stop();
            }
        }

        private sealed class RecordingView : IPrivateLobbyView
        {
            public void Render(PrivateLobbyModel model)
            {
            }
        }
    }
}
