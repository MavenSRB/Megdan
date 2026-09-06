using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;

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
            GuardsDigital.Core.Timing.ActiveEffectLifecycle timing = null)
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
                .Concat(FromActiveEffects(timing, board, offer, actionKind, consumedSourceIds))
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
                    ultimate.Rule.BeforeAction.IsDeclared &&
                    ultimate.Rule.BeforeAction.Action == actionKind &&
                    ultimate.Rule.TriggeredEffect.HasValue &&
                    ultimate.OwnerId.Equals(offer.ActorId) &&
                    board.TryGetHeroCoordinate(ultimate.OwnerId, out _) &&
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
            ActionOffer offer,
            ActionKind actionKind,
            IReadOnlyCollection<string> consumedSourceIds)
        {
            if (timing == null ||
                !BeforeActionTriggerAdmitsKind(actionKind))
            {
                return Array.Empty<BeforeActionTriggerSource>();
            }

            return timing.Effects
                .Where(effect =>
                    effect.Definition.TriggeredEffect.HasValue &&
                    effect.Definition.TriggerCheckpoints.Contains(
                        GuardsDigital.Core.Timing.TimingCheckpoint.BeforePrimaryAction) &&
                    effect.ControllerHeroId.Equals(offer.ActorId) &&
                    board.TryGetHeroCoordinate(effect.ControllerHeroId, out _) &&
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
                        actionKind, BeforeActionTriggerFrequency.OncePerTurn)))
                .ToArray();
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
