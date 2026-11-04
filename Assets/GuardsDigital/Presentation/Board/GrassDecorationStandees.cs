using System.Collections.Generic;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>Visual-only upright samples of the original grass paintings; owns no board state.</summary>
    [DisallowMultipleComponent]
    public sealed class GrassDecorationStandees : MonoBehaviour
    {
        public const float FlatAngle = 80f;
        public const float StandingAngle = 79f;
        // Pixel rectangles in the existing 512px runtime tiles; y is measured from the top.
        private static readonly Rect[][] Crops =
        {
            new[] { R(263,54,444,165), R(39,185,121,241), R(41,328,243,452) },
            new[] { R(59,83,137,141), R(216,184,260,266), R(280,40,365,101), R(105,391,263,505) },
            new[] { R(288,17,371,78), R(217,187,252,245), R(376,309,437,391) },
            new[] { R(44,43,234,152), R(354,43,468,112), R(18,258,83,309), R(267,342,403,469) }
        };
        private readonly List<GameObject> panels = new List<GameObject>();
        private readonly List<Material> materials = new List<Material>();
        private Mesh uprightMesh, groundMesh;
        private GameObject layer;
        private Camera viewCamera;
        public bool IsStanding => layer != null && layer.activeSelf;
        public int DecorationCount => panels.Count;
        private static Rect R(float left, float top, float right, float bottom) =>
            Rect.MinMaxRect(left / 512f, 1f - bottom / 512f, right / 512f, 1f - top / 512f);

        public void Configure(IReadOnlyList<HexTileView> tiles, Camera camera)
        {
            Release();
            viewCamera = camera;
            var shader = Resources.Load<Shader>("Rendering/GrassDecoration");
            if (shader == null) return;
            uprightMesh = Quad(false);
            groundMesh = Quad(true);
            layer = new GameObject("Grass decoration standees");
            layer.transform.SetParent(transform, false);
            layer.SetActive(false);
            var variants = new Dictionary<Texture, int>();
            if (!MegdanGrassTileCatalog.TryLoadTextures(out var textures)) return;
            for (int i = 0; i < textures.Length; i++) variants[textures[i]] = i;
            var sets = new Material[textures.Length][];
            for (int variant = 0; variant < textures.Length; variant++)
            {
                sets[variant] = new Material[Crops[variant].Length * 2];
                for (int index = 0; index < Crops[variant].Length; index++)
                for (int ground = 0; ground < 2; ground++)
                {
                    var crop = Crops[variant][index];
                    var material = new Material(shader) { mainTexture = textures[variant] };
                    material.SetVector("_Crop", new Vector4(crop.x, crop.y, crop.width, crop.height));
                    material.SetFloat("_Ground", ground);
                    materials.Add(material);
                    sets[variant][index * 2 + ground] = material;
                }
            }
            foreach (var tile in tiles)
            {
                var texture = tile.GetComponent<MeshRenderer>().sharedMaterial.mainTexture;
                if (texture == null || !variants.TryGetValue(texture, out int variant)) continue;
                for (int index = 0; index < Crops[variant].Length; index++)
                {
                    var crop = Crops[variant][index];
                    float radius = tile.OuterRadius;
                    var origin = tile.transform.position;
                    var panel = Create("Bush or mushroom", uprightMesh, sets[variant][index * 2]);
                    panel.transform.position = origin + new Vector3((crop.center.x * 2-1)*radius,
                        .018f, (crop.y * 2-1)*radius);
                    panel.transform.localScale = new Vector3(crop.width*2*radius, crop.height*2*radius, 1);
                    panels.Add(panel);
                    var ground = Create("Grass beneath decoration", groundMesh, sets[variant][index * 2 + 1]);
                    ground.transform.position = origin + new Vector3((crop.center.x*2-1)*radius,
                        .003f, (crop.center.y*2-1)*radius);
                    ground.transform.localScale = new Vector3(crop.width*2*radius, 1, crop.height*2*radius);
                }
            }
            Refresh(camera);
        }
        private GameObject Create(string label, Mesh mesh, Material material)
        {
            var result = new GameObject(label, typeof(MeshFilter), typeof(MeshRenderer));
            result.transform.SetParent(layer.transform, false);
            result.GetComponent<MeshFilter>().sharedMesh = mesh;
            result.GetComponent<MeshRenderer>().sharedMaterial = material;
            return result;
        }
        public void Refresh(Camera camera)
        {
            if (layer == null || camera == null) return;
            float elevation = Mathf.Asin(Mathf.Clamp(-camera.transform.forward.y,-1,1))*Mathf.Rad2Deg;
            layer.SetActive(elevation < (IsStanding ? FlatAngle : StandingAngle));
            if (!IsStanding) return;
            var direction = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            if (direction.sqrMagnitude < .0001f) return;
            var rotation = Quaternion.LookRotation(direction, Vector3.up);
            foreach (var panel in panels) panel.transform.rotation = rotation;
        }
        private void LateUpdate() => Refresh(viewCamera);
        private static Mesh Quad(bool ground)
        {
            var mesh = new Mesh { name = "Shared grass decoration quad" };
            mesh.vertices = ground ? new[] { new Vector3(-.5f,0,-.5f),new Vector3(.5f,0,-.5f),new Vector3(.5f,0,.5f),new Vector3(-.5f,0,.5f) }
                : new[] { new Vector3(-.5f,0,0),new Vector3(.5f,0,0),new Vector3(.5f,1,0),new Vector3(-.5f,1,0) };
            mesh.uv = new[] { Vector2.zero,Vector2.right,Vector2.one,Vector2.up };
            mesh.triangles = new[] {0,1,2,0,2,3}; mesh.RecalculateBounds(); mesh.RecalculateNormals(); return mesh;
        }
        private void Release()
        {
            if (layer != null) { layer.SetActive(false); Retire(layer); }
            Retire(uprightMesh); Retire(groundMesh);
            foreach (var material in materials) Retire(material);
            panels.Clear(); materials.Clear(); layer = null;
        }
        private static void Retire(Object value)
        {
            if (value == null) return;
            if (UnityEngine.Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }
        private void OnDestroy() => Release();
    }
}
