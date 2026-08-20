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
        UnavailableLanePushTransition = 14,

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
        IllegalSystemTransitionChoice = 18,

        /// <summary>
        /// Milestone 6.11: p.8's push is already running and stopped on a chooser, so the stage that
        /// began it is waiting for that answer rather than for another advance.
        /// </summary>
        IncompleteLanePushTransition = 19,

        /// <summary>
        /// Milestone 6.11: p.8's push cannot begin on this board - no Battle Zone, a Battle Zone off
        /// the ordered Lane, no Throne for the losing side, or a game that has already ended. Every one
        /// of them leaves the match exactly as it was.
        /// </summary>
        RefusedLanePushTransition = 20,

        /// <summary>
        /// Milestone 7.5: the round reached <see cref="EndOfRoundStage.LevelUpAndUpgrade"/> declaring
        /// the stage available, and this build composed no rules that can run p.15's purchase. Distinct
        /// from <see cref="MissingRequiredEndOfRoundHandler"/> for the reason
        /// <see cref="UnavailableLanePushTransition"/> is: nothing is misconfigured about a handler, the
        /// collaborator that would spend the coins is absent.
        /// </summary>
        UnavailableLevelUpTransition = 21,

        /// <summary>
        /// Milestone 7.6: the round reached <see cref="EndOfRoundStage.CollectPityCoins"/> declaring the
        /// stage available, and this composition has no coin ledger to pay from. Distinct from
        /// <see cref="UnavailableLevelUpTransition"/> only in which collaborator is absent; both say the
        /// match declared a p.8 step it cannot run.
        /// </summary>
        UnavailablePityCoinLedger = 22,

        /// <summary>
        /// Milestone 7.6: p.15's leveling is already running and stopped on an Upgrade a Card choice, so
        /// the stage that began it is waiting for that answer rather than for another advance. The
        /// sibling of <see cref="IncompleteLanePushTransition"/>, and reachable for the first time now
        /// that p.8's Pity Coin can leave a hero able to afford a level in the next round.
        /// </summary>
        IncompleteLevelUpTransition = 23,

        /// <summary>
        /// Milestone 7.9c: the named transition is one the rules begin for themselves, so a director
        /// command may not begin it. p.8's push begins from the request an action recorded and p.15's
        /// leveling from the end-of-round stage that owns it; beginning either by command would run a
        /// rulebook step outside the sequence that owns its bookkeeping. Distinct from
        /// <see cref="UnknownSystemTransition"/>, which says this build has no such definition: these
        /// two exist, and are exactly the ones a command must not name.
        /// </summary>
        SystemTransitionNotBegunByCommand = 24
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
