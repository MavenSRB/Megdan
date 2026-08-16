using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Components.Synthetic
{
    /// <summary>
    /// The bounded synthetic component content milestone 6.2 executes its token and marker operations
    /// against.
    ///
    /// It lives in production Core rather than a test assembly for the same reason
    /// <c>PlaceholderMovementHand</c> and <c>SyntheticEffectCapabilities</c> do: the configured session
    /// composes it, so it must be reachable from the composition root.
    ///
    /// The supplies are deliberately tiny. Rulebook p.18's reclamation rule only exists because a supply
    /// can run out, so a fixture has to be able to exhaust one without placing a realistic number of
    /// tokens; two tokens and one marker make both exhaustion paths reachable in a handful of commands.
    /// The counts imply nothing about verified hero content, which arrives in Phase 8.
    /// </summary>
    public static class SyntheticComponentDefinitions
    {
        public static TokenDefinitionId Hazard { get; } = new TokenDefinitionId("synthetic-hazard-token");

        public static TokenDefinitionId Ward { get; } = new TokenDefinitionId("synthetic-ward-token");

        public static MarkerDefinitionId Focus { get; } = new MarkerDefinitionId("synthetic-focus-marker");

        public static TokenDefinitionReference HazardV3 => new TokenDefinitionReference(Hazard, 3);

        public static TokenDefinitionReference WardV1 => new TokenDefinitionReference(Ward, 1);

        public static MarkerDefinitionReference FocusV1 => new MarkerDefinitionReference(Focus, 1);

        /// <summary>
        /// A two-token supply, so a third placement must reclaim before it can place.
        ///
        /// Revision 3 is milestone 6.8: the property declared at 6.3 gains the typed rule it supplies.
        /// Rulebook p.19 keeps a token's properties outside the Active-effect rules ("this does not
        /// include Active effects"), so the modifier below lives and dies with the placed instance and
        /// reaches the token's controller for exactly as long as the token is on the board.
        /// </summary>
        public static TokenDefinition HazardDefinition { get; } =
            new TokenDefinition(
                Hazard,
                3,
                "TEST - Hazard",
                2,
                new[]
                {
                    new TokenPropertyDefinition(
                        "persistent-hazard",
                        "synthetic-hazard",
                        "while-token-remains-placed",
                        RuleModification.ForValue(PrintedValueChannel.Defense, 1))
                });

        /// <summary>
        /// A second token kind, added by milestone 6.6 so p.19's Replace has "a different object" to
        /// substitute in: a Replace between two instances of one definition would change nothing the
        /// rules can see, and the shared operation refuses it for exactly that reason.
        ///
        /// It declares no property, which is the point rather than an omission. p.19 ties a token's
        /// properties to the instance - "once a token is placed, it retains all its properties until
        /// that token is removed" - so replacing a Hazard with a Ward is observably a loss of the
        /// Hazard's continuous source, and that is what a no-transfer replacement means. Its supply is
        /// one, so a second Replace has nothing left to bring in.
        /// </summary>
        public static TokenDefinition WardDefinition { get; } =
            new TokenDefinition(
                Ward,
                1,
                "TEST - Ward",
                1,
                Array.Empty<TokenPropertyDefinition>());

        /// <summary>A one-marker supply, so a second give has nothing left to take.</summary>
        public static MarkerDefinition FocusDefinition { get; } =
            new MarkerDefinition(Focus, 1, "TEST - Focus", 1, MarkerReturnCondition.GeneralCleanup);

        public static ComponentDefinitionCatalog CreateCatalog() =>
            new ComponentDefinitionCatalog(
                new[] { HazardDefinition, WardDefinition },
                new[] { FocusDefinition });
    }
}
