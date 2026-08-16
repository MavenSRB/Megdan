using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// The one board-facing cue construction, shared by every unit view that has to say "this one".
    ///
    /// Milestone 6.1b reviewed these proportions on the minion Defense cue and milestone 6.1ao settled
    /// the choice marker's shape and size on top of them; milestone 6.5c moved the construction here
    /// rather than copying it, because an effect choice can name a hero as easily as a minion and two
    /// cue builders would be two sets of proportions to keep in step. The constants keep their
    /// reviewed values exactly - this is a move, not a redesign - and each view still owns where its
    /// own cue sits, since a mirrored sprite and an upright prism do not hang a marker the same way.
    /// </summary>
    public static class BoardChoiceCue
    {
        public const string Marker = "▼";
        public const float MarkerCharacterSize = 0.09f;
        public const float ValueCharacterSize = 0.06f;
        public const float BackgroundCharacterSize = 0.18f;
        public const float BackgroundVerticalScale = 1.35f;
        public const float OpticalVerticalOffset = -0.025f;

        /// <summary>The control being pointed at, in the same restrained amber every prompt uses.</summary>
        public static Color HighlightedColor => new Color(0.86f, 0.55f, 0.06f);

        /// <summary>An answer already given, or the subject an offered destination would receive.</summary>
        public static Color SelectedColor => new Color(0.72f, 0.12f, 0.08f);

        /// <summary>
        /// Builds one cue: mirrored-or-upright text over a vertically oval dark-grey backing. A caller
        /// needing a different glyph size sets it on the returned cue after construction.
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
