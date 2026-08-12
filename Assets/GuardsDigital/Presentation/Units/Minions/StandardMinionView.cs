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
        private TextMesh defenseCue;

        public QuickGameMinionSetupState SetupState => setupState;
        public MinionId BoundMinionId => setupState != null
            ? setupState.MinionId
            : string.IsNullOrWhiteSpace(boundMinionId)
                ? default
                : new MinionId(boundMinionId);
        public float OuterRadius => outerRadius;
        public int DefenseCueAmount { get; private set; }

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

        public void SetDefenseContributionCue(int amount)
        {
            if (amount != -1 && amount != 0 && amount != 1)
            {
                throw new System.ArgumentOutOfRangeException(nameof(amount));
            }

            DefenseCueAmount = amount;
            if (amount == 0)
            {
                if (defenseCue != null)
                {
                    defenseCue.gameObject.SetActive(false);
                }

                return;
            }

            if (defenseCue == null)
            {
                var cueObject = new GameObject("Defense Contribution Cue");
                cueObject.transform.SetParent(transform, false);
                cueObject.transform.localPosition = new Vector3(0f, 0.85f, -0.15f);
                cueObject.transform.localRotation = Quaternion.identity;
                defenseCue = cueObject.AddComponent<TextMesh>();
                defenseCue.anchor = TextAnchor.MiddleCenter;
                defenseCue.alignment = TextAlignment.Center;
                defenseCue.fontStyle = FontStyle.Bold;
                defenseCue.fontSize = 64;
                defenseCue.characterSize = 0.11f;
                defenseCue.GetComponent<MeshRenderer>().sortingOrder = 20;
            }

            defenseCue.gameObject.SetActive(true);
            defenseCue.text = amount > 0 ? "+1" : "-1";
            defenseCue.color = amount > 0
                ? new Color(0.2f, 1f, 0.35f)
                : new Color(1f, 0.35f, 0.2f);
        }
    }
}
