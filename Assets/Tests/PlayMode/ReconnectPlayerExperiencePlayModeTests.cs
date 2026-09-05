using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Replay;
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
    /// Milestone 8.34. The player's half of reconnect, in a live scene: while a seat is away the
    /// match holds still and the board that player left is the board they come back to; a returning
    /// seat is rebuilt from its own complete projection with no other seat's cards in it, no stale
    /// selection, and one of each control; and when the host is gone every control the seat was
    /// offered becomes something it cannot press, with the surface clearing rather than
    /// accumulating.
    /// </summary>
    /// <remarks>
    /// <para>The session half runs a real <see cref="PrivateSessionHost"/> and real
    /// <see cref="PrivateSessionClient"/> machines over the scene's own authority, so what the
    /// fixture asserts about pausing, epochs and reprojection is what the shipped session
    /// machinery does rather than a restatement of it.</para>
    /// <para>What this fixture deliberately does not do is drive the HUD <em>through</em> a network
    /// client: the seat-scoped HUD reads a local seat projection, and putting the network adapter
    /// under it is milestone 8.35b's whole subject. The two surfaces are compared here - the
    /// projection the HUD is built from and the record the wire delivers are the same seat's, from
    /// the same authority - which is the strongest honest statement available before that
    /// milestone exists.</para>
    /// </remarks>
    public sealed class ReconnectPlayerExperiencePlayModeTests
    {
        private const int DriveBudget = 400;

        /// <summary>The in-memory wire this fixture puts over the scene's own authority.</summary>
        private sealed class SceneWire
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
                private readonly SceneWire wire;
                private readonly Link link;

                public ClientOutbox(SceneWire wire, Link link)
                {
                    this.wire = wire;
                    this.link = link;
                }

                public void Send(byte[] frame) => wire.toHost.Enqueue((link.Ordinal, frame));

                public void Close() => wire.CloseFromClient(link.Ordinal);
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

            private readonly Queue<(int Ordinal, byte[] Bytes)> toHost =
                new Queue<(int, byte[])>();
            private readonly Queue<(int Ordinal, byte[] Bytes)> toClient =
                new Queue<(int, byte[])>();
            private readonly Dictionary<int, Link> links = new Dictionary<int, Link>();
            private readonly Dictionary<int, PrivateSessionFrameAssembler> retired =
                new Dictionary<int, PrivateSessionFrameAssembler>();
            private int nextOrdinal = 10;

            private SceneWire(PrivateSessionHost session, PrivateSessionIdentity identity)
            {
                Session = session;
                Identity = identity;
            }

            public PrivateSessionHost Session { get; }
            public PrivateSessionIdentity Identity { get; }

            public static SceneWire Compose(IMatchHost matchHost, PrivateSessionIdentity identity)
            {
                SceneWire wire = null;
                var session = new PrivateSessionHost(
                    matchHost, identity, string.Empty,
                    new HostOutboxProxy(() => wire), new Nonces());
                wire = new SceneWire(session, identity);
                return wire;
            }

            private sealed class HostOutboxProxy : IPrivateSessionOutbox
            {
                private readonly System.Func<SceneWire> wire;

                public HostOutboxProxy(System.Func<SceneWire> wire)
                {
                    this.wire = wire;
                }

                public void Send(int connectionOrdinal, byte[] frame) =>
                    wire().toClient.Enqueue((connectionOrdinal, frame));

                public void Close(int connectionOrdinal) => wire().CloseFromHost(connectionOrdinal);
            }

            public PrivateSessionClient Client(int ordinal) => links[ordinal].Machine;

            public PrivateSessionClient Connect(int ordinal, string seat)
            {
                var link = new Link { Ordinal = ordinal, Seat = seat };
                link.Machine = new PrivateSessionClient(
                    Identity, new SeatId(seat), string.Empty,
                    new ClientOutbox(this, link), new Nonces());
                links[ordinal] = link;
                nextOrdinal = Mathf.Max(nextOrdinal, ordinal + 1);
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

            public int Reconnect(int ordinal)
            {
                var link = links[ordinal];
                Assert.That(link.Machine.BeginReconnect(), Is.True,
                    "Seat " + link.Seat + " could not arm a return from " + link.Machine.State + ".");
                var replacement = nextOrdinal++;
                links.Remove(ordinal);
                retired[ordinal] = new PrivateSessionFrameAssembler();
                link.Ordinal = replacement;
                link.FromHost = new PrivateSessionFrameAssembler();
                link.ToHost = new PrivateSessionFrameAssembler();
                link.Closed = false;
                links[replacement] = link;
                link.Machine.OnConnected();
                Session.OnConnected(replacement, 0);
                Pump();
                return replacement;
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
                if (!links.TryGetValue(ordinal, out var link))
                {
                    if (!retired.TryGetValue(ordinal, out var assembler))
                    {
                        retired[ordinal] = assembler = new PrivateSessionFrameAssembler();
                    }

                    if (assembler.Add(bytes, 0, bytes.Length))
                    {
                        while (assembler.TryDequeue(out var late))
                        {
                            Session.OnFrame(ordinal, late);
                        }
                    }

                    return;
                }

                if (link.Closed || !link.ToHost.Add(bytes, 0, bytes.Length))
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
        /// While a seat is away the match holds still, and the player who comes back is rebuilt
        /// from a complete projection of the position they left - under a rotated epoch, carrying
        /// their own cards and no other seat's.
        /// </summary>
        [UnityTest]
        public IEnumerator AnAwaySeat_HoldsTheMatchStill_AndReturnsToTheBoardItLeft()
        {
            UiPlayer.EnsurePlayerViewport();
            yield return LoadSeatScene(0);
            var root = Root();
            var runtime = root.Runtime;
            runtime.RefreshAll();
            yield return null;
            yield return null;

            var wire = SceneWire.Compose(runtime.Host, runtime.SessionIdentity);
            var seats = runtime.Host.Seats.Seats.Select(seat => seat.Id.Value).ToArray();
            var mine = runtime.Seat.Id.Value;
            var theirs = seats.First(seat => seat != mine);
            var client = wire.Connect(1, mine);
            var neighbour = wire.Connect(2, theirs);
            Assert.That(client.State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(neighbour.State, Is.EqualTo(PrivateSessionClientState.Seated));

            var offeredBefore = Commands(runtime);
            Assert.That(offeredBefore, Is.Not.Empty,
                "This seat must be offered something, or nothing below is about anything.");
            Assert.That(client.LatestProjection.AuthorizedCommands.Select(command => command.ToString()),
                Is.EqualTo(offeredBefore),
                "The projection the HUD is built from and the record the wire delivered are the " +
                "same seat's view of the same authority.");
            var drawnBefore = DrawnCardControls(root);
            var version = runtime.Host.Version;

            wire.LoseTheLink(1);
            Assert.That(wire.Session.IsPaused, Is.True);
            Assert.That(client.State, Is.EqualTo(PrivateSessionClientState.HostLost));

            // Nobody else may move the board this player will come back to.
            Assert.That(neighbour.TrySubmitCommand(
                neighbour.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            wire.Pump();
            Assert.That(neighbour.LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.SessionPaused));
            Assert.That(runtime.Host.Version, Is.EqualTo(version),
                "A paused session moved the match anyway.");

            runtime.RefreshAll();
            yield return null;
            Assert.That(Commands(runtime), Is.EqualTo(offeredBefore),
                "The board the away player left is the board still drawn.");
            Assert.That(DrawnCardControls(root), Is.EqualTo(drawnBefore));

            var returned = wire.Reconnect(1);
            Assert.That(wire.Client(returned).State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(wire.Client(returned).ConnectionEpoch,
                Is.EqualTo(PrivateSessionProtocol.InitialConnectionEpoch + 1),
                "The returning seat speaks under a rotated connection epoch.");
            Assert.That(wire.Session.IsPaused, Is.False);
            Assert.That(runtime.Host.Version, Is.EqualTo(version));

            var rebuilt = wire.Client(returned).LatestProjection;
            Assert.That(rebuilt.SeatId, Is.EqualTo(mine));
            Assert.That(rebuilt.AuthorizedCommands.Select(command => command.ToString()),
                Is.EqualTo(offeredBefore),
                "The returning player was rebuilt onto a different set of answers than they left.");
            Assert.That(rebuilt.Detail, Is.Not.Null,
                "A return is answered with the complete projection, not a fragment of one.");

            var ownedByOthers = runtime.Host.Projection.Cards
                .Where(card => !card.OwnerId.Equals(runtime.Seat.HeroId) &&
                    (card.State.Location == GuardsDigital.Core.Cards.CardLocation.Hand ||
                        card.State.Location == GuardsDigital.Core.Cards.CardLocation.Deck))
                .Select(card => card.Id.Value)
                .ToArray();
            Assert.That(ownedByOthers, Is.Not.Empty,
                "The official table deals other seats hidden cards; with none there is nothing " +
                "for this check to be about.");
            var delivered = rebuilt.OwnCards.Select(card => card.InstanceId)
                .Concat(rebuilt.PublicCards.Select(card => card.InstanceId))
                .ToArray();
            Assert.That(delivered.Intersect(ownedByOthers), Is.Empty,
                "The rebuilt projection carried another hero's hidden card.");
        }

        /// <summary>
        /// When the host is gone, every control this seat was offered is something it can no
        /// longer press, and the surface that drew them clears completely rather than leaving one
        /// behind.
        /// </summary>
        [UnityTest]
        public IEnumerator HostLoss_LeavesTheSeatWithNothingItCanPress()
        {
            UiPlayer.EnsurePlayerViewport();
            yield return LoadSeatSceneForATurnItMayEnd();
            var root = Root();
            var runtime = root.Runtime;
            yield return DriveUntilTheSeatHoldsARoundControl(runtime);
            runtime.RefreshAll();
            yield return null;
            yield return null;

            var live = runtime.RoundView.Model;
            var standing = RoundControls(root);
            Assert.That(standing, Is.Not.Empty,
                "The drive must reach a surface with controls on it, or the clearing below " +
                "would be about an empty screen.");

            var wire = SceneWire.Compose(runtime.Host, runtime.SessionIdentity);
            var client = wire.Connect(1, runtime.Seat.Id.Value);
            Assert.That(client.State, Is.EqualTo(PrivateSessionClientState.Seated));
            var offered = client.LatestProjection.AuthorizedCommands.ToArray();
            Assert.That(offered, Is.Not.Empty);

            wire.LoseTheLink(1);
            Assert.That(client.State, Is.EqualTo(PrivateSessionClientState.HostLost));
            Assert.That(client.RejectionReason, Is.EqualTo(SessionRejectionReason.None),
                "Nobody refused anything; the authority is simply not there.");
            foreach (var command in offered)
            {
                Assert.That(client.TrySubmitCommand(command, out _), Is.False,
                    "A control still drawn after host loss must be one the session refuses: " +
                    command + ".");
            }

            // The surface a terminal session has to draw is the one with nothing offered on it.
            runtime.RoundView.Render(new RoundActionViewModel(
                live.Version, live.RoundIndex, live.TurnIndex, live.Phase, live.Status,
                new TypedInvocationViewModel[0], null, null,
                new TypedInvocationViewModel[0], new TypedInvocationViewModel[0]));
            yield return null;
            yield return null;

            Assert.That(RoundControls(root), Is.Empty,
                "The action surface kept a control standing that nothing offers any more; it still " +
                "draws " + string.Join(", ", RoundControls(root)) + ".");
            Assert.That(UiPlayer.Descendants(root.Hud.RootElement)
                    .Count(element => element.name == "gd-end-turn-control" &&
                        UiPlayer.IsShown(element)), Is.Zero,
                "The End Turn banner outlived the offer that put it there.");
            Assert.That(Object.FindObjectsByType<ConfiguredMatchHudRenderer>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1),
                "Clearing the surface must not leave a second HUD behind.");
            Assert.That(runtime.RoundView.Model.Status, Is.Not.Empty,
                "The player still reads where the match stood; what they lose is the ability to act.");
        }

        /// <summary>
        /// Rebuilding the returned seat's surface leaves one of each control, no selection from
        /// before, and no growth in what the scene owns.
        /// </summary>
        [UnityTest]
        public IEnumerator RebuildingTheReturnedSeat_LeavesNoStaleSelectionOrDuplicateSurface()
        {
            UiPlayer.EnsurePlayerViewport();
            yield return LoadSeatScene(0);
            var root = Root();
            var runtime = root.Runtime;
            runtime.RefreshAll();
            yield return null;
            yield return null;

            var seat = runtime.Seat.Id;
            var selectable = runtime.HandsView.Model.ViewedHand.Cards.First();
            Assert.That(runtime.SelectCommitmentCard(selectable.Id), Is.True);
            runtime.RefreshAll();
            yield return null;
            Assert.That(runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId, Is.Not.Null,
                "The surface must hold a real selection for its rebuild to prove it drops one.");
            var selectorsBefore = Object.FindObjectsByType<PrimitiveHeroPointerSelector>(
                FindObjectsSortMode.None).Length;
            var highlightsBefore = root.OwnedHighlightNativeResourceCount;

            yield return ComposeForSeat(seat);
            root = Root();
            runtime = root.Runtime;
            runtime.RefreshAll();
            yield return null;
            yield return null;

            Assert.That(runtime.Seat.Id, Is.EqualTo(seat));
            Assert.That(runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId, Is.Null,
                "A rebuilt surface carries no selection from the one it replaced.");
            Assert.That(runtime.HandsView.Model.Hands
                    .Where(hand => !hand.HeroId.Equals(runtime.Seat.HeroId))
                    .SelectMany(hand => hand.Cards),
                Is.Empty,
                "A rebuilt surface carries no other player's hand.");
            Assert.That(Object.FindObjectsByType<ConfiguredMatchHudRenderer>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1),
                "One rebuilt surface, one HUD; two would be two sets of input handlers.");
            Assert.That(Object.FindObjectsByType<MatchSceneCompositionRoot>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<PrimitiveHeroPointerSelector>(
                    FindObjectsSortMode.None).Length,
                Is.EqualTo(selectorsBefore),
                "Rebuilding owns the same bounded input surfaces, not one more set.");
            Assert.That(root.OwnedHighlightNativeResourceCount, Is.EqualTo(highlightsBefore),
                "Rebuilding owns the same bounded highlight resources.");
            foreach (var name in new[]
                     {
                         "gd-narration", "gd-topbar", "gd-card-buttons", "gd-tie-breaker-rail-banner"
                     })
            {
                Assert.That(UiPlayer.Descendants(root.Hud.RootElement)
                        .Count(element => element.name == name), Is.EqualTo(1),
                    "Exactly one " + name + " survives the rebuild.");
            }
        }

        private static IEnumerable<string> Commands(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Host.ProjectionForSeat(runtime.Seat.Id).AuthorizedCommands
                .Select(command => command.ToString())
                .ToArray();

        private static IReadOnlyList<string> DrawnCardControls(MatchSceneCompositionRoot root) =>
            UiPlayer.Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-card-choice" && UiPlayer.IsShown(element))
                .Select(element => element.tooltip ?? string.Empty)
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();

        /// <summary>
        /// Every control the round action surface currently draws for this seat, wherever the
        /// renderer puts it.
        /// </summary>
        /// <remarks>
        /// The round view's own root is not the whole surface: milestone 8.30l moved the End Turn
        /// banner into the status bar's corner while <c>RoundActionUnityView</c> kept building and
        /// clearing it, so a check reading only the view's subtree would find an empty screen
        /// before the clear as well as after it - which is how this fixture's first run reported a
        /// drawn End Turn as no control at all.
        /// </remarks>
        private static IReadOnlyList<string> RoundControls(MatchSceneCompositionRoot root)
        {
            var own = new HashSet<VisualElement>(
                UiPlayer.Descendants(root.Runtime.RoundView.Root));
            return UiPlayer.Descendants(root.Hud.RootElement)
                .OfType<Button>()
                .Where(button => UiPlayer.IsShown(button) &&
                    (own.Contains(button) || button.name == "gd-end-turn-control"))
                .Select(button => button.name + ":" + button.text)
                .OrderBy(value => value, System.StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Drives every seat through its own client until the composed seat's own surface offers an
        /// End Turn outside an exclusive context, which is the state the clearing case needs to
        /// start from: a drawn control the player could press until the authority goes away.
        /// </summary>
        /// <remarks>
        /// The drawn control rather than the projected invocation alone, for milestone 8.30b's
        /// reason: an exclusive context leaves the corner empty even where the authority would
        /// accept an End Turn, and a drive that stopped on the model would hand this case a hidden
        /// control and prove nothing by clearing it.
        /// </remarks>
        private static IEnumerator DriveUntilTheSeatHoldsARoundControl(
            ConfiguredMatchSceneRuntime runtime)
        {
            var host = runtime.Host;
            var clients = host.Seats.Seats.ToDictionary(
                seat => seat.Id, seat => new SeatMatchClient(host.ClientForSeat(seat.Id)));

            for (var step = 0; step < DriveBudget; step++)
            {
                runtime.RefreshAll();
                if (runtime.RoundView.Model?.EndTurnInvocation != null &&
                    !runtime.RoundView.HasExclusiveDecision)
                {
                    yield return null;
                    yield return null;
                    yield break;
                }

                var advanced = false;
                foreach (var seat in host.Seats.Seats)
                {
                    var command = host.ProjectionForSeat(seat.Id).AuthorizedCommands.FirstOrDefault();
                    if (command != null)
                    {
                        advanced |= clients[seat.Id].Submit(command).Accepted;
                    }
                }

                Assert.That(advanced, Is.True,
                    "The drive stalled at step " + step + " with no round control standing; host " +
                    "version " + host.Version + ".");
                yield return null;
            }

            Assert.Fail("No round control stood on seat " + runtime.Seat.Id.Value + " within " +
                DriveBudget + " steps; host version " + host.Version + ".");
        }

        /// <summary>
        /// Loads the match composed for a seat the drive above will hand a turn it may end.
        /// </summary>
        /// <remarks>
        /// Which seat that is cannot be guessed - the actor is whoever the revealed Initiative order
        /// selects, and a fixed index composes a seat that may sit out the whole budget, which is
        /// exactly what a fixed index did here on this fixture's first run. The Director's own
        /// composition is loaded first and driven by precisely the policy the seat scene will be
        /// driven by, purely to read that seat's identity off it; the composed match then starts
        /// from the same initial state and follows the same path, because the authority is
        /// deterministic and the policy takes each seat's first authorized command every time.
        /// </remarks>
        private static IEnumerator LoadSeatSceneForATurnItMayEnd()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            yield return LoadComposedScene();
            yield return ComposeForSeat(FindSeatThatReachesATurnItMayEnd(Root().Runtime.Host));
        }

        /// <summary>
        /// Drives a throwaway host by the drive policy and returns the first seat whose own read
        /// model offers an End Turn outside an exclusive context.
        /// </summary>
        private static SeatId FindSeatThatReachesATurnItMayEnd(IMatchHost host)
        {
            var clients = host.Seats.Seats.ToDictionary(
                seat => seat.Id, seat => new SeatMatchClient(host.ClientForSeat(seat.Id)));

            for (var step = 0; step < DriveBudget; step++)
            {
                foreach (var seat in host.Seats.Seats)
                {
                    var model = RoundActionPresenter.CreateViewModel(host.ProjectionForSeat(seat.Id));
                    if (model.EndTurnInvocation != null && !DrawsAnExclusiveDecision(model))
                    {
                        return seat.Id;
                    }
                }

                var advanced = false;
                foreach (var seat in host.Seats.Seats)
                {
                    var command = host.ProjectionForSeat(seat.Id).AuthorizedCommands.FirstOrDefault();
                    if (command != null)
                    {
                        advanced |= clients[seat.Id].Submit(command).Accepted;
                    }
                }

                Assert.That(advanced, Is.True,
                    "The probe drive stalled at step " + step + ", host version " + host.Version +
                    ", with no seat holding an End Turn.");
            }

            Assert.Fail("No seat reached a turn it may end within " + DriveBudget +
                " steps; host version " + host.Version + ".");
            return default;
        }

        private static bool DrawsAnExclusiveDecision(RoundActionViewModel model)
        {
            var view = new RoundActionUnityView();
            view.Render(model);
            return view.HasExclusiveDecision;
        }

        private static IEnumerator LoadSeatScene(int seatIndex)
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            yield return LoadComposedScene();
            var seats = Root().Runtime.Host.Seats.Seats.Select(seat => seat.Id).ToArray();
            yield return ComposeForSeat(seats[seatIndex]);
        }

        private static IEnumerator ComposeForSeat(SeatId seat)
        {
            MatchSceneCompositionRoot.NextComposedSeat = seat;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            yield return LoadComposedScene();
            Assert.That(Root().Runtime.Seat, Is.Not.Null);
            Assert.That(Root().Runtime.Seat.Id, Is.EqualTo(seat));
        }

        private static IEnumerator LoadComposedScene()
        {
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }
    }
}
