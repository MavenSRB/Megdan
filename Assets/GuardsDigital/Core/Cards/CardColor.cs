namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// A card's printed colour, as a typed rules-facing characteristic.
    ///
    /// The first five members are the five every printed HAND card uses. Page 7 states the relationship
    /// that fixes the set - "Primary Action Icons (Always match the card's color)" over the five primary
    /// icons - and page 15 names two of them as the Basic pair: "Your Basic cards (Gold and Silver), are
    /// never upgraded, have no Tier". Page 15 also states the count from the other side: after leveling
    /// "You should now have exactly 5 cards, 1 of each color, in your hand", and page 6's setup deals
    /// that hand as "the three Tier I cards and the two basic cards with no Tier".
    ///
    /// <see cref="Red"/> and <see cref="Blue"/> are card colours and have nothing to do with
    /// <c>TeamSide.Red</c> and <c>TeamSide.Blue</c>. Nothing may convert between the two types, and the
    /// synthetic content deliberately gives each team a card of each colour so that no derivation from
    /// team identity could reproduce the assignment. A colour is likewise never inferred from artwork,
    /// tint, display name, hero identity, printed action icon, hand position, or UI grouping: every
    /// definition states its own through <see cref="CardProgressionProfile"/>, which
    /// <see cref="CardDefinition"/> refuses to be constructed without.
    ///
    /// <see cref="None"/> is not a seventh colour and no definition may declare it as one. It exists so
    /// that a definition with no printed colour has a value to carry, and the profile is what makes
    /// that absence explicit rather than defaulted. Two definitions carry it, for different reasons:
    /// <see cref="CardProgressionProfile.NotStated"/> states no progression content at all, and
    /// milestone 7.3's <see cref="CardProgressionProfile.UltimateCard"/> states progression without a
    /// colour.
    ///
    /// <para><b><see cref="Purple"/>, added at milestone 8.37, and why the type held at
    /// five members for eight heroes.</b> Every Ultimate's printed card carries a swatch that is none of
    /// the five hand colours, and no rule in the source read it before Tali: p.15 matches colours among
    /// the cards in a HAND - "the same color as the chosen card", "exactly 5 cards, 1 of each color, in
    /// your hand" - and p.16 keeps the Ultimate out of the hand entirely, so a sixth member would have
    /// been a value no rule asked about, which is the same objection <see cref="CardTier"/> records
    /// against widening itself. RULING 21 is what changes the question: Tali's Commune with Spirits has
    /// a friendly hero name any of p.9's six colours, mandatorily, with no restriction to the five in a
    /// hand - so the naming step needs a value to carry for the sixth. <see cref="Purple"/> is
    /// deliberately INERT rather than wired into anything a hand-colour reads: no definition's
    /// <see cref="CardProgressionProfile"/> may declare it (only the naming vocabulary admits it), and
    /// the borrow contract's own refusal for an Ultimate - it prints no performable primary action -
    /// already makes naming it a step that completes and finds nothing, by construction, before this
    /// member existed to be named at all.</para>
    /// </summary>
    public enum CardColor
    {
        None = 0,
        Gold = 1,
        Silver = 2,
        Red = 3,
        Green = 4,
        Blue = 5,

        /// <summary>
        /// p.9's sixth colour, printed only on an Ultimate card and never on one in a hand. See the type
        /// remarks: nameable since RULING 21, and inert everywhere a hand-colour is read.
        /// </summary>
        Purple = 6
    }
}
