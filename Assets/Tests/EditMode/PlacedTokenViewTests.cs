using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
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
        private static readonly HeroId Owner = new HeroId("token-owner-hero");
        private static readonly CardInstanceId Source = new CardInstanceId("token-source-card");

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
        /// Milestone 6.3a: the piece wears the shared board cue while a control names it.
        ///
        /// Both visible kinds say "this one", so the marker is the minion prompt's and only the colour
        /// separates a chosen piece from the one being pointed at. The cue hangs from an anchor
        /// carrying the shared board-facing rotation for the reason milestone 6.5c recorded: the board
        /// is the XZ plane under a top-down camera, so a glyph at identity rotation on this upright
        /// transform would render edge-on while every local-offset assertion about it still passed.
        /// </summary>
        [Test]
        public void APointedAtToken_WearsTheSharedBoardCueAndDropsItWhenNothingNamesIt()
        {
            var space = new HexCoordinate(1, 0);
            using (var board = new TokenBoard(space))
            {
                var token = Token("hazard-1", SyntheticComponentDefinitions.HazardDefinition, space);
                board.Render(1, token);
                var view = board.View.TokenViews.Single();

                Assert.That(view.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.None));
                Assert.That(view.ChoiceCue, Is.Null, "No cue is built until one is asked for.");

                board.Render(2, new[] { token },
                    new TokenChoiceCueViewModel(token.Id, MinionChoiceCueKind.Highlighted));

                Assert.That(view.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.Highlighted));
                Assert.That(view.ChoiceCue.gameObject.activeSelf, Is.True);
                Assert.That(view.ChoiceCue.text, Is.EqualTo(BoardChoiceCue.Marker));
                // `TextMesh` quantises to 8 bits per channel, so the comparison is to that precision
                // rather than to the float the shared construction declares.
                Assert.That(view.ChoiceCue.color,
                    Is.EqualTo(BoardChoiceCue.HighlightedColor).Using(ColorComparer));
                Assert.That(view.ChoiceCue.transform.rotation,
                    Is.EqualTo(BoardChoiceCue.BoardFacingRotation).Using(RotationComparer),
                    "It faces the camera that looks down the board rather than lying edge-on to it.");

                board.Render(3, new[] { token },
                    new TokenChoiceCueViewModel(token.Id, MinionChoiceCueKind.Selected));

                Assert.That(view.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.Selected));
                Assert.That(view.ChoiceCue.color,
                    Is.EqualTo(BoardChoiceCue.SelectedColor).Using(ColorComparer));

                board.Render(4, token);

                Assert.That(view.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.None));
                Assert.That(view.ChoiceCue.gameObject.activeSelf, Is.False);
            }
        }

        /// <summary>
        /// A cue is addressed by identity, so a released view cannot keep one: the piece it marked is
        /// no longer projected, and the pooled view is reused for whatever arrives next.
        /// </summary>
        [Test]
        public void AReleasedTokenView_LosesItsCueRatherThanCarryingItToTheNextPiece()
        {
            var space = new HexCoordinate(1, 0);
            using (var board = new TokenBoard(space))
            {
                var hazard = Token("hazard-1", SyntheticComponentDefinitions.HazardDefinition, space);
                board.Render(1, new[] { hazard },
                    new TokenChoiceCueViewModel(hazard.Id, MinionChoiceCueKind.Selected));
                var view = board.View.TokenViews.Single();
                Assert.That(view.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.Selected));

                board.Render(2);

                Assert.That(view.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.None));

                board.Render(3, Token("ward-1", SyntheticComponentDefinitions.WardDefinition, space));

                Assert.That(board.View.TokenViews.Single(), Is.SameAs(view));
                Assert.That(view.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.None),
                    "The pooled view draws the incoming piece plainly rather than inheriting a cue.");
            }
        }

        private static readonly IEqualityComparer<Quaternion> RotationComparer =
            new QuaternionComparer();

        private static readonly IEqualityComparer<Color> ColorComparer = new EightBitColorComparer();

        private sealed class QuaternionComparer : IEqualityComparer<Quaternion>
        {
            public bool Equals(Quaternion left, Quaternion right) =>
                Quaternion.Angle(left, right) < 0.01f;

            public int GetHashCode(Quaternion value) => 0;
        }

        private sealed class EightBitColorComparer : IEqualityComparer<Color>
        {
            public bool Equals(Color left, Color right) =>
                Mathf.Abs(left.r - right.r) <= 1f / 255f &&
                Mathf.Abs(left.g - right.g) <= 1f / 255f &&
                Mathf.Abs(left.b - right.b) <= 1f / 255f &&
                Mathf.Abs(left.a - right.a) <= 1f / 255f;

            public int GetHashCode(Color value) => 0;
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

            public TokenBoard(HexCoordinate tileAt, bool withFactory = true)
            {
                material = new Material(Shader.Find("Standard"));
                var tileObject = new GameObject("Token Board Tile");
                owned.Add(tileObject);
                var tile = tileObject.AddComponent<HexTileView>();
                tile.Configure(tileAt, HexWorldLayout.DefaultOuterRadius);
                View = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(),
                    new[] { tile },
                    material,
                    resultingSpaceMaterial: null,
                    tokenViewFactory: withFactory ? Create : (Func<PlacedToken, PlacedTokenView>)null);
            }

            public BoardInteractionUnityView View { get; }

            public int Created { get; private set; }

            public void Render(long version, params PlacedToken[] tokens) =>
                Render(version, tokens, Array.Empty<TokenChoiceCueViewModel>());

            public void Render(
                long version, PlacedToken[] tokens, params TokenChoiceCueViewModel[] cues)
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
                    tokenChoiceCues: cues));
            }

            public void Dispose()
            {
                foreach (var value in owned)
                {
                    Object.DestroyImmediate(value);
                }

                Object.DestroyImmediate(material);
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
