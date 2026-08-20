using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Which group of rulebook p.6's current-round hand a card is in right now.
    ///
    /// The three are read from projected card lifecycle state, never maintained here: milestone 7.8a's
    /// acceptance rule is that retrieval, a Defense discard, any other implemented discard effect, and a
    /// round reset are not reinterpreted by the presenter, and the only way to keep that promise is for
    /// the group to be a reading of <c>CardLocation</c> plus the round's own assignment history.
    /// </summary>
    public enum TrayCardGroup
    {
        None = 0,

        /// <summary>Committed and resolved this round: the assignment history names its turn.</summary>
        Played = 1,

        /// <summary>Still in hand, so still choosable for a later turn of this round.</summary>
        Available = 2,

        /// <summary>In the discard pile, wherever it came from.</summary>
        Discarded = 3
    }

    /// <summary>
    /// One card of the viewed hero's current round, as the tray draws it.
    ///
    /// It carries the card's authoritative colour rather than a swatch chosen here, and the colour's own
    /// name beside it, so the button is never colour alone. <see cref="PlayedTurnIndex"/> is the turn the
    /// round's assignment history recorded, and is zero for every group but <see cref="TrayCardGroup.Played"/>.
    /// </summary>
    public sealed class TrayCardViewModel
    {
        public TrayCardViewModel(
            CardInstanceId cardInstanceId,
            string displayName,
            CardColor color,
            string valueLine,
            TrayCardGroup group,
            int playedTurnIndex,
            bool isSelectable,
            bool isSelected)
        {
            if (!cardInstanceId.IsDefined)
            {
                throw new ArgumentException("A tray card names its instance.", nameof(cardInstanceId));
            }

            if (group == TrayCardGroup.None)
            {
                throw new ArgumentException("A tray card is in one of the three groups.", nameof(group));
            }

            if (group == TrayCardGroup.Played != (playedTurnIndex > 0))
            {
                throw new ArgumentException(
                    "A played card names the turn it was played on, and only a played card does.",
                    nameof(playedTurnIndex));
            }

            CardInstanceId = cardInstanceId;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Color = color;
            ValueLine = valueLine ?? string.Empty;
            Group = group;
            PlayedTurnIndex = playedTurnIndex;
            IsSelectable = isSelectable;
            IsSelected = isSelected;
            ColorName = MatchHudTheme.ColorName(color);
            HistoryLabel = group == TrayCardGroup.Played
                ? $"PLAYED · TURN {playedTurnIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
                : group == TrayCardGroup.Discarded
                    ? "DISCARDED"
                    : "AVAILABLE";
        }

        public CardInstanceId CardInstanceId { get; }
        public string DisplayName { get; }
        public CardColor Color { get; }
        public string ColorName { get; }

        /// <summary>The card's printed Initiative and action values, compactly.</summary>
        public string ValueLine { get; }

        public TrayCardGroup Group { get; }
        public int PlayedTurnIndex { get; }

        /// <summary>
        /// Whether the authority currently offers this card for commitment. A card that is not
        /// selectable still draws, because reading what a hero has played and discarded is the point of
        /// the tray; it simply carries no command.
        /// </summary>
        public bool IsSelectable { get; }

        public bool IsSelected { get; }

        /// <summary>`PLAYED · TURN n`, `AVAILABLE`, or `DISCARDED` - and never a round number.</summary>
        public string HistoryLabel { get; }
    }

    /// <summary>One of rulebook p.16's item bonuses, as a compact signed chip.</summary>
    public sealed class HeroItemBonusViewModel
    {
        public HeroItemBonusViewModel(PrintedValueChannel channel, int amount)
        {
            if (channel == PrintedValueChannel.None || amount <= 0)
            {
                throw new ArgumentException("An item bonus raises one icon by a positive amount.");
            }

            Channel = channel;
            Amount = amount;
            Token = MatchHudTheme.ChannelToken(channel);
            AmountLabel = MatchHudTheme.Signed(amount);
            AssistiveLabel = $"{MatchHudTheme.ChannelName(channel)} item bonus {AmountLabel}";
        }

        public PrintedValueChannel Channel { get; }
        public int Amount { get; }
        public string Token { get; }
        public string AmountLabel { get; }
        public string AssistiveLabel { get; }
    }

    /// <summary>
    /// One of the four fixed slots standing for this hero's turns in the current round.
    ///
    /// It carries no visible text at all - milestone 7.8a's acceptance rule - and the ordinal and colour
    /// live in <see cref="AssistiveLabel"/>, which is the hover and accessibility reading of the same
    /// slot. That is the mockup's own arrangement: a swatch with an `aria-label` behind it.
    /// </summary>
    public sealed class HeroTurnSlotViewModel
    {
        public HeroTurnSlotViewModel(int turnIndex, CardColor color, bool isOccupied)
        {
            if (turnIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(turnIndex));
            }

            TurnIndex = turnIndex;
            Color = color;
            IsOccupied = isOccupied;
            AssistiveLabel = isOccupied
                ? $"Turn {turnIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)}: " +
                    $"{MatchHudTheme.ColorName(color)} card"
                : $"Turn {turnIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)}: empty";
        }

        public int TurnIndex { get; }
        public CardColor Color { get; }
        public bool IsOccupied { get; }
        public string AssistiveLabel { get; }
    }

    /// <summary>
    /// One hero's compact overview: who they are, what their items raise, what they have played this
    /// round, and how big their discard pile is.
    ///
    /// It is a repeated component rather than a pair of singletons. The configured scene has one hero
    /// per team today and draws one of these on each side; a six-hero scene draws six without this type
    /// learning anything, because nothing in it is about Wasp, about a side of the screen, or about
    /// there being exactly two.
    /// </summary>
    public sealed class HeroOverviewViewModel
    {
        public HeroOverviewViewModel(
            HeroId heroId,
            TeamSide teamSide,
            string heroDisplayName,
            int level,
            IEnumerable<HeroItemBonusViewModel> itemBonuses,
            IEnumerable<HeroTurnSlotViewModel> turnSlots,
            int discardCount,
            IEnumerable<CardColor> discardColors,
            bool isViewed)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("An overview names its hero.", nameof(heroId));
            }

            if (discardCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(discardCount));
            }

            HeroId = heroId;
            TeamSide = teamSide;
            HeroDisplayName = heroDisplayName ?? throw new ArgumentNullException(nameof(heroDisplayName));
            Level = level;
            ItemBonuses = Array.AsReadOnly((itemBonuses ??
                Enumerable.Empty<HeroItemBonusViewModel>()).ToArray());
            TurnSlots = Array.AsReadOnly((turnSlots ??
                throw new ArgumentNullException(nameof(turnSlots))).ToArray());
            DiscardCount = discardCount;
            DiscardColors = Array.AsReadOnly((discardColors ?? Enumerable.Empty<CardColor>()).ToArray());
            IsViewed = isViewed;
            TeamLabel = teamSide == TeamSide.Red ? "RED HERO" : "BLUE HERO";
            LevelLabel = "LVL " + level.ToString(System.Globalization.CultureInfo.InvariantCulture);
            DiscardLabel = "DISCARD";
            DiscardCountLabel = discardCount.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // The icon slot the milestone 7.8a decision wired: a configured image drops in here later,
            // and until one exists the disc carries the hero's own initial so the two rails are told
            // apart by a glyph as well as by a side and a colour.
            IconInitial = heroDisplayName.Trim().Length == 0
                ? "?"
                : heroDisplayName.Trim().Substring(0, 1).ToUpperInvariant();
        }

        public HeroId HeroId { get; }
        public TeamSide TeamSide { get; }
        public string HeroDisplayName { get; }
        public string TeamLabel { get; }
        public string IconInitial { get; }

        /// <summary>p.15's Hero Level, projected rather than counted from upgrades seen.</summary>
        public int Level { get; }

        public string LevelLabel { get; }
        public IReadOnlyList<HeroItemBonusViewModel> ItemBonuses { get; }
        public IReadOnlyList<HeroTurnSlotViewModel> TurnSlots { get; }
        public int DiscardCount { get; }
        public string DiscardLabel { get; }
        public string DiscardCountLabel { get; }

        /// <summary>
        /// The colours currently in this hero's discard pile, so the fifth slot can show what is in it
        /// rather than only how much. The pile has no authoritative order, so these are in colour order
        /// rather than in an order this presenter would have had to invent.
        /// </summary>
        public IReadOnlyList<CardColor> DiscardColors { get; }

        /// <summary>Whether the tray is currently showing this hero, which the rail marks.</summary>
        public bool IsViewed { get; }
    }

    public sealed class HeroOverviewsViewModel
    {
        public HeroOverviewsViewModel(long version, IEnumerable<HeroOverviewViewModel> heroes)
        {
            Version = version;
            Heroes = Array.AsReadOnly((heroes ?? throw new ArgumentNullException(nameof(heroes))).ToArray());
        }

        public long Version { get; }
        public IReadOnlyList<HeroOverviewViewModel> Heroes { get; }
    }

    public interface IHeroOverviewsView
    {
        void Render(HeroOverviewsViewModel model);
    }

    /// <summary>
    /// Builds every hero's overview from the current projection, and nothing else.
    ///
    /// It submits no command and holds no state of its own beyond the viewed hero it is told about, so
    /// restore, replay and retained-prefix Undo redraw it by handing it the projection they produced.
    /// </summary>
    public sealed class HeroOverviewsPresenter
    {
        private readonly IMatchSession session;
        private readonly IHeroOverviewsView view;
        private readonly Func<HeroId> viewedHero;
        private MatchSessionProjection lastProjection;
        private HeroId lastViewedHero;
        private HeroOverviewsViewModel lastViewModel;

        public HeroOverviewsPresenter(
            IMatchSession session,
            IHeroOverviewsView view,
            Func<HeroId> viewedHero = null)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.viewedHero = viewedHero ?? (() => default);
        }

        public void Refresh()
        {
            var projection = session.Projection;
            var viewed = viewedHero();
            if (!ReferenceEquals(projection, lastProjection) || !viewed.Equals(lastViewedHero) ||
                lastViewModel == null)
            {
                lastProjection = projection;
                lastViewedHero = viewed;
                lastViewModel = new HeroOverviewsViewModel(
                    projection.Version,
                    projection.Setup.Heroes
                        .OrderBy(hero => hero.TeamSide)
                        .ThenBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                        .Select(hero => Build(projection, hero.HeroId, hero.TeamSide, hero.DisplayName, viewed)));
            }

            view.Render(lastViewModel);
        }

        private static HeroOverviewViewModel Build(
            MatchSessionProjection projection,
            HeroId heroId,
            TeamSide teamSide,
            string displayName,
            HeroId viewedHeroId)
        {
            var progression = projection.HeroProgression
                .FirstOrDefault(entry => entry.HeroId.Equals(heroId));
            var discarded = projection.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.DiscardPile)
                .ToArray();
            return new HeroOverviewViewModel(
                heroId,
                teamSide,
                displayName,
                progression?.Level ?? 0,
                (progression?.ItemBonuses ?? Array.Empty<KeyValuePair<PrintedValueChannel, int>>())
                    .OrderBy(entry => MatchHudTheme.ChannelOrder.ToList().IndexOf(entry.Key))
                    .Select(entry => new HeroItemBonusViewModel(entry.Key, entry.Value)),
                CurrentRoundCards.TurnSlots(projection, heroId),
                discarded.Length,
                discarded
                    .Select(card => CurrentRoundCards.ColorOf(projection, card))
                    .Distinct()
                    .OrderBy(color => (int)color),
                heroId.Equals(viewedHeroId));
        }
    }

    /// <summary>
    /// The two readings of one hero's current round that both the tray and the overview need: which card
    /// was played on which turn, and what colour a card is.
    ///
    /// Stated once, here, because two readings of the same authority that disagreed would put a colour in
    /// a slot that the tray did not show, and neither surface would be obviously wrong.
    /// </summary>
    public static class CurrentRoundCards
    {
        /// <summary>
        /// The card's own printed colour, from its definition's progression profile. Never inferred from
        /// team, hero, hand position, display name, art, or action type - the one rule
        /// <see cref="CardColor"/> states about itself.
        /// </summary>
        public static CardColor ColorOf(MatchSessionProjection projection, CardInstance card)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            if (card == null) throw new ArgumentNullException(nameof(card));
            var definition = projection.Definitions
                .FirstOrDefault(value => value.Reference.Equals(card.DefinitionReference));
            return definition?.Progression.Color ?? CardColor.None;
        }

        /// <summary>
        /// The turn this hero played this card on in the current round, or zero when the round's
        /// assignment history does not name it.
        /// </summary>
        public static int PlayedTurnIndex(
            MatchSessionProjection projection,
            HeroId heroId,
            CardInstanceId cardInstanceId)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            foreach (var slot in projection.TurnSlots.Where(value => value.RoundIndex == projection.RoundIndex))
            {
                foreach (var assignment in slot.Assignments)
                {
                    if (assignment.HeroId.Equals(heroId) &&
                        assignment.CardInstanceId.HasValue &&
                        assignment.CardInstanceId.Value.Equals(cardInstanceId))
                    {
                        return slot.TurnIndex;
                    }
                }
            }

            return 0;
        }

        /// <summary>
        /// The four fixed slots of the current round for one hero, in turn order. A turn the round has
        /// not reached, or one this hero passed on, is an empty slot rather than a missing one.
        /// </summary>
        public static IReadOnlyList<HeroTurnSlotViewModel> TurnSlots(
            MatchSessionProjection projection,
            HeroId heroId)
        {
            if (projection == null) throw new ArgumentNullException(nameof(projection));
            var slots = new List<HeroTurnSlotViewModel>(RoundResolutionAuthority.TurnsPerRound);
            for (var turnIndex = 1; turnIndex <= RoundResolutionAuthority.TurnsPerRound; turnIndex++)
            {
                var assignment = projection.TurnSlots
                    .Where(slot => slot.RoundIndex == projection.RoundIndex && slot.TurnIndex == turnIndex)
                    .SelectMany(slot => slot.Assignments)
                    .FirstOrDefault(value => value.HeroId.Equals(heroId) && value.CardInstanceId.HasValue);
                var card = assignment == null
                    ? null
                    : projection.Cards.FirstOrDefault(value =>
                        value.Id.Equals(assignment.CardInstanceId.Value));
                slots.Add(card == null
                    ? new HeroTurnSlotViewModel(turnIndex, CardColor.None, false)
                    : new HeroTurnSlotViewModel(turnIndex, ColorOf(projection, card), true));
            }

            return Array.AsReadOnly(slots.ToArray());
        }

        /// <summary>
        /// The card's printed Initiative and the printed icons its actions carry, compactly, in the one
        /// channel order the whole HUD reads values in. It is definition content formatted, never a
        /// modified total: a hero's item bonuses raise a card's totals, and this line is the card.
        /// </summary>
        public static string ValueLine(CardDefinition definition)
        {
            if (definition == null)
            {
                return string.Empty;
            }

            var parts = new List<string>
            {
                $"INIT {definition.Initiative.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            };
            foreach (var channel in MatchHudTheme.ChannelOrder)
            {
                if (channel == PrintedValueChannel.Initiative)
                {
                    continue;
                }

                var amounts = definition.ActionDefinitions
                    .SelectMany(action => action.BaseValues)
                    .Where(value => value.Channel == channel)
                    .Select(value => value.Amount)
                    .Distinct()
                    .OrderBy(amount => amount)
                    .ToArray();
                foreach (var amount in amounts)
                {
                    parts.Add($"{MatchHudTheme.ChannelToken(channel)} " +
                        amount.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }

            return string.Join(" · ", parts);
        }
    }
}
