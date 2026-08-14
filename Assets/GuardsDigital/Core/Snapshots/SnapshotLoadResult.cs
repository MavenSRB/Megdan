using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Snapshots
{
    public enum SnapshotLoadRejectionReason
    {
        None = 0,
        MalformedPayload = 1,
        UnsupportedSchemaVersion = 2,
        CardDefinitionContentRevisionMismatch = 3,
        MapDefinitionMissing = 4,
        MapDefinitionContentRevisionMismatch = 5,

        /// <summary>
        /// A field decoded to an integer that is not a declared member of its enum. Distinct from
        /// <see cref="MalformedPayload"/> on purpose: a garbled byte stream and a payload written by
        /// a build whose enum no longer agrees with this one are different failures, and a
        /// resynchronizing client needs to tell them apart. See <see cref="SnapshotEnumReader"/>.
        /// </summary>
        UnrecognizedEnumValue = 6,

        /// <summary>
        /// Milestone 6.2a. The payload names an effect definition the configured match definitions do
        /// not provide. Distinct from a malformed payload: the bytes parsed, but they describe content
        /// this build does not have.
        /// </summary>
        EffectDefinitionMissing = 7,

        /// <summary>The payload names a capability this build does not register at all.</summary>
        EffectCapabilityMissing = 8,

        /// <summary>The capability is registered, but not at the revision the payload names.</summary>
        EffectCapabilityRevisionMismatch = 9,

        /// <summary>
        /// Milestone 6.2. The payload names a token or marker definition this build does not register.
        /// Kept separate from the effect-capability pair rather than folded into it: a component's
        /// supply size and return condition are content, and a client resynchronizing after a content
        /// update needs to know which kind of content it is missing.
        /// </summary>
        ComponentDefinitionMissing = 10,

        /// <summary>The component is registered, but not at the revision the payload names.</summary>
        ComponentDefinitionRevisionMismatch = 11
    }

    public sealed class SnapshotLoadException : Exception
    {
        public SnapshotLoadException(SnapshotLoadRejectionReason reason, string message, Exception inner = null)
            : base(message, inner)
        {
            Reason = reason;
        }

        public SnapshotLoadRejectionReason Reason { get; }
    }

    public sealed class RestoredAuthoritativeMatch
    {
        internal RestoredAuthoritativeMatch(
            RoundResolutionAuthority round,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            IReadOnlyList<CardInstance> cards)
        {
            Round = round;
            Board = board;
            Participants = participants;
            Cards = cards;
            CardActions = round.CardActions;
        }

        public RoundResolutionAuthority Round { get; }
        public AdjacentMoveAuthority Board { get; }
        public MatchParticipants Participants { get; }
        public IReadOnlyList<CardInstance> Cards { get; }
        public CardActionResolutionAuthority CardActions { get; }
    }

}
