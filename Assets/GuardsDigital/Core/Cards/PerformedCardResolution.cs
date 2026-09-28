using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.114e. One completed action RESOLVED its card - p.10's played card becoming resolved -
    /// whatever the card's classification. Transient, not persisted.
    ///
    /// <para>Wuk's March of Nature, "Each time after you resolve a card, you may place a Tree token in
    /// radius", is the first printed sentence to ask it of every card. <see cref="PerformedBasicAction"/>
    /// carries the same resolution flag but only for p.9's Gold and Silver cards, which is why Swift's
    /// "after you resolve a basic card" reads that fact and this one exists beside it rather than
    /// widening it: a Tier IV Ultimate is unlocked when most of its controller's cards are coloured.
    /// It is produced at the one site every completed action passes through, from the same invocation
    /// classification, so the two readings of "resolved" cannot drift.</para>
    /// </summary>
    public readonly struct PerformedCardResolution
    {
        private PerformedCardResolution(HeroId actor, CardInstanceId source, ActionDefinitionId action)
        {
            ActorId = actor;
            SourceCardInstanceId = source;
            ActionDefinitionId = action;
        }

        public HeroId ActorId { get; }
        public CardInstanceId SourceCardInstanceId { get; }
        public ActionDefinitionId ActionDefinitionId { get; }
        public bool IsDefined => ActorId.IsDefined;

        public static PerformedCardResolution ForCompletedAction(
            HeroId actor, CardInstanceId source, ActionDefinitionId action, bool wasCardResolution) =>
            wasCardResolution && actor.IsDefined && source.IsDefined && action.IsDefined
                ? new PerformedCardResolution(actor, source, action)
                : default;
    }
}
