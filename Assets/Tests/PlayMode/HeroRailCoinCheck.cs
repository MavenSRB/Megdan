using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.21a. The live measurements a journey makes of the coin balance on a hero rail.
    ///
    /// <para>It is shared because two fixtures need the same three questions answered - what a rail
    /// currently reads, where a rail's own box is, and whether the count is standing clear of
    /// everything else on that rail - and two copies of the geometry would be two chances for one of
    /// them to measure something slightly different from the other.</para>
    ///
    /// <para>Everything here reads <em>laid-out</em> bounds rather than declared styles, for the reason
    /// milestone 8.20c's footprint check does: a declared size is a preference a container can
    /// overrule, and the whole claim this milestone makes is about what a player's screen ends up
    /// showing.</para>
    /// </summary>
    internal static class HeroRailCoinCheck
    {
        /// <summary>
        /// A pixel of slack for edges that are accumulated float sums. An overlap worth reporting is
        /// text sitting on text, not two boxes meeting.
        /// </summary>
        private const float EdgeTolerance = 0.5f;

        /// <summary>
        /// Every other surface on a rail the count is forbidden to overlap or clip, by the milestone's
        /// own acceptance rule. A rail that stops drawing one of them fails as a missing surface rather
        /// than quietly shrinking what is measured, except for the two that genuinely come and go -
        /// the Ultimate marker appears only once p.16's card is faceup, and the current-card marker's
        /// slot is always reserved but only occupied part of a round.
        /// </summary>
        private static readonly string[] AlwaysDrawnNeighbours =
        {
            "gd-hero-icon",
            "gd-bonus-strip",
            "gd-current-card-slot",
            "gd-turn-slots-caption",
            "gd-turn-slots",
            "gd-discard"
        };

        private static readonly string[] SometimesDrawnNeighbours =
        {
            "gd-ultimate"
        };

        /// <summary>Every rail currently drawn on one edge of the screen, outermost first.</summary>
        internal static IReadOnlyList<VisualElement> Rails(
            ConfiguredMatchHudRenderer hud, TeamSide side)
        {
            var columnName = side == TeamSide.Red ? "gd-hero-rail-red" : "gd-hero-rail-blue";
            var column = UiPlayer.Descendants(hud.RootElement)
                .Single(element => element.name == columnName);
            var rails = UiPlayer.Descendants(column)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(rails, Is.Not.Empty, $"No hero rail is drawn on the {side} edge.");
            return rails;
        }

        /// <summary>
        /// The one rail on <paramref name="side"/>, for a staging that seats one hero there.
        /// </summary>
        internal static VisualElement Rail(ConfiguredMatchHudRenderer hud, TeamSide side)
        {
            var rails = Rails(hud, side);
            // Counted rather than constrained: `Has.Count` does not work on an array, whose
            // `IList<T>.Count` is an explicit implementation NUnit cannot see.
            Assert.That(rails.Count, Is.EqualTo(1),
                $"The {side} edge is drawing {rails.Count} rails, so naming one of them is ambiguous.");
            return rails[0];
        }

        /// <summary>
        /// The amount the rail on <paramref name="side"/> is currently showing, as drawn.
        /// </summary>
        internal static string CoinAmountOn(ConfiguredMatchHudRenderer hud, TeamSide side) =>
            CoinAmountOn(Rail(hud, side));

        internal static string CoinAmountOn(VisualElement rail)
        {
            var readout = UiPlayer.Descendants(rail)
                .Single(element => element.name == "gd-hero-coins");
            Assert.That(UiPlayer.IsShown(readout), Is.True,
                "The balance is public match information and is never hidden.");
            var amount = UiPlayer.Descendants(readout)
                .OfType<Label>()
                .Single(label => label.name == "gd-hero-coins-amount");
            return amount.text;
        }

        /// <summary>
        /// The rail's own laid-out box, net of the two-pixel top border the rail wears while the tray
        /// is showing its hero.
        ///
        /// <para>That border is subtracted because it is the one part of a rail's box that a turn is
        /// meant to change: the tray follows whichever hero the match is waiting on, so a rail gains
        /// and loses the marker as the round moves, and a comparison across a played turn would report
        /// the marker as a size change. Everything else in the box - the position, the width, and the
        /// height its rows add up to - is what this milestone may not move.</para>
        /// </summary>
        internal static Rect BoxOf(ConfiguredMatchHudRenderer hud, TeamSide side)
        {
            var rail = Rail(hud, side);
            var box = rail.worldBound;
            return new Rect(box.x, box.y, box.width, box.height - rail.resolvedStyle.borderTopWidth);
        }

        /// <summary>
        /// The count stands inside its rail and clear of every other surface on it, and the line it
        /// shares is no taller than the caption alone would make it.
        /// </summary>
        internal static void AssertTheCountStandsClear(
            ConfiguredMatchHudRenderer hud, string where)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                foreach (var rail in Rails(hud, side))
                {
                    AssertOneRailsCountStandsClear(rail, side, where);
                }
            }
        }

        private static void AssertOneRailsCountStandsClear(
            VisualElement rail, TeamSide side, string where)
        {
            var railBox = rail.worldBound;
            Assert.That(
                railBox.width,
                Is.EqualTo(ConfiguredMatchHudRenderer.HeroRailWidth).Within(EdgeTolerance),
                $"The {side} rail is {railBox.width} wide at {where} rather than the width every " +
                "region rect is still computed from. A rail that grew to hold the count would be " +
                "the one thing this milestone may not do.");

            var readout = UiPlayer.Descendants(rail)
                .Single(element => element.name == "gd-hero-coins");
            var box = readout.worldBound;
            Assert.That(box.width, Is.GreaterThan(0f).And.LessThan(railBox.width),
                $"The {side} count at {where} has no drawable width, or takes the whole rail.");
            Assert.That(box.xMin, Is.GreaterThanOrEqualTo(railBox.xMin - EdgeTolerance),
                $"The {side} count is cut off by the rail's left edge at {where}.");
            Assert.That(box.xMax, Is.LessThanOrEqualTo(railBox.xMax + EdgeTolerance),
                $"The {side} count is cut off by the rail's right edge at {where}.");
            Assert.That(box.yMin, Is.GreaterThanOrEqualTo(railBox.yMin - EdgeTolerance));
            Assert.That(box.yMax, Is.LessThanOrEqualTo(railBox.yMax + EdgeTolerance));

            foreach (var label in UiPlayer.Descendants(readout).OfType<Label>())
            {
                var text = label.worldBound;
                Assert.That(text.xMin, Is.GreaterThanOrEqualTo(box.xMin - EdgeTolerance),
                    $"'{label.text}' is clipped by the {side} readout's own box at {where}.");
                Assert.That(text.xMax, Is.LessThanOrEqualTo(box.xMax + EdgeTolerance),
                    $"'{label.text}' is clipped by the {side} readout's own box at {where}.");
            }

            // The line the count shares is the caption's own line, and it is no taller for the
            // sharing: a count that raised this row would push every row under it down, which is the
            // same thing as making the rail taller.
            //
            // Margin boxes are what the comparison is between, because a margin box is what a child
            // contributes to the column above it - the caption's was the rail's whole spend on this
            // line before the row existed, and the row's is the rail's whole spend on it now.
            var row = UiPlayer.Descendants(rail)
                .Single(element => element.name == "gd-turn-slots-caption-row");
            var caption = UiPlayer.Descendants(row)
                .Single(element => element.name == "gd-turn-slots-caption");
            Assert.That(
                MarginBoxHeight(row),
                Is.EqualTo(MarginBoxHeight(caption)).Within(EdgeTolerance),
                $"The {side} caption's line costs the rail {MarginBoxHeight(row)} at {where} where " +
                    $"the caption alone cost it {MarginBoxHeight(caption)}, so the count raised it.");

            foreach (var name in AlwaysDrawnNeighbours)
            {
                var neighbours = UiPlayer.Descendants(rail)
                    .Where(element => element.name == name)
                    .ToArray();
                Assert.That(neighbours, Is.Not.Empty,
                    $"The {side} rail draws no '{name}' at {where}, so nothing was " +
                        "measured against it.");
                foreach (var neighbour in neighbours)
                {
                    AssertClearOf(box, neighbour, name, side, where);
                }
            }

            foreach (var name in SometimesDrawnNeighbours)
            {
                foreach (var neighbour in UiPlayer.Descendants(rail)
                    .Where(element => element.name == name))
                {
                    AssertClearOf(box, neighbour, name, side, where);
                }
            }

            // The hero's own name and level are the two labels in the header with no name of their
            // own, so they are measured as the header's labels rather than by a selector.
            foreach (var label in UiPlayer.Descendants(rail)
                .OfType<Label>()
                .Where(label => !UiPlayer.Descendants(readout).Contains(label)))
            {
                AssertClearOf(box, label, "'" + label.text + "'", side, where);
            }
        }

        /// <summary>
        /// What one element costs the column above it: its border box plus its own margins.
        /// </summary>
        private static float MarginBoxHeight(VisualElement element) =>
            element.worldBound.height +
            element.resolvedStyle.marginTop +
            element.resolvedStyle.marginBottom;

        private static void AssertClearOf(
            Rect count, VisualElement neighbour, string name, TeamSide side, string where)
        {
            if (!UiPlayer.IsShown(neighbour))
            {
                return;
            }

            var other = neighbour.worldBound;
            var overlapsHorizontally =
                count.xMin < other.xMax - EdgeTolerance && other.xMin < count.xMax - EdgeTolerance;
            var overlapsVertically =
                count.yMin < other.yMax - EdgeTolerance && other.yMin < count.yMax - EdgeTolerance;
            Assert.That(overlapsHorizontally && overlapsVertically, Is.False,
                $"The {side} rail's coin count {count} overlaps {name} {other} at {where}.");
        }
    }
}
