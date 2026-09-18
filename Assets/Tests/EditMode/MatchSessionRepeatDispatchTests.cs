using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.7 end to end: a bounded Repeat produces a second, separate action instance through
    /// <see cref="IMatchSession"/> commands, with p.18's after-action correction completed between the
    /// two, and with snapshots, restore, replay, Undo, and ordered facts crossing that boundary.
    ///
    /// The geography is the same official-map ground milestone 6.4a's correction fixture uses, for the
    /// same reason: the acting Red hero at (0,-1) stands beside two Red minions and one Blue Heavy, so
    /// a two-space push sends red-melee-01 across the zone boundary to a space with two equally short
    /// returns - a correction that really pauses - while the Heavy's standing immunity is what the
    /// second instance runs into once the pushed minions are out of reach.
    ///
    /// The frame-level half of this milestone - what the node is and why it cannot run away - is
    /// <see cref="BoundedRepeatActionTests"/>.
    /// </summary>
    public sealed class MatchSessionRepeatDispatchTests
    {
        private static readonly EffectNodeId PushBranch = new EffectNodeId("repeat-object-push");
        private static readonly EffectNodeId MoveBranch = new EffectNodeId("repeat-move-sequence");
        private static readonly EffectNodeId RepeatNode = new EffectNodeId("repeat-again");

        private static readonly ActionDefinitionId RepeatSkill =
            new ActionDefinitionId("primary-repeat-skill");

        private static readonly HexCoordinate ActorAt = new HexCoordinate(0, -1);
        private static readonly HexCoordinate DisplacedTo = new HexCoordinate(-3, -1);

        [Test]
        public void ADeclinedRepeat_EndsTheActionAtOneInstance()
        {
            var session = StartRepeatSkill();
            ChooseOption(session, PushBranch);

            Decline(session);
            AnswerCorrection(session);

            Assert.That(StartedInstances(session), Is.EqualTo(1),
                "Declining is an answer, not a deferral: no second instance is ever created.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Null);
            AssertCardResolved(session);
        }

        [Test]
        public void AConfirmedRepeat_WaitsForTheCorrectionBetweenTheTwoInstances()
        {
            var session = StartRepeatSkill();
            ChooseOption(session, PushBranch);

            Confirm(session);

            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                "The instance that asked to repeat has ended; its effect frame is gone.");
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Not.Null,
                "p.18's correction holds the action boundary open, and the repeat waits behind it.");
            Assert.That(StartedInstances(session), Is.EqualTo(1),
                "Acceptance check 2: the next instance has not begun while the correction is unanswered.");
            Assert.That(Coordinate(session, "red-melee-01"), Is.EqualTo(DisplacedTo));

            AnswerCorrection(session);

            Assert.That(StartedInstances(session), Is.EqualTo(2),
                "Once the boundary is clean the second instance begins, inside the same command.");
            Assert.That(session.Projection.PendingSkillResolution, Is.Not.Null);
        }

        [Test]
        public void TheTwoInstances_AreSeparateActionsWithTheCorrectionOrderedBetweenThem()
        {
            var session = StartRepeatSkill();
            ChooseOption(session, PushBranch);
            Confirm(session);
            AnswerCorrection(session);
            Stop(session);

            Assert.That(
                session.Projection.DiagnosticEvents
                    .Select(entry => entry.EventType)
                    .Where(type =>
                        type == "Action.SkillActionStarted" ||
                        type == "Action.SkillActionCompleted" ||
                        type == "Action.DisplacedMinionCorrectionStarted" ||
                        type == "Action.DisplacedMinionCorrectionCompleted")
                    .ToArray(),
                Is.EqualTo(new[]
                {
                    "Action.SkillActionStarted",
                    "Action.SkillActionCompleted",
                    "Action.DisplacedMinionCorrectionStarted",
                    "Action.DisplacedMinionCorrectionCompleted",
                    "Action.SkillActionStarted",
                    "Action.SkillActionCompleted"
                }),
                "Two complete action envelopes, not one action with an extra tail: each instance opens " +
                "and closes on its own, and the correction of the first finishes before the second opens.");
        }

        [Test]
        public void TheRepeatedInstance_RunsUnderRepeatedContextAndOffersNoRepeatOfItsOwn()
        {
            var session = StartRepeatSkill();
            ChooseOption(session, MoveBranch);
            MoveSubject(session, new HexCoordinate(-1, -1));
            Confirm(session);

            var repeated = session.Projection.PendingSkillResolution;
            Assert.That(repeated, Is.Not.Null);
            Assert.That(repeated.InvocationContext, Is.EqualTo(ActionInvocationContext.Repeated),
                "The explicit repeated context is what makes the second instance's own Repeat node " +
                "ineligible; nothing counts instances.");
            Assert.That(repeated.ResolutionId.Value, Is.Not.EqualTo(
                    $"session-r{session.Projection.RoundIndex}-t{session.Projection.TurnIndex}" +
                    "-red-volley-card-primary-repeat-skill"),
                "A separate instance carries a separate resolution identity.");

            var prompts = new System.Collections.Generic.List<string>();
            while (session.Projection.PendingEffectChoice != null)
            {
                var node = session.Projection.PendingEffectChoice.NodeId.Value;
                if (prompts.Count == 0 || prompts[prompts.Count - 1] != node)
                {
                    prompts.Add(node);
                }

                if (node == "repeat-choice")
                {
                    ChooseOption(session, MoveBranch);
                    continue;
                }

                SelectValue(session, session.Projection.PendingEffectChoice.Candidates[0].Value.Coordinate);
            }

            Assert.That(prompts, Is.EqualTo(new[]
            {
                "repeat-choice", "repeat-select-subject", "repeat-direct-move"
            }), "The repeated instance asks everything an ordinary one asks and nothing more: its own " +
                "Repeat node is skipped in silence rather than offered and then refused.");
            Assert.That(StartedInstances(session), Is.EqualTo(2));
            AssertCardResolved(session);
        }

        [Test]
        public void TheRepeatedInstance_MayNameADifferentLegalSubject()
        {
            var session = StartRepeatSkill();
            ChooseOption(session, MoveBranch);
            Assert.That(
                session.Projection.PendingEffectChoice.Candidates
                    .Select(candidate => candidate.Value.Coordinate),
                Is.EquivalentTo(new[] { new HexCoordinate(-1, -1), new HexCoordinate(-1, 0) }));
            MoveSubject(session, new HexCoordinate(-1, -1));
            Confirm(session);

            ChooseOption(session, MoveBranch);
            var second = session.Projection.PendingEffectChoice.Candidates
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();

            Assert.That(second, Does.Contain(new HexCoordinate(-1, 0)),
                "The repeated instance derives its own candidates from the board as it now stands, " +
                "so it can name a subject the first instance did not.");
            MoveSubject(session, new HexCoordinate(-1, 0));
            Assert.That(StartedInstances(session), Is.EqualTo(2));
            AssertCardResolved(session);
        }

        [Test]
        public void ARepeatedInstanceWithNothingLegalLeft_PausesWithEveryOptionRefusedAndStops()
        {
            var session = StartRepeatSkill();
            ChooseOption(session, PushBranch);
            Confirm(session);
            AnswerCorrection(session);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(
                choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId.Value),
                Is.EquivalentTo(new[] { MoveBranch.Value, PushBranch.Value }),
                "The push put both Red minions out of reach and the one object still adjacent is the " +
                "Blue Heavy, whose standing immunity the repeated instance consults for itself. Every " +
                "mode the first instance could take is refused for the second.");
            Assert.That(choice.StopInvocation, Is.Not.Null,
                "6.5ae's stop is the one legal command, and the player takes it.");

            Stop(session);

            Assert.That(session.Projection.PendingSkillResolution, Is.Null);
            Assert.That(session.Projection.PendingDisplacedMinionCorrection, Is.Null);
            AssertCardResolved(session);
        }

        [Test]
        public void TheSourceCard_ResolvesOnlyWhenTheLastInstanceCompletes()
        {
            var session = StartRepeatSkill();
            ChooseOption(session, PushBranch);
            Confirm(session);
            AssertCardUnresolved(session);

            AnswerCorrection(session);
            AssertCardUnresolved(session);

            Stop(session);
            AssertCardResolved(session);
        }

        [Test]
        public void APausedRepeatDecision_RoundTripsWhenItsPushNodeDeclaresOnlyTheOutputsItUses()
        {
            var session = StartRepeatSkill();
            ChooseOption(session, PushBranch);
            Assert.That(session.Projection.PendingEffectChoice?.Kind,
                Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));

            var payload = session.CaptureSnapshot();
            var restored = StartRepeatSkill();
            var result = restored.RestoreSnapshot(payload);

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload),
                "The real repeat decision must restore byte-identically while its effect frame stands.");
            Assert.That(
                restored.Projection.PendingSkillResolution.CompletedOutcomes
                    .Single(outcome => outcome.NodeId.Equals(PushBranch))
                    .Outputs.Select(output => output.OutputName),
                Is.EqualTo(new[] { "distance", "moved" }),
                "A capability result persists only the outputs this definition node declared.");
            Assert.That(restored.Projection.PendingEffectChoice.ConfirmInvocation, Is.Not.Null);
            Assert.That(restored.Projection.PendingEffectChoice.DeclineInvocation, Is.Not.Null);
        }

        [Test]
        public void ACorrectionHoldingARepeat_SurvivesSnapshotRestore()
        {
            var session = StartRepeatSkill();
            ChooseOption(session, PushBranch);
            Confirm(session);

            var byteIdentical = Deserialize(session);
            Assert.That(
                AuthoritativeSnapshotSerializer.Serialize(byteIdentical.Round, byteIdentical.Board),
                Is.EqualTo(session.Projection.AuthoritativeSnapshot),
                "A correction holding an owed repeat round-trips byte-identically at schema v12.");

            var payload = session.CaptureSnapshot();
            var restored = StartRepeatSkill();
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);
            Assert.That(restored.Projection.PendingDisplacedMinionCorrection, Is.Not.Null);
            Assert.That(StartedInstances(restored), Is.EqualTo(1));

            AnswerCorrection(restored);

            Assert.That(StartedInstances(restored), Is.EqualTo(2),
                "The restored boundary still owes the repeat, so answering the correction starts the " +
                "second instance exactly as the live one did.");
            Assert.That(
                restored.Projection.PendingSkillResolution.InvocationContext,
                Is.EqualTo(ActionInvocationContext.Repeated));
        }

        [Test]
        public void ADeclinedRepeatUnderTheSameCorrection_RestoresWithNothingOwed()
        {
            var session = StartRepeatSkill();
            ChooseOption(session, PushBranch);
            Decline(session);

            var payload = session.CaptureSnapshot();
            var restored = StartRepeatSkill();
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);
            AnswerCorrection(restored);

            Assert.That(StartedInstances(restored), Is.EqualTo(1),
                "The wire distinguishes a correction that owes a repeat from one that does not, " +
                "which is the whole reason v11 is rejected rather than read as `did not`.");
            AssertCardResolved(restored);
        }

        [Test]
        public void AnOwedRepeat_ReplaysAndUndoesDeterministically()
        {
            var session = StartRepeatSkill();
            ChooseOption(session, PushBranch);
            Confirm(session);
            var beforeAnswer = session.Projection.DiagnosticSnapshot;

            AnswerCorrection(session);
            var afterAnswer = session.Projection.DiagnosticSnapshot;
            Assert.That(StartedInstances(session), Is.EqualTo(2));

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeAnswer),
                "Retained-prefix Undo reproduces the paused correction with its repeat still owed.");
            Assert.That(StartedInstances(session), Is.EqualTo(1),
                "The second instance is derived work, so undoing the command that started it undoes it.");

            AnswerCorrection(session);

            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterAnswer),
                "Replaying the same answer recreates the same second instance and the same facts.");
        }

        [Test]
        public void NoOrdinaryStart_CanCreateARepeatedInstance()
        {
            var configuration = MatchConfiguration.CreateForRepeatFixture(TeamSide.Blue);
            var volley = configuration.CardInstances.Single(card => card.Id.Value.Contains("red-volley"));
            var forged = new ActionOffer(
                volley.Id,
                volley.DefinitionReference,
                RepeatSkill,
                volley.OwnerId,
                ActionInvocationContext.Repeated,
                true);

            var rejected = CreateResolutionAuthority(volley.OwnerId)
                .StartTurnSkill(
                    new ActionResolutionId("forged-repeat"),
                    forged,
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    configuration.Participants,
                    new GuardsDigital.Core.Timing.ActiveEffectLifecycle(),
                    GuardsDigital.Core.Rules.Modifications.RuleModificationSet.Empty,
                    TieBreakerState.Unrevealed,
                    1,
                    1);

            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RejectionReason,
                Is.EqualTo(CardActionResolutionRejectionReason.UnsupportedInvocationContext),
                "The dispatcher's start gate admits the two printed contexts only, so a repeated " +
                "instance is reachable through the repeat path alone - which is also why it can never " +
                "be one of the offers Fast Travel or Clear replace.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static int StartedInstances(MatchSession session) =>
            session.Projection.DiagnosticEvents.Count(entry =>
                entry.EventType == "Action.SkillActionStarted");

        private static void AssertCardResolved(MatchSession session) =>
            Assert.That(
                VolleyCard(session).State.ResolutionStatus,
                Is.Not.EqualTo(CardResolutionStatus.Unresolved),
                "The source card resolves only when the last instance, correction included, completes.");

        private static void AssertCardUnresolved(MatchSession session) =>
            Assert.That(
                VolleyCard(session).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Unresolved),
                "One card is spent on the whole action however many instances it runs.");

        private static CardInstance VolleyCard(MatchSession session) =>
            session.Projection.Cards.Single(card => card.Id.Value.Contains("red-volley"));

        private static HexCoordinate Coordinate(MatchSession session, string minionId) =>
            session.Projection.Minions.Single(minion => minion.MinionId.Value == minionId).Coordinate;

        private static void ChooseOption(MatchSession session, EffectNodeId option)
        {
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.NodeId.Value, Is.EqualTo("repeat-choice"));
            // Milestone 8.46b: the completion is asked for rather than assumed, and aimed at the choice
            // this answer went into - one admitting exactly one selection is closed by the select itself.
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(session, 
                choice.SelectInvocations.Single(candidate =>
                    candidate.Candidate.OptionNodeId.Equals(option)));
        }

        /// <summary>Names the adjacent subject, then takes the first destination it is offered.</summary>
        private static void MoveSubject(MatchSession session, HexCoordinate subject)
        {
            SelectValue(session, subject);
            SelectValue(session, session.Projection.PendingEffectChoice.Candidates[0].Value.Coordinate);
        }

        private static void SelectValue(MatchSession session, HexCoordinate coordinate)
        {
            // Milestone 8.46b: the completion is asked for rather than assumed, and aimed at the
            // choice this answer went into. That aim is what this fixture needs most: naming the one
            // space a step admits now closes it inside that command and opens the printed repeat's
            // own confirm-or-decline behind it, which the cases below answer on purpose.
            var choice = session.Projection.PendingEffectChoice;
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(
                session,
                choice.SelectInvocations.Single(candidate =>
                    candidate.Candidate.Value.Coordinate.Equals(coordinate)));
        }

        private static void Confirm(MatchSession session)
        {
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.NodeId, Is.EqualTo(RepeatNode));
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.ConfirmInvocation));
        }

        private static void Decline(MatchSession session)
        {
            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.NodeId, Is.EqualTo(RepeatNode));
            Accept(session, DirectorCommand.AnswerEffectChoice(choice.DeclineInvocation));
        }

        private static void Stop(MatchSession session) =>
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.StopInvocation));

        private static void AnswerCorrection(MatchSession session)
        {
            var pending = session.Projection.PendingDisplacedMinionCorrection;
            Assert.That(pending, Is.Not.Null);
            Accept(session, DirectorCommand.ChooseCorrectionDestination(pending.SelectInvocations[0]));
        }

        /// <summary>
        /// Milestone 8.25e4a's cross-family contract fixture enters the real repeat decision without
        /// copying this synthetic card's setup or its effect-node route.
        /// </summary>
        internal static MatchSession CreateRepeatDecisionPause()
        {
            var session = StartRepeatSkill();
            ChooseOption(session, PushBranch);
            Assert.That(session.Projection.PendingEffectChoice?.Kind,
                Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            return session;
        }

        /// <summary>
        /// The same configured Skill one instance later, where every mode is genuinely unavailable
        /// and the projected Stop is the only answer.
        /// </summary>
        internal static MatchSession CreateStopDecisionPause()
        {
            var session = StartRepeatSkill();
            ChooseOption(session, PushBranch);
            Confirm(session);
            AnswerCorrection(session);
            Assert.That(session.Projection.PendingEffectChoice?.StopInvocation, Is.Not.Null);
            return session;
        }

        private static RestoredAuthoritativeMatch Deserialize(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

        private static CardActionResolutionAuthority CreateResolutionAuthority(HeroId actorId)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            return new CardActionResolutionAuthority(
                MatchContentCatalogs.ForThisBuild(),
                new GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority(
                    map,
                    new[] { new HeroOccupancy(actorId, ActorAt) },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<GuardsDigital.Core.Units.Minions.MinionOccupancy>()));
        }

        private static MatchSession StartRepeatSkill()
        {
            var session = CreateRepeatSession();
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.Id.Value.Contains("volley") &&
                    candidate.State.Location == CardLocation.Hand);
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }

            var offer = session.Projection.AvailableTurnActions
                .SingleOrDefault(candidate => candidate.ActionDefinitionId.Equals(RepeatSkill));
            Assert.That(offer.ActionDefinitionId, Is.EqualTo(RepeatSkill),
                "The Repeat Skill must be the current actor's offer.");
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            return session;
        }

        private static MatchSession CreateRepeatSession()
        {
            var configuration = MatchConfiguration.CreateForRepeatFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
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
                    new HeroOccupancy(redHeroId, ActorAt),
                    new HeroOccupancy(blueHeroId, new HexCoordinate(-1, -5))
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
