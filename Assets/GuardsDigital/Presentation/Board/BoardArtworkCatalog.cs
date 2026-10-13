using GuardsDigital.Core.Board;
using UnityEngine;
namespace GuardsDigital.Presentation.Board
{
    public static class BoardArtworkCatalog
    {
        public const string AlternateResourcePath = "Private/OfficialBoard/alternate-small-single-lane-board";
        public const string GreenDoubleLaneResourcePath = "Private/OfficialBoard/green-water-double-lane-board";
        public const string LavaBridgeDoubleLaneResourcePath = "Private/OfficialBoard/lava-bridge-double-lane-board";
        public static bool TryLoadTexture(MapDefinitionReference map, out Texture2D texture)
        {
            texture = map.Equals(OfficialBoardMapCatalog.SelectedMap) ? Resources.Load<Texture2D>(OfficialBoardArtwork.ResourcePath) :
                map.Equals(OfficialBoardMapCatalog.AlternateSmall) ? Resources.Load<Texture2D>(AlternateResourcePath) :
                map.Equals(OfficialBoardMapCatalog.GreenDoubleLane) ? Resources.Load<Texture2D>(GreenDoubleLaneResourcePath) :
                map.Equals(OfficialBoardMapCatalog.LavaBridgeDoubleLane) ? Resources.Load<Texture2D>(LavaBridgeDoubleLaneResourcePath) : null;
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
        private static Vector3 PixelToWorld(MapDefinitionReference map, Vector2 pixel, float radius = 1f)
        {
            if (map.Equals(OfficialBoardMapCatalog.AlternateSmall)) return PixelToWorld(pixel, radius);
            // Independently source-reviewed transforms. The green board is a vertical staggered-column
            // grid; its printed three-prong stars are vertices, not cell centres. The lava board uses
            // the separately fitted axial basis below. Keeping these transforms map-specific prevents
            // the attractive but false assumption that the two illustrated boards share one lattice.
            var green = map.Equals(OfficialBoardMapCatalog.GreenDoubleLane);
            var originX = green ? 428f : 456.6116080143389f;
            var originY = green ? 482f : 168.47485314734936f;
            var qx = green ? 39.6f : 46.565437915688136f;
            var qy = green ? 22.9f : -12.521833178544053f;
            var rx = green ? 0f : 34.11299470955809f;
            var ry = green ? 45.8f : 33.8906773461424f;
            var x = pixel.x - originX;
            var y = pixel.y - originY;
            var determinant = qx * ry - rx * qy;
            var q = (ry * x - rx * y) / determinant;
            var r = (-qy * x + qx * y) / determinant;
            return new Vector3(Mathf.Sqrt(3f) * (q + r * .5f), 0, -1.5f * r) * radius;
        }
        public static Vector3[] Corners(MapDefinitionReference map, float radius) =>
            map.Equals(OfficialBoardMapCatalog.AlternateSmall) ||
            map.Equals(OfficialBoardMapCatalog.GreenDoubleLane) ||
            map.Equals(OfficialBoardMapCatalog.LavaBridgeDoubleLane)
                ? new[] { PixelToWorld(map,new Vector2(0,900),radius), PixelToWorld(map,new Vector2(900,900),radius),
                    PixelToWorld(map,new Vector2(900,0),radius), PixelToWorld(map,new Vector2(0,0),radius) }
                : OfficialBoardArtwork.Corners(radius);
        public static Vector3 Right(MapDefinitionReference map) => map.Equals(OfficialBoardMapCatalog.AlternateSmall) ||
            map.Equals(OfficialBoardMapCatalog.GreenDoubleLane) || map.Equals(OfficialBoardMapCatalog.LavaBridgeDoubleLane)
            ? (PixelToWorld(map,Vector2.right)-PixelToWorld(map,Vector2.zero)).normalized : OfficialBoardArtwork.BoardRightInWorld;
        public static Vector3 Up(MapDefinitionReference map) => map.Equals(OfficialBoardMapCatalog.AlternateSmall) ||
            map.Equals(OfficialBoardMapCatalog.GreenDoubleLane) || map.Equals(OfficialBoardMapCatalog.LavaBridgeDoubleLane)
            ? (PixelToWorld(map,Vector2.zero)-PixelToWorld(map,Vector2.up)).normalized : OfficialBoardArtwork.BoardUpInWorld;
    }
}
