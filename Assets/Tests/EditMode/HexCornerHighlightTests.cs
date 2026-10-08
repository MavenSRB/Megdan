using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Milestone 8.62. Every case below but one is about the six CORNER MARKS, and configured
        /// play now outlines a piece target's whole hex instead ("I like this. Keep it."). The marks
        /// are unchanged and still mark every space that is not a piece target, so these cases name
        /// the treatment they measure and put the shipped one back.
        /// </summary>
        private BoardTargetCueTreatment restoreCueTreatment;

        [SetUp]
        public void MeasureTheCornerMarkTreatment()
        {
            restoreCueTreatment = BoardTargetCuePresentation.Treatment;
            BoardTargetCuePresentation.Treatment = BoardTargetCueTreatment.RingOnThePiece;
        }

        [TearDown]
        public void RestoreTheShippedCueTreatment() =>
            BoardTargetCuePresentation.Treatment = restoreCueTreatment;
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

        [TestCase(false)]
        [TestCase(true)]
        public void AttackIntentEffectTargetHeroHover_KeepsBloodRedTargetAndPreviewsProjectedSignedContributions(bool attackIntent)
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
            var targetModifier = new HeroDefenseModifierProjection(
                targetId,
                HeroOccupancy.FirstBody,
                targetPreview.MinionContributions);

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
                pieceTargetCues: attackIntent ? new[]
                {
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForHero(targetId),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Attack),
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForMinion(positive.MinionId),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Attack)
                } : Array.Empty<PieceTargetCueViewModel>(),
                attackTargetDefensePreviews: attackIntent ? new[] { targetPreview } : null,
                heroDefensePreviews: new[] { targetPreview },
                heroDefenseModifiers: new[] { targetModifier }));

            var restingKind = attackIntent ? HexCornerHighlightKind.Attack : HexCornerHighlightKind.None;
            Assert.That(targetTile.CornerHighlight.Kind, Is.EqualTo(restingKind));
            Assert.That(positiveTile.CornerHighlight.Kind, Is.EqualTo(restingKind));
            Assert.That(positiveTile.CornerHighlight.ContributionKind,
                Is.EqualTo(HexCornerHighlightKind.None));

            Assert.That(heroView.DefenseModifierShield, Is.Not.Null,
                "The modifier must be visible before the player hovers the hero.");
            Assert.That(heroView.DefenseModifierShield.IsVisible, Is.True);

            view.SetHoveredCoordinate(targetTile.Coordinate);

            Assert.That(positiveView.DefenseContributionAmount, Is.Zero,
                "ordinary hover no longer reveals Defense contributors");
            Assert.That(negativeView.DefenseContributionAmount, Is.Zero);
            view.SetDefenseDetailTarget(targetId, HeroOccupancy.FirstBody);

            Assert.That(targetTile.CornerHighlight.Kind, Is.EqualTo(restingKind));
            Assert.That(targetTile.CornerHighlight.IsHovered, Is.EqualTo(attackIntent));
            Assert.That(positiveTile.CornerHighlight.Kind, Is.EqualTo(attackIntent
                    ? HexCornerHighlightKind.Attack : HexCornerHighlightKind.DefensePositive),
                "A minion that is also an Attack target retains its blood-red target halo.");
            Assert.That(positiveTile.CornerHighlight.ContributionKind,
                Is.EqualTo(HexCornerHighlightKind.DefensePositive));
            if (attackIntent)
                Assert.That(positiveTile.CornerHighlight.VisibleCoreMaterialColor,
                    Is.EqualTo(HexCornerHighlightTemplate.DefensePositiveColor),
                    "A contributing minion that is also an Attack target needs a saturated green core " +
                    "inside its blood-red halo; a white-biased core disappears at gameplay scale.");
            Assert.That(negativeTile.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.DefenseNegative));
            Assert.That(positiveView.DefenseContributionAmount, Is.EqualTo(1));
            Assert.That(positiveView.GetComponent<SpriteRenderer>().color,
                Is.EqualTo(HexCornerHighlightTemplate.DefensePositiveColor));
            Assert.That(negativeView.DefenseContributionAmount, Is.EqualTo(-1));
            Assert.That(negativeView.GetComponent<SpriteRenderer>().color,
                Is.EqualTo(HexCornerHighlightTemplate.DefenseNegativeColor));
            Assert.That(positiveView.GetComponentsInChildren<TextMesh>(true), Is.Empty);
            Assert.That(negativeView.GetComponentsInChildren<TextMesh>(true), Is.Empty);
            Assert.That(heroView.DefenseModifierShield, Is.Not.Null);
            Assert.That(heroView.DefenseModifierShield.IsVisible, Is.True);
            Assert.That(heroView.DefenseModifierShield.Modifier, Is.EqualTo(0));
            Assert.That(heroView.DefenseModifierShield.ValueText.text, Is.EqualTo("0"));
            Assert.That(heroView.DefenseModifierShield.ValueText.text, Does.Not.Contain("DEF"));
            var shieldBounds = heroView.DefenseModifierShield.GetComponent<MeshRenderer>().bounds;
            var valueBounds = heroView.DefenseModifierShield.ValueText
                .GetComponent<MeshRenderer>().bounds;
            Assert.That(new Vector2(valueBounds.size.x, valueBounds.size.z).magnitude,
                Is.LessThan(new Vector2(shieldBounds.size.x, shieldBounds.size.z).magnitude * 0.85f),
                "The actual rendered numeral must fit inside the tiny shield.");
            Assert.That(new Vector2(valueBounds.center.x - shieldBounds.center.x,
                    valueBounds.center.z - shieldBounds.center.z).magnitude,
                Is.LessThan(0.001f), "The signed value is centered inside the shield face.");
            Assert.That(new Vector2(
                    heroView.DefenseModifierShield.transform.localPosition.x,
                    heroView.DefenseModifierShield.transform.localPosition.z).magnitude,
                Is.EqualTo(heroView.SelectionRadius * HeroDefenseModifierShield.EdgeDistanceFactor)
                    .Within(0.001f),
                "The tiny shield stays at the token edge.");
            var screenLeft = Vector3.Cross(BoardChoiceCue.ScreenUpInWorld, Vector3.up);
            var shieldOffset = heroView.DefenseModifierShield.transform.position - heroView.transform.position;
            Assert.That(Vector3.Dot(shieldOffset, screenLeft),
                Is.EqualTo(heroView.SelectionRadius * HeroDefenseModifierShield.EdgeDistanceFactor)
                    .Within(0.001f),
                "The shield must sit on screen-left, not at the bottom beside the hand pips.");
            Assert.That(heroView.DefenseModifierShield.GetComponent<MeshFilter>()
                    .sharedMesh.bounds.size.magnitude,
                Is.LessThan(heroView.SelectionRadius * 0.95f),
                "The shield stays materially smaller than the hero token.");

            view.SetDefenseDetailTarget(default, 0);

            Assert.That(positiveTile.CornerHighlight.Kind, Is.EqualTo(restingKind));
            Assert.That(positiveTile.CornerHighlight.ContributionKind,
                Is.EqualTo(HexCornerHighlightKind.None));
            Assert.That(negativeTile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.None));
            Assert.That(positiveView.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white));
            Assert.That(negativeView.GetComponent<SpriteRenderer>().color, Is.EqualTo(Color.white));
            Assert.That(heroView.DefenseModifierShield.IsVisible, Is.True,
                "Leaving hover removes creep highlights but retains the hero's modifier.");

            view.SetHoveredHero(targetId);
            Assert.That(positiveView.DefenseContributionAmount, Is.Zero,
                "linked rail/interface hover stays quiet unless the board token is Shift-hovered");
            Assert.That(heroView.DefenseModifierShield.IsVisible, Is.True);
            view.SetHoveredHero(default);
            Assert.That(positiveView.DefenseContributionAmount, Is.Zero);
            Assert.That(heroView.DefenseModifierShield.IsVisible, Is.True);
        }

        [TestCase(0f)]
        [TestCase(60f)]
        [TestCase(135f)]
        public void PersistentShieldUsesScreenLeftAndReusesResourcesWithoutMovingCardPips(float cameraHeading)
        {
            var previousUp = BoardChoiceCue.ScreenUpInWorld;
            var token = new GameObject("Shield and actual card pips");
            owned.Add(token);
            try
            {
                BoardChoiceCue.SetScreenUp(Quaternion.Euler(0f, cameraHeading, 0f) * Vector3.forward);
                token.transform.rotation = Quaternion.Euler(0f, 25f, 0f);
                var pips = HeroCardStateIndicator.GetOrCreate(token.transform);
                pips.SetVisible(true, new HeroCardPip(CardColor.Blue),
                    new[] { CardColor.Gold, CardColor.Silver, CardColor.Red, CardColor.Green }
                        .Select(color => new HeroCardPip(color)), 0, 1f, 0.15f);
                var pipPositions = pips.Renderers.Select(value => value.transform.position).ToArray();
                var pipColours = pips.Renderers.Select(value => value.sharedMaterial.color).ToArray();
                // Each pip has an outer and inner renderer; hand pips follow the current pip.
                for (var pipIndex = 1; pipIndex < 4; pipIndex++)
                    Assert.That(Vector3.Distance(pipPositions[pipIndex * 2], pipPositions[(pipIndex + 1) * 2]),
                        Is.EqualTo(0.28f).Within(0.001f),
                        "Unchanged 0.24-unit card pips have a visible 0.04-unit gap.");
                var shield = HeroDefenseModifierShield.GetOrCreate(token.transform);
                shield.SetVisible(-2, 1f, 0.15f);
                var mesh = shield.GetComponent<MeshFilter>().sharedMesh;
                var material = shield.GetComponent<MeshRenderer>().sharedMaterial;
                var materials = shield.GetComponent<MeshRenderer>().sharedMaterials;
                var left = Vector3.Cross(BoardChoiceCue.ScreenUpInWorld, Vector3.up);
                var offset = shield.transform.position - token.transform.position;
                var pipOffset = pipPositions[0] - token.transform.position;
                var pipRimRadius = new Vector2(pipOffset.x, pipOffset.z).magnitude;
                Assert.That(Vector3.Dot(offset, left), Is.EqualTo(pipRimRadius).Within(0.001f),
                    "The shield center straddles the same token rim centerline as the card pips.");
                Assert.That(Vector3.Dot(offset, left),
                    Is.EqualTo(HeroDefenseModifierShield.EdgeDistanceFactor).Within(0.001f));
                Assert.That(Vector3.Dot(offset, BoardChoiceCue.ScreenUpInWorld), Is.EqualTo(0f).Within(0.001f));
                for (var i = 0; i < 20; i++)
                {
                    shield.SetHidden();
                    shield.SetVisible(i % 3 == 0 ? -12 : i % 3 == 1 ? 0 : 12, 1f, 0.15f);
                    var glyphs = shield.ValueText.GetComponent<MeshRenderer>().localBounds.size *
                        shield.ValueText.transform.localScale.x;
                    Assert.That(glyphs.x, Is.LessThanOrEqualTo(
                        HeroDefenseModifierShield.HalfWidthFactor * 2f * 0.81f));
                    Assert.That(glyphs.y, Is.LessThanOrEqualTo(
                        HeroDefenseModifierShield.HeightFactor * 0.67f));
                    Assert.That(Vector3.Dot(shield.ValueText.transform.right, -left),
                        Is.GreaterThan(0.999f), "Signed glyphs read left-to-right, not mirrored.");
                    Assert.That(Vector3.Dot(shield.ValueText.transform.up, BoardChoiceCue.ScreenUpInWorld),
                        Is.GreaterThan(0.999f));
                }
                Assert.That(shield.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(mesh));
                Assert.That(shield.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(material));
                Assert.That(shield.GetComponent<MeshRenderer>().sharedMaterials, Is.EqualTo(materials));
                Assert.That(shield.OwnedNativeResourceCount, Is.EqualTo(3));
                Assert.That(pips.PipCount, Is.EqualTo(5));
                Assert.That(pips.Renderers.Select(value => value.transform.position), Is.EqualTo(pipPositions));
                Assert.That(pips.Renderers.Select(value => value.sharedMaterial.color), Is.EqualTo(pipColours));
            }
            finally { BoardChoiceCue.SetScreenUp(previousUp); }
        }

        [Test]
        public void HeroShieldMatchesMockupTallOutlinedSilhouetteAndReleasesItsMaterials()
        {
            var previousUp = BoardChoiceCue.ScreenUpInWorld;
            var token = new GameObject("Mockup shield");
            owned.Add(token);
            try
            {
                BoardChoiceCue.SetScreenUp(Vector3.forward);
                var shield = HeroDefenseModifierShield.GetOrCreate(token.transform);
                shield.SetVisible(-1, 1f, 0.15f);
                var mesh = shield.GetComponent<MeshFilter>().sharedMesh;
                var materials = shield.GetComponent<MeshRenderer>().sharedMaterials;
                Assert.That(mesh.bounds.size.z / mesh.bounds.size.x, Is.InRange(1.25f, 1.4f));
                Assert.That(mesh.bounds.size.x, Is.EqualTo(0.44f * 1.3f).Within(0.001f));
                Assert.That(mesh.bounds.size.z, Is.EqualTo(0.58f * 1.3f).Within(0.001f));
                Assert.That(mesh.subMeshCount, Is.EqualTo(2));
                Assert.That(materials, Has.Length.EqualTo(2));
                Assert.That(materials[0].color, Is.EqualTo(HeroDefenseModifierShield.OutlineColor));
                Assert.That(materials[1].color, Is.EqualTo(HeroDefenseModifierShield.ShieldColor));
                Assert.That(materials[0].color.grayscale, Is.LessThan(0.1f));
                Assert.That(materials[1].color.r, Is.GreaterThan(0.7f));
                Assert.That(mesh.vertices[1].z, Is.GreaterThan(mesh.vertices[2].z),
                    "The crown rises above the shoulders instead of forming a flat bar.");
                Assert.That(mesh.vertices[7].x, Is.EqualTo(0f).Within(0.001f));
                Assert.That(mesh.vertices[7].z, Is.EqualTo(-HeroDefenseModifierShield.HeightFactor / 2f));
                shield.ReleaseOwnedResources();
                Assert.That(shield.OwnedNativeResourceCount, Is.Zero);
                Assert.That(materials.All(value => value == null), Is.True);
            }
            finally { BoardChoiceCue.SetScreenUp(previousUp); }
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

        /// <summary>
        /// Milestone 8.62. The attack mark is blood red LIGHT, at full strength, whether or not the
        /// pointer is on it - and its shape is the shape it always was.
        ///
        /// <para>The user, on a screenshot of an attack's targets: "can you make targeting highlights
        /// more visible, they are almost impossible to see like this.. Blood red light instead of this
        /// pink i think." What they were looking at is measured here: the filament inside the mark was
        /// carried 82% of the way to white, so the reviewed blood red arrived on painted sand as a
        /// pale warm pink, and both layers rested below full strength.</para>
        ///
        /// <para>Three things are therefore pinned, and one is pinned as UNCHANGED: the filament keeps
        /// its own red, both layers rest at full strength, every other kind keeps the treatment it was
        /// reviewed with, and the geometry of the marks does not move - 8.35z39's promise, which this
        /// milestone had no reason to break.</para>
        /// </summary>
        [Test]
        public void AttackCornerMarks_AreBloodRedAtFullStrengthWhileEveryOtherMarkKeepsItsOwnTreatment()
        {
            var movement = Tile(new HexCoordinate(0, 0));
            var attack = Tile(new HexCoordinate(1, 0));
            var target = new HeroId("target");
            var view = View(new[] { movement, attack });
            view.Render(Model(
                new[] { movement.Coordinate },
                new[] { new HeroOccupancy(target, attack.Coordinate) },
                new[] { target }));

            Assert.That(HexCornerHighlightTemplate.AttackCoreWhiteLift,
                Is.LessThan(HexCornerHighlightTemplate.CoreWhiteLift),
                "The attack's filament keeps more of its own blood red than any other mark's does.");
            Assert.That(
                template.Core(HexCornerHighlightKind.Attack).color,
                Is.EqualTo(Color.Lerp(
                    HexCornerHighlightTemplate.AttackColor,
                    Color.white,
                    HexCornerHighlightTemplate.AttackCoreWhiteLift)),
                "and it is that colour rather than the pale pink 0.82 produced.");
            Assert.That(
                template.Core(HexCornerHighlightKind.Attack).color.g,
                Is.LessThan(0.5f),
                "A blood red reads as red rather than as pink: the filament is nowhere near neutral.");
            Assert.That(
                template.Core(HexCornerHighlightKind.Movement).color,
                Is.EqualTo(Color.Lerp(
                    HexCornerHighlightTemplate.MovementColor,
                    Color.white,
                    HexCornerHighlightTemplate.CoreWhiteLift)),
                "Travel, and every other kind, keeps the filament the light pass reviewed.");
            Assert.That(
                BoardOverlayMaterial.IsAdditive(template.Halo(HexCornerHighlightKind.Attack)),
                Is.True,
                "The attack's skirt is still drawn as light rather than as paint.");

            // Resting strength, read off the renderers the component actually paints through.
            Assert.That(attack.CornerHighlight.IsHovered, Is.False);
            Assert.That(LayerAlpha(attack, "Corner Shine Halo"), Is.EqualTo(1f).Within(0.0001f),
                "The attack's skirt rests at full strength: it is never almost invisible again.");
            Assert.That(LayerAlpha(attack, "Corner Shine Core"), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(LayerAlpha(movement, "Corner Shine Halo"), Is.EqualTo(0.72f).Within(0.0001f),
                "and every other mark keeps the resting pair, so hover still means something there.");
            Assert.That(LayerAlpha(movement, "Corner Shine Core"), Is.EqualTo(0.84f).Within(0.0001f));

            var attackBounds = attack.CornerHighlight.LocalBounds;
            view.SetHoveredCoordinate(attack.Coordinate);

            Assert.That(attack.CornerHighlight.IsHovered, Is.True);
            Assert.That(attack.CornerHighlight.LocalBounds, Is.EqualTo(attackBounds),
                "Milestone 8.35z39's promise holds: the marking shapes do not move.");
            Assert.That(LayerAlpha(attack, "Corner Shine Halo"), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(LayerAlpha(movement, "Corner Shine Halo"), Is.EqualTo(0.72f).Within(0.0001f));
        }

        /// <summary>
        /// Milestone 8.62, the shipped treatment. The user, from the photographs: "Let's try how it
        /// looks when we remove the outline of the token, just outline the hex", "Keep the colors,
        /// blood red on enemy, green on ally", "I like this. Keep it." and, of the first hovered
        /// frames, "So make it a bit thicker on hover. Go x2, or even more."
        ///
        /// <para>The first attempt drew the edge as an additive halo under a white-lifted filament
        /// and photographed as a pale pink line - the same fault the user rejected in the wash. So
        /// this states the edge as PAINT: the ring's own colours at full alpha, no white lift, a
        /// darker rim of the same hue under them, at the ring's own width, and a pointed-at edge
        /// more than twice as wide.</para>
        /// </summary>
        [Test]
        public void ATargetsSpace_KeepsItsCornerMarksInTheSideColourAndClosesIntoAHexWhenPointedAt()
        {
            var tile = Tile(new HexCoordinate(0, 0));
            View(new[] { tile });

            Assert.That(HexCornerHighlightTemplate.OutlineCoreHalfWidth * 2f,
                Is.EqualTo(BoardTargetRing.EligibleWidth).Within(0.0001f),
                "The edge is as heavy as the ring the user compared it against.");

            var enemy = template.OutlineCore(BoardTargetOutlineSide.Enemy);
            var friendly = template.OutlineCore(BoardTargetOutlineSide.Friendly);
            Assert.That(enemy.color, Is.EqualTo(BoardTargetRing.BloodRedColor),
                "An enemy target is the ring's blood red itself, not a lift towards white.");
            Assert.That(friendly.color, Is.EqualTo(BoardTargetRing.AffectColor),
                "and a friendly target is the ring's green itself.");
            Assert.That(enemy.color.a, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(friendly.color.a, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(BoardOverlayMaterial.IsAdditive(enemy), Is.False,
                "It is paint. Drawn as light it photographed as a pale pink line.");
            Assert.That(
                BoardOverlayMaterial.IsAdditive(
                    template.OutlineContour(BoardTargetOutlineSide.Enemy)),
                Is.False,
                "and so is the rim that carries it on sand.");
            Assert.That(template.OutlineContour(BoardTargetOutlineSide.Enemy).color.r,
                Is.LessThan(enemy.color.r),
                "The rim is a DARKER red than the edge, never a lighter halo around it.");

            // AT REST a target's space keeps the SHAPES it has always had - the user, shown a zoom
            // of the original cue: "No, not the ring, but the old highlight mark" - and takes only
            // the side's paint. The meshes are the template's own corner meshes, unchanged.
            BoardTargetCuePresentation.Treatment =
                BoardTargetCueTreatment.CornerMarksThenHoveredHexOutline;
            tile.SetCornerHighlighted(
                HexCornerHighlightKind.Attack, false, HexCornerHighlightKind.None,
                BoardTargetOutlineSide.Enemy);
            Assert.That(tile.CornerHighlight.OutlineSide,
                Is.EqualTo(BoardTargetOutlineSide.Enemy));
            Assert.That(tile.CornerHighlight.IsFullEdgeOutline, Is.False,
                "At rest the six corner bars are the cue, exactly as before this milestone.");
            Assert.That(LayerMesh(tile, "Corner Shine Core"), Is.SameAs(template.CoreMesh),
                "and their geometry is the corner mesh itself, not a new shape.");
            Assert.That(LayerMesh(tile, "Corner Shine Halo"), Is.SameAs(template.HaloMesh));
            Assert.That(LayerAlpha(tile, "Corner Shine Core"), Is.EqualTo(1f).Within(0.0001f),
                "drawn at full opacity rather than the pale resting pair.");

            // POINTED AT they grow into the closed hexagon, at the same edge width.
            tile.SetCornerHighlighted(
                HexCornerHighlightKind.Attack, true, HexCornerHighlightKind.None,
                BoardTargetOutlineSide.Enemy);
            Assert.That(tile.CornerHighlight.IsFullEdgeOutline, Is.True);
            Assert.That(LayerMesh(tile, "Corner Shine Core"), Is.SameAs(template.OutlineCoreMesh));
            Assert.That(
                tile.CornerHighlight.LocalBounds.size.x,
                Is.GreaterThan(0f));

            tile.SetCornerHighlighted(HexCornerHighlightKind.Attack, true);
            Assert.That(tile.CornerHighlight.IsFullEdgeOutline, Is.False,
                "and a space that is not a piece target keeps its six corner bars, pointed at or not.");
        }

        /// <summary>The mesh one named layer of a tile's marks is actually drawing.</summary>
        private static Mesh LayerMesh(HexTileView tile, string layerName)
        {
            var layer = tile.CornerHighlight.transform.Find(layerName);
            Assert.That(layer, Is.Not.Null, "The mark has a layer named " + layerName + ".");
            var filter = layer.GetComponent<MeshFilter>();
            Assert.That(filter, Is.Not.Null);
            return filter.sharedMesh;
        }

        /// <summary>The alpha one named layer of a tile's marks is actually painted with.</summary>
        private static float LayerAlpha(HexTileView tile, string layerName)
        {
            var layer = tile.CornerHighlight.transform.Find(layerName);
            Assert.That(layer, Is.Not.Null, "The mark has a layer named " + layerName + ".");
            var renderer = layer.GetComponent<MeshRenderer>();
            Assert.That(renderer, Is.Not.Null);
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetColor("_BaseColor").a;
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
