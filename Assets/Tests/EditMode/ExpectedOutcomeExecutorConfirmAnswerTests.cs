using System.Collections.Generic;
using NUnit.Framework;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Explicit confirm answers consume only confirm-or-decline questions.</summary>
    public sealed class ExpectedOutcomeExecutorConfirmAnswerTests
    {
        private static ExpectedOutcomeExecutor.Report Run(string body, string expectation)
        {
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Widget\",\"cases\":[]}");
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(body);
            entry.Add(new KeyValuePair<string, object>("expect", ExpectedOutcomeJson.Parse("[" + expectation + "]")));
            var report = ExpectedOutcomeExecutor.Run(document, entry, "executor-confirm/probe");
            TestContext.WriteLine(report.Trace);
            return report;
        }

        [Test]
        public void Confirm_ConsumesFirstPrompt_ThenDeclineConsumesSecond()
        {
            var report = Run(
                @"{""id"":""probe"",""card"":""Airborne Assault"",""action"":""Attack"",
                    ""setup"":{""pieces"":[
                        {""ref"":""token-1"",""kind"":""Pyro"",""owner"":""actor"",""where"":""distance 2 from actor""},
                        {""ref"":""enemy-hero-1"",""where"":""adjacent to token-1""}],
                        ""enemy-hero-1"":{""defends-with"":""none""}},
                    ""answers"":[""confirm"",""enemy-hero-1"",""decline""]}",
                @"{""ref"":""actor"",""swapped-with"":""token-1""}");
            Assert.That(report.Failures, Is.Empty, string.Join(" | ", report.Failures));
        }

        [Test]
        public void Confirm_DoesNotAnswerSelectOption()
        {
            var report = Run(
                @"{""id"":""probe"",""card"":""Dragon Bond"",""action"":""Skill"",
                    ""setup"":{},""answers"":[""confirm""]}",
                @"{""offered"":true}");
            Assert.That(report.Failures, Has.Count.EqualTo(1), string.Join(" | ", report.Failures));
            Assert.That(report.Failures, Has.Some.Contains("does not answer"),
                "Explicit confirm must be rejected by a non-confirm question, not by the phrase parser.");
        }
    }
}
