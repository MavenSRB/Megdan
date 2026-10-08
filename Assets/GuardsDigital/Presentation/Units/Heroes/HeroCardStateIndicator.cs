using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Presentation.Match;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardsDigital.Presentation.Units.Heroes
{
    public readonly struct HeroCardPip
    {
        public HeroCardPip(CardColor color, CardDefinitionReference definitionReference = default)
        {
            Color = color;
            DefinitionReference = definitionReference;
        }

        public CardColor Color { get; }
        public CardDefinitionReference DefinitionReference { get; }
    }

    /// <summary>
    /// Presentation-only card pips that sit above a hero token. A pip may retain an identity already
    /// authorized by the viewer's inspection projection, but it has no collider, so target, selection,
    /// damage, activation, and ordinary token clicks remain the board's layers below.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroCardStateIndicator : MonoBehaviour
    {
        public const int OverlayRenderQueue = 4000;
        public const int OverlaySortingOrder = 100;
        public const float PipWidth = 0.18f;
        public const float PipDepth = 0.27f;
        public const float PipSpacing = 0.22f;
        public const float PipGap = PipSpacing - PipWidth;
        private const float InnerWidth = 0.12f;
        private const float InnerDepth = 0.21f;

        private readonly List<GameObject> pips = new List<GameObject>();
        private readonly List<HeroCardPip> models = new List<HeroCardPip>();
        private readonly List<Material> materials = new List<Material>();
        private Mesh mesh;

        public int PipCount => pips.Count;
        public IReadOnlyList<HeroCardPip> Pips => models;
        public IReadOnlyList<Renderer> Renderers =>
            pips.SelectMany(value => value.GetComponentsInChildren<Renderer>()).ToArray();

        public static HeroCardStateIndicator GetOrCreate(Transform owner)
        {
            var existing = owner.GetComponentInChildren<HeroCardStateIndicator>(true);
            if (existing != null) return existing;
            var host = new GameObject("Hero card-state indicators");
            host.transform.SetParent(owner, false);
            return host.AddComponent<HeroCardStateIndicator>();
        }

        public void SetVisible(
            bool hasCurrent,
            HeroCardPip current,
            IEnumerable<HeroCardPip> knownHand,
            int concealedHandCount,
            float tokenRadius,
            float surface,
            Camera alignmentCamera = null)
        {
            if (concealedHandCount < 0) throw new ArgumentOutOfRangeException(nameof(concealedHandCount));
            Clear();
            var hand = (knownHand ?? throw new ArgumentNullException(nameof(knownHand))).ToList();
            if (!hasCurrent && hand.Count == 0 && concealedHandCount == 0) return;

            var y = surface + 0.055f;
            var rimRadius = tokenRadius * 0.92f;
            var lowerEdge = LowerEdgeDirection(alignmentCamera ?? Camera.main);
            if (hasCurrent)
            {
                var upperEdge = -lowerEdge;
                AddPip(
                    (upperEdge * rimRadius) + (Vector3.up * y),
                    current,
                    TangentRotation(upperEdge));
            }

            var total = hand.Count + concealedHandCount;
            var chord = PipSpacing;
            var step = rimRadius <= 0f
                ? 0f
                : 2f * Mathf.Asin(Mathf.Min(1f, chord / (2f * rimRadius)));
            for (var index = 0; index < total; index++)
            {
                var model = index < hand.Count ? hand[index] : new HeroCardPip(CardColor.None);
                var offset = (index - ((total - 1) * 0.5f)) * step;
                var tangent = Vector3.Cross(lowerEdge, Vector3.up).normalized;
                var radial = (lowerEdge * Mathf.Cos(offset)) + (tangent * Mathf.Sin(offset));
                AddPip(
                    (radial * rimRadius) + (Vector3.up * y),
                    model,
                    TangentRotation(radial));
            }
        }

        public void SetHidden() => Clear();

        public bool TryGetPipFromPointerRay(Ray pointerRay, out HeroCardPip pip, float maxDistance = 500f)
        {
            pip = default;
            var nearest = float.PositiveInfinity;
            var found = -1;
            for (var index = 0; index < pips.Count; index++)
            {
                var centre = pips[index].transform.position;
                var plane = new Plane(transform.up, centre);
                if (!plane.Raycast(pointerRay, out var distance) || distance < 0f ||
                    distance > maxDistance || distance >= nearest)
                    continue;
                var point = pips[index].transform.InverseTransformPoint(pointerRay.GetPoint(distance));
                if (Mathf.Abs(point.x) > PipWidth * 0.5f || Mathf.Abs(point.z) > PipDepth * 0.5f)
                    continue;
                nearest = distance;
                found = index;
            }

            if (found < 0) return false;
            pip = models[found];
            return true;
        }

        private void AddPip(Vector3 position, HeroCardPip model, Quaternion rotation)
        {
            var host = new GameObject("Card state pip");
            host.transform.SetParent(transform, false);
            host.transform.localRotation = rotation;
            host.transform.localPosition = position;
            AddPlate(host.transform, PipWidth, PipDepth, new Color(0.035f, 0.045f, 0.055f, 1f), 0f);
            AddPlate(host.transform, InnerWidth, InnerDepth,
                model.Color == CardColor.None
                    ? new Color(0.48f, 0.52f, 0.57f, 1f)
                    : MatchHudTheme.Face(model.Color),
                0.002f);
            pips.Add(host);
            models.Add(model);
        }

        private void AddPlate(Transform parent, float width, float depth, Color color, float y)
        {
            var plate = new GameObject("Rectangle");
            plate.transform.SetParent(parent, false);
            plate.transform.localPosition = new Vector3(0f, y, 0f);
            plate.transform.localScale = new Vector3(width, 1f, depth);
            plate.AddComponent<MeshFilter>().sharedMesh = Mesh();
            var renderer = plate.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = OverlaySortingOrder;
            renderer.sharedMaterial = Material(color);
        }
        private Vector3 LowerEdgeDirection(Camera alignmentCamera)
        {
            if (alignmentCamera == null) return Vector3.back;
            var screenUp = Vector3.ProjectOnPlane(alignmentCamera.transform.up, transform.up);
            if (screenUp.sqrMagnitude < 0.0001f) return Vector3.back;
            var localScreenUp = transform.InverseTransformDirection(screenUp.normalized);
            var lower = new Vector3(-localScreenUp.x, 0f, -localScreenUp.z);
            return lower.sqrMagnitude < 0.0001f ? Vector3.back : lower.normalized;
        }

        private static Quaternion TangentRotation(Vector3 radial)
        {
            var tangent = Vector3.Cross(radial, Vector3.up);
            return tangent.sqrMagnitude < 0.0001f
                ? Quaternion.identity
                : Quaternion.FromToRotation(Vector3.right, tangent.normalized);
        }


        private Mesh Mesh()
        {
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "Hero card-state pip" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, -0.5f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private Material Material(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("Card-state indicators require the URP Unlit shader.");
            var material = new Material(shader)
            {
                name = "Hero card-state pip overlay",
                color = color,
                renderQueue = OverlayRenderQueue
            };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_ZTest")) material.SetInt("_ZTest", (int)CompareFunction.Always);
            if (material.HasProperty("_Cull")) material.SetInt("_Cull", (int)CullMode.Off);
            materials.Add(material);
            return material;
        }

        private void Clear()
        {
            foreach (var pip in pips) DestroyOwned(pip);
            pips.Clear();
            foreach (var material in materials) DestroyOwned(material);
            materials.Clear();
            models.Clear();
        }

        private void OnDestroy()
        {
            Clear();
            DestroyOwned(mesh);
            mesh = null;
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null) return;
            if (UnityEngine.Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
