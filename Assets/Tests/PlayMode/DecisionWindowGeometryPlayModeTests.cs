using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
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
    /// Milestone 8.46c. WHERE the decision window is actually drawn on a live scaled panel.
    ///
    /// <para><b>Why this cannot be an Edit Mode case.</b> The presenter fixture proves which family
    /// opens in which state and what each control says; none of that needs a panel. What needs one is
    /// the user's own binding instruction for the whole series - "nothing new may overlap anything
    /// that exists at any resolution" - which is a question about live <c>worldBound</c>s against the
    /// renderer's own stated rectangles, asked at more than one viewport.</para>
    ///
    /// <para>The window is rendered directly here rather than driven to through a match, deliberately:
    /// the subject is the RENDERER's placement, and staging the two states by hand is what makes the
    /// measurement deterministic at three viewports instead of dependent on which decision a walk
    /// happened to reach. Milestone 8.46e owns the complete gate; this is its first half.</para>
    /// </summary>
    public sealed class DecisionWindowGeometryPlayModeTests
    {
        /// <summary>
        /// The user's own acceptance sentence for the expanded state: "a defense window holding four
        /// candidates", at the tray's own card footprint - "make sure you don't change the scaling on
        /// the cards" - with every text fitting and nothing overlapping anything that exists.
        /// </summary>
        [UnityTest]
        public IEnumerator TheExpandedWindow_HoldsFourDefenseCandidatesInsideTheBoardAndCoversNothingElse()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                yield return LoadEngagedScene();
                var root = Root();
                foreach (var viewport in Viewports())
                {
                    UiPlayer.SetPlayerViewport(
                        viewport.Width, viewport.Height, "8.46c expanded " + viewport.Label);
                    yield return null;
                    yield return null;

                    StandDefense(root);
                    yield return null;
                    yield return null;

                    var panel = ConfiguredMatchHudRenderer.PanelViewport;
                    var expected = ConfiguredMatchHudRenderer.CalculateDecisionWindowExpandedRect(
                        panel.x, panel.y);
                    var live = Window(root).worldBound;
                    AssertBoundsEqual(expected, live, viewport.Label);
                    AssertClearOfEverythingThatExists(root, live, panel, viewport.Label);

                    // The five faces the user's four-candidate sentence asks for - the attacker's
                    // card and four answers - drawn at the TRAY'S OWN FOOTPRINT, distinct, and wholly
                    // inside the window. The footprint is asserted here rather than taken on trust:
                    // "make sure you don't change the scaling on the cards" is the user's own
                    // instruction, and a window that quietly shrank a face to make one fit would
                    // satisfy every other assertion in this case.
                    var faces = UiPlayer.Descendants(Window(root))
                        .Where(element => element.name == "gd-decision-window-face")
                        .ToArray();
                    Assert.That(faces, Has.Length.EqualTo(5),
                        "Four candidates plus the attacker's own card at " + viewport.Label + ".");
                    foreach (var face in faces)
                    {
                        AssertContains(live, face.worldBound,
                            "Every face is wholly inside the window at " + viewport.Label + ".");
                        Assert.That(face.worldBound.width,
                            Is.EqualTo(CardFootprint.Width).Within(UiPlayer.PixelSnapping()),
                            "A card is the tray's own card at " + viewport.Label + ".");
                        Assert.That(face.worldBound.height,
                            Is.EqualTo(CardFootprint.Height).Within(UiPlayer.PixelSnapping()),
                            "A card is the tray's own card at " + viewport.Label + ".");
                    }

                    for (var first = 0; first < faces.Length; first++)
                    {
                        for (var second = first + 1; second < faces.Length; second++)
                        {
                            Assert.That(
                                faces[first].worldBound.Overlaps(faces[second].worldBound), Is.False,
                                "No two faces overlap at " + viewport.Label + ": " +
                                faces[first].worldBound + " against " + faces[second].worldBound);
                        }
                    }

                    // Every drawn label is laid out inside the window too, which is what "every text
                    // fitting" means for a surface whose labels all wrap.
                    foreach (var label in UiPlayer.Descendants(Window(root)).OfType<Label>()
                        .Where(candidate => UiPlayer.IsShown(candidate) &&
                            !string.IsNullOrEmpty(candidate.text)))
                    {
                        AssertContains(live, label.worldBound,
                            $"'{label.text}' is drawn inside the window at {viewport.Label}.");
                    }
                }
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "8.46c restore");
            }
        }

        /// <summary>
        /// The docked state: the round dock's own rectangle, clear of `DIRECTOR`, the rails, the tray
        /// and - because a board-answered decision is what docks - clear of the board itself.
        /// </summary>
        [UnityTest]
        public IEnumerator TheDockedWindow_StandsInTheRoundDockAndLeavesTheBoardUncovered()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                yield return LoadEngagedScene();
                var root = Root();
                foreach (var viewport in Viewports())
                {
                    UiPlayer.SetPlayerViewport(
                        viewport.Width, viewport.Height, "8.46c docked " + viewport.Label);
                    yield return null;
                    yield return null;

                    StandMinionBattle(root);
                    yield return null;
                    yield return null;

                    var panel = ConfiguredMatchHudRenderer.PanelViewport;
                    var expected = ConfiguredMatchHudRenderer.CalculateDecisionWindowDockedRect(
                        panel.x, panel.y);
                    var live = Window(root).worldBound;
                    AssertBoundsEqual(expected, live, viewport.Label);
                    AssertContains(
                        ConfiguredMatchHudRenderer.CalculateStatusDockRect(panel.x, panel.y), live,
                        "The docked window stands inside the round dock's own rectangle at " +
                        viewport.Label + ".");
                    AssertClearOfEverythingThatExists(root, live, panel, viewport.Label);
                    Assert.That(
                        ConfiguredMatchHudRenderer.CalculatePlayfieldRect(panel.x, panel.y)
                            .Overlaps(live), Is.False,
                        "A board-answered decision never covers the pieces its own question is " +
                        "about, at " + viewport.Label + ".");
                }
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "8.46c restore");
            }
        }

        // -----------------------------------------------------------------------------------------
        // Staging
        // -----------------------------------------------------------------------------------------

        private static void StandDefense(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            var cards = runtime.HandsView.Model.ViewedHand.TrayCards.Take(5).ToArray();
            Assert.That(cards, Has.Length.EqualTo(5),
                "The staged hand must hold five faces for the four-candidate measurement.");
            var controls = cards.Skip(1).Select(card => new DecisionWindowControlViewModel(
                    card.DisplayName.ToUpperInvariant(),
                    DirectorCommand.ChooseDefenseCard(card.CardInstanceId),
                    card: card,
                    valueChip: "DEFENSE 2"))
                .ToArray();
            runtime.DecisionWindowView.Render(new DecisionWindowViewModel(
                1L,
                DecisionWindowFamily.Defense,
                DecisionWindowState.Expanded,
                "8.46c-geometry-defense",
                "DEFEND AGAINST " + cards[0].DisplayName.ToUpperInvariant() + "?",
                "WASP ATTACKS ARIEN",
                "Choose a card to defend with, or take the hit.",
                contextCards: new[]
                {
                    new DecisionWindowCardViewModel(cards[0], "ATTACK 7", "RANGED")
                },
                controls: controls,
                cornerControl: new DecisionWindowControlViewModel(
                    DecisionWindowPresenter.TakeTheHitLabel,
                    DirectorCommand.DeclineDefense(),
                    role: DecisionWindowControlRole.Corner)));
        }

        private static void StandMinionBattle(MatchSceneCompositionRoot root)
        {
            root.Runtime.DecisionWindowView.Render(new DecisionWindowViewModel(
                2L,
                DecisionWindowFamily.MinionBattleRemoval,
                DecisionWindowState.Docked,
                "8.46c-geometry-minion-battle",
                "REMOVE 2 MINIONS",
                "MINION BATTLE · " + MinionChoiceLabels.TeamLabel(TeamSide.Blue),
                "Click minions on the board until 2 are chosen.",
                "1 of 2 chosen"));
        }

        // -----------------------------------------------------------------------------------------
        // Measurement
        // -----------------------------------------------------------------------------------------

        private static IEnumerable<(uint Width, uint Height, string Label)> Viewports() =>
            new[]
            {
                (2560u, 1440u, "the 2560x1440 reference"),
                (3440u, 1440u, "an ultrawide 3440x1440"),
                (1280u, 1024u, "a narrow 5:4 1280x1024")
            };

        /// <summary>
        /// The window is clear of everything the HUD already draws. Each neighbour is asked of the
        /// renderer's own stated rectangle rather than of a literal, so the two cannot drift.
        /// </summary>
        private static void AssertClearOfEverythingThatExists(
            MatchSceneCompositionRoot root, Rect window, Vector2 panel, string label)
        {
            var neighbours = new (string Name, Rect Rect)[]
            {
                ("the status strip",
                    ConfiguredMatchHudRenderer.CalculateStatusBarRect(panel.x, panel.y)),
                ("END TURN",
                    ConfiguredMatchHudRenderer.CalculateEndTurnControlRect(panel.x, panel.y)),
                ("the seat undo square",
                    ConfiguredMatchHudRenderer.CalculateSeatUndoControlRect(panel.x, panel.y)),
                ("the hand tray",
                    ConfiguredMatchHudRenderer.CalculateCompactHandPointerRect(panel.x, panel.y)),
                ("the Red rail",
                    ConfiguredMatchHudRenderer.CalculateHeroRailRect(panel.x, panel.y, TeamSide.Red)),
                ("the Blue rail",
                    ConfiguredMatchHudRenderer.CalculateHeroRailRect(panel.x, panel.y, TeamSide.Blue))
            };

            foreach (var neighbour in neighbours)
            {
                Assert.That(neighbour.Rect.Overlaps(window), Is.False,
                    $"The decision window covers {neighbour.Name} at {label}: {window} against " +
                    neighbour.Rect);
            }

            // The DIRECTOR toggle has no static rectangle - it follows the live hero column - so it is
            // measured where it is actually drawn.
            var toggle = UiPlayer.Descendants(root.Hud.RootElement)
                .FirstOrDefault(element => element.name == "gd-director-toggle");
            if (toggle != null && UiPlayer.IsShown(toggle))
            {
                Assert.That(toggle.worldBound.Overlaps(window), Is.False,
                    $"The decision window covers the DIRECTOR toggle at {label}: {window} against " +
                    toggle.worldBound);
            }

            // And the two strip controls, where they are actually drawn, whenever they are.
            foreach (var pair in new[]
            {
                ("the drawn seat undo square", root.Runtime.RoundView.SeatUndoRoot),
                ("the drawn END TURN", root.Runtime.RoundView.EndTurnRoot)
            })
            {
                if (pair.Item2 != null && UiPlayer.IsShown(pair.Item2))
                {
                    Assert.That(pair.Item2.worldBound.Overlaps(window), Is.False,
                        $"The decision window covers {pair.Item1} at {label}.");
                }
            }
        }

        private static void AssertContains(Rect outer, Rect inner, string because)
        {
            var snap = UiPlayer.PixelSnapping();
            Assert.That(inner.xMin, Is.GreaterThanOrEqualTo(outer.xMin - snap), because);
            Assert.That(inner.yMin, Is.GreaterThanOrEqualTo(outer.yMin - snap), because);
            Assert.That(inner.xMax, Is.LessThanOrEqualTo(outer.xMax + snap), because);
            Assert.That(inner.yMax, Is.LessThanOrEqualTo(outer.yMax + snap), because);
        }

        private static void AssertBoundsEqual(Rect expected, Rect live, string label)
        {
            var snap = UiPlayer.PixelSnapping();
            Assert.That(live.xMin, Is.EqualTo(expected.xMin).Within(snap),
                "The window is drawn where the renderer states it is, at " + label + ".");
            Assert.That(live.yMin, Is.EqualTo(expected.yMin).Within(snap));
            Assert.That(live.width, Is.EqualTo(expected.width).Within(snap));
            Assert.That(live.height, Is.EqualTo(expected.height).Within(snap));
        }

        private static VisualElement Window(MatchSceneCompositionRoot root)
        {
            var found = UiPlayer.Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-decision-window")
                .ToArray();
            Assert.That(found, Has.Length.EqualTo(1),
                "The composed HUD draws exactly one decision window.");
            Assert.That(UiPlayer.IsShown(found[0]), Is.True,
                "and it is shown while a decision stands.");
            return found[0];
        }

        private static IEnumerator LoadEngagedScene()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedCardContent =
                SceneCardContent.VerifiedEngagedMirror;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }
    }
}
