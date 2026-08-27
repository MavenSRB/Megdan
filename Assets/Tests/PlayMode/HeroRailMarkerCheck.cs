using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// The shared reading of a held marker on a hero's own rail, and the shared measurement of the
    /// rail that carries it. Milestone 8.23a.
    ///
    /// <para>It is the marker twin of <see cref="HeroRailCoinCheck"/> and exists for the same reason:
    /// two journeys prove two halves of one reading - that it appears where the marker is and that it
    /// ends when the marker returns - and two readings of the same rail that had drifted apart would
    /// let one journey pass on a row the other could not find.</para>
    /// </summary>
    internal static class HeroRailMarkerCheck
    {
        /// <summary>
        /// Every marker row currently drawn on the rail of <paramref name="side"/>, as the text a
        /// player reads: the marker's name followed by each value chip it carries.
        ///
        /// <para>Read off the drawn Labels rather than off the view model, because what this milestone
        /// claims is that a player can see it. A model that held the right numbers behind a row with
        /// <c>display: none</c> would satisfy every assertion about the model and none about the
        /// screen, which is the failure the whole player-path gate exists to catch.</para>
        /// </summary>
        internal static IReadOnlyList<string> MarkerReadingsOn(
            ConfiguredMatchHudRenderer hud, TeamSide side) =>
            MarkerReadingsOn(HeroRailCoinCheck.Rail(hud, side));

        internal static IReadOnlyList<string> MarkerReadingsOn(VisualElement rail) =>
            UiPlayer.Descendants(rail)
                .Where(element => element.name == "gd-hero-marker" && UiPlayer.IsShown(element))
                .Select(row => string.Join(" ", UiPlayer.Descendants(row)
                    .OfType<Label>()
                    .Where(UiPlayer.IsShown)
                    .Select(label => label.text.Trim())
                    .Where(text => text.Length > 0)))
                .ToArray();

        /// <summary>
        /// Asserts that the rail on <paramref name="side"/> draws exactly the readings named, in order.
        /// </summary>
        internal static void AssertReadings(
            ConfiguredMatchHudRenderer hud, TeamSide side, string where, params string[] expected)
        {
            var drawn = MarkerReadingsOn(hud, side);
            Assert.That(drawn.Count, Is.EqualTo(expected.Length),
                $"The {side} rail draws {drawn.Count} marker rows at {where} where {expected.Length} " +
                    $"were expected: [{string.Join(" | ", drawn)}]");
            for (var index = 0; index < expected.Length; index++)
            {
                Assert.That(drawn[index], Is.EqualTo(expected[index]),
                    $"The {side} rail's marker row {index} reads wrong at {where}.");
            }
        }

        /// <summary>
        /// The reading Tigerclaw's Tier II Poisoned Dagger leaves on the hero it names: the marker's
        /// own name and the three channels its printed sentence lowers by one.
        ///
        /// <para>Written as a literal rather than composed from the definition, deliberately. A check
        /// that built its expectation from the same content the screen was built from would agree with
        /// the screen however wrong both were; this is the printed sentence - "The hero with a poison
        /// marker has -1 Initiative, -1 Attack and -1 Defense" - transcribed once, in the order the
        /// card prints it.</para>
        /// </summary>
        internal const string PoisonOneReading = "POISON INI -1 ATK -1 DEF -1";

        /// <summary>Poisoned Dart's, which is the same sentence at twice the strength.</summary>
        internal const string PoisonTwoReading = "POISON INI -2 ATK -2 DEF -2";

        /// <summary>
        /// The height one rail would have to fit inside for rulebook p.6's three-per-side column to
        /// hold three of them: a third of the hero-rail rect the renderer computes, less the 8-pixel
        /// gap a rail already carries under it.
        ///
        /// <para><b>Milestone 8.23a measured this, and a stacked column of three does not fit at the
        /// validated viewport.</b> At 1920x1080 the rail rect is about 491 - the playfield height,
        /// after the status strip's 56 and the card tray's 360 - so the budget is about 156, and a
        /// rail holding a marker draws about 239. The recomposition removed a row and widened the
        /// rail, and the remaining gap is roughly a third again.</para>
        ///
        /// <para><b>That was a fact about that arrangement, not about the roster</b>, and milestone
        /// 8.25 changed the arrangement on the user's 2026-08-26 decision rather than the rail: the
        /// rail columns now run from the status strip to the compact reserved tray, so the reference
        /// rect is about 878 units and the budget about 285 against the same 239. The formula is
        /// unchanged and is now met; it is asserted rather than only recorded, by
        /// <c>TigerclawProgressedUiJourneyPlayModeTests</c>.</para>
        /// </summary>
        internal static float ThreeUpBudget(float railRectHeight) => (railRectHeight / 3f) - 8f;

        /// <summary>
        /// The height a rail holding a marker actually draws in this build, as a ceiling.
        ///
        /// <para>A regression guard rather than a target: <see cref="ThreeUpBudget"/> is the target and
        /// this build meets it, while this ceiling independently keeps a rail from growing back past
        /// what the recomposition achieved. Measured at 239 and recorded at 245 to leave the runtime
        /// theme's own rounding alone.</para>
        /// </summary>
        internal const float RecordedRailCeiling = 245f;

        /// <summary>
        /// How many independently laid-out rows a hero rail stacks: the header, the item strip, a held
        /// marker's row, the four turn slots, the discard caption and the discard pile.
        ///
        /// <para>Milestone 8.25c. On a scaled panel each of those is snapped to the nearer device
        /// pixel, so a whole rail's measured height can differ from its declared one by about this
        /// many pixels expressed in reference units. It is stated here because two fixtures compare a
        /// rail's height against a number recorded in reference units; see
        /// <see cref="UiPlayer.PixelSnapping"/>.</para>
        /// </summary>
        internal const int RailRowCount = 8;
    }
}
