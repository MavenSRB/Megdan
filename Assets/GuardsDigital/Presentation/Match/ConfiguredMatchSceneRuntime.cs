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
        private readonly IMatchHost host;
        private readonly IMatchSession session;
        private readonly IMatchClient client;
        private readonly MatchFeatureCoordinator coordinator;
        private readonly ConfiguredMatchHudRenderer hud;

        public ConfiguredMatchSceneRuntime(
            BoardMapDefinition map,
            QuickGameSetupState setup,
            MatchConfiguration configuration,
            IEnumerable<BoardMapDefinition> maps,
            IEnumerable<PrimitiveHeroView> heroes,
            IEnumerable<StandardMinionView> minions,
            IEnumerable<HexTileView> tiles,
            System.Collections.Generic.IEnumerable<PlaceholderObstacleView> obstacles,
            UnityEngine.Material highlightMaterial,
            ConfiguredMatchHudRenderer hudRenderer,
            UnityEngine.Material resultingSpaceMaterial = null,
            Func<GuardsDigital.Core.Components.PlacedToken, PlacedTokenView> tokenViewFactory = null,
            UnityEngine.Material highlightRimMaterial = null,
            UnityEngine.Material resultingSpaceRimMaterial = null,
            SeatId? seat = null)
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
                configuration.SetupSeed,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                obstacleViews.Select(view => view.Occupancy));
            // Milestone 8.30: the scene composes the transport-neutral host, never the authority
            // directly. Every presenter below, the Director's own controls and the headless smoke
            // driver submit through the one serialized executor a network host will also submit to,
            // so no surface here holds a session reference of its own to reach around it.
            host = MatchHost.Compose(
                new MatchSessionInitialState(
                    scenario,
                    setup,
                    maps,
                    configuration));
            session = new LocalDirectorMatchClient(host);

            // Milestone 8.30b. One composition serves both surfaces. With no seat named this is the
            // Director's runtime exactly as before; named a seat, every presenter below is handed
            // that seat's authorized view and its submissions travel in the envelope 8.30a
            // authorizes, so the same proven HUD is scoped rather than rewritten.
            Seat = seat.HasValue ? host.Seats.Find(seat.Value) : null;
            if (seat.HasValue && Seat == null)
            {
                throw new ArgumentException(
                    "No seat '" + seat.Value + "' is bound in this match.", nameof(seat));
            }

            client = Seat == null
                ? (IMatchClient)new DirectorMatchClient(session)
                : new SeatMatchClient(host.ClientForSeat(Seat.Id));

            BoardView = new BoardInteractionUnityView(
                heroViews, tileViews, highlightMaterial, minionViews, resultingSpaceMaterial,
                tokenViewFactory, highlightRimMaterial, resultingSpaceRimMaterial);
            HandsView = new DirectorHandsUnityView();
            RoundView = new RoundActionUnityView();
            UndoRestartView = new UndoRestartUnityView();
            DiagnosticView = new DiagnosticLogUnityView();
            MinionChoiceView = new MinionChoiceUnityView();
            HeroOverviewsView = new HeroOverviewsUnityView();
            BoardPresenter = new BoardInteractionPresenter(client, BoardView);
            HandsPresenter = new DirectorHandsPresenter(
                client, HandsView, Seat?.HeroId ?? default);
            HeroOverviewsPresenter = new HeroOverviewsPresenter(
                client, HeroOverviewsView, () => HandsPresenter.ViewedHeroId);
            RoundPresenter = new RoundActionPresenter(client, RoundView);

            // The Director's own controls exist only in the Director's runtime. A seat's HUD is not
            // given an Undo it must remember to hide; it is given a composition that has none.
            UndoRestartPresenter = Seat == null ? new UndoRestartPresenter(session, UndoRestartView) : null;
            DiagnosticPresenter = Seat == null ? new DiagnosticLogPresenter(session, DiagnosticView) : null;
            MinionChoicePresenter = new MinionChoicePresenter(client, MinionChoiceView);

            BoardView.BindIntentHandlers(
                // Milestone 7.8a: the map pointer is the only hero-selection control there is. A click
                // is first offered to the authority, because a hero the current choice makes a legal
                // Attack target is a spatial decision and must stay one; only when the authority offers
                // nothing does the same click become what it now also means - point the tray at that
                // hero. That path submits no command and changes no authoritative state.
                // Milestone 8.11a, at the user's request: every question the board can be asked is
                // answered on the board. Each click is offered to the authority-backed handlers in
                // turn and the first that recognises it wins; only a click nothing claims falls
                // through to pointing the tray at a hero, which submits no command and changes no
                // authoritative state. The order is a route rather than a priority - the pending
                // choices below are mutually exclusive, because the authority pauses on one at a time.
                heroId =>
                    BoardPresenter.SubmitHero(heroId) ||
                    MinionChoicePresenter.SubmitUnit(
                        GuardsDigital.Core.Rules.Modifications.RuleObject.ForHero(heroId)) ||
                    SelectViewedHero(heroId),
                minionId =>
                    BoardPresenter.SubmitMinion(minionId) ||
                    MinionChoicePresenter.SubmitUnit(
                        GuardsDigital.Core.Rules.Modifications.RuleObject.ForMinion(minionId)),
                coordinate =>
                    BoardPresenter.SubmitDestination(coordinate) ||
                    RoundPresenter.SubmitRespawnAtDestination(coordinate) ||
                    // Milestone 8.15a: an offered Clear token is toggled where it stands. The toggle
                    // is local presentation state, so nothing authoritative redraws the surfaces -
                    // the refresh here is what the removed prompt control's handler used to do.
                    Refreshed(RoundPresenter.ToggleClearTokenAtSpace(coordinate)));
            HandsView.CardSelected += cardId => HandsPresenter.SelectCommitmentCard(cardId);
            HandsView.ConfirmRequested += () => HandsPresenter.ConfirmSelectedCard();
            HandsView.CardDroppedOnMap += cardId => HandsPresenter.CommitCardImmediately(cardId);
            HandsView.PassRequested += heroId => HandsPresenter.Pass(heroId);
            HandsView.PrintedActionSubmitted += invocation => RoundPresenter.Submit(invocation);
            HandsView.FastTravelSelected += selection => RoundPresenter.SelectFastTravel(selection);
            HandsView.ClearSelected += offer =>
            {
                if (RoundPresenter.SelectClear(offer))
                {
                    RefreshAll();
                }
            };
            RoundView.InvocationSubmitted += invocation => RoundPresenter.Submit(invocation);
            RoundView.FastTravelSelected += selection => RoundPresenter.SelectFastTravel(selection);
            RoundView.TargetSubmitted += target => RoundPresenter.SubmitTarget(target);
            RoundView.TargetHighlighted += target =>
            {
                // Only the board is refreshed, for the same reason the minion prompt refreshes only
                // the board: this fires inside the HUD's own draw, and rebuilding the immediate-mode
                // models mid-pass would change the control count between layout and repaint.
                //
                // The target rings are static authority-shaped candidates. Hover only controls the
                // optional consequence preview; milestone 8.11d deliberately reserves a brighter
                // ring state without projecting current pointer state into it.
                if (BoardPresenter.HighlightEffectChoiceCandidate(target?.Space))
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
            RoundView.ClearConfirmed += () => RoundPresenter.ConfirmClear();
            RoundView.ClearCancelled += () =>
            {
                if (RoundPresenter.CancelClear())
                {
                    RefreshAll();
                }
            };
            // Milestone 8.15a: the open selection is the board's whole clear-cue input - every offered
            // token marked Eligible where it stands, every chosen one Selected - so the board is the
            // choosing surface with no pointer state involved.
            RoundPresenter.ClearSelectionChanged += selection =>
                BoardPresenter.SetOpenClearSelection(selection);
            // Milestone 8.30b: a seat-scoped runtime composes no Director presenter, so these controls
            // are wired to nothing rather than wired to something that would refuse them.
            UndoRestartView.UndoRequested += () => UndoRestartPresenter?.Undo();
            UndoRestartView.RestartRequested += () => UndoRestartPresenter?.Restart();
            DiagnosticView.ToggleRequested += () => DiagnosticPresenter?.Toggle();
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

            hud.Configure(
                HandsView, RoundView, UndoRestartView, DiagnosticView, MinionChoiceView,
                HeroOverviewsView);
            coordinator = new MatchFeatureCoordinator(
                BoardPresenter,
                HandsPresenter,
                RoundPresenter,
                UndoRestartPresenter,
                DiagnosticPresenter,
                MinionChoicePresenter,
                projection => hud.RenderHeader(new MatchHudHeaderViewModel(
                    projection.TieBreakerState,
                    projection.Setup,
                    projection.TeamLifeCounters,
                    projection.RoundIndex,
                    projection.TurnIndex)),
                HeroOverviewsPresenter);
            RefreshAll();
        }

        /// <summary>
        /// Points the tray at one hero (milestone 7.8a). Local presentation state: no command is
        /// submitted, nothing authoritative moves, and nothing about it is serialized.
        /// </summary>
        public bool SelectViewedHero(HeroId heroId)
        {
            if (!HandsPresenter.SelectViewedHero(heroId))
            {
                return false;
            }

            HeroOverviewsPresenter.Refresh();
            return true;
        }

        /// <summary>The hero whose tray is currently drawn.</summary>
        public HeroId ViewedHeroId => HandsPresenter.ViewedHeroId;

        /// <summary>Opens or closes the compact `DIRECTOR` drawer. It changes no match state.</summary>
        public void ToggleDirectorDrawer() => hud.ToggleDirectorDrawer();

        /// <summary>
        /// Milestone 8.30. The transport-neutral port this scene's authority stands behind. It is
        /// the seam milestone 8.30a scopes to six seats and 8.31 onward puts a network host on; a
        /// consumer that needs the ordered result stream reads it here.
        /// </summary>
        public IMatchHost Host => host;

        /// <summary>
        /// Milestone 8.30b. The seat this runtime was composed for, or <c>null</c> for the Director's
        /// own runtime. It is set once at construction and never moves: choosing a different seat
        /// composes a different runtime, so no live surface can be re-pointed at another player.
        /// </summary>
        public MatchSeat Seat { get; }

        public IMatchSession Session => session;
        /// <summary>
        /// The Director's own full-trust view. Milestone 8.30b left this on the session deliberately:
        /// it is the scene and fixture affordance, while every presenter and every HUD read goes
        /// through <see cref="ReadModel"/>, which is seat-scoped whenever this runtime is.
        /// </summary>
        public MatchSessionProjection Projection => session.Projection;

        /// <summary>
        /// Milestone 8.30b. What the composed surface actually reads - the Director's whole view, or
        /// one seat's authorized one when this runtime was composed for a seat.
        /// </summary>
        public IMatchReadModel ReadModel => client.Projection;
        public QuickGameSetupState Setup => client.Projection.Setup;
        public IReadOnlyList<CardDefinition> Definitions => client.Projection.Definitions;
        public IReadOnlyList<CardInstance> Cards => client.Projection.Cards;
        public IReadOnlyList<HeroOccupancy> Heroes => client.Projection.Heroes;

        /// <summary>
        /// Milestone 8.5b: the card instances currently sourcing a live Active effect, forwarded like
        /// every other projected read here so a scene-facing caller never names the projection type.
        /// </summary>
        public IReadOnlyList<CardInstanceId> ActiveEffectSourceCardInstanceIds =>
            client.Projection.ActiveEffectSourceCardInstanceIds;

        public bool IsActiveEffectSource(CardInstanceId cardInstanceId) =>
            client.Projection.IsActiveEffectSource(cardInstanceId);
        public RoundResolutionPhase Phase => client.Projection.Phase;
        public int RoundIndex => client.Projection.RoundIndex;
        public int TurnIndex => client.Projection.TurnIndex;
        public TieBreakerState TieBreakerState => client.Projection.TieBreakerState;
        public bool IsTurnActive => client.Projection.IsTurnActive;
        public SelectedInitiativeActor SelectedActor => client.Projection.SelectedActor;
        public PendingInitiativeChoice PendingInitiativeChoice => client.Projection.PendingInitiativeChoice;
        public PendingMovementChoice PendingMovementChoice => client.Projection.PendingMovementChoice;
        public PendingAttackTargetChoice PendingAttackTargetChoice => client.Projection.PendingAttackTargetChoice;
        public PendingDefenseResponseChoice PendingDefenseChoice => client.Projection.PendingDefenseChoice;
        public IReadOnlyList<ActionOffer> AvailableTurnActions => client.Projection.AvailableTurnActions;
        public IReadOnlyList<DirectorCommand> AcceptedCommands => session.Projection.AcceptedCommands;

        /// <summary>
        /// Returns every command the current authoritative projection offers, in its projected order.
        /// This is the Core-only boundary used by deterministic player and Play Mode journeys: callers
        /// can choose among real offers without taking a dependency on Application's projection type.
        /// </summary>
        public IReadOnlyList<DirectorCommand> EnumerateProjectedCommands()
        {
            var projection = client.Projection;
            var candidates = new List<DirectorCommand>();
            if (projection.Phase == RoundResolutionPhase.GameEnded)
            {
                return candidates;
            }

            if (projection.DecisionContract != null)
            {
                candidates.AddRange(projection.DecisionContract.Commands.Where(
                    IsProgressingPendingAnswer));
                return candidates;
            }

            if (projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                candidates.Add(DirectorCommand.AdvanceEndOfRound());
                return candidates;
            }

            if (projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                var pending = projection.Commitments.FirstOrDefault(value =>
                    value.Status == TurnCardCommitmentStatus.AwaitingResponse);
                if (!pending.HeroId.IsDefined)
                {
                    return candidates;
                }

                candidates.AddRange(projection.Cards
                    .Where(card => card.OwnerId.Equals(pending.HeroId) &&
                        card.State.Location == CardLocation.Hand)
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                    .Select(card => DirectorCommand.CommitCard(pending.HeroId, card.Id)));
                if (pending.CanPass)
                {
                    candidates.Add(DirectorCommand.Pass(pending.HeroId));
                }

                return candidates;
            }

            if (!projection.IsTurnActive || !projection.SelectedActor.IsDefined)
            {
                return candidates;
            }

            candidates.AddRange(projection.AvailableTurnActions
                .Where(offer => offer.IsAvailable)
                .Select(offer => DirectorCommand.StartAction(
                    offer.SourceCardInstanceId, offer.ActionDefinitionId)));
            foreach (var option in projection.AvailableFastTravelOptions)
            {
                candidates.AddRange(option.LegalDestinations.Select(destination =>
                    DirectorCommand.PerformFastTravel(
                        option.ReplacedMovementActionDefinitionId, destination)));
            }

            foreach (var option in projection.AvailableClearOptions)
            {
                candidates.AddRange(option.SubsetInvocations);
            }

            var hold = GeneralRuleActionOfferQuery.GetAvailableHoldOffer(
                projection.SelectedActor.CardInstanceId,
                projection.SelectedActor.ActorId,
                projection.Definitions,
                projection.Cards,
                RuleModificationSet.Empty);
            if (hold.HasValue && hold.Value.IsAvailable)
            {
                candidates.Add(DirectorCommand.PerformHold(
                    projection.SelectedActor.CardInstanceId));
            }

            return candidates;
        }

        private static bool IsProgressingPendingAnswer(DirectorCommand command) =>
            command.Kind != DirectorCommandKind.DeselectMinionBattleRemoval &&
            !(command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Deselect);

        /// <summary>
        /// Submits one command obtained from <see cref="EnumerateProjectedCommands"/> and refreshes
        /// every configured surface. The authority still performs the complete stale/legality check;
        /// this is only the headless journey counterpart to the individual player controls below.
        /// </summary>
        public bool SubmitProjectedCommand(DirectorCommand command)
        {
            if (command == null)
            {
                return false;
            }

            // Milestone 8.20a: a refused command records why, exactly as a refused restore does. This
            // is the Director's own surface and a refusal it cannot explain is the one thing it must
            // never be - the property was already here and only the snapshot path was setting it.
            var result = client.Submit(command);
            LastOperationDetail = result.Detail;
            if (result.Accepted)
            {
                RefreshAll();
            }

            return result.Accepted;
        }

        public IReadOnlyList<RuleEventLogEntry> DiagnosticEvents => session.Projection.DiagnosticEvents;
        public IReadOnlyList<RoundTurnSlot> TurnSlots => client.Projection.TurnSlots;
        public IReadOnlyList<CompletedRoundRecord> CompletedRounds => client.Projection.CompletedRounds;
        public BoardInteractionUnityView BoardView { get; }
        public DirectorHandsUnityView HandsView { get; }
        public RoundActionUnityView RoundView { get; }
        public UndoRestartUnityView UndoRestartView { get; }
        public DiagnosticLogUnityView DiagnosticView { get; }
        public MinionChoiceUnityView MinionChoiceView { get; }
        public HeroOverviewsUnityView HeroOverviewsView { get; }
        public HeroOverviewsPresenter HeroOverviewsPresenter { get; }
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
                client.Projection.TieBreakerState,
                client.Projection.Setup,
                client.Projection.TeamLifeCounters,
                client.Projection.RoundIndex,
                client.Projection.TurnIndex));
            coordinator.RefreshAll();
            hud.ApplyExclusiveContext();
        }

        public bool SelectCommitmentCard(CardInstanceId cardId) =>
            HandsPresenter.SelectCommitmentCard(cardId);

        public TurnCardCommitmentStatus GetCommitmentStatus(HeroId heroId) =>
            client.Projection.GetCommitment(heroId).Status;

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

        /// <summary>
        /// The paused transition's decision as the player-facing region draws it, or <c>null</c> when
        /// nothing is paused (milestone 7.8a).
        /// </summary>
        public SystemTransitionPromptViewModel SystemTransitionPrompt =>
            RoundView.Model?.SystemTransitionPrompt;

        /// <summary>
        /// Answers the paused transition through the projected control at <paramref name="optionIndex"/>,
        /// which is the path a player takes. It goes through the round presenter's own legality check, so
        /// an option from a superseded prompt executes nothing.
        /// </summary>
        public bool SubmitSystemTransitionOption(int optionIndex)
        {
            var prompt = SystemTransitionPrompt;
            if (prompt == null || optionIndex < 0 || optionIndex >= prompt.Options.Count)
            {
                return false;
            }

            var option = prompt.Options[optionIndex];
            return RoundPresenter.Submit(new TypedInvocationViewModel(option.Label, option.Command));
        }

        /// <summary>
        /// The options a paused multi-step system transition is waiting on, or an empty list when none
        /// is paused (milestone 7.6).
        ///
        /// Milestone 7.8a draws that pause, so this is no longer the only way to answer one: the
        /// contextual region now projects a labelled control per option and forwards the same command.
        /// This surface is nonetheless retained, explicitly, as the director and headless path - the
        /// build-smoke driver and every fixture without a pointer answer through it, and they must be
        /// able to reach a pause by identity rather than by finding a control. It still names an option
        /// rather than choosing one, which is the property that made it safe to keep.
        /// </summary>
        public IReadOnlyList<GuardsDigital.Core.Rules.Transitions.SystemTransitionOptionId>
            PausedSystemTransitionOptions =>
            client.Projection.PendingSystemTransitionStep?.Options ??
            (IReadOnlyList<GuardsDigital.Core.Rules.Transitions.SystemTransitionOptionId>)
                Array.Empty<GuardsDigital.Core.Rules.Transitions.SystemTransitionOptionId>();

        /// <summary>
        /// Answers the paused system transition with one of the options it projected.
        ///
        /// The option is the caller's, deliberately: choosing one here would be presentation deciding a
        /// player's choice, which is the one thing this layer must never do. An option the continuation
        /// did not offer is refused by the authority rather than filtered out here, so a stale one
        /// behaves exactly as every other stale command does.
        /// </summary>
        public bool AnswerPausedSystemTransition(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionOptionId optionId)
        {
            var pending = client.Projection.PendingSystemTransitionStep;
            var invocation = pending?.Invocations
                .FirstOrDefault(candidate => candidate.OptionId.Equals(optionId));
            if (pending == null || invocation == null || !invocation.Value.IsDefined)
            {
                return false;
            }

            var command = PendingDecisionCommandBinding.Require(
                client.Projection,
                PendingDecisionFamilies.SystemTransition,
                candidate => candidate.Kind == DirectorCommandKind.AnswerSystemTransitionStep &&
                    candidate.SystemTransitionChoice.Equals(invocation.Value));
            var accepted = client.Submit(command).Accepted;
            RefreshAll();
            return accepted;
        }

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

        public PendingEffectChoice PendingEffectChoice => client.Projection.PendingEffectChoice;

        /// <summary>
        /// Milestone 8.21. The standing removal-prevention decision, or <c>null</c> when none is. It is
        /// forwarded here beside the other pending families for the same reason they are: a caller in
        /// the Play Mode assembly reads the match through this runtime, which does not reference the
        /// application assembly the whole projection lives in.
        /// </summary>
        public GuardsDigital.Core.Rules.CardResolution.RemovalPreventionPrompt
            PendingRemovalPrevention => client.Projection.PendingRemovalPrevention;

        /// <summary>
        /// Milestone 8.21. One hero's projected coin balance, which is p.13's own reward ledger and the
        /// only thing Brogan's two Skald faces change. Forwarded as the number rather than as the
        /// progression record, on the same assembly-boundary terms as the pause above.
        /// </summary>
        public int CoinsOf(HeroId heroId) =>
            client.Projection.HeroProgression
                .Single(progression => progression.HeroId.Equals(heroId))
                .Coins;

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
            client.Projection.PendingMinionBattleChoice;

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
            return BoardPresenter.HighlightEffectChoiceCandidate(target?.Space);
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

        public GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
            PendingDisplacedMinionCorrection => client.Projection.PendingDisplacedMinionCorrection;

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

        /// <summary>The Director's Undo, and <c>false</c> from a seat-scoped runtime that has none.</summary>
        public bool Undo() => UndoRestartPresenter?.Undo() == true;

        /// <summary>The Director's restart, and <c>false</c> from a seat-scoped runtime.</summary>
        public bool Restart() => UndoRestartPresenter?.Restart() == true;

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
