using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using GuardsDigital.Application;

namespace GuardsDigital.Network
{
    /// <summary>
    /// Milestone 8.107. The pure half of asking the local router for this network's internet
    /// address over UPnP: reading the three replies. Kept apart from the sockets so every reading is
    /// provable offline.
    /// </summary>
    /// <remarks>
    /// <para>The conversation is the standard Internet Gateway Device one. An SSDP search is
    /// multicast on the LAN, and the gateway answers with a <c>LOCATION</c> URL. That URL's device
    /// description names a WAN connection service and its control URL, and a SOAP
    /// <c>GetExternalIPAddress</c> call to that URL returns <c>NewExternalIPAddress</c>. Nothing
    /// leaves the local network. The user chose this over an outside "what is my IP" service.</para>
    /// <para>Every reply comes from whichever device on the LAN chose to answer, so it is treated as
    /// untrusted. A description is fetched only from a private IPv4 host. The address is shown only
    /// if it parses as a public IPv4, and nothing read here is ever dialled or executed.</para>
    /// </remarks>
    public static class RouterAddressParsing
    {
        public const string SsdpAddress = "239.255.255.250";
        public const int SsdpPort = 1900;

        public const string GatewaySearchTarget =
            "urn:schemas-upnp-org:device:InternetGatewayDevice:1";

        /// <summary>The WAN services that answer GetExternalIPAddress, in preference order.</summary>
        public static readonly IReadOnlyList<string> WanServiceTypes = new[]
        {
            "urn:schemas-upnp-org:service:WANIPConnection:2",
            "urn:schemas-upnp-org:service:WANIPConnection:1",
            "urn:schemas-upnp-org:service:WANPPPConnection:1"
        };

        public static string SearchRequest() =>
            "M-SEARCH * HTTP/1.1\r\n" +
            "HOST: " + SsdpAddress + ":" + SsdpPort + "\r\n" +
            "MAN: \"ssdp:discover\"\r\n" +
            "MX: 2\r\n" +
            "ST: " + GatewaySearchTarget + "\r\n\r\n";

        public static string ExternalAddressRequestBody(string serviceType) =>
            "<?xml version=\"1.0\"?>" +
            "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
            "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
            "<s:Body><u:GetExternalIPAddress xmlns:u=\"" + serviceType + "\"/></s:Body>" +
            "</s:Envelope>";

