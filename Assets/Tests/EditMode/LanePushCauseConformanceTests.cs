using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.6 acceptance check 4, stated as a rule rather than as three sampled cases.
    ///
    /// Rulebook p.8: "A Lane Push can never be triggered by moving, pushing, or swapping a minion. It
    /// must be either defeated, or removed." Three tests showing that a move, a push, and a swap of the
    /// last Battle-Zone minion record nothing would pass just as happily on an engine that had wired a
    /// fourth displacement to the cause and forgotten to test it. This rule instead holds that exactly
    /// one thing in the whole engine asks p.8's question, so a displacement cannot acquire the cause by
    /// being written later.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class LanePushCauseConformanceTests
    {
        [Test]
        public void NothingButTheRemovalStage_AsksWhetherALanePushIsOwed()
        {
            var subjects = AuditedTypes().ToArray();

            Assert.That(ILConformanceScanner.UnresolvedTokens(subjects), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(FindQualificationCallers(subjects), Is.Empty,
                "Something other than the removal stage is asking p.8's last-minion question. Moving, " +
                "pushing, and swapping a minion can never trigger a Lane Push, and the way that stays " +
                "true is that they cannot reach the question at all.");
        }

        [Test]
        public void TheRemovalStage_DoesAskIt()
        {
            Assert.That(
                ILConformanceScanner.References(
                    ILConformanceScanner.WithNestedTypes(typeof(RemovalStageDecision)),
                    Qualification()),
                Is.True,
                "A rule forbidding every caller is satisfied by an engine with no callers at all, so " +
                "the one permitted caller must be shown to be there.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(FindQualificationCallers(new[] { typeof(DisplacementAskingSample) }), Is.Not.Empty,
                "A displacement that asks whether it emptied the Battle Zone must be reported, or the " +
                "rule is inert.");

            Assert.That(FindQualificationCallers(new[] { typeof(DisplacementLeavingItAloneSample) }), Is.Empty,
                "A displacement that only relocates must not be reported, or the rule bans the " +
                "ordinary shape it exists to permit.");
        }

        [Test]
        public void TheNamedMember_Resolves()
        {
            Assert.That(Qualification(), Is.Not.Null,
                "A named member no longer resolves. Correct the rule rather than deleting it; a rule " +
                "that names nothing protects nothing.");
        }

        // -------------------------------------------------------------------------------------------
        // The rule
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<string> FindQualificationCallers(IEnumerable<Type> types)
        {
            var forbidden = Qualification();
            return types
                .Where(type => !IsRemovalStage(type))
                .Where(type => ILConformanceScanner.References(new[] { type }, forbidden))
                .Select(type => $"{type.FullName} asks the Lane-Push qualification question")
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// The removal stage itself, and the query that answers it. Both are excluded because the rule
        /// is about who may ask, not about the question existing.
        /// </summary>
        private static bool IsRemovalStage(Type type) =>
            type == typeof(RemovalStageDecision) ||
            type == typeof(LanePushQualificationQuery) ||
            type.DeclaringType == typeof(RemovalStageDecision);

        /// <summary>
        /// Every Core type, so a displacement written after this milestone is audited by existing
        /// rather than by being added to a list. Compiler-generated closures are skipped for the reason
        /// every scan here skips them: they carry the enclosing method's instructions, which are
        /// already scanned. The namespace filter matters in the cross-platform harness, which compiles
        /// Core and these fixtures into one assembly, so without it this rule would report its own
        /// deliberate violation sample.
        /// </summary>
        private static IEnumerable<Type> AuditedTypes() =>
            ILConformanceScanner.TypesIn(typeof(RemoveOperation).Assembly)
                .Where(type => !type.Name.StartsWith("<", StringComparison.Ordinal))
                .Where(type => type.Namespace != null &&
                    type.Namespace.StartsWith("GuardsDigital.Core", StringComparison.Ordinal));

        private static System.Reflection.MethodInfo Qualification() =>
            ILConformanceScanner.Method(
                typeof(LanePushQualificationQuery),
                nameof(LanePushQualificationQuery.AfterRemoval));

        // -------------------------------------------------------------------------------------------
        // The samples the rule is measured against
        // -------------------------------------------------------------------------------------------

        /// <summary>A displacement that wrongly treats an emptied Battle Zone as p.8's cause.</summary>
        private static class DisplacementAskingSample
        {
            public static bool DidItEmptyTheZone(
                BoardObjectReference subject,
                HexCoordinate from,
                IBoardDisplacementAuthority board,
                MatchParticipants participants,
                RuleModificationSet modifications) =>
                LanePushQualificationQuery.AfterRemoval(
                    subject.AsRuleObject(), TeamSide.Red, from, board, participants, modifications)
                    .Qualifies;
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
