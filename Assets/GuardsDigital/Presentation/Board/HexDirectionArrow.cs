using System;
using GuardsDigital.Core.Board;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// Milestone 8.46d. One shared arrow construction, built exactly the way
    /// <see cref="HexCornerHighlightTemplate"/> is: a wide soft halo drawn as LIGHT under a narrow
    /// near-white core drawn as paint, both copied from the shipped Resources templates so the
    /// blended variant reaches a player build (the milestone 8.31b trap
    /// <see cref="BoardOverlayMaterial"/> exists to stop each caller repeating).
    ///
    /// <para>The colour is <see cref="HexCornerHighlightTemplate.EffectColor"/> - the board's declared
    /// "every other selectable effect" grey - because an arrow IS an effect-choice candidate. It says
    /// the same thing the six corner marks say, in the one shape a corner mark cannot: which way.</para>
    ///
    /// <para>The mesh points along +X and is centred on its own origin, so a caller orients it with a
    /// single <c>Quaternion.FromToRotation(Vector3.right, outward)</c> and never has to know six
    /// angles. That is deliberate: the outward vector is derived from
    /// <see cref="HexWorldLayout.ToWorld"/>, which is the one place this project states where a hex
    /// stands, so an arrow can never drift from the lattice it is drawn on.</para>
    /// </summary>
    public sealed class HexDirectionArrowTemplate : IDisposable
    {
        public const string MeshName = "Hex Direction Arrow";
        public const string HaloMaterialName = "Direction Arrow Shine Halo";
        public const string CoreMaterialName = "Direction Arrow Shine Core";

        /// <summary>How far the arrow reaches from the centre of the space it stands on.</summary>
        public const float LengthRatio = 0.46f;

        /// <summary>The shaft's half-width and the head's, as fractions of the hex's outer radius.</summary>
        public const float ShaftHalfWidthRatio = 0.13f;
        public const float HeadHalfWidthRatio = 0.30f;

        /// <summary>Where the head starts, as a fraction of the outer radius.</summary>
        public const float HeadStartRatio = 0.02f;

        /// <summary>How much narrower the core filament is than the halo it sits inside.</summary>
        public const float CoreWidthScale = 0.5f;

        private readonly Mesh haloMesh;
        private readonly Mesh coreMesh;
        private readonly Material halo;
        private readonly Material core;

        public HexDirectionArrowTemplate(float outerRadius)
        {
            if (outerRadius <= 0f || float.IsNaN(outerRadius) || float.IsInfinity(outerRadius))
            {
                throw new ArgumentOutOfRangeException(nameof(outerRadius));
            }

            haloMesh = BuildArrowMesh(outerRadius, 1f);
            haloMesh.name = MeshName + " Halo";
            coreMesh = BuildArrowMesh(outerRadius, CoreWidthScale);
            coreMesh.name = MeshName + " Core";
            halo = BoardOverlayMaterial.CreateLuminous(
                HaloMaterialName, HexCornerHighlightTemplate.EffectColor);
            core = BoardOverlayMaterial.CreateUnlitBlended(
                CoreMaterialName,
                Color.Lerp(HexCornerHighlightTemplate.EffectColor, Color.white, 0.82f));
        }

        public Mesh HaloMesh => haloMesh;
        public Mesh CoreMesh => coreMesh;
        public Material Halo => halo;
        public Material Core => core;

        /// <summary>Two meshes and two materials, however many arrows point at them.</summary>
        public int OwnedNativeResourceCount => 4;

        /// <summary>
        /// A flat arrow in the XZ plane pointing along +X, centred on the origin. Seven vertices and
        /// three triangles: a shaft bar and a head, wound the way
        /// <see cref="HexCornerHighlightTemplate"/>'s own bars are so the face reads from above.
        /// </summary>
        private static Mesh BuildArrowMesh(float outerRadius, float widthScale)
        {
            var length = LengthRatio * outerRadius;
            var shaftHalfWidth = ShaftHalfWidthRatio * outerRadius * widthScale;
            var headHalfWidth = HeadHalfWidthRatio * outerRadius * widthScale;
            var headStart = HeadStartRatio * outerRadius;

            var vertices = new Vector3[7];
            // The shaft, tail to the head's base.
            vertices[0] = new Vector3(-length, 0f, shaftHalfWidth);
            vertices[1] = new Vector3(-length, 0f, -shaftHalfWidth);
            vertices[2] = new Vector3(headStart, 0f, -shaftHalfWidth);
            vertices[3] = new Vector3(headStart, 0f, shaftHalfWidth);
            // The head.
            vertices[4] = new Vector3(headStart, 0f, headHalfWidth);
            vertices[5] = new Vector3(length, 0f, 0f);
            vertices[6] = new Vector3(headStart, 0f, -headHalfWidth);

            var triangles = new[] { 0, 2, 1, 0, 3, 2, 4, 5, 6 };

            var result = new Mesh { name = MeshName, hideFlags = HideFlags.DontSave };
            result.vertices = vertices;
            result.triangles = triangles;
            result.RecalculateBounds();
            result.RecalculateNormals();
            return result;
        }

        public void Dispose()
        {
            DestroyOwned(haloMesh);
            DestroyOwned(coreMesh);
            DestroyOwned(halo);
            DestroyOwned(core);
        }

        private static void DestroyOwned(UnityEngine.Object value)
        {
            if (value == null) return;
            if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }

    /// <summary>
    /// Milestone 8.46d. One arrow affordance around the hero a declared
    /// <c>EffectChoiceValueVocabulary.HexDirection</c> choice is asking, standing ON the neighbouring
    /// hex in that direction.
    ///
    /// <para><b>Standing on the neighbour is what makes it answerable.</b> The board's own click path
    /// raycasts the board plane, finds the nearest drawn tile and forwards that coordinate as a
    /// destination intent; an arrow drawn on the neighbour therefore needs no input plumbing of its
    /// own, and <see cref="Match.BoardInteractionPresenter"/> resolves the coordinate back to the
    /// exact Select the prompt control submits. The direction is never decoded from geometry: the
    /// candidate carries its own command, the way milestone 8.16c's system-transition destinations
    /// already do.</para>
    ///
    /// <para>It is created lazily by the board view and hidden rather than destroyed, the idiom
    /// <see cref="Match.ActingHeroMarker"/> follows: a board that is never asked a direction
    /// allocates nothing at all.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HexDirectionArrow : MonoBehaviour
    {
        public const string ArrowObjectName = "Hex Direction Arrow";
        public const string HaloLayerName = "Direction Arrow Halo";
        public const string CoreLayerName = "Direction Arrow Core";

        /// <summary>
        /// Direction answers remain readable when a hero, minion or token occupies the neighbouring
        /// space they name. Board pieces and their ordinary rings/shields top out at order 30; card
        /// state pips remain above this at their dedicated order 100.
        /// </summary>
        public const int SortingOrder = 31;

        /// <summary>The two heights the six-corner marks already use, so nothing z-fights them.</summary>
        public const float HaloHeight = 0.075f;
        public const float CoreHeight = 0.085f;

        private MeshRenderer halo;
        private MeshRenderer core;
        private HexDirectionArrowTemplate template;
        private MaterialPropertyBlock propertyBlock;

        public bool IsVisible => core != null && core.enabled;
        public bool IsHovered { get; private set; }

        /// <summary>Which of p.17's six directions this arrow answers, while it is drawn.</summary>
        public HexDirection Direction { get; private set; }

        /// <summary>The space it stands on - the neighbour a click on it names.</summary>
        public HexCoordinate Space { get; private set; }

        public static HexDirectionArrow Create(HexDirectionArrowTemplate sharedTemplate)
        {
            if (sharedTemplate == null) throw new ArgumentNullException(nameof(sharedTemplate));
            var created = new GameObject(ArrowObjectName).AddComponent<HexDirectionArrow>();
            created.template = sharedTemplate;
            created.halo = created.EnsureLayer(HaloLayerName, HaloHeight, sharedTemplate.HaloMesh);
            created.core = created.EnsureLayer(CoreLayerName, CoreHeight, sharedTemplate.CoreMesh);
            created.SetHidden();
            return created;
        }

        /// <summary>
        /// Places the arrow on one neighbouring space and points it away from the chooser.
        /// </summary>
        /// <param name="parent">The frame the board's tiles are positioned in, so the arrow lands on
        /// the lattice however the board root itself is placed.</param>
        /// <param name="localPosition">Where that space stands in the same frame.</param>
        /// <param name="outward">The vector from the chooser's space to this one, in that frame.</param>
        public void SetVisible(
            HexDirection direction,
            HexCoordinate space,
            Transform parent,
            Vector3 localPosition,
            Vector3 outward,
            bool hovered)
        {
            if (template == null)
            {
                throw new InvalidOperationException("The shared direction-arrow template is not configured.");
            }

            if (outward.sqrMagnitude <= Mathf.Epsilon)
            {
                throw new ArgumentException(
                    "An arrow points from the chooser to a neighbour, so the two cannot be the same " +
                    "space.", nameof(outward));
            }

            Direction = direction;
            Space = space;
            IsHovered = hovered;
            if (transform.parent != parent)
            {
                transform.SetParent(parent, false);
            }

            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.FromToRotation(Vector3.right, outward);
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);
            halo.enabled = true;
            core.enabled = true;
            SetColor(halo, WithAlpha(template.Halo.color, hovered ? 1f : 0.72f));
            SetColor(core, WithAlpha(template.Core.color, hovered ? 1f : 0.84f));
        }

        /// <summary>The pause has resolved, so nothing is drawn. The object is retained, not destroyed.</summary>
        public void SetHidden()
        {
            IsHovered = false;
            if (halo != null) halo.enabled = false;
            if (core != null) core.enabled = false;
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
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = SortingOrder;
            renderer.sharedMaterial = layerName == HaloLayerName ? template.Halo : template.Core;
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
