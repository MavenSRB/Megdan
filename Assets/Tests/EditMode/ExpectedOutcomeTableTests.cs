using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
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

        public static IEnumerable<TestCaseData> CompletedBatchHeroes()
        {
            yield return new TestCaseData("wasp", WaspCardContent.DealtDefinitions).SetName("coverage/wasp");
            yield return new TestCaseData("arien", ArienCardContent.DealtDefinitions).SetName("coverage/arien");
            yield return new TestCaseData("brogan", BroganCardContent.DealtDefinitions).SetName("coverage/brogan");
            yield return new TestCaseData("sabina", SabinaCardContent.DealtDefinitions).SetName("coverage/sabina");
            yield return new TestCaseData("tigerclaw", TigerclawCardContent.DealtDefinitions).SetName("coverage/tigerclaw");
            yield return new TestCaseData("xargatha", XargathaCardContent.DealtDefinitions).SetName("coverage/xargatha");
            yield return new TestCaseData("dodger", DodgerCardContent.DealtDefinitions).SetName("coverage/dodger");
            yield return new TestCaseData("misa", MisaCardContent.DealtDefinitions).SetName("coverage/misa");
            yield return new TestCaseData("silverarrow", SilverarrowCardContent.DealtDefinitions).SetName("coverage/silverarrow");
            yield return new TestCaseData("ursafar", UrsafarCardContent.DealtDefinitions).SetName("coverage/ursafar");
            yield return new TestCaseData("whisper", WhisperCardContent.DealtDefinitions).SetName("coverage/whisper");
            yield return new TestCaseData("tali", TaliCardContent.DealtDefinitions).SetName("coverage/tali");
            yield return new TestCaseData("swift", SwiftCardContent.DealtDefinitions).SetName("coverage/swift");
            yield return new TestCaseData("cutter", CutterCardContent.DealtDefinitions).SetName("coverage/cutter");
            yield return new TestCaseData("bain", BainCardContent.DealtDefinitions).SetName("coverage/bain");
            yield return new TestCaseData("garrus", GarrusCardContent.DealtDefinitions).SetName("coverage/garrus");
            yield return new TestCaseData("widget", WidgetCardContent.DealtDefinitions).SetName("coverage/widget");
            yield return new TestCaseData("rowenna", RowennaCardContent.DealtDefinitions).SetName("coverage/rowenna");
            yield return new TestCaseData("min", MinCardContent.DealtDefinitions).SetName("coverage/min");
            yield return new TestCaseData("brynn", BrynnCardContent.DealtDefinitions).SetName("coverage/brynn");
        }

        [TestCaseSource(nameof(CompletedBatchHeroes))]
        public void EveryBatchFace_HasAnExecutableOutcomeTable(string hero, IEnumerable<CardDefinition> definitions)
        {
            var folder = Path.Combine(RepositoryRoot(), TableFolder);
            var path = Path.Combine(folder, hero + ".json");
            Assert.That(File.Exists(path), Is.True, hero + " has no independent table.");
            var document = (JsonObject)ExpectedOutcomeJson.Parse(File.ReadAllText(path));
            var dispositions = Dispositions(folder, hero);
            var cases = ((List<object>)document.Get("cases")).OfType<JsonObject>().Where(entry =>
            {
                if (entry.Get("handicap") is bool handicap && handicap) return false;
                if (!dispositions.TryGetValue((string)entry.Get("id"), out var disposition)) return true;
                return (string)disposition.Get("kind") != "open-question" &&
                    !(disposition.Get("execute") is bool execute && !execute);
            }).ToArray();
            foreach (var definition in definitions)
            {
                if (definition.Progression.IsUltimate)
                {
                    Assert.That(cases.Any(entry =>
                    {
                        var actor = (entry.Get("setup") as JsonObject)?.Get("actor") as JsonObject;
                        return actor != null && (actor.Get("ultimate") is bool unlocked && unlocked ||
                            Convert.ToString(actor.Get("level"), System.Globalization.CultureInfo.InvariantCulture) == "8");
                    }), Is.True, hero + ": no executed Ultimate scenario.");
                    continue;
                }

                Assert.That(cases.Any(entry => string.Equals((string)entry.Get("card"), definition.DisplayName,
                    StringComparison.OrdinalIgnoreCase) && entry.Get("expect") is List<object> facts && facts.Count > 0),
                    Is.True, hero + ": no executable outcome table for " + definition.DisplayName);
            }
        }

        public static IEnumerable<TestCaseData> Cases()
        {
            string folder;
            try
            {
                folder = Path.Combine(RepositoryRoot(), TableFolder);
            }
            catch (Exception exception)
            {
                return new[] { new TestCaseData(null, null, exception.Message, null).SetName("tables/unreachable") };
            }

            var cases = new List<TestCaseData>();
            foreach (var file in Directory.GetFiles(folder, "*.json")
                .Where(path => !path.EndsWith(".dispositions.json", StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal))
            {
                var table = Path.GetFileNameWithoutExtension(file);
                Dictionary<string, JsonObject> dispositions;
                try
                {
                    dispositions = Dispositions(folder, table);
                }
                catch (Exception exception)
                {
                    cases.Add(new TestCaseData(null, null, $"{table}.dispositions.json: {exception.Message}", null)
                        .SetName($"{table}/dispositions-unreadable"));
                    continue;
                }

                JsonObject document;
                try
                {
                    document = ExpectedOutcomeJson.Parse(File.ReadAllText(file)) as JsonObject;
                    if (document == null) throw new FormatException("the document is not a JSON object");
                }
                catch (Exception exception)
                {
                    cases.Add(new TestCaseData(null, null, $"{table}.json does not parse: {exception.Message}", null)
                        .SetName($"{table}/unreadable"));
                    continue;
                }

                var list = document.Get("cases") as List<object>;
                if (list == null || list.Count == 0)
                {
                    cases.Add(new TestCaseData(null, null, $"{table}.json names no \"cases\" array", null)
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
                                $"{table}.json case #{index + 1} has no \"id\"", null)
                            .SetName($"{table}/case-{index + 1}-without-id"));
                        continue;
                    }

                    var name = $"{table}/{id}";
                    if (!seen.Add(id))
                    {
                        cases.Add(new TestCaseData(null, null, $"{name}: the id is used twice in {table}.json", null)
                            .SetName($"{name}-duplicate-{index + 1}"));
                        continue;
                    }

                    dispositions.TryGetValue(id, out var disposition);
                    cases.Add(new TestCaseData(document, entry, name, disposition).SetName(name));
                }
            }

            if (cases.Count == 0)
            {
                cases.Add(new TestCaseData(null, null, $"no table in {TableFolder}", null).SetName("tables/none"));
            }

            return cases;
        }

        [TestCaseSource(nameof(Cases))]
        public void TheEngineAgreesWithTheTable(object document, object entry, string name, object disposition)
        {
            if (document == null || entry == null)
            {
                Assert.Fail(name);
            }

            var report = ExpectedOutcomeExecutor.Run((JsonObject)document, (JsonObject)entry, name, (JsonObject)disposition);
            TestContext.WriteLine(report.Trace);
            if (disposition != null)
            {
                TestContext.WriteLine($"disposition: {((JsonObject)disposition).Get("kind")} " +
                    $"({((JsonObject)disposition).Get("ref")}) {((JsonObject)disposition).Get("note")}");
            }

            if (report.Skipped != null)
            {
                Assert.That(report.Failures, Is.Empty);
                Assert.Pass($"{name} not executed - {report.Skipped}");
            }

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
                "\"answers\":[\"enemy-minion-1\",\"enemy-hero-1\"],\"expect\":[{\"ref\":\"enemy-minion-1\",\"is\":\"defeated\"}]}");
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Wasp\",\"cases\":[]}");
            var report = ExpectedOutcomeExecutor.Run(document, entry, "probe-table/probe");
            Assert.That(report.Failures, Has.Exactly(1).Contains("answers never asked for: \"enemy-hero-1\""));
        }

        /// <summary>
        /// Milestone 9.13, engine defect found by the executor. Electroblast defeats its target and
        /// its "After the attack" text defeats a second, empty-handed hero inside the same Attack.
        /// Both p.13 Defeats were given the id "&lt;resolution&gt;:hero-defeat", so the second reward was
        /// refused as OutcomeAlreadyAwarded and the command faulted.
        /// </summary>
        [Test]
        public void TwoHeroDefeatsInOneAttack_EachPaysItsOwnReward()
        {
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(
                "{\"id\":\"two-hero-defeats\",\"card\":\"Electroblast\",\"action\":\"Attack\"," +
                "\"setup\":{\"actor\":{\"level\":5},\"pieces\":[" +
                "{\"ref\":\"enemy-hero-1\",\"where\":\"adjacent to actor\"}," +
                "{\"ref\":\"enemy-hero-2\",\"where\":\"distance 2 from actor in a straight line\"}]," +
                "\"enemy-hero-1\":{\"defends-with\":\"Noble Blade\"},\"enemy-hero-2\":{\"hand\":[]}}," +
                "\"answers\":[\"enemy-hero-1\",\"enemy-hero-2\"]," +
                "\"expect\":[{\"ref\":\"enemy-hero-1\",\"is\":\"defeated\"},{\"ref\":\"enemy-hero-2\",\"is\":\"defeated\"}]}");
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Wasp\",\"cases\":[]}");
            var report = ExpectedOutcomeExecutor.Run(document, entry, "regression/two-hero-defeats");
            TestContext.WriteLine(report.Trace);
            Assert.That(report.Failures, Is.Empty, string.Join(" | ", report.Failures));
        }

        /// <summary>
        /// Milestone 9.13, engine defect found by the executor. Dodger's Darkest Ritual with his
        /// Ultimate gains "+1 Attack item (use any spare card from the box)"; the box item names no dealt
        /// card, and the Defense preview's contribution label looked it up with Single, so the next
        /// Attack on a hero faulted ("Sequence contains no matching element").
        /// </summary>
        [Test]
        public void ABoxItem_CountsInTheNextAttack_WithoutFaultingTheDefensePreview()
        {
            var entry = (JsonObject)ExpectedOutcomeJson.Parse(
                "{\"id\":\"box-item-then-attack\",\"card\":\"Littlefinger of Death\",\"action\":\"Attack\"," +
                "\"setup\":{\"actor\":{\"level\":8},\"standing\":[\"actor played Darkest Ritual last turn\"]," +
                "\"pieces\":[{\"ref\":\"enemy-hero-1\",\"where\":\"adjacent to actor\"}]," +
                "\"enemy-hero-1\":{\"defends-with\":\"none\"}}," +
                "\"answers\":[\"option: Target a unit adjacent to you.\",\"enemy-hero-1\"]," +
                "\"expect\":[{\"ref\":\"actor\",\"attack\":5},{\"ref\":\"enemy-hero-1\",\"is\":\"defeated\"}]}");
            var document = (JsonObject)ExpectedOutcomeJson.Parse("{\"hero\":\"Dodger\",\"cases\":[]}");
            var report = ExpectedOutcomeExecutor.Run(document, entry, "regression/box-item-then-attack");
            TestContext.WriteLine(report.Trace);
            Assert.That(report.Failures, Is.Empty, string.Join(" | ", report.Failures));
        }

        /// <summary>
        /// A table's dispositions: one per case id, each of a known kind, naming a case the table has.
        /// </summary>
        internal static Dictionary<string, JsonObject> Dispositions(string folder, string table)
        {
            var result = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
            var path = Path.Combine(folder, table + ".dispositions.json");
            if (!File.Exists(path)) return result;
            if (!(ExpectedOutcomeJson.Parse(File.ReadAllText(path)) is List<object> list))
            {
                throw new FormatException("the document is not a JSON array");
            }

            foreach (var item in list)
            {
                var entry = item as JsonObject ?? throw new FormatException("an entry is not an object");
                var id = entry.Get("id") as string ?? throw new FormatException("an entry has no id");
                var kind = entry.Get("kind") as string;
                if (!ExpectedOutcomeExecutor.DispositionKinds.Contains(kind ?? string.Empty))
                {
                    throw new FormatException($"{id}: unknown kind \"{kind}\"");
                }

                if (kind == "ruling" && !entry.Has("expect-override"))
                {
                    throw new FormatException($"{id}: a ruling names its expect-override");
                }

                if (result.ContainsKey(id)) throw new FormatException($"{id} is dispositioned twice");
                result[id] = entry;
            }

            return result;
        }

        /// <summary>Every disposition names a case its table has; the open questions are listed for the user.</summary>
        [Test]
        public void EveryDisposition_NamesACase_AndTheOpenQuestionsAreCounted()
        {
            var folder = Path.Combine(RepositoryRoot(), TableFolder);
            var open = new List<string>();
            var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var file in Directory.GetFiles(folder, "*.json")
                .Where(path => !path.EndsWith(".dispositions.json", StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal))
            {
                var table = Path.GetFileNameWithoutExtension(file);
                var ids = new HashSet<string>(((ExpectedOutcomeJson.Parse(File.ReadAllText(file)) as JsonObject)
                        ?.Get("cases") as List<object> ?? new List<object>())
                    .OfType<JsonObject>().Select(entry => entry.Get("id") as string), StringComparer.Ordinal);
                foreach (var pair in Dispositions(folder, table))
                {
                    Assert.That(ids, Does.Contain(pair.Key), $"{table}.dispositions.json names no case of {table}.json");
                    var kind = (string)pair.Value.Get("kind");
                    counts.TryGetValue($"{table} {kind}", out var count);
                    counts[$"{table} {kind}"] = count + 1;
                    if (kind == "open-question")
                    {
                        open.Add($"{table}/{pair.Key} ({pair.Value.Get("ref")}): {pair.Value.Get("note")}");
                    }
                }
            }

            foreach (var pair in counts) TestContext.WriteLine($"{pair.Key}: {pair.Value}");
            TestContext.WriteLine($"OPEN QUESTIONS FOR THE USER: {open.Count}");
            foreach (var line in open) TestContext.WriteLine("  " + line);
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
