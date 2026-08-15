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
            RuleModificationSet modifications)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            var heroes = FindLegalEnemyHeroes(
                attackerId, attackRange, rangeTotal, participants, board.Occupancies, modifications)
                .Select(AttackTarget.Hero);
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
                    !IsImmuneHeavy(minion, board, participants, modifications) &&
                    !IsDeclaredImmune(RuleObject.ForMinion(minion.MinionId), attacker, modifications))
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
            IEnumerable<HeroOccupancy> occupancies,
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

            if (occupancies == null)
            {
                throw new ArgumentNullException(nameof(occupancies));
            }

            if (attackRange != AttackRangeKind.NonRanged && attackRange != AttackRangeKind.Ranged)
            {
                throw new ArgumentException("Attack target query requires a supported range classification.", nameof(attackRange));
            }

            if (rangeTotal < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(rangeTotal), rangeTotal, "Attack Range Total must be positive.");
            }

            var occupancyArray = occupancies.ToArray();
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
                    !IsDeclaredImmune(RuleObject.ForHero(occupancy.HeroId), attacker, modifications))
                .Select(occupancy => occupancy.HeroId)
                .OrderBy(heroId => heroId.Value, StringComparer.Ordinal)
                .ToArray());
        }

        /// <summary>
        /// Whether a declared immunity refuses this Attack. The verified Heavy rule below stays separate:
        /// it is a standing p.13 property of the board position rather than a modification any source
        /// supplies, and folding it into the declaration set would make it look cancellable.
        /// </summary>
        private static bool IsDeclaredImmune(
            RuleObject target,
            RuleObject attacker,
            RuleModificationSet modifications) =>
            modifications != null && modifications.IsImmuneTo(target, RuleActivity.Attack, attacker);

        /// <summary>
        /// The p.13 Heavy rule asks two different unit-type questions, declared per the unit-type
        /// question ledger in Docs/AI/Rules/RULE_SYSTEMS.md. The immune subject - "the heavy minion
        /// is Immune" - is the Heavy miniature itself: an intrinsic subtype identity. The
        /// accompaniment - "any friendly non-heavy minions in the Battle Zone" - is a bare membership
        /// question, so p.17's replacement doctrine applies and it asks the effective question
        /// through the zone-scoped presence query: a hero counting as a minion accompanies. Since
        /// milestone 6.8g this is the effective type's first production consumer.
        /// </summary>
        private static bool IsImmuneHeavy(
            MinionOccupancy candidate,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications)
        {
            if (candidate.MinionType != StandardMinionType.Heavy)
            {
                return false;
            }

            var candidateZone = board.MapDefinition.GetZoneForSpace(candidate.OccupiedSpace.SpaceId);
            if (!candidateZone.IsStartingBattleZone)
            {
                return false;
            }

            return UnitPresenceQuery
                .FindUnitsInZone(candidateZone.Id, EffectiveUnitKind.Minion, board, modifications)
                .Any(unit => AccompaniesHeavy(unit, candidate, board, participants));
        }

        /// <summary>
        /// Whether one effectively-minion unit in the candidate's zone is a friendly non-heavy
        /// accompaniment. A minion miniature answers by its intrinsic subtype; a hero counting as a
        /// minion has no Heavy subtype - p.17 replaces its type with the generic minion type - so it
        /// accompanies whenever it shares the candidate's team.
        /// </summary>
        private static bool AccompaniesHeavy(
            RuleObject unit,
            MinionOccupancy candidate,
            AdjacentMoveAuthority board,
            MatchParticipants participants)
        {
            if (IntrinsicKindQuery.IsMinionMiniature(unit))
            {
                if (unit.MinionId.Equals(candidate.MinionId))
                {
                    return false;
                }

                var occupancy = board.Minions.Single(minion => minion.MinionId.Equals(unit.MinionId));
                return occupancy.TeamId.Equals(candidate.TeamId) &&
                    occupancy.MinionType != StandardMinionType.Heavy;
            }

            return participants.GetTeamForHero(unit.HeroId).Id.Equals(candidate.TeamId);
        }
    }
}
