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
        RemovalPrevention = 7,

        /// <summary>
        /// Milestone 8.20. The affected object may still perform the named activity, but not beyond a
        /// stated size - Arien's Slippery Ground and Deluge, "cannot ... move more than 1 space with a
        /// movement action".
        ///
        /// <para>It is an eighth family rather than a <see cref="Restriction"/> or a negative
        /// <see cref="ValueModifier"/>, because it is neither. A restriction takes the action away, and
        /// the affected hero here still has one - the whole difference between Slippery Ground and a
        /// card that said "cannot move" is that a pinned hero may still take one step. And a value
        /// modifier is a delta the page composes into a total, so "-2 Movement" would bite a Movement 3
        /// card and a Movement 6 card by different amounts while the sentence names one number for
        /// both; p.16's items would then raise the very total the sentence caps.</para>
        ///
        /// <para>A cap is therefore applied after the total is composed, and several caps take the
        /// smallest: two sources that each said "no more than N" are both obeyed only by the tighter,
        /// and a reading that let the looser win would let one card cancel another's sentence without
        /// naming it.</para>
        /// </summary>
        ActivityLimit = 8,

        /// <summary>
        /// Milestone 8.35z28. While the affected object is performing an action, every space on
        /// the board counts as being in the Battle Zone and as carrying a friendly minion Spawn point -
        /// Dodger the Warlock's Ultimate, Tide of Darkness, printed whole: "While you are performing an
        /// action, all spaces count as if they were in the battle zone and had a friendly minion spawn
        /// point."
        ///
        /// <para><b>The whole rule is in the member's name, and that is deliberate.</b> Every other
        /// family here is parameterised by a property this type already carries - an activity, a
        /// channel, an amount, an effective kind. This one names two grants at once and neither is
        /// expressible in any of them, so the alternative was a new public property. That is precisely
        /// the change the identity oracle treats as a content-revision event for every shipped hero
        /// whose definitions reach this type, which is most of the roster; the phase-one worker
        /// measured that cost on a different type and took the same way out. The rule is one printed
        /// sentence with no numbers in it, so a member that says the sentence loses nothing.</para>
        ///
        /// <para><b>It is not <see cref="CountsAs"/> and not <see cref="EffectiveTypeOverlay"/>.</b>
        /// Both of those replace or add the rules and qualities of a unit <em>type</em> to an object.
        /// This says nothing about the object it is declared on: it changes what the <em>board</em>
        /// counts as while that object acts, which is a statement about spaces. p.19's Spawn Point and
        /// Battle Zone are properties of spaces, and no family here had ever spoken about one.</para>
        ///
        /// <para><b>"While you are performing an action" needs no separate scope</b>, because the
        /// questions it widens - <see cref="EmptyMinionSpawnPointQuery"/>'s three parts - are only ever
        /// asked about an acting hero, inside the resolution of the action they are asked for. The
        /// user's 2026-09-06 ruling puts a Defense action squarely inside "an action" (p.14 says
        /// "perform a Defense action"), so the reading is the plain one and the scope falls out of
        /// where the query is consulted rather than being restated as a condition.</para>
        /// </summary>
        AllSpacesCountAsFriendlyMinionSpawnPointsInBattleZone = 9,

        /// <summary>
        /// Milestone 8.35z29. How many options the affected hero may take from a modal on a card
        /// she is resolving - Misa's Power Overwhelming, "Whenever you choose one, you may choose two
        /// different options instead, in any order".
        ///
        /// <para><b>It is a tenth family because it is the first modification of a control-flow node
        /// rather than of an object's values, permissions or characteristics.</b> The kinds above
        /// are all answers to "what is true of this piece": what its Attack Total is, what it may do,
        /// what may be done to it, what it counts as. This one answers "how many bullets does this
        /// decision admit", which is a property of a question the rules are about to ask and of nothing
        /// standing on the board. Folding it into <see cref="ValueModifier"/> would have made it a
        /// contribution to a printed channel, and a modal's cardinality is not printed anywhere -
        /// p.17's glossary states it in words ("Choose a bullet point (or bullet points) and apply that
        /// text, ignoring the rest") and p.10 adds the ordering.</para>
        ///
        /// <para><b>The number is <see cref="RuleModification.Amount"/> and there is no new field.</b>
        /// A declaration that carried its own property would move every hero already referencing this
        /// type through the content-identity oracle, which walks public properties - the trap milestone
        /// 8.35z28 recorded on Dodger. The amount is what the sentence prints: two.</para>
        ///
        /// <para><b>What it does not do.</b> It raises the ceiling and never the floor, because the
        /// card says "you <em>may</em>" - so one bullet stays a legal answer - and it never reaches a
        /// modal offering fewer bullets than it admits, because "two <em>different</em> options" cannot
        /// be satisfied from a list of one. On the two-bullet modal Misa actually prints, admitting two
        /// means admitting both, which is the user's 2026-09-06 ruling in as many words.</para>
        /// </summary>
        ChoiceCardinality = 10,

        /// <summary>
        /// Milestone 8.35z31. A hero defeated by the affected object spends one Life counter
        /// more than rulebook p.13's schedule charges - Ursafar's Tear, "if you defeated a hero, that
        /// hero spends 1 additional [Life] Life counter".
        ///
        /// <para>It is an eleventh family rather than a <see cref="ValueModifier"/> because a Life counter
        /// is not one of p.9's printed value channels and never becomes one: p.13's cost is read off
        /// the <em>defeated</em> hero's lowest card Tier, so it is not a value the attacker's card
        /// prints, has no icon, and no p.16 item could match it. A channel invented to carry it would
        /// be a printed value nothing prints.</para>
        ///
        /// <para><b>It carries no amount, and the omission is the printed reading.</b> The user's 2026-
        /// 09-06 ruling settles the arithmetic: the cost is computed <b>once</b> as Tier + 1 and spent
        /// once, at the defeat - the card's "After the attack:" placement says when the condition is
        /// checked, not when the counter is paid. One printed face says "1 additional", so the family
        /// says one; two declarations therefore charge two, which is the only reading that keeps a
        /// second source from being silently swallowed. A face printing a different number widens this
        /// to an amount then, on the standing rule that this project does not build a reading no
        /// content prints - the same reason <c>RemovalPreventionCostKind</c> has exactly one
        /// member.</para>
        ///
        /// <para>It is declared about the <em>attacker</em>, which is what "you defeated a hero" names,
        /// and read at the moment p.13's spend is computed. That is why Tear declares it before its
        /// Attack rather than in the after-the-attack clause it is printed in: by step 7 the defeat has
        /// already been paid for, and a second payment there would be the two-spend reading the ruling
        /// rejects.</para>
        /// </summary>
        AdditionalDefeatLifeCost = 11
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
        DiscardOwnedCard = 1,

        /// <summary>
        /// The token whose property declared the prevention, removed instead of the object it was
        /// protecting - Tali's <c>If a friendly minion adjacent to a totem would be defeated, remove
        /// the totem instead.</c> Milestone 8.35z32.
        ///
        /// <para><b>It is a second cost kind AND a second shape, and the second half matters more.</b>
        /// Brogan's family is a <em>decision</em>: a hero is asked, may decline, and pays with a card
        /// from a hand that moves - which is why <see cref="RemovalPreventionQuery"/> derives the
        /// payable cards live and drops an option nobody can afford. Tali's sentence carries none of
        /// p.10's three softeners - no "you may", no "up to", no "if able" - so it is mandatory, there
        /// is nobody to ask, and the price is a specific piece already on the board rather than a
        /// choice among cards. A mandatory prevention offered as a decision would be a question with
        /// one legal answer, and a decision-shaped one dropped for having no payable <em>cards</em>
        /// would silently never fire at all.</para>
        ///
        /// <para>So the two are read apart: <see cref="RemovalPreventionQuery.Offered"/> answers for
        /// the decision family and <see cref="RemovalPreventionQuery.Automatic"/> for this one.</para>
        /// </summary>
        RemoveTheSourceToken = 2
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
        ControllerBasicAttackAction = 1,

        /// <summary>
        /// Milestone 8.35z33. Any action that is p.9's Basic supertype, performed by the affected hero -
        /// one condition where <see cref="ControllerBasicAttackAction"/> has two, because Whisper's
        /// Grim Reaper prints one word where Wasp's face prints two: "When performing basic
        /// <em>actions</em>", with no action type named.
        ///
        /// <para>p.9 settles what that covers: "All actions on a basic card are basic actions." So it
        /// reaches Swift Justice's Attack and Death Seeker's Skill alike, and it reaches neither
        /// through anything about their types - which is why widening the member above by dropping its
        /// Attack condition would have been the wrong shape. Both members exist because two cards
        /// print two different sentences.</para>
        /// </summary>
        ControllerBasicAction = 2
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
            RemovalPreventionCost removalPreventionCost = default,
            RestrictedSourceCardScope restrictedSourceCards = null)
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
            RestrictedSourceCards = restrictedSourceCards ?? RestrictedSourceCardScope.EveryCard;
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

        /// <summary>
        /// Milestone 8.20's `restrict-action@2`. A restriction that also names the cards it leaves
        /// alone - Arien's Spell Break, "cannot perform Skill actions, except on the gold cards".
        ///
        /// <para>The carve-out is required rather than defaulted, for the reason milestone 8.7 required
        /// the actor scope: an exception dropped by omission silently widens the card into a blanket
        /// prohibition, and <see cref="RestrictedSourceCardScope.EveryCard"/> is where a declaration
        /// that excepts nothing says so. The two overloads above are that statement written once for
        /// every restriction declared before this one.</para>
        ///
        /// <para>The two scopes are independent and both must be met for the activity to be barred:
        /// Spell Break names every enemy hero in the disk and excepts each of their Gold cards, so a
        /// hero it reaches may still perform the Basic Skill on Gold while their Silver one is shut
        /// down. A consumer that holds no card asks with <see cref="PrintedValueChannel"/>'s own kind
        /// of absence - see <see cref="RestrictedSourceCardScope.Excepts"/>.</para>
        /// </summary>
        public static RuleModification ForRestriction(
            RuleActivity activity,
            RestrictedActorScope restrictedActors,
            RestrictedSourceCardScope restrictedSourceCards) =>
            new RuleModification(
                RuleModificationKind.Restriction,
                new ReadOnlyCollection<RuleActivity>(
                    new[] { RequireActivity(activity, "restriction") }),
                RulePermissionKind.None,
                PrintedValueChannel.None,
                0,
                EffectiveUnitKind.None,
                restrictedActors ?? throw new ArgumentNullException(nameof(restrictedActors)),
                RuleValueApplicability.Always,
                SourceValueQueryKind.None,
                default,
                restrictedSourceCards ?? throw new ArgumentNullException(nameof(restrictedSourceCards)));

        /// <summary>
        /// Milestone 8.20's `limit-activity@1`. The affected object may perform
        /// <paramref name="activity"/>, but no further than <paramref name="maximum"/> - Arien's
        /// Slippery Ground and Deluge, "cannot ... move more than 1 space with a movement action".
        ///
        /// <para>A maximum of zero is refused. It would say "may act, but not at all", which is a
        /// prohibition written in the wrong family - <see cref="ForRestriction(RuleActivity)"/> is
        /// where a card that takes the action away says so, and the two must not have a value in
        /// common or a reader could not tell which rule an effect declared.</para>
        /// </summary>
        public static RuleModification ForActivityLimit(RuleActivity activity, int maximum)
        {
            if (maximum < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximum),
                    maximum,
                    "An activity limit leaves the activity available; a limit of zero is a " +
                    "restriction and is declared as one.");
            }

            return new RuleModification(
                RuleModificationKind.ActivityLimit,
                RequireActivity(activity, "activity limit"),
                RulePermissionKind.None,
                PrintedValueChannel.None,
                maximum,
                EffectiveUnitKind.None,
                RestrictedActorScope.AnyActor);
        }

        /// <summary>
        /// Milestones 8.35z29 and 8.35z33. How many options a modal admits while this source stands -
        /// Misa's Power Overwhelming, "Whenever you choose one, you may choose two different options
        /// instead", and Whisper's Grim Reaper, "When performing basic actions you may choose one, or
        /// both."
        ///
        /// <para>Two or more, because one is the rule already: a declaration of one would say nothing
        /// and a declaration of zero would be a restriction wearing this kind's name. "or both" on a
        /// two-bullet modal is two, and a card printing a different number would declare that number
        /// here rather than need a different kind.</para>
        /// </summary>
        /// <param name="applicability">
        /// Which of the affected hero's actions the widening reaches.
        /// <see cref="RuleValueApplicability.Always"/> is an unqualified declaration - a source whose
        /// sentence names no class of action - and Grim Reaper's own is
        /// <see cref="RuleValueApplicability.ControllerBasicAction"/>, because the face prints "when
        /// performing basic actions" and p.9 makes every action on a basic card one of those.
        /// </param>
        public static RuleModification ForChoiceCardinality(
            int options,
            RuleValueApplicability applicability = RuleValueApplicability.Always)
        {
            if (options < 2)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(options),
                    options,
                    "A choice-cardinality modification raises how many options a modal admits; " +
                    "p.10's own reading already admits one, so a declaration of one or fewer is " +
                    "either nothing or a restriction, and a restriction is declared as one.");
            }

            return new RuleModification(
                RuleModificationKind.ChoiceCardinality,
                Array.Empty<RuleActivity>(),
                RulePermissionKind.None,
                PrintedValueChannel.None,
                options,
                EffectiveUnitKind.None,
                RestrictedActorScope.AnyActor,
                applicability,
                SourceValueQueryKind.None);
        }

        /// <param name="whilePerforming">
        /// Milestone 8.35z30, and RULING 47. The activity the grant is scoped to, or
        /// <see cref="RuleActivity.None"/> for a grant its card states unconditionally.
        ///
        /// <para>Two verified faces print the same permission and only one of them qualifies it.
        /// Xargatha's Metamorphosis says "You may move through obstacles" and names nothing, so it
        /// holds whenever anything asks. Silverarrow's Trailblazer says "may ignore obstacles
        /// <b>while performing movement actions</b>", and the user's ruling is that a Move written in
        /// card text is not a movement action - "something might block movement actions. But this is
        /// moving as part of attack actions, so it does not care" - which cuts both ways: a
        /// movement-action buff does not reach a card-text Move either. Declaring the scope is what
        /// keeps the two sentences different, and an unqualified grant answers every ask exactly as it
        /// did before this parameter existed.</para>
        /// </param>
        public static RuleModification ForPermission(
            RulePermissionKind permission, RuleActivity whilePerforming = RuleActivity.None)
        {
            if (permission == RulePermissionKind.None)
            {
                throw new ArgumentException("A permission requires the exact grant it gives.", nameof(permission));
            }

            return new RuleModification(
                RuleModificationKind.Permission,
                whilePerforming,
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
        /// <summary>
        /// Milestone 8.35z31. One extra Life counter charged for a hero this object defeats.
        /// See <see cref="RuleModificationKind.AdditionalDefeatLifeCost"/> for why it carries nothing.
        /// </summary>
        public static RuleModification ForAdditionalDefeatLifeCost() =>
            new RuleModification(
                RuleModificationKind.AdditionalDefeatLifeCost,
                Array.Empty<RuleActivity>(),
                RulePermissionKind.None,
                PrintedValueChannel.None,
                0,
                EffectiveUnitKind.None,
                RestrictedActorScope.AnyActor,
                RuleValueApplicability.Always,
                SourceValueQueryKind.None);

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

        /// <summary>
        /// Dodger the Warlock's Tide of Darkness, whole. Milestone 8.35z28.
        ///
        /// <para>It takes no argument because the printed sentence has no number, no channel and no
        /// named type in it - see
        /// <see cref="RuleModificationKind.AllSpacesCountAsFriendlyMinionSpawnPointsInBattleZone"/> for
        /// why the rule is stated in the member's name rather than in a property.</para>
        /// </summary>
        public static RuleModification ForAllSpacesCountAsFriendlyMinionSpawnPointsInBattleZone() =>
            new RuleModification(
                RuleModificationKind.AllSpacesCountAsFriendlyMinionSpawnPointsInBattleZone,
                RuleActivity.None,
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
        /// Milestone 8.20. Which cards a restriction leaves alone -
        /// <see cref="RestrictedSourceCardScope.EveryCard"/> on every other kind and on every
        /// restriction that carves nothing out, because the question is not asked of them.
        ///
        /// It is read only by the consumers that hold the card an action would be performed from. One
        /// that holds none - rulebook p.11-p.13's cardless Hold, Clear and Fast Travel - is answered by
        /// the scope itself rather than by omitting the check, so a cardless action cannot slip a
        /// prohibition that names a colour.
        /// </summary>
        public RestrictedSourceCardScope RestrictedSourceCards { get; }

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
                    var barred = RestrictedActors.IsUnqualified
                        ? $"Cannot {named}"
                        : $"Cannot {named} {RestrictedActors}";
                    return RestrictedSourceCards.IsUnqualified
                        ? barred
                        : $"{barred}, {RestrictedSourceCards}";
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
                case RuleModificationKind.ActivityLimit:
                    return $"No more than {Amount} with a {named} action";
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
