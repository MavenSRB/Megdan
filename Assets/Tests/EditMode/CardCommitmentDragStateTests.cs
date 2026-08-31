using GuardsDigital.Core.Cards;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Milestone 8.30g's presentation-only threshold and one-pointer gesture contract.</summary>
    public sealed class CardCommitmentDragStateTests
    {
        [Test]
        public void MovingBelowTheThreshold_RemainsAClickAndReleasesNoDrag()
        {
            var state = new CardCommitmentDragState();
            var card = new CardInstanceId("card-a");

            Assert.That(state.Begin(7, card, new Vector2(100f, 200f)), Is.True);
            Assert.That(state.Move(7, new Vector2(105f, 204f), 12f), Is.False);
            Assert.That(state.Release(7, out var released, out var wasDrag), Is.True);
            Assert.That(released, Is.EqualTo(card));
            Assert.That(wasDrag, Is.False);
            Assert.That(state.IsTracking, Is.False);
        }

        [Test]
        public void CrossingTheThreshold_BecomesOneDragAndCannotReleaseTwice()
        {
            var state = new CardCommitmentDragState();
            var card = new CardInstanceId("card-b");

            Assert.That(state.Begin(3, card, Vector2.zero), Is.True);
            Assert.That(state.Begin(4, new CardInstanceId("card-c"), Vector2.zero), Is.False,
                "A second pointer cannot replace the gesture already in flight.");
            Assert.That(state.Move(4, new Vector2(30f, 0f), 12f), Is.False,
                "Another pointer cannot start the tracked card's drag.");
            Assert.That(state.Move(3, new Vector2(12f, 0f), 12f), Is.True);
            Assert.That(state.IsDragging, Is.True);
            Assert.That(state.Release(3, out var released, out var wasDrag), Is.True);
            Assert.That(released, Is.EqualTo(card));
            Assert.That(wasDrag, Is.True);
            Assert.That(state.Release(3, out _, out _), Is.False,
                "The completed gesture has no second submission-bearing release.");
        }

        [Test]
        public void CancellingAStartedDrag_ClearsAllGestureIdentity()
        {
            var state = new CardCommitmentDragState();
            Assert.That(state.Begin(1, new CardInstanceId("card-a"), Vector2.zero), Is.True);
            Assert.That(state.Move(1, new Vector2(20f, 0f), 12f), Is.True);

            state.Cancel();

            Assert.That(state.IsTracking, Is.False);
            Assert.That(state.IsDragging, Is.False);
            Assert.That(state.CardInstanceId.IsDefined, Is.False);
            Assert.That(state.Release(1, out _, out _), Is.False);
        }
    }
}
