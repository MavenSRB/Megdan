using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Visibility-safe card faces grouped for inspection. Counts describe anonymous backs; no
    /// instance identity, location, commitment, or action state crosses this boundary.
    /// </summary>
    public sealed class HeroCardInspection
    {
        public HeroCardInspection(
            HeroId hero,
            TeamSide team,
            IEnumerable<CardDefinitionReference> handCards,
            IEnumerable<CardDefinitionReference> otherCards,
            IEnumerable<CardDefinitionReference> otherGoldCards,
            int concealedHandCount,
            int concealedOtherCount,
            int concealedOtherGoldCount,
            IEnumerable<CardDefinitionReference> spellbook)
        {
            if (!hero.IsDefined) throw new ArgumentException("An inspection names its hero.", nameof(hero));
            if (team != TeamSide.Red && team != TeamSide.Blue) throw new ArgumentOutOfRangeException(nameof(team));
            if (concealedHandCount < 0 || concealedHandCount > 64)
                throw new ArgumentOutOfRangeException(nameof(concealedHandCount));
            if (concealedOtherCount < 0 || concealedOtherCount > 64)
                throw new ArgumentOutOfRangeException(nameof(concealedOtherCount));
            if (concealedOtherGoldCount < 0 || concealedOtherGoldCount > 64)
                throw new ArgumentOutOfRangeException(nameof(concealedOtherGoldCount));

            Hero = hero;
            Team = team;
            HandCards = Copy(handCards);
            OtherCards = Copy(otherCards);
            OtherGoldCards = Copy(otherGoldCards);
            if (HandCards.Intersect(OtherCards).Any() ||
                HandCards.Intersect(OtherGoldCards).Any() ||
                OtherCards.Intersect(OtherGoldCards).Any())
                throw new ArgumentException("A face cannot be in more than one inspection section.");
            ConcealedHandCount = concealedHandCount;
            ConcealedOtherCount = concealedOtherCount;
            ConcealedOtherGoldCount = concealedOtherGoldCount;
            Cards = Array.AsReadOnly(HandCards.Concat(OtherCards).Concat(OtherGoldCards)
                .OrderBy(value => value.DefinitionId.Value, StringComparer.Ordinal).ToArray());
            Spellbook = Copy(spellbook);
        }

        public HeroId Hero { get; }
        public TeamSide Team { get; }
        public IReadOnlyList<CardDefinitionReference> HandCards { get; }
        public IReadOnlyList<CardDefinitionReference> OtherCards { get; }
        public IReadOnlyList<CardDefinitionReference> OtherGoldCards { get; }
        public int ConcealedHandCount { get; }
        public int ConcealedOtherCount { get; }
        public int ConcealedOtherGoldCount { get; }
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
                .Select(hero => ForHero(source, hero.HeroId, seat.Owns(hero.HeroId))).ToArray());

        public static HeroCardInspection ForHero(IMatchReadModel source, HeroId hero) =>
            ForHero(source, hero, true);

        private static HeroCardInspection ForHero(IMatchReadModel source, HeroId hero, bool revealHidden)
        {
            var setup = source.Setup.Heroes.FirstOrDefault(value => value.HeroId.Equals(hero));
            if (setup == null) return null;
            var definitions = source.Definitions.ToDictionary(value => value.Reference);
            var owned = source.Cards.Where(card => card.OwnerId.Equals(hero)).ToArray();
            bool IsCurrent(CardInstance card) =>
                definitions.TryGetValue(card.DefinitionReference, out var definition) &&
                !definition.Progression.IsSpell &&
                (card.State.Location == CardLocation.Hand || card.State.Location == CardLocation.TurnArea ||
                 card.State.Location == CardLocation.DiscardPile || card.State.Location == CardLocation.UltimateArea ||
                 (card.State.Location == CardLocation.Deck && definition.Progression.IsBasic &&
                  definition.Progression.Color == CardColor.Gold));

            var current = owned.Where(IsCurrent).ToArray();
            var hiddenCommitment = !revealHidden && current.Any(card =>
                card.State.Location == CardLocation.TurnArea &&
                card.State.FaceOrientation == CardFaceOrientation.FaceDown);
            var hiddenRestingColors = !revealHidden
                ? current.Where(CardLifecycleAuthority.IsFacedownRestingCard)
                    .Select(card => definitions[card.DefinitionReference].Progression.Color)
                    .Where(color => color != CardColor.None).Distinct().ToArray()
                : Array.Empty<CardColor>();
            bool Concealed(CardInstance card) =>
                !revealHidden &&
                ((hiddenCommitment && card.State.Location == CardLocation.Hand) ||
                 card.State.FaceOrientation == CardFaceOrientation.FaceDown ||
                 hiddenRestingColors.Contains(definitions[card.DefinitionReference].Progression.Color));
            bool IsOtherGold(CardInstance card)
            {
                var progression = definitions[card.DefinitionReference].Progression;
                return card.State.Location != CardLocation.Hand &&
                       progression.IsBasic &&
                       progression.Color == CardColor.Gold;
            }

            var hand = current.Where(card => card.State.Location == CardLocation.Hand).ToArray();
            var otherGold = current.Where(IsOtherGold).ToArray();
            var other = current.Where(card => card.State.Location != CardLocation.Hand && !IsOtherGold(card)).ToArray();
            var book = owned.Where(card => card.State.Location == CardLocation.Spellbook)
                .Select(card => card.DefinitionReference).Distinct();
            return new HeroCardInspection(
                hero,
                setup.TeamSide,
                hand.Where(card => !Concealed(card)).Select(card => card.DefinitionReference),
                other.Where(card => !Concealed(card)).Select(card => card.DefinitionReference),
                otherGold.Where(card => !Concealed(card)).Select(card => card.DefinitionReference),
                hand.Count(Concealed),
                other.Count(Concealed),
                otherGold.Count(Concealed),
                book);
        }
    }
}
