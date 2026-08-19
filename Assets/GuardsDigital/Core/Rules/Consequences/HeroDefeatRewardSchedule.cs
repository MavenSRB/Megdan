using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Progression;

namespace GuardsDigital.Core.Rules.Consequences
{
    /// <summary>
    /// Rulebook p.13's three numbers for a defeated hero, as declared content rather than as literals
    /// inside the combat backend - the same relocation finding `F-ER2-09` asked for on the minion side,
    /// applied to the rule this milestone adds so the hero numbers never live in code to begin with.
    ///
    /// The page states each one as a sentence and then draws the table that summarises all three:
    ///
    /// <list type="bullet">
    /// <item>"When you defeat an enemy hero, collect coins equal to that hero's level."</item>
    /// <item>"Your friendly heroes collect <em>assist coins</em> based on the defeated hero's lowest
    /// card Tier."</item>
    /// <item>"Defeated hero's team spends the number of Life counters, equal to that hero's
    /// <strong>lowest</strong> card Tier."</item>
    /// </list>
    ///
    /// The table's own columns are what fix the second and third: at Levels 1-3 the lowest card Tier is
    /// I and both the assist coins and the Life counters are 1, at 4-6 it is II and both are 2, at 7-8
    /// it is III and both are 3. So the derivation is the Tier's ordinal in both cases, and the table's
    /// Level row is a convenience reading of a hero that has levelled the ordinary way rather than a
    /// second rule - which matters, because a hero's cards are the authority for its lowest Tier and
    /// this milestone reads them rather than the Level column.
    ///
    /// Assist coins and Life counters are deliberately separate members even though they compute the
    /// same number today. They are two sentences about two different resources, and a rules correction
    /// or a later hero that moves one must not silently move the other.
    /// </summary>
    public static class HeroDefeatRewardSchedule
    {
        /// <summary>p.13: "collect coins equal to that hero's level".</summary>
        public static int DefeatCoins(HeroLevel defeatedHeroLevel)
        {
            if (!defeatedHeroLevel.IsDefined)
            {
                throw new ArgumentException(
                    "p.13 prices a hero Defeat by the defeated hero's Level, which must be a legal one.",
                    nameof(defeatedHeroLevel));
            }

            return defeatedHeroLevel.Value;
        }

        /// <summary>
        /// p.13: "Your friendly heroes collect assist coins based on the defeated hero's lowest card
        /// Tier" - one per Tier step, which is what the table's Assist Coins row states against its
        /// Lowest card Tier row.
        /// </summary>
        public static int AssistCoins(CardTier lowestCardTier) => TierOrdinal(lowestCardTier, nameof(lowestCardTier));

        /// <summary>
        /// p.13: "Defeated hero's team spends the number of Life counters, equal to that hero's lowest
        /// card Tier."
        /// </summary>
        public static int LifeCounters(CardTier lowestCardTier) => TierOrdinal(lowestCardTier, nameof(lowestCardTier));

        /// <summary>
        /// The Tier read as the count the page writes it as. <see cref="CardTier.None"/> is refused
        /// rather than read as zero: p.15's Basic pair has no Tier and is "not considered when checking
        /// your lowest card Tier", so a caller that found no non-Basic card has no Tier to convert and
        /// owes its own answer for that case rather than receiving a silent zero here.
        /// </summary>
        private static int TierOrdinal(CardTier tier, string parameterName)
        {
            switch (tier)
            {
                case CardTier.I:
                    return 1;
                case CardTier.II:
                    return 2;
                case CardTier.III:
                    return 3;
                case CardTier.IV:
                    // p.16's Ultimate is never a lowest card Tier: it is unlocked only once every
                    // non-Basic card has reached Tier III, so a hand containing one always contains a
                    // lower Tier. Reaching here would mean the lowest-Tier reading selected it, which is
                    // a defect in the reading rather than a number this table owes.
                    throw new ArgumentOutOfRangeException(
                        parameterName, tier,
                        "p.16's Ultimate cannot be a hero's lowest card Tier.");
                default:
                    throw new ArgumentOutOfRangeException(
                        parameterName, tier,
                        "p.13's assist coins and Life counters are read from a printed Tier.");
            }
        }
    }
}
