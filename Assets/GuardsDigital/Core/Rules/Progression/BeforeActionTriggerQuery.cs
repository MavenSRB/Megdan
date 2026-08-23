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
            IReadOnlyCollection<string> consumedSourceIds)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (consumedSourceIds == null) throw new ArgumentNullException(nameof(consumedSourceIds));
            if (!offer.IsAvailable || !offer.ActorId.IsDefined)
            {
                return Array.Empty<BeforeActionTriggerSource>();
            }

            return ultimates.Ultimates
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
        }
    }
}
