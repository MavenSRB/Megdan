using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Public per-body minion modifiers, available outside Attack selection as well. Reuses the
    /// existing immutable contribution shape and the combat rules query without reading cards,
    /// accepting commands, or reconstructing match authority on a joined client.
    /// </summary>
    public static class HeroMinionDefenseQuery
    {
        public static IReadOnlyList<AttackTargetDefensePreviewProjection> Create(IMatchReadModel projection)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            return Array.AsReadOnly(projection.Heroes.Select(hero =>
                new AttackTargetDefensePreviewProjection(hero.HeroId, hero.Body,
                    DefenseValueQuery.CalculateMinionContributions(
                        projection.Participants.GetTeamForHero(hero.HeroId).Side,
                        hero.Coordinate, projection.Minions)
                    .Select(value => new ValueContributionProjection(value, value.SourceMinionId.Value))))
                .ToArray());
        }
    }
}
