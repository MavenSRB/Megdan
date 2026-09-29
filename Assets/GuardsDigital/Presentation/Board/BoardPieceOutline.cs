using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// Milestone 9.16. The thin black line drawn around a board piece's printed figure so it pops off
    /// the painted board - the user's call on the Turret captures, "with a thin black outline".
    ///
    /// <para><b>One width, stated in the board's own units.</b> It is a share of the hex's outer
    /// radius rather than a pixel count, so every piece that wears it draws the SAME line at any
    /// camera distance: about one and a half pixels at the default match frame and a clear rule in a
    /// close crop. A piece whose figure is a cut texture dilates its alpha by this much (see
    /// <see cref="PreparedObstacleCutout"/>); a piece whose figure is a mesh widens its silhouette by
    /// it.</para>
    /// </summary>
    public static class BoardPieceOutline
    {
        /// <summary>The line's width as a share of the hex's outer radius.</summary>
        public const float WidthFraction = 0.05f;

        /// <summary>The line's colour.</summary>
        public static readonly Color32 Colour = new Color32(0, 0, 0, 255);

        /// <summary>The line's width in world units on a board of this hex radius.</summary>
        public static float WorldWidth(float outerRadius) => outerRadius * WidthFraction;
    }
}
