using System;
using GuardsDigital.Core.Cards;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// Rulebook p.15's Hero Level table, as declared content rather than as literals inside whatever
    /// runs the purchase - the same shape milestone 7.4 gave p.13's reward table, and for the reason
    /// `F-ER2-09` gave that one: a printed number that lives in code is a number nobody can check
    /// against the page it came from.
    ///
    /// The page prints the table twice over. The row itself reads
    ///
    /// <code>
    /// Hero Level     1   2   3   4   5   6   7   8
    /// Level Up cost  -   1   2   3   4   5   6   7
    /// Max card Tier  -   II  II  II  III III III IV
    /// </code>
    ///
    /// and its own caption says the same thing as a rule: "The first level up costs 1 coin. Each
    /// following level up costs 1 more coin than the previous one." So the cost of leaving a Level is
    /// that Level's own number, which is what <see cref="LevelUpCost"/> answers, and the table's first
    /// column is a dash because no purchase arrives at Level 1 - "All heroes start the game at level 1.
    /// It costs 1 coin to go from level 1 to level 2."
    ///
    /// The cost is priced by the Level being left rather than by the Level being reached, and the page
    /// is explicit that the two readings are not interchangeable: "You cannot skip a level. A level 1
    /// hero needs 3 coins to reach level 3 (1 coin for the first level up, and 2 coins for the second)."
    /// A single purchase of Level 3 for 2 coins would satisfy the table's own column and contradict that
    /// sentence, which is why nothing here prices a jump.
    ///
    /// <see cref="MaxCardTier"/> is the table's third row, and it is deliberately not consulted by the
    /// purchase. p.15's Upgrade a Card step chooses "a non-basic Action card in your hand with the
    /// lowest card Tier or tied for the lowest", and over a hand of p.15's five colours - the Gold and
    /// Silver Basic pair plus three non-Basic cards - that instruction produces this row exactly: three
    /// upgrades take every non-Basic card to Tier II by Level 4, three more take them to Tier III by
    /// Level 7, and p.16's Ultimate is the Tier IV at Level 8. The row is therefore a printed
    /// consequence of the choice rule rather than a second constraint on it, and it is stated here so
    /// that agreement is a fact something can assert rather than an assumption.
    /// </summary>
    public static class HeroLevelSchedule
    {
        /// <summary>
        /// p.15: the coins one level up costs, priced by the Level the hero is leaving.
        ///
        /// The maximum Level is refused rather than priced at zero. p.15's table prints no cost above
        /// Level 8 because there is nothing above it to buy, and p.16 says the same from the other end
        /// ("you cannot gain any additional levels, but can still gain coins"), so a caller that asked
        /// what leaving Level 8 costs has asked about a purchase that does not exist and owes its own
        /// answer rather than receiving a silent zero here.
        /// </summary>
        public static int LevelUpCost(HeroLevel currentLevel)
        {
            if (!currentLevel.IsDefined)
            {
                throw new ArgumentException(
                    "p.15 prices a level up by the Level being left, which must be a legal one.",
                    nameof(currentLevel));
            }

            if (currentLevel.Value >= HeroLevel.Maximum)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentLevel), currentLevel,
                    $"p.15's table ends at Level {HeroLevel.Maximum}, so leaving it has no price.");
            }

            return currentLevel.Value;
        }

        /// <summary>
        /// Whether p.15's table has a row above this Level at all - the structural half of eligibility,
        /// separate from whether the hero can afford it.
        /// </summary>
        public static bool HasNextLevel(HeroLevel currentLevel) =>
            currentLevel.IsDefined && currentLevel.Value < HeroLevel.Maximum;

        /// <summary>The Level one purchase reaches. Refuses the same case <see cref="LevelUpCost"/> does.</summary>
        public static HeroLevel NextLevel(HeroLevel currentLevel)
        {
            if (!HasNextLevel(currentLevel))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentLevel), currentLevel,
                    $"p.15's table ends at Level {HeroLevel.Maximum}, so nothing follows it.");
            }

            return new HeroLevel(currentLevel.Value + 1);
        }

        /// <summary>
        /// p.15's "Max card Tier" row, read at one Level.
        ///
        /// <see cref="CardTier.None"/> at Level 1 is the table's own dash, and it is the honest answer
        /// rather than an absence: a hero who has bought nothing has upgraded nothing, so the row states
        /// no Tier for that column. Every other column is the printed numeral.
        /// </summary>
        public static CardTier MaxCardTier(HeroLevel level)
        {
            if (!level.IsDefined)
            {
                throw new ArgumentException(
                    "p.15's Max card Tier row is read at a legal Level.", nameof(level));
            }

            switch (level.Value)
            {
                case 1:
                    return CardTier.None;
                case 2:
                case 3:
                case 4:
                    return CardTier.II;
                case 5:
                case 6:
                case 7:
                    return CardTier.III;
                default:
                    return CardTier.IV;
            }
        }
    }
}
