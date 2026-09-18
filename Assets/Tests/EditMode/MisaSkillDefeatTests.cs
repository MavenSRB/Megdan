using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class MisaSkillDefeatTests
    {
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void DefeatCompletesOnceAcrossSelectionRestoreUndoAndRemovalPrevention(bool guarded, bool pay)
        {
            var initial = Create(out var misa, out var brogan, out var target);
            var live = new MatchSession(initial);
            if (guarded)
            {
                StartSkill(live, brogan, BroganCardContent.Bolster);
                for (var i = 0; i < 8 && live.Projection.Phase != RoundResolutionPhase.AwaitingCommitments; i++)
                    YieldActor(live);
                Assert.That(live.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            }
            StartSkill(live, misa, MisaCardContent.WatchHowISoar);
            Choose(live, live.Projection.PendingEffectChoice.SelectInvocations.Last());
            var candidates = live.Projection.PendingEffectChoice.SelectInvocations;
            var enemyTeam = live.Projection.Participants.GetTeamForHero(brogan).Id;
            Assert.That(candidates.All(choice => live.Projection.Minions.Single(minion =>
                minion.MinionId.Equals(choice.Candidate.Value.MinionId)).TeamId.Equals(enemyTeam)), Is.True);
            var at = live.Projection.Heroes.Single(hero => hero.HeroId.Equals(misa)).Coordinate;
            Assert.That(candidates.All(choice => HexDistanceQuery.Calculate(at,
                live.Projection.Minions.Single(minion => minion.MinionId.Equals(choice.Candidate.Value.MinionId)).Coordinate) == 1), Is.True);
            var sample = candidates.First();
            var friendly = live.Projection.Minions.First(minion => !minion.TeamId.Equals(enemyTeam));
            var beforeInvalid = live.CaptureSnapshot();
            foreach (var invalid in new[]
            {
                new EffectChoiceCommand(sample.ResolutionId, sample.ChoiceId, EffectChoiceCommandKind.Select,
                    EffectChoiceCandidate.Value_(EffectValue.Minion(friendly.MinionId)), misa),
                new EffectChoiceCommand(sample.ResolutionId, sample.ChoiceId, EffectChoiceCommandKind.Select,
                    sample.Candidate, brogan)
            })
            {
                Assert.That(live.Execute(DirectorCommand.AnswerEffectChoice(invalid)).Accepted, Is.False);
                Assert.That(live.CaptureSnapshot(), Is.EqualTo(beforeInvalid));
            }
            // Milestone 8.46b. The capture moved one command earlier and the answer moved into one
            // command, because the state this used to snapshot no longer exists: the removal admits
            // exactly one subject, so naming it resolves the step rather than parking on a pause
            // standing on Deselect and Confirm. What the case is about is untouched - the standing
            // question restores and completes identically in both sessions, the answer can be taken
            // back and given again, and a stale one cannot apply twice.
            var selected = live.CaptureSnapshot();
            var restored = new MatchSession(initial);
            Assert.That(restored.RestoreSnapshot(selected).Accepted, Is.True);
            foreach (var session in new[] { live, restored })
            {
                Accept(session, DirectorCommand.AnswerEffectChoice(
                    session.Projection.PendingEffectChoice.SelectInvocations.Single(choice =>
                        choice.Candidate.Value.MinionId.Equals(target))));
                if (guarded)
                {
                    Assert.That(session.Projection.PendingRemovalPrevention, Is.Not.Null);
                    var paused = session.CaptureSnapshot();
                    Assert.That(session.RestoreSnapshot(paused).Accepted, Is.True, "A Skill Defeat must restore while removal is pending.");
                    var prompt = session.Projection.PendingRemovalPrevention;
                    Accept(session, DirectorCommand.AnswerRemovalPrevention(pay ? prompt.Payments.First().Invocation : prompt.DeclineInvocation));
                }
                Assert.That(session.Projection.Minions.Any(minion => minion.MinionId.Equals(target)), Is.EqualTo(pay));
                Assert.That(session.Projection.PendingEffectChoice, Is.Null);
                Assert.That(session.Projection.PendingRemovalPrevention, Is.Null);
                Assert.That(session.Projection.PendingTurnRelease, Is.Not.Null);
                Assert.That(session.Projection.HeroProgression.Single(hero => hero.HeroId.Equals(misa)).Coins, Is.EqualTo(2));
                Assert.That(session.Projection.RoundEvents.Select(value => value.TurnEvent.CardActionEvent)
                    .Where(value => value.Kind != CardActionResolutionEventKind.RemovalPreventionPaid)
                    .Select(value => value.MinionDefeatFactKind)
                    .Where(kind => kind != MinionDefeatFactKind.None), Is.EqualTo(new[]
                    {
                        MinionDefeatFactKind.DefeatRecorded, MinionDefeatFactKind.RewardAwarded,
                        MinionDefeatFactKind.RemovalAttempted,
                        pay ? MinionDefeatFactKind.RemovalPrevented : MinionDefeatFactKind.RemovalCompleted
                    }));
            }
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(live.CaptureSnapshot()));
            if (!guarded)
            {
                Assert.That(live.Undo().Accepted, Is.True);
                Assert.That(live.CaptureSnapshot(), Is.EqualTo(selected),
                    "One press back is the whole answer back, because giving it was one command.");
                var naming = DirectorCommand.AnswerEffectChoice(
                    live.Projection.PendingEffectChoice.SelectInvocations.Single(choice =>
                        choice.Candidate.Value.MinionId.Equals(target)));
                Accept(live, naming);
                var complete = live.CaptureSnapshot();
                Assert.That(live.Execute(naming).Accepted, Is.False, "A stale answer cannot defeat or pay twice.");
                Assert.That(live.CaptureSnapshot(), Is.EqualTo(complete));
            }
        }

        private static MatchSessionInitialState Create(out HeroId misa, out HeroId brogan, out MinionId target)
        {
            var configuration = MatchConfiguration.CreateStagedMisaForInternalTest(TeamSide.Red);
            misa = configuration.Participants.Heroes.Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            brogan = configuration.Participants.Heroes.Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(QuickGameSetupConfiguration.CreateStagedWaspVersusMisaOneVsOne(),
                configuration.Participants, map, configuration.TieBreakerState);
            var minion = setup.Minions.First(value => value.TeamSide == TeamSide.Red && value.MinionType == StandardMinionType.Melee);
            target = minion.MinionId;
            var empty = minion.Coordinate.GetNeighbors().Where(space => map.PlayableCoordinates.Contains(space) &&
                setup.Minions.All(value => !value.Coordinate.Equals(space))).Take(2).ToArray();
            Assert.That(empty, Has.Length.EqualTo(2));
            var red = BroganProgressedHands.CreateTierTwoHand(brogan, "audit-brogan", BroganUpgradeBranch.FirstSibling);
            var blue = MisaCardContent.CreateHand(misa, "audit-misa");
            var scenario = new InitialScenarioDescription(configuration.Participants,
                red.Definitions.Concat(blue.Definitions), red.AllInstances.Concat(blue.AllInstances), configuration.TieBreakerState,
                map.Reference, new[] { new HeroOccupancy(misa, empty[0]), new HeroOccupancy(brogan, empty[1]) }, Array.Empty<ObstacleOccupancy>());
            return new MatchSessionInitialState(scenario, setup, new[] { map });
        }

        private static void StartSkill(MatchSession session, HeroId actor, CardDefinitionId definition)
        {
            foreach (var hero in session.Projection.Participants.Heroes)
            {
                var hand = session.Projection.Cards.Where(card => card.OwnerId.Equals(hero.Id) && card.State.Location == CardLocation.Hand);
                var chosen = hero.Id.Equals(actor) ? hand.Single(card => card.DefinitionReference.DefinitionId.Equals(definition)) :
                    hand.OrderBy(card => session.Projection.Definitions.Single(value => value.Reference.Equals(card.DefinitionReference)).Initiative.Value).First();
                Accept(session, DirectorCommand.CommitCard(hero.Id, chosen.Id));
            }
            for (var i = 0; i < 8 && !session.Projection.SelectedActor.ActorId.Equals(actor); i++) YieldActor(session);
            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(actor));
            Accept(session, DirectorCommand.StartAction(new ActionDefinitionId("primary-skill")));
        }

        private static void YieldActor(MatchSession session) => Accept(session, session.Projection.PendingTurnRelease != null
            ? DirectorCommand.ReleaseTurn() : DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId));
        private static void Choose(MatchSession session, GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCommand command, bool confirm = true)
        {
            Accept(session, DirectorCommand.AnswerEffectChoice(command));
            // Milestone 8.46b: the completion is asked for rather than assumed, and never reaches
            // past the question it belongs to - a choice holding nothing is the next one.
            if (confirm) ConfiguredMatchFixtures.ConfirmStandingEffectChoiceIfOwed(session, command.ChoiceId);
        }
        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.RejectionReason + ": " + result.Detail);
        }
    }
}
