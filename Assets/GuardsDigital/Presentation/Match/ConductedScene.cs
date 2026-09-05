namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z12. Why a conducted scene was interrupted, so a scene can tell a
    /// clean release apart from a snap.
    ///
    /// <para>Every member below means "stop now and leave the surface showing authoritative state".
    /// None of them means "unwind" - a scene that has to animate its way back out of an interruption
    /// is a scene that can disagree with the projection for the length of that animation, which is the
    /// one thing the derive-from-projection discipline does not allow.</para>
    /// </summary>
    public enum ConductedSceneInterruption
    {
        None = 0,

        /// <summary>The match log went backwards: an undo, a rewind, a snapshot restore.</summary>
        ProjectionRewound = 1,

        /// <summary>The client lost its projection - a disconnect, a seat change, a teardown.</summary>
        Disconnected = 2,

        /// <summary>The conductor is taking the stage back for something more important.</summary>
        Superseded = 3,

        /// <summary>The surface itself is going away.</summary>
        Shutdown = 4
    }

    /// <summary>
    /// Milestone 8.35z12. A unit of presentation a CONDUCTOR can schedule.
    ///
    /// <para><b>This interface is the whole contract between the attack spotlight and the motion
    /// language's conductor</b> (the TWENTIETH AMENDMENT's principle 3, "one thing at a time"). It is
    /// deliberately six members: a conductor needs to know that something wants the stage, to grant
    /// it, to drive its clock, to see that it is still holding, and to be able to cut it short. It
    /// needs nothing about cameras, cards, panels or easing, and it is given none of them.</para>
    ///
    /// <para><b>A scene with no conductor still works.</b> An implementation is required to grant
    /// itself the stage when nothing else is conducting - see
    /// <see cref="AttackSpotlightStage.AutoGrantsStage"/> - so this interface is a pure upgrade path
    /// rather than a dependency. The spotlight therefore ships and plays correctly on its own, and
    /// the conductor, when it lands, takes ownership by flipping one flag and calling
    /// <see cref="GrantStage"/> in its own order.</para>
    ///
    /// <para><b>Pacing is per viewer and never authoritative.</b> Holding the stage delays what this
    /// client DRAWS. It never delays the authority, never delays a submission, and never delays a
    /// prompt reaching the player - the NINETEENTH AMENDMENT's own rule. A conductor may therefore
    /// let a scene hold for as long as that scene asks, without any consequence for the match.</para>
    /// </summary>
    public interface IConductedScene
    {
        /// <summary>A stable name for the conductor's own diagnostics. Never shown to a player.</summary>
        string Name { get; }

        /// <summary>
        /// True while this scene has something to show and is waiting for its turn. A conductor polls
        /// it, decides the order, and calls <see cref="GrantStage"/> on the one that should play.
        /// </summary>
        bool IsRequestingStage { get; }

        /// <summary>
        /// True from the moment the stage is granted until the scene has completely released it.
        /// </summary>
        bool HoldsStage { get; }

        /// <summary>
        /// Whether a conductor must start NOTHING ELSE right now - the seventh member, added by the
        /// wave-one integration because <see cref="HoldsStage"/> turned out to be the wrong question
        /// for it, and the first complete Play Mode run is what showed the difference.
        ///
        /// <para><b>These are not the same predicate, and the gap between them is the feature.</b>
        /// A spotlight holds the stage while it is ENTERING, HELD and EXITING. But the whole point of
        /// the held phase is that the attack's own cues are RELEASED into it - the user's sequence is
        /// "lose the UI, zoom in, show the cards, show the smack", and the smack is those cues playing
        /// framed, with both faces up. A conductor that refused to start anything while the scene
        /// merely held the stage would keep them queued until the spotlight had CLOSED, which moves
        /// the smack out of the shot it exists for.</para>
        ///
        /// <para>So this is true while the scene is TRAVELLING - asking for the stage, moving onto it,
        /// or moving off - and false while it merely holds. It is the same phase set the cue layer's
        /// own deferral uses, published here so the two cannot drift apart: the conductor and the
        /// deferral now answer one question, rather than two that happen to agree.</para>
        /// </summary>
        bool DefersOtherScenes { get; }

        /// <summary>
        /// The conductor's go-ahead. Idempotent: granting a scene that already holds the stage, or
        /// one that is not requesting it, does nothing.
        /// </summary>
        void GrantStage();

        /// <summary>One frame of this scene's own clock.</summary>
        void Advance(float deltaSeconds);

        /// <summary>
        /// Stop now and release the stage, leaving the surface showing authoritative state. Never
        /// eases, never unwinds, and is safe to call on a scene that is doing nothing.
        /// </summary>
        void Interrupt(ConductedSceneInterruption reason);
    }
}
