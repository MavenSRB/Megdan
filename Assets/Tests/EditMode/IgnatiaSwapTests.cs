using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.75c. Ignatia the Mad's Green SWAP line - Unstable Portal and Chaos Gate - the half
    /// of her Green Tier ladder that exchanges her with somebody rather than laying magma.
    ///
    /// <list type="bullet">
    /// <item><description>Unstable Portal: "☀ Swap with a friendly unit in radius. ☾ Swap with an
    /// enemy unit in radius."</description></item>
    /// <item><description>Chaos Gate: the same swap, and then "☀ you may move that unit 1 space" /
    /// "☾ you may move 1 space" - two different movers on the two faces, which is the one thing about
    /// this pair a reading can get backwards.</description></item>
    /// </list>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    [TestFixture]
    public sealed class IgnatiaSwapTests
    {
        private static readonly HexCoordinate AllyAt = new HexCoordinate(2, 0);
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(0, 2);
        private static readonly HexCoordinate EnemyMinionAt = new HexCoordinate(-2, 0);
        private static readonly HexCoordinate FriendlyMinionAt = new HexCoordinate(0, -2);
        private static readonly HexCoordinate FarAway = new HexCoordinate(0, 7);

        /// <summary>
        /// One hero and one minion on each side inside the printed Radius 4, and one enemy hero far
        /// outside it - so "a unit" can be measured as both of p.19's kinds and the disk as a bound.
        /// </summary>
        private static AdjacentMoveAuthority Board() =>
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
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, EnemyMinionAt),
                    IgnatiaBoard.BlueMinion(IgnatiaBoard.FriendlyMinion, FriendlyMinionAt)
                });

        // -------------------------------------------------------------------------------------------
        // Who she may exchange with
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "a unit" is p.19's word for a hero OR a minion, and the coin picks the SIDE. Both kinds are
        /// offered on the face whose side they stand on and neither on the other, which is the whole of
        /// what these two lines differ in.
        /// </summary>
        [Test]
        public void TheCoinPicksTheSide_AndAUnitIsAHeroOrAMinion()
        {
            var sun = new IgnatiaFrame(IgnatiaCardContent.UnstablePortal, IgnatiaBoard.Sun, Board());
            Assert.That(sun.Choice.NodeId.Value, Is.EqualTo("ignatia-swap-sun-select"));
            Assert.That(
                sun.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.Ally.Value, IgnatiaBoard.FriendlyMinion.Value
                }),
                "☀ \"a friendly unit in radius\" - her teammate and her side's minion, both kinds.");

            var moon = new IgnatiaFrame(IgnatiaCardContent.UnstablePortal, IgnatiaBoard.Moon, Board());
            Assert.That(moon.Choice.NodeId.Value, Is.EqualTo("ignatia-swap-moon-select"));
            Assert.That(
                moon.Candidates(),
                Is.EquivalentTo(new[]
                {
                    IgnatiaBoard.Enemy.Value, IgnatiaBoard.EnemyMinion.Value
                }),
                "☾ \"an enemy unit in radius\" - the exact mirror.");

            Assert.That(
                sun.Candidates().Intersect(moon.Candidates()), Is.Empty,
                "and no unit is on both sides, so neither offer could have come from the other arm.");
        }

        /// <summary>
        /// The printed Radius bounds it, and she is never her own subject: a Swap with herself is not a
        /// Swap, and the selection declares includeSelf false for the same reason her Blue push line
        /// does.
        /// </summary>
        [Test]
        public void ThePrintedRadiusBoundsIt_AndSheIsNeverHerOwnSubject()
        {
            var sun = new IgnatiaFrame(IgnatiaCardContent.UnstablePortal, IgnatiaBoard.Sun, Board());
            Assert.That(sun.Candidates(), Has.No.Member(IgnatiaBoard.Ignatia.Value));

            var moon = new IgnatiaFrame(IgnatiaCardContent.UnstablePortal, IgnatiaBoard.Moon, Board());
            Assert.That(moon.Candidates(), Has.No.Member(IgnatiaBoard.SecondEnemy.Value),
                "and the enemy seven spaces out is beyond the printed Radius 4.");
        }

        /// <summary>
        /// p.18: a unit that may not be targeted is not offered. The selection declares the Swap as its
        /// consumer, so an immune unit is absent from the question rather than accepted and then
        /// refused - which is what a declared consumer buys.
        /// </summary>
        [Test]
        public void AnImmuneUnitIsNotOffered()
        {
            var participants = IgnatiaBoard.Participants();
            var frame = new IgnatiaFrame(
                IgnatiaCardContent.UnstablePortal, IgnatiaBoard.Moon, Board(), participants,
                modifications: IgnatiaBoard.ImmunityFor(
                    participants, IgnatiaBoard.Enemy, RuleActivity.Skill));

            Assert.That(
                frame.Candidates(), Is.EqualTo(new[] { IgnatiaBoard.EnemyMinion.Value }),
                "p.12 makes a Skill's effect the action, so the immunity this hero carries against " +
                "Skills is the one this selection asks about - and the minion beside him is " +
                "untouched by it.");
        }

        // -------------------------------------------------------------------------------------------
        // What a Swap does
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.19's Swap: the two pieces exchange spaces, and nothing else on the board moves. Measured on
        /// both kinds, because a minion and a hero travel through different board ports.
        /// </summary>
        [Test]
        public void TheTwoPiecesExchangeSpaces_WhicheverKindWasNamed()
        {
            var withHero = new IgnatiaFrame(
                IgnatiaCardContent.UnstablePortal, IgnatiaBoard.Sun, Board());
            withHero.Pick(IgnatiaBoard.Ally);

            Assert.That(withHero.Step.IsComplete, Is.True);
            Assert.That(withHero.Where(IgnatiaBoard.Ignatia), Is.EqualTo(AllyAt));
            Assert.That(withHero.Where(IgnatiaBoard.Ally), Is.EqualTo(IgnatiaBoard.Origin));
            Assert.That(withHero.Where(IgnatiaBoard.Enemy), Is.EqualTo(EnemyAt),
                "and nobody the sentence did not name has moved.");

            var withMinion = new IgnatiaFrame(
                IgnatiaCardContent.UnstablePortal, IgnatiaBoard.Moon, Board());
            withMinion.Pick(IgnatiaBoard.EnemyMinion);

            Assert.That(withMinion.Step.IsComplete, Is.True);
            Assert.That(withMinion.Where(IgnatiaBoard.Ignatia), Is.EqualTo(EnemyMinionAt));
            Assert.That(withMinion.Where(IgnatiaBoard.EnemyMinion),
                Is.EqualTo(IgnatiaBoard.Origin));
        }

        /// <summary>
        /// An arm with nobody in it refuses and the other printed line is not run instead - the claim
        /// her Attacks and her pushes each make, on the third family that could hide it.
        /// </summary>
        [Test]
        public void WithNoFriendlyUnitInRadius_TheSunLineDoesNothingAndTheMoonLineIsNotRun()
        {
            var board = IgnatiaBoard.Board(
                new[]
                {
                    IgnatiaBoard.At(IgnatiaBoard.Ignatia, IgnatiaBoard.Origin),
                    IgnatiaBoard.At(IgnatiaBoard.Ally, FarAway),
                    IgnatiaBoard.At(IgnatiaBoard.Enemy, EnemyAt),
                    IgnatiaBoard.At(IgnatiaBoard.SecondEnemy, new HexCoordinate(0, 6))
                },
                minions: new[]
                {
                    IgnatiaBoard.RedMinion(IgnatiaBoard.EnemyMinion, EnemyMinionAt)
                });

            var frame = new IgnatiaFrame(IgnatiaCardContent.UnstablePortal, IgnatiaBoard.Sun, board);

            Assert.That(frame.Choice, Is.Null);
            Assert.That(
                frame.Outcome("ignatia-swap-sun-select"),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(
                frame.Outcome("ignatia-swap-moon-select"),
                Is.EqualTo(EffectExecutionResultKind.None));
            Assert.That(frame.Where(IgnatiaBoard.Ignatia), Is.EqualTo(IgnatiaBoard.Origin),
                "so she is standing exactly where she was.");
        }

        // -------------------------------------------------------------------------------------------
        // Chaos Gate's trailing space
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE ONE THING THIS PAIR CAN GET BACKWARDS. Chaos Gate's two faces move two DIFFERENT pieces:
        /// "☀ you may move THAT UNIT 1 space" and "☾ you may move 1 space", which is her. Measured by
        /// where the offered spaces are, because the two pieces have just exchanged places and the two
        /// readings therefore offer two disjoint rings.
        /// </summary>
        [Test]
        public void ChaosGateSunMovesTheSwappedUnit_AndItsMoonFaceMovesHer()
        {
            var sun = new IgnatiaFrame(IgnatiaCardContent.ChaosGate, IgnatiaBoard.Sun, Board());
            sun.Pick(IgnatiaBoard.Ally);

            Assert.That(sun.Choice, Is.Not.Null);
            Assert.That(sun.Choice.NodeId.Value, Is.EqualTo("ignatia-gate-sun-move"));
            Assert.That(
                sun.Spaces().All(space =>
                    HexDistanceQuery.Calculate(IgnatiaBoard.Origin, space) == 1),
                Is.True,
                "☀ moves THAT UNIT, and the swap has just put the teammate on the space she came " +
                "from - so every destination is one step from the ORIGIN.");
            Assert.That(sun.Where(IgnatiaBoard.Ally), Is.EqualTo(IgnatiaBoard.Origin));

            var moon = new IgnatiaFrame(IgnatiaCardContent.ChaosGate, IgnatiaBoard.Moon, Board());
            moon.Pick(IgnatiaBoard.Enemy);

            Assert.That(moon.Choice.NodeId.Value, Is.EqualTo("ignatia-gate-moon-move"));
            Assert.That(
                moon.Spaces().All(space => HexDistanceQuery.Calculate(EnemyAt, space) == 1),
                Is.True,
                "☾ moves HER, and the swap has just put her on the space the enemy came from - so " +
                "every destination is one step from THERE.");
            Assert.That(moon.Where(IgnatiaBoard.Ignatia), Is.EqualTo(EnemyAt));
        }

        /// <summary>
        /// "You MAY move 1 space" - and "may" means the whole step, so declining is a real answer that
        /// leaves the swap standing. Both faces, because both print the same word.
        /// </summary>
        [Test]
        public void ChaosGatesTrailingSpaceIsOptional_AndDecliningLeavesTheSwapStanding()
        {
            foreach (var coin in new[] { IgnatiaBoard.Sun, IgnatiaBoard.Moon })
            {
                var frame = new IgnatiaFrame(IgnatiaCardContent.ChaosGate, coin, Board());
                var subject = coin.Equals(IgnatiaBoard.Sun)
                    ? IgnatiaBoard.Ally
                    : IgnatiaBoard.Enemy;
                var swappedTo = coin.Equals(IgnatiaBoard.Sun) ? AllyAt : EnemyAt;
                frame.Pick(subject);

                Assert.That(frame.Choice.DeclineInvocation, Is.Not.Null,
                    "\"You may\" is declinable, which is what a `may` node's own policy puts on the " +
                    "pause.");
                frame.Decline();

                Assert.That(frame.Step.IsComplete, Is.True);
                Assert.That(frame.Where(IgnatiaBoard.Ignatia), Is.EqualTo(swappedTo),
                    "and the Swap the sentence performed first is untouched by declining the one " +
                    "that follows it - zero spaces is a legal answer to \"may move 1 space\".");
            }
        }

        /// <summary>
        /// Accepting it moves exactly one space, and moves the piece the face names.
        /// </summary>
        [Test]
        public void AcceptingChaosGatesSpace_MovesThatOnePieceExactlyOneSpace()
        {
            var sun = new IgnatiaFrame(IgnatiaCardContent.ChaosGate, IgnatiaBoard.Sun, Board());
            sun.Pick(IgnatiaBoard.Ally);

            var destination = sun.Spaces().First();
            sun.Pick(destination);

            Assert.That(sun.Step.IsComplete, Is.True);
            Assert.That(sun.Where(IgnatiaBoard.Ally), Is.EqualTo(destination));
            Assert.That(sun.Where(IgnatiaBoard.Ignatia), Is.EqualTo(AllyAt),
                "and she stays where the Swap put her: the sun face moves the unit, not her.");
            Assert.That(HexDistanceQuery.Calculate(IgnatiaBoard.Origin, destination), Is.EqualTo(1),
                "\"1 space\" is a count, so the move is exactly one.");
        }

        /// <summary>
        /// Unstable Portal prints no trailing move at all, which is what its Tier III upgrade adds -
        /// stated as an absence, because what a card does not say is absent rather than a shorter
        /// version of what the next Tier says.
        /// </summary>
        [Test]
        public void UnstablePortalPrintsNoTrailingMove_AndChaosGatePrintsOnePerFace()
        {
            Assert.That(
                IgnatiaBoard.PrimaryText(IgnatiaCardContent.UnstablePortal).Nodes
                    .Select(node => node.Id.Value),
                Is.EquivalentTo(new[]
                {
                    IgnatiaEffectContent.CoinGateNode,
                    "ignatia-swap-sun", "ignatia-swap-sun-select", "ignatia-swap-sun-swap",
                    "ignatia-swap-moon", "ignatia-swap-moon-select", "ignatia-swap-moon-swap"
                }),
                "Six nodes under the coin gate: a selection and a Swap per printed line, and nothing " +
                "else at all.");

            Assert.That(
                IgnatiaBoard.PrimaryText(IgnatiaCardContent.ChaosGate).Nodes
                    .Select(node => node.Id.Value),
                Is.EquivalentTo(new[]
                {
                    IgnatiaEffectContent.CoinGateNode,
                    "ignatia-gate-sun", "ignatia-gate-sun-select", "ignatia-gate-sun-swap",
                    "ignatia-gate-sun-move",
                    "ignatia-gate-moon", "ignatia-gate-moon-select", "ignatia-gate-moon-swap",
                    "ignatia-gate-moon-move"
                }),
                "and the Tier III adds exactly one node per line - the optional space.");
        }
    }
}
