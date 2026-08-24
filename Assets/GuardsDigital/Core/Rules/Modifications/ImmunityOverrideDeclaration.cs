using System;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Why one exact immunity question is answered without rulebook p.18's ordinary standing
    /// immunity. This is trusted definition content, never a card name or a magic integer.
    /// </summary>
    public enum ImmunityOverrideReason
    {
        None = 0,

        /// <summary>
        /// Printed text that says to ignore a Heavy minion's immunity. It bypasses only the standing
        /// Heavy-minion rule; an independently declared immunity to the same activity still applies.
        /// </summary>
        PrintedHeavyMinionImmunity = 1
    }

    /// <summary>
    /// One exact, trusted immunity exception carried by an effect node. Both the selection that offers
    /// a unit and the operation that finally affects it must declare and consume the activity they ask.
    /// </summary>
    public readonly struct ImmunityOverrideDeclaration : IEquatable<ImmunityOverrideDeclaration>
    {
        public ImmunityOverrideDeclaration(
            RuleActivity activity,
            ImmunityOverrideReason reason)
        {
            if (activity == RuleActivity.None || !Enum.IsDefined(typeof(RuleActivity), activity))
            {
                throw new ArgumentException(
                    "An immunity override requires the exact activity whose immunity it bypasses.",
                    nameof(activity));
            }

            if (reason != ImmunityOverrideReason.PrintedHeavyMinionImmunity)
            {
                throw new ArgumentException(
                    "An immunity override requires a supported printed reason.", nameof(reason));
            }

            Activity = activity;
            Reason = reason;
        }

        public RuleActivity Activity { get; }

        public ImmunityOverrideReason Reason { get; }

        public bool IsDefined =>
            Activity != RuleActivity.None && Reason != ImmunityOverrideReason.None;

        public bool Equals(ImmunityOverrideDeclaration other) =>
            Activity == other.Activity && Reason == other.Reason;

        public override bool Equals(object obj) =>
            obj is ImmunityOverrideDeclaration other && Equals(other);

        public override int GetHashCode() => ((int)Activity * 397) ^ (int)Reason;

        public override string ToString() => $"{Reason}:{Activity}";
    }
}
