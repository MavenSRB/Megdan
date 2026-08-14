using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
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

        [Test]
        public void BoardCues_MarkTheSelectedAndPointedAtMinionsAndNothingElse()
        {
            var session = CreatePendingRemovalSession();
            var boardView = new CapturingBoardView();
            var board = new BoardInteractionPresenter(session, boardView);
            var choice = session.Projection.PendingMinionBattleChoice;

            board.Refresh();
            Assert.That(boardView.Model.MinionChoiceCues, Is.Empty,
                "Nothing is selected and nothing is pointed at, so the board stays unmarked.");

            var pointedAt = choice.LegalSelectMinionIds[0];
            Assert.That(board.HighlightMinionChoiceCandidate(pointedAt), Is.True);
            Assert.That(board.HighlightMinionChoiceCandidate(pointedAt), Is.False,
                "An unchanged hover rebuilds nothing.");
            board.Refresh();
            var highlighted = boardView.Model.MinionChoiceCues.Single();
            Assert.That(highlighted.MinionId, Is.EqualTo(pointedAt));
            Assert.That(highlighted.Kind, Is.EqualTo(MinionChoiceCueKind.Highlighted));
            Assert.That(highlighted.Label, Is.EqualTo(
                $"#{MinionChoiceLabels.Ordinal(choice.Candidates, pointedAt)}"),
                "The cue carries the same ordinal as the control that points at it.");

            var heavy = choice.Candidates.Single(value => value.MinionType == StandardMinionType.Heavy);
            Assert.That(board.HighlightMinionChoiceCandidate(heavy.MinionId), Is.True);
            board.Refresh();
            Assert.That(boardView.Model.MinionChoiceCues.Single().MinionId, Is.EqualTo(heavy.MinionId),
                "A candidate can be pointed at even while the authority offers it no control.");

            Assert.That(session.Execute(DirectorCommand.SelectMinionBattleRemoval(
                choice.SelectInvocations.Single(value => value.MinionId.Equals(pointedAt)))).Accepted,
                Is.True);
            board.Refresh();
            var selectedCue = boardView.Model.MinionChoiceCues.Single();
            Assert.That(selectedCue.MinionId, Is.EqualTo(pointedAt),
                "The accepted command replaced the projection, which drops the pointed-at cue with it.");
            Assert.That(selectedCue.Kind, Is.EqualTo(MinionChoiceCueKind.Selected));
            Assert.That(selectedCue.Label, Is.EqualTo(MinionChoiceLabels.SelectedCueText));

            Assert.That(board.HighlightMinionChoiceCandidate(heavy.MinionId), Is.True);
            board.Refresh();
            Assert.That(boardView.Model.MinionChoiceCues.Select(value => (value.MinionId, value.Kind)),
                Is.EquivalentTo(new[]
                {
                    (pointedAt, MinionChoiceCueKind.Selected),
                    (heavy.MinionId, MinionChoiceCueKind.Highlighted)
                }),
                "A selected minion keeps saying so while the player points at another.");

            Assert.That(session.Execute(DirectorCommand.ConfirmMinionBattleRemovals(
                session.Projection.PendingMinionBattleChoice.ConfirmInvocation)).Accepted, Is.True);
            board.Refresh();
            Assert.That(boardView.Model.MinionChoiceCues, Is.Empty);
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
        private static MatchSession CreatePendingRemovalSession()
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
