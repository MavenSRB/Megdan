using System.Security.Cryptography;
using GuardsDigital.Application;

namespace GuardsDigital.Network
{
    /// <summary>
    /// Milestone 8.107. Draws the random bytes behind a host's fresh session ID.
    /// </summary>
    /// <remarks>
    /// It lives beside <see cref="CryptoNonceSource"/> rather than in the Application boundary,
    /// which may draw no unrecorded entropy. A session ID is transport state, the pre-shared key a
    /// host hands its guests, and it never touches the match, so it needs no recorded seed.
    /// </remarks>
    public static class PrivateSessionIdSource
    {
        /// <summary>A fresh session ID from the operating system's cryptographic source.</summary>
        public static string NewSessionId()
        {
            var bytes = new byte[PrivateLobbyEntryText.SessionIdGroups * PrivateLobbyEntryText.SessionIdGroupLength];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
            }

            return PrivateLobbyEntryText.SessionIdFrom(bytes);
        }
    }
}
