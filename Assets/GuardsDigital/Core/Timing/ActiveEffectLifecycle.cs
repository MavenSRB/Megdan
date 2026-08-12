using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Timing
{
    public enum ActiveEffectLifecycleEventKind
    {
        Created = 1,
        Activated = 2,
        Triggered = 3,
        Expired = 4,
        Cancelled = 5,
        DuplicateSuppressed = 6
    }

    public readonly struct ActiveEffectLifecycleEvent
    {
        public ActiveEffectLifecycleEvent(
            ActiveEffectLifecycleEventKind kind,
            ActiveEffectId effectId,
            TimingCheckpoint checkpoint,
            int roundIndex,
            int turnIndex)
        {
            Kind = kind;
            EffectId = effectId;
            Checkpoint = checkpoint;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
        }

        public ActiveEffectLifecycleEventKind Kind { get; }
        public ActiveEffectId EffectId { get; }
        public TimingCheckpoint Checkpoint { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
    }

    public sealed class ActiveEffectCheckpointContext
    {
        public ActiveEffectCheckpointContext(
            TimingCheckpoint checkpoint,
            int roundIndex,
            int turnIndex,
            HeroId affectedHeroId = default,
            CardInstanceId sourceCardInstanceId = default)
        {
            if (checkpoint == TimingCheckpoint.None) throw new ArgumentOutOfRangeException(nameof(checkpoint));
            if (roundIndex < 1 || turnIndex < 1) throw new ArgumentException("Timing context requires round and turn anchors.");
            Checkpoint = checkpoint;
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            AffectedHeroId = affectedHeroId;
            SourceCardInstanceId = sourceCardInstanceId;
        }

        public TimingCheckpoint Checkpoint { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public HeroId AffectedHeroId { get; }
        public CardInstanceId SourceCardInstanceId { get; }
    }

    public sealed class ActiveEffectLifecycle
    {
        private readonly List<ActiveEffectInstance> effects = new List<ActiveEffectInstance>();
        private readonly List<ContinuousRuleSource> continuousSources = new List<ContinuousRuleSource>();
        private long nextCreationSequence;

        public IReadOnlyList<ActiveEffectInstance> Effects => effects.AsReadOnly();
        public IReadOnlyList<ContinuousRuleSource> ContinuousSources => continuousSources.AsReadOnly();

        public ActiveEffectLifecycleEvent Create(
            ActiveEffectId id,
            ActiveEffectDefinition definition,
            CardInstanceId sourceCardId,
            ActionDefinitionId sourceActionId,
            int roundIndex,
            int turnIndex)
        {
            if (effects.Any(effect => effect.Id.Equals(id)))
                throw new ArgumentException("Active-effect instance IDs must be unique.", nameof(id));
            if (effects.Any(effect => effect.Phase != ActiveEffectPhase.Ended &&
                    string.Equals(effect.Definition.UniquenessKey, definition.UniquenessKey, StringComparison.Ordinal)))
                return new ActiveEffectLifecycleEvent(ActiveEffectLifecycleEventKind.DuplicateSuppressed,
                    effects.First(effect => effect.Phase != ActiveEffectPhase.Ended &&
                        string.Equals(effect.Definition.UniquenessKey, definition.UniquenessKey, StringComparison.Ordinal)).Id,
                    definition.CreationCheckpoint,
                    roundIndex,
                    turnIndex);

            var effect = new ActiveEffectInstance(id, definition, sourceCardId, sourceActionId,
                roundIndex, turnIndex, nextCreationSequence++);
            effects.Add(effect);
            if (definition.ActivationCheckpoint == definition.CreationCheckpoint &&
                definition.DurationKind != ActiveEffectDurationKind.NextTurn &&
                definition.DurationKind != ActiveEffectDurationKind.NextTurnAfterPlayingCards)
                effect.Activate(roundIndex, turnIndex);
            return new ActiveEffectLifecycleEvent(ActiveEffectLifecycleEventKind.Created, id,
                definition.CreationCheckpoint, roundIndex, turnIndex);
        }

        public void AddContinuousSource(ContinuousRuleSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (continuousSources.Any(value => string.Equals(value.SourceId, source.SourceId, StringComparison.Ordinal)))
                throw new ArgumentException("Continuous-source IDs must be unique.", nameof(source));
            continuousSources.Add(source);
        }

        public IReadOnlyList<ActiveEffectLifecycleEvent> ApplyCheckpoint(
            ActiveEffectCheckpointContext context,
            TieBreakerState tieBreaker)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!tieBreaker.IsDefined) throw new ArgumentException("Timing order requires Tie Breaker state.", nameof(tieBreaker));

            var results = new List<ActiveEffectLifecycleEvent>();
            foreach (var effect in OrderForCheckpoint(context, tieBreaker))
            {
                if (effect.Phase == ActiveEffectPhase.Ended) continue;
                if (ShouldCancel(effect, context))
                {
                    effect.End(context.Checkpoint);
                    results.Add(new ActiveEffectLifecycleEvent(ActiveEffectLifecycleEventKind.Cancelled,
                        effect.Id, context.Checkpoint, context.RoundIndex, context.TurnIndex));
                    continue;
                }

                if (ShouldCancelAtRoundBoundary(effect, context))
                {
                    effect.End(context.Checkpoint);
                    results.Add(new ActiveEffectLifecycleEvent(ActiveEffectLifecycleEventKind.Cancelled,
                        effect.Id, context.Checkpoint, context.RoundIndex, context.TurnIndex));
                    continue;
                }

                var activatedNow = ShouldActivate(effect, context);
                if (activatedNow)
                {
                    effect.Activate(context.RoundIndex, context.TurnIndex);
                    results.Add(new ActiveEffectLifecycleEvent(ActiveEffectLifecycleEventKind.Activated,
                        effect.Id, context.Checkpoint, context.RoundIndex, context.TurnIndex));
                }

                if (effect.Phase == ActiveEffectPhase.Active &&
                    effect.Definition.ExpirationCheckpoint == context.Checkpoint &&
                    IsExpirationForActivatedWindow(effect, context))
                {
                    effect.End(context.Checkpoint);
                    results.Add(new ActiveEffectLifecycleEvent(ActiveEffectLifecycleEventKind.Expired,
                        effect.Id, context.Checkpoint, context.RoundIndex, context.TurnIndex));
                }
                else if (!activatedNow && effect.Phase == ActiveEffectPhase.Active &&
                    effect.Definition.TriggerCheckpoints.Contains(context.Checkpoint))
                {
                    results.Add(new ActiveEffectLifecycleEvent(ActiveEffectLifecycleEventKind.Triggered,
                        effect.Id, context.Checkpoint, context.RoundIndex, context.TurnIndex));
                }
            }

            return new ReadOnlyCollection<ActiveEffectLifecycleEvent>(results);
        }

        public static ActiveEffectLifecycle Restore(
            IEnumerable<ActiveEffectInstance> effects,
            IEnumerable<ContinuousRuleSource> continuousSources)
        {
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.effects.AddRange(effects ?? throw new ArgumentNullException(nameof(effects)));
            lifecycle.continuousSources.AddRange(continuousSources ?? throw new ArgumentNullException(nameof(continuousSources)));
            if (lifecycle.effects.Select(effect => effect.Id).Distinct().Count() != lifecycle.effects.Count ||
                lifecycle.continuousSources.Select(source => source.SourceId).Distinct(StringComparer.Ordinal).Count() != lifecycle.continuousSources.Count)
                throw new ArgumentException("Restored timing identities must be unique.");
            lifecycle.nextCreationSequence = lifecycle.effects.Count == 0
                ? 0
                : lifecycle.effects.Max(effect => effect.CreationSequence) + 1;
            return lifecycle;
        }

        public ActiveEffectLifecycle Clone()
        {
            var clonedEffects = effects.Select(effect => new ActiveEffectInstance(
                effect.Id,
                effect.Definition,
                effect.SourceCardInstanceId,
                effect.SourceActionDefinitionId,
                effect.CreatedRoundIndex,
                effect.CreatedTurnIndex,
                effect.CreationSequence,
                effect.Phase,
                effect.ActivatedRoundIndex,
                effect.ActivatedTurnIndex,
                effect.EndedAt));
            return Restore(clonedEffects, continuousSources.Select(source => new ContinuousRuleSource(
                source.SourceId,
                source.Kind,
                source.ControllerHeroId,
                source.RuleKind,
                source.ApplicabilityKind,
                source.SourceCardInstanceId)));
        }

        private IEnumerable<ActiveEffectInstance> OrderForCheckpoint(
            ActiveEffectCheckpointContext context,
            TieBreakerState tieBreaker)
        {
            return effects.Where(effect => IsRelevantHero(effect, context.AffectedHeroId))
                .OrderBy(effect => effect.Definition.ControllerTeamSide == tieBreaker.RevealedSide ? 0 : 1)
                .ThenBy(effect => effect.CreationSequence)
                .ThenBy(effect => effect.Id);
        }

        private static bool IsRelevantHero(ActiveEffectInstance effect, HeroId heroId)
        {
            if (!heroId.IsDefined) return true;
            if (effect.Definition.AffectedScope == ActiveEffectAffectedScopeKind.Global) return true;
            if (effect.Definition.AffectedScope == ActiveEffectAffectedScopeKind.ControllerHero)
                return effect.Definition.ControllerHeroId.Equals(heroId);
            return effect.Definition.AffectedScope != ActiveEffectAffectedScopeKind.Hero ||
                effect.Definition.AffectedHeroId.Equals(heroId);
        }

        private static bool ShouldActivate(ActiveEffectInstance effect, ActiveEffectCheckpointContext context)
        {
            if (effect.Phase != ActiveEffectPhase.Dormant || effect.Definition.ActivationCheckpoint != context.Checkpoint)
                return false;
            if (effect.Definition.DurationKind == ActiveEffectDurationKind.NextTurn ||
                effect.Definition.DurationKind == ActiveEffectDurationKind.NextTurnAfterPlayingCards)
            {
                if (context.RoundIndex < effect.CreatedRoundIndex ||
                    (context.RoundIndex == effect.CreatedRoundIndex && context.TurnIndex <= effect.CreatedTurnIndex))
                    return false;
            }
            return true;
        }

        private static bool IsExpirationForActivatedWindow(ActiveEffectInstance effect, ActiveEffectCheckpointContext context)
        {
            if (!effect.ActivatedRoundIndex.HasValue || !effect.ActivatedTurnIndex.HasValue) return false;
            if (effect.Definition.DurationKind == ActiveEffectDurationKind.ThisRound)
                return context.RoundIndex == effect.ActivatedRoundIndex.Value;
            return context.RoundIndex == effect.ActivatedRoundIndex.Value &&
                context.TurnIndex == effect.ActivatedTurnIndex.Value;
        }

        private static bool ShouldCancelAtRoundBoundary(ActiveEffectInstance effect, ActiveEffectCheckpointContext context)
        {
            return context.Checkpoint == TimingCheckpoint.RoundEnd &&
                effect.Definition.DurationKind != ActiveEffectDurationKind.ThisRound &&
                !effect.Definition.CarriesAcrossRoundBoundary;
        }

        private static bool ShouldCancel(ActiveEffectInstance effect, ActiveEffectCheckpointContext context)
        {
            ActiveEffectCancellationCondition condition;
            switch (context.Checkpoint)
            {
                case TimingCheckpoint.SourceCardStateChanged:
                    condition = ActiveEffectCancellationCondition.SourceCardStateChanged;
                    if (!context.SourceCardInstanceId.Equals(effect.SourceCardInstanceId)) return false;
                    break;
                case TimingCheckpoint.AffectedHeroDefeated:
                    condition = ActiveEffectCancellationCondition.AffectedHeroDefeated;
                    break;
                case TimingCheckpoint.CardsRetrieved:
                    condition = ActiveEffectCancellationCondition.CardsRetrieved;
                    break;
                case TimingCheckpoint.ExplicitCancellation:
                    condition = ActiveEffectCancellationCondition.Explicit;
                    break;
                default:
                    return false;
            }
            return effect.Definition.CancellationConditions.Contains(condition);
        }

    }
}
