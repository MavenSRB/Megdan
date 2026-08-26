using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// The permanent hero player-path completion gate's standard-footprint conformance check.
    /// Milestone 8.20c.
    ///
    /// <para>It is the machinery behind
    /// <see cref="PlayerInteractionShape.StandardCardFootprintConformance"/>, and it is hero-neutral
    /// on purpose: it takes a hero's verified content and measures <em>every</em> face in it on the
    /// live panel at the validated player viewport. A hero's own fixture supplies the content and
    /// nothing else, so the coverage cannot be narrowed by the row that names the check - and a hero
    /// added after the mapped pitch roster gets the same measurement as the two that shipped before
    /// the check existed.</para>
    ///
    /// <para><b>What it proves, and why each half is needed.</b> The footprint half asserts that a
    /// face is exactly <see cref="CardFootprint"/> once the panel has laid it out - resolved, not
    /// declared, because a declared width is only a preference until a container overrules it, and
    /// containers had. The fit half asserts that nothing meaningful is lost inside that box: the five
    /// bands stay in order and do not overlap, every drawn label is inside the card's own bounds, and
    /// every label is at least as tall as the text it holds needs at the width it was given. The last
    /// one is the one that catches the failure mode a fixed height introduces - a rules panel squeezed
    /// by a card too short for its sentence clips the words silently, because the card's frame is
    /// <c>overflow: hidden</c> and a clipped label still reports the text it was asked to draw.</para>
    ///
    /// <para>Failures are collected and reported together rather than thrown at the first face. A hero
    /// whose set does not fit is one fact about that set, and reading it one card per run would take
    /// nineteen runs to learn.</para>
    /// </summary>
    internal static class StandardCardFootprintCheck
    {
        /// <summary>
        /// A pixel of slack, for the one place exactness is not meaningful: laid-out edges are floats
        /// and a band's bottom meeting the next band's top is a comparison of two accumulated sums.
        /// The footprint itself is compared exactly.
        /// </summary>
        private const float EdgeTolerance = 0.5f;

        /// <summary>
        /// The five regions of a printed face, top to bottom, in the order
        /// <c>DirectorHandsUnityView</c> builds them. Named here so the check reads the card's own
        /// structure rather than a count: a band that stopped being drawn fails as a missing region
        /// instead of quietly reducing what is measured.
        /// </summary>
        private static readonly string[] BandsTopToBottom =
        {
            "gd-card-name-band",
            "gd-card-secondary-values",
            "gd-card-primary-action",
            "gd-card-rules",
            "gd-card-history"
        };

        /// <summary>
        /// Measures every verified face of one hero against the standard footprint on the live HUD
        /// panel, and fails with every non-conforming face at once.
        /// </summary>
        /// <param name="heroDisplayName">The hero whose set this is, for the failure report.</param>
        /// <param name="definitions">That hero's verified card definitions - all of them.</param>
        /// <param name="hud">The configured HUD, whose panel supplies the real layout.</param>
        public static IEnumerator EveryVerifiedFaceKeepsTheStandardFootprint(
            string heroDisplayName,
            IReadOnlyList<CardDefinition> definitions,
            ConfiguredMatchHudRenderer hud)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (hud == null) throw new ArgumentNullException(nameof(hud));
            Assert.That(definitions, Is.Not.Empty,
                $"{heroDisplayName} supplies no verified faces to measure, which is not a pass.");
            Assert.That(hud.RootElement?.panel, Is.Not.Null,
                "The footprint is measured on the live panel; there is none.");
            // Milestone 8.25c: the gate measures at the reference resolution, which is what the live
            // panel lays out in. It is the same measurement it always was - the reference is the
            // 1920x1080 screen milestone 8.20c took the footprint on - but it is now a fact about the
            // panel rather than about how many pixels the Game View happens to have.
            var panel = ConfiguredMatchHudRenderer.PanelViewport;
            Assert.That(panel.x, Is.GreaterThanOrEqualTo(1280f),
                $"The gate measures the footprint at the reference resolution; the panel resolved " +
                $"{panel} from a {Screen.width}x{Screen.height} viewport.");

            // A stage larger than the footprint in both directions, so a face that does not fit is
            // measured at the size it actually wants instead of being clamped by what it is put in -
            // which is the whole point: the report has to be able to say how much too tall a face is.
            var stage = new VisualElement { name = "gd-footprint-stage" };
            stage.style.position = Position.Absolute;
            stage.style.left = 0f;
            stage.style.top = 0f;
            stage.style.width = CardFootprint.Width + 240f;
            stage.style.height = CardFootprint.Height + 480f;
            stage.style.flexDirection = FlexDirection.Column;
            stage.style.alignItems = Align.FlexStart;
            stage.style.justifyContent = Justify.FlexStart;
            stage.pickingMode = PickingMode.Ignore;
            hud.RootElement.Add(stage);

            var failures = new List<string>();
            var measured = 0;
            try
            {
                foreach (var definition in definitions)
                {
                    stage.Clear();
                    var card = CurrentRoundCards.DeckCard(
                        new CardInstanceId($"footprint-{definition.Id.Value}"), definition);
                    var face = DirectorHandsUnityView.BuildStandaloneCard(card);
                    stage.Add(face);

                    // One frame for the panel to lay the new face out; before it, every resolved
                    // style on it is the previous card's or NaN.
                    yield return null;

                    measured++;
                    failures.AddRange(Inspect(definition, card, face));
                }
            }
            finally
            {
                stage.RemoveFromHierarchy();
            }

            Assert.That(measured, Is.EqualTo(definitions.Count),
                $"Every one of {heroDisplayName}'s verified faces is measured.");
            Assert.That(failures, Is.Empty,
                $"{heroDisplayName}'s faces must all be exactly {CardFootprint.Width} by " +
                $"{CardFootprint.Height} and show their content whole:" + Environment.NewLine +
                string.Join(Environment.NewLine, failures));
        }

        /// <summary>
        /// One laid-out face, against the contract. Returns a line per violation rather than
        /// asserting, so one run reports the whole set.
        /// </summary>
        private static IEnumerable<string> Inspect(
            CardDefinition definition, TrayCardViewModel card, VisualElement face)
        {
            var name = $"{definition.DisplayName} ({definition.Id.Value})";
            var box = face.resolvedStyle;
            if (float.IsNaN(box.width) || float.IsNaN(box.height))
            {
                yield return $"{name}: has no resolved layout at all.";
                yield break;
            }

            // Milestone 8.25c: to within the panel's own pixel grid. A face is declared in reference
            // units and drawn on a scaled panel, so its laid-out size is the declared one snapped to
            // the nearer device pixel - see `UiPlayer.PixelSnapping`. The gate is still an exact-size
            // gate; what is allowed is the snapping, not a face that wanted a different size.
            var snapping = UiPlayer.PixelSnapping();
            if (Mathf.Abs(box.width - CardFootprint.Width) > snapping)
            {
                yield return $"{name}: {box.width} wide, and the standard footprint is " +
                    $"{CardFootprint.Width}.";
            }

            if (Mathf.Abs(box.height - CardFootprint.Height) > snapping)
            {
                yield return $"{name}: {box.height} tall, and the standard footprint is " +
                    $"{CardFootprint.Height} - a difference of " +
                    $"{box.height - CardFootprint.Height}.";
            }

            var bounds = face.worldBound;
            var previousBottom = float.NegativeInfinity;
            foreach (var bandName in BandsTopToBottom)
            {
                var band = Descendants(face).FirstOrDefault(element => element.name == bandName);
                if (band == null)
                {
                    yield return $"{name}: draws no {bandName}.";
                    continue;
                }

                var region = band.worldBound;
                if (region.height <= 0f)
                {
                    yield return $"{name}: its {bandName} is drawn with no height.";
                }

                if (region.yMin + EdgeTolerance < previousBottom)
                {
                    yield return $"{name}: its {bandName} overlaps the region above it by " +
                        $"{previousBottom - region.yMin}.";
                }

                if (region.yMax > bounds.yMax + EdgeTolerance ||
                    region.yMin < bounds.yMin - EdgeTolerance)
                {
                    yield return $"{name}: its {bandName} is outside the card's own bounds.";
                }

                previousBottom = region.yMax;
            }

            foreach (var violation in InspectDrawnText(name, card, face, bounds))
            {
                yield return violation;
            }
        }

        /// <summary>
        /// Every word the face draws, held to being inside the card and to having the room the text
        /// itself needs at the width it was given.
        /// </summary>
        private static IEnumerable<string> InspectDrawnText(
            string name, TrayCardViewModel card, VisualElement face, Rect bounds)
        {
            var printed = card.Face.PrintedTextKind == CardSourceContentKind.Transcribed
                ? card.Face.PrintedText
                : card.Face.EmptyTextLabel;
            var required = new[]
            {
                card.DisplayName.ToUpperInvariant(),
                card.Face.PrimaryActionLabel,
                card.HistoryLabel,
                printed
            };

            var labels = Descendants(face)
                .OfType<Label>()
                .Where(label => !string.IsNullOrEmpty(label.text))
                .ToArray();

            foreach (var expected in required.Where(text => !string.IsNullOrEmpty(text)))
            {
                if (labels.All(label => label.text != expected))
                {
                    yield return $"{name}: does not draw \"{Shorten(expected)}\" at all.";
                }
            }

            foreach (var label in labels)
            {
                var drawn = label.worldBound;
                if (float.IsNaN(drawn.height))
                {
                    yield return $"{name}: \"{Shorten(label.text)}\" has no resolved layout.";
                    continue;
                }

                if (drawn.yMax > bounds.yMax + EdgeTolerance ||
                    drawn.yMin < bounds.yMin - EdgeTolerance ||
                    drawn.xMax > bounds.xMax + EdgeTolerance ||
                    drawn.xMin < bounds.xMin - EdgeTolerance)
                {
                    yield return $"{name}: \"{Shorten(label.text)}\" is drawn outside the card.";
                    continue;
                }

                // What the words need at the width they were actually given. A label squeezed by a
                // card too short for its sentence keeps reporting the whole text while drawing part
                // of it, so the text is measured rather than trusted.
                var width = label.resolvedStyle.width;
                if (float.IsNaN(width) || width <= 0f)
                {
                    yield return $"{name}: \"{Shorten(label.text)}\" is drawn with no width.";
                    continue;
                }

                var needed = label.MeasureTextSize(
                    label.text, width, VisualElement.MeasureMode.Exactly,
                    0f, VisualElement.MeasureMode.Undefined);
                if (drawn.height + EdgeTolerance < needed.y)
                {
                    yield return $"{name}: \"{Shorten(label.text)}\" is given {drawn.height} " +
                        $"pixels of height and needs {needed.y} at {width} wide - it is truncated.";
                }
            }
        }

        private static string Shorten(string text) =>
            text.Length <= 42 ? text : text.Substring(0, 39) + "...";

        private static IEnumerable<VisualElement> Descendants(VisualElement element)
        {
            foreach (var child in element.Children())
            {
                yield return child;
                foreach (var descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
