using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.1k. Proves the instrument the architecture fixtures now depend on, before they
    /// depend on it. A conformance rule built on a scanner that quietly fails to resolve a call would
    /// pass vacuously - the exact defect F613-13 reports about the text scans it replaces.
    /// </summary>
    public sealed class ILConformanceScannerTests
    {
        [Test]
        public void Scanner_ResolvesEveryTokenInEveryProductionAssembly()
        {
            var unresolved = ILConformanceScanner.UnresolvedTokens(ProductionTypes());

            Assert.That(unresolved, Is.Empty,
                "Every rule below is only as strong as the scan behind it. A token the scanner cannot " +
                "resolve is a hole a violation could sit in: " + string.Join("; ", unresolved.Take(20)));
        }

        [Test]
        public void Scanner_SeesAProductionCallItMustSee()
        {
            // Non-vacuity: the scan finds a call that is definitely there. Without this, an empty
            // result would be indistinguishable from a scanner that read nothing at all.
            Assert.That(
                ILConformanceScanner.References(
                    new[] { typeof(DestinationHighlightMaterialOwner) },
                    ILConformanceScanner.Method(
                        typeof(UnityEngine.Shader), nameof(UnityEngine.Shader.Find), typeof(string))),
                Is.True,
                "DestinationHighlightMaterialOwner.Material resolves the Standard shader by name.");
        }

        [Test]
        public void ForbiddenCall_IsFoundHoweverTheCallSiteSpellsIt()
        {
            var forbidden = new MemberInfo[] { ILConformanceScanner.Method(typeof(ScanTarget), "Forbidden") };

            var offenders = ILConformanceScanner.FindForbiddenReferences(
                new[]
                {
                    typeof(DirectCaller),
                    typeof(FullyQualifiedCaller),
                    typeof(LambdaCaller),
                    typeof(GenericCaller),
                    typeof(CompliantCaller)
                },
                forbidden,
                "calls the forbidden member");

            // The fully-qualified case is the one the token scan provably missed at milestone 6.1h,
            // and the lambda case is the one a per-method scan misses if it ignores nested types.
            Assert.That(offenders.Count(value => value.Contains(nameof(DirectCaller))), Is.EqualTo(1));
            Assert.That(offenders.Count(value => value.Contains(nameof(FullyQualifiedCaller))), Is.EqualTo(1));
            Assert.That(offenders.Any(value => value.Contains(nameof(LambdaCaller))), Is.True,
                "A call inside a lambda body lives on a compiler-generated nested type.");
            Assert.That(offenders, Has.None.Contains(nameof(CompliantCaller)));
            Assert.That(offenders, Has.None.Contains(nameof(GenericCaller)),
                "GenericCaller calls a different member; naming one member must not catch its neighbours.");

            var forbiddenGeneric = new MemberInfo[]
            {
                ILConformanceScanner.Method(typeof(ScanTarget), "ForbiddenGeneric")
            };
            var genericOffenders = ILConformanceScanner.FindForbiddenReferences(
                new[] { typeof(GenericCaller), typeof(CompliantCaller) },
                forbiddenGeneric,
                "calls the forbidden member");

            Assert.That(genericOffenders.Any(value => value.Contains(nameof(GenericCaller))), Is.True,
                "A constructed generic call must match the rule that names its definition.");
            Assert.That(genericOffenders, Has.None.Contains(nameof(CompliantCaller)));
        }

        [Test]
        public void ForbiddenConstruction_IsFoundThroughNewObj()
        {
            var offenders = ILConformanceScanner.FindForbiddenReferences(
                new[] { typeof(ConstructingCaller), typeof(CompliantCaller) },
                new MemberInfo[] { ILConformanceScanner.Constructor(typeof(ScanProduct), typeof(string)) },
                "constructs the forbidden type");

            Assert.That(offenders.Any(value => value.Contains(nameof(ConstructingCaller))), Is.True);
            Assert.That(offenders, Has.None.Contains(nameof(CompliantCaller)));
        }

        [Test]
        public void StringLiteralsFeedingACall_AreReadFromTheCallSite()
        {
            var scan = ILConformanceScanner.Scan(typeof(NamedCaller));
            var target = ILConformanceScanner.Method(typeof(ScanTarget), "Named");

            var literals = scan.SelectMany(method => method.StringsPassedTo(target)).ToArray();

            Assert.That(literals, Is.EquivalentTo(new[] { "first-literal", "second-literal" }));
        }

        [Test]
        public void InstructionOrder_ExpressesGuardBeforeUse()
        {
            var target = ILConformanceScanner.Method(typeof(ScanTarget), "Named");
            var construction = ILConformanceScanner.Constructor(typeof(ScanProduct), typeof(string));

            var guarded = ILConformanceScanner.Scan(typeof(GuardedCaller))
                .Single(method => method.Method.Name == nameof(GuardedCaller.Build));
            var unguarded = ILConformanceScanner.Scan(typeof(UnguardedCaller))
                .Single(method => method.Method.Name == nameof(UnguardedCaller.Build));

            Assert.That(guarded.FirstIndexOf(target), Is.GreaterThanOrEqualTo(0));
            Assert.That(guarded.FirstIndexOf(OpCodes.Throw), Is.GreaterThanOrEqualTo(0));
            Assert.That(guarded.FirstIndexOf(OpCodes.Throw), Is.LessThan(guarded.FirstIndexOf(construction)),
                "The guard must throw before the value it guards is used to build anything.");

            Assert.That(unguarded.FirstIndexOf(OpCodes.Throw), Is.EqualTo(-1));
            Assert.That(unguarded.FirstIndexOf(construction), Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void RuleNamingAMemberThatDoesNotExist_FailsInsteadOfDisarmingItself()
        {
            // A rule whose member was renamed away must break loudly. Silently matching nothing is
            // how the token lists this milestone replaces could go quiet without anyone noticing.
            Assert.Throws<ArgumentException>(() =>
                ILConformanceScanner.Method(typeof(ScanTarget), "MemberThatWasRenamedAway"));
            Assert.Throws<ArgumentException>(() =>
                ILConformanceScanner.Property(typeof(ScanTarget), "PropertyThatWasRenamedAway"));
        }

        private static IEnumerable<Type> ProductionTypes() =>
            ProductionAssemblies().SelectMany(ILConformanceScanner.TypesIn);

        private static IEnumerable<Assembly> ProductionAssemblies() =>
            new[]
            {
                typeof(RoundResolutionAuthority).Assembly,
                typeof(CardInstance).Assembly,
                typeof(MatchSession).Assembly,
                typeof(DestinationHighlightMaterialOwner).Assembly
            }.Distinct();

        private static class ScanTarget
        {
            public static int Forbidden(int value) => value + 1;
            public static int Allowed(int value) => value - 1;
            public static string ForbiddenGeneric<T>(T value) => typeof(T).Name + value;
            public static string Named(string name) => name;
        }

        private sealed class ScanProduct
        {
            public ScanProduct(string name) => Name = name;
            public string Name { get; }
        }

        private sealed class DirectCaller
        {
            public int Run() => ScanTarget.Forbidden(1);
        }

        private sealed class FullyQualifiedCaller
        {
            public int Run() => ILConformanceScannerTests.ScanTarget.Forbidden(2);
        }

        private sealed class LambdaCaller
        {
            public int Run() => new[] { 1, 2 }.Select(value => ScanTarget.Forbidden(value)).Sum();
        }

        private sealed class GenericCaller
        {
            public string Run() => ScanTarget.ForbiddenGeneric("value");
        }

        private sealed class ConstructingCaller
        {
            public string Run() => new ScanProduct("constructed").Name;
        }

        private sealed class CompliantCaller
        {
            public int Run() => ScanTarget.Allowed(3);
        }

        private sealed class NamedCaller
        {
            public string Run() => ScanTarget.Named("first-literal") + ScanTarget.Named("second-literal");
        }

        private sealed class GuardedCaller
        {
            public ScanProduct Build(bool missing)
            {
                var name = ScanTarget.Named("guarded");
                if (missing)
                {
                    throw new InvalidOperationException("The guarded value is required.");
                }

                return new ScanProduct(name);
            }
        }

        private sealed class UnguardedCaller
        {
            public ScanProduct Build() => new ScanProduct(ScanTarget.Named("unguarded"));
        }
    }
}
