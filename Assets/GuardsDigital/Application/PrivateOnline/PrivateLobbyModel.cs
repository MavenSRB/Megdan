using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Application
{
    /// <summary>Milestone 8.35a. Which side of the private session a lobby surface is showing.</summary>
    public enum PrivateLobbyRole
    {
        /// <summary>No session yet: the entry form, where an invitation is typed in.</summary>
        None = 0,

        /// <summary>The host surface, including the host's own playing client when present.</summary>
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

        /// <summary>Entry form: host a new session and join it as a player.</summary>
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
        FillWithTestDrivers = 7,

        /// <summary>
        /// Milestone 8.35z37. Seated client only, while rulebook p.22's pick order is on
        /// this seat: take the named hero.
        /// </summary>
        ChooseHero = 8,

        /// <summary>
        /// Milestone 8.35z37. Entry form, host side only: which of rulebook pp.5-6's four
        /// printed setups this session will play.
        /// </summary>
        /// <remarks>
        /// <b>Offered before hosting and never after, and that is a rule rather than a layout
        /// choice.</b> A session's seats are counted from the format, and every connection, epoch
        /// and reconnect capability the host holds is keyed by seat - so a format changed once
        /// anybody is seated would move the seats out from under them. It is also already refused at
        /// the wire: <c>PrivateSessionIdentity</c> rejects a peer on <c>MatchFormatMismatch</c>, so a
        /// host that changed format mid-lobby would refuse every client it already had.
        /// </remarks>
        SetMatchFormat = 9,

        /// <summary>
        /// 8.35z54. Entry form, host side: play the printed FOUR-player setup.
        /// </summary>
        /// <remarks>
        /// <para><b>Named buttons replace the cycling one, and the reason is measured rather than
        /// aesthetic.</b> The user's own host log from the failed session is six consecutive
        /// <c>LOBBY_FORMAT</c> lines and no <c>LOBBY_HOST</c> at all - they pressed one SETUP button
        /// six times hunting for a setup and gave up without hosting. A cycle makes the four printed
        /// options invisible and makes reaching one a guess; four buttons make the whole choice
        /// readable at once, which is what "i need 2v2 and 3v3 buttons" asks for.</para>
        /// <para>Team size and game length are SEPARATE controls because they are separate printed
        /// decisions - pp.5-6 print four setups as two crossed pairs - and a player who wants the
        /// long 3v3 should not have to know which position it holds in somebody's cycle.</para>
        /// </remarks>
        ChooseTwoVsTwo = 10,

        /// <summary>8.35z54. Entry form, host side: play the printed SIX-player setup.</summary>
        ChooseThreeVsThree = 11,

        /// <summary>8.35z54. Entry form, host side: the SHORT (quick) game of that size.</summary>
        ChooseShortGame = 12,

        /// <summary>8.35z54. Entry form, host side: the LONG game of that size.</summary>
        ChooseLongGame = 13,

        /// <summary>
        /// 8.35z54. Seated client, after everyone is ready and before this seat has picked:
        /// take the seat this player pressed.
        /// </summary>
        /// <remarks>
        /// The player-side half of the spec - "they join the server, see open seats, they pick a
        /// seat". It is offered only to a connection that is IN the session and holds no seat yet,
        /// which is <see cref="PrivateSessionClientState.Admitted"/>; before this milestone that
        /// state did not exist and the seat had to be chosen on the entry form, blind.
        /// </remarks>
        ClaimSeat = 14,

        /// <summary>
        /// 8.35z54. Host only, before the transition: step the lobby back by one.
        /// </summary>
        /// <remarks>
        /// <para>The user's stuck-state escape hatch - "have an undo button there, which is director
        /// undo, if we get stuck we can get out". It rewinds the most recent lobby step: the last
        /// hero pick if any has been made, otherwise the readiness that opened the pick phase.</para>
        /// <para><b>It cannot rewind into a started match, and that bound is structural rather than
        /// a check on a button.</b> The transition out of the lobby is one-way by milestone 8.35a's
        /// rule - a later disconnect reserves a seat and pauses rather than reopening the lobby - so
        /// the control is not offered once the match has begun and the host refuses it by name if a
        /// stale surface presses it anyway.</para>
        /// </remarks>
        HostUndo = 15,

        /// <summary>
        /// Milestone 8.107. Host only, after the match has started, when the host's own client
        /// never took a seat: rewind the match by one accepted command, exactly as a playing host's
        /// <c>DIRECTOR</c> drawer does, and republish every seat's projection.
        /// </summary>
        /// <remarks>
        /// A seated host already has this in its match HUD's drawer. A host that is not playing has
        /// no match HUD to hold that drawer, so these two controls are the same tool placed on the
        /// only screen it has.
        /// </remarks>
        DirectorUndo = 16,

        /// <summary>
        /// Milestone 8.107. The non-playing host's counterpart of the drawer's RESTART.
        /// </summary>
        DirectorRestart = 17
    }

    /// <summary>
    /// Milestone 8.35z37. One hero on the selection screen: what it is called, and whether
    /// it can still be taken.
    /// </summary>
    public sealed class PrivateLobbyHeroOption
    {
        public PrivateLobbyHeroOption(
            string heroIdValue,
            string displayName,
            HeroComplexity complexity,
            HeroRole roles,
            bool isAvailable,
            string unavailableReason)
        {
            HeroIdValue = heroIdValue ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Complexity = complexity;
            Roles = roles;
            IsAvailable = isAvailable;
            UnavailableReason = unavailableReason ?? string.Empty;
        }

        public string HeroIdValue { get; }
        public string DisplayName { get; }
        public HeroComplexity Complexity { get; }
        public HeroRole Roles { get; }

        /// <summary>Whether this hero may still be picked - false once anybody has taken it.</summary>
        public bool IsAvailable { get; }

        /// <summary>
        /// Why it cannot be taken, in words; empty when it can. Never a colour and never a bare
        /// greying - the one-copy rule has a reason and a player is told it.
        /// </summary>
        public string UnavailableReason { get; }
    }

    /// <summary>
    /// Milestone 8.35z37. Rulebook p.22's All Pick as a surface draws it: whose turn it is,
    /// and what is left to take.
    /// </summary>
    /// <remarks>
    /// <para>Derived on both sides from the same two published facts - the roster's per-row
    /// <see cref="SeatRosterEntry.HasChosenHero"/> and the coin - through
    /// <c>HeroSelectionStage.PickOrderFor</c>, which is the one implementation of the printed order.
    /// The host could read its own stage instead and deliberately does not: two derivations of one
    /// rule is how a screen comes to show a turn the host would refuse.</para>
    /// <para>It shows the order; it does not enforce it. Every refusal is the host's, arrives by
    /// name, and is what a player is actually told.</para>
    /// </remarks>
    public sealed class PrivateLobbySelection
    {
        public static readonly PrivateLobbySelection NotRunning = new PrivateLobbySelection(
            false, false, string.Empty, string.Empty, Array.Empty<PrivateLobbyHeroOption>());

        public PrivateLobbySelection(
            bool isRunning,
            bool isOwnPick,
            string pickingSeatLabel,
            string statusLabel,
            IReadOnlyList<PrivateLobbyHeroOption> heroes)
        {
            IsRunning = isRunning;
            IsOwnPick = isOwnPick;
            PickingSeatLabel = PrivateLobbyLabels.Bound(pickingSeatLabel);
            StatusLabel = PrivateLobbyLabels.Bound(statusLabel);
            Heroes = heroes ?? Array.Empty<PrivateLobbyHeroOption>();
        }

        /// <summary>Whether a selection is under way at all - false once every seat has picked.</summary>
        public bool IsRunning { get; }

        /// <summary>Whether the order is on the seat this surface's own client holds.</summary>
        public bool IsOwnPick { get; }

        /// <summary>The seat the order is on, as words.</summary>
        public string PickingSeatLabel { get; }

        /// <summary>One bounded line saying whose pick it is.</summary>
        public string StatusLabel { get; }

        /// <summary>Every registered hero, available or greyed with its reason.</summary>
        public IReadOnlyList<PrivateLobbyHeroOption> Heroes { get; }
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
                // 8.35z54. In the session, holding no seat yet - which is a whole step of
                // the flow now rather than an instant inside the handshake, so it has words.
                case PrivateSessionClientState.Admitted: return "PICK YOUR SEAT";
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
                case SessionRejectionReason.MatchFormatMismatch:
                    return "The host is playing a different match format.";
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
                // Milestone 8.35z37. The generic line; the host's reject DETAIL carries the
                // specific one from HeroSelectionLabels.Refusal, which a surface shows beside it.
                // Both exist because this mapping must answer for a reason arriving with no detail
                // at all, and "blank" is what this whole exhaustive switch is here to prevent.
                case SessionRejectionReason.HeroSelectionRefused:
                    return "That hero pick was not accepted.";
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
                // 8.35z54. The user's own word for what this does. It spawns exactly the
                // same keyed seat drivers it always has; what changed is that they are what the
                // spec calls bots, and calling them "test drivers" on a playtest screen described
                // the implementation to somebody who only wants the empty chairs filled.
                case PrivateLobbyControl.FillWithTestDrivers: return "FILL WITH BOTS";
                case PrivateLobbyControl.ChooseHero: return "PICK HERO";
                case PrivateLobbyControl.SetMatchFormat: return "SETUP";
                case PrivateLobbyControl.ChooseTwoVsTwo: return "2V2";
                case PrivateLobbyControl.ChooseThreeVsThree: return "3V3";
                case PrivateLobbyControl.ChooseShortGame: return "SHORT GAME";
                case PrivateLobbyControl.ChooseLongGame: return "LONG GAME";
                case PrivateLobbyControl.ClaimSeat: return "TAKE THIS SEAT";
                case PrivateLobbyControl.HostUndo: return "UNDO";
                case PrivateLobbyControl.DirectorUndo: return "DIRECTOR UNDO";
                case PrivateLobbyControl.DirectorRestart: return "DIRECTOR RESTART";
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

        /// <summary>
        /// Milestone 8.108. What a lobby calls a seat: its player's name when they gave one, else
        /// the seat's own identifier, as before.
        /// </summary>
        public static string SeatName(SeatRosterEntry entry)
        {
            if (entry == null) return string.Empty;
            return entry.PlayerName.Length > 0 ? entry.PlayerName : Identifier(entry.Seat.Value);
        }
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
            bool hasMatchStarted,
            PrivateLobbySelection selection = null,
            string matchFormatLabel = null,
            bool isHostConsole = false)
        {
            IsHostConsole = isHostConsole;
            Role = role;
            IsShown = isShown;
            StatusLabel = PrivateLobbyLabels.Bound(statusLabel);
            FeedbackLabel = PrivateLobbyLabels.Bound(feedbackLabel);
            SessionLabel = PrivateLobbyLabels.Bound(sessionLabel);
            Seats = seats ?? Array.Empty<PrivateLobbySeatRow>();
            Controls = controls ?? Array.Empty<PrivateLobbyControl>();
            HasMatchStarted = hasMatchStarted;
            Selection = selection ?? PrivateLobbySelection.NotRunning;
            MatchFormatLabel = PrivateLobbyLabels.Bound(matchFormatLabel ?? string.Empty);
        }

        /// <summary>
        /// Milestone 8.35z37. Rulebook p.22's All Pick as this surface should draw it.
        /// <see cref="PrivateLobbySelection.NotRunning"/> whenever there is no selection to show.
        /// </summary>
        public PrivateLobbySelection Selection { get; }

        /// <summary>
        /// Milestone 8.35z37. Which of pp.5-6's printed setups this session plays, as words -
        /// the entry form's format control shows it, and a joined client reads what the host chose.
        /// </summary>
        public string MatchFormatLabel { get; }

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

        /// <summary>
        /// Milestone 8.107. The match has started and this surface is a host whose own client holds
        /// no seat: the lobby panel is that host's whole screen for the match, not an overlay on a
        /// match HUD, so it stays drawn as a full screen.
        /// </summary>
        public bool IsHostConsole { get; }

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
        /// <param name="matchFormatLabel">
        /// Milestone 8.35z37. Which of pp.5-6's printed setups this process would host, as
        /// words. Optional so every existing caller is unaffected; when it is given, the entry form
        /// offers <see cref="PrivateLobbyControl.SetMatchFormat"/> beside HOST and JOIN.
        /// </param>
        public static PrivateLobbyModel Entry(
            IReadOnlyList<SeatRosterEntry> seats, string feedback, string matchFormatLabel = null)
        {
            var rows = new List<PrivateLobbySeatRow>();
            foreach (var entry in seats ?? Array.Empty<SeatRosterEntry>())
            {
                rows.Add(new PrivateLobbySeatRow(
                    entry.Seat,
                    PrivateLobbyLabels.Identifier(entry.Seat.Value),
                    string.Empty,
                    PrivateLobbyLabels.Team(entry.TeamSide),
                    "NOT CONNECTED",
                    string.Empty,
                    false));
            }

            // Milestone 8.35z37. The format control lives HERE and nowhere else: before a
            // listener exists, before a seat is claimed, and therefore before changing it could
            // move a seat out from under anybody. See PrivateLobbyControl.SetMatchFormat.
            var controls = new List<PrivateLobbyControl>
            {
                PrivateLobbyControl.Host, PrivateLobbyControl.Join
            };
            if (!string.IsNullOrEmpty(matchFormatLabel))
            {
                // 8.35z54. The four printed setups as TWO named choices, team size and game
                // length, put ahead of HOST because they are decided before hosting: a session's
                // seats are counted from the format, so the format has to be settled before a
                // listener exists. That was already the rule (see SetMatchFormat's remarks); what
                // changes is that the choice is now readable instead of being a cycle.
                controls.Insert(0, PrivateLobbyControl.ChooseTwoVsTwo);
                controls.Insert(1, PrivateLobbyControl.ChooseThreeVsThree);
                controls.Insert(2, PrivateLobbyControl.ChooseShortGame);
                controls.Insert(3, PrivateLobbyControl.ChooseLongGame);
            }

            return new PrivateLobbyModel(
                PrivateLobbyRole.None,
                true,
                "NOT CONNECTED",
                feedback,
                string.Empty,
                rows,
                controls,
                false,
                PrivateLobbySelection.NotRunning,
                matchFormatLabel);
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
                    PrivateLobbyLabels.SeatName(entry),
                    entry.HasChosenHero ? PrivateLobbyLabels.Identifier(entry.Hero.Value) : string.Empty,
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

            var selection = client.HasMatchStarted
                ? PrivateLobbySelection.NotRunning
                : SelectionFrom(client.Roster, client.FirstPickingTeam, client.TargetSeat);

            return new PrivateLobbyModel(
                PrivateLobbyRole.Client,
                IsLobbyShownFor(client),
                status,
                feedback,
                client.SessionCorrelation,
                rows,
                ControlsFor(client, selection),
                client.HasMatchStarted,
                selection);
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
                    PrivateLobbyLabels.SeatName(entry),
                    entry.HasChosenHero ? PrivateLobbyLabels.Identifier(entry.Hero.Value) : string.Empty,
                    PrivateLobbyLabels.Team(entry.TeamSide),
                    PrivateLobbyLabels.Occupancy(entry.Occupancy),
                    PrivateLobbyLabels.Readiness(entry.IsReady),
                    false,
                    testDriverSeats.Contains(entry.Seat)));
            }

            // Milestone 8.35z37. Derived from the roster the host BROADCASTS rather than
            // from its own stage, so the host's screen and every client's screen are the same
            // reading of p.22 - a host drawing from a private source is how two screens come to
            // disagree about whose turn it is.
            var selection = host.HasMatchStarted
                ? PrivateLobbySelection.NotRunning
                : SelectionFrom(host.Roster, host.Selection.FirstPickingTeam, default);

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

            // 8.35z54. The stuck-state escape hatch, host-only and lobby-only. Offered
            // while there is a lobby step to take back and never once the match has begun - the
            // transition is one-way, so an undo that could cross it would be undoing a match rather
            // than a lobby. `CanUndoLobbyStep` is the host's own answer, so the button stands
            // exactly where the host would honour it.
            if (host.CanUndoLobbyStep)
            {
                controls.Add(PrivateLobbyControl.HostUndo);
            }

            controls.Add(PrivateLobbyControl.Leave);

            return new PrivateLobbyModel(
                PrivateLobbyRole.Host,
                true,
                status,
                feedback,
                host.SessionCorrelation,
                rows,
                controls,
                host.HasMatchStarted,
                selection,
                host.Identity.MatchFormatId);
        }

        /// <summary>
        /// Milestone 8.36g1. The shared client's hosting view: owner-scoped seat controls from its
        /// ordinary protocol client, plus the bounded lobby controls that belong only to the host.
        /// </summary>
        public static PrivateLobbyModel ForHostPlayer(
            PrivateSessionHost host,
            PrivateSessionClient client,
            string hostFeedback,
            ISet<SeatId> testDriverSeats = null)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (client == null) throw new ArgumentNullException(nameof(client));

            // Milestone 8.107. A hosting client that never took a seat is never sent MatchStarted
            // (the host announces it to seated connections only), so its own reading would keep
            // showing a lobby for a match other people are already playing. The host's own fact
            // decides instead, and the screen becomes the non-playing host's console.
            if (host.HasMatchStarted && client.State == PrivateSessionClientState.Admitted)
            {
                return ForHostConsole(host, hostFeedback, testDriverSeats);
            }

            var owner = ForClient(client);
            var controls = new List<PrivateLobbyControl>(
                owner.Controls.Where(value => value != PrivateLobbyControl.Leave));
            var anySeatOpen = host.Roster.Any(value => value.Occupancy == SeatOccupancyState.Open);
            if (!host.HasMatchStarted && client.State == PrivateSessionClientState.Seated && anySeatOpen)
            {
                controls.Add(PrivateLobbyControl.FillWithTestDrivers);
            }
            if (host.CanUndoLobbyStep)
            {
                controls.Add(PrivateLobbyControl.HostUndo);
            }
            controls.Add(PrivateLobbyControl.Leave);

            testDriverSeats = testDriverSeats ?? EmptyTestDriverSeats;
            var rows = owner.Seats.Select(row => new PrivateLobbySeatRow(
                row.Seat,
                row.SeatLabel,
                row.HeroLabel,
                row.TeamLabel,
                row.OccupancyLabel,
                row.ReadinessLabel,
                row.IsOwnSeat,
                testDriverSeats.Contains(row.Seat))).ToArray();

            return new PrivateLobbyModel(
                PrivateLobbyRole.Host,
                owner.IsShown,
                "HOSTING AS PLAYER - " + owner.StatusLabel,
                string.IsNullOrEmpty(hostFeedback) ? owner.FeedbackLabel : hostFeedback,
                host.SessionCorrelation,
                rows,
                controls,
                owner.HasMatchStarted,
                owner.Selection,
                host.Identity.MatchFormatId);
        }

        /// <summary>
        /// Milestone 8.107. The screen of a host that is running a started match without playing in
        /// it: the seats, and the Director's undo and restart beside LEAVE.
        /// </summary>
        private static PrivateLobbyModel ForHostConsole(
            PrivateSessionHost host, string hostFeedback, ISet<SeatId> testDriverSeats)
        {
            var plain = ForHost(host, hostFeedback, testDriverSeats);
            return new PrivateLobbyModel(
                PrivateLobbyRole.Host,
                true,
                "HOSTING - MATCH UNDER WAY - YOU ARE NOT PLAYING",
                plain.FeedbackLabel,
                plain.SessionLabel,
                plain.Seats,
                new[]
                {
                    PrivateLobbyControl.DirectorUndo,
                    PrivateLobbyControl.DirectorRestart,
                    PrivateLobbyControl.Leave
                },
                true,
                PrivateLobbySelection.NotRunning,
                host.Identity.MatchFormatId,
                isHostConsole: true);
        }

        /// <summary>
        /// Milestone 8.35z37. Rulebook p.22's All Pick, derived from the two published
        /// facts a roster carries: which rows have chosen, and the coin.
        /// </summary>
        /// <remarks>
        /// <para>The available list is the live registration minus what the roster says is taken, so
        /// a hero dealt in a later milestone appears here with nothing edited - the same rule
        /// <c>RegisteredHeroRoster</c> is built on. A taken hero is greyed and NAMED with the
        /// one-copy rule's own words rather than silently dropped, because a player who cannot see
        /// the hero they wanted has no way to learn why it is gone.</para>
        /// <para>Answers <see cref="PrivateLobbySelection.NotRunning"/> when no coin was published,
        /// which is every session that predates this milestone, and when every seat has picked.</para>
        /// </remarks>
        private static PrivateLobbySelection SelectionFrom(
            IReadOnlyList<SeatRosterEntry> roster, TeamSide firstPickingTeam, SeatId viewingSeat)
        {
            if (firstPickingTeam != TeamSide.Red && firstPickingTeam != TeamSide.Blue)
            {
                return PrivateLobbySelection.NotRunning;
            }

            // 8.35z54. THE PICKS RUN AFTER READINESS, which is the order the user's spec
            // states: "they pick a seat, click ready. Once they are ready, everyone picks their
            // heroes." Before this the selection opened the moment a coin was published, so a
            // joiner who had just sat down was already being shown a hero list and a turn order
            // over a table that was still filling up - which is how a player comes to press PICK
            // HERO on somebody else's turn and be told nothing they can act on.
            //
            // It is derived from the roster and from nothing else, on both sides, so the host's
            // screen and every client's screen open the selection at the same instant without a
            // message saying so: every row Claimed and every row ready are facts the roster has
            // carried since milestone 8.35a. The host enforces the same rule on the pick itself -
            // this is the surface agreeing with the host rather than a second authority.
            if (!IsPickPhaseOpen(roster))
            {
                return PrivateLobbySelection.NotRunning;
            }

            var seats = new List<HeroSelectionSeat>();
            var taken = new HashSet<string>(StringComparer.Ordinal);
            var chosen = new HashSet<SeatId>();
            foreach (var entry in roster)
            {
                if (entry.TeamSide != TeamSide.Red && entry.TeamSide != TeamSide.Blue)
                {
                    return PrivateLobbySelection.NotRunning;
                }

                seats.Add(new HeroSelectionSeat(entry.Seat, entry.TeamSide));
                if (entry.HasChosenHero)
                {
                    taken.Add(entry.Hero.Value);
                    chosen.Add(entry.Seat);
                }
            }

            if (seats.Count == 0)
            {
                return PrivateLobbySelection.NotRunning;
            }

            var picking = default(SeatId);
            foreach (var seat in HeroSelectionStage.PickOrderFor(seats, firstPickingTeam))
            {
                if (!chosen.Contains(seat.Seat))
                {
                    picking = seat.Seat;
                    break;
                }
            }

            if (!picking.IsDefined)
            {
                return PrivateLobbySelection.NotRunning;
            }

            var heroes = new List<PrivateLobbyHeroOption>();
            foreach (var hero in RegisteredHeroRoster.ForThisBuild)
            {
                var available = !taken.Contains(hero.HeroIdValue);
                heroes.Add(new PrivateLobbyHeroOption(
                    hero.HeroIdValue,
                    hero.DisplayName,
                    hero.Complexity,
                    hero.Roles,
                    available,
                    available
                        ? string.Empty
                        : HeroSelectionLabels.Refusal(HeroSelectionRefusal.HeroAlreadyTaken)));
            }

            var isOwnPick = viewingSeat.IsDefined && picking.Equals(viewingSeat);
            // Milestone 8.108: "WAITING ON" names the picking player when they gave a name.
            var pickingLabel = PrivateLobbyLabels.SeatName(roster.First(entry => entry.Seat.Equals(picking)));
            return new PrivateLobbySelection(
                true,
                isOwnPick,
                pickingLabel,
                isOwnPick ? "YOUR PICK" : "WAITING ON " + pickingLabel,
                heroes);
        }

        /// <summary>
        /// 8.35z54. Everything about a roster that a lobby surface DRAWS, as one string, so
        /// a surface that redraws only when its state moves can tell whether it moved.
        /// </summary>
        /// <remarks>
        /// <para><b>It lives here because the bug it exists to prevent was a copy of it living
        /// somewhere else.</b> The shipped lobby driver built this string inline and named a row's
        /// seat, occupancy and readiness only - so an accepted hero pick, which moves the row's HERO
        /// and its CHOSEN flag and nothing else, left the string character-for-character identical
        /// and no surface redrew. The host had accepted the pick and broadcast it; every client had
        /// applied it; and the player saw nothing at all.</para>
        /// <para>Stating it once, beside the rows themselves, is what makes it testable without a
        /// running Unity scene and what makes "a roster field was added" and "the lobby notices it"
        /// the same edit. Every field a <see cref="PrivateLobbySeatRow"/> draws is named here.</para>
        /// </remarks>
        public static string RosterSignature(IReadOnlyList<SeatRosterEntry> roster)
        {
            var builder = new System.Text.StringBuilder();
            foreach (var entry in roster ?? Array.Empty<SeatRosterEntry>())
            {
                builder.Append('|').Append(entry.Seat.Value)
                    .Append(entry.Occupancy)
                    .Append(entry.IsReady ? '1' : '0')
                    .Append(entry.Hero.Value)
                    .Append(entry.HasChosenHero ? '1' : '0')
                    // Milestone 8.108: a name arriving must redraw the rows that show it.
                    .Append(entry.PlayerName.Length).Append(':').Append(entry.PlayerName);
            }

            return builder.ToString();
        }

        /// <summary>
        /// 8.35z54. Whether rulebook p.22's picking has opened: every seat taken and every
        /// seat ready.
        /// </summary>
        /// <remarks>
        /// <para>The one derivation of the spec's ordering, read by the host's screen, by every
        /// client's screen, and - through <c>PrivateSessionHost.IsPickPhaseOpen</c>, which asks the
        /// same two questions of the same roster - by the authority that refuses an early pick. It
        /// takes no message of its own because it needs none: occupancy and readiness have both been
        /// table-public roster facts since milestone 8.35a, which is exactly why readiness was put
        /// on the roster rather than computed locally.</para>
        /// <para>An empty roster is not open. That is not a special case but the same rule: a table
        /// with no seats has no seat that is taken and ready.</para>
        /// </remarks>
        public static bool IsPickPhaseOpen(IReadOnlyList<SeatRosterEntry> roster)
        {
            if (roster == null || roster.Count == 0)
            {
                return false;
            }

            foreach (var entry in roster)
            {
                if (entry.Occupancy != SeatOccupancyState.Claimed || !entry.IsReady)
                {
                    return false;
                }
            }

            return true;
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
        private static IReadOnlyList<PrivateLobbyControl> ControlsFor(
            PrivateSessionClient client, PrivateLobbySelection selection)
        {
            var controls = new List<PrivateLobbyControl>();
            switch (client.State)
            {
                // 8.35z54. In the session, no seat yet: the whole of what this player may
                // do is take one of the seats the roster shows Open. Readiness is not offered
                // because there is nothing to be ready WITH, and the hero list is not offered
                // because a hero belongs to a seat.
                case PrivateSessionClientState.Admitted:
                    if (!client.HasMatchStarted)
                    {
                        controls.Add(PrivateLobbyControl.ClaimSeat);
                    }

                    break;

                case PrivateSessionClientState.Seated:
                    if (!client.HasMatchStarted)
                    {
                        // Milestone 8.35z37. The pick control stands only while p.22's order
                        // is on THIS seat, which is what makes the sequence readable rather than six
                        // people racing. The host refuses an out-of-turn pick regardless; this is the
                        // surface agreeing with it rather than a second enforcement.
                        if (selection.IsRunning && selection.IsOwnPick)
                        {
                            controls.Add(PrivateLobbyControl.ChooseHero);
                        }

                        // 8.35z54. Readiness stops being offered once the picking has
                        // opened. Un-readying mid-selection would close the pick phase under five
                        // other people who are in the middle of it, and the control that exists for
                        // getting a wedged lobby out of a corner is the HOST's undo rather than any
                        // one seat's own switch.
                        if (!selection.IsRunning)
                        {
                            controls.Add(client.IsReady
                                ? PrivateLobbyControl.Unready
                                : PrivateLobbyControl.Ready);
                        }
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
