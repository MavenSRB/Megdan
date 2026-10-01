using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35r. The event channel where it crosses the wire: the codec, the frame's size,
    /// what a returning seat is handed, what a superseded peer is told, and the who-is-awaited fact
    /// the deferred tray arms are built from.
    /// </summary>
    public sealed class PrivateOnlineEventChannelTests
    {
        /// <summary>
        /// Every kind in the vocabulary survives the wire unchanged, and the completeness is
        /// asserted against the enum rather than against a list somebody remembered to extend.
        /// </summary>
        [Test]
        public void EveryLogKind_RoundTripsThroughTheProjectionFrame()
        {
            var kinds = Enum.GetValues(typeof(MatchLogEntryKind))
                .Cast<MatchLogEntryKind>()
                .Where(kind => kind != MatchLogEntryKind.None)
                .ToArray();

            var specimens = kinds.Select(Specimen).ToArray();
            Assert.That(specimens.Select(entry => entry.Kind), Is.EquivalentTo(kinds),
                "A kind joined the vocabulary without a wire specimen. Every one must round-trip.");

            var message = new NetworkSeatProjectionMessage(
                new NetworkEnvelope("correlation", 1, 7, 0),
                new byte[] { 1, 2, 3 },
                Array.AsReadOnly(specimens.Select(NetworkMatchLogCodec.ToRow).ToArray()));

            Assert.That(
                NetworkMessageCodec.TryDecodeSeatProjection(PayloadOf(message), out var decoded),
                Is.True);

            var rebuilt = decoded.MatchLog.Select(NetworkMatchLogCodec.ToEntry).ToArray();
            Assert.That(rebuilt, Is.EqualTo(specimens),
                "A log line changed crossing the wire. The entry type's own equality is the check.");
            Assert.That(decoded.ProjectionPayload, Is.EqualTo(new byte[] { 1, 2, 3 }),
                "The seat's own projection payload must survive beside the shared log.");
        }

        /// <summary>
        /// Milestone 8.35z14-blockerid, protocol version 8: a <c>BlockedAction</c> row naming its
        /// blocker survives the binary wire format - the two fields appended after <c>amount</c> -
        /// through the full seat-projection frame, not merely through <see cref="NetworkMatchLogCodec"/>'s
        /// own bare row conversion.
        /// </summary>
        [Test]
        public void ABlockedActionRowNamingItsBlocker_SurvivesTheWireFrame()
        {
            var entry = new MatchLogEntry(
                1, MatchLogEntryKind.BlockedAction, roundIndex: 1, turnIndex: 1,
                subjectHeroId: new HeroId("hero-sabina"),
                subjectCardInstanceId: new CardInstanceId("card-back-to-back"),
                blockingHeroId: new HeroId("hero-wasp"),
                blockingCardInstanceId: new CardInstanceId("card-magnetic-dagger"));

            var message = new NetworkSeatProjectionMessage(
                new NetworkEnvelope("correlation", 1, 7, 0),
                new byte[] { 1 },
                Array.AsReadOnly(new[] { NetworkMatchLogCodec.ToRow(entry) }));

            Assert.That(
                NetworkMessageCodec.TryDecodeSeatProjection(PayloadOf(message), out var decoded),
                Is.True);

            var rebuilt = decoded.MatchLog.Select(NetworkMatchLogCodec.ToEntry).Single();
            Assert.That(rebuilt, Is.EqualTo(entry));
            Assert.That(rebuilt.BlockingHeroId, Is.EqualTo(new HeroId("hero-wasp")));
            Assert.That(rebuilt.BlockingCardInstanceId, Is.EqualTo(new CardInstanceId("card-magnetic-dagger")));
        }

        /// <summary>
        /// The ordinary case, beside it: a row with no blocker to name carries the same absence
        /// across the wire - never a value manufactured to fill the new fields.
        /// </summary>
        [Test]
        public void ABlockedActionRowNamingNoBlocker_SurvivesTheWireFrameAsAnAbsence()
        {
            var entry = new MatchLogEntry(
                1, MatchLogEntryKind.BlockedAction, roundIndex: 1, turnIndex: 1,
                subjectHeroId: new HeroId("hero-sabina"),
                subjectCardInstanceId: new CardInstanceId("card-back-to-back"));

            var message = new NetworkSeatProjectionMessage(
                new NetworkEnvelope("correlation", 1, 7, 0),
                new byte[] { 1 },
                Array.AsReadOnly(new[] { NetworkMatchLogCodec.ToRow(entry) }));

            Assert.That(
                NetworkMessageCodec.TryDecodeSeatProjection(PayloadOf(message), out var decoded),
                Is.True);

            var rebuilt = decoded.MatchLog.Select(NetworkMatchLogCodec.ToEntry).Single();
            Assert.That(rebuilt.BlockingHeroId.IsDefined, Is.False);
            Assert.That(rebuilt.BlockingCardInstanceId.IsDefined, Is.False);
            Assert.That(rebuilt, Is.EqualTo(entry));
        }

        /// <summary>
        /// A truncated or malformed log payload is refused rather than half-read, at every prefix
        /// length - the same truncation discipline milestone 8.32 applied to every other message.
        /// </summary>
        [Test]
        public void ATruncatedLogPayload_IsRefused_AtEveryPrefix()
        {
            var message = new NetworkSeatProjectionMessage(
                new NetworkEnvelope("correlation", 1, 1, 0),
                new byte[] { 9 },
                Array.AsReadOnly(new[]
                {
                    NetworkMatchLogCodec.ToRow(Specimen(MatchLogEntryKind.Move)),
                    NetworkMatchLogCodec.ToRow(Specimen(MatchLogEntryKind.Attack))
                }));

            var whole = PayloadOf(message);
            for (var length = 0; length < whole.Length; length++)
            {
                var prefix = new byte[length];
                Array.Copy(whole, prefix, length);
                Assert.That(NetworkMessageCodec.TryDecodeSeatProjection(prefix, out _), Is.False,
                    "A " + length + "-byte prefix of a projection-and-log payload decoded.");
            }

            Assert.That(NetworkMessageCodec.TryDecodeSeatProjection(whole, out _), Is.True);
        }

        /// <summary>
        /// The whole frame - one seat's projection plus the shared log - stays well inside the
        /// sealed bound on the seeded six-seat walk, and the margin is the milestone's evidence.
        /// </summary>
        /// <remarks>
        /// The guard is 80% of the bound rather than the bound itself, for the reason the
        /// idempotency window is sized from measured traffic: growth that eats the margin should
        /// fail here and force the retained window to be re-sized DELIBERATELY, rather than run the
        /// frame up against a hard limit in a live playtest. The seat record keeps its own separate
        /// half-bound guard in <c>PrivateOnlineSynchronizationTests</c>; this one is about the frame.
        /// </remarks>
        [Test]
        public void TheProjectionFrameCarryingTheLog_StaysInsideTheSealedBound()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();

            var largest = 0;
            var mostRows = 0;
            harness.DriveThroughWire(700, afterAcceptedStep: () =>
            {
                var seat = harness.MatchHost.Seats.Seats[0];
                var projection = harness.MatchHost.ProjectionForSeat(seat.Id);
                var rows = projection.MatchLog.Select(NetworkMatchLogCodec.ToRow).ToArray();
                mostRows = Math.Max(mostRows, rows.Length);
                largest = Math.Max(largest, NetworkMessageCodec.SeatProjectionFrame(
                    new NetworkSeatProjectionMessage(
                        new NetworkEnvelope("c", 1, 1, 0),
                        NetworkSeatProjectionCodec.Encode(projection),
                        Array.AsReadOnly(rows))).Length);
            }, seed: 1);

            var bound = PrivateSessionSecureChannel.MaxSealedPayloadBytes;
            Assert.That(mostRows, Is.GreaterThan(0), "The walk produced no log; nothing was measured.");
            Assert.That(largest, Is.LessThan(bound * 4 / 5),
                "The largest projection-and-log frame was " + largest + " bytes against a bound of " +
                bound + ", holding " + mostRows + " log lines. Re-size " +
                nameof(MatchLogProjection) + "." + nameof(MatchLogProjection.RetainedEntries) +
                " deliberately rather than letting the frame grow into its limit.");
            Assert.That(largest, Is.GreaterThan(4096),
                "A frame this small means the sweep measured the wrong thing.");
        }

        /// <summary>
        /// Milestone 8.35w. The same bound, measured over a match that runs to its own
        /// ENDING rather than over a bounded prefix of one.
        /// </summary>
        /// <remarks>
        /// <para><b>Why this case exists.</b> The guard above measures 700 wire steps, which is
        /// about eight completed rounds, and it is the reason a session-killing defect shipped past
        /// it. The completed-round history in a seat projection grew 1,183 bytes per completed
        /// round with no limit, and on this same seeded configuration the host threw out of
        /// <c>PrivateSessionFrameCodec</c> at <b>34 completed rounds</b> - inside the length
        /// ordinary matches reach, and terminal for the whole table when it lands, because the send
        /// sequence has already advanced and every resync is served down the same throwing path. A
        /// guard that stops at eight rounds cannot see a 34-round failure. That is the durable
        /// lesson, independent of the window that fixed it, and it is why this case is sized in
        /// ROUNDS and driven until the match ends itself.</para>
        /// <para><b>Why it drives the host rather than the wire.</b> The frame is a pure function of
        /// the seat projection and the log, and the case above already proves those same bytes
        /// reach a real client through real frame assemblers. Driving six client machines four
        /// thousand steps would buy nothing this does not measure and would not fit a test case's
        /// time budget; this one walks a whole match in about half a minute.</para>
        /// </remarks>
        [Test]
        public void TheProjectionFrame_StaysInsideItsBound_ThroughAMatchThatRunsToItsOwnEnding()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var seat = host.Seats.Seats[0];
            var envelope = new NetworkEnvelope("correlation", 1, 1, 0);
            var largest = 0;
            var largestAtRound = 0;

            void Measure()
            {
                var projection = host.ProjectionForSeat(seat.Id);
                var rows = projection.MatchLog.Select(NetworkMatchLogCodec.ToRow).ToArray();

                // Built through the RAW frame codec deliberately: an over-long frame fails here as
                // the codec's own ArgumentException rather than as a number this case compares.
                // The host itself no longer takes this path - its bounded compose trims the log or
                // refuses the one connection by name - so this guard is what keeps even the
                // graceful degradation from quietly becoming the norm.
                var frame = NetworkMessageCodec.SeatProjectionFrame(
                    new NetworkSeatProjectionMessage(
                        envelope,
                        NetworkSeatProjectionCodec.Encode(projection),
                        Array.AsReadOnly(rows)));
                if (frame.Length > largest)
                {
                    largest = frame.Length;
                    largestAtRound = projection.RoundIndex - 1;
                }
            }

            // Seed 5 is a walk whose match reaches its own ending past forty completed rounds -
            // the top of the length a human table plays, rather than a prefix.
            //
            // Re-pinned from seed 9 when this milestone was integrated onto 8.35u. A seeded walk is a
            // pure function of what it is OFFERED, so 8.35u's simultaneous level-up rerouted this
            // trajectory wholesale rather than shortening it: seed 9's match now ends at 31 completed
            // rounds where it ended at 43 before. Re-measured across sixteen seeds under the current
            // rules, ten still pass thirty-five completed rounds and four do not end at all inside a
            // 4,200-command budget, at fifty-three. The match length that made this defect reachable is
            // therefore unchanged; only this one walk moved, which is why the seed is re-pinned rather
            // than the round floor lowered - lowering it would quietly retire the property the case
            // exists to hold.
            //
            // Milestone 8.35z5 re-pinned it again, 2 -> 5, after closing Living Tsunami's
            // empty-Confirm defect: an offer this walk used to be able to take is gone, so seed 2 no
            // longer completes at all (54 rounds and still going at the 4,200-command ceiling, and
            // seeds 1, 4, 10, 15 and 30 all hit the same wall in a thirty-seed offline scan). Seed 5
            // still ends, at 46 completed rounds and 3,641 commands - six rounds of margin above the
            // floor this case asserts and 87% of budget, matching the shape every earlier re-pin here
            // records.
            //
            // Milestone 8.35z20 re-derived it again, 5 -> 1: correcting Blend Into Shadows'
            // secondary rerouted this trajectory and seed 5 now ends at only 17 rounds. A fresh
            // offline scan of seeds 1-30 found seed 1 ends at 47 completed rounds and 3,753
            // commands - seven rounds of margin and 89% of budget, the same shape every earlier
            // re-pin here has recorded.
            //
            // Milestone 8.35z25 re-derived it again, 1 -> 48. Combat Reflexes and Leaping
            // Strike now ask their printed move before their targeting, so this walk reroutes and
            // seed 1 ends at only 30 completed rounds - below the floor this case exists to hold. An
            // offline scan of seeds 1-60 found seed 48 ends at 45 completed rounds and 3,590
            // commands: five rounds of margin above the floor and 85% of budget, which is the same
            // shape as every re-pin above and slightly more command headroom than the last one. The
            // seed moves rather than the floor, for the reason recorded in 8.35u's entry.
            //
            // Milestone 8.35z42 re-derived it again, 48 -> 1, and this one is the
            // mirror image of 8.35z25's. That milestone made Combat Reflexes and Leaping Strike
            // OFFERABLE with nothing inside move-and-reach - a position their text cannot act on -
            // and rerouted this walk onto seed 48. This one proves those positions empty and takes
            // the offer away again, so the walk reroutes back and seed 48 now ends at 37 completed
            // rounds, below the floor. An offline scan of seeds 1-60 under the current rules found
            // fourteen that end past forty; seed 1 ends at 47 completed rounds and 3,753 commands -
            // seven rounds of margin and 89% of budget, the same shape every re-pin above records.
            //
            // AND IT IS THE SAME PAIR OF NUMBERS 8.35z20 PINNED FOR SEED 1, exactly: 47 rounds,
            // 3,753 commands. Seed 1 was rerouted to 30 rounds by 8.35z25's offer widening and
            // returns to its earlier trajectory now that the widening is undone in precisely the
            // positions that were wrong. That is not a coincidence worth ignoring - it is the
            // corroboration that this milestone removes offers 8.35z25 added and touches no others,
            // measured on a walk nobody designed to prove it.
            //
            // Milestone 8.46b re-derived it again, 1 -> 2. A choice admitting exactly one selection
            // is now answered in ONE accepted command instead of two, so this walk draws a different
            // command at every pause past the first such answer. Seed 1 no longer ends at all: at
            // step 3,827 and 49 rounds it draws an `AnswerEffectChoice` the authority refuses
            // (AttackActionRejected: TargetUnavailable), which stops the drive on its own
            // acceptance assertion rather than on anything this case is about. An offline scan of
            // seeds 1-60 found four that end that way (1, 10, 44, 47) - recorded as an open question
            // rather than seeded around silently - and forty-eight that end honestly. Seed 2 ends at
            // 45 completed rounds and 3,538 commands: five rounds of margin above the floor and 84%
            // of budget, the same shape every re-pin above records.
            //
            // Milestone 9.6 re-derived it again, 2 -> 36. The offer walk now reads what each action
            // can do, so this walk reroutes and seed 2 ends early. The same scan closed the open
            // question above: seeds 5, 31 and 40 stopped on an offered Living Tsunami step the rules
            // refused, and that step is now taken (RULING 45's dead end is p.10's stop) - no seed of
            // 1-40 stops on a refusal any more. Seed 36 ends at 43 completed rounds and 3,726
            // commands - three rounds of margin and 88% of budget, the shape every re-pin above
            // records.
            var submitted = OwnerScopedProjectionTests.DriveThroughSeats(
                host, 4200, projection => { Measure(); return false; }, seed: 36);
            Measure();

            var playedRounds = host.Projection.RoundIndex - 1;
            Assert.That(host.Projection.Outcome.HasEnded, Is.True,
                "The walk did not reach an ending in " + submitted + " commands, so this case " +
                "measured a prefix again - which is exactly what it exists to stop doing.");
            Assert.That(playedRounds, Is.GreaterThanOrEqualTo(40),
                "The match ended after only " + playedRounds + " rounds; a frame bound sized " +
                "against a short match is the defect this case is about.");

            var bound = PrivateSessionSecureChannel.MaxSealedPayloadBytes;
            Assert.That(largest, Is.LessThan(bound * 4 / 5),
                "The largest projection-and-log frame over a complete " + playedRounds +
                "-round match was " + largest + " bytes (at round " + largestAtRound +
                ") against a bound of " + bound + ". Re-size " +
                nameof(MatchSessionProjection) + "." +
                nameof(MatchSessionProjection.RetainedCompletedRounds) + " or " +
                nameof(MatchLogProjection) + "." + nameof(MatchLogProjection.RetainedEntries) +
                " deliberately rather than letting the frame grow into its limit.");
            Assert.That(largest, Is.GreaterThan(4096),
                "A frame this small means the walk measured the wrong thing.");
        }

        /// <summary>
        /// The frame-bound corrective's SEND half, driven at the host's own compose decision: a
        /// projection-and-log message past the bound - the state that crashed the live solo
        /// rehearsal host out of <see cref="PrivateSessionFrameCodec"/> and stalled the whole
        /// table in silence - is now sent successfully with the OLDEST log rows dropped. Never a
        /// throw, never silence.
        /// </summary>
        /// <remarks>
        /// The retained windows make this state unreachable through real match state, which is
        /// exactly why this case builds the message directly: the degradation path exists for the
        /// NEXT growth term nobody has met yet, and a guard that waited for real state to reach it
        /// again would wait forever or arrive with the playtest frozen. The raw encoder's throw is
        /// asserted beside the compose so the crash shape this corrects stays pinned.
        /// </remarks>
        [Test]
        public void AMessagePastTheFrameBound_IsTrimmedToTheNewestLogRows_NeverThrown()
        {
            var rows = Enumerable.Range(1, MatchLogProjection.RetainedEntries)
                .Select(sequence => NetworkMatchLogCodec.ToRow(Line(sequence)))
                .ToArray();
            var message = new NetworkSeatProjectionMessage(
                new NetworkEnvelope("correlation", 1, 1, 0),
                new byte[60000],
                Array.AsReadOnly(rows));

            // The crash the corrective answers, pinned: the raw frame encoder still refuses an
            // over-bound payload - that bound is the anti-allocation cap on what any peer may
            // declare, and it does not move - so the graceful path must decide BEFORE it.
            Assert.That(
                Assert.Throws<ArgumentException>(
                    () => NetworkMessageCodec.SeatProjectionFrame(message)).Message,
                Does.StartWith("A payload is bounded."));

            Assert.That(
                PrivateSessionHost.TryComposeProjectionFrame(message, out var frame, out var dropped),
                Is.True,
                "The host's compose refused a message a trimmed log would fit.");
            Assert.That(dropped, Is.GreaterThan(0).And.LessThan(rows.Length),
                "Trimming dropped everything or nothing; either way nothing here was proved.");
            Assert.That(frame.Length - PrivateSessionProtocol.FrameHeaderBytes,
                Is.LessThanOrEqualTo(PrivateSessionSecureChannel.MaxSealedPayloadBytes),
                "The composed frame does not fit the sealed bound it exists to fit.");

            var assembler = new PrivateSessionFrameAssembler();
            Assert.That(assembler.Add(frame, 0, frame.Length), Is.True);
            Assert.That(assembler.TryDequeue(out var assembled), Is.True);
            Assert.That(
                NetworkMessageCodec.TryDecodeSeatProjection(assembled.Payload, out var decoded),
                Is.True, "A trimmed frame did not decode under the ordinary read discipline.");
            Assert.That(decoded.ProjectionPayload, Is.EqualTo(message.ProjectionPayload),
                "The projection payload is authoritative state and must cross untouched.");
            Assert.That(decoded.MatchLog.Select(row => row.Sequence),
                Is.EqualTo(Enumerable.Range(1 + dropped, rows.Length - dropped)),
                "The rows that survive must be the NEWEST suffix, in order - the retained " +
                "window's own idea of which lines matter.");
        }

        /// <summary>
        /// The other arm of the same corrective: a projection payload ALONE past the compose
        /// budget - which no log trimming can mend - fails composition by returning false, never
        /// by throwing. The host answers that with a named <see
        /// cref="SessionRejectionReason.OversizedFrame"/> refusal for that one connection, which
        /// is the loud, one-seat, recoverable failure the silent whole-table stall is corrected to.
        /// </summary>
        [Test]
        public void AProjectionPayloadNoFrameCanCarry_FailsComposition_RatherThanThrows()
        {
            // Sized to fit the plaintext frame bound and NOT the sealed budget: the sharper case,
            // because a compose pinned to the loose bound would pass here and then throw inside
            // Seal on every keyed session - the same crash one layer later.
            var sealedOnlyOverflow = new NetworkSeatProjectionMessage(
                new NetworkEnvelope("correlation", 1, 1, 0),
                new byte[PrivateSessionSecureChannel.MaxSealedPayloadBytes - 10],
                Array.Empty<NetworkMatchLogRow>());
            Assert.That(
                PrivateSessionHost.TryComposeProjectionFrame(sealedOnlyOverflow, out _, out _),
                Is.False,
                "A payload only the PLAINTEXT bound could carry was composed; the budget must be " +
                "the sealed bound, the tighter of the two every frame must eventually pass.");

            var farPastEveryBound = new NetworkSeatProjectionMessage(
                new NetworkEnvelope("correlation", 1, 1, 0),
                new byte[PrivateSessionProtocol.MaxPayloadBytes + 1],
                Array.Empty<NetworkMatchLogRow>());
            Assert.That(
                PrivateSessionHost.TryComposeProjectionFrame(farPastEveryBound, out var frame, out _),
                Is.False);
            Assert.That(frame, Is.Null);
        }

        /// <summary>
        /// A message already inside the bound crosses the bounded compose byte-for-byte as the
        /// plain encoder writes it, with nothing dropped - so the byte-level convergence
        /// assertions the wire suites hold are still looking at ONE encoder, not two.
        /// </summary>
        [Test]
        public void AFrameInsideTheBound_IsByteIdentical_ThroughTheBoundedCompose()
        {
            var rows = Enumerable.Range(1, 40)
                .Select(sequence => NetworkMatchLogCodec.ToRow(Line(sequence)))
                .ToArray();
            var message = new NetworkSeatProjectionMessage(
                new NetworkEnvelope("correlation", 3, 21, 9),
                new byte[] { 1, 2, 3, 4, 5 },
                Array.AsReadOnly(rows));

            Assert.That(
                PrivateSessionHost.TryComposeProjectionFrame(message, out var frame, out var dropped),
                Is.True);
            Assert.That(dropped, Is.Zero, "A fitting message lost log rows.");
            Assert.That(frame, Is.EqualTo(NetworkMessageCodec.SeatProjectionFrame(message)),
                "The bounded compose and the plain encoder disagree about a fitting message.");
        }

        /// <summary>
        /// Milestone 8.35w. The window itself, stated as the two halves that make it
        /// safe: what a seat is shown is bounded, and what the match KEEPS is not.
        /// </summary>
        /// <remarks>
        /// Without the second half this would be a fixture that passes while history is being
        /// destroyed. The authority owns the completed-round history for the rules, the restore
        /// state and the authoritative snapshot; the read model carries a recent window of it
        /// because that is what one session frame has to fit. This case asserts both at once, on a
        /// match that has outrun the window, and it asserts the window is the LAST rounds rather
        /// than merely the right NUMBER of them - a first-N window would decode, converge and be
        /// silently useless.
        /// </remarks>
        [Test]
        public void TheReadModelsRoundHistory_IsTheRecentWindow_WhileTheMatchKeepsEveryRound()
        {
            var initialState = OwnerScopedProjectionTests.CreateOfficialInitialState();
            var host = MatchHost.Compose(initialState);
            OwnerScopedProjectionTests.DriveThroughSeats(host, 1400, seed: 9);

            var playedRounds = host.Projection.RoundIndex - 1;
            Assert.That(playedRounds,
                Is.GreaterThan(MatchSessionProjection.RetainedCompletedRounds),
                "The walk completed " + playedRounds + " rounds, which does not outrun the " +
                "window, so nothing here would be proved.");

            var expected = Enumerable.Range(
                playedRounds - MatchSessionProjection.RetainedCompletedRounds + 1,
                MatchSessionProjection.RetainedCompletedRounds).ToArray();
            Assert.That(host.Projection.CompletedRounds.Select(round => round.RoundIndex),
                Is.EqualTo(expected),
                "The read model's retained rounds are the most recent ones, in order.");
            foreach (var seat in host.Seats.Seats)
            {
                Assert.That(
                    host.ProjectionForSeat(seat.Id).CompletedRounds.Select(round => round.RoundIndex),
                    Is.EqualTo(expected),
                    "Seat " + seat.Id + " holds a different window than the host it derives from.");
            }

            // Rebound against the same content the match was composed with, which is exactly what
            // `MatchAggregate.Restore` passes - a restore that guessed would fail on the minions
            // before it ever reached the round history.
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                host.Projection.AuthoritativeSnapshot,
                initialState.Scenario.CardDefinitions,
                initialState.AvailableMaps,
                initialState.Setup.Minions);
            Assert.That(restored.Round.CompletedRounds, Has.Count.EqualTo(playedRounds),
                "The authoritative snapshot restored " + restored.Round.CompletedRounds.Count +
                " completed rounds of a " + playedRounds + "-round match. The read model's window " +
                "is a bound on what a seat is SHOWN; the match must still hold all of it.");
            Assert.That(restored.Round.CompletedRounds.Select(round => round.RoundIndex),
                Is.EqualTo(Enumerable.Range(1, playedRounds)));
        }

        /// <summary>
        /// The recorded reconnect-history semantics, driven: a seat that leaves and returns is
        /// handed exactly the log every other seat is holding - no more, and nothing older.
        /// </summary>
        [Test]
        public void AReturningSeat_ReceivesTheSameLog_AsOneThatNeverLeft()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(120, seed: 1);

            var staying = harness.Client(2);
            var before = harness.Client(1).MatchLog.ToArray();
            Assert.That(before, Is.Not.Empty, "The walk produced no log to compare.");

            harness.DisconnectClient(1);
            harness.Pump();
            var returned = harness.ReconnectClient(1);
            harness.Pump();

            var back = harness.Client(returned);
            Assert.That(back.MatchLog, Is.EqualTo(staying.MatchLog),
                "A returning seat and a seat that never left hold different logs.");
            Assert.That(back.MatchLog, Is.EqualTo(harness.MatchHost.Projection.MatchLog),
                "The returned seat's log is not the authority's own.");
            Assert.That(back.MatchLog.Count,
                Is.LessThanOrEqualTo(MatchLogProjection.RetainedEntries));
        }

        /// <summary>
        /// Milestone 8.35w. The same reconnect fact for the round history, on a match
        /// that has already outrun the window: a seat that leaves and comes back is served the
        /// retained window over the wire and converges on it, byte for byte.
        /// </summary>
        /// <remarks>
        /// This is the half the bound could have broken without any fixture noticing. A resync is
        /// not a different, larger message - <c>HandleResyncRequest</c> serves it through the same
        /// <c>SendProjectionTo</c> as an ordinary push - which is precisely why an over-long frame
        /// could not self-heal before, and is why the windowed frame has to be proved on the
        /// RETURNING seat and not only on the seats that stayed.
        /// </remarks>
        [Test]
        public void AReturningSeat_IsServedTheRetainedRoundWindow_OverTheWire()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(600, seed: 1);

            var playedRounds = harness.MatchHost.Projection.RoundIndex - 1;
            Assert.That(playedRounds,
                Is.GreaterThan(MatchSessionProjection.RetainedCompletedRounds),
                "The wire walk completed " + playedRounds + " rounds, which does not outrun the " +
                "window, so a returning seat would be served the whole history either way.");

            var staying = harness.Client(2);
            harness.DisconnectClient(1);
            harness.Pump();
            var returned = harness.ReconnectClient(1);
            harness.Pump();

            var back = harness.Client(returned);
            var expected = Enumerable.Range(
                playedRounds - MatchSessionProjection.RetainedCompletedRounds + 1,
                MatchSessionProjection.RetainedCompletedRounds).ToArray();

            Assert.That(back.LatestProjection.Detail.CompletedRounds.Select(row => row.RoundIndex),
                Is.EqualTo(expected),
                "A returning seat was served a different round history than the window.");
            Assert.That(back.LatestProjection.Detail.CompletedRounds.Select(row => row.RoundIndex),
                Is.EqualTo(staying.LatestProjection.Detail.CompletedRounds
                    .Select(row => row.RoundIndex)),
                "A returning seat and a seat that never left hold different round histories.");

            // The byte-for-byte half: the resync payload IS the host's own encoding for that seat.
            harness.AssertConverged();
        }

        /// <summary>
        /// The log converges under the ordinary wire discipline: after every accepted submission
        /// every seated client holds the authority's own log, including across a dropped push.
        /// </summary>
        [Test]
        public void EveryClientsLog_ConvergesOnTheAuthoritys_AcrossTheWireWalk()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();

            harness.DriveThroughWire(200, afterAcceptedStep: () =>
            {
                var authoritative = harness.MatchHost.Projection.MatchLog;
                for (var ordinal = 1; ordinal <= 6; ordinal++)
                {
                    Assert.That(harness.Client(ordinal).MatchLog, Is.EqualTo(authoritative),
                        "Client " + ordinal + " diverged from the authority's log.");
                }
            }, seed: 1);
        }

        /// <summary>
        /// A peer speaking the superseded protocol version is refused from the bounded header,
        /// before any payload is interpreted and before any log or projection byte reaches it.
        /// </summary>
        [Test]
        public void AVersionFivePeer_IsRefused_BeforeAnyPayloadInterpretation()
        {
            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(30),
                "9.19 carries the CARD-TEXT DISCARD ROW: the seat-public match log gained " +
                "CardDiscardedByCardText, the row for a card that card text sent out of a hand, so " +
                "a /28 peer refuses the frame rather than going dark on a kind it cannot name. " +
                "8.80 carries the ATTACKING CARD ON THE DEFENSE QUESTION: the pending Defense "
                + "row gained the attacking card's instance id, its definition id and that "
                + "definition's content revision, and the action definition id, all written "
                + "between the attacker id and the attack total - so a /20 peer reads the first "
                + "of those strings where it expects the total's four bytes and mis-decodes the "
                + "rest of the detail record. It rides the CHOICE rather than the Attack FRAME "
                + "because SeatProjection hands that frame to the ACTING seat alone, and the seat "
                + "being asked to defend is by construction not that seat - which is why the "
                + "attacker's card had been missing from the Defense window on every network "
                + "seat, the host's own loopback seat included, since 8.58 drew it. The three "
                + "facts are table-public: the card stands face up in p.4's turn area, already in "
                + "every seat's own card list. SCHEMA IS UNMOVED at v44 - the choice is derived "
                + "from the authoritative Attack state at projection time and never persisted. " +
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
                "This case pins the CURRENT version rather than the one the event channel moved to, " +
                "so a later move is accounted for here rather than passing unread. Milestone " +
                "8.35z60's integration pass took it to 11 for the shared Wave-counter pool the seat " +
                "projection now carries. Milestone 8.35u " +
                "took it to 7 for the repeated awaited-hero group p.15's simultaneous leveling made " +
                "necessary, milestone 8.35z14-blockerid to 8 for the match log's own blocker-" +
                "identity fields, the ELEVENTH amendment's match formats to 9 for the format " +
                "element the admission tuple's Hello now carries, and milestone 8.35z37 to " +
                "10 for the ChooseHero kind a seat's hero pick travels on; a version 5 peer is " +
                "still refused, and so is a version 6, 7, 8 or 9 one." +
                "Milestone 8.71a carries the REWRITTEN PRINTED INITIATIVE at /20: each "
                + "seat projection's card sections end in a sparse block naming the cards "
                + "Hanu's Hurry up! has set to 11, which a /19 peer would read as whatever "
                + "field follows the last card row. SCHEMA MOVES WITH IT to v44, because "
                + "the rewrite is authoritative state no live state remembers.");

            var harness = PrivateOnlineWireHarness.Compose();
            var seat = harness.Session.Roster[0].Seat.Value;
            var superseded = harness.Connect(1, seat, WithVersion(harness.Identity, 5));
            harness.Pump();

            Assert.That(superseded.State, Is.EqualTo(PrivateSessionClientState.Rejected));
            Assert.That(superseded.RejectionReason,
                Is.EqualTo(SessionRejectionReason.ProtocolVersionMismatch));
            Assert.That(superseded.LatestProjection, Is.Null,
                "A projection byte reached a version 5 peer.");
            Assert.That(superseded.MatchLog, Is.Empty,
                "A match-log line reached a version 5 peer.");

            // And the session is undamaged: a current peer seats normally afterwards.
            var current = harness.Connect(2, seat, harness.Identity);
            harness.Pump();
            Assert.That(current.State, Is.EqualTo(PrivateSessionClientState.Seated));
        }

        /// <summary>
        /// WHO IS AWAITED, for every arm the SEVENTH amendment's tray work had to defer - at the
        /// Director's scope and at every seat's, including the seats that do NOT own the decision.
        /// </summary>
        /// <remarks>
        /// <para>This is the whole point of publishing the owner. Each of these families is scoped
        /// away from a non-owning seat - its pending record reads <c>null</c> there - so before this
        /// milestone a spectator's client could not tell that hero X was levelling, defending or
        /// choosing a respawn, and the tray could not light. The assertion below is deliberately
        /// made from the NON-owner's projection, because the owner's was never the hard case.</para>
        /// <para>The privacy line is asserted beside it: the non-owner learns WHO is awaited and
        /// never WHAT they may choose - the contract stays <c>null</c> for them.</para>
        /// </remarks>
        [Test]
        public void WhoIsAwaited_IsPublicToEverySeat_ForEveryDeferredArm()
        {
            var arms = new (string name, MatchSession session)[]
            {
                ("levelling hero", ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(out _)),
                ("defence answerer", ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse()),
                ("respawn chooser", HeroRespawnTests.CreatePendingRespawnSession())
            };

            foreach (var (name, session) in arms)
            {
                var host = new MatchHost(session);
                var director = host.Projection.DecisionOwner;
                Assert.That(director.IsDefined, Is.True,
                    "The Director cannot say who is awaited for the " + name + " arm.");

                var owners = 0;
                foreach (var seat in host.Seats.Seats)
                {
                    var projection = host.ProjectionForSeat(seat.Id);
                    Assert.That(projection.DecisionOwner, Is.EqualTo(director),
                        "Seat " + seat.Id + " reads a different awaited owner from the Director's " +
                        "for the " + name + " arm. Who is awaited is table-public.");
                    Assert.That(projection.DecisionOwner.FamilyId, Is.EqualTo(director.FamilyId));

                    if (projection.DecisionOwner.IsOwnedBy(seat))
                    {
                        owners++;
                        Assert.That(projection.DecisionContract, Is.Not.Null,
                            "The owning seat of the " + name + " arm was given no answers.");
                    }
                    else
                    {
                        Assert.That(projection.DecisionContract, Is.Null,
                            "A seat that does not own the " + name + " arm was handed its options. " +
                            "Who is awaited is public; what they may choose is not.");
                    }
                }

                Assert.That(owners, Is.GreaterThan(0),
                    "No seat owns the " + name + " arm, so nobody could answer it.");
            }
        }

        /// <summary>
        /// The acting-hero arm's correctness: while a defender is being asked, the attacker is
        /// still the selected actor - so a spectator needs to know THAT a decision stands on top of
        /// that turn and WHOSE it is, which is exactly what the owner now says.
        /// </summary>
        [Test]
        public void ADecisionStandingOnTheActorsTurn_IsVisibleAsSuch_ToEverySeat()
        {
            var host = new MatchHost(ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse());
            var actor = host.Projection.SelectedActor;
            var owner = host.Projection.DecisionOwner;

            Assume.That(actor.IsDefined, Is.True, "This staging has no selected actor.");
            Assert.That(owner.IsDefined, Is.True);
            Assert.That(owner.HeroId, Is.Not.EqualTo(actor.ActorId),
                "This staging is meant to be the case where the AWAITED hero is not the acting one; " +
                "without that the arm below proves nothing.");

            foreach (var seat in host.Seats.Seats)
            {
                var projection = host.ProjectionForSeat(seat.Id);

                // Both public facts reach every seat, which is what lets a spectator draw "BROGAN's
                // turn, waiting on ARIEN" instead of leaving the attacker's tray shining.
                Assert.That(projection.SelectedActor.ActorId, Is.EqualTo(actor.ActorId));
                Assert.That(projection.DecisionOwner.IsDefined, Is.True,
                    "Seat " + seat.Id + " cannot tell that a decision stands on the actor's turn.");
                Assert.That(projection.DecisionOwner.HeroId, Is.EqualTo(owner.HeroId));
            }
        }

        /// <summary>
        /// The log adds new material to a frame every seat receives, so the byte-absence sweep runs
        /// over it too: no line names a card whose identity has never been public.
        /// </summary>
        /// <remarks>
        /// <para><b>The predicate is "never revealed", not "hidden right now", and the difference is
        /// a real one this case was written the wrong way round first.</b> A card revealed at the
        /// joint reveal is public to the whole table by rulebook p.9, and end-of-round retrieval
        /// then puts it back in its owner's hand - so a correct log legitimately names cards that
        /// are, at the moment of the sweep, sitting in somebody's hand. Asserting over currently
        /// hidden cards therefore fails on correct behaviour: the first run of this case flagged
        /// Arien's Spell Break, which she had revealed and played rounds earlier.</para>
        /// <para>What would be a leak is the log naming a card the table has NEVER seen - one dealt
        /// and never committed, or committed and not yet revealed. That is what this sweeps for, and
        /// there is a great deal of it: most of every hand.</para>
        /// </remarks>
        [Test]
        public void TheLogBytes_NameNoCardTheTableHasNeverSeen()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(150, seed: 1);

            // Every card identity the recorded history ever made public, from the reveal events
            // themselves rather than from any claim about which those are.
            var everRevealed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in harness.MatchHost.Projection.DiagnosticEvents)
            {
                foreach (var field in entry.StableReferences.Split(';'))
                {
                    if (field.StartsWith("card=", StringComparison.Ordinal) &&
                        entry.EventType == "Commitment.CardRevealed")
                    {
                        everRevealed.Add(field.Substring("card=".Length));
                    }
                }
            }

            var neverSeen = new List<string>();
            foreach (var seat in harness.MatchHost.Seats.Seats)
            {
                neverSeen.AddRange(harness.MatchHost.ProjectionForSeat(seat.Id).OwnCards
                    .Where(card => card.State.Location == CardLocation.Hand ||
                        card.State.Location == CardLocation.Deck)
                    .Select(card => card.Id.Value)
                    .Where(id => !everRevealed.Contains(id)));
            }

            Assert.That(neverSeen, Is.Not.Empty,
                "Every hero holds cards the table has never seen; the sweep needs some.");

            var rows = harness.MatchHost.Projection.MatchLog
                .Select(NetworkMatchLogCodec.ToRow)
                .ToArray();
            var text = Encoding.UTF8.GetString(NetworkMessageCodec.SeatProjectionFrame(
                new NetworkSeatProjectionMessage(
                    new NetworkEnvelope("c", 1, 1, 0),
                    Array.Empty<byte>(),
                    Array.AsReadOnly(rows))));

            foreach (var id in neverSeen.Distinct())
            {
                Assert.That(text, Does.Not.Contain(id),
                    "The encoded match log names '" + id + "', which no public event ever revealed.");
            }
        }

        private static PrivateSessionIdentity WithVersion(PrivateSessionIdentity identity, int version) =>
            new PrivateSessionIdentity(
                identity.ProtocolFamily,
                version,
                identity.BuildIdentity,
                identity.SnapshotSchemaVersion,
                identity.MapId,
                identity.MapContentRevision,
                identity.MatchFormatId,
                identity.ContentDigest);

        /// <summary>The message's payload, past the frame header the assembler would strip.</summary>
        private static byte[] PayloadOf(NetworkSeatProjectionMessage message)
        {
            var frame = NetworkMessageCodec.SeatProjectionFrame(message);
            var assembler = new PrivateSessionFrameAssembler();
            Assert.That(assembler.Add(frame, 0, frame.Length), Is.True);
            Assert.That(assembler.TryDequeue(out var assembled), Is.True);
            Assert.That(assembled.Kind, Is.EqualTo(SessionMessageKind.SeatProjection));
            return assembled.Payload;
        }

        /// <summary>
        /// One log line at one sequence, sized like a real one: ids of the shape and length the
        /// official configuration actually writes, so a bound driven with these is driven with
        /// honest bytes rather than with a synthetic balloon.
        /// </summary>
        private static MatchLogEntry Line(int sequence) =>
            new MatchLogEntry(
                sequence,
                MatchLogEntryKind.Attack,
                roundIndex: 1 + sequence / 8,
                turnIndex: 1 + sequence % 4,
                subjectHeroId: new HeroId("hero-tigerclaw"),
                targetHeroId: new HeroId("hero-xargatha"),
                subjectMinionId: default,
                subjectCardInstanceId: new CardInstanceId("red-01-wasp-magnetic-dagger"),
                subjectTeamId: new TeamId("team-red"),
                origin: new HexCoordinate(sequence % 5, -(sequence % 5)),
                destination: new HexCoordinate(-(sequence % 3), sequence % 3),
                hasOrigin: true,
                hasDestination: true,
                outcome: MatchLogOutcome.AttackSuccessful,
                amount: sequence);

        /// <summary>One specimen per kind, populated with whatever that kind's row may name.</summary>
        private static MatchLogEntry Specimen(MatchLogEntryKind kind)
        {
            var ordinal = (int)kind;
            return new MatchLogEntry(
                ordinal,
                kind,
                roundIndex: ordinal,
                turnIndex: ordinal + 1,
                subjectHeroId: new HeroId("hero-" + ordinal),
                targetHeroId: new HeroId("target-" + ordinal),
                subjectMinionId: new MinionId("minion-" + ordinal),
                subjectCardInstanceId: new CardInstanceId("card-" + ordinal),
                subjectTeamId: new TeamId("team-" + ordinal),
                origin: new HexCoordinate(ordinal, -ordinal),
                destination: new HexCoordinate(-ordinal, ordinal),
                hasOrigin: true,
                hasDestination: true,
                outcome: MatchLogOutcome.AttackSuccessful,
                amount: ordinal * 3);
        }
    }
}
