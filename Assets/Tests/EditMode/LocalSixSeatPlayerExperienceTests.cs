using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Victory;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.30c's local-session gate: three recorded complete matches, one for each victory
    /// path, replayed only through the six independently authorized seat clients.
    /// </summary>
    /// <remarks>
    /// <para>The existing complete-loop fixture proves the rules can play these records. This gate
    /// proves the player boundary can play the same records without a Director submission, while
    /// inspecting every seat after every accepted command. The record is generated first from the
    /// same fixed seed so a later rules change may legitimately change its commands without turning
    /// this into a brittle handwritten script.</para>
    /// <para>This remains an in-process local test. It proves no wire envelope, network fault,
    /// relay, NAT, latency, disconnect, reconnect, or internet behaviour.</para>
    /// </remarks>
    public sealed class LocalSixSeatPlayerExperienceTests
    {
        // Milestone 8.35g raised this from 1,200, matching the other two complete-match fixtures.
        // The release is one more recorded command per turn AND consumes a draw of its own even
        // when it is the only legal command, so every seeded trajectory is a different match rather
        // than the old one lengthened - which is why the seeds below moved as far as they did.
        private const int CompleteMatchBudget = 2400;

        private static readonly PendingDecisionFamilyId[] RequiredDecisionFamilies =
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
            PendingDecisionFamilies.RemovalPrevention,
            // Milestone 8.35g. The eleventh family, and the only one every ordinary turn reaches:
            // this list is compared for equivalence rather than containment, so leaving the release
            // out would fail here - which is exactly the guard working. Three complete matches now
            // reach all eleven.
            PendingDecisionFamilies.TurnRelease,
            // Milestone 9.21k. The twelfth family, "which of you acts" (9.21b), reached once Razzle -
            // the one many-bodied hero - is seated and Stunt Doubles has spawned: the "razzle" record
            // below reaches it through the same seat boundary as every other family here.
            PendingDecisionFamilies.ActingBody
        };

        private static readonly CardLocation[] PublicCardLocations =
        {
            CardLocation.TurnArea,
            CardLocation.DiscardPile,
            CardLocation.ItemSlot,
            CardLocation.UltimateArea
        };

        [Test]
        public void ThreeRecordedMatches_PlayThroughAllSixSeatsAcrossPrivacyRebuildAndEveryEnding()
        {
            var observedFamilies = new HashSet<PendingDecisionFamilyId>();
            var observedTeamOwners = new HashSet<TeamSide>();
            var observedCauses = new HashSet<MatchVictoryCause>();
            var sawFacedownCommitment = false;
            var sawRevealedCommitment = false;

            foreach (var scenario in Scenarios())
            {
                var recordingSession = scenario.CreateSession();
                var recording = DeterministicMatchLoop.Play(
                    recordingSession, scenario.Seed, CompleteMatchBudget);
                Assert.That(recording.Stalled, Is.False, scenario.Label + " recording stalled.");
                Assert.That(recording.ExhaustedItsBudget, Is.False,
                    scenario.Label + " recording exhausted its command budget.");
                Assert.That(recording.Outcome.Cause, Is.EqualTo(scenario.ExpectedCause));

                IMatchHost host = new MatchHost(scenario.CreateSession());
                AssertSixOfficialSeats(host, scenario.Label);
                var seatUse = host.Seats.Seats.ToDictionary(seat => seat.Id, _ => 0);
                var rejectedCrossSeat = false;
                var rejectedStaleControl = false;
                var replacedSession = false;

                for (var index = 0; index < recording.Commands.Count; index++)
                {
                    AssertSeatViews(
                        host, observedFamilies, observedTeamOwners,
                        ref sawFacedownCommitment, ref sawRevealedCommitment);

                    var command = recording.Commands[index];

                    // Milestone 8.35d. The Director's recording steps p.8's stages by hand; a
                    // seat-served host runs them itself the moment the answer before them lands,
                    // so the recorded advance has already happened here and belongs to no seat by
                    // design. The two streams stay in step because both drive the same authority:
                    // one recorded advance for each stage the host continued through.
                    if (command.Kind == DirectorCommandKind.AdvanceEndOfRound)
                    {
                        Assert.That(
                            host.Projection.Phase != RoundResolutionPhase.EndOfRound ||
                            host.Projection.DecisionContract != null,
                            Is.True,
                            scenario.Label + " command " + index + " is a recorded end-of-round " +
                            "advance the seat-served host had not already run.");
                        continue;
                    }

                    if (TeamAgreementTestDriver.SubmitFinalAgreementCommand(
                        host,
                        command,
                        "8-30n-" + scenario.Label + "-" + index,
                        seat => seatUse[seat.Id]++))
                    {
                        continue;
                    }

                    var authorized = host.Seats.Seats
                        .Where(seat => host.ProjectionForSeat(seat.Id)
                            .AuthorizedCommands.Contains(command))
                        .ToArray();
                    Assert.That(authorized, Is.Not.Empty,
                        scenario.Label + " command " + index + " belongs to no seat (" +
                        command.Kind + ").");

                    if (!rejectedCrossSeat && authorized.Length < host.Seats.Seats.Count)
                    {
                        var other = host.Seats.Seats.First(seat =>
                            authorized.All(owner => !owner.Id.Equals(seat.Id)));
                        AssertRefusedWithoutMutation(
                            host,
                            host.ClientForSeat(other.Id),
                            new SeatSubmission(
                                new SubmissionId("8-30c-cross-" + scenario.Label),
                                host.Version,
                                command),
                            new[]
                            {
                                SeatSubmissionRejectionReason.OwnershipMismatch,
                                SeatSubmissionRejectionReason.UnauthorizedCommand
                            },
                            scenario.Label + " cross-seat submission");
                        rejectedCrossSeat = true;
                    }

                    var chosenSeat = authorized
                        .OrderBy(seat => seatUse[seat.Id])
                        .ThenBy(seat => seat.Id.Value, StringComparer.Ordinal)
                        .First();
                    var client = host.ClientForSeat(chosenSeat.Id);
                    var expectedVersion = client.Projection.Version;
                    var result = client.Submit(new SeatSubmission(
                        new SubmissionId("8-30c-" + scenario.Label + "-" + index),
                        expectedVersion,
                        command));
                    Assert.That(result.Accepted, Is.True,
                        scenario.Label + " seat command " + index + " (" + command.Kind +
                        ") was refused: " + result.Detail);
                    seatUse[chosenSeat.Id]++;

                    if (!rejectedStaleControl)
                    {
                        AssertRefusedWithoutMutation(
                            host,
                            host.ClientForSeat(chosenSeat.Id),
                            new SeatSubmission(
                                new SubmissionId("8-30c-stale-" + scenario.Label),
                                expectedVersion,
                                command),
                            new[] { SeatSubmissionRejectionReason.StaleProjection },
                            scenario.Label + " stale control");
                        rejectedStaleControl = true;
                    }

                    if (!replacedSession && index + 1 == recording.Commands.Count / 2)
                    {
                        host = ReplaceFromSnapshot(host, scenario);
                        replacedSession = true;

                        if (index + 1 < recording.Commands.Count)
                        {
                            var next = recording.Commands[index + 1];
                            if (TeamAgreementTestDriver.IsFinalAgreementCommand(next))
                                continue;
                            var afterRestore = host.Seats.Seats.First(seat =>
                                host.ProjectionForSeat(seat.Id).AuthorizedCommands.Contains(next));
                            AssertRefusedWithoutMutation(
                                host,
                                host.ClientForSeat(afterRestore.Id),
                                new SeatSubmission(
                                    new SubmissionId("8-30c-rebuild-stale-" + scenario.Label),
                                    host.Version - 1,
                                    next),
                                new[] { SeatSubmissionRejectionReason.StaleProjection },
                                scenario.Label + " pre-rebuild control");
                        }
                    }
                }

                AssertSeatViews(
                    host, observedFamilies, observedTeamOwners,
                    ref sawFacedownCommitment, ref sawRevealedCommitment);
                Assert.That(host.Projection.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded));
                Assert.That(host.Projection.Outcome.Cause, Is.EqualTo(scenario.ExpectedCause));
                Assert.That(host.Projection.AuthoritativeSnapshot, Is.EqualTo(recording.FinalSnapshot),
                    scenario.Label + " ended in a different authoritative state after seat play and rebuild.");
                Assert.That(rejectedCrossSeat, Is.True);
                Assert.That(rejectedStaleControl, Is.True);
                Assert.That(replacedSession, Is.True);
                Assert.That(seatUse.Values, Is.All.GreaterThan(0),
                    scenario.Label + " did not exercise every one of its six seat perspectives.");

                foreach (var seat in host.Seats.Seats)
                {
                    var completed = host.ProjectionForSeat(seat.Id);
                    Assert.That(completed.Outcome, Is.EqualTo(host.Projection.Outcome));
                    Assert.That(completed.AuthorizedCommands, Is.Empty,
                        "An ended match offers no player control.");
                    Assert.That(completed.DecisionContract, Is.Null);
                    Assert.That(completed.IsWaitingForAnotherSeat, Is.False);
                }

                observedCauses.Add(host.Projection.Outcome.Cause);
                TestContext.WriteLine(
                    "8.30c local " + scenario.Label + ": commands=" + recording.Commands.Count +
                    ", rounds=" + recording.RoundsReached + ", cause=" + recording.Outcome.Cause +
                    ", seats=" + seatUse.Count + ", replacement=true.");
            }

            // Fortify's removal-prevention prompt is roster-required but deliberately rare: the
            // complete official records above reach the other nine families, while this configured
            // Brogan checkpoint is the existing real-card path that raises the tenth. It is driven
            // through exactly the same local seat boundary, not credited from a Core-only fixture.
            ExerciseConfiguredRemovalPreventionSeat(
                observedFamilies, observedTeamOwners,
                ref sawFacedownCommitment, ref sawRevealedCommitment);

            Assert.That(observedCauses, Is.EquivalentTo(new[]
            {
                MatchVictoryCause.LifeCountersExhausted,
                MatchVictoryCause.FinalWaveFlipped,
                MatchVictoryCause.ThroneCaptured
            }), "The local gate reaches all three rulebook victory paths through seat clients.");
            Assert.That(observedFamilies, Is.EquivalentTo(RequiredDecisionFamilies),
                "The complete local scenario corpus must reach every configured player prompt family.");
            Assert.That(observedTeamOwners, Is.EquivalentTo(new[] { TeamSide.Red, TeamSide.Blue }),
                "Team-owned choices must be exercised from both sides of the table.");
            Assert.That(sawFacedownCommitment, Is.True,
                "The privacy gate must inspect at least one standing facedown reveal boundary.");
            Assert.That(sawRevealedCommitment, Is.True,
                "The privacy gate must inspect the public side of that reveal boundary too.");
        }

        private static void ExerciseConfiguredRemovalPreventionSeat(
            ISet<PendingDecisionFamilyId> observedFamilies,
            ISet<TeamSide> observedTeamOwners,
            ref bool sawFacedownCommitment,
            ref bool sawRevealedCommitment)
        {
            var host = new MatchHost(
                ConfiguredPendingChoiceProjectionTests.CreateConfiguredRemovalPreventionPause());
            AssertSeatViews(
                host, observedFamilies, observedTeamOwners,
                ref sawFacedownCommitment, ref sawRevealedCommitment);
            Assert.That(host.Projection.DecisionContract.FamilyId,
                Is.EqualTo(PendingDecisionFamilies.RemovalPrevention));

            var command = host.Projection.DecisionContract.Commands[0];
            var authorized = host.Seats.Seats.Where(seat =>
                host.ProjectionForSeat(seat.Id).AuthorizedCommands.Contains(command)).ToArray();
            Assert.That(authorized, Has.Length.EqualTo(1),
                "Fortify's payment belongs only to Brogan's seat.");
            var waiting = host.Seats.Seats.Single(seat => !seat.Id.Equals(authorized[0].Id));
            AssertRefusedWithoutMutation(
                host,
                host.ClientForSeat(waiting.Id),
                new SeatSubmission(
                    new SubmissionId("8-30c-removal-cross-seat"), host.Version, command),
                new[]
                {
                    SeatSubmissionRejectionReason.OwnershipMismatch,
                    SeatSubmissionRejectionReason.UnauthorizedCommand
                },
                "configured removal-prevention cross-seat submission");

            var owner = host.ClientForSeat(authorized[0].Id);
            var accepted = owner.Submit(new SeatSubmission(
                new SubmissionId("8-30c-removal-owner"), owner.Projection.Version, command));
            Assert.That(accepted.Accepted, Is.True, accepted.Detail);
            Assert.That(host.Projection.PendingRemovalPrevention, Is.Null,
                "The roster prompt completes through its owning local seat.");
            TestContext.WriteLine(
                "8.30c local roster checkpoint: family=removal-prevention, owner=hero, accepted=true.");
        }

        private static IMatchHost ReplaceFromSnapshot(IMatchHost previous, RecordedScenario scenario)
        {
            var payload = previous.CaptureSnapshot();
            var beforeSnapshot = previous.Projection.AuthoritativeSnapshot;
            var beforeSeats = previous.Seats.Seats.Select(seat => seat.Id.Value).ToArray();
            var beforeViews = previous.Seats.Seats.ToDictionary(
                seat => seat.Id.Value,
                seat => OwnerScopedProjectionTests.Describe(previous.ProjectionForSeat(seat.Id)));

            var replacement = new MatchHost(scenario.CreateSession());
            var restored = replacement.Submit(MatchSubmission.ForSnapshotRestore(payload));
            Assert.That(restored.Accepted, Is.True,
                scenario.Label + " replacement refused its local snapshot: " + restored.Detail);
            Assert.That(replacement.Projection.AuthoritativeSnapshot, Is.EqualTo(beforeSnapshot));
            Assert.That(replacement.Seats.Seats.Select(seat => seat.Id.Value), Is.EqualTo(beforeSeats));
            foreach (var seat in replacement.Seats.Seats)
            {
                Assert.That(OwnerScopedProjectionTests.Describe(replacement.ProjectionForSeat(seat.Id)),
                    Is.EqualTo(beforeViews[seat.Id.Value]),
                    scenario.Label + " rebuilt a different authorized view for one seat.");
            }

            return replacement;
        }

        private static void AssertSeatViews(
            IMatchHost host,
            ISet<PendingDecisionFamilyId> observedFamilies,
            ISet<TeamSide> observedTeamOwners,
            ref bool sawFacedownCommitment,
            ref bool sawRevealedCommitment)
        {
            var global = host.Projection;
            var owner = SeatProjectionFactory.OwnerOfStandingDecision(global);
            if (global.DecisionContract != null)
            {
                Assert.That(owner.IsDefined, Is.True,
                    "Every standing player decision has an owning hero or team.");
                Assert.That(owner.FamilyId, Is.EqualTo(global.DecisionContract.FamilyId));
                observedFamilies.Add(owner.FamilyId);
                if (owner.IsTeamOwned)
                {
                    observedTeamOwners.Add(owner.TeamSide);
                }
            }

            foreach (var seat in host.Seats.Seats)
            {
                var projection = host.ProjectionForSeat(seat.Id);
                Assert.That(projection.Seat.Id, Is.EqualTo(seat.Id));
                Assert.That(projection.Version, Is.EqualTo(host.Version));

                // Milestone 8.53. The match scoreboard is public information for every seat, so the
                // claim is equality with the host's own board rather than a subset of it: every
                // column counts something the whole table watched happen - a Defeat, a declared
                // attack, an answered defense, a removed minion, a paid-out coin - and there is
                // nothing here to scope away. The victory screen every player sees at the end shows
                // the same six numbers per hero, and that has to be true at every version on the way
                // there, not only at the last one.
                Assert.That(projection.Scoreboard, Is.EqualTo(global.Scoreboard),
                    "Every seat reads the same match scoreboard as the host, line for line.");

                var visible = projection.OwnCards.Select(card => card.Id)
                    .Concat(projection.PublicCards.Select(card => card.Id))
                    .ToArray();
                Assert.That(visible.Distinct().Count(), Is.EqualTo(visible.Length),
                    "A card appears at most once in a seat projection.");

                foreach (var card in global.Cards)
                {
                    var isOwn = card.OwnerId.Equals(seat.HeroId);
                    var shouldBePublic = !isOwn &&
                        card.State.FaceOrientation == CardFaceOrientation.FaceUp &&
                        (PublicCardLocations.Contains(card.State.Location) ||
                         card.State.Location == CardLocation.Hand &&
                         projection.MatchLog.Any(entry =>
                             entry.SubjectCardInstanceId.Equals(card.Id)));
                    Assert.That(projection.OwnCards.Any(value => value.Id.Equals(card.Id)),
                        Is.EqualTo(isOwn),
                        "A seat receives all and only its own private cards.");
                    Assert.That(projection.PublicCards.Any(value => value.Id.Equals(card.Id)),
                        Is.EqualTo(shouldBePublic),
                        "Rulebook p.4 open information and bounded memory of revealed cards reach another seat.");
                }

                foreach (var summary in projection.HandSummaries)
                {
                    Assert.That(summary.HandCount, Is.EqualTo(global.Cards.Count(card =>
                        card.OwnerId.Equals(summary.HeroId) && card.State.Location == CardLocation.Hand)));
                    Assert.That(summary.DeckCount, Is.EqualTo(global.Cards.Count(card =>
                        card.OwnerId.Equals(summary.HeroId) && card.State.Location == CardLocation.Deck)));
                }

                if (global.DecisionContract == null)
                {
                    Assert.That(projection.DecisionContract, Is.Null);
                    continue;
                }

                if (owner.IsOwnedBy(seat))
                {
                    Assert.That(projection.DecisionContract, Is.Not.Null);

                    // Milestone 8.35u AMENDED this rather than relaxing it. It compared the seat's
                    // answers against the WHOLE published contract, which was the same statement only
                    // while one seat owned all of it. Rulebook p.15 now asks every hero that bought a
                    // level at once, so one contract carries several heroes' answers and a seat is
                    // shown exactly its own share - the privacy line, since a p.15 option names the
                    // two cards it moves and those sit in that hero's hand. The claim below is
                    // stricter than what it replaced: the seat's authorized set is exactly its own
                    // contract's direct commands, AND that contract is a subset of the published one.
                    Assert.That(global.DecisionContract.Commands,
                        Is.SupersetOf(projection.DecisionContract.Commands),
                        "A seat is never shown an answer the authority did not publish.");
                    var directCommands = projection.DecisionContract.Commands.Where(command =>
                        !TeamAgreementTestDriver.IsFinalAgreementCommand(command)).ToArray();
                    Assert.That(projection.AuthorizedCommands, Is.EqualTo(directCommands));
                    if (global.DecisionContract.Commands.Any(
                        TeamAgreementTestDriver.IsFinalAgreementCommand))
                        Assert.That(projection.TeamAgreement, Is.Not.Null);
                }
                else
                {
                    Assert.That(projection.DecisionContract, Is.Null,
                        "A waiting seat receives no other player's answer payloads.");
                    Assert.That(projection.AuthorizedCommands, Is.Empty);
                    Assert.That(projection.IsWaitingForAnotherSeat, Is.True);
                }
            }

            var facedown = global.Commitments.Where(commitment =>
                commitment.Status == TurnCardCommitmentStatus.CardCommittedFacedown).ToArray();
            if (facedown.Length > 0)
            {
                sawFacedownCommitment = true;
                foreach (var seat in host.Seats.Seats)
                {
                    Assert.That(host.ProjectionForSeat(seat.Id).Commitments
                        .Where(commitment => commitment.Status ==
                            TurnCardCommitmentStatus.CardCommittedFacedown)
                        .All(commitment => !commitment.RevealedCardInstanceId.HasValue), Is.True,
                        "A facedown commitment publishes no card identity before reveal.");
                }
            }

            var revealed = global.Commitments.Where(commitment =>
                    commitment.Status == TurnCardCommitmentStatus.Revealed &&
                    commitment.RevealedCardInstanceId.HasValue)
                .Where(commitment =>
                {
                    var card = global.Cards.Single(value =>
                        value.Id.Equals(commitment.RevealedCardInstanceId.Value));
                    return card.State.FaceOrientation == CardFaceOrientation.FaceUp &&
                        PublicCardLocations.Contains(card.State.Location);
                })
                .ToArray();
            if (revealed.Length > 0)
            {
                sawRevealedCommitment = true;
                foreach (var seat in host.Seats.Seats)
                {
                    var projection = host.ProjectionForSeat(seat.Id);
                    var visible = projection.OwnCards.Select(card => card.Id)
                        .Concat(projection.PublicCards.Select(card => card.Id)).ToArray();
                    Assert.That(revealed.Select(commitment => commitment.RevealedCardInstanceId.Value)
                        .All(visible.Contains), Is.True,
                        "Every revealed commitment is public to every seat.");
                }
            }
        }

        private static void AssertRefusedWithoutMutation(
            IMatchHost host,
            ISeatClient client,
            SeatSubmission submission,
            IEnumerable<SeatSubmissionRejectionReason> expectedReasons,
            string context)
        {
            var version = host.Version;
            var sequence = host.Sequence;
            var snapshot = host.CaptureSnapshot();
            var result = client.Submit(submission);

            Assert.That(result.Accepted, Is.False, context + " was unexpectedly accepted.");
            Assert.That(expectedReasons, Does.Contain(result.RejectionReason),
                context + " was refused for the wrong boundary reason.");
            Assert.That(host.Version, Is.EqualTo(version), context + " changed the version.");
            Assert.That(host.Sequence, Is.EqualTo(sequence), context + " changed the result stream.");
            Assert.That(host.CaptureSnapshot(), Is.EqualTo(snapshot), context + " changed state.");
        }

        private static void AssertSixOfficialSeats(IMatchHost host, string context)
        {
            Assert.That(host.Seats.Seats, Has.Count.EqualTo(6), context + " is not a six-seat match.");
            Assert.That(host.Seats.Seats.GroupBy(seat => seat.TeamSide).Select(group => group.Count()),
                Is.EquivalentTo(new[] { 3, 3 }), context + " is not a deterministic 3v3 table.");
            Assert.That(host.Seats.Seats.Select(seat => seat.Id).Distinct().Count(), Is.EqualTo(6));
            Assert.That(host.Seats.Seats.Select(seat => seat.HeroId).Distinct().Count(), Is.EqualTo(6));
        }

        private static IEnumerable<RecordedScenario> Scenarios()
        {
            // Milestone 8.35g re-derived both moved seeds by scanning seeds 1-60 offline on each
            // scenario's OWN fixture, never by bisecting Unity runs. Seed 3 no longer ends this
            // fixture inside 2,400 commands at all; seed 51 exhausts the Life counters in 942
            // commands over eleven rounds and is the shortest such ending that also reaches the
            // Minion Battle, the Defense and the respawn - chosen for the shape it walks through
            // rather than for being the lowest number that ends.
            //
            // Milestone 8.35z20 re-derived it again: correcting Blend Into Shadows' secondary
            // rerouted this trajectory and seed 51 now ends on FinalWaveFlipped instead of
            // LifeCountersExhausted. An offline scan of seeds 1-80 found seed 7 exhausts the Life
            // counters in 600 commands (25% of the 2,400 budget) while still reaching the Minion
            // Battle, the Defense and the respawn - far more headroom than seed 51 ever had.
            yield return new RecordedScenario(
                // Milestone 8.35z25 re-derived it again, 7 -> 12. Combat Reflexes and
                // Leaping Strike now ask their printed move before their targeting, so this walk
                // reroutes and seed 7 ends on FinalWaveFlipped instead of the Life counters. An
                // offline scan of seeds 1-80 on this scenario's OWN fixture found seed 12 exhausts
                // them after 15 rounds and 1,281 commands - 53% of the budget, and the closest match
                // in length to what seed 7 used to give (14 rounds), so the Minion Battle, the
                // Defense and the respawn this scenario owes all stay well inside the walk.
                // Milestone 9.6 re-derived it again, 12 -> 59: the offer walk now reads what each
                // action can do, so the walk reroutes. An offline scan of seeds 1-80 on this
                // scenario's OWN fixture found eighteen Life-counter endings that reach a hero
                // respawn; seed 59 - 15 rounds and 1,257 commands, the length seed 12 had - walks
                // through the Minion Battle, a Defense and the respawn this corpus needs from this
                // scenario, with a Fast Travel at its halfway index.
                "life", 59, MatchVictoryCause.LifeCountersExhausted,
                () => ConfiguredMatchFixtures.CreateEngagedOfficialSixPlayerSession(2));
            yield return new RecordedScenario(
                // Same re-derivation, same requirement as before: this is the scenario that owes the
                // displaced-minion correction family. Seed 50 no longer ends inside the budget;
                // seed 60 is the shortest of the twelve endings in seeds 1-60 that still reaches a
                // correction - 1,348 commands over fifteen rounds.
                // Milestone 8.35z25 re-derived it again, 60 -> 2, and NOT because the
                // outcome moved: seed 60 still ends on the final Wave. It moved because the reorder
                // shortened this walk from 1,348 commands to 1,144, and its new halfway command is a
                // ChooseTiedActor - a team agreement, which this fixture's walk-through hands to
                // `SubmitFinalAgreementCommand` and `continue`s past before ever reaching the
                // session-replacement check, so `replacedSession` never latched. That is the same
                // trap 8.35z20 recorded on the "throne" scenario, in a different command kind, and
                // it fails as a bare `Expected: True / But was: False` twelve lines below the
                // scenario that caused it - the summary line for the PREVIOUS scenario still prints,
                // which makes it read like the wrong scenario's failure. An offline scan of seeds
                // 1-60 on this fixture found seed 2 ends on the final Wave after 16 rounds and 1,384
                // commands with a plain CommitCard at its halfway index - the closest match in shape
                // to what seed 60 used to give (15 rounds, 1,348).
                // Milestone 8.49 re-derived it again, 2 -> 40, by the same offline scan of seeds
                // 1-80 on this fixture. A Defeat now costs the defeated hero's lowest card Tier in
                // Life counters rather than always one, so every walk with a levelled Defeat reroutes
                // from that Defeat onward: seed 2 still ends on the final Wave after 16 rounds and
                // 1,383 commands, but no longer passes through a displaced-minion correction. Of the
                // eleven seeds that end on the final Wave AND reach a correction, seed 40 - 16 rounds,
                // 1,371 commands, a plain ReleaseTurn at its halfway index - is the closest in shape
                // to what seed 2 gave.
                "wave", 40, MatchVictoryCause.FinalWaveFlipped,
                ConfiguredMatchFixtures.CreateOfficialSixPlayerSession);
            yield return new RecordedScenario(
                // Milestone 8.35z20: correcting Blend Into Shadows' secondary rerouted even this
                // uncontested push (proof that "ends before the trajectory has room to diverge" is
                // about WHICH outcome it reaches, not about the exact command stream) - seed 1's
                // stream shortened from 172 to 169 commands and its new halfway command is an
                // AdvanceEndOfRound, which this fixture's own walk-through skips via `continue`
                // before ever reaching the session-replacement check, so `replacedSession` never
                // latches. Every one of seeds 1-40 still reaches ThroneCaptured (the outcome truly
                // is that stable), so the fix is picking one whose halfway command is not skipped:
                // seed 5 is the lowest such seed, at 170 commands.
                "throne", 5, MatchVictoryCause.ThroneCaptured,
                ConfiguredMatchFixtures.CreateUncontestedOfficialSixPlayerPushSession);
            yield return new RecordedScenario(
                // Milestone 9.21k. Razzle seated in a 3v3 line-up, staged beside Wasp with two Life
                // counters a side. An offline scan of seeds 1-30 on this scenario's OWN fixture found
                // seven complete endings that answer "which of you acts"; seed 4 exhausts the Life
                // counters in 293 commands over four rounds, answers the pick three times, and has a
                // plain CommitCard at its halfway index, so the session replacement latches.
                "razzle", 4, MatchVictoryCause.LifeCountersExhausted,
                () => ConfiguredMatchFixtures.CreateEngagedSixPlayerSessionSeatingRazzle(2));
        }

        private sealed class RecordedScenario
        {
            public RecordedScenario(
                string label,
                int seed,
                MatchVictoryCause expectedCause,
                Func<MatchSession> createSession)
            {
                Label = label;
                Seed = seed;
                ExpectedCause = expectedCause;
                CreateSession = createSession;
            }

            public string Label { get; }
            public int Seed { get; }
            public MatchVictoryCause ExpectedCause { get; }
            public Func<MatchSession> CreateSession { get; }
        }
    }
}
