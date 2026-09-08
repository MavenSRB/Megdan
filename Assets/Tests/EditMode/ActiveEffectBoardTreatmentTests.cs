using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z63, the drawing half. <c>ActiveEffectVisibilityTests</c> proves WHICH
    /// pieces and WHICH hexes an Active effect reaches, offline; this proves that what is drawn on
    /// them is the tray's own violet, that it is drawn UNDER everything already on the board, and that
    /// it never displaces a mark that was there first.
    /// </summary>
    public sealed class ActiveEffectBoardTreatmentTests
    {
        private static readonly HeroId Carrier = new HeroId("carrier-hero");
        private static readonly HeroId Bystander = new HeroId("bystander-hero");

        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private HexCornerHighlightTemplate template;
        private BoardInteractionUnityView view;

        [TearDown]
        public void TearDown()
        {
            view?.ReleaseOwnedResources();
            view = null;
            template?.Dispose();
            template = null;
            foreach (var value in owned)
            {
                if (value != null) UnityEngine.Object.DestroyImmediate(value);
            }

            owned.Clear();
        }

        /// <summary>
        /// THE ONE COLOUR. The ring on the piece and the tint on the tiles both read
        /// <see cref="MatchHudTheme.ActiveEffectAccent"/> - the same static the tray's own
        /// <c>ACTIVE</c> outline and chip have read since milestone 8.5b - so there is exactly one
        /// violet in this project and no second one to drift.
        ///
        /// <para>They differ only in ALPHA, and that difference is the board's own two idioms rather
        /// than a second colour: a thin mark is drawn as light at
        /// <see cref="BoardOverlayMaterial.LuminousAlpha"/>, and a whole-hex area is drawn as a tint at
        /// <see cref="BoardOverlayMaterial.OverlayAlpha"/> so the artwork reads through it.</para>
        /// </summary>
        [Test]
        public void TheRingAndTheAuraAreBothTheTraysOwnActiveViolet()
        {
            var tile = Tile(new HexCoordinate(0, 0));
            var hero = Hero(Carrier, new HexCoordinate(0, 0));
            Build(new[] { tile }, new[] { hero });

            view.Render(Model(
                new[] { new HeroOccupancy(Carrier, tile.Coordinate) },
                new[] { Carrier },
                new[] { tile.Coordinate }));

            var accent = MatchHudTheme.ActiveEffectAccent;
            AssertSameHue(hero.ActiveEffectMarker.VisibleColor, accent, "the piece ring");
            AssertSameHue(view.ActiveEffectAuraMaterial.color, accent, "the hex aura");

            Assert.That(hero.ActiveEffectMarker.VisibleColor.a,
                Is.EqualTo(BoardOverlayMaterial.LuminousAlpha).Within(0.0001f),
                "A thin mark is drawn as light, exactly as the acting-hero ring is.");
            Assert.That(view.ActiveEffectAuraMaterial.color.a,
                Is.EqualTo(BoardOverlayMaterial.OverlayAlpha).Within(0.0001f),
                "A whole-hex area is milestone 8.11a's tint, so the board's painting reads through it.");
        }

        /// <summary>
        /// The user's example, on the board: a hero carrying an Active effect wears the violet on his
        /// own token, and a hero carrying none wears nothing.
        /// </summary>
        [Test]
        public void OnlyAHeroCarryingAnActiveEffectWearsTheRing()
        {
            var carrierTile = Tile(new HexCoordinate(0, 0));
            var bystanderTile = Tile(new HexCoordinate(1, 0));
            var carrier = Hero(Carrier, carrierTile.Coordinate);
            var bystander = Hero(Bystander, bystanderTile.Coordinate);
            Build(new[] { carrierTile, bystanderTile }, new[] { carrier, bystander });

            view.Render(Model(
                new[]
                {
                    new HeroOccupancy(Carrier, carrierTile.Coordinate),
                    new HeroOccupancy(Bystander, bystanderTile.Coordinate)
                },
                new[] { Carrier },
                Array.Empty<HexCoordinate>()));

            Assert.That(carrier.HasActiveEffectMark, Is.True);
            Assert.That(bystander.HasActiveEffectMark, Is.False);
        }

        /// <summary>An effect that ends takes its ring with it on the very next render.</summary>
        [Test]
        public void AnEffectThatEndsClearsTheRingAndTheTint()
        {
            var tile = Tile(new HexCoordinate(0, 0));
            var hero = Hero(Carrier, tile.Coordinate);
            Build(new[] { tile }, new[] { hero });
            var heroes = new[] { new HeroOccupancy(Carrier, tile.Coordinate) };

            view.Render(Model(heroes, new[] { Carrier }, new[] { tile.Coordinate }));
            Assert.That(hero.HasActiveEffectMark, Is.True);
            Assert.That(tile.HasActiveEffectAura, Is.True);

            view.Render(Model(heroes, Array.Empty<HeroId>(), Array.Empty<HexCoordinate>()));
            Assert.That(hero.HasActiveEffectMark, Is.False);
            Assert.That(tile.HasActiveEffectAura, Is.False);
        }

        /// <summary>Exactly the disk's own spaces are tinted, and no others.</summary>
        [Test]
        public void ExactlyTheDisksOwnSpacesAreTinted()
        {
            var inside = Tile(new HexCoordinate(0, 0));
            var alsoInside = Tile(new HexCoordinate(1, -1));
            var outside = Tile(new HexCoordinate(4, 0));
            Build(new[] { inside, alsoInside, outside }, Array.Empty<PrimitiveHeroView>());

            view.Render(Model(
                Array.Empty<HeroOccupancy>(),
                Array.Empty<HeroId>(),
                new[] { inside.Coordinate, alsoInside.Coordinate }));

            Assert.That(inside.HasActiveEffectAura, Is.True);
            Assert.That(alsoInside.HasActiveEffectAura, Is.True);
            Assert.That(outside.HasActiveEffectAura, Is.False);
        }

        /// <summary>
        /// THE OVERLAP RULE. A space that is both inside a live disk and a legal Movement destination
        /// keeps its green six-corner mark AND wears the violet tint, on two separate layers at two
        /// separate heights. Gold means "waiting on you", violet means "an effect is running here", and
        /// where both apply both show.
        ///
        /// <para>It also pins the separation that makes every OTHER fixture in this project still
        /// true: <see cref="HexTileView.IsHighlighted"/> - the retired choice fill a dozen journeys
        /// assert is false on every destination they click - stays false while the aura is drawn,
        /// because the aura is not that fill and never touches it.</para>
        /// </summary>
        [Test]
        public void AnAuraNeverDisplacesTheMarkThatWasAlreadyOnThatSpace()
        {
            var contested = Tile(new HexCoordinate(0, 0));
            Build(new[] { contested }, Array.Empty<PrimitiveHeroView>());

            view.Render(new BoardInteractionViewModel(
                1,
                Array.Empty<HeroOccupancy>(),
                Array.Empty<MinionOccupancy>(),
                new[] { contested.Coordinate },
                Array.Empty<HeroId>(),
                Array.Empty<MinionId>(),
                default,
                activeEffects: new ActiveEffectVisibilityModel(
                    Array.Empty<HeroId>(), new[] { contested.Coordinate })));

            Assert.That(contested.CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Movement),
                "The movement affordance is untouched by an aura beneath it.");
            Assert.That(contested.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
            Assert.That(contested.HasActiveEffectAura, Is.True,
                "and the aura is still drawn, under it.");
            Assert.That(contested.IsHighlighted, Is.False,
                "The aura is its own layer and must never borrow the retired choice fill's flag.");
            Assert.That(
                contested.transform.Find(HexTileView.ActiveEffectAuraLayerName).localPosition.y,
                Is.LessThan(0.015f),
                "and it lies under the tile's own perimeter, the corner marks and every piece.");
        }

        /// <summary>
        /// The violet ring is drawn INSIDE the acting-hero gold and below it, so a hero who is both
        /// being waited on and carrying an effect wears two separate rings rather than one band that
        /// hides the other. Nothing here animates: the acting gold keeps its conductor, and a static
        /// tint needs none.
        /// </summary>
        [Test]
        public void TheActingRingMovesOutsideAnEffectAndReturnsBesideTheTokenWhenItEnds()
        {
            var tile = Tile(new HexCoordinate(0, 0));
            var hero = Hero(Carrier, tile.Coordinate);
            Build(new[] { tile }, new[] { hero });
            var heroes = new[] { hero.Occupancy };
            view.Render(Model(heroes, new[] { Carrier }, Array.Empty<HexCoordinate>(), focused: Carrier));
            var acting = view.ActingHeroMarker;
            Assert.That(
                acting.VisibleRadius * (1f - ActingHeroMarker.RingWidthRatio),
                Is.GreaterThan(hero.ActiveEffectMarker.VisibleRadius),
                "Token, violet aura, then gold activity; neither band overlaps.");
            Assert.That(hero.ActiveEffectMarker.VisibleRadius / hero.SelectionRadius,
                Is.EqualTo(1.18f).Within(0.0001f), "The effect hugs the token.");
            Assert.That(ActiveEffectHeroMarker.SurfaceOffset,
                Is.LessThan(ActingHeroMarker.SurfaceOffset));
            view.Render(Model(heroes, Array.Empty<HeroId>(), Array.Empty<HexCoordinate>(), focused: Carrier));
            Assert.That(view.ActingHeroMarker, Is.SameAs(acting));
            Assert.That(acting.VisibleRadius / hero.SelectionRadius,
                Is.EqualTo(ActingHeroMarker.RadiusMargin).Within(0.0001f));
            Assert.That(hero.HasActiveEffectMark, Is.False);
            view.Render(Model(heroes, new[] { Carrier }, Array.Empty<HexCoordinate>(), focused: Carrier));
            Assert.That(acting.VisibleRadius * (1f - ActingHeroMarker.RingWidthRatio),
                Is.GreaterThan(hero.ActiveEffectMarker.VisibleRadius));
            Assert.That(acting.OwnedNativeResourceCount, Is.EqualTo(2));
            view.Render(Model(heroes, new[] { Carrier }, Array.Empty<HexCoordinate>()));
            Assert.That(acting.IsVisible, Is.False);
            Assert.That(hero.HasActiveEffectMark, Is.True);
        }

        [Test]
        public void TheAuraUsesTheShippedTransparentVariantWithoutChangingItsKeywords()
        {
            var tile = Tile(new HexCoordinate(0, 0));
            Build(new[] { tile }, Array.Empty<PrimitiveHeroView>());
            view.Render(Model(Array.Empty<HeroOccupancy>(), Array.Empty<HeroId>(), new[] { tile.Coordinate }));
            var material = view.ActiveEffectAuraMaterial;
            var template = Resources.Load<Material>(BoardOverlayMaterial.UnlitOverlayTemplateResourcePath);
            Assert.That(material.shader, Is.SameAs(template.shader),
                "Use the shipped Unlit alpha blend, not Lit's specular-preserving premultiplied variant.");
            Assert.That(material.shaderKeywords, Is.EquivalentTo(template.shaderKeywords));
            Assert.That(material.GetInt("_SrcBlend"), Is.EqualTo((int)UnityEngine.Rendering.BlendMode.SrcAlpha));
            Assert.That(material.GetInt("_DstBlend"), Is.EqualTo((int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha));
            Assert.That(material.GetInt("_ZWrite"), Is.Zero);
        }

        /// <summary>
        /// The violet cannot be mistaken for anything else already on this board: not a six-corner
        /// mark, not the acting-hero gold, and not the awaited-shine family.
        /// </summary>
        [Test]
        public void TheActiveVioletIsNoOtherBoardColour()
        {
            var accent = MatchHudTheme.ActiveEffectAccent;
            var others = new[]
            {
                HexCornerHighlightTemplate.MovementColor,
                HexCornerHighlightTemplate.AttackColor,
                HexCornerHighlightTemplate.RespawnColor,
                HexCornerHighlightTemplate.AllyColor,
                HexCornerHighlightTemplate.EffectColor,
                HexCornerHighlightTemplate.EffectResultColor,
                HexCornerHighlightTemplate.DefensePositiveColor,
                HexCornerHighlightTemplate.DefenseNegativeColor,
                MatchHudTheme.ActingHeroMarkerColor,
                MatchHudTheme.ShineRim,
                MatchHudTheme.ShineGlow,
                MatchHudTheme.ShineHalo
            };

            foreach (var other in others)
            {
                Assert.That(
                    Mathf.Abs(accent.r - other.r) + Mathf.Abs(accent.g - other.g) +
                    Mathf.Abs(accent.b - other.b),
                    Is.GreaterThan(0.25f),
                    "The Active violet must not be confusable with an existing board colour.");
            }
        }

        /// <summary>
        /// A board with nothing running generates no material at all, and what it does generate is
        /// released with the board.
        /// </summary>
        [Test]
        public void TheAuraMaterialIsGeneratedOnFirstUseAndReleasedWithTheBoard()
        {
            var tile = Tile(new HexCoordinate(0, 0));
            Build(new[] { tile }, Array.Empty<PrimitiveHeroView>());

            view.Render(Model(
                Array.Empty<HeroOccupancy>(), Array.Empty<HeroId>(), Array.Empty<HexCoordinate>()));
            Assert.That(view.ActiveEffectAuraMaterial, Is.Null);
            Assert.That(view.ActiveEffectAuraMesh, Is.Null);
            Assert.That(view.OwnedNativeResourceCount, Is.Zero);

            view.Render(Model(
                Array.Empty<HeroOccupancy>(), Array.Empty<HeroId>(), new[] { tile.Coordinate }));
            Assert.That(view.ActiveEffectAuraMaterial, Is.Not.Null);
            Assert.That(view.ActiveEffectAuraMesh, Is.Not.Null);
            Assert.That(view.OwnedNativeResourceCount, Is.EqualTo(2),
                "One shared tint and one shared hex mesh, however many spaces a disk covers.");

            view.ReleaseOwnedResources();
            Assert.That(view.ActiveEffectAuraMaterial, Is.Null);
            Assert.That(view.ActiveEffectAuraMesh, Is.Null);
            Assert.That(view.OwnedNativeResourceCount, Is.Zero);
            Assert.That(tile.HasActiveEffectAura, Is.False,
                "and no tile is left pointing at a released mesh.");
        }

        /// <summary>
        /// A piece keeps one marker however many times the board is rebuilt, and releases it with the
        /// piece - the ownership rule every generated board child in this project follows.
        /// </summary>
        [Test]
        public void OnePieceOwnsExactlyOneMarkerAcrossRepeatedRenders()
        {
            var tile = Tile(new HexCoordinate(0, 0));
            var hero = Hero(Carrier, tile.Coordinate);
            Build(new[] { tile }, new[] { hero });
            var heroes = new[] { new HeroOccupancy(Carrier, tile.Coordinate) };

            for (var pass = 1; pass <= 3; pass++)
            {
                view.Render(Model(heroes, new[] { Carrier }, Array.Empty<HexCoordinate>(), pass));
            }

            var markers = hero.GetComponentsInChildren<ActiveEffectHeroMarker>(true);
            Assert.That(markers.Length, Is.EqualTo(1));
            Assert.That(markers[0].OwnedNativeResourceCount, Is.EqualTo(2));
            Assert.That(ActiveEffectHeroMarker.GetOrCreate(hero.transform),
                Is.SameAs(markers[0]),
                "A second request finds the retained marker rather than creating a rival.");
        }

        /// <summary>
        /// THE GLIDE. While the motion system has a carrier's token displaced, that carrier's whole
        /// disk is displaced with it - the user's follow-up, "Does it fly with animation?"
        ///
        /// <para>The displacement is not computed here and no clock is started: the fixture writes the
        /// piece's transform exactly as <c>KineticCueUnityView.ApplyHero</c> does
        /// (<c>authoritative + offset</c>), and the board reads the offset back out. That is the same
        /// composition the 8.35z11 motion fixtures use, and it is why this needs no conductor.</para>
        /// </summary>
        [Test]
        public void MidSlide_TheDiskTravelsWithItsCarriersAnimatedToken()
        {
            var centre = Tile(new HexCoordinate(0, 0));
            var edge = Tile(new HexCoordinate(1, 0));
            var hero = Hero(Carrier, centre.Coordinate);
            Build(new[] { centre, edge }, new[] { hero });
            RenderCarriedAura(hero, centre, edge);

            // Half-way along a one-hex step, written the way the kinetic layer writes it.
            var step = HexWorldLayout.ToWorld(new HexCoordinate(1, 0), 1f) -
                HexWorldLayout.ToWorld(new HexCoordinate(0, 0), 1f);
            var displacement = -step * 0.5f;
            hero.transform.localPosition = hero.AuthoritativeLocalPosition + displacement;

            view.GlideActiveEffectAuras();

            foreach (var tile in new[] { centre, edge })
            {
                Assert.That(tile.ActiveEffectAuraGlide.x,
                    Is.EqualTo(displacement.x).Within(0.0001f));
                Assert.That(tile.ActiveEffectAuraGlide.z,
                    Is.EqualTo(displacement.z).Within(0.0001f));
            }

            Assert.That(hero.KineticDisplacement.x, Is.EqualTo(displacement.x).Within(0.0001f),
                "and the offset the board copied is the one the piece is actually carrying.");
        }

        /// <summary>
        /// When the slide completes the piece is back at its authoritative placement, so the disk sits
        /// exactly on its hex-derived spaces with no offset left over. There is nothing to snap.
        /// </summary>
        [Test]
        public void WhenTheSlideCompletes_TheDiskSitsExactlyOnItsHexDerivedSpaces()
        {
            var centre = Tile(new HexCoordinate(0, 0));
            var hero = Hero(Carrier, centre.Coordinate);
            Build(new[] { centre }, new[] { hero });
            RenderCarriedAura(hero, centre);

            hero.transform.localPosition = hero.AuthoritativeLocalPosition + new Vector3(0.4f, 0f, 0.3f);
            view.GlideActiveEffectAuras();
            Assert.That(centre.ActiveEffectAuraGlide, Is.Not.EqualTo(Vector3.zero));

            hero.ApplyLayout();
            view.GlideActiveEffectAuras();

            Assert.That(hero.KineticDisplacement.magnitude, Is.LessThan(0.0001f));
            Assert.That(centre.ActiveEffectAuraGlide.magnitude, Is.LessThan(0.0001f));
            Assert.That(centre.HasActiveEffectAura, Is.True,
                "and the settled disk is still the hex-derived one it always was.");
        }

        /// <summary>
        /// A glide moves a disk across the board and never out of its own layer. The height is the
        /// overlap rule's and a motion may not spend it: even a displacement with a vertical component
        /// leaves the tint at exactly its surface offset, under every mark and every piece.
        /// </summary>
        [Test]
        public void AGlideNeverLiftsTheAuraOutOfTheLayerItMustStayUnder()
        {
            var tile = Tile(new HexCoordinate(0, 0));
            var hero = Hero(Carrier, tile.Coordinate);
            Build(new[] { tile }, new[] { hero });
            RenderCarriedAura(hero, tile);

            hero.transform.localPosition =
                hero.AuthoritativeLocalPosition + new Vector3(0.3f, 5f, 0.2f);
            view.GlideActiveEffectAuras();

            Assert.That(tile.ActiveEffectAuraGlide.y, Is.EqualTo(0f));
            Assert.That(
                tile.transform.Find(HexTileView.ActiveEffectAuraLayerName).localPosition.y,
                Is.EqualTo(HexTileView.ActiveEffectAuraSurfaceOffset).Within(0.0001f));
        }

        /// <summary>A board with nothing playing writes a zero offset, exactly as it writes a still piece.</summary>
        [Test]
        public void AStillBoardWritesNoGlideAtAll()
        {
            var tile = Tile(new HexCoordinate(0, 0));
            var hero = Hero(Carrier, tile.Coordinate);
            Build(new[] { tile }, new[] { hero });
            RenderCarriedAura(hero, tile);

            view.GlideActiveEffectAuras();

            Assert.That(tile.ActiveEffectAuraGlide, Is.EqualTo(Vector3.zero));
        }

        /// <summary>
        /// One hero's slide moves only that hero's disk. A second carrier standing still keeps its own
        /// disk on its spaces, which is what makes this a per-disk offset rather than a board-wide one.
        /// </summary>
        [Test]
        public void OneCarriersSlideMovesOnlyItsOwnDisk()
        {
            var moving = Tile(new HexCoordinate(0, 0));
            var still = Tile(new HexCoordinate(5, 0));
            var mover = Hero(Carrier, moving.Coordinate);
            var stander = Hero(Bystander, still.Coordinate);
            Build(new[] { moving, still }, new[] { mover, stander });

            view.Render(new BoardInteractionViewModel(
                1,
                new[]
                {
                    new HeroOccupancy(Carrier, moving.Coordinate),
                    new HeroOccupancy(Bystander, still.Coordinate)
                },
                Array.Empty<MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<MinionId>(),
                default,
                activeEffects: new ActiveEffectVisibilityModel(
                    Array.Empty<HeroId>(),
                    new[]
                    {
                        new ActiveEffectAuraViewModel(
                            Carrier, moving.Coordinate, 0, new[] { moving.Coordinate }),
                        new ActiveEffectAuraViewModel(
                            Bystander, still.Coordinate, 0, new[] { still.Coordinate })
                    })));

            mover.transform.localPosition =
                mover.AuthoritativeLocalPosition + new Vector3(0.5f, 0f, 0f);
            view.GlideActiveEffectAuras();

            Assert.That(moving.ActiveEffectAuraGlide.x, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(still.ActiveEffectAuraGlide, Is.EqualTo(Vector3.zero));
        }

        /// <summary>
        /// Renders one disk carried by <paramref name="hero"/> over the given spaces - the shape the
        /// presenter produces, as opposed to the anonymous space-only disk a simpler fixture states.
        /// </summary>
        private void RenderCarriedAura(PrimitiveHeroView hero, params HexTileView[] tiles)
        {
            var occupancy = hero.Occupancy;
            view.Render(new BoardInteractionViewModel(
                1,
                new[] { occupancy },
                Array.Empty<MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<MinionId>(),
                default,
                activeEffects: new ActiveEffectVisibilityModel(
                    Array.Empty<HeroId>(),
                    new[]
                    {
                        new ActiveEffectAuraViewModel(
                            occupancy.HeroId,
                            occupancy.Coordinate,
                            1,
                            tiles.Select(value => value.Coordinate))
                    })));
        }

        private static void AssertSameHue(Color measured, Color expected, string what)
        {
            Assert.That(measured.r, Is.EqualTo(expected.r).Within(0.0001f), what + " red");
            Assert.That(measured.g, Is.EqualTo(expected.g).Within(0.0001f), what + " green");
            Assert.That(measured.b, Is.EqualTo(expected.b).Within(0.0001f), what + " blue");
        }

        private HexTileView Tile(HexCoordinate coordinate)
        {
            var gameObject = new GameObject("Tile " + coordinate);
            owned.Add(gameObject);
            var tile = gameObject.AddComponent<HexTileView>();
            tile.Configure(coordinate, 1f);
            return tile;
        }

        private PrimitiveHeroView Hero(HeroId heroId, HexCoordinate coordinate)
        {
            var gameObject = new GameObject("Hero " + heroId.Value);
            owned.Add(gameObject);
            var hero = gameObject.AddComponent<PrimitiveHeroView>();
            hero.Configure(new HeroOccupancy(heroId, coordinate), 1f);
            return hero;
        }

        private void Build(
            IEnumerable<HexTileView> tiles, IEnumerable<PrimitiveHeroView> heroes)
        {
            template = new HexCornerHighlightTemplate(1f);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            owned.Add(material);
            view = new BoardInteractionUnityView(
                heroes, tiles, material, sharedCornerHighlightTemplate: template);
        }

        private static BoardInteractionViewModel Model(
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<HeroId> markedHeroes,
            IEnumerable<HexCoordinate> auraSpaces,
            long version = 1, HeroId focused = default) =>
            new BoardInteractionViewModel(
                version,
                heroes,
                Array.Empty<MinionOccupancy>(),
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<MinionId>(),
                focused,
                activeEffects: new ActiveEffectVisibilityModel(markedHeroes, auraSpaces));
    }
}
