using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
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
            HeroId controllerHeroId,
            TeamSide controllerTeamSide,
            HeroId affectedHeroId,
            CardInstanceId sourceCardId,
            ActionDefinitionId sourceActionId,
            int roundIndex,
            int turnIndex,
            HeroId boundActorHeroId = default)
        {
            if (effects.Any(effect => effect.Id.Equals(id)))
                throw new ArgumentException("Active-effect instance IDs must be unique.", nameof(id));
            var duplicate = effects.FirstOrDefault(effect => effect.Phase != ActiveEffectPhase.Ended &&
                SharesUniquenessIdentity(
                    effect, definition, controllerHeroId, controllerTeamSide, affectedHeroId));
            if (duplicate != null)
                return new ActiveEffectLifecycleEvent(ActiveEffectLifecycleEventKind.DuplicateSuppressed,
                    duplicate.Id,
                    definition.CreationCheckpoint,
                    roundIndex,
                    turnIndex);

            var effect = new ActiveEffectInstance(id, definition, controllerHeroId, controllerTeamSide,
                affectedHeroId, sourceCardId, sourceActionId, roundIndex, turnIndex, nextCreationSequence++,
                boundActorHeroId: boundActorHeroId);
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
                    effect.Definition.TriggerCheckpoints.Contains(context.Checkpoint) &&
                    IsRelevantHero(effect, context.AffectedHeroId))
                {
                    results.Add(new ActiveEffectLifecycleEvent(ActiveEffectLifecycleEventKind.Triggered,
                        effect.Id, context.Checkpoint, context.RoundIndex, context.TurnIndex));
                }
            }

            return new ReadOnlyCollection<ActiveEffectLifecycleEvent>(results);
        }

        public ActiveEffectLifecycleRestoreState CaptureRestoreState() =>
            new ActiveEffectLifecycleRestoreState(effects, continuousSources);

        public static ActiveEffectLifecycle Restore(ActiveEffectLifecycleRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.effects.AddRange(state.Effects);
            lifecycle.continuousSources.AddRange(state.ContinuousSources);
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
                effect.ControllerHeroId,
                effect.ControllerTeamSide,
                effect.AffectedHeroId,
                effect.SourceCardInstanceId,
                effect.SourceActionDefinitionId,
                effect.CreatedRoundIndex,
                effect.CreatedTurnIndex,
                effect.CreationSequence,
                effect.Phase,
                effect.ActivatedRoundIndex,
                effect.ActivatedTurnIndex,
                effect.EndedAt,
                effect.BoundActorHeroId));
            return Restore(new ActiveEffectLifecycleRestoreState(
                clonedEffects,
                continuousSources.Select(source => new ContinuousRuleSource(
                    source.SourceId,
                    source.Kind,
                    source.ControllerHeroId,
                    source.RuleKind,
                    source.ApplicabilityKind,
                    source.SourceCardInstanceId))));
        }

        // Every live effect is visited at every checkpoint; the hero named by the context narrows
        // only the identity-targeted concerns (activation targeting, triggers, and the
        // ControllerHeroDefeated cancellation, which reads the controller rather than the declared
        // scope since milestone 7.4). An effect's own expiration window and its non-identity
        // cancellations must not depend on which hero happens to be acting.
        private IEnumerable<ActiveEffectInstance> OrderForCheckpoint(
            ActiveEffectCheckpointContext context,
            TieBreakerState tieBreaker)
        {
            return effects
                .OrderBy(effect => effect.ControllerTeamSide == tieBreaker.RevealedSide ? 0 : 1)
                .ThenBy(effect => effect.CreationSequence)
                .ThenBy(effect => effect.Id);
        }

        private static bool IsRelevantHero(ActiveEffectInstance effect, HeroId heroId)
        {
            if (!heroId.IsDefined) return true;
            if (effect.Definition.AffectedScope == ActiveEffectAffectedScopeKind.Global) return true;
            if (effect.Definition.AffectedScope == ActiveEffectAffectedScopeKind.ControllerHero)
                return effect.ControllerHeroId.Equals(heroId);
            return effect.Definition.AffectedScope != ActiveEffectAffectedScopeKind.Hero ||
                effect.AffectedHeroId.Equals(heroId);
        }

        /// <summary>
        /// Uniqueness compares the shared definition key against the scope-specific identity, which
        /// milestone 6.2a moved onto the instance: the key is what the effect is, the controller and
        /// affected hero are which occurrence of it this is. Milestone 6.1q established the scoping.
        /// </summary>
        private static bool SharesUniquenessIdentity(
            ActiveEffectInstance existing,
            ActiveEffectDefinition candidate,
            HeroId candidateControllerHeroId,
            TeamSide candidateControllerTeamSide,
            HeroId candidateAffectedHeroId)
        {
            if (!string.Equals(existing.Definition.UniquenessKey, candidate.UniquenessKey, StringComparison.Ordinal))
                return false;
            if (existing.Definition.AffectedScope != candidate.AffectedScope)
                return false;
            switch (candidate.AffectedScope)
            {
                // Milestone 8.9. A radius-scoped effect is one occurrence per controller, for the same
                // reason a controller-scoped one is: the disk is drawn around that hero, so two heroes
                // playing the same card create two different disks and neither is a duplicate of the
                // other. Falling through to the global arm below would have let the first Wasp to
                // resolve a Magnetic Dagger suppress the opposing Wasp's.
                case ActiveEffectAffectedScopeKind.UnitsInRadius:
                case ActiveEffectAffectedScopeKind.HeroesInRadius:
                case ActiveEffectAffectedScopeKind.ControllerHero:
                    return existing.ControllerHeroId.Equals(candidateControllerHeroId);
                case ActiveEffectAffectedScopeKind.Hero:
                    return existing.AffectedHeroId.Equals(candidateAffectedHeroId);
                case ActiveEffectAffectedScopeKind.ControllerTeam:
                    return existing.ControllerTeamSide == candidateControllerTeamSide;
                default:
                    return true;
            }
        }

        private static bool ShouldActivate(ActiveEffectInstance effect, ActiveEffectCheckpointContext context)
        {
            if (effect.Phase != ActiveEffectPhase.Dormant || effect.Definition.ActivationCheckpoint != context.Checkpoint)
                return false;
            if (!IsRelevantHero(effect, context.AffectedHeroId))
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
                case TimingCheckpoint.ControllerHeroDefeated:
                    condition = ActiveEffectCancellationCondition.ControllerHeroDefeated;

                    // Milestone 7.4 gave this checkpoint its first producer and, with it, its first
                    // real reading. Rulebook p.13 cancels "Defeated hero's actions with Active
                    // effects", and p.17 says the same thing from the effect's own side - an Active
                    // effect on your card ends when "you are defeated" - so both name the hero whose
                    // action created the effect, which is the controller.
                    //
                    // It is deliberately not the scope-aware <see cref="IsRelevantHero"/> reading this
                    // used before, and both directions of that difference matter. A Global effect one
                    // hero created would have been cancelled by *any* hero's Defeat, including the
                    // Defeat of the hero who created it being the only one that should - and a
                    // Hero-scoped effect a surviving hero aimed at the defeated one would have been
                    // cancelled although its owner is alive and its card is untouched. Neither is what
                    // either page says. The condition was named `AffectedHeroDefeated` until this
                    // milestone; the rename is the same correction seen from the declaration.
                    if (!context.AffectedHeroId.IsDefined ||
                        !effect.ControllerHeroId.Equals(context.AffectedHeroId)) return false;
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
