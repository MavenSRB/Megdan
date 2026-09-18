using System;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.46. The words a player reads on the control that answers one effect node's pause:
    /// the consequence of choosing a <c>Choose</c> arm, or of accepting - and, separately, declining -
    /// an optional step. Declared beside the content that raises the pause, so a control can say
    /// "Defeat a minion adjacent to you" instead of the node id it was built from, and "Move it again"
    /// / "Stop here" instead of accept and decline.
    ///
    /// <para>Authored in the printed text's own case; presentation upper-cases for the HUD. A label
    /// names the CONSEQUENCE, never the mechanism, which is the user's rule for the whole series.</para>
    /// </summary>
    public readonly struct DecisionLabel : IEquatable<DecisionLabel>
    {
        public DecisionLabel(string chosen, string declined = null)
        {
            if (string.IsNullOrWhiteSpace(chosen))
            {
                throw new ArgumentException("A decision label names what choosing does.", nameof(chosen));
            }

            if (declined != null && string.IsNullOrWhiteSpace(declined))
            {
                throw new ArgumentException(
                    "A declined label, when given, names what declining does.", nameof(declined));
            }

            Chosen = chosen;
            Declined = declined;
        }

        /// <summary>What pressing the control does: the arm taken, or the optional step accepted.</summary>
        public string Chosen { get; }

        /// <summary>
        /// What declining does, for an optional step; <c>null</c> for a <c>Choose</c> arm, which has no
        /// decline of its own.
        /// </summary>
        public string Declined { get; }

        public bool IsDefined => Chosen != null;

        public bool HasDeclined => Declined != null;

        public bool Equals(DecisionLabel other) =>
            string.Equals(Chosen, other.Chosen, StringComparison.Ordinal) &&
            string.Equals(Declined, other.Declined, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is DecisionLabel other && Equals(other);

        public override int GetHashCode() =>
            ((Chosen == null ? 0 : StringComparer.Ordinal.GetHashCode(Chosen)) * 397) ^
            (Declined == null ? 0 : StringComparer.Ordinal.GetHashCode(Declined));

        public override string ToString() =>
            !IsDefined ? "(undefined)" : Declined == null ? Chosen : Chosen + " / " + Declined;
    }
}
