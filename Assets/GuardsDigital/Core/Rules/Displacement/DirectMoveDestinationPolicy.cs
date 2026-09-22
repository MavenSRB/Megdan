using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.Movement;

namespace GuardsDigital.Core.Rules.Displacement
{
    /// <summary>
    /// Which of the destinations a card-text direct Move could reach it is actually allowed to use,
    /// relative to the acting hero.
    ///
    /// Rulebook p.11 gives card-text movement the Movement action's own reachability and nothing
    /// further; a card that narrows where the subject may end up says so in its own words, and the
    /// readings below are the sentences verified content prints. It is a declared kind
    /// rather than a pair of booleans for the reason <c>SyntheticDisplacementCapabilities</c> records
    /// beside the named Push: two flags saying "towards" and "keeping the same distance" could both be
    /// set, which is an ambiguity the definition validator refuses, and a card cannot mean both.
    ///
    /// Milestone 8.6 replaced milestone 6.4's `towardActor` boolean with this. The measure every
    /// member is judged by is p.18's `Closer (Space)` - spaces counted including spaces with obstacles
    /// - so <see cref="CloserQuery"/> and <see cref="EquidistantQuery"/> own the comparisons and this
    /// type owns only which one a card asked for.
    /// </summary>
    public enum DirectMoveDestinationPolicy
    {
        None = 0,

        /// <summary>
        /// p.11's own breadth: anywhere the Movement action's reachability admits at this distance.
        /// The card says only how far, so nothing is filtered by where the actor stands.
        /// </summary>
        Unrestricted = 1,

        /// <summary>
        /// `towards you`: only spaces strictly closer to the acting hero than the subject's own
        /// starting space. There is no separate Pull keyword - this comparison plus the node's own
        /// distance is what `towards` means.
        /// </summary>
        TowardActor = 2,

        /// <summary>
        /// `without moving it away from you or closer to you`: only spaces exactly as far from the
        /// acting hero as the subject's own starting space - the orbit Wasp's three telekinetic Move
        /// skills print.
        /// </summary>
        EquidistantFromActor = 3,

        /// <summary>
        /// Milestone 8.21. One space the text itself names, rather than a set the mover chooses from -
        /// Brogan's Onslaught, "After the attack: Move into the space it occupied", and the destination
        /// his three charges have already picked out by the time the move runs.
        ///
        /// <para>It is a fourth member rather than a distance of one because the other three narrow a
        /// set and this one replaces it. The named space is a value an earlier node published - a
        /// vacated target's space, a chosen charge destination - so the reachability walk is still the
        /// Movement action's own and the declared distance is still the budget it may spend; what this
        /// policy says is that only that one space satisfies the sentence. A declaration that moved one
        /// space in any direction would be a different card: "into the space it occupied" is not "one
        /// space away".</para>
        ///
        /// <para>It is also the only policy that opens no choice. Every other one hands the actor a set
        /// to pick from; this one names the destination, so the capability applies it rather than
        /// asking a question with one answer - the same reason milestone 6.5a routes "yourself" through
        /// the actor's own space instead of a one-candidate prompt.</para>
        /// </summary>
        NamedSpace = 4,

        /// <summary>
        /// Milestone 8.22. `to a space in radius`: only spaces inside the acting hero's own printed
        /// Radius disk - Sabina's Troop Movement, Marching Orders and Path to Victory, "Move a friendly
        /// minion in radius 1 space, to a space in radius."
        ///
        /// <para>It is a fifth member because the sentence prints the radius twice and means it twice.
        /// The first "in radius" picks the minion and is the selection's own reach; the second bounds
        /// where that minion may be put, and without it the card would move a minion one space in any
        /// direction - including one space further out of the disk, which is the opposite of what a
        /// command card does. Neither of the two actor-relative policies already here says it:
        /// <see cref="TowardActor"/> would forbid a lateral shuffle the disk plainly allows, and
        /// <see cref="EquidistantFromActor"/> would forbid stepping inward.</para>
        ///
        /// <para>The disk is p.9's, measured inclusively from where the hero stands at the moment the
        /// node runs, so a repeat that follows the hero's own Movement measures from the new space. The
        /// radius is a declared number rather than a read of the card, for the same reason
        /// `DeclaredRadius.WrittenInText` exists: the capability is given what the sentence
        /// says and never goes looking for the icon.</para>
        /// </summary>
        WithinActorRadius = 5,

