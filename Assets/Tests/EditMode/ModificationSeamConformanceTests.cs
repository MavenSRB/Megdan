using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
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
    ///
    /// Milestone 6.8i widened it. 6.8f closed the hazard for one parameter type and flagged four
    /// survivors of the same class, optional parameters of other rule-affecting types whose omission
    /// also changes a rules answer in silence: the Tie Breaker state that orders p.17's simultaneous
    /// modifications, and the <see cref="RuleObject"/> actor whose omission disables p.18's "You are
    /// never immune to your own actions" - the exact clause milestone 6.4's Push immunity consumer
    /// depends on. The widened rule governs the modification query surface, which is where a
    /// rule-affecting value is a rules answer rather than incidental data.
    /// </summary>
    public sealed class ModificationSeamConformanceTests
    {
        /// <summary>
        /// The parameter types whose omission silently changes a rules answer rather than merely
        /// leaving a value unset: the live set itself, the ordering state p.17 resolves ties with, and
        /// the object a query is asked about. Each has a named absent value a call site writes when it
        /// genuinely means none - <see cref="RuleModificationSet.Empty"/>,
        /// <see cref="TieBreakerState.Unrevealed"/>, <see cref="RuleObject.None"/> - so requiring them
        /// costs a caller nothing but the statement of what it meant.
        /// </summary>
        private static readonly Type[] RuleAffectingTypes =
        {
            typeof(RuleModificationSet),
            typeof(TieBreakerState),
            typeof(RuleObject)
        };

        [Test]
        public void NoRulesConsumingSeam_DeclaresAnOptionalModificationSet()
        {
            Assert.That(
                FindOptionalParameters(ProductionTypes(), typeof(RuleModificationSet)),
                Is.Empty,
                "A seam whose outcome a modification can change declares an implicit default, so a " +
                "caller that forgets the live set silently resolves against no modifications. " +
                "Passing none must be an explicit, named decision: RuleModificationSet.Empty.");
        }

        [Test]
        public void NoModificationQuerySeam_DeclaresAnOptionalRuleAffectingParameter()
        {
            Assert.That(
                FindOptionalParameters(ModificationSurfaceTypes(), RuleAffectingTypes),
                Is.Empty,
                "A modification-query seam declares an optional parameter of a rule-affecting type, " +
                "so a caller that omits it gets a different rules answer and no diagnostic. An " +
                "absent ordering state, actor, or object must be written at the call site as " +
                "Unrevealed, None, or Empty rather than inherited from a parameter default.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(
                FindOptionalParameters(new[] { typeof(OptionalSetSample) }, typeof(RuleModificationSet)),
                Is.Not.Empty,
                "A method with an optional modification-set parameter must be reported, or the rule " +
                "is inert.");

            Assert.That(
                FindOptionalParameters(new[] { typeof(ExplicitSetSample) }, typeof(RuleModificationSet)),
                Is.Empty,
                "A method that requires its modification set must not be reported, or the rule bans " +
                "the shape it exists to enforce.");

            Assert.That(
                FindOptionalParameters(new[] { typeof(OptionalRuleAffectingSample) }, RuleAffectingTypes),
                Is.Not.Empty,
                "A method with an optional Tie Breaker or rules-object parameter must be reported, " +
                "or the widened rule is inert.");

            Assert.That(
                FindOptionalParameters(new[] { typeof(ExplicitRuleAffectingSample) }, RuleAffectingTypes),
                Is.Empty,
                "A method that requires them must not be reported, or the widened rule bans the " +
                "shape it exists to enforce.");
        }

        /// <summary>
        /// The negative rules are satisfied by an engine with no such parameters at all, so both scans
        /// must be shown to see the real seams they govern.
        /// </summary>
        [Test]
        public void TheScan_SeesTheRealModificationSeams()
        {
            Assert.That(
                CountParameters(ProductionTypes(), typeof(RuleModificationSet)),
                Is.GreaterThanOrEqualTo(30),
                "The scan found fewer modification-set parameters than the production surface is " +
                "known to declare, so the subject set or the parameter-type match has silently " +
                "narrowed and the rule could pass vacuously.");

            Assert.That(
                CountParameters(ModificationSurfaceTypes(), RuleAffectingTypes),
                Is.GreaterThanOrEqualTo(20),
                "The widened scan found fewer rule-affecting parameters than the modification query " +
                "surface is known to declare, so its subject set or type match has narrowed and it " +
                "could pass vacuously.");
        }

        private static IReadOnlyList<string> FindOptionalParameters(
            IEnumerable<Type> types,
            params Type[] parameterTypes) =>
            ScanParameters(types, parameterTypes)
                .Where(entry => entry.Parameter.IsOptional || entry.Parameter.HasDefaultValue)
                .Select(entry => entry.Description)
                .ToArray();

        private static int CountParameters(IEnumerable<Type> types, params Type[] parameterTypes) =>
            ScanParameters(types, parameterTypes).Count();

        private static IEnumerable<(ParameterInfo Parameter, string Description)> ScanParameters(
            IEnumerable<Type> types,
            IReadOnlyList<Type> parameterTypes)
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
                        if (parameterTypes.Contains(parameter.ParameterType))
                        {
                            yield return (parameter,
                                $"{type.FullName}.{member.Name}({parameter.ParameterType.Name} " +
                                $"{parameter.Name})");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The modification query surface: the namespace that owns the live set, its derivation, and
        /// the queries that read it. Scoped there rather than across all production types because a
        /// rule-affecting type can legitimately be an optional convenience elsewhere - a setup or
        /// diagnostic API may take a Tie Breaker it does not resolve rules with - while inside this
        /// namespace such a parameter is always part of a rules answer.
        /// </summary>
        private static IReadOnlyList<Type> ModificationSurfaceTypes() =>
            ProductionTypes()
                .Where(type => string.Equals(
                    type.Namespace, typeof(RuleModificationSet).Namespace, StringComparison.Ordinal))
                .ToArray();

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

        /// <summary>
        /// Deliberate violation of the widened rule: the ordering state and the acting object are
        /// optional, so omitting either changes the answer with nothing to notice.
        /// </summary>
        private sealed class OptionalRuleAffectingSample
        {
            public bool Evaluate(RuleObject target, TieBreakerState tieBreaker = default,
                RuleObject actor = default) =>
                tieBreaker.IsDefined && !actor.Equals(target);
        }

        /// <summary>Compliant twin: both are required, so an absent one is written as Unrevealed or None.</summary>
        private sealed class ExplicitRuleAffectingSample
        {
            public bool Evaluate(RuleObject target, TieBreakerState tieBreaker, RuleObject actor) =>
                tieBreaker.IsDefined && !actor.Equals(target);
        }
    }
}
