using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.9 acceptance check 2, stated as a rule rather than as eight sampled cases.
    ///
    /// Rulebook p.8: "A Lane Push can never be triggered by moving, pushing, or swapping a minion. It
    /// must be either defeated, or removed." Milestone 6.6 already holds exactly one thing in the engine
    /// to asking p.8's question; this holds exactly one thing to turning its answer into a request. The
    /// two together are what make the exclusion structural: a displacement can reach neither end, so it
    /// cannot acquire the push by being written later.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class ImmediateLanePushEmitterConformanceTests
    {
        [Test]
        public void NothingButTheCardActionDispatcher_BuildsAMidTurnLanePushRequest()
        {
            var subjects = AuditedTypes().ToArray();

            Assert.That(ILConformanceScanner.UnresolvedTokens(subjects), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(FindEmitters(subjects), Is.Empty,
                "Something other than the action boundary is producing p.8's mid-turn request. The " +
                "request is a consequence of an action completing, and one producer is what keeps the " +
                "recorded cause the only way to reach it.");
        }

        [Test]
        public void TheCardActionDispatcher_DoesBuildOne()
        {
            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(CardActionResolutionAuthority)),
                    Emission()),
                Is.True,
                "A rule forbidding every caller is satisfied by an engine with no callers at all, so " +
                "the one permitted caller must be shown to be there.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(FindEmitters(new[] { typeof(DisplacementEmittingSample) }), Is.Not.Empty,
                "A displacement that turns an emptied Battle Zone into a push must be reported, or the " +
                "rule is inert.");

            Assert.That(FindEmitters(new[] { typeof(DisplacementLeavingItAloneSample) }), Is.Empty,
                "A displacement that only relocates must not be reported, or the rule bans the " +
                "ordinary shape it exists to permit.");
        }

        [Test]
        public void TheNamedMember_Resolves()
        {
            Assert.That(Emission(), Is.Not.Null,
                "A named member no longer resolves. Correct the rule rather than deleting it; a rule " +
                "that names nothing protects nothing.");
        }

        // -------------------------------------------------------------------------------------------
        // The rule
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<string> FindEmitters(IEnumerable<Type> types)
        {
            var forbidden = Emission();
            return types
                .Where(type => !IsPermittedEmitter(type))
                .Where(type => ILConformanceScanner.References(new[] { type }, forbidden))
                .Select(type => $"{type.FullName} builds a mid-turn Lane-Push request")
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// The dispatcher that owns the action boundary, and the query that answers it. Both are
        /// excluded because the rule is about who may produce a request, not about the question existing.
        /// </summary>
        private static bool IsPermittedEmitter(Type type) =>
            type == typeof(CardActionResolutionAuthority) ||
            type == typeof(ImmediateLanePushQuery) ||
            type.DeclaringType == typeof(CardActionResolutionAuthority);

        /// <summary>
        /// Every Core type, so an operation written after this milestone is audited by existing rather
        /// than by being added to a list. Compiler-generated closures are skipped for the reason every
        /// scan here skips them: they carry the enclosing method's instructions, which are already
        /// scanned. The namespace filter matters in the cross-platform harness, which compiles Core and
        /// these fixtures into one assembly, so without it this rule would report its own deliberate
        /// violation sample.
        /// </summary>
        private static IEnumerable<Type> AuditedTypes() =>
            ILConformanceScanner.TypesIn(typeof(ImmediateLanePushQuery).Assembly)
                .Where(type => !type.Name.StartsWith("<", StringComparison.Ordinal))
                .Where(type => type.Namespace != null &&
                    type.Namespace.StartsWith("GuardsDigital.Core", StringComparison.Ordinal));

        private static System.Reflection.MethodInfo Emission() =>
            ILConformanceScanner.Method(
                typeof(ImmediateLanePushQuery), nameof(ImmediateLanePushQuery.From));

        // -------------------------------------------------------------------------------------------
        // The samples the rule is measured against
        // -------------------------------------------------------------------------------------------

        /// <summary>A displacement that wrongly turns an emptied Battle Zone into p.8's push.</summary>
        private static class DisplacementEmittingSample
        {
            public static PendingImmediateLanePushRequest AfterMoving(
                IBoardDisplacementAuthority board,
                MatchParticipants participants,
                RuleModificationSet modifications) =>
                ImmediateLanePushQuery.From(
                    LanePushCauseKind.Removal,
                    new ActionResolutionId("sample"),
                    board,
                    participants,
                    modifications);
        }

        /// <summary>The ordinary shape: relocate the object and publish where it ended up.</summary>
        private static class DisplacementLeavingItAloneSample
        {
            public static HexCoordinate Relocate(
                BoardObjectReference subject,
                HexCoordinate destination,
                IBoardDisplacementAuthority board) =>
                board.CommitValidatedDisplacement(subject, destination).Destination;
        }
    }
}
