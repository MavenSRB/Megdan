using System;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.35z36. How a piece states which team it is on, when the answer is not
    /// known until the match is composed.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this type exists.</b> Until a hero could be selected, every hero had exactly one
    /// team for the life of the build: the printed line-up put Wasp on Red and Arien on Blue and
    /// nothing could put either anywhere else. That let a token's team be baked into its artwork -
    /// the preparation script writes <c>&lt;content-id&gt;-red.png</c> and
    /// <c>&lt;content-id&gt;-blue.png</c> from the source set's two ringed faces, and the runtime
    /// picks a file by team. With a selector, any hero can be on either team, so a baked pair is
    /// either twice the art forever or a hero that cannot be selected onto one side. The user's
    /// standing directive settles it: new heroes ship ONE white-background token and the team is
    /// applied at runtime.</para>
    /// <para><b>The mechanism chosen, and why it is the smallest faithful one.</b> A piece states
    /// its team with a RING in this project's own Red or Blue, read from the authoritative
    /// <see cref="TeamSide"/>. That ring is not new - it has been on the board since milestone 4.4
    /// as the generated prism's colour and since 8.11b as the token's rim, and it is already the
    /// only thing on a token that is derived rather than drawn. Team-baked and team-neutral faces
    /// now use the same ring width so every player accent has the same visible weight. Nothing tints the
    /// portrait: a tint over artwork would dim the white background the directive asks for and
    /// would make a hero's face read differently on the two teams, which is a cost the ring does
    /// not have.</para>
    /// <para><b>Why it lives in Application.</b> Same reason <c>PrivateLobbyLabels</c> and
    /// <c>SeatProjection</c> do (milestone 8.35a's law): what a surface may show is decided where
    /// the state is, not by the surface. Keeping the decision Unity-free is also what makes it
    /// provable - the whole of it runs in the cross-platform harness, so "the same hero on two
    /// teams presents two identities" and "the shipped six are untouched" are both measured rather
    /// than eyeballed. <c>HeroTokenArtwork</c> is a thin adapter over this.</para>
    /// </remarks>
    public static class RuntimeTeamIdentity
    {
        /// <summary>
        /// The ignored resource folder prepared hero token faces live under. Stated here because
        /// the resource NAME is part of the identity decision; loading it is Unity's job.
        /// </summary>
        public const string TokenResourceRoot = "Private/Heroes";

        /// <summary>
        /// The whole piece's radius in world units, rim included. Unchanged from milestone 8.11b:
        /// a hero token and a minion token are one size on the board because they are one size on
        /// the table.
        /// </summary>
        public const float TokenOuterRadius = 0.81f;

        /// <summary>
        /// One face radius for every artwork-backed hero token. The outer radius and pointer
        /// footprint remain unchanged; the resulting player-colour ring is 0.15 world units.
        /// </summary>
        public const float TokenFaceRadius = 0.66f;

        /// <summary>Compatibility name for the common artwork-backed face radius.</summary>
        public const float TeamBakedFaceRadius = TokenFaceRadius;

        /// <summary>
        /// The face radius inside the ring of a TEAM-NEUTRAL token.
        /// </summary>
        public const float NeutralFaceRadius = TokenFaceRadius;

        /// <summary>
        /// This project's own board colour for a team, as straight RGBA in 0..1.
        /// </summary>
        /// <remarks>
        /// <para>These are the two colours milestone 4.4 settled and every team-coloured surface in
        /// the build has read since. They are stated here, Unity-free, rather than in the view, so
        /// that a fixture can assert a team's colour without a Unity domain and so the board's Red
        /// is one Red whichever piece, rail or bar is drawing it.</para>
        /// <para><see cref="TeamSide.None"/> has no colour and is refused rather than defaulted: a
        /// piece with no team is a bug at the composition, and quietly painting it Red is how such
        /// a bug reaches a player looking correct.</para>
        /// </remarks>
        public static TeamColorValue TeamColor(TeamSide side)
        {
            switch (side)
            {
                case TeamSide.Red: return new TeamColorValue(0.82f, 0.12f, 0.12f, 1f);
                case TeamSide.Blue: return new TeamColorValue(0.1f, 0.35f, 0.9f, 1f);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(side), side,
                        "A piece on the board is on a team; there is no colour for no team.");
            }
        }

        /// <summary>
        /// The token resource a hero's TEAM-BAKED face would be at: content identity and team, in
        /// that order. Exactly milestone 8.11b's path, unchanged.
        /// </summary>
        public static string TeamBakedResourcePath(HeroContentId contentId, TeamSide side)
        {
            RequireContent(contentId);
            switch (side)
            {
                case TeamSide.Red: return $"{TokenResourceRoot}/{contentId.Value}-red";
                case TeamSide.Blue: return $"{TokenResourceRoot}/{contentId.Value}-blue";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(side), side, "A baked face is a Red face or a Blue face.");
            }
        }

        /// <summary>
        /// The token resource a hero's TEAM-NEUTRAL face is at: the content identity alone.
        /// </summary>
        /// <remarks>
        /// It takes no team on purpose, and that absence is the directive made structural - there
        /// is no parameter here through which a team could reach the artwork, so no future caller
        /// can bake one back in by accident.
        /// </remarks>
        public static string NeutralResourcePath(HeroContentId contentId)
        {
            RequireContent(contentId);
            return $"{TokenResourceRoot}/{contentId.Value}";
        }

        /// <summary>
        /// How a piece should be drawn, given which of its faces the build actually has.
        /// </summary>
        /// <param name="side">The authoritative team from the composed match. Never a hero fact.</param>
        /// <param name="hasTeamBakedFace">
        /// Whether a prepared face exists for this hero AND this team. The shipped six have both;
        /// a clean checkout has neither, because the artwork is private and Git-ignored.
        /// </param>
        /// <param name="hasNeutralFace">Whether a prepared white-background face exists.</param>
        /// <remarks>
        /// <b>The baked face wins where it exists, and that ordering is the zero-regression
        /// guarantee.</b> The six shipped heroes still resolve to their prepared faces, while
        /// all artwork-backed heroes now share the same ring width. A hero with only a white token
        /// resolves to it. A hero with neither keeps the generated prism, which has read the team from
        /// <see cref="TeamSide"/> since milestone 4.4 and needed nothing from this milestone.
        /// </remarks>
        public static TokenPresentation ResolveToken(
            HeroContentId contentId, TeamSide side, bool hasTeamBakedFace, bool hasNeutralFace)
        {
            RequireContent(contentId);
            if (side != TeamSide.Red && side != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(side), side, "A hero on the board is on a team.");
            }

            var ring = TeamColor(side);

            if (hasTeamBakedFace)
            {
                return new TokenPresentation(
                    TokenFaceKind.TeamBaked,
                    TeamBakedResourcePath(contentId, side),
                    TeamBakedFaceRadius,
                    ring);
            }

            if (hasNeutralFace)
            {
                return new TokenPresentation(
                    TokenFaceKind.TeamNeutral,
                    NeutralResourcePath(contentId),
                    NeutralFaceRadius,
                    ring);
            }

            return new TokenPresentation(TokenFaceKind.GeneratedPrism, string.Empty, 0f, ring);
        }

        private static void RequireContent(HeroContentId contentId)
        {
            if (!contentId.IsDefined)
            {
                throw new ArgumentException(
                    "A token is addressed by a hero's content identity.", nameof(contentId));
            }
        }
    }

    /// <summary>Milestone 8.35z36. Which of a piece's three drawings the build resolved.</summary>
    public enum TokenFaceKind
    {
        /// <summary>No prepared artwork: milestone 4.4's generated prism, in the team's colour.</summary>
        GeneratedPrism = 0,

        /// <summary>A face prepared for this hero on this team - the shipped six's arrangement.</summary>
        TeamBaked = 1,

        /// <summary>
        /// One white-background face for this hero on either team, with the team stated entirely by
        /// the ring. Every hero from the roster expansion onward.
        /// </summary>
        TeamNeutral = 2
    }

    /// <summary>
    /// Milestone 8.35z36. One straight RGBA colour, Unity-free, so a team's colour can be
    /// asserted in the cross-platform harness and converted at the view.
    /// </summary>
    public readonly struct TeamColorValue : IEquatable<TeamColorValue>
    {
        public TeamColorValue(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float A { get; }

        public bool Equals(TeamColorValue other) =>
            R.Equals(other.R) && G.Equals(other.G) && B.Equals(other.B) && A.Equals(other.A);

        public override bool Equals(object obj) => obj is TeamColorValue other && Equals(other);

        public override int GetHashCode() =>
            R.GetHashCode() ^ G.GetHashCode() ^ B.GetHashCode() ^ A.GetHashCode();

        public override string ToString() => $"rgba({R}, {G}, {B}, {A})";
    }

    /// <summary>
    /// Milestone 8.35z36. Everything a view needs to draw one hero's piece, decided from
    /// the authoritative team rather than from the picture.
    /// </summary>
    public readonly struct TokenPresentation
    {
        public TokenPresentation(
            TokenFaceKind faceKind, string resourcePath, float faceRadius, TeamColorValue ringColor)
        {
            FaceKind = faceKind;
            ResourcePath = resourcePath ?? string.Empty;
            FaceRadius = faceRadius;
            RingColor = ringColor;
        }

        public TokenFaceKind FaceKind { get; }

        /// <summary>The resource to load, or empty when the piece falls back to the prism.</summary>
        public string ResourcePath { get; }

        /// <summary>The face's radius inside the ring; zero when there is no face.</summary>
        public float FaceRadius { get; }

        /// <summary>The team's colour, always present - a prism is drawn in it too.</summary>
        public TeamColorValue RingColor { get; }

        /// <summary>
        /// How wide the team ring is - the band between the piece's outer radius and its face.
        /// Zero for a prism, which is team-coloured throughout rather than ringed.
        /// </summary>
        public float RingWidth =>
            FaceKind == TokenFaceKind.GeneratedPrism
                ? 0f
                : RuntimeTeamIdentity.TokenOuterRadius - FaceRadius;
    }
}
