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
        AttackResultComparison = 15,

        /// <summary>
        /// Milestone 8.19c. Rulebook p.8's end of round, at the moment before its Minion Battle is
        /// planned - the position Xargatha's Constrict prints in its own reminder text, "( Before the
        /// end of round minion battle. )".
        ///
        /// It is a checkpoint rather than a seventh end-of-round stage because p.8 lists the stages a
        /// round runs and this is not one of them: it is a moment inside the first, before that stage
        /// has counted anything. A stage would also have had to appear in every completed round's
        /// record and in the persisted cursor, which would make a card's trigger window part of the
        /// shape of a round.
        /// </summary>
        EndOfRoundBeforeMinionBattle = 16,

        /// <summary>
        /// Milestone 8.19c. Immediately after one p.18 Push has applied, naming the pushed object.
        /// Sabina's Big Sodding Gun is the verified text that reads it - "If you push an enemy hero,
        /// that hero discards a card, or is defeated" - and the page's "if you push" is the applied
        /// displacement rather than the attempt, so a Push that moved nothing reaches no trigger.
        /// </summary>
        ObjectPushApplied = 17
    }
}
