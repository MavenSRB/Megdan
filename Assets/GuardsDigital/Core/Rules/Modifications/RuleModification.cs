using System;
using GuardsDigital.Core.Cards;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// The five reusable families <c>CARD_EFFECT_CAPABILITIES.md</c> groups as
    /// "modifier/restriction capability": they change a derived value, a permission, or an effective
    /// characteristic rather than performing a state transition. They stay distinct because the rulebook
    /// answers different questions with them and a query consults exactly one.
    /// </summary>
    public enum RuleModificationKind
    {
        None = 0,

        /// <summary>A typed contribution to one printed value channel (p.9-10, p.14).</summary>
        ValueModifier = 1,

        /// <summary>The affected object cannot perform the named activity.</summary>
        Restriction = 2,

        /// <summary>The affected object may do something the general rules otherwise forbid (p.18).</summary>
        Permission = 3,

        /// <summary>The affected object cannot be targeted or affected by the named activity (p.18).</summary>
        Immunity = 4,

        /// <summary>The affected object's rules and qualities are replaced by another type's (p.17).</summary>
        CountsAs = 5
    }

    /// <summary>
    /// The exact thing a permission grants. Rulebook p.18 defines one such grant today - a unit that
    /// ignores obstacles "is allowed to enter a space with an obstacle and leave that space in any
    /// direction, while moving", which p.18's <c>Move Through</c> entry points at - so this enum has one
    /// member. A permission is narrow by construction: it names a grant, not an activity, so it can never
    /// be read as cancelling an unrelated restriction.
    /// </summary>
    public enum RulePermissionKind
    {
        None = 0,
        MoveThroughObstacles = 1
    }

    /// <summary>
    /// An effective object type a <c>Counts As</c> modification can supply. Rulebook p.19 defines units
    /// as heroes and minions, and p.17 says the object's rules and qualities are replaced by those of the
    /// type it now counts as.
    /// </summary>
    public enum EffectiveUnitKind
    {
        None = 0,
        Hero = 1,
        Minion = 2
    }

    /// <summary>
    /// One immutable typed rules modification, as definition content.
    ///
    /// It is deliberately not bound to an object or a lifetime: a definition declares what the
    /// modification does, and <see cref="AppliedRuleModification"/> binds it to the object it affects and
    /// the source whose lifetime it follows. That split is what lets an Active-effect definition and a
    /// token property declare the same modification without either owning the other's lifetime rules.
    /// </summary>
    public sealed class RuleModification
    {
        private RuleModification(
            RuleModificationKind kind,
            RuleActivity activity,
            RulePermissionKind permission,
            PrintedValueChannel channel,
            int amount,
            EffectiveUnitKind effectiveUnitKind)
        {
            Kind = kind;
            Activity = activity;
            Permission = permission;
            Channel = channel;
            Amount = amount;
            EffectiveUnitKind = effectiveUnitKind;
        }

        /// <summary>A typed contribution of <paramref name="amount"/> to one value channel.</summary>
        public static RuleModification ForValue(PrintedValueChannel channel, int amount)
        {
            if (channel == PrintedValueChannel.None)
            {
                throw new ArgumentException("A value modifier requires a channel.", nameof(channel));
            }

            if (amount == 0)
            {
                throw new ArgumentException(
                    "A value modifier contributes a nonzero amount; a zero contribution is not a rule.",
                    nameof(amount));
            }

            return new RuleModification(
                RuleModificationKind.ValueModifier,
                RuleActivity.None,
                RulePermissionKind.None,
                channel,
                amount,
                EffectiveUnitKind.None);
        }

        public static RuleModification ForRestriction(RuleActivity activity) =>
            new RuleModification(
                RuleModificationKind.Restriction,
                RequireActivity(activity, "restriction"),
                RulePermissionKind.None,
                PrintedValueChannel.None,
                0,
                EffectiveUnitKind.None);

        public static RuleModification ForPermission(RulePermissionKind permission)
        {
            if (permission == RulePermissionKind.None)
            {
                throw new ArgumentException("A permission requires the exact grant it gives.", nameof(permission));
            }

            return new RuleModification(
                RuleModificationKind.Permission,
                RuleActivity.None,
                permission,
                PrintedValueChannel.None,
                0,
                EffectiveUnitKind.None);
        }

        public static RuleModification ForImmunity(RuleActivity activity) =>
            new RuleModification(
                RuleModificationKind.Immunity,
                RequireActivity(activity, "immunity"),
                RulePermissionKind.None,
                PrintedValueChannel.None,
                0,
                EffectiveUnitKind.None);

        public static RuleModification ForCountsAs(EffectiveUnitKind effectiveUnitKind)
        {
            if (effectiveUnitKind == EffectiveUnitKind.None)
            {
                throw new ArgumentException(
                    "A Counts As modification requires the effective type it supplies.", nameof(effectiveUnitKind));
            }

            return new RuleModification(
                RuleModificationKind.CountsAs,
                RuleActivity.None,
                RulePermissionKind.None,
                PrintedValueChannel.None,
                0,
                effectiveUnitKind);
        }

        public RuleModificationKind Kind { get; }

        /// <summary>The named activity for a restriction or an immunity; otherwise <c>None</c>.</summary>
        public RuleActivity Activity { get; }

        /// <summary>The exact grant for a permission; otherwise <c>None</c>.</summary>
        public RulePermissionKind Permission { get; }

        /// <summary>The value channel for a value modifier; otherwise <c>None</c>.</summary>
        public PrintedValueChannel Channel { get; }

        /// <summary>The contributed amount for a value modifier; otherwise zero.</summary>
        public int Amount { get; }

        /// <summary>The supplied effective type for a <c>Counts As</c>; otherwise <c>None</c>.</summary>
        public EffectiveUnitKind EffectiveUnitKind { get; }

        /// <summary>
        /// Whether this declaration reaches <paramref name="activity"/>. An <see cref="RuleActivity.All"/>
        /// declaration reaches every activity; a specific one reaches only itself, which is p.18's "is
        /// affected by other effects and actions".
        /// </summary>
        public bool Covers(RuleActivity activity)
        {
            if (activity == RuleActivity.None)
            {
                throw new ArgumentException("Coverage requires a named activity.", nameof(activity));
            }

            return Activity == RuleActivity.All || Activity == activity;
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case RuleModificationKind.ValueModifier:
                    return $"{Channel} {(Amount > 0 ? "+" : string.Empty)}{Amount}";
                case RuleModificationKind.Restriction:
                    return $"Cannot {Activity}";
                case RuleModificationKind.Permission:
                    return $"May {Permission}";
                case RuleModificationKind.Immunity:
                    return $"Immune to {Activity}";
                case RuleModificationKind.CountsAs:
                    return $"Counts as {EffectiveUnitKind}";
                default:
                    return "Undeclared modification";
            }
        }

        private static RuleActivity RequireActivity(RuleActivity activity, string family)
        {
            if (activity == RuleActivity.None)
            {
                throw new ArgumentException(
                    $"A {family} requires the activity it names; omitting it would leave its scope implicit.",
                    nameof(activity));
            }

            return activity;
        }
    }
}
