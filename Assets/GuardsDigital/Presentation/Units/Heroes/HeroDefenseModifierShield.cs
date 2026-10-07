using System;
using GuardsDigital.Presentation.Board;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardsDigital.Presentation.Units.Heroes
{
    /// <summary>
    /// A deliberately tiny shield at each hero token's screen-left edge. It carries only the
    /// projected signed net minion modifier and remains visible independently of hover.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class HeroDefenseModifierShield : MonoBehaviour
    {
        public const string ShieldObjectName = "Hero Defense Modifier Shield";
        public const string ValueObjectName = "Defense Modifier Value";
        public const float HalfWidthFactor = 0.22f;
        public const float HeightFactor = 0.32f;
        public const float EdgeDistanceFactor = 0.72f;
        public const float SurfaceOffset = 0.03f;
        public const float ValueCharacterSizeFactor = 0.19f;
        public static readonly Color ShieldColor = new Color(0.62f, 0.67f, 0.72f, 1f);
        public static readonly Color ValueColor = new Color(0.05f, 0.06f, 0.07f, 1f);

        private Mesh ownedMesh;
        private Material ownedMaterial;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private TextMesh valueText;

        public int? Modifier { get; private set; }
        public bool IsVisible => gameObject.activeSelf && Modifier.HasValue;
        public TextMesh ValueText => valueText;
        public int OwnedNativeResourceCount =>
            (ownedMesh == null ? 0 : 1) + (ownedMaterial == null ? 0 : 1);

        public static HeroDefenseModifierShield GetOrCreate(Transform piece)
        {
            if (piece == null) throw new ArgumentNullException(nameof(piece));
            foreach (var candidate in piece.GetComponentsInChildren<HeroDefenseModifierShield>(true))
            {
                if (candidate.transform.parent == piece) return candidate;
            }

            var shieldObject = new GameObject(ShieldObjectName);
            shieldObject.transform.SetParent(piece, false);
            return shieldObject.AddComponent<HeroDefenseModifierShield>();
        }

        public void SetVisible(int modifier, float pieceRadius, float surfaceY)
        {
            if (pieceRadius <= 0f || float.IsNaN(pieceRadius) || float.IsInfinity(pieceRadius))
            {
                throw new ArgumentOutOfRangeException(nameof(pieceRadius));
            }

            EnsureResources();
            var parent = transform.parent;
            var worldDown = -BoardChoiceCue.ScreenUpInWorld;
            var localDown = parent == null ? worldDown : parent.InverseTransformDirection(worldDown);
            localDown = new Vector3(localDown.x, 0f, localDown.z);
            localDown = localDown.sqrMagnitude < 1e-6f ? Vector3.back : localDown.normalized;
            var localLeft = new Vector3(localDown.z, 0f, -localDown.x);
            transform.localPosition = (localLeft * (pieceRadius * EdgeDistanceFactor)) +
                (Vector3.up * (surfaceY + SurfaceOffset));
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            BuildShield(localDown, pieceRadius);
            valueText.text = modifier.ToString("+0;-0;0");
            valueText.characterSize = pieceRadius * ValueCharacterSizeFactor;
            // TextMesh's readable face looks along local -Z; face it toward the overhead camera.
            valueText.transform.rotation = Quaternion.LookRotation(Vector3.down, BoardChoiceCue.ScreenUpInWorld);
            valueText.transform.localScale = Vector3.one;
            valueText.transform.localPosition = Vector3.up * SurfaceOffset;
            Modifier = modifier;
            meshRenderer.enabled = true;
            valueText.gameObject.SetActive(true);
            gameObject.SetActive(true);
            FitValue(pieceRadius);
            CenterValue();
        }

        private void FitValue(float pieceRadius)
        {
            // TextMesh font size affects world-space glyph bounds as well as raster quality.
            // Fit the actual glyphs so signed values cannot spill over the tiny shield face.
            var size = valueText.GetComponent<MeshRenderer>().localBounds.size;
            var width = pieceRadius * HalfWidthFactor * 2f * 0.80f;
            var height = pieceRadius * HeightFactor * 0.66f;
            var scale = Mathf.Min(1f, Mathf.Min(width / Mathf.Max(size.x, 0.0001f),
                height / Mathf.Max(size.y, 0.0001f)));
            valueText.transform.localScale = Vector3.one * scale;
        }

        private void CenterValue()
        {
            var valueRenderer = valueText.GetComponent<MeshRenderer>();
            var worldOffset = meshRenderer.bounds.center - valueRenderer.bounds.center;
            var localOffset = transform.InverseTransformVector(worldOffset);
            valueText.transform.localPosition += new Vector3(localOffset.x, 0f, localOffset.z);
        }

        public void SetHidden()
        {
            Modifier = null;
            if (meshRenderer != null) meshRenderer.enabled = false;
            if (valueText != null) valueText.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        private void BuildShield(Vector3 localDown, float pieceRadius)
        {
            var right = new Vector3(localDown.z, 0f, -localDown.x);
            var up = -localDown;
            var halfWidth = pieceRadius * HalfWidthFactor;
            var height = pieceRadius * HeightFactor;
            var vertices = new[]
            {
                Vector3.zero,
                (-right * halfWidth) + (up * height * 0.42f),
                (right * halfWidth) + (up * height * 0.42f),
                (right * halfWidth * 0.82f) + (localDown * height * 0.10f),
                localDown * height * 0.58f,
                (-right * halfWidth * 0.82f) + (localDown * height * 0.10f)
            };
            var triangles = new[]
            {
                0, 2, 1,
                0, 3, 2,
                0, 4, 3,
                0, 5, 4,
                0, 1, 5
            };
            ownedMesh.Clear();
            ownedMesh.SetVertices(vertices);
            ownedMesh.SetTriangles(triangles, 0);
            ownedMesh.RecalculateBounds();
            ownedMesh.RecalculateNormals();
        }

        private void EnsureResources()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.sortingOrder = 29;
            }

            if (ownedMesh == null)
            {
                ownedMesh = new Mesh { name = ShieldObjectName };
                ownedMesh.MarkDynamic();
                meshFilter.sharedMesh = ownedMesh;
            }

            if (ownedMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    throw new InvalidOperationException("The Defense modifier shield requires the URP Unlit shader.");
                }

                ownedMaterial = new Material(shader)
                {
                    name = ShieldObjectName + " Material",
                    color = ShieldColor
                };
                ownedMaterial.SetFloat("_Cull", (float)CullMode.Off);
            }

            meshRenderer.sharedMaterial = ownedMaterial;
            if (valueText == null)
            {
                var valueObject = new GameObject(ValueObjectName);
                valueObject.transform.SetParent(transform, false);
                valueText = valueObject.AddComponent<TextMesh>();
                valueText.anchor = TextAnchor.MiddleCenter;
                valueText.alignment = TextAlignment.Center;
                valueText.fontStyle = FontStyle.Bold;
                valueText.fontSize = 64;
                valueText.color = ValueColor;
                valueText.GetComponent<MeshRenderer>().sortingOrder = 30;
            }
        }

        public void ReleaseOwnedResources()
        {
            var mesh = ownedMesh;
            var material = ownedMaterial;
            ownedMesh = null;
            ownedMaterial = null;
            if (meshFilter != null && meshFilter.sharedMesh == mesh) meshFilter.sharedMesh = null;
            if (meshRenderer != null) meshRenderer.sharedMaterial = null;
            DestroyOwned(mesh);
            DestroyOwned(material);
        }

        private static void DestroyOwned(UnityEngine.Object resource)
        {
            if (resource == null) return;
            if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(resource);
            else UnityEngine.Object.DestroyImmediate(resource);
        }

        private void OnDestroy() => ReleaseOwnedResources();
    }
}
