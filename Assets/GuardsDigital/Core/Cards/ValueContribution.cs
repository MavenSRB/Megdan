using System;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards
{
    public readonly struct ValueContribution : IEquatable<ValueContribution>
    {
        public ValueContribution(
            ValueContributionKind kind,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId sourceActionDefinitionId,
            PrintedValueChannel channel,
            int amount)
            : this(
                kind,
                sourceCardInstanceId,
                sourceActionDefinitionId,
                default,
                default,
                channel,
                amount,
                true)
        {
        }

        public ValueContribution(
            ValueContributionKind kind,
            CardInstanceId sourceCardInstanceId,
            PrintedValueChannel channel,
            int amount)
            : this(
                kind,
                sourceCardInstanceId,
                default,
                default,
                default,
                channel,
                amount,
                false)
        {
        }

        public ValueContribution(
            ValueContributionKind kind,
            MinionId sourceMinionId,
            PrintedValueChannel channel,
            int amount)
            : this(
                kind,
                default,
                default,
                sourceMinionId,
                default,
                channel,
                amount,
                false)
        {
        }

        public ValueContribution(
            ValueContributionKind kind,
            ActiveEffectId sourceActiveEffectId,
            PrintedValueChannel channel,
            int amount)
            : this(
                kind,
                default,
                default,
                default,
                sourceActiveEffectId,
                channel,
                amount,
                false)
        {
        }

        private ValueContribution(
            ValueContributionKind kind,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId sourceActionDefinitionId,
            MinionId sourceMinionId,
            ActiveEffectId sourceActiveEffectId,
            PrintedValueChannel channel,
            int amount,
            bool requiresActionDefinition)
        {
            if (kind == ValueContributionKind.None)
            {
                throw new ArgumentException("Printed value contribution requires a kind.", nameof(kind));
            }

            var usesCardSource = kind == ValueContributionKind.PrintedIcon ||
                kind == ValueContributionKind.ItemModifier ||
                kind == ValueContributionKind.CardTextModifier;
            if (usesCardSource && !sourceCardInstanceId.IsDefined)
            {
                throw new ArgumentException("Card-sourced value contributions require a source card instance ID.", nameof(sourceCardInstanceId));
            }

            if (kind == ValueContributionKind.PrintedIcon && requiresActionDefinition && !sourceActionDefinitionId.IsDefined)
            {
                throw new ArgumentException("Printed value contribution requires a source action definition ID.", nameof(sourceActionDefinitionId));
            }

            if (kind == ValueContributionKind.PrintedIcon && !requiresActionDefinition && channel != PrintedValueChannel.Initiative)
            {
                throw new ArgumentException(
                    "Only a card-wide Initiative contribution may omit an action definition ID.",
                    nameof(channel));
            }

            if (kind == ValueContributionKind.ItemModifier && requiresActionDefinition)
            {
                throw new ArgumentException("An item contribution identifies its item card, not an action definition.");
            }

            if (kind == ValueContributionKind.CardTextModifier &&
                (!requiresActionDefinition || !sourceActionDefinitionId.IsDefined))
            {
                throw new ArgumentException(
                    "A card-text contribution requires its source card and action definition.");
            }

            if (kind == ValueContributionKind.ActiveEffectModifier &&
                (!sourceActiveEffectId.IsDefined || sourceCardInstanceId.IsDefined ||
                    sourceActionDefinitionId.IsDefined || sourceMinionId.IsDefined))
            {
                throw new ArgumentException(
                    "An Active-effect contribution requires exactly one typed Active-effect source.");
            }

            if (kind == ValueContributionKind.MinionDefenseModifier &&
                (!sourceMinionId.IsDefined || channel != PrintedValueChannel.Defense ||
                    (amount != -1 && amount != 1)))
            {
                throw new ArgumentException(
                    "A minion Defense contribution requires a minion source, the Defense channel, and an amount of -1 or +1.");
            }

            if (kind != ValueContributionKind.PrintedIcon &&
                kind != ValueContributionKind.MinionDefenseModifier &&
                kind != ValueContributionKind.ItemModifier &&
                kind != ValueContributionKind.CardTextModifier &&
                kind != ValueContributionKind.ActiveEffectModifier)
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported value contribution kind.");
            }

            if (channel == PrintedValueChannel.None)
            {
                throw new ArgumentException("Printed value contribution requires a channel.", nameof(channel));
            }

            Kind = kind;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceActionDefinitionId = sourceActionDefinitionId;
            SourceMinionId = sourceMinionId;
            SourceActiveEffectId = sourceActiveEffectId;
            Channel = channel;
            Amount = amount;
        }

        public ValueContributionKind Kind { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public ActionDefinitionId SourceActionDefinitionId { get; }

        public bool HasSourceActionDefinitionId => SourceActionDefinitionId.IsDefined;

        public MinionId SourceMinionId { get; }

        public bool HasSourceMinionId => SourceMinionId.IsDefined;

        public ActiveEffectId SourceActiveEffectId { get; }

        public bool HasSourceActiveEffectId => SourceActiveEffectId.IsDefined;

        public PrintedValueChannel Channel { get; }

        public int Amount { get; }

        public bool Equals(ValueContribution other)
        {
            return Kind == other.Kind &&
                SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
                SourceActionDefinitionId.Equals(other.SourceActionDefinitionId) &&
                SourceMinionId.Equals(other.SourceMinionId) &&
                SourceActiveEffectId.Equals(other.SourceActiveEffectId) &&
                Channel == other.Channel &&
                Amount == other.Amount;
        }

        public override bool Equals(object obj)
        {
            return obj is ValueContribution other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ SourceCardInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ SourceActionDefinitionId.GetHashCode();
                hashCode = (hashCode * 397) ^ SourceMinionId.GetHashCode();
                hashCode = (hashCode * 397) ^ SourceActiveEffectId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Channel;
                hashCode = (hashCode * 397) ^ Amount;
                return hashCode;
            }
        }
    }
}
