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

        // -------------------------------------------------------------------------------------------
        // Milestone 8.8: the distance a card writes as a choice between named numbers
        // -------------------------------------------------------------------------------------------

        [Test]
        public void APrintedFloor_NarrowsTheOptionalReadingToTheNumbersTheCardNames()
        {
            // Kinetic Blast: "Push up to 2 enemy units adjacent to you 3 or 4 spaces." The `up to` is
            // over the units, so the step is non-mandatory; the spaces are two named numbers, so the
            // reading has to admit those two and nothing between them and zero.
            var admitted = AdmittedDistances.ForDeclared(EffectFailurePolicy.UpTo, 4, 3);

            Assert.That(admitted.Enumerate(), Is.EqualTo(new[] { 3, 4 }));
            Assert.That(admitted.AdmitsStandingStill, Is.False,
                "The card names no number below three, so no wording of the step admits zero spaces.");
            Assert.That(admitted.NamesOneDistance, Is.False, "Two numbers are still a decision.");
        }

        [Test]
        public void APrintedFloorEqualToTheDistance_LeavesNothingToDecideUnderAnyWording()
        {
            // Kinetic Repulse: "Push up to 2 enemy units adjacent to you 3 spaces." The same
            // non-mandatory wording over the units, and one number over the spaces. Before the floor
            // existed the only way to say `up to` about the units was to make the spaces optional too.
            foreach (var policy in new[]
            {
                EffectFailurePolicy.UpTo, EffectFailurePolicy.Mandatory,
                EffectFailurePolicy.May, EffectFailurePolicy.IfAble
            })
            {
                var admitted = AdmittedDistances.ForDeclared(policy, 3, 3);

                Assert.That(admitted.Enumerate(), Is.EqualTo(new[] { 3 }), policy.ToString());
                Assert.That(admitted.NamesOneDistance, Is.True, policy.ToString());
            }
        }

        [Test]
        public void AFloorOfZero_IsTheReadingEveryDeclarationBeforeMilestoneEightEightHad()
        {
            foreach (var policy in new[] { EffectFailurePolicy.UpTo, EffectFailurePolicy.Mandatory })
            {
                Assert.That(
                    AdmittedDistances.ForDeclared(policy, 2, 0).Enumerate(),
                    Is.EqualTo(AdmittedDistances.ForDeclared(policy, 2).Enumerate()),
                    policy + ": naming no floor has to read exactly as it did before there was one.");
            }
        }

        [Test]
        public void AnExactDeclarationIgnoresTheFloor_BecauseThereIsNothingBelowTheNumberToNarrow()
        {
            // A floor cannot raise what a declaration admits; p.11 already fixed an exact wording at
            // its declared number, and the floor only narrows the optional reading from below.
            Assert.That(
                AdmittedDistances.ForDeclared(EffectFailurePolicy.Mandatory, 4, 3).Enumerate(),
                Is.EqualTo(new[] { 4 }));
        }

        [Test]
        public void AFloorAboveTheDeclaredDistance_IsRefusedRatherThanReadAsAnEmptySet()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AdmittedDistances.ForDeclared(EffectFailurePolicy.UpTo, 3, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                AdmittedDistances.ForDeclared(EffectFailurePolicy.UpTo, 3, -1));
        }
    }
}
