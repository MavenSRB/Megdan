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
        public const float DefaultScale = 1.35f;
        public const float DefenseCueCharacterSize = 0.06f;
        public const float DefenseCueBackgroundCharacterSize = 0.18f;
        public const float DefenseCueBackgroundVerticalScale = 1.35f;
        public const float DefenseCueOpticalVerticalOffset = -0.025f;

        private static readonly Vector3 DefenseCueLocalPosition = new Vector3(0f, 0.85f, -0.15f);

        [SerializeField, HideInInspector]
        private string boundMinionId;

        [SerializeField, HideInInspector]
        private float outerRadius = HexWorldLayout.DefaultOuterRadius;

        private MinionOccupancy occupancy;
        private TextMesh defenseCue;
        private TextMesh defenseCueBackground;

        public MinionOccupancy Occupancy => occupancy;
        public MinionId BoundMinionId => occupancy != null
            ? occupancy.MinionId
            : string.IsNullOrWhiteSpace(boundMinionId)
                ? default
                : new MinionId(boundMinionId);
        public float OuterRadius => outerRadius;
        public int DefenseCueAmount { get; private set; }

        public void Configure(MinionOccupancy state, float outerRadius)
        {
            occupancy = state ?? throw new System.ArgumentNullException(nameof(state));
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
                    defenseCueBackground.gameObject.SetActive(false);
                }

                return;
            }

            if (defenseCue == null)
            {
                var cueObject = new GameObject("Defense Contribution Cue");
                cueObject.transform.SetParent(transform, false);
                cueObject.transform.localPosition = DefenseCueLocalPosition;
                cueObject.transform.localRotation = Quaternion.identity;
                cueObject.transform.localScale = new Vector3(-1f, 1f, 1f);
                var backgroundObject = new GameObject("Defense Contribution Cue Background");
                backgroundObject.transform.SetParent(transform, false);
                backgroundObject.transform.localPosition = cueObject.transform.localPosition;
                backgroundObject.transform.localRotation = Quaternion.identity;
                backgroundObject.transform.localScale = new Vector3(
                    1f,
                    DefenseCueBackgroundVerticalScale,
                    1f);
                defenseCueBackground = backgroundObject.AddComponent<TextMesh>();
                defenseCueBackground.anchor = TextAnchor.MiddleCenter;
                defenseCueBackground.alignment = TextAlignment.Center;
                defenseCueBackground.fontStyle = FontStyle.Bold;
                defenseCueBackground.fontSize = 64;
                defenseCueBackground.characterSize = DefenseCueBackgroundCharacterSize;
                defenseCueBackground.text = "●";
                defenseCueBackground.color = new Color(0.16f, 0.17f, 0.18f, 0.9f);
                defenseCueBackground.GetComponent<MeshRenderer>().sortingOrder = 19;
                defenseCue = cueObject.AddComponent<TextMesh>();
                defenseCue.anchor = TextAnchor.MiddleCenter;
                defenseCue.alignment = TextAlignment.Center;
                defenseCue.fontStyle = FontStyle.Bold;
                defenseCue.fontSize = 64;
                defenseCue.characterSize = DefenseCueCharacterSize;
                defenseCue.GetComponent<MeshRenderer>().sortingOrder = 20;
            }

            defenseCue.gameObject.SetActive(true);
            defenseCueBackground.gameObject.SetActive(true);
            defenseCue.text = amount > 0 ? "+1" : "-1";
            defenseCue.color = amount > 0
                ? new Color(0.08f, 0.62f, 0.18f)
                : new Color(0.72f, 0.12f, 0.08f);
            CenterDefenseCueText();
        }

        private void CenterDefenseCueText()
        {
            defenseCue.transform.localPosition = DefenseCueLocalPosition;
            var cueCenter = defenseCue.GetComponent<MeshRenderer>().bounds.center;
            var backgroundCenter = defenseCueBackground.GetComponent<MeshRenderer>().bounds.center;
            var localOffset = transform.InverseTransformVector(backgroundCenter - cueCenter);
            defenseCue.transform.localPosition += new Vector3(
                localOffset.x,
                localOffset.y + DefenseCueOpticalVerticalOffset,
                0f);
        }
    }
}
