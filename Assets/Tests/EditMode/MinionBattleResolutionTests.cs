using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MinionBattleResolutionTests
    {
        [Test]
        public void EqualBattleZoneCounts_CompleteWithoutChoiceRemovalOrLanePush()
        {
            var session = CreateSession(minions => minions);
            AdvanceToEndOfRound(session);
            var beforeIds = session.Projection.Minions.Select(value => value.MinionId).ToArray();

            var result = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Events.Select(value => value.Kind),
                Is.EqualTo(new[] { RoundResolutionEventKind.EndOfRoundStageResolved }));
            Assert.That(result.Events.Single().EndOfRoundOutcomeKind,
                Is.EqualTo(EndOfRoundStageOutcomeKind.Completed));
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.RemoveTokensAndReturnMarkers));
            Assert.That(session.Projection.PendingMinionBattleChoice, Is.Null);
            Assert.That(session.Projection.PendingLanePushRequest, Is.Null);
            Assert.That(session.Projection.Minions.Select(value => value.MinionId), Is.EqualTo(beforeIds));
        }

        [Test]
        public void RemovalChoice_EnforcesOneAtATimeHeavyLastIdentityChooserAndAtomicConfirmation()
        {
            var session = CreateSession(RemoveOneBlueRanged);
            AdvanceToEndOfRound(session);
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            var choice = session.Projection.PendingMinionBattleChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.RequiredRemovalCount, Is.EqualTo(1));
            Assert.That(choice.ChoosingSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(choice.LosingTeamId,
                Is.EqualTo(session.Projection.Participants.Teams.Single(value => value.Side == TeamSide.Blue).Id));
            Assert.That(choice.SelectedMinionIds, Is.Empty);
            Assert.That(choice.LegalSelectMinionIds, Is.Not.Empty);
            Assert.That(choice.LegalSelectMinionIds, Has.None.Matches<MinionId>(id =>
                choice.Candidates.Single(value => value.MinionId.Equals(id)).MinionType == StandardMinionType.Heavy));
            Assert.That(choice.SelectInvocations.Select(value => value.MinionId),
                Is.EqualTo(choice.LegalSelectMinionIds));

            AssertAtomicRejection(
                session,
                DirectorCommand.ConfirmMinionBattleRemovals(choice.ConfirmInvocation));

            var legal = choice.SelectInvocations[0];
            var stale = new MinionBattleChoiceCommand(
                MinionBattleChoiceCommandKind.Select,
                new EndOfRoundResolutionId("stale-minion-battle"),
                choice.ChoiceId,
                choice.ChoosingSide,
                legal.MinionId);
            AssertAtomicRejection(session, DirectorCommand.SelectMinionBattleRemoval(stale));

            var wrongChooser = new MinionBattleChoiceCommand(
                MinionBattleChoiceCommandKind.Select,
                choice.ResolutionId,
                choice.ChoiceId,
                TeamSide.Red,
                legal.MinionId);
            AssertAtomicRejection(session, DirectorCommand.SelectMinionBattleRemoval(wrongChooser));

            var heavy = choice.Candidates.Single(value => value.MinionType == StandardMinionType.Heavy);
            var ineligibleHeavy = new MinionBattleChoiceCommand(
                MinionBattleChoiceCommandKind.Select,
                choice.ResolutionId,
                choice.ChoiceId,
                choice.ChoosingSide,
                heavy.MinionId);
            AssertAtomicRejection(session, DirectorCommand.SelectMinionBattleRemoval(ineligibleHeavy));

            var selectResult = session.Execute(DirectorCommand.SelectMinionBattleRemoval(legal));
            Assert.That(selectResult.Accepted, Is.True);
            Assert.That(selectResult.Events, Has.Count.EqualTo(1));
            Assert.That(selectResult.Events[0].Kind,
                Is.EqualTo(RoundResolutionEventKind.MinionBattleSelectionChanged));
            Assert.That(selectResult.Events[0].MinionBattleChoiceCommandKind,
                Is.EqualTo(MinionBattleChoiceCommandKind.Select));
            var selected = session.Projection.PendingMinionBattleChoice;
            Assert.That(selected.SelectedMinionIds, Is.EqualTo(new[] { legal.MinionId }));
            Assert.That(selected.CanConfirm, Is.True);
            Assert.That(selected.LegalSelectMinionIds, Is.Empty);

            var other = choice.Candidates.First(value => !value.MinionId.Equals(legal.MinionId));
            var overCount = new MinionBattleChoiceCommand(
                MinionBattleChoiceCommandKind.Select,
                choice.ResolutionId,
                choice.ChoiceId,
                choice.ChoosingSide,
                other.MinionId);
            AssertAtomicRejection(session, DirectorCommand.SelectMinionBattleRemoval(overCount));

            var deselect = selected.DeselectInvocations.Single(value => value.MinionId.Equals(legal.MinionId));
            var deselectResult = session.Execute(DirectorCommand.DeselectMinionBattleRemoval(deselect));
            Assert.That(deselectResult.Accepted, Is.True);
            Assert.That(deselectResult.Events, Has.Count.EqualTo(1));
            Assert.That(deselectResult.Events[0].MinionBattleChoiceCommandKind,
                Is.EqualTo(MinionBattleChoiceCommandKind.Deselect));
            Assert.That(session.Projection.PendingMinionBattleChoice.SelectedMinionIds, Is.Empty);
            Assert.That(session.Execute(DirectorCommand.SelectMinionBattleRemoval(legal)).Accepted, Is.True);

            var beforeCount = session.Projection.Minions.Count;
            var confirm = session.Execute(DirectorCommand.ConfirmMinionBattleRemovals(
                session.Projection.PendingMinionBattleChoice.ConfirmInvocation));

            Assert.That(confirm.Accepted, Is.True);
            Assert.That(confirm.Events.Select(value => value.Kind), Is.EqualTo(new[]
            {
                RoundResolutionEventKind.MinionBattleMinionRemoved,
                RoundResolutionEventKind.EndOfRoundStageResolved
            }));
            Assert.That(confirm.Events[0].MinionId, Is.EqualTo(legal.MinionId));
            Assert.That(session.Projection.Minions, Has.Count.EqualTo(beforeCount - 1));
            Assert.That(session.Projection.Minions.Select(value => value.MinionId),
                Has.None.EqualTo(legal.MinionId));
            Assert.That(session.Projection.PendingMinionBattleChoice, Is.Null);
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.RemoveTokensAndReturnMarkers));
        }

        [Test]
        public void PendingChoice_RoundTripsAndRebuildsByteIdenticallyThroughRestoreAndUndoPrefixes()
        {
            var session = CreateSession(RemoveOneBlueRanged);
            AdvanceToEndOfRound(session);
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            var select = session.Projection.PendingMinionBattleChoice.SelectInvocations[0];
            Assert.That(session.Execute(DirectorCommand.SelectMinionBattleRemoval(select)).Accepted, Is.True);
            var selectedSnapshot = session.CaptureSnapshot();
            var selectedCommands = session.Projection.AcceptedCommands.ToArray();

            var replayed = CreateSession(RemoveOneBlueRanged);
            foreach (var command in selectedCommands)
            {
                Assert.That(replayed.Execute(command).Accepted, Is.True);
            }

            Assert.That(replayed.CaptureSnapshot(), Is.EqualTo(selectedSnapshot));
            AssertChoicesEquivalent(
                session.Projection.PendingMinionBattleChoice,
                replayed.Projection.PendingMinionBattleChoice);

            var restored = CreateSession(RemoveOneBlueRanged);
            Assert.That(restored.RestoreSnapshot(selectedSnapshot).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(selectedSnapshot));
            AssertChoicesEquivalent(
                session.Projection.PendingMinionBattleChoice,
                restored.Projection.PendingMinionBattleChoice);

            Assert.That(session.Execute(DirectorCommand.ConfirmMinionBattleRemovals(
                session.Projection.PendingMinionBattleChoice.ConfirmInvocation)).Accepted, Is.True);
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(selectedSnapshot));
            AssertChoicesEquivalent(
                restored.Projection.PendingMinionBattleChoice,
                session.Projection.PendingMinionBattleChoice);

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.PendingMinionBattleChoice.SelectedMinionIds, Is.Empty);
            Assert.That(session.Projection.CurrentEndOfRoundStage, Is.EqualTo(EndOfRoundStage.MinionBattle));
        }

        [Test]
        public void AutomaticRemoval_EmitsHeavyLastAndDefersRecordedLanePushUntilAfterCleanup()
        {
            var session = CreateSession(KeepOneBlueOfEachType);
            AdvanceToEndOfRound(session);

            var battle = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(battle.Accepted, Is.True);
            var removals = battle.Events
                .Where(value => value.Kind == RoundResolutionEventKind.MinionBattleMinionRemoved)
                .ToArray();
            Assert.That(removals, Has.Length.EqualTo(3));
            var removedTypes = removals.Select(value => session.Projection.Setup.Minions
                .Single(minion => minion.MinionId.Equals(value.MinionId)).MinionType).ToArray();
            Assert.That(removedTypes.Take(2), Has.None.EqualTo(StandardMinionType.Heavy));
            Assert.That(removedTypes.Last(), Is.EqualTo(StandardMinionType.Heavy));
            Assert.That(battle.Events.Select(value => value.Kind), Does.Contain(RoundResolutionEventKind.LanePushRequested));
            Assert.That(session.Projection.PendingLanePushRequest, Is.Not.Null);
            Assert.That(session.Projection.PendingLanePushRequest.LosingSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(session.Projection.PendingLanePushRequest.ConsumptionCheckpoint,
                Is.EqualTo(EndOfRoundStage.ResolveRequestedLanePush));
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.RemoveTokensAndReturnMarkers));

            var requestId = session.Projection.PendingLanePushRequest.RequestId;
            var requestSnapshot = session.CaptureSnapshot();
            var restored = CreateSession(KeepOneBlueOfEachType);
            Assert.That(restored.RestoreSnapshot(requestSnapshot).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(requestSnapshot));
            Assert.That(restored.Projection.PendingLanePushRequest.RequestId, Is.EqualTo(requestId));
            Assert.That(restored.Projection.PendingLanePushRequest.CauseResolutionId,
                Is.EqualTo(session.Projection.PendingLanePushRequest.CauseResolutionId));
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.ResolveRequestedLanePush));
            Assert.That(session.Projection.PendingLanePushRequest.RequestId, Is.EqualTo(requestId));
            AssertAtomicRejection(session, DirectorCommand.AdvanceEndOfRound());
            Assert.That(session.Projection.PendingLanePushRequest.RequestId, Is.EqualTo(requestId));
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

        private static IEnumerable<QuickGameMinionSetupState> KeepOneBlueOfEachType(
            IReadOnlyList<QuickGameMinionSetupState> minions)
        {
            var retainedBlue = minions
                .Where(value => value.TeamSide == TeamSide.Blue)
                .GroupBy(value => value.MinionType)
                .Select(group => group.OrderBy(value => value.MinionId.Value, StringComparer.Ordinal).First())
                .Select(value => value.MinionId)
                .ToArray();
            return minions.Where(value =>
                value.TeamSide == TeamSide.Red || retainedBlue.Contains(value.MinionId));
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
            var blue = EligibleCards(session, TeamSide.Blue).OrderByDescending(value => Initiative(session, value)).First();
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

        private static void AssertAtomicRejection(MatchSession session, DirectorCommand command)
        {
            var snapshot = session.CaptureSnapshot();
            var history = session.Projection.AcceptedCommands.ToArray();
            var choice = session.Projection.PendingMinionBattleChoice;
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.False);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(snapshot));
            Assert.That(session.Projection.AcceptedCommands, Is.EqualTo(history));
            if (choice != null)
            {
                AssertChoicesEquivalent(choice, session.Projection.PendingMinionBattleChoice);
            }
        }

        private static void AssertChoicesEquivalent(
            PendingMinionBattleChoice expected,
            PendingMinionBattleChoice actual)
        {
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.ResolutionId, Is.EqualTo(expected.ResolutionId));
            Assert.That(actual.ChoiceId, Is.EqualTo(expected.ChoiceId));
            Assert.That(actual.LosingTeamId, Is.EqualTo(expected.LosingTeamId));
            Assert.That(actual.ChoosingSide, Is.EqualTo(expected.ChoosingSide));
            Assert.That(actual.RequiredRemovalCount, Is.EqualTo(expected.RequiredRemovalCount));
            Assert.That(actual.Candidates, Is.EqualTo(expected.Candidates));
            Assert.That(actual.SelectedMinionIds, Is.EqualTo(expected.SelectedMinionIds));
            Assert.That(actual.LegalSelectMinionIds, Is.EqualTo(expected.LegalSelectMinionIds));
            Assert.That(actual.LegalDeselectMinionIds, Is.EqualTo(expected.LegalDeselectMinionIds));
            Assert.That(actual.SelectInvocations, Is.EqualTo(expected.SelectInvocations));
            Assert.That(actual.DeselectInvocations, Is.EqualTo(expected.DeselectInvocations));
            Assert.That(actual.ConfirmInvocation, Is.EqualTo(expected.ConfirmInvocation));
        }
    }
}
