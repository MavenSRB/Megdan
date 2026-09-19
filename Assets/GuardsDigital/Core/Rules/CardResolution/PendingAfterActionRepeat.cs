using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 8.23: the completed basic action a permanent after-action ability may repeat, held
    /// from the moment that action ends until the ability has been answered.
    ///
    /// <para><b>Why it is held rather than re-derived.</b> Rulebook p.16's Ultimate is asked at p.8's
    /// <c>ActionEnded</c> checkpoint, and Tigerclaw's Cloak and Daggers - "After you perform a basic
    /// action you may repeat it once" - opens a decision there. A decision is a pause, and by the time
    /// it is answered the action it is about has finished: its resolution is cleared, its card may have
    /// left the turn area, and <see cref="PerformedBasicAction"/> is explicitly a fact produced by the
    /// checkpoint rather than state anything keeps. So the subject has to survive the pause, and this
    /// is what survives it.</para>
    ///
    /// <para><b>Why it is not <see cref="PendingRepeatedAction"/>.</b> That type requires the effect
    /// definition the repeated instance re-runs, because milestone 6.7's repeat is always a Skill whose
    /// card text asked for it. This one repeats whatever p.9 counted as basic - an Attack, a Skill, a
    /// Movement, or one of p.11's, p.12's and p.13's general-rule actions - and most of those have no
    /// effect definition at all. What it carries instead is the kind, which is what decides who starts
    /// the second instance.</para>
    ///
    /// <para>Like every other continuation here it carries identity alone. The action's meaning comes
    /// from the trusted card definitions when the second instance starts, so a tampered payload can
    /// name a different card but cannot move a rule - and a card whose definition this build does not
    /// supply produces no repeat rather than a reconstructed one.</para>
    /// </summary>
    public sealed class PendingAfterActionRepeat
    {
        public PendingAfterActionRepeat(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            ActionKind kind,
            AttackTarget completedAttackTarget = default)
        {
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException(
                    "An owed after-action repeat requires the resolution ID its instance will run under.",
                    nameof(resolutionId));
            }

            if (!sourceCardInstanceId.IsDefined || !actionDefinitionId.IsDefined || !actorId.IsDefined)
            {
                throw new ArgumentException(
                    "An owed after-action repeat names the card, the action and the hero it repeats.",
                    nameof(sourceCardInstanceId));
            }

            if (kind == ActionKind.None || !Enum.IsDefined(typeof(ActionKind), kind))
            {
                throw new ArgumentException(
                    "An owed after-action repeat names the kind of action it repeats; p.9's supertype " +
                    "makes every action on a basic card basic, and which one it was decides who runs " +
                    "the second instance.",
                    nameof(kind));
            }

            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActionDefinitionId = actionDefinitionId;
            ActorId = actorId;
            Kind = kind;
            CompletedAttackTarget = completedAttackTarget;
        }

        /// <summary>
        /// The identity the repeated instance runs under, minted from the completing instance's own so
        /// it is a pure function of the accepted-command prefix rather than of when it was created.
        /// </summary>
        public ActionResolutionId ResolutionId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        public HeroId ActorId { get; }

        /// <summary>Which action is repeated, which is what decides which resolver starts it.</summary>
        public ActionKind Kind { get; }

        /// <summary>
        /// The unit the completed Attack took, and <c>default</c> for every other kind. It is captured
        /// here because a printed exclusion - "you cannot target the same unit" - is answered after the
        /// resolution that knew the target has been cleared.
        /// </summary>
        public AttackTarget CompletedAttackTarget { get; }

        /// <summary>
        /// The owed repeat for one completed basic action, or <c>null</c> when the fact names none.
        ///
        /// <para>Rulebook p.10's "You cannot repeat a repeated action!" is enforced here rather than at
        /// the decision, because it is a property of what ended rather than of who is asking: a
        /// repeated instance produces nothing to hold, so the ability's node finds nothing to offer and
        /// skips in silence.</para>
        /// </summary>
        public static PendingAfterActionRepeat For(
            PerformedBasicAction basicAction,
            ActionResolutionId resolutionId,
            AttackTarget completedAttackTarget = default) =>
            basicAction.IsDefined && !basicAction.WasRepeatedInstance && resolutionId.IsDefined
                ? new PendingAfterActionRepeat(
                    resolutionId,
                    basicAction.SourceCardInstanceId,
                    basicAction.ActionDefinitionId,
                    basicAction.ActorId,
                    basicAction.Kind,
                    completedAttackTarget)
                : null;

        /// <summary>
        /// The owed repeat for one completed ATTACK action, or <c>null</c> when the fact names none.
        /// Milestone 8.61h.
        ///
        /// <para><b>A second reading of the same page, for the actions the first one cannot see.</b>
        /// p.9's basic supertype is a property of the CARD, so the fact above is absent for every
        /// Attack performed off a coloured card - which is five of Min the Dragonmonk's six Attack
        /// faces, and what Flurry of Blows is mostly played off. The kind this records is always
        /// <see cref="ActionKind.Attack"/>, which is one of the four
        /// <see cref="PerformedBasicAction.KindCanBeRepeated"/> admits, so the subject it builds is
        /// indistinguishable from the one a basic Attack builds and the same resolver starts the
        /// second instance.</para>
        ///
        /// <para>p.10's "You cannot repeat a repeated action!" is enforced here exactly as it is
        /// above, and for the same reason: it is a property of what ended rather than of who is
        /// asking, so a repeated instance produces nothing to hold and the ability's node finds
        /// nothing to offer.</para>
        /// </summary>
        public static PendingAfterActionRepeat For(
            PerformedAttackAction attackAction,
            ActionResolutionId resolutionId,
            AttackTarget completedAttackTarget = default) =>
            attackAction.IsDefined && !attackAction.WasRepeatedInstance && resolutionId.IsDefined
                ? new PendingAfterActionRepeat(
                    resolutionId,
                    attackAction.SourceCardInstanceId,
                    attackAction.ActionDefinitionId,
                    attackAction.ActorId,
                    ActionKind.Attack,
                    completedAttackTarget)
                : null;

        public override string ToString() =>
            $"repeat of {Kind} on {SourceCardInstanceId.Value} by {ActorId.Value}";
    }
}
