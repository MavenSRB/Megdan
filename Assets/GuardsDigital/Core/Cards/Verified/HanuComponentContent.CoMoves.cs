using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Hanu the Trickster's two co-moves - This Way! and That Way! - and the contracts they run on.
    /// Milestone 8.71e.
    ///
    /// <para><b>This block declares NO capability at all, and that is its headline result.</b> The
    /// series plan expected a new deferred handler, <c>hanu-friend-chooses-distance@1</c>, whose
    /// <see cref="EffectChoiceRequest"/> would offer the printed distances as Integer candidates to the
    /// named friend and grey the ones no direction admits. The greying half of that is not expressible:
    /// <c>PendingEffectChoice</c> refuses an unavailable candidate that is not an
    /// <c>EffectChoiceCandidateKind.Option</c> - "Only an option can be unperformable. A capability's
    /// value candidates are already the set it derived, so one it offered is one it can act on" - and
    /// <see cref="EffectChoiceRequest"/> carries no unavailable set for a handler to populate in the
    /// first place. A handler-offered distance list could therefore only have DROPPED an infeasible
    /// distance silently, which is the opposite of what HA-Q9 and milestone 8.70 settle.</para>
    ///
    /// <para><b>So the distances are OPTIONS, which is what the printed sentence makes them.</b> "A
    /// friendly hero in radius chooses a distance of 1 or 2" is a modal with two printed bullets whose
    /// chooser the text names, which is exactly <see cref="EffectNodeKind.Choose"/> with
    /// <see cref="EffectChooserRole.BoundHero"/> - Whisper's "An enemy hero in range chooses one" and
    /// Cutter's X Marks the Spot, shipped since milestone 8.35z33. Each bullet is one
    /// <c>coupled-direct-move@1</c> at that exact distance, so milestone 6.5ac's own walk greys a
    /// distance whose every direction is blocked, milestone 6.5ae's stop closes a modal whose every
    /// distance is blocked, and Hanu is asked for the direction inside the bullet the friend took
    /// because that capability asks the ACTOR. Nothing new is minted and nothing shared is widened; see
    /// <c>HanuEffectContent.CoMoves.cs</c> for the clause-by-clause reading and HA-Q13e.</para>
    ///
    /// <para><b>Nothing is registered, and nothing is deferred either.</b> Milestone 7.9's gate holds
    /// every Hanu declaration out of <see cref="MatchContentCatalogs"/> until 8.71h deals him; this
    /// block has no declaration to hold, so both lists below are empty and <c>HanuCoMoveTests</c>
    /// asserts the absence from both sides rather than assuming it.</para>
    /// </summary>
    public static partial class HanuComponentContent
    {
        /// <summary>
        /// Nothing. The two co-moves compose entirely on contracts this build already registers -
        /// <c>actor-space@1</c>, <c>synthetic-radius-hero-target@1</c> and
        /// <c>coupled-direct-move@1</c> - so 8.71h has no registration to add for them and no new
        /// handler type to give a showcase consumer.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> CoMoveDeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// Nothing, for the same reason, and stated rather than omitted so the pair reads the way every
        /// other block's does.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> CoMoveRegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();
    }
}
