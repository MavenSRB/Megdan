namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// The condition a printed Block card states, as the card states it.
    ///
    /// <para><b>Milestone 8.23, and the reason it exists is that a hard-coded reading finally met a
    /// card that contradicts it.</b> Rulebook p.14 resolves a Block by its own condition - "If the
    /// Block condition is fulfilled, you successfully defend, regardless of the attack value. If not,
    /// you are defeated" - and says nothing about what any particular condition is. Every Block in
    /// verified content until now printed the same one, so milestone 8.11 read it off the attacking
    /// action directly: Wasp's Wing Shield, Deflect and Parasol all print "Block a ranged attack".
    /// Tigerclaw's five Blue cards print both halves of that sentence - Dodge, Sidestep and Evade
    /// block a ranged attack, while Parry and Riposte block a <em>non-ranged</em> attack - so the
    /// condition became a fact about the card rather than a fact about Blocks.</para>
    ///
    /// <para><b>Why it is declared and not derived.</b> There is nothing else on a Block card to
    /// derive it from. A Block prints no Defense number, and its own <see cref="AttackRangeKind"/> is
    /// <see cref="AttackRangeKind.None"/> because a Defense action is not an Attack and has no reach
    /// of its own; reusing that member to mean "the attacks I stop" would give one field two
    /// unrelated meanings on the same definition. The card prints the condition in words, so the
    /// definition carries it in a member, and <c>DefenseBlockQuery</c> asks that member instead of
    /// asking the attack a question the card never posed.</para>
    ///
    /// <para><b>Why the two members are stated rather than one boolean.</b> The same reason
    /// <see cref="PrintedDefenseResolution"/> is an enum with a <c>None</c>: a definition that
    /// declares nothing must be distinguishable from one that declares the negative arm, or a Block
    /// whose author forgot the condition would silently mean "blocks non-ranged attacks". A Block is
    /// required to name one of the two; nothing else may name either.</para>
    /// </summary>
    public enum PrintedBlockCondition
    {
        /// <summary>Not a Block - p.14's numeric Defense, or no Defense action at all.</summary>
        None = 0,

        /// <summary>
        /// `Block a ranged attack`. Wasp's three Green Blocks and Tigerclaw's Dodge, Sidestep and
        /// Evade. p.13 classifies the attacking card, so the answer is read off the attack's own
        /// printed classification and off nothing the defender holds.
        /// </summary>
        AttackIsRanged = 1,

        /// <summary>
        /// `Block a non-ranged attack`. Tigerclaw's Parry and Riposte, and the first verified content
        /// to print this arm.
        ///
        /// <para>It is the complement of <see cref="AttackIsRanged"/> over p.13's two Attack
        /// classifications and not merely "not ranged": an incoming action that is not classified as
        /// an Attack at all never reaches a Block, because p.14's Defense window opens inside an
        /// Attack. The query states the complement explicitly so that an unclassified attack - which
        /// no printed content produces - fulfils neither condition rather than fulfilling this one by
        /// falling through.</para>
        /// </summary>
        AttackIsNotRanged = 2
    }
}
