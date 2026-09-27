using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Milestone 8.112c. Which resolved cards a hero may use in place of a card in hand, read from the
    /// live rule set rather than from any card's name.
    ///
    /// <para>A card qualifies while three things hold: a live grant of the asked permission affects the
    /// hero, the grant's source card is that hero's own, and the card is still resolved in the turn
    /// area - p.17 cancels the grant the moment its card changes state, and this re-reads the card so an
    /// answer arriving after the card moved is refused rather than trusted.</para>
    /// </summary>
    public static class SourceCardSubstitutionQuery
    {
        public static IReadOnlyList<CardInstance> Substitutes(
            RuleModificationSet modifications,
            HeroId heroId,
            RulePermissionKind permission,
            IEnumerable<CardInstance> instances)
        {
            if (permission != RulePermissionKind.DiscardSourceCardInsteadOfHandCard &&
                permission != RulePermissionKind.DefendWithSourceCardAsIfInHand)
            {
                throw new ArgumentException("Only a source-card substitution is asked here.",
                    nameof(permission));
            }

            if (modifications == null || !heroId.IsDefined || instances == null)
            {
                return Array.Empty<CardInstance>();
            }

            var hero = RuleObject.ForHero(heroId);
            var sources = new HashSet<CardInstanceId>(modifications.Modifications
                .Where(applied =>
                    applied.AffectedObject.Equals(hero) &&
                    applied.Modification.Kind == RuleModificationKind.Permission &&
                    applied.Modification.Permission == permission &&
                    applied.SourceCardInstanceId.IsDefined)
                .Select(applied => applied.SourceCardInstanceId));
            if (sources.Count == 0)
            {
                return Array.Empty<CardInstance>();
            }

            return instances
                .Where(card => card != null && sources.Contains(card.Id) &&
                    card.OwnerId.Equals(heroId) &&
                    CardLifecycleAuthority.IsResolvedCardInTurnArea(card))
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
