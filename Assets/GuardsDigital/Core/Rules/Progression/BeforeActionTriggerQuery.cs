using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// One rule source that has asked to run before an action starts, and the effect it runs.
    /// Milestone 8.19a.
    /// </summary>
    public sealed class BeforeActionTriggerSource
    {
        public BeforeActionTriggerSource(
            string sourceId,
            GuardsDigital.Core.Units.Heroes.HeroId controllerId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId sourceActionDefinitionId,
            GuardsDigital.Core.Effects.EffectDefinitionReference triggeredEffect,
            BeforeActionTrigger trigger)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "A before-action trigger source requires stable identity.", nameof(sourceId));
            }

            if (!trigger.IsDeclared)
            {
                throw new ArgumentException(
                    "A before-action trigger source carries the declaration that produced it.",
                    nameof(trigger));
            }

            SourceId = sourceId;
            ControllerId = controllerId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionReference = sourceDefinitionReference;
            SourceActionDefinitionId = sourceActionDefinitionId;
            TriggeredEffect = triggeredEffect;
            Trigger = trigger;
        }

        public string SourceId { get; }
        public GuardsDigital.Core.Units.Heroes.HeroId ControllerId { get; }
        public CardInstanceId SourceCardInstanceId { get; }
        public CardDefinitionReference SourceDefinitionReference { get; }
        public ActionDefinitionId SourceActionDefinitionId { get; }
        public GuardsDigital.Core.Effects.EffectDefinitionReference TriggeredEffect { get; }
        public BeforeActionTrigger Trigger { get; }
    }

    /// <summary>
    /// Milestone 8.19a. Which unlocked Ultimates have declared that they run before the action a hero
    /// is about to perform.
    ///
    /// <para>It is the exact counterpart of <see cref="UltimateTriggeredEffectSourceQuery"/> and reads
    /// the same two facts that query does: the declaration on the trusted rule, and rulebook p.16's
    /// one shared applicability - the ability is in effect while its hero is on the board. What
    /// differs is the fact matched against. That query matches a completed basic action; this one
    /// matches an action's <em>offer</em>, because the action has not started and its identity is still
    /// the offer the player selected.</para>
    ///
    /// <para>The consumed set is a parameter rather than state here, because "once per turn" is a fact
    /// about the turn and the turn owns it. A query that remembered would answer differently on a
    /// restored match than on a live one.</para>
    /// </summary>
    public static class BeforeActionTriggerQuery
    {
        public static IReadOnlyList<BeforeActionTriggerSource> Derive(
            UnlockedUltimateSet ultimates,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            ActionOffer offer,
            ActionKind actionKind,
            IReadOnlyCollection<string> consumedSourceIds,
            GuardsDigital.Core.Timing.ActiveEffectLifecycle timing = null,
            HeroItemState heroItems = null)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (consumedSourceIds == null) throw new ArgumentNullException(nameof(consumedSourceIds));
            if (!offer.IsAvailable || !offer.ActorId.IsDefined)
            {
                return Array.Empty<BeforeActionTriggerSource>();
            }

            return FromUltimates(ultimates, board, offer, actionKind, consumedSourceIds)
                .Concat(FromActiveEffects(timing, board, participants, offer, actionKind,
                    consumedSourceIds, heroItems))
                .ToArray();
        }

        private static IEnumerable<BeforeActionTriggerSource> FromUltimates(
            UnlockedUltimateSet ultimates,
            AdjacentMoveAuthority board,
            ActionOffer offer,
            ActionKind actionKind,
            IReadOnlyCollection<string> consumedSourceIds) =>
            ultimates.Ultimates
                .Where(ultimate =>
                    // Milestone 8.35z30. The declaration answers whether it fires before an
                    // action of this kind, because Wild Hunt's sentence names no kind and Living
                    // Tsunami's names one; asking the declaration keeps that difference in the one
                    // place that states it.
                    ultimate.Rule.BeforeAction.FiresBefore(actionKind) &&
                    (BeforeActionTriggerAdmitsKind(actionKind) ||
                        ultimate.Rule.BeforeAction.Frequency ==
                            BeforeActionTriggerFrequency.EveryActionIncludingReactions) &&
                    ultimate.Rule.TriggeredEffect.HasValue &&
                    ultimate.OwnerId.Equals(offer.ActorId) &&
                    board.IsHeroOnBoard(ultimate.OwnerId) &&
                    !consumedSourceIds.Contains(ultimate.Source.SourceId))
                .Select(ultimate => new BeforeActionTriggerSource(
                    ultimate.Source.SourceId,
                    ultimate.OwnerId,
                    ultimate.SourceCardInstanceId,
                    ultimate.SourceDefinitionReference,
                    ultimate.Rule.EffectSourceId,
                    ultimate.Rule.TriggeredEffect.Value,
                    ultimate.Rule.BeforeAction))
                .ToArray();

        /// <summary>
        /// Milestone 8.35z29. The same window, opened by a standing Active effect rather than by a
        /// p.16 permanent ability - Misa's Focus, Discipline and Mastery, "<b>Next turn:</b> Before you
        /// perform a primary action, you may move up to N spaces."
        ///
        /// <para><b>Why the declaration is a checkpoint and not a
        /// <see cref="BeforeActionTrigger"/> on the definition.</b> An Active effect's own vocabulary
        /// for "when does my triggered effect run" is its trigger checkpoints, and
        /// <see cref="GuardsDigital.Core.Timing.TimingCheckpoint.BeforePrimaryAction"/> is that moment
        /// named. A declaration field beside it would have been a second way to say the same thing,
        /// and - because a shipped hero's content identity is measured by walking the public shape of
        /// every declaration it reaches - a new field on the Active-effect definition would have moved
        /// six heroes who print nothing of the sort.</para>
        ///
        /// <para><b>The trigger the source carries is the action that is starting.</b> Arien's clause
        /// names one kind and is matched against it; Misa's names "a primary action" and so matches
        /// whichever primary is beginning. The frequency is <c>OncePerTurn</c>, which is the user's
        /// 2026-09-06 ruling - "It says action, not actions. So just the once." - and it is enforced by
        /// the caller's spent latch rather than by a second counter here, so a repeated instance of the
        /// same action finds the source already consumed and does not re-arm it.</para>
        ///
        /// <para>An effect whose controller is not the acting hero is skipped: p.17 makes an Active
        /// effect the property of the hero whose card raised it, and "before <em>you</em> perform"
        /// names that hero.</para>
        /// </summary>
        private static IEnumerable<BeforeActionTriggerSource> FromActiveEffects(
            GuardsDigital.Core.Timing.ActiveEffectLifecycle timing,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            ActionOffer offer,
            ActionKind actionKind,
            IReadOnlyCollection<string> consumedSourceIds,
            HeroItemState heroItems)
        {
            if (timing == null ||
                !BeforeActionTriggerAdmitsKind(actionKind) ||
                !PrimaryActionQuery.IsPerformingAPrimaryAction(offer.InvocationContext))
            {
                return Array.Empty<BeforeActionTriggerSource>();
            }

            return timing.Effects
                .Where(effect =>
                    effect.Definition.TriggeredEffect.HasValue &&
                    effect.Definition.TriggerCheckpoints.Contains(
                        GuardsDigital.Core.Timing.TimingCheckpoint.BeforePrimaryAction) &&
                    effect.Phase == ActiveEffectPhase.Active &&
                    ((effect.Definition.AffectedScope ==
                        ActiveEffectAffectedScopeKind.ControllerHero &&
                        effect.ControllerHeroId.Equals(offer.ActorId)) ||
                        EnemyInLiveDisk(effect, timing, board, participants, offer.ActorId,
                            heroItems)) &&
                    board.IsHeroOnBoard(effect.ControllerHeroId) &&
                    !consumedSourceIds.Contains(effect.Id.Value))
                .OrderBy(effect => effect.CreationSequence)
                .Select(effect => new BeforeActionTriggerSource(
                    effect.Id.Value,
                    effect.ControllerHeroId,
                    effect.SourceCardInstanceId,
                    default,
                    effect.SourceActionDefinitionId,
                    effect.Definition.TriggeredEffect.Value,
                    new BeforeActionTrigger(
                        effect.Definition.AffectedScope ==
                            ActiveEffectAffectedScopeKind.ControllerHero
                            ? actionKind : ActionKind.None,
                        effect.Definition.AffectedScope ==
                            ActiveEffectAffectedScopeKind.ControllerHero
                            ? BeforeActionTriggerFrequency.OncePerTurn
                            : BeforeActionTriggerFrequency.EveryAction)))
                .ToArray();
        }

        private static bool EnemyInLiveDisk(
            ActiveEffectInstance effect, ActiveEffectLifecycle timing,
            AdjacentMoveAuthority board,
            MatchParticipants participants, GuardsDigital.Core.Units.Heroes.HeroId actor,
            HeroItemState heroItems)
        {
            if (effect.Definition.AffectedScope != ActiveEffectAffectedScopeKind.HeroesInRadius ||
                effect.Definition.AffectedUnitRelation != SourceTeamRelation.OpposingSource ||
                !effect.Definition.Radius.IsDefined ||
                !board.TryGetHeroCoordinate(actor, out var actorAt) ||
                !TargetQuery.StandsInRelationTo(
                    SourceTeamRelation.OpposingSource,
                    BoardObjectReference.ForHero(actor), effect.ControllerHeroId,
                    participants, board, RuleModificationSet.Empty))
                return false;
            // Milestone 9.21h (RZ-Q13). The disk is drawn around every body of its controller; an owned
            // obstacle anchors it at the obstacle alone, as before.
            var anchors = board.GetHeroRadiusAnchors(effect.ControllerHeroId);
            if (anchors.Count == 0)
                return false;
            if (effect.Definition.TryGetOwnedObstacleRadiusAnchorKind(out var kind))
            {
                if (!board.TryGetObstacleCoordinate(
                        OwnedObstacleIdentity.For(kind, effect.ControllerHeroId), out var obstacleAt))
                    return false;
                anchors = new[] { obstacleAt };
            }
            var radius = effect.Definition.Radius.Resolve(
                effect.ControllerHeroId, effect.SourceCardInstanceId,
                effect.SourceActionDefinitionId, heroItems,
                ItemBonusMultiplierQuery.BonusPerItem(effect.ControllerHeroId, timing));
            var disk = new ValueResult(
                PrintedValueChannel.Radius,
                new[] { new ValueContribution(ValueContributionKind.PrintedIcon,
                    effect.SourceCardInstanceId, effect.SourceActionDefinitionId,
                    PrintedValueChannel.Radius, radius) });
            return anchors.Any(anchor => RadiusQuery.Contains(anchor, disk, actorAt));
        }

        /// <summary>
        /// A Defense primary is selected after p.14 opens the response window. A live enemy-disk
        /// trigger runs before that selection, so it can discard the only eligible Defense card.
        /// </summary>
        public static IReadOnlyList<BeforeActionTriggerSource> DeriveForDefense(
            UnlockedUltimateSet ultimates, ActiveEffectLifecycle timing, AdjacentMoveAuthority board,
            MatchParticipants participants,
            GuardsDigital.Core.Units.Heroes.HeroId defenderId,
            HeroItemState heroItems = null)
        {
            if (ultimates == null || board == null || participants == null ||
                !defenderId.IsDefined)
                return Array.Empty<BeforeActionTriggerSource>();
            var permanent = ultimates.Ultimates
                .Where(ultimate =>
                    ultimate.OwnerId.Equals(defenderId) &&
                    ultimate.Rule.BeforeAction.Frequency ==
                        BeforeActionTriggerFrequency.EveryActionIncludingReactions &&
                    ultimate.Rule.TriggeredEffect.HasValue &&
                    board.IsHeroOnBoard(defenderId))
                .Select(ultimate => new BeforeActionTriggerSource(
                    ultimate.Source.SourceId, ultimate.OwnerId,
                    ultimate.SourceCardInstanceId, ultimate.SourceDefinitionReference,
                    ultimate.Rule.EffectSourceId, ultimate.Rule.TriggeredEffect.Value,
                    ultimate.Rule.BeforeAction));
            var active = timing == null
                ? Enumerable.Empty<BeforeActionTriggerSource>()
                : timing.Effects
                .Where(effect =>
                    effect.Phase == ActiveEffectPhase.Active &&
                    effect.Definition.TriggeredEffect.HasValue &&
                    effect.Definition.TriggerCheckpoints.Contains(
                        TimingCheckpoint.BeforePrimaryAction) &&
                    EnemyInLiveDisk(effect, timing, board, participants,
                        defenderId, heroItems))
                .OrderBy(effect => effect.CreationSequence)
                .Select(effect => new BeforeActionTriggerSource(
                    effect.Id.Value, effect.ControllerHeroId,
                    effect.SourceCardInstanceId, default,
                    effect.SourceActionDefinitionId,
                    effect.Definition.TriggeredEffect.Value,
                    new BeforeActionTrigger(ActionKind.None,
                        BeforeActionTriggerFrequency.EveryAction)))
                .ToArray();
            return permanent.Concat(active).ToArray();
        }

        /// <summary>
        /// Whether the action starting is one <see cref="BeforeActionTrigger"/> can name at all. The
        /// three kinds a hero performs from a card's printed band are the three that declaration
        /// admits; a Defense is a reaction with no start of its own to precede, and p.7's Fast Travel
        /// and p.13's Clear are replacements rather than the printed primary the sentence names.
        /// </summary>
        private static bool BeforeActionTriggerAdmitsKind(ActionKind actionKind) =>
            actionKind == ActionKind.Attack ||
            actionKind == ActionKind.Movement ||
            actionKind == ActionKind.Skill;
    }
}
