using System;
using System.Globalization;
using System.Net;

namespace GuardsDigital.Network
{
    /// <summary>
    /// Milestone 8.31. One validated host address and port: what an operator configures and a
    /// client dials.
    /// </summary>
    /// <remarks>
    /// Parsed strictly - a numeric IP and a port in range - rather than resolved: DNS is a service
    /// dependency the private track does not need for one operator-named machine, and refusing a
    /// hostname here keeps "what will be dialed" identical to "what was typed".
    /// </remarks>
    public sealed class PrivateSessionEndpoint
    {
        private PrivateSessionEndpoint(IPAddress address, int port)
        {
            Address = address;
            Port = port;
        }

        public IPAddress Address { get; }
        public int Port { get; }

        public bool IsLoopback => IPAddress.IsLoopback(Address);

        public IPEndPoint ToIPEndPoint() => new IPEndPoint(Address, Port);

        /// <summary>
        /// 8.35t corrective. The address a second process, spawned on this same machine, should
        /// dial to reach a listener bound at THIS endpoint. A wildcard bind (<c>0.0.0.0</c> or
        /// <c>::</c>) is not itself a valid destination - substituted with loopback, which always
        /// reaches a wildcard-bound listener from the same machine. Any other concrete address
        /// (typed so a remote friend's own port-forward lands on it, or to rehearse that exact
        /// setup solo) is the ONE interface actually accepting a connection, and is returned
        /// verbatim - confirmed directly against a real socket pair: a listener bound to a
        /// concrete address refuses a dial to a different one, wildcard or not.
        /// </summary>
        /// <remarks>
        /// Lives here, in the adapter, rather than beside its one caller in Presentation, because
        /// that caller may name no socket type of its own - the architecture record's own rule,
        /// enforced by <c>PrivateSessionArchitectureConformanceTests.SocketTypes_LiveOnlyInTheNetworkAdapter</c>.
        /// </remarks>
        public string ResolveLocalDialAddress() =>
            IPAddress.Any.Equals(Address) || IPAddress.IPv6Any.Equals(Address)
                ? "127.0.0.1"
                : Address.ToString();

        /// <summary>
        /// Milestone 8.100. The interactive HOST path has no reason to ask a player which local
        /// interface should accept the invitation. A keyed session binds every local IPv4
        /// interface so LAN/forwarded traffic can reach it; an unkeyed plaintext session stays on
        /// loopback. Standalone command-line hosting keeps its explicit bind contract.
        /// </summary>
        public static bool TryCreateAutomaticHostBind(
            int port, bool hasSessionKey, out PrivateSessionEndpoint endpoint, out string error) =>
            TryCreate(
                hasSessionKey ? IPAddress.Any.ToString() : IPAddress.Loopback.ToString(),
                port, out endpoint, out error);

        public static bool TryParseAddress(string text, out PrivateSessionEndpoint endpoint, out string error)
        {
            endpoint = null;
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                error = "An address is required, as ip:port.";
                return false;
            }

            var separator = text.LastIndexOf(':');
            if (separator <= 0 || separator == text.Length - 1)
            {
                error = "An address is ip:port.";
                return false;
            }

            var addressText = text.Substring(0, separator);
            var portText = text.Substring(separator + 1);
            if (!IPAddress.TryParse(addressText, out var address))
            {
                error = "The address part is not a numeric IP.";
                return false;
            }

            if (!TryParsePort(portText, out var port, out error))
            {
                return false;
            }

            endpoint = new PrivateSessionEndpoint(address, port);
            return true;
        }

        public static bool TryCreate(string addressText, int port, out PrivateSessionEndpoint endpoint, out string error)
        {
            endpoint = null;
            if (!IPAddress.TryParse(addressText ?? string.Empty, out var address))
            {
                error = "The bind address is not a numeric IP.";
                return false;
            }

            if (port < 1 || port > 65535)
            {
                error = "A port is between 1 and 65535.";
                return false;
            }

            error = string.Empty;
            endpoint = new PrivateSessionEndpoint(address, port);
            return true;
        }

        private static bool TryParsePort(string text, out int port, out string error)
        {
            error = string.Empty;
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out port) ||
                port < 1 || port > 65535)
            {
                port = 0;
                error = "A port is between 1 and 65535.";
                return false;
            }

            return true;
        }

        public override string ToString() =>
            Address + ":" + Port.ToString(CultureInfo.InvariantCulture);
    }
}
