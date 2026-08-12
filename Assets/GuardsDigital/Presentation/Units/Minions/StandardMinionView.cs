using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using UnityEngine;

namespace GuardsDigital.Presentation.Units.Minions
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class StandardMinionView : MonoBehaviour
    {
        public const float DefaultVerticalOffset = 0.18f;
        public const float DefaultScale = 1.35f;

        [SerializeField, HideInInspector]
        private string boundMinionId;

        [SerializeField, HideInInspector]
        private float outerRadius = HexWorldLayout.DefaultOuterRadius;

        private QuickGameMinionSetupState setupState;

        public QuickGameMinionSetupState SetupState => setupState;
        public MinionId BoundMinionId => setupState != null
            ? setupState.MinionId
            : string.IsNullOrWhiteSpace(boundMinionId)
                ? default
                : new MinionId(boundMinionId);
        public float OuterRadius => outerRadius;

        public void Configure(QuickGameMinionSetupState state, float outerRadius)
        {
            setupState = state ?? throw new System.ArgumentNullException(nameof(state));
            boundMinionId = state.MinionId.Value;
            this.outerRadius = outerRadius;
            gameObject.name = $"{state.TeamSide} {state.MinionType} Minion {state.MinionId.Value}";
            transform.localPosition = HexWorldLayout.ToWorld(state.Coordinate, outerRadius) +
                (Vector3.up * DefaultVerticalOffset);
            transform.localRotation = Quaternion.Euler(-90f, 0f, 0f) *
                Quaternion.AngleAxis(180f, Vector3.forward);
            transform.localScale = Vector3.one * DefaultScale;

            var renderer = GetComponent<SpriteRenderer>();
            renderer.sprite = StandardMinionArtCatalog.Load(state.TeamSide, state.MinionType);
            renderer.sortingOrder = 10;
        }
    }
}
