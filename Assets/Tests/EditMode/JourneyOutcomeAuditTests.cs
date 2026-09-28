using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.4. A player-path journey that answers a card's question also reads what the answer
    /// changed - the board, a hand, a pile, tokens, coins, a value - and not only that the question was
    /// asked. Swift's Mark for Death (8.82) and Ursafar's Angry Roar (8.97) reached a playtest because
    /// their journeys saw the prompt and never the result; 8.86 audited the move family by hand.
    ///
    /// <para><b>How it reads.</b> Each [UnityTest] in the player-path fixtures is scanned: after its
    /// first answer (a pick, a space, a confirm or decline, a bullet), some assertion must read a piece
    /// of game state. That is a floor, not a proof that the RIGHT state is read, so every journey this
    /// flags is either extended or listed with the reason its purpose is something else (an undo or
    /// restore that compares whole snapshots is about the re-ask, not the outcome). A newly dealt hero's
    /// journeys meet the same floor the moment they are written.</para>
    /// </summary>
    public sealed class JourneyOutcomeAuditTests
    {
        private static readonly Regex Answer = new Regex(
            @"\b(Pick|PickWhere|PickSpace|Space|May|ClickEffectControl|ClickEffectAnswer|SubmitEffectChoice\w*|Bullet|TakeBullets|AnswerTheFreeDirection|ConfirmIfStillStanding|PickMinions|AnswerTheHandChoice\w*)\s*\(");

        private static readonly Regex Outcome = new Regex(
            @"(Coordinate|Position\(|SpaceOf\w*\(|HandCount|HandOfCount|HeldBy\(|Tokens\(|PlacedTokens|Coins|CoinsOf|Coin\(|Minions\.(Any|Count)|OnBoard|Stands\(|State\.Location|State,|Heroes\.(Any|Single)|Discard\w*Of\(|DiscardsOf|DiscardOf|Obstacles\.|IsActiveEffectSource|Kills|Life|RoundEvents|Trees\(|Rocks\(|MagmaSpaces|Pyro\(|InHand\(|HeroView\(|CaptureSnapshot|PendingAttackTargetChoice|LegalTarget|Initiative|PrintedValue|AttackTotal|DefenseTotal|FinishMovement\()");

        private static readonly Regex Journey = new Regex(
            @"\[UnityTest\]\s*public IEnumerator (\w+)\(\)\s*\{");

        [Test]
        public void EveryJourneyThatAnswersACardsQuestion_ReadsWhatTheAnswerChanged()
        {
            var flagged = JourneysAnsweringWithoutAnOutcome()
                .Except(PurposeIsTheReAsk.Keys)
                .ToArray();

            Assert.That(flagged, Is.Empty,
                "These journeys answer a card-text question and then read no game state. Assert what " +
                "the printed sentence changed - the board, a hand, a pile, tokens, coins or a value - " +
                "including after a decline, or list the journey with the reason its purpose is elsewhere.");
            Assert.That(PurposeIsTheReAsk.Keys.Except(JourneysAnsweringWithoutAnOutcome()), Is.Empty,
                "A listed journey that now reads an outcome is off the list.");
        }

        [Test]
        public void TheScan_FindsThePlayerPathJourneys()
        {
            Assert.That(JourneyCount(), Is.GreaterThan(500),
                "Twenty-six heroes' player-path fixtures hold hundreds of journeys; a scan finding few " +
                "is reading the wrong directory.");
        }

        private static readonly IReadOnlyDictionary<string, string> PurposeIsTheReAsk =
            new Dictionary<string, string>
            {
                ["IgnatiaProgressedUiJourneyPlayModeTests.HerOwnPausesUndoAndTheReplayReproducesTheirOffers"] =
                    "Undo and replay: the claim is that each paused offer is reproduced; the outcomes are the face journeys'."
            };

        private static IEnumerable<string> JourneysAnsweringWithoutAnOutcome()
        {
            foreach (var (fixture, name, body) in Journeys())
            {
                var first = Answer.Match(body);
                if (first.Success && !Outcome.IsMatch(body.Substring(first.Index)))
                {
                    yield return fixture + "." + name;
                }
            }
        }

        private static int JourneyCount() => Journeys().Count();

        private static IEnumerable<(string Fixture, string Name, string Body)> Journeys()
        {
            var directory = Path.Combine(RepositoryRoot(), "Assets", "Tests", "PlayMode");
            var files = Directory.GetFiles(directory, "*UiJourney*PlayModeTests.cs")
                .Concat(Directory.GetFiles(directory, "*JourneyTests.cs"))
                .Distinct()
                .OrderBy(path => path, StringComparer.Ordinal);
            foreach (var path in files)
            {
                var source = File.ReadAllText(path);
                var fixture = Path.GetFileNameWithoutExtension(path);
                foreach (Match match in Journey.Matches(source))
                {
                    var start = match.Index + match.Length;
                    var depth = 1;
                    var index = start;
                    while (depth > 0 && index < source.Length)
                    {
                        if (source[index] == '{') depth++;
                        else if (source[index] == '}') depth--;
                        index++;
                    }

                    yield return (fixture, match.Groups[1].Value, source.Substring(start, index - start));
                }
            }
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null &&
                !Directory.Exists(Path.Combine(directory.FullName, "Assets", "Tests", "PlayMode")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null,
                "The player-path fixtures must be reachable from the working directory this suite runs in.");
            return directory.FullName;
        }
    }
}
