using System;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Displacement;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.5ad's one statement of pages 10 and 11, on its own.
    ///
    /// p.10: "Steps which include 'you may', 'up to' and 'if able' are non-mandatory, all other steps
    /// are mandatory." p.11: "Unless the distance is optional ('up to 2 spaces'), or the move itself is
    /// optional ('you may move'), you must move the exact number of spaces, or stop performing the
    /// action at that step." The pair separates two things that read alike: an optional *distance* lets
    /// the chooser travel fewer spaces, while an optional *step* leaves the number exact and lets the
    /// chooser decline the whole thing. Only the first widens what this reading admits, which is why
    /// `may` and `if able` sit with `Mandatory` here rather than with `up to`.
    /// </summary>
    public sealed class AdmittedDistancesTests
    {
        [Test]
        public void AnOptionalDistance_IsSatisfiedByAnythingFromNoSpacesToTheDeclaredBound()
        {
            var admitted = AdmittedDistances.ForDeclared(EffectFailurePolicy.UpTo, 2);

            Assert.That(admitted.Minimum, Is.EqualTo(0));
            Assert.That(admitted.Maximum, Is.EqualTo(2));
            Assert.That(admitted.AdmitsStandingStill, Is.True,
                "No clause sets a floor of one space, so `up to 2 spaces` admits moving none at all.");
            Assert.That(admitted.Admits(0), Is.True);
            Assert.That(admitted.Admits(1), Is.True);
            Assert.That(admitted.Admits(2), Is.True);
            Assert.That(admitted.Admits(3), Is.False, "The declared number is still the bound.");
        }

        [Test]
        public void AnOptionalStep_LeavesTheNumberExact()
        {
            // `you may move 2 spaces` and `move 2 spaces, if able` are p.10's other two non-mandatory
            // wordings. Both make the step optional and neither makes the distance optional, which is
            // p.11's own distinction between "the distance is optional" and "the move itself is
            // optional" - so a chooser who takes the step at all takes the whole two spaces.
            foreach (var policy in new[]
            {
                EffectFailurePolicy.Mandatory, EffectFailurePolicy.May, EffectFailurePolicy.IfAble
            })
            {
                var admitted = AdmittedDistances.ForDeclared(policy, 2);

                Assert.That(admitted.Minimum, Is.EqualTo(2), policy.ToString());
                Assert.That(admitted.Maximum, Is.EqualTo(2), policy.ToString());
                Assert.That(admitted.AdmitsStandingStill, Is.False, policy.ToString());
                Assert.That(admitted.Admits(1), Is.False, policy.ToString());
                Assert.That(admitted.Admits(2), Is.True, policy.ToString());
            }
        }

        [Test]
        public void TheEnumeratedDistances_AreEveryAdmittedNumberInAscendingOrder()
        {
            // This is the chooser's own candidate list wherever the distance is the decision, as p.18's
            // multi-target push makes it, so its order is projected and has to be deterministic.
            Assert.That(
                AdmittedDistances.ForDeclared(EffectFailurePolicy.UpTo, 3).Enumerate(),
                Is.EqualTo(new[] { 0, 1, 2, 3 }));
            Assert.That(
                AdmittedDistances.ForDeclared(EffectFailurePolicy.Mandatory, 3).Enumerate(),
                Is.EqualTo(new[] { 3 }));
        }

        [Test]
        public void ADeclaredDistanceOfZero_ReadsTheSameWayUnderEitherWording()
        {
            // Nothing declares it, but the reading has to answer: "move exactly zero spaces" is
            // satisfied by standing still, so both arms collapse onto the same admitted set rather than
            // one of them admitting nothing at all.
            foreach (var policy in new[] { EffectFailurePolicy.UpTo, EffectFailurePolicy.Mandatory })
            {
                var admitted = AdmittedDistances.ForDeclared(policy, 0);

                Assert.That(admitted.Enumerate(), Is.EqualTo(new[] { 0 }), policy.ToString());
                Assert.That(admitted.AdmitsStandingStill, Is.True, policy.ToString());
            }
        }

        [Test]
        public void ANegativeDeclaredDistance_IsRefusedRatherThanReadAsNone()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AdmittedDistances.ForDeclared(EffectFailurePolicy.UpTo, -1));
        }
    }
}
