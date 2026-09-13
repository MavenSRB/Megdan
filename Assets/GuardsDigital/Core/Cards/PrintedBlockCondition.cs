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
    /// <para><b>Why the arms are stated rather than one boolean.</b> The same reason
    /// <see cref="PrintedDefenseResolution"/> is an enum with a <c>None</c>: a definition that
    /// declares nothing must be distinguishable from one that declares the negative arm, or a Block
    /// whose author forgot the condition would silently mean "blocks non-ranged attacks". A Block is
    /// required to name one of them; nothing else may name any.</para>
    ///
    /// <para><b>Milestone 8.40g, and what the third member changed about this type.</b> The two
    /// members above are both about the attacking card, which is why the remarks under each of them
    /// say the answer is read "off nothing the defender holds". Bain the Bounty Hunter's three Green
    /// Blocks print a condition about the BOARD instead, so what this enum names is now "the
    /// condition this card prints" in the general sense rather than "which of p.13's two Attack
    /// classifications this card stops". Which facts each member is answered from is the member's own
    /// business and <c>DefenseBlockQuery</c> is the one place that decides it.</para>
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
        AttackIsNotRanged = 2,

        /// <summary>
        /// Milestone 8.40g. `If a hero in play has a [bounty] bounty marker, block the attack` - Bain
        /// the Bounty Hunter's Close Call, Narrow Escape and Perfect Getaway, and the first Block
        /// condition in this build that is a fact about the BOARD rather than about the attack.
        ///
        /// <para><b>Why a third member here rather than a second axis beside this one.</b> His three
        /// faces print no ranged or non-ranged clause at all, so the condition is unconditional in the
        /// attack and conditional in the board, and a reader may fairly ask whether one word is
        /// stating two things. It is stating one: a card that prints no classification clause is not
        /// silently asserting one, it is asking a different question, and this enum names the question
        /// the card asks. The alternative was measured rather than argued. A second axis needs
        /// somewhere to live, and the only place is a new public property on
        /// <see cref="ActionDefinition"/> - which milestone 8.38d measured as the first change ever to
        /// move all twelve corpus digests at once, because the identity walker digests every public
        /// instance property of every object it reaches. A member on an enum an existing property
        /// already carries moves nothing, because no card written before this milestone names it. The
        /// cross product the axis would buy - "block a ranged attack, if the board says so" - is a
        /// sentence no face in this build prints, and building the vocabulary for it before a card
        /// needs it is exactly what milestone 8.3 refused when it declared the Block's shape and left
        /// the condition to the face that first printed one.</para>
        ///
        /// <para><b>Whose marker, and which hero.</b> "A hero in play" is any hero on the board, the
        /// defending hero included (`BA-D5`), and "a bounty marker" is a marker the DEFENDER's own
        /// content put out - read off <c>MarkerAttachment.OwnerHeroId</c> through
        /// <c>HeldMarkerQuery</c>, the single reading milestone 8.40c's predicate and its two
        /// selection narrowings already share. A Defense action's actor is the defender, so this
        /// member asks about "the actor's marker" exactly as those two do rather than about a fourth
        /// thing that could come to disagree with them.</para>
        /// </summary>
        AHeroInPlayHoldsTheActorsMarker = 3
    }
}
