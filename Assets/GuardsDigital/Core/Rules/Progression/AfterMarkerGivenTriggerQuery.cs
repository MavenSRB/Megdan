using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// One rule source that has asked to run after its controller gave a hero a marker, the hero who
    /// received it, and the effect it runs. Milestone 8.40h.
    ///
    /// <para>The recipient is on the source rather than derived later for the reason milestone 8.19c1
    /// put the pushed object on its own: the trigger is ABOUT that hero. Bain's A Complicated
    /// Profession makes "that hero" discard, and a source naming only the moment would leave the effect
    /// asking a board that cannot answer - by the time the question is answered the marker may have been
    /// transferred, returned, or the hero defeated, and "that hero" is the one the give named.</para>
    /// </summary>
    public sealed class AfterMarkerGivenTriggerSource
    {
        public AfterMarkerGivenTriggerSource(
            string sourceId,
            HeroId controllerId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId sourceActionDefinitionId,
            GuardsDigital.Core.Effects.EffectDefinitionReference triggeredEffect,
            HeroId recipientId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "An after-marker-given trigger source requires stable identity.", nameof(sourceId));
            }

            if (!recipientId.IsDefined)
            {
                throw new ArgumentException(
                    "An after-marker-given trigger source names the hero who received the marker.",
                    nameof(recipientId));
            }

            SourceId = sourceId;
            ControllerId = controllerId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionReference = sourceDefinitionReference;
            SourceActionDefinitionId = sourceActionDefinitionId;
            TriggeredEffect = triggeredEffect;
            RecipientId = recipientId;
        }

        public string SourceId { get; }
        public HeroId ControllerId { get; }
        public CardInstanceId SourceCardInstanceId { get; }
        public CardDefinitionReference SourceDefinitionReference { get; }
        public ActionDefinitionId SourceActionDefinitionId { get; }
        public GuardsDigital.Core.Effects.EffectDefinitionReference TriggeredEffect { get; }

        /// <summary>The hero this give put the marker on, which is what the trigger runs about.</summary>
        public HeroId RecipientId { get; }

        /// <summary>
        /// The identity the trigger's own effect frame resolves under. It names the recipient as well
        /// as the source, so two gives in one resolution - a card that gives, has its marker taken, and
        /// gives again - produce two distinguishable frames rather than one identity reused.
        /// </summary>
        public string ResolutionIdValue => $"after-marker-given:{SourceId}:{RecipientId.Value}";
    }

    /// <summary>
    /// Milestone 8.40h. Which unlocked Ultimates have declared that they run after their controller has
    /// given a hero a marker, for the one give that has just happened.
    ///
    /// <para>It is the exact counterpart of <see cref="AfterPushTriggerQuery"/> and reads the same two
    /// facts: the declaration on the trusted rule, and rulebook p.16's one shared applicability - the
    /// ability is in effect while its hero is on the board. What differs is the fact matched against.
    /// That one matches a displacement; this one matches an attachment that now stands, reported by the
    /// giving capability itself.</para>
    ///
    /// <para><b>The giver is matched by controller, because the card says "after YOU give".</b> BA-D5:
    /// Close Call's transfer is another hero giving the marker to Bain, and a give another hero
    /// performs is that hero's give even when the marker lands on the same hero. Two of the three
    /// exclusions never reach this query at all - a transfer and a return report no recipient, so the
    /// resolver never stops for them - and this is where the third is decided.</para>
    ///
    /// <para><b>An Ultimate that is not unlocked is not here to be asked.</b> p.16's ability exists
    /// only once the card is beside the dashboard, and <see cref="UltimateUnlockQuery"/> is what
    /// decides that; a locked card contributes no member to the set this query walks, so a trigger it
    /// would have declared fires not at all.</para>
    /// </summary>
    public static class AfterMarkerGivenTriggerQuery
    {
        /// <summary>
        /// Whether any of these unlocked Ultimates watches the moment at all - the coarse question the
        /// effect resolver is told the answer to, so it knows whether to stop at a give.
        /// </summary>
        public static bool AnyWatching(UnlockedUltimateSet ultimates) =>
            ultimates != null && ultimates.Ultimates.Any(Watches);

        public static IReadOnlyList<AfterMarkerGivenTriggerSource> Derive(
            UnlockedUltimateSet ultimates,
            AdjacentMoveAuthority board,
            HeroId giverId,
            HeroId recipientId)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (!giverId.IsDefined || !recipientId.IsDefined)
            {
                return Array.Empty<AfterMarkerGivenTriggerSource>();
            }

            var watching = ultimates.Ultimates
                .Where(ultimate =>
                    Watches(ultimate) &&
                    ultimate.OwnerId.Equals(giverId) &&
                    board.TryGetHeroCoordinate(ultimate.OwnerId, out _))
                .ToArray();
            if (watching.Length == 0)
            {
                return Array.Empty<AfterMarkerGivenTriggerSource>();
            }

            // Rulebook p.16 gives a hero one Ultimate card, and this query is already narrowed to the
            // one hero who gave, so exactly one source can be watching. It is stated rather than
            // assumed for the reason `AfterPushTriggerQuery` states it: the resume path re-derives the
            // source from the giver alone, and a second watcher would make "the source for this giver"
            // ambiguous - picking one silently would run a card the player was never offered.
            if (watching.Length > 1)
            {
                throw new InvalidOperationException(
                    "Two unlocked Ultimates of one hero watch a marker being given; rulebook p.16 " +
                    "gives a hero one Ultimate.");
            }

            var ultimateWatching = watching[0];
            return new[]
            {
                new AfterMarkerGivenTriggerSource(
                    ultimateWatching.Source.SourceId,
                    ultimateWatching.OwnerId,
                    ultimateWatching.SourceCardInstanceId,
                    ultimateWatching.SourceDefinitionReference,
                    ultimateWatching.Rule.EffectSourceId,
                    ultimateWatching.Rule.TriggeredEffect.Value,
                    recipientId)
            };
        }

        /// <summary>
        /// Whether one unlocked Ultimate declares this milestone's trigger. Both halves are asked -
        /// the condition and the moment its declaration names - because
        /// <see cref="UltimateRuleDefinition"/> pairs them and a reader that asked only one would be
        /// trusting that pairing from the outside.
        /// </summary>
        private static bool Watches(UnlockedUltimate ultimate) =>
            ultimate.Rule.TriggerCondition ==
                UltimateTriggerCondition.AfterControllerGivesMarker &&
            ultimate.Rule.TriggerCheckpoints.Contains(TimingCheckpoint.MarkerGivenToHero) &&
            ultimate.Rule.TriggeredEffect.HasValue;
    }
}
