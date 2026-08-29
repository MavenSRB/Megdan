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

        /// <summary>
        /// Milestone 8.30b's closing check, and it exists because the amendment changed what this
        /// sentence is for.
        ///
        /// <para>Until this milestone the narration was the first row of the round dock down beside
        /// the cards; the user-approved amendment moved it into the top status bar and asked that the
        /// bar report what the match is doing <em>at every step</em>. The rule above compares
        /// <c>FormatStatus</c> against the presenter's own pause list, which is the set of families
        /// whose standing suppresses the offers a second action would produce - and two of the ten
        /// families the project declares are not on it, because both already suppress their own
        /// offers by name. So both passed the rule above while reaching a player as "ROUND 1 COMPLETE
        /// - END OF ROUND" with the match in fact waiting on an answer.</para>
        ///
        /// <para>This compares against the declared inventory instead, which is the whole set. It is
        /// the same comparison milestone 8.20d made and the same failure it found - a family the
        /// status derivation never reads is a decision the player meets under whatever the fallback
        /// happens to say - widened from the presenter's list to the project's.</para>
        /// </summary>
        [Test]
        public void EveryDeclaredPendingFamilyIsNamedByTheStatusDerivation()
        {
            var declared = DecisionFamilyInventory.ChoiceProjectionMembers;
            var named = ProjectionReadsOf(FormatStatus)
                .Select(getter => getter.Name.Substring("get_".Length))
                .Distinct()
                .ToArray();

            Assert.That(declared, Has.Length.GreaterThan(KnownDecisionFamilies),
                "The declared inventory is wider than the presenter's pause list, which is why this " +
                "rule is not the one above. An inventory that has shrunk to the pause list is a " +
                "broken rule rather than a pass.");
            Assert.That(named, Has.Length.GreaterThanOrEqualTo(declared.Length),
                "A scan of the status derivation that finds fewer projection reads than there are " +
                "declared families has stopped seeing the method rather than proved anything - the " +
                "vacuity milestone 8.30b's retype produced in this file once already.");
            Assert.That(
                declared.Where(member => !named.Contains(member, StringComparer.Ordinal))
                    .OrderBy(name => name, StringComparer.Ordinal).ToArray(),
                Is.Empty,
                "A declared pending family the status derivation never reads is a step the top status " +
                "bar cannot report. Add its branch to the derivation; adding it to the pause list " +
                "instead would change which offers are suppressed in order to fix what is said.");
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
        /// Every projection property these method bodies read. A property read compiles to its getter,
        /// so the getter is the identity both sides of every comparison above are made on - never the
        /// name it happens to be spelled with at the call site.
        ///
        /// <para>Milestone 8.30b: both declaring types count. The presenter now reads through
        /// <see cref="IMatchReadModel"/>, which is what lets one surface render the Director's view and
        /// a seat's; naming only the concrete projection made this scan find nothing at all and pass
        /// its own vacuity guard instead of the rule.</para>
        /// </summary>
        private static List<MethodInfo> ProjectionReadsOf(params string[] methodNames) =>
            Scan(methodNames)
                .SelectMany(method => method.Instructions)
                .Select(instruction => instruction.Member as MethodInfo)
                .Where(member => member != null &&
                    (member.DeclaringType == typeof(IMatchReadModel) ||
                        member.DeclaringType == typeof(MatchSessionProjection)) &&
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
