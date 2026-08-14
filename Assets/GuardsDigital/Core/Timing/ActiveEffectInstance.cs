using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Timing
{
    public enum ActiveEffectPhase
    {
        Dormant = 0,
        Active = 1,
        Ended = 2
    }

    public sealed class ActiveEffectInstance
    {
        public ActiveEffectInstance(
            ActiveEffectId id,
            ActiveEffectDefinition definition,
            HeroId controllerHeroId,
            TeamSide controllerTeamSide,
            HeroId affectedHeroId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId sourceActionDefinitionId,
            int createdRoundIndex,
            int createdTurnIndex,
            long creationSequence,
            ActiveEffectPhase phase = ActiveEffectPhase.Dormant,
            int? activatedRoundIndex = null,
            int? activatedTurnIndex = null,
            TimingCheckpoint? endedAt = null)
        {
            if (!id.IsDefined) throw new ArgumentException("An Active-effect instance requires an ID.", nameof(id));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (!controllerHeroId.IsDefined)
                throw new ArgumentException("An Active effect requires a controller hero.", nameof(controllerHeroId));
            if (controllerTeamSide == TeamSide.None)
                throw new ArgumentException("An Active effect requires a controller team.", nameof(controllerTeamSide));
            if (definition.AffectedScope == ActiveEffectAffectedScopeKind.Hero && !affectedHeroId.IsDefined)
                throw new ArgumentException("A hero-scoped Active effect requires the affected hero.", nameof(affectedHeroId));
            if (!sourceCardInstanceId.IsDefined || !sourceActionDefinitionId.IsDefined)
                throw new ArgumentException("An Active effect requires source card and action identities.");
            if (createdRoundIndex < 1 || createdTurnIndex < 1 || creationSequence < 0)
                throw new ArgumentException("An Active effect requires valid creation anchors and order.");
            if (phase == ActiveEffectPhase.Dormant && (activatedRoundIndex.HasValue || activatedTurnIndex.HasValue || endedAt.HasValue))
                throw new ArgumentException("A dormant effect cannot have activation or ending facts.");
            if (phase == ActiveEffectPhase.Active && (!activatedRoundIndex.HasValue || !activatedTurnIndex.HasValue || endedAt.HasValue))
                throw new ArgumentException("An active effect requires activation anchors and no ending checkpoint.");
            if (phase == ActiveEffectPhase.Ended && !endedAt.HasValue)
                throw new ArgumentException("An ended effect requires its exact ending checkpoint.");

            Id = id;
            ControllerHeroId = controllerHeroId;
            ControllerTeamSide = controllerTeamSide;
            AffectedHeroId = affectedHeroId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceActionDefinitionId = sourceActionDefinitionId;
            CreatedRoundIndex = createdRoundIndex;
            CreatedTurnIndex = createdTurnIndex;
            CreationSequence = creationSequence;
            Phase = phase;
            ActivatedRoundIndex = activatedRoundIndex;
            ActivatedTurnIndex = activatedTurnIndex;
            EndedAt = endedAt;
        }

        public ActiveEffectId Id { get; }
        public ActiveEffectDefinition Definition { get; }

        /// <summary>
        /// Runtime binding, not definition semantics. Milestone 6.2a moved these off
        /// <see cref="ActiveEffectDefinition"/>: who controls this occurrence and whom it affects are
        /// facts about this instance, while the definition describes what the effect is. The split is
        /// what lets a restored snapshot supply these from the payload while taking every semantic
        /// field from the trusted catalog.
        /// </summary>
        public HeroId ControllerHeroId { get; }

        public TeamSide ControllerTeamSide { get; }

        public HeroId AffectedHeroId { get; }

        public CardInstanceId SourceCardInstanceId { get; }
        public ActionDefinitionId SourceActionDefinitionId { get; }
        public int CreatedRoundIndex { get; }
        public int CreatedTurnIndex { get; }
        public long CreationSequence { get; }
        public ActiveEffectPhase Phase { get; private set; }
        public int? ActivatedRoundIndex { get; private set; }
        public int? ActivatedTurnIndex { get; private set; }
        public TimingCheckpoint? EndedAt { get; private set; }

        internal void Activate(int roundIndex, int turnIndex)
        {
            if (Phase != ActiveEffectPhase.Dormant) return;
            Phase = ActiveEffectPhase.Active;
            ActivatedRoundIndex = roundIndex;
            ActivatedTurnIndex = turnIndex;
        }

        internal void End(TimingCheckpoint checkpoint)
        {
            if (Phase == ActiveEffectPhase.Ended) return;
            Phase = ActiveEffectPhase.Ended;
            EndedAt = checkpoint;
        }
    }
}
