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
        /// Milestone 8.57c. The same number with a printed clause's multiplier composed into it -
        /// Rowenna's Glorious Triumph, "your friendly heroes gain <em>triple</em> assist coins".
        ///
        /// <para>It is one number rather than a second payment for the reason
        /// <see cref="LifeCounters(CardTier,int)"/> is one: the coins are produced in exactly one
        /// place, and a card that tripled them by paying twice more would show each team-mate two
        /// collections for one Defeat - which the scoreboard counts and a restore taken between the
        /// two would disagree about. So the multiplication happens where the page's own number is
        /// produced, and every Defeat in this build that prints no such clause passes
        /// <see cref="GuardsDigital.Core.Effects.Synthetic.SyntheticConsequenceCapabilities
        /// .UnmultipliedAssistCoins"/> and reads exactly what it read before.</para>
        ///
        /// <para>It multiplies the ASSIST column and nothing else. The defeating hero's own Defeat
        /// coins are a different sentence on the same page and the Life counters are a third; RO-Q3
        /// is the recorded reading, and the separation is the same one this type's remarks give for
        /// keeping assist coins and Life counters apart in the first place.</para>
        /// </summary>
        public static int AssistCoins(CardTier lowestCardTier, int multiplier)
        {
            if (multiplier < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(multiplier),
                    multiplier,
                    "A printed clause multiplies p.13's assist coins; nothing printed reduces them, " +
                    "so the smallest legal multiplier is the page's own schedule.");
            }

            return AssistCoins(lowestCardTier) * multiplier;
        }

        /// <summary>
        /// p.13: "Defeated hero's team spends the number of Life counters, equal to that hero's lowest
        /// card Tier."
        /// </summary>
        public static int LifeCounters(CardTier lowestCardTier) => TierOrdinal(lowestCardTier, nameof(lowestCardTier));

        /// <summary>
        /// Milestone 8.35z31. The same charge with an attacker's declared additions composed
        /// into it - Ursafar's Tear, "that hero spends 1 additional [Life] Life counter".
        ///
        /// <para>It is one number rather than a second call because the user's 2026-09-06 ruling says
        /// so in as many words: the cost is computed once as Tier + 1 and spent once, at the defeat.
        /// The card prints the clause under "After the attack:", which by p.14 is step 7 - after the
        /// defeat and its spend have already happened - and the ruling reads that placement as when the
        /// condition is <em>checked</em> rather than when the counter is paid. So the addition is
        /// composed here, where p.13's own number is produced, and there is no second spend anywhere
        /// for a payload or a replay to disagree about.</para>
        ///
        /// <para>The distinction matters at exactly one moment and it ends the game: a team on its last
        /// counters loses them in this single combined spend, rather than losing the last one to p.13
        /// and never reaching the card's clause at all.</para>
        /// </summary>
        public static int LifeCounters(CardTier lowestCardTier, int additionalCounters)
        {
            if (additionalCounters < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(additionalCounters),
                    additionalCounters,
                    "A declared addition to p.13's Life cost adds counters; nothing printed reduces it.");
            }

            return LifeCounters(lowestCardTier) + additionalCounters;
        }

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
