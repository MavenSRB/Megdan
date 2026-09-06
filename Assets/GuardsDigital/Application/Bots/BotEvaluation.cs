using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application.Bots
{
    /// <summary>
    /// FOURTEENTH amendment, tier-1 phase A. The hand-crafted position score a practice bot judges
    /// by, from one hero's point of view, in whole points.
    /// </summary>
    /// <remarks>
    /// <para><b>THE MAINTENANCE COVENANT, and it is the reason this type is shaped the way it is.</b>
    /// Every term below reads a STRUCTURAL fact the rules publish for all content - a team's Life
    /// counters, a hero's level and coins, how many minions are standing, how far apart two units
    /// are, how many cards are in hand. <b>No term names a card, a hero, an action or an effect.</b>
    /// The roster is expanding while this is written (six hero inventories are being verified in
    /// parallel), and a per-card term would be a term that rots the moment one of them lands. A
    /// later reader adding "if the card is X" to this file is adding the maintenance burden this
    /// covenant exists to refuse; the honest place for card-specific judgment is the content itself,
    /// which already knows what it does. See the project's cards-are-the-definition discipline: what
    /// the faces do not say is absent, and a bot must not invent it.</para>
    /// <para><b>Public state only.</b> A bot holding a network seat sees its own seat projection and
    /// nothing else (the 8.32 privacy law), so every term here is readable from a
    /// <see cref="SeatProjection"/> as well as from the Director's whole view. That is not a comment,
    /// it is a tested property: the same state scored through both must produce the same number, or
    /// the term is one the shipped bot could not have read.</para>
    /// <para><b>Whole points, deliberately.</b> The score is <see cref="int"/> rather than a double so
    /// that "these two candidates tie" is an exact question with an exact answer, and the seeded
    /// tiebreak that follows it is reproducible per seed on every runtime. A float score would make
    /// determinism depend on floating-point association order.</para>
    /// </remarks>
    public static class BotEvaluation
    {
        /// <summary>
        /// Distances are clamped before scoring: past this many hexes, "far" is just far, and letting
        /// an unbounded board distance into the sum would let one term dominate the Life counters that
        /// actually decide the match.
        /// </summary>
        public const int ProximityCap = 10;

        /// <summary>
        /// The weights, named rather than sprinkled through the arithmetic so that tuning is one
        /// edit in one place and a phase-B tuner has something to search over.
        /// </summary>
        /// <remarks>
        /// The ordering of magnitudes is the design, not the exact values: Life counters are the win
        /// condition and dominate everything (a single Life is worth more than any board position);
        /// a hero standing on the board rather than awaiting respawn is next; progression and minion
        /// pressure sit in the middle; geometry and hand size are tiebreak-scale nudges that only
        /// decide between positions the bigger terms already call equal.
        /// </remarks>
        public const int TeamLifeWeight = 1000;
        public const int BoardPresenceWeight = 30;
        public const int LevelAdvantageWeight = 40;
        public const int OwnLevelWeight = 25;
        public const int OwnCoinWeight = 6;
        public const int MinionPressureWeight = 12;
        public const int EngagementProximityWeight = 4;
        public const int ThreatAdjacencyWeight = 15;
        public const int OwnHandSizeWeight = 5;

        /// <summary>
        /// Rulebook p.13's Life-counter price of a hero Defeat, read from the only input a seat can
        /// actually see.
        /// </summary>
        /// <remarks>
        /// <b>This is an approximation, deliberately, and the fact that it is one is the point.</b>
        /// p.13 prices a Defeat by the defeated hero's <i>lowest card Tier</i>, and
        /// <see cref="Core.Rules.Consequences.HeroDefeatRewardSchedule"/> is the authority - it reads
        /// the hero's cards, because the cards are the authority for the Tier. A bot cannot: another
        /// hero's cards are exactly what the privacy law hides. What IS public is the hero's Level,
        /// and p.13's own table prints the Level row against the Tier row (1-3 to I, 4-6 to II, 7-8
        /// to III). That printed correspondence is what this reads.
        /// <para>So this is the rulebook's own convenience reading used as a public proxy, and the
        /// schedule's doc comment is explicit that the Level row is a convenience reading "rather
        /// than a second rule". A hero who reached a Level without levelling the ordinary way would
        /// be priced wrongly here. That is an acceptable error for a practice opponent's ranking and
        /// an unacceptable one for a rule - which is why this lives in the bot and never near the
        /// resolution path, and why it must never be called an authority for the real price.</para>
        /// </remarks>
        public static int PublicDefeatLifeCost(int level)
        {
            if (level >= 7)
            {
                return 3;
            }

            return level >= 4 ? 2 : 1;
        }

        /// <summary>
        /// Scores <paramref name="readModel"/> from <paramref name="perspectiveHeroId"/>'s point of
        /// view. Higher is better for that hero's team. Returns 0 for an unusable argument rather
        /// than throwing: this sits under a practice opponent, and a bot that crashes a playtest
        /// build to complain about its own inputs is worse than a bot that shrugs and plays on.
        /// </summary>
        public static int Evaluate(IMatchReadModel readModel, HeroId perspectiveHeroId) =>
            Explain(readModel, perspectiveHeroId).Total;

        /// <summary>
        /// The same score with its terms itemised. This is what the tests assert against and what a
        /// phase-B tuner would log: a total alone cannot say WHICH term moved.
        /// </summary>
        public static BotEvaluationBreakdown Explain(IMatchReadModel readModel, HeroId perspectiveHeroId)
        {
            if (readModel == null || !perspectiveHeroId.IsDefined)
            {
                return BotEvaluationBreakdown.Empty;
            }

            TeamId ownTeamId;
            try
            {
                ownTeamId = readModel.Participants.GetTeamForHero(perspectiveHeroId).Id;
            }
            catch (Exception)
            {
                // A hero the participant graph does not know is not a hero this evaluation can take
                // a side for. Same shrug as above, for the same reason.
                return BotEvaluationBreakdown.Empty;
            }

            var lives = readModel.TeamLifeCounters ?? Array.Empty<KeyValuePair<TeamId, int>>();
            var ownLives = lives.Where(entry => entry.Key.Equals(ownTeamId))
                .Select(entry => entry.Value).DefaultIfEmpty(0).Max();
            var enemyLives = lives.Where(entry => !entry.Key.Equals(ownTeamId))
                .Select(entry => entry.Value).DefaultIfEmpty(0).Max();

            var heroes = readModel.Heroes ?? Array.Empty<HeroOccupancy>();
            var ownHeroesOnBoard = 0;
            var enemyHeroesOnBoard = 0;
            var ownCoordinate = default(HexCoordinate);
            var ownIsOnBoard = false;
            var enemyPositions = new List<HexCoordinate>();
            foreach (var occupancy in heroes)
            {
                if (occupancy.HeroId.Equals(perspectiveHeroId))
                {
                    ownCoordinate = occupancy.Coordinate;
                    ownIsOnBoard = true;
                }

                if (IsSameTeam(readModel, occupancy.HeroId, ownTeamId))
                {
                    ownHeroesOnBoard++;
                }
                else
                {
                    enemyHeroesOnBoard++;
                    enemyPositions.Add(occupancy.Coordinate);
                }
            }

            var minions = readModel.Minions ?? Array.Empty<Core.Units.Minions.MinionOccupancy>();
            var ownMinions = 0;
            var enemyMinions = 0;
            var enemyMinionPositions = new List<HexCoordinate>();
            foreach (var minion in minions)
            {
                if (minion.TeamId.Equals(ownTeamId))
                {
                    ownMinions++;
                }
                else
                {
                    enemyMinions++;
                    enemyMinionPositions.Add(minion.Coordinate);
                }
            }

            var progression = readModel.HeroProgression ?? Array.Empty<HeroProgressionProjection>();
            var ownLevelTotal = 0;
            var enemyLevelTotal = 0;
            var ownLevel = 0;
            var ownCoins = 0;
            foreach (var row in progression)
            {
                if (row == null)
                {
                    continue;
                }

                if (row.HeroId.Equals(perspectiveHeroId))
                {
                    ownLevel = row.Level;
                    ownCoins = row.Coins;
                }

                if (IsSameTeam(readModel, row.HeroId, ownTeamId))
                {
                    ownLevelTotal += row.Level;
                }
                else
                {
                    enemyLevelTotal += row.Level;
                }
            }

            // "The lane fight" has no seat-visible identity - the live Battle Zone id is Director-only
            // board state, exactly as the tier-0.5 pass recorded - so every enemy unit's space stands
            // in for it, which is the same proxy that pass already proved workable.
            var objectives = enemyPositions.Concat(enemyMinionPositions).ToArray();
            var proximity = 0;
            var adjacentThreats = 0;
            if (ownIsOnBoard && objectives.Length > 0)
            {
                var nearest = objectives.Min(objective => HexDistanceQuery.Calculate(ownCoordinate, objective));
                proximity = ProximityCap - Math.Min(nearest, ProximityCap);
                adjacentThreats = objectives.Count(objective =>
                    HexDistanceQuery.Calculate(ownCoordinate, objective) <= 1);
            }

            // Own hand only. Another hero's hand SIZE is public and its CONTENTS are not, but a term
            // reading other hands would still have to read them the same way through both projection
            // shapes to keep the tested equality above - and it would buy nothing this bot acts on.
            var cards = readModel.Cards ?? Array.Empty<CardInstance>();
            var ownHandSize = cards.Count(card =>
                card != null && card.OwnerId.Equals(perspectiveHeroId) &&
                card.State.Location == CardLocation.Hand);

            return new BotEvaluationBreakdown(
                teamLifeAdvantage: ownLives - enemyLives,
                boardPresence: ownHeroesOnBoard - enemyHeroesOnBoard,
                levelAdvantage: ownLevelTotal - enemyLevelTotal,
                ownLevel: ownLevel,
                ownCoins: ownCoins,
                minionPressure: ownMinions - enemyMinions,
                engagementProximity: proximity,
                adjacentThreats: adjacentThreats,
                ownDefeatLifeCost: PublicDefeatLifeCost(ownLevel),
                ownHandSize: ownHandSize);
        }

        private static bool IsSameTeam(IMatchReadModel readModel, HeroId heroId, TeamId ownTeamId)
        {
            try
            {
                return readModel.Participants.GetTeamForHero(heroId).Id.Equals(ownTeamId);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    /// One evaluation, itemised. Each raw term is kept beside its weighted contribution so a test can
    /// assert the fact ("this position has one more Life") rather than the arithmetic.
    /// </summary>
    public sealed class BotEvaluationBreakdown
    {
        public static readonly BotEvaluationBreakdown Empty =
            new BotEvaluationBreakdown(0, 0, 0, 0, 0, 0, 0, 0, 1, 0);

        public BotEvaluationBreakdown(
            int teamLifeAdvantage,
            int boardPresence,
            int levelAdvantage,
            int ownLevel,
            int ownCoins,
            int minionPressure,
            int engagementProximity,
            int adjacentThreats,
            int ownDefeatLifeCost,
            int ownHandSize)
        {
            OwnDefeatLifeCost = ownDefeatLifeCost;
            TeamLifeAdvantage = teamLifeAdvantage;
            BoardPresence = boardPresence;
            LevelAdvantage = levelAdvantage;
            OwnLevel = ownLevel;
            OwnCoins = ownCoins;
            MinionPressure = minionPressure;
            EngagementProximity = engagementProximity;
            AdjacentThreats = adjacentThreats;
            OwnHandSize = ownHandSize;
        }

        /// <summary>Own team's remaining Life counters minus the strongest enemy team's.</summary>
        public int TeamLifeAdvantage { get; }

        /// <summary>Own heroes standing on the board minus enemy heroes; a hero awaiting respawn is absent.</summary>
        public int BoardPresence { get; }

        public int LevelAdvantage { get; }
        public int OwnLevel { get; }
        public int OwnCoins { get; }

        /// <summary>Own minions still standing minus enemy minions - the board-public lane-pressure proxy.</summary>
        public int MinionPressure { get; }

        /// <summary><see cref="BotEvaluation.ProximityCap"/> minus the clamped distance to the nearest enemy unit.</summary>
        public int EngagementProximity { get; }

        /// <summary>Enemy units within one hex of this hero. Costed, not rewarded.</summary>
        public int AdjacentThreats { get; }

        /// <summary>
        /// What this hero's own Defeat would cost its team in Life counters, by
        /// <see cref="BotEvaluation.PublicDefeatLifeCost"/>. It scales the adjacency cost, which is
        /// the whole reason it is here: standing next to an enemy is a bigger mistake for a
        /// levelled-up hero than for a fresh one, because p.13 charges its team more for the Defeat.
        /// </summary>
        public int OwnDefeatLifeCost { get; }

        public int OwnHandSize { get; }

        public int Total =>
            (TeamLifeAdvantage * BotEvaluation.TeamLifeWeight) +
            (BoardPresence * BotEvaluation.BoardPresenceWeight) +
            (LevelAdvantage * BotEvaluation.LevelAdvantageWeight) +
            (OwnLevel * BotEvaluation.OwnLevelWeight) +
            (OwnCoins * BotEvaluation.OwnCoinWeight) +
            (MinionPressure * BotEvaluation.MinionPressureWeight) +
            (EngagementProximity * BotEvaluation.EngagementProximityWeight) -
            (AdjacentThreats * OwnDefeatLifeCost * BotEvaluation.ThreatAdjacencyWeight) +
            (OwnHandSize * BotEvaluation.OwnHandSizeWeight);
    }
}
