using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8g acceptance check 4: no rule naming a unit type reads a kind without going
    /// through <see cref="IntrinsicKindQuery"/> or <see cref="EffectiveUnitKindQuery"/>.
    ///
    /// The forbidden members are the <see cref="RuleObject.Kind"/> getter - heroes have no kind field
    /// and register membership is the kind, so this getter is the entire hero/minion bypass surface -
    /// and the <see cref="RuleModification.EffectiveUnitKind"/> getter, which would hand-read a
    /// `Counts As` declaration instead of asking <see cref="RuleModificationSet"/>. Register
    /// enumeration and <see cref="GuardsDigital.Core.Setup.StandardMinionType"/> subtype reads stay
    /// legal: enumerating a typed register is unit enumeration, and Heavy/melee/ranged is a subtype
    /// vocabulary the two queries do not own - the unit-type question ledger in
    /// Docs/AI/Rules/RULE_SYSTEMS.md carries those citations.
    ///
    /// <see cref="UnitPresenceQuery"/> is deliberately outside the subject set: it reads
    /// <c>RuleObject.Kind</c> only for canonical identity ordering and already routes its type
    /// question through the effective query - the same reasoning that keeps <c>MatchSession</c> out
    /// of the dispatcher-algorithm rule.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class UnitKindQueryConformanceTests
    {
        [Test]
        public void NoAuditedRule_ReadsAUnitKindOutsideTheTwoQueries()
        {
            var subjects = AuditedRuleTypes();

            Assert.That(ILConformanceScanner.UnresolvedTokens(subjects), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(FindKindReadViolations(subjects), Is.Empty,
                "An audited rule is reading a unit kind directly. The intrinsic-or-effective choice " +
                "must be declared by routing through IntrinsicKindQuery or EffectiveUnitKindQuery, " +
                "or a Counts As declaration silently changes nothing, which is the defect 6.8g " +
                "closes.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(FindKindReadViolations(new[] { typeof(KindReadingRuleSample) }), Is.Not.Empty,
                "A rule that branches on RuleObject.Kind must be reported, or the rule is inert.");

            Assert.That(FindKindReadViolations(new[] { typeof(QueryRoutedRuleSample) }), Is.Empty,
                "A rule that routes its question through the two queries must not be reported, or " +
                "the rule bans the ordinary shape it exists to permit.");
        }

        /// <summary>
        /// The negative rule is satisfied by an engine that never asks about kinds at all, so the two
        /// queries must be shown to own the reads, and the production consumer must be shown to reach
        /// the effective path - the acceptance line this milestone exists to close.
        /// </summary>
        [Test]
        public void TheTwoQueries_OwnTheKindReads_AndTheProductionConsumerReachesTheEffectivePath()
        {
            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(IntrinsicKindQuery)),
                    RuleObjectKindGetter()),
                Is.True,
                "If IntrinsicKindQuery stopped reading RuleObject.Kind, the rule would pass while " +
                "no intrinsic question could be answered at all.");

            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(RuleModificationSet)),
                    EffectiveUnitKindGetter()),
                Is.True,
                "If RuleModificationSet stopped reading declared effective kinds, Counts As could " +
                "not answer anything.");

            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(AttackTargetQuery)),
                    ILConformanceScanner.Method(
                        typeof(UnitPresenceQuery), nameof(UnitPresenceQuery.FindUnitsInZone))),
                Is.True,
                "The p.13 Heavy-immunity accompaniment clause is the effective type's first " +
                "production consumer: AttackTargetQuery must ask the zone-scoped presence question, " +
                "which applies Counts As, rather than scanning the intrinsic minion register.");
        }

        [Test]
        public void TheForbiddenMembers_Resolve()
        {
            Assert.That(RuleObjectKindGetter(), Is.Not.Null,
                "A forbidden member no longer resolves. Correct the rule rather than deleting it; a " +
                "rule that names nothing protects nothing.");
            Assert.That(EffectiveUnitKindGetter(), Is.Not.Null,
                "A forbidden member no longer resolves. Correct the rule rather than deleting it; a " +
                "rule that names nothing protects nothing.");
        }

        private static IReadOnlyList<string> FindKindReadViolations(IEnumerable<Type> types) =>
            ILConformanceScanner.FindForbiddenReferences(
                types,
                new MemberInfo[] { RuleObjectKindGetter(), EffectiveUnitKindGetter() },
                "unit-kind read outside IntrinsicKindQuery/EffectiveUnitKindQuery");

        /// <summary>
        /// The audited rule types from the unit-type question ledger. The two backends are internal
        /// to Core, so they resolve by name off the assembly, with a null guard so a rename fails
        /// loudly rather than silently narrowing the subject set.
        /// </summary>
        private static Type[] AuditedRuleTypes()
        {
            var core = typeof(AttackTargetQuery).Assembly;
            var byName = new[]
                {
                    "GuardsDigital.Core.Rules.CardResolution.AttackDefenseCardActionStrategyBackend",
                    "GuardsDigital.Core.Rules.CardResolution.MovementCardActionStrategyBackend",
                    "GuardsDigital.Core.Cards.MinionBattleRules",
                    "GuardsDigital.Core.Cards.PendingMinionBattleChoice",
                    "GuardsDigital.Core.Cards.RoundResolutionAuthority"
                }
                .Select(name => core.GetType(name))
                .ToArray();
            Assert.That(byName, Has.None.Null,
                "An audited rule type no longer resolves by name; correct the subject list rather " +
                "than letting the scan narrow silently.");

            return new[]
                {
                    typeof(AttackTargetQuery),
                    typeof(DefenseValueQuery),
                    typeof(FastTravelActionQuery),
                    typeof(GeneralRuleActionOfferQuery)
                }
                .Concat(byName)
                .SelectMany(ILConformanceScanner.WithNestedTypes)
                .Distinct()
                .ToArray();
        }

        private static MethodInfo RuleObjectKindGetter() =>
            typeof(RuleObject).GetProperty(nameof(RuleObject.Kind),
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly)
                ?.GetGetMethod(true);

        private static MethodInfo EffectiveUnitKindGetter() =>
            typeof(RuleModification).GetProperty(nameof(RuleModification.EffectiveUnitKind),
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly)
                ?.GetGetMethod(true);

        /// <summary>Deliberate violation: decides a rule by reading the kind directly.</summary>
        private sealed class KindReadingRuleSample
        {
            public bool IsMinion(RuleObject unit) => unit.Kind == RuleObjectKind.Minion;
        }

        /// <summary>Compliant twin: declares its question by routing through the two queries.</summary>
        private sealed class QueryRoutedRuleSample
        {
            public bool IsMinionMiniature(RuleObject unit) =>
                IntrinsicKindQuery.IsMinionMiniature(unit);

            public bool CountsAsMinion(RuleObject unit, RuleModificationSet modifications) =>
                EffectiveUnitKindQuery.Is(unit, EffectiveUnitKind.Minion, modifications);
        }
    }
}
