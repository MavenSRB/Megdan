using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35p. Brogan's three charges in the order their sentence prints: "Move N spaces in
    /// a straight line to a space adjacent to an enemy unit, THEN target that unit."
    ///
    /// <para>The user's words, from live multiplayer testing on 2026-09-04: "Brogan should move for
    /// his charge before the attack resolves ... They move as soon as they select, and then they
    /// attack." Until this milestone both questions were asked inside one capability node placed
    /// before the Move node, so a player named the victim while the hero authoritatively still stood
    /// on his starting space and every board correctly drew him there.</para>
    ///
    /// <para>Unity-free on purpose, so the harness and the Edit Mode assembly both run it.</para>
    /// </summary>
    public sealed class ChargeSequencingTests
    {
        /// <summary>Two spaces straight from the staged hero, with the enemy one further on.</summary>
        private static readonly HexCoordinate Landing = new HexCoordinate(-1, 1);
        private static readonly HexCoordinate EnemyBeyondTheLanding = new HexCoordinate(0, 1);

        /// <summary>
        /// The whole of the user's rule in one case: answering where he lands moves him, and the
        /// question of whom he takes is asked from the space he is now standing on.
        ///
        /// <para>The two facts are read at the same moment deliberately. The destination answer and
        /// the Move land in the SAME accepted command - the authority runs the effect on until the
        /// next pause - so the state a player is shown when the target question opens already has
        /// the hero at his destination. There is no extra click, and no state in which the board
        /// shows him at the origin with the victim question standing.</para>
        /// </summary>
        [Test]
        public void AnsweringWhereHeLandsMovesHim_AndOnlyThenIsTheVictimAsked()
        {
            var board = ChargeBoard();
            board.StartPrimary(BroganCardContent.MadDash);

            var destinations = board.Choice;
            Assert.That(destinations, Is.Not.Null, "The charge opens by asking where he lands.");
            Assert.That(destinations.NodeId.Value, Is.EqualTo("charge-destination"));
            Assert.That(board.CandidateSpaces(destinations), Contains.Item(Landing));
            Assert.That(board.HeroSpace(board.Hero), Is.EqualTo(StagedHeroBoard.HeroAt),
                "Nothing has moved yet: the destination question is asked from where he stands.");

            board.Select(Landing);
            board.ConfirmIfStillOwed();

            Assert.That(board.HeroSpace(board.Hero), Is.EqualTo(Landing),
                "The Move commits on the destination answer, in that same command.");

            var victims = board.Choice;
            Assert.That(victims, Is.Not.Null,
                "The victim question is already standing, so the answer and the move cost one click.");
            Assert.That(victims.NodeId.Value, Is.EqualTo("charge-target"));
            var enemySide = board.SideOf(board.MinionAt(EnemyBeyondTheLanding));
            var besideTheLanding = board.Session.Projection.Minions
                .Where(minion => minion.TeamSide == enemySide &&
                    Distance(minion.Coordinate, Landing) <= 1)
                .Select(minion => minion.MinionId)
                .ToArray();
            Assert.That(besideTheLanding, Contains.Item(board.MinionAt(EnemyBeyondTheLanding)));
            Assert.That(
                board.CandidateMinions(victims), Is.EquivalentTo(besideTheLanding),
                "The units offered are every enemy beside where he is NOW, which is the same set " +
                "the old single node measured from the destination he had just named.");

            board.SelectAndConfirm(board.MinionAt(EnemyBeyondTheLanding));

            Assert.That(
                board.SequenceOfAppliedNode("charge-move"),
                Is.GreaterThan(board.SequenceOfAppliedNode("charge-destination")),
                "The Move runs after the destination is named.");
            Assert.That(
                board.SequenceOfAppliedNode("charge-target"),
                Is.GreaterThan(board.SequenceOfAppliedNode("charge-move")),
                "And the victim is named after the Move, which is the whole of this correction.");
        }

        /// <summary>
        /// The offer survives the reorder, and this is the case that proves it - because the first
        /// attempt at the reorder failed here and was measured failing.
        ///
        /// <para>Rulebook p.14's target for these faces is bound by the card text, so the Attack is
        /// offered only where that text can reach a binding. The walk that decides this stands before
        /// any of the text has run, and once "whom do I take" became an ordinary adjacency question
        /// it answered "nobody" - because the hero has not moved yet. Mad Dash was greyed in every
        /// position where the enemy was not ALREADY adjacent, which is precisely the position a
        /// charge is for. <c>EffectOptionFeasibilityQuery</c> now declines to judge a board reading
        /// that stands behind a Move it has not taken.</para>
        /// </summary>
        [Test]
        public void AChargeIsOfferedWhenTheEnemyItCanReachIsNotAlreadyBesideHim()
        {
            var board = ChargeBoard();

            Assert.That(
                board.Session.Projection.Minions
                    .Where(minion => Distance(minion.Coordinate, StagedHeroBoard.HeroAt) <= 1)
                    .Select(minion => minion.MinionId.Value),
                Is.Empty,
                "Nothing stands beside him, or this case is about a board the charge is not for.");
            Assert.That(board.IsPrimaryAvailable(BroganCardContent.MadDash), Is.True,
                "The charge reaches a straight destination beside an enemy, so p.14's card-text " +
                "binding is reachable and the Attack is offered.");
        }

        /// <summary>
        /// Milestone 8.35p's second half, and the pre-existing gap it closes. The seat's own rewind
        /// is walled above an Attack whose target has been chosen; a card that binds its target in
        /// its own text never emits <c>ChooseAttackTarget</c>, so that wall never rose for this whole
        /// class and a driven charge measured the consequence at the previous revision - the acting
        /// seat's rewind was OFFERED and ACCEPTED on a fully resolved Attack.
        ///
        /// <para>Driven rather than waited for, which is milestone 8.35f's own recorded lesson: a
        /// state a random walk passes through cannot be tested by waiting for the walk to stop in
        /// it.</para>
        /// </summary>
        [Test]
        public void ACardTextBoundAttackWallsItsOwnSeatsRewindOnceTheVictimIsNamed()
        {
            var staged = ChargeBoard();
            var host = MatchHost.Compose(staged.InitialState);
            var seat = host.Seats.FindByHero(staged.Hero);
            Assert.That(seat, Is.Not.Null, "Brogan holds a seat on the staged board.");

            DriveToTheChargesDestinationQuestion(host, staged.Hero);

            var atTheDestinationQuestion = host.ProjectionForSeat(seat.Id);
            Assert.That(atTheDestinationQuestion.IsSeatUndoAvailable, Is.True,
                "Before the target is named the committed Move is the actor's own command, and the " +
                "amendment keeps it rewindable until the Attack's target locks.");

            AnswerTheStandingChoice(host, seat.Id);
            Assert.That(HeroSpace(host, staged.Hero), Is.Not.EqualTo(StagedHeroBoard.HeroAt),
                "The destination answer moved him, so what follows is asked after the Move.");

            AnswerTheStandingChoice(host, seat.Id);

            Assert.That(host.Projection.HasPendingAttackResolution, Is.False,
                "Naming the victim resolves the whole p.14 action in that one command, which is why " +
                "the wall has to read the Attack that FINISHED and not only one still standing.");

            var settled = host.Projection.AcceptedCommands.Count;
            var view = host.ProjectionForSeat(seat.Id);
            Assert.That(view.IsSeatUndoAvailable, Is.False,
                "Its own seat is published no rewind over a resolved card-text Attack.");

            var refused = host.SubmitUndoForSeat(seat.Id, new SeatUndoSubmission(
                new SubmissionId("charge-rewind"), view.Version));

            Assert.That(refused.Accepted, Is.False,
                "The rewind was accepted before this milestone, which is the defect it closes.");
            Assert.That(refused.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.UndoUnavailable));
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(settled),
                "A refused rewind moves nothing at all.");
        }

        /// <summary>
        /// The other side of the wall, and the case that asked whether milestone 8.35p's second
        /// clause is too eager. That clause raises the wall when an Attack that WAS standing is gone
        /// after a command, which is how an Attack answered and completed in one breath still ends
        /// above the seat's floor - but a rewind can also make a standing Attack disappear, by taking
        /// back the command that started it.
        ///
        /// <para>So this drives it: inside the charge's own card text, before any target is named,
        /// the seat rewinds every step it is offered. What must hold is that each offered rewind is
        /// accepted, that the run ends at the turn's own floor rather than at some earlier stop, and
        /// that the actor is back where he started with nothing of the Attack left standing. If the
        /// clause ever strands a seat mid-turn, this is where it shows.</para>
        /// </summary>
        [Test]
        public void RewindingOutOfAChargeBeforeItsTargetIsNamed_RunsBackToItsOwnTurnsFloor()
        {
            var staged = ChargeBoard();
            var host = MatchHost.Compose(staged.InitialState);
            var seat = host.Seats.FindByHero(staged.Hero);

            DriveToTheChargesDestinationQuestion(host, staged.Hero);
            var client = host.ClientForSeat(seat.Id);

            // One step into the destination question, so the rewind has more than the Attack's own
            // start to give back and the run is not vacuous.
            var standing = host.Projection.PendingEffectChoice;
            Assert.That(standing?.SelectInvocations, Is.Not.Empty);
            SubmitFromSeat(host, seat.Id, DirectorCommand.AnswerEffectChoice(
                standing.SelectInvocations[0]));

            var round = host.Projection.RoundIndex;
            var turn = host.Projection.TurnIndex;
            var reverted = 0;
            while (client.Projection.IsSeatUndoAvailable && reverted < 20)
            {
                var before = host.Projection.AcceptedCommands.Count;
                var result = client.SubmitUndo(new SeatUndoSubmission(
                    new SubmissionId("charge-rewind-" + reverted), client.Projection.Version));
                Assert.That(result.Accepted, Is.True, result.Detail);
                Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(before - 1),
                    "One press reverts exactly one accepted command.");
                reverted++;
            }

            Assert.That(reverted, Is.GreaterThan(1),
                "The rewind must give back more than one step, or this proves nothing about the " +
                "step where the Attack itself disappears.");
            Assert.That(host.Projection.HasPendingAttackResolution, Is.False,
                "The Attack the rewind unwound is no longer standing.");
            Assert.That(host.Projection.RoundIndex, Is.EqualTo(round));
            Assert.That(host.Projection.TurnIndex, Is.EqualTo(turn),
                "The rewind stays inside its own turn.");
            Assert.That(HeroSpace(host, staged.Hero), Is.EqualTo(StagedHeroBoard.HeroAt),
                "and it put him back on the space he charged from.");
        }

        /// <summary>
        /// The staged board this milestone is about: nothing beside the hero, and one enemy minion a
        /// straight three spaces away, so a two-space charge lands beside it.
        /// </summary>
        private static StagedHeroBoard ChargeBoard() =>
            BroganBoard.Create(
                minionPlacements: StagedHeroBoard.At("red-melee-01", EnemyBeyondTheLanding));

        /// <summary>
        /// Commits Mad Dash, lets every other seat yield, starts its printed Attack, and stops on the
        /// destination question. Every step is the one command the position leaves, so this drives a
        /// named position rather than searching for one.
        /// </summary>
        private static void DriveToTheChargesDestinationQuestion(IMatchHost host, HeroId brogan)
        {
            foreach (var hero in host.Projection.Participants.Heroes.Select(value => value.Id))
            {
                var card = hero.Equals(brogan)
                    ? HandCard(host, hero, BroganCardContent.MadDash)
                    : LowestInitiativeHandCard(host, hero);
                Submit(host, DirectorCommand.CommitCard(hero, card.Id));
            }

            for (var step = 0; step < 24; step++)
            {
                var projection = host.Projection;
                if (projection.PendingEffectChoice != null &&
                    projection.PendingEffectChoice.NodeId.Value == "charge-destination")
                {
                    return;
                }

                var legal = DeterministicMatchLoop.EnumerateLegalCommands(projection);
                Assert.That(legal, Is.Not.Empty, "The staged board ran out of commands early.");

                // Brogan's own turn takes his printed Attack; every other seat takes whatever the
                // position leaves, which on this board is a yield.
                var start = projection.SelectedActor.IsDefined &&
                    projection.SelectedActor.ActorId.Equals(brogan)
                    ? legal.FirstOrDefault(command =>
                        command.Kind == DirectorCommandKind.StartAction)
                    : null;
                Submit(host, start ?? legal[0]);
            }

            Assert.Fail("The staged charge never reached its destination question.");
        }

        /// <summary>Names the first candidate of the standing choice and closes it.</summary>
        /// <summary>
        /// Answers the standing choice the way its own shape says it is answered, and asserts that
        /// it took the number of commands that shape owes.
        /// </summary>
        /// <remarks>
        /// Milestone 8.46b. This used to say "a select is not an answer; the confirm behind it is",
        /// unconditionally, because every choice really did stand on Select then Confirm. The user's
        /// rule for the decision window makes a choice admitting exactly one selection answer in ONE
        /// press, closed inside that command by the resolution session. The count is not incidental
        /// here: the charges this fixture drives name their victim through <c>set-attack-target@1</c>
        /// rather than through p.14's own command, so the number of commands the naming takes is
        /// exactly what milestone 8.35p's wall has to rise on.
        /// </remarks>
        private static void AnswerTheStandingChoice(IMatchHost host, SeatId seat)
        {
            var standing = host.Projection.PendingEffectChoice;
            Assert.That(standing, Is.Not.Null, "There is no standing choice to answer.");
            Assert.That(standing.SelectInvocations, Is.Not.Empty);
            var answeredByNamingIt = standing.ResolvesOnSelection;
            var before = host.Projection.AcceptedCommands.Count;

            SubmitFromSeat(host, seat, DirectorCommand.AnswerEffectChoice(
                standing.SelectInvocations[0]));

            var selected = host.Projection.PendingEffectChoice;
            if (answeredByNamingIt)
            {
                Assert.That(
                    selected == null || !selected.ChoiceId.Equals(standing.ChoiceId), Is.True,
                    "A choice of exactly one is answered by naming a candidate, so nothing of it " +
                    "is left standing to press.");
                Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(before + 1),
                    "and naming it is ONE accepted command - the command the wall rises on and the " +
                    "command a rewind would have to step back.");
                return;
            }

            Assert.That(selected?.ConfirmInvocation, Is.Not.Null,
                "A selection that could still grow is not an answer; the confirm behind it is.");
            SubmitFromSeat(host, seat, DirectorCommand.AnswerEffectChoice(
                selected.ConfirmInvocation));
        }

        private static void Submit(IMatchHost host, DirectorCommand command)
        {
            var owner = host.Seats.Seats.FirstOrDefault(seat =>
                host.ProjectionForSeat(seat.Id).AuthorizedCommands.Contains(command));
            Assert.That(owner, Is.Not.Null, "No seat is authorized for " + command + ".");
            SubmitFromSeat(host, owner.Id, command);
        }

        private static void SubmitFromSeat(IMatchHost host, SeatId seat, DirectorCommand command)
        {
            var client = host.ClientForSeat(seat);
            var result = client.Submit(new SeatSubmission(
                new SubmissionId("charge-drive-" + host.Sequence),
                client.Projection.Version,
                command));
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static CardInstance HandCard(
            IMatchHost host, HeroId ownerId, CardDefinitionId definitionId) =>
            host.Projection.Cards.Single(card =>
                card.OwnerId.Equals(ownerId) &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(definitionId));

        private static CardInstance LowestInitiativeHandCard(IMatchHost host, HeroId ownerId) =>
            host.Projection.Cards
                .Where(card =>
                    card.OwnerId.Equals(ownerId) && card.State.Location == CardLocation.Hand)
                .OrderBy(card => host.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .ThenBy(card => card.Id.Value, StringComparer.Ordinal)
                .First();

        private static HexCoordinate HeroSpace(IMatchHost host, HeroId heroId) =>
            host.Projection.Heroes.Single(hero => hero.HeroId.Equals(heroId)).Coordinate;

        private static int Distance(HexCoordinate first, HexCoordinate second)
        {
            var dq = first.Q - second.Q;
            var dr = first.R - second.R;
            return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dq + dr)) / 2;
        }
    }
}
