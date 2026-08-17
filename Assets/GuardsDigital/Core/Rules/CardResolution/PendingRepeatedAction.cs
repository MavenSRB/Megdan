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
            HeroId controllerId)
        {
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
        /// The repeat a completed instance asked for, named from that instance's own frame.
        ///
        /// The suffix is what keeps the two instances distinguishable in the fact stream and in any
        /// pending choice either one opens; because the repeated instance's own Repeat node is
        /// ineligible, there is never a third and so never a counter to advance.
        /// </summary>
        public static PendingRepeatedAction Mint(
            ActionResolutionId completedResolutionId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            EffectDefinitionReference effectDefinitionReference,
            HeroId actorId,
            HeroId controllerId) =>
            new PendingRepeatedAction(
                new ActionResolutionId($"{completedResolutionId.Value}-repeat"),
                sourceCardInstanceId,
                sourceDefinitionReference,
                actionDefinitionId,
                effectDefinitionReference,
                actorId,
                controllerId);
    }
}
