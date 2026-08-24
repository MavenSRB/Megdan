using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// One rule source that has asked to run after a Push applied, the object it applied to, and the
    /// effect it runs. Milestone 8.19c1.
    ///
    /// <para>The pushed object is on the source rather than derived later because the trigger is about
    /// that object: Sabina's Big Sodding Gun makes "that hero" discard, and a source that named only
    /// the moment would leave the effect asking the board which of several pushed units it meant.</para>
    /// </summary>
    public sealed class AfterPushTriggerSource
    {
        public AfterPushTriggerSource(
            string sourceId,
            HeroId controllerId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId sourceActionDefinitionId,
            GuardsDigital.Core.Effects.EffectDefinitionReference triggeredEffect,
            AfterPushTrigger trigger,
            RuleObject pushedObject)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "An after-push trigger source requires stable identity.", nameof(sourceId));
            }

            if (!trigger.IsDeclared)
            {
                throw new ArgumentException(
                    "An after-push trigger source carries the declaration that produced it.",
                    nameof(trigger));
            }

            if (!pushedObject.IsDefined)
            {
                throw new ArgumentException(
                    "An after-push trigger source names the object the Push applied to.",
                    nameof(pushedObject));
            }

            SourceId = sourceId;
            ControllerId = controllerId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionReference = sourceDefinitionReference;
            SourceActionDefinitionId = sourceActionDefinitionId;
            TriggeredEffect = triggeredEffect;
            Trigger = trigger;
            PushedObject = pushedObject;
        }

        public string SourceId { get; }
        public HeroId ControllerId { get; }
        public CardInstanceId SourceCardInstanceId { get; }
        public CardDefinitionReference SourceDefinitionReference { get; }
        public ActionDefinitionId SourceActionDefinitionId { get; }
        public GuardsDigital.Core.Effects.EffectDefinitionReference TriggeredEffect { get; }
        public AfterPushTrigger Trigger { get; }

        /// <summary>The object this Push actually displaced, which is what the trigger runs for.</summary>
        public RuleObject PushedObject { get; }

        /// <summary>
        /// The identity the trigger's own effect frame resolves under. It names the push subject as
        /// well as the source, because one push can displace several objects a single source watches
        /// and each of them gets its own run of the effect.
        /// </summary>
        public string ResolutionIdValue =>
            $"after-push:{SourceId}:{DescribePushedObject()}";

        private string DescribePushedObject() =>
            IntrinsicKindQuery.Of(PushedObject) == RuleObjectKind.Hero
                ? PushedObject.HeroId.Value
                : PushedObject.MinionId.Value;
    }

    /// <summary>
    /// Milestone 8.19c1. Which unlocked Ultimates have declared that they run after a Push that has
    /// just applied, once per object each of them names.
    ///
    /// <para>It is the exact counterpart of <see cref="BeforeActionTriggerQuery"/> and reads the same
    /// two facts: the declaration on the trusted rule, and rulebook p.16's one shared applicability -
    /// the ability is in effect while its hero is on the board. What differs is the fact matched
    /// against. That query matches an action's offer; this one matches a displacement that has already
    /// happened, which is why <paramref name="pushedObjects"/> is what the pusher actually moved rather
    /// than what it tried to move. p.18's "if you push" is the applied displacement, so a push that
    /// moved nothing hands this query an empty list and reaches no trigger at all.</para>
    ///
    /// <para>The pusher is matched by controller, because the card says "if <em>you</em> push": a Push
    /// performed by anybody else is somebody else's push even when it displaces the same hero.</para>
    /// </summary>
    public static class AfterPushTriggerQuery
    {
        public static IReadOnlyList<AfterPushTriggerSource> Derive(
            UnlockedUltimateSet ultimates,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            HeroId pusherId,
            IReadOnlyList<RuleObject> pushedObjects)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (pushedObjects == null) throw new ArgumentNullException(nameof(pushedObjects));
            if (!pusherId.IsDefined || pushedObjects.Count == 0)
            {
                return Array.Empty<AfterPushTriggerSource>();
            }

            var watching = ultimates.Ultimates
                .Where(ultimate =>
                    ultimate.Rule.AfterPush.IsDeclared &&
                    ultimate.Rule.TriggeredEffect.HasValue &&
                    ultimate.OwnerId.Equals(pusherId) &&
                    board.TryGetHeroCoordinate(ultimate.OwnerId, out _))
                .ToArray();
            if (watching.Length == 0)
            {
                return Array.Empty<AfterPushTriggerSource>();
            }

            // Rulebook p.16 gives a hero one Ultimate card, and this query is already narrowed to the
            // one hero who performed the push, so exactly one source can be watching. It is stated
            // rather than assumed because the resume path re-derives the source from the pusher alone:
            // a second watcher would make "the source for this pusher" ambiguous, and picking one
            // silently would run a card the player was never offered.
            if (watching.Length > 1)
            {
                throw new InvalidOperationException(
                    "Two unlocked Ultimates of one hero declare an after-push trigger; rulebook p.16 " +
                    "gives a hero one Ultimate, and resuming a paused trigger reads the source back " +
                    "from its pusher.");
            }

            var sources = new List<AfterPushTriggerSource>();
            foreach (var ultimate in watching)
            {
                foreach (var pushed in pushedObjects)
                {
                    if (!Concerns(ultimate.Rule.AfterPush, pushed, pusherId, participants))
                    {
                        continue;
                    }

                    sources.Add(new AfterPushTriggerSource(
                        ultimate.Source.SourceId,
                        ultimate.OwnerId,
                        ultimate.SourceCardInstanceId,
                        ultimate.SourceDefinitionReference,
                        ultimate.Rule.EffectSourceId,
                        ultimate.Rule.TriggeredEffect.Value,
                        ultimate.Rule.AfterPush,
                        pushed));
                }
            }

            return sources;
        }

        /// <summary>
        /// Whether one declared subject is met by one displaced object.
        ///
        /// An explicit switch for the reason <see cref="UltimateTriggeredEffectSourceQuery"/>'s is one:
        /// a second subject added later must not silently widen the first. The hero reading is
        /// intrinsic - p.19's own - rather than effective, because a card that says "enemy hero" is
        /// naming a unit type and an object merely counting as one for another rule has not become the
        /// hero this trigger would then make discard a card.
        /// </summary>
        private static bool Concerns(
            AfterPushTrigger trigger,
            RuleObject pushed,
            HeroId pusherId,
            MatchParticipants participants)
        {
            switch (trigger.Subject)
            {
                case AfterPushTriggerSubject.EnemyHero:
                    return IntrinsicKindQuery.Of(pushed) == RuleObjectKind.Hero &&
                        !pushed.HeroId.Equals(pusherId) &&
                        participants.GetTeamForHero(pushed.HeroId).Side !=
                            participants.GetTeamForHero(pusherId).Side;
                default:
                    return false;
            }
        }
    }
}
