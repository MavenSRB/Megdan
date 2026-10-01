using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 8.70 widened this file's <c>board</c> parameter from
    /// <see cref="GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority"/> to the declared port
    /// <see cref="IBoardDisplacementAuthority"/>, and nothing else about it moved.
    ///
    /// <para>The three members this query reads - the hero register, the minion register and what
    /// stands on one space - are all on that port, and the two rules queries it hands the board to
    /// (<see cref="Modifications.TerrainPresenceQuery"/> and <see cref="Modifications.TargetQuery"/>)
    /// already take it. What made the widening necessary is that a modal Attack's bullet is now judged
    /// where the modal is OFFERED, and the capability standing on that bullet's node holds
    /// <c>MatchComponents.Board</c> - the port - rather than the movement authority. Widening the
    /// parameter is how that caller reaches the ONE target derivation instead of a second one written
    /// against what it happens to hold, which is the rule <see cref="Modifications.ObstacleQuery"/>
    /// records for the same situation at milestone 8.69a. Every caller passes the concrete authority
    /// exactly as before.</para>
    /// </summary>
    public static class AttackTargetQuery
    {
        /// <summary>
        /// Milestone 9.30f. The p.9 classification of the card an Attack offer is performed from - its
        /// definition's own, which for a cast spell is the SPELL's (the borrow path's offer names the
        /// spell, GY-Q3). The one derivation both Attack target sites read, so the offer, the target
        /// choice and the bullet subjects classify an Attack the same way. A definition this content set
        /// does not supply reads as <see cref="CardClassification.None"/>.
        /// </summary>
        public static CardClassification AttackClassificationOf(
            ActionOffer offer, IEnumerable<CardDefinition> definitions) =>
            (definitions ?? Enumerable.Empty<CardDefinition>())
                .FirstOrDefault(definition => definition.Reference.Equals(offer.SourceDefinitionReference))?
                .Progression.Classification ?? CardClassification.None;

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
            IBoardDisplacementAuthority board,
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
        ///
        /// <para>Milestone 8.10 applies the third half of that declaration, p.19's straight-line
        /// complement, here rather than in card text: both Boomerangs print it as the sentence that
        /// says what the Attack targets, so the offer, the target choice and the resolution all read
        /// it from this one query and cannot disagree about which units the card admits.</para>
        ///
        /// <para>Milestone 9.30f. <paramref name="attackClassification"/> is the attacking card
        /// definition's p.9 classification, so an immunity scoped to basic or non-basic attacks (Gydion's
        /// Shield and Invulnerability) is asked about the right class through
        /// <see cref="RuleActivityQuery.ForAttack"/>. A cast spell's Attack passes the SPELL's (GY-Q3).
        /// <see cref="CardClassification.None"/> asks the unclassified <see cref="RuleActivity.Attack"/>,
        /// which only an unscoped Attack immunity covers - exactly what every caller asked before.</para>
        /// </summary>
        public static IReadOnlyList<AttackTarget> FindLegalEnemyTargets(
            HeroId attackerId,
            PrintedAttackTargetPolicy policy,
            int rangeTotal,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            RuleModificationSet modifications,
            CardClassification attackClassification = CardClassification.None)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            var attackRange = policy.Reach == AttackTargetReach.AdjacentOnly
                ? AttackRangeKind.NonRanged
                : AttackRangeKind.Ranged;

            // Milestone 9.30e. A Range Total of 0 - Sunburst's printed "0+" that nothing has raised
            // yet - reaches no unit: p.9's smallest printed Range, 1, is what reaches the adjacent ring.
            // An Attack offered on its own is greyed by the empty set (7.8a1); a cast Sunburst is still
            // cast and reaches nobody (GY-Q12).
            if (attackRange == AttackRangeKind.Ranged && rangeTotal < 1)
            {
                return Array.Empty<AttackTarget>();
            }
            // Milestone 9.21a. The attacker's space is the acting body's (RZ-Q4; body 1 until 9.21b
            // makes it a pick), read through the port rather than as the single row of a hero that may
            // stand on several spaces. A hero standing nowhere reads as the undefined occupancy the
            // checks below were written against.
            var attackerOccupancy = board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(attackerId), out var attackerAt)
                ? new HeroOccupancy(attackerId, attackerAt)
                : default;
            var occupancies = board.Occupancies.ToArray();
            var minionOccupancies = board.Minions.ToArray();

            // Milestone 8.10. The Boomerangs' "not in a straight line", measured from the attacker's
            // own space. Nothing is written here for the reminder that adjacent units are in one:
            // p.19 makes adjacency the separation-of-one case of a file, so the single membership
            // question already answers it, and a second adjacency test would be a second rule.
            // Milestone 8.38a reads the complement from the same membership question, so a card that
            // demands the file and a card that forbids it cannot disagree about what one is.
            // Milestone 8.40d's fourth member asks that same question and then walks what stands on
            // the file, so all four readings of "which file" are still one query. The switch is
            // exhaustive on purpose: a fifth member added later reaches the refusal below rather than
            // silently admitting everything.
            bool StandsOnAnAdmittedLine(HexCoordinate candidate)
            {
                switch (policy.Line)
                {
                    case AttackTargetLine.AnyLine:
                        return true;
                    case AttackTargetLine.NotInStraightLine:
                        return !StraightLineQuery.AreInStraightLine(
                            attackerOccupancy.Coordinate, candidate);
                    case AttackTargetLine.InStraightLine:
                        return StraightLineQuery.AreInStraightLine(
                            attackerOccupancy.Coordinate, candidate);
                    case AttackTargetLine.InStraightLineClearOfUnitsAndTerrain:
                        return StraightLineQuery.AreInStraightLine(
                                attackerOccupancy.Coordinate, candidate) &&
                            NoUnitOrTerrainStandsBetween(
                                attackerOccupancy.Coordinate, candidate);
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(policy), policy.Line,
                            "A printed Attack target policy states whether it excludes, demands or " +
                            "ignores rulebook p.19's straight line.");
                }
            }

            // Milestone 8.40d. "with no units, or terrain, between you" - Light Crossbow's, Heavy
            // Crossbow's and Arbalest's clearance clause, walked over the spaces STRICTLY between the
            // two ends of the file and neither endpoint. p.19's own words are "between you and the
            // target", so the attacker's space and the target's space are the ends of the line rather
            // than places on it: a unit is not an obstacle to itself, and the attacker is not one to
            // his own shot. An adjacent target therefore has nothing between it and the shooter and is
            // unaffected, which falls out of the loop rather than being written as a second rule.
            //
            // WHICH TWO QUESTIONS ARE ASKED, AND WHICH THIRD IS DELIBERATELY NOT.
            // "Unit" is p.19's type-insensitive union of both registers, asked of the board's
            // occupancy: a hero or a minion standing on the space, and nothing else the index holds.
            // A token is an occupant and is not a unit, which is the whole of BA-03; so is p.18's
            // fourth kind, "any objects represented by miniatures", which this sentence names no more
            // than it names tokens.
            //
            // "Terrain" is TerrainPresenceQuery's question rather than a second reading of the map,
            // and that is the deliberate part. That query folds the map's printed terrain together
            // with milestone 8.19c's EffectiveTypeOverlay, so a card that makes something count as
            // terrain reaches this clause without this file knowing the card exists. Today the
            // overlay cannot change an answer here - Xargatha's Stone Gaze makes enemy HEROES count
            // as terrain, and a hero standing between is already a unit stopping the walk - so the
            // choice is invisible on every board this build can produce and is made anyway, because
            // "what counts as terrain" having two owners is how the two come to disagree.
            //
            // What is NOT asked is p.18's obstacle classification. ObstacleQuery answers the wider
            // word - tokens, the void past the map's edge, and milestone 8.7's ConditionalObstacleOverlay
            // among them - and the wider word is precisely the one these three faces decline to print
            // while Get over here! prints it. Off the map is therefore neither a unit nor terrain and
            // does not block, which is milestone 8.39b's recorded reading of the same void ("the
            // absence of a space") and is unreachable on a configured map anyway, because p.19 makes
            // the water around the board declared terrain rather than nothing.
            bool NoUnitOrTerrainStandsBetween(HexCoordinate from, HexCoordinate to)
            {
                if (!StraightLineQuery.TryGetDirection(from, to, out var direction, out var distance))
                {
                    // p.19's degenerate line - a space with itself - has no space between its ends.
                    // Unreachable here, because no attacker is his own enemy; answered rather than
                    // left to a walk that would read an undefined direction.
                    return true;
                }

                for (var step = 1; step < distance; step++)
                {
                    var between = StraightLineQuery.Step(from, direction, step);
                    if (TerrainPresenceQuery.IsTerrain(board, between, modifications))
                    {
                        return false;
                    }

                    if (board.TryGetObjectAt(between, out var standing) &&
                        (standing.Kind == BoardObjectKind.Hero ||
                            standing.Kind == BoardObjectKind.Minion))
                    {
                        return false;
                    }
                }

                return true;
            }

            // Milestone 8.35z30, RULING 1. "at maximum range" is a band and "in range" is
            // p.13's ceiling, and the composed Range Total is the one number both read. The distance
            // is measured here for both unit kinds, so the two halves of the answer below cannot
            // disagree about what the card reaches.
            // Milestone 8.38a adds the fourth band under the same ceiling: "in range, and not
            // adjacent to you" is the printed Range with p.19's separation-of-one taken out from
            // under it. The ceiling itself is already applied by the two per-register queries below,
            // which read rangeTotal; only the floor is stated here.
            bool StandsAtAnAdmittedDistance(HexCoordinate candidate)
            {
                switch (policy.Reach)
                {
                    case AttackTargetReach.ExactlyPrintedRange:
                        return HexDistanceQuery.Calculate(
                            attackerOccupancy.Coordinate, candidate) == rangeTotal;
                    case AttackTargetReach.PrintedRangeBeyondAdjacent:
                        return HexDistanceQuery.Calculate(
                            attackerOccupancy.Coordinate, candidate) > 1;
                    default:
                        return true;
                }
            }

            // Milestone 8.35z30. The union Silverarrow's three Red modals admit: a unit
            // beside the attacker, or a unit no unit at all stands beside. p.19's "unit" is the
            // type-insensitive union of both registers, so both are enumerated and neither asks what
            // anything counts as - the same reading this file already takes of p.13's own sentence.
            bool IsIsolatedOrAdjacentToTheAttacker(HexCoordinate candidate) =>
                policy.TargetableUnits != AttackTargetableUnits.IsolatedUnitsOrUnitsAdjacentToYou ||
                HexDistanceQuery.Calculate(attackerOccupancy.Coordinate, candidate) == 1 ||
                (!occupancies.Any(value =>
                     !value.Coordinate.Equals(candidate) &&
                     HexDistanceQuery.Calculate(value.Coordinate, candidate) == 1) &&
                 !minionOccupancies.Any(value =>
                     !value.Coordinate.Equals(candidate) &&
                     HexDistanceQuery.Calculate(value.Coordinate, candidate) == 1));

            // Milestone 9.30d. Fireball's "not adjacent ... to your friendly unit": no unit on the
            // attacker's team other than the attacker stands beside the candidate (p.17's Friendly
            // never includes you, nor any other body of a many-bodied attacker). Asked of the one owner
            // of "units within a disk in a team relation", as Hanu's friendly-hero bullet asks it;
            // standing beside a unit is not something done to it, so no immunity is asked of it.
            bool IsBesideNoFriendlyUnitOfTheAttacker(HexCoordinate candidate) =>
                policy.TargetableUnits != AttackTargetableUnits.UnitsNotAdjacentToAFriendlyUnitOfTheAttacker ||
                RosterPresenceQuery.FindUnitsWithin(
                    candidate,
                    1,
                    RosterUnitKind.Unit,
                    SourceTeamRelation.AlliedWithSource,
                    attackerId,
                    board,
                    participants,
                    modifications,
                    excludedFirst: BoardObjectReference.ForHero(attackerId)).Count == 0;

            bool IsAdmitted(HexCoordinate candidate) =>
                StandsOnAnAdmittedLine(candidate) &&
                StandsAtAnAdmittedDistance(candidate) &&
                IsIsolatedOrAdjacentToTheAttacker(candidate) &&
                IsBesideNoFriendlyUnitOfTheAttacker(candidate);

            // Milestone 8.71c. The mirror narrowing: Hanu's Outnumber and Pile On print "Target a
            // minion in range", so an enemy HERO in exactly that position is not a legal target of the
            // bullet. The hero half is skipped rather than derived and then discarded, because
            // FindLegalEnemyHeroes refuses an attacker standing nowhere by returning nothing and this
            // arm must answer the same way for a reason of its own - see the guard below it.
            var minionsOnly = policy.TargetableUnits == AttackTargetableUnits.MinionsOnly;
            var heroes = minionsOnly
                ? Enumerable.Empty<AttackTarget>()
                : FindLegalEnemyHeroBodies(
                        attackerId, attackRange, rangeTotal, participants, board, modifications,
                        attackClassification)
                    .Where(body => IsAdmitted(body.Coordinate))
                    .Select(body => AttackTarget.HeroBodyOf(body.HeroId, body.Body));

            // p.13's narrowing, and the only place it is applied: Crackling Dagger prints "Target a
            // hero adjacent to you", so a minion standing beside her is not a legal target of it even
            // though the same page's default admits one.
            if (policy.TargetableUnits == AttackTargetableUnits.HeroesOnly)
            {
                return Array.AsReadOnly(heroes
                    .OrderBy(target => target.HeroId.Value, StringComparer.Ordinal)
                    .ThenBy(target => target.HeroBody)
                    .ToArray());
            }
            if (!attackerOccupancy.HeroId.IsDefined)
            {
                // An attacker who is not on the board reaches nothing, which for every policy above is
                // what the empty hero set already says. A minions-only policy has no hero set to say it
                // with, so it is said here: the minion walk below measures from `attackerOccupancy`,
                // and a default occupancy would measure every distance from (0,0).
                return Array.AsReadOnly(heroes.ToArray());
            }

            var attackerTeam = participants.GetTeamForHero(attackerId).Id;
            var maximumDistance = attackRange == AttackRangeKind.NonRanged ? 1 : rangeTotal;
            var attacker = RuleObject.ForHero(attackerId);
            var minions = minionOccupancies
                .Where(minion =>
                    !minion.TeamId.Equals(attackerTeam) &&
                    HexDistanceQuery.Calculate(attackerOccupancy.Coordinate, minion.Coordinate) <= maximumDistance &&
                    IsAdmitted(minion.Coordinate) &&
                    !TargetQuery.IsPrevented(
                        RuleObject.ForMinion(minion.MinionId), RuleActivityQuery.ForAttack(attackClassification),
                        attacker,
                        board, participants, modifications))
                .Select(minion => AttackTarget.Minion(minion.MinionId));

            return Array.AsReadOnly(heroes.Concat(minions)
                .OrderBy(target => target.Kind)
                .ThenBy(target => target.Kind == AttackTargetKind.Hero
                    ? target.HeroId.Value
                    : target.MinionId.Value, StringComparer.Ordinal)
                .ThenBy(target => target.HeroBody)
                .ToArray());
        }

        public static IReadOnlyList<HeroId> FindLegalEnemyHeroes(
            HeroId attackerId,
            AttackRangeKind attackRange,
            int rangeTotal,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            RuleModificationSet modifications,
            CardClassification attackClassification = CardClassification.None) =>
            // Milestone 9.21a. A hero is a legal target when any of its bodies is (RZ-Q2: presence
            // and reach per body); each hero is named once.
            Array.AsReadOnly(FindLegalEnemyHeroBodies(
                    attackerId, attackRange, rangeTotal, participants, board, modifications,
                    attackClassification)
                .Select(body => body.HeroId)
                .Distinct()
                .ToArray());

        /// <summary>
        /// Milestone 9.21a (RZ-Q3). The enemy hero BODIES this attacker may target, in (hero, body)
        /// order: an Attack targets one body, and p.14's reach and the Defense are measured from it.
        /// A hero with one body contributes exactly the row it always did.
        /// </summary>
        /// <param name="attackClassification">
        /// Milestone 9.30f. The attacking card's p.9 classification, which decides whether an immunity
        /// scoped to basic or non-basic attacks reaches this Attack - see
        /// <see cref="FindLegalEnemyTargets(HeroId, PrintedAttackTargetPolicy, int, MatchParticipants, IBoardDisplacementAuthority, RuleModificationSet, CardClassification)"/>.
        /// </param>
        public static IReadOnlyList<HeroOccupancy> FindLegalEnemyHeroBodies(
            HeroId attackerId,
            AttackRangeKind attackRange,
            int rangeTotal,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            RuleModificationSet modifications,
            CardClassification attackClassification = CardClassification.None)
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

            // The attacker's acting body (milestone 9.21a); an attacker standing nowhere reaches
            // nothing, exactly as before.
            if (!board.TryGetObjectCoordinate(BoardObjectReference.ForHero(attackerId), out var attackerCoordinate))
            {
                return Array.Empty<HeroOccupancy>();
            }

            var occupancyArray = board.Occupancies.ToArray();
            var participantHeroIds = new HashSet<HeroId>(participants.Heroes.Select(hero => hero.Id));
            var attackerTeamId = participants.GetTeamForHero(attackerId).Id;
            var maximumDistance = attackRange == AttackRangeKind.NonRanged ? 1 : rangeTotal;

            var attacker = RuleObject.ForHero(attackerId);
            return Array.AsReadOnly(occupancyArray
                .Where(occupancy =>
                    !occupancy.HeroId.Equals(attackerId) &&
                    participantHeroIds.Contains(occupancy.HeroId) &&
                    HexDistanceQuery.Calculate(attackerCoordinate, occupancy.Coordinate) <= maximumDistance &&
                    !participants.GetTeamForHero(occupancy.HeroId).Id.Equals(attackerTeamId) &&
                    !TargetQuery.IsPrevented(
                        RuleObject.ForHero(occupancy.HeroId), RuleActivityQuery.ForAttack(attackClassification),
                        attacker,
                        board, participants, modifications))
                .OrderBy(occupancy => occupancy.HeroId.Value, StringComparer.Ordinal)
                .ThenBy(occupancy => occupancy.Body)
                .ToArray());
        }

    }
}
