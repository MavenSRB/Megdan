using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class AttackActionResolutionAuthorityTests
    {
        [Test]
        public void AdjacentEnemyTarget_OpensDefenderOwnedChoiceWithPrintedAttackTotalAndOrderedEvents()
        {
            var fixture = CreateFixture();
            var originalStates = fixture.Configuration.CardInstances
                .ToDictionary(instance => instance.Id, instance => instance.State);

            var start = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("attack-resolution"),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            var answer = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    fixture.DefenderId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(start.Accepted, Is.True);
            Assert.That(start.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.AttackTargetChoiceRequested
            }));
            Assert.That(start.PendingTargetChoice.ChooserId, Is.EqualTo(fixture.AttackerId));
            Assert.That(start.PendingTargetChoice.LegalTargetHeroIds, Is.EqualTo(new[] { fixture.DefenderId }));
            Assert.That(answer.Accepted, Is.True);
            Assert.That(answer.PendingResolution.Phase, Is.EqualTo(AttackResolutionPhase.DefenseResponse));
            Assert.That(answer.PendingResolution.TargetHeroId, Is.EqualTo(fixture.DefenderId));
            Assert.That(answer.PendingResolution.AttackValue.Channel, Is.EqualTo(PrintedValueChannel.Attack));
            Assert.That(answer.PendingResolution.AttackValue.Contributions, Has.Count.EqualTo(1));
            Assert.That(
                answer.PendingResolution.AttackValue.Contributions.Single().Kind,
                Is.EqualTo(PrintedValueContributionKind.PrintedIcon));
            Assert.That(answer.PendingResolution.AttackTotal, Is.EqualTo(3));
            Assert.That(answer.PendingDefenseChoice.ChooserId, Is.EqualTo(fixture.DefenderId));
            Assert.That(answer.PendingDefenseChoice.AttackerId, Is.EqualTo(fixture.AttackerId));
            Assert.That(answer.PendingDefenseChoice.AttackTotal, Is.EqualTo(3));
            Assert.That(answer.PendingDefenseChoice.CanDecline, Is.True);
            Assert.That(answer.PendingDefenseChoice.LegalDefenseOffers, Has.Count.EqualTo(2));
            Assert.That(
                answer.PendingDefenseChoice.LegalDefenseOffers.All(offer =>
                    offer.ActorId.Equals(fixture.DefenderId) &&
                    offer.InvocationContext == ActionInvocationContext.Reaction),
                Is.True);
            Assert.That(answer.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.AttackStarted,
                CardActionResolutionEventKind.AttackTotalCalculated,
                CardActionResolutionEventKind.DefenseResponseChoiceRequested
            }));
            Assert.That(answer.Events.All(ruleEvent => ruleEvent.TargetHeroId.Equals(fixture.DefenderId)), Is.True);
            Assert.That(answer.Events.All(ruleEvent => ruleEvent.AttackTotal == 3), Is.True);
            Assert.That(fixture.Authority.PendingAttackTargetChoice, Is.Null);
            Assert.That(fixture.Authority.PendingDefenseChoice, Is.SameAs(answer.PendingDefenseChoice));
            Assert.That(
                fixture.Configuration.CardInstances.All(instance => instance.State.Equals(originalStates[instance.Id])),
                Is.True,
                "Initiating an Attack and opening Defense must not consume either hero's card.");
        }

        [Test]
        public void TargetProjection_IncludesOnlyAdjacentEnemyHeroesAndNeverOccupancyObstacles()
        {
            var attackerId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var adjacentEnemyId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var friendlyId = new HeroId("red-friendly-hero");
            var nonAdjacentEnemyId = new HeroId("blue-far-hero");
            var participants = CreateFourHeroParticipants(attackerId, adjacentEnemyId, friendlyId, nonAdjacentEnemyId);
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            var authority = new CardActionResolutionAuthority(new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(adjacentEnemyId, new HexCoordinate(1, 0)),
                    new HeroOccupancy(friendlyId, new HexCoordinate(0, 1)),
                    new HeroOccupancy(nonAdjacentEnemyId, new HexCoordinate(2, 0))
                },
                new[] { new ObstacleOccupancy(new ObstacleId("terrain-token-obstacle"), new HexCoordinate(-1, 0)) }));
            var attackOffer = GetAttackOffer(configuration, attackerId);

            var start = authority.StartAttackResolution(
                new ActionResolutionId("attack-resolution"),
                attackOffer,
                configuration.CardDefinitions,
                configuration.CardInstances,
                participants);

            Assert.That(start.Accepted, Is.True);
            Assert.That(start.PendingTargetChoice.LegalTargetHeroIds, Is.EqualTo(new[] { adjacentEnemyId }));

            var invalidTargets = new[]
            {
                attackerId,
                friendlyId,
                nonAdjacentEnemyId,
                new HeroId("empty-space"),
                new HeroId("terrain"),
                new HeroId("token"),
                new HeroId("terrain-token-obstacle")
            };
            foreach (var invalidTarget in invalidTargets)
            {
                var rejection = authority.AnswerAttackTarget(
                    new AttackTargetChoiceCommand(
                        start.PendingTargetChoice.ResolutionId,
                        start.PendingTargetChoice.ChoiceId,
                        invalidTarget),
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    participants);

                Assert.That(rejection.Accepted, Is.False, $"Target '{invalidTarget}' should be rejected.");
                Assert.That(rejection.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.TargetUnavailable));
                Assert.That(rejection.Events, Is.Empty);
                Assert.That(rejection.PendingResolution, Is.SameAs(start.PendingResolution));
                Assert.That(rejection.PendingTargetChoice, Is.SameAs(start.PendingTargetChoice));
            }
        }

        [Test]
        public void StartAttack_WhenNoAdjacentEnemyExists_RejectsWithoutPendingStateOrCardMutation()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            var attackerId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var defenderId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var authority = new CardActionResolutionAuthority(new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(defenderId, new HexCoordinate(2, 0))
                }));
            var originalStates = configuration.CardInstances.Select(instance => instance.State).ToArray();

            var result = authority.StartAttackResolution(
                new ActionResolutionId("attack-resolution"),
                GetAttackOffer(configuration, attackerId),
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants);

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.TargetUnavailable));
            Assert.That(result.Events, Is.Empty);
            Assert.That(authority.PendingAttackResolution, Is.Null);
            Assert.That(authority.PendingAttackTargetChoice, Is.Null);
            Assert.That(
                configuration.CardInstances.Select(instance => instance.State),
                Is.EqualTo(originalStates));
        }

        [Test]
        public void TargetChoice_WhenBoardChanges_RejectsWithoutChangingPendingAttackOrCards()
        {
            var fixture = CreateFixture();
            var start = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("attack-resolution"),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            var originalStates = fixture.Configuration.CardInstances.Select(instance => instance.State).ToArray();
            var moved = fixture.BoardAuthority.TryCommit(new AdjacentMoveCommand(
                fixture.DefenderId,
                new HexCoordinate(1, 0),
                new HexCoordinate(2, 0)));

            var result = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    fixture.DefenderId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(moved.Accepted, Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.InvalidChoice));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.PendingResolution, Is.SameAs(start.PendingResolution));
            Assert.That(result.PendingTargetChoice, Is.SameAs(start.PendingTargetChoice));
            Assert.That(
                fixture.Configuration.CardInstances.Select(instance => instance.State),
                Is.EqualTo(originalStates));
        }

        [Test]
        public void PendingDefenseResponse_RestoresOnlyWhenChoiceAndAttackTotalMatchAuthority()
        {
            var fixture = CreateFixture();
            var start = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("attack-resolution"),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            var answer = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    fixture.DefenderId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            var restoredBoard = CreateBoardAuthority(fixture.AttackerId, fixture.DefenderId);

            var restored = CardActionResolutionAuthority.RestoreAttack(
                restoredBoard,
                answer.PendingResolution,
                null,
                answer.PendingDefenseChoice,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(restored.PendingAttackResolution, Is.SameAs(answer.PendingResolution));
            Assert.That(restored.PendingDefenseChoice, Is.SameAs(answer.PendingDefenseChoice));

            var tamperedChoice = new PendingDefenseResponseChoice(
                answer.PendingDefenseChoice.ResolutionId,
                answer.PendingDefenseChoice.ChoiceId,
                answer.PendingDefenseChoice.ChooserId,
                answer.PendingDefenseChoice.AttackerId,
                answer.PendingDefenseChoice.AttackTotal + 1,
                answer.PendingDefenseChoice.LegalDefenseOffers,
                answer.PendingDefenseChoice.CanDecline);
            Assert.Throws<ArgumentException>(() => CardActionResolutionAuthority.RestoreAttack(
                CreateBoardAuthority(fixture.AttackerId, fixture.DefenderId),
                answer.PendingResolution,
                null,
                tamperedChoice,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants));
        }

        [Test]
        public void SameAttackCommands_ProduceSamePendingStateAndOrderedEvents()
        {
            var first = RunAttackReplay();
            var second = RunAttackReplay();

            Assert.That(first.Events, Is.EqualTo(second.Events));
            Assert.That(first.TargetHeroId, Is.EqualTo(second.TargetHeroId));
            Assert.That(first.AttackTotal, Is.EqualTo(second.AttackTotal));
            Assert.That(first.DefenseChooserId, Is.EqualTo(second.DefenseChooserId));
        }

        [Test]
        public void StartAttack_RejectsForgedOfferAndCannotOverlapMovementResolution()
        {
            var fixture = CreateFixture();
            var forgedOffer = new ActionOffer(
                new CardInstanceId("forged-card"),
                fixture.AttackOffer.SourceDefinitionReference,
                fixture.AttackOffer.ActionDefinitionId,
                fixture.AttackerId,
                fixture.AttackOffer.InvocationContext,
                true,
                AttackRangeKind.NonRanged,
                default);
            var forged = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("forged-attack"),
                forgedOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(forged.Accepted, Is.False);
            Assert.That(forged.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.UnavailableOffer));
            Assert.That(fixture.Authority.PendingAttackResolution, Is.Null);

            var movementOffer = ActionOfferQuery.GetAvailableOffers(
                    fixture.Configuration.CardDefinitions,
                    fixture.Configuration.CardInstances,
                    fixture.AttackerId,
                    ActionOfferWindow.NormalPlay)
                .First(offer => offer.ActionDefinitionId.Equals(new ActionDefinitionId("primary-movement")));
            var movement = fixture.Authority.StartMovementResolution(
                new ActionResolutionId("movement-resolution"),
                movementOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances);
            var overlap = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("overlap-attack"),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);

            Assert.That(movement.Accepted, Is.True);
            Assert.That(overlap.Accepted, Is.False);
            Assert.That(overlap.RejectionReason, Is.EqualTo(CardActionResolutionRejectionReason.ResolutionAlreadyPending));
            Assert.That(fixture.Authority.PendingResolution, Is.SameAs(movement.PendingResolution));
        }

        private static ReplayResult RunAttackReplay()
        {
            var fixture = CreateFixture();
            var events = new List<CardActionResolutionEvent>();
            var start = fixture.Authority.StartAttackResolution(
                new ActionResolutionId("attack-resolution"),
                fixture.AttackOffer,
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            events.AddRange(start.Events);
            var answer = fixture.Authority.AnswerAttackTarget(
                new AttackTargetChoiceCommand(
                    start.PendingTargetChoice.ResolutionId,
                    start.PendingTargetChoice.ChoiceId,
                    fixture.DefenderId),
                fixture.Configuration.CardDefinitions,
                fixture.Configuration.CardInstances,
                fixture.Configuration.Participants);
            events.AddRange(answer.Events);

            return new ReplayResult(
                answer.PendingResolution.TargetHeroId,
                answer.PendingResolution.AttackTotal,
                answer.PendingDefenseChoice.ChooserId,
                events);
        }

        private static AttackFixture CreateFixture()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            var attackerId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var defenderId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var boardAuthority = CreateBoardAuthority(attackerId, defenderId);
            return new AttackFixture(
                configuration,
                attackerId,
                defenderId,
                boardAuthority,
                new CardActionResolutionAuthority(boardAuthority),
                GetAttackOffer(configuration, attackerId));
        }

        private static AdjacentMoveAuthority CreateBoardAuthority(HeroId attackerId, HeroId defenderId)
        {
            return new AdjacentMoveAuthority(
                CreateRadiusThreeCoordinates(),
                new[]
                {
                    new HeroOccupancy(attackerId, new HexCoordinate(0, 0)),
                    new HeroOccupancy(defenderId, new HexCoordinate(1, 0))
                });
        }

        private static ActionOffer GetAttackOffer(
            PlaceholderOneVsOneTestConfiguration configuration,
            HeroId attackerId)
        {
            return ActionOfferQuery.GetAvailableOffers(
                    configuration.CardDefinitions,
                    configuration.CardInstances,
                    attackerId,
                    ActionOfferWindow.NormalPlay)
                .Single(offer => offer.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));
        }

        private static MatchParticipants CreateFourHeroParticipants(
            HeroId attackerId,
            HeroId adjacentEnemyId,
            HeroId friendlyId,
            HeroId nonAdjacentEnemyId)
        {
            var redTeamId = new TeamId(PlaceholderOneVsOneParticipants.RedTeamIdValue);
            var blueTeamId = new TeamId(PlaceholderOneVsOneParticipants.BlueTeamIdValue);
            var redPlayerId = new PlayerId(PlaceholderOneVsOneParticipants.RedPlayerIdValue);
            var bluePlayerId = new PlayerId(PlaceholderOneVsOneParticipants.BluePlayerIdValue);
            var friendlyPlayerId = new PlayerId("red-friendly-player");
            var farPlayerId = new PlayerId("blue-far-player");
            return new MatchParticipants(
                new[]
                {
                    new Team(redTeamId, TeamSide.Red),
                    new Team(blueTeamId, TeamSide.Blue)
                },
                new[]
                {
                    new Player(redPlayerId, redTeamId),
                    new Player(bluePlayerId, blueTeamId),
                    new Player(friendlyPlayerId, redTeamId),
                    new Player(farPlayerId, blueTeamId)
                },
                new[]
                {
                    new Hero(attackerId, redPlayerId),
                    new Hero(adjacentEnemyId, bluePlayerId),
                    new Hero(friendlyId, friendlyPlayerId),
                    new Hero(nonAdjacentEnemyId, farPlayerId)
                });
        }

        private static IEnumerable<HexCoordinate> CreateRadiusThreeCoordinates()
        {
            const int radius = 3;
            for (var q = -radius; q <= radius; q++)
            {
                var minimumR = Math.Max(-radius, -q - radius);
                var maximumR = Math.Min(radius, -q + radius);
                for (var r = minimumR; r <= maximumR; r++)
                {
                    yield return new HexCoordinate(q, r);
                }
            }
        }

        private sealed class AttackFixture
        {
            public AttackFixture(
                PlaceholderOneVsOneTestConfiguration configuration,
                HeroId attackerId,
                HeroId defenderId,
                AdjacentMoveAuthority boardAuthority,
                CardActionResolutionAuthority authority,
                ActionOffer attackOffer)
            {
                Configuration = configuration;
                AttackerId = attackerId;
                DefenderId = defenderId;
                BoardAuthority = boardAuthority;
                Authority = authority;
                AttackOffer = attackOffer;
            }

            public PlaceholderOneVsOneTestConfiguration Configuration { get; }
            public HeroId AttackerId { get; }
            public HeroId DefenderId { get; }
            public AdjacentMoveAuthority BoardAuthority { get; }
            public CardActionResolutionAuthority Authority { get; }
            public ActionOffer AttackOffer { get; }
        }

        private readonly struct ReplayResult
        {
            public ReplayResult(
                HeroId targetHeroId,
                int attackTotal,
                HeroId defenseChooserId,
                IEnumerable<CardActionResolutionEvent> events)
            {
                TargetHeroId = targetHeroId;
                AttackTotal = attackTotal;
                DefenseChooserId = defenseChooserId;
                Events = Array.AsReadOnly(events.ToArray());
            }

            public HeroId TargetHeroId { get; }
            public int AttackTotal { get; }
            public HeroId DefenseChooserId { get; }
            public IReadOnlyList<CardActionResolutionEvent> Events { get; }
        }
    }
}
