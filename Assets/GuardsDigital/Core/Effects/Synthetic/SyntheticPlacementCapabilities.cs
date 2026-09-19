using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 6.5's placement capabilities: rulebook p.18's Place, its Spawn, and the small bridge
    /// that lets a definition name the acting hero's own space.
    ///
    /// Place and Spawn are two capability IDs rather than one, because p.18 defines the second by
    /// ruling out half of the first - "similar to place, but unlike 'Place', the spawned object must be
    /// taken from the supply and cannot be taken from the board" - and
    /// <c>CARD_EFFECT_CAPABILITIES.md</c> makes treating them as interchangeable a content-load
    /// rejection. Neither emits a movement fact of any kind: p.18 says placing "does not count as
    /// movement", so a Place that published one would be stating a rule it never performed.
    ///
    /// Registered into the one shared catalog for the reason milestone 6.2 records: a match has one
    /// capability contract set, and the snapshot records it as one list.
    /// </summary>
    public static class SyntheticPlacementCapabilities
    {
        public static EffectCapabilityId ActorSpace { get; } =
            new EffectCapabilityId("synthetic-actor-space");

        public static EffectCapabilityId Place { get; } =
            new EffectCapabilityId("synthetic-place");

        public static EffectCapabilityId Spawn { get; } =
            new EffectCapabilityId("synthetic-spawn");

        public static EffectCapabilityReference ActorSpaceV1 =>
            new EffectCapabilityReference(ActorSpace, 1);

        /// <summary>
        /// Revision 2, minted at milestone 8.6: revision 1's subject and radius plus the two things
        /// p.18 leaves to the card - which kind of piece this Place may take, and the space its
        /// destinations are measured from.
        ///
        /// Revision 1 is retired rather than kept beside it, exactly as direct Move retired its own
        /// when the subject widened. Both new parameters are declarations revision 1 made silently: it
        /// always took any game piece and always measured from the piece itself, so a definition
        /// carrying the old contract would be asking for one particular pair of answers while claiming
        /// to ask for none.
        /// </summary>
        public static EffectCapabilityReference PlaceV2 =>
            new EffectCapabilityReference(Place, 2);

        /// <summary>
        /// Milestone 8.20. The printed-Range twin of <see cref="Place"/>, for card text that measures
        /// its destinations by p.9's icon rather than by a number the definition writes down - Arien's
        /// Liquid Leap, Magical Current and Stranger Tide, "Place yourself into a space in range".
        ///
        /// <para>A second capability ID rather than a parameter on the one above, by exactly the rule
        /// <see cref="SyntheticTargetingCapabilities.RangeTarget"/> records for the selections: p.9
        /// gives the two readings two meanings and p.16 two consequences, and a definition that could
        /// name either through one parameter would let a card whose reach is printed be widened by an
        /// item it does not carry, or the reverse. The handler beneath is nevertheless shared, so
        /// p.18's Place, its immunity question and its destination query are written once.</para>
        ///
        /// <para>It declares one thing the written-radius arm does not, and only because a verified
        /// card prints it: which Spawn-point spaces the destination refuses.</para>
        /// </summary>
        public static EffectCapabilityId PlaceInRange { get; } =
            new EffectCapabilityId("synthetic-place-in-range");

        public static EffectCapabilityReference PlaceInRangeV1 =>
            new EffectCapabilityReference(PlaceInRange, 1);

        /// <summary>
        /// Milestone 8.23. The printed-Radius twin, for Tigerclaw's Blend Into Shadows: "place yourself
        /// into a space <b>in radius</b>", on a card printing a Radius 2 icon.
        ///
        /// <para>A third capability ID rather than a parameter, by the rule
        /// <see cref="PlaceInRange"/> states and for the reason it states it: the card's reach is
        /// printed, so p.16's +1 Radius item raises it - the shard records that item as covering this
        /// exact card - and a written-down number could not be raised by anything. The handler beneath
        /// is the same one, so p.18's Place, its immunity question and its destination query stay
        /// written once.</para>
        /// </summary>
        public static EffectCapabilityId PlaceInRadius { get; } =
            new EffectCapabilityId("synthetic-place-in-radius");

        public static EffectCapabilityReference PlaceInRadiusV1 =>
            new EffectCapabilityReference(PlaceInRadius, 1);

        /// <summary>
        /// Milestone 8.35z29. The printed-Range arm measured as a <em>ring</em> rather than a disk -
        /// Misa's Watch How I Soar and Leaf on the Wind, "Place yourself into a space at maximum
        /// range", on faces printing a Range 4 icon.
        ///
        /// <para><b>A fourth capability ID rather than a parameter, by the rule
        /// <see cref="PlaceInRange"/> states.</b> p.9 gives "in range" a settled meaning - up to the
        /// printed value - and the user's 2026-09-06 ruling gives "at maximum range" the other one, so
        /// the two are different reaches and not two settings of one. Naming them apart is also what
        /// keeps the difference auditable from a definition alone: a reader who sees this reference
        /// knows the candidate set is a ring without opening the handler.</para>
        ///
        /// <para><b>What it is the first Place in this build to admit: nowhere to go.</b> A disk drawn
        /// around a hero standing anywhere on a playable board has empty members almost always; a ring
        /// of radius four need not. So a mandatory node carrying this reference reports itself
        /// impossible as an ordinary outcome, which milestone 8.35l's offer machinery must show as a
        /// greyed control rather than accept and then refuse.</para>
        /// </summary>
        public static EffectCapabilityId PlaceAtMaximumRange { get; } =
            new EffectCapabilityId("synthetic-place-at-maximum-range");

        public static EffectCapabilityReference PlaceAtMaximumRangeV1 =>
            new EffectCapabilityReference(PlaceAtMaximumRange, 1);

        /// <summary>
        /// Milestone 8.35z29. The printed-Radius arm whose destinations must themselves stand
        /// beside two or more enemy units - Misa's Swoop In, "Place yourself into a space in radius
        /// adjacent to two or more enemy units", on a face printing a Radius 3 icon.
        ///
        /// <para><b>The threshold is in the name, and that is deliberate.</b> Milestone 8.35z28's Dodger
        /// work established the rule for this catalog: a printed number that qualifies a predicate
        /// belongs in the identity of the thing declared, not in a field beside it, because a shared
        /// declaration carrying a new field moves every hero who already referenced it. Every verified
        /// face printing this clause prints "two or more"; a face printing a different number would be
        /// a different capability here.</para>
        ///
        /// <para><b>Why the count is not <see cref="SyntheticRosterCapabilities.PresenceCountV1"/>.</b>
        /// That one asks the question once, around the actor. This one asks it of every candidate
        /// space, because the sentence qualifies where Misa is going rather than where she is - and a
        /// hero beside nobody may still have a destination beside two.</para>
        /// </summary>
        public static EffectCapabilityId PlaceInRadiusBesideTwoOrMoreEnemies { get; } =
            new EffectCapabilityId("synthetic-place-in-radius-beside-two-or-more-enemies");

        public static EffectCapabilityReference PlaceInRadiusBesideTwoOrMoreEnemiesV1 =>
            new EffectCapabilityReference(PlaceInRadiusBesideTwoOrMoreEnemies, 1);

        /// <summary>
        /// Milestone 8.38b. The printed-Radius arm whose destinations stand on one of rulebook p.19's
        /// straight lines through the acting hero - Swift's Steam Jump, Assault Jump and Drop Trooper,
        /// "Place yourself into a space <b>in a straight line</b> in radius", on faces printing a
        /// Radius icon.
        ///
        /// <para><b>A separate capability ID rather than a parameter, by the rule
        /// <see cref="PlaceInRange"/> states and milestone 8.35z28 generalised:</b> a printed clause
        /// that qualifies a declaration belongs in the identity of the thing declared, because a
        /// shared declaration gaining a field moves every hero who already referenced it. It is also
        /// what keeps the difference auditable from a definition alone.</para>
        ///
        /// <para><b>It shares its geometry with the Attacks that print the same words.</b> The
        /// membership question is <see cref="GuardsDigital.Core.Board.StraightLineQuery"/>'s, measured
        /// from the actor's own space, which is the same one milestone 8.38a's
        /// <c>AttackTargetLine.InStraightLine</c> reads. Swift prints the clause on both a targeting
        /// sentence and a placement sentence, and a build where those two meant different lines would
        /// be a build whose own cards disagreed about p.19.</para>
        ///
        /// <para><b>Like the ring above, it can legally be empty.</b> Six files through a hero, cut to
        /// the printed radius and then to empty playable spaces, is a much thinner set than the disk;
        /// a mandatory node carrying this reports itself impossible as an ordinary outcome and the
        /// offer machinery greys it rather than accepting and then refusing.</para>
        /// </summary>
        public static EffectCapabilityId PlaceInRadiusInStraightLine { get; } =
            new EffectCapabilityId("synthetic-place-in-radius-in-straight-line");

        public static EffectCapabilityReference PlaceInRadiusInStraightLineV1 =>
            new EffectCapabilityReference(PlaceInRadiusInStraightLine, 1);

        /// <summary>
        /// Milestone 8.38b. The complement of the declaration above - Swift's Delayed Jump and Mobile
        /// Scout, "Place yourself into a space in radius <b>not in a straight line from you</b>", on
        /// faces printing a Radius icon.
        ///
        /// <para><b>Two declarations rather than one carrying a direction, for the reason the whole
        /// file gives</b>, and with one consequence worth stating: because both read the same
        /// membership question, the two candidate sets partition the disk exactly, and
        /// <c>PlacementLineQualifierTests</c> asserts that partition rather than either set's contents.
        /// A card that demanded the file and a card that forbade it could not otherwise be shown to
        /// disagree about nothing.</para>
        /// </summary>
        public static EffectCapabilityId PlaceInRadiusOffEveryStraightLine { get; } =
            new EffectCapabilityId("synthetic-place-in-radius-off-every-straight-line");

        public static EffectCapabilityReference PlaceInRadiusOffEveryStraightLineV1 =>
            new EffectCapabilityReference(PlaceInRadiusOffEveryStraightLine, 1);

        /// <summary>
        /// Milestone 8.57b. The printed-Range arm whose destinations stand beside a hero an earlier
        /// node has already named - Rowenna's Silver basic Throw the Gauntlet, "Place yourself into a
        /// space <b>in range adjacent to an enemy hero in range</b>", on a face printing a Range 4
        /// icon.
        ///
        /// <para><b>A separate capability ID rather than a parameter on
        /// <see cref="PlaceInRange"/>, by the rule <see cref="PlaceInRange"/> states and milestone
        /// 8.35z28 generalised:</b> a printed clause that qualifies a declaration belongs in the
        /// identity of the thing declared, because a shared declaration gaining a field moves every
        /// hero who already referenced it - and Arien's three leaps do. It is the same
        /// <see cref="PlaceHandler"/> underneath, so p.18's Place, its immunity question and its
        /// destination query stay written once, and it is therefore no new handler TYPE at all.</para>
        ///
        /// <para><b>Why the hero is BOUND rather than derived.</b> Misa's Swoop In qualifies its
        /// destinations by a count of enemies and never needs to know which ones; this sentence
        /// continues "<b>that hero</b> may move 1 space", so the qualifier and the next clause have to
        /// agree about one identity. A destination adjacent to two enemy heroes would leave "that
        /// hero" ambiguous if the space were chosen first, so the hero is chosen first - by an
        /// ordinary <c>synthetic-range-hero-target@1</c> selection that reads the same printed Range
        /// this Place reads, and whose published space arrives here as
        /// <see cref="BesideSpaceParameterName"/>. Reading the clause the other way round would need a
        /// second decision the card does not print.</para>
        ///
        /// <para><b>Both "in range" bounds are the same printed icon read twice, and deliberately so.</b>
        /// p.9 makes a card's Range one number with p.16's items composed onto it; the hero selection
        /// asks <see cref="GuardsDigital.Core.Rules.CardResolution.RangeValueQuery"/> for it and so
        /// does this Place, so an item that widens her Range widens both halves of the sentence
        /// together and neither half can invent reach the other does not have.</para>
        ///
        /// <para><b>Like the ring and the two line arms, it can legally be empty.</b> An enemy hero
        /// hemmed in by units and terrain qualifies no space at all, and a mandatory node carrying
        /// this reports itself impossible as an ordinary outcome - which milestone 8.35l's offer
        /// machinery greys rather than accepting and then refusing. That is also what the naming
        /// selection's own consumer declaration is for: a hero with no qualifying space beside him is
        /// not offered in the first place.</para>
        /// </summary>
        public static EffectCapabilityId PlaceInRangeBesideANamedHero { get; } =
            new EffectCapabilityId("synthetic-place-in-range-beside-a-named-hero");

        public static EffectCapabilityReference PlaceInRangeBesideANamedHeroV1 =>
            new EffectCapabilityReference(PlaceInRangeBesideANamedHero, 1);

        /// <summary>
        /// Milestone 8.57d. The printed-Radius twin of <see cref="PlaceInRangeBesideANamedHero"/>, and
        /// the first declaration in this file whose subject is somebody OTHER than the acting hero -
        /// Rowenna's Close Quarters, Melee and Grand Melee, "Place a friendly minion <b>in radius</b>
        /// into a space <b>adjacent to that enemy hero</b>", on faces printing a Radius 2 or 3 icon.
        ///
        /// <para><b>A separate capability ID rather than a parameter on
        /// <see cref="PlaceInRangeBesideANamedHero"/>, by the rule <see cref="PlaceInRange"/> states
        /// and milestone 8.35z28 generalised:</b> p.9 gives Range and Radius two meanings and p.16 two
        /// items, so one declaration spanning both would let a Radius card be widened by a Range item.
        /// It is the same <see cref="PlaceHandler"/> and the same qualifier underneath - 8.57b's
        /// <c>AdjacentToTheNamedSpace</c>, unchanged - so this milestone writes no handler code at all
        /// and adds no handler TYPE.</para>
        ///
        /// <para><b>The reach bound is the card's own printed Radius read a SECOND time, and it can
        /// never exclude a space the sentence admits.</b> The printed "in radius" bounds the MINION
        /// this Place takes, which is the selection's business; what this arm carries is p.18's own
        /// requirement that a Place measure its destinations from somewhere. The named enemy hero is
        /// adjacent to Rowenna - the rider's own predicate says so before the modal is ever offered,
        /// and neither bullet moves her or him - so every neighbour of his stands at most two spaces
        /// from her, while every face printing this clause prints a Radius of 2 or 3 and p.16's item
        /// only ever raises it. The intersection is therefore exactly the six neighbours, which
        /// <c>RowennaCloseQuartersTests</c> measures rather than assumes.</para>
        ///
        /// <para><b>The subject is a minion and the destination is measured from a hero, which is what
        /// makes this different from every arm above.</b> Arien's leaps, Misa's ring, Swift's files and
        /// Rowenna's own gauntlet all place the ACTOR; here the acting hero chooses a piece that is not
        /// herself, so <see cref="PlaceHandler"/>'s immunity question and milestone 8.7's
        /// place-restriction question are asked about that minion - an immune enemy minion is not a
        /// subject this sentence can take, and the selection that names it declares this node as its
        /// consumer so the refusal greys the minion rather than being discovered after it is picked.
        /// </para>
        ///
        /// <para><b>Like every qualified arm it can legally be empty</b>, and here that is ordinary
        /// rather than cornered: an enemy hero hemmed in by units and terrain qualifies no space, and
        /// the bullet is then simply not offered - which is what <c>Choose</c> does with an option it
        /// can prove unperformable.</para>
        /// </summary>
        public static EffectCapabilityId PlaceInRadiusBesideANamedHero { get; } =
            new EffectCapabilityId("synthetic-place-in-radius-beside-a-named-hero");

        public static EffectCapabilityReference PlaceInRadiusBesideANamedHeroV1 =>
            new EffectCapabilityReference(PlaceInRadiusBesideANamedHero, 1);

        /// <summary>
        /// Milestone 8.57b. The space the destinations of
        /// <see cref="PlaceInRangeBesideANamedHero"/> - and, since milestone 8.57d, of
        /// <see cref="PlaceInRadiusBesideANamedHero"/> - must stand beside, which is where the hero an
        /// earlier node named is standing.
        ///
        /// <para>A coordinate rather than a hero, which is the subject identity milestone 6.4 chose
        /// for every repositioning operation in this build and which p.19's one-obstacle-per-space rule
        /// makes unambiguous. It is carried by the two declarations whose clause names a piece, for the
        /// reason <see cref="SpawnPointExclusionParameterName"/> is carried by the printed arms alone:
        /// a parameter nothing sets is the ambiguity the Range/Radius split exists to refuse.</para>
        /// </summary>
        public const string BesideSpaceParameterName = "besideSpace";

        /// <summary>The eligible-piece parameter's name, spelled once for the reason the Push's is.</summary>
        public const string EligiblePieceParameterName = "eligiblePiece";

        /// <summary>The destination-anchor parameter's name, spelled once for the same reason.</summary>
        public const string DestinationAnchorParameterName = "destinationAnchor";

        /// <summary>
        /// The Spawn-point exclusion parameter's name, carrying a
        /// <see cref="PlaceSpawnPointExclusion"/>. Milestone 8.20.
        /// </summary>
        public const string SpawnPointExclusionParameterName = "spawnPointExclusion";

        public static EffectCapabilityReference SpawnV1 =>
            new EffectCapabilityReference(Spawn, 1);

        /// <summary>
        /// Milestone 8.35z28. `respawn-friendly-minion-adjacent@1` - rulebook p.19's Respawn, into
        /// an empty friendly minion Spawn point one space away.
        ///
        /// <para>Dodger the Warlock's Necromancy names it: "Respawn a friendly minion in an empty
        /// friendly spawn point <b>adjacent to you</b> in the battle zone." The face prints no Range and
        /// no Radius icon at all, so the reach is a number the sentence writes down and p.16's items
        /// never raise it - which is why this is a separate declaration from the printed-Radius twin
        /// below, by the rule the Range/Radius selection pairs have recorded since milestone 8.20.</para>
        ///
        /// <para><b>It is a Respawn and not the <see cref="Spawn"/> above.</b> That capability puts a
        /// miniature back on the Spawn point it belongs to and asks p.19's eligibility nothing; this one
        /// takes a chosen point, and p.19 attaches a count to every Respawn - "more minion spawn points
        /// of that type and color (empty or not), than there are minion miniatures of that type and
        /// color present in that Battle Zone". <c>MinionRespawnEligibilityQuery</c> is that rule, stated
        /// once for both faces.</para>
        ///
        /// <para><b>At a printed point the point decides what comes back.</b> That is the user's
        /// 2026-09-06 ruling on D-07, following p.8's own respawn - "Respawn one minion of the matching
        /// type in each Spawn point in the new Battle Zone" - and a Heavy is excluded by the arithmetic
        /// rather than by a rule written here.</para>
        ///
        /// <para><b>At a point Dodger's Ultimate granted there is a second question, and RULING 48 is
        /// what puts it there.</b> "Respawn anywhere in reach": post-Ultimate every empty space in the
        /// card's reach is a destination, and a granted point names no type for the point to decide
        /// with. p.19 answers from its own side instead - the page's eligibility count already says
        /// which types may return at all - so the type comes from
        /// <c>MinionRespawnEligibilityQuery.EligibleTypesFor</c>, and where the page admits more than
        /// one, p.10's default hands the choice to the actor. This node therefore asks up to two
        /// questions, on the same one-capability-asking-twice shape p.18's batch Push has used since
        /// milestone 8.8; one eligible type opens no control at all, for the reason milestone 8.20b
        /// gives about a single printed push distance.</para>
        /// </summary>
        public static EffectCapabilityId RespawnFriendlyMinionAdjacent { get; } =
            new EffectCapabilityId("synthetic-respawn-friendly-minion-adjacent");

        public static EffectCapabilityReference RespawnFriendlyMinionAdjacentV1 =>
            new EffectCapabilityReference(RespawnFriendlyMinionAdjacent, 1);

        /// <summary>
        /// Milestone 8.35z28. `respawn-friendly-minion-in-radius@1` - the printed-Radius twin, for
        /// Dodger's Necromastery: "Respawn a friendly minion in an empty friendly spawn point <b>in
        /// radius</b> in the battle zone", on a card printing a Radius 1 icon that p.16's Radius item
        /// raises.
        /// </summary>
        public static EffectCapabilityId RespawnFriendlyMinionInRadius { get; } =
            new EffectCapabilityId("synthetic-respawn-friendly-minion-in-radius");

        public static EffectCapabilityReference RespawnFriendlyMinionInRadiusV1 =>
            new EffectCapabilityReference(RespawnFriendlyMinionInRadius, 1);

        /// <summary>The minion a Respawn put back on the board.</summary>
        public const string RespawnedOutputName = "respawned";

        /// <summary>The Spawn point it went onto, as a coordinate.</summary>
        public const string RespawnedAtOutputName = "respawnedAt";

        /// <summary>
        /// The whole vocabulary, for a bounded fixture catalog that exercises every declaration this
        /// file makes. It is deliberately not what the build registers - see
        /// <see cref="DeferredHandlers"/>.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            RegisteredHandlers().Concat(DeferredHandlers());

        /// <summary>
        /// The part a printed card in this build actually reaches, which is what
        /// <see cref="SyntheticEffectCapabilities.CreateCatalog"/> registers.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new ActorSpaceHandler(),
                new PlaceHandler(PlaceV2, PrintedValueChannel.None),
                new PlaceHandler(PlaceInRangeV1, PrintedValueChannel.Range),
                new PlaceHandler(PlaceInRadiusV1, PrintedValueChannel.Radius),
                new SpawnHandler(),

                // Milestone 8.35z34. p.19's two Respawns, moved across by the milestone that deals
                // Dodger the Warlock: Necromancy starts the adjacent arm and Necromastery the
                // printed-Radius one, and both are now cards a configured match puts in a hand. The
                // showcase half of the gate is answered by that milestone's Red Gravedigger, because
                // RespawnFriendlyMinionHandler is a handler type this build had never registered.
                new RespawnFriendlyMinionHandler(
                    RespawnFriendlyMinionAdjacentV1, readsPrintedRadius: false),
                new RespawnFriendlyMinionHandler(
                    RespawnFriendlyMinionInRadiusV1, readsPrintedRadius: true),

                // Milestone 8.35z38. Misa's two, moved across by the milestone that deals her: Watch
                // How I Soar starts the exact-Range ring and Swoop In the qualified Radius, and both
                // are now cards a configured match puts in a hand. These owe the showcase half of the
                // gate NOTHING, and it is worth saying why rather than leaving it to be noticed: both
                // are further declarations over PlaceHandler, the type the three arms above have
                // registered since milestone 6.6, and that half of the gate is asked of a handler
                // TYPE. They move the conformance count by nought for exactly the same reason.
                new PlaceHandler(
                    PlaceAtMaximumRangeV1, PrintedValueChannel.Range,
                    PlaceDestinationScope.EmptySpacesAtExactlyRadius),
                new PlaceHandler(
                    PlaceInRadiusBesideTwoOrMoreEnemiesV1, PrintedValueChannel.Radius,
                    PlaceDestinationScope.EmptySpacesWithinRadiusAndInScope,
                    PlaceDestinationQualifier.AdjacentToTwoOrMoreEnemyUnits),

                // Milestone 8.38b built these two and milestone 8.38e moves them across, which is the
                // milestone that deals Swift: her Blue line is now cards a configured match puts in a
                // hand, so both halves of milestone 7.9's gate are answered at once. Like Misa's pair
                // above they owe the showcase half NOTHING and move the conformance count by nought,
                // for the same reason stated there - both are further declarations over PlaceHandler,
                // and that half of the gate is asked of a handler TYPE.
                new PlaceHandler(
                    PlaceInRadiusInStraightLineV1, PrintedValueChannel.Radius,
                    PlaceDestinationScope.EmptySpacesWithinRadiusAndInScope,
                    PlaceDestinationQualifier.OnAStraightLineFromTheActor),
                new PlaceHandler(
                    PlaceInRadiusOffEveryStraightLineV1, PrintedValueChannel.Radius,
                    PlaceDestinationScope.EmptySpacesWithinRadiusAndInScope,
                    PlaceDestinationQualifier.OffEveryStraightLineFromTheActor)
            };

        /// <summary>
        /// Every declaration in this file that no dealt card names yet, held out of the registered set
        /// by milestone 7.9's deferral gate - which refuses a registration no printed card can start,
        /// and which is also what keeps the pinned capability list, and therefore every snapshot's
        /// bytes, from moving for a hero who is not in a match. It is ONE list per file, under the one
        /// name the content wave settled on, so that what is deferred can be read and counted without a
        /// caller having to know which hero deferred it.
        ///
        /// <para>Milestone 8.35z28's two Respawns left this list at milestone 8.35z34, which deals
        /// Dodger; milestone 8.35z29's two Places left it at milestone 8.35z38, which deals Misa;
        /// milestone 8.38b's two straight-line Places left it at milestone 8.38e, which deals Swift.
        /// All three pairs are now cards a match puts in a hand, and the gate that held them here is
        /// satisfied on both halves at once - see <see cref="RegisteredHandlers"/>.</para>
        ///
        /// <para>Milestone 8.38b's note that the list was empty said this file should go on being able
        /// to make that statement, and MILESTONE 8.57b IS THE NEXT WORKER IT WAS WRITTEN FOR. Rowenna's
        /// Throw the Gauntlet names <see cref="PlaceInRangeBesideANamedHeroV1"/> and no configured match
        /// deals her yet - 8.57g is the milestone that does - so the declaration is held here rather
        /// than in the registered set, and milestone 7.9's gate is satisfied by that deferral rather
        /// than by a showcase consumer. Its second half has nothing to ask of this one in any case: it
        /// is a further declaration over <see cref="PlaceHandler"/>, the type the three arms above have
        /// registered since milestone 6.6, and that half of the gate is asked of a handler TYPE.</para>
        ///
        /// <para>MILESTONE 8.57d ADDS THE SECOND, and it is the same hero's: her three Green Movement
        /// faces name <see cref="PlaceInRadiusBesideANamedHeroV1"/>, which is the printed-Radius twin of
        /// the declaration above. Both leave this list together at 8.57g, which is what deals her, and
        /// both owe the gate's second half nothing for the reason just given.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new PlaceHandler(
                    PlaceInRangeBesideANamedHeroV1, PrintedValueChannel.Range,
                    PlaceDestinationScope.EmptySpacesWithinRadiusAndInScope,
                    PlaceDestinationQualifier.AdjacentToTheNamedSpace),

                // Milestone 8.57d. The printed-Radius twin, declared over the SAME handler with the
                // SAME qualifier - the channel is the whole of the difference, which is the
                // Range/Radius rule stated once more rather than a second mechanism.
                new PlaceHandler(
                    PlaceInRadiusBesideANamedHeroV1, PrintedValueChannel.Radius,
                    PlaceDestinationScope.EmptySpacesWithinRadiusAndInScope,
                    PlaceDestinationQualifier.AdjacentToTheNamedSpace)
            };

        /// <summary>
        /// Milestone 8.35z29. What a Place's destinations must themselves satisfy, beyond being
        /// empty and in reach - or <see cref="None"/> for every declaration written before Misa's, none
        /// of which qualifies its destinations at all.
        ///
        /// <para>It is the handler's own vocabulary rather than <see cref="PlacePolicy"/>'s because
        /// answering it needs the unit roster and the acting hero's side, which
        /// <see cref="PlaceDestinationQuery"/> deliberately does not take: every other clause that
        /// query answers is a question about the map. The handler answers this one, hands the
        /// qualifying spaces down as the policy's scope, and hands the same set to the commit.</para>
        /// </summary>
        private enum PlaceDestinationQualifier
        {
            None = 0,

            /// <summary>
            /// Swoop In's clause. "Enemy" is measured against the acting hero's side and "units" is
            /// p.19's word - heroes and minions both - so an immune Heavy minion counts, p.18 making
            /// immunity irrelevant to a presence question.
            /// </summary>
            AdjacentToTwoOrMoreEnemyUnits = 1,

            /// <summary>
            /// Milestone 8.38b. Swift's three jump Skills: the destination stands on one of rulebook
            /// p.19's six files through the acting hero's own space. Measured by
            /// <see cref="GuardsDigital.Core.Board.StraightLineQuery"/>, which is the same question
            /// milestone 8.38a's <c>AttackTargetLine.InStraightLine</c> asks - so a build whose cards
            /// print the clause on both a targeting sentence and a placement sentence answers it once.
            /// </summary>
            OnAStraightLineFromTheActor = 2,

            /// <summary>
            /// Milestone 8.38b. The complement, which Delayed Jump and Mobile Scout print: the
            /// destination stands on no file through the actor at all. Note what this excludes without
            /// saying so - p.19 counts an adjacent space as a file, so every neighbour of the actor is
            /// out, exactly as it is for the Attack clause of the same name.
            /// </summary>
            OffEveryStraightLineFromTheActor = 3,

            /// <summary>
            /// Milestone 8.57b. Rowenna's Throw the Gauntlet: the destination stands next to the space
            /// <see cref="BesideSpaceParameterName"/> names, which is where the enemy hero an earlier
            /// node chose is standing.
            ///
            /// <para>It is the first qualifier in this handler measured from a BOUND space rather
            /// than from the actor or from the board at large - see
            /// <see cref="PlaceInRangeBesideANamedHero"/> for why the hero is named before the space
            /// and not after it.</para>
            ///
            /// <para>Milestone 8.57d reads it a second time, unchanged, for her three Green faces'
            /// "into a space adjacent to that enemy hero" - see
            /// <see cref="PlaceInRadiusBesideANamedHero"/>. Two declarations, one qualifier: the clause
            /// the two cards print is word for word the same, and the icon they print it beside is the
            /// only thing that differs.</para>
            /// </summary>
            AdjacentToTheNamedSpace = 4
        }

        /// <summary>
        /// Publishes the acting hero's own space as a typed coordinate, deciding nothing.
        ///
        /// It exists because of the gap milestone 6.4a recorded: role bindings resolve from the
        /// execution frame, which holds no board reference, so no role can supply a coordinate and the
        /// definition validator refuses one that tries. A subject therefore reaches a capability only
        /// through an earlier node's published output - and until now the only such node was a target
        /// selection, which is a decision. `Place your hero` is not a decision about which piece, so
        /// asking the actor to pick itself out of a one-candidate prompt would invent a choice the
        /// card text does not offer. This is that same bridge without the prompt.
        /// </summary>
        private sealed class ActorSpaceHandler
            : IEffectCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => ActorSpaceV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.CanDisplace ||
                    !components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var at))
                {
                    // A defeated or off-board hero stands nowhere, and a capability must be able to
                    // tell that apart from standing somewhere uninteresting.
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("space", EffectValue.Space(at))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// Applies whenever the actor stands somewhere. Publishing a space is applying, so a branch
            /// beginning here is judged performable by this node and never by what consumes the space -
            /// the declared conservative boundary of milestone 6.5ac's walk.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var components = context.Components;
                return components != null && components.CanDisplace &&
                    components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out _)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }

            /// <summary>Reading a hero's own coordinate changes nothing. Milestone 8.35l.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>
            /// Milestone 8.35l. The space this node publishes, answered before it runs.
            ///
            /// <para>This is the node the feasibility walk needed and could not have. A Place whose
            /// subject arrives from here has a required parameter bound to a later output, so the walk
            /// declared it undecidable and offered the branch unread - and Tigerclaw's Blend Into
            /// Shadows was therefore offered with Wasp's Magnetic Dagger standing, drew no prompt at
            /// all, and evaporated with the card spent. Reading a hero's own coordinate decides
            /// nothing, mutates nothing and mints nothing, which is what makes it safe to answer here
            /// rather than by executing: the walk carries the answer forward and the Place is judged on
            /// the subject it would actually receive.</para>
            /// </summary>
            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var components = context.Components;
                return components != null && components.CanDisplace &&
                    components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var at)
                    ? new[] { new EffectOutcomeFact("space", EffectValue.Space(at)) }
                    : Array.Empty<EffectOutcomeFact>();
            }
        }

        /// <summary>
        /// Rulebook p.18's Place, board arm: "Take a game piece (such as a miniature, or a token) from
        /// the board ... and put it into a different space. A game piece can only be placed into an
        /// empty space (this does not count as movement)."
        ///
        /// The subject is the piece standing at the bound coordinate, the same space-as-identity direct
        /// Move and Object Push use, which one-obstacle-per-space (p.19) makes unambiguous and which
        /// keeps a token expressible without widening the typed value union. The destination is the one
        /// decision, so this is one node.
        ///
        /// The radius bound is synthetic, not a rule: which spaces a real effect may place into comes
        /// from its card text. What is not synthetic is everything the declared
        /// <see cref="PlacePolicy"/> states - board source, any game piece, a geometric destination
        /// question rather than a route, the acting hero choosing, and what having nowhere to put it
        /// means - and the shared <see cref="PlaceOperation"/> beneath enforces all of it.
        /// </summary>
        private sealed class PlaceHandler
            : IEffectCapabilityHandler, IEffectRefusalExplainingCapabilityHandler
        {
            /// <summary>
            /// Where this declaration's reach comes from: <see cref="PrintedValueChannel.None"/> for a
            /// number the definition writes down, and <see cref="PrintedValueChannel.Range"/> for
            /// p.9's printed icon. Milestone 8.20, and the same constructor argument
            /// <see cref="SyntheticTargetingCapabilities"/>' shared selection handler takes.
            /// </summary>
            private readonly PrintedValueChannel boundChannel;

            /// <summary>
            /// Milestone 8.35z29. Which destination question this declaration asks - the disk every
            /// arm before Misa's asked, or her ring, or her qualified disk. It is a constructor
            /// argument rather than a parameter for the reason <see cref="boundChannel"/> is one: the
            /// capability's identity is what a definition names, and a reader auditing a node should
            /// not have to open a parameter to learn whether the reach is a ring.
            /// </summary>
            private readonly PlaceDestinationScope destinationScope;

            /// <summary>
            /// Milestone 8.35z29. What this declaration's destinations must satisfy in their own
            /// right, or <see cref="PlaceDestinationQualifier.None"/> on every arm that qualifies none.
            /// </summary>
            private readonly PlaceDestinationQualifier qualifier;

            public PlaceHandler(
                EffectCapabilityReference reference,
                PrintedValueChannel boundChannel,
                PlaceDestinationScope destinationScope =
                    PlaceDestinationScope.EmptySpacesWithinRadius,
                PlaceDestinationQualifier qualifier = PlaceDestinationQualifier.None)
            {
                Reference = reference;
                this.boundChannel = boundChannel;
                this.destinationScope = destinationScope;
                this.qualifier = qualifier;

                var parameters = new List<EffectCapabilityParameter>
                {
                    new EffectCapabilityParameter("subject", EffectValueKind.Coordinate)
                };
                if (boundChannel == PrintedValueChannel.None)
                {
                    parameters.Add(new EffectCapabilityParameter("radius", EffectValueKind.Integer));
                }

                parameters.Add(new EffectCapabilityParameter(
                    EligiblePieceParameterName,
                    EffectValueKind.Integer,
                    integerDomain: EffectIntegerDomain.AllExcept(
                        PlaceEligiblePieceKind.None)));
                parameters.Add(new EffectCapabilityParameter(
                    DestinationAnchorParameterName,
                    EffectValueKind.Integer,
                    integerDomain: EffectIntegerDomain.AllExcept(
                        PlaceDestinationAnchor.None)));
                if (boundChannel != PrintedValueChannel.None)
                {
                    // Carried only by the printed arm, because only a verified card printing this
                    // reading prints the clause: a parameter nothing sets is the ambiguity the
                    // Range/Radius split exists to refuse.
                    parameters.Add(new EffectCapabilityParameter(
                        SpawnPointExclusionParameterName,
                        EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.All<PlaceSpawnPointExclusion>()));
                }

                // Milestone 8.57b. Carried by the one declaration whose clause names a piece, for the
                // reason the exclusion above is carried by the printed arms alone.
                if (qualifier == PlaceDestinationQualifier.AdjacentToTheNamedSpace)
                {
                    parameters.Add(new EffectCapabilityParameter(
                        BesideSpaceParameterName, EffectValueKind.Coordinate));
                }

                Schema = new EffectCapabilityParameterSchema(
                    parameters,
                    new[] { new EffectOutputBinding("placed", EffectValueKind.Coordinate) });
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; }

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!TryDeclare(context, out var policy, out _, out var origin))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var destinations = PlaceDestinationQuery.FindDestinations(
                    policy, context.Components.Tokens, origin, QualifyingSpaces(context, policy, origin),
                    context.Modifications, context.Components.Units, context.Components.Board);
                if (destinations.Count == 0)
                {
                    return policy.NoDestinationPolicy == PlaceNoDestinationPolicy.Decline
                        ? EffectExecutionResult.DeclinedOptional()
                        : EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    destinations.Select(EffectValue.Space).ToArray(),
                    1,
                    1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate ||
                    !TryDeclare(context, out var policy, out var subject, out var measuredFrom))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // The operation re-derives the admitted destinations itself, so a space that was legal
                // when the choice opened but has since been taken is refused rather than committed.
                var outcome = PlaceOperation.PlaceFromBoard(
                    policy,
                    context.Components.Board,
                    context.Components.Tokens,
                    subject,
                    measuredFrom,
                    selected[0].Coordinate,
                    QualifyingSpaces(context, policy, measuredFrom),
                    context.Modifications,
                    context.Components.Units);
                return outcome.Accepted
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("placed", EffectValue.Space(outcome.Destination))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            /// <summary>
            /// The same declaration and destination question <see cref="Execute"/> asks, without the
            /// choice. The no-destination answer follows the node's own policy exactly as the
            /// execution's does, so a Place that would decline is reported declining rather than
            /// impossible.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                if (!TryDeclare(context, out var policy, out _, out var origin))
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                var destinations = PlaceDestinationQuery.FindDestinations(
                    policy, context.Components.Tokens, origin, QualifyingSpaces(context, policy, origin),
                    context.Modifications, context.Components.Units, context.Components.Board);
                if (destinations.Count > 0)
                {
                    return EffectExecutionResultKind.NeedsChoice;
                }

                return policy.NoDestinationPolicy == PlaceNoDestinationPolicy.Decline
                    ? EffectExecutionResultKind.DeclinedOptional
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }

            /// <summary>
            /// Milestone 8.35z29. The spaces this declaration's qualifier admits, or an empty set
            /// where it qualifies nothing - which the scopes that ignore the argument never read.
            ///
            /// <para><b>Derived on every ask, and never captured.</b> The offer, the answer and a
            /// restored pause each call this, for the reason every selection in this catalog re-derives
            /// its own candidates: a set remembered from the moment the question opened could name a
            /// space a unit has since walked away from, and Swoop In's whole clause is about who is
            /// standing where.</para>
            /// </summary>
            private IReadOnlyList<HexCoordinate> QualifyingSpaces(
                EffectCapabilityContext context, PlacePolicy policy, HexCoordinate origin)
            {
                // Milestone 8.38b. The two line qualifiers are answered from the map alone - p.19's
                // membership question needs no roster at all - so they are answered before the live
                // board is asked for, and they stay answerable on a board with nobody else on it.
                if (qualifier == PlaceDestinationQualifier.OnAStraightLineFromTheActor ||
                    qualifier == PlaceDestinationQualifier.OffEveryStraightLineFromTheActor)
                {
                    var wantOnTheLine =
                        qualifier == PlaceDestinationQualifier.OnAStraightLineFromTheActor;
                    var lineSpaces = new List<HexCoordinate>();
                    for (var dq = -policy.DestinationRadius; dq <= policy.DestinationRadius; dq++)
                    {
                        for (var dr = Math.Max(
                                 -policy.DestinationRadius, -dq - policy.DestinationRadius);
                            dr <= Math.Min(policy.DestinationRadius, -dq + policy.DestinationRadius);
                            dr++)
                        {
                            var candidate = new HexCoordinate(origin.Q + dq, origin.R + dr);
                            if (candidate.Equals(origin))
                            {
                                // The origin is the actor's own space, which no Place admits and
                                // which p.19 would count as being on every file at once.
                                continue;
                            }

                            if (StraightLineQuery.AreInStraightLine(origin, candidate) ==
                                wantOnTheLine)
                            {
                                lineSpaces.Add(candidate);
                            }
                        }
                    }

                    return lineSpaces;
                }

                // Milestone 8.57b. Throw the Gauntlet's clause, answered from the map and the bound
                // space alone: the six neighbours of where the named hero stands. Emptiness, the
                // printed reach and p.18's own refusals are the Place's to apply, and the scope is
                // intersected with them rather than duplicating any of them here - which is why a
                // neighbour off the map or under somebody costs nothing to name at this step.
                if (qualifier == PlaceDestinationQualifier.AdjacentToTheNamedSpace)
                {
                    var beside = context.Input(BesideSpaceParameterName);
                    if (beside.Kind != EffectValueKind.Coordinate)
                    {
                        // An unbound clause is not a wider Place: a declaration that names a piece and
                        // is handed none qualifies nothing, which is the reading every bound narrowing
                        // in this vocabulary takes of an unresolved binding.
                        return Array.Empty<HexCoordinate>();
                    }

                    return beside.Coordinate.GetNeighbors();
                }

                if (qualifier != PlaceDestinationQualifier.AdjacentToTwoOrMoreEnemyUnits)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var components = context.Components;
                if (components == null || !components.IsLive || components.Board == null ||
                    context.Participants == null)
                {
                    return Array.Empty<HexCoordinate>();
                }

                // p.19's units - heroes and minions both - on the side opposing the acting hero's, read
                // off the live board. Immunity is not asked: p.18 makes it a rule about what may be
                // done to a unit, and standing somewhere is not something done to it.
                //
                // The disk is the policy's own reach plus one, which is exactly as far as an enemy can
                // stand and still be adjacent to a space this Place could reach. It is a bound rather
                // than a filter, so nothing that could qualify is dropped and nothing that could not is
                // measured.
                var enemies = RosterPresenceQuery.FindUnitsWithin(
                        origin, policy.DestinationRadius + 1, RosterUnitKind.Unit,
                        SourceTeamRelation.OpposingSource, context.ActorId, components.Board,
                        context.Participants, context.Modifications)
                    .Select(unit => components.Board.TryGetObjectCoordinate(unit, out var at)
                        ? (Found: true, At: at)
                        : (Found: false, At: default(HexCoordinate)))
                    .Where(value => value.Found)
                    .Select(value => value.At)
                    .ToArray();
                if (enemies.Length < 2)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var qualifying = new List<HexCoordinate>();
                for (var dq = -policy.DestinationRadius; dq <= policy.DestinationRadius; dq++)
                {
                    for (var dr = Math.Max(-policy.DestinationRadius, -dq - policy.DestinationRadius);
                        dr <= Math.Min(policy.DestinationRadius, -dq + policy.DestinationRadius);
                        dr++)
                    {
                        var space = new HexCoordinate(origin.Q + dq, origin.R + dr);
                        if (enemies.Count(enemy => HexDistanceQuery.Calculate(enemy, space) == 1) >= 2)
                        {
                            qualifying.Add(space);
                        }
                    }
                }

                return qualifying;
            }

            /// <summary>
            /// This node's declared Place, the piece it names, and the space the question is measured
            /// from. The no-destination policy is read from the node's own
            /// <see cref="EffectFailurePolicy"/> rather than a parameter of its own, which is the
            /// decision milestone 6.4 recorded for distance optionality: p.10's mandatory-versus-`may`
            /// distinction is already carried by the four failure policies, and a second parameter
            /// saying the same thing could disagree with them.
            ///
            /// Immunity is asked of the named piece and only of the named piece. p.18 makes it a
            /// targeting rule about units, and <see cref="BoardObjectReference.AsRuleObject"/> is the
            /// named absence for a token, which p.19 says is not a unit - so a token is placed without
            /// an immunity question rather than by one that silently answers no.
            /// </summary>
            /// <summary>
            /// Milestone 8.7's question, keeping milestone 8.35l's answer.
            ///
            /// <para>Wasp's Magnetic Dagger forbids a unit being "swapped or placed by themselves, or
            /// by enemy heroes", so the piece is asked whether this actor may place it at all - here,
            /// where the piece is named, rather than at the commit, because a piece the rules will not
            /// let this hero move is not one to offer and then refuse. 8.35l keeps the applied
            /// modification rather than reducing it to a boolean, so the greyed control can name the
            /// hero and card that made it grey.</para>
            /// </summary>
            private static bool TryGetPlaceRestriction(
                EffectCapabilityContext context,
                RuleObject unit,
                out AppliedRuleModification blocking)
            {
                blocking = null;
                return unit.IsDefined && RestrictionQuery.TryGetRestriction(
                    unit,
                    RuleActivity.Place,
                    RuleObject.ForHero(context.ActorId),
                    context.Participants,
                    context.Modifications,
                    out blocking);
            }

            /// <summary>
            /// Milestone 8.35l. Which standing restriction bars this Place, asked about the same named
            /// piece <see cref="TryDeclare"/> asks about. A Place refused for any other reason - no
            /// reach, an immune piece, nowhere to put it - says nothing here, so a greyed control is
            /// never given a borrowed explanation.
            /// </summary>
            public bool TryExplainRefusal(
                EffectCapabilityContext context, out AppliedRuleModification blocking)
            {
                blocking = null;
                var components = context.Components;
                var named = context.Input("subject");
                return components != null && components.CanDisplace && components.IsLive &&
                    named.Kind == EffectValueKind.Coordinate &&
                    components.Board.TryGetObjectAt(named.Coordinate, out var named_) &&
                    TryGetPlaceRestriction(context, named_.AsRuleObject(), out blocking);
            }

            private bool TryDeclare(
                EffectCapabilityContext context,
                out PlacePolicy policy,
                out BoardObjectReference subject,
                out HexCoordinate measuredFrom)
            {
                policy = null;
                subject = default;
                measuredFrom = default;

                var components = context.Components;
                if (components == null || !components.CanDisplace || !components.IsLive)
                {
                    return false;
                }

                // Milestone 8.20. The written arm reads the number the definition states; the printed
                // arm reads p.9's icon with p.16's items already composed onto it, through the same
                // query the declared channel names - milestone 8.23 taught it the Radius twin. A card whose
                // declared channel prints no icon has no such reach at all and p.16 does not invent one,
                // which the zero total below turns into an impossible Place rather than an adjacent one.
                var radius = boundChannel == PrintedValueChannel.None
                    ? context.Input("radius").IntegerValue
                    : PrintedReach(context);
                var named = context.Input("subject");
                if (radius < 1 || named.Kind != EffectValueKind.Coordinate ||
                    !components.Board.TryGetObjectAt(named.Coordinate, out subject))
                {
                    return false;
                }

                var eligiblePiece =
                    (PlaceEligiblePieceKind)context.Input(EligiblePieceParameterName).IntegerValue;
                var anchor =
                    (PlaceDestinationAnchor)context.Input(DestinationAnchorParameterName).IntegerValue;
                if (!Enum.IsDefined(typeof(PlaceEligiblePieceKind), eligiblePiece) ||
                    eligiblePiece == PlaceEligiblePieceKind.None ||
                    !Enum.IsDefined(typeof(PlaceDestinationAnchor), anchor) ||
                    anchor == PlaceDestinationAnchor.None)
                {
                    return false;
                }

                var unit = subject.AsRuleObject();
                if (unit.IsDefined && (
                        IsImmuneTo(context, unit, context.EnclosingActivity) ||
                        IsImmuneTo(context, unit, RuleActivity.Place)))
                {
                    return false;
                }

                // Milestone 8.7. Wasp's Magnetic Dagger forbids a unit being "swapped or placed by
                // themselves, or by enemy heroes", so the piece is asked whether this actor may place
                // it at all - here, where the piece is named, rather than at the commit, because a
                // piece the rules will not let this hero move is not one to offer and then refuse.
                if (TryGetPlaceRestriction(context, unit, out _))
                {
                    return false;
                }

                if (anchor == PlaceDestinationAnchor.ActingHero)
                {
                    if (!components.Board.TryGetObjectCoordinate(
                            BoardObjectReference.ForHero(context.ActorId), out measuredFrom))
                    {
                        return false;
                    }
                }
                else
                {
                    measuredFrom = named.Coordinate;
                }

                var exclusion = boundChannel == PrintedValueChannel.None
                    ? PlaceSpawnPointExclusion.None
                    : (PlaceSpawnPointExclusion)context
                        .Input(SpawnPointExclusionParameterName).IntegerValue;
                if (!Enum.IsDefined(typeof(PlaceSpawnPointExclusion), exclusion))
                {
                    return false;
                }

                policy = new PlacePolicy(
                    PlaceSourceContainer.Board,
                    eligiblePiece,
                    destinationScope,
                    radius,
                    PlaceChooserKind.ActingHero,
                    context.FailurePolicy == EffectFailurePolicy.Mandatory
                        ? PlaceNoDestinationPolicy.Impossible
                        : PlaceNoDestinationPolicy.Decline,
                    anchor,
                    exclusion);
                return true;
            }

            /// <summary>
            /// This card's printed reach for the declared channel, with p.16's items composed on -
            /// exactly the total <see cref="SyntheticTargetingCapabilities"/>' printed-icon selections
            /// derive, asked here so a card's Place and a card's selection cannot come to measure
            /// different disks. Re-derived on every ask and never captured, for the reason those
            /// selections re-derive theirs.
            ///
            /// <para><b>The channel decides which query answers</b>, which is milestone 8.23's
            /// correction. Milestone 8.20 wrote this against <see cref="RangeValueQuery"/> alone,
            /// because the printed arm was the printed-<em>Range</em> Place and nothing else; this
            /// milestone added the printed-Radius twin, and a card printing a Radius icon and no Range
            /// icon measured its Range - zero - so the Place reported itself impossible and Blend Into
            /// Shadows could never place. The pair is read exactly as
            /// <see cref="SyntheticTargetingCapabilities"/> reads it, so a card's Place and a card's
            /// selection still measure one disk.</para>
            /// </summary>
            private int PrintedReach(EffectCapabilityContext context)
            {
                if (!context.CardState.IsAvailable)
                {
                    return 0;
                }

                return boundChannel == PrintedValueChannel.Radius
                    ? GuardsDigital.Core.Rules.CardResolution.RadiusValueQuery.Calculate(
                        context.SourceCardInstanceId,
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions,
                        context.ActorId,
                        context.HeroItems).Total
                    : GuardsDigital.Core.Rules.CardResolution.RangeValueQuery.Calculate(
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
                            context.CardState.Definitions)).Total;
            }

            /// <summary>
            /// p.18's immunity for the named piece, asked once per activity the question has: the
            /// action this Place sits inside, and p.18's Place itself. Milestone 8.7 adds the second
            /// for the reason milestone 6.4 gave the Push one - an effect family the rules name is
            /// something a unit can be immune to in its own right.
            /// </summary>
            private static bool IsImmuneTo(
                EffectCapabilityContext context, RuleObject unit, RuleActivity activity) =>
                TargetQuery.IsPrevented(
                    unit,
                    activity,
                    RuleObject.ForHero(context.ActorId),
                    context.Components.Board,
                    context.Participants,
                    context.Modifications);
        }

        /// <summary>
        /// Rulebook p.18's Spawn: "Similar to place, but unlike 'Place', the spawned object must be
        /// taken from the supply and cannot be taken from the board."
        ///
        /// The supply is the declared miniatures not currently standing, derived by the board port on
        /// every call. The destination is not a decision: this capability declares that a unit Spawns
        /// on its own configured Spawn point, which is the invoking rule's destination constraint here,
        /// and p.19 defines a Spawn point as a space "used to spawn minions and heroes". Which unit is
        /// therefore the one decision, so this is one node.
        ///
        /// It implements no eligibility rule of its own. p.18's Respawn adds one - a minion may return
        /// only while its type and colour have more Spawn points than miniatures present in the Battle
        /// Zone - and that specialized rule stays unimplemented and out of this milestone's scope, so
        /// this capability's tighter destination constraint is deliberately not the wider freedom
        /// Respawn would need. Offering only the acting hero's own team is a synthetic bound, like the
        /// adjacency bound Object Push uses: which units a real effect may Spawn comes from its card
        /// text through the reusable targeting contracts, and this milestone has no verified text.
        /// </summary>
        /// <summary>
        /// p.19's Respawn from card text: the actor names an empty friendly minion Spawn point in reach
        /// and the minion that point spawns comes back onto it. Milestone 8.35z28.
        ///
        /// <para>One handler under two declarations, split only by where the reach comes from - p.19's
        /// innermost ring, which a sentence writes down, or p.9's printed Radius icon, which p.16's item
        /// raises.</para>
        /// </summary>
        private sealed class RespawnFriendlyMinionHandler : IEffectCapabilityHandler
        {
            private readonly bool readsPrintedRadius;

            public RespawnFriendlyMinionHandler(
                EffectCapabilityReference reference, bool readsPrintedRadius)
            {
                Reference = reference;
                this.readsPrintedRadius = readsPrintedRadius;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[]
                    {
                        new EffectOutputBinding(RespawnedOutputName, EffectValueKind.Minion),
                        new EffectOutputBinding(RespawnedAtOutputName, EffectValueKind.Coordinate)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                // RESTORE SAFETY, AND IT IS MEASURED RATHER THAN DEFENSIVE. A restored pause re-enters
                // a node through Execute rather than through Resume, so a node that asks TWO questions
                // must not ask the first one again once the first has been answered. Milestone
                // 8.35z34's Play Mode work measured exactly that failure: a snapshot captured while
                // the type question stood came back showing the DESTINATION question, because Execute
                // always asked for a destination and knew nothing of the answer already recorded
                // against this node.
                //
                // The rule this states, which is wider than this handler: a capability that asks more
                // than once is only restore-safe while its Execute is idempotent with respect to
                // `AnswersAlreadyGiven`. p.18's batch Push shares the two-question shape, and
                // milestone 8.35z43 CHECKED it rather than leaving the question open: its
                // `ExecutePush` has read those answers since milestone 8.8, in the same change that
                // gave it a second question, so it never had this gap. The note here previously said
                // it consulted them only in Resume, which was a misreading of the code rather than a
                // measurement. Both tiers now pin it - `DisplacementCapabilityTests` at the handler
                // and `ConfiguredKineticSkillTests` through a real session.
                if (TryReadDestinationAnswer(context, out var alreadyAnswered))
                {
                    return ResumeWithDestination(context, alreadyAnswered);
                }

                var destinations = Destinations(context);
                if (destinations.Count == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    destinations.Select(fact => EffectValue.Space(fact.At)).ToArray(),
                    1,
                    1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // RULING 48 gives this node two questions rather than one, on the shape p.18's batch
                // Push has used since milestone 8.8: which destination, and - only at a point the
                // Ultimate granted, and only while p.19 lets more than one type back - which minion
                // type comes back. The stage is read off the answer's own kind rather than stored,
                // which is what keeps a restored pause re-deriving with exactly the answers it had.
                switch (selected[0].Kind)
                {
                    case EffectValueKind.Coordinate:
                        return ResumeWithDestination(context, selected[0].Coordinate);
                    case EffectValueKind.Minion:
                        return ResumeWithChosenType(context, selected[0].MinionId);
                    default:
                        return EffectExecutionResult.ImpossibleMandatory();
                }
            }

            /// <summary>
            /// The destination answer. A printed friendly point knows what comes back; a granted one
            /// takes p.19's own eligibility list, and asks the actor which when the page admits more
            /// than one.
            /// </summary>
            private EffectExecutionResult ResumeWithDestination(
                EffectCapabilityContext context, HexCoordinate chosen)
            {
                // Re-derived against current state rather than trusted from the paused choice, exactly
                // as the Spawn above is and for the same reason: a Spawn point empty when the choice
                // opened may have been taken before the answer arrived, and p.19's eligibility count is
                // live board state on both sides of its comparison.
                if (!TryFindDestination(context, chosen, out var destination))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                if (destination.IsPrinted)
                {
                    return Commit(context, destination, destination.MinionType);
                }

                var eligible = EligibleTypes(context, destination.TeamSide);
                if (eligible.Count == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                if (eligible.Count == 1)
                {
                    // One legal answer is not a question. The same reading milestone 8.20b took for
                    // Kinetic Repulse's single printed push distance: a control with one option is a
                    // control no player is shown.
                    return Commit(context, destination, eligible[0]);
                }

                var candidates = eligible
                    .Select(type => FindSupplyMiniature(context, destination.TeamSide, type))
                    .Where(id => id.IsDefined)
                    .Select(EffectValue.Minion)
                    .ToArray();
                if (candidates.Length == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                if (candidates.Length == 1)
                {
                    return ResumeWithChosenType(context, candidates[0].MinionId);
                }

                // p.10's default: a decision the card leaves open belongs to the player performing it.
                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor, candidates, 1, 1));
            }

            /// <summary>
            /// The type answer, which only a granted destination ever opens. The destination itself is
            /// re-read from this node's own earlier answer and re-validated, never trusted.
            /// </summary>
            private EffectExecutionResult ResumeWithChosenType(
                EffectCapabilityContext context, MinionId chosenMiniature)
            {
                if (!TryReadDestinationAnswer(context, out var chosen) ||
                    !TryFindDestination(context, chosen, out var destination) ||
                    destination.IsPrinted)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var miniature = context.Components.Units.FindUnitSupply()
                    .Where(candidate => candidate.MinionId.Equals(chosenMiniature))
                    .Where(candidate => candidate.TeamSide == destination.TeamSide)
                    .Where(candidate => EligibleTypes(context, destination.TeamSide)
                        .Contains(candidate.MinionType))
                    .Select(candidate => candidate.MinionId)
                    .FirstOrDefault(id => id.IsDefined);
                if (!miniature.IsDefined)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return CommitMiniature(context, destination, miniature);
            }

            /// <summary>The whole commit for a destination whose type is settled.</summary>
            private EffectExecutionResult Commit(
                EffectCapabilityContext context,
                EmptyMinionSpawnPointFact destination,
                StandardMinionType minionType)
            {
                var miniature = FindSupplyMiniature(context, destination.TeamSide, minionType);
                if (!miniature.IsDefined)
                {
                    // p.19's count said a miniature of this type and colour is missing from the Zone,
                    // and the supply says none is available to bring back. The two disagreeing is a
                    // board a Respawn cannot act on rather than a rule this handler should paper over.
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return CommitMiniature(context, destination, miniature);
            }

            /// <summary>
            /// p.19's placement, taken through the port half that matches the destination: a printed
            /// point is respawned onto by its own identity, and a point the Ultimate granted has no
            /// identity to name, so it goes onto the chosen empty space - which is the same commit
            /// p.8's own sidebar uses when a Spawn point cannot take its miniature.
            /// </summary>
            private static EffectExecutionResult CommitMiniature(
                EffectCapabilityContext context,
                EmptyMinionSpawnPointFact destination,
                MinionId miniature)
            {
                var components = context.Components;
                HexCoordinate at;
                if (destination.IsPrinted)
                {
                    var committed = components.Lanes.CommitValidatedRespawnAtSpawnPoint(
                        miniature, destination.SpawnPointId);
                    if (!committed.Accepted)
                    {
                        return EffectExecutionResult.ImpossibleMandatory();
                    }

                    at = committed.Destination;
                }
                else
                {
                    if (!components.Lanes.CommitValidatedLanePushPlacement(miniature, destination.At))
                    {
                        return EffectExecutionResult.ImpossibleMandatory();
                    }

                    at = destination.At;
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(RespawnedOutputName, EffectValue.Minion(miniature)),
                    new EffectOutcomeFact(RespawnedAtOutputName, EffectValue.Space(at))
                });
            }

            /// <summary>
            /// The destination this node was already answered with, read off the resolution's own
            /// accepted choices rather than remembered - so a snapshot restore resumes with exactly the
            /// answer it had. The newest coordinate answer wins, which is the same scan
            /// <c>SyntheticDisplacementCapabilities</c>' two-question push makes.
            /// </summary>
            private static bool TryReadDestinationAnswer(
                EffectCapabilityContext context, out HexCoordinate destination)
            {
                for (var index = context.AnswersAlreadyGiven.Count - 1; index >= 0; index--)
                {
                    var answer = context.AnswersAlreadyGiven[index];
                    if (answer.Count == 1 && answer[0].Kind == EffectValueKind.Coordinate)
                    {
                        destination = answer[0].Coordinate;
                        return true;
                    }
                }

                destination = default;
                return false;
            }

            /// <summary>
            /// One destination by coordinate, matched by membership rather than by
            /// <c>FirstOrDefault</c>: the fact is a struct, so a miss would come back as a default whose
            /// own coordinate is a real space on the board.
            /// </summary>
            private bool TryFindDestination(
                EffectCapabilityContext context,
                HexCoordinate chosen,
                out EmptyMinionSpawnPointFact destination)
            {
                foreach (var candidate in Destinations(context))
                {
                    if (candidate.At.Equals(chosen))
                    {
                        destination = candidate;
                        return true;
                    }
                }

                destination = default;
                return false;
            }

            private static IReadOnlyList<StandardMinionType> EligibleTypes(
                EffectCapabilityContext context, TeamSide side) =>
                MinionRespawnEligibilityQuery.EligibleTypesFor(
                    context.Components.Board,
                    side,
                    SpawnPointOverlay.ForActor(
                        context.Modifications, context.Participants, context.ActorId));

            private static MinionId FindSupplyMiniature(
                EffectCapabilityContext context, TeamSide side, StandardMinionType minionType) =>
                context.Components.Units.FindUnitSupply()
                    .Where(candidate => candidate.TeamSide == side &&
                        candidate.MinionType == minionType)
                    .Select(candidate => candidate.MinionId)
                    .FirstOrDefault(id => id.IsDefined);

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Destinations(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// "an empty friendly spawn point [adjacent to you / in radius] in the battle zone" that
            /// p.19 will actually let a minion come back into.
            /// </summary>
            private IReadOnlyList<EmptyMinionSpawnPointFact> Destinations(
                EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive || !components.CanRespawnMinions ||
                    !components.CanSpawnUnits || context.Participants == null ||
                    !components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return Array.Empty<EmptyMinionSpawnPointFact>();
                }

                var team = context.Participants.GetTeamForHero(context.ActorId);
                if (team == null)
                {
                    return Array.Empty<EmptyMinionSpawnPointFact>();
                }

                var distance = 1;
                if (readsPrintedRadius)
                {
                    if (!context.CardState.IsAvailable)
                    {
                        return Array.Empty<EmptyMinionSpawnPointFact>();
                    }

                    var radius = RadiusValueQuery.Calculate(
                        context.SourceCardInstanceId,
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions,
                        context.ActorId,
                        context.HeroItems);
                    if (radius.Total <= 0)
                    {
                        // p.16 raises a printed icon and never invents one, so a card printing no
                        // Radius has no disk - the same reading every printed-icon consumer takes.
                        return Array.Empty<EmptyMinionSpawnPointFact>();
                    }

                    distance = radius.Total;
                }

                return MinionRespawnEligibilityQuery.FindRespawnDestinationsWithin(
                    components.Board,
                    components.Tokens,
                    team.Side,
                    actorAt,
                    distance,
                    SpawnPointOverlay.ForActor(
                        context.Modifications, context.Participants, context.ActorId));
            }
        }

        private sealed class SpawnHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => SpawnV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[]
                {
                    new EffectOutputBinding("spawned", EffectValueKind.Minion),
                    new EffectOutputBinding("at", EffectValueKind.Coordinate)
                });

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
                    candidates.Select(candidate => EffectValue.Minion(candidate.MinionId)).ToArray(),
                    1,
                    1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Minion)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived against current state rather than trusted from the paused choice: a
                // Spawn point empty when the choice opened may have been taken before the answer
                // arrived, and a miniature in the supply then may be standing now.
                var chosen = selected[0].MinionId;
                if (!Candidates(context).Any(candidate => candidate.MinionId.Equals(chosen)))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var committed = context.Components.Units.CommitValidatedUnitSpawn(chosen);
                if (!committed.Accepted)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("spawned", EffectValue.Minion(chosen)),
                    new EffectOutcomeFact("at", EffectValue.Space(committed.Destination))
                });
            }

            /// <summary>
            /// Whether the supply holds a miniature this actor could Spawn onto an eligible point. This
            /// is the reading milestone 6.5ac exists for: on the first round every declared miniature
            /// stands, so the supply is empty, and a player who chose this mode before spent the card
            /// on nothing.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// The supply miniatures this Spawn may take: the acting hero's own team, whose declared
            /// Spawn point resolves on the selected map and is either empty or covered by a token
            /// alone. p.19 clears a covering token as the Spawn's own first step, so a token-covered
            /// point is legal; any other obstacle is not, because the same page admits no second
            /// occupant in a space.
            /// </summary>
            private static IReadOnlyList<SpawnableUnit> Candidates(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.CanSpawnUnits || !components.IsLive ||
                    context.Participants == null)
                {
                    return Array.Empty<SpawnableUnit>();
                }

                var side = context.Participants.GetTeamForHero(context.ActorId).Side;
                var found = new List<SpawnableUnit>();
                foreach (var candidate in components.Units.FindUnitSupply())
                {
                    if (candidate.TeamSide != side ||
                        !components.Units.TryGetSpawnPointCoordinate(candidate.SpawnPoint, out var at))
                    {
                        continue;
                    }

                    if (!components.Tokens.IsEmptyPlayableSpace(at) &&
                        !components.Tokens.PlacedTokens.Any(token => token.Coordinate.Equals(at)))
                    {
                        continue;
                    }

                    found.Add(candidate);
                }

                return found;
            }
        }
    }
}
