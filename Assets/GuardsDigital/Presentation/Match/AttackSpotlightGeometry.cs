using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z12. Where the camera stands at full framing.
    ///
    /// <para>It is an answer in the camera's own basis rather than a transform, deliberately: building
    /// the rotation needs <c>Quaternion.LookRotation</c>, which is engine-native, and keeping it out of
    /// this struct is what lets the whole framing calculation be unit-tested with no Unity runtime at
    /// all. The view composes the rotation from <see cref="ViewDirection"/> and
    /// <see cref="ViewUp"/>.</para>
    /// </summary>
    public readonly struct AttackSpotlightCameraFrame
    {
        public AttackSpotlightCameraFrame(
            Vector3 focus,
            Vector3 viewDirection,
            Vector3 viewUp,
            float orthographicSize,
            float tiltDegrees)
        {
            Focus = focus;
            ViewDirection = viewDirection;
            ViewUp = viewUp;
            OrthographicSize = orthographicSize;
            TiltDegrees = tiltDegrees;
        }

        /// <summary>The point on the board plane the camera looks at: between the two combatants.</summary>
        public Vector3 Focus { get; }

        /// <summary>Where the camera looks, tilted off straight-down by <see cref="TiltDegrees"/>.</summary>
        public Vector3 ViewDirection { get; }

        /// <summary>The camera's up, kept in the board's own screen-up plane so the board reads level.</summary>
        public Vector3 ViewUp { get; }

        /// <summary>The orthographic half-height, in board units.</summary>
        public float OrthographicSize { get; }

        public float TiltDegrees { get; }
    }

    /// <summary>
    /// Milestone 8.35z12. The whole camera calculation for the NINETEENTH AMENDMENT's
    /// "zoom in on the heroes", as arithmetic rather than as a scene setup.
    ///
    /// <para><b>Why this can be arithmetic at all.</b> The board camera is ORTHOGRAPHIC and looks
    /// straight down (<c>PlaceholderTestGridView.FrameWholeMap</c> writes
    /// <c>LookRotation(Vector3.down, screenUp)</c>), so zoom is one number - the orthographic
    /// half-height - and distance changes nothing at all about what is drawn. A tilt of t about the
    /// camera's own right axis foreshortens the board along screen-up by exactly <c>cos t</c> and shows
    /// exactly <c>depth * sin t</c> of a piece's side. Both are closed forms, which is why the framing
    /// is proven offline and only its application needs Unity.</para>
    /// </summary>
    public static class AttackSpotlightFraming
    {
        /// <summary>
        /// The frame that holds both combatants at the stated tilt.
        /// </summary>
        /// <param name="attackerPosition">The attacking piece's world position.</param>
        /// <param name="defenderPosition">The defending piece's world position.</param>
        /// <param name="screenRight">The board's own screen-right axis, as the board camera uses it.</param>
        /// <param name="screenUp">The board's own screen-up axis.</param>
        /// <param name="aspect">The camera's width-over-height aspect.</param>
        /// <param name="pieceRadius">How much room one piece needs around its own centre.</param>
        /// <param name="restingOrthographicSize">
        /// The board camera's standing size. The spotlight never zooms OUT past it: a "spotlight" that
        /// widened the view would be the opposite of the word.
        /// </param>
        /// <param name="tiltDegrees">The tilt, clamped to the amendment's own 10-20 degree band.</param>
        public static AttackSpotlightCameraFrame Frame(
            Vector3 attackerPosition,
            Vector3 defenderPosition,
            Vector3 screenRight,
            Vector3 screenUp,
            float aspect,
            float pieceRadius,
            float restingOrthographicSize,
            float tiltDegrees = AttackSpotlightTiming.TiltDegrees)
        {
            var right = screenRight.sqrMagnitude > 0f ? screenRight.normalized : Vector3.right;
            var up = screenUp.sqrMagnitude > 0f ? screenUp.normalized : Vector3.forward;
            var safeAspect = aspect > 0.01f ? aspect : 16f / 9f;
            var radius = pieceRadius > 0f ? pieceRadius : 1f;

            var focus = (attackerPosition + defenderPosition) * 0.5f;
            var separation = defenderPosition - attackerPosition;

            // The tilt is a function of how far apart the pair stands, not a constant, and the
            // caller's own value is the CLOSE-RANGE tilt this tapers down from. See
            // AttackSpotlightTiming.TiltTaperEndSeparation for why a wide shot flattens.
            var tilt = TiltForSeparation(separation.magnitude, tiltDegrees);

            // The span the two pieces need in each of the camera's own axes: the distance between
            // their centres projected onto that axis, plus a whole piece at each end.
            var rightSpan = Mathf.Abs(Vector3.Dot(separation, right)) + (2f * radius);
            var upSpan = Mathf.Abs(Vector3.Dot(separation, up)) + (2f * radius);

            // The tilt foreshortens the board along screen-up, so the same world span needs a
            // proportionally larger half-height to stay on screen. cos(16 degrees) is 0.961, so this
            // is a small correction - and leaving it out is exactly how a tilted framing clips the
            // piece at the top of the screen.
            var cosine = Mathf.Cos(tilt * Mathf.Deg2Rad);
            var neededForUp = upSpan * 0.5f / Mathf.Max(0.05f, cosine);
            var neededForRight = rightSpan * 0.5f / safeAspect;
            var needed = Mathf.Max(neededForUp, neededForRight) *
                (1f + AttackSpotlightTiming.FramingMargin);

            // Clamped BOTH ways. The floor keeps an adjacent pair tight and cinematic rather than
            // pressed against the frame; the ceiling is what stops a ranged pair from opening the
            // shot out until it is the board view again. The resting size is a third, softer ceiling:
            // it only binds on a board framed tighter than this milestone's own maximum, and it is
            // what guarantees the spotlight is never a ZOOM OUT on any board.
            var size = Mathf.Clamp(
                needed,
                AttackSpotlightTiming.MinimumOrthographicSize,
                AttackSpotlightTiming.MaximumOrthographicSize);
            if (restingOrthographicSize > 0f)
            {
                size = Mathf.Min(size, restingOrthographicSize);
            }

            // Straight down, rotated by the tilt about the camera's own right axis. The camera ends up
            // slightly "behind" the focus in screen-up terms, looking down and forward at it.
            var radians = tilt * Mathf.Deg2Rad;
            var sine = Mathf.Sin(radians);
            var direction = ((-Vector3.up) * cosine) + (up * sine);
            var viewUp = (up * cosine) + (Vector3.up * sine);
            return new AttackSpotlightCameraFrame(
                focus, direction.normalized, viewUp.normalized, size, tilt);
        }

        /// <summary>
        /// The tilt for a pair this far apart: the full close-range tilt up to
        /// <see cref="AttackSpotlightTiming.TiltTaperStartSeparation"/>, the amendment's floor at and
        /// beyond <see cref="AttackSpotlightTiming.TiltTaperEndSeparation"/>, and a straight ramp
        /// between them.
        ///
        /// <para>The ramp is linear on purpose and is not a breach of the motion language's
        /// "no linear motion": nothing here moves over TIME. It is a static parameter read once per
        /// frame from a separation that is itself already changing smoothly, and easing an easing
        /// would only make the relationship harder to reason about.</para>
        /// </summary>
        public static float TiltForSeparation(
            float separationWorld, float closeRangeTiltDegrees = AttackSpotlightTiming.TiltDegrees)
        {
            var close = Mathf.Clamp(
                closeRangeTiltDegrees,
                AttackSpotlightTiming.MinimumTiltDegrees,
                AttackSpotlightTiming.MaximumTiltDegrees);
            var start = AttackSpotlightTiming.TiltTaperStartSeparation;
            var end = AttackSpotlightTiming.TiltTaperEndSeparation;
            if (end <= start || separationWorld <= start)
            {
                return close;
            }

            if (separationWorld >= end)
            {
                return AttackSpotlightTiming.MinimumTiltDegrees;
            }

            var travelled = (separationWorld - start) / (end - start);
            return Mathf.Lerp(close, AttackSpotlightTiming.MinimumTiltDegrees, travelled);
        }

        /// <summary>
        /// The camera position that looks at <paramref name="frame"/>'s focus from
        /// <paramref name="distance"/> away. Under an orthographic camera the distance changes nothing
        /// but clipping, so it is the board camera's own 60-unit standoff by default.
        /// </summary>
        public static Vector3 PositionFor(AttackSpotlightCameraFrame frame, float distance = 60f)
        {
            var back = frame.ViewDirection.sqrMagnitude > 0f
                ? frame.ViewDirection.normalized
                : Vector3.down;
            return frame.Focus - (back * Mathf.Max(1f, distance));
        }

        /// <summary>
        /// How much of a piece's side a tilt reveals, in board units - the number that decides whether
        /// the NINETEENTH AMENDMENT's "give tokens a bit of depth, so we can even tilt camera a bit"
        /// actually reads. It is exact under an orthographic camera, which is why the depth constant
        /// and the tilt constant can be chosen together rather than by eye.
        /// </summary>
        public static float VisibleSideHeight(float pieceDepth, float tiltDegrees) =>
            Mathf.Max(0f, pieceDepth) * Mathf.Sin(tiltDegrees * Mathf.Deg2Rad);
    }

    /// <summary>
    /// Milestone 8.35z12. Where the two card faces stand, in the HUD's own reference units.
    ///
    /// <para><b>The rule, stated once:</b> each card takes the screen edge its own piece is nearer to.
    /// It is the whole reason this is a stated rule rather than "beside the piece" - two cards placed
    /// beside two adjacent pieces overlap each other and cover both pieces, which is precisely what the
    /// zoom exists to stop. Taking opposite edges makes three properties true by construction rather
    /// than by hoping: the cards never overlap, both stay wholly on screen, and the framed pieces in
    /// the middle of the screen stay uncovered.</para>
    /// </summary>
    public static class AttackSpotlightCardLayout
    {
        /// <summary>The clearance kept between a card and the panel edge, in reference units.</summary>
        public const float EdgeMargin = 48f;

        /// <summary>The clearance kept between a card and the piece it belongs to.</summary>
        public const float PieceGap = 40f;

        /// <summary>
        /// Places the two faces, each beside its OWN combatant.
        ///
        /// <para>Milestone 8.35z12's ranged pass replaced a fixed screen-edge rule with this.
        /// The edge rule was right for the adjacent pair it was written against - both pieces near the
        /// middle, one card to each side - and wrong the moment a Ranged Attack put the pair far
        /// apart, because a card pinned to the far edge is nowhere near the combatant it describes.
        /// Each card now sits directly beside its own piece, on the side AWAY from the other one, and
        /// FLIPS to the piece's inner side when the outer side would leave the panel.</para>
        ///
        /// <para>Three properties are then enforced rather than hoped for, in this order: both cards
        /// wholly on screen, the two cards disjoint from each other, and neither card covering either
        /// PIECE - the last being the one that actually ruins a spotlight, since the pieces are what
        /// the camera just travelled to show.</para>
        /// </summary>
        /// <returns>
        /// False - with both rectangles empty - when the panel is too narrow to hold two cards, their
        /// gaps and their margins. A spotlight then shows no cards rather than overlapping ones.
        /// </returns>
        public static bool TryPlace(
            Vector2 attackerPanelPoint,
            Vector2 defenderPanelPoint,
            Vector2 panelViewport,
            Vector2 cardSize,
            out Rect attackerCard,
            out Rect defenderCard,
            float edgeMargin = EdgeMargin)
        {
            attackerCard = default;
            defenderCard = default;
            if (cardSize.x <= 0f || cardSize.y <= 0f ||
                panelViewport.x < ((2f * cardSize.x) + (2f * edgeMargin)) ||
                panelViewport.y < (cardSize.y + (2f * edgeMargin)))
            {
                return false;
            }

            var attackerIsLeft = TakesLeftEdge(attackerPanelPoint, defenderPanelPoint);
            attackerCard = Beside(
                attackerPanelPoint, panelViewport, cardSize, attackerIsLeft, edgeMargin);
            defenderCard = Beside(
                defenderPanelPoint, panelViewport, cardSize, !attackerIsLeft, edgeMargin);

            Separate(ref attackerCard, ref defenderCard, panelViewport, cardSize, edgeMargin, attackerIsLeft);
            ClearOfPieces(ref attackerCard, attackerPanelPoint, defenderPanelPoint, panelViewport, edgeMargin);
            ClearOfPieces(ref defenderCard, attackerPanelPoint, defenderPanelPoint, panelViewport, edgeMargin);
            return true;
        }

        /// <summary>
        /// One card beside one piece: outward if it fits, flipped inward if it does not, clamped if
        /// neither side does.
        /// </summary>
        private static Rect Beside(
            Vector2 piecePoint,
            Vector2 panelViewport,
            Vector2 cardSize,
            bool outwardIsLeft,
            float edgeMargin)
        {
            var lowest = edgeMargin;
            var highest = Mathf.Max(edgeMargin, panelViewport.x - edgeMargin - cardSize.x);
            var outward = outwardIsLeft
                ? piecePoint.x - PieceGap - cardSize.x
                : piecePoint.x + PieceGap;
            var inward = outwardIsLeft
                ? piecePoint.x + PieceGap
                : piecePoint.x - PieceGap - cardSize.x;

            var x = outward;
            if (outward < lowest || outward > highest)
            {
                // The piece is close enough to its own edge that the outward side has run out of
                // panel. Flip to the other side of the SAME piece rather than sliding the card away
                // from it - a card beside the wrong piece is worse than a card on the wrong side.
                x = inward >= lowest && inward <= highest ? inward : Mathf.Clamp(outward, lowest, highest);
            }

            var y = Mathf.Clamp(
                piecePoint.y - (cardSize.y * 0.5f),
                edgeMargin,
                Mathf.Max(edgeMargin, panelViewport.y - edgeMargin - cardSize.y));
            return new Rect(x, y, cardSize.x, cardSize.y);
        }

        /// <summary>
        /// Pushes two overlapping cards apart along x, and falls back to the opposite panel edges when
        /// the panel is too tight to separate them any other way - which the width check in
        /// <see cref="TryPlace"/> has already proven is always possible.
        /// </summary>
        private static void Separate(
            ref Rect attackerCard,
            ref Rect defenderCard,
            Vector2 panelViewport,
            Vector2 cardSize,
            float edgeMargin,
            bool attackerIsLeft)
        {
            if (!attackerCard.Overlaps(defenderCard))
            {
                return;
            }

            ref var leftCard = ref attackerIsLeft ? ref attackerCard : ref defenderCard;
            ref var rightCard = ref attackerIsLeft ? ref defenderCard : ref attackerCard;
            var lowest = edgeMargin;
            var highest = Mathf.Max(edgeMargin, panelViewport.x - edgeMargin - cardSize.x);
            var overlap = leftCard.xMax - rightCard.xMin;
            leftCard.x = Mathf.Clamp(leftCard.x - (overlap * 0.5f) - 1f, lowest, highest);
            rightCard.x = Mathf.Clamp(rightCard.x + (overlap * 0.5f) + 1f, lowest, highest);
            if (!leftCard.Overlaps(rightCard))
            {
                return;
            }

            leftCard.x = lowest;
            rightCard.x = highest;
        }

        /// <summary>
        /// Nudges a card off a piece it is covering. Vertical only, so it cannot undo
        /// <see cref="Separate"/>, and it takes whichever direction has more panel left.
        /// </summary>
        private static void ClearOfPieces(
            ref Rect card,
            Vector2 attackerPanelPoint,
            Vector2 defenderPanelPoint,
            Vector2 panelViewport,
            float edgeMargin)
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var covered = card.Contains(attackerPanelPoint)
                    ? attackerPanelPoint
                    : card.Contains(defenderPanelPoint)
                        ? defenderPanelPoint
                        : (Vector2?)null;
                if (covered == null)
                {
                    return;
                }

                var lowest = edgeMargin;
                var highest = Mathf.Max(edgeMargin, panelViewport.y - edgeMargin - card.height);
                var above = covered.Value.y - card.height - PieceGap;
                var below = covered.Value.y + PieceGap;
                var roomAbove = above - lowest;
                var roomBelow = highest - below;
                card.y = Mathf.Clamp(roomAbove >= roomBelow ? above : below, lowest, highest);
            }
        }

        /// <summary>
        /// Whether the ATTACKER is the left member of the pair, which is what decides each card's
        /// outward side. A dead heat on x is broken by y and then by nothing at all, because a
        /// deterministic answer for two pieces on the same point matters more than which answer it is -
        /// two clients that disagreed here would draw the same attack mirrored.
        /// </summary>
        public static bool TakesLeftEdge(Vector2 attackerPanelPoint, Vector2 defenderPanelPoint)
        {
            const float tolerance = 0.001f;
            if (Mathf.Abs(attackerPanelPoint.x - defenderPanelPoint.x) > tolerance)
            {
                return attackerPanelPoint.x < defenderPanelPoint.x;
            }

            if (Mathf.Abs(attackerPanelPoint.y - defenderPanelPoint.y) > tolerance)
            {
                return attackerPanelPoint.y < defenderPanelPoint.y;
            }

            return true;
        }
    }
}
