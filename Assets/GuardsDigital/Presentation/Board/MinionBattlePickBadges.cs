using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// Milestone 9.29. The small tokens of the teammates who picked a Minion Battle candidate, drawn on
    /// the piece they picked.
    /// </summary>
    /// <remarks>
    /// <para><b>From the user:</b> "you need to see what other people picked as well", with the
    /// teammate's small portrait on the minions they picked. The full hex outline already says that
    /// SOMEONE picked the piece; this says WHO, so a player can tell their own pick from a teammate's
    /// and knows which one to change to. The viewer's own pick carries no token.</para>
    /// <para><b>One badge per teammate, along the top of the piece.</b> Each badge is the teammate's
    /// board token face - the same picture their own piece wears - inside a thin black outline,
    /// 9.18's outline on every token. They sit on an arc of the piece's upper edge centred on the
    /// framing camera's screen-up, spaced so two never touch.</para>
    /// <para><b>World-aligned under the piece.</b> A minion's own transform faces the camera and is
    /// scaled; this object undoes both on every <see cref="Show"/>, so its local plane is the board
    /// plane in world units, and it still moves with the piece when the kinetic layer slides it.</para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MinionBattlePickBadges : MonoBehaviour
    {
        public const string BadgesObjectName = "Minion Battle Pick Badges";
        public const int Segments = 32;

        /// <summary>A badge's radius as a fraction of the piece's.</summary>
        public const float BadgeRadiusRatio = 0.36f;

        /// <summary>How far from the piece's centre a badge's centre sits, as a fraction of its radius.</summary>
        public const float BadgeCentreRatio = 0.8f;

        /// <summary>The black outline's width as a fraction of the badge radius.</summary>
        public const float OutlineWidthRatio = 0.16f;

        /// <summary>Above the minion token, so the badge is never under it.</summary>
        public const float Lift = 0.04f;

        /// <summary>One teammate's badge: their token face, or their team colour where no face ships.</summary>
        public readonly struct Picker
        {
            public Picker(Texture2D face, Color fallback)
            {
                Face = face;
                Fallback = fallback;
            }

            public Texture2D Face { get; }
            public Color Fallback { get; }
        }

        private readonly List<GameObject> badges = new List<GameObject>();
        private readonly List<UnityEngine.Object> owned = new List<UnityEngine.Object>();
        private Material outlineMaterial;
        private string builtSignature;

        /// <summary>How many badges are drawn now.</summary>
        public int VisibleCount { get; private set; }

        /// <summary>The faces drawn now, in badge order, for a fixture to compare.</summary>
        public IReadOnlyList<Texture2D> VisibleFaces { get; private set; } = Array.Empty<Texture2D>();

        public static MinionBattlePickBadges GetOrCreate(Transform piece)
        {
            if (piece == null) throw new ArgumentNullException(nameof(piece));
            for (var index = 0; index < piece.childCount; index++)
            {
                var existing = piece.GetChild(index).GetComponent<MinionBattlePickBadges>();
                if (existing != null) return existing;
            }

            var badgesObject = new GameObject(BadgesObjectName);
            badgesObject.transform.SetParent(piece, false);
            return badgesObject.AddComponent<MinionBattlePickBadges>();
        }

        /// <summary>
        /// Draws one badge per picker on a piece whose visible radius on the board is
        /// <paramref name="pieceRadius"/> world units, or hides them all for an empty list.
        /// </summary>
        public void Show(IReadOnlyList<Picker> pickers, float pieceRadius)
        {
            if (pickers == null || pickers.Count == 0)
            {
                Hide();
                return;
            }

            if (pieceRadius <= 0f || float.IsNaN(pieceRadius) || float.IsInfinity(pieceRadius))
            {
                throw new ArgumentOutOfRangeException(nameof(pieceRadius));
            }

            AlignToTheBoard();
            var signature = Signature(pickers, pieceRadius);
            if (!string.Equals(signature, builtSignature, StringComparison.Ordinal))
            {
                Rebuild(pickers, pieceRadius);
                builtSignature = signature;
            }

            foreach (var badge in badges) badge.SetActive(true);
            VisibleCount = pickers.Count;
        }

        public void Hide()
        {
            foreach (var badge in badges) badge.SetActive(false);
            VisibleCount = 0;
            VisibleFaces = Array.Empty<Texture2D>();
            builtSignature = null;
        }

        private void AlignToTheBoard()
        {
            var piece = transform.parent;
            var board = piece == null ? null : piece.parent;
            transform.rotation = board == null ? Quaternion.identity : board.rotation;
            var scale = piece == null ? 1f : piece.lossyScale.x;
            var inverse = Mathf.Approximately(scale, 0f) ? 1f : 1f / scale;
            transform.localScale = new Vector3(inverse, inverse, inverse);
            var at = piece == null ? Vector3.zero : piece.position;
            transform.position = at + (Vector3.up * Lift);
        }

        private void Rebuild(IReadOnlyList<Picker> pickers, float pieceRadius)
        {
            ReleaseBadges();
            if (outlineMaterial == null)
            {
                outlineMaterial = NewUnlitMaterial(BadgesObjectName + " Outline");
                outlineMaterial.color = Color.black;
            }

            var screenUp = BoardChoiceCue.ScreenUpInWorld;
            var up = transform.parent == null
                ? screenUp
                : transform.InverseTransformDirection(screenUp);
            up = new Vector3(up.x, 0f, up.z);
            up = up.sqrMagnitude < 1e-6f ? Vector3.forward : up.normalized;
            var baseAngle = Mathf.Atan2(up.z, up.x);
            var badgeRadius = pieceRadius * BadgeRadiusRatio;
            var centreDistance = pieceRadius * BadgeCentreRatio;
            var step = (badgeRadius * 2.3f) / centreDistance;
            var faces = new Texture2D[pickers.Count];
            for (var index = 0; index < pickers.Count; index++)
            {
                // Laid right to left across screen-up, so the first teammate stands on the right.
                var angle = baseAngle - ((index - ((pickers.Count - 1) * 0.5f)) * step);
                var centre = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * centreDistance;
                badges.Add(BuildBadge(index, centre, badgeRadius, pickers[index]));
                faces[index] = pickers[index].Face;
            }

            VisibleFaces = Array.AsReadOnly(faces);
        }

        private GameObject BuildBadge(int index, Vector3 centre, float radius, Picker picker)
        {
            var badge = new GameObject(BadgesObjectName + " " + index);
            badge.transform.SetParent(transform, false);
            badge.transform.localPosition = centre;
            var filter = badge.AddComponent<MeshFilter>();
            var renderer = badge.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var mesh = BuildDiscWithOutline(radius);
            owned.Add(mesh);
            filter.sharedMesh = mesh;
            var face = NewUnlitMaterial(BadgesObjectName + " Face " + index);
            if (picker.Face != null)
            {
                face.mainTexture = picker.Face;
                face.color = Color.white;
            }
            else
            {
                face.color = picker.Fallback;
            }

            owned.Add(face);
            renderer.sharedMaterials = new[] { outlineMaterial, face };
            return badge;
        }

        /// <summary>
        /// A face disc mapped onto the token's inscribed circle, as the hero token maps it, and a black
        /// annulus around it - two submeshes, one renderer. Wound so each triangle faces +Y, the
        /// framing camera's side.
        /// </summary>
        private static Mesh BuildDiscWithOutline(float radius)
        {
            var faceRadius = radius * (1f - OutlineWidthRatio);
            var vertices = new Vector3[(Segments * 2) + 1];
            var uv = new Vector2[vertices.Length];
            vertices[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0.5f);
            for (var segment = 0; segment < Segments; segment++)
            {
                var angle = Mathf.PI * 2f * segment / Segments;
                var x = Mathf.Cos(angle);
                var z = Mathf.Sin(angle);
                vertices[1 + segment] = new Vector3(x * faceRadius, 0.002f, z * faceRadius);
                uv[1 + segment] = new Vector2(0.5f + (0.49f * x), 0.5f + (0.49f * z));
                vertices[1 + Segments + segment] = new Vector3(x * radius, 0f, z * radius);
                uv[1 + Segments + segment] = new Vector2(0.5f, 0.5f);
            }

            vertices[0] = new Vector3(0f, 0.002f, 0f);
            var outline = new int[Segments * 6];
            var face = new int[Segments * 3];
            for (var segment = 0; segment < Segments; segment++)
            {
                var next = (segment + 1) % Segments;
                face[(segment * 3) + 0] = 0;
                face[(segment * 3) + 1] = 1 + next;
                face[(segment * 3) + 2] = 1 + segment;
                outline[(segment * 6) + 0] = 1 + segment;
                outline[(segment * 6) + 1] = 1 + next;
                outline[(segment * 6) + 2] = 1 + Segments + next;
                outline[(segment * 6) + 3] = 1 + segment;
                outline[(segment * 6) + 4] = 1 + Segments + next;
                outline[(segment * 6) + 5] = 1 + Segments + segment;
            }

            var mesh = new Mesh { name = BadgesObjectName, vertices = vertices, uv = uv, subMeshCount = 2 };
            mesh.SetTriangles(outline, 0);
            mesh.SetTriangles(face, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Material NewUnlitMaterial(string name)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                throw new InvalidOperationException("The Minion Battle pick badge requires the URP Unlit shader.");
            }

            return new Material(shader) { name = name };
        }

        private static string Signature(IReadOnlyList<Picker> pickers, float pieceRadius)
        {
            var parts = new string[pickers.Count + 1];
            parts[0] = pieceRadius.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            for (var index = 0; index < pickers.Count; index++)
            {
                var face = pickers[index].Face;
                parts[index + 1] = face != null
                    ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(face).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : pickers[index].Fallback.ToString();
            }

            return string.Join("|", parts);
        }

        private void ReleaseBadges()
        {
            foreach (var badge in badges) DestroyOwned(badge);
            badges.Clear();
            foreach (var resource in owned) DestroyOwned(resource);
            owned.Clear();
        }

        public void ReleaseOwnedResources()
        {
            ReleaseBadges();
            DestroyOwned(outlineMaterial);
            outlineMaterial = null;
            builtSignature = null;
            VisibleCount = 0;
            VisibleFaces = Array.Empty<Texture2D>();
        }

        private static void DestroyOwned(UnityEngine.Object resource)
        {
            if (resource == null) return;
            if (UnityEngine.Application.isPlaying) UnityEngine.Object.Destroy(resource);
            else UnityEngine.Object.DestroyImmediate(resource);
        }

        private void OnDestroy() => ReleaseOwnedResources();
    }
}
