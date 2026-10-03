using NUnit.Framework;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ExpectedOutcomeExecutorOwnedObstacleTests
    {
        [TestCase(true)]
        [TestCase(false)]
        public void PlacedOwnedObstacle_IsObservedAtItsActualPosition(bool correct)
        {
            var where = correct ? "adjacent to actor" : "distance 4 from actor";
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Trinkets\",\"cases\":[]}");
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(@"{
              ""id"":""placed-obstacle"",""card"":""Rapid Redeployment"",""action"":""Skill"",
              ""answers"":[""option: Move up to 3 spaces and place the Turret into a space adjacent to you; it counts as an obstacle"",
                ""distance 0"",""space: adjacent to actor""],
              ""expect"":[{""owned-obstacle"":{""kind"":""trinkets-turret"",""owner"":""actor"",""at"":""" + where + @"""}}]
            }");
            var report = ExpectedOutcomeExecutor.Run(document, entry, "owned-obstacle/probe");
            TestContext.WriteLine(report.Trace);
            if (correct) Assert.That(report.Failures, Is.Empty, string.Join(" | ", report.Failures));
            else Assert.That(report.Failures, Has.Exactly(1).Contains("it is at"));
        }

        [TestCase("\"at\":\"adjacent to actor\",\"on-board\":true")]
        [TestCase("\"on-board\":true,\"extra\":1")]
        public void OwnedObstacleObservation_RejectsAmbiguousOrUnknownFields(string fields)
        {
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Trinkets\",\"cases\":[]}");
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(
                "{\"id\":\"bad-obstacle\",\"card\":\"Rapid Redeployment\",\"action\":\"Hold\",\"answers\":[]," +
                "\"expect\":[{\"owned-obstacle\":{\"kind\":\"trinkets-turret\",\"owner\":\"actor\"," + fields + "}}]}");
            var report = ExpectedOutcomeExecutor.Run(document, entry, "owned-obstacle/invalid");
            Assert.That(report.Failures, Has.Exactly(1).Contains("unsupported owned-obstacle expectation"));
        }

        [Test]
        public void StagedOwnedObstacle_UsesItsOwnerIdentity_AndCanBeRemoved()
        {
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Trinkets\",\"cases\":[]}");
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(@"{
              ""id"":""remove-obstacle"",""card"":""Early Prototype"",""action"":""Skill"",
              ""setup"":{""pieces"":[
                {""ref"":""obstacle-1"",""kind"":""owned"",""identity-kind"":""trinkets-turret"",""owner"":""actor"",""where"":""adjacent to actor""},
                {""ref"":""enemy-hero-1"",""where"":""adjacent to actor and obstacle-1""}]},
              ""answers"":[""enemy-hero-1""],
              ""expect"":[{""ref"":""actor"",""swapped-with"":""enemy-hero-1""},
                {""owned-obstacle"":{""kind"":""trinkets-turret"",""owner"":""actor"",""on-board"":false}}]
            }");
            var report = ExpectedOutcomeExecutor.Run(document, entry, "owned-obstacle/probe");
            TestContext.WriteLine(report.Trace);
            Assert.That(report.Failures, Is.Empty, string.Join(" | ", report.Failures));
        }
    }
}
