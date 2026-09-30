using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 6.4's two displacement capabilities: rulebook p.11's direct Move and p.18's Object Push.
    ///
    /// They are two capability IDs rather than one relocation handler because the rulebook gives them
    /// different rules and <c>CARD_EFFECT_CAPABILITIES.md</c> forbids treating Movement, direct Move, and
    /// Object Push as interchangeable. Neither emits a movement fact, neither can become Fast Travel, and
    /// neither requests a Lane Push: p.18 states outright that "displacing a minion miniature (by moving,
    /// pushing, or swapping it) has no impact on the Minion Battle".
    ///
    /// Both take their optionality from the node's own <see cref="EffectFailurePolicy"/> rather than a
    /// parameter of their own. That is p.11's rule expressed in milestone 6.2a's existing vocabulary:
    /// "unless the distance is optional ('up to 2 spaces'), or the move itself is optional ('you may
    /// move'), you must move the exact number of spaces, or stop performing the action at that step".
    /// A mandatory node therefore means exactly that distance, and an `up to` node makes the distance the
    /// decision - which is the same distinction the four failure policies already carry.
    ///
    /// Milestone 8.8 adds the one thing that distinction could not carry. Wasp's Kinetic Repulse prints
    /// "Push up to 2 enemy units adjacent to you 3 spaces": the `up to` is over the units and the spaces
    /// are exact, and a node has one policy. So the Push declarations now say the count and the distance
    /// separately - a declared maximum number of targets beside the declared distance, and a printed
    /// least distance beside it that <see cref="AdmittedDistances"/> reads - and the node's policy keeps
    /// its p.10 meaning over the step as a whole. Kinetic Blast's "3 or 4 spaces" is the first distance
    /// a card writes as a choice rather than as a number.
    ///
    /// Registered into the one shared catalog for the reason milestone 6.2 records: a match has one
    /// capability contract set, and the snapshot records it as one list.
    /// </summary>
    public static class SyntheticDisplacementCapabilities
    {
        public static EffectCapabilityId DirectMove { get; } =
            new EffectCapabilityId("synthetic-direct-move");

        public static EffectCapabilityId ObjectPush { get; } =
            new EffectCapabilityId("synthetic-object-push");

        /// <summary>
        /// Milestone 6.5a. p.18's Push with the target the definition names rather than the adjacency
        /// set <see cref="ObjectPush"/> derives.
        ///
        /// A second capability ID rather than a second revision, because the two are not versions of
        /// one declaration - both are live, and a card that pushes "multiple targets up to X spaces"
        /// and a card that pushes one named unit are different declarations of the same rule. It is
        /// not a second implementation of that rule either: the geometry, the immunity filter, the
        /// one-pre-effect-state resolution, the farthest-first commit, and p.18's zero-space result all
        /// live in the shared routine below, and these two handlers differ only in how their target set
        /// is produced. <c>CARD_EFFECT_CAPABILITIES.md</c> names this move for exactly this case: when
        /// existing parameters cannot express an effect, add a capability rather than overload a term.
        /// They could not - a coordinate cannot say "every adjacent object", and a boolean beside it
        /// would leave one parameter meaningless, which the definition validator refuses as ambiguous.
        /// </summary>
        public static EffectCapabilityId PushTarget { get; } =
            new EffectCapabilityId("synthetic-push-target");

        /// <summary>
        /// Revision 3, minted at milestone 8.6: revision 2's declaration with the `towardActor` boolean
        /// replaced by a declared <see cref="DirectMoveDestinationPolicy"/>.
        ///
        /// Revision 2 - milestone 6.8k's, which widened the subject from a typed hero to a typed
        /// coordinate - is retired rather than kept beside it, exactly as 6.8k retired revision 1 when
        /// it widened that subject. The registration list records contracts, and two live revisions of
        /// one declaration would let a definition ask the same capability a question it can no longer
        /// answer the same way: a boolean cannot express Wasp's orbit, and a policy that also carried
        /// the boolean could state two narrowings at once.
        /// </summary>
        public static EffectCapabilityReference DirectMoveV3 =>
            new EffectCapabilityReference(DirectMove, 3);

        /// <summary>
        /// The destination-policy parameter's name, spelled once so a definition cannot mistype it and
        /// so the enum it carries has exactly one reading site.
        /// </summary>
        public const string DestinationPolicyParameterName = "destinationPolicy";

        /// <summary>
        /// Milestone 8.21. The one space
        /// <see cref="DirectMoveDestinationPolicy.NamedSpace"/> moves into, bound from the node that
        /// published it.
        ///
        /// <para>Optional, and defaulting to nothing, so every declaration written before this
        /// milestone keeps its exact candidate set - the same shape milestone 8.20 gave
        /// <c>excludeTarget</c>. A policy that names a space and binds none admits nothing, which is
        /// what an unresolved binding means everywhere else in this catalog.</para>
        /// </summary>
        public const string NamedDestinationParameterName = "destination";

        /// <summary>
        /// Milestone 8.22. The disk
        /// <see cref="DirectMoveDestinationPolicy.WithinActorRadius"/> keeps the destination inside,
        /// written down by the definition rather than read off the card's icon.
        ///
        /// <para>Optional and defaulting to nothing, exactly as
        /// <see cref="NamedDestinationParameterName"/> is, so every declaration written before this
        /// milestone keeps its exact candidate set. It is a separate parameter from
        /// <c>distance</c> because the two bound different things and a card can print them
        /// differently: Sabina's Path to Victory moves a minion <em>one</em> space and keeps it within
        /// <em>three</em>, so one number would have to be both the step and the leash.</para>
        ///
        /// <para><b>Milestone 8.82. It is read by the DECLARED member alone.</b>
        /// <see cref="DirectMoveDestinationPolicy.WithinActorPrintedRadius"/> draws the same disk from
        /// the card's own Radius icon with p.16's items composed onto it, so it asks
        /// <see cref="RadiusValueQuery"/> instead and this parameter means nothing to it - the reading
        /// <see cref="NamedDestinationParameterName"/> and <see cref="BesideSpaceParameterName"/>
        /// already have of a bound belonging to another member. A definition writes one or the other:
        /// text that prints the number declares it here, and text that says "in radius" points at the
        /// icon and declares nothing.</para>
        /// </summary>
        public const string ActorRadiusParameterName = "actorRadius";

        /// <summary>
        /// Milestone 8.61c. The space
        /// <see cref="DirectMoveDestinationPolicy.AdjacentToTheNamedSpace"/> keeps this Move's
        /// destination next to - Min the Dragonmonk's Tiger and Dragon Stance, "You may move 1 space
        /// [1 or 2 spaces] <b>to a space adjacent to the target</b>", where the target is the enclosing
        /// Attack's own and arrives from the node that published its space.
        ///
        /// <para><b>The same spelling p.18's Place uses for the same clause</b> -
        /// <see cref="SyntheticPlacementCapabilities.BesideSpaceParameterName"/>, milestone 8.57b's -
        /// because Rowenna's "into a space adjacent to that enemy hero" and Min's "to a space adjacent
        /// to the target" are one printed shape read by two rules, and a card author who declared the
        /// bound for a Place must not be able to get it wrong for a Move. What differs is which rule
        /// answers it: that one places and this one walks, which is p.18 against p.11.</para>
        ///
        /// <para><b>A coordinate rather than a unit</b>, which is the subject identity milestone 6.4
        /// chose for every repositioning operation in this build and which p.19's one-obstacle-per-space
        /// rule makes unambiguous.</para>
        ///
        /// <para>Optional and unbound everywhere else, so every Move composed before this milestone
        /// keeps its exact candidate set - the shape <see cref="NamedDestinationParameterName"/> and
        /// <see cref="ActorRadiusParameterName"/> both take - and a parameter added to a registered
        /// capability moves no pinned byte, because the identity walker digests a capability's
        /// reference and its Active-effect definitions and never its parameter schema. That is the
        /// same reading milestone 8.57b recorded when it added <see cref="ChooserParameterName"/> to
        /// this very declaration without minting a revision 4.</para>
        /// </summary>
        public const string BesideSpaceParameterName =
            SyntheticPlacementCapabilities.BesideSpaceParameterName;

        /// <summary>
        /// Milestone 8.23. Whether this Move crosses obstacles on its way - Blink Strike's "Move 2
        /// spaces in a straight line <em>through</em> an enemy unit", and Nimble Strike's same
        /// sentence about a hero.
        ///
        /// <para>Optional and defaulting to absent, exactly as
        /// <see cref="NamedDestinationParameterName"/> and <see cref="ActorRadiusParameterName"/>
        /// are, so every declaration written before this milestone keeps its exact candidate set: a
        /// card that says nothing about crossing is a card whose mover is stopped by p.18's
        /// obstacles.</para>
        ///
        /// <para>It is declared on the Move rather than granted to the hero, which is the whole
        /// distinction p.18's <c>Move Through</c> entry draws. A granted
        /// <see cref="RulePermissionKind.MoveThroughObstacles"/> is a property of a unit and lasts
        /// while its source does; this is a property of one printed movement, so nothing about
        /// Tigerclaw changes and the next space he moves into is closed to him again. p.18's Push
        /// has read the same pair since milestone 6.4 - see the <c>ignoresObstacles</c> parameter
        /// beside it - and the two are answered by one transit rule rather than by two.</para>
        ///
        /// <para>It reaches transit alone. Where a Move may END is p.18's other sentence, which no
        /// declaration is offered: a crossing Move still may not stop on an obstacle, which is why
        /// Blink Strike's line needs the space beyond the crossed unit empty before it exists at
        /// all.</para>
        /// </summary>
        public const string MovesThroughObstaclesParameterName = "movesThroughObstacles";

        /// <summary>
        /// Milestone 8.112e. "to a space adjacent to terrain, or a rock token" - Mrak's Treacherous
        /// Ground, Rockslide and Avalanche: the DESTINATION must stand beside terrain, or beside a
        /// token of the named kind, or beside either. The spellings are 8.88's printed-icon adjacency
        /// clauses, and the reading is theirs (<c>PrintedIconSpaceClauses</c>): p.17's Adjacent asked
        /// of the destination's six neighbours, terrain as <c>TerrainPresenceQuery</c> answers it.
        ///
        /// <para>Optional and unbound everywhere else, so every Move composed before this milestone
        /// keeps its exact candidate set; a parameter added to a registered capability moves no pinned
        /// byte.</para>
        /// </summary>
        public const string DestinationAdjacentToTerrainParameterName =
            SyntheticTargetingCapabilities.AdjacentToTerrainParameterName;

        /// <summary>Milestone 8.112e. See <see cref="DestinationAdjacentToTerrainParameterName"/>.</summary>
        public const string DestinationAdjacentToTokenKindParameterName =
            SyntheticTargetingCapabilities.AdjacentToTokenKindParameterName;

        /// <summary>
        /// Milestone 8.57b. WHO decides where this Move goes, where that is not the acting hero -
        /// Rowenna's Throw the Gauntlet, "Place yourself into a space in range adjacent to an enemy
        /// hero in range; <b>that hero</b> may move 1 space".
        ///
        /// <para><b>It states rulebook p.10's own rule where this capability had been assuming the
        /// ordinary case.</b> A step's decisions belong to whoever performs it, and this contract has
        /// moved units that are not the actor since milestone 6.8k - Arien's Noble Blade moves another
        /// unit, Sabina's Path to Victory moves a minion - but every one of those movers is a MINION or
        /// a token, which holds no seat, so handing the choice to the actor was the page's answer
        /// rather than a shortcut. A hero has a seat, and this is the first printed sentence that moves
        /// one at somebody else's instruction: the 2026-08-19 designer answer settles it - "There's no
        /// functional distinction between 'Player' and 'Hero' in Guards of Atlantis II as far as the
        /// rules text is concerned" - so the decision is the mover's.</para>
        ///
        /// <para><b>A bound chooser rather than a role, for the reason
        /// <c>defended-card-forced-move@1</c> names one.</b> With several opponents there is no single
        /// "the" opposing hero, and this one was picked out by an earlier node of the same sentence.
        /// The named hero travels to the pause as <see cref="EffectChoiceRequest.ChooserHeroId"/>,
        /// which milestone 7.1a built so that "another hero discards a card" could put the card choice
        /// to that hero and which milestone 8.35r's decision-owner projection already routes to the
        /// right seat - so nothing about the wire or the seat filter is new here, and the acting seat
        /// is refused as the wrong chooser exactly as it is on every other hero-owned prompt.</para>
        ///
        /// <para><b>A hero this match does not field is a refusal rather than a fallback to the
        /// actor.</b> A declaration that names a mover-chooser and resolves to nobody is a definition
        /// making a claim this vocabulary cannot answer; quietly handing the decision back to the actor
        /// would turn a fault into a different card. That is <c>retrieve-card@1</c>'s own reading of an
        /// unknown chooser, stated here for the same reason.</para>
        ///
        /// <para>Optional and unbound everywhere else, so every Move composed before this milestone
        /// keeps its exact prompt and its exact chooser - and a parameter added to a registered
        /// capability moves no pinned byte, because the identity walker digests a capability's
        /// reference and its Active-effect definitions and never its parameter schema.</para>
        /// </summary>
        public const string ChooserParameterName = "chooser";

        /// <summary>
        /// Milestone 8.57b. Whether this Move actually happened, published only on success - the "if
        /// they do" of Rowenna's Throw the Gauntlet, "that hero may move 1 space; <b>if they do</b>,
        /// gain 2 coins".
        ///
        /// <para><b>It is the third of one family and exists for the identical recorded reason as the
        /// first two.</b> <c>retrieve-card@1</c>'s <c>didRetrieve</c> (milestone 8.40i) and
        /// <c>gain-resource@1</c>'s <c>didGrant</c> (milestone 8.57a) both record why a binding cannot
        /// carry the condition where the dependent step is about somebody else: the coins here are
        /// ROWENNA's, and nothing about them can be bound from a move the opponent made. So the fact
        /// is published as a Boolean an <see cref="EffectNodeKind.Conditional"/> over
        /// <c>synthetic-is-flagged@1</c> reads, and a step declined or impossible publishes nothing -
        /// an unbound flag reads as false, which is exactly what the printed "if" asks for.</para>
        ///
        /// <para><b>Why the destination could not carry it.</b> <c>moved</c> is a coordinate, and no
        /// predicate in this vocabulary asks whether a coordinate was published; the distance output
        /// beside it is an integer with the same problem. Both are also published only on the same
        /// success this is, so nothing is being answered twice - this is that one success in the shape
        /// a Conditional can read.</para>
        ///
        /// <para>Published only for a node that declared it, so every Move composed before this
        /// milestone is byte-identical without naming it.</para>
        /// </summary>
        public const string DidMoveOutputName = "didMove";

        /// <summary>
        /// Revision 3, minted at milestone 8.8: revision 2's declaration plus the two things Wasp's
        /// kinetic Skills print that a node's failure policy could not say - the fewest spaces the card
        /// names, and how many units it may push.
        ///
        /// Revision 2 - milestone 6.4c's, which added the eligible piece kind - is retired rather than
        /// kept beside it, for the reason milestone 6.8k and milestone 8.6 retired the revisions they
        /// replaced: the registration list records contracts, and a definition bound to a declaration
        /// that cannot state its count bound would be pushing everything standing there while its
        /// printed text says two.
        ///
        /// Both Push declarations move to revision 3 together because both resolve through one routine
        /// and one pair of target finders, so a parameter added to only the named one would leave the
        /// batch reading a declaration it does not have. Only the shared reading moves to both: the
        /// count bound and the target relation are the batch's alone, because the named push is handed
        /// one object by a selection node that has already answered both questions.
        /// </summary>
        public static EffectCapabilityReference ObjectPushV3 =>
            new EffectCapabilityReference(ObjectPush, 3);

        /// <summary>Revision 3, minted at milestone 8.8, for the same reason the batch's is.</summary>
        public static EffectCapabilityReference PushTargetV3 =>
            new EffectCapabilityReference(PushTarget, 3);

        /// <summary>
        /// The printed-least-distance parameter's name, spelled once so a definition cannot mistype it.
        /// A card naming one distance declares the same number here; Kinetic Blast's "3 or 4 spaces"
        /// declares 3 beside a distance of 4. Direct Move also reads the optional floor when a card's
        /// destination clause makes standing still incapable of satisfying its printed instruction.
        /// </summary>
        public const string LeastDistanceParameterName = "leastDistance";

        /// <summary>
        /// The count bound's parameter name: the most units the card lets one push take.
        ///
        /// Zero is the named absence, and it is the reading every Push declaration had before milestone
        /// 8.8 - p.18's batch takes every object adjacent to the pusher, and a card that says so names
        /// no number for this to carry. A card that does name one, as both of Wasp's kinetic Skills do,
        /// makes which units are pushed a decision, because two of the three standing there is not a
        /// set the rule can pick on the player's behalf.
        /// </summary>
        public const string MaximumTargetsParameterName = "maximumTargets";

        /// <summary>
        /// Milestone 9.8d. The space a push is made FROM when the pusher is not the acting hero -
        /// Mortimer's "that Zombie token may push a unit or a token adjacent to it 1 space". p.18
        /// measures "directly away" from the pushing piece, so its space is the push's origin and
        /// the pieces beside it are what it may push; the acting hero stays the source the push is
        /// judged against for immunity, because it is his card's effect. A bound space with nothing
        /// standing on it pushes nothing. Unbound, the acting hero pushes, as before.
        /// </summary>
        public const string PusherSpaceParameterName = "pusherSpace";

        /// <summary>
        /// The declared side of the board the batch may push, carrying a
        /// <see cref="SourceTeamRelation"/>. Wasp's kinetic Skills print "enemy units"; a card that
        /// names no side declares <see cref="SourceTeamRelation.AnyTeam"/>, which is p.18's own breadth.
        /// </summary>
        public const string TargetRelationParameterName = "targetRelation";

        /// <summary>
        /// Milestone 8.8's stop-cause facts, in the order the batch commits. Each pushed hero gets a
        /// pair - whether an obstacle stopped it, and which hero it was - so a rider hanging off the
        /// push can ask a <see cref="EffectNodeKind.Conditional"/> about one hero at a time and hand the
        /// same hero to whatever the card makes them do.
        ///
        /// <para>Two pairs, because the verified text that reads them pushes up to two units. The batch
        /// itself can stop more, and publishes stop causes only for as many heroes as it has names for;
        /// a card that pushes more and prints a rider for all of them needs a third name declared here,
        /// which is visible at content load - a definition can only bind an output the schema declares -
        /// rather than silently dropped. The pair is heroes only because the fact is a
        /// <see cref="EffectValueKind.Hero"/> and because a minion has no hand to answer a rider with;
        /// p.19's "stopped by an obstacle" is the same event for one, and stays unpublished until a card
        /// prints something a minion can be told to do.</para>
        /// </summary>
        public const string FirstStoppedHeroOutputName = "firstStoppedHero";

        /// <summary>Whether an obstacle stopped the first hero this push moved. See <see cref="FirstStoppedHeroOutputName"/>.</summary>
        public const string FirstHeroWasStoppedOutputName = "firstHeroWasStopped";


        /// <summary>
        /// Milestone 8.39c. The first hero this push carried out of the zone it started in, and
        /// whether there was one - Cutter's Walk the Plank and Few More Steps, "Push an enemy hero
        /// adjacent to you up to 4 spaces; <b>if that hero is pushed into another zone</b>, that hero
        /// discards a card, or is defeated."
        ///
        /// <para><b>It is the exact sibling of <see cref="FirstStoppedHeroOutputName"/>, and that is
        /// the whole design.</b> Wasp's Kinetic Repulse prints "Push up to 2 enemy units adjacent to
        /// you 3 spaces; if a pushed hero is stopped by an obstacle, that hero discards a card, if
        /// able" - a push and a rider about a hero the push did something to, read off the push itself.
        /// Cutter's sentence is that shape with a different rider, so it is a second pair here rather
        /// than a predicate somewhere else.</para>
        ///
        /// <para><b>It is NOT <c>after-push-trigger@1</c>, which is the near miss worth naming.</b>
        /// That declaration is how an unlocked ULTIMATE watches a push some other card performed -
        /// Sabina's Big Sodding Gun, "If you push an enemy hero, that hero discards a card, or is
        /// defeated", a p.16 permanent ability with no push of its own. Cutter's clause is a
        /// consequence on the same card that does the pushing, so it needs no watcher and no
        /// checkpoint.</para>
        ///
        /// <para><b>Judged on origin against final destination, never on the spaces between.</b> p.18's
        /// Push travels one file directly away from the pusher, so a path may leave a zone and re-enter
        /// it where the map's zones interleave along that file; a hero who ends where they began is not
        /// "pushed into another zone", whatever they crossed on the way. Read from the resolved path
        /// rather than from the board afterwards, for the reason the stopped pair is: a later commit in
        /// the same batch must not be able to change what this push did.</para>
        ///
        /// <para>Published only for heroes, and only for a push that actually displaced one - p.18's
        /// "if you push" is what happened rather than what was tried, so a hero the geometry left
        /// standing still has crossed nothing.</para>
        /// </summary>
        public const string FirstZoneCrossingHeroOutputName = "firstZoneCrossingHero";

        /// <summary>
        /// Whether this push carried a hero out of the zone it started in. See
        /// <see cref="FirstZoneCrossingHeroOutputName"/>.
        /// </summary>
        public const string FirstHeroCrossedAZoneOutputName = "firstHeroCrossedAZone";
        /// <summary>The second stopped hero, in commit order. See <see cref="FirstStoppedHeroOutputName"/>.</summary>
        public const string SecondStoppedHeroOutputName = "secondStoppedHero";

        /// <summary>Whether an obstacle stopped the second hero this push moved.</summary>
        public const string SecondHeroWasStoppedOutputName = "secondHeroWasStopped";

        /// <summary>
        /// Milestone 8.35z30. How far the subject of a direct Move actually went, which is
        /// what Silverarrow's Divert Attention and Lead Astray mean by "that number" (RULING 37).
        /// </summary>
        public const string RealisedDistanceOutputName = "distance";

        /// <summary>
        /// Milestone 9.21f. How many spaces the ONE unit a named Push took actually travelled - Razzle's
        /// Magic Trick and Aaaand it's gone!, "for every space the target moved" (RZ-Q10).
        ///
        /// <para>The Push's own <c>distance</c> fact is the distance CHOSEN, which p.18 then stops short
        /// wherever an obstacle stands behind the target, so it is not "every space the target moved".
        /// This is the resolved path's realised length, 0 for a push the geometry left standing or
        /// the commit refused. Declared on the named Push alone, because a batch has no one target;
        /// published only to a node that declares it, so every Push composed before it produces
        /// exactly the facts it did - and an added OUTPUT moves no corpus digest (the identity walker
        /// digests a capability's reference and Active effects, never its schema).</para>
        /// </summary>
        public const string PushedSpacesOutputName = "pushedSpaces";

        /// <summary>
        /// Direct self Move ending beside an eligible enemy hero for a following Push. The player
        /// chooses the destination here and the named hero through the ordinary adjacent selection.
        /// GA-Q1 permits the current space when it already satisfies that target requirement.
        /// </summary>
        public static EffectCapabilityReference MoveAdjacentToEnemyHeroForPushV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("move-adjacent-to-enemy-hero-for-push"), 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new DirectMoveHandler(DirectMoveV3, supportsImmunityOverrides: true),
                new ObjectPushHandler(),
                new PushTargetHandler(),
                // Milestone 8.37, the milestone that deals Tali the Spiritcaller. Cold Snap and
                // Snowstorm are dealt primary Skills that name both, so the standing rule the
                // deferred list below states is satisfied and they join the one registered set.
                new ChooseDirectionHandler(),
                new BatchDirectMoveHandler(),
                new DirectMoveHandler(MoveAdjacentToEnemyHeroForPushV1,
                    supportsImmunityOverrides: true, requiresAdjacentEnemyHeroForPush: true)
            };

        /// <summary>
        /// Milestone 8.37 moved <c>synthetic-choose-direction@1</c> and
        /// <c>synthetic-batch-direct-move@1</c> into <see cref="Handlers"/> beside the dealing that
        /// starts them. Milestone 8.41e registers Garrus's adjacency-qualified self Move beside
        /// his dealt consumer; it shares ordinary direct-Move execution and narrows only endpoints.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// Milestone 8.37. p.19's six directions, in the same order
        /// <see cref="HexDirection"/> declares them - the order every offer and every test reads them
        /// in, so a printed direction has one canonical position rather than one per caller.
        /// </summary>
        public static readonly IReadOnlyList<HexDirection> AllDirections = Array.AsReadOnly(new[]
        {
            HexDirection.East, HexDirection.NorthEast, HexDirection.NorthWest,
            HexDirection.West, HexDirection.SouthWest, HexDirection.SouthEast
        });

        public static EffectCapabilityId ChooseDirection { get; } =
            new EffectCapabilityId("synthetic-choose-direction");

        /// <summary>
        /// "Choose a direction" - Cold Snap, Snowstorm and Blizzard's opening sentence, and the
        /// unconstrained actor choice phase one's costed design named as the one genuinely new half of
        /// the three faces (the batch half being p.18's own simultaneity, reused rather than
        /// reinvented). Unlike every other direction-shaped reading in the build - Brogan's charge,
        /// Silverarrow's line, Blink Strike's crossing - this one derives no candidate from the board at
        /// all: p.17's six directions are always all six, whichever way the actor is facing and whatever
        /// stands around her, because the card names no origin and no destination to constrain them.
        /// </summary>
        public static EffectCapabilityReference ChooseDirectionV1 =>
            new EffectCapabilityReference(ChooseDirection, 1);

        /// <summary>The direction the actor chose, published for the batch Move to bind.</summary>
        public const string ChosenDirectionOutput = "chosenDirection";

        public static EffectCapabilityId BatchDirectMove { get; } =
            new EffectCapabilityId("synthetic-batch-direct-move");

        /// <summary>
        /// "Move each enemy unit in radius 1 space in that direction, if able" - the printed batch half
        /// of Cold Snap, Snowstorm and Blizzard, distinct from <see cref="ObjectPushV3"/>'s batch because
        /// the geometry is p.11's direct Move (a fixed step in a NAMED direction, the same for every
        /// unit) rather than p.18's Push (away from a pusher) - two different rules the sentence itself
        /// tells apart: "Move", never "Push".
        ///
        /// <para><b>No count is a decision and nobody is asked which units.</b> The printed sentence
        /// names "each enemy unit in radius" outright, so - unlike Wasp's kinetic Push, which lets a
        /// printed maximum turn the target SET into a choice - every eligible unit is affected and there
        /// is nothing here for a player to select.</para>
        ///
        /// <para><b>Simultaneous, per Arty's own ruling</b> ("Since it's the same clause, it happens at
        /// the same time"): every unit's destination is computed against the board as it stood before
        /// this step touched anything, never against a space another unit in the same batch has already
        /// vacated. A unit whose computed destination is off the map, obstructed at that same pre-effect
        /// moment, or made an obstacle by a standing overlay simply does not move - the printed "if
        /// able", read per unit rather than as a gate on the whole step, which is why the node itself is
        /// <see cref="EffectFailurePolicy.Mandatory"/> and always applies, even against an empty
        /// radius.</para>
        ///
        /// <para><b>Immunity is asked per unit</b>, exactly as every other roster selection asks it,
        /// because this is Tali's own action affecting an enemy unit and p.18's cross-cutting rule
        /// reaches a Move the same way it reaches a Push or a Remove.</para>
        /// </summary>
        public static EffectCapabilityReference BatchDirectMoveV1 =>
            new EffectCapabilityReference(BatchDirectMove, 1);

        /// <summary>The printed Radius disk this batch reads its units from.</summary>
        public const string BatchRadiusParameterName = "radius";

        /// <summary>The direction every affected unit steps in - bound from <see cref="ChooseDirectionV1"/>.</summary>
        public const string BatchDirectionParameterName = "direction";

        /// <summary>How many units this batch actually moved, published for a test or a rider to read.</summary>
        public const string BatchMovedCountOutput = "movedCount";

        /// <summary>
        /// Rulebook p.11's movement written inside card text: "This movement follows the same rules and
        /// restrictions as a Movement action, except: it cannot be used to Fast Travel; effects which
        /// stop you from performing a Movement action do not prevent movement in the card text."
        ///
        /// It moves any displaceable subject. p.11 says card text may move "another unit or a token",
        /// and since milestone 6.8k the subject parameter is a typed coordinate naming whatever stands
        /// there - a hero, a minion, or a token - the same space-as-identity Object Push below already
        /// uses for its targets, which one-obstacle-per-space makes unambiguous and which keeps a token
        /// expressible without widening the typed value union. The reachability beneath is unchanged:
        /// the port has served every kind since milestone 6.4a widened it.
        ///
        /// The mover is a bound parameter rather than always the actor, so the source-relative policy has
        /// a real subject: `move a unit towards you` is this capability under
        /// <see cref="DirectMoveDestinationPolicy.TowardActor"/>, not a separate Pull keyword, exactly
        /// as `BOARD_CAPABILITIES.md` requires. A definition fills the
        /// parameter from a prior target-selection node's published space, never from a role - the frame
        /// resolves roles without board access, so no role can supply a coordinate, and the definition
        /// validator refuses one that tries at content load.
        /// </summary>
        private sealed class DirectMoveHandler :
            IEffectCapabilityHandler,
            IEffectImmunityOverrideCapabilityHandler,
            IEffectActorRelocationCapabilityHandler,
            IEffectZeroSpaceMoveCapabilityHandler
        {
            private readonly bool supportsImmunityOverrides;
            private readonly bool requiresAdjacentEnemyHeroForPush;

            public DirectMoveHandler(
                EffectCapabilityReference reference,
                bool supportsImmunityOverrides,
                bool requiresAdjacentEnemyHeroForPush = false)
            {
                Reference = reference;
                this.supportsImmunityOverrides = supportsImmunityOverrides;
                this.requiresAdjacentEnemyHeroForPush = requiresAdjacentEnemyHeroForPush;
            }

            public EffectCapabilityReference Reference { get; }

            public IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                supportsImmunityOverrides
                    ? DirectMoveActivity
                    : Array.Empty<RuleActivity>();

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter("mover", EffectValueKind.Coordinate),
                    new EffectCapabilityParameter("distance", EffectValueKind.Integer),
                    new EffectCapabilityParameter(
                        DestinationPolicyParameterName,
                        EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.AllExcept(
                            DirectMoveDestinationPolicy.None)),
                    new EffectCapabilityParameter(
                        NamedDestinationParameterName, EffectValueKind.Coordinate, isRequired: false),
                    new EffectCapabilityParameter(
                        ActorRadiusParameterName, EffectValueKind.Integer, isRequired: false),

                    // Milestone 8.61c. Min's Tiger and Dragon Stance, "to a space adjacent to the
                    // target" - the ring the destination has to stand on, named by an earlier node.
                    // Optional and unbound everywhere else, so every Move composed before it keeps
                    // its exact candidate set.
                    new EffectCapabilityParameter(
                        BesideSpaceParameterName, EffectValueKind.Coordinate, isRequired: false),
                    new EffectCapabilityParameter(
                        MovesThroughObstaclesParameterName,
                        EffectValueKind.Boolean,
                        isRequired: false),
                    new EffectCapabilityParameter(
                        LeastDistanceParameterName, EffectValueKind.Integer, isRequired: false),

                    // Milestone 8.57b. Rowenna's Throw the Gauntlet, "that hero may move 1 space" -
                    // the mover's own decision. Optional and unbound everywhere else, so every Move
                    // composed before it keeps the actor as its chooser.
                    new EffectCapabilityParameter(
                        ChooserParameterName, EffectValueKind.Hero, isRequired: false),

                    // Milestone 8.112e. Mrak's "to a space adjacent to terrain, or a rock token" -
                    // see DestinationAdjacentToTerrainParameterName.
                    new EffectCapabilityParameter(
                        DestinationAdjacentToTerrainParameterName, EffectValueKind.Boolean,
                        isRequired: false),
                    new EffectCapabilityParameter(
                        DestinationAdjacentToTokenKindParameterName, EffectValueKind.TokenKind,
                        isRequired: false)
                },
                new[]
                {
                    new EffectOutputBinding("moved", EffectValueKind.Coordinate),
                    // Milestone 8.35z30. Declared beside the destination and published only
                    // when a definition binds it, which is the shape Object Push already has: a node
                    // that says nothing about the distance produces exactly the facts it did before.
                    new EffectOutputBinding(RealisedDistanceOutputName, EffectValueKind.Integer),

                    // Milestone 8.57b. The same success as a Boolean, for a card whose "if they do"
                    // gates a step about somebody else - see DidMoveOutputName.
                    new EffectOutputBinding(DidMoveOutputName, EffectValueKind.Boolean)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                // Milestone 8.57b. A declaration naming a mover-chooser this match does not field is
                // asked before anything else, because what follows would otherwise open a prompt
                // addressed to nobody. See ChooserParameterName for why this is a refusal rather than
                // a fall back to the actor.
                if (!TryResolveChooser(context, out var chooser))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var candidates = Candidates(context);
                if (candidates.Count == 0)
                {
                    // p.11: a mandatory exact distance that cannot be walked stops the action at that
                    // step. Under `up to` or `if able` the enclosing node's policy turns this into a
                    // skip rather than a stop, which is why the handler reports the same thing either
                    // way and the node decides what it means.
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Milestone 8.21. A declaration that names its destination has nothing to ask, so it
                // moves rather than opening a prompt with one answer in it - milestone 6.5a's reading
                // of "yourself", applied to where rather than to who. The commit below is the same one
                // the answered path reaches; only the question is missing, because the card asks none.
                if ((DirectMoveDestinationPolicy)context.Input(DestinationPolicyParameterName)
                        .IntegerValue == DirectMoveDestinationPolicy.NamedSpace)
                {
                    return Resume(context, new[] { EffectValue.Space(candidates[0]) });
                }

                // Milestone 8.57b. `EffectChooserRole.None` with a named hero is the shape milestone
                // 7.1a built and `defended-card-forced-move@1` uses: a decision issued to one hero
                // rather than resolved through the frame's roles. A declaration that names nobody is
                // unchanged and still asks the actor.
                return EffectExecutionResult.NeedsChoice(chooser.IsDefined
                    ? new EffectChoiceRequest(
                        EffectChooserRole.None,
                        candidates.Select(candidate => EffectValue.Space(candidate)).ToArray(),
                        1,
                        1,
                        chooser)
                    : new EffectChoiceRequest(
                        EffectChooserRole.Actor,
                        candidates.Select(candidate => EffectValue.Space(candidate)).ToArray(),
                        1,
                        1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 || selected[0].Kind != EffectValueKind.Coordinate)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived against current state rather than trusted from the paused choice: a space
                // legal when the choice opened may have been taken before the answer arrived.
                var destination = selected[0].Coordinate;
                if (!Candidates(context).Contains(destination))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                if (!TryResolveMover(context, out var mover, out var moverAt))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var committed = context.Components.Board.CommitValidatedDisplacement(
                    mover, destination, WalkedThrough(context, mover, destination));
                if (!committed.Accepted)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Milestone 8.35z30, RULING 37. "move up to that number of spaces" points
                // back at how far the first Move actually went, so the Move publishes that number the
                // way Object Push has published its own since milestone 8.21 - as a fact a later node
                // binds, not as a number a second reader re-derives from two coordinates it would have
                // to be handed. It is the straight-line separation between where the subject started
                // and where it stands, which p.19 makes the one distance on this board; a route that
                // walked around an obstacle is still that many spaces from where it began, and the
                // card counts spaces moved rather than steps taken.
                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact("moved", EffectValue.Space(committed.Destination))
                };
                if (context.Publishes(RealisedDistanceOutputName))
                {
                    facts.Add(new EffectOutcomeFact(
                        RealisedDistanceOutputName,
                        EffectValue.Integer(
                            HexDistanceQuery.Calculate(moverAt, committed.Destination))));
                }

                // Milestone 8.57b. The same success as a flag, for the "if they do" of a card whose
                // next clause is about the ACTOR rather than about the mover.
                if (context.Publishes(DidMoveOutputName))
                {
                    facts.Add(new EffectOutcomeFact(
                        DidMoveOutputName, EffectValue.Boolean(true)));
                }

                return EffectExecutionResult.Applied(facts);
            }

            /// <summary>
            /// The same reachability question <see cref="Execute"/> asks, without opening the choice.
            /// An unbound subject leaves the mover unresolvable and the node impossible, which is what
            /// the enclosing walk reads as "not decidable here" when the space comes from a later node.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                !TryResolveChooser(context, out _) || Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : (DirectMoveDestinationPolicy)context.Input(DestinationPolicyParameterName)
                        .IntegerValue == DirectMoveDestinationPolicy.NamedSpace
                        ? EffectExecutionResultKind.Applied
                        : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// Milestone 9.22, user ruling W9: a Move whose sentence names where the mover goes, and
            /// whose mover already stands there, moves it 0 spaces - Get over here!'s target already
            /// beside Bain ("until you are adjacent" is satisfied where it stands), Grappling Bolt's
            /// obstacle Cutter already stands beside. Such a mover is still a legal subject; the Move
            /// itself has no destination to walk to, which its own failure policy decides at
            /// resolution exactly as before. Only a named destination is read: every other policy
            /// offers destinations, and one that offers none has not moved anything 0 spaces.
            /// </summary>
            public bool WouldMoveZeroSpaces(EffectCapabilityContext context)
            {
                var named = context.Input(NamedDestinationParameterName);
                return (DirectMoveDestinationPolicy)context.Input(DestinationPolicyParameterName)
                        .IntegerValue == DirectMoveDestinationPolicy.NamedSpace &&
                    named.Kind == EffectValueKind.Coordinate &&
                    TryResolveChooser(context, out _) &&
                    TryResolveMover(context, out _, out var moverAt) &&
                    moverAt.Equals(named.Coordinate);
            }

            /// <summary>
            /// Milestone 8.57b. The hero this Move's declaration hands the decision to, or the
            /// undefined hero where it hands it to nobody in particular - which is every Move composed
            /// before this milestone and is read as the actor's own decision.
            ///
            /// <para>False means the declaration named a mover-chooser and the match does not field
            /// them, which is a refusal rather than a fallback: see
            /// <see cref="ChooserParameterName"/>. A binding an earlier node has not published yet
            /// arrives as no value at all rather than as a wrong one, so the offer-time walk reads it
            /// the way it reads every other unresolved input - and the mover coordinate it needs is
            /// unresolved at that same moment, which is what already decides the node there.</para>
            /// </summary>
            private static bool TryResolveChooser(
                EffectCapabilityContext context, out HeroId chooser)
            {
                chooser = default;
                var declared = context.Input(ChooserParameterName);
                if (declared.Kind != EffectValueKind.Hero)
                {
                    return true;
                }

                if (!declared.HeroId.IsDefined || context.Participants == null ||
                    !context.Participants.Heroes.Any(hero => hero.Id.Equals(declared.HeroId)))
                {
                    return false;
                }

                chooser = declared.HeroId;
                return true;
            }

            /// <summary>
            /// Milestone 8.35z42. Where this node could leave the ACTING HERO, for a walk
            /// standing before it - the destinations <see cref="Execute"/> would offer, plus his own
            /// space wherever the step can be declined.
            ///
            /// <para><b>Both halves are the node's own wording, not a guess.</b> The destinations are
            /// <see cref="Candidates"/> exactly - the same list the prompt would carry, so a walk that
            /// judges from them judges from spaces the player can really pick. Standing still is the
            /// other answer, and which declarations admit it is already stated twice: p.11's optional
            /// <em>distance</em> puts the mover's own space in <see cref="Candidates"/> through
            /// <see cref="AdmittedDistances.AdmitsStandingStill"/>, and p.10's optional <em>step</em>
            /// - `you may`, `if able` - is the node's failure policy, which is a decision about
            /// whether the node runs at all and therefore lives here rather than in the candidate
            /// walk. A mandatory exact distance admits neither, so a hero who must move is judged only
            /// from the spaces he must move to.</para>
            ///
            /// <para><b>Only the actor.</b> p.11 lets card text move another unit or a token, and where
            /// this node moves one of those the acting hero has not moved at all - so the question is
            /// answered <c>false</c> and the caller keeps reading the board it already has. The subject
            /// is compared by space, which is the identity milestone 6.4 chose for displacement.</para>
            /// </summary>
            public bool TryAnticipateActorSpaces(
                EffectCapabilityContext context, out IReadOnlyList<HexCoordinate> spaces)
            {
                spaces = Array.Empty<HexCoordinate>();
                var components = context?.Components;
                if (components == null || !components.CanDisplace ||
                    !TryResolveMover(context, out _, out var moverAt) ||
                    !components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt) ||
                    !actorAt.Equals(moverAt))
                {
                    return false;
                }

                var destinations = Candidates(context);
                var declinable = context.FailurePolicy != EffectFailurePolicy.Mandatory;
                if (destinations.Count == 0 && !declinable)
                {
                    // A mandatory step with nowhere to go stops the resolution at itself, which the
                    // caller learns from Anticipate rather than from here. Answering with an empty set
                    // would read as "he could be standing nowhere".
                    return false;
                }

                spaces = declinable && !destinations.Contains(actorAt)
                    ? destinations.Concat(new[] { actorAt }).ToArray()
                    : destinations;
                return true;
            }

            /// <summary>
            /// The destinations this move may use, from the Movement action's own reachability plus the
            /// node's distance policy and the source-relative policy. Nothing here walks the board.
            ///
            /// How much of the declared distance is optional is <see cref="AdmittedDistances"/>'s
            /// answer rather than this handler's, which is milestone 6.5ad's correction: reading p.10
            /// and p.11 here as well let this capability and Object Push disagree about whether `up to
            /// X spaces` admits no spaces at all, and they did.
            /// </summary>
            /// <summary>
            /// The spaces this Move entered and left on its way to <paramref name="destination"/>, for
            /// rulebook p.18's "any special effects of tokens are still applied". Milestone 8.61e.
            ///
            /// <para>It re-asks the one reachability the Move already offered from rather than keeping
            /// a route the choice was opened with, for the same reason the destination itself is
            /// re-derived here: the board may have moved between the question and the answer, and a
            /// remembered route could name spaces this Move no longer walks. p.11 says a card-text move
            /// "follows the same rules and restrictions as a Movement action", so the walk that decides
            /// which spaces were crossed is the walk that decided the move was legal.</para>
            /// </summary>
            private static IReadOnlyList<HexCoordinate> WalkedThrough(
                EffectCapabilityContext context,
                BoardObjectReference mover,
                HexCoordinate destination)
            {
                var board = context.Components?.Board;
                if (board == null)
                {
                    return null;
                }

                var crossing = context.Input(MovesThroughObstaclesParameterName);
                var route = board.FindDirectMoveDestinations(
                        mover,
                        context.Input("distance").IntegerValue,
                        context.Modifications,
                        crossing.Kind == EffectValueKind.Boolean && crossing.BooleanValue)
                    .Where(candidate => candidate.Destination.Equals(destination))
                    .Select(candidate => candidate.Route)
                    .FirstOrDefault();
                return route == null
                    ? null
                    : GuardsDigital.Core.Rules.Displacement.MoveThroughTrapQuery.SpacesMovedThrough(
                        route);
            }

            private IReadOnlyList<HexCoordinate> Candidates(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.CanDisplace)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var distance = context.Input("distance").IntegerValue;
                if (!TryResolveMover(context, out var mover, out var moverAt) || distance < 0)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var destinationPolicy =
                    (DirectMoveDestinationPolicy)context.Input(DestinationPolicyParameterName).IntegerValue;
                if (!Enum.IsDefined(typeof(DirectMoveDestinationPolicy), destinationPolicy) ||
                    destinationPolicy == DirectMoveDestinationPolicy.None)
                {
                    return Array.Empty<HexCoordinate>();
                }

                // p.18's `Closer (Space)`, measured from the acting hero, is what both narrowing
                // policies compare against, so an actor who is not on the board can satisfy neither.
                // The unrestricted reading needs no such position and is therefore not refused for
                // lacking one.
                var actorAt = default(HexCoordinate);
                if (DirectMoveDestinations.NeedsActorPosition(destinationPolicy) &&
                    !components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out actorAt))
                {
                    return Array.Empty<HexCoordinate>();
                }

                // Milestone 8.21. A named destination is what the sentence pins, so the declared
                // number stops being the exact count the mover walks and becomes the budget it may
                // spend reaching that one space. Brogan's charges are why: the charge line has already
                // chosen a destination two, three or four spaces away under one declaration, and a
                // reading that admitted only the largest of those would refuse the shorter charges the
                // same card offers.
                var namesASpace = destinationPolicy == DirectMoveDestinationPolicy.NamedSpace;
                var leastDistanceInput = context.Input(LeastDistanceParameterName);
                var leastDistance = leastDistanceInput.Kind == EffectValueKind.Integer
                    ? leastDistanceInput.IntegerValue
                    : 0;
                if (leastDistance < 0 || leastDistance > distance)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var admitted = AdmittedDistances.ForDeclared(
                    namesASpace ? EffectFailurePolicy.UpTo : context.FailurePolicy,
                    distance,
                    leastDistance);
                var namedDestination = context.Input(NamedDestinationParameterName);
                var actorRadius = context.Input(ActorRadiusParameterName);

                // Milestone 8.82. `to a space in radius` where the radius is the card's own icon
                // rather than a number the sentence writes down - Swift's Mark for Death and Hunting
                // Season. p.9: "if the card text mentions Range/Radius, it refers to this value", and
                // p.16 raises that value wherever the hero prints it, so the leash is asked of
                // `RadiusValueQuery` with the same arguments the printed-icon SELECTION asks it
                // with. Both halves of "an enemy minion in radius ... to a space in radius" therefore
                // read one total from one place: the offer and the leash cannot come apart, which is
                // exactly what they had done - the offer widened with her Super-Shotgun and the move
                // stayed where an unbound literal left it, which was nowhere.
                //
                // A card printing no icon has no such bound at all rather than a total of zero, so the
                // radius arrives at `Admit` as no value and admits nothing. That is the printed-icon
                // selection's own rule - an item raises a printed icon and never invents one - and it
                // is the reading every unbound narrowing in `DirectMoveDestinations` already takes.
                int? radiusBound = actorRadius.Kind == EffectValueKind.Integer
                    ? actorRadius.IntegerValue
                    : (int?)null;
                if (destinationPolicy == DirectMoveDestinationPolicy.WithinActorPrintedRadius)
                {
                    var printedRadius = RadiusValueQuery.Calculate(
                        context.SourceCardInstanceId,
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions,
                        context.ActorId,
                        context.HeroItems,
                        context.Modifications);
                    radiusBound = printedRadius.Total > 0 ? printedRadius.Total : (int?)null;
                }

                // Milestone 8.61c. The space a destination has to stand beside, read exactly as the
                // two bounds above are: an unresolved binding arrives as no value at all, and the
                // policy that reads it admits nothing rather than everything.
                var besideSpace = context.Input(BesideSpaceParameterName);

                // Milestone 8.23. An unbound declaration is the absence of the sentence, and the
                // absence of "through" is p.18's ordinary movement - so a card that never mentions
                // crossing reaches the same walk it always did.
                var crossing = context.Input(MovesThroughObstaclesParameterName);
                var movesThroughObstacles =
                    crossing.Kind == EffectValueKind.Boolean && crossing.BooleanValue;
                IEnumerable<HexCoordinate> travelled = DirectMoveDestinations.Admit(
                    destinationPolicy,
                    actorAt,
                    moverAt,
                    components.Board
                        .FindDirectMoveDestinations(
                            mover, distance, context.Modifications, movesThroughObstacles)
                        .Where(destination => admitted.Admits(destination.Cost))
                        .Select(destination => destination.Destination),
                    namedDestination.Kind == EffectValueKind.Coordinate
                        ? namedDestination.Coordinate
                        : (HexCoordinate?)null,
                    radiusBound,
                    besideSpace.Kind == EffectValueKind.Coordinate
                        ? besideSpace.Coordinate
                        : (HexCoordinate?)null);

                // Milestone 8.112e. The destination's own adjacency, asked through 8.88's clause
                // reader over the same parameter names. Unbound, it declares no adjacency and admits
                // every space, which is every Move composed before it.
                if (context.Input(DestinationAdjacentToTerrainParameterName).BooleanValue ||
                    context.Input(DestinationAdjacentToTokenKindParameterName).Kind ==
                        EffectValueKind.TokenKind)
                {
                    var clauses = new SyntheticTargetingCapabilities.PrintedIconSpaceClauses(
                        context, components.Board, moverAt, distance);
                    travelled = travelled.Where(clauses.Admit).ToArray();
                }

                // Standing still is p.10's decision not to move, and a sentence that names where to
                // move offers no such decision: the named space is the only thing that satisfies it.
                if (namesASpace)
                {
                    return AdmitPushEndpoints(context, travelled)
                        .OrderBy(coordinate => coordinate.Q)
                        .ThenBy(coordinate => coordinate.R)
                        .ToArray();
                }


                // Standing still is admitted by the wording, not by the geometry, so it is added to
                // whatever the geometry admits rather than filtered through it. The reachability walk
                // beneath never emits the mover's own space - a Movement action's destination costs at
                // least one point - and the destination policy is not asked about it either. Both are
                // right about travel and neither is being asked about travel: this is p.10's decision
                // not to move.
                var spaces = admitted.AdmitsStandingStill
                    ? travelled.Concat(new[] { moverAt })
                    : travelled;

                return AdmitPushEndpoints(context, spaces)
                    .OrderBy(coordinate => coordinate.Q)
                    .ThenBy(coordinate => coordinate.R)
                    .ToArray();
            }

            private IEnumerable<HexCoordinate> AdmitPushEndpoints(
                EffectCapabilityContext context, IEnumerable<HexCoordinate> spaces)
            {
                if (!requiresAdjacentEnemyHeroForPush) return spaces;
                var board = context.Components.Board;
                var actor = BoardObjectReference.ForHero(context.ActorId);
                if (context.Participants == null ||
                    !board.TryGetObjectCoordinate(actor, out var actorAt) ||
                    !context.Input("mover").Coordinate.Equals(actorAt))
                    return Array.Empty<HexCoordinate>();

                // Adjacency is a target-existence requirement, including when choosing zero. A later
                // ordinary target choice names which qualifying hero; no hero is selected implicitly.
                return spaces.Where(space => RosterPresenceQuery.FindUnitsWithin(
                        space, 1, RosterUnitKind.Hero, SourceTeamRelation.OpposingSource,
                        context.ActorId, board, context.Participants, context.Modifications, actor)
                    .Any(target => !IsImmuneToThisPush(context, target, actor.AsRuleObject())));
            }

            /// <summary>
            /// The object this node moves, and where it stands.
            ///
            /// The subject is the object standing at the bound coordinate, resolved through the board
            /// port the way Object Push resolves its targets. An empty space names nothing and the node
            /// reports itself impossible, which is what a stale selection becomes by the time it is
            /// consumed here: the space is re-read, not remembered.
            /// </summary>
            private bool TryResolveMover(
                EffectCapabilityContext context,
                out BoardObjectReference mover,
                out HexCoordinate moverAt)
            {
                mover = default;
                moverAt = default;

                var components = context.Components;
                if (components == null || !components.CanDisplace)
                {
                    return false;
                }

                var subject = context.Input("mover");
                if (subject.Kind != EffectValueKind.Coordinate)
                {
                    return false;
                }

                moverAt = subject.Coordinate;
                if (!components.Board.TryGetObjectAt(moverAt, out mover))
                {
                    return false;
                }

                var unit = mover.AsRuleObject();
                if (!unit.IsDefined)
                {
                    return true;
                }

                var overrides = supportsImmunityOverrides
                    ? context.ImmunityOverrides
                    : Array.Empty<ImmunityOverrideDeclaration>();
                var source = RuleObject.ForHero(context.ActorId);
                return !TargetQuery.IsPrevented(
                        unit,
                        context.EnclosingActivity,
                        source,
                        components.Board,
                        context.Participants,
                        context.Modifications,
                        overrides,
                        RuleActivity.DirectMove) &&
                    !TargetQuery.IsPrevented(
                        unit,
                        RuleActivity.DirectMove,
                        source,
                        components.Board,
                        context.Participants,
                        context.Modifications,
                        overrides,
                        RuleActivity.DirectMove);
            }

            private static IReadOnlyList<RuleActivity> DirectMoveActivity { get; } =
                Array.AsReadOnly(new[] { RuleActivity.DirectMove });
        }

        /// <summary>
        /// Rulebook p.18's Push: "When a unit or a token is pushed, move it in a straight line directly
        /// away from the pushing unit the number of spaces equal to the push distance."
        ///
        /// Its targets are every object adjacent to the acting hero. That bound is synthetic, not a rule:
        /// which objects a real effect may push comes from its card text through the reusable targeting
        /// contracts milestone 6.8 owns, and this milestone has no verified text to read. It is chosen
        /// because it makes both shapes one code path - one adjacent object is the single-target push, and
        /// several is p.18's multi-target batch - and because adjacency guarantees the straight line the
        /// rule needs, since every neighbour lies on one of the six files by definition.
        ///
        /// The one decision is the distance, which is why this is one node. Under `up to` the chooser
        /// picks it ("you choose the distance and then push all those units that many spaces at the same
        /// time"); under any other policy the bound distance is fixed and the node opens nothing. Every
        /// target is then resolved against one pre-effect state and committed in a single batch, which is
        /// the one-batch commit milestone 6.4 owns. It is discharged inside this capability rather than as
        /// a general `SimultaneousBatch` control node, following milestone 6.3's precedent of implementing
        /// Clear through the existing seam instead of generalising `ReplaceAction` before a verified card
        /// needs it.
        /// </summary>
        private sealed class ObjectPushHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ObjectPushV3;

            public EffectCapabilityParameterSchema Schema { get; } = PushSchema(named: false);

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                ExecutePush(context, TryFindAdjacentTargets);

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                ResumePush(context, selected, TryFindAdjacentTargets);

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                AnticipatePush(context, TryFindAdjacentTargets);
        }

        /// <summary>
        /// Milestone 6.5a's Push over the one object a definition names, for card text of the shape
        /// `choose another adjacent unit and Push it 3 spaces`.
        ///
        /// The subject arrives as a coordinate from an earlier target-selection node, the same
        /// space-as-identity direct Move and Place use. Which objects are offerable - adjacency,
        /// self-exclusion, immunity - is that node's declaration through the reusable targeting
        /// contract, so this capability states no target bound of its own; what it does state, and
        /// re-derives on every call, is that the named space still holds an object this actor may push.
        /// </summary>
        private sealed class PushTargetHandler : IEffectCapabilityHandler,
            IEffectInertDisplacementCapabilityHandler
        {
            /// <summary>Milestone 9.8f: see <see cref="IEffectInertDisplacementCapabilityHandler"/>.</summary>
            public bool WouldDisplaceNothing(EffectCapabilityContext context) =>
                PushWouldMoveNothing(context, TryFindNamedTarget);

            public EffectCapabilityReference Reference => PushTargetV3;

            public EffectCapabilityParameterSchema Schema { get; } = PushSchema(named: true);

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                ExecutePush(context, TryFindNamedTarget);

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                ResumePush(context, selected, TryFindNamedTarget);

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                AnticipatePush(context, TryFindNamedTarget);
        }

        /// <summary>
        /// "Choose a direction" - always all six, unconditionally, because nothing about the board or
        /// the actor's own state can make one of p.17's six directions illegal to name.
        /// </summary>
        private sealed class ChooseDirectionHandler : IEffectCapabilityHandler,
            IEffectSelectionCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => ChooseDirectionV1;

            // Milestone 9.6. The step the direction is chosen FOR may be declared, so an offer can ask
            // it whether any direction lets it do anything - Cold Snap with no enemy unit in radius.
            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(
                        EffectSelectionDeclaration.ConsumerParameterName,
                        EffectValueKind.Node,
                        isRequired: false)
                },
                new[]
                {
                    new EffectOutputBinding(ChosenDirectionOutput, EffectValueKind.Integer)
                });

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                AllDirections.Select(direction => EffectValue.Integer((int)direction)).ToArray();

            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                chosen != null && chosen.Count == 1 && chosen[0].Kind == EffectValueKind.Integer &&
                    AllDirections.Contains((HexDirection)chosen[0].IntegerValue)
                    ? new[] { new EffectOutcomeFact(ChosenDirectionOutput, chosen[0]) }
                    : Array.Empty<EffectOutcomeFact>();

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                // Milestone 8.37 declares the vocabulary, so the actor is offered six named
                // DIRECTIONS rather than the digits 0 through 5.
                EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    AllDirections.Select(direction => EffectValue.Integer((int)direction)),
                    1,
                    1,
                    valueVocabulary: Rules.EffectResolution.EffectChoiceValueVocabulary.HexDirection));

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Integer)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var direction = (HexDirection)selected[0].IntegerValue;
                if (!AllDirections.Contains(direction))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(ChosenDirectionOutput, EffectValue.Integer((int)direction))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.NeedsChoice;
        }

        /// <summary>
        /// "Move each enemy unit in radius 1 space in that direction, if able" - see the type remarks
        /// beside <see cref="BatchDirectMoveV1"/> for the rules this composes.
        /// </summary>
        private sealed class BatchDirectMoveHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => BatchDirectMoveV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(BatchRadiusParameterName, EffectValueKind.Integer),
                    new EffectCapabilityParameter(
                        BatchDirectionParameterName,
                        EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.All<HexDirection>())
                },
                new[]
                {
                    new EffectOutputBinding(BatchMovedCountOutput, EffectValueKind.Integer)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!TryReadBatch(context, out var board, out var direction, out var candidates) ||
                    !AnyCanStep(context, board, candidates))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var moved = 0;
                foreach (var (target, at) in candidates)
                {
                    if (!CanStep(context, board, at, direction))
                    {
                        // "if able": a unit with nowhere to go simply does not move, and the batch
                        // continues to the next one rather than stopping - the same reading p.18 gives a
                        // Push that resolves to zero spaces.
                        continue;
                    }

                    var committed = board.CommitValidatedDisplacement(
                        target, StraightLineQuery.Step(at, direction, 1));
                    if (committed.Accepted)
                    {
                        moved++;
                    }
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(BatchMovedCountOutput, EffectValue.Integer(moved))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// Milestone 9.6: impossible exactly when no enemy unit in the disk could take a step in ANY
            /// direction - nobody there, or everybody hemmed in - which is when the sentence can move
            /// nobody whichever direction is named. A direction that moves nobody while another would is
            /// still a direction the card lets the actor choose, so the answer never depends on the
            /// chosen one: Cold Snap is greyed on an empty disk and offers all six directions otherwise.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                TryReadBatch(context, out var board, out _, out var candidates) &&
                    AnyCanStep(context, board, candidates)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            private static bool TryReadBatch(
                EffectCapabilityContext context,
                out IBoardDisplacementAuthority board,
                out HexDirection direction,
                out (BoardObjectReference target, HexCoordinate at)[] candidates)
            {
                board = context.Components?.Board;
                var radius = context.Input(BatchRadiusParameterName).IntegerValue;
                direction = (HexDirection)context.Input(BatchDirectionParameterName).IntegerValue;
                candidates = null;
                if (board == null || context.Participants == null || radius < 0 ||
                    !AllDirections.Contains(direction) ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return false;
                }

                // p.18's "at the same time": every candidate's own coordinate is read once, up front,
                // before any of this batch's own moves can change what a later candidate's destination
                // check would see. Order is stable rather than incidental, so a genuine collision - two
                // units approaching one empty space from different directions - commits deterministically
                // through the board's own validated displacement rather than by iteration accident.
                var reading = board;
                candidates = RosterPresenceQuery.FindUnitsWithin(
                        actorAt, radius, RosterUnitKind.Unit, SourceTeamRelation.OpposingSource,
                        context.ActorId, board, context.Participants, context.Modifications)
                    .Where(target => !TargetQuery.IsPrevented(
                        target.AsRuleObject(), context.EnclosingActivity,
                        RuleObject.ForHero(context.ActorId), reading, context.Participants,
                        context.Modifications, context.ImmunityOverrides))
                    .OrderBy(RosterPresenceQuery.Identity, StringComparer.Ordinal)
                    .Select(target => (target, at: reading.TryGetObjectCoordinate(target, out var at) ? at : (HexCoordinate?)null))
                    .Where(pair => pair.at.HasValue)
                    .Select(pair => (pair.target, at: pair.at.Value))
                    .ToArray();
                return true;
            }

            /// <summary>
            /// Whether anybody in the batch has a free space one step away in some direction. When
            /// nobody has, nobody moves whichever direction is named: a space only opens when a unit
            /// has already stepped out of it, and none can take the first step.
            /// </summary>
            private static bool AnyCanStep(
                EffectCapabilityContext context,
                IBoardDisplacementAuthority board,
                (BoardObjectReference target, HexCoordinate at)[] candidates) =>
                candidates.Any(candidate =>
                    AllDirections.Any(direction => CanStep(context, board, candidate.at, direction)));

            private static bool CanStep(
                EffectCapabilityContext context,
                IBoardDisplacementAuthority board,
                HexCoordinate at,
                HexDirection direction)
            {
                var destination = StraightLineQuery.Step(at, direction, 1);
                return board.MapDefinition.PlayableCoordinates.Contains(destination) &&
                    !board.TryGetObjectAt(destination, out _) &&
                    !context.Modifications.AnyOverlayMakesAnObstacleOf(destination);
            }
        }

        /// <summary>How a Push handler produces its target set. The only thing the two differ in.</summary>
        private delegate bool PushTargetFinder(
            EffectCapabilityContext context,
            out HexCoordinate pusherAt,
            out IReadOnlyList<BoardObjectReference> targets);

        private static EffectCapabilityParameterSchema PushSchema(bool named)
        {
            var parameters = new List<EffectCapabilityParameter>();
            if (named)
            {
                parameters.Add(new EffectCapabilityParameter("target", EffectValueKind.Coordinate));
            }

            parameters.Add(new EffectCapabilityParameter("distance", EffectValueKind.Integer));

            // Milestone 8.8. The distance a card names when it names more than one - "3 or 4 spaces" -
            // stated as the floor of the same declaration rather than as a second distance parameter,
            // because there is one distance and the card writes its ends.
            parameters.Add(new EffectCapabilityParameter(
                LeastDistanceParameterName, EffectValueKind.Integer));
            parameters.Add(new EffectCapabilityParameter("ignoresObstacles", EffectValueKind.Boolean));
            parameters.Add(new EffectCapabilityParameter(
                PusherSpaceParameterName, EffectValueKind.Coordinate, isRequired: false));

            // Milestone 6.4c. Both declarations carry it, because both produce their target set
            // through the finders below and the reading lives there rather than per handler. The
            // spelling is milestone 6.6a's own constant rather than a second one with the same
            // literal: a card author declaring an eligible kind for a Push and for a Remove must not
            // be able to get one of them wrong.
            parameters.Add(new EffectCapabilityParameter(
                SyntheticExchangeCapabilities.EligiblePieceParameterName,
                EffectValueKind.Integer,
                integerDomain: EffectIntegerDomain.AllExcept(
                    PushEligiblePieceKind.None)));

            var outputs = new List<EffectOutputBinding>
            {
                new EffectOutputBinding("distance", EffectValueKind.Integer),
                new EffectOutputBinding("moved", EffectValueKind.Integer),
                new EffectOutputBinding(FirstStoppedHeroOutputName, EffectValueKind.Hero),
                new EffectOutputBinding(FirstHeroWasStoppedOutputName, EffectValueKind.Boolean),

                // Milestone 8.39c. Optional in the only sense an output can be: a node that does not
                // declare it is published nothing, so every push written before this milestone
                // produces byte-identical facts.
                new EffectOutputBinding(FirstZoneCrossingHeroOutputName, EffectValueKind.Hero),
                new EffectOutputBinding(FirstHeroCrossedAZoneOutputName, EffectValueKind.Boolean)
            };

            if (named)
            {
                // Milestone 9.21f. The one named target's realised travel - see PushedSpacesOutputName.
                outputs.Add(new EffectOutputBinding(PushedSpacesOutputName, EffectValueKind.Integer));
            }

            if (!named)
            {
                // Milestone 8.8, and the batch's alone. Which side of the board a push may take and
                // how many of it are the two things a card says about a target set it does not name
                // one by one; the named push is handed its object by a selection node that has
                // already answered both, so a count bound there would be a parameter with one legal
                // value and a relation there would ask the same question twice.
                parameters.Add(new EffectCapabilityParameter(
                    TargetRelationParameterName,
                    EffectValueKind.Integer,
                    integerDomain: EffectIntegerDomain.AllExcept(
                        SourceTeamRelation.None)));
                parameters.Add(new EffectCapabilityParameter(
                    MaximumTargetsParameterName, EffectValueKind.Integer));

                // The second stop-cause pair, for the same reason: one named target can stop one hero.
                outputs.Add(new EffectOutputBinding(SecondStoppedHeroOutputName, EffectValueKind.Hero));
                outputs.Add(new EffectOutputBinding(
                    SecondHeroWasStoppedOutputName, EffectValueKind.Boolean));
            }

            return new EffectCapabilityParameterSchema(parameters, outputs);
        }

        /// <summary>
        /// The spaces this declaration admits, read through the one statement of pages 10 and 11 that
        /// every capability moving something a stated number of spaces consults.
        /// </summary>
        private static AdmittedDistances Distances(EffectCapabilityContext context) =>
            AdmittedDistances.ForDeclared(
                context.FailurePolicy,
                context.Input("distance").IntegerValue,
                context.Input(LeastDistanceParameterName).IntegerValue);

        /// <summary>
        /// The most units this declaration lets one push take, or 0 where it names no number - a batch
        /// whose card says every adjacent object, and the named push, whose definition already named
        /// its one object and declares no such parameter at all.
        /// </summary>
        private static int MaximumTargets(EffectCapabilityContext context) =>
            context.Input(MaximumTargetsParameterName).IntegerValue;

        /// <summary>
        /// Whether the declared distance is one this capability can read at all. Asked in both entry
        /// points and stated once, because a probe that admitted a declaration the execution refuses
        /// is the disagreement milestone 6.6a found in another handler.
        /// </summary>
        private static bool DeclaresALegalDistance(EffectCapabilityContext context)
        {
            var declared = context.Input("distance").IntegerValue;
            var least = context.Input(LeastDistanceParameterName).IntegerValue;
            return declared >= 0 && least >= 0 && least <= declared;
        }

        /// <summary>
        /// Whether which of the admitted objects to push is the chooser's decision, which is exactly
        /// whether the card named a maximum at all.
        ///
        /// A card that names one has said something the rule cannot answer for the player: "up to 2
        /// enemy units adjacent to you" leaves which two, and - under p.10's non-mandatory wording -
        /// whether to push two, one or none. A card that names none is p.18's plain batch, where every
        /// object standing beside the pusher is pushed and there is nothing to ask.
        /// </summary>
        private static bool CountIsADecision(EffectCapabilityContext context) =>
            MaximumTargets(context) > 0;

        private static EffectExecutionResult ExecutePush(
            EffectCapabilityContext context, PushTargetFinder finder)
        {
            if (!DeclaresALegalDistance(context) || !finder(context, out _, out var admitted))
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            // A push that has already been told which units - a resolution restored or replayed
            // between its two questions - is past question one, and asking it again would reopen a
            // decision the frame records as made. The answers decide which question this is, exactly
            // as they do in Resume, which is what lets snapshot restore re-ask this handler what it
            // is offering and get the question the pause was written on.
            if (TryFindTargetsNamedEarlier(context, admitted, out var namedEarlier))
            {
                return PushOrAskDistance(context, namedEarlier);
            }

            // Question one: which units. p.18 settles the distance over "all those units", so the set
            // is named before the distance is asked, and the card's own sentence reads in that order
            // too - "Push up to 2 enemy units adjacent to you 3 or 4 spaces".
            if (CountIsADecision(context))
            {
                return OfferTargets(context, admitted);
            }

            return PushOrAskDistance(context, admitted);
        }

        /// <summary>
        /// Whether a push would do anything, asked without pushing. Both declarations need it and both
        /// need it for the same reason: with nothing left to decide the execution above resolves and
        /// commits the batch in the same call, so this is the one shape where probing by executing
        /// would move every adjacent object across the board.
        ///
        /// It stops at the target question, which is the whole of what "can this be done" means here.
        /// p.18 admits a zero-space push as a legal result, so a target that cannot travel is still a
        /// target and this reports the push performable - the rule that a blocked push resolves to
        /// zero is the same rule whether or not anyone asked first.
        /// </summary>
        /// <summary>
        /// Milestone 9.8f. Whether, as the board stands, the push would leave every target where it is
        /// at every distance the declaration admits - and no target is a hero, whose 0-space push is a
        /// stop other text may read. Resolved exactly as <see cref="Push"/> resolves, without committing.
        /// </summary>
        private static bool PushWouldMoveNothing(EffectCapabilityContext context, PushTargetFinder finder)
        {
            if (!DeclaresALegalDistance(context) || !finder(context, out var pusherAt, out var targets) ||
                targets.Any(target => target.Kind == BoardObjectKind.Hero))
            {
                return false;
            }

            var board = context.Components.Board;
            var ignoresObstacles = context.Input("ignoresObstacles").BooleanValue;
            var admitted = Distances(context);
            for (var distance = Math.Max(1, admitted.Minimum); distance <= admitted.Maximum; distance++)
            {
                foreach (var target in targets)
                {
                    if (board.TryGetObjectCoordinate(target, out var targetAt) &&
                        board.ResolvePush(pusherAt, targetAt, distance, target.AsRuleObject(),
                            ignoresObstacles, context.Modifications).Moves)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static EffectExecutionResultKind AnticipatePush(
            EffectCapabilityContext context, PushTargetFinder finder)
        {
            if (!DeclaresALegalDistance(context) || !finder(context, out _, out _))
            {
                return EffectExecutionResultKind.ImpossibleMandatory;
            }

            return CountIsADecision(context) || !Distances(context).NamesOneDistance
                ? EffectExecutionResultKind.NeedsChoice
                : EffectExecutionResultKind.Applied;
        }

        /// <summary>
        /// One answer, routed by what it answers.
        ///
        /// A push asks at most two questions and they carry different kinds, so nothing has to be
        /// remembered about which one is open: coordinates name the units, an integer names the
        /// distance. What the earlier answer said is read off the frame through
        /// <see cref="EffectCapabilityContext.AnswersAlreadyGiven"/>, and re-derived against live state
        /// before it is used, because an object named when the choice opened may have moved, been
        /// removed, or become immune since.
        /// </summary>
        private static EffectExecutionResult ResumePush(
            EffectCapabilityContext context,
            IReadOnlyList<EffectValue> selected,
            PushTargetFinder finder)
        {
            if (selected == null)
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            if (selected.Count == 0)
            {
                // Confirming an `up to` selection with nothing in it is the count the card allows -
                // "up to 2" includes none - so the step was offered and not taken rather than failed.
                return EffectExecutionResult.DeclinedOptional();
            }

            if (selected.All(value => value.Kind == EffectValueKind.Coordinate))
            {
                if (!finder(context, out _, out var admitted))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var chosen = NamedTargets(context, selected, admitted);
                if (chosen.Count != selected.Count)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return PushOrAskDistance(context, chosen);
            }

            if (selected.Count != 1 || selected[0].Kind != EffectValueKind.Integer)
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            var distance = selected[0].IntegerValue;
            if (!Distances(context).Admits(distance) || !finder(context, out _, out var stillAdmitted))
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            TryFindTargetsNamedEarlier(context, stillAdmitted, out var targets);
            return Push(context, distance, targets);
        }

        /// <summary>
        /// Whether an earlier answer to this node already named the units, and which of the admitted
        /// ones they are. False where no such answer exists, which is the unbounded batch p.18
        /// describes and every declaration before milestone 8.8.
        /// </summary>
        private static bool TryFindTargetsNamedEarlier(
            EffectCapabilityContext context,
            IReadOnlyList<BoardObjectReference> admitted,
            out IReadOnlyList<BoardObjectReference> named)
        {
            for (var index = context.AnswersAlreadyGiven.Count - 1; index >= 0; index--)
            {
                var answer = context.AnswersAlreadyGiven[index];
                if (answer.Count > 0 && answer.All(value => value.Kind == EffectValueKind.Coordinate))
                {
                    named = NamedTargets(context, answer, admitted);
                    return true;
                }
            }

            named = admitted;
            return false;
        }

        /// <summary>
        /// The admitted objects standing where these coordinates point, in the admitted set's own
        /// order. An answer naming a space that no longer holds an admitted object simply drops out,
        /// which is what makes the count check at the call site meaningful.
        /// </summary>
        private static IReadOnlyList<BoardObjectReference> NamedTargets(
            EffectCapabilityContext context,
            IReadOnlyList<EffectValue> named,
            IReadOnlyList<BoardObjectReference> admitted)
        {
            var board = context.Components.Board;
            var chosen = new List<BoardObjectReference>();
            foreach (var target in admitted)
            {
                if (board.TryGetObjectCoordinate(target, out var standing) &&
                    named.Any(value =>
                        value.Kind == EffectValueKind.Coordinate &&
                        value.Coordinate.Equals(standing)))
                {
                    chosen.Add(target);
                }
            }

            return chosen;
        }

        /// <summary>
        /// Where the admitted objects stand right now. A push names its targets by space, the identity
        /// milestone 6.4 chose for displacement, so this is what the chooser is offered.
        /// </summary>
        private static IReadOnlyList<HexCoordinate> AdmittedCoordinates(
            EffectCapabilityContext context, IReadOnlyList<BoardObjectReference> admitted)
        {
            var board = context.Components.Board;
            var coordinates = new List<HexCoordinate>();
            foreach (var target in admitted)
            {
                if (board.TryGetObjectCoordinate(target, out var standing))
                {
                    coordinates.Add(standing);
                }
            }

            return coordinates;
        }

        private static EffectExecutionResult OfferTargets(
            EffectCapabilityContext context, IReadOnlyList<BoardObjectReference> admitted)
        {
            var candidates = AdmittedCoordinates(context, admitted)
                .Select(EffectValue.Space)
                .ToArray();
            var maximum = Math.Min(MaximumTargets(context), candidates.Length);
            var minimum = context.FailurePolicy == EffectFailurePolicy.UpTo ? 0 : maximum;
            return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                EffectChooserRole.Actor, candidates, minimum, maximum));
        }

        /// <summary>
        /// Either the distance question or the push itself. p.18: "If a card allows you to push
        /// multiple targets up to X spaces, you choose the distance and then push all those units that
        /// many spaces at the same time" - so a declaration admitting more than one number asks, and
        /// one admitting a single number simply uses it.
        /// </summary>
        private static EffectExecutionResult PushOrAskDistance(
            EffectCapabilityContext context,
            IReadOnlyList<BoardObjectReference> targets)
        {
            var admitted = Distances(context);
            if (!admitted.NamesOneDistance)
            {
                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    admitted.Enumerate().Select(EffectValue.Integer).ToArray(),
                    1,
                    1));
            }

            return Push(context, admitted.Maximum, targets);
        }

        /// <summary>
        /// One chosen distance applied to every target at once: each is resolved against the state as
        /// it stands before anything moves, and only then is anything committed. p.18's geometry lives
        /// here alone, whichever declaration produced the target set.
        /// </summary>
        private static EffectExecutionResult Push(
            EffectCapabilityContext context,
            int distance,
            IReadOnlyList<BoardObjectReference> targets)
        {
            if (!TryFindPusher(context, out var pusherAt, out _))
            {
                return EffectExecutionResult.ImpossibleMandatory();
            }

            var ignoresObstacles = context.Input("ignoresObstacles").BooleanValue;
            var board = context.Components.Board;

            // Phase one: resolve every path against one pre-effect state. Nothing is committed
            // here, so no target's outcome can depend on another target having already moved -
            // which is what p.18's "at the same time" forbids.
            var resolved = new List<KeyValuePair<BoardObjectReference, PushPathResult>>();
            foreach (var target in targets)
            {
                if (!board.TryGetObjectCoordinate(target, out var targetAt))
                {
                    continue;
                }

                var path = board.ResolvePush(
                    pusherAt, targetAt, distance, target.AsRuleObject(), ignoresObstacles,
                    context.Modifications);
                if (path.HasDirection)
                {
                    resolved.Add(new KeyValuePair<BoardObjectReference, PushPathResult>(target, path));
                }
            }

            // Phase two: read the stop causes, in the order the targets themselves are in.
            //
            // Deliberately not the commit order below. That order is p.18's geometry protecting itself
            // from its own batch; this one is what a card's rider reads, and "the first hero this push
            // stopped" should mean the first of the units the card named rather than whichever of them
            // happened to travel furthest. A hero pushed zero spaces by an obstacle standing right
            // behind it is counted here, because p.18 calls that a push.
            var stopped = resolved
                .Where(entry =>
                    entry.Key.Kind == BoardObjectKind.Hero && entry.Value.StoppedByAnObstacle)
                .Select(entry => entry.Key.HeroId)
                .ToList();

            // Milestone 8.39c, read in the same order and from the same resolved paths as the stop
            // causes above. Origin against final endpoint: a path that leaves a zone and re-enters it
            // has carried nobody anywhere, whatever it crossed on the way. A target the geometry left
            // standing is excluded by `Moves`, because p.18's "if you push" is the displacement.
            var crossedAZone = resolved
                .Where(entry =>
                    entry.Key.Kind == BoardObjectKind.Hero && entry.Value.Moves &&
                    LeavesItsZone(board, entry.Value.Origin, entry.Value.Endpoint))
                .Select(entry => entry.Key.HeroId)
                .ToList();

            // Phase three: commit farthest-from-the-pusher first. Every target travels directly away
            // along its own file, so working outwards means a space is always vacated before
            // anything is asked to enter it, and the batch never refuses itself.
            var moved = 0;

            // Milestone 9.21f. The realised travel of the committed paths, which a named Push (one
            // target) publishes as PushedSpacesOutputName; nought when nothing moved.
            var realised = 0;

            // Milestone 8.19c1. What a Push applied to, in the order it was committed. p.18's "if you
            // push" is the displacement rather than the attempt, so this list is built from accepted
            // commits alone: a target the geometry left standing where it was is not in it, and a push
            // that moved nothing produces an empty list and reaches no after-push trigger.
            var displaced = new List<RuleObject>();
            foreach (var entry in resolved
                .Where(entry => entry.Value.Moves)
                .OrderByDescending(entry => entry.Value.ActualDistance)
                .ThenBy(entry => entry.Value.Origin.Q)
                .ThenBy(entry => entry.Value.Origin.R))
            {
                // Milestone 8.61e. p.18's realised line, which is what the pushed object actually
                // entered and left: a push stopped short never reached the spaces beyond where it
                // stopped, so nothing standing on them was moved through.
                var committed = board.CommitValidatedDisplacement(
                    entry.Key,
                    entry.Value.Endpoint,
                    GuardsDigital.Core.Rules.Displacement.MoveThroughTrapQuery.RealisedLine(
                        entry.Value.Origin, entry.Value.Endpoint));
                if (committed.Accepted)
                {
                    moved++;
                    realised = Math.Max(realised, entry.Value.ActualDistance);
                    var subject = entry.Key.AsRuleObject();
                    if (subject.IsDefined)
                    {
                        displaced.Add(subject);
                    }
                }
            }

            return EffectExecutionResult.Applied(
                PushOutcomeFacts(context, distance, moved, stopped, crossedAZone, realised),
                appliedPushes: displaced);
        }

        /// <summary>
        /// What one push publishes: how far it pushed, how many it moved, and milestone 8.8's stop
        /// causes, one pair per hero this declaration has a name for.
        ///
        /// A requested boolean is published even when false, while a requested hero is published only
        /// when there is one, because <see cref="EffectValue.Hero"/> refuses an undefined hero and a
        /// fact naming nobody would be a worse answer than no fact. Milestone 8.25e4a1 also keeps every
        /// fact behind <see cref="EffectCapabilityContext.Publishes"/>: the node's declaration is the
        /// exact output set a handler may persist, not merely a type-checking subset of its schema.
        /// </summary>

        /// <summary>
        /// Milestone 8.39c. Whether a resolved push path ends in a different zone from the one it
        /// started in - rulebook p.19's zones, read through the map's own spaces.
        ///
        /// <para>A space with no zone is terrain, which a push can never end on, so a pair this
        /// cannot resolve is not a crossing rather than an unknown. Origin and endpoint only: see
        /// <see cref="FirstZoneCrossingHeroOutputName"/> for why the spaces between do not count.</para>
        /// </summary>
        private static bool LeavesItsZone(
            IBoardDisplacementAuthority board, HexCoordinate origin, HexCoordinate endpoint) =>
            TryGetZone(board, origin, out var from) &&
            TryGetZone(board, endpoint, out var to) &&
            !from.Equals(to);

        private static bool TryGetZone(
            IBoardDisplacementAuthority board, HexCoordinate at, out ZoneId zoneId)
        {
            foreach (var space in board.MapDefinition.Spaces)
            {
                if (space.Coordinate.Equals(at) && space.ZoneId.HasValue)
                {
                    zoneId = space.ZoneId.Value;
                    return true;
                }
            }

            zoneId = default;
            return false;
        }
        private static IReadOnlyList<EffectOutcomeFact> PushOutcomeFacts(
            EffectCapabilityContext context,
            int distance,
            int moved,
            IReadOnlyList<HeroId> stopped,
            IReadOnlyList<HeroId> crossedAZone,
            int realised)
        {
            var facts = new List<EffectOutcomeFact>();
            if (context.Publishes("distance"))
                facts.Add(new EffectOutcomeFact("distance", EffectValue.Integer(distance)));
            if (context.Publishes("moved"))
                facts.Add(new EffectOutcomeFact("moved", EffectValue.Integer(moved)));
            if (context.Publishes(FirstHeroWasStoppedOutputName))
                facts.Add(new EffectOutcomeFact(
                    FirstHeroWasStoppedOutputName, EffectValue.Boolean(stopped.Count > 0)));

            if (stopped.Count > 0 && context.Publishes(FirstStoppedHeroOutputName))
            {
                facts.Add(new EffectOutcomeFact(
                    FirstStoppedHeroOutputName, EffectValue.Hero(stopped[0])));
            }

            if (context.Publishes(SecondHeroWasStoppedOutputName))
                facts.Add(new EffectOutcomeFact(
                    SecondHeroWasStoppedOutputName, EffectValue.Boolean(stopped.Count > 1)));
            if (stopped.Count > 1 && context.Publishes(SecondStoppedHeroOutputName))
            {
                facts.Add(new EffectOutcomeFact(
                    SecondStoppedHeroOutputName, EffectValue.Hero(stopped[1])));
            }

            // Milestone 8.39c. One pair, because the only printed sentence that reads it pushes one
            // hero; a card pushing two and asking about both adds the second pair the way the stop
            // causes gained theirs.
            if (context.Publishes(FirstHeroCrossedAZoneOutputName))
            {
                facts.Add(new EffectOutcomeFact(
                    FirstHeroCrossedAZoneOutputName, EffectValue.Boolean(crossedAZone.Count > 0)));
            }

            if (crossedAZone.Count > 0 && context.Publishes(FirstZoneCrossingHeroOutputName))
            {
                facts.Add(new EffectOutcomeFact(
                    FirstZoneCrossingHeroOutputName, EffectValue.Hero(crossedAZone[0])));
            }

            // Milestone 9.21f. Declared by the named Push alone (see PushedSpacesOutputName).
            if (context.Publishes(PushedSpacesOutputName))
            {
                facts.Add(new EffectOutcomeFact(PushedSpacesOutputName, EffectValue.Integer(realised)));
            }

            return facts;
        }

        /// <summary>
        /// The acting hero's space and every object it may push out of the six adjacent spaces.
        ///
        /// Immunity filters here rather than later, because p.18 makes it a targeting rule: "If a
        /// unit is immune, you cannot target that unit". The same page keeps presence separate -
        /// "effects that check for unit presence will count immune units" - so a presence query over
        /// these spaces would still see what this list drops. A token is never filtered: p.19 states
        /// tokens are not units, so no immunity is addressed to one.
        /// </summary>
        private static bool TryFindAdjacentTargets(
            EffectCapabilityContext context,
            out HexCoordinate pusherAt,
            out IReadOnlyList<BoardObjectReference> targets)
        {
            targets = Array.Empty<BoardObjectReference>();
            if (!TryFindPusher(context, out pusherAt, out var pushingUnit))
            {
                return false;
            }

            var board = context.Components.Board;
            var found = new List<BoardObjectReference>();
            foreach (var neighbor in pusherAt.GetNeighbors()
                .OrderBy(coordinate => coordinate.Q)
                .ThenBy(coordinate => coordinate.R))
            {
                if (board.TryGetObjectAt(neighbor, out var occupant) &&
                    AdmitsThisPiece(context, occupant) &&
                    StandsOnTheDeclaredSide(context, occupant) &&
                    !IsImmuneToThisPush(context, occupant, pushingUnit))
                {
                    found.Add(occupant);
                }
            }

            targets = found;
            return found.Count > 0;
        }

        /// <summary>
        /// Milestone 8.8: whether the declared side of the board admits this object - "enemy units" for
        /// Wasp's two kinetic Skills, and everything for a card that names no side.
        ///
        /// It filters here, beside the piece kind and the immunity question, because p.19 makes it the
        /// same kind of question: what the card's own words let this action target. The reading itself
        /// is <see cref="TargetQuery.StandsInRelationTo"/>'s, so the selection a player is offered and
        /// the operation that follows cannot part company over what "enemy" means.
        /// </summary>
        private static bool StandsOnTheDeclaredSide(
            EffectCapabilityContext context, BoardObjectReference occupant)
        {
            var declared = context.Input(TargetRelationParameterName);
            if (declared.Kind != EffectValueKind.Integer)
            {
                // The named push declares no relation at all: its one object came from a selection
                // node that already asked whatever its card says about whose unit it is.
                return true;
            }

            var relation = (SourceTeamRelation)declared.IntegerValue;
            if (!Enum.IsDefined(typeof(SourceTeamRelation), relation) ||
                relation == SourceTeamRelation.None)
            {
                return false;
            }

            return TargetQuery.StandsInRelationTo(
                relation, occupant, context.ActorId, context.Participants, context.Components.Board);
        }

        /// <summary>
        /// The acting hero's space and the one object standing where the definition points.
        ///
        /// The bound space is re-read rather than remembered: a target chosen when the selection opened
        /// may have moved, been removed, or become immune before this node runs, and an empty space
        /// names nothing at all. Immunity is asked here for the same p.18 reason as above, so a target
        /// selected under one activity's rules cannot slip past this one's.
        /// </summary>
        private static bool TryFindNamedTarget(
            EffectCapabilityContext context,
            out HexCoordinate pusherAt,
            out IReadOnlyList<BoardObjectReference> targets)
        {
            targets = Array.Empty<BoardObjectReference>();
            if (!TryFindPusher(context, out pusherAt, out var pushingUnit))
            {
                return false;
            }

            var named = context.Input("target");
            if (named.Kind != EffectValueKind.Coordinate || named.Coordinate.Equals(pusherAt) ||
                !context.Components.Board.TryGetObjectAt(named.Coordinate, out var occupant) ||
                !AdmitsThisPiece(context, occupant) ||
                IsImmuneToThisPush(context, occupant, pushingUnit))
            {
                return false;
            }

            targets = new[] { occupant };
            return true;
        }

        /// <summary>The pushing unit and where it stands - p.18 measures every direction from it.</summary>
        private static bool TryFindPusher(
            EffectCapabilityContext context, out HexCoordinate pusherAt, out RuleObject pushingUnit)
        {
            pusherAt = default;
            pushingUnit = RuleObject.None;

            var components = context.Components;
            if (components == null || !components.CanDisplace)
            {
                return false;
            }

            var pusher = BoardObjectReference.ForHero(context.ActorId);
            var declaredPusher = context.Input(PusherSpaceParameterName);
            if (declaredPusher.Kind == EffectValueKind.Coordinate)
            {
                // Milestone 9.8d. A piece other than the actor pushes: it must still stand there.
                pusherAt = declaredPusher.Coordinate;
                pushingUnit = pusher.AsRuleObject();
                return components.Board.TryGetObjectAt(pusherAt, out _);
            }

            if (!components.Board.TryGetObjectCoordinate(pusher, out pusherAt))
            {
                return false;
            }

            pushingUnit = pusher.AsRuleObject();
            return true;
        }

        /// <summary>
        /// Milestone 6.4c: whether the declared eligible kind admits this piece, asked in the one place
        /// both finders share.
        ///
        /// p.18 pushes "a unit or a token", so the breadth is the rule's and the narrowing is the
        /// card's. Reading it here rather than in each handler is what makes the probe and the
        /// operation the same answer: <c>Anticipate</c>, <c>Execute</c>, and <c>Resume</c> all reach a
        /// finder, so neither can admit a piece the other refuses, and milestone 6.8l turns that one
        /// answer into the offer a player is shown.
        /// </summary>
        private static bool AdmitsThisPiece(
            EffectCapabilityContext context, BoardObjectReference occupant) =>
            PushEligibility.Admits(
                (PushEligiblePieceKind)context
                    .Input(SyntheticExchangeCapabilities.EligiblePieceParameterName).IntegerValue,
                occupant.Kind);

        private static bool IsImmuneToThisPush(
            EffectCapabilityContext context, BoardObjectReference occupant, RuleObject pushingUnit)
        {
            var asRuleObject = occupant.AsRuleObject();
            return asRuleObject.IsDefined && TargetQuery.IsPrevented(
                asRuleObject,
                RuleActivity.ObjectPush,
                pushingUnit,
                context.Components.Board,
                context.Participants,
                context.Modifications);
        }
    }
}
