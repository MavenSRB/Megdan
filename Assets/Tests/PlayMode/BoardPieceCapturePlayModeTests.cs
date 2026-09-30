using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 9.16. The board-piece VISUAL EVIDENCE the user judges a piece treatment on,
    /// captured rather than described: pieces staged on the configured match board beside a hero,
    /// photographed through the default match camera and through a close crop of the same pieces.
    /// </summary>
    /// <remarks>
    /// These cases capture; they do not judge the look - that is the user's call on the picture, as
    /// it was at 8.62. They do assert that the pieces they stage were drawn, so a capture of an empty
    /// space cannot pass for evidence. The view regressions live in the Edit Mode view fixtures.
    /// </remarks>
    public sealed class BoardPieceCapturePlayModeTests
    {
        [UnityTest]
        public IEnumerator TurretBesideTheRockAndTheTree()
        {
            yield return LoadTheMatch();
            var view = Root().Runtime.BoardView;
            var original = view.Model;
            var hero = original.Heroes.First();
            var free = FreeSpacesNearest(view, original, hero.Coordinate, 3);
            var tokens = original.PlacedTokens.Concat(new[]
            {
                Token("rock", MrakComponentContent.Rock, hero.HeroId, free[1]),
                Token("tree", WukComponentContent.Tree, hero.HeroId, free[2])
            }).ToArray();
            try
            {
                view.Render(Staged(original, tokens, new[]
                {
                    new ObstacleOccupancy(new ObstacleId("trinkets-turret:capture"), free[0])
                }));
                for (var frame = 0; frame < 3; frame++) yield return null;
                var turret = Object.FindObjectsByType<PlaceholderObstacleView>(FindObjectsInactive.Exclude)
                    .Single(value => value.Occupancy.Coordinate.Equals(free[0]));
                Assert.That(turret.transform.Find("Obstacle Standee"), Is.Not.Null,
                    "The prepared Turret resource must be present in this checkout.");
                yield return Capture(view, "tmp/9-16-turret-final",
                    new[] { free[0], free[1], free[2], hero.Coordinate });
            }
            finally
            {
                view.Render(original);
            }
        }

        /// <summary>
        /// Milestone 9.18. Every token kind this build's catalog registers, plus the Turret, staged in
        /// one cluster on the board. Derived from the catalog, never hand-listed, so a kind a later
        /// hero registers appears on the sheet without an edit here. Written to
        /// <c>tmp/9-18-tokens-capture[-close].png</c>; the before and after sheets are copies of it.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryTokenKindAndTheTurret()
        {
            yield return LoadTheMatch();
            var view = Root().Runtime.BoardView;
            var original = view.Model;
            var catalog = MatchContentCatalogs.ForThisBuild().Components;
            var kinds = catalog.RegisteredTokens.Select(catalog.LoadToken).ToArray();
            Assert.That(kinds, Is.Not.Empty);
            var owner = original.Heroes.First().HeroId;
            var free = FreeSpacesNearest(view, original, OpenestSpace(view, original), kinds.Length + 1);
            var tokens = original.PlacedTokens.Concat(kinds.Select((kind, index) =>
                Token(kind.Id.Value, kind, owner, free[index + 1]))).ToArray();
            try
            {
                view.Render(Staged(original, tokens, new[]
                {
                    new ObstacleOccupancy(new ObstacleId("trinkets-turret:capture"), free[0])
                }));
                for (var frame = 0; frame < 3; frame++) yield return null;
                Assert.That(Object.FindObjectsByType<PlacedTokenView>(FindObjectsInactive.Exclude)
                    .Count(piece => piece.Token != null && piece.Token.Id.Value.StartsWith("capture-",
                        StringComparison.Ordinal)), Is.EqualTo(kinds.Length));
                Debug.Log("GUARDS_9_18 staged " + string.Join(", ", kinds.Select(kind => kind.Id.Value)));
                yield return Capture(view, "tmp/9-18-tokens-capture", free);
            }
            finally
            {
                view.Render(original);
            }
        }

        /// <summary>The free playable space with the most free playable spaces around it.</summary>
        private static HexCoordinate OpenestSpace(BoardInteractionUnityView view, BoardInteractionViewModel model)
        {
            var radius = view.TileViews[0].OuterRadius;
            var occupied = new HashSet<HexCoordinate>(model.Heroes.Select(value => value.Coordinate)
                .Concat(model.Minions.Select(value => value.Coordinate))
                .Concat(model.PlacedTokens.Select(value => value.Coordinate)));
            var free = view.TileViews.Where(tile => tile.IsPlayable && !occupied.Contains(tile.Coordinate))
                .Select(tile => (tile.Coordinate, At: HexWorldLayout.ToWorld(tile.Coordinate, radius)))
                .ToArray();
            var reach = 2.6f * Mathf.Sqrt(3f) * radius;
            return free.OrderByDescending(space => free.Count(other => Vector3.Distance(other.At, space.At) <= reach))
                .ThenBy(space => space.Coordinate.Q).ThenBy(space => space.Coordinate.R)
                .First().Coordinate;
        }

        private static IEnumerator LoadTheMatch()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone) yield return null;
            for (var frame = 0; frame < 10; frame++) yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }

        private static HexCoordinate[] FreeSpacesNearest(
            BoardInteractionUnityView view, BoardInteractionViewModel model, HexCoordinate centre, int count)
        {
            var radius = view.TileViews[0].OuterRadius;
            var occupied = new HashSet<HexCoordinate>(model.Heroes.Select(value => value.Coordinate)
                .Concat(model.Minions.Select(value => value.Coordinate))
                .Concat(model.PlacedTokens.Select(value => value.Coordinate))
                .Concat(model.Obstacles.Select(value => value.Coordinate)));
            var origin = HexWorldLayout.ToWorld(centre, radius);
            var free = view.TileViews
                .Where(tile => tile.IsPlayable && !occupied.Contains(tile.Coordinate))
                .OrderBy(tile => Vector3.Distance(HexWorldLayout.ToWorld(tile.Coordinate, radius), origin))
                .Select(tile => tile.Coordinate)
                .Take(count)
                .ToArray();
            Assert.That(free.Length, Is.EqualTo(count), "The board has too few free spaces to stage on.");
            return free;
        }

        private static PlacedToken Token(string name, TokenDefinition definition, HeroId owner, HexCoordinate at) =>
            new PlacedToken(new TokenInstanceId("capture-" + name), definition, owner, owner,
                new CardInstanceId("capture-source"), at);

        private static BoardInteractionViewModel Staged(
            BoardInteractionViewModel original,
            IEnumerable<PlacedToken> tokens,
            IEnumerable<ObstacleOccupancy> obstacles) =>
            new BoardInteractionViewModel(original.Version, original.Heroes, original.Minions,
                Array.Empty<HexCoordinate>(), Array.Empty<HeroId>(), Array.Empty<Core.Setup.MinionId>(),
                default, placedTokens: tokens, obstacles: obstacles);

        /// <summary>
        /// Writes the default match frame to <c>prefix.png</c> and a close crop centred on the named
        /// spaces to <c>prefix-close.png</c>, restoring the camera afterwards.
        /// </summary>
        internal static IEnumerator Capture(
            BoardInteractionUnityView view, string prefix, IReadOnlyList<HexCoordinate> subject)
        {
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(prefix + ".png")));
            Write(camera, prefix + ".png");

            var radius = view.TileViews[0].OuterRadius;
            var points = subject.Select(space => HexWorldLayout.ToWorld(space, radius)).ToArray();
            var middle = points.Aggregate(Vector3.zero, (sum, point) => sum + point) / points.Length;
            var spread = points.Max(point => Vector3.Distance(point, middle));
            var position = camera.transform.position;
            var size = camera.orthographicSize;
            try
            {
                camera.transform.position = new Vector3(middle.x, position.y, middle.z);
                camera.orthographicSize = Mathf.Max(2.4f * radius, spread + (1.2f * radius));
                // One frame, so each camera-facing piece re-aims at the moved camera.
                yield return null;
                Write(camera, prefix + "-close.png");
            }
            finally
            {
                camera.transform.position = position;
                camera.orthographicSize = size;
            }

            Assert.That(new FileInfo(prefix + ".png").Length, Is.GreaterThan(1000));
            Assert.That(new FileInfo(prefix + "-close.png").Length, Is.GreaterThan(1000));
        }

        private static void Write(Camera camera, string relativePath)
        {
            const int width = 1920;
            const int height = 1080;
            var target = new RenderTexture(width, height, 24) { name = "Board Piece Capture" };
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(Path.GetFullPath(relativePath), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Object.Destroy(target);
                if (texture != null) Object.Destroy(texture);
            }
        }
    }
}
