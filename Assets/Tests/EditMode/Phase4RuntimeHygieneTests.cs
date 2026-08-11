using System;
using System.IO;
using System.Linq;
using System.Text;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class Phase4RuntimeHygieneTests
    {
        [Test]
        public void RuleEventEnvelope_RoundTripsThroughTypedNonThrowingContract()
        {
            var payload = RuleEventLogSnapshotEnvelope.Serialize("authoritative-payload", new[]
            {
                new RuleEventLogEntry(1, "Setup.Created", "map=official-single-lane@1", "setup"),
                new RuleEventLogEntry(2, "Turn.Held", "hero=red-wasp", "hold")
            });

            Assert.That(RuleEventLogSnapshotEnvelope.TryDeserialize(
                payload,
                out var envelope,
                out var rejection), Is.True);
            Assert.That(rejection, Is.EqualTo(RuleEventLogEnvelopeRejectionReason.None));
            Assert.That(envelope.AuthoritativePayload, Is.EqualTo("authoritative-payload"));
            Assert.That(envelope.Entries.Select(value => value.Sequence), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void RuleEventEnvelope_RejectsMalformedFamiliesWithoutPartialEnvelopeOrException()
        {
            var valid = RuleEventLogSnapshotEnvelope.Serialize("authoritative-payload", new[]
            {
                new RuleEventLogEntry(1, "Setup.Created", "map=official-single-lane@1", "setup")
            });
            var validBytes = Convert.FromBase64String(valid);
            var trailing = validBytes.Concat(new byte[] { 0x4d }).ToArray();
            var cases = new[]
            {
                new RejectionCase("blank", "", RuleEventLogEnvelopeRejectionReason.BlankPayload),
                new RejectionCase("invalid-base64", "%%%", RuleEventLogEnvelopeRejectionReason.InvalidBase64),
                new RejectionCase("truncated-header", Convert.ToBase64String(validBytes.Take(2).ToArray()), RuleEventLogEnvelopeRejectionReason.TruncatedPayload),
                new RejectionCase("truncated-body", Convert.ToBase64String(validBytes.Take(validBytes.Length - 1).ToArray()), RuleEventLogEnvelopeRejectionReason.TruncatedPayload),
                new RejectionCase("invalid-header", CreateRaw(writer => writer.Write("NOPE")), RuleEventLogEnvelopeRejectionReason.InvalidHeader),
                new RejectionCase("unsupported-version", CreateEnvelopeHeader(99, 1), RuleEventLogEnvelopeRejectionReason.UnsupportedVersion),
                new RejectionCase("negative-count", CreateEnvelopeHeader(1, -1), RuleEventLogEnvelopeRejectionReason.InvalidEntryCount),
                new RejectionCase("oversized-count", CreateEnvelopeHeader(1, int.MaxValue), RuleEventLogEnvelopeRejectionReason.InvalidEntryCount),
                new RejectionCase("invalid-entry", CreateRaw(writer =>
                {
                    writer.Write("GDRL"); writer.Write(1); writer.Write("authoritative-payload"); writer.Write(1);
                    writer.Write(1); writer.Write(""); writer.Write("refs"); writer.Write("detail");
                }), RuleEventLogEnvelopeRejectionReason.InvalidEntry),
                new RejectionCase("trailing-data", Convert.ToBase64String(trailing), RuleEventLogEnvelopeRejectionReason.TrailingData),
                new RejectionCase("oversized-input", new string('A', 5600000), RuleEventLogEnvelopeRejectionReason.OversizedPayload)
            };

            foreach (var value in cases)
            {
                Assert.That(
                    RuleEventLogSnapshotEnvelope.TryDeserialize(value.Payload, out var envelope, out var rejection),
                    Is.False,
                    value.Name);
                Assert.That(envelope, Is.Null, value.Name);
                Assert.That(rejection, Is.EqualTo(value.Expected), value.Name);
            }
        }

        [Test]
        public void CompleteMapRebuild_ReusesOwnedNativeResourcesAndTearsThemDown()
        {
            var baselineMaterials = CountOwnedMaterials();
            var baselineMeshes = CountOwnedMeshes();
            var root = new GameObject("Phase4 Resource Owner Test");
            try
            {
                var view = root.AddComponent<PlaceholderTestGridView>();
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);

                view.Rebuild(map);
                var firstOwnedCount = view.OwnedNativeResourceCount;
                var firstMaterials = CountOwnedMaterials();
                var firstMeshes = CountOwnedMeshes();
                view.Rebuild(map);

                Assert.That(view.OwnedNativeResourceCount, Is.EqualTo(firstOwnedCount));
                Assert.That(CountOwnedMaterials(), Is.EqualTo(firstMaterials));
                Assert.That(CountOwnedMeshes(), Is.EqualTo(firstMeshes));
                Assert.That(root.GetComponentsInChildren<HexTileView>(true), Has.Length.EqualTo(292));
                Assert.That(root.GetComponentsInChildren<BoardMapSpawnMarkerView>(true), Has.Length.EqualTo(40));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }

            Assert.That(CountOwnedMaterials(), Is.EqualTo(baselineMaterials));
            Assert.That(CountOwnedMeshes(), Is.EqualTo(baselineMeshes));
        }

        private static int CountOwnedMaterials()
        {
            return Resources.FindObjectsOfTypeAll<Material>().Count(value =>
                value != null && value.hideFlags == HideFlags.DontSave &&
                (value.name.EndsWith(" Debug", StringComparison.Ordinal) ||
                 value.name.EndsWith(" Placeholder", StringComparison.Ordinal)));
        }

        private static int CountOwnedMeshes()
        {
            return Resources.FindObjectsOfTypeAll<Mesh>().Count(value =>
                value != null && value.name == "Owned Placeholder Grid Hex Mesh");
        }

        private static string CreateEnvelopeHeader(int version, int entryCount)
        {
            return CreateRaw(writer =>
            {
                writer.Write("GDRL");
                writer.Write(version);
                writer.Write("authoritative-payload");
                writer.Write(entryCount);
            });
        }

        private static string CreateRaw(Action<BinaryWriter> write)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                write(writer);
                writer.Flush();
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        private readonly struct RejectionCase
        {
            public RejectionCase(string name, string payload, RuleEventLogEnvelopeRejectionReason expected)
            {
                Name = name;
                Payload = payload;
                Expected = expected;
            }

            public string Name { get; }
            public string Payload { get; }
            public RuleEventLogEnvelopeRejectionReason Expected { get; }
        }
    }
}
