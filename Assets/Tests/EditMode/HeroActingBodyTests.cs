using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.21b: "which of you acts" (RZ-Q4, settled by the user as (A)). A hero standing on
    /// several spaces picks the acting body when his card resolves, before the action is chosen; the
    /// body stays fixed for the whole action; a Defense is the attacked body's and asks nothing; one
    /// standing body asks nothing. Proven on 9.21a's staged two-body board (<see cref="HeroBodiesBoard"/>);
    /// no Razzle content is registered.
    /// </summary>
    public sealed class HeroActingBodyTests
    {
        [Test]
        public void TwoBodiesStanding_AskWhichOfHimActs_BeforeAnyActionOfTheCard()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheActingBodyPick(out var red, out var blue);
            var projection = session.Projection;
            var choice = projection.PendingActingBodyChoice;

            Assert.That(choice.HeroId, Is.EqualTo(blue));
            Assert.That(choice.CardInstanceId, Is.EqualTo(projection.SelectedActor.CardInstanceId));
            Assert.That(choice.Bodies.Select(body => body.Body), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(projection.DecisionContract.FamilyId, Is.EqualTo(PendingDecisionFamilies.ActingBody));
            Assert.That(projection.DecisionContract.Commands, Is.EqualTo(new[]
            {
                DirectorCommand.ChooseActingBody(new ActingBodyCommand(blue, choice.CardInstanceId, 1)),
                DirectorCommand.ChooseActingBody(new ActingBodyCommand(blue, choice.CardInstanceId, 2))
            }), "One answer per standing body, in body order, each naming its body.");

            Assert.That(projection.AvailableTurnActions, Is.Empty,
                "The pick precedes the action choice (RZ-Q4), so no action is offered yet.");
            Assert.That(projection.AvailableHoldOffer, Is.Null, "Hold is an action of the card too.");
            Assert.That(projection.AvailableFastTravelOptions, Is.Empty);
            Assert.That(projection.AvailableClearOptions, Is.Empty);

            var before = session.CaptureSnapshot();
            var hold = session.Execute(DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId));
            Assert.That(hold.Accepted, Is.False, "No action of the card starts before the pick.");
            Assert.That(hold.TurnRejectionReason, Is.EqualTo(TurnResolutionRejectionReason.ActingBodyChoicePending));
            var absent = session.Execute(DirectorCommand.ChooseActingBody(
                new ActingBodyCommand(blue, choice.CardInstanceId, 3)));
            Assert.That(absent.Accepted, Is.False, "Body 3 does not stand.");
            var otherCard = session.Execute(DirectorCommand.ChooseActingBody(
                new ActingBodyCommand(blue, new CardInstanceId("not-the-resolving-card"), 1)));
            Assert.That(otherCard.Accepted, Is.False);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(before), "Every refusal is atomic.");

            // Projected to the owning seat only, and authorized only there.
            var host = new MatchHost(session);
            foreach (var seat in host.Seats.Seats)
            {
                var view = host.ProjectionForSeat(seat.Id);
                Assert.That(view.DecisionOwner.FamilyId, Is.EqualTo(PendingDecisionFamilies.ActingBody));
                Assert.That(view.DecisionOwner.HeroIds, Is.EqualTo(new[] { blue }));
                Assert.That(view.AuthorizedCommands,
                    seat.HeroId.Equals(blue)
                        ? Is.EqualTo(projection.DecisionContract.Commands)
                        : Is.Empty,
                    seat.HeroId + ": only the hero whose card resolved answers the pick.");
            }

            // Answered: the pick is gone, the chosen body is settled, and actions are offered.
            HeroBodiesBoard.Accept(session, projection.DecisionContract.Commands[1]);
            Assert.That(session.Projection.PendingActingBodyChoice, Is.Null);
            Assert.That(session.Projection.DecisionContract, Is.Null);
            Assert.That(session.Projection.AvailableTurnActions, Is.Not.Empty);
            Assert.That(session.Projection.AvailableHoldOffer, Is.Not.Null);
            Assert.That(red, Is.Not.EqualTo(blue));
        }

        [Test]
        public void TheOffers_AreTheChosenBodys_AnAttackFromOneBodyAndNotTheOther()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheActingBodyPick(out var red, out var blue, bodyTwoAtSpawn: true);
            var atPick = session.CaptureSnapshot();
            var card = session.Projection.SelectedActor.CardInstanceId;

            HeroBodiesBoard.Accept(session, Pick(blue, card, 1));
            var fromBodyOne = AttackOffer(session);
            Assert.That(fromBodyOne.IsAvailable, Is.True,
                "Body 1 stands beside the Red hero, so its Basic Attack has a target.");
            Assert.That(session.Projection.AvailableFastTravelOptions, Is.Empty,
                "Body 1 stands in a Zone with enemies, so p.11's Fast Travel is not offered from it.");

            Assert.That(session.RestoreSnapshot(atPick).Accepted, Is.True);
            HeroBodiesBoard.Accept(session, Pick(blue, card, 2));
            var fromBodyTwo = AttackOffer(session);
            Assert.That(fromBodyTwo.ActionDefinitionId, Is.EqualTo(fromBodyOne.ActionDefinitionId));
            Assert.That(fromBodyTwo.IsAvailable, Is.False,
                "Body 2 stands on his spawn, out of reach of every Red unit (lesson 23).");
            Assert.That(session.Projection.AvailableFastTravelOptions, Is.Not.Empty,
                "Body 2's Zone holds no enemy, so Fast Travel is offered from it.");

            // The Attack answered from body 1 reaches the Red hero; started from body 2 it is refused.
            var refused = session.Execute(DirectorCommand.StartAction(
                fromBodyTwo.SourceCardInstanceId, fromBodyTwo.ActionDefinitionId));
            Assert.That(refused.Accepted, Is.False, "An offer greyed for body 2 is refused for body 2.");
            Assert.That(session.RestoreSnapshot(atPick).Accepted, Is.True);
            HeroBodiesBoard.Accept(session, Pick(blue, card, 1));
            HeroBodiesBoard.Accept(session, DirectorCommand.StartAction(
                fromBodyOne.SourceCardInstanceId, fromBodyOne.ActionDefinitionId));
            Assert.That(session.Projection.PendingAttackTargetChoice.LegalTargetHeroIds, Is.EqualTo(new[] { red }));
        }

        [Test]
        public void MovementAndFastTravel_MoveTheChosenBody_AndLeaveTheOther()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheActingBodyPick(out _, out var blue, bodyTwoAtSpawn: true);
            var atPick = session.CaptureSnapshot();
            var card = session.Projection.SelectedActor.CardInstanceId;
            var bodyOneAt = BodyAt(session, blue, 1);
            var bodyTwoAt = BodyAt(session, blue, 2);

            // The secondary Movement, from body 2.
            HeroBodiesBoard.Accept(session, Pick(blue, card, 2));
            var movement = OfferOfKind(session, ActionKind.Movement);
            HeroBodiesBoard.Accept(session, DirectorCommand.StartAction(
                movement.SourceCardInstanceId, movement.ActionDefinitionId));
            var destinations = session.Projection.PendingMovementChoice.LegalDestinations
                .Select(value => value.Destination).ToArray();
            Assert.That(destinations, Is.Not.Empty);
            Assert.That(destinations.Any(at => HexDistanceQuery.Calculate(at, bodyTwoAt) == 1), Is.True);
            Assert.That(destinations.All(at =>
                    HexDistanceQuery.Calculate(at, bodyTwoAt) < HexDistanceQuery.Calculate(at, bodyOneAt)),
                Is.True, "The Movement is measured from body 2: every destination lies on its side.");
            var step = destinations.First(at => HexDistanceQuery.Calculate(at, bodyTwoAt) == 1);
            var choice = session.Projection.PendingMovementChoice;
            HeroBodiesBoard.Accept(session, DirectorCommand.ChooseMovementDestination(
                choice.ResolutionId, choice.ChoiceId, step));
            Assert.That(BodyAt(session, blue, 2), Is.EqualTo(step), "Body 2 moved.");
            Assert.That(BodyAt(session, blue, 1), Is.EqualTo(bodyOneAt), "Body 1 stayed.");

            // Fast Travel, from body 2.
            Assert.That(session.RestoreSnapshot(atPick).Accepted, Is.True);
            HeroBodiesBoard.Accept(session, Pick(blue, card, 2));
            var travel = session.Projection.AvailableFastTravelOptions.First();
            var destination = travel.LegalDestinations.First();
            HeroBodiesBoard.Accept(session, DirectorCommand.PerformFastTravel(
                travel.ReplacedMovementActionDefinitionId, destination));
            Assert.That(BodyAt(session, blue, 2), Is.EqualTo(destination), "Body 2 travelled.");
            Assert.That(BodyAt(session, blue, 1), Is.EqualTo(bodyOneAt), "Body 1 stayed.");
        }

        [Test]
        public void ADefense_IsTheAttackedBodys_AndAsksNoPick()
        {
            // Red acts first and attacks body 2; the Defense question stands on Blue without a pick.
            var session = HeroBodiesBoard.CreateSessionAtTheDefenseOfBodyTwo();
            Assert.That(session.Projection.PendingActingBodyChoice, Is.Null);
            Assert.That(session.Projection.DecisionContract.FamilyId, Is.EqualTo(PendingDecisionFamilies.Defense));
            Assert.That(session.Projection.SelectedActor.ActorId, Is.Not.EqualTo(
                session.Projection.PendingDefenseChoice.ChooserId));
        }

        [Test]
        public void OneBodyStanding_AsksNoPick_AndThatBodyActs()
        {
            // A single-bodied hero's turn is never asked (Red acts first here).
            var session = HeroBodiesBoard.CreateSessionAtTheAttackTargetChoice(out var red, out var blue, out _);
            Assert.That(session.Projection.PendingActingBodyChoice, Is.Null);

            // Red defeats Blue through body 2, and every body leaves the board (RZ-Q3).
            var target = session.Projection.PendingAttackTargetChoice;
            HeroBodiesBoard.Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, blue, 2));
            var defense = session.Projection.PendingDefenseChoice;
            HeroBodiesBoard.Accept(session, DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));
            HeroBodiesBoard.Accept(session, DirectorCommand.ReleaseTurn());

            // Next turn Blue respawns as one body of his four; that body acts, and nothing is asked.
            HeroBodiesBoard.CommitNextTurn(session, red, blue);
            HeroBodiesBoard.Accept(session, DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId));
            if (session.Projection.PendingHeroRespawnChoice == null)
            {
                HeroBodiesBoard.Accept(session, DirectorCommand.ReleaseTurn());
            }

            var respawn = session.Projection.PendingHeroRespawnChoice;
            Assert.That(respawn, Is.Not.Null);
            Assert.That(session.Projection.PendingActingBodyChoice, Is.Null, "No body stands to pick.");
            HeroBodiesBoard.Accept(session, DirectorCommand.RespawnHero(respawn.RespawnInvocations[0]));
            Assert.That(session.Projection.Heroes.Count(hero => hero.HeroId.Equals(blue)), Is.EqualTo(1));
            Assert.That(session.Projection.PendingActingBodyChoice, Is.Null,
                "RZ-Q4's pick stands only while more than one body stands.");
            Assert.That(session.Projection.DecisionContract, Is.Null);
            Assert.That(session.Projection.AvailableTurnActions.Any(offer => offer.IsAvailable), Is.True,
                "The one body acts at once.");
        }

        [Test]
        public void ThePick_RestoresReplaysAndRewinds_AndTheActingBodyIsPersisted()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheActingBodyPick(out _, out var blue, bodyTwoAtSpawn: true);
            var atPick = session.CaptureSnapshot();
            var card = session.Projection.SelectedActor.CardInstanceId;

            // Restore at the pending pick.
            var restored = HeroBodiesBoard.CreateSession(out _, out _, out _, bodyTwoAtSpawn: true);
            Assert.That(restored.RestoreSnapshot(atPick).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(atPick));
            Assert.That(restored.Projection.DecisionContract.Commands,
                Is.EqualTo(session.Projection.DecisionContract.Commands));

            // Answer, then restore after the answer: the acting body travels with the snapshot.
            HeroBodiesBoard.Accept(session, Pick(blue, card, 2));
            var afterPick = session.CaptureSnapshot();
            Assert.That(afterPick, Is.Not.EqualTo(atPick), "The settled acting body is state.");
            Assert.That(restored.RestoreSnapshot(afterPick).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(afterPick));
            Assert.That(restored.Projection.PendingActingBodyChoice, Is.Null, "The pick is not asked again.");
            Assert.That(restored.Projection.AvailableTurnActions, Is.EqualTo(session.Projection.AvailableTurnActions));
            Assert.That(restored.Projection.AvailableFastTravelOptions.Count,
                Is.EqualTo(session.Projection.AvailableFastTravelOptions.Count),
                "The restored action is body 2's, as the answered one is.");

            // Replay from the start converges.
            var replayed = HeroBodiesBoard.CreateSession(out _, out _, out _, bodyTwoAtSpawn: true);
            DeterministicMatchLoop.Replay(replayed, session.Projection.AcceptedCommands.ToArray());
            Assert.That(replayed.CaptureSnapshot(), Is.EqualTo(afterPick));

            // Undo gives the pick back.
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(atPick));
            Assert.That(session.Projection.PendingActingBodyChoice, Is.Not.Null);

            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(50));
        }

        [Test]
        public void TheSeatsRewind_ReachesBackToThePick_AndNoFurther()
        {
            var session = HeroBodiesBoard.CreateSession(out var red, out var blue, out _, bodyTwoAtSpawn: true);
            var host = new MatchHost(session);
            HeroBodiesBoard.CommitBlueFirst(session, red, blue, command =>
            {
                var accepted = host.Submit(MatchSubmission.ForCommand(command));
                Assert.That(accepted.Accepted, Is.True, command.Kind + ": " + accepted.Detail);
            });
            Assert.That(host.Projection.PendingActingBodyChoice, Is.Not.Null);
            var seat = host.Seats.FindByHero(blue);
            var atPick = host.CaptureSnapshot();

            Submit(host, seat.Id, host.ProjectionForSeat(seat.Id).AuthorizedCommands[1]);
            var movement = OfferOfKind(host.Projection, ActionKind.Movement);
            Submit(host, seat.Id, DirectorCommand.StartAction(
                movement.SourceCardInstanceId, movement.ActionDefinitionId));

            var rewinds = 0;
            while (host.ProjectionForSeat(seat.Id).IsSeatUndoAvailable && rewinds < 10)
            {
                var result = host.SubmitUndoForSeat(seat.Id, new SeatUndoSubmission(
                    new SubmissionId("rewind-" + rewinds), host.ProjectionForSeat(seat.Id).Version));
                Assert.That(result.Accepted, Is.True, result.Detail);
                rewinds++;
            }

            Assert.That(rewinds, Is.EqualTo(2), "The Movement's start and the pick are the seat's own to take back.");
            Assert.That(host.CaptureSnapshot(), Is.EqualTo(atPick), "The rewind stops at the pick, not before it.");
            Assert.That(host.Projection.PendingActingBodyChoice, Is.Not.Null);
        }

        [Test]
        public void TheWire_CarriesThePick_ToTheOwningSeat()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheActingBodyPick(out _, out var blue);
            var host = new MatchHost(session);
            foreach (var seat in host.Seats.Seats)
            {
                var view = host.ProjectionForSeat(seat.Id);
                Assert.That(NetworkSeatProjectionCodec.TryDecode(
                    NetworkSeatProjectionCodec.Encode(view), out var record), Is.True);
                var client = new NetworkMatchReadModel(
                    host.Projection, MatchContentCatalogs.ForThisBuild(), record, true);
                Assert.That(client.DecisionOwner.FamilyId, Is.EqualTo(PendingDecisionFamilies.ActingBody));
                Assert.That(client.DecisionOwner.HeroIds, Is.EqualTo(new[] { blue }));
                if (seat.HeroId.Equals(blue))
                {
                    Assert.That(client.DecisionContract.FamilyId, Is.EqualTo(PendingDecisionFamilies.ActingBody));
                    Assert.That(client.DecisionContract.Commands, Is.EqualTo(view.DecisionContract.Commands));
                    Assert.That(client.DecisionContract.Commands.Select(command => command.HeroBody),
                        Is.EqualTo(new[] { 1, 2 }));
                }

                Assert.That(client.Heroes.Where(hero => hero.HeroId.Equals(blue)).Select(hero => hero.Body),
                    Is.EqualTo(new[] { 1, 2 }));
            }

            var command = session.Projection.DecisionContract.Commands[1];
            Assert.That(NetworkCommandCodec.TryDecode(NetworkCommandCodec.Encode(command), out var decoded), Is.True);
            Assert.That(decoded, Is.EqualTo(command));
            Assert.That(decoded.HeroBody, Is.EqualTo(2));
            Assert.That(PrivateSessionProtocol.Version, Is.EqualTo(27));
        }

        [Test]
        public void TheBot_PicksTheBodyNearestTheFight_ForEverySeed()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheActingBodyPick(out _, out var blue, bodyTwoAtSpawn: true);
            var host = new MatchHost(session);
            var seat = host.Seats.FindByHero(blue);
            var view = host.ProjectionForSeat(seat.Id);
            foreach (var seed in Enumerable.Range(1, 8))
            {
                var picked = BotPolicy.SelectPrivateSessionTactics(
                    view.AuthorizedCommands, view, blue, new DeterministicBotRandom(seed));
                Assert.That(picked.Kind, Is.EqualTo(DirectorCommandKind.ChooseActingBody));
                Assert.That(picked.HeroBody, Is.EqualTo(1),
                    "Seed " + seed + ": body 1 stands beside the Red hero and its Attack is the offer the pick opens.");
            }
        }

        // -------------------------------------------------------------------------------------------

        private static DirectorCommand Pick(HeroId hero, CardInstanceId card, int body) =>
            DirectorCommand.ChooseActingBody(new ActingBodyCommand(hero, card, body));

        private static HexCoordinate BodyAt(MatchSession session, HeroId hero, int body) =>
            session.Projection.Heroes.Single(value => value.HeroId.Equals(hero) && value.Body == body).Coordinate;

        private static ActionOffer AttackOffer(MatchSession session) => OfferOfKind(session.Projection, ActionKind.Attack);

        private static ActionOffer OfferOfKind(MatchSession session, ActionKind kind) =>
            OfferOfKind(session.Projection, kind);

        private static ActionOffer OfferOfKind(MatchSessionProjection projection, ActionKind kind) =>
            projection.AvailableTurnActions.Single(offer =>
                projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == kind);

        private static void Submit(MatchHost host, SeatId seat, DirectorCommand command)
        {
            var result = host.SubmitForSeat(seat, new SeatSubmission(
                new SubmissionId("submit-" + host.Projection.Version), host.ProjectionForSeat(seat).Version, command));
            Assert.That(result.Accepted, Is.True, command.Kind + ": " + result.Detail);
        }
    }
}
