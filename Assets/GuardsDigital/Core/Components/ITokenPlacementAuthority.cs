using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// The exact authoritative board operations a token capability may perform.
    ///
    /// This port exists so the effect frame depends on the component contract rather than on the board
    /// authority that implements it. <c>AdjacentMoveAuthority</c> implements it, and remains the only
    /// implementation: tokens are board occupants, so their placement has to go through the one
    /// occupancy index milestone 6.1r established, and nothing else owns that index.
    ///
    /// It is deliberately narrow. There is no query here that answers "which spaces may this effect
    /// target" - reusable targeting, presence, and obstacle contracts belong to milestone 6.8, and a
    /// capability that needed one would be reaching past this milestone's scope rather than past this
    /// interface.
    /// </summary>
    public interface ITokenPlacementAuthority
    {
        /// <summary>Every token currently on the board, in stable placement order.</summary>
        IReadOnlyList<PlacedToken> PlacedTokens { get; }

        /// <summary>
        /// Where a hero currently stands, or <c>false</c> when that hero is not on the board. A defeated
        /// hero has no space, and a capability must be able to tell that apart from an occupied one.
        ///
        /// <para>Milestone 9.21a. For a hero standing on several spaces this is the ACTING body's space (body
        /// 1 until milestone 9.21b makes it a pick); a read about the hero rather than about the actor
        /// asks <see cref="IsHeroOnBoard"/> or <see cref="GetHeroBodies"/>.</para>
        /// </summary>
        bool TryGetHeroCoordinate(HeroId heroId, out HexCoordinate coordinate);

        /// <summary>Milestone 9.21a. Whether any body of this hero stands on the board.</summary>
        bool IsHeroOnBoard(HeroId heroId);

        /// <summary>Milestone 9.21a. Every body of this hero on the board, in body order.</summary>
        IReadOnlyList<HeroOccupancy> GetHeroBodies(HeroId heroId);

        /// <summary>
        /// Whether this space is playable and holds no occupant of any kind. Rulebook p.18: "A game
        /// piece can only be placed into an empty space", and p.19: "There can never be more than one
        /// obstacle in any one space. Ever. No exceptions."
        /// </summary>
        bool IsEmptyPlayableSpace(HexCoordinate coordinate);

        /// <summary>
        /// Commits a placement whose legality the caller has already established. Throws rather than
        /// refusing: reaching here with an occupied space or an exhausted supply is an engine defect,
        /// and the aggregate's commit boundary rolls the whole command back.
        /// </summary>
        void CommitValidatedTokenPlacement(PlacedToken token);

        /// <summary>
        /// Removes a token from the board and returns it to its supply, yielding the instance that left
        /// so the caller can publish which token was reclaimed and from where.
        /// </summary>
        PlacedToken CommitValidatedTokenReclamation(TokenInstanceId tokenInstanceId);
    }
}
