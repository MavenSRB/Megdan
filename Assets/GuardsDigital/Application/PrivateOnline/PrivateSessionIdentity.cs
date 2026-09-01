using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Snapshots;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.31. The immutable admission tuple: everything two processes must agree on before
    /// one may join the other's session.
    /// </summary>
    /// <remarks>
    /// <para>The amended architecture record names the elements - protocol, build identity,
    /// snapshot schema, map revision, and the registered content digest - and requires a mismatch
    /// to be refused by its exact failing element before the client is told anything else. That is
    /// what <see cref="FirstMismatch"/> answers.</para>
    /// <para>The schema and content elements are read from the same authorities the snapshot
    /// serializer writes and validates, so wire compatibility and snapshot compatibility cannot
    /// disagree: a pair of builds this tuple admits is a pair whose snapshots would restore across
    /// the same boundary.</para>
    /// </remarks>
    public sealed class PrivateSessionIdentity : IEquatable<PrivateSessionIdentity>
    {
        public PrivateSessionIdentity(
            string protocolFamily,
            int protocolVersion,
            string buildIdentity,
            int snapshotSchemaVersion,
            string mapId,
            int mapContentRevision,
            string contentDigest)
        {
            if (string.IsNullOrWhiteSpace(protocolFamily))
            {
                throw new ArgumentException("An identity requires a protocol family.", nameof(protocolFamily));
            }

            if (string.IsNullOrWhiteSpace(buildIdentity))
            {
                throw new ArgumentException("An identity requires a build identity.", nameof(buildIdentity));
            }

            if (string.IsNullOrWhiteSpace(mapId))
            {
                throw new ArgumentException("An identity requires a map id.", nameof(mapId));
            }

            if (string.IsNullOrWhiteSpace(contentDigest))
            {
                throw new ArgumentException("An identity requires a content digest.", nameof(contentDigest));
            }

            ProtocolFamily = protocolFamily;
            ProtocolVersion = protocolVersion;
            BuildIdentity = buildIdentity;
            SnapshotSchemaVersion = snapshotSchemaVersion;
            MapId = mapId;
            MapContentRevision = mapContentRevision;
            ContentDigest = contentDigest;
        }

        public string ProtocolFamily { get; }
        public int ProtocolVersion { get; }
        public string BuildIdentity { get; }
        public int SnapshotSchemaVersion { get; }
        public string MapId { get; }
        public int MapContentRevision { get; }

        /// <summary>The digest of every registered content revision; see <see cref="PrivateSessionContentDigest"/>.</summary>
        public string ContentDigest { get; }

        /// <summary>
        /// The identity a composed match publishes: protocol constants, the serializer's current
        /// schema, the composed map's pinned reference, and the digest of the configuration's
        /// registered content.
        /// </summary>
        public static PrivateSessionIdentity ForComposition(
            BoardMapDefinition map, MatchConfiguration configuration, string buildIdentity)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            return new PrivateSessionIdentity(
                PrivateSessionProtocol.FamilyName,
                PrivateSessionProtocol.Version,
                buildIdentity,
                AuthoritativeSnapshotSerializer.CurrentSchemaVersion,
                map.Id.Value,
                map.ContentRevision,
                PrivateSessionContentDigest.Compute(configuration));
        }

        /// <summary>
        /// The first element of <paramref name="other"/> that differs from this tuple, in the fixed
        /// order the record lists them, or <see cref="SessionRejectionReason.None"/> when the tuples
        /// agree. The order is part of the contract: a peer is told the first failing element and
        /// nothing about the ones after it.
        /// </summary>
        public SessionRejectionReason FirstMismatch(PrivateSessionIdentity other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            if (!string.Equals(ProtocolFamily, other.ProtocolFamily, StringComparison.Ordinal))
            {
                return SessionRejectionReason.ProtocolFamilyMismatch;
            }

            if (ProtocolVersion != other.ProtocolVersion)
            {
                return SessionRejectionReason.ProtocolVersionMismatch;
            }

            if (!string.Equals(BuildIdentity, other.BuildIdentity, StringComparison.Ordinal))
            {
                return SessionRejectionReason.BuildIdentityMismatch;
            }

            if (SnapshotSchemaVersion != other.SnapshotSchemaVersion)
            {
                return SessionRejectionReason.SnapshotSchemaMismatch;
            }

            if (!string.Equals(MapId, other.MapId, StringComparison.Ordinal) ||
                MapContentRevision != other.MapContentRevision)
            {
                return SessionRejectionReason.MapRevisionMismatch;
            }

            if (!string.Equals(ContentDigest, other.ContentDigest, StringComparison.Ordinal))
            {
                return SessionRejectionReason.ContentDigestMismatch;
            }

            return SessionRejectionReason.None;
        }

        /// <summary>
        /// A short deterministic correlation for logs: derived from the tuple itself, so two
        /// processes composed from the same content log the same value and no entropy is drawn.
        /// </summary>
        public string Correlation =>
            PrivateSessionContentDigest.HashText(CanonicalText).Substring(0, 12);

        /// <summary>The whole tuple as one canonical line, for hashing and diagnostics.</summary>
        public string CanonicalText =>
            ProtocolFamily + "/" + ProtocolVersion.ToString(CultureInfo.InvariantCulture) +
            " build=" + BuildIdentity +
            " schema=" + SnapshotSchemaVersion.ToString(CultureInfo.InvariantCulture) +
            " map=" + MapId + "@" + MapContentRevision.ToString(CultureInfo.InvariantCulture) +
            " content=" + ContentDigest;

        public bool Equals(PrivateSessionIdentity other) =>
            other != null && FirstMismatch(other) == SessionRejectionReason.None;

        public override bool Equals(object obj) => Equals(obj as PrivateSessionIdentity);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(CanonicalText);

        public override string ToString() => CanonicalText;
    }

    /// <summary>
    /// Milestone 8.31. One digest over every registered content revision a configuration runs on.
    /// </summary>
    /// <remarks>
    /// The listed inputs are exactly the lists the snapshot serializer's setup section writes and a
    /// restore validates - card definitions, capability registrations, and token/marker
    /// registrations, each as id-at-revision - so this digest agrees when and only when a snapshot
    /// written by one build would rebind on the other. A second, coarser content identity beside
    /// the snapshot's own is what `RISK-COMPAT-001` refuses; a digest of the same list is not one.
    /// </remarks>
    public static class PrivateSessionContentDigest
    {
        public static string Compute(MatchConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var lines = new List<string>();
            foreach (var definition in configuration.CardDefinitions
                .OrderBy(definition => definition.Id.Value, StringComparer.Ordinal))
            {
                lines.Add("card:" + definition.Id.Value + "@" +
                    definition.ContentRevision.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var reference in configuration.ContentCatalogs.Capabilities.Registered)
            {
                lines.Add("cap:" + reference.CapabilityId.Value + "@" +
                    reference.Revision.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var reference in configuration.ContentCatalogs.Components.RegisteredTokens)
            {
                lines.Add("tok:" + reference.DefinitionId.Value + "@" +
                    reference.Revision.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var reference in configuration.ContentCatalogs.Components.RegisteredMarkers)
            {
                lines.Add("mark:" + reference.DefinitionId.Value + "@" +
                    reference.Revision.ToString(CultureInfo.InvariantCulture));
            }

            return HashText(string.Join("\n", lines));
        }

        /// <summary>Lowercase hex SHA-256 of the UTF-8 text.</summary>
        public static string HashText(string text)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var value in hash)
                {
                    builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }
    }

    /// <summary>
    /// Milestone 8.31. The proof a client offers that it holds the session's pre-shared key,
    /// without the key itself crossing the wire.
    /// </summary>
    /// <remarks>
    /// The host issues a random nonce and the client answers with the hash of nonce and key. This
    /// is admission control, not encryption: it keeps a session invite-only against an uninvited
    /// connection, and the amended record says plainly that an on-path observer of the plaintext
    /// link is out of its reach - that is milestone 8.32's encryption decision.
    /// </remarks>
    public static class PrivateSessionKeyProof
    {
        /// <summary>The proof for that nonce and key; an absent key proves as the empty string.</summary>
        public static string Compute(byte[] nonce, string sessionKey)
        {
            if (nonce == null) throw new ArgumentNullException(nameof(nonce));
            if (string.IsNullOrEmpty(sessionKey))
            {
                return string.Empty;
            }

            var key = Encoding.UTF8.GetBytes(sessionKey);
            var material = new byte[nonce.Length + key.Length];
            Array.Copy(nonce, 0, material, 0, nonce.Length);
            Array.Copy(key, 0, material, nonce.Length, key.Length);

            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha.ComputeHash(material);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var value in hash)
                {
                    builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// Whether the offered proof matches the expected one, compared over every character so a
        /// mismatch costs the same as a match.
        /// </summary>
        public static bool Matches(string expected, string offered)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (offered == null)
            {
                return false;
            }

            var difference = expected.Length ^ offered.Length;
            var length = Math.Min(expected.Length, offered.Length);
            for (var index = 0; index < length; index++)
            {
                difference |= expected[index] ^ offered[index];
            }

            return difference == 0;
        }
    }
}
