using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.1a: the player-facing Minion Battle removal choice.
    ///
    /// Every rule the prompt appears to express - which minions are candidates, that Heavy comes
    /// last, how many must go, when the choice is complete - is decided by milestone 6.1's authority
    /// and reaches presentation only through the immutable pending choice. These cases therefore
    /// assert two things at once: that the prompt shows what the projection holds, and that it
    /// forwards the projection's own typed invocations rather than composing its own.
    /// </summary>
    public sealed class MinionChoicePresentationTests
    {
        [Test]
        public void Prompt_NamesTheChooserAndOffersExactlyTheLegalCandidates()
        {
            var session = CreatePendingRemovalSession();
            var view = new CapturingMinionChoiceView();
            var presenter = new MinionChoicePresenter(session, view);

            presenter.Refresh();

            var choice = session.Projection.PendingMinionBattleChoice;
            var model = view.Model;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.ChoosingSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(model.ChoosingTeamId, Is.EqualTo(choice.LosingTeamId));
            Assert.That(model.ResolutionId, Is.EqualTo(choice.ResolutionId));
            Assert.That(model.ChoiceId, Is.EqualTo(choice.ChoiceId));
            Assert.That(model.RequiredRemovalCount, Is.EqualTo(choice.RequiredRemovalCount));
            Assert.That(model.SelectedCount, Is.Zero);
            Assert.That(model.PromptLabel, Does.Contain("MINION BATTLE").And.Contain("[B] BLUE")
                .And.Contain(choice.RequiredRemovalCount.ToString()));
            Assert.That(model.StatusLabel, Is.EqualTo($"SELECTED 0 OF {choice.RequiredRemovalCount}"));

            Assert.That(model.Candidates.Select(value => value.MinionId),
                Is.EqualTo(choice.LegalSelectMinionIds),
                "The prompt offers exactly the candidates the authority currently calls legal.");
            Assert.That(model.Candidates, Has.All.Matches<MinionChoiceCandidateViewModel>(value =>
                !value.IsSelected));
            Assert.That(model.ConfirmInvocation, Is.Null,
                "Nothing is selected yet, so there is no confirmation to offer.");

            var heavy = choice.Candidates.Single(value => value.MinionType == StandardMinionType.Heavy);
            Assert.That(model.Candidates.Select(value => value.MinionId), Has.None.EqualTo(heavy.MinionId),
                "Heavy is removed last, so while a non-Heavy is unselected it carries no control.");

            foreach (var candidate in model.Candidates)
            {
                var projected = choice.Candidates.Single(value => value.MinionId.Equals(candidate.MinionId));
                Assert.That(candidate.Label, Does.Contain("REMOVE").And.Contain("[B] BLUE")
                    .And.Contain(projected.MinionType.ToString().ToUpperInvariant())
                    .And.Contain($"#{candidate.Ordinal}"),
                    "Team and intrinsic type are readable as text, not signalled by color alone.");
                Assert.That(candidate.Command.Kind,
                    Is.EqualTo(DirectorCommandKind.SelectMinionBattleRemoval));
                Assert.That(candidate.Command, Is.EqualTo(DirectorCommand.SelectMinionBattleRemoval(
                    choice.SelectInvocations.Single(value => value.MinionId.Equals(candidate.MinionId)))));
            }

            Assert.That(model.Candidates.Select(value => value.Ordinal).Distinct().Count(),
                Is.EqualTo(model.Candidates.Count),
                "Two candidates of the same team and type stay distinguishable by their ordinal.");
        }

        [Test]
        public void Prompt_ForwardsProjectedSelectionAndCompletesAtTheExactRequiredCount()
        {
            var session = CreatePendingRemovalSession();
            var view = new CapturingMinionChoiceView();
            var presenter = new MinionChoicePresenter(session, view);
            presenter.Refresh();
            var required = session.Projection.PendingMinionBattleChoice.RequiredRemovalCount;
            var chosen = view.Model.Candidates[0];

            Assert.That(presenter.Submit(chosen), Is.True);

            Assert.That(session.Projection.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.SelectMinionBattleRemoval));
            Assert.That(session.Projection.PendingMinionBattleChoice.SelectedMinionIds,
                Is.EqualTo(new[] { chosen.MinionId }));

            presenter.Refresh();
            var selectedRow = view.Model.Candidates.Single(value => value.MinionId.Equals(chosen.MinionId));
            Assert.That(selectedRow.IsSelected, Is.True);
            Assert.That(selectedRow.Label, Does.StartWith("KEEP"));
            Assert.That(selectedRow.Command.Kind, Is.EqualTo(DirectorCommandKind.DeselectMinionBattleRemoval));
            Assert.That(view.Model.SelectedCount, Is.EqualTo(1));
            Assert.That(view.Model.StatusLabel, Is.EqualTo($"SELECTED 1 OF {required}"));
            Assert.That(view.Model.ConfirmInvocation, Is.Not.Null,
                "One selection is the required count in this fixture, so the choice can be confirmed.");

            Assert.That(presenter.Submit(selectedRow), Is.True, "A selected candidate can be taken back.");
            Assert.That(session.Projection.PendingMinionBattleChoice.SelectedMinionIds, Is.Empty);
            presenter.Refresh();
            Assert.That(view.Model.ConfirmInvocation, Is.Null);
            Assert.That(presenter.Confirm(), Is.False,
                "An under-count confirmation is not offered and is not submitted.");
            Assert.That(session.Projection.PendingMinionBattleChoice, Is.Not.Null);

            presenter.Refresh();
            Assert.That(presenter.Submit(view.Model.Candidates.Single(value =>
                value.MinionId.Equals(chosen.MinionId))), Is.True);
            var beforeMinionCount = session.Projection.Minions.Count;
            Assert.That(presenter.Confirm(), Is.True);

            Assert.That(session.Projection.PendingMinionBattleChoice, Is.Null);
            Assert.That(session.Projection.Minions, Has.Count.EqualTo(beforeMinionCount - 1));
            Assert.That(session.Projection.Minions.Select(value => value.MinionId),
                Has.None.EqualTo(chosen.MinionId));
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.RemoveTokensAndReturnMarkers));
            presenter.Refresh();
            Assert.That(view.Model, Is.Null, "A completed choice leaves no controls behind.");
        }

        [Test]
        public void Prompt_ExposesNoControlsWithoutAPendingChoiceAndHidesTheEndOfRoundAdvance()
        {
            var equal = CreateSession(minions => minions);
            AdvanceToEndOfRound(equal);
            var view = new CapturingMinionChoiceView();
            var presenter = new MinionChoicePresenter(equal, view);

            presenter.Refresh();
            Assert.That(view.Model, Is.Null, "Equal Battle-Zone counts are not a choice.");
            Assert.That(presenter.Confirm(), Is.False);
            Assert.That(equal.Projection.AcceptedCommands.Select(value => value.Kind),
                Has.None.EqualTo(DirectorCommandKind.ConfirmMinionBattleRemovals));
            Assert.That(RoundActionPresenter.CreateViewModel(equal.Projection).ContinueEndOfRoundInvocation,
                Is.Not.Null,
                "With no mandatory choice the end-of-round advance stays available.");

            var pending = CreatePendingRemovalSession();
            Assert.That(RoundActionPresenter.CreateViewModel(pending.Projection).ContinueEndOfRoundInvocation,
                Is.Null,
                "The authority rejects an advance while a removal choice stands, so no control offers it.");
        }

        [Test]
        public void Prompt_RefusesAStaleControlAndRebuildsTheVisibleSelectionFromAuthority()
        {
            var session = CreatePendingRemovalSession();
            var view = new CapturingMinionChoiceView();
            var presenter = new MinionChoicePresenter(session, view);
            presenter.Refresh();
            var staleRow = view.Model.Candidates[0];
            var otherRow = view.Model.Candidates[1];
            Assert.That(presenter.Submit(staleRow), Is.True);
            var snapshot = session.CaptureSnapshot();
            var history = session.Projection.AcceptedCommands.ToArray();

            // `staleRow` came from the projection before that selection. Its select invocation is no
            // longer offered, and the exact-count rule would refuse it anyway.
            Assert.That(presenter.Submit(staleRow), Is.False);
            Assert.That(presenter.Submit(otherRow), Is.False);

            Assert.That(session.CaptureSnapshot(), Is.EqualTo(snapshot),
                "A refused control changes no authoritative state.");
            Assert.That(session.Projection.AcceptedCommands, Is.EqualTo(history));
            Assert.That(view.Model.SelectedCount, Is.EqualTo(1));
            Assert.That(view.Model.Candidates.Single(value =>
                value.MinionId.Equals(staleRow.MinionId)).IsSelected, Is.True,
                "The visible selection is rebuilt from the pending choice the refusal preserved.");
        }

        [Test]
        public void Prompt_ReconstructsItselfAfterRestoreReplayAndUndo()
        {
            var live = CreatePendingRemovalSession();
            var livePresenter = new MinionChoicePresenter(live, new CapturingMinionChoiceView());
            livePresenter.Refresh();
            var selected = MinionChoicePresenter.CreateViewModel(live.Projection).Candidates[0];
            Assert.That(livePresenter.Submit(selected), Is.True);
            var expected = MinionChoicePresenter.CreateViewModel(live.Projection);
            var payload = live.CaptureSnapshot();

            // Both rebuilds start from a fresh match: replay re-executes the whole accepted history,
            // and restore installs the captured state, so neither may begin already advanced.
            var replayed = CreateSession(RemoveOneBlueRanged);
            foreach (var command in live.Projection.AcceptedCommands)
            {
                Assert.That(replayed.Execute(command).Accepted, Is.True);
            }

            var restored = CreateSession(RemoveOneBlueRanged);
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);

            AssertPromptsEquivalent(expected, MinionChoicePresenter.CreateViewModel(replayed.Projection));
            AssertPromptsEquivalent(expected, MinionChoicePresenter.CreateViewModel(restored.Projection));

            Assert.That(live.Execute(DirectorCommand.ConfirmMinionBattleRemovals(
                live.Projection.PendingMinionBattleChoice.ConfirmInvocation)).Accepted, Is.True);
            Assert.That(MinionChoicePresenter.CreateViewModel(live.Projection), Is.Null);

            Assert.That(live.Undo().Accepted, Is.True);
            AssertPromptsEquivalent(expected, MinionChoicePresenter.CreateViewModel(live.Projection));
            var afterUndo = new CapturingMinionChoiceView();
            var rebuilt = new MinionChoicePresenter(live, afterUndo);
            rebuilt.Refresh();
            Assert.That(rebuilt.Confirm(), Is.True,
                "The rebuilt prompt drives the same continuation, holding no separate local one.");
            Assert.That(live.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.RemoveTokensAndReturnMarkers));
        }

        /// <summary>
        /// Milestone 8.11a, at the user's request: the Minion Battle is answered by clicking the piece,
        /// so the board marks every piece that may be clicked - not only one a control was pointing at,
        /// because there are no controls naming candidates any more.
        ///
        /// <para>What is marked is the authority's own legal selection, never the raw candidate list.
        /// Rulebook p.8 removes Heavy minions last, so while a non-Heavy still stands the Heavy is a
        /// candidate and not a legal one, and marking it would be an offer the rules refuse. That case
        /// is the reason this test exists in this shape: the first version of the board-first marking
        /// read the candidate list and offered the Heavy.</para>
        /// </summary>
        [Test]
        public void BoardCues_MarkEveryLegallySelectableUnitAndNeverAHeavyTheRulesWithhold()
        {
            var session = CreatePendingRemovalSession();
            var boardView = new CapturingBoardView();
            var board = new BoardInteractionPresenter(session, boardView);
            var choice = session.Projection.PendingMinionBattleChoice;
            var heavy = choice.Candidates.Single(value => value.MinionType == StandardMinionType.Heavy);

            board.Refresh();

            Assert.That(choice.LegalSelectMinionIds, Is.Not.Empty,
                "The scenario has something to choose, or the rest of this asserts over nothing.");
            Assert.That(choice.Candidates.Select(value => value.MinionId),
                Does.Contain(heavy.MinionId),
                "and it has a Heavy among the candidates, which is the case being pinned.");
            Assert.That(choice.LegalSelectMinionIds, Has.No.Member(heavy.MinionId),
                "which the authority withholds while a non-Heavy still stands.");
            Assert.That(
                boardView.Model.PieceTargetCues.Select(value =>
                    (value.Target, value.State, value.ConsequenceIntent)),
                Is.EquivalentTo(choice.LegalSelectMinionIds
                    .Select(id => (
                        BoardObjectReference.ForMinion(id),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Removal))),
                "Every legal selection is marked, and nothing else is - the Heavy included.");

            var chosen = choice.LegalSelectMinionIds[0];
            Assert.That(session.Execute(DirectorCommand.SelectMinionBattleRemoval(
                choice.SelectInvocations.Single(value => value.MinionId.Equals(chosen)))).Accepted,
                Is.True);
            board.Refresh();

            var afterSelection = session.Projection.PendingMinionBattleChoice;
            var selectedCue = boardView.Model.PieceTargetCues.Single(value =>
                value.Target.Equals(BoardObjectReference.ForMinion(chosen)));
            Assert.That(selectedCue.State, Is.EqualTo(BoardTargetCueState.Selected),
                "A chosen piece says so.");
            Assert.That(selectedCue.ConsequenceIntent,
                Is.EqualTo(BoardTargetConsequenceIntent.Removal),
                "Minion Battle invokes removal, so its cue carries the removal palette intent.");
            Assert.That(
                boardView.Model.PieceTargetCues
                    .Where(value => value.State == BoardTargetCueState.Eligible)
                    .Select(value => value.Target.MinionId),
                Is.EquivalentTo(afterSelection.LegalSelectMinionIds),
                "and the rest of the board still offers exactly what the authority still offers.");

            Assert.That(session.Execute(DirectorCommand.ConfirmMinionBattleRemovals(
                session.Projection.PendingMinionBattleChoice.ConfirmInvocation)).Accepted, Is.True);
            board.Refresh();
            Assert.That(boardView.Model.PieceTargetCues, Is.Empty);
            Assert.That(boardView.Model.MinionDefenseCues, Is.Empty,
                "Choice cues stay separate from the Defense contribution cues.");
        }

        private static void AssertPromptsEquivalent(
            MinionChoiceViewModel expected,
            MinionChoiceViewModel actual)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.ResolutionId, Is.EqualTo(expected.ResolutionId));
            Assert.That(actual.ChoiceId, Is.EqualTo(expected.ChoiceId));
            Assert.That(actual.ChoosingTeamId, Is.EqualTo(expected.ChoosingTeamId));
            Assert.That(actual.ChoosingSide, Is.EqualTo(expected.ChoosingSide));
            Assert.That(actual.RequiredRemovalCount, Is.EqualTo(expected.RequiredRemovalCount));
            Assert.That(actual.SelectedCount, Is.EqualTo(expected.SelectedCount));
            Assert.That(actual.PromptLabel, Is.EqualTo(expected.PromptLabel));
            Assert.That(actual.StatusLabel, Is.EqualTo(expected.StatusLabel));
            Assert.That(
                actual.Candidates.Select(value => (value.MinionId, value.Label, value.Ordinal,
                    value.IsSelected, value.Command)),
                Is.EqualTo(expected.Candidates.Select(value => (value.MinionId, value.Label,
                    value.Ordinal, value.IsSelected, value.Command))));
            Assert.That(actual.ConfirmInvocation?.Command, Is.EqualTo(expected.ConfirmInvocation?.Command));
        }

        /// <summary>A configured match paused on a real one-of-several removal choice.</summary>
        internal static MatchSession CreatePendingRemovalSession()
        {
            var session = CreateSession(RemoveOneBlueRanged);
            AdvanceToEndOfRound(session);
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            Assert.That(session.Projection.PendingMinionBattleChoice, Is.Not.Null);
            return session;
        }

        private static MatchSession CreateSession(
            Func<IReadOnlyList<QuickGameMinionSetupState>, IEnumerable<QuickGameMinionSetupState>> selectMinions)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var fullSetup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var setup = new QuickGameSetupState(
                fullSetup.SelectedMap,
                fullSetup.WaveCount,
                fullSetup.TieBreakerState,
                fullSetup.Teams,
                fullSetup.Heroes,
                selectMinions(fullSetup.Minions));
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

        private static IEnumerable<QuickGameMinionSetupState> RemoveOneBlueRanged(
            IReadOnlyList<QuickGameMinionSetupState> minions)
        {
            var removed = minions.First(value =>
                value.TeamSide == TeamSide.Blue && value.MinionType == StandardMinionType.Ranged);
            return minions.Where(value => !value.MinionId.Equals(removed.MinionId));
        }

        private static void AdvanceToEndOfRound(MatchSession session)
        {
            while (session.Projection.Phase != RoundResolutionPhase.EndOfRound)
            {
                if (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitUnequalCards(session);
                    continue;
                }

                Assert.That(session.Projection.IsTurnActive, Is.True);
                Assert.That(session.Execute(DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId)).Accepted, Is.True);
            }
        }

        private static void CommitUnequalCards(MatchSession session)
        {
            var red = EligibleCards(session, TeamSide.Red).OrderBy(value => Initiative(session, value)).First();
            var blue = EligibleCards(session, TeamSide.Blue)
                .OrderByDescending(value => Initiative(session, value)).First();
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null);
        }

        private static IEnumerable<CardInstance> EligibleCards(MatchSession session, TeamSide side)
        {
            var heroId = session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId;
            return session.Projection.Cards.Where(value =>
                value.OwnerId.Equals(heroId) && value.State.Location == CardLocation.Hand);
        }

        private static int Initiative(MatchSession session, CardInstance card) =>
            session.Projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference)).Initiative.Value;

        // ---------------------------------------------------------------------------------------
        // Milestone 6.4b: the displaced-minion correction rides the same interface. The fixtures are
        // the 6.4a authority scenarios - a push that displaces red-melee-01 to (-3,-1) with two
        // equal-shortest returns, driven through the Skill-fixture hands - and every case below runs
        // through the one presenter the removal cases above use, which is the no-mode-leakage claim
        // made executable.
        // ---------------------------------------------------------------------------------------

        [Test]
        public void CorrectionPrompt_NamesTheChooserAndOffersExactlyTheProjectedDestinations()
        {
            var session = CreatePendingCorrectionSession();
            var view = new CapturingMinionChoiceView();
            var presenter = new MinionChoicePresenter(session, view);

            presenter.Refresh();

            var pending = session.Projection.PendingDisplacedMinionCorrection;
            var model = view.Model;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Kind, Is.EqualTo(MinionChoicePromptKind.CorrectionMove));
            Assert.That(model.SectionLabel, Is.EqualTo("Correction"));
            Assert.That(model.PromptLabel,
                Is.EqualTo("CORRECTION (MOVE) - [R] RED CHOOSES THE RETURN SPACE"));
            Assert.That(model.ChoosingTeamId, Is.EqualTo(pending.ChoosingTeamId));
            Assert.That(model.ChoosingSide, Is.EqualTo(TeamSide.Red));
            Assert.That(model.ActionResolutionId, Is.EqualTo(pending.ResolutionId));
            Assert.That(model.ActionChoiceId, Is.EqualTo(pending.ChoiceId));
            Assert.That(model.SubjectMinionId, Is.EqualTo(pending.SubjectMinionId),
                "The prompt names the displaced stable minion identity.");
            Assert.That(model.ConfirmInvocation, Is.Null,
                "Selection is the advance for a correction; there is nothing separate to confirm.");
            Assert.That(
                model.Candidates.Select(value => value.Destination.Value),
                Is.EqualTo(pending.DestinationCandidates.Select(value => value.Destination)),
                "Exactly the projected route endpoints, in the projection's own order.");
            foreach (var candidate in model.Candidates)
            {
                Assert.That(candidate.MinionId, Is.EqualTo(pending.SubjectMinionId));
                Assert.That(candidate.Label,
                    Does.StartWith("MOVE TO").And.Contain(
                        $"({candidate.Destination.Value.Q},{candidate.Destination.Value.R})"));
                Assert.That(candidate.Command, Is.EqualTo(DirectorCommand.ChooseCorrectionDestination(
                    pending.SelectInvocations.Single(value =>
                        value.Destination.Equals(candidate.Destination.Value)))));
            }
        }

        [Test]
        public void CorrectionPrompt_SubmitsThroughTheSharedPresenterAndCompletesTheAction()
        {
            var session = CreatePendingCorrectionSession();
            var view = new CapturingMinionChoiceView();
            var presenter = new MinionChoicePresenter(session, view);
            presenter.Refresh();
            var chosen = view.Model.Candidates[0];

            Assert.That(presenter.Submit(chosen), Is.True,
                "The same Submit path the removal uses forwards the correction invocation.");

            Assert.That(session.Projection.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.ChooseCorrectionDestination));
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null);
            Assert.That(
                session.Projection.Minions.Single(value => value.MinionId.Equals(chosen.MinionId))
                    .Coordinate,
                Is.EqualTo(chosen.Destination.Value));
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Value.Contains("red-shove"))
                    .State.ResolutionStatus,
                Is.Not.EqualTo(CardResolutionStatus.Unresolved),
                "Answering resumes the same authoritative continuation: the source card resolves.");
            presenter.Refresh();
            Assert.That(view.Model, Is.Null, "A completed correction leaves no prompt behind.");
        }

        [Test]
        public void CorrectionPrompt_RefusesAStaleControlAndPreservesThePendingChoice()
        {
            var session = CreatePendingCorrectionSession();
            var view = new CapturingMinionChoiceView();
            var presenter = new MinionChoicePresenter(session, view);
            presenter.Refresh();
            var stale = new MinionChoiceCandidateViewModel(
                view.Model.SubjectMinionId,
                "MOVE TO (99,99)",
                1,
                false,
                DirectorCommand.ChooseCorrectionDestination(
                    new GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionCommand(
                        GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionCommandKind.SelectDestination,
                        session.Projection.PendingDisplacedMinionCorrection.ResolutionId,
                        session.Projection.PendingDisplacedMinionCorrection.ChoiceId,
                        TeamSide.Red,
                        view.Model.SubjectMinionId,
                        new GuardsDigital.Core.Board.HexCoordinate(99, 99))),
                new GuardsDigital.Core.Board.HexCoordinate(99, 99));
            var snapshotBefore = session.Projection.AuthoritativeSnapshot;
            var acceptedBefore = session.Projection.AcceptedCommands.Count;

            Assert.That(presenter.Submit(stale), Is.False,
                "A control the current model does not carry submits nothing.");

            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshotBefore));
            Assert.That(session.Projection.AcceptedCommands, Has.Count.EqualTo(acceptedBefore));
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Not.Null,
                "The authoritative pending choice survives untouched.");
            Assert.That(view.Model, Is.Not.Null,
                "The refusal rebuilt the visible prompt from authority.");
        }

        [Test]
        public void CorrectionSubjectPrompt_OffersTheTeamsMinionsAndProgressesThroughBoth()
        {
            var session = CreateTwoSubjectCorrectionSession();
            var view = new CapturingMinionChoiceView();
            var presenter = new MinionChoicePresenter(session, view);
            presenter.Refresh();

            var model = view.Model;
            Assert.That(model, Is.Not.Null);
            Assert.That(model.Kind, Is.EqualTo(MinionChoicePromptKind.CorrectionSubject));
            Assert.That(model.PromptLabel,
                Is.EqualTo("CORRECTION - [R] RED RETURNS A DISPLACED MINION"));
            Assert.That(model.Candidates.Select(value => value.MinionId.Value),
                Is.EqualTo(new[] { "red-melee-01", "red-melee-02" }),
                "Both correctable displaced minions, in the projection's canonical order.");
            Assert.That(model.Candidates.Select(value => value.Label),
                Has.All.StartWith("RETURN [R] RED"),
                "Subject candidates stay distinguishable by team and intrinsic type as text.");

            Assert.That(presenter.Submit(model.Candidates.Single(value =>
                value.MinionId.Value == "red-melee-02")), Is.True);

            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null,
                "Each remaining subject had one shortest return, so the rest auto-continued.");
            presenter.Refresh();
            Assert.That(view.Model, Is.Null,
                "Deterministic no-choice corrections expose no prompt.");
        }

        [Test]
        public void CorrectionPrompt_ReconstructsItselfAfterRestore()
        {
            var session = CreatePendingCorrectionSession();
            var live = MinionChoicePresenter.CreateViewModel(session.Projection);
            var payload = session.CaptureSnapshot();

            var restored = CreatePendingCorrectionSession();
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);
            var revived = MinionChoicePresenter.CreateViewModel(restored.Projection);

            Assert.That(revived, Is.Not.Null);
            Assert.That(revived.Kind, Is.EqualTo(live.Kind));
            Assert.That(revived.PromptLabel, Is.EqualTo(live.PromptLabel));
            Assert.That(revived.SubjectMinionId, Is.EqualTo(live.SubjectMinionId));
            Assert.That(revived.Candidates.Select(value => value.Label),
                Is.EqualTo(live.Candidates.Select(value => value.Label)),
                "The prompt is rebuilt from authoritative pending state, never from a local copy.");
            Assert.That(revived.Candidates.Select(value => value.Command),
                Is.EqualTo(live.Candidates.Select(value => value.Command)));
        }

        /// <summary>
        /// Milestone 6.2d. Milestone 6.4b lit every legal endpoint for as long as the choice stood,
        /// which said only that several spaces were involved; each of them already carries its own
        /// control, so the board now follows the pointer to one of them. Nothing about which endpoints
        /// are legal, or how one is answered, moved with it - the submit below is 6.4b's, unchanged.
        /// </summary>
        [Test]
        public void CorrectionBoardAdapter_LightsOnlyTheEndpointBeingPointedAtAndMarksTheSubject()
        {
            var session = CreatePendingCorrectionSession();
            var boardView = new CapturingBoardView();
            var boardPresenter = new BoardInteractionPresenter(session, boardView);

            boardPresenter.Refresh();

            var pending = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(pending.DestinationCandidates.Count, Is.GreaterThan(1),
                "The fixture has to offer more than one endpoint, or following the pointer to one " +
                "of them proves nothing.");
            Assert.That(boardView.Model.CorrectionDestination, Is.Null,
                "Nobody is pointing at a control yet, so no endpoint is lit.");
            var subjectCue = boardView.Model.PieceTargetCues.Single();
            Assert.That(subjectCue.Target,
                Is.EqualTo(BoardObjectReference.ForMinion(pending.SubjectMinionId)),
                "The board marks which minion the offered destinations would receive, throughout.");
            Assert.That(subjectCue.State, Is.EqualTo(BoardTargetCueState.Selected));
            Assert.That(subjectCue.ConsequenceIntent,
                Is.EqualTo(BoardTargetConsequenceIntent.Affect),
                "Correction moves the subject; presentation does not predict a removal consequence.");

            var subject = RuleObject.ForMinion(pending.SubjectMinionId);
            var pointedAt = pending.DestinationCandidates[1].Destination;
            Assert.That(boardPresenter.HighlightMinionChoiceCandidate(subject, pointedAt), Is.True);
            Assert.That(boardPresenter.HighlightMinionChoiceCandidate(subject, pointedAt), Is.False,
                "An unchanged hover rebuilds nothing.");
            boardPresenter.Refresh();
            Assert.That(boardView.Model.CorrectionDestination, Is.EqualTo(pointedAt));
            Assert.That(boardView.Model.PieceTargetCues.Single().Target,
                Is.EqualTo(BoardObjectReference.ForMinion(pending.SubjectMinionId)),
                "Pointing at a destination says where it would land as well as which minion lands " +
                "there; the pair is what the prompt names.");

            var elsewhere = pending.DestinationCandidates[0].Destination;
            Assert.That(boardPresenter.HighlightMinionChoiceCandidate(subject, elsewhere), Is.True);
            boardPresenter.Refresh();
            Assert.That(boardView.Model.CorrectionDestination, Is.EqualTo(elsewhere),
                "One control at a time - moving the pointer moves the light rather than adding one.");

            Assert.That(
                boardPresenter.HighlightMinionChoiceCandidate(
                    subject, new HexCoordinate(99, 99)),
                Is.True);
            boardPresenter.Refresh();
            Assert.That(boardView.Model.CorrectionDestination, Is.Null,
                "A space this choice does not offer lights nothing, however it is reported.");

            Assert.That(boardPresenter.HighlightMinionChoiceCandidate(subject, null), Is.False,
                "Dropping an already-dropped highlight rebuilds nothing.");
            Assert.That(boardPresenter.SubmitDestination(elsewhere), Is.True,
                "Clicking a space still forwards only the projected invocation, lit or not.");
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null);
            Assert.That(
                session.Projection.Minions.Single(value =>
                    value.MinionId.Equals(pending.SubjectMinionId)).Coordinate,
                Is.EqualTo(elsewhere));

            boardPresenter.Refresh();
            Assert.That(boardView.Model.CorrectionDestination, Is.Null);
            Assert.That(boardView.Model.PieceTargetCues, Is.Empty);
        }


        [Test]
        public void RoundControls_AreWithheldWhileACorrectionIsPending()
        {
            var session = CreatePendingCorrectionSession();

            var model = RoundActionPresenter.CreateViewModel(session.Projection);

            Assert.That(model.Actions, Is.Empty,
                "No new action can start while the correction holds the current one open.");
            Assert.That(model.FastTravelOffers, Is.Empty);
            Assert.That(model.EndTurnInvocation, Is.Null,
                "End Turn is suppressed during the mandatory correction choice.");
            Assert.That(model.Status, Does.Contain("correction").IgnoreCase,
                "The status line names what the match is waiting on.");
        }

        /// <summary>
        /// The 6.4a push fixture: the Red hero at (0,-1) pushes red-melee-01 out of the Battle Zone to
        /// (-3,-1), whose two equal-shortest returns pause the correction on Red's route choice.
        /// </summary>
        private static MatchSession CreatePendingCorrectionSession()
        {
            var session = StartDisplacementSkill(CreateDisplacementSession(
                new GuardsDigital.Core.Board.HexCoordinate(0, -1), null, null));
            ChooseEffectOption(session, new EffectNodeId("displacement-object-push"));
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Not.Null,
                "The fixture must pause on the route choice, or these cases prove nothing.");
            return session;
        }

        /// <summary>
        /// Two Red minions displaced by one action: red-melee-01 out at (-3,-1) with its second return
        /// walled, red-melee-02 out at (-1,-3); the correction pauses on Red's subject choice, and each
        /// chosen subject then has exactly one shortest return.
        /// </summary>
        private static MatchSession CreateTwoSubjectCorrectionSession()
        {
            var session = StartDisplacementSkill(CreateDisplacementSession(
                new GuardsDigital.Core.Board.HexCoordinate(-2, -5),
                new[]
                {
                    ("red-melee-01", new GuardsDigital.Core.Board.HexCoordinate(-3, -1)),
                    ("red-melee-02", new GuardsDigital.Core.Board.HexCoordinate(-1, -3))
                },
                new[] { new GuardsDigital.Core.Board.HexCoordinate(-3, 0) }));
            ChooseEffectOption(session, new EffectNodeId("displacement-move-branch"));
            AcceptCommand(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.DeclineInvocation));
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Not.Null);
            return session;
        }

        private static void ChooseEffectOption(MatchSession session, EffectNodeId option)
        {
            var choice = session.Projection.PendingEffectChoice;
            var invocation = choice.SelectInvocations.Single(candidate =>
                candidate.Candidate.OptionNodeId.Equals(option));
            AcceptCommand(session, DirectorCommand.AnswerEffectChoice(invocation));
            AcceptCommand(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static void AcceptCommand(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static MatchSession StartDisplacementSkill(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.Id.Value.Contains("shove") &&
                    candidate.State.Location == CardLocation.Hand);
                AcceptCommand(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                AcceptCommand(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }

            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                candidate.ActionDefinitionId.Equals(new ActionDefinitionId("primary-displacement-skill")));
            AcceptCommand(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            return session;
        }

        private static MatchSession CreateDisplacementSession(
            GuardsDigital.Core.Board.HexCoordinate redAt,
            (string, GuardsDigital.Core.Board.HexCoordinate)[] minionMoves,
            IEnumerable<GuardsDigital.Core.Board.HexCoordinate> obstacleCoordinates)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var fullSetup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var moves = (minionMoves ?? System.Array.Empty<(string, GuardsDigital.Core.Board.HexCoordinate)>())
                .ToDictionary(move => move.Item1, move => move.Item2, StringComparer.Ordinal);
            var minions = fullSetup.Minions.Select(minion =>
                moves.TryGetValue(minion.MinionId.Value, out var at)
                    ? new QuickGameMinionSetupState(
                        minion.MinionId, minion.TeamId, minion.TeamSide, minion.MinionType,
                        minion.SpawnPoint,
                        new ConfiguredSpaceReference(
                            minion.OccupiedSpace.MapId, new SpaceId($"q{at.Q}_r{at.R}")),
                        at)
                    : minion).ToArray();
            var setup = new QuickGameSetupState(
                fullSetup.SelectedMap,
                fullSetup.WaveCount,
                fullSetup.TieBreakerState,
                fullSetup.Teams,
                fullSetup.Heroes,
                minions);
            var occupied = new HashSet<GuardsDigital.Core.Board.HexCoordinate>(
                minions.Select(minion => minion.Coordinate));
            var walls = (obstacleCoordinates ?? Enumerable.Empty<GuardsDigital.Core.Board.HexCoordinate>())
                .Where(coordinate => !occupied.Contains(coordinate))
                .Select((coordinate, index) => new ObstacleOccupancy(
                    new ObstacleId($"correction-presentation-wall-{index}"), coordinate));
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
                    new HeroOccupancy(blueHeroId, new GuardsDigital.Core.Board.HexCoordinate(-1, -5))
                },
                walls);
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private sealed class CapturingMinionChoiceView : IMinionChoiceView
        {
            public MinionChoiceViewModel Model { get; private set; }
            public void Render(MinionChoiceViewModel model) => Model = model;
        }

        private sealed class CapturingBoardView : IBoardInteractionView
        {
            public BoardInteractionViewModel Model { get; private set; }
            public void Render(BoardInteractionViewModel model) => Model = model;
        }
    }
}
