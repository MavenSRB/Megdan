using System;

namespace GuardsDigital.Core.Rules.Placement
{
    /// <summary>
    /// Where the placed piece is taken from. Rulebook p.18 states both arms in one sentence: "Take a
    /// game piece (such as a miniature, or a token) from the board or from the supply, and put it into
    /// a different space."
    ///
    /// The two arms are not interchangeable, which is why this is declared rather than inferred from
    /// whatever the subject happens to be: a supply Place consumes a finite component and a board Place
    /// vacates a space, and p.18's Spawn is defined by ruling one of them out ("the spawned object must
    /// be taken from the supply and cannot be taken from the board").
    /// </summary>
    public enum PlaceSourceContainer
    {
        None = 0,
        Board = 1,
        Supply = 2
    }

    /// <summary>
    /// What kind of piece a Place may take. p.18's own general answer is any game piece; a narrower
    /// rule states its own narrower kind, and p.18's displaced-minion correction is exactly such a
    /// rule - it applies to "minions represented by minion miniatures" and says outright that "objects
    /// that count as minions are unaffected".
    ///
    /// This is an intrinsic-representation question, not an effective one: it asks which miniature is
    /// being picked up, so no <c>Counts As</c> is consulted anywhere it is used.
    /// </summary>
    public enum PlaceEligiblePieceKind
    {
        None = 0,

        /// <summary>Any game piece: p.18's unrestricted "such as a miniature, or a token".</summary>
        GamePiece = 1,

        /// <summary>A minion miniature only, for p.18's after-action correction.</summary>
        MinionMiniature = 2,

        /// <summary>A token only, for p.18's token placement and its short-supply rule.</summary>
        Token = 3,

        /// <summary>
        /// A unit or a token - the breadth Wasp's Telekinesis and Mass Telekinesis print, added at
        /// milestone 8.6.
        ///
        /// It admits the same three kinds <see cref="GamePiece"/> does, because the board holds
        /// nothing else today, and it is still a separate member for the reason
        /// <see cref="GuardsDigital.Core.Rules.Displacement.PushEligiblePieceKind"/> refuses to call
        /// its own unrestricted value `GamePiece`: the name is the content. `A game piece` is p.18's
        /// sentence about the Place keyword and `a unit or a token` is the card's sentence about this
        /// Place, so recording the card's as the page's would attribute to Wasp a breadth she does not
        /// print - and the two would part company the first time a placeable piece is neither.
        /// </summary>
        UnitOrToken = 4
    }

    /// <summary>
    /// The space a Place's destination question is measured from.
    ///
    /// p.18 puts the piece "into a different space" and says nothing about where that space is
    /// measured from, because the invoking card says it. Both readings verified content prints are
    /// here: p.18's own correction measures from the piece being moved, while Wasp's Telekinesis
    /// measures from the acting hero - "into a space adjacent to you" - and no amount of radius around
    /// the piece could express that.
    ///
    /// It is declared rather than inferred from whichever coordinate a caller passed, so a definition
    /// states its anchor and <see cref="PlaceOperation"/> can refuse a caller whose measured-from space
    /// disagrees with the declaration. Milestone 8.6.
    /// </summary>
    public enum PlaceDestinationAnchor
    {
        None = 0,

        /// <summary>The space the placed piece is standing on.</summary>
        PlacedPiece = 1,

        /// <summary>The space the hero performing the action is standing on.</summary>
        ActingHero = 2
    }

    /// <summary>
    /// The declared question that produces a Place's legal destinations. A Place never walks a path -
    /// p.18 says outright that placing "does not count as movement" - so every scope here is geometric
    /// or set-membership, and each is measured with the shared queries rather than local arithmetic.
    /// </summary>
    public enum PlaceDestinationScope
    {
        None = 0,

        /// <summary>
        /// Every empty playable space within an inclusive rulebook distance of the origin, excluding
        /// the origin itself. Distance counts spaces including spaces with obstacles (p.18's
        /// Farther/Nearest), which is the same measure Range and Radius use.
        /// </summary>
        EmptySpacesWithinRadius = 1,

        /// <summary>
        /// The equally-nearest empty spaces of a declared coordinate set - p.18's correction fallback,
        /// "place that minion in the nearest empty space of the Battle Zone".
        /// </summary>
        NearestEmptySpacesInScope = 2,

