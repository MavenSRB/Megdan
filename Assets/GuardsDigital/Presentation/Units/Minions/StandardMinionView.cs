using System.Collections.Generic;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Core.Units.Minions;
using UnityEngine;

namespace GuardsDigital.Presentation.Units.Minions
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StandardMinionView : MonoBehaviour
    {
        public const float DefaultVerticalOffset = 0.18f;
        /// <summary>
        /// Milestone 8.11a, at the user's request: raised from 1.35. With the official face beneath the
        /// board, a token that left any of its space uncovered showed the space's own printed icon
        /// around its edge, which read as a second piece rather than as the board underneath. The token
        /// now covers its space, and stays well inside the 1.732 between one space and the next.
        /// </summary>
        public const float DefaultScale = 1.62f;
        [SerializeField, HideInInspector]
        private string boundMinionId;

        [SerializeField, HideInInspector]
        private float outerRadius = HexWorldLayout.DefaultOuterRadius;

        private MinionOccupancy occupancy;
        private BoardTargetRing targetRing;
        private MinionBattlePickBadges pickBadges;
        private int defenseContributionAmount;
        private Mesh tokenBodyMesh;
        private Material tokenBodyMaterial;
        private GameObject tokenBody;
        public Renderer TokenBodyRenderer => tokenBody == null ? null : tokenBody.GetComponent<MeshRenderer>();

        public MinionOccupancy Occupancy => occupancy;
        public MinionId BoundMinionId => occupancy != null
            ? occupancy.MinionId
            : string.IsNullOrWhiteSpace(boundMinionId)
                ? default
                : new MinionId(boundMinionId);
        public float OuterRadius => outerRadius;
        public BoardTargetRing TargetRing => targetRing;
        public BoardTargetCueState TargetCueState => targetRing == null
            ? BoardTargetCueState.None
            : targetRing.State;
        public BoardTargetConsequenceIntent TargetConsequenceIntent => targetRing == null
            ? BoardTargetConsequenceIntent.None
            : targetRing.ConsequenceIntent;

        /// <summary>Milestone 9.44. Signed hover contribution, or zero when this minion is unrelated.</summary>
        public int DefenseContributionAmount => defenseContributionAmount;

        /// <summary>
        /// Tints only a projected contributor green or blood red. No value text is created here;
        /// the net signed modifier belongs to the hovered hero's shield.
        /// </summary>
        public void SetDefenseContributionHighlight(int amount)
        {
            if (amount != -1 && amount != 0 && amount != 1)
            {
                throw new System.ArgumentOutOfRangeException(nameof(amount));
            }

            defenseContributionAmount = amount;
            GetComponent<SpriteRenderer>().color = amount > 0
                ? HexCornerHighlightTemplate.DefensePositiveColor
                : amount < 0
                    ? HexCornerHighlightTemplate.DefenseNegativeColor
                    : Color.white;
        }

        public void Configure(MinionOccupancy state, float outerRadius)
        {
            if (state == null) throw new System.ArgumentNullException(nameof(state));
            var identityChanged = occupancy != null && !occupancy.MinionId.Equals(state.MinionId);
            occupancy = state;
            boundMinionId = state.MinionId.Value;
            this.outerRadius = outerRadius;
            gameObject.name = $"{state.TeamSide} {state.MinionType} Minion {state.MinionId.Value}";
            transform.localPosition = HexWorldLayout.ToWorld(state.Coordinate, outerRadius) +
                (Vector3.up * DefaultVerticalOffset);
            // Milestone 8.11a: this had the board-facing rotation written out by hand, which is why it
            // alone stayed crooked when the framing camera moved onto the board's axes. It reads the
            // shared construction now, so the token and every cue hanging off it turn together.
            transform.localRotation = BoardChoiceCue.BoardFacingRotation;
            transform.localScale = Vector3.one * DefaultScale;

            var renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = StandardMinionArtCatalog.Load(state.TeamSide, state.MinionType);
            renderer.sortingOrder = 10;
            EnsureTokenBody(renderer.sprite);
            if (identityChanged)
            {
                SetDefenseContributionHighlight(0);
                SetTargetCue(BoardTargetCueState.None, BoardTargetConsequenceIntent.None);
            }
            else
            {
                RefreshTargetCue();
            }
        }

        /// <summary>
        /// Projects this minion's typed target state into its one retained fitted ring and translucent
        /// exact-footprint wash.
        /// The cue adds no collider and does not own selection or legality.
        /// </summary>
        public void SetTargetCue(
            BoardTargetCueState state,
            BoardTargetConsequenceIntent consequenceIntent)
        {
            EnsureTargetRing().SetCue(
                state,
                consequenceIntent,
                state == BoardTargetCueState.None ? null : GetComponent<SpriteRenderer>(),
                BoardPlaneWorldY(),
                BoardTargetFootprintKind.Circular);
        }

        /// <summary>
        /// Milestone 8.62. Whether this piece is currently drawn as something a fitted ring can be
        /// measured against - see <see cref="BoardTargetRing.CanFit"/>. A minion whose sprite the art
        /// catalog has not supplied keeps the corner marks on its space and wears no ring.
        /// </summary>
        public bool CanWearATargetCue =>
            BoardTargetRing.CanFit(
                GetComponent<SpriteRenderer>(), BoardTargetFootprintKind.Circular);

        /// <summary>
        /// Milestone 9.29. Draws the tokens of the teammates who picked this minion for a Minion
        /// Battle removal, or clears them for an empty list. A minion whose sprite the art catalog has
        /// not supplied measures its radius from its space instead.
        /// </summary>
        public void SetPickBadges(IReadOnlyList<MinionBattlePickBadges.Picker> pickers)
        {
            if (pickBadges == null && (pickers == null || pickers.Count == 0))
            {
                return;
            }

            if (pickBadges == null)
            {
                pickBadges = MinionBattlePickBadges.GetOrCreate(transform);
            }

            var renderer = GetComponent<SpriteRenderer>();
            var radius = renderer != null && renderer.sprite != null
                ? Mathf.Min(renderer.bounds.extents.x, renderer.bounds.extents.z)
                : 0f;
            if (radius <= 0.0001f)
            {
                radius = outerRadius * 0.8f;
            }

            pickBadges.Show(pickers, radius);
        }

        /// <summary>Milestone 9.29. The teammate tokens on this minion, for a fixture to read.</summary>
        public MinionBattlePickBadges PickBadges => pickBadges;

        private BoardTargetRing EnsureTargetRing() =>
            targetRing != null ? targetRing : targetRing = BoardTargetRing.GetOrCreate(transform);

        private void OnDestroy()
        {
            DestroyBodyResource(tokenBodyMesh);
            DestroyBodyResource(tokenBodyMaterial);
            if (!ReferenceEquals(targetRing, null))
            {
                targetRing.ReleaseOwnedResources();
            }

            if (!ReferenceEquals(pickBadges, null))
            {
                pickBadges.ReleaseOwnedResources();
            }
        }

        private void EnsureTokenBody(Sprite sprite)
        {
            if (tokenBody == null)
            {
                tokenBody = new GameObject("Solid creep token", typeof(MeshFilter), typeof(MeshRenderer));
                tokenBody.transform.SetParent(transform, false);
                float radius = Mathf.Min(sprite.bounds.extents.x, sprite.bounds.extents.y);
                tokenBodyMesh = CreateTokenBody(radius, BoardPieceDepth.Standard / DefaultScale);
                tokenBody.GetComponent<MeshFilter>().sharedMesh = tokenBodyMesh;
                tokenBodyMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                tokenBody.GetComponent<MeshRenderer>().sharedMaterial = tokenBodyMaterial;
            }
            tokenBodyMaterial.color = Heroes.HeroTokenArtwork.TeamRim(occupancy.TeamSide);
        }

        public static Mesh CreateTokenBody(float radius, float depth)
        {
            const int count = 64;
            var vertices = new Vector3[count * 2 + 2];
            var triangles = new int[count * 12];
            vertices[count * 2] = new Vector3(0, 0, -0.001f);
            vertices[count * 2 + 1] = new Vector3(0, 0, -depth);
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                vertices[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -0.001f);
                vertices[i + count] = new Vector3(vertices[i].x, vertices[i].y, -depth);
                int next = (i + 1) % count;
                int k = i * 12;
                triangles[k] = count * 2; triangles[k+1] = i; triangles[k+2] = next;
                triangles[k+3] = count * 2 + 1; triangles[k+4] = next + count; triangles[k+5] = i + count;
                triangles[k+6] = i; triangles[k+7] = i + count; triangles[k+8] = next;
                triangles[k+9] = next; triangles[k+10] = i + count; triangles[k+11] = next + count;
            }
            var mesh = new Mesh { name = "Solid circular creep token", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }

        private static void DestroyBodyResource(Object value)
        {
            if (value == null) return;
            if (UnityEngine.Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }

        private void RefreshTargetCue()
        {
            var ring = EnsureTargetRing();
            ring.SetCue(
                ring.State,
                ring.ConsequenceIntent,
                ring.State == BoardTargetCueState.None ? null : GetComponent<SpriteRenderer>(),
                BoardPlaneWorldY(),
                BoardTargetFootprintKind.Circular);
        }

        private float BoardPlaneWorldY() => transform.parent == null
            ? transform.position.y - DefaultVerticalOffset
            : transform.parent.TransformPoint(Vector3.zero).y;

        public void RefreshTokenFacing(Camera camera)
        {
            if (camera == null) return;
            var up = Vector3.ProjectOnPlane(camera.transform.up, Vector3.up);
            if (up.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(Vector3.up, up);
        }

        private void LateUpdate() => RefreshTokenFacing(Camera.main);

        private void Awake()
        {
            EnsureTargetRing();
        }
    }
}
