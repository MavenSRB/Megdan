using NUnit.Framework;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Malformed source-facing staging must refuse rather than silently weaken a fixture.</summary>
    public sealed class ExpectedOutcomeExecutorSpawnTests
    {
        private static ExpectedOutcomeExecutor.Report Run(string setup)
        {
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Dodger\",\"cases\":[]}");
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(
                "{\"id\":\"probe\",\"card\":\"Dark Ritual\",\"action\":\"Skill\",\"setup\":" + setup +
                ",\"answers\":[],\"expect\":[{\"ref\":\"actor\",\"coins\":1}]}");
            var report = ExpectedOutcomeExecutor.Run(document, entry, "executor-spawns/probe");
            TestContext.WriteLine(report.Trace);
            return report;
        }

        [TestCase("\"2\"")]
        [TestCase("null")]
        [TestCase("1.5")]
        public void SpawnCount_RejectsNonIntegralNumericValues(string value)
        {
            var report = Run("{\"empty-minion-spawns\":[{\"count\":" + value + ",\"within\":3}]}");
            Assert.That(report.Failures, Has.Some.Contains("spawn count"));
        }

        [TestCase("\"3\"")]
        [TestCase("null")]
        [TestCase("1.5")]
        public void SpawnWithin_RejectsNonIntegralNumericValues(string value)
        {
            var report = Run("{\"empty-minion-spawns\":[{\"count\":2,\"within\":" + value + "}]}");
            Assert.That(report.Failures, Has.Some.Contains("spawn within"));
        }

        [TestCase(-1, 3)]
        [TestCase(2, -1)]
        public void SpawnConstraint_RejectsNegativeCountOrDistance(int count, int within)
        {
            var report = Run("{\"empty-minion-spawns\":[{\"count\":" + count + ",\"within\":" + within + "}]}");
            Assert.That(report.Failures, Has.Some.Contains("invalid empty-minion-spawns"));
        }

        [TestCase("1")]
        [TestCase("true")]
        [TestCase("null")]
        [TestCase("{}")]
        public void SpawnOrigin_RejectsExplicitNonStringRatherThanDefaultingToActor(string value)
        {
            var report = Run("{\"empty-minion-spawns\":[{\"count\":2,\"within\":3,\"of\":" + value + "}]}");
            Assert.That(report.Failures, Has.Some.Contains("spawn origin"));
        }

        [TestCase("")]
        [TestCase("unknown-hero")]
        public void SpawnOrigin_RejectsUnknownReference(string reference)
        {
            var report = Run("{\"empty-minion-spawns\":[{\"count\":2,\"within\":3,\"of\":\"" + reference + "\"}]}");
            Assert.That(report.Failures, Has.Some.Contains("invalid empty-minion-spawns"));
        }

        [Test]
        public void ReserveMinion_RequiresAnExplicitReference()
        {
            var report = Run("{\"reserve-minions\":[{\"type\":\"melee\"}]}");
            Assert.That(report.Failures, Has.Some.Contains("reserve minion reference"));
        }

        [Test]
        public void OmittedSpawnOrigin_DefaultsToActor_AndRunsTheSourceOutcome()
        {
            var report = Run("{\"empty-minion-spawns\":[{\"count\":2,\"within\":3}]}");
            Assert.That(report.Failures, Is.Empty, string.Join(" | ", report.Failures));
            Assert.That(report.Trace, Does.Contain("verified empty minion spawns: 2"));
            Assert.That(report.Trace, Does.Contain("SkillUsed"));
        }
    }
}
