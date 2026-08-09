using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class PlaceholderTestGridView : MonoBehaviour
    {
        [SerializeField]
        private int gridRadius = 2;

        [SerializeField]
        private float outerRadius = HexWorldLayout.DefaultOuterRadius;

        public int GridRadius => gridRadius;

        public float OuterRadius => outerRadius;

        public IReadOnlyCollection<HexCoordinate> Coordinates => CreateCoordinates(gridRadius).ToArray();

        public void EnsureGrid()
        {
            if (gridRadius < 0 || outerRadius <= 0f || float.IsNaN(outerRadius) || float.IsInfinity(outerRadius))
            {
                return;
            }

            var desiredCoordinates = new HashSet<HexCoordinate>(CreateCoordinates(gridRadius));
            var tilesByCoordinate = new Dictionary<HexCoordinate, HexTileView>();
            HexTileView templateTile = null;

            foreach (var tile in GetComponentsInChildren<HexTileView>(true))
            {
                templateTile ??= tile;
                if (!desiredCoordinates.Contains(tile.Coordinate) || tilesByCoordinate.ContainsKey(tile.Coordinate))
                {
                    DestroyTile(tile.gameObject);
                    continue;
                }

                tile.Configure(tile.Coordinate, outerRadius);
                tilesByCoordinate.Add(tile.Coordinate, tile);
            }

            foreach (var coordinate in desiredCoordinates.OrderBy(coordinate => coordinate.Q).ThenBy(coordinate => coordinate.R))
            {
                if (!tilesByCoordinate.ContainsKey(coordinate))
                {
                    CreateTile(coordinate, templateTile);
                }
            }
        }

        private static IEnumerable<HexCoordinate> CreateCoordinates(int radius)
        {
            for (var q = -radius; q <= radius; q++)
            {
                var minimumR = Mathf.Max(-radius, -q - radius);
                var maximumR = Mathf.Min(radius, -q + radius);
                for (var r = minimumR; r <= maximumR; r++)
                {
                    yield return new HexCoordinate(q, r);
                }
            }
        }

        private HexTileView CreateTile(HexCoordinate coordinate, HexTileView templateTile)
        {
            var tileObject = new GameObject($"Hex {coordinate.Q},{coordinate.R}");
            tileObject.transform.SetParent(transform, false);

            var meshFilter = tileObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = templateTile != null
                ? templateTile.GetComponent<MeshFilter>().sharedMesh
                : FlatHexMeshBuilder.Create(outerRadius);

            var meshRenderer = tileObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = templateTile != null
                ? templateTile.GetComponent<MeshRenderer>().sharedMaterial
                : CreateMaterial(new Color(0.72f, 0.72f, 0.72f, 1f));

            var tile = tileObject.AddComponent<HexTileView>();
            tile.Configure(coordinate, outerRadius);

            var edgeObject = new GameObject("Dark Gray Edge");
            edgeObject.transform.SetParent(tileObject.transform, false);
            var lineRenderer = edgeObject.AddComponent<LineRenderer>();
            lineRenderer.sharedMaterial = GetEdgeMaterial(templateTile);
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = 6;
            lineRenderer.startWidth = 0.055f;
            lineRenderer.endWidth = 0.055f;
            lineRenderer.numCornerVertices = 2;

            for (var side = 0; side < 6; side++)
            {
                var angle = Mathf.Deg2Rad * (30f + (side * 60f));
                lineRenderer.SetPosition(side, new Vector3(Mathf.Cos(angle) * outerRadius, 0.015f, Mathf.Sin(angle) * outerRadius));
            }

            return tile;
        }

        private static Material GetEdgeMaterial(HexTileView templateTile)
        {
            var templateEdge = templateTile != null ? templateTile.GetComponentInChildren<LineRenderer>() : null;
            return templateEdge != null ? templateEdge.sharedMaterial : CreateMaterial(new Color(0.22f, 0.22f, 0.22f, 1f));
        }

        private static Material CreateMaterial(Color color)
        {
            var shader = Shader.Find("Standard");
            return new Material(shader)
            {
                color = color
            };
        }

        private static void DestroyTile(GameObject tileObject)
        {
            if (Application.isPlaying)
            {
                Destroy(tileObject);
            }
            else
            {
                DestroyImmediate(tileObject);
            }
        }

        private void OnEnable()
        {
            EnsureGrid();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureGrid();
        }
#endif
    }
}
