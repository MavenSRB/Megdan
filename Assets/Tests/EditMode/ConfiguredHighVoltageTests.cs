using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>Milestone 8.13 source-linked scenarios for Wasp's printed Ultimate.</summary>
    public sealed class ConfiguredHighVoltageTests
    {
        [Test]
        public void BasicSkill_WithUnlockedHighVoltage_DefeatsTheChosenMinionThenDiscardsFromAWasAdjacentHero()
        {
            var fixture = CreateFixture(StandardMinionType.Melee, lastEnemyMinion: false,
                adjacentEnemyHero: true, controllerOnBoard: true);

            StartBasicSkill(fixture);
            Assert.That(fixture.Turn.PendingEffectChoice, Is.Not.Null,
                "The unlocked on-board Ultimate fires after Static Barrier's basic Skill.");
            Answer(fixture.Turn, fixture.Turn.PendingEffectChoice.ConfirmInvocation);

            var minionChoice = fixture.Turn.PendingEffectChoice;
            Assert.That(
                minionChoice.SelectInvocations.Select(value => value.Candidate.Value.MinionId),
                Does.Contain(fixture.TargetId));
            Answer(fixture.Turn, minionChoice.SelectInvocations.Single(value =>
                value.Candidate.Value.MinionId.Equals(fixture.TargetId)));
            Answer(fixture.Turn, fixture.Turn.PendingEffectChoice.ConfirmInvocation);

            var heroChoice = fixture.Turn.PendingEffectChoice;
            Assert.That(heroChoice.ChooserId, Is.EqualTo(fixture.RedHeroId),
                "p.10 gives the actor the card's unassigned choice.");
            Assert.That(
                heroChoice.SelectInvocations.Select(value => value.Candidate.Value.HeroId),
                Is.EqualTo(new[] { fixture.BlueHeroId }));
            Assert.That(fixture.Board.Minions.Any(value => value.MinionId.Equals(fixture.TargetId)),
                Is.True,
                "'who was adjacent' is read while the staged target is still on the board.");

            Answer(fixture.Turn, heroChoice.SelectInvocations.Single());
            Answer(fixture.Turn, fixture.Turn.PendingEffectChoice.ConfirmInvocation);
            var discardChoice = fixture.Turn.PendingEffectChoice;
            Assert.That(discardChoice.ChooserId, Is.EqualTo(fixture.BlueHeroId));
            var discarded = discardChoice.SelectInvocations.First().Candidate.Value.CardInstanceId;
            Answer(fixture.Turn, discardChoice.SelectInvocations.First());
            var completed = Answer(fixture.Turn, fixture.Turn.PendingEffectChoice.ConfirmInvocation);

            Assert.That(fixture.Board.Minions.Any(value => value.MinionId.Equals(fixture.TargetId)),
                Is.False);
            Assert.That(fixture.Instances.Single(value => value.Id.Equals(discarded)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(fixture.Dispatcher.CoinLedger.GetCoins(fixture.RedHeroId), Is.EqualTo(2),
                "p.17's minion Defeat keyword awards the ordinary melee reward.");
            Assert.That(
                completed.Events.Where(value =>
                    value.Kind == TurnResolutionEventKind.TriggeredEffectProgressed)
                    .Select(value => value.CardActionEvent.MinionDefeatFactKind),
                Is.EqualTo(new[]
                {
                    MinionDefeatFactKind.DefeatRecorded,
                    MinionDefeatFactKind.RewardAwarded,
                    MinionDefeatFactKind.RemovalAttempted,
                    MinionDefeatFactKind.RemovalCompleted
                }));
        }

        [Test]
        public void HighVoltage_FiltersAProtectedHeavyAndDoesNotFireForAnOffBoardController()
        {
            var heavy = CreateFixture(StandardMinionType.Heavy, lastEnemyMinion: false,
                adjacentEnemyHero: true, controllerOnBoard: true);
            StartBasicSkill(heavy);

            // Milestone 8.29e. Until then this fixture confirmed the Ultimate's `may` and only
            // afterwards discovered that the protected Heavy is filtered out - a question whose two
            // answers left the same board, asked at the one moment every other control is withheld.
            // The filtering is unchanged and is still what this case is about; what changed is that
            // the offer now reads its own branch before opening, so the step is simply not taken.
            Assert.That(heavy.Turn.PendingEffectChoice, Is.Null,
                "The optional step has no legal target because the protected Heavy is filtered, so " +
                "p.10's confirm-or-decline is never opened.");
            Assert.That(heavy.Board.Minions.Any(value => value.MinionId.Equals(heavy.TargetId)), Is.True);

            var offBoard = CreateFixture(StandardMinionType.Melee, lastEnemyMinion: false,
                adjacentEnemyHero: true, controllerOnBoard: false);
            var sourceCard = offBoard.Instances.Single(card => card.OwnerId.Equals(offBoard.RedHeroId) &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.StaticBarrier));
            var basicSkill = BasicActionQuery.ForCompletedAction(
                offBoard.RedHeroId, sourceCard.Id, WaspCardContent.PrimarySkill, ActionKind.Skill,
                offBoard.Definitions, offBoard.Instances);
            Assert.That(
                UltimateTriggeredEffectSourceQuery.Derive(
                    UltimateUnlockQuery.Derive(offBoard.Definitions, offBoard.Instances),
                    offBoard.Board, offBoard.Participants, basicSkill),
                Is.Empty,
                "p.16 applies an Ultimate only while its controller is on the board.");
        }

        [Test]
        public void DefeatingTheLastEnemyMinion_RecordsTheOrdinaryMidTurnLanePushCause()
        {
            var fixture = CreateFixture(StandardMinionType.Melee, lastEnemyMinion: true,
                adjacentEnemyHero: false, controllerOnBoard: true);
            StartBasicSkill(fixture);
            Answer(fixture.Turn, fixture.Turn.PendingEffectChoice.ConfirmInvocation);
            Answer(fixture.Turn, fixture.Turn.PendingEffectChoice.SelectInvocations.Single());
            var completed = Answer(fixture.Turn, fixture.Turn.PendingEffectChoice.ConfirmInvocation);

            Assert.That(fixture.Board.Minions.Any(value => value.MinionId.Equals(fixture.TargetId)),
                Is.False);
            Assert.That(fixture.Turn.PendingImmediateLanePush, Is.Not.Null);
            Assert.That(
                fixture.Turn.PendingImmediateLanePush.Cause,
                Is.EqualTo(GuardsDigital.Core.Rules.BoardObjects.LanePushCauseKind.Defeat));
            Assert.That(
                completed.Events.Any(value =>
                    value.Kind == TurnResolutionEventKind.ImmediateLanePushRequested),
                Is.True);
        }

        [Test]
        public void WasAdjacentChoice_RestoresAndCompletesThroughTheExistingSkillContinuation()
        {
            var fixture = CreateSessionFixture();
            Commit(fixture.Session, fixture.RedHeroId, WaspCardContent.StaticBarrier);
            Commit(fixture.Session, fixture.BlueHeroId, WaspCardContent.StopProjectiles);
            Accept(fixture.Session, DirectorCommand.StartAction(WaspCardContent.PrimarySkill));
            Accept(fixture.Session, DirectorCommand.AnswerEffectChoice(
                fixture.Session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(
                fixture.Session.Projection.PendingEffectChoicePreview.All(value =>
                    value.ConsequenceIntent == EffectChoiceConsequenceIntent.Removal),
                Is.True,
                "The target is previewed with the ordinary removal consequence before confirmation.");
            var minion = fixture.Session.Projection.PendingEffectChoice.SelectInvocations.Single(value =>
                value.Candidate.Value.MinionId.Equals(fixture.TargetId));
            Accept(fixture.Session, DirectorCommand.AnswerEffectChoice(minion));
            Accept(fixture.Session, DirectorCommand.AnswerEffectChoice(
                fixture.Session.Projection.PendingEffectChoice.ConfirmInvocation));

            var snapshot = fixture.Session.CaptureSnapshot();
            var restoredFixture = CreateSessionFixture();
            var restored = restoredFixture.Session.RestoreSnapshot(snapshot);
            Assert.That(restored.Accepted, Is.True, restored.Detail);
            Assert.That(
                restoredFixture.Session.Projection.PendingEffectChoice.SelectInvocations
                    .Select(value => value.Candidate.Value.HeroId),
                Is.EqualTo(new[] { fixture.BlueHeroId }));
            Assert.That(restoredFixture.Session.CaptureSnapshot(), Is.EqualTo(snapshot));
            Accept(restoredFixture.Session, DirectorCommand.AnswerEffectChoice(
                restoredFixture.Session.Projection.PendingEffectChoice.SelectInvocations.Single()));
            Accept(restoredFixture.Session, DirectorCommand.AnswerEffectChoice(
                restoredFixture.Session.Projection.PendingEffectChoice.ConfirmInvocation));
            Accept(restoredFixture.Session, DirectorCommand.AnswerEffectChoice(
                restoredFixture.Session.Projection.PendingEffectChoice.SelectInvocations.First()));
            Accept(restoredFixture.Session, DirectorCommand.AnswerEffectChoice(
                restoredFixture.Session.Projection.PendingEffectChoice.ConfirmInvocation));
            Assert.That(
                restoredFixture.Session.Projection.Minions.Any(value =>
                    value.MinionId.Equals(fixture.TargetId)),
                Is.False);
        }

        private static void StartBasicSkill(Fixture fixture)
        {
            Assert.That(fixture.Turn.TryStart().Accepted, Is.True);
            var offer = fixture.Turn.GetTurnActionOffers().Single(value =>
                value.ActionDefinitionId.Equals(WaspCardContent.PrimarySkill));
            Assert.That(fixture.Turn.TryStartSkill(
                new ActionResolutionId("high-voltage-basic-skill"), offer).Accepted, Is.True);
        }

        private static TurnResolutionResult Answer(
            TurnResolutionAuthority turn,
            GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCommand command)
        {
            var result = turn.TryAnswerEffectChoice(command);
            Assert.That(result.Accepted, Is.True, result.RejectionReason.ToString());
            return result;
        }

        private static void Accept(MatchSession session, DirectorCommand command) =>
            Assert.That(session.Execute(command).Accepted, Is.True);

        private static void Commit(
            MatchSession session, HeroId ownerId, CardDefinitionId definitionId)
        {
            var card = session.Projection.Cards.Single(value => value.OwnerId.Equals(ownerId) &&
                value.State.Location == CardLocation.Hand &&
                value.DefinitionReference.DefinitionId.Equals(definitionId));
            Accept(session, DirectorCommand.CommitCard(ownerId, card.Id));
        }

        private static SessionFixture CreateSessionFixture()
        {
            var configuration = MatchConfiguration
                .CreateVerifiedMirrorForInternalTest(TeamSide.Red);
            var participants = configuration.Participants;
            var redHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var ultimate = configuration.CardInstances.Single(card =>
                card.OwnerId.Equals(redHero) &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.HighVoltage));
            Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);

            // The board itself is ConfiguredMatchFixtures'. Milestone 8.14 wants the same one
            // under an Ultimate that p.15's leveling unlocked, and two copies of it could
            // disagree about which minion this fixture is aimed at.
            var session = ConfiguredMatchFixtures.CreateVerifiedMirrorInRadiusOfAMinion(
                configuration, out var red, out var blue, out var target);
            return new SessionFixture(session, red, blue, target);
        }

        private static Fixture CreateFixture(
            StandardMinionType targetType,
            bool lastEnemyMinion,
            bool adjacentEnemyHero,
            bool controllerOnBoard)
        {
            var configuration = MatchConfiguration
                .CreateVerifiedMirrorForInternalTest(TeamSide.Red);
            var participants = configuration.Participants;
            var redHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHero = participants.Heroes.Single(hero =>
                participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var instances = configuration.CardInstances.ToArray();
            var definitions = configuration.CardDefinitions.ToArray();

            var redStatic = instances.Single(card => card.OwnerId.Equals(redHero) &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.StaticBarrier));
            var blueTurn = instances.Single(card => card.OwnerId.Equals(blueHero) &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.StopProjectiles));
            var ultimate = instances.Single(card => card.OwnerId.Equals(redHero) &&
                card.DefinitionReference.DefinitionId.Equals(WaspCardContent.HighVoltage));
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(redStatic), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(redStatic), Is.True);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(blueTurn), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(blueTurn), Is.True);
            Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);

            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                participants,
                map,
                configuration.TieBreakerState);
            var targetSetup = setup.Minions.First(minion =>
                minion.TeamSide == TeamSide.Blue && minion.MinionType == targetType &&
                GuardsDigital.Core.Rules.Modifications.BattleZoneQuery.IsInStartingBattleZone(
                    map, minion.Coordinate));
            var target = MinionOccupancy.FromSetup(targetSetup);
            var minions = setup.Minions.Select(MinionOccupancy.FromSetup)
                .Where(minion => !lastEnemyMinion || minion.TeamSide != TeamSide.Blue ||
                    minion.MinionId.Equals(target.MinionId))
                .ToArray();
            var occupiedByMinions = new HashSet<HexCoordinate>(minions.Select(value => value.Coordinate));
            var playable = map.Spaces.Where(space => space.Kind == BoardMapSpaceKind.Playable).ToArray();
            var redAt = playable.Select(space => space.Coordinate).First(coordinate =>
                !occupiedByMinions.Contains(coordinate) &&
                HexDistanceQuery.Calculate(coordinate, target.Coordinate) <= 3 &&
                HexDistanceQuery.Calculate(coordinate, target.Coordinate) > 0);
            var blueAt = target.Coordinate.GetNeighbors().First(coordinate =>
                !occupiedByMinions.Contains(coordinate) && !coordinate.Equals(redAt) &&
                playable.Any(space => space.Coordinate.Equals(coordinate)));

            var heroes = new List<HeroOccupancy>();
            if (controllerOnBoard) heroes.Add(new HeroOccupancy(redHero, redAt));
            if (adjacentEnemyHero) heroes.Add(new HeroOccupancy(blueHero, blueAt));
            var board = new AdjacentMoveAuthority(
                map, heroes, Array.Empty<ObstacleOccupancy>(),
                (IEnumerable<MinionOccupancy>)minions);
            var dispatcher = new CardActionResolutionAuthority(MatchContentCatalogs.ForThisBuild(), board);
            var turn = new TurnResolutionAuthority(
                definitions,
                instances,
                participants,
                configuration.TieBreakerState,
                dispatcher,
                new ActiveEffectLifecycle(),
                1,
                1);
            return new Fixture(
                turn, dispatcher, board, participants, definitions, instances,
                redHero, blueHero, target.MinionId);
        }

        private sealed class Fixture
        {
            public Fixture(
                TurnResolutionAuthority turn,
                CardActionResolutionAuthority dispatcher,
                AdjacentMoveAuthority board,
                MatchParticipants participants,
                IReadOnlyList<CardDefinition> definitions,
                IReadOnlyList<CardInstance> instances,
                HeroId redHeroId,
                HeroId blueHeroId,
                MinionId targetId)
            {
                Turn = turn;
                Dispatcher = dispatcher;
                Board = board;
                Participants = participants;
                Definitions = definitions;
                Instances = instances;
                RedHeroId = redHeroId;
                BlueHeroId = blueHeroId;
                TargetId = targetId;
            }

            public TurnResolutionAuthority Turn { get; }
            public CardActionResolutionAuthority Dispatcher { get; }
            public AdjacentMoveAuthority Board { get; }
            public MatchParticipants Participants { get; }
            public IReadOnlyList<CardDefinition> Definitions { get; }
            public IReadOnlyList<CardInstance> Instances { get; }
            public HeroId RedHeroId { get; }
            public HeroId BlueHeroId { get; }
            public MinionId TargetId { get; }
        }

        private sealed class SessionFixture
        {
            public SessionFixture(
                MatchSession session, HeroId redHeroId, HeroId blueHeroId, MinionId targetId)
            {
                Session = session;
                RedHeroId = redHeroId;
                BlueHeroId = blueHeroId;
                TargetId = targetId;
            }

            public MatchSession Session { get; }
            public HeroId RedHeroId { get; }
            public HeroId BlueHeroId { get; }
            public MinionId TargetId { get; }
        }
    }
}
