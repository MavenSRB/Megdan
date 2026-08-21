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
    /// three readings below are the three sentences verified content prints. It is a declared kind
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
        EquidistantFromActor = 3
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
        /// </summary>
        public static bool NeedsActorPosition(DirectMoveDestinationPolicy policy) =>
            policy == DirectMoveDestinationPolicy.TowardActor ||
            policy == DirectMoveDestinationPolicy.EquidistantFromActor;

        /// <param name="actorAt">Where the acting hero stands, for a policy that measures from them.</param>
        /// <param name="moverAt">The subject's own starting space, which every comparison is against.</param>
        public static IReadOnlyList<HexCoordinate> Admit(
            DirectMoveDestinationPolicy policy,
            HexCoordinate actorAt,
            HexCoordinate moverAt,
            IEnumerable<HexCoordinate> reachable)
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

                case DirectMoveDestinationPolicy.EquidistantFromActor:
                    return EquidistantQuery.FilterEquidistantTo(actorAt, moverAt, reachable);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(policy),
                        policy,
                        "A direct Move states which destinations relative to the actor it admits.");
            }
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
    }
}
