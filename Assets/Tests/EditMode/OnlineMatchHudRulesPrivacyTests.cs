using System;
using System.Reflection;
using GuardsDigital.Application;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35b independent-review closure checks for the online HUD's authoritative wait,
    /// reconnect, latency, and wire-version boundaries.
    /// </summary>
    public sealed class OnlineMatchHudRulesPrivacyTests
    {
        [Test]
        public void AcceptedResult_KeepsInputWaiting_UntilItsCompleteProjectionArrives()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var client = harness.Client(1);
            harness.SetHostToClientInjector(1, new PrivateSessionFaultInjector(
                new PrivateSessionFaultPlan(
                    "accepted-result-before-projection",
                    new[] { new SessionFaultStep(1, SessionFaultKind.Delay, 32) })));

            Assert.That(client.TrySubmitCommand(
                client.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            harness.Pump();

            Assert.That(client.LastSubmissionResult, Is.Not.Null);
            Assert.That(client.LastSubmissionResult.Accepted, Is.True,
                client.LastSubmissionResult.Detail);
            Assert.That(client.IsWaitingForHost, Is.True,
                "The result answers acceptance, but only its following complete projection says " +
                "which controls now exist.");

            harness.FlushInjectors();
            Assert.That(client.IsWaitingForHost, Is.False);
            Assert.That(client.State, Is.EqualTo(PrivateSessionClientState.Seated));
        }

        [Test]
        public void StaleResult_KeepsInputWaiting_UntilItsRecoveryProjectionArrives()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var stale = harness.Client(2);
            var staleVersion = stale.LatestProjection.Version;

            harness.SetHostToClientInjector(2, new PrivateSessionFaultInjector(
                new PrivateSessionFaultPlan(
                    "leave-one-seat-stale",
                    new[] { new SessionFaultStep(0, SessionFaultKind.Drop) })));
            var mover = harness.Client(1);
            Assert.That(mover.TrySubmitCommand(
                mover.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            harness.Pump();
            Assert.That(harness.MatchHost.Version, Is.GreaterThan(staleVersion));
            Assert.That(stale.LatestProjection.Version, Is.EqualTo(staleVersion));

            harness.SetHostToClientInjector(2, new PrivateSessionFaultInjector(
                new PrivateSessionFaultPlan(
                    "stale-result-before-recovery",
                    new[]
                    {
                        new SessionFaultStep(1, SessionFaultKind.Delay, 32),
                        new SessionFaultStep(2, SessionFaultKind.Delay, 32)
                    })));
            Assert.That(stale.TrySubmitCommand(
                stale.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            harness.Pump();

            Assert.That(stale.LastSubmissionResult, Is.Not.Null);
            Assert.That(stale.LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));
            Assert.That(stale.IsWaitingForHost, Is.True,
                "A stale refusal promises a current projection; the stale controls stay disabled " +
                "until that recovery is actually received.");

            harness.FlushInjectors();
            Assert.That(stale.IsWaitingForHost, Is.False);
            Assert.That(stale.State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(stale.LatestProjection.Version, Is.EqualTo(harness.MatchHost.Version));
        }

        [Test]
        public void NewerGapHealingProjection_ClearsAWait_WhenItsResultArrivesLateAndIsDiscarded()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var client = harness.Client(1);
            var recoveredBefore = client.RecoveredProjectionGapCount;
            harness.SetHostToClientInjector(1, new PrivateSessionFaultInjector(
                new PrivateSessionFaultPlan(
                    "projection-overtakes-result",
                    new[] { new SessionFaultStep(0, SessionFaultKind.Delay, 1) })));

            Assert.That(client.TrySubmitCommand(
                client.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            harness.Pump();

            Assert.That(client.RecoveredProjectionGapCount, Is.EqualTo(recoveredBefore + 1));
            Assert.That(client.DiscardedFrameCount, Is.GreaterThan(0),
                "The delayed result follows its newer complete projection and is stale by sequence.");
            Assert.That(client.IsWaitingForHost, Is.False,
                "The complete gap-healing projection is the authoritative answer boundary even " +
                "when the separate result was lost or overtaken.");
            Assert.That(client.State, Is.EqualTo(PrivateSessionClientState.Seated));
        }

        [Test]
        public void UnrelatedNewerProjection_AfterLocalSend_DoesNotClearThatSubmissionsWait()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var target = harness.Client(2);
            var previousVersion = target.LatestProjection.Version;

            var mover = harness.Client(1);
            Assert.That(mover.TrySubmitCommand(
                mover.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            Assert.That(harness.PumpOne(), Is.True,
                "The other seat's submission must reach the host and queue its projections.");
            Assert.That(harness.MatchHost.Version, Is.GreaterThan(previousVersion));
            Assert.That(target.LatestProjection.Version, Is.EqualTo(previousVersion),
                "The target has not received the unrelated push yet.");

            Assert.That(target.TrySubmitCommand(
                target.LatestProjection.AuthorizedCommands[0], out _), Is.True);
            Assert.That(target.IsWaitingForHost, Is.True);
            Assert.That(harness.PumpOneHostActionFor(2), Is.True,
                "The independently timed host stream must deliver its already-queued push.");

            Assert.That(target.LatestProjection.Version, Is.EqualTo(harness.MatchHost.Version));
            Assert.That(target.IsWaitingForHost, Is.True,
                "A newer but unsolicited projection says nothing about the submission still " +
                "queued in the opposite direction.");

            harness.Pump();
            Assert.That(target.LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));
            Assert.That(target.IsWaitingForHost, Is.False,
                "The projection correlated to the stale answer closes this submission's wait.");
        }

        [Test]
        public void ReconnectGrant_DoesNotRestoreInput_UntilTheFreshProjectionArrives()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var client = harness.Client(2);
            var oldEpoch = client.ConnectionEpoch;
            var oldRevision = client.ProjectionRevision;
            harness.DisconnectClient(2);

            harness.BeginReconnectClient(2);
            for (var step = 0; step < 128 && client.ConnectionEpoch == oldEpoch; step++)
            {
                Assert.That(harness.PumpOne(), Is.True, "The reconnect handshake stopped before its grant.");
            }

            Assert.That(client.ConnectionEpoch, Is.GreaterThan(oldEpoch),
                "The fixture must stop immediately after accepting the reconnect grant.");
            Assert.That(client.ProjectionRevision, Is.EqualTo(oldRevision),
                "No fresh match projection has crossed yet.");
            Assert.That(client.State, Is.Not.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(client.IsReconnecting, Is.True);
            Assert.That(client.TrySubmitCommand(
                client.LatestProjection.AuthorizedCommands[0], out _), Is.False,
                "The retained pre-disconnect view is visible context, never an actionable view.");

            for (var step = 0;
                 step < 128 && client.State != PrivateSessionClientState.Seated;
                 step++)
            {
                Assert.That(harness.PumpOne(), Is.True,
                    "The reconnect handshake stopped before its fresh projection.");
            }

            Assert.That(client.State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(client.IsReconnecting, Is.False);
            Assert.That(client.ProjectionRevision, Is.GreaterThan(oldRevision));
        }

        [Test]
        public void ReconnectTransportDialFailure_RestoresRetryEligibility_WithoutLosingTheCapability()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var client = harness.Client(2);
            harness.DisconnectClient(2);
            Assert.That(client.BeginReconnect(), Is.True);
            Assert.That(client.CanAttemptReconnect, Is.False,
                "Only one transport attempt may be armed at a time.");

            var method = typeof(PrivateSessionClient).GetMethod(
                "OnReconnectTransportFailed", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(method, Is.Not.Null,
                "A synchronous dial failure needs an explicit client-state transition.");
            method.Invoke(client, Array.Empty<object>());

            Assert.That(client.State, Is.EqualTo(PrivateSessionClientState.Disconnected));
            Assert.That(client.CanAttemptReconnect, Is.True);
            Assert.That(client.MatchFeedback, Does.Contain("Try reconnecting"));
            Assert.That(client.BeginReconnect(), Is.True,
                "The same owner-private capability remains eligible for a later dial.");
        }

        [Test]
        public void WaitingLatency_ChangesAtTheDeterministicThreshold_AndClearsOnRecovery()
        {
            var thresholdField = typeof(PrivateSessionClient).GetField(
                "DelayedHostResponseThresholdTicks", BindingFlags.Public | BindingFlags.Static);
            var delayedProperty = typeof(PrivateSessionClient).GetProperty(
                "IsHostResponseDelayed", BindingFlags.Instance | BindingFlags.Public);
            Assert.That(thresholdField, Is.Not.Null,
                "The delayed state needs one named bounded threshold.");
            Assert.That(delayedProperty, Is.Not.Null,
                "The HUD needs a transport-neutral delayed-state projection.");
            var threshold = (long)thresholdField.GetRawConstantValue();
            Assert.That(threshold, Is.GreaterThan(0));
            Assert.That(threshold, Is.LessThanOrEqualTo(5 * TimeSpan.TicksPerSecond));

            var now = 10 * TimeSpan.TicksPerSecond;
            var harness = PrivateOnlineWireHarness.Compose();
            var seat = harness.Session.Roster[0].Seat.Value;
            var client = harness.Connect(1, seat, timeSource: () => now);
            harness.Pump();
            Assert.That(client.State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(client.TrySubmitCommand(
                client.LatestProjection.AuthorizedCommands[0], out _), Is.True);

            Assert.That(ReadDelayed(delayedProperty, client), Is.False);
            now += threshold - 1;
            Assert.That(ReadDelayed(delayedProperty, client), Is.False);
            now++;
            Assert.That(ReadDelayed(delayedProperty, client), Is.True,
                "The player gets distinct feedback at the stated threshold, without a sleep.");

            harness.Pump();
            Assert.That(client.IsWaitingForHost, Is.False);
            Assert.That(ReadDelayed(delayedProperty, client), Is.False,
                "An answer plus its complete projection clears the latency state.");
        }

        /// <summary>
        /// The wire's own compatibility rule, restated at each version that moved it: an older
        /// peer is refused by the frame assembler, before either side interprets a payload whose
        /// shape it would read wrongly.
        /// </summary>
        /// <remarks>
        /// Milestone 8.35b took this to protocol 2, because SelectedActor gained
        /// FlipTieBreakerAfterResolution. Milestone 8.35f takes it to protocol 3, because the seat
        /// record gained <c>IsSeatUndoAvailable</c> - a flag in the middle of it, so a protocol 2
        /// peer would mis-decode everything after it - and because two message kinds were added
        /// that a protocol 2 host answers with <c>UnexpectedMessage</c>. Milestone 8.35g takes it to
        /// protocol 4 for the carried turn-release family, and milestone 8.35l to protocol 5 for the
        /// blocked-reason block on each action offer row. Milestone 8.35r takes it to protocol 6 for
        /// the seat-public match log and the standing rewind marker. Milestone 8.35u takes it to
        /// protocol 7 for the repeated awaited-hero group on DecisionOwner, which p.15's simultaneous
        /// leveling made plural. Milestone 8.35z14-blockerid takes it to protocol 8 for the blocker
        /// identity - two optional fields appended to the match log's own <c>BlockedAction</c> row.
        /// Every prior version is exercised, so the refusal is about the version and not about any
        /// one number.
        /// </remarks>
        [Test]
        public void SeatWireMeaning_UsesProtocolEight_AndRejectsEveryOlderFrame()
        {
            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(8),
                "Milestone 8.35z14-blockerid gave the match log's BlockedAction row the hero and card " +
                "naming the standing rule modification that refused a mid-resolution step: two more " +
                "optional fields appended after `amount`, so a protocol seven peer's own reader - " +
                "which stops there - would silently drop a fact rather than mis-decode a following " +
                "one, which is exactly the kind of drift the bounded header exists to make loud. " +
                "Milestone 8.35u makes rulebook p.15's leveling simultaneous, so the authority can " +
                "be waiting on several heroes at once and DecisionOwner carries a repeated hero " +
                "group where protocol six wrote one optional hero: a protocol six peer reads that " +
                "count where it expects a present/absent boolean and mis-decodes the rest of the " +
                "record. The paused transition's own detail row is byte-identical, because each " +
                "seat's payload is narrowed to the one item that seat may answer and so never " +
                "carried more than one. Protocol six opened the seat-public event channel: the " +
                "match log rides inside " +
                "the seat projection message as a bounded window of typed lines, and the projection " +
                "detail record gained the standing rewind marker at its end, so a protocol five " +
                "peer would read a line count it does not expect and run off the end of the " +
                "payload. DecisionOwner joined the read model in the same milestone and moved no " +
                "bytes, because the seat record has carried it since 8.32. Protocol five gave a " +
                "greyed action offer a reason: each offer row carries an optional block naming the " +
                "rule modification that refused it, so a protocol four peer would read that block " +
                "as the next offer's card id and mis-decode the whole remainder of the detail " +
                "record. Protocol four carried the eleventh pending family, " +
                "PendingTurnRelease, plus the ReleaseTurn command kind; protocol three carried " +
                "IsSeatUndoAvailable plus SubmitUndo and UndoResult, which protocol two peers could " +
                "not decode safely.");

            foreach (var older in new[] { 1, 2, 3, 4, 5, 6, 7 })
            {
                var bytes = PrivateSessionFrameCodec.ChallengeFrame(
                    new SessionChallengeMessage(new byte[] { 1, 2, 3 }));
                bytes[4] = (byte)older;
                bytes[5] = 0;
                var assembler = new PrivateSessionFrameAssembler();
                Assert.That(assembler.Add(bytes, 0, bytes.Length), Is.False,
                    "Protocol " + older + " must be refused before any payload is read.");
                Assert.That(assembler.Rejection,
                    Is.EqualTo(SessionRejectionReason.ProtocolVersionMismatch));
            }
        }

        private static bool ReadDelayed(PropertyInfo property, PrivateSessionClient client) =>
            (bool)property.GetValue(client);
    }
}
