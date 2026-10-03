using System.Collections.Generic;
using NUnit.Framework;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Each positive observation has a deliberate wrong-value control against the same action.</summary>
    public sealed class ExpectedOutcomeExecutorAssertionsTests
    {
        private static ExpectedOutcomeExecutor.Report Run(string hero, string body, string expectation, string checkpoint = null)
        {
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"" + hero + "\",\"cases\":[]}");
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(body);
            entry.Add(new KeyValuePair<string, object>("expect", ExpectedOutcomeJson.Parse("[" + expectation + "]")));
            if (checkpoint != null)
                entry.Add(new KeyValuePair<string, object>("checkpoint", ExpectedOutcomeJson.Parse(checkpoint)));
            var report = ExpectedOutcomeExecutor.Run(document, entry, "executor-assertions/probe");
            TestContext.WriteLine(report.Trace);
            return report;
        }

        private static void Verify(ExpectedOutcomeExecutor.Report report, bool correct, string mismatch)
        {
            if (correct)
                Assert.That(report.Failures, Is.Empty, string.Join(" | ", report.Failures));
            else
            {
                Assert.That(report.Failures, Has.Count.EqualTo(1), string.Join(" | ", report.Failures));
                Assert.That(report.Failures, Has.Some.Contains(mismatch),
                    "The wrong assertion must fail on its observed fact, not a setup refusal.");
            }
        }

        // Source fixture: misa-living-tornado-swap. Exact exchange, not a movement-distance proxy.
        [TestCase(true)]
        [TestCase(false)]
        public void SwappedWith_ChecksBothInitialPositions(bool correct)
        {
            var expectation = correct ? @"{""ref"":""enemy-hero-1"",""swapped-with"":""friendly-hero-1""}"
                : @"{""ref"":""enemy-hero-1"",""swapped-with"":""actor""}";
            Verify(Run("Misa", @"{""id"":""probe"",""card"":""Living Tornado"",""action"":""Skill"",""setup"":{""pieces"":[{""ref"":""enemy-hero-1"",""where"":""distance 3 from actor""},{""ref"":""friendly-hero-1"",""where"":""distance 3 from actor""}]},""answers"":[""enemy-hero-1"",""friendly-hero-1""]}", expectation), correct, "did not exchange their initial positions");
        }

        // Source fixture: dodger-darkest-ritual-unlocked-grants-attack-item.
        [TestCase(true)]
        [TestCase(false)]
        public void ItemCount_ChecksTheGrantedItem(bool correct)
        {
            var expectation = correct ? @"{""ref"":""actor"",""item-count"":{""Attack +1"":1}}"
                : @"{""ref"":""actor"",""item-count"":{""Attack +1"":2}}";
            Verify(Run("Dodger", @"{""id"":""probe"",""card"":""Darkest Ritual"",""action"":""Skill"",""setup"":{""actor"":{""level"":8,""coins"":0}},""answers"":[]}", expectation), correct, "Attack +1 count is 1");
        }

        // Dread Razor's adjacent-unit bullet performs one complete Attack.
        [TestCase(true)]
        [TestCase(false)]
        public void AttackCount_ChecksCompletedAttacks(bool correct)
        {
            var expectation = correct ? @"{""ref"":""actor"",""attack-count"":1}"
                : @"{""ref"":""actor"",""attack-count"":2}";
            Verify(Run("Dodger", @"{""id"":""probe"",""card"":""Dread Razor"",""action"":""Attack"",""setup"":{""pieces"":[{""ref"":""enemy-hero-1"",""where"":""adjacent to actor""}],""enemy-hero-1"":{""defends-with"":""none""}},""answers"":[""option: Target a unit adjacent to you."",""enemy-hero-1""]}", expectation), correct, "completed 1 attacks");
        }

        // Source fixture: silverarrow-grasping-roots-inside; Focus is reduced from 2 to 1.
        [TestCase(true)]
        [TestCase(false)]
        public void MovementOfferedMax_ChecksTheObservedChoiceBound(bool correct)
        {
            var expectation = correct ? @"{""ref"":""enemy-hero-1"",""movement-offered-max"":1}"
                : @"{""ref"":""enemy-hero-1"",""movement-offered-max"":2}";
            Verify(Run("Silverarrow", @"{""id"":""probe"",""card"":""Grasping Roots"",""action"":""Skill"",""setup"":{""pieces"":[{""ref"":""enemy-hero-1"",""where"":""distance 2 from actor""}],""enemy-hero-1"":{""hero"":""Misa""}},""answers"":[],""follow-up"":{""hero"":""enemy-hero-1"",""card"":""Focus"",""action"":""Movement"",""answers"":[""away from actor"",""distance 1""],""offered"":true}}", expectation), correct, "maximum offered walk cost was 1");
        }

        // Source fixture: silverarrow-trailblazer-ally-crosses-obstacle.
        [TestCase(true)]
        [TestCase(false)]
        public void MovedThrough_ChecksTheCommittedWalk(bool correct)
        {
            var expectation = correct ? @"{""ref"":""friendly-hero-1"",""moved-through"":""obstacle-1""}"
                : @"{""ref"":""friendly-hero-1"",""moved-through"":""actor""}";
            Verify(Run("Silverarrow", @"{""id"":""probe"",""card"":""Trailblazer"",""action"":""Skill"",""setup"":{""pieces"":[{""ref"":""friendly-hero-1"",""where"":""distance 3 from actor in a straight line""},{""ref"":""obstacle-1"",""where"":""distance 2 from actor in a straight line""}],""friendly-hero-1"":{""hero"":""Misa""}},""answers"":[""decline""],""follow-up"":{""hero"":""friendly-hero-1"",""card"":""Focus"",""action"":""Movement"",""answers"":[""toward obstacle-1"",""distance 2""],""offered"":true}}", expectation), correct, "did not traverse the named initial space");
        }

        [TestCase("null")]
        [TestCase("true")]
        [TestCase("1")]
        [TestCase("\"after-everything\"")]
        public void Checkpoint_RejectsMalformedOrUnknownValue(string checkpoint)
        {
            var report = Run("Dodger", @"{""id"":""probe"",""card"":""Darkest Ritual"",""action"":""Skill"",""setup"":{""actor"":{""level"":8,""coins"":0}},""answers"":[]}", @"{""ref"":""actor"",""item-count"":{""Attack +1"":1}}", checkpoint);
            Assert.That(report.Failures, Has.Some.Contains("checkpoint must be"));
        }

        [TestCase("\"end-of-turn\"")]
        [TestCase("\"before-minion-battle\"")]
        public void Defense_RejectsCheckpointInsteadOfIgnoringIt(string checkpoint)
        {
            var report = Run("Dodger", @"{""id"":""probe"",""card"":""Shield of Decay"",""action"":""Defend"",""setup"":{""pieces"":[{""ref"":""enemy-hero-1"",""where"":""adjacent to actor""}],""attack"":{""by"":""enemy-hero-1"",""value"":5}},""answers"":[]}", @"{""ref"":""actor"",""is"":""defeated""}", checkpoint);
            Assert.That(report.Failures, Has.Some.Contains("checkpoint").And.Contains("non-Defense"));
        }
    }
}
