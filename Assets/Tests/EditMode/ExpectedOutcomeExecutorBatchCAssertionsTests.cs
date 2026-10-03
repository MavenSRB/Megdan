using System.Collections.Generic;
using NUnit.Framework;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Positive observations and deliberate wrong-fact controls for the batch C executor vocabulary.</summary>
    public sealed class ExpectedOutcomeExecutorBatchCAssertionsTests
    {
        private static ExpectedOutcomeExecutor.Report Run(string hero, string body, string expectation)
        {
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"" + hero + "\",\"cases\":[]}");
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(body);
            entry.Add(new KeyValuePair<string, object>("expect", ExpectedOutcomeJson.Parse("[" + expectation + "]")));
            var report = ExpectedOutcomeExecutor.Run(document, entry, "executor-batch-c/probe");
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
                    "A deliberate wrong fact must fail on the observation, not on setup.");
            }
        }

        // swift-bounce-crosses-obstacle: the actor's final position is measured from its original space.
        [TestCase(true)]
        [TestCase(false)]
        public void AtInitial_BounceUsesOriginalActorGeometry(bool correct)
        {
            var distance = correct ? 2 : 3;
            var expectation = "{\"ref\":\"actor\",\"at-initial\":\"distance " + distance +
                " from actor in a straight line through obstacle-1\"}";
            Verify(Run("Swift",
                @"{""id"":""probe"",""card"":""Bounce"",""action"":""Skill"",
                    ""setup"":{""pieces"":[{""ref"":""obstacle-1"",""where"":""adjacent to actor""}]},
                    ""answers"":[""toward obstacle-1"",""distance 2""]}",
                expectation), correct, "it is at");
        }

        // Batch D: equality to a named initial position must be expressible after a swap.
        [TestCase(true)]
        [TestCase(false)]
        public void AtInitial_ZeroDistanceChecksTheExactOriginalSpace(bool correct)
        {
            var reference = correct ? "actor" : "enemy-hero-1";
            var expectation = "{\"ref\":\"enemy-hero-1\",\"at-initial\":\"distance 0 from " + reference + "\"}";
            Verify(Run("Brynn",
                @"{""id"":""probe"",""card"":""Tread Lightly"",""action"":""Skill"",
                    ""setup"":{""pieces"":[{""ref"":""enemy-hero-1"",""where"":""adjacent to actor""}]},
                    ""answers"":[""enemy-hero-1""]}", expectation), correct, "it is at");
        }

        // cutter-brace-for-impact-charge-through-obstacle: charge changes the actor's coordinate.
        [TestCase(true)]
        [TestCase(false)]
        public void AtInitial_ChargeUsesOriginalActorGeometry(bool correct)
        {
            var distance = correct ? 3 : 2;
            var expectation = "{\"ref\":\"actor\",\"at-initial\":\"distance " + distance +
                " from actor in a straight line through obstacle-1\"}";
            Verify(Run("Cutter",
                @"{""id"":""probe"",""card"":""Brace for Impact"",""action"":""Skill"",
                    ""setup"":{""actor"":{""level"":1},""pieces"":[
                        {""ref"":""obstacle-1"",""where"":""adjacent to actor""},
                        {""ref"":""enemy-hero-1"",""where"":""distance 3 beyond obstacle-1 from actor""}]},
                    ""answers"":[""toward enemy-hero-1"",""distance 3"",""enemy-hero-1""]}",
                expectation), correct, "it is at");
        }

        private const string TotemBoard = @"{""id"":""probe"",""card"":""Ice Blast"",""action"":""Hold"",
            ""setup"":{""pieces"":[
                {""ref"":""token-1"",""kind"":""tali-totem-ancestral"",""owner"":""actor"",""where"":""adjacent to actor""},
                {""ref"":""token-2"",""kind"":""tali-totem-venerated"",""owner"":""enemy"",""where"":""distance 2 from actor""}]},
            ""answers"":[]}";

        // Different owners have independent supplies; the printed name spans both token-definition roles.
        [TestCase(true)]
        [TestCase(false)]
        public void Tokens_PrintedTotemNameAggregatesBothRoles(bool correct)
        {
            Verify(Run("Tali", TotemBoard, "{\"tokens\":\"Totem\",\"on-board\":" +
                (correct ? 2 : 1) + "}"), correct, "2 on the board");
        }

        [Test]
        public void Tokens_UnknownPrintedNameRefusesInsteadOfCountingZero()
        {
            var report = Run("Tali", TotemBoard, @"{""tokens"":""Unprinted Totem"",""on-board"":0}");
            Assert.That(report.Failures, Has.Count.EqualTo(1), string.Join(" | ", report.Failures));
            Assert.That(report.Failures, Has.Some.Contains("unsupported token kind"));
        }

        [TestCase("None")]
        [TestCase("1")]
        [TestCase("Ultraviolet")]
        public void Color_RejectsNonPrintedValues(string color)
        {
            var report = Run("Tali",
                @"{""id"":""probe"",""card"":""Commune with Spirits"",""action"":""Skill"",
                    ""answers"":[""friendly-hero-1"",""color: " + color + @"""]}",
                @"{""offered"":true}");
            Assert.That(report.Failures, Has.Count.EqualTo(1), string.Join(" | ", report.Failures));
            Assert.That(report.Failures, Has.Some.Contains("unsupported printed color"));
        }

        [Test]
        public void Color_DoesNotAnswerANonColorTargetQuestion()
        {
            var report = Run("Tali",
                @"{""id"":""probe"",""card"":""Commune with Spirits"",""action"":""Skill"",
                    ""answers"":[""color: Purple""]}", @"{""offered"":true}");
            Assert.That(report.Failures, Has.Count.EqualTo(1), string.Join(" | ", report.Failures));
            Assert.That(report.Failures, Has.Some.Contains("does not answer"));
        }

        // bain-close-call-blocks-with-bounty: an actual previous attack establishes the required marker.
        [TestCase(true)]
        [TestCase(false)]
        public void StandingAttack_BeforeDefensePreservesBountyAndDefenseChoice(bool correct)
        {
            var markerHolder = correct ? "actor" : "enemy-hero-1";
            var expectation = "{\"ref\":\"actor\",\"is\":\"not defeated\"}," +
                "{\"ref\":\"" + markerHolder + "\",\"marker\":\"Bounty\"}";
            Verify(Run("Bain",
                @"{""id"":""probe"",""card"":""Close Call"",""action"":""Defend"",
                    ""setup"":{""actor"":{""level"":1},""pieces"":[
                        {""ref"":""enemy-hero-1"",""where"":""adjacent to actor""}],
                        ""enemy-hero-1"":{""defends-with"":4},
                        ""standing"":[""actor played Dead or Alive last turn""],
                        ""attack"":{""by"":""enemy-hero-1"",""value"":4,""ranged"":false}},
                    ""answers"":[]}",
                expectation), correct, "no such marker is attached");
        }
    }
}
