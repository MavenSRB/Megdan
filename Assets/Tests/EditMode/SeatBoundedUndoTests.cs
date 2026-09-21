using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
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
            // Milestone 8.35g. The turn a rewind stays inside is named by the release pause when
            // one stands and by the selection when the resolution is still running - the two are
            // the same turn, and acknowledging the resolved card is what moves the name from one
            // to the other. Reading only the selection would compare an emptied one against the
            // hero the rewind correctly restores.
            var actor = TurnActorOf(before);
            var card = TurnCardOf(before);
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
            Assert.That(TurnActorOf(after), Is.EqualTo(actor),
                "The rewind stays inside the turn Initiative already selected.");
            Assert.That(TurnCardOf(after), Is.EqualTo(card));
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

            var walked = OwnerScopedProjectionTests.DriveThroughSeats(host, 1600, _ =>
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
                // Seed 14 since milestone 8.35l, and the move is the seed's own hazard rather than a
                // relaxation: this walk chooses from what the projection offers, so a milestone that
                // stops offering something reroutes the whole trajectory instead of shortening it.
                // 8.35l stopped offering a Skill whose Swap or Place a standing restriction had
                // already barred, and seed 1 then reached no Attack at all inside the budget - which
                // this case's own counter caught and reported rather than passing vacuously. A scan of
                // seeds 1-24 was run offline; 14 observes five Attack target selections against the
                // old seed's zero, so the replacement strengthens the evidence rather than hunting for
                // any seed that passes.
                //
                // Milestone 8.35u moved it again, 14 -> 11, for the same class of reason and caught the
                // same way: rulebook p.15 now asks every hero that bought a level at once, so the
                // trajectory diverges after the first level-up and seed 14 reached no Attack at all in
                // 1,600 commands. A thirty-seed offline scan
                // THIS is the one seed a forward-walking probe could not predict: the sweep above presses
                // the rewind at every third offer, so the walk is taken apart as fast as it is
                // played and a probe replicating its inputs but not its undos reported five Attack
                // targets on a seed that reaches none. Candidates were therefore tried against the
                // real fixture; five of twelve pass (11, 12, 17, 21, 28), so 11 is the lowest of a
                // healthy set rather than a lucky singleton.
                //
                // Milestone 8.35z20 re-derived it again, 11 -> 3: correcting Blend Into Shadows'
                // secondary rerouted this trajectory and seed 11 reached no Attack at all in 1,600
                // commands. Candidates were again tried against the REAL undo-interleaved fixture,
                // not a plain forward-walk probe (the same 8.35u lesson still holds): of the seeds
                // scanned, 3 is the lowest that presses at least one rewind and resolves at least one
                // Attack (two, in fact), so 3 is the lowest of a healthy set rather than a lucky
                // singleton.
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
            var budget = 1800;

            // Milestone 8.46b re-derived this walk's seed, 6 -> 27. A choice admitting exactly one

            // selection is now answered in ONE accepted command instead of two, so every trajectory past

            // the first such answer is a different match: seed 6 still reaches two Attack targets but

            // neither of them takes a further command while it resolves, and "the resolving half of the

            // wall was never observed" is the red it leaves. A thirty-seed offline scan counted, per seed,

            // how many Attacks it reaches and how many mid-action states those Attacks pass through; seed

            // 27 is the richest - three Attacks, all three with the actor still holding earlier steps, and

            // five mid-action states between them.

            // Each Attack the seeded walk reaches is driven by hand from its own target choice,
            // because the states this claim is about are the ones between accepting a target and
            // the action settling, and a walk that only ever appends never stops in them.
            while (budget > 0 && midActionRefusals == 0 && attacks < 8)
            {
                budget -= OwnerScopedProjectionTests.DriveThroughSeats(
                    host, budget, projection => projection.PendingAttackTargetChoice != null, 27);
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

                    var stepped = OwnerScopedProjectionTests.DriveThroughSeats(host, 1, null, 27);
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

        /// <summary>
        /// Milestone 8.35g. Which hero's turn this is, whether it is still resolving or waiting
        /// for its own player's release.
        /// </summary>
        private static HeroId TurnActorOf(MatchSessionProjection projection) =>
            projection.PendingTurnRelease != null
                ? projection.PendingTurnRelease.ActorId
                : projection.SelectedActor.ActorId;

        private static CardInstanceId TurnCardOf(MatchSessionProjection projection) =>
            projection.PendingTurnRelease != null
                ? projection.PendingTurnRelease.CardInstanceId
                : projection.SelectedActor.CardInstanceId;

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
        /// <summary>
        /// Milestone 8.35g, and the whole reason the release exists - stated as a case rather than
        /// left as intent. From INSIDE the release pause the acting seat still steps back into the
        /// resolution it has just finished, and once the release is accepted it is itself below the
        /// next turn's floor, so no later seat can take it back.
        /// </summary>
        /// <remarks>
        /// <para>The user's report is exactly this position: "If user makes the wrong movement, the
        /// turn auto ends, so no undo will fix that." Under 8.35f alone the last action of a turn
        /// was unreachable, because completing the resolution was also the hand-off and the floor
        /// moved with it. The release keeps the turn open for its own player, and this case is what
        /// says the rewind reaches into it rather than merely surviving beside it.</para>
        /// <para>The two halves are deliberately one case: a rewind that reached backwards but let
        /// a later seat revert the release would trade one crossing for another.</para>
        /// </remarks>
        [Test]
        public void FromInsideTheReleasePause_TheSeatRewindsIntoItsFinishedResolutionAndNoOneRewindsThroughTheRelease()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var acting = DriveToAReleasePauseItsOwnSeatCanRewind(host, out var walked);
            Assert.That(acting, Is.Not.Null,
                "The seeded seat walk reached no release pause whose own seat still had a step to " +
                "take back, in " + walked + " commands.");

            var paused = host.Projection;
            var release = paused.PendingTurnRelease;
            Assert.That(release, Is.Not.Null);
            Assert.That(acting.HeroId, Is.EqualTo(release.ActorId),
                "The pause is owned by the hero whose resolution finished, and it is that seat - " +
                "not the next one - that is offered the rewind.");

            var round = paused.RoundIndex;
            var turn = paused.TurnIndex;
            var before = paused.AcceptedCommands.Count;
            var client = host.ClientForSeat(acting.Id);

            Assert.That(client.Projection.IsSeatUndoAvailable, Is.True,
                "8.35g amends 8.35f's rule from 'while the seat's card resolution is active' to " +
                "'while it is active OR while its completed resolution awaits release'.");

            var stepped = client.SubmitUndo(new SeatUndoSubmission(
                new SubmissionId("undo-from-inside-the-release"), client.Projection.Version));

            Assert.That(stepped.Accepted, Is.True, stepped.Detail);
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(before - 1),
                "One press from inside the pause takes exactly one accepted command back.");

            var rewound = host.Projection;
            Assert.That(rewound.RoundIndex, Is.EqualTo(round));
            Assert.That(rewound.TurnIndex, Is.EqualTo(turn));
            Assert.That(TurnActorOf(rewound), Is.EqualTo(release.ActorId),
                "The press lands back inside the same hero's own turn - the resolution it just " +
                "finished - rather than in the turn before it or in nothing at all.");
            Assert.That(TurnCardOf(rewound), Is.EqualTo(release.CardInstanceId));

            // The second half: play the same turn forward again and accept the release. The floor
            // then sits ON the release, so the command that ended the turn is not something the
            // next player can quietly take back.
            var replayed = ReplayForwardToTheReleasePause(host, acting);
            Assert.That(replayed, Is.True,
                "The rewound turn has to be playable forward to its release again, or the second " +
                "half of this case would be asserting about a position nothing reached.");

            var releasesBefore = ReleaseCount(host);
            var owner = host.ClientForSeat(acting.Id);
            Assert.That(owner.Submit(new SeatSubmission(
                    new SubmissionId("accept-the-release"),
                    owner.Projection.Version,
                    DirectorCommand.ReleaseTurn())).Accepted,
                Is.True);
            Assert.That(ReleaseCount(host), Is.EqualTo(releasesBefore + 1));

            Assert.That(
                host.Seats.Seats.Any(seat => host.ProjectionForSeat(seat.Id).IsSeatUndoAvailable),
                Is.False,
                "The accepted release is the new turn's floor: no seat is offered a rewind that " +
                "would step back through another player's decision to hand the turn on.");
        }

        private static int ReleaseCount(IMatchHost host) =>
            host.Projection.AcceptedCommands
                .Count(command => command.Kind == DirectorCommandKind.ReleaseTurn);

        /// <summary>
        /// Plays the acting seat's own turn forward until its release pause stands again, using the
        /// seat's own authorized commands. Milestone 8.35g.
        /// </summary>
        private static bool ReplayForwardToTheReleasePause(IMatchHost host, MatchSeat acting)
        {
            for (var step = 0; step < 40; step++)
            {
                if (host.Projection.PendingTurnRelease != null)
                {
                    return true;
                }

                var legal = DeterministicMatchLoop.EnumerateLegalCommands(host.Projection);
                var seatProjection = host.ProjectionForSeat(acting.Id);
                var command = legal.FirstOrDefault(candidate =>
                    seatProjection.AuthorizedCommands.Contains(candidate));
                if (command == null)
                {
                    return false;
                }

                var client = host.ClientForSeat(acting.Id);
                if (!client.Submit(new SeatSubmission(
                        new SubmissionId("replay-" + step),
                        client.Projection.Version,
                        command)).Accepted)
                {
                    return false;
                }
            }

            return false;
        }

        private static MatchSeat DriveToAReleasePauseItsOwnSeatCanRewind(
            IMatchHost host, out int walked)
        {
            MatchSeat found = null;
            walked = OwnerScopedProjectionTests.DriveThroughSeats(host, 900, _ =>
            {
                if (host.Projection.PendingTurnRelease == null)
                {
                    return false;
                }

                found = host.Seats.Seats.FirstOrDefault(seat =>
                    host.ProjectionForSeat(seat.Id).IsSeatUndoAvailable);
                return found != null;
            });

            return found;
        }

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

        /// <summary>
        /// Milestone 8.42e, and the CLOSING CHECK of the user's 2026-09-18 rule: "undo should stop
        /// at any interaction". No command that changed anything the acting seat's OPPONENTS own is
        /// ever left rewindable.
        /// </summary>
        /// <remarks>
        /// <para>Stated as a property over whole runs rather than at one position, on 8.35f's own
        /// terms. What it reads is deliberately NOT the host's own fingerprint but an independent
        /// and narrower one - where opposing heroes and minions stand, and where opposing players'
        /// cards are - so this fails if the host ever stops walling one of those, instead of
        /// agreeing with the implementation by construction.</para>
        /// <para>Measured before the wall existed: of 13 such commands across these two seeds, FIVE
        /// were still rewindable, two of them having moved an enemy HERO several spaces
        /// (`hero-brogan` (0,5) -> (-1,3) at seed 21 command 89, `hero-arien` (1,6) -> (-2,8) at
        /// 524). Those five are this case's regression seeds.</para>
        /// <para>The second counter is the shape the user chose between: the floor rises above the
        /// interacting command rather than ending the turn's rewind, so a seat that has touched an
        /// opponent still owns whatever it does afterwards. If that is ever tightened into a
        /// turn-long wall, this stops observing it and says so.</para>
        /// </remarks>
        [Test]
        public void NoCommandThatTouchedAnOpponentIsLeftRewindable()
        {
            var touching = 0;
            var rewindableAfterTouching = new List<string>();
            var rewindsAfterAnEarlierTouch = 0;

            foreach (var seed in new[] { 3, 21 })
            {
                var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
                var touchedThisTurn = string.Empty;

                for (var step = 0; step < 1200; step++)
                {
                    var seat = ActingSeatOf(host);
                    if (seat == null)
                    {
                        if (OwnerScopedProjectionTests.DriveThroughSeats(host, 1, null, seed) == 0)
                        {
                            break;
                        }

                        continue;
                    }

                    var before = OpposingState(host, seat.TeamSide);
                    var index = host.Projection.AcceptedCommands.Count;
                    var turn = TurnKeyOf(host);
                    if (OwnerScopedProjectionTests.DriveThroughSeats(host, 1, null, seed) == 0)
                    {
                        break;
                    }

                    if (host.Projection.AcceptedCommands.Count <= index)
                    {
                        continue;
                    }

                    var after = ActingSeatOf(host);
                    if (after == null || !after.Id.Equals(seat.Id))
                    {
                        continue;
                    }

                    var undoable = host.ProjectionForSeat(seat.Id).IsSeatUndoAvailable;
                    var changed = Difference(before, OpposingState(host, seat.TeamSide));
                    if (changed != null)
                    {
                        touching++;
                        touchedThisTurn = turn;
                        if (undoable)
                        {
                            rewindableAfterTouching.Add(
                                "seed " + seed + " command " + index + ": " + changed);
                        }
                    }
                    else if (undoable && string.Equals(turn, touchedThisTurn, StringComparison.Ordinal))
                    {
                        rewindsAfterAnEarlierTouch++;
                    }
                }
            }

            Assert.That(touching, Is.GreaterThan(0),
                "No command in either run changed anything an opponent owns, so the wall was never " +
                "the thing under test.");
            Assert.That(rewindableAfterTouching, Is.Empty,
                "A command that changed what an opponent owns was left rewindable: " +
                string.Join(" | ", rewindableAfterTouching));
            Assert.That(rewindsAfterAnEarlierTouch, Is.GreaterThan(0),
                "After touching an opponent, no later command in the same turn was rewindable, so " +
                "the wall is no longer the floor-raising shape the user chose and this case can no " +
                "longer tell the two apart.");
        }

        /// <summary>The seat whose turn is being resolved, release pause included, or null.</summary>
        private static MatchSeat ActingSeatOf(IMatchHost host)
        {
            var projection = host.Projection;
            var release = projection.PendingTurnRelease;
            if (release != null)
            {
                return host.Seats.FindByHero(release.ActorId);
            }

            return !projection.IsTurnActive || !projection.SelectedActor.IsDefined
                ? null
                : host.Seats.FindByHero(projection.SelectedActor.ActorId);
        }

        private static string TurnKeyOf(IMatchHost host) =>
            host.Projection.RoundIndex + ":" + host.Projection.TurnIndex;

        /// <summary>
        /// Milestone 8.42e. What the seats OPPOSING the named side own, read independently of the
        /// host's own fingerprint and narrower than it: heroes and minions by where they stand,
        /// cards by where they are.
        /// </summary>
        private static Dictionary<string, string> OpposingState(IMatchHost host, TeamSide side)
        {
            var projection = host.Projection;
            var state = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var seat in host.Seats.Seats.Where(value => value.TeamSide != side))
            {
                var hero = projection.Heroes.FirstOrDefault(value => value.HeroId.Equals(seat.HeroId));
                state["hero:" + seat.HeroId.Value] =
                    hero.HeroId.IsDefined ? hero.Coordinate.ToString() : "absent";
                foreach (var card in projection.Cards.Where(value => value.OwnerId.Equals(seat.HeroId)))
                {
                    // Milestone 8.71a. Where the card is, AND the printed Initiative some card text
                    // has rewritten on it - Hanu the Trickster's Hurry up! changes the second
                    // without changing the first, and an observer that watched only the location
                    // would agree with the host by construction about a state neither of them could
                    // see. No seeded walk reaches one yet, because nothing of his is dealt until
                    // 8.71h; the term is here so that the day one is, this observer is already
                    // independent of the wall it is checking.
                    state["card:" + card.Id.Value] = card.State.Location +
                        (card.InitiativeOverride.IsDefined
                            ? "!" + card.InitiativeOverride.Value
                            : string.Empty);
                }
            }

            foreach (var minion in projection.Minions.Where(value => value.TeamSide != side))
            {
                state["minion:" + minion.MinionId.Value] = minion.Coordinate.ToString();
            }

            return state;
        }

        /// <summary>The first difference between two readings, or null when they agree.</summary>
        private static string Difference(
            Dictionary<string, string> before, Dictionary<string, string> after)
        {
            foreach (var entry in after)
            {
                if (!before.TryGetValue(entry.Key, out var was))
                {
                    return entry.Key + " appeared as " + entry.Value;
                }

                if (!string.Equals(was, entry.Value, StringComparison.Ordinal))
                {
                    return entry.Key + " " + was + " -> " + entry.Value;
                }
            }

            foreach (var entry in before.Keys.Where(key => !after.ContainsKey(key)))
            {
                return entry + " left the board";
            }

            return null;
        }

    }
}
