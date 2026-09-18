using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>One actual Movement action completed, including zero distance. Transient, not persisted.</summary>
    public readonly struct PerformedMovementAction
    {
        private PerformedMovementAction(HeroId actor, CardInstanceId source, ActionDefinitionId action)
        { ActorId = actor; SourceCardInstanceId = source; ActionDefinitionId = action; }

        public HeroId ActorId { get; }
        public CardInstanceId SourceCardInstanceId { get; }
        public ActionDefinitionId ActionDefinitionId { get; }
        public bool IsDefined => ActorId.IsDefined;

        public static PerformedMovementAction ForCompletedAction(HeroId actor, CardInstanceId source,
            ActionDefinitionId action, ActionKind kind) => kind == ActionKind.Movement && actor.IsDefined &&
                source.IsDefined && action.IsDefined ? new PerformedMovementAction(actor, source, action) : default;
    }
}
