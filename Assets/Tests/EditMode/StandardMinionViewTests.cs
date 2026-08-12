using System;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class StandardMinionViewTests
    {
        [Test]
        public void ConfiguredIdentity_SurvivesLossOfTransientSetupState()
        {
            var minionObject = new GameObject("Reloaded Minion");
            try
            {
                var state = CreateState("reload-safe-minion");
                var minion = minionObject.AddComponent<StandardMinionView>();
                minion.Configure(state, HexWorldLayout.DefaultOuterRadius);

                typeof(StandardMinionView)
                    .GetField("setupState", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(minion, null);

                Assert.That(minion.SetupState, Is.Null,
                    "The regression must exercise the transient setup-state loss seen during editor backup restoration.");
                Assert.That(minion.BoundMinionId, Is.EqualTo(state.MinionId),
                    "Serialized binding identity must survive independently of transient setup state.");
            }
            finally
            {
                Object.DestroyImmediate(minionObject);
            }
        }

        [Test]
        public void PassiveBoardView_RebindsReplacementBeforeRefreshingProjectedMinions()
        {
            var originalObject = new GameObject("Original Minion");
            var replacementObject = new GameObject("Replacement Minion");
            var tileObject = new GameObject("Replacement Tile");
            var material = new Material(Shader.Find("Standard"));
            try
            {
                var state = CreateState("replaceable-minion");
                var original = originalObject.AddComponent<StandardMinionView>();
                original.Configure(state, HexWorldLayout.DefaultOuterRadius);
                var tile = tileObject.AddComponent<HexTileView>();
                tile.Configure(state.Coordinate, HexWorldLayout.DefaultOuterRadius);
                var view = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(),
                    new[] { tile },
                    material,
                    new[] { original });
                var model = new BoardInteractionViewModel(
                    1,
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroOccupancy>(),
                    new[] { state },
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroId>(),
                    new[] { state.MinionId },
                    default);
                view.Render(model);

                Object.DestroyImmediate(originalObject);
                var replacement = replacementObject.AddComponent<StandardMinionView>();
                replacement.Configure(state, HexWorldLayout.DefaultOuterRadius);
                var selector = replacementObject.AddComponent<StandardMinionPointerSelector>();
                MinionId submitted = default;
                view.BindIntentHandlers(_ => false, id => { submitted = id; return true; }, _ => false);

                Assert.DoesNotThrow(() => view.ReplaceMinionViews(new[] { replacement }),
                    "Replacing stale cached components must reconcile the current projection atomically.");
                selector.Configure(view);

                Assert.That(view.MinionViews, Is.EquivalentTo(new[] { replacement }));
                Assert.That(replacementObject.activeSelf, Is.True);
                Assert.That(selector.TrySelectFromPointerRay(
                    new Ray(replacement.transform.position + (Vector3.up * 10f), Vector3.down)), Is.True);
                Assert.That(submitted, Is.EqualTo(state.MinionId));
            }
            finally
            {
                Object.DestroyImmediate(material);
                if (originalObject != null) Object.DestroyImmediate(originalObject);
                Object.DestroyImmediate(replacementObject);
                Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void PassiveBoardView_UsesProjectedMinionIdentityForCuePointerAndRemoval()
        {
            var minionObject = new GameObject("Projected Minion");
            var tileObject = new GameObject("Projected Minion Tile");
            var material = new Material(Shader.Find("Standard"));
            try
            {
                var state = new QuickGameMinionSetupState(
                    new MinionId("blue-target"),
                    new TeamId("blue-team"),
                    TeamSide.Blue,
                    StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(new MapId("test-map"), new SpawnPointId("blue-target-spawn")),
                    new ConfiguredSpaceReference(new MapId("test-map"), new SpaceId("q1_r0")),
                    new HexCoordinate(1, 0));
                var minion = minionObject.AddComponent<StandardMinionView>();
                minion.Configure(state, HexWorldLayout.DefaultOuterRadius);
                var selector = minionObject.AddComponent<StandardMinionPointerSelector>();
                var tile = tileObject.AddComponent<HexTileView>();
                tile.Configure(state.Coordinate, HexWorldLayout.DefaultOuterRadius);
                var view = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(),
                    new[] { tile },
                    material,
                    new[] { minion });
                MinionId submitted = default;
                view.BindIntentHandlers(_ => false, id => { submitted = id; return true; }, _ => false);
                selector.Configure(view);

                view.Render(new BoardInteractionViewModel(
                    1,
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroOccupancy>(),
                    new[] { state },
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroId>(),
                    new[] { state.MinionId },
                    default));

                Assert.That(tile.IsHighlighted, Is.True);
                Assert.That(selector.TrySelectFromPointerRay(
                    new Ray(minion.transform.position + (Vector3.up * 10f), Vector3.down)), Is.True);
                Assert.That(submitted, Is.EqualTo(state.MinionId));

                view.Render(new BoardInteractionViewModel(
                    2,
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>(),
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroId>(),
                    Array.Empty<MinionId>(),
                    default));

                Assert.That(minionObject.activeSelf, Is.False);
                Assert.That(tile.IsHighlighted, Is.False);

                view.Render(new BoardInteractionViewModel(
                    3,
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroOccupancy>(),
                    new[] { state },
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroId>(),
                    Array.Empty<MinionId>(),
                    default));

                Assert.That(minionObject.activeSelf, Is.True,
                    "A later authoritative projection such as Undo/restore must re-enable the retained view.");
                Assert.That(tile.IsHighlighted, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(minionObject);
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

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

        private static QuickGameMinionSetupState CreateState(string id) =>
            new QuickGameMinionSetupState(
                new MinionId(id),
                new TeamId("blue-team"),
                TeamSide.Blue,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(new MapId("test-map"), new SpawnPointId($"{id}-spawn")),
                new ConfiguredSpaceReference(new MapId("test-map"), new SpaceId("q1_r0")),
                new HexCoordinate(1, 0));
    }
}
