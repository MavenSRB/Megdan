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
            if (identityChanged)
            {
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

        private BoardTargetRing EnsureTargetRing() =>
            targetRing != null ? targetRing : targetRing = BoardTargetRing.GetOrCreate(transform);

        private void OnDestroy()
        {
            if (!ReferenceEquals(targetRing, null))
            {
                targetRing.ReleaseOwnedResources();
            }
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

        private void Awake()
        {
            EnsureTargetRing();
        }
    }
}
