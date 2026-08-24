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

        /// <summary>
        /// The one restriction that obeys both of two - milestone 8.20, where a Movement action-start
        /// answer comes from two places for the first time.
        ///
        /// <para><b>Why one value and not two consultations.</b> The composed evaluator seam has always
        /// answered this question, and milestone 8.20's Slippery Ground and Deluge declare a cap
        /// through the ordinary modification set instead - so a backend that asked each in turn would
        /// have two answers to the same question and a reader would have to know which won. Combining
        /// them here means the backend still holds exactly one.</para>
        ///
        /// <para>A prevention wins over any cap, because a hero who may not take the action at all is
        /// not a hero who may take a short one; and two caps take the smaller, which is the same
        /// reading <see cref="Modifications.RuleModificationSet.TryGetActivityLimit"/> takes among
        /// declarations, stated once more here because these two arrive from different families.</para>
        /// </summary>
        public static MovementActionStartRestriction Tightest(
            MovementActionStartRestriction first, MovementActionStartRestriction second)
        {
            if (first.PreventsAction || second.PreventsAction)
            {
                return Prevent();
            }

            if (!first.MaximumDistance.HasValue) return second;
            if (!second.MaximumDistance.HasValue) return first;
            return LimitTo(Math.Min(first.MaximumDistance.Value, second.MaximumDistance.Value));
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
