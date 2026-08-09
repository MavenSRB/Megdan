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
        DefenseResponseChoiceRequested = 8
    }
}
