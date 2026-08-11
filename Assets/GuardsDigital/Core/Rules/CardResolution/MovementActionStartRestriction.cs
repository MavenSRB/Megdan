using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public readonly struct MovementActionStartContext
    {
        public MovementActionStartContext(
            HeroId actorId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            ActionInvocationContext invocationContext,
            int movementAllowance)
        {
            if (!actorId.IsDefined)
            {
                throw new ArgumentException("Movement action start context requires an actor.", nameof(actorId));
            }

            if (!sourceCardInstanceId.IsDefined)
            {
                throw new ArgumentException("Movement action start context requires a source card.", nameof(sourceCardInstanceId));
            }

            if (!actionDefinitionId.IsDefined)
            {
                throw new ArgumentException("Movement action start context requires an action definition.", nameof(actionDefinitionId));
            }

            if (!Enum.IsDefined(typeof(ActionInvocationContext), invocationContext) ||
                invocationContext == ActionInvocationContext.None)
            {
                throw new ArgumentException("Movement action start context requires an invocation context.", nameof(invocationContext));
            }

            if (movementAllowance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(movementAllowance));
            }

            ActorId = actorId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActionDefinitionId = actionDefinitionId;
            InvocationContext = invocationContext;
            MovementAllowance = movementAllowance;
        }

        public HeroId ActorId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        public ActionInvocationContext InvocationContext { get; }

        public int MovementAllowance { get; }
    }

    public readonly struct MovementActionStartRestriction
    {
        private MovementActionStartRestriction(bool preventsAction, int? maximumDistance)
        {
            PreventsAction = preventsAction;
            MaximumDistance = maximumDistance;
        }

        public bool PreventsAction { get; }

        public int? MaximumDistance { get; }

        public static MovementActionStartRestriction None => default;

        public static MovementActionStartRestriction Prevent() =>
            new MovementActionStartRestriction(true, null);

        public static MovementActionStartRestriction LimitTo(int maximumDistance)
        {
            if (maximumDistance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumDistance));
            }

            return new MovementActionStartRestriction(false, maximumDistance);
        }

        public int ApplyTo(int movementAllowance)
        {
            if (movementAllowance < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(movementAllowance));
            }

            return MaximumDistance.HasValue
                ? Math.Min(movementAllowance, MaximumDistance.Value)
                : movementAllowance;
        }
    }

    public interface IMovementActionStartRestrictionEvaluator
    {
        MovementActionStartRestriction Evaluate(MovementActionStartContext context);
    }

    internal sealed class UnrestrictedMovementActionStartEvaluator : IMovementActionStartRestrictionEvaluator
    {
        internal static readonly UnrestrictedMovementActionStartEvaluator Instance =
            new UnrestrictedMovementActionStartEvaluator();

        private UnrestrictedMovementActionStartEvaluator()
        {
        }

        public MovementActionStartRestriction Evaluate(MovementActionStartContext context) =>
            MovementActionStartRestriction.None;
    }
}
