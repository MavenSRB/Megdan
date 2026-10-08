using System.IO;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.108. A player's typed name travels with their seat claim, the host cleans it,
    /// and every lobby shows it in place of the seat's identifier. Empty keeps the old label.
    /// </summary>
    public sealed class PrivateLobbyPlayerNameTests
    {
        [TestCase("Stefan", "Stefan")]
        [TestCase("  Stefan \t", "Stefan")]
        [TestCase("Big\t \nJim", "Big Jim")]
        [TestCase("Zero​Width", "ZeroWidth")]
        [TestCase("\u0007Bell\u001B", "Bell")]
        [TestCase("‮Reversed", "Reversed")]
        [TestCase("ABCDEFGHIJKLMNOPQRSTUVWXYZ", "ABCDEFGHIJKLMNOP")]
        [TestCase("Fifteen letters 😀", "Fifteen letters")]
        [TestCase("   ", "")]
        [TestCase(null, "")]
        public void AName_IsCleanedToWhatEveryLobbyShows(string typed, string shown)
        {
            Assert.That(PrivateLobbyEntryText.CleanPlayerName(typed), Is.EqualTo(shown));
            Assert.That(PrivateLobbyEntryText.CleanPlayerName(typed).Length,
                Is.LessThanOrEqualTo(PrivateLobbyEntryText.MaxPlayerNameChars));
        }

        [Test]
        public void TheProtocolVersion_MovedForTheName()
        {
            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(48),
                "8.108 adds a name to the seat claim and to every roster row, so a v23 peer is refused.");
        }

        [Test]
        public void AClaim_CarriesTheName_AndTheHostReadsItCleaned()
        {
            Assert.That(PrivateSessionFrameCodec.TryDecodeClaimSeat(
                ClaimPayload("red-player-01", "  Stefan​ "), out var claim), Is.True);
            Assert.That(claim.Seat.Value, Is.EqualTo("red-player-01"));
            Assert.That(claim.PlayerName, Is.EqualTo("Stefan"));
        }

        [Test]
        public void AClaim_WithAnOversizedName_IsRefusedAsMalformed()
        {
            var oversized = new string('x', PrivateSessionProtocol.MaxPlayerNameWireChars + 1);

            Assert.That(PrivateSessionFrameCodec.TryDecodeClaimSeat(
                ClaimPayload("red-player-01", oversized), out _), Is.False,
                "A name longer than the wire bound is not read into memory at all.");
            Assert.That(PrivateSessionFrameCodec.TryDecodeClaimSeat(
                ClaimPayload("red-player-01", new string('x', PrivateSessionProtocol.MaxPlayerNameWireChars)),
                out var claim), Is.True);
            Assert.That(claim.PlayerName.Length, Is.EqualTo(PrivateLobbyEntryText.MaxPlayerNameChars));
        }

        [Test]
        public void AClaimedSeat_ShowsItsPlayersName_OnEveryLobby()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var stefan = harness.ConnectSeatless(1);
            stefan.PlayerName = "  Stefan ";
            var other = harness.ConnectSeatless(2);
            harness.Pump();

            Assert.That(stefan.TryClaimSeat(new SeatId("red-player-01")), Is.True);
            Assert.That(other.TryClaimSeat(new SeatId("blue-player-01")), Is.True);
            harness.Pump();

            var hostRow = harness.Session.Roster.Single(entry => entry.Seat.Value == "red-player-01");
            Assert.That(hostRow.PlayerName, Is.EqualTo("Stefan"));

            var seenByOther = PrivateLobbyModel.ForClient(other);
            Assert.That(seenByOther.Seats.Single(row => row.Seat.Value == "red-player-01").SeatLabel,
                Is.EqualTo("Stefan"), "The name crossed the wire to another player's lobby.");
            Assert.That(seenByOther.Seats.Single(row => row.Seat.Value == "blue-player-01").SeatLabel,
                Is.EqualTo("BLUE-PLAYER-01"), "A player who typed no name keeps the seat's own label.");
            Assert.That(PrivateLobbyModel.ForHost(harness.Session, string.Empty)
                    .Seats.Single(row => row.Seat.Value == "red-player-01").SeatLabel,
                Is.EqualTo("Stefan"));
        }

        [Test]
        public void AnAwaySeat_KeepsItsName_AndAForfeitedSeat_LosesIt()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var stefan = harness.ConnectSeatless(1);
            stefan.PlayerName = "Stefan";
            var ana = harness.ConnectSeatless(2);
            ana.PlayerName = "Ana";
            harness.Pump();
            Assert.That(stefan.TryClaimSeat(new SeatId("red-player-01")), Is.True);
            Assert.That(ana.TryClaimSeat(new SeatId("blue-player-01")), Is.True);
            harness.Pump();
            harness.NoteSeatClaimed(1);
            harness.NoteSeatClaimed(2);

            harness.DisconnectClient(1);
            harness.Pump();
            var away = harness.Session.Roster.Single(entry => entry.Seat.Value == "red-player-01");
            Assert.That(away.Occupancy, Is.EqualTo(SeatOccupancyState.Reserved));
            Assert.That(away.PlayerName, Is.EqualTo("Stefan"), "A dropped player comes back under the same name.");

            ana.SendGoodbye();
            harness.Pump();
            var left = harness.Session.Roster.Single(entry => entry.Seat.Value == "blue-player-01");
            Assert.That(left.Occupancy, Is.EqualTo(SeatOccupancyState.Open));
            Assert.That(left.PlayerName, Is.Empty, "An open chair carries nobody's name.");
        }

        [Test]
        public void TheWaitingOnLine_NamesThePickingPlayer()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var seats = harness.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
            for (var index = 0; index < seats.Length; index++)
            {
                var client = harness.Connect(index + 1, seats[index]);
                client.PlayerName = "Player " + (index + 1);
            }

            harness.Pump();
            foreach (var ordinal in harness.Ordinals)
            {
                Assert.That(harness.Client(ordinal).TrySetReadiness(true), Is.True);
                harness.Pump();
            }

            var picking = harness.Session.Selection.PickingSeat;
            var pickerOrdinal = harness.Ordinals.First(ordinal => harness.SeatOf(ordinal) == picking.Value);
            var watcher = harness.Ordinals.First(ordinal => ordinal != pickerOrdinal);

            Assert.That(PrivateLobbyModel.ForClient(harness.Client(watcher)).Selection.StatusLabel,
                Is.EqualTo("WAITING ON Player " + pickerOrdinal));
        }

        [Test]
        public void ANameArriving_MovesTheRedrawSignature()
        {
            var seat = new SeatId("red-player-01");
            var before = new[] { new SeatRosterEntry(seat, default, TeamSide.Red, SeatOccupancyState.Claimed) };
            var after = new[]
            {
                new SeatRosterEntry(seat, default, TeamSide.Red, SeatOccupancyState.Claimed, playerName: "Stefan")
            };

            Assert.That(PrivateLobbyModel.RosterSignature(after),
                Is.Not.EqualTo(PrivateLobbyModel.RosterSignature(before)));
        }

        [Test]
        public void TheName_IsRememberedCleaned_AndLoadedBack()
        {
            var memory = new InMemoryPrivateLobbyFieldMemory();
            PrivateLobbyRememberedFields.RememberPlayerName(memory, "  Stefan​ ");

            Assert.That(PrivateLobbyRememberedFields.Load(memory, "127.0.0.1", "45677").PlayerName,
                Is.EqualTo("Stefan"));
            Assert.That(PrivateLobbyRememberedFields.Load(new InMemoryPrivateLobbyFieldMemory(), "127.0.0.1", "45677")
                .PlayerName, Is.Empty);
        }

        private static byte[] ClaimPayload(string seat, string playerName)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(seat);
                writer.Write(playerName);
                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
