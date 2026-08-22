using System;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// Milestone 8.11a. Where the official board artwork sits relative to the reviewed map, and how to
    /// load it when it is present.
    ///
    /// <para>The artwork is private. It is prepared out of the local Tabletopia corpus by
    /// <c>Tools/Prepare-OfficialBoardArtwork.ps1</c> into an ignored resource, so a clean checkout has
    /// no artwork at all and <see cref="TryLoadTexture"/> simply answers false. Nothing in this type
    /// is rules authority: the map definition owns which spaces exist and where, and this owns only
    /// where to draw a picture underneath them.</para>
    ///
    /// <para><b>How the registration was derived.</b> The bundled manifest states, for the element
    /// named <c>GuardsII_Small_Board</c>, a 47.64-unit square face and a magnetic map of 332 circle
    /// centres. Those centres fit a single hexagonal lattice exactly - 332 distinct lattice indices,
    /// worst deviation 10.4% of the 2.4074-unit spacing - and they are stated in the same frame as the
    /// sprite, which was confirmed by drawing all 332 onto the extracted face and finding every one on
    /// a painted hex.</para>
    ///
    /// <para><b>What the centres could not settle, and what did.</b> Matching the map's 292 spaces onto
    /// that lattice is nearly six-fold ambiguous: the twelve lattice symmetries score 272 to 283 of 292,
    /// so the best-scoring one is not meaningfully better than the others and the orientation cannot be
    /// read off the count. It was settled against the artwork instead, by drawing the map's seven zones
    /// over the face: at the orientation below every zone boundary follows a painted boundary - the Red
    /// Throne on the drill, the Blue Throne on the crystal, both jungles on the forests, and the terrain
    /// ring on the rock - and at every other orientation they do not. The highest-scoring lattice match
    /// is <i>not</i> that orientation, which is the whole reason this note exists.</para>
    ///
    /// <para><b>Measured registration.</b> Fitting a similarity - one scale, one rotation, no shear and
    /// no mirroring - over the 275 spaces that have a magnetic centre gives a mean deviation of 0.031
    /// and a worst of 0.076 board units, which is <see cref="MeasuredWorstRegistrationError"/> world
    /// units, or 5.4% of a space's outer radius. <see cref="RegistrationTolerance"/> is the declared
    /// bound, at a tenth of a space, and the conformance test measures against it rather than trusting
    /// this comment.</para>
    /// </summary>
    public static class OfficialBoardArtwork
    {
        /// <summary>
        /// Where <c>Tools/Prepare-OfficialBoardArtwork.ps1</c> writes the prepared face. The path is
        /// ignored by Git, so this resolving to nothing is an ordinary state and not a fault.
        /// </summary>
        public const string ResourcePath = "Private/OfficialBoard/official-single-lane-board";

        /// <summary>The side of the square face, in the manifest's own board units.</summary>
        public const float ArtworkSideInBoardUnits = 47.64f;

        /// <summary>
        /// The fitted similarity, board units to world units at <see cref="HexWorldLayout.DefaultOuterRadius"/>.
        /// A world position scales linearly with the outer radius, so every method here takes it.
        /// </summary>
        public const float BoardToWorldScale = 0.71333466f;

        /// <summary>The fitted rotation, board frame to world frame, in degrees about world +Y.</summary>
        public const float BoardToWorldRotationDegrees = -14.984605f;

        /// <summary>The declared visual tolerance for the whole board, in world units at radius 1.</summary>
        public const float RegistrationTolerance = 0.10f;

        /// <summary>
        /// The worst deviation actually measured across the 275 registered spaces, recorded so a
        /// regression that widens it is visible as a number rather than as a passing test.
        /// </summary>
        public const float MeasuredWorstRegistrationError = 0.0542f;

        private static readonly Vector2 BoardOriginInWorld = new Vector2(-3.40922343f, -4.36677368f);

        // The rotation, precomputed. cos(-14.984605 deg) and sin(-14.984605 deg) scaled by the fit.
        private const float LinearA = 0.68907795f;
        private const float LinearB = 0.18443945f;

        /// <summary>
        /// The board's own +X axis as a unit world direction on the board plane.
        ///
        /// <para>Milestone 8.11a, by the user's decision of 2026-08-22: the framing camera is oriented
        /// by these axes rather than by the world ones, so the square face reads upright on screen, the
        /// way it does looking down at the physical board. The lane then runs corner to corner, because
        /// on this board it does - the hex lattice sits about fifteen degrees inside its own square, so
        /// the square and the lane cannot both be square to the screen and the artwork wins.</para>
        /// </summary>
        public static Vector3 BoardRightInWorld =>
            new Vector3(LinearA, 0f, -LinearB) / BoardToWorldScale;

        /// <summary>The board's own +Y axis as a unit world direction. See <see cref="BoardRightInWorld"/>.</summary>
        public static Vector3 BoardUpInWorld =>
            new Vector3(LinearB, 0f, LinearA) / BoardToWorldScale;

        /// <summary>
        /// The world position, on the board plane, of a point stated in the manifest's board units.
        /// </summary>
        public static Vector3 BoardToWorld(Vector2 board, float outerRadius = HexWorldLayout.DefaultOuterRadius)
        {
            RequireRadius(outerRadius);
            var x = (LinearA * board.x) + (LinearB * board.y) + BoardOriginInWorld.x;
            var z = (-LinearB * board.x) + (LinearA * board.y) + BoardOriginInWorld.y;
            return new Vector3(x * outerRadius, 0f, z * outerRadius);
        }

        /// <summary>
        /// The inverse of <see cref="BoardToWorld"/>, which is what a registration check needs: it puts
        /// a space's world position back into the artwork's own frame so the two can be compared there.
        /// </summary>
        public static Vector2 WorldToBoard(Vector3 world, float outerRadius = HexWorldLayout.DefaultOuterRadius)
        {
            RequireRadius(outerRadius);
            var x = (world.x / outerRadius) - BoardOriginInWorld.x;
            var z = (world.z / outerRadius) - BoardOriginInWorld.y;
            var inverse = 1f / ((LinearA * LinearA) + (LinearB * LinearB));
            return new Vector2(
                ((LinearA * x) - (LinearB * z)) * inverse,
                ((LinearB * x) + (LinearA * z)) * inverse);
        }

        /// <summary>
        /// The four corners of the face in world space, counter-clockwise from the board's own
        /// (-half, -half). The underlay mesh is built from exactly these, so the artwork's placement and
        /// this registration cannot drift apart.
        /// </summary>
        public static Vector3[] Corners(float outerRadius = HexWorldLayout.DefaultOuterRadius)
        {
            var half = ArtworkSideInBoardUnits * 0.5f;
            return new[]
            {
                BoardToWorld(new Vector2(-half, -half), outerRadius),
                BoardToWorld(new Vector2(half, -half), outerRadius),
                BoardToWorld(new Vector2(half, half), outerRadius),
                BoardToWorld(new Vector2(-half, half), outerRadius)
            };
        }

        /// <summary>
        /// Loads the prepared face, answering false when it is absent. Absence is the tracked project's
        /// ordinary state: the resource is ignored by Git, so a clean checkout has none and the board
        /// keeps its generated presentation.
        /// </summary>
        public static bool TryLoadTexture(out Texture2D texture)
        {
            texture = Resources.Load<Texture2D>(ResourcePath);
            return texture != null;
        }

        private static void RequireRadius(float outerRadius)
        {
            if (outerRadius <= 0f || float.IsNaN(outerRadius) || float.IsInfinity(outerRadius))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(outerRadius),
                    outerRadius,
                    "The board artwork registration requires a finite positive outer radius.");
            }
        }
    }
}
