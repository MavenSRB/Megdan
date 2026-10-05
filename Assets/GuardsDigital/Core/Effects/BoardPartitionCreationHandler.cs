using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// Chooses one of three unoriented hex axes and captures the acting body's current
    /// coordinate as a fixed, persisted Active-effect line. The definition alone decides
    /// whether the line also separates units from its controller.
    /// </summary>
    public sealed class BoardPartitionCreationHandler : IEffectCapabilityHandler
    {
        private static readonly EffectValue[] Axes =
        {
            EffectValue.Integer((int)BoardPartitionAxis.EastWest),
            EffectValue.Integer((int)BoardPartitionAxis.NorthEastSouthWest),
            EffectValue.Integer((int)BoardPartitionAxis.NorthWestSouthEast)
        };

        private readonly ActiveEffectDefinition definition;

        public BoardPartitionCreationHandler(
            EffectCapabilityReference reference, ActiveEffectDefinition definition)
        {
            if (!reference.IsDefined)
                throw new ArgumentException("A line-creation capability requires a reference.",
                    nameof(reference));
            if (definition == null ||
                definition.BoardPartitionMode == BoardPartitionMode.None)
                throw new ArgumentException(
                    "A line-creation capability requires a board-partition definition.",
                    nameof(definition));
            Reference = reference;
            this.definition = definition;
            ActiveEffectDefinitions = Array.AsReadOnly(new[] { definition });
        }

        public EffectCapabilityReference Reference { get; }
        public EffectCapabilityParameterSchema Schema { get; } =
            new EffectCapabilityParameterSchema();
        public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; }

        public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
            CanCreate(context) ? EffectExecutionResultKind.NeedsChoice
                : EffectExecutionResultKind.ImpossibleMandatory;

        public EffectExecutionResult Execute(EffectCapabilityContext context) =>
            CanCreate(context)
                ? EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor, Axes, 1, 1,
                    valueVocabulary: EffectChoiceValueVocabulary.BoardPartitionAxis))
                : EffectExecutionResult.ImpossibleMandatory();

        public EffectExecutionResult Resume(
            EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
        {
            if (!CanCreate(context) || selected == null || selected.Count != 1 ||
                !AxesContains(selected[0]) ||
                !context.Components.Board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(context.ActorId), out var origin))
                return EffectExecutionResult.ImpossibleMandatory();

            var line = new BoardPartitionLine(origin,
                (BoardPartitionAxis)selected[0].IntegerValue);
            var id = ActiveEffectInstanceIdentity.Mint(context, definition);
            if (!HasEffect(context, id))
                context.Timing.Create(
                    id, definition, context.ActorId,
                    context.Participants.GetTeamForHero(context.ActorId).Side,
                    default, context.SourceCardInstanceId, context.ActionDefinitionId,
                    Math.Max(context.RoundIndex, 1), Math.Max(context.TurnIndex, 1),
                    boardPartitionLine: line);
            return EffectExecutionResult.Applied();
        }

        private static bool AxesContains(EffectValue value) =>
            value.Kind == EffectValueKind.Integer &&
            Array.Exists(Axes, axis => axis.Equals(value));

        private static bool HasEffect(EffectCapabilityContext context, ActiveEffectId id)
        {
            foreach (var effect in context.Timing.Effects)
                if (effect.Id.Equals(id))
                    return true;
            return false;
        }

        private static bool CanCreate(EffectCapabilityContext context) =>
            context?.Timing != null && context.Participants != null &&
            context.Components?.Board != null &&
            context.Components.Board.TryGetObjectCoordinate(
                BoardObjectReference.ForHero(context.ActorId), out _);
    }
}
