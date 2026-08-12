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

            var presentationSources = new[]
            {
                typeof(HeroSelectionController),
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
            var coordinator = new MatchFeatureCoordinator(
                new BoardInteractionPresenter(session, board),
                new DirectorHandsPresenter(session, hands),
                new RoundActionPresenter(session, round),
                new UndoRestartPresenter(session, undo),
                new DiagnosticLogPresenter(session, diagnostics));

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
        public void ConfiguredPresenters_ProjectAndSubmitAuthoritativeFastTravelDestination()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var option = session.Projection.AvailableFastTravelOptions.Single();
            var roundModel = RoundActionPresenter.CreateViewModel(session.Projection);
            var boardView = new CapturingBoardView();
            var boardPresenter = new BoardInteractionPresenter(session, boardView);

            boardPresenter.Refresh();
            Assert.That(roundModel.FastTravelOffers, Has.Count.EqualTo(1));
            Assert.That(roundModel.FastTravelOffers[0].Label,
                Is.EqualTo("REPLACEMENT FAST TRAVEL\nSELECT A HIGHLIGHTED SPACE"));
            Assert.That(roundModel.FastTravelOffers[0].ReplacedMovementActionDefinitionId,
                Is.EqualTo(option.ReplacedMovementActionDefinitionId));
            Assert.That(roundModel.FastTravelOffers[0].LegalDestinationCount,
                Is.EqualTo(option.LegalDestinations.Count));
            Assert.That(boardView.Model.FastTravelDestinations,
                Is.EquivalentTo(option.LegalDestinations));

            var actorId = session.Projection.SelectedActor.ActorId;
            var destination = option.LegalDestinations[0];
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
    }
}
