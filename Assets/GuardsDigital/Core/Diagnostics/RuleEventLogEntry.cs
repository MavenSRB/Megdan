using System;

namespace GuardsDigital.Core.Diagnostics
{
    public sealed class RuleEventLogEntry : IEquatable<RuleEventLogEntry>
    {
        public RuleEventLogEntry(
            int sequence,
            string eventType,
            string stableReferences,
            string detail)
        {
            if (sequence < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(sequence));
            }

            if (string.IsNullOrWhiteSpace(eventType))
            {
                throw new ArgumentException("A rule-event log entry requires an event type.", nameof(eventType));
            }

            if (string.IsNullOrWhiteSpace(stableReferences))
            {
                throw new ArgumentException(
                    "A rule-event log entry requires stable rules-object references.",
                    nameof(stableReferences));
            }

            if (string.IsNullOrWhiteSpace(detail))
            {
                throw new ArgumentException("A rule-event log entry requires a diagnostic detail.", nameof(detail));
            }

            Sequence = sequence;
            EventType = eventType;
            StableReferences = stableReferences;
            Detail = detail;
        }

        public int Sequence { get; }

        public string EventType { get; }

        public string StableReferences { get; }

        public string Detail { get; }

        public string DisplayText =>
            $"{Sequence:0000} | {EventType} | {StableReferences} | {Detail}";

        public bool Equals(RuleEventLogEntry other)
        {
            return other != null &&
                Sequence == other.Sequence &&
                string.Equals(EventType, other.EventType, StringComparison.Ordinal) &&
                string.Equals(StableReferences, other.StableReferences, StringComparison.Ordinal) &&
                string.Equals(Detail, other.Detail, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RuleEventLogEntry);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Sequence;
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(EventType);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(StableReferences);
                hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Detail);
                return hashCode;
            }
        }
    }
}
