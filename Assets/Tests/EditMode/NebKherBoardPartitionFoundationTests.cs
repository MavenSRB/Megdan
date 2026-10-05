using System;
using GuardsDigital.Core.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class NebKherBoardPartitionFoundationTests
    {
        [TestCase(BoardPartitionAxis.EastWest, 0, 1, 0, -1, 1, 0)]
        [TestCase(BoardPartitionAxis.NorthEastSouthWest, 0, 1, 0, -1, 1, -1)]
        [TestCase(BoardPartitionAxis.NorthWestSouthEast, 1, 0, -1, 0, 0, 1)]
        public void EveryAxisHasTwoSidesAndLineSpacesBelongToNeither(
            BoardPartitionAxis axis,
            int positiveQ, int positiveR,
            int negativeQ, int negativeR,
            int lineQ, int lineR)
        {
            var line = new BoardPartitionLine(new HexCoordinate(0, 0), axis);
            var positive = new HexCoordinate(positiveQ, positiveR);
            var negative = new HexCoordinate(negativeQ, negativeR);
            var onLine = new HexCoordinate(lineQ, lineR);
            Assert.That(line.SideOf(positive), Is.EqualTo(BoardPartitionSide.Positive));
            Assert.That(line.SideOf(negative), Is.EqualTo(BoardPartitionSide.Negative));
            Assert.That(line.SideOf(onLine), Is.EqualTo(BoardPartitionSide.OnLine));
            Assert.That(line.Separates(positive, negative), Is.True);
            Assert.That(line.Separates(negative, positive), Is.True);
            Assert.That(line.Separates(positive, onLine), Is.False);
            Assert.That(line.Separates(onLine, negative), Is.False);
        }

        [Test]
        public void ChosenOriginRemainsFixedAfterChoosingHeroMoves()
        {
            var chosen = new BoardPartitionLine(
                new HexCoordinate(2, -1), BoardPartitionAxis.EastWest);
            Assert.That(chosen.SideOf(new HexCoordinate(4, -1)),
                Is.EqualTo(BoardPartitionSide.OnLine));
            Assert.That(chosen.Separates(
                new HexCoordinate(2, 0), new HexCoordinate(2, -2)), Is.True);
            Assert.That(chosen.Origin, Is.EqualTo(new HexCoordinate(2, -1)),
                "The choosing hero's later coordinate does not enter the fixed line.");
        }

        [Test]
        public void UndefinedOrInvalidAxisCannotAnswerAPlacementQuestion()
        {
            Assert.That(default(BoardPartitionLine).IsDefined, Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                default(BoardPartitionLine).SideOf(new HexCoordinate(0, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new BoardPartitionLine(new HexCoordinate(0, 0), BoardPartitionAxis.None));
        }
    }
}
