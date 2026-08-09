using System;
using GuardsDigital.Core.Board;

namespace GuardsDigital.Core.Rules.CardResolution
{
    public readonly struct MovementChoiceCommand
    {
        private MovementChoiceCommand(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            MovementChoiceKind choiceKind,
            HexCoordinate destination)
        {
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("Movement choice command requires a resolution ID.", nameof(resolutionId));
            }

            if (!choiceId.IsDefined)
            {
                throw new ArgumentException("Movement choice command requires a choice ID.", nameof(choiceId));
            }

            if (choiceKind == MovementChoiceKind.None)
            {
                throw new ArgumentException("Movement choice command requires a choice kind.", nameof(choiceKind));
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChoiceKind = choiceKind;
            Destination = destination;
        }

        public ActionResolutionId ResolutionId { get; }

        public ActionResolutionStepId ChoiceId { get; }

        public MovementChoiceKind ChoiceKind { get; }

        public HexCoordinate Destination { get; }

        public static MovementChoiceCommand SelectDestination(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            HexCoordinate destination)
        {
            return new MovementChoiceCommand(
                resolutionId,
                choiceId,
                MovementChoiceKind.Destination,
                destination);
        }

        public static MovementChoiceCommand Stop(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId)
        {
            return new MovementChoiceCommand(
                resolutionId,
                choiceId,
                MovementChoiceKind.Stop,
                default);
        }
    }
}
