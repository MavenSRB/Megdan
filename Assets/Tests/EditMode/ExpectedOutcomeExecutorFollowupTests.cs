using NUnit.Framework;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Regression checks that table probes cannot silently omit requested evidence.</summary>
    public sealed class ExpectedOutcomeExecutorFollowupTests
    {
        private static ExpectedOutcomeExecutor.Report Run(string hero, string json)
        {
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"" + hero + "\",\"cases\":[]}");
            var report = ExpectedOutcomeExecutor.Run(document, (JsonObject)ExpectedOutcomeJson.Parse(json),
                "executor-followup/probe");
            TestContext.WriteLine(report.Trace);
            return report;
        }

        private static string SpellBreakProbe(string followupFields)
        {
            return @"{
              ""id"":""probe"",""card"":""Spell Break"",""action"":""Skill"",
              ""setup"":{
                ""pieces"":[
                  {""ref"":""enemy-hero-1"",""where"":""distance 3 from actor""},
                  {""ref"":""enemy-hero-2"",""where"":""adjacent to enemy-hero-1""}],
                ""enemy-hero-1"":{""hero"":""Wasp"",""hand"":[""Magnetic Dagger"",""Static Barrier"",""Shock"",""Lift Up"",""Stop Projectiles""]}
              },
              ""answers"":[],
              ""expect"":[{""offered"":true}],
              ""follow-up"":{""hero"":""enemy-hero-1"",""card"":""Lift Up"",""action"":""Skill""," +
                followupFields + "}}";
        }

        [Test]
        public void OfferOnlyCase_StillRunsItsFollowup_AndDetectsTheWrongOfferExpectation()
        {
            var report = Run("Arien", SpellBreakProbe(@"""offered"":true,""answers"":[]"));
            Assert.That(report.Failures, Has.Some.Contains("follow-up offered").And.Contains("expected"));
        }

        [Test]
        public void OfferOnlyCase_PerformsTheCard_BeforeCheckingItsRestrictedFollowup()
        {
            var report = Run("Arien", SpellBreakProbe(@"""offered"":false,""answers"":[]"));
            Assert.That(report.Failures, Is.Empty, string.Join(" | ", report.Failures));
            Assert.That(report.Trace, Does.Contain("SkillUsed"),
                "A passing offer probe must include execution of Spell Break, not just its initial offer.");
        }

        [TestCase("1")]
        [TestCase("\"false\"")]
        [TestCase("null")]
        public void FollowupOffered_RejectsNonBooleanValues(string value)
        {
            var report = Run("Arien", SpellBreakProbe("\"offered\":" + value + ",\"answers\":[]"));
            Assert.That(report.Failures, Has.Some.Contains("follow-up").And.Contains("offered"));
        }

        [TestCase("\"decline\"")]
        [TestCase("{}")]
        [TestCase("[1]")]
        [TestCase("[null]")]
        public void FollowupAnswers_RejectsAnythingOtherThanAStringArray(string value)
        {
            var report = Run("Arien", SpellBreakProbe("\"offered\":false,\"answers\":" + value));
            Assert.That(report.Failures, Has.Some.Contains("follow-up").And.Contains("answer"));
        }

        [Test]
        public void DefenseCase_RejectsUnsupportedFollowup_InsteadOfIgnoringIt()
        {
            var report = Run("Wasp", @"{
              ""id"":""probe"",""card"":""Stop Projectiles"",""action"":""Defend"",
              ""setup"":{
                ""pieces"":[{""ref"":""enemy-hero-1"",""where"":""adjacent to actor""}],
                ""enemy-hero-1"":{""hero"":""Arien""},
                ""attack"":{""by"":""enemy-hero-1"",""value"":4}},
              ""expect"":[{""ref"":""actor"",""is"":""defeated""}],
              ""follow-up"":{""hero"":""enemy-hero-1"",""card"":""Liquid Leap"",""action"":""Skill"",""answers"":[]}
            }");
            Assert.That(report.Failures, Has.Some.Contains("follow-up").And.Contains("Defen"));
        }

        [TestCase("")]
        [TestCase(" ")]
        public void BlankMarkerName_DoesNotMatchAnAttachedMarker(string marker)
        {
            var report = Run("Tigerclaw", @"{
              ""id"":""probe"",""card"":""Poisoned Dagger"",""action"":""Skill"",
              ""setup"":{""actor"":{""level"":2},
                ""pieces"":[{""ref"":""enemy-hero-1"",""where"":""distance 2 from actor""}]},
              ""answers"":[""enemy-hero-1""],
              ""expect"":[{""ref"":""enemy-hero-1"",""marker"":""" + marker + @"""}]
            }");
            Assert.That(report.Failures, Has.Some.Contains("marker"));
        }
    }
}
