using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>Read-only faces, with no instance IDs, locations, commitment or action state.</summary>
    public sealed class HeroCardInspection
    {
        public HeroCardInspection(HeroId hero, TeamSide team,
            IEnumerable<CardDefinitionReference> cards, IEnumerable<CardDefinitionReference> spellbook)
        {
            if (!hero.IsDefined) throw new ArgumentException("An inspection names its hero.", nameof(hero));
            if (team != TeamSide.Red && team != TeamSide.Blue) throw new ArgumentOutOfRangeException(nameof(team));
            Hero = hero;
            Team = team;
            Cards = Copy(cards);
            Spellbook = Copy(spellbook);
        }
        public HeroId Hero { get; }
        public TeamSide Team { get; }
        public IReadOnlyList<CardDefinitionReference> Cards { get; }
        public IReadOnlyList<CardDefinitionReference> Spellbook { get; }

        private static IReadOnlyList<CardDefinitionReference> Copy(IEnumerable<CardDefinitionReference> source)
        {
            var values = (source ?? throw new ArgumentNullException(nameof(source))).ToArray();
            if (values.Length > 64 || values.Any(value => !value.IsDefined) ||
                values.Distinct().Count() != values.Length)
                throw new ArgumentException("Invalid inspection faces.");
            return Array.AsReadOnly(values.OrderBy(value => value.DefinitionId.Value, StringComparer.Ordinal).ToArray());
        }

        public static IReadOnlyList<HeroCardInspection> ForSeat(
            MatchSessionProjection source, MatchSeat seat, ExperimentalMatchOptions options) =>
            Array.AsReadOnly(source.Setup.Heroes.Where(hero => seat.Owns(hero.HeroId) ||
                (hero.TeamSide == seat.TeamSide ? options.TeammateCoordination : options.EnemyCardInformation))
                .Select(hero => ForHero(source, hero.HeroId)).ToArray());

        public static HeroCardInspection ForHero(IMatchReadModel source, HeroId hero)
        {
            var setup = source.Setup.Heroes.FirstOrDefault(value => value.HeroId.Equals(hero));
            if (setup == null) return null;
            var definitions = source.Definitions.ToDictionary(value => value.Reference);
            var owned = source.Cards.Where(card => card.OwnerId.Equals(hero)).ToArray();
            var current = owned.Where(card => definitions.TryGetValue(card.DefinitionReference, out var definition) &&
                !definition.Progression.IsSpell &&
                (card.State.Location == CardLocation.Hand || card.State.Location == CardLocation.TurnArea ||
                 card.State.Location == CardLocation.DiscardPile || card.State.Location == CardLocation.UltimateArea ||
                 (card.State.Location == CardLocation.Deck && definition.Progression.IsBasic &&
                  definition.Progression.Color == CardColor.Gold)))
                .Select(card => card.DefinitionReference).Distinct();
            var book = owned.Where(card => card.State.Location == CardLocation.Spellbook)
                .Select(card => card.DefinitionReference).Distinct();
            return new HeroCardInspection(hero, setup.TeamSide, current, book);
        }
    }
}
