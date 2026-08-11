using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public readonly struct CardActionStrategyId : IEquatable<CardActionStrategyId>
    {
        public CardActionStrategyId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A card-action strategy ID is required.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(CardActionStrategyId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is CardActionStrategyId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;
    }

    public static class CardActionStrategyIds
    {
        public static readonly CardActionStrategyId Movement = new CardActionStrategyId("movement-v1");

        public static readonly CardActionStrategyId AttackDefense =
            new CardActionStrategyId("attack-defense-v1");
    }

    public interface ICardActionStrategyHandler
    {
        CardActionStrategyId StrategyId { get; }

        ActionKind ActionKind { get; }
    }

    public sealed class CardActionStrategyRegistry
    {
        private readonly IReadOnlyDictionary<ActionKind, ICardActionStrategyHandler> handlers;
        private readonly IReadOnlyDictionary<CardActionStrategyId, ICardActionStrategyHandler> handlersById;

        public CardActionStrategyRegistry(IEnumerable<ICardActionStrategyHandler> handlers)
        {
            var materialized = handlers?.ToArray() ?? throw new ArgumentNullException(nameof(handlers));
            if (materialized.Length == 0 || materialized.Any(handler => handler == null))
            {
                throw new ArgumentException("At least one non-null card-action strategy handler is required.", nameof(handlers));
            }

            if (materialized.Any(handler =>
                    !handler.StrategyId.IsDefined || handler.ActionKind == ActionKind.None))
            {
                throw new ArgumentException("Every card-action strategy requires stable strategy and action-kind identities.", nameof(handlers));
            }

            if (materialized.GroupBy(handler => handler.StrategyId).Any(group => group.Count() != 1) ||
                materialized.GroupBy(handler => handler.ActionKind).Any(group => group.Count() != 1))
            {
                throw new ArgumentException("Card-action strategy and action-kind identities must be unique.", nameof(handlers));
            }

            this.handlers = materialized.ToDictionary(handler => handler.ActionKind);
            handlersById = materialized.ToDictionary(handler => handler.StrategyId);
        }

        public IReadOnlyCollection<ActionKind> SupportedActionKinds => handlers.Keys.ToArray();

        public TResult Dispatch<THandler, TResult>(
            ActionKind actionKind,
            Func<THandler, TResult> operation)
            where THandler : class, ICardActionStrategyHandler
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return operation(GetRequired<THandler>(actionKind));
        }

        public TResult Dispatch<THandler, TResult>(
            CardActionStrategyId strategyId,
            Func<THandler, TResult> operation)
            where THandler : class, ICardActionStrategyHandler
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation));
            }

            return operation(GetRequired<THandler>(strategyId));
        }

        private THandler GetRequired<THandler>(ActionKind actionKind)
            where THandler : class, ICardActionStrategyHandler
        {
            if (!handlers.TryGetValue(actionKind, out var handler))
            {
                throw new KeyNotFoundException($"No card-action strategy is registered for {actionKind}.");
            }

            if (!(handler is THandler typedHandler))
            {
                throw new InvalidOperationException(
                    $"The strategy registered for {actionKind} does not implement {typeof(THandler).Name}.");
            }

            return typedHandler;
        }

        private THandler GetRequired<THandler>(CardActionStrategyId strategyId)
            where THandler : class, ICardActionStrategyHandler
        {
            if (!strategyId.IsDefined)
            {
                throw new ArgumentException("A card-action strategy ID is required.", nameof(strategyId));
            }

            if (!handlersById.TryGetValue(strategyId, out var handler))
            {
                throw new KeyNotFoundException($"No card-action strategy is registered for {strategyId}.");
            }

            if (!(handler is THandler typedHandler))
            {
                throw new InvalidOperationException(
                    $"The strategy registered for {strategyId} does not implement {typeof(THandler).Name}.");
            }

            return typedHandler;
        }
    }

    public interface IMovementCardActionStrategyBackend
    {
        CardActionResolutionResult StartMovement(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances);

        CardActionResolutionResult StartTurnMovement(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances);

        CardActionResolutionResult AnswerMovement(MovementChoiceCommand command);
    }

    public interface IAttackDefenseCardActionStrategyBackend
    {
        AttackActionResolutionResult StartAttack(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants);

        AttackActionResolutionResult StartTurnAttack(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants);

        AttackActionResolutionResult AnswerAttackTarget(
            AttackTargetChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants);

        AttackActionResolutionResult AnswerDefense(
            DefenseResponseChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants);

        AttackActionResolutionResult ResolveOutcome(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants);
    }

    public sealed class MovementCardActionStrategyHandler : ICardActionStrategyHandler
    {
        private readonly IMovementCardActionStrategyBackend backend;

        public MovementCardActionStrategyHandler(IMovementCardActionStrategyBackend backend)
        {
            this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public CardActionStrategyId StrategyId => CardActionStrategyIds.Movement;

        public ActionKind ActionKind => ActionKind.Movement;

        public CardActionResolutionResult Start(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances) =>
            backend.StartMovement(resolutionId, offer, definitions, instances);

        public CardActionResolutionResult StartTurn(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances) =>
            backend.StartTurnMovement(resolutionId, offer, definitions, instances);

        public CardActionResolutionResult Answer(MovementChoiceCommand command) =>
            backend.AnswerMovement(command);
    }

    public sealed class AttackDefenseCardActionStrategyHandler : ICardActionStrategyHandler
    {
        private readonly IAttackDefenseCardActionStrategyBackend backend;

        public AttackDefenseCardActionStrategyHandler(IAttackDefenseCardActionStrategyBackend backend)
        {
            this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

        public CardActionStrategyId StrategyId => CardActionStrategyIds.AttackDefense;

        public ActionKind ActionKind => ActionKind.Attack;

        public AttackActionResolutionResult Start(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants) =>
            backend.StartAttack(resolutionId, offer, definitions, instances, participants);

        public AttackActionResolutionResult StartTurn(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants) =>
            backend.StartTurnAttack(resolutionId, offer, definitions, instances, participants);

        public AttackActionResolutionResult AnswerTarget(
            AttackTargetChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants) =>
            backend.AnswerAttackTarget(command, definitions, instances, participants);

        public AttackActionResolutionResult AnswerDefense(
            DefenseResponseChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants) =>
            backend.AnswerDefense(command, definitions, instances, participants);

        public AttackActionResolutionResult ResolveOutcome(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants) =>
            backend.ResolveOutcome(definitions, instances, participants);
    }
}
