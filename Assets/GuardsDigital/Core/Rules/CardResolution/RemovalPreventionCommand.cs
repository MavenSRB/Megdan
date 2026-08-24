using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 8.19c1. The answer to a standing removal-prevention decision: pay one source's price
    /// and keep the miniature on the board, or decline and let the removal happen.
    ///
    /// <para>Declining is always available, which is what keeps the decision from ever deadlocking a
    /// match: the removal was going to happen anyway, and a source whose price can no longer be paid
    /// simply stops being offered.</para>
    /// </summary>
    public sealed class RemovalPreventionCommand : IEquatable<RemovalPreventionCommand>
    {
        private RemovalPreventionCommand(
            string requestId,
            HeroId decidingHeroId,
            bool accepted,
            string sourceId,
            CardInstanceId paymentCardInstanceId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                throw new ArgumentException(
                    "A removal-prevention answer names the decision it answers.", nameof(requestId));
            }

            if (!decidingHeroId.IsDefined)
            {
                throw new ArgumentException(
                    "A removal-prevention answer names the hero giving it.", nameof(decidingHeroId));
            }

            RequestId = requestId;
            DecidingHeroId = decidingHeroId;
            Accepted = accepted;
            SourceId = sourceId;
            PaymentCardInstanceId = paymentCardInstanceId;
        }

        /// <summary>Pays <paramref name="paymentCardInstanceId"/> to the named source's price.</summary>
        public static RemovalPreventionCommand Accept(
            string requestId,
            HeroId decidingHeroId,
            string sourceId,
            CardInstanceId paymentCardInstanceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "An accepted prevention names the source whose price is being paid.",
                    nameof(sourceId));
            }

            if (!paymentCardInstanceId.IsDefined)
            {
                throw new ArgumentException(
                    "An accepted prevention names the card that pays for it.",
                    nameof(paymentCardInstanceId));
            }

            return new RemovalPreventionCommand(
                requestId, decidingHeroId, true, sourceId, paymentCardInstanceId);
        }

        /// <summary>Lets the removal happen, paying nothing.</summary>
        public static RemovalPreventionCommand Decline(string requestId, HeroId decidingHeroId) =>
            new RemovalPreventionCommand(requestId, decidingHeroId, false, null, default);

        public string RequestId { get; }
        public HeroId DecidingHeroId { get; }
        public bool Accepted { get; }

        /// <summary>The source whose price is paid; <c>null</c> on a decline.</summary>
        public string SourceId { get; }

        /// <summary>The card that pays; undefined on a decline.</summary>
        public CardInstanceId PaymentCardInstanceId { get; }

        public bool Equals(RemovalPreventionCommand other) =>
            other != null &&
            string.Equals(RequestId, other.RequestId, StringComparison.Ordinal) &&
            DecidingHeroId.Equals(other.DecidingHeroId) &&
            Accepted == other.Accepted &&
            string.Equals(SourceId, other.SourceId, StringComparison.Ordinal) &&
            PaymentCardInstanceId.Equals(other.PaymentCardInstanceId);

        public override bool Equals(object obj) => Equals(obj as RemovalPreventionCommand);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StringComparer.Ordinal.GetHashCode(RequestId ?? string.Empty);
                hash = (hash * 397) ^ DecidingHeroId.GetHashCode();
                hash = (hash * 397) ^ (Accepted ? 1 : 0);
                hash = (hash * 397) ^ StringComparer.Ordinal.GetHashCode(SourceId ?? string.Empty);
                return (hash * 397) ^ PaymentCardInstanceId.GetHashCode();
            }
        }
    }
}
