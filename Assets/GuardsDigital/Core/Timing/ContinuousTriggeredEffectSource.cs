using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Timing
{
    /// <summary>
    /// One currently applicable continuous source bound to the triggered effect its trusted definition
    /// declares. Like an unlocked Ultimate, this is derived from card state and definition content; it
    /// is not snapshot state of its own.
    /// </summary>
    public sealed class ContinuousTriggeredEffectSource
    {
        public ContinuousTriggeredEffectSource(
            string sourceId,
            HeroId controllerHeroId,
            TeamSide controllerTeamSide,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId sourceActionDefinitionId,
            ActionKind enclosingActionKind,
            EffectDefinitionReference triggeredEffect,
            IEnumerable<TimingCheckpoint> triggerCheckpoints,
            long orderingSequence = 0,
            PerformedBasicAction triggeringBasicAction = default,
            PerformedAttackAction triggeringAttackAction = default,
            PerformedPrimaryAction triggeringPrimaryAction = default)
        {
            if (string.IsNullOrWhiteSpace(sourceId)) throw new ArgumentException("A continuous trigger requires stable identity.", nameof(sourceId));
            if (!controllerHeroId.IsDefined || !sourceCardInstanceId.IsDefined ||
                !sourceDefinitionReference.IsDefined || !sourceActionDefinitionId.IsDefined ||
                !triggeredEffect.IsDefined)
                throw new ArgumentException("A continuous trigger requires typed controller, source, and effect identity.");
            if (!Enum.IsDefined(typeof(TeamSide), controllerTeamSide) || controllerTeamSide == TeamSide.None)
                throw new ArgumentOutOfRangeException(nameof(controllerTeamSide));
            if (!Enum.IsDefined(typeof(ActionKind), enclosingActionKind) || enclosingActionKind == ActionKind.None)
                throw new ArgumentOutOfRangeException(nameof(enclosingActionKind));
            var checkpoints = (triggerCheckpoints ?? throw new ArgumentNullException(nameof(triggerCheckpoints))).ToArray();
            if (checkpoints.Length == 0 || checkpoints.Contains(TimingCheckpoint.None) ||
                checkpoints.Distinct().Count() != checkpoints.Length)
                throw new ArgumentException("A continuous trigger requires defined, unique checkpoints.", nameof(triggerCheckpoints));
            if (checkpoints.Any(value => value != TimingCheckpoint.ActionEnded))
                throw new ArgumentException(
                    "Milestone 8.12 implements only the Action Ended continuous trigger window.",
                    nameof(triggerCheckpoints));

            SourceId = sourceId;
            ControllerHeroId = controllerHeroId;
            ControllerTeamSide = controllerTeamSide;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionReference = sourceDefinitionReference;
            SourceActionDefinitionId = sourceActionDefinitionId;
            EnclosingActionKind = enclosingActionKind;
            TriggeredEffect = triggeredEffect;
            TriggerCheckpoints = new ReadOnlyCollection<TimingCheckpoint>(checkpoints);
            OrderingSequence = orderingSequence;
            TriggeringBasicAction = triggeringBasicAction;
            TriggeringAttackAction = triggeringAttackAction;
            TriggeringPrimaryAction = triggeringPrimaryAction;
        }

        public string SourceId { get; }
        public HeroId ControllerHeroId { get; }
        public TeamSide ControllerTeamSide { get; }
        public CardInstanceId SourceCardInstanceId { get; }
        public CardDefinitionReference SourceDefinitionReference { get; }
        public ActionDefinitionId SourceActionDefinitionId { get; }
        public ActionKind EnclosingActionKind { get; }
        public EffectDefinitionReference TriggeredEffect { get; }
        public IReadOnlyList<TimingCheckpoint> TriggerCheckpoints { get; }
        public long OrderingSequence { get; }

        /// <summary>
        /// The completed basic action this occurrence fired for, or
        /// <see cref="PerformedBasicAction.None"/> for a source that is not derived from one.
        /// Milestone 8.23: Tigerclaw's Cloak and Daggers offers to repeat what just ended, so its
        /// occurrence has to name what that was - the fact is produced by the checkpoint and is gone
        /// by the time the effect runs.
        /// </summary>
        public PerformedBasicAction TriggeringBasicAction { get; }

        /// <summary>
        /// The completed ATTACK action this occurrence fired for, or
        /// <see cref="PerformedAttackAction.None"/> for a source that is not derived from one.
        /// Milestone 8.61h.
        ///
        /// <para>It travels beside <see cref="TriggeringBasicAction"/> rather than replacing it
        /// because the two answer different sentences and most sources carry exactly one of them:
        /// Tigerclaw's Cloak and Daggers fires on p.9's basic supertype and Min's Flurry of Blows on
        /// p.13's action kind. A BASIC Attack defines both, and both name the same card, action and
        /// hero - so whichever an occurrence reads, the subject it offers to repeat is the same
        /// one.</para>
        /// </summary>
        public PerformedAttackAction TriggeringAttackAction { get; }

        /// <summary>
        /// The completed PRIMARY action this occurrence fired for, or
        /// <see cref="PerformedPrimaryAction.None"/> for a source that is not derived from one.
        /// Milestone 8.75a.
        ///
        /// <para>It travels beside the two facts above for the reason
        /// <see cref="TriggeringAttackAction"/> travels beside the first: the three answer different
        /// sentences, and an occurrence carries whichever ones its completion defined. Ignatia the
        /// Mad's Chaos Incarnate fires on "a primary action", which every card in her deck can
        /// answer and neither of the other two facts can see off a coloured Skill - the condition
        /// <see cref="GuardsDigital.Core.Cards.UltimateTriggerCondition.AfterControllerPerformsPrimaryAction"/>
        /// has read this fact since milestone 8.39f1, but the occurrence it derived carried no record
        /// of it, so an ability firing on it had nothing it could offer to repeat.</para>
        /// </summary>
        public PerformedPrimaryAction TriggeringPrimaryAction { get; }
    }
}