        /// <summary>
        /// Milestone 8.35z28. `to a space not adjacent to you`: only spaces that are not one step from
        /// the acting hero - Dodger's Burning Skull and Blazing Skull, "After the attack: Move up to N
        /// minions adjacent to you 1 space to a space not adjacent to you."
        ///
        /// <para>It is a sixth member because none of the five says it. <see cref="TowardActor"/> and
        /// <see cref="EquidistantFromActor"/> are the wrong direction and the wrong shape;
        /// <see cref="WithinActorRadius"/> keeps a piece inside a disk where this pushes it out of the
        /// innermost ring; and <see cref="Unrestricted"/> would let the minion shuffle sideways to
        /// another space still beside her, which is exactly the outcome the clause exists to
        /// forbid.</para>
        ///
        /// <para><b>It is stated as "not adjacent" and not as "further away".</b> p.19 measures
        /// adjacency as a distance of one, and the sentence names that ring rather than a comparison
        /// against where the minion started - so a minion that could somehow end two spaces away in a
        /// direction the card allows satisfies it however it got there. The hero's own space is a
        /// distance of zero and would satisfy the letter of this test; it never arises, because the
        /// hero is standing on it and the reachability walk beneath has already refused an occupied
        /// destination.</para>
        /// </summary>
        NotAdjacentToActor = 6,

        /// <summary>
        /// Milestone 8.35z29. Strictly farther from the acting hero than the subject's own starting
        /// space - Misa's Power Shot and Thunder Shot, "you may move it 1 space, <b>to a space farther
        /// away from you</b>".
        ///
        /// <para><b>It is <see cref="TowardActor"/> read the other way, and it is written as the mirror
        /// on purpose.</b> Equal separation is not farther, exactly as it is not closer, so a sideways
        /// step around the actor is refused by both policies. Writing it as its own member rather than
        /// as a direction flag keeps the two sentences that print it - "toward you" and "farther away
        /// from you" - two declarations a reader can tell apart in a definition.</para>
        ///
        /// <para>Whether a space that satisfies it exists is an ordinary question with an ordinary
        /// negative answer: a unit pinned against the map's edge or ringed by obstacles has nowhere
        /// farther to go, and both cards print "you may", so the step is simply not taken.</para>
        /// </summary>
        AwayFromActor = 7,

        /// <summary>
        /// Milestone 8.61c. `to a space adjacent to the target`: only spaces one step from the space
        /// <see cref="DirectMoveDestinations.Admit"/> is handed as the named one - Min the
        /// Dragonmonk's Tiger Stance, "After the attack: You may move 1 space <b>to a space adjacent
        /// to the target</b>", and Dragon Stance's "1 or 2 spaces" printing of the same clause.
        ///
        /// <para><b>It is an eighth member because none of the seven says it, and the near miss is
        /// <see cref="NamedSpace"/>.</b> That one replaces the destination set with one space the
        /// text picked out - Brogan's "Move into the space it occupied" - and this one narrows the set
        /// to a RING around a space the text picked out. A card that moved into the target's own
        /// space would be a different card, and one that moved anywhere at the printed distance would
        /// be Tigerclaw's Combat Reflexes rather than a Stance. The three actor-relative comparisons
        /// measure from the acting hero and this measures from somebody else entirely, which is the
        /// whole of what makes it a new member rather than a reuse.</para>
        ///
        /// <para><b>The ring is p.19's adjacency and nothing more.</b> A distance of exactly one from
        /// the named space, measured by <see cref="HexDistanceQuery"/> like every other comparison
        /// here - so the named space itself is excluded (a distance of nought is not adjacency, and
        /// the reachability walk beneath has already refused an occupied space anyway) and a
        /// destination two steps away is out however it was reached.</para>
        ///
        /// <para><b>It narrows and never widens.</b> The declared distance is still the budget the
        /// Movement-action walk may spend and p.18's obstacles still stop it, so a space beside the
        /// target that cannot be legally walked to is not admitted - which is what keeps this a p.11
        /// move rather than the Place its <c>PlaceDestinationQualifier.AdjacentToTheNamedSpace</c>
        /// twin performs. The two are deliberately the same words: milestone 8.57b's Place qualifier
        /// answers "put it beside that space" and this answers "walk to beside that space", and a
        /// card that says "move" is never allowed to teleport.</para>
        ///
        /// <para><b>An unbound name admits nothing rather than everything</b>, exactly as
        /// <see cref="NamedSpace"/> and <see cref="WithinActorRadius"/> do: a declaration that names a
        /// piece and is handed none has a referent that has not happened, and reading it as "anywhere"
        /// would silently remove the only bound this policy exists to impose. Min's own text is why
        /// that case is reachable at all - rulebook p.14 step 6 can take her target off the board
        /// before step 7's rider runs, and a removed target is a referent the sentence no longer has.
        /// </para>
        /// </summary>
        AdjacentToTheNamedSpace = 8,

