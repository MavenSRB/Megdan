using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 6.7: the match state finishing an action boundary needs.
    ///
    /// p.18's after-action correction has always needed the participants and the Tie Breaker, and a
    /// bounded Repeat now needs the trusted card definitions, the Active-effect lifecycle, and the
    /// round and turn the next instance runs in. They travel together because they are answers to one
    /// question - what is true right now - and because the repeated instance must read them at the
    /// moment it starts rather than inherit anything the finished instance was given. That is what
    /// makes an Active effect the first instance created restrict the second.
    ///
    /// Milestone 7.1a adds the card instances for the same reason it added the definitions: a repeated
    /// Skill whose effect reads card state must read the hands as they stand when the second instance
    /// starts, including any card the first instance discarded.
    /// </summary>
    public readonly struct ActionBoundaryContext
    {
        public ActionBoundaryContext(
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            TieBreakerState tieBreaker,
            int roundIndex,
            int turnIndex)
        {
            Definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
            Instances = instances ?? throw new ArgumentNullException(nameof(instances));
            Participants = participants;
            Timing = timing;
            TieBreaker = tieBreaker;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
        }

        public ActionBoundaryContext(
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            TieBreakerState tieBreaker,
            int roundIndex,
            int turnIndex)
            : this(
                definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions)),
                instances?.ToArray() ?? throw new ArgumentNullException(nameof(instances)),
                participants,
                timing,
                tieBreaker,
                roundIndex,
                turnIndex)
        {
        }

        public IReadOnlyList<CardDefinition> Definitions { get; }
        public IReadOnlyList<CardInstance> Instances { get; }
        public MatchParticipants Participants { get; }
        public ActiveEffectLifecycle Timing { get; }
        public TieBreakerState TieBreaker { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
    }
}
