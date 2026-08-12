using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;

namespace GuardsDigital.Presentation.Match
{
    public sealed class ConfiguredMatchSceneRuntime
    {
        private readonly IMatchSession session;
        private readonly MatchFeatureCoordinator coordinator;
        private readonly ConfiguredMatchHudRenderer hud;

        public ConfiguredMatchSceneRuntime(
            BoardMapDefinition map,
            QuickGameSetupState setup,
            PlaceholderOneVsOneTestConfiguration configuration,
            IEnumerable<BoardMapDefinition> maps,
            IEnumerable<PrimitiveHeroView> heroes,
            IEnumerable<StandardMinionView> minions,
            IEnumerable<HexTileView> tiles,
            System.Collections.Generic.IEnumerable<PlaceholderObstacleView> obstacles,
            UnityEngine.Material highlightMaterial,
            ConfiguredMatchHudRenderer hudRenderer)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            hud = hudRenderer ?? throw new ArgumentNullException(nameof(hudRenderer));

            var heroViews = (heroes ?? throw new ArgumentNullException(nameof(heroes)))
                .Where(value => value != null)
                .Distinct()
                .ToArray();
            var tileViews = (tiles ?? throw new ArgumentNullException(nameof(tiles)))
                .Where(value => value != null)
                .Distinct()
                .ToArray();
            var minionViews = (minions ?? throw new ArgumentNullException(nameof(minions)))
                .Where(value => value != null)
                .Distinct()
                .ToArray();
            var obstacleViews = (obstacles ?? Array.Empty<PlaceholderObstacleView>())
                .Where(value => value != null)
                .Distinct()
                .ToArray();
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                obstacleViews.Select(view => view.Occupancy));
            session = new MatchSession(new MatchSessionInitialState(
                scenario,
                setup,
                maps,
                configuration));

            BoardView = new BoardInteractionUnityView(heroViews, tileViews, highlightMaterial, minionViews);
            HandsView = new DirectorHandsUnityView();
            RoundView = new RoundActionUnityView();
            UndoRestartView = new UndoRestartUnityView();
            DiagnosticView = new DiagnosticLogUnityView();
            BoardPresenter = new BoardInteractionPresenter(session, BoardView);
            HandsPresenter = new DirectorHandsPresenter(session, HandsView);
            RoundPresenter = new RoundActionPresenter(session, RoundView);
            UndoRestartPresenter = new UndoRestartPresenter(session, UndoRestartView);
            DiagnosticPresenter = new DiagnosticLogPresenter(session, DiagnosticView);

            BoardView.BindIntentHandlers(
                BoardPresenter.SubmitHero,
                BoardPresenter.SubmitMinion,
                BoardPresenter.SubmitDestination);
            HandsView.CardSelected += cardId => HandsPresenter.SelectCommitmentCard(cardId);
            HandsView.ConfirmRequested += () => HandsPresenter.ConfirmSelectedCard();
            HandsView.PassRequested += heroId => HandsPresenter.Pass(heroId);
            RoundView.InvocationSubmitted += invocation => RoundPresenter.Submit(invocation);
            RoundView.FastTravelSelected += selection => RoundPresenter.SelectFastTravel(selection);
            RoundPresenter.FastTravelSelectionRequested += selection =>
            {
                if (BoardPresenter.ActivateFastTravel(selection.ReplacedMovementActionDefinitionId))
                {
                    RefreshAll();
                }
            };
            UndoRestartView.UndoRequested += () => UndoRestartPresenter.Undo();
            UndoRestartView.RestartRequested += () => UndoRestartPresenter.Restart();
            DiagnosticView.ToggleRequested += () => DiagnosticPresenter.Toggle();

