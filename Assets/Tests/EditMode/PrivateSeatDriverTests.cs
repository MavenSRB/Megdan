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

        [SetUp]
        public void SaveEnvironment()
        {
            savedAddress = Environment.GetEnvironmentVariable(
                PrivateSeatDriverOptions.AddressEnvironmentVariable);
            savedPort = Environment.GetEnvironmentVariable(
                PrivateSeatDriverOptions.PortEnvironmentVariable);
            savedKey = Environment.GetEnvironmentVariable(
                PrivateSeatDriverOptions.KeyEnvironmentVariable);
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
        }

        private static void SetEnvironment(string address, string port, string key)
        {
            Environment.SetEnvironmentVariable(PrivateSeatDriverOptions.AddressEnvironmentVariable, address);
            Environment.SetEnvironmentVariable(PrivateSeatDriverOptions.PortEnvironmentVariable, port);
            Environment.SetEnvironmentVariable(PrivateSeatDriverOptions.KeyEnvironmentVariable, key);
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

        [Test]
        public void ANonLoopbackAddress_IsRefused()
        {
            SetEnvironment("198.51.100.7", "45677", "key");

            PrivateSeatDriverOptions.TryParse(
                Args(PrivateSeatDriverOptions.SeatFlag, "red-player-01"), out var result, out var error);

            Assert.That(result, Is.Null);
            Assert.That(error, Does.Contain("loopback"));
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

        private sealed class RecordingView : IPrivateLobbyView
        {
            public void Render(PrivateLobbyModel model)
            {
            }
        }
    }
}
