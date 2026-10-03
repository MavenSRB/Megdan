using NUnit.Framework;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ExpectedOutcomeExecutorNumberedOptionsTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void NumberedPrintedBullet_IsMatchedByItsWords_AndUnknownWordsAreRefused(bool known)
        {
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Rowenna\",\"cases\":[]}");
            var option = known ? "Place an enemy minion in radius into a space adjacent to you" : "Invent a third option";
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(@"{
              ""id"":""numbered-option"",""card"":""Melee"",""action"":""Movement"",
              ""setup"":{""pieces"":[{""ref"":""enemy-hero-1"",""where"":""adjacent to actor""},
                {""ref"":""enemy-minion-1"",""where"":""distance 2 from actor""}]},
              ""answers"":[""distance 0"",""option: " + option + @""",""enemy-minion-1"",""place: adjacent to actor""],
              ""expect"":[{""ref"":""enemy-minion-1"",""at"":""adjacent to actor""}]
            }");
            var report = ExpectedOutcomeExecutor.Run(document, entry, "executor-numbered/probe");
            TestContext.WriteLine(report.Trace);
            if (known) Assert.That(report.Failures, Is.Empty, string.Join(" | ", report.Failures));
            else Assert.That(report.Failures, Has.Exactly(1).Contains("names no single bullet"));
        }
    }
}
