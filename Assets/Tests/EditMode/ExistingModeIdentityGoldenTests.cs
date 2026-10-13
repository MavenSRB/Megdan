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
            "1d79960a1e20285b03e894833ea313be70fd5d66cc7426d9912a07eafa4fc78b",
            "35a780be9218fd3cf8426b5aea19dc348fd238a0b919dc521d1ef373deafb292",
            "04bac47507f65faa6df40356b9bba0e2e1841cfe6ac72883ce18af6e60beb1ad",
            "0ea0eaf5cb5497fae288e41d53414f8b797530f850bd2982b368c1bffcf83034",
            "1efc237c10c0466b67796705119baf497084fbc60f8660db6d98a5d8fa7ba32e",
            "3e788e02fcd09d6a12502f64f9e6c78ef0bc5294da2a7795dd5e48fe3a79ba05",
            "3dbd80b6f2e529f37ff2ad2e7259c07a9b4fe42cae1ad113f7b541fd2b81f92c",
            "277c764d2cb42cc710aedda13aa44c0eca1ba3a4116b682e7f1c2b84004413a5",
            "b1d0230631650917f22344731ae59e3ac353f37a7eb543a4ba872aef04cef036",
            "4599fcefb5e53640179200ea92a1e9abbd1a97eaf9eecb5558bbedcd733ccf0e",
            "5ff3e385d0116914f4f9d7934fc421b8b083557dfdbf87746bc5cdec05357baf"
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