        /// <summary>
        /// The description URL an SSDP reply names, when it names one on a private IPv4 host.
        /// </summary>
        public static bool TryReadSsdpLocation(string response, out Uri location)
        {
            location = null;
            if (string.IsNullOrEmpty(response))
            {
                return false;
            }

            foreach (var rawLine in response.Split('\n'))
            {
                var line = rawLine.Trim();
                var colon = line.IndexOf(':');
                if (colon <= 0 ||
                    !string.Equals(line.Substring(0, colon).Trim(), "LOCATION",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = line.Substring(colon + 1).Trim();
                if (Uri.TryCreate(value, UriKind.Absolute, out var parsed) &&
                    parsed.Scheme == Uri.UriSchemeHttp &&
                    IsPrivateIPv4(parsed.Host))
                {
                    location = parsed;
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// The control URL of the first WAN connection service a device description lists, resolved
        /// against its <c>URLBase</c> or, failing that, against the URL the description came from.
        /// </summary>
        public static bool TryFindWanControlUrl(
            string descriptionXml, Uri descriptionUri, out Uri controlUrl, out string serviceType)
        {
            controlUrl = null;
            serviceType = null;
            if (string.IsNullOrEmpty(descriptionXml) || descriptionUri == null)
            {
                return false;
            }

            XmlDocument document;
            try
            {
                document = new XmlDocument { XmlResolver = null };
                using (var reader = XmlReader.Create(
                           new StringReader(descriptionXml),
                           new XmlReaderSettings
                           {
                               DtdProcessing = DtdProcessing.Prohibit,
                               XmlResolver = null
                           }))
                {
                    document.Load(reader);
                }
            }
            catch (XmlException)
            {
                return false;
            }

            var baseUri = descriptionUri;
            var urlBase = document.SelectSingleNode("//*[local-name()='URLBase']")?.InnerText?.Trim();
            if (!string.IsNullOrEmpty(urlBase) &&
                Uri.TryCreate(urlBase, UriKind.Absolute, out var parsedBase) &&
                parsedBase.Scheme == Uri.UriSchemeHttp)
            {
                baseUri = parsedBase;
            }

            var services = document.SelectNodes("//*[local-name()='service']");
            if (services == null)
            {
                return false;
            }

            foreach (var wanted in WanServiceTypes)
            {
                foreach (XmlNode service in services)
                {
                    var type = service.SelectSingleNode("*[local-name()='serviceType']")?.InnerText?.Trim();
                    if (!string.Equals(type, wanted, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var control = service.SelectSingleNode("*[local-name()='controlURL']")?.InnerText?.Trim();
                    if (string.IsNullOrEmpty(control) ||
                        !Uri.TryCreate(baseUri, control, out var resolved) ||
                        resolved.Scheme != Uri.UriSchemeHttp ||
                        !IsPrivateIPv4(resolved.Host))
                    {
                        continue;
                    }

                    controlUrl = resolved;
                    serviceType = wanted;
                    return true;
                }
            }

            return false;
        }

        /// <summary>The public IPv4 a GetExternalIPAddress answer names, when it names one.</summary>
        public static bool TryReadExternalAddress(string soapResponse, out string address)
        {
            address = null;
            if (string.IsNullOrEmpty(soapResponse))
            {
                return false;
            }

            const string Tag = "NewExternalIPAddress";
            var open = soapResponse.IndexOf("<" + Tag, StringComparison.OrdinalIgnoreCase);
            if (open < 0)
            {
                return false;
            }

            var start = soapResponse.IndexOf('>', open);
            var end = soapResponse.IndexOf("</" + Tag, StringComparison.OrdinalIgnoreCase);
            if (start < 0 || end <= start)
            {
                return false;
            }

            var candidate = soapResponse.Substring(start + 1, end - start - 1).Trim();
            if (!IsPublicIPv4(candidate))
            {
                return false;
            }

            address = candidate;
            return true;
        }

        /// <summary>
        /// Whether text is a dotted IPv4 outside every private, shared, loopback, link-local and
        /// reserved range. A router behind another router reports the inner network's address, and
        /// that address is useless to somebody on the internet.
        /// </summary>
        public static bool IsPublicIPv4(string text)
        {
            if (!TryParseDottedIPv4(text, out var octets))
            {
                return false;
            }

            return !IsPrivateOrReserved(octets);
        }

        /// <summary>Whether text is a dotted IPv4 inside the private or link-local ranges.</summary>
        public static bool IsPrivateIPv4(string text)
        {
            if (!TryParseDottedIPv4(text, out var o))
            {
                return false;
            }

            return o[0] == 10 ||
                (o[0] == 172 && o[1] >= 16 && o[1] <= 31) ||
                (o[0] == 192 && o[1] == 168) ||
                (o[0] == 169 && o[1] == 254) ||
                (o[0] == 100 && o[1] >= 64 && o[1] <= 127);
        }

        private static bool IsPrivateOrReserved(byte[] o) =>
            o[0] == 0 || o[0] == 10 || o[0] == 127 || o[0] >= 224 ||
            (o[0] == 100 && o[1] >= 64 && o[1] <= 127) ||
            (o[0] == 169 && o[1] == 254) ||
            (o[0] == 172 && o[1] >= 16 && o[1] <= 31) ||
            (o[0] == 192 && o[1] == 168) ||
            (o[0] == 192 && o[1] == 0 && o[2] == 2) ||
            (o[0] == 198 && (o[1] == 18 || o[1] == 19)) ||
            (o[0] == 198 && o[1] == 51 && o[2] == 100) ||
            (o[0] == 203 && o[1] == 0 && o[2] == 113);

        private static bool TryParseDottedIPv4(string text, out byte[] octets)
        {
            octets = null;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            var parts = text.Split('.');
            if (parts.Length != 4)
            {
                return false;
            }

            var parsed = new byte[4];
            for (var index = 0; index < 4; index++)
            {
                var part = parts[index];
                if (part.Length == 0 || part.Length > 3)
                {
                    return false;
                }

                var value = 0;
                foreach (var character in part)
                {
                    if (character < '0' || character > '9')
                    {
                        return false;
                    }

                    value = value * 10 + (character - '0');
                }

                if (value > 255)
                {
                    return false;
                }

                parsed[index] = (byte)value;
            }

            octets = parsed;
            return true;
        }
    }

    /// <summary>
    /// Milestone 8.107. Asks the local router, once and in the background, which internet address
    /// this network has, and finds this machine's own LAN IPv4 in case it does not answer.
    /// </summary>
    /// <remarks>
    /// The lobby polls <see cref="Discovery"/> from its frame loop. The whole question is bounded by
    /// one budget, a few seconds by default, and every failure lands on
    /// <see cref="HostAddressDiscovery.RouterSilent"/> rather than an exception.
    /// </remarks>
    public sealed class RouterExternalAddressProbe
    {
        private const int MaxReplyBytes = 64 * 1024;

        private readonly object gate = new object();
        private HostAddressDiscovery discovery = HostAddressDiscovery.NotStarted;
        private string internetAddress = string.Empty;
        private string lanAddress = string.Empty;

        public HostAddressDiscovery Discovery { get { lock (gate) return discovery; } }

        public string InternetAddress { get { lock (gate) return internetAddress; } }

        public string LanAddress { get { lock (gate) return lanAddress; } }

        /// <summary>Starts the question once; later calls do nothing.</summary>
        public void Start(TimeSpan budget)
        {
            lock (gate)
            {
                if (discovery != HostAddressDiscovery.NotStarted)
                {
                    return;
                }

                discovery = HostAddressDiscovery.Searching;
            }

            ThreadPool.QueueUserWorkItem(_ => Run(budget));
        }

        private void Run(TimeSpan budget)
        {
            var lan = string.Empty;
            string found = null;
            try
            {
                lan = FindLanIPv4();
                found = AskRouter(DateTime.UtcNow + budget);
            }
            catch (Exception)
            {
                found = null;
            }

            lock (gate)
            {
                lanAddress = lan ?? string.Empty;
                internetAddress = found ?? string.Empty;
                discovery = string.IsNullOrEmpty(found)
                    ? HostAddressDiscovery.RouterSilent
                    : HostAddressDiscovery.RouterAnswered;
            }
        }

        /// <summary>
        /// This machine's LAN IPv4: the first private address on an interface that is up and has a
        /// default gateway, else the first private address on any interface that is up.
        /// </summary>
        public static string FindLanIPv4()
        {
            string fallback = null;
            try
            {
                foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (adapter.OperationalStatus != OperationalStatus.Up ||
                        adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                        adapter.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                    {
                        continue;
                    }

                    var properties = adapter.GetIPProperties();
                    var hasGateway = false;
                    foreach (var gateway in properties.GatewayAddresses)
                    {
                        if (gateway.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !gateway.Address.Equals(IPAddress.Any))
                        {
                            hasGateway = true;
                            break;
                        }
                    }

                    foreach (var unicast in properties.UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily != AddressFamily.InterNetwork)
                        {
                            continue;
                        }

                        var text = unicast.Address.ToString();
                        if (!RouterAddressParsing.IsPrivateIPv4(text) || text.StartsWith("169.254.", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        if (hasGateway)
                        {
                            return text;
                        }

                        fallback = fallback ?? text;
                    }
                }
            }
            catch (Exception)
            {
                // Some platforms refuse to enumerate adapters; the caller shows "unknown".
            }

            return fallback ?? string.Empty;
        }

        private static string AskRouter(DateTime deadline)
        {
            foreach (var location in SearchForGateways(deadline))
            {
                var description = HttpText(location, "GET", null, null, deadline);
                if (!RouterAddressParsing.TryFindWanControlUrl(
                        description, location, out var control, out var serviceType))
                {
                    continue;
                }

                var answer = HttpText(
                    control,
                    "POST",
                    "\"" + serviceType + "#GetExternalIPAddress\"",
                    RouterAddressParsing.ExternalAddressRequestBody(serviceType),
                    deadline);
                if (RouterAddressParsing.TryReadExternalAddress(answer, out var address))
                {
                    return address;
                }
            }

            return null;
        }

        private static IEnumerable<Uri> SearchForGateways(DateTime deadline)
        {
            var found = new List<Uri>();
            using (var udp = new UdpClient(AddressFamily.InterNetwork))
            {
                var request = Encoding.ASCII.GetBytes(RouterAddressParsing.SearchRequest());
                var target = new IPEndPoint(
                    IPAddress.Parse(RouterAddressParsing.SsdpAddress), RouterAddressParsing.SsdpPort);
                udp.Send(request, request.Length, target);
                udp.Send(request, request.Length, target);

                // Replies arrive within MX seconds; stop at the first usable gateway or the deadline.
                var searchEnd = Min(deadline, DateTime.UtcNow + TimeSpan.FromSeconds(2.5));
                while (DateTime.UtcNow < searchEnd && found.Count == 0)
                {
                    var remaining = (int)(searchEnd - DateTime.UtcNow).TotalMilliseconds;
                    if (remaining <= 0)
                    {
                        break;
                    }

                    udp.Client.ReceiveTimeout = remaining;
                    byte[] reply;
                    try
                    {
                        IPEndPoint from = null;
                        reply = udp.Receive(ref from);
                    }
                    catch (SocketException)
                    {
                        break;
                    }

                    var text = Encoding.ASCII.GetString(reply, 0, Math.Min(reply.Length, 4096));
                    if (RouterAddressParsing.TryReadSsdpLocation(text, out var location) &&
                        !found.Contains(location))
                    {
                        found.Add(location);
                    }
                }
            }

            return found;
        }

        private static string HttpText(
            Uri uri, string method, string soapAction, string body, DateTime deadline)
        {
            var remaining = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
            if (remaining <= 0)
            {
                return null;
            }

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = method;
                request.Timeout = remaining;
                request.ReadWriteTimeout = remaining;
                request.AllowAutoRedirect = false;
                request.Proxy = null;
                if (body != null)
                {
                    var bytes = Encoding.UTF8.GetBytes(body);
                    request.ContentType = "text/xml; charset=\"utf-8\"";
                    request.Headers.Add("SOAPAction", soapAction);
                    request.ContentLength = bytes.Length;
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var stream = response.GetResponseStream())
                {
                    return stream == null ? null : ReadBounded(stream);
                }
            }
            catch (WebException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        private static string ReadBounded(Stream stream)
        {
            var buffer = new byte[8192];
            using (var collected = new MemoryStream())
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    collected.Write(buffer, 0, read);
                    if (collected.Length > MaxReplyBytes)
                    {
                        return null;
                    }
                }

                return Encoding.UTF8.GetString(collected.ToArray());
            }
        }

        private static DateTime Min(DateTime left, DateTime right) => left < right ? left : right;
    }
}
