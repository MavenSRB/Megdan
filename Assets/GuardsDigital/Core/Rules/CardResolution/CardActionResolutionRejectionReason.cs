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
        ActorUnavailable = 9
    }
}
