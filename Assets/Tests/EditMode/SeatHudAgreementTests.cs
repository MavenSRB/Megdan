using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.BeforeActionStepPositions;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.9. The third layer: the HUD read model a seat is drawn from agrees with what that
    /// seat's own projection publishes, on the constructed before-the-action-step positions and at
    /// every step of the seeded 9.6 walks.
    ///
    /// <para><b>Why a read model and not a view.</b> The presenters are what turn a projection into
    /// what a player sees (8.30b's reasoning for <c>SeatScopedMatchHudTests</c>), so they are the
    /// surface under test; they need Presentation, which is why this fixture is excluded from the
    /// offline harness and compiled by the presentation probe.</para>
    ///
    /// <para><b>What is compared, for every seat.</b> (1) Each printed-action region: drawn for an
    /// offer the projection publishes, enabled exactly when it is available, naming its blocker when
    /// the authority named one; every start the seat is authorized is drawn. (2) The seat UNDO square,
    /// exactly when <c>IsSeatUndoAvailable</c> is published (8.42d's hidden square). (3) END TURN,
    /// carrying only a command the seat is authorized, and drawn whenever the turn release is. (4) The
    /// Defense window on the defending seat: drawn with the attacker's card as context, offering every
    /// authorized Defense answer (8.80's missing attacker card). (5) Any decision window: drawn only
    /// for a seat the projection names as a decision's owner, with no control the seat is not
    /// authorized to submit.</para>
    /// </summary>
    public sealed class SeatHudAgreementTests
    {
        /// <summary>Milestone 9.9a: one case per step shape, so each stays inside the per-case budget.</summary>
        [TestCaseSource(typeof(BeforeActionStepOfferTests), nameof(BeforeActionStepOfferTests.Shapes))]
        public void EverySteppedFace_IsDrawnAsTheRulesOfferIt(string shape)
        {
            var findings = new List<string>();
            var read = 0;
            foreach (var face in Faces.Where(face => face.Shape.ToString() == shape))
            {
                foreach (var position in PositionsFor(face))
                {
                    var staged = Stage(face, position, out _);
                    if (staged == null)
                    {
                        continue;
                    }

                    var host = new MatchHost(staged.Session);
                    var seat = host.Seats.FindByHero(staged.Actor);
                    var projection = host.ProjectionForSeat(seat.Id);
                    var where = $"{face.Name} - {position.Name}";
                    var model = RoundActionPresenter.CreateViewModel(projection, seat.HeroId);
                    var region = model.PrintedActionRegions.SingleOrDefault(value =>
                        value.Offer.SourceCardInstanceId.Equals(staged.Offer.SourceCardInstanceId) &&
                        value.Offer.ActionDefinitionId.Equals(staged.Offer.ActionDefinitionId));
                    if (region == null)
                    {
                        findings.Add($"{where}: the rules offer it and the HUD draws no region for it");
                        continue;
                    }

                    if ((region.Invocation != null) != staged.Offer.IsAvailable)
                    {
                        findings.Add($"{where}: the rules {(staged.Offer.IsAvailable ? "offer" : "grey")} it, " +
                            $"the HUD {(region.Invocation != null ? "enables" : "greys")} it");
                    }

                    CompareSeat(findings, projection, seat.HeroId, where);
                    read++;
                }
            }

            Assert.That(findings, Is.Empty,
                "The HUD read model disagreed with the rules offer on these stepped faces.");
            Assert.That(read, Is.GreaterThan(100));
        }

        [Test]
        public void EverySeatsHud_DrawsWhatItsProjectionPublishes_AtEveryWalkStep()
        {
            var findings = new List<string>();
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var dealt = QuickGameSetupConfiguration.DealtHeroes.ToArray();
            var readings = 0;
            foreach (var heroes in new[] { dealt, dealt.Reverse().ToArray() })
            {
                var lineups = (heroes.Length + 5) / 6;
                for (var lineup = 0; lineup < lineups; lineup++)
                {
                    var picked = Enumerable.Range(0, 6)
                        .Select(offset => heroes[(lineup * 6 + offset) % heroes.Length])
                        .ToArray();
                    var names = string.Join(",", picked.Select(hero => hero.ContentId.Value));
                    for (var seed = 1; seed <= OfferOutcomeTests.SeedsPerLineup; seed++)
                    {
                        readings += Walk(findings, map, picked, seed, names);
                    }
                }
            }

            Assert.That(findings.Take(40), Is.Empty,
                $"A seat's HUD read model disagreed with its projection at {findings.Count} readings.");
            Assert.That(readings, Is.GreaterThan(1000));
        }

        /// <summary>
        /// Milestone 9.21i. The sixth thing compared, and the one control 9.21i adds: the board's
        /// standee pick of "which of you acts" (RZ-Q4). Every seat's board is built from that seat's
        /// own projection; the pick is cued on a standee exactly when the seat is authorized to answer
        /// it, and each cued standee answers with a command the seat may submit. The walks above
        /// never reach it - no dealt hero has several bodies until 9.21j - so it is read on 9.21a's
        /// staged two-body board.
        /// </summary>
        [Test]
        public void TheActingBodyPick_IsCuedOnTheBoard_ExactlyForTheSeatAuthorizedToAnswerIt()
        {
            var host = new MatchHost(HeroBodiesBoard.CreateSessionAtTheActingBodyPick(out _, out var blue));
            var findings = new List<string>();
            var owners = 0;
            foreach (var seat in host.Seats.Seats)
            {
                var client = new SeatMatchClient(host.ClientForSeat(seat.Id));
                var board = new CapturingBoard();
                new BoardInteractionPresenter(client, board).Refresh();
                var authorized = client.SeatProjection.AuthorizedCommands
                    .Where(command => command.Kind == DirectorCommandKind.ChooseActingBody)
                    .ToArray();
                var cued = board.Model.PieceTargetCues
                    .Where(cue => cue.Target.HeroId.Equals(blue) &&
                        cue.ConsequenceIntent == BoardTargetConsequenceIntent.Ally)
                    .Select(cue => cue.Target.HeroBody)
                    .OrderBy(body => body)
                    .ToArray();
                var answerable = authorized.Select(command => command.HeroBody).OrderBy(body => body).ToArray();
                if (!cued.SequenceEqual(answerable))
                {
                    findings.Add($"{seat.HeroId.Value}: cued bodies [{string.Join(",", cued)}], " +
                        $"authorized answers [{string.Join(",", answerable)}]");
                }

                owners += authorized.Length > 0 ? 1 : 0;
            }

            Assert.That(findings, Is.Empty);
            Assert.That(owners, Is.EqualTo(1), "Exactly the owning seat answers the pick.");

            // The owning seat's click on body 2 is the command it is authorized to send, and it lands.
            var owner = host.Seats.FindByHero(blue);
            var ownerClient = new SeatMatchClient(host.ClientForSeat(owner.Id));
            var presenter = new BoardInteractionPresenter(ownerClient, new CapturingBoard());
            Assert.That(presenter.SubmitHeroBody(blue, 2), Is.True);
            Assert.That(host.Projection.PendingActingBodyChoice, Is.Null);
            Assert.That(host.Projection.ActingBodies.Single().Body, Is.EqualTo(2));
        }

        private sealed class CapturingBoard : IBoardInteractionView
        {
            public BoardInteractionViewModel Model { get; private set; }
            public void Render(BoardInteractionViewModel model) => Model = model;
        }

        /// <summary>
        /// The 9.6 line-up and seed, played through the seats as a client plays it - which is what
        /// publishes each seat's own rewind - comparing every seat before every command.
        /// </summary>
        private static int Walk(
            List<string> findings, GuardsDigital.Core.Board.BoardMapDefinition map,
            RegisteredHero[] picked, int seed, string names)
        {
            var host = MatchHost.Compose(OfferOutcomeTests.Compose(map, picked));
            var random = new MatchLoopRandom(seed);
            var readings = 0;
            for (var step = 0; step < OfferOutcomeTests.CommandsPerMatch &&
                 host.Projection.Phase != RoundResolutionPhase.GameEnded; step++)
            {
                foreach (var seat in host.Seats.Seats)
                {
                    CompareSeat(findings, host.ProjectionForSeat(seat.Id), seat.HeroId,
                        $"{seat.HeroId.Value} ({names} seed {seed} step {step})");
                    readings++;
                }

                var legal = TeamAgreementTestDriver.EnumerateWalkCommands(host.Projection);
                if (legal.Count == 0)
                {
                    break;
                }

                var command = legal[random.Next(legal.Count)];
                if (TeamAgreementTestDriver.SubmitFinalAgreementCommand(
                        host, command, "hud-walk-" + host.Sequence + "-" + step))
                {
                    continue;
                }

                var owner = host.Seats.Seats.FirstOrDefault(value =>
                    host.ProjectionForSeat(value.Id).AuthorizedCommands.Contains(command));
                if (owner == null)
                {
                    findings.Add($"{command.Kind} ({names} seed {seed} step {step}) belongs to no seat");
                    break;
                }

                var client = host.ClientForSeat(owner.Id);
                var result = client.Submit(new SeatSubmission(
                    new SubmissionId("hud-walk-" + host.Sequence + "-" + step),
                    client.Projection.Version,
                    command));
                if (!result.Accepted)
                {
                    findings.Add($"{command.Kind} ({names} seed {seed} step {step}) refused: {result.Detail}");
                    break;
                }
            }

            return readings;
        }

        private static void CompareSeat(
            List<string> findings, SeatProjection projection, HeroId hero, string where)
        {
            var model = RoundActionPresenter.CreateViewModel(projection, hero);
            var authorized = projection.AuthorizedCommands;

            // (1) Every printed-action region stands for a published offer, enabled exactly when it is
            // available and naming the blocker the authority named; every authorized start is drawn.
            foreach (var region in model.PrintedActionRegions)
            {
                var offer = region.Offer;
                if (!projection.AvailableTurnActions.Contains(offer))
                {
                    findings.Add($"{where}: a region is drawn for an offer the seat is not published");
                    continue;
                }

                if ((region.Invocation != null) != offer.IsAvailable)
                {
                    findings.Add($"{where}: {offer.ActionDefinitionId.Value} is " +
                        $"{(offer.IsAvailable ? "available" : "greyed")} and drawn " +
                        $"{(region.Invocation != null ? "enabled" : "greyed")}");
                }

                if (!offer.IsAvailable &&
                    offer.Unavailability.Kind == ActionUnavailabilityKind.BlockedByRuleModification &&
                    region.AssistiveLabel.IndexOf("BLOCKED BY", StringComparison.Ordinal) < 0)
                {
                    findings.Add($"{where}: {offer.ActionDefinitionId.Value} is blocked by a named rule " +
                        "and its region does not name it");
                }
            }

            foreach (var start in authorized.Where(command => command.Kind == DirectorCommandKind.StartAction))
            {
                if (!model.Actions.Any(action => action.Command.Equals(start)))
                {
                    findings.Add($"{where}: the seat may start {start.ActionDefinitionId.Value} and no " +
                        "control is drawn for it");
                }
            }

            foreach (var action in model.Actions)
            {
                if (!authorized.Contains(action.Command))
                {
                    findings.Add($"{where}: a {action.Command.Kind} control is drawn the seat may not submit");
                }
            }

            // (2) The seat's own rewind square.
            if (model.SeatUndoAvailable != projection.IsSeatUndoAvailable)
            {
                findings.Add($"{where}: the rewind is {(projection.IsSeatUndoAvailable ? "" : "not ")}" +
                    $"published and the square is {(model.SeatUndoAvailable ? "" : "not ")}drawn");
            }

            // (3) END TURN.
            if (model.EndTurnInvocation != null && !authorized.Contains(model.EndTurnInvocation.Command))
            {
                findings.Add($"{where}: END TURN carries a {model.EndTurnInvocation.Command.Kind} the " +
                    "seat may not submit");
            }

            if (authorized.Any(command => command.Kind == DirectorCommandKind.ReleaseTurn) &&
                model.EndTurnInvocation?.Command.Kind != DirectorCommandKind.ReleaseTurn)
            {
                findings.Add($"{where}: the seat may release the turn and END TURN does not");
            }

            // (4) and (5) The decision window.
            var window = DecisionWindowPresenter.CreateViewModel(projection, hero);
            var defense = authorized.Where(command =>
                    command.Kind == DirectorCommandKind.ChooseDefenseCard ||
                    command.Kind == DirectorCommandKind.DeclineDefense)
                .ToArray();
            var drawn = window == null
                ? Array.Empty<DirectorCommand>()
                : window.Controls.Concat(window.CornerControl == null
                        ? Array.Empty<DecisionWindowControlViewModel>()
                        : new[] { window.CornerControl })
                    .Where(control => control.Command != null)
                    .Select(control => control.Command)
                    .ToArray();
            if (defense.Length > 0)
            {
                if (window == null)
                {
                    findings.Add($"{where}: the seat must answer a Defense and no window is drawn");
                }
                else
                {
                    if (!window.ContextCards.Any())
                    {
                        findings.Add($"{where}: the Defense window shows no attacking card");
                    }

                    foreach (var answer in defense.Where(answer => !drawn.Contains(answer)))
                    {
                        findings.Add($"{where}: the Defense answer {answer.Kind} is authorized and not drawn");
                    }
                }
            }

            if (window != null)
            {
                var owner = projection.DecisionOwner;
                var owns = !owner.IsDefined ||
                    (owner.IsTeamOwned
                        ? projection.Setup.Heroes.Any(value =>
                            value.HeroId.Equals(hero) && value.TeamSide == owner.TeamSide)
                        : owner.HeroIds.Contains(hero));
                if (!owns)
                {
                    findings.Add($"{where}: a {window.Family} window is drawn for a seat that owns no decision");
                }

                foreach (var command in drawn.Where(command => !authorized.Contains(command)))
                {
                    findings.Add($"{where}: the {window.Family} window draws a {command.Kind} the seat " +
                        "may not submit");
                }
            }
        }
    }
}
