using GuardsDigital.Network;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.31a. A private-online process must keep pumping its session while its window
    /// is hidden or unfocused.
    /// </summary>
    /// <remarks>
    /// The checked-in player pauses its whole loop on focus loss (`runInBackground: 0`), which is
    /// right for a normal launch and fatal for a session pumped on Update: 8.31's first manual
    /// windowed run showed seven processes of which only the focused one advanced, the handshake
    /// silently stalled, and unfocused windows froze before their first rendered frame. Configuring
    /// a driver is activating that process's network role, so that is where background execution
    /// is switched on - and only there, which is what keeps a normally launched build's focus
    /// behavior untouched.
    /// </remarks>
    public sealed class PrivateOnlineBackgroundExecutionTests
    {
        private bool saved;

        [SetUp]
        public void RememberTheProcessSetting()
        {
            saved = UnityEngine.Application.runInBackground;
            UnityEngine.Application.runInBackground = false;
        }

        [TearDown]
        public void RestoreTheProcessSetting()
        {
            UnityEngine.Application.runInBackground = saved;
        }

        [Test]
        public void ConfiguringTheHostDriver_EnablesBackgroundExecution()
        {
            var carrier = new GameObject(nameof(ConfiguringTheHostDriver_EnablesBackgroundExecution));
            try
            {
                var driver = carrier.AddComponent<PrivateHostDriver>();
                driver.Configure(null, PrivateSessionLaunchOptions.Parse(
                    new[] { PrivateSessionLaunchOptions.HostFlag }));

                Assert.That(UnityEngine.Application.runInBackground, Is.True,
                    "A host that pauses when its window loses focus stops pumping every " +
                    "connection it holds; activating the host role must enable background " +
                    "execution for this process.");
            }
            finally
            {
                Object.DestroyImmediate(carrier);
            }
        }

        [Test]
        public void ConfiguringTheClientDriver_EnablesBackgroundExecution()
        {
            var carrier = new GameObject(nameof(ConfiguringTheClientDriver_EnablesBackgroundExecution));
            try
            {
                var driver = carrier.AddComponent<PrivateClientDriver>();
                driver.Configure(null, PrivateSessionLaunchOptions.Parse(new[]
                {
                    PrivateSessionLaunchOptions.ClientFlag,
                    PrivateSessionLaunchOptions.AddressFlag, "127.0.0.1:45677",
                    PrivateSessionLaunchOptions.SeatFlag, "red-player-01"
                }));

                Assert.That(UnityEngine.Application.runInBackground, Is.True,
                    "An unfocused client that stops pumping never answers the challenge and " +
                    "never claims its seat; activating the client role must enable background " +
                    "execution for this process.");
            }
            finally
            {
                Object.DestroyImmediate(carrier);
            }
        }

        [Test]
        public void RehearsalOptions_AllowAPlayingHostSeatOnlyForThePackageSmoke()
        {
            var addressName = PrivateOnlineRehearsalDriver.AddressEnvironmentVariable;
            var portName = PrivateOnlineRehearsalDriver.PortEnvironmentVariable;
            var keyName = PrivateOnlineRehearsalDriver.KeyEnvironmentVariable;
            var savedAddress = System.Environment.GetEnvironmentVariable(addressName);
            var savedPort = System.Environment.GetEnvironmentVariable(portName);
            var savedKey = System.Environment.GetEnvironmentVariable(keyName);
            try
            {
                System.Environment.SetEnvironmentVariable(addressName, "127.0.0.1");
                System.Environment.SetEnvironmentVariable(portName, "45677");
                System.Environment.SetEnvironmentVariable(keyName, "package-smoke-key");
                var common = new[]
                {
                    PrivateSessionLaunchOptions.LobbyFlag,
                    PrivateOnlineRehearsalDriver.RoleFlag, "host",
                    PrivateOnlineRehearsalDriver.ModeFlag, "smoke",
                    PrivateOnlineRehearsalDriver.SeatFlag, "red-player-02"
                };

                Assert.That(PrivateOnlineRehearsalOptions.TryParse(
                    common, out var playingHost, out var error), Is.True, error);
                Assert.That(playingHost.Seat.Value, Is.EqualTo("red-player-02"));

                common[4] = "match";
                Assert.That(PrivateOnlineRehearsalOptions.TryParse(
                    common, out _, out error), Is.False);
                StringAssert.Contains("seatless outside playing-host smoke", error);
            }
            finally
            {
                System.Environment.SetEnvironmentVariable(addressName, savedAddress);
                System.Environment.SetEnvironmentVariable(portName, savedPort);
                System.Environment.SetEnvironmentVariable(keyName, savedKey);
            }
        }
    }
}
