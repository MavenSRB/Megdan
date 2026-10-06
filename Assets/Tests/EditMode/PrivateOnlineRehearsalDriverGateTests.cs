using System;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The rehearsal driver's pure gates. Split from <see cref="PrivateOnlineRehearsalContractTests"/>
    /// because <see cref="PrivateOnlineRehearsalDriver"/> is a Unity component: kept together, the
    /// Unity-free harness could not compile the rest of that file.
    /// </summary>
    public sealed class PrivateOnlineRehearsalDriverGateTests
    {
        [Test]
        public void ProjectionAudit_AllowsOnlyPubliclyNamedFaceUpCardsInAnotherHerosHand()
        {
            const string ownHeroId = "hero-own";
            const string otherHeroId = "hero-other";
            const string cardId = "card-known";
            var namedLog = new[]
            {
                new MatchLogEntry(
                    1,
                    MatchLogEntryKind.CardRevealed,
                    1,
                    1,
                    subjectCardInstanceId: new CardInstanceId(cardId))
            };
            var emptyLog = Array.Empty<MatchLogEntry>();

            Assert.That(PrivateOnlineRehearsalDriver.PublicCardVisibilityIsValid(
                Card(cardId, otherHeroId, CardLocation.Hand, CardFaceOrientation.FaceUp),
                namedLog,
                ownHeroId), Is.True,
                "A face-up card that the public match log names remains public when it returns to hand.");
            Assert.That(PrivateOnlineRehearsalDriver.PublicCardVisibilityIsValid(
                Card(cardId, otherHeroId, CardLocation.Hand, CardFaceOrientation.FaceUp),
                emptyLog,
                ownHeroId), Is.False);
            Assert.That(PrivateOnlineRehearsalDriver.PublicCardVisibilityIsValid(
                Card(cardId, otherHeroId, CardLocation.Deck, CardFaceOrientation.FaceUp),
                namedLog,
                ownHeroId), Is.False);
            Assert.That(PrivateOnlineRehearsalDriver.PublicCardVisibilityIsValid(
                Card(cardId, otherHeroId, CardLocation.TurnArea, CardFaceOrientation.FaceDown),
                namedLog,
                ownHeroId), Is.False);
            Assert.That(PrivateOnlineRehearsalDriver.PublicCardVisibilityIsValid(
                Card(cardId, ownHeroId, CardLocation.TurnArea, CardFaceOrientation.FaceUp),
                namedLog,
                ownHeroId), Is.False);
            Assert.That(PrivateOnlineRehearsalDriver.PublicCardVisibilityIsValid(
                Card(cardId, otherHeroId, CardLocation.TurnArea, CardFaceOrientation.FaceUp),
                emptyLog,
                ownHeroId), Is.True);
        }

        [TestCase(false, true, 20, false, false)]
        [TestCase(true, false, 20, false, false)]
        [TestCase(true, true, 19, false, false)]
        [TestCase(true, true, 20, true, false)]
        [TestCase(true, true, 20, false, true)]
        public void FreshProcessRejoinCut_WaitsUntilAllPreobservedChoreographyIsActuallyComplete(
            bool initiativeObserved,
            bool minionBattleObserved,
            long version,
            bool waitingForHost,
            bool expected)
        {
            Assert.That(PrivateOnlineRehearsalDriver.RejoinCutIsReady(
                initiativeObserved,
                minionBattleObserved,
                version,
                waitingForHost), Is.EqualTo(expected));
        }

        [Test]
        public void SmokeHostWaitsForBothRemoteWitnessesToLeaveBeforeExiting()
        {
            var first = new SeatId("red-player-01");
            var second = new SeatId("blue-player-01");
            var peers = new[] { first, second };
            SeatRosterEntry Row(SeatId seat, SeatOccupancyState state) =>
                new SeatRosterEntry(seat, default(HeroId), TeamSide.None, state);
            var firstHere = Row(first, SeatOccupancyState.Claimed);
            var firstGone = Row(first, SeatOccupancyState.Reserved);
            var secondHere = Row(second, SeatOccupancyState.Claimed);
            var secondGone = Row(second, SeatOccupancyState.Open);
            var local = Row(new SeatId("red-player-02"), SeatOccupancyState.Claimed);
            var bot = Row(new SeatId("blue-player-02"), SeatOccupancyState.Claimed);

            Assert.That(PrivateOnlineRehearsalDriver.SmokeHostCanExit(true, peers,
                new[] { firstHere, secondHere, local, bot }), Is.False);
            Assert.That(PrivateOnlineRehearsalDriver.SmokeHostCanExit(true, peers,
                new[] { firstGone, secondHere, local, bot }), Is.False,
                "A slow second client must still be able to finish its round trip.");
            Assert.That(PrivateOnlineRehearsalDriver.SmokeHostCanExit(true, peers,
                new[] { firstHere, secondGone, local, bot }), Is.False);
            Assert.That(PrivateOnlineRehearsalDriver.SmokeHostCanExit(false, peers,
                new[] { firstGone, secondGone, local, bot }), Is.False);
            Assert.That(PrivateOnlineRehearsalDriver.SmokeHostCanExit(true, peers,
                new[] { firstGone, local, bot }), Is.False,
                "An incomplete roster is not evidence of departure.");
            Assert.That(PrivateOnlineRehearsalDriver.SmokeHostCanExit(true,
                Array.Empty<SeatId>(), new[] { local, bot }), Is.False);
            Assert.That(PrivateOnlineRehearsalDriver.SmokeHostCanExit(true, peers,
                new[] { firstGone, secondGone, local, bot }), Is.True,
                "Host and in-process bots must not keep the finished cohort alive.");
        }

        private static NetworkCardRow Card(
            string instanceId,
            string ownerHeroId,
            CardLocation location,
            CardFaceOrientation faceOrientation) =>
            new NetworkCardRow(
                instanceId,
                "definition",
                "revision",
                ownerHeroId,
                (int)location,
                false,
                (int)CardResolutionStatus.Unresolved,
                (int)faceOrientation);
    }
}
