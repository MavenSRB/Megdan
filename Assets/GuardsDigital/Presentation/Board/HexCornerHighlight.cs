using System;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>The projected board-choice meaning carried by a hex's six printed corner marks.</summary>
    public enum HexCornerHighlightKind
    {
        None = 0,
        Movement = 1,
        Attack = 2,
        Respawn = 3,
        Effect = 4,
        EffectResult = 5,
        Ally = 6
    }

    /// <summary>
    /// One shared, fixed-geometry six-corner construction. The halo and core use the same mesh, so
    /// hover can only strengthen the materials; it cannot move or resize a mark.
    /// </summary>
    public sealed class HexCornerHighlightTemplate : IDisposable
    {
        public const int CornerCount = 6;
        public const string MeshName = "Six Hex Corner Marks";
        public const string MovementHaloMaterialName = "Movement Corner Shine Halo";
        public const string MovementCoreMaterialName = "Movement Corner Shine Core";
        public const string AttackHaloMaterialName = "Attack Corner Shine Halo";
        public const string AttackCoreMaterialName = "Attack Corner Shine Core";
        public const string RespawnHaloMaterialName = "Respawn Corner Shine Halo";
        public const string RespawnCoreMaterialName = "Respawn Corner Shine Core";
        public const string EffectHaloMaterialName = "Effect Corner Shine Halo";
        public const string EffectCoreMaterialName = "Effect Corner Shine Core";
        public const string EffectResultHaloMaterialName = "Effect Result Corner Shine Halo";
        public const string EffectResultCoreMaterialName = "Effect Result Corner Shine Core";
        public const string AllyHaloMaterialName = "Teammate Corner Shine Halo";
        public const string AllyCoreMaterialName = "Teammate Corner Shine Core";

        // The whole board now speaks one six-corner language. Green is travel, cyan is return to the
        // board, blood red is an Attack or removal, warm ivory is a teammate, and cool neutral grey
        // is every other selectable effect. A darker slate keeps a projected result distinct.
        public static readonly Color MovementColor = new Color(0.22f, 1f, 0.48f, 1f);
        public static readonly Color AttackColor = new Color(0.72f, 0.025f, 0.035f, 1f);
        public static readonly Color RespawnColor = new Color(0.05f, 0.78f, 1f, 1f);
        public static readonly Color AllyColor = new Color(0.92f, 0.90f, 0.80f, 1f);
        public static readonly Color EffectColor = new Color(0.62f, 0.68f, 0.72f, 1f);
        public static readonly Color EffectResultColor = new Color(0.42f, 0.48f, 0.55f, 1f);

        private readonly Mesh haloMesh;
        private readonly Mesh coreMesh;
        private readonly Material movementHalo;
        private readonly Material movementCore;
        private readonly Material attackHalo;
        private readonly Material attackCore;
        private readonly Material respawnHalo;
        private readonly Material respawnCore;
        private readonly Material effectHalo;
        private readonly Material effectCore;
        private readonly Material effectResultHalo;
        private readonly Material effectResultCore;
        private readonly Material allyHalo;
        private readonly Material allyCore;

        public HexCornerHighlightTemplate(float outerRadius)
        {
            if (outerRadius <= 0f || float.IsNaN(outerRadius) || float.IsInfinity(outerRadius))
            {
                throw new ArgumentOutOfRangeException(nameof(outerRadius));
            }

            haloMesh = BuildSixCornerMesh(outerRadius, 0.075f);
            haloMesh.name = MeshName + " Halo";
            coreMesh = BuildSixCornerMesh(outerRadius, 0.034f);
            coreMesh.name = MeshName + " Core";
            movementHalo = CreateMaterial(MovementHaloMaterialName, MovementColor);
            movementCore = CreateMaterial(MovementCoreMaterialName, Color.Lerp(MovementColor, Color.white, 0.82f));
            attackHalo = CreateMaterial(AttackHaloMaterialName, AttackColor);
            attackCore = CreateMaterial(AttackCoreMaterialName, Color.Lerp(AttackColor, Color.white, 0.82f));
            respawnHalo = CreateMaterial(RespawnHaloMaterialName, RespawnColor);
            respawnCore = CreateMaterial(RespawnCoreMaterialName, Color.Lerp(RespawnColor, Color.white, 0.82f));
            effectHalo = CreateMaterial(EffectHaloMaterialName, EffectColor);
            effectCore = CreateMaterial(EffectCoreMaterialName, Color.Lerp(EffectColor, Color.white, 0.82f));
            effectResultHalo = CreateMaterial(EffectResultHaloMaterialName, EffectResultColor);
            effectResultCore = CreateMaterial(EffectResultCoreMaterialName,
                Color.Lerp(EffectResultColor, Color.white, 0.82f));
            allyHalo = CreateMaterial(AllyHaloMaterialName, AllyColor);
            allyCore = CreateMaterial(AllyCoreMaterialName, Color.Lerp(AllyColor, Color.white, 0.82f));
        }

        public Mesh HaloMesh => haloMesh;
        public Mesh CoreMesh => coreMesh;
        public int MarkCount => CornerCount;
        public int OwnedNativeResourceCount => 14;

        public Material Halo(HexCornerHighlightKind kind) => kind == HexCornerHighlightKind.Movement
            ? movementHalo
            : kind == HexCornerHighlightKind.Attack
                ? attackHalo
                : kind == HexCornerHighlightKind.Respawn
                    ? respawnHalo
                    : kind == HexCornerHighlightKind.Effect
                        ? effectHalo
                        : kind == HexCornerHighlightKind.EffectResult
                            ? effectResultHalo
                            : kind == HexCornerHighlightKind.Ally
                                ? allyHalo
                            : null;

        public Material Core(HexCornerHighlightKind kind) => kind == HexCornerHighlightKind.Movement
            ? movementCore
            : kind == HexCornerHighlightKind.Attack
                ? attackCore
                : kind == HexCornerHighlightKind.Respawn
                    ? respawnCore
                    : kind == HexCornerHighlightKind.Effect
                        ? effectCore
                        : kind == HexCornerHighlightKind.EffectResult
                            ? effectResultCore
                            : kind == HexCornerHighlightKind.Ally
                                ? allyCore
                            : null;

        private static Mesh BuildSixCornerMesh(float radius, float halfWidthRatio)
        {
            const float markLengthRatio = 0.30f;
            var vertices = new Vector3[CornerCount * 8];
            var triangles = new int[CornerCount * 12];
            for (var corner = 0; corner < CornerCount; corner++)
            {
                var angle = Mathf.Deg2Rad * (30f + (corner * 60f));
                var center = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                var previousAngle = Mathf.Deg2Rad * (30f + (((corner + 5) % 6) * 60f));
                var nextAngle = Mathf.Deg2Rad * (30f + (((corner + 1) % 6) * 60f));
                var previous = new Vector3(Mathf.Cos(previousAngle) * radius, 0f, Mathf.Sin(previousAngle) * radius);
                var next = new Vector3(Mathf.Cos(nextAngle) * radius, 0f, Mathf.Sin(nextAngle) * radius);
                AddBar(vertices, triangles, corner * 8, corner * 12, center,
                    Vector3.Lerp(center, previous, markLengthRatio), halfWidthRatio * radius);
                AddBar(vertices, triangles, (corner * 8) + 4, (corner * 12) + 6, center,
                    Vector3.Lerp(center, next, markLengthRatio), halfWidthRatio * radius);
            }

            var result = new Mesh { name = MeshName };
            result.vertices = vertices;
            result.triangles = triangles;
            result.RecalculateBounds();
            result.RecalculateNormals();
            return result;
        }

        private static void AddBar(
            Vector3[] vertices, int[] triangles, int vertex, int triangle,
            Vector3 start, Vector3 end, float halfWidth)
        {
            var direction = (end - start).normalized;
            var perpendicular = Vector3.Cross(Vector3.up, direction) * halfWidth;
            vertices[vertex] = start - perpendicular;
            vertices[vertex + 1] = start + perpendicular;
            vertices[vertex + 2] = end + perpendicular;
            vertices[vertex + 3] = end - perpendicular;
            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 2;
            triangles[triangle + 2] = vertex + 1;
            triangles[triangle + 3] = vertex;
            triangles[triangle + 4] = vertex + 3;
            triangles[triangle + 5] = vertex + 2;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                throw new InvalidOperationException("Hex corner highlights require the URP Unlit shader.");
            }

            var material = new Material(shader) { name = name, color = color };
            BoardOverlayMaterial.ApplyBlending(material);
            return material;
        }

        public void Dispose()
        {
            DestroyOwned(haloMesh);
            DestroyOwned(coreMesh);
            DestroyOwned(movementHalo);
            DestroyOwned(movementCore);
            DestroyOwned(attackHalo);
            DestroyOwned(attackCore);
            DestroyOwned(respawnHalo);
            DestroyOwned(respawnCore);
            DestroyOwned(effectHalo);
            DestroyOwned(effectCore);
            DestroyOwned(effectResultHalo);
            DestroyOwned(effectResultCore);
            DestroyOwned(allyHalo);
            DestroyOwned(allyCore);
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null) return;
            if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }

    [DisallowMultipleComponent]
    public sealed class HexCornerHighlight : MonoBehaviour
    {
        private MeshRenderer halo;
        private MeshRenderer core;
        private HexCornerHighlightTemplate template;
        private MaterialPropertyBlock propertyBlock;

        public HexCornerHighlightKind Kind { get; private set; }
        public bool IsHovered { get; private set; }
        public int VisibleMarkCount => Kind == HexCornerHighlightKind.None ? 0 : HexCornerHighlightTemplate.CornerCount;
        public Bounds LocalBounds => core == null ? default : core.localBounds;

        public void Configure(HexCornerHighlightTemplate sharedTemplate)
        {
            template = sharedTemplate ?? throw new ArgumentNullException(nameof(sharedTemplate));
            halo = EnsureLayer("Corner Shine Halo", 0.075f, template.HaloMesh);
            core = EnsureLayer("Corner Shine Core", 0.085f, template.CoreMesh);
            Set(HexCornerHighlightKind.None, false);
        }

        public void Set(HexCornerHighlightKind kind, bool hovered)
        {
            if (kind != HexCornerHighlightKind.None && template == null)
            {
                throw new InvalidOperationException("The shared corner-highlight template is not configured.");
            }

            Kind = kind;
            IsHovered = kind != HexCornerHighlightKind.None && hovered;
            if (halo == null || core == null) return;
            var visible = kind != HexCornerHighlightKind.None;
            halo.enabled = visible;
            core.enabled = visible;
            if (!visible) return;
            halo.sharedMaterial = template.Halo(kind);
            core.sharedMaterial = template.Core(kind);
            halo.transform.localScale = Vector3.one;
            core.transform.localScale = Vector3.one;
            var strength = IsHovered ? 1f : 0.84f;
            SetColor(halo, WithAlpha(template.Halo(kind).color, IsHovered ? 1f : 0.72f));
            SetColor(core, WithAlpha(template.Core(kind).color, strength));
        }

        private MeshRenderer EnsureLayer(string layerName, float localY, Mesh mesh)
        {
            var child = transform.Find(layerName);
            if (child == null)
            {
                child = new GameObject(layerName).transform;
                child.SetParent(transform, false);
            }

            child.localPosition = new Vector3(0f, localY, 0f);
            var filter = child.GetComponent<MeshFilter>();
            if (filter == null) filter = child.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = child.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = child.gameObject.AddComponent<MeshRenderer>();
            return renderer;
        }

        private static Color WithAlpha(Color color, float alpha) =>
            new Color(color.r, color.g, color.b, alpha);

        private void SetColor(Renderer renderer, Color color)
        {
            propertyBlock ??= new MaterialPropertyBlock();
            propertyBlock.Clear();
            propertyBlock.SetColor("_BaseColor", color);
            propertyBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}
