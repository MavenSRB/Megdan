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
        private readonly SwitchableMatchClient client;
        private readonly IMatchReadModel trustedContent;
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
            SeatId? seat = null,
            HexCornerHighlightTemplate cornerHighlightTemplate = null,
            // TWELFTH amendment. Optional and appended, so every existing composition and fixture
            // keeps its exact call - the same reason 8.20a's heroOverviews and 8.35y's kinetics
            // were appended rather than inserted.
            MatchSessionInitialState stagedInitialState = null, ExperimentalMatchOptions experiments = null)
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
            //
            // TWELFTH amendment: a staged initial state, when the composition root loaded one, is
            // used as it stands rather than rebuilt from the scenario above. It already carries the
            // staged hands, Levels and coins, and rebuilding would deal them a second time. Every
            // line below this one is identical for a staged and a dealt match, which is the whole
            // design: from here there is nothing sandbox-shaped left.
            // Milestone 8.35z26. Retained, not just passed: a host-owned practice bot's one-ply
            // probe needs a SECOND session of the same match to try candidates on, so that a probe can
            // never damage the match being played (see ShadowSessionBotLookahead). This is the only
            // description of "the same match" the composition has, and rebuilding it from the scene a
            // second time would be a second deal.
            InitialState = stagedInitialState ??
                new MatchSessionInitialState(
                    scenario,
                    setup,
                    maps,
                    configuration, experiments: experiments);
            host = MatchHost.Compose(InitialState);
            session = new LocalDirectorMatchClient(host);
            IsSandbox = stagedInitialState != null;

            // Milestone 8.31. The admission tuple this composition publishes: what another process
            // must match before it may hold one of this match's seats. Derived here because the
            // map, configuration and setup are in scope exactly once, at composition - and the
            // ELEVENTH amendment's format element is read off the setup rather than passed beside
            // it, so the tuple describes the match that exists and not the one that was asked for.
            SessionIdentity = PrivateSessionIdentity.ForComposition(
                map, configuration, setup, BuildIdentityStamp.Current);

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

            client = new SwitchableMatchClient(Seat == null
                ? (IMatchClient)new DirectorMatchClient(session)
                : new SeatMatchClient(host.ClientForSeat(Seat.Id)));
            trustedContent = client.Projection;
            ContentCatalogs = configuration.ContentCatalogs;

            BoardView = new BoardInteractionUnityView(
                heroViews, tileViews, highlightMaterial, minionViews, resultingSpaceMaterial,
                tokenViewFactory, highlightRimMaterial, resultingSpaceRimMaterial,
                cornerHighlightTemplate);
            HandsView = new DirectorHandsUnityView();
            RoundView = new RoundActionUnityView();
            UndoRestartView = new UndoRestartUnityView();
            DiagnosticView = new DiagnosticLogUnityView();
            MinionChoiceView = new MinionChoiceUnityView();
            // Milestone 8.46c. Composed beside every other view, and scoped to the same seat: a
            // window opens only for the seat that owns the standing decision, so the Director's
            // composition draws every question and a seat's draws only its own.
            DecisionWindowView = new DecisionWindowUnityView();
            // Milestone 8.53. Composed for the Director and for every seat alike and NOT scoped to
            // either: the outcome and the match scoreboard are public information in every projection,
            // so the ended match reads the same on all six screens.
            VictoryScreenView = new VictoryScreenUnityView();
            HeroOverviewsView = new HeroOverviewsUnityView();
            // Milestone 8.35z7 (the TENTH amendment's drop-down). Composed for both the Director and a
            // seat, unlike the Director-only diagnostics beside it: the match log's own contract is
            // "the same log for everyone", so there is no scoped-out composition for it to skip.
            MatchLogView = new MatchLogUnityView();
            // Milestone 8.35z63: the board's Active-effect treatment reads the Active-effect
            // definitions this match's own content declares, so it is handed THIS composition's
            // capability catalog rather than asking the build for one - the rule
            // `EffectSeamConformanceTests` states and the offline harness enforces.
            BoardPresenter = new BoardInteractionPresenter(
                client, BoardView, ContentCatalogs?.Capabilities);
            HandsPresenter = new DirectorHandsPresenter(
                client, HandsView, Seat?.HeroId ?? default);
            // Milestone 8.61j pays milestone 8.61h's recorded debt, on exactly the terms the board
            // treatment above is composed on. 8.61h threaded THIS composition's capability catalog
            // down to the 8.55 Initiative badge so the badge can read p.16's item bonus as the rules
            // do while Min's "This round: Double your item bonuses" stands, and then had no
            // composition site to pass one from - she was not dealt, so no configured match could
            // raise the effect. She is dealt here, so the argument is supplied here: without it the
            // badge goes on reading "11+1" while the authority resolves "11+2", which is the one
            // number on the rail a player would check the card against.
            HeroOverviewsPresenter = new HeroOverviewsPresenter(
                client, HeroOverviewsView, () => HandsPresenter.ViewedHeroId,
                ContentCatalogs?.Capabilities);
            RoundPresenter = new RoundActionPresenter(client, RoundView, Seat?.HeroId ?? default);
            DecisionWindowPresenter = new DecisionWindowPresenter(
                client, DecisionWindowView, Seat?.HeroId ?? default);
            VictoryScreenPresenter = new VictoryScreenPresenter(client, VictoryScreenView);

            // Milestone 8.35z8 (SIXTH amendment part b, "old M7"). Composed independently of
            // MatchFeatureCoordinator/RoundActionPresenter, deliberately: the fizzle notice reads
            // the seat-public match log directly rather than through RoundActionViewModel, which
            // keeps this milestone's edits out of the file a parallel milestone is reworking.
            FizzleNoticePresenter = new FizzleNoticePresenter(client, RoundView, Seat?.HeroId ?? default);

            // Milestone 8.47. The phase banner reads the same projection as every panel and is
            // scoped to the same seat: "YOUR TURN" is true on exactly one of them. Its host attaches
            // to the HUD root lazily, the way the kinetic layer does, because that root does not
            // exist yet here.
            PhaseBannerView = new PhaseBannerUnityView(() => hud?.RootElement);
            PhaseBannerPresenter = new PhaseBannerPresenter(client, PhaseBannerView, Seat?.HeroId ?? default);
            // Milestone 8.46e (owed by 8.47). The banner yields to the decision window opening
            // EXPANDED on the same refresh: the window is refreshed first in the coordinator, so its
            // model already reflects this projection when the banner decides.
            PhaseBannerPresenter.YieldsWhen = () =>
                DecisionWindowView.Model?.State == DecisionWindowState.Expanded;

            // The Director's own controls exist only in the Director's runtime. A seat's HUD is not
            // given an Undo it must remember to hide; it is given a composition that has none.
            UndoRestartPresenter = Seat == null ? new UndoRestartPresenter(session, UndoRestartView) : null;
            DiagnosticPresenter = Seat == null ? new DiagnosticLogPresenter(session, DiagnosticView) : null;
            MinionChoicePresenter = new MinionChoicePresenter(client, MinionChoiceView);
            MatchLogPresenter = new MatchLogPresenter(client, MatchLogView, ContentCatalogs?.Capabilities);

            // Milestone 8.35y. The kinetic pass is composed like any other panel: a view that
            // consumes typed cues, and a presenter that derives them from the seat-public match log.
            // It is scoped to the same seat as every surface above, so a seat's client and the
            // Director's client each play the story their own projection carries.
            KineticView = new KineticCueUnityView(BoardView, hud);
            KineticPresenter = new KineticCuePresenter(
                client, KineticView, Seat?.HeroId ?? default);

            // Milestone 8.35z12 (the NINETEENTH AMENDMENT). Composed like every other panel,
            // and scoped to the same seat, so a seat's client and the Director's each frame the story
            // their own projection carries.
            SpotlightView = new AttackSpotlightUnityView(BoardView, hud);
            SpotlightPresenter = new AttackSpotlightPresenter(client, SpotlightView);
            DecisionWindowPresenter.DeferWhile(() =>
            {
                if (SpotlightPresenter.DefersCues)
                {
                    return true;
                }

                var framedAttack = SpotlightPresenter.Stage.Current;
                return framedAttack != null &&
                    !KineticView.Playback.LungeTargetFor(framedAttack.Attacker).IsDefined;
            });

            // MILESTONE 8.35z12: THE TWO CONDUCTORS BECOME ONE, AND THIS IS THE WHOLE OF IT.
            //
            // The motion pass (8.35z11) and this one independently designed the same seam, and the
            // mapping between them is exact: KineticConductor <-> IConductedScene, a held scene <->
            // the Held phase, Acknowledge() <-> the CONTINUE press. 8.35z11's is the more developed
            // piece because it paces every cue kind and not only this one, so IT is the conductor
            // that survives and this milestone's stage becomes a scene it grants.
            //
            // AutoGrantsStage exists so the spotlight could ship standalone with no conductor at all;
            // a conductor now exists, so it takes ownership here and nowhere else.
            SpotlightPresenter.Stage.AutoGrantsStage = false;
            KineticView.Conductor.ConductedScene = SpotlightPresenter.Stage;
            SpotlightView.ContinueRequested += () =>
            {
                SpotlightPresenter.Continue();
                // The CONTINUE press is ONE release for both mechanisms. Acknowledge() does nothing
                // unless a cue scene is itself holding, so this can never skip a beat the viewer has
                // not seen - the conductor's own guard, not a hope.
                KineticView.Conductor.Acknowledge();
            };

            // THE DEFERRAL SURVIVES, AGAINST THE BRANCH NOTE'S OWN RECOMMENDATION, AND THE REASON IS
            // MEASURABLE FROM THE TWO PREDICATES RATHER THAN A MATTER OF TASTE.
            //
            // worker-spotlight/NOTES.md section 10 step 4 proposed deleting this pair because "the
            // conductor already holds cues while a scene is on stage, so keeping both would hold them
            // twice". Reading the two predicates side by side shows they are NOT the same hold:
            //
            //   AttackSpotlightStage.HoldsStage       = Entering | Held | Exiting   (the conductor's)
            //   AttackSpotlightPresenter.DefersCues   = Requested | Entering | Exiting
            //                                            | (Held, for its opening PostZoomBeatSeconds)
            //
            // They differ across most of HELD, and that phase is the point of the feature. The
            // conductor's hold covers Held, so relying on it alone would keep the attack's own lunge
            // and flash queued until the spotlight had CLOSED - and the ranged smack is built on those
            // cues being released mid-hold, framed, with both faces up, one beat after the camera
            // settles ("zoom in, a small pause, then smack" - the 2026-09-08 camera-timing directive,
            // milestone 8.35z56). The deferral is what lands them there. So the conductor
            // owns WHICH scene plays and the deferral owns WHEN within a granted spotlight its cues
            // fire; they are two jobs, and only the first was ever done twice.
            KineticPresenter.DeferWhile(() => SpotlightPresenter.DefersCues);
            AttackSpotlightScene fadingAttack = null;
            var strikeStarted = false;
            BoardView.DeferDefeatFades = () =>
            {
                var scene = SpotlightPresenter.Stage.Current;
                if (!ReferenceEquals(scene, fadingAttack))
                {
                    fadingAttack = scene;
                    strikeStarted = false;
                }
                // Releasing queued cues is not the strike: earlier HUD flights can still own
                // the conductor. Retain only the decorative echo until the lunge actually starts.
                if (scene != null && KineticView.Playback.LungeTargetFor(scene.Attacker).IsDefined)
                    strikeStarted = true;
                return scene != null && (SpotlightPresenter.DefersCues || !strikeStarted);
            };
            SpotlightPresenter.CuesReleased += () => KineticPresenter.Resume();
            // Queue release is not necessarily attack start: an earlier reveal or flight can still
            // own the conductor. This first attempt handles an interruption (whose stage is gone),
            // while SceneStarted retries at each actual scene boundary. The predicate above releases
            // only when the framed attack's lunge has entered playback.
            SpotlightPresenter.CuesReleased += () => DecisionWindowPresenter.ResumePresentation();
            KineticView.SceneStarted += () => DecisionWindowPresenter.ResumePresentation();

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
                //
                // MILESTONE 8.35z48 MOVED THE TRAY-POINTING FALLBACK OUT OF THIS ARM, and the
                // sentence above is why it had to move: "only a click nothing claims" means nothing
                // in the WHOLE chain claims it, and while the fallback sat here it ran after only the
                // first third of the chain. Since it succeeds for any hero that is not already the
                // viewed one, it made a hero's pick volume swallow board clicks that grazed it - the
                // recorded anomaly where a space accepted a click and the piece did not move. The
                // three arms below are now purely authority-backed, each free to decline, and the
                // fallback is bound as the fourth handler that runs only when all of them have.
                heroId =>
                    BoardPresenter.SubmitHero(heroId) ||
                    MinionChoicePresenter.SubmitUnit(
                        GuardsDigital.Core.Rules.Modifications.RuleObject.ForHero(heroId)),
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
                    Refreshed(RoundPresenter.ToggleClearTokenAtSpace(coordinate)),
                // The cosmetic last resort of the whole chain: no command, no authoritative state,
                // and it stays a first-class feature - clicking a hero to read their tray is how a
                // player looks around, and nothing about that is changed by no longer letting it
                // pre-empt the board.
                heroId => SelectViewedHero(heroId),
                // Milestone 9.21i. A click on one standee of a hero: the body answers the acting-body
                // pick or an Attack on that body, and otherwise the click means what the hero arm means.
                (heroId, body) =>
                    BoardPresenter.SubmitHeroBody(heroId, body) ||
                    MinionChoicePresenter.SubmitUnit(
                        GuardsDigital.Core.Rules.Modifications.RuleObject.ForHero(heroId)));
            HandsView.SuggestedBy = id =>
            {
                if (!(client.Projection is IExperimentalReadModel experimental) || HeroOverviewsView?.Model == null)
                    return Array.Empty<HeroOverviewViewModel>();
                return experimental.TeammateMarkers.Where(m => m.Card.Equals(id))
                    .Select(m => HeroOverviewsView.Model.Heroes.FirstOrDefault(h => h.HeroId.Equals(m.Sender)))
                    .Where(h => h != null).ToArray();
            };
            HandsView.MyCardsRequested += () => SelectViewedHero(HandsPresenter.HomeHeroId);
            HandsView.CardSuggested += SuggestCard;
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
            RoundView.AgreementSubmitted += choice => RoundPresenter.SubmitAgreement(choice);
            RoundView.SeatUndoRequested += () => RoundPresenter.SubmitSeatUndo();
            // Milestone 8.35v: the drawn control raises this event unconditionally - whether a
            // direct invocation stands or the press must first discard an open Attack-target
            // selection - and `SubmitEndTurn` composes whichever the live projection calls for.
            RoundView.EndTurnRequested += () =>
            {
                // Milestone 8.51: every press is written to this player's own log, naming how the
                // control was activated (pointer, keyboard submit, or a driver), at which version
                // and for which actor, and whether anything composed from it - so "he says he
                // pressed nothing" is answerable from the returned log next time. The view took
                // the report at the moment of the press; the line is written after the press so
                // that it can also say what came of it.
                var pressedAgainst = client.Projection;
                var actor = pressedAgainst.SelectedActor.IsDefined
                    ? pressedAgainst.SelectedActor.ActorId.Value
                    : pressedAgainst.PendingTurnRelease?.ActorId.Value;
                var progressed = RoundPresenter.SubmitEndTurn();
                UnityEngine.Debug.Log(EndTurnPressProvenance.Marker(
                    RoundView.LastEndTurnPressReport, DateTimeOffset.UtcNow,
                    pressedAgainst.Version, actor, progressed));
            };
            RoundView.AgreementHeroHighlighted += heroId =>
            {
                BoardView.SetHoveredHero(heroId);
                HeroOverviewsView?.SetHighlightedHero(heroId);
            };
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
            MatchLogView.ToggleRequested += () => MatchLogPresenter.Toggle();
            MatchLogView.DismissRequested += () => MatchLogPresenter.Dismiss();
            MinionChoiceView.CandidateSubmitted += candidate => MinionChoicePresenter.Submit(candidate);
            MinionChoiceView.ConfirmRequested += () => MinionChoicePresenter.Confirm();
            MinionChoiceView.AgreementRequested += choice =>
                MinionChoicePresenter.SubmitAgreement(choice);
            BoardView.HeroHighlighted += heroId =>
                HeroOverviewsView?.SetHighlightedHero(heroId);
            if (HeroOverviewsView != null)
            {
                HeroOverviewsView.HeroHighlighted += heroId =>
                    BoardView.SetHoveredHero(heroId);
                HeroOverviewsView.HeroSelected += heroId => SelectViewedHero(heroId);
            }
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

            // Milestone 8.46c. The window answers through its own presenter, exactly as every other
            // surface here does: the view raises the press and composes nothing.
            DecisionWindowView.ControlActivated += control => DecisionWindowPresenter.Submit(control);
            DecisionWindowView.StateToggleRequested += () => DecisionWindowPresenter.ToggleState();
            // The same board and rail cue the strip's own Initiative preference row raised, on the
            // same two surfaces: a control about a hero points at that hero where they stand.
            DecisionWindowView.HeroHighlighted += heroId =>
            {
                BoardView.SetHoveredHero(heroId);
                HeroOverviewsView?.SetHighlightedHero(heroId);
            };
            // Milestone 8.46g. The level-up hand preview moved into the window and kept the HUD's ONE
            // read-only inspection layer, which lives on the round strip and has been shared by the
            // Defense offer and the preview since 8.35z58. The window raises the pointer; this
            // composition brings it to that layer, so a card inspected from either surface is drawn by
            // one builder at one position rather than by a second peek of the window's own.
            DecisionWindowView.CardInspected += (card, anchor) =>
            {
                if (card == null)
                {
                    RoundView.ClearCardInspection();
                }
                else
                {
                    RoundView.ShowCardInspection(card, anchor);
                }
            };

            // Milestone 8.58. The per-card Defense breakdown the strip's own offer opened on hover,
            // kept exactly where it was when the question moved into the window: "Remove it and keep
            // the old mouseover when mousing over the defense cards." One breakdown layer, one
            // builder, one dismissal clock - the window only says which card the pointer is over.
            DecisionWindowView.DefenseBreakdownPointedAt += (control, anchor) =>
            {
                if (control?.DefenseBreakdown == null || anchor == null)
                {
                    RoundView.ScheduleDefensePreviewDismissal(
                        UnityEngine.Time.realtimeSinceStartupAsDouble);
                    return;
                }

                RoundView.ShowDefensePreview(control.DefenseBreakdown, anchor.worldBound);
            };

            hud.Configure(
                HandsView, RoundView, UndoRestartView, DiagnosticView, MinionChoiceView,
                HeroOverviewsView, MatchLogView, DecisionWindowView, VictoryScreenView);
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
                    projection.TurnIndex,
                    AwaitedInputQuery.IsWaitingOnViewer(projection, Seat?.HeroId ?? default),
                    projection.RemainingWaveCounters)),
                HeroOverviewsPresenter,
                // Milestone 8.35h. One panel's throw stops that panel and nothing else, and it is
                // said out loud where the playtest logs already look: a marker naming the panel, and
                // the exception itself so the stack survives. Without a sink the coordinator raises
                // instead, which is what keeps a composition from swallowing a throw by omission.
                (panel, error) =>
                {
                    UnityEngine.Debug.LogError("GUARDS_HUD: PANEL_REFRESH_FAILED panel=" + panel);
                    UnityEngine.Debug.LogException(error);
                },
                KineticPresenter,
                MatchLogPresenter,
                SpotlightPresenter,
                DecisionWindowPresenter,
                PhaseBannerPresenter,
                VictoryScreenPresenter);
            RefreshAll();
        }

        /// <summary>
        /// Points the tray at one hero (milestone 7.8a). Local presentation state: no command is
        /// submitted, nothing authoritative moves, and nothing about it is serialized.
        /// </summary>
        public bool SuggestHex(HexCoordinate hex)
        {
            var projection = client.Projection;
            if (!(projection is IExperimentalReadModel experimental) || !experimental.Experiments.TeammateCoordination ||
                !(client is ICoordinationClient communication)) return false;
            var own = experimental.TeammateMarkers.FirstOrDefault(m => m.Sender.Equals(HandsPresenter.HomeHeroId));
            return communication.SubmitCoordination(new CoordinationCommand(own?.Revision ?? 0, CoordinationTargetKind.Hex,
                own?.Hex.HasValue == true && own.Hex.Value.Equals(hex), projection.SelectedMap.Reference, hex, generation: experimental.CoordinationGeneration));
        }
        private void SuggestCard(CardInstanceId id)
        {
            var projection = client.Projection;
            if (!(projection is IExperimentalReadModel experimental) || !experimental.Experiments.TeammateCoordination ||
                !(client is ICoordinationClient communication)) return;
            var card = experimental.AlliedHandCards.FirstOrDefault(c => c.Id.Equals(id));
            if (card == null) return;
            var own = experimental.TeammateMarkers.FirstOrDefault(m => m.Sender.Equals(HandsPresenter.HomeHeroId));
            communication.SubmitCoordination(new CoordinationCommand(own?.Revision ?? 0, CoordinationTargetKind.Card,
                own != null && own.Card.Equals(id), projection.SelectedMap.Reference, cardOwner: card.OwnerId, card: id, generation: experimental.CoordinationGeneration));
            RefreshAll();
        }

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
        /// Milestone 8.35z26. The description this match was composed from, kept so a
        /// full-information composition can build a throwaway twin of it for a one-ply probe.
        /// </summary>
        /// <remarks>
        /// Two sessions built from one of these are independent, which is a tested property rather
        /// than an assumption - see
        /// <c>BotTierTwoTests.TwoSessionsFromOneInitialStateDoNotShareState</c>. If it were ever
        /// false, a probe would be corrupting the live match through the back door.
        /// </remarks>
        public MatchSessionInitialState InitialState { get; }

        /// <summary>
        /// Milestone 8.31. The immutable admission tuple of this composition - protocol, build,
        /// snapshot schema, map revision, and registered-content digest - published for the
        /// private host and client drivers to agree on before a seat is held.
        /// </summary>
        public PrivateSessionIdentity SessionIdentity { get; }

        /// <summary>
        /// TWELFTH amendment. Whether this runtime began from a staged sandbox board rather than a
        /// dealt one. It changes no behaviour here at all - it is read by the HUD to draw the
        /// Director-only sandbox banner, so a tester can always tell which kind of board they are
        /// looking at.
        /// </summary>
        public bool IsSandbox { get; }

        public MatchContentCatalogs ContentCatalogs { get; }

        /// <summary>
        /// Milestone 8.30b/8.35b. The seat this runtime currently presents, or <c>null</c> before the
        /// interactive lobby admits an online player. A later explicit lobby join may reattach the
        /// same retained HUD to its newly admitted seat; the owner-scoped client still supplies all
        /// mutable state and authority.
        /// </summary>
        public MatchSeat Seat { get; private set; }

        /// <summary>
        /// Milestone 8.35b. Performs the lobby's one-way handoff to the already-composed HUD. The
        /// immutable local content remains the trusted dictionary for the admitted digest, while
        /// every mutable read and every submission switches to this connection's owner-scoped port.
        /// </summary>
        public void AttachOnlineClient(
            PrivateSessionClient online,
            bool exposeHostDirectorTools = false,
            Action republishAfterHostDebugChange = null)
        {
            if (online == null) throw new ArgumentNullException(nameof(online));
            if (online.GrantedSeat == null || online.LatestProjection == null || !online.HasMatchStarted)
            {
                throw new InvalidOperationException(
                    "An online HUD attaches only after seating, match start, and its first projection.");
            }

            Seat = host.Seats.Find(online.TargetSeat) ??
                throw new InvalidOperationException("The admitted seat is not in this build's participant graph.");
            client.Replace(new NetworkMatchClient(online, trustedContent, ContentCatalogs));
            BindHomeHero(Seat.HeroId);
            hud.SetDirectorToolsVisible(exposeHostDirectorTools);
            if (exposeHostDirectorTools && republishAfterHostDebugChange != null &&
                UndoRestartPresenter != null)
            {
                UndoRestartPresenter.SessionProjectionChanged +=
                    _ => republishAfterHostDebugChange();
            }
            RefreshAll();
        }

        /// <summary>
        /// Milestone 8.35z4. Every presenter whose output depends on WHICH SEAT is looking, in one
        /// list, so that "who must be told the admitted seat's hero" has exactly one answer.
        ///
        /// <para>The composition order this exists for is unavoidable: a networked client's seat is
        /// granted by the lobby, which runs after the scene has already built its presenters, so
        /// every entry below is constructed with the UNDEFINED hero and corrected here. What is
        /// avoidable - and what the first live internet playtest paid for - is a presenter being
        /// left OUT of that correction. Before this milestone the repair covered only
        /// <see cref="HandsPresenter"/>, and the two that were missed stored their identity in
        /// <c>readonly</c> fields that could not be corrected at all.</para>
        /// </summary>
        public IReadOnlyList<ISeatScopedPresenter> SeatScopedPresenters =>
            new ISeatScopedPresenter[]
            {
                HandsPresenter, RoundPresenter, KineticPresenter, FizzleNoticePresenter,
                // Milestone 8.46c. The FIFTH, and the reason it must be here: a window opens only for
                // the seat that OWNS the standing decision, and an unrepaired identity is the
                // Director's - who owns every decision at once - so a networked seat left unbound
                // would open a window for every other player's question.
                DecisionWindowPresenter,
                // Milestone 8.47. The SIXTH: "YOUR TURN" is true on exactly one seat.
                PhaseBannerPresenter
            };

        /// <summary>
        /// Milestone 8.35z4. Binds one hero as the viewer identity of EVERY seat-scoped presenter at
        /// once. It refuses loudly rather than partially: a hero absent from this build's setup is
        /// the same condition <c>DirectorHandsPresenter.SetHomeHero</c> has always refused, and a
        /// half-bound HUD - some panels reading as the seat, others as the Director - is precisely
        /// the state this fan-out exists to make unreachable.
        /// </summary>
        private void BindHomeHero(HeroId heroId)
        {
            foreach (var presenter in SeatScopedPresenters)
            {
                if (!presenter.SetHomeHero(heroId))
                {
                    throw new InvalidOperationException(
                        "The admitted seat's hero is absent from this build's setup.");
                }
            }
        }

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
            if (!projection.IsInputEnabled || projection.Phase == RoundResolutionPhase.GameEnded)
            {
                return candidates;
            }

            // Milestone 8.36g1. A hosting player is still a network seat. Use the exact bounded
            // surface the authority sent rather than reconstructing a plausible Director command
            // (notably Hold) that this seat was never authorized to submit.
            if (projection is NetworkMatchReadModel network)
            {
                candidates.AddRange(network.AuthorizedCommands);
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

        /// <summary>
        /// Submits one agreement statement through the same presenter choice the retained HUD
        /// currently renders. This is the agreement counterpart to
        /// <see cref="SubmitProjectedCommand"/> for the opt-in multi-process rehearsal: it may
        /// select only a host-published choice that is on the visible Initiative or Minion Battle
        /// surface, so the rehearsal cannot manufacture a final rules command or bypass the
        /// player-facing unanimity boundary.
        /// </summary>
        public bool SubmitProjectedAgreement(TeamAgreementCommand statement)
        {
            if (statement == null)
            {
                return false;
            }

            var roundChoice = RoundView.Model?.TeamAgreement?.Choices.FirstOrDefault(value =>
                value.Command.Equals(statement));
            if (roundChoice != null)
            {
                return RoundPresenter.SubmitAgreement(roundChoice);
            }

            var minionChoice = MinionChoiceView.Model?.Agreement?.Choices.FirstOrDefault(value =>
                value.Command.Equals(statement));
            if (minionChoice != null)
            {
                return MinionChoicePresenter.SubmitAgreement(minionChoice);
            }

            // Milestone 9.29. A Minion Battle pick has no button: a player makes it by clicking the
            // minion, and the prompt's candidate for that minion carries the statement. So a projected
            // pick is answered through exactly that candidate - the click a player would make.
            var pick = MinionChoiceView.Model?.Candidates.FirstOrDefault(value =>
                statement.Equals(value.AgreementCommand));
            return pick != null && MinionChoicePresenter.Submit(pick);
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

        /// <summary>
        /// Milestone 8.46c. The one surface that asks the player a question, and its presenter.
        /// Public for the reason every other pair here is: a journey reads the standing decision's own
        /// labels off the model rather than pressing a literal string, which is what lets milestone
        /// 8.46a reword every control in the game without touching a journey.
        /// </summary>
        public DecisionWindowUnityView DecisionWindowView { get; }

        public HeroOverviewsUnityView HeroOverviewsView { get; }
        public HeroOverviewsPresenter HeroOverviewsPresenter { get; }

        /// <summary>Milestone 8.35y. The kinetic surface and its presenter.</summary>
        public KineticCueUnityView KineticView { get; }
        public KineticCuePresenter KineticPresenter { get; }

        /// <summary>Milestone 8.35z12. Public so a Play Mode case can assert what is framed.</summary>
        public AttackSpotlightUnityView SpotlightView { get; }

        public AttackSpotlightPresenter SpotlightPresenter { get; }

        /// <summary>
        /// Milestone 8.35z7 (the TENTH amendment's drop-down). The narration bar's match log and its
        /// presenter - composed for every viewer, Director included, since the contract's own log is
        /// the same for everyone.
        /// </summary>
        public MatchLogUnityView MatchLogView { get; }
        public MatchLogPresenter MatchLogPresenter { get; }
        public BoardInteractionPresenter BoardPresenter { get; }
        public DirectorHandsPresenter HandsPresenter { get; }
        public RoundActionPresenter RoundPresenter { get; }

        /// <summary>Milestone 8.35z8. The fizzle notice's own presenter; see its own doc for why.</summary>
        public FizzleNoticePresenter FizzleNoticePresenter { get; }
        /// <summary>Milestone 8.47. The phase banner and its presenter.</summary>
        public PhaseBannerUnityView PhaseBannerView { get; }
        public PhaseBannerPresenter PhaseBannerPresenter { get; }
        public UndoRestartPresenter UndoRestartPresenter { get; }
        public DiagnosticLogPresenter DiagnosticPresenter { get; }
        public MinionChoicePresenter MinionChoicePresenter { get; }

        /// <summary>Milestone 8.46c. The decision window's own seat-scoped presenter.</summary>
        public DecisionWindowPresenter DecisionWindowPresenter { get; }

        /// <summary>
        /// Milestone 8.53. The ended-match screen and its presenter. Public on the same terms as every
        /// other pair here: a journey reads the printed banner, cause and rows off the model rather
        /// than re-deriving what it expects to see.
        /// </summary>
        public VictoryScreenUnityView VictoryScreenView { get; }

        public VictoryScreenPresenter VictoryScreenPresenter { get; }

        public string LastOperationDetail { get; private set; }

        public void RefreshAll()
        {
            HandsView.CoordinationEnabled = (client.Projection as IExperimentalReadModel)?.Experiments.TeammateCoordination == true;
            hud.RenderHeader(new MatchHudHeaderViewModel(
                client.Projection.TieBreakerState,
                client.Projection.Setup,
                client.Projection.TeamLifeCounters,
                client.Projection.RoundIndex,
                client.Projection.TurnIndex,
                AwaitedInputQuery.IsWaitingOnViewer(client.Projection, Seat?.HeroId ?? default),
                client.Projection.RemainingWaveCounters));
            coordinator.RefreshAll();
            FizzleNoticePresenter.Refresh();
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

        /// <summary>
        /// Milestone 8.35v (the THIRD amendment's REVISED SEMANTICS): the one press that ends the
        /// turn, whatever state it finds it in - a direct completion, or first discarding the seat's
        /// one open, uncommitted Attack-target selection. Every existing caller of this method keeps
        /// working unchanged; what changed is what one press now does on its behalf.
        /// </summary>
        public bool SubmitEndTurn() => RoundPresenter.SubmitEndTurn();

        /// <summary>
        /// Milestone 8.35f. Presses this seat's own UNDO square, through the same presenter path
        /// the drawn control raises.
        /// </summary>
        public bool SubmitSeatUndo() => RoundPresenter.SubmitSeatUndo();

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
            // Milestone 8.35u: every open item's options, since p.15 can hold one per hero and
            // asking the pause for "the" option list throws when several stand.
            client.Projection.PendingSystemTransitionStep?.AllOptions ??
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

        /// <summary>
        /// Milestone 8.35g. The finished turn waiting for its own player, when one stands. Read by
        /// the journeys' headless filler steps, which have to hand a turn on before they can read a
        /// selection or a commitment phase - the acknowledgement that raised the pause is what
        /// empties the selection they used to steer by.
        /// </summary>
        public GuardsDigital.Core.Cards.PendingTurnRelease PendingTurnRelease =>
            client.Projection.PendingTurnRelease;

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
