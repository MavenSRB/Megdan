using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
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
            ConfiguredMatchHudRenderer hudRenderer,
            UnityEngine.Material resultingSpaceMaterial = null,
            Func<GuardsDigital.Core.Components.PlacedToken, PlacedTokenView> tokenViewFactory = null)
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
            session = new MatchSession(
                new MatchSessionInitialState(
                    scenario,
                    setup,
                    maps,
                    configuration));

            BoardView = new BoardInteractionUnityView(
                heroViews, tileViews, highlightMaterial, minionViews, resultingSpaceMaterial,
                tokenViewFactory);
            HandsView = new DirectorHandsUnityView();
            RoundView = new RoundActionUnityView();
            UndoRestartView = new UndoRestartUnityView();
            DiagnosticView = new DiagnosticLogUnityView();
            MinionChoiceView = new MinionChoiceUnityView();
            BoardPresenter = new BoardInteractionPresenter(session, BoardView);
            HandsPresenter = new DirectorHandsPresenter(session, HandsView);
            RoundPresenter = new RoundActionPresenter(session, RoundView);
            UndoRestartPresenter = new UndoRestartPresenter(session, UndoRestartView);
            DiagnosticPresenter = new DiagnosticLogPresenter(session, DiagnosticView);
            MinionChoicePresenter = new MinionChoicePresenter(session, MinionChoiceView);

            BoardView.BindIntentHandlers(
                BoardPresenter.SubmitHero,
                BoardPresenter.SubmitMinion,
                BoardPresenter.SubmitDestination);
            HandsView.CardSelected += cardId => HandsPresenter.SelectCommitmentCard(cardId);
            HandsView.ConfirmRequested += () => HandsPresenter.ConfirmSelectedCard();
            HandsView.PassRequested += heroId => HandsPresenter.Pass(heroId);
            RoundView.InvocationSubmitted += invocation => RoundPresenter.Submit(invocation);
            RoundView.FastTravelSelected += selection => RoundPresenter.SelectFastTravel(selection);
            RoundView.TargetSubmitted += target => RoundPresenter.SubmitTarget(target);
            RoundView.TargetHighlighted += target =>
            {
                // Only the board is refreshed, for the same reason the minion prompt refreshes only
                // the board: this fires inside the HUD's own draw, and rebuilding the immediate-mode
                // models mid-pass would change the control count between layout and repaint.
                //
                // Milestone 6.3a: both halves are reported. A hero or minion target is marked by the
                // space, which is how the effect definition names it; a token target is marked by the
                // identity the control carries, because the board must not read occupancy out of a
                // coordinate to find out which piece the words mean.
                var moved = BoardPresenter.HighlightEffectChoiceCandidate(target?.Space);
                moved |= BoardPresenter.HighlightTokenChoiceCandidate(
                    target?.TokenId ?? default);
                if (moved)
                {
                    BoardPresenter.Refresh();
                }
            };
            RoundView.RespawnHighlighted += option =>
            {
                if (BoardPresenter.HighlightHeroRespawnDestination(option?.Destination))
                {
                    BoardPresenter.Refresh();
                }
            };
            RoundPresenter.FastTravelSelectionRequested += selection =>
            {
                if (BoardPresenter.ActivateFastTravel(selection.ReplacedMovementActionDefinitionId))
                {
                    RefreshAll();
                }
            };
            RoundView.ClearSelected += offer =>
            {
                if (RoundPresenter.SelectClear(offer))
                {
                    RefreshAll();
                }
            };
            RoundView.ClearTokenToggled += candidate =>
            {
                if (RoundPresenter.ToggleClearToken(candidate))
                {
                    RefreshAll();
                }
            };
            RoundView.ClearConfirmed += () => RoundPresenter.ConfirmClear();
            RoundView.ClearCancelled += () =>
            {
                if (RoundPresenter.CancelClear())
                {
                    RefreshAll();
                }
            };
            RoundView.ClearTokenHighlighted += candidate =>
            {
                // Only the board, for the reason every other hover notification gives: this fires
                // inside the HUD's own draw, and rebuilding the immediate-mode models mid-pass would
                // change the control count between layout and repaint.
                if (BoardPresenter.HighlightTokenChoiceCandidate(
                    candidate?.TokenId ?? default))
                {
                    BoardPresenter.Refresh();
                }
            };
            RoundPresenter.ClearSelectionChanged += selection =>
                BoardPresenter.SetSelectedClearTokens(
                    selection?.SelectedTokenIds ??
                    Array.Empty<GuardsDigital.Core.Components.TokenInstanceId>());
            UndoRestartView.UndoRequested += () => UndoRestartPresenter.Undo();
            UndoRestartView.RestartRequested += () => UndoRestartPresenter.Restart();
            DiagnosticView.ToggleRequested += () => DiagnosticPresenter.Toggle();
            MinionChoiceView.CandidateSubmitted += candidate => MinionChoicePresenter.Submit(candidate);
            MinionChoiceView.ConfirmRequested += () => MinionChoicePresenter.Confirm();
            MinionChoiceView.CandidateHighlighted += candidate =>
            {
                // Only the board is refreshed here. This fires from inside the HUD's own draw, and
                // rebuilding the immediate-mode models mid-pass would change the control count
                // between the layout and repaint events of the same frame.
                if (BoardPresenter.HighlightMinionChoiceCandidate(
                    candidate?.Unit ?? RuleObject.None, candidate?.Destination))
                {
                    BoardPresenter.Refresh();
                }
            };

            hud.Configure(HandsView, RoundView, UndoRestartView, DiagnosticView, MinionChoiceView);
            coordinator = new MatchFeatureCoordinator(
                BoardPresenter,
                HandsPresenter,
                RoundPresenter,
                UndoRestartPresenter,
                DiagnosticPresenter,
                MinionChoicePresenter,
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
        public MinionChoiceUnityView MinionChoiceView { get; }
        public BoardInteractionPresenter BoardPresenter { get; }
        public DirectorHandsPresenter HandsPresenter { get; }
        public RoundActionPresenter RoundPresenter { get; }
        public UndoRestartPresenter UndoRestartPresenter { get; }
        public DiagnosticLogPresenter DiagnosticPresenter { get; }
        public MinionChoicePresenter MinionChoicePresenter { get; }
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

        public PendingEffectChoice PendingEffectChoice => session.Projection.PendingEffectChoice;

        /// <summary>
        /// Forwards the projected invocation for one option of a paused effect frame, addressed by the
        /// option's node. The runtime looks the invocation up in the rendered model rather than building
        /// one, so a caller cannot reach a decision presentation is not currently showing.
        /// </summary>
        public bool SubmitEffectChoiceOption(EffectNodeId optionNodeId)
        {
            var invocation = RoundView.Model?.EffectChoices.SingleOrDefault(value =>
                value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                value.Command.EffectChoice.Candidate.OptionNodeId.Equals(optionNodeId));
            return RoundPresenter.Submit(invocation);
        }

        public bool ConfirmEffectChoice()
        {
            var invocation = RoundView.Model?.EffectChoices.SingleOrDefault(value =>
                value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            return RoundPresenter.Submit(invocation);
        }

        public PendingMinionBattleChoice PendingMinionBattleChoice =>
            session.Projection.PendingMinionBattleChoice;

        public bool SubmitMinionChoiceCandidate(MinionId minionId)
        {
            var candidate = MinionChoiceView.Model?.Candidates.SingleOrDefault(value =>
                value.MinionId.Equals(minionId));
            return MinionChoicePresenter.Submit(candidate);
        }

        public bool ConfirmMinionChoice() => MinionChoicePresenter.Confirm();

        /// <summary>
        /// Milestone 6.4b: the projected invocation for one value-shaped candidate of a paused effect
        /// frame, addressed by its coordinate - the addressing 6.8k deliberately deferred until this
        /// consumer existed, because a value candidate carries no option node to name.
        /// </summary>
        public bool SubmitEffectChoiceValue(GuardsDigital.Core.Board.HexCoordinate coordinate)
        {
            // Milestone 6.5c split where a value candidate is answered: an empty space carries a
            // board-only invocation and an occupied one carries a named control. Addressing by
            // coordinate still reaches either, which is what keeps a saved-scene driver honest.
            var model = RoundView.Model;
            var target = model?.EffectChoiceTargets.SingleOrDefault(value =>
                value.Space.Equals(coordinate));
            if (target != null)
            {
                return RoundPresenter.SubmitTarget(target);
            }

            var invocation = model?.EffectChoices
                .Concat(model.EffectChoiceDestinations)
                .SingleOrDefault(value =>
                    value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    value.Command.EffectChoice.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                    value.Command.EffectChoice.Candidate.Value.Coordinate.Equals(coordinate));
            return RoundPresenter.Submit(invocation);
        }

        /// <summary>
        /// Reports which named target control is pointed at, addressed by space, for a driver with no
        /// pointer. Passing no coordinate clears the cue.
        /// </summary>
        public bool HighlightEffectChoiceTarget(GuardsDigital.Core.Board.HexCoordinate? coordinate)
        {
            var target = coordinate.HasValue
                ? RoundView.Model?.EffectChoiceTargets.SingleOrDefault(value =>
                    value.Space.Equals(coordinate.Value))
                : null;
            RoundView.ForwardTargetHighlight(target);
            var moved = BoardPresenter.HighlightEffectChoiceCandidate(target?.Space);
            moved |= BoardPresenter.HighlightTokenChoiceCandidate(target?.TokenId ?? default);
            return moved;
        }

        public IReadOnlyList<ClearOfferViewModel> ClearOffers =>
            RoundView.Model?.ClearOffers ?? Array.Empty<ClearOfferViewModel>();

        public ClearSelectionViewModel ClearSelection => RoundView.Model?.ClearSelection;

        /// <summary>
        /// Milestone 6.3a: opens the one Clear the current card offers, for a driver with no pointer.
        /// It goes through the rendered model rather than the projection, so a caller cannot reach a
        /// replacement presentation is not currently showing.
        /// </summary>
        public bool SelectClear() =>
            Refreshed(RoundPresenter.SelectClear(RoundView.Model?.ClearOffers.SingleOrDefault()));

        public bool SelectClear(ActionDefinitionId replacedAttackActionDefinitionId) =>
            Refreshed(RoundPresenter.SelectClear(RoundView.Model?.ClearOffers.SingleOrDefault(value =>
                value.ReplacedAttackActionDefinitionId.Equals(replacedAttackActionDefinitionId))));

        /// <summary>Adds or removes one offered token, addressed by its stable identity.</summary>
        public bool ToggleClearToken(GuardsDigital.Core.Components.TokenInstanceId tokenId) =>
            Refreshed(RoundPresenter.ToggleClearToken(RoundView.Model?.ClearSelection?.Candidates
                .SingleOrDefault(value => value.TokenId.Equals(tokenId))));

        /// <summary>
        /// Forwards the confirm. It needs no refresh of its own: an accepted command rebuilds the
        /// projection, which the coordinator already redraws every surface from.
        /// </summary>
        public bool ConfirmClear() => RoundPresenter.ConfirmClear();

        public bool CancelClear() => Refreshed(RoundPresenter.CancelClear());

        /// <summary>
        /// A local Clear change moves no authoritative state, so nothing redraws on its own. The
        /// pointer path refreshes from the view's own handler; a driver with no pointer takes the same
        /// step here, so both routes leave the same rendered models behind.
        /// </summary>
        private bool Refreshed(bool changed)
        {
            if (changed)
            {
                RefreshAll();
            }

            return changed;
        }

        /// <summary>
        /// Reports which Clear token control is pointed at, addressed by identity, for a driver with
        /// no pointer. Passing an undefined identity clears the cue. It reaches the board through the
        /// view, so a driver and a pointer take the identical route - and because that route already
        /// marks the board, what is returned is whether the named control was found and forwarded
        /// rather than whether this call is what moved the cue.
        /// </summary>
        public bool HighlightClearToken(GuardsDigital.Core.Components.TokenInstanceId tokenId)
        {
            var candidate = tokenId.IsDefined
                ? RoundView.Model?.ClearSelection?.Candidates.SingleOrDefault(value =>
                    value.TokenId.Equals(tokenId))
                : null;
            RoundView.ForwardClearTokenHighlight(candidate);
            BoardPresenter.HighlightTokenChoiceCandidate(candidate?.TokenId ?? default);
            return candidate != null || !tokenId.IsDefined;
        }

        public GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            PendingDisplacedMinionCorrection => session.Projection.PendingDisplacedMinionCorrection;

        /// <summary>Forwards the rendered subject control of a paused correction, addressed by minion.</summary>
        public bool SubmitCorrectionSubject(MinionId minionId)
        {
            var candidate = MinionChoiceView.Model?.Candidates.SingleOrDefault(value =>
                value.Destination == null && value.MinionId.Equals(minionId));
            return MinionChoicePresenter.Submit(candidate);
        }

        /// <summary>Forwards the rendered destination control of a paused correction, addressed by space.</summary>
        public bool SubmitCorrectionDestination(GuardsDigital.Core.Board.HexCoordinate destination)
        {
            var candidate = MinionChoiceView.Model?.Candidates.SingleOrDefault(value =>
                value.Destination != null && value.Destination.Value.Equals(destination));
            return MinionChoicePresenter.Submit(candidate);
        }

        /// <summary>
        /// Milestone 6.2d: reports which minion-choice control is pointed at, addressed by the space
        /// its candidate would use, for a driver with no pointer. Passing no coordinate clears the cue.
        /// It is the correction destination prompt's counterpart of
        /// <see cref="HighlightEffectChoiceTarget"/>, and reaches the board the same way - through the
        /// view, so a driver and a pointer take the identical route.
        /// </summary>
        public bool HighlightCorrectionDestination(
            GuardsDigital.Core.Board.HexCoordinate? destination)
        {
            var candidate = destination.HasValue
                ? MinionChoiceView.Model?.Candidates.SingleOrDefault(value =>
                    value.Destination != null && value.Destination.Value.Equals(destination.Value))
                : null;
            MinionChoiceView.ForwardHighlight(candidate);
            return BoardPresenter.HighlightMinionChoiceCandidate(
                candidate?.Unit ?? RuleObject.None, candidate?.Destination);
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
