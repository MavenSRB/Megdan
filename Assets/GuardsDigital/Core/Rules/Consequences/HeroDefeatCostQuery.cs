using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Consequences
{
    /// <summary>
    /// Milestone 8.49. Rulebook p.13's "that hero's lowest card Tier", read over the cards a hero is
    /// PLAYING WITH: the hand, the turn area and the discard pile.
    ///
    /// <para><b>Why the scope is stated here and not left to the caller.</b> Milestone 7.4 read the
    /// Tier over every card the hero owns, on the argument that p.13 attaches no location to the
    /// phrase. That reading has a defect the 2026-09-18 playtest found at the table - "Lives are not
    /// being counted correctly. It just takes -1 from pool." - because p.15's Upgrade a Card RETURNS
    /// the upgraded card to the deck, face up, and it stays there: a Level 4 hero holding three Tier II
    /// cards still owns three Tier I cards in its deck, so the whole-pool reading answered Tier I for
    /// every hero at every Level, and every Defeat cost one Life counter and paid one assist coin.
    /// p.13's own table (Level 4-6 -> Tier II -> 2, Level 7-8 -> Tier III -> 3) is only reproduced by
    /// reading the cards the hero is playing this game with, which are the three locations below;
    /// the deck holds what the hero has NOT taken yet or has given back, and a set-aside card is a
    /// facedown future item, which p.15 says is "not considered".</para>
    ///
    /// <para>One query for both readers: the combat backend prices the Defeat through it, and the
    /// hero rail's worth badge shows the same number before the Defeat happens, so a player can see
    /// what a hero is worth - and the two can never disagree.</para>
    /// </summary>
    public static class HeroDefeatCostQuery
    {
        /// <summary>The locations p.13's reading covers: what the hero is playing with this game.</summary>
        public static readonly IReadOnlyList<CardLocation> CountedLocations = Array.AsReadOnly(new[]
        {
            CardLocation.Hand,
            CardLocation.TurnArea,
            CardLocation.DiscardPile
        });

        /// <summary>
        /// The hero's lowest non-Basic card Tier among the counted locations. False when the hero has
        /// no non-Basic card there, which p.13 has no number for.
        /// </summary>
        public static bool TryGetLowestCardTier(
            HeroId heroId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            out CardTier lowestTier)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("p.13 reads the Tier of a named hero.", nameof(heroId));
            }

            return CardCharacteristicQuery.TryGetLowestNonBasicTier(
                new CardCharacteristicCriteria(ownerId: heroId, locations: CountedLocations),
                definitions,
                instances,
                out lowestTier);
        }

        /// <summary>
        /// What defeating the hero is worth, as p.13's table prints it: the Tier read as 1, 2 or 3 -
        /// the assist coins every friendly hero of the attacker collects and the Life counters the
        /// defeated hero's team spends. Zero when the hero has no Tier to read.
        /// </summary>
        public static int Worth(
            HeroId heroId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances) =>
            TryGetLowestCardTier(heroId, definitions, instances, out var tier)
                ? HeroDefeatRewardSchedule.LifeCounters(tier)
                : 0;
    }
}
