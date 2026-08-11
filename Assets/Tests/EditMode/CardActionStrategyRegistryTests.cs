using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class CardActionStrategyRegistryTests
    {
        [Test]
        public void StableStrategyIdentity_UsesOrdinalValueSemantics()
        {
            Assert.That(CardActionStrategyIds.Movement, Is.EqualTo(new CardActionStrategyId("movement-v1")));
            Assert.That(CardActionStrategyIds.AttackDefense, Is.Not.EqualTo(CardActionStrategyIds.Movement));
        }

        [Test]
        public void SyntheticStrategy_RegistersWithoutChangingRegistryControlFlow()
        {
            var synthetic = new SyntheticSkillStrategyHandler();
            var registry = new CardActionStrategyRegistry(new[] { synthetic });

            var byKind = registry.Dispatch<SyntheticSkillStrategyHandler, string>(
                ActionKind.Skill,
                handler => handler.Resolve("kind"));
            var byIdentity = registry.Dispatch<SyntheticSkillStrategyHandler, string>(
                synthetic.StrategyId,
                handler => handler.Resolve("identity"));

            Assert.That(byKind, Is.EqualTo("synthetic:kind"));
            Assert.That(byIdentity, Is.EqualTo("synthetic:identity"));
        }

        [Test]
        public void DuplicateOrUntypedIdentity_IsRejectedAtComposition()
        {
            var first = new SyntheticSkillStrategyHandler();
            var duplicate = new DuplicateSyntheticSkillStrategyHandler();

            Assert.Throws<ArgumentException>(() =>
                new CardActionStrategyRegistry(new ICardActionStrategyHandler[] { first, duplicate }));
        }

        [Test]
        public void ConcreteHandlers_AreSeparatelyTestableThroughTypedBackends()
        {
            var movementBackend = new RecordingMovementBackend();
            var attackBackend = new RecordingAttackBackend();
            var movement = new MovementCardActionStrategyHandler(movementBackend);
            var attack = new AttackDefenseCardActionStrategyHandler(attackBackend);

            movement.Answer(default);
            attack.ResolveOutcome(
                Array.Empty<CardDefinition>(),
                Array.Empty<CardInstance>(),
                null);

            Assert.That(movementBackend.AnswerWasCalled, Is.True);
            Assert.That(attackBackend.ResolveOutcomeWasCalled, Is.True);
        }

        private sealed class SyntheticSkillStrategyHandler : ICardActionStrategyHandler
        {
            public CardActionStrategyId StrategyId => new CardActionStrategyId("synthetic-skill-v1");

            public ActionKind ActionKind => ActionKind.Skill;

            public string Resolve(string value) => $"synthetic:{value}";
        }

        private sealed class DuplicateSyntheticSkillStrategyHandler : ICardActionStrategyHandler
        {
            public CardActionStrategyId StrategyId => new CardActionStrategyId("synthetic-skill-v2");

            public ActionKind ActionKind => ActionKind.Skill;
        }

        private sealed class RecordingMovementBackend : IMovementCardActionStrategyBackend
        {
            public bool AnswerWasCalled { get; private set; }

            public CardActionResolutionResult StartMovement(
                ActionResolutionId resolutionId,
                ActionOffer offer,
                System.Collections.Generic.IEnumerable<CardDefinition> definitions,
                System.Collections.Generic.IEnumerable<CardInstance> instances) => null;

            public CardActionResolutionResult StartTurnMovement(
                ActionResolutionId resolutionId,
                ActionOffer offer,
                System.Collections.Generic.IEnumerable<CardDefinition> definitions,
                System.Collections.Generic.IEnumerable<CardInstance> instances) => null;

            public CardActionResolutionResult AnswerMovement(MovementChoiceCommand command)
            {
                AnswerWasCalled = true;
                return null;
            }
        }

        private sealed class RecordingAttackBackend : IAttackDefenseCardActionStrategyBackend
        {
            public bool ResolveOutcomeWasCalled { get; private set; }

            public AttackActionResolutionResult StartAttack(
                ActionResolutionId resolutionId,
                ActionOffer offer,
                System.Collections.Generic.IEnumerable<CardDefinition> definitions,
                System.Collections.Generic.IEnumerable<CardInstance> instances,
                MatchParticipants participants) => null;

            public AttackActionResolutionResult StartTurnAttack(
                ActionResolutionId resolutionId,
                ActionOffer offer,
                System.Collections.Generic.IEnumerable<CardDefinition> definitions,
                System.Collections.Generic.IEnumerable<CardInstance> instances,
                MatchParticipants participants) => null;

            public AttackActionResolutionResult AnswerAttackTarget(
                AttackTargetChoiceCommand command,
                System.Collections.Generic.IEnumerable<CardDefinition> definitions,
                System.Collections.Generic.IEnumerable<CardInstance> instances,
                MatchParticipants participants) => null;

            public AttackActionResolutionResult AnswerDefense(
                DefenseResponseChoiceCommand command,
                System.Collections.Generic.IEnumerable<CardDefinition> definitions,
                System.Collections.Generic.IEnumerable<CardInstance> instances,
                MatchParticipants participants) => null;

            public AttackActionResolutionResult ResolveOutcome(
                System.Collections.Generic.IEnumerable<CardDefinition> definitions,
                System.Collections.Generic.IEnumerable<CardInstance> instances,
                MatchParticipants participants)
            {
                ResolveOutcomeWasCalled = true;
                return null;
            }
        }
    }
}
