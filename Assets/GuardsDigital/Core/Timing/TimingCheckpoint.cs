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
        ObjectPushApplied = 17,

        /// <summary>
        /// Milestone 8.35z29. The moment a hero's primary action is about to start, before it has
        /// started - Misa's Focus, Discipline and Mastery, "<b>Next turn:</b> Before you perform a
        /// primary action, you may move 1 / up to 2 / up to 3 spaces."
        ///
        /// <para><b>It is the same moment <c>before-action-trigger@1</c> already owns, named so that an
        /// Active effect can declare it.</b> Milestone 8.19a built the window for Arien's Living
        /// Tsunami, whose sentence is nearly Misa's - "Once per turn, before performing an Attack
        /// action, you may move 1 space" - but declared it on an Ultimate, where a
        /// <see cref="GuardsDigital.Core.Cards.BeforeActionTrigger"/> could sit. Misa prints hers on
        /// three ordinary Green cards as p.17 <c>Next turn:</c> effects, so the declaration has to live
        /// where an Active effect can carry it, and an Active effect's own vocabulary for "when does my
        /// triggered effect run" is its trigger checkpoints.</para>
        ///
        /// <para><b>Why "primary" is in the name.</b> Arien's clause names one action kind and Misa's
        /// names none - it says "a primary action", which p.9 makes the action printed in the card's
        /// band, whichever kind that is. So this window opens once before whichever primary the hero
        /// performs, and never before a secondary: p.9's Movement, Defense, Hold, Fast Travel and Clear
        /// are not what the sentence says.</para>
        ///
        /// <para><b>It is not raised as an ordinary lifecycle checkpoint.</b> Nothing calls
        /// <c>ApplyCheckpoint</c> with it; it is read as a declaration by
        /// <see cref="GuardsDigital.Core.Rules.Progression.BeforeActionTriggerQuery"/>, which turns a
        /// standing Active effect that names it into a source on the very machinery Arien's Ultimate
        /// already runs on - including that machinery's spent latch, which is what makes the clause
        /// fire once.</para>
        /// </summary>
        BeforePrimaryAction = 18,

        /// <summary>
        /// Milestone 8.40h. Immediately after a marker has been GIVEN to a hero, naming the hero who
        /// received it. Bain's A Complicated Profession is the verified text that reads it - "After you
        /// give a hero the [bounty] bounty marker that hero discards a card, if able."
        ///
        /// <para><b>It is the give and only the give.</b> Milestone 8.40b built two other ways a marker
        /// changes hands and neither reaches this moment: a TRANSFER moves an attachment that already
        /// stands - Close Call's "that hero gives the marker to you", which is another hero giving - and
        /// a RETURN puts one back in the supply, which is not a give at all. The producer is the giving
        /// capability itself, reporting the hero it gave to, so the exclusion is by construction rather
        /// than by a check on anybody's name. A give that MOVED an already-held copy because the supply
        /// was empty is still a give the actor performed and still reaches here - BA-D1.</para>
        ///
        /// <para><b>It is not raised as an ordinary lifecycle checkpoint</b>, exactly as
        /// <see cref="ObjectPushApplied"/> and <see cref="BeforePrimaryAction"/> are not. Nothing calls
        /// <c>ApplyCheckpoint</c> with it and no Active effect declares it; it is the name a rule source
        /// writes into its own trigger declaration to say which moment it is watching, and the moment is
        /// produced by the handler that performed the give.</para>
        /// </summary>
        MarkerGivenToHero = 19,

        ResolvedCardDiscarded = 20
    }
}
