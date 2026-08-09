namespace GuardsDigital.Core.Rules.AdjacentMove
{
    public enum AdjacentMoveRejectionReason
    {
        None,
        UnknownHero,
        WrongOrigin,
        DestinationOffBoard,
        DestinationNotAdjacent,
        DestinationOccupied
    }
}
