using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.12: rulebook p.8's "Remove Tokens, Return Markers" step.
    ///
    /// The page states the whole rule in two sentences - "Remove all tokens from the board. Return all
    /// markers back into the supply." - and p.19 supplies the one condition on the first of them:
    /// "Unless the card text says otherwise, tokens are removed at the end of round (See Page 8)."
    ///
    /// The stage has existed in <see cref="EndOfRoundStage"/> since the list was written from p.8's own
    /// headings, and the round authority's restore guard already refused a pending Lane-Push request at
    /// any earlier stage. What this fixture proves is that the stage now does the page's work, and that
    /// it does it before the two things p.8 prints after it: the Lane Push and Retrieve Cards.
    /// </summary>
    public sealed class EndOfRoundComponentCleanupTests
    {
        private static readonly EffectNodeId PlaceBranch = new EffectNodeId("component-place-branch");
        private static readonly EffectNodeId GiveBranch = new EffectNodeId("component-give-marker");

        [Test]
        public void TheCleanupStage_RemovesEveryBoardTokenAndReturnsEveryMarker()
        {
            var session = StartRoundWithAPlacedTokenAndAHeldMarker();
            AdvanceToEndOfRound(session);
            Assert.That(session.Projection.PlacedTokens, Is.Not.Empty,
                "The token has to survive to the cleanup for the cleanup to be what removed it.");
            Assert.That(RestoreMarkers(session).Attachments, Is.Not.Empty);

            AdvanceStage(session, EndOfRoundStage.MinionBattle);
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.RemoveTokensAndReturnMarkers));

            var result = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(session.Projection.PlacedTokens, Is.Empty,
                "p.8: \"Remove all tokens from the board.\"");
            var markers = RestoreMarkers(session);
            Assert.That(markers.Attachments, Is.Empty,
                "p.8: \"Return all markers back into the supply.\"");
            Assert.That(markers.CountAvailable(SyntheticComponentDefinitions.FocusDefinition),
                Is.EqualTo(SyntheticComponentDefinitions.FocusDefinition.SupplyCount),
                "A returned marker is back in the supply, not merely detached.");
            var board = RestoreBoard(session);
            Assert.That(
                SyntheticComponentDefinitions.CreateCatalog().CountAvailableTokens(
                    SyntheticComponentDefinitions.HazardV4, board.PlacedTokens),
                Is.EqualTo(SyntheticComponentDefinitions.HazardDefinition.SupplyCount),
                "A removed token is back in its supply for the next round to place.");
        }

        /// <summary>
        /// The block's second sentence: "An end-of-round Lane-Push request remains pending until this
        /// cleanup finishes." p.8 prints the cleanup between the Minion Battle that can request a push
        /// and the Push itself, and Retrieve Cards after both, so this drives all four stages in one
        /// round and reads the order off the events rather than off the stage enum's integers.
        /// </summary>
        [Test]
        public void TheCleanup_FinishesBeforeAnOwedLanePushAndBeforeCardRetrieval()
        {
            var session = StartRoundWithAPlacedTokenAndAHeldMarker(EmptyBlueBattleZoneExceptOneMelee);
            AdvanceToEndOfRound(session);

            AdvanceStage(session, EndOfRoundStage.MinionBattle);
            AnswerEveryMinionBattleRemoval(session);
            Assert.That(session.Projection.PendingLanePushRequest, Is.Not.Null,
                "Losing the last effective Battle-Zone minion records p.8's request.");
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.RemoveTokensAndReturnMarkers));
            Assert.That(session.Projection.PlacedTokens, Is.Not.Empty,
                "The request is standing and the board is not cleared yet, which is the whole ordering.");

            var cleanup = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(cleanup.Accepted, Is.True, cleanup.Detail);
            Assert.That(cleanup.Events.Select(value => value.Kind), Is.EqualTo(new[]
            {
                RoundResolutionEventKind.TokenRemovedAtEndOfRound,
                RoundResolutionEventKind.MarkerReturnedAtEndOfRound,
                RoundResolutionEventKind.EndOfRoundStageResolved
            }), "Tokens then markers, in the order p.8's two sentences are printed.");
            Assert.That(session.Projection.PlacedTokens, Is.Empty);
            Assert.That(RestoreMarkers(session).Attachments, Is.Empty);
            Assert.That(session.Projection.PendingLanePushRequest, Is.Not.Null,
                "The push is still owed: the cleanup runs before it, and consumes nothing of it.");
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.ResolveRequestedLanePush));

            var push = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(push.Accepted, Is.True, push.Detail);
            Assert.That(push.Events.Select(value => value.Kind),
                Has.Member(RoundResolutionEventKind.LanePushStageResolved));
            Assert.That(session.Projection.PendingLanePushRequest, Is.Null,
                "Only now is the request consumed, which is after the cleanup finished.");

            var retrieval = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(retrieval.Accepted, Is.True, retrieval.Detail);
            Assert.That(retrieval.Events.Select(value => value.Kind),
                Has.Member(RoundResolutionEventKind.CardRetrieved).And
                    .Member(RoundResolutionEventKind.ActiveEffectCancellationCheckpoint),
                "Retrieve Cards and its one checkpoint come last of the four, exactly as p.8 prints them.");
        }

        /// <summary>
        /// The cleanup crosses the persistence boundary like every other stage: a snapshot taken after it
        /// restores to the same cleared state, a full replay of the retained command prefix reproduces it
        /// byte for byte, and an Undo of the advance puts the token and the marker back rather than
        /// leaving the round holding components the board no longer has.
        /// </summary>
        [Test]
        public void TheCleanup_SurvivesRestoreReplayAndRetainedPrefixUndo()
        {
            var session = StartRoundWithAPlacedTokenAndAHeldMarker();
            AdvanceToEndOfRound(session);
            AdvanceStage(session, EndOfRoundStage.MinionBattle);
            var tokenBefore = session.Projection.PlacedTokens.Single().Id;
            var markerBefore = RestoreMarkers(session).Attachments.Single().Id;

            Accept(session, DirectorCommand.AdvanceEndOfRound());
            var expectedSnapshot = session.Projection.AuthoritativeSnapshot;

            var restored = Restore(session);
            Assert.That(restored.Board.PlacedTokens, Is.Empty);
            Assert.That(restored.Round.Markers.Attachments, Is.Empty);

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.PlacedTokens.Single().Id, Is.EqualTo(tokenBefore),
                "Undo truncates the accepted prefix and rebuilds, so the cleared token is standing again.");
            Assert.That(RestoreMarkers(session).Attachments.Single().Id, Is.EqualTo(markerBefore));
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.RemoveTokensAndReturnMarkers));

            Accept(session, DirectorCommand.AdvanceEndOfRound());

            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(expectedSnapshot),
                "Replaying the same advance reproduces the same authoritative state exactly.");
            Assert.That(session.Projection.PlacedTokens, Is.Empty);
            Assert.That(RestoreMarkers(session).Attachments, Is.Empty);
        }

        private static void AnswerEveryMinionBattleRemoval(MatchSession session)
        {
            while (session.Projection.PendingMinionBattleChoice != null)
            {
                var choice = session.Projection.PendingMinionBattleChoice;
                if (choice.CanConfirm)
                {
                    Accept(session, DirectorCommand.ConfirmMinionBattleRemovals(choice.ConfirmInvocation));
                    continue;
                }

                Accept(session, DirectorCommand.SelectMinionBattleRemoval(choice.SelectInvocations[0]));
            }
        }

        /// <summary>
        /// Leaves Blue exactly one Melee minion in the Battle Zone against Red's full complement, so the
        /// Minion Battle removes that one minion, empties Blue's side, and records p.8's Lane-Push
        /// request while the fixture's token and marker are still standing.
        /// </summary>
        private static IEnumerable<QuickGameMinionSetupState> EmptyBlueBattleZoneExceptOneMelee(
            IReadOnlyList<QuickGameMinionSetupState> minions)
        {
            var retained = minions
                .Where(value => value.TeamSide == TeamSide.Blue)
                .OrderBy(value => value.MinionId.Value, StringComparer.Ordinal)
                .First(value => value.MinionType == StandardMinionType.Melee)
                .MinionId;
            return minions.Where(value =>
                value.TeamSide != TeamSide.Blue || value.MinionId.Equals(retained));
        }

        private static MatchSession StartRoundWithAPlacedTokenAndAHeldMarker(
            Func<IReadOnlyList<QuickGameMinionSetupState>, IEnumerable<QuickGameMinionSetupState>>
                selectMinions = null)
        {
            var session = CreateComponentSession(selectMinions);
            CommitComponentCards(session);

            StartComponentSkillOnCurrentTurn(session);
            ChooseOption(session, PlaceBranch);
            AnswerFirstCandidate(session);

            StartComponentSkillOnCurrentTurn(session);
            ChooseOption(session, GiveBranch);
            AnswerFirstCandidate(session);

            Assert.That(session.Projection.PlacedTokens.Count, Is.EqualTo(1));
            Assert.That(RestoreMarkers(session).Attachments.Count, Is.EqualTo(1));
            return session;
        }

        /// <summary>
        /// Answers a target choice with its first candidate and confirms it, which is the two-command
        /// shape every selection in this engine has.
        /// </summary>
        private static void AnswerFirstCandidate(MatchSession session)
        {
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null);
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations[0]));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static void ChooseOption(MatchSession session, EffectNodeId option)
        {
            var choice = session.Projection.PendingEffectChoice;
            var invocation = choice.SelectInvocations.Single(candidate =>
                candidate.Candidate.OptionNodeId.Equals(option));
            Accept(session, DirectorCommand.AnswerEffectChoice(invocation));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static void StartComponentSkillOnCurrentTurn(MatchSession session)
        {
            var offer = session.Projection.AvailableTurnActions.Single(value =>
                value.ActionDefinitionId.Equals(new ActionDefinitionId("primary-component-skill")));
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
        }

        private static void AdvanceStage(MatchSession session, EndOfRoundStage expected)
        {
            Assert.That(session.Projection.CurrentEndOfRoundStage, Is.EqualTo(expected));
            Accept(session, DirectorCommand.AdvanceEndOfRound());
        }

        private static void AdvanceToEndOfRound(MatchSession session)
        {
            while (session.Projection.Phase != RoundResolutionPhase.EndOfRound)
            {
                if (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitAnyCards(session);
                    continue;
                }

                Assert.That(session.Projection.IsTurnActive, Is.True);
                Accept(session, DirectorCommand.PerformHold(
                    session.Projection.SelectedActor.CardInstanceId));
            }
        }

        private static void CommitAnyCards(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = HandCards(session, side).First();
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            ResolveInitiativeTie(session);
        }

        private static void CommitComponentCards(MatchSession session)
        {
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = HandCards(session, side)
                    .Single(candidate => candidate.Id.Value.Contains("cache"));
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            ResolveInitiativeTie(session);
        }

        private static CardInstance[] HandCards(MatchSession session, TeamSide side) =>
            session.Projection.Cards
                .Where(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.State.Location == CardLocation.Hand)
                .ToArray();

        private static void ResolveInitiativeTie(MatchSession session)
        {
            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }
        }

        private static AdjacentMoveAuthority RestoreBoard(MatchSession session) => Restore(session).Board;

        private static MarkerAttachmentRegister RestoreMarkers(MatchSession session) =>
            Restore(session).Round.Markers;

        private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static MatchSession CreateComponentSession(
            Func<IReadOnlyList<QuickGameMinionSetupState>, IEnumerable<QuickGameMinionSetupState>>
                selectMinions)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForSkillFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var fullSetup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var setup = selectMinions == null
                ? fullSetup
                : new QuickGameSetupState(
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
    }
}
