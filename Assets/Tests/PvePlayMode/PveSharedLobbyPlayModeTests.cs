using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Network;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Pve.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.Pve.PlayMode
{
    public sealed class PveSharedLobbyPlayModeTests
    {
        private sealed class RemoteOutbox : IPrivateSessionClientOutbox
        {
            public TcpSessionClientTransport Transport;
            public void Send(byte[] frame) => Transport.Send(frame);
            public void Close() => Transport.Close();
        }
        private PveSharedLobby lobby;
        private GameObject entryObject;
        private PanelSettings entrySettings;
        private TcpSessionClientTransport remoteTransport;
        private PrivateSessionClient remote;
        private static T Field<T>(object instance, string name) => (T)instance.GetType()
            .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(instance);
        private IEnumerator Until(Func<bool> predicate, string message)
        {
            var end = Time.realtimeSinceStartup + 30;
            while (!predicate() && Time.realtimeSinceStartup < end)
            {
                if (remoteTransport != null)
                    foreach (var e in remoteTransport.Drain())
                    {
                        if (e.Kind == TransportEventKind.Connected) remote.OnConnected();
                        else if (e.Kind == TransportEventKind.Frame) remote.OnFrame(e.Frame);
                        else if (e.Kind == TransportEventKind.Disconnected) remote.OnDisconnected();
                    }
                yield return null;
            }
            Assert.That(predicate(), Is.True, message);
            yield return null;
        }
        private static void Click(Button button)
        {
            Assert.That(button, Is.Not.Null);
            Assert.That(button.enabledInHierarchy, Is.True);
            var point = button.worldBound.center;
            var picked = button.panel.Pick(point);
            Assert.That(picked == button || button.Contains(picked), Is.True, "Pointer reachable: " + button.text);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, mousePosition = point, button = 0, clickCount = 1 }))
            { down.target = picked; picked.SendEvent(down); }
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, mousePosition = point, button = 0, clickCount = 1 }))
            { up.target = picked; picked.SendEvent(up); }
        }

        [UnityTest]
        public IEnumerator SharedMapSelectionHidesExperimentsAndOnePlayerClaimsThreeHeroes()
        {
            PveSharedLobby.Register();
            entryObject = new GameObject("Shared map selection test");
            var entryDocument = entryObject.AddComponent<UIDocument>();
            entrySettings = Object.Instantiate(Resources.Load<PanelSettings>(ConfiguredMatchHudRenderer.RuntimePanelSettingsResourcePath));
            entryDocument.panelSettings = entrySettings;
            var entry = new PrivateLobbyUnityView();
            entryDocument.rootVisualElement.Add(entry.Root);
            entry.Render(PrivateLobbyModel.Entry(Array.Empty<SeatRosterEntry>(), "", "3v3"));
            entry.ShowPage(PrivateLobbyEntryPage.Host);
            yield return null;
            entry.Root.Q<DropdownField>("host-map-selector").value = PrivateLobbyMode.PveLabel;
            Assert.That(entry.CurrentEntry().ModeId, Is.EqualTo(PrivateLobbyMode.PveId));
            Assert.That(entry.CurrentEntry().Experiments.TeammateCoordination, Is.True);
            Assert.That(entry.CurrentEntry().Experiments.TurnTimer, Is.False);
            Assert.That(entry.Root.Q<Button>("experimental-button").style.display.value, Is.EqualTo(DisplayStyle.None));
            Object.Destroy(entryObject);
            yield return null;

            lobby = PveSharedLobby.Open(new PrivateLobbyEntry("", "45791", "shared-test", default,
                "Solo player", modeId: PrivateLobbyMode.PveId), true, 42);
            yield return Until(() => Field<PrivateSessionClient>(lobby, "active")?.State == PrivateSessionClientState.Admitted,
                "Host admitted before choosing seats");
            var view = Field<PrivateLobbyUnityView>(lobby, "view");
            for (var count = 1; count <= 3; count++)
            {
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.SeatPickName).ToList().First());
                var expected = count;
                yield return Until(() => Field<PrivateSessionClient>(lobby, "active").Roster.Count(r =>
                    r.Occupancy == SeatOccupancyState.Claimed) == expected, "Claim " + count);
            }
            Assert.That(Field<PrivateLobbyModel>(lobby, "model").Seats.Count(r => r.IsOwnSeat), Is.EqualTo(3));
            Click(view.Root.Query<Button>(PrivateLobbyUnityView.ControlName).ToList().Single(b => b.text == "READY"));
            for (var count = 1; count <= 3; count++)
            {
                yield return Until(() => Field<PrivateLobbyModel>(lobby, "model").Selection.IsOwnPick, "Next owned pick");
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.HeroOptionName).ToList().First(b => b.enabledSelf));
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.ControlName).ToList().Single(b => b.text == "PICK HERO"));
                var expected = count;
                yield return Until(() => Field<PrivateSessionClient>(lobby, "active").Roster.Count(r => r.HasChosenHero) == expected,
                    "Hero choice " + count);
            }
            yield return Until(() => Field<ConfiguredMatchSceneRuntime>(lobby, "runtime") != null, "Shared lobby enters match");
            var runtime = Field<ConfiguredMatchSceneRuntime>(lobby, "runtime");
            Assert.That(runtime.InitialState.Experiments.TeammateCoordination, Is.True);
            var root = lobby.GetComponent<UIDocument>().rootVisualElement;
            foreach (var button in root.Query<Button>().ToList().Where(b => b.name.StartsWith("pve-control-")))
            {
                Click(button);
                Assert.That(runtime.Seat.Id.Value, Is.EqualTo(button.name.Substring("pve-control-".Length)));
                Assert.That(((IExperimentalReadModel)runtime.ReadModel).Experiments.TeammateCoordination, Is.True);
            }
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("tmp/w045-solo-shared-lobby.png");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (lobby != null) Object.Destroy(lobby.gameObject);
            if (entryObject != null) Object.Destroy(entryObject);
            if (entrySettings != null) Object.Destroy(entrySettings);
            remote?.SendGoodbye();
            remoteTransport?.Dispose();
            remote = null; remoteTransport = null;
            foreach (var overlay in Object.FindObjectsByType<PveEncounterView>(FindObjectsSortMode.None)) Object.Destroy(overlay.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TwoPlayersSplitThreeSeatsAndReconnectWithoutSharingControl()
        {
            lobby = PveSharedLobby.Open(new PrivateLobbyEntry("", "45792", "two-player-test", default, "Host"), true, 71);
            yield return Until(() => Field<PrivateSessionClient>(lobby, "active")?.State == PrivateSessionClientState.Admitted, "Host admitted");
            var view = Field<PrivateLobbyUnityView>(lobby, "view");
            for (var count = 1; count <= 2; count++)
            {
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.SeatPickName).ToList().First());
                var expected = count;
                yield return Until(() => Field<PrivateSessionClient>(lobby, "active").Roster.Count(r => r.Occupancy == SeatOccupancyState.Claimed) == expected,
                    "Host claim " + count);
            }
            var server = Field<PrivateSessionHost>(lobby, "server");
            remoteTransport = new TcpSessionClientTransport();
            var remoteOutbox = new RemoteOutbox { Transport = remoteTransport };
            remote = new PrivateSessionClient(server.Identity, new SeatId("blue-player-03"), "two-player-test", remoteOutbox, new CryptoNonceSource());
            Assert.That(PrivateSessionEndpoint.TryCreate("127.0.0.1", 45792, out var endpoint, out _), Is.True);
            Assert.That(remoteTransport.Connect(endpoint, 3, out var problem), Is.True, problem);
            yield return Until(() => remote.GrantedSeat != null, "Other player claims third seat");
            Assert.That(Field<PrivateLobbyModel>(lobby, "model").Seats.Count(s => s.IsOwnSeat), Is.EqualTo(2));
            remote.TrySetReadiness(true);
            Click(view.Root.Query<Button>(PrivateLobbyUnityView.ControlName).ToList().Single(b => b.text == "READY"));
            for (var count = 1; count <= 2; count++)
            {
                yield return Until(() => Field<PrivateLobbyModel>(lobby, "model").Selection.IsOwnPick, "Host pick");
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.HeroOptionName).ToList().First(b => b.enabledSelf));
                Click(view.Root.Query<Button>(PrivateLobbyUnityView.ControlName).ToList().Single(b => b.text == "PICK HERO"));
                var expected = count;
                yield return Until(() => remote.Roster.Count(r => r.HasChosenHero) == expected, "Remote receives host pick");
            }
            Assert.That(remote.TryChooseHero("hero-brogan"), Is.True);
            yield return Until(() => Field<ConfiguredMatchSceneRuntime>(lobby, "runtime") != null && remote.HasMatchStarted, "Two-player match starts");
            Assert.That(lobby.GetComponent<UIDocument>().rootVisualElement.Query<Button>().ToList()
                .Count(b => b.name.StartsWith("pve-control-")), Is.EqualTo(2));
            remoteTransport.Dispose(); remote.OnDisconnected();
            yield return Until(() => server.IsPaused, "Disconnect pauses authority");
            Assert.That(remote.BeginReconnect(), Is.True);
            remoteTransport = new TcpSessionClientTransport();
            remoteOutbox.Transport = remoteTransport;
            Assert.That(remoteTransport.Connect(endpoint, 3, out problem), Is.True, problem);
            yield return Until(() => remote.State == PrivateSessionClientState.Seated && !server.IsPaused, "Same third seat reconnects");
            Assert.That(remote.TargetSeat.Value, Is.EqualTo("blue-player-03"));
        }
    }
}
