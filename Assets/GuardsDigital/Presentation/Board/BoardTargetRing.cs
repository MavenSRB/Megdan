using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>The static state one projected piece target communicates on the board.</summary>
    public enum BoardTargetCueState
    {
        None = 0,
        Eligible = 1,
        Selected = 2,
        Hovered = 3
    }

    /// <summary>
    /// The consequence the projected operation intends for a piece. It describes the operation being
    /// invoked, not whether later prevention or resolution will let that consequence happen.
    /// </summary>
    public enum BoardTargetConsequenceIntent
    {
        None = 0,
        Affect = 1,
        Removal = 2,
        Attack = 3,
        Ally = 4
    }

    /// <summary>
    /// How the visible piece exposes the base silhouette the ring must meet. Circular artwork keeps
    /// a true circle; generated bases expose the convex perimeter of their lowest mesh vertices.
    /// The caller supplies this fact so the shared cue never infers it from a name, material, team,
    /// or hard-coded side count.
    /// </summary>
    public enum BoardTargetFootprintKind
    {
        Circular = 1,
        ConvexMeshBase = 2
    }

    /// <summary>
    /// The one generated board-facing target cue shared by hero, minion, and token views.
    ///
    /// Its inner contour is measured from the current visible footprint: transformed sprite-local
    /// bounds retain a true circle, while a generated base supplies the convex contour of its lowest
    /// mesh vertices. The same construction therefore fits the official hero token, primitive fallback,
    /// configured minion tokens, and placed tokens without a world-AABB air gap. A translucent fill of
    /// that exact footprint makes the target immediately readable while keeping its art legible. It
    /// creates no collider or input surface. One retained mesh and three retained materials change state
    /// in place; clearing a target disables the renderer rather than destroying the cue.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class BoardTargetRing : MonoBehaviour
    {
        public const int EligibleSegmentCount = 8;
        public const int HoverTickCount = 4;
        public const float EligibleWidth = 0.11f;
        public const float SelectedWidth = 0.055f;
        public const float SelectedGap = 0.03f;
        public const float HoverWidth = 0.11f;
        public const float HoverTickLength = 0.14f;
        public const float HoverTickWidth = 0.07f;
        public const float KeylineWidth = 0.018f;
        public const float BoardSurfaceOffset = 0.028f;
        public const float EligibleWashAlpha = 0.18f;
        public const float SelectedWashAlpha = 0.23f;
        public const float HoverWashAlpha = BoardOverlayMaterial.OverlayAlpha;
        public const float PieceWashSurfaceOffset = 0.004f;

        private const int ArcSubdivisions = 4;
        private const int SolidRingSubdivisions = 64;
        private const float ColourLayerOffset = 0.001f;
        private const float GeometryTolerance = 0.00001f;
        private const float MaximumMiterRatio = 4f;
        private const string CueObjectName = "Board Target Ring";
        private const string KeylineMaterialName = "Board Target Ring Keyline";
        private const string ConsequenceMaterialName = "Board Target Ring Consequence";
        private const string WashMaterialName = "Board Target Ring Piece Wash";

        // User-reviewed 8.11d palette, exposed and pinned by focused tests so ring, keyline, and wash
        // retain the exact values accepted in the targeted Game View inspection.
        public static Color AffectColor => new Color32(27, 129, 71, 255);
        public static Color BloodRedColor => new Color32(194, 10, 24, 255);
        public static Color KeylineColor => new Color32(248, 247, 242, 255);

        private Mesh ownedMesh;
        private Material ownedKeylineMaterial;
        private Material ownedConsequenceMaterial;
        private Material ownedWashMaterial;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        public BoardTargetCueState State { get; private set; }
        public BoardTargetConsequenceIntent ConsequenceIntent { get; private set; }
        public float InnerRadius { get; private set; }
        public string AssistiveReading { get; private set; } = string.Empty;
        public Color VisibleConsequenceColor => ownedConsequenceMaterial == null
            ? default
            : ownedConsequenceMaterial.GetColor("_EmissionColor");
        public Color VisibleKeylineColor => ownedKeylineMaterial == null
            ? default
            : ownedKeylineMaterial.GetColor("_EmissionColor");
        public Color VisibleWashColor => ownedWashMaterial == null
            ? default
            : ownedWashMaterial.color;
        public bool IsVisible => meshRenderer != null && meshRenderer.enabled;
        public Mesh GeneratedMesh => ownedMesh;
        public int OwnedNativeResourceCount =>
            (ownedMesh == null ? 0 : 1) +
            (ownedKeylineMaterial == null ? 0 : 1) +
            (ownedConsequenceMaterial == null ? 0 : 1) +
            (ownedWashMaterial == null ? 0 : 1);

        /// <summary>
        /// Finds the retained cue directly below one piece or creates it once. Searching before
        /// creation makes runtime-created views safe across editor backup/domain restoration, where a
        /// non-serialized field may be lost while its generated child survives.
        /// </summary>
        public static BoardTargetRing GetOrCreate(Transform piece)
        {
            if (piece == null)
            {
                throw new ArgumentNullException(nameof(piece));
            }

            BoardTargetRing found = null;
            foreach (var candidate in piece.GetComponentsInChildren<BoardTargetRing>(true))
            {
                if (candidate.transform.parent != piece)
                {
                    continue;
                }

                if (found != null)
                {
                    throw new InvalidOperationException("One piece cannot own two target-ring cues.");
                }

                found = candidate;
            }

            if (found != null)
            {
                found.EnsureResources();
                return found;
            }

            var cueObject = new GameObject(CueObjectName);
            cueObject.transform.SetParent(piece, false);
            cueObject.layer = piece.gameObject.layer;
            var created = cueObject.AddComponent<BoardTargetRing>();
            created.EnsureResources();
            return created;
        }

        /// <summary>
        /// Applies one projected target state. <paramref name="boardPlaneWorldY"/> is the authoritative
        /// occupied space's plane: ring geometry is raised only enough to clear the board artwork and
        /// debug edge, while the exact-footprint wash is raised just above the piece art. A hidden cue
        /// needs no renderer bounds and simply retains its one instance.
        /// </summary>
        public void SetCue(
            BoardTargetCueState state,
            BoardTargetConsequenceIntent consequenceIntent,
            Renderer footprint,
            float boardPlaneWorldY,
            BoardTargetFootprintKind footprintKind)
        {
            Validate(state, consequenceIntent);
            if (footprintKind != BoardTargetFootprintKind.Circular &&
                footprintKind != BoardTargetFootprintKind.ConvexMeshBase)
            {
                throw new ArgumentOutOfRangeException(nameof(footprintKind));
            }

            EnsureResources();
            if (transform.parent != null)
            {
                gameObject.layer = transform.parent.gameObject.layer;
            }

            State = state;
            ConsequenceIntent = consequenceIntent;
            if (state == BoardTargetCueState.None)
            {
                AssistiveReading = string.Empty;
                meshRenderer.enabled = false;
                return;
            }

            if (footprint == null)
            {
                throw new ArgumentNullException(nameof(footprint));
            }

            meshRenderer.sortingLayerID = footprint.sortingLayerID;
            meshRenderer.sortingOrder = footprint.sortingOrder + 1;

            var bounds = footprint.bounds;
            var centre = bounds.center;
            var radius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            if (footprintKind == BoardTargetFootprintKind.Circular &&
                footprint is SpriteRenderer spriteRenderer && spriteRenderer.sprite != null)
            {
                var spriteBounds = spriteRenderer.sprite.bounds;
                centre = spriteRenderer.transform.TransformPoint(spriteBounds.center);
                radius = Mathf.Max(
                    spriteRenderer.transform.TransformVector(
                        Vector3.right * spriteBounds.extents.x).magnitude,
                    spriteRenderer.transform.TransformVector(
                        Vector3.up * spriteBounds.extents.y).magnitude);
            }

            if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(footprint), "A target ring requires a visible board footprint.");
            }

            transform.position = new Vector3(
                centre.x,
                boardPlaneWorldY + BoardSurfaceOffset,
                centre.z);
            transform.rotation = Quaternion.FromToRotation(
                Vector3.forward, BoardChoiceCue.ScreenUpInWorld);
            ApplyWorldUnitScale();

            var outline = footprintKind == BoardTargetFootprintKind.Circular
                ? FootprintOutline.Circle(radius)
                : CreateConvexMeshBaseOutline(footprint);
            InnerRadius = outline.MaximumRadius;

            SetVisibleColor(ownedKeylineMaterial, KeylineColor);
            var consequenceColor = ConsequenceColor(state, consequenceIntent);
            SetVisibleColor(ownedConsequenceMaterial, consequenceColor);
            SetWashColor(ownedWashMaterial, consequenceColor, WashAlpha(state));
            AssistiveReading = Describe(state, consequenceIntent);
            var washSurface = transform.InverseTransformPoint(new Vector3(
                centre.x,
                bounds.max.y + PieceWashSurfaceOffset,
                centre.z)).y;
            RebuildMesh(state, outline, washSurface);
            meshRenderer.enabled = true;
        }

        public static string Describe(
            BoardTargetCueState state, BoardTargetConsequenceIntent consequenceIntent)
        {
            Validate(state, consequenceIntent);
            if (state == BoardTargetCueState.None)
            {
                return string.Empty;
            }

            var stateText = state == BoardTargetCueState.Eligible
                ? "ELIGIBLE"
                : state == BoardTargetCueState.Selected
                    ? "SELECTED"
                    : "HOVERED";
            var intentText = consequenceIntent == BoardTargetConsequenceIntent.Removal
                ? "REMOVAL - BLOOD RED"
                : consequenceIntent == BoardTargetConsequenceIntent.Attack
                    ? "ATTACK - BLOOD RED"
                    : "AFFECT - DARK GREEN";
            return $"{stateText} TARGET - {intentText}";
        }

        private static void Validate(
            BoardTargetCueState state, BoardTargetConsequenceIntent consequenceIntent)
        {
            if (state != BoardTargetCueState.None &&
                state != BoardTargetCueState.Eligible &&
                state != BoardTargetCueState.Selected &&
                state != BoardTargetCueState.Hovered)
            {
                throw new ArgumentOutOfRangeException(nameof(state));
            }

            if (state == BoardTargetCueState.None)
            {
                if (consequenceIntent != BoardTargetConsequenceIntent.None)
                {
                    throw new ArgumentException(
                        "A hidden target ring carries no consequence intent.", nameof(consequenceIntent));
                }

                return;
            }

            if (consequenceIntent != BoardTargetConsequenceIntent.Affect &&
                consequenceIntent != BoardTargetConsequenceIntent.Removal &&
                consequenceIntent != BoardTargetConsequenceIntent.Attack &&
                consequenceIntent != BoardTargetConsequenceIntent.Ally)
            {
                throw new ArgumentOutOfRangeException(nameof(consequenceIntent));
            }
        }

        private static Color ConsequenceColor(
            BoardTargetCueState state, BoardTargetConsequenceIntent consequenceIntent)
        {
            var basis = consequenceIntent == BoardTargetConsequenceIntent.Removal ||
                consequenceIntent == BoardTargetConsequenceIntent.Attack
                ? BloodRedColor
                : AffectColor;
            var lift = state == BoardTargetCueState.Hovered ? 0.28f : 0f;
            return Color.Lerp(basis, Color.white, lift);
        }

        private static float WashAlpha(BoardTargetCueState state) =>
            state == BoardTargetCueState.Eligible
                ? EligibleWashAlpha
                : state == BoardTargetCueState.Selected
                    ? SelectedWashAlpha
                    : HoverWashAlpha;

        private void EnsureResources()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
            }

            // Domain restoration may preserve the generated child and its renderer references while
            // clearing these managed ownership fields. Adopt only this component's exact named
            // resources before creating anything, so restoration cannot orphan a native mesh or
            // material and a later OnDestroy still releases all four.
            if (ownedMesh == null && meshFilter.sharedMesh != null &&
                meshFilter.sharedMesh.name == CueObjectName)
            {
                ownedMesh = meshFilter.sharedMesh;
            }

            var retainedMaterials = meshRenderer.sharedMaterials;
            if (ownedKeylineMaterial == null)
            {
                ownedKeylineMaterial = retainedMaterials.FirstOrDefault(material =>
                    material != null && material.name == KeylineMaterialName);
            }

            if (ownedConsequenceMaterial == null)
            {
                ownedConsequenceMaterial = retainedMaterials.FirstOrDefault(material =>
                    material != null && material.name == ConsequenceMaterialName);
            }

            if (ownedWashMaterial == null)
            {
                ownedWashMaterial = retainedMaterials.FirstOrDefault(material =>
                    material != null && material.name == WashMaterialName);
            }

            if (ownedMesh == null)
            {
                ownedMesh = new Mesh { name = CueObjectName };
                ownedMesh.MarkDynamic();
                meshFilter.sharedMesh = ownedMesh;
            }

            if (ownedKeylineMaterial == null)
            {
                ownedKeylineMaterial = BoardOverlayMaterial.CreateRim(
                    KeylineMaterialName, KeylineColor);
            }

            if (ownedConsequenceMaterial == null)
            {
                ownedConsequenceMaterial = BoardOverlayMaterial.CreateRim(
                    ConsequenceMaterialName, AffectColor);
            }

            if (ownedWashMaterial == null)
            {
                ownedWashMaterial = BoardOverlayMaterial.Create(
                    WashMaterialName,
                    new Color(AffectColor.r, AffectColor.g, AffectColor.b, EligibleWashAlpha));
            }

            meshRenderer.sharedMaterials = new[]
            {
                ownedKeylineMaterial,
                ownedConsequenceMaterial,
                ownedWashMaterial
            };
            meshRenderer.enabled = State != BoardTargetCueState.None;
        }

        private static void SetVisibleColor(Material material, Color color)
        {
            var opaque = new Color(color.r, color.g, color.b, 1f);
            material.SetColor("_EmissionColor", opaque);
        }

        private static void SetWashColor(Material material, Color color, float alpha)
        {
            material.color = new Color(color.r, color.g, color.b, alpha);
        }

        private void ApplyWorldUnitScale()
        {
            var parentScale = transform.parent == null
                ? Vector3.one
                : transform.parent.lossyScale;
            transform.localScale = new Vector3(
                InverseScale(parentScale.x),
                InverseScale(parentScale.y),
                InverseScale(parentScale.z));
        }

        private static float InverseScale(float value)
        {
            var magnitude = Mathf.Abs(value);
            if (magnitude < 1e-5f || float.IsNaN(magnitude) || float.IsInfinity(magnitude))
            {
                throw new InvalidOperationException("A target ring cannot follow a zero-scaled piece.");
            }

            return 1f / magnitude;
        }

        private FootprintOutline CreateConvexMeshBaseOutline(Renderer footprint)
        {
            var meshFilter = footprint.GetComponent<MeshFilter>();
            var sourceMesh = meshFilter == null ? null : meshFilter.sharedMesh;
            if (sourceMesh == null)
            {
                throw new InvalidOperationException(
                    "A convex-mesh target footprint requires the rendered piece's MeshFilter.");
            }

            var minimumY = sourceMesh.bounds.min.y;
            var basePoints = sourceMesh.vertices
                .Where(vertex => Mathf.Abs(vertex.y - minimumY) <= GeometryTolerance)
                .Select(vertex => footprint.transform.TransformPoint(vertex))
                .Select(vertex => transform.InverseTransformPoint(vertex))
                .Select(vertex => new Vector2(vertex.x, vertex.z));
            var hull = ConvexHull(basePoints);
            if (hull.Length < 3)
            {
                throw new InvalidOperationException(
                    "A convex-mesh target footprint requires at least three base-perimeter points.");
            }

            for (var index = 0; index < hull.Length; index++)
            {
                var edge = hull[(index + 1) % hull.Length] - hull[index];
                if (Cross(edge, -hull[index]) < -GeometryTolerance)
                {
                    throw new InvalidOperationException(
                        "A target footprint must contain its projected board-space centre.");
                }
            }

            return FootprintOutline.Polygon(hull);
        }

        private static Vector2[] ConvexHull(IEnumerable<Vector2> source)
        {
            var sorted = source
                .OrderBy(point => point.x)
                .ThenBy(point => point.y)
                .ToArray();
            var unique = new List<Vector2>(sorted.Length);
            foreach (var point in sorted)
            {
                if (unique.Count == 0 ||
                    Vector2.Distance(unique[unique.Count - 1], point) > GeometryTolerance)
                {
                    unique.Add(point);
                }
            }

            if (unique.Count < 3)
            {
                return unique.ToArray();
            }

            var lower = new List<Vector2>();
            foreach (var point in unique)
            {
                while (lower.Count >= 2 && Cross(
                           lower[lower.Count - 1] - lower[lower.Count - 2],
                           point - lower[lower.Count - 1]) <= GeometryTolerance)
                {
                    lower.RemoveAt(lower.Count - 1);
                }

                lower.Add(point);
            }

            var upper = new List<Vector2>();
            for (var index = unique.Count - 1; index >= 0; index--)
            {
                var point = unique[index];
                while (upper.Count >= 2 && Cross(
                           upper[upper.Count - 1] - upper[upper.Count - 2],
                           point - upper[upper.Count - 1]) <= GeometryTolerance)
                {
                    upper.RemoveAt(upper.Count - 1);
                }

                upper.Add(point);
            }

            lower.RemoveAt(lower.Count - 1);
            upper.RemoveAt(upper.Count - 1);
            lower.AddRange(upper);
            return lower.ToArray();
        }

        private void RebuildMesh(
            BoardTargetCueState state,
            FootprintOutline outline,
            float washSurface)
        {
            var vertices = new List<Vector3>();
            var keylineTriangles = new List<int>();
            var consequenceTriangles = new List<int>();
            var washTriangles = new List<int>();

            AddPieceWash(vertices, washTriangles, outline, washSurface);

            switch (state)
            {
                case BoardTargetCueState.Eligible:
                    AddSegmentedRing(vertices, keylineTriangles, consequenceTriangles, outline);
                    break;
                case BoardTargetCueState.Selected:
                    AddDoubleRing(vertices, keylineTriangles, consequenceTriangles, outline);
                    break;
                case BoardTargetCueState.Hovered:
                    AddHoverRing(vertices, keylineTriangles, consequenceTriangles, outline);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }

            ownedMesh.Clear();
            ownedMesh.SetVertices(vertices);
            ownedMesh.subMeshCount = 3;
            ownedMesh.SetTriangles(keylineTriangles, 0);
            ownedMesh.SetTriangles(consequenceTriangles, 1);
            ownedMesh.SetTriangles(washTriangles, 2);
            ownedMesh.RecalculateBounds();
            ownedMesh.RecalculateNormals();
        }

        private static void AddPieceWash(
            List<Vector3> vertices,
            List<int> triangles,
            FootprintOutline outline,
            float y)
        {
            var boundary = outline.Boundary();
            var centre = vertices.Count;
            vertices.Add(new Vector3(0f, y, 0f));
            var first = vertices.Count;
            foreach (var point in boundary)
            {
                vertices.Add(new Vector3(point.x, y, point.y));
            }

            for (var index = 0; index < boundary.Count; index++)
            {
                triangles.Add(centre);
                triangles.Add(first + ((index + 1) % boundary.Count));
                triangles.Add(first + index);
            }
        }

        private static void AddSegmentedRing(
            List<Vector3> vertices,
            List<int> keylineTriangles,
            List<int> consequenceTriangles,
            FootprintOutline outline)
        {
            var step = Mathf.PI * 2f / EligibleSegmentCount;
            var gap = step * 0.22f;
            for (var segment = 0; segment < EligibleSegmentCount; segment++)
            {
                var start = (segment * step) + (gap * 0.5f);
                var end = ((segment + 1) * step) - (gap * 0.5f);
                AddKeylinedArc(
                    vertices, keylineTriangles, consequenceTriangles,
                    outline, 0f, EligibleWidth, start, end, ArcSubdivisions);
            }
        }

        private static void AddDoubleRing(
            List<Vector3> vertices,
            List<int> keylineTriangles,
            List<int> consequenceTriangles,
            FootprintOutline outline)
        {
            AddKeylinedArc(
                vertices, keylineTriangles, consequenceTriangles,
                outline, 0f, SelectedWidth, 0f, Mathf.PI * 2f,
                SolidRingSubdivisions);
            var outerOffset = SelectedWidth + KeylineWidth + SelectedGap;
            AddKeylinedArc(
                vertices, keylineTriangles, consequenceTriangles,
                outline, outerOffset, SelectedWidth, 0f, Mathf.PI * 2f,
                SolidRingSubdivisions);
        }

        private static void AddHoverRing(
            List<Vector3> vertices,
            List<int> keylineTriangles,
            List<int> consequenceTriangles,
            FootprintOutline outline)
        {
            AddKeylinedArc(
                vertices, keylineTriangles, consequenceTriangles,
                outline, 0f, HoverWidth, 0f, Mathf.PI * 2f,
                SolidRingSubdivisions);

            var consequenceOuterContour = outline.Offset(HoverWidth);
            for (var tick = 0; tick < HoverTickCount; tick++)
            {
                var angle = tick * Mathf.PI * 0.5f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var ringOuterRadius = consequenceOuterContour.PointAt(angle).magnitude;
                var overlap = HoverWidth * 0.08f;
                AddKeylinedTick(
                    vertices, keylineTriangles, consequenceTriangles,
                    direction, ringOuterRadius - overlap,
                    ringOuterRadius + HoverTickLength, HoverTickWidth);
            }
        }

        private static void AddKeylinedArc(
            List<Vector3> vertices,
            List<int> keylineTriangles,
            List<int> consequenceTriangles,
            FootprintOutline outline,
            float innerOffset,
            float width,
            float startAngle,
            float endAngle,
            int subdivisions)
        {
            var keylineInner = outline.Offset(innerOffset);
            var keylineOuter = outline.Offset(innerOffset + width + KeylineWidth);
            var consequenceInner = outline.Offset(innerOffset);
            var consequenceOuter = outline.Offset(innerOffset + width);
            var angles = ArcAngles(
                startAngle, endAngle, subdivisions,
                keylineInner, keylineOuter, consequenceInner, consequenceOuter);
            AddArc(
                vertices, keylineTriangles,
                keylineInner, keylineOuter, angles, 0f);
            AddArc(
                vertices, consequenceTriangles,
                consequenceInner, consequenceOuter, angles, ColourLayerOffset);
        }

        private static IReadOnlyList<float> ArcAngles(
            float startAngle,
            float endAngle,
            int subdivisions,
            params OutlineContour[] contours)
        {
            var angles = new List<float>(subdivisions + 1);
            for (var step = 0; step <= subdivisions; step++)
            {
                angles.Add(Mathf.Lerp(startAngle, endAngle, step / (float)subdivisions));
            }

            foreach (var contour in contours)
            {
                foreach (var cornerAngle in contour.CornerAngles())
                {
                    if (cornerAngle > startAngle + GeometryTolerance &&
                        cornerAngle < endAngle - GeometryTolerance)
                    {
                        angles.Add(cornerAngle);
                    }
                }
            }

            angles.Sort();
            var unique = new List<float>(angles.Count);
            foreach (var angle in angles)
            {
                if (unique.Count == 0 ||
                    Mathf.Abs(unique[unique.Count - 1] - angle) > GeometryTolerance)
                {
                    unique.Add(angle);
                }
            }

            return unique;
        }

        private static void AddArc(
            List<Vector3> vertices,
            List<int> triangles,
            OutlineContour innerContour,
            OutlineContour outerContour,
            IReadOnlyList<float> angles,
            float y)
        {
            var first = vertices.Count;
            foreach (var angle in angles)
            {
                var inner = innerContour.PointAt(angle);
                var outer = outerContour.PointAt(angle);
                vertices.Add(new Vector3(inner.x, y, inner.y));
                vertices.Add(new Vector3(outer.x, y, outer.y));
            }

            for (var step = 0; step < angles.Count - 1; step++)
            {
                var inner = first + (step * 2);
                var outer = inner + 1;
                var nextInner = inner + 2;
                var nextOuter = inner + 3;
                triangles.Add(inner);
                triangles.Add(nextInner);
                triangles.Add(outer);
                triangles.Add(outer);
                triangles.Add(nextInner);
                triangles.Add(nextOuter);
            }
        }

        private static float Cross(Vector2 left, Vector2 right) =>
            (left.x * right.y) - (left.y * right.x);

        private sealed class FootprintOutline
        {
            private readonly bool circular;
            private readonly float circleRadius;
            private readonly Vector2[] polygon;

            private FootprintOutline(float radius)
            {
                circular = true;
                circleRadius = radius;
                polygon = Array.Empty<Vector2>();
                MaximumRadius = radius;
            }

            private FootprintOutline(Vector2[] vertices)
            {
                circular = false;
                circleRadius = 0f;
                polygon = vertices;
                MaximumRadius = vertices.Max(vertex => vertex.magnitude);
            }

            public float MaximumRadius { get; }

            public static FootprintOutline Circle(float radius) => new FootprintOutline(radius);

            public static FootprintOutline Polygon(Vector2[] vertices) =>
                new FootprintOutline(vertices ?? throw new ArgumentNullException(nameof(vertices)));

            public IReadOnlyList<Vector2> Boundary()
            {
                if (!circular)
                {
                    return polygon;
                }

                var boundary = new Vector2[SolidRingSubdivisions];
                for (var index = 0; index < boundary.Length; index++)
                {
                    var angle = index * Mathf.PI * 2f / boundary.Length;
                    boundary[index] = new Vector2(
                        Mathf.Cos(angle) * circleRadius,
                        Mathf.Sin(angle) * circleRadius);
                }

                return boundary;
            }

            public OutlineContour Offset(float distance)
            {
                if (distance < 0f || float.IsNaN(distance) || float.IsInfinity(distance))
                {
                    throw new ArgumentOutOfRangeException(nameof(distance));
                }

                if (circular)
                {
                    return OutlineContour.Circle(circleRadius + distance);
                }

                if (distance <= GeometryTolerance)
                {
                    return OutlineContour.Polygon(polygon);
                }

                var offset = new Vector2[polygon.Length];
                for (var index = 0; index < polygon.Length; index++)
                {
                    var previous = polygon[(index + polygon.Length - 1) % polygon.Length];
                    var current = polygon[index];
                    var next = polygon[(index + 1) % polygon.Length];
                    var previousEdge = (current - previous).normalized;
                    var nextEdge = (next - current).normalized;
                    var previousNormal = new Vector2(previousEdge.y, -previousEdge.x);
                    var nextNormal = new Vector2(nextEdge.y, -nextEdge.x);
                    var miter = previousNormal + nextNormal;
                    if (miter.sqrMagnitude <= GeometryTolerance * GeometryTolerance)
                    {
                        throw new InvalidOperationException(
                            "A convex target footprint cannot contain a straight-back corner.");
                    }

                    miter.Normalize();
                    var denominator = Vector2.Dot(miter, nextNormal);
                    if (denominator <= GeometryTolerance)
                    {
                        throw new InvalidOperationException(
                            "A convex target footprint produced an invalid outward miter.");
                    }

                    var ratio = 1f / denominator;
                    if (ratio > MaximumMiterRatio)
                    {
                        throw new InvalidOperationException(
                            "A target footprint corner is too acute for the bounded ring miter.");
                    }

                    offset[index] = current + (miter * distance * ratio);
                }

                return OutlineContour.Polygon(offset);
            }
        }

        private sealed class OutlineContour
        {
            private readonly bool circular;
            private readonly float circleRadius;
            private readonly Vector2[] polygon;

            private OutlineContour(float radius)
            {
                circular = true;
                circleRadius = radius;
                polygon = Array.Empty<Vector2>();
            }

            private OutlineContour(Vector2[] vertices)
            {
                circular = false;
                circleRadius = 0f;
                polygon = vertices;
            }

            public static OutlineContour Circle(float radius) => new OutlineContour(radius);

            public static OutlineContour Polygon(Vector2[] vertices) =>
                new OutlineContour(vertices ?? throw new ArgumentNullException(nameof(vertices)));

            public IEnumerable<float> CornerAngles()
            {
                if (circular)
                {
                    yield break;
                }

                foreach (var vertex in polygon)
                {
                    var angle = Mathf.Atan2(vertex.y, vertex.x);
                    yield return angle < 0f ? angle + (Mathf.PI * 2f) : angle;
                }
            }

            public Vector2 PointAt(float angle)
            {
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (circular)
                {
                    return direction * circleRadius;
                }

                var nearest = float.PositiveInfinity;
                for (var index = 0; index < polygon.Length; index++)
                {
                    var from = polygon[index];
                    var edge = polygon[(index + 1) % polygon.Length] - from;
                    var denominator = Cross(direction, edge);
                    if (Mathf.Abs(denominator) <= GeometryTolerance)
                    {
                        continue;
                    }

                    var rayDistance = Cross(from, edge) / denominator;
                    var edgeDistance = Cross(from, direction) / denominator;
                    if (rayDistance >= -GeometryTolerance &&
                        edgeDistance >= -GeometryTolerance &&
                        edgeDistance <= 1f + GeometryTolerance)
                    {
                        nearest = Mathf.Min(nearest, Mathf.Max(0f, rayDistance));
                    }
                }

                if (float.IsInfinity(nearest))
                {
                    throw new InvalidOperationException(
                        "A convex target contour did not intersect a radial mesh sample.");
                }

                return direction * nearest;
            }
        }

        private static void AddKeylinedTick(
            List<Vector3> vertices,
            List<int> keylineTriangles,
            List<int> consequenceTriangles,
            Vector2 direction,
            float innerRadius,
            float outerRadius,
            float width)
        {
            AddTick(
                vertices, keylineTriangles, direction,
                innerRadius - KeylineWidth,
                outerRadius + KeylineWidth,
                width + (KeylineWidth * 2f), 0f);
            AddTick(
                vertices, consequenceTriangles, direction,
                innerRadius,
                outerRadius,
                width, ColourLayerOffset);
        }

        private static void AddTick(
            List<Vector3> vertices,
            List<int> triangles,
            Vector2 direction,
            float innerRadius,
            float outerRadius,
            float width,
            float y)
        {
            var tangent = new Vector2(-direction.y, direction.x) * (width * 0.5f);
            var inner = direction * innerRadius;
            var outer = direction * outerRadius;
            var first = vertices.Count;
            vertices.Add(new Vector3(inner.x - tangent.x, y, inner.y - tangent.y));
            vertices.Add(new Vector3(inner.x + tangent.x, y, inner.y + tangent.y));
            vertices.Add(new Vector3(outer.x - tangent.x, y, outer.y - tangent.y));
            vertices.Add(new Vector3(outer.x + tangent.x, y, outer.y + tangent.y));
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first + 1);
            triangles.Add(first + 3);
            triangles.Add(first + 2);
        }

        /// <summary>
        /// Releases the four native resources owned by this retained cue. Piece hosts call this
        /// before their hierarchy is torn down because Unity can destroy a generated child component
        /// without delivering its child <c>OnDestroy</c> early enough to release resources still
        /// referenced by that child's renderer. The component also calls it for independent teardown.
        /// Repeated calls are harmless.
        /// </summary>
        internal void ReleaseOwnedResources()
        {
            var mesh = ownedMesh;
            var keyline = ownedKeylineMaterial;
            var consequence = ownedConsequenceMaterial;
            var wash = ownedWashMaterial;

            ownedMesh = null;
            ownedKeylineMaterial = null;
            ownedConsequenceMaterial = null;
            ownedWashMaterial = null;

            if (meshFilter != null && meshFilter.sharedMesh == mesh)
            {
                meshFilter.sharedMesh = null;
            }

            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterials = Array.Empty<Material>();
            }

            DestroyOwnedResource(mesh);
            DestroyOwnedResource(keyline);
            DestroyOwnedResource(consequence);
            DestroyOwnedResource(wash);
        }

        private static void DestroyOwnedResource(UnityEngine.Object resource)
        {
            if (resource == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                Destroy(resource);
            }
            else
            {
                // Every resource reaching this owner was created at runtime. Unity can still treat a
                // mesh/material assigned through a shared renderer slot as asset-like during hierarchy
                // teardown; allow destruction explicitly after first detaching those slots above.
                DestroyImmediate(resource, true);
            }
        }

        private void OnDestroy() => ReleaseOwnedResources();
    }
}
