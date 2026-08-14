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

        public static MarkerDefinitionId Focus { get; } = new MarkerDefinitionId("synthetic-focus-marker");

        public static TokenDefinitionReference HazardV1 => new TokenDefinitionReference(Hazard, 1);

        public static MarkerDefinitionReference FocusV1 => new MarkerDefinitionReference(Focus, 1);

        /// <summary>A two-token supply, so a third placement must reclaim before it can place.</summary>
        public static TokenDefinition HazardDefinition { get; } =
            new TokenDefinition(Hazard, 1, "TEST - Hazard", 2);

        /// <summary>A one-marker supply, so a second give has nothing left to take.</summary>
        public static MarkerDefinition FocusDefinition { get; } =
            new MarkerDefinition(Focus, 1, "TEST - Focus", 1, MarkerReturnCondition.GeneralCleanup);

        public static ComponentDefinitionCatalog CreateCatalog() =>
            new ComponentDefinitionCatalog(
                new[] { HazardDefinition },
                new[] { FocusDefinition });
    }
}
