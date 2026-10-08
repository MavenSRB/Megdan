using GuardsDigital.Application;
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
    /// <para><b>Milestone 8.35z36 made the team a runtime fact.</b> A baked pair of faces
    /// per hero was sound only while each hero had one team for the life of the build. With a hero
    /// selector any hero can be on either side, so a hero may instead ship ONE team-neutral
    /// white-background face and state its team entirely in the ring. The decision of which face
    /// and which ring lives in <see cref="RuntimeTeamIdentity"/>, Unity-free, so it is provable in
    /// the cross-platform harness; this type is the adapter that loads what that decision names.
    /// The baked face still wins wherever one exists, which is why the six shipped heroes' pieces
    /// are exactly what they were.</para>
    /// </summary>
    public static class HeroTokenArtwork
    {
        /// <summary>
        /// The ignored resource folder the preparation script writes into. It is under
        /// <c>Private/</c> for the reason milestone 8.11a's board face is: Git keeps the whole folder
        /// out, so the tracked project cannot carry the artwork even by accident.
        /// </summary>
        public const string ResourceRoot = RuntimeTeamIdentity.TokenResourceRoot;

        /// <summary>
        /// The whole piece's radius in world units at <see cref="Board.HexWorldLayout.DefaultOuterRadius"/>,
        /// rim included.
        ///
        /// It is <see cref="Units.Minions.StandardMinionView.DefaultScale"/> halved, so a hero token and
        /// a minion token are one size on the board - which is what they are on the table, both being
        /// round tokens standing on one space - and so a hero covers its space the way milestone 8.11a
        /// made a minion cover its own.
        /// </summary>
        public const float TokenOuterRadius = RuntimeTeamIdentity.TokenOuterRadius;

        /// <summary>
        /// The shared artwork-backed face radius inside the player-colour rim.
        /// </summary>
        public const float FaceRadius = RuntimeTeamIdentity.TokenFaceRadius;

        /// <summary>
        /// How much of a neutral source face's inscribed circle the disc samples.
        ///
        /// The prepared token is a circle inscribed in its square with transparent corners, and the
        /// mesh is drawn with an opaque shader, so the disc must stay inside that circle rather than
        /// reach its very edge - a sample landing on the transparent border would pull the cleared
        /// pixels in as black. The small margin avoids sampling those cleared edge pixels.
        /// </summary>
        public const float FaceInset = 0.985f;

        /// <summary>
        /// Team-baked art contains its old orange or blue printed rim. Crop inside that rim before
        /// drawing the face so no source-team colour peeks through the player's new accent ring.
        /// The original rim begins around 0.90 of the source radius; 0.875 leaves a pixel buffer.
        /// </summary>
        public const float TeamBakedFaceInset = 0.875f;

        public static float SourceInsetFor(TokenFaceKind kind) =>
            kind == TokenFaceKind.TeamBaked ? TeamBakedFaceInset : FaceInset;

        /// <summary>
        /// How many segments the disc and its rim are built from. Sixty-four is under a tenth of a
        /// degree of chord error at this radius, which is well inside one screen pixel at every
        /// validated viewport.
        /// </summary>
        public const int Segments = 64;

        /// <summary>
        /// The resource this hero's team-baked face would be at. Content identity and team, in that
        /// order, the same shape <see cref="Units.Minions.StandardMinionArtCatalog"/> addresses a
        /// minion by.
        /// </summary>
        public static string ResourcePath(HeroContentId contentId, TeamSide teamSide) =>
            RuntimeTeamIdentity.TeamBakedResourcePath(contentId, teamSide);

        /// <summary>
        /// The resource this hero's team-neutral white-background face would be at: the content
        /// identity alone, with no team in the address at all.
        /// </summary>
        public static string NeutralResourcePath(HeroContentId contentId) =>
            RuntimeTeamIdentity.NeutralResourcePath(contentId);

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
        /// Milestone 8.35z36. Loads whichever of this hero's faces the build has, and says
        /// how the piece must then be drawn.
        /// </summary>
        /// <remarks>
        /// Resolution order and its whole point: the team-baked face first, so the six shipped
        /// heroes resolve to their prepared file; then the team-neutral face, drawn inside the
        /// same-width player-colour ring; then nothing, and the caller keeps the prism. The
        /// ORDER is the zero-regression guarantee, and it is asserted in the harness against the
        /// shipped six rather than argued for here.
        /// </remarks>
        public static TokenPresentation ResolveToken(
            HeroContentId contentId, TeamSide teamSide, out Texture2D texture)
        {
            texture = null;
            if (teamSide != TeamSide.Red && teamSide != TeamSide.Blue)
            {
                return RuntimeTeamIdentity.ResolveToken(
                    contentId, TeamSide.Red, false, false);
            }

            var baked = Resources.Load<Texture2D>(
                RuntimeTeamIdentity.TeamBakedResourcePath(contentId, teamSide));
            var neutral = baked != null
                ? null
                : Resources.Load<Texture2D>(RuntimeTeamIdentity.NeutralResourcePath(contentId));

            texture = baked != null ? baked : neutral;
            return RuntimeTeamIdentity.ResolveToken(
                contentId, teamSide, baked != null, neutral != null);
        }

        /// <summary>
        /// This project's own board colour for a team, from the authoritative side.
        ///
        /// These are the two colours the generated hero prism has carried since milestone 4.4. They are
        /// stated in <see cref="RuntimeTeamIdentity.TeamColor"/> rather than here so that the token's
        /// rim, the prism and every Unity-free fixture read one Red and one Blue - the piece changes
        /// shape when the private artwork is present, and a player should not see the team's colour
        /// change with it.
        /// </summary>
        public static Color TeamRim(TeamSide teamSide)
        {
            var value = RuntimeTeamIdentity.TeamColor(
                teamSide == TeamSide.Blue ? TeamSide.Blue : TeamSide.Red);
            return new Color(value.R, value.G, value.B, value.A);
        }
    }
}
