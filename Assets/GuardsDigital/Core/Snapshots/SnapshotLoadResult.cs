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
        MapDefinitionContentRevisionMismatch = 5
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
