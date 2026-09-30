namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// Milestone 9.22, user ruling W9 (superseding W6): a Move that can say, without performing
    /// anything, that it would move its subject 0 spaces because the subject already stands where
    /// the sentence sends it - Get over here!'s target already beside Bain, Grappling Bolt's obstacle
    /// Cutter already stands beside.
    ///
    /// <para><b>"A Movement of 0 spaces is still a move."</b> Such a subject is a legal target, so a
    /// selection naming this step as its consumer keeps it, and an offer walk reading this step
    /// finds a legal play rather than an empty one: the card is greyed only when no legal target
    /// exists at all (rule 14). Nothing changes at runtime - the Move has no destination to walk to
    /// and its own failure policy decides that step exactly as before, so the card resolves.</para>
    ///
    /// <para>It is not W8's <see cref="IEffectInertDisplacementCapabilityHandler"/>. That push is
    /// judged with the card whole and the card is greyed when nothing else changes anything; W9
    /// keeps the play offered on the target alone.</para>
    /// </summary>
    public interface IEffectZeroSpaceMoveCapabilityHandler
    {
        bool WouldMoveZeroSpaces(EffectCapabilityContext context);
    }
}