        /// <summary>
        /// Milestone 8.35z29. Every empty playable space at <em>exactly</em> the declared distance
        /// from the origin - the ring rather than the disk. Misa's Watch How I Soar and Leaf on the
        /// Wind, "Place yourself into a space <b>at maximum range</b>".
        ///
        /// <para><b>It is a separate scope rather than a flag, because the two answer different
        /// questions and one of the answers can legally be empty.</b> p.9 already makes a printed icon
        /// a maximum - "your primary action can affect targets <em>up to</em> 3 spaces away" - which is
        /// what <see cref="EmptySpacesWithinRadius"/> draws, and it is nonempty whenever any neighbour
        /// is free. A ring is not: a hero whose board has no empty space at exactly four can have
        /// plenty at three, so this is the first destination question in the build whose emptiness is
        /// an ordinary outcome rather than a cornered one. The user's 2026-09-06 ruling settles the
        /// reading - "A is the number, it says AT MAXIMUM RANGE" - and the rulebook's own worked
        /// example on p.11 uses the other phrase for the other meaning, captioning Arien's "in range"
        /// Place as "any of the empty spaces in Range 2".</para>
        ///
        /// <para>Distance is p.18's, counted across spaces with obstacles exactly as
        /// <see cref="EmptySpacesWithinRadius"/> counts it, so the ring is a ring of the same measure
        /// Range and Radius use and not a walk.</para>
        /// </summary>
        EmptySpacesAtExactlyRadius = 3,

        /// <summary>
        /// Milestone 8.35z29. <see cref="EmptySpacesWithinRadius"/> intersected with a coordinate
        /// set the caller derives - Misa's Swoop In, "Place yourself into a space in radius
        /// <b>adjacent to two or more enemy units</b>".
        ///
        /// <para><b>Why the qualification arrives as a set rather than as a predicate here.</b> The
        /// clause qualifies the space being considered, not the space the actor stands in, and
        /// answering it needs the unit roster and the acting hero's own side - collaborators this query
        /// has never taken and should not start taking, since every other clause it answers is a map
        /// question. The capability handler has both, derives the qualifying spaces, and hands them
        /// down; <see cref="PlaceOperation"/> is handed the same set when it re-derives, so the offer
        /// and the commit still ask one question.</para>
        ///
        /// <para>It is not <see cref="NearestEmptySpacesInScope"/> with a different filter: that one
        /// keeps only the equally-nearest members of its set and discards the rest, which is p.18's
        /// correction rule and would silently narrow a card that names no such preference.</para>
        /// </summary>
        EmptySpacesWithinRadiusAndInScope = 4
    }

    /// <summary>
    /// Which Spawn-point spaces a Place refuses as destinations, as definition content.
    ///
    /// <para>Milestone 8.20. Arien's three Green Place cards print the two readings verified content
    /// contains and nothing wider: Stranger Tide bars "a space ... without a spawn point", and Liquid
    /// Leap and Magical Current bar the same spaces and also any "adjacent to an empty spawn point".
    /// The two are one enum rather than two flags because a card prints one of them, and a
    /// combination nothing prints would be a reading a reader would have to invent.</para>
    ///
    /// <para><b>What "empty" means.</b> The same thing p.18 means by it everywhere else - a playable
    /// space holding no occupant of any kind - which is <see cref="Components.ITokenPlacementAuthority
    /// .IsEmptyPlayableSpace"/>'s answer. A Spawn point with a minion standing on it is not empty, and
    /// that is exactly the case the rulebook's own p.12 worked example illustrates: the longer clause
    /// keeps a hero from stepping next to a Spawn point that is about to produce something.</para>
    ///
    /// <para><b>Milestone 8.19's matrix calls this `place-destination@2`.</b> It is a refinement of the
    /// declared destination question rather than a new operation: nothing about how a Place commits
    /// changes, and p.18's "does not count as movement" still holds.</para>
    /// </summary>
    public enum PlaceSpawnPointExclusion
    {
        /// <summary>
        /// The Place refuses no space for having a Spawn point, which is what every Place declared
        /// before milestone 8.20 means.
        /// </summary>
        None = 0,

        /// <summary>`without a spawn point` - Arien's Stranger Tide.</summary>
        SpacesWithASpawnPoint = 1,

        /// <summary>
        /// `without a spawn point and not adjacent to an empty spawn point` - Arien's Liquid Leap and
        /// Magical Current. It is a strict narrowing of <see cref="SpacesWithASpawnPoint"/>, which is
        /// what makes Stranger Tide the upgrade: the same reach, one clause fewer.
        /// </summary>
        SpacesWithASpawnPointOrAdjacentToAnEmptyOne = 2,

