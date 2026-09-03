using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Mechanical ownership of milestone 8.35c's Full rehearsal inventory.</summary>
    public sealed class PrivateOnlineRehearsalContractTests
    {
        [Test]
        public void FullPlan_HasUniqueStableCaseIdsAndPositiveReplayCoordinates()
        {
            var cases = PrivateOnlineRehearsalPlan.FullCases;

            Assert.That(cases.Count, Is.GreaterThan(0));
            Assert.That(cases.All(value =>
                !string.IsNullOrWhiteSpace(value.Id) &&
                value.Seed > 0 &&
                value.SmallestMessagePrefixLength > 0), Is.True);
            Assert.That(cases.Select(value => value.Id).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(cases.Count), "Every Full-plan case ID must be unique and stable.");
        }

        [Test]
        public void FullPlan_CoversEveryRequiredCategoryAtItsExactCardinality()
        {
            var expected = new Dictionary<PrivateOnlineRehearsalCaseCategory, int>
            {
                { PrivateOnlineRehearsalCaseCategory.ProjectionDrop, 1 },
                { PrivateOnlineRehearsalCaseCategory.BoundedDelay, 1 },
                { PrivateOnlineRehearsalCaseCategory.DuplicateDelivery, 2 },
                { PrivateOnlineRehearsalCaseCategory.TwoMessageReorder, 1 },
                { PrivateOnlineRehearsalCaseCategory.CompletedSubmissionReplay, 1 },
                { PrivateOnlineRehearsalCaseCategory.ProjectionVersion, 3 },
                { PrivateOnlineRehearsalCaseCategory.UnknownMessageKind, 1 },
                { PrivateOnlineRehearsalCaseCategory.UnknownDiscriminator, 1 },
                { PrivateOnlineRehearsalCaseCategory.FrameTruncation, 1 },
                { PrivateOnlineRehearsalCaseCategory.MismatchedLength, 1 },
                { PrivateOnlineRehearsalCaseCategory.ProtocolLimitExceeded, 11 },
                { PrivateOnlineRehearsalCaseCategory.ForgedEnvelopeIdentity, 4 },
                { PrivateOnlineRehearsalCaseCategory.PendingDecisionReconnect, 50 },
                { PrivateOnlineRehearsalCaseCategory.ReconnectEligibilityExpiry, 1 },
                { PrivateOnlineRehearsalCaseCategory.HostTermination, 1 }
            };

            Assert.That(expected.Count,
                Is.EqualTo(Enum.GetValues(typeof(PrivateOnlineRehearsalCaseCategory)).Length));
            foreach (var row in expected)
            {
                Assert.That(PrivateOnlineRehearsalPlan.FullCases.Count(value =>
                        value.Category == row.Key),
                    Is.EqualTo(row.Value), row.Key.ToString());
            }

            var versions = PrivateOnlineRehearsalPlan.FullCases
                .Where(value => value.Category ==
                    PrivateOnlineRehearsalCaseCategory.ProjectionVersion)
                .Select(value => value.VersionRelation)
                .OrderBy(value => value)
                .ToArray();
            CollectionAssert.AreEqual(new[]
            {
                PrivateOnlineRehearsalVersionRelation.Stale,
                PrivateOnlineRehearsalVersionRelation.Current,
                PrivateOnlineRehearsalVersionRelation.Future
            }, versions);
        }

        [Test]
        public void FullPlan_DerivesAndLabelsEveryProtocolSizeOrCountLimitPlusOne()
        {
            var expected = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "frame-payload-bytes", PrivateSessionProtocol.MaxPayloadBytes },
                { "sealed-payload-bytes", PrivateSessionSecureChannel.MaxSealedPayloadBytes },
                { "challenge-nonce-bytes", PrivateSessionProtocol.MaxNonceBytes },
                { "reject-detail-characters", PrivateSessionProtocol.MaxRejectDetailChars },
                { "seat-count", PrivateSessionProtocol.MaxSeats },
                { "concurrent-connection-count", PrivateSessionProtocol.MaxConcurrentConnections },
                { "reconnect-capability-characters", PrivateSessionProtocol.MaxReconnectCapabilityChars },
                { "submission-identity-characters", NetworkMessageCodec.MaxSubmissionIdChars },
                { "command-token-selection-count", NetworkCommandCodec.MaxTokenSelections },
                { "projection-list-entry-count", NetworkSeatProjectionCodec.MaxListEntries },
                { "submission-idempotency-window-count",
                    PrivateSessionProtocol.SubmissionIdempotencyWindowEntries }
            };
            var cases = PrivateOnlineRehearsalPlan.FullCases
                .Where(value => value.Category ==
                    PrivateOnlineRehearsalCaseCategory.ProtocolLimitExceeded)
                .ToArray();

            Assert.That(cases.Length, Is.EqualTo(expected.Count));
            CollectionAssert.AreEquivalent(expected.Keys, cases.Select(value => value.LimitId));
            foreach (var value in cases)
            {
                Assert.That(value.DeclaredLimit.HasValue, Is.True, value.Id);
                Assert.That(value.ExercisedValue.HasValue, Is.True, value.Id);
                Assert.That(value.DeclaredLimit.Value, Is.EqualTo(expected[value.LimitId]), value.Id);
                Assert.That(value.ExercisedValue.Value,
                    Is.EqualTo(checked(expected[value.LimitId] + 1)), value.Id);
                Assert.That(value.Id, Is.EqualTo("limit-" + value.LimitId + "-plus-one"));
            }
        }

        [Test]
        public void FullPlan_ForgesEveryStableNetworkEnvelopeIdentityField()
        {
            var expected = typeof(NetworkEnvelope).GetProperties()
                .Select(value => value.Name)
                .OrderBy(value => value)
                .ToArray();
            var actual = PrivateOnlineRehearsalPlan.FullCases
                .Where(value => value.Category ==
                    PrivateOnlineRehearsalCaseCategory.ForgedEnvelopeIdentity)
                .Select(value => value.EnvelopeField.ToString())
                .OrderBy(value => value)
                .ToArray();

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void FullPlan_CrossesEveryPendingFamilyWithEveryReconnectCutPoint()
        {
            var expectedFamilies = new[]
            {
                PendingDecisionFamilies.Initiative,
                PendingDecisionFamilies.Movement,
                PendingDecisionFamilies.AttackTarget,
                PendingDecisionFamilies.Defense,
                PendingDecisionFamilies.Effect,
                PendingDecisionFamilies.Correction,
                PendingDecisionFamilies.MinionBattle,
                PendingDecisionFamilies.HeroRespawn,
                PendingDecisionFamilies.SystemTransition,
                PendingDecisionFamilies.RemovalPrevention
            };
            var expectedCutPoints = Enum.GetValues(typeof(PrivateOnlineRehearsalReconnectCutPoint))
                .Cast<PrivateOnlineRehearsalReconnectCutPoint>()
                .Where(value => value != PrivateOnlineRehearsalReconnectCutPoint.None)
                .OrderBy(value => value)
                .ToArray();

            CollectionAssert.AreEqual(
                expectedFamilies.Select(value => value.Value),
                PrivateOnlineRehearsalPlan.PendingFamilies.Select(value => value.Value));
            CollectionAssert.AreEqual(expectedCutPoints,
                PrivateOnlineRehearsalPlan.ReconnectCutPoints);

            var cases = PrivateOnlineRehearsalPlan.FullCases
                .Where(value => value.Category ==
                    PrivateOnlineRehearsalCaseCategory.PendingDecisionReconnect)
                .ToArray();
            Assert.That(cases.Length,
                Is.EqualTo(expectedFamilies.Length * expectedCutPoints.Length));

            var actualPairs = new HashSet<string>(cases.Select(value =>
                value.PendingFamily.Value + "|" + (int)value.ReconnectCutPoint),
                StringComparer.Ordinal);
            Assert.That(actualPairs.Count, Is.EqualTo(cases.Length),
                "Every family/cut-point pair must have exactly one case.");
            foreach (var family in expectedFamilies)
            {
                foreach (var cutPoint in expectedCutPoints)
                {
                    Assert.That(actualPairs.Contains(family.Value + "|" + (int)cutPoint),
                        Is.True, family.Value + "/" + cutPoint);
                }
            }
        }

        [Test]
        public void Topology_IsOneSeatlessHostAndSixCanonicalDistinctClients()
        {
            var expectedSeats = new[]
            {
                OfficialSixPlayerParticipants.RedFirstPlayerIdValue,
                OfficialSixPlayerParticipants.RedSecondPlayerIdValue,
                OfficialSixPlayerParticipants.RedThirdPlayerIdValue,
                OfficialSixPlayerParticipants.BlueFirstPlayerIdValue,
                OfficialSixPlayerParticipants.BlueSecondPlayerIdValue,
                OfficialSixPlayerParticipants.BlueThirdPlayerIdValue
            };

            Assert.That(PrivateOnlineRehearsalPlan.HostProcessCount, Is.EqualTo(1));
            Assert.That(PrivateOnlineRehearsalPlan.HostSeatCount, Is.Zero);
            Assert.That(PrivateOnlineRehearsalPlan.ClientProcessCount, Is.EqualTo(6));
            Assert.That(PrivateOnlineRehearsalPlan.TotalProcessCount, Is.EqualTo(7));
            Assert.That(PrivateOnlineRehearsalPlan.SeatIds.Count, Is.EqualTo(6));
            Assert.That(PrivateOnlineRehearsalPlan.SeatIds
                    .Select(value => value.Value)
                    .Distinct(StringComparer.Ordinal)
                    .Count(),
                Is.EqualTo(6));
            CollectionAssert.AreEqual(expectedSeats,
                PrivateOnlineRehearsalPlan.SeatIds.Select(value => value.Value));
        }

        /// <summary>
        /// A decision standing INSIDE end of round is not the round's own advance: a team-owned
        /// stage question falls to whichever side the rules name, so a rule that reserves the
        /// whole phase for one fixed seat cannot answer it.
        /// </summary>
        /// <remarks>
        /// <para>Milestone 8.35c's first live six-process match deadlocked on exactly this shape -
        /// a Blue-owned end-of-round system transition waiting on a Red seat that held nothing -
        /// and this is the session fact the correction rests on, pinned here so a later change to
        /// who may answer an end-of-round decision fails loudly rather than at the next
        /// rehearsal.</para>
        /// <para>Milestone 8.35d answered the other half of that shape and narrowed this case to
        /// the team-owned pause it was always about. The phase's advance is no longer authorized
        /// to any seat at all - the seat-serving host runs the stages itself - so there is no
        /// single-advancer rule left to collide with; and p.15's upgrade pause, which the walk can
        /// also stop on here, is now owned by the one hero it names rather than by that hero's
        /// whole side. <c>EndOfRoundContinuationTests</c> owns that half. The stop condition below
        /// therefore selects a team-owned end-of-round decision, which is the one this case is
        /// about.</para>
        /// </remarks>
        [Test]
        public void AnEndOfRoundDecision_IsAnsweredByItsOwningTeamAndNotByWhoeverAdvancesTheRound()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var walked = OwnerScopedProjectionTests.DriveThroughSeats(host, 2000,
                projection => projection.Phase == RoundResolutionPhase.EndOfRound &&
                    projection.DecisionContract != null &&
                    SeatProjectionFactory.OwnerOfStandingDecision(
                        (MatchSessionProjection)projection).IsTeamOwned,
                // Milestone 8.35d re-derived the seed: seed 2's Minion Battle - the team-owned
                // end-of-round pause this case is about - now falls outside this budget.
                3);
            var source = host.Projection;

            Assert.That(source.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound),
                "The seeded seat walk did not stop inside end of round after " + walked + " steps.");
            Assert.That(source.DecisionContract, Is.Not.Null,
                "The seeded seat walk reached no end-of-round decision after " + walked + " steps.");

            var owner = SeatProjectionFactory.OwnerOfStandingDecision(source);
            Assert.That(owner.IsTeamOwned, Is.True,
                "An end-of-round decision that no team owns would make this case vacuous.");

            var answering = 0;
            foreach (var seat in host.Seats.Seats)
            {
                var authorized = host.ProjectionForSeat(seat.Id).AuthorizedCommands;
                Assert.That(authorized.Any(command =>
                        command.Kind == DirectorCommandKind.AdvanceEndOfRound), Is.False,
                    seat.Id.Value + " was offered the round advance while a decision stands.");
                if (seat.PlaysFor(owner.TeamSide))
                {
                    Assert.That(authorized.Count, Is.GreaterThan(0),
                        seat.Id.Value + " plays for the owning side and holds no answer.");
                    answering++;
                }
                else
                {
                    Assert.That(authorized, Is.Empty,
                        seat.Id.Value + " does not play for the owning side and holds an answer.");
                }
            }

            Assert.That(answering, Is.EqualTo(3),
                "Exactly the owning side's three seats answer a team-owned end-of-round decision.");
        }

        /// <summary>
        /// The Fault-plan constants' "exceed every configured size limit by one byte" clause,
        /// executed rather than declared: every limit the catalog names is driven at its own
        /// declared value plus one against the shipped code that enforces it.
        /// </summary>
        /// <remarks>
        /// <para>The sweep is driven by the catalog, so it is not a second inventory: a twelfth
        /// declared limit arrives here as an unexercised case and fails at the default arm rather
        /// than passing silently. Five of these bounds were declared by the plan and exercised
        /// nowhere before this fixture - the challenge nonce, the rejection detail, the reconnect
        /// capability, the command token selection list and the projection list - which is exactly
        /// the gap a declaration-only inventory hides.</para>
        /// <para>Each arm drives the smallest real refusal path for its bound, not a restatement
        /// of the constant: a decode where the wire declares the count, a constructor guard where
        /// the bound is a construction rule, and the host's own accept path where the bound is a
        /// live resource.</para>
        /// </remarks>
        [Test]
        public void FullPlan_DrivesEveryDeclaredProtocolLimitAtItsDeclaredValuePlusOne()
        {
            var cases = PrivateOnlineRehearsalPlan.FullCases
                .Where(value => value.Category ==
                    PrivateOnlineRehearsalCaseCategory.ProtocolLimitExceeded)
                .ToArray();

            Assert.That(cases.Length, Is.GreaterThan(0),
                "A plan that declares no protocol limit would make this sweep vacuous.");
            foreach (var value in cases)
            {
                ExerciseDeclaredLimit(value);
            }
        }

        private static void ExerciseDeclaredLimit(PrivateOnlineRehearsalCase value)
        {
            var limit = value.DeclaredLimit.Value;
            var over = value.ExercisedValue.Value;
            switch (value.LimitId)
            {
                case "frame-payload-bytes":
                {
                    var header = PrivateSessionFrameCodec.GoodbyeFrame()
                        .Take(PrivateSessionProtocol.FrameHeaderBytes).ToArray();
                    header[8] = (byte)(over & 0xFF);
                    header[9] = (byte)((over >> 8) & 0xFF);
                    header[10] = (byte)((over >> 16) & 0xFF);
                    header[11] = (byte)((over >> 24) & 0xFF);
                    var assembler = new PrivateSessionFrameAssembler();
                    Assert.That(assembler.Add(header, 0, header.Length), Is.False, value.Id);
                    Assert.That(assembler.Rejection,
                        Is.EqualTo(SessionRejectionReason.OversizedFrame), value.Id);
                    Assert.Throws<ArgumentException>(() => PrivateSessionFrameCodec.EncodeFrame(
                        SessionMessageKind.Hello, new byte[over]), value.Id);
                    return;
                }

                case "sealed-payload-bytes":
                {
                    var channel = PrivateSessionSecureChannel.ForHost(
                        "rehearsal-session-key", new byte[16]);
                    Assert.Throws<ArgumentException>(
                        () => channel.Seal(SessionMessageKind.Hello, new byte[over]), value.Id);
                    Assert.That(channel.Seal(SessionMessageKind.Hello, new byte[limit]),
                        Is.Not.Null, value.Id + ": the declared bound itself must still seal.");
                    return;
                }

                case "challenge-nonce-bytes":
                {
                    Assert.Throws<ArgumentException>(
                        () => new SessionChallengeMessage(new byte[over]), value.Id);
                    Assert.That(PrivateSessionFrameCodec.TryDecodeChallenge(
                        LengthPrefixedPayload(over), out _), Is.False, value.Id);
                    return;
                }

                case "reject-detail-characters":
                {
                    var bounded = new SessionRejectMessage(
                        SessionRejectionReason.SessionFull, new string('x', over));
                    Assert.That(bounded.Detail.Length, Is.EqualTo(limit),
                        value.Id + ": a detail past the bound is cut to it, never carried.");
                    Assert.That(PrivateSessionFrameCodec.TryDecodeReject(
                            Payload(writer =>
                            {
                                writer.Write((int)SessionRejectionReason.SessionFull);
                                writer.Write(new string('x', over));
                            }), out _),
                        Is.False, value.Id);
                    return;
                }

                case "seat-count":
                {
                    Assert.That(PrivateSessionFrameCodec.TryDecodeRosterUpdate(
                            Payload(writer => writer.Write(over)), out _),
                        Is.False, value.Id);
                    return;
                }

                case "concurrent-connection-count":
                {
                    var driver = PrivateSessionTestDriver.Compose();
                    for (var ordinal = 1; ordinal <= limit; ordinal++)
                    {
                        driver.Session.OnConnected(ordinal, 0);
                    }

                    driver.Session.OnConnected(over, 0);
                    Assert.That(driver.Outbox.RejectFor(over).Reason,
                        Is.EqualTo(SessionRejectionReason.TooManyConnections), value.Id);
                    Assert.That(driver.Outbox.WasClosed(over), Is.True, value.Id);
                    return;
                }

                case "reconnect-capability-characters":
                {
                    Assert.Throws<ArgumentException>(() => new SeatReclaimMessage(
                        new SeatId(OfficialSixPlayerParticipants.RedFirstPlayerIdValue),
                        new string('x', over)), value.Id);
                    Assert.That(PrivateSessionFrameCodec.TryDecodeReclaimSeat(
                            Payload(writer =>
                            {
                                writer.Write(OfficialSixPlayerParticipants.RedFirstPlayerIdValue);
                                writer.Write(new string('x', over));
                            }), out _),
                        Is.False, value.Id);
                    return;
                }

                case "submission-identity-characters":
                {
                    Assert.Throws<ArgumentException>(() => new NetworkSubmissionMessage(
                        new NetworkEnvelope("rehearsal-correlation", 1, 1, 0),
                        new string('x', over), 0, Array.Empty<byte>()), value.Id);
                    return;
                }

                case "command-token-selection-count":
                {
                    var tokens = Enumerable.Range(0, over)
                        .Select(index => new TokenInstanceId("rehearsal-token-" + index))
                        .ToArray();
                    var encoded = NetworkCommandCodec.Encode(DirectorCommand.PerformClear(
                        new ActionDefinitionId("primary-attack"), tokens));
                    Assert.That(NetworkCommandCodec.TryDecode(encoded, out _), Is.False, value.Id);
                    return;
                }

                case "projection-list-entry-count":
                {
                    // The smallest projection prefix that reaches the first declared list count.
                    Assert.That(NetworkSeatProjectionCodec.TryDecode(
                            Payload(writer =>
                            {
                                writer.Write(0L);
                                writer.Write(OfficialSixPlayerParticipants.RedFirstPlayerIdValue);
                                writer.Write(OfficialSixPlayerParticipants.RedFirstPlayerIdValue);
                                writer.Write(OfficialSixPlayerParticipants.WaspHeroIdValue);
                                writer.Write(0);
                                writer.Write(0);
                                writer.Write(0);
                                writer.Write(0);
                                writer.Write(false);
                                writer.Write(false);
                                writer.Write(0);
                                writer.Write(false);
                                writer.Write(false);
                                writer.Write(over);
                            }), out _),
                        Is.False, value.Id);
                    return;
                }

                case "submission-idempotency-window-count":
                {
                    ExerciseIdempotencyWindow(value, limit);
                    return;
                }

                default:
                    Assert.Fail("The Full plan declares the protocol limit '" + value.LimitId +
                        "' and nothing here drives it. Add the exercise rather than letting a " +
                        "declared bound stand unexercised.");
                    return;
            }
        }

        /// <summary>
        /// The identity window's own boundary, driven rather than restated: the first consumed
        /// submission identity is still recognized while the window holds it, and one consumption
        /// past the declared count it has been forgotten - answered by the version check instead,
        /// and never executed a second time.
        /// </summary>
        private static void ExerciseIdempotencyWindow(PrivateOnlineRehearsalCase value, int limit)
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var random = new MatchLoopRandom(2);
            var consumed = 0;
            SeatId firstSeat = default(SeatId);
            DirectorCommand firstCommand = null;
            var firstId = string.Empty;
            long firstVersion = 0;
            var insideProbes = 0;
            var outsideProbes = 0;

            while (consumed <= limit + 1)
            {
                var legal = DeterministicMatchLoop.EnumerateLegalCommands(host.Projection);
                Assert.That(legal.Count, Is.GreaterThan(0),
                    value.Id + ": the walk ended before the window could be filled.");
                var command = legal[random.Next(legal.Count)];
                var id = "window-" + host.Sequence + "-" + consumed;
                if (TeamAgreementTestDriver.SubmitFinalAgreementCommand(
                    host, command, id, seat => consumed++))
                {
                    continue;
                }

                var owner = host.Seats.Seats.First(seat =>
                    host.ProjectionForSeat(seat.Id).AuthorizedCommands.Contains(command));
                var client = host.ClientForSeat(owner.Id);
                var version = client.Projection.Version;
                var result = client.Submit(new SeatSubmission(
                    new SubmissionId(id), version, command));
                Assert.That(result.Accepted, Is.True, result.Detail);
                consumed++;
                if (firstCommand == null)
                {
                    firstSeat = owner.Id;
                    firstCommand = command;
                    firstId = id;
                    firstVersion = version;
                }

                if (consumed < limit - 1)
                {
                    continue;
                }

                // A probe is a refused submission, so it consumes no identity of its own and
                // cannot disturb the window it is measuring.
                var versionBefore = host.Projection.Version;
                var probe = host.ClientForSeat(firstSeat).Submit(new SeatSubmission(
                    new SubmissionId(firstId), firstVersion, firstCommand));
                Assert.That(probe.Accepted, Is.False, value.Id);
                Assert.That(host.Projection.Version, Is.EqualTo(versionBefore),
                    value.Id + ": a replay moved the authoritative version.");
                if (consumed <= limit)
                {
                    insideProbes++;
                    Assert.That(probe.RejectionReason,
                        Is.EqualTo(SeatSubmissionRejectionReason.DuplicateSubmission),
                        value.Id + ": inside the window a replay is recognized by its identity.");
                }
                else
                {
                    outsideProbes++;
                    Assert.That(probe.RejectionReason,
                        Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection),
                        value.Id + ": past the window the identity is forgotten and the stale " +
                        "view answers instead - the replay is still never executed.");
                }
            }

            Assert.That(insideProbes, Is.GreaterThan(0),
                value.Id + ": no probe landed inside the declared window.");
            Assert.That(outsideProbes, Is.GreaterThan(0),
                value.Id + ": no probe landed one consumption past the declared window.");
            Assert.That(MatchHost.RetainedSubmissionIdCount, Is.EqualTo(limit),
                value.Id + ": the host's retained-identity window is the declared constant.");
        }

        private static byte[] LengthPrefixedPayload(int declaredLength) =>
            Payload(writer => writer.Write(declaredLength));

        private static byte[] Payload(Action<BinaryWriter> write)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                write(writer);
                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
