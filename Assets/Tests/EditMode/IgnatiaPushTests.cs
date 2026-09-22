using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.75c. Ignatia the Mad's Blue PUSH line - Searing Heat and Scorching Blaze - the two
    /// faces whose coin arms differ only in WHOSE hero is moved.
    ///
    /// <list type="bullet">
    /// <item><description>Searing Heat: "☀ Move a friendly hero in radius 2 spaces in a straight line.
    /// ☾ Move an enemy hero in radius 2 spaces in a straight line."</description></item>
    /// <item><description>Scorching Blaze: the same at "2 or 3 spaces".</description></item>
    /// </list>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    [TestFixture]
    public sealed class IgnatiaPushTests
    {
        private static readonly HexCoordinate AllyAt = new HexCoordinate(0, 2);
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(2, 0);
        private static readonly HexCoordinate FarAway = new HexCoordinate(0, 7);

        /// <summary>
        /// One friendly hero and one enemy hero inside the printed Radius, one enemy hero far outside
        /// it, and one friendly minion beside her so the KIND the sentence names can be measured.
        /// </summary>
        private static AdjacentMoveAuthority Board(int radius = 7) =>
            IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, AllyAt),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, EnemyAt),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, FarAway)
                },
                minions: new[]
                {
                    IgnatiaBoard.BlueMinion(IgnatiaBoard.FriendlyMinion, new HexCoordinate(-1, 0)),
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, new HexCoordinate(0, -1))
                },
                radius: radius);

        // -------------------------------------------------------------------------------------------
        // Whose hero the coin names
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The one difference between her two printed lines, and the whole of it: the SIDE. Both faces
        /// print the same reach and the same distance, so the coin decides only whether she is helping
        /// a teammate along or shoving an opponent.
        /// </summary>
        [Test]
        public void TheCoinDecidesWhoseHeroMoves_AndNothingElseAboutTheSentence()
        {
            var sun = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Sun, Board());
            Assert.That(sun.Choice.NodeId.Value, Is.EqualTo("ignatia-push-sun-select"));
            Assert.That(sun.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.Ally.Value }),
                "☀ \"a friendly hero in radius\" - her teammate, and nobody else.");

            var moon = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Moon, Board());
            Assert.That(moon.Choice.NodeId.Value, Is.EqualTo("ignatia-push-moon-select"));
            Assert.That(moon.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.Enemy.Value }),
                "☾ \"an enemy hero in radius\" - the enemy inside the disk, and not the one seven " +
                "spaces away.");
        }

        /// <summary>
        /// p.17: "friendly" never includes you. The sun line could otherwise name the acting hero
        /// herself, which would turn a support Skill into a free two-space Movement - and the printed
        /// word says it does not.
        ///
        /// <para>The kind is stated in the same breath, because "hero" is a narrower word than p.19's
        /// "unit": a friendly MINION standing beside her is not a subject this sentence has, and her
        /// Green line's swap - which does print "unit" - is where minions belong.</para>
        /// </summary>
        [Test]
        public void SheIsNeverHerOwnFriendlyHero_AndNeitherIsAMinion()
        {
            var sun = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Sun, Board());

            Assert.That(sun.Candidates(), Has.No.Member(IgnatiaBoard.Ignatia.Value),
                "\"a friendly hero\" is somebody else - p.17's own word, and the reason the " +
                "selection declares includeSelf false.");
            Assert.That(sun.Candidates(), Has.No.Member(IgnatiaBoard.FriendlyMinion.Value),
                "and a friendly minion one space away is the wrong KIND: the card prints \"hero\".");

            var moon = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Moon, Board());
            Assert.That(moon.Candidates(), Has.No.Member(IgnatiaBoard.EnemyMinion.Value),
                "and so is an enemy minion on the moon face, for the same printed word.");
        }

        /// <summary>
        /// The printed RADIUS is what bounds the selection, and it is the card's own icon: Searing Heat
        /// prints 3 and Scorching Blaze prints 3, so a hero four spaces out is nobody's subject until a
        /// p.16 Radius item says otherwise.
        /// </summary>
        [Test]
        public void ThePrintedRadiusBoundsWhoMayBeMoved_AndAnItemWidensIt()
        {
            var participants = IgnatiaBoard.Participants();
            var board = IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, new HexCoordinate(4, 0)),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, new HexCoordinate(0, 6))
                });

            var bare = new IgnatiaFrame(
                IgnatiaCardContent.SearingHeat, IgnatiaBoard.Moon, board, participants);
            Assert.That(bare.Choice, Is.Null,
                "Radius 3 does not reach a hero standing four spaces out, so the sentence names " +
                "nobody and nobody is asked.");

            var widened = new IgnatiaFrame(
                IgnatiaCardContent.SearingHeat, IgnatiaBoard.Moon, board, participants,
                items: IgnatiaBoard.Items(participants, PrintedValueChannel.Radius, 1));
            Assert.That(widened.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.Enemy.Value }),
                "and Radius 3 + 1 does, which is p.16's item composing onto the printed icon exactly " +
                "as it does for every registered printed-Radius selection.");
        }

        // -------------------------------------------------------------------------------------------
        // How far
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "2 spaces" is a COUNT, not a ceiling: every destination Searing Heat offers is exactly two
        /// away, and one space away is not a shorter version of it.
        /// </summary>
        [Test]
        public void SearingHeatMovesExactlyTwo_AndOffersNothingCloser()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Sun, Board());
            frame.Pick(IgnatiaBoard.Ally);

            Assert.That(frame.Choice.NodeId.Value, Is.EqualTo("ignatia-push-sun-line"),
                "Milestone 8.75b: the destination question is the straight-line OFFER's, and the " +
                "Move behind it is pinned to the one space that offer named.");
            Assert.That(
                frame.Spaces().Select(space => HexDistanceQuery.Calculate(AllyAt, space)).Distinct(),
                Is.EqualTo(new[] { 2 }),
                "Every offered destination is exactly two spaces from where the hero stands - " +
                "milestone 8.59's correction, which is that an offer with only a ceiling lights " +
                "spaces the card cannot move to.");
        }

        /// <summary>
        /// "2 or 3 spaces" is two counts, so Scorching Blaze offers both distances and still nothing
        /// shorter - the one printed difference between the Tier II and the Tier III of this line.
        /// </summary>
        [Test]
        public void ScorchingBlazeMovesTwoOrThree_AndStillNothingCloser()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.ScorchingBlaze, IgnatiaBoard.Sun, Board());
            frame.Pick(IgnatiaBoard.Ally);

            var distances = frame.Spaces()
                .Select(space => HexDistanceQuery.Calculate(AllyAt, space))
                .Distinct()
                .OrderBy(value => value)
                .ToArray();

            Assert.That(distances, Is.EqualTo(new[] { 2, 3 }),
                "\"2 or 3 spaces\" - both, and only those two.");
            Assert.That(
                frame.Spaces(), Is.SupersetOf(
                    new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Sun, Board())
                        .Then(IgnatiaBoard.Ally)
                        .Spaces()),
                "and the Tier III's offer contains the Tier II's, because the only thing the upgrade " +
                "changes about this clause is the extra space.");
        }

        /// <summary>
        /// The move actually happens: the hero the sentence named stands where the answer said, and
        /// nobody else has moved.
        /// </summary>
        [Test]
        public void TheNamedHeroActuallyMoves_AndTheActorDoesNot()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Sun, Board());
            frame.Pick(IgnatiaBoard.Ally);

            var destination = new HexCoordinate(0, 4);
            Assert.That(frame.Spaces(), Has.Member(destination));
            frame.Pick(destination);

            Assert.That(frame.Step.IsComplete, Is.True);
            Assert.That(frame.Where(IgnatiaBoard.Ally), Is.EqualTo(destination));
            Assert.That(frame.Where(IgnatiaBoard.Ignatia), Is.EqualTo(IgnatiaBoard.Origin),
                "and the acting hero has not moved a space: the card moves somebody else.");
            Assert.That(frame.Where(IgnatiaBoard.Enemy), Is.EqualTo(EnemyAt),
                "nor has anybody the sentence did not name.");
        }

        // -------------------------------------------------------------------------------------------
        // What stops it
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.18's obstacles stop the move like any other: a space whose every route is blocked is not
        /// offered, and the occupied spaces themselves never are.
        /// </summary>
        [Test]
        public void AnOccupiedSpaceIsNeverADestination_AndAnObstacleClosesTheRoutesThroughIt()
        {
            var open = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Sun, Board());
            open.Pick(IgnatiaBoard.Ally);

            Assert.That(open.Spaces(), Has.No.Member(IgnatiaBoard.Origin),
                "The acting hero is standing on it, and p.18 makes a hero an obstacle.");
            Assert.That(open.Spaces(), Has.No.Member(EnemyAt),
                "and so is the enemy hero two spaces the other way.");

            // Box the ally in on every side but one: five of her six neighbours hold an obstacle, so
            // every two-space route has to leave through the sixth.
            var neighbours = AllyAt.GetNeighbors().ToArray();
            var boxed = IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, AllyAt),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, EnemyAt),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, FarAway)
                },
                obstacles: neighbours.Where(space => !space.Equals(new HexCoordinate(0, 3))).ToArray());

            var blocked = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Sun, boxed);
            blocked.Pick(IgnatiaBoard.Ally);

            Assert.That(
                blocked.Spaces(),
                Is.EquivalentTo(new[] { new HexCoordinate(0, 4) }),
                "Only the one open neighbour can be stepped into, and the printed \"straight line\" " +
                "then leaves exactly ONE destination: the space beyond it on the same file. An " +
                "obstacle closes a route rather than shortening the count, and the two corner-turning " +
                "spaces beyond the same opening - (-1,4) and (1,3) - are not offered at all, which " +
                "is milestone 8.75b carrying the clause the composition used to drop.");
        }

        /// <summary>
        /// The board edge stops it too, and for a different reason: p.19 makes the water around the map
        /// terrain, so a space off the map is not a destination at all rather than a blocked one.
        /// </summary>
        [Test]
        public void TheBoardEdgeStopsIt_BecauseOffTheMapIsNoSpace()
        {
            // A disk of three: the ally stands on the rim, so three of her six files run off the map
            // within two steps.
            var rim = new HexCoordinate(0, 3);
            var board = IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, rim),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, new HexCoordinate(3, 0)),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, new HexCoordinate(-3, 0))
                },
                radius: 3);

            var frame = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Sun, board);
            frame.Pick(IgnatiaBoard.Ally);

            Assert.That(
                frame.Spaces().All(space => HexDistanceQuery.Calculate(IgnatiaBoard.Origin, space) <= 3),
                Is.True,
                "Every destination is on the map; the spaces two steps out over the water are not " +
                "refused, they do not exist.");
            Assert.That(
                frame.Spaces(), Has.No.Member(new HexCoordinate(0, 5)),
                "which is why the space straight on past the rim is absent rather than blocked.");
        }

        /// <summary>
        /// An arm whose selection names nobody refuses, and the other printed line is not run instead -
        /// the same claim her Attacks make, stated on the line where "friendly" and "enemy" make it
        /// easiest to reach: a board with no teammate in the disk.
        /// </summary>
        [Test]
        public void WithNoFriendlyHeroInRadius_TheSunLineDoesNothingAndTheMoonLineIsNotRun()
        {
            var board = IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, EnemyAt),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, new HexCoordinate(0, 6))
                });

            var sun = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Sun, board);
            Assert.That(sun.Choice, Is.Null);
            Assert.That(
                sun.Outcome("ignatia-push-sun-select"),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(
                sun.Outcome("ignatia-push-moon-select"),
                Is.EqualTo(EffectExecutionResultKind.None),
                "and the enemy standing two spaces away is not reached by the line the coin did not " +
                "name.");
            Assert.That(sun.Where(IgnatiaBoard.Enemy), Is.EqualTo(EnemyAt));
        }

        // -------------------------------------------------------------------------------------------
        // The printed straight line
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "In a straight line" - CARRIED at milestone 8.75b, and this is the case the 8.75 fixtures
        /// left behind to say so.
        ///
        /// <para><b>What it replaces.</b> The composition landed at 8.75 on
        /// <c>synthetic-direct-move@3</c> under
        /// <see cref="GuardsDigital.Core.Rules.Displacement.DirectMoveDestinationPolicy.Unrestricted"/>,
        /// which is p.11's ordinary Movement reachability and turns corners: on this very board it
        /// offered TEN spaces where the six files reach four. The defect was recorded rather than
        /// fixed because the build had no SUBJECT-anchored straight-line displacement -
        /// <c>choose-line-destination@1</c> walked the files from <c>context.ActorId</c>, which is
        /// right for a hero moving HERSELF and wrong for a hero moving somebody else - and the
        /// recorded case was written to FAIL the day the clause was carried, which is the signal this
        /// case is the other side of.</para>
        ///
        /// <para>The correction is that contract's own <c>subjectSpace</c> anchor, bound from the
        /// hero the selection named. The offer is now exactly p.19's six files from the SUBJECT, so
        /// the two sets below are the same set - and the four that survive are four rather than six
        /// because the acting hero and the enemy are standing on two of them.</para>
        /// </summary>
        [Test]
        public void ThePrintedStraightLineIsCarried_AndOnlyTheSixFilesAreOffered()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Sun, Board());
            frame.Pick(IgnatiaBoard.Ally);

            var offered = frame.Spaces();
            var onAFile = offered
                .Where(space => StraightLineQuery.AreInStraightLine(AllyAt, space))
                .ToArray();

            Assert.That(offered, Is.EquivalentTo(onAFile),
                "Every offered destination lies on one of p.19's six files FROM THE HERO BEING " +
                "MOVED, which is what the printed \"in a straight line\" names - and the ten-space " +
                "reachability disk the 8.75 composition offered is gone.");
            Assert.That(offered.Length, Is.EqualTo(4),
                "Four rather than six: the six files reach six spaces at distance two, and the " +
                "acting hero and the enemy hero are standing on two of them.");
            Assert.That(
                offered.All(space => HexDistanceQuery.Calculate(AllyAt, space) == 2), Is.True,
                "and the count is still exactly two, because the line carries the printed distance " +
                "rather than replacing it.");
        }

        /// <summary>
        /// The anchor is the SUBJECT's and nobody else's - the half of milestone 8.75b's widening that
        /// a fixture has to state out loud, because an offer measured from the wrong hero would still
        /// look like six files.
        ///
        /// <para>Ignatia stands at the origin and her teammate two spaces away on the (0,+1) file. The
        /// spaces on HER files are not the spaces on HIS, and the offer contains only his: the space
        /// two steps past her own shoulder is on a file of hers and on none of his, so it is absent,
        /// and the space two steps past him is on a file of his that continues one of hers, so it is
        /// present. That pair is the whole claim.</para>
        /// </summary>
        [Test]
        public void TheLineIsWalkedFromTheChosenHero_AndNotFromTheActor()
        {
            var frame = new IgnatiaFrame(IgnatiaCardContent.SearingHeat, IgnatiaBoard.Sun, Board());
            frame.Pick(IgnatiaBoard.Ally);

            var offered = frame.Spaces();

            Assert.That(offered, Has.Member(new HexCoordinate(0, 4)),
                "Two steps on from where HE stands, on his own (0,+1) file.");
            Assert.That(offered, Has.No.Member(new HexCoordinate(0, -2)),
                "and two steps out on a file of HERS is not a destination at all: it is four spaces " +
                "from him, which no reading of \"2 spaces\" admits.");
            Assert.That(
                offered.All(space => StraightLineQuery.AreInStraightLine(AllyAt, space)), Is.True,
                "Every offer is on one of his files. Anchoring at the actor would have kept the " +
                "spaces on hers, which is the bug this parameter exists to make impossible.");
        }
    }

    /// <summary>
    /// A one-line continuation so a case can answer a selection inside an expression - used where the
    /// second offer of the SAME card is the thing being compared rather than the subject of the case.
    /// </summary>
    internal static class IgnatiaFrameContinuation
    {
        internal static IgnatiaFrame Then(this IgnatiaFrame frame, HeroId hero)
        {
            frame.Pick(hero);
            return frame;
        }
    }
}
