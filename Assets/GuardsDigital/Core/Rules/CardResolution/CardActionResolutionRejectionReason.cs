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
        IllegalCorrectionChoice = 20
    }
}
