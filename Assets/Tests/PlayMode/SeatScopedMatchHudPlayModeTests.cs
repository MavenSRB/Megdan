using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
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

            // The strip is the identity row plus the lane, and the Tie Breaker chip is still measured
            // against the identity row alone - the amendment widened the strip without moving the chip.
            var snap = UiPlayer.PixelSnapping();
            Assert.That(topBar.worldBound.height,
                Is.EqualTo(ConfiguredMatchHudRenderer.StatusBarHeight).Within(snap));
            Assert.That(Named(root.Hud.RootElement, "gd-topbar-identity").worldBound.height,
                Is.EqualTo(ConfiguredMatchHudRenderer.StatusBarIdentityRowHeight).Within(snap));
            var chip = Named(root.Hud.RootElement, "gd-tie-breaker").worldBound;
            Assert.That(chip.yMin, Is.EqualTo(root.Hud.TieBreakerIndicatorRect.yMin).Within(snap));
            Assert.That(chip.height,
                Is.EqualTo(root.Hud.TieBreakerIndicatorRect.height).Within(snap));
            Assert.That(chip.yMax, Is.LessThanOrEqualTo(lane.worldBound.yMin + snap),
                "The chip stays inside the identity row rather than reaching down over the sentence.");

            // The narration reads across the bar rather than being clipped into the corner it left.
            Assert.That(narration.worldBound.width,
                Is.GreaterThan(ConfiguredMatchHudRenderer.StatusDockWidth),
                "The lane is wider than the dock the sentence used to be drawn in.");
            Assert.That(narration.worldBound.yMax,
                Is.LessThanOrEqualTo(topBar.worldBound.yMax + snap),
                "and it is inside the strip that carries it.");
        }

        /// <summary>
        /// The amendment's second half: End Turn is a control of its own in the panel bottom-right
        /// corner, it is measured where the renderer says it is, and pressing it advances the match
        /// through the seat own client.
        /// </summary>
        [UnityTest]
        public IEnumerator SeatScene_StandsEndTurnAloneInThePanelCornerAndEndsTheTurnWhenPressed()
        {
            // A realistic desktop viewport, because this case measures where a control is drawn. The
            // batch runner's own 640x480 default is a viewport `UiPlayer` itself calls unplayable.
            UiPlayer.EnsurePlayerViewport();
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
            Assert.That(drawn.xMax, Is.LessThanOrEqualTo(panel.x + snap),
                "It is inside the panel right edge,");
            Assert.That(drawn.yMax, Is.LessThanOrEqualTo(panel.y + snap),
                "and inside its bottom edge - the corner the amendment asked for.");
            Assert.That(drawn.xMin, Is.GreaterThan(panel.x / 2f),
                "It is on the right half of the panel,");
            Assert.That(drawn.yMin, Is.GreaterThan(panel.y / 2f),
                "and the bottom half.");

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
            var hand = ConfiguredMatchHudRenderer.CalculateCompactHandPointerRect(panel.x, panel.y);
            TestContext.WriteLine(
                "8.30b short viewport " + Screen.width + "x" + Screen.height + ": panel " + panel +
                ", topBar " + topBar + ", narration " + narration + ", endTurn " + control +
                ", compact hand " + hand + ".");

            Assert.That(topBar.height,
                Is.EqualTo(ConfiguredMatchHudRenderer.StatusBarHeight).Within(snap),
                "The strip keeps its stated height on a panel this short rather than collapsing.");
            Assert.That(narration.width, Is.GreaterThan(0f));
            Assert.That(narration.yMax, Is.LessThanOrEqualTo(topBar.yMax + snap));
            Assert.That(control.xMax, Is.LessThanOrEqualTo(panel.x + snap));
            Assert.That(control.yMax, Is.LessThanOrEqualTo(panel.y + snap));
            Assert.That(control.xMin, Is.GreaterThanOrEqualTo(hand.xMax - snap),
                "End Turn yields to the compact hand rather than being drawn over it.");
            Assert.That(control.yMin,
                Is.GreaterThanOrEqualTo(ConfiguredMatchHudRenderer.StatusBarHeight - snap),
                "and never reaches the strip.");

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
