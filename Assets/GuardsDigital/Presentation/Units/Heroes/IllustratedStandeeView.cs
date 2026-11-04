using System;
using System.Collections.Generic;
using GuardsDigital.Presentation.Board;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardsDigital.Presentation.Units.Heroes
{
    /// <summary>Private, unchanged portrait fronts on a thin extruded silhouette; no game state.</summary>
    [DisallowMultipleComponent]
    public sealed class IllustratedStandeeView : MonoBehaviour
    {
        public const float TokenAngle = 80f;
        public const float StandeeAngle = 79f;
        public const float PanelHeight = 1.85f;
        public const float PanelThickness = 0.045f;
        [Serializable] public sealed class Outline
        {
            public float left, bottom, width, height, aspect;
            public float[] edges;
        }

        private PrimitiveHeroView owner;
        private GameObject panel;
        private Mesh mesh;
        private Material face, edge;
        private string resource;
        public bool IsStanding => panel != null && panel.activeSelf;
        public bool HasArtwork => panel != null;

        public void Configure(PrimitiveHeroView hero, string contentId)
        {
            owner = hero;
            var path = "Private/Megdan/Standees/" + contentId;
            if (resource == path) return;
            Release();
            resource = path;
            var texture = Resources.Load<Texture2D>(path);
            var outline = Resources.Load<TextAsset>(path);
            if (texture == null || outline == null) return;
            var geometry = JsonUtility.FromJson<Outline>(outline.text);
            mesh = BuildPanel(geometry);
            face = new Material(Resources.Load<Material>("Rendering/StandeeCutoutTemplate"));
            face.name = "Unchanged illustrated standee";
            face.SetTexture("_BaseMap", texture);
            face.color = Color.white;
            edge = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            edge.name = "Dark standee panel edge";
            edge.color = new Color(0.065f, 0.055f, 0.045f);
            edge.SetFloat("_Cull", 0f);
            panel = new GameObject("Illustrated standee panel", typeof(MeshFilter), typeof(MeshRenderer));
            panel.transform.SetParent(transform, false);
            panel.transform.localPosition = Vector3.up * BoardPieceDepth.Standard;
            panel.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = panel.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { face, edge };
            // Alpha-tested fronts cast their silhouette rather than a rectangular card shadow.
            renderer.shadowCastingMode = ShadowCastingMode.On;
            panel.SetActive(false);
        }

        public void Refresh(Camera camera)
        {
            if (owner == null || panel == null || camera == null) return;
            float elevation = Mathf.Asin(Mathf.Clamp(-camera.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            bool standing = IsStanding ? elevation < TokenAngle : elevation < StandeeAngle;
            standing &= owner.HasTokenVisual;
            if (standing != IsStanding)
            {
                panel.SetActive(standing);
                owner.SetStandeeBase(standing);
            }
            if (!standing) return;
            // Upright yaw-only facing keeps the art readable as the viewer circles the board.
            var facing = Vector3.ProjectOnPlane(-camera.transform.forward, Vector3.up);
            if (facing.sqrMagnitude > 0.0001f)
                panel.transform.rotation = Quaternion.LookRotation(-facing, Vector3.up);
        }

        private void LateUpdate() => Refresh(Camera.main);

        public static Mesh BuildPanel(Outline outline)
        {
            float width = Mathf.Min(1.55f, PanelHeight * outline.aspect);
            float height = Mathf.Min(PanelHeight, 1.55f / outline.aspect);
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var fronts = new List<int>();
            var sides = new List<int>();
            Action<float, float, float> vertex = (x, y, z) =>
            {
                vertices.Add(new Vector3((x - 0.5f) * width, y * height, z));
                uv.Add(new Vector2(outline.left + x * outline.width, outline.bottom + y * outline.height));
            };
            foreach (float z in new[] { -PanelThickness * 0.5f, PanelThickness * 0.5f })
            {
                int start = vertices.Count;
                vertex(0, 0, z); vertex(1, 0, z); vertex(1, 1, z); vertex(0, 1, z);
                fronts.AddRange(new[] { start, start+1, start+2, start, start+2, start+3 });
            }
            for (int i = 0; i < outline.edges.Length; i += 4)
            {
                int start = vertices.Count;
                vertex(outline.edges[i], outline.edges[i+1], -PanelThickness * 0.5f);
                vertex(outline.edges[i+2], outline.edges[i+3], -PanelThickness * 0.5f);
                vertex(outline.edges[i+2], outline.edges[i+3], PanelThickness * 0.5f);
                vertex(outline.edges[i], outline.edges[i+1], PanelThickness * 0.5f);
                sides.AddRange(new[] { start, start+1, start+2, start, start+2, start+3 });
            }
            var result = new Mesh { name = "Extruded portrait silhouette", subMeshCount = 2 };
            result.SetVertices(vertices); result.SetUVs(0, uv);
            result.SetTriangles(fronts, 0); result.SetTriangles(sides, 1);
            result.RecalculateNormals(); result.RecalculateBounds();
            return result;
        }

        private void Release()
        {
            if (panel != null) { panel.SetActive(false); DestroyResource(panel); }
            DestroyResource(mesh); DestroyResource(face); DestroyResource(edge);
            panel = null; mesh = null; face = null; edge = null;
        }
        private static void DestroyResource(UnityEngine.Object value)
        {
            if (value == null) return;
            if (UnityEngine.Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
        private void OnDestroy() => Release();
    }
}
