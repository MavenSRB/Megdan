using System;
using System.Collections.Generic;
using System.Globalization;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Application
{
    /// <summary>Milestone 8.35a. Which side of the private session a lobby surface is showing.</summary>
    public enum PrivateLobbyRole
    {
        /// <summary>No session yet: the entry form, where an invitation is typed in.</summary>
        None = 0,

        /// <summary>The dedicated seatless host, observing the seats it serves.</summary>
        Host = 1,

        /// <summary>One invited player, holding one seat.</summary>
        Client = 2
    }

    /// <summary>
    /// Milestone 8.35a. Every control a lobby surface may offer. A bounded set on purpose: what a
    /// lobby can do is decided here, by the session's own state, rather than by whichever button a
    /// view happens to have drawn.
    /// </summary>
    public enum PrivateLobbyControl
    {
        None = 0,

        /// <summary>Entry form: become the dedicated seatless host of a new session.</summary>
        Host = 1,

        /// <summary>Entry form: join the typed session as the picked seat.</summary>
        Join = 2,

        /// <summary>This seat is ready to begin.</summary>
        Ready = 3,

        /// <summary>This seat is no longer ready.</summary>
        Unready = 4,

        /// <summary>Leave the session and return to the entry form.</summary>
        Leave = 5,

        /// <summary>Begin an authorized return to the seat this client already held.</summary>
        Reconnect = 6,

        /// <summary>
        /// THIRTEENTH amendment. Host only, before the match starts: spawn a real keyed test-driver
        /// client process onto every seat still Open, so a solo player can practice without six
        /// windows. QA tooling - explicitly not an AI opponent - offered only while a seat remains
        /// to fill.
        /// </summary>
        FillWithTestDrivers = 7
    }

    /// <summary>
    /// Milestone 8.35a. One row of the lobby's seat table, as words.
    /// </summary>
    /// <remarks>
    /// Every field is a reading of one <see cref="SeatRosterEntry"/>, which is public at a real
    /// table: who the seat is, which side it plays for, whether somebody is in it, and whether they
    /// are ready. Nothing owner-private has a field here to travel in.
    /// <para><see cref="IsTestDriverSeat"/> is the one exception, and deliberately not sourced from
    /// the roster: which seats a spawned test driver claimed is known only to the host process that
    /// spawned them (<c>PrivateLobbyDriver</c>'s own local list), so it never crosses the wire - the
    /// THIRTEENTH amendment's "no protocol change expected" holds because of exactly this. A
    /// connecting player's own lobby therefore reads an ordinary SEATED row for a bot-filled seat;
    /// only the host's own screen, where the fact originates, labels it.</para>
    /// </remarks>
    public sealed class PrivateLobbySeatRow
    {
        public PrivateLobbySeatRow(
            SeatId seat,
            string seatLabel,
            string heroLabel,
            string teamLabel,
            string occupancyLabel,
            string readinessLabel,
            bool isOwnSeat,
            bool isTestDriverSeat = false)
        {
            Seat = seat;
            SeatLabel = seatLabel ?? string.Empty;
            HeroLabel = heroLabel ?? string.Empty;
            TeamLabel = teamLabel ?? string.Empty;
            OccupancyLabel = occupancyLabel ?? string.Empty;
            ReadinessLabel = readinessLabel ?? string.Empty;
            IsOwnSeat = isOwnSeat;
            IsTestDriverSeat = isTestDriverSeat;
        }

        public SeatId Seat { get; }
        public string SeatLabel { get; }
        public string HeroLabel { get; }
        public string TeamLabel { get; }

        /// <summary>Open, seated, or away - in words, never as a colour.</summary>
        public string OccupancyLabel { get; }

        /// <summary>Ready or not ready - in words, never as a colour.</summary>
        public string ReadinessLabel { get; }

        /// <summary>
        /// Whether the host's own local record says this seat's connection is a spawned test
        /// driver rather than a real player. Host-only knowledge - see the class remarks.
        /// </summary>
        public bool IsTestDriverSeat { get; }

        /// <summary>Whether this is the row of the seat the surface's own client holds.</summary>
        public bool IsOwnSeat { get; }
    }

    /// <summary>
    /// Milestone 8.35a. The words a lobby says, in one place.
    /// </summary>
    /// <remarks>
    /// <para>Team identity, occupancy and readiness are written out rather than shown as a colour,
    /// because the block's third acceptance check says a player who cannot tell two colours apart
    /// must still be able to read their own team and everyone's status. A view is free to add
    /// colour on top of these; it may not replace them.</para>
    /// <para>Every mapping is exhaustive and throws on a value it does not name, so a state or a
    /// refusal reason added later cannot reach a player as a blank line. That is also what makes
    /// the fixture over these mappings non-vacuous: it enumerates the enum and requires bounded
    /// text for every member.</para>
    /// </remarks>
    public static class PrivateLobbyLabels
    {
        /// <summary>The most characters any single lobby line may carry to a player.</summary>
        public const int MaxLabelChars = 200;

        /// <summary>
        /// Milestone 8.35a1. Why the lobby will not host off loopback without a session key, in
        /// the words a player reads.
        /// </summary>
        /// <remarks>
        /// The decision itself is <see cref="PrivateSessionProtocol.BindRequiresSessionKey"/>, so
        /// the command line and this surface refuse the same binds; only the wording differs,
        /// because a lobby has a key field rather than a flag to name.
        /// </remarks>
        public const string UnkeyedPublicBindRefusal =
            "Hosting on any address but 127.0.0.1 needs a session key: without one the link " +
            "would not be encrypted.";

        public static string Team(TeamSide side)
        {
            switch (side)
            {
                case TeamSide.Red: return "RED TEAM";
                case TeamSide.Blue: return "BLUE TEAM";
                case TeamSide.None: return "NO TEAM";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(side), side, "A lobby row must be able to name its team in words.");
            }
        }

        public static string Occupancy(SeatOccupancyState occupancy)
        {
            switch (occupancy)
            {
                case SeatOccupancyState.Open: return "OPEN";
                case SeatOccupancyState.Claimed: return "SEATED";
                case SeatOccupancyState.Reserved: return "AWAY";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(occupancy), occupancy,
                        "A lobby row must be able to say whether a seat is taken.");
            }
        }

        public static string Readiness(bool isReady) => isReady ? "READY" : "NOT READY";

        /// <summary>Where a client's own connection stands, as one bounded line.</summary>
        public static string ConnectionState(PrivateSessionClientState state)
        {
            switch (state)
            {
                case PrivateSessionClientState.None: return "NOT CONNECTED";
                case PrivateSessionClientState.AwaitingChallenge: return "CONNECTING";
                case PrivateSessionClientState.AwaitingWelcome: return "CHECKING COMPATIBILITY";
                case PrivateSessionClientState.AwaitingSeat: return "CLAIMING SEAT";
                case PrivateSessionClientState.Seated: return "SEATED";
                case PrivateSessionClientState.Rejected: return "REFUSED";
                case PrivateSessionClientState.Disconnected: return "DISCONNECTED";
                case PrivateSessionClientState.Faulted: return "CONNECTION FAULT";
                case PrivateSessionClientState.Resynchronizing: return "RESYNCHRONIZING";
                case PrivateSessionClientState.HostLost: return "HOST LOST";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(state), state,
                        "A lobby must be able to say where a connection stands.");
            }
        }

        /// <summary>
        /// Why the host said no, in the player's words. Bounded, and about this connection only -
        /// no other seat's state, no address, and nothing about the session beyond what the refusal
        /// itself is.
        /// </summary>
        public static string Refusal(SessionRejectionReason reason)
        {
            switch (reason)
            {
                case SessionRejectionReason.None: return string.Empty;
                case SessionRejectionReason.ProtocolFamilyMismatch:
                    return "This build speaks a different private-session protocol.";
                case SessionRejectionReason.ProtocolVersionMismatch:
                    return "This build speaks a different protocol version.";
                case SessionRejectionReason.BuildIdentityMismatch:
                    return "The host is running a different build.";
                case SessionRejectionReason.SnapshotSchemaMismatch:
                    return "The host is running a different save schema.";
                case SessionRejectionReason.MapRevisionMismatch:
                    return "The host is running a different map revision.";
                case SessionRejectionReason.ContentDigestMismatch:
                    return "The host is running different registered content.";
                case SessionRejectionReason.SessionKeyMismatch:
                    return "The session key did not match the host's.";
                case SessionRejectionReason.SeatAlreadyClaimed:
                    return "That seat is already taken. Pick another and join again.";
                case SessionRejectionReason.SeatReserved:
                    return "That seat is held for the player who dropped out of it.";
                case SessionRejectionReason.UnknownSeat:
                    return "There is no seat of that name in this match.";
                case SessionRejectionReason.SeatChangeRefused:
                    return "A seat is fixed for the session; leave and join again to change it.";
                case SessionRejectionReason.SessionFull:
                    return "Every seat is taken.";
                case SessionRejectionReason.TooManyConnections:
                    return "The host is already holding as many connections as it will.";
                case SessionRejectionReason.MalformedFrame:
                    return "The host refused a message it could not read.";
                case SessionRejectionReason.OversizedFrame:
                    return "The host refused a message larger than it accepts.";
                case SessionRejectionReason.UnexpectedMessage:
                    return "The host did not expect that message at this point.";
                case SessionRejectionReason.HandshakeTimeout:
                    return "The join took too long and the host closed it.";
                case SessionRejectionReason.ProtocolViolation:
                    return "The connection broke the protocol and was closed.";
                case SessionRejectionReason.EnvelopeMismatch:
                    return "A message named the wrong session, connection, or order.";
                case SessionRejectionReason.SealViolation:
                    return "A message failed its seal check.";
                case SessionRejectionReason.ReconnectNotAuthorized:
                    return "This seat's return was not authorized.";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(reason), reason,
                        "Every refusal a player can receive must have words of its own.");
            }
        }

        /// <summary>
        /// What a control says on its face. Named here rather than in a view so that every surface
        /// offering a control offers the same words, and so the mapping is exhaustive against the
        /// enum in both test tiers.
        /// </summary>
        public static string Control(PrivateLobbyControl control)
        {
            switch (control)
            {
                case PrivateLobbyControl.Host: return "HOST";
                case PrivateLobbyControl.Join: return "JOIN";
                case PrivateLobbyControl.Ready: return "READY";
                case PrivateLobbyControl.Unready: return "CANCEL READY";
                case PrivateLobbyControl.Leave: return "LEAVE";
                case PrivateLobbyControl.Reconnect: return "RECONNECT";
                case PrivateLobbyControl.FillWithTestDrivers: return "FILL WITH TEST DRIVERS";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(control), control, "Every offered control must have words on it.");
            }
        }

        /// <summary>Trims a line to the lobby's own bound, so no surface can grow one without limit.</summary>
        public static string Bound(string text)
        {
            text = text ?? string.Empty;
            return text.Length <= MaxLabelChars ? text : text.Substring(0, MaxLabelChars);
        }

        /// <summary>
        /// A seat or hero id as a lobby prints it. The identity itself, upper-cased and otherwise
        /// untouched: a primitive lobby names the seats the build names, and inventing display
        /// names for them would be presentation this milestone's non-goals rule out.
        /// </summary>
        public static string Identifier(string value) =>
            (value ?? string.Empty).ToUpperInvariant();
    }

    /// <summary>
    /// Milestone 8.35a. Everything a primitive private lobby shows and everything it may do,
    /// derived from the session contracts and from nothing else.
    /// </summary>
    /// <remarks>
    /// <para>This is the block's fourth acceptance check made structural. The model is a
    /// <em>reading</em>: it is built from a <see cref="PrivateSessionClient"/> or a
    /// <see cref="PrivateSessionHost"/>, it holds no reference to either afterwards, and it names
    /// no command, submission or session type through which a lobby could move the match. A surface
    /// drawn from it cannot assign match ownership, because there is nothing here to assign it
    /// with; who owns which seat is the host's answer, arriving in the roster.</para>
    /// <para>It lives beside the session contracts rather than in a view assembly for the same
    /// reason <see cref="SeatProjection"/> does: what a surface may see is decided where the state
    /// is, and a surface that decided for itself would be deciding privacy and authority. It names
    /// no engine type, so both tiers of the test suite read exactly what a player will.</para>
    /// </remarks>
    public sealed class PrivateLobbyModel
    {
        private static readonly ISet<SeatId> EmptyTestDriverSeats = new HashSet<SeatId>();

        private PrivateLobbyModel(
            PrivateLobbyRole role,
            bool isShown,
            string statusLabel,
            string feedbackLabel,
            string sessionLabel,
            IReadOnlyList<PrivateLobbySeatRow> seats,
            IReadOnlyList<PrivateLobbyControl> controls,
            bool hasMatchStarted)
        {
            Role = role;
            IsShown = isShown;
            StatusLabel = PrivateLobbyLabels.Bound(statusLabel);
            FeedbackLabel = PrivateLobbyLabels.Bound(feedbackLabel);
            SessionLabel = PrivateLobbyLabels.Bound(sessionLabel);
            Seats = seats ?? Array.Empty<PrivateLobbySeatRow>();
            Controls = controls ?? Array.Empty<PrivateLobbyControl>();
            HasMatchStarted = hasMatchStarted;
        }

        public PrivateLobbyRole Role { get; }

        /// <summary>
        /// Whether the lobby is on screen at all. It hands off when a started match is being
        /// played and comes back when the session reaches something a player has to answer, which
        /// is what keeps a control from standing on a surface nothing offers any more.
        /// </summary>
        public bool IsShown { get; }

        /// <summary>Where this surface stands, in one bounded line.</summary>
        public string StatusLabel { get; }

        /// <summary>The last thing that went wrong, in one bounded line; empty when nothing has.</summary>
        public string FeedbackLabel { get; }

        /// <summary>The host's opaque session correlation, for support; never an address or a key.</summary>
        public string SessionLabel { get; }

        public IReadOnlyList<PrivateLobbySeatRow> Seats { get; }

        /// <summary>Exactly the controls this state offers, in the order a surface should draw them.</summary>
        public IReadOnlyList<PrivateLobbyControl> Controls { get; }

        public bool HasMatchStarted { get; }

        public bool Offers(PrivateLobbyControl control)
        {
            foreach (var candidate in Controls)
            {
                if (candidate == control)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The entry form: the six seats this build knows about, offered as picks, and whatever the
        /// last attempt has to say.
        /// </summary>
        /// <remarks>
        /// The seats come from the local match binding rather than from a host, because there is no
        /// host yet - every invited player runs the same build, so the same six seats exist on
        /// every one of them. Their occupancy is honestly unknown until a session answers.
        /// </remarks>
        public static PrivateLobbyModel Entry(IReadOnlyList<SeatRosterEntry> seats, string feedback)
        {
            var rows = new List<PrivateLobbySeatRow>();
            foreach (var entry in seats ?? Array.Empty<SeatRosterEntry>())
            {
                rows.Add(new PrivateLobbySeatRow(
                    entry.Seat,
                    PrivateLobbyLabels.Identifier(entry.Seat.Value),
                    PrivateLobbyLabels.Identifier(entry.Hero.Value),
                    PrivateLobbyLabels.Team(entry.TeamSide),
                    "NOT CONNECTED",
                    string.Empty,
                    false));
            }

            return new PrivateLobbyModel(
                PrivateLobbyRole.None,
                true,
                "NOT CONNECTED",
                feedback,
                string.Empty,
                rows,
                new[] { PrivateLobbyControl.Host, PrivateLobbyControl.Join },
                false);
        }

        /// <summary>One invited player's lobby, read off that player's own session client.</summary>
        public static PrivateLobbyModel ForClient(PrivateSessionClient client)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var rows = new List<PrivateLobbySeatRow>();
            foreach (var entry in client.Roster)
            {
                rows.Add(new PrivateLobbySeatRow(
                    entry.Seat,
                    PrivateLobbyLabels.Identifier(entry.Seat.Value),
                    PrivateLobbyLabels.Identifier(entry.Hero.Value),
                    PrivateLobbyLabels.Team(entry.TeamSide),
                    PrivateLobbyLabels.Occupancy(entry.Occupancy),
                    PrivateLobbyLabels.Readiness(entry.IsReady),
                    entry.Seat.Equals(client.TargetSeat)));
            }

            var status = PrivateLobbyLabels.ConnectionState(client.State);
            if (client.IsReconnecting)
            {
                status = "RECONNECTING - " + status;
            }

            var refusal = PrivateLobbyLabels.Refusal(client.RejectionReason);
            var feedback = refusal.Length == 0
                ? string.Empty
                : client.RejectionDetail.Length == 0
                    ? refusal
                    : refusal + " (" + client.RejectionDetail + ")";

            return new PrivateLobbyModel(
                PrivateLobbyRole.Client,
                IsLobbyShownFor(client),
                status,
                feedback,
                client.SessionCorrelation,
                rows,
                ControlsFor(client),
                client.HasMatchStarted);
        }

        /// <summary>
        /// The dedicated seatless host's lobby: the seats it serves, and the one control that lets
        /// it fill empty ones with test drivers before the match starts.
        /// </summary>
        /// <remarks>
        /// The host holds no seat by the topology's own rule, so it is never offered readiness -
        /// there is nothing of its own for it to be ready with - and its panel stands after the
        /// transition, because a seatless process has no match surface of its own to hand off to.
        /// </remarks>
        /// <param name="testDriverSeats">
        /// THIRTEENTH amendment. Which seats this host's own spawned test drivers claimed, read
        /// from the host process's local record rather than the roster - see
        /// <see cref="PrivateLobbySeatRow.IsTestDriverSeat"/>. Optional and defaults to none, so
        /// every existing caller (and fixture) is unaffected.
        /// </param>
        public static PrivateLobbyModel ForHost(
            PrivateSessionHost host, string feedback,
            ISet<SeatId> testDriverSeats = null)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            testDriverSeats = testDriverSeats ?? EmptyTestDriverSeats;

            var rows = new List<PrivateLobbySeatRow>();
            var anySeatOpen = false;
            foreach (var entry in host.Roster)
            {
                if (entry.Occupancy == SeatOccupancyState.Open)
                {
                    anySeatOpen = true;
                }

                rows.Add(new PrivateLobbySeatRow(
                    entry.Seat,
                    PrivateLobbyLabels.Identifier(entry.Seat.Value),
                    PrivateLobbyLabels.Identifier(entry.Hero.Value),
                    PrivateLobbyLabels.Team(entry.TeamSide),
                    PrivateLobbyLabels.Occupancy(entry.Occupancy),
                    PrivateLobbyLabels.Readiness(entry.IsReady),
                    false,
                    testDriverSeats.Contains(entry.Seat)));
            }

            var status = host.HasMatchStarted
                ? "HOSTING - MATCH UNDER WAY"
                : "HOSTING - " +
                    host.ClaimedSeatCount.ToString(CultureInfo.InvariantCulture) + "/" +
                    host.TotalSeats.ToString(CultureInfo.InvariantCulture) + " SEATED, " +
                    host.ReadySeatCount.ToString(CultureInfo.InvariantCulture) + "/" +
                    host.TotalSeats.ToString(CultureInfo.InvariantCulture) + " READY";

            var controls = new List<PrivateLobbyControl>();
            if (!host.HasMatchStarted && anySeatOpen)
            {
                controls.Add(PrivateLobbyControl.FillWithTestDrivers);
            }

            controls.Add(PrivateLobbyControl.Leave);

            return new PrivateLobbyModel(
                PrivateLobbyRole.Host,
                true,
                status,
                feedback,
                host.Identity.Correlation,
                rows,
                controls,
                host.HasMatchStarted);
        }

        /// <summary>
        /// The lobby stands aside exactly while a started match is being played, and returns the
        /// moment the session is in a state the player has to answer.
        /// </summary>
        private static bool IsLobbyShownFor(PrivateSessionClient client) =>
            !client.HasMatchStarted ||
            (client.State != PrivateSessionClientState.Seated &&
                client.State != PrivateSessionClientState.Resynchronizing);

        /// <summary>
        /// What this client may press, decided by the session's own state.
        /// </summary>
        /// <remarks>
        /// Host loss is the shape worth reading twice: it offers a return where the session says
        /// one is authorized, it offers leaving, and it offers nothing else at all - which is
        /// milestone 8.34's rule that a terminal surface keeps no control a player could still
        /// press against a host that is gone.
        /// </remarks>
        private static IReadOnlyList<PrivateLobbyControl> ControlsFor(PrivateSessionClient client)
        {
            var controls = new List<PrivateLobbyControl>();
            switch (client.State)
            {
                case PrivateSessionClientState.Seated:
                    if (!client.HasMatchStarted)
                    {
                        controls.Add(client.IsReady
                            ? PrivateLobbyControl.Unready
                            : PrivateLobbyControl.Ready);
                    }

                    break;

                case PrivateSessionClientState.HostLost:
                case PrivateSessionClientState.Disconnected:
                case PrivateSessionClientState.Rejected:
                case PrivateSessionClientState.Faulted:
                    if (client.CanAttemptReconnect)
                    {
                        controls.Add(PrivateLobbyControl.Reconnect);
                    }

                    break;
            }

            controls.Add(PrivateLobbyControl.Leave);
            return controls;
        }
    }
}
