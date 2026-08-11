using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class StandardMinionViewTests
    {
        [TestCase(TeamSide.Red, StandardMinionType.Heavy, "Minions/red-heavy")]
        [TestCase(TeamSide.Red, StandardMinionType.Melee, "Minions/red-melee")]
        [TestCase(TeamSide.Red, StandardMinionType.Ranged, "Minions/red-ranged")]
        [TestCase(TeamSide.Blue, StandardMinionType.Heavy, "Minions/blue-heavy")]
        [TestCase(TeamSide.Blue, StandardMinionType.Melee, "Minions/blue-melee")]
        [TestCase(TeamSide.Blue, StandardMinionType.Ranged, "Minions/blue-ranged")]
        public void ArtCatalog_SelectsReviewedCircularArtFromAuthoritativeTeamAndType(
            TeamSide teamSide,
            StandardMinionType minionType,
            string expectedResourcePath)
        {
            Assert.That(StandardMinionArtCatalog.GetResourcePath(teamSide, minionType), Is.EqualTo(expectedResourcePath));
            Assert.That(StandardMinionArtCatalog.Load(teamSide, minionType), Is.Not.Null);
        }

        [Test]
        public void Configure_ProjectsStableAuthoritativeIdentityAndPlacementWithoutReadingArtMetadata()
        {
            var gameObject = new GameObject("Minion View Test");
            try
            {
                var view = gameObject.AddComponent<StandardMinionView>();
                var state = new QuickGameMinionSetupState(
                    new MinionId("blue-melee-01"),
                    new TeamId("blue-team"),
                    TeamSide.Blue,
                    StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(new MapId("official-single-lane"), new SpawnPointId("blue-melee-minion-03")),
                    new ConfiguredSpaceReference(new MapId("official-single-lane"), new SpaceId("q-3_r2")),
                    new HexCoordinate(-3, 2));

                view.Configure(state, HexWorldLayout.DefaultOuterRadius);

                Assert.That(view.SetupState, Is.SameAs(state));
                Assert.That(view.SetupState.TeamSide, Is.EqualTo(TeamSide.Blue));
                Assert.That(view.SetupState.MinionType, Is.EqualTo(StandardMinionType.Melee));
                Assert.That(view.GetComponent<SpriteRenderer>().sprite.name, Is.EqualTo("blue-melee"));
                Assert.That(view.transform.localPosition,
                    Is.EqualTo(HexWorldLayout.ToWorld(state.Coordinate) + (Vector3.up * StandardMinionView.DefaultVerticalOffset)));
                Assert.That(Vector3.Angle(view.transform.localRotation * Vector3.up, Vector3.forward),
                    Is.LessThan(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