        /// <summary>
        /// Milestone 8.82. `to a space in radius` where the radius IS the card's own Radius icon as
        /// rulebook p.16 raises it - Swift's Mark for Death and Hunting Season, "Move [up to two] enemy
        /// minion[s] in radius up to 3 spaces to [a space / spaces] in radius."
        ///
        /// <para><b>It is the ninth member because the disk it draws is not a number a definition can
        /// write down.</b> <see cref="WithinActorRadius"/> takes a declared integer, which is right for
        /// text that writes the number - Xargatha's Siren's Call ends "to a space adjacent to you", and
        /// p.16's items never touch a value printed in the sentence. This one is for text that says
        /// "in radius" and therefore points at the icon: p.9 states that "if the card text mentions
        /// Range/Radius, it refers to this value", and p.16 raises that value wherever the hero prints
        /// it. Swift's Super-Shotgun grants +1 Radius, so a literal 4 on Mark for Death would be the
        /// wrong disk the moment she holds the item - which is the live playtest this member closes.</para>
        ///
        /// <para><b>The same icon read twice, by one query.</b> The sentence prints "in radius" twice:
        /// the first picks the minion and is the selection's own reach, which already asks
        /// <c>RadiusValueQuery</c> through the printed-icon selection, and the second bounds where that
        /// minion may be put. Both halves therefore read one total in one place, and cannot come to
        /// disagree about how wide her command is - the reason the Place's printed-Radius twin exists
        /// beside its declared one.</para>
        ///
        /// <para><b>A card printing no Radius admits nothing</b>, which is the printed-icon
        /// selection's own rule stated here: "an item raises a printed icon and never invents one", so
        /// a face with no icon has no such bound at all rather than an adjacent-by-default one. That
        /// joins the readings <see cref="NamedSpace"/>, <see cref="WithinActorRadius"/> and
        /// <see cref="AdjacentToTheNamedSpace"/> already take of a bound they were handed none of.</para>
        ///
        /// <para>The disk is measured from where the hero stands when the node runs, exactly as
        /// <see cref="WithinActorRadius"/>'s is, so a repeat that follows her own Movement measures
        /// from the new space.</para>
        /// </summary>
        WithinActorPrintedRadius = 9
    }

    /// <summary>
    /// The one statement of which reachable destinations a declared policy admits.
    ///
    /// One statement rather than one per caller, for the reason <see cref="PushEligibility"/> is one:
    /// every entry point of the direct Move - the live execution, the choice resumption's re-derivation
    /// and the feasibility probe - reaches this, so a probe that offered a space the commit then
    /// refused is a disagreement this shape cannot express.
    /// </summary>
    public static class DirectMoveDestinations
    {
        /// <summary>
        /// Whether the policy needs to know where the acting hero is standing.
        /// <see cref="DirectMoveDestinationPolicy.Unrestricted"/> does not, so a definition that
        /// declares it still resolves for an actor who is off the board - which is the case a hero
        /// awaiting respawn is in.
        ///
        /// <para>Milestone 8.82's <see cref="DirectMoveDestinationPolicy.WithinActorPrintedRadius"/>
        /// is classified exactly as <see cref="DirectMoveDestinationPolicy.WithinActorRadius"/> is:
        /// the two draw the same disk from the same centre and differ only in where the number comes
        /// from, so every actor-relative question about them has one answer.</para>
        /// </summary>
        public static bool NeedsActorPosition(DirectMoveDestinationPolicy policy) =>
            policy == DirectMoveDestinationPolicy.TowardActor ||
            policy == DirectMoveDestinationPolicy.EquidistantFromActor ||
            policy == DirectMoveDestinationPolicy.WithinActorRadius ||
            policy == DirectMoveDestinationPolicy.WithinActorPrintedRadius ||
            policy == DirectMoveDestinationPolicy.NotAdjacentToActor ||
            policy == DirectMoveDestinationPolicy.AwayFromActor;