            hud.Configure(HandsView, RoundView, UndoRestartView, DiagnosticView);
            coordinator = new MatchFeatureCoordinator(
                BoardPresenter,
                HandsPresenter,
                RoundPresenter,
                UndoRestartPresenter,
                DiagnosticPresenter,
                projection => hud.RenderHeader(new MatchHudHeaderViewModel(
                    projection.TieBreakerState,
                    projection.Setup)));
            RefreshAll();
        }

        public IMatchSession Session => session;
        public MatchSessionProjection Projection => session.Projection;
        public QuickGameSetupState Setup => session.Projection.Setup;
        public IReadOnlyList<CardDefinition> Definitions => session.Projection.Definitions;
        public IReadOnlyList<CardInstance> Cards => session.Projection.Cards;
        public IReadOnlyList<HeroOccupancy> Heroes => session.Projection.Heroes;
        public RoundResolutionPhase Phase => session.Projection.Phase;
        public int RoundIndex => session.Projection.RoundIndex;
        public TieBreakerState TieBreakerState => session.Projection.TieBreakerState;
        public bool IsTurnActive => session.Projection.IsTurnActive;
        public SelectedInitiativeActor SelectedActor => session.Projection.SelectedActor;
        public PendingInitiativeChoice PendingInitiativeChoice => session.Projection.PendingInitiativeChoice;
        public PendingMovementChoice PendingMovementChoice => session.Projection.PendingMovementChoice;
        public PendingAttackTargetChoice PendingAttackTargetChoice => session.Projection.PendingAttackTargetChoice;
        public PendingDefenseResponseChoice PendingDefenseChoice => session.Projection.PendingDefenseChoice;
        public IReadOnlyList<ActionOffer> AvailableTurnActions => session.Projection.AvailableTurnActions;
        public IReadOnlyList<DirectorCommand> AcceptedCommands => session.Projection.AcceptedCommands;
        public IReadOnlyList<RuleEventLogEntry> DiagnosticEvents => session.Projection.DiagnosticEvents;
        public IReadOnlyList<RoundTurnSlot> TurnSlots => session.Projection.TurnSlots;
        public IReadOnlyList<CompletedRoundRecord> CompletedRounds => session.Projection.CompletedRounds;
        public BoardInteractionUnityView BoardView { get; }
        public DirectorHandsUnityView HandsView { get; }
        public RoundActionUnityView RoundView { get; }
        public UndoRestartUnityView UndoRestartView { get; }
        public DiagnosticLogUnityView DiagnosticView { get; }
        public BoardInteractionPresenter BoardPresenter { get; }
        public DirectorHandsPresenter HandsPresenter { get; }
        public RoundActionPresenter RoundPresenter { get; }
        public UndoRestartPresenter UndoRestartPresenter { get; }
        public DiagnosticLogPresenter DiagnosticPresenter { get; }
        public string LastOperationDetail { get; private set; }

        public void RefreshAll()
        {
            hud.RenderHeader(new MatchHudHeaderViewModel(
                session.Projection.TieBreakerState,
                session.Projection.Setup));
            coordinator.RefreshAll();
        }

        public bool SelectCommitmentCard(CardInstanceId cardId) =>
            HandsPresenter.SelectCommitmentCard(cardId);

        public TurnCardCommitmentStatus GetCommitmentStatus(HeroId heroId) =>
            session.Projection.GetCommitment(heroId).Status;

        public bool ConfirmSelectedCommitment() => HandsPresenter.ConfirmSelectedCard();

        public bool Pass(HeroId heroId) => HandsPresenter.Pass(heroId);

        public bool SubmitAction(ActionDefinitionId actionId)
        {
            var invocation = RoundView.Model?.Actions.SingleOrDefault(value =>
                value.Command.ActionDefinitionId.Equals(actionId));
            return RoundPresenter.Submit(invocation);
        }

        public bool SubmitEndTurn() => RoundPresenter.Submit(RoundView.Model?.EndTurnInvocation);

        public bool SelectFastTravel()
        {
            var selection = RoundView.Model?.FastTravelOffers.SingleOrDefault();
            return RoundPresenter.SelectFastTravel(selection);
        }

        public bool ContinueEndOfRound() =>
            RoundPresenter.Submit(RoundView.Model?.ContinueEndOfRoundInvocation);

        public bool SubmitInitiativeChoice(CardInstanceId cardId)
        {
            var invocation = RoundView.Model?.InitiativeChoices.SingleOrDefault(value =>
                value.Command.CardInstanceId.Equals(cardId));
            return RoundPresenter.Submit(invocation);
        }

        public bool SubmitDefenseCard(CardInstanceId cardId)
        {
            var invocation = RoundView.Model?.DefenseChoices.SingleOrDefault(value =>
                value.Command.Kind == DirectorCommandKind.ChooseDefenseCard &&
                value.Command.CardInstanceId.Equals(cardId));
            return RoundPresenter.Submit(invocation);
        }

        public bool DeclineDefense()
        {
            var invocation = RoundView.Model?.DefenseChoices.SingleOrDefault(value =>
                value.Command.Kind == DirectorCommandKind.DeclineDefense);
            return RoundPresenter.Submit(invocation);
        }

        public bool Undo() => UndoRestartPresenter.Undo();

        public bool Restart() => UndoRestartPresenter.Restart();

        public string CaptureSnapshot() => session.CaptureSnapshot();

        public bool RestoreSnapshot(string payload)
        {
            var result = session.RestoreSnapshot(payload);
            LastOperationDetail = result.Detail;
            if (result.Accepted)
            {
                RefreshAll();
            }

            return result.Accepted;
        }
    }
}
