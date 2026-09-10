using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.38a. The two printed-target narrowings Swift's Red line needs, proved as
    /// vocabulary before any of her content exists.
    ///
    /// <para><b>What is new and what is not.</b> <see cref="AttackTargetLine.NotInStraightLine"/> has
    /// been here since milestone 8.10 because both of Wasp's Boomerangs print it;
    /// <see cref="AttackTargetLine.InStraightLine"/> is its complement and is what Snipe, Prepared
    /// Shot and Killshot print. <see cref="AttackTargetReach.PrintedRangeBeyondAdjacent"/> is the
    /// fourth band this build reads over one printed number - beside adjacency alone, the ceiling,
    /// and the ceiling exactly - and is Prepared Shot's and Killshot's "not adjacent to you".</para>
    ///
    /// <para><b>Why the assertions are set relationships rather than named units.</b> A case that
    /// names one placed unit proves the query answered that unit; the milestone's claim is that one
    /// query answers every declaration consistently, and the honest form of that is
    /// <c>InStraightLine</c> and <c>NotInStraightLine</c> partitioning the default set exactly.
    /// Naming units on top of it is what makes the partition non-vacuous, so both are here, and every
    /// case asserts its own board is discriminating before it asserts anything about the answer.</para>
    /// </summary>
    public sealed class PrintedTargetLineAndProximityTests
    {
        private const int RangeTotal = 3;

        // ---------------------------------------------------------------------------------------
        // The declarations themselves
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// p.19 counts an adjacent space as a straight line, so an adjacent-only Attack restricted TO
        /// the straight line admits exactly what it would have admitted anyway. It is refused for a
        /// reason of its own rather than for the impossibility its complement is refused for: a policy
        /// that declares a narrowing while narrowing nothing would make
        /// <see cref="PrintedAttackTargetPolicy.NarrowsTheDefault"/> report one that is not there.
        /// </summary>
        [Test]
        public void RestrictingAnAdjacentOnlyAttackToTheStraightLine_IsRefusedAsANarrowingThatNarrowsNothing()
        {
            var message = Assert.Throws<ArgumentException>(() => new PrintedAttackTargetPolicy(
                AttackTargetableUnits.HeroesAndMinions,
                AttackTargetReach.AdjacentOnly,
                AttackTargetLine.InStraightLine)).Message;

            Assert.That(message, Does.Contain("narrows nothing"));
            Assert.That(message, Does.Contain("p.19"),
                "and the refusal cites the page it comes from, as its complement's does.");
        }

        /// <summary>
        /// The pairing Prepared Shot actually prints, which the refusal above must not catch: the
        /// straight line beside a reach that is not adjacency.
        /// </summary>
        [Test]
        public void TheStraightLineBesideAReachThatIsNotAdjacency_IsAcceptedBecauseAFacePrintsIt()
        {
            var policy = PrintedAttackTargetPolicy
                .ForClassification(AttackRangeKind.Ranged)
                .RestrictedToTheStraightLine()
                .BeyondAdjacentWithinThePrintedRange();

            Assert.That(policy.Line, Is.EqualTo(AttackTargetLine.InStraightLine));
            Assert.That(policy.Reach, Is.EqualTo(AttackTargetReach.PrintedRangeBeyondAdjacent));
            Assert.That(policy.TargetableUnits, Is.EqualTo(AttackTargetableUnits.HeroesAndMinions),
                "\"a unit\" is p.13's default union, so neither clause touches it.");
            Assert.That(policy.NarrowsTheDefault(AttackRangeKind.Ranged), Is.True);
        }

        /// <summary>
        /// Snipe's pairing: the exact band and the file together. Both are reach-and-line rather than
        /// one compound member, which is what lets Snipe and Prepared Shot share the line clause while
        /// differing on the band.
        /// </summary>
        [Test]
        public void TheExactBandAndTheStraightLine_ComposeWithoutEitherDisplacingTheOther()
        {
            var policy = PrintedAttackTargetPolicy
                .ForClassification(AttackRangeKind.Ranged)
                .AtExactlyThePrintedRange()
                .RestrictedToTheStraightLine();

            Assert.That(policy.Reach, Is.EqualTo(AttackTargetReach.ExactlyPrintedRange));
            Assert.That(policy.Line, Is.EqualTo(AttackTargetLine.InStraightLine));
        }

        /// <summary>
        /// The new band reads the card's Range exactly as p.13's ceiling does, so an Attack declaring
        /// it cannot be built without one.
        ///
        /// <para><b>Which rule refuses it is worth stating, because it is not the one this milestone
        /// extended.</b> <see cref="ActionDefinition"/> already requires every Ranged Attack to print
        /// exactly one Attack and one Range value, and that rule runs first - so a Ranged Attack
        /// declaring this band over no Range never reaches the target-policy refusal at all. The
        /// milestone extends that later refusal anyway, on the same defence-in-depth terms its two
        /// existing members are written on, and this case asserts the refusal that actually fires
        /// rather than the one the code reads as if it were guarding.</para>
        /// </summary>
        [Test]
        public void TheBeyondAdjacentBand_CannotBeDeclaredByAnAttackThatPrintsNoRange()
        {
            var message = Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("probe-attack"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.Ranged,
                default,
                null,
                PrintedTextComposition.None,
                PrintedDefenseResolution.None,
                CardTextTiming.None,
                PrintedAttackTargetPolicy
                    .ForClassification(AttackRangeKind.Ranged)
                    .BeyondAdjacentWithinThePrintedRange())).Message;

            Assert.That(
                message,
                Does.Contain("require exactly one Attack and one Range printed value"),
                "The printed-value rule is the one that fires, and it fires before the policy is " +
                "looked at. Both say the same thing about the same card.");
        }

        // ---------------------------------------------------------------------------------------
        // One query, read three ways, on one board
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// The partition. Every enemy unit p.13's default Ranged Attack admits is admitted by exactly
        /// one of the two line declarations, because both read the same
        /// <see cref="StraightLineQuery"/> membership question from the attacker's own space.
        /// </summary>
        [Test]
        public void TheTwoLineDeclarations_PartitionThePageThirteenDefaultExactly()
        {
            var board = Board();

            var any = Targets(board, Policy(AttackTargetLine.AnyLine));
            var onFile = Targets(board, Policy(AttackTargetLine.InStraightLine));
            var offFile = Targets(board, Policy(AttackTargetLine.NotInStraightLine));

            Assert.That(onFile, Is.Not.Empty, "The board has to hold a unit on a file through her.");
            Assert.That(offFile, Is.Not.Empty, "and one off every file, or the partition is vacuous.");
            Assert.That(onFile.Intersect(offFile), Is.Empty,
                "A unit cannot be both on and off the attacker's file.");
            Assert.That(
                onFile.Concat(offFile).OrderBy(key => key, StringComparer.Ordinal),
                Is.EqualTo(any.OrderBy(key => key, StringComparer.Ordinal)),
                "and between them the two declarations admit the whole default set - which is what " +
                "makes them one query read two ways rather than two queries.");
        }

        /// <summary>
        /// The band. "In range and not adjacent to you" is p.13's ceiling with p.19's separation-of-one
        /// taken out from under it, and the case asserts both halves: nothing at distance 1 survives,
        /// and everything else the default admits does.
        /// </summary>
        [Test]
        public void TheBeyondAdjacentBand_IsThePageThirteenCeilingWithoutItsAdjacentUnits()
        {
            var board = Board();
            var origin = ActorCoordinate(board);

            var any = Targets(board, Policy(AttackTargetLine.AnyLine));
            var beyond = Targets(
                board,
                Policy(AttackTargetLine.AnyLine).BeyondAdjacentWithinThePrintedRange());

            var adjacent = any
                .Where(key => HexDistanceQuery.Calculate(origin, CoordinateOfKey(board, key)) == 1)
                .ToArray();

            Assert.That(adjacent, Is.Not.Empty,
                "The board has to stand an enemy beside her, or the band excludes nothing here.");
            Assert.That(beyond, Is.Not.Empty, "and one further off, or it admits nothing here.");
            Assert.That(beyond.Intersect(adjacent), Is.Empty,
                "Nothing at p.19's separation of one survives the band.");
            Assert.That(
                beyond.OrderBy(key => key, StringComparer.Ordinal),
                Is.EqualTo(any.Except(adjacent).OrderBy(key => key, StringComparer.Ordinal)),
                "and everything else the ceiling admits does, so the band removes exactly the " +
                "adjacent units and nothing besides.");
        }

        /// <summary>
        /// Prepared Shot's whole sentence at once. The clauses are independent, so the answer is the
        /// intersection of the two answers - which is a stronger statement than any board could make
        /// on its own, and is what a card composing both must be able to rely on.
        /// </summary>
        [Test]
        public void BothClausesTogether_AdmitExactlyTheIntersectionOfTheirSeparateAnswers()
        {
            var board = Board();

            var onFile = Targets(board, Policy(AttackTargetLine.InStraightLine));
            var beyond = Targets(
                board,
                Policy(AttackTargetLine.AnyLine).BeyondAdjacentWithinThePrintedRange());
            var both = Targets(
                board,
                Policy(AttackTargetLine.InStraightLine).BeyondAdjacentWithinThePrintedRange());

            Assert.That(
                both.OrderBy(key => key, StringComparer.Ordinal),
                Is.EqualTo(onFile.Intersect(beyond).OrderBy(key => key, StringComparer.Ordinal)));
            Assert.That(both, Is.Not.EqualTo(onFile),
                "and the two clauses are not the same clause on this board, or the case proves " +
                "nothing about composing them.");
        }

        /// <summary>
        /// Snipe's whole sentence. The exact band and the file are also independent, and the case that
        /// matters is that a unit standing at the right distance off every file is refused - which is
        /// the difference between "at maximum range" alone and what Snipe prints.
        /// </summary>
        [Test]
        public void TheExactBandOnTheFile_RefusesAUnitAtTheRightDistanceOffEveryFile()
        {
            var board = Board();
            var origin = ActorCoordinate(board);

            var exact = Targets(
                board, Policy(AttackTargetLine.AnyLine).AtExactlyThePrintedRange());
            var exactOnFile = Targets(
                board,
                Policy(AttackTargetLine.InStraightLine).AtExactlyThePrintedRange());

            Assert.That(
                exact.All(key =>
                    HexDistanceQuery.Calculate(origin, CoordinateOfKey(board, key)) == RangeTotal),
                Is.True,
                "The band is the band whatever the line says.");
            Assert.That(
                exactOnFile.All(key =>
                    StraightLineQuery.AreInStraightLine(origin, CoordinateOfKey(board, key))),
                Is.True);
            Assert.That(exactOnFile, Is.SubsetOf(exact));
        }

        // ---------------------------------------------------------------------------------------
        // The board, and the keys the cases compare
        // ---------------------------------------------------------------------------------------

        private static PrintedAttackTargetPolicy Policy(AttackTargetLine line)
        {
            var ranged = PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.Ranged);
            switch (line)
            {
                case AttackTargetLine.AnyLine:
                    return ranged;
                case AttackTargetLine.InStraightLine:
                    return ranged.RestrictedToTheStraightLine();
                case AttackTargetLine.NotInStraightLine:
                    return ranged.ExcludingTheStraightLine();
                default:
                    throw new ArgumentOutOfRangeException(nameof(line), line, null);
            }
        }

        /// <summary>
        /// Milestone 8.10's discriminating board, reused unchanged: the Blue hero stands beside the Red
        /// one and a Blue melee minion stands inside Range 3 on no file through her. It already
        /// separates every clause these cases ask about, so nothing new is staged for them.
        /// </summary>
        private static ProbeBoard Board()
        {
            var session = ConfiguredMatchFixtures.CreateBoomerangSession(
                WaspCardContent.ChargedBoomerang, out _);
            var actorId = session.Projection.Heroes
                .Single(occupancy =>
                    session.Projection.Participants.GetTeamForHero(occupancy.HeroId).Side ==
                    TeamSide.Red)
                .HeroId;

            return new ProbeBoard(session, actorId);
        }

        private static HexCoordinate ActorCoordinate(ProbeBoard board) =>
            board.Session.Projection.Heroes
                .Single(occupancy => occupancy.HeroId.Equals(board.ActorId)).Coordinate;

        private static string[] Targets(ProbeBoard board, PrintedAttackTargetPolicy policy) =>
            AttackTargetQuery
                .FindLegalEnemyTargets(
                    board.ActorId,
                    policy,
                    RangeTotal,
                    board.Session.Projection.Participants,
                    board.Restored.Board,
                    RuleModificationSet.Empty)
                .Select(Key)
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToArray();

        private static string Key(AttackTarget target) =>
            target.Kind == AttackTargetKind.Hero
                ? "hero:" + target.HeroId.Value
                : "minion:" + target.MinionId.Value;

        private static HexCoordinate CoordinateOfKey(ProbeBoard board, string key)
        {
            var projection = board.Session.Projection;
            var value = key.Substring(key.IndexOf(':') + 1);
            return key.StartsWith("hero:", StringComparison.Ordinal)
                ? projection.Heroes.Single(occupancy => occupancy.HeroId.Value == value).Coordinate
                : projection.Minions.Single(occupancy => occupancy.MinionId.Value == value).Coordinate;
        }

        private sealed class ProbeBoard
        {
            public ProbeBoard(MatchSession session, HeroId actorId)
            {
                Session = session;
                ActorId = actorId;
                Restored = AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(),
                    session.Projection.AuthoritativeSnapshot,
                    session.Projection.Definitions,
                    new[] { session.Projection.SelectedMap },
                    session.Projection.Setup.Minions);
            }

            public MatchSession Session { get; }

            public HeroId ActorId { get; }

            public RestoredAuthoritativeMatch Restored { get; }
        }
    }
}
