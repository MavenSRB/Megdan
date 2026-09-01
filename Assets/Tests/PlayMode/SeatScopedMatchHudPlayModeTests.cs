using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.30b. The seat-scoped HUD as a player meets it, and the two surfaces the
    /// user-approved amendment moved.
    /// </summary>
    /// <remarks>
    /// <para>The Edit Mode fixture proves the presenters scope to one seat and that the two amended
    /// elements carry what the projection derived. This is the half a fixture with no panel cannot
    /// see: where the elements are actually drawn on a live scaled panel, that a player pressing the
    /// corner control advances the authority through the seat's own client, and that composing a
    /// second seat leaves exactly one of each behind.</para>
    /// <para>Every rectangle below is compared against the renderer's own stated rectangle rather
    /// than against a literal, so the two cannot drift; the literals live in
    /// <c>MatchHudPanelScalingTests</c>, where the amendment recorded its measurements.</para>
    /// </remarks>
    public sealed class SeatScopedMatchHudPlayModeTests
    {
        /// <summary>
        /// The amendment's first half: the contextual narration is in the top status bar, and it is
        /// there once.
        /// </summary>
        [UnityTest]
        public IEnumerator SeatScene_DrawsTheContextualNarrationInTheTopStatusBarAndNowhereElse()
        {
            UiPlayer.EnsurePlayerViewport();
            yield return LoadSeatScene(0);
            var root = Root();
            root.Runtime.RefreshAll();
            yield return null;
            yield return null;

            var topBar = Named(root.Hud.RootElement, "gd-topbar");
            var narration = Named(root.Hud.RootElement, "gd-narration");
            var lane = Named(root.Hud.RootElement, "gd-narration-lane");
            var context = Named(root.Hud.RootElement, "gd-context");

            Assert.That(narration, Is.Not.Null, "The HUD draws a narration element.");
            Assert.That(narration.parent, Is.SameAs(lane));
            Assert.That(lane.parent, Is.SameAs(topBar),
                "The lane is the status bar own second row, not a floating panel over it.");
            Assert.That(UiPlayer.Descendants(context).Contains(narration), Is.False,
                "It left the contextual region beside the cards rather than being duplicated out of it.");
            Assert.That(
                UiPlayer.Descendants(root.Hud.RootElement)
                    .Count(element => element.name == "gd-narration"),
                Is.EqualTo(1),
                "One narration, in one place. Two would be two answers to what the match is doing.");

            var status = root.Runtime.RoundView.Model.Status;
            Assert.That(status, Is.Not.Empty,
                "There is no step at which the derivation has nothing to report.");
            Assert.That(
                UiPlayer.Descendants(topBar).OfType<Label>()
                    .Where(label => UiPlayer.IsShown(label))
                    .Select(label => label.text),
                Does.Contain(status),
                "and the bar is showing exactly the sentence the seat own presenter derived. " +
                UiPlayer.Describe(root.Hud));

            // The strip is the identity row plus the lane. Milestone 8.30l centres the Tie Breaker
            // across that complete height as a round badge while End Turn takes its former right slot.
            var snap = UiPlayer.PixelSnapping();
            Assert.That(topBar.worldBound.height,
                Is.EqualTo(ConfiguredMatchHudRenderer.StatusBarHeight).Within(snap));
            Assert.That(Named(root.Hud.RootElement, "gd-topbar-identity").worldBound.height,
                Is.EqualTo(ConfiguredMatchHudRenderer.StatusBarIdentityRowHeight).Within(snap));
            var chipElement = Named(root.Hud.RootElement, "gd-tie-breaker");
            var chip = chipElement.worldBound;
            Assert.That(chip.yMin, Is.EqualTo(root.Hud.TieBreakerIndicatorRect.yMin).Within(snap));
            Assert.That(chip.height,
                Is.EqualTo(root.Hud.TieBreakerIndicatorRect.height).Within(snap));
            Assert.That(chip.width, Is.EqualTo(chip.height).Within(snap),
                "The Tie Breaker is a circle, not the retired wide chip.");
            Assert.That(chip.center.x,
                Is.EqualTo(topBar.worldBound.center.x).Within(snap),
                "The round Tie Breaker badge is centred in the status bar.");
            var badgeLabel = UiPlayer.Descendants(chipElement).OfType<Label>().Single();
            Assert.That(badgeLabel.text, Is.EqualTo("TIEBREAKER"));
            Assert.That(badgeLabel.resolvedStyle.whiteSpace, Is.EqualTo(WhiteSpace.NoWrap),
                "The requested word remains one line inside the round badge.");

            // The narration reads across the bar rather than being clipped into the corner it left.
            Assert.That(narration.worldBound.width,
                Is.GreaterThan(ConfiguredMatchHudRenderer.StatusDockWidth),
                "The lane is wider than the dock the sentence used to be drawn in.");
            Assert.That(narration.worldBound.yMax,
                Is.LessThanOrEqualTo(topBar.worldBound.yMax + snap),
                "and it is inside the strip that carries it.");
        }

        /// <summary>
        /// Milestone 8.30f: selecting a card keeps the existing click-then-confirm path, but the one
        /// confirmation banner now stands below DIRECTOR at both supported desktop viewports and a
        /// tall/narrow live window, and submits exactly once through the seat client.
        /// </summary>
        [UnityTest]
        public IEnumerator SeatScene_ConfirmStandsBelowDirectorAtBothDesktopViewportsAndCommitsOnce()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            UiPlayer.SetPlayerViewport(2560, 1440, "8.30f confirm reference viewport");
            yield return LoadSeatScene(0);
            var root = Root();
            var runtime = root.Runtime;
            var selectable = runtime.HandsView.Model.ViewedHand.Cards.First();
            Assert.That(runtime.SelectCommitmentCard(selectable.Id), Is.True);

            foreach (var viewport in new[]
                     {
                         (Width: 2560u, Height: 1440u, Label: "reference"),
                         (Width: 2048u, Height: 1152u, Label: "validated desktop"),
                         (Width: 756u, Height: 1247u, Label: "tall narrow")
                     })
            {
                UiPlayer.SetPlayerViewport(
                    viewport.Width, viewport.Height, "8.30f confirm " + viewport.Label);
                runtime.RefreshAll();
                yield return null;
                yield return null;
                var director = Named(root.Hud.RootElement, "gd-director-toggle").worldBound;
                var confirm = Named(root.Hud.RootElement, "gd-confirm-control");
                var drawn = confirm.worldBound;
                var stated = root.Hud.ConfirmFacedownControlRect;
                var snap = UiPlayer.PixelSnapping();
                TestContext.WriteLine(
                    $"8.30f {viewport.Label}: panel={ConfiguredMatchHudRenderer.PanelViewport}, " +
                    $"director={director}, confirm={drawn}, host={runtime.HandsView.ConfirmActionRoot.worldBound}, " +
                    $"stated={stated}, hostStyleTop={runtime.HandsView.ConfirmActionRoot.style.top.value.value}");
                Assert.That(UiPlayer.IsShown(confirm), Is.True);
                Assert.That(drawn.xMin, Is.EqualTo(stated.xMin).Within(snap));
                Assert.That(drawn.yMin, Is.EqualTo(stated.yMin).Within(snap));
                Assert.That(drawn.width, Is.EqualTo(stated.width).Within(snap * 2f));
                Assert.That(drawn.yMin - director.yMax,
                    Is.EqualTo(ConfiguredMatchHudRenderer.ConfirmDirectorGap).Within(snap * 2f),
                    "The banner has its approved breathing room below DIRECTOR.");
                Assert.That(UiPlayer.Descendants(root.Hud.RootElement)
                    .Count(element => element.name == "gd-confirm-control"), Is.EqualTo(1));
                Assert.That(drawn.Overlaps(root.Hud.EndTurnControlRect), Is.False,
                    "Commitment confirmation no longer occupies End Turn's corner.");
            }

            var before = runtime.Host.Version;
            UiPlayer.Click(Named(root.Hud.RootElement, "gd-confirm-control"));
            yield return null;
            yield return null;
            Assert.That(runtime.Host.Version, Is.EqualTo(before + 1));
            Assert.That(runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId, Is.Null);
            Assert.That(UiPlayer.Descendants(root.Hud.RootElement)
                .Any(element => element.name == "gd-confirm-control" && UiPlayer.IsShown(element)),
                Is.False,
                "One successful press removes the confirmation surface and cannot double-submit.");

            UiPlayer.SetPlayerViewport(
                restoreWidth, restoreHeight, "8.30f restore prior viewport");
            yield return null;
        }

        /// <summary>
        /// Milestone 8.30g's successful route at both supported desktop resolutions. The events are
        /// genuine UI Toolkit pointer down/move/up events, the release lands in the board camera's
        /// live safe surface, and the exact full printed face leaves its hand slot and follows the
        /// pointer before committing through the seat client without ever drawing the click path's
        /// confirmation control.
        /// </summary>
        [UnityTest]
        public IEnumerator DraggingAnEligibleHandCardOntoTheMap_CommitsItOnceWithoutConfirmation()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            foreach (var viewport in new[]
                     {
                         (Width: 2560u, Height: 1440u, Label: "reference"),
                         (Width: 2048u, Height: 1152u, Label: "validated desktop")
                     })
            {
                UiPlayer.SetPlayerViewport(
                    viewport.Width, viewport.Height, "8.30g drag " + viewport.Label);
                yield return LoadSeatScene(0);
                var root = Root();
                var runtime = root.Runtime;
                runtime.RefreshAll();
                yield return null;
                yield return null;
                var model = runtime.HandsView.Model.ViewedHand.TrayCards.Last(card => card.IsSelectable);
                var card = CardControl(root.Hud.RootElement, model);
                var drop = root.Hud.CameraSafeLayout.UpperRegion.center;
                Assert.That(root.Hud.IsMapDropPoint(drop), Is.True);
                var before = runtime.Host.Version;

                var target = BeginDrag(card, out _);
                SendPointerMove(target, drop);
                var dragProxy = Named(root.Hud.RootElement, "gd-card-drag-proxy");
                Assert.That(dragProxy.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(dragProxy.Q<VisualElement>("gd-card-preview"), Is.Not.Null,
                    "Dragging must use the shared complete-card face, not a name-only abstraction.");
                Assert.That(card.parent.resolvedStyle.visibility, Is.EqualTo(Visibility.Hidden),
                    "The picked-up card must disappear from its hand slot while the drag stands.");
                var sourceArtwork = card.Q<Image>("gd-card-artwork")?.image;
                if (sourceArtwork != null)
                {
                    Assert.That(dragProxy.Q<Image>("gd-card-artwork")?.image, Is.SameAs(sourceArtwork),
                        "A prepared front must carry the exact same card picture under the pointer.");
                }
                Assert.That(UiPlayer.Descendants(root.Hud.RootElement)
                    .Any(element => element.name == "gd-confirm-control" && UiPlayer.IsShown(element)),
                    Is.False, "The drag path never selects first and never flashes confirmation.");

                if (!UnityEngine.Application.isBatchMode)
                {
                    yield return Capture(
                        "tmp/milestone-8-30g-card-drag-" + viewport.Width + "x" +
                        viewport.Height + ".png");
                }

                SendPointerUp(target, drop);
                yield return null;
                yield return null;

                Assert.That(runtime.Host.Version, Is.EqualTo(before + 1));
                Assert.That(runtime.GetCommitmentStatus(runtime.Seat.HeroId),
                    Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));
                Assert.That(runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId, Is.Null);
                Assert.That(Named(root.Hud.RootElement, "gd-card-drag-proxy")
                    .resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                Assert.That(UiPlayer.Descendants(root.Hud.RootElement)
                    .Any(element => element.name == "gd-confirm-control" && UiPlayer.IsShown(element)),
                    Is.False);

                // The refreshed projection still draws the remaining cards, but none is eligible for
                // another commitment. A complete second pointer sequence therefore cannot resubmit.
                var remaining = UiPlayer.Descendants(root.Hud.RootElement)
                    .First(element => element.name == "gd-card-choice");
                SendDrag(remaining, drop);
                Assert.That(runtime.Host.Version, Is.EqualTo(before + 1));
            }

            UiPlayer.SetPlayerViewport(
                restoreWidth, restoreHeight, "8.30g restore prior viewport");
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnavailableCardGroup_RetainsDimmingAndCannotCommitAcrossRefreshAndSessionReplacementAtBothDesktopViewports()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            foreach (var viewport in new[]
                     {
                         (Width: 2560u, Height: 1440u, Label: "reference"),
                         (Width: 2048u, Height: 1152u, Label: "validated desktop")
                     })
            {
                UiPlayer.SetPlayerViewport(
                    viewport.Width, viewport.Height, "8.30l unavailable " + viewport.Label);
                yield return LoadSeatScene(0);
                var root = Root();
                var runtime = root.Runtime;
                runtime.RefreshAll();
                yield return null;
                yield return null;
                var safeLayoutBeforeUnavailableGroup = root.Hud.CameraSafeLayout;
                var chosen = runtime.HandsView.Model.ViewedHand.TrayCards.First(card =>
                    card.IsSelectable);
                UiPlayer.Click(CardControl(root.Hud.RootElement, chosen));
                yield return null;
                Assert.That(runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId,
                    Is.EqualTo(chosen.CardInstanceId));
                var before = runtime.Host.Version;
                UiPlayer.Click(Named(root.Hud.RootElement, "gd-confirm-control"));
                yield return null;
                yield return null;

                Assert.That(runtime.Host.Version, Is.EqualTo(before + 1));
                var hand = runtime.HandsView.Model.ViewedHand;
                Assert.That(hand.PlayableTrayCards, Is.Empty,
                    "After this seat commits, none of its cards carries another commitment command.");
                Assert.That(hand.UnavailableTrayCards, Has.Count.EqualTo(hand.TrayCards.Count));
                var playableHost = Named(root.Hud.RootElement, "gd-card-buttons");
                var unavailableHost = Named(root.Hud.RootElement, "gd-unavailable-card-buttons");
                Assert.That(UiPlayer.Descendants(playableHost)
                    .Any(element => element.name == "gd-card-choice"), Is.False);
                var controls = UiPlayer.Descendants(unavailableHost)
                    .Where(element => element.name == "gd-card-choice")
                    .OfType<Button>()
                    .ToArray();
                Assert.That(controls, Has.Length.EqualTo(hand.UnavailableTrayCards.Count));
                Assert.That(controls, Has.All.Matches<Button>(control =>
                    control.enabledSelf && control.focusable));
                Assert.That(controls.All(control => UiPlayer.Descendants(control).Count(element =>
                    element.name == "gd-card-unavailable-veil") == 1), Is.True);

                var group = Named(root.Hud.RootElement, "gd-unavailable-card-group");
                var stated = root.Hud.UnavailableCardGroupRect;
                var snap = UiPlayer.PixelSnapping();
                Assert.That(group.worldBound.xMin, Is.EqualTo(stated.xMin).Within(snap));
                Assert.That(group.worldBound.xMax, Is.EqualTo(stated.xMax).Within(snap));
                Assert.That(group.worldBound.Overlaps(root.Hud.RedHeroRailRect), Is.False);
                var liveRails = UiPlayer.Descendants(root.Hud.RootElement)
                    .Where(element => element.name == "gd-hero-rail" && UiPlayer.IsShown(element))
                    .ToArray();
                var overlappingRailBottom = liveRails
                    .Where(rail => rail.worldBound.xMax > group.worldBound.xMin)
                    .Max(rail => rail.worldBound.yMax);
                Assert.That(group.worldBound.yMin,
                    Is.GreaterThanOrEqualTo(overlappingRailBottom),
                    $"The vertical fan begins below the actual right-side rail stack; " +
                    $"group={group.worldBound}, railBottom={overlappingRailBottom}.");
                Assert.That(group.worldBound.Overlaps(root.Hud.ConfirmFacedownControlRect), Is.False);
                Assert.That(root.Hud.CameraSafeLayout.UpperRegion,
                    Is.EqualTo(safeLayoutBeforeUnavailableGroup.UpperRegion),
                    "Moving cards between the two hosts must not move or reframe the map.");
                Assert.That(root.Hud.CameraSafeLayout.LowerRightRegion,
                    Is.EqualTo(safeLayoutBeforeUnavailableGroup.LowerRightRegion));
                var endTurn = UiPlayer.Descendants(root.Hud.RootElement)
                    .SingleOrDefault(element => element.name == "gd-end-turn-control");
                if (endTurn != null && UiPlayer.IsShown(endTurn))
                {
                    Assert.That(group.worldBound.Overlaps(endTurn.worldBound), Is.False,
                        "The supported five-card fan leaves the action control clear.");
                }

                var unavailable = controls[controls.Length - 1];
                UiPlayer.PointAt(unavailable);
                yield return null;
                var preview = Named(root.Hud.RootElement, "gd-hand-hover-preview");
                Assert.That(preview.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(UiPlayer.Descendants(preview).Count(element =>
                    element.name == "gd-card-unavailable-veil"), Is.EqualTo(1),
                    "Inspecting the whole face retains the unavailable dimming.");
                UiPlayer.PointAway(unavailable);
                yield return null;

                UiPlayer.Click(unavailable);
                yield return null;
                Assert.That(runtime.Host.Version, Is.EqualTo(before + 1));
                Assert.That(runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId, Is.Null);
                SendDrag(unavailable, root.Hud.CameraSafeLayout.UpperRegion.center);
                yield return null;
                Assert.That(runtime.Host.Version, Is.EqualTo(before + 1));
                Assert.That(Named(root.Hud.RootElement, "gd-card-drag-proxy")
                    .resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

                if (!UnityEngine.Application.isBatchMode)
                {
                    yield return Capture(
                        "tmp/milestone-8-30l-unavailable-" + viewport.Width + "x" +
                        viewport.Height + ".png");
                }

                var seat = runtime.Seat.Id;
                yield return ComposeForSeat(seat);
                root = Root();
                runtime = root.Runtime;
                runtime.RefreshAll();
                yield return null;
                yield return null;
                Assert.That(runtime.Host.Version, Is.Zero);
                Assert.That(Named(root.Hud.RootElement, "gd-unavailable-card-group")
                    .resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
                Assert.That(UiPlayer.Descendants(root.Hud.RootElement)
                    .Count(element => element.name == "gd-card-choice"),
                    Is.EqualTo(runtime.HandsView.Model.ViewedHand.TrayCards.Count),
                    "Session replacement rebuilds one complete surface per visible owned card.");
            }

            UiPlayer.SetPlayerViewport(
                restoreWidth, restoreHeight, "8.30l restore prior viewport");
            yield return null;
        }

        [UnityTest]
        public IEnumerator DraggingADefenseCardOntoTheMap_SubmitsThatExactProjectedOfferOnce()
        {
            UiPlayer.EnsurePlayerViewport();
            yield return LoadSeatScene(0);
            var root = Root();
            var runtime = root.Runtime;
            var model = runtime.RoundView.Model;
            var card = runtime.HandsView.Model.ViewedHand.TrayCards.First();
            var offer = new TypedInvocationViewModel(
                "DISCARD " + card.DisplayName.ToUpperInvariant(),
                DirectorCommand.ChooseDefenseCard(card.CardInstanceId),
                null,
                card,
                CardShineSlot.SecondaryDefense);
            var decline = new TypedInvocationViewModel(
                "DECLINE DEFENSE", DirectorCommand.DeclineDefense());
            TypedInvocationViewModel submitted = null;
            var submissionCount = 0;
            runtime.RoundView.InvocationSubmitted += invocation =>
            {
                if (object.ReferenceEquals(invocation, offer))
                {
                    submitted = invocation;
                    submissionCount++;
                }
            };
            runtime.RoundView.Render(new RoundActionViewModel(
                model.Version,
                model.RoundIndex,
                model.TurnIndex,
                model.Phase,
                model.Status,
                model.Actions,
                model.EndTurnInvocation,
                model.ContinueEndOfRoundInvocation,
                model.InitiativeChoices,
                new[] { offer, decline }));
            yield return null;

            var control = Named(runtime.RoundView.Root, "gd-defense-card");
            var slot = control.parent;
            var declineControl = Named(root.Hud.RootElement, "gd-decline-defense-control");
            var declineCenter = declineControl.worldBound.center;
            var expectedDeclineCenter = root.Hud.ConfirmFacedownControlRect.center;
            var snap = UiPlayer.PixelSnapping();
            Assert.That(declineCenter.x, Is.EqualTo(expectedDeclineCenter.x).Within(snap));
            Assert.That(declineCenter.y, Is.EqualTo(expectedDeclineCenter.y).Within(snap));
            var director = Named(root.Hud.RootElement, "gd-director-toggle").worldBound;
            Assert.That(declineControl.worldBound.yMin - director.yMax,
                Is.EqualTo(ConfiguredMatchHudRenderer.ConfirmDirectorGap).Within(snap * 2f),
                "The Defense decline occupies the same immediately-below-Director lane as Confirm.");
            // The runner's hardware pointer can already be resting over the freshly composed card.
            // Normalize that external hover before the explicit pointer-enter journey below.
            UiPlayer.PointAway(control);
            using (ConfiguredCardShine.UseTextureLoader(_ => Texture2D.whiteTexture))
            {
                UiPlayer.PointAt(control);
            }
            yield return null;
            var hoverPreview = Named(root.Hud.RootElement, "gd-defense-card-hover-preview");
            Assert.That(hoverPreview.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(hoverPreview.Q<VisualElement>("gd-card-preview"), Is.Not.Null,
                "An offered Defense card raises the ordinary hand's complete-face peek.");
            var hoverShine = UiPlayer.Descendants(hoverPreview)
                .OfType<Image>()
                .SingleOrDefault(element => element.name == "gd-card-shine");
            Assert.That(hoverShine, Is.Not.Null,
                "The hover replacement must retain the offered Defense shield shine.");
            Assert.That(hoverShine.userData, Is.EqualTo(CardShineSlot.SecondaryDefense));
            Assert.That(slot.resolvedStyle.opacity, Is.Zero.Within(0.001f),
                "The peek replaces its overlapped source slot exactly as the ordinary hand does.");
            var drop = root.Hud.CameraSafeLayout.UpperRegion.center;
            var target = BeginDrag(control, out _);
            SendPointerMove(target, drop);
            var proxy = Named(root.Hud.RootElement, "gd-defense-card-drag-proxy");
            Assert.That(proxy.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(proxy.Q<VisualElement>("gd-card-preview"), Is.Not.Null,
                "Defense uses the same complete-card drag reading as facedown commitment.");
            Assert.That(slot.resolvedStyle.visibility, Is.EqualTo(Visibility.Hidden));

            SendPointerUp(target, drop);
            yield return null;

            Assert.That(submitted, Is.SameAs(offer));
            Assert.That(submissionCount, Is.EqualTo(1));
            Assert.That(proxy.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        /// <summary>
        /// Milestone 8.30g's cancellation and compatibility route: an off-map drag submits nothing,
        /// a below-threshold move remains the existing click-and-confirm path, and replacing the
        /// session while a pointer is captured leaves no proxy, selection, callback, or submission.
        /// </summary>
        [UnityTest]
        public IEnumerator ClickingOrCancellingACardDrag_PreservesTheExistingConfirmationFlow()
        {
            UiPlayer.EnsurePlayerViewport();
            yield return LoadSeatScene(0);
            var root = Root();
            var runtime = root.Runtime;
            yield return null;
            yield return null;

            var model = runtime.HandsView.Model.ViewedHand.TrayCards.Last(card => card.IsSelectable);
            var card = CardControl(root.Hud.RootElement, model);
            var hudChrome = Named(root.Hud.RootElement, "gd-topbar").worldBound.center;
            Assert.That(root.Hud.IsMapDropPoint(hudChrome), Is.False);
            var before = runtime.Host.Version;

            SendDrag(card, hudChrome);
            yield return null;
            Assert.That(runtime.Host.Version, Is.EqualTo(before));
            Assert.That(card.parent.resolvedStyle.visibility, Is.EqualTo(Visibility.Visible),
                "Cancelling the drag must return the exact card to its hand slot.");
            Assert.That(runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId, Is.Null,
                "An off-map release is cancellation, not click selection.");

            // A movement below the threshold is still Button.Clickable's ordinary click route.
            var target = BeginDrag(card, out var start);
            var belowThreshold = start + new Vector2(
                DirectorHandsUnityView.CardDragThreshold * 0.25f, 0f);
            SendPointerMove(target, belowThreshold);
            Assert.That(card.parent.resolvedStyle.visibility, Is.EqualTo(Visibility.Visible),
                "Movement below the threshold must not pick the card up from the hand.");
            SendPointerUp(target, belowThreshold);
            yield return null;
            Assert.That(runtime.Host.Version, Is.EqualTo(before));
            Assert.That(runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId,
                Is.EqualTo(model.CardInstanceId));
            Assert.That(Named(root.Hud.RootElement, "gd-confirm-control"), Is.Not.Null);

            // Begin a real drag, then replace the scene before release. Detachment must cancel the
            // captured gesture; the fresh seat scene starts with neither selection nor proxy.
            card = CardControl(root.Hud.RootElement, model);
            target = BeginDrag(card, out _);
            SendPointerMove(target, root.Hud.CameraSafeLayout.UpperRegion.center);
            Assert.That(Named(root.Hud.RootElement, "gd-card-drag-proxy")
                .resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(card.parent.resolvedStyle.visibility, Is.EqualTo(Visibility.Hidden));
            var oldHost = runtime.Host;
            var seat = runtime.Seat.Id;

            yield return ComposeForSeat(seat);
            root = Root();
            runtime = root.Runtime;
            yield return null;
            yield return null;

            Assert.That(oldHost.Version, Is.EqualTo(before),
                "Replacing the session cannot submit the captured old gesture.");
            Assert.That(runtime.Host.Version, Is.Zero);
            Assert.That(runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId, Is.Null);
            Assert.That(Named(root.Hud.RootElement, "gd-card-drag-proxy")
                .resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        /// <summary>
        /// End Turn is a control of its own, measured where the renderer says it is, and pressing it
        /// advances the match through the seat own client. Milestone 8.30l moves that control into
        /// the status bar's top-right slot while the unavailable fan remains bottom-right.
        /// </summary>
        [UnityTest]
        public IEnumerator SeatScene_StandsEndTurnAloneInThePanelCornerAndEndsTheTurnWhenPressed()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            UiPlayer.SetPlayerViewport(2560, 1440, "8.30l top-bar reference viewport");
            yield return LoadSeatSceneForATurnItMayEnd();
            var root = Root();
            var runtime = root.Runtime;

            yield return DriveUntilTheComposedSeatMayEndItsTurn(root);

            var model = runtime.RoundView.Model;
            Assert.That(model.EndTurnInvocation, Is.Not.Null,
                "The drive must reach a turn this seat may end; a skipped case would be a hole in " +
                "this milestone evidence rather than a pass.");

            var control = (Button)Named(root.Hud.RootElement, "gd-end-turn-control");
            Assert.That(control, Is.Not.Null);
            Assert.That(UiPlayer.IsShown(control), Is.True);
            Assert.That(control.text, Is.EqualTo(model.EndTurnInvocation.Label));

            // Where it is drawn, against the renderer own stated corner.
            var snap = UiPlayer.PixelSnapping();
            var stated = root.Hud.EndTurnControlRect;
            var drawn = control.worldBound;
            TestContext.WriteLine(
                "8.30b End Turn at " + Screen.width + "x" + Screen.height + ": panel " +
                ConfiguredMatchHudRenderer.PanelViewport + ", stated " + stated + ", drawn " + drawn +
                ", tray " + root.Hud.CardPanelRect + ", dock " +
                Named(root.Hud.RootElement, "gd-status-dock").worldBound + ".");
            Assert.That(drawn.xMin, Is.EqualTo(stated.xMin).Within(snap));
            Assert.That(drawn.yMin, Is.EqualTo(stated.yMin).Within(snap));
            Assert.That(drawn.width, Is.EqualTo(stated.width).Within(snap * 2f));
            Assert.That(drawn.height, Is.EqualTo(stated.height).Within(snap * 2f));

            var panel = ConfiguredMatchHudRenderer.PanelViewport;
            var unavailable = Named(root.Hud.RootElement, "gd-unavailable-card-group");
            var playableHand = Named(root.Hud.RootElement, "gd-card-buttons");
            var resolving = runtime.HandsView.Model.ViewedHand.PlayableTrayCards
                .Single(card => card.IsResolving);
            Assert.That(UiPlayer.IsShown(unavailable), Is.True,
                "The cards no longer playable on this resolving turn stand in their right-side fan.");
            Assert.That(unavailable.worldBound.xMin, Is.GreaterThan(panel.x / 2f));
            Assert.That(unavailable.worldBound.yMax,
                Is.EqualTo(root.Hud.UnavailableCardGroupRect.yMax).Within(snap * 2f),
                "The unavailable fan is anchored to the panel's bottom-right margin.");
            Assert.That(drawn.Overlaps(unavailable.worldBound), Is.False,
                "End Turn's status-bar slot never overlaps the bottom-right unavailable cards.");
            Assert.That(playableHand.worldBound.xMin, Is.LessThan(panel.x / 2f),
                "The playable and resolving hand remains on the established left side.");
            Assert.That(UiPlayer.Descendants(playableHand).Any(element =>
                    element.name == "gd-card-choice" &&
                    element.tooltip.Contains(resolving.DisplayName)),
                Is.True, "The resolving card stays in the left hand instead of joining unavailable cards.");
            Assert.That(drawn.xMax, Is.LessThanOrEqualTo(panel.x + snap),
                "It is inside the panel right edge,");
            Assert.That(drawn.yMin, Is.EqualTo(0f).Within(snap),
                "and anchored to the panel's top edge.");
            Assert.That(drawn.xMin, Is.GreaterThan(panel.x / 2f),
                "It is on the right half of the panel,");
            Assert.That(drawn.yMax,
                Is.EqualTo(ConfiguredMatchHudRenderer.StatusBarHeight).Within(snap * 2f),
                "and fills the status bar's height.");
            Assert.That(drawn.Overlaps(root.Hud.TieBreakerIndicatorRect), Is.False,
                "The centred Tie Breaker badge keeps a clear gap from End Turn.");

            // Larger than the row it left. `ActionControlFontSize` is what the action row spends.
            Assert.That(control.resolvedStyle.fontSize,
                Is.GreaterThan(RoundActionUnityView.ActionControlFontSize),
                "It is a larger control than the ones it used to sit beside.");
            Assert.That(drawn.height, Is.GreaterThan(HudControlHeightFloor),
                "and it is taller than an ordinary control.");

            // It stands alone: the row it left draws no second copy anywhere on the HUD.
            Assert.That(
                UiPlayer.Descendants(root.Hud.RootElement).OfType<Button>()
                    .Count(button => UiPlayer.IsShown(button) && button.text == control.text),
                Is.EqualTo(1),
                "Exactly one control on the whole HUD carries this label. " +
                UiPlayer.Describe(root.Hud));

            if (!UnityEngine.Application.isBatchMode)
            {
                yield return Capture("tmp/milestone-8-30l-topbar-2560x1440.png");
            }

            UiPlayer.SetPlayerViewport(2048, 1152, "8.30l top-bar validated desktop viewport");
            runtime.RefreshAll();
            yield return null;
            yield return null;
            control = (Button)Named(root.Hud.RootElement, "gd-end-turn-control");
            drawn = control.worldBound;
            stated = root.Hud.EndTurnControlRect;
            Assert.That(drawn.xMin, Is.EqualTo(stated.xMin).Within(snap));
            Assert.That(drawn.yMin, Is.EqualTo(0f).Within(snap));
            Assert.That(drawn.yMax,
                Is.EqualTo(ConfiguredMatchHudRenderer.StatusBarHeight).Within(snap * 2f));
            Assert.That(drawn.Overlaps(root.Hud.TieBreakerIndicatorRect), Is.False);
            Assert.That(unavailable.worldBound.xMin,
                Is.EqualTo(root.Hud.UnavailableCardGroupRect.xMin).Within(snap));
            Assert.That(unavailable.worldBound.yMax,
                Is.EqualTo(root.Hud.UnavailableCardGroupRect.yMax).Within(snap * 2f));

            if (!UnityEngine.Application.isBatchMode)
            {
                yield return Capture("tmp/milestone-8-30l-topbar-2048x1152.png");
            }

            // And pressing it ends the turn, through the seat own client.
            var version = runtime.Host.Version;
            Assert.That(runtime.Seat, Is.Not.Null, "This scene is composed for a seat.");
            UiPlayer.Click(control);
            yield return null;
            yield return null;

            Assert.That(runtime.Host.Version, Is.GreaterThan(version),
                "The press reached the authority.");
            Assert.That(runtime.RoundView.Model.EndTurnInvocation, Is.Null,
                "and the turn it ended offers no second End Turn.");
            Assert.That(UiPlayer.IsShown(Named(root.Hud.RootElement, "gd-end-turn")), Is.False,
                "so the corner is empty rather than holding a control that can only be refused.");

            UiPlayer.SetPlayerViewport(
                restoreWidth, restoreHeight, "8.30l restore prior viewport");
            yield return null;
        }

        /// <summary>
        /// Both amended surfaces at the short supported desktop viewport, which is the shape that
        /// squeezes the panel hardest. Milestone 8.20c introduced it for exactly this purpose.
        /// </summary>
        [UnityTest]
        public IEnumerator SeatScene_KeepsBothAmendedSurfacesInsideTheShortDesktopViewport()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            UiPlayer.SetPlayerViewport(2560, 540, "8.30b short viewport");
            yield return LoadSeatSceneForATurnItMayEnd();
            var root = Root();
            yield return DriveUntilTheComposedSeatMayEndItsTurn(root);
            yield return null;

            var panel = ConfiguredMatchHudRenderer.PanelViewport;
            var snap = UiPlayer.PixelSnapping();
            var topBar = Named(root.Hud.RootElement, "gd-topbar").worldBound;
            var narration = Named(root.Hud.RootElement, "gd-narration").worldBound;
            var control = Named(root.Hud.RootElement, "gd-end-turn-control").worldBound;
            TestContext.WriteLine(
                "8.30b short viewport " + Screen.width + "x" + Screen.height + ": panel " + panel +
                ", topBar " + topBar + ", narration " + narration + ", endTurn " + control +
                ".");

            Assert.That(topBar.height,
                Is.EqualTo(ConfiguredMatchHudRenderer.StatusBarHeight).Within(snap),
                "The strip keeps its stated height on a panel this short rather than collapsing.");
            Assert.That(narration.width, Is.GreaterThan(0f));
            Assert.That(narration.yMax, Is.LessThanOrEqualTo(topBar.yMax + snap));
            Assert.That(control.xMax, Is.LessThanOrEqualTo(panel.x + snap));
            Assert.That(control.yMax, Is.LessThanOrEqualTo(panel.y + snap));
            Assert.That(control.yMin, Is.EqualTo(topBar.yMin).Within(snap),
                "End Turn is anchored to the status bar's top edge.");
            Assert.That(control.yMax, Is.EqualTo(topBar.yMax).Within(snap * 2f),
                "and occupies the bar rather than the compact-hand band.");
            Assert.That(control.Overlaps(root.Hud.TieBreakerIndicatorRect), Is.False,
                "The centred Tie Breaker badge remains separate on the short viewport.");

            UiPlayer.SetPlayerViewport(
                restoreWidth, restoreHeight, "8.30b restore the viewport this session was running at");
            yield return null;
        }

        /// <summary>
        /// The lifetime half of the milestone fourth acceptance check, for the two surfaces this
        /// amendment moved. Switching seats is a recomposition, so the question is whether the second
        /// composition leaves one of each behind or two.
        /// </summary>
        [UnityTest]
        public IEnumerator SeatScene_ComposingASecondSeatLeavesExactlyOneNarrationAndOneCorner()
        {
            UiPlayer.EnsurePlayerViewport();
            yield return LoadSeatScene(0);
            var firstSeat = Root().Runtime.Seat;
            Assert.That(firstSeat, Is.Not.Null);

            yield return LoadSeatScene(1);
            var second = Root();

            Assert.That(second.Runtime.Seat, Is.Not.Null);
            Assert.That(second.Runtime.Seat.Id, Is.Not.EqualTo(firstSeat.Id),
                "The second composition is a different seat, or this case proves nothing.");
            second.Runtime.RefreshAll();
            yield return null;
            yield return null;

            var elements = UiPlayer.Descendants(second.Hud.RootElement).ToArray();
            Assert.That(elements.Count(element => element.name == "gd-narration"), Is.EqualTo(1));
            Assert.That(elements.Count(element => element.name == "gd-narration-lane"), Is.EqualTo(1));
            Assert.That(elements.Count(element => element.name == "gd-end-turn"), Is.EqualTo(1));
            Assert.That(
                Object.FindObjectsByType<ConfiguredMatchHudRenderer>(FindObjectsSortMode.None),
                Has.Length.EqualTo(1),
                "One scene, one HUD; a leaked renderer would draw a second of each of the above.");

            var status = second.Runtime.RoundView.Model.Status;
            Assert.That(status, Is.Not.Empty);
            Assert.That(
                UiPlayer.Descendants(Named(second.Hud.RootElement, "gd-topbar")).OfType<Label>()
                    .Select(label => label.text),
                Does.Contain(status),
                "and the second seat bar reports the second seat own reading.");
        }

        /// <summary>
        /// Milestone 8.30c's six-perspective presentation/lifecycle gate. The development switcher
        /// replaces the scene runtime instead of repointing one, so every composition must begin
        /// with the requested seat's own hand, one handler surface, no local selection from the last
        /// seat, and the same bounded generated resources at every supported viewport.
        /// </summary>
        [UnityTest]
        public IEnumerator SeatScene_RecomposesAllSixPerspectivesWithoutLeakingStateHandlersOrResources()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;

            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            yield return LoadComposedScene();
            var seats = Root().Runtime.Host.Seats.Seats
                .Select(seat => (seat.Id, seat.HeroId))
                .ToArray();
            Assert.That(seats, Has.Length.EqualTo(6));

            var viewports = new[]
            {
                (Width: 2560u, Height: 1440u, Label: "reference"),
                (Width: 2048u, Height: 1152u, Label: "validated desktop"),
                (Width: 2560u, Height: 540u, Label: "short desktop")
            };
            var materialCounts = new List<int>();
            var meshCounts = new List<int>();
            var highlightCounts = new List<int>();
            var selectorCounts = new List<int>();

            for (var index = 0; index < seats.Length; index++)
            {
                var viewport = viewports[index % viewports.Length];
                UiPlayer.SetPlayerViewport(
                    viewport.Width, viewport.Height,
                    "8.30c six-seat " + viewport.Label + " perspective " + index);
                yield return ComposeForSeat(seats[index].Id);
                var root = Root();
                var runtime = root.Runtime;
                runtime.RefreshAll();
                yield return null;
                yield return null;

                Assert.That(Screen.width, Is.EqualTo((int)viewport.Width));
                Assert.That(Screen.height, Is.EqualTo((int)viewport.Height));
                Assert.That(runtime.Seat.Id, Is.EqualTo(seats[index].Id));
                Assert.That(runtime.Seat.HeroId, Is.EqualTo(seats[index].HeroId));
                Assert.That(MatchSceneCompositionRoot.NextComposedSeat, Is.Null,
                    "The development selection is one-shot and cannot choose a later scene.");
                Assert.That(runtime.ReadModel, Is.TypeOf<SeatProjection>(),
                    "Every HUD presenter reads the requested seat projection.");
                Assert.That(runtime.Host.Version, Is.Zero,
                    "A seat switch composes a fresh local session rather than carrying state across.");
                Assert.That(runtime.UndoRestartPresenter, Is.Null);
                Assert.That(runtime.DiagnosticPresenter, Is.Null);

                var hands = runtime.HandsView.Model;
                Assert.That(hands, Is.Not.Null);
                Assert.That(hands.ViewedHand.HeroId, Is.EqualTo(seats[index].HeroId),
                    "A seat opens on its own playable hand, not another player's empty tray.");
                Assert.That(hands.ViewedHand.Cards, Is.Not.Empty,
                    "The requested seat can choose its opening commitment.");
                Assert.That(hands.Hands
                        .Where(hand => !hand.HeroId.Equals(seats[index].HeroId))
                        .SelectMany(hand => hand.Cards),
                    Is.Empty,
                    "Another seat's private hand never enters the drawn model.");
                Assert.That(hands.Hands.Select(hand => hand.SelectedCardInstanceId),
                    Is.All.Null,
                    "No local card selection survives a session replacement.");

                var visibleIds = runtime.Cards.Select(card => card.Id).ToArray();
                foreach (var hidden in runtime.Host.Projection.Cards.Where(card =>
                    !card.OwnerId.Equals(seats[index].HeroId) &&
                    (card.State.Location == CardLocation.Hand ||
                        card.State.Location == CardLocation.Deck)))
                {
                    Assert.That(visibleIds.Contains(hidden.Id), Is.False,
                        "A seat runtime carries no other player's private hand or deck identity.");
                }

                Assert.That(runtime.RoundView.Model.Status, Is.Not.Empty);
                Assert.That(
                    UiPlayer.Descendants(Named(root.Hud.RootElement, "gd-topbar"))
                        .OfType<Label>().Select(label => label.text),
                    Does.Contain(runtime.RoundView.Model.Status));
                Assert.That(Named(root.Hud.RootElement, "gd-narration").worldBound.width,
                    Is.GreaterThan(0f));

                Assert.That(
                    Object.FindObjectsByType<MatchSceneCompositionRoot>(),
                    Has.Length.EqualTo(1));
                Assert.That(
                    Object.FindObjectsByType<ConfiguredMatchHudRenderer>(),
                    Has.Length.EqualTo(1),
                    "One replacement scene leaves one HUD and one set of input handlers.");
                var selectors = Object.FindObjectsByType<PrimitiveHeroPointerSelector>().Length;
                selectorCounts.Add(selectors);
                Assert.That(selectors, Is.EqualTo(6),
                    "The official table has one pointer selector per hero, never an accumulated copy.");

                materialCounts.Add(CountOwnedMaterials());
                meshCounts.Add(CountOwnedMeshes());
                highlightCounts.Add(root.OwnedHighlightNativeResourceCount);

                if (!UnityEngine.Application.isBatchMode && index < viewports.Length)
                {
                    yield return Capture(
                        "tmp/milestone-8-30c-seat-" + index + "-" +
                        viewport.Width + "x" + viewport.Height + ".png");
                }

                if (index == 0)
                {
                    Assert.That(runtime.SelectCommitmentCard(hands.ViewedHand.Cards[0].Id), Is.True);
                    Assert.That(runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId, Is.Not.Null,
                        "The first scene must hold a real local selection for the next replacement " +
                        "to prove it does not retain one.");
                    Assert.That(runtime.Host.Version, Is.Zero,
                        "Selecting a card is local presentation state, not authority.");
                }
            }

            Assert.That(materialCounts.Distinct().Count(), Is.EqualTo(1),
                "Repeated seat composition does not grow generated materials monotonically.");
            Assert.That(meshCounts.Distinct().Count(), Is.EqualTo(1),
                "Repeated seat composition does not grow generated meshes monotonically.");
            Assert.That(highlightCounts.Distinct().Count(), Is.EqualTo(1),
                "Every replacement owns the same bounded highlight resources.");
            Assert.That(selectorCounts.Distinct().Count(), Is.EqualTo(1),
                "Every replacement owns the same bounded input surfaces.");

            UiPlayer.SetPlayerViewport(
                restoreWidth, restoreHeight, "8.30c restore the prior viewport");
            yield return null;
        }

        /// <summary>
        /// The targeted Game View inspection the private-online validation record asks this milestone
        /// for: the amended HUD at the reference viewport and at the short supported desktop one, as
        /// pixels rather than as rectangles. Captures are no-ops under batch mode, so the recipe runs
        /// this fixture with batchMode false.
        /// </summary>
        [UnityTest]
        public IEnumerator SeatScene_CapturesTheAmendedHudAtBothValidatedViewports()
        {
            if (UnityEngine.Application.isBatchMode)
            {
                Assert.That(UnityEngine.Application.isBatchMode, Is.True,
                    "The capture branch is exercised by the dedicated nonbatch recipe.");
                yield break;
            }

            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;

            UiPlayer.SetPlayerViewport(2560, 1440, "8.30b amended HUD at the reference viewport");
            yield return LoadSeatSceneForATurnItMayEnd();
            var root = Root();
            yield return DriveUntilTheComposedSeatMayEndItsTurn(root);
            yield return null;
            TestContext.WriteLine(Describe(root, "2560x1440"));
            yield return Capture("tmp/milestone-8-30b-seat-hud-2560x1440.png");

            UiPlayer.SetPlayerViewport(2560, 540, "8.30b amended HUD at the short desktop viewport");
            yield return null;
            root.Runtime.RefreshAll();
            yield return null;
            yield return null;
            TestContext.WriteLine(Describe(root, "2560x540"));
            yield return Capture("tmp/milestone-8-30b-seat-hud-2560x540.png");

            UiPlayer.SetPlayerViewport(
                restoreWidth, restoreHeight, "8.30b restore the viewport this session was running at");
            yield return null;
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        /// <summary>An ordinary control height, which End Turn is deliberately well above.</summary>
        private const float HudControlHeightFloor = 46f;

        /// <summary>How many rounds of the drive policy either drive will take before failing.</summary>
        private const int DriveBudget = 400;

        private static string Describe(MatchSceneCompositionRoot root, string viewport)
        {
            var hud = root.Hud;
            return "8.30b " + viewport + ": panel " + ConfiguredMatchHudRenderer.PanelViewport +
                ", strip " + Named(hud.RootElement, "gd-topbar").worldBound +
                ", narration " + Named(hud.RootElement, "gd-narration").worldBound +
                " reading [" + root.Runtime.RoundView.Model.Status + "]" +
                ", end turn " + Named(hud.RootElement, "gd-end-turn").worldBound +
                ", tray " + hud.CardPanelRect +
                ", red rail " + hud.RedHeroRailRect + ".";
        }

        /// <summary>
        /// Loads rulebook p.5 official six-player match composed for one of its seats.
        ///
        /// <para>The seat is chosen from the authoritative binding rather than by a written-down id,
        /// so this fixture cannot name a seat the participant graph does not have. That costs one
        /// Director-composed load to read the binding from, which is also the honest way to discover
        /// it: the seats exist because the match does.</para>
        /// </summary>
        private static IEnumerator LoadSeatScene(int seatIndex)
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            yield return LoadComposedScene();

            var seats = Root().Runtime.Host.Seats.Seats;
            Assert.That(seats, Has.Count.GreaterThan(seatIndex),
                "The configured match has fewer seats than this case asks for.");

            yield return ComposeForSeat(seats[seatIndex].Id);
        }

        /// <summary>
        /// Loads the same match composed for a seat the deterministic drive below will hand a turn it
        /// may end.
        ///
        /// <para>Which seat that is cannot be guessed: the actor is whoever the revealed Initiative
        /// order selects, so a fixed index composes a seat that may sit out the whole budget. The
        /// Director's own composition is loaded first and driven by exactly the policy the seat scene
        /// will be driven by, purely to read that seat's identity off it; the composed match then
        /// starts from the same initial state and follows the same path, because the authority is
        /// deterministic and the policy takes each seat's first authorized command every time.</para>
        /// </summary>
        private static IEnumerator LoadSeatSceneForATurnItMayEnd()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            yield return LoadComposedScene();

            var seat = FindSeatThatReachesATurnItMayEnd(Root().Runtime.Host);
            yield return ComposeForSeat(seat);
        }

        private static IEnumerator ComposeForSeat(SeatId seat)
        {
            MatchSceneCompositionRoot.NextComposedSeat = seat;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            yield return LoadComposedScene();

            Assert.That(Root().Runtime.Seat, Is.Not.Null,
                "The scene composed the Director runtime rather than the requested seat.");
            Assert.That(Root().Runtime.Seat.Id, Is.EqualTo(seat));
        }

        /// <summary>
        /// Drives a throwaway host by the drive policy and returns the first seat whose own read model
        /// offers an End Turn outside an exclusive context.
        /// </summary>
        private static SeatId FindSeatThatReachesATurnItMayEnd(IMatchHost host)
        {
            var clients = host.Seats.Seats.ToDictionary(
                seat => seat.Id, seat => new SeatMatchClient(host.ClientForSeat(seat.Id)));

            for (var step = 0; step < DriveBudget; step++)
            {
                foreach (var seat in host.Seats.Seats)
                {
                    var model = RoundActionPresenter.CreateViewModel(host.ProjectionForSeat(seat.Id));
                    if (model.EndTurnInvocation != null && !HasExclusiveDecision(model))
                    {
                        TestContext.WriteLine(
                            "8.30b drive: seat " + seat.Id.Value + " reaches an End Turn at step " +
                            step + ", host version " + host.Version + ".");
                        return seat.Id;
                    }
                }

                var advanced = false;
                foreach (var seat in host.Seats.Seats)
                {
                    var command = host.ProjectionForSeat(seat.Id).AuthorizedCommands.FirstOrDefault();
                    if (command != null)
                    {
                        advanced |= clients[seat.Id].Submit(command).Accepted;
                    }
                }

                Assert.That(advanced, Is.True,
                    "The probe drive stalled at step " + step + ", host version " + host.Version +
                    ", with no seat holding an End Turn.");
            }

            Assert.Fail(
                "No seat reached a turn it may end within " + DriveBudget + " steps; host version " +
                host.Version + ".");
            return default;
        }

        /// <summary>
        /// The view's own exclusivity rule, asked of a model rather than of a drawn view, so the probe
        /// above and the drive below agree about when the corner is empty.
        /// </summary>
        private static bool HasExclusiveDecision(RoundActionViewModel model)
        {
            var view = new RoundActionUnityView();
            view.Render(model);
            return view.HasExclusiveDecision;
        }

        private static IEnumerator LoadComposedScene()
        {
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        /// <summary>
        /// Drives the table through the host until the composed seat own surface offers an End Turn.
        ///
        /// <para>Every seat is answered through its own client, which is the point: a seat surface can
        /// only ever submit what its own projection published, so reaching a turn this seat may end
        /// means the rest of the table has played. Those submissions only get the match moving; the
        /// press this fixture is about is the one made on the drawn control.</para>
        /// </summary>
        private static IEnumerator DriveUntilTheComposedSeatMayEndItsTurn(
            MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            var host = runtime.Host;
            var clients = host.Seats.Seats.ToDictionary(
                seat => seat.Id, seat => new SeatMatchClient(host.ClientForSeat(seat.Id)));

            for (var step = 0; step < DriveBudget; step++)
            {
                runtime.RefreshAll();

                // The drawn control, not merely the projected invocation: an exclusive context keeps
                // the corner empty even when the authority would accept an End Turn, and a drive that
                // stopped on the model alone could hand the cases below a hidden control.
                if (runtime.RoundView.Model?.EndTurnInvocation != null &&
                    !runtime.RoundView.HasExclusiveDecision)
                {
                    yield return null;
                    yield return null;
                    yield break;
                }

                var advanced = false;
                foreach (var seat in host.Seats.Seats)
                {
                    var command = host.ProjectionForSeat(seat.Id).AuthorizedCommands.FirstOrDefault();
                    if (command == null)
                    {
                        continue;
                    }

                    advanced |= clients[seat.Id].Submit(command).Accepted;
                }

                Assert.That(advanced, Is.True,
                    "No seat had anything to answer and the composed seat cannot end a turn, so the " +
                    "drive is stuck rather than finished. Version " + host.Version + ".");
                yield return null;
            }

            Assert.Fail(
                "The drive did not reach a turn the composed seat " + runtime.Seat.Id.Value +
                " may end within " + DriveBudget + " steps; host version " + host.Version + ".");
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }

        private static VisualElement Named(VisualElement root, string name)
        {
            var found = UiPlayer.Descendants(root).Where(element => element.name == name).ToArray();
            Assert.That(found, Has.Length.EqualTo(1),
                "Expected exactly one element named " + name + "; found " + found.Length + ".");
            return found[0];
        }

        private static VisualElement CardControl(VisualElement root, TrayCardViewModel card)
        {
            var found = UiPlayer.Descendants(root).Where(element =>
                element.name == "gd-card-choice" &&
                !string.IsNullOrEmpty(element.tooltip) &&
                element.tooltip.Contains(card.DisplayName)).ToArray();
            Assert.That(found, Has.Length.EqualTo(1),
                "Expected one drawn card control for " + card.DisplayName + ".");
            return found[0];
        }

        private static VisualElement BeginDrag(VisualElement card, out Vector2 start)
        {
            Assert.That(card?.panel, Is.Not.Null);
            start = card.worldBound.center;
            var target = card.panel.Pick(start);
            Assert.That(target, Is.Not.Null);
            using (var down = PointerDownEvent.GetPooled(new Event
            {
                type = EventType.MouseDown,
                mousePosition = start,
                button = 0,
                clickCount = 1
            }))
            {
                down.target = target;
                target.SendEvent(down);
            }

            return target;
        }

        private static void SendDrag(VisualElement card, Vector2 end)
        {
            var target = BeginDrag(card, out _);
            SendPointerMove(target, end);
            SendPointerUp(target, end);
        }

        private static void SendPointerMove(VisualElement target, Vector2 position)
        {
            using (var move = PointerMoveEvent.GetPooled(new Event
            {
                type = EventType.MouseDrag,
                mousePosition = position,
                button = 0
            }))
            {
                move.target = target;
                target.SendEvent(move);
            }
        }

        private static void SendPointerUp(VisualElement target, Vector2 position)
        {
            using (var up = PointerUpEvent.GetPooled(new Event
            {
                type = EventType.MouseUp,
                mousePosition = position,
                button = 0,
                clickCount = 1
            }))
            {
                up.target = target;
                target.SendEvent(up);
            }
        }

        private static IEnumerator Capture(string relativePath)
        {
            var path = Path.GetFullPath(relativePath);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            yield return null;
            yield return null;
            Assert.That(File.Exists(path), Is.True,
                "The targeted Game View visual evidence must be captured at " + relativePath + ".");
        }

        private static int CountOwnedMaterials()
        {
            return Resources.FindObjectsOfTypeAll<Material>().Count(value =>
                value != null && value.hideFlags == HideFlags.DontSave &&
                (value.name.EndsWith(" Debug", System.StringComparison.Ordinal) ||
                 value.name.EndsWith(" Placeholder", System.StringComparison.Ordinal) ||
                 value.name == DestinationHighlightMaterialOwner.GeneratedMaterialName));
        }

        private static int CountOwnedMeshes()
        {
            return Resources.FindObjectsOfTypeAll<Mesh>().Count(value =>
                value != null && value.name == "Owned Placeholder Grid Hex Mesh");
        }
    }
}
