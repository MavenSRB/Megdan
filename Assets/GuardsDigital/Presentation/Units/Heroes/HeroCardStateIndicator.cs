using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Presentation.Match;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardsDigital.Presentation.Units.Heroes
{
    /// <summary>
    /// Presentation-only card pips that sit above a hero token. They own no card identity and have no
    /// collider, so target, hover, selection, damage, and activation remain the board's layers below.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HeroCardStateIndicator : MonoBehaviour
    {
        public const int OverlayRenderQueue = 4000;
        public const int OverlaySortingOrder = 100;
        private const float OuterRadius = 0.12f;
        private const float InnerRadius = 0.085f;
        private const float PipSpacing = 0.19f;

        private readonly List<GameObject> pips = new List<GameObject>();
        private readonly List<Material> materials = new List<Material>();
        private Mesh mesh;

        public int PipCount => pips.Count;
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
            CardColor current,
            IEnumerable<CardColor> knownHand,
            int concealedHandCount,
            float tokenRadius,
            float surface)
        {
            if (concealedHandCount < 0) throw new ArgumentOutOfRangeException(nameof(concealedHandCount));
            Clear();
            var hand = (knownHand ?? throw new ArgumentNullException(nameof(knownHand))).ToList();
            if (!hasCurrent && hand.Count == 0 && concealedHandCount == 0) return;

            var y = surface + 0.055f;
            var rimRadius = tokenRadius * 0.92f;
            if (hasCurrent)
                AddPip(new Vector3(0f, y, rimRadius), current);

            var total = hand.Count + concealedHandCount;
            var step = rimRadius <= 0f
                ? 0f
                : 2f * Mathf.Asin(Mathf.Min(1f, PipSpacing / (2f * rimRadius)));
            for (var index = 0; index < total; index++)
            {
                var color = index < hand.Count ? hand[index] : CardColor.None;
                var angle = (-Mathf.PI * 0.5f) + ((index - ((total - 1) * 0.5f)) * step);
                AddPip(new Vector3(Mathf.Cos(angle) * rimRadius, y, Mathf.Sin(angle) * rimRadius), color);
            }
        }

        public void SetHidden() => Clear();

        private void AddPip(Vector3 position, CardColor color)
        {
            var host = new GameObject("Card state pip");
            host.transform.SetParent(transform, false);
            host.transform.localPosition = position;
            AddDisc(host.transform, OuterRadius, new Color(0.035f, 0.045f, 0.055f, 1f), 0f);
            AddDisc(host.transform, InnerRadius,
                color == CardColor.None ? new Color(0.48f, 0.52f, 0.57f, 1f) : MatchHudTheme.Face(color),
                0.002f);
            pips.Add(host);
        }

        private void AddDisc(Transform parent, float radius, Color color, float y)
        {
            var disc = new GameObject("Disc");
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = new Vector3(0f, y, 0f);
            disc.transform.localScale = new Vector3(radius, 1f, radius);
            disc.AddComponent<MeshFilter>().sharedMesh = Mesh();
            var renderer = disc.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = OverlaySortingOrder;
            renderer.sharedMaterial = Material(color);
        }

        private Mesh Mesh()
        {
            if (mesh != null) return mesh;
            const int segments = 24;
            var vertices = new Vector3[segments + 1];
            var triangles = new int[segments * 3];
            for (var index = 0; index < segments; index++)
            {
                var angle = (Mathf.PI * 2f * index) / segments;
                vertices[index + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                triangles[index * 3] = 0;
                triangles[(index * 3) + 1] = ((index + 1) % segments) + 1;
                triangles[(index * 3) + 2] = index + 1;
            }
            mesh = new Mesh { name = "Hero card-state pip" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
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
