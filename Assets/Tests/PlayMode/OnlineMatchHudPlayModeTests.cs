using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>Milestone 8.35b's shipped lobby-to-existing-HUD handoff in a real scene.</summary>
    public sealed class OnlineMatchHudPlayModeTests
    {
        private sealed class Wire
        {
            private sealed class NonceSource : IPrivateSessionNonceSource
            {
                private byte value;

                public NonceSource(byte seed) => value = seed;

                public byte[] NextNonce()
                {
                    value++;
                    return Enumerable.Repeat(value, 32).ToArray();
                }
            }

            private sealed class Link
            {
                public PrivateSessionClient Client;
                public readonly PrivateSessionFrameAssembler ToHost = new PrivateSessionFrameAssembler();
                public readonly PrivateSessionFrameAssembler ToClient = new PrivateSessionFrameAssembler();
            }

            private sealed class ClientOutbox : IPrivateSessionClientOutbox
            {
                private readonly Wire wire;
                private readonly int ordinal;
                public ClientOutbox(Wire wire, int ordinal) { this.wire = wire; this.ordinal = ordinal; }
                public void Send(byte[] frame) => wire.toHost.Enqueue((ordinal, frame));
                public void Close() => wire.toHost.Enqueue((ordinal, null));
            }

            private sealed class HostOutbox : IPrivateSessionOutbox
            {
                private readonly System.Func<Wire> wire;
                public HostOutbox(System.Func<Wire> wire) { this.wire = wire; }
                public void Send(int ordinal, byte[] frame) => wire().toClient.Enqueue((ordinal, frame));
                public void Close(int ordinal) => wire().toClient.Enqueue((ordinal, null));
            }

            private readonly Queue<(int Ordinal, byte[] Frame)> toHost = new Queue<(int, byte[])>();
            private readonly Queue<(int Ordinal, byte[] Frame)> toClient = new Queue<(int, byte[])>();
            private readonly Dictionary<int, Link> links = new Dictionary<int, Link>();
            public PrivateSessionHost Host { get; private set; }

            public static Wire Compose(
                ConfiguredMatchSceneRuntime runtime,
                System.Func<long> timeSource = null,
                IDeferredMatchComposition composition = null)
            {
                Wire wire = null;
                wire = new Wire();
                wire.Host = new PrivateSessionHost(
                    runtime.Host, runtime.SessionIdentity, string.Empty,
                    new HostOutbox(() => wire), new NonceSource(1));
                // Milestone 8.35z54. The picks below reach a composition, and a host that admits hero
                // picks while holding none refuses the transition loudly rather than quietly fielding
                // the provisional line-up. Callers that care which heroes are composed pass the
                // scene's own root; the rest pass nothing and get the printed assignment.
                wire.Host.Composition = composition;
                var ordinal = 0;
                foreach (var seat in runtime.Host.Seats.Seats)
                {
                    ordinal++;
                    var link = new Link();
                    link.Client = new PrivateSessionClient(
                        runtime.SessionIdentity, seat.Id, string.Empty,
                        new ClientOutbox(wire, ordinal), new NonceSource((byte)(ordinal + 10)),
                        timeSource);
                    wire.links.Add(ordinal, link);
                    link.Client.OnConnected();
                    wire.Host.OnConnected(ordinal, 0);
                    wire.Pump();
                }

                foreach (var link in wire.links.Values)
                {
                    Assert.That(link.Client.TrySetReadiness(true), Is.True);
                    wire.Pump();
                }

                // Milestone 8.35z54. Readiness OPENS the picking; the LAST PICK starts the match.
                // This harness exists to reach a started network seat, so it now walks the picks the
                // way the shipped drivers do - out of the options each seat's own model offers, in
                // p.22's printed order - rather than expecting readiness alone to compose anything.
                var guard = 0;
                while (!wire.Host.HasMatchStarted && guard++ < wire.links.Count + 1)
                {
                    var picking = wire.Host.Selection.PickingSeat;
                    var picker = wire.links.Values.Single(value =>
                        value.Client.TargetSeat.Equals(picking));
                    var offered = PrivateLobbyModel.ForClient(picker.Client).Selection.Heroes
                        .First(option => option.IsAvailable);
                    Assert.That(picker.Client.TryChooseHero(offered.HeroIdValue), Is.True,
                        "Seat " + picking.Value + " could not take " + offered.HeroIdValue + ".");
                    wire.Pump();
                }

                Assert.That(wire.Host.HasMatchStarted, Is.True);
                Assert.That(wire.links.Values.All(value =>
                    value.Client.HasMatchStarted && value.Client.LatestProjection != null), Is.True);
                return wire;
            }

            public PrivateSessionClient Client(int ordinal) => links[ordinal].Client;

            public void LoseHostFor(int ordinal)
            {
                Host.OnDisconnected(ordinal);
                links[ordinal].Client.OnDisconnected();
                Pump();
            }

            public void Pump()
            {
                while (toHost.Count > 0 || toClient.Count > 0)
                {
                    if (toHost.Count > 0)
                    {
                        var item = toHost.Dequeue();
                        if (item.Frame == null) { Host.OnDisconnected(item.Ordinal); continue; }
                        var link = links[item.Ordinal];
                        Assert.That(link.ToHost.Add(item.Frame, 0, item.Frame.Length), Is.True);
                        while (link.ToHost.TryDequeue(out var frame)) Host.OnFrame(item.Ordinal, frame);
                    }
                    else
                    {
                        var item = toClient.Dequeue();
                        if (item.Frame == null) { links[item.Ordinal].Client.OnDisconnected(); continue; }
                        var link = links[item.Ordinal];
                        Assert.That(link.ToClient.Add(item.Frame, 0, item.Frame.Length), Is.True);
                        while (link.ToClient.TryDequeue(out var frame)) link.Client.OnFrame(frame);
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator HostingPlayerKeepsOwnerProjectionAndAloneGetsDirectorDebugTools()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            var load = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root?.Runtime, Is.Not.Null);
            var wire = Wire.Compose(root.Runtime, composition: root);

            var setupCommand = root.Runtime.EnumerateProjectedCommands().First();
            Assert.That(root.Runtime.SubmitProjectedCommand(setupCommand), Is.True);
            wire.Host.RepublishProjections();
            wire.Pump();

            var hostingClient = wire.Client(1);
            var remoteClient = wire.Client(2);
            root.Runtime.AttachOnlineClient(
                hostingClient,
                exposeHostDirectorTools: true,
                republishAfterHostDebugChange: wire.Host.RepublishProjections);
            root.Runtime.RefreshAll();
            yield return null;

            Assert.That(root.Runtime.ReadModel, Is.TypeOf<NetworkMatchReadModel>(),
                "Playing as host still reads the same owner projection as a joining player.");
            AssertPlayerTrayStaysHome(root.Runtime);
            Assert.That(root.Runtime.ReadModel.Cards.All(card =>
                card.OwnerId.Equals(root.Runtime.Seat.HeroId) ||
                card.State.Location != GuardsDigital.Core.Cards.CardLocation.Hand), Is.True);
            var director = root.Hud.RootElement.Q<Button>("gd-director-toggle");
            Assert.That(director, Is.Not.Null);
            Assert.That(director.style.display.value, Is.EqualTo(DisplayStyle.Flex),
                "The local hosting player alone is deliberately offered the debug drawer.");

            var remoteReceiptBefore = remoteClient.ProjectionRevision;
            Assert.That(root.Runtime.UndoRestartPresenter.Undo(), Is.True);
            wire.Pump();
            Assert.That(remoteClient.ProjectionRevision, Is.GreaterThan(remoteReceiptBefore));
            Assert.That(remoteClient.LatestProjection.Version, Is.EqualTo(root.Runtime.Host.Version),
                "A host debug mutation is republished to every owner-scoped remote client.");
        }

        [UnityTest]
        public IEnumerator StartedNetworkSeat_UsesExistingHud_AndHostLossClearsItsControls()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            var load = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            while (!load.isDone) yield return null;
            yield return null;

            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root?.Runtime, Is.Not.Null);
            var now = 10 * System.TimeSpan.TicksPerSecond;
            var wire = Wire.Compose(root.Runtime, () => now, root);
            var client = wire.Client(1);
            var lobby = root.gameObject.AddComponent<PrivateLobbyDriver>();
            lobby.Configure(root);
            typeof(PrivateLobbyDriver).GetField(
                    "machine", System.Reflection.BindingFlags.Instance |
                               System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(lobby, client);
            typeof(PrivateLobbyDriver).GetMethod(
                    "Update", System.Reflection.BindingFlags.Instance |
                              System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(lobby, null);
            yield return null;

            Assert.That(root.Runtime.ReadModel, Is.TypeOf<NetworkMatchReadModel>());
            Assert.That(root.Runtime.Seat.Id, Is.EqualTo(client.TargetSeat));
            AssertPlayerTrayStaysHome(root.Runtime);
            Assert.That(root.Runtime.ReadModel.Cards.All(card =>
                card.OwnerId.Equals(root.Runtime.Seat.HeroId) ||
                card.State.Location != GuardsDigital.Core.Cards.CardLocation.Hand), Is.True);
            var director = root.Hud.RootElement.Query<Button>().ToList()
                .SingleOrDefault(button => button.text == "DIRECTOR");
            Assert.That(director == null || director.style.display.value == DisplayStyle.None, Is.True);
            var status = lobby.GetComponent<UIDocument>().rootVisualElement.Q<Label>(
                PrivateLobbyDriver.MatchStatusName);
            // Item 3 of the 2026-09-06 log drop-down refinements INVERTED this assertion, and
            // deliberately: it used to read Flex + "CONNECTED", which is precisely the banner the
            // user screenshotted sitting across the open match log's header. "A HEALTHY connection
            // is the default state and earns no screen - show NOTHING when connected." The seat is
            // still proved connected by everything around this line (the read model, the seat id,
            // the private hand); it is simply no longer proved by a slab of chrome saying so.
            Assert.That(status.style.display.value, Is.EqualTo(DisplayStyle.None),
                "A healthy connection draws no banner at all.");
            Assert.That(status.text, Is.Empty,
                "And it is genuinely empty, not merely hidden with its sentence retained.");

            var initialHands = root.Runtime.HandsView.Model;
            var initialVersion = client.LatestProjection.Version;
            var initialReceipt = client.ProjectionRevision;
            wire.Host.RepublishProjections();
            wire.Pump();
            typeof(PrivateLobbyDriver).GetMethod(
                    "Update", System.Reflection.BindingFlags.Instance |
                              System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(lobby, null);
            Assert.That(client.LatestProjection.Version, Is.EqualTo(initialVersion),
                "A public/session-only projection receipt need not advance gameplay authority.");
            Assert.That(client.ProjectionRevision, Is.GreaterThan(initialReceipt));
            Assert.That(root.Runtime.HandsView.Model, Is.Not.SameAs(initialHands),
                "A same-version projection receipt must still redraw the attached HUD.");

            initialHands = root.Runtime.HandsView.Model;
            var homeHero = root.Runtime.Seat.HeroId;
            var card = initialHands.ViewedHand.PlayableTrayCards.First().CardInstanceId;
            Assert.That(root.Runtime.HandsPresenter.CommitCardImmediately(card), Is.True);
            root.Runtime.RefreshAll();
            var waitingHands = root.Runtime.HandsView.Model;
            Assert.That(root.Runtime.EnumerateProjectedCommands(), Is.Empty,
                "The journey helper must not synthesize an End-of-Round or commitment command while input is disabled.");
            var waitingCardsAreInert = waitingHands.ViewedHand.PlayableTrayCards.All(
                value => !value.IsSelectable);
            typeof(PrivateLobbyDriver).GetMethod(
                    "Update", System.Reflection.BindingFlags.Instance |
                              System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(lobby, null);
            Assert.That(status.text, Does.Contain("WAITING FOR THE HOST"));
            now += PrivateSessionClient.DelayedHostResponseThresholdTicks;
            typeof(PrivateLobbyDriver).GetMethod(
                    "Update", System.Reflection.BindingFlags.Instance |
                              System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(lobby, null);
            Assert.That(status.text, Does.Contain("TAKING LONGER"),
                "The visible status must refresh when only the presentation clock crosses its delay threshold.");
            wire.Pump();
            root.Runtime.RefreshAll();
            var answeredHands = root.Runtime.HandsView.Model;
            Assert.That(waitingHands, Is.Not.SameAs(initialHands),
                "A same-version waiting transition must invalidate presenter caches.");
            Assert.That(waitingCardsAreInert, Is.True,
                "No commitment remains actionable while the host answer is in flight.");
            Assert.That(answeredHands, Is.Not.SameAs(waitingHands),
                "A received host projection must invalidate presenter caches.");
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(homeHero),
                "An online seat keeps its own private tray when the awaited hero advances.");

            var resources = root.OwnedHighlightNativeResourceCount;
            var children = root.Hud.RootElement.childCount;
            for (var index = 0; index < 20; index++) root.Runtime.RefreshAll();
            Assert.That(root.OwnedHighlightNativeResourceCount, Is.EqualTo(resources));
            Assert.That(root.Hud.RootElement.childCount, Is.EqualTo(children));

            typeof(PrivateLobbyDriver).GetMethod(
                    "Leave", System.Reflection.BindingFlags.Instance |
                             System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(lobby, null);
            root.Runtime.RefreshAll();
            Assert.That(root.Runtime.ReadModel.IsInputEnabled, Is.False,
                "Leaving must disable the retained match surface immediately.");
            Assert.That(status.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(status.text, Is.Empty);

            var replacementWire = Wire.Compose(root.Runtime, null, root);
            var replacement = replacementWire.Client(2);
            typeof(PrivateLobbyDriver).GetField(
                    "machine", System.Reflection.BindingFlags.Instance |
                               System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(lobby, replacement);
            typeof(PrivateLobbyDriver).GetMethod(
                    "Update", System.Reflection.BindingFlags.Instance |
                              System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(lobby, null);
            Assert.That(root.Runtime.Seat.Id, Is.EqualTo(replacement.TargetSeat),
                "A later join must bind its own seat rather than retaining the departed one.");
            AssertPlayerTrayStaysHome(root.Runtime);
            Assert.That(root.Runtime.ReadModel.IsInputEnabled, Is.True);
            Assert.That(root.Runtime.ReadModel.Cards.All(value =>
                    value.OwnerId.Equals(root.Runtime.Seat.HeroId) ||
                    value.State.Location != GuardsDigital.Core.Cards.CardLocation.Hand), Is.True,
                "Reattachment must use the new owner projection and the original admitted static content.");

            replacementWire.LoseHostFor(2);
            root.Runtime.RefreshAll();
            Assert.That(root.Runtime.ReadModel.DecisionContract, Is.Null);
            Assert.That(root.Runtime.ReadModel.AvailableTurnActions, Is.Empty);
            Assert.That(root.Runtime.ReadModel.TeamAgreement, Is.Null);
        }

        private static void AssertPlayerTrayStaysHome(ConfiguredMatchSceneRuntime runtime)
        {
            var version = runtime.ReadModel.Version;
            foreach (var hero in runtime.Setup.Heroes.Where(value => !value.HeroId.Equals(runtime.Seat.HeroId)))
                Assert.That(runtime.SelectViewedHero(hero.HeroId), Is.False);
            runtime.RefreshAll();
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(runtime.Seat.HeroId));
            Assert.That(runtime.HandsView.Model.Hands.Select(hand => hand.HeroId),
                Is.EqualTo(new[] { runtime.Seat.HeroId }));
            Assert.That(runtime.ReadModel.Version, Is.EqualTo(version));
        }
    }
}
