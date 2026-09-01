using System;
using System.Globalization;
using GuardsDigital.Application;

namespace GuardsDigital.Network
{
    /// <summary>Which role, if any, the command line asked this process to play.</summary>
    public enum PrivateSessionRole
    {
        None = 0,
        Host = 1,
        Client = 2
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
        public const string BindFlag = "-guardsBind";
        public const string PortFlag = "-guardsPort";
        public const string AddressFlag = "-guardsAddress";
        public const string SeatFlag = "-guardsSeat";
        public const string SessionKeyFlag = "-guardsSessionKey";
        public const string TimeoutFlag = "-guardsOnlineTimeoutSeconds";

        public const int DefaultTimeoutSeconds = 120;

        private PrivateSessionLaunchOptions(
            PrivateSessionRole role,
            PrivateSessionEndpoint endpoint,
            SeatId seat,
            string sessionKey,
            int timeoutSeconds,
            string error)
        {
            Role = role;
            Endpoint = endpoint;
            Seat = seat;
            SessionKey = sessionKey;
            TimeoutSeconds = timeoutSeconds;
            Error = error;
        }

        public PrivateSessionRole Role { get; }

        /// <summary>The bind endpoint for a host, the dial endpoint for a client.</summary>
        public PrivateSessionEndpoint Endpoint { get; }

        /// <summary>The one seat a client claims; undefined for a host.</summary>
        public SeatId Seat { get; }

        public string SessionKey { get; }

        public int TimeoutSeconds { get; }

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

            switch (role)
            {
                case PrivateSessionRole.None:
                    return new PrivateSessionLaunchOptions(
                        PrivateSessionRole.None, null, default, string.Empty,
                        timeoutSeconds, string.Empty);

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

                    return new PrivateSessionLaunchOptions(
                        PrivateSessionRole.Host, endpoint, default, sessionKey,
                        timeoutSeconds, string.Empty);
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
                        timeoutSeconds, string.Empty);
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
                DefaultTimeoutSeconds, error);
    }
}
