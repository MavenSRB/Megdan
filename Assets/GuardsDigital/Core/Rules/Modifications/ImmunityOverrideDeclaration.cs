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
        PrintedHeavyMinionImmunity = 1,

        /// <summary>
        /// Milestone 8.39b. Printed text that says to ignore immunity with no qualification at all -
        /// Cutter's Grappling Bolt, "Target an obstacle in range and in a straight line, with no
        /// obstacles between you; <b>ignore immunity</b>."
        ///
        /// <para><b>It is a second reason rather than a widening of the first, because the two
        /// sentences say different things.</b> p.22's Heavy rule and a card's own
        /// <c>immunity@2</c> declaration are two protections, and "ignore a Heavy minion's immunity"
        /// names one of them. This clause names neither and therefore both: p.18's Immune keyword is
        /// written to be overridden in exactly these words - "If a unit is immune, you cannot target
        /// that unit ... <em>unless the card text says otherwise</em>" - and this is the card text it
        /// anticipates.</para>
        ///
        /// <para>Grappling Bolt prints it because p.18's obstacles include units, so the thing it
        /// grapples to may be a hero or a minion and may be immune. Terrain and tokens are obstacles
        /// that are not units and carry no immunity for this to reach.</para>
        /// </summary>
        PrintedUnqualifiedImmunity = 2
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

            // The vocabulary is closed at runtime as well as at compile time, which is what kept
            // milestone 8.39b's new member from working until it was named here too: a reason this
            // build does not implement is a declaration nothing would honour.
            if (reason != ImmunityOverrideReason.PrintedHeavyMinionImmunity &&
                reason != ImmunityOverrideReason.PrintedUnqualifiedImmunity)
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
