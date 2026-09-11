namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// The narrowing a permanent ability puts on the targets of the actions IT grants, as a closed
    /// vocabulary. Milestone 8.38d.
    ///
    /// <para>Swift's Bullet Time is what prints one: "After you resolve a basic card you may perform
    /// the primary action on that card; <b>you cannot target the same enemy hero twice in the same turn
    /// this way</b>." The second half is not a rule about her, about the borrowed card, or about
    /// attacking - it is a rule about what may be reached THROUGH THIS GRANT, which is why it is
    /// declared on the granting ability rather than on any card the grant reaches.</para>
    ///
    /// <para><b>"this way" is the whole of why this is a declaration and not a restriction.</b>
    /// <see cref="GuardsDigital.Core.Rules.Modifications.RestrictionQuery"/> answers "may this actor do
    /// this to this object", and an answer there would bar Swift's ordinary Attack on the same enemy in
    /// the same turn - which the card does not say. The exclusion counts only the instances the grant
    /// itself produced, so its window is the grant, and its clock is the turn.</para>
    /// </summary>
    public enum GrantedTargetExclusion
    {
        /// <summary>What every granting ability in this build but Bullet Time declares.</summary>
        None = 0,

        /// <summary>
        /// One enemy hero may be targeted at most once per turn by the actions this ability grants.
        /// Minions are untouched - the clause names the enemy HERO - and so is every action the hero
        /// performs by any other route in the same turn.
        /// </summary>
        SameEnemyHeroOncePerTurn = 1
    }
}
