using GuardsDigital.Core.Board;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// The board port rulebook p.8's Lane-Push transition reaches (milestone 6.10), beside the
    /// displacement, token-placement, unit-spawn and exchange ports and for the same reason each of
    /// those exists: the rule needs the authoritative questions it actually asks and one validated
    /// commit, not the whole board authority.
    ///
    /// What it asks is narrow, because the Zone stage is narrow: which Zone the Lane has reached, what
    /// the map says the Lane is, and one move along it. Notably absent is anything about occupancy -
    /// the stage that removes and respawns minions is milestone 6.11's, and a port that could reach
    /// them here would let this milestone expose half of it.
    /// </summary>
    public interface ILaneAdvanceAuthority
    {
        BoardMapDefinition MapDefinition { get; }

        /// <summary>The Zone the Lane has reached, or the named absence on a map that declares none.</summary>
        ZoneId? CurrentBattleZoneId { get; }

        /// <summary>
        /// Moves the Battle Zone one verified step along the ordered Lane, or refuses and leaves it
        /// where it is. Which Zone is next is the caller's rules decision; that the answer is a Zone
        /// one step away on this map's Lane is verified here.
        /// </summary>
        bool CommitValidatedBattleZoneAdvance(ZoneId destination);
    }
}
