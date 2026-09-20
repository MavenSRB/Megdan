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
        Ally = 6,
        DefensePositive = 7,
        DefenseNegative = 8
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
        public const string DefensePositiveHaloMaterialName = "Positive Defense Corner Shine Halo";
        public const string DefensePositiveCoreMaterialName = "Positive Defense Corner Shine Core";
        public const string DefenseNegativeHaloMaterialName = "Negative Defense Corner Shine Halo";
        public const string DefenseNegativeCoreMaterialName = "Negative Defense Corner Shine Core";
        public const string TargetEnemyOutlineContourMaterialName = "Enemy Target Hex Outline Contour";
        public const string TargetEnemyOutlineCoreMaterialName = "Enemy Target Hex Outline Core";
        public const string TargetFriendlyOutlineContourMaterialName =
            "Friendly Target Hex Outline Contour";
        public const string TargetFriendlyOutlineCoreMaterialName =
            "Friendly Target Hex Outline Core";
        public const string FullEdgeMeshName = "Six Hex Edge Outline";

        /// <summary>
        /// Milestone 8.62 hex-outline variant, sized in WORLD units on a unit-radius hex - the same
        /// units <see cref="BoardTargetRing.EligibleWidth"/> is measured in, and the same number, so
        /// the edge reads as heavily as the ring the user is comparing it against.
        /// </summary>
        public const float OutlineCoreHalfWidth = BoardTargetRing.EligibleWidth / 2f;

        /// <summary>The dark rim carried each side of the coloured edge, so it holds on sand.</summary>
        public const float OutlineContourBleed = 0.022f;

        public const float OutlineContourHalfWidth = OutlineCoreHalfWidth + OutlineContourBleed;

        /// <summary>How far the dark rim is carried towards black from the edge own colour.</summary>
        public const float OutlineContourDarkening = 0.55f;

        public const string DefensePositiveSaturatedCoreMaterialName =
            "Positive Defense Corner Shine Saturated Core";
        public const string DefenseNegativeSaturatedCoreMaterialName =
            "Negative Defense Corner Shine Saturated Core";

        // The whole board now speaks one six-corner language. Green is travel, cyan is return to the
        // board, blood red is an Attack or removal, warm ivory is a teammate, and cool neutral grey
        // is every other selectable effect. A darker slate keeps a projected result distinct.
        /// <summary>
        /// How far a mark's narrow filament is carried towards white. Milestone 8.35z39's light pass
        /// chose it so the filament keeps the shape on the bright half of the board.
        /// </summary>
        public const float CoreWhiteLift = 0.82f;

        /// <summary>
        /// Milestone 8.62, at the user's direction. The ATTACK mark's filament keeps far more of its
        /// own blood red than every other mark's does.
        ///
        /// <para>The user, on a screenshot of an attack's targets: "can you make targeting highlights
        /// more visible, they are almost impossible to see like this.. Blood red light instead of this
        /// pink i think." The declared colour was already the reviewed blood red; what reached the
        /// screen was this filament, and at <see cref="CoreWhiteLift"/> the blood red arrives as
        /// <c>(0.95, 0.82, 0.82)</c> - a pale pink, and a pale warm pink on painted SAND at that,
        /// which is the lowest-contrast thing this mark could have been. At this lift it arrives as
        /// <c>(0.78, 0.23, 0.24)</c>: blood red against the sand by hue AND by value, and still well
        /// above the board's dark rock and water by value.</para>
        ///
        /// <para>Only the harmful mark moves. The other seven keep <see cref="CoreWhiteLift"/>,
        /// because none of them was reported as unreadable and the six-corner language's whole point
        /// is that a colour means one thing everywhere it appears.</para>
        /// </summary>
        public const float AttackCoreWhiteLift = 0.22f;

        public static readonly Color MovementColor = new Color(0.22f, 1f, 0.48f, 1f);
        public static readonly Color AttackColor = new Color(0.72f, 0.025f, 0.035f, 1f);
        public static readonly Color RespawnColor = new Color(0.05f, 0.78f, 1f, 1f);
        public static readonly Color AllyColor = new Color(0.92f, 0.90f, 0.80f, 1f);
        public static readonly Color EffectColor = new Color(0.62f, 0.68f, 0.72f, 1f);
        public static readonly Color EffectResultColor = new Color(0.42f, 0.48f, 0.55f, 1f);
        public static readonly Color DefensePositiveColor = new Color(0.02f, 0.70f, 0.18f, 1f);
        public static readonly Color DefenseNegativeColor = new Color(1f, 0.16f, 0.05f, 1f);

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
        private readonly Material defensePositiveHalo;
        private readonly Material defensePositiveCore;
        private readonly Material defenseNegativeHalo;
        private readonly Material defenseNegativeCore;

        // Milestone 8.35z39. See SaturatedCore: the composite mark's core is the one core in this
        // type that is NOT white-biased, and before the light pass it borrowed the halo material to
        // get that. Halos are light now, so it needs saturated paint of its own.
        private readonly Material defensePositiveSaturatedCore;
        private readonly Material defenseNegativeSaturatedCore;

        // Milestone 8.62's hex-outline variant. A continuous six-edge outline of the WHOLE tile,
        // in the ring's own two colours, on the same two layers the corner marks use - a target hex
        // shows the outline INSTEAD of its corner bars, never both.
        private readonly Mesh outlineContourMesh;
        private readonly Mesh outlineCoreMesh;
        private readonly Material targetEnemyOutlineContour;
        private readonly Material targetEnemyOutlineCore;
        private readonly Material targetFriendlyOutlineContour;
        private readonly Material targetFriendlyOutlineCore;

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
            movementHalo = CreateHaloMaterial(MovementHaloMaterialName, MovementColor);
            movementCore = CreateMaterial(MovementCoreMaterialName, Color.Lerp(MovementColor, Color.white, CoreWhiteLift));
            attackHalo = CreateHaloMaterial(AttackHaloMaterialName, AttackColor);
            attackCore = CreateMaterial(
                AttackCoreMaterialName, Color.Lerp(AttackColor, Color.white, AttackCoreWhiteLift));
            respawnHalo = CreateHaloMaterial(RespawnHaloMaterialName, RespawnColor);
            respawnCore = CreateMaterial(RespawnCoreMaterialName, Color.Lerp(RespawnColor, Color.white, CoreWhiteLift));
            effectHalo = CreateHaloMaterial(EffectHaloMaterialName, EffectColor);
            effectCore = CreateMaterial(EffectCoreMaterialName, Color.Lerp(EffectColor, Color.white, CoreWhiteLift));
            effectResultHalo = CreateHaloMaterial(EffectResultHaloMaterialName, EffectResultColor);
            effectResultCore = CreateMaterial(EffectResultCoreMaterialName,
                Color.Lerp(EffectResultColor, Color.white, CoreWhiteLift));
            allyHalo = CreateHaloMaterial(AllyHaloMaterialName, AllyColor);
            allyCore = CreateMaterial(AllyCoreMaterialName, Color.Lerp(AllyColor, Color.white, CoreWhiteLift));
            defensePositiveHalo = CreateHaloMaterial(
                DefensePositiveHaloMaterialName, DefensePositiveColor);
            defensePositiveCore = CreateMaterial(
                DefensePositiveCoreMaterialName,
                Color.Lerp(DefensePositiveColor, Color.white, 0.78f));
            defenseNegativeHalo = CreateHaloMaterial(
                DefenseNegativeHaloMaterialName, DefenseNegativeColor);
            defenseNegativeCore = CreateMaterial(
                DefenseNegativeCoreMaterialName,
                Color.Lerp(DefenseNegativeColor, Color.white, 0.78f));
            defensePositiveSaturatedCore = CreateMaterial(
                DefensePositiveSaturatedCoreMaterialName, DefensePositiveColor);
            defenseNegativeSaturatedCore = CreateMaterial(
                DefenseNegativeSaturatedCoreMaterialName, DefenseNegativeColor);
            outlineContourMesh = BuildSixEdgeMesh(outerRadius, OutlineContourHalfWidth);
            outlineContourMesh.name = FullEdgeMeshName + " Contour";
            outlineCoreMesh = BuildSixEdgeMesh(outerRadius, OutlineCoreHalfWidth);
            outlineCoreMesh.name = FullEdgeMeshName + " Core";
            targetEnemyOutlineContour = CreateMaterial(
                TargetEnemyOutlineContourMaterialName,
                Color.Lerp(BoardTargetRing.BloodRedColor, Color.black, OutlineContourDarkening));
            targetEnemyOutlineCore = CreateMaterial(
                TargetEnemyOutlineCoreMaterialName, BoardTargetRing.BloodRedColor);
            targetFriendlyOutlineContour = CreateMaterial(
                TargetFriendlyOutlineContourMaterialName,
                Color.Lerp(BoardTargetRing.AffectColor, Color.black, OutlineContourDarkening));
            targetFriendlyOutlineCore = CreateMaterial(
                TargetFriendlyOutlineCoreMaterialName, BoardTargetRing.AffectColor);
        }

        public Mesh HaloMesh => haloMesh;
        public Mesh CoreMesh => coreMesh;
        public Mesh OutlineContourMesh => outlineContourMesh;
        public Mesh OutlineCoreMesh => outlineCoreMesh;

        public int MarkCount => CornerCount;
        public int OwnedNativeResourceCount => 26;

        /// <summary>
        /// Milestone 8.62's hex-outline variant. The light and the paint a target hex's continuous
        /// outline is drawn with, chosen by the target's SIDE. The user: "Keep the colors, blood red
        /// on enemy, green on ally" - and the colours are the RING's own two, so the variant is the
        /// approved palette on a different shape rather than a third palette.
        /// </summary>
        public Material OutlineContour(BoardTargetOutlineSide side) =>
            side == BoardTargetOutlineSide.Enemy
                ? targetEnemyOutlineContour
                : side == BoardTargetOutlineSide.Friendly
                    ? targetFriendlyOutlineContour
                    : null;

        public Material OutlineCore(BoardTargetOutlineSide side) =>
            side == BoardTargetOutlineSide.Enemy
                ? targetEnemyOutlineCore
                : side == BoardTargetOutlineSide.Friendly
                    ? targetFriendlyOutlineCore
                    : null;

        /// <summary>
        /// Milestone 8.35z39. The composite mark's core: the contributing kind's own colour at FULL
        /// strength, painted rather than added.
        ///
        /// <para><b>Why this family exists at all.</b> A tile that is both an Attack target and a
        /// Defense contributor draws the attack's halo around the contribution's core, and that one
        /// core must be SATURATED - the white-biased cores every other mark uses vanish at gameplay
        /// scale inside a blood-red halo, which is a requirement this type has carried since the
        /// Defense-preview work and which its fixture states in those words. Before the light pass it
        /// was met by borrowing the HALO material, whose colour was exactly right. Halos are light
        /// now, so borrowing one hands this core the luminous alpha and it stops being saturated
        /// paint: the first complete Edit Mode run of the light pass caught precisely that, expecting
        /// alpha 1.000 and measuring 0.620.</para>
        ///
        /// <para>So the borrow becomes a declaration. Only the two Defense kinds can ever be asked
        /// for - <c>Set</c> admits no other contribution kind - and both are blended paint at full
        /// alpha, which is what the composite core has always been.</para>
        /// </summary>
        public Material SaturatedCore(HexCornerHighlightKind kind) =>
            kind == HexCornerHighlightKind.DefensePositive
                ? defensePositiveSaturatedCore
                : kind == HexCornerHighlightKind.DefenseNegative
                    ? defenseNegativeSaturatedCore
                    : null;

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
                                : kind == HexCornerHighlightKind.DefensePositive
                                    ? defensePositiveHalo
                                    : kind == HexCornerHighlightKind.DefenseNegative
                                        ? defenseNegativeHalo
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
                                : kind == HexCornerHighlightKind.DefensePositive
                                    ? defensePositiveCore
                                    : kind == HexCornerHighlightKind.DefenseNegative
                                        ? defenseNegativeCore
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

        /// <summary>
        /// Milestone 8.62's hex-outline variant. The same bar construction as the corner marks, but
        /// each of the six EDGES drawn whole instead of two 30% stubs at each corner, so the tile
        /// reads as one closed hexagon. Each bar is extended by its own half-width at both ends, so
        /// consecutive edges meet without a notch at the corner.
        /// </summary>
        private static Mesh BuildSixEdgeMesh(float radius, float halfWidth)
        {
            var vertices = new Vector3[CornerCount * 4];
            var triangles = new int[CornerCount * 6];
            for (var edge = 0; edge < CornerCount; edge++)
            {
                var angle = Mathf.Deg2Rad * (30f + (edge * 60f));
                var nextAngle = Mathf.Deg2Rad * (30f + (((edge + 1) % 6) * 60f));
                var start = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                var end = new Vector3(
                    Mathf.Cos(nextAngle) * radius, 0f, Mathf.Sin(nextAngle) * radius);
                var along = (end - start).normalized * halfWidth;
                AddBar(vertices, triangles, edge * 4, edge * 6, start - along, end + along, halfWidth);
            }

            var result = new Mesh { name = FullEdgeMeshName };
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

        // Milestone 8.31b. Copied from the shipped blended Unlit template rather than built from a
        // found shader: a corner mark blended only at runtime has no variant in a player build and
        // draws opaque there, which is the defect that hid the whole board.
        private static Material CreateMaterial(string name, Color color) =>
            BoardOverlayMaterial.CreateUnlitBlended(name, color);

        /// <summary>
        /// Milestone 8.35z39. The halo, drawn as LIGHT.
        ///
        /// <para><b>The mark's shape does not move, and that is the point.</b> The user's tightening
        /// of the light direction is that the marking shapes are already right - keep every one of
        /// them, keep what each colour means, and change only what the pixels inside them do. So
        /// <see cref="BuildSixCornerMesh"/> is untouched, both meshes are built from the same two
        /// ratios they always were, every declared colour above is the colour it was, and the
        /// halo/core pairing is the pairing this type has had since it was written.</para>
        ///
        /// <para><b>Why the halo alone.</b> The pair is already shaped like a light and was only ever
        /// rendered like paint: a wide soft-edged skirt at <c>0.075</c> under a narrow filament at
        /// <c>0.034</c> that is lerped 78-82% of the way to white. Additive on the WIDE layer turns
        /// that skirt into the spill a light throws onto the surface around it - the board's sand,
        /// its printed icons and its painted detail all still visible inside the mark, brightened in
        /// the mark's own hue - while the narrow near-white filament keeps carrying the shape at full
        /// strength on the bright half of the board, where added light saturates toward white and
        /// would otherwise cost the mark its outline. Lighting BOTH layers is the version that looks
        /// correct on dark ground and dissolves on sand; this is the one that holds on both.</para>
        /// </summary>
        private static Material CreateHaloMaterial(string name, Color color) =>
            BoardOverlayMaterial.CreateLuminous(name, color);

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
            DestroyOwned(defensePositiveHalo);
            DestroyOwned(defensePositiveCore);
            DestroyOwned(defenseNegativeHalo);
            DestroyOwned(defenseNegativeCore);
            DestroyOwned(defensePositiveSaturatedCore);
            DestroyOwned(defenseNegativeSaturatedCore);
            DestroyOwned(outlineContourMesh);
            DestroyOwned(outlineCoreMesh);
            DestroyOwned(targetEnemyOutlineContour);
            DestroyOwned(targetEnemyOutlineCore);
            DestroyOwned(targetFriendlyOutlineContour);
            DestroyOwned(targetFriendlyOutlineCore);
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

        public HexCornerHighlightKind BaseKind { get; private set; }
        public HexCornerHighlightKind ContributionKind { get; private set; }
        public HexCornerHighlightKind Kind => BaseKind != HexCornerHighlightKind.None
            ? BaseKind
            : ContributionKind;
        public bool IsHovered { get; private set; }

        /// <summary>
        /// Milestone 8.62's hex-outline variant. Which side's continuous outline this tile is
        /// wearing instead of its corner bars, or <c>None</c> for the corner bars.
        /// </summary>
        public BoardTargetOutlineSide OutlineSide { get; private set; }

        /// <summary>
        /// Whether the six marks are currently drawn as one closed hexagon rather than as the six
        /// corner bars. A target's space does that when the pointer is on it.
        /// </summary>
        public bool IsFullEdgeOutline { get; private set; }
        public int VisibleMarkCount => Kind == HexCornerHighlightKind.None ? 0 : HexCornerHighlightTemplate.CornerCount;
        public Bounds LocalBounds => core == null ? default : core.localBounds;
        public Color VisibleCoreMaterialColor => core == null || core.sharedMaterial == null
            ? default
            : core.sharedMaterial.color;

        public void Configure(HexCornerHighlightTemplate sharedTemplate)
        {
            template = sharedTemplate ?? throw new ArgumentNullException(nameof(sharedTemplate));
            halo = EnsureLayer("Corner Shine Halo", 0.075f, template.HaloMesh);
            core = EnsureLayer("Corner Shine Core", 0.085f, template.CoreMesh);
            Set(HexCornerHighlightKind.None, false);
        }

        /// <param name="outlineSide">
        /// Milestone 8.62's hex-outline variant. When it is not <c>None</c> the two layers draw the
        /// tile's six EDGES as one continuous hexagon in that side's colour instead of the six
        /// corner bars - the user: "Let's try how it looks when we remove the outline of the token,
        /// just outline the hex", and "Keep the colors, blood red on enemy, green on ally". The
        /// declared <paramref name="kind"/> is still recorded, so every reader that asks what a
        /// space MEANS reads the same answer under either treatment.
        /// </param>
        public void Set(
            HexCornerHighlightKind kind,
            bool hovered,
            HexCornerHighlightKind contributionKind = HexCornerHighlightKind.None,
            BoardTargetOutlineSide outlineSide = BoardTargetOutlineSide.None)
        {
            if (contributionKind != HexCornerHighlightKind.None &&
                contributionKind != HexCornerHighlightKind.DefensePositive &&
                contributionKind != HexCornerHighlightKind.DefenseNegative)
            {
                throw new ArgumentOutOfRangeException(nameof(contributionKind));
            }

            if ((kind != HexCornerHighlightKind.None ||
                 contributionKind != HexCornerHighlightKind.None) && template == null)
            {
                throw new InvalidOperationException("The shared corner-highlight template is not configured.");
            }

            BaseKind = kind;
            ContributionKind = contributionKind;
            IsHovered = Kind != HexCornerHighlightKind.None && hovered;
            OutlineSide = Kind == HexCornerHighlightKind.None
                ? BoardTargetOutlineSide.None
                : outlineSide;
            IsFullEdgeOutline = OutlineSide != BoardTargetOutlineSide.None &&
                (IsHovered || BoardTargetCuePresentation.OutlinesTheWholeHex);
            if (halo == null || core == null) return;
            var visible = Kind != HexCornerHighlightKind.None;
            halo.enabled = visible;
            core.enabled = visible;
            if (!visible) return;
            var haloKind = kind != HexCornerHighlightKind.None ? kind : contributionKind;
            var coreKind = contributionKind != HexCornerHighlightKind.None ? contributionKind : kind;
            var composite = kind != HexCornerHighlightKind.None &&
                contributionKind != HexCornerHighlightKind.None;
            // Milestone 8.35z39: the composite core takes saturated PAINT of its own rather than
            // borrowing the halo, which is light now. Same colour as before, alpha 1 as before.
            var coreMaterial = composite
                ? template.SaturatedCore(coreKind)
                : template.Core(coreKind);
            if (OutlineSide != BoardTargetOutlineSide.None)
            {
                // The variant, and it is PAINT rather than light. The first photographed attempt
                // drew the edge as an additive halo under a white-lifted filament, and the result
                // measured as a pale pink line with a whitish halo - the same fault the user
                // rejected in the original wash. So the wide layer is a DARK rim of the edge own
                // colour and the narrow layer is the ring colour itself, both at full alpha:
                // (194,10,24) on an enemy, the Affect green on a friendly piece, at the ring own
                // EligibleWidth. Pointing at it swaps in the thicker pair and lifts the colour, so
                // hovered and resting cannot be mistaken for each other.
                var wider = template.OutlineContour(OutlineSide);
                var edge = template.OutlineCore(OutlineSide);

                // AT REST the shapes are the six corner bars this type has always drawn - the user,
                // shown a zoom of the original cue: "No, not the ring, but the old highlight mark."
                // Only the paint changes. POINTED AT, the same two layers become one closed hexagon
                // at the same edge width: "Then on mouseover we can do full hex."
                SetLayerMesh(halo, IsFullEdgeOutline
                    ? template.OutlineContourMesh
                    : template.HaloMesh);
                SetLayerMesh(core, IsFullEdgeOutline
                    ? template.OutlineCoreMesh
                    : template.CoreMesh);
                halo.sharedMaterial = wider;
                core.sharedMaterial = edge;
                halo.transform.localScale = Vector3.one;
                core.transform.localScale = Vector3.one;
                SetColor(halo, WithAlpha(wider.color, 1f));
                SetColor(core, WithAlpha(edge.color, 1f));
                return;
            }

            SetLayerMesh(halo, template.HaloMesh);
            SetLayerMesh(core, template.CoreMesh);
            halo.sharedMaterial = template.Halo(haloKind);
            core.sharedMaterial = coreMaterial;
            halo.transform.localScale = Vector3.one;
            core.transform.localScale = Vector3.one;
            // Milestone 8.62. The harmful mark is drawn at full strength whether or not the pointer
            // is on it: "they are almost impossible to see like this". Every other mark keeps the
            // reviewed resting pair, so hover still means something everywhere else on the board,
            // and the attack mark still has somewhere to go - its own hovered treatment is the
            // brightened one the shared `ConsequenceColor` lift already gives the piece cue.
            var harmful = haloKind == HexCornerHighlightKind.Attack;
            var strength = IsHovered || harmful ? 1f : 0.84f;
            var haloAlpha = IsHovered || harmful ? 1f : 0.72f;
            SetColor(halo, WithAlpha(template.Halo(haloKind).color, haloAlpha));
            SetColor(core, WithAlpha(coreMaterial.color, strength));
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

        private static void SetLayerMesh(Renderer layer, Mesh mesh)
        {
            var filter = layer.GetComponent<MeshFilter>();
            if (filter != null) filter.sharedMesh = mesh;
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