        /// <param name="actorAt">Where the acting hero stands, for a policy that measures from them.</param>
        /// <param name="moverAt">The subject's own starting space, which every comparison is against.</param>
        /// <param name="namedDestination">
        /// The one space <see cref="DirectMoveDestinationPolicy.NamedSpace"/> admits, and nothing to
        /// any other policy. A named policy without one admits nothing rather than everything: the
        /// space is bound from another node's output, and an unbound binding is a move whose
        /// destination has not happened yet.
        /// </param>
        /// <param name="actorRadius">
        /// The disk <see cref="DirectMoveDestinationPolicy.WithinActorRadius"/> keeps the destination
        /// inside, and nothing to any other policy. A radius policy without one admits nothing rather
        /// than everything, for the same reason a named policy without a space does: an undeclared
        /// bound is not an absent bound, and reading it as "anywhere" would silently widen the only
        /// thing this policy exists to narrow.
        ///
        /// <para>Milestone 8.82. It is also the total
        /// <see cref="DirectMoveDestinationPolicy.WithinActorPrintedRadius"/> keeps the destination
        /// inside - one argument rather than two, because the two members draw the same disk and only
        /// disagree about where its number comes from. WHERE it comes from is the caller's question
        /// and not this filter's: a declared member is handed the definition's literal, and a printed
        /// member is handed what the caller read off the card's own icon with p.16's items composed
        /// onto it. A printed icon the card does not carry arrives here as no value at all and admits
        /// nothing, which is the same reading as an undeclared literal.</para>
        /// </param>
        /// <param name="besideSpace">
        /// Milestone 8.61c. The space
        /// <see cref="DirectMoveDestinationPolicy.AdjacentToTheNamedSpace"/> keeps the destination
        /// next to, and nothing to any other policy. It is a fourth argument rather than a reuse of
        /// <paramref name="namedDestination"/> because the two name different things - one IS the
        /// destination and this one is merely beside it - and a card binding the wrong one would be
        /// silently legal. Absent, it admits nothing, for the reason the two above do.
        /// </param>
        public static IReadOnlyList<HexCoordinate> Admit(
            DirectMoveDestinationPolicy policy,
            HexCoordinate actorAt,
            HexCoordinate moverAt,
            IEnumerable<HexCoordinate> reachable,
            HexCoordinate? namedDestination = null,
            int? actorRadius = null,
            HexCoordinate? besideSpace = null)
        {
            if (reachable == null)
            {
                throw new ArgumentNullException(nameof(reachable));
            }

            switch (policy)
            {
                case DirectMoveDestinationPolicy.Unrestricted:
                    return AsList(reachable);

                case DirectMoveDestinationPolicy.TowardActor:
                    return CloserQuery.FilterCloserTo(actorAt, moverAt, reachable);

                case DirectMoveDestinationPolicy.AwayFromActor:
                    return CloserQuery.FilterFartherFrom(actorAt, moverAt, reachable);

                case DirectMoveDestinationPolicy.EquidistantFromActor:
                    return EquidistantQuery.FilterEquidistantTo(actorAt, moverAt, reachable);

                case DirectMoveDestinationPolicy.NamedSpace:
                    return namedDestination.HasValue
                        ? AsList(reachable, namedDestination.Value)
                        : Array.Empty<HexCoordinate>();

                // Milestone 8.82. One disk, two ways of saying how wide it is: the declared member
                // carries the definition's own literal and the printed one carries the total the
                // caller read off the card's Radius icon. p.9 makes them the same geometry - a disk
                // measured inclusively from where the hero stands - so they share the filter and
                // never come to disagree about the shape of a command.
                case DirectMoveDestinationPolicy.WithinActorRadius:
                case DirectMoveDestinationPolicy.WithinActorPrintedRadius:
                    return actorRadius.HasValue && actorRadius.Value >= 0
                        ? WithinRadius(reachable, actorAt, actorRadius.Value)
                        : Array.Empty<HexCoordinate>();

                case DirectMoveDestinationPolicy.NotAdjacentToActor:
                    return OutsideAdjacency(reachable, actorAt);

                // Milestone 8.61c. p.19's ring around a space another node named, and the mirror of
                // the ring `NotAdjacentToActor` excludes - one comparison, read from the bound space
                // rather than from the actor. An unbound name admits nothing, which is the reading
                // every bound narrowing in this file takes of a referent that has not happened.
                case DirectMoveDestinationPolicy.AdjacentToTheNamedSpace:
                    return besideSpace.HasValue
                        ? WithinAdjacency(reachable, besideSpace.Value)
                        : Array.Empty<HexCoordinate>();

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(policy),
                        policy,
                        "A direct Move states which destinations relative to the actor it admits.");
            }
        }

