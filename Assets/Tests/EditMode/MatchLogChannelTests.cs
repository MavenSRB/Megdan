using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35r. The seat-public match log: what it selects, what it refuses to carry, that
    /// every seat's copy is the same copy, and that a rewind takes its lines back with it.
    /// </summary>
    /// <remarks>
    /// The privacy argument this fixture makes is structural rather than a scan for leaked strings.
    /// The selection admits only events the whole table can already see, so the strongest statement
    /// available is that every seat's log EQUALS the Director's - there is no redaction, because
    /// there is nothing any seat may be shown less of. The byte-level sweep the 8.32 instruments
    /// perform still runs over the frame in <see cref="PrivateOnlineEventChannelTests"/>; this
    /// fixture proves the property those bytes are meant to have.
    /// </remarks>
    public sealed class MatchLogChannelTests
    {
        /// <summary>
        /// The log reads spaces back out of the rule-event log's own text, so that text's inverse
        /// has to be exact. If <see cref="HexCoordinate.ToString"/> ever changes shape, this fails
        /// here rather than the log quietly losing every origin and destination it prints.
        /// </summary>
        [Test]
        public void EveryCoordinate_RoundTripsThroughItsOwnLogText()
        {
            for (var q = -12; q <= 12; q++)
            {
                for (var r = -12; r <= 12; r++)
                {
                    var coordinate = new HexCoordinate(q, r);
                    Assert.That(
                        HexCoordinateText.TryParse(coordinate.ToString(), out var parsed), Is.True,
                        "The log's own text for " + coordinate + " did not parse back.");
                    Assert.That(parsed, Is.EqualTo(coordinate));
                }
            }

            foreach (var malformed in new[]
                { null, "", "  ", "(1)", "1, 2", "(a, 2)", "(1, b)", "(1, 2", "1, 2)", "()" })
            {
                Assert.That(HexCoordinateText.TryParse(malformed, out _), Is.False,
                    "'" + (malformed ?? "<null>") + "' is not a coordinate and must not read as one.");
            }
        }

        /// <summary>
        /// The exclusions, asserted by KIND rather than by scanning strings: there is no member of
        /// the log's vocabulary that could carry a hand, an unrevealed commitment, a per-hex step,
        /// a turn slot's assignments or a pending choice's options - which is why none of them can
        /// reach a client through this channel by being mistaken for something else.
        /// </summary>
        [Test]
        public void TheVocabulary_NamesNothingTheSelectionExcludes()
        {
            var kinds = Enum.GetNames(typeof(MatchLogEntryKind));

            foreach (var forbidden in new[]
            {
                "Hand", "Commitment", "Facedown", "Committed",
                "Step", "Choice", "Requested", "Option", "Assignment", "Progressed", "Calculated"
            })
            {
                Assert.That(
                    kinds.Where(name => name.IndexOf(forbidden, StringComparison.OrdinalIgnoreCase) >= 0),
                    Is.Empty,
                    "A log kind named for '" + forbidden + "' would put an excluded event on this " +
                    "channel. The exclusions are enforced by the vocabulary having no word for them.");
            }

            // The one commitment-derived row the contract DOES carry names the revealed form only.
            Assert.That(kinds, Contains.Item(nameof(MatchLogEntryKind.CardRevealed)));
        }

        /// <summary>
        /// A driven match produces the log, and every seat holds the identical one. This is the
        /// privacy property stated positively.
        /// </summary>
        [Test]
        public void EverySeatsLog_IsTheDirectorsLog()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(host, 260);

            var director = host.Projection.MatchLog;
            Assert.That(director, Is.Not.Empty, "The driven match produced no log at all.");

            foreach (var seat in host.Seats.Seats)
            {
                Assert.That(host.ProjectionForSeat(seat.Id).MatchLog, Is.EqualTo(director),
                    "Seat " + seat.Id + " holds a different log from the Director's. The selection " +
                    "is table-public, so any difference is either a leak or a loss.");
            }
        }

        /// <summary>
        /// One line per move action, never one per hex - the contract's own first exclusion, and
        /// the reason the Move line is a fold over the steps rather than a translation of one event.
        /// </summary>
        /// <summary>
        /// On a real driven match, each one-for-one row carries exactly as many lines as its source
        /// event occurred - and the excluded events contribute none. This is the anti-drift check
        /// over real recorded text: if a reference key or an event name moved, the counts part.
        /// </summary>
        [Test]
        public void TheSelection_CarriesExactlyItsSourceEvents_AndNothingExcluded()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(host, 400);

            var events = host.Projection.DiagnosticEvents;
            var derived = MatchLogProjection.Derive(events, int.MaxValue);
            Assert.That(derived, Is.Not.Empty, "The walk carried nothing; the case measures nothing.");

            var oneForOne = new (string source, MatchLogEntryKind kind)[]
            {
                ("Action.MovementActionCompleted", MatchLogEntryKind.Move),
                ("Action.FastTravelActionCompleted", MatchLogEntryKind.FastTravel),
                ("Action.AttackResolutionCompleted", MatchLogEntryKind.Attack),
                ("Action.DefenseResponseCompleted", MatchLogEntryKind.Defense),
                ("Action.SkillActionCompleted", MatchLogEntryKind.SkillUsed),
                ("Action.TokenCleared", MatchLogEntryKind.Clear),
                ("Commitment.CardRevealed", MatchLogEntryKind.CardRevealed),
                ("Round.TurnSlotRecorded", MatchLogEntryKind.TurnStarted),
                ("Turn.TurnEnded", MatchLogEntryKind.TurnEnded),
                ("Round.NextRoundStarted", MatchLogEntryKind.RoundStarted),
                ("Round.RoundCompleted", MatchLogEntryKind.RoundCompleted),
                ("Round.PityCoinCollected", MatchLogEntryKind.PityCoinCollected),
                ("Round.HeroLevelUpLevelPurchased", MatchLogEntryKind.LevelPurchased),
                ("Turn.HeroRespawned", MatchLogEntryKind.HeroRespawned)
            };

            var covered = 0;
            foreach (var (source, kind) in oneForOne)
            {
                var occurrences = events.Count(entry => entry.EventType == source);
                Assert.That(derived.Count(entry => entry.Kind == kind), Is.EqualTo(occurrences),
                    source + " occurred " + occurrences + " times and the log does not carry that " +
                    "many " + kind + " lines. Either an event name drifted or the row is wrong.");
                if (occurrences > 0)
                {
                    covered++;
                }
            }

            Assert.That(covered, Is.GreaterThan(3),
                "This walk exercised too few rows for the identity above to mean much.");

            // The exclusions, over the same real events: the per-hex steps and every pending
            // request occurred, and contributed no line.
            foreach (var excluded in new[]
            {
                "Action.MovementStepCommitted", "Commitment.CardCommittedFacedown",
                "Action.MovementChoiceRequested", "Action.SkillChoiceRequested",
                "Action.AttackTargetChoiceRequested", "Setup.MatchInitialized"
            })
            {
                var occurrences = events.Count(entry => entry.EventType == excluded);
                if (occurrences == 0)
                {
                    continue;
                }

                // Nothing in the derived log may sit at one of those sequences.
                var sequences = events
                    .Where(entry => entry.EventType == excluded)
                    .Select(entry => entry.Sequence)
                    .ToHashSet();
                Assert.That(derived.Where(entry => sequences.Contains(entry.Sequence)), Is.Empty,
                    excluded + " is excluded, and a log line was derived at its sequence.");
            }
        }

        /// <summary>
        /// The fold, proven deterministically: several committed hexes inside one move action
        /// produce ONE line, carrying the first step's origin and the last step's destination.
        /// </summary>
        /// <remarks>
        /// The recorded log states origin and destination only on <c>MovementStepCommitted</c> -
        /// <c>MovementActionCompleted</c> carries neither - so the line the contract asks for is a
        /// fold over the steps closed by the completion, not a translation of one event. This case
        /// is what pins that, because no driven walk reliably produces a multi-hex move.
        /// </remarks>
        [Test]
        public void AMultiHexMove_FoldsToOneLine_FromFirstOriginToLastDestination()
        {
            var log = MatchLogProjection.Derive(new[]
            {
                Entry(1, "Action.MovementActionStarted",
                    "round=1;turn=2;resolution=r1;hero=wasp;card=c1;action=a1", "step=s1;remaining=3"),
                Entry(2, "Action.MovementStepCommitted",
                    "round=1;turn=2;resolution=r1;hero=wasp;card=c1;action=a1",
                    "step=s2;remaining=2;move=(0, 0)->(1, 0)"),
                Entry(3, "Action.MovementStepCommitted",
                    "round=1;turn=2;resolution=r1;hero=wasp;card=c1;action=a1",
                    "step=s3;remaining=1;move=(1, 0)->(2, -1)"),
                Entry(4, "Action.MovementStepCommitted",
                    "round=1;turn=2;resolution=r1;hero=wasp;card=c1;action=a1",
                    "step=s4;remaining=0;move=(2, -1)->(3, -1)"),
                Entry(5, "Action.MovementActionCompleted",
                    "round=1;turn=2;resolution=r1;hero=wasp;card=c1;action=a1", "step=s5;remaining=0")
            });

            Assert.That(log.Count, Is.EqualTo(1), "Three committed hexes made more than one line.");
            var line = log[0];
            Assert.That(line.Kind, Is.EqualTo(MatchLogEntryKind.Move));
            Assert.That(line.Sequence, Is.EqualTo(5), "The line belongs at the action's completion.");
            Assert.That(line.SubjectHeroId, Is.EqualTo(new HeroId("wasp")));
            Assert.That(line.RoundIndex, Is.EqualTo(1));
            Assert.That(line.TurnIndex, Is.EqualTo(2));
            Assert.That(line.HasOrigin, Is.True);
            Assert.That(line.Origin, Is.EqualTo(new HexCoordinate(0, 0)),
                "The move began where its FIRST step did.");
            Assert.That(line.HasDestination, Is.True);
            Assert.That(line.Destination, Is.EqualTo(new HexCoordinate(3, -1)),
                "The move ended where its LAST step did.");
        }

        /// <summary>
        /// An attack folds the same way: started, resolved, and closed by its own completion, as one
        /// line naming the attacker, the target and the outcome.
        /// </summary>
        [Test]
        public void AnAttack_FoldsToOneLine_CarryingItsOutcome()
        {
            foreach (var (resolved, expected) in new[]
            {
                ("Action.AttackSuccessful", MatchLogOutcome.AttackSuccessful),
                ("Action.AttackSuccessfullyDefended", MatchLogOutcome.AttackSuccessfullyDefended)
            })
            {
                var references = "round=3;turn=1;resolution=r9;hero=brogan;card=c2;action=a2;target=sabina";
                var log = MatchLogProjection.Derive(new[]
                {
                    Entry(1, "Action.AttackStarted", references, "step=s1"),
                    Entry(2, "Action.AttackTotalCalculated", references, "step=s2;attack=4"),
                    Entry(3, resolved, references, "step=s3;outcome=Successful"),
                    Entry(4, "Action.AttackResolutionCompleted", references, "step=s4")
                });

                Assert.That(log.Count, Is.EqualTo(1), resolved);
                Assert.That(log[0].Kind, Is.EqualTo(MatchLogEntryKind.Attack));
                Assert.That(log[0].Sequence, Is.EqualTo(4));
                Assert.That(log[0].SubjectHeroId, Is.EqualTo(new HeroId("brogan")));
                Assert.That(log[0].TargetHeroId, Is.EqualTo(new HeroId("sabina")));
                Assert.That(log[0].Outcome, Is.EqualTo(expected), resolved);
            }
        }

        /// <summary>
        /// A defense is one line about the hero who ANSWERED, with its outcome - and the pending
        /// request that preceded it is not carried at all, because it names an option set.
        /// </summary>
        [Test]
        public void ADefense_IsOneLineAboutTheDefender_AndItsRequestIsNotCarried()
        {
            var references =
                "round=2;turn=4;resolution=r4;hero=tigerclaw;card=c3;action=a3;target=arien;defenseCard=d7";
            var log = MatchLogProjection.Derive(new[]
            {
                Entry(1, "Action.DefenseResponseChoiceRequested", references, "step=s1"),
                Entry(2, "Action.DefenseCardDiscarded", references, "step=s2"),
                Entry(3, "Action.DefenseResponseCompleted", references, "step=s3")
            });

            Assert.That(log.Count, Is.EqualTo(1),
                "Either the pending request reached the log, or the answer did not.");
            Assert.That(log[0].Kind, Is.EqualTo(MatchLogEntryKind.Defense));
            Assert.That(log[0].SubjectHeroId, Is.EqualTo(new HeroId("arien")),
                "The defence line is about the hero who answered it.");
            Assert.That(log[0].TargetHeroId, Is.EqualTo(new HeroId("tigerclaw")));
            Assert.That(log[0].Outcome, Is.EqualTo(MatchLogOutcome.DefenseCardDiscarded));
        }

        /// <summary>
        /// The blocked-action line, in both forms - a stopped skill node and a stopped card-text
        /// node - and the card-text node that APPLIED, which is not a blocked line at all.
        /// </summary>
        [Test]
        public void ABlockedStep_IsOneLine_AndNamesNoBlocker()
        {
            var references = "round=1;turn=1;resolution=r2;hero=sabina;card=c8;action=a8";
            var log = MatchLogProjection.Derive(new[]
            {
                Entry(1, "Action.SkillEffectNodeStopped", references, "step=OnSkill:n1:ImpossibleMandatory"),
                Entry(2, "Action.CardTextApplied", references, "step=OnAttack:n2:ImpossibleMandatory"),
                Entry(3, "Action.CardTextApplied", references, "step=OnAttack:n3:Applied"),
                Entry(4, "Action.SkillEffectNodeApplied", references, "step=OnSkill:n4:Applied")
            });

            Assert.That(log.Select(entry => entry.Kind), Is.EqualTo(new[]
            {
                MatchLogEntryKind.BlockedAction, MatchLogEntryKind.BlockedAction
            }), "Only the two STOPPED nodes are blocked lines.");

            foreach (var line in log)
            {
                Assert.That(line.SubjectHeroId, Is.EqualTo(new HeroId("sabina")));
                Assert.That(line.SubjectCardInstanceId.Value, Is.EqualTo("c8"),
                    "The line names the card whose text stopped.");
            }
        }

        /// <summary>
        /// One Clear is one line - the token's removal, not also the action's closure.
        /// </summary>
        /// <remarks>
        /// This is a regression. The selection first mapped BOTH <c>Action.TokenCleared</c> and
        /// <c>Action.ClearActionCompleted</c> to the Clear row, which put two lines on every clear
        /// that removed one token. No driven walk caught it because the seeded walks perform no
        /// Clear at all - the same blind spot milestone 8.32's trap 1 records, and the reason the
        /// rare rows need built specimens rather than a longer walk.
        /// </remarks>
        [Test]
        public void AClear_IsOneLine_NamingTheTokenAndItsSpace()
        {
            var references = "round=4;turn=2;resolution=r7;hero=xargatha;card=c5;action=a5;token=t1";
            var log = MatchLogProjection.Derive(new[]
            {
                Entry(1, "Action.ClearActionStarted", references, "step=s1"),
                Entry(2, "Action.TokenCleared", references,
                    "step=s2;tokenDefinition=trap;space=(2, 3)"),
                Entry(3, "Action.ClearActionCompleted", references, "step=s3")
            });

            Assert.That(log.Count, Is.EqualTo(1),
                "A clear that removed one token wrote " + log.Count + " lines.");
            Assert.That(log[0].Kind, Is.EqualTo(MatchLogEntryKind.Clear));
            Assert.That(log[0].Sequence, Is.EqualTo(2), "The line is the removal, not the closure.");
            Assert.That(log[0].SubjectHeroId, Is.EqualTo(new HeroId("xargatha")));
            Assert.That(log[0].HasOrigin, Is.True);
            Assert.That(log[0].Origin, Is.EqualTo(new HexCoordinate(2, 3)),
                "The clear line names the space the token came off.");
        }

        private static RuleEventLogEntry Entry(
            int sequence, string eventType, string references, string detail) =>
            new RuleEventLogEntry(sequence, eventType, references, detail);

        /// <summary>
        /// The log is bounded, and the bound is the contract with a reconnecting seat: whatever the
        /// match has produced, no reader is ever handed more than the retained window.
        /// </summary>
        [Test]
        public void TheLog_NeverExceedsItsRetainedWindow()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(host, 900);

            Assert.That(host.Projection.DiagnosticEvents.Count,
                Is.GreaterThan(MatchLogProjection.RetainedEntries),
                "This walk did not produce enough history to test the bound.");
            Assert.That(host.Projection.MatchLog.Count,
                Is.LessThanOrEqualTo(MatchLogProjection.RetainedEntries));

            foreach (var seat in host.Seats.Seats)
            {
                Assert.That(host.ProjectionForSeat(seat.Id).MatchLog.Count,
                    Is.LessThanOrEqualTo(MatchLogProjection.RetainedEntries));
            }
        }

        /// <summary>
        /// The derivation is a pure function of the retained rule-event log, which is what makes
        /// "restore, replay, resync and undo redraw it for free" true rather than aspirational.
        /// </summary>
        [Test]
        public void TheLog_IsAPureFunctionOfTheRetainedEvents()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(host, 300);

            var events = host.Projection.DiagnosticEvents;
            Assert.That(MatchLogProjection.Derive(events), Is.EqualTo(MatchLogProjection.Derive(events)),
                "Two derivations from one event list disagreed.");
            Assert.That(host.Projection.MatchLog, Is.EqualTo(MatchLogProjection.Derive(events)),
                "The published log is not what the retained events derive to.");

            // A prefix of the events derives to a prefix of the log - the property a rewind relies
            // on, stated over the derivation itself rather than over a driven undo.
            var prefix = events.Take(events.Count / 2).ToArray();
            var fromPrefix = MatchLogProjection.Derive(prefix, MatchLogProjection.RetainedEntries);
            var whole = MatchLogProjection.Derive(events, MatchLogProjection.RetainedEntries);
            var carriedFromPrefix = fromPrefix.Select(entry => entry.Sequence).ToArray();
            var carriedFromWhole = whole.Select(entry => entry.Sequence).ToArray();
            Assert.That(
                carriedFromPrefix.Where(sequence => carriedFromWhole.Contains(sequence)).ToArray(),
                Is.SubsetOf(carriedFromWhole),
                "A shorter event history derived lines the longer one does not contain.");
        }

        /// <summary>
        /// A real rewind, driven through the seat control: the undone lines cease to exist, and the
        /// marker that explains their absence names the hero and counts the run.
        /// </summary>
        [Test]
        public void ARewind_DropsItsLines_AndTheMarkerNamesWhoTookThemBack()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(host, 120);

            var undoing = host.Seats.Seats.FirstOrDefault(seat =>
                host.ProjectionForSeat(seat.Id).IsSeatUndoAvailable);
            Assume.That(undoing, Is.Not.Null,
                "This walk reached no point where a seat could rewind; the case would measure nothing.");

            var before = host.ProjectionForSeat(undoing.Id).MatchLog.ToArray();
            Assert.That(host.ProjectionForSeat(undoing.Id).UndoMarker.IsStated, Is.False,
                "A marker stood before anything had been taken back.");

            var first = host.SubmitUndoForSeat(undoing.Id, new SeatUndoSubmission(
                new SubmissionId(Guid.NewGuid().ToString("N")),
                host.ProjectionForSeat(undoing.Id).Version));
            Assert.That(first.Accepted, Is.True, first.Detail);

            var afterOne = host.ProjectionForSeat(undoing.Id);
            Assert.That(afterOne.UndoMarker.IsStated, Is.True);
            Assert.That(afterOne.UndoMarker.HeroId, Is.EqualTo(undoing.HeroId));
            Assert.That(afterOne.UndoMarker.Count, Is.EqualTo(1));
            Assert.That(afterOne.MatchLog.Count, Is.LessThanOrEqualTo(before.Length),
                "A rewind left the log longer than it found it.");

            // The marker is table-public: every other seat reads the same standing rewind.
            foreach (var seat in host.Seats.Seats)
            {
                Assert.That(host.ProjectionForSeat(seat.Id).UndoMarker,
                    Is.EqualTo(afterOne.UndoMarker),
                    "Seat " + seat.Id + " could not see that " + undoing.HeroId.Value + " is rewinding.");
            }

            // A run of rewinds by the same hero counts up rather than restarting.
            if (host.ProjectionForSeat(undoing.Id).IsSeatUndoAvailable)
            {
                var second = host.SubmitUndoForSeat(undoing.Id, new SeatUndoSubmission(
                    new SubmissionId(Guid.NewGuid().ToString("N")),
                    host.ProjectionForSeat(undoing.Id).Version));
                Assert.That(second.Accepted, Is.True, second.Detail);
                Assert.That(host.ProjectionForSeat(undoing.Id).UndoMarker.Count, Is.EqualTo(2),
                    "Two rewinds in a row by one hero read as one, not as a run of two.");
            }
        }

        /// <summary>
        /// The Director's own full-trust view mints no marker, exactly as it publishes no seat undo
        /// availability - the marker is about the seat control and belongs to a hero.
        /// </summary>
        [Test]
        public void TheDirectorsOwnView_StatesNoRewindMarker()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(host, 60);

            Assert.That(host.Projection.UndoMarker.IsStated, Is.False);
            Assert.That(host.Projection.IsSeatUndoAvailable, Is.False);
        }
    }
}
