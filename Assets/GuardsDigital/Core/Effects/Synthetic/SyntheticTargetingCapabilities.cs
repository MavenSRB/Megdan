using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 6.8k's target-selection capability: an effect definition's typed way to name another
    /// board object.
    ///
    /// It is an ordinary registered capability rather than a new node kind because the acceptance text
    /// requires the chosen space to reach a later node through the existing node-output binding - the
    /// selection pauses through the same capability-choice seam every other chooser-owned decision uses,
    /// and publishes one <see cref="EffectValueKind.Coordinate"/> output. The coordinate is the subject
    /// identity milestone 6.4 chose for displacement: one obstacle per space makes the object standing
    /// there nameable without widening the typed value union, which is how a token - unrepresentable as
    /// a rules object by p.19 - stays expressible.
    ///
    /// Its candidate set comes from <see cref="TargetQuery"/>, the reusable targeting contract, rather
    /// than effect-local geometry. Immunity therefore filters selection - p.18 makes it a targeting
    /// rule - while a presence question over the same spaces still counts the immune unit. The activity
    /// consulted is declared by revision 4. Revision 3 is bound to Skill; definitions feeding a
    /// consuming effect with its own activity use revision 4 so the offer and final operation ask the
    /// identical immunity question.
    ///
    /// Milestone 6.8l replaced revisions 1 and 2 with 3 and 4, which declare one further optional
    /// parameter: the node this selection selects for. A legal target and a target the consuming step
    /// can act on are different questions, and until the selection could name its consumer only the
    /// first was asked - so a branch behind one was offered whatever the step after it would do. The
    /// revisions moved rather than being joined because the registration list records contracts, the
    /// same reason milestone 6.8k retired direct Move revision 1 when it widened the subject.
    ///
    /// This is the seam 6.5b and 6.6a reuse: Red Needle's Push, Red Bombard's direct Move of another
    /// adjacent unit, and Blue Sentinel's Swap all name their object through this one selection rather
    /// than each inventing a binding.
    /// </summary>
    public static class SyntheticTargetingCapabilities
    {
        public static EffectCapabilityId ChooseTarget { get; } =
            new EffectCapabilityId("synthetic-choose-target");

        /// <summary>
        /// Revision 3, minted at milestone 6.8l: revision 1's declaration plus an optional consumer.
        /// The enclosing action's activity is what it asks immunity for.
        /// </summary>
        public static EffectCapabilityReference ChooseTargetV3 =>
            new EffectCapabilityReference(ChooseTarget, 3);

        /// <summary>
        /// Revision 4, minted at milestone 6.8l: revision 2's declaration plus an optional consumer.
        /// The activity it asks immunity for is the definition's, for an operation p.18 gives one.
        /// </summary>
        public static EffectCapabilityReference ChooseTargetV4 =>
            new EffectCapabilityReference(ChooseTarget, 4);

        /// <summary>
        /// Milestone 7.2b. The same selection bounded by rulebook p.9's printed Radius icon instead of
        /// a number the definition writes down.
        ///
        /// A second capability ID rather than a third revision of the one above, for the reason the
        /// named Push is a second ID beside the batch Push: these are two live declarations of
        /// different content, not two versions of one. Where a card's text says "choose a unit within
        /// 1 space" the range is card text and the definition states it; where the card prints a Radius
        /// icon, p.9 says "the value on the Range/Radius icon determines the maximum Range/Radius of
        /// your action. If the card text mentions Range/Radius, it refers to this value" - so the bound
        /// is printed content the capability reads, and p.16's items raise it. A card cannot hold both
        /// readings of one parameter, which is exactly the ambiguity the definition validator refuses.
        /// </summary>
        public static EffectCapabilityId RadiusTarget { get; } =
            new EffectCapabilityId("synthetic-radius-target");

        public static EffectCapabilityReference RadiusTargetV1 =>
            new EffectCapabilityReference(RadiusTarget, 1);

        /// <summary>
        /// Milestone 8.6. The printed-Range twin of <see cref="RadiusTarget"/>, for card text that says
        /// "in range" rather than "in radius".
        ///
        /// A second capability ID rather than a parameter on the one above, for the reason p.9 gives
        /// two icons two meanings and p.16 two items: a card prints one of them, and a definition that
        /// could name either through the same parameter would let a Radius card read a Range item. The
        /// handler beneath is nevertheless shared - the two differ only in which
        /// <see cref="PrintedValueChannel"/> they read - so no geometry, immunity or item composition
        /// is written twice.
        ///
        /// It declares one thing the Radius selection does not: p.19's straight-line complement, which
        /// Wasp's Telekinesis, Mass Telekinesis and both Boomerangs print as "in range, which is not in
        /// a straight line". No verified card pairs that filter with a Radius, so the Radius
        /// declaration does not carry the parameter rather than carrying one nothing sets.
        /// </summary>
        public static EffectCapabilityId RangeTarget { get; } =
            new EffectCapabilityId("synthetic-range-target");

        public static EffectCapabilityReference RangeTargetV1 =>
            new EffectCapabilityReference(RangeTarget, 1);

        /// <summary>
        /// Milestone 8.10. The printed-Radius selection asked about heroes rather than about board
        /// objects, for card text that names one: Wasp's Shock, Electrocute and Electroblast each print
        /// "An enemy hero in radius and not adjacent to you".
        ///
        /// <para>A second capability ID rather than a revision of the Radius selection above, by the
        /// same rule that made the Range twin one: these are two live declarations of different
        /// content. That one offers every board object p.19 admits and publishes the space it stands
        /// on, because what consumes it is a displacement and a token has no other name; this one
        /// offers heroes and publishes the hero, because what consumes it is a discard and a space
        /// cannot be asked for a card. The handler beneath is nevertheless the same one, exactly as it
        /// is for the Range twin - so p.9's disk, p.18's immunity and p.16's item composition are
        /// written once, and milestone 7.9's deferral gate keeps holding: this slice registers a hero's
        /// declaration on an implementation the showcase already proves, rather than introducing an
        /// implementation inside a hero slice.</para>
        ///
        /// <para>It declares two things the object selection does not, and both are printed words
        /// rather than defaults: the side of the board the hero must be on - "enemy", read from the
        /// acting hero's own perspective - and p.9's disk without its innermost ring, which is what
        /// "and not adjacent to you" removes.</para>
        /// </summary>
        public static EffectCapabilityId RadiusHeroTarget { get; } =
            new EffectCapabilityId("synthetic-radius-hero-target");

        public static EffectCapabilityReference RadiusHeroTargetV1 =>
            new EffectCapabilityReference(RadiusHeroTarget, 1);

        /// <summary>
        /// Milestone 8.11. The printed-Range twin of <see cref="RadiusHeroTarget"/>, for card text that
        /// names a hero "in range": Wasp's Deflect Projectiles and Reflect Projectiles.
        ///
        /// <para>A second capability ID rather than a parameter, by the rule that made the object
        /// selections two: p.9 gives the two icons two meanings and p.16 two items, so a definition
        /// that could name either through one parameter would let a Range card read a Radius item. The
        /// handler beneath is the same one all four printed-icon declarations share.</para>
        ///
        /// <para>It declares one thing the Radius twin does not - whether the attacker is excluded -
        /// and the Radius twin declares one this does not, p.9's disk without its innermost ring.
        /// Neither carries a parameter nothing sets: no verified card pairs "in range" with "not
        /// adjacent", and none pairs "in radius" with "other than the attacker".</para>
        /// </summary>
        public static EffectCapabilityId RangeHeroTarget { get; } =
            new EffectCapabilityId("synthetic-range-hero-target");

        public static EffectCapabilityReference RangeHeroTargetV1 =>
            new EffectCapabilityReference(RangeHeroTarget, 1);

        public static EffectCapabilityId RadiusMinionTarget { get; } =
            new EffectCapabilityId("synthetic-radius-minion-target");

        public static EffectCapabilityReference RadiusMinionTargetV1 =>
            new EffectCapabilityReference(RadiusMinionTarget, 1);

        /// <summary>
        /// Milestone 8.20. The printed-Range twin of <see cref="RadiusMinionTarget"/>, for card text
        /// that names a minion "in range": Arien's Arcane Whirlpool and Ebb and Flow, "Swap with an
        /// enemy minion in range".
        ///
        /// <para>A second capability ID rather than a parameter, by the rule the other three pairs
        /// here already record: p.9 gives the two icons two meanings and p.16 two items, so a
        /// definition that could name either through one parameter would let a Range card read a Radius
        /// item. The handler beneath is the same one all five printed-icon declarations share.</para>
        /// </summary>
        public static EffectCapabilityId RangeMinionTarget { get; } =
            new EffectCapabilityId("synthetic-range-minion-target");

        public static EffectCapabilityReference RangeMinionTargetV1 =>
            new EffectCapabilityReference(RangeMinionTarget, 1);

        /// <summary>
        /// Milestone 8.21. The printed-Range selection that admits rulebook p.19's units - a hero or a
        /// minion - for card text that says "a unit in range": Brogan's Throwing Axe and Throwing
        /// Spear, whose second bullet names the target this Attack takes.
        ///
        /// <para>A sixth declaration over the same handler rather than a parameter on one of the
        /// others, by the rule that made the hero and minion selections two: p.9 gives the two icons
        /// two meanings and p.16 two items, and the kinds a card admits is a statement its own text
        /// makes. It publishes both identities and fills in whichever was named, which is what
        /// <c>set-attack-target@1</c> takes either of.</para>
        ///
        /// <para>No card before these two chose a unit through card text at all - every earlier
        /// printed-Range Attack takes its target through the action's own targeting, where p.13
        /// already admits both kinds without anything being written down.</para>
        /// </summary>
        public static EffectCapabilityId RangeUnitTarget { get; } =
            new EffectCapabilityId("synthetic-range-unit-target");

        public static EffectCapabilityReference RangeUnitTargetV1 =>
            new EffectCapabilityReference(RangeUnitTarget, 1);

        /// <summary>
        /// Milestone 8.20. The chosen minion's current space, published beside its identity.
        ///
        /// <para>Rulebook p.19's Swap exchanges two <em>spaces</em>, and every repositioning operation
        /// in this build names its subject by the space it stands on - which p.19's one-obstacle-per-
        /// space rule makes unambiguous. So a selection whose consumer is a Swap has to publish both:
        /// the minion, because the card's sentence names one, and where it is standing, because the
        /// operation takes that. The board-object selection has published a space since milestone
        /// 7.2b; this is the same output on the minion arm rather than a second idea.</para>
        /// </summary>
        public const string ChosenMinionSpaceOutputName = "chosenMinionSpace";

        public static EffectCapabilityId HeroAdjacentToMinion { get; } =
            new EffectCapabilityId("synthetic-hero-adjacent-to-minion");

        public static EffectCapabilityReference HeroAdjacentToMinionV1 =>
            new EffectCapabilityReference(HeroAdjacentToMinion, 1);

        /// <summary>
        /// Milestone 8.35z33. The printed-Radius twin of <see cref="RangeUnitTarget"/>, for card text
        /// naming p.19's units inside p.9's Radius disk - Whisper's Seeds of Fear, Sprouts of Panic
        /// and Blooming Nightmare, "Target an enemy unit in radius occupying a spawn point."
        ///
        /// <para>A seventh declaration over the one printed-icon handler, and a separate id rather
        /// than a parameter, by exactly the rule <see cref="RangeUnitTarget"/> records: p.9 gives the
        /// two icons two meanings and p.16 two items, so a declaration that could name either would
        /// let a Radius card be widened by a Range item. Brogan minted the Range arm at 8.21 for the
        /// same reason and it is the same handler underneath, so the disk, p.18's immunity and the
        /// published pair are written once.</para>
        /// </summary>
        public static EffectCapabilityId RadiusUnitTarget { get; } =
            new EffectCapabilityId("synthetic-radius-unit-target");

        public static EffectCapabilityReference RadiusUnitTargetV1 =>
            new EffectCapabilityReference(RadiusUnitTarget, 1);

        /// <summary>
        /// Milestone 8.35z33. "occupying a spawn point" - Whisper's three Blue displacement Skills, as a
        /// narrowing on which units the disk offers.
        ///
        /// <para><b>It is deliberately not the empty-minion-spawn-point question her Red and Green
        /// lines ask.</b> Those faces print "empty <em>minion</em> spawn point in radius <em>in the
        /// battle zone</em>", which is three conditions; these three print "occupying a spawn point"
        /// and print none of them. p.19's own definition is the broader one - "Spaces on the board used
        /// to spawn minions and heroes" - so a hero standing on a hero Spawn point outside the Battle
        /// Zone is a legal target of Seeds of Fear and is not one of the points Sanguine Path counts.
        /// Reading one phrase through the other would be inventing a qualifier the face does not
        /// print.</para>
        ///
        /// <para><b>Emptiness cannot be part of it, by construction.</b> The unit this selection names
        /// is standing on the space, so the space is occupied - which is why the word "empty" appears
        /// in the other phrase and not in this one.</para>
        ///
        /// <para>Optional and defaulting to false, so Brogan's and Xargatha's unit selections keep
        /// their exact candidate sets.</para>
        /// </summary>
        public const string OccupiesSpawnPointParameterName = "occupiesSpawnPoint";

        /// <summary>
        /// The straight-line filter parameter's name, spelled once so a definition cannot mistype it.
        /// </summary>
        public const string ExcludeStraightLineParameterName = "excludeStraightLine";

        /// <summary>
        /// The adjacency filter parameter's name, spelled once so a definition cannot mistype it.
        /// Milestone 8.10.
        /// </summary>
        public const string ExcludeAdjacentParameterName = "excludeAdjacent";

        /// <summary>
        /// Whether a selection's own actor is one of its candidates. The board-object arm has always
        /// required it; milestone 8.35z30 gave the hero arm the same parameter, optional,
        /// for Silverarrow's unsided "A hero in radius" (RULING 8).
        /// </summary>
        public const string IncludeSelfParameterName = "includeSelf";

        /// <summary>
        /// The attacker-exclusion parameter's name, spelled once so a definition cannot mistype it.
        /// Milestone 8.11, for Deflect Projectiles' "other than the attacker".
        /// </summary>
        public const string ExcludeTheAttackerParameterName = "excludeTheAttacker";

        /// <summary>
        /// The side-of-the-board parameter's name, carrying a <see cref="SourceTeamRelation"/>.
        /// Milestone 8.10.
        /// </summary>
        public const string TargetRelationParameterName = "relation";

        /// <summary>The hero a printed-Radius hero selection chose.</summary>
        public const string ChosenHeroOutputName = "chosenHero";

        public const string ChosenMinionOutputName = "chosenMinion";

        public const string MinionParameterName = "minion";

        /// <summary>
        /// Milestone 8.24. Which of p.22's minion classes a printed-icon minion selection admits, as
        /// <see cref="RosterMinionClass"/>.
        ///
        /// <para>A parameter rather than a third and fourth capability id, unlike the Range/Radius
        /// split above it: that split exists because p.9 gives the two icons two meanings and p.16 two
        /// items, so one parameter spanning them would let a Range card read a Radius item. A class
        /// adjective narrows <em>within</em> one icon's meaning and reads no item at all, which is the
        /// same kind of thing <c>excludeAdjacent</c> and <c>excludeStraightLine</c> already are.</para>
        ///
        /// <para>It is optional, and an undeclared parameter admits every class - so the five minion
        /// selections composed before Xargatha's Green line are unchanged without naming it.</para>
        /// </summary>
        public const string AdmittedMinionClassParameterName = "admittedMinionClass";

        /// <summary>
        /// Milestone 8.35z28. Whether a printed-icon hero selection admits only heroes who own at least
        /// one card in their own discard pile - Dodger's Littlefinger of Death and Finger of Death,
        /// "Target a hero in range <b>who has one or more cards in the discard</b>".
        ///
        /// <para>It narrows the candidates rather than gating what happens to the chosen one, which is
        /// the reading milestone 8.21's <c>playedAttackThisTurn</c> and milestone 8.22's
        /// <c>adjacentToFriendlyMinion</c> already record for the same grammar: "who has" is part of who
        /// may be picked, so a hero with an empty pile is not one to offer and then refuse.</para>
        ///
        /// <para><b>The pile is the named hero's own.</b> "who has" binds it to the target, and p.10
        /// returns every card to its owner's hand at the end of a round - so the condition tracks heroes
        /// who have discarded during the current round, by defending or by being made to discard, and
        /// it empties again at the boundary without anything here having to know that.</para>
        ///
        /// <para>Optional and defaulting to false, so every hero selection composed before Dodger keeps
        /// its exact candidate set.</para>
        /// </summary>
        public const string HasCardInDiscardParameterName = "hasCardInDiscard";

        /// <summary>
        /// Milestone 8.35z28. Whether a printed-icon hero selection admits only heroes standing beside an
        /// empty minion Spawn point of the current Battle Zone - Dodger's Death Trap, "An enemy hero in
        /// radius <b>who is adjacent to an empty spawn point in the battle zone</b> discards a card, if
        /// able".
        ///
        /// <para>The sentence has two bounds measured from two different places, which is why this
        /// cannot be expressed by moving the selection's origin: the disk is measured from Dodger and
        /// the adjacency is measured from each candidate. That is the identical shape milestone 8.22
        /// recorded for <c>adjacentToFriendlyMinion</c>.</para>
        ///
        /// <para>Which Spawn points qualify is
        /// <see cref="GuardsDigital.Core.Rules.Modifications.EmptyMinionSpawnPointQuery"/>'s answer -
        /// minion points only, per the user's D-01 ruling, in the Battle Zone, empty - asked here rather
        /// than answered a second way, because eight other faces of hers read the identical printed
        /// phrase. Note which piece the Battle Zone is asked about: the Spawn point, never the hero.
        /// That is the designer's own 2026-07-25 ruling on Aegis of Doom, and it holds for this face for
        /// the same reason - the clause qualifies the Spawn point and nothing else.</para>
        ///
        /// <para>Optional and defaulting to false, so every hero selection composed before Dodger keeps
        /// its exact candidate set.</para>
        /// </summary>
        public const string AdjacentToEmptyMinionSpawnPointParameterName =
            "adjacentToEmptyMinionSpawnPoint";

        private static IReadOnlyList<RuleActivity> AllActivities { get; } =
            Array.AsReadOnly(Enum.GetValues(typeof(RuleActivity)).Cast<RuleActivity>()
                .Where(value => value != RuleActivity.None).ToArray());

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            RegisteredHandlers().Concat(DeferredHandlers());

        /// <summary>
        /// Milestone 8.35z33. The declarations this build does <em>not</em> register, stated as its own
        /// list for the reason <see cref="SyntheticRosterCapabilities.DeferredHandlers"/> is: the
        /// registration list is what a snapshot compares, so a capability registered before a dealt
        /// card can start it moves pinned bytes and fails milestone 7.9's reachability gate.
        ///
        /// <para>Whisper's Blue displacement Skills name <c>synthetic-radius-unit-target@1</c> and
        /// this phase does not deal her, so it waits here for her completion milestone. It is a
        /// declaration over a handler this build already registers six times, not a new handler type,
        /// so the gate's second half - a new handler must be reachable from a showcase card - has
        /// nothing to ask of it.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new PrintedIconTargetHandler(
                    RadiusUnitTargetV1, PrintedValueChannel.Radius,
                    declaresStraightLineFilter: false, PrintedIconSelectionSubject.Unit)
            };

        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new ChooseTargetHandler(ChooseTargetV3, declaresActivity: false),
                new ChooseTargetHandler(ChooseTargetV4, declaresActivity: true),
                new PrintedIconTargetHandler(
                    RadiusTargetV1, PrintedValueChannel.Radius, declaresStraightLineFilter: false),
                new PrintedIconTargetHandler(
                    RangeTargetV1, PrintedValueChannel.Range, declaresStraightLineFilter: true),
                new PrintedIconTargetHandler(
                    RadiusHeroTargetV1, PrintedValueChannel.Radius,
                    declaresStraightLineFilter: false, PrintedIconSelectionSubject.Hero),
                new PrintedIconTargetHandler(
                    RangeHeroTargetV1, PrintedValueChannel.Range,
                    declaresStraightLineFilter: false, PrintedIconSelectionSubject.Hero),
                new PrintedIconTargetHandler(
                    RadiusMinionTargetV1, PrintedValueChannel.Radius,
                    declaresStraightLineFilter: false, PrintedIconSelectionSubject.Minion),
                new PrintedIconTargetHandler(
                    RangeMinionTargetV1, PrintedValueChannel.Range,
                    declaresStraightLineFilter: false, PrintedIconSelectionSubject.Minion),
                new PrintedIconTargetHandler(
                    RangeUnitTargetV1, PrintedValueChannel.Range,
                    declaresStraightLineFilter: false, PrintedIconSelectionSubject.Unit),
                new HeroAdjacentToMinionHandler()
            };

        /// <summary>
        /// Which kind of thing a printed-icon selection offers, and therefore what it publishes.
        /// Milestone 8.10.
        /// </summary>
        private enum PrintedIconSelectionSubject
        {
            /// <summary>Milestone 7.2b's: every board object in the disk, published as its space.</summary>
            BoardObject = 0,

            /// <summary>The heroes on a declared side of it, published as the hero.</summary>
            Hero = 1
            ,
            Minion = 2,

            /// <summary>
            /// Milestone 8.21. Rulebook p.19's units - a hero or a minion - published as whichever one
            /// was named. Brogan's Throwing Axe and Throwing Spear print "target a unit in range", and
            /// no card before them chose a unit through card text: every earlier printed-Range Attack
            /// takes its target through the action's own targeting, where p.13 already admits both.
            /// </summary>
            Unit = 3
        }

        /// <summary>
        /// Every board object the acting hero may target inside its card's printed icon, with the
        /// origin the disk was measured from.
        ///
        /// <para>Shared by both selections this class builds on p.9's icons - milestone 7.2b's, which
        /// publishes the chosen object's space, and milestone 8.10's, which publishes the chosen hero -
        /// so p.9's disk, p.18's immunity and p.16's item composition are asked in exactly one place
        /// and the two selections cannot come to disagree about who is inside a card's icon.</para>
        ///
        /// <para>The total is re-derived on every ask rather than captured, which is what makes the
        /// offered disk and the disk an answer is checked against the same one: an item gained between
        /// the two would widen both, and neither is written to the snapshot.</para>
        /// </summary>
        private static IReadOnlyList<BoardObjectReference> PrintedIconTargets(
            EffectCapabilityContext context,
            PrintedValueChannel channel,
            bool includeSelf,
            out HexCoordinate origin)
        {
            origin = default;
            var components = context.Components;
            if (components == null || !components.CanDisplace || !context.CardState.IsAvailable)
            {
                return Array.Empty<BoardObjectReference>();
            }

            var activity = (RuleActivity)context.Input("activity").IntegerValue;
            if (activity == RuleActivity.None || !Enum.IsDefined(typeof(RuleActivity), activity))
            {
                return Array.Empty<BoardObjectReference>();
            }

            var board = components.Board;
            var selector = BoardObjectReference.ForHero(context.ActorId);
            if (!board.TryGetObjectCoordinate(selector, out origin))
            {
                return Array.Empty<BoardObjectReference>();
            }

            var printedTotal = channel == PrintedValueChannel.Radius
                ? RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems)
                : RangeValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications,
                    // Milestone 8.19c. Card text resolving inside an action asks for the same Range
                    // total that action's own targeting asked for, so the class is read from the card
                    // and action this node is inside rather than assumed from the node.
                    ActionValueApplicabilityQuery.For(
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions));
            if (printedTotal.Total <= 0)
            {
                // A card printing no icon of this channel has no such bound at all, and p.16 does
                // not give it one: an item raises a printed icon and never invents one. The
                // selection is therefore impossible rather than adjacent-by-default.
                return Array.Empty<BoardObjectReference>();
            }

            // p.9 states one geometry for both icons - "targets up to 3 spaces away", counted
            // through obstacles because there are no line-of-sight rules - so the Range reading
            // asks the same disk question. RadiusQuery is where that question lives and it refuses
            // a total of the wrong channel by construction, which is why the Range arm asks
            // TargetQuery, the shared contract underneath it, rather than mislabelling its total.
            return channel == PrintedValueChannel.Radius
                ? RadiusQuery.Targets(
                    origin,
                    printedTotal,
                    includeSelf,
                    selector.AsRuleObject(),
                    activity,
                    board,
                    context.Participants,
                    context.Modifications,
                    context.ImmunityOverrides,
                    activity)
                : TargetQuery.FindTargetsWithin(
                    origin,
                    printedTotal.Total,
                    includeSelf,
                    selector.AsRuleObject(),
                    activity,
                    board,
                    context.Participants,
                    context.Modifications,
                    context.ImmunityOverrides,
                    activity);
        }

        /// <summary>
        /// Milestone 7.2b's printed-icon selection: the same chooser-owned target selection, whose
        /// bound is the acting card's Radius or Range Total rather than an integer the definition
        /// carries.
        ///
        /// It states no range parameter at all, which is the whole point - there is nothing for a
        /// definition to write down, because p.9 puts the value on the card. What it does state is the
        /// activity it targets under, since p.18 declares immunity per activity and this selection
        /// filters immunity through the same <see cref="TargetQuery"/> the capability above uses. The
        /// disk itself comes from <see cref="RadiusQuery"/>, and the total from
        /// <see cref="RadiusValueQuery"/> or <see cref="RangeValueQuery"/>, so neither the geometry nor
        /// the item composition is repeated here.
        ///
        /// Milestone 8.6 gave it the channel and the optional straight-line complement as constructor
        /// state, exactly as <see cref="ChooseTargetHandler"/> takes its activity declaration: two
        /// capability IDs, one implementation, and no way for the two icons' selections to disagree
        /// about anything except which icon they read.
        /// </summary>
        private sealed class PrintedIconTargetHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler,
                IEffectAnticipatedOutcomeCapabilityHandler,
                IEffectImmunityOverrideCapabilityHandler
        {
            private readonly PrintedValueChannel channel;
            private readonly bool declaresStraightLineFilter;
            private readonly PrintedIconSelectionSubject subject;

            public PrintedIconTargetHandler(
                EffectCapabilityReference reference,
                PrintedValueChannel channel,
                bool declaresStraightLineFilter,
                PrintedIconSelectionSubject subject = PrintedIconSelectionSubject.BoardObject)
            {
                Reference = reference;
                this.channel = channel;
                this.declaresStraightLineFilter = declaresStraightLineFilter;
                this.subject = subject;

                var parameters = new List<EffectCapabilityParameter>
                {
                    new EffectCapabilityParameter(
                        "activity",
                        EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.AllExcept(RuleActivity.None))
                };
                if (subject == PrintedIconSelectionSubject.BoardObject)
                {
                    // Only an object selection can be asked about the actor's own space; a selection
                    // that names heroes on a declared side answers that question through the side.
                    parameters.Insert(0, new EffectCapabilityParameter(
                        "includeSelf", EffectValueKind.Boolean));
                }
                else if (subject == PrintedIconSelectionSubject.Hero)
                {
                    parameters.Add(new EffectCapabilityParameter(
                        TargetRelationParameterName,
                        EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.AllExcept(
                            SourceTeamRelation.None)));

                    // Milestone 8.11. Each icon's hero selection declares the one narrowing a verified
                    // card pairs with it and no other: p.9's disk without its innermost ring for the
                    // Radius cards' "not adjacent to you", and the attacker's exclusion for the Range
                    // cards' "other than the attacker". Declaring both on both would carry a parameter
                    // nothing sets, which is the ambiguity the Range/Radius split exists to refuse.
                    parameters.Add(channel == PrintedValueChannel.Radius
                        ? new EffectCapabilityParameter(
                            ExcludeAdjacentParameterName, EffectValueKind.Boolean)
                        : new EffectCapabilityParameter(
                            ExcludeTheAttackerParameterName, EffectValueKind.Boolean));

                    // Milestone 8.35z28. The two narrowings Dodger's hero selections print, each declared
                    // on the arm whose face prints it and neither on the other - the same rule that
                    // kept `excludeAdjacent` off the Range arm and `excludeTheAttacker` off the Radius
                    // one. Both are optional, so every hero selection composed before her is unchanged
                    // without naming either.
                    parameters.Add(channel == PrintedValueChannel.Range
                        ? new EffectCapabilityParameter(
                            HasCardInDiscardParameterName,
                            EffectValueKind.Boolean,
                            isRequired: false)
                        : new EffectCapabilityParameter(
                            AdjacentToEmptyMinionSpawnPointParameterName,
                            EffectValueKind.Boolean,
                            isRequired: false));

                    // Milestone 8.35z30. Optional, and false where nothing sets it, which is
                    // every hero selection written before Silverarrow: see RULING 8 and
                    // <see cref="HeroCandidates"/> for why hers is the first face that can be its own
                    // candidate.
                    parameters.Add(new EffectCapabilityParameter(
                        IncludeSelfParameterName, EffectValueKind.Boolean, isRequired: false));
                }
                else
                {
                    parameters.Add(new EffectCapabilityParameter(
                        TargetRelationParameterName,
                        EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.AllExcept(
                            SourceTeamRelation.None)));

                    // Milestone 8.24. The class adjective Xargatha's Green line prints, on the minion
                    // arm alone: a unit selection's sentence names heroes as well, so a minion-class
                    // narrowing there would be a parameter that could not describe half its own
                    // candidates.
                    if (subject == PrintedIconSelectionSubject.Minion)
                    {
                        parameters.Add(new EffectCapabilityParameter(
                            AdmittedMinionClassParameterName,
                            EffectValueKind.Integer,
                            isRequired: false,
                            integerDomain: EffectIntegerDomain.All<RosterMinionClass>()));
                    }

                    // Milestone 8.24. "not adjacent to you", which the hero arm has declared since
                    // 8.11 and which a verified card now pairs with the unit arm as well: Siren's
                    // Call, "Target an enemy unit not adjacent to you and in range". It is added here
                    // by the same rule that kept it off the arms that had no card printing it -
                    // declare the narrowing a verified face pairs with this selection - and it is
                    // optional, so Brogan's two unit selections keep their exact candidate sets.
                    if (subject == PrintedIconSelectionSubject.Unit)
                    {
                        parameters.Add(new EffectCapabilityParameter(
                            ExcludeAdjacentParameterName,
                            EffectValueKind.Boolean,
                            isRequired: false));

                        // Milestone 8.35z33. "occupying a spawn point", which Whisper's three Blue
                        // displacement Skills pair with the unit arm and no earlier face prints. It
                        // is declared here by the same rule the line above follows - declare the
                        // narrowing a verified face pairs with this selection - and it is optional,
                        // so Brogan's and Xargatha's unit selections keep their exact candidate sets.
                        parameters.Add(new EffectCapabilityParameter(
                            OccupiesSpawnPointParameterName,
                            EffectValueKind.Boolean,
                            isRequired: false));
                    }
                }

                if (declaresStraightLineFilter)
                {
                    parameters.Add(new EffectCapabilityParameter(
                        ExcludeStraightLineParameterName, EffectValueKind.Boolean));
                }

                parameters.Add(new EffectCapabilityParameter(
                    EffectSelectionDeclaration.ConsumerParameterName,
                    EffectValueKind.Node,
                    isRequired: false));
                Schema = new EffectCapabilityParameterSchema(
                    parameters,
                    new[]
                    {
                        subject == PrintedIconSelectionSubject.BoardObject
                            ? new EffectOutputBinding("chosen", EffectValueKind.Coordinate)
                            : subject == PrintedIconSelectionSubject.Hero
                                ? new EffectOutputBinding(ChosenHeroOutputName, EffectValueKind.Hero)
                                : new EffectOutputBinding(ChosenMinionOutputName, EffectValueKind.Minion)
                    }
                    // Milestone 8.21. A unit selection publishes both identities, because which kind
                    // was named is the answer rather than something the node decided in advance - the
                    // same pair `choose-target@2` publishes for the same reason.
                    .Concat(subject == PrintedIconSelectionSubject.Unit
                        ? new[]
                        {
                            new EffectOutputBinding(ChosenHeroOutputName, EffectValueKind.Hero),
                            // Milestone 8.24. Siren's Call moves whichever kind it named, and a Move
                            // takes a coordinate, so the unit arm publishes the space beside the
                            // identity exactly as the minion arm has since 8.20.
                            new EffectOutputBinding(
                                ChosenMinionSpaceOutputName, EffectValueKind.Coordinate)
                        }
                        : Enumerable.Empty<EffectOutputBinding>())
                    .Concat(subject == PrintedIconSelectionSubject.Minion
                        // Milestone 8.20. The minion arm publishes where its answer is standing as
                        // well as who it is, because a Swap takes a space and the card's sentence
                        // names a minion. A node that does not declare it never sees it.
                        ? new[]
                        {
                            new EffectOutputBinding(
                                ChosenMinionSpaceOutputName, EffectValueKind.Coordinate)
                        }
                        : Enumerable.Empty<EffectOutputBinding>()));
            }

            public EffectCapabilityReference Reference { get; }

            public IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                AllActivities;

            public EffectCapabilityParameterSchema Schema { get; }

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = AnticipateCandidates(context);
                if (candidates.Count == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor, candidates, 1, 1));
            }

            /// <summary>
            /// Milestone 8.24. One selection outcome, with the chosen piece's space beside it where the
            /// consuming node asked for one.
            ///
            /// <para>It is the same publication the minion arm has made since 8.20 and it is here for
            /// the same reason: a card whose sentence names a unit and then moves it needs both halves
            /// - the identity, because the sentence names one, and the space, because every
            /// repositioning operation in this build takes a coordinate. Xargatha's Siren's Call is
            /// what needs it on the unit arm, where the piece it moves may be either kind. It is
            /// published only for a node that declared the output, so Brogan's two unit selections see
            /// exactly what they saw.</para>
            /// </summary>
            private static IReadOnlyList<EffectOutcomeFact> WithChosenSpace(
                EffectCapabilityContext context,
                BoardObjectReference chosen,
                EffectOutcomeFact identity)
            {
                var outcomes = new List<EffectOutcomeFact> { identity };
                if (context.Publishes(ChosenMinionSpaceOutputName) &&
                    context.Components?.Board != null &&
                    context.Components.Board.TryGetObjectCoordinate(chosen, out var at))
                {
                    outcomes.Add(new EffectOutcomeFact(
                        ChosenMinionSpaceOutputName, EffectValue.Space(at)));
                }

                return outcomes;
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var published = Publish(context, selected);
                return published == null
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(published);
            }

            /// <summary>
            /// What answering this selection with <paramref name="selected"/> publishes, or nothing
            /// where that answer is not one this selection would take.
            ///
            /// <para>Milestone 8.35l lifted this out of <see cref="Resume"/> unchanged, so the facts
            /// the resolver receives after an answer and the facts milestone 8.35l's narrowing puts to
            /// the consuming node before the offer are the same derivation rather than two readings
            /// that could drift - which is the discipline milestone 8.29d named for the greyed control
            /// and the refusal behind it.</para>
            /// </summary>
            private IReadOnlyList<EffectOutcomeFact> Publish(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                // Milestone 8.21. A unit selection is answered with whichever kind was named, and
                // publishes that kind alone - a node consuming it binds both outputs and one of them
                // resolves, which is exactly how `set-attack-target@1` takes either.
                if (subject == PrintedIconSelectionSubject.Unit)
                {
                    if (selected == null || selected.Count != 1)
                    {
                        return null;
                    }

                    if (selected[0].Kind == EffectValueKind.Hero &&
                        HeroCandidates(context).Contains(selected[0].HeroId))
                    {
                        return WithChosenSpace(
                            context,
                            BoardObjectReference.ForHero(selected[0].HeroId),
                            new EffectOutcomeFact(
                                ChosenHeroOutputName, EffectValue.Hero(selected[0].HeroId)));
                    }

                    if (selected[0].Kind == EffectValueKind.Minion &&
                        MinionCandidates(context).Contains(selected[0].MinionId))
                    {
                        return WithChosenSpace(
                            context,
                            BoardObjectReference.ForMinion(selected[0].MinionId),
                            new EffectOutcomeFact(
                                ChosenMinionOutputName, EffectValue.Minion(selected[0].MinionId)));
                    }

                    return null;
                }

                if (subject == PrintedIconSelectionSubject.Hero)
                {
                    if (selected == null || selected.Count != 1 ||
                        selected[0].Kind != EffectValueKind.Hero ||
                        !HeroCandidates(context).Contains(selected[0].HeroId))
                    {
                        return null;
                    }

                    return new[]
                    {
                        new EffectOutcomeFact(
                            ChosenHeroOutputName, EffectValue.Hero(selected[0].HeroId))
                    };
                }

                if (subject == PrintedIconSelectionSubject.Minion)
                {
                    if (selected == null || selected.Count != 1 ||
                        selected[0].Kind != EffectValueKind.Minion ||
                        !MinionCandidates(context).Contains(selected[0].MinionId))
                    {
                        return null;
                    }

                    var chosenMinion = BoardObjectReference.ForMinion(selected[0].MinionId);
                    var minionOutcomes = new List<EffectOutcomeFact>
                    {
                        new EffectOutcomeFact(
                            ChosenMinionOutputName, EffectValue.Minion(selected[0].MinionId))
                    };
                    // Published for a node that asked, and only for one: Wasp's High Voltage names the
                    // minion it defeats and wants no space, while Arien's Swap takes a space and wants
                    // no name. See EffectCapabilityContext.DeclaredOutputs.
                    if (context.Publishes(ChosenMinionSpaceOutputName) &&
                        context.Components?.Board != null &&
                        context.Components.Board.TryGetObjectCoordinate(chosenMinion, out var minionAt))
                    {
                        minionOutcomes.Add(new EffectOutcomeFact(
                            ChosenMinionSpaceOutputName, EffectValue.Space(minionAt)));
                    }

                    return minionOutcomes;
                }

                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate)
                {
                    return null;
                }

                var chosen = selected[0].Coordinate;
                if (!Candidates(context).Contains(chosen))
                {
                    return null;
                }

                return new[]
                {
                    new EffectOutcomeFact("chosen", EffectValue.Space(chosen))
                };
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                AnticipateCandidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>Selecting mutates nothing; the consuming node does whatever the effect does.
            /// Milestone 8.35l.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>One piece: every icon selection names exactly one, which is what its own
            /// choice request has always declared.</summary>
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                Publish(context, chosen) ?? Array.Empty<EffectOutcomeFact>();

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                subject == PrintedIconSelectionSubject.Hero
                    ? HeroCandidates(context).Select(EffectValue.Hero).ToArray()
                    : subject == PrintedIconSelectionSubject.Minion
                        ? MinionCandidates(context).Select(EffectValue.Minion).ToArray()
                    : subject == PrintedIconSelectionSubject.Unit
                        // p.19's two unit kinds in one offer, heroes first and each kind in its own
                        // stable order, because a card that says "a unit" says both and says nothing
                        // about which it prefers.
                        ? HeroCandidates(context).Select(EffectValue.Hero)
                            .Concat(MinionCandidates(context).Select(EffectValue.Minion))
                            .ToArray()
                    : Candidates(context).Select(candidate => EffectValue.Space(candidate)).ToArray();

            private IReadOnlyList<MinionId> MinionCandidates(EffectCapabilityContext context)
            {
                var relation = (SourceTeamRelation)context
                    .Input(TargetRelationParameterName).IntegerValue;
                if (relation == SourceTeamRelation.None ||
                    !Enum.IsDefined(typeof(SourceTeamRelation), relation))
                {
                    return Array.Empty<MinionId>();
                }

                // Milestone 8.24. The class adjective, where the sentence prints one. An undeclared
                // parameter is p.22's whole set, which is what every minion selection before
                // Xargatha's Green line means by naming no class; an integer that is not one of the
                // three is a definition making a claim this vocabulary cannot answer, so it offers
                // nobody rather than quietly widening to everybody.
                var admittedClass = (RosterMinionClass)context
                    .Input(AdmittedMinionClassParameterName).IntegerValue;
                if (!Enum.IsDefined(typeof(RosterMinionClass), admittedClass))
                {
                    return Array.Empty<MinionId>();
                }

                var targets = PrintedIconTargets(context, channel, includeSelf: false, out var origin);
                var board = context.Components?.Board;
                if (board == null) return Array.Empty<MinionId>();

                // Milestone 8.24. A unit selection's "not adjacent to you" has to remove minions as
                // well as heroes, or Siren's Call would refuse the adjacent hero and offer the
                // adjacent minion standing beside him - which is not what "an enemy unit not adjacent
                // to you" says. The hero half applies the identical rule.
                var excludeAdjacent = subject == PrintedIconSelectionSubject.Unit &&
                    context.Input(ExcludeAdjacentParameterName).BooleanValue;
                var onSpawnPoint = RequiresSpawnPoint(context);

                return targets
                    .Where(target => target.Kind == BoardObjectKind.Minion &&
                        TargetQuery.StandsInRelationTo(
                            relation, target, context.ActorId, context.Participants, board) &&
                        AdmitsClass(admittedClass, target.MinionId, board) &&
                        !(excludeAdjacent &&
                            board.TryGetObjectCoordinate(target, out var at) &&
                            HexDistanceQuery.Calculate(origin, at) == 1) &&
                        (!onSpawnPoint || StandsOnASpawnPoint(board, target)))
                    .Select(target => target.MinionId)
                    .ToArray();
            }

            /// <summary>
            /// Whether one minion on the board is of a class the printed sentence admits.
            ///
            /// <para>It reads the minion's own <see cref="StandardMinionType"/> off the board rather
            /// than deriving a class from behaviour, because p.22 prints the three classes as
            /// identities and a card that says "ranged minion" is naming that identity. A minion the
            /// board does not know is admitted by nothing, which cannot happen for a target the disk
            /// just produced and is stated so the answer does not depend on that.</para>
            /// </summary>
            private static bool AdmitsClass(
                RosterMinionClass admittedClass, MinionId minionId, IBoardDisplacementAuthority board)
            {
                if (admittedClass == RosterMinionClass.AnyClass)
                {
                    return true;
                }

                var minion = board.Minions
                    .FirstOrDefault(value => value.MinionId.Equals(minionId));
                if (minion == null)
                {
                    return false;
                }

                return SyntheticRosterCapabilities.AdmittedClassIncludes(
                    admittedClass, minion.MinionType);
            }

            /// <summary>
            /// Milestone 8.10. The heroes on the declared side of the board inside the same disk, which
            /// is what card text naming "an enemy hero in radius" chooses among.
            ///
            /// <para>The acting hero is never among them: the disk is asked without self, and the side
            /// would exclude them in any case. An empty result is
            /// <see cref="EffectExecutionResultKind.ImpossibleMandatory"/> rather than a decline,
            /// because by p.10 the sentence is one mandatory step and a step with nobody to perform it
            /// is one that cannot complete; the `if able` its lower-Tier printings add is about the
            /// card, and is declared on the discard node where the page puts it.</para>
            /// </summary>
            private IReadOnlyList<HeroId> HeroCandidates(EffectCapabilityContext context)
            {
                var relation = (SourceTeamRelation)context
                    .Input(TargetRelationParameterName).IntegerValue;
                if (relation == SourceTeamRelation.None ||
                    !Enum.IsDefined(typeof(SourceTeamRelation), relation))
                {
                    return Array.Empty<HeroId>();
                }

                // Milestone 8.35z30, RULING 8. Every hero selection written before
                // Silverarrow names a side its own hero cannot be on - "an enemy hero", "a friendly
                // hero" - so leaving the actor out decided nothing. Fae Healing and Nature's Blessing
                // print "A hero in radius" with no side at all, and the user settled that this reaches
                // enemies and reaches her: "sure, she can target enemies if she wants" and "We include
                // self". So the arm carries the same optional widening the board-object arm has always
                // had, and a card that says nothing sets nothing and keeps its exact candidates.
                var includeSelf = context.Inputs.ContainsKey(IncludeSelfParameterName) &&
                    context.Input(IncludeSelfParameterName).BooleanValue;
                var targets = PrintedIconTargets(context, channel, includeSelf, out var origin);
                var board = context.Components?.Board;
                if (board == null)
                {
                    return Array.Empty<HeroId>();
                }

                // Milestone 8.24 widens the arm rather than the meaning: the Radius hero selection has
                // declared this since 8.11, and a unit selection declares it now because Siren's Call
                // prints it on one. The word means the same thing on both - p.9's reach without its
                // innermost ring.
                var excludeAdjacent =
                    (channel == PrintedValueChannel.Radius ||
                        subject == PrintedIconSelectionSubject.Unit) &&
                    context.Input(ExcludeAdjacentParameterName).BooleanValue;

                // Milestone 8.11. "other than the attacker", which only a Block's rider prints and only
                // a Range selection therefore declares. The attacker is the enclosing Attack's own
                // actor - a fact about the action this text is answering rather than about anything
                // the text did - so it is read from the same context p.13's subtype is.
                var excludedAttacker = channel == PrintedValueChannel.Range &&
                    context.Input(ExcludeTheAttackerParameterName).BooleanValue
                        ? context.EnclosingAttack.ActorId
                        : default;

                // Milestone 8.35z28. Dodger's two hero narrowings, each read only on the arm that
                // declares it. An unbound optional parameter is the absence of the clause, which is why
                // both default to false and every earlier hero selection is untouched.
                var requiresACardInDiscard = channel == PrintedValueChannel.Range &&
                    context.Input(HasCardInDiscardParameterName).BooleanValue;
                var requiresAnEmptySpawnPointBeside = channel == PrintedValueChannel.Radius &&
                    context.Input(AdjacentToEmptyMinionSpawnPointParameterName).BooleanValue;

                // Derived once for the whole offer rather than per candidate: the qualifying points are
                // a property of the board and the map, and asking six times would answer identically
                // six times.
                //
                // The overlay is read from the ACTOR and never from the candidate. Tide of Darkness is
                // Dodger's own Ultimate and its clause is "while YOU are performing an action"; Death
                // Trap's sentence then asks whether the enemy hero it is about stands beside a
                // qualifying point. Two different heroes, one of whom supplies the board reading and
                // the other of whom is measured against it - which is exactly what the printed sentence
                // says. Milestone 8.35z28.
                var qualifyingSpawnPoints =
                    requiresAnEmptySpawnPointBeside && context.Components?.Tokens != null
                        ? EmptyMinionSpawnPointQuery.FindInCurrentBattleZone(
                            board,
                            context.Components.Tokens,
                            SpawnPointOverlay.ForActor(
                                context.Modifications, context.Participants, context.ActorId))
                        : Array.Empty<HexCoordinate>();

                var heroes = new List<HeroId>(targets.Count);
                foreach (var target in targets)
                {
                    if (target.Kind != BoardObjectKind.Hero ||
                        !TargetQuery.StandsInRelationTo(
                            relation, target, context.ActorId, context.Participants, board))
                    {
                        continue;
                    }

                    if (excludedAttacker.IsDefined && target.HeroId.Equals(excludedAttacker))
                    {
                        continue;
                    }

                    // p.9's disk without its innermost ring, which is what "and not adjacent to you"
                    // removes. It is stated as the exact distance the word means rather than as
                    // "further than one", so it says nothing about the actor's own space and does not
                    // quietly depend on that space having been excluded above.
                    if (excludeAdjacent &&
                        board.TryGetObjectCoordinate(target, out var at) &&
                        HexDistanceQuery.Calculate(origin, at) == 1)
                    {
                        continue;
                    }

                    // "who has one or more cards in the discard" - the named hero's own pile, read the
                    // same way `owned-card-present@1` reads one so a card that selects by the fact and
                    // a card that conditions on it cannot come to disagree about what it means.
                    //
                    // Milestone 8.35z45 made that sentence true. It was three copies of the
                    // same two clauses; the modal-Attack bullet's narrowing would have been a fourth,
                    // so the reading became one derivation that all of them ask.
                    if (requiresACardInDiscard &&
                        (!context.CardState.IsAvailable ||
                            !HeroDiscardPileQuery.HasAnyCard(
                                context.CardState.Instances, target.HeroId)))
                    {
                        continue;
                    }

                    // "who is adjacent to an empty spawn point in the battle zone" - measured from each
                    // candidate's own space, which is what makes it a second bound rather than a wider
                    // disk.
                    if (requiresAnEmptySpawnPointBeside &&
                        (!board.TryGetObjectCoordinate(target, out var beside) ||
                            !qualifyingSpawnPoints.Any(point =>
                                HexDistanceQuery.Calculate(beside, point) == 1)))
                    {
                        continue;
                    }

                    // Milestone 8.35z33. "occupying a spawn point" reaches heroes as well as minions,
                    // because the sentence it comes from names p.19's units: Seeds of Fear moves
                    // "an enemy unit in radius occupying a spawn point", and an enemy hero standing
                    // on one is exactly that.
                    if (RequiresSpawnPoint(context) && !StandsOnASpawnPoint(board, target))
                    {
                        continue;
                    }

                    heroes.Add(target.HeroId);
                }

                return heroes;
            }

            /// <summary>
            /// Whether this selection's own sentence prints "occupying a spawn point". Only the unit
            /// arm declares the parameter, so every other arm reads it as absent and narrows nothing.
            /// </summary>
            private bool RequiresSpawnPoint(EffectCapabilityContext context) =>
                subject == PrintedIconSelectionSubject.Unit &&
                context.Input(OccupiesSpawnPointParameterName).BooleanValue;

            /// <summary>
            /// Whether the space one unit is standing on carries a Spawn point.
            ///
            /// <para><b>Any Spawn point, of either type and either team, anywhere on the board.</b>
            /// That is p.19's own definition - "Spaces on the board used to spawn minions and heroes"
            /// - and it is the whole of what these three faces print. The narrower
            /// empty-minion-spawn-point-in-the-battle-zone question the Red and Green lines ask is a
            /// different phrase with three more conditions, derived in
            /// <see cref="GuardsDigital.Core.Rules.Modifications.EmptyMinionSpawnPointQuery"/>; the
            /// two are deliberately not shared, because reading one through the other would put
            /// qualifiers on these faces that they do not print.</para>
            /// </summary>
            private static bool StandsOnASpawnPoint(
                IBoardDisplacementAuthority board, BoardObjectReference target)
            {
                if (!board.TryGetObjectCoordinate(target, out var at))
                {
                    return false;
                }

                return board.MapDefinition.SpawnPoints.Any(point =>
                    board.MapDefinition.Spaces.Any(space =>
                        space.Id.Equals(point.SpaceId) && space.Coordinate.Equals(at)));
            }

            /// <summary>
            /// The spaces of every object the acting hero may target inside its card's printed icon, in
            /// canonical axial order.
            ///
            /// The total is re-derived on every ask rather than captured, which is what makes the
            /// offered disk and the disk the answer is checked against the same one: an item gained
            /// between the two would widen both, and neither is written to the snapshot.
            /// </summary>
            private IReadOnlyList<HexCoordinate> Candidates(EffectCapabilityContext context)
            {
                var targets = PrintedIconTargets(
                    context, channel, context.Input("includeSelf").BooleanValue, out var origin);
                var board = context.Components?.Board;
                if (board == null)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var spaces = new List<HexCoordinate>(targets.Count);
                foreach (var target in targets)
                {
                    if (board.TryGetObjectCoordinate(target, out var at))
                    {
                        spaces.Add(at);
                    }
                }

                // p.19's straight-line rule read as a complement, anchored at the selecting hero. It is
                // applied after immunity rather than before it because the two narrow for different
                // reasons and neither is a special case of the other: a unit can be out of line and
                // immune at once, and both refusals are the card's.
                return declaresStraightLineFilter &&
                    context.Input(ExcludeStraightLineParameterName).BooleanValue
                    ? StraightLineQuery.FilterOutStraightLine(origin, spaces)
                    : (IReadOnlyList<HexCoordinate>)spaces;
            }
        }

        private sealed class HeroAdjacentToMinionHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler,
                IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => HeroAdjacentToMinionV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(MinionParameterName, EffectValueKind.Minion),
                        new EffectCapabilityParameter(
                            TargetRelationParameterName,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(
                                SourceTeamRelation.None)),
                        new EffectCapabilityParameter(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValueKind.Node, isRequired: false)
                    },
                    new[] { new EffectOutputBinding(ChosenHeroOutputName, EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(EffectValue.Hero).ToArray(), 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Hero ||
                    !Candidates(context).Contains(selected[0].HeroId))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        ChosenHeroOutputName, EffectValue.Hero(selected[0].HeroId))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(EffectValue.Hero).ToArray();

            /// <summary>Selecting mutates nothing. Milestone 8.35l.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>One hero: the selection names the one unit its sentence names.</summary>
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            /// <summary>
            /// Milestone 8.35l. The single output this selection publishes, for the answer it is asked
            /// about - the same fact <see cref="Resume"/> publishes, from the same candidate check.
            /// </summary>
            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                chosen != null && chosen.Count == 1 &&
                chosen[0].Kind == EffectValueKind.Hero &&
                Candidates(context).Contains(chosen[0].HeroId)
                    ? new[]
                    {
                        new EffectOutcomeFact(
                            ChosenHeroOutputName, EffectValue.Hero(chosen[0].HeroId))
                    }
                    : Array.Empty<EffectOutcomeFact>();

            private static IReadOnlyList<HeroId> Candidates(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var minionId = context.Input(MinionParameterName).MinionId;
                var relation = (SourceTeamRelation)context
                    .Input(TargetRelationParameterName).IntegerValue;
                if (board == null || !minionId.IsDefined || relation == SourceTeamRelation.None ||
                    !Enum.IsDefined(typeof(SourceTeamRelation), relation))
                {
                    return Array.Empty<HeroId>();
                }

                var minion = BoardObjectReference.ForMinion(minionId);
                if (!board.TryGetObjectCoordinate(minion, out var origin))
                    return Array.Empty<HeroId>();

                return board.Occupancies
                    .Where(occupancy =>
                        HexDistanceQuery.Calculate(origin, occupancy.Coordinate) == 1 &&
                        TargetQuery.StandsInRelationTo(
                            relation, BoardObjectReference.ForHero(occupancy.HeroId),
                            context.ActorId, context.Participants, board))
                    .Select(occupancy => occupancy.HeroId)
                    .OrderBy(id => id.Value, StringComparer.Ordinal)
                    .ToArray();
            }
        }

        /// <summary>
        /// Offers every targetable object within range of the acting hero as an authoritative
        /// chooser-owned selection and publishes the chosen object's space.
        ///
        /// It always pauses, even on a single candidate: that is the codebase's uniform capability
        /// precedent - direct Move pauses on one destination and an `up to 0` push pauses on one
        /// distance - and it keeps "chooser-owned" honest. Restore re-derives the candidates by
        /// re-asking this handler, exactly as every other capability choice restores. Selecting mutates
        /// nothing; the consuming node does whatever the effect does.
        /// </summary>
        private sealed class ChooseTargetHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler,
                IEffectAnticipatedOutcomeCapabilityHandler
        {
            private readonly bool declaresActivity;

            public ChooseTargetHandler(
                EffectCapabilityReference reference,
                bool declaresActivity)
            {
                Reference = reference;
                this.declaresActivity = declaresActivity;
                var parameters = new List<EffectCapabilityParameter>
                {
                    new EffectCapabilityParameter("range", EffectValueKind.Integer),
                    new EffectCapabilityParameter("includeSelf", EffectValueKind.Boolean)
                };
                if (declaresActivity)
                {
                    parameters.Add(new EffectCapabilityParameter(
                        "activity",
                        EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.AllExcept(RuleActivity.None)));
                }

                // Milestone 6.8l. Optional, because a selection whose result no single later node
                // consumes still has a legal shape, and one that names nothing keeps exactly the
                // conservative reading milestone 6.5ac chose. Nothing here reads this parameter: the
                // handler cannot see the definition, so the resolver owns the narrowing and this
                // declaration is what lets a definition state it.
                parameters.Add(new EffectCapabilityParameter(
                    EffectSelectionDeclaration.ConsumerParameterName,
                    EffectValueKind.Node,
                    isRequired: false));

                Schema = new EffectCapabilityParameterSchema(
                    parameters,
                    new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) });
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; }

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                if (candidates.Count == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    candidates.Select(candidate => EffectValue.Space(candidate)).ToArray(),
                    1,
                    1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived against current state rather than trusted from the paused choice: an
                // object targetable when the choice opened may have moved or become immune before the
                // answer arrived.
                var chosen = selected[0].Coordinate;
                if (!Candidates(context).Contains(chosen))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("chosen", EffectValue.Space(chosen))
                });
            }

            /// <summary>
            /// The same targeting question <see cref="Execute"/> asks, without opening the selection.
            /// Selecting mutates nothing even when it runs, so what this avoids is not a side effect
            /// but a pause: a probe that executed would leave a pending choice nobody asked for.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// Milestone 6.8l: the same spaces <see cref="Execute"/> would offer, as the same typed
            /// values, from the same derivation. The resolver asks for them so it can put each one to
            /// the node this selection selects for; what it does with the answer is not this handler's
            /// business, and could not be - a handler sees its own context, never the definition
            /// around it.
            /// </summary>
            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(candidate => EffectValue.Space(candidate)).ToArray();

            /// <summary>Selecting mutates nothing. Milestone 8.35l.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>One space: this selection has always requested exactly one.</summary>
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            /// <summary>
            /// Milestone 8.35l. The one space this selection publishes for the answer it is asked
            /// about, re-derived against current state exactly as <see cref="Resume"/> re-derives it.
            /// </summary>
            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                chosen != null && chosen.Count == 1 &&
                chosen[0].Kind == EffectValueKind.Coordinate &&
                Candidates(context).Contains(chosen[0].Coordinate)
                    ? new[]
                    {
                        new EffectOutcomeFact("chosen", EffectValue.Space(chosen[0].Coordinate))
                    }
                    : Array.Empty<EffectOutcomeFact>();

            /// <summary>
            /// The spaces of every object the acting hero may target, in canonical axial order. Nothing
            /// here walks the board or invents geometry: the reusable targeting contract owns both.
            /// </summary>
            private IReadOnlyList<HexCoordinate> Candidates(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.CanDisplace)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var range = context.Input("range").IntegerValue;
                if (range < 0)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var board = components.Board;
                var selector = BoardObjectReference.ForHero(context.ActorId);
                if (!board.TryGetObjectCoordinate(selector, out var origin))
                {
                    return Array.Empty<HexCoordinate>();
                }

                // Milestone 8.5 (F-BOARD-03). A node that names its own activity still wins - that is
                // what the declaration is for - and one that names none performs as the action it is
                // running inside rather than as the Skill it used to be the only one of.
                var activity = declaresActivity
                    ? (RuleActivity)context.Input("activity").IntegerValue
                    : context.EnclosingActivity;
                if (activity == RuleActivity.None || !Enum.IsDefined(typeof(RuleActivity), activity))
                {
                    return Array.Empty<HexCoordinate>();
                }

                var targets = TargetQuery.FindTargetsWithin(
                    origin,
                    range,
                    context.Input("includeSelf").BooleanValue,
                    selector.AsRuleObject(),
                    activity,
                    board,
                    context.Participants,
                    context.Modifications);

                var spaces = new List<HexCoordinate>(targets.Count);
                foreach (var target in targets)
                {
                    if (board.TryGetObjectCoordinate(target, out var at))
                    {
                        spaces.Add(at);
                    }
                }

                return spaces;
            }
        }
    }
}
