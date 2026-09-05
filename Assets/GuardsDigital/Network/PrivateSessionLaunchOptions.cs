using System;
using System.Globalization;
using GuardsDigital.Application;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Network
{
    /// <summary>Which role, if any, the command line asked this process to play.</summary>
    public enum PrivateSessionRole
    {
        None = 0,
        Host = 1,
        Client = 2,

        /// <summary>
        /// Milestone 8.35a. The interactive lobby: this process shows the primitive host/join
        /// surface and decides at a pressed control which of the other two roles it becomes.
        /// </summary>
        /// <remarks>
        /// It carries no endpoint and no seat off the command line, because the whole point of the
        /// lobby is that a player types the invitation in. The two command-line roles are
        /// untouched: they stay the way the rehearsal script drives six processes.
        /// </remarks>
        Lobby = 3
    }

    /// <summary>
    /// Milestone 8.31. The parsed private-online command line, following the smoke driver's
    /// pattern: a flag selects the role, everything else configures it, and a malformed line is a
    /// typed refusal rather than a half-configured process.
    /// </summary>
    /// <remarks>
    /// Flags: <c>-guardsPrivateHost</c> with optional <c>-guardsBind ip</c> and
    /// <c>-guardsPort n</c>; <c>-guardsPrivateClient</c> with required <c>-guardsAddress ip:port</c>
    /// and <c>-guardsSeat seat-id</c>; either role takes optional <c>-guardsSessionKey value</c>
    /// and <c>-guardsOnlineTimeoutSeconds n</c>. The host binds loopback unless an operator names
    /// another interface - a public listener is always an explicit decision.
    /// </remarks>
    public sealed class PrivateSessionLaunchOptions
    {
        public const string HostFlag = "-guardsPrivateHost";
        public const string ClientFlag = "-guardsPrivateClient";

        /// <summary>Milestone 8.35a. Show the interactive private lobby in this process.</summary>
        public const string LobbyFlag = "-guardsPrivateLobby";
        public const string BindFlag = "-guardsBind";
        public const string PortFlag = "-guardsPort";
        public const string AddressFlag = "-guardsAddress";
        public const string SeatFlag = "-guardsSeat";
        public const string SessionKeyFlag = "-guardsSessionKey";
        public const string TimeoutFlag = "-guardsOnlineTimeoutSeconds";

        /// <summary>
        /// ELEVENTH amendment. Which of rulebook pp.5-6's printed setups this process composes,
        /// named by <see cref="MatchFormat.Id"/>. Absent, the build composes
        /// <see cref="MatchFormat.Default"/> - milestone 8.25's six-player Quick Game - so every
        /// existing launch line, script and shortcut keeps starting exactly the match it started.
        /// </summary>
        /// <remarks>
        /// It is accepted beside EVERY role, the lobby included, and that is deliberate rather than
        /// an oversight of the lobby's "takes no address, bind, port, seat or key" rule. Those five
        /// are refused there because the lobby is where a player types an invitation, so a command
        /// line carrying one would be a second answer to a question the surface asks. The format is
        /// not an invitation: it is the match this process composes, settled before any front door
        /// opens, and until the lobby grows a control of its own this flag is the only way to ask
        /// for one.
        /// </remarks>
        public const string MatchFormatFlag = "-guardsMatchFormat";

        /// <summary>
        /// TWELFTH amendment. The staged situation this process loads and plays, named by the path
        /// of a <c>guards-sandbox-staging/1</c> file. Absent, the build composes the ordinary dealt
        /// match, so every existing launch line, script and shortcut is untouched.
        /// </summary>
        /// <remarks>
        /// <para><b>It is refused beside every networked role, and that is the enforcement rather
        /// than a comment.</b> The sandbox is a <c>DEV-004</c> development affordance whose ledger
        /// row requires it never be exposed in a real online match, and a staged board reaches a
        /// real online match by exactly one route: a host composing one before seats join. Refusing
        /// the flag beside the host, client and lobby roles closes that route in the parser, where
        /// no later surface can reopen it - so "Director-only" is a property of what will parse,
        /// not of what a caller remembers to check.</para>
        /// <para>This is the deliberate opposite of <see cref="MatchFormatFlag"/>, which IS accepted
        /// beside every role. The two differ because a format is the match a whole table agrees to
        /// play and is checked into the admission tuple, while a staging is one machine's private
        /// experiment that no admission tuple describes.</para>
        /// </remarks>
        public const string SandboxFlag = "-guardsSandbox";

        public const int DefaultTimeoutSeconds = 120;

        private PrivateSessionLaunchOptions(
            PrivateSessionRole role,
            PrivateSessionEndpoint endpoint,
            SeatId seat,
            string sessionKey,
            int timeoutSeconds,
            MatchFormat matchFormat,
            string sandboxStagingPath,
            string error)
        {
            Role = role;
            Endpoint = endpoint;
            Seat = seat;
            SessionKey = sessionKey;
            TimeoutSeconds = timeoutSeconds;
            MatchFormat = matchFormat ?? MatchFormat.Default;
            SandboxStagingPath = sandboxStagingPath ?? string.Empty;
            Error = error;
        }

        public PrivateSessionRole Role { get; }

        /// <summary>The bind endpoint for a host, the dial endpoint for a client.</summary>
        public PrivateSessionEndpoint Endpoint { get; }

        /// <summary>The one seat a client claims; undefined for a host.</summary>
        public SeatId Seat { get; }

        public string SessionKey { get; }

        public int TimeoutSeconds { get; }

        /// <summary>
        /// The printed setup this process composes. Never null - an absent flag reads as
        /// <see cref="MatchFormat.Default"/> - so no caller has to decide what nothing means.
        /// </summary>
        public MatchFormat MatchFormat { get; }

        /// <summary>
        /// TWELFTH amendment. The staging file this process loads, or empty for the ordinary dealt
        /// match. Never null, so no caller has to decide what nothing means, and non-empty only on
        /// a <see cref="PrivateSessionRole.None"/> line - the parser refuses it beside every
        /// networked role.
        /// </summary>
        public string SandboxStagingPath { get; }

        /// <summary>Whether this process composes a staged sandbox board rather than a dealt one.</summary>
        public bool IsSandbox => SandboxStagingPath.Length > 0;

        /// <summary>Why the line was refused; empty when it parsed.</summary>
        public string Error { get; }

        public bool IsValid => Error.Length == 0;

        public static PrivateSessionLaunchOptions Parse(string[] args)
        {
            args = args ?? Array.Empty<string>();
            var role = PrivateSessionRole.None;
            string bind = null;
            string portText = null;
            string address = null;
            string seatText = null;
            var sessionKey = string.Empty;
            var timeoutSeconds = DefaultTimeoutSeconds;
            var matchFormat = MatchFormat.Default;
            var sandboxStagingPath = string.Empty;

            for (var index = 0; index < args.Length; index++)
            {
                var argument = args[index] ?? string.Empty;
                switch (argument)
                {
                    case HostFlag:
                        if (role != PrivateSessionRole.None)
                        {
                            return Refused("The host and client flags are mutually exclusive.");
                        }

                        role = PrivateSessionRole.Host;
                        continue;

                    case ClientFlag:
                        if (role != PrivateSessionRole.None)
                        {
                            return Refused("The host and client flags are mutually exclusive.");
                        }

                        role = PrivateSessionRole.Client;
                        continue;

                    case LobbyFlag:
                        if (role != PrivateSessionRole.None)
                        {
                            return Refused("The host, client and lobby flags are mutually exclusive.");
                        }

                        role = PrivateSessionRole.Lobby;
                        continue;

                    case BindFlag:
                        if (!TryTakeValue(args, ref index, out bind))
                        {
                            return Refused(BindFlag + " requires an IP value.");
                        }

                        continue;

                    case PortFlag:
                        if (!TryTakeValue(args, ref index, out portText))
                        {
                            return Refused(PortFlag + " requires a port value.");
                        }

                        continue;

                    case AddressFlag:
                        if (!TryTakeValue(args, ref index, out address))
                        {
                            return Refused(AddressFlag + " requires an ip:port value.");
                        }

                        continue;

                    case SeatFlag:
                        if (!TryTakeValue(args, ref index, out seatText))
                        {
                            return Refused(SeatFlag + " requires a seat id.");
                        }

                        continue;

                    case SessionKeyFlag:
                        if (!TryTakeValue(args, ref index, out sessionKey))
                        {
                            return Refused(SessionKeyFlag + " requires a value.");
                        }

                        continue;

                    case MatchFormatFlag:
                        if (!TryTakeValue(args, ref index, out var matchFormatText))
                        {
                            return Refused(
                                MatchFormatFlag + " requires one of: " +
                                MatchFormat.DescribeEveryId() + ".");
                        }

                        matchFormat = MatchFormat.FindById(matchFormatText);
                        if (matchFormat == null)
                        {
                            return Refused(
                                "'" + matchFormatText + "' is not a printed match format. " +
                                MatchFormatFlag + " takes one of: " +
                                MatchFormat.DescribeEveryId() + ".");
                        }

                        continue;

                    case SandboxFlag:
                        if (!TryTakeValue(args, ref index, out sandboxStagingPath))
                        {
                            return Refused(
                                SandboxFlag + " requires the path of a staging file.");
                        }

                        continue;

                    case TimeoutFlag:
                        if (!TryTakeValue(args, ref index, out var timeoutText) ||
                            !int.TryParse(timeoutText, NumberStyles.None, CultureInfo.InvariantCulture,
                                out timeoutSeconds) ||
                            timeoutSeconds < 1)
                        {
                            return Refused(TimeoutFlag + " requires a positive whole number of seconds.");
                        }

                        continue;
                }
            }

            // TWELFTH amendment, and the whole of what makes the sandbox Director-only. A staged
            // board reaches a real online match by exactly one route - a host composing one before
            // its seats join - so the staging flag is refused beside every networked role here,
            // before any surface exists that could decide otherwise. DEV-004's "never expose in
            // real online matches" is therefore a fact about what parses rather than a rule someone
            // has to remember, and the refusal names the reason so a tester is not left guessing.
            if (sandboxStagingPath.Length > 0 && role != PrivateSessionRole.None)
            {
                return Refused(
                    SandboxFlag + " is a Director-only development affordance and takes no host, " +
                    "client or lobby role: a staged board is one machine's private experiment, " +
                    "and no seat may be dealt one.");
            }

            switch (role)
            {
                case PrivateSessionRole.None:
                    return new PrivateSessionLaunchOptions(
                        PrivateSessionRole.None, null, default, string.Empty,
                        timeoutSeconds, matchFormat, sandboxStagingPath, string.Empty);

                case PrivateSessionRole.Lobby:
                {
                    // The lobby is where an invitation is typed, so a command line that also
                    // carried one would be two answers to the same question.
                    if (bind != null || portText != null || address != null || seatText != null ||
                        sessionKey.Length > 0)
                    {
                        return Refused(
                            "The lobby takes no address, bind, port, seat or key; a player types " +
                            "the invitation into it.");
                    }

                    return new PrivateSessionLaunchOptions(
                        PrivateSessionRole.Lobby, null, default, string.Empty,
                        timeoutSeconds, matchFormat, string.Empty, string.Empty);
                }

                case PrivateSessionRole.Host:
                {
                    if (address != null || seatText != null)
                    {
                        return Refused("A host takes no address or seat; those are client flags.");
                    }

                    var port = PrivateSessionProtocol.DefaultPort;
                    if (portText != null &&
                        (!int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out port) ||
                            port < 1 || port > 65535))
                    {
                        return Refused(PortFlag + " requires a port between 1 and 65535.");
                    }

                    if (!PrivateSessionEndpoint.TryCreate(bind ?? "127.0.0.1", port, out var endpoint, out var error))
                    {
                        return Refused(error);
                    }

                    // Milestone 8.32. An unkeyed session has no channel keys, so its link stays
                    // plaintext - acceptable only where the record's on-path observer does not
                    // exist. A public listener therefore requires the session key that seals it.
                    // Milestone 8.35a1 moved the condition itself into the session layer, so this
                    // role and the lobby's HOST control refuse exactly the same binds; the words
                    // below stay this surface's own.
                    if (PrivateSessionProtocol.BindRequiresSessionKey(endpoint.IsLoopback, sessionKey))
                    {
                        return Refused(
                            "A non-loopback bind requires " + SessionKeyFlag + ": without a key " +
                            "the link is plaintext, and owner-private bytes may cross plaintext " +
                            "only on the loopback interface.");
                    }

                    return new PrivateSessionLaunchOptions(
                        PrivateSessionRole.Host, endpoint, default, sessionKey,
                        timeoutSeconds, matchFormat, string.Empty, string.Empty);
                }

                default:
                {
                    if (bind != null || portText != null)
                    {
                        return Refused("A client takes no bind or port; those are host flags.");
                    }

                    if (string.IsNullOrWhiteSpace(address))
                    {
                        return Refused("A client requires " + AddressFlag + " ip:port.");
                    }

                    if (!PrivateSessionEndpoint.TryParseAddress(address, out var endpoint, out var error))
                    {
                        return Refused(error);
                    }

                    if (string.IsNullOrWhiteSpace(seatText))
                    {
                        return Refused("A client requires " + SeatFlag + " with the seat to claim.");
                    }

                    return new PrivateSessionLaunchOptions(
                        PrivateSessionRole.Client, endpoint, new SeatId(seatText), sessionKey,
                        timeoutSeconds, matchFormat, string.Empty, string.Empty);
                }
            }
        }

        private static bool TryTakeValue(string[] args, ref int index, out string value)
        {
            value = null;
            if (index + 1 >= args.Length)
            {
                return false;
            }

            var candidate = args[index + 1];
            if (string.IsNullOrEmpty(candidate) || candidate.StartsWith("-", StringComparison.Ordinal))
            {
                return false;
            }

            index++;
            value = candidate;
            return true;
        }

        private static PrivateSessionLaunchOptions Refused(string error) =>
            new PrivateSessionLaunchOptions(
                PrivateSessionRole.None, null, default, string.Empty,
                DefaultTimeoutSeconds, MatchFormat.Default, string.Empty, error);
    }
}
