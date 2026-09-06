using System;
using System.Collections.Generic;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Minions;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class HexCornerHighlightTests
    {
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private HexCornerHighlightTemplate template;

        [TearDown]
        public void TearDown()
        {
            template?.Dispose();
            template = null;
            foreach (var value in owned)
            {
                if (value != null) UnityEngine.Object.DestroyImmediate(value);
            }
            owned.Clear();
        }

        /// <summary>
        /// Milestone 8.35z39. THE MARKS DID NOT MOVE.
        ///
        /// <para>The user's tightening of the light direction was that the marking shapes are already
        /// right: keep every one of them, keep what each colour means, and change ONLY how the pixels
        /// inside them are drawn. This is that promise made checkable. It pins the two meshes and the
        /// eight declared colours against transcribed values - written out here rather than read back
        /// from the type under test, so an edit to the builder cannot move the mesh and the expectation
        /// together - and then asserts the one thing that did change.</para>
        ///
        /// <para>The numbers come from <c>BuildSixCornerMesh</c>'s own arithmetic: six corners, two
        /// bars each, four vertices and six indices per bar. Nothing here is a magic constant that a
        /// future reader cannot re-derive.</para>
        /// </summary>
        [Test]
        public void TheSixCornerMarks_KeepTheirExactGeometryAndColoursAndOnlyTheHaloBecomesLight()
        {
            template = new HexCornerHighlightTemplate(1f);

            foreach (var mesh in new[] { template.HaloMesh, template.CoreMesh })
            {
                Assert.That(mesh.vertexCount, Is.EqualTo(6 * 2 * 4),
                    "Six corners, two bars each, four vertices a bar - the footprint is unchanged.");
                Assert.That(mesh.triangles.Length, Is.EqualTo(6 * 2 * 6),
                    "and two triangles a bar.");
            }

            // The halo is the WIDE layer and the core the narrow one, which is the falloff profile the
            // light is drawn on. If these ever swapped, the treatment would light the filament and
            // leave the skirt flat - the exact inversion that looks correct on dark ground and
            // dissolves on sand.
            Assert.That(
                template.HaloMesh.bounds.size.x,
                Is.GreaterThan(template.CoreMesh.bounds.size.x),
                "The halo is the wider of the two, which is what makes it the spill.");

            var declared = new Dictionary<HexCornerHighlightKind, Color>
            {
                { HexCornerHighlightKind.Movement, new Color(0.22f, 1f, 0.48f, 1f) },
                { HexCornerHighlightKind.Attack, new Color(0.72f, 0.025f, 0.035f, 1f) },
                { HexCornerHighlightKind.Respawn, new Color(0.05f, 0.78f, 1f, 1f) },
                { HexCornerHighlightKind.Ally, new Color(0.92f, 0.90f, 0.80f, 1f) },
                { HexCornerHighlightKind.Effect, new Color(0.62f, 0.68f, 0.72f, 1f) },
                { HexCornerHighlightKind.EffectResult, new Color(0.42f, 0.48f, 0.55f, 1f) },
                { HexCornerHighlightKind.DefensePositive, new Color(0.02f, 0.70f, 0.18f, 1f) },
                { HexCornerHighlightKind.DefenseNegative, new Color(1f, 0.16f, 0.05f, 1f) }
            };

            foreach (var pair in declared)
            {
                var halo = template.Halo(pair.Key);
                var core = template.Core(pair.Key);
                Assert.That(halo, Is.Not.Null, pair.Key + " must still have a halo.");
                Assert.That(core, Is.Not.Null, pair.Key + " must still have a core.");

                // COLOUR SEMANTICS UNCHANGED: which spaces are marked and what each mark means are
                // untouched by a rendering change, and the hue is the whole of what carries the
                // meaning.
                Assert.That(halo.color.r, Is.EqualTo(pair.Value.r).Within(0.0001f), pair.Key + " red");
                Assert.That(halo.color.g, Is.EqualTo(pair.Value.g).Within(0.0001f), pair.Key + " green");
                Assert.That(halo.color.b, Is.EqualTo(pair.Value.b).Within(0.0001f), pair.Key + " blue");

                // AND THE ONE CHANGE: the skirt is light, the filament is still paint.
                Assert.That(BoardOverlayMaterial.IsAdditive(halo), Is.True,
                    pair.Key + "'s halo must be ADDED to the board - that is the whole milestone.");
                Assert.That(BoardOverlayMaterial.IsAdditive(core), Is.False,
                    pair.Key + "'s core still carries the mark's shape at full strength over painted " +
                    "sand, where added light saturates toward white. Lighting it too is the version " +
                    "that dissolves on the bright half of the board.");
            }
        }

        [Test]
        public void EveryLegalBoardChoice_LightsSixFixedHexCornerMarksFromProjectedGeometry()
        {
            var movement = Tile(new HexCoordinate(0, 0));
            var attack = Tile(new HexCoordinate(1, 0));
            var unavailable = Tile(new HexCoordinate(2, 0));
            var target = new HeroId("target");
            var view = View(new[] { movement, attack, unavailable });

            view.Render(Model(
                new[] { movement.Coordinate },
                new[] { new HeroOccupancy(target, attack.Coordinate) },
                new[] { target }));

            Assert.That(movement.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Movement));
            Assert.That(attack.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Attack));
            Assert.That(movement.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
            Assert.That(attack.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
            Assert.That(unavailable.CornerHighlight.VisibleMarkCount, Is.Zero);
            Assert.That(movement.IsHighlighted, Is.False,
                "Movement uses the six printed marks, not the retired whole-hex wash.");
            Assert.That(movement.CornerHighlight.LocalBounds.size,
                Is.EqualTo(attack.CornerHighlight.LocalBounds.size));
            Assert.That(template.MarkCount, Is.EqualTo(6));
            Assert.That(template.CoreMesh.normals,
                Has.All.Matches<Vector3>(normal => Vector3.Dot(normal, Vector3.up) > 0.99f),
                "Every mark faces the board camera rather than being back-face culled.");
        }

        [Test]
        public void RespawnDestinations_UseGreenSixCornerShineWithoutTheLegacyFill()
        {
            var destination = Tile(new HexCoordinate(0, 0));
            var view = View(new[] { destination });
            view.Render(new BoardInteractionViewModel(
                1,
                Array.Empty<HeroOccupancy>(),
                Array.Empty<GuardsDigital.Core.Units.Minions.MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<GuardsDigital.Core.Setup.MinionId>(),
                default,
                heroRespawnDestinations: new[] { destination.Coordinate }));

            Assert.That(destination.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Respawn));
            Assert.That(destination.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
            Assert.That(destination.IsHighlighted, Is.False);
            Assert.That(HexCornerHighlightTemplate.RespawnColor,
                Is.Not.EqualTo(HexCornerHighlightTemplate.MovementColor));
        }

        [Test]
        public void MovementAndAttackHighlights_StrengthenInPlaceWithDistinctSignedOffColours()
        {
            var movement = Tile(new HexCoordinate(0, 0));
            var attack = Tile(new HexCoordinate(1, 0));
            var target = new HeroId("target");
            var view = View(new[] { movement, attack });
            view.Render(Model(
                new[] { movement.Coordinate },
                new[] { new HeroOccupancy(target, attack.Coordinate) },
                new[] { target }));
            var movementBounds = movement.CornerHighlight.LocalBounds;
            var attackBounds = attack.CornerHighlight.LocalBounds;

            view.SetHoveredCoordinate(movement.Coordinate);

            Assert.That(movement.CornerHighlight.IsHovered, Is.True);
            Assert.That(attack.CornerHighlight.IsHovered, Is.False);
            Assert.That(movement.CornerHighlight.LocalBounds, Is.EqualTo(movementBounds));
            Assert.That(attack.CornerHighlight.LocalBounds, Is.EqualTo(attackBounds));
            Assert.That(HexCornerHighlightTemplate.MovementColor,
                Is.Not.EqualTo(HexCornerHighlightTemplate.AttackColor));
            Assert.That(Vector4.Distance(
                HexCornerHighlightTemplate.MovementColor,
                HexCornerHighlightTemplate.AttackColor), Is.GreaterThan(0.5f));
        }

        [Test]
        public void MinionDefenseContributions_LightSixHexCornerMarksInSignedContributionColours()
        {
            var positiveTile = Tile(new HexCoordinate(0, 0));
            var negativeTile = Tile(new HexCoordinate(1, 0));
            var mapId = new MapId("defense-corner-map");
            var positive = GuardsDigital.Core.Units.Minions.MinionOccupancy.FromSetup(
                new QuickGameMinionSetupState(
                    new MinionId("positive-defense"),
                    new TeamId("blue-team"),
                    TeamSide.Blue,
                    StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(mapId, new SpawnPointId("blue-spawn")),
                    new ConfiguredSpaceReference(mapId, new SpaceId("positive-space")),
                    positiveTile.Coordinate));
            var negative = GuardsDigital.Core.Units.Minions.MinionOccupancy.FromSetup(
                new QuickGameMinionSetupState(
                    new MinionId("negative-defense"),
                    new TeamId("red-team"),
                    TeamSide.Red,
                    StandardMinionType.Heavy,
                    new ConfiguredSpawnPointReference(mapId, new SpawnPointId("red-spawn")),
                    new ConfiguredSpaceReference(mapId, new SpaceId("negative-space")),
                    negativeTile.Coordinate));
            var positiveObject = new GameObject("Positive Defense Minion");
            var negativeObject = new GameObject("Negative Defense Minion");
            owned.Add(positiveObject);
            owned.Add(negativeObject);
            var positiveView = positiveObject.AddComponent<StandardMinionView>();
            var negativeView = negativeObject.AddComponent<StandardMinionView>();
            positiveView.Configure(positive, 1f);
            negativeView.Configure(negative, 1f);
            template = new HexCornerHighlightTemplate(1f);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            owned.Add(material);
            var view = new BoardInteractionUnityView(
                Array.Empty<GuardsDigital.Presentation.Units.Heroes.PrimitiveHeroView>(),
                new[] { positiveTile, negativeTile },
                material,
                new[] { positiveView, negativeView },
                sharedCornerHighlightTemplate: template);

            view.Render(new BoardInteractionViewModel(
                1,
                Array.Empty<HeroOccupancy>(),
                new[] { positive, negative },
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<GuardsDigital.Core.Setup.MinionId>(),
                default,
                minionDefenseCues: new[]
                {
                    new MinionDefenseCueViewModel(positive.MinionId, 1),
                    new MinionDefenseCueViewModel(negative.MinionId, -1)
                }));

            Assert.That(positiveTile.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.DefensePositive));
            Assert.That(negativeTile.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.DefenseNegative));
            Assert.That(positiveTile.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
            Assert.That(negativeTile.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
            Assert.That(positiveTile.IsHighlighted, Is.False);
            Assert.That(negativeTile.IsHighlighted, Is.False);
            Assert.That(HexCornerHighlightTemplate.DefensePositiveColor,
                Is.Not.EqualTo(HexCornerHighlightTemplate.MovementColor));
            Assert.That(HexCornerHighlightTemplate.DefenseNegativeColor,
                Is.Not.EqualTo(HexCornerHighlightTemplate.AttackColor));
        }

        [Test]
        public void AttackIntentEffectTargetHeroHover_KeepsBloodRedTargetAndPreviewsProjectedSignedContributions()
        {
            var targetTile = Tile(new HexCoordinate(0, 0));
            var positiveTile = Tile(new HexCoordinate(1, 0));
            var negativeTile = Tile(new HexCoordinate(1, 1));
            var targetId = new HeroId("hovered-target");
            var target = new HeroOccupancy(targetId, targetTile.Coordinate);
            var mapId = new MapId("hover-preview-map");
            MinionOccupancy Minion(string id, TeamSide side, HexTileView tile) =>
                MinionOccupancy.FromSetup(new QuickGameMinionSetupState(
                    new MinionId(id),
                    new TeamId(side == TeamSide.Blue ? "blue-team" : "red-team"),
                    side,
                    StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(mapId, new SpawnPointId(id + "-spawn")),
                    new ConfiguredSpaceReference(mapId, new SpaceId(id + "-space")),
                    tile.Coordinate));
            var positive = Minion("hover-positive", TeamSide.Blue, positiveTile);
            var negative = Minion("hover-negative", TeamSide.Red, negativeTile);
            ValueContributionProjection Contribution(MinionOccupancy minion, int amount) =>
                new ValueContributionProjection(
                    new ValueContribution(
                        ValueContributionKind.MinionDefenseModifier,
                        minion.MinionId,
                        PrintedValueChannel.Defense,
                        amount),
                    minion.MinionId.Value);
            var targetPreview = new AttackTargetDefensePreviewProjection(
                targetId,
                new[] { Contribution(positive, 1), Contribution(negative, -1) });

            var heroObject = new GameObject("Hovered Target Hero");
            var positiveObject = new GameObject("Hover Positive Minion");
            var negativeObject = new GameObject("Hover Negative Minion");
            owned.Add(heroObject);
            owned.Add(positiveObject);
            owned.Add(negativeObject);
            var heroView = heroObject.AddComponent<PrimitiveHeroView>();
            heroView.Configure(target, 1f);
            var positiveView = positiveObject.AddComponent<StandardMinionView>();
            var negativeView = negativeObject.AddComponent<StandardMinionView>();
            positiveView.Configure(positive, 1f);
            negativeView.Configure(negative, 1f);
            template = new HexCornerHighlightTemplate(1f);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            owned.Add(material);
            var view = new BoardInteractionUnityView(
                new[] { heroView },
                new[] { targetTile, positiveTile, negativeTile },
                material,
                new[] { positiveView, negativeView },
                sharedCornerHighlightTemplate: template);
            view.Render(new BoardInteractionViewModel(
                1,
                new[] { target },
                new[] { positive, negative },
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<MinionId>(),
                default,
                pieceTargetCues: new[]
                {
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForHero(targetId),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Attack),
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForMinion(positive.MinionId),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Attack)
                },
                attackTargetDefensePreviews: new[] { targetPreview }));

            Assert.That(targetTile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Attack));
            Assert.That(positiveTile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Attack));
            Assert.That(positiveTile.CornerHighlight.ContributionKind,
                Is.EqualTo(HexCornerHighlightKind.None));

            view.SetHoveredCoordinate(targetTile.Coordinate);

            Assert.That(targetTile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Attack));
            Assert.That(targetTile.CornerHighlight.IsHovered, Is.True);
            Assert.That(positiveTile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Attack),
                "A minion that is also an Attack target retains its blood-red target halo.");
            Assert.That(positiveTile.CornerHighlight.ContributionKind,
                Is.EqualTo(HexCornerHighlightKind.DefensePositive));
            Assert.That(positiveTile.CornerHighlight.VisibleCoreMaterialColor,
                Is.EqualTo(HexCornerHighlightTemplate.DefensePositiveColor),
                "A contributing minion that is also an Attack target needs a saturated green core " +
                "inside its blood-red halo; a white-biased core disappears at gameplay scale.");
            Assert.That(negativeTile.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.DefenseNegative));

            view.SetHoveredCoordinate(null);

            Assert.That(positiveTile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Attack));
            Assert.That(positiveTile.CornerHighlight.ContributionKind,
                Is.EqualTo(HexCornerHighlightKind.None));
            Assert.That(negativeTile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.None));
        }

        [Test]
        public void FastTravelDestinations_UseTheSameMovementCornerTreatmentWithoutAWholeHexWash()
        {
            var destination = Tile(new HexCoordinate(0, 0));
            var view = View(new[] { destination });
            view.Render(new BoardInteractionViewModel(
                1,
                Array.Empty<HeroOccupancy>(),
                Array.Empty<GuardsDigital.Core.Units.Minions.MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<GuardsDigital.Core.Setup.MinionId>(),
                default,
                fastTravelDestinations: new[] { destination.Coordinate }));

            Assert.That(destination.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Movement));
            Assert.That(destination.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
            Assert.That(destination.IsHighlighted, Is.False);
        }

        [Test]
        public void EffectsAndRemoval_UseNeutralAndBloodRedCornersWithoutLegacyMarks()
        {
            var effectDestination = Tile(new HexCoordinate(0, 0));
            var affectedHeroTile = Tile(new HexCoordinate(1, 0));
            var removedHeroTile = Tile(new HexCoordinate(2, 0));
            var resultingSpace = Tile(new HexCoordinate(3, 0));
            var affectedHero = new HeroId("affected");
            var removedHero = new HeroId("removed");
            var view = View(new[]
            {
                effectDestination, affectedHeroTile, removedHeroTile, resultingSpace
            });

            view.Render(new BoardInteractionViewModel(
                1,
                new[]
                {
                    new HeroOccupancy(affectedHero, affectedHeroTile.Coordinate),
                    new HeroOccupancy(removedHero, removedHeroTile.Coordinate)
                },
                Array.Empty<GuardsDigital.Core.Units.Minions.MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<GuardsDigital.Core.Setup.MinionId>(),
                default,
                pieceTargetCues: new[]
                {
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForHero(affectedHero),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Affect),
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForHero(removedHero),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Removal)
                },
                effectChoiceDestinations: new[] { effectDestination.Coordinate },
                effectChoiceTargetSpace: affectedHeroTile.Coordinate,
                effectChoiceResultingSpace: resultingSpace.Coordinate));

            Assert.That(effectDestination.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Effect));
            Assert.That(affectedHeroTile.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Effect));
            Assert.That(removedHeroTile.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack),
                "Minion Battle and other removal choices share the blood-red destructive language.");
            Assert.That(resultingSpace.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.EffectResult));
            Assert.That(new[]
                {
                    effectDestination, affectedHeroTile, removedHeroTile, resultingSpace
                }, Has.All.Matches<HexTileView>(tile =>
                    tile.CornerHighlight.VisibleMarkCount == 6 && !tile.IsHighlighted));
            Assert.That(HexCornerHighlightTemplate.EffectColor,
                Is.Not.EqualTo(HexCornerHighlightTemplate.AttackColor));
        }

        [Test]
        public void TeammateActions_KeepTheWarmIvoryCornersWhileOtherEffectsStayNeutral()
        {
            var allyTile = Tile(new HexCoordinate(0, 0));
            var neutralTile = Tile(new HexCoordinate(1, 0));
            var ally = new HeroId("ally");
            var affected = new HeroId("affected");
            var view = View(new[] { allyTile, neutralTile });

            view.Render(new BoardInteractionViewModel(
                1,
                new[]
                {
                    new HeroOccupancy(ally, allyTile.Coordinate),
                    new HeroOccupancy(affected, neutralTile.Coordinate)
                },
                Array.Empty<GuardsDigital.Core.Units.Minions.MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<GuardsDigital.Core.Setup.MinionId>(),
                default,
                pieceTargetCues: new[]
                {
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForHero(ally),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Ally),
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForHero(affected),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Affect)
                }));

            Assert.That(allyTile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Ally));
            Assert.That(neutralTile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Effect));
            Assert.That(HexCornerHighlightTemplate.AllyColor,
                Is.EqualTo(new Color(0.92f, 0.90f, 0.80f, 1f)));
            Assert.That(HexCornerHighlightTemplate.AllyColor,
                Is.Not.EqualTo(HexCornerHighlightTemplate.EffectColor));
        }

        private HexTileView Tile(HexCoordinate coordinate)
        {
            var gameObject = new GameObject("Tile " + coordinate);
            owned.Add(gameObject);
            var tile = gameObject.AddComponent<HexTileView>();
            tile.Configure(coordinate, 1f);
            return tile;
        }

        private BoardInteractionUnityView View(IEnumerable<HexTileView> tiles)
        {
            template = new HexCornerHighlightTemplate(1f);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            owned.Add(material);
            return new BoardInteractionUnityView(
                Array.Empty<GuardsDigital.Presentation.Units.Heroes.PrimitiveHeroView>(),
                tiles,
                material,
                sharedCornerHighlightTemplate: template);
        }

        private static BoardInteractionViewModel Model(
            IEnumerable<HexCoordinate> movement,
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<HeroId> attackTargets) =>
            new BoardInteractionViewModel(
                1,
                heroes,
                Array.Empty<GuardsDigital.Core.Units.Minions.MinionOccupancy>(),
                movement,
                attackTargets,
                Array.Empty<GuardsDigital.Core.Setup.MinionId>(),
                default);
    }
}
