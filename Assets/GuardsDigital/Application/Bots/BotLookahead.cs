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

        /// <summary>
        /// The weights the returned score is denominated in. A caller comparing a simulated score
        /// against a standing one MUST evaluate the standing position under this same vector, or the
        /// difference it computes is a difference of units rather than of positions.
        /// </summary>
        BotEvaluationWeights Weights { get; }

        /// <summary>
        /// Announces that the live position has moved, so the next candidate is tried from where the
        /// match actually is. Cheap to call and required before every decision.
        /// </summary>
        void BeginDecision();
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

        public SessionBotLookahead(IMatchSession session)
            : this(session, null)
        {
        }

        public SessionBotLookahead(IMatchSession session, BotEvaluationWeights weights)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            Weights = weights ?? BotEvaluationWeights.Default;
        }

        /// <inheritdoc />
        public BotEvaluationWeights Weights { get; }

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
                score = BotEvaluation.Evaluate(session.Projection, perspectiveHeroId, Weights);
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

    /// <summary>
    /// FOURTEENTH amendment, tier-2. One ply tried on a THROWAWAY session restored to the live
    /// position, so that a probe can never damage the match being played.
    /// </summary>
    /// <remarks>
    /// <para><b>This exists because <see cref="SessionBotLookahead"/> must not be pointed at a live
    /// match, and that is a measured hazard rather than a cautious guess.</b> That class mutates the
    /// real session and puts it back, which is sound exactly as long as the restore succeeds. The
    /// snapshot round-trip corrective that unblocked this milestone recorded a second-order finding
    /// on the way: <c>MatchSession.RestoreSnapshot</c>'s own rollback re-deserializes the pre-image,
    /// so a payload that cannot be read throws again, uncaught, and leaves the session damaged rather
    /// than refusing and staying unchanged. On a headless measurement bench that costs a run. In a
    /// solo player's live practice match it would cost the match, and the player would have no idea
    /// why. A bot's opinion about a candidate move is never worth that risk, so the probe is moved
    /// off the live object entirely.</para>
    /// <para><b>The shadow is a real session, not a model of one.</b> It is composed from the same
    /// <see cref="MatchSessionInitialState"/> the live match was composed from and then restored to
    /// the live position before each candidate, so the rules that judge a candidate here are the same
    /// rules, at the same version, with the same content. What it is not is the live one: nothing it
    /// executes is ever observed by the host, the result stream, or any seat.</para>
    /// <para><b>The cost is one restore per candidate</b>, where the live-session probe paid one
    /// capture per decision and one restore per candidate. So this trades a per-decision capture for
    /// a per-candidate restore and buys the safety above; the measured per-decision latency is on
    /// <c>BotTierTwoTests</c>, against the 2.0 s pace a solo player actually watches.</para>
    /// <para>Poisoning works exactly as it does on the live-session probe, and for the same reason:
    /// a restore that fails means this shadow is no longer standing where the match is, and a ply
    /// measured from somewhere else is worse than no ply at all. A poisoned lookahead degrades tier-2
    /// to tier-2's static judgment, which is a strictly better bot than the one that ships to a
    /// network seat - so the failure mode is "less clever", never "broken".</para>
    /// </remarks>
    public sealed class ShadowSessionBotLookahead : IBotLookahead
    {
        private readonly IMatchSession live;
        private readonly Func<IMatchSession> shadowFactory;
        private IMatchSession shadow;
        private string decisionSnapshot;

        /// <summary>
        /// The shipped way to build one: from the live session and the description the match was
        /// composed from.
        /// </summary>
        /// <remarks>
        /// <b>This factory exists so that the shadow session is CONSTRUCTED INSIDE THE APPLICATION
        /// BOUNDARY.</b> The first version had the Unity driver call <c>new MatchSession(...)</c>
        /// itself, and <c>OnlineArchitectureConformanceTests
        /// .MatchAuthority_IsConstructedOnlyInsideTheApplicationBoundary</c> failed it by name -
        /// correctly. Authority is composed in Application and reached through
        /// <see cref="IMatchHost"/>, so that a local Director adapter and a network host submit to
        /// one executor; a presentation component that can build its own session is a presentation
        /// component that can build a second authority. A probe is not an exception to that, it is
        /// exactly the kind of thing that would quietly become one.
        /// </remarks>
        public static ShadowSessionBotLookahead ForMatch(
            IMatchSession live, MatchSessionInitialState initialState, BotEvaluationWeights weights)
        {
            if (initialState == null)
            {
                throw new ArgumentNullException(nameof(initialState));
            }

            return new ShadowSessionBotLookahead(
                live, () => new MatchSession(initialState), weights);
        }

        public ShadowSessionBotLookahead(
            IMatchSession live, Func<IMatchSession> shadowFactory, BotEvaluationWeights weights)
        {
            this.live = live ?? throw new ArgumentNullException(nameof(live));
            this.shadowFactory = shadowFactory ?? throw new ArgumentNullException(nameof(shadowFactory));
            Weights = weights ?? BotEvaluationWeights.Default;
        }

        /// <inheritdoc />
        public BotEvaluationWeights Weights { get; }

        /// <summary>How many candidate plies were actually simulated. Measurement, not logic.</summary>
        public int SimulatedPlies { get; private set; }

        /// <summary>True once a capture or a restore failed. Inert from that moment.</summary>
        public bool IsPoisoned { get; private set; }

        /// <summary>The reason the failure is latched rather than retried.</summary>
        public string PoisonReason { get; private set; }

        public void BeginDecision()
        {
            if (IsPoisoned)
            {
                return;
            }

            try
            {
                decisionSnapshot = live.CaptureSnapshot();
            }
            catch (Exception failure)
            {
                // A capture that throws is the LIVE session declining to describe itself. Nothing has
                // been done to it, so the match is unharmed - this lookahead simply has nothing to
                // stand on and says so.
                Poison(failure.GetType().Name + ": " + failure.Message);
            }
        }

        public bool TryEvaluateAfter(DirectorCommand candidate, HeroId perspectiveHeroId, out int score)
        {
            score = 0;
            if (IsPoisoned || candidate == null || decisionSnapshot == null || !perspectiveHeroId.IsDefined)
            {
                return false;
            }

            if (!TryStandWhereTheMatchIs())
            {
                return false;
            }

            MatchCommandResult applied;
            try
            {
                applied = shadow.Execute(candidate);
            }
            catch (Exception)
            {
                // A candidate the engine refuses to even attempt is one this ply has no opinion on.
                // The shadow is now standing somewhere unknown, which costs nothing: the next
                // candidate restores it before touching it.
                return false;
            }

            if (applied == null || !applied.Accepted)
            {
                return false;
            }

            score = BotEvaluation.Evaluate(shadow.Projection, perspectiveHeroId, Weights);
            SimulatedPlies++;
            return true;
        }

        private bool TryStandWhereTheMatchIs()
        {
            try
            {
                if (shadow == null)
                {
                    shadow = shadowFactory();
                    if (shadow == null)
                    {
                        Poison("the shadow session factory produced nothing");
                        return false;
                    }
                }

                var restored = shadow.RestoreSnapshot(decisionSnapshot);
                if (restored != null && restored.Accepted)
                {
                    return true;
                }

                Poison(restored?.Detail ?? "the shadow restore was refused without a reason");
                return false;
            }
            catch (Exception failure)
            {
                // The shadow may now be damaged - the rollback hazard recorded in the class remarks
                // applies to it too. That is exactly why it is the shadow that is damaged: it is
                // dropped, and a later decision builds a fresh one only if this object is ever
                // un-poisoned, which it never is.
                Poison(failure.GetType().Name + ": " + failure.Message);
                return false;
            }
        }

        private void Poison(string reason)
        {
            IsPoisoned = true;
            PoisonReason = reason;
            decisionSnapshot = null;
            shadow = null;
        }
    }
}
