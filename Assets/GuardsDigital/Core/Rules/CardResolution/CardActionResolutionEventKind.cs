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
        SkillActionCompleted = 30,
        ClearActionStarted = 31,
        TokenCleared = 32,
        ClearActionCompleted = 33,
        DisplacedMinionCorrectionStarted = 34,
        DisplacedMinionCorrectionChoiceRequested = 35,
        DisplacedMinionCorrectionMoved = 36,
        DisplacedMinionCorrectionPlaced = 37,
        DisplacedMinionCorrectionLeftInPlace = 38,
        DisplacedMinionCorrectionCompleted = 39,
        MinionRemovalPrevented = 40,

        /// <summary>Milestone 6.12: rulebook p.13 returned one marker a defeated hero held.</summary>
        MarkerReturnedOnHeroDefeat = 41,

        /// <summary>Milestone 7.4: p.13's "Remove the defeated hero from the board" was attempted.</summary>
        HeroRemovalAttempted = 42,

        /// <summary>Milestone 7.4: the defeated hero left the board.</summary>
        HeroRemovalCompleted = 43,

        /// <summary>Milestone 7.4: the removal was stopped and the defeated hero is still standing.</summary>
        HeroRemovalPrevented = 44,

        /// <summary>
        /// Milestone 7.4: p.13's "If the defeated hero had an unresolved card, it is resolved, without
        /// performing an action."
        /// </summary>
        HeroUnresolvedCardResolved = 45,

        /// <summary>Milestone 7.4: p.13's Defeat coins and assist coins were collected.</summary>
        HeroDefeatRewardAwarded = 46,

        /// <summary>Milestone 7.4: the defeated hero's team spent p.13's Life counters.</summary>
        TeamLifeSpent = 47,

        /// <summary>Milestone 7.4: p.13's last counter was spent, so that team loses the game.</summary>
        TeamEliminated = 48,

        /// <summary>
        /// Milestone 8.5: one node of a printed Attack's or Defense's card text was applied at the
        /// moment rulebook p.14 numbers it. The fact names the card, the action, the hero whose text it
        /// is, and which of the page's three moments this was, so a reader can place it in the order
        /// without inferring anything from position.
        /// </summary>
        CardTextApplied = 49
    }
}
