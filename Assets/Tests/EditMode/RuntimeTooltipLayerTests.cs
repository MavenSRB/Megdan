using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z19 (placeholder id). Covers <see cref="RuntimeTooltipHoverPlan"/>'s
    /// pure hover-timing state machine, <see cref="RuntimeTooltipGeometry"/>'s clamped placement math,
    /// and <see cref="RuntimeTooltipCandidateLookup"/>'s ancestor walk - none of which need a live
    /// panel. The actual on-screen render of <see cref="RuntimeTooltipLayer"/> in a packaged Player
    /// build is Unity-owed; see the milestone report.
    /// </summary>
    public sealed class RuntimeTooltipLayerTests
    {
        private const string KeyA = "candidate-a";
        private const string KeyB = "candidate-b";

        [Test]
        public void ANewCandidate_GoesPendingAndAsksForTheShowDelay()
        {
            var plan = new RuntimeTooltipHoverPlan();

            var command = plan.SetCandidate(KeyA, "Hello", new Vector2(10f, 20f));

            Assert.That(command, Is.EqualTo(RuntimeTooltipCommand.StartDelay));
            Assert.That(plan.Visibility, Is.EqualTo(RuntimeTooltipVisibility.Pending));
        }

        [Test]
        public void ContinuingToHoverTheSamePendingCandidate_AsksForNothing_TheDelayIsNeverRestarted()
        {
            var plan = new RuntimeTooltipHoverPlan();
            plan.SetCandidate(KeyA, "Hello", new Vector2(10f, 20f));

            var command = plan.SetCandidate(KeyA, "Hello", new Vector2(11f, 21f));

            Assert.That(command, Is.EqualTo(RuntimeTooltipCommand.None),
                "Still the same candidate, still pending - not a new hover.");
            Assert.That(plan.Visibility, Is.EqualTo(RuntimeTooltipVisibility.Pending));
        }

        [Test]
        public void WhenTheDelayElapsesForTheStillCurrentCandidate_ItShows()
        {
            var plan = new RuntimeTooltipHoverPlan();
            plan.SetCandidate(KeyA, "Hello", new Vector2(10f, 20f));

            var elapsed = plan.TryElapseDelay(KeyA);

            Assert.That(elapsed, Is.True);
            Assert.That(plan.Visibility, Is.EqualTo(RuntimeTooltipVisibility.Shown));
            Assert.That(plan.CurrentText, Is.EqualTo("Hello"));
        }

        [Test]
        public void AStaleDelayFiringAfterTheCandidateMovedOn_IsAHarmlessNoOp()
        {
            var plan = new RuntimeTooltipHoverPlan();
            plan.SetCandidate(KeyA, "Hello", new Vector2(10f, 20f));
            // The pointer moved to a different candidate before A's scheduled delay fired.
            plan.SetCandidate(KeyB, "World", new Vector2(50f, 60f));

            var elapsed = plan.TryElapseDelay(KeyA);

            Assert.That(elapsed, Is.False, "A's timer is stale - B is current now.");
            Assert.That(plan.Visibility, Is.EqualTo(RuntimeTooltipVisibility.Pending),
                "B's own delay has not elapsed yet.");
            Assert.That(plan.CurrentText, Is.EqualTo("World"));
        }

        [Test]
        public void WhileShown_TheSameCandidateContinuingToMove_RepositionsWithoutRestartingAnything()
        {
            var plan = new RuntimeTooltipHoverPlan();
            plan.SetCandidate(KeyA, "Hello", new Vector2(10f, 20f));
            plan.TryElapseDelay(KeyA);

            var command = plan.SetCandidate(KeyA, "Hello", new Vector2(30f, 40f));

            Assert.That(command, Is.EqualTo(RuntimeTooltipCommand.Reposition));
            Assert.That(plan.Visibility, Is.EqualTo(RuntimeTooltipVisibility.Shown));
            Assert.That(plan.CurrentPosition, Is.EqualTo(new Vector2(30f, 40f)));
        }

        [Test]
        public void WhileShown_MovingToADifferentCandidate_GoesBackToPendingForTheNewOne()
        {
            var plan = new RuntimeTooltipHoverPlan();
            plan.SetCandidate(KeyA, "Hello", new Vector2(10f, 20f));
            plan.TryElapseDelay(KeyA);

            var command = plan.SetCandidate(KeyB, "World", new Vector2(30f, 40f));

            Assert.That(command, Is.EqualTo(RuntimeTooltipCommand.StartDelay),
                "The view must hide A immediately and start B's own delay - it never jumps " +
                "straight from A's popup to B's.");
            Assert.That(plan.Visibility, Is.EqualTo(RuntimeTooltipVisibility.Pending));
            Assert.That(plan.CurrentText, Is.EqualTo("World"));
        }

        [TestCase(null, "Hello")]
        [TestCase(KeyA, null)]
        [TestCase(KeyA, "")]
        public void HoveringNothingThisLayerSpeaksFor_HidesWhateverWasShowing(
            string candidateKey, string candidateText)
        {
            var plan = new RuntimeTooltipHoverPlan();
            plan.SetCandidate(KeyA, "Hello", new Vector2(10f, 20f));
            plan.TryElapseDelay(KeyA);

            var command = plan.SetCandidate(candidateKey, candidateText, new Vector2(0f, 0f));

            Assert.That(command, Is.EqualTo(RuntimeTooltipCommand.Hide));
            Assert.That(plan.Visibility, Is.EqualTo(RuntimeTooltipVisibility.Hidden));
        }

        [Test]
        public void HidingAnAlreadyHiddenPlan_IsANoOp()
        {
            var plan = new RuntimeTooltipHoverPlan();

            var command = plan.Hide();

            Assert.That(command, Is.EqualTo(RuntimeTooltipCommand.None));
        }

        [Test]
        public void AnExplicitHide_ClearsThePendingOrShownCandidate_AndReturningToItStartsFreshDelay()
        {
            var plan = new RuntimeTooltipHoverPlan();
            plan.SetCandidate(KeyA, "Hello", new Vector2(10f, 20f));
            plan.TryElapseDelay(KeyA);

            var hideCommand = plan.Hide();
            Assert.That(hideCommand, Is.EqualTo(RuntimeTooltipCommand.Hide));

            // Hovering straight back onto the same element after a press/leave owes a fresh delay,
            // not an instant re-show - nothing is remembered across a hide.
            var again = plan.SetCandidate(KeyA, "Hello", new Vector2(10f, 20f));
            Assert.That(again, Is.EqualTo(RuntimeTooltipCommand.StartDelay));
            Assert.That(plan.Visibility, Is.EqualTo(RuntimeTooltipVisibility.Pending));
        }

        // -- Placement math -------------------------------------------------------------------

        [Test]
        public void AnchorNearCursor_OffsetsDownAndRightOfThePointer_WhenThereIsRoom()
        {
            var pointer = new Vector2(200f, 200f);
            var popupSize = new Vector2(100f, 40f);
            var viewport = new Vector2(2560f, 1440f);

            var anchored = RuntimeTooltipGeometry.AnchorNearCursor(pointer, popupSize, viewport);

            Assert.That(anchored, Is.EqualTo(pointer + RuntimeTooltipLayer.CursorOffset));
        }

        [Test]
        public void Clamp_NeverLetsThePopupCrossTheRightOrBottomEdge()
        {
            var viewport = new Vector2(2560f, 1440f);
            var popupSize = new Vector2(300f, 100f);
            var desired = new Vector2(2550f, 1430f); // far past both edges once the popup's size is added

            var clamped = RuntimeTooltipGeometry.Clamp(desired, popupSize, viewport);

            Assert.That(clamped.x + popupSize.x, Is.LessThanOrEqualTo(viewport.x));
            Assert.That(clamped.y + popupSize.y, Is.LessThanOrEqualTo(viewport.y));
            Assert.That(clamped, Is.EqualTo(new Vector2(viewport.x - popupSize.x, viewport.y - popupSize.y)));
        }

        [Test]
        public void Clamp_NeverLetsThePopupCrossTheLeftOrTopEdge()
        {
            var viewport = new Vector2(2560f, 1440f);
            var popupSize = new Vector2(300f, 100f);
            var desired = new Vector2(-40f, -25f);

            var clamped = RuntimeTooltipGeometry.Clamp(desired, popupSize, viewport);

            Assert.That(clamped, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void Clamp_APopupLargerThanTheViewport_SettlesAtTheOriginRatherThanGoingNegative()
        {
            var viewport = new Vector2(200f, 100f);
            var popupSize = new Vector2(500f, 400f);

            var clamped = RuntimeTooltipGeometry.Clamp(new Vector2(50f, 50f), popupSize, viewport);

            Assert.That(clamped, Is.EqualTo(Vector2.zero),
                "A popup bigger than the viewport clamps to the near edge rather than centring " +
                "itself off-screen on both sides.");
        }

        // -- Candidate lookup, including the disabled-element case ----------------------------

        [Test]
        public void Find_ReturnsTheHoveredElementItself_WhenItCarriesTheTooltip()
        {
            var element = new VisualElement { tooltip = "Explains itself" };

            var found = RuntimeTooltipCandidateLookup.Find(element);

            Assert.That(found, Is.SameAs(element));
        }

        [Test]
        public void Find_WalksUpToTheNearestAncestorThatCarriesATooltip()
        {
            var parent = new VisualElement { tooltip = "Owner's own reading" };
            var child = new VisualElement(); // no tooltip of its own - a leaf glyph inside a button
            parent.Add(child);

            var found = RuntimeTooltipCandidateLookup.Find(child);

            Assert.That(found, Is.SameAs(parent));
        }

        [Test]
        public void Find_ReturnsNull_WhenNothingInTheChainCarriesATooltip()
        {
            var parent = new VisualElement();
            var child = new VisualElement();
            parent.Add(child);

            Assert.That(RuntimeTooltipCandidateLookup.Find(child), Is.Null);
            Assert.That(RuntimeTooltipCandidateLookup.Find(null), Is.Null);
        }

        [Test]
        public void Find_StopsAtAnExplicitTooltipBarrier_InsteadOfFallingThroughToAParent()
        {
            var parent = new VisualElement { tooltip = "Redundant parent reading" };
            var actionHotspot = new VisualElement();
            actionHotspot.AddToClassList(RuntimeTooltipCandidateLookup.SuppressTooltipClass);
            parent.Add(actionHotspot);

            Assert.That(RuntimeTooltipCandidateLookup.Find(actionHotspot), Is.Null,
                "A printed action whose words are already visible must not reveal its card's " +
                "fallback tooltip through the invisible hotspot.");
        }

        /// <summary>
        /// THE STRUCTURAL WRINKLE the milestone brief calls out by name: a blocked reason lives on a
        /// control <c>HudElements.MarkUnavailable</c> has already called <c>SetEnabled(false)</c> on.
        /// The lookup reads the plain property directly rather than through event dispatch, so a
        /// disabled control's tooltip stays reachable - proven here with no live panel at all.
        /// </summary>
        [Test]
        public void Find_StillReportsTheTooltip_OnAControlThatHasBeenDisabled()
        {
            var control = new Button { tooltip = ActionUnavailabilityLabels.Generic };
            control.SetEnabled(false);

            Assert.That(control.enabledSelf, Is.False, "Fixture precondition.");

            var found = RuntimeTooltipCandidateLookup.Find(control);

            Assert.That(found, Is.SameAs(control));
            Assert.That(found.tooltip, Is.EqualTo(ActionUnavailabilityLabels.Generic));
        }

        [Test]
        public void Find_StillWalksThroughADisabledAncestorToItsTooltip()
        {
            var disabledParent = new VisualElement { tooltip = "BLOCKED BY SOMEONE — SOME CARD" };
            disabledParent.SetEnabled(false);
            var child = new VisualElement();
            disabledParent.Add(child);

            var found = RuntimeTooltipCandidateLookup.Find(child);

            Assert.That(found, Is.SameAs(disabledParent));
        }
    }
}
