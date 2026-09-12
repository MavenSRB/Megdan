namespace GuardsDigital.Core.Board
{
    /// <summary>
    /// Milestone 8.39a. The continuation of a line an earlier step of the same action already
    /// travelled: a line that ran from one space, through the acting hero, and carries on past them.
    ///
    /// <para><b>Two printed clauses read this one question, which is why it is a query rather than two
    /// filters.</b> Cutter's three lunges print "Before the attack: Move 1 space in a straight line.
    /// Target a hero adjacent to you <b>in the direction of the move</b>" - the line ran from the space
    /// she left, through where she now stands, and the target is the next space along it. Her two
    /// ranged Reds print "After the attack: Move up to 2 spaces <b>in the opposite direction</b>" - the
    /// line ran from the space she attacked, through where she stands, and the retreat is the spaces
    /// along it. Both sentences name the far end of an established line and continue it past the
    /// actor; they differ only in what is standing at the far end and in what the continuation is used
    /// for.</para>
    ///
    /// <para><b>Neither clause is a property of the board or of the target.</b>
    /// <see cref="StraightLineQuery"/> answers whether two spaces share a file, which both ends of an
    /// attack already do before anybody moves. What these sentences need is a HEADING, and a heading
    /// exists only because an earlier step fixed it - so this query takes the two spaces that fixed it
    /// rather than asking the board for a direction the board has no opinion about.</para>
    ///
    /// <para><b>A line of no length names no heading, and that is the degenerate case both callers
    /// need.</b> When the far space and the actor's space are the same - a lunge whose branch performed
    /// no move, or an Attack that somehow resolved against the actor's own space - there is no
    /// direction to continue and every answer here is <c>false</c>. That is deliberately not the same
    /// as "every direction": a clause that narrows to the continuation of a line offers nothing at all
    /// when no line was travelled, rather than falling back to the unnarrowed set.</para>
    ///
    /// <para>Purely geometric, on <see cref="StraightLineQuery"/>'s own terms: nothing here reads
    /// occupancy, obstacles or the map, so whether a continuation space may be entered or holds
    /// anybody stays the caller's question.</para>
    /// </summary>
    public static class ContinuedLineQuery
    {
        /// <summary>
        /// The heading of the line that runs from <paramref name="from"/> through
        /// <paramref name="through"/>, which is the direction its continuation travels.
        ///
        /// <para>False when the two spaces are the same - no line, no heading - and false when they do
        /// not share one of p.19's six files, because a bent path fixes no single direction.</para>
        /// </summary>
        public static bool TryGetHeading(
            HexCoordinate from, HexCoordinate through, out HexDirection heading) =>
            StraightLineQuery.TryGetDirection(from, through, out heading, out _);

        /// <summary>
        /// The space <paramref name="steps"/> further along that line, past
        /// <paramref name="through"/>. One step is the single space a "target a unit adjacent to you in
        /// the direction of the move" clause admits.
        ///
        /// <para>Zero steps is <paramref name="through"/> itself, which is what a declined "up to"
        /// retreat resolves to; a negative step count is the caller asking to travel backwards along a
        /// line it named the direction of, and is refused by <see cref="StraightLineQuery.Step"/>
        /// rather than quietly reversed here.</para>
        /// </summary>
        public static bool TryGetContinuation(
            HexCoordinate from, HexCoordinate through, int steps, out HexCoordinate space)
        {
            if (!TryGetHeading(from, through, out var heading))
            {
                space = default;
                return false;
            }

            space = StraightLineQuery.Step(through, heading, steps);
            return true;
        }

        /// <summary>
        /// Whether <paramref name="candidate"/> lies on that continuation - strictly past
        /// <paramref name="through"/>, along the same heading.
        ///
        /// <para><paramref name="through"/> itself is not on its own continuation: the clauses that ask
        /// this name a target the actor is not standing on and a retreat that travels somewhere. Nor is
        /// any space on the near side, which is what makes "the direction of the move" and "the
        /// opposite direction" mean opposite things when read from the same pair of ends.</para>
        /// </summary>
        public static bool ContinuesTheLine(
            HexCoordinate from, HexCoordinate through, HexCoordinate candidate) =>
            TryGetHeading(from, through, out var heading) &&
            StraightLineQuery.TryGetDirection(through, candidate, out var onward, out _) &&
            onward == heading;
    }
}
