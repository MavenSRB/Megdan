using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8f acceptance check 2: no rules-consuming entry point declares an implicit
    /// no-modifications default.
    ///
    /// The hazard this rule closes is recorded twice in the roadmap: milestone 6.8's review noted the
    /// optional <c>RuleModificationSet modifications = null</c> parameter loses rules silently when a
    /// caller forgets it, and that is exactly how the Move-Through permission reached a configured
    /// card with no consumer. 6.8c then added further optional parameters rather than closing the
    /// hazard. A required parameter makes the compiler the enforcement; the named no-modification
    /// value is <see cref="RuleModificationSet.Empty"/>.
    ///
    /// This rule is reflection- rather than IL-based - a parameter default lives in metadata, not in
    /// a method body - and it is the repo's first use of <see cref="ParameterInfo.HasDefaultValue"/>.
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class ModificationSeamConformanceTests
    {
        [Test]
        public void NoRulesConsumingSeam_DeclaresAnOptionalModificationSet()
        {
            Assert.That(
                FindOptionalModificationParameters(ProductionTypes()),
                Is.Empty,
                "A seam whose outcome a modification can change declares an implicit default, so a " +
                "caller that forgets the live set silently resolves against no modifications. " +
                "Passing none must be an explicit, named decision: RuleModificationSet.Empty.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(
                FindOptionalModificationParameters(new[] { typeof(OptionalSetSample) }),
                Is.Not.Empty,
                "A method with an optional modification-set parameter must be reported, or the rule " +
                "is inert.");

            Assert.That(
                FindOptionalModificationParameters(new[] { typeof(ExplicitSetSample) }),
                Is.Empty,
                "A method that requires its modification set must not be reported, or the rule bans " +
                "the shape it exists to enforce.");
        }

        /// <summary>
        /// The negative rule is satisfied by an engine with no modification parameters at all, so the
        /// scan must be shown to see the real seams it governs.
        /// </summary>
        [Test]
        public void TheScan_SeesTheRealModificationSeams()
        {
            var seen = CountModificationParameters(ProductionTypes());

            Assert.That(seen, Is.GreaterThanOrEqualTo(30),
                "The scan found fewer modification-set parameters than the production surface is " +
                "known to declare, so the subject set or the parameter-type match has silently " +
                "narrowed and the rule could pass vacuously.");
        }

        private static IReadOnlyList<string> FindOptionalModificationParameters(
            IEnumerable<Type> types) =>
            ScanParameters(types)
                .Where(entry => entry.Parameter.IsOptional || entry.Parameter.HasDefaultValue)
                .Select(entry => entry.Description)
                .ToArray();

        private static int CountModificationParameters(IEnumerable<Type> types) =>
            ScanParameters(types).Count();

        private static IEnumerable<(ParameterInfo Parameter, string Description)> ScanParameters(
            IEnumerable<Type> types)
        {
            const BindingFlags declared = BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            foreach (var type in types)
            {
                var members = type.GetMethods(declared)
                    .Cast<MethodBase>()
                    .Concat(type.GetConstructors(declared));
                foreach (var member in members)
                {
                    foreach (var parameter in member.GetParameters())
                    {
                        if (parameter.ParameterType == typeof(RuleModificationSet))
                        {
                            yield return (parameter,
                                $"{type.FullName}.{member.Name}({parameter.Name})");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Every type of the Core and Application production assemblies. The namespace filter keeps
        /// the harness's single merged assembly from sweeping this fixture's own samples into the
        /// real subject set; under Unity the two typeof anchors resolve to the two production
        /// assemblies and the filter is a no-op.
        /// </summary>
        private static IReadOnlyList<Type> ProductionTypes() =>
            ILConformanceScanner.TypesIn(typeof(RuleModificationSet).Assembly)
                .Concat(ILConformanceScanner.TypesIn(typeof(MatchSession).Assembly))
                .Distinct()
                .Where(type => (type.Namespace ?? string.Empty).StartsWith(
                        "GuardsDigital", StringComparison.Ordinal) &&
                    !(type.Namespace ?? string.Empty).StartsWith(
                        "GuardsDigital.Tests", StringComparison.Ordinal))
                .ToArray();

        /// <summary>Deliberate violation: the set is optional, so forgetting it loses rules silently.</summary>
        private sealed class OptionalSetSample
        {
            public bool Evaluate(RuleModificationSet modifications = null) => modifications != null;
        }

        /// <summary>Compliant twin: the set is required; supplying none must be written as Empty.</summary>
        private sealed class ExplicitSetSample
        {
            public bool Evaluate(RuleModificationSet modifications) => modifications != null;
        }
    }
}
