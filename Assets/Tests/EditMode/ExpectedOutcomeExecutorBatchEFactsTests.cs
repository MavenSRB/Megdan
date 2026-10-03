using NUnit.Framework;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ExpectedOutcomeExecutorBatchEFactsTests
    {
        private static ExpectedOutcomeExecutor.Report Run(string hero, string body)
        {
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"" + hero + "\",\"cases\":[]}");
            var report = ExpectedOutcomeExecutor.Run(document,
                (JsonObject)ExpectedOutcomeJson.Parse(body), "batch-e/probe");
            TestContext.WriteLine(report.Trace);
            return report;
        }

        [TestCase(true)]
        [TestCase(false)]
        public void UnresolvedInitiative_ReadsTheRewrittenPrintedValue(bool correct)
        {
            var report = Run("Hanu", @"{
              ""id"":""initiative"",""card"":""Hurry up!"",""action"":""Skill"",
              ""setup"":{""pieces"":[{""ref"":""friendly-hero-1"",""where"":""adjacent to actor""}]},
              ""answers"":[""friendly-hero-1""],
              ""expect"":[{""ref"":""friendly-hero-1"",""unresolved-initiative"":" + (correct ? "11" : "12") + @"}]
            }");
            Check(report, correct, "printed Initiative is");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void FollowupAction_RecordsActualDecidingHero(bool correct)
        {
            var report = Run("Hanu", @"{
              ""id"":""delegation"",""card"":""Hurry up!"",""action"":""Skill"",
              ""setup"":{""actor"":{""level"":8},""friendly-hero-1"":{""hero"":""Wasp""},
                ""pieces"":[{""ref"":""friendly-hero-1"",""where"":""adjacent to actor""},
                  {""ref"":""enemy-minion-1"",""where"":""adjacent to friendly-hero-1""}]},
              ""answers"":[""friendly-hero-1""],
              ""follow-up"":{""hero"":""friendly-hero-1"",""card"":""Shock"",""action"":""Attack"",
                ""answers"":[""enemy-minion-1""],""offered"":true},
              ""expect"":[{""ref"":""friendly-hero-1"",""action-chosen-by"":""" +
                (correct ? "actor" : "friendly-hero-1") + @"""}]
            }");
            Check(report, correct, "action was chosen by");
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TieBreaker_IsExplicitAndObserved(bool correct)
        {
            var report = Run("Ignatia", @"{
              ""id"":""coin"",""card"":""Chaos Bolt"",""action"":""Attack"",
              ""setup"":{""tie-breaker"":""Blue"",""pieces"":[{""ref"":""enemy-minion-1"",""where"":""adjacent to actor""}]},
              ""answers"":[""enemy-minion-1""],
              ""expect"":[{""ref"":""enemy-minion-1"",""is"":""defeated""},
                {""tie-breaker"":""" + (correct ? "Blue" : "Red") + @"""}]
            }");
            Check(report, correct, "Tie Breaker is");
        }

        [Test]
        public void PrintedTextFace_AnswersOnlyItsTypedQuestion()
        {
            var report = Run("Ignatia", @"{
              ""id"":""text-face"",""card"":""Equilibrium"",""action"":""Skill"",
              ""setup"":{""tie-breaker"":""Red"",""pieces"":[{""ref"":""enemy-minion-1"",""where"":""adjacent to actor""}]},
              ""answers"":[],
              ""follow-up"":{""hero"":""actor"",""turn"":""next"",""card"":""Chaos Bolt"",""action"":""Attack"",
                ""answers"":[""text-face: Sun"",""enemy-minion-1""],""offered"":true},
              ""expect"":[{""ref"":""enemy-minion-1"",""is"":""defeated""},{""tie-breaker"":""Red""}]
            }");
            Check(report, true, "");
        }

        [Test]
        public void PrintedTextFace_DoesNotAnswerATargetQuestion()
        {
            var report = Run("Ignatia", @"{
              ""id"":""wrong-question"",""card"":""Chaos Bolt"",""action"":""Attack"",
              ""setup"":{""tie-breaker"":""Blue"",""pieces"":[{""ref"":""enemy-minion-1"",""where"":""adjacent to actor""},
                {""ref"":""enemy-minion-2"",""where"":""adjacent to actor""}]},
              ""answers"":[""text-face: Sun""],""expect"":[{""offered"":true}]
            }");
            Check(report, false, "does not answer");
        }

        [TestCase("Orange")]
        [TestCase("1")]
        public void TieBreaker_RejectsUnknownSides(string side)
        {
            var report = Run("Ignatia", "{\"id\":\"bad-coin\",\"card\":\"Chaos Bolt\",\"action\":\"Hold\"," +
                "\"setup\":{\"tie-breaker\":\"" + side + "\"},\"answers\":[],\"expect\":[{\"offered\":true}]}");
            Check(report, false, "unsupported tie-breaker");
        }

        [Test]
        public void ActionChooser_RequiresAnObservedStart()
        {
            var report = Run("Hanu", @"{
              ""id"":""missing-start"",""card"":""Hurry up!"",""action"":""Hold"",
              ""answers"":[],""expect"":[{""ref"":""friendly-hero-1"",""action-chosen-by"":""actor""}]
            }");
            Check(report, false, "no accepted action start");
        }

        private static void Check(ExpectedOutcomeExecutor.Report report, bool correct, string fragment)
        {
            if (correct) Assert.That(report.Failures, Is.Empty, string.Join(" | ", report.Failures));
            else Assert.That(report.Failures, Has.Exactly(1).Contains(fragment));
        }
    }
}
