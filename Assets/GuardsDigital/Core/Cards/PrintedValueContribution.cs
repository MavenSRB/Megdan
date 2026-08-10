using System;

namespace GuardsDigital.Core.Cards
{
    public readonly struct PrintedValueContribution : IEquatable<PrintedValueContribution>
    {
        public PrintedValueContribution(
            PrintedValueContributionKind kind,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId sourceActionDefinitionId,
            PrintedValueChannel channel,
            int amount)
            : this(
                kind,
                sourceCardInstanceId,
                sourceActionDefinitionId,
                channel,
                amount,
                true)
        {
        }

        public PrintedValueContribution(
            PrintedValueContributionKind kind,
            CardInstanceId sourceCardInstanceId,
            PrintedValueChannel channel,
            int amount)
            : this(
                kind,
                sourceCardInstanceId,
                default,
                channel,
                amount,
                false)
        {
        }

        private PrintedValueContribution(
            PrintedValueContributionKind kind,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId sourceActionDefinitionId,
            PrintedValueChannel channel,
            int amount,
            bool requiresActionDefinition)
        {
            if (kind == PrintedValueContributionKind.None)
            {
                throw new ArgumentException("Printed value contribution requires a kind.", nameof(kind));
            }

            if (!sourceCardInstanceId.IsDefined)
            {
                throw new ArgumentException("Printed value contribution requires a source card instance ID.", nameof(sourceCardInstanceId));
            }

            if (requiresActionDefinition && !sourceActionDefinitionId.IsDefined)
            {
                throw new ArgumentException("Printed value contribution requires a source action definition ID.", nameof(sourceActionDefinitionId));
            }

            if (!requiresActionDefinition && channel != PrintedValueChannel.Initiative)
            {
                throw new ArgumentException(
                    "Only a card-wide Initiative contribution may omit an action definition ID.",
                    nameof(channel));
            }

            if (channel == PrintedValueChannel.None)
            {
                throw new ArgumentException("Printed value contribution requires a channel.", nameof(channel));
            }

            Kind = kind;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceActionDefinitionId = sourceActionDefinitionId;
            Channel = channel;
            Amount = amount;
        }

        public PrintedValueContributionKind Kind { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public ActionDefinitionId SourceActionDefinitionId { get; }

        public bool HasSourceActionDefinitionId => SourceActionDefinitionId.IsDefined;

        public PrintedValueChannel Channel { get; }

        public int Amount { get; }

        public bool Equals(PrintedValueContribution other)
        {
            return Kind == other.Kind &&
                SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
                SourceActionDefinitionId.Equals(other.SourceActionDefinitionId) &&
                Channel == other.Channel &&
                Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            return obj is PrintedValueContribution other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ SourceCardInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ SourceActionDefinitionId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Channel;
                hashCode = (hashCode * 397) ^ Amount;
                return hashCode;
            }
        }
    }
}
