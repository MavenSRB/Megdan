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
                    AssertClearOfTheRoundDocksLiveContent(root, live, viewport.Label);

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

        /// <summary>
        /// Milestone 8.46g. Rulebook p.15's Upgrade a Card, expanded: SIX candidate faces at the
        /// tray's own footprint, the seat's hand above them at the one reduced scale this window
        /// declares, and the whole of it inside the fixed rectangle at every viewport the two cases
        /// above sweep.
        ///
        /// <para><b>Why the scale of the preview is measured and the scale of the candidates is
        /// asserted to be none.</b> The user's rule for the series is "make sure you don't change the
        /// scaling on the cards" - which is about the cards a player is CHOOSING BETWEEN. The hand
        /// preview is not one of those: it is the reminder of what the seat is holding, it has been
        /// drawn at a reduced scale since milestone 8.35z58, and the window's fixed 848-unit height
        /// cannot hold the strip's own 0.85 above a full-footprint candidate row (see
        /// <see cref="DecisionWindowUnityView.LevelUpHandPreviewHeight"/>, which carries the
        /// arithmetic). So the candidates are asserted to be EXACTLY the tray's card, and the preview
        /// is asserted to be exactly the scale the window derived - not merely "smaller".</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheExpandedLevelUp_HoldsSixCandidatesAtTheTraysFootprintAboveTheSeatsHand()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                yield return LoadEngagedScene();
                var root = Root();
                // The three the cases above sweep, plus the SHORTEST viewport this project's
                // journeys drive. That one is where the first Play Mode run of this block found the
                // offer 16 units outside the rectangle: at 2560x540 one device pixel is 2.67
                // reference units, and a column of stacked boxes measures a dozen of them taller
                // than its declared sum. The arithmetic in `DecisionWindowUnityView` now carries
                // that cost, and this is what proves it.
                foreach (var viewport in Viewports()
                    .Concat(new[] { (Width: 2560u, Height: 540u, Label: "a short 2560x540") }))
                {
                    UiPlayer.SetPlayerViewport(
                        viewport.Width, viewport.Height, "8.46g level-up " + viewport.Label);
                    yield return null;
                    yield return null;

                    StandLevelUp(root);
                    yield return null;
                    yield return null;

                    var panel = ConfiguredMatchHudRenderer.PanelViewport;
                    var expected = ConfiguredMatchHudRenderer.CalculateDecisionWindowExpandedRect(
                        panel.x, panel.y);
                    var live = Window(root).worldBound;
                    AssertBoundsEqual(expected, live, viewport.Label);
                    Assert.That(live.height,
                        Is.GreaterThanOrEqualTo(
                            DecisionWindowUnityView.ExpandedReferenceHeight - UiPlayer.PixelSnapping()),
                        "The level-up's whole arithmetic is written against the reference height, " +
                        "which the panel's Expand match mode makes the MINIMUM at " + viewport.Label + ".");
                    AssertClearOfEverythingThatExists(root, live, panel, viewport.Label);
                    AssertClearOfTheRoundDocksLiveContent(root, live, viewport.Label);

                    // p.15's six candidates, at the tray's own card and wholly inside the window.
                    var faces = UiPlayer.Descendants(Window(root))
                        .Where(element => element.name == "gd-decision-window-face")
                        .ToArray();
                    Assert.That(faces, Has.Length.EqualTo(6),
                        "Three pairs of two candidates at " + viewport.Label + ".");
                    foreach (var face in faces)
                    {
                        AssertContains(live, face.worldBound,
                            "Every candidate face is wholly inside the window at " + viewport.Label + ".");
                        Assert.That(face.worldBound.width,
                            Is.EqualTo(CardFootprint.Width).Within(UiPlayer.PixelSnapping()),
                            "A candidate is the tray's own card at " + viewport.Label + ".");
                        Assert.That(face.worldBound.height,
                            Is.EqualTo(CardFootprint.Height).Within(UiPlayer.PixelSnapping()),
                            "A candidate is the tray's own card at " + viewport.Label + ".");
                    }

                    for (var first = 0; first < faces.Length; first++)
                    {
                        for (var second = first + 1; second < faces.Length; second++)
                        {
                            Assert.That(
                                faces[first].worldBound.Overlaps(faces[second].worldBound), Is.False,
                                "No two candidates overlap at " + viewport.Label + ": " +
                                faces[first].worldBound + " against " + faces[second].worldBound);
                        }
                    }

                    // The pairs the pause grouped, and the option controls every level-up journey
                    // presses by name.
                    var band = UiPlayer.Descendants(Window(root))
                        .Where(element => element.name == "gd-upgrade-comparison-band")
                        .ToArray();
                    Assert.That(band, Has.Length.EqualTo(1));
                    AssertContains(live, band[0].worldBound,
                        "The offer band is inside the window at " + viewport.Label + ".");
                    Assert.That(
                        UiPlayer.Descendants(band[0]).Count(element => element.name == "gd-upgrade-pair"),
                        Is.EqualTo(3), "p.15's three source cards keep their pairs at " + viewport.Label + ".");
                    var options = UiPlayer.Descendants(band[0])
                        .Where(element => element.name == "gd-upgrade-option")
                        .ToArray();
                    Assert.That(options, Has.Length.EqualTo(6));
                    foreach (var option in options)
                    {
                        AssertContains(live, option.worldBound,
                            "A complete option, its gained-item fact included, fits the window at " +
                            viewport.Label + ".");
                        var item = UiPlayer.Descendants(option)
                            .Single(element => element.name == "gd-upgrade-item");
                        AssertContains(live, item.worldBound,
                            "and so does the GAINS box drawn over its bottom band at " +
                            viewport.Label + ".");
                    }

                    // The seat's own hand above the offer, at the derived scale, clear of the band.
                    var preview = UiPlayer.Descendants(Window(root))
                        .Single(element => element.name == "gd-levelup-hand-preview");
                    var slots = UiPlayer.Descendants(preview)
                        .Where(element => element.name == "gd-levelup-hand-preview-card")
                        .ToArray();
                    Assert.That(slots, Has.Length.EqualTo(5),
                        "All five cards the seat is holding at " + viewport.Label + ".");
                    foreach (var slot in slots)
                    {
                        AssertContains(live, slot.worldBound,
                            "Every preview card is inside the window at " + viewport.Label + ".");
                        Assert.That(slot.worldBound.height,
                            Is.EqualTo(CardFootprint.Height *
                                DecisionWindowUnityView.LevelUpHandPreviewScale)
                                .Within(UiPlayer.PixelSnapping()),
                            "at the scale the window's own arithmetic derived, at " + viewport.Label + ".");
                        Assert.That(slot.worldBound.Overlaps(band[0].worldBound), Is.False,
                            "and above the offer rather than over it, at " + viewport.Label + ".");
                    }

                    // Every drawn label outside the preview - whose slots clip their own scaled face
                    // by construction - is laid out inside the window too.
                    foreach (var label in UiPlayer.Descendants(Window(root)).OfType<Label>()
                        .Where(candidate => UiPlayer.IsShown(candidate) &&
                            !string.IsNullOrEmpty(candidate.text) &&
                            !UiPlayer.Descendants(preview).Contains(candidate)))
                    {
                        AssertContains(live, label.worldBound,
                            $"'{label.text}' is drawn inside the window at {viewport.Label}.");
                    }
                }
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "8.46g restore");
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

        /// <summary>
        /// Milestone 8.46g. p.15's pause as the window draws it, staged by hand for the reason every
        /// case in this file stages by hand: the subject is the RENDERER's placement, and standing the
        /// shape deterministically at three viewports is what makes the measurement about geometry
        /// rather than about which decision a walk happened to reach. That the options are the
        /// authority's own - and that every string the strip showed survives the move - is proved in
        /// <c>DecisionWindowPresenterTests</c> against a live pause.
        /// </summary>
        private static void StandLevelUp(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            var hand = runtime.HandsView.Model.ViewedHand.TrayCards.Take(5).ToArray();
            Assert.That(hand, Has.Length.EqualTo(5),
                "The staged hand must hold five faces for the preview measurement.");

            var controls = new List<DecisionWindowControlViewModel>();
            var colours = new[] { CardColor.Blue, CardColor.Green, CardColor.Red };
            for (var pair = 0; pair < 3; pair++)
            {
                var source = new CardInstanceId("8-46g-upgraded-" + pair);
                for (var candidate = 0; candidate < 2; candidate++)
                {
                    var face = hand[((pair * 2) + candidate) % hand.Length];
                    var option = new SystemTransitionOptionViewModel(
                        "UPGRADE " + face.DisplayName.ToUpperInvariant() + " INTO " +
                            face.DisplayName.ToUpperInvariant(),
                        "SETS " + face.DisplayName.ToUpperInvariant() + " ASIDE AS A MOVE +1 ITEM",
                        DirectorCommand.AdvanceEndOfRound(),
                        colours[pair],
                        source,
                        face,
                        face.DisplayName,
                        // Both arms of the shared GAINS overlay: a printed item, and the absence p.15
                        // and p.16 produce between them, which the box states in words.
                        candidate == 0 ? PrintedValueChannel.Attack : PrintedValueChannel.None,
                        face.DisplayName);
                    controls.Add(new DecisionWindowControlViewModel(
                        option.Label,
                        option.Command,
                        card: option.Card,
                        detail: option.Detail,
                        upgradeOption: option));
                }
            }

            runtime.DecisionWindowView.Render(new DecisionWindowViewModel(
                3L,
                DecisionWindowFamily.LevelUp,
                DecisionWindowState.Expanded,
                "8.46g-geometry-level-up",
                "UPGRADE A CARD",
                "LEVEL UP",
                "RED WASP REACHED LEVEL 2 - UPGRADE A CARD",
                "PAID 1 COIN - 1 REMAINING",
                controls: controls,
                handPreview: hand));
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

        /// <summary>
        /// Milestone 8.46e. The expanded rectangle overlaps the round dock's DECLARED rectangle by
        /// construction - it spans the whole column between the rails - so the honest check is
        /// against what the dock is actually DRAWING: every shown child with a size must sit clear of
        /// the window, or the window is covering a control the player may need.
        /// </summary>
        private static void AssertClearOfTheRoundDocksLiveContent(
            MatchSceneCompositionRoot root, Rect window, string label)
        {
            var dock = UiPlayer.Descendants(root.Hud.RootElement)
                .FirstOrDefault(element => element.name == "gd-status-dock");
            if (dock == null || !UiPlayer.IsShown(dock))
            {
                return;
            }

            foreach (var child in dock.Children())
            {
                if (!UiPlayer.IsShown(child) || child.worldBound.width <= 0f || child.worldBound.height <= 0f ||
                    child.name == "gd-decision-window")
                {
                    continue;
                }

                Assert.That(child.worldBound.Overlaps(window), Is.False,
                    $"The decision window covers the round dock's live content '{child.name}' at {label}: " +
                    $"{window} against {child.worldBound}.");
            }
        }

        /// <summary>
        /// Milestone 8.46e. The longest consequence label the roster declares is laid out in a real
        /// window control at the reference and must fit it: the control wraps, and the wrapped text
        /// is no taller than the control that holds it, so nothing is clipped.
        /// </summary>
        [UnityTest]
        public IEnumerator TheLongestDeclaredLabel_FitsItsControlWithoutClipping()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                yield return LoadEngagedScene();
                var root = Root();
                UiPlayer.SetPlayerViewport(2560u, 1440u, "8.46e longest label");
                yield return null;
                yield return null;

                var longest = DecisionLabelCatalog.Registered.Values
                    .SelectMany(table => table.Values)
                    .SelectMany(declared => declared.HasDeclined
                        ? new[] { declared.Chosen, declared.Declined }
                        : new[] { declared.Chosen })
                    .OrderByDescending(text => text.Length)
                    .First();
                var probe = new DecisionWindowControlViewModel(longest.ToUpperInvariant());
                var model = new DecisionWindowViewModel(
                    root.Runtime.Session?.Projection?.Version ?? 1,
                    DecisionWindowFamily.Mode,
                    DecisionWindowState.Expanded,
                    "8.46e-longest-label-probe",
                    "LONGEST LABEL",
                    "8.46E PROBE",
                    "The longest consequence label the roster declares, in a real control.",
                    controls: new[] { probe, new DecisionWindowControlViewModel("A SHORT ONE") });
                root.Runtime.DecisionWindowView.Render(model);
                yield return null;
                yield return null;

                var element = root.Runtime.DecisionWindowView.ElementFor(probe);
                Assert.That(element, Is.Not.Null, "The probe control is drawn.");
                var window = Window(root).worldBound;
                AssertContains(window, element.worldBound,
                    $"The control holding '{longest}' ({longest.Length} characters) sits inside the window.");
                var text = element as TextElement ?? UiPlayer.Descendants(element).OfType<TextElement>()
                    .FirstOrDefault(candidate => candidate.text == probe.Label);
                Assert.That(text, Is.Not.Null, "The control carries the label as text.");
                Assert.That(text.resolvedStyle.whiteSpace, Is.EqualTo(WhiteSpace.Normal),
                    "A long label wraps rather than running out of its control.");
                var wrapped = text.MeasureTextSize(
                    text.text, text.contentRect.width, VisualElement.MeasureMode.AtMost,
                    0f, VisualElement.MeasureMode.Undefined);
                Assert.That(text.contentRect.height, Is.GreaterThanOrEqualTo(wrapped.y - UiPlayer.PixelSnapping()),
                    $"The wrapped text of '{longest}' is no taller than the control that holds it, so nothing is clipped.");
            }
            finally
            {
                root_RestoreAfterProbe();
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "8.46e restore");
            }
        }

        private static void root_RestoreAfterProbe()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            root?.Runtime?.RefreshAll();
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
