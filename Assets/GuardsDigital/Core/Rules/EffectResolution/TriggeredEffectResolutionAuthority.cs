using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.EffectResolution
{
    /// <summary>
    /// Executes the effect reference carried by an Active-effect definition when the lifecycle emits
    /// its ordered Triggered visit. It deliberately delegates node execution to
    /// <see cref="EffectResolutionSession"/>: a trigger is another entry into the ordinary typed frame,
    /// not a second effect language.
    /// </summary>
    public sealed class TriggeredEffectResolutionAuthority
    {
        private readonly TriggeredEffectCatalog effects;
        private readonly EffectCapabilityCatalog capabilities;
        private readonly MatchComponents components;
        private EffectRosterState rosterState = EffectRosterState.Absent;

        public TriggeredEffectResolutionAuthority(
            TriggeredEffectCatalog effects,
            EffectCapabilityCatalog capabilities,
            MatchComponents components = null,
            PendingResolution pendingResolution = null)
        {
            this.effects = effects ?? throw new ArgumentNullException(nameof(effects));
            this.capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
            this.components = components;
            PendingResolution = pendingResolution;
            var rejections = effects.Definitions
                .SelectMany(effect => EffectDefinitionValidator.Validate(effect, capabilities))
                .ToArray();
            if (rejections.Length > 0)
                throw new ArgumentException(
                    "A triggered-effect catalog contains an invalid definition: " + rejections[0],
                    nameof(effects));
        }

        public PendingResolution PendingResolution { get; private set; }

        public PendingResolution LastCompletedResolution { get; private set; }

        internal void BindRosterState(EffectRosterState state)
        {
            rosterState = state ?? throw new ArgumentNullException(nameof(state));
        }

        internal void RestorePending(PendingResolution pending)
        {
            if (PendingResolution != null) throw new InvalidOperationException("A triggered continuation is already present.");
            PendingResolution = pending ?? throw new ArgumentNullException(nameof(pending));
        }

        public IReadOnlyList<ActiveEffectLifecycleEvent> ApplyCheckpoint(
            ActiveEffectLifecycle timing,
            ActiveEffectCheckpointContext checkpoint,
            TieBreakerState tieBreaker,
            IEnumerable<CardDefinition> cardDefinitions,
            IEnumerable<CardInstance> cardInstances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null,
            IEnumerable<ContinuousTriggeredEffectSource> continuousSources = null)
        {
            if (PendingResolution != null)
                throw new InvalidOperationException("A paused triggered effect must be answered before another checkpoint is visited.");

            var definitions = (cardDefinitions ?? throw new ArgumentNullException(nameof(cardDefinitions))).ToArray();
            var instances = (cardInstances ?? throw new ArgumentNullException(nameof(cardInstances))).ToArray();
            var events = timing.ApplyCheckpoint(checkpoint, tieBreaker);
            var active = events.Where(value => value.Kind == ActiveEffectLifecycleEventKind.Triggered)
                .Select(triggered =>
                {
                    var source = timing.Effects.Single(effect => effect.Id.Equals(triggered.EffectId));
                    if (!source.Definition.TriggeredEffect.HasValue)
                        return null;

                    var sourceCard = instances.Single(card => card.Id.Equals(source.SourceCardInstanceId));
                    var sourceDefinition = definitions.Single(definition =>
                        definition.Reference.Equals(sourceCard.DefinitionReference));
                    var sourceAction = sourceDefinition.ActionDefinitions.Single(action =>
                        action.Id.Equals(source.SourceActionDefinitionId));
                    return TriggerCandidate.FromActive(source, sourceDefinition.Reference, sourceAction.Kind);
                })
                .Where(value => value != null);
            var continuous = (continuousSources ?? Enumerable.Empty<ContinuousTriggeredEffectSource>())
                .Where(source => source.TriggerCheckpoints.Contains(checkpoint.Checkpoint))
                .Select(TriggerCandidate.FromContinuous);
            var candidates = active.Concat(continuous)
                .OrderBy(source => source.ControllerTeamSide == tieBreaker.RevealedSide ? 0 : 1)
                .ThenBy(source => source.OrderingSequence)
                .ThenBy(source => source.SourceId, StringComparer.Ordinal)
                .ToArray();

            foreach (var candidate in candidates)
            {
                if (!effects.TryGet(candidate.TriggeredEffect, out var effect))
                    throw new InvalidOperationException(
                        $"Triggered effect '{candidate.TriggeredEffect}' is not configured.");

                var context = new EffectResolutionContext(
                    effect,
                    capabilities,
                    participants,
                    timing,
                    checkpoint.RoundIndex,
                    checkpoint.TurnIndex,
                    modifications ?? throw new ArgumentNullException(nameof(modifications)),
                    EffectCardState.Of(definitions, instances),
                    candidate.EnclosingActionKind,
                    components: components,
                    heroItems: heroItems,
                    rosterState: rosterState);
                var step = EffectResolutionSession.Start(
                    new EffectResolutionId(
                        $"trigger-{candidate.SourceId}-{checkpoint.Checkpoint}-{checkpoint.RoundIndex}-{checkpoint.TurnIndex}"),
                    candidate.SourceCardInstanceId,
                    candidate.SourceDefinitionReference,
                    candidate.SourceActionDefinitionId,
                    candidate.ControllerHeroId,
                    candidate.ControllerHeroId,
                    ActionInvocationContext.PrintedPrimary,
                    context);
                if (!step.Accepted)
                    throw new InvalidOperationException("A configured triggered effect was refused by its effect frame.");
                PendingResolution = step.Frame;
                if (PendingResolution != null)
                    break;
            }

            return events;
        }

        private sealed class TriggerCandidate
        {
            private TriggerCandidate(
                string sourceId,
                HeroId controllerHeroId,
                TeamSide controllerTeamSide,
                CardInstanceId sourceCardInstanceId,
                CardDefinitionReference sourceDefinitionReference,
                ActionDefinitionId sourceActionDefinitionId,
                ActionKind enclosingActionKind,
                EffectDefinitionReference triggeredEffect,
                long orderingSequence)
            {
                SourceId = sourceId;
                ControllerHeroId = controllerHeroId;
                ControllerTeamSide = controllerTeamSide;
                SourceCardInstanceId = sourceCardInstanceId;
                SourceDefinitionReference = sourceDefinitionReference;
                SourceActionDefinitionId = sourceActionDefinitionId;
                EnclosingActionKind = enclosingActionKind;
                TriggeredEffect = triggeredEffect;
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
            public long OrderingSequence { get; }

            public static TriggerCandidate FromActive(
                ActiveEffectInstance source,
                CardDefinitionReference sourceDefinitionReference,
                ActionKind actionKind) =>
                new TriggerCandidate(
                    source.Id.Value,
                    source.ControllerHeroId,
                    source.ControllerTeamSide,
                    source.SourceCardInstanceId,
                    sourceDefinitionReference,
                    source.SourceActionDefinitionId,
                    actionKind,
                    source.Definition.TriggeredEffect.Value,
                    source.CreationSequence);

            public static TriggerCandidate FromContinuous(ContinuousTriggeredEffectSource source) =>
                new TriggerCandidate(
                    source.SourceId,
                    source.ControllerHeroId,
                    source.ControllerTeamSide,
                    source.SourceCardInstanceId,
                    source.SourceDefinitionReference,
                    source.SourceActionDefinitionId,
                    source.EnclosingActionKind,
                    source.TriggeredEffect,
                    source.OrderingSequence);
        }

        public EffectResolutionStepResult Answer(
            EffectChoiceCommand command,
            ActiveEffectLifecycle timing,
            IEnumerable<CardDefinition> cardDefinitions,
            IEnumerable<CardInstance> cardInstances,
            MatchParticipants participants,
            RuleModificationSet modifications,
            int roundIndex,
            int turnIndex,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (PendingResolution == null)
                throw new InvalidOperationException("No triggered effect is waiting for an answer.");
            var resolving = PendingResolution;
            if (!effects.TryGet(PendingResolution.EffectDefinitionReference, out var effect))
                throw new InvalidOperationException($"Triggered effect '{PendingResolution.EffectDefinitionReference}' is not configured.");

            var definitions = (cardDefinitions ?? throw new ArgumentNullException(nameof(cardDefinitions))).ToArray();
            var instances = (cardInstances ?? throw new ArgumentNullException(nameof(cardInstances))).ToArray();
            var sourceDefinition = definitions.Single(definition =>
                definition.Reference.Equals(PendingResolution.SourceDefinitionReference));
            var sourceAction = sourceDefinition.ActionDefinitions.SingleOrDefault(action =>
                action.Id.Equals(PendingResolution.ActionDefinitionId));
            var enclosingActionKind = sourceAction?.Kind ??
                (sourceDefinition.UltimateRule != null &&
                 sourceDefinition.UltimateRule.EffectSourceId.Equals(PendingResolution.ActionDefinitionId)
                    ? ActionKind.Skill
                    : throw new InvalidOperationException(
                        "A triggered continuation references neither an action nor its Ultimate ability."));
            var context = new EffectResolutionContext(
                effect,
                capabilities,
                participants,
                timing,
                roundIndex,
                turnIndex,
                modifications ?? throw new ArgumentNullException(nameof(modifications)),
                EffectCardState.Of(definitions, instances),
                enclosingActionKind,
                components: components,
                heroItems: heroItems,
                rosterState: rosterState);
            var step = EffectResolutionSession.Answer(PendingResolution, command, context);
            if (step.Accepted)
            {
                PendingResolution = step.Frame;
                LastCompletedResolution = step.IsComplete ? resolving : null;
            }
            return step;
        }
    }
}
