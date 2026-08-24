using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        CountsAs = 5,

        /// <summary>
        /// Milestone 8.19c. The affected object counts as another type <em>in addition to</em> the
        /// types it already counts as - Xargatha's Stone Gaze, "Enemy heroes in radius count as both
        /// heroes and terrain".
        ///
        /// It is a sixth family rather than a mode of <see cref="CountsAs"/> because the two say
        /// opposite things about what survives. p.17's Counts As is a replacement: "all rules and
        /// qualities of that object are replaced by the rules and qualities of the object type(s) it
        /// now counts as". A card that prints "count as both" keeps the intrinsic reading standing
        /// beside the added one, so a declaration that could mean either would decide, by omission,
        /// whether a petrified hero is still a hero.
        /// </summary>
        EffectiveTypeOverlay = 6,

        /// <summary>
        /// Milestone 8.19c1. The affected object's removal may be stopped, at a price, by the hero
        /// whose source declared it - rulebook p.13's own example of the family, "Bolster", which that
        /// page names while stopping short of printing it.
        ///
        /// It is a seventh family rather than a qualified <see cref="Immunity"/> because the two are
        /// different rules. p.18's immunity is unconditional and answers before anything happens: an
        /// immune unit is not a legal target at all. This one is a decision taken after a Defeat has
        /// been recorded and its reward awarded, by somebody who pays for it, and declining it is a
        /// legal answer - so a declaration folded into immunity would have made the unit untargetable
        /// and deleted the Defeat the card is meant to keep.
        /// </summary>
        RemovalPrevention = 7
    }

    /// <summary>What a source charges for stopping one removal, as definition content.</summary>
    public enum RemovalPreventionCostKind
    {
        None = 0,

        /// <summary>
        /// One card the deciding hero owns, discarded from a declared location. Brogan's Shield,
        /// Bolster and Fortify are the roster's shape of it; the card is the price and the discard is
        /// what makes the decision cost something rather than being taken every time.
        /// </summary>
        DiscardOwnedCard = 1
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
        Minion = 2,

        /// <summary>
        /// Milestone 8.19c. Rulebook p.18's terrain, which that page lists beside units and tokens as
        /// an obstacle. It is not a unit, and it is here rather than in a vocabulary of its own because
        /// a card that says an object "counts as" it is asking exactly the question this enum answers:
        /// what does a type-based rule find when it looks at this object. Only
        /// <see cref="RuleModificationKind.EffectiveTypeOverlay"/> may supply it - a replacement that
        /// left a hero counting as terrain alone would delete the hero from its own team.
        /// </summary>
        Terrain = 3
    }

    /// <summary>
    /// Which action a source-declared value modifier reaches, as definition content.
    ///
    /// Milestone 8.19c. Rulebook p.9 gives a printed value one meaning per card, so a modifier that
    /// reaches every value the affected hero ever has is the ordinary reading and is what every
    /// declaration before this one meant. Sabina's Big Sodding Gun is the first verified source that
    /// qualifies it - "Your basic attack has +2 Range and +2 Attack" - and the qualification is about
    /// the action being performed rather than about the hero, so it cannot be resolved when the live
    /// set is derived. The consumer that holds the action applies it; a consumer that holds none sees
    /// only the unqualified declarations, which is the honest answer rather than an omission.
    /// </summary>
    public enum RuleValueApplicability
    {
        /// <summary>Every value on that channel, which is what an unqualified declaration means.</summary>
        Always = 0,

        /// <summary>
        /// Only an action that is both p.9's Basic supertype and an Attack, performed by the affected
        /// hero. Two conditions rather than one because the page prints two words.
        /// </summary>
        ControllerBasicAttackAction = 1
    }

    /// <summary>
    /// The derived count that scales a source-declared value modifier, as definition content.
    ///
    /// Milestone 8.19c. Xargatha's Metamorphosis is the roster's one such source - "Gain +1 Movement
    /// and +1 Initiative for each enemy unit adjacent to you" - and the count moves while the effect
    /// stands still, so the amount cannot be written into the declaration. The query is named narrowly
    /// on purpose: one printed shape needs it, and a general counting language would be a vocabulary
    /// no card has asked for.
    /// </summary>
    public enum SourceValueQueryKind
    {
        None = 0,

        /// <summary>
        /// Rulebook p.19's units on the team opposing the source's controller, standing adjacent to
        /// the affected object. Counted from live occupancy at the moment the set is derived, which is
        /// what "for each ... adjacent to you" means on a card that names no timing.
        /// </summary>
        EnemyUnitsAdjacentToAffectedObject = 1
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
            : this(
                kind,
                activity == RuleActivity.None
                    ? Array.Empty<RuleActivity>()
                    : new[] { activity },
                permission,
                channel,
                amount,
                effectiveUnitKind,
                restrictedActors,
                RuleValueApplicability.Always,
                SourceValueQueryKind.None)
        {
        }

        private RuleModification(
            RuleModificationKind kind,
            IReadOnlyList<RuleActivity> activities,
            RulePermissionKind permission,
            PrintedValueChannel channel,
            int amount,
            EffectiveUnitKind effectiveUnitKind,
            RestrictedActorScope restrictedActors,
            RuleValueApplicability valueApplicability,
            SourceValueQueryKind derivedQuery,
            RemovalPreventionCost removalPreventionCost = default)
        {
            Kind = kind;
            Activities = activities;
            Permission = permission;
            Channel = channel;
            Amount = amount;
            EffectiveUnitKind = effectiveUnitKind;
            RestrictedActors = restrictedActors;
            ValueApplicability = valueApplicability;
            DerivedQuery = derivedQuery;
            RemovalPreventionCost = removalPreventionCost;
        }

        /// <summary>A typed contribution of <paramref name="amount"/> to one value channel.</summary>
        public static RuleModification ForValue(PrintedValueChannel channel, int amount) =>
            ForValue(channel, amount, RuleValueApplicability.Always);

        /// <summary>
        /// Milestone 8.19c's `modify-value@2`: the same contribution, qualified to the action it
        /// reaches. The qualification is required rather than defaulted for the reason milestone 6.8f
        /// required the modification set - a qualification dropped by omission silently widens "your
        /// basic attack" into every value the hero has - and
        /// <see cref="RuleValueApplicability.Always"/> is where a declaration that qualifies nothing
        /// says so. The overload above is that statement written once for every existing declaration.
        /// </summary>
        public static RuleModification ForValue(
            PrintedValueChannel channel, int amount, RuleValueApplicability applicability)
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
                Array.Empty<RuleActivity>(),
                RulePermissionKind.None,
                channel,
                amount,
                EffectiveUnitKind.None,
                RestrictedActorScope.AnyActor,
                RequireApplicability(applicability),
                SourceValueQueryKind.None);
        }

        /// <summary>
        /// Milestone 8.19c's `modify-value@2` in its counted form: <paramref name="amountPerMatch"/>
        /// for every fact <paramref name="query"/> finds, resolved against live state every time the
        /// live set is derived.
        ///
        /// The amount is stated per match rather than as a total because the total is not a property of
        /// the card: Xargatha's Metamorphosis prints "+1 ... for each enemy unit adjacent to you", and
        /// the enemy units walk in and out while the ability stands unchanged. A zero count therefore
        /// contributes no modification at all rather than a zero one, which is the same statement
        /// <see cref="ForValue(PrintedValueChannel,int)"/> makes by refusing a zero amount.
        /// </summary>
        public static RuleModification ForDerivedValue(
            PrintedValueChannel channel,
            SourceValueQueryKind query,
            int amountPerMatch,
            RuleValueApplicability applicability)
        {
            if (channel == PrintedValueChannel.None)
            {
                throw new ArgumentException("A value modifier requires a channel.", nameof(channel));
            }

            if (query == SourceValueQueryKind.None ||
                !Enum.IsDefined(typeof(SourceValueQueryKind), query))
            {
                throw new ArgumentException(
                    "A derived value modifier requires the count it scales by.", nameof(query));
            }

            if (amountPerMatch == 0)
            {
                throw new ArgumentException(
                    "A derived value modifier contributes a nonzero amount per match; a zero " +
                    "contribution is not a rule.",
                    nameof(amountPerMatch));
            }

            return new RuleModification(
                RuleModificationKind.ValueModifier,
                Array.Empty<RuleActivity>(),
                RulePermissionKind.None,
                channel,
                amountPerMatch,
                EffectiveUnitKind.None,
                RestrictedActorScope.AnyActor,
                RequireApplicability(applicability),
                query);
        }

        /// <summary>
        /// This derived declaration with its count already taken. The result is an ordinary value
        /// modifier, so nothing downstream has to know a count was ever involved.
        /// </summary>
        internal RuleModification WithResolvedAmount(int matches)
        {
            if (!IsDerived)
            {
                throw new InvalidOperationException(
                    "Only a derived value modifier has a count to resolve.");
            }

            if (matches < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(matches), matches, "A derived count is never negative.");
            }

            return ForValue(Channel, checked(Amount * matches), ValueApplicability);
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

        /// <summary>
        /// Milestone 8.19c's `immunity@2`: one immunity that names several activities and the actors it
        /// protects against - Brogan's Bulwark, "You and friendly units in radius cannot be moved,
        /// pushed, swapped or placed by enemy heroes".
        ///
        /// <para>Both additions answer a question the one-activity form decided in silence. Four
        /// separate declarations would be four rules that could be cancelled, ordered, and explained
        /// independently, while the card prints one sentence; and an actor scope carried by
        /// restrictions but not by immunities meant the same qualification survived on one family and
        /// was dropped from the other, which is how "by enemy heroes" becomes "by anyone" - including
        /// the hero's own teammates, whom the card leaves free to reposition the units it protects.</para>
        ///
        /// <para>p.18's fixed exception still wins over the declaration: "You are never immune to your
        /// own actions" is a rule, not a scope, so no declaration can name the affected object's own
        /// actions back into immunity. <see cref="ImmunityQuery"/> is the one asker of the directed
        /// question, exactly as <see cref="RestrictionQuery"/> is for restrictions.</para>
        /// </summary>
        public static RuleModification ForImmunity(
            IEnumerable<RuleActivity> activities, RestrictedActorScope fromActors)
        {
            var named = (activities ?? throw new ArgumentNullException(nameof(activities))).ToArray();
            if (named.Length == 0)
            {
                throw new ArgumentException(
                    "An immunity names at least one activity; state RuleActivity.All when it names " +
                    "every one.",
                    nameof(activities));
            }

            if (named.Any(activity => activity == RuleActivity.None) ||
                named.Any(activity => !Enum.IsDefined(typeof(RuleActivity), activity)))
            {
                throw new ArgumentException(
                    "An immunity names defined activities.", nameof(activities));
            }

            if (named.Distinct().Count() != named.Length)
            {
                throw new ArgumentException(
                    "An immunity names each activity once.", nameof(activities));
            }

            if (named.Length > 1 && named.Contains(RuleActivity.All))
            {
                throw new ArgumentException(
                    "RuleActivity.All already reaches every activity, so naming it beside another " +
                    "would state a narrowing the declaration does not make.",
                    nameof(activities));
            }

            return new RuleModification(
                RuleModificationKind.Immunity,
                new ReadOnlyCollection<RuleActivity>(named),
                RulePermissionKind.None,
                PrintedValueChannel.None,
                0,
                EffectiveUnitKind.None,
                fromActors ?? throw new ArgumentNullException(nameof(fromActors)),
                RuleValueApplicability.Always,
                SourceValueQueryKind.None);
        }

        /// <summary>
        /// Milestone 8.19c's `effective-type-overlay@1`: the affected object counts as
        /// <paramref name="addedEffectiveUnitKind"/> as well as whatever it already counts as.
        ///
        /// Intrinsic identity, occupancy, ownership and supply are untouched, for the reason p.17's
        /// replacement leaves them untouched: this is an answer a type-based rule gets, not a change to
        /// the miniature on the table. The overlay lasts exactly as long as the source supplying it,
        /// which is the whole of what a card's "Next turn:" keyword means.
        /// </summary>
        public static RuleModification ForEffectiveTypeOverlay(EffectiveUnitKind addedEffectiveUnitKind)
        {
            if (addedEffectiveUnitKind == EffectiveUnitKind.None)
            {
                throw new ArgumentException(
                    "An effective-type overlay requires the type it adds.", nameof(addedEffectiveUnitKind));
            }

            if (!Enum.IsDefined(typeof(EffectiveUnitKind), addedEffectiveUnitKind))
            {
                throw new ArgumentException(
                    "An effective-type overlay adds a defined type.", nameof(addedEffectiveUnitKind));
            }

            return new RuleModification(
                RuleModificationKind.EffectiveTypeOverlay,
                Array.Empty<RuleActivity>(),
                RulePermissionKind.None,
                PrintedValueChannel.None,
                0,
                addedEffectiveUnitKind,
                RestrictedActorScope.AnyActor,
                RuleValueApplicability.Always,
                SourceValueQueryKind.None);
        }

        /// <summary>
        /// Milestone 8.19c1's `before-removal-prevention@1`: the affected object's removal may be
        /// stopped by the hero controlling this source, for <paramref name="cost"/>.
        ///
        /// <para>The declaration carries the price and nothing else. Who decides is the source's
        /// controller, which <see cref="AppliedRuleModification"/> already binds; which objects are
        /// covered is the source's own affected scope, so "friendly minions in radius" is stated once,
        /// by the effect, and moves with the disk exactly as every other radius-scoped rule does. What
        /// the prevention <em>does</em> is p.13's, not this declaration's: the Defeat and its reward
        /// stand, and the miniature stays on the board.</para>
        ///
        /// <para>A free prevention is refused. A source that stops a removal for nothing is an
        /// immunity, and p.18 already has one; declaring it here would give a card two spellings of the
        /// same rule with different answers to "may this unit be targeted".</para>
        /// </summary>
        public static RuleModification ForRemovalPrevention(RemovalPreventionCost cost)
        {
            if (!cost.IsDeclared)
            {
                throw new ArgumentException(
                    "A removal prevention states what it costs; a prevention that costs nothing is " +
                    "rulebook p.18's immunity, which is declared as an immunity.",
                    nameof(cost));
            }

            return new RuleModification(
                RuleModificationKind.RemovalPrevention,
                Array.Empty<RuleActivity>(),
                RulePermissionKind.None,
                PrintedValueChannel.None,
                0,
                EffectiveUnitKind.None,
                RestrictedActorScope.AnyActor,
                RuleValueApplicability.Always,
                SourceValueQueryKind.None,
                cost);
        }

        public static RuleModification ForCountsAs(EffectiveUnitKind effectiveUnitKind)
        {
            if (effectiveUnitKind == EffectiveUnitKind.None)
            {
                throw new ArgumentException(
                    "A Counts As modification requires the effective type it supplies.", nameof(effectiveUnitKind));
            }

            // Milestone 8.19c. p.17 replaces "all rules and qualities" with the named type's, and p.18's
            // terrain has none a unit could be run by - it is a property of a space rather than an
            // object with a side, a controller, or an action. A replacement naming it would take a hero
            // off its own team; the overlay family says the only thing the page lets a card say here.
            if (effectiveUnitKind == EffectiveUnitKind.Terrain)
            {
                throw new ArgumentException(
                    "Terrain is added by an effective-type overlay, never by a p.17 replacement.",
                    nameof(effectiveUnitKind));
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

        /// <summary>
        /// The named activity for a restriction or a single-activity immunity; otherwise <c>None</c>.
        ///
        /// Milestone 8.19c makes it derived rather than stored, and makes it report <c>None</c> for a
        /// declaration that names several. Brogan's Bulwark is the first verified text with more than
        /// one - "cannot be moved, pushed, swapped or placed by enemy heroes" - and a property that
        /// answered with the first of four would be a reader's invitation to act on a quarter of the
        /// card. <see cref="Activities"/> and <see cref="Covers"/> are what a multi-activity aware
        /// consumer asks.
        /// </summary>
        public RuleActivity Activity =>
            Activities.Count == 1 ? Activities[0] : RuleActivity.None;

        /// <summary>
        /// Every activity this declaration names, in the order the card prints them; empty for the
        /// families that name none.
        /// </summary>
        public IReadOnlyList<RuleActivity> Activities { get; }

        /// <summary>
        /// Milestone 8.19c. Which action a value modifier reaches.
        /// <see cref="RuleValueApplicability.Always"/> on every other family, because the question is
        /// not asked of them.
        /// </summary>
        public RuleValueApplicability ValueApplicability { get; }

        /// <summary>
        /// Milestone 8.19c. The derived count that scales this value modifier, or
        /// <see cref="SourceValueQueryKind.None"/> when <see cref="Amount"/> is the whole of it.
        /// A declaration carrying one states its amount <em>per match</em>;
        /// <see cref="RuleModificationQuery"/> resolves it against live state and no consumer ever
        /// sees an unresolved one.
        /// </summary>
        public SourceValueQueryKind DerivedQuery { get; }

        /// <summary>Whether this declaration's amount still has to be counted from live state.</summary>
        public bool IsDerived => DerivedQuery != SourceValueQueryKind.None;

        /// <summary>
        /// Milestone 8.19c1. What a removal prevention charges;
        /// <see cref="Modifications.RemovalPreventionCost.Free"/> on every other family, because the
        /// question is not asked of them.
        /// </summary>
        public RemovalPreventionCost RemovalPreventionCost { get; }

        /// <summary>The exact grant for a permission; otherwise <c>None</c>.</summary>
        public RulePermissionKind Permission { get; }

        /// <summary>The value channel for a value modifier; otherwise <c>None</c>.</summary>
        public PrintedValueChannel Channel { get; }

        /// <summary>The contributed amount for a value modifier; otherwise zero.</summary>
        public int Amount { get; }

        /// <summary>The supplied effective type for a <c>Counts As</c>; otherwise <c>None</c>.</summary>
        public EffectiveUnitKind EffectiveUnitKind { get; }

        /// <summary>
        /// Which actors a restriction bars, and - since milestone 8.19c's `immunity@2` - which actors
        /// an immunity protects against. <see cref="RestrictedActorScope.AnyActor"/> on every other
        /// kind, because the question is not asked of them: a value modifier, a permission and a
        /// <c>Counts As</c> are about the affected object rather than about who is acting on it.
        ///
        /// p.18's one fixed actor rule for immunity - "you are never immune to your own actions" -
        /// remains a rule rather than a declaration, so it is applied before this scope is read and no
        /// scope can name it away.
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

            for (var index = 0; index < Activities.Count; index++)
            {
                if (Activities[index] == RuleActivity.All || Activities[index] == activity)
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            var named = string.Join(", ", Activities);
            switch (Kind)
            {
                case RuleModificationKind.ValueModifier:
                    var amount = $"{Channel} {(Amount > 0 ? "+" : string.Empty)}{Amount}";
                    if (IsDerived) amount += $" per {DerivedQuery}";
                    return ValueApplicability == RuleValueApplicability.Always
                        ? amount
                        : $"{amount} on {ValueApplicability}";
                case RuleModificationKind.Restriction:
                    return RestrictedActors.IsUnqualified
                        ? $"Cannot {named}"
                        : $"Cannot {named} {RestrictedActors}";
                case RuleModificationKind.Permission:
                    return $"May {Permission}";
                case RuleModificationKind.Immunity:
                    return RestrictedActors.IsUnqualified
                        ? $"Immune to {named}"
                        : $"Immune to {named} {RestrictedActors}";
                case RuleModificationKind.CountsAs:
                    return $"Counts as {EffectiveUnitKind}";
                case RuleModificationKind.EffectiveTypeOverlay:
                    return $"Also counts as {EffectiveUnitKind}";
                default:
                    return "Undeclared modification";
            }
        }

        private static RuleValueApplicability RequireApplicability(RuleValueApplicability applicability)
        {
            if (!Enum.IsDefined(typeof(RuleValueApplicability), applicability))
            {
                throw new ArgumentException(
                    "A value modifier states a defined applicability.", nameof(applicability));
            }

            return applicability;
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
