using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The mechanical Phase 4 architecture boundaries.
    ///
    /// Milestone 6.1k, finding F613-13, replaced every token list and source-text scan in this fixture
    /// with rules over compiled code. The old instrument read `.cs` files and asked whether they
    /// contained a substring; it therefore broke on renames that changed no behavior, and it let
    /// through any call spelled differently from the token. Milestone 6.1h measured the second half:
    /// a fully-qualified `new GuardsDigital.Core.Rules.Movement.MovementSession(` in the dispatcher
    /// evaded every token here, and the limitation was recorded rather than fixed because 6.1k owned
    /// it.
    ///
    /// Each rule below names its forbidden members through the type system - `typeof(...)` plus
    /// <see cref="ILConformanceScanner.Method"/>, which throws when the member does not exist - and
    /// compares them against the members the compiler actually emitted. What the boundary protects is
    /// unchanged in every case; only the instrument changed. The mapping is stated per rule.
    /// </summary>
    public sealed class Phase4ArchitectureConformanceTests
    {
        [Test]
        public void ProjectSources_PreserveMechanicalPhase4ArchitectureBoundaries()
        {
            var violations = new List<string>();

            violations.AddRange(FindUnityReferenceViolations(PureAssemblies));
            violations.AddRange(FindNameBranchViolations(PureTypes, PureTypes));
            violations.AddRange(FindRuntimeTextParsingViolations(PureTypes));
            violations.AddRange(FindGlobalDiscoveryViolations(PresentationTypesOutsideCompositionRoot));
            violations.AddRange(FindServiceLocatorTypes(PresentationTypes));
            violations.AddRange(FindObsoleteDiscoveryViolations(PresentationTypes.Concat(TestTypes)));
            violations.AddRange(FindViewAuthorityViolations(PassiveViewTypes));
            violations.AddRange(FindDispatcherAlgorithmViolations(DispatcherTypes));

            // A rule built on an unreadable body would pass by looking at nothing. Every assembly this
            // fixture scans has to be fully readable, or the result above means nothing.
            violations.AddRange(ILConformanceScanner
                .UnresolvedTokens(PureTypes.Concat(PresentationTypes).Concat(TestTypes))
                .Select(value => $"unresolved IL token, so a rule could be evaded through it: {value}"));

            Assert.That(violations, Is.Empty, string.Join(Environment.NewLine, violations));
        }

        [Test]
        public void ConformanceRules_DistinguishAllowedAndForbiddenExamples()
        {
            // Every rule is run against a type that deliberately violates it and against a compliant
            // twin. A rule that has stopped looking - because a member it named was renamed away, or
            // because the scan silently skipped a construct - fails here rather than passing quietly
            // over production forever.
            Assert.That(FindUnityReferenceViolations(new[] { typeof(BoardInteractionUnityView).Assembly }),
                Is.Not.Empty, "The presentation assembly does reference Unity; the rule must say so.");
            Assert.That(FindUnityReferenceViolations(PureAssemblies), Is.Empty);

            AssertRuleDistinguishes(
                types => FindNameBranchViolations(types, new[] { typeof(NameCarryingSample) }),
                typeof(NameBranchingSample),
                typeof(NameForwardingSample));

            AssertRuleDistinguishes(
                FindRuntimeTextParsingViolations,
                typeof(RuntimeTextParsingSample),
                typeof(TypedEffectSample));

            AssertRuleDistinguishes(
                FindGlobalDiscoveryViolations,
                typeof(GlobalDiscoverySample),
                typeof(InjectedCollaboratorSample));

            AssertRuleDistinguishes(
                FindObsoleteDiscoveryViolations,
                typeof(ObsoleteDiscoverySample),
                typeof(GlobalDiscoverySample));

            AssertRuleDistinguishes(
                FindViewAuthorityViolations,
                typeof(SessionHoldingViewSample),
                typeof(PassiveViewSample));
            Assert.That(FindViewAuthorityViolations(new[] { typeof(SessionFieldViewSample) }), Is.Not.Empty,
                "A session held as a field is a violation even when no method ever calls through it.");

            AssertRuleDistinguishes(
                FindDispatcherAlgorithmViolations,
                typeof(AlgorithmOwningDispatcherSample),
                typeof(ForwardingDispatcherSample));

            // The regression that motivated the milestone: the same forbidden construction, written
            // fully qualified. The token scan this replaced passed on exactly this input.
            Assert.That(
                FindDispatcherAlgorithmViolations(new[] { typeof(FullyQualifiedDispatcherSample) }),
                Is.Not.Empty,
                "A fully-qualified call is the spelling that defeated the token scan.");

            Assert.That(FindServiceLocatorTypes(new[] { typeof(ServiceLocator) }), Is.Not.Empty);
            Assert.That(FindServiceLocatorTypes(new[] { typeof(ForwardingDispatcherSample) }), Is.Empty);

            // The three production backends are resolved as types rather than as `.cs` files on disk.
            // They are `internal` to Core, so this rule names them by full type name - the one place a
            // name survives, and it survives as an assertion that the type exists, not as a scan of
            // source text. Deleting or renaming a backend fails here, exactly as the file check did.
            var coreAssembly = typeof(CardActionResolutionAuthority).Assembly;
            var backends = new[]
            {
                "GuardsDigital.Core.Rules.CardResolution.MovementCardActionStrategyBackend",
                "GuardsDigital.Core.Rules.CardResolution.AttackDefenseCardActionStrategyBackend",
                "GuardsDigital.Core.Rules.CardResolution.FastTravelCardActionStrategyBackend"
            }.Select(name => coreAssembly.GetType(name, false)).ToArray();

            Assert.That(backends, Has.None.Null, "A production strategy backend is missing.");
            Assert.That(backends.Distinct().Count(), Is.EqualTo(3));
            Assert.That(backends, Has.All.Matches<Type>(type => type.IsClass && !type.IsAbstract));
        }

        private static void AssertRuleDistinguishes(
            Func<IEnumerable<Type>, IReadOnlyList<string>> rule,
            Type violator,
            Type compliant)
        {
            Assert.That(rule(new[] { violator }), Is.Not.Empty,
                $"{violator.Name} violates this rule; the rule reported nothing, so it is not looking.");
            Assert.That(rule(new[] { compliant }), Is.Empty,
                $"{compliant.Name} complies with this rule; the rule reported a violation anyway.");
        }

        // ---------------------------------------------------------------------------------------
        // Scanned sets
        // ---------------------------------------------------------------------------------------

        private static IEnumerable<Assembly> PureAssemblies => new[]
        {
            typeof(RoundResolutionAuthority).Assembly,
            typeof(CardInstance).Assembly,
            typeof(MatchSession).Assembly
        }.Distinct();

        private static IEnumerable<Type> PureTypes => PureAssemblies.SelectMany(ILConformanceScanner.TypesIn);

        private static IEnumerable<Type> PresentationTypes =>
            ILConformanceScanner.TypesIn(typeof(MatchSceneCompositionRoot).Assembly);

        /// <summary>
        /// Discovery is the composition root's job alone. Its nested types go with it: a lambda in the
        /// root compiles into a nested type, and excluding the root but not its closures would forbid
        /// the very thing the root exists to do.
        /// </summary>
        private static IEnumerable<Type> PresentationTypesOutsideCompositionRoot
        {
            get
            {
                var root = new HashSet<Type>(ILConformanceScanner.WithNestedTypes(typeof(MatchSceneCompositionRoot)));
                return PresentationTypes.Where(type => !root.Contains(type));
            }
        }

        /// <summary>
        /// The passive views, identified by the view interfaces they implement rather than by the file
        /// they happen to share. Moving one to its own file no longer removes it from this rule, and
        /// adding a sixth passive view adds it automatically.
        /// </summary>
        private static IEnumerable<Type> PassiveViewTypes
        {
            get
            {
                var viewInterfaces = new[]
                {
                    typeof(IBoardInteractionView),
                    typeof(IDirectorHandsView),
                    typeof(IRoundActionView),
                    typeof(IUndoRestartView),
                    typeof(IDiagnosticLogView)
                };
                var types = PresentationTypes
                    .Where(type => type.IsClass && viewInterfaces.Any(contract => contract.IsAssignableFrom(type)))
                    .ToArray();

                Assert.That(types, Has.Length.EqualTo(5),
                    "The passive Unity views are the five implementations of the view interfaces. " +
                    "If that count changed legitimately, update this expectation deliberately: " +
                    string.Join(", ", types.Select(type => type.Name)));
                return types;
            }
        }

        private static IEnumerable<Type> DispatcherTypes =>
            ILConformanceScanner.WithNestedTypes(typeof(CardActionResolutionAuthority));

        private static IEnumerable<Type> TestTypes
        {
            get
            {
                var playMode = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(assembly => string.Equals(
                        assembly.GetName().Name, "GuardsDigital.PlayModeTests", StringComparison.Ordinal));

                Assert.That(playMode, Is.Not.Null,
                    "The Play Mode test assembly was not loaded, so scanning it would pass vacuously.");

                // This fixture's own deliberate violators are excluded by type identity, not by file
                // name. The old rule had to skip this file because the file named the forbidden
                // strings; the exclusion now covers exactly the samples that exist to be violations.
                var samples = new HashSet<Type>(
                    ILConformanceScanner.WithNestedTypes(typeof(Phase4ArchitectureConformanceTests)));
                return ILConformanceScanner.TypesIn(typeof(Phase4ArchitectureConformanceTests).Assembly)
                    .Where(type => !samples.Contains(type))
                    .Concat(ILConformanceScanner.TypesIn(playMode));
            }
        }

        // ---------------------------------------------------------------------------------------
        // Rules
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Replaces the `UnityReferenceTokens` source scan. Protected: the pure assemblies never touch
        /// Unity. Reading the assembly's own reference table is strictly stronger than scanning for
        /// `using UnityEngine` - a fully-qualified `UnityEngine.Debug.Log` needs the reference too -
        /// and the former exemption for `ApplicationAssemblyDependencyPolicy.cs`, which failed the
        /// scan only because it names the forbidden strings, disappears with the string matching.
        /// </summary>
        private static IReadOnlyList<string> FindUnityReferenceViolations(IEnumerable<Assembly> assemblies) =>
            assemblies.SelectMany(assembly => ApplicationAssemblyDependencyPolicy
                    .FindForbiddenReferences(assembly.GetReferencedAssemblies().Select(name => name.Name))
                    .Select(forbidden => $"{assembly.GetName().Name}: pure assembly references Unity: {forbidden}"))
                .ToArray();

        /// <summary>
        /// Replaces the `NameOrTextBranch` regex. Protected: rules and application code never branch on
        /// a card's display name or printed text. The regex matched only when the identifier appeared
        /// inside the parentheses of an `if`/`switch`, so `var name = card.DisplayName; if (name == x)`
        /// passed it. This rule instead reports any method that reads such a property and then performs
        /// a string comparison, which covers the local-variable form the regex missed.
        ///
        /// The name-like properties are discovered by name, because "display name" has no other marker
        /// in the type system; the rule then asserts on the resolved property, and the discovery is
        /// checked for emptiness so a rename cannot quietly disarm it.
        ///
        /// Precision, stated rather than implied: a violation is a name read whose *next* member call
        /// is a string comparison. That is the shape of `if (card.DisplayName == x)`, of
        /// `var n = card.DisplayName; if (n == x)`, and of a `switch` on the value, and it does not fire
        /// on `new State(..., definition.DisplayName, ...)` in a method that compares unrelated strings
        /// elsewhere - which `QuickGameSetupAuthority.Create` legitimately does. It would not see
        /// `card.DisplayName.Trim() == x`; no rule here claims to be a dataflow analysis.
        /// </summary>
        private static IReadOnlyList<string> FindNameBranchViolations(
            IEnumerable<Type> scannedTypes,
            IEnumerable<Type> declaringTypes)
        {
            var getters = NameLikeGetters(declaringTypes).ToArray();
            var comparisons = StringComparisonMembers().ToArray();
            var violations = new List<string>();

            foreach (var method in ILConformanceScanner.Scan(scannedTypes))
            {
                var members = method.Instructions.Where(instruction => instruction.Member != null).ToArray();
                for (var index = 0; index < members.Length - 1; index++)
                {
                    if (!getters.Any(getter => SameMember(members[index].Member, getter)))
                    {
                        continue;
                    }

                    var next = members[index + 1].Member;
                    var comparesTheName = comparisons.Any(comparison => SameMember(next, comparison)) ||
                        next.Name.IndexOf("ComputeStringHash", StringComparison.Ordinal) >= 0;
                    if (comparesTheName)
                    {
                        violations.Add(
                            $"{method.Description}: rules/application branches on display name or printed text");
                    }
                }
            }

            return violations.Distinct().ToArray();
        }

        private static bool SameMember(MemberInfo left, MemberInfo right)
        {
            if (left == null || right == null)
            {
                return false;
            }

            try
            {
                return left.Module.Equals(right.Module) && left.MetadataToken == right.MetadataToken;
            }
            catch (InvalidOperationException)
            {
                return ReferenceEquals(left, right);
            }
        }

        private static IReadOnlyList<MemberInfo> NameLikeGetters(IEnumerable<Type> declaringTypes)
        {
            var getters = declaringTypes
                .SelectMany(type => type.GetProperties(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(property =>
                    string.Equals(property.Name, "DisplayName", StringComparison.Ordinal) ||
                    string.Equals(property.Name, "PrintedText", StringComparison.Ordinal))
                .Select(property => property.GetGetMethod(true))
                .Where(getter => getter != null)
                .Cast<MemberInfo>()
                .ToArray();

            Assert.That(getters, Is.Not.Empty,
                "No display-name property was discovered, so this rule would pass vacuously. If the " +
                "property was renamed, rename it here too rather than leaving the rule blind.");
            return getters;
        }

        private static IEnumerable<MemberInfo> StringComparisonMembers() =>
            new[] { "Equals", "op_Equality", "op_Inequality", "Compare", "CompareTo", "StartsWith", "EndsWith", "Contains" }
                .SelectMany(name => ILConformanceScanner.Methods(typeof(string), name))
                .Cast<MemberInfo>();

        /// <summary>
        /// Replaces the `RuntimePrintedTextParsingTokens` source scan. Protected: no runtime
        /// printed-text parsing in the pure assemblies. The tokens named two hypothetical parser
        /// members that have never existed plus two `Regex` methods; this rule forbids the whole
        /// <see cref="System.Text.RegularExpressions.Regex"/> type, so a third `Regex` entry point
        /// cannot appear without being reported.
        /// </summary>
        private static IReadOnlyList<string> FindRuntimeTextParsingViolations(IEnumerable<Type> types) =>
            FindForbiddenTypeReferences(
                types,
                new[] { typeof(System.Text.RegularExpressions.Regex) },
                "runtime printed-text parsing outside an approved isolated boundary");

        /// <summary>
        /// Replaces the `GlobalDiscoveryTokens` source scan. Protected: global discovery and service
        /// location live only in the composition root.
        /// </summary>
        private static IReadOnlyList<string> FindGlobalDiscoveryViolations(IEnumerable<Type> types) =>
            ILConformanceScanner.FindForbiddenReferences(
                types,
                DiscoveryMembers(),
                "global discovery/service location outside composition root");

        private static IEnumerable<MemberInfo> DiscoveryMembers()
        {
            var members = new[]
                {
                    nameof(UnityEngine.Object.FindAnyObjectByType),
                    nameof(UnityEngine.Object.FindFirstObjectByType),
                    nameof(UnityEngine.Object.FindObjectsByType)
                }
                .SelectMany(name => ILConformanceScanner.Methods(typeof(UnityEngine.Object), name))
                .Concat(ILConformanceScanner.Methods(typeof(GameObject), nameof(GameObject.Find)))
                .Concat(ILConformanceScanner.Methods(typeof(Resources), nameof(Resources.FindObjectsOfTypeAll)))
                .Concat(ObsoleteDiscoveryMembers())
                .Cast<MemberInfo>()
                .ToArray();

            Assert.That(members, Is.Not.Empty, "No discovery API resolved, so this rule would be blind.");
            return members;
        }

        /// <summary>
        /// Replaces the `ObsoleteDiscoveryTokens` source scan, which ran over the runtime and test
        /// trees and had to skip this fixture's own file because it named the forbidden strings. A rule
        /// that names members instead of strings has nothing to exclude.
        /// </summary>
        private static IReadOnlyList<string> FindObsoleteDiscoveryViolations(IEnumerable<Type> types) =>
            ILConformanceScanner.FindForbiddenReferences(
                types,
                ObsoleteDiscoveryMembers(),
                "obsolete Unity object discovery API");

        private static IEnumerable<MethodInfo> ObsoleteDiscoveryMembers() =>
            new[] { "FindObjectOfType", "FindObjectsOfType" }
                .SelectMany(name => ILConformanceScanner.Methods(typeof(UnityEngine.Object), name));

        /// <summary>
        /// Replaces the type-name scan for `ServiceLocator`, `GlobalService`, and `Singleton&lt;&gt;`.
        /// These name a pattern that has never existed in this project, so there is no member to
        /// resolve; the rule stays a name check and is stated as one.
        /// </summary>
        private static IReadOnlyList<string> FindServiceLocatorTypes(IEnumerable<Type> types) =>
            types.Where(type =>
                    type.Name.IndexOf("ServiceLocator", StringComparison.Ordinal) >= 0 ||
                    type.Name.IndexOf("GlobalService", StringComparison.Ordinal) >= 0 ||
                    (type.IsGenericTypeDefinition && type.Name.StartsWith("Singleton", StringComparison.Ordinal)))
                .Select(type => $"{type.FullName}: service-location pattern outside composition root")
                .ToArray();

        /// <summary>
        /// Replaces the `ViewAuthorityTokens` scan of one source file. Protected: a passive view holds
        /// and reaches no session or authoritative implementation. This rule covers both the signature
        /// - fields, properties, parameters, returns - and the compiled body, so a view that merely
        /// calls through a local it never stores is caught too.
        /// </summary>
        private static IReadOnlyList<string> FindViewAuthorityViolations(IEnumerable<Type> types) =>
            FindForbiddenTypeReferences(
                types,
                new[]
                {
                    typeof(IMatchSession),
                    typeof(AuthoritativeSnapshotSerializer),
                    typeof(SnapshotLoadException),
                    typeof(RoundResolutionAuthority),
                    typeof(CardActionResolutionAuthority),
                    typeof(DirectorCommandHistory),
                    typeof(RuleEventLogProjection)
                },
                "passive view references session/authoritative implementation");

        /// <summary>
        /// Replaces the `CardActionDispatcherAlgorithmTokens` scan. Protected: the outer dispatcher
        /// routes to a backend and never performs the action itself. The forbidden set is the same one
        /// milestone 6.1h chose - the algorithm entry points each backend actually calls - now resolved
        /// as members. `ActionOfferQuery` stays deliberately absent: offer enumeration is a
        /// dispatcher-level concern the authority legitimately performs.
        ///
        /// `CardInstance.TryApplyLocationTransition` is still absent for 6.1f's reason, which is
        /// stronger than any rule here: it lives in the `GuardsDigital.Core.CardState` assembly, so
        /// naming it from the dispatcher is a compile error.
        /// </summary>
        private static IReadOnlyList<string> FindDispatcherAlgorithmViolations(IEnumerable<Type> types) =>
            ILConformanceScanner.FindForbiddenReferences(
                types,
                DispatcherForbiddenAlgorithms(),
                "card-action dispatcher owns a strategy backend or action-specific algorithm");

        private static IEnumerable<MemberInfo> DispatcherForbiddenAlgorithms() =>
            ILConformanceScanner.Constructors(typeof(MovementSession)).Cast<MemberInfo>()
                .Concat(ILConformanceScanner.Methods(
                    typeof(MovementSession), nameof(MovementSession.TryCommitStep)))
                .Concat(ILConformanceScanner.Methods(
                    typeof(MovementReachabilityQuery),
                    nameof(MovementReachabilityQuery.FindReachableDestinations)))
                .Concat(ILConformanceScanner.Methods(
                    typeof(AttackTargetQuery), nameof(AttackTargetQuery.FindLegalEnemyTargets)))
                .Concat(ILConformanceScanner.Methods(
                    typeof(DefenseValueQuery), nameof(DefenseValueQuery.Calculate)))
                .Concat(ILConformanceScanner.Methods(
                    typeof(PrintedValueQuery), nameof(PrintedValueQuery.CalculatePrintedValue)))
                .Concat(ILConformanceScanner.Methods(
                    typeof(FastTravelActionQuery), nameof(FastTravelActionQuery.EvaluateDestination)))
                .Concat(ILConformanceScanner.Methods(
                    typeof(CardLifecycleAuthority), nameof(CardLifecycleAuthority.TryDiscardDefenseFromHand)));

        /// <summary>
        /// Type-level rule: neither the compiled body nor any declared signature may mention one of the
        /// forbidden types.
        /// </summary>
        private static IReadOnlyList<string> FindForbiddenTypeReferences(
            IEnumerable<Type> types,
            IReadOnlyCollection<Type> forbiddenTypes,
            string description)
        {
            var scanned = types.ToArray();
            var violations = new List<string>();

            foreach (var method in ILConformanceScanner.Scan(scanned))
            {
                foreach (var forbidden in forbiddenTypes)
                {
                    if (method.Instructions.Any(instruction => MentionsType(instruction.Member, forbidden)))
                    {
                        violations.Add($"{method.Description}: {description}: {forbidden.Name}");
                    }
                }
            }

            foreach (var type in scanned.SelectMany(ILConformanceScanner.WithNestedTypes))
            {
                var signature = SignatureTypes(type).ToArray();
                foreach (var forbidden in forbiddenTypes.Where(candidate => signature.Contains(candidate)))
                {
                    violations.Add($"{type.FullName}: {description}: {forbidden.Name}");
                }
            }

            return violations.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray();
        }

        private static bool MentionsType(MemberInfo member, Type forbidden)
        {
            if (member == null)
            {
                return false;
            }

            if (member is Type type)
            {
                return Flatten(type).Contains(forbidden);
            }

            return member.DeclaringType != null && Flatten(member.DeclaringType).Contains(forbidden);
        }

        private static IEnumerable<Type> SignatureTypes(Type type)
        {
            const BindingFlags declared = BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            foreach (var field in type.GetFields(declared))
            {
                foreach (var candidate in Flatten(field.FieldType))
                {
                    yield return candidate;
                }
            }

            foreach (var property in type.GetProperties(declared))
            {
                foreach (var candidate in Flatten(property.PropertyType))
                {
                    yield return candidate;
                }
            }

            foreach (var method in type.GetMethods(declared))
            {
                foreach (var candidate in Flatten(method.ReturnType)
                    .Concat(method.GetParameters().SelectMany(parameter => Flatten(parameter.ParameterType))))
                {
                    yield return candidate;
                }
            }

            foreach (var constructor in type.GetConstructors(declared))
            {
                foreach (var candidate in constructor.GetParameters()
                    .SelectMany(parameter => Flatten(parameter.ParameterType)))
                {
                    yield return candidate;
                }
            }
        }

        /// <summary>A type plus everything it is built from, so `IReadOnlyList&lt;IMatchSession&gt;`
        /// and `IMatchSession[]` are not hiding places.</summary>
        private static IEnumerable<Type> Flatten(Type type)
        {
            if (type == null)
            {
                yield break;
            }

            yield return type;

            if (type.HasElementType)
            {
                foreach (var candidate in Flatten(type.GetElementType()))
                {
                    yield return candidate;
                }
            }

            if (!type.IsGenericType)
            {
                yield break;
            }

            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var candidate in Flatten(argument))
                {
                    yield return candidate;
                }
            }
        }

        // ---------------------------------------------------------------------------------------
        // Deliberate violators and their compliant twins, for the rule self-check
        // ---------------------------------------------------------------------------------------

        private sealed class NameCarryingSample
        {
            public NameCarryingSample(string displayName) => DisplayName = displayName;
            public string DisplayName { get; }
        }

        private sealed class NameBranchingSample
        {
            // The local-variable form the old regex could not see.
            public bool Matches(NameCarryingSample card, string wanted)
            {
                var name = card.DisplayName;
                return name == wanted;
            }
        }

        private sealed class NameForwardingSample
        {
            public string Label(NameCarryingSample card) => card.DisplayName;
        }

        private sealed class RuntimeTextParsingSample
        {
            public bool Looks(string text) =>
                System.Text.RegularExpressions.Regex.IsMatch(text, "^move");
        }

        private sealed class TypedEffectSample
        {
            public bool Looks(string text) => text.Length > 0;
        }

        private sealed class GlobalDiscoverySample
        {
            public Camera Find() => UnityEngine.Object.FindAnyObjectByType<Camera>();
        }

        private sealed class ObsoleteDiscoverySample
        {
#pragma warning disable 618
            // Deliberately obsolete. The warning is suppressed here alone so the Unity log stays clean;
            // if Unity ever removes these overloads, this sample stops compiling, which is a louder and
            // more permanent signal than the rule it feeds.
            public Camera Find() => UnityEngine.Object.FindObjectOfType<Camera>();
#pragma warning restore 618
        }

        private sealed class InjectedCollaboratorSample
        {
            public InjectedCollaboratorSample(Camera camera) => Camera = camera;
            public Camera Camera { get; }
        }

        private sealed class ServiceLocator
        {
            public object Resolve() => null;
        }

        private sealed class SessionHoldingViewSample
        {
            public void Render(IMatchSession session) => session.Undo();
        }

        private sealed class SessionFieldViewSample
        {
#pragma warning disable 649
            private IMatchSession session;
#pragma warning restore 649
            public bool IsBound => session != null;
        }

        private sealed class PassiveViewSample
        {
            public string Text { get; private set; }
            public void Render(string text) => Text = text;
        }

        private sealed class AlgorithmOwningDispatcherSample
        {
            public object Resolve() => DefenseValueQuery.Calculate(default, null, default, null, null);
        }

        private sealed class FullyQualifiedDispatcherSample
        {
            public object Resolve() =>
                GuardsDigital.Core.Rules.CardResolution.DefenseValueQuery.Calculate(
                    default, null, default, null, null);
        }

        /// <summary>The shape milestone 6.1h introduced: the dispatcher names its backend and
        /// forwards. That must stay allowed, or the rule would forbid what replaced the registry. The
        /// stand-in stands in because the production backends are internal to Core.</summary>
        private sealed class BackendStandInSample
        {
            public object ResolveOutcome() => this;
        }

        private sealed class ForwardingDispatcherSample
        {
            public object Resolve(BackendStandInSample backend) => backend.ResolveOutcome();
        }
    }
}
