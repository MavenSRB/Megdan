using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class Phase4ArchitectureConformanceTests
    {
        private static readonly string[] UnityReferenceTokens =
        {
            "using UnityEngine",
            "using UnityEditor",
            "UnityEngine.",
            "UnityEditor."
        };

        private static readonly string[] GlobalDiscoveryTokens =
        {
            "FindAnyObjectByType<",
            "FindFirstObjectByType<",
            "FindObjectsByType<",
            "FindObjectOfType<",
            "FindObjectsOfType<",
            "GameObject.Find(",
            "Resources.FindObjectsOfTypeAll<",
            "ServiceLocator",
            "GlobalService",
            "Singleton<"
        };

        private static readonly string[] ObsoleteDiscoveryTokens =
        {
            "FindObjectOfType<",
            "FindObjectsOfType<",
            "Object.FindObjectOfType(",
            "Object.FindObjectsOfType("
        };

        private static readonly string[] ViewAuthorityTokens =
        {
            "IMatchSession",
            "AuthoritativeSnapshotSerializer",
            "SnapshotLoadException",
            "RoundResolutionAuthority",
            "CardActionResolutionAuthority",
            "DirectorCommandHistory",
            "RuleEventLogProjection"
        };

        private static readonly string[] RuntimePrintedTextParsingTokens =
        {
            "PrintedTextParser",
            "ParsePrintedText",
            "PrintedText.Parse",
            "Regex.Match(",
            "Regex.IsMatch("
        };

        // The outer card-action dispatcher must route to a backend, never perform the action itself.
        // Milestone 6.1h removed the three I*CardActionStrategyBackend interfaces along with the
        // registry that needed them, so the tokens naming those interfaces were re-pointed rather
        // than left guarding names that no longer exist. What replaces them is the set of algorithm
        // entry points each backend actually calls, read from the backends rather than invented: a
        // dispatcher that names one of these has started owning the algorithm. `ActionOfferQuery` is
        // deliberately absent - offer enumeration is a dispatcher-level concern and the authority
        // legitimately calls it three times. `.TryApplyLocationTransition(` was dropped rather than
        // kept: 6.1f moved that member into the `GuardsDigital.Core.CardState` assembly, so naming
        // it from here is now a compile error, and a compile error outranks a text scan.
        //
        // Known limit of this instrument, measured during 6.1h rather than assumed: it matches
        // substrings, so a fully-qualified `new GuardsDigital.Core.Rules.Movement.MovementSession(`
        // evades every token here. It was observed doing exactly that. The rule catches the way the
        // code is actually written, not a determined author; replacing structural assertions of this
        // kind is milestone 6.1k's subject.
        private static readonly string[] CardActionDispatcherAlgorithmTokens =
        {
            "new MovementSession(",
            ".TryCommitStep(",
            "MovementReachabilityQuery.FindReachableDestinations(",
            "AttackTargetQuery.FindLegalEnemyTargets(",
            "DefenseValueQuery.Calculate(",
            "PrintedValueQuery.CalculatePrintedValue(",
            "FastTravelActionQuery.EvaluateDestination(",
            "CardLifecycleAuthority.TryDiscardDefenseFromHand("
        };

        private static readonly Regex NameOrTextBranch = new Regex(
            @"\b(if|switch)\s*\([^)]*\b(DisplayName|PrintedText)\b",
            RegexOptions.CultureInvariant | RegexOptions.Singleline);

        [Test]
        public void ProjectSources_PreserveMechanicalPhase4ArchitectureBoundaries()
        {
            var assets = UnityEngine.Application.dataPath;
            var runtime = Path.Combine(assets, "GuardsDigital");
            var pureRoots = new[]
            {
                Path.Combine(runtime, "Core"),
                Path.Combine(runtime, "Application")
            };
            var violations = new List<string>();

            foreach (var file in EnumerateSources(pureRoots))
            {
                var source = File.ReadAllText(file);
                if (!file.EndsWith("ApplicationAssemblyDependencyPolicy.cs", StringComparison.OrdinalIgnoreCase))
                {
                    AddTokenViolations(violations, file, source, UnityReferenceTokens, "pure assembly references Unity");
                }
                if (NameOrTextBranch.IsMatch(source))
                {
                    violations.Add($"{Relative(file)}: rules/application branches on display name or printed text");
                }

                AddTokenViolations(
                    violations,
                    file,
                    source,
                    RuntimePrintedTextParsingTokens,
                    "runtime printed-text parsing outside an approved isolated boundary");
            }

            var presentation = Path.Combine(runtime, "Presentation");
            foreach (var file in EnumerateSources(new[] { presentation }))
            {
                if (file.EndsWith("MatchSceneCompositionRoot.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AddTokenViolations(
                    violations,
                    file,
                    File.ReadAllText(file),
                    GlobalDiscoveryTokens,
                    "global discovery/service location outside composition root");
            }

            var passiveViews = Path.Combine(presentation, "Match", "UnityMatchFeatureViews.cs");
            AddTokenViolations(
                violations,
                passiveViews,
                File.ReadAllText(passiveViews),
                ViewAuthorityTokens,
                "passive view references session/authoritative implementation");

            var cardResolution = Path.Combine(runtime, "Core", "Rules", "CardResolution");
            var cardActionDispatcher = Path.Combine(cardResolution, "CardActionResolutionAuthority.cs");
            AddTokenViolations(
                violations,
                cardActionDispatcher,
                File.ReadAllText(cardActionDispatcher),
                CardActionDispatcherAlgorithmTokens,
                "card-action dispatcher owns a strategy backend or action-specific algorithm");

            foreach (var backendFile in new[]
                     {
                         "MovementCardActionStrategyBackend.cs",
                         "AttackDefenseCardActionStrategyBackend.cs",
                         "FastTravelCardActionStrategyBackend.cs"
                     })
            {
                if (!File.Exists(Path.Combine(cardResolution, backendFile)))
                {
                    violations.Add($"Assets/GuardsDigital/Core/Rules/CardResolution/{backendFile}: production strategy backend is missing");
                }
            }

            foreach (var file in EnumerateSources(new[]
                     {
                         runtime,
                         Path.Combine(assets, "Tests")
                     }))
            {
                if (file.EndsWith("Phase4ArchitectureConformanceTests.cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AddTokenViolations(
                    violations,
                    file,
                    File.ReadAllText(file),
                    ObsoleteDiscoveryTokens,
                    "obsolete Unity object discovery API");
            }

            Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
        }

        [Test]
        public void ConformanceRules_DistinguishAllowedAndForbiddenExamples()
        {
            Assert.That(FindTokens("using System;", UnityReferenceTokens), Is.Empty);
            Assert.That(FindTokens("using UnityEngine;", UnityReferenceTokens), Is.Not.Empty);
            Assert.That(FindTokens("var view = FindAnyObjectByType<Camera>();", GlobalDiscoveryTokens), Is.Not.Empty);
            Assert.That(FindTokens("view.Render(model);", GlobalDiscoveryTokens), Is.Empty);
            Assert.That(FindTokens("private IMatchSession session;", ViewAuthorityTokens), Is.Not.Empty);
            Assert.That(FindTokens("private RoundActionViewModel model;", ViewAuthorityTokens), Is.Empty);
            Assert.That(NameOrTextBranch.IsMatch("if (card.DisplayName == name) return true;"), Is.True);
            Assert.That(NameOrTextBranch.IsMatch("var label = card.DisplayName;"), Is.False);
            Assert.That(NameOrTextBranch.IsMatch("switch (definition.PrintedText) { }"), Is.True);
            Assert.That(FindTokens("ParsePrintedText(source);", RuntimePrintedTextParsingTokens), Is.Not.Empty);
            Assert.That(FindTokens("ParseTypedEffect(source);", RuntimePrintedTextParsingTokens), Is.Empty);
            Assert.That(FindTokens("movementBackend.StartMovement(offer);", CardActionDispatcherAlgorithmTokens), Is.Empty);
            Assert.That(FindTokens("new MovementSession(authority, state);", CardActionDispatcherAlgorithmTokens), Is.Not.Empty);
            Assert.That(FindTokens("var total = DefenseValueQuery.Calculate(offer);", CardActionDispatcherAlgorithmTokens), Is.Not.Empty);
            Assert.That(FindTokens("CardLifecycleAuthority.TryDiscardDefenseFromHand(card);", CardActionDispatcherAlgorithmTokens), Is.Not.Empty);

            // The allowed example is the shape 6.1h introduced: the dispatcher naming its backend
            // and forwarding. That must stay allowed, or the rule would forbid the very thing the
            // milestone replaced the registry with.
            Assert.That(
                FindTokens("attackDefenseBackend.ResolveOutcome(definitions, instances, participants);",
                    CardActionDispatcherAlgorithmTokens),
                Is.Empty);
            Assert.That(FindTokens("FindObjectOfType<Camera>();", ObsoleteDiscoveryTokens), Is.Not.Empty);
            Assert.That(FindTokens("FindAnyObjectByType<Camera>();", ObsoleteDiscoveryTokens), Is.Empty);
        }

        private static IEnumerable<string> EnumerateSources(IEnumerable<string> roots)
        {
            return roots.SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase);
        }

        private static void AddTokenViolations(
            ICollection<string> violations,
            string file,
            string source,
            IEnumerable<string> tokens,
            string description)
        {
            foreach (var token in FindTokens(source, tokens))
            {
                violations.Add($"{Relative(file)}: {description}: {token}");
            }
        }

        private static IEnumerable<string> FindTokens(string source, IEnumerable<string> tokens)
        {
            return tokens.Where(token => source.IndexOf(token, StringComparison.Ordinal) >= 0);
        }

        private static string Relative(string path)
        {
            return path.Replace(UnityEngine.Application.dataPath, "Assets").Replace('\\', '/');
        }
    }
}
