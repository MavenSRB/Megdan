using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Effects;
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

            // Milestone 7.8a: the tray also clears the standing status strip, so the bar and the tray
            // cannot both claim the same pixels on a short viewport and hide one of two guaranteed
            // surfaces behind the other.
            //
            // Milestone 8.25c: 778 by 300 is now a PANEL rectangle in reference units rather than a
            // screen in device pixels, and the assertions below are unchanged because the arithmetic
            // this measures is unchanged - what moved is which viewport reaches it. A panel this small
            // is reached by a window whose shape is this extreme rather than by one whose pixel count
            // is, which is the whole point of the scaled panel.
            //
            // Milestone 8.30b re-measured it. The user-approved amendment widened the status strip
            // from 56 to 96 reference units - a 56-unit identity row plus a 40-unit narration lane -
            // and on a panel this short the tray height is bounded by what the strip leaves rather
            // than by its own preferred 460, so both numbers moved together: 220 tall from y 68
            // becomes 180 tall from y 108, and the tray still starts below the strip.
            Assert.That(panel, Is.EqualTo(new UnityEngine.Rect(0f, 108f, 778f, 180f)));
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(778f));
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(300f));
            Assert.That(
                panel.yMin,
                Is.GreaterThanOrEqualTo(ConfiguredMatchHudRenderer.StatusBarHeight));
            Assert.That(
                ConfiguredMatchHudRenderer.CalculateStatusBarRect(778f, 300f).yMax,
                Is.LessThanOrEqualTo(panel.yMin));
        }

        [Test]
        public void ConfiguredHudLayout_UsesExpandedDesktopPanelForCompleteDefenseRow()
        {
            var panel = ConfiguredMatchHudRenderer.CalculateCardPanelRect(2048f, 1152f);

            // Milestone 8.25f2 reserves the whole bottom width and enough height for one readable
            // 420-unit face plus its tray header and chrome. The rail columns end above that band.
            Assert.That(panel.width, Is.EqualTo(2048f));
            Assert.That(panel.x, Is.Zero);
            Assert.That(
                ConfiguredMatchHudRenderer.CalculateHeroRailRect(
                    2048f, 1152f, TeamSide.Red).yMax,
                Is.LessThanOrEqualTo(panel.yMin),
                "The Red rail column ends before the reserved tray begins.");
            Assert.That(panel.height,
                Is.EqualTo(ConfiguredMatchHudRenderer.CardPanelHeight).Within(0.01f));
            Assert.That(panel.yMax, Is.EqualTo(1152f - 12f).Within(0.01f));
            Assert.That(panel.yMin,
                Is.GreaterThanOrEqualTo(ConfiguredMatchHudRenderer.StatusBarHeight));
        }

        /// <summary>
        /// Milestone 5.1c's guarantee, restated for the surface milestone 7.8a moved it onto: a Defense
        /// control carries its complete label and nothing narrows it.
        ///
        /// It used to be a measurement, because an immediate-mode control was given an explicit width
        /// and the case existed to prove that width was not the clipping one. A retained control is
        /// content-sized unless something sets a width, so the guarantee is now structural and this
        /// reads it that way: the complete label is on the control, the label wraps rather than
        /// truncates, and no width, max-width, or text-overflow was assigned anywhere on it.
        ///
        /// <para>Milestone 8.11b narrowed what this covers, and deliberately: an offer that carries a
        /// card is drawn as that card's whole face at one fixed width, which is a stronger guarantee
        /// than a label that fits, and <c>MatchHudPresentationTests</c> is where it is checked. What is
        /// left here is the control for an offer with no card - <c>DECLINE DEFENSE</c> - which is still
        /// content-sized and must still be able to carry the longest label the model can hand it. The
        /// model below therefore carries no card on purpose.</para>
        /// </summary>
        [Test]
        public void ADefenseControlWithNoCard_CarriesTheCompleteLabelAndNothingNarrowsIt()
        {
            const string longestCurrentLabel =
                "DISCARD TEST - BLUE SCOUT SHOT\nPRINTED DEFENSE 1";
            var view = new RoundActionUnityView();
            view.Render(TestRoundModel(defenseChoices: new[]
            {
                new TypedInvocationViewModel(
                    longestCurrentLabel,
                    DirectorCommand.DeclineDefense(
                        new ActionResolutionId("r"), new ActionResolutionStepId("s"))),
                new TypedInvocationViewModel(
                    "DECLINE DEFENSE",
                    DirectorCommand.DeclineDefense(
                        new ActionResolutionId("r"), new ActionResolutionStepId("s2")))
            }));

            var controls = Buttons(view.Root).ToArray();
            Assert.That(controls.Select(button => button.text),
                Is.EquivalentTo(new[] { longestCurrentLabel, "DECLINE DEFENSE" }),
                "Every projected Defense choice is drawn, carrying its complete label.");
            foreach (var control in controls)
            {
                Assert.That(control.style.width.keyword, Is.EqualTo(UnityEngine.UIElements.StyleKeyword.Null),
                    "A Defense control that was given a width could clip the label the model carries.");
                Assert.That(control.style.maxWidth.keyword, Is.EqualTo(UnityEngine.UIElements.StyleKeyword.Null));
                Assert.That(control.style.whiteSpace.value,
                    Is.EqualTo(UnityEngine.UIElements.WhiteSpace.Normal),
                    "A long label wraps rather than being cut off.");
            }
        }

        [Test]
        public void TheDiscardChooser_NamesEachControlAsTheCardItWouldGiveUp()
        {
            // Milestone 8.15. The first player-pipeline journey met `OPTION #1` where rulebook
            // p.10's discard chooser stood: the chosen hero was picking which of their own cards to
            // give up from controls that did not say which card any of them was. The candidates are
            // card-instance values, and each control is named as its card - the projected display
            // name, exactly the reading the tray names the same card by.
            var session = ConfiguredMatchFixtures.CreateRadiusDiscardRiderSession(out var minionId);
            var red = session.Projection.Setup.Heroes
                .Single(hero => hero.TeamSide == TeamSide.Red).HeroId;
            var blue = session.Projection.Setup.Heroes
                .Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;
            Accept(session, DirectorCommand.CommitCard(
                red, HandCardOf(session, red, WaspCardContent.Shock).Id));
            Accept(session, DirectorCommand.CommitCard(
                blue, HandCardOf(session, blue, WaspCardContent.StopProjectiles).Id));
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.IsAvailable && offer.ActionDefinitionId.Equals(WaspCardContent.PrimaryAttack));
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var target = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(
                target.ResolutionId, target.ChoiceId, minionId));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.SelectInvocations.Single()));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(session.Projection.PendingEffectChoice?.ChooserId, Is.EqualTo(blue),
                "The fixture must be standing on the chosen hero's own card question.");

            var view = new RoundActionUnityView();
            new RoundActionPresenter(session, view).Refresh();

            var chooserChoices = view.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            var handNames = session.Projection.Cards
                .Where(card => card.OwnerId.Equals(blue) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .DisplayName.ToUpperInvariant())
                .ToArray();
            Assert.That(chooserChoices.Select(value => value.Label), Is.EquivalentTo(handNames),
                "One control per in-hand card, each named as the card it would give up.");
            Assert.That(chooserChoices.Select(value => value.Label),
                Has.All.Not.StartsWith("OPTION #"),
                "No anonymous ordinal survives where a card has a name.");

            // Milestone 8.15a, at the user's direction: each control carries the whole printed face,
            // built by the same reading a Defense offer's is, and the view draws it as the card.
            Assert.That(chooserChoices.Select(value => value.Card), Has.All.Not.Null,
                "A choice about a card carries the card.");
            Assert.That(
                chooserChoices.Select(value => value.Card.DisplayName.ToUpperInvariant()),
                Is.EquivalentTo(handNames),
                "and the face is the card the control gives up, one for one.");
            Assert.That(chooserChoices.Select(value => value.Card.Face.PrintedTextKind),
                Has.All.EqualTo(CardSourceContentKind.Transcribed),
                "Wasp's faces carry their transcribed words, exactly as the tray shows them.");
            var faces = Buttons(view.Root)
                .Where(button => button.name == "gd-effect-card")
                .ToArray();
            Assert.That(faces, Has.Length.EqualTo(chooserChoices.Length),
                "The region draws one whole-face control per offered card, none as a text button.");
        }

        private static CardInstance HandCardOf(
            MatchSession session, HeroId heroId, CardDefinitionId definitionId) =>
            session.Projection.Cards.Single(card =>
                card.OwnerId.Equals(heroId) &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(definitionId));

        private static IEnumerable<UnityEngine.UIElements.Button> Buttons(
            UnityEngine.UIElements.VisualElement element)
        {
            if (element is UnityEngine.UIElements.Button button)
            {
                yield return button;
            }

            foreach (var child in element.Children())
            {
                foreach (var nested in Buttons(child))
                {
                    yield return nested;
                }
            }
        }

        private static RoundActionViewModel TestRoundModel(
            IEnumerable<TypedInvocationViewModel> defenseChoices = null) =>
            new RoundActionViewModel(
                1L,
                1,
                1,
                RoundResolutionPhase.ResolvingTurn,
                "STATUS",
                Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                Array.Empty<TypedInvocationViewModel>(),
                defenseChoices ?? Array.Empty<TypedInvocationViewModel>());

        [Test]
        public void FeaturePresenters_DependOnlyOnSessionAndTheirViewPort()
        {
            AssertPresenterDependencies<BoardInteractionPresenter, IBoardInteractionView>();
            AssertPresenterDependencies<DirectorHandsPresenter, IDirectorHandsView>();
            AssertPresenterDependencies<RoundActionPresenter, IRoundActionView>();
            AssertPresenterDependencies<UndoRestartPresenter, IUndoRestartView>(directorOnly: true);
            AssertPresenterDependencies<DiagnosticLogPresenter, IDiagnosticLogView>(directorOnly: true);
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
            var targetCue = view.Model.PieceTargetCues.Single(value =>
                value.Target.Equals(BoardObjectReference.ForMinion(targetId)));
            Assert.That(targetCue.Target, Is.EqualTo(BoardObjectReference.ForMinion(targetId)));
            Assert.That(targetCue.State, Is.EqualTo(BoardTargetCueState.Eligible));
            Assert.That(targetCue.ConsequenceIntent,
                Is.EqualTo(BoardTargetConsequenceIntent.Attack),
                "Attack carries its own typed blood-red intent even when this fixture will defeat " +
                "the minion; it is not inferred as Removal from that later result.");
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
            Assert.That(view.Model.PieceTargetCues, Is.Empty,
                "The identity-keyed cue leaves with the removed target.");
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
            var targetCue = view.Model.PieceTargetCues.Single();
            Assert.That(targetCue.Target, Is.EqualTo(BoardObjectReference.ForHero(targetId)));
            Assert.That(targetCue.State, Is.EqualTo(BoardTargetCueState.Eligible));
            Assert.That(targetCue.ConsequenceIntent,
                Is.EqualTo(BoardTargetConsequenceIntent.Attack),
                "Attack's invoked operation carries the typed blood-red Attack intent without " +
                "predicting the later Defense outcome.");
            Assert.That(presenter.SubmitHero(targetId), Is.True);
            Assert.That(session.Projection.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.ChooseAttackTarget));
            Assert.That(session.Projection.AcceptedCommands.Last().HeroId, Is.EqualTo(targetId));
            Assert.That(session.Projection.AcceptedCommands.Last().MinionId.IsDefined, Is.False);
            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null);
            presenter.Refresh();
            Assert.That(view.Model.PieceTargetCues, Is.Empty,
                "Answering the target choice clears its cue before Defense continues.");
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
        public void RoundPresenter_WithholdsEndTurnWhileAHeroRespawnChoiceStands()
        {
            // Milestone 7.9c, finding F-PRES-01. A defeated hero whose next card is revealed while it is
            // off board owes p.7's respawn answer before the turn can end. The authority knows: it
            // refuses `PerformHold` with `HeroRespawnChoicePending` and publishes no turn action while
            // the choice stands. The presenter drew END TURN beside the respawn options anyway, because
            // it derived the Hold itself past a second, hand-maintained list of mandatory choices that
            // had drifted from its sibling by exactly this one entry - a control that could only ever
            // produce a rejection, which is the class milestones 6.5ac and 6.5af exist to prevent.
            var session = CreateSessionWithBlueHeroOffBoard();
            CommitBlueFirstTurnRevealingTheOffBoardHero(session);

            var pending = session.Projection.PendingHeroRespawnChoice;
            Assert.That(pending, Is.Not.Null,
                "The fixture is only the fixture while the respawn choice actually stands.");
            Assert.That(session.Projection.AvailableTurnActions, Is.Empty,
                "The authority already withholds every offer-derived control here, which is why the " +
                "one control the presenter derives itself was the only one that escaped.");

            var model = RoundActionPresenter.CreateViewModel(session.Projection);

            Assert.That(model.EndTurnInvocation, Is.Null,
                "p.7's respawn answer is mandatory, so no control may offer to end the turn instead.");
            Assert.That(model.Actions, Is.Empty,
                "And nothing else offers to start an action while the choice holds the turn open.");

            // The other half of what makes it a defect rather than a cosmetic slip: the command that
            // control carried is refused, so drawing it could only ever waste a click.
            var refused = session.Execute(
                DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId));
            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.TurnRejectionReason,
                Is.EqualTo(TurnResolutionRejectionReason.HeroRespawnChoicePending));
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

        /// <summary>
        /// Milestone 6.5c. `choose another adjacent unit and Push it 3 spaces` is answered by controls
        /// named after the units they target, and each carries the space the authority projects the
        /// push would leave that unit on. Nothing here is derived: the names come from projected
        /// occupancy and the resulting space from the projected preview.
        /// </summary>
        [Test]
        public void RoundPresenter_NamesEveryPushTargetAndCarriesItsProjectedResultingSpace()
        {
            var session = CreateNeedleSession(NeedleRedAt, NeedleBlueAt);
            StartNeedleTurn(session);
            ChooseNeedleBranch(session, NeedlePushBranch);

            var model = RoundActionPresenter.CreateViewModel(session.Projection);
            var choice = session.Projection.PendingEffectChoice;
            var blueHero = session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue);

            Assert.That(model.EffectChoiceDestinations, Is.Empty,
                "Every candidate of this branch stands on the space it names, so none is a destination.");
            Assert.That(model.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select),
                Is.False,
                "A target is offered as a named control, never as an anonymous select.");
            Assert.That(model.EffectChoiceTargets.Select(value => value.Space),
                Is.EqualTo(choice.SelectInvocations.Select(value => value.Candidate.Value.Coordinate)),
                "One control per selectable target, in the projection's own order.");

            var target = model.EffectChoiceTargets.Single(value => value.Space.Equals(NeedleBlueAt));
            Assert.That(target.Label, Is.EqualTo(
                EffectChoiceLabels.HeroTargetLabel(TeamSide.Blue, blueHero.DisplayName)),
                "The control is named after the unit it targets.");
            Assert.That(target.ResultingSpace, Is.EqualTo(NeedleBeyondBlue),
                "The projected push endpoint rides the control, so the board can light it.");
            Assert.That(target.Command.Kind, Is.EqualTo(DirectorCommandKind.AnswerEffectChoice));
            Assert.That(target.Command.EffectChoice.ResolutionId, Is.EqualTo(choice.ResolutionId));
            Assert.That(target.Command.EffectChoice.ChoiceId, Is.EqualTo(choice.ChoiceId));
        }

        /// <summary>
        /// Milestone 8.11c. A candidate that names its hero by identity rather than by coordinate is
        /// a named target like every other piece on the board, and keeps no anonymous control in the
        /// prompt.
        ///
        /// <para>Reflect Projectiles' rider is the live subject: "an enemy hero in range discards a
        /// card" publishes the hero, because what consumes the choice is a discard and a space cannot
        /// be asked for a card. Before this milestone such a candidate reached the prompt as
        /// `CANDIDATE n`, since the presenter could only recognise a candidate that carried its own
        /// coordinate.</para>
        /// </summary>
        [Test]
        public void RoundPresenter_MakesAHeroNamedByIdentityANamedTargetRatherThanAnAnonymousControl()
        {
            var session = ConfiguredMatchFixtures.CreateBlockRiderSession(
                WaspCardContent.ReflectProjectiles);
            ConfiguredDefenseCardTests.BlockTheRangedAttack(
                session, WaspCardContent.ReflectProjectiles);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            var heroCandidates = choice.SelectInvocations
                .Where(value => value.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                    value.Candidate.Value.Kind == EffectValueKind.Hero)
                .ToArray();
            Assert.That(heroCandidates, Is.Not.Empty,
                "The rider names its hero by identity, or this asserts nothing.");

            var model = RoundActionPresenter.CreateViewModel(session.Projection);

            Assert.That(model.EffectChoices.Any(value =>
                    value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select),
                Is.False,
                "No select is left in the prompt: each is answered on the piece it names.");
            Assert.That(model.EffectChoiceTargets, Has.Count.EqualTo(heroCandidates.Length),
                "One named target per hero the authority offers.");

            foreach (var invocation in heroCandidates)
            {
                var occupancy = session.Projection.Heroes
                    .Single(value => value.HeroId.Equals(invocation.Candidate.Value.HeroId));
                var setup = session.Projection.Setup.Heroes
                    .Single(value => value.HeroId.Equals(invocation.Candidate.Value.HeroId));
                var target = model.EffectChoiceTargets.Single(value =>
                    value.Space.Equals(occupancy.Coordinate));

                Assert.That(target.Label, Is.EqualTo(
                    EffectChoiceLabels.HeroTargetLabel(setup.TeamSide, setup.DisplayName)),
                    "It is named after the hero it targets, exactly as a coordinate-named one is.");
                Assert.That(target.ResultingSpace, Is.Null,
                    "A discard relocates nobody, so the target carries no resulting space.");
                Assert.That(target.Command,
                    Is.EqualTo(DirectorCommand.AnswerEffectChoice(invocation)),
                    "and it forwards the authority's own invocation for that candidate.");
            }
        }

        /// <summary>
        /// Milestone 8.11c, the other half: clicking the hero answers the choice. The click carries a
        /// `HeroId`, the board resolves it through the space that hero stands on, and the command the
        /// authority accepts is the one it offered for the candidate naming that hero.
        /// </summary>
        [Test]
        public void BoardPresenter_AnswersAHeroNamedEffectChoiceByClickingThatHero()
        {
            var session = ConfiguredMatchFixtures.CreateBlockRiderSession(
                WaspCardContent.ReflectProjectiles);
            ConfiguredDefenseCardTests.BlockTheRangedAttack(
                session, WaspCardContent.ReflectProjectiles);

            var chosen = session.Projection.PendingEffectChoice.SelectInvocations
                .First(value => value.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                    value.Candidate.Value.Kind == EffectValueKind.Hero)
                .Candidate.Value.HeroId;
            var accepted = session.Projection.AcceptedCommands.Count;
            var presenter = new BoardInteractionPresenter(session, new CapturingBoardView());

            Assert.That(presenter.SubmitHero(chosen), Is.True,
                "The click is claimed by the pending choice rather than falling through.");

            // The click carries the select, and - as it has since milestone 6.5c - the confirm that
            // completes a one-of-one choice immediately after it, so a single click finishes the
            // question rather than leaving a confirm nobody was offered.
            var submitted = session.Projection.AcceptedCommands.Skip(accepted).ToArray();
            Assert.That(submitted, Is.Not.Empty);
            Assert.That(
                submitted.All(value => value.Kind == DirectorCommandKind.AnswerEffectChoice), Is.True);
            var select = submitted.Single(value =>
                value.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            Assert.That(select.EffectChoice.Candidate.Kind,
                Is.EqualTo(EffectChoiceCandidateKind.Value));
            Assert.That(select.EffectChoice.Candidate.Value.HeroId, Is.EqualTo(chosen),
                "and it answers with the candidate naming the hero that was clicked.");
        }

        /// <summary>
        /// Milestone 6.5c. `Place your hero in a different empty space within Radius 4` offers no
        /// control at all: the board lights the spaces and a click answers the choice.
        /// </summary>
        [Test]
        public void RoundPresenter_LeavesEveryPlaceDestinationToTheBoardWithNoControl()
        {
            var session = CreateNeedleSession(NeedleRedAt, NeedleBlueAt);
            StartNeedleTurn(session);
            ChooseNeedleBranch(session, NeedlePlaceBranch);

            var model = RoundActionPresenter.CreateViewModel(session.Projection);
            var choice = session.Projection.PendingEffectChoice;

            Assert.That(model.EffectChoiceTargets, Is.Empty,
                "An empty space names no object, so no control is named after one.");
            Assert.That(model.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select),
                Is.False,
                "The anonymous OPTION #n buttons this milestone exists to remove are gone.");
            Assert.That(model.EffectChoiceDestinations.Count,
                Is.EqualTo(choice.SelectInvocations.Count));
            Assert.That(model.EffectChoiceDestinations.All(value =>
                    value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select),
                Is.True,
                "Each still carries the projected invocation a board click forwards.");
        }

        /// <summary>
        /// Milestone 6.5b. Red Bombard's `directly Move another adjacent unit up to 2 spaces` inherits
        /// both 6.5c surfaces without adding presentation of its own: the unit is a named control, and
        /// the destinations it then offers are empty spaces the board lights.
        /// </summary>
        [Test]
        public void RoundPresenter_GivesTheDirectMoveItsNamedTargetThenLeavesDestinationsToTheBoard()
        {
            var session = CreateNeedleSession(NeedleRedAt, NeedleBlueAt);
            StartBombardTurn(session);
            ChooseNeedleBranch(session, BombardMoveBranch);
            var blueHero = session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue);

            var naming = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(naming.EffectChoiceDestinations, Is.Empty);
            Assert.That(naming.EffectChoiceTargets.Single().Label, Is.EqualTo(
                EffectChoiceLabels.HeroTargetLabel(TeamSide.Blue, blueHero.DisplayName)),
                "`another adjacent unit` is a control named after the unit, exactly as a Push target is.");
            Assert.That(naming.EffectChoiceTargets.Single().ResultingSpace, Is.Null,
                "An `up to` distance is an unanswered decision, so no landing space is projected.");

            Assert.That(session.Execute(DirectorCommand.AnswerEffectChoice(
                naming.EffectChoiceTargets.Single().Command.EffectChoice)).Accepted, Is.True);

            var moving = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(moving.EffectChoiceTargets, Is.Empty,
                "Every destination is an empty space, so none names an object.");
            Assert.That(moving.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select),
                Is.False,
                "No anonymous OPTION #n button survives on either half of this branch.");
            Assert.That(moving.EffectChoiceDestinations.Count,
                Is.EqualTo(session.Projection.PendingEffectChoice.SelectInvocations.Count));
        }

        /// <summary>
        /// Milestone 6.5b. A Spawn candidate names a miniature standing nowhere, which is a value kind
        /// 6.5c's split had never met: it is not an option, and it is not a space the board can light.
        /// It is a control named after the miniature, from the declared roster rather than the board.
        /// </summary>
        [Test]
        public void RoundPresenter_NamesEverySpawnCandidateAfterTheMiniatureItWouldTakeFromTheSupply()
        {
            var session = CreateBombardSpawnSession(out var removed);
            ChooseNeedleBranch(session, BombardSpawn);
            var declared = session.Projection.Setup.Minions.Single(value =>
                value.MinionId.Equals(removed));

            var model = RoundActionPresenter.CreateViewModel(session.Projection);

            Assert.That(model.EffectChoiceTargets, Is.Empty,
                "A supply miniature stands on no space, so no board cue can point at it.");
            Assert.That(model.EffectChoiceDestinations, Is.Empty,
                "Its destination is declared by its own Spawn point rather than offered.");
            var control = model.EffectChoices.Single(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            Assert.That(control.Label, Is.EqualTo(
                EffectChoiceLabels.MinionTargetLabel(declared.TeamSide, declared.MinionType, 1)),
                "The control reads as the miniature it would put back, not as OPTION #1.");
            Assert.That(control.Command.Kind, Is.EqualTo(DirectorCommandKind.AnswerEffectChoice));

            Assert.That(session.Execute(control.Command).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation)).Accepted, Is.True);
            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "Passive forwarding of the projected command is the whole of the presenter's part.");
        }

        /// <summary>
        /// Milestone 6.2d. Milestone 6.5c wanted the catalog name here and settled for `TOKEN #n`,
        /// because 6.2's decision left presentation nothing to read one from. With the projection in
        /// place the label states what the token is, and keeps the ordinal only where the choice
        /// offers that name twice - which is the rule the minion label already follows and the reason
        /// the hero label carries no ordinal at all.
        /// </summary>
        [Test]
        public void TokenTargetLabels_NameTheTokenAndKeepAnOrdinalOnlyWhenOneWouldRepeat()
        {
            Assert.That(EffectChoiceLabels.TokenTargetLabel("TEST - Hazard", null),
                Is.EqualTo("TEST - HAZARD"),
                "One Hazard among the candidates is fully identified by its name.");
            Assert.That(EffectChoiceLabels.TokenTargetLabel("TEST - Hazard", 2),
                Is.EqualTo("TEST - HAZARD #2"),
                "Two of one kind are otherwise indistinguishable, so the ordinal stays.");
            Assert.That(EffectChoiceLabels.TokenTargetLabel("TEST - Ward", null),
                Is.EqualTo("TEST - WARD"),
                "Nothing here is a token kind this label knows about; the name is projected state.");
            Assert.That(
                () => EffectChoiceLabels.TokenTargetLabel(" ", null),
                Throws.ArgumentException,
                "A label with no projected name would be a made-up one.");
            Assert.That(
                () => EffectChoiceLabels.TokenTargetLabel("TEST - Hazard", 0),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// Milestone 6.5ac. Red Bombard's Spawn mode can do nothing while every declared miniature
        /// stands, so the authority refuses it. Presentation shows it greyed rather than hiding it -
        /// the player reads that the mode exists and cannot be taken - and it carries no command at
        /// all, so a click on it cannot reach the session by any route.
        /// </summary>
        [Test]
        public void RoundPresenter_GreysTheModeTheActorCannotPerformAndRefusesToSubmitIt()
        {
            var session = CreateNeedleSession(NeedleRedAt, NeedleBlueAt);
            StartBombardTurn(session);
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { BombardSpawn }),
                "Fixture precondition: the supply is empty and a unit stands adjacent, so exactly one " +
                "of the two modes can be performed.");

            var model = RoundActionPresenter.CreateViewModel(session.Projection);

            var greyed = model.UnavailableEffectChoices.Single();
            Assert.That(greyed.Candidate, Is.EqualTo(EffectChoiceCandidate.Option(BombardSpawn)));
            Assert.That(greyed.Label, Is.EqualTo(EffectChoiceLabels.CandidateLabel(BombardSpawn, 2)),
                "It keeps the label and the ordinal it would have had, so the refusal reads as the " +
                "same mode rather than as a different control.");
            Assert.That(
                model.EffectChoices
                    .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                    .Select(value => value.Command.EffectChoice.Candidate.OptionNodeId),
                Is.EqualTo(new[] { BombardMoveBranch }),
                "Only the performable mode carries an invocation.");

            var presenter = new RoundActionPresenter(session, new CapturingRoundView());
            var forged = new TypedInvocationViewModel(
                greyed.Label,
                DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                    choice.ResolutionId,
                    choice.ChoiceId,
                    EffectChoiceCommandKind.Select,
                    EffectChoiceCandidate.Option(BombardSpawn))));
            Assert.That(presenter.Submit(forged), Is.False,
                "The unavailable list joins the presenter's legality decision rather than sitting " +
                "outside it, so a command left over from an older model is refused here too.");
            Assert.That(session.Projection.PendingEffectChoice.SelectedCandidates, Is.Empty,
                "Nothing about the paused decision moved.");
        }

        /// <summary>
        /// Milestone 6.5af. A Skill that can apply nothing is drawn greyed rather than dropped - the
        /// treatment milestone 6.5ac gave a refused mode, on an offer - and it carries no command, so a
        /// click cannot reach the session by any route. The Movement beside it is untouched, which is
        /// the turn the old behaviour spent.
        ///
        /// This position is where milestone 6.5ae's stop surface used to be reached from. 6.5af turns
        /// it into a position the player never enters, so the stop's own presentation arm has no
        /// configured route left; it is covered at the authority level in `EffectOptionFeasibilityTests`
        /// and recorded as a material coverage limitation in 6.5af's evidence.
        /// </summary>
        [Test]
        public void RoundPresenter_GreysASkillThatCanDoNothingAndKeepsEveryOtherActionStartable()
        {
            var session = CreateNeedleSession(NeedleRedAt, new HexCoordinate(-3, 10));
            CommitBombardCards(session);
            var skill = session.Projection.AvailableTurnActions
                .Single(offer => GetActionKind(session.Projection, offer) == ActionKind.Skill);
            Assert.That(skill.IsAvailable, Is.False,
                "Fixture precondition: nothing stands adjacent and the supply is empty, so neither " +
                "mode of this Skill can be taken.");

            var model = RoundActionPresenter.CreateViewModel(session.Projection);

            var greyed = model.UnavailableActions.Single();
            Assert.That(greyed.ActionDefinitionId, Is.EqualTo(skill.ActionDefinitionId));
            Assert.That(greyed.Label, Does.StartWith("PRIMARY "),
                "It keeps the label it would have had, so the refusal reads as the same action " +
                "rather than as a different control.");
            Assert.That(model.Actions, Is.Not.Empty,
                "Every other action the card prints is still a control - the Movement above all.");
            Assert.That(
                model.Actions.Any(value =>
                    value.Command.ActionDefinitionId.Equals(skill.ActionDefinitionId)), Is.False,
                "And the Skill is not among them.");
            Assert.That(model.EndTurnInvocation, Is.Not.Null,
                "Hold is still offered throughout, which is p.12's own answer to this position.");

            var presenter = new RoundActionPresenter(session, new CapturingRoundView());
            var forged = new TypedInvocationViewModel(
                greyed.Label, DirectorCommand.StartAction(skill.ActionDefinitionId));
            Assert.That(presenter.Submit(forged), Is.False,
                "The unavailable list joins the presenter's legality decision rather than sitting " +
                "outside it, so a start left over from an older model is refused here too.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                "Nothing about the turn moved.");
        }

        /// <summary>
        /// Milestone 6.5c. The board is the whole Place surface: every offered space is lit with the
        /// same highlight Movement, Fast Travel, and corrections use, and clicking one answers the
        /// choice outright, because an exactly-one selection leaves nothing further to confirm.
        /// </summary>
        [Test]
        public void BoardPresenter_LightsEveryPlaceDestinationAndAnswersTheChoiceInOneClick()
        {
            var session = CreateNeedleSession(NeedleRedAt, NeedleBlueAt);
            StartNeedleTurn(session);
            ChooseNeedleBranch(session, NeedlePlaceBranch);
            var view = new CapturingBoardView();
            var presenter = new BoardInteractionPresenter(session, view);
            presenter.Refresh();

            // Read before the click: completing the Skill hands the turn on, so the projection's
            // selected actor is the other hero by the time the assertion runs.
            var actorId = session.Projection.SelectedActor.ActorId;
            var offered = session.Projection.PendingEffectChoice.SelectInvocations
                .Select(value => value.Candidate.Value.Coordinate)
                .ToArray();
            Assert.That(view.Model.EffectChoiceDestinations, Is.EqualTo(offered));
            Assert.That(view.Model.EffectChoiceTargetSpace, Is.Null);
            Assert.That(view.Model.EffectChoiceResultingSpace, Is.Null);

            var chosen = offered.First();
            Assert.That(presenter.SubmitDestination(chosen), Is.True);

            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "Select and the confirm the authority offers with it are both forwarded.");
            Assert.That(session.Projection.Heroes.Single(hero => hero.HeroId.Equals(actorId)).Coordinate,
                Is.EqualTo(chosen),
                "One click placed the hero on the space that was clicked.");
        }

        /// <summary>
        /// Milestone 6.5c. Pointing at a named target marks that unit and lights where the push would
        /// leave it, and both cues belong to the projection they were read from.
        /// </summary>
        [Test]
        public void BoardPresenter_MarksThePointedAtTargetAndItsProjectedResultingSpace()
        {
            var session = CreateNeedleSession(NeedleRedAt, NeedleBlueAt);
            StartNeedleTurn(session);
            ChooseNeedleBranch(session, NeedlePushBranch);
            var view = new CapturingBoardView();
            var presenter = new BoardInteractionPresenter(session, view);
            presenter.Refresh();

            Assert.That(view.Model.EffectChoiceTargetSpace, Is.Null, "Nothing is pointed at yet.");
            var blueTarget = BoardObjectReference.ForHero(BlueHeroId(session));
            var eligibleCue = view.Model.PieceTargetCues.Single(value =>
                value.Target.Equals(blueTarget));
            Assert.That(eligibleCue.State, Is.EqualTo(BoardTargetCueState.Eligible),
                "Every selectable object carries its static eligible ring before anything is hovered.");
            Assert.That(eligibleCue.ConsequenceIntent,
                Is.EqualTo(BoardTargetConsequenceIntent.Affect),
                "Push is a typed piece-affecting operation, not a removal prediction.");
            Assert.That(view.Model.PieceTargetCues.Select(value => value.Target),
                Is.EquivalentTo(session.Projection.PendingEffectChoicePreview
                    .Where(value => value.NamesObject)
                    .Select(value => value.Occupant)),
                "The board projection is keyed by the authority preview's stable identities.");
            Assert.That(presenter.HighlightEffectChoiceCandidate(NeedleBlueAt), Is.True);
            presenter.Refresh();

            Assert.That(view.Model.EffectChoiceTargetSpace, Is.EqualTo(NeedleBlueAt));
            Assert.That(view.Model.EffectChoiceResultingSpace, Is.EqualTo(NeedleBeyondBlue));
            Assert.That(view.Model.PieceTargetCues.Single(value =>
                    value.Target.Equals(blueTarget)).State,
                Is.EqualTo(BoardTargetCueState.Eligible),
                "Current hover remains reserved: pointing does not project the future hover state.");
            Assert.That(view.Model.EffectChoiceDestinations, Is.Empty,
                "The resulting space is a prediction, so it never joins the selectable set.");

            Assert.That(presenter.HighlightEffectChoiceCandidate(NeedleBlueAt), Is.False,
                "An unchanged hover does not rebuild the board on every repaint.");
            Assert.That(presenter.HighlightEffectChoiceCandidate(new HexCoordinate(99, 99)), Is.True);
            presenter.Refresh();
            Assert.That(view.Model.EffectChoiceTargetSpace, Is.Null,
                "A space the choice does not offer marks nothing.");

            Assert.That(presenter.HighlightEffectChoiceCandidate(NeedleBlueAt), Is.True);
            presenter.Refresh();
            var selectBlue = session.Projection.PendingEffectChoice.SelectInvocations.Single(value =>
                value.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                value.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                value.Candidate.Value.Coordinate.Equals(NeedleBlueAt));
            Assert.That(session.Execute(DirectorCommand.AnswerEffectChoice(selectBlue)).Accepted,
                Is.True);
            presenter.Refresh();
            var selectedCue = view.Model.PieceTargetCues.Single(value =>
                value.Target.Equals(blueTarget));
            Assert.That(selectedCue.State, Is.EqualTo(BoardTargetCueState.Selected),
                "The target's selected state comes from the pending authoritative choice.");
            Assert.That(selectedCue.ConsequenceIntent,
                Is.EqualTo(BoardTargetConsequenceIntent.Affect));
            Assert.That(view.Model.EffectChoiceTargetSpace, Is.Null,
                "The accepted selection rebuild drops the older projection's pointed-at state.");
            Assert.That(session.Undo().Accepted, Is.True);
            presenter.Refresh();
            Assert.That(view.Model.EffectChoiceTargetSpace, Is.Null,
                "Undo rebuilds authoritative state, and the cue does not outlive its projection.");
            Assert.That(view.Model.PieceTargetCues.Single(value =>
                    value.Target.Equals(blueTarget)).State,
                Is.EqualTo(BoardTargetCueState.Eligible),
                "Undo rebuilds the identity cue from restored authority rather than retaining its " +
                "selected treatment.");
        }

        /// <summary>
        /// Milestone 6.5c. A named target control answers its choice in one click too, and a control
        /// left over from an earlier projection submits nothing.
        /// </summary>
        [Test]
        public void RoundPresenter_SubmitsANamedTargetOnceAndRefusesAStaleOne()
        {
            var session = CreateNeedleSession(NeedleRedAt, NeedleBlueAt);
            StartNeedleTurn(session);
            ChooseNeedleBranch(session, NeedlePushBranch);
            var presenter = new RoundActionPresenter(session, new CapturingRoundView());
            var stale = RoundActionPresenter.CreateViewModel(session.Projection).EffectChoiceTargets
                .Single(value => value.Space.Equals(NeedleBlueAt));

            Assert.That(presenter.SubmitTarget(stale), Is.True);
            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "Select and confirm are forwarded together for an exactly-one choice.");
            Assert.That(session.Projection.Heroes.Single(hero =>
                    hero.HeroId.Equals(BlueHeroId(session))).Coordinate,
                Is.EqualTo(NeedleBeyondBlue),
                "The push resolved exactly where the control said it would.");

            Assert.That(presenter.SubmitTarget(stale), Is.False,
                "The same control belongs to a projection that has moved on.");
            Assert.That(presenter.SubmitTarget(null), Is.False);
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

        [Test]
        public void RespawnButton_HoverAndKeyboardFocusForwardItsExactBoardDestination()
        {
            var destination = new HexCoordinate(-3, 11);
            var command = DirectorCommand.RespawnHero(new HeroRespawnCommand(
                HeroRespawnCommandKind.Respawn,
                new HeroId("blue-synthetic-hero"),
                new CardInstanceId("blue-feint-card"),
                destination));
            var option = new HeroRespawnOptionViewModel(
                "RESPAWN AT SPAWN #1", destination, command);
            var model = new RoundActionViewModel(
                7,
                2,
                1,
                RoundResolutionPhase.ResolvingTurn,
                "Choose respawn",
                Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                Array.Empty<TypedInvocationViewModel>(),
                Array.Empty<TypedInvocationViewModel>(),
                heroRespawnOptions: new[] { option });
            var view = new RoundActionUnityView();
            HeroRespawnOptionViewModel highlighted = null;
            view.RespawnHighlighted += value => highlighted = value;

            view.Render(model);
            view.ForwardRespawnHighlight(option);

            Assert.That(view.HighlightedRespawn, Is.SameAs(option));
            Assert.That(highlighted, Is.SameAs(option));
            Assert.That(highlighted.Destination, Is.EqualTo(destination),
                "The board receives the authoritative button destination and can render it with " +
                "the ordinary movement highlight material.");

            view.Render(new RoundActionViewModel(
                8,
                2,
                1,
                RoundResolutionPhase.ResolvingTurn,
                "No respawn",
                Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                Array.Empty<TypedInvocationViewModel>(),
                Array.Empty<TypedInvocationViewModel>()));
            Assert.That(view.HighlightedRespawn, Is.Null,
                "A rebuilt projection that no longer offers the button clears the board preview.");
        }

        /// <summary>
        /// Milestone 8.30b named which port each presenter may hold, which is stricter than the rule
        /// this replaced. A player-facing presenter holds <see cref="IMatchClient"/> - the Director's
        /// view or one seat's, and it cannot tell which - while Undo, restart and the diagnostic log
        /// hold <see cref="IMatchSession"/>, so a seat's client can never be handed to them.
        /// </summary>
        private static void AssertPresenterDependencies<TPresenter, TView>(bool directorOnly = false)
        {
            var fields = typeof(TPresenter).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
            var port = directorOnly ? typeof(IMatchSession) : typeof(IMatchClient);
            Assert.That(fields.Any(field => field.FieldType == port), Is.True,
                typeof(TPresenter).Name + " must depend on " + port.Name + " and nothing wider.");
            Assert.That(fields.Any(field => field.FieldType == typeof(TView)), Is.True);
            Assert.That(fields.Any(field => field.FieldType.Name.EndsWith("Presenter", StringComparison.Ordinal)),
                Is.False);
            if (!directorOnly)
            {
                Assert.That(fields.Any(field => field.FieldType == typeof(IMatchSession)), Is.False,
                    typeof(TPresenter).Name + " is a player-facing surface and must not hold the " +
                    "session, whose Undo, restart, snapshot and history are the Director's alone.");
            }
        }

        private static MatchSession CreateSession()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
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

        // Milestone 6.5c's geography, the same official-map spots milestone 6.5a proved Red Needle on:
        // from `q-2_r-5` a push of 3 carries the adjacent Blue hero exactly one space, because
        // `q1_r-5` is terrain.
        private static readonly HexCoordinate NeedleRedAt = new HexCoordinate(-2, -5);
        private static readonly HexCoordinate NeedleBlueAt = new HexCoordinate(-1, -5);
        private static readonly HexCoordinate NeedleBeyondBlue = new HexCoordinate(0, -5);
        private static readonly EffectNodeId NeedlePushBranch = new EffectNodeId("needle-push-branch");
        private static readonly EffectNodeId NeedlePlaceBranch = new EffectNodeId("needle-place-branch");

        // Milestone 6.5b's card on the same geography, and the one space beside a minion Spawn point
        // that the Spawn fixture needs.
        private static readonly EffectNodeId BombardMoveBranch = new EffectNodeId("bombard-move-branch");
        private static readonly EffectNodeId BombardSpawn = new EffectNodeId("bombard-spawn");

        // Milestone 6.7a's card. The Blue hero stands at `q0_r-1` beside two Red minions, which is
        // where its Push has a real choice and where its repeat prompt is reachable in one turn.
        private static readonly ActionDefinitionId LongbowSkill =
            new ActionDefinitionId("primary-push-repeat-skill");
        private static readonly EffectNodeId LongbowRepeat = new EffectNodeId("longbow-repeat");
        private static readonly HexCoordinate LongbowBlueAt = new HexCoordinate(0, -1);
        private static readonly HexCoordinate LongbowSubjectAt = new HexCoordinate(-1, -1);

        /// <summary>
        /// Milestone 6.7a. `you may Repeat this action once` reaches the player as the confirm and the
        /// decline p.10's `may` has always projected, and this asserts that rather than assuming it:
        /// the milestone's claim is that a bounded Repeat needs no surface of its own, and a claim
        /// about presentation is measured through the presenter.
        /// </summary>
        [Test]
        public void RoundPresenter_ForwardsARepeatPromptAsAnOrdinaryConfirmOrDecline()
        {
            var session = CreateNeedleSession(NeedleRedAt, LongbowBlueAt);
            StartLongbowTurn(session);

            // The one named target this geography offers the Push, answered through the presenter so
            // the forwarded control has to be one the presenter currently projects.
            var presenter = new RoundActionPresenter(session, new CapturingRoundView());
            var target = RoundActionPresenter.CreateViewModel(session.Projection).EffectChoiceTargets
                .Single(value => value.Space.Equals(LongbowSubjectAt));
            Assert.That(presenter.SubmitTarget(target), Is.True);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.NodeId, Is.EqualTo(LongbowRepeat));
            var model = RoundActionPresenter.CreateViewModel(session.Projection);

            Assert.That(model.EffectChoiceTargets, Is.Empty, "A `may` names no object.");
            Assert.That(model.EffectChoiceDestinations, Is.Empty, "and no space.");
            Assert.That(model.Actions, Is.Empty,
                "A paused frame withholds the offers Core would refuse, repeat prompt included.");
            Assert.That(
                model.EffectChoices.Select(value => value.Label),
                Is.EqualTo(new[] { EffectChoiceLabels.ConfirmLabel, EffectChoiceLabels.DeclineLabel }),
                "Exactly the two controls the projection carries, in its order and under the labels " +
                "milestone 6.2a's `may` has used since it shipped - this conversion invents none.");
            Assert.That(
                model.EffectChoices.Select(value => value.Command.EffectChoice.Kind),
                Is.EqualTo(new[] { EffectChoiceCommandKind.Confirm, EffectChoiceCommandKind.Decline }));
            Assert.That(model.EffectChoices.All(value =>
                value.Command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                value.Command.EffectChoice.ResolutionId.Equals(choice.ResolutionId) &&
                value.Command.EffectChoice.ChoiceId.Equals(choice.ChoiceId)), Is.True,
                "Every forwarded invocation is one the projection built, identities included.");

            Assert.That(presenter.Submit(model.EffectChoices[0]), Is.True);
            var repeated = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(repeated.EffectChoiceTargets, Is.Not.Empty,
                "Confirming starts the second instance, whose own prompt the presenter forwards the " +
                "same way; nothing about a repeated instance is presented differently.");
            Assert.That(repeated.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm &&
                    value.Command.EffectChoice.ChoiceId.Value.Contains(LongbowRepeat.Value)),
                Is.False,
                "The repeated instance never offers a repeat of its own, so no control exists for one.");
        }

        /// <summary>
        /// Commits Red Bombard at Initiative 5 against Blue Longbow at 4, then spends the Red turn on
        /// p.12's Hold, because Blue Longbow sits below every Red card and so always acts second.
        /// </summary>
        private static void StartLongbowTurn(MatchSession session)
        {
            var red = GetHandCards(session, TeamSide.Red)
                .Single(card => card.Id.Value == "red-bombard-card");
            var blue = GetHandCards(session, TeamSide.Blue)
                .Single(card => card.Id.Value == "blue-longbow-card");
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            Accept(session, DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(blue.Id));
            Accept(session, DirectorCommand.StartAction(LongbowSkill));
        }

        /// <summary>
        /// Commits Red Bombard at Initiative 5 against Blue Longbow at 4, which is the only Blue card
        /// below it, so the Red hero acts first and its converted Skill is the current actor's offer.
        /// </summary>
        private static void StartBombardTurn(MatchSession session)
        {
            CommitBombardCards(session);
            var skill = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Skill);
            Assert.That(session.Execute(DirectorCommand.StartAction(skill.ActionDefinitionId)).Accepted,
                Is.True);
        }

        /// <summary>
        /// The same turn with the Skill left unstarted, for the positions milestone 6.5af refuses to
        /// start at all.
        /// </summary>
        private static void CommitBombardCards(MatchSession session)
        {
            var red = GetHandCards(session, TeamSide.Red)
                .Single(card => card.Id.Value == "red-bombard-card");
            var blue = GetHandCards(session, TeamSide.Blue)
                .Single(card => card.Id.Value == "blue-longbow-card");
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));
        }

        /// <summary>
        /// A session whose Red side is one miniature short, played through its first Minion Battle so
        /// the supply a Spawn draws from is not empty, with Red Bombard's Skill then started. p.8 has
        /// the losing team remove one, which is how an ordinary match reaches a supply at all.
        /// </summary>
        private static MatchSession CreateBombardSpawnSession(out MinionId removed)
        {
            var session = CreateNeedleSession(NeedleRedAt, NeedleBlueAt, redOutnumbered: true);
            removed = default;
            for (var step = 0; step < 64 && !removed.IsDefined; step++)
            {
                var projection = session.Projection;
                var battle = projection.PendingMinionBattleChoice;
                if (battle != null)
                {
                    removed = battle.SelectInvocations[0].MinionId;
                    Accept(session, DirectorCommand.SelectMinionBattleRemoval(battle.SelectInvocations[0]));
                    Accept(session, DirectorCommand.ConfirmMinionBattleRemovals(
                        session.Projection.PendingMinionBattleChoice.ConfirmInvocation));
                    continue;
                }

                if (projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
                    {
                        var card = GetHandCards(session, side)
                            .Where(candidate => candidate.Id.Value != "red-bombard-card" &&
                                candidate.Id.Value != "blue-longbow-card")
                            .OrderBy(candidate => session.Projection.Definitions
                                .Single(value => value.Reference.Equals(candidate.DefinitionReference))
                                .Initiative.Value)
                            .First();
                        Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
                    }

                    var tie = session.Projection.PendingInitiativeChoice;
                    if (tie != null)
                    {
                        Accept(session, DirectorCommand.ChooseTiedActor(
                            tie.OwningSide, tie.CandidateCardInstanceIds[0]));
                    }

                    continue;
                }

                if (projection.Phase == RoundResolutionPhase.EndOfRound)
                {
                    Accept(session, DirectorCommand.AdvanceEndOfRound());
                    continue;
                }

                Accept(session, DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId));
            }

            Assert.That(removed.IsDefined, Is.True, "The imbalance must reach a real removal choice.");
            while (session.Projection.Phase != RoundResolutionPhase.AwaitingCommitments)
            {
                Accept(session, DirectorCommand.AdvanceEndOfRound());
            }

            StartBombardTurn(session);
            return session;
        }

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static HeroId BlueHeroId(MatchSession session) =>
            session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;

        /// <summary>
        /// Commits Red Needle at Initiative 13 against Blue Longbow at 4, so the Red hero acts first
        /// and its converted Skill is the current actor's offer.
        /// </summary>
        private static void StartNeedleTurn(MatchSession session)
        {
            var red = GetHandCards(session, TeamSide.Red)
                .Single(card => card.Id.Value == "red-needle-card");
            var blue = GetHandCards(session, TeamSide.Blue)
                .Single(card => card.Id.Value == "blue-longbow-card");
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));
            var skill = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Skill);
            Assert.That(session.Execute(DirectorCommand.StartAction(skill.ActionDefinitionId)).Accepted,
                Is.True);
        }

        /// <summary>
        /// Answers the mode choice. A mode keeps its explicit confirm, which is exactly what milestone
        /// 6.5c leaves alone, so both invocations are sent here.
        /// </summary>
        private static void ChooseNeedleBranch(MatchSession session, EffectNodeId branch)
        {
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(session.Execute(DirectorCommand.AnswerEffectChoice(
                    choice.SelectInvocations.Single(candidate =>
                        candidate.Candidate.OptionNodeId.Equals(branch)))).Accepted,
                Is.True);
            Assert.That(session.Execute(DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation)).Accepted, Is.True);
        }

        private static MatchSession CreateNeedleSession(
            HexCoordinate redAt, HexCoordinate blueAt, bool redOutnumbered = false)
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                redOutnumbered
                    ? RedOutnumberedSetup()
                    : QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var redHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, redAt),
                    new HeroOccupancy(blueHeroId, blueAt)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>
        /// Milestone 7.9c. The Blue hero starts off board, exactly as a Defeat leaves it, so revealing
        /// its next card opens p.7's respawn choice. Built here rather than shared with
        /// <c>HeroRespawnTests</c> for the reason every session builder in this suite is fixture-local:
        /// a shared one makes an unrelated milestone's fixture change reach across suites.
        /// </summary>
        private static MatchSession CreateSessionWithBlueHeroOffBoard()
        {
            var configuration = MatchConfiguration.CreateForSkillFixture(TeamSide.Blue);
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
                setup.Heroes
                    .Where(hero => hero.TeamSide == TeamSide.Red)
                    .Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static void CommitBlueFirstTurnRevealingTheOffBoardHero(MatchSession session)
        {
            var red = session.Projection.Cards.Single(card =>
                card.Id.Value.Contains("bombard") && card.State.Location == CardLocation.Hand);
            var blue = session.Projection.Cards.Single(card =>
                card.Id.Value.Contains("feint") && card.State.Location == CardLocation.Hand);
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(blue.Id),
                "The off-board hero's card takes the turn, which is what opens the respawn choice.");
        }

        /// <summary>One fewer Red miniature, so p.8's Minion Battle removes a Red one and fills the supply.</summary>
        private static QuickGameSetupConfiguration RedOutnumberedSetup()
        {
            var full = QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne();
            var dropped = full.Minions.First(value =>
                value.TeamSide == TeamSide.Red && value.MinionType == StandardMinionType.Ranged);
            return new QuickGameSetupConfiguration(
                full.RequiredMap,
                full.WaveCount,
                full.LifeCountPerTeam,
                full.Heroes,
                full.Minions.Where(value => !value.MinionId.Equals(dropped.MinionId)));
        }

        private static MatchSession CreateAdjacentSession()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
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
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
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
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
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
            // Blue's lowest Initiative, so the Red card carrying the required action is the acting one
            // whatever the showcase conversions have done to the top of either band. Red's highest
            // Attack was Red Needle at 13 until milestone 6.5a converted it and Red Lancer at 9 took
            // over, which is below Blue's own top card.
            var blue = GetHandCards(session, TeamSide.Blue)
                .OrderBy(card => GetDefinition(session, card).Initiative.Value)
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
