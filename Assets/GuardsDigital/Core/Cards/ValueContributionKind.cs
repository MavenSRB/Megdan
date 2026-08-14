namespace GuardsDigital.Core.Cards
{
    public enum ValueContributionKind
    {
        None = 0,
        PrintedIcon = 1,
        MinionDefenseModifier = 2,
        ItemModifier = 3,
        CardTextModifier = 4,
        ActiveEffectModifier = 5,

        /// <summary>
        /// A persistent source that is not an Active effect - a token-owned property today, an Ultimate or
        /// a marker later. Rulebook p.19 keeps the two apart ("this does not include Active effects"), so
        /// their contributions stay distinguishable in a value total rather than merging into one number.
        /// </summary>
        ContinuousSourceModifier = 6
    }
}
