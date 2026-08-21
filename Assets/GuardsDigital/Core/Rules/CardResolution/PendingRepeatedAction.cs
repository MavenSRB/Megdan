using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 6.7: an action instance that has been asked for and cannot start yet.
    ///
    /// It exists for exactly one situation. A completed instance requested a repeat, and the
    /// after-action displaced-minion correction between the two instances paused on a team choice, so
    /// the second instance is owed but the boundary before it is not clean. A correction that needs no
    /// choice never produces one of these: the second instance starts inside the same command, and
    /// there is no moment to hold.
    ///
    /// It is authority-owned serializable data with no handler, delegate, or callback in it, like every
    /// other continuation in this engine. What it carries is identity alone - which card, which action,
    /// who acts, which effect definition - because the semantics come from the trusted card definitions
    /// when the instance starts, never from this record.
    /// </summary>
    public sealed class PendingRepeatedAction
    {
        public PendingRepeatedAction(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            EffectDefinitionReference effectDefinitionReference,
            HeroId actorId,
            HeroId controllerId,
            RepeatContinuation continuation)
        {
            if (!continuation.IsRepeatedInstance)
            {
                throw new ArgumentException(
                    "A pending repeated action is at least the first repeat of its chain.",
                    nameof(continuation));
            }

            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException(
                    "A pending repeated action requires the resolution ID its instance will run under.",
                    nameof(resolutionId));
            }

            if (!sourceCardInstanceId.IsDefined || !sourceDefinitionReference.IsDefined ||
                !actionDefinitionId.IsDefined)
            {
                throw new ArgumentException(
                    "A pending repeated action requires its source card, definition, and action.");
            }

            if (!effectDefinitionReference.IsDefined)
            {
                throw new ArgumentException(
                    "A pending repeated action requires the effect definition reference it repeats.",
                    nameof(effectDefinitionReference));
            }

            if (!actorId.IsDefined || !controllerId.IsDefined)
            {
                throw new ArgumentException(
                    "A pending repeated action requires an actor and a controller.");
            }

            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionReference = sourceDefinitionReference;
            ActionDefinitionId = actionDefinitionId;
            EffectDefinitionReference = effectDefinitionReference;
            ActorId = actorId;
            ControllerId = controllerId;
            Continuation = continuation;
        }

        /// <summary>
        /// The identity the repeated instance runs under. Minted from the completing instance's own
        /// resolution ID by <see cref="Mint"/>, so it is a pure function of the accepted-command prefix
        /// and reproduces under snapshot restore, replay, and retained-prefix Undo.
        /// </summary>
        public ActionResolutionId ResolutionId { get; }

        public CardInstanceId SourceCardInstanceId { get; }
        public CardDefinitionReference SourceDefinitionReference { get; }
        public ActionDefinitionId ActionDefinitionId { get; }
        public EffectDefinitionReference EffectDefinitionReference { get; }
        public HeroId ActorId { get; }
        public HeroId ControllerId { get; }

        /// <summary>
        /// Milestone 8.6: which repeat of its chain this instance is, and the target it inherits when
        /// its card said `on the same target`. It is the one thing here that is not identity, and it is
        /// still not semantics: the binding it belongs to is read from the trusted definition when the
        /// instance reaches the node.
        /// </summary>
        public RepeatContinuation Continuation { get; }

        /// <summary>
        /// The repeat a completed instance asked for, named from that instance's own frame.
        ///
        /// The suffix is what keeps the instances distinguishable in the fact stream and in any pending
        /// choice any of them opens. It is applied per repeat rather than counted, so the second repeat
        /// of a chain is named from the first repeat's own id and stays unique without this record
        /// having to spell the ordinal into a string - the ordinal it does carry is the
        /// <see cref="Continuation"/>, which is a rules fact rather than a name.
        /// </summary>
        public static PendingRepeatedAction Mint(
            ActionResolutionId completedResolutionId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            EffectDefinitionReference effectDefinitionReference,
            HeroId actorId,
            HeroId controllerId,
            RepeatContinuation continuation) =>
            new PendingRepeatedAction(
                new ActionResolutionId($"{completedResolutionId.Value}-repeat"),
                sourceCardInstanceId,
                sourceDefinitionReference,
                actionDefinitionId,
                effectDefinitionReference,
                actorId,
                controllerId,
                continuation);
    }
}
