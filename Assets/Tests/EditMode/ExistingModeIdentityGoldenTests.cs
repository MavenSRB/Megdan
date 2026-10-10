using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using GuardsDigital.Application;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// W-021. The existing mode's behavior fingerprint, pinned before the PvE sequence added its
    /// first shared hook. A seeded official six-player seat walk records the authoritative
    /// snapshot at fixed command counts; every later PvE hook is additive and default-off, so the
    /// same walk must keep producing byte-identical snapshots.
    /// </summary>
    /// <remarks>
    /// The hashes were captured on `work/W-020` (bb2f2627), before any W-021 change to shared code.
    /// A legitimate future change to existing-mode rules or snapshot content re-pins them in the
    /// same commit, citing the reason; a PvE change never does.
    /// </remarks>
    public sealed class ExistingModeIdentityGoldenTests
    {
        private const int Seed = 29;
        private const int Checkpoint = 150;
        private const int Budget = 1500;

        private static readonly string[] PinnedSnapshotHashes =
        {
            "327997125f6927b0e9cdfea019ec9c198cf62b2853a614d94e8ce3eaa31cccb8",
            "13fc700bd2bc1b070ea9ee781e4220e7a360c76c29408318bcf870e307ced1b5",
            "475e041e12e40132844b83136c6b9fcfbafd8d85f163f47655d8b07ebd87072f",
            "3cb28b2250e0efc91b2abbd1d90bc7bdf7e286a3fcd4c35f7211df65f5df076f",
            "d9f791e5574ff83a9ddf759d3a69b2dff974f3e01d2bc33c9823bcf2b5335a85",
            "d6c19afc1f90d65da4ab154437551f972ab25b168bd380b12df9f55857d6a1ab",
            "8bc6d0c75611aac6228537a9a540c52c5c5554331d61fbd710a9dfe16bab10ad",
            "7731d72b51d787044c9c75bb9192a2827d22b6b68b24cbcdc4ba49c5144d8473",
            "d4b10e4a53ccb41c145c095263e9fe8f4ad37d4a1ce7b3ae424abf8ffbb0c315",
            "0a9c3571e5e5695ac294e63e8884e81cb5d9e137fed99d053cd902005fc2e5c0",
            "ef7b7e3c4f8c2c3f31fce48d7d63b0e9a7e4cb767bde0923e64a7ddd8c11cd8f"
        };

        [Test]
        public void SeededOfficialSeatWalkKeepsItsPinnedSnapshots()
        {
            var observed = Walk();

            Assert.That(observed.Count, Is.GreaterThan(1),
                "The walk must reach several checkpoints to fingerprint more than setup.");
            Assert.That(observed, Is.EqualTo(PinnedSnapshotHashes),
                "Existing-mode snapshots moved. Observed:\n" + string.Join(",\n", observed.Select(
                    value => "\"" + value + "\"")));
        }

        [Test]
        public void TheWalkIsReproducibleWithinOneRun()
        {
            Assert.That(Walk(), Is.EqualTo(Walk()),
                "The fingerprint is only meaningful if the walk itself is deterministic.");
        }

        private static List<string> Walk()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var hashes = new List<string> { Hash(host.CaptureSnapshot()) };
            var driven = 0;
            while (driven < Budget)
            {
                var step = OwnerScopedProjectionTests.DriveThroughSeats(
                    host, Checkpoint, null, Seed + driven);
                driven += Checkpoint;
                hashes.Add(Hash(host.CaptureSnapshot()));
                if (step < Checkpoint)
                {
                    break;
                }
            }

            return hashes;
        }

        private static string Hash(string snapshot)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(snapshot)))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }
    }
}
