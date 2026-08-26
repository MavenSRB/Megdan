using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.2a acceptance check 3: one bounded synthetic capability fixture executes end to end
    /// through <see cref="IMatchSession"/>, the round/turn authorities, the effect dispatch seam,
    /// snapshots, restore, replay, and retained-prefix Undo, with rejected input atomic and accepted
    /// execution emitting deterministic ordered facts.
    ///
    /// It runs on <c>CreateForSkillFixture</c> rather than the default configuration, because the
    /// default is what every pinned digest measures and a Skill card in it would move them for a
    /// content reason rather than a schema one.
    /// </summary>
    public sealed class MatchSessionSkillDispatchTests
    {
        [Test]
        public void Skill_IsOfferedAsAnOrdinaryTurnAction()
        {
            var session = CreateSkillSession();
            CommitSkillCards(session);

            var offer = CurrentSkillOffer(session);

            Assert.That(offer.IsAvailable, Is.True,
                "A printed Skill is an action offer like any other; presentation must not have to " +
                "derive its legality.");
        }

        [Test]
        public void StartingASkill_PausesOnAnAuthoritativeChoiceCarryingItsOwnInvocations()
        {
            var session = CreateSkillSession();
            CommitSkillCards(session);

            var started = session.Execute(DirectorCommand.StartAction(
                CurrentSkillOffer(session).ActionDefinitionId));

            Assert.That(started.Accepted, Is.True, started.Detail);
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, "The `may` step must open a real decision.");
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            Assert.That(choice.DeclineInvocation, Is.Not.Null);
            Assert.That(session.Projection.PendingSkillResolution, Is.Not.Null);
        }

        [Test]
        public void StaleEffectChoiceInput_IsRejectedAtomically()
        {
            var session = CreateSkillSession();
            CommitSkillCards(session);
            Accept(session, DirectorCommand.StartAction(CurrentSkillOffer(session).ActionDefinitionId));

            var before = session.Projection;
            var stale = DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                before.PendingEffectChoice.ResolutionId,
                new EffectChoiceId("stale-effect-choice"),
                EffectChoiceCommandKind.Confirm));

            var rejected = session.Execute(stale);

            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RejectionReason,
                Is.EqualTo(MatchSessionRejectionReason.CommandUnavailable));
            Assert.That(session.Projection.Version, Is.EqualTo(before.Version));
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(before.DiagnosticSnapshot));
        }

        [Test]
        public void DecliningTheOptionalStep_CompletesTheSkillAndResolvesItsCard()
        {
            var session = CreateSkillSession();
            CommitSkillCards(session);
            var card = session.Projection.SelectedActor.CardInstanceId;
            Accept(session, DirectorCommand.StartAction(CurrentSkillOffer(session).ActionDefinitionId));

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.DeclineInvocation));

            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                "A completed Skill leaves no pending resolution behind.");
            Assert.That(
                session.Projection.Cards.Single(candidate => candidate.Id.Equals(card))
                    .State.ResolutionStatus,
                Is.Not.EqualTo(CardResolutionStatus.Unresolved),
                "A Skill resolves its source card exactly as every other completed action does.");
        }

        [Test]
        public void TakingTheOptionalStep_RunsTheCapabilityChoiceToCompletion()
        {
            var session = CreateSkillSession();
            CommitSkillCards(session);
            Accept(session, DirectorCommand.StartAction(CurrentSkillOffer(session).ActionDefinitionId));

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            var target = session.Projection.PendingEffectChoice;
            Assert.That(target, Is.Not.Null, "Confirming the `may` step runs its child, which asks.");
            Assert.That(target.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));

            Accept(session, DirectorCommand.AnswerEffectChoice(target.SelectInvocations[0]));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            Assert.That(session.Projection.PendingSkillResolution, Is.Null);
        }

        [Test]
        public void PausedSkill_SurvivesSnapshotRestoreAndContinuesIdentically()
        {
            var session = CreateSkillSession();
            CommitSkillCards(session);
            Accept(session, DirectorCommand.StartAction(CurrentSkillOffer(session).ActionDefinitionId));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));

            var payload = session.CaptureSnapshot();
            var live = session.Projection.PendingEffectChoice;

            var restored = CreateSkillSession();
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);

            var revived = restored.Projection.PendingEffectChoice;
            Assert.That(revived, Is.Not.Null, "A paused Skill must survive the snapshot boundary.");
            Assert.That(revived.ChoiceId, Is.EqualTo(live.ChoiceId));
            Assert.That(revived.NodeId, Is.EqualTo(live.NodeId));
            Assert.That(revived.ChooserId, Is.EqualTo(live.ChooserId));
            Assert.That(
                revived.Candidates.Select(candidate => candidate.ToString()),
                Is.EqualTo(live.Candidates.Select(candidate => candidate.ToString())),
                "Candidates are never written; they are re-derived from the trusted definition, so a " +
                "restored choice offers exactly what the live one did.");

            Accept(restored, DirectorCommand.AnswerEffectChoice(revived.SelectInvocations[0]));
            Accept(restored, DirectorCommand.AnswerEffectChoice(
                restored.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(restored.Projection.PendingSkillResolution, Is.Null);
        }

        [Test]
        public void SkillExecution_ReplaysAndUndoesDeterministically()
        {
            var session = CreateSkillSession();
            CommitSkillCards(session);
            Accept(session, DirectorCommand.StartAction(CurrentSkillOffer(session).ActionDefinitionId));
            var beforeAnswer = session.Projection.DiagnosticSnapshot;

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            var afterAnswer = session.Projection.DiagnosticSnapshot;
            Assert.That(afterAnswer, Is.Not.EqualTo(beforeAnswer),
                "Accepted effect execution must emit ordered facts.");

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeAnswer),
                "Retained-prefix Undo must reproduce the exact prior state of a paused effect frame.");

            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterAnswer),
                "Replaying the same answer must reproduce the same ordered facts.");
        }

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        /// <summary>Commits each side's Skill card, so the Skill card is the one being resolved.</summary>
        private static void CommitSkillCards(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.Id.Value.Contains("focus"));
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
            {
                Accept(session, DirectorCommand.ChooseTiedActor(
                    tie.OwningSide, tie.CandidateCardInstanceIds[0]));
            }
        }

        private static ActionOffer CurrentSkillOffer(MatchSession session) =>
            session.Projection.AvailableTurnActions.Single(offer =>
                session.Projection.Definitions
                    .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                    .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId))
                    .Kind == ActionKind.Skill);

        private static MatchSession CreateSkillSession()
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
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
