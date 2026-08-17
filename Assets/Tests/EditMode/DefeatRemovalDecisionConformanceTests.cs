using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.6b acceptance check 1, stated as a rule rather than as one sampled call site, in the
    /// shape milestone 6.6 used to hold p.8's Lane-Push question to a single caller.
    ///
    /// The defect this closes was not that the Attack forgot to ask whether a removal may happen; it was
    /// that the Attack could not ask, because it decided the question itself and recorded the answer as
    /// a constant. A test that watched one prevented Defeat would pass just as happily on an engine that
    /// grew a second Defeat producer later and left it deciding for itself, which is precisely how the
    /// two paths came to disagree in the first place. So the rule is stated over a subject set derived
    /// from the contract: whatever builds p.13's staged Defeat outcome must reach the shared removal
    /// decision, and a producer written later joins the rule by building one.
    ///
    /// Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </summary>
    public sealed class DefeatRemovalDecisionConformanceTests
    {
        [Test]
        public void EverythingThatStagesADefeat_AsksTheSharedRemovalDecision()
        {
            var subjects = AuditedTypes().ToArray();

            Assert.That(ILConformanceScanner.UnresolvedTokens(subjects), Is.Empty,
                "Unresolved IL tokens mean this scan skipped instructions, so a violation could hide " +
                "behind one.");

            Assert.That(FindDefeatStagersDecidingForThemselves(subjects), Is.Empty,
                "Something stages p.13's Defeat without asking the shared removal decision. A minion " +
                "protected from a Skill's Remove would then still die to that path's Defeat, which is " +
                "the half-true rule this milestone closed.");
        }

        [Test]
        public void TheDefeatPath_IsActuallyAmongThem()
        {
            Assert.That(FindDefeatStagers(AuditedTypes()), Is.Not.Empty,
                "A rule about everything that stages a Defeat is satisfied by an engine that stages " +
                "none, so the path it was written for must be shown to be there.");
        }

        [Test]
        public void ConformanceRule_DistinguishesAllowedAndForbiddenExamples()
        {
            Assert.That(
                FindDefeatStagersDecidingForThemselves(new[] { typeof(DefeatDecidingForItselfSample) }),
                Is.Not.Empty,
                "A Defeat that hard-codes its own removal result must be reported, or the rule is inert.");

            Assert.That(
                FindDefeatStagersDecidingForThemselves(new[] { typeof(DefeatAskingTheDecisionSample) }),
                Is.Empty,
                "A Defeat that asks the shared decision must not be reported, or the rule bans the " +
                "shape it exists to require.");

            Assert.That(
                FindDefeatStagersDecidingForThemselves(new[] { typeof(NotADefeatAtAllSample) }),
                Is.Empty,
                "The rule is addressed to Defeat producers; an ordinary board mutation is none of its " +
                "business, and a rule that reported one would be reporting every caller of the board.");
        }

        [Test]
        public void TheNamedMembers_Resolve()
        {
            Assert.That(RemovalDecision(), Is.Not.Null);
            Assert.That(DefeatStages(), Is.Not.Empty,
                "A named member no longer resolves. Correct the rule rather than deleting it; a rule " +
                "that names nothing protects nothing.");
        }

        // -------------------------------------------------------------------------------------------
        // The rule
        // -------------------------------------------------------------------------------------------

        private static IReadOnlyList<string> FindDefeatStagersDecidingForThemselves(IEnumerable<Type> types) =>
            FindDefeatStagers(types)
                .Where(type => !ILConformanceScanner.References(new[] { type }, RemovalDecision()))
                .Select(type => $"{type.FullName} stages a Defeat without the shared removal decision")
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        /// <summary>
        /// Every type that builds p.13's staged Defeat outcome. Derived from the contract rather than
        /// listed, so the rule reaches a second producer by existing - the discipline milestone 6.5ad
        /// recorded when it derived a rule's subjects from the declared capability schemas.
        /// </summary>
        private static IReadOnlyList<Type> FindDefeatStagers(IEnumerable<Type> types)
        {
            var stages = DefeatStages().ToArray();
            return types
                .Where(type => type != typeof(MinionDefeatResolution))
                .Where(type => stages.Any(stage => ILConformanceScanner.References(new[] { type }, stage)))
                .ToArray();
        }

        /// <summary>
        /// Every Core type, so a Defeat producer written after this milestone is audited by existing
        /// rather than by being added to a list. Compiler-generated closures are skipped because they
        /// carry the enclosing method's instructions, which are already scanned, and the namespace
        /// filter matters in the cross-platform harness, which compiles Core and these fixtures into one
        /// assembly and would otherwise hand this rule its own deliberate violation sample.
        /// </summary>
        private static IEnumerable<Type> AuditedTypes() =>
            ILConformanceScanner.TypesIn(typeof(MinionDefeatResolution).Assembly)
                .Where(type => !type.Name.StartsWith("<", StringComparison.Ordinal))
                .Where(type => type.Namespace != null &&
                    type.Namespace.StartsWith("GuardsDigital.Core", StringComparison.Ordinal));

        private static IEnumerable<MethodInfo> DefeatStages() =>
            ILConformanceScanner.Methods(typeof(MinionDefeatResolution), nameof(MinionDefeatResolution.Removed))
                .Concat(ILConformanceScanner.Methods(
                    typeof(MinionDefeatResolution), nameof(MinionDefeatResolution.Prevented)));

        private static MethodInfo RemovalDecision() =>
            ILConformanceScanner.Method(
                typeof(RemovalStageDecision), nameof(RemovalStageDecision.IsPrevented));

        // -------------------------------------------------------------------------------------------
        // The samples the rule is measured against
        // -------------------------------------------------------------------------------------------

        /// <summary>The pre-6.6b shape: the removal result is a constant the caller wrote down.</summary>
        private static class DefeatDecidingForItselfSample
        {
            public static MinionDefeatResolution Stage(
                DefeatOutcome outcome,
                DefeatRewardResult reward,
                GuardsDigital.Core.Units.Minions.MinionOccupancy removed) =>
                MinionDefeatResolution.Removed(
                    outcome,
                    reward,
                    new DefeatRemovalAttempt(outcome.Id, outcome.TargetId),
                    new DefeatRemovalResult(outcome.Id, outcome.TargetId, DefeatRemovalResultKind.Removed),
                    removed,
                    LanePushQualification.None);
        }

        /// <summary>The required shape: the rules are asked whether the removal happens.</summary>
        private static class DefeatAskingTheDecisionSample
        {
            public static MinionDefeatResolution Stage(
                DefeatOutcome outcome,
                DefeatRewardResult reward,
                BoardObjectReference subject,
                RemovalStageDecision removalStage)
            {
                var attempt = new DefeatRemovalAttempt(outcome.Id, outcome.TargetId);
                if (removalStage.IsPrevented(subject))
                {
                    return MinionDefeatResolution.Prevented(
                        outcome,
                        reward,
                        attempt,
                        new DefeatRemovalResult(
                            outcome.Id, outcome.TargetId, DefeatRemovalResultKind.RemovalPrevented),
                        LanePushQualification.None);
                }

                throw new InvalidOperationException("The sample exists for its instructions, not its behaviour.");
            }
        }

        /// <summary>An ordinary board mutation that stages no Defeat, which the rule must leave alone.</summary>
        private static class NotADefeatAtAllSample
        {
            public static bool Relocate(
                BoardObjectReference subject,
                GuardsDigital.Core.Board.HexCoordinate destination,
                IBoardDisplacementAuthority board) =>
                board.CommitValidatedDisplacement(subject, destination).Accepted;
        }
    }
}
