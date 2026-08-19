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
        /// <summary>
        /// p.13's hero Defeat, raised naming the defeated hero. Milestone 7.4 gave it its producer and
        /// its controller-based reading; it was called `AffectedHeroDefeated` before that.
        /// </summary>
        ControllerHeroDefeated = 12,
        ExplicitCancellation = 13,
        DefenseResponse = 14,
        AttackResultComparison = 15
    }
}