        /// <summary>
        /// The reachable spaces lying inside the acting hero's disk. p.9 measures a Radius inclusively
        /// from the hero's own space, so the comparison is <c>&lt;=</c> and the hero's space itself is
        /// in the disk - it is excluded from the result only because it is occupied, which the
        /// reachability walk beneath has already decided rather than this filter.
        /// </summary>
        private static IReadOnlyList<HexCoordinate> WithinRadius(
            IEnumerable<HexCoordinate> reachable, HexCoordinate actorAt, int radius)
        {
            var list = new List<HexCoordinate>();
            foreach (var coordinate in reachable)
            {
                if (HexDistanceQuery.Calculate(actorAt, coordinate) <= radius)
                {
                    list.Add(coordinate);
                }
            }

            return list;
        }

        /// <summary>
        /// The reachable spaces outside the acting hero's innermost ring. p.19 measures adjacency as a
        /// distance of one, so this is that one comparison and nothing else. Milestone 8.35z28.
        /// </summary>
        private static IReadOnlyList<HexCoordinate> OutsideAdjacency(
            IEnumerable<HexCoordinate> reachable, HexCoordinate actorAt)
        {
            var list = new List<HexCoordinate>();
            foreach (var coordinate in reachable)
            {
                if (HexDistanceQuery.Calculate(actorAt, coordinate) != 1)
                {
                    list.Add(coordinate);
                }
            }

            return list;
        }

        /// <summary>
        /// The reachable spaces on the innermost ring around a space the text named. p.19 measures
        /// adjacency as a distance of one, so this is that one comparison and nothing else - the exact
        /// mirror of <see cref="OutsideAdjacency"/>, measured from the bound space instead of from the
        /// acting hero. Milestone 8.61c.
        /// </summary>
        private static IReadOnlyList<HexCoordinate> WithinAdjacency(
            IEnumerable<HexCoordinate> reachable, HexCoordinate besideSpace)
        {
            var list = new List<HexCoordinate>();
            foreach (var coordinate in reachable)
            {
                if (HexDistanceQuery.Calculate(besideSpace, coordinate) == 1)
                {
                    list.Add(coordinate);
                }
            }

            return list;
        }

        private static IReadOnlyList<HexCoordinate> AsList(IEnumerable<HexCoordinate> reachable)
        {
            var list = new List<HexCoordinate>();
            foreach (var coordinate in reachable)
            {
                list.Add(coordinate);
            }

            return list;
        }

        /// <summary>
        /// The named space, if the walk reached it. It is an intersection rather than a bare return of
        /// the name: the space has to be somewhere the mover could actually walk to, which is what
        /// keeps "move into the space it occupied" a Movement and not a Place.
        /// </summary>
        private static IReadOnlyList<HexCoordinate> AsList(
            IEnumerable<HexCoordinate> reachable, HexCoordinate named)
        {
            foreach (var coordinate in reachable)
            {
                if (coordinate.Equals(named))
                {
                    return new[] { named };
                }
            }

            return Array.Empty<HexCoordinate>();
        }
    }
}
