using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Rules.Victory;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Network;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35a. The primitive private host/join lobby, from both ends: the session
    /// vocabulary a lobby needs and could not invent for itself - readiness, and the transition out
    /// of the lobby - and the bounded reading of the session contracts that a surface draws.
    /// </summary>
    /// <remarks>
    /// <para>Unity-free on purpose, so it runs in the cross-platform harness as well as in Unity
    /// Edit Mode: what a player will read is decided here, and deciding it in an engine assembly
    /// would put it out of reach of the fast tier. The engine half - that the words reach a panel
    /// and that the controls can be pressed at every supported desktop viewport - is
    /// <c>PrivateLobbyPlayModeTests</c>.</para>
    /// <para>Vocabulary is neutral throughout, per the user's 2026-09-02 direction: a frame that
    /// does not decode is a malformed frame and a case that proves a refusal is a negative-path
    /// case.</para>
    /// </remarks>
    public sealed class PrivateLobbyTests
    {
        // --- The wire vocabulary readiness and the transition needed ---------------------------

        [Test]
        public void AReadinessFrame_RoundTripsItsOneValue([Values(true, false)] bool ready)
        {
            var frame = PrivateSessionTestDriver.Decode(
                PrivateSessionFrameCodec.SetReadinessFrame(new SeatReadinessMessage(ready)));

            Assert.That(frame.Kind, Is.EqualTo(SessionMessageKind.SetReadiness));
            Assert.That(
                PrivateSessionFrameCodec.TryDecodeSetReadiness(frame.Payload, out var decoded),
                Is.True);
            Assert.That(decoded.IsReady, Is.EqualTo(ready));
        }

        /// <summary>
        /// The readiness payload names no seat, deliberately: the seat is the connection's, bound
        /// by the grant, and a payload that named one would be milestone 8.30a's forbidden
        /// payload-claimed identity.
        /// </summary>
        [Test]
        public void AReadinessMessage_CarriesNothingButTheValue()
        {
            var declared = typeof(SeatReadinessMessage)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name)
                .ToArray();

            Assert.That(declared, Is.EqualTo(new[] { nameof(SeatReadinessMessage.IsReady) }),
                "A readiness statement says only whether this seat is ready. A seat, hero or " +
                "player field here would be a claim about identity the connection binding " +
                "already answers.");
        }

        [Test]
        public void AReadinessPayload_WithTrailingBytesOrNone_IsRefused()
        {
            Assert.That(
                PrivateSessionFrameCodec.TryDecodeSetReadiness(new byte[] { 1, 0 }, out _),
                Is.False, "Trailing bytes after the one value must be refused.");
            Assert.That(
                PrivateSessionFrameCodec.TryDecodeSetReadiness(Array.Empty<byte>(), out _),
                Is.False, "A truncated readiness payload must be refused.");
            Assert.That(PrivateSessionFrameCodec.TryDecodeSetReadiness(null, out _), Is.False);
        }

        [Test]
        public void TheMatchStartedAnnouncement_CarriesNothingAndRefusesAPayload()
        {
            var frame = PrivateSessionTestDriver.Decode(PrivateSessionFrameCodec.MatchStartedFrame());

            Assert.That(frame.Kind, Is.EqualTo(SessionMessageKind.MatchStarted));
            Assert.That(frame.Payload, Is.Empty);
            Assert.That(PrivateSessionFrameCodec.TryDecodeMatchStarted(frame.Payload), Is.True);
            Assert.That(PrivateSessionFrameCodec.TryDecodeMatchStarted(new byte[] { 0 }), Is.False,
                "A phase change that accepted arguments would be a second way to say what the " +
                "roster already says.");
        }

        /// <summary>
        /// The refuse-undefined floor, restated for the two kinds this milestone adds: every
        /// defined kind is readable off the stream and the first integer past them is not.
        /// </summary>
        [Test]
        public void EveryDefinedMessageKind_IsReadableFromTheStream_AndTheNextIntegerIsNot()
        {
            var defined = Enum.GetValues(typeof(SessionMessageKind))
                .Cast<SessionMessageKind>()
                .Where(kind => kind != SessionMessageKind.None)
                .ToArray();
            Assert.That(defined.Length, Is.GreaterThanOrEqualTo(18),
                "A kind sweep that reads fewer kinds than the protocol defines is a broken rule " +
                "rather than a pass.");

            foreach (var kind in defined)
            {
                var assembler = new PrivateSessionFrameAssembler();
                var frame = PrivateSessionFrameCodec.EncodeFrame(kind, Array.Empty<byte>());
                Assert.That(assembler.Add(frame, 0, frame.Length), Is.True,
                    "Kind " + kind + " is defined but its own frame is refused by the assembler.");
                Assert.That(assembler.TryDequeue(out var decoded), Is.True);
                Assert.That(decoded.Kind, Is.EqualTo(kind));
            }

            var undefined = (int)defined.Max() + 1;
            var beyond = PrivateSessionFrameCodec.EncodeFrame(
                SessionMessageKind.MatchStarted, Array.Empty<byte>());
            beyond[6] = (byte)(undefined & 0xFF);
            beyond[7] = (byte)((undefined >> 8) & 0xFF);
            var refusing = new PrivateSessionFrameAssembler();
            Assert.That(refusing.Add(beyond, 0, beyond.Length), Is.False);
            Assert.That(refusing.Rejection, Is.EqualTo(SessionRejectionReason.MalformedFrame));
        }

        [Test]
        public void ARosterEntry_CarriesItsReadinessAcrossTheWire()
        {
            var roster = new[]
            {
                new SeatRosterEntry(new SeatId("red-player-01"), new HeroId("hero-wasp"),
                    TeamSide.Red, SeatOccupancyState.Claimed, true),
                new SeatRosterEntry(new SeatId("blue-player-01"), new HeroId("hero-arien"),
                    TeamSide.Blue, SeatOccupancyState.Open)
            };

            var frame = PrivateSessionTestDriver.Decode(
                PrivateSessionFrameCodec.RosterUpdateFrame(new SeatRosterMessage(roster)));
            Assert.That(
                PrivateSessionFrameCodec.TryDecodeRosterUpdate(frame.Payload, out var decoded),
                Is.True);

            Assert.That(decoded.Roster[0].IsReady, Is.True);
            Assert.That(decoded.Roster[1].IsReady, Is.False);
        }

        [Test]
        public void TheAdmissionAnswer_CarriesTheSessionsPublicPhase([Values(true, false)] bool started)
        {
            var roster = new[]
            {
                new SeatRosterEntry(new SeatId("red-player-01"), new HeroId("hero-wasp"),
                    TeamSide.Red, SeatOccupancyState.Claimed, true)
            };

            var frame = PrivateSessionTestDriver.Decode(PrivateSessionFrameCodec.WelcomeFrame(
                new SessionWelcomeMessage("correlation-lobby", roster, started)));
            Assert.That(PrivateSessionFrameCodec.TryDecodeWelcome(frame.Payload, out var decoded),
                Is.True);

            Assert.That(decoded.MatchStarted, Is.EqualTo(started));
            Assert.That(decoded.Roster[0].IsReady, Is.True);
        }

        [Test]
        public void AnOversizedReadinessFrame_IsRefusedBeforeItsPayloadIsBuffered()
        {
            var header = PrivateSessionFrameCodec.EncodeFrame(
                SessionMessageKind.SetReadiness, new byte[] { 1 });
            var oversized = (long)PrivateSessionProtocol.MaxPayloadBytes + 1;
            header[8] = (byte)(oversized & 0xFF);
            header[9] = (byte)((oversized >> 8) & 0xFF);
            header[10] = (byte)((oversized >> 16) & 0xFF);
            header[11] = (byte)((oversized >> 24) & 0xFF);

            var assembler = new PrivateSessionFrameAssembler();
            Assert.That(assembler.Add(header, 0, PrivateSessionProtocol.FrameHeaderBytes), Is.False,
                "The declared length is checked before a buffer for it exists.");
            Assert.That(assembler.Rejection, Is.EqualTo(SessionRejectionReason.OversizedFrame));
        }

        // --- The host owns readiness -------------------------------------------------------------

        [Test]
        public void ASeatedConnectionsReadiness_ReachesTheRosterAndEveryAdmittedConnection()
        {
            var driver = PrivateSessionTestDriver.Compose();
            var seats = SeatsOf(driver);
            driver.Seat(1, seats[0]);
            driver.Seat(2, seats[1]);

            SetReadiness(driver, 1, true);

            Assert.That(RosterOf(driver.Session, seats[0]).IsReady, Is.True);
            Assert.That(LastRoster(driver, 2).Single(entry => entry.Seat.Value == seats[0]).IsReady,
                Is.True, "Readiness is public: the seat beside it learns of it without asking.");
        }

        [Test]
        public void RepeatingTheReadinessASeatAlreadyHolds_ChangesNothingAndSaysNothing()
        {
            var driver = PrivateSessionTestDriver.Compose();
            var seats = SeatsOf(driver);
            driver.Seat(1, seats[0]);
            driver.Seat(2, seats[1]);
            SetReadiness(driver, 1, true);
            var broadcasts = RosterFrameCount(driver, 2);

            SetReadiness(driver, 1, true);

            Assert.That(RosterFrameCount(driver, 2), Is.EqualTo(broadcasts),
                "Setting the value a seat already holds is accepted and changes nothing, so a " +
                "surface resending what it believes cannot make the roster flicker.");
            Assert.That(RosterOf(driver.Session, seats[0]).IsReady, Is.True);
            Assert.That(driver.Outbox.WasClosed(1), Is.False);
        }

        [Test]
        public void ReadinessFromAnAdmittedButUnseatedConnection_IsRefusedAsUnexpected()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Admit(1);

            SetReadiness(driver, 1, true);

            Assert.That(driver.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.UnexpectedMessage));
            Assert.That(driver.Outbox.WasClosed(1), Is.True,
                "A connection holding no seat has no readiness to state.");
        }

        [Test]
        public void ReadinessBeforeTheHandshake_IsRefusedAsUnexpected()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Session.OnConnected(1, 0);

            SetReadiness(driver, 1, true);

            Assert.That(driver.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.UnexpectedMessage));
        }

        [Test]
        public void AMalformedReadinessPayload_IsRefusedAsMalformed()
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Seat(1, SeatsOf(driver)[0]);

            driver.Submit(1, PrivateSessionFrameCodec.EncodeFrame(
                SessionMessageKind.SetReadiness, new byte[] { 1, 1, 1 }));

            Assert.That(driver.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.MalformedFrame));
        }

        // --- The transition out of the lobby -----------------------------------------------------

        /// <remarks>
        /// 8.35z54 moved what the sixth readiness DOES. It used to end the lobby; the user's
        /// flow spec puts the hero picks after readiness - "they pick a seat, click ready. Once they
        /// are ready, everyone picks their heroes" - so the sixth readiness now OPENS the selection
        /// and the last pick ends the lobby. The half of the original claim that is about readiness
        /// is kept exactly: not one seat before the sixth changes anything.
        /// </remarks>
        [Test]
        public void TheSelectionOpens_WhenTheSixthSeatIsReady_AndNotOneSeatBefore()
        {
            var driver = SeatEveryone(out var seats);

            for (var index = 0; index < seats.Count - 1; index++)
            {
                SetReadiness(driver, index + 1, true);
                Assert.That(driver.Session.IsPickPhaseOpen, Is.False,
                    "The picking opened with " + (index + 1) + " of " + seats.Count +
                    " seats ready.");
                Assert.That(driver.Session.HasMatchStarted, Is.False);
            }

            SetReadiness(driver, seats.Count, true);

            Assert.That(driver.Session.IsPickPhaseOpen, Is.True);
            Assert.That(driver.Session.ReadySeatCount, Is.EqualTo(driver.Session.TotalSeats));
            Assert.That(driver.Session.HasMatchStarted, Is.False,
                "Readiness opens the picking; the match begins on the last pick.");

            PickEveryHero(driver, seats);
            Assert.That(driver.Session.HasMatchStarted, Is.True);
        }

        [Test]
        public void TheAnnouncement_ReachesEverySeatedConnectionExactlyOnce()
        {
            var driver = SeatEveryone(out var seats);

            for (var index = 0; index < seats.Count; index++)
            {
                SetReadiness(driver, index + 1, true);
            }

            // 8.35z54: the announcement follows the last PICK now, not the last readiness.
            PickEveryHero(driver, seats);

            for (var index = 0; index < seats.Count; index++)
            {
                Assert.That(
                    driver.Outbox.FramesFor(index + 1)
                        .Count(frame => frame.Kind == SessionMessageKind.MatchStarted),
                    Is.EqualTo(1),
                    "Seat " + seats[index] + " must be told the match began exactly once.");
            }
        }

        [Test]
        public void ASeatThatStandsDownBeforeTheSixth_HoldsTheLobbyOpen()
        {
            var driver = SeatEveryone(out var seats);
            for (var index = 0; index < seats.Count - 1; index++)
            {
                SetReadiness(driver, index + 1, true);
            }

            SetReadiness(driver, 1, false);
            SetReadiness(driver, seats.Count, true);

            Assert.That(driver.Session.IsPickPhaseOpen, Is.False,
                "Five ready seats and one that stood down is not six ready seats.");
            Assert.That(driver.Session.HasMatchStarted, Is.False);

            SetReadiness(driver, 1, true);

            // 8.35z54: the sixth readiness opens the picking, and the picks end the lobby.
            Assert.That(driver.Session.IsPickPhaseOpen, Is.True);
            PickEveryHero(driver, seats);
            Assert.That(driver.Session.HasMatchStarted, Is.True);
        }

        [Test]
        public void ASeatLostBeforeTheTransition_IsNoLongerReady()
        {
            var driver = SeatEveryone(out var seats);
            SetReadiness(driver, 1, true);
            Assert.That(RosterOf(driver.Session, seats[0]).IsReady, Is.True);

            driver.Session.OnDisconnected(1);

            var row = RosterOf(driver.Session, seats[0]);
            Assert.That(row.Occupancy, Is.EqualTo(SeatOccupancyState.Reserved));
            Assert.That(row.IsReady, Is.False,
                "A chair nobody is sitting in cannot have agreed to begin.");
            Assert.That(driver.Session.ReadySeatCount, Is.Zero);
        }

        [Test]
        public void ReadinessAfterTheTransition_IsRefusedWithoutClosingTheConnection()
        {
            var driver = ReadyEveryone(out var seats);

            SetReadiness(driver, 1, false);

            var reject = driver.Outbox.RejectFor(1);
            Assert.That(reject.Reason, Is.EqualTo(SessionRejectionReason.UnexpectedMessage));
            Assert.That(reject.Detail, Is.Not.Empty);
            Assert.That(driver.Outbox.WasClosed(1), Is.False,
                "Closing a live match's connection over one late lobby message would cost that " +
                "player their seat.");
            Assert.That(RosterOf(driver.Session, seats[0]).IsReady, Is.True);
        }

        [Test]
        public void TheTransition_IsOneWay_AndALostSeatDoesNotReturnTheSessionToTheLobby()
        {
            var driver = ReadyEveryone(out _);

            driver.Session.OnDisconnected(1);

            Assert.That(driver.Session.HasMatchStarted, Is.True,
                "A match that has begun does not un-begin; a lost seat is the reconnect " +
                "policy's business, not the lobby's.");
            Assert.That(driver.Session.IsPaused, Is.True);
        }

        /// <summary>
        /// A returning player was not there to hear the announcement, so its own admission has to
        /// say what phase it is coming back to.
        /// </summary>
        [Test]
        public void AReturningSeat_LearnsTheMatchIsUnderWay_FromItsOwnAdmission()
        {
            var driver = ReadyEveryone(out var seats);
            var capability = GrantedCapability(driver, 1);
            Assert.That(capability, Is.Not.Empty);
            driver.Session.OnDisconnected(1);

            driver.Admit(7);
            var welcome = driver.Outbox.FramesFor(7)
                .Last(frame => frame.Kind == SessionMessageKind.Welcome);
            Assert.That(PrivateSessionFrameCodec.TryDecodeWelcome(welcome.Payload, out var decoded),
                Is.True);
            Assert.That(decoded.MatchStarted, Is.True);

            driver.Submit(7, PrivateSessionFrameCodec.ReclaimSeatFrame(
                new SeatReclaimMessage(new SeatId(seats[0]), capability)));

            Assert.That(RosterOf(driver.Session, seats[0]).Occupancy,
                Is.EqualTo(SeatOccupancyState.Claimed));
            Assert.That(driver.Session.HasMatchStarted, Is.True);
        }

        // --- Negative paths on a keyed session ---------------------------------------------------

        [Test]
        public void OnAKeyedSession_APlaintextReadinessFrame_IsRefusedAsASealViolation()
        {
            var driver = PrivateSessionTestDriver.Compose("invitation-key");
            driver.Seat(1, SeatsOf(driver)[0]);

            driver.Session.OnFrame(1, PrivateSessionTestDriver.Decode(
                PrivateSessionFrameCodec.SetReadinessFrame(new SeatReadinessMessage(true))));

            Assert.That(driver.Outbox.RejectFor(1).Reason,
                Is.EqualTo(SessionRejectionReason.SealViolation));
            Assert.That(RosterOf(driver.Session, SeatsOf(driver)[0]).IsReady, Is.False);
        }

        [Test]
        public void OnAKeyedSession_ReadinessAndTheAnnouncement_TravelSealed()
        {
            var driver = PrivateSessionTestDriver.Compose("invitation-key");
            driver.Session.Composition = new NoRecompositionComposer();
            var seats = SeatsOf(driver);
            for (var index = 0; index < seats.Count; index++)
            {
                driver.Seat(index + 1, seats[index]);
            }

            var before = driver.Outbox.SealedFrameCounts[1];
            for (var index = 0; index < seats.Count; index++)
            {
                SetReadiness(driver, index + 1, true);
            }

            // 8.35z54: the announcement follows the last PICK now.
            PickEveryHero(driver, seats);
            Assert.That(driver.Session.HasMatchStarted, Is.True);
            Assert.That(driver.Outbox.SealedFrameCounts[1], Is.GreaterThan(before),
                "Every post-proof frame on a keyed session is sealed, the two new kinds included.");
            Assert.That(
                driver.Outbox.FramesFor(1)
                    .Count(frame => frame.Kind == SessionMessageKind.MatchStarted),
                Is.EqualTo(1),
                "The announcement is readable through the seal and nowhere else.");
        }

        // --- Six real clients on the in-memory wire ----------------------------------------------

        [Test]
        public void EverySeatReadiesOverTheWire_AndEveryClientReadsTheMatchBegin()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var ordinals = harness.Ordinals;

            foreach (var ordinal in ordinals)
            {
                Assert.That(harness.Client(ordinal).HasMatchStarted, Is.False);
            }

            // 8.35z54: readiness opens rulebook p.22's picking and the last pick ends the
            // lobby, so reaching the announcement now takes both halves.
            harness.ReadyAndPickEverySeat();

            Assert.That(harness.Session.HasMatchStarted, Is.True);
            foreach (var ordinal in ordinals)
            {
                Assert.That(harness.Client(ordinal).HasMatchStarted, Is.True,
                    "Seat " + harness.SeatOf(ordinal) + " never learned the lobby was over.");
                Assert.That(harness.Client(ordinal).IsReady, Is.True);
                Assert.That(harness.Client(ordinal).TrySetReadiness(false), Is.False,
                    "Readiness is refused by the client itself once the match has begun.");
            }
        }

        [Test]
        public void EveryClientReadsEveryOtherSeatsReadiness_FromTheRosterAlone()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var ordinals = harness.Ordinals;
            var readied = ordinals.Take(3).ToArray();

            foreach (var ordinal in readied)
            {
                harness.Client(ordinal).TrySetReadiness(true);
                harness.Pump();
            }

            var readySeats = readied.Select(harness.SeatOf).OrderBy(value => value).ToArray();
            foreach (var ordinal in ordinals)
            {
                var seen = harness.Client(ordinal).Roster
                    .Where(entry => entry.IsReady)
                    .Select(entry => entry.Seat.Value)
                    .OrderBy(value => value)
                    .ToArray();
                Assert.That(seen, Is.EqualTo(readySeats),
                    "Seat " + harness.SeatOf(ordinal) + " reads a different table than the host's.");
            }
        }

        // --- What a lobby surface shows ----------------------------------------------------------

        [Test]
        public void EveryConnectionState_ReadsAsBoundedWords()
        {
            foreach (PrivateSessionClientState state in
                Enum.GetValues(typeof(PrivateSessionClientState)))
            {
                var words = PrivateLobbyLabels.ConnectionState(state);
                Assert.That(words, Is.Not.Empty, state + " reaches a player as a blank line.");
                Assert.That(words.Length, Is.LessThanOrEqualTo(PrivateLobbyLabels.MaxLabelChars));
            }
        }

        [Test]
        public void EveryRefusalReason_ReadsAsBoundedWords()
        {
            var reasons = Enum.GetValues(typeof(SessionRejectionReason))
                .Cast<SessionRejectionReason>()
                .Where(reason => reason != SessionRejectionReason.None)
                .ToArray();
            Assert.That(reasons.Length, Is.GreaterThanOrEqualTo(22),
                "A sweep reading fewer reasons than the session layer defines is a broken rule. " +
                "The twenty-second is the ELEVENTH amendment's MatchFormatMismatch.");

            foreach (var reason in reasons)
            {
                var words = PrivateLobbyLabels.Refusal(reason);
                Assert.That(words, Is.Not.Empty, reason + " reaches a player as a blank line.");
                Assert.That(words.Length, Is.LessThanOrEqualTo(PrivateLobbyLabels.MaxLabelChars));
            }

            Assert.That(PrivateLobbyLabels.Refusal(SessionRejectionReason.None), Is.Empty);
        }

        [Test]
        public void EveryOfferableControl_ReadsAsBoundedWords()
        {
            foreach (PrivateLobbyControl control in Enum.GetValues(typeof(PrivateLobbyControl)))
            {
                if (control == PrivateLobbyControl.None)
                {
                    continue;
                }

                Assert.That(PrivateLobbyLabels.Control(control), Is.Not.Empty);
            }
        }

        /// <summary>
        /// The block's third acceptance check, from the model's end: nothing a lobby row says about
        /// a team or a status exists only as a colour, because the row has no colour to say it in.
        /// </summary>
        [Test]
        public void TeamAndStatus_AreWordsRatherThanColour()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var ordinal = harness.Ordinals[0];
            harness.Client(ordinal).TrySetReadiness(true);
            harness.Pump();

            var model = PrivateLobbyModel.ForClient(harness.Client(ordinal));
            Assert.That(model.Seats.Count, Is.EqualTo(PrivateSessionProtocol.MaxSeats));
            foreach (var row in model.Seats)
            {
                Assert.That(row.TeamLabel, Is.Not.Empty);
                Assert.That(row.OccupancyLabel, Is.Not.Empty);
                Assert.That(row.ReadinessLabel, Is.Not.Empty);
            }

            var teams = model.Seats.Select(row => row.TeamLabel).Distinct().OrderBy(v => v).ToArray();
            Assert.That(teams, Is.EqualTo(new[] { "BLUE TEAM", "RED TEAM" }),
                "Both sides must be readable as words.");
            Assert.That(model.Seats.Count(row => row.ReadinessLabel == "READY"), Is.EqualTo(1));
            Assert.That(model.Seats.Count(row => row.IsOwnSeat), Is.EqualTo(1),
                "A player must be able to find their own row.");

            var readable = typeof(PrivateLobbySeatRow)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.PropertyType)
                .Distinct()
                .ToArray();
            Assert.That(readable.All(type =>
                    type == typeof(string) || type == typeof(bool) || type == typeof(SeatId)),
                Is.True,
                "A lobby row carries words, a flag and a seat id. A colour, texture or style here " +
                "would be a status a player could fail to see.");
        }

        [Test]
        public void TheEntryForm_OffersHostingAndJoiningAndSaysWhyTheLastAttemptFailed()
        {
            var driver = PrivateSessionTestDriver.Compose();
            var model = PrivateLobbyModel.Entry(
                driver.Session.Roster, "The host could not be reached: connection refused.");

            Assert.That(model.Role, Is.EqualTo(PrivateLobbyRole.None));
            Assert.That(model.IsShown, Is.True);
            Assert.That(model.Controls,
                Is.EqualTo(new[] { PrivateLobbyControl.Host, PrivateLobbyControl.Join }));
            Assert.That(model.Seats.Count, Is.EqualTo(PrivateSessionProtocol.MaxSeats),
                "Every invited player runs this build, so the six seats can be offered as picks " +
                "before any host has answered.");
            Assert.That(model.Seats.All(row => row.HeroLabel.Length > 0), Is.True);
            Assert.That(model.FeedbackLabel, Does.StartWith("The host could not be reached"));
        }

        [Test]
        public void TheSeatedLobby_OffersReadyThenCancelReady_AndAlwaysLeaving()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var ordinal = harness.Ordinals[0];
            var client = harness.Client(ordinal);

            var before = PrivateLobbyModel.ForClient(client);
            Assert.That(before.Role, Is.EqualTo(PrivateLobbyRole.Client));
            Assert.That(before.IsShown, Is.True);
            Assert.That(before.Controls,
                Is.EqualTo(new[] { PrivateLobbyControl.Ready, PrivateLobbyControl.Leave }));
            Assert.That(before.StatusLabel, Is.EqualTo("SEATED"));

            client.TrySetReadiness(true);
            harness.Pump();

            var after = PrivateLobbyModel.ForClient(client);
            Assert.That(after.Controls,
                Is.EqualTo(new[] { PrivateLobbyControl.Unready, PrivateLobbyControl.Leave }),
                "What a seat may press is decided by what the host says it currently is.");
        }

        [Test]
        public void AStartedMatch_StandsTheLobbyAsideWithNoControlOnIt()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.ReadyAndPickEverySeat();

            var model = PrivateLobbyModel.ForClient(harness.Client(harness.Ordinals[0]));

            Assert.That(model.HasMatchStarted, Is.True);
            Assert.That(model.IsShown, Is.False,
                "The lobby hands off to the match surface rather than sitting over it.");
            Assert.That(model.Offers(PrivateLobbyControl.Ready), Is.False);
            Assert.That(model.Offers(PrivateLobbyControl.Unready), Is.False);
        }

        [Test]
        public void HostLoss_OffersOnlyAnAuthorizedReturnAndLeaving()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var ordinal = harness.Ordinals[0];
            harness.DisconnectClient(ordinal);
            var client = harness.Client(ordinal);
            Assert.That(client.State, Is.EqualTo(PrivateSessionClientState.HostLost));
            Assert.That(client.CanAttemptReconnect, Is.True);

            var model = PrivateLobbyModel.ForClient(client);

            Assert.That(model.IsShown, Is.True,
                "A terminal session is exactly when a player needs the lobby back.");
            Assert.That(model.StatusLabel, Is.EqualTo("HOST LOST"));
            Assert.That(model.Controls,
                Is.EqualTo(new[] { PrivateLobbyControl.Reconnect, PrivateLobbyControl.Leave }),
                "Milestone 8.34's rule: a terminal surface keeps no control a player could still " +
                "press against a host that is gone.");
        }

        [Test]
        public void AConnectionLostBeforeItHeldASeat_OffersOnlyLeaving()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            const int ordinal = 1;
            harness.Connect(ordinal, harness.Session.Roster[0].Seat.Value);
            harness.DisconnectClient(ordinal);
            var client = harness.Client(ordinal);

            var model = PrivateLobbyModel.ForClient(client);

            Assert.That(client.CanAttemptReconnect, Is.False,
                "A connection lost before any claim leaves no reservation, so there is nothing " +
                "to return to and no control that could pretend otherwise.");
            Assert.That(model.Controls, Is.EqualTo(new[] { PrivateLobbyControl.Leave }));
            Assert.That(model.Offers(PrivateLobbyControl.Reconnect), Is.False);
        }

        [Test]
        public void ARefusal_ReadsAsTheReasonAndTheHostsOwnBoundedDetail()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var seat = harness.Session.Roster[0].Seat.Value;
            harness.Connect(1, seat);
            harness.Pump();
            harness.Connect(2, seat);
            harness.Pump();
            var refused = harness.Client(2);

            var model = PrivateLobbyModel.ForClient(refused);

            Assert.That(refused.RejectionReason,
                Is.EqualTo(SessionRejectionReason.SeatAlreadyClaimed));
            Assert.That(model.StatusLabel, Is.EqualTo("REFUSED"));
            Assert.That(model.FeedbackLabel,
                Does.StartWith("That seat is already taken."));
            Assert.That(model.FeedbackLabel.Length,
                Is.LessThanOrEqualTo(PrivateLobbyLabels.MaxLabelChars));
        }

        /// <summary>
        /// An incompatible build is refused before it holds anything, and the lobby says which
        /// element of the admission tuple differs - the compatibility half of the block's first
        /// acceptance check, end to end rather than as a label map.
        /// </summary>
        [Test]
        public void AnIncompatibleBuild_IsRefusedWithTheElementItDiffersOn()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var identity = harness.Identity;
            var incompatible = new PrivateSessionIdentity(
                identity.ProtocolFamily,
                identity.ProtocolVersion,
                identity.BuildIdentity,
                identity.SnapshotSchemaVersion + 1,
                identity.MapId,
                identity.MapContentRevision,
                identity.MatchFormatId,
                identity.ContentDigest);
            harness.Connect(1, harness.Session.Roster[0].Seat.Value, incompatible);
            harness.Pump();
            var client = harness.Client(1);

            var model = PrivateLobbyModel.ForClient(client);

            Assert.That(client.RejectionReason,
                Is.EqualTo(SessionRejectionReason.SnapshotSchemaMismatch));
            Assert.That(model.StatusLabel, Is.EqualTo("REFUSED"));
            Assert.That(model.FeedbackLabel,
                Does.StartWith("The host is running a different save schema."));
            Assert.That(model.FeedbackLabel, Does.Contain("Snapshot schema differs"),
                "A compatibility refusal names the mismatching element, which is what makes it " +
                "actionable rather than only true.");
            Assert.That(model.Seats, Is.Empty,
                "A refused connection was never told the roster, so there is none to draw.");
        }

        /// <summary>
        /// A session whose every seat is held by a live connection refuses an arriving player by
        /// name, at admission, before any seat-level reason could shadow it.
        /// </summary>
        [Test]
        public void AFullSession_IsRefusedByNameWithoutSayingWhoHoldsWhat()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();

            harness.Connect(7, harness.Session.Roster[0].Seat.Value);
            harness.Pump();
            var late = harness.Client(7);

            var model = PrivateLobbyModel.ForClient(late);

            Assert.That(late.RejectionReason, Is.EqualTo(SessionRejectionReason.SessionFull));
            Assert.That(model.FeedbackLabel, Does.StartWith("Every seat is taken."));
            Assert.That(model.Controls, Is.EqualTo(new[] { PrivateLobbyControl.Leave }));
            Assert.That(model.Seats, Is.Empty);
        }

        /// <summary>
        /// A seated connection asking for another seat is refused without losing the one it holds,
        /// and the lobby says so while still offering everything a seated player may do.
        /// </summary>
        [Test]
        public void AnUnauthorizedSeatChange_IsSaidPlainlyAndLeavesTheSeatMapUntouched()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var mine = harness.SeatOf(1);
            var theirs = harness.Session.Roster.First(entry => entry.Seat.Value != mine).Seat;
            var before = harness.Session.Roster
                .Select(entry => entry.Seat.Value + ":" + entry.Occupancy).ToArray();

            harness.SendRaw(1, PrivateSessionFrameCodec.ClaimSeatFrame(
                new SeatClaimMessage(theirs)));
            var client = harness.Client(1);

            var model = PrivateLobbyModel.ForClient(client);

            Assert.That(client.State, Is.EqualTo(PrivateSessionClientState.Seated),
                "A refused seat change leaves the connection holding the seat it had.");
            Assert.That(client.RejectionReason,
                Is.EqualTo(SessionRejectionReason.SeatChangeRefused));
            Assert.That(model.FeedbackLabel, Does.StartWith("A seat is fixed for the session"));
            Assert.That(model.Offers(PrivateLobbyControl.Ready), Is.True,
                "Being told no about another seat does not take away what this seat may do.");
            Assert.That(
                harness.Session.Roster.Select(entry => entry.Seat.Value + ":" + entry.Occupancy),
                Is.EqualTo(before), "The seat map moved on a refusal.");
        }

        /// <summary>
        /// Every named feedback case of the block's second acceptance check, each reaching a player
        /// as its own bounded sentence rather than as a state with no words.
        /// </summary>
        [Test]
        public void EveryNamedFeedbackCase_ReachesTheSurfaceAsItsOwnSentence()
        {
            var named = new[]
            {
                SessionRejectionReason.SeatAlreadyClaimed,
                SessionRejectionReason.SeatChangeRefused,
                SessionRejectionReason.SessionFull,
                SessionRejectionReason.BuildIdentityMismatch,
                SessionRejectionReason.SnapshotSchemaMismatch,
                SessionRejectionReason.ContentDigestMismatch,
                SessionRejectionReason.MapRevisionMismatch,
                SessionRejectionReason.ProtocolFamilyMismatch,
                SessionRejectionReason.ProtocolVersionMismatch,
                SessionRejectionReason.SessionKeyMismatch,
                SessionRejectionReason.ReconnectNotAuthorized
            };

            var sentences = named.Select(PrivateLobbyLabels.Refusal).ToArray();
            Assert.That(sentences.Distinct().Count(), Is.EqualTo(named.Length),
                "Two different refusals reading identically would tell a player nothing about " +
                "which one happened.");
            Assert.That(sentences.All(sentence => sentence.Length > 0), Is.True);
        }

        [Test]
        public void TheSeatlessHostsLobby_NeverOffersReadiness()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();

            var model = PrivateLobbyModel.ForHost(harness.Session, string.Empty);

            Assert.That(model.Role, Is.EqualTo(PrivateLobbyRole.Host));
            Assert.That(model.Controls, Is.EqualTo(new[] { PrivateLobbyControl.Leave }),
                "The host holds no seat by the topology's own rule, so it has nothing of its own " +
                "to be ready with.");
            Assert.That(model.Seats.Any(row => row.IsOwnSeat), Is.False);
            Assert.That(model.StatusLabel, Does.Contain("6/6 SEATED"));
            Assert.That(model.SessionLabel, Is.EqualTo(harness.Session.SessionCorrelation));
        }

        // --- THIRTEENTH amendment: fill empty seats with test drivers -----------------------------

        [Test]
        public void TheSeatlessHostsLobby_OffersFillWithTestDrivers_WhileASeatIsStillOpen()
        {
            var harness = PrivateOnlineWireHarness.Compose();

            var model = PrivateLobbyModel.ForHost(harness.Session, string.Empty);

            // NUnit 3.5's Does.Contain over a sequence binds the string overload against an enum
            // and fails to compile under Unity; Any(...) + Is.True is this codebase's own fix shape.
            Assert.That(model.Controls.Any(control => control == PrivateLobbyControl.FillWithTestDrivers),
                Is.True,
                "A freshly composed session has every seat Open, which is exactly the state this " +
                "control exists for.");
        }

        [Test]
        public void TheSeatlessHostsLobby_StopsOfferingFillWithTestDrivers_OnceEverySeatIsClaimed()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();

            var model = PrivateLobbyModel.ForHost(harness.Session, string.Empty);

            Assert.That(model.Controls.Any(control => control == PrivateLobbyControl.FillWithTestDrivers),
                Is.False,
                "Nothing left to fill; offering it anyway would invite pressing it for no effect.");
        }

        [Test]
        public void APartiallySeatedHost_StillOffersFillWithTestDrivers_ForTheSeatsStillOpen()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.Connect(1, "red-player-01");
            harness.Pump();

            var model = PrivateLobbyModel.ForHost(harness.Session, string.Empty);

            Assert.That(model.Controls.Any(control => control == PrivateLobbyControl.FillWithTestDrivers),
                Is.True);
        }

        [Test]
        public void ATestDriverSeat_IsLabeledOnlyOnTheHostsOwnRow_FromTheHostsOwnLocalRecord()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var filled = new HashSet<SeatId>(new[] { new SeatId("red-player-01") });

            var model = PrivateLobbyModel.ForHost(harness.Session, string.Empty, filled);

            var filledRow = model.Seats.Single(row => row.Seat.Value == "red-player-01");
            Assert.That(filledRow.IsTestDriverSeat, Is.True);
            var otherRow = model.Seats.Single(row => row.Seat.Value == "blue-player-01");
            Assert.That(otherRow.IsTestDriverSeat, Is.False,
                "The set names one seat; every other row must read false, not inherit true.");
        }

        [Test]
        public void ForHost_WithNoTestDriverSeatsArgument_LabelsNoRowATestDriver()
        {
            var harness = PrivateOnlineWireHarness.Compose();

            var model = PrivateLobbyModel.ForHost(harness.Session, string.Empty);

            Assert.That(model.Seats.All(row => !row.IsTestDriverSeat), Is.True,
                "Every existing caller (and fixture) that never heard of this feature must see " +
                "exactly the same rows it always did.");
        }

        // --- Privacy and the presentation-only boundary -------------------------------------------

        /// <summary>
        /// The block's second acceptance check ends "without exposing another player's private
        /// match state or sensitive diagnostic data". The lobby is read at the moment it knows the
        /// most - six seats, a started match - and every word it would draw is searched for the
        /// things it must never carry.
        /// </summary>
        [Test]
        public void NothingTheLobbyDraws_CarriesPrivateStateOrASecret()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            foreach (var ordinal in harness.Ordinals)
            {
                harness.Client(ordinal).TrySetReadiness(true);
                harness.Pump();
            }

            var reader = harness.Ordinals[0];
            var mine = harness.SeatOf(reader);
            var mineHero = harness.MatchHost.Seats.Seats
                .Single(seat => seat.Id.Value == mine).HeroId;
            var hidden = harness.MatchHost.Projection.Cards
                .Where(card => !card.OwnerId.Equals(mineHero))
                .Select(card => card.Id.Value)
                .ToArray();
            Assert.That(hidden, Is.Not.Empty,
                "The official table deals other seats cards; with none there is nothing for this " +
                "check to be about.");
            var capabilities = harness.Ordinals
                .Select(harness.CapabilityDeliveredTo)
                .Where(value => value.Length > 0)
                .ToArray();
            Assert.That(capabilities, Is.Not.Empty);

            var drawn = Everything(PrivateLobbyModel.ForClient(harness.Client(reader)))
                .Concat(Everything(PrivateLobbyModel.ForHost(harness.Session, string.Empty)))
                .ToArray();
            var text = string.Join("\n", drawn);

            foreach (var card in hidden)
            {
                Assert.That(text.Contains(card), Is.False,
                    "The lobby drew another hero's card identity: " + card + ".");
            }

            foreach (var capability in capabilities)
            {
                Assert.That(text.Contains(capability), Is.False,
                    "The lobby drew a seat's owner-private reconnect capability.");
            }

            Assert.That(text.Contains("127.0.0.1"), Is.False,
                "Milestone 8.31's rule: no player-facing diagnostic names an address.");
        }

        /// <summary>
        /// The block's fourth acceptance check, made mechanical. No lobby source names a way into
        /// match authority, so lobby state cannot assign match ownership however a view is drawn;
        /// who holds which seat is the host's answer, arriving in the roster.
        /// </summary>
        [Test]
        public void NoLobbySource_ReachesMatchAuthority()
        {
            var sources = LobbySources();
            Assert.That(sources.Count, Is.EqualTo(3),
                "A scan that finds no lobby source is a broken rule rather than a pass; it found " +
                string.Join(", ", sources.Keys) + ".");

            var forbidden = new[]
            {
                "new MatchSession(", "MatchHost.Compose(", "IMatchSession", "DirectorCommand",
                "MatchSubmission", "RestoreSnapshot", ".Execute(", ".Submit("
            };

            foreach (var source in sources)
            {
                foreach (var fragment in forbidden)
                {
                    Assert.That(source.Value.Contains(fragment), Is.False,
                        source.Key + " names " + fragment + ". The lobby reads the session " +
                        "contracts and issues session calls; a path to the match authority here " +
                        "would let a surface decide what only the host may.");
                }
            }
        }

        [Test]
        public void TheLobbyModel_IsBuiltFromTheSessionContractsAndNothingElse()
        {
            var allowed = new[]
            {
                typeof(PrivateSessionClient),
                typeof(PrivateSessionHost),
                typeof(IReadOnlyList<SeatRosterEntry>),
                typeof(string),
                // THIRTEENTH amendment. Deliberately admitted, not a session-contract type: which
                // seats a spawned test driver claimed is knowledge the HOST PROCESS holds locally
                // (its own list of what it spawned), never a fact the session/roster carries - see
                // PrivateLobbySeatRow.IsTestDriverSeat's own remarks. ForHost reads it exactly like
                // the feedback string beside it: reported, not decided.
                typeof(ISet<SeatId>)
            };

            var factories = typeof(PrivateLobbyModel)
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => method.ReturnType == typeof(PrivateLobbyModel))
                .ToArray();
            Assert.That(factories.Length, Is.EqualTo(3),
                "A scan that finds no factory is a broken rule; it found " +
                string.Join(", ", factories.Select(method => method.Name)) + ".");

            foreach (var factory in factories)
            {
                foreach (var parameter in factory.GetParameters())
                {
                    Assert.That(allowed.Contains(parameter.ParameterType), Is.True,
                        factory.Name + " takes a " + parameter.ParameterType.Name + ". The lobby " +
                        "is a reading of the session contracts, so anything else here is state it " +
                        "would be deciding for itself.");
                }
            }

            var writable = typeof(PrivateLobbyModel)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Concat(typeof(PrivateLobbySeatRow)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance))
                .Where(property => property.CanWrite)
                .Select(property => property.Name)
                .ToArray();
            Assert.That(writable, Is.Empty,
                "A settable member would let a view write seat ownership into the lobby's own " +
                "state instead of reading the host's answer.");
        }

        [Test]
        public void TheLobbyModel_NamesNoEngineType()
        {
            var sources = LobbySources();
            var model = sources.Single(pair => pair.Key == "PrivateLobbyModel.cs");

            Assert.That(model.Value.Contains("UnityEngine"), Is.False,
                "The lobby's model lives beside the session contracts precisely so both test " +
                "tiers read exactly what a player will.");
        }

        // --- The invitation mechanism the amended record selected ---------------------------------

        /// <summary>
        /// The lobby flag is the third opt-in beside the two command-line driver flags, and it
        /// carries no invitation of its own: a player types the address, port and key in.
        /// </summary>
        [Test]
        public void TheLobbyFlag_SelectsTheInteractiveRoleAndCarriesNoInvitation()
        {
            var options = PrivateSessionLaunchOptions.Parse(
                new[] { PrivateSessionLaunchOptions.LobbyFlag });

            Assert.That(options.IsValid, Is.True, options.Error);
            Assert.That(options.Role, Is.EqualTo(PrivateSessionRole.Lobby));
            Assert.That(options.Endpoint, Is.Null);
            Assert.That(options.Seat.IsDefined, Is.False);
            Assert.That(options.SessionKey, Is.Empty);
        }

        [Test]
        public void TheLobbyFlag_IsExclusiveWithTheCommandLineRolesAndTheirValues()
        {
            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                PrivateSessionLaunchOptions.LobbyFlag, PrivateSessionLaunchOptions.HostFlag
            }).IsValid, Is.False);

            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                PrivateSessionLaunchOptions.ClientFlag, PrivateSessionLaunchOptions.LobbyFlag
            }).IsValid, Is.False);

            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                PrivateSessionLaunchOptions.LobbyFlag,
                PrivateSessionLaunchOptions.AddressFlag, "127.0.0.1:45677"
            }).IsValid, Is.False, "An invitation on the command line is a second answer.");
        }

        /// <summary>
        /// A normal launch and both command-line roles are untouched, which is what the 8.31a/8.31b
        /// gates and milestone 8.35c's rehearsal depend on.
        /// </summary>
        [Test]
        public void TheExistingDriverRoles_AreUnchangedByTheLobbyFlag()
        {
            Assert.That(PrivateSessionLaunchOptions.Parse(Array.Empty<string>()).Role,
                Is.EqualTo(PrivateSessionRole.None));

            var host = PrivateSessionLaunchOptions.Parse(
                new[] { PrivateSessionLaunchOptions.HostFlag });
            Assert.That(host.IsValid, Is.True, host.Error);
            Assert.That(host.Role, Is.EqualTo(PrivateSessionRole.Host));
            Assert.That(host.Endpoint.Port, Is.EqualTo(PrivateSessionProtocol.DefaultPort));

            var client = PrivateSessionLaunchOptions.Parse(new[]
            {
                PrivateSessionLaunchOptions.ClientFlag,
                PrivateSessionLaunchOptions.AddressFlag, "127.0.0.1:45677",
                PrivateSessionLaunchOptions.SeatFlag, "red-player-01"
            });
            Assert.That(client.IsValid, Is.True, client.Error);
            Assert.That(client.Role, Is.EqualTo(PrivateSessionRole.Client));
            Assert.That(client.Seat.Value, Is.EqualTo("red-player-01"));
        }

        /// <summary>
        /// The join-failure half of the block's second acceptance check: a host that is not there
        /// is words on the entry form rather than a state with nothing said about it.
        /// </summary>
        [Test]
        public void AJoinToAHostThatIsNotListening_FailsWithWordsTheEntryFormCanCarry()
        {
            // The endpoint type refuses port 0 by design, so a throwaway bind learns a free
            // ephemeral port and releases it - leaving an address nothing is listening on.
            var probe = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            probe.Start();
            var port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            Assert.That(
                PrivateSessionEndpoint.TryCreate("127.0.0.1", port, out var endpoint, out _),
                Is.True);
            using (var transport = new TcpSessionClientTransport())
            {
                Assert.That(transport.Connect(endpoint, 2, out var error), Is.False,
                    "Nothing is listening on the port this test just released.");
                Assert.That(error, Is.Not.Empty,
                    "A failed join must report a reason a surface can print.");

                var model = PrivateLobbyModel.Entry(
                    Array.Empty<SeatRosterEntry>(), "The host could not be reached: " + error);
                Assert.That(model.FeedbackLabel, Is.Not.Empty);
                Assert.That(model.FeedbackLabel.Length,
                    Is.LessThanOrEqualTo(PrivateLobbyLabels.MaxLabelChars));
                Assert.That(model.FeedbackLabel.Contains("127.0.0.1"), Is.False,
                    "A join failure says what happened, never where.");
            }
        }

        // --- Which binds an unkeyed session may take ----------------------------------------------

        /// <summary>
        /// Milestone 8.35a1. An unkeyed session derives no channel keys, so its link stays
        /// plaintext, and the architecture record permits an owner-private byte to cross plaintext
        /// only on the loopback interface. The rule now has one statement, because it had two
        /// surfaces and only the command line obeyed it.
        /// </summary>
        [Test]
        public void AnUnkeyedSession_MayBindOnlyLoopback_AndBothSurfacesSaySo()
        {
            Assert.That(PrivateSessionProtocol.BindRequiresSessionKey(true, string.Empty), Is.False,
                "An unkeyed loopback session is the local default and stays allowed.");
            Assert.That(PrivateSessionProtocol.BindRequiresSessionKey(false, string.Empty), Is.True,
                "An unkeyed bind off loopback would put owner-private bytes on a plaintext link.");
            Assert.That(PrivateSessionProtocol.BindRequiresSessionKey(false, "an-issued-key"),
                Is.False, "A keyed session is sealed from the key proof on, so it may bind any " +
                "interface. Refusing it would be a stricter rule than the record states.");
            Assert.That(PrivateSessionProtocol.BindRequiresSessionKey(true, "an-issued-key"),
                Is.False);

            var refused = PrivateSessionLaunchOptions.Parse(new[]
            {
                PrivateSessionLaunchOptions.HostFlag,
                PrivateSessionLaunchOptions.BindFlag, "203.0.113.10"
            });
            Assert.That(refused.IsValid, Is.False, "The command line refuses the same bind.");
            Assert.That(refused.Error,
                Does.Contain(PrivateSessionLaunchOptions.SessionKeyFlag),
                "A command line is refused in the words of a command line: it names the flag.");

            var accepted = PrivateSessionLaunchOptions.Parse(new[]
            {
                PrivateSessionLaunchOptions.HostFlag,
                PrivateSessionLaunchOptions.BindFlag, "203.0.113.10",
                PrivateSessionLaunchOptions.SessionKeyFlag, "an-issued-key"
            });
            Assert.That(accepted.IsValid, Is.True, accepted.Error);

            Assert.That(PrivateLobbyLabels.UnkeyedPublicBindRefusal, Does.Contain("session key"),
                "A lobby is refused in the words of a lobby: it names the field, not a flag.");
            Assert.That(PrivateLobbyLabels.UnkeyedPublicBindRefusal.Length,
                Is.LessThanOrEqualTo(PrivateLobbyLabels.MaxLabelChars),
                "A refusal is one bounded line like every other the lobby draws.");
        }

        // --- The one ending an invited playtest reads back out of a process ------------------------

        /// <summary>
        /// Milestone 8.36. Six people press the controls in an invited playtest, so the rehearsal
        /// driver that wrote milestone 8.35c's convergence markers is not attached and the ordinary
        /// lobby path said nothing at all about the ending it reached. One bounded line per process
        /// replaces it: the same shape from the seatless host and from a seat, and silence while
        /// the match is still being played.
        /// </summary>
        [Test]
        public void AnEndedMatch_IsOnePublicOutcomeLineFromTheHostAndFromASeat()
        {
            var ended = MatchOutcome.Won(
                MatchVictoryCause.FinalWaveFlipped, new TeamId("blue-team"), new TeamId("red-team"));

            Assert.That(
                PrivateOnlineDiagnostics.OutcomeLine(string.Empty, 2250, ended),
                Is.EqualTo("MATCH_OUTCOME version=2250 cause=FinalWaveFlipped winner=blue-team"),
                "The dedicated host names no seat, because it holds none.");
            Assert.That(
                PrivateOnlineDiagnostics.OutcomeLine("red-player-02", 2250, ended),
                Is.EqualTo(
                    "MATCH_OUTCOME seat=red-player-02 version=2250 cause=FinalWaveFlipped " +
                    "winner=blue-team"),
                "A seat names itself, so six returned logs can be told apart and each compared " +
                "with the host's.");
            Assert.That(
                PrivateOnlineDiagnostics.OutcomeLine("red-player-02", 41, MatchOutcome.Unfinished),
                Is.Empty,
                "A match still being played has no ending to report, and a process with nothing " +
                "to report writes nothing.");
        }

        /// <summary>
        /// The line cannot carry owner-private state, and that has to be structural rather than
        /// careful: nothing owner-private has a parameter to arrive through.
        /// </summary>
        [Test]
        public void TheOutcomeLine_IsGivenOnlyASeatAVersionAndThePublicOutcome()
        {
            var parameters = typeof(PrivateOnlineDiagnostics)
                .GetMethod(nameof(PrivateOnlineDiagnostics.OutcomeLine))
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();

            Assert.That(
                parameters,
                Is.EqualTo(new[] { typeof(string), typeof(long), typeof(MatchOutcome) }),
                "A hand, an unrevealed commitment identity, an owner-only offer or a reconnect " +
                "capability must have no way in. Widening this signature is how a diagnostic " +
                "line becomes a side channel.");
        }

        // --- Helpers ------------------------------------------------------------------------------

        private static IReadOnlyList<string> SeatsOf(PrivateSessionTestDriver driver) =>
            driver.Session.Roster.Select(entry => entry.Seat.Value).ToArray();

        private static SeatRosterEntry RosterOf(PrivateSessionHost session, string seat) =>
            session.Roster.Single(entry => entry.Seat.Value == seat);

        private static void SetReadiness(PrivateSessionTestDriver driver, int ordinal, bool ready) =>
            driver.Submit(ordinal, PrivateSessionFrameCodec.SetReadinessFrame(
                new SeatReadinessMessage(ready)));

        /// <summary>
        /// 8.35z54. The composer these lobby cases need, and it is the one milestone
        /// 8.35z43 documents as legitimate: a scene that has already composed exactly what was
        /// asked for returns <c>null</c> and the session stays on the match it serves.
        /// </summary>
        /// <remarks>
        /// It became necessary the moment every seat picks: a completed selection with NO composer
        /// is a wiring defect and is thrown on by name, which is z43's guard working exactly as
        /// written. These cases are about readiness, the announcement and the one-way transition
        /// rather than about what gets composed, so the honest composer for them is the one that
        /// composes nothing.
        /// </remarks>
        private sealed class NoRecompositionComposer : IDeferredMatchComposition
        {
            public IMatchHost ComposeForLineup(HeroLineup lineup) => null;
        }

        private static PrivateSessionTestDriver SeatEveryone(out IReadOnlyList<string> seats)
        {
            var driver = PrivateSessionTestDriver.Compose();
            driver.Session.Composition = new NoRecompositionComposer();
            seats = SeatsOf(driver);
            for (var index = 0; index < seats.Count; index++)
            {
                driver.Seat(index + 1, seats[index]);
            }

            return driver;
        }

        /// <summary>
        /// Brings a session all the way out of the lobby: every seat taken, every seat ready, and
        /// then - 8.35z54 - every seat's hero picked, because readiness OPENS rulebook
        /// p.22's selection and the last pick is what ends the lobby.
        /// </summary>
        /// <remarks>
        /// The helper carries the extra step rather than every case repeating it, which is what kept
        /// this file's edit for the flow change to one method: the nine cases that go through here
        /// are about the ANNOUNCEMENT, the one-way transition and the seat states around them, and
        /// none of them is about how the table got there.
        /// </remarks>
        private static PrivateSessionTestDriver ReadyEveryone(out IReadOnlyList<string> seats)
        {
            var driver = SeatEveryone(out seats);
            for (var index = 0; index < seats.Count; index++)
            {
                SetReadiness(driver, index + 1, true);
            }

            Assert.That(driver.Session.IsPickPhaseOpen, Is.True,
                "Readiness opens the picking rather than ending the lobby.");
            PickEveryHero(driver, seats);
            Assert.That(driver.Session.HasMatchStarted, Is.True);
            return driver;
        }

        /// <summary>
        /// 8.35z54. Runs p.22's order to completion, each seat taking the first hero the
        /// stage still has available for it.
        /// </summary>
        private static void PickEveryHero(
            PrivateSessionTestDriver driver, IReadOnlyList<string> seats)
        {
            var guard = 0;
            while (!driver.Session.HasMatchStarted && guard++ <= seats.Count)
            {
                var picking = driver.Session.Selection.PickingSeat;
                if (!picking.IsDefined)
                {
                    break;
                }

                var ordinal = 0;
                for (var index = 0; index < seats.Count; index++)
                {
                    if (seats[index] == picking.Value)
                    {
                        ordinal = index + 1;
                        break;
                    }
                }

                Assert.That(ordinal, Is.GreaterThan(0),
                    "The seat p.22's order is on must be one of the seats this session serves.");
                var hero = driver.Session.Selection.AvailableHeroes[0];
                driver.Submit(ordinal, PrivateSessionFrameCodec.ChooseHeroFrame(
                    new HeroChoiceMessage(hero.HeroIdValue)));
            }
        }

        private static IReadOnlyList<SeatRosterEntry> LastRoster(
            PrivateSessionTestDriver driver, int ordinal)
        {
            var frame = driver.Outbox.FramesFor(ordinal)
                .Last(candidate => candidate.Kind == SessionMessageKind.RosterUpdate);
            Assert.That(PrivateSessionFrameCodec.TryDecodeRosterUpdate(frame.Payload, out var roster),
                Is.True);
            return roster.Roster;
        }

        private static int RosterFrameCount(PrivateSessionTestDriver driver, int ordinal) =>
            driver.Outbox.FramesFor(ordinal)
                .Count(frame => frame.Kind == SessionMessageKind.RosterUpdate);

        private static string GrantedCapability(PrivateSessionTestDriver driver, int ordinal)
        {
            var frame = driver.Outbox.FramesFor(ordinal)
                .Last(candidate => candidate.Kind == SessionMessageKind.SeatGranted);
            Assert.That(PrivateSessionFrameCodec.TryDecodeSeatGranted(frame.Payload, out var granted),
                Is.True);
            return granted.ReconnectCapability;
        }

        /// <summary>Every string a surface built from this model would draw.</summary>
        private static IEnumerable<string> Everything(PrivateLobbyModel model)
        {
            yield return model.StatusLabel;
            yield return model.FeedbackLabel;
            yield return model.SessionLabel;
            foreach (var row in model.Seats)
            {
                yield return row.SeatLabel;
                yield return row.HeroLabel;
                yield return row.TeamLabel;
                yield return row.OccupancyLabel;
                yield return row.ReadinessLabel;
            }

            foreach (var control in model.Controls)
            {
                yield return PrivateLobbyLabels.Control(control);
            }
        }

        /// <summary>
        /// The lobby's three production sources by file name, read as text so the rule runs
        /// identically in the Unity Editor and the cross-platform harness.
        /// </summary>
        private static IReadOnlyDictionary<string, string> LobbySources()
        {
            var production = ProductionSourceRoot();
            var found = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var path in Directory.GetFiles(production, "PrivateLobby*.cs",
                SearchOption.AllDirectories))
            {
                found[Path.GetFileName(path)] = File.ReadAllText(path);
            }

            return found;
        }

        private static string ProductionSourceRoot()
        {
            var roots = new[]
            {
                Directory.GetCurrentDirectory(),
                TestContext.CurrentContext.TestDirectory
            };

            foreach (var root in roots)
            {
                var directory = new DirectoryInfo(root);
                while (directory != null)
                {
                    var candidate = Path.Combine(directory.FullName, "Assets", "GuardsDigital");
                    if (Directory.Exists(candidate))
                    {
                        return candidate;
                    }

                    directory = directory.Parent;
                }
            }

            Assert.Fail("Could not locate Assets/GuardsDigital from the runner's directory.");
            return null;
        }
    }
}
