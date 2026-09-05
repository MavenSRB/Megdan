using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35d. Rulebook p.8's end-of-round sequence as a seat-served match runs it: the
    /// host steps every stage itself, in order, and stops only where a stage asks somebody
    /// something - and p.15's Upgrade a Card is asked of the one player whose hero it is about.
    ///
    /// <para>Both halves come from the same live multiplayer session on 2026-09-03. Six players
    /// shared one CONTINUE END OF ROUND control, so the round's pace was whoever clicked first;
    /// and the level-up pause was owned by a whole side, so a teammate could - and did - answer
    /// another player's upgrade, which is what "leveling was skipped for some players" was.</para>
    ///
    /// <para>Unity-free on purpose, so the harness and the Edit Mode assembly both run it.</para>
    /// </summary>
    public sealed class EndOfRoundContinuationTests
    {
        /// <summary>
        /// The continuation and the removed offer, proved together over a real seat walk: several
        /// rounds are completed, no seat is ever offered the advance in any phase, the projection
        /// is never left resting inside end of round with nothing standing, and the host recorded
        /// no refusal of its own.
        /// </summary>
        [Test]
        public void ASeatServedMatchCrossesEndOfRoundWithNoSeatEverBeingOfferedAnAdvance()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var advanceOffers = 0;
            var restedInsideEndOfRound = 0;

            var walked = OwnerScopedProjectionTests.DriveThroughSeats(host, 1200, projection =>
            {
                // The state a seat surface would be looking at between two of its own answers. If
                // the round can rest here, somebody has to press something to leave it, and before
                // this milestone that somebody was all six of them at once.
                if (projection.Phase == RoundResolutionPhase.EndOfRound &&
                    projection.DecisionContract == null)
                {
                    restedInsideEndOfRound++;
                }

                foreach (var seat in host.Seats.Seats)
                {
                    if (host.ProjectionForSeat(seat.Id).AuthorizedCommands.Any(command =>
                            command.Kind == DirectorCommandKind.AdvanceEndOfRound))
                    {
                        advanceOffers++;
                    }
                }

                return false;
            });

            Assert.That(host.Projection.CompletedRounds, Is.Not.Empty,
                "The walk completed no round in " + walked + " seat commands, so it never reached " +
                "an end of round and this case would prove nothing.");
            Assert.That(restedInsideEndOfRound, Is.Zero,
                "A seat-served round never waits inside end of round with no decision standing: " +
                "the host runs the stage the moment the answer before it lands.");
            Assert.That(advanceOffers, Is.Zero,
                "No seat is offered the round advance in any phase.");
            Assert.That(((MatchHost)host).EndOfRoundContinuationRefusals, Is.Empty,
                "A refused host-authored advance means the authority and the continuation disagree.");
        }

        /// <summary>
        /// The other side of the same fact: the advances exist in the recorded stream, so the
        /// stages ran, and nobody who could have submitted them did.
        /// </summary>
        [Test]
        public void TheRecordedStreamStillCarriesTheRoundAdvancesTheHostAuthored()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var advances = 0;
            long seen = 0;

            OwnerScopedProjectionTests.DriveThroughSeats(host, 1200, projection =>
            {
                var read = host.ReadResults(seen);
                if (read.IsComplete)
                {
                    advances += read.Records.Count(record =>
                        record.Command != null &&
                        record.Command.Kind == DirectorCommandKind.AdvanceEndOfRound);
                    seen = read.NextSequence;
                }

                return false;
            });

            Assert.That(host.Projection.CompletedRounds, Is.Not.Empty);

            // Milestone 8.35w. Counted from the round index rather than from
            // `CompletedRounds.Count`, which is now the retained window and would silently weaken
            // this claim to the last few rounds the moment a walk outran the window. The round the
            // match is on minus one IS every round it finished, which is what the claim is about.
            var completedRounds = host.Projection.RoundIndex - 1;
            Assert.That(completedRounds, Is.GreaterThan(0));
            Assert.That(advances, Is.GreaterThanOrEqualTo(completedRounds * 6),
                "Each completed round crosses all six of p.8's stages, and every crossing is an " +
                "ordinary accepted command in the one recorded stream - which is why replay, " +
                "Undo-prefix replay and snapshot restore see the sequence they always saw.");
        }

        /// <summary>
        /// p.15's Upgrade a Card belongs to the one player whose hero bought the level. A teammate
        /// and an opponent are both refused before the rules are consulted, and neither refusal
        /// moves anything.
        /// </summary>
        [Test]
        public void AnUpgradePauseIsRefusedToATeammateAndAnOpponentAndAcceptedFromItsOwnSeat()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var walked = OwnerScopedProjectionTests.DriveThroughSeats(
                host, 1500, IsPausedOnAnUpgrade);

            Assert.That(IsPausedOnAnUpgrade(host.Projection), Is.True,
                "The seeded seat walk reached no p.15 upgrade pause in " + walked + " commands.");

            var item = UpgradeItems(host.Projection)[0];
            var owner = host.Seats.FindByHero(item.HeroId);
            Assert.That(owner, Is.Not.Null);
            var teammate = host.Seats.Seats.First(seat =>
                seat.TeamSide == owner.TeamSide && !seat.Id.Equals(owner.Id));
            var opponent = host.Seats.Seats.First(seat => seat.TeamSide != owner.TeamSide);

            var answer = host.ProjectionForSeat(owner.Id).AuthorizedCommands.First(command =>
                command.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                command.SystemTransitionChoice.ItemId.Equals(item.ToItemId()));
            var version = host.Version;
            var snapshot = host.CaptureSnapshot();

            var byTeammate = host.SubmitForSeat(teammate.Id, new SeatSubmission(
                new SubmissionId("upgrade-teammate"), version, answer));
            var byOpponent = host.SubmitForSeat(opponent.Id, new SeatSubmission(
                new SubmissionId("upgrade-opponent"), version, answer));

            Assert.That(byTeammate.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.UnauthorizedCommand),
                "Seat " + teammate.Id + " plays for " + owner.TeamSide + " and does not own " +
                item.HeroId.Value + "'s deck, so p.15's question is not theirs to answer.");
            Assert.That(byOpponent.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.UnauthorizedCommand));
            Assert.That(host.Version, Is.EqualTo(version));
            Assert.That(host.CaptureSnapshot(), Is.EqualTo(snapshot));

            var accepted = host.SubmitForSeat(owner.Id, new SeatSubmission(
                new SubmissionId("upgrade-owner"), version, answer));

            Assert.That(accepted.Accepted, Is.True, accepted.Detail);
            Assert.That(host.Version, Is.GreaterThan(version));
        }

        /// <summary>
        /// Milestone 8.35u, and the reason this milestone has a regression fixture of its own rather
        /// than only new behaviour.
        ///
        /// <para>p.15 now asks every hero that bought a level at the same time, so one contract carries
        /// several heroes' answers - and "this seat owns the standing decision" stops being the same
        /// statement as "this seat may submit this command". A teammate who is ALSO being asked passes
        /// the ownership test, because they own a question of their own; if the contract's commands are
        /// not filtered per item, that teammate is handed this hero's upgrades too. That is milestone
        /// 8.35d's live defect exactly, one level up, and its symptom would be identical: leveling
        /// looking skipped for the player whose hero it was about.</para>
        ///
        /// <para>The case only exists when two seats of one side are both awaited, which is why it is
        /// asserted here rather than folded into the fixture above.</para>
        /// </summary>
        [Test]
        public void ASeatBeingAskedItsOwnUpgrade_IsStillRefusedATeammatesOpenItem()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var walked = OwnerScopedProjectionTests.DriveThroughSeats(host, 2500, projection =>
                UpgradeItems(projection)
                    .Select(item => host.Seats.FindByHero(item.HeroId))
                    .Where(seat => seat != null)
                    .GroupBy(seat => seat.TeamSide)
                    .Any(group => group.Count() > 1));

            var awaited = UpgradeItems(host.Projection);
            var pairs = awaited
                .Select(item => new { Item = item, Seat = host.Seats.FindByHero(item.HeroId) })
                .Where(pair => pair.Seat != null)
                .ToArray();
            var sameSide = pairs.GroupBy(pair => pair.Seat.TeamSide)
                .FirstOrDefault(group => group.Count() > 1);
            Assert.That(sameSide, Is.Not.Null,
                "The seeded seat walk reached no round in which two seats of one side were both " +
                "asked p.15's upgrade, in " + walked + " commands. p.8's pity coin makes that the " +
                "ordinary case, so a walk that never finds one is a broken instrument rather than a " +
                "pass.");

            var mine = sameSide.First();
            var theirs = sameSide.Last();
            Assert.That(mine.Seat.Id, Is.Not.EqualTo(theirs.Seat.Id));

            var myCommands = host.ProjectionForSeat(mine.Seat.Id).AuthorizedCommands;
            var theirCommands = host.ProjectionForSeat(theirs.Seat.Id).AuthorizedCommands;

            Assert.That(
                myCommands.Where(command =>
                        command.Kind == DirectorCommandKind.AnswerSystemTransitionStep)
                    .Select(command => command.SystemTransitionChoice.ItemId.Value)
                    .Distinct(),
                Is.EqualTo(new[] { mine.Item.ToItemId().Value }),
                "A seat is offered its OWN hero's upgrade and no other hero's, however many stand.");
            Assert.That(myCommands.Any(command =>
                    command.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                    command.SystemTransitionChoice.ItemId.Equals(theirs.Item.ToItemId())),
                Is.False,
                "Seat " + mine.Seat.Id + " and seat " + theirs.Seat.Id + " play for the same side and " +
                "are both being asked; neither may answer the other's.");
            Assert.That(theirCommands.Any(command =>
                    command.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                    command.SystemTransitionChoice.ItemId.Equals(mine.Item.ToItemId())),
                Is.False);

            // And the host refuses it, not only the projection - a seat that composed the command
            // itself gets the same answer as one that was never offered it.
            var theirAnswer = theirCommands.First(command =>
                command.Kind == DirectorCommandKind.AnswerSystemTransitionStep);
            var version = host.Version;
            var snapshot = host.CaptureSnapshot();
            var stolen = host.SubmitForSeat(mine.Seat.Id, new SeatSubmission(
                new SubmissionId("upgrade-teammates-item"), version, theirAnswer));

            Assert.That(stolen.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.UnauthorizedCommand));
            Assert.That(host.Version, Is.EqualTo(version));
            Assert.That(host.CaptureSnapshot(), Is.EqualTo(snapshot),
                "A refusal before the rules moves nothing at all.");

            // Each of them answers its own, and the transition stands until the last one does.
            var myAnswer = myCommands.First(command =>
                command.Kind == DirectorCommandKind.AnswerSystemTransitionStep);
            var mineAccepted = host.SubmitForSeat(mine.Seat.Id, new SeatSubmission(
                new SubmissionId("upgrade-own-item"), version, myAnswer));
            Assert.That(mineAccepted.Accepted, Is.True, mineAccepted.Detail);
            Assert.That(
                UpgradeItems(host.Projection).Any(item => item.HeroId.Equals(theirs.Item.HeroId)),
                Is.True,
                "One player answering does not answer for the rest; p.15 has them wait for each other.");
        }

        /// <summary>
        /// Milestone 8.35u. The PRIVACY half of the same defect, asserted at every seat scope at once.
        /// </summary>
        /// <remarks>
        /// <para>Filtering <see cref="SeatProjectionFactory.AuthorizedCommands"/> was not enough, and
        /// this is the case that says so. A seat's <c>DecisionContract</c> is read DIRECTLY by the
        /// prompt presenters, so a seat holding the whole contract would be DRAWN every hero's upgrade
        /// options even while the host refused all but its own - the milestone 8.35d symptom at the
        /// surface, and a privacy leak besides, because a p.15 option id names the two cards it moves
        /// and those sit in another hero's hand.</para>
        /// <para>The claim is therefore made about EVERY seat, not one specimen, and about every
        /// surface a seat reads a question through: the contract it is shown, the answers it may
        /// submit, the paused step it is projected, and the description drawn from it. A seat sees its
        /// own hero's options and no other hero's - and a seat that owns none of the open items is
        /// shown no contract at all, rather than an empty one.</para>
        /// </remarks>
        [Test]
        public void AtEverySeatScope_ASeatIsShownOnlyItsOwnHerosUpgradeOptions()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var walked = OwnerScopedProjectionTests.DriveThroughSeats(host, 2500, projection =>
                UpgradeItems(projection).Length > 1);

            var items = UpgradeItems(host.Projection);
            Assert.That(items.Length, Is.GreaterThan(1),
                "The seeded seat walk reached no round in which two heroes were asked p.15's upgrade " +
                "at once, in " + walked + " commands. p.8's pity coin makes that the ordinary case, " +
                "so a walk that never finds one is a broken instrument rather than a pass.");

            var everyOption = host.Projection.PendingSystemTransitionStep.OpenItems
                .ToDictionary(open => open.ItemId, open => open.Options.ToArray());

            foreach (var seat in host.Seats.Seats)
            {
                var view = host.ProjectionForSeat(seat.Id);
                var own = items.Where(item => seat.Owns(item.HeroId)).ToArray();
                var foreignOptions = everyOption
                    .Where(pair => !own.Any(item => item.ToItemId().Equals(pair.Key)))
                    .SelectMany(pair => pair.Value)
                    .ToArray();

                if (own.Length == 0)
                {
                    Assert.That(view.DecisionContract, Is.Null,
                        "Seat " + seat.Id + " is being asked nothing and is shown no contract.");
                    Assert.That(view.PendingSystemTransitionStep, Is.Null);
                    Assert.That(view.PendingSystemTransitionDescription, Is.Null);
                    Assert.That(view.AuthorizedCommands, Is.Empty);
                    continue;
                }

                var ownItemIds = own.Select(item => item.ToItemId()).ToArray();

                // 1. The contract it is shown.
                Assert.That(view.DecisionContract, Is.Not.Null);
                Assert.That(
                    view.DecisionContract.Commands
                        .Where(command => command.Kind == DirectorCommandKind.AnswerSystemTransitionStep)
                        .Select(command => command.SystemTransitionChoice.ItemId)
                        .Distinct(),
                    Is.EquivalentTo(ownItemIds),
                    "Seat " + seat.Id + " is DRAWN an upgrade prompt for a hero that is not its own.");
                Assert.That(
                    view.DecisionContract.Commands.Any(command =>
                        foreignOptions.Contains(command.SystemTransitionChoice.OptionId)),
                    Is.False,
                    "A p.15 option names the two cards it moves, and those are in another hero's hand.");

                // 2. The answers it may submit.
                Assert.That(
                    view.AuthorizedCommands
                        .Where(command => command.Kind == DirectorCommandKind.AnswerSystemTransitionStep)
                        .Select(command => command.SystemTransitionChoice.ItemId)
                        .Distinct(),
                    Is.EquivalentTo(ownItemIds));

                // 3. The paused step it is projected - narrowed, so another hero's options are ABSENT
                //    rather than present and unrendered. That is what F-ER2-07 means by a separate
                //    read model, and it is why this asserts the step and not only the contract.
                var step = view.PendingSystemTransitionStep;
                Assert.That(step, Is.Not.Null);
                Assert.That(step.OpenItems.Select(open => open.ItemId), Is.EquivalentTo(ownItemIds));
                Assert.That(
                    step.OpenItems.SelectMany(open => open.Options).Any(foreignOptions.Contains),
                    Is.False);
                Assert.That(step.Invocations.Any(invocation =>
                    foreignOptions.Contains(invocation.OptionId)), Is.False,
                    "Invocations is the one member every prompt presenter reads, so a narrowed step " +
                    "whose invocations still named another hero's answers would undo the narrowing " +
                    "one property access away.");

                // 4. The description drawn from it, which CARRIES its own pause - so a seat handed the
                //    Director's description would reach every hero's answers through it.
                var description = view.PendingSystemTransitionDescription;
                if (description != null)
                {
                    Assert.That(ownItemIds, Has.Member(description.ItemId));
                    Assert.That(
                        description.Options.Any(option => foreignOptions.Contains(option.OptionId)),
                        Is.False);
                    Assert.That(
                        description.PendingStep.Invocations.Any(invocation =>
                            foreignOptions.Contains(invocation.OptionId)),
                        Is.False,
                        "The description travels with a pause; that pause must be the narrowed one.");
                }
            }
        }

        /// <summary>
        /// Every upgrade pause a whole round produces is owned by exactly one seat - its own
        /// hero's - and both teams pause, so the ownership rule is exercised on each side rather
        /// than on one specimen.
        /// </summary>
        [Test]
        public void EveryUpgradePauseOfARoundIsOwnedByExactlyTheSeatWhoseHeroItNames()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var paused = new List<HeroId>();
            var sides = new HashSet<TeamSide>();

            OwnerScopedProjectionTests.DriveThroughSeats(host, 1500, projection =>
            {
                if (!IsPausedOnAnUpgrade(projection))
                {
                    return false;
                }

                // Milestone 8.35u. The same rule, once per open item, because p.15 asks every hero
                // that bought a level at the same time.
                var items = UpgradeItems(projection);
                var declared = SeatProjectionFactory.OwnerOfStandingDecision(host.Projection);
                Assert.That(declared.IsTeamOwned, Is.False,
                    "p.15's upgrade is one player's own decision about their own deck.");
                Assert.That(declared.HeroIds, Is.EquivalentTo(items.Select(value => value.HeroId)),
                    "Who is awaited is public and is every hero still choosing, not one of them.");

                foreach (var item in items)
                {
                    var owner = host.Seats.FindByHero(item.HeroId);
                    Assert.That(owner, Is.Not.Null,
                        "A pause whose hero no seat owns could be answered by nobody.");

                    var authorized = host.Seats.Seats
                        .Where(seat => host.ProjectionForSeat(seat.Id).AuthorizedCommands.Any(command =>
                            command.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                            command.SystemTransitionChoice.ItemId.Equals(item.ToItemId())))
                        .ToArray();
                    Assert.That(authorized.Select(seat => seat.Id.Value), Is.EqualTo(
                        new[] { owner.Id.Value }),
                        "Exactly the seat whose hero the item names holds p.15's answer to it.");

                    if (!paused.Contains(item.HeroId))
                    {
                        paused.Add(item.HeroId);
                    }

                    sides.Add(owner.TeamSide);
                }

                return false;
            });

            Assert.That(paused.Count, Is.GreaterThan(1),
                "The walk found " + paused.Count + " levelling heroes, which is too few to show " +
                "that each answers its own.");
            Assert.That(sides, Is.EquivalentTo(new[] { TeamSide.Red, TeamSide.Blue }),
                "Both teams level, so the rule is proved on each side rather than on one.");
        }

        /// <summary>
        /// The auto-skip half, which already held and is now asserted: a hero that can afford no
        /// level is passed over silently - it is named by no pause and by no completed item - while
        /// the hero that can afford one stops the round on its own question.
        /// </summary>
        [Test]
        public void AHeroThatCanAffordNoLevelIsPassedOverWithNoPauseAndNoItemOfItsOwn()
        {
            var session = ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(
                out var levellingHeroId);
            var pending = session.Projection.PendingSystemTransitionStep;
            var idleHeroId = session.Projection.Setup.Heroes
                .Single(hero => !hero.HeroId.Equals(levellingHeroId)).HeroId;

            Assert.That(session.Projection.HeroProgression
                    .Single(progression => progression.HeroId.Equals(idleHeroId)).Coins,
                Is.Zero,
                "The hero that collected nothing is what makes this case about the skip.");
            Assert.That(HeroLevelUpItem.Read(pending.ItemId).HeroId, Is.EqualTo(levellingHeroId));
            Assert.That(
                pending.CompletedItemIds.Select(HeroLevelUpItem.Read)
                    .Any(item => item.HeroId.Equals(idleHeroId)),
                Is.False,
                "p.15 passes over a hero with nothing to buy without recording an item for it, " +
                "so nobody is ever asked a question that hero does not have.");
        }

        /// <summary>
        /// The Director's own direct surface is untouched: its door does not continue the round,
        /// and it still advances a stage by hand.
        /// </summary>
        [Test]
        public void TheDirectorsOwnDoorStillStepsTheRoundByHand()
        {
            var session = ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(out _);
            var host = new MatchHost(session);
            var answer = DirectorCommand.AnswerSystemTransitionStep(
                session.Projection.PendingSystemTransitionStep.Invocations[0]);

            var answered = host.Submit(MatchSubmission.ForCommand(answer));

            Assert.That(answered.Accepted, Is.True, answered.Detail);
            Assert.That(host.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound),
                "p.8 still has stages left, and the Director's door ran none of them by itself.");
            Assert.That(host.Projection.DecisionContract, Is.Null);

            var advanced = host.Submit(
                MatchSubmission.ForCommand(DirectorCommand.AdvanceEndOfRound()));

            Assert.That(advanced.Accepted, Is.True, advanced.Detail);
            Assert.That(((MatchHost)host).EndOfRoundContinuationRefusals, Is.Empty);
        }

        /// <summary>
        /// The same guarantee across the wire, on six real client machines: the round crosses end
        /// of round with no seat submitting an advance, and every seat holds the settled version
        /// afterwards rather than the one the submission alone produced.
        /// </summary>
        [Test]
        public void SixWireClientsCrossEndOfRoundWithoutAnAdvanceAndConvergeOnTheSettledVersion()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var completedRounds = 0;
            var crossings = 0;
            var advanceOffers = 0;

            var walked = harness.DriveThroughWire(400, null, () =>
            {
                foreach (var seat in harness.MatchHost.Seats.Seats)
                {
                    if (harness.MatchHost.ProjectionForSeat(seat.Id).AuthorizedCommands.Any(
                            command => command.Kind == DirectorCommandKind.AdvanceEndOfRound))
                    {
                        advanceOffers++;
                    }
                }

                // Milestone 8.35w. The round-boundary edge is detected from the round
                // index, not from `CompletedRounds.Count`: the count is now the retained window and
                // stops rising once the window saturates, so a longer walk would stop seeing
                // crossings that are still happening.
                if (harness.MatchHost.Projection.RoundIndex - 1 <= completedRounds)
                {
                    return;
                }

                completedRounds = harness.MatchHost.Projection.RoundIndex - 1;
                crossings++;
                harness.AssertConverged();
            });

            Assert.That(crossings, Is.GreaterThan(0),
                "The wire walk completed no round in " + walked + " commands.");
            Assert.That(advanceOffers, Is.Zero,
                "No wire client was published an advance it could submit.");
            harness.AssertConverged();
        }

        /// <summary>
        /// Milestone 8.35u widened this from "the item" to "any open item": p.15 asks every hero that
        /// bought a level at once, so a pause is on an upgrade when one of the questions it is holding
        /// is one.
        /// </summary>
        private static bool IsPausedOnAnUpgrade(IMatchReadModel projection) =>
            UpgradeItems(projection).Length > 0;

        /// <summary>Every open p.15 upgrade of the standing pause, in the pause's own order.</summary>
        private static HeroLevelUpItem[] UpgradeItems(IMatchReadModel projection)
        {
            var step = projection.PendingSystemTransitionStep;
            if (step == null || !step.DefinitionId.Equals(HeroLevelUpSystemTransition.DefinitionId))
            {
                return Array.Empty<HeroLevelUpItem>();
            }

            return step.OpenItems
                .Where(open => open.ItemId.IsDefined)
                .Select(open => HeroLevelUpItem.Read(open.ItemId))
                .Where(item => item.Kind == HeroLevelUpItemKind.Upgrade)
                .ToArray();
        }
    }
}
