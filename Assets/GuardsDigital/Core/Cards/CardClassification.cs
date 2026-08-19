namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Whether a definition is one of the hero's Basic cards, one of its upgradeable ones, or its
    /// Ultimate.
    ///
    /// The distinction is a rule rather than a label. Rulebook p.15 gives the Basic pair three separate
    /// consequences - they "are never upgraded, have no Tier, and are not considered when checking your
    /// lowest card Tier" - and p.15's Upgrade a Card step opens with "Choose a non-basic Action card in
    /// your hand with the lowest card Tier", so the classification decides eligibility for both the
    /// upgrade choice and the lowest-Tier reading that later Life and leveling rules take.
    ///
    /// <see cref="Ultimate"/> is milestone 7.3's, and it is a third value rather than a flavour of
    /// <see cref="NonBasic"/> because p.16 excludes the Ultimate from both of those readings for a
    /// reason of its own. It is not upgraded - "instead of upgrading a card and gaining an item, you
    /// unlock the Ultimate card" - and it is never in the hand p.15 chooses from, since the page places
    /// it "to the left side of your hero dashboard". Classifying it <see cref="NonBasic"/> would make it
    /// an answer to "the lowest non-Basic Tier" and a legal upgrade choice, both of which the source
    /// contradicts.
    ///
    /// <see cref="None"/> means the definition states no classification, which
    /// <see cref="CardProgressionProfile.NotStated"/> is the only way to declare.
    /// </summary>
    public enum CardClassification
    {
        None = 0,
        Basic = 1,
        NonBasic = 2,

        /// <summary>p.16's Ultimate card: unlocked instead of upgraded, and never in hand or deck once it is.</summary>
        Ultimate = 3
    }
}
