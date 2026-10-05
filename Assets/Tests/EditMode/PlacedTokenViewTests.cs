using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.2d: what the board does with a projected token.
    ///
    /// The discipline under test is the retained-view one hero and minion views already follow -
    /// projection presence owns visibility, no view is destroyed to hide a piece, and a projected
    /// identity the board cannot draw is a loud fault rather than a silently missing piece. What is
    /// new is where a view comes from, because a token identity is minted when an effect places one
    /// and so cannot be built from a declared roster ahead of the match.
    /// </summary>
    public sealed class PlacedTokenViewTests
    {
        [Test]
        public void EmmittGlitchLoadsItsPrivateFaceAndKeepsFallbackReadable()
        {
            var definition = EmmittComponentContent.Glitch;
            Assert.That(PlacedTokenArtwork.TryLoadTexture(definition.Id, out var face), Is.True);
            Assert.That(face.width, Is.GreaterThan(0)); Assert.That(face.height, Is.GreaterThan(0));
            Assert.That(PlacedTokenArtwork.Caption(definition, true), Is.Empty);
            Assert.That(PlacedTokenArtwork.Caption(definition, false), Is.EqualTo("GLITCH"));
            using (var board = new TokenBoard(new HexCoordinate(1, 0)))
            {
                board.Render(1, Token("glitch", definition, new HexCoordinate(1, 0)));
                Assert.That(board.View.TokenViews.Single().GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.SameAs(face));
            }
        }

        private static readonly HeroId Owner = new HeroId("token-owner-hero");
        private static readonly CardInstanceId Source = new CardInstanceId("token-source-card");

        /// <summary>
        /// Milestone 9.18: "i think all of our tokens need some sort of thin black outline to pop out
        /// more." Every token kind this build's catalog registers - read from the catalog, never
        /// listed here - is drawn standing on a black outline exactly one
        /// <see cref="BoardPieceOutline"/> width wider than the piece on every side, with its top
        /// under the piece's so only the rim shows.
        /// </summary>
        [Test]
        public void EveryCatalogTokenKind_IsDrawnWithTheThinBlackOutline()
        {
            var catalog = MatchContentCatalogs.ForThisBuild().Components;
            var kinds = catalog.RegisteredTokens.Select(catalog.LoadToken).ToArray();
            Assert.That(kinds, Is.Not.Empty, "The catalog registered no token kind, so this would pass vacuously.");
            const float radius = 1.25f;
            var expectedWidth = BoardPieceOutline.WorldWidth(radius);
            foreach (var kind in kinds)
            {
                var gameObject = new GameObject("Outline " + kind.Id.Value);
                try
                {
                    var view = gameObject.AddComponent<PlacedTokenView>();
                    view.Configure(new PlacedToken(new TokenInstanceId("outline-" + kind.Id.Value), kind,
                        Owner, Owner, Source, new HexCoordinate(0, 0)), radius);
                    var outline = gameObject.transform.Find("Placed Token Outline");
                    Assert.That(outline, Is.Not.Null, kind.Id.Value + " is drawn with no outline.");
                    var renderer = outline.GetComponent<MeshRenderer>();
                    Assert.That(outline.gameObject.activeInHierarchy && renderer.enabled, Is.True,
                        kind.Id.Value + "'s outline is not drawn.");
                    Assert.That(renderer.sharedMaterial.color, Is.EqualTo((Color)BoardPieceOutline.Colour),
                        kind.Id.Value + "'s outline is not black.");

                    var piece = gameObject.GetComponent<MeshFilter>().sharedMesh.bounds;
                    var rim = outline.GetComponent<MeshFilter>().sharedMesh.bounds;
                    // A pointy-top hex's flat-to-flat width runs along x: the outline adds exactly
                    // one line width on each side, the same for every kind.
                    Assert.That(rim.extents.x - piece.extents.x, Is.EqualTo(expectedWidth).Within(0.0005f),
                        kind.Id.Value + "'s outline is not the board's one line width.");
                    Assert.That(rim.max.y, Is.LessThan(piece.max.y),
                        kind.Id.Value + "'s outline would cover its own face.");
                }
                finally
                {
                    Object.DestroyImmediate(gameObject);
                }
            }
        }

        [Test]
        public void TalisFiveGreenCardsRenderThePreparedIceAndTotemFaces()
        {
            var roles = new[]
            {
                TaliComponentContent.IceDefinition,
                TaliComponentContent.AncestralTotemDefinition,
                TaliComponentContent.VeneratedTotemDefinition
            };
            using (var board = new TokenBoard(new HexCoordinate(1, 0)))
            {
                for (var index = 0; index < roles.Length; index++)
                {
                    var definition = roles[index];
                    Assert.That(PlacedTokenArtwork.TryLoadTexture(definition.Id, out var face),
                        Is.True, definition.Id.Value + " must have its prepared private face.");
                    board.Render(index + 1, Token("tali-green-" + index, definition,
                        new HexCoordinate(1, 0)));
                    var view = board.View.TokenViews.Single();
                    Assert.That(view.GetComponent<MeshRenderer>().sharedMaterial.mainTexture,
                        Is.SameAs(face),
                        definition.Id.Value + " must show its face after a pooled-view refresh.");
                }
            }
        }

        /// <summary>
        /// Milestone 9.25, the user on the 2026-09-30 playtest board: "can you remove totem and ice
        /// text from these ones as well". Tali's three kinds print their own faces, so none wears a
        /// caption over it; a kind drawn without its prepared face keeps its name.
        /// </summary>
        [Test]
        public void TalisIceAndTotems_WearNoCaptionOverTheirPreparedFaces()
        {
            var roles = new[]
            {
                TaliComponentContent.IceDefinition,
                TaliComponentContent.AncestralTotemDefinition,
                TaliComponentContent.VeneratedTotemDefinition
            };
            using (var board = new TokenBoard(new HexCoordinate(1, 0)))
            {
                for (var index = 0; index < roles.Length; index++)
                {
                    var definition = roles[index];
                    Assert.That(PlacedTokenArtwork.Caption(definition, true), Is.Empty,
                        definition.Id.Value + " must wear no caption over its prepared face.");
                    Assert.That(PlacedTokenArtwork.Caption(definition, false),
                        Is.EqualTo(definition.DisplayName.ToUpperInvariant()),
                        definition.Id.Value + " must keep its name when its face is absent.");
                    Assert.That(PlacedTokenArtwork.TryLoadTexture(definition.Id, out _),
                        Is.True, definition.Id.Value + " must have its prepared private face.");
                    board.Render(index + 1, Token("tali-caption-" + index, definition,
                        new HexCoordinate(1, 0)));
                    Assert.That(board.View.TokenViews.Single().NameText, Is.Empty,
                        definition.Id.Value + " is drawn under a caption.");
                }
            }
        }

        /// <summary>
        /// Milestone 9.25: "make them all a bit bigger". Every catalog kind's piece, outline
        /// included, still ends inside its space with a tenth of the radius to spare at the corners
        /// and at the flats, so the space's own edge and a destination highlight stay visible
        /// around it whatever the piece's share of the space is set to.
        /// </summary>
        [Test]
        public void EveryCatalogTokenKind_LeavesTheSpacesEdgeVisibleAroundItsOutline()
        {
            var catalog = MatchContentCatalogs.ForThisBuild().Components;
            var kinds = catalog.RegisteredTokens.Select(catalog.LoadToken).ToArray();
            Assert.That(kinds, Is.Not.Empty, "The catalog registered no token kind, so this would pass vacuously.");
            const float radius = 1.25f;
            var cornerLimit = radius * 0.9f;
            var flatLimit = radius * Mathf.Cos(30f * Mathf.Deg2Rad) * 0.9f;
            foreach (var kind in kinds)
            {
                var gameObject = new GameObject("Fit " + kind.Id.Value);
                try
                {
                    var view = gameObject.AddComponent<PlacedTokenView>();
                    view.Configure(new PlacedToken(new TokenInstanceId("fit-" + kind.Id.Value), kind,
                        Owner, Owner, Source, new HexCoordinate(0, 0)), radius);
                    var outline = gameObject.transform.Find("Placed Token Outline");
                    Assert.That(outline, Is.Not.Null, kind.Id.Value + " is drawn with no outline.");
                    var rim = outline.GetComponent<MeshFilter>().sharedMesh.bounds;
                    // A pointy-top hex reaches its corners along z and its flats along x.
                    Assert.That(rim.extents.z, Is.LessThanOrEqualTo(cornerLimit),
                        kind.Id.Value + "'s outline reaches the space's corners.");
                    Assert.That(rim.extents.x, Is.LessThanOrEqualTo(flatLimit),
                        kind.Id.Value + "'s outline reaches the space's flats.");
                }
                finally
                {
                    Object.DestroyImmediate(gameObject);
                }
            }
        }

        [Test]
        public void PyrosPreparedArtworkHasNoNameOverlayAndPooledFallbackKeepsItsName()
        {
            Assert.That(PlacedTokenArtwork.Caption(WidgetComponentContent.PyroDefinition, false), Is.EqualTo("PYRO"));
            Assert.That(PlacedTokenArtwork.TryLoadTexture(WidgetComponentContent.PyroDefinition.Id, out var face), Is.True);
            using (var board = new TokenBoard(new HexCoordinate(1, 0)))
            {
                board.Render(1, Token("pyro", WidgetComponentContent.PyroDefinition, new HexCoordinate(1, 0)));
                var view = board.View.TokenViews.Single();
                Assert.That(view.NameText, Is.Empty, "Pyro's artwork must not be covered by a PYRO caption.");
                Assert.That(view.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.SameAs(face));
                view.Configure(Token("hazard", SyntheticComponentDefinitions.HazardDefinition, new HexCoordinate(1, 0)),
                    HexWorldLayout.DefaultOuterRadius);
                Assert.That(view.NameText, Is.EqualTo("TEST - HAZARD"));
                Assert.That(view.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.Null);
            }
        }

        /// <summary>
        /// Milestone 8.75b. Ignatia the Mad's magma, on Pyro's terms and Min's: the prepared face is
        /// a pool of molten rock on a hex and says what the piece is without a word over it, and a
        /// clean checkout with no private artwork keeps the readable fallback disc. Hers is the first
        /// real token face a dealing has added since Min's, and the caption matters more on her board
        /// than on any before it - her three Paths can leave three magma tokens on ONE file, and
        /// three stacked captions is a board a player reads slower than the art.
        /// </summary>
        [Test]
        public void MagmasPreparedArtworkHasNoNameOverlayAndTheCleanCheckoutKeepsItsName()
        {
            Assert.That(
                PlacedTokenArtwork.Caption(IgnatiaMagmaContent.MagmaTokenDefinition, false),
                Is.EqualTo("MAGMA"),
                "The absence-tolerant contract: with no prepared face the disc keeps its name.");
            Assert.That(
                PlacedTokenArtwork.ResourcePath(IgnatiaMagmaContent.Magma),
                Is.EqualTo("Private/Tokens/ignatia-magma"),
                "addressed by definition id and nothing else.");
            Assert.That(
                PlacedTokenArtwork.Caption(IgnatiaMagmaContent.MagmaTokenDefinition, true),
                Is.Empty,
                "and with one prepared, the art speaks for itself.");

            Assert.That(
                PlacedTokenArtwork.TryLoadTexture(IgnatiaMagmaContent.Magma, out var face), Is.True,
                "The artwork block prepared it under `Resources/Private/Tokens/`; a worktree " +
                "without the private corpus is the one state this case cannot run in.");
            using (var board = new TokenBoard(new HexCoordinate(1, 0)))
            {
                board.Render(1, Token(
                    "magma", IgnatiaMagmaContent.MagmaTokenDefinition, new HexCoordinate(1, 0)));
                var view = board.View.TokenViews.Single();
                Assert.That(view.NameText, Is.Empty,
                    "Magma's artwork must not be covered by a MAGMA caption.");
                Assert.That(
                    view.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.SameAs(face));
            }
        }

        [Test]
        public void APlacedToken_IsDrawnAtItsCoordinateAndNamedByItsDefinition()
        {
            using (var board = new TokenBoard(new HexCoordinate(1, 0)))
            {
                var token = Token("hazard-1", SyntheticComponentDefinitions.HazardDefinition,
                    new HexCoordinate(1, 0));

                board.Render(1, token);

                var view = board.View.TokenViews.Single();
                Assert.That(view.gameObject.activeSelf, Is.True);
                Assert.That(view.BoundTokenId, Is.EqualTo(token.Id));
                Assert.That(view.Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));
                Assert.That(view.NameText, Is.EqualTo("TEST - HAZARD"),
                    "The name comes off the projected instance's trusted definition, so a token kind " +
                    "this view has never seen still reads correctly.");
                Assert.That(view.transform.localPosition,
                    Is.EqualTo(HexWorldLayout.ToWorld(
                            new HexCoordinate(1, 0), HexWorldLayout.DefaultOuterRadius) +
                        (Vector3.up * PlacedTokenView.DefaultVerticalOffset)));
            }
        }

        /// <summary>
        /// A Remove, a Clear and an Undo all reach the board the same way: the instance leaves the
        /// projection. The view is kept and hidden rather than destroyed, and it is bound again when
        /// the same instance comes back - which is what makes restore, replay and retained-prefix
        /// Undo redraw the piece rather than leave a hole.
        /// </summary>
        [Test]
        public void ARemovedToken_StopsBeingDrawnAndReturnsWithItsOwnIdentity()
        {
            using (var board = new TokenBoard(new HexCoordinate(1, 0)))
            {
                var token = Token("hazard-1", SyntheticComponentDefinitions.HazardDefinition,
                    new HexCoordinate(1, 0));
                board.Render(1, token);
                var view = board.View.TokenViews.Single();

                board.Render(2);

                Assert.That(view.gameObject.activeSelf, Is.False);
                Assert.That(board.View.TokenViews, Has.Count.EqualTo(1),
                    "The view is retained, exactly as a removed minion's view is.");
                Assert.That(board.Created, Is.EqualTo(1));

                board.Render(3, token);

                Assert.That(board.View.TokenViews.Single(), Is.SameAs(view),
                    "The returning token is drawn by the pooled view rather than a new one.");
                Assert.That(view.gameObject.activeSelf, Is.True);
                Assert.That(view.BoundTokenId, Is.EqualTo(token.Id));
                Assert.That(board.Created, Is.EqualTo(1));
            }
        }

        /// <summary>
        /// A Replace is one substitution and the board draws it as one: the outgoing definition stops
        /// being drawn and the incoming one starts, in a space that is never drawn empty between them.
        /// That is the single-stage property the operation itself declares, and it needs no arm of its
        /// own here because both halves arrive in the same projection.
        /// </summary>
        [Test]
        public void AReplacedToken_SwapsWhatIsDrawnWithoutEmptyingTheSpace()
        {
            var space = new HexCoordinate(1, 0);
            using (var board = new TokenBoard(space))
            {
                board.Render(1, Token("hazard-1", SyntheticComponentDefinitions.HazardDefinition, space));
                Assert.That(board.View.TokenViews.Single().NameText, Is.EqualTo("TEST - HAZARD"));

                board.Render(2, Token("ward-1", SyntheticComponentDefinitions.WardDefinition, space));

                var drawn = board.View.TokenViews.Where(value => value.gameObject.activeSelf).ToArray();
                Assert.That(drawn, Has.Length.EqualTo(1),
                    "One piece stands there before and one after; the space is never drawn empty.");
                Assert.That(drawn[0].Coordinate, Is.EqualTo(space));
                Assert.That(drawn[0].NameText, Is.EqualTo("TEST - WARD"));
                Assert.That(board.Created, Is.EqualTo(1),
                    "The substitution reuses the pooled view rather than building a second one.");
            }
        }

        /// <summary>
        /// Milestone 8.62, at the user's direction, and this test USED TO SAY THE OPPOSITE - it was
        /// called "uses its hex corners and never reactivates the retired piece ring".
        ///
        /// <para>8.30h routed every board choice into the occupied hex's six corner marks and left the
        /// fitted piece ring built, projected and never drawn. On 2026-09-19 the user looked at an
        /// attack's targets in the packaged build: "can you make targeting highlights more visible,
        /// they are almost impossible to see like this.. Blood red light instead of this pink i
        /// think." A piece now wears the cue the projection has been computing for it all along, and
        /// the hex corners are unchanged - the two together are the target.</para>
        /// </summary>
        [Test]
        public void ATargetedToken_WearsItsOwnRingAndItsHexCornersFromOneProjectedCue()
        {
            var space = new HexCoordinate(1, 0);
            using (var board = new TokenBoard(space))
            {
                var token = Token("hazard-1", SyntheticComponentDefinitions.HazardDefinition, space);
                board.Render(1, token);
                var view = board.View.TokenViews.Single();
                var ring = view.TargetRing;
                var retainedMesh = ring.GeneratedMesh;

                Assert.That(view.TargetCueState,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.None)));
                Assert.That(ring.IsVisible, Is.False);

                board.Render(2, new[] { token },
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForToken(token.Id),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Affect));

                Assert.That(view.TargetRing, Is.SameAs(ring),
                    "One retained cue changes state in place rather than being rebuilt.");
                Assert.That(view.TargetCueState,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.Eligible)));
                Assert.That(ring.ConsequenceIntent,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(
                        BoardTargetConsequenceIntent.Affect)));
                Assert.That(ring.IsVisible,
                    Is.EqualTo(BoardTargetCuePresentation.DrawsTheRingOnThePiece));
                Assert.That(board.View.TileViews.Single().CornerHighlight.Kind,
                    Is.EqualTo(HexCornerHighlightKind.Effect));

                board.Render(3, new[] { token },
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForToken(token.Id),
                        BoardTargetCueState.Selected,
                        BoardTargetConsequenceIntent.Removal));

                Assert.That(view.TargetRing, Is.SameAs(ring));
                Assert.That(view.TargetCueState,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.Selected)));
                Assert.That(ring.ConsequenceIntent,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(
                        BoardTargetConsequenceIntent.Removal)));
                Assert.That(ring.IsVisible,
                    Is.EqualTo(BoardTargetCuePresentation.DrawsTheRingOnThePiece));
                Assert.That(board.View.TileViews.Single().CornerHighlight.Kind,
                    Is.EqualTo(HexCornerHighlightKind.Attack));

                board.Render(4, token);

                Assert.That(view.TargetRing, Is.SameAs(ring));
                Assert.That(ring.GeneratedMesh, Is.SameAs(retainedMesh));
                Assert.That(view.TargetCueState, Is.EqualTo(BoardTargetCueState.None),
                    "and a projection with no cue for this piece clears it rather than leaving a " +
                    "stale ring standing.");
                Assert.That(ring.IsVisible, Is.False);
                Assert.That(board.View.TileViews.Single().CornerHighlight.Kind,
                    Is.EqualTo(HexCornerHighlightKind.None));
            }
        }

        /// <summary>
        /// A cue is addressed by identity, so a released view cannot keep one: the piece it marked is
        /// no longer projected, and the pooled view is reused for whatever arrives next.
        /// </summary>
        [Test]
        public void AReleasedTokenView_CannotCarryAHexCornerCueToTheNextPiece()
        {
            var space = new HexCoordinate(1, 0);
            using (var board = new TokenBoard(space))
            {
                var hazard = Token("hazard-1", SyntheticComponentDefinitions.HazardDefinition, space);
                board.Render(1, new[] { hazard },
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForToken(hazard.Id),
                        BoardTargetCueState.Selected,
                        BoardTargetConsequenceIntent.Removal));
                var view = board.View.TokenViews.Single();
                var ring = view.TargetRing;
                Assert.That(view.TargetCueState,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.Selected)));
                Assert.That(board.View.TileViews.Single().CornerHighlight.Kind,
                    Is.EqualTo(HexCornerHighlightKind.Attack));

                board.Render(2);

                Assert.That(view.TargetCueState,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.None)));
                Assert.That(ring.IsVisible, Is.False);
                Assert.That(board.View.TileViews.Single().CornerHighlight.Kind,
                    Is.EqualTo(HexCornerHighlightKind.None));

                board.Render(3, Token("ward-1", SyntheticComponentDefinitions.WardDefinition, space));

                Assert.That(board.View.TokenViews.Single(), Is.SameAs(view));
                Assert.That(view.TargetRing, Is.SameAs(ring));
                Assert.That(view.TargetCueState, Is.EqualTo(BoardTargetCueState.None),
                    "The pooled view draws the incoming identity plainly rather than inheriting a cue.");
                Assert.That(board.View.TileViews.Single().CornerHighlight.Kind,
                    Is.EqualTo(HexCornerHighlightKind.None));
                Assert.That(view.GetComponentsInChildren<BoardTargetRing>(true),
                    Has.Length.EqualTo(1),
                    "Pool reuse retains exactly one cue child.");
            }
        }

        [Test]
        public void AProjectedTokenWithNoAvailableView_IsAFaultRatherThanAMissingPiece()
        {
            using (var board = new TokenBoard(new HexCoordinate(1, 0), withFactory: false))
            {
                var token = Token("hazard-1", SyntheticComponentDefinitions.HazardDefinition,
                    new HexCoordinate(1, 0));

                Assert.That(
                    () => board.Render(1, token),
                    Throws.InvalidOperationException.With.Message.Contains("projected token identity"),
                    "The equivalent minion case throws, and a token that cannot be drawn must not be " +
                    "quieter than a minion that cannot be.");
            }
        }

        private static PlacedToken Token(string id, TokenDefinition definition, HexCoordinate coordinate) =>
            new PlacedToken(
                new TokenInstanceId(id), definition, Owner, Owner, Source, coordinate);

        /// <summary>
        /// A board view with one tile and a counting token-view factory, so a case can tell a pooled
        /// view from a newly built one.
        /// </summary>
        private sealed class TokenBoard : IDisposable
        {
            private readonly List<GameObject> owned = new List<GameObject>();
            private readonly Material material;
            private readonly HexCornerHighlightTemplate cornerHighlightTemplate;

            public TokenBoard(HexCoordinate tileAt, bool withFactory = true)
            {
                material = new Material(Shader.Find("Standard"));
                cornerHighlightTemplate = new HexCornerHighlightTemplate(
                    HexWorldLayout.DefaultOuterRadius);
                var tileObject = new GameObject("Token Board Tile");
                owned.Add(tileObject);
                var tile = tileObject.AddComponent<HexTileView>();
                tile.Configure(tileAt, HexWorldLayout.DefaultOuterRadius);
                View = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(),
                    new[] { tile },
                    material,
                    resultingSpaceMaterial: null,
                    tokenViewFactory: withFactory ? Create : (Func<PlacedToken, PlacedTokenView>)null,
                    sharedCornerHighlightTemplate: cornerHighlightTemplate);
            }

            public BoardInteractionUnityView View { get; }

            public int Created { get; private set; }

            public void Render(long version, params PlacedToken[] tokens) =>
                Render(version, tokens, Array.Empty<PieceTargetCueViewModel>());

            public void Render(
                long version, PlacedToken[] tokens, params PieceTargetCueViewModel[] cues)
            {
                View.Render(new BoardInteractionViewModel(
                    version,
                    Array.Empty<HeroOccupancy>(),
                    Array.Empty<GuardsDigital.Core.Units.Minions.MinionOccupancy>(),
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<HeroId>(),
                    Array.Empty<MinionId>(),
                    default,
                    placedTokens: tokens,
                    pieceTargetCues: cues));
            }

            public void Dispose()
            {
                foreach (var value in owned)
                {
                    Object.DestroyImmediate(value);
                }

                Object.DestroyImmediate(material);
                cornerHighlightTemplate.Dispose();
            }

            private PlacedTokenView Create(PlacedToken token)
            {
                Created++;
                var tokenObject = new GameObject("Placed Token");
                owned.Add(tokenObject);
                var view = tokenObject.AddComponent<PlacedTokenView>();
                view.Configure(token, HexWorldLayout.DefaultOuterRadius);
                return view;
            }
        }
    }
}
