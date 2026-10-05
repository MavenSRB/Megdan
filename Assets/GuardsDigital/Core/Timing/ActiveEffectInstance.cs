using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
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
            TimingCheckpoint? endedAt = null,
            HeroId boundActorHeroId = default,
            BoardPartitionLine boardPartitionLine = default)
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

            // Milestone 8.20. A definition that points at nobody may not be handed somebody, and one
            // that does may still find nobody there - Expert Duelist discarded to defend an attack
            // that never happened is not a state this build can reach, but a payload claiming it is.
            // The first is refused because it is a caller mistake; the second is admitted because it
            // is the honest reading of an occurrence whose "this" found no referent, and
            // RestrictedActorScope.ExceptsTheBoundActor then excepts nobody.
            if (boundActorHeroId.IsDefined &&
                definition.BoundActorSource == ActiveEffectBoundActorSource.None)
            {
                throw new ArgumentException(
                    "An Active effect whose definition binds no actor cannot be given one.",
                    nameof(boundActorHeroId));
            }
            if (boardPartitionLine.IsDefined !=
                (definition.BoardPartitionMode != BoardPartitionMode.None))
                throw new ArgumentException(
                    "A line binding is required exactly when its definition declares a board partition.",
                    nameof(boardPartitionLine));

            Id = id;
            BoundActorHeroId = boundActorHeroId;
            BoardPartitionLine = boardPartitionLine;
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

        /// <summary>
        /// Milestone 8.20. The one hero this occurrence's declarations point at with "this" - Arien's
        /// Expert and Master Duelist, "except this attacker" - or the undefined hero where the
        /// definition points at nobody.
        ///
        /// <para>It is a third runtime binding beside the controller and the affected hero, and a
        /// third for the same reason: which hero the word "this" turned out to name is a fact about
        /// this occurrence, while <see cref="ActiveEffectDefinition.BoundActorSource"/> - where to look
        /// - is what the effect is. Two Master Duelists discarded against two different attackers in
        /// one round are one definition and two bindings.</para>
        ///
        /// <para>It is persisted, which is what made snapshot schema v28 necessary: the exception
        /// cannot be re-derived after the Attack it was taken inside has ended, and an effect restored
        /// without it would silently become immunity against the attacker too - a stronger card than
        /// the one printed.</para>
        /// </summary>
        public HeroId BoundActorHeroId { get; }
        public BoardPartitionLine BoardPartitionLine { get; }

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
