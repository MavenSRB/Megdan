using NUnit.Framework;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ExpectedOutcomeExecutorAttackSelectorTests
    {
        [Test]
        public void NamedPrintedAttack_RejectsMismatchedValue()
        {
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Snorri\",\"cases\":[]}");
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(@"{
              ""id"":""wrong-attack-value"",""card"":""Oath of Endurance"",""action"":""Defend"",
              ""setup"":{""pieces"":[{""ref"":""enemy-hero-1"",""where"":""adjacent to actor""}],
                ""enemy-hero-1"":{""hero"":""Sabina""},
                ""attack"":{""by"":""enemy-hero-1"",""card"":""Point Blank Shot"",
                  ""value"":5,""ranged"":true}},
              ""expect"":[{""offered"":true},{""ref"":""actor"",""is"":""not defeated""}]
            }");
            var report = ExpectedOutcomeExecutor.Run(document, entry, "named-attack/wrong-value");
            Assert.That(report.Failures, Has.Exactly(1).Contains("has no named Point Blank Shot whose primary Attack prints 5"));
        }
    }
}
