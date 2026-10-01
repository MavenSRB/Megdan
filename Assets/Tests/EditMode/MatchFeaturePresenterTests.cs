using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
using UnityEngine.UIElements;

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
                        new ActionResolutionId("r"), new ActionResolutionStepId("s")))
            }));

            var controls = Buttons(view.Root).ToArray();
            Assert.That(controls.Select(button => button.text),
                Is.EquivalentTo(new[] { longestCurrentLabel }),
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
            // Milestone 8.46b: the completion is asked for rather than assumed, and aimed at the choice
            // this answer went into - one admitting exactly one selection is closed by the select itself.
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(session, 
                session.Projection.PendingEffectChoice.SelectInvocations.Single());
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

        /// <summary>
        /// Milestone 8.35h. One panel throwing leaves every other panel rendering, and the failure is
        /// named rather than swallowed.
        ///
        /// <para>The throw is staged in a view rather than by breaking a presenter, because the claim
        /// is about the coordinator's isolation and not about any particular presenter's defect. It is
        /// the same position the invited playtest hit one layer in, where a view model's constructor
        /// threw inside <c>DirectorHandsPresenter.Refresh</c> and silently took the five panels behind
        /// it with it on every projection change.</para>
        ///
        /// <para>The round panel is chosen deliberately: it is third of seven, so the case proves both
        /// that the two panels ahead of it still rendered and that the four behind it - which is
        /// exactly what used to be skipped - rendered too.</para>
        /// </summary>
        [Test]
        public void OneThrowingPresenter_LeavesEveryOtherPanelRenderingAndNamesTheFailure()
        {
            var session = CreateSession();
            var board = new CapturingBoardView();
            var hands = new CapturingHandsView();
            var round = new ThrowingRoundView();
            var undo = new CapturingUndoView();
            var diagnostics = new CapturingDiagnosticView();
            var minionChoice = new CapturingMinionChoiceView();
            var reported = new List<string>();
            var coordinator = new MatchFeatureCoordinator(
                new BoardInteractionPresenter(session, board),
                new DirectorHandsPresenter(session, hands),
                new RoundActionPresenter(session, round),
                new UndoRestartPresenter(session, undo),
                new DiagnosticLogPresenter(session, diagnostics),
                new MinionChoicePresenter(session, minionChoice),
                panelRefreshFailed: (panel, error) => reported.Add(panel + ":" + error.GetType().Name));

            Assert.DoesNotThrow(() => coordinator.RefreshAll(),
                "A composed HUD reports its broken panel rather than raising through the whole refresh.");

            Assert.That(round.Attempts, Is.EqualTo(1), "The failing panel was still attempted.");
            Assert.That(board.Model.Version, Is.EqualTo(session.Projection.Version));
            Assert.That(hands.Model.Version, Is.EqualTo(session.Projection.Version),
                "The panels ahead of the failing one keep the projection they already had.");
            Assert.That(undo.Model.Version, Is.EqualTo(session.Projection.Version));
            Assert.That(diagnostics.Model.Version, Is.EqualTo(session.Projection.Version));
            Assert.That(minionChoice.RenderCount, Is.EqualTo(1),
                "The four panels behind the failing one are exactly what a straight-line fan-out " +
                "used to skip, so their rendering is the whole claim.");
            Assert.That(coordinator.FailedPanelNames, Is.EqualTo(new[] { "round" }),
                "The coordinator publishes which panel failed, so a surface can say so.");
            Assert.That(reported, Is.EqualTo(new[] { "round:InvalidOperationException" }),
                "and the sink is told the panel and the exception behind it.");
        }

        /// <summary>
        /// Milestone 8.35h. With no failure sink the throw still arrives - after every other panel has
        /// refreshed - so isolation cannot be turned into silent swallowing by a composition that
        /// simply forgot to wire one.
        /// </summary>
        [Test]
        public void AThrowingPanelWithNoSink_RaisesOnlyAfterEveryOtherPanelHasRefreshed()
        {
            var session = CreateSession();
            var board = new CapturingBoardView();
            var minionChoice = new CapturingMinionChoiceView();
            var coordinator = new MatchFeatureCoordinator(
                new BoardInteractionPresenter(session, board),
                new DirectorHandsPresenter(session, new CapturingHandsView()),
                new RoundActionPresenter(session, new ThrowingRoundView()),
                new UndoRestartPresenter(session, new CapturingUndoView()),
                new DiagnosticLogPresenter(session, new CapturingDiagnosticView()),
                new MinionChoicePresenter(session, minionChoice));

            var raised = Assert.Throws<AggregateException>(() => coordinator.RefreshAll());

            Assert.That(raised.Message, Does.Contain("round"), "The raise names the panel that failed.");
            Assert.That(raised.InnerExceptions, Has.Count.EqualTo(1));
            Assert.That(board.Model.Version, Is.EqualTo(session.Projection.Version));
            Assert.That(minionChoice.RenderCount, Is.EqualTo(1),
                "The panel behind the failing one refreshed before the throw was re-raised.");
            Assert.That(coordinator.FailedPanelNames, Is.EqualTo(new[] { "round" }));
        }

        /// <summary>
        /// Milestone 8.35h. The Initiative agreement reads a candidate it cannot name with the same
        /// tolerance its hero-name lookup already had, instead of a bare indexer and a `Single`.
        ///
        /// <para>Those three readings sat in one method beside a `TryGetValue` fallback, which is the
        /// shape milestone 8.35e's scoped-seat crash arrived in: a presenter shared between the
        /// Director and a seat assuming the Director's complete collections. This poses the awkward
        /// projection directly rather than waiting for a filtering path to produce one, because the
        /// claim is about the reader's tolerance and not about which projection reaches it.</para>
        /// </summary>
        [Test]
        public void AnInitiativeAgreementNamingAnUnknownCandidate_DegradesRatherThanThrowing()
        {
            var session = CreateSession();
            var known = GetHandCards(session, TeamSide.Blue)[0].Id;
            var unknown = new CardInstanceId("card-this-seat-cannot-see");
            var proposal = new TeamAgreementProposalId("initiative-tolerance");
            var blueHero = session.Projection.Setup.Heroes
                .First(hero => hero.TeamSide == TeamSide.Blue).HeroId;

            var agreement = new TeamAgreementProjection(
                TeamAgreementKind.Initiative,
                proposal,
                TeamSide.Blue,
                new[] { new TeamAgreementCandidateProjection(known, blueHero) },
                new[]
                {
                    new TeamAgreementParticipantProjection(blueHero, true, unknown)
                },
                0,
                new[] { TeamAgreementCommand.Prefer(proposal, unknown) });

            TeamAgreementHudViewModel model = null;
            Assert.DoesNotThrow(
                () => model = TeamAgreementHudPresentation.Create(
                    new AgreementReadModel(session.Projection, agreement)),
                "A candidate the projection does not carry must not take the panel down.");

            Assert.That(model, Is.Not.Null);
            Assert.That(model.Choices.Single().Label, Is.EqualTo(unknown.Value.ToUpperInvariant()),
                "The unnamed candidate degrades to its own identity rather than to an exception.");
            Assert.That(model.Choices.Single().HeroId.IsDefined, Is.False,
                "and names no hero, which is the same absence the non-Initiative arm already passes.");
            Assert.That(model.Participants.Single().Status,
                Is.EqualTo("chose " + unknown.Value.ToUpperInvariant()));
        }

        // -------------------------------------------------------------------------------------
        // The waiting-on predicate and the gold top bar
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// The wait this closes. A commitment is not one of the declared pending-decision families
        /// and exposes no round-action control, because the hand owns it - so the reading the bar
        /// used before this milestone, a disjunction over offered controls, was calm through the
        /// most common wait in the game. The case asserts the gap and its closure together: the two
        /// facts the old reading had are both absent, and the bar is gold anyway.
        /// </summary>
        [Test]
        public void AnAwaitedCommitmentFromTheViewer_LightsTheBarTheOldReadingLeftCold()
        {
            var session = CreateSession();
            var viewer = session.Projection.Setup.Heroes.First().HeroId;
            var projection = new CommitmentReadModel(
                session.Projection, Awaiting(session.Projection, viewer));

            Assert.That(projection.DecisionContract, Is.Null,
                "No decision stands during the commitment, which is half of why it was missed.");
            Assert.That(projection.AvailableTurnActions.Any(action => action.IsAvailable), Is.False,
                "and the hand owns the commitment, so no turn action is offered either.");

            Assert.That(AwaitedInputQuery.IsWaitingOnViewer(projection, viewer), Is.True,
                "The match is waiting on this player to commit a card, which is a wait.");
        }

        /// <summary>
        /// The same board with the commitment already made is not a wait, which is what keeps the
        /// case above from passing for any reason at all.
        /// </summary>
        [Test]
        public void ACommittedViewer_IsNotWaitedOnForTheCommitment()
        {
            var session = CreateSession();
            var viewer = session.Projection.Setup.Heroes.First().HeroId;
            var projection = new CommitmentReadModel(
                session.Projection, Committed(session.Projection, viewer));

            Assert.That(AwaitedInputQuery.IsWaitingOnViewer(projection, viewer), Is.False);
        }

        /// <summary>
        /// A decision standing on another seat leaves this seat's bar cold. A seat projection
        /// publishes the contract only to the owner, and the scoped offer set is empty while any
        /// decision stands - which is why the selected actor is not what the turn arm reads: an
        /// attacker is still the actor while the defender is the one being asked.
        /// </summary>
        [Test]
        public void ADecisionStandingOnAnotherSeat_LeavesThisViewersBarCold()
        {
            var session = CreateSession();
            var viewer = session.Projection.Setup.Heroes.First().HeroId;
            var projection = new OfferlessReadModel(
                session.Projection, Committed(session.Projection, viewer));

            Assert.That(projection.SelectedActor.IsDefined || !projection.SelectedActor.IsDefined,
                Is.True, "The case is about the offer set rather than about who the actor is.");
            Assert.That(AwaitedInputQuery.IsWaitingOnViewer(projection, viewer), Is.False,
                "Nothing is offered, nothing is owned, and no commitment is owed.");
        }

        /// <summary>An ended match is a terminal state rather than another wait (milestone 8.30c's reading).</summary>
        [Test]
        public void AnEndedMatch_IsNeverAWait()
        {
            var session = CreateSession();
            var viewer = session.Projection.Setup.Heroes.First().HeroId;
            var projection = new EndedReadModel(
                session.Projection, Awaiting(session.Projection, viewer));

            Assert.That(AwaitedInputQuery.IsWaitingOnViewer(projection, viewer), Is.False);
        }

        /// <summary>
        /// The conformance hold. Every declared pending-decision family reaches the bar, and it does
        /// so without this query naming a single family: the contract is family-complete by
        /// construction, so a twelfth family lights the bar the day it is declared. The rule is the
        /// same one that makes the status derivation name every family, asked of the other surface
        /// that must not miss one.
        /// </summary>
        [Test]
        public void EveryDeclaredDecisionFamily_ReachesTheBarThroughTheContract()
        {
            var carried = typeof(PendingDecisionContract)
                .GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic)
                .GetParameters()
                .Select(parameter => parameter.ParameterType.Name)
                .ToArray();

            // Milestone 9.21b: twelve - "which of you acts" joins through the same contract argument.
            Assert.That(DecisionFamilyInventory.ChoiceProjectionMembers, Has.Length.EqualTo(12),
                "The declared inventory is the list this rule is about; a shrunk one is a broken " +
                "rule rather than a pass.");
            Assert.That(carried, Has.Length.EqualTo(DecisionFamilyInventory.ChoiceProjectionMembers.Length),
                "The contract takes one argument per declared family, which is what makes " +
                "'a contract stands' equivalent to 'some declared family stands'.");

            var session = CreateSession();
            var viewer = session.Projection.Setup.Heroes.First().HeroId;
            Assert.That(
                AwaitedInputQuery.IsWaitingOnViewer(
                    new ContractReadModel(session.Projection, StandingContract()), viewer),
                Is.True,
                "so a standing contract - whichever family produced it - is a wait on its owner.");
        }

        /// <summary>
        /// The whole strip carries the treatment, which is the user's actual requirement: "the whole
        /// top bar needs to be gold, not just naration bar". Both rows live inside this Root, so the
        /// case reads the Root's own paint rather than the lane's, and the cold state is asserted
        /// beside it so the case cannot pass by painting everything always.
        /// </summary>
        [Test]
        public void TheWholeTopStrip_CarriesTheGoldWhileTheMatchWaitsOnTheViewer()
        {
            var session = CreateSession();
            var waiting = new MatchStatusBarUnityView();
            waiting.Render(Header(session, isAwaitingViewerInput: true));
            var calm = new MatchStatusBarUnityView();
            calm.Render(Header(session, isAwaitingViewerInput: false));

            Assert.That(waiting.Root.style.backgroundColor.value,
                Is.EqualTo(MatchHudTheme.NarrationWarmBackground),
                "The strip itself is gold, not only the narration lane inside it.");
            Assert.That(waiting.Root.style.borderBottomColor.value,
                Is.EqualTo(MatchHudTheme.ShineGlow));
            Assert.That(calm.Root.style.backgroundColor.value,
                Is.EqualTo(MatchHudTheme.TopBarBackground),
                "and the non-waiting state keeps the look it already had.");
            Assert.That(calm.Root.style.borderBottomColor.value,
                Is.EqualTo(MatchHudTheme.PanelBorder));
        }

        private static MatchHudHeaderViewModel Header(
            MatchSession session, bool isAwaitingViewerInput) =>
            new MatchHudHeaderViewModel(
                session.Projection.TieBreakerState,
                session.Projection.Setup,
                session.Projection.TeamLifeCounters,
                session.Projection.RoundIndex,
                session.Projection.TurnIndex,
                isAwaitingViewerInput);

        private static PendingDecisionContract StandingContract() =>
            (PendingDecisionContract)typeof(PendingDecisionContract)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0]
                .Invoke(new object[]
                {
                    new PendingDecisionFamilyId("probe"),
                    new[] { DirectorCommand.AdvanceEndOfRound() }
                });

        private static IReadOnlyList<MatchCommitmentProjection> Awaiting(
            IMatchReadModel projection, HeroId viewer) =>
            projection.Setup.Heroes
                .Select(hero => new MatchCommitmentProjection(
                    hero.HeroId,
                    hero.HeroId.Equals(viewer)
                        ? TurnCardCommitmentStatus.AwaitingResponse
                        : TurnCardCommitmentStatus.CardCommittedFacedown,
                    true,
                    null))
                .ToArray();

        private static IReadOnlyList<MatchCommitmentProjection> Committed(
            IMatchReadModel projection, HeroId viewer) =>
            projection.Setup.Heroes
                .Select(hero => new MatchCommitmentProjection(
                    hero.HeroId, TurnCardCommitmentStatus.CardCommittedFacedown, true, null))
                .ToArray();

        private class CommitmentReadModel : DelegatingReadModel
        {
            private readonly IReadOnlyList<MatchCommitmentProjection> commitments;

            public CommitmentReadModel(
                IMatchReadModel inner, IReadOnlyList<MatchCommitmentProjection> commitments)
                : base(inner) => this.commitments = commitments;

            public override IReadOnlyList<MatchCommitmentProjection> Commitments => commitments;
            public override PendingDecisionContract DecisionContract => null;
            public override IReadOnlyList<ActionOffer> AvailableTurnActions =>
                Array.Empty<ActionOffer>();
            public override ActionOffer? AvailableHoldOffer => null;
            public override RoundResolutionPhase Phase => RoundResolutionPhase.AwaitingCommitments;
        }

        private sealed class OfferlessReadModel : CommitmentReadModel
        {
            public OfferlessReadModel(
                IMatchReadModel inner, IReadOnlyList<MatchCommitmentProjection> commitments)
                : base(inner, commitments)
            {
            }

            public override RoundResolutionPhase Phase => RoundResolutionPhase.ResolvingTurn;
        }

        private sealed class EndedReadModel : CommitmentReadModel
        {
            public EndedReadModel(
                IMatchReadModel inner, IReadOnlyList<MatchCommitmentProjection> commitments)
                : base(inner, commitments)
            {
            }

            public override RoundResolutionPhase Phase => RoundResolutionPhase.GameEnded;
        }

        private sealed class ContractReadModel : DelegatingReadModel
        {
            private readonly PendingDecisionContract contract;

            public ContractReadModel(IMatchReadModel inner, PendingDecisionContract contract)
                : base(inner) => this.contract = contract;

            public override PendingDecisionContract DecisionContract => contract;
            public override RoundResolutionPhase Phase => RoundResolutionPhase.ResolvingTurn;
        }

        private sealed class AgreementReadModel : DelegatingReadModel
        {
            private readonly TeamAgreementProjection agreement;

            public AgreementReadModel(IMatchReadModel inner, TeamAgreementProjection agreement)
                : base(inner) => this.agreement = agreement;

            public override TeamAgreementProjection TeamAgreement => agreement;
        }

        // -------------------------------------------------------------------------------------
        // Hero scope (milestone 8.35m, the SEVENTH amendment) - the SAME predicate M2 built at
        // viewer scope, asked instead of a named hero, but scoped down to the coordinator's
        // ruling to only its three provably public arms. See AwaitedInputQuery.IsHeroAwaited's
        // own doc comment for exactly which cases are - and, deliberately, are not - covered.
        // -------------------------------------------------------------------------------------

        /// <summary>The amendment's own example: several trays shine at once during commitment.</summary>
        [Test]
        public void SeveralHeroesOweACommitment_AndSeveralTraysShineAtOnceAtHeroScope()
        {
            var session = CreateSession();
            var heroes = session.Projection.Setup.Heroes.Select(hero => hero.HeroId).ToArray();
            Assert.That(heroes, Has.Length.EqualTo(2),
                "The mirrored fixture's own two heroes are what stand in for 'several' here.");
            var projection = new CommitmentReadModel(
                session.Projection,
                heroes.Select(hero => new MatchCommitmentProjection(
                    hero, TurnCardCommitmentStatus.AwaitingResponse, true, null)).ToArray());

            Assert.That(heroes.Select(hero => AwaitedInputQuery.IsHeroAwaited(projection, hero)),
                Is.All.True, "Every hero still owed a commitment shines, several at once.");
        }

        [Test]
        public void ACommittedHero_IsNotAwaitedAtHeroScopeAnyMore()
        {
            var session = CreateSession();
            var hero = session.Projection.Setup.Heroes.First().HeroId;
            var projection = new CommitmentReadModel(
                session.Projection, Committed(session.Projection, hero));

            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, hero), Is.False);
        }

        /// <summary>
        /// The turn-release owner is the one standing-decision family whose owner already reaches
        /// <see cref="IMatchReadModel"/> unscoped (milestone 8.35g's own decision), so it is the one
        /// of the eleven declared families hero scope CAN read directly.
        /// </summary>
        [Test]
        public void TheTurnReleaseOwner_ShinesAtHeroScope_AndNoOtherHeroDoes()
        {
            var session = CreateSession();
            var heroes = session.Projection.Setup.Heroes.ToArray();
            var owner = heroes[0];
            var other = heroes[1];
            var release = new GuardsDigital.Core.Cards.PendingTurnRelease(
                owner.HeroId,
                GetHandCards(session, owner.TeamSide)[0].Id,
                owner.TeamSide);
            var projection = new TurnReleaseReadModel(
                new CommitmentReadModel(session.Projection, Committed(session.Projection, owner.HeroId)),
                release);

            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, owner.HeroId), Is.True);
            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, other.HeroId), Is.False,
                "A release names one actor; it is not a team fact.");
        }

        /// <summary>
        /// A stated Initiative preference is not "still owed a response" even though the hero could
        /// still revise it - the same distinction the commitment arm already draws.
        /// </summary>
        [Test]
        public void AnInitiativeParticipantWhoHasNotStatedAPreference_ShinesAtHeroScope_AndOneWhoHasDoesNot()
        {
            var session = CreateSession();
            var heroes = session.Projection.Setup.Heroes.ToArray();
            var stated = heroes[0];
            var undecided = heroes[1];
            var proposal = new TeamAgreementProposalId("hero-scope-initiative");
            var card = GetHandCards(session, stated.TeamSide)[0].Id;
            var agreement = new TeamAgreementProjection(
                TeamAgreementKind.Initiative,
                proposal,
                TeamSide.Blue,
                new[] { new TeamAgreementCandidateProjection(card, stated.HeroId) },
                new[]
                {
                    new TeamAgreementParticipantProjection(stated.HeroId, true, card),
                    new TeamAgreementParticipantProjection(undecided.HeroId, false, default)
                },
                0,
                new[] { TeamAgreementCommand.Prefer(proposal, card) });
            var projection = new AgreementReadModel(
                new CommitmentReadModel(session.Projection, Committed(session.Projection, stated.HeroId)),
                agreement);

            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, undecided.HeroId), Is.True,
                "Has not yet stated a preference, so their tray shines.");
            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, stated.HeroId), Is.False,
                "Already stated - could still revise, but that is not 'still owed a response'.");
        }

        [Test]
        public void AMinionBattleParticipantWhoHasNotFinishedPicking_ShinesAtHeroScope_AndOneWhoHasDoesNot()
        {
            var session = CreateSession();
            var heroes = session.Projection.Setup.Heroes.ToArray();
            var picked = heroes[0];
            var undecided = heroes[1];
            var unit = GuardsDigital.Core.Rules.Modifications.RuleObject.ForMinion(
                session.Projection.Minions.First().MinionId);
            var proposal = new TeamAgreementProposalId("hero-scope-minion-battle");
            var agreement = new TeamAgreementProjection(
                TeamAgreementKind.MinionBattle,
                proposal,
                TeamSide.Blue,
                Array.Empty<TeamAgreementCandidateProjection>(),
                new[]
                {
                    // Milestone 9.29: a participant has given its part once its own picks are complete.
                    new TeamAgreementParticipantProjection(picked.HeroId, true, default, new[] { unit }, true),
                    new TeamAgreementParticipantProjection(undecided.HeroId, false, default)
                },
                1,
                new[] { TeamAgreementCommand.Unpick(proposal, unit) },
                picked.HeroId);
            var projection = new AgreementReadModel(
                new CommitmentReadModel(session.Projection, Committed(session.Projection, picked.HeroId)),
                agreement);

            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, undecided.HeroId), Is.True);
            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, picked.HeroId), Is.False,
                "Picked already - they could still change a pick, but that is not owed.");
        }

        // -------------------------------------------------------------------------------------
        // Milestone 8.35z7 (the event-channel follow-up). The FOUR arms 8.35m deferred, now
        // derivable through IMatchReadModel.DecisionOwner (milestone 8.35r) - the same non-goal
        // proved above FLIPS to these positive assertions deliberately, per the milestone's own
        // instruction: the gap that test proved is exactly what closes here.
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// The amendment's own multi-leveler example, now at hero scope: rulebook p.15's leveling is
        /// simultaneous since milestone 8.35u, so <see cref="PendingDecisionOwner.ForHeroes"/> names
        /// several heroes at once and every one of them shines, not only the first.
        /// </summary>
        [Test]
        public void TheLevelingHeroesShineTogetherAtHeroScope_TheMultiLevelerCase()
        {
            var session = CreateSession();
            var heroes = session.Projection.Setup.Heroes.Select(hero => hero.HeroId).ToArray();
            Assert.That(heroes, Has.Length.EqualTo(2),
                "The mirrored fixture's own two heroes stand in for 'several' here, exactly as the " +
                "commitment case above already established.");
            var owner = PendingDecisionOwner.ForHeroes(PendingDecisionFamilies.SystemTransition, heroes);
            var projection = new DecisionOwnerReadModel(
                new CommitmentReadModel(session.Projection, Committed(session.Projection, heroes[0])),
                owner);

            Assert.That(heroes.Select(hero => AwaitedInputQuery.IsHeroAwaited(projection, hero)),
                Is.All.True,
                "Every hero still levelling shines, several at once - the same shine-together shape " +
                "the commitment arm proved, now for milestone 8.35u's own simultaneous family.");
        }

        /// <summary>
        /// The defense answerer's tray shines - and, proved in the SAME case, the M3 trap this
        /// milestone answers: the attacker is still <see cref="IMatchReadModel.SelectedActor"/> while
        /// the DEFENDER is who is being asked, so the actor's tray must not shine through a decision
        /// that is no longer theirs.
        /// </summary>
        [Test]
        public void TheDefenseAnswererShinesAtHeroScope_AndTheActingHeroDoesNotWhileItStands()
        {
            var session = CreateSession();
            var heroes = session.Projection.Setup.Heroes.ToArray();
            var attacker = heroes[0];
            var defender = heroes[1];
            var card = GetHandCards(session, attacker.TeamSide)[0].Id;
            var actorState = new SelectedInitiativeActor(
                card, attacker.HeroId, attacker.TeamSide, 5, false);
            var owner = PendingDecisionOwner.ForHero(PendingDecisionFamilies.Defense, defender.HeroId);
            var projection = new DecisionOwnerReadModel(
                new SelectedActorReadModel(
                    new CommitmentReadModel(
                        session.Projection, Committed(session.Projection, attacker.HeroId)),
                    actorState),
                owner);

            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, defender.HeroId), Is.True,
                "The defense answerer's tray shines.");
            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, attacker.HeroId), Is.False,
                "THE M3 TRAP: SelectedActor still names the attacker while the defender is being " +
                "asked, so a naive SelectedActor read would shine the attacker's tray through " +
                "someone else's standing decision - this predicate must not.");
        }

        [Test]
        public void TheRespawnChooserShinesAtHeroScope_AndNoOtherHeroDoes()
        {
            var session = CreateSession();
            var heroes = session.Projection.Setup.Heroes.ToArray();
            var chooser = heroes[0];
            var other = heroes[1];
            var owner = PendingDecisionOwner.ForHero(PendingDecisionFamilies.HeroRespawn, chooser.HeroId);
            var projection = new DecisionOwnerReadModel(
                new CommitmentReadModel(session.Projection, Committed(session.Projection, chooser.HeroId)),
                owner);

            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, chooser.HeroId), Is.True);
            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, other.HeroId), Is.False);
        }

        /// <summary>
        /// The positive half of the M3 trap: with NOTHING else standing on top of the actor's own
        /// turn, their tray shines - the acting-hero arm 8.35m recorded as underivable and this
        /// milestone completes.
        /// </summary>
        [Test]
        public void TheActingHeroShinesAtHeroScope_WhenNoDecisionStandsOnTopOfTheirTurn()
        {
            var session = CreateSession();
            var heroes = session.Projection.Setup.Heroes.ToArray();
            var actor = heroes[0];
            var other = heroes[1];
            var card = GetHandCards(session, actor.TeamSide)[0].Id;
            var actorState = new SelectedInitiativeActor(card, actor.HeroId, actor.TeamSide, 5, false);
            var projection = new DecisionOwnerReadModel(
                new SelectedActorReadModel(
                    new CommitmentReadModel(session.Projection, Committed(session.Projection, actor.HeroId)),
                    actorState),
                PendingDecisionOwner.None);

            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, actor.HeroId), Is.True,
                "Their own turn, nothing else standing on top of it.");
            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, other.HeroId), Is.False);
        }

        /// <summary>
        /// Integration confidence beside the four synthetic cases above, and the privacy case in the
        /// same breath: a REAL p.15 pause walked to through
        /// <see cref="ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade"/> exercises the
        /// production <c>SeatProjectionFactory.OwnerOfStandingDecision</c> derivation this predicate
        /// now depends on, end to end - and a non-owning seat's own projection carries no
        /// <see cref="IMatchReadModel.DecisionContract"/> for a pause it does not own, yet still
        /// shines the levelling hero's tray, because who is awaited is public while what they may
        /// choose is not.
        /// </summary>
        [Test]
        public void TheLevelingHero_ShinesFromARealPause_AndStaysPublicAtANonOwningSeat()
        {
            var session = ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(
                out var levellingHeroId);
            var host = new MatchHost(session);
            var other = host.Seats.Seats.First(seat => !seat.HeroId.Equals(levellingHeroId));

            Assert.That(AwaitedInputQuery.IsHeroAwaited(host.Projection, levellingHeroId), Is.True,
                "The real p.15 pause: the levelling hero's tray shines on the Director's own view.");

            var otherProjection = host.ProjectionForSeat(other.Id);
            Assert.That(otherProjection.DecisionContract, Is.Null,
                "PRIVACY: the non-owning seat's own projection carries no answer for a pause it " +
                "does not own.");
            Assert.That(AwaitedInputQuery.IsHeroAwaited(otherProjection, levellingHeroId), Is.True,
                "but who is awaited is still public, so this seat's own read still shines the " +
                "levelling hero's tray.");
            Assert.That(AwaitedInputQuery.IsHeroAwaited(otherProjection, other.HeroId), Is.False,
                "and this seat's own tray does not shine merely from watching someone else's pause.");
        }

        [Test]
        public void AnEndedMatch_IsNeverAWaitAtHeroScopeEither()
        {
            var session = CreateSession();
            var hero = session.Projection.Setup.Heroes.First().HeroId;
            var projection = new EndedReadModel(
                session.Projection, Awaiting(session.Projection, hero));

            Assert.That(AwaitedInputQuery.IsHeroAwaited(projection, hero), Is.False);
        }

        private sealed class TurnReleaseReadModel : DelegatingReadModel
        {
            private readonly GuardsDigital.Core.Cards.PendingTurnRelease release;

            public TurnReleaseReadModel(
                IMatchReadModel inner, GuardsDigital.Core.Cards.PendingTurnRelease release)
                : base(inner) => this.release = release;

            public override GuardsDigital.Core.Cards.PendingTurnRelease PendingTurnRelease => release;
        }

        /// <summary>
        /// Milestone 8.35z7. Overrides the standing decision's public owner directly - the synthetic
        /// composition every hero-scope case above the four new arms uses, the same way
        /// <see cref="ContractReadModel"/> overrides the owner's private half.
        /// </summary>
        private sealed class DecisionOwnerReadModel : DelegatingReadModel
        {
            private readonly PendingDecisionOwner owner;

            public DecisionOwnerReadModel(IMatchReadModel inner, PendingDecisionOwner owner)
                : base(inner) => this.owner = owner;

            public override PendingDecisionOwner DecisionOwner => owner;
        }

        /// <summary>
        /// Milestone 8.35z7. Overrides the acting hero directly, for the acting-hero arm's cases - the
        /// same public, unscoped fact <see cref="IMatchReadModel.SelectedActor"/> the board's own
        /// acting-hero marker already reads.
        /// </summary>
        private sealed class SelectedActorReadModel : DelegatingReadModel
        {
            private readonly SelectedInitiativeActor actor;

            public SelectedActorReadModel(IMatchReadModel inner, SelectedInitiativeActor actor)
                : base(inner) => this.actor = actor;

            public override SelectedInitiativeActor SelectedActor => actor;
            public override bool IsTurnActive => true;
        }

        // -------------------------------------------------------------------------------------
        // Milestone 8.35z7 (the TENTH amendment's drop-down). MatchLogFormatter, MatchLogPresenter,
        // and MatchLogUnityView - the log the narration bar unrolls.
        // -------------------------------------------------------------------------------------

        /// <summary>Overrides the seat-public log and the standing undo marker directly.</summary>
        private sealed class MatchLogReadModel : DelegatingReadModel
        {
            private readonly IReadOnlyList<MatchLogEntry> log;
            private readonly MatchUndoMarker marker;

            public MatchLogReadModel(
                IMatchReadModel inner, IReadOnlyList<MatchLogEntry> log, MatchUndoMarker marker)
                : base(inner)
            {
                this.log = log;
                this.marker = marker;
            }

            public override IReadOnlyList<MatchLogEntry> MatchLog => log;
            public override MatchUndoMarker UndoMarker => marker;
        }

        private sealed class RecordingMatchLogView : IMatchLogView
        {
            public MatchLogViewModel LastModel { get; private set; }
            public int RenderCount { get; private set; }

            public void Render(MatchLogViewModel model)
            {
                LastModel = model;
                RenderCount++;
            }
        }

        /// <summary>
        /// Log rendering per entry kind. Every <see cref="MatchLogEntryKind"/> the selection contract
        /// carries produces a line, ROUND/TURN boundaries render as headers and everything else does
        /// not - a closed switch proved against the closed vocabulary rather than a handful of samples.
        ///
        /// <para>It asks <see cref="MatchLogFormatter.ComposeOldestFirst"/> rather than the whole
        /// view model, because item 2 of the 2026-09-06 refinements made the READING ORDER a second
        /// decision (<see cref="MatchLogFormatter.NewestFirst"/>) with its own fixtures below. This
        /// one is about the closed vocabulary only, and stays a straight index-for-index walk.</para>
        /// </summary>
        [Test]
        public void EveryMatchLogEntryKind_RendersItsOwnLineOrHeader()
        {
            var session = CreateSession();
            var heroes = session.Projection.Setup.Heroes.ToArray();
            var hero = heroes[0].HeroId;
            var otherHero = heroes[1].HeroId;
            var minion = new MinionId("probe-minion");
            var origin = new HexCoordinate(0, 0);
            var destination = new HexCoordinate(1, 1);
            var sequence = 1;

            var cases = new (MatchLogEntry Entry, string MustContain, bool IsHeader)[]
            {
                (new MatchLogEntry(sequence++, MatchLogEntryKind.RoundStarted, 3, 0),
                    "ROUND 3", true),
                (new MatchLogEntry(sequence++, MatchLogEntryKind.RoundCompleted, 3, 0),
                    "ROUND 3 COMPLETE", true),
                (new MatchLogEntry(sequence++, MatchLogEntryKind.TurnStarted, 3, 2),
                    "TURN 2", true),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.TurnEnded, 3, 2, subjectHeroId: hero),
                    "TURN 2 ENDED", true),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.Move, 1, 1, subjectHeroId: hero,
                        origin: origin, destination: destination, hasOrigin: true, hasDestination: true),
                    "MOVED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.FastTravel, 1, 1, subjectHeroId: hero,
                        destination: destination, hasDestination: true),
                    "FAST TRAVELED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.Attack, 1, 1, subjectHeroId: hero,
                        targetHeroId: otherHero, outcome: MatchLogOutcome.AttackSuccessful),
                    "ATTACKED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.Defense, 1, 1, subjectHeroId: otherHero,
                        targetHeroId: hero, outcome: MatchLogOutcome.DefenseCardDiscarded),
                    "DEFENDED", false),
                (new MatchLogEntry(sequence++, MatchLogEntryKind.SkillUsed, 1, 1, subjectHeroId: hero),
                    "USED", false),
                (new MatchLogEntry(sequence++, MatchLogEntryKind.Clear, 1, 1, subjectHeroId: hero),
                    "CLEARED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.BlockedAction, 1, 1, subjectHeroId: hero),
                    "BLOCKED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.MinionDefeated, 1, 1, subjectMinionId: minion),
                    "DEFEATED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.MinionRemoved, 1, 1, subjectMinionId: minion),
                    "REMOVED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.MinionRemovalPrevented, 1, 1,
                        subjectMinionId: minion),
                    "PREVENTED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.HeroDefeated, 1, 1, subjectHeroId: hero),
                    "DEFEATED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.HeroRemoved, 1, 1, subjectHeroId: hero),
                    "REMOVED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.HeroRemovalPrevented, 1, 1, subjectHeroId: hero),
                    "PREVENTED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.HeroRespawned, 1, 1, subjectHeroId: hero),
                    "RESPAWNED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.TeamLifeSpent, 1, 1, subjectHeroId: hero),
                    "LIFE", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.TeamEliminated, 1, 1, subjectHeroId: hero),
                    "ELIMINATED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.DisplacedMinionCorrected, 1, 1,
                        subjectMinionId: minion, origin: origin, destination: destination,
                        hasOrigin: true, hasDestination: true),
                    "CORRECTED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.MinionBattleMinionRemoved, 1, 1,
                        subjectMinionId: minion),
                    "MINION BATTLE", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.LanePush, 1, 1, subjectHeroId: hero),
                    "LANE PUSH", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.LevelPurchased, 1, 1, subjectHeroId: hero,
                        amount: 2),
                    "LEVEL 2", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.CardUpgraded, 1, 1, subjectHeroId: hero),
                    "UPGRADED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.ItemGained, 1, 1, subjectHeroId: hero),
                    "GAINED", false),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.PityCoinCollected, 1, 1, subjectHeroId: hero,
                        amount: 1),
                    "PITY COIN", false),
                (new MatchLogEntry(sequence++, MatchLogEntryKind.GameEnded, 1, 1),
                    "GAME ENDED", true),
                (new MatchLogEntry(
                        sequence++, MatchLogEntryKind.CardRevealed, 1, 1, subjectHeroId: hero),
                    "REVEALED", false),
                // Milestone 8.35z61 added this kind and its rendering, and left this closed-
                // vocabulary walk unswept - which is exactly what this case's own count assertion
                // exists to catch, and it did, on the merged tree rather than in the worktree. The
                // harness could not have caught it: this file is exclude-listed there, so only a
                // complete Unity Edit Mode run reaches it.
                (new MatchLogEntry(
                        sequence++,
                        MatchLogEntryKind.HeroCardResolvedWithoutAction,
                        1,
                        1,
                        subjectHeroId: hero),
                    "RESOLVED WITH NO ACTION", false),
                // Milestone 8.40f minted the THIRTY-FIRST kind and its own rendering arm and left
                // this closed-vocabulary walk unswept, exactly as 8.35z61 did above - and exactly as
                // the note above predicts, the harness could not see it, because this file is
                // exclude-listed there. Milestone 8.40j's complete Unity Edit Mode run is the first
                // tier that could, and it failed `Expected: 31, But was: 30` before this line
                // existed. The rendered word is the same as `CardRevealed`'s and the arms are still
                // separate, because the two kinds carry different rules about the card they name.
                (new MatchLogEntry(
                        sequence++,
                        MatchLogEntryKind.CardRevealedByCardText,
                        1,
                        1,
                        subjectHeroId: hero),
                    "REVEALED", false),
                // Milestone 8.51 minted the THIRTY-SECOND kind - rulebook p.12's Hold by the seat's
                // own command - and sweeps this walk in the same change rather than leaving it to
                // the next complete Unity run, as the two notes above record happening twice.
                (new MatchLogEntry(
                        sequence++,
                        MatchLogEntryKind.CardHeld,
                        1,
                        1,
                        subjectHeroId: hero),
                    "HELD", false),
                // Milestone 8.61e minted the THIRTY-THIRD kind - a hero walked over a facedown piece
                // that goes off, and the removal turned it face up for the whole table - and sweeps
                // this walk in the same change, as 8.51 did and as the two notes above record NOT
                // happening twice before that. The rendered word says which kind it turned out to be,
                // which is the whole content of the reveal.
                (new MatchLogEntry(
                        sequence++,
                        MatchLogEntryKind.MoveThroughTrapSprung,
                        1,
                        1,
                        subjectHeroId: hero,
                        origin: origin,
                        hasOrigin: true,
                        amount: 1),
                    "A BLAST", false),
                // Milestone 8.35z73 minted the THIRTY-FOURTH kind from the ledger operation itself,
                // so the amount rendered here is authoritative rather than a balance delta inferred
                // by presentation.
                (new MatchLogEntry(
                        sequence++,
                        MatchLogEntryKind.CoinAwarded,
                        1,
                        1,
                        subjectHeroId: hero,
                        amount: 2),
                    "GAINED 2 COINS", false),
                // Milestone 9.19 minted the THIRTY-FIFTH kind - card text sent a card out of a hand -
                // and sweeps this walk in the same change. The rendered word differs from the
                // reveal's because the card changed state rather than being shown.
                (new MatchLogEntry(
                        sequence++,
                        MatchLogEntryKind.CardDiscardedByCardText,
                        1,
                        1,
                        subjectHeroId: hero),
                    "DISCARDED", false),
                // Milestone 9.30h minted the THIRTY-SIXTH to THIRTY-EIGHTH kinds - a spellbook's cast,
                // faceup removal and refill - and sweeps this walk in the same change. A row whose
                // spell the seat cannot name says "A SPELL"; a refill names a count and no spell.
                (new MatchLogEntry(
                        sequence++,
                        MatchLogEntryKind.SpellCast,
                        1,
                        1,
                        subjectHeroId: hero),
                    "CAST A SPELL", false),
                (new MatchLogEntry(
                        sequence++,
                        MatchLogEntryKind.SpellRemovedFaceup,
                        1,
                        1,
                        subjectHeroId: hero),
                    "REMOVED A SPELL FROM THE SPELLBOOK", false),
                (new MatchLogEntry(
                        sequence++,
                        MatchLogEntryKind.SpellbookRefilled,
                        1,
                        1,
                        subjectHeroId: hero,
                        amount: 5),
                    "RETURNED 5 SPELLS TO THE SPELLBOOK", false),
            };

            Assert.That(cases.Select(entry => entry.Entry.Kind).Distinct().Count(),
                Is.EqualTo(Enum.GetValues(typeof(MatchLogEntryKind)).Length - 1),
                "One case per named kind (every member but the unused None), so a kind added to the " +
                "enum without a rendering case here is caught by this count rather than by a silent " +
                "gap in the switch.");

            var projection = new MatchLogReadModel(
                session.Projection, cases.Select(entry => entry.Entry).ToArray(), MatchUndoMarker.None);
            var lines = MatchLogFormatter.ComposeOldestFirst(projection);

            Assert.That(lines, Has.Count.EqualTo(cases.Length));
            for (var index = 0; index < cases.Length; index++)
            {
                var line = lines[index];
                var expected = cases[index];
                Assert.That(line.Text, Does.Contain(expected.MustContain),
                    $"Case {index} ({expected.Entry.Kind}): '{line.Text}'.");
                Assert.That(line.IsHeader, Is.EqualTo(expected.IsHeader),
                    $"Case {index} ({expected.Entry.Kind})'s header styling.");
                Assert.That(line.Sequence, Is.EqualTo(expected.Entry.Sequence));
            }
        }

        // -------------------------------------------------------------------------------------
        // USER DIRECTIVE (2026-09-08, log-tray wording, verbatim): "in the history log tray
        // thing. Can you lose the coordinates; they dont mean shit to player. Just say
        // Hero-name moved 5 spaces, or hero fast traveled." The six cases below pin the new
        // wording for every MatchLogEntryKind that used to interpolate a raw HexCoordinate
        // (Move, FastTravel, DisplacedMinionCorrected - the audited full set, see
        // MatchLogFormatter.SpacesPhrase's remarks) and the regex guard is the durable
        // regression: it fails if ANY kind, present or future, ever prints the "(Q, R)" shape
        // HexCoordinate.ToString states.
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// The directive's own first example, reproduced as a fixture: the two endpoints are
        /// exactly 3 spaces apart under the board's own <see cref="HexDistanceQuery"/>, and the
        /// rendered line states that number and the hero's name - never the two coordinates the
        /// entry actually carries.
        /// </summary>
        [Test]
        public void Move_RendersHeroNameAndHexDistanceInSpaces_NeverStatesRawCoordinates()
        {
            var session = CreateSession();
            var hero = session.Projection.Setup.Heroes.First();
            var origin = new HexCoordinate(0, 0);
            var destination = new HexCoordinate(0, 3);
            Assert.That(HexDistanceQuery.Calculate(origin, destination), Is.EqualTo(3),
                "The fixture's own two endpoints, pinned so the expected string below is legible.");

            var entry = new MatchLogEntry(
                1, MatchLogEntryKind.Move, 1, 1, subjectHeroId: hero.HeroId,
                origin: origin, destination: destination, hasOrigin: true, hasDestination: true);
            var projection = new MatchLogReadModel(
                session.Projection, new[] { entry }, MatchUndoMarker.None);

            var lines = MatchLogFormatter.ComposeOldestFirst(projection);

            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(lines[0].Text,
                Is.EqualTo($"{hero.DisplayName.ToUpperInvariant()} MOVED 3 SPACES"));
        }

        /// <summary>A one-space move states the singular noun, not "1 SPACES".</summary>
        [Test]
        public void Move_OneSpace_UsesTheSingularNoun()
        {
            var session = CreateSession();
            var hero = session.Projection.Setup.Heroes.First();
            var origin = new HexCoordinate(0, 0);
            var destination = new HexCoordinate(1, 0);
            Assert.That(HexDistanceQuery.Calculate(origin, destination), Is.EqualTo(1));

            var entry = new MatchLogEntry(
                1, MatchLogEntryKind.Move, 1, 1, subjectHeroId: hero.HeroId,
                origin: origin, destination: destination, hasOrigin: true, hasDestination: true);
            var projection = new MatchLogReadModel(
                session.Projection, new[] { entry }, MatchUndoMarker.None);

            var lines = MatchLogFormatter.ComposeOldestFirst(projection);

            Assert.That(lines[0].Text,
                Is.EqualTo($"{hero.DisplayName.ToUpperInvariant()} MOVED 1 SPACE"));
        }

        /// <summary>
        /// A Move entry with no stated route (the defensive shape the constructor still allows,
        /// even though today's projection never produces it - see MatchLogProjection.MoveFold)
        /// derives no distance, so the action name alone is the whole fact: never a half-invented
        /// number standing in for a route that was never recorded.
        /// </summary>
        [Test]
        public void Move_WithNoStatedRoute_RendersTheActionNameAlone()
        {
            var session = CreateSession();
            var hero = session.Projection.Setup.Heroes.First();
            var entry = new MatchLogEntry(1, MatchLogEntryKind.Move, 1, 1, subjectHeroId: hero.HeroId);
            var projection = new MatchLogReadModel(
                session.Projection, new[] { entry }, MatchUndoMarker.None);

            var lines = MatchLogFormatter.ComposeOldestFirst(projection);

            Assert.That(lines[0].Text, Is.EqualTo($"{hero.DisplayName.ToUpperInvariant()} MOVED"));
        }

        /// <summary>
        /// The directive's own second example: "hero fast traveled", with NO number - a zone
        /// teleport has no hex-by-hex distance a player would recognise as spaces travelled. This
        /// holds even when the entry's own two endpoints ARE five spaces apart, proving the kind
        /// itself (not merely an absent route) is what suppresses the phrase.
        /// </summary>
        [Test]
        public void FastTravel_NeverStatesASpacesCount_RegardlessOfWhetherARouteWasRecorded()
        {
            var session = CreateSession();
            var hero = session.Projection.Setup.Heroes.First();
            var withRoute = new MatchLogEntry(
                1, MatchLogEntryKind.FastTravel, 1, 1, subjectHeroId: hero.HeroId,
                origin: new HexCoordinate(0, 0), destination: new HexCoordinate(5, 5),
                hasOrigin: true, hasDestination: true);
            var withoutRoute = new MatchLogEntry(
                2, MatchLogEntryKind.FastTravel, 1, 1, subjectHeroId: hero.HeroId);
            var projection = new MatchLogReadModel(
                session.Projection, new[] { withRoute, withoutRoute }, MatchUndoMarker.None);

            var lines = MatchLogFormatter.ComposeOldestFirst(projection);

            var expected = $"{hero.DisplayName.ToUpperInvariant()} FAST TRAVELED";
            Assert.That(lines[0].Text, Is.EqualTo(expected),
                "Five stated spaces apart, and still no number - a teleport is never measured in " +
                "spaces.");
            Assert.That(lines[1].Text, Is.EqualTo(expected));
        }

        /// <summary>
        /// The one other kind that used to interpolate a route (rulebook p.18's correction of a
        /// minion displaced off a legal space) gets the identical spaces treatment as Move.
        /// </summary>
        [Test]
        public void DisplacedMinionCorrected_RendersHexDistanceInSpaces_NeverStatesRawCoordinates()
        {
            var session = CreateSession();
            var minion = new MinionId("probe-minion-log-wording");
            var origin = new HexCoordinate(0, 0);
            var destination = new HexCoordinate(2, -1);
            Assert.That(HexDistanceQuery.Calculate(origin, destination), Is.EqualTo(2));

            var entry = new MatchLogEntry(
                1, MatchLogEntryKind.DisplacedMinionCorrected, 1, 1, subjectMinionId: minion,
                origin: origin, destination: destination, hasOrigin: true, hasDestination: true);
            var projection = new MatchLogReadModel(
                session.Projection, new[] { entry }, MatchUndoMarker.None);

            var lines = MatchLogFormatter.ComposeOldestFirst(projection);

            Assert.That(lines[0].Text, Is.EqualTo("A MINION'S DISPLACEMENT WAS CORRECTED 2 SPACES"));
        }

        /// <summary>
        /// THE DURABLE REGRESSION. No rendered match-log line may ever match the "(Q, R)" shape
        /// <see cref="HexCoordinate.ToString"/> prints, on any of the three coordinate-carrying
        /// kinds, stated route or not, plus the standing undo marker line alongside them. A future
        /// kind - or a reverted call site - that reintroduces the printed shape fails this
        /// regardless of which line it appears on.
        /// </summary>
        [Test]
        public void NoRenderedMatchLogLine_EverMatchesARawCoordinatePattern()
        {
            var session = CreateSession();
            var hero = session.Projection.Setup.Heroes.First().HeroId;
            var minion = new MinionId("probe-minion-log-wording");
            var farOrigin = new HexCoordinate(-3, 4);
            var farDestination = new HexCoordinate(2, -6);
            var sequence = 1;

            var entries = new[]
            {
                new MatchLogEntry(
                    sequence++, MatchLogEntryKind.Move, 1, 1, subjectHeroId: hero,
                    origin: farOrigin, destination: farDestination,
                    hasOrigin: true, hasDestination: true),
                new MatchLogEntry(sequence++, MatchLogEntryKind.Move, 1, 1, subjectHeroId: hero),
                new MatchLogEntry(
                    sequence++, MatchLogEntryKind.FastTravel, 1, 1, subjectHeroId: hero,
                    origin: farOrigin, destination: farDestination,
                    hasOrigin: true, hasDestination: true),
                new MatchLogEntry(
                    sequence++, MatchLogEntryKind.DisplacedMinionCorrected, 1, 1,
                    subjectMinionId: minion, origin: farOrigin, destination: farDestination,
                    hasOrigin: true, hasDestination: true),
            };
            var projection = new MatchLogReadModel(session.Projection, entries, MatchUndoMarker.Of(hero, 1));

            var viewModel = MatchLogFormatter.CreateViewModel(projection, isOpen: true);
            var coordinatePattern = new Regex(@"\(\s*-?\d+\s*,\s*-?\d+\s*\)");

            Assert.That(viewModel.Lines, Is.Not.Empty);
            foreach (var line in viewModel.Lines)
            {
                Assert.That(coordinatePattern.IsMatch(line.Text), Is.False,
                    $"'{line.Text}' reads like it still carries a raw hex coordinate.");
            }
        }

        /// <summary>
        /// The undo marker: "&lt;HERO&gt; took back N actions", reading at the "now" end of the list
        /// - the current status rather than a moment in the history beside it. Item 2 of the
        /// 2026-09-06 refinements turned the list around, so that end is now the TOP.
        /// </summary>
        [Test]
        public void TheUndoMarker_RendersFirstAsTheCurrentStatus()
        {
            var session = CreateSession();
            var hero = session.Projection.Setup.Heroes.First().HeroId;
            var entry = new MatchLogEntry(1, MatchLogEntryKind.RoundStarted, 1, 0);
            var projection = new MatchLogReadModel(
                session.Projection, new[] { entry }, MatchUndoMarker.Of(hero, 2));

            var viewModel = MatchLogFormatter.CreateViewModel(projection, isOpen: true);

            Assert.That(viewModel.Lines, Has.Count.EqualTo(2));
            var marker = viewModel.Lines[0];
            Assert.That(marker.Text, Does.Contain("TOOK BACK 2 ACTIONS"));
            Assert.That(marker.Sequence, Is.EqualTo(int.MaxValue),
                "It names no rule event, so it cannot sort by one - it takes the 'now' end.");
            Assert.That(viewModel.Lines[1].Text, Does.Contain("ROUND 1"),
                "And the history reads beneath it.");

            // It is not part of the recorded stream, so the composition step does not carry it at
            // all - the marker is the view model's own addition and only there.
            Assert.That(MatchLogFormatter.ComposeOldestFirst(projection), Has.Count.EqualTo(1));
        }

        /// <summary>
        /// Item 2 of the 2026-09-06 log drop-down refinements: "NEWEST TEXT ON TOP -
        /// reverse-chronological ordering, with the ROUND/TURN group headers adapting correctly (the
        /// newest round's group at the top, entries within a group also newest-first)".
        /// </summary>
        /// <remarks>
        /// The worked example a plain <c>Reverse()</c> gets wrong. Reversed naively, ROUND 1 would
        /// lead the list and every header would sit UNDER the entries it heads - which is precisely
        /// the "headers adapting correctly" half of the item. Asserted as an exact expected list
        /// rather than as a property, so the reading order is legible in the fixture itself.
        /// </remarks>
        [Test]
        public void NewestFirst_LeadsWithTheNewestGroup_AndKeepsEveryHeaderAboveItsOwnEntries()
        {
            var lines = new[]
            {
                new MatchLogLineViewModel(1, "ROUND 1", isHeader: true),
                new MatchLogLineViewModel(2, "TURN 1", isHeader: true),
                new MatchLogLineViewModel(3, "A MOVED", isHeader: false),
                new MatchLogLineViewModel(4, "A ATTACKED", isHeader: false),
                new MatchLogLineViewModel(5, "TURN 1 ENDED", isHeader: true),
                new MatchLogLineViewModel(6, "TURN 2", isHeader: true),
                new MatchLogLineViewModel(7, "B MOVED", isHeader: false),
                new MatchLogLineViewModel(8, "ROUND 1 COMPLETE", isHeader: true),
                new MatchLogLineViewModel(9, "ROUND 2", isHeader: true),
                new MatchLogLineViewModel(10, "TURN 3", isHeader: true),
                new MatchLogLineViewModel(11, "C MOVED", isHeader: false)
            };

            Assert.That(
                MatchLogFormatter.NewestFirst(lines).Select(line => line.Text).ToArray(),
                Is.EqualTo(new[]
                {
                    "TURN 3",            // the newest group leads,
                    "C MOVED",           // with its own entries under its own header.
                    "ROUND 2",
                    "ROUND 1 COMPLETE",
                    "TURN 2",
                    "B MOVED",
                    "TURN 1 ENDED",
                    "TURN 1",
                    "A ATTACKED",        // newest-first WITHIN the group,
                    "A MOVED",           // still beneath the header that names it.
                    "ROUND 1"
                }));
        }

        /// <summary>
        /// A retained window truncated mid-round opens with activity lines and no header of their
        /// own. They form a headerless leading group, reversed like any other - nothing is dropped
        /// and no header is invented to cover them.
        /// </summary>
        [Test]
        public void NewestFirst_ReversesALeadingRunThatHasNoHeaderOfItsOwn()
        {
            var lines = new[]
            {
                new MatchLogLineViewModel(1, "A MOVED", isHeader: false),
                new MatchLogLineViewModel(2, "A ATTACKED", isHeader: false),
                new MatchLogLineViewModel(3, "TURN 4", isHeader: true),
                new MatchLogLineViewModel(4, "B MOVED", isHeader: false)
            };

            Assert.That(
                MatchLogFormatter.NewestFirst(lines).Select(line => line.Text).ToArray(),
                Is.EqualTo(new[] { "TURN 4", "B MOVED", "A ATTACKED", "A MOVED" }));
        }

        /// <summary>
        /// The ordering step re-orders and ONLY re-orders. Every line in, every line out, none
        /// added and none reworded.
        /// </summary>
        /// <remarks>
        /// This is the seat-public guarantee stated where it can be checked cheaply: milestone
        /// 8.35r drew the selection and the privacy predicate once, in <c>MatchLogProjection</c>,
        /// and a presentation-side re-order must not be able to surface anything that was not
        /// already in the list handed to it. A permutation cannot.
        /// </remarks>
        [Test]
        public void NewestFirst_IsAPermutation_AndInventsNothing()
        {
            var session = CreateSession();
            var hero = session.Projection.Setup.Heroes.First().HeroId;
            var entries = new[]
            {
                new MatchLogEntry(1, MatchLogEntryKind.RoundStarted, 1, 0),
                new MatchLogEntry(2, MatchLogEntryKind.TurnStarted, 1, 1),
                new MatchLogEntry(3, MatchLogEntryKind.Move, 1, 1, subjectHeroId: hero),
                new MatchLogEntry(4, MatchLogEntryKind.Clear, 1, 1, subjectHeroId: hero),
                new MatchLogEntry(5, MatchLogEntryKind.TurnEnded, 1, 1, subjectHeroId: hero),
                new MatchLogEntry(6, MatchLogEntryKind.RoundCompleted, 1, 0)
            };

            var projection = new MatchLogReadModel(
                session.Projection, entries, MatchUndoMarker.None);
            var oldestFirst = MatchLogFormatter.ComposeOldestFirst(projection);
            var newestFirst = MatchLogFormatter.NewestFirst(oldestFirst);

            Assert.That(newestFirst, Has.Count.EqualTo(oldestFirst.Count));
            Assert.That(
                newestFirst.OrderBy(line => line.Sequence).Select(line => line.Text).ToArray(),
                Is.EqualTo(oldestFirst.OrderBy(line => line.Sequence)
                    .Select(line => line.Text).ToArray()),
                "The same lines, in a different order - never a different set of lines.");
            Assert.That(newestFirst, Is.Not.EqualTo(oldestFirst),
                "And it really did re-order them.");
        }

        /// <summary>
        /// The whole drop-down, end to end, at item 2's own words: the newest activity is the first
        /// line a player reads under the tray's title.
        /// </summary>
        [Test]
        public void TheDropDown_DrawsTheNewestLineFirstAllTheWayIntoTheElementTree()
        {
            var session = CreateSession();
            var hero = session.Projection.Setup.Heroes.First().HeroId;
            var entries = new[]
            {
                new MatchLogEntry(1, MatchLogEntryKind.RoundStarted, 1, 0),
                new MatchLogEntry(2, MatchLogEntryKind.Move, 1, 1, subjectHeroId: hero),
                new MatchLogEntry(3, MatchLogEntryKind.Clear, 1, 1, subjectHeroId: hero)
            };

            var tray = new MatchLogUnityView();
            tray.Render(MatchLogFormatter.CreateViewModel(
                new MatchLogReadModel(session.Projection, entries, MatchUndoMarker.None),
                isOpen: true));

            var drawn = tray.PanelRoot.Query<Label>().ToList()
                .Select(label => label.text)
                .Where(text => !string.IsNullOrEmpty(text))
                .ToList();

            Assert.That(drawn.First(), Is.EqualTo("MATCH LOG"), "The tray's own title leads.");
            var clearIndex = drawn.FindIndex(text => text.Contains("CLEARED"));
            var moveIndex = drawn.FindIndex(text => text.Contains("MOVED"));
            Assert.That(clearIndex, Is.GreaterThanOrEqualTo(0), "The newest line is drawn.");
            Assert.That(moveIndex, Is.GreaterThan(clearIndex),
                "The newer Clear is drawn above the older Move - newest text on top in the real " +
                "element tree, not only in the view model.");
        }

        /// <summary>
        /// Item 3 of the 2026-09-06 log drop-down refinements: "a HEALTHY connection is the default
        /// state and earns no screen - show NOTHING when connected".
        /// </summary>
        [Test]
        public void AHealthyConnection_DrawsNoBannerAtAll()
        {
            var banner = MatchConnectionBanner.For(
                attached: true,
                matchStarted: true,
                state: PrivateSessionClientState.Seated,
                reconnecting: false,
                hasProjection: true,
                waiting: false,
                feedback: string.Empty,
                rejection: string.Empty,
                delayed: false);

            Assert.That(banner.IsShown, Is.False);
            Assert.That(banner.Text, Is.Empty,
                "Not merely hidden with its sentence retained - there is nothing to draw.");
            Assert.That(banner, Is.EqualTo(MatchConnectionBannerViewModel.Hidden));
        }

        /// <summary>
        /// The other half of item 3, swept: EVERY state that is not the healthy one earns its
        /// banner, and every state that words itself "Connected to the host." is the healthy one.
        /// </summary>
        /// <remarks>
        /// <para><b>Why the sweep is stated against the wording table.</b>
        /// <see cref="MatchConnectionBanner.EarnsScreen"/> deliberately does NOT read the composed
        /// sentence back - a wording change would then silently change which states are hidden. So
        /// the two are kept independent in the code and pinned together here: if
        /// <c>NetworkMatchFeedback</c> ever grows a second healthy-sounding branch, or the predicate
        /// drifts from it, this fails rather than a player losing a notice they needed.</para>
        /// </remarks>
        [Test]
        public void EveryStateThatIsNotTheHealthyOne_EarnsItsBanner()
        {
            var flags = new[] { false, true };
            var feedbacks = new[] { string.Empty, "the host refused it" };
            var seen = 0;
            var shown = 0;

            foreach (PrivateSessionClientState state in
                Enum.GetValues(typeof(PrivateSessionClientState)))
            {
                foreach (var hasProjection in flags)
                foreach (var waiting in flags)
                foreach (var delayed in flags)
                foreach (var reconnecting in flags)
                foreach (var feedback in feedbacks)
                {
                    seen++;
                    var wording = NetworkMatchFeedback.ForState(
                        state, reconnecting, hasProjection, waiting, feedback, "Unknown", delayed);
                    var earns = MatchConnectionBanner.EarnsScreen(
                        state, hasProjection, waiting, feedback);

                    Assert.That(earns, Is.EqualTo(wording != "Connected to the host."),
                        $"{state}/proj:{hasProjection}/wait:{waiting}/fb:'{feedback}' worded " +
                        $"'{wording}' but EarnsScreen said {earns}.");

                    var banner = MatchConnectionBanner.For(
                        attached: true,
                        matchStarted: true,
                        state: state,
                        reconnecting: reconnecting,
                        hasProjection: hasProjection,
                        waiting: waiting,
                        feedback: feedback,
                        rejection: "Unknown",
                        delayed: delayed);
                    Assert.That(banner.IsShown, Is.EqualTo(earns));
                    if (banner.IsShown)
                    {
                        shown++;
                        Assert.That(banner.Text, Is.EqualTo(wording.ToUpperInvariant()),
                            "A shown banner says exactly what the wording table already says.");
                    }
                }
            }

            Assert.That(seen, Is.GreaterThan(200), "The sweep really did cover the state space.");
            Assert.That(shown, Is.GreaterThan(0), "And anomalous states really are surfaced.");
        }

        /// <summary>
        /// The two structural silences: no online HUD on screen, and a match that has not started.
        /// Neither is an anomaly to report on the match HUD - the lobby is still drawing its own
        /// status in its own layout - so neither takes the banner.
        /// </summary>
        [Test]
        public void TheBanner_StaysSilentBeforeTheOnlineHudIsShowingAMatch()
        {
            foreach (var (attached, started) in
                new[] { (false, false), (false, true), (true, false) })
            {
                Assert.That(
                    MatchConnectionBanner.For(
                        attached,
                        started,
                        PrivateSessionClientState.Disconnected,
                        reconnecting: false,
                        hasProjection: false,
                        waiting: false,
                        feedback: string.Empty,
                        rejection: "Unknown",
                        delayed: false).IsShown,
                    Is.False,
                    $"attached:{attached} started:{started} - even a Disconnected state draws " +
                    "nothing here, because this banner is not that surface yet.");
            }
        }

        [Test]
        public void NoStandingUndo_RendersNoMarkerLine()
        {
            var session = CreateSession();
            var entry = new MatchLogEntry(1, MatchLogEntryKind.RoundStarted, 1, 0);
            var projection = new MatchLogReadModel(
                session.Projection, new[] { entry }, MatchUndoMarker.None);

            var viewModel = MatchLogFormatter.CreateViewModel(projection, isOpen: true);

            Assert.That(viewModel.Lines, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// A closed drop-down composes no lines at all - the amendment's own "nothing for a closed
        /// drop-down to draw", proved rather than assumed.
        /// </summary>
        [Test]
        public void AClosedDropDown_ComposesNoLines()
        {
            var session = CreateSession();
            var entry = new MatchLogEntry(1, MatchLogEntryKind.RoundStarted, 1, 0);
            var projection = new MatchLogReadModel(
                session.Projection, new[] { entry }, MatchUndoMarker.Of(
                    session.Projection.Setup.Heroes.First().HeroId, 1));

            var viewModel = MatchLogFormatter.CreateViewModel(projection, isOpen: false);

            Assert.That(viewModel.IsOpen, Is.False);
            Assert.That(viewModel.Lines, Is.Empty);
            Assert.That(viewModel.ToggleLabel, Is.EqualTo("▾"));
        }

        /// <summary>
        /// The window is derived, never accumulated: an entry present in one call's window and
        /// absent from the next (the exact shape of an undo, a resync, or the retained-window
        /// truncation itself) does not survive into the new rendering - there is no buffer inside
        /// this formatter for it to survive in.
        /// </summary>
        [Test]
        public void ARewoundEntry_DoesNotSurviveIntoTheNextRendering()
        {
            var session = CreateSession();
            var before = new MatchLogEntry(1, MatchLogEntryKind.Clear, 1, 1,
                subjectHeroId: session.Projection.Setup.Heroes.First().HeroId);
            var after = new MatchLogEntry(1, MatchLogEntryKind.RoundStarted, 1, 0);

            var firstViewModel = MatchLogFormatter.CreateViewModel(
                new MatchLogReadModel(session.Projection, new[] { before }, MatchUndoMarker.None),
                isOpen: true);
            var secondViewModel = MatchLogFormatter.CreateViewModel(
                new MatchLogReadModel(session.Projection, new[] { after }, MatchUndoMarker.None),
                isOpen: true);

            Assert.That(firstViewModel.Lines.Single().Text, Does.Contain("CLEARED"));
            Assert.That(secondViewModel.Lines, Has.Count.EqualTo(1),
                "The rewound Clear line does not persist alongside the truncated window's own line.");
            Assert.That(secondViewModel.Lines.Single().Text, Does.Not.Contain("CLEARED"));
        }

        /// <summary>Drop-down toggle/overlay state: open, close, and the idempotent dismiss.</summary>
        [Test]
        public void MatchLogPresenter_TogglesOpenAndClosed_AndDismissNeverReopens()
        {
            var session = CreateSession();
            var client = new DirectorMatchClient(session);
            var view = new RecordingMatchLogView();
            var presenter = new MatchLogPresenter(client, view);

            presenter.Refresh();
            Assert.That(presenter.IsOpen, Is.False);
            Assert.That(view.LastModel.IsOpen, Is.False);
            Assert.That(view.LastModel.Lines, Is.Empty, "Closed, so nothing is composed to draw.");

            presenter.Toggle();
            Assert.That(presenter.IsOpen, Is.True);
            Assert.That(view.LastModel.IsOpen, Is.True);

            var afterOpenRenderCount = view.RenderCount;
            presenter.Dismiss();
            Assert.That(presenter.IsOpen, Is.False, "Dismiss closes an open drop-down.");
            Assert.That(view.LastModel.IsOpen, Is.False);
            Assert.That(view.RenderCount, Is.EqualTo(afterOpenRenderCount + 1));

            var afterCloseRenderCount = view.RenderCount;
            presenter.Dismiss();
            Assert.That(presenter.IsOpen, Is.False,
                "Dismiss on an already-closed drop-down must not reopen it.");
            Assert.That(view.RenderCount, Is.EqualTo(afterCloseRenderCount),
                "and composes nothing new - the whole point of Dismiss over Toggle for a click-away.");
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
        public void DirectorHandsPresenter_DirectCommitRevalidatesTheExactStandingCardOnce()
        {
            var session = CreateSession();
            var presenter = new DirectorHandsPresenter(session, new CapturingHandsView());
            var redCard = GetHandCards(session, TeamSide.Red)[0];

            Assert.That(presenter.CommitCardImmediately(redCard.Id), Is.True);
            Assert.That(session.Projection.GetCommitment(redCard.OwnerId).Status,
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));
            Assert.That(session.Projection.AcceptedCommands, Has.Count.EqualTo(1));

            Assert.That(presenter.CommitCardImmediately(redCard.Id), Is.False,
                "The card is no longer an eligible hand commitment in the standing projection.");
            Assert.That(session.Projection.AcceptedCommands, Has.Count.EqualTo(1),
                "A stale or repeated drag cannot double-submit.");
        }

        [Test]
        public void DirectorHandsPresenter_ReboundHomeSurvivesAnAdvancedCommitmentWait()
        {
            var session = CreateSession();
            var view = new CapturingHandsView();
            var presenter = new DirectorHandsPresenter(session, view);
            var homeHero = presenter.AwaitedHeroId;

            Assert.That(presenter.SetHomeHero(homeHero), Is.True);
            var card = view.Model.ViewedHand.PlayableTrayCards.First().CardInstanceId;
            Assert.That(presenter.CommitCardImmediately(card), Is.True);

            Assert.That(presenter.AwaitedHeroId, Is.Not.EqualTo(homeHero),
                "The fixture must advance the public commitment wait to another hero.");
            Assert.That(presenter.ViewedHeroId, Is.EqualTo(homeHero));
            Assert.That(view.Model.ViewedHand.HeroId, Is.EqualTo(homeHero),
                "The admitted online seat returns to its own tray when the public wait advances.");
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
            Assert.That(holdModel.FinishMovementInvocation, Is.Null);
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
            Assert.That(movementModel.FinishMovementInvocation.Command, Is.EqualTo(movementModel.EndTurnInvocation.Command));
            Assert.That(movementModel.FinishMovementInvocation, Is.Not.SameAs(movementModel.EndTurnInvocation),
                "Finishing Movement must not arm End Turn's optional-decline/release composition.");

            var attackSession = CreateAdjacentSession();
            StartTiedTurn(attackSession, ActionKind.Attack);
            var attack = attackSession.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(attackSession.Projection, offer) == ActionKind.Attack);
            Assert.That(attackSession.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted, Is.True);
            Assert.That(attackSession.Projection.PendingAttackTargetChoice, Is.Not.Null);
            var mandatoryModel = RoundActionPresenter.CreateViewModel(attackSession.Projection);
            Assert.That(mandatoryModel.Actions, Is.Empty);
            Assert.That(mandatoryModel.FinishMovementInvocation, Is.Null);
            Assert.That(mandatoryModel.EndTurnInvocation, Is.Null,
                "A mandatory target choice projects no DIRECT completion - the target itself cannot " +
                "be skipped.");

            // Milestone 8.35v (the THIRD amendment). Rulebook p.14's target choice IS one of the
            // states a player may walk away from before its irreversible point (choosing the
            // target) - but a Director session's own `IsSeatUndoAvailable` is permanently false
            // (milestone 8.35f's square is a seat-only control), so a Director's own END TURN has
            // no rewind to compose here either and draws disabled with the honest generic word
            // rather than inventing one; the seat-scoped cancel this same state composes for a real
            // player is `RoundPresenter_EndTurnCancelsAnOpenAttackTargetSelectionAndReleasesInOnePress`
            // below, over a seat client whose rewind genuinely stands.
            Assert.That(mandatoryModel.EndTurnCancelsOpenAttack, Is.False,
                "Director has no seat-scoped rewind to compose the cancel from.");
            Assert.That(mandatoryModel.EndTurnUnavailableReason, Is.EqualTo(ActionUnavailabilityLabels.Generic));
        }

        /// <summary>
        /// Milestone 8.35g's raw mechanics, still exactly as shipped: Hold finishes the resolution,
        /// the turn is then held in its own release pause, and the SAME projected command
        /// (<c>ReleaseTurn</c>) is what answers it.
        /// </summary>
        /// <remarks>
        /// <para><b>Superseded framing, kept honest for milestone 8.35v.</b> This case executes each
        /// projected command directly against the session, bypassing <see cref="RoundActionPresenter"/>
        /// entirely - so it still proves the two RAW commands exist and each is individually legal,
        /// which is exactly the mechanics <see cref="RoundActionPresenter.Submit"/>'s own chaining now
        /// builds on. It no longer describes what one player press does: the THIRD amendment's REVISED
        /// SEMANTICS collapse both into the SAME press through the presenter, proved instead by
        /// <see cref="RoundPresenter_SubmitEndTurnCollapsesHoldAndReleaseIntoOnePlayerPress"/> below.</para>
        /// <para>The accepted-command half is not decoration: a control that could only ever produce
        /// a rejection is the exact class milestones 6.5ac and 6.5af exist to prevent, and 7.9c's
        /// F-PRES-01 above is what it looks like when one escapes.</para>
        /// </remarks>
        [Test]
        public void RoundPresenter_DrawsTheTurnReleaseAsASecondEndTurnPressThatTheAuthorityAccepts()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);

            var resolving = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(resolving.EndTurnInvocation.Command.Kind,
                Is.EqualTo(DirectorCommandKind.PerformHold),
                "The first press is still p.12's Hold, unchanged.");
            Assert.That(session.Execute(resolving.EndTurnInvocation.Command).Accepted, Is.True);

            Assert.That(session.Projection.PendingTurnRelease, Is.Not.Null,
                "The finished resolution is held for its own player instead of handing on.");
            Assert.That(session.Projection.SelectedActor.IsDefined, Is.False,
                "and the control below cannot be derived from the selection: acknowledging the " +
                "resolved card is what empties it, which is why the pause carries the hero itself.");

            var released = RoundActionPresenter.CreateViewModel(session.Projection);

            Assert.That(released.EndTurnInvocation, Is.Not.Null,
                "The control returns for the release rather than disappearing with the resolution.");
            Assert.That(released.EndTurnInvocation.Label, Is.EqualTo("END TURN"));
            Assert.That(released.EndTurnInvocation.Command.Kind,
                Is.EqualTo(DirectorCommandKind.ReleaseTurn));
            Assert.That(released.Actions, Is.Empty,
                "Nothing else is offered beside it; the release is the whole of what is asked.");

            Assert.That(session.Execute(released.EndTurnInvocation.Command).Accepted, Is.True);
            Assert.That(session.Projection.PendingTurnRelease, Is.Null);
            Assert.That(session.Projection.AcceptedCommands[session.Projection.AcceptedCommands.Count - 1].Kind,
                Is.EqualTo(DirectorCommandKind.ReleaseTurn),
                "and the release is a recorded command like any other, so a replay of this turn " +
                "contains the hand-off rather than inferring it.");
        }

        // ---------------------------------------------------------------------------------------
        // Milestone 8.35v (the THIRD amendment's REVISED SEMANTICS): END TURN is omnipresent during
        // the acting seat's own turn and one press both cancels any open, not-yet-committed
        // selection and ends the turn. Every state below is exercised through
        // `RoundActionPresenter.SubmitEndTurn()` itself - the real one-press entry point every
        // caller (the drawn control, `ConfiguredMatchSceneRuntime.SubmitEndTurn`, every Play Mode
        // journey filler) now shares - rather than through raw `session.Execute` calls, so what is
        // proved here is the press a player actually makes.
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// State: untouched card - nothing started this turn. One press performs p.12's Hold AND
        /// releases the turn, where milestone 8.35g shipped that as two.
        /// </summary>
        [Test]
        public void RoundPresenter_SubmitEndTurnCollapsesHoldAndReleaseIntoOnePlayerPress()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            Assert.That(session.Projection.PendingMovementChoice, Is.Null,
                "The fixture is only this state while nothing has been started yet.");

            var presenter = new RoundActionPresenter(session, new CapturingRoundView());
            Assert.That(presenter.SubmitEndTurn(), Is.True);

            Assert.That(session.Projection.PendingTurnRelease, Is.Null,
                "One press both Held and released - it never leaves the turn standing in the " +
                "release pause waiting for a second press.");
            var commands = session.Projection.AcceptedCommands;
            Assert.That(commands[commands.Count - 2].Kind, Is.EqualTo(DirectorCommandKind.PerformHold));
            Assert.That(commands[commands.Count - 1].Kind, Is.EqualTo(DirectorCommandKind.ReleaseTurn),
                "Both commands are recorded, in order, exactly as a two-press turn would have " +
                "recorded them - the press is faster, the replay is not different in shape.");
        }

        [Test]
        public void FinishMovementSubmitsOnlyTheProjectedStopAndRejectsItsStaleRepeat()
        {
            var session = CreateSession();
            StartTiedTurn(session,ActionKind.Movement);
            var movement = session.Projection.AvailableTurnActions.Single(offer => GetActionKind(session.Projection,offer) == ActionKind.Movement);
            Assert.That(session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted, Is.True);
            var presenter = new RoundActionPresenter(session,new CapturingRoundView());
            var finish = RoundActionPresenter.CreateViewModel(session.Projection).FinishMovementInvocation;
            var count = session.Projection.AcceptedCommands.Count;
            Assert.That(presenter.Submit(finish), Is.True);
            Assert.That(session.Projection.AcceptedCommands.Count, Is.EqualTo(count+1));
            Assert.That(session.Projection.AcceptedCommands.Last().Kind, Is.EqualTo(DirectorCommandKind.StopMovement));
            Assert.That(session.Projection.PendingTurnRelease, Is.Not.Null);
            Assert.That(presenter.Submit(finish), Is.False);
            Assert.That(session.Projection.AcceptedCommands.Count, Is.EqualTo(count+1));
        }

        [Test]
        public void FinishMovementDoesNotArmAnAsynchronousEndTurnContinuation()
        {
            var session = CreateSession();
            StartTiedTurn(session,ActionKind.Movement);
            var actor = session.Projection.SelectedActor.ActorId;
            var movement = session.Projection.AvailableTurnActions.Single(offer => GetActionKind(session.Projection,offer) == ActionKind.Movement);
            Assert.That(session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted, Is.True);
            var client = new DeferredMatchClient(session);
            var presenter = new RoundActionPresenter(client,new CapturingRoundView(),actor);
            var finish = RoundActionPresenter.CreateViewModel(client.Projection,actor).FinishMovementInvocation;
            var count = session.Projection.AcceptedCommands.Count;
            Assert.That(presenter.Submit(finish), Is.True);
            Assert.That(presenter.Submit(finish), Is.False,"No duplicate while the host is answering.");
            for (var frame=0; frame<4; frame++) { client.DeliverPending(); presenter.Refresh(); }
            Assert.That(session.Projection.AcceptedCommands.Count, Is.EqualTo(count+1));
            Assert.That(session.Projection.AcceptedCommands.Last().Kind, Is.EqualTo(DirectorCommandKind.StopMovement));
            Assert.That(session.Projection.PendingTurnRelease, Is.Not.Null);
        }

        /// <summary>
        /// THE SECOND DEFECT THE FIRST LIVE INTERNET PLAYTEST FOUND: "first end turn clears the
        /// card, second ends the turn". Milestone 8.35z4.
        ///
        /// <para><b>The dimension this whole matrix was missing.</b> Every case above - and every
        /// other <c>SubmitEndTurn</c> assertion in the repository - drives a SYNCHRONOUS client,
        /// where a submission has already been decided by the time <c>Submit</c> returns. The
        /// shipped build's networked seats do not have one. <c>NetworkMatchClient.Submit</c> returns
        /// as soon as the command is SENT, handing back the pre-command projection, and the session
        /// then refuses every further submission while it waits on the host. So the composition's
        /// SECOND step - the release after the Hold - was refused, one command landed per press, and
        /// the player pressed twice. The rule was never wrong; nothing ever tested it over the
        /// client that actually ships.</para>
        ///
        /// <para>The state is the identical untouched-card Hold of the case above, so the only
        /// variable between them is the client. What follows the press here is not a second press -
        /// it is only what <c>PrivateLobbyViews.Update</c> already does every frame while online:
        /// <c>RefreshAll</c>, and therefore <c>RoundActionPresenter.Refresh</c>.</para>
        /// </summary>
        [Test]
        public void OneEndTurnPressOverAnAsynchronousClient_EndsTheTurnWithNoSecondPress()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var actor = session.Projection.SelectedActor.ActorId;
            var client = new DeferredMatchClient(session);
            var presenter = new RoundActionPresenter(client, new CapturingRoundView(), actor);

            Assert.That(presenter.SubmitEndTurn(), Is.True, "The one press the player makes.");
            Assert.That(client.InFlight, Is.True,
                "and it lands exactly one command on the wire, because that is all an " +
                "asynchronous client will accept at a time.");
            Assert.That(session.Projection.AcceptedCommands.Last().Kind,
                Is.Not.EqualTo(DirectorCommandKind.ReleaseTurn),
                "The turn is NOT yet released - which is precisely where the shipped build " +
                "stopped and asked the player to press again.");

            // An impatient second press, mid-round-trip. It must submit nothing AND must not
            // disarm the first press's continuation - otherwise the turn would need a THIRD.
            Assert.That(presenter.SubmitEndTurn(), Is.False,
                "A press while the client is waiting on the host submits nothing.");

            // From here on: only the frames Unity already runs. No further press.
            for (var frame = 0; frame < 8; frame++)
            {
                client.DeliverPending();
                presenter.Refresh();
            }

            Assert.That(session.Projection.PendingTurnRelease, Is.Null,
                "One press, plus the ordinary per-frame refresh, ends the turn. This is the " +
                "assertion the shipped build fails.");
            var commands = session.Projection.AcceptedCommands;
            Assert.That(commands[commands.Count - 2].Kind, Is.EqualTo(DirectorCommandKind.PerformHold));
            Assert.That(commands[commands.Count - 1].Kind, Is.EqualTo(DirectorCommandKind.ReleaseTurn),
                "Both commands are recorded, in order - the same replay shape a synchronous " +
                "client produces, reached across two round trips instead of inside one call.");
            Assert.That(
                commands.Count(command => command.Kind == DirectorCommandKind.ReleaseTurn),
                Is.EqualTo(1),
                "and exactly ONE release: the continuation stops at its own actor's boundary " +
                "rather than running on into the next player's turn.");
        }

        /// <summary>
        /// Reproduces <c>NetworkMatchClient</c>'s contract without a wire: a submission is ACCEPTED
        /// but only SENT - the caller gets the pre-command projection back, that projection reports
        /// <c>IsInputEnabled == false</c>, and every further submission is refused until the host's
        /// answer is delivered. Built on the real <c>NetworkMatchReadModel</c> rather than a
        /// hand-written stub, so the stripped view a waiting client actually renders is the one
        /// under test.
        /// </summary>
        private sealed class DeferredMatchClient : IMatchClient
        {
            private readonly IMatchSession session;
            private readonly MatchHost host;
            private DirectorCommand pending;

            public DeferredMatchClient(IMatchSession session)
            {
                this.session = session;
                host = new MatchHost(session);
            }

            public bool InFlight => pending != null;

            public bool IsDirector => false;

            public IMatchReadModel Projection
            {
                get
                {
                    if (pending == null)
                    {
                        return session.Projection;
                    }

                    var seat = host.Seats.Seats[0];
                    Assert.That(NetworkSeatProjectionCodec.TryDecode(
                        NetworkSeatProjectionCodec.Encode(host.ProjectionForSeat(seat.Id)),
                        out var record), Is.True);
                    var waiting = new NetworkMatchReadModel(
                        host.Projection, MatchContentCatalogs.ForThisBuild());
                    waiting.Replace(record, false);
                    return waiting;
                }
            }

            public MatchClientResult Submit(DirectorCommand command)
            {
                if (pending != null)
                {
                    return new MatchClientResult(false, "Waiting for the host.", Projection, true);
                }

                pending = command;
                return new MatchClientResult(true, "Sent to the host.", Projection, false);
            }

            public MatchClientResult SubmitAgreement(TeamAgreementCommand command) =>
                new MatchClientResult(false, "Not used by this fixture.", Projection, true);

            public MatchClientResult SubmitUndo() =>
                new MatchClientResult(false, "Not used by this fixture.", Projection, true);

            /// <summary>The host's answer arriving - one round trip.</summary>
            public void DeliverPending()
            {
                if (pending == null)
                {
                    return;
                }

                var command = pending;
                pending = null;
                session.Execute(command);
            }
        }

        /// <summary>
        /// Milestone 8.35z4, and the case the six-client rehearsal wrote. The continuation's
        /// <c>MaxEndTurnResumes</c> bound was UNREACHABLE as first written: every resumption re-armed
        /// through <c>ArmOrClearEndTurnContinuation</c>, which reset the counter, so the comparison
        /// could never be met and a press whose submissions kept being refused resumed on every
        /// frame forever. Live, one seat spent 17,486 refused submissions at a single authoritative
        /// version while the whole table stood still.
        ///
        /// <para>Nothing offline had reached it. Against an in-memory wire the composition always
        /// COMPLETED within two frames, so the state left the press's own actor and the continuation
        /// disarmed at its boundary long before any bound mattered. The missing dimension is a host
        /// that answers without ever changing the match: the gate re-opens, the state still concerns
        /// the same actor, and the press has somewhere to go again - forever, unless the budget is
        /// spent rather than refreshed.</para>
        /// </summary>
        [Test]
        public void AnEndTurnPressWhoseSubmissionsAreAlwaysRefused_SpendsItsBudgetInsteadOfResumingForever()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var actor = session.Projection.SelectedActor.ActorId;
            var client = new RefusedDeferredMatchClient(session);
            var presenter = new RoundActionPresenter(client, new CapturingRoundView(), actor);

            Assert.That(presenter.SubmitEndTurn(), Is.True, "The one press the player makes.");

            // Only the frames Unity already runs, and a host that refuses every one of them.
            for (var frame = 0; frame < 200; frame++)
            {
                client.DeliverRefusal();
                presenter.Refresh();
            }

            Assert.That(client.SubmissionsSent, Is.GreaterThan(1),
                "Anti-vacuity: the continuation must really have resumed, or this case would " +
                "pass against a mechanism that never armed at all.");
            Assert.That(client.SubmissionsSent, Is.LessThanOrEqualTo(13),
                "One press plus at most MaxEndTurnResumes (12) resumptions. Pre-fix this reached " +
                "201 - one submission per frame, without end - which is the live runaway.");
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(actor),
                "and the match is exactly where it was: a refused press changes nothing, which " +
                "is why the loop could never terminate itself.");
        }

        /// <summary>
        /// A host that ACCEPTS the send and then refuses it, leaving the match untouched. The gate
        /// closes on submission and re-opens on the refusal, so the press is offered its own
        /// unchanged state back on every frame - the shape the live rehearsal produced.
        /// </summary>
        private sealed class RefusedDeferredMatchClient : IMatchClient
        {
            private readonly IMatchSession session;
            private readonly MatchHost host;
            private bool inFlight;

            public RefusedDeferredMatchClient(IMatchSession session)
            {
                this.session = session;
                host = new MatchHost(session);
            }

            public int SubmissionsSent { get; private set; }

            public bool IsDirector => false;

            public IMatchReadModel Projection
            {
                get
                {
                    if (!inFlight)
                    {
                        return session.Projection;
                    }

                    var seat = host.Seats.Seats[0];
                    Assert.That(NetworkSeatProjectionCodec.TryDecode(
                        NetworkSeatProjectionCodec.Encode(host.ProjectionForSeat(seat.Id)),
                        out var record), Is.True);
                    var waiting = new NetworkMatchReadModel(
                        host.Projection, MatchContentCatalogs.ForThisBuild());
                    waiting.Replace(record, false);
                    return waiting;
                }
            }

            public MatchClientResult Submit(DirectorCommand command)
            {
                if (inFlight)
                {
                    return new MatchClientResult(false, "Waiting for the host.", Projection, true);
                }

                inFlight = true;
                SubmissionsSent++;
                return new MatchClientResult(true, "Sent to the host.", Projection, false);
            }

            public MatchClientResult SubmitAgreement(TeamAgreementCommand command) =>
                new MatchClientResult(false, "Not used by this fixture.", Projection, true);

            public MatchClientResult SubmitUndo() =>
                new MatchClientResult(false, "Not used by this fixture.", Projection, true);

            /// <summary>The host's REFUSAL arriving: the gate re-opens, the match is untouched.</summary>
            public void DeliverRefusal() => inFlight = false;
        }

        /// <summary>
        /// Milestone 8.35z4. The Hold path was the only multi-command press the defect batch probed
        /// over an asynchronous client, and the shape table names four more. This is the movement
        /// Stop at two commands: the composition that stops the movement and then releases, each
        /// step now costing its own round trip.
        /// </summary>
        [Test]
        public void OneEndTurnPressOverAnAsynchronousClient_StopsAMovementAndReleasesWithNoSecondPress()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var movement = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Movement);
            Assert.That(session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted,
                Is.True);
            Assert.That(session.Projection.PendingMovementChoice?.CanStop, Is.True,
                "The fixture is only this state while an early stop is legal.");

            var actor = session.Projection.SelectedActor.ActorId;
            var client = new DeferredMatchClient(session);
            var presenter = new RoundActionPresenter(client, new CapturingRoundView(), actor);

            Assert.That(presenter.SubmitEndTurn(), Is.True, "The one press the player makes.");

            for (var frame = 0; frame < 8; frame++)
            {
                client.DeliverPending();
                presenter.Refresh();
            }

            Assert.That(session.Projection.PendingMovementChoice, Is.Null);
            Assert.That(session.Projection.PendingTurnRelease, Is.Null,
                "One press stopped the movement AND released it, across two round trips.");
            var commands = session.Projection.AcceptedCommands;
            Assert.That(commands[commands.Count - 2].Kind, Is.EqualTo(DirectorCommandKind.StopMovement));
            Assert.That(commands[commands.Count - 1].Kind, Is.EqualTo(DirectorCommandKind.ReleaseTurn));
            Assert.That(
                commands.Count(command => command.Kind == DirectorCommandKind.ReleaseTurn),
                Is.EqualTo(1),
                "and exactly one release, at this press's own actor boundary.");
        }

        /// <summary>
        /// Milestone 8.35z4, and the DEEPEST composition the shape table names: an open Attack
        /// target discarded, the action completed, the turn released - three host commands from one
        /// press, each now costing its own round trip. Staged exactly as the synchronous case above
        /// does, through the seeded walk, then driven over the asynchronous contract.
        /// </summary>
        [Test]
        public void OneEndTurnPressOverAnAsynchronousClient_CancelsAnOpenAttackTargetAndReleases()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var budget = 1800;
            MatchSeat actingSeat = null;
            while (budget > 0)
            {
                budget -= OwnerScopedProjectionTests.DriveThroughSeats(
                    host, budget, projection => projection.PendingAttackTargetChoice != null, 6);
                var projection = host.Projection;
                if (projection.PendingAttackTargetChoice == null)
                {
                    break;
                }

                var seat = host.Seats.FindByHero(projection.SelectedActor.ActorId);
                var seatClient = host.ClientForSeat(seat.Id);
                if (seatClient.Projection.IsSeatUndoAvailable)
                {
                    actingSeat = seat;
                    break;
                }

                var target = seatClient.Projection.AuthorizedCommands.First(command =>
                    command.Kind == DirectorCommandKind.ChooseAttackTarget);
                Assert.That(seatClient.Submit(new SeatSubmission(
                        new SubmissionId("skip-target-" + projection.AcceptedCommands.Count),
                        seatClient.Projection.Version, target))
                    .Accepted, Is.True);
            }

            Assert.That(actingSeat, Is.Not.Null,
                "The seeded walk must reach an Attack target choice whose actor still holds a " +
                "rewind into its own turn, or this case proves nothing about the cancel.");

            var client = new DeferringSeatClient(
                new SeatMatchClient(host.ClientForSeat(actingSeat.Id)), host, actingSeat);
            var attackCommandFloor = host.Projection.AcceptedCommands.Count;
            var presenter = new RoundActionPresenter(
                client, new CapturingRoundView(), actingSeat.HeroId);

            Assert.That(presenter.SubmitEndTurn(), Is.True, "The one press the player makes.");

            for (var frame = 0; frame < 12; frame++)
            {
                client.DeliverPending();
                presenter.Refresh();
            }

            Assert.That(host.Projection.PendingAttackTargetChoice, Is.Null);
            Assert.That(host.Projection.PendingTurnRelease, Is.Null,
                "One press discarded the open target, completed the action and released the " +
                "turn - three commands over three round trips, with no second press.");
            Assert.That(
                host.Projection.AcceptedCommands.Skip(attackCommandFloor)
                    .Any(command => command.Kind == DirectorCommandKind.ChooseAttackTarget),
                Is.False,
                "and the open target was still never answered, not even to a legal candidate.");
        }

        /// <summary>
        /// The same asynchronous contract as <see cref="DeferredMatchClient"/>, wrapped around a
        /// SEAT client rather than the session, so a state reachable only through a real host and
        /// its seats can be driven over the wire's own accepted-but-only-sent semantics.
        /// </summary>
        private sealed class DeferringSeatClient : IMatchClient
        {
            private readonly IMatchClient inner;
            private readonly MatchHost host;
            private readonly MatchSeat seat;
            private DirectorCommand pending;
            private bool pendingUndo;

            public DeferringSeatClient(IMatchClient inner, MatchHost host, MatchSeat seat)
            {
                this.inner = inner;
                this.host = host;
                this.seat = seat;
            }

            public bool IsDirector => false;

            /// <summary>
            /// The open-Attack-target composition opens with the seat's own REWIND, not with a
            /// command, so a wrapper that deferred only <c>Submit</c> would refuse the press's very
            /// first step and report no progress at all. Both carriers are in flight here.
            /// </summary>
            private bool InFlight => pending != null || pendingUndo;

            public IMatchReadModel Projection
            {
                get
                {
                    if (!InFlight)
                    {
                        return inner.Projection;
                    }

                    Assert.That(NetworkSeatProjectionCodec.TryDecode(
                        NetworkSeatProjectionCodec.Encode(host.ProjectionForSeat(seat.Id)),
                        out var record), Is.True);
                    var waiting = new NetworkMatchReadModel(
                        host.Projection, MatchContentCatalogs.ForThisBuild());
                    waiting.Replace(record, false);
                    return waiting;
                }
            }

            public MatchClientResult Submit(DirectorCommand command)
            {
                if (InFlight)
                {
                    return new MatchClientResult(false, "Waiting for the host.", Projection, true);
                }

                pending = command;
                return new MatchClientResult(true, "Sent to the host.", Projection, false);
            }

            public MatchClientResult SubmitAgreement(TeamAgreementCommand command) =>
                new MatchClientResult(false, "Not used by this fixture.", Projection, true);

            public MatchClientResult SubmitUndo()
            {
                if (InFlight)
                {
                    return new MatchClientResult(false, "Waiting for the host.", Projection, true);
                }

                pendingUndo = true;
                return new MatchClientResult(true, "Sent to the host.", Projection, false);
            }

            /// <summary>The host's answer arriving - one round trip, of whichever carrier is out.</summary>
            public void DeliverPending()
            {
                if (pendingUndo)
                {
                    pendingUndo = false;
                    inner.SubmitUndo();
                    return;
                }

                if (pending == null)
                {
                    return;
                }

                var command = pending;
                pending = null;
                inner.Submit(command);
            }
        }

        /// <summary>
        /// State: mid-movement, free to stop early. One press stops the movement AND releases.
        /// </summary>
        [Test]
        public void RoundPresenter_SubmitEndTurnCollapsesAMovementStopAndReleaseIntoOnePlayerPress()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var movement = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Movement);
            Assert.That(session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted,
                Is.True);
            Assert.That(session.Projection.PendingMovementChoice?.CanStop, Is.True,
                "The fixture is only this state while an early stop is legal.");

            var presenter = new RoundActionPresenter(session, new CapturingRoundView());
            Assert.That(presenter.SubmitEndTurn(), Is.True);

            Assert.That(session.Projection.PendingMovementChoice, Is.Null);
            Assert.That(session.Projection.PendingTurnRelease, Is.Null,
                "One press both stopped the movement and released - never a stand-in release pause.");
            var commands = session.Projection.AcceptedCommands;
            Assert.That(commands[commands.Count - 2].Kind, Is.EqualTo(DirectorCommandKind.StopMovement));
            Assert.That(commands[commands.Count - 1].Kind, Is.EqualTo(DirectorCommandKind.ReleaseTurn));
        }

        /// <summary>
        /// State: natural completion, already standing in the release pause - not reached through
        /// END TURN's own composition this time, so this proves the OTHER shape a real turn takes:
        /// a resolution that finished on its own still needs exactly the one press to hand on.
        /// </summary>
        [Test]
        public void RoundPresenter_SubmitEndTurnReleasesANaturallyCompletedTurnInOnePress()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            // Completed directly against the session, standing in for whatever authoritative path
            // finished the resolution without a player pressing END TURN for it (an Attack running
            // to its conclusion is the ordinary shape; Hold is the cheapest fixture that reaches the
            // identical release pause).
            Assert.That(session.Execute(
                    DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId))
                .Accepted, Is.True);
            Assert.That(session.Projection.PendingTurnRelease, Is.Not.Null,
                "The fixture is only this state while the release is still owed.");

            var presenter = new RoundActionPresenter(session, new CapturingRoundView());
            Assert.That(presenter.SubmitEndTurn(), Is.True);

            Assert.That(session.Projection.PendingTurnRelease, Is.Null);
            Assert.That(
                session.Projection.AcceptedCommands[session.Projection.AcceptedCommands.Count - 1].Kind,
                Is.EqualTo(DirectorCommandKind.ReleaseTurn));
        }

        /// <summary>
        /// State: an Attack is open and its target is not yet chosen - rulebook p.14's irreversible
        /// point has not been reached. One press discards the whole open action (milestone 8.35f's
        /// own bounded rewind, still short of its wall) AND releases the turn - proving both halves
        /// of "cancels open selections and ends the turn" at once: the discard-does-not-commit case
        /// (the target is never answered) and the one-press release.
        /// </summary>
        /// <remarks>
        /// Milestone 8.35f's own bounded rewind (`IsSeatUndoAvailable`) is HOST-tracked - it needs
        /// the `MatchHost` present from the turn's own beginning, exactly as `SeatBoundedUndoTests`'
        /// own fixtures build it, never a `MatchSession` driven ahead first and wrapped afterward
        /// (the shape this case's first draft used, and the reason its own seeded assertion failed
        /// at Unity validation: `IsSeatUndoAvailable` read false for a host that only ever saw the
        /// turn already mid-resolution). `OwnerScopedProjectionTests.DriveThroughSeats`'ANDs seeded
        /// walk is the proven way to reach a real open Attack target choice from the official
        /// six-seat state, matching `SeatBoundedUndoTests.AnAttackWhoseTargetWasChosenOffersItsOwnActorNoRewindWhileItResolves`'s
        /// own technique; an Attack the walk reaches with no rewind yet standing is answered and
        /// skipped rather than asserted against, since that is not the state this case is about.
        /// </remarks>
        [Test]
        public void RoundPresenter_EndTurnCancelsAnOpenAttackTargetSelectionAndReleasesInOnePress()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var budget = 1800;
            MatchSeat actingSeat = null;
            while (budget > 0)
            {
                budget -= OwnerScopedProjectionTests.DriveThroughSeats(
                    host, budget, projection => projection.PendingAttackTargetChoice != null, 6);
                var projection = host.Projection;
                if (projection.PendingAttackTargetChoice == null)
                {
                    break;
                }

                var seat = host.Seats.FindByHero(projection.SelectedActor.ActorId);
                var seatClient = host.ClientForSeat(seat.Id);
                if (seatClient.Projection.IsSeatUndoAvailable)
                {
                    actingSeat = seat;
                    break;
                }

                // This Attack's actor has nothing earlier in its own turn to rewind to; answer its
                // target and keep walking toward one that does, rather than asserting against a
                // state this case is not about.
                var target = seatClient.Projection.AuthorizedCommands.First(command =>
                    command.Kind == DirectorCommandKind.ChooseAttackTarget);
                Assert.That(seatClient.Submit(new SeatSubmission(
                        new SubmissionId("skip-target-" + projection.AcceptedCommands.Count),
                        seatClient.Projection.Version, target))
                    .Accepted, Is.True);
            }

            Assert.That(actingSeat, Is.Not.Null,
                "The seeded walk must reach an Attack target choice whose actor still holds a " +
                "rewind into its own turn, or this case proves nothing about the cancel.");

            var client = new SeatMatchClient(host.ClientForSeat(actingSeat.Id));
            Assert.That(client.Projection.IsSeatUndoAvailable, Is.True,
                "The seat's own rewind must actually stand for the cancel this case proves.");
            Assert.That(
                RoundActionPresenter.CreateViewModel(client.Projection, actingSeat.HeroId)
                    .EndTurnCancelsOpenAttack,
                Is.True);

            var attackCommandFloor = host.Projection.AcceptedCommands.Count;
            var presenter = new RoundActionPresenter(client, new CapturingRoundView(), actingSeat.HeroId);
            Assert.That(presenter.SubmitEndTurn(), Is.True);

            Assert.That(host.Projection.PendingAttackTargetChoice, Is.Null);
            Assert.That(host.Projection.PendingTurnRelease, Is.Null,
                "The discarded action still completes the turn and releases it in the same press.");
            Assert.That(
                host.Projection.AcceptedCommands.Skip(attackCommandFloor)
                    .Any(command => command.Kind == DirectorCommandKind.ChooseAttackTarget),
                Is.False,
                "The discard-does-not-commit case: the open target was never answered, not even to " +
                "a legal candidate a lesser fix might have defaulted to.");
        }

        /// <summary>
        /// State: Movement is open but its printed minimum has not been reached, so stopping early
        /// is not yet legal. END TURN draws disabled with the MANDATORY STEP reason rather than
        /// inventing a stop the rules do not offer.
        /// </summary>
        [Test]
        public void RoundPresenter_EndTurnIsDisabledWithMandatoryStepWhenMovementCannotStopYet()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var movement = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Movement);
            Assert.That(session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)).Accepted,
                Is.True);
            var real = session.Projection.PendingMovementChoice;
            Assert.That(real, Is.Not.Null);

            // No fixture in this catalog prints a Movement whose minimum exceeds zero, so the
            // blocked minimum is composed here from the real choice's own fields rather than left
            // unproved - the same technique `DelegatingReadModel`'s siblings use to pose a state the
            // authority itself does not currently produce.
            var blocked = new PendingMovementChoice(
                real.ResolutionId, real.ChoiceId, real.ChooserId, real.MovingHeroId, real.Origin,
                real.RemainingAllowance, false, real.LegalDestinations);
            var wrapped = new MovementChoiceOverrideReadModel(session.Projection, blocked);

            var model = RoundActionPresenter.CreateViewModel(wrapped);
            Assert.That(model.EndTurnInvocation, Is.Null);
            Assert.That(model.EndTurnCancelsOpenAttack, Is.False);
            Assert.That(model.EndTurnUnavailableReason,
                Is.EqualTo(ActionUnavailabilityLabels.MandatoryStep));
        }

        /// <summary>
        /// State: a Skill's mode choice is genuinely mandatory - `StartSkillTurn`'s own fixture,
        /// already proved above (<c>RoundPresenter_ForwardsAPausedEffectFramesModesAndWithholdsEveryOtherControl</c>)
        /// to project no direct completion. This asserts the disabled reason that state now draws
        /// rather than only its absence.
        /// </summary>
        [Test]
        public void RoundPresenter_EndTurnIsDisabledWithMandatoryStepForAMandatoryEffectChoice()
        {
            var session = CreateSession();
            StartSkillTurn(session);
            var skill = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Skill);
            Assert.That(session.Execute(DirectorCommand.StartAction(skill.ActionDefinitionId)).Accepted,
                Is.True);
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.DeclineInvocation, Is.Null,
                "The fixture is only mandatory while it offers neither a Decline...");
            Assert.That(choice.StopInvocation, Is.Null, "...nor a Stop.");

            var presenter = new RoundActionPresenter(session, new CapturingRoundView());
            var model = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(model.EndTurnInvocation, Is.Null);
            Assert.That(model.EndTurnCancelsOpenAttack, Is.False);
            Assert.That(model.EndTurnUnavailableReason,
                Is.EqualTo(ActionUnavailabilityLabels.MandatoryStep));
            Assert.That(presenter.SubmitEndTurn(), Is.False,
                "A disabled control refuses the press rather than silently doing nothing wrong.");
        }

        /// <summary>
        /// State: the wall. An Attack target is already chosen and accepted, so the defender now
        /// owns the wait - milestone 8.35f/8.35p's own wall, read here as END TURN's own disabled
        /// reason rather than a rewind boundary. Not cancelling: disabled, precisely the THIRD
        /// amendment's distinction from the still-open pre-target state above.
        /// </summary>
        [Test]
        public void RoundPresenter_EndTurnIsDisabledAsAnotherSeatsDecisionMidAttackAfterTargetIsChosen()
        {
            var session = CreateAdjacentSession();
            StartTiedTurn(session, ActionKind.Attack);
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Attack);
            Assert.That(session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted,
                Is.True);
            var targetChoice = session.Projection.PendingAttackTargetChoice;
            Assert.That(session.Execute(DirectorCommand.ChooseAttackTarget(
                targetChoice.ResolutionId,
                targetChoice.ChoiceId,
                targetChoice.LegalTargetHeroIds.Single())).Accepted, Is.True);

            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null,
                "The fixture is only the wall once the target is answered and gone.");
            Assert.That(session.Projection.HasPendingAttackResolution, Is.True,
                "and the attack's own resolution is still standing - now on the defender's side.");

            var presenter = new RoundActionPresenter(session, new CapturingRoundView());
            var model = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(model.EndTurnInvocation, Is.Null);
            Assert.That(model.EndTurnCancelsOpenAttack, Is.False,
                "The wall means no rewind is composed here either - milestone 8.35f's own wall has " +
                "already risen on the accepted ChooseAttackTarget.");
            Assert.That(model.EndTurnUnavailableReason,
                Is.EqualTo(ActionUnavailabilityLabels.AnotherSeatDeciding));
            Assert.That(presenter.SubmitEndTurn(), Is.False,
                "A disabled control refuses the press; it never quietly resolves the defense for " +
                "the other seat on its behalf.");
        }

        /// <summary>
        /// State: an open Clear selection (milestone 6.3a) - presentation-only, since nothing is
        /// submitted to the authority until it is confirmed. Caught by the 8.35u worker's own Edit
        /// Mode run against this milestone's uncommitted work: `CreateEndTurnInvocation` does not
        /// read `ClearSelection` at all, so with nothing else pending it already projects the
        /// ordinary p.12 Hold alongside an open, unconfirmed board selection - submitting that Hold
        /// FIRST would leave the Clear orphaned rather than cancelled. One press must discard the
        /// open selection before completing the turn, exactly as it does for an open Attack target.
        /// </summary>
        [Test]
        public void RoundPresenter_EndTurnCancelsAnOpenClearSelectionFirst()
        {
            var session = ClearPresentationTests.StartAttackTurnBesideAHazard(out _, out _);
            var view = new CapturingRoundView();
            var presenter = new RoundActionPresenter(session, view);
            presenter.Refresh();
            Assert.That(presenter.SelectClear(view.Model.ClearOffers.Single()), Is.True);
            presenter.Refresh();

            Assert.That(view.Model.ClearSelection, Is.Not.Null,
                "The fixture is only this state while the Clear selection is genuinely open.");
            Assert.That(view.Model.EndTurnInvocation, Is.Not.Null,
                "Nothing authoritative has paused - the Clear is presentation-only - so END TURN " +
                "already projects the ordinary completion alongside the open selection.");

            Assert.That(presenter.SubmitEndTurn(), Is.True);
            presenter.Refresh();

            Assert.That(view.Model.ClearSelection, Is.Null,
                "The open selection is discarded, not answered.");
            Assert.That(
                session.Projection.AcceptedCommands.Any(command =>
                    command.Kind == DirectorCommandKind.PerformClear),
                Is.False,
                "Discard-does-not-commit: the Clear was never submitted to the authority at all.");
            Assert.That(session.Projection.PendingTurnRelease, Is.Null,
                "The same press that discarded the selection also completed the turn (p.12's Hold, " +
                "since nothing else was ever started) and released it.");
        }

        private sealed class MovementChoiceOverrideReadModel : DelegatingReadModel
        {
            private readonly PendingMovementChoice choice;

            public MovementChoiceOverrideReadModel(IMatchReadModel inner, PendingMovementChoice choice)
                : base(inner) => this.choice = choice;

            public override PendingMovementChoice PendingMovementChoice => choice;
        }

        [Test]
        public void RoundPresenter_DisabledNetworkView_FabricatesNoEndOfRoundContinuation()
        {
            // Milestone 8.35d. The boundary this case needs - end of round with nothing standing -
            // no longer occurs in a seat-served match at all: the host runs the stage the moment
            // the answer before it lands, which is the whole of that milestone. It is still an
            // ordinary Director position, so the fixture is composed at one directly instead of
            // walking a wire that will never rest there.
            var session = ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(out _);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.AnswerSystemTransitionStep(
                session.Projection.PendingSystemTransitionStep.Invocations[0]));
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
            Assert.That(session.Projection.DecisionContract, Is.Null);
            Assert.That(RoundActionPresenter.CreateViewModel(session.Projection)
                    .ContinueEndOfRoundInvocation,
                Is.Not.Null,
                "The anti-vacuity arm: an enabled Director view IS offered the control in exactly " +
                "this position, so the null below is about the disabled view and nothing else.");

            var host = new MatchHost(session);
            var seat = host.Seats.Seats[0];
            Assert.That(NetworkSeatProjectionCodec.TryDecode(
                    NetworkSeatProjectionCodec.Encode(host.ProjectionForSeat(seat.Id)),
                    out var record),
                Is.True);
            var disabled = new NetworkMatchReadModel(
                host.Projection, MatchContentCatalogs.ForThisBuild());
            disabled.Replace(record, false);
            Assert.That(disabled.IsInputEnabled, Is.False);

            var model = RoundActionPresenter.CreateViewModel(disabled);

            Assert.That(model.ContinueEndOfRoundInvocation, Is.Null,
                "A waiting, stale, disconnected, or reconnecting network view may retain the " +
                "public stage, but it cannot mint a control the host did not currently authorize.");
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
            var acceptedBeforeSelection = session.Projection.AcceptedCommands.Count;
            Assert.That(presenter.Submit(selections[0]), Is.True);
            var afterSelect = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(afterSelect.EffectChoices, Is.Empty,
                "An exactly-one mode finishes when it is picked; no generic confirmation button remains.");
            Assert.That(
                session.Projection.AcceptedCommands.Skip(acceptedBeforeSelection)
                    .Select(value => value.EffectChoice.Kind),
                Is.EqualTo(new[] { EffectChoiceCommandKind.Select }),
                "Milestone 8.46b: an exactly-one mode resolves on the selection itself, so the one " +
                "accepted command is the select; no confirm exists to forward.");
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
        /// both 6.5c surfaces without adding presentation of its own: the unit is a named map target,
        /// and the destinations it then offers remain on the map. The zero-distance answer to `up to`
        /// is the occupied source space; the other destinations are empty spaces.
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

            var presenter = new RoundActionPresenter(session, new CapturingRoundView());
            Assert.That(presenter.SubmitTarget(naming.EffectChoiceTargets.Single()), Is.True,
                "The player's one target click carries the authority's immediate confirmation too.");

            var moving = RoundActionPresenter.CreateViewModel(session.Projection);
            Assert.That(moving.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select),
                Is.False,
                "No anonymous OPTION #n button survives on either half of this branch.");
            Assert.That(
                moving.EffectChoiceDestinations.Count + moving.EffectChoiceTargets.Count,
                Is.EqualTo(session.Projection.PendingEffectChoice.SelectInvocations.Count));
            Assert.That(moving.EffectChoiceTargets, Has.Count.EqualTo(1),
                "The occupied source space is the zero-distance answer to `up to`; it remains clickable.");
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
            // Milestone 8.46b: the single spawn candidate is an exactly-one choice, so the select
            // resolves it; a confirm is owed only where the same choice still stands and offers one.
            ConfiguredMatchFixtures.ConfirmStandingEffectChoiceIfOwed(
                session, control.Command.EffectChoice.ChoiceId);
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
            Assert.That(skill.Unavailability.IsStated, Is.False,
                "Fixture precondition for the milestone 8.35s case below: this is the ambiguous " +
                "unstated cause, not a standing rule modification.");

            var model = RoundActionPresenter.CreateViewModel(session.Projection);

            // Milestone 8.35s: StampAvailability's own doc comment says only the Skill-restriction
            // clause can explain itself; this Skill is greyed for want of a target instead, which is
            // one of at least three other causes the same unstated Kind could mean. The hover therefore
            // reads the recorded generic word rather than a guessed "NO TARGET" - proving the
            // no-invented-reasons rule on a real fixture rather than a synthetic one.
            var greyedRegion = model.PrintedActionRegions.Single(region =>
                region.ActionDefinitionId.Equals(skill.ActionDefinitionId));
            Assert.That(greyedRegion.AssistiveLabel, Does.EndWith(" - " + ActionUnavailabilityLabels.Generic));

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
            Assert.That(view.Model.EffectChoiceMovementDestinations, Is.EqualTo(offered),
                "Place is not rules Movement, but choosing where to Place shares the green " +
                "movement/placement board language.");
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
            // Milestone 8.46b: the push's target is an exactly-one choice, so the select resolved it
            // inside the same command and there is no standing "selected" cue left to read back.
            // What the rebuild must show is the choice gone from the board.
            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "An exactly-one target answers on its own press.");
            Assert.That(view.Model.PieceTargetCues.Any(value => value.Target.Equals(blueTarget)),
                Is.False, "A resolved choice leaves no target cue behind.");
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
        public void HoveringALegalHeroAttackTarget_PreviewsItsAuthorityProjectedMinionDefenseContributions()
        {
            var session = CreateDefensePreviewSession();
            StartTiedTurn(session, ActionKind.Attack);
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                GetActionKind(session.Projection, offer) == ActionKind.Attack);
            Assert.That(session.Execute(
                DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted, Is.True);

            var previews = session.Projection.PendingAttackTargetDefensePreviews;
            Assert.That(previews, Has.Count.EqualTo(
                session.Projection.PendingAttackTargetChoice.LegalTargetHeroIds.Count));
            var preview = previews.Single();
            Assert.That(preview.HeroId,
                Is.EqualTo(session.Projection.PendingAttackTargetChoice.LegalTargetHeroIds.Single()));
            Assert.That(preview.MinionContributions.Select(value =>
                    (value.SourceMinionId.Value, value.Amount)),
                Is.EquivalentTo(new[]
                {
                    ("blue-defense-support", 1),
                    ("red-defense-hinder", -1),
                    ("red-ranged-hinder", -1)
                }));
            var boardView = new CapturingBoardView();
            new BoardInteractionPresenter(session, boardView).Refresh();
            Assert.That(boardView.Model.AttackTargetDefensePreviews, Is.EqualTo(previews),
                "The passive view model copies the immutable sequence but retains each projected preview.");
            Assert.That(boardView.Model.MinionDefenseCues, Is.Empty,
                "Before hover, the speculative contribution preview does not light the whole board.");
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

            var anchor = new UnityEngine.Rect(120f, 40f, 300f, 220f);
            var popup = RoundActionUnityView.CalculateDefensePreviewRect(
                anchor,
                new UnityEngine.Vector2(1536f, 864f),
                new UnityEngine.Vector2(440f, 240f));
            Assert.That(popup.xMin, Is.EqualTo(anchor.xMax + 8f),
                "The breakdown opens immediately to the right of the complete Defense-card fan.");
            Assert.That(popup.yMin, Is.EqualTo(anchor.yMin),
                "The right-side breakdown aligns with the top of the available cards.");
            Assert.That(popup.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(popup.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(popup.xMax, Is.LessThanOrEqualTo(1536f));
            Assert.That(popup.yMax, Is.LessThanOrEqualTo(864f));
            Assert.That(popup.Overlaps(anchor), Is.False);

            var rightEdgeAnchor = new UnityEngine.Rect(1200f, 700f, 300f, 150f);
            var leftFallback = RoundActionUnityView.CalculateDefensePreviewRect(
                rightEdgeAnchor,
                new UnityEngine.Vector2(1536f, 864f),
                new UnityEngine.Vector2(440f, 240f));
            Assert.That(leftFallback.xMax, Is.EqualTo(rightEdgeAnchor.xMin - 8f),
                "Only a constrained right edge moves the breakdown to the fan's left.");
            Assert.That(leftFallback.yMax, Is.LessThanOrEqualTo(864f));
            Assert.That(leftFallback.Overlaps(rightEdgeAnchor), Is.False);
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
                Is.EqualTo(new[] { EffectChoiceLabels.AcceptLabel, EffectChoiceLabels.DeclineLabel }),
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

            // Milestone 8.35g. The Hold finished Red's resolution, and this helper is straight-line
            // rather than a loop: the release is what puts Blue in the selection asserted below.
            Accept(session, DirectorCommand.ReleaseTurn());
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
            for (var step = 0; step < 96 && !removed.IsDefined; step++)
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

                // Milestone 8.35g. ABOVE the selection read below: the acknowledgement that raised
                // the pause is what empties it, so the Hold would be refused NoActorSelected here.
                if (projection.PendingTurnRelease != null)
                {
                    Accept(session, DirectorCommand.ReleaseTurn());
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
            // Milestone 8.46b: the mode resolves on its selection; a confirm is owed only where one stands.
            ConfiguredMatchFixtures.ConfirmStandingEffectChoiceIfOwed(session, choice.ChoiceId);
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

        /// <summary>
        /// Milestone 8.35h. A read model that is a real projection in every respect except the one
        /// fact a case wants to doctor.
        ///
        /// <para>It exists because <see cref="IMatchReadModel"/> is the only seam a presenter reads
        /// through, so a case about what a surface does with an awkward projection has no other way
        /// to pose its question. Every member delegates; a derived case overrides exactly what it is
        /// about. When the interface grows, this fails to compile - which is the loud answer, since
        /// the wire's own conformance rule already forces a decision about every new member.</para>
        /// </summary>
        // Milestone 8.58: internal rather than private, so `DecisionWindowPresenterTests` can pose the
        // NETWORK shape - a projection that withholds the running frame and still names the resolving
        // card - through the one double that already delegates every member of this interface.
        internal class DelegatingReadModel : IMatchReadModel
        {
            private readonly IMatchReadModel inner;

            public DelegatingReadModel(IMatchReadModel inner) =>
                this.inner = inner ?? throw new ArgumentNullException(nameof(inner));

            public virtual long Version => inner.Version;
            public virtual bool IsInputEnabled => inner.IsInputEnabled;
            public virtual bool IsSeatUndoAvailable => inner.IsSeatUndoAvailable;

            // Milestone 8.35r. Virtual like the rest, so a spectator-scope case can override the
            // awaited owner the way it already overrides AvailableTurnActions.
            public virtual GuardsDigital.Application.PendingDecisionOwner DecisionOwner =>
                inner.DecisionOwner;
            public virtual IReadOnlyList<GuardsDigital.Core.Diagnostics.MatchLogEntry> MatchLog =>
                inner.MatchLog;
            public virtual GuardsDigital.Application.MatchUndoMarker UndoMarker => inner.UndoMarker;
            public virtual QuickGameSetupState Setup => inner.Setup;
            public virtual BoardMapDefinition SelectedMap => inner.SelectedMap;
            public virtual MatchParticipants Participants => inner.Participants;
            public virtual IReadOnlyList<CardDefinition> Definitions => inner.Definitions;
            public virtual IReadOnlyList<HeroOccupancy> Heroes => inner.Heroes;
            public virtual IReadOnlyList<GuardsDigital.Core.Units.Minions.MinionOccupancy> Minions =>
                inner.Minions;
            public virtual IReadOnlyList<ObstacleOccupancy> Obstacles => inner.Obstacles;
            public virtual IReadOnlyList<PlacedToken> PlacedTokens => inner.PlacedTokens;
            public virtual IReadOnlyList<MarkerAttachment> HeldMarkers => inner.HeldMarkers;
            public virtual IEnumerable<MarkerAttachment> MarkersHeldBy(HeroId holderHeroId) =>
                inner.MarkersHeldBy(holderHeroId);
            public virtual int RoundIndex => inner.RoundIndex;
            public virtual int TurnIndex => inner.TurnIndex;
            public virtual RoundResolutionPhase Phase => inner.Phase;
            public virtual TieBreakerState TieBreakerState => inner.TieBreakerState;
            public virtual IReadOnlyList<RoundTurnSlot> TurnSlots => inner.TurnSlots;
            public virtual IReadOnlyList<CompletedRoundRecord> CompletedRounds => inner.CompletedRounds;
            public virtual EndOfRoundStage? CurrentEndOfRoundStage => inner.CurrentEndOfRoundStage;
            public virtual IReadOnlyList<MatchCommitmentProjection> Commitments => inner.Commitments;
            public virtual MatchCommitmentProjection GetCommitment(HeroId heroId) =>
                inner.GetCommitment(heroId);
            public virtual SelectedInitiativeActor SelectedActor => inner.SelectedActor;
            public virtual bool IsTurnActive => inner.IsTurnActive;
            public virtual IReadOnlyList<KeyValuePair<TeamId, int>> TeamLifeCounters =>
                inner.TeamLifeCounters;

            // Milestone 8.35z60. Virtual and delegating like the rest, so a case about the
            // wave label the same growth-forces-a-decision way every other member here does.
            public virtual int RemainingWaveCounters => inner.RemainingWaveCounters;

            public virtual IReadOnlyList<HeroProgressionProjection> HeroProgression =>
                inner.HeroProgression;

            public virtual IReadOnlyList<GuardsDigital.Core.Rules.Victory.HeroScoreboardEntry>
                Scoreboard => inner.Scoreboard;
            public virtual GuardsDigital.Core.Rules.Victory.MatchOutcome Outcome => inner.Outcome;
            public virtual IReadOnlyList<CardInstanceId> ActiveEffectSourceCardInstanceIds =>
                inner.ActiveEffectSourceCardInstanceIds;
            public virtual bool IsActiveEffectSource(CardInstanceId cardInstanceId) =>
                inner.IsActiveEffectSource(cardInstanceId);
            public virtual TeamAgreementProjection TeamAgreement => inner.TeamAgreement;
            public virtual IReadOnlyList<CardInstance> Cards => inner.Cards;
            public virtual IReadOnlyList<SeatHandSummary> HandSummaries => inner.HandSummaries;
            public virtual PendingDecisionContract DecisionContract => inner.DecisionContract;
            public virtual PendingInitiativeChoice PendingInitiativeChoice =>
                inner.PendingInitiativeChoice;
            public virtual PendingCardActionResolution PendingMovementResolution =>
                inner.PendingMovementResolution;
            public virtual bool HasPendingMovementResolution => inner.HasPendingMovementResolution;
            public virtual PendingMovementChoice PendingMovementChoice => inner.PendingMovementChoice;
            public virtual PendingAttackResolution PendingAttackResolution =>
                inner.PendingAttackResolution;
            public virtual bool HasPendingAttackResolution => inner.HasPendingAttackResolution;
            public virtual PendingAttackTargetChoice PendingAttackTargetChoice =>
                inner.PendingAttackTargetChoice;
            public virtual IReadOnlyList<AttackTargetDefensePreviewProjection>
                PendingAttackTargetDefensePreviews => inner.PendingAttackTargetDefensePreviews;
            public virtual PendingDefenseResponseChoice PendingDefenseChoice =>
                inner.PendingDefenseChoice;
            public virtual PendingDefensePreviewProjection PendingDefensePreview =>
                inner.PendingDefensePreview;
            public virtual PendingResolution PendingSkillResolution => inner.PendingSkillResolution;
            public virtual bool HasPendingSkillResolution => inner.HasPendingSkillResolution;
            public virtual CardInstanceId ResolvingSourceCardInstanceId =>
                inner.ResolvingSourceCardInstanceId;
            public virtual PendingEffectChoice PendingEffectChoice => inner.PendingEffectChoice;
            public virtual IReadOnlyList<EffectChoiceCandidatePreview> PendingEffectChoicePreview =>
                inner.PendingEffectChoicePreview;
            public virtual GuardsDigital.Core.Rules.Displacement.PendingDisplacedMinionCorrection
                PendingDisplacedMinionCorrection => inner.PendingDisplacedMinionCorrection;
            public virtual PendingMinionBattleChoice PendingMinionBattleChoice =>
                inner.PendingMinionBattleChoice;
            public virtual PendingHeroRespawnChoice PendingHeroRespawnChoice =>
                inner.PendingHeroRespawnChoice;
            public virtual PendingActingBodyChoice PendingActingBodyChoice =>
                inner.PendingActingBodyChoice;
            public virtual IReadOnlyList<HeroActingBody> ActingBodies => inner.ActingBodies;
            public virtual GuardsDigital.Core.Rules.Transitions.PendingSystemTransitionStep
                PendingSystemTransitionStep => inner.PendingSystemTransitionStep;
            public virtual RemovalPreventionPrompt PendingRemovalPrevention =>
                inner.PendingRemovalPrevention;
            public virtual GuardsDigital.Core.Cards.PendingTurnRelease PendingTurnRelease =>
                inner.PendingTurnRelease;
            public virtual GuardsDigital.Core.Rules.Transitions.SystemTransitionStepDescription
                PendingSystemTransitionDescription => inner.PendingSystemTransitionDescription;
            public virtual IReadOnlyList<ActionOffer> AvailableTurnActions => inner.AvailableTurnActions;
            public virtual IReadOnlyList<FastTravelOptionProjection> AvailableFastTravelOptions =>
                inner.AvailableFastTravelOptions;
            public virtual IReadOnlyList<ClearOptionProjection> AvailableClearOptions =>
                inner.AvailableClearOptions;
            public virtual ActionOffer? AvailableHoldOffer => inner.AvailableHoldOffer;
        }

        /// <summary>
        /// Milestone 8.35h. A round panel whose rendering throws, and counts the attempts so a case
        /// can say the failing panel was tried rather than skipped.
        /// </summary>
        private sealed class ThrowingRoundView : IRoundActionView
        {
            public int Attempts { get; private set; }

            public void Render(RoundActionViewModel model)
            {
                Attempts++;
                throw new InvalidOperationException("This panel's rendering is deliberately broken.");
            }
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

        // -------------------------------------------------------------------------------------
        // Narration content and typography (milestone 8.35n, the FIFTH amendment: "hero name,
        // who is acting, not only initiative ... its important, make it look it").
        //
        // The split is derivability, exactly as M2 built it at viewer scope and M3 recorded for
        // hero scope: a decision the VIEWER'S OWN projection carries in full reads the rich
        // "YOUR <FAMILY> - <activity>" line (NarrationLabels.Own); everything else this method
        // can say is a fact IMatchReadModel already publishes to EVERY viewer regardless of
        // ownership - the acting hero's name (SelectedActor), the release owner's name
        // (PendingTurnRelease.ActorId, unscoped by 8.35g), and who still owes a commitment or a
        // team-agreement answer (both public on their own declaration).
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// A client whose only job is to answer <see cref="IMatchClient.Projection"/> for a
        /// <see cref="RoundActionPresenter"/> built with a specific viewer hero - the one thing
        /// the two public static <c>CreateViewModel</c> overloads cannot do, because both fix the
        /// viewer at the Director's undefined hero.
        /// </summary>
        private sealed class StubMatchClient : IMatchClient
        {
            public StubMatchClient(IMatchReadModel projection) => Projection = projection;
            public IMatchReadModel Projection { get; }
            public bool IsDirector => false;

            public MatchClientResult Submit(DirectorCommand command) =>
                throw new NotSupportedException("This stub answers Refresh() reads only.");

            public MatchClientResult SubmitAgreement(TeamAgreementCommand command) =>
                throw new NotSupportedException("This stub answers Refresh() reads only.");

            public MatchClientResult SubmitUndo() =>
                throw new NotSupportedException("This stub answers Refresh() reads only.");
        }

        private static RoundActionViewModel Narrate(IMatchReadModel projection, HeroId viewerHeroId)
        {
            var view = new CapturingRoundView();
            new RoundActionPresenter(new StubMatchClient(projection), view, viewerHeroId).Refresh();
            return view.Model;
        }

        private sealed class DefenseChoiceReadModel : DelegatingReadModel
        {
            private readonly PendingDefenseResponseChoice choice;

            public DefenseChoiceReadModel(IMatchReadModel inner, PendingDefenseResponseChoice choice)
                : base(inner) => this.choice = choice;

            public override PendingDefenseResponseChoice PendingDefenseChoice => choice;
        }

        private sealed class MovementChoiceReadModel : DelegatingReadModel
        {
            private readonly PendingMovementChoice choice;

            public MovementChoiceReadModel(IMatchReadModel inner, PendingMovementChoice choice)
                : base(inner) => this.choice = choice;

            public override PendingMovementChoice PendingMovementChoice => choice;
        }

        /// <summary>
        /// A viewer who is not the acting hero. <see cref="AwaitedInputQuery.MayTakeATurnAction"/>
        /// reads <c>AvailableTurnActions</c>/<c>AvailableHoldOffer</c> straight off the projection
        /// with no viewer parameter of its own - Application scopes those to <c>isActor &amp;&amp;
        /// !standing</c> FOR ONE SEAT's own real <c>SeatProjection</c>, which is what keeps an
        /// attacker's own bar from lighting during a defender's answer. The Director's
        /// <c>MatchSessionProjection</c> carries no such per-seat scoping at all - it is one
        /// omniscient view, not six - so a spectator case built on it has to fake the withholding
        /// this wrapper stands for, or it silently answers the wrong question.
        /// </summary>
        private sealed class SpectatorReadModel : DelegatingReadModel
        {
            public SpectatorReadModel(IMatchReadModel inner) : base(inner)
            {
            }

            public override IReadOnlyList<ActionOffer> AvailableTurnActions =>
                Array.Empty<ActionOffer>();
            public override ActionOffer? AvailableHoldOffer => null;
        }

        [Test]
        public void TheActingHerosOwnScreen_ReadsYourTurnWithInitiativeDemotedToDetail()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var actor = session.Projection.SelectedActor;

            var model = Narrate(session.Projection, actor.ActorId);

            Assert.That(model.Status, Is.EqualTo("YOUR TURN — CHOOSE AN ACTION, OR END YOUR TURN"));
            Assert.That(model.StatusDetail, Is.EqualTo($"INITIATIVE {actor.InitiativeTotal}"));
        }

        [Test]
        public void EveryOtherViewer_ReadsTheActingHerosNameAndInitiativeOnly()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var actor = session.Projection.SelectedActor;
            var spectator = session.Projection.Setup.Heroes
                .Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;
            var actorName = session.Projection.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(actor.ActorId)).DisplayName.ToUpperInvariant();

            var model = Narrate(new SpectatorReadModel(session.Projection), spectator);

            Assert.That(model.Status,
                Is.EqualTo($"{actorName} — ACTING (INITIATIVE {actor.InitiativeTotal})"));
            Assert.That(model.StatusDetail, Is.Null,
                "Initiative is stated once, inside the headline itself, for a spectator's line.");
        }

        [Test]
        public void ADefenseAnsweredByTheOwningViewer_ReadsTheFullRichOwnDecisionLine()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var attacker = session.Projection.SelectedActor.ActorId;
            var defender = session.Projection.Setup.Heroes
                .Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;
            // Milestone 8.80. The choice names the attacking card as well as the attacker; this
            // narration case reads neither, so the three are a defined probe identity.
            var choice = new PendingDefenseResponseChoice(
                new ActionResolutionId("probe-resolution"),
                new ActionResolutionStepId("probe-step"),
                defender,
                attacker,
                new CardInstanceId("probe-attack-card"),
                new CardDefinitionReference(new CardDefinitionId("probe-definition"), "probe-revision"),
                new ActionDefinitionId("probe-action"),
                7,
                Array.Empty<ActionOffer>(),
                true);
            var projection = new DefenseChoiceReadModel(session.Projection, choice);

            var model = Narrate(projection, defender);

            Assert.That(model.Status, Is.EqualTo("YOUR DEFENSE — CHOOSE A CARD, OR DECLINE"));
            Assert.That(model.StatusDetail, Is.EqualTo("ATTACK 7"));
        }

        /// <summary>
        /// Without a standing public decision, another viewer sees ordinary activation. An active
        /// hero alone cannot imply a Defense, respawn or other response; 8.42c names those only
        /// from the explicit public DecisionOwner, never from private answer details.
        /// </summary>
        [Test]
        public void ANonOwningViewerWithoutAStandingDecisionNamesTheActingHero()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var actor = session.Projection.SelectedActor;
            var otherViewer = session.Projection.Setup.Heroes
                .Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;
            Assert.That(session.Projection.PendingDefenseChoice, Is.Null,
                "The fixture under test must genuinely carry no owned pending family, or this " +
                "proves nothing about ordinary activation.");
            Assert.That(session.Projection.DecisionOwner.IsDefined, Is.False);

            var model = Narrate(new SpectatorReadModel(session.Projection), otherViewer);
            var actorName = session.Projection.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(actor.ActorId)).DisplayName.ToUpperInvariant();

            Assert.That(model.Status, Does.Not.Contain("DEFENSE"));
            Assert.That(model.Status, Does.Not.Contain("ATTACK"));
            Assert.That(model.Status, Does.Not.Contain("LEVEL UP"));
            Assert.That(model.Status, Does.Not.Contain("RESPAWN"));
            Assert.That(model.StatusDetail, Is.Null);
            Assert.That(model.Status,
                Is.EqualTo($"{actorName} — ACTING (INITIATIVE {actor.InitiativeTotal})"));
        }

        [Test]
        public void AMovementChoiceForTheOwningViewer_ReadsTheOwnDecisionLineWithSpacesDemoted()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var actor = session.Projection.SelectedActor.ActorId;
            var origin = session.Projection.Heroes.Single(hero => hero.HeroId.Equals(actor)).Coordinate;
            var choice = new PendingMovementChoice(
                new ActionResolutionId("probe-resolution"),
                new ActionResolutionStepId("probe-step"),
                actor,
                actor,
                origin,
                3,
                // `false`: a movement that could stop mid-way is a legal End Turn candidate too
                // (see `CreateEndTurnInvocation`), which is a different case from this one - this
                // choice's own family header and demoted detail.
                false,
                Array.Empty<GuardsDigital.Core.Rules.Movement.ReachableMovementDestination>());
            var projection = new MovementChoiceReadModel(session.Projection, choice);

            var model = Narrate(projection, actor);

            Assert.That(model.Status, Is.EqualTo("YOUR MOVEMENT — CHOOSE A DESTINATION"));
            Assert.That(model.StatusDetail, Is.EqualTo("3 SPACES LEFT"));
        }

        [Test]
        public void TheReleasingHerosOwnScreen_ReadsYourTurnReleaseToContinue()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var actor = session.Projection.SelectedActor;
            var release = new PendingTurnRelease(actor.ActorId, actor.CardInstanceId, actor.TeamSide);
            var projection = new TurnReleaseReadModel(session.Projection, release);

            var model = Narrate(projection, actor.ActorId);

            Assert.That(model.Status, Is.EqualTo("YOUR TURN — RELEASE TO CONTINUE"));
        }

        [Test]
        public void EveryOtherViewer_ReadsTheReleasingHerosNameAwaitingRelease()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var actor = session.Projection.SelectedActor;
            var spectator = session.Projection.Setup.Heroes
                .Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;
            var actorName = session.Projection.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(actor.ActorId)).DisplayName.ToUpperInvariant();
            var release = new PendingTurnRelease(actor.ActorId, actor.CardInstanceId, actor.TeamSide);
            var projection = new TurnReleaseReadModel(session.Projection, release);

            var model = Narrate(projection, spectator);

            Assert.That(model.Status, Is.EqualTo($"{actorName} — TURN COMPLETE, AWAITING RELEASE"));
        }

        [Test]
        public void TheViewersOwnUnansweredCommitment_ReadsTheOwnDecisionLine()
        {
            var session = CreateSession();
            var hero = session.Projection.Setup.Heroes.First().HeroId;
            var projection = new CommitmentReadModel(
                session.Projection, Awaiting(session.Projection, hero));

            var model = Narrate(projection, hero);

            Assert.That(model.Status, Is.EqualTo("YOUR COMMITMENT — CHOOSE A CARD, OR PASS"));
        }

        [Test]
        public void AnotherHerosUnansweredCommitment_NamesThemInAWaitingOnLine()
        {
            var session = CreateSession();
            var heroes = session.Projection.Setup.Heroes.ToArray();
            var owing = heroes[0];
            var viewer = heroes[1].HeroId;
            var projection = new CommitmentReadModel(
                session.Projection, Awaiting(session.Projection, owing.HeroId));

            var model = Narrate(projection, viewer);

            Assert.That(model.Status, Is.EqualTo(
                $"COMMITTING — WAITING ON: {owing.DisplayName.ToUpperInvariant()}"));
        }

        [Test]
        public void AnUnansweredAgreementParticipant_IsNamedInAWaitingOnLineForEveryViewer()
        {
            var session = CreateSession();
            var red = session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var blue = session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue);
            var proposal = new TeamAgreementProposalId("narration-probe");
            var bluesCard = session.Projection.Cards.First(card => card.OwnerId.Equals(blue.HeroId)).Id;
            var agreement = new TeamAgreementProjection(
                TeamAgreementKind.Initiative,
                proposal,
                TeamSide.Blue,
                new[] { new TeamAgreementCandidateProjection(bluesCard, blue.HeroId) },
                new[]
                {
                    new TeamAgreementParticipantProjection(red.HeroId, false, default),
                    new TeamAgreementParticipantProjection(blue.HeroId, true, bluesCard)
                },
                0,
                Array.Empty<TeamAgreementCommand>());
            var projection = new AgreementReadModel(session.Projection, agreement);

            var redsView = Narrate(projection, red.HeroId);
            var bluesView = Narrate(projection, blue.HeroId);

            Assert.That(redsView.Status, Is.EqualTo(
                $"TIED INITIATIVE — WAITING ON: {red.DisplayName.ToUpperInvariant()}"));
            Assert.That(bluesView.Status, Is.EqualTo(redsView.Status),
                "The proposal and every participant's statement are public and identical for " +
                "every seat - only AuthorizedCommands differs, which this line never reads.");
        }

        // -------------------------------------------------------------------------------------
        // Milestone 8.35z4 - THE EMPTY WAITING-SET MATRIX.
        //
        // The first live internet playtest crashed the round panel within minutes, on every
        // refresh, for the rest of the match:
        //
        //     GUARDS_HUD: PANEL_REFRESH_FAILED panel=round
        //     ArgumentException: A waiting-on line requires at least one named hero.
        //
        // 2,550 green cases did not see it, and the reason is exactly what these cases add. Every
        // narration case above stages a waiting set with somebody IN it - which is the interesting
        // case to a test author and the harmless one to a player. Nothing staged the EMPTY set: the
        // ordinary, legal, transient moment when the last participant has answered and the
        // authority has not yet resolved what that unblocked. Every seat refreshes through it.
        //
        // These cases stage that moment in each phase that can produce it. The bug they pin is not
        // "a vocabulary method throws" - it is "a caller handed a decision to a method that had
        // already documented it would refuse it", so the assertions below are about what the panel
        // SAYS in that state, not merely that it survives.
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// State: a tied-Initiative agreement every participant has already answered. The exact
        /// shape that took the panel down live.
        /// </summary>
        [Test]
        public void ATiedInitiativeEveryParticipantHasAnswered_ReadsTheEveryAnswerInLine()
        {
            var session = CreateSession();
            var red = session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var blue = session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue);
            var redsCard = session.Projection.Cards.First(card => card.OwnerId.Equals(red.HeroId)).Id;
            var bluesCard = session.Projection.Cards.First(card => card.OwnerId.Equals(blue.HeroId)).Id;
            var agreement = new TeamAgreementProjection(
                TeamAgreementKind.Initiative,
                new TeamAgreementProposalId("every-answer-in-probe"),
                TeamSide.Blue,
                new[] { new TeamAgreementCandidateProjection(bluesCard, blue.HeroId) },
                new[]
                {
                    // BOTH have stated. `AgreementStillOwed` reads `!HasStatement` for an
                    // Initiative agreement, so the owed set is empty - and before this milestone
                    // that empty set went straight into `NarrationLabels.WaitingOn`.
                    new TeamAgreementParticipantProjection(red.HeroId, true, redsCard),
                    new TeamAgreementParticipantProjection(blue.HeroId, true, bluesCard)
                },
                0,
                Array.Empty<TeamAgreementCommand>());
            var projection = new AgreementReadModel(session.Projection, agreement);

            var redsView = Narrate(projection, red.HeroId);
            var bluesView = Narrate(projection, blue.HeroId);

            Assert.That(redsView.Status,
                Is.EqualTo("TIED INITIATIVE — " + NarrationLabels.EveryAnswerIn),
                "An answered-by-everyone agreement is a real state and gets a real sentence - " +
                "never a waiting-on line naming nobody, and never a throw.");
            Assert.That(bluesView.Status, Is.EqualTo(redsView.Status),
                "Every participant's statement is public, so this line is identical on every seat.");
        }

        /// <summary>
        /// State: a Minion Battle every participant has finished picking, with picks that differ.
        /// Milestone 9.29 made it a real standing state - complete sets that differ remove nothing -
        /// and it carries a status detail, asserted here so the demoted line is not silently lost.
        /// </summary>
        [Test]
        public void AMinionBattleEveryParticipantHasPickedDifferently_ReadsThatPicksDoNotMatch()
        {
            var session = CreateSession();
            var red = session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var blue = session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue);
            var minions = session.Projection.Minions
                .Select(minion => GuardsDigital.Core.Rules.Modifications.RuleObject.ForMinion(minion.MinionId))
                .Take(3)
                .ToArray();
            var agreement = new TeamAgreementProjection(
                TeamAgreementKind.MinionBattle,
                new TeamAgreementProposalId("every-pick-in-probe"),
                TeamSide.Blue,
                Array.Empty<TeamAgreementCandidateProjection>(),
                new[]
                {
                    new TeamAgreementParticipantProjection(
                        red.HeroId, true, default, new[] { minions[0], minions[1] }, true),
                    new TeamAgreementParticipantProjection(
                        blue.HeroId, true, default, new[] { minions[1], minions[2] }, true)
                },
                2,
                Array.Empty<TeamAgreementCommand>(),
                red.HeroId);
            var projection = new AgreementReadModel(session.Projection, agreement);

            var model = Narrate(projection, red.HeroId);

            Assert.That(model.Status, Is.EqualTo("MINION BATTLE — PICKS DON'T MATCH YET"),
                "Every set is complete and the minions are still standing - the line says why, " +
                "never a waiting-on line naming nobody, and never a throw.");
            Assert.That(model.StatusDetail, Is.EqualTo("2 OF 2 MINIONS PICKED"),
                "The demoted detail counts the viewer's own picks.");
        }

        /// <summary>
        /// Milestone 9.29. The other side is told who is choosing and nothing of what.
        /// </summary>
        [Test]
        public void AMinionBattleTheViewerIsNotChoosing_SaysWhoIsChoosingAndNothingElse()
        {
            var session = CreateSession();
            var red = session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var blue = session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue);
            var agreement = new TeamAgreementProjection(
                TeamAgreementKind.MinionBattle,
                new TeamAgreementProposalId("other-side-probe"),
                TeamSide.Blue,
                Array.Empty<TeamAgreementCandidateProjection>(),
                new[] { new TeamAgreementParticipantProjection(blue.HeroId, false, default) },
                1,
                Array.Empty<TeamAgreementCommand>(),
                red.HeroId);

            var model = Narrate(new AgreementReadModel(session.Projection, agreement), red.HeroId);

            Assert.That(model.Status, Is.EqualTo(
                "MINION BATTLE — " + MinionChoiceLabels.TeamLabel(TeamSide.Blue) +
                " IS CHOOSING WHICH MINION TO REMOVE"));
            Assert.That(model.StatusDetail, Is.Null.Or.Empty);
        }

        /// <summary>
        /// State: the commitment phase with nothing still owed. This branch already guarded its
        /// empty set before this milestone - it is staged here anyway, because the point of the
        /// matrix is that EVERY phase which can build a waiting-on line is checked with an empty
        /// one, not that the two that happened to crash are.
        /// </summary>
        [Test]
        public void TheCommitmentPhaseWithNothingStillOwed_ReadsItsOwnLineAndNeverAnEmptyWaitingOn()
        {
            var session = CreateSession();
            var viewer = session.Projection.Setup.Heroes.First().HeroId;
            var projection = new CommitmentReadModel(
                session.Projection, Array.Empty<MatchCommitmentProjection>());

            var model = Narrate(projection, viewer);

            Assert.That(model.Status, Does.Not.Contain("WAITING ON"),
                "Nobody is owed, so nobody may be named as owed.");
            Assert.That(model.Status, Does.Contain("choosing their cards"));
        }

        /// <summary>
        /// The vocabulary's own guard is deliberately UNCHANGED by this milestone. The fix belongs
        /// at the callers, which owe a real sentence for the empty case; relaxing this instead would
        /// have bought a meaningless "WAITING ON: " line on the panel and disarmed the guard for
        /// every future caller.
        /// </summary>
        [Test]
        public void TheWaitingOnVocabulary_StillRefusesToNameNobody()
        {
            Assert.That(
                () => NarrationLabels.WaitingOn(Array.Empty<string>()),
                Throws.ArgumentException,
                "A waiting-on line naming nobody is not a sentence, and the vocabulary must go on " +
                "saying so - that is what makes the next caller's omission loud instead of silent.");
            Assert.That(NarrationLabels.EveryAnswerIn, Is.Not.Empty,
                "and the honest alternative it forces the caller to supply exists.");
        }

        /// <summary>
        /// THE GUARD THAT WOULD HAVE CAUGHT THIS CLASS FOREVER, and the one this milestone most
        /// wants kept.
        ///
        /// <para>Every case above pins one state somebody thought to stage. This one stages nothing:
        /// it drives a real six-seat match through a long seeded walk and rebuilds the round panel
        /// for EVERY seat, plus the Director, at EVERY step - the same sweep
        /// <c>ConfiguredMatchSceneRuntime.RefreshAll</c> performs on every projection a live client
        /// receives. It asserts the failure sink stays empty.</para>
        ///
        /// <para>This is the shape of the defect that shipped: not a state anybody got wrong on
        /// purpose, but a legal state nobody enumerated. A panel that must survive every projection
        /// of a real match is a claim a walk can check and a hand-written matrix cannot.</para>
        /// </summary>
        [Test]
        public void TheRoundPanelSurvivesEveryProjectionOfASeededMatchForEveryViewer()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var seats = host.Seats.Seats.ToArray();
            Assert.That(seats, Has.Length.EqualTo(6), "The official six-seat configuration.");

            // The stand-in for milestone 8.35h's panel-failure sink. Live, a throw here is caught,
            // logged as PANEL_REFRESH_FAILED, and the panel FREEZES at its last good text - which is
            // why the defect reached a human as a stuck sentence rather than as a crash.
            var failures = new List<string>();
            var refreshes = 0;

            void SweepEveryViewer()
            {
                foreach (var seat in seats)
                {
                    var seatProjection = host.ProjectionForSeat(seat.Id);
                    refreshes++;
                    try
                    {
                        RoundActionPresenter.CreateViewModel(seatProjection, seat.HeroId);
                    }
                    catch (Exception error)
                    {
                        failures.Add($"seat {seat.Id} hero {seat.HeroId}: {error.GetType().Name}: " +
                            error.Message);
                    }
                }

                refreshes++;
                try
                {
                    // The Director's own composition, which legitimately passes the undefined hero.
                    RoundActionPresenter.CreateViewModel(host.Projection);
                }
                catch (Exception error)
                {
                    failures.Add($"director: {error.GetType().Name}: {error.Message}");
                }
            }

            SweepEveryViewer();
            var walked = OwnerScopedProjectionTests.DriveThroughSeats(host, 600, _ =>
            {
                SweepEveryViewer();
                // Never stop: the hook is being used to sweep the whole walk, not to find a state.
                return false;
            });

            Assert.That(walked, Is.GreaterThan(100),
                "The walk must actually cover a long stretch of a real match, or the sweep below " +
                "proves very little.");
            Assert.That(refreshes, Is.GreaterThan(700),
                "Seven viewers per step over that walk.");
            Assert.That(failures, Is.Empty,
                "The round panel must compose for every viewer at every projection of a real " +
                "match. Any entry here is a live PANEL_REFRESH_FAILED: the panel freezes at its " +
                "last good text and every surface riding the same refresh - the kinetic cue " +
                "emission included - is starved for the rest of the match.");
        }

        // -------------------------------------------------------------------------------------
        // Acting-hero board highlight (milestone 8.35n, the FOURTH amendment): every viewer's
        // board marks SelectedActor's own piece, and clears the moment no actor stands.
        // `BoardInteractionViewModel.FocusedHeroId` is the public, unscoped fact the marker reads
        // (see `UnityMatchFeatureViews.BoardInteractionUnityView.RenderActingHeroMarker`) - proven
        // here at the pure view-model layer, which is what "present on all viewers" and "clears
        // with no actor" are actually claims about.
        // -------------------------------------------------------------------------------------

        [Test]
        public void TheBoardsActingHeroFact_IsPublicAndIdenticalForEveryViewer()
        {
            var session = CreateSession();
            StartTiedTurn(session, ActionKind.Movement);
            var actor = session.Projection.SelectedActor.ActorId;
            var directorView = new CapturingBoardView();
            new BoardInteractionPresenter(
                new StubMatchClient(session.Projection), directorView).Refresh();

            Assert.That(directorView.Model.FocusedHeroId, Is.EqualTo(actor),
                "SelectedActor is public and unscoped, so the board's acting-hero fact is the " +
                "same for the Director and for every seat's own projection alike.");
        }

        [Test]
        public void TheBoardsActingHeroFact_ClearsTheMomentNoActorStands()
        {
            var session = CreateSession();
            Assert.That(session.Projection.SelectedActor.IsDefined, Is.False,
                "A freshly-configured match awaiting its first commitments has no actor yet, or " +
                "this fixture proves nothing about clearing.");
            var view = new CapturingBoardView();
            new BoardInteractionPresenter(
                new StubMatchClient(session.Projection), view).Refresh();

            Assert.That(view.Model.FocusedHeroId.IsDefined, Is.False);
        }
    }
}
