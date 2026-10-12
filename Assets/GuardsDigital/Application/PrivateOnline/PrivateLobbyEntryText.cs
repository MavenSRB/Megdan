using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.107. What the lobby's typed fields mean once a player has pasted into them.
    /// </summary>
    /// <remarks>
    /// <para>A playtest invitation is copied out of a chat window, and a chat window hands back a
    /// trailing space, a non-breaking space or a zero-width joiner along with the address. The
    /// endpoint parser then refuses <c>"45677 "</c> as "not a whole number", which reads to the
    /// player as a broken lobby. An IP address and a port contain no whitespace and no invisible
    /// characters, so every one of them is removed wherever it stands. A session ID is a secret the
    /// host may have typed with inner spaces on purpose, so only its ends are trimmed.</para>
    /// </remarks>
    public static class PrivateLobbyEntryText
    {
        /// <summary>The alphabet a generated session ID is drawn from: no 0/O, 1/I/L or U.</summary>
        public const string SessionIdAlphabet = "ABCDEFGHJKMNPQRSTVWXYZ23456789";

        /// <summary>Groups of four characters, joined by dashes: <c>K7QM-4TZP-9WXR</c>.</summary>
        public const int SessionIdGroups = 3;

        public const int SessionIdGroupLength = 4;

        /// <summary>An address or a port with every whitespace and invisible character removed.</summary>
        public static string CleanEndpointPart(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                if (!IsBlankOrInvisible(character))
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        /// <summary>A session ID with whitespace and invisible characters trimmed from both ends.</summary>
        public static string CleanSessionId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var start = 0;
            var end = value.Length;
            while (start < end && IsBlankOrInvisible(value[start]))
            {
                start++;
            }

            while (end > start && IsBlankOrInvisible(value[end - 1]))
            {
                end--;
            }

            return value.Substring(start, end - start);
        }

        /// <summary>Milestone 8.108. The longest player name a lobby shows.</summary>
        public const int MaxPlayerNameChars = 16;

        /// <summary>
        /// Milestone 8.108. A player name as every lobby shows it. Control and invisible characters
        /// are removed, each run of blanks becomes one space, the ends are trimmed, and the result
        /// is cut to <see cref="MaxPlayerNameChars"/> without splitting a surrogate pair.
        /// </summary>
        /// <remarks>
        /// The host applies this to every name it receives, because a name arrives from another
        /// machine and is shown on everybody's screen. Each client applies it again when it
        /// decodes a roster, so no build can draw more than this allows.
        /// </remarks>
        public static string CleanPlayerName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(Math.Min(value.Length, MaxPlayerNameChars * 2));
            var pendingSpace = false;
            foreach (var character in value)
            {
                if (char.IsWhiteSpace(character) ||
                    CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.SpaceSeparator)
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }

                if (IsBlankOrInvisible(character))
                {
                    continue;
                }

                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(character);
            }

            var cleaned = builder.ToString();
            if (cleaned.Length <= MaxPlayerNameChars)
            {
                return cleaned;
            }

            var cut = MaxPlayerNameChars;
            if (char.IsHighSurrogate(cleaned[cut - 1]))
            {
                cut--;
            }

            return cleaned.Substring(0, cut).TrimEnd();
        }

        /// <summary>
        /// The session ID a given run of random bytes names. The bytes come from
        /// <c>GuardsDigital.Network.PrivateSessionIdSource</c>: the Application boundary draws no
        /// unrecorded entropy of its own (SetupRandomnessConformanceTests), and a session ID is
        /// transport state rather than match state.
        /// </summary>
        public static string SessionIdFrom(IReadOnlyList<byte> bytes)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Count < SessionIdGroups * SessionIdGroupLength)
            {
                throw new ArgumentException(
                    "A session ID needs one random byte per character.", nameof(bytes));
            }

            var builder = new StringBuilder(SessionIdGroups * (SessionIdGroupLength + 1));
            for (var index = 0; index < SessionIdGroups * SessionIdGroupLength; index++)
            {
                if (index > 0 && index % SessionIdGroupLength == 0)
                {
                    builder.Append('-');
                }

                // A 256-value byte over a 30-letter alphabet is very slightly uneven. At twelve
                // characters that still leaves about 58 bits of guessing, far past what a private
                // table's key needs, and it keeps the ID short enough to read aloud.
                builder.Append(SessionIdAlphabet[bytes[index] % SessionIdAlphabet.Length]);
            }

            return builder.ToString();
        }

        private static bool IsBlankOrInvisible(char character)
        {
            if (char.IsWhiteSpace(character) || char.IsControl(character))
            {
                return true;
            }

            switch (CharUnicodeInfo.GetUnicodeCategory(character))
            {
                // Zero-width space/joiners, the byte-order mark and the other format characters
                // a chat client slips into copied text.
                case UnicodeCategory.Format:
                case UnicodeCategory.SpaceSeparator:
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.ParagraphSeparator:
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Milestone 8.107. Where the lobby keeps the values a player typed last time.
    /// </summary>
    /// <remarks>
    /// A string-to-string store the Unity layer backs with its player preferences, so the lobby's
    /// rules about WHAT is remembered can be proved here without one.
    /// </remarks>
    public interface IPrivateLobbyFieldMemory
    {
        string Read(string key);

        void Write(string key, string value);
    }

    /// <summary>Milestone 8.107. A memory that lasts as long as this object does.</summary>
    public sealed class InMemoryPrivateLobbyFieldMemory : IPrivateLobbyFieldMemory
    {
        private readonly Dictionary<string, string> values =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public string Read(string key) =>
            key != null && values.TryGetValue(key, out var value) ? value : null;

        public void Write(string key, string value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            values[key] = value ?? string.Empty;
        }
    }

    /// <summary>
    /// Milestone 8.107. The fields the lobby remembers between launches, and the one it does not.
    /// </summary>
    /// <remarks>
    /// The host's session ID is left out on purpose: the user's call was a fresh one every launch,
    /// so an ID handed to last week's table does not quietly keep working.
    /// </remarks>
    public sealed class PrivateLobbyRememberedFields
    {
        public const string JoinAddressKey = "guards.lobby.join-address";
        public const string JoinPortKey = "guards.lobby.join-port";
        public const string JoinSessionIdKey = "guards.lobby.join-session-id";
        public const string HostPortKey = "guards.lobby.host-port";
        public const string CardCommitmentSecondsKey = "guards.lobby.timer.card-commitment-seconds";
        public const string CardResolutionSecondsKey = "guards.lobby.timer.card-resolution-seconds";
        public const string ResponseSecondsKey = "guards.lobby.timer.response-seconds";
        public const string CardUpgradeSecondsKey = "guards.lobby.timer.card-upgrade-seconds";
        public const string TeamBankSecondsKey = "guards.lobby.timer.team-bank-seconds";

        public const int DefaultCardCommitmentSeconds = 120;
        public const int DefaultCardResolutionSeconds = 30;
        public const int DefaultResponseSeconds = 30;
        public const int DefaultCardUpgradeSeconds = 120;
        public const int DefaultTeamBankSeconds = 300;

        /// <summary>Milestone 8.108. The player's own name, shared by both menus.</summary>
        public const string PlayerNameKey = "guards.lobby.player-name";

        public PrivateLobbyRememberedFields(
            string joinAddress, string joinPort, string joinSessionId, string hostPort,
            string playerName = "",
            int cardCommitmentSeconds = DefaultCardCommitmentSeconds,
            int cardResolutionSeconds = DefaultCardResolutionSeconds,
            int responseSeconds = DefaultResponseSeconds,
            int cardUpgradeSeconds = DefaultCardUpgradeSeconds,
            int teamBankSeconds = DefaultTeamBankSeconds)
        {
            JoinAddress = joinAddress ?? string.Empty;
            JoinPort = joinPort ?? string.Empty;
            JoinSessionId = joinSessionId ?? string.Empty;
            HostPort = hostPort ?? string.Empty;
            PlayerName = playerName ?? string.Empty;
            CardCommitmentSeconds = cardCommitmentSeconds;
            CardResolutionSeconds = cardResolutionSeconds;
            ResponseSeconds = responseSeconds;
            CardUpgradeSeconds = cardUpgradeSeconds;
            TeamBankSeconds = teamBankSeconds;
        }

        public string JoinAddress { get; }
        public string JoinPort { get; }
        public string JoinSessionId { get; }
        public string HostPort { get; }
        public string PlayerName { get; }
        public int CardCommitmentSeconds { get; }
        public int CardResolutionSeconds { get; }
        public int ResponseSeconds { get; }
        public int CardUpgradeSeconds { get; }
        public int TeamBankSeconds { get; }

        /// <summary>
        /// What the store holds, with each missing value replaced by the lobby's default. The
        /// defaults are this machine's loopback address, the protocol's port and no session ID.
        /// </summary>
        public static PrivateLobbyRememberedFields Load(
            IPrivateLobbyFieldMemory memory, string defaultAddress, string defaultPort)
        {
            string Read(string key, string fallback)
            {
                var value = memory?.Read(key);
                return string.IsNullOrEmpty(value) ? fallback : value;
            }

            int Duration(string key, int fallback, int minimum)
            {
                var value = memory?.Read(key);
                return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) &&
                    parsed >= minimum && parsed <= 86400 ? parsed : fallback;
            }

            return new PrivateLobbyRememberedFields(
                Read(JoinAddressKey, defaultAddress),
                Read(JoinPortKey, defaultPort),
                Read(JoinSessionIdKey, string.Empty),
                Read(HostPortKey, defaultPort),
                PrivateLobbyEntryText.CleanPlayerName(Read(PlayerNameKey, string.Empty)),
                Duration(CardCommitmentSecondsKey, DefaultCardCommitmentSeconds, 1),
                Duration(CardResolutionSecondsKey, DefaultCardResolutionSeconds, 1),
                Duration(ResponseSecondsKey, DefaultResponseSeconds, 1),
                Duration(CardUpgradeSecondsKey, DefaultCardUpgradeSeconds, 1),
                Duration(TeamBankSecondsKey, DefaultTeamBankSeconds, 0));
        }

        /// <summary>Milestone 8.108. Remembers the name a HOST or JOIN press used.</summary>
        public static void RememberPlayerName(IPrivateLobbyFieldMemory memory, string playerName)
        {
            if (memory == null) return;
            memory.Write(PlayerNameKey, PrivateLobbyEntryText.CleanPlayerName(playerName));
        }

        /// <summary>Remembers the five valid timer values without remembering any feature toggle.</summary>
        public static void RememberTimerSettings(
            IPrivateLobbyFieldMemory memory,
            ExperimentalMatchOptions options)
        {
            if (memory == null || options == null) return;
            memory.Write(CardCommitmentSecondsKey, options.CardCommitmentSeconds.ToString(CultureInfo.InvariantCulture));
            memory.Write(CardResolutionSecondsKey, options.CardResolutionSeconds.ToString(CultureInfo.InvariantCulture));
            memory.Write(ResponseSecondsKey, options.ResponseSeconds.ToString(CultureInfo.InvariantCulture));
            memory.Write(CardUpgradeSecondsKey, options.CardUpgradeSeconds.ToString(CultureInfo.InvariantCulture));
            memory.Write(TeamBankSecondsKey, options.TeamBankSeconds.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Remembers what a JOIN press used, cleaned exactly as the join reads it.</summary>
        public static void RememberJoin(
            IPrivateLobbyFieldMemory memory, string address, string port, string sessionId)
        {
            if (memory == null) return;
            memory.Write(JoinAddressKey, PrivateLobbyEntryText.CleanEndpointPart(address));
            memory.Write(JoinPortKey, PrivateLobbyEntryText.CleanEndpointPart(port));
            memory.Write(JoinSessionIdKey, PrivateLobbyEntryText.CleanSessionId(sessionId));
        }

        /// <summary>Remembers what a HOST press used: the port, and never the session ID.</summary>
        public static void RememberHost(IPrivateLobbyFieldMemory memory, string port)
        {
            if (memory == null) return;
            memory.Write(HostPortKey, PrivateLobbyEntryText.CleanEndpointPart(port));
        }
    }

    /// <summary>How far the host's search for its own internet address has got.</summary>
    public enum HostAddressDiscovery
    {
        /// <summary>Nothing has been asked yet, or the session is local-only.</summary>
        NotStarted = 0,

        /// <summary>The local router and, if needed, HTTPS lookup are still being asked.</summary>
        Searching = 1,

        /// <summary>The router named a public address.</summary>
        RouterAnswered = 2,

        /// <summary>No lookup produced a public address.</summary>
        RouterSilent = 3,

        /// <summary>A bounded HTTPS fallback named a public address after UPnP did not.</summary>
        PublicServiceAnswered = 4
    }

    /// <summary>
    /// Milestone 8.107. The two lines a host's lobby shows so they can invite people: which session
    /// ID to hand out and which address to dial.
    /// </summary>
    /// <remarks>
    /// This reads the host's own typed key and its own network, and it is built only for the host's
    /// own screen. It is never part of <see cref="PrivateLobbyModel"/>, which a joining player's
    /// screen is also drawn from and which by its own rule carries neither a key nor an address.
    /// </remarks>
    public sealed class PrivateLobbyInvitation
    {
        public static readonly PrivateLobbyInvitation None =
            new PrivateLobbyInvitation(string.Empty, string.Empty);

        private PrivateLobbyInvitation(string sessionIdLine, string addressLine)
        {
            SessionIdLine = PrivateLobbyLabels.Bound(sessionIdLine);
            AddressLine = PrivateLobbyLabels.Bound(addressLine);
        }

        public string SessionIdLine { get; }

        public string AddressLine { get; }

        public bool IsShown => SessionIdLine.Length > 0 || AddressLine.Length > 0;

        /// <param name="sessionId">The key the session was hosted with; empty for a local-only session.</param>
        /// <param name="port">The port the listener actually bound.</param>
        /// <param name="discovery">How far the router question has got.</param>
        /// <param name="internetAddress">The public address the router named, when it did.</param>
        /// <param name="lanAddress">This machine's own LAN IPv4, or empty when it has none.</param>
        public static PrivateLobbyInvitation For(
            string sessionId,
            int port,
            HostAddressDiscovery discovery,
            string internetAddress,
            string lanAddress)
        {
            var portText = port.ToString(CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(sessionId))
            {
                // 8.100/8.35a1: an unkeyed session binds loopback only, so no other machine can
                // reach it whatever address the host reads out.
                return new PrivateLobbyInvitation(
                    "SESSION ID: NONE",
                    "THIS COMPUTER ONLY (127.0.0.1)   PORT " + portText +
                    " - host with a session ID to let others join");
            }

            string address;
            switch (discovery)
            {
                case HostAddressDiscovery.RouterAnswered when !string.IsNullOrEmpty(internetAddress):
                case HostAddressDiscovery.PublicServiceAnswered when !string.IsNullOrEmpty(internetAddress):
                    address = "INTERNET IP: " + internetAddress + "   PORT " + portText;
                    break;
                case HostAddressDiscovery.Searching:
                case HostAddressDiscovery.NotStarted:
                    address = "INTERNET IP: FINDING PUBLIC IP...   PORT " + portText;
                    break;
                default:
                    address = string.IsNullOrEmpty(lanAddress)
                        ? "INTERNET IP: UNKNOWN (lookup failed)   PORT " + portText
                        : "LAN IP: " + lanAddress + "   PORT " + portText +
                            " (public IP lookup failed)";
                    break;
            }

            return new PrivateLobbyInvitation("SESSION ID: " + sessionId, address);
        }
    }
}
