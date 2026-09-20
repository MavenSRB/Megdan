using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class HexCornerHighlightPlayModeTests
    {
        [UnityTest]
        public IEnumerator MovementHeroAndMinionChoices_HoverClearAndRebuildWithoutGeometryOrResourceGrowth()
        {
            var owned = new List<UnityEngine.Object>();
            HexCornerHighlightTemplate template = null;
            try
            {
                var movement = Tile(new HexCoordinate(0, 0), owned);
                var heroTile = Tile(new HexCoordinate(1, 0), owned);
                var minionTile = Tile(new HexCoordinate(2, 0), owned);
                var heroId = new HeroId("attack-hero");
                var minion = CreateMinion("attack-minion", minionTile.Coordinate);
                var minionObject = new GameObject("Minion target");
                owned.Add(minionObject);
                var minionView = minionObject.AddComponent<StandardMinionView>();
                minionView.Configure(minion, 1f);

                var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                owned.Add(material);
                template = new HexCornerHighlightTemplate(1f);
                var view = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(),
                    new[] { movement, heroTile, minionTile },
                    material,
                    new[] { minionView },
                    sharedCornerHighlightTemplate: template);
                var model = Model(
                    new[] { movement.Coordinate },
                    new[] { new HeroOccupancy(heroId, heroTile.Coordinate) },
                    new[] { minion },
                    new[] { heroId },
                    new[] { minion.MinionId });
                view.Render(model);

                var inputObject = new GameObject("Pointer input");
                owned.Add(inputObject);
                var cameraObject = new GameObject("Camera");
                owned.Add(cameraObject);
                var camera = cameraObject.AddComponent<Camera>();
                var input = inputObject.AddComponent<PrimitiveHeroPointerInput>();
                input.Configure(
                    camera,
                    Array.Empty<PrimitiveHeroPointerSelector>(),
                    Array.Empty<StandardMinionPointerSelector>(),
                    view,
                    new[] { movement, heroTile, minionTile },
                    _ => false);

                var movementBounds = movement.CornerHighlight.LocalBounds;
                var heroBounds = heroTile.CornerHighlight.LocalBounds;
                Assert.That(input.RefreshHoverFromPointerRay(RayAbove(movement)), Is.True);
                Assert.That(movement.CornerHighlight.IsHovered, Is.True);
                Assert.That(input.RefreshHoverFromPointerRay(RayAbove(heroTile)), Is.True);
                Assert.That(heroTile.CornerHighlight.IsHovered, Is.True);
                Assert.That(movement.CornerHighlight.IsHovered, Is.False);
                Assert.That(input.RefreshHoverFromPointerRay(RayAbove(minionTile)), Is.True);
                Assert.That(minionTile.CornerHighlight.IsHovered, Is.True);
                Assert.That(movement.CornerHighlight.LocalBounds, Is.EqualTo(movementBounds));
                Assert.That(heroTile.CornerHighlight.LocalBounds, Is.EqualTo(heroBounds));

                var layerCounts = new[] { movement, heroTile, minionTile }
                    .Select(tile => tile.GetComponentsInChildren<MeshRenderer>(true).Length)
                    .ToArray();
                for (var rebuild = 0; rebuild < 4; rebuild++)
                {
                    view.Render(Model(
                        new[] { movement.Coordinate },
                        new[] { new HeroOccupancy(heroId, heroTile.Coordinate) },
                        new[] { minion },
                        new[] { heroId },
                        new[] { minion.MinionId }));
                }

                // Milestone 8.35z39: eighteen plus the two saturated-core materials the composite
                // mark needs now that halos are light. What this case guards is that four rebuilds
                // grow the set by NOTHING, which is unchanged - the bound simply moved once.
                // Milestone 8.62's hex-outline cue adds SIX: two full-edge outline meshes
                // and the four materials (light and paint) of the two sides the user named - "blood
                // red on enemy, green on ally". They are built with the rest and released with the
                // rest, which is what this case guards; the bound simply moved a second time.
                Assert.That(template.OwnedNativeResourceCount, Is.EqualTo(26));
                Assert.That(new[] { movement, heroTile, minionTile }
                    .Select(tile => tile.GetComponentsInChildren<MeshRenderer>(true).Length),
                    Is.EqualTo(layerCounts));

                view.Render(Model(
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<HeroOccupancy>(),
                    Array.Empty<MinionOccupancy>(),
                    Array.Empty<HeroId>(),
                    Array.Empty<MinionId>()));
                Assert.That(movement.CornerHighlight.VisibleMarkCount, Is.Zero);
                Assert.That(heroTile.CornerHighlight.VisibleMarkCount, Is.Zero);
                Assert.That(minionTile.CornerHighlight.VisibleMarkCount, Is.Zero);
                yield return null;
            }
            finally
            {
                template?.Dispose();
                foreach (var value in owned)
                {
                    if (value != null) UnityEngine.Object.Destroy(value);
                }
            }
        }

        private static HexTileView Tile(HexCoordinate coordinate, ICollection<UnityEngine.Object> owned)
        {
            var gameObject = new GameObject("Tile " + coordinate);
            owned.Add(gameObject);
            var tile = gameObject.AddComponent<HexTileView>();
            tile.Configure(coordinate, 1f);
            return tile;
        }

        private static Ray RayAbove(HexTileView tile) =>
            new Ray(tile.transform.position + (Vector3.up * 10f), Vector3.down);

        private static MinionOccupancy CreateMinion(string id, HexCoordinate coordinate) =>
            MinionOccupancy.FromSetup(new QuickGameMinionSetupState(
                new MinionId(id),
                new TeamId("blue-team"),
                TeamSide.Blue,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(
                    new MapId("test-map"), new SpawnPointId(id + "-spawn")),
                new ConfiguredSpaceReference(
                    new MapId("test-map"), new SpaceId(id + "-space")),
                coordinate));

        private static BoardInteractionViewModel Model(
            IEnumerable<HexCoordinate> movement,
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<MinionOccupancy> minions,
            IEnumerable<HeroId> attackHeroes,
            IEnumerable<MinionId> attackMinions) =>
            new BoardInteractionViewModel(
                1, heroes, minions, movement, attackHeroes, attackMinions, default);
    }
}
