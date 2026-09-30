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
        CardTextApplied = 49,

        /// <summary>
        /// Milestone 8.19c1: a Defeat reached its removal stage and a trusted source offered to stop
        /// the removal at a price, so the game is waiting for the hero controlling it to answer.
        ///
        /// It is a kind of its own rather than a flag on <see cref="MinionRemovalAttempted"/> because
        /// the two say different things: that fact says the removal was tried, and this one says it has
        /// not been tried yet and a decision is standing in front of it.
        /// </summary>
        RemovalPreventionRequested = 50,

        /// <summary>
        /// Milestone 8.19c1: the hero paid a prevention source's price. The card it discarded is the
        /// fact; what the payment bought is reported by the Defeat's own
        /// <see cref="MinionRemovalPrevented"/>, exactly as it is when p.18's immunity stops one.
        /// </summary>
        RemovalPreventionPaid = 51,

        /// <summary>
        /// Milestone 8.40f: a node of card text turned one card face up for the whole table - the
        /// "then reveal the card" half of Bain's three guessing Skills.
        ///
        /// <para>It is a kind of its own rather than the commitment flow's <c>CardRevealed</c>
        /// because the two say different things about where the card is. p.7's reveal turns over the
        /// card a hero committed to this turn, and every consumer of that fact reads it that way -
        /// the HUD's flip cue borrows the hero's CURRENT CARD SLOT for it, and the channel's own
        /// one-for-one ledger asserts exactly as many revealed rows as commitment reveals occurred.
        /// A card turned over out of a HAND mid-resolution is in neither place, so inheriting those
        /// two rules would animate the wrong card and falsify a declared ledger.</para>
        ///
        /// <para>The fact names the card's OWNER through <c>TargetHeroId</c> - the hero whose hand it
        /// came from, never the acting hero whose text asked for it - and the card itself through
        /// <c>RevealedCardInstanceId</c>, because <c>SourceCardInstanceId</c> is already spoken for
        /// by the card the text is printed on.</para>
        /// </summary>
        CardTextRevealedCard = 52,

        /// <summary>
        /// Milestone 8.61e: a hero moved through a token whose own text says it goes off, and the
        /// removal turned it face up for the whole table - Min the Dragonmonk's mines, "After an enemy
        /// hero moves through a mine, remove it".
        ///
        /// <para>It is a kind of its own rather than <see cref="TokenCleared"/>, which is the other
        /// event that names a removed token, because the two are different facts with different
        /// subjects. p.13's Clear is an ACTION a hero performs in place of an Attack, so its row says
        /// which hero cleared and off which card; this is something that happened TO the hero who
        /// walked, off no card of theirs at all, and its whole point is the kind it revealed. Sharing
        /// the kind would have made every mine going off look like somebody spending their Attack.</para>
        ///
        /// <para>The fact names the crossing hero through <c>TargetHeroId</c> - the hero the mine went
        /// off under, never the acting hero, who on a push is the one who did the pushing - the space
        /// through the token coordinate, and what it cost through
        /// <c>MoveThroughTrapCardsOwed</c>, which is the blast-or-dud the removal made public.</para>
        /// </summary>
        MoveThroughTrapSprung = 53,

        /// <summary>
        /// Milestone 9.19. Card text sent a card from a hand to its owner's discard pile. Written
        /// beside the node's applied fact at every altitude that publishes a reveal, on the same
        /// terms as <see cref="CardTextRevealedCard"/>: the card's owner rides the target field and
        /// the card itself the discarded-card field, and the seat-public log derives its row from it.
        /// </summary>
        CardTextDiscardedCard = 54
    }
}
