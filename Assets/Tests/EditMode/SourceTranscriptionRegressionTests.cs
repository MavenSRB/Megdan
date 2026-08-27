using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.25e5b's exact source-provenance checks for the eleven literal fields found by the
    /// independent 114-front review, plus milestone 8.25e5d's raw-ledger conformance guard. The
    /// literal expectations are transcribed from sealed source-first readings rather than derived
    /// from production definitions.
    /// </summary>
    public sealed class SourceTranscriptionRegressionTests
    {
        private const string XargathaLedgerRelativePath =
            "Docs/AI/Rules/HERO_XARGATHA.md";

        private static readonly TestCaseData[] LiteralSourceFields =
        {
            Literal(
                SabinaCardContent.PointBlankShot.Value,
                "Target a unit in range. After the attack: If the target is adjacent to you, push it " +
                "1 space. (A unit pushed into an obstacle will stop. This is a valid push.)"),
            Literal(
                TigerclawCardContent.BlinkStrike.Value,
                "Before the attack: Move 2 spaces in a straight line through an enemy unit; target " +
                "that unit. (If you cannot make this move, you cannot attack)."),
            Literal(
                TigerclawCardContent.Backstab.Value,
                "Target a unit adjacent to you; If a friendly unit is adjacent to the target, +2 " +
                "[Attack] Attack. (A “friendly unit” is another hero or a minion on your team)."),
            Literal(
                TigerclawCardContent.BlendIntoShadows.Value,
                "If you are adjacent to terrain, place yourself into a space in radius; if you do, " +
                "Next turn: You are immune to enemy attack actions."),
            Literal(
                XargathaCardContent.Cleave.Value,
                "Target a unit adjacent to you. After the attack: May repeat once on a different " +
                "enemy hero. ( You may repeat even if the original target was a minion. )"),
            Literal(
                XargathaCardContent.DangerousSlash.Value,
                "Target a unit adjacent to you. +1 [Attack] Attack for each other enemy unit " +
                "adjacent to you. ( Do not count the target when calculating the attack bonus. )"),
            Literal(
                XargathaCardContent.StoneGaze.Value,
                "Next turn: Enemy heroes in radius count as both heroes and terrain, and cannot " +
                "perform movement actions. ( If you move, the radius “moves” with you. )"),
            Literal(
                XargathaCardContent.PetrifyingStare.Value,
                "Next turn: Enemy heroes in radius count as both heroes and terrain, and cannot " +
                "perform movement actions."),
            Literal(
                XargathaCardContent.TurnIntoStatues.Value,
                "Next turn: Enemy heroes in radius count as both heroes and terrain, and cannot " +
                "perform movement actions."),
            Literal(
                XargathaCardContent.Constrict.Value,
                "End of round: Defeat an enemy melee minion adjacent to you. ( Before the end of " +
                "round minion battle. )"),
            Literal(
                XargathaCardContent.FinalEmbrace.Value,
                "End of round: Defeat an enemy melee or ranged minion adjacent to you.")
        };

        [TestCaseSource(nameof(LiteralSourceFields))]
        public void LiteralSourceField_MatchesTheSealedIndependentReading(
            string stableDefinitionId,
            string expectedPrintedText)
        {
            var definition = AllAffectedDefinitions().Single(value =>
                value.Id.Value == stableDefinitionId);

            Assert.That(
                definition.SourceContent.PrintedText,
                Is.EqualTo(expectedPrintedText),
                stableDefinitionId + " must retain the exact source string, including punctuation, " +
                "reminder spacing, typographic quotation marks, and no presentation markup.");
        }

        [Test]
        public void NimbleStrike_RemainsTheGoldBasicSetupExcludedHandicap()
        {
            var definition = TigerclawCardContent.Definition(TigerclawCardContent.NimbleStrike);
            var primary = definition.ActionDefinitions.Single(value =>
                value.Origin == ActionDefinitionOrigin.PrintedPrimary);

            Assert.That(definition.Progression.Color, Is.EqualTo(CardColor.Gold));
            Assert.That(definition.Progression.IsBasic, Is.True);
            Assert.That(
                TigerclawCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { TigerclawCardContent.NimbleStrike }),
                "p.6 returns Tigerclaw's p.20 Handicap to the box in the six-player setup.");
            Assert.That(
                primary.TextComposition.IsAwaited,
                Is.True,
                "The Handicap remains behaviorally deferred until an odd-player configuration.");
        }

        [Test]
        public void ConstrictLedgerReminderMatchesProductionSourceContent()
        {
            const string rowPrefix = "| Constrict | ";
            const string rowSuffix = " |";
            const string timingEmphasis = "**End of round:**";
            const string reminderEmphasisOpening = "*(";
            const string reminderEmphasisClosing = ")*";

            var ledgerPath = FindProjectFile(XargathaLedgerRelativePath);
            var rows = File.ReadAllLines(ledgerPath)
                .Where(line => line.StartsWith(rowPrefix, StringComparison.Ordinal))
                .ToArray();

            Assert.That(
                rows,
                Has.Length.EqualTo(1),
                "The Xargatha ledger must contain exactly one raw Constrict row.");
            Assert.That(
                rows[0],
                Does.EndWith(rowSuffix),
                "The raw Constrict row must retain the expected two-column Markdown shape.");

            var markdownLiteral = rows[0].Substring(
                rowPrefix.Length,
                rows[0].Length - rowPrefix.Length - rowSuffix.Length);

            Assert.That(CountOccurrences(markdownLiteral, timingEmphasis), Is.EqualTo(1));
            Assert.That(CountOccurrences(markdownLiteral, reminderEmphasisOpening), Is.EqualTo(1));
            Assert.That(CountOccurrences(markdownLiteral, reminderEmphasisClosing), Is.EqualTo(1));

            var sourceLiteral = markdownLiteral
                .Replace(timingEmphasis, "End of round:")
                .Replace(reminderEmphasisOpening, "(")
                .Replace(reminderEmphasisClosing, ")");
            var productionLiteral = XargathaCardContent
                .Definition(XargathaCardContent.Constrict)
                .SourceContent.PrintedText;

            Assert.That(productionLiteral, Is.Not.Null.And.Not.Empty);
            Assert.That(
                sourceLiteral,
                Is.EqualTo(productionLiteral),
                "Constrict's raw hero-ledger literal must match production source content after " +
                "removing only its declared Markdown emphasis markers.");
        }

        [TestCase(
            "sabina-point-blank-shot",
            "sabina-8-22-rev-1",
            SabinaCardContent.ContentRevision)]
        [TestCase(
            "tigerclaw-blink-strike",
            "tigerclaw-8-23-rev-1",
            TigerclawCardContent.ContentRevision)]
        [TestCase(
            "xargatha-cleave",
            "xargatha-8-25e5f-rev-1",
            XargathaCardContent.ContentRevision)]
        public void OfficialSixPlayerContent_UsesTheAdvancedRevisionAndRejectsTheSupersededOne(
            string stableDefinitionId,
            string supersededRevision,
            string currentRevision)
        {
            var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(
                TeamSide.Red);
            var definitions = configuration.CardDefinitions.Where(value =>
                value.Id.Value == stableDefinitionId).ToArray();

            Assert.That(definitions, Has.Length.EqualTo(1));
            Assert.That(definitions[0].ContentRevision, Is.EqualTo(currentRevision));
            Assert.That(
                configuration.CardDefinitions.Any(value => value.Reference.Equals(
                    new CardDefinitionReference(value.Id, supersededRevision))),
                Is.False,
                stableDefinitionId + "'s superseded reference must not resolve in current content.");
            Assert.That(
                configuration.CardInstances.Any(value =>
                    value.DefinitionReference.DefinitionId.Value == stableDefinitionId &&
                    value.DefinitionReference.ContentRevision == supersededRevision),
                Is.False,
                "No configured instance may retain the superseded content reference.");
        }

        private static TestCaseData Literal(string stableDefinitionId, string printedText) =>
            new TestCaseData(stableDefinitionId, printedText).SetName(
                "LiteralSourceField_" + stableDefinitionId);

        private static int CountOccurrences(string value, string fragment)
        {
            var count = 0;
            var index = 0;
            while ((index = value.IndexOf(fragment, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += fragment.Length;
            }

            return count;
        }

        private static string FindProjectFile(string relativePath)
        {
            var roots = new[]
            {
                Directory.GetCurrentDirectory(),
                TestContext.CurrentContext.TestDirectory
            };

            foreach (var root in roots)
            {
                var directory = new DirectoryInfo(root);
                while (directory != null)
                {
                    var candidate = Path.Combine(directory.FullName, relativePath);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }

                    directory = directory.Parent;
                }
            }

            Assert.Fail("Could not locate project file " + relativePath + ".");
            return null;
        }

        private static IEnumerable<CardDefinition> AllAffectedDefinitions() =>
            SabinaCardContent.AllDefinitions
                .Concat(TigerclawCardContent.AllDefinitions)
                .Concat(XargathaCardContent.AllDefinitions);
    }
}
