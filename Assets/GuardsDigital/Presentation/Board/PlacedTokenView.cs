using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
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
    /// It is deliberately smaller than a unit and sits lower, so a highlighted destination remains
    /// visible around it. The view carries no pointer selector or legality: its retained target ring
    /// only projects the typed state and consequence intent supplied by the interaction presenter.
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
        /// The generated disc's colour, kept when a token definition has no prepared face. Milestone
        /// 8.37 named it because two paths now read it: the material's first build, and the re-bind
        /// that clears a previous token's face off a pooled view.
        /// </summary>
        public static readonly Color DiscColor = new Color(0.30f, 0.24f, 0.42f, 1f);

        /// <summary>
        /// Milestone 8.35z12. The name hangs over the disc's TOP face, which
        /// <see cref="BoardPieceDepth.Standard"/> raised. Left at its old 0.01 the label would be
        /// inside the solid piece - invisible, and invisible in a way every existing local-offset
        /// assertion about it would still have passed.
        /// </summary>
        private static readonly Vector3 NameLocalPosition =
            new Vector3(0f, BoardPieceDepth.Standard + 0.01f, 0f);

        [SerializeField, HideInInspector]
        private string boundTokenId;

        [SerializeField, HideInInspector]
        private float outerRadius = HexWorldLayout.DefaultOuterRadius;

        private PlacedToken token;
        private TextMesh nameLabel;
        private BoardTargetRing targetRing;
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

        public BoardTargetRing TargetRing => targetRing;

        public BoardTargetCueState TargetCueState => targetRing == null
            ? BoardTargetCueState.None
            : targetRing.State;

        public BoardTargetConsequenceIntent TargetConsequenceIntent => targetRing == null
            ? BoardTargetConsequenceIntent.None
            : targetRing.ConsequenceIntent;

        public void Configure(PlacedToken state, float radius)
        {
            if (state == null) throw new System.ArgumentNullException(nameof(state));
            if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius))
            {
                throw new System.ArgumentOutOfRangeException(nameof(radius));
            }

            var identityChanged = token != null && !token.Id.Equals(state.Id);
            token = state;
            boundTokenId = state.Id.Value;
            outerRadius = radius;
            gameObject.name = $"Token {state.Definition.DisplayName} {state.Id.Value}";
            transform.localPosition = HexWorldLayout.ToWorld(state.Coordinate, radius) +
                (Vector3.up * DefaultVerticalOffset);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            EnsureVisual();
            ApplyFace(state);
            nameLabel.text = state.Definition.DisplayName.ToUpperInvariant();
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
        /// Projects this token's typed target state into its one retained fitted ring and translucent
        /// exact-footprint wash.
        /// Pool reuse changes identity through <see cref="Configure"/>, which clears a prior owner's
        /// state before the new projection is applied.
        /// </summary>
        public void SetTargetCue(
            BoardTargetCueState state,
            BoardTargetConsequenceIntent consequenceIntent)
        {
            EnsureTargetRing().SetCue(
                state,
                consequenceIntent,
                state == BoardTargetCueState.None ? null : GetComponent<MeshRenderer>(),
                BoardPlaneWorldY(),
                BoardTargetFootprintKind.ConvexMeshBase);
        }

        private BoardTargetRing EnsureTargetRing() =>
            targetRing != null ? targetRing : targetRing = BoardTargetRing.GetOrCreate(transform);

        private void RefreshTargetCue()
        {
            var ring = EnsureTargetRing();
            ring.SetCue(
                ring.State,
                ring.ConsequenceIntent,
                ring.State == BoardTargetCueState.None ? null : GetComponent<MeshRenderer>(),
                BoardPlaneWorldY(),
                BoardTargetFootprintKind.ConvexMeshBase);
        }

        private float BoardPlaneWorldY() => transform.parent == null
            ? transform.position.y - DefaultVerticalOffset
            : transform.parent.TransformPoint(Vector3.zero).y;

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
                // Milestone 8.35z12: solid rather than flat. The silhouette from directly
                // above is identical - see BoardPieceDepth - so the standing board is unchanged and
                // only a tilted camera can tell the difference.
                ownedMesh = FlatHexMeshBuilder.CreateSolid(outerRadius * DiscRadiusFraction);
                ownedMesh.name = "Placed Token";
                meshFilter.sharedMesh = ownedMesh;
            }

            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer.sharedMaterial == null)
            {
                ownedMaterial = CreateDiscMaterial();
                meshRenderer.sharedMaterial = ownedMaterial;
            }

            EnsureTargetRing();

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

        /// <summary>
        /// Milestone 8.37. Draws the token's own printed face when the build has one, and leaves the
        /// generated disc alone when it does not.
        ///
        /// <para>The name label stays either way. It is the only thing that distinguishes two tokens
        /// whose definitions share a physical piece - Tali's Ancestral and Venerated Totems are one
        /// double-sided token and wear one face - so removing it under art would make a board with
        /// both totems on it unreadable in exactly the case the art was added for.</para>
        ///
        /// <para>Absence is the ordinary state of a clean checkout, so nothing here throws or logs:
        /// see <see cref="PlacedTokenArtwork"/>'s own remarks. A token whose art is missing draws as
        /// the disc every token drew before this milestone.</para>
        /// </summary>
        private void ApplyFace(PlacedToken state)
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            var material = meshRenderer.sharedMaterial;
            if (material == null)
            {
                return;
            }

            if (!PlacedTokenArtwork.TryLoadTexture(state.Definition.Id, out var face))
            {
                // A pooled view may be re-bound from a token that HAD a face to one that has none,
                // and the material is retained across that, so the old face is cleared rather than
                // left standing under a different token's name.
                material.mainTexture = null;
                material.color = DiscColor;
                return;
            }

            material.mainTexture = face;
            // White, so the printed face is shown at its own colours rather than tinted by the
            // placeholder's purple.
            material.color = Color.white;
        }

        private static Material CreateDiscMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new System.InvalidOperationException(
                    "The placed token view requires the URP Lit shader.");
            }

            return new Material(shader)
            {
                name = "Placed Token",
                color = DiscColor
            };
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(targetRing, null))
            {
                targetRing.ReleaseOwnedResources();
            }

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
