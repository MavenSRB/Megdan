using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
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

        /// <summary>One actual TCP joiner beside the lobby driver's own loopback playing client.</summary>
        private sealed class SocketJoiner : System.IDisposable, IPrivateSessionClientOutbox
        {
            private sealed class SocketNonces : IPrivateSessionNonceSource
            {
                private long value;
                public byte[] NextNonce() => System.BitConverter.GetBytes(++value);
            }

            private readonly System.Net.Sockets.TcpClient socket = new System.Net.Sockets.TcpClient();
            private readonly PrivateSessionFrameAssembler assembler = new PrivateSessionFrameAssembler();
            private readonly byte[] buffer = new byte[65536];
            internal readonly PrivateSessionClient Machine;

            internal SocketJoiner(
                PrivateSessionIdentity identity,
                System.Net.IPAddress address,
                int port,
                string sessionKey)
            {
                socket.Connect(address, port);
                Machine = new PrivateSessionClient(
                    identity, default, sessionKey, this, new SocketNonces());
                Machine.OnConnected();
            }

            internal void Pump()
            {
                while (socket.Available > 0)
                {
                    var read = socket.GetStream().Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                    {
                        Machine.OnDisconnected();
                        return;
                    }

                    Assert.That(assembler.Add(buffer, 0, read), Is.True);
                    while (assembler.TryDequeue(out var frame)) Machine.OnFrame(frame);
                }
            }

            public void Send(byte[] frame) => socket.GetStream().Write(frame, 0, frame.Length);

            public void Close() => socket.Close();

            public void Dispose() => socket.Close();
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
            Assert.That(Texts(driver, "gd-lobby-seat-hero").All(text => text.Length == 0), Is.True,
                "Seating is not picking: no provisional hero name should be shown.");
            Assert.That(Texts(driver, "gd-lobby-seat-occupancy").Count(text => text == "SEATED"),
                Is.EqualTo(seats.Count));
            Assert.That(Texts(driver, "gd-lobby-seat-readiness").Count(text => text == "NOT READY"),
                Is.EqualTo(seats.Count));
            Assert.That(Texts(driver, "gd-lobby-seat-own").Count(text => text == "YOU"),
                Is.EqualTo(1), "A player must be able to find their own row on the panel.");
            // Milestone 8.35z37 added PICK HERO to the seated set, and whether this seat is offered it
            // depends on the Tie Breaker coin p.22's order opens on - which this case does not fix.
            // Everything that is not the pick control stays exact; the pick control's own rule belongs
            // to `ASeatItIsNotTheTurnOf_IsOfferedNoPickControl`.
            Assert.That(
                ControlLabels(driver).Where(label => label != "PICK HERO").ToArray(),
                Is.EqualTo(new[] { "LEAVE", "READY" }));
        }

        /// <summary>
        /// Milestone <c>8.35z54</c> SPLIT WHAT THIS CASE USED TO ASSERT IN ONE STEP, and the split is
        /// the user's own flow spec: "once players are full, everyone is ready... Once they are ready,
        /// everyone picks their heroes, and game goes on." So the sixth READINESS no longer begins the
        /// match - it OPENS THE PICKING - and the match begins on the LAST PICK. Both halves are
        /// asserted here through the real panel, in that order, because a build that started the match
        /// on readiness would compose a line-up nobody chose, which is the shape one rejected design
        /// attempt actually produced and got measured out.
        ///
        /// <para>The lobby standing aside without leaving a pressable control is unchanged, and is
        /// asserted at its new moment.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ReadyingTheSixthSeat_OpensThePicking_AndTheLastPickBeginsTheMatch()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            // Milestone 8.35z54. THE PICKS NOW REACH A COMPOSITION, so this fixture must supply one.
            // A host that admits hero picks and holds no `IDeferredMatchComposition` refuses the
            // transition loudly rather than silently fielding the provisional line-up - which is the
            // debt item "null Composition made loud", and it caught this fixture on the first run
            // after readiness stopped being the thing that started the match.
            wire.Session.Composition = Root();
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
            Assert.That(wire.Session.IsPickPhaseOpen, Is.False,
                "Five ready seats is not six, and the sixth is the one being pressed.");

            UiPlayer.Click(Control(driver, "READY"));
            wire.Pump();
            driver.Presenter.Refresh();
            yield return null;
            yield return null;

            Assert.That(mine.IsReady, Is.True,
                "The pressed control reached the host and came back in the roster.");
            Assert.That(wire.Session.IsPickPhaseOpen, Is.True,
                "THE SIXTH READINESS OPENS THE PICKING.");
            Assert.That(wire.Session.HasMatchStarted, Is.False,
                "...and it does NOT start the match. A match started here would be a match on a " +
                "line-up nobody had chosen yet, because at this instant nobody has picked.");
            Assert.That(UiPlayer.IsShown(driver.View.Root), Is.True,
                "The lobby is still the surface a player is looking at - it has picking to draw.");

            var picks = PickEverySeat(wire, seats);
            Assert.That(picks, Is.EqualTo(seats.Count),
                "One pick per seat, in p.22's printed order.");

            driver.Presenter.Refresh();
            yield return null;
            yield return null;

            Assert.That(wire.Session.HasMatchStarted, Is.True, "THE LAST PICK BEGINS THE MATCH.");
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
            // Milestone 8.35z37 added PICK HERO to the seated set, and whether THIS seat is offered it
            // depends on the Tie Breaker coin: p.22's order opens on the coin's team, so the control
            // comes and goes with a composition this case does not control. Asserted coin-independently
            // rather than pinned to one outcome - everything that is not the pick control is still
            // exact, and `ASeatItIsNotTheTurnOf_IsOfferedNoPickControl` owns the pick control's rule.
            Assert.That(
                ControlLabels(driver).Where(label => label != "PICK HERO").ToArray(),
                Is.EqualTo(new[] { "LEAVE", "READY" }));

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
            // Milestone 8.107: leaving lands on the first screen, which is HOST and JOIN alone.
            Assert.That(ControlLabels(driver), Is.EqualTo(LandingControls));
            Assert.That(UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.EntryName)), Is.False);
            yield return null;
            yield return UiPlayer.CaptureScreen("tmp/8-108-lobby-landing.png");

            yield return OpenJoinMenu(driver);
            yield return UiPlayer.CaptureScreen("tmp/8-108-lobby-join-menu.png");
            foreach (var field in new[]
                     {
                         PrivateLobbyUnityView.AddressFieldName,
                         PrivateLobbyUnityView.PortFieldName,
                         PrivateLobbyUnityView.KeyFieldName
                     })
            {
                Assert.That(UiPlayer.IsShown(Element(driver, field)), Is.True,
                    "The whole invitation - host IP, port and session ID - is typed into the join menu.");
            }

            Assert.That(Field(driver, PrivateLobbyUnityView.AddressFieldName).label, Is.EqualTo("HOST IP"));
            Assert.That(Field(driver, PrivateLobbyUnityView.KeyFieldName).label, Is.EqualTo("SESSION ID"));
            Assert.That(ControlLabels(driver), Is.EqualTo(new[] { PrivateLobbyUnityView.CancelLabel, "JOIN" }));

            Field(driver, PrivateLobbyUnityView.AddressFieldName).value = "81.2.69.160";
            UiPlayer.Click(Control(driver, PrivateLobbyUnityView.CancelLabel));
            yield return null;
            yield return null;
            Assert.That(ControlLabels(driver), Is.EqualTo(LandingControls), "CANCEL goes back to the first screen.");

            yield return OpenHostMenu(driver);
            yield return UiPlayer.CaptureScreen("tmp/8-108-lobby-host-menu.png");
            Assert.That(ControlLabels(driver), Is.EqualTo(new[]
            {
                "2V2", "3V3", PrivateLobbyUnityView.CancelLabel, "HOST"
            }));
            Assert.That(UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.WaveCountFieldName)), Is.True);
            Assert.That(UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.LifeCountFieldName)), Is.True);
            Assert.That(UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.HostPortFieldName)), Is.True);
            Assert.That(UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.HostKeyFieldName)), Is.True);
            Assert.That(UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.AddressFieldName)), Is.False,
                "The host never types its own address.");
            Assert.That(Field(driver, PrivateLobbyUnityView.HostKeyFieldName).value, Is.Not.Empty,
                "A fresh session ID is waiting in the host menu.");
            UiPlayer.Click(Control(driver, PrivateLobbyUnityView.CancelLabel));
            yield return null;
            yield return null;

            yield return OpenJoinMenu(driver);
            Assert.That(Field(driver, PrivateLobbyUnityView.AddressFieldName).value, Is.EqualTo("81.2.69.160"),
                "CANCEL kept what was typed.");

            Assert.That(Named(driver, PrivateLobbyUnityView.SeatPickName), Is.Empty,
                "Milestone 8.35z54 REMOVED the pre-join seat picker, and this is the assertion that " +
                "keeps it removed. A seat is taken from inside the session against the host's own " +
                "roster; before a host has answered there is no roster to take a seat from, and the " +
                "old surface let a player choose one anyway and then discover it was gone.");
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

            yield return OpenJoinMenu(driver);
            Field(driver, PrivateLobbyUnityView.PortFieldName).value =
                port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            // Milestone 8.35z54: a join names NO seat. There is no pre-join picker to press any
            // more, and the entry form says as much rather than implying a choice was made.
            yield return null;
            Assert.That(Text(driver, PrivateLobbyUnityView.PickedSeatName),
                Is.EqualTo("SEAT: NONE PICKED"),
                "You join the server first and take a chair from inside it, so at the moment of " +
                "dialling there is genuinely no seat to name.");

            // 8.35t corrective. BeginJoining's own failed Dial() now logs the host-console-side
            // diagnostic PrivateOnlineMarkers.Fail writes - the exact fix for this test's own
            // scenario, a join to nothing listening, which used to leave no seam for anyone but the
            // player-facing feedback line to observe. Expected here as the log-side half of that
            // same fix; the assertions below are unchanged and still read the player-facing text.
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(
                "^GUARDS_ONLINE: FAIL lobby-join seat=.+ could not reach the host at 127\\.0\\.0\\.1:\\d+$"));
            UiPlayer.Click(Control(driver, "JOIN"));
            yield return null;

            var said = Text(driver, PrivateLobbyUnityView.FeedbackName);
            Assert.That(said, Does.StartWith("The host could not be reached"),
                "A failed join says so; the entry form is still standing behind it.");
            Assert.That(said.Contains("127.0.0.1"), Is.False,
                "Milestone 8.31's rule: a player-facing diagnostic never names an address.");
            Assert.That(ControlLabels(driver), Is.EqualTo(new[] { PrivateLobbyUnityView.CancelLabel, "JOIN" }),
                "A failed join stays in the join menu, so the player can correct and retry.");
        }

        [UnityTest]
        public IEnumerator RepeatedSocketJoinsAndLeaves_RestoreEverySetupControl()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                Field(driver, PrivateLobbyUnityView.AddressFieldName).value = "127.0.0.1";
                Field(driver, PrivateLobbyUnityView.PortFieldName).value =
                    ((System.Net.IPEndPoint)listener.LocalEndpoint).Port.ToString();
                Field(driver, PrivateLobbyUnityView.KeyFieldName).value = string.Empty;
                for (var attempt = 0; attempt < 2; attempt++)
                {
                    yield return null;
                    yield return OpenJoinMenu(driver);
                    UiPlayer.Click(Control(driver, "JOIN"));
                    yield return null;
                    yield return null;
                    yield return null;
                    // The listener accepts TCP but sends no Welcome: a player may leave even
                    // when joining has not yet completed. Seated Leave is covered separately.
                    UiPlayer.Click(Control(driver, "LEAVE"));
                    yield return null;
                    Assert.That(ControlLabels(driver), Is.EqualTo(LandingControls));
                    Assert.That(driver.Presenter.Model.Role, Is.EqualTo(PrivateLobbyRole.None));
                }
            }
            finally { listener.Stop(); }
        }

        /// <summary>
        /// Milestone 8.100's reported player path. ADDRESS used to mean both "where JOIN dials"
        /// and "which local interface HOST binds", forcing a host to know and type this machine's
        /// numeric address. It is now join-only input: even deliberately unusable join text cannot
        /// prevent an unkeyed HOST from opening its local-only listener and admitting its own
        /// ordinary loopback player client.
        /// </summary>
        [UnityTest]
        public IEnumerator HostIgnoresTheJoinAddress_AndUnkeyedHostingStaysLocal()
        {
            yield return LoadTheLobby();
            var driver = Driver();

            var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            probe.Start();
            var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            var joinAddress = Field(driver, PrivateLobbyUnityView.AddressFieldName);
            Assert.That(joinAddress.label, Is.EqualTo("HOST IP"));
            joinAddress.value = "this-is-not-a-numeric-address";
            yield return OpenHostMenu(driver);
            Field(driver, PrivateLobbyUnityView.HostPortFieldName).value =
                port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Field(driver, PrivateLobbyUnityView.HostKeyFieldName).value = string.Empty;
            yield return null;

            UiPlayer.Click(Control(driver, "HOST"));
            var deadline = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < deadline &&
                !driver.Presenter.Model.Offers(PrivateLobbyControl.ClaimSeat))
                yield return null;

            Assert.That(driver.Presenter.Model.Role, Is.EqualTo(PrivateLobbyRole.Host),
                "HOST must not parse the field that now belongs only to JOIN; the lobby said: '" +
                Text(driver, PrivateLobbyUnityView.FeedbackName) + "'.");
            Assert.That(driver.Presenter.Model.Offers(PrivateLobbyControl.ClaimSeat), Is.True,
                "The hosting player reaches its ordinary owner-scoped client through loopback.");

            yield return null;
            yield return null;
            UiPlayer.Click(Control(driver, "LEAVE"));
            yield return null;
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

            yield return OpenHostMenu(driver);
            // Milestone 8.107: the port arrives the way a chat window hands it over - padded with
            // an ordinary and a non-breaking space - and the host still binds it.
            Field(driver, PrivateLobbyUnityView.HostPortFieldName).value =
                " " + port.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ";
            Field(driver, PrivateLobbyUnityView.HostKeyFieldName).value = string.Empty;
            yield return null;

            UiPlayer.Click(Control(driver, "HOST"));
            var hostingDeadline = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < hostingDeadline &&
                !driver.Presenter.Model.Offers(PrivateLobbyControl.ClaimSeat))
                yield return null;

            Assert.That(driver.Presenter.Model.Role, Is.EqualTo(PrivateLobbyRole.Host),
                "An unkeyed loopback host is the local default and must not be refused; the " +
                "lobby said: '" + Text(driver, PrivateLobbyUnityView.FeedbackName) + "'.");
            Assert.That(driver.Presenter.Model.Offers(PrivateLobbyControl.ClaimSeat), Is.True,
                "HOST now admits this process's ordinary protocol client and shows its seat claim.");
            Assert.That(ControlLabels(driver), Does.Contain("TAKE THIS SEAT"));
            Assert.That(ControlLabels(driver), Does.Not.Contain("FILL WITH BOTS"),
                "The host must take its own seat before bots may fill the rest.");
            Assert.That(Text(driver, PrivateLobbyUnityView.InvitationSessionName), Is.EqualTo("SESSION ID: NONE"));
            Assert.That(Text(driver, PrivateLobbyUnityView.InvitationAddressName),
                Does.StartWith("THIS COMPUTER ONLY (127.0.0.1)   PORT " +
                    port.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                "The host's screen says who can reach it; a cleared session ID means this computer only.");

            var capture = typeof(PrivateLobbyDriver)
                .GetField("evidence", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .GetValue(driver);
            Assert.That(capture, Is.Not.Null, "The ordinary HOST button must open attributable evidence.");
            var capturePath = (string)capture.GetType().GetProperty("Path").GetValue(capture);
            var runId = (string)capture.GetType().GetProperty("RunId").GetValue(capture);
            string begun;
            using (var reader = new System.IO.StreamReader(new System.IO.FileStream(capturePath,
                System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite)))
                begun = reader.ReadToEnd();
            Assert.That(begun, Does.Contain("GD_EVIDENCE_BEGIN run=" + runId));
            Assert.That(begun, Does.Contain("role=host seat=-"));
            Assert.That(begun, Does.Not.Contain("GD_EVIDENCE_END"));

            // Close the listener this journey opened, through the control a player would use.
            yield return null;
            UiPlayer.Click(Control(driver, "LEAVE"));
            yield return null;
            yield return null;
            Assert.That(driver.Presenter.Model.Role, Is.EqualTo(PrivateLobbyRole.None));
            Assert.That(System.IO.File.ReadAllText(capturePath), Does.Contain("GD_EVIDENCE_ABORT"),
                "Leaving an unfinished lobby must not manufacture a completed-match capture.");
            Assert.That(ControlLabels(driver), Is.EqualTo(LandingControls));
            yield return OpenHostMenu(driver);
            Assert.That(Field(driver, PrivateLobbyUnityView.HostPortFieldName).value.Trim(' ', ' '),
                Is.EqualTo(port.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                "The host menu kept its port across the session.");
            Assert.That(Field(driver, PrivateLobbyUnityView.HostKeyFieldName).value, Is.Not.Empty,
                "Leaving a hosted session puts a fresh session ID in the host menu.");
            Field(driver, PrivateLobbyUnityView.HostKeyFieldName).value = string.Empty;
            UiPlayer.Click(Control(driver, "HOST"));
            yield return null;
            Assert.That(driver.Presenter.Model.Role, Is.EqualTo(PrivateLobbyRole.Host));
            yield return null;
            yield return null;
            UiPlayer.Click(Control(driver, "LEAVE"));
            yield return null;
            Assert.That(ControlLabels(driver), Is.EqualTo(LandingControls));
        }

        /// <summary>
        /// Milestone 8.107. A host that never takes a seat runs the match from its lobby screen.
        /// Four socket players fill a 2v2, and once the match starts the host's screen becomes its
        /// console. DIRECTOR UNDO there rewinds the authority and reaches every seat.
        /// </summary>
        [UnityTest]
        public IEnumerator ANonPlayingHost_UndoesFromItsConsole_AndEverySeatIsUpdated()
        {
            var remotes = new List<SocketJoiner>();
            try
            {
                yield return LoadTheLobby();
                var driver = Driver();
                yield return OpenHostMenu(driver);
                UiPlayer.Click(Control(driver, "2V2"));
                yield return null;
                yield return null;

                var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
                probe.Start();
                var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
                probe.Stop();
                Field(driver, PrivateLobbyUnityView.HostPortFieldName).value = port.ToString();
                Field(driver, PrivateLobbyUnityView.HostKeyFieldName).value = string.Empty;
                yield return null;
                UiPlayer.Click(Control(driver, "HOST"));
                var hostDeadline = Time.realtimeSinceStartup + 5f;
                while (Time.realtimeSinceStartup < hostDeadline &&
                    !driver.Presenter.Model.Offers(PrivateLobbyControl.ClaimSeat))
                    yield return null;
                Assert.That(driver.Presenter.Model.Role, Is.EqualTo(PrivateLobbyRole.Host));

                var seatCount = driver.Presenter.Model.Seats.Count;
                Assert.That(seatCount, Is.EqualTo(4));
                for (var index = 0; index < seatCount; index++)
                {
                    remotes.Add(new SocketJoiner(
                        Runtime().SessionIdentity, System.Net.IPAddress.Loopback, port, string.Empty));
                }

                var admitDeadline = Time.realtimeSinceStartup + 10f;
                while (Time.realtimeSinceStartup < admitDeadline &&
                    remotes.Any(remote => remote.Machine.State != PrivateSessionClientState.Admitted))
                {
                    foreach (var remote in remotes) remote.Pump();
                    yield return null;
                }

                var claimed = new HashSet<SeatId>();
                var named = 0;
                foreach (var remote in remotes)
                {
                    remote.Pump();
                    // Milestone 8.108: every player names themselves, and one pads it with spaces.
                    remote.Machine.PlayerName = named == 0 ? "  Player 1 " : "Player " + (named + 1);
                    named++;
                    var seat = remote.Machine.Roster.First(row =>
                        row.Occupancy == SeatOccupancyState.Open && !claimed.Contains(row.Seat)).Seat;
                    claimed.Add(seat);
                    Assert.That(remote.Machine.TryClaimSeat(seat), Is.True);
                }

                var seatDeadline = Time.realtimeSinceStartup + 10f;
                while (Time.realtimeSinceStartup < seatDeadline &&
                    remotes.Any(remote => remote.Machine.State != PrivateSessionClientState.Seated))
                {
                    foreach (var remote in remotes) remote.Pump();
                    yield return null;
                }

                foreach (var remote in remotes)
                {
                    Assert.That(remote.Machine.State, Is.EqualTo(PrivateSessionClientState.Seated));
                    Assert.That(remote.Machine.TrySetReadiness(true), Is.True);
                }

                var startDeadline = Time.realtimeSinceStartup + 20f;
                while (Time.realtimeSinceStartup < startDeadline && !driver.Presenter.Model.IsHostConsole)
                {
                    foreach (var remote in remotes)
                    {
                        remote.Pump();
                        var lobby = PrivateLobbyModel.ForClient(remote.Machine);
                        if (lobby.Selection.IsOwnPick)
                        {
                            Assert.That(remote.Machine.TryChooseHero(
                                lobby.Selection.Heroes.First(hero => hero.IsAvailable).HeroIdValue), Is.True);
                        }
                    }

                    yield return null;
                }

                Assert.That(driver.Presenter.Model.IsHostConsole, Is.True,
                    "The match started without the host in a seat, so its screen is the host console.");
                Assert.That(driver.Presenter.Model.Seats.Select(row => row.SeatLabel),
                    Is.EquivalentTo(new[] { "Player 1", "Player 2", "Player 3", "Player 4" }),
                    "8.108: every seat row shows its player's name, cleaned, in place of the seat id.");
                Assert.That(Texts(driver, "gd-lobby-seat-name"), Is.SupersetOf(new[] { "Player 1", "Player 4" }));
                yield return null;
                yield return null;
                Assert.That(ControlLabels(driver), Is.EqualTo(new[] { "DIRECTOR RESTART", "DIRECTOR UNDO", "LEAVE" }));
                Assert.That(UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.InvitationSessionName)), Is.True,
                    "The host console still shows what guests need to rejoin.");

                var random = new GuardsDigital.Application.Bots.DeterministicBotRandom(107);
                var matches = remotes.Select(remote => new NetworkMatchClient(
                    remote.Machine, Runtime().Projection, Runtime().ContentCatalogs)).ToArray();
                var playDeadline = Time.realtimeSinceStartup + 20f;
                while (Time.realtimeSinceStartup < playDeadline &&
                    Runtime().Host.Projection.AcceptedCommands.Count < 2)
                {
                    for (var index = 0; index < remotes.Count; index++)
                    {
                        remotes[index].Pump();
                        PlayNetworkHuman(remotes[index].Machine, matches[index], random);
                    }

                    yield return null;
                }

                var acceptedBefore = Runtime().Host.Projection.AcceptedCommands.Count;
                Assert.That(acceptedBefore, Is.GreaterThanOrEqualTo(2), "The players have made moves to take back.");

                UiPlayer.Click(Control(driver, "DIRECTOR UNDO"));
                yield return null;
                Assert.That(Runtime().Host.Projection.AcceptedCommands.Count, Is.EqualTo(acceptedBefore - 1),
                    "DIRECTOR UNDO takes back exactly one accepted command on the authority.");
                Assert.That(Text(driver, PrivateLobbyUnityView.FeedbackName), Does.StartWith("Director undo done"));

                var rewoundVersion = Runtime().Host.Projection.Version;
                var syncDeadline = Time.realtimeSinceStartup + 5f;
                while (Time.realtimeSinceStartup < syncDeadline &&
                    remotes.Any(remote => remote.Machine.LatestProjection.Version != rewoundVersion))
                {
                    foreach (var remote in remotes) remote.Pump();
                    yield return null;
                }

                foreach (var remote in remotes)
                {
                    Assert.That(remote.Machine.LatestProjection.Version, Is.EqualTo(rewoundVersion),
                        "Every seat was republished the rewound match.");
                }

                UiPlayer.Click(Control(driver, "LEAVE"));
                yield return null;
                Assert.That(ControlLabels(driver), Is.EqualTo(LandingControls));
            }
            finally
            {
                foreach (var remote in remotes) remote.Dispose();
            }
        }

        /// <summary>
        /// Milestone 8.36g1's final topology: HOST is an ordinary owner-scoped player connection,
        /// a second process-shaped TCP client joins it, and only the still-open chairs become bots.
        /// The two human clients ready, pick and play the same authoritative match to its outcome.
        /// </summary>
        [UnityTest]
        public IEnumerator PlayingHostAndRemoteTcpClient_CompleteOneLocalMatch()
        {
            var previousPacing = System.Environment.GetEnvironmentVariable("GUARDS_TESTDRIVER_THINK_SECONDS");
            System.Environment.SetEnvironmentVariable("GUARDS_TESTDRIVER_THINK_SECONDS", "0");
            SocketJoiner remote = null;
            try
            {
                yield return LoadTheLobby();
                var driver = Driver();
                yield return OpenHostMenu(driver);
                UiPlayer.Click(Control(driver, "2V2"));
                yield return null;
                yield return null;

                var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
                probe.Start();
                var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
                probe.Stop();
                const string Key = "g1-playing-host";
                Field(driver, PrivateLobbyUnityView.HostPortFieldName).value = port.ToString();
                Field(driver, PrivateLobbyUnityView.HostKeyFieldName).value = Key;
                yield return null;
                UiPlayer.Click(Control(driver, "HOST"));

                var hostAdmissionDeadline = Time.realtimeSinceStartup + 5f;
                while (Time.realtimeSinceStartup < hostAdmissionDeadline &&
                    !driver.Presenter.Model.Offers(PrivateLobbyControl.ClaimSeat))
                    yield return null;
                Assert.That(driver.Presenter.Model.Offers(PrivateLobbyControl.ClaimSeat), Is.True);
                yield return null;
                yield return null;
                var hostSeatButton = Named(driver, PrivateLobbyUnityView.SeatPickName)
                    .OfType<Button>().First(UiPlayer.IsShown);
                UiPlayer.Click(hostSeatButton);
                var hostSeatDeadline = Time.realtimeSinceStartup + 5f;
                while (Time.realtimeSinceStartup < hostSeatDeadline &&
                    !driver.Presenter.Model.Seats.Any(row => row.IsOwnSeat))
                    yield return null;
                var hostSeat = driver.Presenter.Model.Seats.Single(row => row.IsOwnSeat).Seat;
                var hostTeam = driver.Presenter.Model.Seats.Single(row => row.IsOwnSeat).TeamLabel;

                remote = new SocketJoiner(
                    Runtime().SessionIdentity, System.Net.IPAddress.Loopback, port, Key);
                var remoteAdmissionDeadline = Time.realtimeSinceStartup + 5f;
                while (Time.realtimeSinceStartup < remoteAdmissionDeadline &&
                    remote.Machine.State != PrivateSessionClientState.Admitted)
                {
                    remote.Pump();
                    yield return null;
                }
                Assert.That(remote.Machine.State, Is.EqualTo(PrivateSessionClientState.Admitted));
                var remoteSeat = remote.Machine.Roster.First(row =>
                    row.Occupancy == SeatOccupancyState.Open &&
                    !row.Seat.Equals(hostSeat) &&
                    PrivateLobbyLabels.Team(row.TeamSide) != hostTeam).Seat;
                Assert.That(remote.Machine.TryClaimSeat(remoteSeat), Is.True);
                var remoteSeatDeadline = Time.realtimeSinceStartup + 5f;
                while (Time.realtimeSinceStartup < remoteSeatDeadline &&
                    remote.Machine.State != PrivateSessionClientState.Seated)
                {
                    remote.Pump();
                    yield return null;
                }
                Assert.That(remote.Machine.State, Is.EqualTo(PrivateSessionClientState.Seated));

                // A deliberate remote departure releases its chair, and a fresh client can join
                // and claim it again before the match. This is distinct from reconnect capability:
                // Goodbye explicitly relinquishes that capability and the seat reservation.
                remote.Machine.SendGoodbye();
                for (var frame = 0; frame < 20; frame++)
                {
                    remote.Pump();
                    yield return null;
                    if (driver.Presenter.Model.Seats.Any(row =>
                        row.Seat.Equals(remoteSeat) && row.OccupancyLabel == "OPEN")) break;
                }
                remote.Dispose();
                remote = new SocketJoiner(
                    Runtime().SessionIdentity, System.Net.IPAddress.Loopback, port, Key);
                remoteAdmissionDeadline = Time.realtimeSinceStartup + 5f;
                while (Time.realtimeSinceStartup < remoteAdmissionDeadline &&
                    remote.Machine.State != PrivateSessionClientState.Admitted)
                {
                    remote.Pump();
                    yield return null;
                }
                Assert.That(remote.Machine.State, Is.EqualTo(PrivateSessionClientState.Admitted));
                Assert.That(remote.Machine.TryClaimSeat(remoteSeat), Is.True);
                remoteSeatDeadline = Time.realtimeSinceStartup + 5f;
                while (Time.realtimeSinceStartup < remoteSeatDeadline &&
                    remote.Machine.State != PrivateSessionClientState.Seated)
                {
                    remote.Pump();
                    yield return null;
                }
                Assert.That(remote.Machine.State, Is.EqualTo(PrivateSessionClientState.Seated));

                Assert.That(driver.Presenter.Press(
                    PrivateLobbyControl.FillWithTestDrivers, driver.View.CurrentEntry()), Is.True);
                yield return null;
                Assert.That(driver.Presenter.Model.Seats.Count(row => row.IsTestDriverSeat), Is.EqualTo(2),
                    "The two human-owned seats must not be replaced by host-owned bots.");
                Assert.That(driver.Presenter.Press(
                    PrivateLobbyControl.Ready, driver.View.CurrentEntry()), Is.True);
                Assert.That(remote.Machine.TrySetReadiness(true), Is.True);

                var pickDeadline = Time.realtimeSinceStartup + 20f;
                while (Time.realtimeSinceStartup < pickDeadline &&
                    !driver.Presenter.Model.HasMatchStarted)
                {
                    remote.Pump();
                    if (driver.Presenter.Model.Selection.IsOwnPick)
                    {
                        driver.Presenter.SelectedHeroIdValue = driver.Presenter.Model.Selection.Heroes
                            .First(hero => hero.IsAvailable).HeroIdValue;
                        Assert.That(driver.Presenter.Press(
                            PrivateLobbyControl.ChooseHero, driver.View.CurrentEntry()), Is.True);
                    }

                    var remoteLobby = PrivateLobbyModel.ForClient(remote.Machine);
                    if (remoteLobby.Selection.IsOwnPick)
                    {
                        Assert.That(remote.Machine.TryChooseHero(remoteLobby.Selection.Heroes
                            .First(hero => hero.IsAvailable).HeroIdValue), Is.True);
                    }
                    yield return null;
                }
                remote.Pump();
                Assert.That(driver.Presenter.Model.HasMatchStarted, Is.True);
                Assert.That(remote.Machine.HasMatchStarted, Is.True);
                Assert.That(remote.Machine.LatestProjection, Is.Not.Null);

                var remoteMatch = new NetworkMatchClient(
                    remote.Machine, Runtime().Projection, Runtime().ContentCatalogs);
                var hostRandom = new GuardsDigital.Application.Bots.DeterministicBotRandom(811);
                var remoteRandom = new GuardsDigital.Application.Bots.DeterministicBotRandom(977);
                var playDeadline = Time.realtimeSinceStartup + 90f;
                while (Time.realtimeSinceStartup < playDeadline && !Runtime().ReadModel.Outcome.HasEnded)
                {
                    remote.Pump();
                    PlayProjectedHuman(Runtime(), hostRandom);
                    PlayNetworkHuman(remote.Machine, remoteMatch, remoteRandom);
                    yield return null;
                }
                remote.Pump();
                var standing = Runtime().ReadModel;
                var standingAgreement = standing.TeamAgreement;
                Assert.That(standing.Outcome.HasEnded, Is.True,
                    "The actual playing-host topology must reach an authoritative local outcome. " +
                    "Stopped at round " + standing.RoundIndex + ", phase " + standing.Phase + ", version " +
                    standing.Version + ", decision " + (standing.DecisionOwner.IsDefined
                        ? standing.DecisionOwner.FamilyId.Value
                        : "none") +
                    (standingAgreement == null
                        ? ""
                        : ", agreement " + standingAgreement.Kind + " picks [" + string.Join("; ",
                            standingAgreement.Participants.Select(participant => participant.HeroId.Value +
                                ":" + string.Join(",", participant.Picks.Select(unit => unit.ToString())))) +
                            "] offered " + standingAgreement.AuthorizedCommands.Count) + ".");
                Assert.That(remote.Machine.LatestProjection.Version, Is.EqualTo(Runtime().Host.Version),
                    "The remote owner projection must finish on the authoritative host version.");
            }
            finally
            {
                remote?.Dispose();
                System.Environment.SetEnvironmentVariable("GUARDS_TESTDRIVER_THINK_SECONDS", previousPacing);
            }
        }

        [UnityTest]
        public IEnumerator FillButton_ConnectsInProcessBotsAndLeaveReleasesTheHost()
        {
            var previousPacing = System.Environment.GetEnvironmentVariable("GUARDS_TESTDRIVER_THINK_SECONDS");
            System.Environment.SetEnvironmentVariable("GUARDS_TESTDRIVER_THINK_SECONDS", "0");
            try
            {
                foreach (var format in new[] { "2V2", "3V3" })
                {
                    yield return LoadTheLobby();
                    var driver = Driver();
                    yield return OpenHostMenu(driver);
                    UiPlayer.Click(Control(driver, format));
                    yield return null;
                    yield return null;
                    var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
                    probe.Start();
                    var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
                    probe.Stop();
                    Field(driver, PrivateLobbyUnityView.HostPortFieldName).value = port.ToString();
                    // Before 8.107 the host menu's session ID started empty; this journey hosts the
                    // unkeyed local session it always has.
                    Field(driver, PrivateLobbyUnityView.HostKeyFieldName).value = string.Empty;
                    yield return null;
                    UiPlayer.Click(Control(driver, "HOST"));
                    var admittedDeadline = Time.realtimeSinceStartup + 5f;
                    while (Time.realtimeSinceStartup < admittedDeadline &&
                        !Named(driver, PrivateLobbyUnityView.SeatPickName).OfType<Button>()
                            .Any(UiPlayer.IsShown))
                        yield return null;
                    yield return null;
                    var hostSeat = Named(driver, PrivateLobbyUnityView.SeatPickName)
                        .OfType<Button>().First(UiPlayer.IsShown);
                    UiPlayer.Click(hostSeat);
                    var seatedDeadline = Time.realtimeSinceStartup + 5f;
                    while (Time.realtimeSinceStartup < seatedDeadline &&
                        !ControlLabels(driver).Contains("FILL WITH BOTS"))
                        yield return null;
                    yield return null;
                    UiPlayer.Click(Control(driver, "FILL WITH BOTS"));
                    // This is the regression for the shipped button, not a rehearsal-only spawn path.
                    yield return null;
                    Assert.That(driver.Presenter.Model.Seats.Count(row => row.IsTestDriverSeat && row.OccupancyLabel == "SEATED"),
                        Is.EqualTo(format == "2V2" ? 3 : 5),
                        "The host's own seat stays human; bots fill every other chair.");
                    Assert.That(
                        driver.Presenter.Press(PrivateLobbyControl.Ready, driver.View.CurrentEntry()),
                        Is.True);
                    var matchStartDeadline = Time.realtimeSinceStartup + 20f;
                    while (Time.realtimeSinceStartup < matchStartDeadline &&
                        !driver.Presenter.Model.HasMatchStarted)
                    {
                        if (driver.Presenter.Model.Selection.IsOwnPick)
                        {
                            driver.Presenter.SelectedHeroIdValue = driver.Presenter.Model.Selection.Heroes
                                .First(option => option.IsAvailable).HeroIdValue;
                            Assert.That(driver.Presenter.Press(
                                PrivateLobbyControl.ChooseHero, driver.View.CurrentEntry()), Is.True);
                        }
                        yield return null;
                    }
                    Assert.That(driver.Presenter.Model.HasMatchStarted, Is.True,
                        "The hosting player and every bot must finish readiness and picks.");
                    var deadline = Time.realtimeSinceStartup + 30f;
                    var hostRandom = new GuardsDigital.Application.Bots.DeterministicBotRandom(17);
                    while (Time.realtimeSinceStartup < deadline && Runtime().Host.Version < 20)
                    {
                        var agreement = Runtime().ReadModel.TeamAgreement;
                        if (agreement != null && agreement.AuthorizedCommands.Count > 0)
                        {
                            var statement = StatementToMake(agreement);
                            if (statement != null)
                                Runtime().SubmitProjectedAgreement(statement);
                            yield return null;
                            continue;
                        }

                        var commands = Runtime().EnumerateProjectedCommands();
                        if (commands.Count > 0)
                        {
                            var selected = GuardsDigital.Application.Bots.BotPolicy.SelectPrivateSessionTactics(
                                commands,
                                Runtime().ReadModel,
                                Runtime().Seat.HeroId,
                                hostRandom);
                            Runtime().SubmitProjectedCommand(selected);
                        }
                        yield return null;
                    }
                    Assert.That(driver.Presenter.Model.HasMatchStarted, Is.True);
                    Assert.That(Runtime().Host.Version,
                        Is.GreaterThanOrEqualTo(20), "The actual button must reach accepted gameplay, not just launch something.");
                    if (format == "2V2")
                    {
                        Assert.That(driver.Presenter.Press(
                            PrivateLobbyControl.Leave, driver.View.CurrentEntry()), Is.True);
                        yield return null;
                        yield return null;
                        Assert.That(driver.Presenter.Model.Seats.Any(row => row.IsTestDriverSeat), Is.False);
                        Assert.That(driver.Presenter.Model.Role, Is.EqualTo(PrivateLobbyRole.None));
                    }
                    else
                    {
                        Object.Destroy(driver.gameObject);
                        yield return null;
                        yield return null;
                    }
                    var stoppedVersion = Runtime().Host.Version;
                    yield return null;
                    Assert.That(Runtime().Host.Version, Is.EqualTo(stoppedVersion));
                    var rebound = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
                    rebound.Start(); // Leaving releases the actual port, not the probe's previous ephemeral bind.
                    rebound.Stop();
                }
            }
            finally { System.Environment.SetEnvironmentVariable("GUARDS_TESTDRIVER_THINK_SECONDS", previousPacing); }
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

        // --- Milestone 8.35z37: rulebook p.22's selection, and the seam behind it ----------

        /// <summary>
        /// The selection screen as a player meets it: whose pick it is in words, every registered
        /// hero on the panel, and a taken hero greyed with the one-copy rule's reason ON the option.
        /// </summary>
        /// <remarks>
        /// INTEGRATION-OWED: written and compile-checked only. No Unity run has executed this - the
        /// dealing worker held the launcher for this milestone's whole session - so what it asserts
        /// is a claim about the shipped surface rather than an observation of it.
        /// </remarks>
        [UnityTest]
        public IEnumerator TheSelectionScreen_SaysWhosePickItIsAndGreysTakenHeroesWithTheReason()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            // Milestone 8.35z54: readiness is what opens the picking, so it comes first now.
            ReadyEverySeat(wire, seats);

            // The coin's team picks first, so the pick opens on that team's first seat rather than
            // on the roster's first row - which is p.22's alternation, not lobby order.
            var picking = wire.Session.Selection.PickingSeat.Value;
            var pickingOrdinal = seats.ToList().IndexOf(picking) + 1;
            driver.Presenter.AttachClient(wire.Client(pickingOrdinal));
            yield return null;

            Assert.That(Text(driver, PrivateLobbyUnityView.SelectionStatusName),
                Is.EqualTo("YOUR PICK"),
                "The seat the printed order is on is told so, in words.");
            var options = HeroOptions(driver);
            Assert.That(options.Count, Is.EqualTo(RegisteredHeroRoster.ForThisBuild.Count),
                "Every hero this build can deal is offered, counted from the registration itself.");
            Assert.That(options.All(button => button.enabledSelf), Is.True,
                "Nothing is taken yet, so nothing is greyed.");
            Assert.That(ControlLabels(driver), Contains.Item("PICK HERO"));

            var selectedOption = options[0];
            var idleEdge = selectedOption.resolvedStyle.borderTopColor;
            UiPlayer.Click(selectedOption);
            yield return null;
            Assert.That(selectedOption.resolvedStyle.borderTopColor, Is.Not.EqualTo(idleEdge),
                "Clicking a candidate must immediately show which hero PICK HERO will confirm.");
            driver.Presenter.Refresh();
            yield return null;
            // Milestone 8.76: the candidate is shown by its gold ring and class alone, never a suffix.
            Assert.That(HeroOptions(driver)[0].ClassListContains("gd-hero-selected"), Is.True,
                "An ordinary redraw preserves the candidate.");
            Assert.That(HeroOptions(driver)[0].text, Is.EqualTo(((PrivateLobbyHeroOption)HeroOptions(driver)[0].userData).DisplayName),
                "8.76: the label is the hero's name alone; the ring says it is selected.");
            Assert.That(driver.View.Root.resolvedStyle.backgroundColor.a, Is.EqualTo(1f),
                "No part of the provisional map should show through the pre-match screen.");
            Assert.That(UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.WordmarkName)), Is.True);
            foreach (var option in HeroOptions(driver))
            {
                UiPlayer.PointAt(option.parent);
                yield return null;
                var preview = (Image)Element(driver, PrivateLobbyUnityView.HeroOverviewName);
                var hero = (PrivateLobbyHeroOption)option.userData;
                Assert.That(preview.image, Is.SameAs(Resources.Load<Texture2D>("Private/HeroSelection/" + hero.HeroIdValue)));
                Assert.That(preview.image, Is.Not.Null, "Prepared overview missing for " + hero.HeroIdValue);
                Assert.That(preview.image.width, Is.EqualTo(372));
                Assert.That(preview.image.height, Is.EqualTo(260));
                Assert.That(UiPlayer.IsShown(preview), Is.True);
                Assert.That(driver.View.Root.worldBound.Contains(preview.worldBound.min), Is.True);
                Assert.That(driver.View.Root.worldBound.Contains(preview.worldBound.max), Is.True);
                UiPlayer.PointAway(option.parent);
            }

            // Somebody takes a hero; every other seat must now see it greyed AND be told why.
            var taken = RegisteredHeroRoster.ForThisBuild[0];
            Assert.That(wire.Client(pickingOrdinal).TryChooseHero(taken.HeroIdValue), Is.True);
            wire.Pump();
            driver.Presenter.Refresh();
            yield return null;

            var greyed = HeroOptions(driver)
                .Single(button => button.text == taken.DisplayName);
            Assert.That(greyed.enabledSelf, Is.False);
            // Milestone 8.76: the greyed chrome is the whole statement - the label is the name alone
            // and no reason rides the button anywhere ("I dont need a reason as a tooltip at all, it
            // is obvious"); hovering the greyed hero still opens the pick card ("Mouse over is still
            // a pick card").
            Assert.That(greyed.tooltip, Is.Null.Or.Empty, "8.76: no reason on the button.");
            UiPlayer.PointAt(greyed.parent);
            yield return null;
            var greyedPreview = (Image)Element(driver, PrivateLobbyUnityView.HeroOverviewName);
            Assert.That(UiPlayer.IsShown(greyedPreview), Is.True, "8.76: a greyed hero still shows its pick card on hover.");
            Assert.That(greyedPreview.image, Is.SameAs(Resources.Load<Texture2D>("Private/HeroSelection/" + taken.HeroIdValue)));
            // 8.76, "make sure pick card is in the front": the card is the last child of the lobby
            // view, and the view itself is the last child of whatever it is mounted on while the
            // card is open - so a later sibling on the host document cannot cover it.
            var lobbyRoot = driver.View.Root;
            Assert.That(ReferenceEquals(lobbyRoot.ElementAt(lobbyRoot.childCount - 1), greyedPreview), Is.True,
                "the pick card is the lobby view's topmost child");
            Assert.That(lobbyRoot.parent, Is.Not.Null);
            Assert.That(ReferenceEquals(lobbyRoot.parent.ElementAt(lobbyRoot.parent.childCount - 1), lobbyRoot), Is.True,
                "the lobby view is raised above every sibling while a pick card is open");
            UiPlayer.PointAway(greyed.parent);
            yield return null;
            Assert.That(driver.View.HighlightedHero, Is.Empty, "An accepted pick clears the local candidate.");
            Assert.That(Texts(driver, "gd-lobby-seat-hero").Where(text => text.Length > 0),
                Is.EqualTo(new[] { PrivateLobbyLabels.Identifier(taken.HeroIdValue) }));
            Assert.That(PrivateLobbyModel.ForHost(wire.Session, "").Seats.Count(row => row.HeroLabel.Length > 0), Is.EqualTo(1));
            // The next picker (on the other team) sees the confirmed opponent before choosing.
            var nextOrdinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
            driver.Presenter.AttachClient(wire.Client(nextOrdinal));
            yield return null;
            yield return null;
            Assert.That(Text(driver, PrivateLobbyUnityView.SelectionStatusName), Is.EqualTo("YOUR PICK"));
            Assert.That(Texts(driver, "gd-lobby-seat-hero").Where(text => text.Length > 0),
                Is.EqualTo(new[] { PrivateLobbyLabels.Identifier(taken.HeroIdValue) }));
            var nextHero = HeroOptions(driver).First(button => button.enabledSelf);
            var nextHeroId = ((PrivateLobbyHeroOption)nextHero.userData).HeroIdValue;
            UiPlayer.Click(nextHero);
            yield return null;
            UiPlayer.Click(Control(driver, "PICK HERO"));
            wire.Pump();
            driver.Presenter.Refresh();
            yield return null;
            CollectionAssert.AreEquivalent(new[] { PrivateLobbyLabels.Identifier(taken.HeroIdValue), PrivateLobbyLabels.Identifier(nextHeroId) },
                Texts(driver, "gd-lobby-seat-hero").Where(text => text.Length > 0));
            Assert.That(PrivateLobbyModel.ForHost(wire.Session, "").Seats.Count(row => row.HeroLabel.Length > 0), Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator HeroProfileFilters_UseOneStarAndEverySelectedRole_AndClearWithTheSameButton()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            ReadyEverySeat(wire, seats);
            var ordinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
            driver.Presenter.AttachClient(wire.Client(ordinal));
            yield return null;

            var starButtons = Named(driver, PrivateLobbyUnityView.HeroStarFilterName)
                .OfType<Button>().ToArray();
            var roleButtons = Named(driver, PrivateLobbyUnityView.HeroRoleFilterName)
                .OfType<Button>().ToArray();
            var clear = (Button)Element(driver, PrivateLobbyUnityView.HeroFilterClearName);
            Assert.That(starButtons, Has.Length.EqualTo(4));
            Assert.That(roleButtons, Has.Length.EqualTo(10));
            Assert.That(HeroOptions(driver).Count, Is.EqualTo(RegisteredHeroRoster.ForThisBuild.Count));
            // Milestone 8.76: the roster box is sized by the whole roster and never resizes under a
            // filter - a filtered-out hero is hidden in place, and every other button stays put.
            var heroBox = Element(driver, PrivateLobbyUnityView.HeroesName);
            var fullHeight = heroBox.resolvedStyle.height;
            var fullWidth = heroBox.resolvedStyle.width;
            var restingBounds = AllHeroOptions(driver).ToDictionary(
                button => ((PrivateLobbyHeroOption)button.userData).HeroIdValue,
                button => button.worldBound);
            Assert.That(fullHeight, Is.GreaterThan(0f));
            Assert.That(clear.resolvedStyle.backgroundColor,
                Is.EqualTo(starButtons[0].resolvedStyle.backgroundColor),
                "CLEAR is a filter button, not a text link or separate treatment.");
            Assert.That(clear.resolvedStyle.borderTopLeftRadius,
                Is.EqualTo(starButtons[0].resolvedStyle.borderTopLeftRadius));

            Button Star(HeroComplexity value) => starButtons.Single(button =>
                button.userData is HeroComplexity complexity && complexity == value);
            Button Role(HeroRole value) => roleButtons.Single(button =>
                button.userData is HeroRole role && role == value);

            UiPlayer.Click(Star(HeroComplexity.Complex));
            yield return null;
            Assert.That(driver.View.SelectedComplexityFilter, Is.EqualTo(HeroComplexity.Complex));
            Assert.That(HeroOptions(driver).Select(button =>
                    ((PrivateLobbyHeroOption)button.userData).Complexity),
                Is.All.EqualTo(HeroComplexity.Complex));

            UiPlayer.Click(Star(HeroComplexity.VeryComplex));
            yield return null;
            Assert.That(driver.View.SelectedComplexityFilter, Is.EqualTo(HeroComplexity.VeryComplex),
                "Choosing another star replaces the first; star filters never accumulate.");
            UiPlayer.Click(Star(HeroComplexity.VeryComplex));
            yield return null;
            Assert.That(driver.View.SelectedComplexityFilter, Is.Null,
                "Clicking the active star clears it.");

            UiPlayer.Click(Role(HeroRole.Durable));
            UiPlayer.Click(Role(HeroRole.Healer));
            yield return null;
            Assert.That(HeroOptions(driver).Select(button =>
                    ((PrivateLobbyHeroOption)button.userData).Roles),
                Is.All.Matches<HeroRole>(roles =>
                    (roles & (HeroRole.Durable | HeroRole.Healer)) ==
                    (HeroRole.Durable | HeroRole.Healer)),
                "Role filters combine with AND, not OR.");

            UiPlayer.Click(Star(HeroComplexity.Complex));
            yield return null;
            CollectionAssert.AreEquivalent(
                new[] { "hero-bain", "hero-rowenna" },
                HeroOptions(driver).Select(button =>
                    ((PrivateLobbyHeroOption)button.userData).HeroIdValue));
            yield return UiPlayer.CaptureScreen(
                "tmp/8-35z86-hero-filters-1920x1080.png");

            var rowenna = HeroOptions(driver).Single(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-rowenna");
            UiPlayer.Click(rowenna);
            yield return null;
            Assert.That(driver.View.HighlightedHero, Is.EqualTo("hero-rowenna"));
            UiPlayer.Click(Role(HeroRole.Sniper));
            yield return null;
            Assert.That(driver.View.HighlightedHero, Is.Empty,
                "Filtering out the local candidate must not leave PICK HERO pointing at it.");
            CollectionAssert.AreEqual(
                new[] { "hero-bain" },
                HeroOptions(driver).Select(button =>
                    ((PrivateLobbyHeroOption)button.userData).HeroIdValue));

            driver.Presenter.Refresh();
            yield return null;
            Assert.That(HeroOptions(driver).Count, Is.EqualTo(1),
                "An ordinary model redraw preserves this client's local filter state.");
            Assert.That(AllHeroOptions(driver).Count, Is.EqualTo(RegisteredHeroRoster.ForThisBuild.Count),
                "8.76: a filter hides heroes in place; it removes none.");
            Assert.That(heroBox.resolvedStyle.height, Is.EqualTo(fullHeight).Within(0.5f),
                "8.76: the roster box keeps the size of the whole roster under a filter.");
            Assert.That(heroBox.resolvedStyle.width, Is.EqualTo(fullWidth).Within(0.5f));
            foreach (var button in AllHeroOptions(driver))
            {
                var id = ((PrivateLobbyHeroOption)button.userData).HeroIdValue;
                Assert.That(button.worldBound.position, Is.EqualTo(restingBounds[id].position),
                    "8.76: no hero moves when a filter changes - " + id);
            }
            var hiddenTile = AllHeroOptions(driver)
                .First(button => ((PrivateLobbyHeroOption)button.userData).HeroIdValue != "hero-bain");
            Assert.That(hiddenTile.focusable, Is.False, "a hidden hero takes no focus");
            Assert.That(hiddenTile.pickingMode, Is.EqualTo(PickingMode.Ignore), "nor any pointer");

            UiPlayer.Click(clear);
            yield return null;
            Assert.That(driver.View.SelectedComplexityFilter, Is.Null);
            Assert.That(driver.View.SelectedRoleFilters, Is.Empty);
            Assert.That(HeroOptions(driver).Count, Is.EqualTo(RegisteredHeroRoster.ForThisBuild.Count));

            UiPlayer.Click(Star(HeroComplexity.Challenging));
            // MILESTONE 8.71h. The frame is let settle between these two clicks, which is what every
            // other pair in this case already does and what this one alone did not.
            //
            // <b>Why it had to change at the twenty-first hero.</b> A click that changes the filter
            // rebuilds the hero list, and the list's height is what decides where the filter rows
            // above it sit. Reading a control's `worldBound` resolves that control's own layout on
            // demand, while `panel.Pick` answers from the panel's built pick geometry, so between a
            // tree mutation and the next frame the two can disagree - and a click aimed at the
            // freshly resolved centre lands on the row behind the button. With twenty heroes the two
            // readings happened to agree; with twenty-one the row moves far enough that they do not,
            // and milestone 8.71h's dealing is what made it visible. Nothing about the SCREEN is
            // wrong: a player's clicks are frames apart, and
            // `HanuIsFoundByHisThreeStarsAndEachOfHisThreeRoles` drives four filter buttons over the
            // whole twenty-one-hero roster with the layout settled between each.
            yield return null;
            UiPlayer.Click(Role(HeroRole.Tokens));
            yield return null;
            Assert.That(HeroOptions(driver), Is.Empty);
            Assert.That(heroBox.resolvedStyle.height, Is.EqualTo(fullHeight).Within(0.5f),
                "8.76: even an empty match keeps the box at its full size.");
            Assert.That(UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.HeroFilterEmptyName)), Is.True,
                "Zero matches is explicit rather than a stale highlighted hero or a blank unexplained row.");
        }

        [UnityTest]
        public IEnumerator EmmittIsPickableByHisFourStarsAndFivePrintedRoles()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            ReadyEverySeat(wire, seats);
            var ordinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
            driver.Presenter.AttachClient(wire.Client(ordinal));
            yield return null;

            var stars = Named(driver, PrivateLobbyUnityView.HeroStarFilterName)
                .OfType<Button>().Single(button => button.userData is HeroComplexity value &&
                    value == HeroComplexity.Wizard);
            var roleButtons = Named(driver, PrivateLobbyUnityView.HeroRoleFilterName)
                .OfType<Button>().ToArray();
            Button Role(HeroRole value) => roleButtons.Single(button =>
                button.userData is HeroRole role && role == value);
            bool Visible() => HeroOptions(driver).Any(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-emmitt");

            Assert.That(Visible(), Is.True, "Emmitt must be offered before filtering.");
            UiPlayer.Click(stars);
            yield return null;
            Assert.That(Visible(), Is.True, "the selection front prints four stars");
            foreach (var role in new[] { HeroRole.Melee, HeroRole.Tactician, HeroRole.Durable,
                         HeroRole.Disabler, HeroRole.Tokens })
            {
                UiPlayer.Click(Role(role));
                yield return null;
                Assert.That(Visible(), Is.True, $"Emmitt prints {role} and survives AND filtering.");
            }

            var option = HeroOptions(driver).Single(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-emmitt");
            UiPlayer.Click(option);
            yield return null;
            Assert.That(driver.View.HighlightedHero, Is.EqualTo("hero-emmitt"));
            if (option.parent != null) UiPlayer.PointAt(option.parent);
            UiPlayer.PointAt(option);
            yield return null;
            Assert.That(UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.HeroOverviewName)),
                Is.True, "hovering Emmitt shows his prepared selection front");
            yield return UiPlayer.CaptureScreen("tmp/949-emmitt-picker-1920x1080.png");

            UiPlayer.Click(Role(HeroRole.Healer));
            yield return null;
            Assert.That(Visible(), Is.False, "a role Emmitt does not print hides him");
            UiPlayer.Click(Element(driver, PrivateLobbyUnityView.HeroFilterClearName));
            yield return null;
            Assert.That(Visible(), Is.True, "clearing filters restores his button");
        }

        /// <summary>
        /// Milestone 8.69g, and THE USER'S ONE NAMED REQUIREMENT FOR THE TWENTIETH HERO: "keep in mind
        /// he needs to be correctly implemented in new hero pick screen". Brynn the Seeker's roster
        /// row carries three stars and Tactician, Damager, Durable, and this case drives the RENDERED
        /// star and role buttons over his button to prove the picker reads them.
        ///
        /// <para>It is the rendered tier rather than <c>HeroSelectionStageTests</c>'s because a row
        /// with the right facts and a filter that cannot see them looks identical offline. Each of his
        /// three roles is selected in turn and his button has to survive all three - role filters
        /// combine with AND, so a row missing one of them drops out at that click - and then Healer, a
        /// role he does not carry, has to remove him. The capture is taken with the three-star filter
        /// and Durable selected and is INSPECTED, not merely written.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BrynnIsFoundByHisThreeStarsAndEachOfHisThreeRoles()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            ReadyEverySeat(wire, seats);
            var ordinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
            driver.Presenter.AttachClient(wire.Client(ordinal));
            yield return null;

            var starButtons = Named(driver, PrivateLobbyUnityView.HeroStarFilterName)
                .OfType<Button>().ToArray();
            var roleButtons = Named(driver, PrivateLobbyUnityView.HeroRoleFilterName)
                .OfType<Button>().ToArray();
            Button Star(HeroComplexity value) => starButtons.Single(button =>
                button.userData is HeroComplexity complexity && complexity == value);
            Button Role(HeroRole value) => roleButtons.Single(button =>
                button.userData is HeroRole role && role == value);

            bool BrynnIsVisible() => HeroOptions(driver).Any(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-brynn");

            Assert.That(BrynnIsVisible(), Is.True,
                "He is the twentieth pick and the unfiltered list has to offer him at all.");

            UiPlayer.Click(Star(HeroComplexity.VeryComplex));
            yield return null;
            Assert.That(BrynnIsVisible(), Is.True,
                "THREE STARS: the selection card prints three filled stars, which p.21 calls Very " +
                "Complex, and the star filter reads the roster row this dealing wrote.");
            Assert.That(HeroOptions(driver).Select(button =>
                    ((PrivateLobbyHeroOption)button.userData).Complexity),
                Is.All.EqualTo(HeroComplexity.VeryComplex));

            UiPlayer.Click(Role(HeroRole.Tactician));
            yield return null;
            Assert.That(BrynnIsVisible(), Is.True, "and he is a TACTICIAN.");

            UiPlayer.Click(Role(HeroRole.Damager));
            yield return null;
            Assert.That(BrynnIsVisible(), Is.True,
                "and a DAMAGER - role filters combine with AND, so surviving the second click is a " +
                "claim about the row carrying both.");

            UiPlayer.Click(Role(HeroRole.Durable));
            yield return null;
            Assert.That(BrynnIsVisible(), Is.True, "and DURABLE, which is his third and last role.");

            var brynn = HeroOptions(driver).Single(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-brynn");
            UiPlayer.Click(brynn);
            yield return null;
            Assert.That(driver.View.HighlightedHero, Is.EqualTo("hero-brynn"),
                "and his button is pickable where it stands, with his own pick card shown beside it.");

            UiPlayer.Click(Role(HeroRole.Tactician));
            UiPlayer.Click(Role(HeroRole.Damager));
            yield return null;
            Assert.That(BrynnIsVisible(), Is.True,
                "Three stars and DURABLE alone is the state the capture below is taken in.");

            // The hover half, which is what the capture has to show: the picker draws the hovered
            // hero's own selection front out of `Private/HeroSelection/<heroId>`, so an overview that
            // stays hidden for him is a prepared front the dealing never wrote. The pointer is rested
            // on the button and on its tile, because the view registers the enter on the tile and the
            // focus on the button and a fixture must not depend on which of the two it gets.
            var hovered = HeroOptions(driver).Single(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-brynn");
            if (hovered.parent != null)
            {
                UiPlayer.PointAt(hovered.parent);
            }

            UiPlayer.PointAt(hovered);
            yield return null;
            Assert.That(
                UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.HeroOverviewName)), Is.True,
                "Hovering his button draws HIS pick card - the extraction's own selection front, " +
                "prepared as an ignored private resource by this same dealing.");
            yield return UiPlayer.CaptureScreen("tmp/869g-brynn-filters-1920x1080.png");

            UiPlayer.Click(Role(HeroRole.Healer));
            yield return null;
            Assert.That(BrynnIsVisible(), Is.False,
                "and a role he does NOT carry removes him, which is what says the filter is reading " +
                "his row rather than showing every hero whatever is selected.");

            // Milestone 8.69h. CLEAR is the other end of the same claim and his player-path gate
            // names this journey for it: a hero the filters can hide has to be a hero the filters
            // give back, or the picker's own escape hatch is a control that leaves the twentieth
            // pick unreachable for the rest of the lobby.
            UiPlayer.Click(Element(driver, PrivateLobbyUnityView.HeroFilterClearName));
            yield return null;
            Assert.That(driver.View.SelectedComplexityFilter, Is.Null);
            Assert.That(driver.View.SelectedRoleFilters, Is.Empty);
            Assert.That(BrynnIsVisible(), Is.True,
                "CLEAR restores him to the unfiltered list, where he is one of the registered " +
                "roster's own options.");
            Assert.That(HeroOptions(driver).Count,
                Is.EqualTo(RegisteredHeroRoster.ForThisBuild.Count));
        }

        /// <summary>
        /// Milestone 8.71h, and THE USER'S ONE NAMED REQUIREMENT FOR THE TWENTY-FIRST HERO: "Make
        /// sure pick screen is correctly imeplented". Hanu the Trickster's roster row carries three
        /// stars and Tactician, Sniper, Pusher, and this case drives the RENDERED star and role
        /// buttons over his button to prove the picker reads them.
        ///
        /// <para>It is the rendered tier rather than <c>HeroSelectionStageTests</c>'s because a row
        /// with the right facts and a filter that cannot see them looks identical offline. Each of
        /// his three roles is selected in turn and his button has to survive all three - role filters
        /// combine with AND, so a row missing one of them drops out at that click - and then Healer, a
        /// role he does not carry, has to remove him. The capture is taken with the three-star filter
        /// and Pusher selected and is INSPECTED, not merely written.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HanuIsFoundByHisThreeStarsAndEachOfHisThreeRoles()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            ReadyEverySeat(wire, seats);
            var ordinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
            driver.Presenter.AttachClient(wire.Client(ordinal));
            yield return null;

            var starButtons = Named(driver, PrivateLobbyUnityView.HeroStarFilterName)
                .OfType<Button>().ToArray();
            var roleButtons = Named(driver, PrivateLobbyUnityView.HeroRoleFilterName)
                .OfType<Button>().ToArray();
            Button Star(HeroComplexity value) => starButtons.Single(button =>
                button.userData is HeroComplexity complexity && complexity == value);
            Button Role(HeroRole value) => roleButtons.Single(button =>
                button.userData is HeroRole role && role == value);

            bool HanuIsVisible() => HeroOptions(driver).Any(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-hanu");

            Assert.That(HanuIsVisible(), Is.True,
                "He is the twenty-first pick and the unfiltered list has to offer him at all.");

            UiPlayer.Click(Star(HeroComplexity.VeryComplex));
            yield return null;
            Assert.That(HanuIsVisible(), Is.True,
                "THREE STARS: the selection card prints three filled stars, which p.21 calls Very " +
                "Complex, and the star filter reads the roster row this dealing wrote.");
            Assert.That(HeroOptions(driver).Select(button =>
                    ((PrivateLobbyHeroOption)button.userData).Complexity),
                Is.All.EqualTo(HeroComplexity.VeryComplex));

            UiPlayer.Click(Role(HeroRole.Tactician));
            yield return null;
            Assert.That(HanuIsVisible(), Is.True, "and he is a TACTICIAN.");

            UiPlayer.Click(Role(HeroRole.Sniper));
            yield return null;
            Assert.That(HanuIsVisible(), Is.True,
                "and a SNIPER - role filters combine with AND, so surviving the second click is a " +
                "claim about the row carrying both.");

            UiPlayer.Click(Role(HeroRole.Pusher));
            yield return null;
            Assert.That(HanuIsVisible(), Is.True, "and a PUSHER, which is his third and last role.");

            var hanu = HeroOptions(driver).Single(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-hanu");
            UiPlayer.Click(hanu);
            yield return null;
            Assert.That(driver.View.HighlightedHero, Is.EqualTo("hero-hanu"),
                "and his button is pickable where it stands, with his own pick card shown beside it.");

            UiPlayer.Click(Role(HeroRole.Tactician));
            UiPlayer.Click(Role(HeroRole.Sniper));
            yield return null;
            Assert.That(HanuIsVisible(), Is.True,
                "Three stars and PUSHER alone is the state the capture below is taken in.");

            // The hover half, which is what the capture has to show: the picker draws the hovered
            // hero's own selection front out of `Private/HeroSelection/<heroId>`, so an overview that
            // stays hidden for him is a prepared front the dealing never wrote. The pointer is rested
            // on the button and on its tile, because the view registers the enter on the tile and the
            // focus on the button and a fixture must not depend on which of the two it gets.
            var hovered = HeroOptions(driver).Single(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-hanu");
            if (hovered.parent != null)
            {
                UiPlayer.PointAt(hovered.parent);
            }

            UiPlayer.PointAt(hovered);
            yield return null;
            Assert.That(
                UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.HeroOverviewName)), Is.True,
                "Hovering his button draws HIS pick card - the extraction's own selection front, " +
                "prepared as an ignored private resource by this same dealing.");
            yield return UiPlayer.CaptureScreen("tmp/871h-hanu-filters-1920x1080.png");

            UiPlayer.Click(Role(HeroRole.Healer));
            yield return null;
            Assert.That(HanuIsVisible(), Is.False,
                "and a role he does NOT carry removes him, which is what says the filter is reading " +
                "his row rather than showing every hero whatever is selected.");
        }

        [UnityTest]
        public IEnumerator TrinketsIsPickedThroughThreeStarsAndAllFourPrintedRoles()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            ReadyEverySeat(wire, seats);
            var ordinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
            driver.Presenter.AttachClient(wire.Client(ordinal));
            yield return null;

            var stars = Named(driver, PrivateLobbyUnityView.HeroStarFilterName)
                .OfType<Button>().ToArray();
            var roles = Named(driver, PrivateLobbyUnityView.HeroRoleFilterName)
                .OfType<Button>().ToArray();
            Button Star(HeroComplexity value) => stars.Single(button =>
                button.userData is HeroComplexity complexity && complexity == value);
            Button Role(HeroRole value) => roles.Single(button =>
                button.userData is HeroRole role && role == value);
            bool Visible() => HeroOptions(driver).Any(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-trinkets");

            Assert.That(Visible(), Is.True);
            UiPlayer.Click(Star(HeroComplexity.VeryComplex));
            yield return null;
            Assert.That(Visible(), Is.True);
            foreach (var role in new[]
            {
                HeroRole.Sniper, HeroRole.Damager, HeroRole.Pusher, HeroRole.Tokens
            })
            {
                UiPlayer.Click(Role(role));
                yield return null;
                Assert.That(Visible(), Is.True, role.ToString());
            }

            var trinkets = HeroOptions(driver).Single(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-trinkets");
            UiPlayer.Click(trinkets);
            yield return null;
            Assert.That(driver.View.HighlightedHero, Is.EqualTo("hero-trinkets"));
            UiPlayer.PointAt(trinkets.parent ?? trinkets);
            UiPlayer.PointAt(trinkets);
            yield return null;
            Assert.That(UiPlayer.IsShown(Element(driver,
                PrivateLobbyUnityView.HeroOverviewName)), Is.True);
            yield return UiPlayer.CaptureScreen("tmp/892f-trinkets-pick-1920x1080.png");

            UiPlayer.Click(Role(HeroRole.Healer));
            yield return null;
            Assert.That(Visible(), Is.False);
        }
        [UnityTest]
        public IEnumerator SnorriIsPickedThroughFourStarsAndAllSixPrintedRoles()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            ReadyEverySeat(wire, seats);
            var ordinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
            driver.Presenter.AttachClient(wire.Client(ordinal));
            yield return null;

            var stars = Named(driver, PrivateLobbyUnityView.HeroStarFilterName)
                .OfType<Button>().ToArray();
            var roles = Named(driver, PrivateLobbyUnityView.HeroRoleFilterName)
                .OfType<Button>().ToArray();
            Button Star(HeroComplexity value) => stars.Single(button =>
                button.userData is HeroComplexity complexity && complexity == value);
            Button Role(HeroRole value) => roles.Single(button =>
                button.userData is HeroRole role && role == value);
            bool Visible() => HeroOptions(driver).Any(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-snorri");

            Assert.That(Visible(), Is.True);
            UiPlayer.Click(Star(HeroComplexity.Wizard));
            yield return null;
            Assert.That(Visible(), Is.True);
            foreach (var role in new[]
            {
                HeroRole.Sniper, HeroRole.Farming, HeroRole.Durable,
                HeroRole.Damager, HeroRole.Pusher, HeroRole.Healer
            })
            {
                UiPlayer.Click(Role(role));
                yield return null;
                Assert.That(Visible(), Is.True, role.ToString());
            }

            var snorri = HeroOptions(driver).Single(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-snorri");
            UiPlayer.Click(snorri);
            yield return null;
            Assert.That(driver.View.HighlightedHero, Is.EqualTo("hero-snorri"));
            UiPlayer.PointAt(snorri.parent ?? snorri);
            UiPlayer.PointAt(snorri);
            yield return null;
            Assert.That(UiPlayer.IsShown(Element(driver,
                PrivateLobbyUnityView.HeroOverviewName)), Is.True);
            yield return UiPlayer.CaptureScreen("tmp/893f-snorri-pick-1920x1080.png");

            UiPlayer.Click(Role(HeroRole.Tactician));
            yield return null;
            Assert.That(Visible(), Is.False);
        }

        /// <summary>
        /// Milestone 8.112g. Mrak's roster row carries three stars and Melee, Tokens, Durable,
        /// Disabler, Tactician; the rendered star and role buttons keep him visible through all
        /// five, a role he does not print hides him, and his button picks him.
        /// </summary>
        [UnityTest]
        public IEnumerator MrakIsPickedThroughThreeStarsAndAllFivePrintedRoles()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            ReadyEverySeat(wire, seats);
            var ordinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
            driver.Presenter.AttachClient(wire.Client(ordinal));
            yield return null;

            var stars = Named(driver, PrivateLobbyUnityView.HeroStarFilterName)
                .OfType<Button>().ToArray();
            var roles = Named(driver, PrivateLobbyUnityView.HeroRoleFilterName)
                .OfType<Button>().ToArray();
            Button Star(HeroComplexity value) => stars.Single(button =>
                button.userData is HeroComplexity complexity && complexity == value);
            Button Role(HeroRole value) => roles.Single(button =>
                button.userData is HeroRole role && role == value);
            bool Visible() => HeroOptions(driver).Any(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-mrak");

            Assert.That(Visible(), Is.True);
            UiPlayer.Click(Star(HeroComplexity.VeryComplex));
            yield return null;
            Assert.That(Visible(), Is.True);
            foreach (var role in new[]
            {
                HeroRole.Melee, HeroRole.Tokens, HeroRole.Durable, HeroRole.Disabler,
                HeroRole.Tactician
            })
            {
                UiPlayer.Click(Role(role));
                yield return null;
                Assert.That(Visible(), Is.True, role.ToString());
            }

            var mrak = HeroOptions(driver).Single(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-mrak");
            UiPlayer.Click(mrak);
            yield return null;
            Assert.That(driver.View.HighlightedHero, Is.EqualTo("hero-mrak"));
            UiPlayer.PointAt(mrak.parent ?? mrak);
            UiPlayer.PointAt(mrak);
            yield return null;
            Assert.That(UiPlayer.IsShown(Element(driver,
                PrivateLobbyUnityView.HeroOverviewName)), Is.True);
            yield return UiPlayer.CaptureScreen("tmp/8112g-mrak-pick-1920x1080.png");

            UiPlayer.Click(Role(HeroRole.Sniper));
            yield return null;
            Assert.That(Visible(), Is.False);
        }

        /// <summary>
        /// Milestone 8.114f. Wuk's roster row carries three stars and Tokens, Pusher, Durable,
        /// Sniper, Tactician, Healer; the rendered star and role buttons keep him visible through all
        /// six, a role he does not print hides him, and his button picks him.
        /// </summary>
        [UnityTest]
        public IEnumerator WukIsPickedThroughThreeStarsAndAllSixPrintedRoles()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            ReadyEverySeat(wire, seats);
            var ordinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
            driver.Presenter.AttachClient(wire.Client(ordinal));
            yield return null;

            var stars = Named(driver, PrivateLobbyUnityView.HeroStarFilterName)
                .OfType<Button>().ToArray();
            var roles = Named(driver, PrivateLobbyUnityView.HeroRoleFilterName)
                .OfType<Button>().ToArray();
            Button Star(HeroComplexity value) => stars.Single(button =>
                button.userData is HeroComplexity complexity && complexity == value);
            Button Role(HeroRole value) => roles.Single(button =>
                button.userData is HeroRole role && role == value);
            bool Visible() => HeroOptions(driver).Any(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-wuk");

            Assert.That(Visible(), Is.True);
            UiPlayer.Click(Star(HeroComplexity.VeryComplex));
            yield return null;
            Assert.That(Visible(), Is.True);
            foreach (var role in new[]
            {
                HeroRole.Tokens, HeroRole.Pusher, HeroRole.Durable, HeroRole.Sniper,
                HeroRole.Tactician, HeroRole.Healer
            })
            {
                UiPlayer.Click(Role(role));
                yield return null;
                Assert.That(Visible(), Is.True, role.ToString());
            }

            var wuk = HeroOptions(driver).Single(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-wuk");
            UiPlayer.Click(wuk);
            yield return null;
            Assert.That(driver.View.HighlightedHero, Is.EqualTo("hero-wuk"));
            UiPlayer.PointAt(wuk.parent ?? wuk);
            UiPlayer.PointAt(wuk);
            yield return null;
            Assert.That(UiPlayer.IsShown(Element(driver,
                PrivateLobbyUnityView.HeroOverviewName)), Is.True);
            yield return UiPlayer.CaptureScreen("tmp/8114f-wuk-pick-1920x1080.png");

            UiPlayer.Click(Role(HeroRole.Melee));
            yield return null;
            Assert.That(Visible(), Is.False);
        }

        /// <summary>
        /// Milestone 8.75b. Ignatia the Mad's roster row carries FOUR stars and five roles - Sniper,
        /// Damager, Tokens, Tactician, Pusher - and this case drives the RENDERED star and role
        /// buttons over her button to prove the picker reads them.
        ///
        /// <para><b>She is the first hero this picker's FOURTH star can find.</b> The star row has
        /// drawn four buttons since milestone 8.35z86 and twenty-one dealings left the last one
        /// filtering to an empty list, because no dealt hero printed p.21's Wizard band. Selecting it
        /// and finding exactly one hero is a claim nothing before this milestone could make.</para>
        ///
        /// <para>It is the rendered tier rather than <c>HeroSelectionStageTests</c>'s because a row
        /// with the right facts and a filter that cannot see them looks identical offline. Each of
        /// her FIVE roles is selected in turn and her button has to survive all five - role filters
        /// combine with AND, so a row missing one of them drops out at that click, and five is two
        /// more than any hero before her carried - and then Healer, a role she does not carry, has to
        /// remove her. The capture is taken with the four-star filter and Tokens selected and is
        /// INSPECTED, not merely written.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator IgnatiaIsFoundByHerFourStarsAndEachOfHerFiveRoles()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            ReadyEverySeat(wire, seats);
            var ordinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
            driver.Presenter.AttachClient(wire.Client(ordinal));
            yield return null;

            var starButtons = Named(driver, PrivateLobbyUnityView.HeroStarFilterName)
                .OfType<Button>().ToArray();
            var roleButtons = Named(driver, PrivateLobbyUnityView.HeroRoleFilterName)
                .OfType<Button>().ToArray();
            Button Star(HeroComplexity value) => starButtons.Single(button =>
                button.userData is HeroComplexity complexity && complexity == value);
            Button Role(HeroRole value) => roleButtons.Single(button =>
                button.userData is HeroRole role && role == value);

            bool IgnatiaIsVisible() => HeroOptions(driver).Any(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-ignatia");

            Assert.That(IgnatiaIsVisible(), Is.True,
                "She is the twenty-second pick and the unfiltered list has to offer her at all.");

            UiPlayer.Click(Star(HeroComplexity.Wizard));
            yield return null;
            Assert.That(IgnatiaIsVisible(), Is.True,
                "FOUR STARS: her selection card prints four filled stars, which p.21 calls Wizard, " +
                "and the star filter reads the roster row this dealing wrote. Twenty-one dealings " +
                "left this button filtering to nothing.");
            Assert.That(HeroOptions(driver).Select(button =>
                    ((PrivateLobbyHeroOption)button.userData).Complexity),
                Is.All.EqualTo(HeroComplexity.Wizard));
            Assert.That(HeroOptions(driver).Count, Is.EqualTo(6),
                "she, Snorri, Razzle, Gydion, Takahide and Emmitt print four stars.");

            UiPlayer.Click(Role(HeroRole.Sniper));
            yield return null;
            Assert.That(IgnatiaIsVisible(), Is.True, "and she is a SNIPER.");

            UiPlayer.Click(Role(HeroRole.Damager));
            yield return null;
            Assert.That(IgnatiaIsVisible(), Is.True,
                "and a DAMAGER - role filters combine with AND, so surviving the second click is a " +
                "claim about the row carrying both.");

            UiPlayer.Click(Role(HeroRole.Tokens));
            yield return null;
            Assert.That(IgnatiaIsVisible(), Is.True,
                "and TOKENS, which her three magma Paths are the reason for.");

            UiPlayer.Click(Role(HeroRole.Tactician));
            yield return null;
            Assert.That(IgnatiaIsVisible(), Is.True, "and a TACTICIAN.");

            UiPlayer.Click(Role(HeroRole.Pusher));
            yield return null;
            Assert.That(IgnatiaIsVisible(), Is.True,
                "and a PUSHER, which is her fifth and last role - two more than any hero before her " +
                "prints, and all five have to be on the row for her button to be here.");

            var ignatia = HeroOptions(driver).Single(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-ignatia");
            UiPlayer.Click(ignatia);
            yield return null;
            Assert.That(driver.View.HighlightedHero, Is.EqualTo("hero-ignatia"),
                "and her button is pickable where it stands, with her own pick card shown beside it.");

            UiPlayer.Click(Role(HeroRole.Sniper));
            yield return null;
            UiPlayer.Click(Role(HeroRole.Damager));
            yield return null;
            UiPlayer.Click(Role(HeroRole.Tactician));
            yield return null;
            UiPlayer.Click(Role(HeroRole.Pusher));
            yield return null;
            Assert.That(IgnatiaIsVisible(), Is.True,
                "Four stars and TOKENS alone is the state the capture below is taken in.");

            // The hover half, which is what the capture has to show: the picker draws the hovered
            // hero's own selection front out of `Private/HeroSelection/<heroId>`, so an overview that
            // stays hidden for her is a prepared front the dealing never wrote. The pointer is rested
            // on the button and on its tile, because the view registers the enter on the tile and the
            // focus on the button and a fixture must not depend on which of the two it gets.
            var hovered = HeroOptions(driver).Single(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-ignatia");
            if (hovered.parent != null)
            {
                UiPlayer.PointAt(hovered.parent);
            }

            UiPlayer.PointAt(hovered);
            yield return null;
            Assert.That(
                UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.HeroOverviewName)), Is.True,
                "Hovering her button draws HER pick card - the extraction's own selection front, " +
                "prepared as an ignored private resource by this dealing's artwork half.");
            yield return UiPlayer.CaptureScreen("tmp/875b-ignatia-filters-1920x1080.png");

            UiPlayer.Click(Role(HeroRole.Healer));
            yield return null;
            Assert.That(IgnatiaIsVisible(), Is.False,
                "and a role she does NOT carry removes her, which is what says the filter is reading " +
                "her row rather than showing every hero whatever is selected.");
        }

        /// <summary>
        /// Milestone 8.75c, and the half of her identity her match journeys cannot reach: the twenty-
        /// second hero is PICKED, on the real selection screen, out of a filtered list.
        ///
        /// <para>The sibling case above proves the filter READS her row. This one closes the loop a
        /// player actually walks: narrow the list to four stars and one of her roles, watch a role she
        /// does not carry hide her IN PLACE - milestone 8.76 hides a filtered-out hero by visibility
        /// rather than by display, so the assertion has to walk visibility too - press CLEAR to bring
        /// the whole roster back, hover her button to draw her own pick card, select her and press PICK
        /// HERO. The seat's chosen hero is then hers, which is the fact a composed match is dealt
        /// from.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator IgnatiaIsPickedThroughTheFilteredPickerAndClearRestoresHer()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            wire.Session.Composition = Root();
            ReadyEverySeat(wire, seats);
            var ordinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
            var client = wire.Client(ordinal);
            driver.Presenter.AttachClient(client);
            yield return null;

            var starButtons = Named(driver, PrivateLobbyUnityView.HeroStarFilterName)
                .OfType<Button>().ToArray();
            var roleButtons = Named(driver, PrivateLobbyUnityView.HeroRoleFilterName)
                .OfType<Button>().ToArray();
            Button Star(HeroComplexity value) => starButtons.Single(button =>
                button.userData is HeroComplexity complexity && complexity == value);
            Button Role(HeroRole value) => roleButtons.Single(button =>
                button.userData is HeroRole role && role == value);

            bool IgnatiaIsVisible() => HeroOptions(driver).Any(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-ignatia");

            UiPlayer.Click(Star(HeroComplexity.Wizard));
            yield return null;
            UiPlayer.Click(Role(HeroRole.Tokens));
            yield return null;
            Assert.That(HeroOptions(driver).Count, Is.EqualTo(3),
                "FOUR STARS and TOKENS narrows the roster to her, Gydion and Emmitt.");
            Assert.That(IgnatiaIsVisible(), Is.True);

            UiPlayer.Click(Role(HeroRole.Healer));
            yield return null;
            Assert.That(IgnatiaIsVisible(), Is.False,
                "A role she does not carry hides her - in place, by visibility, so the tile she " +
                "occupies stays where it was and nothing else moves.");
            Assert.That(
                AllHeroOptions(driver).Any(button =>
                    ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-ignatia"),
                Is.True,
                "and her button is still in the flow rather than removed from it.");

            UiPlayer.Click(Element(driver, PrivateLobbyUnityView.HeroFilterClearName));
            yield return null;
            Assert.That(HeroOptions(driver).Count,
                Is.EqualTo(RegisteredHeroRoster.ForThisBuild.Count),
                "CLEAR puts the whole roster back.");
            Assert.That(IgnatiaIsVisible(), Is.True);

            UiPlayer.Click(Star(HeroComplexity.Wizard));
            yield return null;
            var hers = HeroOptions(driver).Single(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-ignatia");
            if (hers.parent != null)
            {
                UiPlayer.PointAt(hers.parent);
            }

            UiPlayer.PointAt(hers);
            yield return null;
            Assert.That(
                UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.HeroOverviewName)), Is.True,
                "Hovering her button draws HER pick card.");

            UiPlayer.Click(hers);
            yield return null;
            Assert.That(driver.View.HighlightedHero, Is.EqualTo("hero-ignatia"),
                "and pressing it names the hero PICK HERO will confirm.");

            UiPlayer.Click(Control(driver, "PICK HERO"));
            wire.Pump();
            driver.Presenter.Refresh();
            yield return null;
            Assert.That(
                PrivateLobbyModel.ForClient(client).Selection.Heroes
                    .Single(option => option.HeroIdValue == "hero-ignatia").IsAvailable,
                Is.False,
                "THE PICK IS TAKEN: the twenty-second hero is no longer on offer to anybody, which " +
                "is what a completed pick looks like from every seat.");
        }

        /// <summary>
        /// Milestone 9.30i. The twenty-ninth hero on the real selection screen: Gydion's roster row
        /// carries four stars and six roles - Sniper, Tactician, Farming, Damager, Pusher and Tokens -
        /// and the rendered star and role buttons have to keep his button through all six (role filters
        /// combine with AND). Farming is the one role of his that Ignatia, the other four-star hero with
        /// the five she shares with him, does not print, so it narrows the list to him alone. Hovering
        /// his button draws his own pick card - the prepared selection front - and pressing it and PICK
        /// HERO takes him. The capture is written for the user's review.
        /// </summary>
        [UnityTest]
        public IEnumerator GydionIsFoundByHisFourStarsAndSixRoles_AndPicked()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            wire.Session.Composition = Root();
            ReadyEverySeat(wire, seats);
            var ordinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
            var client = wire.Client(ordinal);
            driver.Presenter.AttachClient(client);
            yield return null;

            var starButtons = Named(driver, PrivateLobbyUnityView.HeroStarFilterName)
                .OfType<Button>().ToArray();
            var roleButtons = Named(driver, PrivateLobbyUnityView.HeroRoleFilterName)
                .OfType<Button>().ToArray();
            Button Star(HeroComplexity value) => starButtons.Single(button =>
                button.userData is HeroComplexity complexity && complexity == value);
            Button Role(HeroRole value) => roleButtons.Single(button =>
                button.userData is HeroRole role && role == value);
            bool GydionIsVisible() => HeroOptions(driver).Any(button =>
                ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-gydion");

            Assert.That(GydionIsVisible(), Is.True, "He is the twenty-ninth pick and the unfiltered list offers him.");
            UiPlayer.Click(Star(HeroComplexity.Wizard));
            yield return null;
            Assert.That(GydionIsVisible(), Is.True, "FOUR STARS: his selection front prints four filled stars.");
            foreach (var role in new[]
            {
                HeroRole.Sniper, HeroRole.Tactician, HeroRole.Damager, HeroRole.Pusher, HeroRole.Tokens,
                HeroRole.Farming
            })
            {
                UiPlayer.Click(Role(role));
                yield return null;
                Assert.That(GydionIsVisible(), Is.True, role.ToString());
            }

            Assert.That(HeroOptions(driver).Select(button => ((PrivateLobbyHeroOption)button.userData).HeroIdValue).ToArray(),
                Is.EqualTo(new[] { "hero-gydion" }),
                "Four stars and all six of his roles leave him alone: Farming is the one Ignatia lacks.");

            var his = HeroOptions(driver).Single();
            if (his.parent != null)
            {
                UiPlayer.PointAt(his.parent);
            }

            UiPlayer.PointAt(his);
            yield return null;
            Assert.That(
                UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.HeroOverviewName)), Is.True,
                "Hovering his button draws HIS pick card - randomizers-tt32, prepared as an ignored " +
                "private resource by this dealing's artwork half.");
            yield return UiPlayer.CaptureScreen("tmp/9-30i-gydion-picker-1920x1080.png");

            UiPlayer.Click(his);
            yield return null;
            Assert.That(driver.View.HighlightedHero, Is.EqualTo("hero-gydion"));
            UiPlayer.Click(Control(driver, "PICK HERO"));
            wire.Pump();
            driver.Presenter.Refresh();
            yield return null;
            Assert.That(
                PrivateLobbyModel.ForClient(client).Selection.Heroes
                    .Single(option => option.HeroIdValue == "hero-gydion").IsAvailable,
                Is.False,
                "THE PICK IS TAKEN: the twenty-ninth hero is no longer on offer to anybody.");
        }

        /// <summary>
        /// Milestone 9.30j. The hero picker row with Gydion in it, captured for the user's review at
        /// 2560x1440 in <c>tmp/9-30j-gydion/</c>: the unfiltered row, then his button hovered with his
        /// pick card drawn. His starting hand and a cast in progress are
        /// <c>GydionDealingPlayModeTests</c>' captures.
        /// </summary>
        [UnityTest]
        public IEnumerator GydionsPickerRow_IsCapturedAt2560x1440()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                UiPlayer.SetPlayerViewport(2560, 1440, "9.30j picker row");
                yield return LoadTheLobby();
                var driver = Driver();
                var wire = SeatEveryone(out var seats);
                wire.Session.Composition = Root();
                ReadyEverySeat(wire, seats);
                var ordinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
                driver.Presenter.AttachClient(wire.Client(ordinal));
                yield return null;
                yield return null;

                var his = HeroOptions(driver).Single(button =>
                    ((PrivateLobbyHeroOption)button.userData).HeroIdValue == "hero-gydion");
                UiPlayer.EnsureVisible(his);
                yield return null;
                Assert.That(UiPlayer.IsShown(his), Is.True, "His button is in the picker row.");
                yield return UiPlayer.CaptureScreen("tmp/9-30j-gydion/picker-row-2560x1440.png");

                if (his.parent != null)
                {
                    UiPlayer.PointAt(his.parent);
                }

                UiPlayer.PointAt(his);
                yield return null;
                Assert.That(UiPlayer.IsShown(Element(driver, PrivateLobbyUnityView.HeroOverviewName)), Is.True,
                    "Hovering his button draws his pick card.");
                yield return UiPlayer.CaptureScreen("tmp/9-30j-gydion/picker-hover-2560x1440.png");
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "9.30j restore the viewport");
            }
        }

        [UnityTest]
        public IEnumerator PickerRemainsReadableAtNormalAndShortViewports()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            ReadyEverySeat(wire, seats);
            var ordinal = seats.ToList().IndexOf(wire.Session.Selection.PickingSeat.Value) + 1;
            driver.Presenter.AttachClient(wire.Client(ordinal));
            try
            {
                foreach (var viewport in new[] { new Vector2(1920, 1080), new Vector2(2560, 540) })
                {
                    UiPlayer.SetPlayerViewport((uint)viewport.x, (uint)viewport.y, "8.36e picker");
                    yield return null;
                    yield return null;
                    driver.Presenter.Refresh();
                    yield return null;
                    var option = HeroOptions(driver).Last();
                    UiPlayer.EnsureVisible(option);
                    yield return null;
                    UiPlayer.Click(option);
                    yield return null;
                    Assert.That(option.ClassListContains("gd-hero-selected"), Is.True, "8.76: the ring, not a suffix.");
                    if (viewport.x == 2560)
                        Assert.That(option.worldBound.height * Screen.width / driver.View.Root.worldBound.width,
                            Is.GreaterThanOrEqualTo(24f), "A short window must scroll, not miniaturize the picker.");
                    UiPlayer.PointAway(option.parent);
                    yield return null;
                    yield return UiPlayer.CaptureGameView("8-36e", "selection-" + (int)viewport.x + "x" + (int)viewport.y);
                    UiPlayer.PointAt(option.parent);
                    yield return null;
                    var overview = (Image)Element(driver, PrivateLobbyUnityView.HeroOverviewName);
                    Assert.That(UiPlayer.IsShown(overview), Is.True);
                    Assert.That(driver.View.Root.worldBound.Contains(overview.worldBound.min), Is.True);
                    Assert.That(driver.View.Root.worldBound.Contains(overview.worldBound.max), Is.True);
                    yield return UiPlayer.CaptureGameView("8-36e", "overview-" + (int)viewport.x + "x" + (int)viewport.y);
                    UiPlayer.PointAway(option.parent);
                    UiPlayer.EnsureVisible(Control(driver, "PICK HERO"));
                    yield return null;
                    Assert.That(UiPlayer.IsShown(Control(driver, "PICK HERO")), Is.True);
                }
                UiPlayer.Click(Control(driver, "PICK HERO"));
                wire.Pump();
                driver.Presenter.Refresh();
                yield return null;
                Assert.That(wire.Client(ordinal).Roster.Single(row => row.Seat.Value == seats[ordinal - 1]).HasChosenHero, Is.True);
                Assert.That(ControlLabels(driver), Does.Not.Contain("PICK HERO"));
            }
            finally { UiPlayer.SetPlayerViewport(1920, 1080, "8.36e restore"); }
        }

        /// <summary>
        /// A seat the printed order is NOT on is offered no pick control at all, so the sequence
        /// reads as a sequence rather than six people racing.
        /// </summary>
        /// <remarks>INTEGRATION-OWED: written and compile-checked only.</remarks>
        [UnityTest]
        public IEnumerator ASeatItIsNotTheTurnOf_IsOfferedNoPickControl()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            // Milestone 8.35z54: readiness is what opens the picking, so it comes first now.
            ReadyEverySeat(wire, seats);

            var picking = wire.Session.Selection.PickingSeat.Value;
            var waitingOrdinal = seats.ToList().FindIndex(seat => seat != picking) + 1;
            driver.Presenter.AttachClient(wire.Client(waitingOrdinal));
            yield return null;

            Assert.That(ControlLabels(driver), Does.Not.Contain("PICK HERO"));
            Assert.That(Text(driver, PrivateLobbyUnityView.SelectionStatusName),
                Is.EqualTo("WAITING ON " + PrivateLobbyLabels.Identifier(picking)));
        }

        /// <summary>
        /// THE WHOLE SEAM, end to end: six players pick heroes in the printed order, the sixth
        /// readies, and the board that appears is the one they chose rather than the shipped one.
        /// </summary>
        /// <remarks>
        /// <para>This is the journey the milestone actually owes, and the one thing that exercises
        /// <see cref="MatchSceneCompositionRoot.Recompose"/> against real Unity teardown: a composed
        /// runtime is discarded and rebuilt while a HUD, a map view and a pointer input all survive.
        /// The offline proof shows the two compositions are byte-identical; only a run shows the
        /// scene survives being recomposed.</para>
        /// <para>INTEGRATION-OWED: written and compile-checked only. NEVER EXECUTED.</para>
        /// </remarks>
        [UnityTest]
        public IEnumerator SixPickedHeroes_ComposeTheBoardThatWasChosen()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);

            // Milestone 8.35z37, corrected at integration. THE SEAM HAS TO BE ATTACHED FOR THIS CASE
            // TO BE TESTING IT. `PrivateSessionHost.Composition` is a settable property defaulting to
            // null, and a host with none silently keeps its provisional match - so this fixture's own
            // wire, which never set it, drove a host that could not recompose and fielded the printed
            // line-up while every pick was accepted. Production wires it in `PrivateLobbyViews`; the
            // scene's composition root IS the implementation, so the fixture attaches the same one.
            wire.Session.Composition = Root();

            driver.Presenter.AttachClient(wire.Client(1));
            yield return null;

            // Milestone 8.35z54 MOVED READINESS IN FRONT OF THE PICKS. It used to follow them, and the
            // sixth readiness is what started the match; now the sixth readiness OPENS the picking and
            // the LAST PICK starts the match. The order below is the user's spec read literally.
            ReadyEverySeat(wire, seats);
            Assert.That(wire.Session.HasMatchStarted, Is.False,
                "Readiness alone must not compose anything - at this instant nobody has picked, so a " +
                "match started here would be a match on a line-up nobody chose.");

            // Each side deliberately takes the OTHER side's printed heroes, so a board that ignored
            // the selection is indistinguishable from nothing at every assertion below.
            var chosen = new List<string>();
            // Milestone 8.35z37, corrected at integration. Recorded BY SEAT, not as a bag. Six
            // registered heroes filling six seats means every complete selection is the same SET, so
            // a sorted comparison is satisfied by a board that ignored the picks entirely - the exact
            // blindness the seam's own byte-identical proof needed a negative case to escape. What
            // distinguishes a board that honoured the selection is WHICH SEAT holds which hero.
            var chosenBySeat = new string[seats.Count];
            for (var pick = 0; pick < seats.Count; pick++)
            {
                var picking = wire.Session.Selection.PickingSeat.Value;
                var ordinal = seats.ToList().IndexOf(picking) + 1;
                var hero = wire.Session.Selection.AvailableHeroes.Last().HeroIdValue;
                Assert.That(wire.Client(ordinal).TryChooseHero(hero), Is.True,
                    "Seat " + picking + " could not take " + hero + ".");
                wire.Pump();
                chosen.Add(hero);
                chosenBySeat[ordinal - 1] = hero;
            }

            Assert.That(wire.Session.Selection.IsComplete, Is.True);

            driver.Presenter.Refresh();
            yield return null;
            yield return null;

            Assert.That(wire.Session.HasMatchStarted, Is.True);
            // The roster's order IS the composed seat order - `seats` was read straight off it - so
            // index i names the same chair on both sides of this comparison.
            var composedBySeat = Runtime().Host.Seats.Seats
                .Select(seat => seat.HeroId.Value)
                .ToArray();
            CollectionAssert.AreEqual(
                chosenBySeat,
                composedBySeat,
                "Each seat fields the hero THAT SEAT picked, which is the entire point of the " +
                "recomposition seam. A set comparison cannot say this: six heroes into six seats is " +
                "always the same six heroes.");

            // The negative half is carried by the selection itself rather than by a second fixture:
            // each seat takes the LAST available hero, so the assignment above is the printed one
            // reversed, and a board that had ignored the picks would field the printed assignment and
            // fail the comparison. That is what makes the equality above evidence rather than a
            // tautology.
            Assert.That(Root().ComposedPlan, Is.Not.Null);
        }

        /// <summary>
        /// The format picker, which is the seam's other half: pressing it on the entry form
        /// recomposes the scene onto a different printed setup, seats and all.
        /// </summary>
        /// <remarks>
        /// Milestone <c>8.35z54</c> REPLACED THE CONTROL THIS DRIVES, on measured evidence rather than
        /// taste. The old single SETUP button CYCLED through pp.5-6's four printed setups, and the
        /// user's own host log from the failed z51 session is six LOBBY_FORMAT presses and no
        /// LOBBY_HOST at all - they pressed it six times trying to reach 2v2, never got there, and
        /// gave up. Their words were "i dont need setup button, i need 2v2 and 3v3 buttons". So the
        /// cycle is now four NAMED controls, and this case presses one by name and asserts it lands
        /// on the format it names rather than merely on a different one.
        /// </remarks>
        [UnityTest]
        public IEnumerator TheFormatPicker_RecomposesTheSceneBeforeAnybodyIsSeated()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            yield return null;

            var before = Root().ComposedPlan.Format.Id;
            var seatsBefore = Runtime().Host.Seats.Seats.Count;
            // Milestone 8.107: the setup is chosen in the host menu.
            yield return OpenHostMenu(driver);
            foreach (var named in new[] { "2V2", "3V3" })
            {
                Assert.That(ControlLabels(driver), Contains.Item(named),
                    "The format controls stand on the entry form, where changing one can move no " +
                    "seat - and each says what it DOES. Drawn: " +
                    string.Join(", ", ControlLabels(driver)) + ".");
            }

            UiPlayer.Click(Control(driver, "2V2"));
            yield return null;
            yield return null;

            Assert.That(Root().ComposedPlan.Format.Id, Is.Not.EqualTo(before),
                "Pressing a named format recomposes onto that printed setup.");
            Assert.That(driver.View.Page, Is.EqualTo(PrivateLobbyEntryPage.Host),
                "Choosing a setup keeps the host menu open.");
            Assert.That(Text(driver, PrivateLobbyUnityView.ModeName), Does.StartWith("SETUP: 2V2"));
            Assert.That(driver.View.CurrentEntry().WaveCount, Is.EqualTo(3));
            Assert.That(driver.View.CurrentEntry().LifeCountPerTeam, Is.EqualTo(4),
                "Choosing 2V2 restores the former Quick values as that team size's defaults.");
            Assert.That(Root().ComposedPlan.Format.PlayerCount, Is.EqualTo(4),
                "2V2 IS FOUR SEATS, and this is the assertion the cycling button could not make: " +
                "a named control lands on the format it names, in one press, from any starting " +
                "format. Reached: " + Root().ComposedPlan.Format.Id + ".");
            Assert.That(Runtime(), Is.Not.Null,
                "...and the scene is composed again rather than left without a match.");
            Assert.That(
                Runtime().Host.Seats.Seats.Count,
                Is.EqualTo(Root().ComposedPlan.Format.PlayerCount),
                "The seats the entry form then lists are the new format's, which is what a joining " +
                "client's admission tuple will be checked against.");
            Assert.That(seatsBefore, Is.Not.Zero);
        }

        /// <summary>
        /// Milestone 8.35z62, DEFECT A. A format press that cannot apply SAYS SO, at the panel, in
        /// the same place every other lobby refusal is drawn.
        /// </summary>
        /// <remarks>
        /// <para>The user's 2026-09-08 session is the whole reason this case exists. They pressed
        /// 2V2 and got a six-seat 3v3 table, and their host log carries NO <c>LOBBY_FORMAT</c> line
        /// at all - because both format entry points opened with a guard that <c>return</c>ed
        /// SILENTLY once hosting had begun. Nothing was logged, nothing was drawn, and from the
        /// chair it is a dead button.</para>
        /// <para>The guard is CORRECT and is kept: seats are counted from the format, and every
        /// connection, epoch and reconnect capability the host holds is keyed by seat. This case
        /// pins the SILENCE as the defect, not the refusal - it presses the control through the
        /// real panel, exactly as a finger does, and requires a drawn sentence naming why.</para>
        /// </remarks>
        [UnityTest]
        public IEnumerator AFormatPressAfterHostingBegins_IsRefusedInWordsRatherThanIgnored()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            yield return null;

            var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            probe.Start();
            var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            yield return OpenHostMenu(driver);
            Field(driver, PrivateLobbyUnityView.HostPortFieldName).value =
                port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Field(driver, PrivateLobbyUnityView.HostKeyFieldName).value = string.Empty;
            yield return null;

            var composedBeforeHosting = Root().ComposedPlan.Format.Id;
            UiPlayer.Click(Control(driver, "HOST"));
            yield return null;
            yield return null;
            yield return null;
            Assert.That(driver.Presenter.Model.Role, Is.EqualTo(PrivateLobbyRole.Host),
                "This case is about a press made WHILE hosting, so hosting has to have begun.");

            // The press a finger makes. If the format controls are not drawn once hosting has begun,
            // that is itself an acceptable answer to "never ignored" - a control that is not there
            // cannot be pressed - so the case admits either shape and requires only that the press,
            // where it is possible, is answered.
            var formatControls = ControlLabels(driver).Where(label =>
                label == "2V2" || label == "3V3").ToArray();
            if (formatControls.Length == 0)
            {
                Assert.Pass(
                    "The lobby withdraws its format controls once hosting begins, which answers " +
                    "'never silently ignored' by making the press unreachable. Drawn: " +
                    string.Join(", ", ControlLabels(driver)) + ".");
            }

            var pressed = formatControls[0];
            UiPlayer.Click(Control(driver, pressed));
            yield return null;
            yield return null;

            var said = Text(driver, PrivateLobbyUnityView.FeedbackName);
            Assert.That(said, Is.Not.Empty,
                "Pressing " + pressed + " while hosting drew NOTHING. That is the defect: the " +
                "refusal is correct, the silence is not.");
            Assert.That(said.Length, Is.LessThanOrEqualTo(PrivateLobbyLabels.MaxLabelChars),
                "A refusal is one bounded line like every other the lobby draws.");
            Assert.That(Root().ComposedPlan.Format.Id, Is.EqualTo(composedBeforeHosting),
                "...and the refusal actually refused: the composed setup is unmoved, so no seat " +
                "was pulled out from under anybody holding one.");
        }

        /// <summary>
        /// Milestone 8.35z62, DEFECT B. THE RENDERED TIER after the one-way transition: the hero
        /// rails on screen name the heroes that were PICKED, and the seat is no longer the Director.
        /// </summary>
        /// <remarks>
        /// <para><b>Why this case exists beside
        /// <see cref="SixPickedHeroes_ComposeTheBoardThatWasChosen"/>, which already passes.</b> That
        /// case asserts the MODEL - <c>Runtime().Host.Seats.Seats</c> - and the 2026-09-08 session
        /// proved a model can be entirely right while the screen is wrong: the board spawned the
        /// picked heroes correctly and the side rails went on drawing the printed demo six, with a
        /// hand that was still Director-scoped and cyclable across everybody's cards. A projection
        /// assertion cannot see that. This one queries the actual UI Toolkit panel.</para>
        /// <para>Each rail stamps its own hero on <c>userData</c> (milestone 8.35z11 put it there for
        /// the kinetic anchors), so "which heroes are on screen" is answerable exactly rather than by
        /// reading labels.</para>
        /// </remarks>
        [UnityTest]
        public IEnumerator TheTransition_DrawsTheRailsOfTheHeroesThatWerePicked()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            wire.Session.Composition = Root();
            driver.Presenter.AttachClient(wire.Client(1));
            yield return null;

            ReadyEverySeat(wire, seats);

            var chosenBySeat = new string[seats.Count];
            for (var pick = 0; pick < seats.Count; pick++)
            {
                var picking = wire.Session.Selection.PickingSeat.Value;
                var ordinal = seats.ToList().IndexOf(picking) + 1;
                var hero = wire.Session.Selection.AvailableHeroes.Last().HeroIdValue;
                Assert.That(wire.Client(ordinal).TryChooseHero(hero), Is.True,
                    "Seat " + picking + " could not take " + hero + ".");
                wire.Pump();
                chosenBySeat[ordinal - 1] = hero;
            }

            Assert.That(wire.Session.Selection.IsComplete, Is.True);

            driver.Presenter.Refresh();
            yield return null;
            yield return null;
            Assert.That(wire.Session.HasMatchStarted, Is.True);

            // THE SEAM, driven exactly as PrivateLobbyDriver drives it at the one-way transition.
            //
            // This fixture attaches its client to the PRESENTER, which is how every case in this
            // file has always seated one - but the driver's own transition block reads its private
            // `machine` field, which only a real host/join through the panel sets. So a case that
            // stopped at Refresh() would exercise no transition at all, and would "fail" for the
            // uninteresting reason that the HUD was never attached to anything. The runtime call
            // below IS the seam under test: it is the one line the driver runs after recomposing,
            // and it is where every seat-scoped presenter and every rebuilt view is bound.
            var seatedClient = wire.Client(1);
            Assert.That(seatedClient.HasMatchStarted, Is.True,
                "The admitted client must see the match started before the HUD may attach.");
            Root().Runtime.AttachOnlineClient(seatedClient);
            Root().Runtime.RefreshAll();
            yield return null;
            yield return null;

            var hud = Root().Hud;
            Assert.That(hud, Is.Not.Null, "The match HUD is not composed into the scene.");

            var rails = UiPlayer.Descendants(hud.RootElement)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            var drawnHeroes = rails
                .Select(rail => rail.userData)
                .OfType<HeroId>()
                .Select(id => id.Value)
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

            var composedHeroes = Runtime().Host.Seats.Seats
                .Select(seat => seat.HeroId.Value)
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();
            var pickedHeroes = chosenBySeat
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

            // Diagnostics, printed before the assertions so a failure says WHICH tier disagreed:
            // the runtime's own read model, the view instance the HUD is currently configured with,
            // and how many rail containers are in the tree at all.
            var readModelHeroes = Runtime().ReadModel.Heroes
                .Select(occupancy => occupancy.HeroId.Value)
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();
            var railRoots = UiPlayer.Descendants(hud.RootElement)
                .Count(element => element.name == "gd-hero-rails");
            Debug.Log(
                "GUARDS_DIAG readModel=[" + string.Join(",", readModelHeroes) + "] " +
                "railRoots=" + railRoots + " rails=" + rails.Length +
                " viewRootInTree=" + (Runtime().HeroOverviewsView?.Root?.panel != null) +
                " seat=" + (Runtime().Seat?.Id.Value ?? "<none>"));

            CollectionAssert.AreEqual(pickedHeroes, composedHeroes,
                "Precondition: the composed match must already field the picked heroes, or this " +
                "case is testing the wrong seam.");
            CollectionAssert.AreEqual(pickedHeroes, readModelHeroes,
                "The runtime's OWN read model must field the picked heroes. If this is what fails, " +
                "the defect is upstream of any view: the HUD is bound to a match nobody chose.");
            Assert.That(rails, Has.Length.EqualTo(composedHeroes.Length),
                "One rail per hero in the composed match. Drawn " + rails.Length + " rails for " +
                composedHeroes.Length + " seats.");
            CollectionAssert.AreEqual(pickedHeroes, drawnHeroes,
                "THE RAILS ON SCREEN MUST NAME THE HEROES THAT WERE PICKED. Drawn: " +
                string.Join(", ", drawnHeroes) + ". Picked: " + string.Join(", ", pickedHeroes) +
                ". A mismatch here is the 2026-09-08 report exactly: correct tokens on the board, " +
                "six strangers in the trays.");

            Assert.That(Runtime().Seat, Is.Not.Null,
                "The transition must bind the admitted seat, or the HUD is still the Director's.");
            Assert.That(
                Runtime().SeatScopedPresenters.All(presenter => presenter.HomeHeroId.IsDefined),
                Is.True,
                "Every seat-scoped presenter carries the admitted seat's hero after the transition; " +
                "a half-bound HUD - some panels the seat's, others the Director's - is the state " +
                "milestone 8.35z4's fan-out exists to make unreachable.");
        }

        /// <summary>
        /// Milestone 8.35z58's owed DISABLED-state evidence, photographed on the one lobby control
        /// that genuinely draws one: p.22's already-taken hero option, dim and carrying the reason.
        /// </summary>
        /// <remarks>
        /// It lives here rather than beside the wave's other captures because reaching a greyed
        /// option needs this file's own seating, readiness and pick helpers - the entry form draws no
        /// unavailable control at all before anybody is seated, which is correct and is why the
        /// standalone capture case could not photograph one.
        /// </remarks>
        [UnityTest]
        public IEnumerator TheTakenHeroOption_IsPhotographedInItsDisabledTreatment()
        {
            yield return LoadTheLobby();
            var driver = Driver();
            var wire = SeatEveryone(out var seats);
            ReadyEverySeat(wire, seats);

            var picking = wire.Session.Selection.PickingSeat.Value;
            var pickingOrdinal = seats.ToList().IndexOf(picking) + 1;
            driver.Presenter.AttachClient(wire.Client(pickingOrdinal));
            yield return null;

            const string Shot =
                "8-35d-8-35e-multiplayer-corrections/integration-screenshots/polish-wave";
            yield return UiPlayer.CaptureGameView(Shot, "lobby-hero-options-idle");

            var taken = RegisteredHeroRoster.ForThisBuild[0];
            Assert.That(wire.Client(pickingOrdinal).TryChooseHero(taken.HeroIdValue), Is.True);
            wire.Pump();
            driver.Presenter.Refresh();
            yield return null;
            yield return null;

            var greyed = HeroOptions(driver)
                .Single(button => button.text.StartsWith(taken.DisplayName));
            Assert.That(greyed.enabledSelf, Is.False,
                "The shot is only evidence of the disabled treatment if the control is disabled.");
            UiPlayer.EnsureVisible(greyed);
            yield return null;
            yield return UiPlayer.CaptureGameView(Shot, "lobby-button-disabled");

            // And the hover treatment ON the disabled control, which is where 8.35z58's cursor
            // tooltip naming the unavailable reason is drawn.
            UiPlayer.PointAt(greyed);
            yield return null;
            yield return UiPlayer.CaptureGameView(Shot, "lobby-button-disabled-hover");
            UiPlayer.PointAway(greyed);
        }

        /// <summary>Every registered hero's option button on the selection panel.</summary>
        /// <summary>
        /// The hero buttons a player can see. Milestone 8.76: every hero keeps its tile in the flow
        /// and a filtered-out one is hidden IN PLACE (visibility, not display), so "the options" a
        /// case reasons about are the shown ones, read through the same visibility walk a player's
        /// eye makes; <see cref="AllHeroOptions"/> is the whole tree.
        /// </summary>
        private static IReadOnlyList<Button> HeroOptions(PrivateLobbyDriver driver) =>
            AllHeroOptions(driver).Where(IsHeroOptionShown).ToArray();

        private static IReadOnlyList<Button> AllHeroOptions(PrivateLobbyDriver driver) =>
            Named(driver, PrivateLobbyUnityView.HeroOptionName)
                .OfType<Button>()
                .ToArray();

        private static bool IsHeroOptionShown(VisualElement element)
        {
            for (var current = element; current != null; current = current.hierarchy.parent)
            {
                if (current.resolvedStyle.display == DisplayStyle.None ||
                    current.resolvedStyle.visibility == Visibility.Hidden)
                {
                    return false;
                }
            }

            return true;
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

        /// <summary>
        /// Milestone <c>8.35z54</c>. Readies every seated client, which is now what OPENS the picking.
        /// </summary>
        /// <remarks>
        /// Before this milestone the selection was open from the moment everyone was seated, so a case
        /// that wanted to pick could read <c>Selection.PickingSeat</c> straight after seating. The
        /// user's flow spec put readiness in front of it ("Once they are ready, everyone picks their
        /// heroes"), so every picking case now goes through here first; without it the selection is
        /// closed, no seat is picking, and the surface correctly says nothing at all.
        /// </remarks>
        private static void ReadyEverySeat(LobbyWire wire, IReadOnlyList<string> seats)
        {
            for (var ordinal = 1; ordinal <= seats.Count; ordinal++)
            {
                Assert.That(wire.Client(ordinal).TrySetReadiness(true), Is.True,
                    "Seat " + seats[ordinal - 1] + " could not ready.");
                wire.Pump();
            }

            Assert.That(wire.Session.IsPickPhaseOpen, Is.True,
                "Every seat is ready, so p.22's picking is open.");
        }

        /// <summary>
        /// Milestone <c>8.35z54</c>. Runs rulebook p.22's picking to its end and answers with how many
        /// picks it took, so a caller asserts the count rather than trusting the loop.
        /// </summary>
        /// <remarks>
        /// It picks the way the shipped drivers do and for the same reason (milestone 8.35c's reusable
        /// finding): out of the options THIS seat's own model offers, never out of a list the test
        /// happens to know. The picking seat is read from the session between picks because the order
        /// alternates by team and is opened by a coin this fixture does not fix.
        /// </remarks>
        private static int PickEverySeat(LobbyWire wire, IReadOnlyList<string> seats)
        {
            var picked = 0;
            while (!wire.Session.HasMatchStarted && picked < seats.Count)
            {
                var picking = wire.Session.Selection.PickingSeat;
                var ordinal = -1;
                for (var index = 0; index < seats.Count; index++)
                {
                    if (string.Equals(seats[index], picking.Value, System.StringComparison.Ordinal))
                    {
                        ordinal = index + 1;
                        break;
                    }
                }

                Assert.That(ordinal, Is.GreaterThan(0),
                    "The seat the order is on must be one of the seats this wire connected: " +
                    picking.Value + ".");
                var client = wire.Client(ordinal);
                var offered = PrivateLobbyModel.ForClient(client).Selection.Heroes
                    .First(option => option.IsAvailable);
                Assert.That(client.TryChooseHero(offered.HeroIdValue), Is.True,
                    "Seat " + picking.Value + " was offered " + offered.HeroIdValue +
                    " and the press must reach the host.");
                wire.Pump();
                picked++;
            }

            return picked;
        }

        private static void PlayProjectedHuman(
            ConfiguredMatchSceneRuntime runtime,
            GuardsDigital.Application.Bots.IBotRandom random)
        {
            if (runtime.ReadModel.Outcome.HasEnded)
                return;
            var agreement = runtime.ReadModel.TeamAgreement;
            if (agreement != null && agreement.AuthorizedCommands.Count > 0)
            {
                var statement = StatementToMake(agreement);
                if (statement != null)
                    runtime.SubmitProjectedAgreement(statement);
                return;
            }

            var commands = runtime.EnumerateProjectedCommands();
            if (commands.Count == 0) return;
            var selected = GuardsDigital.Application.Bots.BotPolicy.SelectPrivateSessionTactics(
                commands, runtime.ReadModel, runtime.Seat.HeroId, random) ?? commands[0];
            runtime.SubmitProjectedCommand(selected);
        }

        private static void PlayNetworkHuman(
            PrivateSessionClient machine,
            NetworkMatchClient client,
            GuardsDigital.Application.Bots.IBotRandom random)
        {
            if (machine.IsWaitingForHost || machine.LatestProjection == null) return;
            var projection = client.Projection;
            if (projection.Outcome.HasEnded) return;
            var agreement = projection.TeamAgreement;
            if (agreement != null && agreement.AuthorizedCommands.Count > 0)
            {
                var statement = StatementToMake(agreement);
                if (statement != null)
                    client.SubmitAgreement(statement);
                return;
            }

            var commands = machine.LatestProjection.AuthorizedCommands;
            if (commands.Count == 0) return;
            var hero = new HeroId(machine.LatestProjection.HeroId);
            var selected = GuardsDigital.Application.Bots.BotPolicy.SelectPrivateSessionTactics(
                commands, projection, hero, random) ?? commands[0];
            client.Submit(selected);
        }

        /// <summary>
        /// Milestone 9.29. The first offered preference of a tied Initiative; at a Minion Battle, the first
        /// offered pick until this seat's own set is complete, and nothing after - pressing the first
        /// offered statement every frame would take back the pick it had just made, for ever.
        /// </summary>
        private static TeamAgreementCommand StatementToMake(TeamAgreementProjection agreement)
        {
            if (agreement.Kind != TeamAgreementKind.MinionBattle)
                return agreement.AuthorizedCommands[0];
            return agreement.ViewerPicks.Count < agreement.RequiredSelectionCount
                ? agreement.AuthorizedCommands.FirstOrDefault(value =>
                    value.Kind == TeamAgreementCommandKind.PickMinionBattleRemoval)
                : null;
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
            // Milestone 8.107: every journey starts from an empty remembered-field store and never
            // asks the real router for this network's internet address.
            PrivateLobbyDriver.FieldMemoryOverride = new InMemoryPrivateLobbyFieldMemory();
            PrivateLobbyDriver.SuppressRouterQuestion = true;
            var host = new GameObject("gd-private-lobby");
            host.AddComponent<PrivateLobbyDriver>().Configure(Root());
            yield return null;
            yield return null;
        }

        /// <summary>Milestone 8.107. The first screen's two buttons, sorted as ControlLabels sorts.</summary>
        private static readonly string[] LandingControls = { "HOST", "JOIN" };

        /// <summary>
        /// Milestone 8.107. Presses the first screen's HOST, which opens the host menu, and waits
        /// for the menu's controls to be laid out so the next click can land on one.
        /// </summary>
        private static IEnumerator OpenHostMenu(PrivateLobbyDriver driver) =>
            OpenMenu(driver, "HOST", PrivateLobbyEntryPage.Host);

        /// <summary>Milestone 8.107. Presses the first screen's JOIN, which opens the join menu.</summary>
        private static IEnumerator OpenJoinMenu(PrivateLobbyDriver driver) =>
            OpenMenu(driver, "JOIN", PrivateLobbyEntryPage.Join);

        private static IEnumerator OpenMenu(
            PrivateLobbyDriver driver, string label, PrivateLobbyEntryPage page)
        {
            Assert.That(driver.View.Page, Is.EqualTo(PrivateLobbyEntryPage.Landing),
                "A menu opens from the first screen.");
            // The landing's buttons are rebuilt on every render; give a fresh one a layout pass.
            yield return null;
            UiPlayer.Click(Control(driver, label));
            yield return null;
            Assert.That(driver.View.Page, Is.EqualTo(page));
            yield return null;
        }
    }
}
