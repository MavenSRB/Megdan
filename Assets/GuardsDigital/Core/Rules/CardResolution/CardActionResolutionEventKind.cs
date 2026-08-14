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
        DefenseResponseCompleted = 12,
        AttackSuccessfullyDefended = 13,
        AttackSuccessful = 14,
        AttackResolutionCompleted = 15,
        FastTravelActionStarted = 16,
        FastTravelCommitted = 17,
        FastTravelActionCompleted = 18,
        MinionDefeatRecorded = 19,
        MinionRewardAwarded = 20,
        MinionRemovalAttempted = 21,
        MinionRemovalCompleted = 22,
        AttackTargetValidated = 23,
        HeroDefeatRecorded = 24,
        SkillActionStarted = 25,
        SkillEffectNodeApplied = 26,
        SkillEffectNodeDeclined = 27,
        SkillEffectNodeStopped = 28,
        SkillChoiceRequested = 29,
        SkillActionCompleted = 30
    }
}
