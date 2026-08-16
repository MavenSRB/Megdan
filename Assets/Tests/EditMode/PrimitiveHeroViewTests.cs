using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class PrimitiveHeroViewTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void Configure_PlacesViewFromAuthoritativeOccupancy()
        {
            var gameObject = new GameObject("Primitive Hero Test");

            try
            {
                var view = gameObject.AddComponent<PrimitiveHeroView>();
                var occupancy = new HeroOccupancy(new HeroId("test-hero"), new HexCoordinate(2, -1));

                view.Configure(occupancy, 1.25f);

                var expectedPosition = HexWorldLayout.ToWorld(occupancy.Coordinate, 1.25f) +
                    (Vector3.up * view.VerticalOffset);
                Assert.That(view.Occupancy, Is.EqualTo(occupancy));
                Assert.That(Vector3.Distance(view.transform.localPosition, expectedPosition), Is.LessThan(Tolerance));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void EnsurePrimitiveVisual_CreatesVisiblePlaceholderMesh()
        {
            var gameObject = new GameObject("Primitive Hero Test");

            try
            {
                var view = gameObject.AddComponent<PrimitiveHeroView>();

                view.EnsurePrimitiveVisual();

                Assert.That(gameObject.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
                Assert.That(gameObject.GetComponent<MeshRenderer>().sharedMaterial, Is.Not.Null);
                Assert.That(gameObject.GetComponent<MeshRenderer>().enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Milestone 6.5c. An effect choice can name a hero as its target, so the hero view carries
        /// the same marker the minion view does - the shared construction, the shared marker size,
        /// and the two colors that separate a selection from the control being pointed at - hung
        /// clear of the prism rather than at the minion's sprite offset.
        /// </summary>
        [Test]
        public void SetChoiceCue_ShowsTheSharedMarkerAboveTheHeroAndClearsIt()
        {
            var gameObject = new GameObject("Primitive Hero Test");

            try
            {
                var view = gameObject.AddComponent<PrimitiveHeroView>();
                Assert.That(view.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.None));
                Assert.That(gameObject.GetComponentsInChildren<TextMesh>(true), Is.Empty,
                    "A hero carrying no cue builds no cue objects at all.");

                view.SetChoiceCue(MinionChoiceCueKind.Highlighted);

                var cue = gameObject.GetComponentsInChildren<TextMesh>(true)
                    .Single(value => value.text == BoardChoiceCue.Marker);
                Assert.That(view.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.Highlighted));
                Assert.That(cue.characterSize, Is.EqualTo(BoardChoiceCue.MarkerCharacterSize).Within(Tolerance));
                AssertColor(cue.color, BoardChoiceCue.HighlightedColor);
                Assert.That(cue.gameObject.activeSelf, Is.True);
                Assert.That(cue.transform.localPosition.y, Is.GreaterThan(PrimitiveHeroView.DefaultHeight),
                    "The marker hangs above the prism rather than inside it.");
                Assert.That(cue.transform.localScale.x, Is.EqualTo(1f).Within(Tolerance),
                    "An upright mesh needs none of the minion sprite's horizontal inversion.");

                view.SetChoiceCue(MinionChoiceCueKind.Selected);
                AssertColor(cue.color, BoardChoiceCue.SelectedColor);

                view.SetChoiceCue(MinionChoiceCueKind.None);
                Assert.That(view.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.None));
                Assert.That(cue.gameObject.activeSelf, Is.False,
                    "Clearing hides the cue rather than destroying and rebuilding it.");

                Assert.That(
                    () => view.SetChoiceCue((MinionChoiceCueKind)7),
                    Throws.TypeOf<System.ArgumentOutOfRangeException>());
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Per channel and with tolerance, because a color assigned to a `TextMesh` comes back
        /// quantised to eight bits per channel; asserting exact equality would test the quantisation
        /// rather than the cue.
        /// </summary>
        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(0.01f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(0.01f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(0.01f));
        }
    }
}
