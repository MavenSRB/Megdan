using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardsDigital.Presentation.Units.Minions
{
    /// <summary>
    /// Veljko's team-specific minion illustration on a thin upright panel. The existing minion
    /// sprite remains the independent flat/top-down representation and is never replaced.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class IllustratedMinionStandeeView : MonoBehaviour
    {
        private StandardMinionView owner;
        private GameObject panel;
        private Mesh mesh;
        private Material face;
        private Material edge;
        private string resource;
        private Vector3 attackLean;

        public bool IsStanding => panel != null && panel.activeSelf;
        public bool HasArtwork => panel != null;
        public Renderer PanelRenderer => panel == null ? null : panel.GetComponent<MeshRenderer>();

        public void SetAttackLean(Vector3 lean, Camera camera)
        {
            attackLean = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(lean, Vector3.up), 1f);
            Refresh(camera);
        }

        public void Configure(
            StandardMinionView minion,
            TeamSide teamSide,
            StandardMinionType minionType)
        {
            owner = minion;
            var path = StandardMinionArtCatalog.GetStandeeResourcePath(teamSide, minionType);
            if (resource == path)
            {
                return;
            }

            Release();
            resource = path;
            var texture = Resources.Load<Texture2D>(path);
            var outline = Resources.Load<TextAsset>(path);
            if (texture == null || outline == null)
            {
                owner.SetStandeeRepresentation(false);
                return;
            }

            var geometry = JsonUtility.FromJson<IllustratedStandeeView.Outline>(outline.text);
            mesh = IllustratedStandeeView.BuildPanel(geometry);
            face = new Material(Resources.Load<Material>("Rendering/StandeeCutoutTemplate"));
            face.name = "Unchanged Veljko minion standee";
            face.SetTexture("_BaseMap", texture);
            face.color = Color.white;
            edge = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            edge.name = "Dark minion standee panel edge";
            edge.color = new Color(0.065f, 0.055f, 0.045f);
            edge.SetFloat("_Cull", 0f);

            panel = new GameObject("Illustrated minion standee panel", typeof(MeshFilter), typeof(MeshRenderer));
            panel.transform.SetParent(transform, false);
            panel.transform.localPosition = Vector3.up * (BoardPieceDepth.Standard / StandardMinionView.DefaultScale);
            panel.transform.localScale = Vector3.one / StandardMinionView.DefaultScale;
            panel.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = panel.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { face, edge };
            renderer.shadowCastingMode = ShadowCastingMode.On;
            panel.SetActive(false);
            owner.SetStandeeRepresentation(false);
        }

        public void SetTint(Color color)
        {
            if (face != null)
            {
                face.color = color;
            }
        }

        public void Refresh(Camera camera)
        {
            if (owner == null || panel == null || camera == null)
            {
                return;
            }

            var elevation = Mathf.Asin(Mathf.Clamp(-camera.transform.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            var standing = IsStanding
                ? elevation < IllustratedStandeeView.TokenAngle
                : elevation < IllustratedStandeeView.StandeeAngle;
            standing &= owner.HasTokenVisual;
            if (standing != IsStanding)
            {
                panel.SetActive(standing);
                owner.SetStandeeRepresentation(standing);
            }

            if (!standing)
            {
                return;
            }

            var facing = Vector3.ProjectOnPlane(-camera.transform.forward, Vector3.up);
            if (facing.sqrMagnitude > 0.0001f)
            {
                panel.transform.rotation = IllustratedStandeeView.AttackFacing(facing, attackLean);
            }
        }

        private void LateUpdate() => Refresh(Camera.main);

        private void Release()
        {
            if (panel != null)
            {
                panel.SetActive(false);
                DestroyResource(panel);
            }
            DestroyResource(mesh);
            DestroyResource(face);
            DestroyResource(edge);
            panel = null;
            mesh = null;
            face = null;
            edge = null;
        }

        private static void DestroyResource(Object value)
        {
            if (value == null) return;
            if (UnityEngine.Application.isPlaying) Destroy(value); else DestroyImmediate(value);
        }

        private void OnDestroy() => Release();
    }
}
