using System;
using System.IO;
using GuardsDigital.Application;
using GuardsDigital.Core.Snapshots;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.33. Compatibility over the wire: every admission-tuple element mismatch -
    /// protocol family, protocol version, build identity, snapshot schema, map revision, content
    /// digest - is refused by its own name before a single projection byte crosses, the session
    /// stays usable for a compatible client afterwards, a snapshot of another schema is refused
    /// whole with no seat diverging, and the identity the wire admits with restates the same pins
    /// the protocol constants and the snapshot serializer declare, so the two contracts cannot
    /// drift apart.
    /// </summary>
    /// <remarks>
    /// Milestone 8.31's admission fixtures prove the tuple rule at the session machine; these
    /// cases prove it through real client machines on the 8.33 wire, where the thing at stake is
    /// what an incompatible client is told - a bounded name - and what it is never sent - any
    /// gameplay or projection byte.
    /// </remarks>
    public sealed class PrivateOnlineCompatibilityTests
    {
        /// <summary>
        /// One case per tuple element: the refusal names exactly that element, no projection or
        /// roster byte reaches the refused client, the seat map is untouched, and a compatible
        /// client seats normally on the same session afterwards.
        /// </summary>
        [Test]
        public void EachAdmissionMismatch_IsRefusedByName_BeforeAnyProjectionByte()
        {
            var cases = new (string element,
                Func<PrivateSessionIdentity, PrivateSessionIdentity> vary,
                SessionRejectionReason reason)[]
            {
                ("protocol family",
                    identity => With(identity, family: "guards-other-family"),
                    SessionRejectionReason.ProtocolFamilyMismatch),
                ("protocol version",
                    identity => With(identity, version: identity.ProtocolVersion + 1),
                    SessionRejectionReason.ProtocolVersionMismatch),
                ("build identity",
                    identity => With(identity, build: identity.BuildIdentity + "-other"),
                    SessionRejectionReason.BuildIdentityMismatch),
                ("snapshot schema",
                    identity => With(identity, schema: identity.SnapshotSchemaVersion + 1),
                    SessionRejectionReason.SnapshotSchemaMismatch),
                ("map revision",
                    identity => With(identity, mapRevision: identity.MapContentRevision + 1),
                    SessionRejectionReason.MapRevisionMismatch),
                ("content digest",
                    identity => With(identity, digest: new string('0', identity.ContentDigest.Length)),
                    SessionRejectionReason.ContentDigestMismatch)
            };

            foreach (var (element, vary, reason) in cases)
            {
                var harness = PrivateOnlineWireHarness.Compose();
                var seat = harness.Session.Roster[0].Seat.Value;
                var incompatible = harness.Connect(1, seat, vary(harness.Identity));
                harness.Pump();

                Assert.That(incompatible.State, Is.EqualTo(PrivateSessionClientState.Rejected),
                    element);
                Assert.That(incompatible.RejectionReason, Is.EqualTo(reason), element);
                Assert.That(incompatible.LatestProjection, Is.Null,
                    "A projection byte reached the " + element + "-mismatched client.");
                Assert.That(incompatible.Roster, Is.Empty,
                    "The refusal disclosed the roster to the " + element + "-mismatched client.");
                Assert.That(harness.Session.ClaimedSeatCount, Is.Zero, element);

                // The refusal is that connection's alone: a compatible client seats normally on
                // the very seat the refused one asked for.
                var compatible = harness.Connect(2, seat);
                harness.Pump();
                Assert.That(compatible.State, Is.EqualTo(PrivateSessionClientState.Seated), element);
                Assert.That(compatible.LatestProjection, Is.Not.Null, element);
            }
        }

        /// <summary>
        /// The incompatible-snapshot shape of the failure policy: a payload of another schema
        /// version is refused whole through the host port, nothing is mutated, and every seated
        /// client's view still equals the host's own encoding.
        /// </summary>
        [Test]
        public void AnIncompatibleSnapshotSchema_IsRefusedWhole_AndNoSeatDiverges()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(25);
            harness.AssertConverged();
            var versionBefore = harness.MatchHost.Version;

            var incompatible = RewriteSchemaVersion(
                harness.MatchHost.CaptureSnapshot(),
                AuthoritativeSnapshotSerializer.CurrentSchemaVersion + 1);
            var result = harness.MatchHost.Submit(MatchSubmission.ForSnapshotRestore(incompatible));

            Assert.That(result.Accepted, Is.False,
                "A snapshot of another schema restored anyway.");
            Assert.That(result.SessionRejectionReason,
                Is.EqualTo(MatchSessionRejectionReason.SnapshotRejected));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionBefore),
                "The refusal mutated nothing.");
            harness.AssertConverged();
            for (var ordinal = 1; ordinal <= 6; ordinal++)
            {
                Assert.That(harness.Client(ordinal).State,
                    Is.EqualTo(PrivateSessionClientState.Seated),
                    "The refused restore cost a seat its connection.");
            }
        }

        /// <summary>
        /// The admission identity restates the declared pins rather than carrying numbers of its
        /// own: the protocol constants, the serializer's current schema, and a content digest two
        /// independent compositions of the same registered content agree on - which is what makes
        /// admission deterministic across two processes on two machines. The session correlation
        /// is derived from the whole tuple, so equal tuples name the same session and a different
        /// build names a different one.
        /// </summary>
        [Test]
        public void TheAdmissionIdentity_RestatesTheDeclaredPins()
        {
            var first = PrivateOnlineWireHarness.Compose();
            var second = PrivateOnlineWireHarness.Compose();

            Assert.That(first.Identity.ProtocolFamily, Is.EqualTo(PrivateSessionProtocol.FamilyName));
            Assert.That(first.Identity.ProtocolVersion, Is.EqualTo(PrivateSessionProtocol.Version));
            Assert.That(first.Identity.SnapshotSchemaVersion,
                Is.EqualTo(AuthoritativeSnapshotSerializer.CurrentSchemaVersion));

            Assert.That(second.Identity.ContentDigest, Is.EqualTo(first.Identity.ContentDigest),
                "Two compositions of the same registered content must publish one digest.");
            Assert.That(second.Identity.MapContentRevision,
                Is.EqualTo(first.Identity.MapContentRevision));
            Assert.That(second.Identity.Correlation, Is.EqualTo(first.Identity.Correlation),
                "Equal tuples derive equal session correlations.");
            Assert.That(
                With(first.Identity, build: first.Identity.BuildIdentity + "-other").Correlation,
                Is.Not.EqualTo(first.Identity.Correlation),
                "A different build is a different session.");
        }

        /// <summary>A copy of the identity with exactly one element replaced.</summary>
        private static PrivateSessionIdentity With(
            PrivateSessionIdentity identity,
            string family = null,
            int? version = null,
            string build = null,
            int? schema = null,
            string mapId = null,
            int? mapRevision = null,
            string digest = null) =>
            new PrivateSessionIdentity(
                family ?? identity.ProtocolFamily,
                version ?? identity.ProtocolVersion,
                build ?? identity.BuildIdentity,
                schema ?? identity.SnapshotSchemaVersion,
                mapId ?? identity.MapId,
                mapRevision ?? identity.MapContentRevision,
                digest ?? identity.ContentDigest);

        /// <summary>
        /// The serializer's own layout, one field in: past the magic string sits the int schema
        /// version, and overwriting it yields a payload of another schema over valid content.
        /// </summary>
        private static string RewriteSchemaVersion(string payload, int schemaVersion)
        {
            var bytes = Convert.FromBase64String(payload);
            using (var stream = new MemoryStream(bytes, true))
            {
                using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true))
                {
                    reader.ReadString();
                }

                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                {
                    writer.Write(schemaVersion);
                }
            }

            return Convert.ToBase64String(bytes);
        }
    }
}
