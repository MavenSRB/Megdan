using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.20d, and written because the seventh family had already fallen through.
    ///
    /// <para>The status derivation ran two phase checks and six pending-family branches - Initiative,
    /// Defense, Respawn, Attack target, Movement, displaced-minion correction - and had no
    /// <c>PendingEffectChoice</c> branch at all. So every effect choice since milestone 8.15 reached
    /// the table under "Initiative N: choose an action or End Turn", and a player asked which card to
    /// discard was labelled as though they were picking an action. Nothing failed, because nothing
    /// compared the families the presenter treats as a pause against the families it can name.</para>
    ///
    /// <para>These cases are that comparison, and they read compiled code rather than a written-down
    /// list: a family added to the pause list without a branch in the status derivation fails here
    /// rather than in play. It is the rule and not the fix that keeps this closed - milestone 8.21's
    /// removal-prevention pause is the next family to arrive, and it would otherwise inherit exactly
    /// this defect by construction.</para>
    ///
    /// <para>It is the same move milestone 6.1k made for the architecture fixtures. Both sides of
    /// every comparison come from the type system, so a rename moves them together and the spelling
    /// at the call site is irrelevant.</para>
    /// </summary>
    public sealed class StatusDerivationConformanceTests
    {
        private static readonly Type Presenter = typeof(RoundActionPresenter);

        // The four derivations this rule is about are private to the presenter, so they are named as
        // text. Every name is proven to resolve to exactly one method body by `Scan`, which fails
        // naming what it looked for rather than scanning nothing.
        private const string FormatStatus = "FormatStatus";
        private const string HasPendingDecision = "HasPendingDecision";
        private const string AwaitingAnAnsweredDecision = "IsAwaitingAnAnsweredDecision";
        private const string InsideAnUnfinishedResolution = "IsInsideAnUnfinishedResolution";

        /// <summary>
        /// The eight families the presenter knows about today, and the three unfinished resolutions
        /// beside them. Both are floors rather than the sets themselves, so a rule scanning a
        /// derivation that has quietly lost its branches fails instead of passing, and neither has to
        /// be re-listed when a ninth family arrives.
        ///
        /// <para>Milestone 8.21 raised the first from seven: its removal-prevention pause is the
        /// eighth family, and it arrived named because this rule required it to. The floor moves with
        /// each family for the reason it exists at all - a floor that lagged behind would stop
        /// noticing branches going missing.</para>
        /// </summary>
        private const int KnownDecisionFamilies = 8;

        private const int KnownUnfinishedResolutions = 3;

        [Test]
        public void EveryPendingDecisionFamilyIsNamedByTheStatusDerivation()
        {
            var decisions = ProjectionReadsOf(AwaitingAnAnsweredDecision);
            var named = ProjectionReadsOf(FormatStatus);

            Assert.That(decisions, Has.Count.GreaterThanOrEqualTo(KnownDecisionFamilies),
                "The pause list is what this rule compares against, so a scan finding fewer families " +
                "than the presenter has always had is a broken rule rather than a pass.");
            Assert.That(
                decisions.Where(family => !named.Contains(family)).Select(family => family.Name)
                    .OrderBy(name => name, StringComparer.Ordinal).ToArray(),
                Is.Empty,
                "A pending family the presenter treats as a pause and the status derivation never " +
                "reads is a decision the player meets under whatever the fallback happens to say. " +
                "Add its branch to the status derivation; do not take it off the pause list.");
        }

        [Test]
        public void ThePauseListReadsNothingOfItsOwnAndIsExactlyItsTwoHalves()
        {
            var scanned = Scan(HasPendingDecision).Single();

            Assert.That(ProjectionReadsOf(HasPendingDecision), Is.Empty,
                "Milestone 7.9c left one list so there was no second place to forget a family. A " +
                "member read here rather than in one of the two halves is that second place, and it " +
                "would make the match treat a family as a pause that this rule never checks.");
            Assert.That(scanned.Calls(ILConformanceScanner.Method(Presenter, AwaitingAnAnsweredDecision)),
                Is.True);
            Assert.That(scanned.Calls(ILConformanceScanner.Method(Presenter, InsideAnUnfinishedResolution)),
                Is.True);
        }

        [Test]
        public void AQuestionAndAnUnfinishedResolutionAreNeverTheSameFamily()
        {
            var decisions = ProjectionReadsOf(AwaitingAnAnsweredDecision);
            var frames = ProjectionReadsOf(InsideAnUnfinishedResolution);

            Assert.That(frames, Has.Count.GreaterThanOrEqualTo(KnownUnfinishedResolutions));
            Assert.That(
                frames.Intersect(decisions).Select(member => member.Name).ToArray(), Is.Empty,
                "A family declared both a question and a frame would let the rule above be satisfied " +
                "by calling it a frame, which is exactly the evasion this split must not create.");
        }

        [Test]
        public void NoScannedInstructionInTheseDerivationsIsUnresolved()
        {
            var unresolved = Scan(
                    FormatStatus,
                    HasPendingDecision,
                    AwaitingAnAnsweredDecision,
                    InsideAnUnfinishedResolution)
                .SelectMany(method => method.UnresolvedTokens)
                .ToArray();

            Assert.That(unresolved, Is.Empty,
                "A rule must not be evadable through a construct the scanner silently skipped.");
        }

        /// <summary>
        /// Every <see cref="MatchSessionProjection"/> property these method bodies read. A property
        /// read compiles to its getter, so the getter is the identity both sides of every comparison
        /// above are made on - never the name it happens to be spelled with at the call site.
        /// </summary>
        private static List<MethodInfo> ProjectionReadsOf(params string[] methodNames) =>
            Scan(methodNames)
                .SelectMany(method => method.Instructions)
                .Select(instruction => instruction.Member as MethodInfo)
                .Where(member => member != null &&
                    member.DeclaringType == typeof(MatchSessionProjection) &&
                    member.IsSpecialName &&
                    member.Name.StartsWith("get_", StringComparison.Ordinal))
                .Distinct()
                .ToList();

        private static IReadOnlyList<ILConformanceScanner.ScannedMethod> Scan(params string[] methodNames)
        {
            var scanned = ILConformanceScanner.Scan(Presenter)
                .Where(method => methodNames.Contains(method.Method.Name, StringComparer.Ordinal))
                .ToArray();
            Assert.That(scanned, Has.Length.EqualTo(methodNames.Length),
                "A conformance rule cannot name a member that does not exist; correct the rule " +
                "rather than deleting it. Looked for: " + string.Join(", ", methodNames));
            return scanned;
        }
    }
}
