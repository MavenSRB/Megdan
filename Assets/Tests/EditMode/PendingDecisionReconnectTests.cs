using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Replay;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.34. The reconnect matrix the decision record requires: every one of the ten
    /// pending-decision families, at each of the five cut points a disconnect can fall on, proving
    /// convergence or the documented terminal result.
    /// </summary>
    /// <remarks>
    /// <para>One case per family, and each case walks all five cut points over one authority - the
    /// disconnect before the answer, the disconnect after the answer was sent but before its
    /// result arrived, the duplicate that retry produces after the return, the stale answer
    /// decided against a view the resync replaced, and the successful resume. Splitting by family
    /// rather than by cut point is deliberate: reaching a family is the expensive half, and a
    /// single case covering fifty combinations would be one long soak whose failure named
    /// nothing.</para>
    /// <para>The seven families the deterministic wire walk reaches are reached by walking to
    /// them; the three it does not - a Defense response, a hero respawn, a removal-prevention
    /// pause - are staged by the fixture library and put on the same wire. Rewinding between cut
    /// points is a snapshot restore through the authority's own port, so the family stands again
    /// exactly as it did rather than being walked to twice.</para>
    /// </remarks>
    public sealed class PendingDecisionReconnectTests
    {
        private const int WalkBudget = 1800;

        /// <summary>The eight families the deterministic seeded wire walk reaches on its own.</summary>
        [TestCase("initiative")]
        [TestCase("movement")]
        [TestCase("attack-target")]
        [TestCase("effect")]
        [TestCase("correction")]
        [TestCase("minion-battle")]
        [TestCase("system-transition")]
        [TestCase("turn-release")]
        public void AWalkedFamily_SurvivesEveryCutPoint(string family) =>
            WalkTheCutPoints(ComposeAt(family), family);

        /// <summary>
        /// The three families the walk does not reach, staged directly and put on the same wire.
        /// </summary>
        [TestCase("defense")]
        [TestCase("hero-respawn")]
        [TestCase("removal-prevention")]
        public void AStagedFamily_SurvivesEveryCutPoint(string family) =>
            WalkTheCutPoints(ComposeAt(family), family);

        /// <summary>
        /// One six-seat wire standing on that family, however that family is reached: walked to by
        /// the deterministic seeded walk, or staged directly where the walk does not go.
        /// </summary>
        private static PrivateOnlineWireHarness ComposeAt(string family)
        {
            var staged = StagedSession(family);
            var harness = staged == null
                ? PrivateOnlineWireHarness.Compose()
                : PrivateOnlineWireHarness.Compose(new MatchHost(staged));
            harness.SeatAll();

            var reached = 0;
            if (staged == null)
            {
                // Milestone 8.35u re-derived the seed AGAIN, and the cause is the same class as
                // 8.35g's: p.15 now pauses once for every hero that bought a level instead of once
                // per hero in turn, so the number of standing answers at an end of round changed
                // and every seeded trajectory after the first level-up is a different match. Seed 1
                // stopped reaching Minion Battle at all inside 2,600 steps, and Attack-target
                // slipped to step 2,549. An offline scan of twenty-four seeds
                // (`tmp/8-35d-8-35e-multiplayer-corrections/seed-scan-results.txt`) put every
                // walked family on SEED 6, the last of them - Minion Battle - at step 408, which is
                // a third of what seed 1 used to need and leaves the budget below generous.
                //
                // Milestone 8.35z20 re-derived it again: correcting Blend Into Shadows' secondary
                // rerouted this trajectory and seed 6 no longer reaches Minion Battle at all inside
                // 2,600 steps ("minion-battle: the composition stopped on another family after
                // 1800 steps" was the observed pre-fix red). A fresh offline scan of seeds 1-30 put
                // every walked family on SEED 22, the last of them (minion-battle) at step 319.
                // Milestone 8.46b re-derived it again. A choice admitting exactly one selection is now
                // answered in ONE accepted command instead of two, so every seeded trajectory past the
                // first such answer is a different match and seed 22 stopped reaching attack-target and
                // minion-battle at all inside 1,800 steps. A fresh offline scan of seeds 1-30 put every
                // walked family on SEED 29, the last of them (minion-battle) at step 558.
                reached = harness.DriveThroughWire(WalkBudget, projection =>
                    projection.DecisionContract != null &&
                    projection.DecisionContract.FamilyId.Value == family, seed: 29);
            }

            Assert.That(harness.MatchHost.Projection.DecisionContract, Is.Not.Null,
                family + ": no decision stands after " + reached + " steps.");
            Assert.That(harness.MatchHost.Projection.DecisionContract.FamilyId.Value,
                Is.EqualTo(family),
                family + ": the composition stopped on another family after " + reached + " steps.");
            harness.AssertConverged();
            return harness;
        }

        /// <summary>
        /// The staged session for a family the walk does not reach, or <c>null</c> for one it does.
        /// </summary>
        private static IMatchSession StagedSession(string family)
        {
            switch (family)
            {
                case "defense":
                    return ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse();
                case "hero-respawn":
                    return HeroRespawnTests.CreatePendingRespawnSession();
                case "removal-prevention":
                    return ConfiguredPendingChoiceProjectionTests
                        .CreateConfiguredRemovalPreventionPause();
                default:
                    return null;
            }
        }

        /// <summary>
        /// The five cut points. Two of them need the family standing and unanswered, and two need
        /// it already answered, so each half runs over its own occurrence of the family rather
        /// than over a rewound one - a rewind through the snapshot would restore the authority
        /// while leaving the host-side agreement coordinator where it was, which is a difference
        /// this matrix is not about and should not be quietly standing on.
        /// </summary>
        private static void WalkTheCutPoints(PrivateOnlineWireHarness harness, string family)
        {
            AnswerLostThenRetried(family);
            AwayBeforeAnswering(harness, family);
        }

        /// <summary>
        /// Cut points 2 and 3: the seat sends its answer, every reply to it is lost, and the seat
        /// disconnects knowing nothing. After the return the answer stands exactly once, and the
        /// retry that not knowing produces is answered from what that submission already did.
        /// </summary>
        private static void AnswerLostThenRetried(string family)
        {
            var harness = ComposeAt(family);
            var owner = OwningOrdinal(harness, family);

            harness.SetHostToClientInjector(owner, new PrivateSessionFaultInjector(
                new PrivateSessionFaultPlan(
                    "lose-the-answer's-replies",
                    Enumerable.Range(0, 4)
                        .Select(index => new SessionFaultStep(index, SessionFaultKind.Drop)))));

            var sequenceBeforeAnswer = harness.MatchHost.Sequence;
            var identity = harness.AnswerOnce(owner, out var answeredCommand, out var answeredStatement);
            var sequenceAfterAnswer = harness.MatchHost.Sequence;
            var versionAfterAnswer = harness.MatchHost.Version;
            // Milestone 8.35d. One answer is still one rules command of its own. When that answer
            // completes an end-of-round stage the seat-serving host runs the rest of rulebook p.8
            // itself, and those advances are the only other thing this window may contain.
            var appendedByAnswer = harness.MatchHost.ReadResults(sequenceBeforeAnswer);
            Assert.That(appendedByAnswer.IsComplete, Is.True, family);
            Assert.That(
                appendedByAnswer.Records.Count(record =>
                    record.Command == null ||
                    record.Command.Kind != DirectorCommandKind.AdvanceEndOfRound),
                Is.LessThanOrEqualTo(1),
                family + ": one answer appended more than one record of its own to the ordered " +
                "stream.");

            harness.DisconnectClient(owner);
            Assert.That(harness.Session.IsPaused, Is.True,
                family + ": a seat is away and the session did not hold still.");
            var returned = harness.ReconnectClient(owner);
            Assert.That(harness.Client(returned).State,
                Is.EqualTo(PrivateSessionClientState.Seated), family);
            Assert.That(harness.Client(returned).LatestProjection.Version,
                Is.EqualTo(harness.MatchHost.Version),
                family + ": the returning seat was not reprojected onto the authority's own view.");
            Assert.That(harness.MatchHost.Sequence, Is.EqualTo(sequenceAfterAnswer),
                family + ": the return moved the authority.");
            harness.AssertConverged();

            if (answeredCommand != null)
            {
                Assert.That(harness.Client(returned).TryResubmitCommand(answeredCommand, identity),
                    Is.True, family);
                harness.Pump();
                Assert.That(harness.Client(returned).LastSubmissionResult.Accepted, Is.True,
                    family + ": the retry of an accepted answer was not answered from its result.");
            }
            else
            {
                Assert.That(
                    harness.Client(returned).TryResubmitAgreement(answeredStatement, identity),
                    Is.True, family);
                harness.Pump();
                Assert.That(harness.Client(returned).LastAgreementResult.Accepted, Is.True,
                    family + ": the retry of an accepted statement was not answered from its result.");
            }

            Assert.That(harness.MatchHost.Sequence, Is.EqualTo(sequenceAfterAnswer),
                family + ": the retry executed a second time.");
            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionAfterAnswer), family);
            harness.AssertConverged();
        }

        /// <summary>
        /// Cut points 1, 5 and 4: the seat leaves with the family standing and unanswered, the
        /// match holds still for it, it comes back to the same answers under a rotated epoch, it
        /// resumes, and an answer still decided against the view it returned to is refused.
        /// </summary>
        private static void AwayBeforeAnswering(PrivateOnlineWireHarness harness, string family)
        {
            var owner = OwningOrdinal(harness, family);
            var seat = harness.SeatOf(owner);
            var epochBefore = harness.Session.EpochForSeat(new SeatId(seat));
            var standingAnswers = AnswerSurface(harness, owner);
            Assert.That(standingAnswers, Is.Not.Empty,
                family + ": the owning seat is offered nothing to answer with.");

            // ---- Cut point 1: disconnect before answering. Nothing may answer for the away
            // player, and nothing may move the board they will come back to.
            var versionAway = harness.MatchHost.Version;
            var sequenceAway = harness.MatchHost.Sequence;
            harness.DisconnectClient(owner);
            Assert.That(harness.Session.IsPaused, Is.True, family);
            Assert.That(harness.MatchHost.Projection.DecisionContract?.FamilyId.Value,
                Is.EqualTo(family),
                family + ": the pending family did not survive its owner leaving.");

            var waiting = harness.Ordinals.First(value => value != owner);
            Assert.That(harness.Client(waiting).TrySubmitCommand(
                DirectorCommand.AdvanceEndOfRound(), out _), Is.True, family);
            harness.Pump();
            Assert.That(harness.Client(waiting).LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.SessionPaused),
                family + ": another seat advanced the match while its owner was away.");
            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionAway), family);
            Assert.That(harness.MatchHost.Sequence, Is.EqualTo(sequenceAway),
                family + ": something moved while the seat was away - a default answer, or a " +
                "timeout, neither of which this policy has.");

            var second = harness.ReconnectClient(owner);
            Assert.That(harness.Session.EpochForSeat(
                    new SeatId(seat)),
                Is.GreaterThan(epochBefore),
                family + ": the returning seat's connection epoch was not rotated.");
            Assert.That(harness.Session.IsPaused, Is.False, family);
            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionAway), family);
            Assert.That(AnswerSurface(harness, second), Is.EqualTo(standingAnswers),
                family + ": the returning player was reprojected onto a different set of answers " +
                "than the one they left standing.");
            harness.AssertConverged();

            // ---- Cut point 5: the successful resume. The answer this player came back to give
            // is accepted, and every seat converges on what it did.
            harness.AnswerOnce(second, out var resumeCommand, out _);
            if (resumeCommand != null)
            {
                Assert.That(harness.Client(second).LastSubmissionResult.Accepted, Is.True,
                    family + ": the resumed answer was refused - " +
                    harness.Client(second).LastSubmissionResult.RejectionReason + " (" +
                    harness.Client(second).LastSubmissionResult.Detail + ").");
            }
            else
            {
                Assert.That(harness.Client(second).LastAgreementResult.Accepted, Is.True,
                    family + ": the resumed statement was refused - " +
                    harness.Client(second).LastAgreementResult.RejectionReason + " (" +
                    harness.Client(second).LastAgreementResult.Detail + ").");
            }

            Assert.That(harness.MatchHost.Sequence, Is.GreaterThanOrEqualTo(sequenceAway), family);
            harness.AssertConverged();

            // ---- Cut point 4: a stale answer after the resync. The view this player returned to
            // is the one the resync delivered; the match has moved past it since, and an answer
            // still decided against it is refused by name with nothing mutated - while the resync
            // offered beside that refusal converges the seat without it asking.
            var staleVersion = versionAway;
            if (harness.MatchHost.Version == staleVersion)
            {
                // A public statement short of unanimity moves nothing, which is the point of that
                // family: the rest of the team states the same preference so the host's own
                // exactly-once choice moves the match past the view this seat returned to.
                var standing = harness.MatchHost.Projection.DecisionContract;
                Assert.That(standing, Is.Not.Null,
                    family + ": the answer moved nothing and left no decision to complete.");
                harness.SubmitAgreementThroughWire(standing.Commands[0]);
            }

            var movedVersion = harness.MatchHost.Version;
            Assert.That(movedVersion, Is.GreaterThan(staleVersion),
                family + ": nothing moved, so there is no stale view to answer against.");

            harness.SendSubmissionAtVersion(
                second, "wire-stale-after-resync-" + family, staleVersion,
                harness.Client(second).LatestProjection.AuthorizedCommands.FirstOrDefault() ??
                    DirectorCommand.AdvanceEndOfRound());
            Assert.That(harness.Client(second).State,
                Is.EqualTo(PrivateSessionClientState.Seated),
                family + ": a stale answer is an ordinary gameplay moment, not a disconnect - " +
                "the connection ended with " + harness.Client(second).RejectionReason + " (" +
                harness.Client(second).RejectionDetail + ").");
            Assert.That(harness.Client(second).LastSubmissionResult.Accepted, Is.False, family);
            Assert.That(harness.Client(second).LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection), family);
            Assert.That(harness.MatchHost.Version, Is.EqualTo(movedVersion),
                family + ": a stale answer mutated the board it was not about.");
            Assert.That(harness.Client(second).LatestProjection.Version, Is.EqualTo(movedVersion),
                family + ": the resync offered beside the refusal did not converge the seat.");
            harness.AssertConverged();
        }

        /// <summary>
        /// The ordinal of a seat whose own projection carries the standing decision - the
        /// authoritative owner as milestone 8.30a derived it, read back through the wire record
        /// rather than assumed.
        /// </summary>
        private static int OwningOrdinal(PrivateOnlineWireHarness harness, string family)
        {
            foreach (var ordinal in harness.Ordinals)
            {
                var record = harness.Client(ordinal).LatestProjection;
                if (record != null && record.DecisionContractFamilyId == family &&
                    AnswerSurface(harness, ordinal).Count > 0)
                {
                    return ordinal;
                }
            }

            Assert.Fail("No seated client's own projection owns the standing " + family +
                " decision, so this case would prove nothing about who may answer it.");
            return 0;
        }

        /// <summary>
        /// Everything this seat may answer the standing decision with, as text, so two projections
        /// can be compared for having offered the same answers rather than for object identity.
        /// </summary>
        private static IReadOnlyList<string> AnswerSurface(
            PrivateOnlineWireHarness harness, int ordinal)
        {
            var record = harness.Client(ordinal).LatestProjection;
            if (record == null)
            {
                return Array.Empty<string>();
            }

            var answers = record.AuthorizedCommands.Select(command => "command:" + command).ToList();
            var agreement = record.Detail?.TeamAgreement;
            if (agreement != null)
            {
                answers.AddRange(agreement.AuthorizedStatements
                    .Select(statement => "statement:" + statement.Kind + ":" +
                        statement.ProposalId.Value + ":" +
                        statement.PreferredCardInstanceId.Value));
            }

            return answers;
        }
    }
}
