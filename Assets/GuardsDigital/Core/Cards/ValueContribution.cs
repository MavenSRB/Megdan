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
                null,
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
                null,
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
                null,
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
                null,
                channel,
                amount,
                false)
        {
        }

        /// <summary>
        /// A contribution from a persistent non-Active-effect source, identified by that source's stable
        /// ID. The identity is a string because <c>ContinuousRuleSource</c> identifies itself with one:
        /// a token property, an Ultimate, and a marker are different objects with no shared typed ID, and
        /// inventing one here would make this struct the authority on what a continuous source is.
        /// </summary>
        public static ValueContribution FromContinuousSource(
            string sourceId,
            PrintedValueChannel channel,
            int amount) =>
            new ValueContribution(
                ValueContributionKind.ContinuousSourceModifier,
                default,
                default,
                default,
                default,
                sourceId,
                channel,
                amount,
                false);

        private ValueContribution(
            ValueContributionKind kind,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId sourceActionDefinitionId,
            MinionId sourceMinionId,
            ActiveEffectId sourceActiveEffectId,
            string sourceContinuousSourceId,
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

            if (kind == ValueContributionKind.ContinuousSourceModifier &&
                (string.IsNullOrWhiteSpace(sourceContinuousSourceId) || sourceCardInstanceId.IsDefined ||
                    sourceActionDefinitionId.IsDefined || sourceMinionId.IsDefined ||
                    sourceActiveEffectId.IsDefined))
            {
                throw new ArgumentException(
                    "A continuous-source contribution requires exactly one continuous-source identity.");
            }

            if (kind != ValueContributionKind.ContinuousSourceModifier &&
                !string.IsNullOrEmpty(sourceContinuousSourceId))
            {
                throw new ArgumentException(
                    "Only a continuous-source contribution carries a continuous-source identity.");
            }

            if (kind != ValueContributionKind.PrintedIcon &&
                kind != ValueContributionKind.MinionDefenseModifier &&
                kind != ValueContributionKind.ItemModifier &&
                kind != ValueContributionKind.CardTextModifier &&
                kind != ValueContributionKind.ActiveEffectModifier &&
                kind != ValueContributionKind.ContinuousSourceModifier)
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
            SourceContinuousSourceId = sourceContinuousSourceId;
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

        /// <summary>The stable ID of the continuous source, when one supplied this contribution.</summary>
        public string SourceContinuousSourceId { get; }

        public bool HasSourceContinuousSourceId => !string.IsNullOrEmpty(SourceContinuousSourceId);

        public PrintedValueChannel Channel { get; }

        public int Amount { get; }

        public bool Equals(ValueContribution other)
        {
            return Kind == other.Kind &&
                SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
                SourceActionDefinitionId.Equals(other.SourceActionDefinitionId) &&
                SourceMinionId.Equals(other.SourceMinionId) &&
                SourceActiveEffectId.Equals(other.SourceActiveEffectId) &&
                string.Equals(SourceContinuousSourceId, other.SourceContinuousSourceId, StringComparison.Ordinal) &&
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
                hashCode = (hashCode * 397) ^ (SourceContinuousSourceId == null
                    ? 0
                    : StringComparer.Ordinal.GetHashCode(SourceContinuousSourceId));
                hashCode = (hashCode * 397) ^ (int)Channel;
                hashCode = (hashCode * 397) ^ Amount;
                return hashCode;
            }
        }
    }
}
