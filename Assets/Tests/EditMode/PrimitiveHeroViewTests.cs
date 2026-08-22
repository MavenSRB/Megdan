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
        /// Milestone 8.11b. A hero this project has no prepared token for keeps the generated prism,
        /// which is the tracked checkout's ordinary state rather than a fault.
        /// </summary>
        [Test]
        public void WithNoPreparedToken_TheHeroKeepsTheGeneratedPrismAndItsOwnPointerRadius()
        {
            var gameObject = new GameObject("Primitive Hero Test");

            try
            {
                var view = gameObject.AddComponent<PrimitiveHeroView>();

                view.EnsureVisual();

                Assert.That(view.HasTokenVisual, Is.False,
                    "A view with no content identity resolves no face, and never faults over one.");
                Assert.That(gameObject.GetComponent<MeshFilter>().sharedMesh, Is.Not.Null);
                Assert.That(gameObject.GetComponent<MeshFilter>().sharedMesh.subMeshCount, Is.EqualTo(1));
                Assert.That(gameObject.GetComponent<MeshRenderer>().sharedMaterials, Has.Length.EqualTo(1));
                Assert.That(gameObject.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.Null);
                Assert.That(view.SelectionRadius,
                    Is.EqualTo(PrimitiveHeroView.DefaultBaseRadius).Within(Tolerance));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Milestone 8.11b. Which face a hero would use is decided by projected content identity and
        /// authoritative team, and by nothing else - the resource is addressed the same way
        /// <see cref="StandardMinionArtCatalog"/> addresses a minion's.
        /// </summary>
        [Test]
        public void AHeroTokenIsAddressedByContentIdentityAndTeam_AndAbsenceIsAnOrdinaryAnswer()
        {
            var wasp = new GuardsDigital.Core.Setup.HeroContentId("wasp");
            Assert.That(
                HeroTokenArtwork.ResourcePath(wasp, GuardsDigital.Core.Participants.TeamSide.Red),
                Is.EqualTo("Private/Heroes/wasp-red"));
            Assert.That(
                HeroTokenArtwork.ResourcePath(wasp, GuardsDigital.Core.Participants.TeamSide.Blue),
                Is.EqualTo("Private/Heroes/wasp-blue"));
            Assert.That(
                HeroTokenArtwork.ResourcePath(wasp, GuardsDigital.Core.Participants.TeamSide.Red),
                Is.Not.EqualTo(
                    HeroTokenArtwork.ResourcePath(wasp, GuardsDigital.Core.Participants.TeamSide.Blue)),
                "so the two teams can never resolve one face.");
            Assert.That(
                HeroTokenArtwork.ResourcePath(
                    new GuardsDigital.Core.Setup.HeroContentId("tigerclaw"),
                    GuardsDigital.Core.Participants.TeamSide.Red),
                Is.EqualTo("Private/Heroes/tigerclaw-red"),
                "and a hero this project has not reached yet needs no new rule to be addressed.");

            Assert.That(
                HeroTokenArtwork.TryLoadTexture(
                    new GuardsDigital.Core.Setup.HeroContentId("no-such-hero"),
                    GuardsDigital.Core.Participants.TeamSide.Red,
                    out var absent),
                Is.False,
                "An absent face answers false rather than throwing: it is the tracked state.");
            Assert.That(absent, Is.Null);

            // The team is stated in this project's own colour, from the side rather than the picture,
            // and it is the same colour the generated prism has always carried.
            Assert.That(
                HeroTokenArtwork.TeamRim(GuardsDigital.Core.Participants.TeamSide.Red),
                Is.Not.EqualTo(HeroTokenArtwork.TeamRim(GuardsDigital.Core.Participants.TeamSide.Blue)));
            Assert.That(HeroTokenArtwork.FaceRadius,
                Is.LessThan(HeroTokenArtwork.TokenOuterRadius),
                "The rim stands outside the face rather than over it.");
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

                // The regression this fixture exists for. The board is the XZ plane and the camera
                // looks down it, so a cue is visible only when it lies flat facing up; parented
                // straight to this upright view it faced sideways and rendered as an invisible
                // sliver. Asserting the facing rather than a local offset is what makes that
                // observable at all - the first version was in the right place and unseeable.
                Assert.That(Vector3.Distance(cue.transform.forward, Vector3.up), Is.LessThan(0.001f),
                    "The marker faces the camera that looks down the board plane.");
                // Milestone 8.11a: up-screen is what the framing camera calls up, which is world +Z
                // until the official artwork puts the camera on the board's own axes.
                Assert.That(
                    Vector3.Distance(cue.transform.up, BoardChoiceCue.ScreenUpInWorld),
                    Is.LessThan(0.001f),
                    "And its own up points up-screen, so the marker is not upside down.");
                Assert.That(cue.transform.position.z,
                    Is.GreaterThan(view.transform.position.z + 0.5f),
                    "It sits above the hero on screen, which is +Z on this board.");
                Assert.That(cue.transform.position.y, Is.GreaterThan(0f),
                    "And above the board plane, so no tile draws over it.");

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
