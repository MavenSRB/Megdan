using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using UnityEngine;

namespace GuardsDigital.Presentation.Units.Heroes
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class PrimitiveHeroView : MonoBehaviour
    {
        public const string DefaultHeroId = "primitive-hero";
        /// <summary>
        /// Milestone 8.11a, at the user's request: raised from 0.32 for the reason the minion scale was.
        /// `PrimitiveHeroPointerSelector` builds its capsule from this, so the pointer target grows with
        /// the piece and picking stays exactly what a player sees.
        /// </summary>
        public const float DefaultBaseRadius = 0.40f;
        public const float DefaultHeight = 0.5f;

        /// <summary>
        /// How far above the tile plane the piece stands, in world units.
        ///
        /// Milestone 8.11b: the flat token is drawn at this height too, and it is the one number that
        /// decides either piece's clearance - above the tiles at y = 0 and their debug edges at
        /// y = 0.015, and well under the minion tokens at 0.18, not that a hero and a minion can share
        /// a space.
        /// </summary>
        public const float DefaultVerticalOffset = 0.03f;

        private const int RedSideCount = 16;
        private const int BlueSideCount = 4;

        [SerializeField]
        private string heroId = DefaultHeroId;

        [SerializeField]
        private TeamSide teamSide = TeamSide.Red;

        [SerializeField]
        private string spawnPointId;

        [SerializeField]
        private string heroContentId;

        [SerializeField]
        private string heroDisplayName;

        [SerializeField]
        private int q;

        [SerializeField]
        private int r;

        [SerializeField]
        private float outerRadius = HexWorldLayout.DefaultOuterRadius;

        [SerializeField]
        private float baseRadius = DefaultBaseRadius;

        [SerializeField]
        private float height = DefaultHeight;

        [SerializeField]
        private float verticalOffset = DefaultVerticalOffset;

        private Mesh ownedMesh;
        private Material ownedMaterial;
        private Material ownedRimMaterial;
        private BoardTargetRing targetRing;

        public HeroOccupancy Occupancy => new HeroOccupancy(new HeroId(heroId), Coordinate);

        public TeamSide TeamSide => teamSide;

        public HeroContentId? HeroContentId => string.IsNullOrWhiteSpace(heroContentId)
            ? (HeroContentId?)null
            : new HeroContentId(heroContentId);

        public string HeroDisplayName => heroDisplayName;

        public SpawnPointId? ConfiguredSpawnPointId => string.IsNullOrWhiteSpace(spawnPointId)
            ? (SpawnPointId?)null
            : new SpawnPointId(spawnPointId);

        public HexCoordinate Coordinate => new HexCoordinate(q, r);

        public float OuterRadius => outerRadius;

        public float VerticalOffset => verticalOffset;

        /// <summary>
        /// Milestone 8.11b: whether this hero is currently drawn as the official token rather than as
        /// the generated prism. False is the tracked project's ordinary state - the artwork is private
        /// and ignored, so a clean checkout resolves none and every hero keeps its prism.
        /// </summary>
        public bool HasTokenVisual { get; private set; }

        /// <summary>
        /// The radius the pointer target is built at, which is the radius of the piece a player can
        /// actually see.
        ///
        /// Milestone 8.11b, on milestone 8.11a's reasoning for raising <see cref="DefaultBaseRadius"/>:
        /// the pointer target grows with the piece so picking stays exactly what a player sees. The
        /// token covers its space the way a minion's does and is wider than the prism it replaces, so
        /// the two states answer with two radii rather than the collider being left at the smaller one.
        /// </summary>
        public float SelectionRadius => HasTokenVisual ? HeroTokenArtwork.TokenOuterRadius : baseRadius;

        public BoardTargetRing TargetRing => targetRing;

        public BoardTargetCueState TargetCueState => targetRing == null
            ? BoardTargetCueState.None
            : targetRing.State;

        public BoardTargetConsequenceIntent TargetConsequenceIntent => targetRing == null
            ? BoardTargetConsequenceIntent.None
            : targetRing.ConsequenceIntent;

        public void Configure(HeroOccupancy occupancy, float radius)
        {
            var identityChanged = !string.IsNullOrWhiteSpace(heroId) &&
                !string.Equals(heroId, occupancy.HeroId.Value, System.StringComparison.Ordinal);
            heroId = occupancy.HeroId.Value;
            q = occupancy.Coordinate.Q;
            r = occupancy.Coordinate.R;
            outerRadius = radius;
            ApplyLayout();
            if (identityChanged)
            {
                SetTargetCue(BoardTargetCueState.None, BoardTargetConsequenceIntent.None);
            }
            else
            {
                RefreshTargetCueIfCreated();
            }
        }

        public void ConfigureFromSetup(QuickGameHeroSetupState setupState, BoardMapDefinition definition, float radius)
        {
            if (setupState == null) throw new System.ArgumentNullException(nameof(setupState));
            if (definition == null) throw new System.ArgumentNullException(nameof(definition));
            if (!setupState.HeroId.Equals(Occupancy.HeroId))
            {
                throw new System.InvalidOperationException(
                    $"Setup hero '{setupState.HeroId}' does not match view hero '{Occupancy.HeroId}'.");
            }

            var spawn = definition.SpawnPoints.SingleOrDefault(value =>
                value.Id.Equals(setupState.SpawnPoint.SpawnPointId));
            var space = definition.Spaces.SingleOrDefault(value =>
                value.Id.Equals(setupState.OccupiedSpace.SpaceId));
            if (!setupState.SpawnPoint.MapId.Equals(definition.Id) ||
                !setupState.OccupiedSpace.MapId.Equals(definition.Id) ||
                spawn == null || space == null || !spawn.SpaceId.Equals(space.Id) ||
                !space.Coordinate.Equals(setupState.Coordinate))
            {
                throw new System.InvalidOperationException("Hero setup placement is not valid for the rendered map.");
            }

            teamSide = setupState.TeamSide;
            spawnPointId = setupState.SpawnPoint.SpawnPointId.Value;
            heroContentId = setupState.ContentId.Value;
            heroDisplayName = setupState.DisplayName;
            Configure(new HeroOccupancy(setupState.HeroId, setupState.Coordinate), radius);
            gameObject.name = $"{teamSide} {heroDisplayName} ({setupState.HeroId.Value})";
            // Milestone 8.11b: the content identity and the team this view was just given are exactly
            // what selects a token face, so the visual is settled here rather than left at whatever
            // Awake could build before the setup was known.
            EnsureVisual();
        }

        public bool PlaceAtConfiguredSpawnPoint(BoardMapDefinition definition)
        {
            if (definition == null) throw new System.ArgumentNullException(nameof(definition));
            if (!ConfiguredSpawnPointId.HasValue) return false;

            var selectedSpawnPoint = definition.SpawnPoints.SingleOrDefault(spawnPoint =>
                spawnPoint.Id.Equals(ConfiguredSpawnPointId.Value));
            if (selectedSpawnPoint == null)
            {
                throw new System.InvalidOperationException(
                    $"Configured spawn point '{ConfiguredSpawnPointId.Value}' is unavailable on map '{definition.Reference}'.");
            }

            var expectedTeam = teamSide == TeamSide.Red ? "red" : "blue";
            if (selectedSpawnPoint.TeamId != expectedTeam || selectedSpawnPoint.UnitType != "hero")
            {
                throw new System.InvalidOperationException(
                    $"Spawn point '{selectedSpawnPoint.Id}' is not a {expectedTeam} hero spawn point.");
            }

            var selectedSpace = definition.Spaces.Single(space => space.Id.Equals(selectedSpawnPoint.SpaceId));
            Configure(new HeroOccupancy(Occupancy.HeroId, selectedSpace.Coordinate), outerRadius);
            return true;
        }

        public void ApplyLayout()
        {
            transform.localPosition = HexWorldLayout.ToWorld(Coordinate, outerRadius) + (Vector3.up * verticalOffset);
        }

        /// <summary>
        /// Projects this hero's typed target state into its one retained fitted ring and translucent
        /// exact-footprint wash.
        /// The cue follows either generated prism or official token without changing the hero's
        /// collider, stable identity, or command path.
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
                TargetFootprintKind());
        }

        private BoardTargetRing EnsureTargetRing() =>
            targetRing != null ? targetRing : targetRing = BoardTargetRing.GetOrCreate(transform);

        private void RefreshTargetCueIfCreated()
        {
            if (targetRing == null)
            {
                return;
            }

            targetRing.SetCue(
                targetRing.State,
                targetRing.ConsequenceIntent,
                targetRing.State == BoardTargetCueState.None ? null : GetComponent<MeshRenderer>(),
                BoardPlaneWorldY(),
                TargetFootprintKind());
        }

        private BoardTargetFootprintKind TargetFootprintKind() => HasTokenVisual
            ? BoardTargetFootprintKind.Circular
            : BoardTargetFootprintKind.ConvexMeshBase;

        private float BoardPlaneWorldY() => transform.parent == null
            ? transform.position.y - verticalOffset
            : transform.parent.TransformPoint(Vector3.zero).y;

        /// <summary>
        /// Draws this hero as the official token when a prepared face is present, and as the generated
        /// prism when there is none.
        ///
        /// <para>Milestone 8.11b. The two are alternatives on one pair of components rather than two
        /// views: the mesh and the materials on this object's own <see cref="MeshFilter"/> and
        /// <see cref="MeshRenderer"/> are replaced, and nothing else about the object changes - not its
        /// transform, not its collider's owner, not the <see cref="HeroId"/> everything binds to. That
        /// is why selection, the target ring, movement, Defeat and respawn visibility, Undo, restore
        /// and replay are untouched by the swap, and why an absent face is an ordinary state rather
        /// than a missing reference.</para>
        /// </summary>
        public void EnsureVisual()
        {
            if (!TryApplyTokenVisual())
            {
                ApplyPrimitiveVisual();
            }

            RefreshTargetCueIfCreated();
        }

        public void EnsurePrimitiveVisual()
        {
            ApplyPrimitiveVisual();
            RefreshTargetCueIfCreated();
        }

        /// <summary>
        /// Milestone 8.11b. Replaces the visible mesh and materials with the official token: this
        /// hero's own face, selected by projected content identity and authoritative team, inside a rim
        /// in the project's own team colour.
        ///
        /// <para>The rim is not decoration. The source set rings its two faces orange and light blue,
        /// which are its team colours rather than this project's, and a player should not have to know
        /// that orange means Red - so the piece states the team a second time in
        /// <see cref="HeroTokenArtwork.TeamRim"/>, the same colour the hero's HUD rail is edged in, read
        /// from <see cref="TeamSide"/> rather than from the picture.</para>
        /// </summary>
        private bool TryApplyTokenVisual()
        {
            var contentId = HeroContentId;
            if (!contentId.HasValue ||
                !HeroTokenArtwork.TryLoadTexture(contentId.Value, teamSide, out var face))
            {
                return false;
            }

            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            ReplaceOwnedMesh(CreateTokenMesh());
            meshFilter.sharedMesh = ownedMesh;

            // The rim is lit exactly as the prism it replaces was, so this project's team colour reads
            // on the board as the colour a player has been reading since milestone 4.4. The face is
            // unlit for milestone 8.11a's reason: it is a picture of a printed token, and what it must
            // show is its own pixels.
            ReplaceOwnedMaterial(ref ownedRimMaterial, CreateTeamRimMaterial(teamSide));
            ReplaceOwnedMaterial(ref ownedMaterial, CreateTokenFaceMaterial(teamSide, face));
            meshRenderer.sharedMaterials = new[] { ownedRimMaterial, ownedMaterial };
            meshRenderer.enabled = true;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            HasTokenVisual = true;
            return true;
        }

        private void ApplyPrimitiveVisual()
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }

            // Milestone 8.11b: a mesh this view generated for a token is replaced rather than kept,
            // because the two shapes are alternatives. A mesh authored in the scene is still left
            // alone, which is what the null check has always been for.
            if (meshFilter.sharedMesh == null || HasTokenVisual)
            {
                ReplaceOwnedMesh(CreatePrismMesh(baseRadius, height, GetSideCount()));
                meshFilter.sharedMesh = ownedMesh;
            }

            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            if (meshRenderer.sharedMaterial == null || HasTokenVisual)
            {
                ReplaceOwnedMaterial(ref ownedRimMaterial, null);
                ReplaceOwnedMaterial(ref ownedMaterial, CreatePlaceholderMaterial(teamSide));
                meshRenderer.sharedMaterials = new[] { ownedMaterial };
            }

            HasTokenVisual = false;
        }

        private void ReplaceOwnedMesh(Mesh replacement)
        {
            DestroyOwnedResource(ownedMesh);
            ownedMesh = replacement;
        }

        private static void ReplaceOwnedMaterial(ref Material owned, Material replacement)
        {
            DestroyOwnedResource(owned);
            owned = replacement;
        }

        /// <summary>
        /// The flat piece: the official face as a disc, and the team rim as the annulus around it, in
        /// two submeshes so one renderer carries both without a second object to keep in step.
        ///
        /// The face's texture coordinates map the disc onto the prepared token's own inscribed circle,
        /// inset by <see cref="HeroTokenArtwork.FaceInset"/>. The mesh is a disc rather than a quad for
        /// the same reason: the prepared face is a circle with cleared corners, the shader that draws
        /// it is opaque, and a quad would render those corners as black.
        /// </summary>
        private static Mesh CreateTokenMesh()
        {
            var segments = HeroTokenArtwork.Segments;
            var faceRadius = HeroTokenArtwork.FaceRadius;
            var rimRadius = HeroTokenArtwork.TokenOuterRadius;
            var vertices = new Vector3[(segments * 2) + 1];
            var uv = new Vector2[vertices.Length];
            var normals = new Vector3[vertices.Length];
            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (var segment = 0; segment < segments; segment++)
            {
                var angle = (Mathf.PI * 2f / segments) * segment;
                var x = Mathf.Cos(angle);
                var z = Mathf.Sin(angle);
                vertices[1 + segment] = new Vector3(x * faceRadius, 0f, z * faceRadius);
                uv[1 + segment] = new Vector2(
                    0.5f + (0.5f * HeroTokenArtwork.FaceInset * x),
                    0.5f + (0.5f * HeroTokenArtwork.FaceInset * z));
                vertices[1 + segments + segment] = new Vector3(x * rimRadius, 0f, z * rimRadius);
                uv[1 + segments + segment] = new Vector2(0.5f, 0.5f);
            }

            for (var index = 0; index < normals.Length; index++)
            {
                normals[index] = Vector3.up;
            }

            var rim = new int[segments * 6];
            var faceTriangles = new int[segments * 3];
            for (var segment = 0; segment < segments; segment++)
            {
                var next = (segment + 1) % segments;
                var inner = 1 + segment;
                var nextInner = 1 + next;
                var outer = 1 + segments + segment;
                var nextOuter = 1 + segments + next;

                // Wound so every triangle's normal is +Y, which is the way the framing camera looks.
                faceTriangles[(segment * 3) + 0] = 0;
                faceTriangles[(segment * 3) + 1] = nextInner;
                faceTriangles[(segment * 3) + 2] = inner;

                rim[(segment * 6) + 0] = inner;
                rim[(segment * 6) + 1] = nextInner;
                rim[(segment * 6) + 2] = nextOuter;
                rim[(segment * 6) + 3] = inner;
                rim[(segment * 6) + 4] = nextOuter;
                rim[(segment * 6) + 5] = outer;
            }

            var mesh = new Mesh
            {
                name = "Official Hero Token",
                vertices = vertices,
                uv = uv,
                normals = normals,
                subMeshCount = 2
            };
            mesh.SetTriangles(rim, 0);
            mesh.SetTriangles(faceTriangles, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        private int GetSideCount()
        {
            return teamSide == TeamSide.Blue ? BlueSideCount : RedSideCount;
        }

        private static Mesh CreatePrismMesh(float radius, float cylinderHeight, int sideCount)
        {
            var vertices = new Vector3[(sideCount * 2) + 2];
            var triangles = new int[sideCount * 12];
            var bottomCenterIndex = sideCount * 2;
            var topCenterIndex = bottomCenterIndex + 1;

            vertices[bottomCenterIndex] = Vector3.zero;
            vertices[topCenterIndex] = Vector3.up * cylinderHeight;

            for (var side = 0; side < sideCount; side++)
            {
                var angle = Mathf.Deg2Rad * ((360f / sideCount) * side);
                var x = Mathf.Cos(angle) * radius;
                var z = Mathf.Sin(angle) * radius;
                vertices[side] = new Vector3(x, 0f, z);
                vertices[side + sideCount] = new Vector3(x, cylinderHeight, z);
            }

            var triangleIndex = 0;
            for (var side = 0; side < sideCount; side++)
            {
                var next = (side + 1) % sideCount;
                var top = side + sideCount;
                var nextTop = next + sideCount;

                triangles[triangleIndex++] = side;
                triangles[triangleIndex++] = top;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = top;
                triangles[triangleIndex++] = nextTop;

                triangles[triangleIndex++] = topCenterIndex;
                triangles[triangleIndex++] = nextTop;
                triangles[triangleIndex++] = top;

                triangles[triangleIndex++] = bottomCenterIndex;
                triangles[triangleIndex++] = side;
                triangles[triangleIndex++] = next;
            }

            var mesh = new Mesh
            {
                name = $"Primitive {sideCount}-Sided Hero Placeholder",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material CreatePlaceholderMaterial(TeamSide side)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                throw new System.InvalidOperationException(
                    "The primitive hero placeholder requires the Standard shader.");
            }

            var material = new Material(shader)
            {
                name = $"{side} Primitive Hero Placeholder",
                // Milestone 8.11b: read from the one statement of this project's board team colours,
                // so the prism and the token's rim cannot drift into two different Reds.
                color = HeroTokenArtwork.TeamRim(side)
            };
            return material;
        }

        /// <summary>
        /// Milestone 8.11b: the token's rim, in this project's own team colour and lit exactly as the
        /// prism it replaces is, so the board's Red reads as the same Red it did before the swap.
        /// </summary>
        private static Material CreateTeamRimMaterial(TeamSide side)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                throw new System.InvalidOperationException(
                    "The hero token rim requires the Standard shader.");
            }

            return new Material(shader)
            {
                name = $"{side} Hero Token Rim",
                color = HeroTokenArtwork.TeamRim(side)
            };
        }

        /// <summary>
        /// Milestone 8.11b: the official face, unlit for milestone 8.11a's reason - it is a picture of
        /// a printed token, and what it must show is its own pixels rather than this scene's ambient.
        /// <c>Unlit/Texture</c> is declared in Graphics settings, which is what
        /// <c>RuntimeShaderResolutionTests</c> requires of a shader resolved by name.
        /// </summary>
        private static Material CreateTokenFaceMaterial(TeamSide side, Texture2D face)
        {
            var shader = Shader.Find("Unlit/Texture");
            if (shader == null)
            {
                throw new System.InvalidOperationException(
                    "The hero token face requires the Unlit/Texture shader.");
            }

            return new Material(shader)
            {
                name = $"{side} Hero Token Face",
                mainTexture = face
            };
        }

        private void Awake()
        {
            ApplyLayout();
            EnsureVisual();
            EnsureTargetRing();
        }

        private void OnDestroy()
        {
            if (!ReferenceEquals(targetRing, null))
            {
                targetRing.ReleaseOwnedResources();
            }

            DestroyOwnedResource(ownedMesh);
            DestroyOwnedResource(ownedMaterial);
            DestroyOwnedResource(ownedRimMaterial);
            ownedMesh = null;
            ownedMaterial = null;
            ownedRimMaterial = null;
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

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (outerRadius > 0f && !float.IsNaN(outerRadius) && !float.IsInfinity(outerRadius))
            {
                ApplyLayout();
            }
        }
#endif
    }
}
