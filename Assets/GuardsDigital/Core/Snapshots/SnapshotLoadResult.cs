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
        ComponentDefinitionRevisionMismatch = 11,

        /// <summary>
        /// Milestone 7.2a. The payload states a hero dashboard whose four item slots are not the ones
        /// this build supplies. Kept separate from the card-definition and component mismatches for the
        /// reason those two are separate from each other: a client resynchronizing after a content
        /// update needs to know which content differs, and a dashboard is what decides where an item
        /// may be placed at all rather than what a card or a token does.
        /// </summary>
        HeroDashboardMismatch = 12,

        /// <summary>
        /// Milestone 7.3. The payload places a card beside a hero dashboard that rulebook p.16 could
        /// not have put there: one that is not an Ultimate, or a second one for a hero who already has
        /// one. Both are claims about this match's own cards rather than about content this build
        /// lacks, which is why they are one reason of their own rather than a card-definition mismatch.
        ///
        /// It exists because the unlock is recorded by the card's position and nothing else. That is
        /// what keeps a single record of it, and the price of a single record is that the reader owes
        /// the check the record would otherwise have carried - so the position is verified on the way
        /// in rather than trusted.
        /// </summary>
        UnlockedUltimateMismatch = 13,

        /// <summary>
        /// Milestone 7.7. The payload does not describe exactly one ending: two of the rulebook's three
        /// victory sentences claim to have ended it, two teams have run out of Life counters, the ending
        /// names sides this match does not have, or the recorded phase and the victory paths disagree
        /// about whether the match is over at all.
        ///
        /// It is one reason rather than four for the reason milestone 6.10a's
        /// <see cref="Cards.RoundResolutionRejectionReason.IllegalSystemTransitionChoice"/> is one: they
        /// are a single fact to a caller, which is that this payload has no single winner to restore to.
        /// Which of them it was is in the message, because it is a diagnostic rather than a decision.
        ///
        /// Like milestone 7.3's ultimate mismatch, it exists because the ending is derived from state
        /// this match already holds instead of recorded again beside it. A live match cannot produce one
        /// of these - the authorities throw instead, since a second ending there would be an engine
        /// defect - so every payload that carries one was written by something other than this build.
        /// </summary>
        IncoherentGameOutcome = 14
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
