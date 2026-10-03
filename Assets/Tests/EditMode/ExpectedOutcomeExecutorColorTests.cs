using NUnit.Framework;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ExpectedOutcomeExecutorColorTests
    {
        [Test]
        public void PrintedColorAnswer_UsesTheTypedColorQuestion()
        {
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Tali\",\"cases\":[]}");
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(@"{
              ""id"":""color-probe"",""card"":""Commune with Spirits"",""action"":""Skill"",
              ""answers"":[""friendly-hero-1"",""color: Purple""],
              ""expect"":[{""offered"":true},{""ref"":""actor"",""coins"":0}]
            }");
            var report = ExpectedOutcomeExecutor.Run(document, entry, "executor-color/probe");
            TestContext.WriteLine(report.Trace);
            Assert.That(report.Failures, Is.Empty, string.Join(" | ", report.Failures));
            Assert.That(report.Trace, Does.Contain("color: Purple"));
        }
    }
}
