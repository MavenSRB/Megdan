using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Rules.LanePush
{
    /// <summary>What one attempt to consume a Lane-Push request did.</summary>
    public sealed class LanePushConsumptionResult
    {
        private LanePushConsumptionResult(
            LanePushPrefixRejectionReason rejectionReason,
            PendingSystemTransitionStep pendingStep,
            LanePushPrefixResult finished)
        {
            RejectionReason = rejectionReason;
            PendingStep = pendingStep;
            Finished = finished;
        }

        public LanePushPrefixRejectionReason RejectionReason { get; }
        public bool Accepted => RejectionReason == LanePushPrefixRejectionReason.None;

        /// <summary>Where the push stopped on a chooser, or <c>null</c> when it did not stop.</summary>
        public PendingSystemTransitionStep PendingStep { get; }

        /// <summary>
        /// What the push did - the stages it ran, the counters and Zone it left behind, and the victory
        /// sentence that ended the game if either did. <c>null</c> only while the push is still paused,
        /// because a refusal is also something the push did: nothing, and for a stated reason.
        /// </summary>
        public LanePushPrefixResult Finished { get; }

        public bool IsPaused => PendingStep != null;

        internal static LanePushConsumptionResult Reject(
            LanePushPrefixRejectionReason reason, LanePushPrefixResult refused) =>
            new LanePushConsumptionResult(reason, null, refused);

        internal static LanePushConsumptionResult Paused(PendingSystemTransitionStep pendingStep) =>
            new LanePushConsumptionResult(LanePushPrefixRejectionReason.None, pendingStep, null);

        internal static LanePushConsumptionResult Complete(LanePushPrefixResult finished) =>
            new LanePushConsumptionResult(LanePushPrefixRejectionReason.None, null, finished);
    }

    /// <summary>
    /// Turns a recorded Lane-Push request into rulebook p.8's transition (milestone 6.11).
    ///
    /// It exists as its own small type because there are two checkpoints and they must not drift: the
    /// end-of-round stage milestone 6.1 built and the mid-turn Initiative boundary milestone 6.9 built
    /// both consume a request the same way, and the only thing they disagree about is which request
    /// they are holding. Two copies of "begin the push, run it, and see whether it stopped" would be
    /// two places for p.8's order to be got wrong.
    ///
    /// It drives the transition and keeps no record of where the transition is, which is what milestone
    /// 6.10a's conformance rule asks of anything that does.
    /// </summary>
    public sealed class LanePushConsumption
    {
        private readonly LanePushTransitionAuthority lanePush;
        private readonly SystemTransitionAuthority systemTransitions;

        public LanePushConsumption(
            LanePushTransitionAuthority lanePush, SystemTransitionAuthority systemTransitions)
        {
            this.lanePush = lanePush ?? throw new ArgumentNullException(nameof(lanePush));
            this.systemTransitions =
                systemTransitions ?? throw new ArgumentNullException(nameof(systemTransitions));
        }

        public LanePushTransitionAuthority LanePush => lanePush;

        /// <summary>Whether a Lane Push is the transition currently stopped on a chooser.</summary>
        public bool IsPaused =>
            systemTransitions.PendingStep != null &&
            systemTransitions.PendingStep.DefinitionId.Equals(LanePushSystemTransition.DefinitionId);

        /// <summary>Whether a push is running at all, stopped or not.</summary>
        public bool IsRunning => lanePush.Running != null;

        /// <summary>
        /// Begins p.8's push for a request and runs it until it stops on a chooser or finishes.
        ///
        /// The instance identity is composed from the request that caused it rather than minted from a
        /// counter, for the reason milestone 6.10a gave: a replay of the same command prefix has to
        /// produce the same identity, and a counter would rebuild an equal match under different names.
        /// </summary>
        public LanePushConsumptionResult TryConsume(
            LanePushRequestId requestId,
            TeamSide winningSide,
            TeamSide losingSide,
            TieBreakerState tieBreakerState)
        {
            if (lanePush.Running != null)
            {
                throw new InvalidOperationException(
                    "A second Lane Push began while one was still running; p.8's pushes are one at a " +
                    "time and each of the two checkpoints stops the match while its own is unfinished.");
            }

            var rejection = lanePush.TryBeginPush(winningSide, losingSide, tieBreakerState);
            if (rejection != LanePushPrefixRejectionReason.None)
            {
                return LanePushConsumptionResult.Reject(
                    rejection,
                    LanePushPrefixResult.Reject(
                        rejection,
                        lanePush.RemainingWaveCounters,
                        lanePush.Board.CurrentBattleZoneId));
            }

            var result = systemTransitions.TryBegin(
                LanePushSystemTransition.DefinitionId,
                new SystemTransitionInstanceId($"lane-push-{requestId.Value}"));
            if (!result.Accepted)
            {
                throw new InvalidOperationException(
                    $"A validated Lane Push could not begin its transition ({result.RejectionReason}).");
            }

            return Report(result.PendingStep);
        }

        /// <summary>
        /// Closes the push after an answer that finished it, or reports that it stopped again on the
        /// next minion the sidebar owes a Place.
        /// </summary>
        public LanePushConsumptionResult ReportAfterAnswer() => Report(systemTransitions.PendingStep);

        private LanePushConsumptionResult Report(PendingSystemTransitionStep pendingStep)
        {
            return pendingStep != null
                ? LanePushConsumptionResult.Paused(pendingStep)
                : LanePushConsumptionResult.Complete(lanePush.CompletePush());
        }
    }
}
