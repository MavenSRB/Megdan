using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Progression
{
    /// <summary>
    /// Milestone 8.112f. One run owed by an after-tokens-placed Ultimate: the source, and the one enemy
    /// hero the run concerns.
    /// </summary>
    public sealed class AfterTokensPlacedTriggerSource
    {
        public AfterTokensPlacedTriggerSource(
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
                    "An after-tokens-placed trigger source requires stable identity.", nameof(sourceId));
            }

            if (!recipientId.IsDefined)
            {
                throw new ArgumentException(
                    "An after-tokens-placed trigger source names the hero the run concerns.",
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
        public HeroId RecipientId { get; }

        public string ResolutionIdValue => $"after-tokens-placed:{SourceId}:{RecipientId.Value}";

        /// <summary>The per-turn key that spends "Once per turn" for this source.</summary>
        public string OncePerTurnKey => AfterTokensPlacedTriggerQuery.OncePerTurnKeyPrefix + SourceId;
    }

    /// <summary>
    /// Milestone 8.112f. Which runs an after-tokens-placed Ultimate owes for one placing step - Mrak's
    /// Rock and a Hard Place, "Once per turn, after you place one or more Rock tokens into one or more
    /// spaces adjacent to one or more enemy heroes, each of those heroes discards a card, if able."
    ///
    /// <para><b>"Those heroes"</b> are the enemy heroes standing on the board adjacent to at least one
    /// token of the declared kind that this step placed, each counted once and ordered by identity so a
    /// replay owes them in the same order. A step that placed none beside an enemy hero owes nothing and
    /// spends nothing - the "once" is spent by a placement the sentence is about, not by any placement.
    /// Whether this turn's once is already spent is the caller's question, asked of the turn's own
    /// key set with <see cref="AfterTokensPlacedTriggerSource.OncePerTurnKey"/>.</para>
    ///
    /// <para>Only the placing hero's own Ultimate watches (p.16 gives a hero one Ultimate, stated
    /// rather than assumed exactly as <see cref="AfterPushTriggerQuery"/> states it), and only while
    /// that hero is on the board.</para>
    /// </summary>
    public static class AfterTokensPlacedTriggerQuery
    {
        public const string OncePerTurnKeyPrefix = "after-tokens-placed:";

        public static bool AnyWatching(UnlockedUltimateSet ultimates) =>
            ultimates != null && ultimates.Ultimates.Any(Watches);

        public static IReadOnlyList<AfterTokensPlacedTriggerSource> Derive(
            UnlockedUltimateSet ultimates,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            HeroId placerId,
            IReadOnlyList<PlacedToken> placed)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (!placerId.IsDefined || placed == null || placed.Count == 0)
            {
                return Array.Empty<AfterTokensPlacedTriggerSource>();
            }

            var watching = ultimates.Ultimates
                .Where(ultimate =>
                    Watches(ultimate) &&
                    ultimate.OwnerId.Equals(placerId) &&
                    board.TryGetHeroCoordinate(ultimate.OwnerId, out _))
                .ToArray();
            if (watching.Length == 0)
            {
                return Array.Empty<AfterTokensPlacedTriggerSource>();
            }

            if (watching.Length > 1)
            {
                throw new InvalidOperationException(
                    "Two unlocked Ultimates of one hero watch a placement; rulebook p.16 gives a hero " +
                    "one Ultimate.");
            }

            var ultimate = watching[0];
            var kind = ultimate.Rule.AfterTokensPlaced.TokenKind;
            var spaces = placed
                .Where(token => token.Definition.Id.Equals(kind))
                .Select(token => token.Coordinate)
                .ToArray();
            if (spaces.Length == 0)
            {
                return Array.Empty<AfterTokensPlacedTriggerSource>();
            }

            var placerSide = participants.GetTeamForHero(placerId).Side;
            return participants.Heroes
                .Select(hero => hero.Id)
                .Where(hero =>
                    !hero.Equals(placerId) &&
                    participants.GetTeamForHero(hero).Side != placerSide &&
                    board.TryGetHeroCoordinate(hero, out var at) &&
                    spaces.Any(space => HexDistanceQuery.Calculate(space, at) == 1))
                .OrderBy(hero => hero.Value, StringComparer.Ordinal)
                .Select(hero => new AfterTokensPlacedTriggerSource(
                    ultimate.Source.SourceId,
                    ultimate.OwnerId,
                    ultimate.SourceCardInstanceId,
                    ultimate.SourceDefinitionReference,
                    ultimate.Rule.EffectSourceId,
                    ultimate.Rule.TriggeredEffect.Value,
                    hero))
                .ToArray();
        }

        /// <summary>
        /// The runs still owed to these heroes by the controller's one watching Ultimate - the answer
        /// path's re-derivation of a paused queue, which a snapshot carries as the heroes alone.
        /// </summary>
        public static IReadOnlyList<AfterTokensPlacedTriggerSource> ForRecipients(
            UnlockedUltimateSet ultimates,
            HeroId controllerId,
            IEnumerable<HeroId> recipients)
        {
            if (ultimates == null) throw new ArgumentNullException(nameof(ultimates));
            var ultimate = ultimates.Ultimates.SingleOrDefault(
                candidate => Watches(candidate) && candidate.OwnerId.Equals(controllerId));
            if (ultimate == null || recipients == null)
            {
                return Array.Empty<AfterTokensPlacedTriggerSource>();
            }

            return recipients
                .Where(hero => hero.IsDefined)
                .Select(hero => new AfterTokensPlacedTriggerSource(
                    ultimate.Source.SourceId,
                    ultimate.OwnerId,
                    ultimate.SourceCardInstanceId,
                    ultimate.SourceDefinitionReference,
                    ultimate.Rule.EffectSourceId,
                    ultimate.Rule.TriggeredEffect.Value,
                    hero))
                .ToArray();
        }

        private static bool Watches(UnlockedUltimate ultimate) =>
            ultimate.Rule.AfterTokensPlaced.IsDeclared && ultimate.Rule.TriggeredEffect.HasValue;
    }
}
