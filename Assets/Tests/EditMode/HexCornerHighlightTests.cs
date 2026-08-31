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
