using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using UnityEngine;

namespace GuardsDigital.Presentation.Units.Heroes
{
    /// <summary>
    /// Milestone 8.11b. Where a hero's official circular token sits, and how to load it when it is
    /// present.
    ///
    /// <para>The artwork is private. It is prepared out of the local Tabletopia corpus by
    /// <c>Tools/Prepare-HeroTokens.ps1</c> into ignored resources, so a clean checkout has no token
    /// at all and <see cref="TryLoadTexture"/> simply answers false - which is what keeps the generated
    /// primitive prism as the tracked project's ordinary presentation.</para>
    ///
    /// <para><b>The face is selected, never read.</b> The resource is addressed by the hero's projected
    /// content identity and their authoritative team, exactly as
    /// <see cref="Units.Minions.StandardMinionArtCatalog"/> addresses a minion's. Nothing asks a pixel
    /// which team a piece is on: the preparation script settled Steam/orange to Red and
    /// Crystal/light-blue to Blue once, off-line, under the reviewed milestone 4.4 mapping, and at
    /// runtime the team decides the file rather than the file deciding the team.</para>
    ///
    /// <para><b>The team is stated a second time, in the project's own colours.</b> The official faces
    /// are ringed orange and light blue, which are the source set's team colours rather than this
    /// project's; a player reading the board should not have to know that orange means Red. So the
    /// piece carries a rim in <see cref="TeamRim"/>, drawn from the authoritative
    /// <see cref="TeamSide"/> - the same two colours the prism it replaces has been drawn in since
    /// milestone 4.4, stated once here and read by both, so the board's Red is one Red whichever piece
    /// is standing on it.</para>
    /// </summary>
    public static class HeroTokenArtwork
    {
        /// <summary>
        /// The ignored resource folder the preparation script writes into. It is under
        /// <c>Private/</c> for the reason milestone 8.11a's board face is: Git keeps the whole folder
        /// out, so the tracked project cannot carry the artwork even by accident.
        /// </summary>
        public const string ResourceRoot = "Private/Heroes";

        /// <summary>
        /// The whole piece's radius in world units at <see cref="Board.HexWorldLayout.DefaultOuterRadius"/>,
        /// rim included.
        ///
        /// It is <see cref="Units.Minions.StandardMinionView.DefaultScale"/> halved, so a hero token and
        /// a minion token are one size on the board - which is what they are on the table, both being
        /// round tokens standing on one space - and so a hero covers its space the way milestone 8.11a
        /// made a minion cover its own.
        /// </summary>
        public const float TokenOuterRadius = 0.81f;

        /// <summary>The official face's radius, inside the rim.</summary>
        public const float FaceRadius = 0.72f;

        /// <summary>
        /// How much of the source face's inscribed circle the disc samples.
        ///
        /// The prepared token is a circle inscribed in its square with transparent corners, and the
        /// mesh is drawn with an opaque shader, so the disc must stay inside that circle rather than
        /// reach its very edge - a sample landing on the transparent border would pull the cleared
        /// pixels in as black. One and a half percent is under half a source pixel of the ring.
        /// </summary>
        public const float FaceInset = 0.985f;

        /// <summary>
        /// How many segments the disc and its rim are built from. Sixty-four is under a tenth of a
        /// degree of chord error at this radius, which is well inside one screen pixel at every
        /// validated viewport.
        /// </summary>
        public const int Segments = 64;

        /// <summary>
        /// The resource this hero's face would be at. Content identity and team, in that order, the
        /// same shape <see cref="Units.Minions.StandardMinionArtCatalog"/> addresses a minion by.
        /// </summary>
        public static string ResourcePath(HeroContentId contentId, TeamSide teamSide) =>
            $"{ResourceRoot}/{contentId.Value}-{TeamToken(teamSide)}";

        /// <summary>
        /// Loads a hero's prepared token face, answering false when there is none. Absence is the
        /// tracked project's ordinary state: the resources are ignored by Git, so a clean checkout
        /// resolves nothing and the hero keeps its generated prism.
        /// </summary>
        public static bool TryLoadTexture(HeroContentId contentId, TeamSide teamSide, out Texture2D texture)
        {
            texture = teamSide == TeamSide.Red || teamSide == TeamSide.Blue
                ? Resources.Load<Texture2D>(ResourcePath(contentId, teamSide))
                : null;
            return texture != null;
        }

        /// <summary>
        /// This project's own board colour for a team, from the authoritative side.
        ///
        /// These are the two colours the generated hero prism has carried since milestone 4.4. They are
        /// stated here rather than there because the token's rim and the prism must be one Red and one
        /// Blue - the piece changes shape when the private artwork is present, and a player should not
        /// see the team's colour change with it.
        /// </summary>
        public static Color TeamRim(TeamSide teamSide) =>
            teamSide == TeamSide.Blue
                ? new Color(0.1f, 0.35f, 0.9f, 1f)
                : new Color(0.82f, 0.12f, 0.12f, 1f);

        private static string TeamToken(TeamSide teamSide) =>
            teamSide == TeamSide.Blue ? "blue" : "red";
    }
}
