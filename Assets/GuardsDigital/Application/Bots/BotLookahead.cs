using System;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application.Bots
{
    /// <summary>
    /// The optional one-ply capability: "if I played this, what would the position be worth?"
    /// </summary>
    /// <remarks>
    /// <para><b>Optional because a practice bot usually cannot have it, and that is not a gap to
    /// close.</b> A bot filling a seat is a network client. Its whole view is the seat projection the
    /// host sends it, which publishes other heroes' hands as counts and never their contents
    /// (<c>NetworkSeatProjection.NetworkHandSummaryRow</c>), and carries no accepted-command log to
    /// replay. There is therefore no legal <see cref="MatchSession"/> a seat could build to try a
    /// move on, and the session its scene composed at startup is frozen at the opening position and
    /// is not the live match. A bot that COULD simulate the live match would be a bot that could see
    /// six hands - so the absence of this capability at a seat is the 8.32 privacy law holding, not
    /// a defect, and it must not be "fixed".</para>
    /// <para>Where a full-information composition genuinely exists - the Director's own runtime, the
    /// staged sandbox, the offline harness that measures all of this - <see cref="SessionBotLookahead"/>
    /// supplies it. Everywhere else the policy is handed <c>null</c> and judges by
    /// <see cref="BotEvaluation"/> plus the previews the authority already publishes.</para>
    /// </remarks>
    public interface IBotLookahead
    {
        /// <summary>
        /// Scores the position that <paramref name="candidate"/> would produce, from
        /// <paramref name="perspectiveHeroId"/>'s side. Returns <c>false</c> when this candidate
        /// cannot be tried - which the caller must treat as "no opinion", never as "bad move".
        /// </summary>
        bool TryEvaluateAfter(DirectorCommand candidate, HeroId perspectiveHeroId, out int score);
    }

    /// <summary>
    /// One ply by the engine's own snapshot/restore: capture, execute the candidate, score, restore.
    /// </summary>
    /// <remarks>
    /// <para>The pre-state snapshot is captured <b>once per decision</b> via
    /// <see cref="BeginDecision"/> and reused for every candidate of that decision, because capturing
    /// it per candidate would pay the serialisation cost N times to reproduce the same string. Call
    /// <see cref="BeginDecision"/> whenever the live position has moved.</para>
    /// <para><b>This mutates the real session and puts it back.</b> That is safe only because the
    /// restore is unconditional and verified: if a restore is ever refused the object latches
    /// <see cref="IsPoisoned"/> and refuses to look ahead again, because continuing would mean
    /// playing on from a position the match never actually reached. A poisoned lookahead degrades the
    /// policy to its static judgment, which is the same thing every seat runs anyway.</para>
    /// </remarks>
    public sealed class SessionBotLookahead : IBotLookahead
    {
        private readonly IMatchSession session;
        private string decisionSnapshot;

        public SessionBotLookahead(IMatchSession session) =>
            this.session = session ?? throw new ArgumentNullException(nameof(session));

        /// <summary>How many candidate plies have actually been simulated. Measurement, not logic.</summary>
        public int SimulatedPlies { get; private set; }

        /// <summary>
        /// True once a restore was refused. The object is inert from that moment: see the remarks.
        /// </summary>
        public bool IsPoisoned { get; private set; }

        /// <summary>
        /// Captures the position every candidate of the current decision will be tried from. One
        /// capture per decision, not per candidate.
        /// </summary>
        public void BeginDecision()
        {
            if (IsPoisoned)
            {
                return;
            }

            decisionSnapshot = session.CaptureSnapshot();
        }

        public bool TryEvaluateAfter(DirectorCommand candidate, HeroId perspectiveHeroId, out int score)
        {
            score = 0;
            if (IsPoisoned || candidate == null || decisionSnapshot == null || !perspectiveHeroId.IsDefined)
            {
                return false;
            }

            MatchCommandResult applied;
            try
            {
                applied = session.Execute(candidate);
            }
            catch (Exception)
            {
                // A candidate the engine refuses to even attempt is one this ply has no opinion on.
                // Restore anyway: an exception mid-execute is exactly when the position is least
                // trustworthy.
                return Restore() && false;
            }

            if (applied != null && applied.Accepted)
            {
                score = BotEvaluation.Evaluate(session.Projection, perspectiveHeroId);
                SimulatedPlies++;
            }

            var restored = Restore();
            return restored && applied != null && applied.Accepted;
        }

        /// <summary>
        /// The reason this last failure is latched, not retried.
        /// </summary>
        public string PoisonReason { get; private set; }

        private bool Restore()
        {
            try
            {
                var result = session.RestoreSnapshot(decisionSnapshot);
                if (result != null && result.Accepted)
                {
                    return true;
                }

                PoisonReason = result?.Detail ?? "the restore was refused without a reason";
            }
            catch (Exception failure)
            {
                // MEASURED, NOT HYPOTHETICAL: capture/restore is not currently round-trip safe for
                // every live position. A self-play run of the symmetric bench met
                // `SnapshotLoadException: Snapshot names effect definition
                // 'xargatha-constrict-defeat@...', which the configured match definitions do not
                // provide for action 'primary-skill'` - a snapshot the SAME session had just written
                // and could not read back. A practice bot must not take a playtest build down over
                // that, so a throwing restore poisons this object exactly as a refused one does and
                // the policy falls back to the static judgment every seat already uses.
                PoisonReason = failure.GetType().Name + ": " + failure.Message;
            }

            IsPoisoned = true;
            decisionSnapshot = null;
            return false;
        }
    }
}
