using GuardsDigital.Core.Board;
using UnityEngine;
namespace GuardsDigital.Presentation.Board
{
    public static class BoardArtworkCatalog
    {
        public const string AlternateResourcePath = "Private/OfficialBoard/alternate-small-single-lane-board";
        public static bool TryLoadTexture(MapDefinitionReference map, out Texture2D texture)
        {
            texture = map.Equals(OfficialBoardMapCatalog.SelectedMap) ? Resources.Load<Texture2D>(OfficialBoardArtwork.ResourcePath) :
                map.Equals(OfficialBoardMapCatalog.AlternateSmall) ? Resources.Load<Texture2D>(AlternateResourcePath) : null;
            return texture != null;
        }
        public static Vector3 PixelToWorld(Vector2 pixel, float radius = 1f)
        {
            var x = pixel.x - 453.4f; var y = pixel.y - 424.4f;
            const float determinant = 42.04f * 31.52f + 29.49f * 9.78f;
            var q = (31.52f * x - 29.49f * y) / determinant;
            var r = (9.78f * x + 42.04f * y) / determinant;
            return new Vector3(Mathf.Sqrt(3f) * (q + r * .5f), 0, -1.5f * r) * radius;
        }
        public static Vector3[] Corners(MapDefinitionReference map, float radius) =>
            map.Equals(OfficialBoardMapCatalog.AlternateSmall)
                ? new[] { PixelToWorld(new Vector2(0,900),radius), PixelToWorld(new Vector2(900,900),radius),
                    PixelToWorld(new Vector2(900,0),radius), PixelToWorld(new Vector2(0,0),radius) }
                : OfficialBoardArtwork.Corners(radius);
        public static Vector3 Right(MapDefinitionReference map) => map.Equals(OfficialBoardMapCatalog.AlternateSmall)
            ? (PixelToWorld(Vector2.right)-PixelToWorld(Vector2.zero)).normalized : OfficialBoardArtwork.BoardRightInWorld;
        public static Vector3 Up(MapDefinitionReference map) => map.Equals(OfficialBoardMapCatalog.AlternateSmall)
            ? (PixelToWorld(Vector2.zero)-PixelToWorld(Vector2.up)).normalized : OfficialBoardArtwork.BoardUpInWorld;
    }
}
