using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// The shared board-facing text construction used by signed Defense contribution badges and
    /// board labels.
    ///
    /// Milestone 6.1b reviewed these proportions on the minion Defense cue. Milestone 8.11d moved
    /// piece targeting to generated fitted rings with a translucent over-piece wash, while this helper
    /// retains the signed-value treatment and the one camera-relative board orientation used by flat
    /// text and target-cue geometry.
    /// </summary>
    public static class BoardChoiceCue
    {
        /// <summary>
        /// Which world direction is up on screen, which every board-facing cue is turned to match.
        ///
        /// <para>Milestone 8.11a. Until the official artwork went underneath, screen up was world +Z and
        /// nothing had to say so. The framing camera now stands on the board's own axes so the square
        /// face reads upright, and a cue still turned to world +Z is drawn at that same angle to the
        /// screen - readable, but visibly crooked, which is what the user saw. The map view sets this
        /// once when it frames the board, and it is the only mutable state in the cue construction:
        /// there is one board, one camera, and one up.</para>
        ///
        /// <para>It defaults to world +Z, so a build with no artwork - which is a clean checkout - draws
        /// every cue exactly where it drew it before this milestone.</para>
        /// </summary>
        public static Vector3 ScreenUpInWorld { get; private set; } = Vector3.forward;

        /// <summary>
        /// Points the cue construction at the direction the framing camera treats as up. Called by the
        /// map view as it frames the board, because that is where the camera's orientation is decided.
        /// </summary>
        public static void SetScreenUp(Vector3 screenUpInWorld)
        {
            var flattened = new Vector3(screenUpInWorld.x, 0f, screenUpInWorld.z);
            ScreenUpInWorld = flattened.sqrMagnitude < 1e-6f ? Vector3.forward : flattened.normalized;
        }

        /// <summary>
        /// The orientation that lays a cue flat on the board, facing the camera that looks down the
        /// XZ plane, with its own up pointing up-screen. `StandardMinionView` already carries it on
        /// the token itself; a view whose transform is upright hangs its cue from an anchor holding
        /// this, because a cue at identity rotation on such a view is edge-on and invisible.
        /// </summary>
        public static Quaternion BoardFacingRotation =>
            Quaternion.FromToRotation(Vector3.forward, ScreenUpInWorld) *
            Quaternion.Euler(-90f, 0f, 0f) * Quaternion.AngleAxis(180f, Vector3.forward);

        public const float ValueCharacterSize = 0.06f;
        public const float BackgroundCharacterSize = 0.18f;
        public const float BackgroundVerticalScale = 1.35f;
        public const float OpticalVerticalOffset = -0.025f;

        /// <summary>
        /// Builds one signed-value badge: mirrored-or-upright text over a vertically oval dark-grey
        /// backing.
        /// </summary>
        public static void CreatePair(
            Transform parent,
            string cueName,
            Vector3 localPosition,
            bool mirrored,
            out TextMesh cue,
            out TextMesh backing)
        {
            if (parent == null)
            {
                throw new System.ArgumentNullException(nameof(parent));
            }

            var cueObject = new GameObject(cueName);
            cueObject.transform.SetParent(parent, false);
            cueObject.transform.localPosition = localPosition;
            cueObject.transform.localRotation = Quaternion.identity;
            cueObject.transform.localScale = new Vector3(mirrored ? -1f : 1f, 1f, 1f);
            var backgroundObject = new GameObject($"{cueName} Background");
            backgroundObject.transform.SetParent(parent, false);
            backgroundObject.transform.localPosition = localPosition;
            backgroundObject.transform.localRotation = Quaternion.identity;
            backgroundObject.transform.localScale = new Vector3(1f, BackgroundVerticalScale, 1f);
            backing = backgroundObject.AddComponent<TextMesh>();
            backing.anchor = TextAnchor.MiddleCenter;
            backing.alignment = TextAlignment.Center;
            backing.fontStyle = FontStyle.Bold;
            backing.fontSize = 64;
            backing.characterSize = BackgroundCharacterSize;
            backing.text = "●";
            backing.color = new Color(0.16f, 0.17f, 0.18f, 0.9f);
            backing.GetComponent<MeshRenderer>().sortingOrder = 19;
            cue = cueObject.AddComponent<TextMesh>();
            cue.anchor = TextAnchor.MiddleCenter;
            cue.alignment = TextAlignment.Center;
            cue.fontStyle = FontStyle.Bold;
            cue.fontSize = 64;
            cue.characterSize = ValueCharacterSize;
            cue.GetComponent<MeshRenderer>().sortingOrder = 20;
        }

        /// <summary>
        /// Re-centres the glyph inside its backing from current renderer bounds, then applies the
        /// reviewed downward optical offset that corrects the font's visually high placement.
        /// </summary>
        public static void Center(
            Transform parent, TextMesh cue, TextMesh backing, Vector3 localPosition)
        {
            if (parent == null) throw new System.ArgumentNullException(nameof(parent));
            if (cue == null) throw new System.ArgumentNullException(nameof(cue));
            if (backing == null) throw new System.ArgumentNullException(nameof(backing));

            cue.transform.localPosition = localPosition;
            var cueCenter = cue.GetComponent<MeshRenderer>().bounds.center;
            var backgroundCenter = backing.GetComponent<MeshRenderer>().bounds.center;
            var localOffset = parent.InverseTransformVector(backgroundCenter - cueCenter);
            cue.transform.localPosition += new Vector3(
                localOffset.x,
                localOffset.y + OpticalVerticalOffset,
                0f);
        }
    }
}
