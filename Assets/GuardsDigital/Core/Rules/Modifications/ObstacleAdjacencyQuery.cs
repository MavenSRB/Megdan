using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Rulebook p.18's obstacle count around one space, read as the THRESHOLD fifteen of Brynn the
    /// Seeker's nineteen printed faces ask for: "adjacent to 3 or more obstacles". Milestone 8.69a.
    ///
    /// <para><b>One owner, because one printed phrase has one meaning.</b> His Gold basic prints the
    /// reminder that says what counts - "( You, other heroes, minions, tokens and terrain are obstacles
    /// )" - and every one of his clauses reads the same sentence: an attack-target bullet, a +2 on the
    /// Attack total, a radius-hero selection, a presence predicate, three swaps, three traps and two
    /// repeats. <see cref="ObstacleQuery"/> already owns what an obstacle IS, so this owns only the
    /// count and the number, and asks that owner six times. The alternative - each consumer walking its
    /// own six neighbours - is the shape that lets one of them quietly forget p.19's water ring or
    /// milestone 8.7's overlay.</para>
    ///
    /// <para><b>It is a threshold and never a count</b>, which is why the number is in the method's
    /// name rather than in a parameter. That is the discipline
    /// <c>RosterValueQueryKind.TwoOrMoreEmptyMinionSpawnPointsInBattleZoneInPrintedRadius</c> records
    /// at length: every face of his prints "3 or more", the bonus beside it is the same whether three
    /// neighbours qualify or six, and a face printing a different number would be a different member
    /// here rather than an argument. It short-circuits at the third for the same reason - nothing above
    /// three is a fact this project's content has asked for.</para>
    ///
    /// <para><b>Nothing is targeted by asking it</b> (p.19: "Effects that check for unit presence ...
    /// do not target"), so p.18's immunity never enters: an immune hero standing beside the subject is
    /// counted exactly as any other unit is, on the same page's "presence checks count immune units".
    /// The subject's own space is not counted, and Brynn himself is counted for everybody else, which
    /// is the Gold's reminder read literally.</para>
    ///
    /// <para><b>The one rule that is not geometry lives here too</b>, at the hero-scoped entry: Over
    /// the Top, his Purple Ultimate, prints "While you are performing actions all enemy heroes in play
    /// count as adjacent to 3 or more obstacles." That is p.17's Counts As applied to this predicate
    /// rather than to a unit type, so it is asked BEFORE the neighbours are walked and it short-
    /// circuits them - a hero the overlay reaches qualifies standing alone in the middle of an empty
    /// map. Both halves of its condition - who is acting, and who is an enemy of whom - are resolved
    /// where the board and the participants are, in <see cref="RuleModificationQuery"/>, exactly as
    /// milestone 8.7's <see cref="AppliedObstacleOverlay"/> resolves its own; what arrives here is a
    /// declaration addressed to one hero, which is what makes this query pure.</para>
    /// </summary>
    public static class ObstacleAdjacencyQuery
    {
        /// <summary>
        /// The number every face of his prints. Stated once, beside the rule, because the phrase is
        /// the same on all fifteen and a consumer writing its own 3 would be a second place it could
        /// disagree.
        /// </summary>
        public const int PrintedObstacleThreshold = 3;

        /// <summary>
        /// Whether at least <see cref="PrintedObstacleThreshold"/> of the six spaces neighbouring
        /// <paramref name="coordinate"/> hold an obstacle right now.
        ///
        /// <para>p.17's Adjacent is the six-space ring and nothing else, so the subject's own space is
        /// never among them - a hero standing on a space an overlay has closed is not thereby adjacent
        /// to itself.</para>
        /// </summary>
        public static bool IsAdjacentToThreeOrMoreObstacles(
            ITokenPlacementAuthority spaces,
            HexCoordinate coordinate,
            RuleModificationSet modifications)
        {
            if (spaces == null)
            {
                throw new ArgumentNullException(nameof(spaces));
            }

            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            var found = 0;
            foreach (var neighbor in coordinate.GetNeighbors())
            {
                if (!ObstacleQuery.IsObstacle(spaces, neighbor, modifications))
                {
                    continue;
                }

                if (++found >= PrintedObstacleThreshold)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The same question asked about one HERO, which is the noun every printed clause of his uses.
        ///
        /// <para>The Ultimate's overlay is consulted first and decides on its own, because p.17's
        /// "counts as" is an answer rather than an addition to the count: a hero it reaches satisfies
        /// the clause wherever it is standing. Only then is the hero's space looked up.</para>
        ///
        /// <para><b>A hero who is not on the board answers false rather than throwing</b>, on the
        /// geometry branch: a hero standing nowhere has no six neighbours to count, and every consumer
        /// that asks this of a candidate derives that candidate from the board.</para>
        ///
        /// <para><b>The overlay branch is deliberately not board-scoped, and milestone 8.69h says so
        /// where it is read.</b> Over the Top's counts-as is derived per enemy hero of the MATCH while
        /// its controller acts (<see cref="RuleModificationQuery"/>), so a hero rulebook p.14's step 6
        /// has just removed still carries it - which is what lets
        /// <c>brynn-attack-target-was-adjacent-to-obstacles@1</c> answer at step 7 about a targeting
        /// that happened at step 3. It cannot make an absent hero "the most eligible target on the
        /// board", because no selection in this build offers a hero who is standing nowhere.</para>
        /// </summary>
        public static bool HeroIsAdjacentToThreeOrMoreObstacles(
            ITokenPlacementAuthority spaces,
            HeroId hero,
            RuleModificationSet modifications,
            int body = 0)
        {
            if (spaces == null)
            {
                throw new ArgumentNullException(nameof(spaces));
            }

            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            if (!hero.IsDefined)
            {
                return false;
            }

            if (modifications.CountsAsAdjacentToThreeOrMoreObstacles(RuleObject.ForHero(hero)))
            {
                return true;
            }

            // Milestone 9.21a (RZ-Q2). Adjacency is read per body: a hero standing on several spaces
            // is adjacent to what any of its bodies is adjacent to. A hero with one body reads its one
            // space exactly as before. Milestone 9.21c: a read that knows WHICH body it means - the
            // body an Attack took, or the body a selection picked as its space - names it, and only
            // that body's space is read.
            return spaces.GetHeroBodies(hero).Any(standing =>
                (body <= 0 || standing.Body == body) &&
                IsAdjacentToThreeOrMoreObstacles(spaces, standing.Coordinate, modifications));
        }
    }
}
