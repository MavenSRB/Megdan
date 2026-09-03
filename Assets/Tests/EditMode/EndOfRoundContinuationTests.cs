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
            Assert.That(advances, Is.GreaterThanOrEqualTo(
                host.Projection.CompletedRounds.Count * 6),
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

            var item = HeroLevelUpItem.Read(host.Projection.PendingSystemTransitionStep.ItemId);
            var owner = host.Seats.FindByHero(item.HeroId);
            Assert.That(owner, Is.Not.Null);
            var teammate = host.Seats.Seats.First(seat =>
                seat.TeamSide == owner.TeamSide && !seat.Id.Equals(owner.Id));
            var opponent = host.Seats.Seats.First(seat => seat.TeamSide != owner.TeamSide);

            var answer = host.ProjectionForSeat(owner.Id).AuthorizedCommands.First(command =>
                command.Kind == DirectorCommandKind.AnswerSystemTransitionStep);
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

                var item = HeroLevelUpItem.Read(projection.PendingSystemTransitionStep.ItemId);
                var owner = host.Seats.FindByHero(item.HeroId);
                Assert.That(owner, Is.Not.Null,
                    "A pause whose hero no seat owns could be answered by nobody.");
                var declared = SeatProjectionFactory.OwnerOfStandingDecision(host.Projection);
                Assert.That(declared.IsTeamOwned, Is.False,
                    "p.15's upgrade is one player's own decision about their own deck.");
                Assert.That(declared.HeroId, Is.EqualTo(item.HeroId));

                var authorized = host.Seats.Seats
                    .Where(seat => host.ProjectionForSeat(seat.Id).AuthorizedCommands.Any(command =>
                        command.Kind == DirectorCommandKind.AnswerSystemTransitionStep))
                    .ToArray();
                Assert.That(authorized.Select(seat => seat.Id.Value), Is.EqualTo(
                    new[] { owner.Id.Value }),
                    "Exactly the seat whose hero the item names holds p.15's answer.");

                if (!paused.Contains(item.HeroId))
                {
                    paused.Add(item.HeroId);
                }

                sides.Add(owner.TeamSide);
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

                if (harness.MatchHost.Projection.CompletedRounds.Count <= completedRounds)
                {
                    return;
                }

                completedRounds = harness.MatchHost.Projection.CompletedRounds.Count;
                crossings++;
                harness.AssertConverged();
            });

            Assert.That(crossings, Is.GreaterThan(0),
                "The wire walk completed no round in " + walked + " commands.");
            Assert.That(advanceOffers, Is.Zero,
                "No wire client was published an advance it could submit.");
            harness.AssertConverged();
        }

        private static bool IsPausedOnAnUpgrade(IMatchReadModel projection)
        {
            var step = projection.PendingSystemTransitionStep;
            return step != null &&
                step.DefinitionId.Equals(HeroLevelUpSystemTransition.DefinitionId) &&
                step.ItemId.IsDefined &&
                HeroLevelUpItem.Read(step.ItemId).Kind == HeroLevelUpItemKind.Upgrade;
        }
    }
}
