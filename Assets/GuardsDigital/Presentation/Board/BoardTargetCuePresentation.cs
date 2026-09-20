namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// Milestone 8.62. Which of the two reviewed treatments a projected target cue is drawn with.
    /// </summary>
    public enum BoardTargetCueTreatment
    {
        /// <summary>
        /// The piece wears the fitted ring of its own footprint, and its space keeps the six corner
        /// marks. The user, having seen it: "I like the new outline, but coloring on the inside is a
        /// bit too much" and then "actually, just keep the previous shape, and use the new color for
        /// the outline. Don't color on the inside."
        /// </summary>
        RingOnThePiece = 0,

        /// <summary>
        /// Nothing at all on the piece; the whole TILE is outlined as a continuous hexagon in the
        /// cue colour, pointed at or not. The user: "Let's try how it looks when we remove the
        /// outline of the token, just outline the hex."
        /// </summary>
        OutlineTheWholeHex = 1,

        /// <summary>
        /// The shipped treatment. Nothing on the piece; the space keeps the six corner marks it has
        /// always had, in the target's own side colour, and POINTING at it grows them into the full
        /// hex edge. The user, of the original marks: "No, not the ring, but the old highlight
        /// mark" - and earlier, "Then on mouseover we can do full hex. That is maybe the best?"
        /// </summary>
        CornerMarksThenHoveredHexOutline = 2
    }

    /// <summary>
    /// Which side of the match a target stands on, relative to the seat that is choosing. Only the
    /// hex-outline treatment reads it.
    /// </summary>
    public enum BoardTargetOutlineSide
    {
        None = 0,

        /// <summary>A piece on the OTHER side from the choosing seat. Blood red.</summary>
        Enemy = 1,

        /// <summary>A piece on the choosing seat's OWN side. Green.</summary>
        Friendly = 2
    }

    /// <summary>
    /// Milestone 8.62. The one place that says which target-cue treatment configured play draws, and
    /// whether the cue's colour comes from the target's SIDE or from the operation's INTENT.
    ///
    /// <para><b>Why a switch rather than a replacement.</b> The user asked to see the hex-outline
    /// variant beside the ring - "Let's try how it looks..." - and chooses between two photographed
    /// results. Both treatments are therefore whole, shipped, pinned code; this type decides which
    /// one a frame uses, and flipping <see cref="Treatment"/> is the entire change. Nothing about
    /// WHICH pieces are cued moves either way: <c>BoardInteractionPresenter</c> is untouched by
    /// both, and every other reader of its cues - the pointer, the log, the corner marks of spaces
    /// that are not piece targets - is unaffected.</para>
    ///
    /// <para><b>The colour rule.</b> The user, on the variant: "Keep the colors, blood red on enemy,
    /// green on ally." That is a rule about SIDES, and the shipped palette is a rule about INTENTS
    /// (Attack and Removal blood red, everything else green). The two agree everywhere except an
    /// effect that targets an ENEMY - which the intent palette paints green - and any consequence
    /// aimed at a friendly piece. <see cref="ColourByTargetSide"/> selects the user's rule; with it
    /// off the intent palette decides, exactly as the ring does.</para>
    /// </summary>
    public static class BoardTargetCuePresentation
    {
        /// <summary>
        /// The treatment configured play SHIPS with. One line decides it. Three shapes were
        /// photographed for the user in turn - the fitted ring on the piece, the whole hex edged at
        /// all times, and the original corner marks that grow into that edge when pointed at - and
        /// the third is the one they kept. The other two stay whole behind this switch rather than
        /// being deleted, because each was approved at some point and a reviewer can ask for either
        /// again by name.
        /// </summary>
        public const BoardTargetCueTreatment ShippedTreatment =
            BoardTargetCueTreatment.CornerMarksThenHoveredHexOutline;

        /// <summary>
        /// The treatment a frame is drawn with. It is <see cref="ShippedTreatment"/> unless a
        /// reviewer asks for the other one - the photograph fixture sets it for the frames the user
        /// compares and puts it back - so both treatments are live, pinned code and neither is a
        /// branch nobody runs.
        /// </summary>
        public static BoardTargetCueTreatment Treatment { get; set; } = ShippedTreatment;

        /// <summary>
        /// Whether the hex outline takes its colour from the target's side rather than from the
        /// invoked operation's intent. The ring treatment never reads this: a ring is the intent's
        /// own colour, which is what the user approved of it.
        /// </summary>
        public static bool ColourByTargetSide { get; set; } = true;

        /// <summary>Whether the cued PIECE wears the fitted ring of its own footprint.</summary>
        public static bool DrawsTheRingOnThePiece =>
            Treatment == BoardTargetCueTreatment.RingOnThePiece;

        /// <summary>Whether a target's tile is edged whole, pointed at or not.</summary>
        public static bool OutlinesTheWholeHex =>
            Treatment == BoardTargetCueTreatment.OutlineTheWholeHex;

        /// <summary>
        /// Whether a target's TILE takes the target's side colour at all - the corner marks in it
        /// at rest, the full hex edge when pointed at.
        /// </summary>
        public static bool MarksTheTargetTileBySide =>
            Treatment != BoardTargetCueTreatment.RingOnThePiece;

        /// <summary>
        /// The cue state a PIECE actually ends up wearing when the projection offers it
        /// <paramref name="projected"/>. Under the hex-outline treatment nothing is drawn on the
        /// piece at all - "remove the outline of the token, just outline the hex" - so every journey
        /// that pins a target's piece cue states its expectation through this one place and reads
        /// correctly under either treatment.
        /// </summary>
        public static BoardTargetCueState AsDrawn(BoardTargetCueState projected) =>
            DrawsTheRingOnThePiece ? projected : BoardTargetCueState.None;

        /// <summary>
        /// The consequence a piece's retained ring reports when the projection offers it
        /// <paramref name="projected"/>. <c>None</c> under the hex-outline treatment.
        /// </summary>
        public static BoardTargetConsequenceIntent AsDrawn(
            BoardTargetConsequenceIntent projected) =>
            DrawsTheRingOnThePiece ? projected : BoardTargetConsequenceIntent.None;
    }
}
