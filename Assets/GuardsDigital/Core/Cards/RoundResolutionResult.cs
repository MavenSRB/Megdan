using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards
{
    public enum RoundResolutionRejectionReason
    {
        None = 0,
        RoundAlreadyEnded = 1,
        NotAwaitingCommitments = 2,
        NotResolvingTurn = 3,
        CommitmentCommandRejected = 4,
        TurnCommandRejected = 5,
        NotResolvingEndOfRound = 6,
        MissingRequiredEndOfRoundHandler = 7,
        GameAlreadyEnded = 8,
        NoPendingMinionBattleChoice = 9,
        StaleMinionBattleChoice = 10,
        WrongMinionBattleChooser = 11,
        IneligibleMinionBattleSelection = 12,
        IncompleteMinionBattleSelection = 13,

        /// <summary>
        /// The round reached <see cref="EndOfRoundStage.ResolveRequestedLanePush"/> holding a
        /// recorded Lane-Push request, and nothing consumes one yet. Distinct from
        /// <see cref="MissingRequiredEndOfRoundHandler"/>: the handler is not missing by
        /// misconfiguration, the rule it would apply is unimplemented.
        /// </summary>
        UnimplementedLanePushConsumption = 14,

        /// <summary>Milestone 6.10a: this build supplies no transition definition by that name.</summary>
        UnknownSystemTransition = 15,

        /// <summary>
        /// A transition is already running. Two at once would be two resume locations, and the
        /// continuation contract is built on there being exactly one.
        /// </summary>
        SystemTransitionAlreadyRunning = 16,

        /// <summary>No transition is paused, so there is nothing to answer.</summary>
        NoPendingSystemTransitionStep = 17,

        /// <summary>
        /// The answer is not about the step that is paused - a different running transition, a step
        /// already completed or not yet reached, somebody other than the chooser, or an option the step
        /// never offered. One reason rather than four, because they are one fact to a caller: the
        /// continuation is exactly as it was.
        /// </summary>
        IllegalSystemTransitionChoice = 18
    }

    public readonly struct RoundResolutionResult
    {
        private RoundResolutionResult(
            bool accepted,
            RoundResolutionRejectionReason rejectionReason,
            TurnCardCommitmentRejectionReason commitmentRejectionReason,
            TurnResolutionRejectionReason turnRejectionReason,
            IEnumerable<RoundResolutionEvent> events)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            CommitmentRejectionReason = commitmentRejectionReason;
            TurnRejectionReason = turnRejectionReason;
            Events = Array.AsReadOnly((events ?? throw new ArgumentNullException(nameof(events))).ToArray());
        }

        public bool Accepted { get; }

        public RoundResolutionRejectionReason RejectionReason { get; }

        public TurnCardCommitmentRejectionReason CommitmentRejectionReason { get; }

        public TurnResolutionRejectionReason TurnRejectionReason { get; }

        public IReadOnlyList<RoundResolutionEvent> Events { get; }

        public static RoundResolutionResult Accept(IEnumerable<RoundResolutionEvent> events)
        {
            return new RoundResolutionResult(
                true,
                RoundResolutionRejectionReason.None,
                TurnCardCommitmentRejectionReason.None,
                TurnResolutionRejectionReason.None,
                events);
        }

        public static RoundResolutionResult Reject(
            RoundResolutionRejectionReason reason,
            TurnCardCommitmentRejectionReason commitmentReason = TurnCardCommitmentRejectionReason.None,
            TurnResolutionRejectionReason turnReason = TurnResolutionRejectionReason.None)
        {
            if (reason == RoundResolutionRejectionReason.None)
            {
                throw new ArgumentException("A rejected round command requires a reason.", nameof(reason));
            }

            return new RoundResolutionResult(
                false,
                reason,
                commitmentReason,
                turnReason,
                Array.Empty<RoundResolutionEvent>());
        }
    }
}
