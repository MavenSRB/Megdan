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
    }
}