        /// <summary>
        /// Milestone 8.35z33, and the first member that carves the board the other way round: everything
        /// <em>except</em> an empty minion Spawn point of the current Battle Zone - Whisper's Shadow
        /// Step and Shadow Walk, "Place yourself into an empty minion spawn point in range in the
        /// battle zone."
        ///
        /// <para><b>It is still an exclusion and not a new kind of clause.</b> The two members above
        /// name spaces a Place refuses and so does this one; what differs is that their sentences
        /// carve a few spaces out of the disk and hers leaves only a few in. Expressing it as a third
        /// exclusion rather than as an "admission" vocabulary keeps one filter over one derived
        /// candidate set, which is what stops a Place's offer and its commit from being able to
        /// disagree.</para>
        ///
        /// <para>The three conditions - a <em>minion</em> Spawn point, in the <em>current</em> Battle
        /// Zone, <em>empty</em> - are not restated here. They are
        /// <see cref="GuardsDigital.Core.Rules.Modifications.EmptyMinionSpawnPointQuery"/>'s, which is
        /// the same derivation the counting clauses on her Red line read, because it is the same
        /// printed phrase.</para>
        /// </summary>
        SpacesThatAreNotAnEmptyMinionSpawnPointInTheBattleZone = 3,

        /// <summary>
        /// Milestone 8.35z33. The same carve-out dilated by one ring - Whisper's Creeping Shadow,
        /// "...or into a space in range adjacent to such spawn point."
        ///
        /// <para>It is a strict widening of
        /// <see cref="SpacesThatAreNotAnEmptyMinionSpawnPointInTheBattleZone"/>, which is what makes
        /// Creeping Shadow the Tier III upgrade of the two places below it: the same reach, one clause
        /// more. Note which way the pair runs compared with Arien's - his Tier III buys a
        /// <em>narrower</em> exclusion, hers buys a <em>wider</em> admission - and both are the same
        /// relation between a card and its upgrade read off what each face prints.</para>
        ///
        /// <para>The word "such" is load-bearing and is read as the whole of the preceding phrase: the
        /// neighbouring space qualifies by being beside a point that is itself an empty minion Spawn
        /// point of the Battle Zone, and needs no property of its own beyond being a legal Place
        /// destination. So Creeping Shadow can put her outside the Battle Zone, one step from a point
        /// inside it, which is what the sentence says.</para>
        /// </summary>
        SpacesThatAreNeitherSuchAPointNorAdjacentToOne = 4
    }

    /// <summary>
    /// Who decides among the legal destinations. Placement never picks for a player, so a Place states
    /// the chooser the way every other authoritative choice does. The two members are the two the
    /// rulebook actually names: the hero performing the action, and - for the correction - "the
    /// minion's team decides" (p.18).
    /// </summary>
    public enum PlaceChooserKind
    {
        None = 0,
        ActingHero = 1,
        OwningTeam = 2
    }

    /// <summary>
    /// What a Place means when its destination query yields nothing at all. Distinct from a rejected
    /// commit: this is the rules answer to "there is nowhere to put it", which p.10 makes either a
    /// stop or a skip depending on whether the step is mandatory.
    /// </summary>
    public enum PlaceNoDestinationPolicy
    {
        None = 0,

        /// <summary>A mandatory Place with no destination stops the action at that step.</summary>
        Impossible = 1,

        /// <summary>A `may` or `if able` Place with no destination is skipped and the action continues.</summary>
        Decline = 2
    }

