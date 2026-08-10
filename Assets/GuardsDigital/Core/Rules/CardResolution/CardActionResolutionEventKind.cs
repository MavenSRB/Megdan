namespace GuardsDigital.Core.Rules.CardResolution
{
    public enum CardActionResolutionEventKind
    {
        None = 0,
        MovementActionStarted = 1,
        MovementChoiceRequested = 2,
        MovementStepCommitted = 3,
        MovementActionCompleted = 4,
        AttackTargetChoiceRequested = 5,
        AttackStarted = 6,
        AttackTotalCalculated = 7,
        DefenseResponseChoiceRequested = 8,
        DefenseCardDiscarded = 9,
        DefenseTotalCalculated = 10,
        DefenseDeclined = 11,
        DefenseResponseCompleted = 12
    }
}
