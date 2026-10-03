using System;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ExpectedOutcomeExecutorRuneTests
    {
        private static readonly JsonObject Document =
            (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Snorri\",\"cases\":[]}");

        [TestCase(true)]
        [TestCase(false)]
        public void InscribeAssignments_AreReadFromRealMarkerSlots(bool correct)
        {
            var active = correct ? "[2]" : "[1]";
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(@"{
              ""id"":""rune-slot-probe"",""card"":""Inscribe the Runes"",""action"":""Skill"",
              ""answers"":[""slot: 1, rune 1"",""slot: 2, rune 2"",
                ""slot: 3, rune 3"",""slot: 4, rune 4""],
              ""expect"":[{""offered"":true},
                {""runes"":{""assigned"":4,""slots"":[1,2,3,4],""active"":" + active + @"}}]
            }");
            var report = ExpectedOutcomeExecutor.Run(Document, entry, "rune-slot/probe");
            TestContext.WriteLine(report.Trace);
            if (correct) Assert.That(report.Failures, Is.Empty, string.Join(" | ", report.Failures));
            else Assert.That(report.Failures, Has.Exactly(1).Contains("active runes are"));
        }

        [Test]
        public void StagedRuneSlots_RefuseDuplicateGlyphs()
        {
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(@"{
              ""id"":""duplicate-runes"",""card"":""Runic Dagger"",""action"":""Attack"",
              ""setup"":{""standing"":[""actor played Inscribe the Runes last turn""],
                ""rune-slots"":[1,1,2,3],
                ""pieces"":[{""ref"":""enemy-minion-1"",""where"":""adjacent to actor""}]},
              ""answers"":[""enemy-minion-1""],
              ""expect"":[{""offered"":true},{""ref"":""enemy-minion-1"",""is"":""defeated""}]
            }");
            var report = ExpectedOutcomeExecutor.Run(Document, entry, "rune-slot/duplicate");
            Assert.That(report.Failures, Has.Exactly(1).Contains("unsupported rune-slots permutation"));
        }

        [Test]
        public void RuneSlots_RefuseWithoutAPriorAssignmentAction()
        {
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(@"{
              ""id"":""unconsumed-runes"",""card"":""Runic Dagger"",""action"":""Attack"",
              ""setup"":{""rune-slots"":[1,2,3,4],
                ""pieces"":[{""ref"":""enemy-minion-1"",""where"":""adjacent to actor""}]},
              ""answers"":[""enemy-minion-1""],
              ""expect"":[{""offered"":true},{""ref"":""enemy-minion-1"",""is"":""defeated""}]
            }");
            var report = ExpectedOutcomeExecutor.Run(Document, entry, "rune-slot/unconsumed");
            Assert.That(report.Failures, Has.Exactly(1).Contains("not used by a real prior rune-slot assignment"));
        }

        [Test]
        public void RuneAnswer_DoesNotSelectAnUnrelatedIntegerQuestion()
        {
            var candidates = Enumerable.Range(1, 4)
                .Select(number => EffectChoiceCandidate.Value_(EffectValue.Integer(number))).ToArray();
            var unrelated = new PendingEffectChoice(
                new EffectResolutionId("unrelated-integer-effect"),
                new EffectChoiceId("choose-number"), new EffectNodeId("choose-number"),
                new HeroId("hero-snorri"), EffectChoiceKind.SelectTarget,
                candidates, 1, 1);
            var runType = typeof(ExpectedOutcomeExecutor).GetNestedType("CaseRun", BindingFlags.NonPublic);
            var shapeType = runType.GetNestedType("AnswerShape", BindingFlags.NonPublic);
            var run = Activator.CreateInstance(runType,
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { Document,
                    (JsonObject)ExpectedOutcomeJson.Parse("{\"id\":\"probe\"}"),
                    "unrelated-integer/probe", null }, null);
            var method = runType.GetMethod("SelectRuneCandidate", BindingFlags.Instance | BindingFlags.NonPublic);
            var result = method.Invoke(run, new object[]
            {
                null, unrelated, candidates, Enum.Parse(shapeType, "Rune"), "1"
            });
            Assert.That(result, Is.Null, "A rune answer must not consume an unrelated integer offer.");
        }
    }
}
