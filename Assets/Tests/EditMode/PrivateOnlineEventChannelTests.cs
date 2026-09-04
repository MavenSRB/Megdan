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
            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(6),
                "This case pins the version the event channel moved to.");

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
