using System;
using System.Collections.Generic;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z12. The spotlight's whole lifecycle, with no session, no camera and
    /// no Unity attached to it.
    ///
    /// <para>It is a separate type from <see cref="AttackSpotlightPresenter"/> for the reason
    /// <see cref="KineticCueStream"/> is separate from its presenter: this is where every rule that
    /// could get a viewer-paced scene WRONG lives - what arms the continue, what a second attack does
    /// to the one on screen, what an undo does to both - and a test of those rules should not have to
    /// stand up an <c>IMatchReadModel</c> to ask the question.</para>
    ///
    /// <para><b>The one rule everything else follows from:</b> holding the stage delays what this
    /// client DRAWS and nothing else. No method here submits, blocks, or waits on anything
    /// authoritative, which is why a spotlight can be held for six seconds on one screen and dismissed
    /// instantly on another while both remain projection-true.</para>
    /// </summary>
    public sealed class AttackSpotlightStage : IConductedScene
    {
        private readonly List<AttackSpotlightScene> queue = new List<AttackSpotlightScene>();
        private float elapsed;

        /// <summary>
        /// Whether this stage grants itself the stage the moment a scene is ready.
        ///
        /// <para><b>True is the shipping default and it is what makes <see cref="IConductedScene"/> a
        /// pure upgrade.</b> With no conductor composed, a spotlight plays on its own exactly as the
        /// amendment describes. When the motion language's conductor lands it sets this false and
        /// calls <see cref="GrantStage"/> in its own order, and not one line of the spotlight
        /// changes.</para>
        /// </summary>
        public bool AutoGrantsStage { get; set; } = true;

        /// <inheritdoc />
        public string Name => "attack-spotlight";

        public AttackSpotlightPhase Phase { get; private set; } = AttackSpotlightPhase.None;

        /// <summary>The scene on screen, or the one waiting to be granted. Null when idle.</summary>
        public AttackSpotlightScene Current { get; private set; }

        /// <summary>How many scenes are waiting behind <see cref="Current"/>.</summary>
        public int QueuedSceneCount => queue.Count;

        /// <summary>
        /// How many scenes this stage has dropped because the queue was full. It exists so a defect
        /// that quietly discards half a match's attacks is visible instead of silent.
        /// </summary>
        public int DroppedSceneCount { get; private set; }

        /// <summary>
        /// Why the last <see cref="Interrupt"/> fired, or <see cref="ConductedSceneInterruption.None"/>
        /// if the stage has never been interrupted or has played cleanly since.
        /// </summary>
        public ConductedSceneInterruption LastInterruption { get; private set; }

        /// <inheritdoc />
        public bool IsRequestingStage => Phase == AttackSpotlightPhase.Requested;

        /// <inheritdoc />
        public bool HoldsStage =>
            Phase == AttackSpotlightPhase.Entering ||
            Phase == AttackSpotlightPhase.Held ||
            Phase == AttackSpotlightPhase.Exiting;

        /// <summary>Whether the viewer may press CONTINUE right now.</summary>
        public bool IsContinueArmed => Phase == AttackSpotlightPhase.Held;

        /// <summary>
        /// How far the surface is into the spotlight: 0 is the ordinary board and HUD, 1 is the
        /// framed, tilted, chrome-free view. Eased, so a view interpolates rather than steps.
        /// </summary>
        public float FramingWeight
        {
            get
            {
                switch (Phase)
                {
                    case AttackSpotlightPhase.Entering:
                        return AttackSpotlightTiming.Ease(
                            AttackSpotlightTiming.EnterSeconds <= 0f
                                ? 1f
                                : elapsed / AttackSpotlightTiming.EnterSeconds);
                    case AttackSpotlightPhase.Held:
                        return 1f;
                    case AttackSpotlightPhase.Exiting:
                        return 1f - AttackSpotlightTiming.Ease(
                            AttackSpotlightTiming.ExitSeconds <= 0f
                                ? 1f
                                : elapsed / AttackSpotlightTiming.ExitSeconds);
                    default:
                        return 0f;
                }
            }
        }

        /// <summary>How long the current phase has been running, in seconds.</summary>
        public float ElapsedInPhase => elapsed;

        /// <summary>
        /// Offers a scene. It plays now if the stage is idle and self-conducting, and queues
        /// otherwise.
        ///
        /// <para>A scene whose sequence this stage has already shown or queued is ignored, so an
        /// observer that offers the same row twice cannot make the viewer watch it twice.</para>
        /// </summary>
        public bool Offer(AttackSpotlightScene scene)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            if (Current != null && Current.Sequence == scene.Sequence)
            {
                return false;
            }

            for (var index = 0; index < queue.Count; index++)
            {
                if (queue[index].Sequence == scene.Sequence)
                {
                    return false;
                }
            }

            if (Phase == AttackSpotlightPhase.None)
            {
                Current = scene;
                Enter(AttackSpotlightPhase.Requested);
                if (AutoGrantsStage)
                {
                    GrantStage();
                }

                return true;
            }

            queue.Add(scene);
            while (queue.Count > AttackSpotlightTiming.MaxQueuedScenes)
            {
                // The oldest goes. See AttackSpotlightTiming.MaxQueuedScenes for why the three most
                // recent attacks in order beat the three oldest.
                queue.RemoveAt(0);
                DroppedSceneCount++;
            }

            return true;
        }

        /// <inheritdoc />
        public void GrantStage()
        {
            if (Phase != AttackSpotlightPhase.Requested || Current == null)
            {
                return;
            }

            LastInterruption = ConductedSceneInterruption.None;
            Enter(AttackSpotlightPhase.Entering);
        }

        /// <summary>
        /// The viewer's press. It ends the hold and starts the camera home; it submits nothing and
        /// changes no authoritative state.
        /// </summary>
        public bool Continue()
        {
            if (Phase != AttackSpotlightPhase.Held)
            {
                return false;
            }

            Enter(AttackSpotlightPhase.Exiting);
            return true;
        }

        /// <inheritdoc />
        public void Advance(float deltaSeconds)
        {
            if (Phase == AttackSpotlightPhase.None)
            {
                return;
            }

            if (deltaSeconds > 0f)
            {
                // Clamped for the reason KineticPlayback.Advance clamps: one hitched frame must not
                // skip a 450ms framing past its middle, and a paused editor must not fire the hold's
                // safety valve the instant it resumes.
                elapsed += Math.Min(deltaSeconds, 0.25f);
            }

            switch (Phase)
            {
                case AttackSpotlightPhase.Requested:
                    // Waiting on a conductor that has not granted it yet. Nothing moves and nothing
                    // is hidden, so this state is invisible to a player.
                    return;

                case AttackSpotlightPhase.Entering:
                    if (elapsed >= AttackSpotlightTiming.EnterSeconds)
                    {
                        Enter(AttackSpotlightPhase.Held);
                    }

                    return;

                case AttackSpotlightPhase.Held:
                    if (elapsed >= AttackSpotlightTiming.MaximumHeldSeconds)
                    {
                        Enter(AttackSpotlightPhase.Exiting);
                    }

                    return;

                case AttackSpotlightPhase.Exiting:
                    if (elapsed >= AttackSpotlightTiming.ExitSeconds)
                    {
                        Release();
                    }

                    return;
            }
        }

        /// <inheritdoc />
        public void Interrupt(ConductedSceneInterruption reason)
        {
            if (Phase == AttackSpotlightPhase.None && queue.Count == 0)
            {
                return;
            }

            // No easing out. An interrupted spotlight is one whose subject may no longer be true -
            // an undone attack, a lost projection - so the surface goes back to authoritative state
            // on the very next frame rather than spending 350ms describing something that did not
            // happen. The queue goes with it for the same reason.
            queue.Clear();
            Current = null;
            Phase = AttackSpotlightPhase.None;
            elapsed = 0f;
            LastInterruption = reason;
        }

        /// <summary>
        /// Ends the current scene cleanly and starts the next queued one, if any. Called when the
        /// exit finishes.
        /// </summary>
        private void Release()
        {
            Current = null;
            Phase = AttackSpotlightPhase.None;
            elapsed = 0f;
            if (queue.Count == 0)
            {
                return;
            }

            Current = queue[0];
            queue.RemoveAt(0);
            Enter(AttackSpotlightPhase.Requested);
            if (AutoGrantsStage)
            {
                GrantStage();
            }
        }

        private void Enter(AttackSpotlightPhase phase)
        {
            Phase = phase;
            elapsed = 0f;
        }
    }
}
