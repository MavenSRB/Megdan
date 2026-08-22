using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Presentation.Units.Minions;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// One placed token on the board, drawn as a flat disc under a board-facing name.
    ///
    /// Milestone 6.2d added it. Until then the whole Presentation assembly named a token nowhere, so
    /// three implemented operations - milestone 6.3's Clear, and milestone 6.6a's Replace and Remove -
    /// changed authoritative state that a player could only infer from a side effect. Everything drawn
    /// here comes off the projected instance: the coordinate it stands on and the display name its
    /// trusted definition carries. There is no catalog lookup and no per-definition art mapping, which
    /// is what makes a Replace read correctly for a token kind this view has never seen - the name
    /// changes and the space is never drawn empty, because the two are one projection apart.
    ///
    /// It is deliberately smaller than a unit and sits lower, so a highlighted tile still reads
    /// through, and it carries no pointer selector: a token is named by a prompt control and marked by
    /// its space, exactly as milestone 6.5c settled for every other board object a choice can name.
    ///
    /// Milestone 6.3a gave it the marking half of that sentence. Until then "marked by its space" was
    /// true of a hero and a minion and of nothing else - milestone 6.6a's finding was a control
    /// reading `TEST - HAZARD` beside a board that said nothing - because this view had no cue to set.
    /// It now carries the shared <see cref="BoardChoiceCue"/> construction and the same two
    /// treatments, so a Clear candidate, a Remove target and a minion up for removal are all pointed
    /// at in one vocabulary.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PlacedTokenView : MonoBehaviour
    {
        public const float DefaultVerticalOffset = 0.05f;

        /// <summary>The disc's share of a tile's outer radius, leaving the tile's own edge visible.</summary>
        public const float DiscRadiusFraction = 0.68f;

        public const float NameCharacterSize = 0.035f;

        /// <summary>
        /// The choice cue's glyph and size, taken from the shared construction rather than restated,
        /// so a token being pointed at reads exactly as a minion being pointed at does.
        /// </summary>
        public const string ChoiceCueMarker = BoardChoiceCue.Marker;
        public const float ChoiceCueCharacterSize = BoardChoiceCue.MarkerCharacterSize;

        /// <summary>
        /// How far up-screen the cue hangs, as a share of the tile's outer radius. The token is
        /// smaller than a unit and sits lower, so its cue is proportional to the board rather than
        /// borrowing the minion's absolute offset, which was measured against a 1.35-scaled sprite.
        /// </summary>
        public const float ChoiceCueRadiusFraction = 0.85f;

        private static readonly Vector3 NameLocalPosition = new Vector3(0f, 0.01f, 0f);

        [SerializeField, HideInInspector]
        private string boundTokenId;

        [SerializeField, HideInInspector]
        private float outerRadius = HexWorldLayout.DefaultOuterRadius;

        private PlacedToken token;
        private TextMesh nameLabel;
        private Transform cueAnchor;
        private TextMesh choiceCue;
        private TextMesh choiceCueBackground;
        private Mesh ownedMesh;
        private Material ownedMaterial;

        /// <summary>The projected instance this view last drew, or <c>null</c> before its first bind.</summary>
        public PlacedToken Token => token;

        public TokenInstanceId BoundTokenId => token != null
            ? token.Id
            : string.IsNullOrWhiteSpace(boundTokenId)
                ? default
                : new TokenInstanceId(boundTokenId);

        public HexCoordinate Coordinate => token != null ? token.Coordinate : default;

        public float OuterRadius => outerRadius;

        /// <summary>The name this view is currently drawing, or the empty string before its first bind.</summary>
        public string NameText => nameLabel != null ? nameLabel.text : string.Empty;

        /// <summary>What this piece currently says about its part in a pending decision.</summary>
        public MinionChoiceCueKind ChoiceCueKind { get; private set; }

        /// <summary>The drawn cue, for a fixture that has to read it through the camera.</summary>
        public TextMesh ChoiceCue => choiceCue;

        public void Configure(PlacedToken state, float radius)
        {
            token = state ?? throw new System.ArgumentNullException(nameof(state));
            if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius))
            {
                throw new System.ArgumentOutOfRangeException(nameof(radius));
            }

            boundTokenId = state.Id.Value;
            outerRadius = radius;
            gameObject.name = $"Token {state.Definition.DisplayName} {state.Id.Value}";
            transform.localPosition = HexWorldLayout.ToWorld(state.Coordinate, radius) +
                (Vector3.up * DefaultVerticalOffset);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            EnsureVisual();
            nameLabel.text = state.Definition.DisplayName.ToUpperInvariant();
        }

        /// <summary>
        /// Milestone 6.3a: shows this token's part in a pending decision, in the shared board-facing
        /// vocabulary. `Selected` outranks `Highlighted`, decided by the caller, on the same terms the
        /// minion prompt set: a piece already chosen keeps saying so while the pointer moves on.
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

            EnsureVisual();
            if (choiceCue == null)
            {
                BoardChoiceCue.CreatePair(
                    cueAnchor, "Placed Token Choice Cue", ChoiceCueLocalPosition(),
                    mirrored: true, out choiceCue, out choiceCueBackground);
                choiceCue.characterSize = ChoiceCueCharacterSize;
            }

            choiceCue.gameObject.SetActive(true);
            choiceCueBackground.gameObject.SetActive(true);
            choiceCue.text = ChoiceCueMarker;
            choiceCue.color = kind == MinionChoiceCueKind.Selected
                ? BoardChoiceCue.SelectedColor
                : BoardChoiceCue.HighlightedColor;
            BoardChoiceCue.Center(
                cueAnchor, choiceCue, choiceCueBackground, ChoiceCueLocalPosition());
        }

        /// <summary>
        /// Where the cue hangs, in the board-facing anchor's own space: up-screen from the disc, and a
        /// touch nearer the camera so it is never lost inside the piece it marks.
        /// </summary>
        private Vector3 ChoiceCueLocalPosition() =>
            new Vector3(0f, outerRadius * ChoiceCueRadiusFraction, 0.02f);

        /// <summary>
        /// Builds the disc, the name, and the anchor they share once. All are generated rather than
        /// authored, for the reason every other placeholder board object is: a token instance does not
        /// exist until an effect places one, so there is no scene object to author against.
        /// </summary>
        private void EnsureVisual()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh == null)
            {
                ownedMesh = FlatHexMeshBuilder.Create(outerRadius * DiscRadiusFraction);
                ownedMesh.name = "Placed Token";
                meshFilter.sharedMesh = ownedMesh;
            }

            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer.sharedMaterial == null)
            {
                ownedMaterial = CreateDiscMaterial();
                meshRenderer.sharedMaterial = ownedMaterial;
            }

            // Milestone 6.3a: a second anchor at the piece's own origin, carrying the same rotation
            // for the same reason. It is separate from the name's so the cue's offset is measured
            // from the token rather than from wherever the name happens to sit.
            if (cueAnchor == null)
            {
                var cueObject = new GameObject("Placed Token Cue Anchor");
                cueObject.transform.SetParent(transform, false);
                cueObject.transform.localPosition = Vector3.zero;
                cueObject.transform.localRotation = BoardChoiceCue.BoardFacingRotation;
                cueAnchor = cueObject.transform;
            }

            // Milestone 8.11a: both anchors re-take the rotation on every pass, because the direction
            // the framing camera calls up is settled when the board is framed and either may already
            // exist by then.
            cueAnchor.localRotation = BoardChoiceCue.BoardFacingRotation;

            if (nameLabel != null)
            {
                nameLabel.transform.localRotation = BoardChoiceCue.BoardFacingRotation;
                return;
            }

            // The board is the XZ plane under a top-down camera, so the name hangs from an anchor
            // carrying the shared board-facing rotation. Milestone 6.5c recorded what happens
            // otherwise: text at identity rotation on an upright transform renders edge-on as a
            // sliver, while every local-offset assertion about it still passes.
            var anchor = new GameObject("Placed Token Name");
            anchor.transform.SetParent(transform, false);
            anchor.transform.localPosition = NameLocalPosition;
            anchor.transform.localRotation = BoardChoiceCue.BoardFacingRotation;
            nameLabel = anchor.AddComponent<TextMesh>();
            nameLabel.anchor = TextAnchor.MiddleCenter;
            nameLabel.alignment = TextAlignment.Center;
            nameLabel.fontStyle = FontStyle.Bold;
            nameLabel.fontSize = 64;
            nameLabel.characterSize = NameCharacterSize;
            nameLabel.color = new Color(0.94f, 0.93f, 0.88f);
            nameLabel.GetComponent<MeshRenderer>().sortingOrder = 9;
        }

        private static Material CreateDiscMaterial()
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                throw new System.InvalidOperationException(
                    "The placed token view requires the Standard shader.");
            }

            return new Material(shader)
            {
                name = "Placed Token",
                color = new Color(0.30f, 0.24f, 0.42f, 1f)
            };
        }

        private void OnDestroy()
        {
            DestroyOwnedResource(ownedMesh);
            DestroyOwnedResource(ownedMaterial);
            ownedMesh = null;
            ownedMaterial = null;
        }

        private static void DestroyOwnedResource(Object resource)
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
                DestroyImmediate(resource);
            }
        }
    }
}
