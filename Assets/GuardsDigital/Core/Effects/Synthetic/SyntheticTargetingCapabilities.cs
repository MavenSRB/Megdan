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
    /// <para><b>Milestone 8.88 changed the rule this file's parameter notes describe.</b> Until then a
    /// narrowing was declared only on the arm whose verified face printed it, so a later phrase pairing
    /// a known clause with another arm minted a capability. Every clause of the printed-icon family is
    /// now declared on every arm it can mean something on, optional, and refused by name only where it
    /// cannot be true of anything the arm offers - see <see cref="PrintedIconFilterParameterNames"/> and
    /// <see cref="PrintedIconFilterRefusal"/>. The "declared on one arm alone" notes below record where
    /// each clause was first needed; they no longer describe where it may be bound.</para>
    /// </summary>
    public static partial class SyntheticTargetingCapabilities
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


        /// <summary>
        /// Milestone 8.39b. The obstacle a printed-Range sentence grapples to, and the space along
        /// that line the actor ends up on - Cutter's Silver basic Grappling Bolt, "Target an obstacle
        /// in range and in a straight line, with no obstacles between you; ignore immunity. Move in a
        /// straight line towards that obstacle until you are adjacent."
        ///
        /// <para><b>It publishes a SPACE rather than a unit, and that is rulebook fidelity rather than
        /// a workaround.</b> p.18 defines an obstacle as "Units, terrain (including printed on the
        /// board), tokens, as well as any objects represented by miniatures", and it is emphatic about
        /// the consequence - "There can never be more than one obstacle in any one space. Ever. No
        /// exceptions." So an obstacle is named one-to-one by where it stands, and p.19's own Target
        /// keyword is "Target (Unit / Token / <b>Space</b>)". Nothing here needs a new
        /// <see cref="RosterUnitKind"/> member, a new <c>BoardObjectKind</c> or a new
        /// <c>EffectValueKind</c>: a heap of terrain is not a unit, and the coordinate says
        /// everything the following Move needs.</para>
        ///
        /// <para><b>"Ignore immunity" does real work, and p.18 is what says so.</b> The Immune keyword
        /// reads "If a unit is immune, you cannot target that unit ... <em>unless the card text says
        /// otherwise</em>" - and because an obstacle may BE a unit, this sentence is the card text that
        /// keyword anticipates. So the declaration implements
        /// <see cref="IEffectImmunityOverrideCapabilityHandler"/> and a node that prints the clause
        /// declares the override; one that does not still cannot reach an immune unit. That settles
        /// CU-Q2 out of the rulebook rather than out of a reading.</para>
        ///
        /// <para><b>"With no obstacles between you" is a printed narrowing of a default that admits
        /// everything.</b> p.9 states the default in as many words - "There are no 'line of sight'
        /// rules. You can target units through obstacles, as long as they are in range/radius" - so the
        /// clause is the card's own restriction and not a rule this selection would have applied
        /// anyway. Its arithmetic consequence is that each of p.19's six files offers AT MOST ONE
        /// candidate, the first obstacle along it, because a second one has the first between it and
        /// the actor. An immune unit the node may not target still occupies its file: it is an obstacle
        /// whether or not it can be chosen, so nothing behind it is offered either.</para>
        ///
        /// <para><b>Off the map is not an obstacle.</b> It is the absence of a space, which is the
        /// reading <c>choose-line-destination@1</c> already records; terrain IS one, by p.19's own
        /// definition - "Any board space without a full grid outline counts as terrain and is an
        /// obstacle, including the water spaces surrounding the board" - so the walk stops at the map's
        /// edge and offers the terrain ring it reaches first.</para>
        ///
        /// <para>The approach space is published beside the target for the reason
        /// <c>choose-charge-line@1</c> and <c>choose-through-target-line@1</c> publish theirs: the
        /// sentence that names the target is the same sentence that names where it takes you, and
        /// re-deriving the line in a later node would be a second source of truth about one step.</para>
        ///
        /// <para><b>Milestone 8.40e widened it rather than writing a second one</b>, for Bain's Silver
        /// basic Get over here! - "Target a unit or a token in a straight line and in range with no
        /// obstacles between you and the target; move the target towards you in a straight line, until
        /// you are adjacent." That is this walk's geometry exactly: p.19's six directions, the printed
        /// Range Total, the first-blocker stop, and p.18's full blocking list, which is what "no
        /// obstacles" asks for. The two things his sentence needs that hers does not are an optional
        /// narrowing of what may be NAMED - <see cref="ExcludeTerrainParameterName"/> - and the
        /// actor-side twin of the approach space - <see cref="ActorApproachSpaceOutputName"/>. Both are
        /// optional and default off, so her declaration and the showcase Skill beside it keep their
        /// exact candidate sets and their exact published facts.</para>
        /// </summary>
        public static EffectCapabilityId RangeObstacleTarget { get; } =
            new EffectCapabilityId("synthetic-range-obstacle-target");

        public static EffectCapabilityReference RangeObstacleTargetV1 =>
            new EffectCapabilityReference(RangeObstacleTarget, 1);

        /// <summary>The chosen obstacle's space.</summary>
        public const string ChosenObstacleSpaceOutputName = "chosenObstacleSpace";

        /// <summary>
        /// The space on the chosen obstacle's own file that leaves the actor adjacent to it - one step
        /// back along the line the selection walked. An obstacle already adjacent publishes the actor's
        /// own space, because "until you are adjacent" is satisfied where she stands and the Move it
        /// feeds is then a move of no spaces rather than an impossible step.
        /// </summary>
        public const string ObstacleApproachSpaceOutputName = "obstacleApproachSpace";

        /// <summary>
        /// Milestone 8.40e. The mirror of <see cref="ObstacleApproachSpaceOutputName"/>: the space on
        /// the chosen obstacle's own file that leaves IT adjacent to the ACTOR - one step out from the
        /// actor along the line this selection walked.
        ///
        /// <para>Grappling Bolt moves the hero and Get over here! moves the target, so one sentence
        /// ends beside the obstacle and the other ends beside the hero. They are the same file, which
        /// is why this is a second output on one selection rather than a second selection: re-deriving
        /// the line in the node that moves would be a second source of truth about one step.</para>
        ///
        /// <para>A target already adjacent publishes its own space, because "until you are adjacent"
        /// is satisfied where it stands. The Move that consumes it then has nowhere to go - p.11's
        /// reachability never offers the mover's own space - so an <c>IfAble</c> node reads it as a
        /// step not taken rather than as a failure, which is the reading the printed sentence asks for
        /// and the shape Grappling Bolt's own Move already has.</para>
        ///
        /// <para>Published only for a node that declared it, and the obstacle-side output is now
        /// published on the same terms. That is not a convenience: see
        /// <see cref="EffectCapabilityContext.DeclaredOutputs"/> - a restored payload's outcomes are
        /// checked against the node's declaration, so a handler publishing a fact its node did not ask
        /// for writes a snapshot the build refuses to read back.</para>
        /// </summary>
        public const string ActorApproachSpaceOutputName = "actorApproachSpace";

        /// <summary>
        /// Milestone 8.40e. Whether this selection offers only the obstacles that are p.19's units and
        /// tokens - Get over here!'s "Target a unit <b>or a token</b>".
        ///
        /// <para><b>It narrows what may be NAMED and never what BLOCKS, and Bain's own deck is what
        /// makes that distinction load-bearing.</b> The same sentence still prints "no obstacles",
        /// which is p.18's full list - "Units, terrain (including printed on the board), tokens, as
        /// well as any objects represented by miniatures" - while his five crossbows print the strictly
        /// narrower "no units, or terrain, between you". So the walk stops where it always stopped, and
        /// a file whose first obstacle is terrain offers NOTHING on that file rather than offering what
        /// stands behind it. Reading this narrowing into the blocking set would let the pull reach past
        /// a wall, which is a sentence no face prints.</para>
        ///
        /// <para>What it excludes is exactly "no board object stands here". p.19's one-obstacle-per-
        /// space rule names a unit or a token one-to-one by its space, and the other two things this
        /// walk can stop on carry no object at all: a terrain space, and a space an
        /// <c>EffectiveTypeOverlay</c> makes count as one.</para>
        ///
        /// <para>Optional and defaulting to false, so Cutter's Grappling Bolt and the showcase Skill
        /// beside it keep their exact candidate sets without naming it.</para>
        /// </summary>
        public const string ExcludeTerrainParameterName = "excludeTerrain";

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
        public const string ChosenHeroSpaceOutputName = "chosenHeroSpace";

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
        /// Milestone 8.71a. Whether a printed-Range hero selection admits only heroes who currently
        /// hold rulebook p.10's UNRESOLVED card - Hanu the Trickster's Hurry up!, "Set the printed
        /// [Initiative] Initiative value of an <b>unresolved card of a hero in range</b> to 11".
        ///
        /// <para><b>It narrows who may be picked and nothing else</b>, which is the grammar every
        /// flag above records: "of a hero" makes the card a property of the candidate, so a hero whose
        /// card is already resolved - or who has not played one, or whose card is still face down - is
        /// not one to offer and then refuse. It matters more here than for any earlier narrowing,
        /// because the step that follows the selection is MANDATORY and has nothing else to do: an
        /// offer made to a hero with no such card would be p.10's impossible mandatory step arriving
        /// one node too late, after the player had already chosen.</para>
        ///
        /// <para><b>A Boolean, and unbound is the absence of the clause</b> - the shape 8.57c settled
        /// and 8.69a restated. There is no count to take and no threshold to name: a hero has an
        /// unresolved card or has not.</para>
        ///
        /// <para><b>The reading is <see cref="HeroUnresolvedCardQuery"/>'s</b>, which is the same
        /// reading <c>InitiativeResolutionAuthority</c> orders the round by and the same one the
        /// rewrite's own lifecycle transition admits. One phrase, one owner - so a hero this greys out
        /// cannot be a hero whose card the turn order is counting.</para>
        ///
        /// <para><b>It discloses nothing.</b> The turn area is one of p.4's OPEN zones
        /// (<see cref="PublicCardZoneQuery"/>) and this asks only about face-up cards in it, so every
        /// seat can already see exactly which heroes qualify.</para>
        ///
        /// <para><b>First declared on the Range arm alone</b> (see milestone 8.88's note on the class), by the rule this file then followed: Hurry up! is
        /// the one face in the corpus printing the clause and it prints a Range 4 icon. A parameter
        /// added to a registered capability moves no pinned byte - see
        /// <see cref="BothDiscardPilesEmptyParameterName"/>'s own note for why.</para>
        /// </summary>
        public const string HoldsAnUnresolvedCardParameterName = "holdsAnUnresolvedCard";

        /// <summary>
        /// Milestone 8.57d. Whether a printed-Radius hero selection admits only candidates for whom
        /// BOTH discard piles - the acting hero's and the candidate's own - hold nothing at all:
        /// Rowenna's Opening Shots and Opening Volley, "If <b>both you and an enemy hero in radius have
        /// no cards in the discard</b>, that hero discards a card, if able".
        ///
        /// <para><b>It is the negative complement of
        /// <see cref="HasCardInDiscardParameterName"/> and of
        /// <c>enemy-hero-with-discard-present@1</c>, and it is ONE parameter because it is ONE printed
        /// clause.</b> "Both you and an enemy hero in radius" is a single condition with two subjects;
        /// splitting it would put the actor's half somewhere the candidate list cannot see, and the
        /// two halves have to be answered together or the selection cannot say what the card says.
        /// The compound is carried in one name for the reason
        /// <see cref="AdjacentToAnEnemyHeroOrHoldingADiscardParameterName"/> carries a disjunction in
        /// one: the parameter is the clause, spelled the way the card spells it.</para>
        ///
        /// <para><b>A condition on the candidate SET rather than a gate on what happens next, and
        /// that is what the printed "if" earns here.</b> Whichever party's pile holds a card, the
        /// sentence names nobody - so an "if" written as a Conditional would leave the card starting
        /// and stopping, while a narrowing makes an unsatisfiable clause an empty candidate set, which
        /// a mandatory selection reports as impossible and milestone 8.35l's offer machinery greys.
        /// Rowenna's OWN pile is therefore load-bearing in a way no earlier narrowing's is: a card in
        /// it empties the whole candidate list at once, and the face has no legal target at all.</para>
        ///
        /// <para><b>"In the discard" means the discard pile and nothing adjacent to it.</b> p.10 puts a
        /// card in exactly one place, so a facedown card, a played card and a resolved card are not in
        /// the discard - and the reading is
        /// <see cref="GuardsDigital.Core.Cards.HeroDiscardPileQuery"/>'s, which is the same one
        /// <see cref="HasCardInDiscardParameterName"/> and <c>owned-card-present@1</c> ask, so a card
        /// that selects by the absence and a card that conditions on the presence cannot come to
        /// disagree about one phrase.</para>
        ///
        /// <para><b>It is read live, every time.</b> p.10 returns every card to its owner's hand at the
        /// end of a round, so both piles empty at the boundary without anything here knowing; and
        /// Opening Volley's repeat re-derives the whole list, which is what makes the hero who just
        /// discarded drop out of it and the actor - who discarded nothing - stay in.</para>
        ///
        /// <para><b>First declared on the Radius arm alone</b> (see milestone 8.88's note on the class), by the rule this file then followed: her
        /// two faces print a Radius 3 icon and no face in the corpus pairs the clause with a Range one.
        /// Optional and defaulting to false, so every hero selection composed before her keeps its
        /// exact candidate set without naming it - and a parameter added to a registered capability
        /// moves no pinned byte, because the identity walker digests a capability's reference and its
        /// Active-effect definitions and never its parameter schema.</para>
        /// </summary>
        public const string BothDiscardPilesEmptyParameterName = "bothDiscardPilesEmpty";

        /// <summary>
        /// Milestone 8.69a. Whether a printed-Radius hero selection's candidate stands beside three or
        /// more of rulebook p.18's obstacles - Brynn the Seeker's Decoy, his three tracking swaps and
        /// his three traps, all of which print "an enemy hero in radius <b>who is adjacent to 3 or more
        /// obstacles</b>".
        ///
        /// <para><b>A Boolean and never an Integer, which is the shape this file settled at 8.57c.</b>
        /// An unbound optional Integer reads as ZERO, and a threshold of zero is satisfied by every
        /// hero on the board - so a selection that forgot to bind it would silently admit everybody.
        /// The number the sentence names is not a call-site parameter at all: it is in
        /// <see cref="GuardsDigital.Core.Rules.Modifications.ObstacleAdjacencyQuery"/>'s method name,
        /// because all fifteen of his faces print the same three and a face printing a different one
        /// would be a different rule rather than a different argument.</para>
        ///
        /// <para><b>It narrows who may be picked and nothing else</b>, which is the grammar the two
        /// flags above record: "who is adjacent to" qualifies the hero being chosen, so a hero standing
        /// in the open is not offered rather than offered and then refused. The predicate is asked
        /// through the one owner of the printed phrase, so this selection, the modal-Attack bullet, the
        /// +2 on the Attack total and the presence predicate cannot come to disagree about it - and his
        /// Ultimate's counts-as overlay reaches all four at once.</para>
        ///
        /// <para><b>First declared on the Radius arm alone</b> (see milestone 8.88's note on the class), by the rule this file then followed: every face of
        /// his that prints the clause beside a reach prints a Radius icon, and his one Range face
        /// (Familiar Ground) states it as a modal-Attack bullet instead. Optional and defaulting to
        /// false, so every hero selection composed before him keeps its exact candidate set, and a
        /// parameter added to a registered capability moves no pinned byte - see
        /// <see cref="BothDiscardPilesEmptyParameterName"/>'s own note for why.</para>
        /// </summary>
        public const string AdjacentToThreeOrMoreObstaclesParameterName =
            "adjacentToThreeOrMoreObstacles";

        /// <summary>
        /// Milestone 8.69c. The hero a SECOND pass of one printed sentence must not name again -
        /// Brynn the Seeker's Decoy, "Choose up to two times, <b>on different targets</b>", and his
        /// two guides, "move a <b>different</b> friendly unit in radius".
        ///
        /// <para><b>It is spelled with the anchored family's own constant rather than a second literal
        /// that happens to match</b>, by the rule milestone 8.39h stated for "not adjacent to you" and
        /// milestone 8.57c restated for "with no cards in hand": one printed clause has one name in
        /// this catalog, so a card author who declares the exclusion for an anchored selection cannot
        /// mistype it for a printed-icon one. The reading is
        /// <see cref="SyntheticAnchoredTargetCapabilities.ExcludedHeroParameterName"/>'s, unchanged -
        /// a narrowing of who may be NAMED, bound to a node output because the card names the hero one
        /// node earlier rather than in its own words.</para>
        ///
        /// <para><b>Optional, and an unbound binding is the whole of what "the first pass named
        /// nobody" means.</b> Decoy's first performance may be declined and Mountain Guide's first
        /// sentence prints "You may", so the producing selection can finish without publishing a hero;
        /// the binding then resolves to nothing and this narrows nothing, which is exactly the printed
        /// answer - a sentence that named nobody excludes nobody. That is 8.61f's own note, restated
        /// here because the two families now read one clause.</para>
        ///
        /// <para><b>First declared on the Radius hero and Radius unit arms alone</b> (see milestone 8.88's note on the class), which are the two his
        /// faces print: Decoy's second bullet names "an enemy hero in radius" and the guides' second
        /// sentence names "a friendly unit in radius". No arm that no face pairs it with declares it,
        /// and a parameter added to a registered capability moves no pinned byte - see
        /// <see cref="BothDiscardPilesEmptyParameterName"/>'s own note for why.</para>
        /// </summary>
        public const string ExcludedHeroParameterName =
            SyntheticAnchoredTargetCapabilities.ExcludedHeroParameterName;

        /// <summary>
        /// Milestone 8.69c. The minion half of the same clause - Decoy's first bullet, "Move an enemy
        /// minion in radius 1 space", read a second time "on different targets", and the guides'
        /// second sentence where the first sentence moved a friendly minion.
        ///
        /// <para><b>Two parameters rather than one, because
        /// <see cref="EffectValueKind"/> has no "unit".</b> p.19's unit is a hero or a minion and this
        /// vocabulary names each by its own kind, which is the same reason a unit selection publishes
        /// <see cref="ChosenHeroOutputName"/> and <see cref="ChosenMinionOutputName"/> as two outputs
        /// and fills in whichever was named. A declaration excluding a unit therefore binds both and
        /// exactly one of them resolves - which is what makes "a different friendly unit" one clause
        /// even though it is written twice.</para>
        ///
        /// <para>Optional, on the Radius minion and Radius unit arms alone, on the terms the hero half
        /// above records.</para>
        /// </summary>
        public const string ExcludedMinionParameterName = "excludeMinion";

        /// <summary>
        /// Milestone 8.40f. The fewest cards a printed-Radius hero selection's candidate must be
        /// holding - Bain's three guessing Skills, "An enemy hero in radius <b>with two or more cards
        /// in hand</b> chooses one of those cards".
        ///
        /// <para>It narrows the candidates rather than gating what happens to the chosen one, which
        /// is the identical reading <see cref="HasCardInDiscardParameterName"/> records for the same
        /// grammar: "with two or more" is part of who may be picked, so a hero holding one card is
        /// not one to offer and then refuse. It is the COUNT form of that flag, and the threshold is
        /// the card's own printed number declared at the call site rather than compared in a
        /// definition - <c>presence-count@1</c>'s <c>minimumPresent</c> is the precedent, and the
        /// recorded reason it exists is that "every card that printed a different number would
        /// declare the comparison again".</para>
        ///
        /// <para><b>It discloses nothing, and that is measurable rather than argued.</b> Hand SIZE is
        /// already table-public: <c>SeatProjection.SeatHandSummary</c> publishes every hero's hand and
        /// deck count to every seat and has crossed the wire since milestone 8.30a, so any client can
        /// verify this greying for itself. Both readings ask
        /// <see cref="GuardsDigital.Core.Cards.HeroHandQuery"/>, so the count a seat is shown and the
        /// count this narrows by cannot come apart.</para>
        ///
        /// <para>Optional, and an unbound parameter is no narrowing at all, so every hero selection
        /// composed before Bain keeps its exact candidate set without naming it. First declared on
        /// the Radius arm alone (see milestone 8.88's note on the class), by the rule this file then followed - his three faces print "in
        /// radius" and no face in the corpus pairs the clause with a Range icon.</para>
        /// </summary>
        public const string MinimumCardsInHandParameterName = "minimumCardsInHand";

        /// <summary>
        /// Milestone 8.57c. Whether a selection admits only candidates holding NO cards at all -
        /// Rowenna's Accept Surrender and Glorious Triumph, "Defeat an enemy hero adjacent to you
        /// <b>with no cards in hand</b>".
        ///
        /// <para><b>It is <see cref="MinimumCardsInHandParameterName"/>'s complement and it is a
        /// BOOLEAN rather than a second count, which is measurement rather than taste.</b> The
        /// obvious spelling - a <c>maximumCardsInHand</c> bound to zero - cannot be written in this
        /// vocabulary: an unbound optional Integer reads as zero, so "at most zero cards" and "no
        /// narrowing at all" would be the same declaration, and every hero selection in the corpus
        /// would silently start demanding an empty hand. A flag says what the card says and an
        /// unbound one narrows nothing, which is the shape
        /// <see cref="HasCardInDiscardParameterName"/> and
        /// <see cref="AdjacentToEmptyMinionSpawnPointParameterName"/> already take.</para>
        ///
        /// <para><b>The hand is the hand, and p.10 is what makes that a complete answer.</b> A card
        /// is in exactly one place: played, resolved, facedown and discarded cards are not in hand,
        /// so a hero whose whole hand has been played qualifies and a hero holding one card does
        /// not. Both readings ask <see cref="GuardsDigital.Core.Cards.HeroHandQuery"/> -
        /// <c>IsEmpty</c> here, <c>HasAtLeast</c> for the count form - so the two clauses and the
        /// hand count every seat is already shown cannot come apart.</para>
        ///
        /// <para><b>It discloses nothing</b>, for the reason
        /// <see cref="MinimumCardsInHandParameterName"/> records at length: hand SIZE crosses the
        /// wire to every seat on every frame, so any client can verify this greying for itself.
        /// </para>
        ///
        /// <para><b>Where it is declared.</b> Her two faces print no Range and no Radius icon at all
        /// - the clause is "adjacent to you", which is card text and never an icon - so the arm that
        /// carries it is the written-distance roster selection, which names this constant through
        /// <c>SyntheticRosterCapabilities.RequiresEmptyHandParameter</c> exactly as milestone 8.39h
        /// made one name serve both families for "not adjacent to you". The name lives here beside
        /// the clause it complements so a card author cannot declare one of the two and mistype the
        /// other; no printed-icon arm declares it, because no face in the corpus pairs the clause
        /// with an icon.</para>
        /// </summary>
        public const string RequiresEmptyHandParameterName = "requiresEmptyHand";

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

        /// <summary>
        /// Milestone 8.57b. Whether a printed-icon UNIT selection admits only candidates that satisfy
        /// Rowenna's printed qualifier - Stand Guard, Protect the Weak and Defend the Innocent, "Swap
        /// with a friendly unit in range <b>which is adjacent to an enemy hero, or has a card in the
        /// discard</b>".
        ///
        /// <para><b>One parameter because it is ONE printed clause, and a disjunction is not two
        /// narrowings.</b> Every other narrowing in this family filters the set the one before it left,
        /// which is what a card printing two clauses means; this face prints "or", so two booleans
        /// would read it as "and" and admit only the friendly hero who is both beside an enemy AND
        /// holding a discard. The compound is carried in one name for the reason
        /// <see cref="AdjacentToEmptyMinionSpawnPointParameterName"/> carries three conditions in one:
        /// the parameter is the clause, spelled the way the card spells it.</para>
        ///
        /// <para><b>The two halves qualify different kinds of candidate, and that falls out of the
        /// rulebook rather than being arranged.</b> p.19's units are heroes and minions; only a hero
        /// owns cards, so a minion can satisfy this clause only by standing beside an enemy hero,
        /// which is what the printed sentence already says without saying it. Neither half is a
        /// property of the ACTOR: "an enemy hero" is measured from each candidate's own space - a
        /// second bound, exactly the shape milestone 8.22's <c>adjacentToFriendlyMinion</c> and
        /// milestone 8.35z28's spawn-point clause record - and "has a card in the discard" is the
        /// candidate's own pile, read through the same
        /// <see cref="GuardsDigital.Core.Cards.HeroDiscardPileQuery"/> every other reading of that
        /// phrase asks, so a card that selects by the fact and a card that conditions on it cannot come
        /// to disagree about what "in the discard" means.</para>
        ///
        /// <para><b>"Enemy" is read from the ACTOR, as every relation in this catalog is.</b> The
        /// candidate is on Rowenna's own side - the selection declares
        /// <see cref="SourceTeamRelation.AlliedWithSource"/> - so "an enemy hero" means the same thing
        /// measured from either of them, and asking it of the actor keeps one reading rather than
        /// inventing a relation measured from the subject. It also means the candidate can never be
        /// found in its own neighbourhood, so nothing needs excluding there.</para>
        ///
        /// <para>Optional and defaulting to false, so every unit selection composed before Rowenna
        /// keeps its exact candidate set without naming it - and a parameter added to a registered
        /// capability moves no pinned byte, because the identity walker digests a capability's
        /// reference and its Active-effect definitions and never its parameter schema.</para>
        /// </summary>
        public const string AdjacentToAnEnemyHeroOrHoldingADiscardParameterName =
            "adjacentToAnEnemyHeroOrHoldingADiscard";

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
        /// <para>Milestone 8.35z53 emptied this list. Whisper's Blue displacement Skills name
        /// <c>synthetic-radius-unit-target@1</c> and that milestone deals her, so the declaration moved
        /// into <see cref="RegisteredHandlers"/> below. It is a declaration over a handler this build
        /// already registers six times, not a new handler type, so the gate's second half - a new
        /// handler must be reachable from a showcase card - had nothing to ask of it.</para>
        ///
        /// <para>Milestone 8.39b refilled it with one and MILESTONE 8.39i HAS EMPTIED IT AGAIN.
        /// Cutter's Grappling Bolt is the only face in the corpus that targets an obstacle, the
        /// milestone that deals her is what moved the declaration into <see cref="RegisteredHandlers"/>
        /// below, and both halves of the gate were satisfied in that one commit: her Silver basic is a
        /// card a configured match puts in a hand, and the Red Grapnel showcase Skill is the consumer a
        /// new handler TYPE owes. The list is kept rather than deleted for the same reason 8.35z53 kept
        /// it empty: the next declaration this file defers finds the name already here.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new ChooseTargetHandler(ChooseTargetV3, declaresActivity: false),
                new ChooseTargetHandler(ChooseTargetV4, declaresActivity: true),
                new PrintedIconTargetHandler(
                    RadiusTargetV1, PrintedValueChannel.Radius),
                new PrintedIconTargetHandler(
                    RangeTargetV1, PrintedValueChannel.Range),
                new PrintedIconTargetHandler(
                    RadiusHeroTargetV1, PrintedValueChannel.Radius,
                    PrintedIconSelectionSubject.Hero),
                new PrintedIconTargetHandler(
                    RangeHeroTargetV1, PrintedValueChannel.Range,
                    PrintedIconSelectionSubject.Hero),
                new PrintedIconTargetHandler(
                    RadiusMinionTargetV1, PrintedValueChannel.Radius,
                    PrintedIconSelectionSubject.Minion),
                new PrintedIconTargetHandler(
                    RangeMinionTargetV1, PrintedValueChannel.Range,
                    PrintedIconSelectionSubject.Minion),
                new PrintedIconTargetHandler(
                    RangeUnitTargetV1, PrintedValueChannel.Range,
                    PrintedIconSelectionSubject.Unit),
                // Milestone 8.35z53. The Radius half of the printed-icon UNIT pair, which waited in
                // `DeferredHandlers` above until a dealt card named it: Whisper's three Blue
                // displacement Skills are the first, and this is the seventh declaration over the one
                // handler p.9's two icons already share rather than a new type.
                new PrintedIconTargetHandler(
                    RadiusUnitTargetV1, PrintedValueChannel.Radius,
                    PrintedIconSelectionSubject.Unit),
                new HeroAdjacentToMinionHandler(),
                // Milestone 8.39i. The obstacle selection Grappling Bolt names, which waited in
                // `DeferredHandlers` above until a dealt card started it - her Silver basic is that
                // card. Unlike the printed-icon declarations beside it this is its own handler TYPE,
                // because p.18's obstacles include terrain and its candidates are therefore SPACES
                // rather than board objects; what it shares with them is `RangeValueQuery`, so p.9's
                // icon and p.16's items are still read in exactly one place.
                new RangeObstacleTargetHandler()
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
        /// Milestone 8.39b. See <see cref="RangeObstacleTarget"/>.
        ///
        /// <para>It is its own handler rather than an eighth arm of
        /// <see cref="PrintedIconTargetHandler"/> because that handler's candidates are
        /// <see cref="BoardObjectReference"/>s and these are spaces: p.18's obstacles include terrain,
        /// which is a property of the map rather than an object standing on it. What it does share is
        /// the printed total - <see cref="RangeValueQuery"/>, items and all - so p.9's icon is still
        /// read in one place and an item that widens her Range widens this line with it.</para>
        /// </summary>
        private sealed class RangeObstacleTargetHandler
            : IEffectCapabilityHandler, IEffectImmunityOverrideCapabilityHandler
        {
            private static readonly HexDirection[] Directions =
            {
                HexDirection.East, HexDirection.NorthEast, HexDirection.NorthWest,
                HexDirection.West, HexDirection.SouthWest, HexDirection.SouthEast
            };

            /// <summary>
            /// p.18's unqualified "if a unit is immune", which is the arm Grappling Bolt's bare
            /// "ignore immunity" names: the sentence qualifies no activity, so neither does this.
            /// </summary>
            public IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities { get; } =
                Array.AsReadOnly(new[] { RuleActivity.All });

            public EffectCapabilityReference Reference => RangeObstacleTargetV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            "activity",
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(RuleActivity.None)),

                        // Milestone 8.40e. See ExcludeTerrainParameterName: a narrowing of what may be
                        // named, never of what blocks the file, optional and default-off.
                        new EffectCapabilityParameter(
                            ExcludeTerrainParameterName, EffectValueKind.Boolean, isRequired: false)
                    },
                    new[]
                    {
                        new EffectOutputBinding(
                            ChosenObstacleSpaceOutputName, EffectValueKind.Coordinate),
                        new EffectOutputBinding(
                            ObstacleApproachSpaceOutputName, EffectValueKind.Coordinate),

                        // Milestone 8.40e. The actor-side twin, for the sentence that moves the target
                        // rather than the hero.
                        new EffectOutputBinding(
                            ActorApproachSpaceOutputName, EffectValueKind.Coordinate)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var options = Options(context);
                return options.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor,
                        options.Select(option => EffectValue.Space(option.Obstacle)), 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var chosen = selected != null && selected.Count == 1 &&
                    selected[0].Kind == EffectValueKind.Coordinate
                        ? Options(context).FirstOrDefault(
                            option => option.Obstacle.Equals(selected[0].Coordinate))
                        : null;
                if (chosen == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact(
                        ChosenObstacleSpaceOutputName, EffectValue.Space(chosen.Obstacle))
                };

                // Milestone 8.40e. Each approach space goes to the node that asked for it and to no
                // other: Grappling Bolt binds the obstacle-side one, Get over here! the actor-side one,
                // and a fact a node did not declare is a snapshot the build refuses to read back. Every
                // declaration written before this milestone binds the obstacle-side output, so every
                // one of them publishes exactly the pair it always published.
                if (context.Publishes(ObstacleApproachSpaceOutputName))
                {
                    facts.Add(new EffectOutcomeFact(
                        ObstacleApproachSpaceOutputName, EffectValue.Space(chosen.Approach)));
                }

                if (context.Publishes(ActorApproachSpaceOutputName))
                {
                    facts.Add(new EffectOutcomeFact(
                        ActorApproachSpaceOutputName, EffectValue.Space(chosen.ActorApproach)));
                }

                return EffectExecutionResult.Applied(facts);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Options(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// Derived on every ask - the offer, the answer and a restored pause - for the reason every
            /// other selection here re-derives: a unit that has moved since the question opened has
            /// changed which space is the first obstacle on its file.
            /// </summary>
            private static IReadOnlyList<GrappleOption> Options(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.CanDisplace || !context.CardState.IsAvailable)
                {
                    return Array.Empty<GrappleOption>();
                }

                var activity = (RuleActivity)context.Input("activity").IntegerValue;
                if (activity == RuleActivity.None || !Enum.IsDefined(typeof(RuleActivity), activity))
                {
                    return Array.Empty<GrappleOption>();
                }

                var board = components.Board;
                var actor = BoardObjectReference.ForHero(context.ActorId);
                if (board == null || !board.TryGetObjectCoordinate(actor, out var actorAt))
                {
                    return Array.Empty<GrappleOption>();
                }

                var printed = RangeValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications,
                    ActionValueApplicabilityQuery.For(
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions));
                if (printed.Total <= 0)
                {
                    // A card printing no Range icon has no such bound and p.16 does not give it one,
                    // which is the reading the printed-icon family already records.
                    return Array.Empty<GrappleOption>();
                }

                // Milestone 8.40e. Unbound is the absence of the clause, which is today's set - every
                // obstacle p.18 names. Bound, the offer is narrowed to the obstacles that are units or
                // tokens; the walk below is untouched, because the narrowing is about what may be
                // named and the stop is about what stands in the way.
                var excludeTerrain = context.Input(ExcludeTerrainParameterName);
                var unitsAndTokensOnly =
                    excludeTerrain.Kind == EffectValueKind.Boolean && excludeTerrain.BooleanValue;

                var found = new List<GrappleOption>();
                foreach (var direction in Directions)
                {
                    for (var step = 1; step <= printed.Total; step++)
                    {
                        var at = StraightLineQuery.Step(actorAt, direction, step);
                        if (!TryClassifySpace(board, at, out var isTerrain))
                        {
                            // Off the map is the absence of a space rather than an obstacle, so the
                            // walk ends without offering anything further along this file.
                            break;
                        }

                        // p.18 names the three kinds in one sentence: "Units, terrain (including
                        // printed on the board), tokens". Stated here rather than borrowed from
                        // ObstacleQuery, whose enterability reading folds "off the map" in with
                        // terrain - a distinction this clause has to keep.
                        if (!isTerrain && !board.TryGetObjectAt(at, out _) &&
                            !context.Modifications.AnyOverlayMakesAnObstacleOf(at))
                        {
                            continue;
                        }

                        // The first obstacle on this file, and therefore the only candidate it can
                        // ever offer - anything beyond it has this one "between you". That holds
                        // whether or not this one may be targeted, which is why the loop breaks
                        // either way: an immune unit still occupies its file, and so does a heap of
                        // terrain a narrowed selection may not name.
                        if (MayBeTargeted(context, board, at, actor, activity) &&
                            (!unitsAndTokensOnly || board.TryGetObjectAt(at, out _)))
                        {
                            found.Add(new GrappleOption(
                                at,
                                StraightLineQuery.Step(actorAt, direction, step - 1),
                                StraightLineQuery.Step(actorAt, direction, 1)));
                        }

                        break;
                    }
                }

                return found.OrderBy(option => option.Obstacle.Q)
                    .ThenBy(option => option.Obstacle.R)
                    .ToArray();
            }

            /// <summary>
            /// Whether a coordinate is a space this map declares at all - playable or terrain. p.19
            /// makes terrain an obstacle "including the water spaces surrounding the board", so the
            /// ring is a legal thing to grapple to; the void past it is not a thing at all.
            /// </summary>
            private static bool TryClassifySpace(
                IBoardDisplacementAuthority board, HexCoordinate at, out bool isTerrain)
            {
                foreach (var space in board.MapDefinition.Spaces)
                {
                    if (space.Coordinate.Equals(at))
                    {
                        isTerrain = space.Kind == BoardMapSpaceKind.Terrain;
                        return true;
                    }
                }

                isTerrain = false;
                return false;
            }

            /// <summary>
            /// p.18's immunity, asked only of the obstacles that can carry it. Terrain and tokens are
            /// obstacles that are not units, and the Immune keyword is a rule about units, so nothing
            /// is asked on their behalf.
            /// </summary>
            private static bool MayBeTargeted(
                EffectCapabilityContext context,
                IBoardDisplacementAuthority board,
                HexCoordinate at,
                BoardObjectReference actor,
                RuleActivity activity) =>
                !board.TryGetObjectAt(at, out var standing) ||
                standing.Kind == BoardObjectKind.Token ||
                !TargetQuery.IsPrevented(
                    standing.AsRuleObject(),
                    activity,
                    actor.AsRuleObject(),
                    board,
                    context.Participants,
                    context.Modifications,
                    context.ImmunityOverrides,
                    activity);

            private sealed class GrappleOption
            {
                public GrappleOption(
                    HexCoordinate obstacle, HexCoordinate approach, HexCoordinate actorApproach)
                {
                    Obstacle = obstacle;
                    Approach = approach;
                    ActorApproach = actorApproach;
                }

                public HexCoordinate Obstacle { get; }

                /// <summary>One step back from the obstacle: where the ACTOR ends up beside it.</summary>
                public HexCoordinate Approach { get; }

                /// <summary>
                /// One step out from the actor: where the OBSTACLE ends up beside him. Milestone
                /// 8.40e. The two coincide on an adjacent obstacle only in the sense that neither
                /// piece has anywhere to travel - the obstacle-side value is then the actor's own
                /// space and this one is the obstacle's own space.
                /// </summary>
                public HexCoordinate ActorApproach { get; }
            }
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
            private readonly PrintedIconSelectionSubject subject;

            public PrintedIconTargetHandler(
                EffectCapabilityReference reference,
                PrintedValueChannel channel,
                PrintedIconSelectionSubject subject = PrintedIconSelectionSubject.BoardObject)
            {
                Reference = reference;
                this.channel = channel;
                this.subject = subject;

                // Milestone 8.88. One declaration for every arm: each filter the family composes, on
                // every arm it can mean something on, each optional but the ones the arm always required.
                Schema = new EffectCapabilityParameterSchema(
                    PrintedIconParameters(subject, channel),
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
                        : Enumerable.Empty<EffectOutputBinding>())
                    .Concat(subject == PrintedIconSelectionSubject.Hero
                        ? new[] { new EffectOutputBinding(ChosenHeroSpaceOutputName, EffectValueKind.Coordinate) }
                        : Enumerable.Empty<EffectOutputBinding>()));
            }

            public EffectCapabilityReference Reference { get; }

            /// <summary>Which kind this arm offers. Milestone 8.88, for the refusal table.</summary>
            public PrintedIconSelectionSubject Subject => subject;

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

                    var heroOutcomes = new List<EffectOutcomeFact>
                    {
                        new EffectOutcomeFact(
                            ChosenHeroOutputName, EffectValue.Hero(selected[0].HeroId))
                    };
                    // A declared coordinate lets a following object operation consume the same
                    // selected hero. Existing nodes that request only identity remain unchanged.
                    if (context.Publishes(ChosenHeroSpaceOutputName) && context.Components?.Board != null &&
                        context.Components.Board.TryGetObjectCoordinate(
                            BoardObjectReference.ForHero(selected[0].HeroId), out var heroAt))
                        heroOutcomes.Add(new EffectOutcomeFact(ChosenHeroSpaceOutputName, EffectValue.Space(heroAt)));
                    return heroOutcomes;
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

            /// <summary>
            /// Milestone 8.88. The minions this arm offers, from the family's one pipeline.
            /// </summary>
            private IReadOnlyList<MinionId> MinionCandidates(EffectCapabilityContext context) =>
                Offered(context)
                    .Where(target => target.Kind == BoardObjectKind.Minion)
                    .Select(target => target.MinionId)
                    .ToArray();

            /// <summary>
            /// Milestone 8.88. The heroes this arm offers, from the family's one pipeline. The acting
            /// hero is among them only where the sentence includes them.
            /// </summary>
            private IReadOnlyList<HeroId> HeroCandidates(EffectCapabilityContext context) =>
                Offered(context)
                    .Where(target => target.Kind == BoardObjectKind.Hero)
                    .Select(target => target.HeroId)
                    .ToArray();

            /// <summary>
            /// The spaces of every object an object arm offers, in canonical axial order - the disk's
            /// own order, which every clause preserves.
            /// </summary>
            private IReadOnlyList<HexCoordinate> Candidates(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                if (board == null)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var spaces = new List<HexCoordinate>();
                foreach (var target in Offered(context))
                {
                    if (board.TryGetObjectCoordinate(target, out var at))
                    {
                        spaces.Add(at);
                    }
                }

                return spaces;
            }

            /// <summary>
            /// Milestone 8.88. What this arm offers: its own kinds through the family's one pipeline. A
            /// hero, minion or unit arm requires a side; an object arm may leave it unbound.
            /// </summary>
            private IReadOnlyList<BoardObjectReference> Offered(EffectCapabilityContext context) =>
                PrintedIconSelection(
                    context,
                    channel,
                    subject == PrintedIconSelectionSubject.Hero
                        ? PrintedIconKinds.Heroes
                        : subject == PrintedIconSelectionSubject.Minion
                            ? PrintedIconKinds.Minions
                            : subject == PrintedIconSelectionSubject.Unit
                                ? PrintedIconKinds.Units
                                : PrintedIconKinds.All,
                    relationRequired: subject != PrintedIconSelectionSubject.BoardObject);
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

                // Milestone 8.90, the user's ruling on open question 36: an immune hero is never the
                // one this clause chooses, exactly as Shock's radius selection has always read it -
                // asked for the action the text is running inside.
                var activity = context.EnclosingActivity;
                return board.Occupancies
                    .Where(occupancy =>
                        HexDistanceQuery.Calculate(origin, occupancy.Coordinate) == 1 &&
                        TargetQuery.StandsInRelationTo(
                            relation, BoardObjectReference.ForHero(occupancy.HeroId),
                            context.ActorId, context.Participants, board) &&
                        (activity == RuleActivity.None || !TargetQuery.IsPrevented(
                            RuleObject.ForHero(occupancy.HeroId),
                            activity,
                            RuleObject.ForHero(context.ActorId),
                            board,
                            context.Participants,
                            context.Modifications,
                            context.ImmunityOverrides)))
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
