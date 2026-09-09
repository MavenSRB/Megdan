using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Network;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    // Operator bundle collection and packaged .bat launchers target the supported Windows player.
    [Platform("Win")]
    public sealed class PrivateOnlineEvidenceTests
    {
        [Test]
        public void OnlineEvidenceBundleRejectsMixedRunIdentity()
        {
            var setup = PrivateSessionTestDriver.Compose();
            var first = AdmitFreshHost(setup);
            var second = AdmitFreshHost(setup);
            Assert.That(second, Is.Not.EqualTo(first),
                "Two ordinary hosts on the same build must not produce interchangeable run evidence.");
            CheckBundle("run", false);
            CheckBundle("build", false);
            CheckBundle("none", true);
        }

        [TestCase("version")]
        [TestCase("missing")]
        [TestCase("duplicate")]
        [TestCase("private-field")]
        [TestCase("aborted")]
        [TestCase("participants")]
        public void IncompleteOrUnattributableEvidenceIsRejected(string fault) => CheckBundle(fault, false);

        [TestCase(MatchVictoryCause.FinalWaveFlipped)]
        [TestCase(MatchVictoryCause.ThroneCaptured)]
        [TestCase(MatchVictoryCause.LifeCountersExhausted)]
        public void EachPublicEndingCanBeCaptured(MatchVictoryCause cause) => CheckBundle("none", true, cause);

        private static void CheckBundle(string fault, bool passes,
            MatchVictoryCause cause = MatchVictoryCause.FinalWaveFlipped)
        {
            var setup = PrivateSessionTestDriver.Compose();
            var run = AdmitFreshHost(setup);
            var root = ProjectRoot();
            var directory = Path.Combine(root, "tmp", "8-36d-contract", Guid.NewGuid().ToString("N"));
            var paths = new List<string>();
            var humans = new[] { "blue-player-01", "red-player-01" };
            var bots = new[] { "blue-player-02", "blue-player-03", "red-player-02", "red-player-03" };
            var outcome = MatchOutcome.Won(cause, new TeamId("blue-team"), new TeamId("red-team"));
            using (var host = new PrivateOnlineEvidenceFile(directory, setup.Identity, run, PrivateOnlineEvidenceRole.Host, ""))
            {
                host.Complete(50, outcome, humans, fault == "participants" ? bots.Take(3) : bots);
                paths.Add(host.Path);
            }
            foreach (var seat in humans)
            {
                if (fault == "missing" && seat == humans[1]) continue;
                var identity = setup.Identity;
                if (fault == "build") identity = new PrivateSessionIdentity(identity.ProtocolFamily, identity.ProtocolVersion,
                    identity.BuildIdentity + "-different", identity.SnapshotSchemaVersion, identity.MapId,
                    identity.MapContentRevision, identity.MatchFormatId, identity.ContentDigest);
                using (var client = new PrivateOnlineEvidenceFile(directory, identity,
                    fault == "run" ? AdmitFreshHost(setup) : run, PrivateOnlineEvidenceRole.Client, seat))
                {
                    if (fault != "aborted") client.Complete(fault == "version" ? 49 : 50, outcome);
                    paths.Add(client.Path);
                }
            }
            if (fault == "duplicate") paths[2] = paths[1];
            if (fault == "private-field") File.AppendAllText(paths[1], "PRIVATE_HAND=must-not-be-collected\n");
            // Execute the actual operator checker on files from the shipped typed capture writer.
            // Fixtures are synthetic terminal declarations, not evidence of a played match.
            string Quote(string value) => "'" + value.Replace("'", "''") + "'";
            var command = "& " + Quote(Path.Combine(root, "Tools", "Validate-PrivateOnlineEvidence.ps1")) +
                " -LogPaths @(" + string.Join(",", paths.Select(Quote)) + ") -OutputDirectory " + Quote(Path.Combine(directory, "bundle"));
            var encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(command));
            using (var process = Process.Start(new ProcessStartInfo("powershell.exe", "-NoProfile -EncodedCommand " + encoded)
            { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true }))
            {
                Assert.That(process.WaitForExit(20000), Is.True, "Bounded evidence checker did not exit.");
                var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
                Assert.That(process.ExitCode == 0, Is.EqualTo(passes), output);
                if (passes)
                {
                    Assert.That(File.Exists(Path.Combine(directory, "bundle", "manifest.json")), Is.True);
                    Assert.That(Directory.GetFiles(Path.Combine(directory, "bundle"), "*.log").Length, Is.EqualTo(3));
                }
            }
        }

        private static string AdmitFreshHost(PrivateSessionTestDriver setup)
        {
            var outbox = new RecordingSessionOutbox();
            var host = new PrivateSessionHost(setup.MatchHost, setup.Identity, "", outbox, new CryptoNonceSource());
            host.OnConnected(1, 0);
            host.OnFrame(1, PrivateSessionTestDriver.Decode(PrivateSessionFrameCodec.HelloFrame(
                new SessionHelloMessage(setup.Identity, ""))));
            var frame = outbox.FramesFor(1).Single(value => value.Kind == SessionMessageKind.Welcome);
            Assert.That(PrivateSessionFrameCodec.TryDecodeWelcome(frame.Payload, out var welcome), Is.True);
            return welcome.SessionCorrelation;
        }

        private static string ProjectRoot()
        {
            for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "Tools", "Validate-PrivateOnlineEvidence.ps1")))
                    return directory.FullName;
            throw new InvalidOperationException("Cannot locate the project evidence checker.");
        }

        [TestCase("PLAY - Join Match.bat")]
        [TestCase("HOST - Start Server.bat")]
        public void PackagedLauncherRetainsDistinctLogs(string launcher)
        {
            var root = ProjectRoot();
            var source = File.ReadAllLines(Path.Combine(root, "Tools", "PlaytestLaunchers", launcher));
            Assert.That(source.Count(line => line.StartsWith("start ", StringComparison.Ordinal)), Is.EqualTo(1));
            var directory = Path.Combine(root, "tmp", "8-36d-launcher", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var script = Path.Combine(directory, "probe.bat");
            // Execute the actual launcher's directory-creation prefix without launching a player.
            File.WriteAllLines(script, source.Select(line => line.StartsWith("start ", StringComparison.Ordinal) ? "exit /b 0" : line));
            for (var index = 0; index < 2; index++)
            {
                using (var process = Process.Start(new ProcessStartInfo("cmd.exe", "/c \"" + script + "\"")
                { UseShellExecute = false, CreateNoWindow = true }))
                {
                    Assert.That(process.WaitForExit(20000), Is.True);
                    Assert.That(process.ExitCode, Is.EqualTo(0));
                }
            }
            Assert.That(Directory.GetDirectories(Path.Combine(directory, "Logs", "launches")).Length, Is.EqualTo(2));
        }
    }
}
