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

        /// <summary>
        /// The named activity may not happen to or from the affected object. Unqualified, that is the
        /// object itself performing it. Milestone 8.7 lets a declaration also name <em>who</em> is
        /// barred (<see cref="RestrictedActorScope"/>), which is how card text that forbids an object
        /// being repositioned by one side and not by the other reads.
        /// </summary>
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
            EffectiveUnitKind effectiveUnitKind,
            RestrictedActorScope restrictedActors)
        {
            Kind = kind;
            Activity = activity;
            Permission = permission;
            Channel = channel;
            Amount = amount;
            EffectiveUnitKind = effectiveUnitKind;
            RestrictedActors = restrictedActors;
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
                EffectiveUnitKind.None,
                RestrictedActorScope.AnyActor);
        }

        /// <summary>
        /// A restriction naming no actor, which is p.18's ordinary form: the activity is barred
        /// however it is reached.
        /// </summary>
        public static RuleModification ForRestriction(RuleActivity activity) =>
            ForRestriction(activity, RestrictedActorScope.AnyActor);

        /// <summary>
        /// Milestone 8.7. A restriction that also names who is barred - Magnetic Dagger's "by
        /// themselves, or by enemy heroes". The scope is required rather than defaulted, for the
        /// reason milestone 6.8f required the modification set: a qualification dropped by omission
        /// silently widens a card's text into a blanket prohibition, and
        /// <see cref="RestrictedActorScope.AnyActor"/> is where a declaration that qualifies nobody
        /// says so. The overload above is that statement written once for every existing declaration.
        /// </summary>
        public static RuleModification ForRestriction(
            RuleActivity activity, RestrictedActorScope restrictedActors) =>
            new RuleModification(
                RuleModificationKind.Restriction,
                RequireActivity(activity, "restriction"),
                RulePermissionKind.None,
                PrintedValueChannel.None,
                0,
                EffectiveUnitKind.None,
                restrictedActors ?? throw new ArgumentNullException(nameof(restrictedActors)));

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
                EffectiveUnitKind.None,
                RestrictedActorScope.AnyActor);
        }

        public static RuleModification ForImmunity(RuleActivity activity) =>
            new RuleModification(
                RuleModificationKind.Immunity,
                RequireActivity(activity, "immunity"),
                RulePermissionKind.None,
                PrintedValueChannel.None,
                0,
                EffectiveUnitKind.None,
                RestrictedActorScope.AnyActor);

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
                effectiveUnitKind,
                RestrictedActorScope.AnyActor);
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
        /// Which actors a restriction bars. <see cref="RestrictedActorScope.AnyActor"/> on every other
        /// kind, because the question is not asked of them - a value modifier, a permission, an
        /// immunity and a <c>Counts As</c> are all about the affected object rather than about who is
        /// acting on it, and p.18's one actor rule for immunity ("you are never immune to your own
        /// actions") is a fixed exception rather than a declaration.
        /// </summary>
        public RestrictedActorScope RestrictedActors { get; }

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
                    return RestrictedActors.IsUnqualified
                        ? $"Cannot {Activity}"
                        : $"Cannot {Activity} {RestrictedActors}";
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
