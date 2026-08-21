using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public static class AttackTargetQuery
    {
        /// <summary>
        /// The enemy units this attacker may target.
        ///
        /// Unit-type question (6.8g ledger): none for the assembly itself. p.13 "You may attack
        /// enemy heroes and minions" names the union of both unit types, so enumerating the hero and
        /// minion registers is unit enumeration and a `Counts As` declaration cannot move an object
        /// into or out of the union. The Heavy-immunity filter below asks its own two declared
        /// questions.
        /// </summary>
        /// <param name="modifications">
        /// The modifications in effect; <see cref="RuleModificationSet.Empty"/> is the named
        /// no-modification value (milestone 6.8f). Rulebook p.18: an immune unit
        /// cannot be targeted, while an effect that merely checks presence still counts it - which is why
        /// this query filters and <see cref="Modifications.UnitPresenceQuery"/> does not.
        /// </param>
        public static IReadOnlyList<AttackTarget> FindLegalEnemyTargets(
            HeroId attackerId,
            AttackRangeKind attackRange,
            int rangeTotal,
            MatchParticipants participants,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications) =>
            FindLegalEnemyTargets(
                attackerId,
                PrintedAttackTargetPolicy.ForClassification(attackRange),
                rangeTotal,
                participants,
                board,
                modifications);

        /// <summary>
        /// The same question asked of the card rather than of its subtype label. Milestone 8.5.
        ///
        /// <para>p.13 attributes both halves of an Attack's targeting to the card: "You may attack enemy
        /// heroes and minions", narrowed where a card's own words say otherwise, and a reach that is
        /// adjacency for a non-Ranged Attack "even if the card has a radius icon" or the printed Range
        /// for a Ranged one. The overload above is that page's default for a card that narrows nothing,
        /// so nothing about an ordinary Attack changed; what changed is that a card which does narrow it
        /// now has somewhere to say so as typed content instead of in a sentence something parses.</para>
        /// </summary>
        public static IReadOnlyList<AttackTarget> FindLegalEnemyTargets(
            HeroId attackerId,
            PrintedAttackTargetPolicy policy,
            int rangeTotal,
            MatchParticipants participants,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            var attackRange = policy.Reach == AttackTargetReach.PrintedRange
                ? AttackRangeKind.Ranged
                : AttackRangeKind.NonRanged;
            var heroes = FindLegalEnemyHeroes(
                attackerId, attackRange, rangeTotal, participants, board, modifications)
                .Select(AttackTarget.Hero);

            // p.13's narrowing, and the only place it is applied: Crackling Dagger prints "Target a
            // hero adjacent to you", so a minion standing beside her is not a legal target of it even
            // though the same page's default admits one.
            if (policy.TargetableUnits == AttackTargetableUnits.HeroesOnly)
            {
                return Array.AsReadOnly(heroes
                    .OrderBy(target => target.HeroId.Value, StringComparer.Ordinal)
                    .ToArray());
            }
            var attackerOccupancy = board.Occupancies.SingleOrDefault(value => value.HeroId.Equals(attackerId));
            if (!attackerOccupancy.HeroId.IsDefined)
            {
                return Array.AsReadOnly(heroes.ToArray());
            }

            var attackerTeam = participants.GetTeamForHero(attackerId).Id;
            var maximumDistance = attackRange == AttackRangeKind.NonRanged ? 1 : rangeTotal;
            var attacker = RuleObject.ForHero(attackerId);
            var minions = board.Minions
                .Where(minion =>
                    !minion.TeamId.Equals(attackerTeam) &&
                    HexDistanceQuery.Calculate(attackerOccupancy.Coordinate, minion.Coordinate) <= maximumDistance &&
                    !ImmunityQuery.IsImmuneTo(
                        RuleObject.ForMinion(minion.MinionId), RuleActivity.Attack, attacker,
                        board, participants, modifications))
                .Select(minion => AttackTarget.Minion(minion.MinionId));

            return Array.AsReadOnly(heroes.Concat(minions)
                .OrderBy(target => target.Kind)
                .ThenBy(target => target.Kind == AttackTargetKind.Hero
                    ? target.HeroId.Value
                    : target.MinionId.Value, StringComparer.Ordinal)
                .ToArray());
        }

        public static IReadOnlyList<HeroId> FindLegalEnemyHeroes(
            HeroId attackerId,
            AttackRangeKind attackRange,
            int rangeTotal,
            MatchParticipants participants,
            AdjacentMoveAuthority board,
            RuleModificationSet modifications)
        {
            if (!attackerId.IsDefined)
            {
                throw new ArgumentException("Attack target query requires a defined attacker ID.", nameof(attackerId));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (attackRange != AttackRangeKind.NonRanged && attackRange != AttackRangeKind.Ranged)
            {
                throw new ArgumentException("Attack target query requires a supported range classification.", nameof(attackRange));
            }

            if (rangeTotal < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(rangeTotal), rangeTotal, "Attack Range Total must be positive.");
            }

            var occupancyArray = board.Occupancies.ToArray();
            var attackerOccupancies = occupancyArray
                .Where(occupancy => occupancy.HeroId.Equals(attackerId))
                .ToArray();
            if (attackerOccupancies.Length != 1)
            {
                return Array.Empty<HeroId>();
            }

            var participantHeroIds = new HashSet<HeroId>(participants.Heroes.Select(hero => hero.Id));
            var attackerTeamId = participants.GetTeamForHero(attackerId).Id;
            var maximumDistance = attackRange == AttackRangeKind.NonRanged ? 1 : rangeTotal;
            var attackerCoordinate = attackerOccupancies[0].Coordinate;

            var attacker = RuleObject.ForHero(attackerId);
            return Array.AsReadOnly(occupancyArray
                .Where(occupancy =>
                    !occupancy.HeroId.Equals(attackerId) &&
                    participantHeroIds.Contains(occupancy.HeroId) &&
                    HexDistanceQuery.Calculate(attackerCoordinate, occupancy.Coordinate) <= maximumDistance &&
                    !participants.GetTeamForHero(occupancy.HeroId).Id.Equals(attackerTeamId) &&
                    !ImmunityQuery.IsImmuneTo(
                        RuleObject.ForHero(occupancy.HeroId), RuleActivity.Attack, attacker,
                        board, participants, modifications))
                .Select(occupancy => occupancy.HeroId)
                .OrderBy(heroId => heroId.Value, StringComparer.Ordinal)
                .ToArray());
        }

    }
}
