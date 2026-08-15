namespace GuardsDigital.Core.Rules.AdjacentMove
{
    public enum AdjacentMoveRejectionReason
    {
        None,
        UnknownHero,
        WrongOrigin,
        DestinationOffBoard,
        DestinationNotAdjacent,
        DestinationOccupied,

        /// <summary>
        /// The space is a terrain cell of the selected map: on the board, permanently an obstacle
        /// (rulebook p.18), so it can be crossed under the Move-Through permission but never entered
        /// as an endpoint. Distinct from <see cref="DestinationOffBoard"/>, which no permission can
        /// answer. Milestone 6.8e.
        /// </summary>
        DestinationTerrain
    }
}
