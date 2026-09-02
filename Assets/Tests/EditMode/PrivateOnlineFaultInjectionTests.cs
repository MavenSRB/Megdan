using System;
using System.IO;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.33. The deterministic fault plan, executed: dropped, delayed, duplicated,
    /// reordered, replayed, stale, corrupt, truncated, unknown and oversized input, each under a
    /// named fixed plan, and after each one every client either converges on the host's
    /// authorized version or stands in exactly one explicit recoverable or terminal state.
    /// </summary>
    /// <remarks>
    /// The injector sits where the fault-plan constants put it - above the transport, below the
    /// codec - so every case here replays identically at the 8.35c rehearsal. The sessions are
    /// unkeyed: that is the loopback-legitimate plaintext configuration, and it is the one where
    /// delivery faults reach the protocol layer at all - on a keyed session the seal layer
    /// refuses a tampered or replayed frame first, which milestone 8.32's channel cases prove.
    /// </remarks>
    public sealed class PrivateOnlineFaultInjectionTests
    {
        /// <summary>
        /// Loss, healed without a round trip: the next complete projection past the gap is
        /// adopted whole, because a newer complete view is exactly what a resync would answer.
        /// </summary>
        [Test]
        public void ADroppedProjectionPush_IsHealedByTheNextCompleteProjection()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var injector = new PrivateSessionFaultInjector(new PrivateSessionFaultPlan(
                "drop-one-projection",
                new[] { new SessionFaultStep(0, SessionFaultKind.Drop) }));
            harness.SetHostToClientInjector(2, injector);

            var actor = harness.Client(1);
            var observer = harness.Client(2);
            Assert.That(actor.TrySubmitCommand(
                actor.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            harness.Pump();
            Assert.That(observer.LatestProjection.Version,
                Is.LessThan(harness.MatchHost.Version),
                "The dropped push left the observer behind, silently.");

            // The next accepted step comes from whichever seat still holds an offer - the first
            // actor's commitment was answered, so its own list is empty until the reveal.
            harness.SubmitOneAcceptedCommand(excludedOrdinal: 2);

            Assert.That(observer.State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(observer.RecoveredProjectionGapCount, Is.EqualTo(1));
            Assert.That(observer.ResyncRequestCount, Is.Zero,
                "A complete projection past the gap needs no request.");
            Assert.That(observer.LatestProjection.Version, Is.EqualTo(harness.MatchHost.Version));
            harness.AssertConverged();
            Assert.That(injector.AppliedActions,
                Is.EqualTo(new[] { "drop-one-projection: frame 0 dropped" }));
        }

        /// <summary>
        /// Loss surfaced by a result instead: the seat that missed a push and then acted learns
        /// of the gap from its own answer, enters the one recoverable state, requests one bounded
        /// resync, and converges.
        /// </summary>
        [Test]
        public void AMissedPush_SurfacedByAResult_TriggersOneBoundedResyncAndConverges()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.SetHostToClientInjector(2, new PrivateSessionFaultInjector(
                new PrivateSessionFaultPlan(
                    "drop-then-act",
                    new[] { new SessionFaultStep(0, SessionFaultKind.Drop) })));

            var actor = harness.Client(1);
            var stale = harness.Client(2);
            Assert.That(actor.TrySubmitCommand(
                actor.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            harness.Pump();

            // The stale seat acts against the view it still holds.
            Assert.That(stale.TrySubmitCommand(
                stale.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            harness.Pump();

            Assert.That(stale.LastSubmissionResult.Accepted, Is.False);
            Assert.That(stale.LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));
            Assert.That(stale.ResyncRequestCount, Is.EqualTo(1),
                "The gap under the result asked for exactly one resync.");
            Assert.That(stale.State, Is.EqualTo(PrivateSessionClientState.Seated),
                "The resync answer returned the seat to Seated.");
            Assert.That(stale.LatestProjection.Version, Is.EqualTo(harness.MatchHost.Version));
            harness.AssertConverged();
        }

        /// <summary>Delay within the bounded window: the late frame is discarded, nothing diverges.</summary>
        [Test]
        public void ADelayedProjection_ArrivesLate_AndIsDiscardedAsStale()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var injector = new PrivateSessionFaultInjector(new PrivateSessionFaultPlan(
                "delay-one-projection",
                new[] { new SessionFaultStep(0, SessionFaultKind.Delay, 1) }));
            harness.SetHostToClientInjector(2, injector);

            var observer = harness.Client(2);
            harness.SubmitOneAcceptedCommand(excludedOrdinal: 2);
            harness.SubmitOneAcceptedCommand(excludedOrdinal: 2);

            // The observer saw the second push first - a reorder of two - and the late first
            // push after it, stale.
            Assert.That(observer.RecoveredProjectionGapCount, Is.EqualTo(1));
            Assert.That(observer.DiscardedFrameCount, Is.EqualTo(1));
            Assert.That(observer.State, Is.EqualTo(PrivateSessionClientState.Seated));
            harness.AssertConverged();
        }

        /// <summary>Duplication: the second delivery is a stale sequence, discarded unapplied.</summary>
        [Test]
        public void ADuplicatedProjection_IsDiscardedOnItsSecondDelivery()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.SetHostToClientInjector(2, new PrivateSessionFaultInjector(
                new PrivateSessionFaultPlan(
                    "duplicate-one-projection",
                    new[] { new SessionFaultStep(0, SessionFaultKind.Duplicate) })));

            var actor = harness.Client(1);
            var observer = harness.Client(2);
            Assert.That(actor.TrySubmitCommand(
                actor.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            harness.Pump();

            Assert.That(observer.DiscardedFrameCount, Is.EqualTo(1));
            Assert.That(observer.State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(observer.LatestProjection.Version, Is.EqualTo(harness.MatchHost.Version));
            harness.AssertConverged();
        }

        /// <summary>
        /// A replayed submission identity - the retry shape - executes nothing twice.
        /// </summary>
        /// <remarks>
        /// Milestone 8.34 completed the failure policy's first row here, and this case moved with
        /// it. An identical repeat is answered from what that submission already did rather than
        /// refused, because refusing a retry is what makes a client ask again under a new name;
        /// a repeat of the identity asking something else is still refused outright. Neither runs
        /// anything twice, which is what the row is for and what the version and the ordered
        /// stream below are asserted against.
        /// </remarks>
        [Test]
        public void AReplayedSubmissionIdentity_CannotExecuteTwice()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var client = harness.Client(2);
            var command = client.LatestProjection.AuthorizedCommands[0];
            var version = client.LatestProjection.Version;

            harness.SendRaw(2, NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(harness.Identity.Correlation, 1, 1, 1),
                "wire-replay-1", version, NetworkCommandCodec.Encode(command))));
            Assert.That(client.LastSubmissionResult.Accepted, Is.True,
                client.LastSubmissionResult.Detail);
            var versionAfter = harness.MatchHost.Version;
            var sequenceAfter = harness.MatchHost.Sequence;
            Assert.That(versionAfter, Is.EqualTo(version + 1));

            // The same identity asking the same thing again, on the next sequence: answered from
            // the retained result, with nothing executed and nothing appended.
            harness.SendRaw(2, NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(harness.Identity.Correlation, 1, 2, 2),
                "wire-replay-1", versionAfter,
                NetworkCommandCodec.Encode(command))));
            Assert.That(client.LastSubmissionResult.Accepted, Is.True);
            Assert.That(client.LastSubmissionResult.Version, Is.EqualTo(versionAfter));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionAfter));
            Assert.That(harness.MatchHost.Sequence, Is.EqualTo(sequenceAfter));

            // The same identity asking something else is a used name, and is refused.
            var other = harness.Client(3).LatestProjection.AuthorizedCommands[0];
            harness.SendRaw(3, NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(harness.Identity.Correlation, 1, 1, 1),
                "wire-replay-1", harness.MatchHost.Version,
                NetworkCommandCodec.Encode(other))));
            Assert.That(harness.Client(3).LastSubmissionResult.Accepted, Is.False);
            Assert.That(harness.Client(3).LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.DuplicateSubmission));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionAfter));
            Assert.That(harness.MatchHost.Sequence, Is.EqualTo(sequenceAfter));
            harness.AssertConverged();
        }

        /// <summary>
        /// The three expected-version shapes on one wire: current is accepted, a version the
        /// authority has moved past is refused as stale and healed, and an invalid future version
        /// is refused exactly the same way - a claimed version is never trusted.
        /// </summary>
        [Test]
        public void StaleCurrentAndFutureVersions_AnswerByName()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var client = harness.Client(3);
            var current = client.LatestProjection.Version;
            var command = client.LatestProjection.AuthorizedCommands[0];

            harness.SendRaw(3, NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(harness.Identity.Correlation, 1, 1, 1),
                "wire-version-future", 999_999, NetworkCommandCodec.Encode(command))));
            Assert.That(client.LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));

            harness.SendRaw(3, NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(harness.Identity.Correlation, 1, 2, 2),
                "wire-version-current", current, NetworkCommandCodec.Encode(command))));
            Assert.That(client.LastSubmissionResult.Accepted, Is.True,
                client.LastSubmissionResult.Detail);

            // The version the first answer was decided against now trails the authority by one,
            // so the same expected version is the stale shape - no arithmetic invented it.
            harness.SendRaw(3, NetworkMessageCodec.SubmitCommandFrame(new NetworkSubmissionMessage(
                new NetworkEnvelope(harness.Identity.Correlation, 1, 3, 3),
                "wire-version-stale", current, NetworkCommandCodec.Encode(command))));
            Assert.That(client.LastSubmissionResult.Accepted, Is.False);
            Assert.That(client.LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));
            harness.AssertConverged();
        }

        /// <summary>
        /// A corrupted client frame is an explicit terminal state for that connection alone, with
        /// nothing mutated: the flipped byte lands in the envelope's session correlation, and a
        /// wrong session is a protocol violation, not a rules answer.
        /// </summary>
        [Test]
        public void ACorruptedSubmission_ClosesThatConnectionAlone_WithNothingMutated()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var injector = new PrivateSessionFaultInjector(new PrivateSessionFaultPlan(
                "corrupt-one-byte",
                new[] { new SessionFaultStep(0, SessionFaultKind.Corrupt, 14) }));
            harness.SetClientToHostInjector(2, injector);

            var versionBefore = harness.MatchHost.Version;
            var hostile = harness.Client(2);
            Assert.That(hostile.TrySubmitCommand(
                hostile.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            harness.Pump();

            Assert.That(hostile.State, Is.EqualTo(PrivateSessionClientState.Disconnected),
                "The corrupted frame ended its own connection, by name.");
            Assert.That(hostile.RejectionReason,
                Is.EqualTo(SessionRejectionReason.EnvelopeMismatch).Or
                    .EqualTo(SessionRejectionReason.MalformedFrame));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionBefore),
                "Nothing reached the authority.");
            Assert.That(harness.Client(1).State, Is.EqualTo(PrivateSessionClientState.Seated),
                "Only the corrupted connection closed.");
            harness.AssertConverged();
        }

        /// <summary>A corrupted host push is the client's explicit terminal state, not a wrong board.</summary>
        [Test]
        public void ACorruptedProjectionPush_FaultsTheReceivingClientExplicitly()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            // The frame's last byte: the projection payload's own trailing field, past the frame
            // header and envelope, so the stream and envelope parse and the projection decode
            // itself must refuse. The injector clamps the index to the final byte.
            harness.SetHostToClientInjector(2, new PrivateSessionFaultInjector(
                new PrivateSessionFaultPlan(
                    "corrupt-projection-payload",
                    new[] { new SessionFaultStep(0, SessionFaultKind.Corrupt, int.MaxValue) })));

            var actor = harness.Client(1);
            Assert.That(actor.TrySubmitCommand(
                actor.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            harness.Pump();

            Assert.That(harness.Client(2).State,
                Is.EqualTo(PrivateSessionClientState.Faulted).Or
                    .EqualTo(PrivateSessionClientState.Disconnected),
                "A view that does not decode is never shown; the connection ends instead.");
            Assert.That(actor.State, Is.EqualTo(PrivateSessionClientState.Seated));
        }

        /// <summary>
        /// A truncated frame corrupts the byte stream itself, and the stream discipline refuses
        /// the connection rather than resynchronizing on garbage.
        /// </summary>
        [Test]
        public void ATruncatedFrame_RejectsTheStreamItself()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.SetClientToHostInjector(2, new PrivateSessionFaultInjector(
                new PrivateSessionFaultPlan(
                    "truncate-one-frame",
                    new[] { new SessionFaultStep(0, SessionFaultKind.Truncate, 20) })));

            var versionBefore = harness.MatchHost.Version;
            var hostile = harness.Client(2);
            Assert.That(hostile.TrySubmitCommand(
                hostile.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            harness.Pump();
            // The stream is misaligned from the cut on: the following frames' bytes finish the
            // half-declared payload as garbage, and the remainder reads as a header that is not
            // one. Enough bytes must follow to overrun the declared length.
            for (var filler = 0; filler < 40; filler++)
            {
                harness.SendRaw(2, PrivateSessionFrameCodec.GoodbyeFrame());
            }

            Assert.That(hostile.State, Is.Not.EqualTo(PrivateSessionClientState.Seated),
                "A truncated stream cannot keep its seat acting.");
            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionBefore));
            Assert.That(harness.Client(1).State, Is.EqualTo(PrivateSessionClientState.Seated));
        }

        /// <summary>An unknown message kind is refused at the stream, before any decode.</summary>
        [Test]
        public void AnUnknownMessageKind_IsRefusedBeforeDecode()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var versionBefore = harness.MatchHost.Version;

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(PrivateSessionProtocol.FrameMagic0);
                writer.Write(PrivateSessionProtocol.FrameMagic1);
                writer.Write(PrivateSessionProtocol.FrameMagic2);
                writer.Write(PrivateSessionProtocol.FrameMagic3);
                writer.Write((ushort)PrivateSessionProtocol.Version);
                // One past the highest kind this protocol defines, so the case stays about an
                // unknown kind as the vocabulary grows rather than about whichever number was
                // spare when it was written.
                writer.Write((ushort)((int)SessionMessageKind.ReclaimSeat + 1));
                writer.Write(0);
                writer.Flush();
                harness.SendRaw(2, stream.ToArray());
            }

            Assert.That(harness.Client(2).State, Is.Not.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(harness.Client(2).RejectionReason,
                Is.EqualTo(SessionRejectionReason.MalformedFrame));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionBefore));
        }

        /// <summary>A declared length one past the bound is refused before any buffer exists.</summary>
        [Test]
        public void AnOversizedDeclaredLength_IsRefusedBeforeAllocation()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(PrivateSessionProtocol.FrameMagic0);
                writer.Write(PrivateSessionProtocol.FrameMagic1);
                writer.Write(PrivateSessionProtocol.FrameMagic2);
                writer.Write(PrivateSessionProtocol.FrameMagic3);
                writer.Write((ushort)PrivateSessionProtocol.Version);
                writer.Write((ushort)SessionMessageKind.SubmitCommand);
                writer.Write(PrivateSessionProtocol.MaxPayloadBytes + 1);
                writer.Flush();
                harness.SendRaw(2, stream.ToArray());
            }

            Assert.That(harness.Client(2).RejectionReason,
                Is.EqualTo(SessionRejectionReason.OversizedFrame));
            Assert.That(harness.Client(2).State, Is.Not.EqualTo(PrivateSessionClientState.Seated));
        }

        /// <summary>
        /// Forged envelope identity on each 8.33 message kind closes the connection before any
        /// seat-level answer, exactly as it does on a submission.
        /// </summary>
        [Test]
        public void AForgedEnvelope_OnAgreementOrResync_ClosesTheConnection()
        {
            var wrongSession = PrivateOnlineWireHarness.Compose();
            wrongSession.SeatAll();
            wrongSession.SendRaw(2, NetworkMessageCodec.RequestResyncFrame(
                new NetworkResyncRequestMessage(
                    new NetworkEnvelope("not-this-session", 1, 1, 1))));
            Assert.That(wrongSession.Client(2).RejectionReason,
                Is.EqualTo(SessionRejectionReason.EnvelopeMismatch));

            var skippedSequence = PrivateOnlineWireHarness.Compose();
            skippedSequence.SeatAll();
            var proposal = new NetworkEnvelope(skippedSequence.Identity.Correlation, 1, 9, 9);
            skippedSequence.SendRaw(2, NetworkMessageCodec.SubmitAgreementFrame(
                new NetworkAgreementSubmissionMessage(
                    proposal, "wire-forged-agreement", 0,
                    ForgedStatement())));
            Assert.That(skippedSequence.Client(2).RejectionReason,
                Is.EqualTo(SessionRejectionReason.EnvelopeMismatch));

            var wrongEpoch = PrivateOnlineWireHarness.Compose();
            wrongEpoch.SeatAll();
            wrongEpoch.SendRaw(2, NetworkMessageCodec.RequestResyncFrame(
                new NetworkResyncRequestMessage(
                    new NetworkEnvelope(wrongEpoch.Identity.Correlation, 2, 1, 1))));
            Assert.That(wrongEpoch.Client(2).RejectionReason,
                Is.EqualTo(SessionRejectionReason.EnvelopeMismatch));
        }

        private static TeamAgreementCommand ForgedStatement()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write("forged-proposal");
                writer.Write((int)TeamAgreementCommandKind.AgreeToMinionBattleSelection);
                writer.Write(false);
                writer.Flush();
                stream.Position = 0;
                using (var reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    Assert.That(NetworkProjectionDetailCodec.TryReadAgreementStatement(
                        reader, out var statement), Is.True);
                    return statement;
                }
            }
        }

        /// <summary>
        /// Interrupted synchronization: with the resync answer itself lost, the seat stands in
        /// the explicit recoverable state - and the next ordinary push converges it.
        /// </summary>
        [Test]
        public void ADroppedResyncAnswer_LeavesTheClientRecoverable_UntilTheNextPush()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.SetHostToClientInjector(2, new PrivateSessionFaultInjector(
                new PrivateSessionFaultPlan(
                    "drop-push-and-both-resyncs",
                    new[]
                    {
                        new SessionFaultStep(0, SessionFaultKind.Drop),
                        new SessionFaultStep(2, SessionFaultKind.Drop),
                        new SessionFaultStep(3, SessionFaultKind.Drop)
                    })));

            var actor = harness.Client(1);
            var stale = harness.Client(2);
            Assert.That(actor.TrySubmitCommand(
                actor.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            harness.Pump();
            Assert.That(stale.TrySubmitCommand(
                stale.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            harness.Pump();

            Assert.That(stale.State, Is.EqualTo(PrivateSessionClientState.Resynchronizing),
                "With every resync answer lost, the seat holds the recoverable state - it " +
                "neither acts on a stale view nor pretends convergence.");
            Assert.That(stale.TrySubmitCommand(
                stale.LatestProjection.AuthorizedCommands[0], out _), Is.False,
                "A resynchronizing seat does not submit.");

            harness.SubmitOneAcceptedCommand(excludedOrdinal: 2);
            Assert.That(stale.State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(stale.LatestProjection.Version, Is.EqualTo(harness.MatchHost.Version));
            harness.AssertConverged();
        }

        /// <summary>Host termination is terminal for every client, and nothing acts past it.</summary>
        [Test]
        public void HostTermination_IsTerminalForEveryClient()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.Session.Shutdown();
            harness.Pump();

            for (var ordinal = 1; ordinal <= 6; ordinal++)
            {
                Assert.That(harness.Client(ordinal).State,
                    Is.EqualTo(PrivateSessionClientState.Disconnected));
                Assert.That(harness.Client(ordinal).TrySubmitCommand(
                    harness.Client(ordinal).LatestProjection.AuthorizedCommands.FirstOrDefault()
                        ?? GuardsDigital.Core.Replay.DirectorCommand.AdvanceEndOfRound(),
                    out _), Is.False);
            }
        }
    }
}
