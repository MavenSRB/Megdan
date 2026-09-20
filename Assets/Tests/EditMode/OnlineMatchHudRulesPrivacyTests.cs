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
        public void SeatWireMeaning_UsesProtocolTwelve_AndRejectsEveryOlderFrame()
        {
            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(19),
                "8.35z73 carries COIN-AWARD ROWS: the seat-public match log now names each " +
                "authoritative reward recipient and amount. No bytes grew, but the closed enum " +
                "vocabulary widened, so an /18 peer refuses rather than going dark. " +
                "8.61e carries the SPRUNG-TRAP ROW: the seat-public match log gained " +
                "MoveThroughTrapSprung, which is Min the Dragonmonk's mines going off - an enemy " +
                "hero moves through a mine, it is removed, and for every blast removed that hero " +
                "discards a card if able. NO BYTES GREW: the row spends the subject hero, the " +
                "origin and the amount the layout has carried since 8.35r, and the bump is here for " +
                "the reason 8.40f's and 8.51's were - NetworkMatchLogCodec.TryRead refuses any kind " +
                "its own build does not declare, so a version 17 peer meeting this number refuses the " +
                "whole FRAME and that seat goes dark. The row has to exist because the reveal has no " +
                "other surface: until it goes off a mine is facedown and every seat but its " +
                "controller's is handed a face naming no kind, and the board shows only a space " +
                "emptying. SCHEMA IS UNMOVED at v43 - the row is re-derived from the retained " +
                "rule-event log, and the pause the trap opens is an ordinary effect interrupt whose " +
                "kind the card-action payload already writes as an int. " +
                "8.53 carries the MATCH SCOREBOARD: NetworkSeatProjectionRecord gained a repeated " +
                "row - hero id plus six counters, kills, deaths, attacks at heroes, times defended, " +
                "minions killed and coins earned - written between the per-hero progression rows and " +
                "the Active-effect source list, so a version 16 peer reads that row count as the " +
                "source count and mis-decodes the entire remainder of the frame. It is CARRIED " +
                "because it cannot be derived: every fact these columns count does cross the wire, " +
                "as a seat-public match-log row, but that log is a BOUNDED WINDOW of recent lines, " +
                "so a client counting its own totals would report fewer kills the longer a match ran " +
                "and would start a reconnected seat's board from the moment it came back. SCHEMA " +
                "MOVED WITH IT to v43 - only the second time these two have moved together - because " +
                "the board is accumulated state rather than a derived reading, so it has to be " +
                "persisted. " +
                "8.51 carries a HOLD: the seat-public match log gained CardHeld, rulebook p.12's Hold performed by the seat's own command, so a version 15 peer refuses the whole frame for the same Enum.IsDefined reason a version 14 peer did below. NO BYTES GREW - the row spends the subject hero and subject card the layout has carried since 8.35r - and the schema stays at v42, the row being re-derived from the retained rule-event log's Turn.HoldActionPerformed. " +
                "8.40f carries a REVEAL that card text performed. The seat-public match log gained one member, CardRevealedByCardText, and NetworkMatchLog.TryRead validates every row's kind with Enum.IsDefined - so a protocol 14 peer meeting the new number does not mis-read the row, it refuses the whole FRAME and that seat goes dark, which is the one kind of change this ratchet exists for. THE ROW ITSELF GREW NO BYTES: MatchLogEntry has carried a subject hero and a subject card since 8.35r and this member spends exactly those two, so only the VOCABULARY widened. It is a new member rather than CardRevealed reused because every consumer of that one reads it as rulebook p.7's turn-card commitment - the flip cue borrows the hero's CURRENT CARD SLOT to play it at, the reveal batch is re-staged in printed-Initiative order because the commitment flow emits its reveals in roster order, and the channel's own one-for-one ledger asserts exactly as many such rows as Commitment.CardRevealed events occurred - and a card turned face up out of a HAND mid-resolution is in neither place, so reuse would have animated the wrong card and falsified a declared ledger. SCHEMA IS UNMOVED at v42: the reveal's memory is the DERIVED seat-public log, re-derived from the retained rule-event log on restore, replay and Undo, so nothing new is persisted and no corpus digest or pinned constant moves with it. " +
                "8.39g carries a SOLO ending. Every projected outcome before it named a winning TEAM and a losing team, because all three of the rulebook's victory sentences are about teams; Cutter's A Fistful of Coins awards the match to one HERO and leaves that hero's own team-mates among the losers, so the outcome row gained a hero field and the two team fields became the named absence for that shape. A protocol 13 peer reads the hero id where it expects the next record and mis-decodes the remainder, so it is refused from the bounded header. SCHEMA MOVED WITH IT to v42, which is the only time these two have moved together: a solo winner is the one victory fact this build cannot re-derive on restore. 8.37b carries a pending effect choice's declared VALUE VOCABULARY, so a joined seat draws six named colours or six named directions rather than the digits 0 through 5 - Tali's Commune with Spirits puts one of those decisions to a hero who is not the acting seat, so the seat that must answer it is by construction a remote one. A protocol 12 peer is refused before any payload is read. At 8.37b the schema was UNMOVED at v41: restore re-asks the handler, which re-declares it. " +
                "8.36b required authenticated keyed departures and rejected protocol 11. " +
                "Milestone 8.35z60's integration pass took it to 11: the seat projection now carries " +
                "rulebook p.8's shared Wave-counter pool as a plain Int32 beside the per-team Life " +
                "counters, so a remote seat's top strip reads the host's live count rather than the " +
                "frozen starting total of its own local composition. A version 10 peer would take " +
                "that integer as the hero-row count following it and mis-decode the rest of the " +
                "frame, so it is refused from the bounded header before any payload is interpreted. " +
                "Milestone 8.35z37 wired rulebook p.22's hero selection to the host with " +
                "ONE new client-to-host kind, ChooseHero, carrying a bounded hero id and no seat - " +
                "the connection says whose pick it is. A protocol nine peer has no case for kind 21 " +
                "and refuses it as an unexpected message, which is a protocol surface change and is " +
                "taken as one. Its downstream half moved no bytes: a chosen hero rides the roster " +
                "row that has carried a hero id since milestone 8.31, and the roster's two added " +
                "public facts - whether that hero was CHOSEN, and p.22's coin - are what a client " +
                "needs to draw the pick order and cannot infer. " +
                "The ELEVENTH amendment's host-selectable match formats gave the admission tuple a " +
                "format element, so the Hello payload carries one more string between the map " +
                "revision and the content digest: a protocol eight peer's reader takes that string " +
                "as the content digest and then finds the key proof missing, so its hello fails to " +
                "decode rather than mis-comparing - which is why the version moves rather than the " +
                "element being folded into the digest. " +
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

            for (var older = 1; older < PrivateSessionProtocol.Version; older++)
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