    /// <summary>
    /// The complete declaration of one Place operation: its source container, eligible piece kind,
    /// destination query, chooser, and no-destination policy.
    ///
    /// It exists as one object rather than five arguments because those five are exactly what
    /// <c>CARD_EFFECT_CAPABILITIES.md</c> requires a placement definition to state - "Source container,
    /// destination, occupancy, supply, or endpoint policy is incomplete for the operation" is a
    /// content-load rejection - so a Place that fails to state one cannot be constructed at all. Every
    /// caller declares its own instance: the general capability, the token placement, and the
    /// after-action correction fallback each state a different five, and nothing infers them.
    /// </summary>
    public sealed class PlacePolicy
    {
        public PlacePolicy(
            PlaceSourceContainer sourceContainer,
            PlaceEligiblePieceKind eligiblePiece,
            PlaceDestinationScope destinationScope,
            int destinationRadius,
            PlaceChooserKind chooser,
            PlaceNoDestinationPolicy noDestinationPolicy,
            PlaceDestinationAnchor destinationAnchor,
            PlaceSpawnPointExclusion spawnPointExclusion = PlaceSpawnPointExclusion.None)
        {
            if (destinationAnchor == PlaceDestinationAnchor.None)
            {
                throw new ArgumentException(
                    "A Place declares the space its destination question is measured from.",
                    nameof(destinationAnchor));
            }

            if (sourceContainer == PlaceSourceContainer.None)
            {
                throw new ArgumentException(
                    "A Place declares whether its piece comes from the board or the supply.",
                    nameof(sourceContainer));
            }

            if (eligiblePiece == PlaceEligiblePieceKind.None)
            {
                throw new ArgumentException(
                    "A Place declares which kind of game piece it may take.", nameof(eligiblePiece));
            }

            if (destinationScope == PlaceDestinationScope.None)
            {
                throw new ArgumentException(
                    "A Place declares the question that produces its destinations.", nameof(destinationScope));
            }

            if (chooser == PlaceChooserKind.None)
            {
                throw new ArgumentException(
                    "A Place declares who chooses among its destinations.", nameof(chooser));
            }

            if (noDestinationPolicy == PlaceNoDestinationPolicy.None)
            {
                throw new ArgumentException(
                    "A Place declares what having no destination means.", nameof(noDestinationPolicy));
            }

            if (destinationScope == PlaceDestinationScope.EmptySpacesWithinRadius && destinationRadius < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(destinationRadius),
                    destinationRadius,
                    "A radius scope needs a positive radius; a radius of zero admits only the origin, " +
                    "which p.18 excludes by requiring a different space.");
            }

            if (!Enum.IsDefined(typeof(PlaceSpawnPointExclusion), spawnPointExclusion))
            {
                throw new ArgumentException(
                    "A Place declares a defined Spawn-point exclusion, or None when it refuses none.",
                    nameof(spawnPointExclusion));
            }

            // A Spawn-point exclusion narrows a set of candidate spaces, and p.18's own correction
            // fallback does not have one to narrow: it places into "the nearest empty space of the
            // Battle Zone", a set the rules hand it. A declaration that carved Spawn points out of
            // that set would be changing what the page's correction does rather than what a card does.
            if (spawnPointExclusion != PlaceSpawnPointExclusion.None &&
                destinationScope != PlaceDestinationScope.EmptySpacesWithinRadius)
            {
                throw new ArgumentException(
                    "Only a Place that derives its own candidate spaces can exclude Spawn points " +
                    "from them.",
                    nameof(spawnPointExclusion));
            }

            SourceContainer = sourceContainer;
            EligiblePiece = eligiblePiece;
            DestinationScope = destinationScope;
            DestinationRadius = destinationRadius;
            Chooser = chooser;
            NoDestinationPolicy = noDestinationPolicy;
            DestinationAnchor = destinationAnchor;
            SpawnPointExclusion = spawnPointExclusion;
        }

        public PlaceSourceContainer SourceContainer { get; }
        public PlaceEligiblePieceKind EligiblePiece { get; }
        public PlaceDestinationScope DestinationScope { get; }

        /// <summary>The space this Place's destination question is measured from.</summary>
        public PlaceDestinationAnchor DestinationAnchor { get; }

        /// <summary>
        /// The inclusive rulebook distance <see cref="PlaceDestinationScope.EmptySpacesWithinRadius"/>
        /// measures, and zero for a scope that derives its own candidate set.
        /// </summary>
        public int DestinationRadius { get; }

        public PlaceChooserKind Chooser { get; }
        public PlaceNoDestinationPolicy NoDestinationPolicy { get; }

        /// <summary>
        /// Milestone 8.20. Which Spawn-point spaces this Place refuses, or
        /// <see cref="PlaceSpawnPointExclusion.None"/> when it refuses none.
        /// </summary>
        public PlaceSpawnPointExclusion SpawnPointExclusion { get; }

        public override string ToString() =>
            $"Place {EligiblePiece} from {SourceContainer} into {DestinationScope}" +
            (DestinationScope == PlaceDestinationScope.EmptySpacesWithinRadius
                ? $"({DestinationRadius})"
                : string.Empty) +
            $" around {DestinationAnchor}, chosen by {Chooser}, {NoDestinationPolicy} without one" +
            (SpawnPointExclusion == PlaceSpawnPointExclusion.None
                ? string.Empty
                : $", excluding {SpawnPointExclusion}");
    }
}
