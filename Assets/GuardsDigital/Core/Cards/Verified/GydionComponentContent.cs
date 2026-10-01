using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Gydion the Archwizard's physical component - the FAMILIAR token - and the capabilities his
    /// composed spells name. Milestone 9.30e.
    ///
    /// <para><b>GY-Q5, as the user settled it:</b> ONE Familiar (supply one); an ordinary p.19 token,
    /// so an obstacle ("All tokens are obstacles") with no property of its own; removed at the end of
    /// the round (p.19's default, <see cref="TokenRemovalCondition.EndOfRoundCleanup"/>); NOT removed
    /// when Gydion is defeated (p.19: "Tokens are not removed when the hero who placed them is
    /// defeated" - nothing here removes it); and Find Familiar with it on the board MOVES it, Widget's
    /// Pyro shape (a Place may take a token from the board, p.18). It does nothing else. The piece is
    /// element 5831117 <c>raven</c>, a white owl holding a book (GY-03).</para>
    ///
    /// <para><b>Registered with the dealing (9.30i)</b>, milestone 7.9's gate: <see cref="Tokens"/> joins
    /// <c>SyntheticComponentDefinitions.CreateCatalog</c> - which moves the registered token list every
    /// snapshot pins - and <see cref="Handlers"/> the capability catalog, as every hero's tokens and
    /// capabilities joined at their dealing.</para>
    /// </summary>
    public static class GydionComponentContent
    {
        public static TokenDefinitionId FamiliarId { get; } = new TokenDefinitionId("gydion-familiar");

        /// <summary>GY-Q5 (user, 2026-10-01): one Familiar.</summary>
        public const int FamiliarSupplyCount = 1;

        /// <summary>The Familiar: one, an obstacle with no property, removed at the end of the round.</summary>
        public static TokenDefinition Familiar { get; } = new TokenDefinition(
            FamiliarId, 1, "Familiar", FamiliarSupplyCount,
            Array.Empty<TokenPropertyDefinition>(), TokenRemovalCondition.EndOfRoundCleanup);

        public static IEnumerable<TokenDefinition> Tokens() => new[] { Familiar };

        /// <summary>Whether Gydion's Familiar is on the board - the question that decides "move it" or "take one".</summary>
        public static EffectCapabilityReference FamiliarPresentV1 => Reference("gydion-familiar-present");

        /// <summary>The space Gydion's Familiar stands on.</summary>
        public static EffectCapabilityReference FamiliarSpaceV1 => Reference("gydion-familiar-space");

        /// <summary>"Place a Familiar token in radius" from the supply - the card's printed Radius with items.</summary>
        public static EffectCapabilityReference PlaceFamiliarInRadiusV1 => Reference("gydion-place-familiar-in-radius");

        /// <summary>The Familiar's three capabilities, over the shared owned-token and hero-token handlers.</summary>
        public static IEnumerable<IEffectCapabilityHandler> FamiliarHandlers() =>
            new[]
            {
                SyntheticOwnedTokenCapabilities.PresenceHandler(FamiliarPresentV1, Familiar),
                SyntheticOwnedTokenCapabilities.SpaceHandler(FamiliarSpaceV1, Familiar),
                SyntheticTokenCapabilities.PlaceHandler(PlaceFamiliarInRadiusV1, Familiar, PrintedValueChannel.Radius)
            };

        /// <summary>
        /// The twenty-two naming steps and the Familiar's three, registered with the dealing (9.30i) by
        /// <c>MatchContentCatalogs</c>. The engine's spellbook contracts
        /// (<see cref="SyntheticSpellbookCapabilities.RegisteredHandlers"/>) and Dimension Door's Place
        /// (<see cref="SyntheticPlacementCapabilities.RegisteredHandlers"/>) register from their own
        /// files.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            GydionCardContent.SpellInBookHandlers().Concat(FamiliarHandlers()).ToArray();

        private static EffectCapabilityReference Reference(string id) =>
            new EffectCapabilityReference(new EffectCapabilityId(id), 1);
    }
}
