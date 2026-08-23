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
            long orderingSequence = 0)
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
    }
}
