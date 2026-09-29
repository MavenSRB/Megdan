namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// Milestone 9.8f. A displacement that can say, without performing anything, that as the board
    /// stands it would move nothing at every distance it admits - a push whose every target has
    /// terrain or the board's edge directly behind it.
    ///
    /// <para><b>It speaks only to whether an ACTION is offered.</b> The user's ruling of 2026-09-29
    /// (Boulder Rush with a Tree against terrain): a piece the push cannot move stays a legal target -
    /// p.18 still calls a push of 0 spaces a push - and the card is judged whole. So the offer walk
    /// reads such a step as taken and applying nothing, and lets the steps after it decide (Mrak's
    /// "you may move ... in the direction of the push"); nothing about which targets are offered, and
    /// nothing at runtime, changes.</para>
    ///
    /// <para>A handler answers true only where nothing downstream could read a consequence of the
    /// 0-space push: a hero stopped by an obstacle is a p.18 fact riders read, so a push with a hero
    /// among its targets answers false.</para>
    /// </summary>
    public interface IEffectInertDisplacementCapabilityHandler
    {
        bool WouldDisplaceNothing(EffectCapabilityContext context);
    }
}
