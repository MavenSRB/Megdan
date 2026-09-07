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

            // Somebody takes a hero; every other seat must now see it greyed AND be told why.
            var taken = RegisteredHeroRoster.ForThisBuild[0];
            Assert.That(wire.Client(pickingOrdinal).TryChooseHero(taken.HeroIdValue), Is.True);
            wire.Pump();
            driver.Presenter.Refresh();
            yield return null;

            var greyed = HeroOptions(driver)
                .Single(button => button.text.StartsWith(taken.DisplayName));
            Assert.That(greyed.enabledSelf, Is.False);
            Assert.That(greyed.text, Does.Contain("already taken"),
                "A hero that vanished would teach a player nothing; the one-copy rule's own " +
                "sentence rides the option itself.");
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
            foreach (var named in new[] { "2V2", "3V3", "SHORT GAME", "LONG GAME" })
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

            Field(driver, PrivateLobbyUnityView.AddressFieldName).value = "127.0.0.1";
            Field(driver, PrivateLobbyUnityView.PortFieldName).value =
                port.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Field(driver, PrivateLobbyUnityView.KeyFieldName).value = string.Empty;
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
                label == "2V2" || label == "3V3" ||
                label == "SHORT GAME" || label == "LONG GAME").ToArray();
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
        private static IReadOnlyList<Button> HeroOptions(PrivateLobbyDriver driver) =>
            Named(driver, PrivateLobbyUnityView.HeroOptionName)
                .OfType<Button>()
                .ToArray();

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
