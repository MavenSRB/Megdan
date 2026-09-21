using System;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.71a. A printed Initiative icon that some card text has REWRITTEN on one card
    /// instance, together with the card whose text wrote it.
    ///
    /// <para><b>The printed sentence this exists for.</b> Hanu the Trickster's Silver basic, Hurry up!:
    /// "Set the printed [Initiative] Initiative value of an unresolved card of a hero in range to 11,
    /// until it is resolved or otherwise changes state." Nothing in this build before it replaced a
    /// printed value. The three sources <c>InitiativeResolutionAuthority.QuerySelection</c> composes -
    /// the printed icon, p.16's Initiative items and a hero-scoped
    /// <c>ValueContributions</c> on <c>PrintedValueChannel.Initiative</c> - are all ADDITIVE, and the
    /// last two are keyed on the HERO. This one is not: the sentence names a CARD, so the rewrite is
    /// carried by the card instance and follows that card rather than its owner (HA-Q1).</para>
    ///
    /// <para><b>It replaces the icon and nothing else.</b> The value written here IS the printed value
    /// for every reader - "set the PRINTED value" is what the card says - so items and hero-scoped
    /// modifiers still compose on top of it exactly as they compose on top of an unrewritten icon.
    /// <see cref="PrintedValueQuery.CalculatePrintedInitiative"/> is the one place that reading is
    /// taken, which is why the turn order and the hero rail's Initiative badge cannot come to
    /// disagree about the same card.</para>
    ///
    /// <para><b>A second rewrite of the same card is a rewrite and not a stack.</b> The sentence sets a
    /// value rather than adding one, so writing 11 over 11 leaves 11 and writing a different number
    /// would leave that number; the source moves with it, because the newest sentence is the one in
    /// effect.</para>
    ///
    /// <para><see cref="None"/> is the named absence, which every card in this build carries until a
    /// sentence rewrites it.</para>
    /// </summary>
    public readonly struct PrintedInitiativeOverride : IEquatable<PrintedInitiativeOverride>
    {
        private readonly bool isDefined;

        public PrintedInitiativeOverride(int value, CardInstanceId sourceCardInstanceId)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "A rewritten printed Initiative is a printed icon's value and is never negative.");
            }

            if (!sourceCardInstanceId.IsDefined)
            {
                throw new ArgumentException(
                    "A rewritten printed Initiative names the card whose text rewrote it.",
                    nameof(sourceCardInstanceId));
            }

            isDefined = true;
            Value = value;
            SourceCardInstanceId = sourceCardInstanceId;
        }

        /// <summary>No sentence has rewritten this card's printed Initiative.</summary>
        public static PrintedInitiativeOverride None => default;

        /// <summary>The number the printed icon now reads, when one was written.</summary>
        public int Value { get; }

        /// <summary>
        /// The card instance whose printed text performed the rewrite - Hanu's own Hurry up! - kept so
        /// that a seat, a log row and a restore can all name the sentence in effect rather than guess
        /// at it.
        /// </summary>
        public CardInstanceId SourceCardInstanceId { get; }

        public bool IsDefined => isDefined;

        public bool Equals(PrintedInitiativeOverride other) =>
            isDefined == other.isDefined && Value == other.Value &&
            SourceCardInstanceId.Equals(other.SourceCardInstanceId);

        public override bool Equals(object obj) =>
            obj is PrintedInitiativeOverride other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = isDefined ? 1 : 0;
                hashCode = (hashCode * 397) ^ Value;
                hashCode = (hashCode * 397) ^ SourceCardInstanceId.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString() =>
            isDefined
                ? $"printed Initiative {Value} (set by {SourceCardInstanceId.Value})"
                : "no printed Initiative override";
    }
}
