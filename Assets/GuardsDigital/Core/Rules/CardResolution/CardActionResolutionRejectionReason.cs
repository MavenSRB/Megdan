namespace GuardsDigital.Core.Rules.CardResolution
{
    public enum CardActionResolutionRejectionReason
    {
        None = 0,
        ResolutionAlreadyPending = 1,
        NoPendingResolution = 2,
        StaleChoice = 3,
        InvalidChoice = 4,
        UnavailableOffer = 5,
        UnsupportedActionKind = 6,
        UnsupportedInvocationContext = 7,
        MovementStepRejected = 8,
        ActorUnavailable = 9,
        UnsupportedAttackRange = 10,
        TargetUnavailable = 11,
        MovementActionPrevented = 12,
        FastTravelDestinationUnavailable = 13,
        FastTravelZoneNotEligible = 14,
        FastTravelBlockedByEnemyInOriginZone = 15,
        FastTravelBlockedByEnemyInDestinationZone = 16,
        ClearTokenSelectionUnavailable = 17,
        NoPendingCorrection = 18,
        StaleCorrectionChoice = 19,
        IllegalCorrectionChoice = 20,

        /// <summary>
        /// Milestone 7.1a: the answer was submitted for a hero whose decision this is not. Distinct
        /// from <see cref="InvalidChoice"/> because the choice itself was legal and still stands; what
        /// was wrong is who sent it.
        /// </summary>
        WrongChooser = 21,

        /// <summary>
        /// Milestone 7.4: the successful Attack defeated a hero, and the state rulebook p.13's
        /// consequences read - the defeated hero's Level, and its team's Life counters - is not bound to
        /// this dispatcher. Distinct from <see cref="InvalidChoice"/> because nothing about the Attack
        /// or its target is wrong: the composition cannot price the Defeat, so it refuses the outcome
        /// rather than recording one whose coins and counters it made up. The refusal happens before
        /// anything is removed, resolved, cancelled or spent.
        /// </summary>
        DefeatConsequenceStateUnavailable = 22
    }
}
