using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.32. The wire codecs: every command kind round-trips through its bounded DTO,
    /// every command a live seat surface offers round-trips equal, the seat projection round-trips
    /// through its wire record, the sealed channel refuses tampering and replay, and malformed
    /// input of every shape is refused rather than read.
    /// </summary>
    public sealed class NetworkSubmissionCodecTests
    {
        /// <summary>
        /// One hand-built specimen per command kind, so total coverage of the vocabulary is a fact
        /// this fixture states rather than a hope about what a walk happened to reach. A kind with
        /// no specimen fails the completeness assertion below.
        /// </summary>
        private static readonly IReadOnlyList<DirectorCommand> Specimens = new[]
        {
            DirectorCommand.CommitCard(new HeroId("hero-wasp"), new CardInstanceId("wasp-card-01")),
            DirectorCommand.Pass(new HeroId("hero-wasp")),
            DirectorCommand.ChooseTiedActor(TeamSide.Red, new CardInstanceId("wasp-card-01")),
            DirectorCommand.PerformHold(new CardInstanceId("wasp-card-01")),
            DirectorCommand.StartAction(
                new CardInstanceId("wasp-card-01"), new ActionDefinitionId("primary-attack")),
            DirectorCommand.ChooseMovementDestination(
                new ActionResolutionId("resolution-1"),
                new ActionResolutionStepId("step-1"),
                new HexCoordinate(-2, 3)),
            DirectorCommand.ChooseMovementDestination(new HexCoordinate(0, 0)),
            DirectorCommand.StopMovement(
                new ActionResolutionId("resolution-1"), new ActionResolutionStepId("step-1")),
            DirectorCommand.ChooseAttackTarget(
                new ActionResolutionId("resolution-1"),
                new ActionResolutionStepId("step-2"),
                new HeroId("hero-arien")),
            DirectorCommand.ChooseAttackTarget(new MinionId("red-melee-01")),
            DirectorCommand.ChooseDefenseCard(
                new ActionResolutionId("resolution-1"),
                new ActionResolutionStepId("step-3"),
                new CardInstanceId("arien-card-02")),
            DirectorCommand.DeclineDefense(
                new ActionResolutionId("resolution-1"), new ActionResolutionStepId("step-3")),
            DirectorCommand.AdvanceEndOfRound(),
            // Milestone 8.35g. Payload-free like the advance above it: the only turn a release
            // can release is the one standing in the pause, so there is nothing to carry.
            DirectorCommand.ReleaseTurn(),
            DirectorCommand.PerformFastTravel(
                new ActionDefinitionId("secondary-movement"), new HexCoordinate(4, -1)),
            DirectorCommand.SelectMinionBattleRemoval(new MinionBattleChoiceCommand(
                MinionBattleChoiceCommandKind.Select,
                new EndOfRoundResolutionId("end-1"),
                new EndOfRoundChoiceId("choice-1"),
                TeamSide.Blue,
                RuleObject.ForMinion(new MinionId("blue-ranged-02")))),
            DirectorCommand.DeselectMinionBattleRemoval(new MinionBattleChoiceCommand(
                MinionBattleChoiceCommandKind.Deselect,
                new EndOfRoundResolutionId("end-1"),
                new EndOfRoundChoiceId("choice-1"),
                TeamSide.Blue,
                RuleObject.ForHero(new HeroId("hero-brogan")))),
            DirectorCommand.ConfirmMinionBattleRemovals(new MinionBattleChoiceCommand(
                MinionBattleChoiceCommandKind.Confirm,
                new EndOfRoundResolutionId("end-1"),
                new EndOfRoundChoiceId("choice-1"),
                TeamSide.Blue)),
            DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                new EffectResolutionId("effect-1"),
                new EffectChoiceId("effect-choice-1"),
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Option(new EffectNodeId("node-a")),
                new HeroId("hero-sabina"))),
            DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                new EffectResolutionId("effect-1"),
                new EffectChoiceId("effect-choice-2"),
                EffectChoiceCommandKind.Deselect,
                EffectChoiceCandidate.Value_(EffectValue.Space(new HexCoordinate(1, -4))))),
            DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                new EffectResolutionId("effect-1"),
                new EffectChoiceId("effect-choice-3"),
                EffectChoiceCommandKind.Confirm)),
            DirectorCommand.PerformClear(
                new ActionDefinitionId("primary-attack"),
                new[] { new TokenInstanceId("token-1"), new TokenInstanceId("token-2") }),
            DirectorCommand.ChooseCorrectionSubject(new DisplacedMinionCorrectionCommand(
                DisplacedMinionCorrectionCommandKind.SelectSubject,
                new ActionResolutionId("resolution-9"),
                new ActionResolutionStepId("correction-1"),
                TeamSide.Red,
                new MinionId("red-heavy-01"),
                default)),
            DirectorCommand.ChooseCorrectionDestination(new DisplacedMinionCorrectionCommand(
                DisplacedMinionCorrectionCommandKind.SelectDestination,
                new ActionResolutionId("resolution-9"),
                new ActionResolutionStepId("correction-2"),
                TeamSide.Red,
                new MinionId("red-heavy-01"),
                new HexCoordinate(2, 2))),
            DirectorCommand.RespawnHero(new HeroRespawnCommand(
                HeroRespawnCommandKind.Respawn,
                new HeroId("hero-tigerclaw"),
                new CardInstanceId("tigerclaw-card-05"),
                new HexCoordinate(-3, 0))),
            DirectorCommand.ResolveCardWithoutRespawn(new HeroRespawnCommand(
                HeroRespawnCommandKind.ResolveWithoutAction,
                new HeroId("hero-tigerclaw"),
                new CardInstanceId("tigerclaw-card-05"))),
            DirectorCommand.BeginSystemTransition(new SystemTransitionDefinitionId("transition-1")),
            DirectorCommand.AnswerSystemTransitionStep(new SystemTransitionChoiceCommand(
                new SystemTransitionInstanceId("instance-1"),
                new SystemTransitionStepId("step-1"),
                new SystemTransitionItemId("item-1"),
                TeamSide.Blue,
                new SystemTransitionOptionId("option-1"))),
            DirectorCommand.AnswerRemovalPrevention(RemovalPreventionCommand.Accept(
                "request-1", new HeroId("hero-xargatha"), "prevention-source",
                new CardInstanceId("xargatha-card-03"))),
            DirectorCommand.AnswerRemovalPrevention(
                RemovalPreventionCommand.Decline("request-1", new HeroId("hero-xargatha")))
        };

        [Test]
        public void EveryCommandKind_RoundTripsFromAHandBuiltSpecimen()
        {
            foreach (var specimen in Specimens)
            {
                var encoded = NetworkCommandCodec.Encode(specimen);
                Assert.That(NetworkCommandCodec.TryDecode(encoded, out var decoded), Is.True,
                    specimen.Kind + " did not decode.");
                Assert.That(decoded, Is.EqualTo(specimen),
                    specimen.Kind + " decoded to a different command.");
                Assert.That(NetworkCommandCodec.Encode(decoded), Is.EqualTo(encoded),
                    specimen.Kind + " re-encoded to different bytes, so equal commands would not " +
                    "encode identically and the host's offered-instance mapping could miss.");
            }

            var covered = Specimens.Select(specimen => specimen.Kind).Distinct().ToArray();
            var vocabulary = Enum.GetValues(typeof(DirectorCommandKind)).Cast<DirectorCommandKind>();
            Assert.That(covered, Is.EquivalentTo(vocabulary),
                "Every command kind has at least one specimen, or a kind could ship with no " +
                "round-trip evidence at all.");
        }

        /// <summary>
        /// The live half: a seeded official six-seat match is played through the seats themselves,
        /// and every command any seat's surface offers along the way - authorized commands and the
        /// standing decision contract's answers alike - round-trips through the codec equal.
        /// </summary>
        [Test]
        public void EveryOfferedCommandAcrossASeededOfficialMatch_RoundTripsEqual()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var coveredKinds = new HashSet<DirectorCommandKind>();
            var roundTripped = 0;
            var submitted = OwnerScopedProjectionTests.DriveThroughSeats(host, 1800, seed: 6,
                stopBeforeCommand: projection =>
            {
                foreach (var seat in host.Seats.Seats)
                {
                    var view = host.ProjectionForSeat(seat.Id);
                    var offered = view.AuthorizedCommands.Concat(
                        view.DecisionContract == null
                            ? Enumerable.Empty<DirectorCommand>()
                            : view.DecisionContract.Commands);
                    foreach (var command in offered)
                    {
                        var encoded = NetworkCommandCodec.Encode(command);
                        Assert.That(NetworkCommandCodec.TryDecode(encoded, out var decoded), Is.True,
                            "An offered " + command.Kind + " did not decode at version " + view.Version + ".");
                        Assert.That(decoded, Is.EqualTo(command),
                            "An offered " + command.Kind + " round-tripped unequal at version " +
                            view.Version + ".");
                        coveredKinds.Add(command.Kind);
                        roundTripped++;
                    }
                }

                return false;
            });

            Assert.That(submitted, Is.GreaterThan(500),
                "The seeded walk ended early; the sweep saw less of the match than it must.");
            Assert.That(roundTripped, Is.GreaterThan(1000));
            // The anti-vacuity floor: what this exact seat-driven walk deterministically reaches.
            // Total kind coverage is the specimen case's, not this sweep's; a floor violation here
            // means the sweep itself broke, not that a family moved. Milestone 8.35d re-derived
            // the seed (2 -> 3) and dropped `AdvanceEndOfRound`: the round's advance is no longer
            // a command any seat's surface offers, so a sweep over offered commands cannot see it.
            // Milestone 8.35g re-derived it once more (3 -> 1) and raised the budget: the release
            // is drawn from a one-command set and `Next(1)` still advances the stream, so seed 3
            // became a different match that reaches no Attack target and no Minion Battle at all.
            // An offline scan put both on seed 1, the Minion Battle last at step 1,167.
            Assert.That(coveredKinds, Is.SupersetOf(new[]
            {
                DirectorCommandKind.CommitCard,
                DirectorCommandKind.ChooseTiedActor,
                DirectorCommandKind.StartAction,
                DirectorCommandKind.ChooseMovementDestination,
                DirectorCommandKind.ChooseAttackTarget,
                DirectorCommandKind.AnswerEffectChoice,
                DirectorCommandKind.SelectMinionBattleRemoval,
                DirectorCommandKind.ConfirmMinionBattleRemovals,
                DirectorCommandKind.AnswerSystemTransitionStep
            }), "This seeded walk has always offered these kinds; a sweep that no longer sees " +
                "them is a broken sweep rather than a pass.");
        }

        /// <summary>
        /// The families the seeded walk cannot reach come from their own staged sessions, so the
        /// Defense response, the hero respawn, and the removal prevention each round-trip too.
        /// </summary>
        [Test]
        public void TheStagedPendingFamilies_RoundTripTheirOfferedAnswers()
        {
            var sessions = new (string name, MatchSession session, DirectorCommandKind[] expected)[]
            {
                ("defense", ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse(),
                    new[] { DirectorCommandKind.ChooseDefenseCard, DirectorCommandKind.DeclineDefense }),
                ("respawn", HeroRespawnTests.CreatePendingRespawnSession(),
                    new[] { DirectorCommandKind.RespawnHero, DirectorCommandKind.ResolveCardWithoutRespawn }),
                ("removal-prevention", ConfiguredPendingChoiceProjectionTests.CreateConfiguredRemovalPreventionPause(),
                    new[] { DirectorCommandKind.AnswerRemovalPrevention })
            };

            foreach (var (name, session, expected) in sessions)
            {
                var host = new MatchHost(session);
                var covered = new HashSet<DirectorCommandKind>();
                foreach (var seat in host.Seats.Seats)
                {
                    var view = host.ProjectionForSeat(seat.Id);
                    var offered = view.AuthorizedCommands.Concat(
                        view.DecisionContract == null
                            ? Enumerable.Empty<DirectorCommand>()
                            : view.DecisionContract.Commands);
                    foreach (var command in offered)
                    {
                        var encoded = NetworkCommandCodec.Encode(command);
                        Assert.That(NetworkCommandCodec.TryDecode(encoded, out var decoded), Is.True);
                        Assert.That(decoded, Is.EqualTo(command), name + " round-tripped unequal.");
                        covered.Add(command.Kind);
                    }
                }

                Assert.That(covered, Is.SupersetOf(expected),
                    "The " + name + " session no longer offers the answers this case exists for.");
            }
        }

        [Test]
        public void AMalformedCommandPayload_IsRefusedNotRead()
        {
            // An unknown discriminator.
            Assert.That(NetworkCommandCodec.TryDecode(BitConverter.GetBytes(999), out _), Is.False);

            // A truncated specimen of every kind: cutting any suffix off a valid encoding must
            // refuse, never yield a different command.
            foreach (var specimen in Specimens)
            {
                var encoded = NetworkCommandCodec.Encode(specimen);
                for (var length = 0; length < encoded.Length; length++)
                {
                    var truncated = new byte[length];
                    Array.Copy(encoded, truncated, length);
                    Assert.That(NetworkCommandCodec.TryDecode(truncated, out _), Is.False,
                        specimen.Kind + " truncated to " + length + " bytes decoded anyway.");
                }

                // Trailing bytes after a complete command are refused whole.
                var padded = encoded.Concat(new byte[] { 0 }).ToArray();
                Assert.That(NetworkCommandCodec.TryDecode(padded, out _), Is.False,
                    specimen.Kind + " accepted a trailing byte.");
            }

            // An attack target naming both a hero and a minion is a field combination no factory
            // produces; the decoder refuses it rather than picking one.
            var both = NetworkCommandCodec.Encode(
                DirectorCommand.ChooseAttackTarget(new HeroId("hero-arien")));
            // Rewrite by hand: kind, hero present, minion present too.
            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream, System.Text.Encoding.UTF8))
            {
                writer.Write((int)DirectorCommandKind.ChooseAttackTarget);
                writer.Write(true);
                writer.Write("hero-arien");
                writer.Write(true);
                writer.Write("red-melee-01");
                writer.Write(false);
                writer.Write(false);
                writer.Flush();
                Assert.That(NetworkCommandCodec.TryDecode(stream.ToArray(), out _), Is.False,
                    "A target naming a hero and a minion at once was read.");
            }

            Assert.That(both, Is.Not.Null);
        }

        [Test]
        public void TheSealedChannel_RoundTripsAndRefusesTamperingReplayAndReordering()
        {
            var nonce = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
            var host = PrivateSessionSecureChannel.ForHost("codec-key", nonce);
            var client = PrivateSessionSecureChannel.ForClient("codec-key", nonce);

            // Client to host, in order.
            var payload = new byte[] { 1, 2, 3, 4, 5 };
            var first = PrivateSessionTestDriver.Decode(
                client.Seal(SessionMessageKind.ClaimSeat, payload));
            Assert.That(first.Kind, Is.EqualTo(SessionMessageKind.Sealed));
            Assert.That(host.TryUnseal(first.Payload, out var inner), Is.True);
            Assert.That(inner.Kind, Is.EqualTo(SessionMessageKind.ClaimSeat));
            Assert.That(inner.Payload, Is.EqualTo(payload));

            // A replay of the same sealed frame is refused: its sequence already passed.
            Assert.That(host.TryUnseal(first.Payload, out _), Is.False,
                "A replayed sealed frame was unsealed twice.");

            // Reordering is refused: the third frame cannot arrive before the second, and the
            // refusal advances nothing, so the stream resumes in its true order.
            var second = PrivateSessionTestDriver.Decode(
                client.Seal(SessionMessageKind.SubmitCommand, new byte[] { 9 }));
            var third = PrivateSessionTestDriver.Decode(
                client.Seal(SessionMessageKind.SubmitCommand, new byte[] { 10 }));
            Assert.That(host.TryUnseal(third.Payload, out _), Is.False,
                "An out-of-order sealed frame was accepted.");
            Assert.That(host.TryUnseal(second.Payload, out _), Is.True,
                "The refusal of the out-of-order frame must not advance the sequence.");
            Assert.That(host.TryUnseal(third.Payload, out _), Is.True);

            // One flipped ciphertext byte fails the MAC, and the failed verification advances
            // nothing - the untampered original still arrives.
            var tampered = PrivateSessionTestDriver.Decode(
                client.Seal(SessionMessageKind.SubmitCommand, new byte[] { 7, 7, 7 }));
            var bytes = (byte[])tampered.Payload.Clone();
            bytes[9] ^= 0x40;
            Assert.That(host.TryUnseal(bytes, out _), Is.False, "A tampered frame verified.");
            Assert.That(host.TryUnseal(tampered.Payload, out _), Is.True);

            // A channel derived from the wrong key cannot read the right one's first frame, so
            // the refusal is the MAC's, not a sequence accident.
            var wrongKey = PrivateSessionSecureChannel.ForHost("other-key", nonce);
            var strangerFirst = PrivateSessionTestDriver.Decode(
                PrivateSessionSecureChannel.ForClient("codec-key", nonce)
                    .Seal(SessionMessageKind.Goodbye, Array.Empty<byte>()));
            Assert.That(wrongKey.TryUnseal(strangerFirst.Payload, out _), Is.False);

            // Directions are separated: a client cannot unseal its own first frame as if the host
            // had sent it, even at the matching sequence.
            var reflector = PrivateSessionSecureChannel.ForClient("codec-key", nonce);
            var reflected = PrivateSessionTestDriver.Decode(
                reflector.Seal(SessionMessageKind.Goodbye, Array.Empty<byte>()));
            Assert.That(reflector.TryUnseal(reflected.Payload, out _), Is.False,
                "A reflected frame - the client's own bytes played back - was accepted.");

            // Host to client works symmetrically.
            var down = PrivateSessionTestDriver.Decode(
                host.Seal(SessionMessageKind.Welcome, new byte[] { 42 }));
            Assert.That(client.TryUnseal(down.Payload, out var downInner), Is.True);
            Assert.That(downInner.Kind, Is.EqualTo(SessionMessageKind.Welcome));

            // Misuse is a throw, not wire behavior: no nested seal, no unbounded payload.
            Assert.Throws<ArgumentException>(() =>
                client.Seal(SessionMessageKind.Sealed, Array.Empty<byte>()));
            Assert.Throws<ArgumentException>(() => client.Seal(
                SessionMessageKind.SubmitCommand,
                new byte[PrivateSessionSecureChannel.MaxSealedPayloadBytes + 1]));
        }

        [Test]
        public void TheGameplayEnvelopeMessages_RoundTripAndRefuseMalformedInput()
        {
            var envelope = new NetworkEnvelope("correlation-12", 1, 7, 7);
            var submission = new NetworkSubmissionMessage(
                envelope, "wire-abc123", 41,
                NetworkCommandCodec.Encode(DirectorCommand.AdvanceEndOfRound()));
            var submitFrame = PrivateSessionTestDriver.Decode(
                NetworkMessageCodec.SubmitCommandFrame(submission));
            Assert.That(submitFrame.Kind, Is.EqualTo(SessionMessageKind.SubmitCommand));
            Assert.That(NetworkMessageCodec.TryDecodeSubmitCommand(
                submitFrame.Payload, out var decodedSubmission), Is.True);
            Assert.That(decodedSubmission.Envelope.SessionCorrelation, Is.EqualTo("correlation-12"));
            Assert.That(decodedSubmission.Envelope.Epoch, Is.EqualTo(1));
            Assert.That(decodedSubmission.Envelope.Sequence, Is.EqualTo(7));
            Assert.That(decodedSubmission.SubmissionId, Is.EqualTo("wire-abc123"));
            Assert.That(decodedSubmission.ExpectedVersion, Is.EqualTo(41));
            Assert.That(decodedSubmission.CommandPayload, Is.EqualTo(submission.CommandPayload));

            var result = new NetworkSubmissionResultMessage(
                new NetworkEnvelope("correlation-12", 1, 3, 7),
                "wire-abc123", false,
                SeatSubmissionRejectionReason.StaleProjection,
                MatchSessionRejectionReason.None,
                "The submission expects version 40.", 41, 0);
            var resultFrame = PrivateSessionTestDriver.Decode(
                NetworkMessageCodec.SubmissionResultFrame(result));
            Assert.That(NetworkMessageCodec.TryDecodeSubmissionResult(
                resultFrame.Payload, out var decodedResult), Is.True);
            Assert.That(decodedResult.Accepted, Is.False);
            Assert.That(decodedResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));
            Assert.That(decodedResult.Envelope.Correlation, Is.EqualTo(7));
            Assert.That(decodedResult.Version, Is.EqualTo(41));

            var projectionMessage = new NetworkSeatProjectionMessage(
                new NetworkEnvelope("correlation-12", 1, 4, 0), new byte[] { 1, 2, 3 });
            var projectionFrame = PrivateSessionTestDriver.Decode(
                NetworkMessageCodec.SeatProjectionFrame(projectionMessage));
            Assert.That(NetworkMessageCodec.TryDecodeSeatProjection(
                projectionFrame.Payload, out var decodedProjection), Is.True);
            Assert.That(decodedProjection.ProjectionPayload, Is.EqualTo(new byte[] { 1, 2, 3 }));

            // Truncations refuse at every length.
            for (var length = 0; length < submitFrame.Payload.Length; length++)
            {
                var truncated = new byte[length];
                Array.Copy(submitFrame.Payload, truncated, length);
                Assert.That(NetworkMessageCodec.TryDecodeSubmitCommand(truncated, out _), Is.False,
                    "A submission truncated to " + length + " bytes decoded anyway.");
            }

            // A declared command length past the remaining bytes is refused before allocation.
            var oversized = (byte[])submitFrame.Payload.Clone();
            var lengthOffset = oversized.Length - submission.CommandPayload.Length - 4;
            oversized[lengthOffset] = 0xFF;
            oversized[lengthOffset + 1] = 0xFF;
            Assert.That(NetworkMessageCodec.TryDecodeSubmitCommand(oversized, out _), Is.False);

            // Envelope bounds are constructor guards, not wire tolerance.
            Assert.Throws<ArgumentOutOfRangeException>(() => new NetworkEnvelope("c", 0, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new NetworkEnvelope("c", 1, 0, 0));
            Assert.Throws<ArgumentException>(() => new NetworkEnvelope(" ", 1, 1, 0));
            Assert.Throws<ArgumentException>(() => new NetworkSubmissionMessage(
                envelope, new string('x', NetworkMessageCodec.MaxSubmissionIdChars + 1), 0,
                Array.Empty<byte>()));
        }

        /// <summary>
        /// The projection wire record: for the seeded match's states and the staged sessions, each
        /// seat's encoding decodes to exactly the typed inventory the seat projection holds.
        /// </summary>
        [Test]
        public void ASeatProjection_RoundTripsThroughItsWireRecord()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            AssertProjectionsRoundTrip(host);
            OwnerScopedProjectionTests.DriveThroughSeats(host, 300);
            AssertProjectionsRoundTrip(host);

            AssertProjectionsRoundTrip(new MatchHost(
                ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse()));
            AssertProjectionsRoundTrip(new MatchHost(
                HeroRespawnTests.CreatePendingRespawnSession()));
            AssertProjectionsRoundTrip(new MatchHost(
                ConfiguredPendingChoiceProjectionTests.CreateConfiguredRemovalPreventionPause()));
        }

        private static void AssertProjectionsRoundTrip(IMatchHost host)
        {
            foreach (var seat in host.Seats.Seats)
            {
                var projection = host.ProjectionForSeat(seat.Id);
                var encoded = NetworkSeatProjectionCodec.Encode(projection);
                Assert.That(NetworkSeatProjectionCodec.TryDecode(encoded, out var record), Is.True,
                    "Seat " + seat.Id + "'s projection did not decode at version " +
                    projection.Version + ".");

                Assert.That(record.Version, Is.EqualTo(projection.Version));
                Assert.That(record.SeatId, Is.EqualTo(seat.Id.Value));
                Assert.That(record.PlayerId, Is.EqualTo(seat.PlayerId.Value));
                Assert.That(record.HeroId, Is.EqualTo(seat.HeroId.Value));
                Assert.That(record.TeamSide, Is.EqualTo((int)seat.TeamSide));
                Assert.That(record.RoundIndex, Is.EqualTo(projection.RoundIndex));
                Assert.That(record.TurnIndex, Is.EqualTo(projection.TurnIndex));
                Assert.That(record.Phase, Is.EqualTo((int)projection.Phase));
                Assert.That(record.IsTurnActive, Is.EqualTo(projection.IsTurnActive));
                Assert.That(record.EndOfRoundStage, Is.EqualTo(projection.CurrentEndOfRoundStage
                    .HasValue ? (int?)projection.CurrentEndOfRoundStage.Value : null));
                Assert.That(record.TieBreakerRevealedSide,
                    Is.EqualTo((int)projection.TieBreakerState.RevealedSide));

                Assert.That(record.SelectedActor.IsDefined,
                    Is.EqualTo(projection.SelectedActor.IsDefined));
                if (projection.SelectedActor.IsDefined)
                {
                    Assert.That(record.SelectedActor.ActorId,
                        Is.EqualTo(projection.SelectedActor.ActorId.Value));
                    Assert.That(record.SelectedActor.CardInstanceId,
                        Is.EqualTo(projection.SelectedActor.CardInstanceId.Value));
                    Assert.That(record.SelectedActor.InitiativeTotal,
                        Is.EqualTo(projection.SelectedActor.InitiativeTotal));
                    Assert.That(record.SelectedActor.FlipTieBreakerAfterResolution,
                        Is.EqualTo(projection.SelectedActor.FlipTieBreakerAfterResolution));
                }

                Assert.That(record.Outcome.Cause, Is.EqualTo((int)projection.Outcome.Cause));
                Assert.That(record.TeamLifeCounters.Select(row => row.TeamId + ":" + row.Count),
                    Is.EqualTo(projection.TeamLifeCounters
                        .Select(counter => counter.Key.Value + ":" + counter.Value)));
                Assert.That(record.Heroes.Select(row => row.HeroId + "@" + row.Q + "," + row.R),
                    Is.EqualTo(projection.Heroes.Select(hero =>
                        hero.HeroId.Value + "@" + hero.Coordinate.Q + "," + hero.Coordinate.R)));
                Assert.That(record.Minions.Select(row => row.MinionId + "@" + row.Q + "," + row.R),
                    Is.EqualTo(projection.Minions.Select(minion =>
                        minion.MinionId.Value + "@" + minion.Coordinate.Q + "," + minion.Coordinate.R)));
                Assert.That(record.Obstacles.Count, Is.EqualTo(projection.Obstacles.Count));
                Assert.That(record.PlacedTokens.Select(row => row.TokenInstanceId),
                    Is.EqualTo(projection.PlacedTokens.Select(token => token.Id.Value)));
                Assert.That(record.HeldMarkers.Select(row => row.MarkerInstanceId),
                    Is.EqualTo(projection.HeldMarkers.Select(marker => marker.Id.Value)));
                Assert.That(record.TurnSlots.Count, Is.EqualTo(projection.TurnSlots.Count));
                Assert.That(record.Commitments.Select(row =>
                        row.HeroId + ":" + row.Status + ":" + (row.RevealedCardInstanceId ?? "-")),
                    Is.EqualTo(projection.Commitments.Select(commitment =>
                        commitment.HeroId.Value + ":" + (int)commitment.Status + ":" +
                        (commitment.RevealedCardInstanceId?.Value ?? "-"))));
                Assert.That(record.HandSummaries.Select(row =>
                        row.HeroId + ":" + row.HandCount + ":" + row.DeckCount),
                    Is.EqualTo(projection.HandSummaries.Select(summary =>
                        summary.HeroId.Value + ":" + summary.HandCount + ":" + summary.DeckCount)));
                Assert.That(record.OwnCards.Select(row => row.InstanceId),
                    Is.EqualTo(projection.OwnCards.Select(card => card.Id.Value)));
                Assert.That(record.OwnCards.Select(row => row.Location),
                    Is.EqualTo(projection.OwnCards.Select(card => (int)card.State.Location)));
                Assert.That(record.PublicCards.Select(row => row.InstanceId),
                    Is.EqualTo(projection.PublicCards.Select(card => card.Id.Value)));
                Assert.That(record.HeroProgression.Select(row =>
                        row.HeroId + ":" + row.Level + ":" + row.Coins),
                    Is.EqualTo(projection.HeroProgression.Select(value =>
                        value.HeroId.Value + ":" + value.Level + ":" + value.Coins)));
                Assert.That(record.ActiveEffectSourceCardInstanceIds,
                    Is.EqualTo(projection.ActiveEffectSourceCardInstanceIds.Select(id => id.Value)));

                Assert.That(record.DecisionOwner.FamilyId, Is.EqualTo(
                    projection.DecisionOwner.IsDefined ? projection.DecisionOwner.FamilyId.Value : null));
                Assert.That(record.DecisionContractFamilyId, Is.EqualTo(
                    projection.DecisionContract?.FamilyId.Value));
                Assert.That(record.DecisionContractCommands, Is.EqualTo(
                    (IEnumerable<DirectorCommand>)(projection.DecisionContract?.Commands ??
                        (IReadOnlyList<DirectorCommand>)Array.Empty<DirectorCommand>())));
                Assert.That(record.AuthorizedCommands,
                    Is.EqualTo((IEnumerable<DirectorCommand>)projection.AuthorizedCommands),
                    "The decoded authorized commands must equal the projection's exactly - they " +
                    "are what a network client acts with.");
            }
        }
    }
}
