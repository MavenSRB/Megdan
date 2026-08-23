using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.EffectResolution;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>Why an effect frame is standing outside an action rather than inside one.</summary>
    public enum PreActionContinuationKind
    {
        None = 0,

        /// <summary>
        /// `before-action-trigger@1`. A trusted rule source asked to run before an action of a named
        /// kind begins - Arien's Living Tsunami prints "Once per turn, before performing an Attack
        /// action, you may move 1 space". The action itself has not started; its offer is held exactly
        /// as it was validated and started when the frame finishes.
        /// </summary>
        BeforeActionTrigger = 1,

        /// <summary>
        /// `around-primary-action@1`. The action's own printed text is applied either side of it, and
        /// which side is the actor's decision - Xargatha's Charm moves an enemy minion "before or after"
        /// her own Movement. The frame runs first either way, because the decision is inside it.
        /// </summary>
        AroundPrimaryAction = 2
    }

    /// <summary>Which side of the held action this frame's remaining work belongs to.</summary>
    public enum PreActionPlacement
    {
        /// <summary>
        /// The frame has not answered yet. Only an <see cref="PreActionContinuationKind.AroundPrimaryAction"/>
        /// frame is ever in this state, and only until its ordering capability is answered.
        /// </summary>
        Undecided = 0,

        /// <summary>The frame finishes, and then the held action starts.</summary>
        BeforeTheAction = 1,

        /// <summary>The held action starts and completes, and then the frame finishes.</summary>
        AfterTheAction = 2
    }

    /// <summary>
    /// Milestone 8.19a. An effect frame that owns an action it is not inside: the outer pre-action
    /// continuation.
    ///
    /// <para><b>Why it exists at all.</b> Every pause this engine had before it stood <em>inside</em> an
    /// action - a Movement mid-route, an Attack awaiting its Defense, a Skill mid-node, card text inside
    /// rulebook p.14's order - so the action's own resolution was always the thing that held the
    /// continuation. Two of the roster's printed shapes ask a question with no action open to hold it.
    /// Arien's Ultimate asks before an Attack is performed at all; Xargatha's Charm asks which side of
    /// her Movement a rider runs on, which is a question about the Movement rather than a step in it. In
    /// both, what has to survive is the frame <em>and</em> the action the frame is standing outside of.</para>
    ///
    /// <para><b>What it carries.</b> The frame, the reason it is standing there, the side of the action
    /// its remaining work belongs to, and the exact validated offer plus resolution ID the held action
    /// resumes from. The offer is carried rather than re-derived because that is the whole point of the
    /// altitude: a trigger that moved the hero one space may have changed which offers the turn would
    /// now build, and p.10 gives the action the player selected rather than the one the board would
    /// re-offer afterwards. Nothing about the effect's semantics is here - the frame names an effect
    /// definition and a restore rebinds to the trusted catalog, exactly as every other continuation
    /// does.</para>
    ///
    /// <para>It is written to a snapshot beside the card-action continuation rather than as one of its
    /// kinds, because it genuinely coexists with one: an `AfterTheAction` frame is parked while the
    /// held Movement runs, and that Movement has a continuation of its own.</para>
    /// </summary>
    public sealed class PendingPreActionResolution
    {
        public PendingPreActionResolution(
            PendingResolution frame,
            PreActionContinuationKind kind,
            PreActionPlacement placement,
            ActionResolutionId heldResolutionId,
            ActionOffer heldOffer,
            bool heldActionHasStarted = false)
        {
            // The frame is absent in exactly one shape: the side has been decided and the work it
            // places has not been entered yet. Every other shape is a frame standing somewhere - paused
            // on its ordering question, or paused inside the work itself - and a continuation with
            // neither a frame nor a decided side would be describing nothing at all.
            if (frame == null && placement == PreActionPlacement.Undecided)
            {
                throw new ArgumentNullException(
                    nameof(frame),
                    "A pre-action continuation is either a frame standing outside an action or a side " +
                    "the actor has chosen for work not yet entered.");
            }

            if (kind == PreActionContinuationKind.None ||
                !Enum.IsDefined(typeof(PreActionContinuationKind), kind))
            {
                throw new ArgumentException(
                    "A pre-action continuation states why it is standing outside an action.",
                    nameof(kind));
            }

            if (!Enum.IsDefined(typeof(PreActionPlacement), placement))
            {
                throw new ArgumentException(
                    "A pre-action continuation states which side of the held action it belongs to.",
                    nameof(placement));
            }

            if (kind == PreActionContinuationKind.BeforeActionTrigger &&
                placement != PreActionPlacement.BeforeTheAction)
            {
                throw new ArgumentException(
                    "A trigger that fires before an action runs before it; only text applied around a " +
                    "primary action chooses a side.",
                    nameof(placement));
            }

            if (!heldResolutionId.IsDefined || !heldOffer.IsAvailable)
            {
                throw new ArgumentException(
                    "A pre-action continuation holds the exact validated offer its action resumes from.",
                    nameof(heldOffer));
            }

            if (heldActionHasStarted && placement != PreActionPlacement.AfterTheAction)
            {
                throw new ArgumentException(
                    "Only a frame whose remaining work belongs after the action is parked while that " +
                    "action runs.",
                    nameof(heldActionHasStarted));
            }

            Frame = frame;
            Kind = kind;
            Placement = placement;
            HeldResolutionId = heldResolutionId;
            HeldOffer = heldOffer;
            HeldActionHasStarted = heldActionHasStarted;
        }

        /// <summary>The paused effect frame, identical in kind to a paused Skill's.</summary>
        public PendingResolution Frame { get; }

        public PreActionContinuationKind Kind { get; }

        public PreActionPlacement Placement { get; }

        /// <summary>The identity the held action will resolve under, minted before the frame ran.</summary>
        public ActionResolutionId HeldResolutionId { get; }

        /// <summary>
        /// The offer the held action starts from, exactly as the turn validated it. Milestone 8.19a's
        /// acceptance check names this: a trigger resumes the offer and version it interrupted, not one
        /// re-derived from a board its own effect may have changed.
        /// </summary>
        public ActionOffer HeldOffer { get; }

        /// <summary>Whether the held action has already been started and this frame is parked behind it.</summary>
        public bool HeldActionHasStarted { get; }

        /// <summary>The same continuation carrying a frame that has taken one more step.</summary>
        public PendingPreActionResolution WithFrame(PendingResolution frame) =>
            new PendingPreActionResolution(
                frame, Kind, Placement, HeldResolutionId, HeldOffer, HeldActionHasStarted);

        /// <summary>
        /// The same continuation once its ordering capability has answered. Only an
        /// <see cref="PreActionContinuationKind.AroundPrimaryAction"/> frame is ever undecided, and it
        /// decides exactly once.
        /// </summary>
        public PendingPreActionResolution WithPlacement(PreActionPlacement placement)
        {
            if (Placement != PreActionPlacement.Undecided)
            {
                throw new InvalidOperationException(
                    "A pre-action continuation's side is decided once; deciding it again would move " +
                    "work the actor has already placed.");
            }

            return new PendingPreActionResolution(
                Frame, Kind, placement, HeldResolutionId, HeldOffer);
        }

        /// <summary>
        /// The same continuation, parked behind the action it has just started and holding no frame:
        /// the work the actor placed after the action has not been entered yet.
        /// </summary>
        public PendingPreActionResolution Parked()
        {
            if (Placement != PreActionPlacement.AfterTheAction)
            {
                throw new InvalidOperationException(
                    "Only work the actor placed after the action waits for that action to finish.");
            }

            return new PendingPreActionResolution(
                null, Kind, Placement, HeldResolutionId, HeldOffer, heldActionHasStarted: true);
        }

        /// <summary>
        /// Whether the work this continuation places still has to be entered. True on both sides: the
        /// difference between them is whether the held action has run in between, which
        /// <see cref="HeldActionHasStarted"/> says.
        /// </summary>
        public bool OwesWork => Frame == null;
    }
}
