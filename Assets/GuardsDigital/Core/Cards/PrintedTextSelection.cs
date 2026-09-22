namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.75b. What a printed face says about WHICH of its printed texts applies.
    ///
    /// <para><b>Why a declaration on the definition rather than a reading of the card.</b> Ignatia the
    /// Mad is the first hero whose action cards print TWO texts - a sun (☀) line and a moon (☾) line -
    /// with the live face of the Tie Breaker coin selecting which one is in force. A player looking at
    /// the card has to be told which half of it is live right now, and presentation may not work that
    /// out for itself: it would have to recognise a hero, a card name, a capability id or the printed
    /// words, and every one of those is a branch this codebase refuses (AGENTS.md's "no card-name /
    /// hero-name branches, printed-text parsing, or UI-owned resolution", policed by
    /// <c>VerifiedContentIsolationConformanceTests</c>). So the CONTENT states the fact and the HUD
    /// reads the statement - exactly the rule
    /// <see cref="Rules.EffectResolution.EffectChoiceValueVocabulary"/> was written under.</para>
    ///
    /// <para><b>It is inert to the rules.</b> Nothing in the resolver branches on this: the coin branch
    /// itself is an ordinary <c>Conditional</c> over a predicate capability, exactly as it would be
    /// without this type. This is the same fact said in a form a surface can read, which is why adding
    /// it moves no schema - a definition is content every seat reconstructs from its own catalogs, and
    /// no snapshot or wire frame carries one.</para>
    /// </summary>
    public enum PrintedTextSelection
    {
        /// <summary>
        /// The face prints one text, or prints none. Every card of the twenty-one heroes dealt before
        /// milestone 8.75 carries this, and a surface draws nothing extra for it.
        /// </summary>
        None = 0,

        /// <summary>
        /// The face prints two texts and the live Tie Breaker coin face selects which one applies -
        /// "Apply the text matching the symbol on the Tie Breaker coin."
        /// </summary>
        TieBreakerCoin = 1,

        /// <summary>
        /// The face's own text FREES its controller from the coin while what it creates stands -
        /// Ignatia's Equilibrium, "This round: Each time you perform or repeat a primary action you may
        /// apply either ☀ or ☾ card text, regardless of the Tie Breaker coin."
        ///
        /// <para>It is a second member of this enum rather than a separate reading of the live Active
        /// effect for one measured reason: a seat's projection carries
        /// <c>IMatchReadModel.ActiveEffectSourceCardInstanceIds</c> - which CARDS are sourcing a live
        /// Active effect - and nothing else about an effect instance. Given this declaration, "may this
        /// hero choose the face right now" is answerable from that list plus the card's owner and its
        /// definition, with no capability catalog to thread through every card surface and without
        /// presentation ever naming a hero's content. See
        /// <see cref="Rules.CardResolution.CoinSelectedTextQuery.MayChooseTheFace"/>.</para>
        /// </summary>
        FreesTheCoinChoice = 2
    }

    /// <summary>
    /// Milestone 8.75b. One of the two symbols the Tie Breaker coin's faces bear, as a printed face's
    /// two texts name them.
    ///
    /// <para><b>The ordinals are load-bearing.</b> They are the integer candidates the free-choice pause
    /// offers, under <see cref="Rules.EffectResolution.EffectChoiceValueVocabulary.PrintedTextFace"/> -
    /// the same arrangement <see cref="CardColor"/> has under
    /// <see cref="Rules.EffectResolution.EffectChoiceValueVocabulary.CardColor"/>, where
    /// <see cref="CardColor.None"/> takes 0 and every offerable member takes a positive number. So a
    /// candidate is never the default integer, which is the trap milestone 8.71's brief names ("an
    /// unbound optional Integer reads as zero").</para>
    ///
    /// <para>The engine models the coin only as a <c>TeamSide</c>
    /// (<c>Setup.TieBreakerState.RevealedSide</c>); the mapping from a side to the symbol printed on
    /// that side lives in exactly one place,
    /// <see cref="Rules.CardResolution.CoinSelectedTextQuery"/>, so the content that branches on it and
    /// the HUD that draws it can never drift apart.</para>
    /// </summary>
    public enum PrintedTextFace
    {
        /// <summary>No face: the coin is not revealed, so neither printed text is live.</summary>
        None = 0,

        /// <summary>The sun (☀) face.</summary>
        Sun = 1,

        /// <summary>The moon (☾) face.</summary>
        Moon = 2
    }
}
