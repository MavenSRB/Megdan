using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Core.Units.Minions;
using UnityEngine;

namespace GuardsDigital.Presentation.Units.Minions
{
    /// <summary>
    /// What the board says about one minion while a minion choice is pending. `Selected` outranks
    /// `Highlighted`: a minion already chosen keeps saying so while the player points at the others.
    /// </summary>
    public enum MinionChoiceCueKind
    {
        None = 0,
        Highlighted = 1,
        Selected = 2
    }

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

        /// <summary>
        /// One marker for every visible minion-choice cue kind. Both kinds say "this minion", so the
        /// shape is the same and only the color separates a selection from the control being pointed
        /// at; a downward triangle reads as pointing at the token it sits above.
        /// </summary>
        public const string ChoiceCueMarker = "▼";
        public const float ChoiceCueCharacterSize = 0.09f;

        private static readonly Vector3 DefenseCueLocalPosition = new Vector3(0f, 0.85f, -0.15f);

        [SerializeField, HideInInspector]
        private string boundMinionId;

        [SerializeField, HideInInspector]
        private float outerRadius = HexWorldLayout.DefaultOuterRadius;

        private MinionOccupancy occupancy;
        private TextMesh defenseCue;
        private TextMesh defenseCueBackground;
        private TextMesh choiceCue;
        private TextMesh choiceCueBackground;

        public MinionOccupancy Occupancy => occupancy;
        public MinionId BoundMinionId => occupancy != null
            ? occupancy.MinionId
            : string.IsNullOrWhiteSpace(boundMinionId)
                ? default
                : new MinionId(boundMinionId);
        public float OuterRadius => outerRadius;
        public int DefenseCueAmount { get; private set; }
        public MinionChoiceCueKind ChoiceCueKind { get; private set; }

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
                CreateCuePair("Defense Contribution Cue", out defenseCue, out defenseCueBackground);
            }

            defenseCue.gameObject.SetActive(true);
            defenseCueBackground.gameObject.SetActive(true);
            defenseCue.text = amount > 0 ? "+1" : "-1";
            defenseCue.color = amount > 0
                ? new Color(0.08f, 0.62f, 0.18f)
                : new Color(0.72f, 0.12f, 0.08f);
            CenterCueText(defenseCue, defenseCueBackground);
        }

        /// <summary>
        /// Shows this minion's part in a pending minion choice above the token, in the same board
        /// position and proportions as the Defense contribution cue. The two cannot appear together:
        /// a Defense response resolves inside a turn and a minion choice only at end of round.
        /// </summary>
        public void SetChoiceCue(MinionChoiceCueKind kind)
        {
            if (kind != MinionChoiceCueKind.None && kind != MinionChoiceCueKind.Highlighted &&
                kind != MinionChoiceCueKind.Selected)
            {
                throw new System.ArgumentOutOfRangeException(nameof(kind));
            }

            ChoiceCueKind = kind;
            if (kind == MinionChoiceCueKind.None)
            {
                if (choiceCue != null)
                {
                    choiceCue.gameObject.SetActive(false);
                    choiceCueBackground.gameObject.SetActive(false);
                }

                return;
            }

            if (choiceCue == null)
            {
                CreateCuePair("Minion Choice Cue", out choiceCue, out choiceCueBackground);
                choiceCue.characterSize = ChoiceCueCharacterSize;
            }

            choiceCue.gameObject.SetActive(true);
            choiceCueBackground.gameObject.SetActive(true);
            choiceCue.text = ChoiceCueMarker;
            choiceCue.color = kind == MinionChoiceCueKind.Selected
                ? new Color(0.72f, 0.12f, 0.08f)
                : new Color(0.86f, 0.55f, 0.06f);
            CenterCueText(choiceCue, choiceCueBackground);
        }

        /// <summary>
        /// Builds one board-facing cue: mirrored text over a vertically oval dark-grey backing. The
        /// constants keep their `DefenseCue` names because milestone 6.1b's reviewed proportions are
        /// pinned by documentation and fixtures; every cue reuses those, and a caller needing a
        /// different glyph size overrides only that after construction.
        /// </summary>
        private void CreateCuePair(string cueName, out TextMesh cue, out TextMesh backing)
        {
            var cueObject = new GameObject(cueName);
            cueObject.transform.SetParent(transform, false);
            cueObject.transform.localPosition = DefenseCueLocalPosition;
            cueObject.transform.localRotation = Quaternion.identity;
            cueObject.transform.localScale = new Vector3(-1f, 1f, 1f);
            var backgroundObject = new GameObject($"{cueName} Background");
            backgroundObject.transform.SetParent(transform, false);
            backgroundObject.transform.localPosition = cueObject.transform.localPosition;
            backgroundObject.transform.localRotation = Quaternion.identity;
            backgroundObject.transform.localScale = new Vector3(
                1f,
                DefenseCueBackgroundVerticalScale,
                1f);
            backing = backgroundObject.AddComponent<TextMesh>();
            backing.anchor = TextAnchor.MiddleCenter;
            backing.alignment = TextAlignment.Center;
            backing.fontStyle = FontStyle.Bold;
            backing.fontSize = 64;
            backing.characterSize = DefenseCueBackgroundCharacterSize;
            backing.text = "●";
            backing.color = new Color(0.16f, 0.17f, 0.18f, 0.9f);
            backing.GetComponent<MeshRenderer>().sortingOrder = 19;
            cue = cueObject.AddComponent<TextMesh>();
            cue.anchor = TextAnchor.MiddleCenter;
            cue.alignment = TextAlignment.Center;
            cue.fontStyle = FontStyle.Bold;
            cue.fontSize = 64;
            cue.characterSize = DefenseCueCharacterSize;
            cue.GetComponent<MeshRenderer>().sortingOrder = 20;
        }

        private void CenterCueText(TextMesh cue, TextMesh backing)
        {
            cue.transform.localPosition = DefenseCueLocalPosition;
            var cueCenter = cue.GetComponent<MeshRenderer>().bounds.center;
            var backgroundCenter = backing.GetComponent<MeshRenderer>().bounds.center;
            var localOffset = transform.InverseTransformVector(backgroundCenter - cueCenter);
            cue.transform.localPosition += new Vector3(
                localOffset.x,
                localOffset.y + DefenseCueOpticalVerticalOffset,
                0f);
        }
    }
}
