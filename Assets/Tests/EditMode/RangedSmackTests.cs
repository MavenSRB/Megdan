using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z12 (ranged pass). How a strike that has to cross ground reads, and
    /// where the fact that it IS one actually lives.
    /// </summary>
    public sealed class RangedSmackTests
    {
        private const float Tolerance = 0.0005f;
        private const float Hex = AttackSpotlightTiming.HexStepWorld;

        [Test]
        public void AdjacentIsNotRanged_AndAnythingThatCrossedGroundIs()
        {
            Assert.That(RangedSmackProfile.IsRangedSeparation(Hex), Is.False,
                "Two pieces on neighbouring spaces crossed nothing.");
            Assert.That(RangedSmackProfile.IsRangedSeparation(Hex * 2f), Is.True);
            Assert.That(RangedSmackProfile.IsRangedSeparation(Hex * 4f), Is.True,
                "Sabina's Shootout prints Range 4, which is the case this exists for.");
        }

        /// <summary>
        /// The complaint the ranged pass was opened on - "a full-body lunge across N hexes reads
        /// wrong" - asserted as a property rather than trusted. It was ALREADY true at melee range
        /// because the shipped cue view caps its lunge; this keeps it true at every separation the
        /// board can produce, and tightens it as the target recedes.
        /// </summary>
        [Test]
        public void TheJab_NeverTraversesTheGapAndShortensAsTheTargetRecedes()
        {
            Assert.That(
                RangedSmackProfile.JabReach(Hex),
                Is.EqualTo(Mathf.Min(Hex * 0.42f, RangedSmackProfile.MaxJabReach)).Within(Tolerance),
                "At melee range this restates the cap KineticCueUnityView already applies, so the " +
                "two cannot drift apart.");

            var previous = float.MaxValue;
            for (var hexes = 2f; hexes <= 20f; hexes += 0.5f)
            {
                var separation = hexes * Hex;
                var reach = RangedSmackProfile.JabReach(separation);

                Assert.That(reach, Is.LessThanOrEqualTo(previous + Tolerance),
                    "The further the target, the less a lean toward it means.");
                Assert.That(reach, Is.GreaterThanOrEqualTo(RangedSmackProfile.MinJabReach - Tolerance),
                    "but never nothing, or the attacker reads as doing nothing at all.");
                Assert.That(reach, Is.LessThan(separation * 0.25f),
                    "and never anything like crossing the gap - that is the tracer's job.");
                previous = reach;
            }
        }

        [Test]
        public void TheTracer_LeavesFastAndArrivesExactlyOnce()
        {
            const float separation = Hex * 4f;
            var flight = RangedSmackProfile.TracerSeconds(separation);

            Assert.That(RangedSmackProfile.TracerProgress(0f, separation),
                Is.EqualTo(0f).Within(Tolerance));
            Assert.That(RangedSmackProfile.TracerProgress(flight, separation),
                Is.EqualTo(1f).Within(Tolerance));
            Assert.That(RangedSmackProfile.TracerProgress(flight * 2f, separation),
                Is.EqualTo(1f).Within(Tolerance), "It does not overshoot past its arrival.");
            Assert.That(RangedSmackProfile.TracerProgress(flight * 0.5f, separation),
                Is.GreaterThan(0.5f),
                "Eased OUT: a projectile leaves fast and is already travelling, so the " +
                "acceleration an ease-in would draw is the one thing it does not do.");

            var previous = -1f;
            for (var step = 0f; step <= 1.01f; step += 0.05f)
            {
                var progress = RangedSmackProfile.TracerProgress(flight * step, separation);
                Assert.That(progress, Is.GreaterThanOrEqualTo(previous));
                Assert.That(progress, Is.InRange(0f, 1f));
                previous = progress;
            }
        }

        [Test]
        public void TheImpact_LandsWhenTheTracerDoesAndThenFades()
        {
            const float separation = Hex * 6f;
            var flight = RangedSmackProfile.TracerSeconds(separation);

            Assert.That(RangedSmackProfile.HasImpacted(flight * 0.99f, separation), Is.False,
                "Nothing has arrived yet, so nothing has been hit yet.");
            Assert.That(RangedSmackProfile.HasImpacted(flight, separation), Is.True);
            Assert.That(RangedSmackProfile.ImpactStrength(flight * 0.5f, separation),
                Is.EqualTo(0f).Within(Tolerance));
            Assert.That(RangedSmackProfile.ImpactStrength(flight, separation),
                Is.EqualTo(1f).Within(Tolerance));
            Assert.That(
                RangedSmackProfile.ImpactStrength(
                    flight + RangedSmackProfile.ImpactSeconds, separation),
                Is.EqualTo(0f).Within(Tolerance),
                "The mark fades rather than staying on the board describing an old event.");
        }

        [Test]
        public void TheWholeRangedSmack_StaysShortAtEverySeparationTheBoardCanProduce()
        {
            for (var hexes = 2f; hexes <= 20f; hexes += 1f)
            {
                var separation = hexes * Hex;
                var flight = RangedSmackProfile.TracerSeconds(separation);

                Assert.That(flight, Is.InRange(
                    RangedSmackProfile.MinTracerSeconds, RangedSmackProfile.MaxTracerSeconds),
                    "A two-hex shot must not be a single frame, and the longest must not become a " +
                    "wait - the spotlight is never blocked on a projectile.");
                Assert.That(RangedSmackProfile.TotalSeconds(separation), Is.LessThan(0.6f));
            }

            Assert.That(RangedSmackProfile.TracerLengthFraction, Is.LessThan(0.5f),
                "A dash, not a beam: a line joining the two pieces reads as a wall between them, " +
                "which is the opposite of something travelling.");
        }

        // ---- Where "ranged" actually lives.

        /// <summary>
        /// The recorded finding, asserted against the real card the coordinator named. It is NOT on
        /// the match-log row; it is on `ActionDefinition.AttackRange`, reachable from the card the
        /// Attack row already names, so every client derives it from public data with nothing added
        /// to the wire.
        /// </summary>
        [Test]
        public void TheRangedFact_IsOnTheActionDefinitionAndReachableFromTheCardTheRowNames()
        {
            var shootout = SabinaCardContent.AllDefinitions
                .Single(definition => definition.Id.Equals(SabinaCardContent.Shootout));

            Assert.That(
                shootout.ActionDefinitions.Any(
                    action => action.AttackRange == AttackRangeKind.Ranged),
                Is.True,
                "Shootout prints a Ranged Attack, which is what makes it the case this pass exists " +
                "for. If this ever fails, the content moved and the spotlight's reading moved with it.");

            Assert.That(
                shootout.ActionDefinitions
                    .Where(action => action.AttackRange == AttackRangeKind.Ranged)
                    .All(action => action.BaseValues.Any(
                        value => value.Channel == PrintedValueChannel.Range && value.Amount > 0)),
                Is.True,
                "A Ranged Attack prints a positive Range, which is the bound the framing's " +
                "MaximumFramedSeparation was picked against.");
        }

        [Test]
        public void TheFramedMaximum_ClearsTheLargestPrintedRangeInTheCorpus()
        {
            var largestPrintedRange = SabinaCardContent.AllDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .Where(action => action.AttackRange == AttackRangeKind.Ranged)
                .SelectMany(action => action.BaseValues)
                .Where(value => value.Channel == PrintedValueChannel.Range)
                .Select(value => value.Amount)
                .DefaultIfEmpty(0)
                .Max();

            Assert.That(largestPrintedRange, Is.GreaterThan(0));
            Assert.That(
                AttackSpotlightTiming.MaximumFramedSeparation,
                Is.GreaterThanOrEqualTo(largestPrintedRange * Hex * 2f),
                "The framed maximum is the largest printed Range with its dynamic headroom DOUBLED. " +
                "If a hero ever prints a longer one, this fails rather than silently cropping the " +
                "shot on a legal attack.");
        }
    }
}
