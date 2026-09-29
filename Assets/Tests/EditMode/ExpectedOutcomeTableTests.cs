using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using JsonObject = GuardsDigital.Tests.EditMode.ExpectedOutcomeJson.JsonObject;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.13. Every case of every expected-outcome table under
    /// <c>Assets/Tests/ExpectedOutcomes</c>, executed against the engine as one NUnit case named
    /// <c>&lt;table&gt;/&lt;case id&gt;</c>.
    ///
    /// <para><b>Why the tables are data, not code.</b> The tables are written by a reader who has seen
    /// only the printed card and the rulebook page, never the implementation, so the class of defect
    /// every journey so far shared - the same agent implementing a card and writing its proof, reading
    /// the text the same way twice - cannot hide in them. The executor
    /// (<see cref="ExpectedOutcomeExecutor"/>) is the only code between the table and the engine, and
    /// it refuses rather than guesses: a phrase outside the format's vocabulary, an unknown card name,
    /// a placement the official map cannot hold or a question the answers do not cover fails its case
    /// with the phrase and the case id quoted. The format and the recipe are in
    /// <c>Docs/AI/Testing/EXPECTED_OUTCOME_TABLES.md</c>.</para>
    /// </summary>
    public sealed class ExpectedOutcomeTableTests
    {
        internal const string TableFolder = "Assets/Tests/ExpectedOutcomes";

        public static IEnumerable<TestCaseData> Cases()
        {
            string folder;
            try
            {
                folder = Path.Combine(RepositoryRoot(), TableFolder);
            }
            catch (Exception exception)
            {
                return new[] { new TestCaseData(null, null, exception.Message).SetName("tables/unreachable") };
            }

            var cases = new List<TestCaseData>();
            foreach (var file in Directory.GetFiles(folder, "*.json").OrderBy(path => path, StringComparer.Ordinal))
            {
                var table = Path.GetFileNameWithoutExtension(file);
                JsonObject document;
                try
                {
                    document = ExpectedOutcomeJson.Parse(File.ReadAllText(file)) as JsonObject;
                    if (document == null) throw new FormatException("the document is not a JSON object");
                }
                catch (Exception exception)
                {
                    cases.Add(new TestCaseData(null, null, $"{table}.json does not parse: {exception.Message}")
                        .SetName($"{table}/unreadable"));
                    continue;
                }

                var list = document.Get("cases") as List<object>;
                if (list == null || list.Count == 0)
                {
                    cases.Add(new TestCaseData(null, null, $"{table}.json names no \"cases\" array")
                        .SetName($"{table}/no-cases"));
                    continue;
                }

                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < list.Count; index++)
                {
                    var entry = list[index] as JsonObject;
                    var id = entry?.Get("id") as string;
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        cases.Add(new TestCaseData(null, null,
                                $"{table}.json case #{index + 1} has no \"id\"")
                            .SetName($"{table}/case-{index + 1}-without-id"));
                        continue;
                    }

                    var name = $"{table}/{id}";
                    if (!seen.Add(id))
                    {
                        cases.Add(new TestCaseData(null, null, $"{name}: the id is used twice in {table}.json")
                            .SetName($"{name}-duplicate-{index + 1}"));
                        continue;
                    }

                    cases.Add(new TestCaseData(document, entry, name).SetName(name));
                }
            }

            if (cases.Count == 0)
            {
                cases.Add(new TestCaseData(null, null, $"no table in {TableFolder}").SetName("tables/none"));
            }

            return cases;
        }

        [TestCaseSource(nameof(Cases))]
        public void TheEngineAgreesWithTheTable(object document, object entry, string name)
        {
            if (document == null || entry == null)
            {
                Assert.Fail(name);
            }

            var report = ExpectedOutcomeExecutor.Run((JsonObject)document, (JsonObject)entry, name);
            TestContext.WriteLine(report.Trace);
            if (report.Failures.Count > 0)
            {
                Assert.Fail($"{name}:\n  - " + string.Join("\n  - ", report.Failures));
            }
        }

        /// <summary>The executor refuses what it does not know rather than guessing.</summary>
        [Test]
        public void AnUnknownCardName_FailsNamingThePhraseAndTheCase()
        {
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(
                "{\"id\":\"probe\",\"card\":\"No Such Card\",\"action\":\"Attack\",\"expect\":[{\"offered\":true}]}");
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Wasp\",\"cases\":[]}");
            var report = ExpectedOutcomeExecutor.Run(document, entry, "probe-table/probe");
            Assert.That(report.Failures, Has.Some.Contains("No Such Card").And.Contains("probe-table/probe"));
        }

        [Test]
        public void AnUnsupportedPhrase_FailsNamingThePhraseAndTheCase()
        {
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(
                "{\"id\":\"probe\",\"card\":\"Shock\",\"action\":\"Attack\"," +
                "\"setup\":{\"pieces\":[{\"ref\":\"enemy-minion-1\",\"where\":\"in front of actor\"}]}," +
                "\"expect\":[{\"offered\":true}]}");
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Wasp\",\"cases\":[]}");
            var report = ExpectedOutcomeExecutor.Run(document, entry, "probe-table/probe");
            Assert.That(report.Failures, Has.Some.Contains("in front of actor").And.Contains("probe-table/probe"));
        }

        [Test]
        public void AnAnswerTheCardNeverAsksFor_FailsTheCase()
        {
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(
                "{\"id\":\"probe\",\"card\":\"Shock\",\"action\":\"Attack\"," +
                "\"setup\":{\"pieces\":[{\"ref\":\"enemy-minion-1\",\"where\":\"adjacent to actor\"}]}," +
                "\"answers\":[\"enemy-minion-1\",\"decline\"],\"expect\":[{\"ref\":\"enemy-minion-1\",\"is\":\"defeated\"}]}");
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Wasp\",\"cases\":[]}");
            var report = ExpectedOutcomeExecutor.Run(document, entry, "probe-table/probe");
            Assert.That(report.Failures, Has.Exactly(1).Contains("answers never asked for: \"decline\""));
        }

        internal static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null &&
                !Directory.Exists(Path.Combine(directory.FullName, "Assets", "Tests", "ExpectedOutcomes")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new DirectoryNotFoundException(
                    $"{TableFolder} is not reachable from {Directory.GetCurrentDirectory()}");
            }

            return directory.FullName;
        }
    }
}
