namespace GuardsDigital.Core.Timing
{
    public enum TimingCheckpoint
    {
        None = 0,
        CardsPlayedAndRevealed = 1,
        TurnStarted = 2,
        ActionStarted = 3,
        BeforeAttack = 4,
        AfterAttack = 5,
        ActionEnded = 6,
        TurnOnlyCancellationBeforeTurnEnded = 7,
        TurnEnded = 8,
        RoundEnd = 9,
        CardsRetrieved = 10,
        SourceCardStateChanged = 11,
        AffectedHeroDefeated = 12,
        ExplicitCancellation = 13
    }
}
