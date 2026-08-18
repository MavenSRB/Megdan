namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Whether a definition is one of the hero's Basic cards or one of its upgradeable ones.
    ///
    /// The distinction is a rule rather than a label. Rulebook p.15 gives the Basic pair three separate
    /// consequences - they "are never upgraded, have no Tier, and are not considered when checking your
    /// lowest card Tier" - and p.15's Upgrade a Card step opens with "Choose a non-basic Action card in
    /// your hand with the lowest card Tier", so the classification decides eligibility for both the
    /// upgrade choice and the lowest-Tier reading that later Life and leveling rules take.
    ///
    /// <see cref="None"/> means the definition states no classification, which
    /// <see cref="CardProgressionProfile.NotStated"/> is the only way to declare. Ultimate is absent for
    /// the reason <see cref="CardTier"/> stops at III: milestone 7.3 owns it.
    /// </summary>
    public enum CardClassification
    {
        None = 0,
        Basic = 1,
        NonBasic = 2
    }
}
