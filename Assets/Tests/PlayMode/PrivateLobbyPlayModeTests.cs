using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.35a. The primitive private lobby as a player meets it: real session machines
    /// under a real panel, the words drawn where a player reads them, and the controls pressed the
    /// way a pointer presses them.
    /// </summary>
    /// <remarks>
    /// <para>The session half runs a real <see cref="PrivateSessionHost"/> and real
    /// <see cref="PrivateSessionClient"/> machines over the scene's own authority through an
    /// ordered in-memory wire - the shape milestone 8.34's Play Mode fixture established - so what
    /// is asserted about readiness, the transition and host loss is what the shipped session layer
    /// does. The join-failure journey uses the shipped driver's own socket path instead, because
    /// the thing being proven there is exactly that an unreachable host reaches the player as
    /// words.</para>
    /// <para>What this fixture deliberately does not do is play the match through the lobby's
    /// handoff: the seat-scoped HUD over a network client is milestone 8.35b's whole subject. What
    /// it proves at the handoff point is that the lobby stands aside leaving no control behind,
    /// which is the state that milestone starts from.</para>
    /// </remarks>
    public sealed class PrivateLobbyPlayModeTests
    {
        /// <summary>The supported desktop validation resolutions the panel scaling fixture names.</summary>
        private static readonly Vector2[] SupportedViewports =
        {
            new Vector2(1280f, 720f),
            new Vector2(1600f, 900f),
            new Vector2(1920f, 1080f),
            new Vector2(2560f, 1440f),
            new Vector2(1280f, 800f),
            new Vector2(3440f, 1440f)
        };

        /// <summary>One ordered in-memory wire between the scene's authority and real clients.</summary>
        private sealed class LobbyWire
        {
            private sealed class Link
            {
                public PrivateSessionClient Machine;
                public PrivateSessionFrameAssembler FromHost = new PrivateSessionFrameAssembler();
                public PrivateSessionFrameAssembler ToHost = new PrivateSessionFrameAssembler();
                public int Ordinal;
                public string Seat;
                public bool Closed;
            }

            private sealed class ClientOutbox : IPrivateSessionClientOutbox
            {
                private readonly LobbyWire wire;
                private readonly Link link;

                public ClientOutbox(LobbyWire wire, Link link)
                {
                    this.wire = wire;
                    this.link = link;
                }

                public void Send(byte[] frame) => wire.toHost.Enqueue((link.Ordinal, frame));

                // A close is queued behind whatever was already sent. Closing out of band would
                // overtake the frame that explains it, which is exactly how a refusal arrives at a
                // client that has already been told the connection ended.
                public void Close() => wire.toHost.Enqueue((link.Ordinal, null));
            }

            private sealed class HostOutbox : IPrivateSessionOutbox
            {
                private readonly System.Func<LobbyWire> wire;

                public HostOutbox(System.Func<LobbyWire> wire)
                {
                    this.wire = wire;
                }

                public void Send(int connectionOrdinal, byte[] frame) =>
                    wire().toClient.Enqueue((connectionOrdinal, frame));

                public void Close(int connectionOrdinal) =>
                    wire().toClient.Enqueue((connectionOrdinal, null));
            }

            private sealed class Nonces : IPrivateSessionNonceSource
            {
                private long counter;

                public byte[] NextNonce()
                {
                    counter++;
                    var nonce = new byte[8];
                    for (var index = 0; index < 8; index++)
                    {
                        nonce[index] = (byte)((counter >> (8 * index)) & 0xFF);
                    }

                    return nonce;
                }
            }

            private readonly Queue<(int Ordinal, byte[] Bytes)> toHost = new Queue<(int, byte[])>();
            private readonly Queue<(int Ordinal, byte[] Bytes)> toClient = new Queue<(int, byte[])>();
            private readonly Dictionary<int, Link> links = new Dictionary<int, Link>();

            private LobbyWire(PrivateSessionHost session, PrivateSessionIdentity identity)
            {
                Session = session;
                Identity = identity;
            }

            public PrivateSessionHost Session { get; }
            public PrivateSessionIdentity Identity { get; }

            public static LobbyWire Compose(IMatchHost matchHost, PrivateSessionIdentity identity)
            {
                LobbyWire wire = null;
                var session = new PrivateSessionHost(
                    matchHost, identity, string.Empty, new HostOutbox(() => wire), new Nonces());
                wire = new LobbyWire(session, identity);
                return wire;
            }

            public PrivateSessionClient Client(int ordinal) => links[ordinal].Machine;

            public PrivateSessionClient Connect(int ordinal, string seat)
            {
                var link = new Link { Ordinal = ordinal, Seat = seat };
                link.Machine = new PrivateSessionClient(
                    Identity, new SeatId(seat), string.Empty,
                    new ClientOutbox(this, link), new Nonces());
                links[ordinal] = link;
                link.Machine.OnConnected();
                Session.OnConnected(ordinal, 0);
                Pump();
                return link.Machine;
            }

            public void LoseTheLink(int ordinal)
            {
                var link = links[ordinal];
                link.Closed = true;
                Session.OnDisconnected(ordinal);
                link.Machine.OnDisconnected();
                Pump();
            }

            public void Pump()
            {
                var progressed = true;
                while (progressed)
                {
                    progressed = false;
                    if (toHost.Count > 0)
                    {
                        var action = toHost.Dequeue();
                        DeliverToHost(action.Ordinal, action.Bytes);
                        progressed = true;
                        continue;
                    }

                    if (toClient.Count > 0)
                    {
                        var action = toClient.Dequeue();
                        DeliverToClient(action.Ordinal, action.Bytes);
                        progressed = true;
                    }
                }
            }

            private void DeliverToHost(int ordinal, byte[] bytes)
            {
                if (bytes == null)
                {
                    CloseFromClient(ordinal);
                    return;
                }

                if (!links.TryGetValue(ordinal, out var link) || link.Closed ||
                    !link.ToHost.Add(bytes, 0, bytes.Length))
                {
                    return;
                }

                while (link.ToHost.TryDequeue(out var frame))
                {
                    Session.OnFrame(ordinal, frame);
                }
            }

            private void DeliverToClient(int ordinal, byte[] bytes)
            {
                if (bytes == null)
                {
                    CloseFromHost(ordinal);
                    return;
                }

                if (!links.TryGetValue(ordinal, out var link) || link.Closed ||
                    !link.FromHost.Add(bytes, 0, bytes.Length))
                {
                    return;
                }

                while (link.FromHost.TryDequeue(out var frame))
                {
                    link.Machine.OnFrame(frame);
                }
            }

            private void CloseFromHost(int ordinal)
            {
                if (links.TryGetValue(ordinal, out var link) && !link.Closed)
                {
                    link.Closed = true;
                    link.Machine.OnDisconnected();
                }
            }

            private void CloseFromClient(int ordinal)
            {
                if (links.TryGetValue(ordinal, out var link) && !link.Closed)
                {
                    link.Closed = true;
                    Session.OnDisconnected(ordinal);
                }
            }
        }

        /// <summary>
        /// Six invited players see one table: their own seat named, both teams written out, and
        /// every seat's status in words rather than in a colour.
        /// </summary>
        [UnityTest]
        public IEnumerator SixSeatedPlayers_ReadOneTableWithTeamAndHeroInWords()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            driver.Presenter.AttachClient(wire.Client(1));
            yield return null;

            var rows = Named(driver, PrivateLobbyUnityView.SeatRowName).ToArray();
            Assert.That(rows.Length, Is.EqualTo(seats.Count),
                "The lobby draws one row per stable seat.");
            var teams = Texts(driver, "gd-lobby-seat-team").Distinct().OrderBy(v => v).ToArray();
            Assert.That(teams, Is.EqualTo(new[] { "BLUE TEAM", "RED TEAM" }),
                "Both sides are readable as words on the panel itself.");
            Assert.That(Texts(driver, "gd-lobby-seat-hero").All(text => text.Length > 0), Is.True);
            Assert.That(Texts(driver, "gd-lobby-seat-occupancy").Count(text => text == "SEATED"),
                Is.EqualTo(seats.Count));
            Assert.That(Texts(driver, "gd-lobby-seat-readiness").Count(text => text == "NOT READY"),
                Is.EqualTo(seats.Count));
            Assert.That(Texts(driver, "gd-lobby-seat-own").Count(text => text == "YOU"),
                Is.EqualTo(1), "A player must be able to find their own row on the panel.");
            Assert.That(ControlLabels(driver), Is.EqualTo(new[] { "LEAVE", "READY" }));
        }

        /// <summary>
        /// The sixth ready seat begins the match, and the lobby stands aside without leaving one
        /// control a player could still press.
        /// </summary>
        [UnityTest]
        public IEnumerator ReadyingTheSixthSeat_BeginsTheMatchAndStandsTheLobbyAside()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            var mine = wire.Client(1);
            driver.Presenter.AttachClient(mine);
            yield return null;

            for (var ordinal = 2; ordinal <= seats.Count; ordinal++)
            {
                Assert.That(wire.Client(ordinal).TrySetReadiness(true), Is.True);
                wire.Pump();
            }

            driver.Presenter.Refresh();
            yield return null;
            Assert.That(wire.Session.HasMatchStarted, Is.False,
                "Five ready seats is not six, and the sixth is the one being pressed.");

            UiPlayer.Click(Control(driver, "READY"));
            wire.Pump();
            driver.Presenter.Refresh();
            yield return null;
            yield return null;

            Assert.That(mine.IsReady, Is.True,
                "The pressed control reached the host and came back in the roster.");
            Assert.That(wire.Session.HasMatchStarted, Is.True);
            Assert.That(mine.HasMatchStarted, Is.True);
            Assert.That(UiPlayer.IsShown(driver.View.Root), Is.False,
                "The lobby hands off to the match surface rather than sitting over it.");
            Assert.That(ShownButtons(driver), Is.Empty,
                "A lobby that stood aside must leave no control a player could still press; it " +
                "still draws " + string.Join(", ", ControlLabels(driver)) + ".");
        }

        /// <summary>A refusal is words on the panel, not a state with nothing said about it.</summary>
        [UnityTest]
        public IEnumerator ADuplicateSeatClaim_IsDrawnAsWords()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = LobbyWire.Compose(Runtime().Host, Runtime().SessionIdentity);
            var seat = wire.Session.Roster[0].Seat.Value;
            wire.Connect(1, seat);
            var refused = wire.Connect(2, seat);
            driver.Presenter.AttachClient(refused);
            yield return null;

            Assert.That(refused.RejectionReason,
                Is.EqualTo(SessionRejectionReason.SeatAlreadyClaimed));
            Assert.That(Text(driver, PrivateLobbyUnityView.StatusName), Is.EqualTo("REFUSED"));
            Assert.That(Text(driver, PrivateLobbyUnityView.FeedbackName),
                Does.StartWith("That seat is already taken."));
            Assert.That(ControlLabels(driver), Is.EqualTo(new[] { "LEAVE" }),
                "A refused connection is offered nothing but leaving.");
        }

        /// <summary>
        /// When the host is gone the panel keeps what the player was shown and offers only the
        /// authorized return and leaving.
        /// </summary>
        [UnityTest]
        public IEnumerator HostLoss_DrawsOnlyAnAuthorizedReturnAndLeaving()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out _);
            var mine = wire.Client(1);
            driver.Presenter.AttachClient(mine);
            yield return null;
            Assert.That(ControlLabels(driver), Is.EqualTo(new[] { "LEAVE", "READY" }));

            wire.LoseTheLink(1);
            driver.Presenter.Refresh();
            yield return null;

            Assert.That(mine.State, Is.EqualTo(PrivateSessionClientState.HostLost));
            Assert.That(Text(driver, PrivateLobbyUnityView.StatusName), Is.EqualTo("HOST LOST"));
            Assert.That(ControlLabels(driver), Is.EqualTo(new[] { "LEAVE", "RECONNECT" }),
                "Milestone 8.34's rule drawn: nothing stands that the session would refuse.");
            Assert.That(Named(driver, PrivateLobbyUnityView.SeatRowName).Count(), Is.EqualTo(6),
                "The player still reads the table they were in; what they lose is the ability to " +
                "act in it.");
        }

        /// <summary>Leaving returns the player to the form the invitation is typed into.</summary>
        [UnityTest]
        public IEnumerator LeavingTheLobby_ReturnsToTheEntryForm()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out _);
            driver.Presenter.AttachClient(wire.Client(1));
            yield return null;

            UiPlayer.Click(Control(driver, "LEAVE"));
            yield return null;

            Assert.That(driver.Presenter.Model.Role, Is.EqualTo(PrivateLobbyRole.None));
            Assert.That(ControlLabels(driver), Is.EqualTo(new[] { "HOST", "JOIN" }));
            Assert.That(UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.EntryName)), Is.True);
            foreach (var field in new[]
                     {
                         PrivateLobbyUnityView.AddressFieldName,
                         PrivateLobbyUnityView.PortFieldName,
                         PrivateLobbyUnityView.KeyFieldName
                     })
            {
                Assert.That(UiPlayer.IsShown(Element(driver, field)), Is.True,
                    "The whole invitation - address, port and key - is typed into this form.");
            }

            Assert.That(Named(driver, PrivateLobbyUnityView.SeatPickName).Count(), Is.EqualTo(6),
                "Every stable seat is offered as a pick before a host has answered.");
        }

        /// <summary>
        /// A host that is not there reaches the player as a sentence on the entry form. This one
        /// goes through the shipped driver's own socket path, because that is the thing being
        /// proven.
        /// </summary>
        [UnityTest]
        public IEnumerator AJoinToAHostThatIsNotThere_IsDrawnOnTheEntryForm()
        {
            yield return LoadTheLobby();
            var driver = Driver();

            // A throwaway bind learns a free port and releases it, leaving an address on this
            // machine that nothing is listening on.
            var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            probe.Start();
            var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            Field(driver, PrivateLobbyUnityView.PortFieldName).value =
                port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            UiPlayer.Click(Named(driver, PrivateLobbyUnityView.SeatPickName).First());
            yield return null;
            Assert.That(Text(driver, PrivateLobbyUnityView.PickedSeatName),
                Does.StartWith("SEAT: "));

            UiPlayer.Click(Control(driver, "JOIN"));
            yield return null;

            var said = Text(driver, PrivateLobbyUnityView.FeedbackName);
            Assert.That(said, Does.StartWith("The host could not be reached"),
                "A failed join says so; the entry form is still standing behind it.");
            Assert.That(said.Contains("127.0.0.1"), Is.False,
                "Milestone 8.31's rule: a player-facing diagnostic never names an address.");
            Assert.That(ControlLabels(driver), Is.EqualTo(new[] { "HOST", "JOIN" }));
        }

        /// <summary>
        /// Milestone 8.35a1's closing check. An unkeyed session derives no channel keys, so its
        /// link stays plaintext, and the architecture record permits an owner-private byte to
        /// cross plaintext only on the loopback interface. The command line has always refused an
        /// unkeyed non-loopback bind; the lobby's own HOST control - the path an operator actually
        /// uses to host an invited playtest - did not, which milestone 8.36's readiness review
        /// found. The refusal has to come before the bind, so it is read here off the entry form.
        /// </summary>
        /// <remarks>
        /// The address is a documentation-range one this machine does not own, so no interface can
        /// be bound here whatever the rule decides and the test opens no listener of its own. That
        /// is also what makes the pre-fix observation legible rather than dangerous: before the
        /// fix this journey reached the bind attempt and the lobby reported the failed bind, which
        /// on an operator's own public interface would instead have been an unkeyed public host.
        /// </remarks>
        [UnityTest]
        public IEnumerator HostingOffLoopbackWithoutASessionKey_IsRefusedBeforeAnyBind()
        {
            yield return LoadTheLobby();
            var driver = Driver();

            Field(driver, PrivateLobbyUnityView.AddressFieldName).value = "203.0.113.10";
            Field(driver, PrivateLobbyUnityView.KeyFieldName).value = string.Empty;
            yield return null;

            UiPlayer.Click(Control(driver, "HOST"));
            yield return null;

            var said = Text(driver, PrivateLobbyUnityView.FeedbackName);
            Assert.That(said, Does.Contain("session key"),
                "An unkeyed bind off loopback must be refused for the reason it is refused for - " +
                "the missing key - rather than being carried as far as a bind attempt. The lobby " +
                "said: '" + said + "'.");
            Assert.That(said.Length, Is.LessThanOrEqualTo(PrivateLobbyLabels.MaxLabelChars),
                "A refusal is one bounded line like every other the lobby draws.");
            Assert.That(said.Contains("203.0.113.10"), Is.False,
                "Milestone 8.31's rule: a player-facing diagnostic never names an address.");
            Assert.That(ControlLabels(driver), Is.EqualTo(new[] { "HOST", "JOIN" }),
                "A refused host stays on the entry form with both of its controls.");
            Assert.That(driver.Presenter.Model.Role, Is.EqualTo(PrivateLobbyRole.None),
                "A refused host opened no session.");
        }

        /// <summary>
        /// The rule is about the key and not about refusing to host at all: loopback without a key
        /// is the local default, and it must still reach a listening host.
        /// </summary>
        [UnityTest]
        public IEnumerator HostingLoopbackWithoutASessionKey_IsStillAccepted()
        {
            yield return LoadTheLobby();
            var driver = Driver();

            var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            probe.Start();
            var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            Field(driver, PrivateLobbyUnityView.AddressFieldName).value = "127.0.0.1";
            Field(driver, PrivateLobbyUnityView.PortFieldName).value =
                port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Field(driver, PrivateLobbyUnityView.KeyFieldName).value = string.Empty;
            yield return null;

            UiPlayer.Click(Control(driver, "HOST"));
            yield return null;
            yield return null;
            yield return null;

            Assert.That(driver.Presenter.Model.Role, Is.EqualTo(PrivateLobbyRole.Host),
                "An unkeyed loopback host is the local default and must not be refused; the " +
                "lobby said: '" + Text(driver, PrivateLobbyUnityView.FeedbackName) + "'.");

            // Close the listener this journey opened, through the control a player would use.
            UiPlayer.Click(Control(driver, "LEAVE"));
            yield return null;
            yield return null;
            Assert.That(driver.Presenter.Model.Role, Is.EqualTo(PrivateLobbyRole.None));
        }

        /// <summary>
        /// Every control the lobby offers can actually be pressed at every supported desktop
        /// validation resolution - drawn inside the panel, and reached by a pointer at its centre.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryControl_IsReachableAtEverySupportedDesktopViewport()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out _);
            driver.Presenter.AttachClient(wire.Client(1));
            yield return null;

            foreach (var viewport in SupportedViewports)
            {
                UiPlayer.SetPlayerViewport((uint)viewport.x, (uint)viewport.y, "8.35a lobby");
                yield return null;
                yield return null;
                driver.Presenter.Refresh();
                yield return null;
                yield return null;

                var panel = driver.View.Root.panel;
                Assert.That(panel, Is.Not.Null);
                var surface = panel.visualTree.worldBound;
                var buttons = ShownButtons(driver);
                Assert.That(buttons, Is.Not.Empty,
                    "A reachability sweep over no control is a broken rule rather than a pass.");

                foreach (var button in buttons)
                {
                    var bounds = button.worldBound;
                    Assert.That(float.IsNaN(bounds.width) || float.IsNaN(bounds.height), Is.False,
                        button.text + " has no resolved layout at " + viewport + ".");
                    Assert.That(bounds.width, Is.GreaterThan(0f));
                    Assert.That(surface.Contains(new Vector2(bounds.xMin, bounds.yMin)) &&
                            surface.Contains(new Vector2(bounds.xMax - 0.01f, bounds.yMax - 0.01f)),
                        Is.True,
                        "'" + button.text + "' is drawn outside the panel at " + viewport +
                        ": control " + bounds + " against surface " + surface + ".");
                    var picked = panel.Pick(bounds.center);
                    Assert.That(picked == button || IsUnder(picked, button), Is.True,
                        "'" + button.text + "' cannot be reached by a pointer at " + viewport +
                        "; the pointer lands on '" + (picked == null ? "nothing" : picked.name) +
                        "'.");
                }

                // Every seat row stays legible at the same time, which is what the controls are
                // being pressed about.
                Assert.That(Named(driver, PrivateLobbyUnityView.SeatRowName).Count(),
                    Is.EqualTo(6), "A seat row was lost at " + viewport + ".");
            }

            UiPlayer.SetPlayerViewport(1920, 1080, "8.35a lobby restore");
            yield return null;
        }

        /// <summary>
        /// The targeted non-batch visual inspection the test matrix asks of this milestone: the
        /// lobby photographed at two supported viewports, into <c>tmp/</c>. It no-ops in batch,
        /// where there is no Game View to photograph.
        /// </summary>
        [UnityTest]
        public IEnumerator TheLobbyIsPhotographedForVisualInspection()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out _);
            for (var ordinal = 2; ordinal <= 4; ordinal++)
            {
                wire.Client(ordinal).TrySetReadiness(true);
                wire.Pump();
            }

            driver.Presenter.AttachClient(wire.Client(1));
            yield return null;

            foreach (var viewport in new[] { new Vector2(1920f, 1080f), new Vector2(2560f, 1440f) })
            {
                UiPlayer.SetPlayerViewport((uint)viewport.x, (uint)viewport.y, "8.35a capture");
                yield return null;
                driver.Presenter.Refresh();
                yield return null;
                yield return null;
                yield return UiPlayer.CaptureGameView(
                    "8-35a", "lobby-" + (int)viewport.x + "x" + (int)viewport.y);
            }

            UiPlayer.SetPlayerViewport(1920, 1080, "8.35a capture restore");
            yield return null;
            Assert.That(Named(driver, PrivateLobbyUnityView.SeatRowName).Count(), Is.EqualTo(6));
        }

        // --- Helpers ------------------------------------------------------------------------------

        private LobbyWire SeatEveryone(out IReadOnlyList<string> seats)
        {
            var wire = LobbyWire.Compose(Runtime().Host, Runtime().SessionIdentity);
            seats = wire.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
            for (var index = 0; index < seats.Count; index++)
            {
                var client = wire.Connect(index + 1, seats[index]);
                Assert.That(client.State, Is.EqualTo(PrivateSessionClientState.Seated),
                    "Seat " + seats[index] + " did not seat over the wire.");
            }

            return wire;
        }

        private static bool IsUnder(VisualElement candidate, VisualElement ancestor)
        {
            for (var current = candidate; current != null; current = current.hierarchy.parent)
            {
                if (current == ancestor)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<VisualElement> Named(PrivateLobbyDriver driver, string name) =>
            UiPlayer.Descendants(driver.View.Root).Where(element => element.name == name);

        private static VisualElement Element(PrivateLobbyDriver driver, string name)
        {
            var matches = Named(driver, name).ToArray();
            Assert.That(matches, Has.Length.EqualTo(1),
                "Expected exactly one '" + name + "' on the lobby panel.");
            return matches[0];
        }

        private static TextField Field(PrivateLobbyDriver driver, string name) =>
            (TextField)Element(driver, name);

        private static string Text(PrivateLobbyDriver driver, string name) =>
            ((Label)Element(driver, name)).text;

        private static IEnumerable<string> Texts(PrivateLobbyDriver driver, string name) =>
            Named(driver, name).OfType<Label>().Select(label => label.text);

        private static IReadOnlyList<Button> ShownButtons(PrivateLobbyDriver driver) =>
            UiPlayer.Descendants(driver.View.Root)
                .OfType<Button>()
                .Where(UiPlayer.IsShown)
                .ToArray();

        private static IReadOnlyList<string> ControlLabels(PrivateLobbyDriver driver) =>
            Named(driver, PrivateLobbyUnityView.ControlName)
                .OfType<Button>()
                .Where(UiPlayer.IsShown)
                .Select(button => button.text)
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

        private static Button Control(PrivateLobbyDriver driver, string label)
        {
            var matches = Named(driver, PrivateLobbyUnityView.ControlName)
                .OfType<Button>()
                .Where(button => UiPlayer.IsShown(button) && button.text == label)
                .ToArray();
            Assert.That(matches, Has.Length.EqualTo(1),
                "Expected exactly one shown '" + label + "' control; the lobby draws " +
                string.Join(", ", ControlLabels(driver)) + ".");
            return matches[0];
        }

        private static ConfiguredMatchSceneRuntime Runtime() => Root().Runtime;

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }

        private static PrivateLobbyDriver Driver()
        {
            var driver = Object.FindAnyObjectByType<PrivateLobbyDriver>();
            Assert.That(driver, Is.Not.Null, "The lobby driver was not composed into the scene.");
            Assert.That(driver.View, Is.Not.Null,
                "The lobby driver has not built its panel yet.");
            return driver;
        }

        /// <summary>
        /// Loads the match scene and composes the shipped lobby driver into it on a host object of
        /// its own, which is what a player launched with the lobby flag gets.
        /// </summary>
        private static IEnumerator LoadTheLobby()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            var host = new GameObject("gd-private-lobby");
            host.AddComponent<PrivateLobbyDriver>().Configure(Root());
            yield return null;
            yield return null;
        }
    }
}
