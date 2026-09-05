using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z11. What a scene is about, so a surface can decide where to draw it and a
    /// later milestone can slot a new kind in without touching the conductor.
    /// </summary>
    public enum KineticSceneKind
    {
        None = 0,

        /// <summary>A piece on the board moved, struck, was struck, left, or came back.</summary>
        Board = 1,

        /// <summary>A card travelled between the hand and a commitment slot.</summary>
        Commitment = 2,

        /// <summary>A commitment turned face up.</summary>
        Reveal = 3,

        /// <summary>Coins moved.</summary>
        Economy = 4,

        /// <summary>A hero's Level or card changed on the rail.</summary>
        Progression = 5,

        /// <summary>
        /// Reserved for the NINETEENTH amendment's attack spotlight, which is a separate task.
        ///
        /// <para>It is declared here, unemitted, for one reason: the spotlight's scene is the only
        /// kind that will set <see cref="KineticScene.HoldsForAcknowledgement"/>, and a conductor whose
        /// hold path exists but has no kind that uses it is a hold path nobody has exercised. This
        /// member and that flag are the seam the spotlight slots into - it builds scenes of this kind
        /// and calls <see cref="KineticConductor.Acknowledge"/> from its CONTINUE button, and nothing
        /// in this file changes.</para>
        /// </summary>
        Spotlight = 6
    }

    /// <summary>
    /// Milestone 8.35z11. ONE THING - the unit the conductor plays at a time.
    ///
    /// <para><b>A scene is not one cue, and that distinction is the whole design.</b> An attack is one
    /// thing that happened, and it is a lunge AND a flash: playing them apart would read as two events
    /// and would be a lie about the match. So cues that describe one moment share a scene and run
    /// together, while separate moments - six reveals, four level-ups, a move and then an attack - are
    /// separate scenes and are staged. The amendment's "one thing at a time" is a statement about
    /// moments, not about animations.</para>
    /// </summary>
    public sealed class KineticScene
    {
        public KineticScene(
            int order,
            KineticSceneKind kind,
            IReadOnlyList<KineticCue> cues,
            float staggerAfterSeconds = KineticStaging.SceneStaggerSeconds,
            bool holdsForAcknowledgement = false,
            string label = null,
            bool hidesChrome = false)
        {
            if (kind == KineticSceneKind.None)
            {
                throw new ArgumentException("A scene requires a named kind.", nameof(kind));
            }

            var array = cues == null
                ? Array.Empty<KineticCue>()
                : System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(cues, cue => cue != null));
            if (array.Length == 0)
            {
                throw new ArgumentException(
                    "A scene is something the player sees, so it carries at least one cue.",
                    nameof(cues));
            }

            if (staggerAfterSeconds < 0f || float.IsNaN(staggerAfterSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(staggerAfterSeconds));
            }

            Order = order;
            Kind = kind;
            Cues = new ReadOnlyCollection<KineticCue>(array);
            StaggerAfterSeconds = staggerAfterSeconds;
            HoldsForAcknowledgement = holdsForAcknowledgement;
            HidesChrome = hidesChrome;
            Label = label ?? kind.ToString();

            var longest = 0f;
            for (var index = 0; index < array.Length; index++)
            {
                if (array[index].DurationSeconds > longest)
                {
                    longest = array[index].DurationSeconds;
                }
            }

            DurationSeconds = longest;
        }

        /// <summary>
        /// The rank this scene sorts to within the batch it arrived in.
        ///
        /// <para>For a scene derived from a log row it is that row's sequence, so scenes play in the
        /// order the match recorded them. For a batch of reveals it is the INITIATIVE rank, which is
        /// how principle 4's "staged in initiative order" becomes true rather than incidental: the
        /// engine emits reveals in its own roster order (<c>TurnCardCommitmentAuthority</c> walks
        /// <c>expectedHeroIds</c>), which is not initiative order, so ordering by sequence alone would
        /// stage them in an order that means nothing to a player.</para>
        /// </summary>
        public int Order { get; }

        public KineticSceneKind Kind { get; }

        /// <summary>The cues that play together because they are one moment.</summary>
        public IReadOnlyList<KineticCue> Cues { get; }

        /// <summary>The longest cue in the scene: how long the scene itself takes.</summary>
        public float DurationSeconds { get; }

        /// <summary>The deliberate beat left after this scene before the next one starts.</summary>
        public float StaggerAfterSeconds { get; }

        /// <summary>
        /// Whether the scene waits for the VIEWER rather than for the clock - the attack spotlight's
        /// CONTINUE button.
        ///
        /// <para><b>This can never make the match wait.</b> The conductor is downstream of the
        /// projection and upstream of nothing: commands, offers, prompts and the authority itself never
        /// consult it. A held scene delays this viewer's ANIMATIONS while their board, HUD and controls
        /// keep showing and accepting authoritative state, which is the same per-viewer pacing the cue
        /// queue has had since 8.35y - stated as a flag rather than left implicit.</para>
        /// </summary>
        public bool HoldsForAcknowledgement { get; }

        /// <summary>
        /// Whether this scene hides the HUD while it plays, which SHORTENS its own hold valve.
        ///
        /// <para>Milestone 8.35z12's reconciliation, and the rule is general rather than a special case
        /// for one scene kind: <b>the valve is short when the chrome is hidden.</b> A held scene that
        /// leaves the HUD up is merely slow for a viewer who walked away, so it may wait
        /// <see cref="KineticStaging.MaxHoldSeconds"/>. A held scene that has HIDDEN the HUD leaves that
        /// viewer with a client they cannot play, so it waits only
        /// <see cref="KineticStaging.ChromeHiddenHoldSeconds"/>. The attack spotlight is the first scene
        /// of the second kind and its own <c>AttackSpotlightTiming.MaximumHeldSeconds</c> is pinned
        /// equal to that constant by a test, so the two sources cannot drift apart.</para>
        /// </summary>
        public bool HidesChrome { get; }

        public string Label { get; }

        public override string ToString() =>
            Order.ToString("0000") + " | " + Kind + " | " + Cues.Count + " cue(s) | " +
            DurationSeconds.ToString("0.00") + "s";

        /// <summary>Every cue in a run of scenes, in play order. The flat view, for a caller that wants one.</summary>
        public static IReadOnlyList<KineticCue> Flatten(IReadOnlyList<KineticScene> scenes)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return Array.Empty<KineticCue>();
            }

            var flat = new List<KineticCue>();
            for (var index = 0; index < scenes.Count; index++)
            {
                if (scenes[index] == null)
                {
                    continue;
                }

                flat.AddRange(scenes[index].Cues);
            }

            return new ReadOnlyCollection<KineticCue>(flat.ToArray());
        }
    }

    /// <summary>
    /// Milestone 8.35z11. The staging constants: the beats between scenes and the bounds on how
    /// far behind the match a viewer's presentation may fall.
    /// </summary>
    public static class KineticStaging
    {
        /// <summary>
        /// The beat between one scene and the next.
        ///
        /// <para>It is deliberately NOT held to <see cref="KineticMotionDurations"/>'s 200-450ms band,
        /// because it is not a motion - it is the silence that makes two motions read as two events. A
        /// stagger inside that band would double every sequence's length for no added legibility.</para>
        /// </summary>
        public const float SceneStaggerSeconds = 0.10f;

        /// <summary>
        /// Beyond this many waiting scenes the conductor stops leaving beats and plays faster.
        ///
        /// <para>A viewer who alt-tabbed back, a resync delivering a window, an end-of-round cascade:
        /// each hands over a run of scenes at once. Playing them all at a leisurely pace would put this
        /// viewer's story minutes behind their own board, so the queue compresses under load - which is
        /// the honest reading of "the board never waits on animation".</para>
        /// </summary>
        public const int CatchUpQueueBudget = 6;

        /// <summary>How much faster a compressed queue plays.</summary>
        public const float CatchUpRate = 2.5f;

        /// <summary>
        /// The most scenes that may wait at once. Past it the OLDEST are dropped, because the newest
        /// are the ones a player is still trying to understand and the end state is projection-true
        /// either way - the same trade milestone 8.35y made for a burst, applied to a queue.
        /// </summary>
        public const int MaxQueuedScenes = 24;

        /// <summary>
        /// The longest a scene may hold waiting for a viewer who is not going to answer.
        ///
        /// <para>Nothing about the match depends on the answer, so this is not a timeout on a decision;
        /// it is the guarantee that a player who walks away comes back to a board that caught up rather
        /// than to a spotlight frozen since round three.</para>
        /// </summary>
        public const float MaxHoldSeconds = 20f;

        /// <summary>
        /// The same valve for a scene that has HIDDEN the HUD - milestone 8.35z12's one flagged conflict
        /// between the two conductors that were designed in parallel, resolved as a rule rather than as
        /// a special case.
        ///
        /// <para>Twenty seconds of a paused BOARD is merely slow: everything a player needs is still on
        /// screen and still accepting input. Twenty seconds of a HIDDEN HUD is a client they cannot use,
        /// because the controls are not there to press. So the valve tracks the chrome and not the scene
        /// kind: <see cref="KineticScene.HidesChrome"/> selects this one.</para>
        ///
        /// <para>It is the attack spotlight's own <c>AttackSpotlightTiming.MaximumHeldSeconds</c>, and
        /// <c>TheChromeHiddenHoldValve_IsTheSameNumberOnBothSidesOfTheReconciliation</c> asserts the two
        /// constants are equal so no future edit can move one without the other.</para>
        /// </summary>
        public const float ChromeHiddenHoldSeconds = 6f;
    }

    /// <summary>
    /// Milestone 8.35z11 (the TWENTIETH AMENDMENT's principle 3). The CONDUCTOR: the cue queue
    /// gains a sense of time, so events play one at a time with a deliberate beat between them instead
    /// of arriving as simultaneous soup.
    ///
    /// <para><b>What actually changed, in one sentence.</b> Milestone 8.35y started every cue of a
    /// projection change at once and dropped the oldest when more than six arrived; this plays them in
    /// order, one moment at a time, and drops only when a viewer has fallen a long way behind.</para>
    ///
    /// <para><b>Why queueing is safe here when it would not be in a naive view,</b> and this is the one
    /// correctness point worth reading twice. Deferring a cue means playing it against a projection
    /// LATER than the one it was derived from. For every cue kind but one that is harmless, because a
    /// cue is a relative flourish over an authoritative placement the view recomputes every frame: a
    /// lunge toward a piece that has since gone finds no target and leans nowhere; a flash on a missing
    /// piece finds no piece; a defeat's numbers are inert. The exception is a SLIDE, which is drawn as
    /// an offset back along a route whose end the projection is assumed to stand the piece at - and if
    /// the piece has moved on, that assumption is false and the offset draws it somewhere meaningless.
    /// <see cref="KineticRouteFreshness.IsRouteStillCurrent"/> is that check, and the view applies route
    /// travel only when it passes. A stale slide costs a flourish; without the check it would cost a
    /// piece appearing where nothing is.</para>
    ///
    /// <para><b>It holds no reference to anything authoritative.</b> No session, no projection, no
    /// command. It cannot delay a prompt, a command or a frame of the board, which is what lets a
    /// per-viewer pace exist at all: six clients watching the same match at six different points in
    /// the same story, all six boards showing the same truth.</para>
    /// </summary>
    public sealed class KineticConductor
    {
        private readonly List<KineticScene> pending = new List<KineticScene>();
        private KineticScene active;
        private float activeElapsed;
        private float holdElapsed;
        private int droppedScenes;

        /// <summary>
        /// The one scene this conductor paces that is NOT made of cues - milestone 8.35z12's attack
        /// spotlight, registered here as an <see cref="IConductedScene"/> so there is ONE conductor
        /// rather than two.
        ///
        /// <para><b>It is a settable property and not a constructor parameter on purpose.</b>
        /// <c>TheConductor_HoldsNothingAuthoritative</c> asserts by reflection that this type's
        /// constructor takes nothing, which is the guarantee that a conductor cannot be handed a
        /// session; the registration has to arrive afterwards to keep that guarantee true. An
        /// <see cref="IConductedScene"/> is a lifecycle, not a read model, so the assertion still holds
        /// with this field present.</para>
        ///
        /// <para>Leaving it null is a supported composition: the conductor then paces cue scenes alone,
        /// exactly as it did before the spotlight existed.</para>
        /// </summary>
        public IConductedScene ConductedScene { get; set; }

        /// <summary>Whether nothing is playing and nothing is waiting.</summary>
        public bool IsIdle => active == null && pending.Count == 0;

        /// <summary>The scene playing right now, or null.</summary>
        public KineticScene ActiveScene => active;

        /// <summary>How far into the active scene the conductor is.</summary>
        public float ActiveElapsedSeconds => activeElapsed;

        /// <summary>How many scenes are waiting behind the active one.</summary>
        public int PendingSceneCount => pending.Count;

        /// <summary>
        /// Whether the active scene has finished its motion and is waiting for the viewer to continue.
        /// </summary>
        public bool IsHoldingForAcknowledgement =>
            active != null && active.HoldsForAcknowledgement && activeElapsed >= active.DurationSeconds;

        /// <summary>
        /// Whether the queue is running compressed because this viewer fell behind. Exposed so a Play
        /// Mode case and a perf capture can tell a paced run from a catching-up one.
        /// </summary>
        public bool IsCatchingUp => pending.Count > KineticStaging.CatchUpQueueBudget;

        /// <summary>
        /// How many scenes have been dropped for queue length over this conductor's life. A healthy
        /// session ends at zero; a non-zero count is the signal that a viewer was left far behind.
        /// </summary>
        public int DroppedSceneCount => droppedScenes;

        /// <summary>
        /// Appends one projection change's scenes, sorted by <see cref="KineticScene.Order"/>.
        ///
        /// <para>The sort is WITHIN the batch and stable. Across batches order is arrival order and
        /// always will be, because a later projection is later in time - re-sorting the whole queue
        /// would let a low-ordered scene from a new round jump ahead of the round before it.</para>
        /// </summary>
        public void Enqueue(IReadOnlyList<KineticScene> scenes)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return;
            }

            var batch = new List<KineticScene>(scenes.Count);
            for (var index = 0; index < scenes.Count; index++)
            {
                if (scenes[index] != null)
                {
                    batch.Add(scenes[index]);
                }
            }

            if (batch.Count == 0)
            {
                return;
            }

            StableSortByOrder(batch);
            pending.AddRange(batch);
            TrimToBudget();
        }

        /// <summary>
        /// Advances the queue and reports the scene that STARTED on this call, or null.
        ///
        /// <para>A caller begins that scene's cues on its own clock when this returns one - the
        /// conductor paces, and the existing <see cref="KineticPlayback"/> still owns every curve.</para>
        /// </summary>
        public KineticScene Advance(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || deltaSeconds <= 0f)
            {
                return StartIfIdle();
            }

            // The same clamp KineticPlayback.Advance applies, and for the same reason: a scene load or
            // an Editor pause must not skip a whole scene between two frames.
            var delta = deltaSeconds > 0.25f ? 0.25f : deltaSeconds;
            if (IsCatchingUp)
            {
                delta *= KineticStaging.CatchUpRate;
            }

            if (active == null)
            {
                return StartIfIdle();
            }

            activeElapsed += delta;

            if (IsHoldingForAcknowledgement)
            {
                holdElapsed += delta;
                if (holdElapsed < HoldValveSeconds(active))
                {
                    return null;
                }

                // The viewer is not coming back. Release rather than freeze this client's story.
                return Release();
            }

            var stagger = IsCatchingUp ? 0f : active.StaggerAfterSeconds;
            if (activeElapsed < active.DurationSeconds + stagger)
            {
                return null;
            }

            return Release();
        }

        /// <summary>
        /// The viewer continued. Ends a holding scene at once; does nothing to a scene that is not
        /// holding, so a stray click can never skip a beat the player has not seen yet.
        /// </summary>
        public bool Acknowledge()
        {
            if (!IsHoldingForAcknowledgement)
            {
                return false;
            }

            Release();
            return true;
        }

        /// <summary>
        /// Drops everything, playing and waiting.
        ///
        /// <para>The presenter asks for this on exactly the two occasions milestone 8.35y named: the
        /// first sight of a match, and a log that went BACKWARDS. A queue is the one thing a rewind
        /// could turn into a story that never happened, so the queue is emptied rather than replayed,
        /// and the board is left showing authoritative state with nothing pending over it.</para>
        /// </summary>
        public void CancelAll()
        {
            pending.Clear();
            active = null;
            activeElapsed = 0f;
            holdElapsed = 0f;
        }

        /// <summary>
        /// How long <paramref name="scene"/> may hold before releasing itself. See
        /// <see cref="KineticStaging.ChromeHiddenHoldSeconds"/> for why a scene that hid the HUD gets a
        /// shorter valve than one that did not.
        /// </summary>
        private static float HoldValveSeconds(KineticScene scene) =>
            scene != null && scene.HidesChrome
                ? KineticStaging.ChromeHiddenHoldSeconds
                : KineticStaging.MaxHoldSeconds;

        private KineticScene StartIfIdle()
        {
            // Milestone 8.35z12. The registered conducted scene is offered the stage FIRST and holds it
            // exclusively while it has it. This is principle 3 - one thing at a time - applied to the
            // one scene that is not made of cues: a spotlight framing an attack must not open while a
            // reveal is flipping, and a reveal must not flip behind a hidden HUD.
            var conducted = ConductedScene;
            if (conducted != null)
            {
                if (conducted.IsRequestingStage)
                {
                    conducted.GrantStage();
                    return null;
                }

                // DefersOtherScenes and not HoldsStage, and the difference is the whole feature: the
                // scene holds the stage through its HELD phase too, and that phase is exactly where
                // the attack's own released cues must play, framed. See IConductedScene.
                if (conducted.DefersOtherScenes)
                {
                    return null;
                }
            }

            if (active != null || pending.Count == 0)
            {
                return null;
            }

            active = pending[0];
            pending.RemoveAt(0);
            activeElapsed = 0f;
            holdElapsed = 0f;
            return active;
        }

        private KineticScene Release()
        {
            active = null;
            activeElapsed = 0f;
            holdElapsed = 0f;
            return StartIfIdle();
        }

        private void TrimToBudget()
        {
            var excess = pending.Count - KineticStaging.MaxQueuedScenes;
            if (excess <= 0)
            {
                return;
            }

            pending.RemoveRange(0, excess);
            droppedScenes += excess;
        }

        /// <summary>
        /// An insertion sort, because it is stable and the batches are tiny (a projection change
        /// carries single digits of scenes). <c>List.Sort</c> is introsort and is NOT stable, which
        /// would let two heroes on the same initiative swap places between two clients.
        /// </summary>
        private static void StableSortByOrder(List<KineticScene> batch)
        {
            for (var index = 1; index < batch.Count; index++)
            {
                var item = batch[index];
                var scan = index - 1;
                while (scan >= 0 && batch[scan].Order > item.Order)
                {
                    batch[scan + 1] = batch[scan];
                    scan--;
                }

                batch[scan + 1] = item;
            }
        }
    }

    /// <summary>
    /// Milestone 8.35z11. The one check that makes a QUEUED slide safe to draw.
    ///
    /// <para>See <see cref="KineticConductor"/>'s own remarks for why this exists. It is a free
    /// function rather than a method on the view so the rule can be tested without a board.</para>
    /// </summary>
    public static class KineticRouteFreshness
    {
        /// <summary>
        /// Whether a slide's route still ends where the piece now authoritatively stands.
        ///
        /// <para>False means the piece moved again while this slide waited its turn, so the route
        /// describes a journey that is over. The view then places the piece authoritatively and skips
        /// the travel - the end state was always going to be the projection's, and this is what stops
        /// the middle from being fiction.</para>
        /// </summary>
        public static bool IsRouteStillCurrent(
            IReadOnlyList<GuardsDigital.Core.Board.HexCoordinate> route,
            GuardsDigital.Core.Board.HexCoordinate current)
        {
            if (route == null || route.Count < 2)
            {
                return false;
            }

            return route[route.Count - 1].Equals(current);
        }
    }
}
