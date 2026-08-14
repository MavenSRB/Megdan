using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MatchFeaturePresenterTests
    {
        [Test]
        public void ConfiguredHudLayout_KeepsCardPanelInsideReportedShortViewport()
        {
            var panel = ConfiguredMatchHudRenderer.CalculateCardPanelRect(778f, 300f);

            Assert.That(panel, Is.EqualTo(new UnityEngine.Rect(12f, 12f, 754f, 276f)));
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(778f));
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(300f));
        }

        [Test]
        public void ConfiguredHudLayout_UsesExpandedDesktopPanelForCompleteDefenseRow()
        {
            var panel = ConfiguredMatchHudRenderer.CalculateCardPanelRect(2048f, 1152f);

            Assert.That(panel, Is.EqualTo(new UnityEngine.Rect(12f, 780f, 1280f, 360f)));
        }

        [Test]
        public void DefenseChoiceLayout_AllocatesAtLeastTheMeasuredFullLabelWidth()
        {
            const string longestCurrentLabel =
                "DISCARD TEST - BLUE SCOUT SHOT\nPRINTED DEFENSE 1";
            var style = new UnityEngine.GUIStyle
            {
                font = UnityEngine.Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf"),
                fontSize = 12,
                padding = new UnityEngine.RectOffset(6, 6, 4, 4),
                wordWrap = false
            };

            var measuredWidth = style.CalcSize(new UnityEngine.GUIContent(longestCurrentLabel)).x;
            var allocatedMinimumWidth = RoundActionUnityView.CalculateContentWidth(
                longestCurrentLabel,
                style);

            Assert.That(allocatedMinimumWidth, Is.GreaterThanOrEqualTo(measuredWidth));
            Assert.That(allocatedMinimumWidth, Is.GreaterThan(150f),
                "The longest current Defense label must not regress to the clipping fixed width.");

            var turnOneLabels = new[]
            {
                "DISCARD TEST - BLUE FEINT\nPRINTED DEFENSE 2",
                "DISCARD TEST - BLUE SENTINEL\nPRINTED DEFENSE 4",
                longestCurrentLabel,
                "DISCARD TEST - BLUE HAMMER\nPRINTED DEFENSE 1",
                "DECLINE DEFENSE"
            };
            var completeRowWidth = 54f + turnOneLabels.Sum(label =>
                RoundActionUnityView.CalculateContentWidth(label, style));
            Assert.That(completeRowWidth, Is.LessThanOrEqualTo(1256f),
                "The expanded desktop panel must fit four Defense cards plus Decline without horizontal scrolling.");
        }

        [Test]
        public void FeaturePresenters_DependOnlyOnSessionAndTheirViewPort()
        {
            AssertPresenterDependencies<BoardInteractionPresenter, IBoardInteractionView>();
            AssertPresenterDependencies<DirectorHandsPresenter, IDirectorHandsView>();
            AssertPresenterDependencies<RoundActionPresenter, IRoundActionView>();
            AssertPresenterDependencies<UndoRestartPresenter, IUndoRestartView>();
            AssertPresenterDependencies<DiagnosticLogPresenter, IDiagnosticLogView>();
            AssertPresenterDependencies<MinionChoicePresenter, IMinionChoiceView>();

            var presentationSources = new[]
            {
                typeof(PrimitiveHeroPointerInput),
                typeof(PrimitiveHeroPointerSelector)
            };
            Assert.That(presentationSources, Has.All.Not.EqualTo(typeof(MatchSceneCompositionRoot)));
        }

        [Test]
        public void Coordinator_RefreshesAllIndependentViewsFromOneImmutableProjection()
        {
            var session = CreateSession();
            var board = new CapturingBoardView();
            var hands = new CapturingHandsView();
            var round = new CapturingRoundView();
            var undo = new CapturingUndoView();
            var diagnostics = new CapturingDiagnosticView();
            var minionChoice = new CapturingMinionChoiceView();
            var coordinator = new MatchFeatureCoordinator(
                new BoardInteractionPresenter(session, board),
                new DirectorHandsPresenter(session, hands),
                new RoundActionPresenter(session, round),
                new UndoRestartPresenter(session, undo),
                new DiagnosticLogPresenter(session, diagnostics),
                new MinionChoicePresenter(session, minionChoice));

            coordinator.RefreshAll();

            var firstBoard = board.Model;
            var firstHands = hands.Model;
            var firstRound = round.Model;
            var firstUndo = undo.Model;
            var firstDiagnostics = diagnostics.Model;
            coordinator.RefreshAll();

            Assert.That(board.Model.Version, Is.EqualTo(session.Projection.Version));
            Assert.That(hands.Model.Version, Is.EqualTo(session.Projection.Version));
            Assert.That(round.Model.Version, Is.EqualTo(session.Projection.Version));
            Assert.That(undo.Model.Version, Is.EqualTo(session.Projection.Version));
            Assert.That(diagnostics.Model.Version, Is.EqualTo(session.Projection.Version));
            Assert.That(hands.Model.Hands, Has.Count.EqualTo(2));
            Assert.That(diagnostics.Model.Entries, Has.Count.EqualTo(1));
            Assert.That(minionChoice.Model, Is.Null,
                "No minion choice is pending here, so its prompt exposes no controls at all.");
            Assert.That(minionChoice.RenderCount, Is.EqualTo(2),
                "The prompt is refreshed with every other view even while it has nothing to show.");
            Assert.That(board.Model, Is.SameAs(firstBoard));
            Assert.That(hands.Model, Is.SameAs(firstHands));
            Assert.That(round.Model, Is.SameAs(firstRound));
            Assert.That(undo.Model, Is.SameAs(firstUndo));
            Assert.That(diagnostics.Model, Is.SameAs(firstDiagnostics));
        }

        [Test]
        public void DirectorHandsPresenter_SelectsAndCommitsWithoutOwningCardAuthority()
        {
            var session = CreateSession();
            var view = new CapturingHandsView();
            var presenter = new DirectorHandsPresenter(session, view);
            var redCard = GetHandCards(session, TeamSide.Red)[0];

            presenter.Refresh();
            Assert.That(presenter.SelectCommitmentCard(redCard.Id), Is.True);
            Assert.That(view.Model.Hands.Single(hand => hand.TeamSide == TeamSide.Red)
                .SelectedCardInstanceId, Is.EqualTo(redCard.Id));
            Assert.That(presenter.ConfirmSelectedCard(), Is.True);

            Assert.That(session.Projection.GetCommitment(redCard.OwnerId).Status,
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));
            Assert.That(session.Projection.AcceptedCommands.Single().Kind,
                Is.EqualTo(DirectorCommandKind.CommitCard));
        }

        [Test]
        public void BoardPresenter_ProjectsAndSubmitsExactPendingMovementChoice()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var movement = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Movement);
            Assert.That(session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted, Is.True);
            var view = new CapturingBoardView();
            var presenter = new BoardInteractionPresenter(session, view);

            presenter.Refresh();
            var destination = session.Projection.PendingMovementChoice.LegalDestinations.First().Destination;
            Assert.That(view.Model.MovementDestinations, Does.Contain(destination));
            Assert.That(presenter.SubmitDestination(destination), Is.True);
            Assert.That(session.Projection.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.ChooseMovementDestination));
        }

        [Test]
        public void BoardPresenter_ProjectsAndSubmitsExactPendingMinionAttackTarget()
        {
            var session = CreateAdjacentMinionSession();
            StartTiedTurn(session, ActionKind.Attack);
            var sourceCardId = session.Projection.SelectedActor.CardInstanceId;
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Attack);
            Assert.That(session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted, Is.True);
            var choice = session.Projection.PendingAttackTargetChoice;
            var targetId = choice.LegalTargetMinionIds.Single();
            var attackingActorId = session.Projection.SelectedActor.ActorId;
            var view = new CapturingBoardView();
            var presenter = new BoardInteractionPresenter(session, view);

            presenter.Refresh();
            Assert.That(view.Model.Minions.Select(value => value.MinionId), Is.EqualTo(new[] { targetId }));
            Assert.That(view.Model.MinionAttackTargets, Is.EqualTo(new[] { targetId }));
            var snapshot = session.CaptureSnapshot();
            var commandCount = session.Projection.AcceptedCommands.Count;
            Assert.That(presenter.SubmitMinion(new MinionId("stale-minion")), Is.False);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(snapshot));
            Assert.That(session.Projection.AcceptedCommands, Has.Count.EqualTo(commandCount));

            Assert.That(presenter.SubmitMinion(targetId), Is.True);
            var command = session.Projection.AcceptedCommands.Last();
            Assert.That(command.Kind, Is.EqualTo(DirectorCommandKind.ChooseAttackTarget));
            Assert.That(command.ResolutionId, Is.EqualTo(choice.ResolutionId));
            Assert.That(command.ChoiceId, Is.EqualTo(choice.ChoiceId));
            Assert.That(command.MinionId, Is.EqualTo(targetId));
            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null);
            Assert.That(session.Projection.Minions, Is.Empty);
            Assert.That(session.Projection.Cards.Single(card => card.Id.Equals(sourceCardId)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(session.Projection.SelectedActor.ActorId, Is.Not.EqualTo(attackingActorId));
            var completedSnapshot = session.CaptureSnapshot();
            var completedCommandCount = session.Projection.AcceptedCommands.Count;
            Assert.That(presenter.SubmitMinion(targetId), Is.False);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(completedSnapshot));
            Assert.That(session.Projection.AcceptedCommands, Has.Count.EqualTo(completedCommandCount));
            presenter.Refresh();
            Assert.That(view.Model.Minions, Is.Empty);
            Assert.That(view.Model.MinionAttackTargets, Is.Empty);
        }

        [Test]
        public void BoardPresenter_PreservesHeroTargetProjectionAndTypedDefenseContinuation()
        {
            var session = CreateAdjacentSession();
            StartTiedTurn(session, ActionKind.Attack);
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Attack);
            Assert.That(session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted, Is.True);
            var choice = session.Projection.PendingAttackTargetChoice;
            var targetId = choice.LegalTargetHeroIds.Single();
            var view = new CapturingBoardView();
            var presenter = new BoardInteractionPresenter(session, view);

            presenter.Refresh();
            Assert.That(view.Model.AttackTargets, Is.EqualTo(new[] { targetId }));
            Assert.That(view.Model.MinionAttackTargets, Is.Empty);
            Assert.That(presenter.SubmitHero(targetId), Is.True);
            Assert.That(session.Projection.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.ChooseAttackTarget));
            Assert.That(session.Projection.AcceptedCommands.Last().HeroId, Is.EqualTo(targetId));
            Assert.That(session.Projection.AcceptedCommands.Last().MinionId.IsDefined, Is.False);
            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null);
        }

        [Test]
        public void ConfiguredPresenters_RequireExplicitFastTravelSelectionBeforeDestinationSubmission()
        {
            var session = CreateSession();
            Assert.That(RoundActionPresenter.CreateViewModel(session.Projection).FastTravelOffers, Is.Empty,
                "The Fast Travel button must be absent when authority projects no eligible replacement.");
            StartTiedTurn(session, ActionKind.Movement);
            var option = session.Projection.AvailableFastTravelOptions.Single();
            var roundView = new CapturingRoundView();
            var roundPresenter = new RoundActionPresenter(session, roundView);
            var boardView = new CapturingBoardView();
            var boardPresenter = new BoardInteractionPresenter(session, boardView);
            FastTravelOfferViewModel requestedSelection = null;
            roundPresenter.FastTravelSelectionRequested += selection =>
            {
                requestedSelection = selection;
                Assert.That(boardPresenter.ActivateFastTravel(
                    selection.ReplacedMovementActionDefinitionId), Is.True);
            };

            roundPresenter.Refresh();
            boardPresenter.Refresh();
            Assert.That(roundView.Model.FastTravelOffers, Has.Count.EqualTo(1));
            Assert.That(roundView.Model.FastTravelOffers[0].Label, Is.EqualTo("FAST TRAVEL"));
            Assert.That(roundView.Model.Actions, Is.Not.Empty,
                "Normal projected actions remain the default interaction state.");
            Assert.That(roundView.Model.FastTravelOffers[0].ReplacedMovementActionDefinitionId,
                Is.EqualTo(option.ReplacedMovementActionDefinitionId));
            Assert.That(roundView.Model.FastTravelOffers[0].LegalDestinationCount,
                Is.EqualTo(option.LegalDestinations.Count));
            Assert.That(boardView.Model.FastTravelDestinations, Is.Empty,
                "Receiving an eligible projection must not activate Fast Travel highlights.");

            var actorId = session.Projection.SelectedActor.ActorId;
            var destination = option.LegalDestinations[0];
            var commandCount = session.Projection.AcceptedCommands.Count;
            Assert.That(boardPresenter.SubmitDestination(destination), Is.False,
                "A legal Fast Travel coordinate is not actionable before explicit selection.");
            Assert.That(session.Projection.AcceptedCommands, Has.Count.EqualTo(commandCount));

            Assert.That(roundPresenter.SelectFastTravel(roundView.Model.FastTravelOffers.Single()), Is.True);
            Assert.That(requestedSelection, Is.SameAs(roundView.Model.FastTravelOffers.Single()));
            boardPresenter.Refresh();
            Assert.That(boardView.Model.FastTravelDestinations,
                Is.EquivalentTo(option.LegalDestinations));
            Assert.That(boardPresenter.SubmitDestination(destination), Is.True);
            Assert.That(session.Projection.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.PerformFastTravel));
            Assert.That(session.Projection.Heroes.Single(hero => hero.HeroId.Equals(actorId)).Coordinate,
                Is.EqualTo(destination));
        }

        [Test]
        public void RoundPresenter_ProjectsContextualEndTurnAsExactTypedInvocation()
        {
            var holdSession = CreateSession();
            StartTiedTurn(holdSession, ActionKind.Movement);
            var holdModel = RoundActionPresenter.CreateViewModel(holdSession.Projection);
            Assert.That(holdModel.Actions.Any(action => action.Label.StartsWith("PRIMARY ")), Is.True);
            Assert.That(holdModel.Actions.Any(action => action.Label.StartsWith("SECONDARY ")), Is.True);
            Assert.That(holdModel.Actions.All(action => !action.Label.Contains("PRINTED")), Is.True,
                "Player-facing action labels must not expose invocation-context enum names.");
            Assert.That(holdModel.EndTurnInvocation, Is.Not.Null);
            Assert.That(holdModel.EndTurnInvocation.Label, Is.EqualTo("END TURN"));
            Assert.That(holdModel.EndTurnInvocation.Command.Kind, Is.EqualTo(DirectorCommandKind.PerformHold));
            Assert.That(holdModel.EndTurnInvocation.Command.CardInstanceId,
                Is.EqualTo(holdSession.Projection.SelectedActor.CardInstanceId));

            var movement = holdSession.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(holdSession.Projection, offer) == ActionKind.Movement);
            Assert.That(holdSession.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted, Is.True);
            var movementModel = RoundActionPresenter.CreateViewModel(holdSession.Projection);
            Assert.That(movementModel.Actions, Is.Empty);
            Assert.That(movementModel.EndTurnInvocation.Command.Kind,
                Is.EqualTo(DirectorCommandKind.StopMovement));
            Assert.That(movementModel.EndTurnInvocation.Command.ResolutionId,
                Is.EqualTo(holdSession.Projection.PendingMovementChoice.ResolutionId));
            Assert.That(movementModel.EndTurnInvocation.Command.ChoiceId,
                Is.EqualTo(holdSession.Projection.PendingMovementChoice.ChoiceId));

            var attackSession = CreateAdjacentSession();
            StartTiedTurn(attackSession, ActionKind.Attack);
            var attack = attackSession.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(attackSession.Projection, offer) == ActionKind.Attack);
            Assert.That(attackSession.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted, Is.True);
            Assert.That(attackSession.Projection.PendingAttackTargetChoice, Is.Not.Null);
            var mandatoryModel = RoundActionPresenter.CreateViewModel(attackSession.Projection);
            Assert.That(mandatoryModel.Actions, Is.Empty);
            Assert.That(mandatoryModel.EndTurnInvocation, Is.Null,
                "A mandatory target choice cannot be bypassed by the contextual control.");
        }

        [Test]
        public void PassiveRoundView_ForwardsAnyProjectedTypedTurnEndingInvocationWithoutBranching()
        {
            var view = new RoundActionUnityView();
            TypedInvocationViewModel submitted = null;
            view.InvocationSubmitted += invocation => submitted = invocation;
            var futureInvocation = new TypedInvocationViewModel(
                "END TURN",
                DirectorCommand.AdvanceEndOfRound());
            view.Render(new RoundActionViewModel(
                7,
                2,
                4,
                RoundResolutionPhase.EndOfRound,
                "Future projected invocation",
                Array.Empty<TypedInvocationViewModel>(),
                futureInvocation,
                null,
                Array.Empty<TypedInvocationViewModel>(),
                Array.Empty<TypedInvocationViewModel>()));

            view.ForwardInvocation(view.Model.EndTurnInvocation);

            Assert.That(submitted, Is.SameAs(futureInvocation));
            Assert.That(submitted.Command.Kind, Is.EqualTo(DirectorCommandKind.AdvanceEndOfRound));
        }

        [Test]
        public void PassiveRoundView_ForwardsProjectedFastTravelSelectionWithoutCreatingACommand()
        {
            var view = new RoundActionUnityView();
            var offer = new FastTravelOfferViewModel(
                "FAST TRAVEL",
                new ActionDefinitionId("test-movement"),
                3);
            FastTravelOfferViewModel selected = null;
            view.FastTravelSelected += value => selected = value;
            view.Render(new RoundActionViewModel(
                4,
                1,
                1,
                RoundResolutionPhase.ResolvingTurn,
                "Choose an action",
                Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                Array.Empty<TypedInvocationViewModel>(),
                Array.Empty<TypedInvocationViewModel>(),
                new[] { offer }));

            view.ForwardFastTravelSelection(view.Model.FastTravelOffers.Single());

            Assert.That(selected, Is.SameAs(offer));
        }

        [Test]
        public void RoundPresenter_ProjectsPrintedDefenseValuesWithoutChangingTypedChoices()
        {
            var session = CreateAdjacentSession();
            StartTiedTurn(session, ActionKind.Attack);
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Attack);
            Assert.That(session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted, Is.True);
            var targetChoice = session.Projection.PendingAttackTargetChoice;
            Assert.That(session.Execute(DirectorCommand.ChooseAttackTarget(
                targetChoice.ResolutionId,
                targetChoice.ChoiceId,
                targetChoice.LegalTargetHeroIds.Single())).Accepted, Is.True);

            var choice = session.Projection.PendingDefenseChoice;
            var model = RoundActionPresenter.CreateViewModel(session.Projection);
            var cardChoices = model.DefenseChoices.Where(value =>
                value.Command.Kind == DirectorCommandKind.ChooseDefenseCard).ToArray();

            Assert.That(cardChoices, Has.Length.EqualTo(choice.LegalDefenseOffers.Count));
            Assert.That(cardChoices.Select(value => value.Command.CardInstanceId),
                Is.EquivalentTo(choice.LegalDefenseOffers.Select(value => value.SourceCardInstanceId)));
            Assert.That(cardChoices.Select(value => PrintedDefenseValue(session.Projection, value.Command.CardInstanceId))
                .Distinct().Count(), Is.GreaterThan(1));
            foreach (var invocation in cardChoices)
            {
                var card = session.Projection.Cards.Single(value =>
                    value.Id.Equals(invocation.Command.CardInstanceId));
                var definition = session.Projection.Definitions.Single(value =>
                    value.Reference.Equals(card.DefinitionReference));
                Assert.That(invocation.Label, Is.EqualTo(
                    $"DISCARD {definition.DisplayName.ToUpperInvariant()}\n" +
                    $"PRINTED DEFENSE {PrintedDefenseValue(session.Projection, card.Id)}"));
                Assert.That(invocation.Command.ResolutionId, Is.EqualTo(choice.ResolutionId));
                Assert.That(invocation.Command.ChoiceId, Is.EqualTo(choice.ChoiceId));
            }

            var decline = model.DefenseChoices.Single(value =>
                value.Command.Kind == DirectorCommandKind.DeclineDefense);
            Assert.That(decline.Label, Is.EqualTo("DECLINE DEFENSE"));
            Assert.That(decline.Command.ResolutionId, Is.EqualTo(choice.ResolutionId));
            Assert.That(decline.Command.ChoiceId, Is.EqualTo(choice.ChoiceId));
        }

        /// <summary>
        /// Milestone 6.8a put the first Skill into the configured hand, so a paused effect frame became
        /// reachable in the real scene. The presenter forwards the decision the projection already
        /// bounded and withholds every control the authority would refuse while it stands.
        /// </summary>
        [Test]
        public void RoundPresenter_ForwardsAPausedEffectFramesModesAndWithholdsEveryOtherControl()
        {
            var session = CreateSession();
            StartSkillTurn(session);
            var skill = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Skill);
            var beforeStart = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(beforeStart.EffectChoices, Is.Empty, "No frame is paused yet.");
            Assert.That(beforeStart.Actions, Is.Not.Empty);
            Assert.That(session.Execute(DirectorCommand.StartAction(skill.ActionDefinitionId)).Accepted,
                Is.True);

            var choice = session.Projection.PendingEffectChoice;
            var model = RoundActionPresenter.CreateViewModel(session.Projection);
            var selections = model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();

            Assert.That(model.Actions, Is.Empty,
                "A paused Skill frame withholds the offers Core would refuse, as paused Movement does.");
            Assert.That(model.FastTravelOffers, Is.Empty);
            Assert.That(model.EndTurnInvocation, Is.Null,
                "The mode choice is mandatory, so End Turn is not offered against it.");
            Assert.That(selections.Select(value => value.Command.EffectChoice.Candidate.OptionNodeId),
                Is.EqualTo(choice.Candidates.Select(candidate => candidate.OptionNodeId)),
                "Modes are forwarded in the projection's own order.");
            Assert.That(selections.All(value =>
                value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                value.Command.EffectChoice.ResolutionId.Equals(choice.ResolutionId) &&
                value.Command.EffectChoice.ChoiceId.Equals(choice.ChoiceId)), Is.True,
                "Every forwarded invocation is one the projection built, identities included.");
            for (var index = 0; index < selections.Length; index++)
            {
                Assert.That(selections[index].Label, Is.EqualTo(EffectChoiceLabels.CandidateLabel(
                    choice.Candidates[index].OptionNodeId, index + 1)));
            }

            // Driven through the presenter rather than the session, because the presenter accepts only
            // invocations it currently projects: a forwarded decision missing from that legality list
            // is refused, and nothing else in this fixture would notice.
            var presenter = new RoundActionPresenter(session, new CapturingRoundView());
            Assert.That(presenter.Submit(selections[0]), Is.True);
            var afterSelect = RoundActionPresenter.CreateViewModel(session.Projection);
            var confirm = afterSelect.EffectChoices.Single(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            Assert.That(confirm.Label, Is.EqualTo(EffectChoiceLabels.ConfirmLabel));
            Assert.That(afterSelect.EffectChoices.Any(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Deselect), Is.True,
                "A selected mode can be taken back before it is confirmed.");

            Assert.That(presenter.Submit(confirm), Is.True);
            var afterConfirm = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(afterConfirm.EffectChoices, Is.Empty,
                "The completed frame leaves no decision behind.");
        }

        [Test]
        public void DefensePreview_ProjectsAuthorityComputedBreakdownsAndStableSignedMinionCues()
        {
            var session = CreateDefensePreviewSession();
            StartTiedTurn(session, ActionKind.Attack);
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Attack);
            Assert.That(session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted, Is.True);
            var target = session.Projection.PendingAttackTargetChoice;
            Assert.That(session.Execute(DirectorCommand.ChooseAttackTarget(
                target.ResolutionId,
                target.ChoiceId,
                target.LegalTargetHeroIds.Single())).Accepted, Is.True);

            var projection = session.Projection;
            var preview = projection.PendingDefensePreview;
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.AttackTotal, Is.EqualTo(projection.PendingAttackResolution.AttackTotal));
            Assert.That(preview.AttackContributions.Select(value => value.Contribution),
                Is.EqualTo(projection.PendingAttackResolution.AttackValue.Contributions));
            Assert.That(preview.MinionContributions.Select(value =>
                    (value.SourceMinionId.Value, value.Amount)),
                Is.EquivalentTo(new[]
                {
                    ("blue-defense-support", 1),
                    ("red-defense-hinder", -1),
                    ("red-ranged-hinder", -1)
                }));
            Assert.That(preview.MinionContributions.All(value =>
                value.SourceLabel.Contains("minion (")), Is.True);
            Assert.That(preview.Choices.Count, Is.EqualTo(
                projection.PendingDefenseChoice.LegalDefenseOffers.Count + 1));

            foreach (var cardPreview in preview.Choices.Where(value => !value.IsDecline))
            {
                Assert.That(cardPreview.DefenseContributions.Select(value => value.Contribution.SourceMinionId)
                        .Where(value => value.IsDefined),
                    Is.EquivalentTo(preview.MinionContributions.Select(value => value.SourceMinionId)));
                Assert.That(cardPreview.DefenseTotal,
                    Is.EqualTo(cardPreview.DefenseContributions.Sum(value => value.Amount)));
                Assert.That(cardPreview.Outcome, Is.EqualTo(
                    cardPreview.DefenseTotal.Value >= preview.AttackTotal
                        ? AttackResolutionOutcome.SuccessfullyDefended
                        : AttackResolutionOutcome.AttackSuccessful));
            }

            var decline = preview.Choices.Single(value => value.IsDecline);
            Assert.That(decline.DefenseTotal, Is.Null);
            Assert.That(decline.Outcome, Is.EqualTo(AttackResolutionOutcome.AttackSuccessful));

            var boardView = new CapturingBoardView();
            new BoardInteractionPresenter(session, boardView).Refresh();
            Assert.That(boardView.Model.MinionDefenseCues.Select(value =>
                    (value.MinionId.Value, value.Amount)),
                Is.EquivalentTo(preview.MinionContributions.Select(value =>
                    (value.SourceMinionId.Value, value.Amount))));
            Assert.That(boardView.Model.MinionAttackTargets, Is.Empty,
                "Defense contribution cues must remain separate from selectable Attack-target cues.");

            var pendingSnapshot = session.CaptureSnapshot();
            var selected = preview.Choices.First(value => !value.IsDecline);
            var command = DirectorCommand.ChooseDefenseCard(
                preview.ResolutionId,
                preview.ChoiceId,
                selected.CardInstanceId.Value);
            var response = session.Execute(command);
            Assert.That(response.Accepted, Is.True);
            Assert.That(session.Projection.PendingDefensePreview, Is.Null);
            Assert.That(session.Projection.Cards.Single(value =>
                value.Id.Equals(selected.CardInstanceId.Value)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile));
            new BoardInteractionPresenter(session, boardView).Refresh();
            Assert.That(boardView.Model.MinionDefenseCues, Is.Empty);

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.PendingDefensePreview.MinionContributions.Select(value =>
                    (value.SourceMinionId.Value, value.Amount)),
                Is.EquivalentTo(preview.MinionContributions.Select(value =>
                    (value.SourceMinionId.Value, value.Amount))));
            Assert.That(session.Execute(DirectorCommand.DeclineDefense(
                session.Projection.PendingDefenseChoice.ResolutionId,
                session.Projection.PendingDefenseChoice.ChoiceId)).Accepted, Is.True);
            Assert.That(session.RestoreSnapshot(pendingSnapshot).Accepted, Is.True);
            Assert.That(session.Projection.PendingDefensePreview.MinionContributions.Select(value =>
                    (value.SourceMinionId.Value, value.Amount)),
                Is.EquivalentTo(preview.MinionContributions.Select(value =>
                    (value.SourceMinionId.Value, value.Amount))));
        }

        [Test]
        public void DefensePreview_HoverSwitchDismissalAndVersionResetNeverSubmitACommand()
        {
            var session = CreateDefensePreviewSession();
            StartTiedTurn(session, ActionKind.Attack);
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Attack);
            Assert.That(session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted, Is.True);
            var target = session.Projection.PendingAttackTargetChoice;
            Assert.That(session.Execute(DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, target.LegalTargetHeroIds.Single())).Accepted, Is.True);
            var model = RoundActionPresenter.CreateViewModel(session.Projection);
            var view = new RoundActionUnityView();
            view.Render(model);
            var beforeCommands = session.Projection.AcceptedCommands.Count;

            view.ShowDefensePreview(model.DefenseChoices[0].DefenseBreakdown,
                new UnityEngine.Rect(100f, 240f, 180f, 46f));
            Assert.That(view.ActiveDefensePreview, Is.SameAs(model.DefenseChoices[0].DefenseBreakdown));
            view.ShowDefensePreview(model.DefenseChoices[1].DefenseBreakdown,
                new UnityEngine.Rect(290f, 240f, 180f, 46f));
            Assert.That(view.ActiveDefensePreview, Is.SameAs(model.DefenseChoices[1].DefenseBreakdown));
            Assert.That(session.Projection.AcceptedCommands, Has.Count.EqualTo(beforeCommands));

            view.ScheduleDefensePreviewDismissal(10d);
            view.UpdateDefensePreviewDismissal(10.1d);
            Assert.That(view.ActiveDefensePreview, Is.Not.Null);
            view.UpdateDefensePreviewDismissal(10.13d);
            Assert.That(view.ActiveDefensePreview, Is.Null);

            view.ShowDefensePreview(model.DefenseChoices[0].DefenseBreakdown,
                new UnityEngine.Rect(100f, 240f, 180f, 46f));
            view.Render(new RoundActionViewModel(
                model.Version + 1,
                model.RoundIndex,
                model.TurnIndex,
                model.Phase,
                model.Status,
                Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                Array.Empty<TypedInvocationViewModel>(),
                Array.Empty<TypedInvocationViewModel>()));
            Assert.That(view.ActiveDefensePreview, Is.Null);
        }

        [Test]
        public void DefensePreview_FormatsBlockDeclineAndViewportPlacementExplicitly()
        {
            var attackContribution = new ValueContributionProjection(
                new ValueContribution(
                    ValueContributionKind.PrintedIcon,
                    new CardInstanceId("attack-card"),
                    new ActionDefinitionId("attack-action"),
                    PrintedValueChannel.Attack,
                    3),
                "Attack Card");
            var blocked = new DefenseChoicePreviewProjection(
                new CardInstanceId("block-card"),
                "Block Card",
                Array.Empty<ValueContributionProjection>(),
                0,
                DefenseBlockResult.Blocked,
                AttackResolutionOutcome.SuccessfullyDefended);
            var decline = new DefenseChoicePreviewProjection(
                null,
                "Decline Defense",
                Array.Empty<ValueContributionProjection>(),
                null,
                DefenseBlockResult.None,
                AttackResolutionOutcome.AttackSuccessful);
            var preview = new PendingDefensePreviewProjection(
                new ActionResolutionId("preview-resolution"),
                new ActionResolutionStepId("preview-choice"),
                new[] { attackContribution },
                3,
                new[] { blocked, decline },
                Array.Empty<ValueContributionProjection>());

            var blockedModel = RoundActionPresenter.CreateDefenseBreakdown(4, preview, blocked);
            var declineModel = RoundActionPresenter.CreateDefenseBreakdown(4, preview, decline);
            Assert.That(blockedModel.Text, Does.Contain("BLOCK - DEFENDED"));
            Assert.That(blockedModel.Text, Does.Not.Contain("2147483647"));
            Assert.That(declineModel.Text, Does.Contain("NO DEFENSE TOTAL - ATTACK SUCCEEDS"));
            Assert.That(blockedModel.Text, Does.StartWith("ATTACK:"));
            Assert.That(blockedModel.Text, Does.Contain("\nDEFENSE:\n"));
            Assert.That(blockedModel.Text, Does.Contain("RESULT: BLOCK - DEFENDED"));
            Assert.That(RoundActionUnityView.DefensePreviewBackgroundColor.a, Is.EqualTo(1f),
                "The breakdown must be fully opaque over the card HUD.");

            var anchor = new UnityEngine.Rect(300f, 260f, 180f, 46f);
            var popup = RoundActionUnityView.CalculateDefensePreviewRect(
                anchor,
                new UnityEngine.Vector2(778f, 300f),
                new UnityEngine.Vector2(440f, 220f));
            Assert.That(popup.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(popup.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(popup.xMax, Is.LessThanOrEqualTo(778f));
            Assert.That(popup.yMax, Is.LessThanOrEqualTo(300f));
            Assert.That(popup.Overlaps(anchor), Is.False);
        }

        [Test]
        public void UndoRestartAndDiagnostics_PresentersKeepOperationsAndLogProjectionSeparate()
        {
            var session = CreateSession();
            var card = GetHandCards(session, TeamSide.Red)[0];
            Assert.That(session.Execute(DirectorCommand.CommitCard(card.OwnerId, card.Id)).Accepted, Is.True);
            var undoView = new CapturingUndoView();
            var undo = new UndoRestartPresenter(session, undoView);
            var diagnosticsView = new CapturingDiagnosticView();
            var diagnostics = new DiagnosticLogPresenter(session, diagnosticsView);

            undo.Refresh();
            diagnostics.Refresh();
            Assert.That(undoView.Model.CanUndo, Is.True);
            Assert.That(diagnosticsView.Model.IsExpanded, Is.False);
            Assert.That(diagnosticsView.Model.Entries.Count, Is.GreaterThan(1));
            diagnostics.Toggle();
            Assert.That(diagnosticsView.Model.IsExpanded, Is.True);
            Assert.That(undo.Undo(), Is.True);
            Assert.That(session.Projection.AcceptedCommands, Is.Empty);
        }

        private static void AssertPresenterDependencies<TPresenter, TView>()
        {
            var fields = typeof(TPresenter).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fields.Any(field => field.FieldType == typeof(IMatchSession)), Is.True);
            Assert.That(fields.Any(field => field.FieldType == typeof(TView)), Is.True);
            Assert.That(fields.Any(field => field.FieldType.Name.EndsWith("Presenter", StringComparison.Ordinal)),
                Is.False);
        }

        private static MatchSession CreateSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static MatchSession CreateAdjacentSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("presenter-adjacent-fixture"),
                1,
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) });
            var redHeroId = configuration.RedHand.Instances[0].OwnerId;
            var blueHeroId = configuration.BlueHand.Instances[0].OwnerId;
            var redTeam = configuration.Participants.GetTeamForHero(redHeroId);
            var blueTeam = configuration.Participants.GetTeamForHero(blueHeroId);
            var redCoordinate = new HexCoordinate(0, 0);
            var blueCoordinate = new HexCoordinate(1, 0);
            var setup = new QuickGameSetupState(
                map.Reference,
                3,
                configuration.TieBreakerState,
                new[]
                {
                    new QuickGameTeamSetupState(redTeam.Id, TeamSide.Red, 4, new ZoneId("red-zone")),
                    new QuickGameTeamSetupState(blueTeam.Id, TeamSide.Blue, 4, new ZoneId("blue-zone"))
                },
                new[]
                {
                    new QuickGameHeroSetupState(
                        redHeroId,
                        new HeroContentId("red-presenter"),
                        "Red Presenter",
                        redTeam.Id,
                        TeamSide.Red,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("red-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q0_r0")),
                        redCoordinate),
                    new QuickGameHeroSetupState(
                        blueHeroId,
                        new HeroContentId("blue-presenter"),
                        "Blue Presenter",
                        blueTeam.Id,
                        TeamSide.Blue,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("blue-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q1_r0")),
                        blueCoordinate)
                },
                Array.Empty<QuickGameMinionSetupState>());
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, redCoordinate),
                    new HeroOccupancy(blueHeroId, blueCoordinate)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static MatchSession CreateAdjacentMinionSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("presenter-adjacent-minion-fixture"),
                1,
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(2, 0) });
            var redHeroId = configuration.RedHand.Instances[0].OwnerId;
            var blueHeroId = configuration.BlueHand.Instances[0].OwnerId;
            var redTeam = configuration.Participants.GetTeamForHero(redHeroId);
            var blueTeam = configuration.Participants.GetTeamForHero(blueHeroId);
            var redCoordinate = new HexCoordinate(0, 0);
            var minionCoordinate = new HexCoordinate(1, 0);
            var blueCoordinate = new HexCoordinate(2, 0);
            var minion = new QuickGameMinionSetupState(
                new MinionId("blue-presenter-minion"),
                blueTeam.Id,
                TeamSide.Blue,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("blue-minion-spawn")),
                new ConfiguredSpaceReference(map.Id, new SpaceId("q1_r0")),
                minionCoordinate);
            var setup = new QuickGameSetupState(
                map.Reference,
                3,
                configuration.TieBreakerState,
                new[]
                {
                    new QuickGameTeamSetupState(redTeam.Id, TeamSide.Red, 4, new ZoneId("red-zone")),
                    new QuickGameTeamSetupState(blueTeam.Id, TeamSide.Blue, 4, new ZoneId("blue-zone"))
                },
                new[]
                {
                    new QuickGameHeroSetupState(
                        redHeroId,
                        new HeroContentId("red-presenter"),
                        "Red Presenter",
                        redTeam.Id,
                        TeamSide.Red,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("red-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q0_r0")),
                        redCoordinate),
                    new QuickGameHeroSetupState(
                        blueHeroId,
                        new HeroContentId("blue-presenter"),
                        "Blue Presenter",
                        blueTeam.Id,
                        TeamSide.Blue,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("blue-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q2_r0")),
                        blueCoordinate)
                },
                new[] { minion });
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, redCoordinate),
                    new HeroOccupancy(blueHeroId, blueCoordinate)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static MatchSession CreateDefensePreviewSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("defense-preview-fixture"),
                1,
                new[]
                {
                    new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0),
                    new HexCoordinate(1, 1),
                    new HexCoordinate(2, 0),
                    new HexCoordinate(2, 1)
                });
            var redHeroId = configuration.RedHand.Instances[0].OwnerId;
            var blueHeroId = configuration.BlueHand.Instances[0].OwnerId;
            var redTeam = configuration.Participants.GetTeamForHero(redHeroId);
            var blueTeam = configuration.Participants.GetTeamForHero(blueHeroId);
            var redCoordinate = new HexCoordinate(0, 0);
            var blueCoordinate = new HexCoordinate(1, 0);
            var minions = new[]
            {
                CreateMinion(map.Id, "blue-defense-support", blueTeam.Id, TeamSide.Blue,
                    StandardMinionType.Melee, new HexCoordinate(1, 1)),
                CreateMinion(map.Id, "red-defense-hinder", redTeam.Id, TeamSide.Red,
                    StandardMinionType.Heavy, new HexCoordinate(2, 0)),
                CreateMinion(map.Id, "red-ranged-hinder", redTeam.Id, TeamSide.Red,
                    StandardMinionType.Ranged, new HexCoordinate(2, 1))
            };
            var setup = new QuickGameSetupState(
                map.Reference,
                3,
                configuration.TieBreakerState,
                new[]
                {
                    new QuickGameTeamSetupState(redTeam.Id, TeamSide.Red, 4, new ZoneId("red-zone")),
                    new QuickGameTeamSetupState(blueTeam.Id, TeamSide.Blue, 4, new ZoneId("blue-zone"))
                },
                new[]
                {
                    new QuickGameHeroSetupState(redHeroId, new HeroContentId("red-preview"), "Red Preview",
                        redTeam.Id, TeamSide.Red,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("red-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q0_r0")), redCoordinate),
                    new QuickGameHeroSetupState(blueHeroId, new HeroContentId("blue-preview"), "Blue Preview",
                        blueTeam.Id, TeamSide.Blue,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("blue-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q1_r0")), blueCoordinate)
                },
                minions);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, redCoordinate),
                    new HeroOccupancy(blueHeroId, blueCoordinate)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static QuickGameMinionSetupState CreateMinion(
            MapId mapId,
            string id,
            TeamId teamId,
            TeamSide side,
            StandardMinionType type,
            HexCoordinate coordinate)
        {
            return new QuickGameMinionSetupState(
                new MinionId(id),
                teamId,
                side,
                type,
                new ConfiguredSpawnPointReference(mapId, new SpawnPointId($"{id}-spawn")),
                new ConfiguredSpaceReference(mapId, new SpaceId($"q{coordinate.Q}_r{coordinate.R}")),
                coordinate);
        }

        /// <summary>
        /// Commits Red Bulwark's Skill against a Blue card below its Initiative 11, so the Red hero acts
        /// first. <see cref="StartTiedTurn"/> cannot reach it: it commits Blue's highest card, and Blue
        /// Feint at 12 outranks the Skill.
        /// </summary>
        private static void StartSkillTurn(MatchSession session)
        {
            var red = GetHandCards(session, TeamSide.Red)
                .Single(card => card.Id.Value == "red-bulwark-card");
            var blue = GetHandCards(session, TeamSide.Blue)
                .Single(card => card.Id.Value == "blue-hammer-card");
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null);
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));
        }

        private static void StartTiedTurn(MatchSession session, ActionKind requiredAction)
        {
            var red = GetHandCards(session, TeamSide.Red)
                .Where(card => GetDefinition(session, card).ActionDefinitions.Any(action =>
                    action.Kind == requiredAction))
                .OrderByDescending(card => GetDefinition(session, card).Initiative.Value)
                .First();
            var blue = GetHandCards(session, TeamSide.Blue)
                .OrderByDescending(card => GetDefinition(session, card).Initiative.Value)
                .First();
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            var choice = session.Projection.PendingInitiativeChoice;
            if (choice != null)
            {
                Assert.That(session.Execute(DirectorCommand.ChooseTiedActor(choice.OwningSide, red.Id)).Accepted, Is.True);
            }
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));
        }

        private static CardDefinition GetDefinition(MatchSession session, CardInstance card)
        {
            return session.Projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference));
        }

        private static IReadOnlyList<CardInstance> GetHandCards(MatchSession session, TeamSide side)
        {
            return session.Projection.Cards.Where(card =>
                    session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                    card.State.Location == CardLocation.Hand)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private static ActionKind GetActionKind(MatchSessionProjection projection, ActionOffer offer)
        {
            return projection.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(offer.ActionDefinitionId)).Kind;
        }

        private static int PrintedDefenseValue(MatchSessionProjection projection, CardInstanceId cardId)
        {
            var card = projection.Cards.Single(value => value.Id.Equals(cardId));
            return projection.Definitions.Single(value => value.Reference.Equals(card.DefinitionReference))
                .ActionDefinitions.Single(value => value.Kind == ActionKind.Defense)
                .BaseValues.Single(value => value.Channel == PrintedValueChannel.Defense)
                .Amount;
        }

        private sealed class CapturingBoardView : IBoardInteractionView
        {
            public BoardInteractionViewModel Model { get; private set; }
            public void Render(BoardInteractionViewModel model) => Model = model;
        }

        private sealed class CapturingHandsView : IDirectorHandsView
        {
            public DirectorHandsViewModel Model { get; private set; }
            public void Render(DirectorHandsViewModel model) => Model = model;
        }

        private sealed class CapturingRoundView : IRoundActionView
        {
            public RoundActionViewModel Model { get; private set; }
            public void Render(RoundActionViewModel model) => Model = model;
        }

        private sealed class CapturingUndoView : IUndoRestartView
        {
            public UndoRestartViewModel Model { get; private set; }
            public void Render(UndoRestartViewModel model) => Model = model;
        }

        private sealed class CapturingDiagnosticView : IDiagnosticLogView
        {
            public DiagnosticLogViewModel Model { get; private set; }
            public void Render(DiagnosticLogViewModel model) => Model = model;
        }

        private sealed class CapturingMinionChoiceView : IMinionChoiceView
        {
            public MinionChoiceViewModel Model { get; private set; }
            public int RenderCount { get; private set; }

            public void Render(MinionChoiceViewModel model)
            {
                Model = model;
                RenderCount++;
            }
        }
    }
}
