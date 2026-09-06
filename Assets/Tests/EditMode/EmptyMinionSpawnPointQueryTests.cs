using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.DodgerSpawnPointBoard;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z28. The derivation nine of Dodger's nineteen faces stand on, measured against the
    /// two user rulings of 2026-09-06 that decide what it counts.
    ///
    /// <para>Every case here is a difference the rulings make. A board on which the two candidate
    /// readings of D-01 agree would prove nothing about which one shipped, which is why
    /// <see cref="DodgerSpawnPointBoard"/> puts a hero spawn point inside the Battle Zone and a minion
    /// spawn point outside it, both within one printed Radius of the same space.</para>
    /// </summary>
    public sealed class EmptyMinionSpawnPointQueryTests
    {
        private static readonly HeroId Dodger = new HeroId("dodger-hero");

        /// <summary>
        /// RULING 4, measured: "empty spawn points" means minion spawn points, so the hero spawn point
        /// one space away is not one of them.
        ///
        /// <para>The board holds six empty spawn points inside a Radius-2 disk on the origin. Four
        /// qualify. The fifth is a hero's - the whole of what this ruling settles - and the sixth is a
        /// minion's standing outside the Battle Zone, which the printed clause excludes on its own
        /// words. Under the rejected reading of D-01 this number would be five.</para>
        /// </summary>
        [Test]
        public void TheCount_ExcludesAHeroSpawnPointInsideTheBattleZone()
        {
            var board = CreateBoard(Dodger, Origin);

            var qualifying = EmptyMinionSpawnPointQuery.FindInCurrentBattleZone(board, board);

            Assert.That(qualifying, Has.No.Member(HeroSpawn),
                "RULING 4 (user, 2026-09-06): \"empty spawn points\" means MINION spawn points only, " +
                "matching the rulebook's own count of Vampiric Shield on printed p.14. The hero spawn " +
                "point on (0, 1) is empty, is in the Battle Zone, and is one space from her - so under " +
                "the rejected reading of D-01 it would be counted.");
            Assert.That(EmptyMinionSpawnPointQuery.CountWithin(board, board, Origin, 2), Is.EqualTo(4),
                "The four minion spawn points of the Battle Zone within a Radius of 2, and nothing " +
                "else: not the hero's point beside her, and not the Throne Zone's minion point at the " +
                "same distance.");
        }

        /// <summary>
        /// The other half of the printed clause: "in the battle zone" is asked of the spawn point, so a
        /// minion spawn point in the Throne Zone does not qualify however close it stands.
        /// </summary>
        [Test]
        public void TheCount_ExcludesAMinionSpawnPointOutsideTheBattleZone()
        {
            var board = CreateBoard(Dodger, Origin);

            var qualifying = EmptyMinionSpawnPointQuery.FindInCurrentBattleZone(board, board);

            Assert.That(qualifying, Has.No.Member(ThroneMinionSpawn),
                "The Throne Zone's melee spawn point on (2, -1) is empty and is exactly two spaces " +
                "from the origin, so only its Zone can exclude it.");
            Assert.That(
                HexDistanceQuery.Calculate(Origin, ThroneMinionSpawn), Is.EqualTo(2),
                "Stated so a reader can see the exclusion is the Zone's doing and not the disk's.");
        }

        /// <summary>
        /// THE DESIGNER RULING, measured: the condition is zone-anchored and not self-anchored.
        ///
        /// <para>Captured 2026-07-25 from the official "rules-questions-goa2" Discord, on Aegis of Doom.
        /// Q: "Can Dodger be outside the battle zone and still get the +4 for defense as long as the
        /// empty spawns are in battle zone?" A (the designer, as OP): "Sure, why not." So a Dodger
        /// standing in a Throne Zone still counts every qualifying point her printed Radius reaches -
        /// and here that is two, which is exactly what her three Blue Defense primaries ask for.</para>
        /// </summary>
        [Test]
        public void TheCount_IsMeasuredFromTheHeroWhereverSheStands_IncludingOutsideTheBattleZone()
        {
            var board = CreateBoard(Dodger, ThroneStandingSpace);

            Assert.That(
                BattleZoneQuery.IsInCurrentBattleZone(board, ThroneStandingSpace), Is.False,
                "The regression is only about something if she is genuinely outside the Zone.");
            Assert.That(
                EmptyMinionSpawnPointQuery.CountWithin(board, board, ThroneStandingSpace, 2),
                Is.EqualTo(2),
                "The designer's 2026-07-25 ruling: Dodger herself need not be in the Battle Zone, and " +
                "only the counted spawn points must be. From (2, 0) her Radius-2 disk reaches the " +
                "melee point on (1, 0) and the melee point on (0, 2), both in the Zone - and also the " +
                "Throne Zone's own minion point one space away, which is excluded. Two is the number " +
                "\"2 or more empty spawn points in radius in the battle zone\" asks for.");
        }

        /// <summary>
        /// p.19's "empty": a Spawn point in an empty space, and p.18 makes any occupant an obstacle. A
        /// minion standing on a qualifying point removes it from the count and the count is live, so it
        /// comes back the moment the minion leaves.
        /// </summary>
        [Test]
        public void AnOccupiedSpawnPoint_StopsQualifying()
        {
            var occupied = CreateBoard(Dodger, Origin, MeleeSpawnNear, MeleeSpawnFar);

            Assert.That(
                EmptyMinionSpawnPointQuery.CountWithin(occupied, occupied, Origin, 2), Is.EqualTo(2),
                "Two of the four qualifying points now hold a minion, so p.19's \"empty\" excludes " +
                "them - which is what makes her whole deck get better as the board empties out.");
            Assert.That(
                EmptyMinionSpawnPointQuery.FindInCurrentBattleZone(occupied, occupied),
                Has.No.Member(MeleeSpawnNear));
        }

        /// <summary>
        /// The disk is p.9's and it is inclusive, so a smaller printed Radius reaches fewer points. This
        /// is the case that separates a radius question from a whole-Zone one.
        /// </summary>
        [Test]
        public void TheDisk_IsThePrintedRadiusAndNotTheWholeZone()
        {
            var board = CreateBoard(Dodger, Origin);

            Assert.That(EmptyMinionSpawnPointQuery.CountWithin(board, board, Origin, 1), Is.EqualTo(2),
                "At Radius 1 only the melee point on (1, 0) and the ranged point on (-1, 0) are in " +
                "reach; the hero point on (0, 1) is also one space away and is still not counted.");
            Assert.That(EmptyMinionSpawnPointQuery.CountWithin(board, board, Origin, 0), Is.EqualTo(0),
                "A disk of zero reaches her own space, which carries no spawn point.");
        }

        /// <summary>
        /// The adjacency form the two Gold cards ask, which is the same question at a distance of one.
        /// </summary>
        [Test]
        public void Adjacency_IsTheInnermostRingAndAnswersTheGoldCardsClause()
        {
            var beside = CreateBoard(Dodger, Origin);
            Assert.That(EmptyMinionSpawnPointQuery.AnyWithin(beside, beside, Origin, 1), Is.True);

            // (0, -2) is two spaces from every qualifying point on this board, so a hero standing there
            // is adjacent to none of them.
            var away = CreateBoard(Dodger, new HexCoordinate(0, -2));
            Assert.That(
                EmptyMinionSpawnPointQuery.AnyWithin(away, away, new HexCoordinate(0, -2), 1), Is.False,
                "\"If you are adjacent to an empty spawn point in the battlezone\" is false here, and " +
                "the user's RULING 3 makes a false condition skip rather than fail.");
        }

        /// <summary>
        /// A board with no Battle Zone at all - every synthetic map - answers none rather than throwing
        /// or answering everything. A condition that cannot be satisfied is simply not satisfied.
        /// </summary>
        [Test]
        public void ABoardWithNoBattleZone_QualifiesNothing()
        {
            var synthetic = new GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority(
                new[] { Origin, new HexCoordinate(1, 0) },
                new[] { new GuardsDigital.Core.Units.Heroes.HeroOccupancy(Dodger, Origin) });

            Assert.That(
                EmptyMinionSpawnPointQuery.FindInCurrentBattleZone(synthetic, synthetic),
                Is.Empty);
            Assert.That(
                EmptyMinionSpawnPointQuery.CountWithin(synthetic, synthetic, Origin, 4), Is.EqualTo(0));
        }

        /// <summary>
        /// The board itself is what the two rulings are measured against, so the fixture's own claim
        /// about it is asserted rather than trusted. A map edit that quietly moved a spawn point would
        /// otherwise make every case above pass for the wrong reason.
        /// </summary>
        [Test]
        public void TheFixtureBoard_PosesBothRulingsAtOnce()
        {
            var map = CreateMap();

            Assert.That(map.SpawnPoints, Has.Count.EqualTo(6));
            Assert.That(
                map.SpawnPoints.Count(point =>
                    point.UnitType == "hero" &&
                    map.GetZoneForSpace(point.SpaceId).Id.Equals(BattleZone)),
                Is.EqualTo(1),
                "RULING 4 is only measurable where a hero spawn point stands inside the Battle Zone.");
            Assert.That(
                map.SpawnPoints.Count(point =>
                    point.UnitType != "hero" &&
                    map.GetZoneForSpace(point.SpaceId).Id.Equals(ThroneZone)),
                Is.EqualTo(1),
                "The Zone half is only measurable where a minion spawn point stands outside it.");
            Assert.That(
                ThroneCoordinates.Select(value => HexDistanceQuery.Calculate(Origin, value)),
                Is.All.EqualTo(2),
                "The Throne Zone sits at exactly the printed Radius the Defense cards use, so a hero " +
                "standing in it has Battle-Zone points in reach and Throne points in reach at once.");
        }
    }
}
