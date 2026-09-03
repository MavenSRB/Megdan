using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35f. The seat's own bounded undo: one accepted command per press, only inside
    /// its own turn's card resolution, floored at the beginning of that turn, and never through an
    /// attack whose target has been chosen or under another seat's answer.
    ///
    /// <para>The bounds are the user's own printed rule, taken from live multiplayer testing on
    /// 2026-09-03: "each player gets an undo, usable only while cards are being resolved, clickable
    /// step by step all the way back to the beginning of their turn. It can never undo the attack
    /// action: once an attack target has been selected, that is a hard wall."</para>
    ///
    /// <para>Unity-free on purpose, so the harness and the Edit Mode assembly both run it.</para>
    /// </summary>
    public sealed class SeatBoundedUndoTests
    {
        /// <summary>
        /// The ordinary shape: during its own card resolution the acting seat is published a
        /// rewind, every press takes exactly one accepted command back, and the presses run out at
        /// the turn's own beginning rather than at some earlier state.
        /// </summary>
        [Test]
        public void RepeatedUndoStepsBackOneCommandAtATimeAndStopsAtThisTurnsBeginning()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var acting = DriveToAnActingSeatWithSomethingToUndo(host, out var walked);
            Assert.That(acting, Is.Not.Null,
                "The seeded seat walk reached no turn with an undoable step in " + walked +
                " commands.");

            var client = host.ClientForSeat(acting.Id);
            var reverted = 0;
            var previous = host.Projection.AcceptedCommands.Count;
            while (client.Projection.IsSeatUndoAvailable && reverted < 40)
            {
                var version = client.Projection.Version;
                var result = client.SubmitUndo(new SeatUndoSubmission(
                    new SubmissionId("undo-step-" + reverted), version));

                Assert.That(result.Accepted, Is.True, result.Detail);
                Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(previous - 1),
                    "One press reverts exactly one accepted command.");
                Assert.That(host.Version, Is.GreaterThan(version),
                    "A rewind is an authoritative advance like any other: the version moves " +
                    "forward, so no seat can answer against the view it replaced.");
                previous = host.Projection.AcceptedCommands.Count;
                reverted++;
            }

            Assert.That(reverted, Is.GreaterThan(0),
                "The fixture must actually step back, or it proves nothing about the bound below.");
            Assert.That(client.Projection.IsSeatUndoAvailable, Is.False,
                "The presses run out; they do not run on into the previous turn.");

            var refused = client.SubmitUndo(new SeatUndoSubmission(
                new SubmissionId("undo-past-the-floor"), client.Projection.Version));

            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.UndoUnavailable));
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(previous),
                "A refused rewind moves nothing.");
        }

        /// <summary>
        /// The floor is the turn's own beginning, stated positively: what survives every press is
        /// the same round, the same turn and the same selected actor the seat started with, and
        /// the commitments that chose that actor are never reached.
        /// </summary>
        [Test]
        public void UndoNeverReachesTheCommitmentsOrTheActorSelectionOfItsOwnTurn()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var acting = DriveToAnActingSeatWithSomethingToUndo(host, out _);
            Assert.That(acting, Is.Not.Null);

            var before = host.Projection;
            var round = before.RoundIndex;
            var turn = before.TurnIndex;
            var actor = before.SelectedActor.ActorId;
            var card = before.SelectedActor.CardInstanceId;
            var commitments = before.AcceptedCommands
                .Count(command => command.Kind == DirectorCommandKind.CommitCard ||
                    command.Kind == DirectorCommandKind.Pass);

            var client = host.ClientForSeat(acting.Id);
            var pressed = 0;
            while (client.Projection.IsSeatUndoAvailable && pressed < 40)
            {
                Assert.That(client.SubmitUndo(new SeatUndoSubmission(
                        new SubmissionId("undo-floor-" + pressed),
                        client.Projection.Version)).Accepted,
                    Is.True);
                pressed++;
            }

            var after = host.Projection;
            Assert.That(after.RoundIndex, Is.EqualTo(round));
            Assert.That(after.TurnIndex, Is.EqualTo(turn));
            Assert.That(after.SelectedActor.ActorId, Is.EqualTo(actor),
                "The rewind stays inside the turn Initiative already selected.");
            Assert.That(after.SelectedActor.CardInstanceId, Is.EqualTo(card));
            Assert.That(
                after.AcceptedCommands.Count(command =>
                    command.Kind == DirectorCommandKind.CommitCard ||
                    command.Kind == DirectorCommandKind.Pass),
                Is.EqualTo(commitments),
                "No commitment or Pass is ever reverted by a seat's own turn undo.");
        }

        /// <summary>
        /// The attack wall stated the way the user stated it - "it can never undo the attack
        /// action" - as a property of a whole run rather than of one position: a seeded seat walk
        /// presses the rewind at every third state that offers one, and the Attack target
        /// selections already in the recorded stream never go down.
        /// </summary>
        /// <remarks>
        /// <para>One position could satisfy this by luck; dozens of presses across a match cannot,
        /// and the counters below refuse to let the case pass while it observed nothing.</para>
        /// <para>A press the session itself refuses is asserted rather than assumed away. Milestone
        /// 8.35f's notes record a pre-existing malformed-checkpoint refusal this walk can reach at
        /// deep histories, which the Director's own unbounded Undo answers identically from the
        /// same state and which no part of this milestone introduced; what matters to the seat
        /// control is the claim made here, that a refused rewind moves nothing at all.</para>
        /// </remarks>
        [Test]
        public void NoSequenceOfUndosEverRevertsAnAttackWhoseTargetWasChosen()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var offers = 0;
            var rewinds = 0;
            var refusals = 0;
            var attacksSeen = 0;

            var walked = OwnerScopedProjectionTests.DriveThroughSeats(host, 1200, _ =>
            {
                foreach (var seat in host.Seats.Seats)
                {
                    if (!host.ProjectionForSeat(seat.Id).IsSeatUndoAvailable)
                    {
                        continue;
                    }

                    // Every third offer rather than every one: a press at every state takes the
                    // walk apart as fast as it plays, and the run never reaches an Attack at all.
                    offers++;
                    if (offers % 3 != 0)
                    {
                        continue;
                    }

                    var client = host.ClientForSeat(seat.Id);
                    var accepted = host.Projection.AcceptedCommands;
                    var before = accepted.Count;
                    var attacksBefore = AttackTargetCount(host);
                    var reverting = accepted[before - 1];
                    var result = client.SubmitUndo(new SeatUndoSubmission(
                        new SubmissionId("sweep-undo-" + offers), client.Projection.Version));

                    if (!result.Accepted)
                    {
                        Assert.That(result.RejectionReason,
                            Is.EqualTo(SeatSubmissionRejectionReason.SessionRefused),
                            "A rewind this seat was published may only be refused by the session " +
                            "itself: " + result.Detail);
                        Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(before),
                            "A refused rewind moves nothing at all.");
                        refusals++;
                        continue;
                    }

                    Assert.That(reverting.Kind,
                        Is.Not.EqualTo(DirectorCommandKind.ChooseAttackTarget),
                        "A rewind took back an Attack's own target selection, which is the one " +
                        "command rulebook p.14's action never gives back.");
                    Assert.That(AttackTargetCount(host), Is.EqualTo(attacksBefore),
                        "The Attack target selections in the recorded stream never go down.");
                    Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(before - 1),
                        "One press reverts exactly one accepted command.");
                    rewinds++;
                }

                attacksSeen = Math.Max(attacksSeen, AttackTargetCount(host));
                return false;
            }, 3);

            Assert.That(rewinds, Is.GreaterThan(0),
                "The walk pressed no rewind in " + walked + " commands, so the property is vacuous.");
            Assert.That(attacksSeen, Is.GreaterThan(0),
                "The walk resolved no Attack in " + walked + " commands, so the wall was never " +
                "the thing under test.");
            Assert.That(AttackTargetCount(host), Is.EqualTo(attacksSeen),
                "Every Attack target selection the run ever recorded is still recorded, after " +
                rewinds + " accepted rewinds and " + refusals + " the session refused.");
        }

        /// <summary>
        /// The wall's other half, driven rather than waited for: from the moment a target is chosen
        /// until rulebook p.14's action settles, the acting seat is published no rewind and asking
        /// for one is refused by name - and before the target is chosen it still holds the steps it
        /// took earlier in its own turn, so the wall is the target selection and not the Attack.
        /// </summary>
        [Test]
        public void AnAttackWhoseTargetWasChosenOffersItsOwnActorNoRewindWhileItResolves()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var midActionRefusals = 0;
            var rewindsBeforeTheTarget = 0;
            var attacks = 0;
            var budget = 1400;

            // Each Attack the seeded walk reaches is driven by hand from its own target choice,
            // because the states this claim is about are the ones between accepting a target and
            // the action settling, and a walk that only ever appends never stops in them.
            while (budget > 0 && midActionRefusals == 0 && attacks < 8)
            {
                budget -= OwnerScopedProjectionTests.DriveThroughSeats(
                    host, budget, projection => projection.PendingAttackTargetChoice != null, 3);
                var projection = host.Projection;
                if (projection.PendingAttackTargetChoice == null)
                {
                    break;
                }

                attacks++;
                var actorSeat = host.Seats.FindByHero(projection.SelectedActor.ActorId);
                Assert.That(actorSeat, Is.Not.Null,
                    "The hero resolving the turn holds a seat, or the walk left the seat model.");
                Assert.That(host.Seats.FindByHero(projection.PendingAttackTargetChoice.ChooserId).Id,
                    Is.EqualTo(actorSeat.Id),
                    "An Attack's target is chosen by the seat resolving the turn.");

                var client = host.ClientForSeat(actorSeat.Id);
                if (client.Projection.IsSeatUndoAvailable)
                {
                    rewindsBeforeTheTarget++;
                }

                var target = client.Projection.AuthorizedCommands.FirstOrDefault(command =>
                    command.Kind == DirectorCommandKind.ChooseAttackTarget);
                if (target == null)
                {
                    break;
                }

                var attackIndex = projection.AcceptedCommands.Count;
                var chose = client.Submit(new SeatSubmission(
                    new SubmissionId("attack-target-" + attackIndex),
                    client.Projection.Version,
                    target));
                Assert.That(chose.Accepted, Is.True, chose.Detail);
                Assert.That(host.Projection.AcceptedCommands[attackIndex].Kind,
                    Is.EqualTo(DirectorCommandKind.ChooseAttackTarget));

                while (host.Projection.HasPendingAttackResolution)
                {
                    var view = host.ProjectionForSeat(actorSeat.Id);
                    var standing = host.Projection.AcceptedCommands.Count;
                    Assert.That(view.IsSeatUndoAvailable, Is.False,
                        "An Attack whose target has been chosen is resolving; its actor is " +
                        "published no rewind while rulebook p.14's action runs.");

                    var refused = host.SubmitUndoForSeat(actorSeat.Id, new SeatUndoSubmission(
                        new SubmissionId("mid-action-" + attackIndex + "-" + midActionRefusals),
                        view.Version));
                    Assert.That(refused.Accepted, Is.False);
                    Assert.That(refused.RejectionReason,
                        Is.EqualTo(SeatSubmissionRejectionReason.UndoUnavailable));
                    Assert.That(refused.Detail, Is.Not.Empty,
                        "The refusal says which bound it is; they all share one reason.");
                    Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(standing),
                        "A refused rewind moves nothing at all.");
                    midActionRefusals++;

                    var stepped = OwnerScopedProjectionTests.DriveThroughSeats(host, 1, null, 3);
                    budget -= stepped;
                    if (stepped == 0)
                    {
                        break;
                    }
                }

                Assert.That(host.Projection.AcceptedCommands[attackIndex].Kind,
                    Is.EqualTo(DirectorCommandKind.ChooseAttackTarget),
                    "The target selection is still where it was when the action settled.");
            }

            Assert.That(attacks, Is.GreaterThan(0),
                "The seeded walk reached no Attack target choice, so nothing here was tested.");
            Assert.That(rewindsBeforeTheTarget, Is.GreaterThan(0),
                "No Attack was reached whose actor still held earlier steps of its own turn, so " +
                "the claim that the wall is the target selection rather than the Attack is vacuous.");
            Assert.That(midActionRefusals, Is.GreaterThan(0),
                "No Attack in " + attacks + " took a further command after its target was chosen, " +
                "so the resolving half of the wall was never observed.");
        }

        private static int AttackTargetCount(IMatchHost host) =>
            host.Projection.AcceptedCommands
                .Count(command => command.Kind == DirectorCommandKind.ChooseAttackTarget);

        /// <summary>
        /// Off the acting seat there is no rewind at all: every other seat's projection says so and
        /// every other seat's submission is refused, whether it plays for the same side or not.
        /// </summary>
        [Test]
        public void OnlyTheSeatResolvingItsOwnTurnIsPublishedOrAllowedARewind()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var acting = DriveToAnActingSeatWithSomethingToUndo(host, out _);
            Assert.That(acting, Is.Not.Null);

            Assert.That(host.ProjectionForSeat(acting.Id).IsSeatUndoAvailable, Is.True);
            foreach (var other in host.Seats.Seats.Where(seat => !seat.Id.Equals(acting.Id)))
            {
                var view = host.ProjectionForSeat(other.Id);
                Assert.That(view.IsSeatUndoAvailable, Is.False,
                    other.Id + " is not resolving this turn and is published no rewind.");

                var version = host.Version;
                var refused = host.SubmitUndoForSeat(other.Id, new SeatUndoSubmission(
                    new SubmissionId("undo-off-turn-" + other.Id.Value), version));

                Assert.That(refused.Accepted, Is.False);
                Assert.That(refused.RejectionReason,
                    Is.EqualTo(SeatSubmissionRejectionReason.UndoUnavailable));
                Assert.That(host.Version, Is.EqualTo(version));
            }
        }

        /// <summary>
        /// The stale and duplicate discipline, which is what makes a double click safe: an undo is
        /// not idempotent, so the second press of a pair names a version the first one replaced and
        /// is refused, and a genuine repeat of one identity is answered from what it already did.
        /// Neither rewinds twice.
        /// </summary>
        [Test]
        public void ADoubleClickAndARepeatedIdentityBothRewindExactlyOnce()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var acting = DriveToAnActingSeatWithSomethingToUndo(host, out _);
            Assert.That(acting, Is.Not.Null);

            var client = host.ClientForSeat(acting.Id);
            var version = client.Projection.Version;
            var before = host.Projection.AcceptedCommands.Count;

            var first = client.SubmitUndo(new SeatUndoSubmission(new SubmissionId("dbl-1"), version));
            Assert.That(first.Accepted, Is.True, first.Detail);
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(before - 1));

            // The same click again, decided against the same view the button was drawn on.
            var second = client.SubmitUndo(new SeatUndoSubmission(new SubmissionId("dbl-2"), version));
            Assert.That(second.Accepted, Is.False);
            Assert.That(second.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(before - 1),
                "A stale rewind takes no second step.");

            // The same identity again, which is a retry rather than a second question.
            var replay = client.SubmitUndo(new SeatUndoSubmission(new SubmissionId("dbl-1"), version));
            Assert.That(replay.Accepted, Is.EqualTo(first.Accepted));
            Assert.That(replay.Version, Is.EqualTo(first.Version));
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(before - 1),
                "A replayed identity is answered from what it did, never run again.");
        }

        /// <summary>
        /// Determinism, which is the invariant the whole recorded-history design rests on: the
        /// state a mixed run of commands and rewinds lands on replays and restores identically.
        /// </summary>
        [Test]
        public void AMixedRunOfCommandsAndRewindsReplaysAndRestoresToTheSameState()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var acting = DriveToAnActingSeatWithSomethingToUndo(host, out _);
            Assert.That(acting, Is.Not.Null);

            var client = host.ClientForSeat(acting.Id);
            Assert.That(client.SubmitUndo(new SeatUndoSubmission(
                    new SubmissionId("determinism-undo"), client.Projection.Version)).Accepted,
                Is.True);

            var snapshot = host.CaptureSnapshot();
            var retained = host.Projection.AcceptedCommands.ToArray();

            var replayed = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            foreach (var command in retained)
            {
                Assert.That(replayed.Submit(MatchSubmission.ForCommand(command)).Accepted, Is.True,
                    "The retained prefix replays as the ordinary command stream it is: " + command);
            }

            Assert.That(replayed.CaptureSnapshot(), Is.EqualTo(snapshot),
                "Replaying what the rewind left reaches the state the rewind left.");

            var restored = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            Assert.That(restored.Submit(MatchSubmission.ForSnapshotRestore(snapshot)).Accepted,
                Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(snapshot));
        }

        /// <summary>
        /// The Director is untouched: its own client refuses the seat control outright, and its
        /// full-trust view never publishes one, because its Undo is the drawer's and unbounded.
        /// </summary>
        [Test]
        public void TheDirectorsOwnUndoIsUnchangedAndItsViewPublishesNoSeatRewind()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(host, 40);

            Assert.That(host.Projection.IsSeatUndoAvailable, Is.False,
                "The Director's full-trust read model carries no seat-scoped rewind.");

            var director = new DirectorMatchClient(new MatchSession(
                OwnerScopedProjectionTests.CreateOfficialInitialState()));
            var refused = director.SubmitUndo();

            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.WasRefusedBeforeTheRules, Is.True);
            Assert.That(host.Submit(MatchSubmission.ForUndo()).Accepted, Is.True,
                "The Director's own unbounded Undo through the host is exactly as it was.");
        }

        /// <summary>
        /// The same rewind across the wire on six real client machines: every seat converges on
        /// the board the rewind left, and no seat's private hand crosses to another.
        /// </summary>
        [Test]
        public void SixWireClientsConvergeOnTheRewoundBoardWithPrivateHandsIntact()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var ordinal = DriveWireToAnUndoableSeat(harness, out var walked);
            Assert.That(ordinal, Is.GreaterThan(0),
                "The seeded wire walk reached no undoable turn in " + walked + " commands.");

            var client = harness.Client(ordinal);
            var before = harness.MatchHost.Projection.AcceptedCommands.Count;
            Assert.That(client.TrySubmitUndo(out _), Is.True);
            harness.Pump();

            Assert.That(client.LastUndoResult, Is.Not.Null);
            Assert.That(client.LastUndoResult.Accepted, Is.True, client.LastUndoResult.Detail);
            Assert.That(harness.MatchHost.Projection.AcceptedCommands.Count,
                Is.EqualTo(before - 1));
            harness.AssertConverged();

            var actingSeat = harness.MatchHost.Seats.Seats.Single(seat =>
                seat.Id.Value == harness.SeatOf(ordinal));
            for (var other = 1; other <= 6; other++)
            {
                var view = harness.Client(other).LatestProjection;
                Assert.That(view.Version, Is.EqualTo(harness.MatchHost.Version),
                    "Every seat sees the board step back, not only the seat that asked.");
                Assert.That(view.IsSeatUndoAvailable ==
                        (view.SeatId == actingSeat.Id.Value && view.IsSeatUndoAvailable),
                    Is.True,
                    "No seat but the acting one is published a rewind by the wire record.");
            }

            // The privacy half, over the rewound board: a step back rebuilds every seat's record,
            // and another hero's hand and deck must still be absent from the bytes rather than
            // merely unrendered.
            for (var other = 1; other <= 6; other++)
            {
                var seat = harness.MatchHost.Seats.Seats.Single(value =>
                    value.Id.Value == harness.SeatOf(other));
                var record = harness.Client(other).LatestProjection;
                var visible = new HashSet<string>(record.OwnCards.Select(card => card.InstanceId)
                    .Concat(record.PublicCards.Select(card => card.InstanceId)));
                foreach (var hidden in harness.MatchHost.Projection.Cards.Where(card =>
                    !card.OwnerId.Equals(seat.HeroId) &&
                    (card.State.Location == CardLocation.Hand ||
                        card.State.Location == CardLocation.Deck)))
                {
                    Assert.That(visible.Contains(hidden.Id.Value), Is.False,
                        "Seat " + seat.Id + "'s rewound record names another hero's hidden card.");
                }
            }
        }

        /// <summary>
        /// The reconnect policy applies to a rewind exactly as it applies to a command: while a
        /// seat of this session is away, nothing authoritative moves, and the refusal is by name.
        /// </summary>
        [Test]
        public void ARewindIsRefusedByNameWhileASeatIsAwayAndIsTakenAfterItReturns()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var ordinal = DriveWireToAnUndoableSeat(harness, out var walked);
            Assert.That(ordinal, Is.GreaterThan(0),
                "The seeded wire walk reached no undoable turn in " + walked + " commands.");

            var away = ordinal == 1 ? 2 : 1;
            harness.DisconnectClient(away);
            Assert.That(harness.Session.IsPaused, Is.True);

            var client = harness.Client(ordinal);
            var version = harness.MatchHost.Version;
            var accepted = harness.MatchHost.Projection.AcceptedCommands.Count;
            Assert.That(client.TrySubmitUndo(out _), Is.True);
            harness.Pump();

            Assert.That(client.LastUndoResult.Accepted, Is.False);
            Assert.That(client.LastUndoResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.SessionPaused));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(version),
                "A refusal while paused mutates nothing at all.");
            Assert.That(harness.MatchHost.Projection.AcceptedCommands.Count, Is.EqualTo(accepted));

            harness.ReconnectClient(away);
            Assert.That(harness.Session.IsPaused, Is.False);

            var returned = harness.Client(ordinal);
            Assert.That(returned.TrySubmitUndo(out _), Is.True);
            harness.Pump();
            Assert.That(returned.LastUndoResult.Accepted, Is.True,
                returned.LastUndoResult.Detail);
            Assert.That(harness.MatchHost.Projection.AcceptedCommands.Count,
                Is.EqualTo(accepted - 1));
            harness.AssertConverged();
        }

        private static int DriveWireToAnUndoableSeat(
            PrivateOnlineWireHarness harness, out int walked)
        {
            var found = 0;
            walked = harness.DriveThroughWire(900, _ =>
            {
                foreach (var seat in harness.MatchHost.Seats.Seats)
                {
                    if (!harness.MatchHost.ProjectionForSeat(seat.Id).IsSeatUndoAvailable)
                    {
                        continue;
                    }

                    found = harness.OrdinalOf(seat.Id.Value);
                    return true;
                }

                return false;
            });

            return found;
        }

        /// <summary>
        /// Drives the seeded seat walk to a state where some seat is resolving its own turn and has
        /// at least one step of its own to take back.
        /// </summary>
        private static MatchSeat DriveToAnActingSeatWithSomethingToUndo(
            IMatchHost host, out int walked)
        {
            MatchSeat found = null;
            walked = OwnerScopedProjectionTests.DriveThroughSeats(host, 900, _ =>
            {
                found = host.Seats.Seats.FirstOrDefault(seat =>
                    host.ProjectionForSeat(seat.Id).IsSeatUndoAvailable);
                return found != null;
            });

            return found;
        }

    }
}
