using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
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
    }
}
